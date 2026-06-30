using Wolfgang.Conflict.Classic.Engine.Map;
using Wolfgang.Conflict.Classic.Engine.Units;

namespace Wolfgang.Conflict.Classic.Engine.Game;

/// <summary>
/// A complete mission specification: the <see cref="MapDefinition"/>, the
/// <see cref="UnitCatalog"/> in effect, and the initial unit roster for each
/// side.
/// </summary>
/// <param name="Map">The hex map for the mission.</param>
/// <param name="Catalog">The unit catalog used to resolve placements and production.</param>
/// <param name="StartingUnits">The starting placements for both sides.</param>
public sealed record MissionDefinition(
    MapDefinition Map,
    UnitCatalog Catalog,
    IReadOnlyList<UnitPlacement> StartingUnits);
