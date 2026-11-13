using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(Sudoku.Tests.App.TestAppBuilder))]

namespace Sudoku.Tests.App;

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<Sudoku.App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
