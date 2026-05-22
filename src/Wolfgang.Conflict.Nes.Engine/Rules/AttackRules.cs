using Wolfgang.Conflict.Nes.Engine.Combat;
using Wolfgang.Conflict.Nes.Engine.Game;
using Wolfgang.Conflict.Nes.Engine.Units;

namespace Wolfgang.Conflict.Nes.Engine.Rules;

/// <summary>
/// Pure functions describing when an attack is legal in AUTO battle mode.
/// </summary>
public static class AttackRules
{
    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="attacker"/> may
    /// legally engage <paramref name="defender"/> this turn.
    /// </summary>
    /// <param name="attacker">The unit attempting the attack.</param>
    /// <param name="defender">The intended target.</param>
    /// <returns><see langword="true"/> if the attack is legal.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="attacker"/> or <paramref name="defender"/> is null.
    /// </exception>
    public static bool CanAttack(Unit attacker, Unit defender)
    {
        if (attacker is null)
        {
            throw new ArgumentNullException(nameof(attacker));
        }
        if (defender is null)
        {
            throw new ArgumentNullException(nameof(defender));
        }

        if (attacker.Side == defender.Side)
        {
            return false;
        }

        if (attacker.HasAttacked)
        {
            return false;
        }

        if (attacker.Coord.DistanceTo(defender.Coord) != 1)
        {
            return false;
        }

        if (RelationsTable.BaseAttack(attacker.Category, defender.Category) <= 0)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Enumerates every enemy unit <paramref name="attacker"/> can legally
    /// engage this turn.
    /// </summary>
    /// <param name="state">The current game state.</param>
    /// <param name="attacker">The candidate attacker.</param>
    /// <returns>Every enemy unit id the attacker may target.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="state"/> or <paramref name="attacker"/> is null.
    /// </exception>
    public static IReadOnlyList<UnitId> GetLegalTargets(GameState state, Unit attacker)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }
        if (attacker is null)
        {
            throw new ArgumentNullException(nameof(attacker));
        }

        var targets = new List<UnitId>();
        foreach (var other in state.Units.Values)
        {
            if (CanAttack(attacker, other))
            {
                targets.Add(other.Id);
            }
        }
        return targets;
    }
}
