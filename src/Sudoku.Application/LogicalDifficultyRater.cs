using Sudoku.Domain;

namespace Sudoku.Application;

/// <summary>
/// Оценщик сложности v1: потолок логических приёмов.
/// </summary>
/// <remarks>
/// <para>Потолок техник определяется так: минимальный уровень приёмов
/// (<see cref="SudokuTechnique"/>), при котором логический решатель полностью
/// решает головоломку без перебора. Оценка воспроизводима: зависит только от
/// состояния доски.</para>
/// <para>Потолок — <em>не</em> «самая сложная техника, которую пришлось применить»,
/// а минимальный уровень, <em>достаточный</em> для полного решения: это делает
/// оценку устойчивой к порядку применения приёмов.</para>
/// </remarks>
public sealed class LogicalDifficultyRater : IDifficultyRater
{
    private readonly ISudokuSolver _solver;

    /// <summary>
    /// Создаёт оценщик сложности.
    /// </summary>
    /// <param name="solver">Решатель для проверки наличия и единственности решения.</param>
    /// <exception cref="ArgumentNullException">Параметр <paramref name="solver"/> равен <c>null</c>.</exception>
    public LogicalDifficultyRater(ISudokuSolver solver)
    {
        ArgumentNullException.ThrowIfNull(solver);

        _solver = solver;
    }

    /// <inheritdoc />
    public DifficultyRating Rate(Grid puzzle)
    {
        ArgumentNullException.ThrowIfNull(puzzle);

        int givens = CountGivens(puzzle);
        int solutions = _solver.CountSolutions(puzzle, 2);

        if (solutions == 0)
        {
            return DifficultyRating.Unsolvable(givens);
        }

        bool unique = solutions == 1;

        var solver = new LogicalSolver(puzzle);
        for (int level = 1; level <= DifficultyBands.MaxTechniqueLevel; level++)
        {
            if (solver.SolveUpTo(level))
            {
                return DifficultyRating.Solved(unique, (SudokuTechnique)level, givens);
            }
        }

        return DifficultyRating.NeedingGuessing(unique, givens);
    }

    /// <inheritdoc />
    public bool CanSolveWith(Grid puzzle, int maxTechniqueLevel)
    {
        ArgumentNullException.ThrowIfNull(puzzle);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxTechniqueLevel, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxTechniqueLevel, DifficultyBands.MaxTechniqueLevel);

        return new LogicalSolver(puzzle).SolveUpTo(maxTechniqueLevel);
    }

    private static int CountGivens(Grid puzzle)
    {
        int givens = 0;
        for (int index = 0; index < Grid.CellCount; index++)
        {
            if (puzzle[index] != Grid.EmptyCell)
            {
                givens++;
            }
        }

        return givens;
    }
}
