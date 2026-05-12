namespace Wolfgang.Conflict.Nes.Engine.Combat;

/// <summary>
/// Deterministic <see cref="IRandomSource"/> backed by a seeded
/// <see cref="System.Random"/>. Two instances created with the same seed
/// produce identical sequences.
/// </summary>
public sealed class SeededRandomSource : IRandomSource
{
    private readonly Random _random;

    /// <summary>Creates a new source from <paramref name="seed"/>.</summary>
    public SeededRandomSource(int seed)
    {
        _random = new Random(seed);
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="minInclusive"/> exceeds <paramref name="maxInclusive"/>.
    /// </exception>
    public int NextInt(int minInclusive, int maxInclusive)
    {
        if (minInclusive > maxInclusive)
        {
            throw new ArgumentOutOfRangeException(nameof(minInclusive), "min must be <= max.");
        }

        return _random.Next(minInclusive, maxInclusive + 1);
    }
}
