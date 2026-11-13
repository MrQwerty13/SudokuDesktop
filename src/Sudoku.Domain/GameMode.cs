namespace Sudoku.Domain;

/// <summary>
/// Режим игры.
/// </summary>
public enum GameMode
{
    /// <summary>
    /// Неограниченное число головоломок без ограничения времени.
    /// </summary>
    Endless,

    /// <summary>
    /// Игра с ограничением времени на заход.
    /// </summary>
    TimeAttack,
}
