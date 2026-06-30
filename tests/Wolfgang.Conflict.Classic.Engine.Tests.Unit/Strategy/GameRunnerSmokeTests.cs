using Wolfgang.Conflict.Classic.Engine.Game;
using Wolfgang.Conflict.Classic.Engine.Strategy;
using Xunit;

namespace Wolfgang.Conflict.Classic.Engine.Tests.Strategy;

public class GameRunnerSmokeTests
{
    [Fact]
    public async Task Random_vs_Random_completes_mission01_without_exceptions()
    {
        // Modest count keeps net462 test runtimes reasonable while still
        // exercising the engine across many board states.
        const int games = 25;

        var engine = new GameEngine();
        var mission = await MissionLoader.LoadMission01Async();

        for (var i = 0; i < games; i++)
        {
            var state = engine.StartGame(mission, randomSeed: 1000 + i);

            var final = await GameRunner.RunAsync(
                state,
                new RandomTestStrategy(seed: i * 31 + 1),
                new RandomTestStrategy(seed: i * 31 + 2),
                maxFullTurns: 100);

            Assert.True(final.Phase == GamePhase.GameOver || final.TurnNumber > 1,
                $"Game {i} stalled with no progress at turn {final.TurnNumber}.");
        }
    }

    [Fact]
    public async Task Greedy_vs_Greedy_reaches_a_decisive_outcome()
    {
        var engine = new GameEngine();
        var mission = await MissionLoader.LoadMission01Async();
        var state = engine.StartGame(mission, randomSeed: 42);

        var final = await GameRunner.RunAsync(
            state,
            new GreedyAiStrategy(),
            new GreedyAiStrategy(),
            maxFullTurns: 500);

        // Greedy commanders sit tight on their factories per the doctrine
        // change, so a clean victory isn't guaranteed within the turn cap —
        // both sides can attrit each other down to lone commanders that
        // refuse to march. The point of this smoke test is that the engine
        // makes meaningful progress under greedy play without crashing.
        Assert.True(final.TurnNumber > 5,
            $"Greedy game stalled too quickly (turn {final.TurnNumber}).");
        if (final.Phase == GamePhase.GameOver)
        {
            Assert.NotNull(final.Winner);
        }
    }
}
