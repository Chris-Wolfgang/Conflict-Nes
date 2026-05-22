using Wolfgang.Conflict.Nes.Engine.Players;

namespace Wolfgang.Conflict.Nes.Engine.Units;

/// <summary>
/// An immutable, data-driven definition of one unit model (e.g. the M1A1
/// Abrams). Loaded from the <c>unit-types.json</c> catalog. All units share
/// <see cref="UnitStats.MaxHitPoints"/> for LIFE; everything else is per-model.
/// </summary>
/// <param name="Id">Stable lower-case identifier, e.g. <c>"m1a1"</c>.</param>
/// <param name="Name">Human-readable display name, e.g. <c>"M1A1 Abrams"</c>.</param>
/// <param name="ShortName">Compact designation for on-board tiles, e.g. <c>"M1A1"</c>.</param>
/// <param name="Category">Functional role used by combat, movement and AI.</param>
/// <param name="Side">
/// The faction whose roster this model belongs to, or <see langword="null"/>
/// if usable by either side.
/// </param>
/// <param name="MovementDomain">How the unit pays terrain costs.</param>
/// <param name="MovementPoints">Manual "MOVING": hexes per turn.</param>
/// <param name="MaxFuel">Manual "FUEL": turns of movement before refuelling.</param>
/// <param name="MaxAmmo">Manual "SHELL": special-weapon ammunition capacity.</param>
/// <param name="ProductionCost">Manual "F.P." cost to build.</param>
/// <param name="HasManeuver5">Whether the unit can use Defense Maneuver 5.</param>
/// <param name="CanCapture">Whether the unit can capture buildings.</param>
/// <param name="StandardWeapon">Name of the unlimited-ammo standard weapon.</param>
/// <param name="SpecialWeapon">Name of the SHELL-limited special weapon, or null.</param>
public sealed record UnitTypeDefinition(
    string Id,
    string Name,
    string ShortName,
    UnitCategory Category,
    Side? Side,
    MovementDomain MovementDomain,
    int MovementPoints,
    int MaxFuel,
    int MaxAmmo,
    int ProductionCost,
    bool HasManeuver5,
    bool CanCapture,
    string StandardWeapon,
    string? SpecialWeapon);
