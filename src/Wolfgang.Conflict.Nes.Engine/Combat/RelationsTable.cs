using Wolfgang.Conflict.Nes.Engine.Units;

namespace Wolfgang.Conflict.Nes.Engine.Combat;

/// <summary>
/// Combat matchup table keyed by <see cref="UnitCategory"/>. Encodes the
/// manual's rock-paper-scissors core (Attacker ▸ BattleTank ▸ FlakPanzer ▸
/// Fighter ▸ Attacker) plus sensible defaults for the remaining roles.
/// </summary>
/// <remarks>
/// This is the category-default tier table. A future per-model
/// <c>relations.json</c> (transcribed from the manual's RELATIONS chart)
/// will override specific unit pairs without code changes.
/// </remarks>
public static class RelationsTable
{
    /// <summary>True if a category is an aircraft.</summary>
    public static bool IsAir(UnitCategory category) => category
        is UnitCategory.Attacker
        or UnitCategory.Helicopter
        or UnitCategory.Fighter
        or UnitCategory.SupplyPlane;

    /// <summary>
    /// True if <paramref name="attacker"/> can engage <paramref name="defender"/>
    /// at all. Supply units never attack; only Fighters, Helicopters and
    /// Flak Panzers can engage aircraft.
    /// </summary>
    public static bool CanEngage(UnitCategory attacker, UnitCategory defender)
    {
        if (attacker is UnitCategory.SupplyVehicle or UnitCategory.SupplyPlane)
        {
            return false;
        }
        if (IsAir(defender))
        {
            return attacker is UnitCategory.FlakPanzer or UnitCategory.Fighter or UnitCategory.Helicopter;
        }
        return true;
    }

    /// <summary>
    /// Returns the pre-combat matchup outlook from the attacker's perspective.
    /// </summary>
    public static MatchupTier Outlook(UnitCategory attacker, UnitCategory defender)
    {
        if (!CanEngage(attacker, defender))
        {
            return MatchupTier.CompleteDefeat;
        }
        return IsAir(defender)
            ? AirDefenderOutlook(attacker, defender)
            : GroundDefenderOutlook(attacker, defender);
    }

    /// <summary>
    /// Base attack value (0&#x2013;15 scale) derived from the matchup tier.
    /// Zero when the attacker cannot engage the defender.
    /// </summary>
    public static int BaseAttack(UnitCategory attacker, UnitCategory defender)
    {
        if (!CanEngage(attacker, defender))
        {
            return 0;
        }
        return Outlook(attacker, defender) switch
        {
            MatchupTier.TotalVictory => 11,
            MatchupTier.AtAdvantage => 8,
            MatchupTier.Equal => 6,
            MatchupTier.AtDisadvantage => 3,
            MatchupTier.CompleteDefeat => 2,
            _ => 5,
        };
    }

    private static MatchupTier AirDefenderOutlook(UnitCategory attacker, UnitCategory defender) => attacker switch
    {
        UnitCategory.FlakPanzer => MatchupTier.AtAdvantage,
        UnitCategory.Fighter => defender switch
        {
            UnitCategory.Helicopter => MatchupTier.TotalVictory,
            UnitCategory.SupplyPlane => MatchupTier.TotalVictory,
            UnitCategory.Attacker => MatchupTier.AtAdvantage,
            _ => MatchupTier.Equal,
        },
        UnitCategory.Helicopter => defender == UnitCategory.Helicopter
            ? MatchupTier.Equal
            : MatchupTier.AtDisadvantage,
        _ => MatchupTier.CompleteDefeat,
    };

    private static MatchupTier GroundDefenderOutlook(UnitCategory attacker, UnitCategory defender) => attacker switch
    {
        UnitCategory.Attacker => defender == UnitCategory.BattleTank
            ? MatchupTier.AtAdvantage
            : MatchupTier.AtAdvantage,
        UnitCategory.Helicopter => MatchupTier.AtAdvantage,
        UnitCategory.Fighter => MatchupTier.CompleteDefeat,
        UnitCategory.BattleTank => BattleTankOutlook(defender),
        UnitCategory.BattleMissileLauncher => defender switch
        {
            UnitCategory.BattleTank => MatchupTier.AtAdvantage,
            UnitCategory.Infantry => MatchupTier.AtDisadvantage,
            _ => MatchupTier.Equal,
        },
        UnitCategory.Commando => defender switch
        {
            UnitCategory.Infantry => MatchupTier.AtAdvantage,
            UnitCategory.BattleTank => MatchupTier.AtDisadvantage,
            _ => MatchupTier.Equal,
        },
        UnitCategory.Infantry => defender switch
        {
            UnitCategory.Infantry => MatchupTier.Equal,
            UnitCategory.BattleTank => MatchupTier.CompleteDefeat,
            _ => MatchupTier.AtDisadvantage,
        },
        UnitCategory.Jeep => defender == UnitCategory.Infantry
            ? MatchupTier.Equal
            : MatchupTier.AtDisadvantage,
        UnitCategory.FlakPanzer => MatchupTier.AtDisadvantage,
        _ => MatchupTier.Equal,
    };

    private static MatchupTier BattleTankOutlook(UnitCategory defender) => defender switch
    {
        UnitCategory.Infantry => MatchupTier.TotalVictory,
        UnitCategory.Jeep => MatchupTier.TotalVictory,
        UnitCategory.SupplyVehicle => MatchupTier.TotalVictory,
        UnitCategory.Commando => MatchupTier.AtAdvantage,
        UnitCategory.FlakPanzer => MatchupTier.AtAdvantage,
        UnitCategory.BattleMissileLauncher => MatchupTier.AtDisadvantage,
        _ => MatchupTier.Equal,
    };
}
