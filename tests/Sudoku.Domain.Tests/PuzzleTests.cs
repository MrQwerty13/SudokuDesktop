using Sudoku.Domain;
using Xunit;

namespace Sudoku.Tests.Domain;

public class PuzzleTests
{
    private static readonly Grid Clues = Grid.FromString(
        "53..7....6..195....98....6.8...6...34..8.3..17...2...6.6....28....419..5....8..79");

    private static readonly Grid Solution = Grid.FromString(
        "534678912672195348198342567859761423426853791713924856961537284287419635345286179");

    [Fact]
    public void Constructor_keeps_given_state()
    {
        var rating = DifficultyRating.Solved(true, SudokuTechnique.HiddenSingle, 30);

        var puzzle = new Puzzle(Clues, Solution, Difficulty.Normal, rating);

        Assert.Same(Clues, puzzle.Clues);
        Assert.Same(Solution, puzzle.Solution);
        Assert.Equal(Difficulty.Normal, puzzle.Difficulty);
        Assert.Same(rating, puzzle.Rating);
    }

    [Fact]
    public void Constructor_rejects_null_arguments()
    {
        var rating = DifficultyRating.Solved(true, SudokuTechnique.NakedSingle, 30);

        Assert.Throws<ArgumentNullException>(() => new Puzzle(null!, Solution, Difficulty.Easy, rating));
        Assert.Throws<ArgumentNullException>(() => new Puzzle(Clues, null!, Difficulty.Easy, rating));
        Assert.Throws<ArgumentNullException>(() => new Puzzle(Clues, Solution, Difficulty.Easy, null!));
    }

    [Fact]
    public void Rating_reports_unsolvable_state()
    {
        var rating = DifficultyRating.Unsolvable(30);

        Assert.False(rating.HasSolution);
        Assert.False(rating.HasUniqueSolution);
        Assert.False(rating.RequiresGuessing);
        Assert.Null(rating.TechniqueCeiling);
        Assert.Equal(0, rating.TechniqueLevel);
        Assert.Equal(30, rating.GivensCount);
    }

    [Fact]
    public void Rating_reports_state_needing_guessing()
    {
        var rating = DifficultyRating.NeedingGuessing(false, 25);

        Assert.True(rating.HasSolution);
        Assert.False(rating.HasUniqueSolution);
        Assert.True(rating.RequiresGuessing);
        Assert.Null(rating.TechniqueCeiling);
        Assert.Equal(0, rating.TechniqueLevel);
    }

    [Fact]
    public void Rating_reports_technique_ceiling()
    {
        var rating = DifficultyRating.Solved(true, SudokuTechnique.NakedPair, 24);

        Assert.True(rating.HasSolution);
        Assert.True(rating.HasUniqueSolution);
        Assert.False(rating.RequiresGuessing);
        Assert.Equal(SudokuTechnique.NakedPair, rating.TechniqueCeiling);
        Assert.Equal(5, rating.TechniqueLevel);
    }
}
