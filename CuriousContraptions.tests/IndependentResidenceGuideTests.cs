using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;
using Xunit;

namespace CuriousContraptions.Tests;

public sealed class IndependentResidenceGuideTests
{
    private static BundleEntry BaseReceiverEntry => TestBundle.Entry(new PartId("basket"));

    private static BundleEntry CreateEntry(string id, ResidenceSensorDescriptor? sensor, PlanarGuideDescriptor? guide)
    {
        var baseEntry = BaseReceiverEntry;
        return baseEntry with
        {
            Metadata = baseEntry.Metadata with { Id = id, PaletteOrder = 900 + (int)Math.Abs(id.GetHashCode() % 90) },
            Mechanical = baseEntry.Mechanical! with
            {
                Residence = sensor,
                PlanarGuide = guide
            }
        };
    }

    [Fact]
    public async Task UnassistedResidenceSensor_DetectsDwellAndFiresCaptured_WithoutLateralAcceleration()
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());

        var sensor = new ResidenceSensorDescriptor(
            new(-1f, -1f, -1f), new(1f, 1f, 1f),
            MechanicalValue.Fixed(5f),
            MechanicalValue.Fixed(0.1f), // 0.1s dwell = 12 ticks
            "capture_participation",
            Target: TargetSelector.AllPayloads);

        var entry = CreateEntry("unassisted_sensor", sensor, null);
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(new(entry.Metadata.Id), entry.Ports)]));

        var sensorInstance = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId(entry.Metadata.Id), new(1), 0, 1, 0, 0, 0, 0, 1);
        var ballId = new GpuBodyId(2);
        // Start ball at center X = 0, Y = 1.1m inside basket
        var ball = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("ball"), ballId, 0f, 1.1f, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(sensorInstance, ball));

        await harness.Require(WorkshopCommandKind.Construct, construction);
        await harness.Run();

        // Advance 60 ticks (0.5s): ball lands, dwells >= 0.1s
        await harness.Advance(60);

        Assert.True(harness.Committed.Bodies.TryGet(ballId, out var moving));
        // Ball falls vertically: lateral position X remains exactly 0 and Z remains 0
        Assert.Equal(0f, moving.Body.Local.X, 3);
        Assert.Equal(0f, moving.Body.Local.Z, 3);
        Assert.Equal(0f, moving.Body.Velocity.X, 3);
        Assert.Equal(0f, moving.Body.Velocity.Z, 3);

        // Captured event fired because dwelling time exceeded
        Assert.Contains(harness.Records, r => r.Kind == SimulationEventKind.Captured && r.Other == ballId.Value);

        // Capture latch in committed reads is Latched
        Assert.Equal(1, harness.Committed.Captures.Count);
        var capture = harness.Committed.Captures[0];
        Assert.Equal(CaptureLatchPhase.Latched, capture.Phase);
    }

    [Fact]
    public async Task StandalonePlanarGuide_AppliesCentringAcceleration_WithoutProducingCapture()
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());

        var guide = new PlanarGuideDescriptor(
            new(-1.5f, -0.5f, -1.5f), new(1.5f, 3.5f, 1.5f),
            SupportHeight: 0.5f,
            SupportMargin: MechanicalValue.Fixed(0.2f),
            MaximumAcceleration: MechanicalValue.Fixed(10f),
            Target: TargetSelector.AllPayloads);

        var entry = CreateEntry("standalone_guide", null, guide);
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(new(entry.Metadata.Id), entry.Ports)]));

        var guideInstance = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId(entry.Metadata.Id), new(1), 0, 0, 0, 0, 0, 0, 1);
        var ballId = new GpuBodyId(2);
        // Start ball at lateral offset X = 0.8m, Y = 2.0m descending into guide
        var ball = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("ball"), ballId, 0.8f, 2.0f, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(guideInstance, ball));

        await harness.Require(WorkshopCommandKind.Construct, construction);
        await harness.Run();

        // Advance 12 ticks
        await harness.Advance(12);

        Assert.True(harness.Committed.Bodies.TryGet(ballId, out var moving));
        // Guide applies negative X acceleration towards X = 0
        Assert.True(moving.Body.Velocity.X < -0.1f, "Guide should apply lateral centring acceleration toward X=0.");
        Assert.True(moving.Body.Local.X < 0.8f, "Lateral position should decrease toward center.");

        // Zero capture latches or events produced because there is no sensor
        Assert.DoesNotContain(harness.Records, r => r.Kind == SimulationEventKind.Captured);
        Assert.Equal(0, harness.Committed.Captures.Count);
    }

    [Fact]
    public async Task CombinedReceiver_PreservesBothGuidingAndCapture()
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var parameters = InstanceParameterCollection.Defaults(new PartId("basket")).ToDictionary();
        parameters[new("force_maximum_acceleration")] = 8f;
        parameters[new("force_support_margin")] = 0.2f;
        parameters[new("capture_dwell")] = 0.15f;

        var receiverInstance = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("basket"), new(1), 0, 1, 0, 0, 0, 0, 1,
            new InstanceParameterCollection(parameters));
        var ballId = new GpuBodyId(2);
        var ball = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("ball"), ballId, 0f, 1.1f, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(receiverInstance, ball));

        await harness.Require(WorkshopCommandKind.Construct, construction);
        await harness.Run();
        await harness.Advance(180);

        // Receiver has both sensor and guide declared and compiled
        Assert.True(harness.Simulation.Scene.Physics.Sensors.Length > 0, "Receiver must declare and compile residence sensors.");
        Assert.True(harness.Simulation.Scene.Physics.Guides.Length > 0, "Receiver must declare and compile planar guides.");

        // Receiver captures the ball
        Assert.Contains(harness.Records, r => r.Kind == SimulationEventKind.Captured && r.Other == ballId.Value);
        Assert.Equal(1, harness.Committed.Captures.Count);
        var capture = harness.Committed.Captures[0];
        Assert.Equal(CaptureLatchPhase.Latched, capture.Phase);
    }

    [Fact]
    public async Task TransformedFrames_OffsetAndRotatedSensorAndGuide_EvaluatedInLocalCoordinateSpace()
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());

        // Sensor frame: offset at (3, 2, 0), rotated 90 deg around Z
        var rot90Z = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2f);
        var sensorOffset = new Vector3(3f, 2f, 0f);

        var sensor = new ResidenceSensorDescriptor(
            new(-0.8f, -0.8f, -0.8f), new(0.8f, 0.8f, 0.8f),
            MechanicalValue.Fixed(5f),
            MechanicalValue.Fixed(0.1f),
            "capture_participation",
            Target: TargetSelector.AllDynamic,
            Position: sensorOffset,
            Rotation: rot90Z);

        // Guide frame: independent offset at (3, 1, 0), rotated 90 deg around Y
        var rot90Y = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2f);
        var guideOffset = new Vector3(3f, 1f, 0f);

        var guide = new PlanarGuideDescriptor(
            new(-1f, 0f, -1f), new(1f, 3f, 1f),
            SupportHeight: 0.5f,
            SupportMargin: MechanicalValue.Fixed(0.2f),
            MaximumAcceleration: MechanicalValue.Fixed(10f),
            Target: TargetSelector.AllDynamic,
            Position: guideOffset,
            Rotation: rot90Y);

        var entry = CreateEntry("transformed_elem", sensor, guide);
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(new(entry.Metadata.Id), entry.Ports)]));

        var elemInstance = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId(entry.Metadata.Id), new(1), 0, 0, 0, 0, 0, 0, 1);

        // Ball A placed offset laterally at (3.4, 2.5, 0) descending into the transformed guide region
        var ballAId = new GpuBodyId(2);
        var ballA = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("ball"), ballAId, 3.4f, 2.5f, 0, 0, 0, 0, 1);

        // Ball B placed at world origin (0, 2.5, 0) which is OUTSIDE both transformed regions
        var ballBId = new GpuBodyId(3);
        var ballB = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("ball"), ballBId, 0f, 2.5f, 0, 0, 0, 0, 1);

        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(elemInstance, ballA, ballB));
        await harness.Require(WorkshopCommandKind.Construct, construction);
        await harness.Run();

        // Advance 12 ticks: Ball A descends into transformed guide and receives transformed centring acceleration
        await harness.Advance(12);

        Assert.True(harness.Committed.Bodies.TryGet(ballAId, out var movingA));
        Assert.True(harness.Committed.Bodies.TryGet(ballBId, out var movingB));

        // Ball A received negative X centring acceleration towards the transformed guide center X = 3.0
        Assert.True(movingA.Body.Velocity.X < 0f, $"Expected Vx < 0 for Ball A, got {movingA.Body.Velocity.X}");
        // Ball B is outside transformed guide and received zero lateral acceleration
        Assert.Equal(0f, movingB.Body.Velocity.X, 3);

        // Advance further so Ball A settles into the transformed sensor volume and dwells
        await harness.Advance(20);

        // Ball A is inside transformed sensor volume and captured
        Assert.Contains(harness.Records, r => r.Kind == SimulationEventKind.Captured && r.Other == ballAId.Value);

        // Ball B is outside transformed sensor volume and NOT captured
        Assert.DoesNotContain(harness.Records, r => r.Kind == SimulationEventKind.Captured && r.Other == ballBId.Value);
    }

    [Fact]
    public async Task TargetFiltering_OnlyMatchingTargetsAreSensedOrGuided()
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());

        // 1. AllPayloads sensor & guide
        var payloadSensor = new ResidenceSensorDescriptor(
            new(-1f, -1f, -1f), new(1f, 1f, 1f),
            MechanicalValue.Fixed(5f), MechanicalValue.Fixed(0.05f),
            "capture_participation",
            Target: TargetSelector.AllPayloads);
        var payloadGuide = new PlanarGuideDescriptor(
            new(-1.5f, -0.5f, -1.5f), new(1.5f, 3.5f, 1.5f),
            SupportHeight: 0.5f, SupportMargin: MechanicalValue.Fixed(0.2f),
            MaximumAcceleration: MechanicalValue.Fixed(10f),
            Target: TargetSelector.AllPayloads);
        var payloadEntry = CreateEntry("payload_filter", payloadSensor, payloadGuide);

        // 2. AllDynamic sensor & guide
        var dynamicSensor = new ResidenceSensorDescriptor(
            new(-1f, -1f, -1f), new(1f, 1f, 1f),
            MechanicalValue.Fixed(5f), MechanicalValue.Fixed(0.05f),
            "capture_participation",
            Target: TargetSelector.AllDynamic);
        var dynamicGuide = new PlanarGuideDescriptor(
            new(-1.5f, -0.5f, -1.5f), new(1.5f, 3.5f, 1.5f),
            SupportHeight: 0.5f, SupportMargin: MechanicalValue.Fixed(0.2f),
            MaximumAcceleration: MechanicalValue.Fixed(10f),
            Target: TargetSelector.AllDynamic);
        var dynamicEntry = CreateEntry("dynamic_filter", dynamicSensor, dynamicGuide);

        // 3. Named target sensor & guide (targeting body 20)
        var namedSensor = new ResidenceSensorDescriptor(
            new(-1f, -1f, -1f), new(1f, 1f, 1f),
            MechanicalValue.Fixed(5f), MechanicalValue.Fixed(0.05f),
            "capture_participation",
            Target: TargetSelector.Named("20"));
        var namedGuide = new PlanarGuideDescriptor(
            new(-3.0f, -0.5f, -1.5f), new(3.0f, 3.5f, 1.5f),
            SupportHeight: 0.5f, SupportMargin: MechanicalValue.Fixed(0.2f),
            MaximumAcceleration: MechanicalValue.Fixed(10f),
            Target: TargetSelector.Named("20"));
        var namedEntry = CreateEntry("named_filter", namedSensor, namedGuide);

        using var physics = DeclaredPhysics.Scoped([payloadEntry, dynamicEntry, namedEntry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([
            (new(payloadEntry.Metadata.Id), payloadEntry.Ports),
            (new(dynamicEntry.Metadata.Id), dynamicEntry.Ports),
            (new(namedEntry.Metadata.Id), namedEntry.Ports)
        ]));

        // Test 1: AllPayloads vs non-payload dynamic body (Weight has PayloadPolicy.None)
        {
            var sensorInst = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId(payloadEntry.Metadata.Id), new(1), 0, 1, 0, 0, 0, 0, 1);
            var weightId = new GpuBodyId(2);
            var weight = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("weight"), weightId, 0.5f, 1.8f, 0, 0, 0, 0, 1,
                InstanceParameterCollection.Defaults(new PartId("weight")));

            await harness.Require(WorkshopCommandKind.Construct, new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(sensorInst, weight)));
            await harness.Run();
            await harness.Advance(12);

            Assert.True(harness.Committed.Bodies.TryGet(weightId, out var movingWeight));
            // AllPayloads guide ignores non-payload dynamic body (zero lateral acceleration)
            Assert.Equal(0f, movingWeight.Body.Velocity.X, 3);

            await harness.Advance(10);
            // AllPayloads sensor ignores non-payload dynamic body
            Assert.DoesNotContain(harness.Records, r => r.Kind == SimulationEventKind.Captured);
            await harness.Reset();
        }

        // Test 2: AllDynamic with Weight
        {
            harness.Records.Clear();
            var sensorInst = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId(dynamicEntry.Metadata.Id), new(1), 0, 1, 0, 0, 0, 0, 1);
            var weightId = new GpuBodyId(2);
            var weight = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("weight"), weightId, 0.5f, 1.8f, 0, 0, 0, 0, 1,
                InstanceParameterCollection.Defaults(new PartId("weight")));

            await harness.Require(WorkshopCommandKind.Construct, new WorkshopConstruction(new(3), WorkshopCadenceSettings.Default(), new(sensorInst, weight)));
            await harness.Run();
            await harness.Advance(12);

            Assert.True(harness.Committed.Bodies.TryGet(weightId, out var movingWeight));
            // AllDynamic guide centring-accelerates non-payload dynamic Weight towards X = 0 (Vx < 0)
            Assert.True(movingWeight.Body.Velocity.X < 0f, $"Expected Vx < 0 for Weight, got {movingWeight.Body.Velocity.X}");

            await harness.Advance(15);
            // AllDynamic sensor captures dynamic Weight
            Assert.Contains(harness.Records, r => r.Kind == SimulationEventKind.Captured && r.Other == weightId.Value);
            await harness.Reset();
        }

        // Test 3: Named target: body 20 is targeted, body 30 is not
        {
            harness.Records.Clear();
            var sensorInst = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId(namedEntry.Metadata.Id), new(1), 0, 1, 0, 0, 0, 0, 1);
            var targetBall = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("ball"), new GpuBodyId(20), -0.2f, 1.8f, 0, 0, 0, 0, 1);
            var otherBall = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("ball"), new GpuBodyId(30), 2.0f, 1.8f, 0, 0, 0, 0, 1);

            await harness.Require(WorkshopCommandKind.Construct, new WorkshopConstruction(new(4), WorkshopCadenceSettings.Default(), new(sensorInst, targetBall, otherBall)));
            await harness.Run();
            await harness.Advance(12);

            Assert.True(harness.Committed.Bodies.TryGet(new(20), out var movingTarget));
            Assert.True(harness.Committed.Bodies.TryGet(new(30), out var movingOther));

            // Named guide centring-accelerates targeted ball 20 from X = -0.2 towards X = 0 (Vx > 0)
            Assert.True(movingTarget.Body.Velocity.X > 0f, $"Expected Vx > 0 for target ball, got {movingTarget.Body.Velocity.X}");
            // Named guide ignores non-target ball 30 (zero lateral acceleration, even though within [-3, 3] guide volume)
            Assert.Equal(0f, movingOther.Body.Velocity.X, 3);

            await harness.Advance(15);
            // Target ball 20 is captured
            Assert.Contains(harness.Records, r => r.Kind == SimulationEventKind.Captured && r.Other == 20);
            // Non-target ball 30 is NOT captured
            Assert.DoesNotContain(harness.Records, r => r.Kind == SimulationEventKind.Captured && r.Other == 30);
        }
    }

    [Fact]
    public void TryCapture_TargetSelectorSupport_DynamicAndNamedTargets()
    {
        var dynSensor = new ResidenceSensorDescriptor(
            new(-1f, -1f, -1f), new(1f, 1f, 1f),
            MechanicalValue.Fixed(5f), MechanicalValue.Fixed(0.1f), "capture_participation",
            Target: TargetSelector.AllDynamic);
        var dynEntry = CreateEntry("trycap_dyn_sensor", dynSensor, null);

        var numSensor = new ResidenceSensorDescriptor(
            new(-1f, -1f, -1f), new(1f, 1f, 1f),
            MechanicalValue.Fixed(5f), MechanicalValue.Fixed(0.1f), "capture_participation",
            Target: TargetSelector.Named("2"));
        var numEntry = CreateEntry("trycap_num_sensor", numSensor, null);

        var puzzleSensor = new ResidenceSensorDescriptor(
            new(-1f, -1f, -1f), new(1f, 1f, 1f),
            MechanicalValue.Fixed(5f), MechanicalValue.Fixed(0.1f), "capture_participation",
            Target: TargetSelector.Named("puzzle_target"));
        var puzzleEntry = CreateEntry("trycap_puzzle_sensor", puzzleSensor, null);

        var interactionSensor = new ResidenceSensorDescriptor(
            new(-1f, -1f, -1f), new(1f, 1f, 1f),
            MechanicalValue.Fixed(5f), MechanicalValue.Fixed(0.1f), "capture_participation",
            Target: TargetSelector.Named("interaction_target"));
        var interactionEntry = CreateEntry("trycap_interaction_sensor", interactionSensor, null);

        using var physics = DeclaredPhysics.Scoped([dynEntry, numEntry, puzzleEntry, interactionEntry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([
            (new(dynEntry.Metadata.Id), dynEntry.Ports),
            (new(numEntry.Metadata.Id), numEntry.Ports),
            (new(puzzleEntry.Metadata.Id), puzzleEntry.Ports),
            (new(interactionEntry.Metadata.Id), interactionEntry.Ports)
        ]));

        var weightEntry = TestBundle.Entry(new PartId("weight"));
        var wallEntry = TestBundle.Entry(new PartId("wall"));
        var ballEntry = TestBundle.Entry(new PartId("ball"));

        var entries = new Dictionary<PartId, BundleEntry>
        {
            [new(dynEntry.Metadata.Id)] = dynEntry,
            [new(numEntry.Metadata.Id)] = numEntry,
            [new(puzzleEntry.Metadata.Id)] = puzzleEntry,
            [new(interactionEntry.Metadata.Id)] = interactionEntry,
            [new("weight")] = weightEntry,
            [new("wall")] = wallEntry,
            [new("ball")] = ballEntry
        };

        var instances = new IWorkshopInstance[]
        {
            WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId(dynEntry.Metadata.Id), new(1), 0, 0, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("weight"), new(2), 0, 0, 0, 0, 0, 0, 1,
                InstanceParameterCollection.Defaults(new PartId("weight"))),
            WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("wall"), new(3), 0, 0, 0, 0, 0, 0, 1,
                InstanceParameterCollection.Defaults(new PartId("wall"))),
            WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId(numEntry.Metadata.Id), new(4), 0, 0, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId(puzzleEntry.Metadata.Id), new(5), 0, 0, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId(interactionEntry.Metadata.Id), new(6), 0, 0, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("ball"), new(7), 0, 0, 0, 0, 0, 0, 1)
        };

        var map = new CompiledIdentityMap(instances, entries);

        // 1. AllDynamic: matches dynamic bodies (including non-payload Weight), fails for static bodies (Wall) or non-existent
        Assert.True(map.TryCapture(new GpuBodyId(1), new GpuBodyId(2), out var capDyn));
        Assert.NotEqual(0UL, capDyn.Sensor.Value);
        Assert.False(map.TryCapture(new GpuBodyId(1), new GpuBodyId(3), out _)); // Wall is static
        Assert.False(map.TryCapture(new GpuBodyId(1), new GpuBodyId(99), out _)); // Non-existent

        // 2. Named numeric ID: matches body 2, fails for body 7
        Assert.True(map.TryCapture(new GpuBodyId(4), new GpuBodyId(2), out var capNum));
        Assert.NotEqual(0UL, capNum.Sensor.Value);
        Assert.False(map.TryCapture(new GpuBodyId(4), new GpuBodyId(7), out _));

        // 3. Symbolic "puzzle_target": matches dynamic body, fails for static
        Assert.True(map.TryCapture(new GpuBodyId(5), new GpuBodyId(2), out var capPuzzle));
        Assert.NotEqual(0UL, capPuzzle.Sensor.Value);
        Assert.False(map.TryCapture(new GpuBodyId(5), new GpuBodyId(3), out _));

        // 4. Symbolic "interaction_target": matches dynamic body, fails for static
        Assert.True(map.TryCapture(new GpuBodyId(6), new GpuBodyId(7), out var capInteraction));
        Assert.NotEqual(0UL, capInteraction.Sensor.Value);
        Assert.False(map.TryCapture(new GpuBodyId(6), new GpuBodyId(3), out _));
    }

    [Fact]
    public async Task Controls_SpeedLimit_InsufficientDwell_SupportGate_NonDescending()
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());

        var sensor = new ResidenceSensorDescriptor(
            new(-1f, -1f, -1f), new(1f, 1f, 1f),
            MechanicalValue.Fixed(1.0f), // SpeedLimit = 1.0 m/s
            MechanicalValue.Fixed(0.5f), // Dwell = 0.5s = 60 ticks
            "capture_participation",
            Target: TargetSelector.AllPayloads);

        var guide = new PlanarGuideDescriptor(
            new(-1.5f, 0f, -1.5f), new(1.5f, 3f, 1.5f),
            SupportHeight: 1.0f,
            SupportMargin: MechanicalValue.Fixed(0.2f),
            MaximumAcceleration: MechanicalValue.Fixed(10f),
            Target: TargetSelector.AllPayloads);

        var entry = CreateEntry("controls_test_elem", sensor, guide);
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(new(entry.Metadata.Id), entry.Ports)]));

        // 1. Excessive speed: body moving at 10 m/s exceeds SpeedLimit (1.0 m/s) -> does not dwell
        {
            var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
                new(WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId(entry.Metadata.Id), new(1), 0, 0, 0, 0, 0, 0, 1),
                    WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("ball"), new(2), 0, 0, 0, 0, 0, 0, 1)));
            var scene = WorkshopPhysicsCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, construction, new(1, 2));
            var world = PhysicsGpuAbi.Admission(scene, new(1), new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));

            // Set ball velocity to 10 m/s in admitted buffer
            var layout = PhysicsGpuAbi.Layout(world);
            var ballOffset = layout.BodyOffset(scene.Layout.GetBodySlot(new(2)));
            BinaryPrimitives.WriteSingleLittleEndian(world.AsSpan(ballOffset + PhysicsGpuAbi.BodyVelocityOffset + 4), 10f); // Vy = 10

            WorkshopPhysicsEngineTests.StepWorld(world, 5, (tick, state, interactions) =>
            {
                var sensorOffset = layout.SensorOffset(0);
                // State should remain Clear because speed > speedLimit
                Assert.Empty(interactions.Captures);
            });
        }

        // 2. Insufficient dwell: ball exits volume before dwell time elapses -> no capture
        {
            var sensorInst = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId(entry.Metadata.Id), new(1), 0, 0, 0, 0, 0, 0, 1);
            // Ball starts at top edge (Y = 0.9m) and drops out through the bottom in less than 60 ticks
            var ball = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("ball"), new(2), 0, 0.9f, 0, 0, 0, 0, 1);

            await harness.Require(WorkshopCommandKind.Construct, new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(sensorInst, ball)));
            await harness.Run();
            // Advance only 10 ticks (0.083s < 0.5s dwell)
            await harness.Advance(10);

            // Did not reach dwell threshold
            Assert.DoesNotContain(harness.Records, r => r.Kind == SimulationEventKind.Captured);
        }

        // 3. Non-descending in guide: upward velocity (locV.Y > 0) receives zero acceleration
        {
            var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
                new(WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId(entry.Metadata.Id), new(1), 0, 0, 0, 0, 0, 0, 1),
                    WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("ball"), new(2), 0.5f, 1.5f, 0, 0, 0, 0, 1)));
            var scene = WorkshopPhysicsCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, construction, new(1, 2));
            var world = PhysicsGpuAbi.Admission(scene, new(1), new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
            var layout = PhysicsGpuAbi.Layout(world);
            var ballOffset = layout.BodyOffset(scene.Layout.GetBodySlot(new(2)));
            // Set upward velocity Vy = +5 m/s
            BinaryPrimitives.WriteSingleLittleEndian(world.AsSpan(ballOffset + PhysicsGpuAbi.BodyVelocityOffset + 4), 5f);

            WorkshopPhysicsEngineTests.StepWorld(world, 1, (tick, state, interactions) =>
            {
                var vx = BinaryPrimitives.ReadSingleLittleEndian(state.AsSpan(ballOffset + PhysicsGpuAbi.BodyVelocityOffset));
                // Guide applies zero centring acceleration to upward-moving bodies
                Assert.Equal(0f, vx, 4);
            });
        }

        // 4. Below support gate: underside below supportHeight - supportMargin receives zero acceleration
        {
            var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
                new(WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId(entry.Metadata.Id), new(1), 0, 0, 0, 0, 0, 0, 1),
                    WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("ball"), new(2), 0.5f, 0.2f, 0, 0, 0, 0, 1))); // Y = 0.2 < 1.0 - 0.2 = 0.8
            var scene = WorkshopPhysicsCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, construction, new(1, 2));
            var world = PhysicsGpuAbi.Admission(scene, new(1), new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
            var layout = PhysicsGpuAbi.Layout(world);
            var ballOffset = layout.BodyOffset(scene.Layout.GetBodySlot(new(2)));

            WorkshopPhysicsEngineTests.StepWorld(world, 1, (tick, state, interactions) =>
            {
                var vx = BinaryPrimitives.ReadSingleLittleEndian(state.AsSpan(ballOffset + PhysicsGpuAbi.BodyVelocityOffset));
                Assert.Equal(0f, vx, 4);
            });
        }
    }

    [Fact]
    public void AtomicRefusal_InvalidBounds_InvalidFrames_InvalidTargets()
    {
        var validSensor = new ResidenceSensorDescriptor(
            new(-1f, -1f, -1f), new(1f, 1f, 1f),
            MechanicalValue.Fixed(5f), MechanicalValue.Fixed(0.2f),
            "capture_participation");

        var validGuide = new PlanarGuideDescriptor(
            new(-1f, 0f, -1f), new(1f, 2f, 1f),
            1.0f, MechanicalValue.Fixed(0.1f), MechanicalValue.Fixed(5f));

        var entry = CreateEntry("refusal_base", validSensor, validGuide);

        // 1. Invalid volume bounds (Minimum >= Maximum)
        Assert.Throws<ArgumentException>(() =>
            new ResidenceSensorDeclaration(new(1), new(2), new BodyTargetSet(BodyTargetKind.AllPayloads, default), RigidLocalPose.Identity,
                new(1f, 0f, 0f), new(0f, 0f, 0f), new(1f), new(1f), SensorParticipation.Enabled).Validate());

        Assert.Throws<ArgumentException>(() =>
            new PlanarGuideDeclaration(new(1), new(2), new BodyTargetSet(BodyTargetKind.AllPayloads, default), RigidLocalPose.Identity,
                new(1f, 0f, 0f), new(0f, 0f, 0f), new(1f), new(0.1f), new(5f)).Validate());

        var boundsError = BundleValidator.Validate([new("Test", entry with
        {
            Mechanical = entry.Mechanical! with
            {
                Residence = validSensor with { Minimum = new(2f, 0f, 0f), Maximum = new(1f, 0f, 0f) }
            }
        })]);
        Assert.Contains(boundsError, e => e.Code == BundleErrorCode.OutOfRange && e.Detail.Contains("Residence"));

        // 2. Non-finite frame position
        var nonFinitePosError = BundleValidator.Validate([new("Test", entry with
        {
            Mechanical = entry.Mechanical! with
            {
                Residence = validSensor with { Position = new(float.NaN, 0f, 0f) }
            }
        })]);
        Assert.Contains(nonFinitePosError, e => e.Code == BundleErrorCode.NonFinite && e.Detail.Contains("Residence.Position"));

        // 3. Unnormalized rotation quaternion
        var unnormalizedRotError = BundleValidator.Validate([new("Test", entry with
        {
            Mechanical = entry.Mechanical! with
            {
                PlanarGuide = validGuide with { Rotation = new(2f, 0f, 0f, 0f) }
            }
        })]);
        Assert.Contains(unnormalizedRotError, e => e.Code == BundleErrorCode.OutOfRange && e.Detail.Contains("PlanarGuide.Rotation"));

        Assert.Throws<ArgumentException>(() =>
            CanonicalRotation.Normalize(2f, 0f, 0f, 0f));

        // 4. Invalid target selector (empty name)
        Assert.Throws<ArgumentException>(() => TargetSelector.Named(""));
        Assert.Throws<ArgumentException>(() => TargetSelector.Named("   "));

        var invalidTargetError = BundleValidator.Validate([new("Test", entry with
        {
            Mechanical = entry.Mechanical! with
            {
                Residence = validSensor with { Target = new((TargetSelectorKind)99) }
            }
        })]);
        Assert.Contains(invalidTargetError, e => e.Code == BundleErrorCode.UndefinedEnum && e.Detail.Contains("Residence.Target.Kind"));
    }

    [Fact]
    public async Task Lifecycle_RunResetAndSaveLoadRoundtrip()
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());

        var sensor = new ResidenceSensorDescriptor(
            new(-1f, -1f, -1f), new(1f, 1f, 1f),
            MechanicalValue.Fixed(5f), MechanicalValue.Fixed(0.2f),
            "capture_participation",
            Target: TargetSelector.AllPayloads);

        var guide = new PlanarGuideDescriptor(
            new(-1f, 0f, -1f), new(1f, 2f, 1f),
            1.0f, MechanicalValue.Fixed(0.1f), MechanicalValue.Fixed(5f),
            Target: TargetSelector.AllPayloads);

        var entry = CreateEntry("lifecycle_elem", sensor, guide);
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(new(entry.Metadata.Id), entry.Ports)]));

        var elemInstance = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId(entry.Metadata.Id), new(1), 0, 0, 0, 0, 0, 0, 1);
        var ball = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("ball"), new(2), 0.2f, 1.5f, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(elemInstance, ball));

        // 1. Save / Load Codec roundtrip
        var save = new WorkshopSavedConstruction(construction, new(10));
        var elements = SaveFixture.Elements.Append(new PartId(entry.Metadata.Id)).ToHashSet();
        var encoded = WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, save);
        var decoded = WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, elements, encoded);
        Assert.Equal(construction.Instances.Count, decoded.Construction.Instances.Count);
        Assert.Equal(construction.Instances[0].Id, decoded.Construction.Instances[0].Id);
        Assert.Equal(construction.Instances[1].Id, decoded.Construction.Instances[1].Id);

        // 2. Simulation Run and Reset
        await harness.Require(WorkshopCommandKind.Construct, construction);

        var initialCommitted = harness.Committed;
        await harness.Run();
        await harness.Advance(30);

        // Ball has moved
        Assert.True(harness.Committed.Bodies.TryGet(new(2), out var movingBody));
        Assert.NotEqual(1.5f, movingBody.Body.Local.Y);

        // Reset restores initial authored state exactly
        await harness.Reset();
        NoFixedCapacityTests.AssertSameState(initialCommitted, harness.Committed, "Reset should restore initial state exactly.");
    }
}
