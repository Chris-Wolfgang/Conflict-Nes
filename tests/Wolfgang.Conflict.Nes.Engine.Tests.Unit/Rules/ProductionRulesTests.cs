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
    private static GameState SingleUnitState(Unit unit)
    {
        var tile = new Tile(HexCoord.Zero, Terrain.Plains, Building: null, Owner: null);
        var map = new MapDefinition("one", 1, 1, [tile]);
        return new GameState(map, TestCatalog.Catalog,
            new Dictionary<UnitId, Unit> { [unit.Id] = unit },
            new Dictionary<HexCoord, Side>(),
            Side.Blue, turnNumber: 1, phase: GamePhase.PlayerTurn,
            new Dictionary<Side, int> { [Side.Blue] = 0, [Side.Red] = 0 },
            winner: null, randomSeed: 1);
    }

    [Theory]
    [InlineData(BuildingKind.Factory, UnitCategory.BattleTank, true)]
    [InlineData(BuildingKind.Factory, UnitCategory.Infantry, true)]
    [InlineData(BuildingKind.Factory, UnitCategory.Fighter, false)]
    [InlineData(BuildingKind.Airbase, UnitCategory.Fighter, true)]
    [InlineData(BuildingKind.Airbase, UnitCategory.Helicopter, true)]
    [InlineData(BuildingKind.Airbase, UnitCategory.BattleTank, false)]
    [InlineData(BuildingKind.City, UnitCategory.Infantry, false)]
    public void CanBuildCategoryAt_matches_design(BuildingKind building, UnitCategory category, bool expected)
    {
        Assert.Equal(expected, ProductionRules.CanBuildCategoryAt(building, category));
    }

    [Fact]
    public void ProducibleAt_factory_for_blue_returns_blue_ground_units()
    {
        var producible = ProductionRules.ProducibleAt(TestCatalog.Catalog, BuildingKind.Factory, Side.Blue);

        Assert.Contains(producible, d => string.Equals(d.Id, "m1a1", StringComparison.Ordinal));
        Assert.DoesNotContain(producible, d => string.Equals(d.Id, "t80", StringComparison.Ordinal));
        Assert.DoesNotContain(producible, d => d.Category == UnitCategory.Fighter);
    }

    [Fact]
    public void ProducibleAt_airbase_for_red_returns_red_air_units()
    {
        var producible = ProductionRules.ProducibleAt(TestCatalog.Catalog, BuildingKind.Airbase, Side.Red);

        Assert.Contains(producible, d => string.Equals(d.Id, "mig23", StringComparison.Ordinal));
        Assert.All(producible, d => Assert.True(RelationsTableIsAir(d.Category)));
    }

    private static bool RelationsTableIsAir(UnitCategory c)
        => Engine.Combat.RelationsTable.IsAir(c);

    [Fact]
    public void CanSideProduce_true_when_a_unit_has_not_moved()
    {
        var unit = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Infantry, HexCoord.Zero);

        Assert.True(ProductionRules.CanSideProduce(SingleUnitState(unit), Side.Blue));
    }

    [Fact]
    public void CanSideProduce_false_when_all_units_have_moved()
    {
        var unit = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Infantry, HexCoord.Zero) with { HasMoved = true };

        Assert.False(ProductionRules.CanSideProduce(SingleUnitState(unit), Side.Blue));
    }
}
