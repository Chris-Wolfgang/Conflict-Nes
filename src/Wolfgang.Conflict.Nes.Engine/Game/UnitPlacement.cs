using Wolfgang.Conflict.Nes.Engine.Hex;
using Wolfgang.Conflict.Nes.Engine.Players;
using Wolfgang.Conflict.Nes.Engine.Units;

namespace Wolfgang.Conflict.Nes.Engine.Game;

/// <summary>
/// A single unit's starting position in a mission. Used by
/// <see cref="MissionDefinition"/> to seed the initial <c>GameState</c>.
/// </summary>
/// <param name="Side">Owning side.</param>
/// <param name="Kind">Unit kind.</param>
/// <param name="Coord">Starting hex.</param>
/// <param name="IsCommander">
/// True for the side's single commander unit. Killing the commander ends the
/// game for its owner (manual: "Victory is achieved with the defeat of the
/// enemy commander's unit").
/// </param>
public sealed record UnitPlacement(
    Side Side,
    UnitKind Kind,
    HexCoord Coord,
    bool IsCommander);
