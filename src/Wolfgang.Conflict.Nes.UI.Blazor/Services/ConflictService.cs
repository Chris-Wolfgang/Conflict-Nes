using Wolfgang.Conflict.Nes.Engine.Game;
using Wolfgang.Conflict.Nes.Engine.Hex;
using Wolfgang.Conflict.Nes.Engine.Players;
using Wolfgang.Conflict.Nes.Engine.Strategy;
using Wolfgang.Conflict.Nes.Engine.Units;

namespace Wolfgang.Conflict.Nes.UI.Blazor.Services;

/// <summary>
/// Holds the current <see cref="GameState"/> for the Blazor UI, surfaces
/// turn-by-turn state to view models, and dispatches player commands
/// through <see cref="GameEngine"/>. Mirrors Hawsey's <c>GameService</c>.
/// </summary>
public sealed class ConflictService
{
    private readonly GameEngine _engine = new();
    private readonly IPlayerStrategy _aiStrategy = new GreedyAiStrategy();

    /// <summary>Which side is human-controlled in this UI session.</summary>
    public Side HumanSide { get; } = Side.Blue;

    /// <summary>Fired whenever <see cref="CurrentState"/> changes.</summary>
    public event EventHandler? StateChanged;

    /// <summary>The current game state, or <see langword="null"/> if no mission has been started.</summary>
    public GameState? CurrentState { get; private set; }

    /// <summary>The id of the currently-selected unit, if any.</summary>
    public UnitId? SelectedUnitId { get; private set; }

    /// <summary>True if a mission has been started.</summary>
    public bool IsStarted => CurrentState is not null;

    /// <summary>True if it is currently the human's turn.</summary>
    public bool IsHumanTurn => CurrentState is { Phase: GamePhase.PlayerTurn } s && s.NextToAct == HumanSide;

    /// <summary>Starts a new Mission 01 game with a fresh RNG seed.</summary>
    public async Task StartMission01Async(int? seed = null, CancellationToken cancellationToken = default)
    {
        var mission = await MissionLoader.LoadMission01Async(cancellationToken).ConfigureAwait(false);
        var rngSeed = seed ?? unchecked((int)DateTime.UtcNow.Ticks);
        CurrentState = _engine.StartGame(mission, rngSeed);
        SelectedUnitId = null;
        Notify();
    }

    /// <summary>
    /// Handles a click on <paramref name="hex"/>. Selects friendly units,
    /// moves a selected unit to a reachable hex, or attacks an enemy with
    /// the selected unit. No-ops if not the human's turn.
    /// </summary>
    public void OnHexClicked(HexCoord hex)
    {
        if (CurrentState is null || !IsHumanTurn)
        {
            return;
        }

        var unitAtHex = CurrentState.GetUnitAt(hex);

        if (SelectedUnitId is null)
        {
            // No selection yet: clicking a friendly unit selects it.
            if (unitAtHex is { } u && u.Side == HumanSide)
            {
                SelectedUnitId = u.Id;
                Notify();
            }
            return;
        }

        var selected = CurrentState.Units[SelectedUnitId.Value];

        // Clicking another friendly unit reselects.
        if (unitAtHex is { } other && other.Side == HumanSide && other.Id != selected.Id)
        {
            SelectedUnitId = other.Id;
            Notify();
            return;
        }

        // Clicking an enemy on the selected unit's target list: attack.
        if (unitAtHex is { } target && target.Side != HumanSide)
        {
            var targets = _engine.GetLegalTargets(CurrentState, selected.Id);
            if (targets.Contains(target.Id))
            {
                CurrentState = _engine.AttackUnit(CurrentState, selected.Id, target.Id);
                SelectedUnitId = null;
                Notify();
            }
            return;
        }

        // Clicking an empty reachable hex: move.
        if (unitAtHex is null)
        {
            var moves = _engine.GetLegalMoves(CurrentState, selected.Id);
            if (moves.Contains(hex))
            {
                CurrentState = _engine.MoveUnit(CurrentState, selected.Id, hex);
                SelectedUnitId = CurrentState.Units.ContainsKey(selected.Id) ? selected.Id : null;
                Notify();
            }
        }
    }

    /// <summary>Clears any current selection.</summary>
    public void ClearSelection()
    {
        if (SelectedUnitId is not null)
        {
            SelectedUnitId = null;
            Notify();
        }
    }

    /// <summary>
    /// Ends the human's turn, then runs the AI strategy to completion on
    /// the AI side. Returns control to the UI when it is the human's turn
    /// again (or when the game ends).
    /// </summary>
    public async Task EndTurnAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentState is null || !IsHumanTurn)
        {
            return;
        }

        CurrentState = _engine.EndTurn(CurrentState);
        SelectedUnitId = null;
        Notify();

        // Drive the AI's whole phase synchronously (it's fast).
        await RunAiPhaseAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task RunAiPhaseAsync(CancellationToken cancellationToken)
    {
        const int safetyCap = 200;
        var actions = 0;
        while (CurrentState is { Phase: GamePhase.PlayerTurn } s
               && s.NextToAct != HumanSide
               && actions++ < safetyCap)
        {
            var action = await _aiStrategy.ChooseNextActionAsync(s, s.NextToAct, cancellationToken).ConfigureAwait(false);
            CurrentState = ApplyAction(action, s);
            Notify();
        }
    }

    private GameState ApplyAction(StrategyAction action, GameState state) => action.Kind switch
    {
        StrategyActionKind.EndTurn => _engine.EndTurn(state),
        StrategyActionKind.Move    => _engine.MoveUnit(state, action.UnitId, action.Hex),
        StrategyActionKind.Attack  => _engine.AttackUnit(state, action.UnitId, action.TargetId),
        StrategyActionKind.Supply  => _engine.SupplyUnit(state, action.UnitId),
        StrategyActionKind.Build   => _engine.BuildUnit(state, action.Hex, action.ProduceKind),
        _ => state,
    };

    private void Notify() => StateChanged?.Invoke(this, EventArgs.Empty);
}
