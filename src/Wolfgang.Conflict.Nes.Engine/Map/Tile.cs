using Wolfgang.Conflict.Nes.Engine.Hex;
using Wolfgang.Conflict.Nes.Engine.Players;

namespace Wolfgang.Conflict.Nes.Engine.Map;

/// <summary>
/// A single hex on a <see cref="MapDefinition"/>. Tiles are immutable.
/// </summary>
/// <param name="Coord">Axial coordinate of the tile.</param>
/// <param name="Terrain">The terrain category.</param>
/// <param name="Building">
/// The kind of building on this tile, or <see langword="null"/> if none.
/// </param>
/// <param name="Owner">
/// The side that owns the building on this tile, or <see langword="null"/> if
/// the tile has no building or the building is neutral.
/// </param>
public sealed record Tile(
    HexCoord Coord,
    Terrain Terrain,
    BuildingKind? Building,
    Side? Owner);
