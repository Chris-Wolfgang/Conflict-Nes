using Wolfgang.Conflict.Nes.Engine.Map;

namespace Wolfgang.Conflict.Nes.UI.Blazor.Rendering;

/// <summary>Maps <see cref="Terrain"/> values to <see cref="NesPalette"/> fills.</summary>
public static class TerrainColors
{
    /// <summary>Returns the SVG fill colour for a terrain kind.</summary>
    public static string FillFor(Terrain terrain) => terrain switch
    {
        Terrain.Plains => NesPalette.Plains,
        Terrain.Forest => NesPalette.Forest,
        Terrain.Mountain => NesPalette.Mountain,
        Terrain.Road => NesPalette.Road,
        Terrain.River => NesPalette.River,
        Terrain.Bridge => NesPalette.Bridge,
        Terrain.Shoal => NesPalette.Shoal,
        Terrain.Sea => NesPalette.Sea,
        Terrain.Reef => NesPalette.Reef,
        _ => NesPalette.Background,
    };

    /// <summary>Returns the darker stroke colour for a terrain kind's hex outline.</summary>
    public static string StrokeFor(Terrain terrain) => terrain switch
    {
        Terrain.Plains => NesPalette.PlainsDark,
        Terrain.Forest => NesPalette.ForestDark,
        Terrain.Mountain => NesPalette.MountainDark,
        _ => NesPalette.Grid,
    };
}
