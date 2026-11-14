using Sudoku.Application;
using Sudoku.Domain;
using Sudoku.ViewModels;

namespace Sudoku;

/// <summary>
/// Композиционный корень приложения.
/// </summary>
/// <remarks>
/// Объекты создаются напрямую, без DI-контейнера
/// ([ADR-0007](docs/ADR/0007-composition-without-di-container.md)).
/// </remarks>
internal static class Composition
{
    /// <summary>
    /// Создаёт фабрику игровых сессий с реальным генератором головоломок.
    /// </summary>
    /// <returns>Фабрика игровых сессий режима Endless.</returns>
    public static IGameSessionFactory CreateGameSessionFactory()
        => new GameSessionFactory(
            new PuzzleGenerator(
                new MrvSudokuSolver(),
                new LogicalDifficultyRater(new MrvSudokuSolver())));

    /// <summary>
    /// Создаёт модель главного окна.
    /// </summary>
    /// <returns>Модель главного окна.</returns>
    public static MainWindowViewModel CreateMainWindowViewModel()
        => new(GameOptions.Default, CreateGameSessionFactory());
}
