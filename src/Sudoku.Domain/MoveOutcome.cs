namespace Sudoku.Domain;

/// <summary>
/// Результат действия игрока над доской.
/// </summary>
public enum MoveOutcome
{
    /// <summary>
    /// Действие применено, состояние доски изменилось.
    /// </summary>
    Applied = 0,

    /// <summary>
    /// Действие применено, но состояние доски не изменилось: значение уже было таким.
    /// </summary>
    Unchanged = 1,

    /// <summary>
    /// Действие отклонено: не выбрана ячейка.
    /// </summary>
    RejectedNoSelection = 2,

    /// <summary>
    /// Действие отклонено: ячейка исходная (заданная), её значение менять нельзя.
    /// </summary>
    RejectedGivenCell = 3,

    /// <summary>
    /// Действие отклонено: в ячейке нечего стирать.
    /// </summary>
    RejectedEmptyCell = 4,

    /// <summary>
    /// Действие отклонено: игра уже завершена.
    /// </summary>
    RejectedGameCompleted = 5,
}
