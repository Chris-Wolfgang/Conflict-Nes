using Wolfgang.Conflict.Nes.Engine.Game;
using Wolfgang.Conflict.Nes.Engine.Hex;
using Wolfgang.Conflict.Nes.Engine.Players;
using Wolfgang.Conflict.Nes.Engine.Rules;
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
    /// <summary>Delay between successive AI actions so the player can follow the AI's turn.</summary>
    private const int AiActionDelayMs = 650;

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

    /// <summary>
    /// A pending attack awaiting player confirmation: the attacker and the
    /// chosen target. <see langword="null"/> when no attack is pending.
    /// </summary>
    public (UnitId Attacker, UnitId Target)? PendingAttack { get; private set; }

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

        // While an attack is awaiting confirmation, board clicks are ignored
        // — the player must resolve the prompt first.
        if (PendingAttack is not null)
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

        // Clicking an enemy on the selected unit's target list raises a
        // pending attack; the player must then confirm it.
        if (unitAtHex is { } target && target.Side != HumanSide)
        {
            var targets = _engine.GetLegalTargets(CurrentState, selected.Id);
            if (targets.Contains(target.Id))
            {
                PendingAttack = (selected.Id, target.Id);
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

    /// <summary>
    /// Returns true if the selected unit is currently eligible to invoke
    /// the once-per-turn Supply command.
    /// </summary>
    public bool CanSupplySelected()
    {
        if (CurrentState is null || !IsHumanTurn || SelectedUnitId is null)
        {
            return false;
        }
        var unit = CurrentState.Units[SelectedUnitId.Value];
        return SupplyRules.AvailableSource(CurrentState, unit) is not null;
    }

    /// <summary>
    /// Invokes the Supply command on the currently selected unit. Throws via
    /// the engine if the unit is not eligible (caller should gate the UI
    /// with <see cref="CanSupplySelected"/>).
    /// </summary>
    public void SupplySelected()
    {
        if (CurrentState is null || !IsHumanTurn || SelectedUnitId is null)
        {
            return;
        }
        CurrentState = _engine.SupplyUnit(CurrentState, SelectedUnitId.Value);
        Notify();
    }

    /// <summary>Resolves the pending attack — applies it through the engine.</summary>
    public void ConfirmAttack()
    {
        if (CurrentState is null || PendingAttack is not { } pending)
        {
            return;
        }
        CurrentState = _engine.AttackUnit(CurrentState, pending.Attacker, pending.Target);
        PendingAttack = null;
        SelectedUnitId = CurrentState.Units.ContainsKey(pending.Attacker) ? pending.Attacker : null;
        Notify();
    }

    /// <summary>Discards the pending attack without applying it.</summary>
    public void CancelAttack()
    {
        if (PendingAttack is not null)
        {
            PendingAttack = null;
            Notify();
        }
    }

    /// <summary>Clears any current selection.</summary>
    public void ClearSelection()
    {
        if (SelectedUnitId is not null || PendingAttack is not null)
        {
            SelectedUnitId = null;
            PendingAttack = null;
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

            // Pace the AI so the player can watch each move/attack/supply
            // play out one step at a time rather than all at once.
            if (action.Kind != StrategyActionKind.EndTurn)
            {
                await Task.Delay(AiActionDelayMs, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private GameState ApplyAction(StrategyAction action, GameState state) => action.Kind switch
    {
        StrategyActionKind.EndTurn => _engine.EndTurn(state),
        StrategyActionKind.Move    => _engine.MoveUnit(state, action.UnitId, action.Hex),
        StrategyActionKind.Attack  => _engine.AttackUnit(state, action.UnitId, action.TargetId),
        StrategyActionKind.Supply  => _engine.SupplyUnit(state, action.UnitId),
        StrategyActionKind.Build   => _engine.BuildUnit(state, action.Hex, action.ProduceTypeId),
        _ => state,
    };

    private void Notify() => StateChanged?.Invoke(this, EventArgs.Empty);
}
