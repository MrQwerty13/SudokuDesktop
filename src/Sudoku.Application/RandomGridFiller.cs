using Sudoku.Domain;

namespace Sudoku.Application;

/// <summary>
/// Заполнение пустой доски случайным корректным решением.
/// </summary>
internal static class RandomGridFiller
{
    public static Grid Create(IRandomSource random)
    {
        var values = new int[Grid.CellCount];
        var rowMasks = new int[Grid.Size];
        var columnMasks = new int[Grid.Size];
        var boxMasks = new int[Grid.Size];

        if (!Fill(values, rowMasks, columnMasks, boxMasks, random))
        {
            throw new InvalidOperationException("Не удалось заполнить доску корректным решением.");
        }

        return Grid.FromCells(values);
    }

    private static bool Fill(
        int[] values,
        int[] rowMasks,
        int[] columnMasks,
        int[] boxMasks,
        IRandomSource random)
    {
        int cell = SelectCell(values, rowMasks, columnMasks, boxMasks);
        if (cell < 0)
        {
            return true;
        }

        int row = cell / Grid.Size;
        int column = cell % Grid.Size;
        int box = ((row / 3) * 3) + (column / 3);

        Span<int> digits = stackalloc int[Grid.Size];
        for (int digit = 1; digit <= Grid.Size; digit++)
        {
            digits[digit - 1] = digit;
        }

        for (int index = digits.Length - 1; index > 0; index--)
        {
            int swap = random.Next(index + 1);
            (digits[index], digits[swap]) = (digits[swap], digits[index]);
        }

        foreach (int digit in digits)
        {
            int bit = 1 << (digit - 1);
            if (((rowMasks[row] | columnMasks[column] | boxMasks[box]) & bit) != 0)
            {
                continue;
            }

            values[cell] = digit;
            rowMasks[row] |= bit;
            columnMasks[column] |= bit;
            boxMasks[box] |= bit;

            if (Fill(values, rowMasks, columnMasks, boxMasks, random))
            {
                return true;
            }

            values[cell] = Grid.EmptyCell;
            rowMasks[row] &= ~bit;
            columnMasks[column] &= ~bit;
            boxMasks[box] &= ~bit;
        }

        return false;
    }

    private static int SelectCell(int[] values, int[] rowMasks, int[] columnMasks, int[] boxMasks)
    {
        int best = -1;
        int bestCount = int.MaxValue;

        for (int index = 0; index < Grid.CellCount; index++)
        {
            if (values[index] != Grid.EmptyCell)
            {
                continue;
            }

            int row = index / Grid.Size;
            int column = index % Grid.Size;
            int box = ((row / 3) * 3) + (column / 3);

            int used = rowMasks[row] | columnMasks[column] | boxMasks[box];
            int count = 0;
            for (int digit = 1; digit <= Grid.Size; digit++)
            {
                if ((used & (1 << (digit - 1))) == 0)
                {
                    count++;
                }
            }

            if (count < bestCount)
            {
                bestCount = count;
                best = index;

                if (count == 1)
                {
                    break;
                }
            }
        }

        return best;
    }
}
