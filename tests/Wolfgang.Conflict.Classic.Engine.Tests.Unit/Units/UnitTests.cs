using Wolfgang.Conflict.Classic.Engine.Hex;
using Wolfgang.Conflict.Classic.Engine.Players;
using Wolfgang.Conflict.Classic.Engine.Units;
using Xunit;

namespace Wolfgang.Conflict.Classic.Engine.Tests.Units;

public class UnitTests
{
    [Fact]
    public void FullStrength_initializes_from_catalog_definition()
    {
        var type = TestCatalog.Tank;

        var unit = Unit.FullStrength(new UnitId(1), Side.Blue, type, new HexCoord(2, 3));

        Assert.Equal(new UnitId(1), unit.Id);
        Assert.Equal(Side.Blue, unit.Side);
        Assert.Same(type, unit.Type);
        Assert.Equal(UnitCategory.BattleTank, unit.Category);
        Assert.Equal(new HexCoord(2, 3), unit.Coord);
        Assert.Equal(UnitStats.MaxHitPoints, unit.HitPoints);
        Assert.Equal(type.MaxFuel, unit.Fuel);
        Assert.Equal(type.MaxAmmo, unit.Ammo);
        Assert.Equal(type.MovementPoints, unit.MovesRemaining);
        Assert.False(unit.HasMoved);
        Assert.False(unit.HasAttacked);
        Assert.False(unit.HasSupplied);
    }

    [Fact]
    public void FullStrength_null_type_throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => Unit.FullStrength(new UnitId(1), Side.Blue, type: null!, HexCoord.Zero));
    }

    [Theory]
    [InlineData(15, false)]
    [InlineData(1, false)]
    [InlineData(0, true)]
    [InlineData(-1, true)]
    public void IsDead_reports_hit_point_state(int hp, bool dead)
    {
        var unit = Unit.FullStrength(new UnitId(1), Side.Red, TestCatalog.Infantry, HexCoord.Zero) with { HitPoints = hp };

        Assert.Equal(dead, unit.IsDead);
    }
}
