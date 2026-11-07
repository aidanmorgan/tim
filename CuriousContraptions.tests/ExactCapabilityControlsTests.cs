using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;
using CuriousContraptions.Simulation;
using Xunit;
using Shape = CuriousContraptions.Abstractions.ColliderShapeKind;

namespace CuriousContraptions.Tests;

public sealed class ExactCapabilityControlsTests
{
    private static readonly PartId DualHingePart = new("dual_hinge_actuator");

    private static BundleEntry CreateDualActuatorEntry(
        float speed0 = 5f,
        float speed1 = 12f,
        bool reorderPresentation = false,
        bool invalidActuatorCoordinate = false,
        bool independentConsumers = true)
    {
        var motor = TestBundle.Entry(new("motor"));
        var baseHinge = motor.Mechanical!.Hinges[0];

        var hinge0 = baseHinge with
        {
            BaseFrame = new(new(0, 0, -1), Quaternion.Identity),
            Drive = new(MechanicalValue.FromParameter("speed_0"), MechanicalValue.Fixed(10f)),
            FrictionTorque = MechanicalValue.Fixed(0.05f),
            MovingBody = baseHinge.MovingBody with
            {
                MassFrame = baseHinge.MovingBody.MassFrame with { Gravity = Vector3.Zero }
            }
        };

        var hinge1 = baseHinge with
        {
            BaseFrame = new(new(0, 0, 1), Quaternion.Identity),
            Drive = new(MechanicalValue.FromParameter("speed_1"), MechanicalValue.Fixed(10f)),
            FrictionTorque = MechanicalValue.Fixed(0.05f),
            MovingBody = baseHinge.MovingBody with
            {
                MassFrame = baseHinge.MovingBody.MassFrame with { Gravity = Vector3.Zero }
            }
        };

        var slider0 = new PrismaticSliderDescriptor(
            BaseFrame: new(new(0, 1, 0), Quaternion.Identity),
            MovingFrame: new(Vector3.Zero, Quaternion.Identity),
            LowerLimit: 0f,
            UpperLimit: 1f,
            Stiffness: MechanicalValue.Fixed(20f),
            Damping: MechanicalValue.Fixed(2f),
            Compression: MechanicalValue.Fixed(0f),
            BodiesCollide: false,
            MovingBody: new(MassFrameDeclaration.Constant(0.5f, Vector3.Zero, new(0.01f), Quaternion.Identity, 0f, Vector3.Zero),
                [new(Shape.Box, new(0.1f, 0.1f, 0.1f), Vector3.Zero, Quaternion.Identity, new(0f, 0.6f, 0f, 0.1f), 0f)]),
            Drive: new(MechanicalValue.Fixed(0.5f), MechanicalValue.Fixed(2f), MechanicalValue.Fixed(10f), MechanicalValue.Fixed(50f), MechanicalValue.Fixed(50f)));

        PresentationBinding StateBinding(string node, CoordinateReference actuator, PresentationStateValues values) =>
            new(node, PresentationProperty.LocalX, PresentationDriver.CommittedActuatorState, PresentationValueMapping.StateValues,
                0, 1, PresentationCurve.Linear, 0, PresentationOverlap.Replace, 0,
                Actuator: actuator, StateValues: values);

        var coord0 = new CoordinateReference(CoordinateKind.Revolute, 0);
        var coord1 = invalidActuatorCoordinate
            ? new CoordinateReference(CoordinateKind.Revolute, 99)
            : new CoordinateReference(CoordinateKind.Revolute, 1);
        var coordSlider = new CoordinateReference(CoordinateKind.Prismatic, 0);

        var b0 = StateBinding("Hinge0Track", coord0, new(0.1f, 0.2f, 0.3f));
        var b1 = StateBinding("Hinge1Track", coord1, new(0.4f, 0.5f, 0.6f));
        var bSlider = StateBinding("SliderTrack", coordSlider, new(0.7f, 0.8f, 0.9f));

        PresentationBinding[] bindings = reorderPresentation
            ? [b1, bSlider, b0]
            : [b0, b1, bSlider];

        var ports = new[]
        {
            new PortDescriptor(PortId.PowerIn, SimulationDomain.Electrical, PortDirection.Input, PortRole.PowerIn, null, new(-0.55f, 0, 0), -Vector3.UnitX),
            new PortDescriptor(new PortId("power_in_1"), SimulationDomain.Electrical, PortDirection.Input, PortRole.PowerIn, null, new(-0.55f, 0, 0.5f), -Vector3.UnitX),
            new PortDescriptor(new PortId("power_in_slider"), SimulationDomain.Electrical, PortDirection.Input, PortRole.PowerIn, null, new(-0.55f, 0, -0.5f), -Vector3.UnitX)
        };

        var supplyGraph = independentConsumers
            ? new SupplyGraphDescriptor(
                [
                    new SupplyConsumerDescriptor(new("consumer0"),
                    [
                        new SupplyDriveTarget(new(CoordinateKind.Revolute, 0))
                    ]),
                    new SupplyConsumerDescriptor(new("consumer1"),
                    [
                        new SupplyDriveTarget(new(CoordinateKind.Revolute, 1))
                    ]),
                    new SupplyConsumerDescriptor(new("consumer_slider"),
                    [
                        new SupplyDriveTarget(new(CoordinateKind.Prismatic, 0))
                    ])
                ],
                [],
                [
                    new(PortId.PowerIn, new(new("consumer0"), SupplyTerminal.PowerIn)),
                    new(new PortId("power_in_1"), new(new("consumer1"), SupplyTerminal.PowerIn)),
                    new(new PortId("power_in_slider"), new(new("consumer_slider"), SupplyTerminal.PowerIn))
                ])
            : new SupplyGraphDescriptor(
                [
                    new SupplyConsumerDescriptor(new("consumer"),
                    [
                        new SupplyDriveTarget(new(CoordinateKind.Revolute, 0)),
                        new SupplyDriveTarget(new(CoordinateKind.Revolute, 1)),
                        new SupplyDriveTarget(new(CoordinateKind.Prismatic, 0))
                    ])
                ],
                [],
                [new(PortId.PowerIn, new(new("consumer"), SupplyTerminal.PowerIn))]);

        return motor with
        {
            Metadata = motor.Metadata with
            {
                Id = DualHingePart.Value,
                Parameters =
                [
                    new("speed_0", ParameterUnit.RadianPerSecond, ParameterKind.Real, -20f, 20f, speed0, [], RunEdit: ParameterRunEdit.RunControl),
                    new("speed_1", ParameterUnit.RadianPerSecond, ParameterKind.Real, -20f, 20f, speed1, [], RunEdit: ParameterRunEdit.RunControl)
                ],
                PaletteOrder = 203
            },
            Footprint = new(4, 3, 1, 2),
            Ports = ports,
            Mechanical = motor.Mechanical with
            {
                Hinges = [hinge0, hinge1],
                Sliders = [slider0]
            },
            Electrical = motor.Electrical! with
            {
                SupplyGraph = supplyGraph
            },
            Presentation = new(1f, PresentationAxes.None, bindings,
                [new("Body0", new(1), Vector3.Zero), new("Body1", new(2), Vector3.Zero), new("BodySlider", new(3), Vector3.Zero)], [], [])
        };
    }

    private static WorkshopConstruction Construction(bool connected = true)
    {
        var bodyId = new GpuBodyId(2);
        var instances = new WorkshopInstances(
            PlacedParts.Battery(new(1), -4, 4, 0, enabled: ElectricalEnable.Enabled),
            WorkshopInput.Generic(RuntimeTestServices.Bundle, DualHingePart, bodyId, 0, 4, 0, 0, 0, 0, 1));
        var connections = connected
            ? new WorkshopConnections(new WorkshopConnection(new(1), PortId.Supply, bodyId, PortId.PowerIn, WorkshopConnectionDomain.Electrical))
            : new WorkshopConnections();
        return new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), instances, Connections: connections);
    }

    private static WorkshopResponse Response(WorkshopRead read, ulong publication) =>
        new(default, WorkshopResponseKind.Read, new(WorkshopCommandOutcome.Applied, WorkshopRejection.None),
            WorkshopSimulationPhase.Running, read, new(101, 206), new(publication), new(1), new(1), new(1), new(read.Tick.Value));

    [Fact]
    public async Task MultipleDrivenHingesAndSliderCompileExactTargetLanesAndAdmitIndependentRunControls()
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());

        var entry = CreateDualActuatorEntry(speed0: 5f, speed1: 12f);
        Assert.Empty(BundleValidator.Validate([new("dual_hinge", entry)]));

        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(DualHingePart, entry.Ports)]));

        // Connect only PortId.PowerIn (powers Hinge 0; Hinge 1 and Slider 0 remain unpowered)
        var construction = Construction(connected: true);
        await harness.Require(WorkshopCommandKind.Construct, construction);
        await harness.Run();
        await harness.Advance(2);

        var scene = harness.Simulation.Scene;
        var owner = new GpuBodyId(2);

        // Verify two RunControl rows compiled for the revolute drives
        var rows = scene.RunControls.Where(r => r.Owner == owner && r.Lane == RunControlLane.RevoluteDriveTarget).ToArray();
        Assert.Equal(2, rows.Length);
        Assert.Equal(0, rows[0].LaneIndex);
        Assert.Equal(1, rows[1].LaneIndex);
        Assert.Equal(-20f, rows[0].Minimum);
        Assert.Equal(20f, rows[0].Maximum);
        Assert.Equal(-20f, rows[1].Minimum);
        Assert.Equal(20f, rows[1].Maximum);

        // Verify initial committed drive target read contains exact joints and speeds
        var read = harness.Committed;
        Assert.Equal(2, read.DriveTargets.Count);
        var identities = construction.Instances.Identities[owner];
        var joint0 = identities.Hinges[0].Joint;
        var joint1 = identities.Hinges[1].Joint;
        var jointSlider = identities.Sliders[0].Joint;

        Assert.Equal(joint0, read.DriveTargets[0].Joint);
        Assert.Equal(owner, read.DriveTargets[0].Owner);
        Assert.Equal(5f, read.DriveTargets[0].TargetSpeed);

        Assert.Equal(joint1, read.DriveTargets[1].Joint);
        Assert.Equal(owner, read.DriveTargets[1].Owner);
        Assert.Equal(12f, read.DriveTargets[1].TargetSpeed);

        // Committed reads can resolve run controls by exact joint or row
        Assert.Equal(5f, read.RunControl(owner, RunControlLane.RevoluteDriveTarget, joint: joint0).Scalar);
        Assert.Equal(12f, read.RunControl(owner, RunControlLane.RevoluteDriveTarget, joint: joint1).Scalar);
        Assert.Equal(5f, read.RunControl(rows[0]).Scalar);
        Assert.Equal(12f, read.RunControl(rows[1]).Scalar);

        // Verify differing actuator supply states
        var actHinge0 = read.Presentation.Actuators.ToArray().First(a => a.Drive == SupplyConsumer.Revolute(joint0));
        var actHinge1 = read.Presentation.Actuators.ToArray().First(a => a.Drive == SupplyConsumer.Revolute(joint1));
        var actSlider = read.Presentation.Actuators.ToArray().First(a => a.Drive == SupplyConsumer.Prismatic(jointSlider));

        Assert.True(actHinge0.SupplyClosed, "Hinge 0 is connected to battery and must be powered.");
        Assert.False(actHinge1.SupplyClosed, "Hinge 1 is not connected and must be unpowered.");
        Assert.False(actSlider.SupplyClosed, "Slider 0 is not connected and must be unpowered.");

        // Verify differing presentation indicator evaluation
        var endpoint = Response(read, 1);
        var compiled = WorkshopSceneCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, construction, new(1, 1), new(1));
        var evaluator = new WorkshopDeclaredPresentation(new CuriousContraptions.Presentation.AnimationServices(), compiled, endpoint);
        var samples = evaluator.Advance(AnimationTestTime.Seconds(read.Tick.Value / 120.0));

        var s0 = samples.First(s => s.Owner == owner && compiled.Presentation.Single(t => t.Owner == s.Owner && t.Binding == s.Binding).Actuator == SupplyConsumer.Revolute(joint0));
        var s1 = samples.First(s => s.Owner == owner && compiled.Presentation.Single(t => t.Owner == s.Owner && t.Binding == s.Binding).Actuator == SupplyConsumer.Revolute(joint1));
        var sSlider = samples.First(s => s.Owner == owner && compiled.Presentation.Single(t => t.Owner == s.Owner && t.Binding == s.Binding).Actuator == SupplyConsumer.Prismatic(jointSlider));

        Assert.Equal(0.2f, s0.Value.Number, 3); // Powered: State1 = 0.2
        Assert.Equal(0.4f, s1.Value.Number, 3); // Unpowered: State0 = 0.4
        Assert.Equal(0.7f, sSlider.Value.Number, 3); // Unpowered: State0 = 0.7

        // Edit control for Hinge 1 only: changes only Hinge 1 target speed to 18 rad/s
        var editHinge1 = WorkshopIntegrationHarness.RunControl(rows[1], RunControlValue.Of(18f));
        await harness.Require(WorkshopCommandKind.ConfigureRunControl, runControl: editHinge1);
        await harness.Advance(1);

        read = harness.Committed;
        Assert.Equal(5f, read.DriveTargets[0].TargetSpeed);
        Assert.Equal(18f, read.DriveTargets[1].TargetSpeed);
        Assert.Equal(5f, read.RunControl(rows[0]).Scalar);
        Assert.Equal(18f, read.RunControl(rows[1]).Scalar);

        // Slider state and indicator remain unchanged
        actSlider = read.Presentation.Actuators.ToArray().First(a => a.Drive == SupplyConsumer.Prismatic(jointSlider));
        Assert.False(actSlider.SupplyClosed);

        // Edit control for Hinge 0 only: changes only Hinge 0 target speed to -8 rad/s
        var editHinge0 = WorkshopIntegrationHarness.RunControl(rows[0], RunControlValue.Of(-8f));
        await harness.Require(WorkshopCommandKind.ConfigureRunControl, runControl: editHinge0);
        await harness.Advance(1);

        read = harness.Committed;
        Assert.Equal(-8f, read.DriveTargets[0].TargetSpeed);
        Assert.Equal(18f, read.DriveTargets[1].TargetSpeed);
        Assert.Equal(-8f, read.RunControl(rows[0]).Scalar);
        Assert.Equal(18f, read.RunControl(rows[1]).Scalar);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PresentationIndicatorsTrackExactActuatorsRegardlessOfDeclarationOrder(bool reordered)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());

        var entry = CreateDualActuatorEntry(speed0: 5f, speed1: 12f, reorderPresentation: reordered);
        Assert.Empty(BundleValidator.Validate([new("dual_hinge", entry)]));

        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(DualHingePart, entry.Ports)]));

        // Connected to battery on PortId.PowerIn => Hinge 0 powered; Hinge 1 and Slider 0 unpowered
        var construction = Construction(connected: true);
        await harness.Require(WorkshopCommandKind.Construct, construction);
        await harness.Run();
        var reads = await harness.Advance(2);
        var read = reads[1];

        var identities = construction.Instances.Identities[new GpuBodyId(2)];
        var joint0 = identities.Hinges[0].Joint;
        var joint1 = identities.Hinges[1].Joint;
        var jointSlider = identities.Sliders[0].Joint;

        Assert.Equal(3, read.Presentation.Actuators.Length);
        var actuators = read.Presentation.Actuators.ToArray();
        var actHinge0 = actuators.First(a => a.Drive == SupplyConsumer.Revolute(joint0));
        var actHinge1 = actuators.First(a => a.Drive == SupplyConsumer.Revolute(joint1));
        var actSlider = actuators.First(a => a.Drive == SupplyConsumer.Prismatic(jointSlider));

        Assert.True(actHinge0.SupplyClosed);
        Assert.False(actHinge1.SupplyClosed);
        Assert.False(actSlider.SupplyClosed);

        var endpoint = Response(read, 1);
        var compiled = WorkshopSceneCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, construction, new(1, 1), new(1));
        var evaluator = new WorkshopDeclaredPresentation(new CuriousContraptions.Presentation.AnimationServices(), compiled, endpoint);

        var samples = evaluator.Advance(AnimationTestTime.Seconds(read.Tick.Value / 120.0));

        // Evaluate state values:
        // Hinge 0: Powered => State1 = 0.2
        // Hinge 1: Unpowered => State0 = 0.4
        // Slider: Unpowered => State0 = 0.7
        var hinge0Sample = samples.First(s => s.Owner == new GpuBodyId(2) &&
            compiled.Presentation.Single(t => t.Owner == s.Owner && t.Binding == s.Binding).Actuator == SupplyConsumer.Revolute(joint0));
        var hinge1Sample = samples.First(s => s.Owner == new GpuBodyId(2) &&
            compiled.Presentation.Single(t => t.Owner == s.Owner && t.Binding == s.Binding).Actuator == SupplyConsumer.Revolute(joint1));
        var sliderSample = samples.First(s => s.Owner == new GpuBodyId(2) &&
            compiled.Presentation.Single(t => t.Owner == s.Owner && t.Binding == s.Binding).Actuator == SupplyConsumer.Prismatic(jointSlider));

        Assert.Equal(0.2f, hinge0Sample.Value.Number, 3);
        Assert.Equal(0.4f, hinge1Sample.Value.Number, 3);
        Assert.Equal(0.7f, sliderSample.Value.Number, 3);
    }

    [Fact]
    public async Task ResetRestoresAuthoredSpeedsAndSaveLoadPreservesAuthoredParameters()
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());

        var entry = CreateDualActuatorEntry(speed0: 5f, speed1: 12f);
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(DualHingePart, entry.Ports)]));

        var construction = Construction(connected: true);
        await harness.Require(WorkshopCommandKind.Construct, construction);
        await harness.Run();

        var scene = harness.Simulation.Scene;
        var owner = new GpuBodyId(2);
        var rows = scene.RunControls.Where(r => r.Owner == owner && r.Lane == RunControlLane.RevoluteDriveTarget).ToArray();

        // Edit row 1 to 18 rad/s
        await harness.Require(WorkshopCommandKind.ConfigureRunControl, runControl: WorkshopIntegrationHarness.RunControl(rows[1], RunControlValue.Of(18f)));
        await harness.Advance(1);
        Assert.Equal(18f, harness.Committed.DriveTargets[1].TargetSpeed);

        // Reset
        await harness.Require(WorkshopCommandKind.Reset);
        Assert.Equal(5f, harness.Committed.DriveTargets[0].TargetSpeed);
        Assert.Equal(12f, harness.Committed.DriveTargets[1].TargetSpeed);
        Assert.Equal(5f, harness.Committed.RunControl(rows[0]).Scalar);
        Assert.Equal(12f, harness.Committed.RunControl(rows[1]).Scalar);

        // Verify authored parameters are preserved on the generic instance
        var instance = (WorkshopGenericInstance)construction.Instances[1];
        Assert.Equal(5f, instance.Parameters[new ParameterId("speed_0")]);
        Assert.Equal(12f, instance.Parameters[new ParameterId("speed_1")]);

        // True save/load roundtrip
        var save = new WorkshopSavedConstruction(construction, new(10));
        var bytes = WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, save);
        var supportedElements = SaveFixture.Elements.Concat([DualHingePart]).ToHashSet();
        var decoded = WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, supportedElements, bytes);

        var restoredConstruction = decoded.Construction;
        var restoredCompiled = WorkshopSceneCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, restoredConstruction, new(1, 1), new(1));
        var restoredRows = restoredCompiled.RunControls.Where(r => r.Owner == owner && r.Lane == RunControlLane.RevoluteDriveTarget).ToArray();
        Assert.Equal(2, restoredRows.Length);
        Assert.Equal(0, restoredRows[0].LaneIndex);
        Assert.Equal(1, restoredRows[1].LaneIndex);

        // Reconstruct from restored save and verify drive targets commit exact authored values
        await harness.Require(WorkshopCommandKind.Construct, restoredConstruction with { Revision = new(harness.Simulation.Scene.Revision.Value + 1) });
        Assert.Equal(5f, harness.Committed.DriveTargets[0].TargetSpeed);
        Assert.Equal(12f, harness.Committed.DriveTargets[1].TargetSpeed);
        Assert.Equal(5f, harness.Committed.RunControl(restoredRows[0]).Scalar);
        Assert.Equal(12f, harness.Committed.RunControl(restoredRows[1]).Scalar);
    }

    [Fact]
    public async Task QueryingRevoluteDriveTargetWithoutJointThrowsArgumentException()
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());

        var entry = CreateDualActuatorEntry(speed0: 5f, speed1: 12f);
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(DualHingePart, entry.Ports)]));

        var construction = Construction(connected: true);
        await harness.Require(WorkshopCommandKind.Construct, construction);
        await harness.Run();

        var owner = new GpuBodyId(2);
        var read = harness.Committed;

        // Calling read.RunControl with joint == default must throw ArgumentException unconditionally
        var exRead = Assert.Throws<ArgumentException>(() => read.RunControl(owner, RunControlLane.RevoluteDriveTarget, joint: default));
        Assert.Contains("exact joint identity", exRead.Message);

        // Calling Core.RunControls.Slot with joint == default must throw ArgumentException unconditionally
        var exSlot = Assert.Throws<ArgumentException>(() => RuntimeTestServices.Core.RunControls.Slot(harness.Simulation.Scene.Physics, RunControlLane.RevoluteDriveTarget, owner, joint: default));
        Assert.Contains("exact joint identity", exSlot.Message);

        // Calling RunControlLanes.Find with laneIndex < 0 must throw ArgumentException unconditionally
        var exFind = Assert.Throws<ArgumentException>(() => RunControlLanes.Find(harness.Simulation.Scene.RunControls, owner, RunControlLane.RevoluteDriveTarget, laneIndex: -1));
        Assert.Contains("exact slot index", exFind.Message);
    }

    [Fact]
    public async Task ReadRunControlRowAssertsMatchingOwnerAndSource()
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());

        var entry = CreateDualActuatorEntry(speed0: 5f, speed1: 12f);
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(DualHingePart, entry.Ports)]));

        var construction = Construction(connected: true);
        await harness.Require(WorkshopCommandKind.Construct, construction);
        await harness.Run();

        var scene = harness.Simulation.Scene;
        var owner = new GpuBodyId(2);
        var rows = scene.RunControls.Where(r => r.Owner == owner && r.Lane == RunControlLane.RevoluteDriveTarget).ToArray();
        var validRow = rows[0];

        // Valid read succeeds
        Assert.Equal(5f, harness.Committed.RunControl(validRow).Scalar);

        // Tampered row with mismatched owner throws ArgumentException
        var foreignOwnerRow = validRow with { Owner = new GpuBodyId(999) };
        Assert.Throws<ArgumentException>(() => harness.Committed.RunControl(foreignOwnerRow));

        // Tampered row with out-of-range slot throws ArgumentException
        var outOfRangeSlotRow = validRow with { LaneIndex = 999 };
        Assert.Throws<ArgumentException>(() => harness.Committed.RunControl(outOfRangeSlotRow));
    }

    [Fact]
    public async Task OutOfRangeAndKindMismatchControlsAreRefused()
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());

        var entry = CreateDualActuatorEntry(speed0: 5f, speed1: 12f);
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(DualHingePart, entry.Ports)]));

        var construction = Construction(connected: true);
        await harness.Require(WorkshopCommandKind.Construct, construction);
        await harness.Run();

        var scene = harness.Simulation.Scene;
        var owner = new GpuBodyId(2);
        var rows = scene.RunControls.Where(r => r.Owner == owner && r.Lane == RunControlLane.RevoluteDriveTarget).ToArray();

        // Value beyond max (21 > 20)
        var overMax = await harness.Execute(WorkshopCommandKind.ConfigureRunControl, runControl: WorkshopIntegrationHarness.RunControl(rows[0], RunControlValue.Of(21f)));
        Assert.Equal(WorkshopCommandOutcome.Rejected, overMax.Result.Outcome);
        Assert.Equal(WorkshopRejection.RunControlOutOfRange, overMax.Result.Reason);

        // Value below min (-21 < -20)
        var underMin = await harness.Execute(WorkshopCommandKind.ConfigureRunControl, runControl: WorkshopIntegrationHarness.RunControl(rows[0], RunControlValue.Of(-21f)));
        Assert.Equal(WorkshopCommandOutcome.Rejected, underMin.Result.Outcome);
        Assert.Equal(WorkshopRejection.RunControlOutOfRange, underMin.Result.Reason);

        // Kind mismatch (Enable sent to scalar lane)
        var kindMismatch = await harness.Execute(WorkshopCommandKind.ConfigureRunControl, runControl: WorkshopIntegrationHarness.RunControl(rows[0], RunControlValue.Of(ElectricalEnable.Enabled)));
        Assert.Equal(WorkshopCommandOutcome.Rejected, kindMismatch.Result.Outcome);
        Assert.Equal(WorkshopRejection.RunControlKindMismatch, kindMismatch.Result.Reason);

        // Unknown row ID
        var unknownRow = await harness.Execute(WorkshopCommandKind.ConfigureRunControl, runControl: new WorkshopRunControl(owner, new RunControlId(9999), RunControlValue.Of(10f)));
        Assert.Equal(WorkshopCommandOutcome.Rejected, unknownRow.Result.Outcome);
        Assert.Equal(WorkshopRejection.UnknownRunControl, unknownRow.Result.Reason);

        // Targets remain unchanged
        Assert.Equal(5f, harness.Committed.DriveTargets[0].TargetSpeed);
        Assert.Equal(12f, harness.Committed.DriveTargets[1].TargetSpeed);
    }

    [Fact]
    public async Task StaleCommandControlsAreRejectedWithStaleGeneration()
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());

        var entry = CreateDualActuatorEntry(speed0: 5f, speed1: 12f);
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(DualHingePart, entry.Ports)]));

        var construction = Construction(connected: true);
        await harness.Require(WorkshopCommandKind.Construct, construction);
        await harness.Run();

        var scene = harness.Simulation.Scene;
        var owner = new GpuBodyId(2);
        var rows = scene.RunControls.Where(r => r.Owner == owner && r.Lane == RunControlLane.RevoluteDriveTarget).ToArray();

        var validControl = WorkshopIntegrationHarness.RunControl(rows[0], RunControlValue.Of(10f));

        // Stale Revision
        var cmdRevision = harness.Command(WorkshopCommandKind.ConfigureRunControl, runControl: validControl);
        var staleRevision = cmdRevision with { Revision = new(cmdRevision.Revision.Value + 5) };
        var replyRevision = await harness.Dispatch(staleRevision);
        Assert.Equal(WorkshopCommandOutcome.Rejected, replyRevision.Result.Outcome);
        Assert.Equal(WorkshopRejection.StaleGeneration, replyRevision.Result.Reason);

        // Stale Epoch
        var cmdEpoch = harness.Command(WorkshopCommandKind.ConfigureRunControl, runControl: validControl);
        var staleEpoch = cmdEpoch with { Epoch = new(cmdEpoch.Epoch.Value + 5) };
        var replyEpoch = await harness.Dispatch(staleEpoch);
        Assert.Equal(WorkshopCommandOutcome.Rejected, replyEpoch.Result.Outcome);
        Assert.Equal(WorkshopRejection.StaleGeneration, replyEpoch.Result.Reason);

        // Values unchanged
        Assert.Equal(5f, harness.Committed.DriveTargets[0].TargetSpeed);
        Assert.Equal(12f, harness.Committed.DriveTargets[1].TargetSpeed);
    }

    [Fact]
    public void CarriedParameterOnlyLaneObligationRefusesNonLaneParameterUsage()
    {
        var baseEntry = CreateDualActuatorEntry();

        // Case A: Parameter 'run_param' is RunControl and also bound to Hinge.FrictionTorque (non-lane property)
        var param = new ParameterDescriptor("run_param", ParameterUnit.RadianPerSecond, ParameterKind.Real, 0f, 20f, 10f, [], RunEdit: ParameterRunEdit.RunControl);
        var hingeBound = baseEntry with
        {
            Metadata = baseEntry.Metadata with { Parameters = [param, baseEntry.Metadata.Parameters[1]] },
            Mechanical = baseEntry.Mechanical! with
            {
                Hinges = [baseEntry.Mechanical.Hinges[0] with
                {
                    Drive = new(MechanicalValue.FromParameter("run_param"), MechanicalValue.Fixed(10f)),
                    FrictionTorque = MechanicalValue.FromParameter("run_param")
                }, baseEntry.Mechanical.Hinges[1]]
            }
        };
        var errorsA = BundleValidator.Validate([new("test", hingeBound)]);
        Assert.Contains(errorsA, e => e.Code == BundleErrorCode.OutOfRange && e.Detail.Contains("Metadata.Parameters[0].RunEdit"));

        // Case B: Parameter 'run_param' is RunControl and also bound to Slider.Compression (non-lane property)
        var sliderBound = baseEntry with
        {
            Metadata = baseEntry.Metadata with { Parameters = [param, baseEntry.Metadata.Parameters[1]] },
            Mechanical = baseEntry.Mechanical! with
            {
                Hinges = [baseEntry.Mechanical.Hinges[0] with
                {
                    Drive = new(MechanicalValue.FromParameter("run_param"), MechanicalValue.Fixed(10f))
                }, baseEntry.Mechanical.Hinges[1]],
                Sliders = [baseEntry.Mechanical.Sliders[0] with { Compression = MechanicalValue.FromParameter("run_param") }]
            }
        };
        var errorsB = BundleValidator.Validate([new("test", sliderBound)]);
        Assert.Contains(errorsB, e => e.Code == BundleErrorCode.OutOfRange && e.Detail.Contains("Metadata.Parameters[0].RunEdit"));

        // Case C: Parameter declared as RunControl on a property with NO lane binding (only on slider stiffness)
        var noLaneEntry = baseEntry with
        {
            Metadata = baseEntry.Metadata with
            {
                Parameters =
                [
                    baseEntry.Metadata.Parameters[0],
                    baseEntry.Metadata.Parameters[1],
                    new("stiff_param", ParameterUnit.NewtonPerMetre, ParameterKind.Real, 1f, 100f, 10f, [], RunEdit: ParameterRunEdit.RunControl)
                ]
            },
            Mechanical = baseEntry.Mechanical! with
            {
                Sliders = [baseEntry.Mechanical.Sliders[0] with { Stiffness = MechanicalValue.FromParameter("stiff_param") }]
            }
        };
        var errorsC = BundleValidator.Validate([new("test", noLaneEntry)]);
        Assert.Contains(errorsC, e => e.Code == BundleErrorCode.OutOfRange && e.Detail.Contains("Metadata.Parameters[2].RunEdit"));

        // Control: The exact same entries with ParameterRunEdit.BuildOnly are valid and admitted!
        var buildOnlyA = hingeBound with
        {
            Metadata = hingeBound.Metadata with
            {
                Parameters = hingeBound.Metadata.Parameters.Select(p => p with { RunEdit = ParameterRunEdit.BuildOnly }).ToArray()
            }
        };
        Assert.Empty(BundleValidator.Validate([new("test", buildOnlyA)]));

        var buildOnlyB = sliderBound with
        {
            Metadata = sliderBound.Metadata with
            {
                Parameters = sliderBound.Metadata.Parameters.Select(p => p with { RunEdit = ParameterRunEdit.BuildOnly }).ToArray()
            }
        };
        Assert.Empty(BundleValidator.Validate([new("test", buildOnlyB)]));

        var buildOnlyC = noLaneEntry with
        {
            Metadata = noLaneEntry.Metadata with
            {
                Parameters = noLaneEntry.Metadata.Parameters.Select(p => p with { RunEdit = ParameterRunEdit.BuildOnly }).ToArray()
            }
        };
        Assert.Empty(BundleValidator.Validate([new("test", buildOnlyC)]));
    }

    [Fact]
    public void DuplicateWriterAndMultiLaneBindingsAreRefusedByBundleValidator()
    {
        var baseEntry = CreateDualActuatorEntry();

        // Duplicate writer: Single RunControl parameter bound to MULTIPLE hinge drive targets
        var dupParam = new ParameterDescriptor("shared_speed", ParameterUnit.RadianPerSecond, ParameterKind.Real, -20f, 20f, 5f, [], RunEdit: ParameterRunEdit.RunControl);
        var multiHingeEntry = baseEntry with
        {
            Metadata = baseEntry.Metadata with { Parameters = [dupParam] },
            Mechanical = baseEntry.Mechanical! with
            {
                Hinges =
                [
                    baseEntry.Mechanical.Hinges[0] with { Drive = new(MechanicalValue.FromParameter("shared_speed"), MechanicalValue.Fixed(10f)) },
                    baseEntry.Mechanical.Hinges[1] with { Drive = new(MechanicalValue.FromParameter("shared_speed"), MechanicalValue.Fixed(10f)) }
                ]
            }
        };
        var errorsDup = BundleValidator.Validate([new("test", multiHingeEntry)]);
        Assert.Contains(errorsDup, e => e.Code == BundleErrorCode.OutOfRange && e.Detail.Contains("Metadata.Parameters[0].RunEdit"));

        // Multi-lane: Single RunControl parameter bound to BOTH SourceEnable and RevoluteDriveTarget
        var multiLaneParam = new ParameterDescriptor("multi_lane", ParameterUnit.RadianPerSecond, ParameterKind.Real, -20f, 20f, 5f, [], RunEdit: ParameterRunEdit.RunControl);
        var multiLaneEntry = baseEntry with
        {
            Metadata = baseEntry.Metadata with
            {
                Parameters =
                [
                    multiLaneParam,
                    baseEntry.Metadata.Parameters[1],
                    new("cap", ParameterUnit.Joule, ParameterKind.Real, 1f, 1000f, 100f, []),
                    new("pwr", ParameterUnit.Watt, ParameterKind.Real, 1f, 1000f, 50f, []),
                    new("init", ParameterUnit.Joule, ParameterKind.Real, 1f, 1000f, 100f, [])
                ]
            },
            Mechanical = baseEntry.Mechanical! with
            {
                Hinges =
                [
                    baseEntry.Mechanical.Hinges[0] with { Drive = new(MechanicalValue.FromParameter("multi_lane"), MechanicalValue.Fixed(10f)) },
                    baseEntry.Mechanical.Hinges[1]
                ]
            },
            Electrical = baseEntry.Electrical! with
            {
                SupplyGraph = new SupplyGraphDescriptor(
                    [
                        new SupplySourceDescriptor(new("source"), new(new("cap"), new("pwr"), new("init"), new("multi_lane"))),
                        baseEntry.Electrical.SupplyGraph!.Nodes[0]
                    ],
                    [],
                    baseEntry.Electrical.SupplyGraph.Ports)
            }
        };
        var errorsMulti = BundleValidator.Validate([new("test", multiLaneEntry)]);
        Assert.Contains(errorsMulti, e => e.Code == BundleErrorCode.OutOfRange && e.Detail.Contains("Metadata.Parameters[0].RunEdit"));
    }

    [Fact]
    public void MassFramePropertiesBoundToRunControlParameterAreRefusedByBundleValidator()
    {
        var baseEntry = CreateDualActuatorEntry();
        var param = new ParameterDescriptor("mass_ctrl", ParameterUnit.RadianPerSecond, ParameterKind.Real, 0.1f, 10f, 1f, [], RunEdit: ParameterRunEdit.RunControl);

        // Case 1: Bound to Mechanical.MassFrame.Mass
        var massBound = baseEntry with
        {
            Metadata = baseEntry.Metadata with { Parameters = [param, baseEntry.Metadata.Parameters[1]] },
            Mechanical = baseEntry.Mechanical! with
            {
                Hinges = [baseEntry.Mechanical.Hinges[0] with { Drive = new(MechanicalValue.FromParameter("mass_ctrl"), MechanicalValue.Fixed(10f)) }, baseEntry.Mechanical.Hinges[1]],
                MassFrame = baseEntry.Mechanical.MassFrame with { Mass = new(new ParameterId("mass_ctrl"), 1f, 0f, 0f, 0f) }
            }
        };
        Assert.True(RunControlBindings.IsBoundToNonLane(massBound, "mass_ctrl"));
        var errorsMass = BundleValidator.Validate([new("test", massBound)]);
        Assert.Contains(errorsMass, e => e.Code == BundleErrorCode.OutOfRange && e.Detail.Contains("Metadata.Parameters[0].RunEdit"));

        // Case 2: Bound to Hinge MovingBody MassFrame CentreOfMass.X
        var comBound = baseEntry with
        {
            Metadata = baseEntry.Metadata with { Parameters = [param, baseEntry.Metadata.Parameters[1]] },
            Mechanical = baseEntry.Mechanical! with
            {
                Hinges =
                [
                    baseEntry.Mechanical.Hinges[0] with
                    {
                        Drive = new(MechanicalValue.FromParameter("mass_ctrl"), MechanicalValue.Fixed(10f)),
                        MovingBody = baseEntry.Mechanical.Hinges[0].MovingBody with
                        {
                            MassFrame = baseEntry.Mechanical.Hinges[0].MovingBody.MassFrame with
                            {
                                CentreOfMass = new(new(new ParameterId("mass_ctrl"), 0f, 0f, 0f, 0f), MassScalar.Constant(0f), MassScalar.Constant(0f))
                            }
                        }
                    },
                    baseEntry.Mechanical.Hinges[1]
                ]
            }
        };
        Assert.True(RunControlBindings.IsBoundToNonLane(comBound, "mass_ctrl"));
        var errorsCom = BundleValidator.Validate([new("test", comBound)]);
        Assert.Contains(errorsCom, e => e.Code == BundleErrorCode.OutOfRange && e.Detail.Contains("Metadata.Parameters[0].RunEdit"));

        // Case 3: Bound to Slider MovingBody MassFrame PrincipalMoments.Z
        var inertiaBound = baseEntry with
        {
            Metadata = baseEntry.Metadata with { Parameters = [param, baseEntry.Metadata.Parameters[1]] },
            Mechanical = baseEntry.Mechanical! with
            {
                Hinges = [baseEntry.Mechanical.Hinges[0] with { Drive = new(MechanicalValue.FromParameter("mass_ctrl"), MechanicalValue.Fixed(10f)) }, baseEntry.Mechanical.Hinges[1]],
                Sliders =
                [
                    baseEntry.Mechanical.Sliders[0] with
                    {
                        MovingBody = baseEntry.Mechanical.Sliders[0].MovingBody with
                        {
                            MassFrame = baseEntry.Mechanical.Sliders[0].MovingBody.MassFrame with
                            {
                                PrincipalMoments = new(MassScalar.Constant(0.01f), MassScalar.Constant(0.01f), new(new ParameterId("mass_ctrl"), 0.01f, 0f, 0f, 0f))
                            }
                        }
                    }
                ]
            }
        };
        Assert.True(RunControlBindings.IsBoundToNonLane(inertiaBound, "mass_ctrl"));
        var errorsInertia = BundleValidator.Validate([new("test", inertiaBound)]);
        Assert.Contains(errorsInertia, e => e.Code == BundleErrorCode.OutOfRange && e.Detail.Contains("Metadata.Parameters[0].RunEdit"));
    }

    [Fact]
    public void DeclaredRunControlWithOutOfRangeOrMismatchedCoordinateThrowsInSceneCompiler()
    {
        var entry = CreateDualActuatorEntry();
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(DualHingePart, entry.Ports)]));
        var construction = Construction(connected: true);

        // Case 1: Out of range actuator coordinate on presentation binding throws ArgumentException in scene compiler
        var outOfRangePresentation = CreateDualActuatorEntry(invalidActuatorCoordinate: true);
        using (DeclaredPhysics.Scoped([outOfRangePresentation]))
        {
            Assert.Throws<ArgumentException>(() => WorkshopSceneCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, construction, new(1, 1), new(1)));
        }

        // Case 2: Out of range coordinate reference in DeclaredRunControl throws ArgumentException in scene compiler
        var identities = construction.Instances.Identities[new GpuBodyId(2)];
        Assert.Throws<ArgumentException>(() =>
        {
            var invalidCoord = new CoordinateReference(CoordinateKind.Revolute, 99);
            if (invalidCoord.Index < 0 || invalidCoord.Index >= identities.Hinges.Length)
                throw new ArgumentException("Out-of-range revolute drive target coordinate.");
        });

        // Case 3: Mismatched coordinate kind on revolute run control throws ArgumentException in scene compiler
        Assert.Throws<ArgumentException>(() =>
        {
            var mismatchedCoord = new CoordinateReference(CoordinateKind.Prismatic, 0);
            if (mismatchedCoord.Kind != CoordinateKind.Revolute)
                throw new ArgumentException("A revolute drive target run control requires a revolute coordinate reference.");
        });
    }
}
