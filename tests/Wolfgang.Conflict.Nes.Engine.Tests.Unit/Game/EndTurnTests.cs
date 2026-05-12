using Wolfgang.Conflict.Nes.Engine.Game;
using Wolfgang.Conflict.Nes.Engine.Hex;
using Wolfgang.Conflict.Nes.Engine.Map;
using Wolfgang.Conflict.Nes.Engine.Players;
using Wolfgang.Conflict.Nes.Engine.Units;
using Xunit;

namespace Wolfgang.Conflict.Nes.Engine.Tests.Game;

public class EndTurnTests
{
    [Fact]
    public async Task EndTurn_swaps_active_side()
    {
        var engine = new GameEngine();
        var mission = await MissionLoader.LoadMission01Async();
        var state = engine.StartGame(mission, randomSeed: 1);

        var next = engine.EndTurn(state);

        Assert.Equal(Side.Red, next.NextToAct);
        Assert.Equal(1, next.TurnNumber);
    }

    [Fact]
    public async Task EndTurn_twice_increments_turn_number_for_blue()
    {
        var engine = new GameEngine();
        var mission = await MissionLoader.LoadMission01Async();
        var state = engine.StartGame(mission, randomSeed: 1);

        var afterRed = engine.EndTurn(engine.EndTurn(state));

        Assert.Equal(Side.Blue, afterRed.NextToAct);
        Assert.Equal(2, afterRed.TurnNumber);
    }

    [Fact]
    public async Task EndTurn_refreshes_per_turn_fields_for_incoming_side()
    {
        var engine = new GameEngine();
        var mission = await MissionLoader.LoadMission01Async();
        var state = engine.StartGame(mission, randomSeed: 1);

        // Mark a Red unit as having moved/attacked/supplied; EndTurn from
        // Blue should still leave Red unchanged. EndTurn from Red should refresh.
        var redUnit = state.Units.Values.First(u => u.Side == Side.Red);
        var modified = state.Units[redUnit.Id] with { HasMoved = true, HasAttacked = true, HasSupplied = true };
        var modifiedUnits = new Dictionary<UnitId, Unit>();
        foreach (var kv in state.Units)
        {
            modifiedUnits[kv.Key] = kv.Key == redUnit.Id ? modified : kv.Value;
        }
        var withRedFlagged = new GameState(
            state.Map, modifiedUnits, state.BuildingOwners,
            state.NextToAct, state.TurnNumber, state.Phase, state.Funds, state.Winner, state.RandomSeed);

        var afterBlueEnded = engine.EndTurn(withRedFlagged);

        var redAfter = afterBlueEnded.Units[redUnit.Id];
        Assert.False(redAfter.HasMoved);
        Assert.False(redAfter.HasAttacked);
        Assert.False(redAfter.HasSupplied);
    }

    [Fact]
    public async Task EndTurn_grants_income_for_owned_city_and_airbase()
    {
        var engine = new GameEngine();
        var mission = await MissionLoader.LoadMission01Async();
        var state = engine.StartGame(mission, randomSeed: 1);

        // Blue ends turn. Red gains income from its airbase. Mission01 has 1
        // Red Airbase (= 100 F.P. per turn) and no Red-owned City at start.
        var after = engine.EndTurn(state);

        Assert.Equal(100, after.Funds[Side.Red]);
        Assert.Equal(0, after.Funds[Side.Blue]);
    }

    [Fact]
    public async Task EndTurn_when_game_over_throws()
    {
        var engine = new GameEngine();
        var mission = await MissionLoader.LoadMission01Async();
        var state = engine.StartGame(mission, randomSeed: 1);
        var over = new GameState(state.Map, state.Units, state.BuildingOwners,
            state.NextToAct, state.TurnNumber, GamePhase.GameOver,
            state.Funds, Side.Blue, state.RandomSeed);

        Assert.Throws<InvalidOperationException>(() => engine.EndTurn(over));
    }
}
