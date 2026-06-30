namespace Wolfgang.Conflict.Classic.Engine.Units;

/// <summary>
/// The functional role of a unit, per the manual's unit-type classification.
/// Combat matchups, movement domain defaults and AI heuristics key off the
/// category; specific models (M1A1, T-80, …) are data in the unit catalog.
/// </summary>
public enum UnitCategory
{
    /// <summary>Foot soldiers; can capture buildings.</summary>
    Infantry = 0,

    /// <summary>Elite foot soldiers with anti-armour rockets; can capture.</summary>
    Commando = 1,

    /// <summary>Light wheeled scout vehicle.</summary>
    Jeep = 2,

    /// <summary>Main battle tank; ground-to-ground.</summary>
    BattleTank = 3,

    /// <summary>Anti-tank missile carrier; ground-to-ground at range.</summary>
    BattleMissileLauncher = 4,

    /// <summary>Anti-aircraft vehicle ("flak panzer"); ground-to-air.</summary>
    FlakPanzer = 5,

    /// <summary>Fixed-wing ground-attack aircraft.</summary>
    Attacker = 6,

    /// <summary>Rotary-wing ground-attack aircraft.</summary>
    Helicopter = 7,

    /// <summary>Fixed-wing air-superiority aircraft; air-to-air.</summary>
    Fighter = 8,

    /// <summary>Ground supply vehicle; refuels/rearms adjacent friendlies.</summary>
    SupplyVehicle = 9,

    /// <summary>Aerial tanker; refuels adjacent friendly aircraft.</summary>
    SupplyPlane = 10,
}
