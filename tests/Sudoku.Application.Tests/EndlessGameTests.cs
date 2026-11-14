using System.Reflection;
using Sudoku.Application;
using Sudoku.Domain;
using Xunit;

namespace Sudoku.Tests.Application;

public class EndlessGameTests
{
    private readonly IGameSessionFactory _factory = new GameSessionFactory(
        new PuzzleGenerator(
            new MrvSudokuSolver(),
            new LogicalDifficultyRater(new MrvSudokuSolver())));

    [Theory]
    [InlineData(Difficulty.Easy)]
    [InlineData(Difficulty.Normal)]
    [InlineData(Difficulty.Hard)]
    public void New_endless_game_uses_requested_difficulty(Difficulty difficulty)
    {
        var game = new EndlessGame(_factory, difficulty, seed: 11);

        Assert.Equal(difficulty, game.Difficulty);
        Assert.Equal(difficulty, game.Session.Difficulty);
        Assert.Equal(1, game.PuzzleNumber);
        Assert.Equal(0, game.SolvedPuzzleCount);
        Assert.Equal(0, game.HintCount);
        Assert.False(game.IsCurrentPuzzleSolved);
    }

    [Fact]
    public void New_endless_game_creates_first_puzzle_with_unique_solution()
    {
        var game = new EndlessGame(_factory, Difficulty.Normal, seed: 12);

        Assert.Equal(1, new MrvSudokuSolver().CountSolutions(game.Session.InitialState, 2));
        Assert.True(game.Session.Solution.IsComplete);
        Assert.True(game.Session.Solution.IsConsistent);
        Assert.NotEmpty(game.Cells);
        Assert.Equal(Grid.CellCount, game.Cells.Count);
    }

    [Fact]
    public void Endless_game_never_runs_another_mode()
    {
        var game = new EndlessGame(_factory, Difficulty.Normal, seed: 13);

        for (int attempt = 0; attempt < 5; attempt++)
        {
            Assert.Equal(GameMode.Endless, game.Mode);
            Assert.Equal(GameMode.Endless, game.Session.Mode);
            game.StartNextPuzzle();
        }
    }

    [Fact]
    public void Session_factory_exposes_no_other_mode()
    {
        var methods = typeof(IGameSessionFactory)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(method => method.Name)
            .ToArray();

        Assert.Equal(new[] { "CreateNewEndlessGame" }, methods);
    }

    [Fact]
    public void Endless_game_has_no_time_limit()
    {
        var timeLike = typeof(EndlessGame)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.PropertyType == typeof(TimeSpan)
                || property.PropertyType == typeof(DateTime)
                || property.PropertyType == typeof(DateTimeOffset)
                || property.PropertyType.Name.Contains("Time", StringComparison.Ordinal))
            .Select(property => property.Name)
            .ToArray();

        Assert.Empty(timeLike);
    }

    [Fact]
    public void Start_next_puzzle_starts_new_puzzle_of_same_difficulty()
    {
        var game = new EndlessGame(_factory, Difficulty.Hard, seed: 21);
        var firstClues = game.Session.InitialState;

        game.StartNextPuzzle();

        Assert.Equal(2, game.PuzzleNumber);
        Assert.Equal(Difficulty.Hard, game.Difficulty);
        Assert.Equal(Difficulty.Hard, game.Session.Difficulty);
        Assert.NotEqual(firstClues, game.Session.InitialState);
        Assert.Equal(0, game.SolvedPuzzleCount);
    }

    [Fact]
    public void Solved_puzzle_is_counted_when_next_puzzle_starts()
    {
        var game = new EndlessGame(_factory, Difficulty.Easy, seed: 22);

        SolveCurrentPuzzle(game);
        Assert.True(game.IsCurrentPuzzleSolved);

        game.StartNextPuzzle();

        Assert.Equal(1, game.SolvedPuzzleCount);
        Assert.False(game.IsCurrentPuzzleSolved);
    }

    [Fact]
    public void Abandoned_puzzle_is_not_counted()
    {
        var game = new EndlessGame(_factory, Difficulty.Easy, seed: 23);

        game.SelectCell(new CellPosition(0, 0));
        game.EnterValue(1);
        game.StartNextPuzzle();

        Assert.Equal(0, game.SolvedPuzzleCount);
    }

    [Fact]
    public void Win_is_detected_by_common_game_logic()
    {
        var game = new EndlessGame(_factory, Difficulty.Easy, seed: 24);

        Assert.Equal(game.Session.IsSolved, game.IsCurrentPuzzleSolved);

        SolveCurrentPuzzle(game);

        Assert.True(game.Session.IsSolved);
        Assert.True(game.IsCurrentPuzzleSolved);
    }

    [Fact]
    public void Hints_are_unlimited_and_use_confirmed_solution()
    {
        var game = new EndlessGame(_factory, Difficulty.Normal, seed: 25);

        for (int attempt = 0; attempt < 12; attempt++)
        {
            var hint = game.RequestHint();

            Assert.True(hint.Applied);
            Assert.Equal(game.Session.Solution[hint.Position!.Value.Index], hint.Value);
        }

        Assert.Equal(12, game.HintCount);
    }

    [Fact]
    public void Hint_counters_accumulate_across_puzzles()
    {
        var game = new EndlessGame(_factory, Difficulty.Easy, seed: 26);

        game.RequestHint();
        game.StartNextPuzzle();
        game.RequestHint();

        Assert.Equal(2, game.HintCount);
        Assert.Equal(1, game.Session.HintCount);
    }

    [Fact]
    public void Endless_run_is_reproducible_for_the_same_seed()
    {
        var first = new EndlessGame(_factory, Difficulty.Normal, seed: 31);
        var second = new EndlessGame(_factory, Difficulty.Normal, seed: 31);

        Assert.Equal(first.Session.InitialState, second.Session.InitialState);

        first.StartNextPuzzle();
        second.StartNextPuzzle();

        Assert.Equal(first.Session.InitialState, second.Session.InitialState);
        Assert.Equal(first.PuzzleNumber, second.PuzzleNumber);
    }

    [Fact]
    public void Different_seeds_produce_different_runs()
    {
        var first = new EndlessGame(_factory, Difficulty.Normal, seed: 41);
        var second = new EndlessGame(_factory, Difficulty.Normal, seed: 42);

        Assert.NotEqual(first.Session.InitialState, second.Session.InitialState);
    }

    [Fact]
    public void Endless_game_requires_factory()
    {
        Assert.Throws<ArgumentNullException>(() => new EndlessGame(null!, Difficulty.Easy, seed: 1));
    }

    [Fact]
    public void Endless_game_rejects_unknown_difficulty()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EndlessGame(_factory, (Difficulty)42, seed: 1));
    }

    private static void SolveCurrentPuzzle(EndlessGame game)
    {
        for (int index = 0; index < Grid.CellCount; index++)
        {
            if (!game.Session.GetCell(index).IsEmpty)
            {
                continue;
            }

            game.SelectCell(CellPosition.FromIndex(index));
            game.EnterValue(game.Session.Solution[index]);
        }
    }
}
