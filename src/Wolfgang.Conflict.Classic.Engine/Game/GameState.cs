using Wolfgang.Conflict.Classic.Engine.Hex;
using Wolfgang.Conflict.Classic.Engine.Map;
using Wolfgang.Conflict.Classic.Engine.Players;
using Wolfgang.Conflict.Classic.Engine.Units;

namespace Wolfgang.Conflict.Classic.Engine.Game;

/// <summary>
/// Immutable snapshot of the game at a single moment. Engine commands
/// (<c>MoveUnit</c>, <c>AttackUnit</c>, <c>EndTurn</c>, &#x2026;) consume one
/// <see cref="GameState"/> and produce another.
/// </summary>
public sealed class GameState
{
    /// <summary>Maximum hit points for a building (Factory, Airbase, City, ...).</summary>
    public const int MaxBuildingHitPoints = 15;

    private readonly IReadOnlyDictionary<UnitId, Unit> _units;
    private readonly IReadOnlyDictionary<HexCoord, Side> _buildingOwners;
    private readonly IReadOnlyDictionary<Side, int> _funds;
    private readonly IReadOnlyDictionary<HexCoord, int> _buildingHitPoints;
    private readonly IReadOnlyDictionary<Side, HexCoord> _buildThisTurn;
    private readonly IReadOnlyDictionary<Side, PendingProductionOrder> _pendingProduction;

    /// <summary>The map for the current mission.</summary>
    public MapDefinition Map { get; }

    /// <summary>The data-driven unit catalog in effect for this game.</summary>
    public UnitCatalog Catalog { get; }

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

    /// <summary>
    /// Sparse map of building HP overrides. Hexes not in this map use
    /// <see cref="MaxBuildingHitPoints"/>. HP &lt;= 0 means the building has
    /// been destroyed.
    /// </summary>
    public IReadOnlyDictionary<HexCoord, int> BuildingHitPoints => _buildingHitPoints;

    /// <summary>
    /// Per-side hex at which the side has already produced a unit this turn.
    /// A side may build at most one unit per turn across all of its
    /// factories; absence from this map means the side has not built yet.
    /// </summary>
    public IReadOnlyDictionary<Side, HexCoord> BuildThisTurn => _buildThisTurn;

    /// <summary>
    /// Per-side production orders that have been queued but not yet
    /// materialised on the map. A unit ordered this turn appears on the
    /// factory hex at the start of the ordering side's NEXT turn.
    /// </summary>
    public IReadOnlyDictionary<Side, PendingProductionOrder> PendingProduction => _pendingProduction;

    /// <summary>Constructs a state snapshot. Most callers should go through <c>GameEngine.StartGame</c> instead.</summary>
    public GameState(
        MapDefinition map,
        UnitCatalog catalog,
        IReadOnlyDictionary<UnitId, Unit> units,
        IReadOnlyDictionary<HexCoord, Side> buildingOwners,
        Side nextToAct,
        int turnNumber,
        GamePhase phase,
        IReadOnlyDictionary<Side, int> funds,
        Side? winner,
        int randomSeed,
        IReadOnlyDictionary<HexCoord, int>? buildingHitPoints = null,
        IReadOnlyDictionary<Side, HexCoord>? buildThisTurn = null,
        IReadOnlyDictionary<Side, PendingProductionOrder>? pendingProduction = null)
    {
        Map = map ?? throw new ArgumentNullException(nameof(map));
        Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _units = units ?? throw new ArgumentNullException(nameof(units));
        _buildingOwners = buildingOwners ?? throw new ArgumentNullException(nameof(buildingOwners));
        _funds = funds ?? throw new ArgumentNullException(nameof(funds));
        _buildingHitPoints = buildingHitPoints ?? new Dictionary<HexCoord, int>();
        _buildThisTurn = buildThisTurn ?? new Dictionary<Side, HexCoord>();
        _pendingProduction = pendingProduction ?? new Dictionary<Side, PendingProductionOrder>();
        NextToAct = nextToAct;
        TurnNumber = turnNumber;
        Phase = phase;
        Winner = winner;
        RandomSeed = randomSeed;
    }

    /// <summary>True if <paramref name="side"/> has already produced a unit this turn.</summary>
    public bool HasBuiltThisTurn(Side side) => _buildThisTurn.ContainsKey(side);

    /// <summary>The hex at which <paramref name="side"/> built this turn, or <see langword="null"/> if it hasn't.</summary>
    public HexCoord? GetBuildHexThisTurn(Side side)
        => _buildThisTurn.TryGetValue(side, out var hex) ? hex : null;

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

    /// <summary>Current HP of the building at <paramref name="coord"/> (or max if untouched).</summary>
    public int GetBuildingHitPoints(HexCoord coord)
        => _buildingHitPoints.TryGetValue(coord, out var hp) ? hp : MaxBuildingHitPoints;

    /// <summary>True if the hex has a building that hasn't been destroyed.</summary>
    public bool HasIntactBuilding(HexCoord coord)
    {
        if (!Map.Tiles.TryGetValue(coord, out var tile) || tile.Building is null)
        {
            return false;
        }
        return GetBuildingHitPoints(coord) > 0;
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
