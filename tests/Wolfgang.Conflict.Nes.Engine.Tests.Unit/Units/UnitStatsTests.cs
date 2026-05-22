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
    [InlineData(MovementDomain.Foot,  Terrain.Plains,   1)]
    [InlineData(MovementDomain.Foot,  Terrain.Road,     1)]
    [InlineData(MovementDomain.Foot,  Terrain.Beach,    2)]
    [InlineData(MovementDomain.Foot,  Terrain.Forest,   2)]
    [InlineData(MovementDomain.Foot,  Terrain.Mountain, 3)]
    [InlineData(MovementDomain.Foot,  Terrain.Bridge,   2)]
    [InlineData(MovementDomain.Tread, Terrain.Plains,   1)]
    [InlineData(MovementDomain.Tread, Terrain.Mountain, 3)]
    public void TerrainCost_ground_passable_matches_manual(MovementDomain domain, Terrain terrain, int expected)
    {
        Assert.Equal(expected, UnitStats.TerrainCost(domain, terrain, building: null));
    }

    [Theory]
    [InlineData(MovementDomain.Foot,  Terrain.River)]
    [InlineData(MovementDomain.Foot,  Terrain.Sea)]
    [InlineData(MovementDomain.Tread, Terrain.River)]
    [InlineData(MovementDomain.Tread, Terrain.Sea)]
    [InlineData(MovementDomain.Tread, Terrain.Reef)]
    public void TerrainCost_ground_water_is_impassable(MovementDomain domain, Terrain terrain)
    {
        Assert.Null(UnitStats.TerrainCost(domain, terrain, building: null));
    }

    [Theory]
    [InlineData(MovementDomain.Helicopter, Terrain.Mountain)]
    [InlineData(MovementDomain.Helicopter, Terrain.Sea)]
    [InlineData(MovementDomain.Fighter,    Terrain.Mountain)]
    [InlineData(MovementDomain.Fighter,    Terrain.River)]
    public void TerrainCost_air_treats_every_terrain_as_one(MovementDomain domain, Terrain terrain)
    {
        Assert.Equal(1, UnitStats.TerrainCost(domain, terrain, building: null));
    }

    [Theory]
    [InlineData(MovementDomain.Foot)]
    [InlineData(MovementDomain.Tread)]
    [InlineData(MovementDomain.Helicopter)]
    [InlineData(MovementDomain.Fighter)]
    public void TerrainCost_factory_is_impassable_to_all(MovementDomain domain)
    {
        Assert.Null(UnitStats.TerrainCost(domain, Terrain.Plains, BuildingKind.Factory));
    }

    [Theory]
    [InlineData(MovementDomain.Foot,    BuildingKind.City)]
    [InlineData(MovementDomain.Tread,   BuildingKind.Hq)]
    [InlineData(MovementDomain.Fighter, BuildingKind.Airbase)]
    public void TerrainCost_buildings_passable_at_cost_one(MovementDomain domain, BuildingKind building)
    {
        Assert.Equal(1, UnitStats.TerrainCost(domain, Terrain.Plains, building));
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
        Assert.Equal(2, UnitStats.DefenseBonus(Terrain.Forest, BuildingKind.City));
    }
}
