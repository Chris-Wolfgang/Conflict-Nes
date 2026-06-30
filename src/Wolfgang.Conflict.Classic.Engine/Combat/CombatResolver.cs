using Wolfgang.Conflict.Classic.Engine.Game;
using Wolfgang.Conflict.Classic.Engine.Map;
using Wolfgang.Conflict.Classic.Engine.Units;

namespace Wolfgang.Conflict.Classic.Engine.Combat;

/// <summary>
/// Resolves a single AUTO-mode attacker → defender exchange with optional
/// counter-attack. NORMAL mode (player picks weapon and defense maneuver)
/// will replace or wrap this in a future milestone.
/// </summary>
public static class CombatResolver
{
    /// <summary>Bounds for the random damage modifier added per swing.</summary>
    private const int RollMin = -2;
    private const int RollMax = 2;

    /// <summary>
    /// Resolves the exchange and returns the per-side damage. The state is
    /// not mutated; the engine layer applies damage and prunes dead units.
    /// </summary>
    /// <param name="state">The current game state.</param>
    /// <param name="attacker">The unit initiating the attack.</param>
    /// <param name="defender">The unit being attacked.</param>
    /// <param name="rng">RNG source for damage variance.</param>
    /// <returns>The <see cref="CombatResult"/> describing both sides' losses.</returns>
    /// <exception cref="ArgumentNullException">Any required argument is null.</exception>
    public static CombatResult Resolve(GameState state, Unit attacker, Unit defender, IRandomSource rng)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }
        if (attacker is null)
        {
            throw new ArgumentNullException(nameof(attacker));
        }
        if (defender is null)
        {
            throw new ArgumentNullException(nameof(defender));
        }
        if (rng is null)
        {
            throw new ArgumentNullException(nameof(rng));
        }

        var attackerTile = state.Map.Tiles[attacker.Coord];
        var defenderTile = state.Map.Tiles[defender.Coord];

        var dmgToDefender = ComputeDamage(attacker, defender, defenderTile, rng);
        var defenderHpAfter = Math.Max(0, defender.HitPoints - dmgToDefender);
        var defenderDestroyed = defenderHpAfter == 0;

        var defenderCanCounter = !defenderDestroyed
            && RelationsTable.BaseAttack(defender.Category, attacker.Category) > 0;

        var dmgToAttacker = 0;
        var attackerDestroyed = false;
        if (defenderCanCounter)
        {
            // Defender's effective HP for the counter is its post-damage HP.
            var counterDefender = defender with { HitPoints = defenderHpAfter };
            dmgToAttacker = ComputeDamage(counterDefender, attacker, attackerTile, rng);
            attackerDestroyed = attacker.HitPoints - dmgToAttacker <= 0;
        }

        return new CombatResult(
            AttackerId: attacker.Id,
            DefenderId: defender.Id,
            DamageToDefender: dmgToDefender,
            DamageToAttacker: dmgToAttacker,
            DefenderDestroyed: defenderDestroyed,
            AttackerDestroyed: attackerDestroyed,
            DefenderCountered: defenderCanCounter,
            AttackerOutlook: RelationsTable.Outlook(attacker.Category, defender.Category),
            DefenderOutlook: RelationsTable.Outlook(defender.Category, attacker.Category));
    }

    private static int ComputeDamage(Unit shooter, Unit target, Tile targetTile, IRandomSource rng)
    {
        var baseAttack = RelationsTable.BaseAttack(shooter.Category, target.Category);
        if (baseAttack <= 0)
        {
            return 0;
        }

        var defense = UnitStats.DefenseBonus(targetTile.Terrain, targetTile.Building);
        var roll = rng.NextInt(RollMin, RollMax);

        // Scale damage by the shooter's current HP (weakened units hit weaker).
        var scaled = (baseAttack * shooter.HitPoints) / UnitStats.MaxHitPoints;
        var raw = scaled - defense + roll;
        if (raw <= 0)
        {
            return 0;
        }

        // Damage cannot exceed the target's remaining HP.
        return Math.Min(raw, target.HitPoints);
    }
}
