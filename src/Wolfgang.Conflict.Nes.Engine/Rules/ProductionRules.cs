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
        BuildingKind.Airbase => category is UnitCategory.Attacker
            or UnitCategory.Helicopter
            or UnitCategory.Fighter
            or UnitCategory.SupplyPlane,
        _ => false,
    };

    /// <summary>
    /// Returns the catalog unit types <paramref name="side"/> may produce at a
    /// building of kind <paramref name="building"/>.
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

        var result = new List<UnitTypeDefinition>();
        foreach (var def in catalog.ForSide(side))
        {
            if (CanBuildCategoryAt(building, def.Category))
            {
                result.Add(def);
            }
        }
        return result;
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

        if (state.GetBuildingOwner(buildingCoord) != side)
        {
            throw new InvalidOperationException($"{side} does not own the building at {buildingCoord}.");
        }

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
}
