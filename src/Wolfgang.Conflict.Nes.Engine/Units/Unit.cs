using Wolfgang.Conflict.Nes.Engine.Hex;
using Wolfgang.Conflict.Nes.Engine.Players;

namespace Wolfgang.Conflict.Nes.Engine.Units;

/// <summary>
/// A unit instance on the board. Units are immutable; turn-by-turn state
/// transitions are modeled by creating a new <see cref="Unit"/> with the
/// updated values and returning it through a new <c>GameState</c>.
/// </summary>
/// <param name="Id">Stable identifier for this unit.</param>
/// <param name="Side">Owning side.</param>
/// <param name="Kind">Unit kind.</param>
/// <param name="Coord">Current hex.</param>
/// <param name="HitPoints">Current LIFE, in <c>[0, <see cref="UnitStats.MaxHitPoints"/>]</c>.</param>
/// <param name="Fuel">Current FUEL; drains by use.</param>
/// <param name="Ammo">Current SHELL (special-weapon ammo); standard weapon is unlimited.</param>
/// <param name="MovesRemaining">Remaining movement points this turn.</param>
/// <param name="HasMoved">True once the unit has moved this turn (gates production per manual).</param>
/// <param name="HasAttacked">True once the unit has attacked this turn.</param>
/// <param name="HasSupplied">True once the unit has used its one supply action this turn.</param>
/// <param name="IsCommander">True for the side's commander unit; losing it ends the game for its owner.</param>
public sealed record Unit(
    UnitId Id,
    Side Side,
    UnitKind Kind,
    HexCoord Coord,
    int HitPoints,
    int Fuel,
    int Ammo,
    int MovesRemaining,
    bool HasMoved,
    bool HasAttacked,
    bool HasSupplied,
    bool IsCommander)
{
    /// <summary>True if the unit has zero HP.</summary>
    public bool IsDead => HitPoints <= 0;

    /// <summary>Creates a brand-new full-strength unit of the given kind.</summary>
    /// <param name="id">Identifier for the new unit.</param>
    /// <param name="side">Owning side.</param>
    /// <param name="kind">Unit kind.</param>
    /// <param name="coord">Initial hex.</param>
    /// <param name="isCommander">Whether this is the side's commander.</param>
    public static Unit FullStrength(UnitId id, Side side, UnitKind kind, HexCoord coord, bool isCommander = false)
    {
        var stats = UnitStats.For(kind);
        return new Unit(
            id,
            side,
            kind,
            coord,
            HitPoints: UnitStats.MaxHitPoints,
            Fuel: stats.MaxFuel,
            Ammo: stats.MaxAmmo,
            MovesRemaining: stats.MovementPoints,
            HasMoved: false,
            HasAttacked: false,
            HasSupplied: false,
            IsCommander: isCommander);
    }
}
