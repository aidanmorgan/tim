using Godot;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class ImpactLeverTests(HeadlessFixture godot, ITestOutputHelper output)
{
    public enum Route { Transfer, MissedDepth, PivotHit }
    private enum Role { Lever, Driver, Payload, LeftLoad, RightLoad }
    private static string Id(Role role) => role switch
    {
        Role.Lever => "lever", Role.Driver => "driver", Role.Payload => "payload",
        Role.LeftLoad => "left_load", Role.RightLoad => "right_load",
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };
    private static PartSpec Spec(Role role, Vector3 position, Vector3 rotation = default) => new()
    {
        Id = Id(role), Kind = role switch
        {
            Role.Lever => ImpactLeverPart.CatalogId, Role.Driver => "bowling",
            Role.Payload or Role.LeftLoad or Role.RightLoad => "ball",
            _ => throw new ArgumentOutOfRangeException(nameof(role))
        },
        Position = [position.X, position.Y, position.Z], Rotation = [rotation.X, rotation.Y, rotation.Z]
    };

    [Fact]
    public void ParameterEnumBoundaryRejectsUndefinedValues()
    {
        Assert.Equal("beam_mass",PartParameterName.Of(ImpactLeverParameter.BeamMass));
        Assert.Equal("initial_angle",PartParameterName.Of(ImpactLeverParameter.InitialAngle));
        Assert.Throws<ArgumentOutOfRangeException>(() => PartParameterName.Of((ImpactLeverParameter)99));
    }

    [Fact]
    public void EqualOppositeRestingLoadsRemainBalanced()
    {
        var world = new MachineWorld { Gravity = 9.81f, Pressure = 0 };
        godot.Tree.Root.AddChild(world);
        try
        {
            var lever = (ImpactLeverPart)world.AddPart(Spec(Role.Lever,new(0,3,0)));
            var left = world.AddPart(Spec(Role.LeftLoad,new(-1,3.46f,0)));
            var right = world.AddPart(Spec(Role.RightLoad,new(1,3.46f,0)));
            world.Start();
            for (var tick=0;tick<1200;tick++)
            {
                world.Step();
                Assert.InRange(Math.Abs(lever.Beam.Joint.Angle),0,.01);
                Assert.InRange(Math.Abs(left.Position.Y-right.Position.Y),0,.01f);
                Assert.InRange(left.Position.Y,3.45f,3.47f);
                Assert.InRange(right.Position.Y,3.45f,3.47f);
            }
            Assert.InRange(Math.Abs(lever.Beam.Joint.AngularVelocity),0,.02);
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(0,0,0)]
    [InlineData(15,30,0)]
    [InlineData(90,0,0)]
    [InlineData(0,0,90)]
    public void SharedClockRespectsAuthoredThreeDimensionalOrientation(float x,float y,float z)
    {
        var world = new MachineWorld { Gravity = 0, Pressure = 0 };
        godot.Tree.Root.AddChild(world);
        try
        {
            var lever = (ImpactLeverPart)world.AddPart(Spec(Role.Lever,new(0,5,0),new(x,y,z)));
            var ball = world.AddPart(Spec(Role.Driver,lever.Transform * new Vector3(-1.2f,1.3f,0)));
            world.Start();
            ball.Velocity = lever.Basis * Vector3.Down * 5;
            var initialEnergy = .5 * ball.Mass * ball.Velocity.LengthSquared();
            var greatestAngle = 0d;
            for (var tick=0;tick<240;tick++)
            {
                world.Step();
                greatestAngle = Math.Max(greatestAngle,Math.Abs(lever.Beam.Joint.Angle));
                Assert.True(.5 * ball.Mass * ball.Velocity.LengthSquared() + lever.Beam.Joint.Energy <= initialEnergy * 1.01);
            }
            Assert.True(greatestAngle > .05);
            Assert.True(lever.ImpactCount > 0);
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(Route.Transfer)]
    [InlineData(Route.MissedDepth)]
    [InlineData(Route.PivotHit)]
    public void FallingMassTransfersEnergyThroughTheBeamOnlyWhenAligned(Route route)
    {
        var world = new MachineWorld { Gravity = 9.81f, Pressure = 0 };
        godot.Tree.Root.AddChild(world);
        try
        {
            var lever = (ImpactLeverPart)world.AddPart(Spec(Role.Lever,new(0,3,0)));
            var driver = world.AddPart(Spec(Role.Driver,new(route == Route.PivotHit ? 0 : -1.2f,6,route == Route.MissedDepth ? 2 : 0)));
            var payload = world.AddPart(Spec(Role.Payload,new(1.2f,3.46f,0)));
            var saved = JsonSerializer.Serialize(world.Snapshot(), MachineJson.Default.MachineData);
            world.Start();
            double Energy() => lever.Beam.Joint.Energy + world.Bodies.Sum(b =>
                b.Mass * (double)world.Gravity * b.Position.Y + .5 * b.Mass * b.Velocity.LengthSquared());
            var initialEnergy = Energy();
            var peakHeight = payload.Position.Y;
            var peakUp = 0f;
            var maximumAngle = 0d;
            for (var tick = 0; tick < 480; tick++)
            {
                world.Step();
                Assert.True(driver.Position.IsFinite() && payload.Position.IsFinite());
                Assert.True(driver.Velocity.IsFinite() && payload.Velocity.IsFinite());
                Assert.InRange(lever.Beam.Joint.Angle, -ImpactLeverPart.LimitAngle, ImpactLeverPart.LimitAngle);
                Assert.True(Energy() <= initialEnergy * 1.01, $"Energy={Energy()}, initial={initialEnergy}, tick={tick}");
                peakHeight = Math.Max(peakHeight,payload.Position.Y);
                if (payload.Position.Y > 3.5f) peakUp = Math.Max(peakUp,payload.Velocity.Y);
                maximumAngle = Math.Max(maximumAngle,Math.Abs(lever.Beam.Joint.Angle));
            }
            output.WriteLine($"route={route}, peakHeight={peakHeight}, peakUp={peakUp}, maximumAngle={maximumAngle}, driver={driver.Position}, payload={payload.Position}");
            if (route == Route.Transfer) { Assert.True(peakHeight > 4); Assert.True(peakUp > 2); }
            else Assert.True(peakHeight < 3.6f);
            var signature = world.StateSignature();
            world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            var restored = Assert.Single(world.Parts.OfType<ImpactLeverPart>());
            Assert.Equal(0,restored.Beam.Joint.Angle);
            Assert.Equal(0,restored.Beam.Joint.AngularVelocity);
            world.Start();
            for (var tick=0;tick<480;tick++) world.Step();
            Assert.Equal(signature,world.StateSignature());
        }
        finally { world.Free(); }
    }
}
