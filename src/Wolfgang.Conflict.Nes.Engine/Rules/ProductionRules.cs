using Wolfgang.Conflict.Nes.Engine.Game;
using Wolfgang.Conflict.Nes.Engine.Hex;
using Wolfgang.Conflict.Nes.Engine.Map;
using Wolfgang.Conflict.Nes.Engine.Players;
using Wolfgang.Conflict.Nes.Engine.Units;

namespace Wolfgang.Conflict.Nes.Engine.Rules;

/// <summary>
/// Pure rules for the manual's Production Screen. Factories build ground
/// units; airbases build air units; ports (future) build sea units. A side
/// may not produce after all of its existing units have moved this turn.
/// </summary>
public static class ProductionRules
{
    /// <summary>
    /// Returns <see langword="true"/> if a building of the given kind can
    /// produce units at all.
    /// </summary>
    public static bool IsProductionBuilding(BuildingKind building)
        => building is BuildingKind.Factory or BuildingKind.Airbase or BuildingKind.Port;

    /// <summary>
    /// Returns <see langword="true"/> if a unit category may be produced at a
    /// building of the given kind.
    /// </summary>
    public static bool CanBuildCategoryAt(BuildingKind building, UnitCategory category) => building switch
    {
        BuildingKind.Factory => category is UnitCategory.Infantry
            or UnitCategory.Commando
            or UnitCategory.Jeep
            or UnitCategory.BattleTank
            or UnitCategory.BattleMissileLauncher
            or UnitCategory.FlakPanzer
            or UnitCategory.SupplyVehicle,
        // Airbases ship in infantry (airlifted / paratrooper) in addition to aircraft.
        BuildingKind.Airbase => category is UnitCategory.Infantry
            or UnitCategory.Attacker
            or UnitCategory.Helicopter
            or UnitCategory.Fighter
            or UnitCategory.SupplyPlane,
        _ => false,
    };

    /// <summary>
    /// Curated production roster per (factory, side). Each list has exactly
    /// six unit-type ids. Land factories lead with infantry; air factories
    /// carry six aircraft. Red and Blue rosters are complementary — every
    /// NATO unit has a matched Soviet counterpart on the opposite side.
    /// </summary>
    /// <remarks>
    /// Rule of thumb: if a unit appears on the mission's starting map, it
    /// must be in its side's factory roster, and its counterpart must be
    /// in the other side's matching roster (e.g. Blue's M48 SAM is paired
    /// with Red's SA-8; Blue's M247 AA-gun is paired with Red's ZSU-23).
    /// </remarks>
    private static readonly Dictionary<(BuildingKind Building, Side Side), string[]> Rosters = new()
    {
        // Land factories: infantry first, then commando, jeep, MBT, SAM, AA-gun.
        [(BuildingKind.Factory, Side.Blue)] =
            ["us-infantry", "us-commando", "m151", "m60a3", "m48", "m247"],

        [(BuildingKind.Factory, Side.Red)] =
            ["red-infantry", "red-commando", "brdm2", "t62", "sa8", "zsu23"],

        // Air factories: infantry first (paratroopers / airlifted), then five aircraft.
        [(BuildingKind.Airbase, Side.Blue)] =
            ["us-infantry", "ah1s", "ah64", "a10", "f4e", "f16c"],

        [(BuildingKind.Airbase, Side.Red)] =
            ["red-infantry", "mi24", "mi28", "su25", "mig23", "mig29"],
    };

    /// <summary>
    /// Returns the catalog unit types <paramref name="side"/> may produce at a
    /// building of kind <paramref name="building"/>. Order matches the
    /// production menu's intended display order.
    /// </summary>
    /// <param name="catalog">The unit catalog.</param>
    /// <param name="building">The production building kind.</param>
    /// <param name="side">The side requesting production.</param>
    /// <returns>The buildable unit type definitions.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="catalog"/> is null.</exception>
    public static IReadOnlyList<UnitTypeDefinition> ProducibleAt(UnitCatalog catalog, BuildingKind building, Side side)
    {
        if (catalog is null)
        {
            throw new ArgumentNullException(nameof(catalog));
        }

        if (!Rosters.TryGetValue((building, side), out var ids))
        {
            return Array.Empty<UnitTypeDefinition>();
        }

        var result = new List<UnitTypeDefinition>(ids.Length);
        foreach (var id in ids)
        {
            if (catalog.Contains(id))
            {
                result.Add(catalog.Get(id));
            }
        }
        return result;
    }

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="typeId"/> is in
    /// <paramref name="side"/>'s curated roster for the given factory kind.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="typeId"/> is null.</exception>
    public static bool IsInRoster(BuildingKind building, Side side, string typeId)
    {
        if (typeId is null)
        {
            throw new ArgumentNullException(nameof(typeId));
        }
        if (!Rosters.TryGetValue((building, side), out var ids))
        {
            return false;
        }
        foreach (var id in ids)
        {
            if (string.Equals(id, typeId, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="side"/> may produce
    /// any unit this turn at all (per the manual rule "you cannot produce a
    /// unit after all your units have moved").
    /// </summary>
    /// <param name="state">The current game state.</param>
    /// <param name="side">The side asking to produce.</param>
    /// <returns><see langword="true"/> if at least one unit of the side has not yet moved.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="state"/> is null.</exception>
    public static bool CanSideProduce(GameState state, Side side)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        foreach (var unit in state.Units.Values)
        {
            if (unit.Side == side && !unit.HasMoved)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Validates that a build at <paramref name="buildingCoord"/> producing
    /// the catalog type <paramref name="typeId"/> is currently legal.
    /// </summary>
    /// <param name="state">The current game state.</param>
    /// <param name="side">The side requesting the build.</param>
    /// <param name="buildingCoord">The factory or airbase hex.</param>
    /// <param name="typeId">The catalog id of the unit type to produce.</param>
    /// <returns>The resolved unit type definition.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="state"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The build is illegal.</exception>
    public static UnitTypeDefinition ValidateBuild(GameState state, Side side, HexCoord buildingCoord, string typeId)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        if (!state.Map.Tiles.TryGetValue(buildingCoord, out var tile))
        {
            throw new InvalidOperationException($"No tile at {buildingCoord}.");
        }

        if (tile.Building is not { } building)
        {
            throw new InvalidOperationException($"Tile {buildingCoord} has no building.");
        }

        if (!state.HasIntactBuilding(buildingCoord))
        {
            throw new InvalidOperationException($"Building at {buildingCoord} has been destroyed.");
        }

        if (state.GetBuildingOwner(buildingCoord) != side)
        {
            throw new InvalidOperationException($"{side} does not own the building at {buildingCoord}.");
        }

        var type = ResolveAndValidateType(state, side, building, typeId);

        if (state.Funds[side] < type.ProductionCost)
        {
            throw new InvalidOperationException($"{side} cannot afford {type.Name} (cost {type.ProductionCost}, funds {state.Funds[side]}).");
        }

        if (!CanSideProduce(state, side))
        {
            throw new InvalidOperationException($"{side} has already moved every unit this turn.");
        }

        foreach (var existing in state.Units.Values)
        {
            if (existing.Coord == buildingCoord)
            {
                throw new InvalidOperationException($"Building hex {buildingCoord} is occupied by unit {existing.Id}.");
            }
        }

        return type;
    }

    private static UnitTypeDefinition ResolveAndValidateType(GameState state, Side side, BuildingKind building, string typeId)
    {
        if (!state.Catalog.Contains(typeId))
        {
            throw new InvalidOperationException($"Unknown unit type '{typeId}'.");
        }

        var type = state.Catalog.Get(typeId);

        if (type.Side is { } affinity && affinity != side)
        {
            throw new InvalidOperationException($"{side} cannot build {type.Name} ({affinity}-only).");
        }

        if (!CanBuildCategoryAt(building, type.Category))
        {
            throw new InvalidOperationException($"{building} cannot produce {type.Name}.");
        }

        if (!IsInRoster(building, side, typeId))
        {
            throw new InvalidOperationException($"{type.Name} is not in {side}'s {building} production roster.");
        }

        return type;
    }
}
