namespace Wolfgang.Conflict.Nes.Engine.Units;

/// <summary>
/// The expected outlook for an attacker engaging a given defender, drawn from
/// the manual's RELATIONS chart on a 5-point scale.
/// </summary>
public enum MatchupTier
{
    /// <summary>Attacker is expected to be completely defeated (manual "×").</summary>
    CompleteDefeat = 0,

    /// <summary>Attacker is at disadvantage (manual "△").</summary>
    AtDisadvantage = 1,

    /// <summary>Equal strength (manual "○").</summary>
    Equal = 2,

    /// <summary>Attacker is at advantage (manual "◐").</summary>
    AtAdvantage = 3,

    /// <summary>Attacker is expected to score a total victory (manual "●").</summary>
    TotalVictory = 4,
}
