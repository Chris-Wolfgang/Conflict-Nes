using Wolfgang.Conflict.Nes.Engine.Game;
using Wolfgang.Conflict.Nes.Engine.Map;
using Wolfgang.Conflict.Nes.Engine.Players;
using Wolfgang.Conflict.Nes.Engine.Units;

namespace Wolfgang.Conflict.Nes.Engine.Rules;

/// <summary>
/// Pure rules for capturing buildings. Per the manual, only infantry can
/// capture; capture flips ownership of a <see cref="BuildingKind.City"/>,
/// <see cref="BuildingKind.Airbase"/> or <see cref="BuildingKind.Port"/>
/// when an enemy or neutral building is occupied at end of turn. HQ and
/// Factory are bound to the commander unit and cannot be captured by
/// occupation; they vanish when the owning side's commander dies.
/// </summary>
public static class CaptureRules
{
    /// <summary>
    /// Returns <see langword="true"/> if a building of the given kind can be
    /// captured by an infantry unit occupying it.
    /// </summary>
    public static bool IsCapturable(BuildingKind building) => building switch
    {
        BuildingKind.City or BuildingKind.Airbase or BuildingKind.Port => true,
        _ => false,
    };

    /// <summary>
    /// Computes the building-ownership flips that should happen at the end of
    /// <paramref name="side"/>'s turn. Each entry maps a building hex to its
    /// new owner. The returned set does not mutate state; callers fold it in.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="state"/> is null.</exception>
    public static IReadOnlyDictionary<global::Wolfgang.Conflict.Nes.Engine.Hex.HexCoord, Side> ComputeFlips(GameState state, Side side)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        var flips = new Dictionary<global::Wolfgang.Conflict.Nes.Engine.Hex.HexCoord, Side>();
        foreach (var unit in state.Units.Values)
        {
            if (unit.Side != side)
            {
                continue;
            }
            if (!unit.Type.CanCapture)
            {
                continue;
            }
            if (!state.Map.Tiles.TryGetValue(unit.Coord, out var tile))
            {
                continue;
            }
            if (tile.Building is not { } building || !IsCapturable(building))
            {
                continue;
            }
            if (state.GetBuildingOwner(unit.Coord) == side)
            {
                continue;
            }

            flips[unit.Coord] = side;
        }

        return flips;
    }
}
