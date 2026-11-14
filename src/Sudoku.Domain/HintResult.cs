namespace Sudoku.Domain;

/// <summary>
/// Результат запроса подсказки.
/// </summary>
/// <remarks>
/// <para>Подсказка всегда основана на <em>подтверждённом решении</em> текущей головоломки
/// (<see cref="Puzzle.Solution"/>), а не на случайном числе: значение, возвращаемое
/// объектом, гарантированно является правильным для этой головоломки.</para>
/// <para>Продуктовые правила (зафиксированы владельцем):</para>
/// <list type="bullet">
/// <item>исходная (заданная) ячейка — подсказка не применяется, но значение показывается;</item>
/// <item>ячейка с введённым игроком значением — значение исправляется на правильное;</item>
/// <item>если ячейка не выбрана — сессия сама выбирает первую пустую ячейку.</item>
/// </list>
/// </remarks>
public sealed class HintResult
{
    private HintResult(HintStatus status, CellPosition? position, int value)
    {
        Status = status;
        Position = position;
        Value = value;
    }

    /// <summary>
    /// Итог запроса подсказки.
    /// </summary>
    public HintStatus Status { get; }

    /// <summary>
    /// Клетка, к которой относится подсказка, либо <c>null</c>, если подсказка невозможна.
    /// </summary>
    public CellPosition? Position { get; }

    /// <summary>
    /// Правильное значение для клетки по подтверждённому решению.
    /// Значение <see cref="Grid.EmptyCell"/> означает, что показывать нечего.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Признак того, что подсказка изменила состояние доски.
    /// </summary>
    public bool Applied => Status is HintStatus.Revealed or HintStatus.Corrected;

    /// <summary>
    /// Признак того, что подсказка показала значение, но не меняла доску.
    /// </summary>
    public bool RevealsValue => Value != Grid.EmptyCell;

    /// <summary>
    /// Создаёт результат запроса подсказки.
    /// </summary>
    /// <param name="status">Итог запроса.</param>
    /// <param name="position">Клетка, к которой относится подсказка.</param>
    /// <param name="value">Правильное значение из подтверждённого решения.</param>
    /// <returns>Результат запроса подсказки.</returns>
    public static HintResult Create(HintStatus status, CellPosition? position, int value)
        => new(status, position, value);
}
