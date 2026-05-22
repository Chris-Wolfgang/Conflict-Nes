using Wolfgang.Conflict.Nes.Engine.Combat;
using Wolfgang.Conflict.Nes.Engine.Game;
using Wolfgang.Conflict.Nes.Engine.Hex;
using Wolfgang.Conflict.Nes.Engine.Map;
using Wolfgang.Conflict.Nes.Engine.Players;
using Wolfgang.Conflict.Nes.Engine.Units;
using Xunit;

namespace Wolfgang.Conflict.Nes.Engine.Tests.Combat;

public class CombatResolverTests
{
    private sealed class FixedRng(int value) : IRandomSource
    {
        public int NextInt(int min, int max) => value;
    }

    private static GameState OpenMapState(params Unit[] units)
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
            randomSeed: 1);
    }

    [Fact]
    public void Tank_attacks_infantry_inflicts_damage_no_counter()
    {
        var atk = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Tank,     new HexCoord(0, 0));
        var def = Unit.FullStrength(new UnitId(2), Side.Red,  TestCatalog.Infantry, new HexCoord(1, 0));
        var state = OpenMapState(atk, def);

        var result = CombatResolver.Resolve(state, atk, def, new FixedRng(0));

        // BattleTank vs Infantry is a TotalVictory matchup => base attack 11;
        // defense 0 (plains), shooter at full HP, roll 0.
        Assert.Equal(11, result.DamageToDefender);
        Assert.False(result.DefenderDestroyed); // 15 HP - 11 = 4
        Assert.True(result.DefenderCountered);  // Infantry can still chip a Tank, so it counters.
    }

    [Fact]
    public void Defender_killed_does_not_counter()
    {
        var atk = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Tank,     new HexCoord(0, 0));
        // Wounded infantry: 5 HP, dies to Tank hit.
        var def = Unit.FullStrength(new UnitId(2), Side.Red,  TestCatalog.Infantry, new HexCoord(1, 0)) with { HitPoints = 5 };
        var state = OpenMapState(atk, def);

        var result = CombatResolver.Resolve(state, atk, def, new FixedRng(0));

        Assert.True(result.DefenderDestroyed);
        Assert.False(result.DefenderCountered);
        Assert.Equal(0, result.DamageToAttacker);
    }

    [Fact]
    public void Fighter_strafes_tank_for_chip_damage_no_counter()
    {
        var atk = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Fighter, new HexCoord(0, 0));
        var def = Unit.FullStrength(new UnitId(2), Side.Red,  TestCatalog.Tank,    new HexCoord(1, 0));
        var state = OpenMapState(atk, def);

        var result = CombatResolver.Resolve(state, atk, def, new FixedRng(0));

        // Fighter base vs Tank = 2 at full HP, plains defense = 0.
        Assert.Equal(2, result.DamageToDefender);
        Assert.False(result.DefenderDestroyed);
        Assert.False(result.DefenderCountered); // tank base vs fighter = 0
    }

    [Fact]
    public void Defense_terrain_reduces_damage()
    {
        // Same fighters but defender is in a forest hex (defense +3).
        var tiles = MapDefinition.EnumerateCoords(10, 5)
            .Select(c => new Tile(c, c == new HexCoord(1, 0) ? Terrain.Forest : Terrain.Plains, Building: null, Owner: null));
        var map = new MapDefinition("mixed", 10, 5, tiles);
        var atk = Unit.FullStrength(new UnitId(1), Side.Blue, TestCatalog.Fighter, new HexCoord(0, 0));
        var def = Unit.FullStrength(new UnitId(2), Side.Red,  TestCatalog.Fighter, new HexCoord(1, 0));
        var dict = new Dictionary<UnitId, Unit> { [atk.Id] = atk, [def.Id] = def };
        var state = new GameState(
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

        var result = CombatResolver.Resolve(state, atk, def, new FixedRng(0));

        // Fighter base vs Fighter = 6, defense = 3, roll = 0 => 3 damage.
        Assert.Equal(3, result.DamageToDefender);
    }
}
