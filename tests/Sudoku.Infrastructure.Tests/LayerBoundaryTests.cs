using System.Reflection;
using Xunit;

namespace Sudoku.Tests.Infrastructure;

public class LayerBoundaryTests
{
    [Fact]
    public void Infrastructure_does_not_depend_on_ui()
    {
        var assembly = Assembly.Load("Sudoku.Infrastructure");

        var referenced = assembly
            .GetReferencedAssemblies()
            .Select(name => name.Name!)
            .ToArray();

        Assert.DoesNotContain(referenced, name => name.StartsWith("Avalonia", StringComparison.Ordinal));
        Assert.DoesNotContain(referenced, name => name.StartsWith("Sudoku.App", StringComparison.Ordinal));
    }
}
