using Wolfgang.Conflict.Nes.Engine.Game;
using Wolfgang.Conflict.Nes.Engine.Hex;
using Wolfgang.Conflict.Nes.Engine.Map;
using Wolfgang.Conflict.Nes.Engine.Players;
using Wolfgang.Conflict.Nes.Engine.Strategy;
using Wolfgang.Conflict.Nes.Engine.Units;
using Xunit;

namespace Wolfgang.Conflict.Nes.Engine.Tests.Strategy;

public class GreedyAiStrategyTests
{
    private static GameState OpenState(params Unit[] units)
    {
        var map = new MapDefinition("plains", 10, 5,
            MapDefinition.EnumerateCoords(10, 5)
                .Select(c => new Tile(c, Terrain.Plains, Building: null, Owner: null)));
        var dict = new Dictionary<UnitId, Unit>();
        foreach (var u in units)
        {
            dict[u.Id] = u;
        }
        return new GameState(map, TestCatalog.Catalog, dict,
            new Dictionary<HexCoord, Side>(),
            Side.Blue, turnNumber: 1, phase: GamePhase.PlayerTurn,
            new Dictionary<Side, int> { [Side.Blue] = 0, [Side.Red] = 0 },
            winner: null, randomSeed: 1);
    }

    [Fact]
    public async Task Attacks_adjacent_enemy_when_possible()
    {
        var atk = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Tank,     new HexCoord(0, 0));
        var def = Unit.FullStrength(new UnitId(2), Side.Red,  TestCatalog.Infantry, new HexCoord(1, 0));
        var state = OpenState(atk, def);

        var action = await new GreedyAiStrategy().ChooseNextActionAsync(state, Side.Blue);

        Assert.Equal(StrategyActionKind.Attack, action.Kind);
        Assert.Equal(atk.Id, action.UnitId);
        Assert.Equal(def.Id, action.TargetId);
    }

    [Fact]
    public async Task Moves_toward_distant_enemy_when_no_adjacent_target()
    {
        var atk = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Tank, new HexCoord(0, 0));
        var def = Unit.FullStrength(new UnitId(2), Side.Red,  TestCatalog.Tank, new HexCoord(8, 0));
        var state = OpenState(atk, def);

        var action = await new GreedyAiStrategy().ChooseNextActionAsync(state, Side.Blue);

        Assert.Equal(StrategyActionKind.Move, action.Kind);
        Assert.Equal(atk.Id, action.UnitId);
        // Should move east (closer to defender at (8,0)).
        Assert.True(action.Hex.Q > 0);
    }

    [Fact]
    public async Task Commander_defends_itself_against_adjacent_enemy()
    {
        // The commander is the "H" — losing it ends the game. It won't
        // march out looking for trouble, but if an enemy walks up to it,
        // it absolutely fights back.
        var hq = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Tank, new HexCoord(0, 0), isCommander: true);
        var enemy = Unit.FullStrength(new UnitId(2), Side.Red, TestCatalog.Infantry, new HexCoord(1, 0));
        var state = OpenState(hq, enemy);

        var action = await new GreedyAiStrategy().ChooseNextActionAsync(state, Side.Blue);

        Assert.Equal(StrategyActionKind.Attack, action.Kind);
        Assert.Equal(hq.Id, action.UnitId);
    }

    [Fact]
    public async Task Commander_does_not_move_toward_enemy()
    {
        // With only the commander available, the AI should EndTurn rather
        // than march the HQ unit into harm's way.
        var hq = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Tank, new HexCoord(0, 0), isCommander: true);
        var enemy = Unit.FullStrength(new UnitId(2), Side.Red, TestCatalog.Tank, new HexCoord(8, 0));
        var state = OpenState(hq, enemy);

        var action = await new GreedyAiStrategy().ChooseNextActionAsync(state, Side.Blue);

        Assert.Equal(StrategyActionKind.EndTurn, action.Kind);
    }

    [Fact]
    public async Task AI_first_turn_builds_at_airbase_with_most_expensive_affordable_unit()
    {
        // Difficulty 1 doctrine: turn 1 = air, turn 2 = land, alternating.
        // The greedy AI also takes the priciest unit it can pay for.
        var mission = await MissionLoader.LoadMission01Async();
        var engine = new GameEngine();
        var start = engine.StartGame(mission, randomSeed: 1);
        // Skip Blue's turn to land on Red's turn 1, then give Red enough
        // F.P. to afford any of its air roster (top costs 7800).
        var afterBlueEnd = engine.EndTurn(start);
        var richRed = WithFundsAndTurn(afterBlueEnd, redFunds: 10_000, turnNumber: 1);

        var action = await new GreedyAiStrategy().ChooseNextActionAsync(richRed, Side.Red);

        Assert.Equal(StrategyActionKind.Build, action.Kind);
        Assert.Equal(new HexCoord(7, -2), action.Hex); // Red Airbase
        // Most expensive Red air roster: MiG-29 Fulcrum at 6200 F.P.
        // (MiG-33 / Su-27 are catalog units but not in the curated 6.)
        Assert.Equal("mig29", action.ProduceTypeId);
    }

    [Fact]
    public async Task AI_second_turn_builds_at_land_factory()
    {
        var mission = await MissionLoader.LoadMission01Async();
        var engine = new GameEngine();
        var start = engine.StartGame(mission, randomSeed: 1);
        var afterBlueEnd = engine.EndTurn(start);
        // Move the Red commander off the factory neighbour so the factory
        // hex (9,-4) stays empty for production. Then advance to turn 2.
        var turn2 = WithFundsAndTurn(afterBlueEnd, redFunds: 10_000, turnNumber: 2);

        var action = await new GreedyAiStrategy().ChooseNextActionAsync(turn2, Side.Red);

        Assert.Equal(StrategyActionKind.Build, action.Kind);
        Assert.Equal(new HexCoord(9, -4), action.Hex); // Red Factory
        // Most expensive Red land roster: SA-8 SAM at 4600 F.P. (T-80 is HQ-only).
        Assert.Equal("sa8", action.ProduceTypeId);
    }

    private static GameState WithFundsAndTurn(GameState s, int redFunds, int turnNumber) =>
        new(s.Map, s.Catalog, s.Units, s.BuildingOwners,
            s.NextToAct, turnNumber, s.Phase,
            new Dictionary<Side, int> { [Side.Blue] = s.Funds[Side.Blue], [Side.Red] = redFunds },
            s.Winner, s.RandomSeed, s.BuildingHitPoints, s.BuildThisTurn);

    [Fact]
    public async Task Returns_EndTurn_when_no_unit_has_any_action()
    {
        var atk = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Tank, new HexCoord(0, 0))
            with { HasMoved = true, HasAttacked = true };
        var state = OpenState(atk);

        var action = await new GreedyAiStrategy().ChooseNextActionAsync(state, Side.Blue);

        Assert.Equal(StrategyActionKind.EndTurn, action.Kind);
    }
}
