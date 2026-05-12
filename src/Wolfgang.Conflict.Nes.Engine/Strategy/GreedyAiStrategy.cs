using Wolfgang.Conflict.Nes.Engine.Game;
using Wolfgang.Conflict.Nes.Engine.Hex;
using Wolfgang.Conflict.Nes.Engine.Players;
using Wolfgang.Conflict.Nes.Engine.Rules;
using Wolfgang.Conflict.Nes.Engine.Units;

namespace Wolfgang.Conflict.Nes.Engine.Strategy;

/// <summary>
/// Greedy reference AI. Per call, finds the first unit on the side that can
/// still act and returns a sensible action: attack the strongest-matchup
/// adjacent enemy if possible, otherwise move toward the nearest enemy,
/// otherwise wait. Returns <see cref="StrategyAction.EndTurn"/> when no
/// unit has any action left.
/// </summary>
public sealed class GreedyAiStrategy : IPlayerStrategy
{
    /// <inheritdoc/>
    public Task<StrategyAction> ChooseNextActionAsync(GameState state, Side side, CancellationToken cancellationToken = default)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        return Task.FromResult(Choose(state, side));
    }

    private static StrategyAction Choose(GameState state, Side side)
    {
        foreach (var unit in state.Units.Values)
        {
            if (unit.Side != side)
            {
                continue;
            }

            if (!unit.HasAttacked && TryAttack(state, unit, out var attack))
            {
                return attack;
            }

            if (!unit.HasMoved && TryMoveTowardEnemy(state, unit, side, out var move))
            {
                return move;
            }
        }

        return StrategyAction.EndTurn;
    }

    private static bool TryAttack(GameState state, Unit unit, out StrategyAction action)
    {
        var targets = AttackRules.GetLegalTargets(state, unit);
        if (targets.Count == 0)
        {
            action = StrategyAction.EndTurn;
            return false;
        }

        // Pick the target whose matchup tier from our perspective is best.
        var bestTarget = targets[0];
        var bestTier = (int)Matchups.Outlook(unit.Kind, state.Units[bestTarget].Kind);
        for (var i = 1; i < targets.Count; i++)
        {
            var tier = (int)Matchups.Outlook(unit.Kind, state.Units[targets[i]].Kind);
            if (tier > bestTier)
            {
                bestTier = tier;
                bestTarget = targets[i];
            }
        }

        action = StrategyAction.Attack(unit.Id, bestTarget);
        return true;
    }

    private static bool TryMoveTowardEnemy(GameState state, Unit unit, Side side, out StrategyAction action)
    {
        Unit? nearestEnemy = null;
        var bestDistance = int.MaxValue;
        foreach (var other in state.Units.Values)
        {
            if (other.Side == side)
            {
                continue;
            }
            var d = unit.Coord.DistanceTo(other.Coord);
            if (d < bestDistance)
            {
                bestDistance = d;
                nearestEnemy = other;
            }
        }

        if (nearestEnemy is null)
        {
            action = StrategyAction.EndTurn;
            return false;
        }

        var reachable = MovementRules.GetReachable(state, unit);
        if (reachable.Count == 0)
        {
            action = StrategyAction.EndTurn;
            return false;
        }

        HexCoord bestHex = reachable[0];
        var bestDistanceToEnemy = bestHex.DistanceTo(nearestEnemy.Coord);
        for (var i = 1; i < reachable.Count; i++)
        {
            var d = reachable[i].DistanceTo(nearestEnemy.Coord);
            if (d < bestDistanceToEnemy)
            {
                bestDistanceToEnemy = d;
                bestHex = reachable[i];
            }
        }

        action = StrategyAction.Move(unit.Id, bestHex);
        return true;
    }
}
