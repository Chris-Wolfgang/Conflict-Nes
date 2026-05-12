using Wolfgang.Conflict.Nes.Engine.Game;
using Wolfgang.Conflict.Nes.Engine.Hex;
using Wolfgang.Conflict.Nes.Engine.Players;
using Wolfgang.Conflict.Nes.Engine.Units;
using Xunit;

namespace Wolfgang.Conflict.Nes.Engine.Tests.Game;

public class MoveUnitTests
{
    private static async Task<(GameEngine engine, GameState state, Unit blueInfantry)> Setup()
    {
        var mission = await MissionLoader.LoadMission01Async();
        var engine = new GameEngine();
        var state = engine.StartGame(mission, randomSeed: 1);
        var infantry = state.Units.Values.Single(u => u.Side == Side.Blue && u.Kind == UnitKind.Infantry);
        return (engine, state, infantry);
    }

    [Fact]
    public async Task MoveUnit_advances_unit_and_deducts_cost_from_moves_and_fuel()
    {
        var (engine, state, infantry) = await Setup();
        var dest = new HexCoord(3, 6); // one step east from (2, 6) on plains

        var next = engine.MoveUnit(state, infantry.Id, dest);
        var moved = next.Units[infantry.Id];

        Assert.Equal(dest, moved.Coord);
        Assert.Equal(infantry.MovesRemaining - 1, moved.MovesRemaining);
        Assert.Equal(infantry.Fuel - 1, moved.Fuel);
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
        var redInfantry = state.Units.Values.Single(u => u.Side == Side.Red && u.Kind == UnitKind.Infantry);

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
