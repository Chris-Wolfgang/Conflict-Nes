using Wolfgang.Conflict.Classic.Engine.Game;
using Xunit;

namespace Wolfgang.Conflict.Classic.Engine.Tests;

public class SolutionBuildsTests
{
    [Fact]
    public void GameEngine_can_be_constructed()
    {
        var sut = new GameEngine();

        Assert.NotNull(sut);
    }
}
