using Wolfgang.Conflict.Classic.Engine.Game;
using Wolfgang.Conflict.Classic.Engine.Hex;
using Wolfgang.Conflict.Classic.Engine.Map;
using Wolfgang.Conflict.Classic.Engine.Players;
using Wolfgang.Conflict.Classic.Engine.Rules;
using Wolfgang.Conflict.Classic.Engine.Units;
using Xunit;

namespace Wolfgang.Conflict.Classic.Engine.Tests.Rules;

public class VictoryRulesTests
{
    private static GameState StateWith(params Unit[] units)
    {
        var map = new MapDefinition("plains", 4, 2,
            MapDefinition.EnumerateCoords(4, 2)
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
    public void With_both_commanders_alive_no_winner()
    {
        var blue = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Tank, new HexCoord(0, 0), isCommander: true);
        var red = Unit.FullStrength(new UnitId(2), Side.Red, TestCatalog.Tank, new HexCoord(3, 0), isCommander: true);

        Assert.Null(VictoryRules.DetermineWinner(StateWith(blue, red)));
    }

    [Fact]
    public void Blue_wins_when_red_commander_dead()
    {
        var blue = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Tank, new HexCoord(0, 0), isCommander: true);
        // Red has only a non-commander unit alive.
        var redGrunt = Unit.FullStrength(new UnitId(2), Side.Red, TestCatalog.Infantry, new HexCoord(3, 0));

        Assert.Equal(Side.Blue, VictoryRules.DetermineWinner(StateWith(blue, redGrunt)));
    }

    [Fact]
    public void Red_wins_when_blue_routed()
    {
        var red = Unit.FullStrength(new UnitId(1), Side.Red, TestCatalog.Tank, new HexCoord(0, 0), isCommander: true);

        Assert.Equal(Side.Red, VictoryRules.DetermineWinner(StateWith(red)));
    }
}
