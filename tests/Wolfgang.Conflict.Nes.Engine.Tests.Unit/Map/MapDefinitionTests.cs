using Wolfgang.Conflict.Nes.Engine.Hex;
using Wolfgang.Conflict.Nes.Engine.Map;
using Xunit;

namespace Wolfgang.Conflict.Nes.Engine.Tests.Map;

public class MapDefinitionTests
{
    private static IEnumerable<Tile> AllPlains(int width, int height) =>
        MapDefinition.EnumerateCoords(width, height)
            .Select(c => new Tile(c, Terrain.Plains, Building: null, Owner: null));

    [Fact]
    public void Constructor_with_complete_tile_set_succeeds()
    {
        var map = new MapDefinition("test", 4, 3, AllPlains(4, 3));

        Assert.Equal("test", map.Name);
        Assert.Equal(4, map.Width);
        Assert.Equal(3, map.Height);
        Assert.Equal(12, map.Tiles.Count);
    }

    [Fact]
    public void Contains_accepts_offset_rectangular_coords()
    {
        var map = new MapDefinition("test", 4, 3, AllPlains(4, 3));

        Assert.True(map.Contains(new HexCoord(0, 0)));
        Assert.True(map.Contains(new HexCoord(1, 2)));
        Assert.True(map.Contains(new HexCoord(2, -1)));
        Assert.True(map.Contains(new HexCoord(3, 1)));
    }

    [Fact]
    public void Contains_rejects_coords_outside_rectangular_bounds()
    {
        var map = new MapDefinition("test", 4, 3, AllPlains(4, 3));

        Assert.False(map.Contains(new HexCoord(-1, 0)));
        Assert.False(map.Contains(new HexCoord(4, 0)));
        Assert.False(map.Contains(new HexCoord(0, -1)));
        Assert.False(map.Contains(new HexCoord(0, 3)));
        Assert.False(map.Contains(new HexCoord(2, -2)));
        Assert.False(map.Contains(new HexCoord(2, 2)));
    }

    [Fact]
    public void Constructor_rejects_tile_outside_bounds()
    {
        var bad = AllPlains(4, 3).Concat(new[] { new Tile(new HexCoord(5, 0), Terrain.Plains, Building: null, Owner: null) });

        Assert.Throws<ArgumentException>(() => new MapDefinition("test", 4, 3, bad));
    }

    [Fact]
    public void Constructor_rejects_duplicate_coords()
    {
        var dup = AllPlains(4, 3).ToList();
        dup.Add(new Tile(dup[0].Coord, Terrain.Plains, Building: null, Owner: null));

        Assert.Throws<ArgumentException>(() => new MapDefinition("test", 4, 3, dup));
    }

    [Fact]
    public void Constructor_rejects_missing_coords()
    {
        var missing = AllPlains(4, 3).Skip(1);

        Assert.Throws<ArgumentException>(() => new MapDefinition("test", 4, 3, missing));
    }

    [Fact]
    public void Constructor_rejects_nonpositive_dimensions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MapDefinition("test", 0, 3, []));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MapDefinition("test", 4, 0, []));
    }

    [Theory]
    [InlineData(4, 3, 12)]
    [InlineData(12, 10, 120)]
    [InlineData(1, 1, 1)]
    public void EnumerateCoords_yields_width_times_height_unique_coords(int width, int height, int expected)
    {
        var coords = MapDefinition.EnumerateCoords(width, height).ToList();

        Assert.Equal(expected, coords.Count);
        Assert.Equal(expected, coords.Distinct().Count());
    }
}
