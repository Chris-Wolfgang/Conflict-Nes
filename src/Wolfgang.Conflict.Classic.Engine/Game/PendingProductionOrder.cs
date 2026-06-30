using Wolfgang.Conflict.Classic.Engine.Hex;

namespace Wolfgang.Conflict.Classic.Engine.Game;

/// <summary>
/// A unit production order placed at <paramref name="FactoryCoord"/> for
/// the catalog type <paramref name="TypeId"/>, paid for but not yet
/// materialised. The unit appears on the factory hex at the start of the
/// ordering side's next turn (see <see cref="GameEngine.EndTurn"/>).
/// </summary>
public sealed record PendingProductionOrder(HexCoord FactoryCoord, string TypeId);
