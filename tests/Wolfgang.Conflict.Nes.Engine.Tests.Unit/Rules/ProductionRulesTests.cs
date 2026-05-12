using Wolfgang.Conflict.Nes.Engine.Game;
using Wolfgang.Conflict.Nes.Engine.Hex;
using Wolfgang.Conflict.Nes.Engine.Map;
using Wolfgang.Conflict.Nes.Engine.Players;
using Wolfgang.Conflict.Nes.Engine.Rules;
using Wolfgang.Conflict.Nes.Engine.Units;
using Xunit;

namespace Wolfgang.Conflict.Nes.Engine.Tests.Rules;

public class ProductionRulesTests
{
    [Theory]
    [InlineData(BuildingKind.Factory, UnitKind.Infantry)]
    [InlineData(BuildingKind.Factory, UnitKind.Tank)]
    [InlineData(BuildingKind.Airbase, UnitKind.Helicopter)]
    [InlineData(BuildingKind.Airbase, UnitKind.Fighter)]
    public void ProducibleAt_returns_matching_kinds(BuildingKind building, UnitKind expected)
    {
        Assert.Contains(expected, ProductionRules.ProducibleAt(building));
    }

    [Theory]
    [InlineData(BuildingKind.City)]
    [InlineData(BuildingKind.Hq)]
    [InlineData(BuildingKind.Port)]
    public void ProducibleAt_for_non_production_buildings_is_empty(BuildingKind building)
    {
        Assert.Empty(ProductionRules.ProducibleAt(building));
    }

    [Fact]
    public void CanSideProduce_true_when_a_unit_has_not_moved()
    {
        var tile = new Tile(HexCoord.Zero, Terrain.Plains, Building: null, Owner: null);
        var map = new MapDefinition("one", 1, 1, [tile]);
        var unit = Unit.FullStrength(new UnitId(1), Side.Blue, UnitKind.Infantry, HexCoord.Zero);
        var state = new GameState(map,
            new Dictionary<UnitId, Unit> { [unit.Id] = unit },
            new Dictionary<HexCoord, Side>(),
            Side.Blue, turnNumber: 1, phase: GamePhase.PlayerTurn,
            new Dictionary<Side, int> { [Side.Blue] = 0, [Side.Red] = 0 },
            winner: null, randomSeed: 1);

        Assert.True(ProductionRules.CanSideProduce(state, Side.Blue));
    }

    [Fact]
    public void CanSideProduce_false_when_all_units_have_moved()
    {
        var tile = new Tile(HexCoord.Zero, Terrain.Plains, Building: null, Owner: null);
        var map = new MapDefinition("one", 1, 1, [tile]);
        var unit = Unit.FullStrength(new UnitId(1), Side.Blue, UnitKind.Infantry, HexCoord.Zero) with { HasMoved = true };
        var state = new GameState(map,
            new Dictionary<UnitId, Unit> { [unit.Id] = unit },
            new Dictionary<HexCoord, Side>(),
            Side.Blue, turnNumber: 1, phase: GamePhase.PlayerTurn,
            new Dictionary<Side, int> { [Side.Blue] = 0, [Side.Red] = 0 },
            winner: null, randomSeed: 1);

        Assert.False(ProductionRules.CanSideProduce(state, Side.Blue));
    }
}
