using Wolfgang.Conflict.Classic.Engine.Units;

namespace Wolfgang.Conflict.Classic.Engine.Combat;

/// <summary>
/// Outcome of a single AUTO-mode combat exchange.
/// </summary>
/// <param name="AttackerId">The attacking unit.</param>
/// <param name="DefenderId">The defending unit.</param>
/// <param name="DamageToDefender">LIFE lost by the defender from the attacker's strike.</param>
/// <param name="DamageToAttacker">LIFE lost by the attacker from the defender's counter-attack.</param>
/// <param name="DefenderDestroyed">True if the defender's LIFE reached zero.</param>
/// <param name="AttackerDestroyed">True if the attacker's LIFE reached zero (from the counter).</param>
/// <param name="DefenderCountered">True if the defender was alive and able to return fire.</param>
/// <param name="AttackerOutlook">The pre-combat matchup tier from the attacker's perspective.</param>
/// <param name="DefenderOutlook">The pre-combat matchup tier from the defender's perspective.</param>
public sealed record CombatResult(
    UnitId AttackerId,
    UnitId DefenderId,
    int DamageToDefender,
    int DamageToAttacker,
    bool DefenderDestroyed,
    bool AttackerDestroyed,
    bool DefenderCountered,
    MatchupTier AttackerOutlook,
    MatchupTier DefenderOutlook);
