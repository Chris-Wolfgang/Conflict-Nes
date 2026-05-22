using Wolfgang.Conflict.Nes.Engine.Hex;
using Wolfgang.Conflict.Nes.Engine.Players;
using Wolfgang.Conflict.Nes.Engine.Rules;
using Wolfgang.Conflict.Nes.Engine.Units;
using Xunit;

namespace Wolfgang.Conflict.Nes.Engine.Tests.Rules;

public class AttackRulesTests
{
    private static Unit Make(int id, Side side, UnitTypeDefinition type, HexCoord coord, bool hasAttacked = false)
        => Unit.FullStrength(new UnitId(id), side, type, coord) with { HasAttacked = hasAttacked };

    [Fact]
    public void CanAttack_adjacent_enemy_with_positive_damage_is_legal()
    {
        var atk = Make(1, Side.Blue, TestCatalog.Tank,     new HexCoord(0, 0));
        var def = Make(2, Side.Red,  TestCatalog.Infantry, new HexCoord(1, 0));

        Assert.True(AttackRules.CanAttack(atk, def));
    }

    [Fact]
    public void CanAttack_same_side_is_illegal()
    {
        var atk = Make(1, Side.Blue, TestCatalog.Tank,     new HexCoord(0, 0));
        var def = Make(2, Side.Blue, TestCatalog.Infantry, new HexCoord(1, 0));

        Assert.False(AttackRules.CanAttack(atk, def));
    }

    [Fact]
    public void CanAttack_non_adjacent_is_illegal()
    {
        var atk = Make(1, Side.Blue, TestCatalog.Tank,     new HexCoord(0, 0));
        var def = Make(2, Side.Red,  TestCatalog.Infantry, new HexCoord(3, 0));

        Assert.False(AttackRules.CanAttack(atk, def));
    }

    [Fact]
    public void CanAttack_already_attacked_is_illegal()
    {
        var atk = Make(1, Side.Blue, TestCatalog.Tank, new HexCoord(0, 0), hasAttacked: true);
        var def = Make(2, Side.Red,  TestCatalog.Tank, new HexCoord(1, 0));

        Assert.False(AttackRules.CanAttack(atk, def));
    }

    [Fact]
    public void CanAttack_zero_base_damage_pairing_is_illegal()
    {
        // A battle tank cannot engage an aircraft (cannot track it).
        var atk = Make(1, Side.Blue, TestCatalog.Tank,    new HexCoord(0, 0));
        var def = Make(2, Side.Red,  TestCatalog.Fighter, new HexCoord(1, 0));

        Assert.False(AttackRules.CanAttack(atk, def));
    }
}
