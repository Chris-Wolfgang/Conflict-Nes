using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wolfgang.Conflict.Classic.Engine.Players;

namespace Wolfgang.Conflict.Classic.Engine.Units;

/// <summary>
/// An immutable, data-driven catalog of every <see cref="UnitTypeDefinition"/>
/// available in the game, loaded from the <c>unit-types.json</c> embedded
/// resource. New units are added by editing the JSON — no code change.
/// </summary>
public sealed class UnitCatalog
{
    private const string EmbeddedResourceName = "Wolfgang.Conflict.Classic.Engine.Units.unit-types.json";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly IReadOnlyDictionary<string, UnitTypeDefinition> _byId;

    /// <summary>All unit type definitions, in catalog order.</summary>
    public IReadOnlyList<UnitTypeDefinition> All { get; }

    private UnitCatalog(IReadOnlyList<UnitTypeDefinition> types)
    {
        All = types;
        var byId = new Dictionary<string, UnitTypeDefinition>(StringComparer.Ordinal);
        foreach (var t in types)
        {
            if (byId.ContainsKey(t.Id))
            {
                throw new ArgumentException($"Duplicate unit type id '{t.Id}'.", nameof(types));
            }
            byId.Add(t.Id, t);
        }
        _byId = byId;
    }

    /// <summary>Loads the catalog from the embedded <c>unit-types.json</c> resource.</summary>
    /// <param name="cancellationToken">Token used to cancel the read.</param>
    /// <returns>The fully populated catalog.</returns>
    /// <exception cref="InvalidOperationException">The embedded resource is missing.</exception>
    /// <exception cref="ArgumentException">The JSON is malformed or contains duplicate ids.</exception>
    public static async Task<UnitCatalog> LoadEmbeddedAsync(CancellationToken cancellationToken = default)
    {
        var assembly = typeof(UnitCatalog).GetTypeInfo().Assembly;
        using var stream = assembly.GetManifestResourceStream(EmbeddedResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{EmbeddedResourceName}' not found.");
        return await LoadFromStreamAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Loads the catalog from a stream of UTF-8 JSON.</summary>
    /// <param name="stream">The JSON stream.</param>
    /// <param name="cancellationToken">Token used to cancel the read.</param>
    /// <returns>The fully populated catalog.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
    /// <exception cref="ArgumentException">The JSON is malformed or contains duplicate ids.</exception>
    public static async Task<UnitCatalog> LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        CatalogDocument? doc;
        try
        {
            doc = await JsonSerializer.DeserializeAsync<CatalogDocument>(stream, Options, cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Unit catalog JSON is malformed.", nameof(stream), ex);
        }

        if (doc?.UnitTypes is null)
        {
            throw new ArgumentException("Unit catalog JSON is empty or missing 'unitTypes'.", nameof(stream));
        }

        return new UnitCatalog(doc.UnitTypes);
    }

    /// <summary>Returns the definition for <paramref name="id"/>.</summary>
    /// <param name="id">The unit type id.</param>
    /// <returns>The matching definition.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is null.</exception>
    /// <exception cref="KeyNotFoundException">No unit type has that id.</exception>
    public UnitTypeDefinition Get(string id)
    {
        if (id is null)
        {
            throw new ArgumentNullException(nameof(id));
        }
        if (!_byId.TryGetValue(id, out var def))
        {
            throw new KeyNotFoundException($"No unit type with id '{id}'.");
        }
        return def;
    }

    /// <summary>Returns <see langword="true"/> if a unit type with that id exists.</summary>
    public bool Contains(string id) => id is not null && _byId.ContainsKey(id);

    /// <summary>
    /// Returns the unit types a side may field — those with a matching
    /// <see cref="UnitTypeDefinition.Side"/> or no side affinity at all.
    /// </summary>
    public IReadOnlyList<UnitTypeDefinition> ForSide(Side side)
    {
        var result = new List<UnitTypeDefinition>();
        foreach (var t in All)
        {
            if (t.Side is null || t.Side == side)
            {
                result.Add(t);
            }
        }
        return result;
    }

    private sealed class CatalogDocument
    {
        public List<UnitTypeDefinition>? UnitTypes { get; set; }
    }
}
