using Wolfgang.Conflict.Nes.Engine.Hex;
using Wolfgang.Conflict.Nes.Engine.Players;

namespace Wolfgang.Conflict.Nes.Engine.Game;

/// <summary>
/// A single unit's starting position in a mission. Used by
/// <see cref="MissionDefinition"/> to seed the initial <c>GameState</c>.
/// </summary>
/// <param name="Side">Owning side.</param>
/// <param name="TypeId">Catalog id of the unit type to place.</param>
/// <param name="Coord">Starting hex.</param>
/// <param name="IsCommander">
/// True for the side's single commander unit. Killing the commander ends the
/// game for its owner (manual: "Victory is achieved with the defeat of the
/// enemy commander's unit").
/// </param>
public sealed record UnitPlacement(
    Side Side,
    string TypeId,
    HexCoord Coord,
    bool IsCommander);
