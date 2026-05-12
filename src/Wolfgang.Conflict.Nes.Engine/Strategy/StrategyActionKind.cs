namespace Wolfgang.Conflict.Nes.Engine.Strategy;

/// <summary>The kind of <see cref="StrategyAction"/>.</summary>
public enum StrategyActionKind
{
    /// <summary>End this side's turn.</summary>
    EndTurn = 0,

    /// <summary>Move a unit.</summary>
    Move = 1,

    /// <summary>Attack with a unit.</summary>
    Attack = 2,

    /// <summary>Invoke supply on a unit.</summary>
    Supply = 3,

    /// <summary>Build a new unit.</summary>
    Build = 4,
}
