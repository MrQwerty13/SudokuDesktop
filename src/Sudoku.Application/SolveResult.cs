using Sudoku.Domain;

namespace Sudoku.Application;

/// <summary>
/// Результат работы решателя: вид решения и найденные варианты.
/// </summary>
public sealed class SolveResult
{
    private static readonly IReadOnlyList<Grid> NoSolutions = Array.Empty<Grid>();

    private SolveResult(SolveOutcome outcome, IReadOnlyList<Grid> solutions)
    {
        Outcome = outcome;
        Solutions = solutions;
    }

    /// <summary>
    /// Вид решения: отсутствует, единственное или несколько.
    /// </summary>
    public SolveOutcome Outcome { get; }

    /// <summary>
    /// Найденные решения в порядке обнаружения. Их количество ограничено пределом поиска.
    /// </summary>
    public IReadOnlyList<Grid> Solutions { get; }

    /// <summary>
    /// Количество найденных решений (не больше предела поиска).
    /// </summary>
    public int SolutionCount => Solutions.Count;

    /// <summary>
    /// Признак наличия хотя бы одного решения.
    /// </summary>
    public bool HasSolution => Solutions.Count > 0;

    /// <summary>
    /// Признак единственности решения.
    /// </summary>
    public bool HasUniqueSolution => Outcome == SolveOutcome.UniqueSolution;

    /// <summary>
    /// Первое найденное решение либо <c>null</c>, если решений нет.
    /// </summary>
    public Grid? Solution => Solutions.Count > 0 ? Solutions[0] : null;

    internal static SolveResult Create(IReadOnlyList<Grid> solutions)
        => solutions.Count switch
        {
            0 => new SolveResult(SolveOutcome.NoSolution, NoSolutions),
            1 => new SolveResult(SolveOutcome.UniqueSolution, solutions),
            _ => new SolveResult(SolveOutcome.MultipleSolutions, solutions),
        };
}
