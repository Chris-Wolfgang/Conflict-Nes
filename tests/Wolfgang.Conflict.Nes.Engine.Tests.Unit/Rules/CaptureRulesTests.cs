using Wolfgang.Conflict.Nes.Engine.Game;
using Wolfgang.Conflict.Nes.Engine.Hex;
using Wolfgang.Conflict.Nes.Engine.Map;
using Wolfgang.Conflict.Nes.Engine.Players;
using Wolfgang.Conflict.Nes.Engine.Rules;
using Wolfgang.Conflict.Nes.Engine.Units;
using Xunit;

namespace Wolfgang.Conflict.Nes.Engine.Tests.Rules;

public class CaptureRulesTests
{
    [Theory]
    [InlineData(BuildingKind.City, true)]
    [InlineData(BuildingKind.Airbase, true)]
    [InlineData(BuildingKind.Port, true)]
    [InlineData(BuildingKind.Hq, false)]
    [InlineData(BuildingKind.Factory, false)]
    public void IsCapturable_matches_manual_rules(BuildingKind building, bool expected)
    {
        Assert.Equal(expected, CaptureRules.IsCapturable(building));
    }

    [Fact]
    public void ComputeFlips_returns_flip_for_infantry_on_neutral_city()
    {
        var cityCoord = new HexCoord(1, 0);
        var tiles = MapDefinition.EnumerateCoords(3, 1)
            .Select(c => c == cityCoord
                ? new Tile(c, Terrain.Plains, Building: BuildingKind.City, Owner: null)
                : new Tile(c, Terrain.Plains, Building: null, Owner: null));
        var map = new MapDefinition("city", 3, 1, tiles);
        var infantry = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Infantry, cityCoord);
        var state = new GameState(map, TestCatalog.Catalog,
            new Dictionary<UnitId, Unit> { [infantry.Id] = infantry },
            new Dictionary<HexCoord, Side>(),
            Side.Blue, turnNumber: 1, phase: GamePhase.PlayerTurn,
            new Dictionary<Side, int> { [Side.Blue] = 0, [Side.Red] = 0 },
            winner: null, randomSeed: 1);

        var flips = CaptureRules.ComputeFlips(state, Side.Blue);

        Assert.Single(flips);
        Assert.Equal(Side.Blue, flips[cityCoord]);
    }

    [Fact]
    public void ComputeFlips_does_not_flip_when_infantry_already_owns_building()
    {
        var cityCoord = new HexCoord(1, 0);
        var tiles = MapDefinition.EnumerateCoords(3, 1)
            .Select(c => c == cityCoord
                ? new Tile(c, Terrain.Plains, BuildingKind.City, Side.Blue)
                : new Tile(c, Terrain.Plains, Building: null, Owner: null));
        var map = new MapDefinition("city", 3, 1, tiles);
        var infantry = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Infantry, cityCoord);
        var state = new GameState(map, TestCatalog.Catalog,
            new Dictionary<UnitId, Unit> { [infantry.Id] = infantry },
            new Dictionary<HexCoord, Side>(),
            Side.Blue, turnNumber: 1, phase: GamePhase.PlayerTurn,
            new Dictionary<Side, int> { [Side.Blue] = 0, [Side.Red] = 0 },
            winner: null, randomSeed: 1);

        Assert.Empty(CaptureRules.ComputeFlips(state, Side.Blue));
    }

    [Fact]
    public void ComputeFlips_any_unit_can_capture_a_city()
    {
        // Per the original game, any unit captures — not just infantry.
        var cityCoord = new HexCoord(1, 0);
        var tiles = MapDefinition.EnumerateCoords(3, 1)
            .Select(c => c == cityCoord
                ? new Tile(c, Terrain.Plains, Building: BuildingKind.City, Owner: null)
                : new Tile(c, Terrain.Plains, Building: null, Owner: null));
        var map = new MapDefinition("city", 3, 1, tiles);
        var tank = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Tank, cityCoord);
        var state = new GameState(map, TestCatalog.Catalog,
            new Dictionary<UnitId, Unit> { [tank.Id] = tank },
            new Dictionary<HexCoord, Side>(),
            Side.Blue, turnNumber: 1, phase: GamePhase.PlayerTurn,
            new Dictionary<Side, int> { [Side.Blue] = 0, [Side.Red] = 0 },
            winner: null, randomSeed: 1);

        var flips = CaptureRules.ComputeFlips(state, Side.Blue);

        Assert.Single(flips);
        Assert.Equal(Side.Blue, flips[cityCoord]);
    }
}
