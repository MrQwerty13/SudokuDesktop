using System.Diagnostics;
using Sudoku.Application;
using Sudoku.Domain;
using Xunit;

namespace Sudoku.Tests.Application;

/// <summary>
/// Статистическая проверка качества генератора на выборке головоломок.
/// </summary>
/// <remarks>
/// Отдельные тесты проверяют отдельные свойства одной головоломки; этот класс
/// проверяет, что свойства выполняются <em>для всей выборки</em>: каждая
/// головоломка единственна, попадает в заявленный уровень и отличается от соседей.
/// </remarks>
public class PuzzleGeneratorBatchTests
{
    private const int SampleSize = 100;

    /// <summary>
    /// Признак того, что полную выборку из 1000 головоломок нужно пропустить.
    /// Выборка выполняется только при переменной окружения <c>SUDOKU_FULL_BATCH=1</c>.
    /// </summary>
    public static bool SkipFullBatch => Environment.GetEnvironmentVariable("SUDOKU_FULL_BATCH") != "1";

    [Theory]
    [InlineData(Difficulty.Easy)]
    [InlineData(Difficulty.Normal)]
    [InlineData(Difficulty.Hard)]
    public void Batch_of_generated_puzzles_is_unique_and_in_declared_band(Difficulty difficulty)
    {
        var generator = new PuzzleGenerator(
            new MrvSudokuSolver(),
            new LogicalDifficultyRater(new MrvSudokuSolver()));
        var solver = new MrvSudokuSolver();
        var rater = new LogicalDifficultyRater(new MrvSudokuSolver());
        DifficultyBand band = DifficultyBands.For(difficulty);

        var clueSets = new HashSet<string>();
        var stopwatch = Stopwatch.StartNew();

        for (int seed = 1; seed <= SampleSize; seed++)
        {
            var puzzle = generator.Generate(difficulty, seed);

            Assert.Equal(difficulty, puzzle.Difficulty);
            Assert.True(puzzle.Solution.IsComplete);
            Assert.True(puzzle.Solution.IsConsistent);
            Assert.True(ReferenceSolver.IsValidSolution(puzzle.Solution));

            Assert.Equal(1, solver.CountSolutions(puzzle.Clues, 2));
            Assert.Equal(1, ReferenceSolver.CountSolutions(puzzle.Clues, 2));

            Assert.True(puzzle.Rating.HasUniqueSolution);
            Assert.False(puzzle.Rating.RequiresGuessing);
            Assert.InRange(puzzle.Rating.TechniqueLevel, band.MinTechniqueLevel, band.MaxTechniqueLevel);
            Assert.True(DifficultyBands.Contains(puzzle.Rating, band));

            int minimalLevel = 0;
            for (int level = 1; level <= DifficultyBands.MaxTechniqueLevel; level++)
            {
                if (rater.CanSolveWith(puzzle.Clues, level))
                {
                    minimalLevel = level;
                    break;
                }
            }

            Assert.Equal(minimalLevel, puzzle.Rating.TechniqueLevel);

            clueSets.Add(puzzle.Clues.ToString());
        }

        stopwatch.Stop();

        Assert.Equal(SampleSize, clueSets.Count);
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromMinutes(2),
            $"Выборка из {SampleSize} головоломок уровня {difficulty} заняла {stopwatch.Elapsed}.");
    }

    [Fact]
    public void Batch_generation_is_reproducible_for_the_same_seeds()
    {
        var first = new PuzzleGenerator(
            new MrvSudokuSolver(),
            new LogicalDifficultyRater(new MrvSudokuSolver()));
        var second = new PuzzleGenerator(
            new MrvSudokuSolver(),
            new LogicalDifficultyRater(new MrvSudokuSolver()));

        for (int seed = 1; seed <= 20; seed++)
        {
            foreach (Difficulty difficulty in new[] { Difficulty.Easy, Difficulty.Normal, Difficulty.Hard })
            {
                var firstPuzzle = first.Generate(difficulty, seed);
                var secondPuzzle = second.Generate(difficulty, seed);

                Assert.Equal(firstPuzzle.Clues, secondPuzzle.Clues);
                Assert.Equal(firstPuzzle.Solution, secondPuzzle.Solution);
                Assert.Equal(firstPuzzle.Rating.TechniqueLevel, secondPuzzle.Rating.TechniqueLevel);
            }
        }
    }

    [Fact(
        Skip = "Полная выборка из 1000 головоломок: запускается при SUDOKU_FULL_BATCH=1.",
        SkipWhen = nameof(SkipFullBatch))]
    public void Batch_of_1000_puzzles_has_exactly_one_solution_each()
    {
        var generator = new PuzzleGenerator(
            new MrvSudokuSolver(),
            new LogicalDifficultyRater(new MrvSudokuSolver()));
        var solver = new MrvSudokuSolver();

        int total = 0;
        foreach (Difficulty difficulty in new[] { Difficulty.Easy, Difficulty.Normal, Difficulty.Hard })
        {
            for (int seed = 1; seed <= 334; seed++)
            {
                var puzzle = generator.Generate(difficulty, seed);

                Assert.Equal(1, solver.CountSolutions(puzzle.Clues, 2));
                Assert.True(DifficultyBands.Contains(puzzle.Rating, DifficultyBands.For(difficulty)));

                total++;
            }
        }

        Assert.Equal(1002, total);
    }
}
