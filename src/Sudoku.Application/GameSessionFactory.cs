using Sudoku.Domain;

namespace Sudoku.Application;

/// <summary>
/// Фабрика игровых сессий: новая игра = сгенерированная головоломка + сессия.
/// </summary>
/// <remarks>
/// Сценарий намеренно тонкий: вся игровая механика живёт в
/// <see cref="Sudoku.Domain.GameSession"/>, фабрика отвечает только за получение
/// головоломки нужного уровня и создание сессии по ней. Генерация детерминирована:
/// одинаковые сложность и seed дают одинаковую игру.
/// </remarks>
public sealed class GameSessionFactory : IGameSessionFactory
{
    private readonly IPuzzleGenerator _generator;

    /// <summary>
    /// Создаёт фабрику игровых сессий.
    /// </summary>
    /// <param name="generator">Генератор головоломок.</param>
    /// <exception cref="ArgumentNullException">Параметр <paramref name="generator"/> равен <c>null</c>.</exception>
    public GameSessionFactory(IPuzzleGenerator generator)
    {
        ArgumentNullException.ThrowIfNull(generator);

        _generator = generator;
    }

    /// <inheritdoc />
    public GameSession CreateNewGame(Difficulty difficulty, int seed)
    {
        Puzzle puzzle = _generator.Generate(difficulty, seed);

        return new GameSession(puzzle);
    }
}
