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
    public async Task BuildUnit_at_factory_creates_a_tank_and_deducts_funds()
    {
        var (engine, state) = await StartMission01WithFunds();
        // Move Blue's helicopter off the airbase by leaving Helicopter where it is.
        // The factory hex (1,6) is currently empty: nothing was placed there.

        var next = engine.BuildUnit(state, BlueFactory, TestCatalog.Tank.Id);

        var fresh = next.Units.Values.Single(u => u.Coord == BlueFactory);
        Assert.Equal(TestCatalog.Tank, fresh.Type);
        Assert.Equal(Side.Blue, fresh.Side);
        Assert.Equal(10000 - TestCatalog.Tank.ProductionCost, next.Funds[Side.Blue]);
    }

    [Fact]
    public async Task BuildUnit_at_airbase_is_blocked_if_helicopter_still_there()
    {
        var (engine, state) = await StartMission01WithFunds();
        // Blue Helicopter starts on the airbase (1, 5).
        Assert.Throws<InvalidOperationException>(() => engine.BuildUnit(state, BlueAirbase, TestCatalog.Fighter.Id));
    }

    [Fact]
    public async Task BuildUnit_when_kind_not_producible_at_building_throws()
    {
        var (engine, state) = await StartMission01WithFunds();
        // Factories don't produce air units.
        Assert.Throws<InvalidOperationException>(() => engine.BuildUnit(state, BlueFactory, TestCatalog.Fighter.Id));
    }

    [Fact]
    public async Task BuildUnit_without_enough_funds_throws()
    {
        var (engine, state) = await StartMission01WithFunds(blueFunds: 100);
        Assert.Throws<InvalidOperationException>(() => engine.BuildUnit(state, BlueFactory, TestCatalog.Tank.Id));
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
    public async Task Newly_built_unit_cannot_move_or_attack_this_turn()
    {
        var (engine, state) = await StartMission01WithFunds();

        var next = engine.BuildUnit(state, BlueFactory, TestCatalog.Tank.Id);
        var fresh = next.Units.Values.Single(u => u.Coord == BlueFactory);

        Assert.True(fresh.HasMoved);
        Assert.True(fresh.HasAttacked);
        Assert.Equal(0, fresh.MovesRemaining);
    }
}
