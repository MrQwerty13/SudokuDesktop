using Sudoku.Domain;

namespace Sudoku.Application;

/// <summary>
/// Оценка сложности головоломки по потолку логических приёмов.
/// </summary>
public interface IDifficultyRater
{
    /// <summary>
    /// Оценивает головоломку.
    /// </summary>
    /// <param name="puzzle">Головоломка для оценки.</param>
    /// <returns>
    /// Оценка: наличие решения, единственность и потолок приёмов
    /// (<see cref="DifficultyRating"/>).
    /// </returns>
    /// <exception cref="ArgumentNullException">Параметр <paramref name="puzzle"/> равен <c>null</c>.</exception>
    DifficultyRating Rate(Grid puzzle);

    /// <summary>
    /// Проверяет, решается ли головоломка только приёмами уровнем не выше заданного.
    /// </summary>
    /// <param name="puzzle">Головоломка для проверки.</param>
    /// <param name="maxTechniqueLevel">Максимальный уровень приёма: 1–6.</param>
    /// <returns><c>true</c>, если головоломка решена логическими приёмами.</returns>
    /// <exception cref="ArgumentNullException">Параметр <paramref name="puzzle"/> равен <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Уровень приёма вне диапазона 1–6.</exception>
    bool CanSolveWith(Grid puzzle, int maxTechniqueLevel);
}
