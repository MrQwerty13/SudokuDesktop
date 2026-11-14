using Sudoku.Application;
using Sudoku.Domain;
using Sudoku.ViewModels;
using Xunit;

namespace Sudoku.Tests.App;

public class MainWindowViewModelTests
{
    private static MainWindowViewModel CreateViewModel(int seed = 31337)
        => new(
            GameOptions.Default,
            new GameSessionFactory(
                new PuzzleGenerator(
                    new MrvSudokuSolver(),
                    new LogicalDifficultyRater(new MrvSudokuSolver()))),
            seed);

    [Fact]
    public void Window_shows_selected_difficulty_before_the_game_starts()
    {
        var viewModel = CreateViewModel();

        Assert.Equal(Difficulty.Easy, viewModel.SelectedDifficulty);
        Assert.Equal("Режим: Endless", viewModel.ModeText);
        Assert.Contains("Выбрана сложность: Easy", viewModel.StatusText);
        Assert.False(viewModel.HasGame);
        Assert.Null(viewModel.Endless);
    }

    [Fact]
    public void Window_starts_endless_game_with_selected_difficulty()
    {
        var viewModel = CreateViewModel();

        viewModel.SelectedDifficulty = Difficulty.Hard;
        viewModel.StartEndlessGameCommand.Execute(null);

        Assert.True(viewModel.HasGame);
        Assert.NotNull(viewModel.Endless);
        Assert.Equal(Difficulty.Hard, viewModel.Endless!.Game.Difficulty);
        Assert.Equal(GameMode.Endless, viewModel.Endless.Game.Mode);
        Assert.Contains("Сложность: Hard", viewModel.StatusText);
    }

    [Fact]
    public void Window_status_follows_the_running_game()
    {
        var viewModel = CreateViewModel();

        viewModel.StartEndlessGameCommand.Execute(null);

        Assert.Contains("Головоломка 1", viewModel.StatusText);
        Assert.Contains("Режим: Endless", viewModel.StatusText);
    }

    [Fact]
    public void Open_menu_ends_the_run()
    {
        var viewModel = CreateViewModel();

        viewModel.StartEndlessGameCommand.Execute(null);
        viewModel.OpenMenuCommand.Execute(null);

        Assert.False(viewModel.HasGame);
        Assert.Null(viewModel.Endless);
        Assert.Contains("Выбрана сложность", viewModel.StatusText);
    }

    [Fact]
    public void Consecutive_runs_use_different_seeds()
    {
        var viewModel = CreateViewModel();

        viewModel.StartEndlessGameCommand.Execute(null);
        var first = viewModel.Endless!.Game.Session.InitialState;

        viewModel.OpenMenuCommand.Execute(null);
        viewModel.StartEndlessGameCommand.Execute(null);
        var second = viewModel.Endless!.Game.Session.InitialState;

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Window_requires_options_and_factory()
    {
        var factory = new GameSessionFactory(
            new PuzzleGenerator(
                new MrvSudokuSolver(),
                new LogicalDifficultyRater(new MrvSudokuSolver())));

        Assert.Throws<ArgumentNullException>(() => new MainWindowViewModel(null!, factory));
        Assert.Throws<ArgumentNullException>(() => new MainWindowViewModel(GameOptions.Default, null!));
    }
}
