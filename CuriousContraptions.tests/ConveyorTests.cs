using System.Collections.Immutable;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;
using Xunit;

namespace CuriousContraptions.Tests;

/// <summary>
/// Story 11.1b (CAT-019): Conveyor package tests.
/// Tests driven surface transport, stopped/unpowered belt, mid-run power cut deceleration,
/// reversed drive, energy conservation, multiple concurrent loads, rigid side contacts,
/// rotated placements, exact Reset/replay, save/load roundtrips, and atomic parameter/connection refusals.
/// </summary>
public sealed class ConveyorTests
{
    private static readonly PartId Motor = new("motor");
    private static readonly PartId Conveyor = new("conveyor");
    private static readonly PartId Ball = new("ball");

    private const float ConveyorY = 4f;
    private const float BallRadius = 0.34f;
    private const float CarrierHalfHeight = 0.12f;
    private const float BallRestY = ConveyorY + CarrierHalfHeight + BallRadius; // 4.46f

    private const int TicksPerSecond = 120;

    private static WorkshopGenericInstance MotorPart(ulong id = 1, float speed = 6f, float torque = 20f,
        Vector3? position = null, Quaternion? rotation = null)
    {
        var p = position ?? new Vector3(-2f, ConveyorY, 0f);
        var q = rotation ?? Quaternion.Identity;
        return WorkshopInput.Generic(RuntimeTestServices.Bundle, Motor, new(id), p.X, p.Y, p.Z, q.X, q.Y, q.Z, q.W,
            new([new(new("speed"), speed), new(new("torque"), torque)]));
    }

    private static WorkshopGenericInstance ConveyorPart(ulong id = 2, float length = 3f, float width = 1.2f,
        float surfacePerRadian = 0.6666667f, Vector3? position = null, Quaternion? rotation = null)
    {
        var p = position ?? new Vector3(0f, ConveyorY, 0f);
        var q = rotation ?? Quaternion.Identity;
        return WorkshopInput.Generic(RuntimeTestServices.Bundle, Conveyor, new(id), p.X, p.Y, p.Z, q.X, q.Y, q.Z, q.W,
            new([new(new("length"), length), new(new("width"), width), new(new("surface_per_radian"), surfacePerRadian)]));
    }

    private static WorkshopGenericInstance BallPart(ulong id = 3, float x = -0.5f, float y = BallRestY, float z = 0f) =>
        WorkshopInput.Generic(RuntimeTestServices.Bundle, Ball, new(id), x, y, z, 0, 0, 0, 1);

    private static WorkshopConnection PowerWire(GpuBodyId battery, GpuBodyId motor) =>
        new(battery, PortId.Supply, motor, PortId.PowerIn, WorkshopConnectionDomain.Electrical);

    private static WorkshopConnection MechWire(GpuBodyId motor, GpuBodyId conveyor) =>
        new(motor, PortId.Drive, conveyor, PortId.DriveIn, WorkshopConnectionDomain.Mechanical);

    private static GpuBodyId Roller(WorkshopConstruction construction, GpuBodyId conveyor) =>
        construction.Instances.Identities[conveyor].Hinges[0].Body;

    private static Vector3 Origin(PhysicsBodyRead body) =>
        new((body.Body.Cell.X + body.Body.Local.X) / 16f,
            (body.Body.Cell.Y + body.Body.Local.Y) / 16f,
            (body.Body.Cell.Z + body.Body.Local.Z) / 16f);

    private static Vector3 Velocity(PhysicsBodyRead body) =>
        new(body.Body.Velocity.X, body.Body.Velocity.Y, body.Body.Velocity.Z);

    private static Vector3 Angular(PhysicsBodyRead body) =>
        new(body.AngularVelocity.X, body.AngularVelocity.Y, body.AngularVelocity.Z);

    [Fact]
    public async Task PoweredTransport_AcceleratesBallAlongPositiveX()
    {
        var battery = PlacedParts.Battery(new(1), -5, ConveyorY, 0, enabled: ElectricalEnable.Enabled);
        var motor = MotorPart(2, speed: 6f, torque: 20f);
        var conveyor = ConveyorPart(3);
        var ball = BallPart(4, x: -0.5f);

        var construction = new WorkshopConstruction(
            new(2),
            WorkshopCadenceSettings.Default(),
            new([battery, motor, conveyor, ball]),
            Connections: new([PowerWire(battery.Id, motor.Id), MechWire(motor.Id, conveyor.Id)]));

        await using var harness = await WorkshopIntegrationHarness.Running(construction);
        var rollerId = Roller(construction, conveyor.Id);

        await harness.Advance(TicksPerSecond);

        Assert.True(harness.Committed.Bodies.TryGet(ball.Id, out var ballBody));
        Assert.True(harness.Committed.Bodies.TryGet(rollerId, out var rollerBody));

        var pos = Origin(ballBody);
        var rollerW = Angular(rollerBody);

        Assert.True(rollerW.Z > 1f, $"Conveyor roller should be spinning under motor drive, got {rollerW.Z} rad/s.");
        Assert.True(pos.X - (-0.5f) > 0.1f, $"Basketball should have accelerated along +X, got x={pos.X}.");
        Assert.InRange(pos.Y, ConveyorY + CarrierHalfHeight + BallRadius - 0.05f, ConveyorY + CarrierHalfHeight + BallRadius + 0.1f);
    }

    [Theory]
    [InlineData(true)]  // Disconnected mechanical wire
    [InlineData(false)] // Disabled battery
    public async Task UnpoweredBelt_BallStaysStationaryWithNoWork(bool disconnectedWire)
    {
        var battery = PlacedParts.Battery(new(1), -5, ConveyorY, 0,
            enabled: disconnectedWire ? ElectricalEnable.Enabled : ElectricalEnable.Disabled);
        var motor = MotorPart(2, speed: 6f, torque: 20f);
        var conveyor = ConveyorPart(3);
        var ball = BallPart(4, x: -0.5f);

        IReadOnlyList<WorkshopConnection> wires = disconnectedWire
            ? [PowerWire(battery.Id, motor.Id)]
            : [PowerWire(battery.Id, motor.Id), MechWire(motor.Id, conveyor.Id)];

        var construction = new WorkshopConstruction(
            new(2),
            WorkshopCadenceSettings.Default(),
            new([battery, motor, conveyor, ball]),
            Connections: new([.. wires]));

        await using var harness = await WorkshopIntegrationHarness.Running(construction);
        var rollerId = Roller(construction, conveyor.Id);

        await harness.Advance(TicksPerSecond);

        Assert.True(harness.Committed.Bodies.TryGet(ball.Id, out var ballBody));
        Assert.True(harness.Committed.Bodies.TryGet(rollerId, out var rollerBody));

        var pos = Origin(ballBody);
        var vel = Velocity(ballBody);
        var rollerW = Angular(rollerBody);

        Assert.InRange(rollerW.Z, -0.05f, 0.05f);
        Assert.InRange(pos.X - (-0.5f), -0.02f, 0.02f);
        Assert.True(vel.Length() < 0.05f, $"Ball should remain stationary, velocity is {vel.Length()} m/s.");
    }

    [Fact]
    public async Task MidRunPowerCut_DeceleratesToStopUnderFriction()
    {
        var battery = PlacedParts.Battery(new(1), -5, ConveyorY, 0, enabled: ElectricalEnable.Enabled);
        var motor = MotorPart(2, speed: 6f, torque: 20f);
        var conveyor = ConveyorPart(3);
        var ball = BallPart(4, x: -0.5f);

        var construction = new WorkshopConstruction(
            new(2),
            WorkshopCadenceSettings.Default(),
            new([battery, motor, conveyor, ball]),
            Connections: new([PowerWire(battery.Id, motor.Id), MechWire(motor.Id, conveyor.Id)]));

        await using var harness = await WorkshopIntegrationHarness.Running(construction);
        var rollerId = Roller(construction, conveyor.Id);

        // Advance 60 ticks while powered
        await harness.Advance(60);
        Assert.True(harness.Committed.Bodies.TryGet(rollerId, out var runningRoller));
        var runningSpin = Angular(runningRoller).Z;
        Assert.True(runningSpin > 1f, "Roller should be spinning before power cut.");

        // Cut electrical power to motor via battery SourceEnable
        await harness.Require(WorkshopCommandKind.ConfigureRunControl,
            runControl: harness.RunControl(battery.Id, RunControlLane.SourceEnable, RunControlValue.Of(ElectricalEnable.Disabled)));

        // Advance 240 ticks during passive deceleration under bearing friction
        await harness.Advance(240);

        Assert.True(harness.Committed.Bodies.TryGet(rollerId, out var finalRoller));
        Assert.True(harness.Committed.Bodies.TryGet(ball.Id, out var finalBall));

        var finalSpin = Angular(finalRoller).Z;
        Assert.True(MathF.Abs(finalSpin) <= 0.05f, $"Roller should decelerate to rest under bearing friction; got {finalSpin}.");
        var ballVel = Velocity(finalBall);
        Assert.True(ballVel.Length() <= 0.05f, $"Ball should coast to rest under friction without kicks; got {ballVel.Length()}.");
    }

    [Fact]
    public async Task ReversedDrive_SurfaceRunsAlongNegativeX()
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());

        var reversedPart = new PartId("reversed_conveyor");
        var conveyorEntry = TestBundle.Entry(Conveyor);
        var reversedPorts = conveyorEntry.Ports.Select(p => p.Id == PortId.DriveIn
            ? p with { CoordinatePerRadian = -p.CoordinatePerRadian } : p).ToImmutableArray();
        var reversedEntry = conveyorEntry with
        {
            Metadata = conveyorEntry.Metadata with { Id = reversedPart.Value },
            Ports = reversedPorts
        };
        using var physicsScope = DeclaredPhysics.Scoped([reversedEntry]);
        using var portsScope = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(reversedPart, reversedPorts)]));

        var battery = PlacedParts.Battery(new(1), -5, ConveyorY, 0, enabled: ElectricalEnable.Enabled);
        var motor = MotorPart(2, speed: 6f, torque: 20f);
        var conveyor = WorkshopInput.Generic(RuntimeTestServices.Bundle, reversedPart, new(3), 0, ConveyorY, 0, 0, 0, 0, 1,
            new([new(new("length"), 3f), new(new("width"), 1.2f), new(new("surface_per_radian"), 0.6666667f)]));
        var ball = BallPart(4, x: 0.5f);

        var construction = new WorkshopConstruction(
            new(2),
            WorkshopCadenceSettings.Default(),
            new([battery, motor, conveyor, ball]),
            Connections: new([PowerWire(battery.Id, motor.Id), MechWire(motor.Id, conveyor.Id)]));

        await harness.Require(WorkshopCommandKind.Construct, construction);
        await harness.Run();

        var rollerId = Roller(construction, conveyor.Id);

        await harness.Advance(TicksPerSecond);

        Assert.True(harness.Committed.Bodies.TryGet(ball.Id, out var ballBody));
        Assert.True(harness.Committed.Bodies.TryGet(rollerId, out var rollerBody));

        var pos = Origin(ballBody);
        var rollerW = Angular(rollerBody);

        Assert.True(rollerW.Z < -1f, $"Reversed roller spin should be negative, got {rollerW.Z}.");
        Assert.True(pos.X - 0.5f < -0.1f, $"Basketball should accelerate along -X, got x={pos.X}.");
    }

    [Fact]
    public async Task UnloadedBelt_ConservesEnergy()
    {
        var unpoweredBattery = PlacedParts.Battery(new(1), -5, ConveyorY, 0, enabled: ElectricalEnable.Disabled);
        var motor = MotorPart(2, speed: 6f, torque: 20f);
        var conveyor = ConveyorPart(3);

        var unpoweredConstruction = new WorkshopConstruction(
            new(2),
            WorkshopCadenceSettings.Default(),
            new([unpoweredBattery, motor, conveyor]),
            Connections: new([PowerWire(unpoweredBattery.Id, motor.Id), MechWire(motor.Id, conveyor.Id)]));

        await using (var unpowered = await WorkshopIntegrationHarness.Running(unpoweredConstruction))
        {
            await unpowered.Advance(60);
            Assert.True(unpowered.Committed.Bodies.TryGet(Roller(unpoweredConstruction, conveyor.Id), out var restRoller));
            Assert.True(Velocity(restRoller).Length() <= 1e-4f, $"Unpowered roller linear residual exceeded rest envelope: {Velocity(restRoller).Length()}.");
            Assert.True(Angular(restRoller).Length() <= 1e-4f, $"Unpowered roller angular residual exceeded rest envelope: {Angular(restRoller).Length()}.");
            Assert.Equal(0f, PhysicsGpuAbi.ReadRevoluteState(unpowered.Device.CommittedWorld.ToArray(), 0).Work);
            Assert.Equal(0f, PhysicsGpuAbi.ReadRevoluteState(unpowered.Device.CommittedWorld.ToArray(), 1).Work);
        }

        var battery = PlacedParts.Battery(new(1), -5, ConveyorY, 0, enabled: ElectricalEnable.Enabled);
        var construction = new WorkshopConstruction(
            new(2),
            WorkshopCadenceSettings.Default(),
            new([battery, motor, conveyor]),
            Connections: new([PowerWire(battery.Id, motor.Id), MechWire(motor.Id, conveyor.Id)]));

        await using var harness = await WorkshopIntegrationHarness.Running(construction);
        var rollerId = Roller(construction, conveyor.Id);

        await harness.Advance(60);

        Assert.True(harness.Committed.Bodies.TryGet(rollerId, out var rollerBody));

        const float rollerMass = 0.5f;
        const float rollerRadius = 0.16f;
        const float rollerInertia = 0.5f * rollerMass * rollerRadius * rollerRadius;

        var v = Velocity(rollerBody);
        var w = Angular(rollerBody);
        var kinetic = 0.5f * rollerMass * v.LengthSquared() + 0.5f * rollerInertia * w.LengthSquared();

        var motorState = PhysicsGpuAbi.ReadRevoluteState(harness.Device.CommittedWorld.ToArray(), 0);
        var rollerState = PhysicsGpuAbi.ReadRevoluteState(harness.Device.CommittedWorld.ToArray(), 1);
        Assert.Equal(0f, rollerState.Work);
        Assert.True(motorState.Work > 0f, "Powered motor must record positive drive work.");
        const float f32Allowance = 1e-4f;
        Assert.True(kinetic <= motorState.Work + f32Allowance,
            $"Roller kinetic energy ({kinetic:F6} J) must be bounded by input drive work ({motorState.Work:F6} J + {f32Allowance}).");
    }

    [Fact]
    public async Task MultipleConcurrentLoads_TransportBothSimultaneously()
    {
        var battery = PlacedParts.Battery(new(1), -5, ConveyorY, 0, enabled: ElectricalEnable.Enabled);
        var motor = MotorPart(2, speed: 6f, torque: 20f);
        var conveyor = ConveyorPart(3);
        var ball1 = BallPart(4, x: -0.8f);
        var ball2 = BallPart(5, x: -0.2f);

        var construction = new WorkshopConstruction(
            new(2),
            WorkshopCadenceSettings.Default(),
            new([battery, motor, conveyor, ball1, ball2]),
            Connections: new([PowerWire(battery.Id, motor.Id), MechWire(motor.Id, conveyor.Id)]));

        await using var harness = await WorkshopIntegrationHarness.Running(construction);

        await harness.Advance(TicksPerSecond);

        Assert.True(harness.Committed.Bodies.TryGet(ball1.Id, out var body1));
        Assert.True(harness.Committed.Bodies.TryGet(ball2.Id, out var body2));

        var pos1 = Origin(body1);
        var pos2 = Origin(body2);

        Assert.True(pos1.X - (-0.8f) > 0.1f, $"Ball 1 should have moved forward, got {pos1.X}.");
        Assert.True(pos2.X - (-0.2f) > 0.1f, $"Ball 2 should have moved forward, got {pos2.X}.");
        Assert.InRange(pos1.Y, ConveyorY + CarrierHalfHeight + BallRadius - 0.05f, ConveyorY + CarrierHalfHeight + BallRadius + 0.1f);
        Assert.InRange(pos2.Y, ConveyorY + CarrierHalfHeight + BallRadius - 0.05f, ConveyorY + CarrierHalfHeight + BallRadius + 0.1f);
    }

    [Fact]
    public async Task OffBeltMiss_SideContactRemainsOrdinaryRigid()
    {
        var battery = PlacedParts.Battery(new(1), -5, ConveyorY, 0, enabled: ElectricalEnable.Enabled);
        var motor = MotorPart(2, speed: 6f, torque: 20f);
        var conveyor = ConveyorPart(3);
        // Conveyor width is 1.2 (half-width 0.6). Dropping at z = 0.85 glances off the outer side edge without resting on the belt.
        var ball = BallPart(4, x: 0f, y: ConveyorY + 0.5f, z: 0.85f);

        var construction = new WorkshopConstruction(
            new(2),
            WorkshopCadenceSettings.Default(),
            new([battery, motor, conveyor, ball]),
            Connections: new([PowerWire(battery.Id, motor.Id), MechWire(motor.Id, conveyor.Id)]));

        await using var harness = await WorkshopIntegrationHarness.Running(construction);

        await harness.Advance(60);

        Assert.True(harness.Committed.Bodies.TryGet(ball.Id, out var ballBody));
        var pos = Origin(ballBody);
        var vel = Velocity(ballBody);

        Assert.True(pos.Y < ConveyorY, "Ball dropped outside belt should fall past conveyor under gravity.");
        Assert.True(pos.Z > 0.85f, $"Ball hitting outer conveyor side should deflect outward in +Z; got z={pos.Z}.");
        Assert.InRange(vel.X, -0.05f, 0.05f);
    }

    [Fact]
    public async Task RotatedConveyor_AlignsTransportAlongRotatedLocalX()
    {
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2f);
        var battery = PlacedParts.Battery(new(1), 0, ConveyorY, -5, enabled: ElectricalEnable.Enabled);
        var motor = MotorPart(2, speed: 6f, torque: 20f, position: new Vector3(0, ConveyorY, -2), rotation: rotation);
        var conveyor = ConveyorPart(3, rotation: rotation);
        // Local +X rotated 90° about Y becomes world -Z.
        // Authored at local -0.5 along local X, which is world z = 0.5.
        var ball = BallPart(4, x: 0f, y: BallRestY, z: 0.5f);

        var construction = new WorkshopConstruction(
            new(2),
            WorkshopCadenceSettings.Default(),
            new([battery, motor, conveyor, ball]),
            Connections: new([PowerWire(battery.Id, motor.Id), MechWire(motor.Id, conveyor.Id)]));

        await using var harness = await WorkshopIntegrationHarness.Running(construction);

        await harness.Advance(TicksPerSecond);

        Assert.True(harness.Committed.Bodies.TryGet(ball.Id, out var ballBody));
        var pos = Origin(ballBody);

        Assert.True(pos.Z - 0.5f < -0.1f, $"Rotated conveyor should transport ball along world -Z (local +X), got z={pos.Z}.");
        Assert.InRange(pos.X, -0.05f, 0.05f);
    }

    [Fact]
    public async Task ResetAndReplay_RestoresExactAuthoredState()
    {
        var battery = PlacedParts.Battery(new(1), -5, ConveyorY, 0, enabled: ElectricalEnable.Enabled);
        var motor = MotorPart(2, speed: 6f, torque: 20f);
        var conveyor = ConveyorPart(3);
        var ball = BallPart(4, x: -0.5f);

        var construction = new WorkshopConstruction(
            new(2),
            WorkshopCadenceSettings.Default(),
            new([battery, motor, conveyor, ball]),
            Connections: new([PowerWire(battery.Id, motor.Id), MechWire(motor.Id, conveyor.Id)]));

        await using var harness = await WorkshopIntegrationHarness.Building(construction);
        var rollerId = Roller(construction, conveyor.Id);

        Assert.True(harness.Committed.Bodies.TryGet(ball.Id, out var builtBall));
        Assert.True(harness.Committed.Bodies.TryGet(rollerId, out var builtRoller));
        var initialBallPos = Origin(builtBall);

        await harness.Run();
        await harness.Advance(60);

        Assert.True(harness.Committed.Bodies.TryGet(ball.Id, out var runningBall));
        Assert.True(Origin(runningBall).X - initialBallPos.X > 0.05f, "Ball moved during run.");

        await harness.Reset();

        Assert.True(harness.Committed.Bodies.TryGet(ball.Id, out var resetBall));
        Assert.True(harness.Committed.Bodies.TryGet(rollerId, out var resetRoller));

        Assert.InRange(Origin(resetBall).X - initialBallPos.X, -0.001f, 0.001f);
        Assert.InRange(Origin(resetBall).Y - initialBallPos.Y, -0.001f, 0.001f);
        Assert.True(Velocity(resetBall).Length() < 0.001f, "Reset ball velocity must be zero.");
        Assert.True(Angular(resetRoller).Length() < 0.001f, "Reset roller angular velocity must be zero.");
        var resetMotorRevolute = PhysicsGpuAbi.ReadRevoluteState(harness.Device.CommittedWorld.ToArray(), 0);
        var resetRollerRevolute = PhysicsGpuAbi.ReadRevoluteState(harness.Device.CommittedWorld.ToArray(), 1);
        Assert.True(MathF.Abs(resetMotorRevolute.Travel) < 0.001f, $"Reset motor angle must restore to zero, got {resetMotorRevolute.Travel}.");
        Assert.True(MathF.Abs(resetRollerRevolute.Travel) < 0.001f, $"Reset roller angle must restore to zero, got {resetRollerRevolute.Travel}.");

        // Replay run
        await harness.Run();
        await harness.Advance(60);

        Assert.True(harness.Committed.Bodies.TryGet(ball.Id, out var replayBall));
        Assert.Equal(Origin(runningBall), Origin(replayBall));
        Assert.Equal(Velocity(runningBall), Velocity(replayBall));
    }

    [Fact]
    public async Task ChainedConveyors_DriveRelaysRotationToDownstreamConveyor()
    {
        var battery = PlacedParts.Battery(new(1), -8, ConveyorY, 0, enabled: ElectricalEnable.Enabled);
        var motor = MotorPart(2, speed: 6f, torque: 20f, position: new Vector3(-5, ConveyorY, 0));
        var conveyor1 = ConveyorPart(3, position: new Vector3(-2f, ConveyorY, 0f));
        var conveyor2 = ConveyorPart(4, position: new Vector3(2f, ConveyorY, 0f));
        var ball = BallPart(5, x: 1.5f);

        var construction = new WorkshopConstruction(
            new(2),
            WorkshopCadenceSettings.Default(),
            new([battery, motor, conveyor1, conveyor2, ball]),
            Connections: new([
                PowerWire(battery.Id, motor.Id),
                MechWire(motor.Id, conveyor1.Id),
                new WorkshopConnection(conveyor1.Id, PortId.Drive, conveyor2.Id, PortId.DriveIn, WorkshopConnectionDomain.Mechanical)
            ]));

        await using var harness = await WorkshopIntegrationHarness.Running(construction);
        var roller2Id = Roller(construction, conveyor2.Id);

        await harness.Advance(TicksPerSecond);

        Assert.True(harness.Committed.Bodies.TryGet(roller2Id, out var roller2Body));
        Assert.True(harness.Committed.Bodies.TryGet(ball.Id, out var ballBody));

        var roller2W = Angular(roller2Body);
        var ballPos = Origin(ballBody);

        Assert.True(roller2W.Z > 1f, $"Downstream conveyor roller should spin, got {roller2W.Z}.");
        Assert.True(ballPos.X - 1.5f > 0.1f, $"Payload on downstream conveyor should advance along +X, got {ballPos.X}.");
    }

    [Fact]
    public void SaveLoadRoundtrip_PreservesParametersAndMechanicalConnections()
    {
        var motor = MotorPart(1, speed: 8f, torque: 30f);
        var conveyor = ConveyorPart(2, length: 4.5f, width: 1.8f, surfacePerRadian: 1.2f);

        var construction = new WorkshopConstruction(
            new(2),
            WorkshopCadenceSettings.Default(),
            new([motor, conveyor]),
            Connections: new([MechWire(motor.Id, conveyor.Id)]));

        var saved = new WorkshopSavedConstruction(construction, new(10));
        var bytes = WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, saved);
        var decoded = WorkshopSaveCodec.Decode(
            RuntimeTestServices.Core.Codecs.Commands,
            RuntimeTestServices.Core.Supply,
            RuntimeTestServices.Bundle,
            SaveFixture.Elements,
            bytes);

        Assert.Equal(construction.Instances, decoded.Construction.Instances);
        Assert.Equal(construction.Connections, decoded.Construction.Connections);

        var loadedConveyor = Assert.IsType<WorkshopGenericInstance>(decoded.Construction.Instances[1]);
        Assert.Equal(4.5f, loadedConveyor.Parameters[new("length")]);
        Assert.Equal(1.8f, loadedConveyor.Parameters[new("width")]);
        Assert.Equal(1.2f, loadedConveyor.Parameters[new("surface_per_radian")]);

        var loadedConn = Assert.Single(decoded.Construction.Connections);
        Assert.Equal(motor.Id, loadedConn.Source);
        Assert.Equal(PortId.Drive, loadedConn.Output);
        Assert.Equal(conveyor.Id, loadedConn.Target);
        Assert.Equal(PortId.DriveIn, loadedConn.Input);
        Assert.Equal(WorkshopConnectionDomain.Mechanical, loadedConn.Domain);
    }

    public static TheoryData<string, float> RefusedParameters() => new()
    {
        { "length", -1f }, { "length", 0f }, { "length", 0.5f }, { "length", 12f }, { "length", float.NegativeInfinity }, { "length", float.NaN }, { "length", float.PositiveInfinity },
        { "width", -1f }, { "width", 0f }, { "width", 0.2f }, { "width", 5f }, { "width", float.NegativeInfinity }, { "width", float.NaN }, { "width", float.PositiveInfinity },
        { "surface_per_radian", -1f }, { "surface_per_radian", 0f }, { "surface_per_radian", 0.05f }, { "surface_per_radian", 6f }, { "surface_per_radian", float.NegativeInfinity }, { "surface_per_radian", float.NaN }, { "surface_per_radian", float.PositiveInfinity }
    };

    [Theory]
    [MemberData(nameof(RefusedParameters))]
    public void InvalidParameters_RefusedAtomically(string key, float value)
    {
        var len = key == "length" ? value : 3.0f;
        var wid = key == "width" ? value : 1.2f;
        var spr = key == "surface_per_radian" ? value : 0.6666667f;

        var parameters = new InstanceParameterCollection([
            new(new("length"), len),
            new(new("width"), wid),
            new(new("surface_per_radian"), spr)
        ]);

        Assert.Throws<ArgumentException>(() => parameters.Validate(Conveyor));
        Assert.Throws<ArgumentException>(() => ConveyorPart(length: len, width: wid, surfacePerRadian: spr));
    }

    [Theory]
    [InlineData("powered")]
    [InlineData("speed")]
    [InlineData("traction")]
    public void RetiredParameters_RefusedAtomically(string retiredKey)
    {
        var parameters = new InstanceParameterCollection([
            new(new("length"), 3.0f),
            new(new("width"), 1.2f),
            new(new("surface_per_radian"), 0.6666667f),
            new(new(retiredKey), 1.0f)
        ]);

        Assert.Throws<ArgumentException>(() => parameters.Validate(Conveyor));
    }

    [Fact]
    public void InvalidMechanicalConnections_RefusedAtomically()
    {
        var motor1 = MotorPart(1);
        var motor2 = MotorPart(2);
        var conveyor = ConveyorPart(3);

        // 1. Duplicate drive feeds targeting the same driven DriveIn (fan-in rejected)
        var feed1 = new WorkshopConnection(motor1.Id, PortId.Drive, conveyor.Id, PortId.DriveIn, WorkshopConnectionDomain.Mechanical);
        var feed2 = new WorkshopConnection(motor2.Id, PortId.Drive, conveyor.Id, PortId.DriveIn, WorkshopConnectionDomain.Mechanical);
        var exDuplicate = Assert.Throws<WorkshopEditRejectedException>(() =>
            new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new([motor1, motor2, conveyor]),
                Connections: new([feed1, feed2])).Validate(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle));
        Assert.Equal(WorkshopEditRejection.InvalidConnection, exDuplicate.Reason);

        // 2. Cross domain: Electrical wire targeting DriveIn
        var crossDomain = new WorkshopConnection(motor1.Id, PortId.Supply, conveyor.Id, PortId.DriveIn, WorkshopConnectionDomain.Electrical);
        var exDomain = Assert.Throws<WorkshopEditRejectedException>(() =>
            new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new([motor1, conveyor]),
                Connections: new([crossDomain])).Validate(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle));
        Assert.Equal(WorkshopEditRejection.InvalidConnection, exDomain.Reason);

        // 3. Self connection: Conveyor Drive to Conveyor DriveIn
        var selfConn = new WorkshopConnection(conveyor.Id, PortId.Drive, conveyor.Id, PortId.DriveIn, WorkshopConnectionDomain.Mechanical);
        var exSelf = Assert.Throws<WorkshopEditRejectedException>(() =>
            new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new([conveyor]),
                Connections: new([selfConn])).Validate(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle));
        Assert.Equal(WorkshopEditRejection.InvalidConnection, exSelf.Reason);

        // 4. Direction mismatch: Output to Output (Conveyor Drive to Motor Drive)
        var outToOut = new WorkshopConnection(conveyor.Id, PortId.Drive, motor1.Id, PortId.Drive, WorkshopConnectionDomain.Mechanical);
        var exDir = Assert.Throws<WorkshopEditRejectedException>(() =>
            new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new([motor1, conveyor]),
                Connections: new([outToOut])).Validate(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle));
        Assert.Equal(WorkshopEditRejection.InvalidConnection, exDir.Reason);

        // 5. Direction mismatch: Input to Input (Conveyor DriveIn to Motor PowerIn)
        var inToIn = new WorkshopConnection(conveyor.Id, PortId.DriveIn, motor1.Id, PortId.PowerIn, WorkshopConnectionDomain.Mechanical);
        var exIn = Assert.Throws<WorkshopEditRejectedException>(() =>
            new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new([motor1, conveyor]),
                Connections: new([inToIn])).Validate(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle));
        Assert.Equal(WorkshopEditRejection.InvalidConnection, exIn.Reason);
    }

    [Fact]
    public void RunControlParameterLeakage_RejectedByBundleValidator()
    {
        var entry = TestBundle.Entry(Conveyor);
        Assert.Empty(BundleValidator.Validate([new("conveyor", entry)]));

        // Mark surface_per_radian as RunControl
        var leakEntry = entry with
        {
            Metadata = entry.Metadata with
            {
                Parameters =
                [
                    entry.Metadata.Parameters[0],
                    entry.Metadata.Parameters[1],
                    entry.Metadata.Parameters[2] with { RunEdit = ParameterRunEdit.RunControl }
                ]
            }
        };

        Assert.True(RunControlBindings.IsBoundToNonLane(leakEntry, "surface_per_radian"));
        var errors = BundleValidator.Validate([new("conveyor", leakEntry)]);
        Assert.Contains(errors, e => e.Code == BundleErrorCode.OutOfRange && e.Detail.Contains("Metadata.Parameters[2].RunEdit"));
    }
}

/// <summary>
/// Scene and presentation art tests for Conveyor.
/// </summary>
[Collection<NativeSceneCollectionDefinition>]
public sealed class ConveyorArtTests(NativeSceneFixture godot)
{
    private const float PositionTolerance = 1e-4f;
    private const float AngleTolerance = 1e-3f;

    [Fact]
    public async Task TheRollerArtFollowsTheCommittedRollerPoseAndResetsToItsScenePose()
    {
        var battery = PlacedParts.Battery(new(1), -5, 4, 0, enabled: ElectricalEnable.Enabled);
        var motor = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("motor"), new(2), -2, 4, 0, 0, 0, 0, 1,
            new([new(new("speed"), 6f), new(new("torque"), 20f)]));
        var conveyor = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("conveyor"), new(3), 0, 4, 0, 0, 0, 0, 1,
            new([new(new("length"), 3f), new(new("width"), 1.2f), new(new("surface_per_radian"), 0.6666667f)]));

        var construction = new WorkshopConstruction(
            new(2),
            WorkshopCadenceSettings.Default(),
            new([battery, motor, conveyor]),
            Connections: new([
                new(battery.Id, PortId.Supply, motor.Id, PortId.PowerIn, WorkshopConnectionDomain.Electrical),
                new(motor.Id, PortId.Drive, conveyor.Id, PortId.DriveIn, WorkshopConnectionDomain.Mechanical)
            ]));

        var roller = construction.Instances.Identities[conveyor.Id].Hinges[0].Body;
        var registry = new PartRegistry(RuntimeTestServices.Core.Presentation, RuntimeTestServices.Bundle);
        registry.Load();

        var root = new Godot.Node3D();
        var part = Assert.IsType<MachinePart>(registry.Create(new PartId("conveyor")));
        try
        {
            godot.Tree.Root.AddChild(root);
            root.AddChild(part);
            part.EnsureConstructed();
            part.Position = new Godot.Vector3(0, 4, 0);
            part.AuthoredId = conveyor.Id;
            part.InstallPhysicalIdentity(new PackageBodyIndex(1), roller, construction.Instances.Identities);

            var art = Assert.IsAssignableFrom<Godot.Node3D>(part.PhysicalVisual(roller));
            Assert.Same(part.GetNode<Godot.Node3D>("Visual/Roller"), art);
            var pulleySpoke = part.GetNode<Godot.Node3D>("Visual/Roller/PulleyIn/Spoke");
            var authored = art.Transform;
            var authoredSpokeGlobal = pulleySpoke.GlobalTransform;

            var presentation = new WorkshopPhysicalPresentation();
            await using var harness = await WorkshopIntegrationHarness.Running(construction);
            const int ticks = 30;
            await harness.Advance(ticks);

            Assert.True(harness.Committed.Bodies.TryGet(roller, out var body));
            var q = body.Rotation;
            presentation.Apply([part], root.GlobalTransform, new PresentedBodySet([new PresentedBody(roller, body.Body.Cell, body.Body.Local, q)]));

            var committed = new Godot.Vector3(
                (body.Body.Cell.X + body.Body.Local.X) / 16f,
                (body.Body.Cell.Y + body.Body.Local.Y) / 16f,
                (body.Body.Cell.Z + body.Body.Local.Z) / 16f);
            Assert.True(art.GlobalPosition.DistanceTo(committed) <= PositionTolerance,
                $"The roller art is at {art.GlobalPosition}; the body at {committed}");

            var drawn = art.GlobalTransform.Basis.GetRotationQuaternion();
            var angle = drawn.AngleTo(new Godot.Quaternion(q.X, q.Y, q.Z, q.W));
            Assert.True(angle <= AngleTolerance, $"The roller art is turned {angle} rad from the committed roller");

            var turned = authored.Basis.GetRotationQuaternion().AngleTo(art.Transform.Basis.GetRotationQuaternion());
            Assert.True(turned > 0.5f, $"The roller art turned {turned} rad");
            var spokeTurned = authoredSpokeGlobal.Basis.GetRotationQuaternion().AngleTo(pulleySpoke.GlobalTransform.Basis.GetRotationQuaternion());
            Assert.True(spokeTurned > 0.5f, $"The input pulley spoke turned {spokeTurned} rad");

            await harness.Reset();
            part.ResetPhysicalVisuals();
            Assert.Equal(authored, art.Transform);
            Assert.True(harness.Committed.Bodies.TryGet(roller, out var reset));
            presentation.Apply([part], root.GlobalTransform, new PresentedBodySet([new PresentedBody(roller, reset.Body.Cell, reset.Body.Local, reset.Rotation)]));
            Assert.True(art.Transform.Origin.DistanceTo(authored.Origin) <= PositionTolerance &&
                art.Transform.Basis.GetRotationQuaternion().AngleTo(authored.Basis.GetRotationQuaternion()) <= AngleTolerance,
                $"After Reset the roller art is at {art.Transform}; its scene pose is {authored}");
            Assert.True(pulleySpoke.GlobalTransform.Basis.GetRotationQuaternion().AngleTo(authoredSpokeGlobal.Basis.GetRotationQuaternion()) <= AngleTolerance,
                $"After Reset the input pulley spoke did not restore to its scene pose");
            Assert.Empty(godot.Errors.Drain());
        }
        finally
        {
            root.Free();
        }
    }
}
