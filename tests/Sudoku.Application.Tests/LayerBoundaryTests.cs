using Sudoku.Application;
using Xunit;

namespace Sudoku.Tests.Application;

public class LayerBoundaryTests
{
    [Fact]
    public void Application_depends_on_domain_only()
    {
        var referenced = typeof(GameOptions).Assembly
            .GetReferencedAssemblies()
            .Select(name => name.Name!)
            .ToArray();

        Assert.DoesNotContain(referenced, name => name.StartsWith("Avalonia", StringComparison.Ordinal));
        Assert.DoesNotContain(referenced, name => name.StartsWith("Sudoku.Infrastructure", StringComparison.Ordinal));
        Assert.DoesNotContain(referenced, name => name.StartsWith("Sudoku.App", StringComparison.Ordinal));
        Assert.Contains("Sudoku.Domain", referenced);
    }
}
