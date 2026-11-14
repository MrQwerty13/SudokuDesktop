namespace Sudoku.Domain;

/// <summary>
/// Игровая сессия: текущее состояние одной головоломки.
/// </summary>
/// <remarks>
/// <para>Сессия хранит три доски: исходную (<see cref="Puzzle.Clues"/> — заданные значения),
/// текущую (<see cref="State"/> — то, что видит игрок) и правильную
/// (<see cref="Solution"/> — подтверждённое решение головоломки).</para>
/// <para>Правила игры:</para>
/// <list type="bullet">
/// <item>исходные ячейки менять нельзя: ввод и стирание в них отклоняются;</item>
/// <item>значение, конфликтующее с другими значениями, вводится, но помечается
/// как конфликтное — так игрок видит ошибку, а поле остаётся введённым;</item>
/// <item>подсказка всегда опирается на подтверждённое решение и никогда не выдаёт
/// случайное значение;</item>
/// <item>победа засчитывается только при полном и полностью правильном заполнении поля.</item>
/// </list>
/// <para>Тип не зависит от UI: слой представления читает состояние через
/// <see cref="Cells"/> и <see cref="GetCell(int)"/> и вызывает методы действия.</para>
/// </remarks>
public sealed class GameSession
{
    private readonly Puzzle _puzzle;
    private readonly GameMode _mode;
    private Grid _state;
    private bool[] _conflicts;
    private CellPosition? _selectedCell;
    private int _moveCount;
    private int _hintCount;

    /// <summary>
    /// Создаёт игровую сессию по готовой головоломке.
    /// </summary>
    /// <param name="puzzle">Головоломка с подтверждённым решением.</param>
    /// <param name="mode">Режим игры, которому принадлежит сессия.</param>
    /// <exception cref="ArgumentNullException">Параметр <paramref name="puzzle"/> равен <c>null</c>.</exception>
    /// <exception cref="ArgumentException">
    /// Решение неполное или исходные значения не совпадают с решением.
    /// </exception>
    public GameSession(Puzzle puzzle, GameMode mode)
    {
        ArgumentNullException.ThrowIfNull(puzzle);
        Validate(puzzle);

        _puzzle = puzzle;
        _mode = mode;
        _state = puzzle.Clues;
        _conflicts = ConflictScanner.Compute(_state);
    }

    /// <summary>
    /// Головоломка сессии.
    /// </summary>
    public Puzzle Puzzle => _puzzle;

    /// <summary>
    /// Исходное поле: только заданные значения.
    /// </summary>
    public Grid InitialState => _puzzle.Clues;

    /// <summary>
    /// Текущее поле: заданные значения и введённые игроком.
    /// </summary>
    public Grid State => _state;

    /// <summary>
    /// Подтверждённое решение головоломки — источник значений для подсказок.
    /// </summary>
    public Grid Solution => _puzzle.Solution;

    /// <summary>
    /// Уровень сложности головоломки.
    /// </summary>
    public Difficulty Difficulty => _puzzle.Difficulty;

    /// <summary>
    /// Режим игры, которому принадлежит сессия.
    /// </summary>
    /// <remarks>
    /// Режим задаётся при создании и не меняется: это исключает случайный запуск
    /// одного режима вместо другого.
    /// </remarks>
    public GameMode Mode => _mode;

    /// <summary>
    /// Выбранная ячейка либо <c>null</c>, если ячейка не выбрана.
    /// </summary>
    public CellPosition? SelectedCell => _selectedCell;

    /// <summary>
    /// Признак полностью заполненного поля (все ячейки содержат значение).
    /// </summary>
    public bool IsComplete => _state.IsComplete;

    /// <summary>
    /// Признак победы: поле заполнено полностью и каждое значение совпадает с решением.
    /// </summary>
    public bool IsSolved => _state.IsComplete && _state.Equals(_puzzle.Solution);

    /// <summary>
    /// Количество ходов игрока (ввод или стирание значения).
    /// </summary>
    public int MoveCount => _moveCount;

    /// <summary>
    /// Количество применённых подсказок.
    /// </summary>
    public int HintCount => _hintCount;

    /// <summary>
    /// Снимки состояния всех ячеек поля.
    /// </summary>
    public IReadOnlyList<Cell> Cells
    {
        get
        {
            var cells = new Cell[Grid.CellCount];

            for (int index = 0; index < Grid.CellCount; index++)
            {
                cells[index] = CreateCell(index);
            }

            return cells;
        }
    }

    /// <summary>
    /// Выбирает ячейку.
    /// </summary>
    /// <param name="position">Позиция ячейки.</param>
    public void SelectCell(CellPosition position) => _selectedCell = position;

    /// <summary>
    /// Выбирает ячейку по линейному индексу.
    /// </summary>
    /// <param name="index">Линейный индекс ячейки (0–80).</param>
    /// <exception cref="ArgumentOutOfRangeException">Индекс вне диапазона 0–80.</exception>
    public void SelectCell(int index) => _selectedCell = CellPosition.FromIndex(index);

    /// <summary>
    /// Снимает выбор ячейки.
    /// </summary>
    public void ClearSelection() => _selectedCell = null;

    /// <summary>
    /// Вводит значение в выбранную ячейку.
    /// </summary>
    /// <param name="digit">Вводимая цифра (1–9).</param>
    /// <returns>Результат действия.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Цифра вне диапазона 1–9.</exception>
    public MoveOutcome EnterValue(int digit)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(digit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(digit, Grid.Size);

        if (IsSolved)
        {
            return MoveOutcome.RejectedGameCompleted;
        }

        if (_selectedCell is null)
        {
            return MoveOutcome.RejectedNoSelection;
        }

        int index = _selectedCell.Value.Index;

        if (IsGiven(index))
        {
            return MoveOutcome.RejectedGivenCell;
        }

        if (_state[index] == digit)
        {
            return MoveOutcome.Unchanged;
        }

        SetValue(index, digit);
        _moveCount++;

        return MoveOutcome.Applied;
    }

    /// <summary>
    /// Стирает значение, введённое игроком, в выбранной ячейке.
    /// </summary>
    /// <returns>Результат действия.</returns>
    public MoveOutcome EraseValue()
    {
        if (IsSolved)
        {
            return MoveOutcome.RejectedGameCompleted;
        }

        if (_selectedCell is null)
        {
            return MoveOutcome.RejectedNoSelection;
        }

        int index = _selectedCell.Value.Index;

        if (IsGiven(index))
        {
            return MoveOutcome.RejectedGivenCell;
        }

        if (_state[index] == Grid.EmptyCell)
        {
            return MoveOutcome.RejectedEmptyCell;
        }

        SetValue(index, Grid.EmptyCell);
        _moveCount++;

        return MoveOutcome.Applied;
    }

    /// <summary>
    /// Запрашивает подсказку для выбранной ячейки.
    /// </summary>
    /// <remarks>
    /// Значение берётся из подтверждённого решения головоломки. Если ячейка не выбрана,
    /// сессия выбирает первую пустую ячейку самостоятельно. Исходная ячейка не изменяется —
    /// её правильное значение всё равно возвращается для показа.
    /// </remarks>
    /// <returns>Результат запроса подсказки.</returns>
    public HintResult RevealHint()
    {
        if (IsSolved)
        {
            return HintResult.Create(HintStatus.GameCompleted, _selectedCell, Grid.EmptyCell);
        }

        CellPosition position;

        if (_selectedCell is null)
        {
            CellPosition? first = FindFirstEmptyCell();
            if (first is null)
            {
                return HintResult.Create(HintStatus.NoEmptyCells, null, Grid.EmptyCell);
            }

            position = first.Value;
            _selectedCell = position;
        }
        else
        {
            position = _selectedCell.Value;
        }

        int index = position.Index;
        int value = _puzzle.Solution[index];

        if (IsGiven(index))
        {
            return HintResult.Create(HintStatus.AlreadyGiven, position, value);
        }

        bool wasEmpty = _state[index] == Grid.EmptyCell;

        SetValue(index, value);
        _hintCount++;

        return HintResult.Create(
            wasEmpty ? HintStatus.Revealed : HintStatus.Corrected,
            position,
            value);
    }

    /// <summary>
    /// Возвращает снимок состояния ячейки.
    /// </summary>
    /// <param name="index">Линейный индекс ячейки (0–80).</param>
    /// <returns>Снимок состояния ячейки.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Индекс вне диапазона 0–80.</exception>
    public Cell GetCell(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Grid.CellCount);

        return CreateCell(index);
    }

    /// <summary>
    /// Возвращает снимок состояния ячейки.
    /// </summary>
    /// <param name="position">Позиция ячейки.</param>
    /// <returns>Снимок состояния ячейки.</returns>
    public Cell GetCell(CellPosition position) => CreateCell(position.Index);

    /// <summary>
    /// Проверяет, конфликтует ли ячейка: её значение повторяется в строке, столбце или блоке.
    /// </summary>
    /// <param name="index">Линейный индекс ячейки (0–80).</param>
    /// <returns><c>true</c>, если ячейка конфликтует.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Индекс вне диапазона 0–80.</exception>
    public bool HasConflict(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Grid.CellCount);

        return _conflicts[index];
    }

    /// <summary>
    /// Проверяет, является ли ячейка исходной (заданной).
    /// </summary>
    /// <param name="index">Линейный индекс ячейки (0–80).</param>
    /// <returns><c>true</c>, если значение ячейки задано и не может быть изменено.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Индекс вне диапазона 0–80.</exception>
    public bool IsGiven(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Grid.CellCount);

        return _puzzle.Clues[index] != Grid.EmptyCell;
    }

    private Cell CreateCell(int index)
    {
        int value = _state[index];

        return new Cell(
            CellPosition.FromIndex(index),
            value,
            IsGiven(index),
            _selectedCell is not null && _selectedCell.Value.Index == index,
            _conflicts[index],
            IsCorrectValue(index));
    }

    private bool IsCorrectValue(int index)
    {
        int value = _state[index];

        return value == Grid.EmptyCell || value == _puzzle.Solution[index];
    }

    private void SetValue(int index, int value)
    {
        int[] cells = new int[Grid.CellCount];
        for (int cell = 0; cell < Grid.CellCount; cell++)
        {
            cells[cell] = _state[cell];
        }

        cells[index] = value;
        _state = Grid.FromCells(cells);
        _conflicts = ConflictScanner.Compute(_state);
    }

    private CellPosition? FindFirstEmptyCell()
    {
        for (int index = 0; index < Grid.CellCount; index++)
        {
            if (_state[index] == Grid.EmptyCell)
            {
                return CellPosition.FromIndex(index);
            }
        }

        return null;
    }

    private static void Validate(Puzzle puzzle)
    {
        if (!puzzle.Solution.IsComplete)
        {
            throw new ArgumentException("Решение головоломки неполное.", nameof(puzzle));
        }

        if (!puzzle.Solution.IsConsistent)
        {
            throw new ArgumentException("Решение головоломки противоречиво.", nameof(puzzle));
        }

        if (!puzzle.Clues.IsConsistent)
        {
            throw new ArgumentException("Исходные значения головоломки противоречивы.", nameof(puzzle));
        }

        for (int index = 0; index < Grid.CellCount; index++)
        {
            int given = puzzle.Clues[index];

            if (given != Grid.EmptyCell && given != puzzle.Solution[index])
            {
                throw new ArgumentException(
                    $"Исходное значение в ячейке {index} не совпадает с решением.",
                    nameof(puzzle));
            }
        }
    }
}
