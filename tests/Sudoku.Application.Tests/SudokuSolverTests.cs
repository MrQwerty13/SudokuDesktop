using System.Diagnostics;
using Sudoku.Application;
using Sudoku.Domain;
using Xunit;

namespace Sudoku.Tests.Application;

public class SudokuSolverTests
{
    private readonly ISudokuSolver _solver = new MrvSudokuSolver();

    [Fact]
    public void Solve_finds_published_solution()
    {
        var puzzle = Grid.FromString(TestPuzzles.Classic);

        var result = _solver.Solve(puzzle);

        Assert.Equal(SolveOutcome.UniqueSolution, result.Outcome);
        Assert.True(result.HasUniqueSolution);
        Assert.Equal(1, result.SolutionCount);
        Assert.NotNull(result.Solution);
        Assert.Equal(TestPuzzles.ClassicSolution, result.Solution!.ToString());
    }

    [Fact]
    public void Solve_accepts_puzzle_with_row_separators()
    {
        var puzzle = Grid.FromString(TestPuzzles.ClassicWithSeparators);

        var result = _solver.Solve(puzzle);

        Assert.Equal(SolveOutcome.UniqueSolution, result.Outcome);
        Assert.Equal(TestPuzzles.ClassicSolution, result.Solution!.ToString());
    }

    [Fact]
    public void Solve_returns_same_grid_for_already_solved_grid()
    {
        var solved = Grid.FromString(TestPuzzles.ClassicSolution);

        var result = _solver.Solve(solved);

        Assert.Equal(SolveOutcome.UniqueSolution, result.Outcome);
        Assert.Equal(solved, result.Solution);
        Assert.True(solved.IsComplete);
        Assert.True(solved.IsConsistent);
    }

    [Fact]
    public void Solve_reports_no_solution_for_conflicting_grid()
    {
        var puzzle = Grid.FromString(TestPuzzles.Conflicting);

        var result = _solver.Solve(puzzle);

        Assert.Equal(SolveOutcome.NoSolution, result.Outcome);
        Assert.False(result.HasSolution);
        Assert.False(result.HasUniqueSolution);
        Assert.Null(result.Solution);
        Assert.Equal(0, result.SolutionCount);
    }

    [Fact]
    public void Solve_reports_no_solution_for_consistent_but_unsolvable_grid()
    {
        var puzzle = Grid.FromString(TestPuzzles.Unsolvable);

        Assert.True(puzzle.IsConsistent);
        Assert.False(puzzle.IsComplete);

        var result = _solver.Solve(puzzle);

        Assert.Equal(SolveOutcome.NoSolution, result.Outcome);
        Assert.False(result.HasSolution);
        Assert.Equal(0, result.SolutionCount);
        Assert.Equal(0, _solver.CountSolutions(puzzle, 5));
    }

    [Fact]
    public void Solve_reports_multiple_solutions_for_ambiguous_grid()
    {
        var puzzle = Grid.FromString(TestPuzzles.Ambiguous);

        var result = _solver.Solve(puzzle);

        Assert.Equal(SolveOutcome.MultipleSolutions, result.Outcome);
        Assert.Equal(2, result.SolutionCount);

        var solutions = result.Solutions.Select(solution => solution.ToString()).ToArray();
        Assert.Contains(TestPuzzles.AmbiguousSolution, solutions);
        Assert.Contains(TestPuzzles.AmbiguousAlternativeSolution, solutions);
    }

    [Fact]
    public void Solve_returns_complete_and_consistent_solution()
    {
        var puzzle = Grid.FromString(TestPuzzles.Classic);

        var result = _solver.Solve(puzzle);

        var solution = result.Solution;
        Assert.NotNull(solution);
        Assert.True(solution!.IsComplete);
        Assert.True(solution.IsConsistent);
    }

    [Fact]
    public void Solve_reports_multiple_solutions_for_empty_grid()
    {
        var result = _solver.Solve(Grid.Empty);

        Assert.Equal(SolveOutcome.MultipleSolutions, result.Outcome);
        Assert.Equal(2, result.SolutionCount);
    }

    [Fact]
    public void CountSolutions_stops_after_reaching_the_limit()
    {
        var puzzle = Grid.FromString(TestPuzzles.Ambiguous);

        Assert.Equal(1, _solver.CountSolutions(puzzle, 1));
        Assert.Equal(2, _solver.CountSolutions(puzzle, 2));
        Assert.Equal(2, _solver.CountSolutions(puzzle, 10));
    }

    [Fact]
    public void CountSolutions_returns_one_for_unique_puzzle()
    {
        var puzzle = Grid.FromString(TestPuzzles.Classic);

        Assert.Equal(1, _solver.CountSolutions(puzzle, 10));
    }

    [Fact]
    public void CountSolutions_returns_zero_for_conflicting_grid()
    {
        var puzzle = Grid.FromString(TestPuzzles.Conflicting);

        Assert.Equal(0, _solver.CountSolutions(puzzle, 5));
    }

    [Fact]
    public void Solve_does_not_modify_input_grid()
    {
        var puzzle = Grid.FromString(TestPuzzles.Classic);
        string before = puzzle.ToString();
        var snapshot = Grid.FromString(puzzle.ToString());

        var result = _solver.Solve(puzzle);

        Assert.Equal(SolveOutcome.UniqueSolution, result.Outcome);
        Assert.Equal(before, puzzle.ToString());
        Assert.Equal(snapshot, puzzle);
    }

    [Fact]
    public void Solve_does_not_modify_input_grid_when_counting_solutions()
    {
        var puzzle = Grid.FromString(TestPuzzles.Ambiguous);
        string before = puzzle.ToString();

        _solver.CountSolutions(puzzle, 5);

        Assert.Equal(before, puzzle.ToString());
    }

    [Fact]
    public void Solve_is_deterministic()
    {
        var puzzle = Grid.FromString(TestPuzzles.Ambiguous);

        var first = _solver.Solve(puzzle);
        var second = _solver.Solve(puzzle);

        Assert.Equal(first.Outcome, second.Outcome);
        Assert.Equal(
            first.Solutions.Select(solution => solution.ToString()),
            second.Solutions.Select(solution => solution.ToString()));
    }

    [Fact]
    public void Solve_rejects_null_grid()
    {
        Assert.Throws<ArgumentNullException>(() => _solver.Solve(null!));
        Assert.Throws<ArgumentNullException>(() => _solver.CountSolutions(null!, 1));
    }

    [Fact]
    public void Solve_rejects_malformed_grid_text_at_the_boundary()
    {
        Assert.Throws<ArgumentException>(() => Grid.FromString("123"));
        Assert.Throws<ArgumentException>(() => Grid.FromString(TestPuzzles.Classic[..80] + "x"));

        Assert.False(Grid.TryFromString(TestPuzzles.Classic[..80] + "x", out var grid));
        Assert.Null(grid);
    }

    [Fact]
    public void CountSolutions_rejects_non_positive_limit()
    {
        var puzzle = Grid.FromString(TestPuzzles.Classic);

        Assert.Throws<ArgumentOutOfRangeException>(() => _solver.CountSolutions(puzzle, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => _solver.CountSolutions(puzzle, -1));
    }

    [Fact]
    public void Solve_completes_quickly()
    {
        var puzzle = Grid.FromString(TestPuzzles.Classic);

        var stopwatch = Stopwatch.StartNew();
        var result = _solver.Solve(puzzle);
        stopwatch.Stop();

        Assert.Equal(SolveOutcome.UniqueSolution, result.Outcome);
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(5),
            $"Решение заняло {stopwatch.Elapsed}, что превышает допустимые 5 секунд.");
    }
}
