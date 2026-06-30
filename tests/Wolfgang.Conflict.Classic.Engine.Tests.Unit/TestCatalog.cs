using Wolfgang.Conflict.Classic.Engine.Units;

namespace Wolfgang.Conflict.Classic.Engine.Tests;

/// <summary>
/// Shared test fixture exposing the embedded unit catalog plus convenient
/// accessors for representative unit types, so tests need not thread the
/// catalog or unit-type ids everywhere.
/// </summary>
internal static class TestCatalog
{
    /// <summary>The full embedded catalog, loaded once.</summary>
    public static UnitCatalog Catalog { get; } = UnitCatalog.LoadEmbeddedAsync().GetAwaiter().GetResult();

    /// <summary>US Infantry — Blue foot soldier.</summary>
    public static UnitTypeDefinition Infantry => Catalog.Get("us-infantry");

    /// <summary>M60A3 — Blue battle tank (the factory-buildable MBT; M1A1 is HQ-only).</summary>
    public static UnitTypeDefinition Tank => Catalog.Get("m60a3");

    /// <summary>AH-1S Huey Cobra — Blue helicopter.</summary>
    public static UnitTypeDefinition Helicopter => Catalog.Get("ah1s");

    /// <summary>F-4E Phantom II — Blue fighter.</summary>
    public static UnitTypeDefinition Fighter => Catalog.Get("f4e");

    /// <summary>Looks up any unit type by catalog id.</summary>
    public static UnitTypeDefinition Get(string id) => Catalog.Get(id);
}
