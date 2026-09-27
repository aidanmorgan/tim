using Godot;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class ImpactLeverTubeObstructionTests(HeadlessFixture godot)
{
    public enum Route { Shell, DepthMiss, Bore }
    private enum Role { Lever, Pipe, Driver }
    private static PartSpec Spec(Role role, Vector3 at) => new()
    {
        Id = role switch { Role.Lever => "lever", Role.Pipe => "pipe", Role.Driver => "driver", _ => throw new ArgumentOutOfRangeException(nameof(role)) },
        Kind = role switch { Role.Lever => ImpactLeverPart.CatalogId, Role.Pipe => "pipe", Role.Driver => "bowling", _ => throw new ArgumentOutOfRangeException(nameof(role)) },
        Position = [at.X,at.Y,at.Z]
    };

    [Theory]
    [InlineData(Route.Shell)]
    [InlineData(Route.Bore)]
    public void FallingLoadStopsAtEitherSideOfTheShellAndResetIsExact(Route route)
    {
        var world = new MachineWorld { Gravity = 9.81f, Pressure = 0 };
        godot.Tree.Root.AddChild(world);
        try
        {
            var lever = (ImpactLeverPart)world.AddPart(Spec(Role.Lever,new(0,3,0)));
            var pipe = (PipePart)world.AddPart(Spec(Role.Pipe,route == Route.Bore ? new(1.2f,3,0) : new(1.5f,4.1f,0)));
            pipe.SetDimensions(new(1,1.3f,1.3f));
            world.AddPart(Spec(Role.Driver,new(-1.2f,6,0)));
            var before = JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            double maximum = 0;
            for (var tick = 0; tick < 600; tick++)
            {
                world.Step();
                maximum = Math.Max(maximum,lever.Beam.Joint.Angle);
                Assert.InRange(world.MaximumFlightIterationsThisStep,1,100);
            }
            Assert.InRange(maximum,.1,.25);
            Assert.Equal(0,lever.Beam.Joint.AngularVelocity);
            world.Restore();
            Assert.Equal(before,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(Route.Shell)]
    [InlineData(Route.DepthMiss)]
    [InlineData(Route.Bore)]
    public void PipeShellBlocksButItsBoreAndMissedDepthRemainClear(Route route)
    {
        var world = new MachineWorld { Gravity = 0, Pressure = 0 };
        godot.Tree.Root.AddChild(world);
        try
        {
            var lever = (ImpactLeverPart)world.AddPart(Spec(Role.Lever,new(0,3,0)));
            var pipe = (PipePart)world.AddPart(Spec(Role.Pipe,route == Route.Bore ? new(1.2f,3,0) :
                new(1.5f,4.1f,route == Route.DepthMiss ? 2 : 0)));
            pipe.SetDimensions(new(1,1.3f,1.3f));
            world.Start();
            lever.Beam.Joint.ApplyAngularImpulse(lever.Beam.Joint.Inertia * 3);
            if (route == Route.Bore)
            {
                world.Step();
                Assert.True(lever.Beam.Joint.Angle > 0);
                Assert.True(lever.Beam.Joint.AngularVelocity > 0);
                for (var tick = 0; tick < 120; tick++) world.Step();
                Assert.InRange(lever.Beam.Joint.Angle,.12,.14);
                Assert.Equal(0,lever.Beam.Joint.AngularVelocity);
            }
            else
            {
                for (var tick = 0; tick < 120; tick++) world.Step();
                if (route == Route.DepthMiss) Assert.Equal(ImpactLeverPart.LimitAngle,lever.Beam.Joint.Angle);
                else
                {
                    Assert.InRange(lever.Beam.Joint.Angle,.1,.25);
                    Assert.Equal(0,lever.Beam.Joint.AngularVelocity);
                }
            }
        }
        finally { world.Free(); }
    }
}
