using Sudoku.Domain;

namespace Sudoku.Application;

/// <summary>
/// Результат решения головоломки.
/// </summary>
public enum SolveOutcome
{
    /// <summary>
    /// Решений нет: начальное состояние противоречиво или не имеет завершения.
    /// </summary>
    NoSolution = 0,

    /// <summary>
    /// Решение единственное.
    /// </summary>
    UniqueSolution = 1,

    /// <summary>
    /// Решений несколько: поиск остановлен после обнаружения второго решения.
    /// </summary>
    MultipleSolutions = 2,
}
