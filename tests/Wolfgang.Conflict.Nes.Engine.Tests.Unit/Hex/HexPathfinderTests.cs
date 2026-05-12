using Wolfgang.Conflict.Nes.Engine.Hex;
using Xunit;

namespace Wolfgang.Conflict.Nes.Engine.Tests.Hex;

public class HexPathfinderTests
{
    private static int? UniformCost(HexCoord _) => 1;

    [Fact]
    public void Start_equals_goal_returns_single_hex_path()
    {
        var path = HexPathfinder.FindPath(HexCoord.Zero, HexCoord.Zero, UniformCost);

        Assert.NotNull(path);
        Assert.Single(path);
        Assert.Equal(0, path!.TotalCost);
    }

    [Fact]
    public void Straight_east_path_has_correct_cost_and_length()
    {
        var goal = new HexCoord(3, 0);

        var path = HexPathfinder.FindPath(HexCoord.Zero, goal, UniformCost);

        Assert.NotNull(path);
        Assert.Equal(4, path!.Count);
        Assert.Equal(3, path.TotalCost);
        Assert.Equal(HexCoord.Zero, path.Start);
        Assert.Equal(goal, path.Destination);
    }

    [Fact]
    public void Returns_null_when_goal_is_unreachable()
    {
        // Everything is impassable except the start.
        Func<HexCoord, int?> stepCost = h => h == HexCoord.Zero ? 0 : null;

        var path = HexPathfinder.FindPath(HexCoord.Zero, new HexCoord(2, 0), stepCost);

        Assert.Null(path);
    }

    [Fact]
    public void Routes_around_impassable_hex()
    {
        // Block the direct east-east cell (1,0); the path must detour.
        var blocked = new HexCoord(1, 0);
        Func<HexCoord, int?> stepCost = h => h == blocked ? null : 1;

        var path = HexPathfinder.FindPath(HexCoord.Zero, new HexCoord(2, 0), stepCost);

        Assert.NotNull(path);
        Assert.DoesNotContain(blocked, path!);
        Assert.True(path.TotalCost >= 3);
    }

    [Fact]
    public void Higher_cost_terrain_is_preferred_only_when_shorter()
    {
        // (1,0) costs 10; detour through (1,-1) and (0,1) etc. costs 1 each.
        // Direct path: 0,0 -> 1,0 -> 2,0 = 10+1 = 11
        // Detour:      0,0 -> 1,-1 -> 2,-1 -> 2,0 = 1+1+1 = 3
        var expensive = new HexCoord(1, 0);
        Func<HexCoord, int?> stepCost = h => h == expensive ? 10 : 1;

        var path = HexPathfinder.FindPath(HexCoord.Zero, new HexCoord(2, 0), stepCost);

        Assert.NotNull(path);
        Assert.DoesNotContain(expensive, path!);
        Assert.Equal(3, path!.TotalCost);
    }

    [Fact]
    public void MaxCost_excludes_paths_above_threshold()
    {
        var path = HexPathfinder.FindPath(HexCoord.Zero, new HexCoord(5, 0), UniformCost, maxCost: 3);

        Assert.Null(path);
    }

    [Fact]
    public void Null_stepCost_throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            HexPathfinder.FindPath(HexCoord.Zero, new HexCoord(1, 0), stepCost: null!));
    }
}
