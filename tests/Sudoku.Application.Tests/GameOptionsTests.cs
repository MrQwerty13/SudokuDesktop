using Sudoku.Application;
using Sudoku.Domain;
using Xunit;

namespace Sudoku.Tests.Application;

public class GameOptionsTests
{
    [Fact]
    public void Default_options_expose_all_difficulties_in_ascending_order()
    {
        var options = GameOptions.Default;

        Assert.Equal([Difficulty.Easy, Difficulty.Normal, Difficulty.Hard], options.Difficulties);
    }

    [Fact]
    public void Default_options_expose_all_game_modes()
    {
        var options = GameOptions.Default;

        Assert.Equal([GameMode.Endless, GameMode.TimeAttack], options.Modes);
    }
}
