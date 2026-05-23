using Wolfgang.Conflict.Nes.Engine.Hex;
using Wolfgang.Conflict.Nes.Engine.Units;

namespace Wolfgang.Conflict.Nes.UI.Blazor.Services;

/// <summary>
/// Describes an attack that the player has chosen but not yet confirmed.
/// Exactly one of <see cref="TargetUnitId"/> or
/// <see cref="TargetBuildingCoord"/> is non-null.
/// </summary>
/// <param name="AttackerId">The attacking unit.</param>
/// <param name="TargetUnitId">The target unit, if the target is a unit.</param>
/// <param name="TargetBuildingCoord">The target hex, if the target is a building.</param>
public sealed record PendingAttackInfo(
    UnitId AttackerId,
    UnitId? TargetUnitId,
    HexCoord? TargetBuildingCoord)
{
    /// <summary>Creates a pending unit-vs-unit attack.</summary>
    public static PendingAttackInfo Unit(UnitId attacker, UnitId target) =>
        new(attacker, target, TargetBuildingCoord: null);

    /// <summary>Creates a pending unit-vs-building attack.</summary>
    public static PendingAttackInfo Building(UnitId attacker, HexCoord buildingCoord) =>
        new(attacker, TargetUnitId: null, buildingCoord);

    /// <summary>True if this pending attack targets a building.</summary>
    public bool IsBuildingAttack => TargetBuildingCoord is not null;
}
