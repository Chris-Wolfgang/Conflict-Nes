using Wolfgang.Conflict.Nes.Engine.Game;

namespace Wolfgang.Conflict.Nes.UI.Blazor.Services;

/// <summary>
/// Holds the current <see cref="GameState"/> for the Blazor UI and exposes a
/// <see cref="StateChanged"/> event so view models can refresh.
/// </summary>
/// <remarks>
/// Mirrors the role of Hawsey's <c>GameService</c>: pure C# wrapper around
/// <see cref="GameEngine"/> that holds turn-by-turn state and surfaces it to
/// the presentation layer.
/// </remarks>
public sealed class ConflictService
{
    private readonly GameEngine _engine = new();
    private GameState? _state;

    /// <summary>Fired whenever the held <see cref="CurrentState"/> changes.</summary>
    public event EventHandler? StateChanged;

    /// <summary>The current game state, or <see langword="null"/> if no mission has been started.</summary>
    public GameState? CurrentState => _state;

    /// <summary>True if a mission has been started.</summary>
    public bool IsStarted => _state is not null;

    /// <summary>Starts a new Mission 01 game with a fresh RNG seed.</summary>
    public async Task StartMission01Async(int? seed = null, CancellationToken cancellationToken = default)
    {
        var mission = await MissionLoader.LoadMission01Async(cancellationToken).ConfigureAwait(false);
        var rngSeed = seed ?? unchecked((int)DateTime.UtcNow.Ticks);
        _state = _engine.StartGame(mission, rngSeed);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
