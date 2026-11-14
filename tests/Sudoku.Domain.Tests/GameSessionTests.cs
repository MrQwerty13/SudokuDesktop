using Sudoku.Domain;
using Xunit;

namespace Sudoku.Tests.Domain;

public class GameSessionTests
{
    private static GameSession CreateSession()
    {
        var clues = Grid.FromString(SessionTestData.Classic);
        var solution = Grid.FromString(SessionTestData.ClassicSolution);
        var rating = DifficultyRating.Solved(true, SudokuTechnique.NakedSingle, 30);

        return new GameSession(new Puzzle(clues, solution, Difficulty.Normal, rating), GameMode.Endless);
    }

    [Fact]
    public void New_session_contains_only_given_values()
    {
        var session = CreateSession();

        Assert.Equal(Grid.FromString(SessionTestData.Classic), session.InitialState);
        Assert.Equal(Grid.FromString(SessionTestData.Classic), session.State);
        Assert.Equal(Grid.FromString(SessionTestData.ClassicSolution), session.Solution);
        Assert.Equal(Difficulty.Normal, session.Difficulty);
        Assert.False(session.IsComplete);
        Assert.False(session.IsSolved);
        Assert.Equal(0, session.MoveCount);
        Assert.Equal(0, session.HintCount);
        Assert.Null(session.SelectedCell);
    }

    [Fact]
    public void New_session_has_no_user_values()
    {
        var session = CreateSession();

        Assert.All(session.Cells, cell => Assert.False(cell.IsUserValue));
        Assert.Equal(30, session.Cells.Count(cell => cell.IsGiven));
        Assert.Equal(51, session.Cells.Count(cell => cell.IsEmpty));
    }

    [Fact]
    public void Select_cell_marks_only_that_cell_as_selected()
    {
        var session = CreateSession();

        session.SelectCell(SessionTestData.EmptyRowZeroColumnTwo);

        Assert.NotNull(session.SelectedCell);
        Assert.Equal(SessionTestData.EmptyRowZeroColumnTwo, session.SelectedCell!.Value.Index);
        Assert.True(session.GetCell(SessionTestData.EmptyRowZeroColumnTwo).IsSelected);
        Assert.False(session.GetCell(SessionTestData.EmptyRowOneColumnOne).IsSelected);
    }

    [Fact]
    public void Value_conflicting_with_given_value_is_marked_as_conflict()
    {
        var session = CreateSession();

        session.SelectCell(SessionTestData.EmptyRowZeroColumnTwo);
        session.EnterValue(5);

        Assert.Equal(5, session.State[SessionTestData.EmptyRowZeroColumnTwo]);
        Assert.True(session.HasConflict(SessionTestData.EmptyRowZeroColumnTwo));
    }

    [Fact]
    public void Initial_state_is_not_affected_by_player_moves()
    {
        var session = CreateSession();

        session.SelectCell(SessionTestData.EmptyRowZeroColumnTwo);
        session.EnterValue(4);
        session.EraseValue();

        Assert.Equal(Grid.FromString(SessionTestData.Classic), session.InitialState);
        Assert.Equal(Grid.FromString(SessionTestData.Classic), session.Puzzle.Clues);
    }

    [Fact]
    public void Select_cell_by_index_and_position_selects_the_same_cell()
    {
        var session = CreateSession();

        session.SelectCell(SessionTestData.EmptyRowZeroColumnTwo);
        var byIndex = session.SelectedCell;

        session.ClearSelection();
        session.SelectCell(new CellPosition(0, 2));

        Assert.Equal(byIndex, session.SelectedCell);
    }

    [Fact]
    public void Enter_value_places_digit_into_selected_cell()
    {
        var session = CreateSession();

        session.SelectCell(SessionTestData.EmptyRowZeroColumnTwo);
        var outcome = session.EnterValue(4);

        Assert.Equal(MoveOutcome.Applied, outcome);
        Assert.Equal(4, session.State[SessionTestData.EmptyRowZeroColumnTwo]);
        Assert.Equal(1, session.MoveCount);

        var cell = session.GetCell(SessionTestData.EmptyRowZeroColumnTwo);
        Assert.True(cell.IsUserValue);
        Assert.False(cell.IsGiven);
        Assert.False(cell.IsEmpty);
        Assert.True(cell.IsCorrect);
    }

    [Fact]
    public void Enter_value_without_selection_is_rejected()
    {
        var session = CreateSession();

        Assert.Equal(MoveOutcome.RejectedNoSelection, session.EnterValue(4));
        Assert.Equal(0, session.MoveCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void Enter_value_rejects_digit_out_of_range(int digit)
    {
        var session = CreateSession();
        session.SelectCell(SessionTestData.EmptyRowZeroColumnTwo);

        Assert.Throws<ArgumentOutOfRangeException>(() => session.EnterValue(digit));
    }

    [Fact]
    public void Enter_value_in_given_cell_is_rejected()
    {
        var session = CreateSession();

        session.SelectCell(SessionTestData.GivenRowZeroColumnZero);
        var outcome = session.EnterValue(9);

        Assert.Equal(MoveOutcome.RejectedGivenCell, outcome);
        Assert.Equal(5, session.State[SessionTestData.GivenRowZeroColumnZero]);
        Assert.Equal(0, session.MoveCount);
    }

    [Fact]
    public void Enter_value_replaces_previous_user_value()
    {
        var session = CreateSession();

        session.SelectCell(SessionTestData.EmptyRowZeroColumnTwo);
        session.EnterValue(9);
        session.EnterValue(4);

        Assert.Equal(4, session.State[SessionTestData.EmptyRowZeroColumnTwo]);
        Assert.Equal(2, session.MoveCount);
    }

    [Fact]
    public void Enter_same_value_reports_unchanged_state()
    {
        var session = CreateSession();

        session.SelectCell(SessionTestData.EmptyRowZeroColumnTwo);
        session.EnterValue(4);
        var outcome = session.EnterValue(4);

        Assert.Equal(MoveOutcome.Unchanged, outcome);
        Assert.Equal(1, session.MoveCount);
    }

    [Fact]
    public void Erase_value_clears_user_value()
    {
        var session = CreateSession();

        session.SelectCell(SessionTestData.EmptyRowZeroColumnTwo);
        session.EnterValue(4);
        var outcome = session.EraseValue();

        Assert.Equal(MoveOutcome.Applied, outcome);
        Assert.Equal(Grid.EmptyCell, session.State[SessionTestData.EmptyRowZeroColumnTwo]);
        Assert.True(session.GetCell(SessionTestData.EmptyRowZeroColumnTwo).IsEmpty);
        Assert.Equal(2, session.MoveCount);
    }

    [Fact]
    public void Erase_value_in_given_cell_is_rejected()
    {
        var session = CreateSession();

        session.SelectCell(SessionTestData.GivenRowZeroColumnZero);

        Assert.Equal(MoveOutcome.RejectedGivenCell, session.EraseValue());
        Assert.Equal(5, session.State[SessionTestData.GivenRowZeroColumnZero]);
    }

    [Fact]
    public void Erase_value_on_empty_cell_is_rejected()
    {
        var session = CreateSession();

        session.SelectCell(SessionTestData.EmptyRowZeroColumnTwo);

        Assert.Equal(MoveOutcome.RejectedEmptyCell, session.EraseValue());
    }

    [Fact]
    public void Erase_value_without_selection_is_rejected()
    {
        var session = CreateSession();

        Assert.Equal(MoveOutcome.RejectedNoSelection, session.EraseValue());
    }

    [Fact]
    public void Conflicting_value_is_accepted_and_marked_as_conflict()
    {
        var session = CreateSession();

        session.SelectCell(SessionTestData.EmptyRowZeroColumnTwo);
        session.EnterValue(4);
        session.SelectCell(SessionTestData.EmptyRowOneColumnOne);
        session.EnterValue(4);

        Assert.Equal(4, session.State[SessionTestData.EmptyRowZeroColumnTwo]);
        Assert.Equal(4, session.State[SessionTestData.EmptyRowOneColumnOne]);
        Assert.True(session.HasConflict(SessionTestData.EmptyRowZeroColumnTwo));
        Assert.True(session.HasConflict(SessionTestData.EmptyRowOneColumnOne));
        Assert.True(session.GetCell(SessionTestData.EmptyRowZeroColumnTwo).HasConflict);
    }

    [Fact]
    public void Value_without_conflict_is_not_marked()
    {
        var session = CreateSession();

        session.SelectCell(SessionTestData.EmptyRowZeroColumnTwo);
        session.EnterValue(4);

        Assert.False(session.HasConflict(SessionTestData.EmptyRowZeroColumnTwo));
        Assert.False(session.GetCell(SessionTestData.EmptyRowZeroColumnTwo).HasConflict);
    }

    [Fact]
    public void Conflict_disappears_after_the_duplicate_is_erased()
    {
        var session = CreateSession();

        session.SelectCell(SessionTestData.EmptyRowZeroColumnTwo);
        session.EnterValue(4);
        session.SelectCell(SessionTestData.EmptyRowOneColumnOne);
        session.EnterValue(4);
        session.EraseValue();

        Assert.False(session.HasConflict(SessionTestData.EmptyRowZeroColumnTwo));
        Assert.False(session.HasConflict(SessionTestData.EmptyRowOneColumnOne));
    }

    [Fact]
    public void Hint_fills_empty_cell_with_confirmed_solution_value()
    {
        var session = CreateSession();

        session.SelectCell(SessionTestData.EmptyRowZeroColumnTwo);
        var hint = session.RevealHint();

        Assert.Equal(HintStatus.Revealed, hint.Status);
        Assert.True(hint.Applied);
        Assert.Equal(4, hint.Value);
        Assert.Equal(SessionTestData.EmptyRowZeroColumnTwo, hint.Position!.Value.Index);
        Assert.Equal(4, session.State[SessionTestData.EmptyRowZeroColumnTwo]);
        Assert.Equal(1, session.HintCount);
        Assert.True(session.GetCell(SessionTestData.EmptyRowZeroColumnTwo).IsCorrect);
    }

    [Fact]
    public void Hint_without_selection_chooses_empty_cell_by_itself()
    {
        var session = CreateSession();

        var hint = session.RevealHint();

        Assert.Equal(HintStatus.Revealed, hint.Status);
        Assert.NotNull(hint.Position);
        Assert.NotNull(session.SelectedCell);
        Assert.Equal(hint.Position, session.SelectedCell);
        Assert.Equal(session.Solution[hint.Position!.Value.Index], session.State[hint.Position.Value.Index]);
    }

    [Fact]
    public void Hint_on_given_cell_is_refused_but_reveals_value()
    {
        var session = CreateSession();

        session.SelectCell(SessionTestData.GivenRowZeroColumnZero);
        var hint = session.RevealHint();

        Assert.Equal(HintStatus.AlreadyGiven, hint.Status);
        Assert.False(hint.Applied);
        Assert.True(hint.RevealsValue);
        Assert.Equal(session.Solution[SessionTestData.GivenRowZeroColumnZero], hint.Value);
        Assert.Equal(5, session.State[SessionTestData.GivenRowZeroColumnZero]);
        Assert.Equal(0, session.HintCount);
    }

    [Fact]
    public void Hint_corrects_wrong_value_entered_by_player()
    {
        var session = CreateSession();

        session.SelectCell(SessionTestData.EmptyRowZeroColumnTwo);
        session.EnterValue(9);
        var hint = session.RevealHint();

        Assert.Equal(HintStatus.Corrected, hint.Status);
        Assert.True(hint.Applied);
        Assert.Equal(4, hint.Value);
        Assert.Equal(4, session.State[SessionTestData.EmptyRowZeroColumnTwo]);
        Assert.Equal(1, session.HintCount);
    }

    [Fact]
    public void Hint_uses_confirmed_solution_and_not_random_value()
    {
        var session = CreateSession();

        for (int index = 0; index < Grid.CellCount; index++)
        {
            if (session.GetCell(index).IsEmpty)
            {
                session.SelectCell(index);
                var hint = session.RevealHint();

                Assert.Equal(session.Solution[index], hint.Value);
                Assert.Equal(session.Solution[index], session.State[index]);
            }
        }
    }

    [Fact]
    public void Hints_are_unlimited()
    {
        var session = CreateSession();

        for (int attempt = 0; attempt < 10; attempt++)
        {
            var hint = session.RevealHint();

            Assert.True(hint.Applied);
        }

        Assert.Equal(10, session.HintCount);
    }

    [Fact]
    public void Session_is_not_solved_while_field_is_incomplete()
    {
        var session = CreateSession();

        session.SelectCell(SessionTestData.EmptyRowZeroColumnTwo);
        session.EnterValue(4);

        Assert.False(session.IsComplete);
        Assert.False(session.IsSolved);
    }

    [Fact]
    public void Session_is_solved_only_when_field_is_complete_and_correct()
    {
        var session = CreateSession();

        FillWithSolution(session);

        Assert.True(session.IsComplete);
        Assert.True(session.IsSolved);
    }

    [Fact]
    public void Complete_field_with_wrong_value_is_not_a_win()
    {
        var session = CreateSession();

        FillWithSolution(session, wrongCell: SessionTestData.EmptyRowZeroColumnTwo);

        Assert.True(session.IsComplete);
        Assert.False(session.IsSolved);
        Assert.False(session.GetCell(SessionTestData.EmptyRowZeroColumnTwo).IsCorrect);
    }

    [Fact]
    public void Moves_are_rejected_after_the_game_is_completed()
    {
        var session = CreateSession();

        FillWithSolution(session);

        session.SelectCell(SessionTestData.EmptyRowZeroColumnTwo);

        Assert.Equal(MoveOutcome.RejectedGameCompleted, session.EnterValue(1));
        Assert.Equal(MoveOutcome.RejectedGameCompleted, session.EraseValue());
        Assert.Equal(HintStatus.GameCompleted, session.RevealHint().Status);
        Assert.Equal(session.Solution[SessionTestData.EmptyRowZeroColumnTwo], session.State[SessionTestData.EmptyRowZeroColumnTwo]);
    }

    [Fact]
    public void Session_rejects_null_puzzle()
    {
        Assert.Throws<ArgumentNullException>(() => new GameSession(null!, GameMode.Endless));
    }

    [Fact]
    public void Session_rejects_puzzle_whose_clues_do_not_match_solution()
    {
        var clues = Grid.FromString(SessionTestData.Classic);

        // Решение остаётся корректным судоку (переставлены две цифры), но не совпадает
        // с исходными значениями — такая головоломка недопустима.
        var swapped = SessionTestData.ClassicSolution
            .Replace('5', '#')
            .Replace('7', '5')
            .Replace('#', '7');
        var broken = Grid.FromString(swapped);
        var rating = DifficultyRating.Solved(true, SudokuTechnique.NakedSingle, 30);

        var puzzle = new Puzzle(clues, broken, Difficulty.Normal, rating);

        Assert.Throws<ArgumentException>(() => new GameSession(puzzle, GameMode.Endless));
    }

    [Fact]
    public void Session_rejects_puzzle_with_incomplete_solution()
    {
        var clues = Grid.FromString(SessionTestData.Classic);
        var incomplete = Grid.FromString(SessionTestData.ClassicSolution[..40] + "0" + SessionTestData.ClassicSolution[41..]);
        var rating = DifficultyRating.Solved(true, SudokuTechnique.NakedSingle, 30);

        var puzzle = new Puzzle(clues, incomplete, Difficulty.Normal, rating);

        Assert.Throws<ArgumentException>(() => new GameSession(puzzle, GameMode.Endless));
    }

    [Fact]
    public void Get_cell_rejects_out_of_range_index()
    {
        var session = CreateSession();

        Assert.Throws<ArgumentOutOfRangeException>(() => session.GetCell(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => session.GetCell(Grid.CellCount));
        Assert.Throws<ArgumentOutOfRangeException>(() => session.HasConflict(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => session.IsGiven(Grid.CellCount));
    }

    private static void FillWithSolution(GameSession session, int wrongCell = -1)
    {
        for (int index = 0; index < Grid.CellCount; index++)
        {
            if (!session.GetCell(index).IsEmpty)
            {
                continue;
            }

            session.SelectCell(index);
            session.EnterValue(index == wrongCell ? 9 : session.Solution[index]);
        }
    }
}
