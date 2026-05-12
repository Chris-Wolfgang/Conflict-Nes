using Wolfgang.Conflict.Nes.Engine.Game;
using Wolfgang.Conflict.Nes.Engine.Players;
using Wolfgang.Conflict.Nes.Engine.Rules;
using Wolfgang.Conflict.Nes.Engine.Strategy;
using Wolfgang.Conflict.Nes.Engine.Units;

namespace Wolfgang.Conflict.Nes.Engine.Tests.Strategy;

/// <summary>
/// Picks a legal action uniformly at random from the per-turn action set.
/// Used by the headless smoke test to exercise the engine for many games
/// without crashing.
/// </summary>
internal sealed class RandomTestStrategy : IPlayerStrategy
{
    private readonly Random _random;

    public RandomTestStrategy(int seed)
    {
        _random = new Random(seed);
    }

    public Task<StrategyAction> ChooseNextActionAsync(GameState state, Side side, CancellationToken ct = default)
    {
        var candidates = EnumerateActions(state, side).ToList();
        // Always include EndTurn so games can't run forever.
        candidates.Add(StrategyAction.EndTurn);
        var pick = candidates[_random.Next(candidates.Count)];
        return Task.FromResult(pick);
    }

    private static IEnumerable<StrategyAction> EnumerateActions(GameState state, Side side)
    {
        foreach (var unit in state.Units.Values)
        {
            if (unit.Side != side)
            {
                continue;
            }
            if (!unit.HasAttacked)
            {
                foreach (var target in AttackRules.GetLegalTargets(state, unit))
                {
                    yield return StrategyAction.Attack(unit.Id, target);
                }
            }
            if (!unit.HasMoved)
            {
                foreach (var hex in MovementRules.GetReachable(state, unit))
                {
                    yield return StrategyAction.Move(unit.Id, hex);
                }
            }
        }
    }
}
