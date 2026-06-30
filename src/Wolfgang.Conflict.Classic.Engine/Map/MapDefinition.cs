using Wolfgang.Conflict.Classic.Engine.Hex;

namespace Wolfgang.Conflict.Classic.Engine.Map;

/// <summary>
/// An immutable, rectangular axial-offset hex map. Valid coordinates form a
/// rectangle on screen: column <c>Q</c> runs from <c>0</c> to
/// <c>Width - 1</c>, and the valid <c>R</c> range for column <c>Q</c> is
/// <c>[-floor(Q/2), Height - floor(Q/2))</c>.
/// </summary>
public sealed class MapDefinition
{
    private readonly IReadOnlyDictionary<HexCoord, Tile> _tiles;

    /// <summary>Human-readable name of the map / mission.</summary>
    public string Name { get; }

    /// <summary>Number of columns. Must be positive.</summary>
    public int Width { get; }

    /// <summary>Number of rows. Must be positive.</summary>
    public int Height { get; }

    /// <summary>All tiles on the map, keyed by axial coordinate.</summary>
    public IReadOnlyDictionary<HexCoord, Tile> Tiles => _tiles;

    /// <summary>Constructs a map; <paramref name="tiles"/> must cover every valid coordinate exactly once.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="tiles"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="width"/> or <paramref name="height"/> is non-positive.</exception>
    /// <exception cref="ArgumentException">
    /// A tile is outside the map's bounds, a coordinate is duplicated, or some valid coordinate is missing a tile.
    /// </exception>
    public MapDefinition(string name, int width, int height, IEnumerable<Tile> tiles)
    {
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }
        if (tiles is null)
        {
            throw new ArgumentNullException(nameof(tiles));
        }
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be positive.");
        }
        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be positive.");
        }

        Name = name;
        Width = width;
        Height = height;

        var indexed = new Dictionary<HexCoord, Tile>();
        foreach (var tile in tiles)
        {
            if (!Contains(tile.Coord))
            {
                throw new ArgumentException($"Tile {tile.Coord} is outside the rectangular bounds {width}x{height}.", nameof(tiles));
            }
            if (indexed.ContainsKey(tile.Coord))
            {
                throw new ArgumentException($"Duplicate tile at {tile.Coord}.", nameof(tiles));
            }
            indexed.Add(tile.Coord, tile);
        }

        var expectedCount = ExpectedTileCount(width, height);
        if (indexed.Count != expectedCount)
        {
            throw new ArgumentException($"Map has {indexed.Count} tiles but rectangular bounds {width}x{height} require {expectedCount}.", nameof(tiles));
        }

        _tiles = indexed;
    }

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="coord"/> falls within
    /// the rectangular axial-offset bounds of this map.
    /// </summary>
    public bool Contains(HexCoord coord) => IsInsideRectangularBounds(coord, Width, Height);

    /// <summary>
    /// Yields every valid hex coordinate for a rectangular axial-offset map of
    /// the given dimensions, in column-major order.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="width"/> or <paramref name="height"/> is non-positive.</exception>
    public static IEnumerable<HexCoord> EnumerateCoords(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be positive.");
        }
        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be positive.");
        }

        for (var q = 0; q < width; q++)
        {
            var rOffset = q / 2;
            for (var r = -rOffset; r < height - rOffset; r++)
            {
                yield return new HexCoord(q, r);
            }
        }
    }

    private static bool IsInsideRectangularBounds(HexCoord coord, int width, int height)
    {
        if (coord.Q < 0 || coord.Q >= width)
        {
            return false;
        }

        var rOffset = coord.Q / 2;
        return coord.R >= -rOffset && coord.R < height - rOffset;
    }

    private static int ExpectedTileCount(int width, int height) => width * height;
}
