namespace Sudoku.Application;

/// <summary>
/// Источник случайных чисел. Внедряется, чтобы генерация была воспроизводимой.
/// </summary>
public interface IRandomSource
{
    /// <summary>
    /// Возвращает случайное целое число из диапазона <c>[0, exclusiveUpperBound)</c>.
    /// </summary>
    /// <param name="exclusiveUpperBound">Верхняя граница диапазона (exclusive), должна быть положительной.</param>
    /// <returns>Случайное число в диапазоне <c>[0, exclusiveUpperBound)</c>.</returns>
    int Next(int exclusiveUpperBound);
}
