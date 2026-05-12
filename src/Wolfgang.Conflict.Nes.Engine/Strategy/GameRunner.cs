using Wolfgang.Conflict.Nes.Engine.Game;
using Wolfgang.Conflict.Nes.Engine.Players;

namespace Wolfgang.Conflict.Nes.Engine.Strategy;

/// <summary>
/// Drives a complete game by alternately consulting two
/// <see cref="IPlayerStrategy"/> instances and applying each requested
/// <see cref="StrategyAction"/> through a <see cref="GameEngine"/>.
/// </summary>
public static class GameRunner
{
    /// <summary>
    /// Runs the game from <paramref name="initialState"/> until either
    /// <see cref="GamePhase.GameOver"/> is reached or
    /// <paramref name="maxFullTurns"/> have elapsed (a full turn is one
    /// Blue + one Red phase). Each per-side turn is also capped at
    /// <paramref name="maxActionsPerTurn"/> commands to guard against
    /// strategies that fail to return <see cref="StrategyAction.EndTurn"/>.
    /// </summary>
    /// <param name="initialState">The starting state, typically from <c>GameEngine.StartGame</c>.</param>
    /// <param name="blueStrategy">Decision maker for the Blue side.</param>
    /// <param name="redStrategy">Decision maker for the Red side.</param>
    /// <param name="maxFullTurns">Maximum full turns (Blue+Red) before aborting.</param>
    /// <param name="maxActionsPerTurn">Maximum actions per side per turn.</param>
    /// <param name="cancellationToken">Token used to cancel the run.</param>
    /// <returns>The final state at game over or at the cap.</returns>
    /// <exception cref="ArgumentNullException">Any required argument is null.</exception>
    public static async Task<GameState> RunAsync(
        GameState initialState,
        IPlayerStrategy blueStrategy,
        IPlayerStrategy redStrategy,
        int maxFullTurns = 200,
        int maxActionsPerTurn = 100,
        CancellationToken cancellationToken = default)
    {
        if (initialState is null)
        {
            throw new ArgumentNullException(nameof(initialState));
        }
        if (blueStrategy is null)
        {
            throw new ArgumentNullException(nameof(blueStrategy));
        }
        if (redStrategy is null)
        {
            throw new ArgumentNullException(nameof(redStrategy));
        }

        var engine = new GameEngine();
        var state = initialState;
        var fullTurns = 0;

        while (state.Phase != GamePhase.GameOver && fullTurns < maxFullTurns)
        {
            var strategy = state.NextToAct == Side.Blue ? blueStrategy : redStrategy;
            state = await RunOneSidePhaseAsync(engine, state, strategy, maxActionsPerTurn, cancellationToken).ConfigureAwait(false);

            if (state.NextToAct == Side.Blue)
            {
                fullTurns++;
            }
        }

        return state;
    }

    private static async Task<GameState> RunOneSidePhaseAsync(
        GameEngine engine,
        GameState state,
        IPlayerStrategy strategy,
        int maxActions,
        CancellationToken ct)
    {
        var sideAtStart = state.NextToAct;
        for (var i = 0; i < maxActions; i++)
        {
            if (state.Phase == GamePhase.GameOver)
            {
                return state;
            }

            var action = await strategy.ChooseNextActionAsync(state, sideAtStart, ct).ConfigureAwait(false);
            state = Apply(engine, state, action);

            if (action.Kind == StrategyActionKind.EndTurn || state.NextToAct != sideAtStart)
            {
                return state;
            }
        }

        // Safety: if the strategy never asked for EndTurn, force it.
        return state.Phase == GamePhase.GameOver ? state : engine.EndTurn(state);
    }

    private static GameState Apply(GameEngine engine, GameState state, StrategyAction action) => action.Kind switch
    {
        StrategyActionKind.EndTurn => engine.EndTurn(state),
        StrategyActionKind.Move    => engine.MoveUnit(state, action.UnitId, action.Hex),
        StrategyActionKind.Attack  => engine.AttackUnit(state, action.UnitId, action.TargetId),
        StrategyActionKind.Supply  => engine.SupplyUnit(state, action.UnitId),
        StrategyActionKind.Build   => engine.BuildUnit(state, action.Hex, action.ProduceKind),
        _ => throw new InvalidOperationException($"Unknown action kind {action.Kind}."),
    };
}
