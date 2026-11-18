using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;
using CuriousContraptions.Simulation;
using Google.FlatBuffers;
using Godot;
using Xunit;
using Quaternion = System.Numerics.Quaternion;
using Vector3 = System.Numerics.Vector3;

namespace CuriousContraptions.Tests;

/// <summary>Story 11.2a (CAT-018): Clutch package, timed MechanicalTransmission solver law, ABI/wire contracts, and synthetic generality.</summary>
public sealed partial class ClutchTests
{
    private static readonly PartId MotorId = new("motor");
    private static readonly PartId ClutchId = new("clutch");
    private static readonly PartId ConveyorId = new("conveyor");
    private static readonly PartId BallId = new("ball");
    private static readonly PartId SwitchId = new("switch");

    // One-second window: at most 0.05 rad coupled drift and 0.01 rad inactive drift.
    private const float CoupledSpeedAllowance = 0.05f;
    private const float IdleSpeedAllowance = 0.01f;
    // Less than 1% of plate travel: a dimensionless allowance for linear closure samples.
    private const float ClosureAllowance = 0.005f;
    private const float RigY = 4f;
    private const float ConveyorCarrierHalfHeight = 0.12f;
    private const float BallRadius = 0.34f;
    private const float BallRestY = RigY + ConveyorCarrierHalfHeight + BallRadius; // 4.46 m
    private const float ClutchShaftMass = 0.25f;
    private const float ClutchShaftRadius = 0.28f;
    private const float ClutchShaftAxialInertia = 0.5f * ClutchShaftMass * ClutchShaftRadius * ClutchShaftRadius; // 0.0098 kg*m^2
    private const float ClutchBearingFrictionTorque = 0.02f; // N*m

    private static float EnergyAllowance(float speed, float inertia) =>
        0.5f * inertia * (2f * MathF.Abs(speed) * CoupledSpeedAllowance + CoupledSpeedAllowance * CoupledSpeedAllowance);

    private static WorkshopGenericInstance MotorPart(ulong id = 2, float speed = 6f, float torque = 20f,
        Vector3? position = null, Quaternion? rotation = null)
    {
        var p = position ?? new Vector3(-5.5f, RigY, 0f);
        var q = rotation ?? Quaternion.Identity;
        return WorkshopInput.Generic(RuntimeTestServices.Bundle, MotorId, new(id), p.X, p.Y, p.Z, q.X, q.Y, q.Z, q.W,
            new([new(new("speed"), speed), new(new("torque"), torque)]));
    }

    private static WorkshopGenericInstance ClutchPart(ulong id = 3, float closeSeconds = 0.2f,
        Vector3? position = null, Quaternion? rotation = null)
    {
        var p = position ?? new Vector3(-2.5f, RigY, 0f);
        var q = rotation ?? Quaternion.Identity;
        return WorkshopInput.Generic(RuntimeTestServices.Bundle, ClutchId, new(id), p.X, p.Y, p.Z, q.X, q.Y, q.Z, q.W,
            new([new(new("close_seconds"), closeSeconds)]));
    }

    private static WorkshopGenericInstance ConveyorPart(ulong id = 4, float length = 3f, float width = 1.2f,
        float surfacePerRadian = 0.6666667f, Vector3? position = null, Quaternion? rotation = null)
    {
        var p = position ?? new Vector3(1f, RigY, 0f);
        var q = rotation ?? Quaternion.Identity;
        return WorkshopInput.Generic(RuntimeTestServices.Bundle, ConveyorId, new(id), p.X, p.Y, p.Z, q.X, q.Y, q.Z, q.W,
            new([new(new("length"), length), new(new("width"), width), new(new("surface_per_radian"), surfacePerRadian)]));
    }

    private static WorkshopGenericInstance BallPart(ulong id = 5, float x = 0.5f, float y = BallRestY, float z = 0f) =>
        WorkshopInput.Generic(RuntimeTestServices.Bundle, BallId, new(id), x, y, z, 0f, 0f, 0f, 1f);

    private static WorkshopConnection Power(ulong source, ulong target) =>
        new(new(source), PortId.Supply, new(target), PortId.PowerIn, WorkshopConnectionDomain.Electrical);

    private static WorkshopConnection Drive(ulong source, ulong target) =>
        new(new(source), PortId.Drive, new(target), PortId.DriveIn, WorkshopConnectionDomain.Mechanical);

    private static int RateHz(SimulationCadence cadence) => cadence switch
    {
        SimulationCadence.Hz60 => 60,
        SimulationCadence.Hz120 => 120,
        SimulationCadence.Hz240 => 240,
        _ => throw new ArgumentOutOfRangeException(nameof(cadence))
    };

    private static Vector3 BodyPosition(WorkshopRead read, GpuBodyId id)
    {
        Assert.True(read.Bodies.TryGet(id, out var state), $"Missing body {id.Value}.");
        return new Vector3((state.Body.Cell.X + state.Body.Local.X) / 16f,
            (state.Body.Cell.Y + state.Body.Local.Y) / 16f,
            (state.Body.Cell.Z + state.Body.Local.Z) / 16f);
    }

    private static Vector3 BodyAngular(WorkshopRead read, GpuBodyId id)
    {
        Assert.True(read.Bodies.TryGet(id, out var state), $"Missing body {id.Value}.");
        return new Vector3(state.AngularVelocity.X, state.AngularVelocity.Y, state.AngularVelocity.Z);
    }

    private static MechanicalTransmissionRead SingleTransmission(WorkshopRead read, GpuBodyId owner) =>
        Assert.Single(read.Presentation.Transmissions.ToArray(), row => row.Owner == owner);

    private static bool ReadTransmissionSupplied(WorkshopIntegrationHarness harness, MechanicalTransmissionId txId)
    {
        var world = harness.Device.CommittedWorld;
        var txOffset = harness.Simulation.Scene.Physics.Layout.GetMechanicalTransmissionOffset(txId.Value);
        return BinaryPrimitives.ReadUInt32LittleEndian(world.Span.Slice(txOffset + PhysicsGpuAbi.MechanicalTransmissionSuppliedOffset, 4)) != 0u;
    }

    private static WorkshopConstruction StandardConveyorRig(float closeSeconds = 0.2f,
        SimulationCadence cadence = SimulationCadence.Hz120, ElectricalEnable motorBattery = ElectricalEnable.Enabled,
        ElectricalEnable coilBattery = ElectricalEnable.Enabled, bool includeBall = true, bool separateBatteries = true)
    {
        var instances = new List<IWorkshopInstance>();
        var connections = new List<WorkshopConnection>();
        var bMotor = PlacedParts.Battery(new(1), -8.5f, RigY, 0f, enabled: motorBattery);
        var motor = MotorPart(2, speed: 6f, torque: 20f);
        var clutch = ClutchPart(3, closeSeconds: closeSeconds);
        var conveyor = ConveyorPart(4);
        instances.Add(bMotor);
        instances.Add(motor);
        instances.Add(clutch);
        instances.Add(conveyor);
        connections.Add(Power(1, 2));
        connections.Add(Drive(2, 3));
        connections.Add(Drive(3, 4));
        if (separateBatteries)
        {
            var bCoil = PlacedParts.Battery(new(6), -8.5f, RigY + 2f, 0f, enabled: coilBattery);
            instances.Add(bCoil);
            connections.Add(Power(6, 3));
        }
        else
        {
            connections.Add(Power(1, 3));
        }
        if (includeBall)
            instances.Add(BallPart(5));
        var settings = WorkshopCadenceSettings.Default() with { Simulation = cadence };
        return new WorkshopConstruction(new(2), settings, new(instances.ToArray()), default, new(connections.ToArray()));
    }

    [Fact]
    public void ClutchPackage_DeclaresFootprintTransmissionsPortsSupplyAndPresentation()
    {
        var entry = TestBundle.Entry(ClutchId);
        Assert.Equal("clutch", entry.Metadata.Id);
        Assert.Equal("Clutch", entry.Metadata.DisplayName);
        Assert.Equal(38, entry.Metadata.PaletteOrder);
        Assert.Equal(ElementCategory.Power, entry.Metadata.Category);

        var closeParam = Assert.Single(entry.Metadata.Parameters);
        Assert.Equal("close_seconds", closeParam.Key);
        Assert.Equal(ParameterUnit.Second, closeParam.Unit);
        Assert.Equal(ParameterKind.Real, closeParam.Kind);
        Assert.Equal(0.05f, closeParam.Minimum);
        Assert.Equal(2f, closeParam.Maximum);
        Assert.Equal(0.2f, closeParam.Default);
        Assert.Equal(ParameterRunEdit.BuildOnly, closeParam.RunEdit);

        Assert.Equal(new ElementFootprint(3, 2, 0, 2), entry.Footprint);

        var ports = DeclaredPorts.Current.Of(ClutchId).ToArray();
        Assert.Equal(3, ports.Length);
        var driveIn = Assert.Single(ports, p => p.Id == PortId.DriveIn);
        Assert.Equal(WorkshopConnectionDomain.Mechanical, driveIn.Domain);
        Assert.Equal(WorkshopPortDirection.Input, driveIn.Direction);
        Assert.Equal(new CoordinateReference(CoordinateKind.Revolute, 0), driveIn.Coordinate);
        Assert.Equal(1f, driveIn.CoordinatePerRadian);
        Assert.Equal(new Vector3(-0.65f, 0f, 0.55f), driveIn.LocalPosition);

        var driveOut = Assert.Single(ports, p => p.Id == PortId.Drive);
        Assert.Equal(WorkshopConnectionDomain.Mechanical, driveOut.Domain);
        Assert.Equal(WorkshopPortDirection.Output, driveOut.Direction);
        Assert.Equal(new CoordinateReference(CoordinateKind.Revolute, 1), driveOut.Coordinate);
        Assert.Equal(1f, driveOut.CoordinatePerRadian);
        Assert.Equal(new Vector3(0.65f, 0f, 0.55f), driveOut.LocalPosition);

        var powerIn = Assert.Single(ports, p => p.Id == PortId.PowerIn);
        Assert.Equal(WorkshopConnectionDomain.Electrical, powerIn.Domain);
        Assert.Equal(WorkshopPortDirection.Input, powerIn.Direction);

        var mechanical = entry.Mechanical!;
        Assert.Equal(2, mechanical.Hinges.Count);
        Assert.All(mechanical.Hinges, h =>
        {
            Assert.Equal(RevoluteTravel.Continuous, h.Travel);
            Assert.Equal(ClutchBearingFrictionTorque, h.FrictionTorque.Constant);
        });
        var tx = Assert.Single(mechanical.Transmissions);
        Assert.Equal(new LocalMechanicalTransmissionId(1), tx.Id);
        Assert.Equal(0, tx.Driving.Index);
        Assert.Equal(1, tx.Driven.Index);
        Assert.Equal(1f, tx.Ratio.Constant);
        Assert.NotNull(tx.CloseSeconds);
        Assert.Equal("close_seconds", tx.CloseSeconds!.Value.Parameter);

        Assert.Equal(7, entry.Presentation.Bindings.Count);
        Assert.Equal(2, entry.Presentation.Bodies.Count);
    }

    [Fact]
    public async Task ClutchAdmission_StartsOpenUncoupledWithZeroClosureAndUnpowered()
    {
        var construction = StandardConveyorRig(closeSeconds: 0.2f, includeBall: false);
        await using var harness = await WorkshopIntegrationHarness.Building(construction);

        var clutchGuid = new GpuBodyId(3);
        var read = harness.Committed;
        var txRead = SingleTransmission(read, clutchGuid);
        var clutchIds = construction.Instances.Identities[clutchGuid];
        var clutchTxId = clutchIds.Transmissions[new(1)];
        var clutchSlot = harness.Simulation.Scene.Physics.Layout.MechanicalTransmissionSlots[clutchTxId.Value];
        Assert.Equal(clutchTxId, txRead.Transmission);
        Assert.Equal(0f, txRead.Closure);
        Assert.Equal(TransmissionEngagementPhase.Open, txRead.Phase);

        var world = harness.Device.CommittedWorld.ToArray();
        var abiDecl = PhysicsGpuAbi.ReadMechanicalTransmission(world, clutchSlot);
        var abiRead = Assert.Single(PhysicsGpuAbi.ReadMechanicalTransmissionReads(world), row => row.Transmission == clutchTxId);
        Assert.Equal(clutchGuid, abiDecl.Owner);
        Assert.Equal(1f, abiDecl.Ratio);
        Assert.Equal(0.2f, abiDecl.CloseSeconds);
        Assert.False(ReadTransmissionSupplied(harness, clutchTxId));
        Assert.Equal(0f, abiRead.Closure);
        Assert.Equal(TransmissionEngagementPhase.Open, abiRead.Phase);

        await harness.Run();
        var atTickZero = SingleTransmission(harness.Committed, clutchGuid);
        Assert.Equal(0f, atTickZero.Closure);
        Assert.Equal(TransmissionEngagementPhase.Open, atTickZero.Phase);
        Assert.False(ReadTransmissionSupplied(harness, clutchTxId));
    }

    [Fact]
    public async Task PositiveSpinUp_StaysDecoupledWhileClosingThenEngagesWithinWindowAndDrivesConveyor()
    {
        const float closeSeconds = 0.2f;
        const int nominalCloseTicks = 24;

        var construction = StandardConveyorRig(closeSeconds: closeSeconds, cadence: SimulationCadence.Hz120, includeBall: true);
        await using var harness = await WorkshopIntegrationHarness.Running(construction);

        var clutchIds = construction.Instances.Identities[new(3)];
        var clutchTxId = clutchIds.Transmissions[new(1)];
        var conveyorIds = construction.Instances.Identities[new(4)];
        var inputBody = clutchIds.Hinges[0].Body;
        var outputBody = clutchIds.Hinges[1].Body;
        var rollerBody = conveyorIds.Hinges[0].Body;
        var ballBody = new GpuBodyId(5);

        int? engageTick = null;
        var observedClosing = false;

        for (var tick = 1; tick <= nominalCloseTicks + 4; tick++)
        {
            await harness.AdvanceOne();
            var read = harness.Committed;
            var tx = SingleTransmission(read, new(3));
            Assert.True(ReadTransmissionSupplied(harness, clutchTxId), $"Coil should be supplied at tick {tick}.");
            if (tick == nominalCloseTicks / 2)
                Assert.InRange(tx.Closure, 0.5f - ClosureAllowance, 0.5f + ClosureAllowance);

            if (tx.Phase == TransmissionEngagementPhase.Closing)
            {
                observedClosing = true;
                Assert.True(tx.Closure > 0f && tx.Closure < 1f);
                // Strictly decoupled while closing: output, conveyor roller, and ball remain idle.
                Assert.True(MathF.Abs(BodyAngular(read, outputBody).Z) < IdleSpeedAllowance,
                    $"Output shaft coupled early at tick {tick}: w={BodyAngular(read, outputBody).Z}.");
                Assert.True(MathF.Abs(BodyAngular(read, rollerBody).Z) < IdleSpeedAllowance,
                    $"Conveyor roller spun early at tick {tick}.");
                Assert.True(MathF.Abs(BodyPosition(read, ballBody).X - 0.5f) < IdleSpeedAllowance,
                    $"Ball moved horizontally while clutch was closing at tick {tick}.");
            }
            else if (tx.Phase == TransmissionEngagementPhase.Engaged)
            {
                Assert.Equal(1f, tx.Closure);
                engageTick ??= tick;
            }
            else
            {
                Assert.Fail($"Unexpected phase {tx.Phase} during coil-powered spin-up at tick {tick}.");
            }
        }

        Assert.True(observedClosing);
        Assert.NotNull(engageTick);
        Assert.InRange(engageTick.Value, nominalCloseTicks - 1, nominalCloseTicks + 2);
        // Motor input shaft was already spinning before engagement completed.
        Assert.True(BodyAngular(harness.Committed, inputBody).Z > 1f);

        await harness.Advance(48);
        var finalRead = harness.Committed;
        var wIn = BodyAngular(finalRead, inputBody).Z;
        var wOut = BodyAngular(finalRead, outputBody).Z;
        var wRoller = BodyAngular(finalRead, rollerBody).Z;
        var ballPos = BodyPosition(finalRead, ballBody);
        var ballRestDelta = MathF.Abs(ballPos.Y - BallRestY);

        Assert.True(wIn > 1f && wOut > 1f, $"Clutch shafts should spin positively together; got in={wIn}, out={wOut}.");
        Assert.InRange(wOut - wIn, -0.05f, 0.05f);
        Assert.True(wRoller > 1f, $"Conveyor roller should drive positively; got wRoller={wRoller}.");
        Assert.True(ballPos.X > 0.65f, $"Ball should accelerate along +X on engaged conveyor; got x={ballPos.X}.");
        Assert.True(ballRestDelta < 0.05f, $"Ball should remain supported on conveyor surface; y={ballPos.Y}.");
    }

    [Fact]
    public async Task EqualInertiaDeclaredClosure_SplitsSpinHalfAndNeverCreatesKineticEnergy()
    {
        const float targetSpeed = 12f;
        const float closeSeconds = .05f;
        var drivePort = new PortId("DrivePower");
        var coilPort = new PortId("CoilPower");
        var template = TestBundle.Entry(ClutchId);
        var material = new MaterialProperties(0f, 0f, 0f, 0f);
        var hinges = template.Mechanical!.Hinges.Select(hinge => hinge with
        {
            FrictionTorque = MechanicalValue.Fixed(0f),
            MovingBody = hinge.MovingBody with
            {
                MassFrame = MassFrameDeclaration.Constant(ClutchShaftMass, Vector3.Zero,
                    new(ClutchShaftAxialInertia), Quaternion.Identity, 0f, Vector3.Zero),
                Colliders = hinge.MovingBody.Colliders.Select(collider => collider with { Material = material }).ToArray()
            }
        }).ToArray();
        hinges[0] = hinges[0] with { Drive = new(MechanicalValue.Fixed(targetSpeed), MechanicalValue.Fixed(20f)) };
        var powerPort = template.Ports.Single(port => port.Id == PortId.PowerIn);
        var entry = template with
        {
            Metadata = template.Metadata with { Id = "equal_inertia_probe", Parameters = [] },
            Colliders = template.Colliders.Select(collider => collider with { Material = material }).ToArray(),
            Ports = [powerPort with { Id = drivePort }, powerPort with { Id = coilPort }],
            Mechanical = template.Mechanical with { Hinges = hinges, Transmissions =
                [new(new(1), new(CoordinateKind.Revolute, 0), new(CoordinateKind.Revolute, 1),
                    MechanicalValue.Fixed(1f), TransmissionConstraintKind.Bilateral, MechanicalValue.Fixed(closeSeconds))] },
            Electrical = template.Electrical! with { SupplyGraph = new(
                [new SupplyConsumerDescriptor(new("drive"), [new SupplyDriveTarget(new(CoordinateKind.Revolute, 0))]),
                 new SupplyConsumerDescriptor(new("coil"), [new SupplyTransmissionTarget(new(1))])], [],
                [new(drivePort, new(new("drive"), SupplyTerminal.PowerIn)),
                 new(coilPort, new(new("coil"), SupplyTerminal.PowerIn))]) }
        };
        Assert.Empty(BundleValidator.Validate([new("probe", entry)]));
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        using var declarations = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(ElementPortCatalogue.FromBundle(DeclaredPhysics.Entries.Values));
        var part = WorkshopInput.Generic(RuntimeTestServices.Bundle, new(entry.Metadata.Id), new(3),
            0f, RigY, 0f, 0f, 0f, 0f, 1f);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(PlacedParts.Battery(new(1), -8f, RigY, 0f),
                PlacedParts.Battery(new(2), -8f, RigY + 2f, 0f, enabled: ElectricalEnable.Disabled), part),
            Connections: new([
                new(new(1), PortId.Supply, part.Id, drivePort, WorkshopConnectionDomain.Electrical),
                new(new(2), PortId.Supply, part.Id, coilPort, WorkshopConnectionDomain.Electrical)]));
        await harness.Require(WorkshopCommandKind.Construct, construction);
        var physics = harness.Simulation.Scene.Physics;
        var identities = construction.Instances.Identities[part.Id];
        var input = identities.Hinges[0].Body;
        var output = identities.Hinges[1].Body;
        var joint = identities.Hinges[0].Joint;
        foreach (var id in new[] { input, output })
        {
            var body = Assert.Single(physics.Bodies.ToArray(), row => row.Id == id);
            Assert.Equal(ClutchShaftAxialInertia, body.MassProperties.Z.Moment);
            Assert.Equal(default(AccelerationVector), body.Gravity);
            Assert.Equal(default(InverseSeconds), body.LinearDrag);
        }
        Assert.All(physics.Revolutes.ToArray(), hinge =>
            Assert.Equal((0f, 0f, 0f), (hinge.Stiffness, hinge.Damping, hinge.FrictionTorque)));
        var jointSlot = Array.FindIndex(physics.Revolutes.ToArray(), row => row.Id == joint);
        Assert.True(jointSlot >= 0);
        float Work() => PhysicsGpuAbi.ReadRevoluteState(harness.Device.CommittedWorld.ToArray(), jointSlot).Work;
        void NoDriveWork(float priorWork)
        {
            Assert.False(Assert.Single(harness.Committed.Presentation.Actuators.ToArray(),
                row => row.Drive == SupplyConsumer.Revolute(joint)).SupplyClosed);
            Assert.Equal(priorWork, Work()); // Disabled drive must add exactly zero cumulative work.
        }
        await harness.Run();
        await harness.Advance(24);
        Assert.InRange(BodyAngular(harness.Committed, input).Z, targetSpeed - CoupledSpeedAllowance, targetSpeed + CoupledSpeedAllowance);
        Assert.InRange(MathF.Abs(BodyAngular(harness.Committed, output).Z), 0f, IdleSpeedAllowance);
        Assert.Equal(TransmissionEngagementPhase.Open, SingleTransmission(harness.Committed, part.Id).Phase);
        await harness.Require(WorkshopCommandKind.ConfigureRunControl,
            runControl: harness.RunControl(new(1), RunControlLane.SourceEnable, RunControlValue.Of(ElectricalEnable.Disabled)));
        var stoppedWork = Work();
        for (var tick = 0; tick < 3; tick++)
        {
            await harness.AdvanceOne();
            NoDriveWork(stoppedWork);
            Assert.InRange(BodyAngular(harness.Committed, input).Z, targetSpeed - CoupledSpeedAllowance, targetSpeed + CoupledSpeedAllowance);
            Assert.InRange(MathF.Abs(BodyAngular(harness.Committed, output).Z), 0f, IdleSpeedAllowance);
        }
        var inputBefore = BodyAngular(harness.Committed, input).Z;
        var outputBefore = BodyAngular(harness.Committed, output).Z;
        var momentumBefore = ClutchShaftAxialInertia * (inputBefore + outputBefore);
        var energyBefore = .5f * ClutchShaftAxialInertia * (inputBefore * inputBefore + outputBefore * outputBefore);
        var expectedShared = (inputBefore + outputBefore) / 2f;
        var expectedEnergy = ClutchShaftAxialInertia * expectedShared * expectedShared;
        await harness.Require(WorkshopCommandKind.ConfigureRunControl,
            runControl: harness.RunControl(new(2), RunControlLane.SourceEnable, RunControlValue.Of(ElectricalEnable.Enabled)));
        for (var tick = 1; tick <= 6; tick++)
        {
            await harness.AdvanceOne();
            NoDriveWork(stoppedWork);
            Assert.Equal(tick < 6 ? TransmissionEngagementPhase.Closing : TransmissionEngagementPhase.Engaged,
                SingleTransmission(harness.Committed, part.Id).Phase);
        }
        var inputAfter = BodyAngular(harness.Committed, input).Z;
        var outputAfter = BodyAngular(harness.Committed, output).Z;
        Assert.InRange(inputAfter, expectedShared - CoupledSpeedAllowance, expectedShared + CoupledSpeedAllowance);
        Assert.InRange(outputAfter, expectedShared - CoupledSpeedAllowance, expectedShared + CoupledSpeedAllowance);
        var momentumAfter = ClutchShaftAxialInertia * (inputAfter + outputAfter);
        Assert.InRange(momentumAfter - momentumBefore, -2f * ClutchShaftAxialInertia * CoupledSpeedAllowance,
            2f * ClutchShaftAxialInertia * CoupledSpeedAllowance);
        var energyAfter = .5f * ClutchShaftAxialInertia * (inputAfter * inputAfter + outputAfter * outputAfter);
        var allowance = 2f * EnergyAllowance(expectedShared, ClutchShaftAxialInertia);
        Assert.InRange(energyAfter, expectedEnergy - allowance, expectedEnergy + allowance);
        Assert.True(energyAfter < energyBefore && energyAfter <= energyBefore + EnergyAllowance(inputBefore, ClutchShaftAxialInertia));
        await harness.Reset();
        Assert.Equal(Vector3.Zero, BodyAngular(harness.Committed, input));
        Assert.Equal(Vector3.Zero, BodyAngular(harness.Committed, output));
        Assert.Equal((TransmissionEngagementPhase.Open, 0f),
            (SingleTransmission(harness.Committed, part.Id).Phase, SingleTransmission(harness.Committed, part.Id).Closure));
        Assert.Equal(ElectricalEnable.Enabled, harness.Committed.RunControl(physics, new(1), RunControlLane.SourceEnable,
            source: physics.Electrical.Sources.ToArray().Single(source => source.Owner == new GpuBodyId(1)).Id).Enable);
        Assert.Equal(ElectricalEnable.Disabled, harness.Committed.RunControl(physics, new(2), RunControlLane.SourceEnable,
            source: physics.Electrical.Sources.ToArray().Single(source => source.Owner == new GpuBodyId(2)).Id).Enable);
    }

    [Fact]
    public async Task HardwareClutchClosureUnderDisabledMotor_LocksSharedSpinWithoutEnergyCreation()
    {
        var construction = StandardConveyorRig(closeSeconds: 0.05f, cadence: SimulationCadence.Hz120,
            motorBattery: ElectricalEnable.Enabled, coilBattery: ElectricalEnable.Disabled, includeBall: false);
        await using var harness = await WorkshopIntegrationHarness.Running(construction);

        var motorIds = construction.Instances.Identities[new(2)];
        var clutchIds = construction.Instances.Identities[new(3)];
        var conveyorIds = construction.Instances.Identities[new(4)];
        var motorRotor = motorIds.Hinges[0].Body;
        var inBody = clutchIds.Hinges[0].Body;
        var outBody = clutchIds.Hinges[1].Body;
        var rollerBody = conveyorIds.Hinges[0].Body;

        // Spin up motor + input shaft with coil disabled, then cut motor power and engage clutch.
        await harness.Advance(18);
        await harness.Require(WorkshopCommandKind.ConfigureRunControl,
            runControl: harness.RunControl(new(1), RunControlLane.SourceEnable, RunControlValue.Of(ElectricalEnable.Disabled)));
        await harness.Advance(2);
        await harness.Require(WorkshopCommandKind.ConfigureRunControl,
            runControl: harness.RunControl(new(6), RunControlLane.SourceEnable, RunControlValue.Of(ElectricalEnable.Enabled)));

        var initialEnergy = RotationalKinetic(harness.Committed, motorRotor, 0.0225f)
            + RotationalKinetic(harness.Committed, inBody, ClutchShaftAxialInertia)
            + RotationalKinetic(harness.Committed, outBody, ClutchShaftAxialInertia)
            + RotationalKinetic(harness.Committed, rollerBody, 0.01f);
        var energyAllowance = EnergyAllowance(BodyAngular(harness.Committed, motorRotor).Z, 0.0225f)
            + EnergyAllowance(BodyAngular(harness.Committed, inBody).Z, ClutchShaftAxialInertia)
            + EnergyAllowance(BodyAngular(harness.Committed, outBody).Z, ClutchShaftAxialInertia)
            + EnergyAllowance(BodyAngular(harness.Committed, rollerBody).Z, 0.01f);
        var locked = false;
        for (var tick = 0; tick < 10; tick++)
        {
            await harness.AdvanceOne();
            var next = harness.Committed;
            var nextKe = RotationalKinetic(next, motorRotor, 0.0225f)
                       + RotationalKinetic(next, inBody, ClutchShaftAxialInertia)
                       + RotationalKinetic(next, outBody, ClutchShaftAxialInertia)
                       + RotationalKinetic(next, rollerBody, 0.01f);
            // Fixed initial-energy bound, never an accumulating per-tick allowance.
            Assert.True(nextKe <= initialEnergy + energyAllowance, $"Engagement created energy: {initialEnergy} -> {nextKe}.");
            var tx = SingleTransmission(next, new(3));
            if (tx.Phase == TransmissionEngagementPhase.Engaged)
            {
                locked = true;
                var wIn = BodyAngular(next, inBody).Z;
                var wOut = BodyAngular(next, outBody).Z;
                Assert.True(wIn > 1f && wOut > 1f);
                Assert.InRange(wOut - wIn, -0.05f, 0.05f);
                break;
            }
        }
        Assert.True(locked);

        static float RotationalKinetic(WorkshopRead r, GpuBodyId body, float inertiaZ)
        {
            var w = BodyAngular(r, body).Z;
            return 0.5f * inertiaZ * w * w;
        }
    }

    [Fact]
    public async Task UnpoweredControls_KeepOutputIdle_AndInvalidPortWiresAreRefused()
    {
        // Case 1: Missing coil supply wire into Clutch PowerIn -> stays Open and decoupled.
        var noCoil = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(PlacedParts.Battery(new(1), -8.5f, RigY, 0f), MotorPart(2), ClutchPart(3), ConveyorPart(4)),
            default, new([Power(1, 2), Drive(2, 3), Drive(3, 4)]));
        await using (var h = await WorkshopIntegrationHarness.Running(noCoil))
        {
            await h.Advance(48);
            var tx = SingleTransmission(h.Committed, new(3));
            Assert.Equal(0f, tx.Closure);
            Assert.Equal(TransmissionEngagementPhase.Open, tx.Phase);
            Assert.False(ReadTransmissionSupplied(h, noCoil.Instances.Identities[new(3)].Transmissions[new(1)]));
            Assert.True(BodyAngular(h.Committed, noCoil.Instances.Identities[new(3)].Hinges[0].Body).Z > 1f);
            Assert.True(MathF.Abs(BodyAngular(h.Committed, noCoil.Instances.Identities[new(3)].Hinges[1].Body).Z) < IdleSpeedAllowance);
            Assert.True(MathF.Abs(BodyAngular(h.Committed, noCoil.Instances.Identities[new(4)].Hinges[0].Body).Z) < IdleSpeedAllowance);
            Assert.Equal(0f, PhysicsGpuAbi.ReadRevoluteState(h.Device.CommittedWorld.ToArray(), 1).Work);
        }

        // Case 2: Coil powered but no mechanical DriveIn -> closes cleanly, zero output motion.
        var noDrive = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(PlacedParts.Battery(new(1), -8.5f, RigY, 0f), ClutchPart(3, 0.05f), ConveyorPart(4)),
            default, new([Power(1, 3), Drive(3, 4)]));
        await using (var h = await WorkshopIntegrationHarness.Running(noDrive))
        {
            await h.Advance(12);
            var tx = SingleTransmission(h.Committed, new(3));
            Assert.Equal(1f, tx.Closure);
            Assert.Equal(TransmissionEngagementPhase.Engaged, tx.Phase);
            Assert.True(MathF.Abs(BodyAngular(h.Committed, noDrive.Instances.Identities[new(3)].Hinges[1].Body).Z) < IdleSpeedAllowance);
            Assert.True(MathF.Abs(BodyAngular(h.Committed, noDrive.Instances.Identities[new(4)].Hinges[0].Body).Z) < IdleSpeedAllowance);
        }

        // Case 3: Disabled battery wired to both Motor and Clutch -> stays Open and still.
        var disabled = StandardConveyorRig(closeSeconds: 0.05f, motorBattery: ElectricalEnable.Disabled,
            coilBattery: ElectricalEnable.Disabled, includeBall: false, separateBatteries: false);
        await using (var h = await WorkshopIntegrationHarness.Running(disabled))
        {
            await h.Advance(24);
            var tx = SingleTransmission(h.Committed, new(3));
            Assert.Equal(0f, tx.Closure);
            Assert.Equal(TransmissionEngagementPhase.Open, tx.Phase);
            Assert.False(ReadTransmissionSupplied(h, disabled.Instances.Identities[new(3)].Transmissions[new(1)]));
            Assert.Equal(0f, BodyAngular(h.Committed, disabled.Instances.Identities[new(3)].Hinges[0].Body).Z);
            Assert.Equal(0f, BodyAngular(h.Committed, disabled.Instances.Identities[new(3)].Hinges[1].Body).Z);
        }

        // Case 4: Typed port/domain connection refusals.
        var battery = PlacedParts.Battery(new(1), -8f, RigY, 0f);
        var clutch = ClutchPart(3);
        var switchPart = WorkshopInput.Generic(RuntimeTestServices.Bundle, SwitchId, new(7), -5f, RigY, 0f, 0f, 0f, 0f, 1f);
        var instances = new WorkshopInstances(battery, clutch, switchPart);

        foreach (var badConn in new[]
        {
            new WorkshopConnection(switchPart.Id, PortId.ActivationOut, clutch.Id, PortId.PowerIn, WorkshopConnectionDomain.Activation),
            new WorkshopConnection(switchPart.Id, PortId.ActivationOut, clutch.Id, PortId.PowerIn, WorkshopConnectionDomain.Electrical),
            new WorkshopConnection(battery.Id, PortId.Supply, clutch.Id, PortId.DriveIn, WorkshopConnectionDomain.Electrical),
            new WorkshopConnection(battery.Id, PortId.Supply, clutch.Id, PortId.DriveIn, WorkshopConnectionDomain.Mechanical)
        })
            EditRefusal.Is(WorkshopEditRejection.InvalidConnection,
                () => new WorkshopConnections(badConn).Validate(RuntimeTestServices.Core.Supply, instances));
    }

    [Fact]
    public async Task MidRunCoilCut_UncouplesImmediatelyPreservesOutputMomentumAndOpensLinearly()
    {
        const float closeSeconds = 0.6f; // 72 ticks at 120 Hz -> ample room to inspect linear opening and re-accelerated input
        const int closeTicks = 72;
        const float dtTick = 1f / 120f;
        var bMotor = PlacedParts.Battery(new(1), -8.5f, RigY, 0f, enabled: ElectricalEnable.Enabled);
        var bCoil = PlacedParts.Battery(new(6), -8.5f, RigY + 2f, 0f, enabled: ElectricalEnable.Enabled);
        var motor = MotorPart(2, speed: 12f, torque: 20f);
        var clutch = ClutchPart(3, closeSeconds: closeSeconds);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(bMotor, bCoil, motor, clutch), default,
            new([Power(1, 2), Power(6, 3), Drive(2, 3)]));

        await using var harness = await WorkshopIntegrationHarness.Running(construction);
        await harness.Advance(84);

        var ids = construction.Instances.Identities[new(3)];
        var inBody = ids.Hinges[0].Body;
        var outBody = ids.Hinges[1].Body;

        Assert.Equal(TransmissionEngagementPhase.Engaged, SingleTransmission(harness.Committed, new(3)).Phase);
        var preCutOmega = BodyAngular(harness.Committed, outBody).Z;
        Assert.True(preCutOmega > 10f);

        // Cut both coil and motor power so motor+input slows slightly, then re-enable ONLY motor mid-opening.
        await harness.Require(WorkshopCommandKind.ConfigureRunControl,
            runControl: harness.RunControl(bCoil.Id, RunControlLane.SourceEnable, RunControlValue.Of(ElectricalEnable.Disabled)));
        await harness.Require(WorkshopCommandKind.ConfigureRunControl,
            runControl: harness.RunControl(bMotor.Id, RunControlLane.SourceEnable, RunControlValue.Of(ElectricalEnable.Disabled)));

        var expectedStep = dtTick / closeSeconds;
        var inputBeforeReenable = 0f;
        int? openTick = null;
        var sawInputImpulseWhileOpening = false;

        for (var k = 1; k <= closeTicks + 4; k++)
        {
            if (k == 25)
            {
                inputBeforeReenable = BodyAngular(harness.Committed, inBody).Z;
                await harness.Require(WorkshopCommandKind.ConfigureRunControl,
                    runControl: harness.RunControl(bMotor.Id, RunControlLane.SourceEnable, RunControlValue.Of(ElectricalEnable.Enabled)));
            }
            await harness.AdvanceOne();
            var read = harness.Committed;
            var tx = SingleTransmission(read, new(3));
            Assert.False(ReadTransmissionSupplied(harness, ids.Transmissions[new(1)]));

            var wIn = BodyAngular(read, inBody).Z;
            var wOut = BodyAngular(read, outBody).Z;

            if (k == 1)
            {
                // Immediate uncoupling on the first coil-cut tick.
                Assert.Equal(TransmissionEngagementPhase.Opening, tx.Phase);
                Assert.InRange(tx.Closure - (1f - expectedStep), -ClosureAllowance, ClosureAllowance);
            }

            if (k >= 25 && k <= 27 && wIn - inputBeforeReenable > 0.25f)
            {
                sawInputImpulseWhileOpening = true;
                // Re-accelerated input does NOT speed up the uncoupled output shaft.
                var expectedCoast = preCutOmega - ClutchBearingFrictionTorque / ClutchShaftAxialInertia * k * dtTick;
                Assert.InRange(wOut - expectedCoast, -CoupledSpeedAllowance, CoupledSpeedAllowance);
            }

            if (tx.Phase == TransmissionEngagementPhase.Opening)
            {
                if (k == closeTicks / 2)
                    Assert.InRange(tx.Closure, 0.5f - ClosureAllowance, 0.5f + ClosureAllowance);
                // Output coasts independently under 0.02 N*m bearing friction (damping = 0.02 / 0.0098 = 2.0408 rad/s^2).
                var expectedOutput = preCutOmega - (ClutchBearingFrictionTorque / ClutchShaftAxialInertia) * (k * dtTick);
                Assert.InRange(wOut - expectedOutput, -CoupledSpeedAllowance, CoupledSpeedAllowance);
            }
            else if (tx.Phase == TransmissionEngagementPhase.Open)
            {
                Assert.Equal(0f, tx.Closure);
                openTick ??= k;
            }
            else
            {
                Assert.Fail($"Unexpected phase {tx.Phase} during coil-cut opening at tick {k}.");
            }

        }

        Assert.True(sawInputImpulseWhileOpening);
        Assert.NotNull(openTick);
        Assert.InRange(openTick.Value, closeTicks - 1, closeTicks + 2);
    }

    [Fact]
    public async Task ReengagementAfterPartialOpening_ClosesFromCurrentClosureAndLocksUnderLoad()
    {
        const float closeSeconds = 0.4f; // 48 ticks full close/open at 120 Hz
        const int halfTicks = 24;
        var bMotor = PlacedParts.Battery(new(1), -8.5f, RigY, 0f, enabled: ElectricalEnable.Enabled);
        var bCoil = PlacedParts.Battery(new(6), -8.5f, RigY + 2f, 0f, enabled: ElectricalEnable.Enabled);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(bMotor, bCoil, MotorPart(2, speed: 8f, torque: 20f), ClutchPart(3, closeSeconds: closeSeconds), ConveyorPart(4)),
            default, new([Power(1, 2), Power(6, 3), Drive(2, 3), Drive(3, 4)]));

        await using var harness = await WorkshopIntegrationHarness.Running(construction);
        await harness.Advance(60);

        var clutchIds = construction.Instances.Identities[new(3)];
        var inBody = clutchIds.Hinges[0].Body;
        var outBody = clutchIds.Hinges[1].Body;
        Assert.Equal(TransmissionEngagementPhase.Engaged, SingleTransmission(harness.Committed, new(3)).Phase);

        // Cut coil for 24 ticks -> partial opening around 0.5 while Conveyor+Output load slows down.
        await harness.Require(WorkshopCommandKind.ConfigureRunControl,
            runControl: harness.RunControl(bCoil.Id, RunControlLane.SourceEnable, RunControlValue.Of(ElectricalEnable.Disabled)));
        await harness.Advance(halfTicks);

        var partialTx = SingleTransmission(harness.Committed, new(3));
        Assert.Equal(TransmissionEngagementPhase.Opening, partialTx.Phase);
        Assert.InRange(partialTx.Closure, 0.45f, 0.55f);
        var wOutBeforeReenable = BodyAngular(harness.Committed, outBody).Z;
        var wInBeforeReenable = BodyAngular(harness.Committed, inBody).Z;
        Assert.True(wInBeforeReenable - wOutBeforeReenable > 0.3f,
            $"Output under load should have slowed below motor input: in={wInBeforeReenable}, out={wOutBeforeReenable}.");

        // Re-enable coil -> immediately switches to Closing FROM partialTx.Closure, stays decoupled until Closure == 1.
        await harness.Require(WorkshopCommandKind.ConfigureRunControl,
            runControl: harness.RunControl(bCoil.Id, RunControlLane.SourceEnable, RunControlValue.Of(ElectricalEnable.Enabled)));
        var expectedRemainingTicks = (int)MathF.Round((1f - partialTx.Closure) * closeSeconds * 120f);
        int? reengagedTick = null;

        for (var k = 1; k <= expectedRemainingTicks + 4; k++)
        {
            await harness.AdvanceOne();
            var read = harness.Committed;
            var tx = SingleTransmission(read, new(3));
            var wIn = BodyAngular(read, inBody).Z;
            var wOut = BodyAngular(read, outBody).Z;

            if (k == 1)
            {
                Assert.Equal(TransmissionEngagementPhase.Closing, tx.Phase);
                Assert.True(tx.Closure > partialTx.Closure,
                    $"Closing should resume from intermediate {partialTx.Closure}, got {tx.Closure}.");
            }

            if (tx.Phase == TransmissionEngagementPhase.Closing)
            {
                // Still decoupled under load during re-closing.
                Assert.True(wIn - wOut > 0.2f, $"Output coupled prematurely while Closing at tick {k}: in={wIn}, out={wOut}.");
            }
            else if (tx.Phase == TransmissionEngagementPhase.Engaged)
            {
                Assert.Equal(1f, tx.Closure);
                reengagedTick ??= k;
                Assert.InRange(wIn - wOut, -0.05f, 0.05f);
                break;
            }
            else
            {
                Assert.Fail($"Unexpected phase {tx.Phase} during re-engagement.");
            }
        }

        Assert.NotNull(reengagedTick);
        Assert.InRange(reengagedTick.Value, expectedRemainingTicks - 1, expectedRemainingTicks + 2);
    }

    [Theory]
    [InlineData(0.05f, SimulationCadence.Hz60)]
    [InlineData(0.05f, SimulationCadence.Hz120)]
    [InlineData(0.05f, SimulationCadence.Hz240)]
    [InlineData(0.2f, SimulationCadence.Hz60)]
    [InlineData(0.2f, SimulationCadence.Hz120)]
    [InlineData(0.2f, SimulationCadence.Hz240)]
    [InlineData(2.0f, SimulationCadence.Hz60)]
    [InlineData(2.0f, SimulationCadence.Hz120)]
    [InlineData(2.0f, SimulationCadence.Hz240)]
    public async Task CloseSeconds_ClosesWithinWindowAcrossCadences(float closeSeconds, SimulationCadence cadence)
    {
        var rate = RateHz(cadence);
        var dtTick = 1f / rate;
        var expectedTicks = (int)MathF.Round(closeSeconds * rate);
        var settings = WorkshopCadenceSettings.Default() with { Simulation = cadence };
        var construction = new WorkshopConstruction(new(2), settings,
            new(PlacedParts.Battery(new(1), -8.5f, RigY, 0f), ClutchPart(3, closeSeconds: closeSeconds)),
            default, new([Power(1, 3)]));

        await using var harness = await WorkshopIntegrationHarness.Running(construction);

        int? engageTick = null;
        for (var tick = 1; tick <= expectedTicks + 3; tick++)
        {
            await harness.AdvanceOne();
            var tx = SingleTransmission(harness.Committed, new(3));
            if (tick == expectedTicks / 2)
                Assert.InRange(tx.Closure - (tick * dtTick / closeSeconds), -ClosureAllowance, ClosureAllowance);
            if (tx.Phase == TransmissionEngagementPhase.Engaged)
            {
                Assert.Equal(1f, tx.Closure);
                engageTick ??= tick;
                break;
            }
        }

        Assert.NotNull(engageTick);
        Assert.InRange(engageTick.Value, expectedTicks - 1, expectedTicks + 2);
    }

    [Fact]
    public void CloseSeconds_AdmitsExactEndpoints()
    {
        foreach (var valid in new[] { 0.05f, MathF.BitIncrement(0.05f), MathF.BitDecrement(2.0f), 2.0f })
        {
            var instance = ClutchPart(3, closeSeconds: valid);
            Assert.Equal(valid, instance.Parameters[new("close_seconds")]);
        }
    }

    [Theory]
    [InlineData(0f, WorkshopSaveRejection.OutOfRange)]
    [InlineData(3f, WorkshopSaveRejection.OutOfRange)]
    [InlineData(-1f, WorkshopSaveRejection.OutOfRange)]
    [InlineData(float.NaN, WorkshopSaveRejection.NonFinite)]
    [InlineData(float.PositiveInfinity, WorkshopSaveRejection.NonFinite)]
    [InlineData(float.NegativeInfinity, WorkshopSaveRejection.NonFinite)]
    public void CloseSeconds_RefusesInvalidValuesAcrossParametersInputAndSaveDecode(float invalid, WorkshopSaveRejection expectedSaveRejection)
    {
        var collection = new InstanceParameterCollection([new(new("close_seconds"), invalid)]);
        Assert.Throws<ArgumentException>(() => collection.Validate(ClutchId));
        Assert.Throws<ArgumentException>(() => ClutchPart(3, closeSeconds: invalid));

        var validSave = new WorkshopSavedConstruction(
            new(new(1), WorkshopCadenceSettings.Default(), new(ClutchPart(3, closeSeconds: 0.2f))),
            new(4));
        var malformedBytes = BuildSavedConstructionWithRawCloseSeconds(validSave, invalid);
        SaveFixture.Refused(expectedSaveRejection, () => WorkshopSaveCodec.Decode(
            RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply,
            RuntimeTestServices.Bundle, SaveFixture.Elements, malformedBytes));
    }

    [Fact]
    public async Task RotatedMounts_PreserveDomainsAndSign_ClosedLoopStaysAtRest_AndDuplicateDriveInRejected()
    {
        var rotations = new[]
        {
            Quaternion.CreateFromYawPitchRoll(Degrees(40f), Degrees(25f), Degrees(15f)),
            Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2f),
            Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2f),
            Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2f)
        };

        foreach (var q in rotations)
        {
            var b = PlacedParts.Battery(new(1), -9f, RigY, 0f);
            var m = MotorPart(2, speed: 6f, torque: 20f, position: new(-5.5f, RigY, 0f), rotation: q);
            var c = ClutchPart(3, closeSeconds: 0.05f, position: new(-2.5f, RigY, 0f), rotation: q);
            var conv = ConveyorPart(4, position: new(2.5f, RigY, 0f), rotation: q);
            var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
                new(b, m, c, conv), default, new([Power(1, 2), Power(1, 3), Drive(2, 3), Drive(3, 4)]));
            construction.Validate(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle);

            foreach (var p in DeclaredPorts.Current.Of(ClutchId))
            {
                Assert.True(DeclaredPorts.Current.Has(ClutchId, p.Id, p.Domain, p.Direction));
                var worldAnchor = Vector3.Transform(DeclaredPorts.Current.LocalPosition(ClutchId, p.Id), q);
                Assert.True(float.IsFinite(worldAnchor.X) && float.IsFinite(worldAnchor.Y) && float.IsFinite(worldAnchor.Z));
            }

            await using var harness = await WorkshopIntegrationHarness.Running(construction);
            await harness.Advance(18);
            var read = harness.Committed;
            var tx = SingleTransmission(read, new(3));
            Assert.Equal(TransmissionEngagementPhase.Engaged, tx.Phase);

            var worldZ = Vector3.Transform(Vector3.UnitZ, q);
            var clutchIds = construction.Instances.Identities[new(3)];
            var rollerBody = construction.Instances.Identities[new(4)].Hinges[0].Body;
            var signedIn = Vector3.Dot(BodyAngular(read, clutchIds.Hinges[0].Body), worldZ);
            var signedOut = Vector3.Dot(BodyAngular(read, clutchIds.Hinges[1].Body), worldZ);
            var signedRoller = Vector3.Dot(BodyAngular(read, rollerBody), worldZ);
            Assert.True(signedIn > 1f && signedOut > 1f && signedRoller > 1f,
                $"Signed rotated speeds should be positive along rotated hinge axis; got in={signedIn}, out={signedOut}, roller={signedRoller}.");
            Assert.InRange(signedOut - signedIn, -0.05f, 0.05f);
        }

        // Closed mechanical loop Clutch -> Conveyor -> Clutch stays stably at rest once engaged.
        var loopBattery = PlacedParts.Battery(new(1), -8.5f, RigY, 0f);
        var loopClutch = ClutchPart(3, closeSeconds: 0.1f);
        var loopConveyor = ConveyorPart(4);
        var loopConstruction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(loopBattery, loopClutch, loopConveyor), default,
            new([Power(1, 3), Drive(3, 4), Drive(4, 3)]));
        loopConstruction.Validate(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle);

        await using (var loopHarness = await WorkshopIntegrationHarness.Running(loopConstruction))
        {
            await loopHarness.Advance(48);
            var read = loopHarness.Committed;
            var tx = SingleTransmission(read, new(3));
            Assert.Equal(TransmissionEngagementPhase.Engaged, tx.Phase);
            var cIds = loopConstruction.Instances.Identities[new(3)];
            var rId = loopConstruction.Instances.Identities[new(4)].Hinges[0].Body;
            Assert.True(BodyAngular(read, cIds.Hinges[0].Body).Length() < IdleSpeedAllowance);
            Assert.True(BodyAngular(read, cIds.Hinges[1].Body).Length() < IdleSpeedAllowance);
            Assert.True(BodyAngular(read, rId).Length() < IdleSpeedAllowance);
            Assert.Equal(0f, PhysicsGpuAbi.ReadRevoluteState(loopHarness.Device.CommittedWorld.ToArray(), 2).Work);
        }

        // Duplicate DriveIn on Clutch is rejected.
        var m1 = MotorPart(10, position: new(-8f, RigY, 0f));
        var m2 = MotorPart(11, position: new(-5.5f, RigY, 0f));
        var targetClutch = ClutchPart(12);
        var duplicateDriveIn = new WorkshopConnections(Drive(10, 12), Drive(11, 12));
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection,
            () => duplicateDriveIn.Validate(RuntimeTestServices.Core.Supply, new(m1, m2, targetClutch)));

        static float Degrees(float deg) => deg * (MathF.PI / 180f);
    }

    [Fact]
    public async Task RunResetAndSaveLoad_RestoreAuthoredStateAndReproduceCoupling()
    {
        var q = Quaternion.CreateFromYawPitchRoll(40f * MathF.PI / 180f, 25f * MathF.PI / 180f, 15f * MathF.PI / 180f);
        var battery = PlacedParts.Battery(new(1), -8.5f, RigY, 0f);
        var motor = MotorPart(2, speed: 6f, torque: 20f, position: new(-5.5f, RigY, 0f));
        var clutch = ClutchPart(3, closeSeconds: 1.1f, position: new(-2.5f, RigY, 0f), rotation: q);
        var conveyor = ConveyorPart(4, position: new(2f, RigY, 0f));
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(battery, motor, clutch, conveyor), default,
            new([Power(1, 2), Power(1, 3), Drive(2, 3), Drive(3, 4)]));

        var ids = construction.Instances.Identities[new(3)];
        var initialRotations = new CanonicalRotation[2];


        await using (var harness = await WorkshopIntegrationHarness.Building(construction))
        {
            Assert.True(harness.Committed.Bodies.TryGet(ids.Hinges[0].Body, out var initIn));
            Assert.True(harness.Committed.Bodies.TryGet(ids.Hinges[1].Body, out var initOut));
            initialRotations[0] = initIn.Rotation;
            initialRotations[1] = initOut.Rotation;

            await harness.Run();
            await harness.Advance(144);
            Assert.Equal(TransmissionEngagementPhase.Engaged, SingleTransmission(harness.Committed, new(3)).Phase);

            // Reset must restore exact admission state, rotations, zero speeds, and zero work.
            await harness.Reset();
            var resetRead = harness.Committed;
            var resetTx = SingleTransmission(resetRead, new(3));
            Assert.Equal(0f, resetTx.Closure);
            Assert.Equal(TransmissionEngagementPhase.Open, resetTx.Phase);
            Assert.False(ReadTransmissionSupplied(harness, ids.Transmissions[new(1)]));
            Assert.Equal(new RevoluteStateRead(false, 0f, 0f),
                PhysicsGpuAbi.ReadRevoluteState(harness.Device.CommittedWorld.ToArray(), 0));
            Assert.Equal(new RevoluteStateRead(false, 0f, 0f),
                PhysicsGpuAbi.ReadRevoluteState(harness.Device.CommittedWorld.ToArray(), 1));
            Assert.Equal(new RevoluteStateRead(false, 0f, 0f),
                PhysicsGpuAbi.ReadRevoluteState(harness.Device.CommittedWorld.ToArray(), 2));
            Assert.Equal(Vector3.Zero, BodyAngular(resetRead, ids.Hinges[0].Body));
            Assert.Equal(Vector3.Zero, BodyAngular(resetRead, ids.Hinges[1].Body));
            Assert.True(resetRead.Bodies.TryGet(ids.Hinges[0].Body, out var rstIn));
            Assert.True(resetRead.Bodies.TryGet(ids.Hinges[1].Body, out var rstOut));
            Assert.Equal(initialRotations[0], rstIn.Rotation);
            Assert.Equal(initialRotations[1], rstOut.Rotation);
        }

        // Save & Load round-trip must preserve close_seconds = 1.1, rotation, wiring, and replay identically.
        var saved = new WorkshopSavedConstruction(construction, new(5));
        var encoded = WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, saved);
        var loaded = WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands,
            RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, SaveFixture.Elements, encoded);
        Assert.Equal(saved.NextBodyId, loaded.NextBodyId);
        Assert.Equal(saved.Construction.Settings, loaded.Construction.Settings);
        Assert.Equal(saved.Construction.Connections, loaded.Construction.Connections);
        Assert.Equal(saved.Construction.Instances.Count, loaded.Construction.Instances.Count);
        var loadedClutch = Assert.IsType<WorkshopGenericInstance>(loaded.Construction.Instances[2]);
        Assert.Equal(1.1f, loadedClutch.Parameters[new("close_seconds")]);
        Assert.Equal(clutch.Rotation, loadedClutch.Rotation);

        await using var replayHarness = await WorkshopIntegrationHarness.Running(loaded.Construction);
        var replayIds = loaded.Construction.Instances.Identities[new(3)];
        Assert.Equal(TransmissionEngagementPhase.Open, SingleTransmission(replayHarness.Committed, new(3)).Phase);
        await replayHarness.Advance(60);
        Assert.Equal(TransmissionEngagementPhase.Closing, SingleTransmission(replayHarness.Committed, new(3)).Phase);
        await replayHarness.Advance(84);
        var replay = replayHarness.Committed;
        Assert.Equal(TransmissionEngagementPhase.Engaged, SingleTransmission(replay, new(3)).Phase);
        var input = BodyAngular(replay, replayIds.Hinges[0].Body);
        var output = BodyAngular(replay, replayIds.Hinges[1].Body);
        Assert.True(input.Length() > 1f);
        Assert.InRange((output - input).Length(), 0f, CoupledSpeedAllowance);

    }

    // ---- Unfamiliar synthetic generality tests (spec (a)-(f)) ----

    [Fact]
    public async Task SyntheticTwoTransmissionPackage_KeepsSlotsAndReadsIndependent()
    {
        var template = TestBundle.Entry(ClutchId);
        var shaftShape = template.Mechanical!.Hinges[0].MovingBody.Colliders[0];
        var shaftMass = template.Mechanical.Hinges[0].MovingBody.MassFrame;
        RevoluteHingeDescriptor HingeAt(float x, float y) => new(
            new(0),
            new(new(x, y, 0.43f), Quaternion.Identity), new(Vector3.Zero, Quaternion.Identity),
            -MathF.PI, MathF.PI, RevoluteTravel.Continuous,
            MechanicalValue.Fixed(0f), MechanicalValue.Fixed(0f), MechanicalValue.Fixed(0f), MechanicalValue.Fixed(0f),
            false, new(shaftMass, [shaftShape]), null);

        var entry = template with
        {
            Metadata = template.Metadata with { Id = "synthetic_two_tx", PaletteOrder = 250, Parameters = [] },
            Footprint = new(5, 4, 0, 4),
            Ports =
            [
                new(PortId.DriveIn, SimulationDomain.Mechanical, PortDirection.Input, PortRole.DriveIn, new("DriveIn", SocketGlyph.Text), null,
                    new(-1.2f, 0f, 0.43f), Vector3.UnitZ, Coordinate: new(CoordinateKind.Revolute, 0), CoordinatePerRadian: 1f),
                new(PortId.Drive, SimulationDomain.Mechanical, PortDirection.Output, PortRole.Drive, new("Drive", SocketGlyph.Text), null,
                    new(1.2f, -0.8f, 0.43f), Vector3.UnitZ, Coordinate: new(CoordinateKind.Revolute, 3), CoordinatePerRadian: 1f),
                new(PortId.PowerIn, SimulationDomain.Electrical, PortDirection.Input, PortRole.PowerIn, new("PowerIn", SocketGlyph.Text), null,
                    new(-0.55f, 0f, 0f), -Vector3.UnitX)
            ],
            Mechanical = template.Mechanical with
            {
                Hinges = [HingeAt(-1.2f, 0.8f), HingeAt(0f, 0.8f), HingeAt(0f, -0.8f), HingeAt(1.2f, -0.8f)],
                Transmissions =
                [
                    // Slot 1: timed transmission (0 -> 1); Slot 2: untimed fixed gear ratio (2 -> 3)
                    new(new(1), new(CoordinateKind.Revolute, 0), new(CoordinateKind.Revolute, 1), MechanicalValue.Fixed(1f), TransmissionConstraintKind.Bilateral, MechanicalValue.Fixed(0.2f)),
                    new(new(2), new(CoordinateKind.Revolute, 2), new(CoordinateKind.Revolute, 3), MechanicalValue.Fixed(-1.5f), TransmissionConstraintKind.Bilateral, null)
                ]
            },
            Electrical = template.Electrical! with
            {
                SupplyGraph = new(
                    [new SupplyConsumerDescriptor(new("consumer"), [new SupplyTransmissionTarget(new(1))])],
                    [],
                    [new(PortId.PowerIn, new(new("consumer"), SupplyTerminal.PowerIn))])
            },
            Presentation = new(1, PresentationAxes.None, [],
            [
                new("Visual/S1", new(1), Vector3.Zero),
                new("Visual/S2", new(2), Vector3.Zero),
                new("Visual/S3", new(3), Vector3.Zero),
                new("Visual/S4", new(4), Vector3.Zero)
            ], [], [])
        };
        Assert.Empty(BundleValidator.Validate([new("synthetic", entry)]));

        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var bundleEntries = DeclaredPhysics.Entries.Values.Append(entry).ToArray();
        using var physicsScope = DeclaredPhysics.Scoped([entry]);
        using var portScope = DeclaredPorts.Scoped(ElementPortCatalogue.FromBundle(bundleEntries));

        var syntheticId = new PartId("synthetic_two_tx");
        var b = PlacedParts.Battery(new(1), -8.5f, RigY, 0f, enabled: ElectricalEnable.Enabled);
        var m = MotorPart(2, speed: 6f, torque: 20f, position: new(-5.5f, RigY, 0f));
        var twoTx = WorkshopInput.Generic(RuntimeTestServices.Bundle, syntheticId, new(3), 0f, RigY, 0f, 0f, 0f, 0f, 1f);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(b, m, twoTx), default, new([Power(1, 2), Power(1, 3), Drive(2, 3)]));

        await harness.Require(WorkshopCommandKind.Construct, construction);

        var ids = construction.Instances.Identities[new(3)];
        var tx1Id = ids.Transmissions[new(1)];
        var tx2Id = ids.Transmissions[new(2)];
        Assert.NotEqual(tx1Id, tx2Id);

        // At admission: timed transmission 1 starts Open (0), untimed transmission 2 starts Engaged (1).
        var initReads = harness.Committed.Presentation.Transmissions.ToArray();
        Assert.Equal(2, initReads.Length);
        var init1 = Assert.Single(initReads, r => r.Transmission == tx1Id);
        var init2 = Assert.Single(initReads, r => r.Transmission == tx2Id);
        Assert.Equal((0f, TransmissionEngagementPhase.Open, new GpuBodyId(3)), (init1.Closure, init1.Phase, init1.Owner));
        Assert.Equal((1f, TransmissionEngagementPhase.Engaged, new GpuBodyId(3)), (init2.Closure, init2.Phase, init2.Owner));

        await harness.Run();
        await harness.Advance(10); // Timed slot still Closing, untimed slot remains Engaged cleanly.
        var midReads = harness.Committed.Presentation.Transmissions.ToArray();
        var mid1 = Assert.Single(midReads, r => r.Transmission == tx1Id);
        var mid2 = Assert.Single(midReads, r => r.Transmission == tx2Id);
        Assert.Equal(TransmissionEngagementPhase.Closing, mid1.Phase);
        Assert.InRange(mid1.Closure, 0.35f, 0.5f);
        Assert.Equal(TransmissionEngagementPhase.Engaged, mid2.Phase);
        Assert.Equal(1f, mid2.Closure);

        await harness.Advance(20); // Timed slot finishes Closing independently.
        var endReads = harness.Committed.Presentation.Transmissions.ToArray();
        var end1 = Assert.Single(endReads, r => r.Transmission == tx1Id);
        var end2 = Assert.Single(endReads, r => r.Transmission == tx2Id);
        Assert.Equal((1f, TransmissionEngagementPhase.Engaged), (end1.Closure, end1.Phase));
        Assert.Equal((1f, TransmissionEngagementPhase.Engaged), (end2.Closure, end2.Phase));
        Assert.True(BodyAngular(harness.Committed, ids.Hinges[1].Body).Z > 1f);
    }

    [Fact]
    public async Task SyntheticNegatedRatioTimedTransmission_EngagesAndDrivesReversed()
    {
        var template = TestBundle.Entry(ClutchId);
        var entry = template with
        {
            Metadata = template.Metadata with { Id = "synthetic_neg_ratio_clutch", PaletteOrder = 251, Parameters = [] },
            Mechanical = template.Mechanical! with
            {
                Transmissions = [new(new(1), new(CoordinateKind.Revolute, 0), new(CoordinateKind.Revolute, 1),
                    MechanicalValue.Fixed(-2.5f), TransmissionConstraintKind.Bilateral, MechanicalValue.Fixed(0.1f))]
            }
        };
        Assert.Empty(BundleValidator.Validate([new("synthetic", entry)]));

        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var bundleEntries = DeclaredPhysics.Entries.Values.Append(entry).ToArray();
        using var physicsScope = DeclaredPhysics.Scoped([entry]);
        using var portScope = DeclaredPorts.Scoped(ElementPortCatalogue.FromBundle(bundleEntries));

        var partId = new PartId("synthetic_neg_ratio_clutch");
        var b = PlacedParts.Battery(new(1), -8.5f, RigY, 0f);
        var m = MotorPart(2, speed: 4f, torque: 20f);
        var negClutch = WorkshopInput.Generic(RuntimeTestServices.Bundle, partId, new(3), -2.5f, RigY, 0f, 0f, 0f, 0f, 1f);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(b, m, negClutch), default, new([Power(1, 2), Power(1, 3), Drive(2, 3)]));

        await harness.Require(WorkshopCommandKind.Construct, construction);
        await harness.Run();
        var ids = construction.Instances.Identities[new(3)];

        await harness.Advance(6); // Still Closing at 0.05 s
        Assert.Equal(TransmissionEngagementPhase.Closing, SingleTransmission(harness.Committed, new(3)).Phase);
        Assert.True(MathF.Abs(BodyAngular(harness.Committed, ids.Hinges[1].Body).Z) < IdleSpeedAllowance);

        await harness.Advance(18); // Engaged well past 0.1 s (12 ticks)
        var r = harness.Committed;
        Assert.Equal(TransmissionEngagementPhase.Engaged, SingleTransmission(r, new(3)).Phase);
        var wIn = BodyAngular(r, ids.Hinges[0].Body).Z;
        var wOut = BodyAngular(r, ids.Hinges[1].Body).Z;
        Assert.True(wIn > 1f);
        Assert.InRange(wOut - (-2.5f * wIn), -0.1f, 0.1f);
    }

    [Fact]
    public async Task SyntheticRevoluteDriveAndTimedTransmissionOnSameHinge_ComposesDirectly()
    {
        var template = TestBundle.Entry(ClutchId);
        var motorTemplate = TestBundle.Entry(MotorId);
        var driveSpec = motorTemplate.Mechanical!.Hinges[0].Drive!.Value;
        var drivenInputHinge = template.Mechanical!.Hinges[0] with
        {
            Drive = driveSpec with { TargetSpeed = MechanicalValue.Fixed(5f), MaxTorque = MechanicalValue.Fixed(20f) }
        };
        var entry = template with
        {
            Metadata = template.Metadata with { Id = "synthetic_composed_clutch", PaletteOrder = 252, Parameters = [] },
            Ports = [.. template.Ports.Where(p => p.Id != PortId.DriveIn)],
            Mechanical = template.Mechanical with
            {
                Hinges = [drivenInputHinge, template.Mechanical.Hinges[1]],
                Transmissions = [new(new(1), new(CoordinateKind.Revolute, 0), new(CoordinateKind.Revolute, 1),
                    MechanicalValue.Fixed(1f), TransmissionConstraintKind.Bilateral, MechanicalValue.Fixed(0.15f))]
            },
            Electrical = template.Electrical! with
            {
                SupplyGraph = new(
                    [new SupplyConsumerDescriptor(new("consumer"),
                        [new SupplyDriveTarget(new(CoordinateKind.Revolute, 0)), new SupplyTransmissionTarget(new(1))])],
                    [],
                    [new(PortId.PowerIn, new(new("consumer"), SupplyTerminal.PowerIn))])
            }
        };
        Assert.Empty(BundleValidator.Validate([new("synthetic", entry)]));

        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var bundleEntries = DeclaredPhysics.Entries.Values.Append(entry).ToArray();
        using var physicsScope = DeclaredPhysics.Scoped([entry]);
        using var portScope = DeclaredPorts.Scoped(ElementPortCatalogue.FromBundle(bundleEntries));

        var partId = new PartId("synthetic_composed_clutch");
        var b = PlacedParts.Battery(new(1), -8.5f, RigY, 0f);
        var composed = WorkshopInput.Generic(RuntimeTestServices.Bundle, partId, new(3), 0f, RigY, 0f, 0f, 0f, 0f, 1f);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(b, composed), default, new([Power(1, 3)]));

        await harness.Require(WorkshopCommandKind.Construct, construction);
        await harness.Run();
        var ids = construction.Instances.Identities[new(3)];

        // During Closing (< 18 ticks): RevoluteDrive accelerates hinge 0 immediately while hinge 1 waits decoupled.
        await harness.Advance(10);
        var mid = harness.Committed;
        Assert.Equal(TransmissionEngagementPhase.Closing, SingleTransmission(mid, new(3)).Phase);
        Assert.True(BodyAngular(mid, ids.Hinges[0].Body).Z > 3f);
        Assert.True(MathF.Abs(BodyAngular(mid, ids.Hinges[1].Body).Z) < IdleSpeedAllowance);

        // After 18 ticks (0.15 s at 120 Hz): timed transmission engages and drives hinge 1 cleanly.
        await harness.Advance(16);
        var post = harness.Committed;
        Assert.Equal(TransmissionEngagementPhase.Engaged, SingleTransmission(post, new(3)).Phase);
        var w0 = BodyAngular(post, ids.Hinges[0].Body).Z;
        var w1 = BodyAngular(post, ids.Hinges[1].Body).Z;
        Assert.InRange(w0, 4.8f, 5.1f);
        Assert.InRange(w1 - w0, -0.05f, 0.05f);
    }

    [Fact]
    public void MalformedMechanicalTransmissionBundles_RefusedAtValidationAndSchema()
    {
        var valid = TestBundle.Entry(ClutchId);
        var baseMechanical = valid.Mechanical!;
        var baseTx = baseMechanical.Transmissions[0];

        // 1. LocalMechanicalTransmissionId = 0 rejected by validator & JSON schema decode.
        var zeroIdEntry = valid with
        {
            Mechanical = baseMechanical with { Transmissions = [baseTx with { Id = new(0) }] }
        };
        Assert.Contains(BundleValidator.Validate([new("bad", zeroIdEntry)]), e => e.Code == BundleErrorCode.DuplicateId);

        var json = JsonNode.Parse(BundleSchema.WriteEntries([valid]))!;
        json["Elements"]![0]!["Mechanical"]!["Transmissions"]![0]!["Id"] = 0;
        Assert.Throws<JsonException>(() => BundleSchema.Read(json.ToJsonString()));

        // 2. Wrong-unit close_seconds parameter.
        var wrongUnitEntry = valid with
        {
            Metadata = valid.Metadata with
            {
                Parameters = [valid.Metadata.Parameters[0] with { Unit = ParameterUnit.Metre }]
            }
        };
        Assert.Contains(BundleValidator.Validate([new("bad", wrongUnitEntry)]),
            e => e.Code == BundleErrorCode.OutOfRange && e.Detail.Contains("CloseSeconds", StringComparison.Ordinal));

        // 3. Out-of-range / non-finite constant CloseSeconds.
        foreach (var badClose in new[] { -0.1f, -0f, float.MaxValue, float.NaN, float.PositiveInfinity })
        {
            var badEntry = valid with
            {
                Metadata = valid.Metadata with { Parameters = [] },
                Mechanical = baseMechanical with { Transmissions = [baseTx with { CloseSeconds = MechanicalValue.Fixed(badClose) }] },
                Presentation = valid.Presentation with { Bindings = [] }
            };
            Assert.NotEmpty(BundleValidator.Validate([new("bad", badEntry)]));
        }

        // 4. Timed transmission missing SupplyGraph consumer target.
        var noSupplyEntry = valid with { Electrical = new([], new([], [], [])) };
        Assert.Contains(BundleValidator.Validate([new("bad", noSupplyEntry)]),
            e => e.Detail.Contains("Transmission.MissingConsumer", StringComparison.Ordinal));

        // 5. SupplyTransmissionTarget pointing at an untimed transmission or non-existent id.
        var untimedWithSupply = valid with
        {
            Metadata = valid.Metadata with { Parameters = [] },
            Mechanical = baseMechanical with { Transmissions = [baseTx with { CloseSeconds = null }] },
            Presentation = valid.Presentation with { Bindings = [] }
        };
        Assert.Contains(BundleValidator.Validate([new("bad", untimedWithSupply)]),
            e => e.Detail.Contains("Consumer.Target", StringComparison.Ordinal));

        var zeroCloseWithSupply = valid with
        {
            Metadata = valid.Metadata with { Parameters = [] },
            Mechanical = baseMechanical with { Transmissions = [baseTx with { CloseSeconds = MechanicalValue.Fixed(0f) }] },
            Presentation = valid.Presentation with { Bindings = [] }
        };
        Assert.NotEmpty(BundleValidator.Validate([new("bad", zeroCloseWithSupply)]));

        var missingTargetSupply = valid with
        {
            Electrical = valid.Electrical! with
            {
                SupplyGraph = new(
                    [new SupplyConsumerDescriptor(new("consumer"), [new SupplyTransmissionTarget(new(99))])],
                    [],
                    valid.Electrical.SupplyGraph!.Ports)
            }
        };
        Assert.Contains(BundleValidator.Validate([new("bad", missingTargetSupply)]),
            e => e.Detail.Contains("Consumer.Target", StringComparison.Ordinal));

        // 6. Presentation CommittedTransmissionClosure missing or mismatched Transmission id.
        var missingBindingTx = valid with
        {
            Presentation = valid.Presentation with
            {
                Bindings = [valid.Presentation.Bindings[0] with { Transmission = null }]
            }
        };
        Assert.Contains(BundleValidator.Validate([new("bad", missingBindingTx)]),
            e => e.Detail.Contains("Presentation.Transmission", StringComparison.Ordinal));

        var wrongBindingTx = valid with
        {
            Presentation = valid.Presentation with
            {
                Bindings = [valid.Presentation.Bindings[0] with { Transmission = new(99) }]
            }
        };
        Assert.Contains(BundleValidator.Validate([new("bad", wrongBindingTx)]),
            e => e.Detail.Contains("Presentation.Transmission", StringComparison.Ordinal));
    }

    [Fact]
    public async Task MalformedTransmissionAbiCandidate_RejectedByValidateCandidate()
    {
        var construction = StandardConveyorRig(closeSeconds: 0.2f, includeBall: false);
        await using var harness = await WorkshopIntegrationHarness.Running(construction);

        var baseline = harness.Device.CommittedWorld.ToArray();
        await harness.AdvanceOne();
        var stepped = harness.Device.CommittedWorld.ToArray();
        var timedId = construction.Instances.Identities[new(3)].Transmissions[new(1)];
        var txOffset = harness.Simulation.Scene.Physics.Layout.GetMechanicalTransmissionOffset(timedId.Value);
        PhysicsGpuAbi.ValidateCandidate(stepped, baseline, new(1), PhysicsInteractionSnapshot.Empty, PhysicsInteractionSnapshot.Empty);
        var untimed = harness.Simulation.Scene.Physics.Transmissions.ToArray().First(t => t.Owner == default);
        var untimedOffset = harness.Simulation.Scene.Physics.Layout.GetMechanicalTransmissionOffset(untimed.Id.Value);
        foreach (var field in new[] { PhysicsGpuAbi.MechanicalTransmissionSuppliedOffset, PhysicsGpuAbi.MechanicalTransmissionOriginOrdinalOffset })
        {
            var bad = (byte[])stepped.Clone();
            BinaryPrimitives.WriteUInt32LittleEndian(bad.AsSpan(untimedOffset + field), 1u);
            Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(bad, baseline, new(1), PhysicsInteractionSnapshot.Empty, PhysicsInteractionSnapshot.Empty));
        }
        var tickOneInteractions = PhysicsInteractionSnapshot.Empty;

        // Invalid timer state, Closure / Phase combinations and declaration changes refuse atomically.
        var mutations = new (Action<byte[]> Mutate, byte[] Prev, SimulationTick Tick)[]
        {
            (b => BinaryPrimitives.WriteUInt64LittleEndian(b.AsSpan(txOffset + PhysicsGpuAbi.MechanicalTransmissionOriginOrdinalOffset), ulong.MaxValue), baseline, new(1)),
            (b => BinaryPrimitives.WriteSingleLittleEndian(b.AsSpan(txOffset + PhysicsGpuAbi.MechanicalTransmissionOriginClosureOffset), float.NaN), baseline, new(1)),
            (b => BinaryPrimitives.WriteSingleLittleEndian(b.AsSpan(txOffset + PhysicsGpuAbi.MechanicalTransmissionOriginClosureOffset), 1.1f), baseline, new(1)),
            (b => BinaryPrimitives.WriteSingleLittleEndian(b.AsSpan(txOffset + PhysicsGpuAbi.MechanicalTransmissionClosureOffset), -0.1f), baseline, new(1)),
            (b => BinaryPrimitives.WriteSingleLittleEndian(b.AsSpan(txOffset + PhysicsGpuAbi.MechanicalTransmissionClosureOffset), 1.1f), baseline, new(1)),
            (b => BinaryPrimitives.WriteSingleLittleEndian(b.AsSpan(txOffset + PhysicsGpuAbi.MechanicalTransmissionClosureOffset), float.NaN), baseline, new(1)),
            (b => BinaryPrimitives.WriteSingleLittleEndian(b.AsSpan(txOffset + PhysicsGpuAbi.MechanicalTransmissionClosureOffset), -0f), baseline, new(1)),
            (b => BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(txOffset + PhysicsGpuAbi.MechanicalTransmissionPhaseOffset), 99u), baseline, new(1)),
            (b => BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(txOffset + PhysicsGpuAbi.MechanicalTransmissionSuppliedOffset), 0u), baseline, new(1)),
            (b =>
            {
                BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(txOffset + PhysicsGpuAbi.MechanicalTransmissionPhaseOffset), (uint)TransmissionEngagementPhase.Open);
                BinaryPrimitives.WriteSingleLittleEndian(b.AsSpan(txOffset + PhysicsGpuAbi.MechanicalTransmissionClosureOffset), 0.5f);
            }, baseline, new(1)),
            (b =>
            {
                BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(txOffset + PhysicsGpuAbi.MechanicalTransmissionPhaseOffset), (uint)TransmissionEngagementPhase.Engaged);
                BinaryPrimitives.WriteSingleLittleEndian(b.AsSpan(txOffset + PhysicsGpuAbi.MechanicalTransmissionClosureOffset), 0.5f);
            }, baseline, new(1)),
            (b =>
            {
                BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(txOffset + PhysicsGpuAbi.MechanicalTransmissionPhaseOffset), (uint)TransmissionEngagementPhase.Closing);
                BinaryPrimitives.WriteSingleLittleEndian(b.AsSpan(txOffset + PhysicsGpuAbi.MechanicalTransmissionClosureOffset), 0f);
            }, baseline, new(1)),
            (b =>
            {
                BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(txOffset + PhysicsGpuAbi.MechanicalTransmissionPhaseOffset), (uint)TransmissionEngagementPhase.Opening);
                BinaryPrimitives.WriteSingleLittleEndian(b.AsSpan(txOffset + PhysicsGpuAbi.MechanicalTransmissionClosureOffset), 0f);
            }, baseline, new(1)),
            (b => BinaryPrimitives.WriteSingleLittleEndian(b.AsSpan(txOffset + PhysicsGpuAbi.MechanicalTransmissionCloseSecondsOffset), 0.5f), baseline, new(1))
        };

        foreach (var (mutate, prev, expectedTick) in mutations)
        {
            var bad = (byte[])stepped.Clone();
            mutate(bad);
            Assert.Throws<ArgumentException>(() =>
                PhysicsGpuAbi.ValidateCandidate(bad, prev, expectedTick, tickOneInteractions, PhysicsInteractionSnapshot.Empty));
        }
    }

    [Fact]
    public void MalformedActuatorWireRecordWithMechanicalTransmissionKind_ThrowsWireFault()
    {
        var actuator = new ActuatorDriveRead(SupplyConsumer.Revolute(new(42)), new(99), true, false, 1f, 6f, -MathF.PI, MathF.PI);
        var stamp = new WorkshopClockStamp(WorkshopClockDomain.SimulationMonotonic, new ClockGeneration(1), new(1_000_000), new(100));
        var canonical = new CanonicalBody(new(1), 1, 1, default, default, default);
        var bodies = new PhysicsBodyReadSet([new PhysicsBodyRead(canonical, CanonicalRotation.Identity, default, default)]);
        var motion = PhysicsMotionRead.Decode(WireFaultTests.MotionBytes(canonical), bodies, new(1));
        var read = new WorkshopRead(
            new SimulationEpoch(1),
            new SimulationTick(1), new PhysicalCommitRevision(1),
            bodies,
            new(1),
            stamp,
            Motion: motion,
            Presentation: new PhysicsPresentationRead([actuator], [], [], [], [], [], []));
        var response = new WorkshopResponse(
            default,
            WorkshopResponseKind.Read,
            new(WorkshopCommandOutcome.Applied, WorkshopRejection.None),
            WorkshopSimulationPhase.Running,
            read,
            Session: new(1, 1),
            Publication: new(1),
            Cadence: new(1),
            MasterGeneration: new(1),
            Projection: new(1),
            SourcePulse: new(1));

        var responseBytes = WorkshopWireFlatBuffers.EncodeResponse(response);
        Span<byte> actuatorSignature = stackalloc byte[20];
        BinaryPrimitives.WriteUInt64LittleEndian(actuatorSignature, 42UL);
        BinaryPrimitives.WriteUInt64LittleEndian(actuatorSignature[8..], 99UL);
        BinaryPrimitives.WriteUInt32LittleEndian(actuatorSignature[16..], (uint)SupplyConsumerKind.RevoluteDrive);
        var offset = responseBytes.AsSpan().IndexOf(actuatorSignature);
        Assert.True(offset >= 0, "Could not locate encoded ActuatorDriveRecord pattern in FlatBuffer response.");

        var mutatedResponse = (byte[])responseBytes.Clone();
        BinaryPrimitives.WriteUInt32LittleEndian(mutatedResponse.AsSpan(offset + 16), (uint)SupplyConsumerKind.MechanicalTransmission);
        var responseFault = Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeResponse(mutatedResponse));
        Assert.Equal(WireFaultReason.OutOfRange, responseFault.Reason);
    }

    private static byte[] BuildSavedConstructionWithRawCloseSeconds(WorkshopSavedConstruction save, float rawCloseSeconds)
    {
        var builder = new FlatBufferBuilder(512);
        var inst = Assert.IsType<WorkshopGenericInstance>(Assert.Single(save.Construction.Instances));
        var partIdOffset = builder.CreateString(inst.PartId.Value);
        var paramVector = CuriousContraptions.Wire.InstanceRecord.CreateParametersVector(builder,
            [CuriousContraptions.Wire.ParameterRecord.CreateParameterRecord(builder, builder.CreateString("close_seconds"), rawCloseSeconds)]);

        CuriousContraptions.Wire.InstanceRecord.StartInstanceRecord(builder);
        CuriousContraptions.Wire.InstanceRecord.AddParameters(builder, paramVector);
        CuriousContraptions.Wire.InstanceRecord.AddId(builder, inst.Id.Value);
        CuriousContraptions.Wire.InstanceRecord.AddPartId(builder, partIdOffset);
        CuriousContraptions.Wire.InstanceRecord.AddLocked(builder, inst.Locked);
        CuriousContraptions.Wire.InstanceRecord.AddCell(builder, CuriousContraptions.Wire.Vec3i.CreateVec3i(builder, inst.Cell.X, inst.Cell.Y, inst.Cell.Z));
        CuriousContraptions.Wire.InstanceRecord.AddLocal(builder, CuriousContraptions.Wire.Vec3f.CreateVec3f(builder, inst.Local.X, inst.Local.Y, inst.Local.Z));
        CuriousContraptions.Wire.InstanceRecord.AddRotation(builder, CuriousContraptions.Wire.Quat4f.CreateQuat4f(builder, inst.Rotation.X, inst.Rotation.Y, inst.Rotation.Z, inst.Rotation.W));
        var instOffset = CuriousContraptions.Wire.InstanceRecord.EndInstanceRecord(builder);

        var instancesVector = CuriousContraptions.Wire.ConstructionRecord.CreateInstancesVector(builder, [instOffset]);
        CuriousContraptions.Wire.ConstructionRecord.StartConstructionRecord(builder);
        CuriousContraptions.Wire.ConstructionRecord.AddRevision(builder, save.Construction.Revision.Value);
        var s = save.Construction.Settings;
        CuriousContraptions.Wire.ConstructionRecord.AddSettings(builder,
            CuriousContraptions.Wire.CadenceSettingsRecord.CreateCadenceSettingsRecord(
                builder, (uint)s.Simulation, (uint)s.Physical, (uint)s.Animation, (uint)s.Presentation,
                s.PresentationRate.Numerator, s.PresentationRate.Denominator));
        CuriousContraptions.Wire.ConstructionRecord.AddInstances(builder, instancesVector);
        var crOffset = CuriousContraptions.Wire.ConstructionRecord.EndConstructionRecord(builder);
        return SaveFixture.Finish(builder, crOffset, save.NextBodyId.Value);
    }
}

/// <summary>Scene, declared binding, and physical shaft/spoke presentation tests for Clutch.</summary>
[Collection<NativeSceneCollectionDefinition>]
public sealed class ClutchArtTests(NativeSceneFixture godot)
{
    private static readonly Color Slate = new(85f / 255f, 101f / 255f, 115f / 255f); // #556573
    private static readonly Color Cyan = new(102f / 255f, 184f / 255f, 201f / 255f); // #66b8c9
    private static readonly Color Gold = new(247f / 255f, 203f / 255f, 82f / 255f); // #f7cb52

    private static void AssertColorApprox(Color expected, Color actual)
    {
        Assert.InRange(actual.R - expected.R, -0.01f, 0.01f);
        Assert.InRange(actual.G - expected.G, -0.01f, 0.01f);
        Assert.InRange(actual.B - expected.B, -0.01f, 0.01f);
    }

    private static void ApplyPresentation(WorkshopDeclaredPresentation evaluator, MachinePart part, GpuBodyId owner, ulong tick)
    {
        foreach (var sample in evaluator.Advance(AnimationTestTime.Seconds(tick / 120.0)))
        {
            if (sample.Owner == owner)
                part.ApplyDeclaredProperty(sample.Binding, sample.Value);
        }
    }

    private static void ApplyBodyPoses(WorkshopPhysicalPresentation presentation, MachinePart part, Node3D root,
        WorkshopRead read, params GpuBodyId[] ownedBodies)
    {
        Array.Sort(ownedBodies, (a, b) => a.Value.CompareTo(b.Value));
        var poses = new PresentedBody[ownedBodies.Length];
        for (var i = 0; i < ownedBodies.Length; i++)
        {
            Assert.True(read.Bodies.TryGet(ownedBodies[i], out var state));
            poses[i] = new PresentedBody(state.Body.Id, state.Body.Cell, state.Body.Local, state.Rotation);
        }
        presentation.Apply([part], root.GlobalTransform, new PresentedBodySet(poses));
    }

    [Fact]
    public async Task ClutchScene_AnimatesPlatesLampAndShaftSpokesAcrossLifecycleAndResetsCleanly()
    {
        var bMotor = PlacedParts.Battery(new(1), -8.5f, 4f, 0f, enabled: ElectricalEnable.Enabled);
        var bCoil = PlacedParts.Battery(new(6), -8.5f, 6f, 0f, enabled: ElectricalEnable.Enabled);
        var motor = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("motor"), new(2), -5.5f, 4f, 0f,
            0f, 0f, 0f, 1f, new([new(new("speed"), 6f), new(new("torque"), 20f)]));
        var clutch = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("clutch"), new(3), -2.5f, 4f, 0f,
            0f, 0f, 0f, 1f, new([new(new("close_seconds"), 0.2f)]));
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(bMotor, bCoil, motor, clutch), default,
            new([
                new(bMotor.Id, PortId.Supply, motor.Id, PortId.PowerIn, WorkshopConnectionDomain.Electrical),
                new(bCoil.Id, PortId.Supply, clutch.Id, PortId.PowerIn, WorkshopConnectionDomain.Electrical),
                new(motor.Id, PortId.Drive, clutch.Id, PortId.DriveIn, WorkshopConnectionDomain.Mechanical)
            ]));

        var registry = new PartRegistry(RuntimeTestServices.Core.Presentation, RuntimeTestServices.Bundle);
        registry.Load();
        var root = new Node3D();
        var part = registry.Create(new PartId("clutch"));
        part.AuthoredId = clutch.Id;
        part.Position = new Godot.Vector3(-2.5f, 4f, 0f);

        await using var harness = await WorkshopIntegrationHarness.Building(construction);
        var ids = construction.Instances.Identities[clutch.Id];
        var presentation = new WorkshopPhysicalPresentation();

        try
        {
            godot.Tree.Root.AddChild(root);
            root.AddChild(part);
            part.EnsureConstructed();
            part.InstallPhysicalIdentity(new PackageBodyIndex(1), ids.Hinges[0].Body, construction.Instances.Identities);
            part.InstallPhysicalIdentity(new PackageBodyIndex(2), ids.Hinges[1].Body, construction.Instances.Identities);

            var plateLeft = part.GetNode<Node3D>("Visual/PlateLeft");
            var plateRight = part.GetNode<Node3D>("Visual/PlateRight");
            var leftSpin = part.GetNode<Node3D>("Visual/PlateLeft/Spin");
            var rightSpin = part.GetNode<Node3D>("Visual/PlateRight/Spin");
            void AssertPlateGeometry()
            {
                var left = part.GetNode<MeshInstance3D>("Visual/PlateLeft/Spin/Visual_PlateDisc");
                var right = part.GetNode<MeshInstance3D>("Visual/PlateRight/Spin/Visual_PlateDisc");
                foreach (var disc in new[] { left, right })
                {
                    var shape = Assert.IsType<CylinderMesh>(disc.Mesh);
                    var normal = disc.GlobalBasis.Y.Normalized();
                    // Off-axis rim excursion must stay below the existing 1mm plate clearance allowance.
                    var lateral = MathF.Sqrt(normal.Y * normal.Y + normal.Z * normal.Z);
                    Assert.InRange(lateral * shape.TopRadius, 0f, 0.001f);
                }
                var height = Assert.IsType<CylinderMesh>(left.Mesh).Height;
                var gap = right.GlobalPosition.X - left.GlobalPosition.X - height;
                Assert.InRange(gap, -0.001f, 0.5f - height + 0.001f);
            }
            var lamp = part.GetNode<MeshInstance3D>("Visual/Lamp");
            var inputSpoke = part.GetNode<Node3D>("Visual/InputShaft/Visual_InputSpoke");
            var outputSpoke = part.GetNode<Node3D>("Visual/OutputShaft/Visual_OutputSpoke");
            var initialInputSpokeRot = inputSpoke.GlobalBasis;
            var initialOutputSpokeRot = outputSpoke.GlobalBasis;

            // Open admission baseline: plates at +/-0.25 m, lamp slate (#556573).
            ApplyPresentation(harness.Presentation!, part, clutch.Id, 0);
            ApplyBodyPoses(presentation, part, root, harness.Committed, ids.Hinges[0].Body, ids.Hinges[1].Body);
            AssertPlateGeometry();
            Assert.InRange(plateLeft.Position.X - (-0.25f), -0.001f, 0.001f);
            Assert.InRange(plateRight.Position.X - 0.25f, -0.001f, 0.001f);
            AssertColorApprox(Slate, Assert.IsType<StandardMaterial3D>(lamp.MaterialOverride).AlbedoColor);

            // Halfway through Closing (~12 ticks / 0.1 s): plates linearly near -/+0.17, lamp cyan (#66b8c9),
            // InputSpoke rotated, OutputSpoke unchanged.
            await harness.Run();
            await harness.Advance(12);
            ApplyPresentation(harness.Presentation!, part, clutch.Id, harness.Committed.Tick.Value);
            ApplyBodyPoses(presentation, part, root, harness.Committed, ids.Hinges[0].Body, ids.Hinges[1].Body);
            AssertPlateGeometry();
            var midClosure = harness.Committed.Presentation.Transmissions.ToArray().Single(r => r.Owner == clutch.Id).Closure;
            Assert.InRange(plateLeft.Position.X - float.Lerp(-0.25f, -0.09f, midClosure), -1e-3f, 1e-3f);
            Assert.InRange(plateRight.Position.X - float.Lerp(0.25f, 0.09f, midClosure), -1e-3f, 1e-3f);
            AssertColorApprox(Cyan, Assert.IsType<StandardMaterial3D>(lamp.MaterialOverride).AlbedoColor);
            Assert.False(inputSpoke.GlobalBasis.IsEqualApprox(initialInputSpokeRot));
            Assert.True(MathF.Abs(leftSpin.Rotation.Z) > 0.1f);
            Assert.Equal(0f, rightSpin.Rotation.Z);
            Assert.True(outputSpoke.GlobalBasis.IsEqualApprox(initialOutputSpokeRot));

            // Engaged (> 24 ticks): plates locked at -/+0.09, lamp gold (#f7cb52), both spokes rotated.
            await harness.Advance(24);
            ApplyPresentation(harness.Presentation!, part, clutch.Id, harness.Committed.Tick.Value);
            ApplyBodyPoses(presentation, part, root, harness.Committed, ids.Hinges[0].Body, ids.Hinges[1].Body);
            AssertPlateGeometry();
            Assert.InRange(plateLeft.Position.X - (-0.09f), -0.001f, 0.001f);
            Assert.InRange(plateRight.Position.X - 0.09f, -0.001f, 0.001f);
            AssertColorApprox(Gold, Assert.IsType<StandardMaterial3D>(lamp.MaterialOverride).AlbedoColor);
            Assert.True(MathF.Abs(rightSpin.Rotation.Z) > 0.1f);
            var engagedOutputRot = outputSpoke.GlobalBasis;
            Assert.False(engagedOutputRot.IsEqualApprox(initialOutputSpokeRot));

            // Mid-run coil cut -> Opening: plates reopen linearly toward +/-0.25, lamp returns to slate (#556573),
            // OutputSpoke keeps rotating smoothly while decoupled.
            await harness.Require(WorkshopCommandKind.ConfigureRunControl,
                runControl: harness.RunControl(bCoil.Id, RunControlLane.SourceEnable, RunControlValue.Of(ElectricalEnable.Disabled)));
            await harness.Advance(12);
            ApplyPresentation(harness.Presentation!, part, clutch.Id, harness.Committed.Tick.Value);
            ApplyBodyPoses(presentation, part, root, harness.Committed, ids.Hinges[0].Body, ids.Hinges[1].Body);
            AssertPlateGeometry();
            var openingClosure = harness.Committed.Presentation.Transmissions.ToArray().Single(r => r.Owner == clutch.Id).Closure;
            Assert.InRange(plateLeft.Position.X - float.Lerp(-0.25f, -0.09f, openingClosure), -1e-3f, 1e-3f);
            Assert.InRange(plateRight.Position.X - float.Lerp(0.25f, 0.09f, openingClosure), -1e-3f, 1e-3f);
            AssertColorApprox(Slate, Assert.IsType<StandardMaterial3D>(lamp.MaterialOverride).AlbedoColor);
            Assert.False(outputSpoke.GlobalBasis.IsEqualApprox(engagedOutputRot));

            // Reset restores all plates, lamp color, and shaft spoke poses cleanly without Godot errors.
            await harness.Reset();
            part.ResetPhysicalVisuals();
            ApplyPresentation(harness.Presentation!, part, clutch.Id, harness.Committed.Tick.Value);
            ApplyBodyPoses(presentation, part, root, harness.Committed, ids.Hinges[0].Body, ids.Hinges[1].Body);
            AssertPlateGeometry();
            Assert.InRange(plateLeft.Position.X - (-0.25f), -0.001f, 0.001f);
            Assert.InRange(plateRight.Position.X - 0.25f, -0.001f, 0.001f);
            AssertColorApprox(Slate, Assert.IsType<StandardMaterial3D>(lamp.MaterialOverride).AlbedoColor);
            Assert.True(inputSpoke.GlobalBasis.IsEqualApprox(initialInputSpokeRot));
            Assert.True(outputSpoke.GlobalBasis.IsEqualApprox(initialOutputSpokeRot));
            Assert.Equal(0f, leftSpin.Rotation.Z);
            Assert.Equal(0f, rightSpin.Rotation.Z);
            Assert.Empty(godot.Errors.Drain());
        }
        finally
        {
            part.GetParent()?.RemoveChild(part);
            part.Free();
            root.GetParent()?.RemoveChild(root);
            root.Free();
        }
    }
}
