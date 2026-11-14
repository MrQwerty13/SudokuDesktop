namespace Sudoku.Domain;

/// <summary>
/// Позиция ячейки на доске 9×9.
/// </summary>
/// <remarks>
/// Структура неизменяема и проверяет диапазон при создании, поэтому позиция
/// всегда указывает на существующую ячейку.
/// </remarks>
public readonly struct CellPosition : IEquatable<CellPosition>
{
    /// <summary>
    /// Создаёт позицию ячейки.
    /// </summary>
    /// <param name="row">Номер строки (0–8).</param>
    /// <param name="column">Номер столбца (0–8).</param>
    /// <exception cref="ArgumentOutOfRangeException">Номер строки или столбца вне диапазона 0–8.</exception>
    public CellPosition(int row, int column)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(row);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Grid.Size);
        ArgumentOutOfRangeException.ThrowIfNegative(column);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(column, Grid.Size);

        Row = row;
        Column = column;
    }

    /// <summary>
    /// Номер строки (0–8).
    /// </summary>
    public int Row { get; }

    /// <summary>
    /// Номер столбца (0–8).
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// Линейный индекс ячейки на доске (0–80).
    /// </summary>
    public int Index => (Row * Grid.Size) + Column;

    /// <summary>
    /// Создаёт позицию по линейному индексу.
    /// </summary>
    /// <param name="index">Линейный индекс ячейки (0–80).</param>
    /// <returns>Позиция ячейки.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Индекс вне диапазона 0–80.</exception>
    public static CellPosition FromIndex(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Grid.CellCount);

        return new CellPosition(index / Grid.Size, index % Grid.Size);
    }

    /// <inheritdoc />
    public bool Equals(CellPosition other) => Row == other.Row && Column == other.Column;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is CellPosition other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Row, Column);

    /// <inheritdoc />
    public override string ToString() => $"({Row}, {Column})";

    /// <summary>
    /// Сравнивает две позиции на равенство.
    /// </summary>
    /// <param name="left">Первая позиция.</param>
    /// <param name="right">Вторая позиция.</param>
    /// <returns><c>true</c>, если позиции совпадают.</returns>
    public static bool operator ==(CellPosition left, CellPosition right) => left.Equals(right);

    /// <summary>
    /// Сравнивает две позиции на неравенство.
    /// </summary>
    /// <param name="left">Первая позиция.</param>
    /// <param name="right">Вторая позиция.</param>
    /// <returns><c>true</c>, если позиции различаются.</returns>
    public static bool operator !=(CellPosition left, CellPosition right) => !left.Equals(right);
}
