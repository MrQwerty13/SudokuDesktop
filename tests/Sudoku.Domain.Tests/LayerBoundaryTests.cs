using Sudoku.Domain;
using Xunit;

namespace Sudoku.Tests.Domain;

public class LayerBoundaryTests
{
    [Fact]
    public void Domain_does_not_depend_on_ui_or_other_layers()
    {
        var referenced = typeof(Difficulty).Assembly
            .GetReferencedAssemblies()
            .Select(name => name.Name!)
            .ToArray();

        Assert.DoesNotContain(referenced, name => name.StartsWith("Avalonia", StringComparison.Ordinal));
        Assert.DoesNotContain(referenced, name => name.StartsWith("Sudoku.Application", StringComparison.Ordinal));
        Assert.DoesNotContain(referenced, name => name.StartsWith("Sudoku.Infrastructure", StringComparison.Ordinal));
        Assert.DoesNotContain(referenced, name => name.StartsWith("Sudoku.App", StringComparison.Ordinal));
    }
}
