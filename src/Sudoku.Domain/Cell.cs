namespace Sudoku.Domain;

/// <summary>
/// Снимок состояния одной ячейки игрового поля.
/// </summary>
/// <remarks>
/// Тип неизменяемый: он описывает ячейку в конкретный момент времени и используется
/// слоем представления для отображения. Создаётся только <see cref="GameSession"/>.
/// </remarks>
public sealed class Cell
{
    internal Cell(
        CellPosition position,
        int value,
        bool isGiven,
        bool isSelected,
        bool hasConflict,
        bool isCorrect)
    {
        Position = position;
        Value = value;
        IsGiven = isGiven;
        IsSelected = isSelected;
        HasConflict = hasConflict;
        IsCorrect = isCorrect;
    }

    /// <summary>
    /// Позиция ячейки на доске.
    /// </summary>
    public CellPosition Position { get; }

    /// <summary>
    /// Значение ячейки: от 1 до 9 либо <see cref="Grid.EmptyCell"/> для пустой ячейки.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Признак пустой ячейки.
    /// </summary>
    public bool IsEmpty => Value == Grid.EmptyCell;

    /// <summary>
    /// Признак исходной (заданной) ячейки: её значение менять нельзя.
    /// </summary>
    public bool IsGiven { get; }

    /// <summary>
    /// Признак выбранной ячейки.
    /// </summary>
    public bool IsSelected { get; }

    /// <summary>
    /// Признак конфликта: значение ячейки повторяется в строке, столбце или блоке.
    /// </summary>
    public bool HasConflict { get; }

    /// <summary>
    /// Признак корректности значения: значение совпадает с подтверждённым решением.
    /// Для пустой ячейки всегда <c>true</c> — неверного значения нет.
    /// </summary>
    public bool IsCorrect { get; }

    /// <summary>
    /// Признак наличия значения, введённого игроком.
    /// </summary>
    public bool IsUserValue => !IsEmpty && !IsGiven;

    /// <inheritdoc />
    public override string ToString()
        => $"{Position}: значение={Value}, исходная={IsGiven}, конфликт={HasConflict}";
}
