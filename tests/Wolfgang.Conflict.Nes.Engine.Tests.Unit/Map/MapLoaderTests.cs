using Wolfgang.Conflict.Nes.Engine.Hex;
using Wolfgang.Conflict.Nes.Engine.Map;
using Wolfgang.Conflict.Nes.Engine.Players;
using Xunit;

namespace Wolfgang.Conflict.Nes.Engine.Tests.Map;

public class MapLoaderTests
{
    private const string TinyJson =
        """
        {
          "name": "tiny",
          "width": 2,
          "height": 1,
          "tiles": [
            { "q": 0, "r": 0, "terrain": "Plains" },
            { "q": 1, "r": 0, "terrain": "Forest", "building": "City", "owner": "Blue" }
          ]
        }
        """;

    [Fact]
    public void LoadFromJson_parses_tiny_map()
    {
        var map = MapLoader.LoadFromJson(TinyJson);

        Assert.Equal("tiny", map.Name);
        Assert.Equal(2, map.Width);
        Assert.Equal(1, map.Height);
        Assert.Equal(2, map.Tiles.Count);

        var t0 = map.Tiles[new HexCoord(0, 0)];
        Assert.Equal(Terrain.Plains, t0.Terrain);
        Assert.Null(t0.Building);
        Assert.Null(t0.Owner);

        var t1 = map.Tiles[new HexCoord(1, 0)];
        Assert.Equal(Terrain.Forest, t1.Terrain);
        Assert.Equal(BuildingKind.City, t1.Building);
        Assert.Equal(Side.Blue, t1.Owner);
    }

    [Fact]
    public void LoadFromJson_null_throws()
    {
        Assert.Throws<ArgumentNullException>(() => MapLoader.LoadFromJson(null!));
    }

    [Fact]
    public void LoadFromJson_malformed_throws()
    {
        Assert.Throws<ArgumentException>(() => MapLoader.LoadFromJson("{ not json"));
    }

    [Fact]
    public async Task LoadEmbeddedAsync_loads_mission01()
    {
        var map = await MapLoader.LoadEmbeddedAsync("Wolfgang.Conflict.Nes.Engine.Maps.mission01.json");

        Assert.Equal(12, map.Width);
        Assert.Equal(10, map.Height);
        Assert.Equal(120, map.Tiles.Count);
    }

    [Fact]
    public async Task LoadEmbeddedAsync_missing_resource_throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => MapLoader.LoadEmbeddedAsync("Wolfgang.Conflict.Nes.Engine.Maps.does_not_exist.json"));
    }

    [Fact]
    public async Task Mission01_has_one_HQ_per_side()
    {
        var map = await MapLoader.LoadEmbeddedAsync("Wolfgang.Conflict.Nes.Engine.Maps.mission01.json");

        var hqs = map.Tiles.Values.Where(t => t.Building == BuildingKind.Hq).ToList();

        Assert.Equal(2, hqs.Count);
        Assert.Contains(hqs, t => t.Owner == Side.Blue);
        Assert.Contains(hqs, t => t.Owner == Side.Red);
    }
}
