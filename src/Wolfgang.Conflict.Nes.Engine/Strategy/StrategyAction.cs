using Wolfgang.Conflict.Nes.Engine.Hex;
using Wolfgang.Conflict.Nes.Engine.Units;

namespace Wolfgang.Conflict.Nes.Engine.Strategy;

/// <summary>
/// One discrete command an <see cref="IPlayerStrategy"/> can request the
/// engine to apply. Use the static factory methods to construct each kind.
/// </summary>
public sealed class StrategyAction
{
    /// <summary>The kind of action.</summary>
    public StrategyActionKind Kind { get; }

    /// <summary>The unit involved (for Move/Attack/Supply).</summary>
    public UnitId UnitId { get; }

    /// <summary>The defender (for Attack only).</summary>
    public UnitId TargetId { get; }

    /// <summary>The destination hex (for Move) or build hex (for Build).</summary>
    public HexCoord Hex { get; }

    /// <summary>The unit kind to produce (for Build only).</summary>
    public UnitKind ProduceKind { get; }

    private StrategyAction(StrategyActionKind kind, UnitId unitId, UnitId targetId, HexCoord hex, UnitKind produceKind)
    {
        Kind = kind;
        UnitId = unitId;
        TargetId = targetId;
        Hex = hex;
        ProduceKind = produceKind;
    }

    /// <summary>The "no more commands; switch sides" sentinel.</summary>
    public static StrategyAction EndTurn { get; } = new(StrategyActionKind.EndTurn, default, default, default, default);

    /// <summary>Move <paramref name="unitId"/> to <paramref name="destination"/>.</summary>
    public static StrategyAction Move(UnitId unitId, HexCoord destination)
        => new(StrategyActionKind.Move, unitId, default, destination, default);

    /// <summary>Attack <paramref name="targetId"/> with <paramref name="attackerId"/>.</summary>
    public static StrategyAction Attack(UnitId attackerId, UnitId targetId)
        => new(StrategyActionKind.Attack, attackerId, targetId, default, default);

    /// <summary>Invoke the once-per-turn supply on <paramref name="unitId"/>.</summary>
    public static StrategyAction Supply(UnitId unitId)
        => new(StrategyActionKind.Supply, unitId, default, default, default);

    /// <summary>Build a new <paramref name="kind"/> at <paramref name="buildingHex"/>.</summary>
    public static StrategyAction Build(HexCoord buildingHex, UnitKind kind)
        => new(StrategyActionKind.Build, default, default, buildingHex, kind);
}

