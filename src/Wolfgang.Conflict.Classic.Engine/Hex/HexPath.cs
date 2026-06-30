using System.Collections;

namespace Wolfgang.Conflict.Classic.Engine.Hex;

/// <summary>
/// An ordered sequence of <see cref="HexCoord"/>s representing a route between
/// two hexes. The first element is the start hex, the last is the destination.
/// </summary>
public sealed class HexPath : IReadOnlyList<HexCoord>
{
    private readonly IReadOnlyList<HexCoord> _hexes;

    /// <summary>The summed terrain cost of stepping into each hex after the start.</summary>
    public int TotalCost { get; }

    /// <summary>
    /// Creates a path. <paramref name="hexes"/> must contain at least one hex.
    /// </summary>
    public HexPath(IReadOnlyList<HexCoord> hexes, int totalCost)
    {
        if (hexes is null)
        {
            throw new ArgumentNullException(nameof(hexes));
        }
        if (hexes.Count == 0)
        {
            throw new ArgumentException("Path must contain at least one hex.", nameof(hexes));
        }
        if (totalCost < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalCost), totalCost, "Cost must be non-negative.");
        }

        _hexes = hexes;
        TotalCost = totalCost;
    }

    /// <summary>The start hex of the path.</summary>
    public HexCoord Start => _hexes[0];

    /// <summary>The destination hex of the path.</summary>
    public HexCoord Destination => _hexes[_hexes.Count - 1];

    /// <inheritdoc/>
    public int Count => _hexes.Count;

    /// <inheritdoc/>
    public HexCoord this[int index] => _hexes[index];

    /// <inheritdoc/>
    public IEnumerator<HexCoord> GetEnumerator() => _hexes.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
