using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wolfgang.Conflict.Classic.Engine.Hex;
using Wolfgang.Conflict.Classic.Engine.Players;

namespace Wolfgang.Conflict.Classic.Engine.Map;

/// <summary>
/// Loads <see cref="MapDefinition"/> instances from the verbose JSON map format.
/// </summary>
/// <remarks>
/// JSON shape:
/// <code>
/// {
///   "name": "Mission 01: First Strike",
///   "width": 12,
///   "height": 10,
///   "tiles": [
///     { "q": 0, "r": 0, "terrain": "Plains", "building": null, "owner": null },
///     ...
///   ]
/// }
/// </code>
/// </remarks>
public static class MapLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Loads a map from a JSON string.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is null.</exception>
    /// <exception cref="ArgumentException">The JSON is malformed or violates map rules.</exception>
    public static MapDefinition LoadFromJson(string json)
    {
        if (json is null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        MapDocument? doc;
        try
        {
            doc = JsonSerializer.Deserialize<MapDocument>(json, Options);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Map JSON is malformed.", nameof(json), ex);
        }

        if (doc is null)
        {
            throw new ArgumentException("Map JSON is empty.", nameof(json));
        }

        return Build(doc);
    }

    /// <summary>Loads a map from a stream of UTF-8 JSON.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
    /// <exception cref="ArgumentException">The JSON is malformed or violates map rules.</exception>
    public static async Task<MapDefinition> LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        MapDocument? doc;
        try
        {
            doc = await JsonSerializer.DeserializeAsync<MapDocument>(stream, Options, cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Map JSON is malformed.", nameof(stream), ex);
        }

        if (doc is null)
        {
            throw new ArgumentException("Map JSON is empty.", nameof(stream));
        }

        return Build(doc);
    }

    /// <summary>
    /// Loads a map from an embedded JSON resource in the engine assembly.
    /// </summary>
    /// <param name="resourceName">
    /// Fully-qualified resource name, e.g. <c>"Wolfgang.Conflict.Classic.Engine.Maps.mission01.json"</c>.
    /// </param>
    /// <param name="cancellationToken">Token used to cancel the read.</param>
    /// <exception cref="ArgumentNullException"><paramref name="resourceName"/> is null.</exception>
    /// <exception cref="ArgumentException">The resource does not exist or its JSON violates map rules.</exception>
    public static async Task<MapDefinition> LoadEmbeddedAsync(string resourceName, CancellationToken cancellationToken = default)
    {
        if (resourceName is null)
        {
            throw new ArgumentNullException(nameof(resourceName));
        }

        var assembly = typeof(MapLoader).GetTypeInfo().Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new ArgumentException($"Embedded resource '{resourceName}' not found in {assembly.GetName().Name}.", nameof(resourceName));
        return await LoadFromStreamAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    private static MapDefinition Build(MapDocument doc)
    {
        if (doc.Tiles is null)
        {
            throw new ArgumentException("Map JSON is missing the 'tiles' array.", nameof(doc));
        }

        var tiles = new List<Tile>(doc.Tiles.Count);
        foreach (var t in doc.Tiles)
        {
            tiles.Add(new Tile(new HexCoord(t.Q, t.R), t.Terrain, t.Building, t.Owner));
        }

        return new MapDefinition(doc.Name ?? string.Empty, doc.Width, doc.Height, tiles);
    }

    private sealed class MapDocument
    {
        public string? Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public List<TileDocument>? Tiles { get; set; }
    }

    private sealed class TileDocument
    {
        public int Q { get; set; }
        public int R { get; set; }
        public Terrain Terrain { get; set; }
        public BuildingKind? Building { get; set; }
        public Side? Owner { get; set; }
    }
}
