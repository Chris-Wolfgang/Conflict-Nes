namespace Wolfgang.Conflict.Classic.Engine.Map;

/// <summary>
/// The terrain category of a hex. Terrain governs movement cost, defense bonus,
/// and what unit kinds may enter.
/// </summary>
public enum Terrain
{
    /// <summary>Open ground; cheap to cross, no defense bonus.</summary>
    Plains = 0,

    /// <summary>Wooded ground; slow to cross, defense bonus.</summary>
    Forest = 1,

    /// <summary>Impassable to ground units; flyers ignore.</summary>
    Mountain = 2,

    /// <summary>Flowing water; passable only at a <see cref="Bridge"/>.</summary>
    River = 4,

    /// <summary>A steel bridge across a <see cref="River"/>; passable to ground at cost 2.</summary>
    Bridge = 5,

    /// <summary>Shallow water near shore — passable to ground at extra cost (2 MP).</summary>
    Shoal = 6,

    /// <summary>Open water; passable only by sea/air units.</summary>
    Sea = 7,

    /// <summary>Hazardous sea hex; impassable to most sea units.</summary>
    Reef = 8,
}
