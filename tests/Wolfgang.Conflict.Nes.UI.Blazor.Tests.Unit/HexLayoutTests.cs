using Wolfgang.Conflict.Nes.Engine.Hex;
using Wolfgang.Conflict.Nes.UI.Blazor.Rendering;
using Xunit;

namespace Wolfgang.Conflict.Nes.UI.Blazor.Tests.Unit;

public class HexLayoutTests
{
    [Fact]
    public void Centre_for_origin_is_one_hex_in_from_top_left()
    {
        var c = HexLayout.Centre(new HexCoord(0, 0));

        Assert.Equal(HexLayout.HexSize, c.X, precision: 3);
        Assert.Equal(HexLayout.HexSize, c.Y, precision: 3);
    }

    [Fact]
    public void Centre_advances_by_column_stride_in_q()
    {
        var c0 = HexLayout.Centre(new HexCoord(0, 0));
        var c1 = HexLayout.Centre(new HexCoord(1, 0));

        Assert.Equal(c0.X + HexLayout.ColumnStride, c1.X, precision: 3);
    }

    [Fact]
    public void BoardSize_for_mission01_dimensions_is_positive()
    {
        var (w, h) = HexLayout.BoardSize(12, 10);

        Assert.True(w > 0);
        Assert.True(h > 0);
    }

    [Fact]
    public void PolygonPoints_returns_six_comma_separated_pairs()
    {
        var pts = HexLayout.PolygonPoints((100, 100));

        var pairs = pts.Split(' ');
        Assert.Equal(6, pairs.Length);
        Assert.All(pairs, p => Assert.Contains(',', p));
    }
}
