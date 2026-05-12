using Wolfgang.Conflict.Nes.Engine.Map;

namespace Wolfgang.Conflict.Nes.Engine.Units;

/// <summary>
/// Read-only stat tables for each <see cref="UnitKind"/>. Values are sourced
/// from the 1991 NES <c>Conflict</c> instruction manual (Vic Tokai).
/// </summary>
/// <remarks>
/// MVP roster picks one representative model per category:
/// Infantry = "The Liberator", Tank = M1A1 Abrams, Helicopter = AH-1S Cobra,
/// Fighter = F-4E Phantom II. Additional models per category land in a later
/// milestone.
/// </remarks>
public static class UnitStats
{
    /// <summary>Maximum hit points ("LIFE") for every unit at full strength.</summary>
    public const int MaxHitPoints = 15;

    /// <summary>Per-unit static descriptor; numbers match the manual's stat tables.</summary>
    /// <param name="Kind">The unit kind these stats describe.</param>
    /// <param name="MovementDomain">How this unit pays terrain costs.</param>
    /// <param name="MovementPoints">Manual "MOVING": hexes per turn at full fuel.</param>
    /// <param name="MaxFuel">Manual "FUEL": fuel tank capacity (drains by use only).</param>
    /// <param name="MaxAmmo">Manual "SHELL": special-weapon ammo capacity. Standard weapon has unlimited ammo.</param>
    /// <param name="ProductionCost">Manual "F.P." (Fame Points) cost to produce.</param>
    /// <param name="CanCapture">Whether this unit can capture towns and airports.</param>
    /// <param name="HasManeuver5">
    /// True if this unit can select Defense Maneuver 5 in NORMAL battle mode
    /// (manual: "higher-end units have 6 maneuvers, lower-end units have 5").
    /// Not consumed by AUTO mode; metadata for the deferred NORMAL battle UI.
    /// </param>
    public sealed record Stats(
        UnitKind Kind,
        MovementDomain MovementDomain,
        int MovementPoints,
        int MaxFuel,
        int MaxAmmo,
        int ProductionCost,
        bool CanCapture,
        bool HasManeuver5);

    private static readonly Stats[] Table =
    [
        // Source: "The Liberator" infantryman, manual p. 25 area
        new Stats(UnitKind.Infantry,   MovementDomain.Foot,       MovementPoints:  4, MaxFuel: 10, MaxAmmo:  8, ProductionCost:  600, CanCapture: true,  HasManeuver5: false),
        // Source: M1A1 Abrams, manual p. 22 area
        new Stats(UnitKind.Tank,       MovementDomain.Tread,      MovementPoints:  5, MaxFuel:  8, MaxAmmo: 14, ProductionCost: 6000, CanCapture: false, HasManeuver5: true),
        // Source: AH-1S Huey Cobra, manual p. 21 area
        new Stats(UnitKind.Helicopter, MovementDomain.Helicopter, MovementPoints:  7, MaxFuel:  5, MaxAmmo:  6, ProductionCost: 2400, CanCapture: false, HasManeuver5: false),
        // Source: F-4E Phantom II, manual p. 20
        new Stats(UnitKind.Fighter,    MovementDomain.Fighter,    MovementPoints: 10, MaxFuel:  6, MaxAmmo:  6, ProductionCost: 6300, CanCapture: false, HasManeuver5: true),
    ];

    /// <summary>Looks up the static stats for a unit kind.</summary>
    /// <param name="kind">The unit kind to look up.</param>
    /// <returns>The descriptor for the requested kind.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not a recognized unit kind.</exception>
    public static Stats For(UnitKind kind)
    {
        var index = (int)kind;
        if ((uint)index >= (uint)Table.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown unit kind.");
        }

        return Table[index];
    }

    /// <summary>
    /// Returns the cost (in movement points) to enter a hex with the given
    /// terrain and building, or <see langword="null"/> if it is impassable to
    /// the unit kind. Buildings override the underlying terrain.
    /// </summary>
    public static int? TerrainCost(UnitKind kind, Terrain terrain, BuildingKind? building)
    {
        var stats = For(kind);

        // Buildings (when present) take precedence over the underlying terrain.
        if (building is { } bk)
        {
            return BuildingCost(stats.MovementDomain, bk);
        }

        return stats.MovementDomain switch
        {
            MovementDomain.Foot or MovementDomain.Tread => GroundCost(terrain),
            MovementDomain.Helicopter or MovementDomain.Fighter => 1,
            _ => null,
        };
    }

    private static int? GroundCost(Terrain terrain) => terrain switch
    {
        Terrain.Plains => 1,
        Terrain.Road => 1,
        Terrain.Beach => 2,
        Terrain.Bridge => 2,
        Terrain.Forest => 2,
        Terrain.Mountain => 3,
        Terrain.River => null,
        Terrain.Sea => null,
        Terrain.Reef => null,
        _ => null,
    };

    private static int? BuildingCost(MovementDomain domain, BuildingKind building) => building switch
    {
        // No unit may move through a factory per the manual.
        BuildingKind.Factory => null,
        // Towns, HQs, airbases and ports are passable for any unit that can use the underlying domain.
        BuildingKind.City => 1,
        BuildingKind.Hq => 1,
        BuildingKind.Airbase => 1,
        BuildingKind.Port => domain is MovementDomain.Helicopter or MovementDomain.Fighter ? 1 : null,
        _ => null,
    };

    /// <summary>
    /// Returns the defense bonus a unit gets when defending from a hex with
    /// the given terrain and (optional) building. Building bonuses override
    /// terrain bonuses since the unit occupies the structure.
    /// </summary>
    public static int DefenseBonus(Terrain terrain, BuildingKind? building) => building switch
    {
        BuildingKind.City => 2,
        BuildingKind.Hq => 3,
        BuildingKind.Airbase => 0,
        BuildingKind.Port => 0,
        BuildingKind.Factory => 0,
        null => terrain switch
        {
            Terrain.Forest => 3,
            Terrain.Mountain => 3,
            Terrain.Bridge => 2,
            _ => 0,
        },
        _ => 0,
    };

    /// <summary>
    /// Base attack value for <paramref name="attacker"/> shooting at
    /// <paramref name="defender"/> at full strength on a 0&#x2013;15 scale.
    /// Zero means the attacker cannot engage the defender at all.
    /// </summary>
    /// <remarks>
    /// MVP simplification of the manual's 18&#xD7;18 RELATIONS chart, with the
    /// rock-paper-scissors loop preserved for the 4 MVP categories:
    /// Helicopter &#x2192; Tank &#x2192; Fighter &#x2192; Helicopter, and
    /// Infantry on the periphery (strong vs Infantry only).
    /// </remarks>
    public static int BaseAttack(UnitKind attacker, UnitKind defender)
    {
        return (attacker, defender) switch
        {
            (UnitKind.Infantry,   UnitKind.Infantry)   => 6,
            (UnitKind.Infantry,   UnitKind.Tank)       => 1,
            (UnitKind.Infantry,   UnitKind.Helicopter) => 0,
            (UnitKind.Infantry,   UnitKind.Fighter)    => 0,

            (UnitKind.Tank,       UnitKind.Infantry)   => 10,
            (UnitKind.Tank,       UnitKind.Tank)       => 6,
            (UnitKind.Tank,       UnitKind.Helicopter) => 2,
            (UnitKind.Tank,       UnitKind.Fighter)    => 0,

            (UnitKind.Helicopter, UnitKind.Infantry)   => 9,
            (UnitKind.Helicopter, UnitKind.Tank)       => 7,
            (UnitKind.Helicopter, UnitKind.Helicopter) => 6,
            (UnitKind.Helicopter, UnitKind.Fighter)    => 1,

            (UnitKind.Fighter,    UnitKind.Infantry)   => 0,
            (UnitKind.Fighter,    UnitKind.Tank)       => 0,
            (UnitKind.Fighter,    UnitKind.Helicopter) => 10,
            (UnitKind.Fighter,    UnitKind.Fighter)    => 6,

            _ => 0,
        };
    }
}
