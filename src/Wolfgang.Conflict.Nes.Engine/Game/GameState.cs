using Wolfgang.Conflict.Nes.Engine.Hex;
using Wolfgang.Conflict.Nes.Engine.Map;
using Wolfgang.Conflict.Nes.Engine.Players;
using Wolfgang.Conflict.Nes.Engine.Units;

namespace Wolfgang.Conflict.Nes.Engine.Game;

/// <summary>
/// Immutable snapshot of the game at a single moment. Engine commands
/// (<c>MoveUnit</c>, <c>AttackUnit</c>, <c>EndTurn</c>, &#x2026;) consume one
/// <see cref="GameState"/> and produce another.
/// </summary>
public sealed class GameState
{
    private readonly IReadOnlyDictionary<UnitId, Unit> _units;
    private readonly IReadOnlyDictionary<HexCoord, Side> _buildingOwners;
    private readonly IReadOnlyDictionary<Side, int> _funds;

    /// <summary>The map for the current mission.</summary>
    public MapDefinition Map { get; }

    /// <summary>All living units, indexed by id.</summary>
    public IReadOnlyDictionary<UnitId, Unit> Units => _units;

    /// <summary>
    /// Building ownership overrides relative to the starting map. Only
    /// captured buildings appear here; absent buildings retain their
    /// <see cref="MapDefinition"/> initial owner.
    /// </summary>
    public IReadOnlyDictionary<HexCoord, Side> BuildingOwners => _buildingOwners;

    /// <summary>The side whose turn it currently is.</summary>
    public Side NextToAct { get; }

    /// <summary>The current turn number (starts at 1).</summary>
    public int TurnNumber { get; }

    /// <summary>The phase of the game state machine.</summary>
    public GamePhase Phase { get; }

    /// <summary>F.P. (Fame Points) held by each side.</summary>
    public IReadOnlyDictionary<Side, int> Funds => _funds;

    /// <summary>Winning side once <see cref="Phase"/> is <see cref="GamePhase.GameOver"/>; otherwise <see langword="null"/>.</summary>
    public Side? Winner { get; }

    /// <summary>Seed for the combat RNG; preserved across states so combat is reproducible.</summary>
    public int RandomSeed { get; }

    /// <summary>Constructs a state snapshot. Most callers should go through <c>GameEngine.StartGame</c> instead.</summary>
    public GameState(
        MapDefinition map,
        IReadOnlyDictionary<UnitId, Unit> units,
        IReadOnlyDictionary<HexCoord, Side> buildingOwners,
        Side nextToAct,
        int turnNumber,
        GamePhase phase,
        IReadOnlyDictionary<Side, int> funds,
        Side? winner,
        int randomSeed)
    {
        Map = map ?? throw new ArgumentNullException(nameof(map));
        _units = units ?? throw new ArgumentNullException(nameof(units));
        _buildingOwners = buildingOwners ?? throw new ArgumentNullException(nameof(buildingOwners));
        _funds = funds ?? throw new ArgumentNullException(nameof(funds));
        NextToAct = nextToAct;
        TurnNumber = turnNumber;
        Phase = phase;
        Winner = winner;
        RandomSeed = randomSeed;
    }

    /// <summary>
    /// Returns the side that currently owns the building at <paramref name="coord"/>,
    /// or <see langword="null"/> if the hex has no building or the building is neutral.
    /// </summary>
    public Side? GetBuildingOwner(HexCoord coord)
    {
        if (_buildingOwners.TryGetValue(coord, out var owner))
        {
            return owner;
        }

        return Map.Tiles.TryGetValue(coord, out var tile) ? tile.Owner : null;
    }

    /// <summary>
    /// Returns the unit standing on <paramref name="coord"/>, or
    /// <see langword="null"/> if none.
    /// </summary>
    public Unit? GetUnitAt(HexCoord coord)
    {
        foreach (var unit in _units.Values)
        {
            if (unit.Coord == coord)
            {
                return unit;
            }
        }

        return null;
    }
}
