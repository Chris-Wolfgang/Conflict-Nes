namespace Wolfgang.Conflict.Nes.Engine.Hex;

/// <summary>
/// A* pathfinder over a hex grid with caller-supplied step-cost and
/// passability functions. The pathfinder is allocation-light and is safe to
/// call concurrently — it carries no mutable state between calls.
/// </summary>
public static class HexPathfinder
{
    /// <summary>
    /// Finds the cheapest path from <paramref name="start"/> to
    /// <paramref name="goal"/> respecting <paramref name="stepCost"/>. Returns
    /// <see langword="null"/> if no path exists or if <paramref name="maxCost"/>
    /// would be exceeded.
    /// </summary>
    /// <param name="start">The hex to start from.</param>
    /// <param name="goal">The hex to reach.</param>
    /// <param name="stepCost">
    /// Returns the cost of moving into a given hex. Return <see langword="null"/>
    /// to mark a hex as impassable.
    /// </param>
    /// <param name="maxCost">
    /// Optional cap on total path cost. Paths exceeding this are not returned.
    /// </param>
    /// <returns>The cheapest path, or <see langword="null"/> if none was found.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="stepCost"/> is <see langword="null"/>.
    /// </exception>
    public static HexPath? FindPath(
        HexCoord start,
        HexCoord goal,
        Func<HexCoord, int?> stepCost,
        int maxCost = int.MaxValue)
    {
        if (stepCost is null)
        {
            throw new ArgumentNullException(nameof(stepCost));
        }

        if (start == goal)
        {
            return new HexPath([start], 0);
        }

        // SortedSet acts as the open priority queue. The tie-breaker counter
        // gives a stable ordering when two entries share f-score.
        var open = new SortedSet<(int F, int Counter, HexCoord Hex)>(FrontierComparer.Instance);
        var counter = 0;
        var gScore = new Dictionary<HexCoord, int> { [start] = 0 };
        var cameFrom = new Dictionary<HexCoord, HexCoord>();

        open.Add((start.DistanceTo(goal), counter++, start));

        while (open.Count > 0)
        {
            var current = open.Min;
            open.Remove(current);

            if (current.Hex == goal)
            {
                return Reconstruct(cameFrom, current.Hex, gScore[current.Hex]);
            }

            foreach (var neighbor in current.Hex.Neighbors())
            {
                var moveCost = stepCost(neighbor);
                if (moveCost is null)
                {
                    continue;
                }

                var tentativeG = gScore[current.Hex] + moveCost.Value;
                if (tentativeG > maxCost)
                {
                    continue;
                }

                if (gScore.TryGetValue(neighbor, out var existing) && tentativeG >= existing)
                {
                    continue;
                }

                cameFrom[neighbor] = current.Hex;
                gScore[neighbor] = tentativeG;
                open.Add((tentativeG + neighbor.DistanceTo(goal), counter++, neighbor));
            }
        }

        return null;
    }

    private static HexPath Reconstruct(IDictionary<HexCoord, HexCoord> cameFrom, HexCoord end, int totalCost)
    {
        var hexes = new List<HexCoord> { end };
        var cursor = end;
        while (cameFrom.TryGetValue(cursor, out var previous))
        {
            hexes.Add(previous);
            cursor = previous;
        }
        hexes.Reverse();
        return new HexPath(hexes, totalCost);
    }

    private sealed class FrontierComparer : IComparer<(int F, int Counter, HexCoord Hex)>
    {
        public static FrontierComparer Instance { get; } = new();

        public int Compare((int F, int Counter, HexCoord Hex) x, (int F, int Counter, HexCoord Hex) y)
        {
            var f = x.F.CompareTo(y.F);
            return f != 0 ? f : x.Counter.CompareTo(y.Counter);
        }
    }
}
