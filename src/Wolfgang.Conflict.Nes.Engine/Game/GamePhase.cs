namespace Wolfgang.Conflict.Nes.Engine.Game;

/// <summary>
/// Top-level phase of the game state machine.
/// </summary>
public enum GamePhase
{
    /// <summary>The current side may issue commands.</summary>
    PlayerTurn = 0,

    /// <summary>The game is over; <c>GameState.Winner</c> holds the victor.</summary>
    GameOver = 1,
}
