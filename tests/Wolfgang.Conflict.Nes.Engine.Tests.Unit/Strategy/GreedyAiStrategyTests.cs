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
        return new GameState(map, dict,
            new Dictionary<HexCoord, Side>(),
            Side.Blue, turnNumber: 1, phase: GamePhase.PlayerTurn,
            new Dictionary<Side, int> { [Side.Blue] = 0, [Side.Red] = 0 },
            winner: null, randomSeed: 1);
    }

    [Fact]
    public async Task Attacks_adjacent_enemy_when_possible()
    {
        var atk = Unit.FullStrength(new UnitId(1), Side.Blue, UnitKind.Tank,     new HexCoord(0, 0));
        var def = Unit.FullStrength(new UnitId(2), Side.Red,  UnitKind.Infantry, new HexCoord(1, 0));
        var state = OpenState(atk, def);

        var action = await new GreedyAiStrategy().ChooseNextActionAsync(state, Side.Blue);

        Assert.Equal(StrategyActionKind.Attack, action.Kind);
        Assert.Equal(atk.Id, action.UnitId);
        Assert.Equal(def.Id, action.TargetId);
    }

    [Fact]
    public async Task Moves_toward_distant_enemy_when_no_adjacent_target()
    {
        var atk = Unit.FullStrength(new UnitId(1), Side.Blue, UnitKind.Tank, new HexCoord(0, 0));
        var def = Unit.FullStrength(new UnitId(2), Side.Red,  UnitKind.Tank, new HexCoord(8, 0));
        var state = OpenState(atk, def);

        var action = await new GreedyAiStrategy().ChooseNextActionAsync(state, Side.Blue);

        Assert.Equal(StrategyActionKind.Move, action.Kind);
        Assert.Equal(atk.Id, action.UnitId);
        // Should move east (closer to defender at (8,0)).
        Assert.True(action.Hex.Q > 0);
    }

    [Fact]
    public async Task Returns_EndTurn_when_no_unit_has_any_action()
    {
        var atk = Unit.FullStrength(new UnitId(1), Side.Blue, UnitKind.Tank, new HexCoord(0, 0))
            with { HasMoved = true, HasAttacked = true };
        var state = OpenState(atk);

        var action = await new GreedyAiStrategy().ChooseNextActionAsync(state, Side.Blue);

        Assert.Equal(StrategyActionKind.EndTurn, action.Kind);
    }
}
