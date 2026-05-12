using Wolfgang.Conflict.Nes.Engine.Map;

namespace Wolfgang.Conflict.Nes.Engine.Game;

/// <summary>
/// A complete mission specification: the <see cref="MapDefinition"/> plus the
/// initial unit roster for each side. Future iterations will load this from
/// JSON; for MVP <c>Mission01</c> is built in code by <c>MissionLoader</c>.
/// </summary>
/// <param name="Map">The hex map for the mission.</param>
/// <param name="StartingUnits">The starting placements for both sides.</param>
public sealed record MissionDefinition(
    MapDefinition Map,
    IReadOnlyList<UnitPlacement> StartingUnits);
