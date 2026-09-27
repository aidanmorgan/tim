using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class PulleyRopeRouteTests(HeadlessFixture godot)
{
    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(30, 50, 70, 0)]
    [InlineData(90, 0, 0, 2)]
    public void RimArcAndFreeLegsStayOutsideTheWheel(float x, float y, float z, float depth)
    {
        var transform = new Transform3D(Basis.FromEuler(new Vector3(x, y, z) * Mathf.Pi / 180), new(1, 4, 2));
        var from = transform * new Vector3(-2, -4, .12f + depth);
        var to = transform * new Vector3(2, -4, .12f - depth);
        var winding = PulleyRopeRoute.Choose(transform, from, to);
        var route = PulleyRopeRoute.Create(transform, from, to, winding);
        var inverse = transform.AffineInverse();
        Assert.True(Mathf.Abs(route.Sweep) < Mathf.Pi);
        for (var i = 0; i <= 128; i++)
        {
            var t = i / 128f;
            var local = inverse * route.Point(t);
            Assert.InRange(Mathf.Abs(new Vector2(local.X, local.Y).Length() - PulleyRopeRoute.Radius), 0, .00001f);
            Assert.InRange(Mathf.Abs(local.Z - .12f), 0, .00001f);
            foreach (var point in new[] { from.Lerp(route.Point(0), t), to.Lerp(route.Point(1), t) })
            {
                var leg = inverse * point;
                Assert.True(new Vector2(leg.X, leg.Y).Length() >= PulleyRopeRoute.Radius - .00001f);
            }
        }
        // Tangency is defined in the wheel plane even when a leg also traverses depth.
        foreach (var pair in new[] { (from, route.Point(0)), (to, route.Point(1)) })
        {
            var endpoint = inverse * pair.Item1;
            var tangent = inverse * pair.Item2;
            Assert.InRange(Mathf.Abs(new Vector2(tangent.X, tangent.Y).Dot(new Vector2(endpoint.X - tangent.X, endpoint.Y - tangent.Y))), 0, .00001f);
        }
    }

    [Fact]
    public void CloseStartingLoadThreadsOverTheTopRatherThanMakingAFullLoop()
    {
        var from = new Vector3(-4, .4f, .12f);
        var near = new Vector3(0, -.412f, .12f);
        var far = new Vector3(0, -3, .12f);
        var winding = PulleyRopeRoute.Choose(Transform3D.Identity, from, near);
        Assert.Equal(RopeWinding.Clockwise, winding);
        var route = PulleyRopeRoute.Create(Transform3D.Identity, from, far, winding);
        Assert.True(Mathf.Abs(route.Sweep) < Mathf.Pi);
        Assert.True(route.Point(1).X > 0);
    }

    [Fact]
    public void SymmetricThreadingUsesTheTopAndSmallMotionDoesNotFlipItsSide()
    {
        var transform = Transform3D.Identity;
        var a = new Vector3(-3, 0, .12f);
        var b = new Vector3(3, 0, .12f);
        var winding = PulleyRopeRoute.Choose(transform, a, b);
        var route = PulleyRopeRoute.Create(transform, a, b, winding);
        Assert.True(route.Point(.5f).Y > .47f);
        var moved = PulleyRopeRoute.Create(transform, a + new Vector3(0, .001f, .1f), b, winding);
        for (var i = 0; i <= 32; i++)
            Assert.True(route.Point(i / 32f).DistanceTo(moved.Point(i / 32f)) < .001f);
        var reversed = PulleyRopeRoute.Create(transform, b, a, (RopeWinding)(-(int)winding));
        for (var i = 0; i <= 32; i++)
            Assert.True(route.Point(i / 32f).DistanceTo(reversed.Point(1 - i / 32f)) < .00001f);
    }

    [Theory]
    [InlineData("counterweight")]
    [InlineData("pulley_depth")]
    public void CampaignRopeArtworkUpdatesWithoutMutatingSimulation(string id)
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        RopeVisual? visual = null;
        try
        {
            var puzzle = MachineCodec.ReadPuzzles(Godot.FileAccess.GetFileAsString("res://content/puzzles.json")).Single(p => p.Id == id);
            var data = MachineCodec.Clone(puzzle.CreateMachine());
            data.Parts.AddRange(puzzle.Solution);
            data.Connections = puzzle.SolutionConnections;
            world.LoadMachine(data);
            world.Start();
            visual = new() { Path = Assert.Single(world.Ropes) };
            world.AddChild(visual);
            for (var tick = 0; tick < 180 && world.Running; tick++)
            {
                world.Step();
                var signature = world.StateSignature();
                visual._Process(1d / 60);
                Assert.Equal(signature, world.StateSignature());
                foreach (var mesh in visual.GetChildren().OfType<MeshInstance3D>())
                    Assert.True(mesh.Position.IsFinite());
            }
            Assert.Equal(2, visual.GetChildren().OfType<MeshInstance3D>().Count(m => m.Mesh is SphereMesh && m.Visible));
        }
        finally { visual?.Free(); world.Free(); }
    }
}
