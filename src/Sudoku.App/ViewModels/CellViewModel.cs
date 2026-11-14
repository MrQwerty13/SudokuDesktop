using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Sudoku.ViewModels;

/// <summary>
/// Представление одной ячейки игрового поля.
/// </summary>
public partial class CellViewModel : ViewModelBase
{
    private readonly Action<CellViewModel> _onSelected;

    /// <summary>
    /// Создаёт представление ячейки.
    /// </summary>
    /// <param name="index">Линейный индекс ячейки (0–80).</param>
    /// <param name="onSelected">Обработчик выбора ячейки игроком.</param>
    public CellViewModel(int index, Action<CellViewModel> onSelected)
    {
        ArgumentNullException.ThrowIfNull(onSelected);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, 81);

        _onSelected = onSelected;
        Index = index;
    }

    /// <summary>
    /// Линейный индекс ячейки (0–80).
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Номер строки ячейки (0–8).
    /// </summary>
    public int Row => Index / 9;

    /// <summary>
    /// Номер столбца ячейки (0–8).
    /// </summary>
    public int Column => Index % 9;

    /// <summary>
    /// Отображаемое значение: цифра или пустая строка.
    /// </summary>
    [ObservableProperty]
    private string _text = string.Empty;

    /// <summary>
    /// Признак исходной (заданной) ячейки.
    /// </summary>
    [ObservableProperty]
    private bool _isGiven;

    /// <summary>
    /// Признак выбранной ячейки.
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Признак конфликта в ячейке.
    /// </summary>
    [ObservableProperty]
    private bool _hasConflict;

    /// <summary>
    /// Признак того, что значение ячейки совпадает с подтверждённым решением.
    /// </summary>
    [ObservableProperty]
    private bool _isCorrect = true;

    /// <summary>
    /// Признак того, что в ячейке стоит значение, противоречащее решению.
    /// </summary>
    [ObservableProperty]
    private bool _isWrong;

    /// <summary>
    /// Выбирает ячейку.
    /// </summary>
    [RelayCommand]
    private void Select() => _onSelected(this);
}
