using Sudoku.Domain;
using Xunit;

namespace Sudoku.Tests.Domain;

public class GridTests
{
    private const string Valid = "534678912672195348198342567859761423426853791713924856961537284287419635345286179";

    [Fact]
    public void FromString_parses_digits_and_empty_cells()
    {
        var grid = Grid.FromString("53..7....6..195....98....6.8...6...34..8.3..17...2...6.6....28....419..5....8..79");

        Assert.Equal(5, grid[0]);
        Assert.Equal(3, grid[1]);
        Assert.Equal(Grid.EmptyCell, grid[2]);
        Assert.Equal(Grid.EmptyCell, grid[3]);
        Assert.Equal(7, grid[4]);
        Assert.Equal(9, grid[80]);
    }

    [Fact]
    public void FromString_accepts_rows_separated_by_whitespace()
    {
        var grid = Grid.FromString(
            "534678912\n672195348 198342567\r\n859761423\n"
            + "426853791\n713924856\n961537284\n287419635\n345286179");

        Assert.Equal(Valid, grid.ToString());
    }

    [Fact]
    public void FromString_rejects_short_input()
    {
        var exception = Assert.Throws<ArgumentException>(() => Grid.FromString("123"));

        Assert.Equal("text", exception.ParamName);
    }

    [Fact]
    public void FromString_rejects_long_input()
    {
        Assert.Throws<ArgumentException>(() => Grid.FromString(new string('1', 82)));
    }

    [Fact]
    public void FromString_rejects_unknown_symbols()
    {
        var exception = Assert.Throws<ArgumentException>(() => Grid.FromString(new string('x', 81)));

        Assert.Equal("text", exception.ParamName);
    }

    [Fact]
    public void FromString_rejects_null_input()
    {
        Assert.Throws<ArgumentNullException>(() => Grid.FromString(null!));
    }

    [Fact]
    public void TryFromString_reports_invalid_input_without_throwing()
    {
        Assert.False(Grid.TryFromString("123", out var grid));
        Assert.Null(grid);

        Assert.False(Grid.TryFromString(null, out var nullGrid));
        Assert.Null(nullGrid);

        Assert.True(Grid.TryFromString(Valid, out var validGrid));
        Assert.Equal(Valid, validGrid.ToString());
    }

    [Fact]
    public void FromCells_rejects_invalid_length_and_values()
    {
        Assert.Throws<ArgumentException>(() => Grid.FromCells(new int[80]));

        var cells = new int[Grid.CellCount];
        cells[0] = 10;
        Assert.Throws<ArgumentException>(() => Grid.FromCells(cells));

        var negative = new int[Grid.CellCount];
        negative[5] = -1;
        Assert.Throws<ArgumentException>(() => Grid.FromCells(negative));
    }

    [Fact]
    public void IsComplete_refers_to_filled_grid()
    {
        Assert.True(Grid.FromString(Valid).IsComplete);
        Assert.False(Grid.FromString("53" + new string('0', 79)).IsComplete);
    }

    [Fact]
    public void IsConsistent_detects_conflicting_digits()
    {
        Assert.True(Grid.FromString(Valid).IsConsistent);
        Assert.True(Grid.Empty.IsConsistent);
        Assert.False(Grid.FromString("55" + new string('0', 79)).IsConsistent);
    }

    [Fact]
    public void Indexer_rejects_out_of_range_coordinates()
    {
        var grid = Grid.Empty;

        Assert.Throws<ArgumentOutOfRangeException>(() => grid[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => grid[81]);
        Assert.Throws<ArgumentOutOfRangeException>(() => grid[9, 0]);
        Assert.Throws<ArgumentOutOfRangeException>(() => grid[0, 9]);
    }

    [Fact]
    public void Equality_compares_cell_values()
    {
        var first = Grid.FromString(Valid);
        var second = Grid.FromString(Valid);
        var different = Grid.FromString("53" + new string('0', 79));

        Assert.True(first.Equals(second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.False(first.Equals(different));
        Assert.False(first.Equals(null));
    }

    [Fact]
    public void ToString_round_trips_through_FromString()
    {
        var grid = Grid.FromString(Valid);

        var parsed = Grid.FromString(grid.ToString());

        Assert.Equal(grid, parsed);
    }
}
