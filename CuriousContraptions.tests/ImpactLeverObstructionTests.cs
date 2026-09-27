using Godot;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class ImpactLeverObstructionTests(HeadlessFixture godot)
{
    public enum ObstacleCase { Wall, DepthMiss, Deck }
    private enum Role { Lever, Wall, Driver }
    private static PartSpec Spec(Role role,Vector3 at) => new()
    {
        Id = role switch { Role.Lever => "lever", Role.Wall => "wall", Role.Driver => "driver", _ => throw new ArgumentOutOfRangeException(nameof(role)) },
        Kind = role switch { Role.Lever => ImpactLeverPart.CatalogId, Role.Wall => "wall", Role.Driver => "bowling", _ => throw new ArgumentOutOfRangeException(nameof(role)) },
        Position = [at.X,at.Y,at.Z]
    };

    [Fact]
    public void InvalidInitialBeamOverlapIsRejectedBeforeRunWithoutMovingTheConstruction()
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        try
        {
            world.AddPart(Spec(Role.Lever,new(0,3,0)));
            world.AddPart(Spec(Role.Wall,new(1,3,0)));
            var before = JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            Assert.Throws<HingeFixtureOverlapException>(world.Start);
            Assert.False(world.Running);
            Assert.Equal(before,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
        }
        finally { world.Free(); }
    }

    [Fact]
    public void RepeatedPayloadPressureCannotDriveThroughTheWallAndResetClearsTheContact()
    {
        var world = new MachineWorld { Gravity = 9.81f, Pressure = 0 };
        godot.Tree.Root.AddChild(world);
        try
        {
            var lever = (ImpactLeverPart)world.AddPart(Spec(Role.Lever,new(0,3,0)));
            var wall = (WallPart)world.AddPart(Spec(Role.Wall,new(1.5f,3.6f,0)));
            wall.SetDimensions(new(.6f,.4f,1.4f));
            world.AddPart(Spec(Role.Driver,new(-1.2f,6,0)));
            var before = JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            double maximum = 0;
            for (var tick = 0; tick < 600; tick++)
            {
                world.Step();
                maximum = Math.Max(maximum,lever.Beam.Joint.Angle);
                Assert.True(double.IsFinite(lever.Beam.Joint.AngularVelocity));
                Assert.InRange(world.MaximumFlightIterationsThisStep,1,100);
            }
            Assert.InRange(maximum,.15,.17);
            world.Restore();
            Assert.Equal(before,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            var restored = (ImpactLeverPart)world.FindPart(Spec(Role.Lever,Vector3.Zero).Id)!;
            Assert.Equal(AngularBlock.None,restored.Beam.Joint.ContactBlock);
            Assert.Equal(0,restored.Beam.Joint.Angle);
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(ObstacleCase.Wall)]
    [InlineData(ObstacleCase.DepthMiss)]
    [InlineData(ObstacleCase.Deck)]
    public void BeamStopsAtAnObstacleButNotAMissedDepth(ObstacleCase obstacle)
    {
        var world = new MachineWorld { Gravity = 0, Pressure = 0 };
        godot.Tree.Root.AddChild(world);
        try
        {
            var height = obstacle == ObstacleCase.Deck ? .2f : 3;
            var lever = (ImpactLeverPart)world.AddPart(Spec(Role.Lever,new(0,height,0)));
            if (obstacle != ObstacleCase.Deck)
            {
                var wall = (WallPart)world.AddPart(Spec(Role.Wall,new(1.5f,3.6f,obstacle == ObstacleCase.DepthMiss ? 2 : 0)));
                wall.SetDimensions(new(.6f,.4f,1.4f));
            }
            world.Start();
            lever.Beam.Joint.ApplyAngularImpulse(lever.Beam.Joint.Inertia * 3);
            for (var tick=0;tick<120;tick++) world.Step();
            if (obstacle == ObstacleCase.DepthMiss)
                Assert.Equal(ImpactLeverPart.LimitAngle,lever.Beam.Joint.Angle);
            else
            {
                Assert.InRange(lever.Beam.Joint.Angle,.05,.4);
                Assert.Equal(0,lever.Beam.Joint.AngularVelocity);
                Assert.Equal(0,lever.Beam.Joint.Energy);
                lever.Beam.Joint.ApplyAngularImpulse(lever.Beam.Joint.Inertia * 5);
                Assert.Equal(0,lever.Beam.Joint.AngularVelocity);
                var stopped = lever.Beam.Joint.Angle;
                lever.Beam.Joint.ApplyAngularImpulse(-lever.Beam.Joint.Inertia);
                world.Step();
                Assert.True(lever.Beam.Joint.Angle < stopped);
            }
        }
        finally { world.Free(); }
    }
}
