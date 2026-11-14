using Sudoku.Domain;

namespace Sudoku.Application;

/// <summary>
/// Создание игровых сессий.
/// </summary>
public interface IGameSessionFactory
{
    /// <summary>
    /// Создаёт новую игру: генерирует головоломку выбранной сложности и создаёт сессию.
    /// </summary>
    /// <param name="difficulty">Уровень сложности новой головоломки.</param>
    /// <param name="seed">Seed генератора: одинаковый seed даёт одинаковую головоломку.</param>
    /// <returns>Готовая игровая сессия.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Уровень сложности не поддерживается.</exception>
    /// <exception cref="PuzzleGenerationException">
    /// Не удалось построить головоломку заданного уровня за отведённое число попыток.
    /// </exception>
    GameSession CreateNewGame(Difficulty difficulty, int seed);
}
