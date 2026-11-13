using Sudoku.Domain;

namespace Sudoku.Application;

/// <summary>
/// Набор игровых опций, доступных игроку: режимы и уровни сложности.
/// Является единственной точкой входа Presentation-слоя в домен.
/// </summary>
public sealed class GameOptions
{
    /// <summary>
    /// Набор опций по умолчанию: все режимы и все уровни сложности.
    /// </summary>
    public static GameOptions Default { get; } = new();

    /// <summary>
    /// Уровни сложности в порядке возрастания сложности.
    /// </summary>
    public IReadOnlyList<Difficulty> Difficulties { get; } =
    [
        Difficulty.Easy,
        Difficulty.Normal,
        Difficulty.Hard,
    ];

    /// <summary>
    /// Режимы игры.
    /// </summary>
    public IReadOnlyList<GameMode> Modes { get; } =
    [
        GameMode.Endless,
        GameMode.TimeAttack,
    ];
}
