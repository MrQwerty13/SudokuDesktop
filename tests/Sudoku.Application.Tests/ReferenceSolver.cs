using Sudoku.Domain;

namespace Sudoku.Tests.Application;

/// <summary>
/// Независимый эталонный решатель для проверки выводов production-решателя.
/// </summary>
/// <remarks>
/// Реализация намеренно отличается от <see cref="MrvSudokuSolver"/>: используются
/// множества кандидатов и перебор без распространения ограничений. Её задача —
/// не быть быстрым, а быть заведомо корректным: если оба решателя дают разный
/// ответ о количестве решений, значит в одном из них есть ошибка.
/// </remarks>
internal static class ReferenceSolver
{
    private const int Solved = -1;
    private const int Contradiction = -2;

    /// <summary>
    /// Считает решения головоломки, останавливаясь после нахождения <paramref name="limit"/> решений.
    /// </summary>
    /// <param name="grid">Головоломка.</param>
    /// <param name="limit">Предел количества решений.</param>
    /// <returns>Количество найденных решений, не превышающее предел.</returns>
    public static int CountSolutions(Grid grid, int limit)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);

        var cells = new int[Grid.CellCount];
        for (int index = 0; index < Grid.CellCount; index++)
        {
            cells[index] = grid[index];
        }

        int found = 0;
        Search(cells, limit, ref found);
        return found;
    }

    /// <summary>
    /// Проверяет, что заполнение является корректным решением заданных значений.
    /// </summary>
    /// <param name="grid">Заполнение.</param>
    /// <returns><c>true</c>, если заполнение полное, непротиворечивое и корректное.</returns>
    public static bool IsValidSolution(Grid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);

        if (!grid.IsComplete || !grid.IsConsistent)
        {
            return false;
        }

        for (int row = 0; row < Grid.Size; row++)
        {
            var digits = new HashSet<int>();
            for (int column = 0; column < Grid.Size; column++)
            {
                digits.Add(grid[row, column]);
            }

            if (digits.Count != Grid.Size)
            {
                return false;
            }
        }

        for (int column = 0; column < Grid.Size; column++)
        {
            var digits = new HashSet<int>();
            for (int row = 0; row < Grid.Size; row++)
            {
                digits.Add(grid[row, column]);
            }

            if (digits.Count != Grid.Size)
            {
                return false;
            }
        }

        for (int box = 0; box < Grid.Size; box++)
        {
            var digits = new HashSet<int>();
            for (int row = 0; row < Grid.Size; row++)
            {
                for (int column = 0; column < Grid.Size; column++)
                {
                    if ((row / 3) == (box / 3) && (column / 3) == (box % 3))
                    {
                        digits.Add(grid[row, column]);
                    }
                }
            }

            if (digits.Count != Grid.Size)
            {
                return false;
            }
        }

        return true;
    }

    private static void Search(int[] cells, int limit, ref int found)
    {
        if (found >= limit)
        {
            return;
        }

        int cell = SelectCell(cells);
        if (cell == Contradiction)
        {
            return;
        }

        if (cell == Solved)
        {
            found++;
            return;
        }

        for (int digit = 1; digit <= Grid.Size; digit++)
        {
            if (!CanPlace(cells, cell, digit))
            {
                continue;
            }

            cells[cell] = digit;
            Search(cells, limit, ref found);
            cells[cell] = Grid.EmptyCell;

            if (found >= limit)
            {
                return;
            }
        }
    }

    private static int SelectCell(int[] cells)
    {
        int best = -1;
        int bestCount = int.MaxValue;

        for (int index = 0; index < Grid.CellCount; index++)
        {
            if (cells[index] != Grid.EmptyCell)
            {
                continue;
            }

            int count = 0;
            for (int digit = 1; digit <= Grid.Size; digit++)
            {
                if (CanPlace(cells, index, digit))
                {
                    count++;
                }
            }

            if (count == 0)
            {
                return Contradiction;
            }

            if (count < bestCount)
            {
                bestCount = count;
                best = index;
            }
        }

        return best < 0 ? Solved : best;
    }

    private static bool CanPlace(int[] cells, int cell, int digit)
    {
        int row = cell / Grid.Size;
        int column = cell % Grid.Size;

        for (int index = 0; index < Grid.Size; index++)
        {
            if (cells[(row * Grid.Size) + index] == digit)
            {
                return false;
            }

            if (cells[(index * Grid.Size) + column] == digit)
            {
                return false;
            }
        }

        int startRow = (row / 3) * 3;
        int startColumn = (column / 3) * 3;

        for (int r = startRow; r < startRow + 3; r++)
        {
            for (int c = startColumn; c < startColumn + 3; c++)
            {
                if (cells[(r * Grid.Size) + c] == digit)
                {
                    return false;
                }
            }
        }

        return true;
    }
}
