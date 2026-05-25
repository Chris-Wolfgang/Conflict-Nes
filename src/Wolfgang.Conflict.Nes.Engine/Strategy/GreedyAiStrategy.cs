using Wolfgang.Conflict.Nes.Engine.Combat;
using Wolfgang.Conflict.Nes.Engine.Game;
using Wolfgang.Conflict.Nes.Engine.Hex;
using Wolfgang.Conflict.Nes.Engine.Map;
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
        // Difficulty 1 doctrine: spend the once-per-turn build first.
        // Turn 1 buys aircraft; turn 2 buys ground; alternates forever.
        // Within a factory, take the most expensive thing we can afford.
        if (!state.HasBuiltThisTurn(side) && TryBuild(state, side, out var build))
        {
            return build;
        }

        foreach (var unit in state.Units.Values)
        {
            if (unit.Side != side)
            {
                continue;
            }

            // The commander is the "H" — losing it ends the game. It holds
            // its ground (ideally a friendly city for supply / repair), only
            // counter-attacking enemies that walk adjacent. It never marches
            // out to look for trouble.
            if (!unit.HasAttacked && TryAttack(state, unit, out var attack))
            {
                return attack;
            }

            if (unit.IsCommander)
            {
                continue;
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
        var bestTier = (int)RelationsTable.Outlook(unit.Category, state.Units[bestTarget].Category);
        for (var i = 1; i < targets.Count; i++)
        {
            var tier = (int)RelationsTable.Outlook(unit.Category, state.Units[targets[i]].Category);
            if (tier > bestTier)
            {
                bestTier = tier;
                bestTarget = targets[i];
            }
        }

        action = StrategyAction.Attack(unit.Id, bestTarget);
        return true;
    }

    private static bool TryBuild(GameState state, Side side, out StrategyAction action)
    {
        action = StrategyAction.EndTurn;

        // Alternate by turn number: odd turns -> air, even turns -> land.
        // Fall back to the other kind if the preferred factory is gone or
        // already occupied so the AI still produces something on its turn.
        var primary = (state.TurnNumber % 2) == 1 ? BuildingKind.AirFactory : BuildingKind.LandFactory;
        var secondary = primary == BuildingKind.AirFactory ? BuildingKind.LandFactory : BuildingKind.AirFactory;

        return TryBuildAt(state, side, primary, out action)
            || TryBuildAt(state, side, secondary, out action);
    }

    private static bool TryBuildAt(GameState state, Side side, BuildingKind kind, out StrategyAction action)
    {
        action = StrategyAction.EndTurn;

        var hex = FindBuildableFactoryHex(state, side, kind);
        if (hex is null)
        {
            return false;
        }

        // Most expensive thing we can pay for right now.
        var affordable = ProductionRules.AffordableAt(state, kind, side);
        if (affordable.Count == 0)
        {
            return false;
        }
        var best = affordable[0];
        for (var i = 1; i < affordable.Count; i++)
        {
            if (affordable[i].ProductionCost > best.ProductionCost)
            {
                best = affordable[i];
            }
        }

        action = StrategyAction.Build(hex.Value, best.Id);
        return true;
    }

    private static HexCoord? FindBuildableFactoryHex(GameState state, Side side, BuildingKind kind)
    {
        foreach (var tile in state.Map.Tiles.Values)
        {
            if (tile.Building != kind)
            {
                continue;
            }
            if (state.GetBuildingOwner(tile.Coord) != side)
            {
                continue;
            }
            if (!state.HasIntactBuilding(tile.Coord))
            {
                continue;
            }
            if (state.GetUnitAt(tile.Coord) is not null)
            {
                // A unit standing on the factory hex blocks production there.
                continue;
            }
            return tile.Coord;
        }
        return null;
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
