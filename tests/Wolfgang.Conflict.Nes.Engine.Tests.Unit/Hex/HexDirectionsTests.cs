using Wolfgang.Conflict.Nes.Engine.Hex;
using Xunit;

namespace Wolfgang.Conflict.Nes.Engine.Tests.Hex;

public class HexDirectionsTests
{
    [Fact]
    public void All_contains_six_distinct_directions()
    {
        Assert.Equal(6, HexDirections.All.Count);
        Assert.Equal(6, HexDirections.All.Distinct().Count());
    }

    [Theory]
    [InlineData(HexDirection.East, 1, 0)]
    [InlineData(HexDirection.NorthEast, 1, -1)]
    [InlineData(HexDirection.NorthWest, 0, -1)]
    [InlineData(HexDirection.West, -1, 0)]
    [InlineData(HexDirection.SouthWest, -1, 1)]
    [InlineData(HexDirection.SouthEast, 0, 1)]
    public void Offset_returns_axial_delta(HexDirection direction, int dq, int dr)
    {
        var offset = HexDirections.Offset(direction);

        Assert.Equal((dq, dr), offset);
    }

    [Fact]
    public void Offset_unknown_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => HexDirections.Offset((HexDirection)42));
    }
}
