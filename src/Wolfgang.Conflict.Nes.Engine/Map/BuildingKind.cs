namespace Wolfgang.Conflict.Nes.Engine.Map;

/// <summary>
/// The category of a fixed structure on a tile. Buildings can be owned by a
/// <see cref="Players.Side"/>, captured by infantry, and may produce or repair
/// units depending on their kind.
/// </summary>
public enum BuildingKind
{
    /// <summary>Generic city; provides income and produces ground units.</summary>
    City = 0,

    /// <summary>Headquarters; loss of the HQ ends the game for its owner.</summary>
    Hq = 1,

    /// <summary>Airbase; produces and repairs air units.</summary>
    Airbase = 2,

    /// <summary>Port; produces and repairs sea units.</summary>
    Port = 3,

    /// <summary>Factory; produces armoured ground units.</summary>
    Factory = 4,
}
