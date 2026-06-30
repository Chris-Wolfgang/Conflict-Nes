using Wolfgang.Conflict.Classic.Engine.Game;
using Wolfgang.Conflict.Classic.Engine.Players;

namespace Wolfgang.Conflict.Classic.Engine.Strategy;

/// <summary>
/// Outward seam for plugging an AI (or remote/human-input) decision maker
/// into the engine. Implementations receive the current <see cref="GameState"/>
/// and return a sequence of commands to apply on the active side's turn,
/// ending with <see cref="StrategyAction.EndTurn"/>.
/// </summary>
public interface IPlayerStrategy
{
    /// <summary>
    /// Returns the next action the named <paramref name="side"/> wishes to
    /// take. Called repeatedly by the orchestrator until the strategy
    /// returns <see cref="StrategyAction.EndTurn"/>.
    /// </summary>
    /// <param name="state">The current game state.</param>
    /// <param name="side">The side this strategy controls (always <c>state.NextToAct</c>).</param>
    /// <param name="cancellationToken">Token used to cancel decision-making.</param>
    Task<StrategyAction> ChooseNextActionAsync(GameState state, Side side, CancellationToken cancellationToken = default);
}
