using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;

namespace CuriousContraptions.Tests;

public sealed class OwnedFiniteWorkTransactionTests
{
    [Fact]
    public async Task ANewSeatedPayloadUsesCurrentClearanceWithoutReplayingBlockedHistory()
    {
        var fixture = new LawScene(false);
        var frame = fixture.StaticBox(new(0f, 3f, 0f), new(.1f), colliderOffset: new(0f, -2f, 0f));
        var first = fixture.Sphere(new(0f, 3f, 0f), .35f, 1f, new(0, 0, 0, 0), Vector3.Zero, Vector3.Zero, 0, LawScene.Gravity);
        var next = fixture.Sphere(new(0f, 5f, 0f), .35f, 1f, new(0, 0, 0, 0), Vector3.Zero, Vector3.Zero, 0, LawScene.Gravity);
        fixture.Box(new(0f, 4.2f, 0f), new(.1f), 1f, new(0, 0, 0, 0), velocity: new(4f, 0f, 0f), gravity: 0f);
        var actuator = fixture.FiniteWork(frame, first, 45f, 45f,
            targets: new(BodyTargetKind.AllDynamic, default), maximumRelativeSpeed: 16f);
        var physics = fixture.Admit().Scene;
        var settings = WorkshopCadenceSettings.Default();
        var binding = new CompiledPresentationBinding(PresentationProperty.LocalX, PresentationDriver.CommittedFiniteWorkState,
            PresentationValueMapping.StateValues, 0f, 1f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace,
            0f, 0f, null, SimulationEventKind.None, PresentationEventMagnitude.Any,
            FiniteWorkValues: new(0f, 1f, 2f, 3f, 4f, 5f));
        var scene = RuntimeTestServices.Core.CreateScene(new(1), new(1), settings, physics,
            RuntimeTestServices.Core.CreateActivation([], []),
            [new(new(WorkshopDeclaredPresentation.TargetBase), frame, 0, binding, default, 0f,
                FiniteWork: actuator, Store: physics.EnergyStores[0].Id)],
            physics.Bodies.ToArray().Select(body => body.Id), []);
        await using var device = new WorkshopGpuDevice(new BrowserGpuTransport(new EventRingPublication()), physics.Document);
        var profile = new WorkshopGpuProfile(settings.Simulation, settings.Physical, new(1));
        var admission = await device.Admit(scene, new(1), profile, new(1));
        device.Commit(new(1));
        var blocked = await device.ApplyFiniteWork(admission.Read, new(2), new(new(1), actuator, first));
        device.Commit(new(2));
        Assert.Equal(FiniteWorkRequestResult.Obstructed, blocked.Read.FiniteWorkActuators[0].LastResult);
        Assert.Equal(FiniteWorkClearance.Obstructed, blocked.Read.FiniteWorkActuators[0].Clearance);
        var current = blocked.Read;
        var sawEmpty = false;
        var reseated = false;
        for (ulong sequence = 3; sequence < 243; sequence++)
        {
            var advanced = await device.Advance(current, new(sequence), new([]));
            device.Commit(new(sequence));
            current = advanced.Read;
            sawEmpty |= current.ChamberRegions[0].Phase == ChamberPhase.Empty;
            reseated = current.ChamberRegions[0].Candidate == next &&
                current.ChamberRegions[0].Phase == ChamberPhase.Seated;
            if (reseated) break;
        }
        Assert.True(sawEmpty);
        Assert.True(reseated); // two seconds is a stall guard for the authored two-metre gravity feed
        Assert.Equal(FiniteWorkClearance.Clear, current.FiniteWorkActuators[0].Clearance);
        Assert.Equal(FiniteWorkRequestResult.Obstructed, current.FiniteWorkActuators[0].LastResult);
        Assert.Equal(first, current.FiniteWorkActuators[0].LastPayload);
        Assert.Equal(45f, current.EnergyStores[0].Energy.Value);
        Assert.Equal(0u, current.FiniteWorkActuators[0].OccurrenceCount);
        Assert.Equal(0UL, device.Events.WriteSequence);
        var endpoint = new WorkshopResponse(default, WorkshopResponseKind.Read,
            new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), WorkshopSimulationPhase.Running,
            current with { Revision = new(2), Capture = new(WorkshopClockDomain.SimulationMonotonic, new(1), new(0), new(0)) },
            new(101, 206), new(2), new(1), new(1), new(1), SourcePulse: new(current.Tick.Value));
        endpoint = WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(endpoint));
        var presentation = new WorkshopDeclaredPresentation(RuntimeTestServices.Animation, scene, endpoint);
        Assert.Equal(3f, BitConverter.UInt32BitsToSingle(Assert.Single(presentation.Advance(
            new PhysicalTime(current.Tick.Value * settings.PhysicalStepsPerCommit))).Value.CanonicalBits));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnrepresentableExitSweepIsUnavailableWithoutPayingOrMoving(bool extreme)
    {
        var fixture = new LawScene(false);
        var frame = fixture.StaticBox(new(0f, 3f, 0f), new(.1f), colliderOffset: new(0f, -2f, 0f));
        var payload = fixture.Sphere(new(0f, 3f, 0f), .35f, 1f, new(0, 0, 0, 0), Vector3.Zero, Vector3.Zero, 0, 0);
        fixture.StaticBox(new(2f, 3f, 0f), new(.1f));
        var actuator = fixture.FiniteWork(frame, payload, 45f, 45f);
        var source = fixture.Admit().Scene;
        var rows = source.FiniteWorkActuators.ToArray();
        // Finite authored padding is admitted; a nonrepresentable derived sweep bound refuses safely.
        rows[0] = rows[0] with { ExitPadding = extreme ? float.MaxValue : .02f };
        var physics = new PhysicsSceneDeclaration(source.Document, source.NextIdentity,
            source.Bodies, source.Colliders, source.Materials, source.Sensors, source.Guides,
            energyStores: source.EnergyStores, chamberRegions: source.ChamberRegions, finiteWorkActuators: rows);
        var settings = WorkshopCadenceSettings.Default();
        var binding = new CompiledPresentationBinding(PresentationProperty.LocalX, PresentationDriver.CommittedFiniteWorkState,
            PresentationValueMapping.StateValues, 0f, 1f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace,
            0f, 0f, null, SimulationEventKind.None, PresentationEventMagnitude.Any,
            FiniteWorkValues: new(0f, 1f, 2f, 3f, 4f, 5f));
        var scene = RuntimeTestServices.Core.CreateScene(new(1), new(1), settings, physics,
            RuntimeTestServices.Core.CreateActivation([], []),
            [new(new(WorkshopDeclaredPresentation.TargetBase), frame, 0, binding, default, 0f,
                FiniteWork: actuator, Store: physics.EnergyStores[0].Id)],
            physics.Bodies.ToArray().Select(body => body.Id), []);
        await using var device = new WorkshopGpuDevice(new BrowserGpuTransport(new EventRingPublication()), physics.Document);
        var profile = new WorkshopGpuProfile(settings.Simulation, settings.Physical, new(1));
        var admission = await device.Admit(scene, new(1), profile, new(1));
        device.Commit(new(1));
        var result = await device.ApplyFiniteWork(admission.Read, new(2), new(new(1), actuator, payload));
        device.Commit(new(2));
        Assert.Equal(extreme ? FiniteWorkRequestResult.GeometryUnresolved : FiniteWorkRequestResult.Fired,
            result.Read.FiniteWorkActuators[0].LastResult);
        if (!extreme)
        {
            Assert.Equal(1u, result.Read.FiniteWorkActuators[0].OccurrenceCount);
            Assert.True(result.Read.EnergyStores[0].Released.Value > 0f);
            return;
        }
        Assert.Equal(ChamberPhase.Seated, result.Read.ChamberRegions[0].Phase);
        Assert.Equal(FiniteWorkClearance.GeometryUnresolved, result.Read.FiniteWorkActuators[0].Clearance);
        Assert.Equal(admission.Read.Bodies.Count, result.Read.Bodies.Count);
        for (var i = 0; i < admission.Read.Bodies.Count; i++) Assert.Equal(admission.Read.Bodies[i], result.Read.Bodies[i]);
        Assert.Equal(45f, result.Read.EnergyStores[0].Energy.Value);
        Assert.Equal(0f, result.Read.EnergyStores[0].Released.Value);
        Assert.Equal(0UL, device.Events.WriteSequence);
        var endpoint = new WorkshopResponse(default, WorkshopResponseKind.Read,
            new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), WorkshopSimulationPhase.Running,
            result.Read with { Revision = new(2), Capture = new(WorkshopClockDomain.SimulationMonotonic, new(1), new(0), new(0)) },
            new(101, 206), new(2), new(1), new(1), new(1));
        endpoint = WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(endpoint));
        Assert.Equal(FiniteWorkClearance.GeometryUnresolved, endpoint.Read.FiniteWorkActuators[0].Clearance);
        var presentation = new WorkshopDeclaredPresentation(RuntimeTestServices.Animation, scene, endpoint);
        Assert.Equal(5f, BitConverter.UInt32BitsToSingle(Assert.Single(presentation.Advance(AnimationTestTime.Seconds(0))).Value.CanonicalBits));
        var advanced = await device.Advance(result.Read, new(3), new([]));
        device.Commit(new(3));
        Assert.Equal(0u, advanced.Read.FiniteWorkActuators[0].OccurrenceCount);
        Assert.Equal(45f, advanced.Read.EnergyStores[0].Energy.Value);
        Assert.Equal(0UL, device.Events.WriteSequence);
        var reset = await device.Admit(scene, new(2), profile, new(4));
        device.Commit(new(4));
        Assert.Equal(FiniteWorkClearance.NotApplicable, reset.Read.FiniteWorkActuators[0].Clearance);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExactActuatorPaddingSurvivesReorderingDiscardCommitAndReset(bool reverse)
    {
        var fixture = new LawScene(false);
        var frame = fixture.StaticBox(new(0f, 3f, 0f), new(.1f), colliderOffset: new(0f, -2f, 0f));
        var payload = fixture.Sphere(new(0f, 3f, 0f), .35f, 1f, new(0, 0, 0, 0), Vector3.Zero, Vector3.Zero, 0, 0);
        fixture.StaticBox(new(0f, 4.5f, 0f), new(.1f));
        var first = fixture.FiniteWork(frame, payload, 45f, 0f);
        fixture.FiniteWork(frame, payload, 45f, 0f);
        var source = fixture.Admit().Scene;
        var rows = source.FiniteWorkActuators.ToArray();
        rows[1] = rows[1] with { Region = rows[0].Region, ExitPadding = .3f };
        if (reverse) Array.Reverse(rows);
        var physics = new PhysicsSceneDeclaration(source.Document, source.NextIdentity,
            source.Bodies, source.Colliders, source.Materials, source.Sensors, source.Guides,
            energyStores: source.EnergyStores, chamberRegions: source.ChamberRegions, finiteWorkActuators: rows);
        var settings = WorkshopCadenceSettings.Default();
        var scene = RuntimeTestServices.Core.CreateScene(new(1), new(1), settings, physics,
            RuntimeTestServices.Core.CreateActivation([], []), [],
            physics.Bodies.ToArray().Select(body => body.Id), []);
        await using var device = new WorkshopGpuDevice(new BrowserGpuTransport(new EventRingPublication()), physics.Document);
        var profile = new WorkshopGpuProfile(settings.Simulation, settings.Physical, new(1));
        var admission = await device.Admit(scene, new(1), profile, new(1));
        device.Commit(new(1));
        Assert.Equal(2, admission.Read.FiniteWorkActuators.Count);
        for (var i = 0; i < 2; i++) Assert.Equal(FiniteWorkClearance.NotApplicable, admission.Read.FiniteWorkActuators[i].Clearance);
        var request = new OwnedFiniteWorkRequest(new(1), first, payload);
        var discarded = await device.ApplyFiniteWork(admission.Read, new(2), request);
        Assert.Equal(FiniteWorkClearance.Clear, discarded.Read.FiniteWorkActuators[0].Clearance);
        Assert.Equal(FiniteWorkClearance.Obstructed, discarded.Read.FiniteWorkActuators[1].Clearance);
        device.Discard(new(2));
        var committed = await device.ApplyFiniteWork(admission.Read, new(3), request);
        device.Commit(new(3));
        Assert.Equal(FiniteWorkRequestResult.Uncharged, committed.Read.FiniteWorkActuators[0].LastResult);
        Assert.Equal(FiniteWorkClearance.Clear, committed.Read.FiniteWorkActuators[0].Clearance);
        Assert.Equal(FiniteWorkClearance.Obstructed, committed.Read.FiniteWorkActuators[1].Clearance);
        Assert.Equal(admission.Read.Tick, committed.Read.Tick);
        for (var i = 0; i < committed.Read.EnergyStores.Count; i++) Assert.Equal(0f, committed.Read.EnergyStores[i].Energy.Value);
        Assert.Equal(0UL, device.Events.WriteSequence);
        var response = new WorkshopResponse(default, WorkshopResponseKind.Read,
            new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), WorkshopSimulationPhase.Running,
            committed.Read with { Revision = new(2), Capture = new(WorkshopClockDomain.SimulationMonotonic, new(1), new(0), new(0)) },
            new(101, 206), new(2), new(1), new(1), new(1));
        var decoded = WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(response));
        Assert.Equal(2, decoded.Read.FiniteWorkActuators.Count);
        for (var i = 0; i < 2; i++) Assert.Equal(committed.Read.FiniteWorkActuators[i], decoded.Read.FiniteWorkActuators[i]);
        _ = new WorkshopDeclaredPresentation(RuntimeTestServices.Animation, scene, decoded);
        var wrongPhase = decoded with { Read = decoded.Read with
        {
            FiniteWorkActuators = new PhysicsFiniteWorkActuatorRead(
                [decoded.Read.FiniteWorkActuators[0] with { Clearance = FiniteWorkClearance.NotApplicable },
                 decoded.Read.FiniteWorkActuators[1]])
        } };
        wrongPhase = WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(wrongPhase));
        Assert.Throws<ArgumentException>(() => new WorkshopDeclaredPresentation(RuntimeTestServices.Animation, scene, wrongPhase));
        var malformed = WorkshopWireFlatBuffers.EncodeResponse(decoded);
        var buffer = new Google.FlatBuffers.ByteBuffer(malformed);
        var table = new Google.FlatBuffers.Table(4 + buffer.GetInt(4), buffer);
        var actuatorVector = table.__offset(4 + 35 * 2); // WorkshopResponse.finite_work_actuators
        Assert.NotEqual(0, actuatorVector);
        malformed[table.__vector(actuatorVector) + 44] = byte.MaxValue; // current struct's Clearance lane
        Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeResponse(malformed));
        var reset = await device.Admit(scene, new(2), profile, new(4));
        device.Commit(new(4));
        for (var i = 0; i < reset.Read.FiniteWorkActuators.Count; i++)
        {
            Assert.Equal(FiniteWorkClearance.NotApplicable, reset.Read.FiniteWorkActuators[i].Clearance);
            Assert.Null(reset.Read.FiniteWorkActuators[i].LastResult);
        }
    }

    [Theory]
    [InlineData(1u)] // Clear is defined, but an admitted Empty region cannot claim it.
    [InlineData(4u)]
    [InlineData(256u)]
    [InlineData(uint.MaxValue)]
    public void UnavailableChamberRejectsClaimedOrUndefinedClearance(uint encoded)
    {
        var fixture = new LawScene(false);
        var frame = fixture.StaticBox(new(0f, 3f, 0f), new(.1f), colliderOffset: new(0f, -2f, 0f));
        var payload = fixture.Sphere(new(0f, 3f, 0f), .35f, 1f, new(0, 0, 0, 0), Vector3.Zero, Vector3.Zero, 0, 0);
        fixture.FiniteWork(frame, payload, 45f, 45f);
        var world = fixture.Admit();
        var offset = world.Scene.World.FiniteWorkActuatorOffset(0) + PhysicsGpuAbi.ActuatorClearanceOffset;
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(world.Admitted.AsSpan(offset, 4), encoded);
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadFiniteWorkActuators(world.Admitted));
    }


    [Fact]
    public async Task NaturalCurvedContainmentRefusalIsUnavailableUntilKnownExitAndResets()
    {
        var fixture = new LawScene(false);
        var frame = fixture.StaticBox(new(0f, 3f, 0f), new(.1f), colliderOffset: new(0f, -2f, 0f));
        // Exact unit quaternion permutes axes without90-degree trigonometric residue.
        var payload = fixture.Cylinder(new(0f, 3f, 0f), .375f, .5f, 1f, new(0, 0, 0, 0),
            new(.5f, .5f, .5f, .5f), Vector3.UnitY, gravity: 0f);
        var actuator = fixture.FiniteWork(frame, payload, 45f, 45f,
            regionRadius: .625f, maximumWidth: 1.1f, containmentAllowance: 0f);
        var world = fixture.Admit();
        var settings = WorkshopCadenceSettings.Default();
        var binding = new CompiledPresentationBinding(PresentationProperty.LocalX, PresentationDriver.CommittedFiniteWorkState,
            PresentationValueMapping.StateValues, 0f, 1f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace,
            0f, 0f, null, SimulationEventKind.None, PresentationEventMagnitude.Any,
            FiniteWorkValues: new(0f, 1f, 2f, 3f, 4f, 5f));
        var scene = RuntimeTestServices.Core.CreateScene(new(1), new(1), settings, world.Scene,
            RuntimeTestServices.Core.CreateActivation([], []),
            [new(new(WorkshopDeclaredPresentation.TargetBase), frame, 0, binding, default, 0f,
                FiniteWork: actuator, Store: world.Scene.EnergyStores[0].Id)],
            world.Scene.Bodies.ToArray().Select(body => body.Id), []);
        await using var device = new WorkshopGpuDevice(new BrowserGpuTransport(new EventRingPublication()), world.Scene.Document);
        var profile = new WorkshopGpuProfile(settings.Simulation, settings.Physical, new(1));
        var admitted = await device.Admit(scene, new(1), profile, new(1));
        device.Commit(new(1)); device.DrainEvents();
        var refused = await device.ApplyFiniteWork(admitted.Read, new(2), new(new(1), actuator, payload));
        device.Commit(new(2));
        Assert.Equal(FiniteWorkRequestResult.GeometryUnresolved, refused.Read.FiniteWorkActuators[0].LastResult);
        Assert.Equal(ChamberPhase.GeometryUnresolved, refused.Read.ChamberRegions[0].Phase);
        Assert.Equal(0u, refused.Read.ChamberRegions[0].Occupancy); // unavailable count does not mean Empty
        Assert.Equal(default, refused.Read.ChamberRegions[0].Candidate);
        Assert.Equal(45f, refused.Read.EnergyStores[0].Energy.Value);
        Assert.Equal(0f, refused.Read.EnergyStores[0].Released.Value);
        Assert.Equal(0UL, device.Events.WriteSequence);
        var endpoint = new WorkshopResponse(default, WorkshopResponseKind.Read,
            new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), WorkshopSimulationPhase.Running,
            refused.Read with { Revision = new(2), Capture = new(WorkshopClockDomain.SimulationMonotonic, new(1), new(0), new(0)) },
            new(101, 206), new(2), new(1), new(1), new(1));
        endpoint = WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(endpoint));
        var presentation = new WorkshopDeclaredPresentation(RuntimeTestServices.Animation, scene, endpoint);
        var sample = Assert.Single(presentation.Advance(AnimationTestTime.Seconds(0)));
        Assert.Equal(5f, BitConverter.UInt32BitsToSingle(sample.Value.CanonicalBits));
        var current = refused.Read;
        for (ulong sequence = 3; sequence < 243; sequence++)
        {
            var advanced = await device.Advance(current, new(sequence), new([]));
            device.Commit(new(sequence));
            current = advanced.Read;
        }
        Assert.Equal(ChamberPhase.Empty, current.ChamberRegions[0].Phase);
        Assert.Equal(0u, current.FiniteWorkActuators[0].OccurrenceCount);
        Assert.Equal(45f, current.EnergyStores[0].Energy.Value);
        Assert.Equal(0UL, device.Events.WriteSequence); // rejected request never becomes a delayed shot
        var reset = await device.Admit(scene, new(2), profile, new(243));
        device.Commit(new(243));
        Assert.Equal(ChamberPhase.Empty, reset.Read.ChamberRegions[0].Phase);
        Assert.Null(reset.Read.FiniteWorkActuators[0].LastResult);
        Assert.Equal(45f, reset.Read.EnergyStores[0].Energy.Value);
        var again = await device.ApplyFiniteWork(reset.Read, new(244), new(new(2), actuator, payload));
        Assert.Equal(FiniteWorkRequestResult.GeometryUnresolved, again.Read.FiniteWorkActuators[0].LastResult);
        device.Discard(new(244));
    }

    [Theory]
    [InlineData(12u)]
    [InlineData(257u)]
    [InlineData(uint.MaxValue)]
    public void UndefinedOrWrappedActuatorResultIsRefused(uint encoded)
    {
        var fixture = new LawScene(false);
        var frame = fixture.StaticBox(new(0f, 3f, 0f), new(.1f), colliderOffset: new(0f, -2f, 0f));
        var payload = fixture.Sphere(new(0f, 3f, 0f), .35f, 1f, new(0, 0, 0, 0), Vector3.Zero, Vector3.Zero, 0, 0);
        fixture.FiniteWork(frame, payload, 45f, 45f);
        var world = fixture.Admit();
        var offset = world.Scene.World.FiniteWorkActuatorOffset(0) + PhysicsGpuAbi.ActuatorResultOffset;
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(world.Admitted.AsSpan(offset, 4), encoded);
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadFiniteWorkActuators(world.Admitted));
    }

    [Theory]
    [InlineData(FiniteWorkTransactionRefusal.NoWorld)]
    [InlineData(FiniteWorkTransactionRefusal.StaleEpoch)]
    [InlineData(FiniteWorkTransactionRefusal.InvalidSequence)]
    [InlineData(FiniteWorkTransactionRefusal.UnknownActuator)]
    [InlineData(FiniteWorkTransactionRefusal.ForeignTarget)]
    [InlineData(FiniteWorkTransactionRefusal.NonDynamicTarget)]
    public async Task RefusedOwnedIdentityLeavesAValidReleaseAvailable(FiniteWorkTransactionRefusal refusal)
    {
        var fixture = new LawScene(false);
        var frame = fixture.StaticBox(new(0f, 3f, 0f), new(.1f), colliderOffset: new(0f, -2f, 0f));
        var payload = fixture.Sphere(new(0f, 3f, 0f), .35f, 1f, new(0, 0, 0, 0), Vector3.Zero, Vector3.Zero, 0, 0);
        var excluded = fixture.Sphere(new(3f, 3f, 0f), .35f, 1f, new(0, 0, 0, 0), Vector3.Zero, Vector3.Zero, 0, 0);
        var actuator = fixture.FiniteWork(frame, payload, 45f, 45f);
        var world = fixture.Admit();
        var settings = WorkshopCadenceSettings.Default();
        var scene = RuntimeTestServices.Core.CreateScene(new(1), new(1), settings, world.Scene,
            RuntimeTestServices.Core.CreateActivation([], []), [], world.Scene.Bodies.ToArray().Select(body => body.Id), []);
        await using var device = new WorkshopGpuDevice(new BrowserGpuTransport(new EventRingPublication()), world.Scene.Document);
        var valid = new OwnedFiniteWorkRequest(new(1), actuator, payload);
        if (refusal == FiniteWorkTransactionRefusal.NoWorld)
        {
            var absent = await Assert.ThrowsAsync<FiniteWorkTransactionException>(async () =>
                await device.ApplyFiniteWork(default, new(1), valid));
            Assert.Equal(refusal, absent.Reason);
        }
        var admitted = await device.Admit(scene, new(1), new(settings.Simulation, settings.Physical, new(1)), new(1));
        device.Commit(new(1)); device.DrainEvents();
        if (refusal != FiniteWorkTransactionRefusal.NoWorld)
        {
            var request = refusal switch
            {
                FiniteWorkTransactionRefusal.StaleEpoch => valid with { Epoch = new(2) },
                FiniteWorkTransactionRefusal.UnknownActuator => valid with { Actuator = new(ulong.MaxValue) },
                FiniteWorkTransactionRefusal.ForeignTarget => valid with { Payload = new(ulong.MaxValue) },
                FiniteWorkTransactionRefusal.NonDynamicTarget => valid with { Payload = frame },
                _ => valid
            };
            var sequence = refusal == FiniteWorkTransactionRefusal.InvalidSequence ? new CommandSequence(1) : new(2);
            var rejected = await Assert.ThrowsAsync<FiniteWorkTransactionException>(async () =>
                await device.ApplyFiniteWork(admitted.Read, sequence, request));
            Assert.Equal(refusal, rejected.Reason);
            if (refusal == FiniteWorkTransactionRefusal.ForeignTarget)
            {
                var outsideSelector = await Assert.ThrowsAsync<FiniteWorkTransactionException>(async () =>
                    await device.ApplyFiniteWork(admitted.Read, new(2), valid with { Payload = excluded }));
                Assert.Equal(FiniteWorkTransactionRefusal.ForeignTarget, outsideSelector.Reason);
            }
        }
        Assert.Equal(45f, PhysicsGpuAbi.ReadEnergyStores(device.CommittedWorld.Span)[0].Energy.Value);
        Assert.Equal(new PhysicalCommitRevision(0), PhysicsGpuAbi.ReadPhysicalRevision(device.CommittedWorld.Span));
        Assert.Equal(0UL, device.Events.WriteSequence);
        var released = await device.ApplyFiniteWork(admitted.Read, new(2), valid);
        device.Commit(new(2));
        Assert.Equal(FiniteWorkRequestResult.Fired, released.Read.FiniteWorkActuators[0].LastResult);
        Assert.Equal(1u, released.Read.FiniteWorkActuators[0].OccurrenceCount);
    }

    [Fact]
    public async Task KnownUnselectedDynamicTargetCommitsMismatchWithoutPaying()
    {
        var fixture = new LawScene(false);
        var frame = fixture.StaticBox(new(0f, 3f, 0f), new(.1f), colliderOffset: new(0f, -2f, 0f));
        var payload = fixture.Sphere(new(0f, 3f, 0f), .35f, 1f, new(0, 0, 0, 0), Vector3.Zero, Vector3.Zero, 0, 0);
        var other = fixture.Sphere(new(3f, 3f, 0f), .35f, 1f, new(0, 0, 0, 0), Vector3.Zero, Vector3.Zero, 0, 0);
        var actuator = fixture.FiniteWork(frame, payload, 45f, 45f, targets: new(BodyTargetKind.AllDynamic, default));
        var world = fixture.Admit();
        var settings = WorkshopCadenceSettings.Default();
        var scene = RuntimeTestServices.Core.CreateScene(new(1), new(1), settings, world.Scene,
            RuntimeTestServices.Core.CreateActivation([], []), [], world.Scene.Bodies.ToArray().Select(body => body.Id), []);
        await using var device = new WorkshopGpuDevice(new BrowserGpuTransport(new EventRingPublication()), world.Scene.Document);
        var admitted = await device.Admit(scene, new(1), new(settings.Simulation, settings.Physical, new(1)), new(1));
        device.Commit(new(1)); device.DrainEvents();
        var mismatch = await device.ApplyFiniteWork(admitted.Read, new(2), new(new(1), actuator, other));
        device.Commit(new(2));
        Assert.Equal(FiniteWorkRequestResult.TargetMismatch, mismatch.Read.FiniteWorkActuators[0].LastResult);
        Assert.Equal(other, mismatch.Read.FiniteWorkActuators[0].LastPayload);
        var response = new WorkshopResponse(default, WorkshopResponseKind.Read,
            new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), WorkshopSimulationPhase.Running,
            mismatch.Read with { Revision = new(2), Capture = new(WorkshopClockDomain.SimulationMonotonic, new(1), new(0), new(0)) },
            new(101, 206), new(2), new(1), new(1), new(1));
        var decoded = WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(response));
        Assert.Equal(FiniteWorkRequestResult.TargetMismatch, decoded.Read.FiniteWorkActuators[0].LastResult);
        Assert.Equal(other, decoded.Read.FiniteWorkActuators[0].LastPayload);
        Assert.Equal(0f, mismatch.Read.EnergyStores[0].Released.Value);
        Assert.Equal(45f, mismatch.Read.EnergyStores[0].Energy.Value);
        Assert.Equal(0UL, device.Events.WriteSequence);
        var release = await device.ApplyFiniteWork(mismatch.Read, new(3), new(new(1), actuator, payload));
        device.Commit(new(3));
        Assert.Equal(FiniteWorkRequestResult.Fired, release.Read.FiniteWorkActuators[0].LastResult);
        Assert.Equal(payload, release.Read.FiniteWorkActuators[0].LastPayload);
    }

    [Theory]
    [InlineData(10f, 5f, .5f, FiniteWorkRequestResult.Uncharged, 2f)]
    [InlineData(10f, 10f, 1f, FiniteWorkRequestResult.Fired, 4f)]
    [InlineData(0f, 0f, .001f, FiniteWorkRequestResult.NoResponse, 3f)]
    public async Task StoredFractionAndAvailabilityUsePreloadAndSameTimeCommittedState(
        float capacity, float initialEnergy, float initialFraction, FiniteWorkRequestResult expected, float state)
    {
        var fixture = new LawScene(false);
        var centre = new Vector3(0, 3, 0);
        var frame = fixture.StaticBox(centre, new(.1f), colliderOffset: new(0, -2, 0));
        var payload = fixture.Sphere(centre, .35f, 1f, new(0, 0, 0, 0), Vector3.Zero, Vector3.Zero, 0, 0);
        var actuator = fixture.FiniteWork(frame, payload, capacity, initialEnergy);
        var world = fixture.Admit();
        var store = world.Scene.FiniteWorkActuators[0].Store;
        var fraction = new CompiledPresentationBinding(PresentationProperty.ScaleX, PresentationDriver.CommittedStoredFraction,
            PresentationValueMapping.ClampToEndpoints, .001f, 1f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace,
            0f, 0f, null, SimulationEventKind.None, PresentationEventMagnitude.Any);
        var availability = fraction with { Property = PresentationProperty.LocalX, Driver = PresentationDriver.CommittedFiniteWorkState,
            Mapping = PresentationValueMapping.StateValues, From = 0f, To = 1f,
            FiniteWorkValues = new(0f, 1f, 2f, 3f, 4f, 5f) };
        CompiledPresentationTrack[] tracks =
        [
            new(new(WorkshopDeclaredPresentation.TargetBase), frame, 0, fraction, default, 0f, Store: store),
            new(new(WorkshopDeclaredPresentation.TargetBase + 1), frame, 1, availability, default, 0f, FiniteWork: actuator, Store: store)
        ];
        var settings = WorkshopCadenceSettings.Default();
        var scene = RuntimeTestServices.Core.CreateScene(new(1), new(1), settings, world.Scene,
            RuntimeTestServices.Core.CreateActivation([], []), tracks, world.Scene.Bodies.ToArray().Select(body => body.Id), []);
        var command = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(1), default, scene,
            Session: new(101, 206), Cadence: new(1), Projection: new(1));
        scene = WorkshopWireFlatBuffers.DecodeCommand(WorkshopWireFlatBuffers.EncodeCommand(command)).Scene!;
        Assert.Equal(fraction, scene.Presentation[0].Declaration);
        Assert.Equal(availability, scene.Presentation[1].Declaration);
        await using var device = new WorkshopGpuDevice(new BrowserGpuTransport(new EventRingPublication()), world.Scene.Document);
        var profile = new WorkshopGpuProfile(settings.Simulation, settings.Physical, new(1));
        var admitted = await device.Admit(scene, new(1), profile, new(1));
        device.Commit(new(1)); device.DrainEvents();
        WorkshopResponse Endpoint(WorkshopRead read, ulong publication) => new(default, WorkshopResponseKind.Read,
            new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), WorkshopSimulationPhase.Running,
            read with { Revision = new(publication), Capture = new(WorkshopClockDomain.SimulationMonotonic, new(1), new(0), new(0)) },
            new(101, 206), new(publication), new(1), new(1), new(1));
        static float[] Values(WorkshopDeclaredPresentation evaluator) => evaluator.Advance(AnimationTestTime.Seconds(0))
            .OrderBy(sample => sample.Binding).Select(sample => BitConverter.UInt32BitsToSingle(sample.Value.CanonicalBits)).ToArray();
        var presentation = new WorkshopDeclaredPresentation(RuntimeTestServices.Animation, scene, Endpoint(admitted.Read, 1));
        Assert.Equal(new[] { initialFraction, 0f }, Values(presentation));
        var result = await device.ApplyFiniteWork(admitted.Read, new(2), new(new(1), actuator, payload));
        device.Commit(new(2));
        Assert.Equal(expected, result.Read.FiniteWorkActuators[0].LastResult);
        Assert.Equal(admitted.Read.Tick, result.Read.Tick);
        Assert.Equal(new PhysicalCommitRevision(1), result.Read.PhysicalRevision);
        presentation.Accept(Endpoint(result.Read, 2));
        var fractionAfter = expected == FiniteWorkRequestResult.Fired ? .001f : initialFraction;
        Assert.Equal(new[] { fractionAfter, state }, Values(presentation));
        var reset = await device.Admit(scene, new(2), profile, new(3));
        device.Commit(new(3));
        Assert.Equal(initialEnergy, reset.Read.EnergyStores[0].Energy.Value);
        Assert.Equal(ChamberPhase.Empty, reset.Read.ChamberRegions[0].Phase);
        var resetPresentation = new WorkshopDeclaredPresentation(RuntimeTestServices.Animation, scene, Endpoint(reset.Read, 3));
        Assert.Equal(new[] { initialFraction, 0f }, Values(resetPresentation));
    }

    [Fact]
    public async Task TickZeroReleaseCommitsOnceWithoutTimeAndDiscardKeepsOwnershipUsable()
    {
        var fixture = new LawScene(false);
        var centre = new Vector3(0, 3, 0);
        var frame = fixture.StaticBox(centre, new(.1f), colliderOffset: new(0, -2, 0));
        var payload = fixture.Sphere(centre, .35f, 1f, new(0, 0, 0, 0), Vector3.Zero, Vector3.Zero, 0, 0);
        var actuator = fixture.FiniteWork(frame, payload, 45f, 45f);
        var world = fixture.Admit();
        var settings = WorkshopCadenceSettings.Default();
        var scene = RuntimeTestServices.Core.CreateScene(new(1), new(1), settings, world.Scene,
            RuntimeTestServices.Core.CreateActivation([], []), [], world.Scene.Bodies.ToArray().Select(body => body.Id), []);
        var publication = new EventRingPublication();
        await using var device = new WorkshopGpuDevice(new BrowserGpuTransport(publication), world.Scene.Document);
        var profile = new WorkshopGpuProfile(settings.Simulation, settings.Physical, new(1));
        var admission = await device.Admit(scene, new(1), profile, new(1));
        device.Commit(new(1)); device.DrainEvents();
        var request = new OwnedFiniteWorkRequest(new(1), actuator, payload);
        var discarded = await device.ApplyFiniteWork(admission.Read, new(2), request);
        Assert.Equal(FiniteWorkRequestResult.Fired, discarded.Read.FiniteWorkActuators[0].LastResult);
        var busy = await Assert.ThrowsAsync<FiniteWorkTransactionException>(async () =>
            await device.ApplyFiniteWork(admission.Read, new(3), request));
        Assert.Equal(FiniteWorkTransactionRefusal.Busy, busy.Reason);
        Assert.Empty(device.DrainEvents());
        device.Discard(new(2));
        Assert.Equal(45f, PhysicsGpuAbi.ReadEnergyStores(device.CommittedWorld.Span)[0].Energy.Value);
        Assert.Equal(new PhysicalCommitRevision(0), PhysicsGpuAbi.ReadPhysicalRevision(device.CommittedWorld.Span));
        Assert.Equal(0UL, device.Events.WriteSequence);
        var released = await device.ApplyFiniteWork(admission.Read, new(3), request);
        device.Commit(new(3));
        Assert.Equal(new SimulationTick(0), released.Read.Tick);
        Assert.Equal(new PhysicalCommitRevision(1), released.Read.PhysicalRevision);
        Assert.True(released.Read.Bodies.TryGet(payload, out var current));
        Assert.True(admission.Read.Bodies.TryGet(payload, out var original));
        Assert.Equal(original.Body.Cell, current.Body.Cell);
        Assert.Equal(original.Body.Local, current.Body.Local);
        Assert.Equal(original.Rotation, current.Rotation);
        // .01 m/s excludes 1 cm of aim error over a one-second flight; .005 J excludes
        // free work sufficient to raise a 0.5 kg load by 1 mm.
        Assert.InRange(current.Body.Velocity.Y - MathF.Sqrt(90f), -.01f, .01f);
        Assert.InRange(released.Read.EnergyStores[0].Released.Value - 45f, -.005f, .005f);
        Assert.Equal(0, released.Read.Motion!.Count);
        released.Read.Bodies.ValidateScene(world.Scene, released.Read.Epoch, released.Read.Tick, released.Read.PhysicalRevision);
        var batch = device.DrainEvents();
        var header = EventRingHeader.Read(batch);
        Assert.Equal(1UL, header.WriteSequence);
        var occurrence = SimulationEventRecord.Read(batch.AsSpan(EventRingBuffer.HeaderBytes, EventRingBuffer.SlotBytes));
        Assert.Equal(SimulationEventKind.FiniteWorkReleased, occurrence.Kind);
        Assert.Equal(actuator.Value, occurrence.Source.Value);
        Assert.Equal(payload.Value, occurrence.Other);
        Assert.Equal((0UL, 0u, 0f, 1u), (occurrence.Tick, occurrence.Ordinal, occurrence.Phase, occurrence.Count));
        Assert.InRange(occurrence.Magnitude - 45f, -.005f, .005f);
        Assert.Empty(device.DrainEvents());
        var stale = await Assert.ThrowsAsync<FiniteWorkTransactionException>(async () =>
            await device.ApplyFiniteWork(admission.Read, new(4), request));
        Assert.Equal(FiniteWorkTransactionRefusal.StaleCommit, stale.Reason);
        var again = await device.ApplyFiniteWork(released.Read, new(4), request);
        Assert.Equal(FiniteWorkRequestResult.Busy, again.Read.FiniteWorkActuators[0].LastResult);
        device.Commit(new(4));
        Assert.Equal(new PhysicalCommitRevision(2), again.Read.PhysicalRevision);
        Assert.Equal(1u, again.Read.FiniteWorkActuators[0].OccurrenceCount);
        Assert.Equal(1UL, EventRingHeader.Read(device.DrainEvents()).WriteSequence);
        var advanced = await device.Advance(again.Read, new(5), new([]));
        device.Commit(new(5));
        Assert.Equal(new SimulationTick(1), advanced.Read.Tick);
        Assert.Equal(new PhysicalCommitRevision(3), advanced.Read.PhysicalRevision);
        Assert.Equal(1u, advanced.Read.FiniteWorkActuators[0].OccurrenceCount);
        var reset = await device.Admit(scene, new(2), profile, new(6));
        device.Commit(new(6));
        Assert.Equal(new PhysicalCommitRevision(0), reset.Read.PhysicalRevision);
        Assert.Equal(45f, reset.Read.EnergyStores[0].Energy.Value);
        Assert.Null(reset.Read.FiniteWorkActuators[0].LastResult);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TwoSameOwnerActuatorsPublishTheirOwnStoreWorkAtTheSameTime(bool reverse)
    {
        var fixture = new LawScene(false);
        var centre = new Vector3(0, 3, 0);
        var frame = fixture.StaticBox(centre, new(.1f), colliderOffset: new(0, -2, 0));
        var left = fixture.Sphere(centre - 2 * Vector3.UnitX, .35f, 1f, new(0, 0, 0, 0), Vector3.Zero, Vector3.Zero, 0, 0);
        var right = fixture.Sphere(centre + 2 * Vector3.UnitX, .35f, 1f, new(0, 0, 0, 0), Vector3.Zero, Vector3.Zero, 0, 0);
        FiniteWorkActuatorId a, b;
        if (reverse)
        {
            b = fixture.FiniteWork(frame, right, 8f, 8f, 2 * Vector3.UnitX);
            a = fixture.FiniteWork(frame, left, 45f, 45f, -2 * Vector3.UnitX);
        }
        else
        {
            a = fixture.FiniteWork(frame, left, 45f, 45f, -2 * Vector3.UnitX);
            b = fixture.FiniteWork(frame, right, 8f, 8f, 2 * Vector3.UnitX);
        }
        var world = fixture.Admit();
        var settings = WorkshopCadenceSettings.Default();
        var declaration = new CompiledPresentationBinding(PresentationProperty.LocalX, PresentationDriver.Event,
            PresentationValueMapping.Identity, 0f, 1f, PresentationCurve.SmoothStep, 1f, PresentationOverlap.Replace,
            0f, 0f, null, SimulationEventKind.FiniteWorkReleased, PresentationEventMagnitude.Any);
        var aStore = world.Scene.FiniteWorkActuators.ToArray().Single(value => value.Id == a).Store;
        var bStore = world.Scene.FiniteWorkActuators.ToArray().Single(value => value.Id == b).Store;
        CompiledPresentationTrack[] tracks =
        [
            new(new(WorkshopDeclaredPresentation.TargetBase), frame, 0, declaration, default, 0f, FiniteWork: a, Store: aStore),
            new(new(WorkshopDeclaredPresentation.TargetBase + 1), frame, 1, declaration, default, 0f, FiniteWork: b, Store: bStore)
        ];
        var scene = RuntimeTestServices.Core.CreateScene(new(1), new(1), settings, world.Scene,
            RuntimeTestServices.Core.CreateActivation([], []), tracks, world.Scene.Bodies.ToArray().Select(body => body.Id), []);
        var command = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(1), default, scene,
            Session: new(101, 206), Cadence: new(1), Projection: new(1));
        scene = WorkshopWireFlatBuffers.DecodeCommand(WorkshopWireFlatBuffers.EncodeCommand(command)).Scene!;
        Assert.Equal(a, scene.Presentation[0].FiniteWork); Assert.Equal(aStore, scene.Presentation[0].Store);
        Assert.Equal(b, scene.Presentation[1].FiniteWork); Assert.Equal(bStore, scene.Presentation[1].Store);
        Assert.Throws<ArgumentException>(() => RuntimeTestServices.Core.CreateScene(new(2), new(1), settings, world.Scene,
            RuntimeTestServices.Core.CreateActivation([], []), [tracks[0] with { Store = bStore }, tracks[1]],
            world.Scene.Bodies.ToArray().Select(body => body.Id), []));
        await using var device = new WorkshopGpuDevice(new BrowserGpuTransport(new EventRingPublication()), world.Scene.Document);
        var initial = await device.Admit(scene, new(1), new(settings.Simulation, settings.Physical, new(1)), new(1));
        device.Commit(new(1)); device.DrainEvents();
        var first = await device.ApplyFiniteWork(initial.Read, new(2), new(new(1), a, left));
        device.Commit(new(2));
        var discarded = await device.ApplyFiniteWork(first.Read, new(3), new(new(1), b, right));
        Assert.Equal(new PhysicalCommitRevision(2), discarded.Read.PhysicalRevision);
        Assert.Empty(device.DrainEvents());
        device.Discard(new(3));
        var staleSequence = await Assert.ThrowsAsync<FiniteWorkTransactionException>(async () =>
            await device.ApplyFiniteWork(first.Read, new(3), new(new(1), b, right)));
        Assert.Equal(FiniteWorkTransactionRefusal.InvalidSequence, staleSequence.Reason);
        var second = await device.ApplyFiniteWork(first.Read, new(4), new(new(1), b, right));
        Assert.Empty(device.DrainEvents());
        device.Commit(new(4));
        Assert.Equal(new SimulationTick(0), second.Read.Tick);
        var batch = device.DrainEvents();
        Assert.Equal(2UL, EventRingHeader.Read(batch).WriteSequence);
        Assert.Equal(EventRingBuffer.HeaderBytes + 2 * EventRingBuffer.SlotBytes, batch.Length);
        var records = new[]
        {
            SimulationEventRecord.Read(batch.AsSpan(EventRingBuffer.HeaderBytes, EventRingBuffer.SlotBytes)),
            SimulationEventRecord.Read(batch.AsSpan(EventRingBuffer.HeaderBytes + EventRingBuffer.SlotBytes, EventRingBuffer.SlotBytes))
        };
        Assert.Equal(new[] { a.Value, b.Value }, records.Select(record => record.Source.Value));
        Assert.Equal(new[] { left.Value, right.Value }, records.Select(record => record.Other));
        Assert.Equal(new ulong[] { 1, 2 }, records.Select(record => record.Sequence));
        Assert.All(records, record => { Assert.Equal(0UL, record.Tick); Assert.Equal(0u, record.Ordinal); });
        foreach (var record in records)
        {
            var declared = Assert.Single(scene.Physics.FiniteWorkActuators.ToArray(), value => value.Id.Value == record.Source.Value);
            var store = Assert.Single(PhysicsGpuAbi.ReadEnergyStores(device.CommittedWorld.Span), value => value.Id == declared.Store);
            Assert.Equal(frame, store.Owner);
            Assert.InRange(store.Released.Value - record.Magnitude, -.005f, .005f);
            Assert.InRange(store.Energy.Value, 0f, .005f);
        }
        Assert.InRange(records[0].Magnitude - 45f, -.005f, .005f);
        Assert.InRange(records[1].Magnitude - 8f, -.005f, .005f);
        Assert.Empty(device.DrainEvents());
        WorkshopResponse Endpoint(WorkshopRead read, ulong publication) => new(default, WorkshopResponseKind.Read,
            new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), WorkshopSimulationPhase.Running,
            read with { Revision = new(publication), Capture = new(WorkshopClockDomain.SimulationMonotonic, new(1), new(0), new(0)) },
            new(101, 206), new(publication), new(1), new(1), new(1));
        var presentation = new WorkshopDeclaredPresentation(RuntimeTestServices.Animation, scene, Endpoint(second.Read, 1));
        var projection = new WorkshopWorldProjection(new(1), new(1), new(0), new(0), WorldPlayback.Running);
        presentation.AcceptEvents(new(1, 0, [records[0]]), projection);
        // Halfway through this one-second cue, only the selected writer is visibly in its central half.
        var samples = presentation.Advance(AnimationTestTime.Seconds(.5)).OrderBy(value => value.Binding).ToArray();
        Assert.InRange(BitConverter.UInt32BitsToSingle(samples[0].Value.CanonicalBits), .25f, .75f);
        Assert.Equal(0f, BitConverter.UInt32BitsToSingle(samples[1].Value.CanonicalBits));
        Assert.Throws<ArgumentException>(() => presentation.AcceptEvents(new(1, 0, [records[1] with { Source = new(frame.Value) }]), projection));
        Assert.Throws<ArgumentException>(() => presentation.AcceptEvents(new(2, 0, [records[1]]), projection));
        presentation.AcceptEvents(new(1, 0, [records[1]]), projection);
        samples = presentation.Advance(AnimationTestTime.Seconds(.5)).OrderBy(value => value.Binding).ToArray();
        Assert.All(samples, sample => Assert.InRange(BitConverter.UInt32BitsToSingle(sample.Value.CanonicalBits), .25f, .75f));
    }


    [Fact]
    public async Task IdleAfterTicksPreservesClockCreditAndCompletedMotion()
    {
        var fixture = new LawScene(false);
        var centre = new Vector3(0, 3, 0);
        var frame = fixture.StaticBox(centre, new(.1f), colliderOffset: new(0, -2, 0));
        var payload = fixture.Sphere(centre, .35f, 1f, new(0, 0, 0, 0), Vector3.Zero, Vector3.Zero, 0, 0);
        var traveller = fixture.Sphere(centre + 4 * Vector3.UnitX, .2f, 1f, new(0, 0, 0, 0), Vector3.UnitX, Vector3.Zero, 0, 0);
        var capture = fixture.Residence(frame, traveller, 4 * Vector3.UnitX);
        fixture.Sphere(new(5.9f, 3f, 0f), .04f, 1f, new(0, 0, 0, 0), 2 * Vector3.UnitX, Vector3.Zero, 0f, 0f);
        var release = fixture.FiniteWork(frame, payload, 45f, 45f);
        var charging = fixture.FiniteWork(frame, traveller, 45f, 0f, 4 * Vector3.UnitX);
        var source = fixture.Source(frame);
        var storage = fixture.Admit().Scene.FiniteWorkActuators.ToArray().Single(value => value.Id == charging).Store;
        fixture.StoreConsumer(frame, source, storage);
        var world = fixture.Admit();
        var provider = world.Scene.SupplyGraph.Nodes.ToArray().Single(node => node.Kind == SupplyNodeKind.Source).Id;
        var clock = new ActivationNodeDeclaration(new(1000), frame, ActivationNodeKind.Oscillator, default,
            IntervalTicks: 12, PowerProvider: provider);
        var timer = new ActivationNodeDeclaration(new(1001), frame, ActivationNodeKind.Timer, default, 12);
        var passage = new PassageSensorDeclaration(new(1002), frame, new(96, 48, 0), default, CanonicalRotation.Identity, new(.5f), new(.02f));
        var passageNode = new ActivationNodeDeclaration(new(1003), frame, ActivationNodeKind.PassageSource, default, Passage: passage.Id);
        var settings = WorkshopCadenceSettings.Default();
        var scene = RuntimeTestServices.Core.CreateScene(new(1), new(1), settings, world.Scene,
            RuntimeTestServices.Core.CreateActivation([clock, timer, passageNode], [new(clock.Id, timer.Id, PortRole.ActivationIn)], passageSensors: [passage]), [], world.Scene.Bodies.ToArray().Select(body => body.Id), []);
        await using var device = new WorkshopGpuDevice(new BrowserGpuTransport(new EventRingPublication()), world.Scene.Document);
        var current = await device.Admit(scene, new(1), new(settings.Simulation, settings.Physical, new(1)), new(1));
        device.Commit(new(1)); device.DrainEvents();
        for (ulong sequence = 2; sequence <= 14; sequence++)
        {
            current = await device.Advance(current.Read, new(sequence), new([ElectricalEnable.Enabled]));
            device.Commit(new(sequence)); device.DrainEvents();
        }
        var before = current.Read;
        Assert.Equal(13UL, before.Tick.Value);
        Assert.True(before.Captures.TryGet(new(capture, traveller), out var captured));
        Assert.Equal(CaptureLatchPhase.Latched, captured.Phase);
        var passed = Enumerable.Range(0, before.Memory.Count).Select(i => before.Memory[i]).Single(value => value.Kind == ActivationNodeKind.PassageSource);
        Assert.Equal(1UL, passed.PassageSequence.Value);
        Assert.Equal(1u, before.Memory[0].Count);
        Assert.Equal(ActivationTimerPhase.Counting, before.Timers[0].Phase);
        Assert.True(before.Timers[0].DueTick > before.Tick.Value);
        Assert.Equal(OscillatorPhase.Running, before.Memory[0].Phase);
        Assert.True(before.Memory[0].DueTick > before.Tick.Value);
        var credit = Enumerable.Range(0, before.EnergyStores.Count).Select(i => before.EnergyStores[i]).Single(value => value.Id == storage);
        Assert.True(credit.Supplied.Value > 0f);
        Assert.True(before.Electrical[0].Debit.Value > 0f);
        Assert.True(before.Motion!.Count > 0);
        Assert.True(before.Motion.TrySample(traveller, new(50, .5f), out var priorPose));
        var idle = await device.ApplyFiniteWork(before, new(15), new(new(1), release, payload));
        device.Commit(new(15));
        var after = idle.Read;
        Assert.Equal(FiniteWorkRequestResult.Fired, after.FiniteWorkActuators[0].LastResult);
        Assert.Equal(before.Tick, after.Tick);
        Assert.True(after.Captures.TryGet(new(capture, traveller), out var retainedCapture));
        Assert.Equal(captured, retainedCapture);
        Assert.Equal(passed, Enumerable.Range(0, after.Memory.Count).Select(i => after.Memory[i]).Single(value => value.Kind == ActivationNodeKind.PassageSource));
        Assert.Equal(before.Memory[0], after.Memory[0]);
        Assert.Equal(before.Timers[0], after.Timers[0]);
        Assert.Equal(before.Electrical[0], after.Electrical[0]);
        Assert.Equal(credit with { CommitSupplied = default, CommitReleased = default, CommitDissipated = default, Transactions = 0 },
            Enumerable.Range(0, after.EnergyStores.Count).Select(i => after.EnergyStores[i]).Single(value => value.Id == storage));
        Assert.True(before.Bodies.TryGet(traveller, out var original));
        Assert.True(after.Bodies.TryGet(traveller, out var preserved));
        Assert.Equal(original, preserved);
        Assert.Equal(before.Motion.Count, after.Motion!.Count);
        Assert.Equal(before.Motion.FirstOrdinal, after.Motion.FirstOrdinal);
        Assert.Equal(before.Motion.LastOrdinal, after.Motion.LastOrdinal);
        Assert.True(after.Motion.TrySample(traveller, new(50, .5f), out var retainedPose));
        Assert.Equal(priorPose, retainedPose);
        var next = await device.Advance(after, new(16), new([ElectricalEnable.Enabled]));
        device.Commit(new(16));
        Assert.Equal(14UL, next.Read.Tick.Value);
        Assert.True(Enumerable.Range(0, next.Read.EnergyStores.Count).Select(i => next.Read.EnergyStores[i]).Single(value => value.Id == storage).Supplied.Value > credit.Supplied.Value);
        Assert.True(next.Read.Bodies.TryGet(traveller, out var moved));
        Assert.True(moved.Body.Local.X > preserved.Body.Local.X);
    }

}
