namespace Wolfgang.Conflict.Nes.Engine.Units;

/// <summary>
/// Movement domain of a unit. Decides which terrain costs apply.
/// </summary>
public enum MovementDomain
{
    /// <summary>Walks on land; impassable to mountains, rivers, sea.</summary>
    Foot = 0,

    /// <summary>Tracked / wheeled vehicle; same constraints as <see cref="Foot"/>.</summary>
    Tread = 1,

    /// <summary>Rotary aircraft; ignores ground terrain, can enter air spaces.</summary>
    Helicopter = 2,

    /// <summary>Fixed-wing aircraft; same as <see cref="Helicopter"/>, plus fuel drain.</summary>
    Fighter = 3,
}
