using Sudoku.Domain;

namespace Sudoku.Application;

/// <summary>
/// Логический решатель: применяет приёмы уровнем не выше заданного.
/// </summary>
/// <remarks>
/// Решатель не перебирает значения: он делает только корректные выводы (naked single,
/// hidden single, pointing pair, claiming pair, naked pair, hidden pair).
/// Состояние: значения ячеек и маски кандидатов; приём, применённый к ячейке,
/// всегда корректен, поэтому найденное заполнение является решением.
/// </remarks>
internal sealed class LogicalSolver
{
    private const int AllDigitsMask = 0x1FF;

    private static readonly int[] BoxOfCell = CreateBoxOfCell();
    private static readonly int[][] RowUnits = CreateRowUnits();
    private static readonly int[][] ColumnUnits = CreateColumnUnits();
    private static readonly int[][] BoxUnits = CreateBoxUnits();
    private static readonly int[][] AllUnits = [.. RowUnits, .. ColumnUnits, .. BoxUnits];

    private readonly int[] _values = new int[Grid.CellCount];
    private readonly int[] _candidates = new int[Grid.CellCount];
    private readonly int[] _rowMasks = new int[Grid.Size];
    private readonly int[] _columnMasks = new int[Grid.Size];
    private readonly int[] _boxMasks = new int[Grid.Size];

    private bool _contradiction;

    public LogicalSolver(Grid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);

        for (int index = 0; index < Grid.CellCount; index++)
        {
            int value = grid[index];
            if (value == Grid.EmptyCell)
            {
                continue;
            }

            int row = index / Grid.Size;
            int column = index % Grid.Size;
            int box = BoxOfCell[index];
            int bit = 1 << (value - 1);

            if (((_rowMasks[row] | _columnMasks[column] | _boxMasks[box]) & bit) != 0)
            {
                _contradiction = true;
                continue;
            }

            _values[index] = value;
            _rowMasks[row] |= bit;
            _columnMasks[column] |= bit;
            _boxMasks[box] |= bit;
        }

        for (int index = 0; index < Grid.CellCount; index++)
        {
            if (_values[index] != Grid.EmptyCell)
            {
                continue;
            }

            int row = index / Grid.Size;
            int column = index % Grid.Size;
            int box = BoxOfCell[index];

            _candidates[index] = AllDigitsMask
                & ~(_rowMasks[row] | _columnMasks[column] | _boxMasks[box]);
        }
    }

    /// <summary>
    /// Пытается решить головоломку, используя только приёмы уровнем не выше заданного.
    /// </summary>
    /// <param name="maxTechniqueLevel">Максимальный уровень приёма: 1–6.</param>
    /// <returns><c>true</c>, если головоломка решена логическими приёмами.</returns>
    public bool Solve(int maxTechniqueLevel)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxTechniqueLevel, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxTechniqueLevel, DifficultyBands.MaxTechniqueLevel);

        return SolveUpTo(maxTechniqueLevel);
    }

    /// <summary>
    /// Продолжает решение, разрешая приёмы уровнем не выше заданного.
    /// </summary>
    /// <remarks>
    /// Состояние решателя сохраняется между вызовами: решение по уровням идёт
    /// «нарастающим» способом, поэтому не приходится решать головоломку заново
    /// для каждого уровня. Итерация хаотическая (chaotic iteration): приёмы
    /// применяются по кругу до тех пор, пока не перестанет быть прогресса,
    /// поэтому достигается наибольший неподвижный пункт допустимых приёмов.
    /// </remarks>
    /// <param name="maxTechniqueLevel">Максимальный уровень приёма: 1–6.</param>
    /// <returns><c>true</c>, если головоломка решена логическими приёмами.</returns>
    public bool SolveUpTo(int maxTechniqueLevel)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxTechniqueLevel, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxTechniqueLevel, DifficultyBands.MaxTechniqueLevel);

        int mask = 0;
        for (int level = 1; level <= maxTechniqueLevel; level++)
        {
            mask |= 1 << (level - 1);
        }

        return SolveMask(mask);
    }

    /// <summary>
    /// Пытается решить головоломку приёмами не выше заданного уровня
    /// и возвращает полученное заполнение.
    /// </summary>
    /// <param name="maxTechniqueLevel">Максимальный уровень приёма: 1–6.</param>
    /// <param name="solution">
    /// Заполненная доска, если решение найдено; иначе <c>null</c>.
    /// </param>
    /// <returns><c>true</c>, если головоломка решена логическими приёмами.</returns>
    public bool TrySolve(int maxTechniqueLevel, out Grid? solution)
    {
        if (!SolveUpTo(maxTechniqueLevel))
        {
            solution = null;
            return false;
        }

        solution = Grid.FromCells(_values);
        return true;
    }

    private bool SolveMask(int techniqueMask)
    {
        while (true)
        {
            if (_contradiction)
            {
                return false;
            }

            bool progress = false;

            if ((techniqueMask & 1) != 0 && ApplyNakedSingles())
            {
                progress = true;
            }

            if ((techniqueMask & 2) != 0 && ApplyHiddenSingles())
            {
                progress = true;
            }

            if ((techniqueMask & 4) != 0 && ApplyPointingPairs())
            {
                progress = true;
            }

            if ((techniqueMask & 8) != 0 && ApplyClaimingPairs())
            {
                progress = true;
            }

            if ((techniqueMask & 16) != 0 && ApplyNakedPairs())
            {
                progress = true;
            }

            if ((techniqueMask & 32) != 0 && ApplyHiddenPairs())
            {
                progress = true;
            }

            if (_contradiction)
            {
                return false;
            }

            if (IsComplete)
            {
                return true;
            }

            if (!progress)
            {
                return false;
            }
        }
    }

    private bool IsComplete
    {
        get
        {
            for (int index = 0; index < Grid.CellCount; index++)
            {
                if (_values[index] == Grid.EmptyCell)
                {
                    return false;
                }
            }

            return true;
        }
    }

    private bool ApplyNakedSingles()
    {
        bool changed = false;

        for (int index = 0; index < Grid.CellCount; index++)
        {
            if (_values[index] != Grid.EmptyCell)
            {
                continue;
            }

            int candidates = _candidates[index];
            if (candidates == 0)
            {
                _contradiction = true;
                return changed;
            }

            if (BitCount(candidates) == 1)
            {
                Assign(index, BitToDigit(candidates));
                changed = true;
            }
        }

        return changed;
    }

    private bool ApplyHiddenSingles()
    {
        bool changed = false;

        foreach (int[] unit in AllUnits)
        {
            for (int digit = 1; digit <= Grid.Size; digit++)
            {
                if (IsPlacedIn(unit, digit))
                {
                    continue;
                }

                int cells = 0;
                foreach (int cell in unit)
                {
                    if (_values[cell] == Grid.EmptyCell && (_candidates[cell] & (1 << (digit - 1))) != 0)
                    {
                        cells++;
                    }
                }

                if (cells == 0)
                {
                    _contradiction = true;
                    return changed;
                }

                if (cells == 1)
                {
                    Assign(FindSingleCandidate(unit, digit), digit);
                    changed = true;
                }
            }
        }

        return changed;
    }

    private bool ApplyPointingPairs()
    {
        bool changed = false;

        for (int boxIndex = 0; boxIndex < Grid.Size; boxIndex++)
        {
            int[] box = BoxUnits[boxIndex];

            for (int digit = 1; digit <= Grid.Size; digit++)
            {
                if (IsPlacedIn(box, digit))
                {
                    continue;
                }

                int bit = 1 << (digit - 1);
                int mask = 0;
                int row = -1;
                bool sameRow = true;

                for (int position = 0; position < Grid.Size; position++)
                {
                    int cell = box[position];
                    if (_values[cell] != Grid.EmptyCell || (_candidates[cell] & bit) == 0)
                    {
                        continue;
                    }

                    mask |= 1 << position;

                    int cellRow = cell / Grid.Size;
                    if (row < 0)
                    {
                        row = cellRow;
                    }
                    else if (row != cellRow)
                    {
                        sameRow = false;
                    }
                }

                if (mask == 0)
                {
                    _contradiction = true;
                    return changed;
                }

                if (!sameRow || BitCount(mask) < 2)
                {
                    continue;
                }

                for (int column = 0; column < Grid.Size; column++)
                {
                    int cell = (row * Grid.Size) + column;
                    if (BoxOfCell[cell] == boxIndex || _values[cell] != Grid.EmptyCell)
                    {
                        continue;
                    }

                    if ((_candidates[cell] & bit) == 0)
                    {
                        continue;
                    }

                    _candidates[cell] &= ~bit;
                    changed = true;
                }
            }
        }

        return changed;
    }

    private bool ApplyClaimingPairs()
    {
        bool changed = false;

        for (int row = 0; row < Grid.Size; row++)
        {
            int[] unit = RowUnits[row];

            for (int digit = 1; digit <= Grid.Size; digit++)
            {
                if (IsPlacedIn(unit, digit))
                {
                    continue;
                }

                int bit = 1 << (digit - 1);
                int mask = 0;
                int box = -1;
                bool sameBox = true;

                for (int position = 0; position < Grid.Size; position++)
                {
                    int cell = unit[position];
                    if (_values[cell] != Grid.EmptyCell || (_candidates[cell] & bit) == 0)
                    {
                        continue;
                    }

                    mask |= 1 << position;

                    int cellBox = BoxOfCell[cell];
                    if (box < 0)
                    {
                        box = cellBox;
                    }
                    else if (box != cellBox)
                    {
                        sameBox = false;
                    }
                }

                if (mask == 0)
                {
                    _contradiction = true;
                    return changed;
                }

                if (!sameBox || BitCount(mask) < 2)
                {
                    continue;
                }

                foreach (int cell in BoxUnits[box])
                {
                    if (cell / Grid.Size == row || _values[cell] != Grid.EmptyCell)
                    {
                        continue;
                    }

                    if ((_candidates[cell] & bit) == 0)
                    {
                        continue;
                    }

                    _candidates[cell] &= ~bit;
                    changed = true;
                }
            }
        }

        return changed;
    }

    private bool ApplyNakedPairs()
    {
        bool changed = false;

        foreach (int[] unit in AllUnits)
        {
            foreach (int first in unit)
            {
                if (_values[first] != Grid.EmptyCell)
                {
                    continue;
                }

                int mask = _candidates[first];
                if (BitCount(mask) != 2)
                {
                    continue;
                }

                int matches = 0;
                int pairFirst = -1;
                int pairSecond = -1;
                foreach (int cell in unit)
                {
                    if (_values[cell] != Grid.EmptyCell || _candidates[cell] != mask)
                    {
                        continue;
                    }

                    matches++;
                    if (matches == 1)
                    {
                        pairFirst = cell;
                    }
                    else if (matches == 2)
                    {
                        pairSecond = cell;
                    }
                }

                if (matches != 2)
                {
                    continue;
                }

                foreach (int cell in unit)
                {
                    if (_values[cell] != Grid.EmptyCell || cell == pairFirst || cell == pairSecond)
                    {
                        continue;
                    }

                    int updated = _candidates[cell] & ~mask;
                    if (updated == _candidates[cell])
                    {
                        continue;
                    }

                    _candidates[cell] = updated;
                    changed = true;
                }
            }
        }

        return changed;
    }

    private bool ApplyHiddenPairs()
    {
        Span<int> digitCells = stackalloc int[Grid.Size];

        foreach (int[] unit in AllUnits)
        {
            for (int digit = 1; digit <= Grid.Size; digit++)
            {
                int mask = 0;
                for (int position = 0; position < Grid.Size; position++)
                {
                    int cell = unit[position];
                    if (_values[cell] == Grid.EmptyCell && (_candidates[cell] & (1 << (digit - 1))) != 0)
                    {
                        mask |= 1 << position;
                    }
                }

                digitCells[digit - 1] = mask;
            }

            for (int first = 1; first <= Grid.Size; first++)
            {
                if (IsPlacedIn(unit, first))
                {
                    continue;
                }

                for (int second = first + 1; second <= Grid.Size; second++)
                {
                    if (IsPlacedIn(unit, second))
                    {
                        continue;
                    }

                    int union = digitCells[first - 1] | digitCells[second - 1];
                    if (BitCount(union) != 2)
                    {
                        continue;
                    }

                    int allowed = (1 << (first - 1)) | (1 << (second - 1));
                    bool changed = false;

                    for (int position = 0; position < Grid.Size; position++)
                    {
                        if ((union & (1 << position)) == 0)
                        {
                            continue;
                        }

                        int cell = unit[position];
                        if (_values[cell] != Grid.EmptyCell)
                        {
                            continue;
                        }

                        int updated = _candidates[cell] & allowed;
                        if (updated == _candidates[cell])
                        {
                            continue;
                        }

                        _candidates[cell] = updated;
                        changed = true;
                    }

                    if (changed)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool IsPlacedIn(int[] unit, int digit)
    {
        foreach (int cell in unit)
        {
            if (_values[cell] == digit)
            {
                return true;
            }
        }

        return false;
    }

    private int FindSingleCandidate(int[] unit, int digit)
    {
        int bit = 1 << (digit - 1);

        foreach (int cell in unit)
        {
            if (_values[cell] == Grid.EmptyCell && (_candidates[cell] & bit) != 0)
            {
                return cell;
            }
        }

        return -1;
    }

    private void Assign(int cell, int digit)
    {
        int bit = 1 << (digit - 1);
        int row = cell / Grid.Size;
        int column = cell % Grid.Size;
        int box = BoxOfCell[cell];

        _values[cell] = digit;
        _candidates[cell] = 0;
        _rowMasks[row] |= bit;
        _columnMasks[column] |= bit;
        _boxMasks[box] |= bit;

        for (int index = 0; index < Grid.Size; index++)
        {
            RemoveCandidate((row * Grid.Size) + index, bit);
            RemoveCandidate((index * Grid.Size) + column, bit);
        }

        foreach (int peer in BoxUnits[box])
        {
            RemoveCandidate(peer, bit);
        }
    }

    private void RemoveCandidate(int cell, int bit)
    {
        if (_values[cell] != Grid.EmptyCell)
        {
            return;
        }

        _candidates[cell] &= ~bit;
    }

    private static int BitCount(int mask)
    {
        int count = 0;
        while (mask != 0)
        {
            mask &= mask - 1;
            count++;
        }

        return count;
    }

    private static int BitToDigit(int mask)
    {
        for (int digit = 1; digit <= Grid.Size; digit++)
        {
            if ((mask & (1 << (digit - 1))) != 0)
            {
                return digit;
            }
        }

        throw new InvalidOperationException("Маска кандидатов не содержит ни одной цифры.");
    }

    private static int[] CreateBoxOfCell()
    {
        var boxes = new int[Grid.CellCount];
        for (int index = 0; index < Grid.CellCount; index++)
        {
            int row = index / Grid.Size;
            int column = index % Grid.Size;
            boxes[index] = ((row / 3) * 3) + (column / 3);
        }

        return boxes;
    }

    private static int[][] CreateRowUnits()
    {
        var units = new int[Grid.Size][];
        for (int row = 0; row < Grid.Size; row++)
        {
            var cells = new int[Grid.Size];
            for (int column = 0; column < Grid.Size; column++)
            {
                cells[column] = (row * Grid.Size) + column;
            }

            units[row] = cells;
        }

        return units;
    }

    private static int[][] CreateColumnUnits()
    {
        var units = new int[Grid.Size][];
        for (int column = 0; column < Grid.Size; column++)
        {
            var cells = new int[Grid.Size];
            for (int row = 0; row < Grid.Size; row++)
            {
                cells[row] = (row * Grid.Size) + column;
            }

            units[column] = cells;
        }

        return units;
    }

    private static int[][] CreateBoxUnits()
    {
        var units = new int[Grid.Size][];
        for (int box = 0; box < Grid.Size; box++)
        {
            var cells = new int[Grid.Size];
            int index = 0;
            int startRow = (box / 3) * 3;
            int startColumn = (box % 3) * 3;

            for (int row = startRow; row < startRow + 3; row++)
            {
                for (int column = startColumn; column < startColumn + 3; column++)
                {
                    cells[index++] = (row * Grid.Size) + column;
                }
            }

            units[box] = cells;
        }

        return units;
    }
}
