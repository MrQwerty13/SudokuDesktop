using Sudoku.Domain;
using Xunit;

namespace Sudoku.Tests.Domain;

public class DifficultyTests
{
    [Fact]
    public void Difficulty_exposes_exactly_three_levels()
    {
        var levels = Enum.GetValues<Difficulty>();

        Assert.Equal([Difficulty.Easy, Difficulty.Normal, Difficulty.Hard], levels);
    }

    [Fact]
    public void Difficulty_levels_are_ordered_from_easy_to_hard()
    {
        Assert.True(Difficulty.Easy < Difficulty.Normal);
        Assert.True(Difficulty.Normal < Difficulty.Hard);
    }
}
