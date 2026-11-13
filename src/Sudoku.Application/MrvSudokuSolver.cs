using Sudoku.Domain;

namespace Sudoku.Application;

/// <summary>
/// Решатель головоломок судоку на основе битовых масок, распространения ограничений
/// и перебора с выбором ячейки по правилу минимального числа кандидатов (MRV).
/// </summary>
/// <remarks>
/// Алгоритм детерминирован: при одинаковом входе порядок перебора ячеек и цифр
/// не меняется, поэтому результат воспроизводим.
/// </remarks>
public sealed class MrvSudokuSolver : ISudokuSolver
{
    /// <inheritdoc />
    public SolveResult Solve(Grid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);

        List<Grid> solutions = [];
        Search(grid, 2, solutions);

        return SolveResult.Create(solutions);
    }

    /// <inheritdoc />
    public int CountSolutions(Grid grid, int solutionLimit)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentOutOfRangeException.ThrowIfLessThan(solutionLimit, 1);

        List<Grid> solutions = [];
        Search(grid, solutionLimit, solutions);

        return solutions.Count;
    }

    private static void Search(Grid grid, int solutionLimit, List<Grid> solutions)
    {
        var state = new SearchState(grid);
        if (state.HasConflicts)
        {
            return;
        }

        state.Run(solutionLimit, solutions);
    }

    private sealed class SearchState
    {
        private const int AllDigitsMask = 0x1FF;

        private static readonly int[] BoxOfCell = CreateBoxOfCell();
        private static readonly int[][] Units = CreateUnits();

        private readonly int[] _values = new int[Grid.CellCount];
        private readonly int[] _rowMasks = new int[Grid.Size];
        private readonly int[] _columnMasks = new int[Grid.Size];
        private readonly int[] _boxMasks = new int[Grid.Size];
        private readonly List<int> _trail = [];

        private bool _contradiction;

        public SearchState(Grid grid)
        {
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
                    HasConflicts = true;
                    return;
                }

                _values[index] = value;
                _rowMasks[row] |= bit;
                _columnMasks[column] |= bit;
                _boxMasks[box] |= bit;
            }
        }

        public bool HasConflicts { get; }

        public void Run(int solutionLimit, List<Grid> solutions)
        {
            Search(solutionLimit, solutions);
        }

        private void Search(int solutionLimit, List<Grid> solutions)
        {
            if (!Propagate())
            {
                return;
            }

            int cell = SelectCell();
            if (_contradiction)
            {
                return;
            }

            if (cell < 0)
            {
                solutions.Add(Grid.FromCells(_values));
                return;
            }

            int mark = _trail.Count;
            int candidates = Candidates(cell);

            for (int digit = 1; digit <= Grid.Size; digit++)
            {
                if ((candidates & (1 << (digit - 1))) == 0)
                {
                    continue;
                }

                Assign(cell, digit);
                Search(solutionLimit, solutions);
                UndoTo(mark);

                if (solutions.Count >= solutionLimit)
                {
                    return;
                }
            }
        }

        private bool Propagate()
        {
            while (!_contradiction)
            {
                bool changed = false;

                changed |= PlaceNakedSingles();

                foreach (int[] unit in Units)
                {
                    changed |= PlaceHiddenSingles(unit);
                }

                if (_contradiction)
                {
                    return false;
                }

                if (!changed)
                {
                    return true;
                }
            }

            return false;
        }

        private bool PlaceNakedSingles()
        {
            bool changed = false;

            for (int index = 0; index < Grid.CellCount; index++)
            {
                if (_values[index] != Grid.EmptyCell)
                {
                    continue;
                }

                int candidates = Candidates(index);
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

        private bool PlaceHiddenSingles(int[] unit)
        {
            bool changed = false;

            for (int digit = 1; digit <= Grid.Size; digit++)
            {
                int bit = 1 << (digit - 1);
                bool alreadyPlaced = false;

                foreach (int cell in unit)
                {
                    if (_values[cell] == digit)
                    {
                        alreadyPlaced = true;
                        break;
                    }
                }

                if (alreadyPlaced)
                {
                    continue;
                }

                int count = 0;
                int target = -1;

                foreach (int cell in unit)
                {
                    if (_values[cell] != Grid.EmptyCell)
                    {
                        continue;
                    }

                    if ((Candidates(cell) & bit) != 0)
                    {
                        count++;
                        target = cell;

                        if (count > 1)
                        {
                            break;
                        }
                    }
                }

                if (count == 0)
                {
                    _contradiction = true;
                    return changed;
                }

                if (count == 1)
                {
                    Assign(target, digit);
                    changed = true;
                }
            }

            return changed;
        }

        private int SelectCell()
        {
            int best = -1;
            int bestCount = int.MaxValue;

            for (int index = 0; index < Grid.CellCount; index++)
            {
                if (_values[index] != Grid.EmptyCell)
                {
                    continue;
                }

                int count = BitCount(Candidates(index));
                if (count == 0)
                {
                    _contradiction = true;
                    return -1;
                }

                if (count < bestCount)
                {
                    bestCount = count;
                    best = index;

                    if (count == 2)
                    {
                        break;
                    }
                }
            }

            return best;
        }

        private int Candidates(int cell)
        {
            int row = cell / Grid.Size;
            int column = cell % Grid.Size;
            int box = BoxOfCell[cell];

            return AllDigitsMask & ~(_rowMasks[row] | _columnMasks[column] | _boxMasks[box]);
        }

        private void Assign(int cell, int digit)
        {
            int bit = 1 << (digit - 1);

            _values[cell] = digit;
            _rowMasks[cell / Grid.Size] |= bit;
            _columnMasks[cell % Grid.Size] |= bit;
            _boxMasks[BoxOfCell[cell]] |= bit;
            _trail.Add(cell);
        }

        private void UndoTo(int mark)
        {
            while (_trail.Count > mark)
            {
                int cell = _trail[^1];
                _trail.RemoveAt(_trail.Count - 1);

                int bit = 1 << (_values[cell] - 1);
                _rowMasks[cell / Grid.Size] &= ~bit;
                _columnMasks[cell % Grid.Size] &= ~bit;
                _boxMasks[BoxOfCell[cell]] &= ~bit;
                _values[cell] = Grid.EmptyCell;
            }
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

        private static int[][] CreateUnits()
        {
            var units = new int[27][];

            for (int row = 0; row < Grid.Size; row++)
            {
                var cells = new int[Grid.Size];
                for (int column = 0; column < Grid.Size; column++)
                {
                    cells[column] = (row * Grid.Size) + column;
                }

                units[row] = cells;
            }

            for (int column = 0; column < Grid.Size; column++)
            {
                var cells = new int[Grid.Size];
                for (int row = 0; row < Grid.Size; row++)
                {
                    cells[row] = (row * Grid.Size) + column;
                }

                units[Grid.Size + column] = cells;
            }

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

                units[(2 * Grid.Size) + box] = cells;
            }

            return units;
        }
    }
}
