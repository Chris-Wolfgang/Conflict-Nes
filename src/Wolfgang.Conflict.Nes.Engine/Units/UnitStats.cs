using Wolfgang.Conflict.Nes.Engine.Map;

namespace Wolfgang.Conflict.Nes.Engine.Units;

/// <summary>
/// Shared, non-per-model unit constants and terrain interaction tables.
/// Per-model stats (MOVING, FUEL, SHELL, F.P.) live in the data-driven
/// <see cref="UnitCatalog"/>; matchup combat lives in
/// <c>Combat.RelationsTable</c>.
/// </summary>
public static class UnitStats
{
    /// <summary>Maximum hit points ("LIFE") shared by every unit.</summary>
    public const int MaxHitPoints = 15;

    /// <summary>
    /// Returns the cost (in movement points) for a unit of the given
    /// movement domain to enter a hex with the given terrain and building,
    /// or <see langword="null"/> if it is impassable.
    /// </summary>
    public static int? TerrainCost(MovementDomain domain, Terrain terrain, BuildingKind? building)
    {
        if (building is { } bk)
        {
            return BuildingCost(domain, bk);
        }

        return domain switch
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
        Terrain.Shoal => 2,
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
        BuildingKind.Factory => null,
        BuildingKind.City => 1,
        BuildingKind.Hq => 1,
        BuildingKind.Airbase => 1,
        BuildingKind.Port => domain is MovementDomain.Helicopter or MovementDomain.Fighter ? 1 : null,
        _ => null,
    };

    /// <summary>
    /// Returns the defense bonus a unit gets when defending from a hex with
    /// the given terrain and (optional) building.
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
}
