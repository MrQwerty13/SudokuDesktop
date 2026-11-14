using Sudoku.Domain;

namespace Sudoku.Application;

/// <summary>
/// Заход в режиме Endless: последовательность головоломок одной сложности
/// без ограничения времени.
/// </summary>
/// <remarks>
/// <para>Сценарий не дублирует игровую механику: ввод, стирание, конфликты, подсказки
/// и определение победы выполняет <see cref="GameSession"/>. Здесь — только оркестровка
/// захода: какая головоломка сейчас, какая следующая, сколько решено и сколько
/// подсказок использовано.</para>
/// <para>Режим фиксирован: сессии создаются с <see cref="GameMode.Endless"/>, поэтому
/// случайно запустить Time Attack из этого сценария невозможно.</para>
/// <para>Детерминизм: первая головоломка строится по переданному seed, каждая следующая —
/// по seed + (номер − 1). Один и тот же seed даёт один и тот же заход.</para>
/// </remarks>
public sealed class EndlessGame
{
    private readonly IGameSessionFactory _factory;
    private readonly int _seed;
    private int _puzzleIndex;

    /// <summary>
    /// Создаёт заход режима Endless и первую головоломку.
    /// </summary>
    /// <param name="factory">Фабрика игровых сессий.</param>
    /// <param name="difficulty">Сложность всех головоломок захода.</param>
    /// <param name="seed">Seed первой головоломки.</param>
    /// <exception cref="ArgumentNullException">Параметр <paramref name="factory"/> равен <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Уровень сложности не поддерживается.</exception>
    /// <exception cref="PuzzleGenerationException">
    /// Не удалось построить головоломку заданного уровня за отведённое число попыток.
    /// </exception>
    public EndlessGame(IGameSessionFactory factory, Difficulty difficulty, int seed)
    {
        ArgumentNullException.ThrowIfNull(factory);

        _factory = factory;
        _seed = seed;
        Difficulty = difficulty;

        Session = factory.CreateNewEndlessGame(difficulty, seed);
    }

    /// <summary>
    /// Режим игры захода.
    /// </summary>
    public GameMode Mode => GameMode.Endless;

    /// <summary>
    /// Сложность головоломок захода.
    /// </summary>
    public Difficulty Difficulty { get; }

    /// <summary>
    /// Текущая игровая сессия.
    /// </summary>
    public GameSession Session { get; private set; }

    /// <summary>
    /// Номер текущей головоломки в заходе (начиная с 1).
    /// </summary>
    public int PuzzleNumber => _puzzleIndex + 1;

    /// <summary>
    /// Количество решённых головоломок в заходе.
    /// </summary>
    public int SolvedPuzzleCount { get; private set; }

    /// <summary>
    /// Суммарное количество применённых подсказок в заходе. Подсказки не ограничены.
    /// </summary>
    public int HintCount { get; private set; }

    /// <summary>
    /// Признак того, что текущая головоломка решена.
    /// </summary>
    /// <remarks>
    /// Победа определяется общей игровой логикой
    /// (<see cref="GameSession.IsSolved"/>): поле должно быть заполнено полностью
    /// и безошибочно.
    /// </remarks>
    public bool IsCurrentPuzzleSolved => Session.IsSolved;

    /// <summary>
    /// Снимки состояния ячеек текущего поля.
    /// </summary>
    public IReadOnlyList<Cell> Cells => Session.Cells;

    /// <summary>
    /// Выбирает ячейку текущего поля.
    /// </summary>
    /// <param name="position">Позиция ячейки.</param>
    public void SelectCell(CellPosition position) => Session.SelectCell(position);

    /// <summary>
    /// Вводит значение в выбранную ячейку.
    /// </summary>
    /// <param name="digit">Вводимая цифра (1–9).</param>
    /// <returns>Результат действия.</returns>
    public MoveOutcome EnterValue(int digit) => Session.EnterValue(digit);

    /// <summary>
    /// Стирает значение, введённое игроком, в выбранной ячейке.
    /// </summary>
    /// <returns>Результат действия.</returns>
    public MoveOutcome EraseValue() => Session.EraseValue();

    /// <summary>
    /// Запрашивает подсказку для выбранной ячейки.
    /// </summary>
    /// <returns>Итог запроса подсказки.</returns>
    public HintResult RequestHint()
    {
        HintResult result = Session.RevealHint();

        if (result.Applied)
        {
            HintCount++;
        }

        return result;
    }

    /// <summary>
    /// Завершает текущую головоломку и начинает следующую той же сложности.
    /// </summary>
    /// <remarks>
    /// Решённая головоломка засчитывается в счётчике; брошенная — нет.
    /// Время решения не ограничено: переход выполняется по решению игрока.
    /// </remarks>
    public void StartNextPuzzle()
    {
        if (Session.IsSolved)
        {
            SolvedPuzzleCount++;
        }

        _puzzleIndex++;
        Session = _factory.CreateNewEndlessGame(Difficulty, _seed + _puzzleIndex);
    }
}
