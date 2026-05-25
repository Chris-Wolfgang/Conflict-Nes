using Wolfgang.Conflict.Nes.Engine.Game;
using Wolfgang.Conflict.Nes.Engine.Hex;
using Wolfgang.Conflict.Nes.Engine.Map;
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

    /// <summary>How long the AI's production-menu preview lingers before closing.</summary>
    private const int AiBuildPreviewMs = 3200;

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
    /// A pending attack awaiting player confirmation. Exactly one of
    /// <see cref="PendingAttackInfo.TargetUnitId"/> or
    /// <see cref="PendingAttackInfo.TargetBuildingCoord"/> is set.
    /// </summary>
    public PendingAttackInfo? PendingAttack { get; private set; }

    /// <summary>
    /// The factory hex whose production screen is open, or <see langword="null"/>
    /// when no menu is up. The player can open any factory's menu to browse
    /// even after they've already built this turn.
    /// </summary>
    public HexCoord? OpenedFactory { get; private set; }

    /// <summary>
    /// While the AI is showing its build choice, this carries the chosen
    /// unit type id; the menu highlights it for <see cref="AiBuildPreviewMs"/>
    /// and then closes.
    /// </summary>
    public string? AiBuildPreviewTypeId { get; private set; }

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

        // Clicking one of your own factories opens its production screen
        // (browseable any time; one build per turn).
        if (TryOpenFactoryMenu(hex))
        {
            return;
        }

        HandleUnitOrEmptyClick(hex);
    }

    private void HandleUnitOrEmptyClick(HexCoord hex)
    {
        var unitAtHex = CurrentState!.GetUnitAt(hex);

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
                PendingAttack = PendingAttackInfo.Unit(selected.Id, target.Id);
                Notify();
            }
            return;
        }

        // Clicking an empty hex: building attack takes precedence over move
        // if the hex contains an attackable enemy/neutral building.
        if (unitAtHex is null)
        {
            HandleEmptyHexClick(selected, hex);
        }
    }

    private void HandleEmptyHexClick(Unit selected, HexCoord hex)
    {
        var attackableBuildings = AttackRules.GetAttackableBuildings(CurrentState!, selected);
        if (attackableBuildings.Contains(hex))
        {
            PendingAttack = PendingAttackInfo.Building(selected.Id, hex);
            Notify();
            return;
        }

        var moves = _engine.GetLegalMoves(CurrentState!, selected.Id);
        if (moves.Contains(hex))
        {
            CurrentState = _engine.MoveUnit(CurrentState!, selected.Id, hex);
            SelectedUnitId = CurrentState.Units.ContainsKey(selected.Id) ? selected.Id : null;
            Notify();
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

        if (pending.TargetUnitId is { } targetUnit)
        {
            CurrentState = _engine.AttackUnit(CurrentState, pending.AttackerId, targetUnit);
        }
        else if (pending.TargetBuildingCoord is { } buildingCoord)
        {
            CurrentState = _engine.AttackBuilding(CurrentState, pending.AttackerId, buildingCoord);
        }

        PendingAttack = null;
        SelectedUnitId = CurrentState.Units.ContainsKey(pending.AttackerId) ? pending.AttackerId : null;
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

    private bool TryOpenFactoryMenu(HexCoord hex)
    {
        if (CurrentState is null)
        {
            return false;
        }
        if (!CurrentState.Map.Tiles.TryGetValue(hex, out var tile) || tile.Building is not { } building)
        {
            return false;
        }
        if (!ProductionRules.IsProductionBuilding(building))
        {
            return false;
        }
        if (CurrentState.GetBuildingOwner(hex) != HumanSide || !CurrentState.HasIntactBuilding(hex))
        {
            return false;
        }
        // If a unit is standing on the factory hex (e.g. a freshly-built
        // unit that hasn't moved yet, or one that walked back on top),
        // the click belongs to that unit — the player must move it off
        // before the factory becomes available again.
        if (CurrentState.GetUnitAt(hex) is not null)
        {
            return false;
        }
        OpenedFactory = hex;
        SelectedUnitId = null;
        // Defensive: any lingering AI build preview state is unrelated
        // to the human opening their own factory, so wipe it.
        AiBuildPreviewTypeId = null;
        Notify();
        return true;
    }

    /// <summary>Closes the production menu without buying anything.</summary>
    public void CloseFactoryMenu()
    {
        if (OpenedFactory is not null)
        {
            OpenedFactory = null;
            Notify();
        }
    }

    /// <summary>
    /// Builds <paramref name="typeId"/> at the currently-open factory.
    /// No-op if the human has already built somewhere this turn or no menu
    /// is open. After a successful build the menu stays open so the player
    /// can see the box outline; they dismiss it with <see cref="CloseFactoryMenu"/>.
    /// </summary>
    public void BuildAtOpenedFactory(string typeId)
    {
        if (CurrentState is null || !IsHumanTurn || OpenedFactory is not { } factory)
        {
            return;
        }
        if (CurrentState.HasBuiltThisTurn(HumanSide))
        {
            return;
        }
        try
        {
            CurrentState = _engine.BuildUnit(CurrentState, factory, typeId);
        }
        catch (InvalidOperationException)
        {
            // UI is meant to gate this (occupied factory, etc.) — swallow
            // any race that slipped past so the page doesn't crash with
            // Blazor's red error bar.
            return;
        }
        // Close the menu once the build lands. The newly-built unit now
        // sits on the factory hex and the player can pick it up via a
        // normal hex click.
        OpenedFactory = null;
        Notify();
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
        // Close any production menu so it doesn't carry into the AI's turn
        // showing the wrong side's label.
        OpenedFactory = null;
        AiBuildPreviewTypeId = null;
        Notify();

        // Drive the AI's whole phase synchronously (it's fast).
        await RunAiPhaseAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Switches the open production menu to the other production building
    /// of the same kind on the human's side — used by the Land/Air tabs
    /// inside the menu so the player can browse both without going back
    /// to the map.
    /// </summary>
    public void SwitchFactoryView(BuildingKind targetKind)
    {
        if (CurrentState is null || OpenedFactory is null)
        {
            return;
        }
        foreach (var tile in CurrentState.Map.Tiles.Values)
        {
            if (tile.Building != targetKind)
            {
                continue;
            }
            if (CurrentState.GetBuildingOwner(tile.Coord) != HumanSide)
            {
                continue;
            }
            OpenedFactory = tile.Coord;
            Notify();
            return;
        }
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

            if (action.Kind == StrategyActionKind.Build)
            {
                // Pop the factory menu open with the chosen unit highlighted
                // so the player can see what the AI is buying. The try/finally
                // guarantees we tear that preview down even if BuildUnit
                // throws — otherwise the menu would stay locked on the next
                // human turn with Cancel disabled and "Red is building..."
                // stuck on screen.
                try
                {
                    OpenedFactory = action.Hex;
                    AiBuildPreviewTypeId = action.ProduceTypeId;
                    Notify();
                    await Task.Delay(AiBuildPreviewMs, cancellationToken).ConfigureAwait(false);
                    CurrentState = ApplyAction(action, s);
                }
                finally
                {
                    OpenedFactory = null;
                    AiBuildPreviewTypeId = null;
                }
            }
            else
            {
                CurrentState = ApplyAction(action, s);
            }

            Notify();

            // Pace the AI so the player can watch each move/attack/supply
            // play out one step at a time rather than all at once.
            if (action.Kind != StrategyActionKind.EndTurn && action.Kind != StrategyActionKind.Build)
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
