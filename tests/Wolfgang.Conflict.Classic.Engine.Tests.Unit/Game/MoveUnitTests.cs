using Wolfgang.Conflict.Classic.Engine.Game;
using Wolfgang.Conflict.Classic.Engine.Hex;
using Wolfgang.Conflict.Classic.Engine.Players;
using Wolfgang.Conflict.Classic.Engine.Units;
using Xunit;

namespace Wolfgang.Conflict.Classic.Engine.Tests.Game;

public class MoveUnitTests
{
    private static async Task<(GameEngine engine, GameState state, Unit blueInfantry)> Setup()
    {
        var mission = await MissionLoader.LoadMission01Async();
        var engine = new GameEngine();
        var state = engine.StartGame(mission, randomSeed: 1);
        var infantry = state.Units.Values.Single(u => u.Side == Side.Blue && u.Type == TestCatalog.Infantry);
        return (engine, state, infantry);
    }

    [Fact]
    public async Task MoveUnit_advances_unit_and_deducts_path_cost_from_moves_only()
    {
        var (engine, state, infantry) = await Setup();
        var dest = new HexCoord(3, 6); // one step east from (2, 6) on plains

        var next = engine.MoveUnit(state, infantry.Id, dest);
        var moved = next.Units[infantry.Id];

        Assert.Equal(dest, moved.Coord);
        Assert.Equal(infantry.MovesRemaining - 1, moved.MovesRemaining);
        Assert.Equal(infantry.Fuel, moved.Fuel); // fuel unchanged until EndTurn
        Assert.True(moved.HasMoved);
    }

    [Fact]
    public async Task MoveUnit_twice_in_one_turn_throws()
    {
        var (engine, state, infantry) = await Setup();

        var afterFirst = engine.MoveUnit(state, infantry.Id, new HexCoord(3, 6));

        // The unit still has MovesRemaining, but one move action per turn.
        Assert.Throws<InvalidOperationException>(() =>
            engine.MoveUnit(afterFirst, infantry.Id, new HexCoord(4, 6)));
    }

    [Fact]
    public async Task GetLegalMoves_is_empty_after_a_unit_has_moved()
    {
        var (engine, state, infantry) = await Setup();

        var afterMove = engine.MoveUnit(state, infantry.Id, new HexCoord(3, 6));

        Assert.Empty(engine.GetLegalMoves(afterMove, infantry.Id));
    }

    [Fact]
    public async Task MoveUnit_to_unreachable_hex_throws()
    {
        var (engine, state, infantry) = await Setup();

        Assert.Throws<InvalidOperationException>(() =>
            engine.MoveUnit(state, infantry.Id, new HexCoord(10, -4)));
    }

    [Fact]
    public async Task MoveUnit_when_not_your_turn_throws()
    {
        var (engine, state, _) = await Setup();
        var redInfantry = state.Units.Values.Single(u => u.Side == Side.Red && u.Category == Engine.Units.UnitCategory.Infantry);

        Assert.Throws<InvalidOperationException>(() =>
            engine.MoveUnit(state, redInfantry.Id, new HexCoord(9, -4)));
    }

    [Fact]
    public async Task MoveUnit_unknown_id_throws()
    {
        var (engine, state, _) = await Setup();

        Assert.Throws<InvalidOperationException>(() =>
            engine.MoveUnit(state, new UnitId(999), new HexCoord(0, 0)));
    }

    [Fact]
    public async Task MoveUnit_to_same_hex_throws()
    {
        var (engine, state, infantry) = await Setup();

        Assert.Throws<InvalidOperationException>(() =>
            engine.MoveUnit(state, infantry.Id, infantry.Coord));
    }

    [Fact]
    public async Task GetLegalMoves_for_blue_infantry_includes_adjacent_plains()
    {
        var (engine, state, infantry) = await Setup();

        var moves = engine.GetLegalMoves(state, infantry.Id);

        Assert.Contains(new HexCoord(3, 6), moves);
        Assert.DoesNotContain(infantry.Coord, moves);
    }
}
