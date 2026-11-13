using Sudoku.Domain;
using Xunit;

namespace Sudoku.Tests.Domain;

public class GameModeTests
{
    [Fact]
    public void GameMode_exposes_endless_and_time_attack()
    {
        var modes = Enum.GetValues<GameMode>();

        Assert.Equal([GameMode.Endless, GameMode.TimeAttack], modes);
    }
}
