namespace Wolfgang.Conflict.Classic.Engine.Combat;

/// <summary>
/// Abstraction over the combat RNG. Tests inject deterministic implementations.
/// </summary>
public interface IRandomSource
{
    /// <summary>
    /// Returns an integer in <c>[minInclusive, maxInclusive]</c>.
    /// </summary>
    int NextInt(int minInclusive, int maxInclusive);
}
