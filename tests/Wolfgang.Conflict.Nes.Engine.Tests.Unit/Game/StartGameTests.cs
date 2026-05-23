using Wolfgang.Conflict.Nes.Engine.Game;
using Wolfgang.Conflict.Nes.Engine.Hex;
using Wolfgang.Conflict.Nes.Engine.Map;
using Wolfgang.Conflict.Nes.Engine.Players;
using Wolfgang.Conflict.Nes.Engine.Units;
using Xunit;

namespace Wolfgang.Conflict.Nes.Engine.Tests.Game;

public class StartGameTests
{
    private static async Task<MissionDefinition> Mission01() =>
        await MissionLoader.LoadMission01Async();

    [Fact]
    public async Task Mission01_starts_with_blue_to_move_at_turn_one()
    {
        var sut = new GameEngine();

        var state = sut.StartGame(await Mission01(), randomSeed: 1234);

        Assert.Equal(Side.Blue, state.NextToAct);
        Assert.Equal(1, state.TurnNumber);
        Assert.Equal(GamePhase.PlayerTurn, state.Phase);
        Assert.Null(state.Winner);
    }

    [Fact]
    public async Task Mission01_places_equal_mixed_arms_force_per_side()
    {
        var sut = new GameEngine();

        var state = sut.StartGame(await Mission01(), 1);

        var blue = state.Units.Values.Count(u => u.Side == Side.Blue);
        var red = state.Units.Values.Count(u => u.Side == Side.Red);

        Assert.Equal(9, blue);
        Assert.Equal(9, red);
        Assert.Equal(blue, red);
    }

    [Fact]
    public async Task Each_side_has_exactly_one_commander()
    {
        var sut = new GameEngine();

        var state = sut.StartGame(await Mission01(), 1);

        Assert.Single(state.Units.Values.Where(u => u.Side == Side.Blue && u.IsCommander));
        Assert.Single(state.Units.Values.Where(u => u.Side == Side.Red && u.IsCommander));
    }

    [Fact]
    public async Task Commander_is_a_tank_for_both_sides()
    {
        var sut = new GameEngine();

        var state = sut.StartGame(await Mission01(), 1);

        var blueCmd = state.Units.Values.Single(u => u.Side == Side.Blue && u.IsCommander);
        var redCmd = state.Units.Values.Single(u => u.Side == Side.Red && u.IsCommander);

        Assert.Equal(UnitCategory.BattleTank, blueCmd.Category);
        Assert.Equal(UnitCategory.BattleTank, redCmd.Category);
        Assert.Equal("m1a1", blueCmd.Type.Id);
        Assert.Equal("t80", redCmd.Type.Id);
    }

    [Fact]
    public async Task All_units_start_at_full_LIFE()
    {
        var sut = new GameEngine();

        var state = sut.StartGame(await Mission01(), 1);

        Assert.All(state.Units.Values, u => Assert.Equal(UnitStats.MaxHitPoints, u.HitPoints));
    }

    [Fact]
    public async Task Each_unit_id_is_unique()
    {
        var sut = new GameEngine();

        var state = sut.StartGame(await Mission01(), 1);

        var ids = state.Units.Values.Select(u => u.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public async Task Both_sides_start_with_zero_funds()
    {
        var sut = new GameEngine();

        var state = sut.StartGame(await Mission01(), 1);

        Assert.Equal(0, state.Funds[Side.Blue]);
        Assert.Equal(0, state.Funds[Side.Red]);
    }

    [Fact]
    public async Task Building_ownership_falls_back_to_map_for_unmodified_buildings()
    {
        var mission = await Mission01();
        var sut = new GameEngine();

        var state = sut.StartGame(mission, 1);

        var blueFactory = mission.Map.Tiles.Values.Single(t => t.Building == BuildingKind.Factory && t.Owner == Side.Blue);
        Assert.Equal(Side.Blue, state.GetBuildingOwner(blueFactory.Coord));

        var redFactory = mission.Map.Tiles.Values.Single(t => t.Building == BuildingKind.Factory && t.Owner == Side.Red);
        Assert.Equal(Side.Red, state.GetBuildingOwner(redFactory.Coord));
    }

    [Fact]
    public async Task GetUnitAt_returns_unit_on_hex_or_null()
    {
        var mission = await Mission01();
        var sut = new GameEngine();

        var state = sut.StartGame(mission, 1);

        var blueCommander = state.Units.Values.Single(u => u.Side == Side.Blue && u.IsCommander);
        Assert.Same(blueCommander, state.GetUnitAt(blueCommander.Coord));
        Assert.Null(state.GetUnitAt(new HexCoord(5, 0)));
    }

    [Fact]
    public void StartGame_null_mission_throws()
    {
        var sut = new GameEngine();
        Assert.Throws<ArgumentNullException>(() => sut.StartGame(mission: null!, 1));
    }

    [Fact]
    public void StartGame_with_duplicate_starting_hex_throws()
    {
        var sut = new GameEngine();
        var map = new MapDefinition("two", 2, 1,
            MapDefinition.EnumerateCoords(2, 1)
                .Select(c => new Tile(c, Terrain.Plains, Building: null, Owner: null)));
        var placements = new List<UnitPlacement>
        {
            new(Side.Blue, TestCatalog.Tank.Id, new HexCoord(0, 0), IsCommander: true),
            new(Side.Red,  TestCatalog.Tank.Id, new HexCoord(0, 0), IsCommander: true),
        };
        var mission = new MissionDefinition(map, TestCatalog.Catalog, placements);

        Assert.Throws<ArgumentException>(() => sut.StartGame(mission, 1));
    }

    [Fact]
    public void StartGame_with_no_commander_for_a_side_throws()
    {
        var sut = new GameEngine();
        var map = new MapDefinition("two", 2, 1,
            MapDefinition.EnumerateCoords(2, 1)
                .Select(c => new Tile(c, Terrain.Plains, Building: null, Owner: null)));
        var placements = new List<UnitPlacement>
        {
            new(Side.Blue, TestCatalog.Tank.Id, new HexCoord(0, 0), IsCommander: true),
            new(Side.Red,  TestCatalog.Tank.Id, new HexCoord(1, 0), IsCommander: false),
        };
        var mission = new MissionDefinition(map, TestCatalog.Catalog, placements);

        Assert.Throws<ArgumentException>(() => sut.StartGame(mission, 1));
    }
}
