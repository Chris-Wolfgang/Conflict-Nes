using Wolfgang.Conflict.Classic.Engine.Players;
using Wolfgang.Conflict.Classic.Engine.Units;
using Xunit;

namespace Wolfgang.Conflict.Classic.Engine.Tests.Units;

public class UnitCatalogTests
{
    private static Task<UnitCatalog> Catalog() => UnitCatalog.LoadEmbeddedAsync();

    [Fact]
    public async Task LoadEmbeddedAsync_loads_full_roster()
    {
        var catalog = await Catalog();

        Assert.Equal(36, catalog.All.Count);
    }

    [Fact]
    public async Task Each_side_has_eighteen_units()
    {
        var catalog = await Catalog();

        Assert.Equal(18, catalog.ForSide(Side.Blue).Count);
        Assert.Equal(18, catalog.ForSide(Side.Red).Count);
    }

    [Theory]
    [InlineData("m1a1", "M1A1 Abrams", UnitCategory.BattleTank, 5, 8, 14, 6000)]
    [InlineData("f4e", "F-4E Phantom II", UnitCategory.Fighter, 10, 6, 8, 5000)]
    [InlineData("us-infantry", "US Infantry", UnitCategory.Infantry, 4, 10, 0, 0)]
    [InlineData("t80", "T-80", UnitCategory.BattleTank, 5, 8, 14, 6000)]
    public async Task Get_returns_expected_definition(
        string id, string name, UnitCategory category, int moving, int fuel, int shell, int cost)
    {
        var catalog = await Catalog();

        var def = catalog.Get(id);

        Assert.Equal(name, def.Name);
        Assert.Equal(category, def.Category);
        Assert.Equal(moving, def.MovementPoints);
        Assert.Equal(fuel, def.MaxFuel);
        Assert.Equal(shell, def.MaxAmmo);
        Assert.Equal(cost, def.ProductionCost);
    }

    [Fact]
    public async Task Get_unknown_id_throws()
    {
        var catalog = await Catalog();

        Assert.Throws<KeyNotFoundException>(() => catalog.Get("does-not-exist"));
    }

    [Fact]
    public async Task Only_infantry_and_commando_can_capture()
    {
        var catalog = await Catalog();

        foreach (var def in catalog.All)
        {
            var expectCapture = def.Category is UnitCategory.Infantry or UnitCategory.Commando;
            Assert.Equal(expectCapture, def.CanCapture);
        }
    }

    [Fact]
    public async Task Battle_tanks_and_fighters_have_maneuver5()
    {
        var catalog = await Catalog();

        foreach (var def in catalog.All)
        {
            var expectM5 = def.Category is UnitCategory.BattleTank or UnitCategory.Fighter;
            Assert.Equal(expectM5, def.HasManeuver5);
        }
    }

    [Fact]
    public async Task ForSide_blue_excludes_red_only_units()
    {
        var catalog = await Catalog();

        var blue = catalog.ForSide(Side.Blue);

        Assert.Contains(blue, d => string.Equals(d.Id, "m1a1", StringComparison.Ordinal));
        Assert.DoesNotContain(blue, d => string.Equals(d.Id, "t80", StringComparison.Ordinal));
    }

    [Fact]
    public async Task All_ids_are_unique()
    {
        var catalog = await Catalog();

        var ids = catalog.All.Select(d => d.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
    }
}
