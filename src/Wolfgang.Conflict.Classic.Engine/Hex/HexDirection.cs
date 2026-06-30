namespace Wolfgang.Conflict.Classic.Engine.Hex;

/// <summary>
/// The six edge-adjacent directions from a flat-top hex.
/// </summary>
public enum HexDirection
{
    /// <summary>Directly east; axial offset <c>(+1, 0)</c>.</summary>
    East = 0,

    /// <summary>Up and to the east; axial offset <c>(+1, -1)</c>.</summary>
    NorthEast = 1,

    /// <summary>Up and to the west; axial offset <c>(0, -1)</c>.</summary>
    NorthWest = 2,

    /// <summary>Directly west; axial offset <c>(-1, 0)</c>.</summary>
    West = 3,

    /// <summary>Down and to the west; axial offset <c>(-1, +1)</c>.</summary>
    SouthWest = 4,

    /// <summary>Down and to the east; axial offset <c>(0, +1)</c>.</summary>
    SouthEast = 5,
}
