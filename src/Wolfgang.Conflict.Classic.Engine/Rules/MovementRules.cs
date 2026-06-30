using Wolfgang.Conflict.Classic.Engine.Game;
using Wolfgang.Conflict.Classic.Engine.Hex;
using Wolfgang.Conflict.Classic.Engine.Units;

namespace Wolfgang.Conflict.Classic.Engine.Rules;

/// <summary>
/// Pure functions for unit movement. Wraps <see cref="HexPathfinder"/> with
/// the engine's terrain costs, occupancy, fuel and per-turn movement caps.
/// </summary>
public static class MovementRules
{
    /// <summary>
    /// Finds the cheapest path for <paramref name="unit"/> to reach
    /// <paramref name="destination"/>, or <see langword="null"/> if no valid
    /// path exists within the unit's remaining moves and fuel.
    /// </summary>
    /// <param name="state">The current game state.</param>
    /// <param name="unit">The unit attempting to move.</param>
    /// <param name="destination">The hex the unit wants to reach.</param>
    /// <returns>The cheapest legal path, or <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="state"/> or <paramref name="unit"/> is null.
    /// </exception>
    public static HexPath? FindPath(GameState state, Unit unit, HexCoord destination)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }
        if (unit is null)
        {
            throw new ArgumentNullException(nameof(unit));
        }

        if (!state.Map.Contains(destination))
        {
            return null;
        }

        var occupied = OccupiedByOthers(state, unit);
        var budget = Budget(unit);

        return HexPathfinder.FindPath(
            unit.Coord,
            destination,
            hex => StepCost(state, unit, occupied, hex),
            maxCost: budget);
    }

    /// <summary>
    /// Returns every hex the unit can legally reach this turn (excluding its
    /// current hex).
    /// </summary>
    /// <param name="state">The current game state.</param>
    /// <param name="unit">The unit being queried.</param>
    /// <returns>A list of reachable hex coordinates.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="state"/> or <paramref name="unit"/> is null.
    /// </exception>
    public static IReadOnlyList<HexCoord> GetReachable(GameState state, Unit unit)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }
        if (unit is null)
        {
            throw new ArgumentNullException(nameof(unit));
        }

        var budget = Budget(unit);
        if (budget <= 0)
        {
            return [];
        }

        var occupied = OccupiedByOthers(state, unit);
        var costs = Flood(state, unit, occupied, budget);

        var result = new List<HexCoord>(costs.Count);
        foreach (var hex in costs.Keys)
        {
            if (hex != unit.Coord)
            {
                result.Add(hex);
            }
        }
        return result;
    }

    /// <summary>
    /// Per-turn movement budget in hex-cost units. A unit gets one move
    /// action per turn: once it has moved (<see cref="Unit.HasMoved"/>) it
    /// cannot move again, and a unit with zero fuel cannot move at all.
    /// Otherwise the budget is its <see cref="Unit.MovesRemaining"/>.
    /// </summary>
    private static int Budget(Unit unit)
        => unit.Fuel <= 0 || unit.HasMoved ? 0 : unit.MovesRemaining;

    private static HashSet<HexCoord> OccupiedByOthers(GameState state, Unit self)
    {
        var set = new HashSet<HexCoord>();
        foreach (var other in state.Units.Values)
        {
            if (other.Id != self.Id)
            {
                set.Add(other.Coord);
            }
        }
        return set;
    }

    private static int? StepCost(GameState state, Unit unit, HashSet<HexCoord> occupied, HexCoord hex)
    {
        if (hex == unit.Coord)
        {
            return 0;
        }
        if (!state.Map.Tiles.TryGetValue(hex, out var tile))
        {
            return null;
        }
        if (occupied.Contains(hex))
        {
            return null;
        }
        // Factories are bound to the commander and cannot be entered by any
        // unit. (Cities, airbases and ports are still traversable — they're
        // the supply / capture buildings.)
        if (tile.Building == Map.BuildingKind.LandFactory)
        {
            return null;
        }
        return UnitStats.TerrainCost(unit.Type.MovementDomain, tile.Terrain, tile.Building);
    }

    private static Dictionary<HexCoord, int> Flood(GameState state, Unit unit, HashSet<HexCoord> occupied, int budget)
    {
        var costs = new Dictionary<HexCoord, int> { [unit.Coord] = 0 };
        var frontier = new SortedSet<(int Cost, int Tie, HexCoord Hex)>(FrontierComparer.Instance);
        var counter = 0;
        frontier.Add((0, counter++, unit.Coord));

        while (frontier.Count > 0)
        {
            var current = frontier.Min;
            frontier.Remove(current);

            if (costs[current.Hex] < current.Cost)
            {
                continue;
            }

            foreach (var neighbor in current.Hex.Neighbors())
            {
                var step = StepCost(state, unit, occupied, neighbor);
                if (step is null)
                {
                    continue;
                }

                var newCost = current.Cost + step.Value;
                if (newCost > budget)
                {
                    continue;
                }

                if (costs.TryGetValue(neighbor, out var existing) && newCost >= existing)
                {
                    continue;
                }

                costs[neighbor] = newCost;
                frontier.Add((newCost, counter++, neighbor));
            }
        }

        return costs;
    }

    private sealed class FrontierComparer : IComparer<(int Cost, int Tie, HexCoord Hex)>
    {
        public static FrontierComparer Instance { get; } = new();

        public int Compare((int Cost, int Tie, HexCoord Hex) x, (int Cost, int Tie, HexCoord Hex) y)
        {
            var c = x.Cost.CompareTo(y.Cost);
            return c != 0 ? c : x.Tie.CompareTo(y.Tie);
        }
    }
}
