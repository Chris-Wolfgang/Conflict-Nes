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
    [InlineData(UnitKind.Infantry,    300)]   // 600 / 2
    [InlineData(UnitKind.Tank,       3000)]   // 6000 / 2
    [InlineData(UnitKind.Helicopter, 1200)]   // 2400 / 2
    [InlineData(UnitKind.Fighter,    3150)]   // 6300 / 2
    public void LoserPenalty_is_half_production_cost(UnitKind kind, int expected)
    {
        Assert.Equal(expected, EconomyRules.LoserPenalty(kind));
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
