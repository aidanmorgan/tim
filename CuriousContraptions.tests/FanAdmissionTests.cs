using CuriousContraptions.Abstractions;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class FanAdmissionTests
{

    [Fact]
    public async Task ForeignPackageStoreIsRefusedByTheInstalledRouterWithoutSpendingEitherBalance()
    {
        var other = FanIntegrationTests.Fan() with { Id = new(7),
            Cell = new(0, WorkshopInput.Position(8).Cell, 0) };
        var construction = TennisBallTests.Scene(FanIntegrationTests.Fan(), other,
            TennisBallTests.Ball(id: 2, x: 2, y: 5));
        await using var h = await WorkshopIntegrationHarness.Building(construction);
        var command = h.Command(WorkshopCommandKind.Construct,
            construction with { Revision = new(construction.Revision.Value + 1) });
        var bytes = WorkshopWireFlatBuffers.EncodeCommand(command);
        var stores = command.Scene!.Physics.EnergyStores;
        Assert.Equal(2, stores.Length);
        var buffer = new Google.FlatBuffers.ByteBuffer(bytes);
        var envelope = new Google.FlatBuffers.Table(4 + buffer.GetInt(4), buffer);
        var scene = new Google.FlatBuffers.Table(envelope.__indirect(envelope.bb_pos + envelope.__offset(26)), buffer);
        var vector = scene.__vector(scene.__offset(4 + 45 * 2));
        var source = new Google.FlatBuffers.Table(scene.__indirect(vector), buffer);
        var storeOffset = source.__offset(4 + 13 * 2);
        Assert.NotEqual(0, storeOffset);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(
            bytes.AsSpan(source.bb_pos + storeOffset), stores[1].Id.Value);
        var before = h.Committed;
        var fault = Assert.Throws<WireFaultException>(() => h.Router.Dispatch(bytes));
        Assert.Equal(WireFaultReason.OutOfRange, fault.Reason);
        Assert.Equal(construction, h.Construction);
        Assert.Equal(before.Revision, h.Committed.Revision);
        Assert.True(before.EnergyStores.HasSameBits(h.Committed.EnergyStores));
        var accepted = await h.Dispatch(command);
        Assert.Equal(WorkshopCommandOutcome.Applied, accepted.Result.Outcome);
        await h.Run(); await h.Advance(4);
        Assert.True(h.Committed.Airflow[0].Work.Value > 0);
    }

    public enum MissingWireField { SourceAxis, InternalOccluders, ReceiverSamples, SceneSources, SceneReceivers, PresentationSource }

    [Theory]
    [InlineData(MissingWireField.SourceAxis)]
    [InlineData(MissingWireField.InternalOccluders)]
    [InlineData(MissingWireField.ReceiverSamples)]
    [InlineData(MissingWireField.SceneSources)]
    [InlineData(MissingWireField.SceneReceivers)]
    [InlineData(MissingWireField.PresentationSource)]
    public async Task MissingRequiredAirflowWireFieldsRefuseWithoutChangingTheInstallation(MissingWireField missing)
    {
        var construction = TennisBallTests.Scene(FanIntegrationTests.Fan(), TennisBallTests.Ball(id: 2, x: 2, y: 5));
        await using var h = await WorkshopIntegrationHarness.Building(construction);
        var command = h.Command(WorkshopCommandKind.Construct, construction with { Revision = new(3) });
        var bytes = WorkshopWireFlatBuffers.EncodeCommand(command);
        var decoded = WorkshopWireFlatBuffers.DecodeCommand(bytes);
        Assert.Single(decoded.Scene!.Physics.AirflowSources.ToArray());
        Assert.Single(decoded.Scene.Physics.AirflowReceivers.ToArray());
        var buffer = new Google.FlatBuffers.ByteBuffer(bytes);
        var envelope = new Google.FlatBuffers.Table(4 + buffer.GetInt(4), buffer);
        var scene = new Google.FlatBuffers.Table(envelope.__indirect(envelope.bb_pos + envelope.__offset(26)), buffer);
        Google.FlatBuffers.Table First(int field)
        {
            var vector = scene.__vector(scene.__offset(4 + field * 2));
            return new(scene.__indirect(vector), buffer);
        }
        var table = missing switch
        {
            MissingWireField.SourceAxis or MissingWireField.InternalOccluders => First(45),
            MissingWireField.ReceiverSamples => First(46),
            MissingWireField.PresentationSource => First(29),
            _ => scene
        };
        var field = missing switch
        {
            MissingWireField.SourceAxis => 4,
            MissingWireField.InternalOccluders => 11,
            MissingWireField.ReceiverSamples => 4,
            MissingWireField.SceneSources => 45,
            MissingWireField.SceneReceivers => 46,
            MissingWireField.PresentationSource => 16,
            _ => throw new ArgumentOutOfRangeException(nameof(missing))
        };
        Assert.NotEqual(0, table.__offset(4 + field * 2));
        var vtable = table.bb_pos - buffer.GetInt(table.bb_pos);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(vtable + 4 + field * 2), 0);
        var before = h.Committed;
        var fault = Assert.Throws<WireFaultException>(() => h.Router.Dispatch(bytes));
        Assert.Contains(fault.Reason, new[] { WireFaultReason.MissingField, WireFaultReason.VerifierRejected });
        Assert.NotEmpty(fault.Message);
        Assert.True(before.EnergyStores.HasSameBits(h.Committed.EnergyStores));
        Assert.Equal(before.Revision, h.Committed.Revision);
        Assert.Equal(construction, h.Construction);
        var accepted = await h.Dispatch(command);
        Assert.Equal(WorkshopCommandOutcome.Applied, accepted.Result.Outcome);
        await h.Run();
        await h.Advance(4);
        Assert.True(h.Committed.Airflow[0].Work.Value > 0);
    }



    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnfamiliarReorderedSourcesShareTheirExactStoreThroughCompilerWireAndRuntime(bool reverse)
    {
        var entry = TestBundle.Entry(new("fan"));
        var part = new PartId("unfamiliar_dual_cone");
        var prototype = entry.Mechanical!.AirflowSources[0];
        var input = new StoredAirflowInput(null, new(1)) { Enable = null };
        var first = prototype with { Id = new(17), Origin = new(0, 0, -1),
            FlowSpeed = CuriousContraptions.Abstractions.Mechanical.MechanicalValue.Fixed(4),
            MaximumForce = CuriousContraptions.Abstractions.Mechanical.MechanicalValue.Fixed(4),
            MaximumPower = CuriousContraptions.Abstractions.Mechanical.MechanicalValue.Fixed(16), Inputs = [input] };
        var second = first with { Id = new(59), Frame = new(1), Origin = new(0, 0, 1),
            FlowSpeed = CuriousContraptions.Abstractions.Mechanical.MechanicalValue.Fixed(8),
            MaximumForce = CuriousContraptions.Abstractions.Mechanical.MechanicalValue.Fixed(8),
            MaximumPower = CuriousContraptions.Abstractions.Mechanical.MechanicalValue.Fixed(64) };
        var store = entry.Mechanical.EnergyStores[0] with { Selection = null,
            Capacity = EnergyExpression.Direct(CuriousContraptions.Abstractions.Mechanical.MechanicalValue.Fixed(.1f)),
            InitialEnergy = EnergyExpression.Direct(CuriousContraptions.Abstractions.Mechanical.MechanicalValue.Fixed(.1f)) };
        entry = entry with { Metadata = entry.Metadata with { Id = part.Value }, Domains = [SimulationDomain.Mechanical], Ports = [], Electrical = null, Signal = null, Events = [],
            Mechanical = entry.Mechanical with { AirflowSources = reverse ? [second, first] : [first, second], EnergyStores = [store] },
            Presentation = entry.Presentation with { Bindings = [] } };
        Assert.Empty(BundleValidator.Validate([new("dual cone", entry)]));
        await using var h = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        using var physicsScope = DeclaredPhysics.Scoped([entry]);
        using var portsScope = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(part, entry.Ports)]));
        var source = FanIntegrationTests.Fan() with { PartId = part, Id = new(91) };
        var a = TennisBallTests.Ball(id: 23, x: 2, y: 5, drag: 0) with { Cell = new(WorkshopInput.Position(2).Cell, WorkshopInput.Position(5).Cell, WorkshopInput.Position(-1).Cell) };
        var b = TennisBallTests.Ball(id: 77, x: 2, y: 5, drag: 0) with { Cell = new(WorkshopInput.Position(2).Cell, WorkshopInput.Position(5).Cell, WorkshopInput.Position(1).Cell) };
        var construction = TennisBallTests.Scene(source, a, b);
        await h.Require(WorkshopCommandKind.Construct, construction);
        await h.Run();
        await h.Advance(1);
        var reads = Enumerable.Range(0, h.Committed.Airflow.Count).Select(i => h.Committed.Airflow[i]).ToArray();
        var compiled = h.Simulation.Scene.Physics.AirflowSources.ToArray();
        var slow = reads.Single(read => compiled.Single(row => row.Id == read.Id).FlowSpeed == 4);
        var fast = reads.Single(read => compiled.Single(row => row.Id == read.Id).FlowSpeed == 8);
        Assert.Equal(source.Id, slow.Owner); Assert.Equal(source.Id, fast.Owner); Assert.NotEqual(slow.Id, fast.Id);
        // First-window demands 1:4 share 0.1 J. Different speeds convert their exact joule grants to impulses.
        Assert.InRange(slow.Work.Value, .01999f, .02001f);
        Assert.InRange(fast.Work.Value, .07999f, .08001f);
        Assert.InRange(h.Committed.EnergyStores[0].Energy.Value, 0, .00001f);
        Assert.True(h.Committed.Bodies.TryGet(a.Id, out var readA));
        Assert.True(h.Committed.Bodies.TryGet(b.Id, out var readB));
        Assert.InRange(readA.Body.Velocity.X, .02f / (4 * .35f) - .00001f, .02f / (4 * .35f) + .00001f);
        Assert.InRange(readB.Body.Velocity.X, .08f / (8 * .35f) - .00001f, .08f / (8 * .35f) + .00001f);
    }



    public enum ConsumerDamage { Missing, Duplicate, ForeignOwner, WrongKind }

    [Theory]
    [InlineData(ConsumerDamage.Missing)]
    [InlineData(ConsumerDamage.Duplicate)]
    [InlineData(ConsumerDamage.ForeignOwner)]
    [InlineData(ConsumerDamage.WrongKind)]
    public void ResolvedContinuousConsumersRejectInvalidReverseReferences(ConsumerDamage damage)
    {
        var scene = CompiledSceneFixture.Compile(TennisBallTests.Scene(
            FanIntegrationTests.Fan(FanIntegrationTests.Supply.Electrical)));
        var physics = scene.Physics;
        var nodes = physics.SupplyGraph.Nodes.ToArray();
        var index = Array.FindIndex(nodes, node => !node.ContinuousWork.IsDefaultOrEmpty);
        var node = nodes[index];
        nodes[index] = damage switch
        {
            ConsumerDamage.Missing => node with { ContinuousWork = [new(physics.NextIdentity + 10)] },
            ConsumerDamage.Duplicate => node with { ContinuousWork = [node.ContinuousWork[0], node.ContinuousWork[0]] },
            ConsumerDamage.ForeignOwner => node with { Owner = physics.Revolutes[0].BodyB },
            ConsumerDamage.WrongKind => node with { Kind = SupplyNodeKind.Contact },
            _ => throw new ArgumentOutOfRangeException(nameof(damage))
        };
        Assert.Throws<ArgumentException>(() => new PhysicsSceneDeclaration(physics.Document, physics.NextIdentity,
            physics.Bodies, physics.Colliders, physics.Materials, physics.Sensors, physics.Guides,
            supplyNodes: nodes, supplyEdges: physics.SupplyGraph.Edges.ToArray(),
            revolutes: physics.Revolutes, airflowSources: physics.AirflowSources, airflowReceivers: physics.AirflowReceivers));
        Assert.Single(physics.SupplyGraph.Nodes.ToArray().Single(n => n.Id == node.Id).ContinuousWork);
    }


    [Theory]
    [InlineData(FanIntegrationTests.Supply.SelfContained)]
    [InlineData(FanIntegrationTests.Supply.Shaft)]
    public async Task InactiveConnectionAndRetainedWireModeEditPreserveInstallation(FanIntegrationTests.Supply mode)
    {
        var battery = PlacedParts.Battery(new(3), -3, 5, 0);
        var fan = FanIntegrationTests.Fan(FanIntegrationTests.Supply.Electrical);
        var connection = new WorkshopConnection(battery.Id, PortId.Supply, fan.Id, PortId.PowerIn, WorkshopConnectionDomain.Electrical);
        var construction = TennisBallTests.Scene(fan, battery) with { Connections = new(connection) };
        await using var h = await WorkshopIntegrationHarness.Initialized(construction.Settings);
        await h.Require(WorkshopCommandKind.Construct, construction);
        var before = h.Committed;
        var selected = construction with { Revision = new(construction.Revision.Value + 1),
            Instances = construction.Instances.With(FanIntegrationTests.Fan(mode)) };
        EditRefusal.Is(WorkshopEditRejection.InactivePort, () => h.Command(WorkshopCommandKind.Construct, selected));
        Assert.Equal(before.Revision, h.Committed.Revision);
        Assert.Equal(construction, h.Construction);
        var disconnected = selected with { Connections = new() };
        await h.Require(WorkshopCommandKind.Construct, disconnected);
        Assert.Equal((float)mode, Assert.IsType<WorkshopGenericInstance>(h.Construction.Instances[0]).Parameters[new("supply")]);
        EditRefusal.Is(WorkshopEditRejection.InactivePort, () => h.Command(WorkshopCommandKind.Construct, selected));
        Assert.Equal(disconnected, h.Construction);
    }

    [Theory]
    [InlineData(FanIntegrationTests.Supply.SelfContained)]
    [InlineData(FanIntegrationTests.Supply.Electrical)]
    [InlineData(FanIntegrationTests.Supply.Shaft)]
    public async Task SaveLoadPreservesSelectionAndRecreatesAuthoredBalances(FanIntegrationTests.Supply mode)
    {
        var fan = FanIntegrationTests.Fan(mode);
        var battery = PlacedParts.Battery(new(3), -6, 5, 0);
        var parts = new List<IWorkshopInstance> { fan };
        var links = new List<WorkshopConnection>();
        if (mode != FanIntegrationTests.Supply.SelfContained) parts.Add(battery);
        if (mode == FanIntegrationTests.Supply.Electrical)
            links.Add(new(battery.Id, PortId.Supply, fan.Id, PortId.PowerIn, WorkshopConnectionDomain.Electrical));
        if (mode == FanIntegrationTests.Supply.Shaft)
        {
            var motor = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("motor"), new(4), -3, 5, 0, 0, 0, 0, 1);
            parts.Add(motor);
            links.Add(new(battery.Id, PortId.Supply, motor.Id, PortId.PowerIn, WorkshopConnectionDomain.Electrical));
            links.Add(new(motor.Id, PortId.Drive, fan.Id, PortId.DriveIn, WorkshopConnectionDomain.Mechanical));
        }
        var construction = TennisBallTests.Scene(parts.ToArray()) with { Connections = new(links.ToArray()) };
        var saved = new WorkshopSavedConstruction(construction, new(5));
        var decoded = WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply,
            RuntimeTestServices.Bundle, SaveFixture.Elements,
            WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, saved));
        Assert.Equal(construction.Connections, decoded.Construction.Connections);
        var restored = Assert.IsType<WorkshopGenericInstance>(decoded.Construction.Instances.Single(instance => instance.Id == fan.Id));
        Assert.Equal(fan.PartId, restored.PartId);
        foreach (var parameter in fan.Parameters) Assert.Equal(parameter.Value, restored.Parameters[parameter.Key]);
        await using var h = await WorkshopIntegrationHarness.Running(decoded.Construction);
        await h.Advance(2);
        Assert.Equal(0f, h.Committed.Airflow[0].Work.Value);
        await h.Reset();
        Assert.Equal(0f, h.Committed.Airflow[0].Work.Value);
        if (mode == FanIntegrationTests.Supply.SelfContained)
        {
            Assert.Equal(1, h.Committed.EnergyStores.Count);
            Assert.Equal(14400f, h.Committed.EnergyStores[0].Energy.Value);
        }
        else Assert.Equal(0, h.Committed.EnergyStores.Count);
    }

    [Theory]
    [InlineData("force", 0f, true)]
    [InlineData("force", 40f, true)]
    [InlineData("force", -1f, false)]
    [InlineData("force", 41f, false)]
    [InlineData("reach", 12f, true)]
    [InlineData("reach", 0f, false)]
    [InlineData("width", 4f, true)]
    [InlineData("width", 0f, false)]
    [InlineData("supply", 3f, false)]
    [InlineData("supply", .5f, false)]
    [InlineData("force", float.NaN, false)]
    [InlineData("reach", float.PositiveInfinity, false)]
    public void PackageScalarEndpointsAndClosedModeAreAdmittedAtomically(string key, float value, bool admitted)
    {
        var fan = FanIntegrationTests.Fan();
        var parameters = fan.Parameters.ToDictionary();
        parameters[new(key)] = value;
        var changed = fan with { Parameters = new(parameters) };
        if (admitted)
        {
            var source = Assert.Single(CompiledSceneFixture.Compile(TennisBallTests.Scene(changed)).Physics.AirflowSources.ToArray());
            Assert.True(float.IsFinite(source.MaximumForce) && source.Reach > 0 && source.TerminalRadius > 0);
        }
        else Assert.Throws<ArgumentException>(() => changed.Validate(RuntimeTestServices.Bundle));
        Assert.Equal(9f, fan.Parameters[new("force")]);
        Assert.Equal(5f, fan.Parameters[new("reach")]);
    }

    public enum ReceiverDamage { NoSamples, NegativeWeight, WrongTotal, NonFinitePosition, UnknownSampling, MissingCollider, DisabledCollider }

    [Theory]
    [InlineData(ReceiverDamage.NoSamples)]
    [InlineData(ReceiverDamage.NegativeWeight)]
    [InlineData(ReceiverDamage.WrongTotal)]
    [InlineData(ReceiverDamage.NonFinitePosition)]
    [InlineData(ReceiverDamage.UnknownSampling)]
    [InlineData(ReceiverDamage.MissingCollider)]
    [InlineData(ReceiverDamage.DisabledCollider)]
    public void InvalidReceiverSamplesCannotEnterTheBundle(ReceiverDamage damage)
    {
        var entry = TestBundle.Entry(new("tennis"));
        var receiver = Assert.Single(entry.Mechanical!.AirflowReceivers);
        var changed = damage switch
        {
            ReceiverDamage.NoSamples => receiver with { Samples = [] },
            ReceiverDamage.NegativeWeight => receiver with { Samples = [new(System.Numerics.Vector3.Zero, -1)] },
            ReceiverDamage.WrongTotal => receiver with { Samples = [new(System.Numerics.Vector3.Zero, .5f)] },
            ReceiverDamage.NonFinitePosition => receiver with { Samples = [new(new(float.NaN, 0, 0), 1)] },
            ReceiverDamage.UnknownSampling => receiver with { Sampling = (AirflowSampling)255 },
            ReceiverDamage.MissingCollider => receiver with { Collider = new(new(0), new(99)) },
            ReceiverDamage.DisabledCollider => receiver,
            _ => throw new ArgumentOutOfRangeException(nameof(damage))
        };
        entry = entry with { Mechanical = entry.Mechanical with { AirflowReceivers = [changed] } };
        if (damage == ReceiverDamage.DisabledCollider)
            entry = entry with { Colliders = entry.Colliders.Select(c => c with { Participation = CollisionParticipation.Disabled }).ToArray() };
        Assert.NotEmpty(BundleValidator.Validate([new("invalid receiver", entry)]));
    }

    public enum SourceDamage { Duplicate, MissingFrame, DuplicateExclusion, ForeignExclusion, MissingInput, UnknownConsumer, BadSense, ConflictingWriter }

    [Theory]
    [InlineData(SourceDamage.Duplicate)]
    [InlineData(SourceDamage.MissingFrame)]
    [InlineData(SourceDamage.DuplicateExclusion)]
    [InlineData(SourceDamage.ForeignExclusion)]
    [InlineData(SourceDamage.MissingInput)]
    [InlineData(SourceDamage.UnknownConsumer)]
    [InlineData(SourceDamage.BadSense)]
    [InlineData(SourceDamage.ConflictingWriter)]
    public void InvalidExactReferencesAndWritersCannotEnterTheBundle(SourceDamage damage)
    {
        var entry = TestBundle.Entry(new("fan"));
        var source = Assert.Single(entry.Mechanical!.AirflowSources);
        var changed = damage switch
        {
            SourceDamage.MissingFrame => source with { Frame = new(99) },
            SourceDamage.DuplicateExclusion => source with { InternalOccluders = [source.InternalOccluders[0], source.InternalOccluders[0]] },
            SourceDamage.ForeignExclusion => source with { InternalOccluders = [new(new(99), new(0))] },
            SourceDamage.MissingInput => source with { Inputs = [] },
            SourceDamage.UnknownConsumer => source with { Inputs = source.Inputs.Select(input => input is ElectricalAirflowInput electrical
                ? electrical with { Consumer = new("absent") } : input).ToArray() },
            SourceDamage.BadSense => source with { Inputs = source.Inputs.Select(input => input is CoordinateAirflowInput coordinate
                ? coordinate with { Sense = 0 } : input).ToArray() },
            _ => source
        };
        entry = entry with { Mechanical = entry.Mechanical with
            { AirflowSources = damage == SourceDamage.Duplicate ? [changed, changed] : [changed] } };
        if (damage == SourceDamage.ConflictingWriter)
            entry = entry with { Presentation = entry.Presentation with
                { Bindings = [.. entry.Presentation.Bindings, entry.Presentation.Bindings[0]] } };
        Assert.NotEmpty(BundleValidator.Validate([new("invalid airflow", entry)]));
    }
}
