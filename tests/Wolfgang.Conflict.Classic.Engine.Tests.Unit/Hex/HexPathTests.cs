using Wolfgang.Conflict.Classic.Engine.Hex;
using Xunit;

namespace Wolfgang.Conflict.Classic.Engine.Tests.Hex;

public class HexPathTests
{
    [Fact]
    public void Constructor_records_start_destination_and_cost()
    {
        var hexes = new[]
        {
            new HexCoord(0, 0),
            new HexCoord(1, 0),
            new HexCoord(2, 0),
        };

        var path = new HexPath(hexes, totalCost: 5);

        Assert.Equal(new HexCoord(0, 0), path.Start);
        Assert.Equal(new HexCoord(2, 0), path.Destination);
        Assert.Equal(3, path.Count);
        Assert.Equal(5, path.TotalCost);
    }

    [Fact]
    public void Indexer_returns_hex_at_position()
    {
        var path = new HexPath(new[] { new HexCoord(0, 0), new HexCoord(1, 0) }, 1);

        Assert.Equal(new HexCoord(1, 0), path[1]);
    }

    [Fact]
    public void Enumeration_preserves_order()
    {
        var hexes = new[]
        {
            new HexCoord(0, 0),
            new HexCoord(1, 0),
            new HexCoord(1, 1),
        };

        var path = new HexPath(hexes, 2);

        Assert.Equal(hexes, path.ToArray());
    }

    [Fact]
    public void Constructor_null_hexes_throws()
    {
        Assert.Throws<ArgumentNullException>(() => new HexPath(null!, 0));
    }

    [Fact]
    public void Constructor_empty_hexes_throws()
    {
        Assert.Throws<ArgumentException>(() => new HexPath(Array.Empty<HexCoord>(), 0));
    }

    [Fact]
    public void Constructor_negative_cost_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HexPath(new[] { HexCoord.Zero }, -1));
    }
}
