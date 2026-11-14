namespace Sudoku.Domain;

/// <summary>
/// Поиск конфликтов на доске: ячейка конфликтует, если её значение повторяется
/// в строке, столбце или блоке.
/// </summary>
internal static class ConflictScanner
{
    /// <summary>
    /// Вычисляет карту конфликтов для доски.
    /// </summary>
    /// <param name="grid">Доска.</param>
    /// <returns>Массив из 81 признака: <c>true</c>, если ячейка конфликтует.</returns>
    public static bool[] Compute(Grid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);

        var conflicts = new bool[Grid.CellCount];

        MarkDuplicates(grid, conflicts, row: true);
        MarkDuplicates(grid, conflicts, row: false);

        for (int box = 0; box < Grid.Size; box++)
        {
            MarkBoxDuplicates(grid, conflicts, box);
        }

        return conflicts;
    }

    private static void MarkDuplicates(Grid grid, bool[] conflicts, bool row)
    {
        for (int line = 0; line < Grid.Size; line++)
        {
            var counts = new int[Grid.Size + 1];

            for (int offset = 0; offset < Grid.Size; offset++)
            {
                int index = row ? (line * Grid.Size) + offset : (offset * Grid.Size) + line;
                int value = grid[index];

                if (value != Grid.EmptyCell)
                {
                    counts[value]++;
                }
            }

            for (int offset = 0; offset < Grid.Size; offset++)
            {
                int index = row ? (line * Grid.Size) + offset : (offset * Grid.Size) + line;
                int value = grid[index];

                if (value != Grid.EmptyCell && counts[value] > 1)
                {
                    conflicts[index] = true;
                }
            }
        }
    }

    private static void MarkBoxDuplicates(Grid grid, bool[] conflicts, int box)
    {
        var counts = new int[Grid.Size + 1];
        int startRow = (box / 3) * 3;
        int startColumn = (box % 3) * 3;

        for (int row = startRow; row < startRow + 3; row++)
        {
            for (int column = startColumn; column < startColumn + 3; column++)
            {
                int value = grid[row, column];

                if (value != Grid.EmptyCell)
                {
                    counts[value]++;
                }
            }
        }

        for (int row = startRow; row < startRow + 3; row++)
        {
            for (int column = startColumn; column < startColumn + 3; column++)
            {
                int value = grid[row, column];

                if (value != Grid.EmptyCell && counts[value] > 1)
                {
                    conflicts[(row * Grid.Size) + column] = true;
                }
            }
        }
    }
}
