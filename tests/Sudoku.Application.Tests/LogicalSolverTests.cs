using Sudoku.Application;
using Sudoku.Domain;
using Xunit;

namespace Sudoku.Tests.Application;

/// <summary>
/// Тесты логического решателя — основы оценки сложности.
/// </summary>
/// <remarks>
/// Ключевое свойство: любой вывод приёма должен быть корректным. Поэтому найденное
/// логическое заполнение обязано совпадать с истинным решением головоломки, а
/// «потолок приёмов» головоломки обязан совпадать с минимальным уровнем, на котором
/// она решается.
/// </remarks>
public class LogicalSolverTests
{
    private static readonly string ClassicSolution =
        "534678912672195348198342567859761423426853791713924856961537284287419635345286179";

    [Fact]
    public void Solve_finishes_puzzle_with_single_missing_cell()
    {
        var puzzle = Grid.FromString(ClassicSolution[..40] + "0" + ClassicSolution[41..]);

        var solver = new LogicalSolver(puzzle);

        Assert.True(solver.TrySolve(1, out var solution));
        Assert.NotNull(solution);
        Assert.Equal(ClassicSolution, solution!.ToString());
    }

    [Fact]
    public void Solve_finishes_classic_puzzle_with_naked_singles_only()
    {
        // Каноническая головоломка из «Википедии» решается одними одиночными приёмами:
        // её потолок техник равен 1.
        var puzzle = Grid.FromString(TestPuzzles.Classic);

        var solver = new LogicalSolver(puzzle);

        Assert.True(solver.TrySolve(1, out var solution));
        Assert.NotNull(solution);
        Assert.Equal(TestPuzzles.ClassicSolution, solution!.ToString());
    }

    [Fact]
    public void Solve_reports_failure_when_allowed_level_is_too_low()
    {
        var generator = new PuzzleGenerator(
            new MrvSudokuSolver(),
            new LogicalDifficultyRater(new MrvSudokuSolver()));
        var puzzle = generator.Generate(Difficulty.Hard, seed: 11);

        var solver = new LogicalSolver(puzzle.Clues);

        Assert.False(solver.TrySolve(1, out var solution));
        Assert.Null(solution);
    }

    [Theory]
    [InlineData(Difficulty.Easy)]
    [InlineData(Difficulty.Normal)]
    [InlineData(Difficulty.Hard)]
    public void Logical_solution_always_equals_the_true_solution(Difficulty difficulty)
    {
        var generator = new PuzzleGenerator(
            new MrvSudokuSolver(),
            new LogicalDifficultyRater(new MrvSudokuSolver()));

        for (int seed = 1; seed <= 15; seed++)
        {
            var puzzle = generator.Generate(difficulty, seed);

            var solver = new LogicalSolver(puzzle.Clues);

            Assert.True(solver.TrySolve(DifficultyBands.MaxTechniqueLevel, out var solution));
            Assert.Equal(puzzle.Solution, solution);
        }
    }

    [Theory]
    [InlineData(Difficulty.Easy)]
    [InlineData(Difficulty.Normal)]
    [InlineData(Difficulty.Hard)]
    public void Technique_ceiling_is_minimal_level_that_solves_the_puzzle(Difficulty difficulty)
    {
        var generator = new PuzzleGenerator(
            new MrvSudokuSolver(),
            new LogicalDifficultyRater(new MrvSudokuSolver()));

        for (int seed = 1; seed <= 15; seed++)
        {
            var puzzle = generator.Generate(difficulty, seed);
            int ceiling = puzzle.Rating.TechniqueLevel;

            if (ceiling > 1)
            {
                var belowCeiling = new LogicalSolver(puzzle.Clues);
                Assert.False(belowCeiling.TrySolve(ceiling - 1, out _));
            }

            var atCeiling = new LogicalSolver(puzzle.Clues);
            Assert.True(atCeiling.TrySolve(ceiling, out var solution));
            Assert.Equal(puzzle.Solution, solution);
        }
    }

    [Fact]
    public void Solve_detects_contradictory_grid()
    {
        var puzzle = Grid.FromString(TestPuzzles.Conflicting);

        var solver = new LogicalSolver(puzzle);

        Assert.False(solver.SolveUpTo(DifficultyBands.MaxTechniqueLevel));
    }

    [Fact]
    public void Solve_detects_unsolvable_but_consistent_grid()
    {
        var puzzle = Grid.FromString(TestPuzzles.Unsolvable);

        var solver = new LogicalSolver(puzzle);

        Assert.False(solver.SolveUpTo(DifficultyBands.MaxTechniqueLevel));
    }

    [Fact]
    public void Solve_rejects_out_of_range_level()
    {
        var solver = new LogicalSolver(Grid.FromString(TestPuzzles.Classic));

        Assert.Throws<ArgumentOutOfRangeException>(() => solver.SolveUpTo(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => solver.SolveUpTo(7));
        Assert.Throws<ArgumentOutOfRangeException>(() => solver.Solve(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => solver.Solve(7));
    }

    [Fact]
    public void Solve_rejects_null_grid()
    {
        Assert.Throws<ArgumentNullException>(() => new LogicalSolver(null!));
    }

    [Fact]
    public void Escalating_levels_reuse_state_without_changing_the_result()
    {
        var generator = new PuzzleGenerator(
            new MrvSudokuSolver(),
            new LogicalDifficultyRater(new MrvSudokuSolver()));
        var puzzle = generator.Generate(Difficulty.Normal, seed: 12);

        var incremental = new LogicalSolver(puzzle.Clues);
        Assert.False(incremental.SolveUpTo(1));
        Assert.False(incremental.SolveUpTo(2));

        var direct = new LogicalSolver(puzzle.Clues);
        Assert.Equal(direct.SolveUpTo(4), incremental.SolveUpTo(4));
    }
}
