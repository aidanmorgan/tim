using Godot;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class ImpactLeverSphereObstructionTests(NativeSceneFixture godot)
{
    public enum Placement { Aligned, DepthMiss, Overlap }
    private enum Role { Lever, Bumper, Driver }
    private static PartSpec Spec(Role role, Vector3 at) => new()
    {
        Id = role switch { Role.Lever => "lever", Role.Bumper => "bumper", Role.Driver => "driver",
            _ => throw new ArgumentOutOfRangeException(nameof(role)) },
        Kind = role switch { Role.Lever => ImpactLeverPart.CatalogId, Role.Bumper => "bumper", Role.Driver => "bowling",
            _ => throw new ArgumentOutOfRangeException(nameof(role)) },
        Position = [at.X, at.Y, at.Z],
        Properties = role==Role.Bumper?new(){[PartParameterName.Of(BumperParameter.Strength)]=0}:new()
    };

    [Theory]
    [InlineData(Placement.Aligned, 3)]
    [InlineData(Placement.Aligned, -3)]
    [InlineData(Placement.Aligned, 20000)]
    [InlineData(Placement.Aligned, -20000)]
    [InlineData(Placement.DepthMiss, 3)]
    [InlineData(Placement.Overlap, 3)]
    public void SphericalFixtureBlocksOnlyTheIntersectingBeam(Placement placement, double speed)
    {
        var world = new MachineWorld { Gravity = 0, Pressure = 0 };
        godot.Tree.Root.AddChild(world);
        try
        {
            var lever = (ImpactLeverPart)world.AddPart(Spec(Role.Lever,new(0,3,0)));
            var bumper = world.AddPart(Spec(Role.Bumper,new(1.5f * Math.Sign(speed),
                placement == Placement.Overlap ? 3 : 4,
                placement == Placement.DepthMiss ? 2 : 0)));
            var before = JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            if (placement == Placement.Overlap)
            {
                Assert.Throws<ScenePhysicsOverlapException>(world.Start);
                Assert.False(world.Running);
                Assert.Equal(before,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
                return;
            }
            world.Start();
            LeverFixture.Push(world,lever,speed);
            for (var tick = 0; tick < 120; tick++) world.Step();
            if (placement == Placement.DepthMiss)
                Assert.InRange(Math.Abs(LeverFixture.Angle(world,lever)-(ImpactLeverPart.LimitAngle)),0,1e-7);
            else
            {
                // At contact: cos(a) - 1.5 sin(a) = bumper radius + beam half-height.
                var expected = Math.Acos(.77 / Math.Sqrt(3.25)) - Math.Atan(1.5);
                Assert.InRange(Math.Abs(LeverFixture.Angle(world,lever)),expected - .0001,expected + .00001);
                Assert.InRange(LeverFixture.Energy(world,lever),0,1e-10);
                var stopped = LeverFixture.Angle(world,lever);
                LeverFixture.Push(world,lever,-Math.Sign(speed));
                world.Step();
                Assert.True(Math.Abs(LeverFixture.Angle(world,lever)) < Math.Abs(stopped));
                world.Restore();
                Assert.Equal(before,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
                lever=(ImpactLeverPart)world.FindPart(Spec(Role.Lever,default).Id)!;
                bumper=world.FindPart(Spec(Role.Bumper,default).Id)!;
                world.RemovePart(bumper);
                var cleared=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
                world.Start();
                LeverFixture.Push(world,lever,Math.Sign(speed) * 4);
                for (var tick = 0; tick < 120; tick++) world.Step();
                Assert.InRange(Math.Abs(LeverFixture.Angle(world,lever)-(Math.Sign(speed) * ImpactLeverPart.LimitAngle)),0,1e-7);
                world.Restore();
                Assert.Equal(cleared,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
                return;
            }
            world.Restore();
            Assert.Equal(before,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(15, 30, 0)]
    [InlineData(90, 0, 0)]
    [InlineData(0, 0, 90)]
    public void SphereContactFollowsAuthoredThreeDimensionalOrientation(float x, float y, float z)
    {
        var world = new MachineWorld { Gravity = 0, Pressure = 0 };
        godot.Tree.Root.AddChild(world);
        try
        {
            var lever = (ImpactLeverPart)world.AddPart(Spec(Role.Lever,new(0,5,0)));
            lever.RotationDegrees = new(x,y,z);
            world.AddPart(Spec(Role.Bumper,lever.Position + lever.Basis * new Vector3(1.5f,1,0)));
            world.Start();
            LeverFixture.Push(world,lever,3);
            for (var tick = 0; tick < 120; tick++) world.Step();
            var expected = Math.Acos(.77 / Math.Sqrt(3.25)) - Math.Atan(1.5);
            Assert.InRange(LeverFixture.Angle(world,lever),expected - .0001,expected + .00001);
            Assert.InRange(Math.Abs(LeverFixture.Speed(world,lever)),0,1e-7);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void EmptyCornersOfTheSphereBoundsDoNotBlockTheBeam()
    {
        var world = new MachineWorld { Gravity = 0, Pressure = 0 };
        godot.Tree.Root.AddChild(world);
        try
        {
            var lever = (ImpactLeverPart)world.AddPart(Spec(Role.Lever,new(0,3,0)));
            world.AddPart(Spec(Role.Bumper,new(1.5f,3.7f,1.15f)));
            world.Start(); // The sphere's bounding box intersects; its actual curved surface does not.
            LeverFixture.Push(world,lever,-3);
            for (var tick = 0; tick < 120; tick++) world.Step();
            Assert.InRange(Math.Abs(LeverFixture.Angle(world,lever)-(-ImpactLeverPart.LimitAngle)),0,1e-7);
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(2, 1, 1)]
    [InlineData(.5f, .5f, .5f)]
    [InlineData(-1, 1, 1)]
    public void NonRigidSphereFixtureCannotSilentlyUseAnUnscaledRadius(float x, float y, float z)
    {
        var world = new MachineWorld { Gravity = 0, Pressure = 0 };
        godot.Tree.Root.AddChild(world);
        try
        {
            world.AddPart(Spec(Role.Lever,new(0,3,0)));
            var bumper = world.AddPart(Spec(Role.Bumper,new(1.5f,4,0)));
            bumper.Scale = new(x,y,z);
            Assert.Throws<ArgumentException>(world.Start);
            Assert.False(world.Running);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void FallingBallCannotForceBeamThroughBumper()
    {
        var world = new MachineWorld { Gravity = 9.81f, Pressure = 0 };
        godot.Tree.Root.AddChild(world);
        try
        {
            var lever = (ImpactLeverPart)world.AddPart(Spec(Role.Lever,new(0,3,0)));
            var bumper = (BumperPart)world.AddPart(Spec(Role.Bumper,new(1.5f,4,0)));
            world.AddPart(Spec(Role.Driver,new(-1.2f,6,0)));
            world.Start();
            double maximum = 0;
            for (var tick = 0; tick < 600; tick++)
            {
                world.Step();
                maximum = Math.Max(maximum,LeverFixture.Angle(world,lever));
                // Free flight may have zero contact events; every shared substep
                // must still consume its full duration.
                Assert.InRange(world.LastPhysicsStep.Events,0,100);
                Assert.Equal((ulong)((tick+1)*MachineWorld.Substeps),world.Physics.StepIndex);
                Assert.InRange(Math.Abs(world.Physics.Time-(tick+1)*(double)MachineWorld.Tick),0,1e-9);
            }
            Assert.InRange(maximum,.14,.16);
            Assert.InRange(Math.Abs(LeverFixture.Speed(world,lever)),0,1e-7);
            Assert.True(bumper.HitCount>0); // A shared dynamic beam is a real impact participant, even with the kick disabled.
        }
        finally { world.Free(); }
    }
}
