using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
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
    public void MainWindow_renders_domain_options()
    {
        var window = new MainWindow
        {
            DataContext = new MainWindowViewModel(),
        };

        window.Show();

        var texts = window.GetVisualDescendants()
            .OfType<TextBlock>()
            .Select(text => text.Text)
            .ToArray();

        Assert.Contains("Sudoku Desktop", texts);
        Assert.Contains("Difficulties: Easy, Normal, Hard", texts);
        Assert.Contains("Modes: Endless, TimeAttack", texts);
    }
}
