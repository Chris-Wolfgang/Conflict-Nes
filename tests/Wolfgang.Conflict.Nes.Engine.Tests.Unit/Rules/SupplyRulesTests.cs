using Wolfgang.Conflict.Nes.Engine.Game;
using Wolfgang.Conflict.Nes.Engine.Hex;
using Wolfgang.Conflict.Nes.Engine.Map;
using Wolfgang.Conflict.Nes.Engine.Players;
using Wolfgang.Conflict.Nes.Engine.Rules;
using Wolfgang.Conflict.Nes.Engine.Units;
using Xunit;

namespace Wolfgang.Conflict.Nes.Engine.Tests.Rules;

public class SupplyRulesTests
{
    private static GameState BuildState(IEnumerable<Tile> tiles, int width, int height, params Unit[] units)
    {
        var map = new MapDefinition("test", width, height, tiles);
        var dict = new Dictionary<UnitId, Unit>();
        foreach (var u in units)
        {
            dict[u.Id] = u;
        }
        return new GameState(
            map,
            TestCatalog.Catalog,
            dict,
            new Dictionary<HexCoord, Side>(),
            Side.Blue,
            turnNumber: 1,
            phase: GamePhase.PlayerTurn,
            funds: new Dictionary<Side, int> { [Side.Blue] = 0, [Side.Red] = 0 },
            winner: null,
            randomSeed: 1);
    }

    [Fact]
    public void Ground_unit_on_friendly_city_can_supply_from_building()
    {
        var tiles = MapDefinition.EnumerateCoords(3, 1)
            .Select(c => c == new HexCoord(1, 0)
                ? new Tile(c, Terrain.Plains, Building: BuildingKind.City, Owner: Side.Blue)
                : new Tile(c, Terrain.Plains, Building: null, Owner: null));
        var tank = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Tank, new HexCoord(1, 0));
        var state = BuildState(tiles, 3, 1, tank);

        Assert.Equal(SupplyRules.SupplySource.Building, SupplyRules.AvailableSource(state, tank));
    }

    [Fact]
    public void Air_unit_on_friendly_airbase_can_supply_from_building()
    {
        var tiles = MapDefinition.EnumerateCoords(3, 1)
            .Select(c => c == new HexCoord(1, 0)
                ? new Tile(c, Terrain.Plains, Building: BuildingKind.AirFactory, Owner: Side.Blue)
                : new Tile(c, Terrain.Plains, Building: null, Owner: null));
        var fighter = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Fighter, new HexCoord(1, 0));
        var state = BuildState(tiles, 3, 1, fighter);

        Assert.Equal(SupplyRules.SupplySource.Building, SupplyRules.AvailableSource(state, fighter));
    }

    [Fact]
    public void Ground_unit_on_airbase_has_no_supply_source()
    {
        var tiles = MapDefinition.EnumerateCoords(3, 1)
            .Select(c => c == new HexCoord(1, 0)
                ? new Tile(c, Terrain.Plains, Building: BuildingKind.AirFactory, Owner: Side.Blue)
                : new Tile(c, Terrain.Plains, Building: null, Owner: null));
        var tank = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Tank, new HexCoord(1, 0));
        var state = BuildState(tiles, 3, 1, tank);

        Assert.Null(SupplyRules.AvailableSource(state, tank));
    }

    [Fact]
    public void Ground_unit_on_neutral_city_can_supply_from_building()
    {
        var tiles = MapDefinition.EnumerateCoords(3, 1)
            .Select(c => c == new HexCoord(1, 0)
                ? new Tile(c, Terrain.Plains, Building: BuildingKind.City, Owner: null)
                : new Tile(c, Terrain.Plains, Building: null, Owner: null));
        var tank = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Tank, new HexCoord(1, 0));
        var state = BuildState(tiles, 3, 1, tank);

        Assert.Equal(SupplyRules.SupplySource.Building, SupplyRules.AvailableSource(state, tank));
    }

    [Fact]
    public void Ground_unit_on_enemy_city_can_still_supply()
    {
        // Per the original game, a unit refuels/repairs at any city
        // regardless of ownership (and captures it by holding it).
        var tiles = MapDefinition.EnumerateCoords(3, 1)
            .Select(c => c == new HexCoord(1, 0)
                ? new Tile(c, Terrain.Plains, Building: BuildingKind.City, Owner: Side.Red)
                : new Tile(c, Terrain.Plains, Building: null, Owner: null));
        var tank = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Tank, new HexCoord(1, 0));
        var state = BuildState(tiles, 3, 1, tank);

        Assert.Equal(SupplyRules.SupplySource.Building, SupplyRules.AvailableSource(state, tank));
    }

    [Fact]
    public void Unit_already_supplied_this_turn_has_no_source()
    {
        var tiles = MapDefinition.EnumerateCoords(3, 1)
            .Select(c => c == new HexCoord(1, 0)
                ? new Tile(c, Terrain.Plains, Building: BuildingKind.City, Owner: Side.Blue)
                : new Tile(c, Terrain.Plains, Building: null, Owner: null));
        var tank = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Tank, new HexCoord(1, 0)) with { HasSupplied = true };
        var state = BuildState(tiles, 3, 1, tank);

        Assert.Null(SupplyRules.AvailableSource(state, tank));
    }

    [Fact]
    public void ApplySupply_from_building_refuels_rearms_and_repairs()
    {
        var tank = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Tank, HexCoord.Zero) with
        {
            Fuel = 1,
            Ammo = 1,
            HitPoints = 4,
        };

        var supplied = SupplyRules.ApplySupply(tank, SupplyRules.SupplySource.Building);

        var stats = TestCatalog.Tank;
        Assert.Equal(stats.MaxFuel, supplied.Fuel);
        Assert.Equal(stats.MaxAmmo, supplied.Ammo);
        Assert.Equal(4 + SupplyRules.RepairAmount, supplied.HitPoints);
        Assert.True(supplied.HasSupplied);
    }

    [Fact]
    public void ApplySupply_from_vehicle_does_not_repair()
    {
        var tank = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Tank, HexCoord.Zero) with
        {
            Fuel = 1,
            Ammo = 1,
            HitPoints = 4,
        };

        var supplied = SupplyRules.ApplySupply(tank, SupplyRules.SupplySource.Vehicle);

        Assert.Equal(TestCatalog.Tank.MaxFuel, supplied.Fuel);
        Assert.Equal(4, supplied.HitPoints);
        Assert.True(supplied.HasSupplied);
    }

    [Fact]
    public void ApplySupply_caps_at_max_hit_points()
    {
        var tank = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Tank, HexCoord.Zero) with
        {
            HitPoints = UnitStats.MaxHitPoints - 1,
        };

        var supplied = SupplyRules.ApplySupply(tank, SupplyRules.SupplySource.Building);

        Assert.Equal(UnitStats.MaxHitPoints, supplied.HitPoints);
    }
}
