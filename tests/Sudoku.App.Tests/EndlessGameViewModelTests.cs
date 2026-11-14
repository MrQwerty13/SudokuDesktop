using Sudoku.Application;
using Sudoku.Domain;
using Sudoku.ViewModels;
using Xunit;

namespace Sudoku.Tests.App;

public class EndlessGameViewModelTests
{
    private static EndlessGameViewModel CreateViewModel(Action? onExit = null, int seed = 909)
    {
        var factory = new GameSessionFactory(
            new PuzzleGenerator(
                new MrvSudokuSolver(),
                new LogicalDifficultyRater(new MrvSudokuSolver())));

        var game = new EndlessGame(factory, Difficulty.Normal, seed);

        return new EndlessGameViewModel(game, onExit);
    }

    [Fact]
    public void View_model_shows_mode_difficulty_and_state()
    {
        var viewModel = CreateViewModel();

        Assert.Equal("Режим: Endless", viewModel.ModeText);
        Assert.Equal("Сложность: Normal", viewModel.DifficultyText);
        Assert.Contains("Головоломка 1", viewModel.StatusText);
        Assert.Contains("Решено: 0", viewModel.StatusText);
        Assert.Contains("Подсказки: 0", viewModel.StatusText);
        Assert.Equal("Идёт решение", viewModel.BoardStateText);
        Assert.Equal("Время не ограничено", viewModel.TimerText);
        Assert.False(viewModel.IsCurrentPuzzleSolved);
    }

    [Fact]
    public void View_model_contains_all_cells_of_the_board()
    {
        var viewModel = CreateViewModel();

        Assert.Equal(81, viewModel.Cells.Count);
        Assert.Equal(viewModel.Game.Session.Puzzle.Rating.GivensCount, viewModel.Cells.Count(cell => cell.IsGiven));
        Assert.All(
            viewModel.Cells.Where(cell => cell.IsGiven),
            cell => Assert.NotEqual(string.Empty, cell.Text));
        Assert.All(
            viewModel.Cells.Where(cell => !cell.IsGiven),
            cell => Assert.Equal(string.Empty, cell.Text));
    }

    [Fact]
    public void Selecting_cell_marks_it_as_selected()
    {
        var viewModel = CreateViewModel();
        var cell = viewModel.Cells[0];

        viewModel.SelectCellCommand.Execute(cell);

        Assert.True(viewModel.Cells[0].IsSelected);
        Assert.Single(viewModel.Cells, item => item.IsSelected);
    }

    [Fact]
    public void Entering_digit_shows_it_in_the_selected_cell()
    {
        var viewModel = CreateViewModel();
        var emptyCell = viewModel.Cells.First(cell => !cell.IsGiven);

        viewModel.SelectCellCommand.Execute(emptyCell);
        viewModel.EnterDigitCommand.Execute("4");

        Assert.Equal("4", viewModel.Cells[emptyCell.Index].Text);
        Assert.False(viewModel.Cells[emptyCell.Index].IsGiven);
    }

    [Fact]
    public void Entering_invalid_digit_changes_nothing()
    {
        var viewModel = CreateViewModel();
        var emptyCell = viewModel.Cells.First(cell => !cell.IsGiven);

        viewModel.SelectCellCommand.Execute(emptyCell);
        viewModel.EnterDigitCommand.Execute("не цифра");

        Assert.Equal(string.Empty, viewModel.Cells[emptyCell.Index].Text);
    }

    [Fact]
    public void Erasing_clears_the_value_in_the_selected_cell()
    {
        var viewModel = CreateViewModel();
        var emptyCell = viewModel.Cells.First(cell => !cell.IsGiven);

        viewModel.SelectCellCommand.Execute(emptyCell);
        viewModel.EnterDigitCommand.Execute("4");
        viewModel.EraseCommand.Execute(null);

        Assert.Equal(string.Empty, viewModel.Cells[emptyCell.Index].Text);
    }

    [Fact]
    public void Hint_fills_cell_with_confirmed_solution_value()
    {
        var viewModel = CreateViewModel();
        var emptyCell = viewModel.Cells.First(cell => !cell.IsGiven);
        int expected = viewModel.Game.Session.Solution[emptyCell.Index];

        viewModel.SelectCellCommand.Execute(emptyCell);
        viewModel.HintCommand.Execute(null);

        Assert.Equal(expected.ToString(), viewModel.Cells[emptyCell.Index].Text);
        Assert.Contains("Подсказки: 1", viewModel.StatusText);
    }

    [Fact]
    public void New_puzzle_starts_next_puzzle_and_resets_the_board()
    {
        var viewModel = CreateViewModel();
        var emptyCell = viewModel.Cells.First(cell => !cell.IsGiven);

        viewModel.SelectCellCommand.Execute(emptyCell);
        viewModel.HintCommand.Execute(null);
        viewModel.NewPuzzleCommand.Execute(null);

        Assert.Contains("Головоломка 2", viewModel.StatusText);
        Assert.Contains("Подсказки: 1", viewModel.StatusText);
        Assert.All(
            viewModel.Cells.Where(cell => !cell.IsGiven),
            cell => Assert.Equal(string.Empty, cell.Text));
        Assert.DoesNotContain(viewModel.Cells, cell => cell.IsSelected);
    }

    [Fact]
    public void Exit_invokes_the_exit_handler()
    {
        bool exited = false;
        var viewModel = CreateViewModel(() => exited = true);

        viewModel.ExitCommand.Execute(null);

        Assert.True(exited);
    }

    [Fact]
    public void View_model_requires_game()
    {
        Assert.Throws<ArgumentNullException>(() => new EndlessGameViewModel(null!));
    }
}
