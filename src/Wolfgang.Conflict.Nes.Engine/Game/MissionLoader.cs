using Wolfgang.Conflict.Nes.Engine.Hex;
using Wolfgang.Conflict.Nes.Engine.Map;
using Wolfgang.Conflict.Nes.Engine.Players;
using Wolfgang.Conflict.Nes.Engine.Units;

namespace Wolfgang.Conflict.Nes.Engine.Game;

/// <summary>
/// Provides access to the built-in missions.
/// </summary>
public static class MissionLoader
{
    /// <summary>
    /// Loads Mission 01: First Strike. The map data lives in the embedded
    /// JSON resource <c>Wolfgang.Conflict.Nes.Engine.Maps.mission01.json</c>;
    /// the unit placement is hardcoded here for MVP and will move to data in
    /// a future milestone.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the underlying I/O.</param>
    /// <returns>The composed <see cref="MissionDefinition"/>.</returns>
    public static async Task<MissionDefinition> LoadMission01Async(CancellationToken cancellationToken = default)
    {
        var map = await MapLoader.LoadEmbeddedAsync(
            "Wolfgang.Conflict.Nes.Engine.Maps.mission01.json",
            cancellationToken).ConfigureAwait(false);

        var placements = new List<UnitPlacement>
        {
            // Blue: commander tank on HQ, infantry adjacent,
            // helicopter on airbase, fighter adjacent.
            new(Side.Blue, UnitKind.Tank,       new HexCoord(1, 7), IsCommander: true),
            new(Side.Blue, UnitKind.Infantry,   new HexCoord(2, 6), IsCommander: false),
            new(Side.Blue, UnitKind.Helicopter, new HexCoord(1, 5), IsCommander: false),
            new(Side.Blue, UnitKind.Fighter,    new HexCoord(2, 4), IsCommander: false),

            // Red: mirrored layout near Red HQ in the northeast.
            new(Side.Red, UnitKind.Tank,       new HexCoord(11, -5), IsCommander: true),
            new(Side.Red, UnitKind.Infantry,   new HexCoord(10, -4), IsCommander: false),
            new(Side.Red, UnitKind.Helicopter, new HexCoord(7,  -2), IsCommander: false),
            new(Side.Red, UnitKind.Fighter,    new HexCoord(8,  -3), IsCommander: false),
        };

        return new MissionDefinition(map, placements);
    }
}
