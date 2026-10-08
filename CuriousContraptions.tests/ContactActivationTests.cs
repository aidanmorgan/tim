using System.Buffers.Binary;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class ContactActivationTests
{
    private static WorkshopConstruction Construction() => new(new(1), WorkshopCadenceSettings.Default(),
        new(WorkshopInput.Basketball(new(1), 0, 3, 0, 0, 0, 0, 1),
            new WorkshopSwitch(new(2), new(0, 16, 0), default, CanonicalRotation.Identity, ContactTriggerSettings.Default),
            new WorkshopLamp(new(3), new(48, 16, 0), default, CanonicalRotation.Identity)));

    private static PhysicsActivationRead Consume(ActivationNetwork network, PhysicsActivationRead prior, ContactTriggerRead[] events)
    {
        var tick = events.Length == 0 ? 1UL : events.Max(e => new ActivationTime(e.EventOrdinal,e.EventPhase).CeilingTick(4));
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
        Assert.Equal(new MetreVector((Half)0, (Half)(-.15), (Half)0), boxes[0].Pose.Translation);
        Assert.Equal(new MetreVector((Half).55, (Half).125, (Half).5), boxes[0].HalfExtents);
        Assert.Equal(new MetreVector((Half)0, (Half).05, (Half)0), boxes[1].Pose.Translation);
        Assert.Equal(new MetreVector((Half).4, (Half).09, (Half).375), boxes[1].HalfExtents);
        var material = scene.Materials.ToArray().Single(m => m.Id == boxes[0].Material);
        Assert.Equal(new Restitution((Half)1), material.Restitution);
        Assert.Equal(new LinearSpeed((Half).1), material.BounceThreshold);
        Assert.Equal(new FrictionCoefficient((Half).3), material.Friction);
        var trigger = Assert.Single(scene.Triggers.ToArray());
        Assert.Equal(new GpuBodyId(2), trigger.Owner);
        Assert.Equal(new BodyTargetSet(BodyTargetKind.AllDynamic, default), trigger.Targets);
        Assert.True(trigger.Targets.Contains(new(1)));
        Assert.Equal(new LinearSpeed((Half).8), trigger.Threshold);
        Assert.Single(scene.Colliders.ToArray(), c => c.Body == new GpuBodyId(3));
    }

    [Fact]
    public void TriggerHeaderDeclarationsPaddingAndOldStateVersionReject()
    {
        var scene = WorkshopPhysicsCompiler.Compile(Construction(), new(1, 2));
        var bytes = PhysicsGpuAbi.Admission(scene, new(1), new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        PhysicsGpuAbi.ValidateCandidate(bytes, bytes, new(0));
        Assert.Equal(0u, PhysicsGpuAbi.ReadTrigger(bytes, 0).OccurrenceCount);
        foreach (var offset in new[] { 100, PhysicsGpuAbi.TriggersOffset, PhysicsGpuAbi.TriggersOffset + 16,
            PhysicsGpuAbi.TriggersOffset + 18, PhysicsGpuAbi.TriggersOffset + 36,
            PhysicsGpuAbi.TriggersOffset + 48, PhysicsGpuAbi.TriggersOffset + PhysicsGpuAbi.TriggerBytes })
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
            trigger with { Threshold = new(Half.NaN) }, trigger with { Threshold = new((Half)(-1)) }
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
            1, collider.Id, 10, (Half)0, new((Half).8)));
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
        var bounced = Consume(network, candidate, new[] { impact with { EventOrdinal = 20, ApproachSpeed = new((Half)2) } });
        Assert.Equal(candidate[0], bounced[0]); Assert.Equal(candidate[1], bounced[1]);
        var reset = network.Clear();
        Assert.Equal(clear[0], reset[0]); Assert.Equal(clear[1], reset[1]);
    }

    [Fact]
    public void DisconnectedAndAbsentContactCannotActivateLamp()
    {
        var (network, impact) = Activation(false); var clear = network.Clear();
        var absent = impact with { OccurrenceCount = 0, Collider = default, EventOrdinal = 0,
            EventPhase = (Half)0, ApproachSpeed = new((Half)0) };
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
            active[0] with { Body = new(99) }, active[0] with { ApproachSpeed = new((Half).5) }
        }) Assert.Throws<ArgumentException>(() => ValidateRead(network, new(new[] { changed, active[1] }), new(3), 4, scene));
        var disconnected = Activation(false).Network;
        Assert.Throws<ArgumentException>(() => ValidateRead(disconnected, active, new(3), 4, scene));
        Assert.Throws<ArgumentException>(() => ValidateRead(network, new(new[] { active[0], network.Clear()[1] }), new(3), 4, scene));
    }

    [Fact]
    public void ActivationReadWireRetainsIdentityAndRejectsPaddingAndUnknownPhase()
    {
        var (network, _) = Activation(); var clear = network.Clear();
        var bytes = new byte[WorkshopActivationWire.ByteLength]; WorkshopActivationWire.Write(clear, bytes);
        var decoded = WorkshopActivationWire.Read(bytes, clear.Count);
        Assert.Equal(clear[0], decoded[0]); Assert.Equal(clear[1], decoded[1]);
        foreach (var offset in new[] { 40, 52, clear.Count * WorkshopActivationWire.RecordBytes })
        {
            var changed = (byte[])bytes.Clone(); changed[offset] = 255;
            Assert.Throws<ArgumentException>(() => WorkshopActivationWire.Read(changed, clear.Count));
        }
    }
}
