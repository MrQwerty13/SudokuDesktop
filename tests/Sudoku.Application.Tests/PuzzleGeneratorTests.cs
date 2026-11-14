using System.Diagnostics;
using Sudoku.Application;
using Sudoku.Domain;
using Xunit;

namespace Sudoku.Tests.Application;

public class PuzzleGeneratorTests
{
    private readonly ISudokuSolver _solver = new MrvSudokuSolver();
    private readonly LogicalDifficultyRater _rater = new(new MrvSudokuSolver());
    private readonly IPuzzleGenerator _generator = new PuzzleGenerator(
        new MrvSudokuSolver(),
        new LogicalDifficultyRater(new MrvSudokuSolver()));

    [Theory]
    [InlineData(Difficulty.Easy)]
    [InlineData(Difficulty.Normal)]
    [InlineData(Difficulty.Hard)]
    public void Generate_produces_complete_and_consistent_solution(Difficulty difficulty)
    {
        var puzzle = _generator.Generate(difficulty, seed: 20240101);

        Assert.True(puzzle.Solution.IsComplete);
        Assert.True(puzzle.Solution.IsConsistent);
        Assert.Equal(difficulty, puzzle.Difficulty);
        Assert.True(ReferenceSolver.IsValidSolution(puzzle.Solution));
    }

    [Theory]
    [InlineData(Difficulty.Easy)]
    [InlineData(Difficulty.Normal)]
    [InlineData(Difficulty.Hard)]
    public void Generate_keeps_every_given_cell_consistent_with_solution(Difficulty difficulty)
    {
        var puzzle = _generator.Generate(difficulty, seed: 20240102);

        int givens = 0;
        for (int index = 0; index < Grid.CellCount; index++)
        {
            int given = puzzle.Clues[index];
            if (given == Grid.EmptyCell)
            {
                continue;
            }

            givens++;
            Assert.Equal(puzzle.Solution[index], given);
        }

        Assert.Equal(givens, puzzle.Rating.GivensCount);
        Assert.InRange(givens, DifficultyBands.For(difficulty).MinGivens, Grid.CellCount);
    }

    [Theory]
    [InlineData(Difficulty.Easy)]
    [InlineData(Difficulty.Normal)]
    [InlineData(Difficulty.Hard)]
    public void Generate_produces_puzzle_without_repeated_digits_in_source_grid(Difficulty difficulty)
    {
        var puzzle = _generator.Generate(difficulty, seed: 20240103);

        for (int row = 0; row < Grid.Size; row++)
        {
            Assert.Equal(Enumerable.Range(1, Grid.Size), RowValues(puzzle.Solution, row).Order());
        }

        for (int column = 0; column < Grid.Size; column++)
        {
            Assert.Equal(Enumerable.Range(1, Grid.Size), ColumnValues(puzzle.Solution, column).Order());
        }

        for (int box = 0; box < Grid.Size; box++)
        {
            Assert.Equal(Enumerable.Range(1, Grid.Size), BoxValues(puzzle.Solution, box).Order());
        }
    }

    [Theory]
    [InlineData(Difficulty.Easy)]
    [InlineData(Difficulty.Normal)]
    [InlineData(Difficulty.Hard)]
    public void Generate_produces_puzzle_with_exactly_one_solution(Difficulty difficulty)
    {
        var puzzle = _generator.Generate(difficulty, seed: 20240104);

        Assert.Equal(1, _solver.CountSolutions(puzzle.Clues, 2));
        Assert.Equal(1, ReferenceSolver.CountSolutions(puzzle.Clues, 2));

        var result = _solver.Solve(puzzle.Clues);
        Assert.Equal(SolveOutcome.UniqueSolution, result.Outcome);
        Assert.Equal(puzzle.Solution, result.Solution);
    }

    [Theory]
    [InlineData(Difficulty.Easy)]
    [InlineData(Difficulty.Normal)]
    [InlineData(Difficulty.Hard)]
    public void Generate_declared_level_matches_independently_computed_ceiling(Difficulty difficulty)
    {
        var puzzle = _generator.Generate(difficulty, seed: 20240105);

        int minimalLevel = 0;
        for (int level = 1; level <= DifficultyBands.MaxTechniqueLevel; level++)
        {
            if (_rater.CanSolveWith(puzzle.Clues, level))
            {
                minimalLevel = level;
                break;
            }
        }

        Assert.NotEqual(0, minimalLevel);
        Assert.Equal(minimalLevel, puzzle.Rating.TechniqueLevel);

        DifficultyBand band = DifficultyBands.For(difficulty);
        Assert.InRange(minimalLevel, band.MinTechniqueLevel, band.MaxTechniqueLevel);
    }

    [Fact]
    public void Generate_easy_puzzle_is_solvable_by_singles_only()
    {
        var puzzle = _generator.Generate(Difficulty.Easy, seed: 20240106);

        Assert.True(_rater.CanSolveWith(puzzle.Clues, 2));
        Assert.False(puzzle.Rating.RequiresGuessing);
        Assert.InRange(puzzle.Rating.TechniqueLevel, 1, 2);
    }

    [Fact]
    public void Generate_normal_puzzle_requires_pair_techniques()
    {
        var puzzle = _generator.Generate(Difficulty.Normal, seed: 20240107);

        Assert.False(_rater.CanSolveWith(puzzle.Clues, 2));
        Assert.True(_rater.CanSolveWith(puzzle.Clues, 4));
        Assert.InRange(puzzle.Rating.TechniqueLevel, 3, 4);
    }

    [Fact]
    public void Generate_hard_puzzle_requires_techniques_above_pointing_and_claiming_pairs()
    {
        var puzzle = _generator.Generate(Difficulty.Hard, seed: 20240108);

        Assert.False(_rater.CanSolveWith(puzzle.Clues, 4));
        Assert.True(_rater.CanSolveWith(puzzle.Clues, 6));
        Assert.InRange(puzzle.Rating.TechniqueLevel, 5, 6);
    }

    [Theory]
    [InlineData(Difficulty.Easy)]
    [InlineData(Difficulty.Normal)]
    [InlineData(Difficulty.Hard)]
    public void Generate_is_reproducible_for_the_same_seed(Difficulty difficulty)
    {
        var first = _generator.Generate(difficulty, seed: 424242);
        var second = _generator.Generate(difficulty, seed: 424242);

        Assert.Equal(first.Clues, second.Clues);
        Assert.Equal(first.Solution, second.Solution);
        Assert.Equal(first.Rating.TechniqueLevel, second.Rating.TechniqueLevel);
        Assert.Equal(first.Rating.GivensCount, second.Rating.GivensCount);
    }

    [Theory]
    [InlineData(Difficulty.Easy)]
    [InlineData(Difficulty.Normal)]
    [InlineData(Difficulty.Hard)]
    public void Generate_produces_different_puzzles_for_different_seeds(Difficulty difficulty)
    {
        var first = _generator.Generate(difficulty, seed: 1);
        var second = _generator.Generate(difficulty, seed: 2);

        Assert.NotEqual(first.Clues, second.Clues);
    }

    [Theory]
    [InlineData(Difficulty.Easy)]
    [InlineData(Difficulty.Normal)]
    [InlineData(Difficulty.Hard)]
    public void Generate_produces_diverse_puzzles_within_a_level(Difficulty difficulty)
    {
        var clues = new HashSet<string>();
        var solutions = new HashSet<string>();

        for (int seed = 1; seed <= 40; seed++)
        {
            var puzzle = _generator.Generate(difficulty, seed);
            clues.Add(puzzle.Clues.ToString());
            solutions.Add(puzzle.Solution.ToString());
        }

        Assert.Equal(40, clues.Count);
        Assert.Equal(40, solutions.Count);
    }

    [Fact]
    public void Generate_rejects_unknown_difficulty()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _generator.Generate((Difficulty)42, seed: 1));
    }

    [Fact]
    public void Generate_requires_solver_and_rater()
    {
        Assert.Throws<ArgumentNullException>(() => new PuzzleGenerator(null!, _rater));
        Assert.Throws<ArgumentNullException>(() => new PuzzleGenerator(_solver, null!));
    }

    [Fact]
    public void Generate_does_not_modify_published_test_data()
    {
        var puzzle = Grid.FromString(TestPuzzles.Classic);
        string before = puzzle.ToString();

        _generator.Generate(Difficulty.Easy, seed: 7);

        Assert.Equal(before, puzzle.ToString());
    }

    [Theory]
    [InlineData(Difficulty.Easy, 1.0)]
    [InlineData(Difficulty.Normal, 1.0)]
    [InlineData(Difficulty.Hard, 5.0)]
    public void Generate_stays_within_documented_time_budget(Difficulty difficulty, double budgetSeconds)
    {
        const int Runs = 20;

        var durations = new List<double>(Runs);
        for (int seed = 1; seed <= Runs; seed++)
        {
            var stopwatch = Stopwatch.StartNew();
            var puzzle = _generator.Generate(difficulty, seed);
            stopwatch.Stop();

            Assert.True(puzzle.Rating.HasUniqueSolution);
            durations.Add(stopwatch.Elapsed.TotalSeconds);
        }

        durations.Sort();
        double p95 = durations[(int)(Runs * 0.95) - 1];

        Assert.True(
            p95 < budgetSeconds,
            $"95-й перцентиль генерации {difficulty} составил {p95:F3} с, бюджет — {budgetSeconds:F1} с.");
    }

    private static IEnumerable<int> RowValues(Grid grid, int row)
    {
        for (int column = 0; column < Grid.Size; column++)
        {
            yield return grid[row, column];
        }
    }

    private static IEnumerable<int> ColumnValues(Grid grid, int column)
    {
        for (int row = 0; row < Grid.Size; row++)
        {
            yield return grid[row, column];
        }
    }

    private static IEnumerable<int> BoxValues(Grid grid, int box)
    {
        int startRow = (box / 3) * 3;
        int startColumn = (box % 3) * 3;

        for (int row = startRow; row < startRow + 3; row++)
        {
            for (int column = startColumn; column < startColumn + 3; column++)
            {
                yield return grid[row, column];
            }
        }
    }
}
