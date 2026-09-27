using Godot;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class ImpactLeverSphereObstructionTests(HeadlessFixture godot)
{
    public enum Placement { Aligned, DepthMiss, Overlap }
    private enum Role { Lever, Bumper, Driver }
    private static PartSpec Spec(Role role, Vector3 at) => new()
    {
        Id = role switch { Role.Lever => "lever", Role.Bumper => "bumper", Role.Driver => "driver",
            _ => throw new ArgumentOutOfRangeException(nameof(role)) },
        Kind = role switch { Role.Lever => ImpactLeverPart.CatalogId, Role.Bumper => "bumper", Role.Driver => "bowling",
            _ => throw new ArgumentOutOfRangeException(nameof(role)) },
        Position = [at.X, at.Y, at.Z]
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
                Assert.Throws<HingeFixtureOverlapException>(world.Start);
                Assert.False(world.Running);
                Assert.Equal(before,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
                return;
            }
            world.Start();
            lever.Beam.Joint.ApplyAngularImpulse(lever.Beam.Joint.Inertia * speed);
            for (var tick = 0; tick < 120; tick++) world.Step();
            if (placement == Placement.DepthMiss)
                Assert.Equal(ImpactLeverPart.LimitAngle,lever.Beam.Joint.Angle);
            else
            {
                // At contact: cos(a) - 1.5 sin(a) = bumper radius + beam half-height.
                var expected = Math.Acos(.77 / Math.Sqrt(3.25)) - Math.Atan(1.5);
                Assert.InRange(Math.Abs(lever.Beam.Joint.Angle),expected - .0001,expected + .00001);
                Assert.Equal(0,lever.Beam.Joint.Energy);
                var stopped = lever.Beam.Joint.Angle;
                lever.Beam.Joint.ApplyAngularImpulse(-Math.Sign(speed) * lever.Beam.Joint.Inertia);
                world.Step();
                Assert.True(Math.Abs(lever.Beam.Joint.Angle) < Math.Abs(stopped));
                world.RemovePart(bumper);
                lever.Beam.Joint.ApplyAngularImpulse(Math.Sign(speed) * lever.Beam.Joint.Inertia * 4);
                for (var tick = 0; tick < 120; tick++) world.Step();
                Assert.Equal(Math.Sign(speed) * ImpactLeverPart.LimitAngle,lever.Beam.Joint.Angle);
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
            lever.Beam.Joint.ApplyAngularImpulse(lever.Beam.Joint.Inertia * 3);
            for (var tick = 0; tick < 120; tick++) world.Step();
            var expected = Math.Acos(.77 / Math.Sqrt(3.25)) - Math.Atan(1.5);
            Assert.InRange(lever.Beam.Joint.Angle,expected - .0001,expected + .00001);
            Assert.Equal(0,lever.Beam.Joint.AngularVelocity);
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
            lever.Beam.Joint.ApplyAngularImpulse(-lever.Beam.Joint.Inertia * 3);
            for (var tick = 0; tick < 120; tick++) world.Step();
            Assert.Equal(-ImpactLeverPart.LimitAngle,lever.Beam.Joint.Angle);
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
            Assert.Throws<InvalidOperationException>(world.Start);
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
                maximum = Math.Max(maximum,lever.Beam.Joint.Angle);
                Assert.InRange(world.MaximumFlightIterationsThisStep,1,100);
            }
            Assert.InRange(maximum,.14,.16);
            Assert.Equal(0,lever.Beam.Joint.AngularVelocity);
            Assert.Equal(0,bumper.HitCount); // The bumper's kick contract applies to dynamic balls, not hinge beams.
        }
        finally { world.Free(); }
    }
}
