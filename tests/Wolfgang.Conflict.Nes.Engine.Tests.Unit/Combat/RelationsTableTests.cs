using Wolfgang.Conflict.Nes.Engine.Combat;
using Wolfgang.Conflict.Nes.Engine.Units;
using Xunit;

namespace Wolfgang.Conflict.Nes.Engine.Tests.Combat;

public class RelationsTableTests
{
    // Every unit carries at least a machine gun, so every pair can engage —
    // effectiveness is governed by the matchup tier, not by an engagement gate.
    [Theory]
    [InlineData(UnitCategory.Fighter, UnitCategory.Helicopter)]
    [InlineData(UnitCategory.FlakPanzer, UnitCategory.Fighter)]
    [InlineData(UnitCategory.BattleTank, UnitCategory.Infantry)]
    [InlineData(UnitCategory.BattleTank, UnitCategory.Fighter)]
    [InlineData(UnitCategory.Infantry, UnitCategory.Helicopter)]
    [InlineData(UnitCategory.Commando, UnitCategory.Helicopter)]
    [InlineData(UnitCategory.Helicopter, UnitCategory.Fighter)]
    [InlineData(UnitCategory.SupplyVehicle, UnitCategory.Infantry)]
    [InlineData(UnitCategory.SupplyPlane, UnitCategory.Fighter)]
    [InlineData(UnitCategory.Attacker, UnitCategory.Helicopter)]
    public void CanEngage_is_always_true(UnitCategory attacker, UnitCategory defender)
    {
        Assert.True(RelationsTable.CanEngage(attacker, defender));
    }

    [Theory]
    [InlineData(UnitCategory.BattleTank, UnitCategory.Infantry, MatchupTier.TotalVictory)]
    [InlineData(UnitCategory.Fighter, UnitCategory.Helicopter, MatchupTier.TotalVictory)]
    [InlineData(UnitCategory.Fighter, UnitCategory.Fighter, MatchupTier.Equal)]
    [InlineData(UnitCategory.BattleMissileLauncher, UnitCategory.BattleTank, MatchupTier.AtAdvantage)]
    [InlineData(UnitCategory.Infantry, UnitCategory.BattleTank, MatchupTier.CompleteDefeat)]
    public void Outlook_matches_design(UnitCategory attacker, UnitCategory defender, MatchupTier expected)
    {
        Assert.Equal(expected, RelationsTable.Outlook(attacker, defender));
    }

    [Fact]
    public void BaseAttack_is_positive_for_every_pair()
    {
        // Every unit carries at least a machine gun, so every matchup yields
        // a positive (if sometimes very small) base attack value.
        var categories = (UnitCategory[])Enum.GetValues(typeof(UnitCategory));
        foreach (var atk in categories)
        {
            foreach (var def in categories)
            {
                Assert.True(RelationsTable.BaseAttack(atk, def) > 0,
                    $"BaseAttack({atk}, {def}) should be > 0");
            }
        }
    }

    [Fact]
    public void BaseAttack_is_higher_for_strong_matchups_than_weak_ones()
    {
        var strong = RelationsTable.BaseAttack(UnitCategory.BattleTank, UnitCategory.Infantry);
        var weak = RelationsTable.BaseAttack(UnitCategory.Infantry, UnitCategory.Fighter);
        Assert.True(strong > weak);
    }

    [Theory]
    [InlineData(UnitCategory.Fighter, true)]
    [InlineData(UnitCategory.Helicopter, true)]
    [InlineData(UnitCategory.Attacker, true)]
    [InlineData(UnitCategory.SupplyPlane, true)]
    [InlineData(UnitCategory.BattleTank, false)]
    [InlineData(UnitCategory.Infantry, false)]
    public void IsAir_classifies_categories(UnitCategory category, bool expected)
    {
        Assert.Equal(expected, RelationsTable.IsAir(category));
    }
}
