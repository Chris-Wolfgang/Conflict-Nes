using Wolfgang.Conflict.Nes.Engine.Game;
using Wolfgang.Conflict.Nes.Engine.Players;

namespace Wolfgang.Conflict.Nes.Engine.Rules;

/// <summary>
/// Pure rules for determining game termination. Two paths to victory:
/// (1) destroy the enemy commander unit (manual: "Victory is achieved with
/// the defeat of the enemy commander's unit") or (2) rout the enemy (no
/// units left at all).
/// </summary>
public static class VictoryRules
{
    /// <summary>
    /// Returns the winning <see cref="Side"/> if the game has ended, or
    /// <see langword="null"/> if it is still in progress.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="state"/> is null.</exception>
    public static Side? DetermineWinner(GameState state)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        var blueAlive = false;
        var redAlive = false;
        var blueCommanderAlive = false;
        var redCommanderAlive = false;

        foreach (var unit in state.Units.Values)
        {
            if (unit.Side == Side.Blue)
            {
                blueAlive = true;
                if (unit.IsCommander)
                {
                    blueCommanderAlive = true;
                }
            }
            else
            {
                redAlive = true;
                if (unit.IsCommander)
                {
                    redCommanderAlive = true;
                }
            }
        }

        if (!blueCommanderAlive || !blueAlive)
        {
            return Side.Red;
        }
        if (!redCommanderAlive || !redAlive)
        {
            return Side.Blue;
        }
        return null;
    }
}
