using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Sudoku.Application;
using Sudoku.Domain;
using Sudoku.ViewModels;
using Sudoku.Views;
using Xunit;

namespace Sudoku.Tests.App;

public class MainWindowTests
{
    [AvaloniaFact]
    public void MainWindow_opens_with_application_shell()
    {
        var window = new MainWindow
        {
            DataContext = new MainWindowViewModel(),
        };

        window.Show();

        Assert.Equal("Sudoku Desktop", window.Title);
        Assert.True(window.IsVisible);
    }

    [AvaloniaFact]
    public void MainWindow_shows_mode_and_selected_difficulty()
    {
        var window = new MainWindow
        {
            DataContext = CreateViewModel(),
        };

        window.Show();

        var texts = window.GetVisualDescendants()
            .OfType<TextBlock>()
            .Select(text => text.Text)
            .ToArray();

        Assert.Contains("Sudoku Desktop", texts);
        Assert.Contains("Режим: Endless", texts);
        Assert.Contains(texts, text => text is not null && text.Contains("Выбрана сложность: Easy"));
    }

    [AvaloniaFact]
    public void MainWindow_shows_endless_game_state_after_start()
    {
        var viewModel = CreateViewModel();
        var window = new MainWindow { DataContext = viewModel };

        window.Show();

        viewModel.StartEndlessGameCommand.Execute(null);
        window.UpdateLayout();

        var texts = window.GetVisualDescendants()
            .OfType<TextBlock>()
            .Select(text => text.Text)
            .ToArray();

        Assert.Contains("Режим: Endless", texts);
        Assert.Contains(texts, text => text is not null && text.Contains("Сложность: Easy"));
        Assert.Contains(texts, text => text is not null && text.Contains("Головоломка 1"));
        Assert.Contains("Время не ограничено", texts);
    }

    [AvaloniaFact]
    public void MainWindow_renders_endless_board()
    {
        var viewModel = CreateViewModel();
        var window = new MainWindow { DataContext = viewModel };

        window.Show();

        viewModel.StartEndlessGameCommand.Execute(null);
        window.UpdateLayout();

        var buttons = window.GetVisualDescendants()
            .OfType<Button>()
            .ToArray();

        // 81 клетка поля плюс кнопки ввода цифр и действия.
        Assert.True(buttons.Length > 81, $"Ожидалось поле из 81 клетки, найдено кнопок: {buttons.Length}.");
    }

    [AvaloniaFact]
    public void MainWindow_marks_given_and_user_cells_with_styles()
    {
        var viewModel = CreateViewModel();
        var window = new MainWindow { DataContext = viewModel };

        window.Show();

        viewModel.StartEndlessGameCommand.Execute(null);
        window.UpdateLayout();

        var cells = window.GetVisualDescendants()
            .OfType<Button>()
            .Where(button => button.Classes.Contains("cell"))
            .ToArray();

        Assert.Equal(81, cells.Length);
        Assert.Contains(cells, button => button.Classes.Contains("given"));
        Assert.Contains(cells, button => !button.Classes.Contains("given"));
        Assert.DoesNotContain(cells, button => button.Classes.Contains("conflict"));
        Assert.DoesNotContain(cells, button => button.Classes.Contains("wrong"));
    }

    [AvaloniaFact]
    public void MainWindow_marks_conflicting_cell()
    {
        var viewModel = CreateViewModel();
        var window = new MainWindow { DataContext = viewModel };

        window.Show();

        viewModel.StartEndlessGameCommand.Execute(null);
        window.UpdateLayout();

        var game = viewModel.Endless!.Game;
        var cells = viewModel.Endless.Cells;
        int emptyIndex = cells.Select((cell, index) => (cell, index))
            .Where(item => !item.cell.IsGiven)
            .Select(item => item.index)
            .First();
        int row = emptyIndex / 9;

        int conflictingDigit = Enumerable.Range(0, 9)
            .Select(column => game.Session.InitialState[row, column])
            .First(value => value != 0);

        viewModel.Endless.SelectCellCommand.Execute(cells[emptyIndex]);
        viewModel.Endless.EnterDigitCommand.Execute(conflictingDigit.ToString());
        window.UpdateLayout();

        var cellButtons = window.GetVisualDescendants()
            .OfType<Button>()
            .Where(button => button.Classes.Contains("cell"))
            .ToArray();

        Assert.Contains(cellButtons, button => button.Classes.Contains("conflict"));
    }

    private static MainWindowViewModel CreateViewModel()
        => new(
            GameOptions.Default,
            new GameSessionFactory(
                new PuzzleGenerator(
                    new MrvSudokuSolver(),
                    new LogicalDifficultyRater(new MrvSudokuSolver()))),
            seed: 4242);
}
