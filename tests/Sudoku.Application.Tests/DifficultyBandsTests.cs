using Sudoku.Application;
using Sudoku.Domain;
using Xunit;

namespace Sudoku.Tests.Application;

public class DifficultyBandsTests
{
    [Theory]
    [InlineData(Difficulty.Easy, 1, 2)]
    [InlineData(Difficulty.Normal, 3, 4)]
    [InlineData(Difficulty.Hard, 5, 6)]
    public void Bands_are_ordered_and_disjoint(Difficulty difficulty, int min, int max)
    {
        var band = DifficultyBands.For(difficulty);

        Assert.Equal(min, band.MinTechniqueLevel);
        Assert.Equal(max, band.MaxTechniqueLevel);
        Assert.True(band.IsValid);
    }

    [Fact]
    public void Bands_of_different_levels_do_not_overlap()
    {
        var easy = DifficultyBands.For(Difficulty.Easy);
        var normal = DifficultyBands.For(Difficulty.Normal);
        var hard = DifficultyBands.For(Difficulty.Hard);

        Assert.True(easy.MaxTechniqueLevel < normal.MinTechniqueLevel);
        Assert.True(normal.MaxTechniqueLevel < hard.MinTechniqueLevel);
    }

    [Fact]
    public void For_rejects_unknown_difficulty()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DifficultyBands.For((Difficulty)42));
    }

    [Fact]
    public void Contains_requires_unique_solution_and_band_techniques()
    {
        var band = DifficultyBands.For(Difficulty.Normal);

        var inBand = DifficultyRating.Solved(true, SudokuTechnique.PointingPair, 24);
        Assert.True(DifficultyBands.Contains(inBand, band));

        var tooEasy = DifficultyRating.Solved(true, SudokuTechnique.HiddenSingle, 24);
        Assert.False(DifficultyBands.Contains(tooEasy, band));

        var tooHard = DifficultyRating.Solved(true, SudokuTechnique.HiddenPair, 24);
        Assert.False(DifficultyBands.Contains(tooHard, band));

        var ambiguous = DifficultyRating.Solved(false, SudokuTechnique.PointingPair, 24);
        Assert.False(DifficultyBands.Contains(ambiguous, band));

        var guessing = DifficultyRating.NeedingGuessing(true, 24);
        Assert.False(DifficultyBands.Contains(guessing, band));
    }

    [Fact]
    public void Contains_rejects_null_rating()
    {
        Assert.Throws<ArgumentNullException>(() => DifficultyBands.Contains(null!, DifficultyBands.For(Difficulty.Easy)));
    }
}
