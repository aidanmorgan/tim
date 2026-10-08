using Godot;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class TubePlacementSnapTests(NativeSceneFixture godot)
{
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(30, 40, 50)]
    [InlineData(0, 90, 90)]
    public void MouthAlignmentPreservesBoreAndContinuousSeamTravel(float x, float y, float z)
    {
        var world = new MachineWorld { Gravity = 0, Pressure = 0 };
        godot.Tree.Root.AddChild(world);
        try
        {
            var a = (PipePart)world.AddPart(new() { Id = "a", Kind = "pipe", Position = [0, 8, 0], Orientation = PartOrientation.FromEulerDegrees(x, y, z), Locked = true });
            var b = (PipePart)world.AddPart(new() { Id = "b", Kind = "pipe" });
            b.GlobalTransform = a.GlobalTransform * new Transform3D(new Basis(Vector3.Back, .08f), new(3.9f, .05f, 0));
            var original = b.GlobalTransform;
            var snap = TubePlacementSnap.Find(world, b);
            Assert.NotNull(snap);
            Assert.Equal(original, b.GlobalTransform); // Query is non-mutating.
            b.GlobalTransform = snap!.Value;
            var end = a.Mouths.Single(m => m.Id == TubeMouthId.End);
            var start = b.Mouths.Single(m => m.Id == TubeMouthId.Start);
            Assert.True((a.GlobalTransform * end.Position).DistanceTo(b.GlobalTransform * start.Position) < .0001f);
            Assert.True((a.GlobalBasis * end.Outward).Dot(b.GlobalBasis * start.Outward) < -.9999f);
            var ball = world.AddPart(new() { Id = "ball", Kind = "ball" });
            ball.Position = a.Transform * new Vector3(-3, .1f, 0);
            ball.InitialVelocity = a.Basis.X * 4;
            world.Start();
            var previous = ball.Position;
            for (var i = 0; i < 300; i++)
            {
                world.Step();
                Assert.True(ball.Position.DistanceTo(previous) < .04f);
                previous = ball.Position;
            }
            Assert.InRange((a.Transform.AffineInverse() * ball.Position).X, 6.99f, 7.01f);
            Assert.InRange(world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity.Length, 3.99f, 4.01f);
            Assert.Null(TubePlacementSnap.Find(world, b));
            var placement = b.Serialize().Position;
            world.Restore();
            Assert.Equal(placement, world.FindPart("b")!.Serialize().Position);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void CapturedAndForeignPartsCannotSnapUntilReset()
    {
        var world=new MachineWorld();
        var other=new MachineWorld();
        godot.Tree.Root.AddChild(world);
        godot.Tree.Root.AddChild(other);
        try
        {
            world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind="pipe",Position=[0,4,0]});
            var moving=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind="pipe",Position=[3.9f,4,0]});
            var foreign=other.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind="pipe",Position=[3.9f,4,0]});
            Assert.NotNull(TubePlacementSnap.Find(world,moving));
            Assert.Null(TubePlacementSnap.Find(world,foreign));
            var pose=moving.Transform;
            world.Start();
            var physics=world.Physics;
            var bodies=physics.Capture().BodyStates.ToArray();
            Assert.Null(TubePlacementSnap.Find(world,moving));
            world.Running=false;
            Assert.Null(TubePlacementSnap.Find(world,moving));
            Assert.Equal(pose,moving.Transform);
            Assert.Same(physics,world.Physics);
            Assert.Equal(bodies,physics.Capture().BodyStates.ToArray());
            world.Restore();
            moving=world.FindPart(FixtureParts.Id(FixturePartId.Second))!;
            Assert.Equal(pose,moving.Transform);
            Assert.NotNull(TubePlacementSnap.Find(world,moving));
            Assert.Null(TubePlacementSnap.Find(world,foreign));
        }
        finally {world.Free();other.Free();}
    }

    [Theory]
    [InlineData(4.5f, 0f)]
    [InlineData(3.78f, 40f)]
    public void DistantOrWrongFacingMouthsDoNotSnap(float separation, float angle)
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        try
        {
            world.AddPart(new() { Id = "a", Kind = "pipe", Position = [0, 4, 0] });
            var b = world.AddPart(new() { Id = "b", Kind = "pipe", Position = [separation, 4, 0], Orientation = PartOrientation.FromEulerDegrees(0, 0, angle) });
            Assert.Null(TubePlacementSnap.Find(world, b));
        }
        finally { world.Free(); }
    }

    [Fact]
    public void OccupiedMouthAndLockedPartAreNotSnapCandidates()
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        try
        {
            world.AddPart(new() { Id = "a", Kind = "pipe", Position = [0, 4, 0] });
            world.AddPart(new() { Id = "joined", Kind = "pipe", Position = [3.78f, 4, 0] });
            var b = world.AddPart(new() { Id = "moving", Kind = "pipe", Position = [3.9f, 4, 0] });
            Assert.Null(TubePlacementSnap.Find(world, b));
            world.RemovePart(world.FindPart("joined")!);
            Assert.NotNull(TubePlacementSnap.Find(world, b));
            world.RemovePart(b);
            var locked = world.AddPart(new() { Id = "locked", Kind = "pipe", Position = [3.9f, 4, 0], Locked = true });
            Assert.Null(TubePlacementSnap.Find(world, locked));
        }
        finally { world.Free(); }
    }
}
