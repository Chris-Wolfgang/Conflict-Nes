using Wolfgang.Conflict.Nes.Engine.Game;
using Wolfgang.Conflict.Nes.Engine.Hex;
using Wolfgang.Conflict.Nes.Engine.Map;
using Wolfgang.Conflict.Nes.Engine.Players;
using Wolfgang.Conflict.Nes.Engine.Units;
using Xunit;

namespace Wolfgang.Conflict.Nes.Engine.Tests.Game;

public class AttackUnitTests
{
    private static GameState BuildPlainsState(params Unit[] units)
    {
        var map = new MapDefinition("plains", 10, 5,
            MapDefinition.EnumerateCoords(10, 5)
                .Select(c => new Tile(c, Terrain.Plains, Building: null, Owner: null)));
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
            randomSeed: 7);
    }

    [Fact]
    public void AttackUnit_marks_attacker_as_having_attacked()
    {
        var engine = new GameEngine();
        var atk = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Tank,     new HexCoord(0, 0));
        var def = Unit.FullStrength(new UnitId(2), Side.Red,  TestCatalog.Infantry, new HexCoord(1, 0));
        var state = BuildPlainsState(atk, def);

        var next = engine.AttackUnit(state, atk.Id, def.Id);

        Assert.True(next.Units[atk.Id].HasAttacked);
        Assert.Equal(atk.Ammo - 1, next.Units[atk.Id].Ammo);
    }

    [Fact]
    public void AttackUnit_destroying_defender_awards_winner_and_penalizes_loser()
    {
        var engine = new GameEngine();
        // Wounded infantry dies to a Tank hit.
        var atk = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Tank,     new HexCoord(0, 0));
        var def = Unit.FullStrength(new UnitId(2), Side.Red,  TestCatalog.Infantry, new HexCoord(1, 0)) with { HitPoints = 3 };
        var state = BuildPlainsState(atk, def);

        var next = engine.AttackUnit(state, atk.Id, def.Id);

        Assert.False(next.Units.ContainsKey(def.Id));
        // Tank vs Infantry => TotalVictory => +300 winner; Infantry cost 600
        // => 300 loser penalty, but F.P. is floored at zero (Red started at 0).
        Assert.Equal(300, next.Funds[Side.Blue]);
        Assert.Equal(0, next.Funds[Side.Red]);
    }

    [Fact]
    public void AttackUnit_loser_funds_never_go_below_zero()
    {
        var engine = new GameEngine();
        var atk = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Tank,     new HexCoord(0, 0));
        var def = Unit.FullStrength(new UnitId(2), Side.Red,  TestCatalog.Infantry, new HexCoord(1, 0)) with { HitPoints = 1 };
        var state = BuildPlainsState(atk, def); // Red starts at 0 F.P.

        var next = engine.AttackUnit(state, atk.Id, def.Id);

        Assert.True(next.Funds[Side.Red] >= 0);
        Assert.Equal(0, next.Funds[Side.Red]);
    }

    [Fact]
    public void AttackUnit_illegal_pairing_throws()
    {
        // Tank vs Fighter has zero base damage in our table (tank cannot
        // track aircraft), so the attack is rejected.
        var engine = new GameEngine();
        var atk = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Tank,    new HexCoord(0, 0));
        var def = Unit.FullStrength(new UnitId(2), Side.Red,  TestCatalog.Fighter, new HexCoord(1, 0));
        var state = BuildPlainsState(atk, def);

        Assert.Throws<InvalidOperationException>(() => engine.AttackUnit(state, atk.Id, def.Id));
    }

    [Fact]
    public void AttackUnit_not_your_turn_throws()
    {
        var engine = new GameEngine();
        var redAtk = Unit.FullStrength(new UnitId(1), Side.Red,  TestCatalog.Tank,     new HexCoord(0, 0));
        var def    = Unit.FullStrength(new UnitId(2), Side.Blue, TestCatalog.Infantry, new HexCoord(1, 0));
        var state  = BuildPlainsState(redAtk, def); // NextToAct = Blue

        Assert.Throws<InvalidOperationException>(() => engine.AttackUnit(state, redAtk.Id, def.Id));
    }

    [Fact]
    public void RandomSeed_advances_after_an_attack()
    {
        var engine = new GameEngine();
        var atk = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Tank,     new HexCoord(0, 0));
        var def = Unit.FullStrength(new UnitId(2), Side.Red,  TestCatalog.Infantry, new HexCoord(1, 0));
        var state = BuildPlainsState(atk, def);

        var next = engine.AttackUnit(state, atk.Id, def.Id);

        Assert.NotEqual(state.RandomSeed, next.RandomSeed);
    }
}
