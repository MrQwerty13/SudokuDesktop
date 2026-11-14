namespace Sudoku.Domain;

/// <summary>
/// Готовая головоломка: заданные значения, решение, уровень сложности и оценка.
/// </summary>
public sealed class Puzzle
{
    /// <summary>
    /// Создаёт головоломку.
    /// </summary>
    /// <param name="clues">Заданные значения, которые видит игрок.</param>
    /// <param name="solution">Полное решение головоломки.</param>
    /// <param name="difficulty">Заявленный уровень сложности.</param>
    /// <param name="rating">Оценка сложности, подтверждающая уровень.</param>
    /// <exception cref="ArgumentNullException">
    /// Параметр <paramref name="clues"/>, <paramref name="solution"/>
    /// или <paramref name="rating"/> равен <c>null</c>.
    /// </exception>
    public Puzzle(Grid clues, Grid solution, Difficulty difficulty, DifficultyRating rating)
    {
        ArgumentNullException.ThrowIfNull(clues);
        ArgumentNullException.ThrowIfNull(solution);
        ArgumentNullException.ThrowIfNull(rating);

        Clues = clues;
        Solution = solution;
        Difficulty = difficulty;
        Rating = rating;
    }

    /// <summary>
    /// Заданные значения головоломки (поле, которое видит игрок).
    /// </summary>
    public Grid Clues { get; }

    /// <summary>
    /// Полное решение головоломки.
    /// </summary>
    public Grid Solution { get; }

    /// <summary>
    /// Уровень сложности головоломки.
    /// </summary>
    public Difficulty Difficulty { get; }

    /// <summary>
    /// Оценка сложности, подтверждающая <see cref="Difficulty"/>.
    /// </summary>
    public DifficultyRating Rating { get; }
}
