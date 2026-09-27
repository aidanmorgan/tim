using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class PipeBendTests(HeadlessFixture godot, ITestOutputHelper output)
{
    [Theory]
    [InlineData("pipe_bend_45", 0, 0, 0)]
    [InlineData("pipe_bend_90", 0, 0, 0)]
    [InlineData("pipe_bend_45", 30, 40, 50)]
    [InlineData("pipe_bend_90", 30, 40, 50)]
    [InlineData("pipe_bend_45", 0, 90, 90)]
    [InlineData("pipe_bend_90", 0, 90, 90)]
    public void BallsTurnThroughHollowBendsWithoutAddedEnergy(string kind, float x, float y, float z)
    {
        var world = new MachineWorld { Gravity = 0, Pressure = 0 };
        godot.Tree.Root.AddChild(world);
        try
        {
            var bend = (PipeBendPart)world.AddPart(new() { Id = "bend", Kind = kind, Position = [0, 8, 0], Rotation = [x,y,z] });
            var ball = world.AddPart(new() { Id = "ball", Kind = "ball" });
            var inlet = bend.Mouths.Single(m => m.Id == TubeMouthId.Start);
            var outlet = bend.Mouths.Single(m => m.Id == TubeMouthId.End);
            ball.Position = bend.Transform * (inlet.Position + inlet.Outward * .8f);
            world.Start();
            ball.Velocity = bend.Basis * -inlet.Outward * 6;
            var previous = ball.Position;
            var passed = false;
            for (var i = 0; i < 300; i++)
            {
                world.Step();
                Assert.True(ball.Position.DistanceTo(previous) < .051f);
                Assert.InRange(ball.Velocity.Length(), 0, 6.001f);
                previous = ball.Position;
                var local = bend.Transform.AffineInverse() * ball.Position;
                if ((local - outlet.Position).Dot(outlet.Outward) > .5f)
                {
                    Assert.True((bend.Basis.Inverse() * ball.Velocity).Normalized().Dot(outlet.Outward) > .8f);
                    passed = true;
                    break;
                }
            }
            Assert.True(passed);
            Assert.Empty(bend.Boxes);
            Assert.Single(bend.Bends);
            Assert.Equal(2, bend.Tubes.Count);
            world.Restore();
            Assert.Equal(Vector3.Zero, world.FindPart("ball")!.Velocity);
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData("pipe_bend_45")]
    [InlineData("pipe_bend_90")]
    public void BendMouthsSnapToStraightAndOtherBends(string kind)
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        try
        {
            var a = (PipeBendPart)world.AddPart(new() { Id = "a", Kind = kind, Position = [0,4,0], Rotation = [25,35,45] });
            foreach (var otherKind in new[] { "pipe", "pipe_bend_45", "pipe_bend_90" })
            {
                var b = world.AddPart(new() { Id = "b", Kind = otherKind });
                var target = a.Mouths.Single(m => m.Id == TubeMouthId.End);
                var source = ((ITubePart)b).Mouths.Single(m => m.Id == TubeMouthId.Start);
                var targetNormal = a.GlobalBasis * target.Outward;
                b.GlobalBasis = new Basis(new Quaternion(source.Outward, -targetNormal));
                b.GlobalPosition = a.GlobalTransform * target.Position - b.GlobalBasis * source.Position + Vector3.Up * .1f;
                var snapped = TubePlacementSnap.Find(world, b);
                Assert.NotNull(snapped);
                b.GlobalTransform = snapped!.Value;
                Assert.True((a.GlobalTransform * target.Position).DistanceTo(b.GlobalTransform * source.Position) < .0001f);
                world.RemovePart(b);
            }
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData("pipe_bend_45")]
    [InlineData("pipe_bend_90")]
    public void GravityAloneCarriesAFallingBallAroundTheBend(string kind)
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        try
        {
            var bend = (PipeBendPart)world.AddPart(new() { Id = "bend", Kind = kind, Position = [0,4,0], Rotation = [0,0,-90] });
            var inlet = bend.Mouths.Single(m => m.Id == TubeMouthId.Start);
            var ball = world.AddPart(new() { Id = "ball", Kind = "ball" });
            ball.Position = bend.Transform * inlet.Position + Vector3.Up * 1.6f;
            world.Start();
            for (var i = 0; i < 600 && ball.Position.Y > 1.2f; i++) world.Step();
            output.WriteLine($"{kind} at basket height: {ball.Position}; velocity: {ball.Velocity}");
            Assert.True(ball.Position.Y <= 1.2f);
            Assert.True(ball.Position.X < -.5f);
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData("gentle_bend", 0f)]
    [InlineData("gentle_bend", .45f)]
    [InlineData("gentle_bend", 1f)]
    [InlineData("quarter_bend", 0f)]
    [InlineData("quarter_bend", .45f)]
    [InlineData("quarter_bend", 1f)]
    public void PlacementAssistanceAlignsWithinTheAuthoredBendWindow(string id, float precision)
    {
        var world = new MachineWorld { Precision = precision };
        godot.Tree.Root.AddChild(world);
        try
        {
            var puzzle = MachineCodec.ReadPuzzles(Godot.FileAccess.GetFileAsString("res://content/puzzles.json")).Single(p => p.Id == id);
            var data = MachineCodec.Clone(puzzle.CreateMachine());
            foreach (var fixture in data.Parts) fixture.Difficulty.Clear();
            data.Parts.AddRange(puzzle.Solution);
            data.Parts.Single(p => p.Id == "bend_1").Position[2] = .48f;
            world.LoadMachine(data);
            world.Start();
            for (var tick = 0; tick < 3600 && world.Running; tick++) world.Step();
            output.WriteLine($"{id}, precision {precision}: won {world.Won}; corrected bend {world.FindPart("bend_1")!.Position}");
            Assert.Equal(precision < 1 || id == "gentle_bend", world.Won);
            Assert.Equal(precision < 1 ? 0 : .48f, world.FindPart("bend_1")!.Position.Z, 4);
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(45)]
    [InlineData(90)]
    public void CurvedSurfaceDistinguishesBoreShellExteriorAndOpenEnd(int degrees)
    {
        var bend = new BendProxy(Transform3D.Identity, 2.4f, Mathf.DegToRad(degrees), .65f, .70f);
        var angle = bend.Sweep * .5f;
        var centre = bend.Centre(angle);
        var normal = BendProxy.Radial(angle);
        Assert.Equal(.65f, bend.Surface(centre).Distance, 4);
        Assert.Equal(.35f, bend.Surface(centre + normal * .3f).Distance, 4);
        Assert.Equal(-.02f, bend.Surface(centre + normal * .67f).Distance, 4);
        Assert.Equal(.30f, bend.Surface(centre + normal).Distance, 4);
        Assert.True(bend.Surface(centre + normal).Normal.Dot(normal) > .999f);
        Assert.True(bend.Surface(centre + normal * .3f).Normal.Dot(-normal) > .999f);
        Assert.True(bend.Surface(bend.Centre(0) - Vector3.Right).Distance > 1);
    }
}
