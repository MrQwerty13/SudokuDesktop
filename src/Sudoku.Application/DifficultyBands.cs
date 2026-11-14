using Sudoku.Domain;

namespace Sudoku.Application;

/// <summary>
/// Калибровка уровней сложности: диапазон потолка логических приёмов
/// и минимальное число заданных ячеек.
/// </summary>
/// <remarks>
/// <para>Калибровка — единственный источник правды о том, что означает уровень.
/// Генератор и тесты обращаются к ней, поэтому «заявленный уровень» и
/// «проверенный уровень» не могут разойтись.</para>
/// <para>Основание выбора порогов — <c>docs/ADR/0003-difficulty-rating.md</c>
/// (лестница техник v1) и <c>docs/ADR/0009-puzzle-generation-algorithm.md</c>
/// (эмпирическая калибровка по выборке сгенерированных головоломок).</para>
/// <para>Минимальное число заданных ячеек — <em>страховочный</em> предел, а не критерий
/// сложности: он не позволяет выдать слишком пустую доску, но сам по себе
/// уровень не определяет.</para>
/// </remarks>
public static class DifficultyBands
{
    /// <summary>
    /// Потолок приёмов уровня Easy: одиночная и скрытая одиночка.
    /// </summary>
    public const int EasyMinTechniqueLevel = 1;

    /// <summary>
    /// Верхняя граница потолка приёмов уровня Easy.
    /// </summary>
    public const int EasyMaxTechniqueLevel = 2;

    /// <summary>
    /// Потолок приёмов уровня Normal: указывающие и «заявляющие» пары.
    /// </summary>
    public const int NormalMinTechniqueLevel = 3;

    /// <summary>
    /// Верхняя граница потолка приёмов уровня Normal.
    /// </summary>
    public const int NormalMaxTechniqueLevel = 4;

    /// <summary>
    /// Потолок приёмов уровня Hard: голые и скрытые пары.
    /// </summary>
    public const int HardMinTechniqueLevel = 5;

    /// <summary>
    /// Верхняя граница потолка приёмов уровня Hard.
    /// </summary>
    public const int HardMaxTechniqueLevel = 6;

    /// <summary>
    /// Наибольший уровень приёма в лестнице v1.
    /// </summary>
    public const int MaxTechniqueLevel = HardMaxTechniqueLevel;

    /// <summary>
    /// Страховочный минимум заданных ячеек для уровня Easy.
    /// </summary>
    public const int EasyMinGivens = 22;

    /// <summary>
    /// Страховочный минимум заданных ячеек для уровня Normal.
    /// </summary>
    public const int NormalMinGivens = 20;

    /// <summary>
    /// Страховочный минимум заданных ячеек для уровня Hard.
    /// </summary>
    public const int HardMinGivens = 20;

    /// <summary>
    /// Возвращает калибровку для указанного уровня сложности.
    /// </summary>
    /// <param name="difficulty">Уровень сложности.</param>
    /// <returns>Диапазон потолка приёмов и минимальное число заданных ячеек.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Уровень сложности не поддерживается.</exception>
    public static DifficultyBand For(Difficulty difficulty)
        => difficulty switch
        {
            Difficulty.Easy => new DifficultyBand(
                EasyMinTechniqueLevel,
                EasyMaxTechniqueLevel,
                EasyMinGivens),
            Difficulty.Normal => new DifficultyBand(
                NormalMinTechniqueLevel,
                NormalMaxTechniqueLevel,
                NormalMinGivens),
            Difficulty.Hard => new DifficultyBand(
                HardMinTechniqueLevel,
                HardMaxTechniqueLevel,
                HardMinGivens),
            _ => throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, null),
        };

    /// <summary>
    /// Проверяет, попадает ли оценка в полосу заданного уровня сложности.
    /// </summary>
    /// <param name="rating">Оценка головоломки.</param>
    /// <param name="band">Калибровка уровня.</param>
    /// <returns><c>true</c>, если оценка подтверждает заявленный уровень.</returns>
    /// <exception cref="ArgumentNullException">Параметр <paramref name="rating"/> равен <c>null</c>.</exception>
    public static bool Contains(DifficultyRating rating, DifficultyBand band)
    {
        ArgumentNullException.ThrowIfNull(rating);

        return rating.HasUniqueSolution
            && !rating.RequiresGuessing
            && rating.TechniqueLevel >= band.MinTechniqueLevel
            && rating.TechniqueLevel <= band.MaxTechniqueLevel;
    }
}
