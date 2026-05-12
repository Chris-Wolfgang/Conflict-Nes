namespace Wolfgang.Conflict.Nes.Engine.Hex;

/// <summary>
/// Lookup tables and helpers for the six <see cref="HexDirection"/> values.
/// </summary>
public static class HexDirections
{
    private static readonly (int Q, int R)[] Offsets =
    [
        (+1,  0),
        (+1, -1),
        ( 0, -1),
        (-1,  0),
        (-1, +1),
        ( 0, +1),
    ];

    /// <summary>All six directions in clockwise order starting at East.</summary>
    public static IReadOnlyList<HexDirection> All { get; } =
    [
        HexDirection.East,
        HexDirection.NorthEast,
        HexDirection.NorthWest,
        HexDirection.West,
        HexDirection.SouthWest,
        HexDirection.SouthEast,
    ];

    /// <summary>Returns the axial <c>(Q, R)</c> offset for a direction.</summary>
    /// <param name="direction">The direction to look up.</param>
    /// <returns>The axial offset <c>(Q, R)</c> that this direction applies.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="direction"/> is not one of the six valid <see cref="HexDirection"/> values.
    /// </exception>
    public static (int Q, int R) Offset(HexDirection direction)
    {
        var index = (int)direction;
        if ((uint)index >= (uint)Offsets.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown direction.");
        }

        return Offsets[index];
    }
}
