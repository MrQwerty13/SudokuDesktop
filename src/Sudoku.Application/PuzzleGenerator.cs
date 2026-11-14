using Sudoku.Domain;

namespace Sudoku.Application;

/// <summary>
/// Генератор головоломок судоку.
/// </summary>
/// <remarks>
/// <para>Алгоритм (детали — в <c>docs/ADR/0009-puzzle-generation-algorithm.md</c>):</para>
/// <list type="number">
/// <item>случайное заполнение доски корректным решением (MRV + случайный порядок цифр);</item>
/// <item>случайная симметричная перестановка решения (переименование цифр, перестановки
/// строк внутри блоков, столбцов внутри блоков, перестановки блоков) — для разнообразия;</item>
/// <item>последовательное удаление заданных значений в случайном порядке. После каждого
/// удаления проверяется, что решение остаётся <em>единственным</em>
/// (<c>CountSolutions(limit: 2) == 1</c>) и что головоломка остаётся решаемой
/// приёмами не выше верхней границы полосы сложности;</item>
/// <item>удаление откатывается, если единственность нарушена или головоломка стала
/// сложнее верхней границы полосы;</item>
/// <item>головоломка принимается, только если итоговая оценка попадает в полосу
/// запрошенного уровня (<see cref="DifficultyBands"/>). Если после копания оценка
/// вышла за полосу, берётся последняя доска, подтверждённая как принадлежащая полосе;</item>
/// <item>если ни одна попытка не дала головоломку нужного уровня, выбрасывается
/// <see cref="PuzzleGenerationException"/>.</item>
/// </list>
/// <para>Детерминизм: одинаковые уровень сложности и seed дают одинаковую головоломку.</para>
/// </remarks>
public sealed class PuzzleGenerator : IPuzzleGenerator
{
    /// <summary>
    /// Максимальное число попыток построения головоломки заданного уровня.
    /// </summary>
    public const int MaxAttempts = 100;

    private readonly ISudokuSolver _solver;
    private readonly IDifficultyRater _rater;

    /// <summary>
    /// Создаёт генератор.
    /// </summary>
    /// <param name="solver">Решатель для проверки единственности решения.</param>
    /// <param name="rater">Оценщик сложности по потолку техник.</param>
    /// <exception cref="ArgumentNullException">
    /// Параметр <paramref name="solver"/> или <paramref name="rater"/> равен <c>null</c>.
    /// </exception>
    public PuzzleGenerator(ISudokuSolver solver, IDifficultyRater rater)
    {
        ArgumentNullException.ThrowIfNull(solver);
        ArgumentNullException.ThrowIfNull(rater);

        _solver = solver;
        _rater = rater;
    }

    /// <inheritdoc />
    public Puzzle Generate(Difficulty difficulty, int seed)
    {
        DifficultyBand band = DifficultyBands.For(difficulty);

        var random = new SystemRandomSource(seed);

        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            int[] solution = ToCells(RandomGridFiller.Create(random));
            ApplySymmetry(random, solution);

            Puzzle? puzzle = TryBuildPuzzle(solution, difficulty, band, random);
            if (puzzle is not null)
            {
                return puzzle;
            }
        }

        throw new PuzzleGenerationException(
            $"Не удалось построить головоломку уровня {difficulty} за {MaxAttempts} попыток.");
    }

    private Puzzle? TryBuildPuzzle(
        int[] solution,
        Difficulty difficulty,
        DifficultyBand band,
        IRandomSource random)
    {
        int[] board = (int[])solution.Clone();
        int[]? bandBoard = null;
        int givens = Grid.CellCount;

        foreach (int cell in ShuffleOrder(random))
        {
            if (board[cell] == Grid.EmptyCell)
            {
                continue;
            }

            int saved = board[cell];
            board[cell] = Grid.EmptyCell;

            Grid candidate = Grid.FromCells(board);

            if (_solver.CountSolutions(candidate, 2) != 1)
            {
                board[cell] = saved;
                continue;
            }

            if (!_rater.CanSolveWith(candidate, band.MaxTechniqueLevel))
            {
                board[cell] = saved;
                continue;
            }

            if (IsInBand(candidate, band))
            {
                bandBoard = (int[])board.Clone();
            }

            givens--;
            if (givens <= band.MinGivens)
            {
                break;
            }
        }

        Grid clues = Grid.FromCells(board);
        DifficultyRating rating = _rater.Rate(clues);

        if (!DifficultyBands.Contains(rating, band))
        {
            if (bandBoard is null)
            {
                return null;
            }

            clues = Grid.FromCells(bandBoard);
            rating = _rater.Rate(clues);

            if (!DifficultyBands.Contains(rating, band))
            {
                return null;
            }
        }

        return new Puzzle(clues, Grid.FromCells(solution), difficulty, rating);
    }

    private bool IsInBand(Grid candidate, DifficultyBand band)
    {
        if (band.MinTechniqueLevel <= 1)
        {
            return true;
        }

        return !_rater.CanSolveWith(candidate, band.MinTechniqueLevel - 1);
    }

    private static int[] ToCells(Grid grid)
    {
        var cells = new int[Grid.CellCount];
        for (int index = 0; index < Grid.CellCount; index++)
        {
            cells[index] = grid[index];
        }

        return cells;
    }

    private static void ApplySymmetry(IRandomSource random, int[] cells)
    {
        int[] digitMap = CreatePermutation(random, Grid.Size);
        int[] rowMap = CreateLineMap(random);
        int[] columnMap = CreateLineMap(random);

        var result = new int[Grid.CellCount];
        for (int row = 0; row < Grid.Size; row++)
        {
            for (int column = 0; column < Grid.Size; column++)
            {
                int value = cells[(row * Grid.Size) + column];
                result[(rowMap[row] * Grid.Size) + columnMap[column]] = value == Grid.EmptyCell
                    ? Grid.EmptyCell
                    : digitMap[value - 1] + 1;
            }
        }

        result.AsSpan().CopyTo(cells);
    }

    private static int[] CreatePermutation(IRandomSource random, int length)
    {
        var permutation = new int[length];
        for (int index = 0; index < length; index++)
        {
            permutation[index] = index;
        }

        for (int index = length - 1; index > 0; index--)
        {
            int swap = random.Next(index + 1);
            (permutation[index], permutation[swap]) = (permutation[swap], permutation[index]);
        }

        return permutation;
    }

    private static int[] CreateLineMap(IRandomSource random)
    {
        var map = new int[Grid.Size];

        for (int group = 0; group < 3; group++)
        {
            int[] lines = [group * 3, (group * 3) + 1, (group * 3) + 2];
            int[] shuffled = CreatePermutation(random, 3);

            for (int index = 0; index < 3; index++)
            {
                map[lines[index]] = lines[shuffled[index]];
            }
        }

        int[] groups = CreatePermutation(random, 3);
        var result = new int[Grid.Size];
        for (int group = 0; group < 3; group++)
        {
            for (int index = 0; index < 3; index++)
            {
                result[(groups[group] * 3) + index] = map[(group * 3) + index];
            }
        }

        return result;
    }

    private static int[] ShuffleOrder(IRandomSource random)
    {
        var order = new int[Grid.CellCount];
        for (int index = 0; index < Grid.CellCount; index++)
        {
            order[index] = index;
        }

        for (int index = order.Length - 1; index > 0; index--)
        {
            int swap = random.Next(index + 1);
            (order[index], order[swap]) = (order[swap], order[index]);
        }

        return order;
    }
}
