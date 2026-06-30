using Wolfgang.Conflict.Classic.Engine.Combat;
using Xunit;

namespace Wolfgang.Conflict.Classic.Engine.Tests.Combat;

public class SeededRandomSourceTests
{
    [Fact]
    public void Same_seed_produces_same_sequence()
    {
        var a = new SeededRandomSource(42);
        var b = new SeededRandomSource(42);

        for (var i = 0; i < 50; i++)
        {
            Assert.Equal(a.NextInt(-2, 2), b.NextInt(-2, 2));
        }
    }

    [Fact]
    public void NextInt_returns_value_in_inclusive_range()
    {
        var rng = new SeededRandomSource(1);

        for (var i = 0; i < 200; i++)
        {
            var v = rng.NextInt(-2, 2);
            Assert.InRange(v, -2, 2);
        }
    }

    [Fact]
    public void NextInt_throws_when_min_exceeds_max()
    {
        var rng = new SeededRandomSource(1);

        Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(5, 3));
    }
}
