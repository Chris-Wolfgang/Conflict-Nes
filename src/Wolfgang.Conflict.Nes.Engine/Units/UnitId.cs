namespace Wolfgang.Conflict.Nes.Engine.Units;

/// <summary>
/// Opaque identifier for a unit instance in a game.
/// </summary>
public readonly record struct UnitId(int Value)
{
    /// <inheritdoc/>
    public override string ToString() => $"U{Value}";
}
