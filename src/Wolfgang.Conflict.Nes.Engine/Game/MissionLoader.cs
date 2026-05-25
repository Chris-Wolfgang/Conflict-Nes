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
    /// Loads Mission 01: First Strike. Map data and the unit catalog come
    /// from embedded JSON resources; unit placement is hardcoded here for
    /// MVP and will move to data in a future milestone.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the underlying I/O.</param>
    /// <returns>The composed <see cref="MissionDefinition"/>.</returns>
    public static async Task<MissionDefinition> LoadMission01Async(CancellationToken cancellationToken = default)
    {
        var map = await MapLoader.LoadEmbeddedAsync(
            "Wolfgang.Conflict.Nes.Engine.Maps.mission01.json",
            cancellationToken).ConfigureAwait(false);

        var catalog = await UnitCatalog.LoadEmbeddedAsync(cancellationToken).ConfigureAwait(false);

        var placements = new List<UnitPlacement>
        {
            // Blue: commander M1A1 guards the Factory at (1,6); mixed-arms
            // force fans out around it. The commander's "H" badge is the HQ
            // — there is no separate HQ building.
            new(Side.Blue, "m1a1",        new HexCoord(1, 7), IsCommander: true),
            new(Side.Blue, "liberator",   new HexCoord(2, 6), IsCommander: false),
            new(Side.Blue, "us-commando", new HexCoord(3, 7), IsCommander: false),
            new(Side.Blue, "m60a3",       new HexCoord(2, 7), IsCommander: false),
            new(Side.Blue, "m151",        new HexCoord(0, 6), IsCommander: false),
            new(Side.Blue, "m48",         new HexCoord(2, 8), IsCommander: false),
            new(Side.Blue, "ah1s",        new HexCoord(1, 5), IsCommander: false),
            new(Side.Blue, "a10",         new HexCoord(2, 5), IsCommander: false),
            new(Side.Blue, "f4e",         new HexCoord(2, 4), IsCommander: false),

            // Red: commander T-80 guards the Factory at (9,-4); mirrored
            // mixed-arms force around it.
            new(Side.Red, "t80",          new HexCoord(11, -5), IsCommander: true),
            new(Side.Red, "red-infantry", new HexCoord(10, -4), IsCommander: false),
            new(Side.Red, "red-commando", new HexCoord(10, -3), IsCommander: false),
            new(Side.Red, "t62",          new HexCoord(10, -5), IsCommander: false),
            new(Side.Red, "brdm2",        new HexCoord(11, -4), IsCommander: false),
            new(Side.Red, "zsu23",        new HexCoord(11, -3), IsCommander: false),
            new(Side.Red, "mi24",         new HexCoord(7,  -2), IsCommander: false),
            new(Side.Red, "su25",         new HexCoord(8,  -2), IsCommander: false),
            new(Side.Red, "mig23",        new HexCoord(8,  -3), IsCommander: false),
        };

        return new MissionDefinition(map, catalog, placements);
    }
}
