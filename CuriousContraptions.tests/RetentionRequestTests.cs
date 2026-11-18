using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class RetentionRequestTests
{
    [Fact]
    public void SimultaneousSourcesCoalesceByOccurrenceAndExactTargetThenExpire()
    {
        var network = new ActivationNetwork([
            new(new(1), new(1), ActivationNodeKind.ContactSource, new(9)),
            new(new(2), new(2), ActivationNodeKind.ContactSource, new(10)),
            new(new(3), new(7), ActivationNodeKind.PhysicalRequest, default, RequestTarget: PhysicalRequestTarget.ForRetention(new(11)), RequestOperation: PhysicalRequestOperation.Release),
            new(new(4), new(7), ActivationNodeKind.PhysicalRequest, default, RequestTarget: PhysicalRequestTarget.ForRetention(new(12)), RequestOperation: PhysicalRequestOperation.Release),
            new(new(5), new(7), ActivationNodeKind.PhysicalRequest, default, RequestTarget: PhysicalRequestTarget.ForActuator(new(11)), RequestOperation: PhysicalRequestOperation.Release),
            new(new(6), new(7), ActivationNodeKind.PhysicalRequest, default, RequestTarget: PhysicalRequestTarget.ForActuator(new(11)), RequestOperation: PhysicalRequestOperation.Release)
        ], [new(new(1), new(3), PortRole.ActivationIn), new(new(2), new(3), PortRole.ActivationIn),
            new(new(1), new(4), PortRole.ActivationIn), new(new(1), new(5), PortRole.ActivationIn), new(new(2), new(5), PortRole.ActivationIn), new(new(2), new(6), PortRole.ActivationIn)]);
        var early = new ContactTriggerRead(new(9), new(1), new(99), 1, new(100), 2, 0f, new(1f));
        var late = new ContactTriggerRead(new(10), new(2), new(99), 1, new(100), 3, 0f, new(1f));
        var initial = network.Consume(network.Clear(), network.ClearTimers(), [late, early], new(1), [],
            memory: network.ClearMemory());
        var requests = PhysicalRequestComposition.Compose(network, initial.Activations);
        Assert.Equal(3, requests.Length);
        Assert.Equal(new PrismaticRetentionId(11), requests[0].Target.Retention);
        Assert.Equal(new PrismaticRetentionId(12), requests[1].Target.Retention);
        Assert.Equal(PhysicalRequestTarget.ForActuator(new(11)), requests[2].Target);
        Assert.All(requests, request => Assert.Equal(new ActivationNodeId(1), request.Occurrence.Emitter));
        Assert.All(requests, request => Assert.Equal(new ActivationTime(2, 0f), request.Occurrence.Time));
        var consumed = network.ConsumeScheduled(network.AdvanceScheduled(initial, new(2)), [], new(2), [], [], default, []);
        Assert.Empty(PhysicalRequestComposition.Compose(network, consumed.Activations));
        Assert.Equal(3, PhysicalRequestComposition.Compose(network, initial.Activations).Length);
        Assert.Equal(ActivationPhase.Latched, consumed.Activations[0].Phase);
    }

    [Fact]
    public void MalformedPhysicalRequestNodesAndEdgesAreRefusedAtNetworkAdmission()
    {
        var source = new ActivationNodeDeclaration(new(1), new(1), ActivationNodeKind.ContactSource, new(9));
        var timer = new ActivationNodeDeclaration(new(2), new(2), ActivationNodeKind.Timer, default, 12);
        var request = new ActivationNodeDeclaration(new(3), new(3), ActivationNodeKind.PhysicalRequest, default,
            RequestTarget: PhysicalRequestTarget.ForRetention(new(11)), RequestOperation: PhysicalRequestOperation.Release);
        var lamp = new ActivationNodeDeclaration(new(4), new(4), ActivationNodeKind.LevelIndicator, default);
        ActivationEdge Routed(ActivationNodeId from, ActivationNodeId to) => new(from, to, PortRole.ActivationIn);
        _ = new ActivationNetwork([source, timer, request], [Routed(source.Id, timer.Id), Routed(timer.Id, request.Id)]);
        Assert.Throws<ArgumentException>(() => new ActivationNetwork([source, request with { RequestOperation = null }],
            [Routed(source.Id, request.Id)]));
        Assert.Throws<ArgumentException>(() => new ActivationNetwork([source, request with { RequestTarget = default }],
            [Routed(source.Id, request.Id)]));
        Assert.Throws<ArgumentException>(() => new ActivationNetwork([source, timer with { RequestTarget = PhysicalRequestTarget.ForRetention(new(11)) }, request],
            [Routed(source.Id, timer.Id), Routed(timer.Id, request.Id)]));
        Assert.Throws<ArgumentException>(() => new ActivationNetwork([source with { RequestTarget = PhysicalRequestTarget.ForRetention(new(11)) }, request],
            [Routed(source.Id, request.Id)]));
        Assert.Throws<ArgumentException>(() => new ActivationNetwork([source with { RequestOperation = PhysicalRequestOperation.Release }, request],
            [Routed(source.Id, request.Id)]));
        Assert.Throws<ArgumentException>(() => new ActivationNetwork([source, request with
        {
            RequestTarget = new(PhysicalRequestTargetKind.FiniteWorkActuator, new(11), new(11))
        }], [Routed(source.Id, request.Id)]));
        // A request is a sink: it emits nothing onward, to a timer or to an indicator.
        Assert.Throws<ArgumentException>(() => new ActivationNetwork([source, timer, request],
            [Routed(source.Id, request.Id), Routed(request.Id, timer.Id)]));
        Assert.Throws<ArgumentException>(() => new ActivationNetwork([source, request, lamp],
            [Routed(source.Id, request.Id), Routed(request.Id, lamp.Id)]));
    }

    [Fact]
    public void CommittedRequestLatchMustBeOwnedByARoutedOccurrence()
    {
        var instances = new WorkshopInstances(
            WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("ball"), new(1), 0, 4, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("switch"), new(2), 0, 2, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("wound_spring"), new(3), 4, 4, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("switch"), new(5), 0, 1, 0, 0, 0, 0, 1));
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), instances, Connections: new(
            new WorkshopConnection(new(2), PortId.ActivationOut, new(3), PortId.ActivationIn, WorkshopConnectionDomain.Activation)));
        var scene = WorkshopPhysicsCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, construction, new(1, 2));
        var network = WorkshopActivationCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, construction);
        var press = new ContactTriggerRead(instances.Identities[new(2)].Signals[new("signal")].Trigger, new(2), new(1), 1,
            scene.Colliders.ToArray().First(collider => collider.Body.Value == 2).Id, 10, 0f, new(1f));
        var consumed = network.Consume(network.Clear(), network.ClearTimers(), [press], new(3), [], memory: network.ClearMemory());
        var request = Assert.Single(PhysicalRequestComposition.Compose(network, consumed.Activations));
        var routed = request.Occurrence.Emitter;
        network.ValidateRead(consumed.Activations, consumed.Timers, new(3), 4, scene);
        // The same committed latch, with its edge now routed from the other switch: the latch's emitter no longer owns it.
        var other = network.Nodes.ToArray().Single(node => node.Owner == new GpuBodyId(5) && node.Kind == ActivationNodeKind.ContactSource).Id;
        var rerouted = new ActivationNetwork(network.Nodes, network.Edges.ToArray()
                .Select(edge => edge.Source == routed ? edge with { Source = other } : edge).ToArray(),
            network.Substeps, network.PassageSensors, network.Contacts.ToArray());
        Assert.Throws<ArgumentException>(() => rerouted.ValidateRead(consumed.Activations, consumed.Timers, new(3), 4, scene));
    }

    [Fact]
    public void ScheduledTimerOccurrenceReachesSinkAtCommittedBoundaryOnly()
    {
        var network = new ActivationNetwork([
            new(new(1), new(1), ActivationNodeKind.ContactSource, new(9)),
            new(new(2), new(2), ActivationNodeKind.Timer, default, 12),
            new(new(3), new(3), ActivationNodeKind.PhysicalRequest, default, RequestTarget: PhysicalRequestTarget.ForRetention(new(11)), RequestOperation: PhysicalRequestOperation.Release)
        ], [new(new(1), new(2), PortRole.ActivationIn), new(new(2), new(3), PortRole.ActivationIn)]);
        var trigger = new ContactTriggerRead(new(9), new(1), new(99), 1, new(100), 0, 0f, new(1f));
        var first = network.Consume(network.Clear(), network.ClearTimers(), [trigger], new(0), [],
            memory: network.ClearMemory());
        var scheduled = network.AdvanceScheduled(first, new(12));
        Assert.Empty(PhysicalRequestComposition.Compose(network, scheduled.State.Activations));
        var committed = network.ConsumeScheduled(scheduled, [], new(12), [], [], default, []);
        var request = Assert.Single(PhysicalRequestComposition.Compose(network, committed.Activations));
        Assert.Equal(new ActivationNodeId(2), request.Occurrence.Emitter);
        Assert.Equal(new ActivationTime(48, 0f), request.Occurrence.Time);
        Assert.Equal(new ActivationTime(0, 0f), request.Occurrence.Cause.Time);
    }
}
