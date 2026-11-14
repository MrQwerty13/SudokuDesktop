using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sudoku.Application;
using Sudoku.Domain;

namespace Sudoku.ViewModels;

/// <summary>
/// Главное окно: меню выбора режима и сложности, а также экран активного захода.
/// </summary>
/// <remarks>
/// Окно показывает выбранную сложность и состояние игры явно: строка состояния
/// сообщает режим, сложность и прогресс захода.
/// </remarks>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly GameOptions _options;
    private readonly IGameSessionFactory _factory;
    private int _seedOffset;

    /// <summary>
    /// Создаёт модель главного окна с настройками по умолчанию.
    /// </summary>
    public MainWindowViewModel()
        : this(GameOptions.Default, Composition.CreateGameSessionFactory())
    {
    }

    /// <summary>
    /// Создаёт модель главного окна.
    /// </summary>
    /// <param name="options">Игровые опции.</param>
    /// <param name="factory">Фабрика игровых сессий.</param>
    /// <param name="seed">Seed первого захода; последующие заходы используют seed + 1 и далее.</param>
    /// <exception cref="ArgumentNullException">
    /// Параметр <paramref name="options"/> или <paramref name="factory"/> равен <c>null</c>.
    /// </exception>
    public MainWindowViewModel(GameOptions options, IGameSessionFactory factory, int? seed = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(factory);

        _options = options;
        _factory = factory;
        Seed = seed ?? Random.Shared.Next(1, int.MaxValue);
        SelectedDifficulty = options.Difficulties.Count > 0 ? options.Difficulties[0] : Difficulty.Easy;
    }

    /// <summary>
    /// Название приложения.
    /// </summary>
    public string ApplicationName => "Sudoku Desktop";

    /// <summary>
    /// Название активного режима. Доступен только Endless: Time Attack появится
    /// отдельным режимом со своим сценарием.
    /// </summary>
    public string ModeText => "Режим: Endless";

    /// <summary>
    /// Доступные уровни сложности.
    /// </summary>
    public IReadOnlyList<Difficulty> Difficulties => _options.Difficulties;

    /// <summary>
    /// Seed захода: позволяет воспроизвести ту же последовательность головоломок.
    /// </summary>
    public int Seed { get; }

    /// <summary>
    /// Выбранная сложность.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    private Difficulty _selectedDifficulty;

    /// <summary>
    /// Экран активного захода; <c>null</c>, если заход не начат.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(HasGame))]
    private EndlessGameViewModel? _endless;

    /// <summary>
    /// Признак того, что заход идёт.
    /// </summary>
    public bool HasGame => Endless is not null;

    /// <summary>
    /// Состояние игры: выбранная сложность и прогресс захода.
    /// </summary>
    public string StatusText => Endless is null
        ? $"Выбрана сложность: {SelectedDifficulty} · Seed: {Seed}"
        : $"{Endless.ModeText} · {Endless.DifficultyText} · {Endless.StatusText}";

    /// <summary>
    /// Начинает новый заход режима Endless с выбранной сложностью.
    /// </summary>
    [RelayCommand]
    private void StartEndlessGame()
    {
        var game = new EndlessGame(_factory, SelectedDifficulty, Seed + _seedOffset);
        _seedOffset++;

        Endless = new EndlessGameViewModel(game, () => Endless = null);
    }

    /// <summary>
    /// Завершает текущий заход и возвращает игрока в меню.
    /// </summary>
    [RelayCommand]
    private void OpenMenu() => Endless = null;
}
