namespace Wolfgang.Conflict.Classic.UI.Blazor.Rendering;

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
    public const string ForestDark = "#143010";
    public const string ForestLight = "#3e7e30";
    public const string Mountain = "#b06838";
    public const string MountainDark = "#6e3a1c";
    public const string MountainLight = "#cc8a52";
    // Steel girder bridge — cool grey deck with darker rivets/shadow.
    public const string Bridge = "#9aa4b0";
    public const string BridgeDark = "#4c5663";
    public const string BridgeLight = "#cfd6df";
    public const string River = "#3870b8";
    public const string Shoal = "#e8d0a0";
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
