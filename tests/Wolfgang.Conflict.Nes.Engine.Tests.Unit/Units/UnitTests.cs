using Wolfgang.Conflict.Nes.Engine.Hex;
using Wolfgang.Conflict.Nes.Engine.Players;
using Wolfgang.Conflict.Nes.Engine.Units;
using Xunit;

namespace Wolfgang.Conflict.Nes.Engine.Tests.Units;

public class UnitTests
{
    [Fact]
    public void FullStrength_initializes_from_stats_table()
    {
        var stats = UnitStats.For(UnitKind.Tank);

        var unit = Unit.FullStrength(new UnitId(1), Side.Blue, UnitKind.Tank, new HexCoord(2, 3));

        Assert.Equal(new UnitId(1), unit.Id);
        Assert.Equal(Side.Blue, unit.Side);
        Assert.Equal(UnitKind.Tank, unit.Kind);
        Assert.Equal(new HexCoord(2, 3), unit.Coord);
        Assert.Equal(UnitStats.MaxHitPoints, unit.HitPoints);
        Assert.Equal(stats.MaxFuel, unit.Fuel);
        Assert.Equal(stats.MaxAmmo, unit.Ammo);
        Assert.Equal(stats.MovementPoints, unit.MovesRemaining);
        Assert.False(unit.HasAttacked);
    }

    [Theory]
    [InlineData(10, false)]
    [InlineData(1, false)]
    [InlineData(0, true)]
    [InlineData(-1, true)]
    public void IsDead_reports_hit_point_state(int hp, bool dead)
    {
        var unit = Unit.FullStrength(new UnitId(1), Side.Red, UnitKind.Infantry, HexCoord.Zero) with { HitPoints = hp };

        Assert.Equal(dead, unit.IsDead);
    }
}
