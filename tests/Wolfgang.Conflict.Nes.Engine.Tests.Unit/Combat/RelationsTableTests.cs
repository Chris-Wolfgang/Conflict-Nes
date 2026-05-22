using Wolfgang.Conflict.Nes.Engine.Combat;
using Wolfgang.Conflict.Nes.Engine.Units;
using Xunit;

namespace Wolfgang.Conflict.Nes.Engine.Tests.Combat;

public class RelationsTableTests
{
    [Theory]
    [InlineData(UnitCategory.Fighter, UnitCategory.Helicopter, true)]
    [InlineData(UnitCategory.FlakPanzer, UnitCategory.Fighter, true)]
    [InlineData(UnitCategory.BattleTank, UnitCategory.Infantry, true)]
    [InlineData(UnitCategory.BattleTank, UnitCategory.Fighter, false)]
    [InlineData(UnitCategory.Infantry, UnitCategory.Fighter, false)]
    [InlineData(UnitCategory.SupplyVehicle, UnitCategory.Infantry, false)]
    [InlineData(UnitCategory.SupplyPlane, UnitCategory.Fighter, false)]
    public void CanEngage_matches_design(UnitCategory attacker, UnitCategory defender, bool expected)
    {
        Assert.Equal(expected, RelationsTable.CanEngage(attacker, defender));
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
    public void BaseAttack_is_zero_when_cannot_engage()
    {
        Assert.Equal(0, RelationsTable.BaseAttack(UnitCategory.BattleTank, UnitCategory.Fighter));
        Assert.Equal(0, RelationsTable.BaseAttack(UnitCategory.SupplyVehicle, UnitCategory.Infantry));
    }

    [Fact]
    public void BaseAttack_is_positive_for_engageable_pairs()
    {
        Assert.True(RelationsTable.BaseAttack(UnitCategory.BattleTank, UnitCategory.Infantry) > 0);
        Assert.True(RelationsTable.BaseAttack(UnitCategory.Fighter, UnitCategory.Helicopter) > 0);
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
