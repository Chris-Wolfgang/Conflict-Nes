using System.Globalization;
using Wolfgang.Conflict.Nes.Engine.Hex;

namespace Wolfgang.Conflict.Nes.UI.Blazor.Rendering;

/// <summary>
/// Pixel-coordinate math for rendering flat-top axial hexes on an SVG canvas.
/// </summary>
public static class HexLayout
{
    /// <summary>Distance from a hex centre to a vertex, in CSS px.</summary>
    public const double HexSize = 30.0;

    /// <summary>
    /// Uniform scale applied to unit tiles so they sit inside the hex with a
    /// margin rather than filling it edge-to-edge. Preserves the tile's
    /// 1 : √3 width-to-height ratio.
    /// </summary>
    public const double UnitTileScale = 0.78;

    /// <summary>Total width of one flat-top hex (vertex to vertex).</summary>
    public static double HexWidth => 2.0 * HexSize;

    /// <summary>Total height of one flat-top hex (edge to edge).</summary>
    public static double HexHeight => Math.Sqrt(3.0) * HexSize;

    /// <summary>Horizontal step between adjacent column centres.</summary>
    public static double ColumnStride => 1.5 * HexSize;

    /// <summary>
    /// Returns the screen-pixel centre of the given axial hex, in the
    /// coordinate space where (0,0) is the top-left of the board area
    /// (after the padding applied by <see cref="BoardSize"/>).
    /// </summary>
    public static (double X, double Y) Centre(HexCoord coord)
    {
        var x = HexSize + ColumnStride * coord.Q;
        var y = HexSize + HexHeight * (coord.R + coord.Q / 2.0);
        // Translate so that the lowest valid Y (which can be negative for
        // wide maps) sits comfortably below the top edge.
        return (x, y);
    }

    /// <summary>
    /// Returns the total SVG board dimensions needed to draw a map with the
    /// given rectangular axial-offset bounds.
    /// </summary>
    public static (double Width, double Height) BoardSize(int mapWidth, int mapHeight)
    {
        // Width: leftmost vertex of Q=0 column to rightmost vertex of last column.
        var width = HexSize + ColumnStride * (mapWidth - 1) + HexSize;
        var height = HexSize + HexHeight * mapHeight;
        return (width, height);
    }

    /// <summary>
    /// Returns the six vertices of the flat-top hex around <paramref name="centre"/>
    /// formatted as an SVG "points" string.
    /// </summary>
    public static string PolygonPoints((double X, double Y) centre)
    {
        Span<(double X, double Y)> v = stackalloc (double, double)[6];
        for (var i = 0; i < 6; i++)
        {
            var angle = Math.PI / 180.0 * (60.0 * i);
            v[i] = (centre.X + HexSize * Math.Cos(angle),
                    centre.Y + HexSize * Math.Sin(angle));
        }

        var ci = CultureInfo.InvariantCulture;
        return string.Join(' ',
            $"{v[0].X.ToString("F2", ci)},{v[0].Y.ToString("F2", ci)}",
            $"{v[1].X.ToString("F2", ci)},{v[1].Y.ToString("F2", ci)}",
            $"{v[2].X.ToString("F2", ci)},{v[2].Y.ToString("F2", ci)}",
            $"{v[3].X.ToString("F2", ci)},{v[3].Y.ToString("F2", ci)}",
            $"{v[4].X.ToString("F2", ci)},{v[4].Y.ToString("F2", ci)}",
            $"{v[5].X.ToString("F2", ci)},{v[5].Y.ToString("F2", ci)}");
    }
}
