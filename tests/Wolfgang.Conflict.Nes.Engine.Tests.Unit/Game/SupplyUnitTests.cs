using Wolfgang.Conflict.Nes.Engine.Game;
using Wolfgang.Conflict.Nes.Engine.Hex;
using Wolfgang.Conflict.Nes.Engine.Map;
using Wolfgang.Conflict.Nes.Engine.Players;
using Wolfgang.Conflict.Nes.Engine.Units;
using Xunit;

namespace Wolfgang.Conflict.Nes.Engine.Tests.Game;

public class SupplyUnitTests
{
    private static GameState BuildStateWithFriendlyCity(Unit unit)
    {
        var tiles = MapDefinition.EnumerateCoords(3, 1)
            .Select(c => c == new HexCoord(1, 0)
                ? new Tile(c, Terrain.Plains, Building: BuildingKind.City, Owner: Side.Blue)
                : new Tile(c, Terrain.Plains, Building: null, Owner: null));
        var map = new MapDefinition("city", 3, 1, tiles);
        var dict = new Dictionary<UnitId, Unit> { [unit.Id] = unit };
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
    public void SupplyUnit_on_friendly_city_refuels_rearms_and_repairs()
    {
        var engine = new GameEngine();
        var tank = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Tank, new HexCoord(1, 0)) with
        {
            Fuel = 2,
            Ammo = 2,
            HitPoints = 5,
        };
        var state = BuildStateWithFriendlyCity(tank);

        var next = engine.SupplyUnit(state, tank.Id);
        var refreshed = next.Units[tank.Id];

        var stats = TestCatalog.Tank;
        Assert.Equal(stats.MaxFuel, refreshed.Fuel);
        Assert.Equal(stats.MaxAmmo, refreshed.Ammo);
        Assert.Equal(10, refreshed.HitPoints);
        Assert.True(refreshed.HasSupplied);
    }

    [Fact]
    public void SupplyUnit_twice_throws()
    {
        var engine = new GameEngine();
        var tank = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Tank, new HexCoord(1, 0));
        var state = BuildStateWithFriendlyCity(tank);
        var next = engine.SupplyUnit(state, tank.Id);

        Assert.Throws<InvalidOperationException>(() => engine.SupplyUnit(next, tank.Id));
    }

    [Fact]
    public void SupplyUnit_off_a_building_throws()
    {
        var engine = new GameEngine();
        var tank = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Tank, new HexCoord(0, 0));
        var state = BuildStateWithFriendlyCity(tank);

        Assert.Throws<InvalidOperationException>(() => engine.SupplyUnit(state, tank.Id));
    }

    [Fact]
    public void SupplyUnit_on_wrong_turn_throws()
    {
        var engine = new GameEngine();
        var redTank = Unit.FullStrength(new UnitId(1), Side.Red, TestCatalog.Tank, new HexCoord(1, 0));
        var state = BuildStateWithFriendlyCity(redTank);

        Assert.Throws<InvalidOperationException>(() => engine.SupplyUnit(state, redTank.Id));
    }
}
