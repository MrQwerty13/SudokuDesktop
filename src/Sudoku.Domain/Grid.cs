using System.Diagnostics.CodeAnalysis;

namespace Sudoku.Domain;

/// <summary>
/// Доска судоку 9×9. Значение <see cref="EmptyCell"/> означает пустую ячейку.
/// Экземпляры неизменяемы: любые изменения возвращают новый объект.
/// </summary>
public sealed class Grid : IEquatable<Grid>
{
    /// <summary>
    /// Размер стороны доски.
    /// </summary>
    public const int Size = 9;

    /// <summary>
    /// Количество ячеек на доске.
    /// </summary>
    public const int CellCount = Size * Size;

    /// <summary>
    /// Значение пустой ячейки.
    /// </summary>
    public const int EmptyCell = 0;

    private readonly int[] _cells;

    private Grid(int[] cells)
    {
        _cells = cells;
        IsComplete = IsCompleteCore(cells);
        IsConsistent = IsConsistentCore(cells);
    }

    /// <summary>
    /// Пустая доска без заданных значений.
    /// </summary>
    public static Grid Empty { get; } = new(new int[CellCount]);

    /// <summary>
    /// Признак отсутствия пустых ячеек.
    /// </summary>
    public bool IsComplete { get; }

    /// <summary>
    /// Признак отсутствия противоречий: в каждой строке, столбце и блоке
    /// нет повторяющихся заданных цифр.
    /// </summary>
    public bool IsConsistent { get; }

    /// <summary>
    /// Значение ячейки по линейному индексу (0–80).
    /// </summary>
    /// <param name="index">Линейный индекс ячейки.</param>
    /// <returns>Цифра от 1 до 9 либо <see cref="EmptyCell"/>.</returns>
    public int this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, CellCount);

            return _cells[index];
        }
    }

    /// <summary>
    /// Значение ячейки по координатам.
    /// </summary>
    /// <param name="row">Номер строки (0–8).</param>
    /// <param name="column">Номер столбца (0–8).</param>
    /// <returns>Цифра от 1 до 9 либо <see cref="EmptyCell"/>.</returns>
    public int this[int row, int column]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(row);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Size);
            ArgumentOutOfRangeException.ThrowIfNegative(column);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(column, Size);

            return _cells[(row * Size) + column];
        }
    }

    /// <summary>
    /// Создаёт доску из массива значений ячеек.
    /// </summary>
    /// <param name="cells">81 значение: от 1 до 9 либо <see cref="EmptyCell"/>.</param>
    /// <returns>Новая доска.</returns>
    /// <exception cref="ArgumentException">
    /// Длина массива не равна <see cref="CellCount"/> или содержит значение вне диапазона 0–9.
    /// </exception>
    public static Grid FromCells(ReadOnlySpan<int> cells)
    {
        if (cells.Length != CellCount)
        {
            throw new ArgumentException(
                $"Ожидалось {CellCount} значений, получено {cells.Length}.",
                nameof(cells));
        }

        int[] copy = new int[CellCount];
        for (int index = 0; index < CellCount; index++)
        {
            int value = cells[index];
            if (value < EmptyCell || value > Size)
            {
                throw new ArgumentException(
                    $"Значение ячейки {index} равно {value}, допустимый диапазон — 0–9.",
                    nameof(cells));
            }

            copy[index] = value;
        }

        return new Grid(copy);
    }

    /// <summary>
    /// Разбирает доску из строки из 81 символа: цифры <c>1</c>–<c>9</c> — заданные значения,
    /// <c>0</c> или <c>.</c> — пустые ячейки. Пробельные символы игнорируются.
    /// </summary>
    /// <param name="text">Текстовое представление доски.</param>
    /// <returns>Новая доска.</returns>
    /// <exception cref="ArgumentNullException">Параметр <paramref name="text"/> равен <c>null</c>.</exception>
    /// <exception cref="ArgumentException">
    /// Длина текста без пробелов не равна 81 либо встретился недопустимый символ.
    /// </exception>
    public static Grid FromString(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        Span<int> cells = stackalloc int[CellCount];
        int count = 0;

        foreach (char symbol in text)
        {
            if (char.IsWhiteSpace(symbol))
            {
                continue;
            }

            if (count == CellCount)
            {
                throw new ArgumentException(
                    $"Ожидалось {CellCount} символов, получено больше.",
                    nameof(text));
            }

            cells[count++] = symbol switch
            {
                '0' or '.' => EmptyCell,
                >= '1' and <= '9' => symbol - '0',
                _ => throw new ArgumentException(
                    $"Недопустимый символ '{symbol}' в позиции {count}.",
                    nameof(text)),
            };
        }

        if (count != CellCount)
        {
            throw new ArgumentException(
                $"Ожидалось {CellCount} символов, получено {count}.",
                nameof(text));
        }

        return FromCells(cells);
    }

    /// <summary>
    /// Пытается разобрать доску из строки, не выбрасывая исключение при некорректном вводе.
    /// </summary>
    /// <param name="text">Текстовое представление доски.</param>
    /// <param name="grid">Разобранная доска при успешном разборе, иначе <c>null</c>.</param>
    /// <returns><c>true</c>, если текст разобран успешно.</returns>
    public static bool TryFromString(string? text, [MaybeNullWhen(false)] out Grid grid)
    {
        if (text is null)
        {
            grid = null;
            return false;
        }

        try
        {
            grid = FromString(text);
            return true;
        }
        catch (ArgumentException)
        {
            grid = null;
            return false;
        }
    }

    /// <summary>
    /// Возвращает текстовое представление доски: 81 символ, <c>0</c> — пустая ячейка.
    /// </summary>
    /// <returns>Текстовое представление доски.</returns>
    public override string ToString()
    {
        char[] text = new char[CellCount];
        for (int index = 0; index < CellCount; index++)
        {
            text[index] = (char)('0' + _cells[index]);
        }

        return new string(text);
    }

    /// <inheritdoc />
    public bool Equals(Grid? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        for (int index = 0; index < CellCount; index++)
        {
            if (_cells[index] != other._cells[index])
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as Grid);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (int cell in _cells)
        {
            hash.Add(cell);
        }

        return hash.ToHashCode();
    }

    private static bool IsCompleteCore(int[] cells)
    {
        foreach (int value in cells)
        {
            if (value == EmptyCell)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsConsistentCore(int[] cells)
    {
        var rowMasks = new int[Size];
        var columnMasks = new int[Size];
        var boxMasks = new int[Size];

        for (int index = 0; index < CellCount; index++)
        {
            int value = cells[index];
            if (value == EmptyCell)
            {
                continue;
            }

            int row = index / Size;
            int column = index % Size;
            int box = ((row / 3) * 3) + (column / 3);
            int bit = 1 << (value - 1);

            if (((rowMasks[row] | columnMasks[column] | boxMasks[box]) & bit) != 0)
            {
                return false;
            }

            rowMasks[row] |= bit;
            columnMasks[column] |= bit;
            boxMasks[box] |= bit;
        }

        return true;
    }
}
