using Wolfgang.Conflict.Nes.Engine.Map;
using Wolfgang.Conflict.Nes.Engine.Units;
using Xunit;

namespace Wolfgang.Conflict.Nes.Engine.Tests.Units;

public class UnitStatsTests
{
    [Fact]
    public void MaxHitPoints_matches_manual_LIFE_15()
    {
        Assert.Equal(15, UnitStats.MaxHitPoints);
    }

    [Theory]
    [InlineData(UnitKind.Infantry,   MovementDomain.Foot,        4, 10,  8,  600, true)]
    [InlineData(UnitKind.Tank,       MovementDomain.Tread,       5,  8, 14, 6000, false)]
    [InlineData(UnitKind.Helicopter, MovementDomain.Helicopter,  7,  5,  6, 2400, false)]
    [InlineData(UnitKind.Fighter,    MovementDomain.Fighter,    10,  6,  6, 6300, false)]
    public void For_returns_manual_stats(
        UnitKind kind,
        MovementDomain domain,
        int movePoints,
        int maxFuel,
        int maxAmmo,
        int productionCost,
        bool canCapture)
    {
        var stats = UnitStats.For(kind);

        Assert.Equal(kind, stats.Kind);
        Assert.Equal(domain, stats.MovementDomain);
        Assert.Equal(movePoints, stats.MovementPoints);
        Assert.Equal(maxFuel, stats.MaxFuel);
        Assert.Equal(maxAmmo, stats.MaxAmmo);
        Assert.Equal(productionCost, stats.ProductionCost);
        Assert.Equal(canCapture, stats.CanCapture);
    }

    [Theory]
    [InlineData(UnitKind.Infantry,   false)]
    [InlineData(UnitKind.Helicopter, false)]
    [InlineData(UnitKind.Tank,       true)]
    [InlineData(UnitKind.Fighter,    true)]
    public void HasManeuver5_matches_manual_high_end_units(UnitKind kind, bool expected)
    {
        Assert.Equal(expected, UnitStats.For(kind).HasManeuver5);
    }

    [Fact]
    public void For_unknown_kind_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => UnitStats.For((UnitKind)99));
    }

    [Theory]
    [InlineData(UnitKind.Infantry, Terrain.Plains,   1)]
    [InlineData(UnitKind.Infantry, Terrain.Road,     1)]
    [InlineData(UnitKind.Infantry, Terrain.Beach,    2)]
    [InlineData(UnitKind.Infantry, Terrain.Forest,   2)]
    [InlineData(UnitKind.Infantry, Terrain.Mountain, 3)]
    [InlineData(UnitKind.Infantry, Terrain.Bridge,   2)]
    [InlineData(UnitKind.Tank,     Terrain.Plains,   1)]
    [InlineData(UnitKind.Tank,     Terrain.Mountain, 3)]
    public void TerrainCost_ground_passable_matches_manual(UnitKind kind, Terrain terrain, int expected)
    {
        Assert.Equal(expected, UnitStats.TerrainCost(kind, terrain, building: null));
    }

    [Theory]
    [InlineData(UnitKind.Infantry, Terrain.River)]
    [InlineData(UnitKind.Infantry, Terrain.Sea)]
    [InlineData(UnitKind.Tank,     Terrain.River)]
    [InlineData(UnitKind.Tank,     Terrain.Sea)]
    [InlineData(UnitKind.Tank,     Terrain.Reef)]
    public void TerrainCost_ground_water_is_impassable(UnitKind kind, Terrain terrain)
    {
        Assert.Null(UnitStats.TerrainCost(kind, terrain, building: null));
    }

    [Theory]
    [InlineData(UnitKind.Helicopter, Terrain.Mountain)]
    [InlineData(UnitKind.Helicopter, Terrain.Sea)]
    [InlineData(UnitKind.Fighter,    Terrain.Mountain)]
    [InlineData(UnitKind.Fighter,    Terrain.River)]
    public void TerrainCost_air_treats_every_terrain_as_one(UnitKind kind, Terrain terrain)
    {
        Assert.Equal(1, UnitStats.TerrainCost(kind, terrain, building: null));
    }

    [Theory]
    [InlineData(UnitKind.Infantry,   BuildingKind.Factory)]
    [InlineData(UnitKind.Tank,       BuildingKind.Factory)]
    [InlineData(UnitKind.Helicopter, BuildingKind.Factory)]
    [InlineData(UnitKind.Fighter,    BuildingKind.Factory)]
    public void TerrainCost_factory_is_impassable_to_all(UnitKind kind, BuildingKind factory)
    {
        Assert.Null(UnitStats.TerrainCost(kind, Terrain.Plains, factory));
    }

    [Theory]
    [InlineData(UnitKind.Infantry, BuildingKind.City,    1)]
    [InlineData(UnitKind.Tank,     BuildingKind.Hq,      1)]
    [InlineData(UnitKind.Fighter,  BuildingKind.Airbase, 1)]
    public void TerrainCost_buildings_passable_at_cost_one(UnitKind kind, BuildingKind building, int expected)
    {
        Assert.Equal(expected, UnitStats.TerrainCost(kind, Terrain.Plains, building));
    }

    [Theory]
    [InlineData(Terrain.Plains,   0)]
    [InlineData(Terrain.Road,     0)]
    [InlineData(Terrain.Beach,    0)]
    [InlineData(Terrain.Forest,   3)]
    [InlineData(Terrain.Mountain, 3)]
    [InlineData(Terrain.Bridge,   2)]
    public void DefenseBonus_open_terrain_matches_manual(Terrain terrain, int expected)
    {
        Assert.Equal(expected, UnitStats.DefenseBonus(terrain, building: null));
    }

    [Theory]
    [InlineData(BuildingKind.City,    2)]
    [InlineData(BuildingKind.Hq,      3)]
    [InlineData(BuildingKind.Airbase, 0)]
    [InlineData(BuildingKind.Port,    0)]
    public void DefenseBonus_buildings_match_manual(BuildingKind building, int expected)
    {
        Assert.Equal(expected, UnitStats.DefenseBonus(Terrain.Plains, building));
    }

    [Fact]
    public void DefenseBonus_building_overrides_terrain()
    {
        // A city built on forest hex uses the city bonus, not forest.
        Assert.Equal(2, UnitStats.DefenseBonus(Terrain.Forest, BuildingKind.City));
    }

    [Theory]
    [InlineData(UnitKind.Tank,       UnitKind.Infantry,   10)]
    [InlineData(UnitKind.Tank,       UnitKind.Tank,        6)]
    [InlineData(UnitKind.Tank,       UnitKind.Fighter,     0)]
    [InlineData(UnitKind.Helicopter, UnitKind.Tank,        7)]
    [InlineData(UnitKind.Helicopter, UnitKind.Fighter,     1)]
    [InlineData(UnitKind.Fighter,    UnitKind.Helicopter, 10)]
    [InlineData(UnitKind.Fighter,    UnitKind.Tank,        0)]
    [InlineData(UnitKind.Infantry,   UnitKind.Fighter,     0)]
    public void BaseAttack_matches_design_table(UnitKind attacker, UnitKind defender, int expected)
    {
        Assert.Equal(expected, UnitStats.BaseAttack(attacker, defender));
    }
}
