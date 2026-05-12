namespace Wolfgang.Conflict.Nes.UI.Blazor.Rendering;

/// <summary>
/// NES-inspired colour palette. Original — designed to evoke the 1991 Conflict
/// look without reproducing the game's actual palette or sprites.
/// </summary>
public static class NesPalette
{
    /// <summary>Game-board background (outside the hex grid).</summary>
    public const string Background = "#0a0a18";

    // Terrain
    public const string Plains = "#7ca84c";
    public const string PlainsDark = "#5c8030";
    public const string Forest = "#2a5a24";
    public const string ForestDark = "#163818";
    public const string Mountain = "#8c7044";
    public const string MountainDark = "#5c4830";
    public const string Road = "#c8a868";
    public const string Bridge = "#c08858";
    public const string River = "#3870b8";
    public const string Beach = "#e8d0a0";
    public const string Sea = "#1c4c98";
    public const string Reef = "#143058";

    // Sides
    public const string BlueSide = "#3868c0";
    public const string BlueSideDark = "#203070";
    public const string RedSide = "#c83030";
    public const string RedSideDark = "#702018";
    public const string NeutralBuilding = "#a89060";

    // Grid + UI
    public const string Grid = "#1c2030";
    public const string Selection = "#fce18c";
    public const string Damage = "#e02c2c";
    public const string Text = "#e8e8e8";
}
