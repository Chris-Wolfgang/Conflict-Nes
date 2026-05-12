using Wolfgang.Conflict.Nes.Engine.Game;
using Xunit;

namespace Wolfgang.Conflict.Nes.Engine.Tests;

public class SolutionBuildsTests
{
    [Fact]
    public void GameEngine_can_be_constructed()
    {
        var sut = new GameEngine();

        Assert.NotNull(sut);
    }
}
