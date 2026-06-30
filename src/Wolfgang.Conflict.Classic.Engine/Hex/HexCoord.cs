namespace Wolfgang.Conflict.Classic.Engine.Hex;

/// <summary>
/// A hex on a flat-top hex grid, addressed by axial coordinates <c>(Q, R)</c>.
/// </summary>
/// <remarks>
/// <para>
/// Axial coordinates use two of the three cube axes. The implicit third
/// coordinate is <c>S = -Q - R</c>, used here only for distance calculations.
/// </para>
/// <para>
/// Neighbor and distance math is orientation-independent in axial space —
/// flat-top vs pointy-top only affects how a coord is mapped to pixels for
/// rendering. This project is flat-top by convention.
/// </para>
/// </remarks>
public readonly record struct HexCoord(int Q, int R)
{
    /// <summary>The origin hex <c>(0, 0)</c>.</summary>
    public static HexCoord Zero => new(0, 0);

    /// <summary>The implicit cube-coordinate <c>S</c> axis: <c>-Q - R</c>.</summary>
    public int S => -Q - R;

    /// <summary>Returns the hex one step in the given direction.</summary>
    public HexCoord Neighbor(HexDirection direction)
    {
        var (dq, dr) = HexDirections.Offset(direction);
        return new HexCoord(Q + dq, R + dr);
    }

    /// <summary>
    /// Returns the hex distance between this hex and <paramref name="other"/>,
    /// measured in the minimum number of single-step moves.
    /// </summary>
    public int DistanceTo(HexCoord other)
    {
        var dq = Q - other.Q;
        var dr = R - other.R;
        var ds = S - other.S;
        return (Math.Abs(dq) + Math.Abs(dr) + Math.Abs(ds)) / 2;
    }

    /// <summary>
    /// Returns all hexes within <paramref name="radius"/> steps of this hex,
    /// including the hex itself. Radius must be non-negative.
    /// </summary>
    /// <param name="radius">Maximum hex-distance from this hex. Must be non-negative.</param>
    /// <returns>Every hex at distance &lt;= <paramref name="radius"/> from this hex.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="radius"/> is negative.
    /// </exception>
    public IEnumerable<HexCoord> WithinRange(int radius)
    {
        if (radius < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), radius, "Radius must be non-negative.");
        }

        for (var dq = -radius; dq <= radius; dq++)
        {
            var rMin = Math.Max(-radius, -dq - radius);
            var rMax = Math.Min(radius, -dq + radius);
            for (var dr = rMin; dr <= rMax; dr++)
            {
                yield return new HexCoord(Q + dq, R + dr);
            }
        }
    }

    /// <summary>
    /// Returns the six hexes that share an edge with this hex.
    /// </summary>
    public IEnumerable<HexCoord> Neighbors()
    {
        yield return Neighbor(HexDirection.East);
        yield return Neighbor(HexDirection.NorthEast);
        yield return Neighbor(HexDirection.NorthWest);
        yield return Neighbor(HexDirection.West);
        yield return Neighbor(HexDirection.SouthWest);
        yield return Neighbor(HexDirection.SouthEast);
    }

    /// <inheritdoc/>
    public override string ToString() => $"({Q},{R})";
}
