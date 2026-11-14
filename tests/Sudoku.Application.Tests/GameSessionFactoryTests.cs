using Sudoku.Application;
using Sudoku.Domain;
using Xunit;

namespace Sudoku.Tests.Application;

public class GameSessionFactoryTests
{
    private readonly ISudokuSolver _solver = new MrvSudokuSolver();
    private readonly IPuzzleGenerator _generator = new PuzzleGenerator(
        new MrvSudokuSolver(),
        new LogicalDifficultyRater(new MrvSudokuSolver()));
    private readonly IGameSessionFactory _factory = new GameSessionFactory(
        new PuzzleGenerator(
            new MrvSudokuSolver(),
            new LogicalDifficultyRater(new MrvSudokuSolver())));

    [Theory]
    [InlineData(Difficulty.Easy)]
    [InlineData(Difficulty.Normal)]
    [InlineData(Difficulty.Hard)]
    public void New_game_starts_with_given_values_of_generated_puzzle(Difficulty difficulty)
    {
        var session = _factory.CreateNewEndlessGame(difficulty, seed: 101);

        Assert.Equal(difficulty, session.Difficulty);
        Assert.Equal(session.Puzzle.Clues, session.InitialState);
        Assert.Equal(session.InitialState, session.State);
        Assert.True(session.Solution.IsComplete);
        Assert.True(session.Solution.IsConsistent);
        Assert.Equal(1, _solver.CountSolutions(session.InitialState, 2));
        Assert.False(session.IsSolved);
        Assert.DoesNotContain(session.Cells, cell => cell.IsUserValue);
    }

    [Theory]
    [InlineData(Difficulty.Easy)]
    [InlineData(Difficulty.Normal)]
    [InlineData(Difficulty.Hard)]
    public void New_game_is_reproducible_for_the_same_seed(Difficulty difficulty)
    {
        var first = _factory.CreateNewEndlessGame(difficulty, seed: 202);
        var second = _factory.CreateNewEndlessGame(difficulty, seed: 202);

        Assert.Equal(first.InitialState, second.InitialState);
        Assert.Equal(first.Solution, second.Solution);
    }

    [Fact]
    public void New_game_differs_for_different_seeds()
    {
        var first = _factory.CreateNewEndlessGame(Difficulty.Normal, seed: 1);
        var second = _factory.CreateNewEndlessGame(Difficulty.Normal, seed: 2);

        Assert.NotEqual(first.InitialState, second.InitialState);
    }

    [Fact]
    public void New_game_starts_independent_session()
    {
        var first = _factory.CreateNewEndlessGame(Difficulty.Easy, seed: 5);
        var second = _factory.CreateNewEndlessGame(Difficulty.Easy, seed: 5);

        first.SelectCell(0);
        first.EnterValue(1);

        Assert.Null(second.SelectedCell);
        Assert.Equal(second.InitialState, second.State);
    }

    [Fact]
    public void Playing_generated_game_to_the_end_produces_a_win()
    {
        var session = _factory.CreateNewEndlessGame(Difficulty.Easy, seed: 7);

        for (int index = 0; index < Grid.CellCount; index++)
        {
            if (!session.GetCell(index).IsEmpty)
            {
                continue;
            }

            session.SelectCell(index);
            session.EnterValue(session.Solution[index]);
        }

        Assert.True(session.IsComplete);
        Assert.True(session.IsSolved);
        Assert.Equal(0, session.Cells.Count(cell => cell.HasConflict));
    }

    [Fact]
    public void Hint_of_generated_game_matches_puzzle_solution()
    {
        var session = _factory.CreateNewEndlessGame(Difficulty.Normal, seed: 11);

        var emptyIndex = Enumerable.Range(0, Grid.CellCount)
            .First(index => session.GetCell(index).IsEmpty);

        session.SelectCell(emptyIndex);
        var hint = session.RevealHint();

        Assert.True(hint.Applied);
        Assert.Equal(session.Puzzle.Solution[emptyIndex], hint.Value);
        Assert.Equal(session.Puzzle.Solution[emptyIndex], session.State[emptyIndex]);
    }

    [Fact]
    public void Factory_requires_generator()
    {
        Assert.Throws<ArgumentNullException>(() => new GameSessionFactory(null!));
    }

    [Fact]
    public void Factory_rejects_unknown_difficulty()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _factory.CreateNewEndlessGame((Difficulty)42, seed: 1));
    }
}
