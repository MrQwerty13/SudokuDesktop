using Sudoku.Application;

namespace Sudoku.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly GameOptions _options;

    public MainWindowViewModel()
        : this(GameOptions.Default)
    {
    }

    public MainWindowViewModel(GameOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    public string ApplicationName => "Sudoku Desktop";

    public string StatusText => "Application shell is running.";

    public string DifficultiesText => string.Join(", ", _options.Difficulties);

    public string ModesText => string.Join(", ", _options.Modes);
}
