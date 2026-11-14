using Sudoku.Domain;

namespace Sudoku.Application;

/// <summary>
/// Создание игровых сессий.
/// </summary>
/// <remarks>
/// Интерфейс содержит по одному методу на режим. Это сделано намеренно: сценарий
/// Endless физически не может запустить сессию Time Attack (и наоборот) — для другого
/// режима нужен другой метод.
/// </remarks>
public interface IGameSessionFactory
{
    /// <summary>
    /// Создаёт новую игру в режиме Endless: генерирует головоломку выбранной сложности
    /// и создаёт сессию.
    /// </summary>
    /// <param name="difficulty">Уровень сложности новой головоломки.</param>
    /// <param name="seed">Seed генератора: одинаковый seed даёт одинаковую головоломку.</param>
    /// <returns>Готовая игровая сессия режима Endless.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Уровень сложности не поддерживается.</exception>
    /// <exception cref="PuzzleGenerationException">
    /// Не удалось построить головоломку заданного уровня за отведённое число попыток.
    /// </exception>
    GameSession CreateNewEndlessGame(Difficulty difficulty, int seed);
}
