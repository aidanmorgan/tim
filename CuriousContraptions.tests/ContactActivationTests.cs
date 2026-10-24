using System.Buffers.Binary;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class ContactActivationTests
{
    private static WorkshopConstruction Construction() => new(new(1), WorkshopCadenceSettings.Default(),
        new(WorkshopInput.Generic(PartId.Basketball,new(1), 0, 3, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(PartId.ImpactSwitch, new(2), 0, 1, 0, 0, 0, 0, 1),
            PlacedParts.Lamp(new(3), 3, 1, 0, 0, 0, 0, 1)));

    private static PhysicsActivationRead Consume(ActivationNetwork network, PhysicsActivationRead prior, ContactTriggerRead[] events)
    {
        var tick = events.Length == 0 ? 1UL : events.Max(e => new ActivationTime(e.EventOrdinal,(float)e.EventPhase).CeilingTick(4));
        return network.Consume(prior, network.ClearTimers(), events, new(tick), []).Activations;
    }
    private static void ValidateRead(ActivationNetwork network, PhysicsActivationRead read, SimulationTick tick,
        uint substeps, PhysicsSceneDeclaration scene) => network.ValidateRead(read, network.ClearTimers(), tick, substeps, scene);

    [Fact]
    public void CompilerPreservesBothSwitchBoxesAndInheritedStaticMaterials()
    {
        var construction = Construction();
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        var boxes = scene.Colliders.ToArray().Where(c => c.Body == new GpuBodyId(2)).ToArray();
        Assert.Equal(2, boxes.Length);
        Assert.All(boxes, b => Assert.Equal(ColliderShapeKind.Box, b.Shape));
        Assert.Equal(new MetreVector(0f, (float)(-.15), 0f), boxes[0].Pose.Translation);
        Assert.Equal(new MetreVector(.55f, .125f, .5f), boxes[0].HalfExtents);
        Assert.Equal(new MetreVector(0f, .05f, 0f), boxes[1].Pose.Translation);
        Assert.Equal(new MetreVector(.4f, .09f, .375f), boxes[1].HalfExtents);
        var material = scene.Materials.ToArray().Single(m => m.Id == boxes[0].Material);
        Assert.Equal(new Restitution(1f), material.Restitution);
        Assert.Equal(new LinearSpeed(.1f), material.BounceThreshold);
        Assert.Equal(new FrictionCoefficient(.3f), material.Friction);
        var trigger = Assert.Single(scene.Triggers.ToArray());
        Assert.Equal(new GpuBodyId(2), trigger.Owner);
        Assert.Equal(new BodyTargetSet(BodyTargetKind.AllDynamic, default), trigger.Targets);
        Assert.True(trigger.Targets.Contains(new(1)));
        Assert.Equal(new ContactSpeed(.8f), trigger.Threshold);
        Assert.Single(scene.Colliders.ToArray(), c => c.Body == new GpuBodyId(3));
    }

    [Fact]
    public void TriggerHeaderDeclarationsPaddingAndOldStateVersionReject()
    {
        var scene = WorkshopPhysicsCompiler.Compile(Construction(), new(1, 2));
        var bytes = PhysicsGpuAbi.Admission(scene, new(1), new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        PhysicsGpuAbi.ValidateCandidate(bytes, bytes, new(0));
        Assert.Equal(0u, PhysicsGpuAbi.ReadTrigger(bytes, 0).OccurrenceCount);
        // Story 9R.5b: the trigger table is exactly its population, so there is no unused trigger slot to corrupt.
        var trigger = scene.World.TriggerOffset(0);
        foreach (var offset in new[] { PhysicsGpuAbi.HeaderTriggerCountOffset, trigger, trigger + 16,
            trigger + 18, trigger + 36, trigger + 48 })
        {
            var changed = (byte[])bytes.Clone(); changed[offset] ^= 128;
            Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(changed, bytes, new(0)));
        }
        var old = (byte[])bytes.Clone(); BinaryPrimitives.WriteUInt32LittleEndian(old, 4);
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadFailure(old));
    }

    [Fact]
    public void TriggerRequiresActualStaticOwnerDynamicTargetAndUniqueOwnership()
    {
        var scene = WorkshopPhysicsCompiler.Compile(Construction(), new(1, 2));
        var trigger = scene.Triggers[0];
        foreach (var invalid in new[]
        {
            trigger with { Owner = new(1), Targets = new(BodyTargetKind.NamedBody, trigger.Owner) },
            trigger with { Owner = new(99) }, trigger with { Targets = new(BodyTargetKind.NamedBody, trigger.Owner) },
            trigger with { Threshold = new(float.NaN) }, trigger with { Threshold = new((float)(-1)) }
        })
            Assert.Throws<ArgumentException>(() => new PhysicsSceneDeclaration(scene.Document, scene.NextIdentity,
                scene.Bodies, scene.Colliders, scene.Materials, scene.Sensors, scene.Guides, new[] { invalid }));
        Assert.Throws<ArgumentException>(() => new PhysicsSceneDeclaration(scene.Document, scene.NextIdentity,
            scene.Bodies, scene.Colliders, scene.Materials, scene.Sensors, scene.Guides, new[] { trigger, trigger }));
    }

    private static (ActivationNetwork Network, ContactTriggerRead Event) Activation(bool connected = true)
    {
        var construction = Construction();
        if (connected) construction = construction with { Connections = new(new WorkshopConnection(new(2),
            WorkshopSocket.ActivationOut, new(3), WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation)) };
        var source = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        var collider = source.Colliders.ToArray().First(c => c.Body == new GpuBodyId(2));
        return (WorkshopActivationCompiler.Compile(construction), new(source.Triggers[0].Id, new(2), new(1),
            1, collider.Id, 10, 0f, new(.8f)));
    }

    [Fact]
    public void CommittedContactLatchesSourceAndDeclaredTargetOnceWithoutMutatingPriorCheckpoint()
    {
        var (network, impact) = Activation(); var clear = network.Clear();
        var candidate = Consume(network, clear, new[] { impact });
        Assert.Equal(ActivationPhase.Clear, clear[0].Phase);
        Assert.Equal(ActivationPhase.Clear, clear[1].Phase);
        Assert.Equal(ActivationPhase.Latched, candidate[0].Phase);
        Assert.Equal(ActivationPhase.Latched, candidate[1].Phase);
        Assert.Equal(impact.Collider, candidate[1].Collider);
        Assert.Equal(impact.Id.Value, candidate[1].Source.Value);
        var replay = Consume(network, candidate, new[] { impact });
        Assert.Equal(candidate[0], replay[0]); Assert.Equal(candidate[1], replay[1]);
        var bounced = Consume(network, candidate, new[] { impact with { EventOrdinal = 20, ApproachSpeed = new(2f) } });
        Assert.Equal(candidate[0], bounced[0]); Assert.Equal(candidate[1], bounced[1]);
        var reset = network.Clear();
        Assert.Equal(clear[0], reset[0]); Assert.Equal(clear[1], reset[1]);
    }

    [Fact]
    public void DisconnectedAndAbsentContactCannotActivateLamp()
    {
        var (network, impact) = Activation(false); var clear = network.Clear();
        var absent = impact with { OccurrenceCount = 0, Collider = default, EventOrdinal = 0,
            EventPhase = 0f, ApproachSpeed = new(0f) };
        Assert.Equal(ActivationPhase.Clear, Consume(network, clear, new[] { absent })[0].Phase);
        var candidate = Consume(network, clear, new[] { impact });
        Assert.Equal(ActivationPhase.Latched, candidate[0].Phase);
        Assert.Equal(ActivationPhase.Clear, candidate[1].Phase);
    }

    [Fact]
    public void InvalidOrStaleOccurrenceDiscardsWholeLogicalCandidate()
    {
        var (network, impact) = Activation(); var clear = network.Clear();
        Assert.Throws<ArgumentException>(() => Consume(network, clear, new[] { impact, impact }));
        Assert.Throws<ArgumentException>(() => Consume(network, clear, new[] { impact with { Owner = new(99) } }));
        Assert.Throws<ArgumentException>(() => Consume(network, clear, new[] { impact with { Id = new(99) } }));
        Assert.Equal(ActivationPhase.Clear, clear[0].Phase); Assert.Equal(ActivationPhase.Clear, clear[1].Phase);
        var committed = Consume(network, clear, new[] { impact });
        Assert.Throws<ArgumentException>(() => Consume(network, committed, new[] { impact with { EventOrdinal = 9 } }));
        Assert.Throws<ArgumentException>(() => Consume(network, committed, new[] { impact with { Collider = new(99) } }));
        Assert.Equal(impact.Collider, committed[0].Collider);
    }

    [Fact]
    public void ReadContractRejectsWrongIdentityRouteInitialAndFutureActivation()
    {
        var construction = Construction() with { Connections = new(new WorkshopConnection(new(2), WorkshopSocket.ActivationOut,
            new(3), WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation)) };
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        var (network, impact) = Activation();
        var active = Consume(network, network.Clear(), new[] { impact });
        ValidateRead(network, active, new(3), 4, scene);
        ValidateRead(network, network.Clear(), new(0), 4, scene);
        Assert.Throws<ArgumentException>(() => ValidateRead(network, active, new(0), 4, scene));
        Assert.Throws<ArgumentException>(() => ValidateRead(network, active, new(2), 4, scene));
        foreach (var changed in new[]
        {
            active[0] with { Node = new(1) }, active[0] with { Owner = new(99) },
            active[0] with { Collider = new(99) }, active[0] with { Source = new(99) },
            active[0] with { Body = new(99) }, active[0] with { ApproachSpeed = new(.5f) }
        }) Assert.Throws<ArgumentException>(() => ValidateRead(network, new(new[] { changed, active[1] }), new(3), 4, scene));
        var disconnected = Activation(false).Network;
        Assert.Throws<ArgumentException>(() => ValidateRead(disconnected, active, new(3), 4, scene));
        Assert.Throws<ArgumentException>(() => ValidateRead(network, new(new[] { active[0], network.Clear()[1] }), new(3), 4, scene));
    }


    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    public async Task RealImpactAndMissDisconnectedControlsRestoreExactly(bool connected, bool miss, bool lampOn)
    {
        var construction = Construction() with { Revision = new(2), Instances = new(
            WorkshopInput.Generic(PartId.Basketball,new(1), miss ? 2 : 0, 3, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(PartId.ImpactSwitch, new(2), 0, 1, 0, 0, 0, 0, 1,
                new([new(new ParameterId("trigger_threshold"), 1.234567f)])),
            PlacedParts.Lamp(new(3), 3, 1, 0, 0, 0, 0, 1)) };
        if (connected) construction = construction with { Connections = new(new WorkshopConnection(new(2),
            WorkshopSocket.ActivationOut, new(3), WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation)) };
        var saved = new WorkshopSavedConstruction(construction, new(4));
        var bytes = WorkshopSaveCodec.Encode(saved);
        var loaded = WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes);
        Assert.Equal(saved, loaded);
        Assert.Equal(bytes, WorkshopSaveCodec.Encode(loaded));
        var (built, reads, reset) = await NoFixedCapacityTests.RunAndReset(loaded.Construction, 120);
        Assert.Equal(lampOn, reads.Any(read => read.Activations[1].Phase == ActivationPhase.Latched));
        Assert.Equal(!miss, reads.Any(read => read.Activations[0].Phase == ActivationPhase.Latched));
        var latched = reads.Where(read => read.Activations[0].Phase == ActivationPhase.Latched).ToArray();
        if (latched.Length > 0) Assert.All(latched, read => Assert.Equal(latched[0].Activations[0], read.Activations[0]));
        Assert.Equal(built.Activations[0], reset.Activations[0]);
        Assert.Equal(built.Activations[1], reset.Activations[1]);
    }

    [Fact]
    public void CurrentLayoutIdentityRejectsOldOrForeignStatesBeforeWorkerMutation()
    {
        var scene = WorkshopPhysicsCompiler.Compile(Construction(), new(1, 2));
        var bytes = PhysicsGpuAbi.Admission(scene, new(1), new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        foreach (var tag in new[] { 0u, PhysicsGpuAbi.CurrentLayoutTag ^ 1u })
        {
            var old = (byte[])bytes.Clone();
            BinaryPrimitives.WriteUInt32LittleEndian(old.AsSpan(PhysicsGpuAbi.HeaderLayoutTagOffset), tag);
            var candidate = Enumerable.Repeat((byte)123, old.Length).ToArray();
            var untouched = (byte[])candidate.Clone();
            Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadTrigger(old, 0));
            Assert.Throws<ArgumentException>(() => new CuriousContraptions.Simulation.WorkshopPhysicsEngine().Advance(old, candidate, new([], [])));
            Assert.Equal(untouched, candidate);
            Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(old, bytes, new(0)));
        }
        foreach (var padding in new[] { PhysicsGpuAbi.TriggerThresholdPaddingOffset, PhysicsGpuAbi.TriggerNamedPaddingOffset, PhysicsGpuAbi.TriggerPaddingOffset })
        {
            var invalid = (byte[])bytes.Clone(); invalid[scene.World.TriggerOffset(0) + padding] = 1;
            Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadTrigger(invalid, 0));
        }
    }

    [Fact]
    public void ActualBaseBoxContactUsesInclusiveNonHalfThresholdAndExactReportedSpeed()
    {
        const float threshold = 1.234567f;
        Assert.True(TestPrecision.NeedsMoreThanElevenSignificantBits(threshold));
        foreach (var speed in new[] { MathF.BitDecrement(threshold), threshold, MathF.BitIncrement(threshold) })
        {
            var trigger = WorkshopInput.Generic(PartId.ImpactSwitch, new(2), 0, 1, 0, 0, 0, 0, 1,
                new([new(new ParameterId("trigger_threshold"), threshold)]));
            var construction = Construction() with { Instances = new(
                WorkshopInput.Generic(PartId.Basketball,new(1), -.88, .85, 0, 0, 0, 0, 1), trigger,
                PlacedParts.Lamp(new(3), 3, 1, 0, 0, 0, 0, 1)),
                Connections = new(new WorkshopConnection(new(2), WorkshopSocket.ActivationOut, new(3), WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation)) };
            var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
            Assert.Equal(threshold, Assert.Single(scene.Triggers.ToArray()).Threshold.Value);
            var bytes = PhysicsGpuAbi.Admission(scene, new(1), new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
            var body = scene.World.BodyOffset(scene.Layout.GetBodySlot(new(1)));
            BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(body + PhysicsGpuAbi.BodyVelocityOffset), speed);
            bytes.AsSpan(body + PhysicsGpuAbi.BodyGravityOffset, 6).Clear();
            bytes.AsSpan(body + PhysicsGpuAbi.BodyDragOffset, 2).Clear();
            ContactTriggerRead observed = default;
            WorkshopPhysicsEngineTests.StepWorld(bytes, 1, (_, world) => observed = PhysicsGpuAbi.ReadTrigger(world, 0));
            Assert.Equal(speed >= threshold ? 1u : 0u, observed.OccurrenceCount);
            if (observed.OccurrenceCount == 0) continue;
            Assert.Equal(speed, observed.ApproachSpeed.Value);
            Assert.Equal(scene.Colliders.ToArray().First(row => row.Body == new GpuBodyId(2)).Id, observed.Collider);
            var network = WorkshopActivationCompiler.Compile(construction);
            var active = Consume(network, network.Clear(), [observed]);
            Assert.Equal(ActivationPhase.Latched, active[1].Phase);
            Assert.Equal(speed, active[1].ApproachSpeed.Value);
            ValidateRead(network, active, new(1), 4, scene);
        }
    }
}
