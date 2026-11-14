using Sudoku.Domain;

namespace Sudoku.Application;

/// <summary>
/// Генератор головоломок судоку с проверкой единственности решения и оценкой сложности.
/// </summary>
public interface IPuzzleGenerator
{
    /// <summary>
    /// Создаёт головоломку заданного уровня сложности.
    /// </summary>
    /// <param name="difficulty">Требуемый уровень сложности.</param>
    /// <param name="seed">Seed генератора: одинаковый seed даёт одинаковую головоломку.</param>
    /// <returns>Головоломка с решением и подтверждённой оценкой сложности.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Уровень сложности не поддерживается.</exception>
    /// <exception cref="PuzzleGenerationException">
    /// Не удалось построить головоломку заданного уровня за отведённое число попыток.
    /// </exception>
    Puzzle Generate(Difficulty difficulty, int seed);
}
