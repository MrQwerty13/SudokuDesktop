namespace Sudoku.Domain;

/// <summary>
/// Уровень сложности головоломки, предлагаемый игроку.
/// </summary>
public enum Difficulty
{
    /// <summary>
    /// Простая головоломка, решаемая базовыми приёмами.
    /// </summary>
    Easy,

    /// <summary>
    /// Головоломка средней сложности.
    /// </summary>
    Normal,

    /// <summary>
    /// Сложная головоломка, требующая продвинутых приёмов.
    /// </summary>
    Hard,
}
