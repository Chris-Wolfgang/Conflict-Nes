using Wolfgang.Conflict.Nes.Engine.Combat;
using Wolfgang.Conflict.Nes.Engine.Hex;
using Wolfgang.Conflict.Nes.Engine.Map;
using Wolfgang.Conflict.Nes.Engine.Players;
using Wolfgang.Conflict.Nes.Engine.Rules;
using Wolfgang.Conflict.Nes.Engine.Units;

namespace Wolfgang.Conflict.Nes.Engine.Game;

/// <summary>
/// Stateless orchestrator for the Conflict game. Each public method takes a
/// <see cref="GameState"/> (and parameters) and returns a new <see cref="GameState"/>.
/// </summary>
public sealed class GameEngine
{
    /// <summary>
    /// Creates the initial <see cref="GameState"/> for a mission, placing each
    /// side's starting units. Blue moves first.
    /// </summary>
    /// <param name="mission">The mission to start.</param>
    /// <param name="randomSeed">Seed for the combat RNG; preserved across states.</param>
    /// <returns>The starting state with <c>NextToAct = Blue</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="mission"/> is null.</exception>
    /// <exception cref="ArgumentException">A unit placement is invalid (off-map, duplicate hex, unknown type, or duplicate commander for a side).</exception>
    public GameState StartGame(MissionDefinition mission, int randomSeed)
    {
        if (mission is null)
        {
            throw new ArgumentNullException(nameof(mission));
        }

        var units = BuildStartingUnits(mission);

        var funds = new Dictionary<Side, int>
        {
            [Side.Blue] = 0,
            [Side.Red]  = 0,
        };

        return new GameState(
            map: mission.Map,
            catalog: mission.Catalog,
            units: units,
            buildingOwners: new Dictionary<HexCoord, Side>(),
            nextToAct: Side.Blue,
            turnNumber: 1,
            phase: GamePhase.PlayerTurn,
            funds: funds,
            winner: null,
            randomSeed: randomSeed);
    }

    private static Dictionary<UnitId, Unit> BuildStartingUnits(MissionDefinition mission)
    {
        var units = new Dictionary<UnitId, Unit>();
        var occupied = new HashSet<HexCoord>();
        var commanderCount = new Dictionary<Side, int> { [Side.Blue] = 0, [Side.Red] = 0 };
        var nextId = 1;

        foreach (var placement in mission.StartingUnits)
        {
            if (!mission.Map.Contains(placement.Coord))
            {
                throw new ArgumentException($"Placement {placement.Coord} is outside the map.", nameof(mission));
            }
            if (!occupied.Add(placement.Coord))
            {
                throw new ArgumentException($"Duplicate starting hex {placement.Coord}.", nameof(mission));
            }
            if (!mission.Catalog.Contains(placement.TypeId))
            {
                throw new ArgumentException($"Unknown unit type '{placement.TypeId}'.", nameof(mission));
            }

            if (placement.IsCommander)
            {
                commanderCount[placement.Side]++;
            }

            var type = mission.Catalog.Get(placement.TypeId);
            var id = new UnitId(nextId++);
            units[id] = Unit.FullStrength(id, placement.Side, type, placement.Coord, placement.IsCommander);
        }

        foreach (var side in new[] { Side.Blue, Side.Red })
        {
            if (commanderCount[side] != 1)
            {
                throw new ArgumentException($"Side {side} must have exactly one commander; got {commanderCount[side]}.", nameof(mission));
            }
        }

        return units;
    }

    /// <summary>
    /// Returns every hex the named unit can legally move to this turn.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="state"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The unit does not exist.</exception>
    public IReadOnlyList<HexCoord> GetLegalMoves(GameState state, UnitId id)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        if (!state.Units.TryGetValue(id, out var unit))
        {
            throw new InvalidOperationException($"Unit {id} does not exist.");
        }

        return MovementRules.GetReachable(state, unit);
    }

    /// <summary>
    /// Moves the named unit along the cheapest legal path to
    /// <paramref name="destination"/>, deducting movement points.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="state"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// The game is over, it is not the unit's owner's turn, the unit doesn't
    /// exist, or the destination is not reachable.
    /// </exception>
    public GameState MoveUnit(GameState state, UnitId id, HexCoord destination)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        if (state.Phase != GamePhase.PlayerTurn)
        {
            throw new InvalidOperationException("Game is over; no more moves accepted.");
        }

        if (!state.Units.TryGetValue(id, out var unit))
        {
            throw new InvalidOperationException($"Unit {id} does not exist.");
        }

        if (unit.Side != state.NextToAct)
        {
            throw new InvalidOperationException($"It is not {unit.Side}'s turn.");
        }

        if (unit.HasMoved)
        {
            throw new InvalidOperationException($"Unit {id} has already moved this turn.");
        }

        if (destination == unit.Coord)
        {
            throw new InvalidOperationException("Destination is the unit's current hex.");
        }

        var path = MovementRules.FindPath(state, unit, destination)
            ?? throw new InvalidOperationException($"Unit {id} cannot reach {destination}.");

        var moved = unit with
        {
            Coord = destination,
            MovesRemaining = unit.MovesRemaining - path.TotalCost,
            HasMoved = true,
        };

        var newUnits = CopyUnits(state.Units);
        newUnits[id] = moved;

        return WithUnits(state, newUnits);
    }

    /// <summary>
    /// Returns every enemy unit the named attacker can engage this turn.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="state"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The unit does not exist.</exception>
    public IReadOnlyList<UnitId> GetLegalTargets(GameState state, UnitId attackerId)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }
        if (!state.Units.TryGetValue(attackerId, out var attacker))
        {
            throw new InvalidOperationException($"Unit {attackerId} does not exist.");
        }
        return AttackRules.GetLegalTargets(state, attacker);
    }

    /// <summary>
    /// Resolves an AUTO-mode attack. Applies damage to both sides, prunes
    /// destroyed units, awards F.P. per the manual's economy rules, and
    /// advances the combat RNG seed so subsequent attacks differ.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="state"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The attack is illegal under <see cref="AttackRules"/>.</exception>
    public GameState AttackUnit(GameState state, UnitId attackerId, UnitId defenderId)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }
        if (state.Phase != GamePhase.PlayerTurn)
        {
            throw new InvalidOperationException("Game is over; no more attacks accepted.");
        }
        if (!state.Units.TryGetValue(attackerId, out var attacker))
        {
            throw new InvalidOperationException($"Attacker {attackerId} does not exist.");
        }
        if (!state.Units.TryGetValue(defenderId, out var defender))
        {
            throw new InvalidOperationException($"Defender {defenderId} does not exist.");
        }
        if (attacker.Side != state.NextToAct)
        {
            throw new InvalidOperationException($"It is not {attacker.Side}'s turn.");
        }
        if (!AttackRules.CanAttack(attacker, defender))
        {
            throw new InvalidOperationException($"{attackerId} cannot attack {defenderId}.");
        }

        var rng = new SeededRandomSource(state.RandomSeed);
        var result = CombatResolver.Resolve(state, attacker, defender, rng);

        var updatedUnits = ApplyDamage(state.Units, attacker, defender, result);
        var updatedFunds = ApplyEconomyAwards(
            state.Funds, attacker.Side, defender.Side,
            attacker.Type.ProductionCost, defender.Type.ProductionCost, result);

        var after = WithUnitsAndFunds(state, updatedUnits, updatedFunds, advanceSeed: true);
        return CheckVictory(after);
    }

    /// <summary>
    /// Attacks the building at <paramref name="buildingCoord"/> with the
    /// named unit. Buildings are stationary targets: damage is applied
    /// directly to the building's HP and there is no counter-attack. A
    /// building reduced to zero HP is considered destroyed.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="state"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The attack is illegal.</exception>
    public GameState AttackBuilding(GameState state, UnitId attackerId, HexCoord buildingCoord)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }
        if (state.Phase != GamePhase.PlayerTurn)
        {
            throw new InvalidOperationException("Game is over; no more attacks accepted.");
        }
        if (!state.Units.TryGetValue(attackerId, out var attacker))
        {
            throw new InvalidOperationException($"Attacker {attackerId} does not exist.");
        }
        if (attacker.Side != state.NextToAct)
        {
            throw new InvalidOperationException($"It is not {attacker.Side}'s turn.");
        }
        if (!AttackRules.CanAttackBuilding(state, attacker, buildingCoord))
        {
            throw new InvalidOperationException($"{attackerId} cannot attack the building at {buildingCoord}.");
        }

        var rng = new SeededRandomSource(state.RandomSeed);
        var damage = ComputeBuildingDamage(attacker, rng);
        var currentHp = state.GetBuildingHitPoints(buildingCoord);
        var newHp = Math.Max(0, currentHp - damage);
        var newHits = UpdateBuildingHits(state.BuildingHitPoints, buildingCoord, newHp);

        var newUnits = CopyUnits(state.Units);
        newUnits[attackerId] = attacker with
        {
            Ammo = Math.Max(0, attacker.Ammo - 1),
            HasAttacked = true,
        };

        var newFunds = (newHp == 0 && currentHp > 0)
            ? AwardBuildingBounty(state.Funds, attacker.Side)
            : state.Funds;

        return new GameState(
            map: state.Map,
            catalog: state.Catalog,
            units: newUnits,
            buildingOwners: state.BuildingOwners,
            nextToAct: state.NextToAct,
            turnNumber: state.TurnNumber,
            phase: state.Phase,
            funds: newFunds,
            winner: state.Winner,
            randomSeed: unchecked(state.RandomSeed * 1103515245 + 12345),
            buildingHitPoints: newHits);
    }

    // Factories self-repair between attacks: a non-fatal strike heals back
    // to full HP before the next swing (even on the same turn). Only a
    // one-shot kill (newHp == 0) actually persists damage.
    private static Dictionary<HexCoord, int> UpdateBuildingHits(
        IReadOnlyDictionary<HexCoord, int> source,
        HexCoord coord,
        int newHp)
    {
        var copy = CopyBuildingHits(source);
        if (newHp == 0)
        {
            copy[coord] = 0;
        }
        else
        {
            copy.Remove(coord);
        }
        return copy;
    }

    private static Dictionary<HexCoord, int> CopyBuildingHits(IReadOnlyDictionary<HexCoord, int> source)
    {
        var copy = new Dictionary<HexCoord, int>(source.Count);
        foreach (var kv in source)
        {
            copy[kv.Key] = kv.Value;
        }
        return copy;
    }

    private static Dictionary<Side, int> AwardBuildingBounty(IReadOnlyDictionary<Side, int> source, Side beneficiary)
    {
        var funds = CopyFunds(source);
        funds[beneficiary] += 500;
        return funds;
    }

    private static int ComputeBuildingDamage(Unit attacker, IRandomSource rng)
    {
        // Treat buildings as soft stationary ground targets — the attacker's
        // power vs a SupplyVehicle-class defender is a reasonable proxy.
        var baseAttack = RelationsTable.BaseAttack(attacker.Category, UnitCategory.SupplyVehicle);
        if (baseAttack <= 0)
        {
            return 0;
        }
        var scaled = (baseAttack * attacker.HitPoints) / UnitStats.MaxHitPoints;
        var roll = rng.NextInt(-2, 2);
        return Math.Max(0, scaled + roll);
    }

    /// <summary>
    /// Produces a new unit of the catalog type <paramref name="typeId"/> on the
    /// building at <paramref name="buildingCoord"/>. Deducts the F.P. cost.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="state"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The build is illegal.</exception>
    public GameState BuildUnit(GameState state, HexCoord buildingCoord, string typeId)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }
        if (state.Phase != GamePhase.PlayerTurn)
        {
            throw new InvalidOperationException("Game is over; no more commands accepted.");
        }

        var side = state.NextToAct;
        var type = ProductionRules.ValidateBuild(state, side, buildingCoord, typeId);

        var newFunds = CopyFunds(state.Funds);
        newFunds[side] -= ProductionRules.EffectiveBuildCost(type);

        var newId = NextUnitId(state);
        // Newly produced units have already "moved" this turn (manual: they
        // cannot move or attack on the turn they were built).
        var freshUnit = Unit.FullStrength(newId, side, type, buildingCoord) with
        {
            MovesRemaining = 0,
            HasMoved = true,
            HasAttacked = true,
        };
        var newUnits = CopyUnits(state.Units);
        newUnits[newId] = freshUnit;

        return new GameState(
            map: state.Map,
            catalog: state.Catalog,
            units: newUnits,
            buildingOwners: state.BuildingOwners,
            nextToAct: state.NextToAct,
            turnNumber: state.TurnNumber,
            phase: state.Phase,
            funds: newFunds,
            winner: state.Winner,
            buildingHitPoints: state.BuildingHitPoints,
            randomSeed: state.RandomSeed);
    }

    /// <summary>
    /// Ends the current side's turn. Applies infantry-captures of enemy
    /// cities/airbases/ports, transfers control to the other side, refreshes
    /// per-turn fields on the new side's units, accrues +100 income per
    /// owned City and Airbase, and checks for victory.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="state"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The game has already ended.</exception>
    public GameState EndTurn(GameState state)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }
        if (state.Phase != GamePhase.PlayerTurn)
        {
            throw new InvalidOperationException("Game is over; no more commands accepted.");
        }

        var endingSide = state.NextToAct;
        var nextSide = endingSide == Side.Blue ? Side.Red : Side.Blue;

        // 1. Capture buildings on which the ending side has infantry.
        var flips = CaptureRules.ComputeFlips(state, endingSide);
        var newOwners = MergeOwners(state.BuildingOwners, flips);

        // 2. Drain one fuel from every unit on the ending side that moved.
        var fuelDrained = DrainFuelForMovers(state.Units, endingSide);

        // 3. Refresh the next side's per-turn fields.
        var refreshedUnits = RefreshUnitsForTurn(fuelDrained, nextSide);

        // 4. Accrue income for the next side from its City and Airbase holdings.
        var newFunds = CopyFunds(state.Funds);
        newFunds[nextSide] += ComputeIncome(state.Map, newOwners, nextSide, state.BuildingHitPoints);

        // Turn number increments when Blue is about to act again (a full round).
        var newTurnNumber = nextSide == Side.Blue ? state.TurnNumber + 1 : state.TurnNumber;

        var next = new GameState(
            map: state.Map,
            catalog: state.Catalog,
            units: refreshedUnits,
            buildingOwners: newOwners,
            nextToAct: nextSide,
            turnNumber: newTurnNumber,
            phase: state.Phase,
            funds: newFunds,
            winner: state.Winner,
            randomSeed: state.RandomSeed,
            buildingHitPoints: state.BuildingHitPoints);

        return CheckVictory(next);
    }

    /// <summary>
    /// Supplies the named unit if it is currently eligible. Refuels, refills
    /// ammo, and (for buildings) repairs LIFE. Once per turn.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="state"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// The game is over, it is not the unit's owner's turn, the unit doesn't
    /// exist, or no supply source is available.
    /// </exception>
    public GameState SupplyUnit(GameState state, UnitId id)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }
        if (state.Phase != GamePhase.PlayerTurn)
        {
            throw new InvalidOperationException("Game is over; no more commands accepted.");
        }
        if (!state.Units.TryGetValue(id, out var unit))
        {
            throw new InvalidOperationException($"Unit {id} does not exist.");
        }
        if (unit.Side != state.NextToAct)
        {
            throw new InvalidOperationException($"It is not {unit.Side}'s turn.");
        }

        var source = SupplyRules.AvailableSource(state, unit)
            ?? throw new InvalidOperationException($"Unit {id} has no supply source available.");

        var supplied = SupplyRules.ApplySupply(unit, source);
        var newUnits = CopyUnits(state.Units);
        newUnits[id] = supplied;

        return WithUnits(state, newUnits);
    }

    private static int ComputeIncome(
        MapDefinition map,
        IReadOnlyDictionary<HexCoord, Side> owners,
        Side side,
        IReadOnlyDictionary<HexCoord, int> buildingHitPoints)
    {
        var income = 0;
        foreach (var tile in map.Tiles.Values)
        {
            if (tile.Building is not { } building)
            {
                continue;
            }
            var currentOwner = owners.TryGetValue(tile.Coord, out var o) ? o : tile.Owner;
            if (currentOwner != side)
            {
                continue;
            }
            // Destroyed buildings produce no income.
            var hp = buildingHitPoints.TryGetValue(tile.Coord, out var h) ? h : GameState.MaxBuildingHitPoints;
            if (hp <= 0)
            {
                continue;
            }
            income += EconomyRules.BuildingIncomePerTurn(building);
        }
        return income;
    }

    private static GameState CheckVictory(GameState state)
    {
        var winner = VictoryRules.DetermineWinner(state);
        if (winner is null)
        {
            return state;
        }
        return new GameState(
            map: state.Map,
            catalog: state.Catalog,
            units: state.Units,
            buildingOwners: state.BuildingOwners,
            nextToAct: state.NextToAct,
            turnNumber: state.TurnNumber,
            phase: GamePhase.GameOver,
            funds: state.Funds,
            winner: winner,
            buildingHitPoints: state.BuildingHitPoints,
            randomSeed: state.RandomSeed);
    }

    private static UnitId NextUnitId(GameState state)
    {
        var max = 0;
        foreach (var id in state.Units.Keys)
        {
            if (id.Value > max)
            {
                max = id.Value;
            }
        }
        return new UnitId(max + 1);
    }

    private static Dictionary<HexCoord, Side> MergeOwners(
        IReadOnlyDictionary<HexCoord, Side> existing,
        IReadOnlyDictionary<HexCoord, Side> additions)
    {
        var merged = new Dictionary<HexCoord, Side>(existing.Count + additions.Count);
        foreach (var kv in existing)
        {
            merged[kv.Key] = kv.Value;
        }
        foreach (var kv in additions)
        {
            merged[kv.Key] = kv.Value;
        }
        return merged;
    }

    private static Dictionary<UnitId, Unit> DrainFuelForMovers(IReadOnlyDictionary<UnitId, Unit> units, Side endingSide)
    {
        var drained = new Dictionary<UnitId, Unit>(units.Count);
        foreach (var kv in units)
        {
            var u = kv.Value;
            if (u.Side == endingSide && u.HasMoved && u.Fuel > 0)
            {
                drained[kv.Key] = u with { Fuel = u.Fuel - 1 };
            }
            else
            {
                drained[kv.Key] = u;
            }
        }
        return drained;
    }

    private static Dictionary<UnitId, Unit> RefreshUnitsForTurn(IReadOnlyDictionary<UnitId, Unit> units, Side sideStartingTurn)
    {
        var refreshed = new Dictionary<UnitId, Unit>(units.Count);
        foreach (var kv in units)
        {
            var u = kv.Value;
            if (u.Side == sideStartingTurn)
            {
                refreshed[kv.Key] = u with
                {
                    MovesRemaining = u.Type.MovementPoints,
                    HasMoved = false,
                    HasAttacked = false,
                    HasSupplied = false,
                };
            }
            else
            {
                refreshed[kv.Key] = u;
            }
        }
        return refreshed;
    }

    private static Dictionary<Side, int> CopyFunds(IReadOnlyDictionary<Side, int> source)
    {
        var copy = new Dictionary<Side, int>(source.Count);
        foreach (var kv in source)
        {
            copy[kv.Key] = kv.Value;
        }
        return copy;
    }

    private static Dictionary<UnitId, Unit> ApplyDamage(
        IReadOnlyDictionary<UnitId, Unit> source,
        Unit attacker,
        Unit defender,
        CombatResult result)
    {
        var newUnits = CopyUnits(source);

        if (result.DefenderDestroyed)
        {
            newUnits.Remove(defender.Id);
        }
        else
        {
            newUnits[defender.Id] = defender with { HitPoints = defender.HitPoints - result.DamageToDefender };
        }

        var attackerHpAfter = attacker.HitPoints - result.DamageToAttacker;
        if (result.AttackerDestroyed)
        {
            newUnits.Remove(attacker.Id);
        }
        else
        {
            newUnits[attacker.Id] = attacker with
            {
                HitPoints = attackerHpAfter,
                Ammo = Math.Max(0, attacker.Ammo - 1),
                HasAttacked = true,
            };
        }

        return newUnits;
    }

    private static Dictionary<Side, int> ApplyEconomyAwards(
        IReadOnlyDictionary<Side, int> source,
        Side attackerSide,
        Side defenderSide,
        int attackerProductionCost,
        int defenderProductionCost,
        CombatResult result)
    {
        var funds = new Dictionary<Side, int>(source.Count);
        foreach (var kv in source)
        {
            funds[kv.Key] = kv.Value;
        }

        if (result.DefenderDestroyed)
        {
            funds[attackerSide] += EconomyRules.WinnerReward(result.AttackerOutlook);
            funds[defenderSide] -= EconomyRules.LoserPenalty(defenderProductionCost);
        }
        if (result.AttackerDestroyed)
        {
            funds[defenderSide] += EconomyRules.WinnerReward(result.DefenderOutlook);
            funds[attackerSide] -= EconomyRules.LoserPenalty(attackerProductionCost);
        }

        // F.P. is floored at zero — a side never carries negative funds.
        funds[Side.Blue] = Math.Max(0, funds[Side.Blue]);
        funds[Side.Red] = Math.Max(0, funds[Side.Red]);

        return funds;
    }

    private static Dictionary<UnitId, Unit> CopyUnits(IReadOnlyDictionary<UnitId, Unit> source)
    {
        var copy = new Dictionary<UnitId, Unit>(source.Count);
        foreach (var kv in source)
        {
            copy[kv.Key] = kv.Value;
        }
        return copy;
    }

    private static GameState WithUnits(GameState state, IReadOnlyDictionary<UnitId, Unit> newUnits) =>
        new(
            map: state.Map,
            catalog: state.Catalog,
            units: newUnits,
            buildingOwners: state.BuildingOwners,
            nextToAct: state.NextToAct,
            turnNumber: state.TurnNumber,
            phase: state.Phase,
            funds: state.Funds,
            winner: state.Winner,
            randomSeed: state.RandomSeed,
            buildingHitPoints: state.BuildingHitPoints);

    private static GameState WithUnitsAndFunds(
        GameState state,
        IReadOnlyDictionary<UnitId, Unit> newUnits,
        IReadOnlyDictionary<Side, int> newFunds,
        bool advanceSeed) =>
        new(
            map: state.Map,
            catalog: state.Catalog,
            units: newUnits,
            buildingOwners: state.BuildingOwners,
            nextToAct: state.NextToAct,
            turnNumber: state.TurnNumber,
            phase: state.Phase,
            funds: newFunds,
            winner: state.Winner,
            randomSeed: advanceSeed ? unchecked(state.RandomSeed * 1103515245 + 12345) : state.RandomSeed,
            buildingHitPoints: state.BuildingHitPoints);
}
