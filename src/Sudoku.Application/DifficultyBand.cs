using Sudoku.Domain;

namespace Sudoku.Application;

/// <summary>
/// Калибровка уровня сложности: диапазон потолка логических приёмов
/// и страховочный минимум заданных ячеек.
/// </summary>
/// <param name="MinTechniqueLevel">Минимальный допустимый потолок приёмов (1–6).</param>
/// <param name="MaxTechniqueLevel">Максимальный допустимый потолок приёмов (1–6).</param>
/// <param name="MinGivens">Страховочный минимум заданных ячеек.</param>
public readonly record struct DifficultyBand(
    int MinTechniqueLevel,
    int MaxTechniqueLevel,
    int MinGivens)
{
    /// <summary>
    /// Признак того, что диапазон потолка приёмов задан корректно.
    /// </summary>
    public bool IsValid =>
        MinTechniqueLevel >= 1
        && MaxTechniqueLevel <= DifficultyBands.MaxTechniqueLevel
        && MinTechniqueLevel <= MaxTechniqueLevel
        && MinGivens >= MinPossibleGivens
        && MinGivens <= Grid.CellCount;

    /// <summary>
    /// Минимальное число заданных ячеек, при котором решение головоломки
    /// вообще может быть единственным (McGuire, Tugemann, Civario, 2012).
    /// </summary>
    public const int MinPossibleGivens = 17;
}
