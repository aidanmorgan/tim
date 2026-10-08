using Godot;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class FloorTests(NativeSceneFixture godot)
{
    public enum BodyKind { Ball, Bowling, Tennis }
    private const string BodyId="ball";
    private static string Catalog(BodyKind kind)=>kind switch
    {
        BodyKind.Ball=>"ball",BodyKind.Bowling=>"bowling",BodyKind.Tennis=>"tennis",
        _=>throw new ArgumentOutOfRangeException(nameof(kind))
    };
    private MachineWorld World(BodyKind kind = BodyKind.Ball, float x = 0, float z = 0)
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        world.LoadMachine(new() { Parts = [new() { Id = BodyId, Kind = Catalog(kind), Position = [x, 3, z] }] });
        return world;
    }

    [Theory]
    [InlineData(BodyKind.Ball)]
    [InlineData(BodyKind.Bowling)]
    [InlineData(BodyKind.Tennis)]
    public void BallsBounceOnVisibleFloorThenSettle(BodyKind kind)
    {
        var world = World(kind);
        try
        {
            var body = world.FindPart(BodyId)!;
            world.Start();
            var bounced = false;
            for (var i = 0; i < 2400; i++)
            {
                world.Step();
                bounced |= world.PhysicsAssembly.Body(new(body,MachinePart.RootBody)).LinearVelocity.Y > .1f;
                Assert.True(body.Position.Y - body.Radius >= Workbench.SurfaceY - .001f);
                Assert.True(body.Visible);
            }
            Assert.True(bounced);
            Assert.InRange(body.Position.Y - body.Radius, Workbench.SurfaceY - .001f, Workbench.SurfaceY + .002f);
            Assert.InRange(world.PhysicsAssembly.Body(new(body,MachinePart.RootBody)).LinearVelocity.Length, 0, .05f);
            Assert.DoesNotContain(new MachineEvent(MachineEventKind.Escaped, BodyId), world.Events.Keys);
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
            world.Start();
            for (var i = 0; i < 300; i++) world.Step();
            Assert.False(world.FindPart(BodyId)!.Visible);
            Assert.Contains(new MachineEvent(MachineEventKind.Escaped, BodyId), world.Events.Keys);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void FloorStopsFastDescentAndResetRestoresStartingHeight()
    {
        var world = World();
        try
        {
            var body = world.FindPart(BodyId)!;
            body.InitialVelocity = Vector3.Down * 40;
            world.Start();
            for (var i = 0; i < 10; i++) world.Step();
            Assert.True(world.PhysicsAssembly.Body(new(body,MachinePart.RootBody)).LinearVelocity.Y > 0);
            Assert.True(body.Position.Y - body.Radius >= Workbench.SurfaceY);
            world.Restore();
            Assert.Equal(new Vector3(0, 3, 0), world.FindPart(BodyId)!.Position);
            Assert.Equal(Vector3.Down*40,world.FindPart(BodyId)!.InitialVelocity);
            Assert.Single(world.Parts); // The floor is not inventory or a saved puzzle part.
        }
        finally { world.Free(); }
    }
}
