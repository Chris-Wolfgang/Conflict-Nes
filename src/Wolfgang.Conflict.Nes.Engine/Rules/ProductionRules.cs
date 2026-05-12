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
    /// Returns the unit kinds <paramref name="building"/> can produce, or an
    /// empty list if it is not a production building.
    /// </summary>
    public static IReadOnlyList<UnitKind> ProducibleAt(BuildingKind building) => building switch
    {
        BuildingKind.Factory => [UnitKind.Infantry, UnitKind.Tank],
        BuildingKind.Airbase => [UnitKind.Helicopter, UnitKind.Fighter],
        _ => [],
    };

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
    /// <paramref name="kind"/> is currently legal. Returns the resolved
    /// <see cref="BuildingKind"/> on success.
    /// </summary>
    /// <param name="state">The current game state.</param>
    /// <param name="side">The side requesting the build.</param>
    /// <param name="buildingCoord">The factory or airbase hex.</param>
    /// <param name="kind">The unit kind to produce.</param>
    /// <returns>The kind of building at <paramref name="buildingCoord"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="state"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The build is illegal.</exception>
    public static BuildingKind ValidateBuild(GameState state, Side side, HexCoord buildingCoord, UnitKind kind)
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

        var allowed = ProducibleAt(building);
        if (allowed.Count == 0)
        {
            throw new InvalidOperationException($"{building} does not produce units.");
        }

        if (!allowed.Contains(kind))
        {
            throw new InvalidOperationException($"{building} cannot produce {kind}.");
        }

        var cost = UnitStats.For(kind).ProductionCost;
        if (state.Funds[side] < cost)
        {
            throw new InvalidOperationException($"{side} cannot afford {kind} (cost {cost}, funds {state.Funds[side]}).");
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

        return building;
    }
}
