namespace Wolfgang.Conflict.Nes.Engine.Units;

/// <summary>
/// The kinds of units that can appear on the board. MVP roster:
/// Infantry, Tank, Helicopter, Fighter. Additional kinds will be added in
/// future milestones (Anti-Tank, Recon, Howitzer, etc.).
/// </summary>
public enum UnitKind
{
    /// <summary>Foot soldiers. Only unit that can capture buildings.</summary>
    Infantry = 0,

    /// <summary>Armoured ground unit; strong against ground, vulnerable to air.</summary>
    Tank = 1,

    /// <summary>Rotary-wing aircraft; can engage ground and air units.</summary>
    Helicopter = 2,

    /// <summary>Fixed-wing aircraft; fastest unit, air-to-air only.</summary>
    Fighter = 3,
}
