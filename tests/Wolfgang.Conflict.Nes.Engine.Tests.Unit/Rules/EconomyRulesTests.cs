using Wolfgang.Conflict.Nes.Engine.Map;
using Wolfgang.Conflict.Nes.Engine.Rules;
using Wolfgang.Conflict.Nes.Engine.Units;
using Xunit;

namespace Wolfgang.Conflict.Nes.Engine.Tests.Rules;

public class EconomyRulesTests
{
    [Theory]
    [InlineData(BuildingKind.City, 100)]
    [InlineData(BuildingKind.Airbase, 100)]
    [InlineData(BuildingKind.Hq, 0)]
    [InlineData(BuildingKind.Factory, 0)]
    [InlineData(BuildingKind.Port, 0)]
    public void BuildingIncomePerTurn_only_city_and_airbase_generate(BuildingKind building, int expected)
    {
        Assert.Equal(expected, EconomyRules.BuildingIncomePerTurn(building));
    }

    [Theory]
    [InlineData(600, 300)]
    [InlineData(6000, 3000)]
    [InlineData(2400, 1200)]
    [InlineData(6301, 3150)]
    public void LoserPenalty_is_half_production_cost(int productionCost, int expected)
    {
        Assert.Equal(expected, EconomyRules.LoserPenalty(productionCost));
    }

    [Theory]
    [InlineData(MatchupTier.CompleteDefeat, 900)]
    [InlineData(MatchupTier.AtDisadvantage, 600)]
    [InlineData(MatchupTier.Equal,           300)]
    [InlineData(MatchupTier.AtAdvantage,     300)]
    [InlineData(MatchupTier.TotalVictory,    300)]
    public void WinnerReward_scales_with_upset_magnitude(MatchupTier tier, int expected)
    {
        Assert.Equal(expected, EconomyRules.WinnerReward(tier));
    }
}
