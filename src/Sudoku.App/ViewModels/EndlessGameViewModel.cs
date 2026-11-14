using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sudoku.Application;
using Sudoku.Domain;

namespace Sudoku.ViewModels;

/// <summary>
/// Экран режима Endless: текущая головоломка, состояние захода и действия игрока.
/// </summary>
/// <remarks>
/// Экран не содержит игровой логики: она живёт в <see cref="EndlessGame"/>
/// (сценарий захода) и <see cref="GameSession"/> (механика). Здесь — только отображение
/// состояния и команды.
/// </remarks>
public partial class EndlessGameViewModel : ViewModelBase
{
    private readonly EndlessGame _game;
    private readonly Action? _onExit;
    private readonly ObservableCollection<CellViewModel> _cells = [];

    /// <summary>
    /// Создаёт экран режима Endless.
    /// </summary>
    /// <param name="game">Заход режима Endless.</param>
    /// <param name="onExit">Обработчик завершения захода (возврат в меню).</param>
    /// <exception cref="ArgumentNullException">Параметр <paramref name="game"/> равен <c>null</c>.</exception>
    public EndlessGameViewModel(EndlessGame game, Action? onExit = null)
    {
        ArgumentNullException.ThrowIfNull(game);

        _game = game;
        _onExit = onExit;

        for (int index = 0; index < Grid.CellCount; index++)
        {
            _cells.Add(new CellViewModel(index, SelectCell));
        }

        Refresh();
    }

    /// <summary>
    /// Заход режима Endless, отображаемый экраном.
    /// </summary>
    public EndlessGame Game => _game;

    /// <summary>
    /// Ячейки игрового поля.
    /// </summary>
    public ObservableCollection<CellViewModel> Cells => _cells;

    /// <summary>
    /// Название режима.
    /// </summary>
    public string ModeText => $"Режим: {_game.Mode}";

    /// <summary>
    /// Выбранная сложность — отображается явно.
    /// </summary>
    public string DifficultyText => $"Сложность: {_game.Difficulty}";

    /// <summary>
    /// Состояние захода: номер головоломки, число решённых, число подсказок.
    /// </summary>
    public string StatusText =>
        $"Головоломка {_game.PuzzleNumber} · Решено: {_game.SolvedPuzzleCount} · Подсказки: {_game.HintCount}";

    /// <summary>
    /// Состояние текущей головоломки.
    /// </summary>
    public string BoardStateText => _game.IsCurrentPuzzleSolved
        ? "Головоломка решена"
        : "Идёт решение";

    /// <summary>
    /// Условие по времени: в режиме Endless решение не ограничено.
    /// </summary>
    public string TimerText => "Время не ограничено";

    /// <summary>
    /// Признак того, что текущая головоломка решена.
    /// </summary>
    public bool IsCurrentPuzzleSolved => _game.IsCurrentPuzzleSolved;

    /// <summary>
    /// Выбирает ячейку.
    /// </summary>
    /// <param name="cell">Ячейка, выбранная игроком.</param>
    [RelayCommand]
    private void SelectCell(CellViewModel cell)
    {
        ArgumentNullException.ThrowIfNull(cell);

        _game.SelectCell(CellPosition.FromIndex(cell.Index));
        Refresh();
    }

    /// <summary>
    /// Вводит цифру в выбранную ячейку.
    /// </summary>
    /// <param name="digit">Цифра (1–9).</param>
    [RelayCommand]
    private void EnterDigit(string digit)
    {
        if (!int.TryParse(digit, out int value))
        {
            return;
        }

        _game.EnterValue(value);
        Refresh();
    }

    /// <summary>
    /// Стирает значение в выбранной ячейке.
    /// </summary>
    [RelayCommand]
    private void Erase()
    {
        _game.EraseValue();
        Refresh();
    }

    /// <summary>
    /// Запрашивает подсказку для выбранной ячейки.
    /// </summary>
    [RelayCommand]
    private void Hint()
    {
        _game.RequestHint();
        Refresh();
    }

    /// <summary>
    /// Завершает текущую головоломку и начинает следующую той же сложности.
    /// </summary>
    [RelayCommand]
    private void NewPuzzle()
    {
        _game.StartNextPuzzle();
        Refresh();
    }

    /// <summary>
    /// Завершает заход и возвращает игрока в меню.
    /// </summary>
    [RelayCommand]
    private void Exit() => _onExit?.Invoke();

    private void Refresh()
    {
        IReadOnlyList<Cell> cells = _game.Cells;

        for (int index = 0; index < cells.Count; index++)
        {
            Cell cell = cells[index];
            CellViewModel viewModel = _cells[index];

            viewModel.Text = cell.IsEmpty ? string.Empty : cell.Value.ToString();
            viewModel.IsGiven = cell.IsGiven;
            viewModel.IsSelected = cell.IsSelected;
            viewModel.HasConflict = cell.HasConflict;
            viewModel.IsCorrect = cell.IsCorrect;
            viewModel.IsWrong = !cell.IsCorrect;
        }

        OnPropertyChanged(nameof(ModeText));
        OnPropertyChanged(nameof(DifficultyText));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(BoardStateText));
        OnPropertyChanged(nameof(TimerText));
        OnPropertyChanged(nameof(IsCurrentPuzzleSolved));
    }
}
