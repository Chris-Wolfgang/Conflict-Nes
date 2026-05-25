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
    public void ProducibleAt_factory_for_blue_returns_curated_six()
    {
        var producible = ProductionRules.ProducibleAt(TestCatalog.Catalog, BuildingKind.Factory, Side.Blue);

        // Curated roster: Liberator (infantry), Commando, M151 jeep, M60A3,
        // M48 SAM, M247 AA-gun. The HQ M1A1 must NOT be buildable.
        Assert.Equal(6, producible.Count);
        var ids = new HashSet<string>(producible.Select(d => d.Id), StringComparer.Ordinal);
        Assert.Contains("liberator", ids);
        Assert.Contains("us-commando", ids);
        Assert.Contains("m151", ids);
        Assert.Contains("m60a3", ids);
        Assert.Contains("m48", ids);
        Assert.Contains("m247", ids);
        Assert.DoesNotContain("m1a1", ids);
        Assert.DoesNotContain("t80", ids);
    }

    [Fact]
    public void ProducibleAt_airbase_for_red_returns_curated_six_air_units()
    {
        var producible = ProductionRules.ProducibleAt(TestCatalog.Catalog, BuildingKind.Airbase, Side.Red);

        Assert.Equal(6, producible.Count);
        var ids = new HashSet<string>(producible.Select(d => d.Id), StringComparer.Ordinal);
        Assert.Contains("mi24", ids);
        Assert.Contains("mi28", ids);
        Assert.Contains("su25", ids);
        Assert.Contains("su17", ids);
        Assert.Contains("mig23", ids);
        Assert.Contains("mig29", ids);
        Assert.All(producible, d => Assert.True(RelationsTableIsAir(d.Category)));
    }

    [Fact]
    public void Rosters_are_complementary_between_sides()
    {
        // Every map-present unit must be in its side's factory roster, and
        // every roster slot on one side must have a counterpart slot on the
        // other (NATO/Soviet pairings).
        var blueLand = ProductionRules.ProducibleAt(TestCatalog.Catalog, BuildingKind.Factory, Side.Blue);
        var redLand  = ProductionRules.ProducibleAt(TestCatalog.Catalog, BuildingKind.Factory, Side.Red);
        var blueAir  = ProductionRules.ProducibleAt(TestCatalog.Catalog, BuildingKind.Airbase, Side.Blue);
        var redAir   = ProductionRules.ProducibleAt(TestCatalog.Catalog, BuildingKind.Airbase, Side.Red);

        Assert.Equal(blueLand.Count, redLand.Count);
        Assert.Equal(blueAir.Count, redAir.Count);

        // Each land roster's first entry is the side's infantry.
        Assert.Equal(UnitCategory.Infantry, blueLand[0].Category);
        Assert.Equal(UnitCategory.Infantry, redLand[0].Category);

        // Same category mix between matched factories.
        Assert.Equal(
            blueLand.Select(d => d.Category).OrderBy(c => c),
            redLand.Select(d => d.Category).OrderBy(c => c));
        Assert.Equal(
            blueAir.Select(d => d.Category).OrderBy(c => c),
            redAir.Select(d => d.Category).OrderBy(c => c));
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
