namespace Sudoku.Domain;

/// <summary>
/// Приём (техника) решения судоку. Чем выше значение, тем сложнее приём.
/// </summary>
/// <remarks>
/// Порядок значений зафиксирован лестницей техник из
/// <c>docs/ADR/0003-difficulty-rating.md</c> и используется как «потолок техник»
/// при оценке сложности головоломки.
/// </remarks>
public enum SudokuTechnique
{
    /// <summary>
    /// Единственный кандидат в ячейке (naked single).
    /// </summary>
    NakedSingle = 1,

    /// <summary>
    /// Единственное место для цифры в строке, столбце или блоке (hidden single).
    /// </summary>
    HiddenSingle = 2,

    /// <summary>
    /// Все кандидаты цифры в блоке лежат в одной строке (pointing pair).
    /// </summary>
    PointingPair = 3,

    /// <summary>
    /// Все кандидаты цифры в строке лежат в одном блоке (claiming pair).
    /// </summary>
    ClaimingPair = 4,

    /// <summary>
    /// Две ячейки с одинаковой парой кандидатов (naked pair).
    /// </summary>
    NakedPair = 5,

    /// <summary>
    /// Две цифры, которые могут стоять только в одной и той же паре ячеек (hidden pair).
    /// </summary>
    HiddenPair = 6,
}
