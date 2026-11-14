using Sudoku.Application;
using Sudoku.Domain;
using Xunit;

namespace Sudoku.Tests.Application;

public class LogicalDifficultyRaterTests
{
    private readonly ISudokuSolver _solver = new MrvSudokuSolver();
    private readonly LogicalDifficultyRater _rater = new(new MrvSudokuSolver());

    [Fact]
    public void Rate_reports_naked_single_for_puzzle_with_one_missing_cell()
    {
        var puzzle = Grid.FromString(TestPuzzles.ClassicSolution[..2] + "0" + TestPuzzles.ClassicSolution[3..]);

        var rating = _rater.Rate(puzzle);

        Assert.True(rating.HasSolution);
        Assert.True(rating.HasUniqueSolution);
        Assert.False(rating.RequiresGuessing);
        Assert.Equal(SudokuTechnique.NakedSingle, rating.TechniqueCeiling);
        Assert.Equal(1, rating.TechniqueLevel);
        Assert.Equal(80, rating.GivensCount);
    }

    [Fact]
    public void Rate_reports_unique_solution_for_published_puzzle()
    {
        var puzzle = Grid.FromString(TestPuzzles.Classic);

        var rating = _rater.Rate(puzzle);

        Assert.True(rating.HasSolution);
        Assert.True(rating.HasUniqueSolution);
        Assert.False(rating.RequiresGuessing);
        Assert.InRange(rating.TechniqueLevel, 1, 6);
        Assert.Equal(30, rating.GivensCount);
    }

    [Fact]
    public void Rate_reports_ceiling_consistently_with_layered_solving()
    {
        var puzzle = Grid.FromString(TestPuzzles.Classic);

        var rating = _rater.Rate(puzzle);

        int minimalLevel = 0;
        for (int level = 1; level <= 6; level++)
        {
            if (_rater.CanSolveWith(puzzle, level))
            {
                minimalLevel = level;
                break;
            }
        }

        Assert.Equal(minimalLevel, rating.TechniqueLevel);
    }

    [Fact]
    public void Rate_reports_no_solution_for_conflicting_grid()
    {
        var rating = _rater.Rate(Grid.FromString(TestPuzzles.Conflicting));

        Assert.False(rating.HasSolution);
        Assert.False(rating.HasUniqueSolution);
        Assert.Null(rating.TechniqueCeiling);
        Assert.False(rating.RequiresGuessing);
        Assert.Equal(0, rating.TechniqueLevel);
    }

    [Fact]
    public void Rate_reports_no_solution_for_consistent_but_unsolvable_grid()
    {
        var rating = _rater.Rate(Grid.FromString(TestPuzzles.Unsolvable));

        Assert.False(rating.HasSolution);
        Assert.False(rating.HasUniqueSolution);
        Assert.Null(rating.TechniqueCeiling);
    }

    [Fact]
    public void Rate_reports_multiple_solutions_for_ambiguous_grid()
    {
        // Неоднозначная головоломка построена на «смертельном узоре»: две цифры занимают
        // четыре ячейки в пересечении двух строк и двух столбцов, поэтому логическими
        // приёмами её не решить — нужен перебор.
        var rating = _rater.Rate(Grid.FromString(TestPuzzles.Ambiguous));

        Assert.True(rating.HasSolution);
        Assert.False(rating.HasUniqueSolution);
        Assert.Null(rating.TechniqueCeiling);
        Assert.True(rating.RequiresGuessing);
    }

    [Fact]
    public void CanSolveWith_rejects_out_of_range_level()
    {
        var puzzle = Grid.FromString(TestPuzzles.Classic);

        Assert.Throws<ArgumentOutOfRangeException>(() => _rater.CanSolveWith(puzzle, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => _rater.CanSolveWith(puzzle, 7));
        Assert.Throws<ArgumentNullException>(() => _rater.CanSolveWith(null!, 1));
    }

    [Fact]
    public void Rate_rejects_null_grid()
    {
        Assert.Throws<ArgumentNullException>(() => _rater.Rate(null!));
    }

    [Fact]
    public void Rater_requires_solver()
    {
        Assert.Throws<ArgumentNullException>(() => new LogicalDifficultyRater(null!));
    }

    [Fact]
    public void Solver_is_not_shared_with_rater_state()
    {
        var puzzle = Grid.FromString(TestPuzzles.Classic);
        string before = puzzle.ToString();

        _rater.Rate(puzzle);

        Assert.Equal(before, puzzle.ToString());
        Assert.NotNull(_solver);
    }
}
