using Sudoku.Domain;

namespace Sudoku.Application;

/// <summary>
/// Решатель головоломок судоку.
/// </summary>
public interface ISudokuSolver
{
    /// <summary>
    /// Находит решение головоломки. Поиск останавливается после обнаружения второго решения,
    /// чтобы отличить единственное решение от нескольких.
    /// </summary>
    /// <param name="grid">Начальное состояние головоломки.</param>
    /// <returns>Результат с видом решения и найденными вариантами.</returns>
    /// <exception cref="ArgumentNullException">Параметр <paramref name="grid"/> равен <c>null</c>.</exception>
    SolveResult Solve(Grid grid);

    /// <summary>
    /// Считает количество решений головоломки, но не больше заданного предела.
    /// </summary>
    /// <param name="grid">Начальное состояние головоломки.</param>
    /// <param name="solutionLimit">
    /// Максимальное количество решений, которое нужно найти. Значение 1 достаточно,
    /// чтобы проверить существование решения, значение 2 — чтобы проверить уникальность.
    /// </param>
    /// <returns>Количество найденных решений, не превышающее предел.</returns>
    /// <exception cref="ArgumentNullException">Параметр <paramref name="grid"/> равен <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Предел меньше единицы.</exception>
    int CountSolutions(Grid grid, int solutionLimit);
}
