namespace Wolfgang.Conflict.Nes.Engine.Units;

/// <summary>
/// Classifies the relative strength of an attacker versus a defender on the
/// manual's 5-tier RELATIONS scale. Used by the F.P. award rules to score
/// upset wins more than expected ones.
/// </summary>
public static class Matchups
{
    /// <summary>
    /// Returns the manual matchup outlook from <paramref name="attacker"/>'s
    /// perspective when engaging <paramref name="defender"/>.
    /// </summary>
    /// <remarks>
    /// MVP simplification of the manual's 18&#xD7;18 chart. The four MVP units
    /// preserve the rock-paper-scissors loop:
    /// Helicopter ▸ Tank ▸ Fighter ▸ Helicopter.
    /// Combatants outside their attack profile (Fighter vs Tank, Tank vs
    /// Fighter, Infantry vs aircraft) are tiered as <see cref="MatchupTier.CompleteDefeat"/>.
    /// </remarks>
    public static MatchupTier Outlook(UnitKind attacker, UnitKind defender)
    {
        return (attacker, defender) switch
        {
            // Infantry: only effective vs other infantry.
            (UnitKind.Infantry,   UnitKind.Infantry)   => MatchupTier.Equal,
            (UnitKind.Infantry,   UnitKind.Tank)       => MatchupTier.CompleteDefeat,
            (UnitKind.Infantry,   UnitKind.Helicopter) => MatchupTier.CompleteDefeat,
            (UnitKind.Infantry,   UnitKind.Fighter)    => MatchupTier.CompleteDefeat,

            // Tank: dominates ground, vulnerable to air.
            (UnitKind.Tank,       UnitKind.Infantry)   => MatchupTier.TotalVictory,
            (UnitKind.Tank,       UnitKind.Tank)       => MatchupTier.Equal,
            (UnitKind.Tank,       UnitKind.Helicopter) => MatchupTier.AtDisadvantage,
            (UnitKind.Tank,       UnitKind.Fighter)    => MatchupTier.CompleteDefeat,

            // Helicopter: anti-armor and anti-infantry, loses to fighters.
            (UnitKind.Helicopter, UnitKind.Infantry)   => MatchupTier.TotalVictory,
            (UnitKind.Helicopter, UnitKind.Tank)       => MatchupTier.AtAdvantage,
            (UnitKind.Helicopter, UnitKind.Helicopter) => MatchupTier.Equal,
            (UnitKind.Helicopter, UnitKind.Fighter)    => MatchupTier.AtDisadvantage,

            // Fighter: dominates other air, no ground attack.
            (UnitKind.Fighter,    UnitKind.Infantry)   => MatchupTier.CompleteDefeat,
            (UnitKind.Fighter,    UnitKind.Tank)       => MatchupTier.CompleteDefeat,
            (UnitKind.Fighter,    UnitKind.Helicopter) => MatchupTier.TotalVictory,
            (UnitKind.Fighter,    UnitKind.Fighter)    => MatchupTier.Equal,

            _ => MatchupTier.Equal,
        };
    }
}
