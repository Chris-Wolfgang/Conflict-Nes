using Wolfgang.Conflict.Classic.Engine.Game;
using Wolfgang.Conflict.Classic.Engine.Hex;
using Wolfgang.Conflict.Classic.Engine.Map;
using Wolfgang.Conflict.Classic.Engine.Players;
using Wolfgang.Conflict.Classic.Engine.Rules;
using Wolfgang.Conflict.Classic.Engine.Units;
using Xunit;

namespace Wolfgang.Conflict.Classic.Engine.Tests.Rules;

public class MovementRulesTests
{
    private static GameState MakeState(MapDefinition map, params Unit[] units)
    {
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

    private static MapDefinition AllPlains(int width, int height) =>
        new("plains", width, height,
            MapDefinition.EnumerateCoords(width, height)
                .Select(c => new Tile(c, Terrain.Plains, Building: null, Owner: null)));

    [Fact]
    public void GetReachable_returns_full_radius_on_open_plains()
    {
        var map = AllPlains(10, 5);
        var tank = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Tank, new HexCoord(4, 1));
        var state = MakeState(map, tank);

        var reachable = MovementRules.GetReachable(state, tank);

        // Tank moves 5 hexes; expect all hexes within 5 steps (minus current hex, minus off-map).
        var unrestrictedCount = tank.Coord.WithinRange(5).Count(c => map.Contains(c)) - 1;
        Assert.Equal(unrestrictedCount, reachable.Count);
        Assert.DoesNotContain(tank.Coord, reachable);
    }

    [Fact]
    public void GetReachable_excludes_hexes_blocked_by_other_units()
    {
        var map = AllPlains(6, 3);
        var tank = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Tank, new HexCoord(0, 0));
        var blocker = Unit.FullStrength(new UnitId(2), Side.Red, TestCatalog.Infantry, new HexCoord(1, 0));
        var state = MakeState(map, tank, blocker);

        var reachable = MovementRules.GetReachable(state, tank);

        Assert.DoesNotContain(new HexCoord(1, 0), reachable);
    }

    [Fact]
    public void FindPath_returns_null_for_unreachable_destination_over_budget()
    {
        var map = AllPlains(20, 3);
        var infantry = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Infantry, new HexCoord(0, 0));
        var state = MakeState(map, infantry);

        // Infantry MOVING is 4 - cannot reach (10, 0) in one turn.
        var path = MovementRules.FindPath(state, infantry, new HexCoord(10, 0));

        Assert.Null(path);
    }

    [Fact]
    public void FindPath_to_adjacent_plain_costs_one()
    {
        var map = AllPlains(6, 3);
        var tank = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Tank, new HexCoord(2, 1));
        var state = MakeState(map, tank);

        var path = MovementRules.FindPath(state, tank, new HexCoord(3, 1));

        Assert.NotNull(path);
        Assert.Equal(1, path!.TotalCost);
    }

    [Fact]
    public void FindPath_through_forest_costs_two_per_hex()
    {
        var tiles = MapDefinition.EnumerateCoords(6, 3)
            .Select(c => new Tile(c, c.Q == 3 ? Terrain.Forest : Terrain.Plains, Building: null, Owner: null));
        var map = new MapDefinition("forest", 6, 3, tiles);
        var tank = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Tank, new HexCoord(2, 1));
        var state = MakeState(map, tank);

        var path = MovementRules.FindPath(state, tank, new HexCoord(3, 1));

        Assert.NotNull(path);
        Assert.Equal(2, path!.TotalCost);
    }

    [Fact]
    public void GetReachable_returns_empty_for_unit_with_no_moves_remaining()
    {
        var map = AllPlains(6, 3);
        var tank = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Tank, new HexCoord(2, 1)) with { MovesRemaining = 0 };
        var state = MakeState(map, tank);

        Assert.Empty(MovementRules.GetReachable(state, tank));
    }

    [Fact]
    public void GetReachable_returns_empty_for_unit_at_zero_fuel()
    {
        // Fuel is a per-turn counter: 0 = can't move this turn at all.
        var map = AllPlains(6, 3);
        var tank = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Tank, new HexCoord(2, 1)) with { Fuel = 0 };
        var state = MakeState(map, tank);

        Assert.Empty(MovementRules.GetReachable(state, tank));
    }

    [Fact]
    public void GetReachable_full_range_when_fuel_is_one()
    {
        // One unit of fuel buys an entire turn of movement; the budget is
        // MovesRemaining, not Fuel.
        var map = AllPlains(10, 5);
        var tank = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Tank, new HexCoord(2, 1)) with { Fuel = 1 };
        var state = MakeState(map, tank);

        var reachable = MovementRules.GetReachable(state, tank);

        // Tank moves 5 hexes; on open plains expect many reachable hexes.
        Assert.True(reachable.Count > 6);
    }
}
