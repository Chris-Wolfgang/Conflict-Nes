using Wolfgang.Conflict.Nes.Engine.Game;
using Wolfgang.Conflict.Nes.Engine.Hex;
using Wolfgang.Conflict.Nes.Engine.Players;
using Wolfgang.Conflict.Nes.Engine.Units;
using Xunit;

namespace Wolfgang.Conflict.Nes.Engine.Tests.Game;

public class BuildUnitTests
{
    private static async Task<(GameEngine engine, GameState state)> StartMission01WithFunds(int blueFunds = 10000)
    {
        var mission = await MissionLoader.LoadMission01Async();
        var engine = new GameEngine();
        var state = engine.StartGame(mission, randomSeed: 1);
        state = new GameState(state.Map, state.Catalog, state.Units, state.BuildingOwners,
            state.NextToAct, state.TurnNumber, state.Phase,
            new Dictionary<Side, int> { [Side.Blue] = blueFunds, [Side.Red] = state.Funds[Side.Red] },
            state.Winner, state.RandomSeed);
        return (engine, state);
    }

    private static HexCoord BlueFactory { get; } = new(1, 6);
    private static HexCoord BlueAirbase { get; } = new(1, 5);

    [Fact]
    public async Task BuildUnit_queues_a_pending_order_without_deducting_funds()
    {
        // Building is free per the original game. F.P. ProductionCost is
        // the loser-penalty value, not a price tag.
        var (engine, state) = await StartMission01WithFunds();

        var next = engine.BuildUnit(state, BlueFactory, TestCatalog.Tank.Id);

        Assert.Equal(10000, next.Funds[Side.Blue]);
        // The unit is NOT yet on the map — it lives in PendingProduction.
        Assert.DoesNotContain(next.Units.Values, u => u.Coord == BlueFactory);
        Assert.True(next.PendingProduction.TryGetValue(Side.Blue, out var pending));
        Assert.Equal(BlueFactory, pending!.FactoryCoord);
        Assert.Equal(TestCatalog.Tank.Id, pending.TypeId);
    }

    [Fact]
    public async Task BuildUnit_pending_order_materialises_at_start_of_next_blue_turn()
    {
        var (engine, state) = await StartMission01WithFunds();

        var afterBuild = engine.BuildUnit(state, BlueFactory, TestCatalog.Tank.Id);
        // Blue ends turn -> Red plays -> Red ends turn -> back to Blue.
        var afterBlueEnd = engine.EndTurn(afterBuild);
        var backToBlue = engine.EndTurn(afterBlueEnd);

        var fresh = backToBlue.Units.Values.Single(u => u.Coord == BlueFactory);
        Assert.Equal(TestCatalog.Tank, fresh.Type);
        Assert.Equal(Side.Blue, fresh.Side);
        // The pending order is cleared once materialised.
        Assert.False(backToBlue.PendingProduction.ContainsKey(Side.Blue));
        // And the freshly-produced unit starts the turn with full movement.
        Assert.False(fresh.HasMoved);
        Assert.False(fresh.HasAttacked);
        Assert.Equal(fresh.Type.MovementPoints, fresh.MovesRemaining);
    }

    [Fact]
    public async Task BuildUnit_at_airbase_is_blocked_if_a_unit_is_standing_on_it()
    {
        var (engine, state) = await StartMission01WithFunds();
        // The starting placements no longer park a unit on the Blue Airbase
        // (so production isn't permanently blocked turn one). Manually park
        // the AH-1S there to verify the occupancy rule still bites.
        var ah1s = state.Units.Values.First(u => string.Equals(u.Type.Id, "ah1s", StringComparison.Ordinal) && u.Side == Side.Blue);
        var newUnits = new Dictionary<UnitId, Unit>();
        foreach (var kv in state.Units)
        {
            newUnits[kv.Key] = kv.Value;
        }
        newUnits[ah1s.Id] = ah1s with { Coord = BlueAirbase };
        var blocked = new GameState(state.Map, state.Catalog, newUnits, state.BuildingOwners,
            state.NextToAct, state.TurnNumber, state.Phase, state.Funds, state.Winner, state.RandomSeed);

        Assert.Throws<InvalidOperationException>(() => engine.BuildUnit(blocked, BlueAirbase, TestCatalog.Fighter.Id));
    }

    [Fact]
    public async Task BuildUnit_when_kind_not_producible_at_building_throws()
    {
        var (engine, state) = await StartMission01WithFunds();
        // Factories don't produce air units.
        Assert.Throws<InvalidOperationException>(() => engine.BuildUnit(state, BlueFactory, TestCatalog.Fighter.Id));
    }

    [Fact]
    public async Task BuildUnit_with_zero_funds_still_succeeds()
    {
        // Building is free; funds are irrelevant to whether a build is legal.
        var (engine, state) = await StartMission01WithFunds(blueFunds: 0);

        var next = engine.BuildUnit(state, BlueFactory, TestCatalog.Tank.Id);

        Assert.Equal(0, next.Funds[Side.Blue]);
        Assert.True(next.PendingProduction.ContainsKey(Side.Blue));
    }

    [Fact]
    public async Task BuildUnit_when_all_units_have_moved_throws()
    {
        var (engine, state) = await StartMission01WithFunds();
        var movedUnits = new Dictionary<UnitId, Unit>();
        foreach (var kv in state.Units)
        {
            movedUnits[kv.Key] = kv.Value.Side == Side.Blue
                ? kv.Value with { HasMoved = true }
                : kv.Value;
        }
        var allMoved = new GameState(state.Map, state.Catalog, movedUnits, state.BuildingOwners,
            state.NextToAct, state.TurnNumber, state.Phase, state.Funds, state.Winner, state.RandomSeed);

        Assert.Throws<InvalidOperationException>(() => engine.BuildUnit(allMoved, BlueFactory, TestCatalog.Tank.Id));
    }

    [Fact]
    public async Task Newly_built_unit_is_not_on_the_map_until_next_turn()
    {
        // Deferred materialisation makes the original "fresh unit can't
        // act on the build turn" rule implicit — the unit simply doesn't
        // exist on the board until the side's next turn starts.
        var (engine, state) = await StartMission01WithFunds();

        var next = engine.BuildUnit(state, BlueFactory, TestCatalog.Tank.Id);

        Assert.DoesNotContain(next.Units.Values, u => u.Coord == BlueFactory);
    }

    [Fact]
    public async Task A_side_can_only_build_one_unit_per_turn()
    {
        // The manual restricts production to a single unit per turn across
        // all of a side's factories.
        var (engine, state) = await StartMission01WithFunds();

        var afterFirst = engine.BuildUnit(state, BlueFactory, TestCatalog.Tank.Id);

        Assert.True(afterFirst.HasBuiltThisTurn(Side.Blue));
        Assert.Equal(BlueFactory, afterFirst.GetBuildHexThisTurn(Side.Blue));
        // A second build attempt (even of free infantry) must be rejected.
        Assert.Throws<InvalidOperationException>(
            () => engine.BuildUnit(afterFirst, BlueFactory, TestCatalog.Infantry.Id));
    }

    [Fact]
    public async Task EndTurn_clears_the_one_build_per_turn_flag()
    {
        var (engine, state) = await StartMission01WithFunds();

        var afterBuild = engine.BuildUnit(state, BlueFactory, TestCatalog.Tank.Id);
        var afterEndTurn = engine.EndTurn(afterBuild);

        // After Blue ends its turn the flag is cleared so Red can build,
        // and Blue gets a fresh build next turn as well.
        Assert.False(afterEndTurn.HasBuiltThisTurn(Side.Blue));
        Assert.False(afterEndTurn.HasBuiltThisTurn(Side.Red));
    }
}
