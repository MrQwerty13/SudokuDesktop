namespace Sudoku.Tests.Domain;

/// <summary>
/// Опубликованные данные для тестов игровой сессии.
/// </summary>
/// <remarks>
/// Головоломка и её решение взяты из статьи «Sudoku» в Википедии и не получены
/// от собственного решателя.
/// </remarks>
internal static class SessionTestData
{
    /// <summary>
    /// Каноническая головоломка: 30 заданных значений, 51 пустая ячейка.
    /// </summary>
    public const string Classic = "53..7....6..195....98....6.8...6...34..8.3..17...2...6.6....28....419..5....8..79";

    /// <summary>
    /// Опубликованное решение канонической головоломки.
    /// </summary>
    public const string ClassicSolution = "534678912672195348198342567859761423426853791713924856961537284287419635345286179";

    /// <summary>
    /// Пустая ячейка в строке 0, столбце 2; правильное значение — 4.
    /// </summary>
    public const int EmptyRowZeroColumnTwo = 2;

    /// <summary>
    /// Пустая ячейка в строке 1, столбце 1; правильное значение — 7.
    /// </summary>
    public const int EmptyRowOneColumnOne = 10;

    /// <summary>
    /// Пустая ячейка в строке 0, столбце 3; правильное значение — 6.
    /// </summary>
    public const int EmptyRowZeroColumnThree = 3;

    /// <summary>
    /// Исходная ячейка в строке 0, столбце 0; значение — 5.
    /// </summary>
    public const int GivenRowZeroColumnZero = 0;
}
