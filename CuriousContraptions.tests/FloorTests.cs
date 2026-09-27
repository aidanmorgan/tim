using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class FloorTests(HeadlessFixture godot)
{
    private MachineWorld World(string kind = "ball", float x = 0, float z = 0)
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        world.LoadMachine(new() { Parts = [new() { Id = "ball", Kind = kind, Position = [x, 3, z] }] });
        world.Start();
        return world;
    }

    [Theory]
    [InlineData("ball")]
    [InlineData("bowling")]
    [InlineData("tennis")]
    public void BallsBounceOnVisibleFloorThenSettle(string kind)
    {
        var world = World(kind);
        try
        {
            var body = world.FindPart("ball")!;
            var bounced = false;
            for (var i = 0; i < 2400; i++)
            {
                world.Step();
                bounced |= body.Velocity.Y > .1f;
                Assert.True(body.Position.Y - body.Radius >= Workbench.SurfaceY - .001f);
                Assert.True(body.Visible);
            }
            Assert.True(bounced);
            Assert.InRange(body.Position.Y - body.Radius, Workbench.SurfaceY - .001f, Workbench.SurfaceY + .002f);
            Assert.InRange(body.Velocity.Length(), 0, .05f);
            Assert.DoesNotContain(new MachineEvent(MachineEventKind.Escaped, "ball"), world.Events.Keys);
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(10f, 0f)]
    [InlineData(0f, 6f)]
    public void BodiesOutsideTheFiniteBenchStillFall(float x, float z)
    {
        var world = World(x: x, z: z);
        try
        {
            for (var i = 0; i < 300; i++) world.Step();
            Assert.False(world.FindPart("ball")!.Visible);
            Assert.Contains(new MachineEvent(MachineEventKind.Escaped, "ball"), world.Events.Keys);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void FloorStopsFastDescentAndResetRestoresStartingHeight()
    {
        var world = World();
        try
        {
            var body = world.FindPart("ball")!;
            body.Position = new(0, 0, 0);
            body.Velocity = Vector3.Down * 40;
            for (var i = 0; i < 10; i++) world.Step();
            Assert.True(body.Velocity.Y > 0);
            Assert.True(body.Position.Y - body.Radius >= Workbench.SurfaceY);
            world.Restore();
            Assert.Equal(new Vector3(0, 3, 0), world.FindPart("ball")!.Position);
            Assert.Single(world.Parts); // The floor is not inventory or a saved puzzle part.
        }
        finally { world.Free(); }
    }
}
