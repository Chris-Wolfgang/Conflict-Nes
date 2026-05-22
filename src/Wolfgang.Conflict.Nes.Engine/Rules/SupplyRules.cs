using Wolfgang.Conflict.Nes.Engine.Game;
using Wolfgang.Conflict.Nes.Engine.Map;
using Wolfgang.Conflict.Nes.Engine.Units;

namespace Wolfgang.Conflict.Nes.Engine.Rules;

/// <summary>
/// Pure functions for the manual's supply rules. A unit may supply once per
/// turn (start or end of turn) if it occupies a friendly matched building or
/// is adjacent to a friendly matched supply vehicle. Buildings refuel and
/// repair; supply vehicles only refuel.
/// </summary>
public static class SupplyRules
{
    /// <summary>LIFE restored per supply action when supplied by a building.</summary>
    public const int RepairAmount = 5;

    /// <summary>Where the supply came from; affects whether repairs are included.</summary>
    public enum SupplySource
    {
        /// <summary>City (ground units) or Airport (air units): refuel + repair.</summary>
        Building,

        /// <summary>Supply Truck (ground) / Supply Plane (air): refuel only.</summary>
        Vehicle,
    }

    /// <summary>
    /// Returns the kind of supply source available to <paramref name="unit"/>
    /// in the current <paramref name="state"/>, or <see langword="null"/> if
    /// no supply is currently available.
    /// </summary>
    /// <param name="state">The current game state.</param>
    /// <param name="unit">The unit asking for supply.</param>
    /// <returns>The available source, or <see langword="null"/> if none.</returns>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    public static SupplySource? AvailableSource(GameState state, Unit unit)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }
        if (unit is null)
        {
            throw new ArgumentNullException(nameof(unit));
        }

        if (unit.HasSupplied)
        {
            return null;
        }

        if (StandingOnSuppliableBuilding(state, unit))
        {
            return SupplySource.Building;
        }

        // Supply Truck / Supply Plane support not in MVP roster; the adjacency
        // check returns false today and will activate when those kinds land.
        return null;
    }

    /// <summary>
    /// Returns a new <see cref="Unit"/> with fuel (and, for building sources,
    /// HP) replenished. Does not check eligibility; the engine layer must
    /// gate the call with <see cref="AvailableSource"/>.
    /// </summary>
    /// <param name="unit">The unit receiving supply.</param>
    /// <param name="source">Where the supply came from.</param>
    /// <returns>A new unit with refilled stats and <c>HasSupplied = true</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="unit"/> is null.</exception>
    public static Unit ApplySupply(Unit unit, SupplySource source)
    {
        if (unit is null)
        {
            throw new ArgumentNullException(nameof(unit));
        }

        var newHp = source == SupplySource.Building
            ? Math.Min(UnitStats.MaxHitPoints, unit.HitPoints + RepairAmount)
            : unit.HitPoints;

        return unit with
        {
            Fuel = unit.Type.MaxFuel,
            Ammo = unit.Type.MaxAmmo,
            HitPoints = newHp,
            HasSupplied = true,
        };
    }

    private static bool StandingOnSuppliableBuilding(GameState state, Unit unit)
    {
        if (!state.Map.Tiles.TryGetValue(unit.Coord, out var tile))
        {
            return false;
        }
        if (tile.Building is not { } building)
        {
            return false;
        }

        // Per the original game, a unit refuels and repairs at any city or
        // airport regardless of ownership — if it is an enemy building the
        // unit also captures it by holding it through end of turn.
        return unit.Type.MovementDomain switch
        {
            MovementDomain.Foot or MovementDomain.Tread => building == BuildingKind.City,
            MovementDomain.Helicopter or MovementDomain.Fighter => building == BuildingKind.Airbase,
            _ => false,
        };
    }
}
