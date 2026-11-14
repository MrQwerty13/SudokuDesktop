namespace Sudoku.Domain;

/// <summary>
/// Оценка сложности головоломки: наличие решения и «потолок техник».
/// </summary>
/// <remarks>
/// Потолок техник — минимальный уровень приёмов, при котором головоломка
/// решается полностью без перебора (см. <see cref="SudokuTechnique"/>).
/// Если даже полная лестница v1 не решает головоломку, требуется перебор.
/// </remarks>
public sealed class DifficultyRating
{
    private DifficultyRating(
        bool hasSolution,
        bool hasUniqueSolution,
        SudokuTechnique? techniqueCeiling,
        bool requiresGuessing,
        int givensCount)
    {
        HasSolution = hasSolution;
        HasUniqueSolution = hasUniqueSolution;
        TechniqueCeiling = techniqueCeiling;
        RequiresGuessing = requiresGuessing;
        GivensCount = givensCount;
    }

    /// <summary>
    /// Признак наличия хотя бы одного решения.
    /// </summary>
    public bool HasSolution { get; }

    /// <summary>
    /// Признак единственности решения.
    /// </summary>
    public bool HasUniqueSolution { get; }

    /// <summary>
    /// Наиболее сложный приём, необходимый для решения без перебора,
    /// либо <c>null</c>, если решения нет или требуется перебор.
    /// </summary>
    public SudokuTechnique? TechniqueCeiling { get; }

    /// <summary>
    /// Признак того, что для решения нужен перебор (угадывание).
    /// </summary>
    public bool RequiresGuessing { get; }

    /// <summary>
    /// Количество заданных ячеек в оцениваемой головоломке.
    /// </summary>
    public int GivensCount { get; }

    /// <summary>
    /// Числовой уровень потолка техник: от 1 до 6, либо 0, если потолок не определён.
    /// </summary>
    public int TechniqueLevel => TechniqueCeiling is null ? 0 : (int)TechniqueCeiling.Value;

    /// <summary>
    /// Создаёт оценку головоломки без решения.
    /// </summary>
    /// <param name="givensCount">Количество заданных ячеек.</param>
    /// <returns>Оценка: решения нет, потолок техник не определён.</returns>
    public static DifficultyRating Unsolvable(int givensCount)
        => new(false, false, null, false, givensCount);

    /// <summary>
    /// Создаёт оценку головоломки, для которой потребовался перебор.
    /// </summary>
    /// <param name="hasUniqueSolution">Признак единственности решения.</param>
    /// <param name="givensCount">Количество заданных ячеек.</param>
    /// <returns>Оценка: решение есть, но лестница v1 недостаточна.</returns>
    public static DifficultyRating NeedingGuessing(bool hasUniqueSolution, int givensCount)
        => new(true, hasUniqueSolution, null, true, givensCount);

    /// <summary>
    /// Создаёт оценку головоломки, решаемой логическими приёмами.
    /// </summary>
    /// <param name="hasUniqueSolution">Признак единственности решения.</param>
    /// <param name="techniqueCeiling">Потолок техник.</param>
    /// <param name="givensCount">Количество заданных ячеек.</param>
    /// <returns>Оценка головоломки.</returns>
    public static DifficultyRating Solved(
        bool hasUniqueSolution,
        SudokuTechnique techniqueCeiling,
        int givensCount)
        => new(true, hasUniqueSolution, techniqueCeiling, false, givensCount);
}
