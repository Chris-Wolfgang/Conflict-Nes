using Wolfgang.Conflict.Nes.Engine.Units;
using Xunit;

namespace Wolfgang.Conflict.Nes.Engine.Tests.Units;

public class MatchupsTests
{
    [Theory]
    [InlineData(UnitKind.Tank,       UnitKind.Infantry,   MatchupTier.TotalVictory)]
    [InlineData(UnitKind.Helicopter, UnitKind.Tank,       MatchupTier.AtAdvantage)]
    [InlineData(UnitKind.Fighter,    UnitKind.Helicopter, MatchupTier.TotalVictory)]
    [InlineData(UnitKind.Helicopter, UnitKind.Fighter,    MatchupTier.AtDisadvantage)]
    [InlineData(UnitKind.Tank,       UnitKind.Fighter,    MatchupTier.CompleteDefeat)]
    [InlineData(UnitKind.Infantry,   UnitKind.Tank,       MatchupTier.CompleteDefeat)]
    [InlineData(UnitKind.Fighter,    UnitKind.Fighter,    MatchupTier.Equal)]
    [InlineData(UnitKind.Tank,       UnitKind.Tank,       MatchupTier.Equal)]
    public void Outlook_matches_design_table(UnitKind attacker, UnitKind defender, MatchupTier expected)
    {
        Assert.Equal(expected, Matchups.Outlook(attacker, defender));
    }
}
