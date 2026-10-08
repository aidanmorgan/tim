using Godot;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class ImpactLeverObstructionTests(NativeSceneFixture godot)
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
            Assert.Throws<ScenePhysicsOverlapException>(world.Start);
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
                maximum = Math.Max(maximum,LeverFixture.Angle(world,lever));
                Assert.True(double.IsFinite(LeverFixture.Speed(world,lever)));
                // Free flight may have zero contact events; every shared substep
                // must still consume its full duration.
                Assert.InRange(world.LastPhysicsStep.Events,0,100);
                Assert.Equal((ulong)((tick+1)*MachineWorld.Substeps),world.Physics.StepIndex);
                Assert.InRange(Math.Abs(world.Physics.Time-(tick+1)*(double)MachineWorld.Tick),0,1e-9);
            }
            Assert.InRange(maximum,.15,.17);
            world.Restore();
            Assert.Equal(before,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            var restored = (ImpactLeverPart)world.FindPart(Spec(Role.Lever,Vector3.Zero).Id)!;
            Assert.Equal(restored.Transform,restored.BeamTransform);
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
            LeverFixture.Push(world,lever,3);
            for (var tick=0;tick<120;tick++) world.Step();
            if (obstacle == ObstacleCase.DepthMiss)
                Assert.InRange(Math.Abs(LeverFixture.Angle(world,lever)-(ImpactLeverPart.LimitAngle)),0,1e-7);
            else
            {
                Assert.InRange(LeverFixture.Angle(world,lever),.05,.4);
                Assert.InRange(Math.Abs(LeverFixture.Speed(world,lever)),0,1e-7);
                Assert.InRange(LeverFixture.Energy(world,lever),0,1e-10);
                LeverFixture.Push(world,lever,5);
                world.Step();
                Assert.InRange(Math.Abs(LeverFixture.Speed(world,lever)),0,1e-7);
                var stopped = LeverFixture.Angle(world,lever);
                LeverFixture.Push(world,lever,-1);
                world.Step();
                Assert.True(LeverFixture.Angle(world,lever) < stopped);
            }
        }
        finally { world.Free(); }
    }
}
