namespace Sudoku.Application;

/// <summary>
/// Источник случайных чисел поверх <see cref="System.Random"/> с явным seed.
/// </summary>
/// <remarks>
/// Воспроизводимость гарантируется в пределах одной версии .NET: алгоритм
/// <see cref="System.Random"/> может измениться между версиями среды.
/// </remarks>
public sealed class SystemRandomSource : IRandomSource
{
    private readonly Random _random;

    /// <summary>
    /// Создаёт источник случайных чисел с заданным seed.
    /// </summary>
    /// <param name="seed">Seed генератора.</param>
    public SystemRandomSource(int seed)
    {
        _random = new Random(seed);
    }

    /// <inheritdoc />
    public int Next(int exclusiveUpperBound)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(exclusiveUpperBound, 1);

        return _random.Next(exclusiveUpperBound);
    }
}
