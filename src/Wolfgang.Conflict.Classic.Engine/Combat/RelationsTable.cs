using Wolfgang.Conflict.Classic.Engine.Units;

namespace Wolfgang.Conflict.Classic.Engine.Combat;

/// <summary>
/// Combat matchup table keyed by <see cref="UnitCategory"/>, calibrated to
/// the manual's section-4 RELATIONS chart. The chart is per-model, but its
/// rows are category-determined (all fighters share a row, both tanks share
/// a row, &#x2026;), so a category matrix reproduces it faithfully.
/// </summary>
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
    /// at all. Every unit carries at least a machine gun, so every adjacent
    /// engagement is permitted; the matchup tier and the resulting
    /// <see cref="BaseAttack"/> value determine whether the attack does
    /// anything useful.
    /// </summary>
    public static bool CanEngage(UnitCategory attacker, UnitCategory defender)
    {
        _ = attacker;
        _ = defender;
        return true;
    }

    /// <summary>
    /// Returns the pre-combat matchup outlook from the attacker's perspective,
    /// calibrated to the manual's RELATIONS chart.
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

    // RED defender is an aircraft — only Flak Panzers and aircraft reach
    // this branch (see CanEngage).
    private static MatchupTier AirDefenderOutlook(UnitCategory attacker, UnitCategory defender) => attacker switch
    {
        // Anti-air vehicles dominate every aircraft (manual: ◎ across the row).
        UnitCategory.FlakPanzer => MatchupTier.TotalVictory,
        UnitCategory.Fighter => defender switch
        {
            UnitCategory.Helicopter => MatchupTier.TotalVictory,
            UnitCategory.SupplyPlane => MatchupTier.TotalVictory,
            UnitCategory.Attacker => MatchupTier.AtAdvantage,
            _ => MatchupTier.Equal,
        },
        // Ground-attack aircraft can defend themselves against other
        // aircraft but lose a dogfight to a true fighter.
        UnitCategory.Attacker => defender switch
        {
            UnitCategory.Fighter => MatchupTier.AtDisadvantage,
            UnitCategory.SupplyPlane => MatchupTier.AtAdvantage,
            _ => MatchupTier.Equal,
        },
        UnitCategory.Helicopter => defender switch
        {
            UnitCategory.Helicopter => MatchupTier.Equal,
            UnitCategory.SupplyPlane => MatchupTier.AtAdvantage,
            _ => MatchupTier.AtDisadvantage,
        },
        _ => MatchupTier.CompleteDefeat,
    };

    // RED defender is on the ground.
    private static MatchupTier GroundDefenderOutlook(UnitCategory attacker, UnitCategory defender) => attacker switch
    {
        UnitCategory.Infantry => InfantryOutlook(defender),
        UnitCategory.Commando => CommandoOutlook(defender),
        UnitCategory.Jeep => defender is UnitCategory.Infantry or UnitCategory.SupplyVehicle
            ? MatchupTier.Equal
            : MatchupTier.AtDisadvantage,
        UnitCategory.BattleMissileLauncher => MissileLauncherOutlook(defender),
        UnitCategory.BattleTank => BattleTankOutlook(defender),
        UnitCategory.FlakPanzer => FlakPanzerVsGroundOutlook(defender),
        UnitCategory.Attacker => AttackerOutlook(defender),
        UnitCategory.Helicopter => HelicopterVsGroundOutlook(defender),
        // A Fighter strafing ground (M61 Vulcan) — always a poor matchup.
        UnitCategory.Fighter => MatchupTier.CompleteDefeat,
        // Supply vehicles and supply planes carry only a defensive machine
        // gun; if they engage they take heavy losses.
        UnitCategory.SupplyVehicle or UnitCategory.SupplyPlane => MatchupTier.CompleteDefeat,
        _ => MatchupTier.Equal,
    };

    private static MatchupTier InfantryOutlook(UnitCategory defender) => defender switch
    {
        UnitCategory.Infantry => MatchupTier.Equal,
        UnitCategory.Commando => MatchupTier.AtDisadvantage,
        UnitCategory.SupplyVehicle => MatchupTier.Equal,
        _ => MatchupTier.CompleteDefeat,
    };

    private static MatchupTier CommandoOutlook(UnitCategory defender) => defender switch
    {
        UnitCategory.Infantry => MatchupTier.AtAdvantage,
        UnitCategory.Commando => MatchupTier.Equal,
        UnitCategory.SupplyVehicle => MatchupTier.AtAdvantage,
        UnitCategory.BattleMissileLauncher => MatchupTier.AtDisadvantage,
        // RPG-armed — can hurt armour, but loses the exchange.
        UnitCategory.BattleTank => MatchupTier.AtDisadvantage,
        _ => MatchupTier.CompleteDefeat,
    };

    private static MatchupTier MissileLauncherOutlook(UnitCategory defender) => defender switch
    {
        UnitCategory.Infantry => MatchupTier.TotalVictory,
        UnitCategory.Commando => MatchupTier.AtAdvantage,
        UnitCategory.SupplyVehicle => MatchupTier.TotalVictory,
        UnitCategory.BattleMissileLauncher => MatchupTier.Equal,
        UnitCategory.BattleTank => MatchupTier.AtAdvantage,
        UnitCategory.FlakPanzer => MatchupTier.AtAdvantage,
        _ => MatchupTier.Equal,
    };

    private static MatchupTier BattleTankOutlook(UnitCategory defender) => defender switch
    {
        UnitCategory.Infantry => MatchupTier.TotalVictory,
        UnitCategory.Commando => MatchupTier.TotalVictory,
        UnitCategory.Jeep => MatchupTier.TotalVictory,
        UnitCategory.SupplyVehicle => MatchupTier.TotalVictory,
        UnitCategory.BattleMissileLauncher => MatchupTier.AtAdvantage,
        UnitCategory.FlakPanzer => MatchupTier.TotalVictory,
        UnitCategory.BattleTank => MatchupTier.Equal,
        _ => MatchupTier.Equal,
    };

    private static MatchupTier FlakPanzerVsGroundOutlook(UnitCategory defender) => defender switch
    {
        // Anti-air vehicles are poor against ground armour.
        UnitCategory.BattleTank => MatchupTier.CompleteDefeat,
        UnitCategory.BattleMissileLauncher => MatchupTier.AtDisadvantage,
        UnitCategory.Infantry => MatchupTier.AtDisadvantage,
        UnitCategory.Commando => MatchupTier.AtDisadvantage,
        UnitCategory.SupplyVehicle => MatchupTier.AtAdvantage,
        _ => MatchupTier.AtDisadvantage,
    };

    private static MatchupTier AttackerOutlook(UnitCategory defender) => defender switch
    {
        // Fixed-wing ground-attack — a tank-buster.
        UnitCategory.BattleTank => MatchupTier.TotalVictory,
        UnitCategory.FlakPanzer => MatchupTier.AtAdvantage,
        UnitCategory.BattleMissileLauncher => MatchupTier.TotalVictory,
        _ => MatchupTier.TotalVictory,
    };

    private static MatchupTier HelicopterVsGroundOutlook(UnitCategory defender) => defender switch
    {
        UnitCategory.BattleTank => MatchupTier.AtAdvantage,
        UnitCategory.FlakPanzer => MatchupTier.AtDisadvantage,
        UnitCategory.Infantry => MatchupTier.TotalVictory,
        UnitCategory.Commando => MatchupTier.AtAdvantage,
        _ => MatchupTier.AtAdvantage,
    };
}
