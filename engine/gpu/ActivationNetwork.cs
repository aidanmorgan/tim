using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CuriousContraptions.Gpu;

public readonly record struct ActivationNodeId(ulong Value);
public enum ActivationNodeKind : uint { ContactSource = 1, Latch = 2 }
public enum ActivationPhase : uint { Clear, Latched }
public readonly record struct ActivationNodeDeclaration(ActivationNodeId Id, GpuBodyId Owner,
    ActivationNodeKind Kind, GpuContactTriggerId Trigger);
public readonly record struct ActivationEdge(ActivationNodeId Source, ActivationNodeId Target);

public readonly record struct ActivationLatch(ActivationNodeId Node, GpuBodyId Owner, ActivationPhase Phase,
    GpuContactTriggerId Trigger, GpuBodyId ContactBody, GpuColliderId Collider, uint EventOrdinal,
    Half EventPhase, LinearSpeed ApproachSpeed)
{
    public static ActivationLatch Clear(ActivationNodeDeclaration node) => new(node.Id, node.Owner,
        ActivationPhase.Clear, default, default, default, 0, (Half)0, new((Half)0));
    public void Validate()
    {
        if (Node.Value == 0 || Owner.Value == 0 || !Enum.IsDefined(Phase) ||
            !Half.IsFinite(EventPhase) || EventPhase < (Half)(-2048) || EventPhase >= (Half)2048 ||
            (EventOrdinal == 0 && EventPhase < (Half)0) ||
            !Half.IsFinite(ApproachSpeed.Value) || ApproachSpeed.Value < (Half)0 || ApproachSpeed.Value > (Half)128)
            throw new ArgumentException("Invalid activation latch.");
        if (Phase == ActivationPhase.Clear)
        {
            if (Trigger.Value != 0 || ContactBody.Value != 0 || Collider.Value != 0 || EventOrdinal != 0 ||
                !PhysicsDeclarationBounds.Zero(EventPhase) || !PhysicsDeclarationBounds.Zero(ApproachSpeed.Value))
                throw new ArgumentException("Clear activation retained an event.");
        }
        else if (Trigger.Value == 0 || ContactBody.Value == 0 || Collider.Value == 0)
            throw new ArgumentException("Latched activation requires its physical event identities.");
    }
}

[InlineArray(ActivationNetwork.Capacity)]
internal struct ActivationStorage { private ActivationLatch _first; }

public readonly struct PhysicsActivationRead
{
    private readonly ActivationStorage _values;
    public byte Count { get; }
    public PhysicsActivationRead(ReadOnlySpan<ActivationLatch> values)
    {
        if (values.Length > ActivationNetwork.Capacity) throw new ArgumentException("Activation capacity exceeded.");
        Count = checked((byte)values.Length); _values = default;
        for (var i = 0; i < values.Length; i++)
        {
            values[i].Validate();
            if (i != 0 && values[i - 1].Node.Value >= values[i].Node.Value)
                throw new ArgumentException("Activation nodes must be uniquely ordered.");
            _values[i] = values[i];
        }
    }
    public ActivationLatch this[int index] => index >= 0 && index < Count ? _values[index] : throw new ArgumentOutOfRangeException(nameof(index));
}

/// <summary>Bounded discrete routing only. Numerical impact qualification belongs to the GPU.</summary>
public sealed class ActivationNetwork
{
    public const int Capacity = 8;
    private readonly ActivationNodeDeclaration[] _nodes;
    private readonly ActivationEdge[] _edges;
    public ActivationNetwork(ReadOnlySpan<ActivationNodeDeclaration> nodes, ReadOnlySpan<ActivationEdge> edges)
    {
        if (nodes.Length > Capacity || edges.Length > WorkshopConnections.Capacity)
            throw new ArgumentException("Activation declaration capacity exceeded.");
        _nodes = nodes.ToArray(); _edges = edges.ToArray();
        Array.Sort(_nodes, (a, b) => a.Id.Value.CompareTo(b.Id.Value));
        var owners = new HashSet<GpuBodyId>(); var triggers = new HashSet<GpuContactTriggerId>();
        for (var i = 0; i < _nodes.Length; i++)
        {
            var node = _nodes[i];
            if (node.Id.Value == 0 || node.Owner.Value == 0 || !Enum.IsDefined(node.Kind) || !owners.Add(node.Owner) ||
                (i != 0 && _nodes[i - 1].Id == node.Id) ||
                (node.Kind == ActivationNodeKind.ContactSource ? node.Trigger.Value == 0 || !triggers.Add(node.Trigger) : node.Trigger.Value != 0))
                throw new ArgumentException("Invalid activation node declaration.");
        }
        var links = new HashSet<ActivationEdge>();
        foreach (var edge in _edges)
            if (edge.Source == edge.Target || !links.Add(edge) || Slot(edge.Source) < 0 || Slot(edge.Target) < 0 ||
                _nodes[Slot(edge.Source)].Kind != ActivationNodeKind.ContactSource || _nodes[Slot(edge.Target)].Kind != ActivationNodeKind.Latch)
                throw new ArgumentException("Unsupported activation edge.");
        Array.Sort(_edges, (a, b) => a.Source != b.Source ? a.Source.Value.CompareTo(b.Source.Value) : a.Target.Value.CompareTo(b.Target.Value));
    }
    public PhysicsActivationRead Clear()
    {
        Span<ActivationLatch> values = stackalloc ActivationLatch[Capacity];
        for (var i = 0; i < _nodes.Length; i++) values[i] = ActivationLatch.Clear(_nodes[i]);
        return new(values[.._nodes.Length]);
    }

    public PhysicsActivationRead Consume(PhysicsActivationRead committed, ReadOnlySpan<ContactTriggerRead> events)
    {
        if (committed.Count != _nodes.Length || events.Length > PhysicsSceneDeclaration.TriggerCapacity)
            throw new ArgumentException("Activation checkpoint population changed.");
        Span<ActivationLatch> values = stackalloc ActivationLatch[Capacity];
        for (var i = 0; i < _nodes.Length; i++)
        {
            var value = committed[i]; value.Validate();
            if (value.Node != _nodes[i].Id || value.Owner != _nodes[i].Owner)
                throw new ArgumentException("Activation checkpoint identity changed.");
            values[i] = value;
        }
        Span<ContactTriggerRead> ordered = stackalloc ContactTriggerRead[PhysicsSceneDeclaration.TriggerCapacity];
        events.CopyTo(ordered);
        // Stable physical event order, independent of declaration array order.
        for (var i = 1; i < events.Length; i++)
        {
            var item = ordered[i]; var j = i;
            while (j > 0 && Compare(item, ordered[j - 1]) < 0) { ordered[j] = ordered[j - 1]; j--; }
            ordered[j] = item;
        }
        for (var i = 0; i < events.Length; i++)
        {
            var item = ordered[i]; var slot = -1;
            for (var j = 0; j < _nodes.Length; j++)
                if (_nodes[j].Kind == ActivationNodeKind.ContactSource && _nodes[j].Trigger == item.Id) slot = j;
            if (slot < 0 || item.Owner != _nodes[slot].Owner || item.OccurrenceCount > 1)
                throw new ArgumentException("Unowned contact occurrence.");
            for (var j = 0; j < i; j++) if (ordered[j].Id == item.Id)
                throw new ArgumentException("Duplicate contact occurrence batch.");
            if (item.OccurrenceCount == 0) continue;
            var activated = new ActivationLatch(_nodes[slot].Id, item.Owner, ActivationPhase.Latched,
                item.Id, item.Target, item.Collider, item.EventOrdinal, item.EventPhase, item.ApproachSpeed);
            activated.Validate();
            if (values[slot].Phase == ActivationPhase.Latched)
            {
                var prior = values[slot];
                var time = CompareTime(item.EventOrdinal, item.EventPhase, prior.EventOrdinal, prior.EventPhase);
                if (time < 0 || (time == 0 && activated != prior))
                    throw new ArgumentException("Stale or inconsistent activation event.");
                // New legitimate impacts never replace the original once-only activation.
                continue;
            }
            values[slot] = activated;
            foreach (var edge in _edges)
            {
                if (edge.Source != activated.Node) continue;
                var target = Slot(edge.Target);
                if (values[target].Phase == ActivationPhase.Clear)
                    values[target] = activated with { Node = _nodes[target].Id, Owner = _nodes[target].Owner };
            }
        }
        return new(values[.._nodes.Length]);
    }
    public void ValidateRead(PhysicsActivationRead values, SimulationTick tick, uint substeps, PhysicsSceneDeclaration scene)
    {
        if (values.Count != _nodes.Length || substeps is < 1 or > 8)
            throw new ArgumentException("Activation read population or cadence changed.");
        var end = checked((uint)(tick.Value * (ulong)substeps));
        for (var i = 0; i < _nodes.Length; i++)
        {
            var value = values[i]; value.Validate(); var node = _nodes[i];
            if (value.Node != node.Id || value.Owner != node.Owner ||
                (tick.Value == 0 && value.Phase != ActivationPhase.Clear))
                throw new ArgumentException("Activation read does not own its construction/admission.");
            if (value.Phase == ActivationPhase.Clear) continue;
            if (value.EventOrdinal > end || (value.EventOrdinal == end && value.EventPhase > (Half)0))
                throw new ArgumentException("Activation event lies beyond the committed physical endpoint.");
            ContactTriggerDeclaration? trigger = null;
            foreach (var declared in scene.Triggers) if (declared.Id == value.Trigger) trigger = declared;
            if (trigger is not { } contact || value.ContactBody != contact.Target || value.ApproachSpeed.Value < contact.Threshold.Value ||
                (node.Kind == ActivationNodeKind.ContactSource && (value.Trigger != node.Trigger || contact.Owner != node.Owner)))
                throw new ArgumentException("Activation event differs from its declared contact trigger.");
            var colliderOwned = false;
            foreach (var collider in scene.Colliders) if (collider.Id == value.Collider && collider.Body == contact.Owner) colliderOwned = true;
            if (!colliderOwned) throw new ArgumentException("Activation collider does not belong to its trigger.");
        }
        for (var i = 0; i < _nodes.Length; i++)
        {
            if (_nodes[i].Kind != ActivationNodeKind.Latch) continue;
            ActivationLatch? expected = null;
            foreach (var edge in _edges)
            {
                if (edge.Target != _nodes[i].Id) continue;
                var source = values[Slot(edge.Source)];
                if (source.Phase != ActivationPhase.Latched) continue;
                if (expected is not { } prior || CompareTime(source.EventOrdinal, source.EventPhase, prior.EventOrdinal, prior.EventPhase) < 0 ||
                    (CompareTime(source.EventOrdinal, source.EventPhase, prior.EventOrdinal, prior.EventPhase) == 0 && source.Trigger.Value < prior.Trigger.Value))
                    expected = source;
            }
            var routed = expected is { } occurrence ? occurrence with { Node = _nodes[i].Id, Owner = _nodes[i].Owner } : ActivationLatch.Clear(_nodes[i]);
            if (values[i] != routed) throw new ArgumentException("Activation target differs from its declared routed source.");
        }
    }
    private int Slot(ActivationNodeId id)
    {
        for (var i = 0; i < _nodes.Length; i++) if (_nodes[i].Id == id) return i;
        return -1;
    }
    private static int Compare(ContactTriggerRead a, ContactTriggerRead b)
    {
        var time = CompareTime(a.EventOrdinal, a.EventPhase, b.EventOrdinal, b.EventPhase);
        return time != 0 ? time : a.Id.Value.CompareTo(b.Id.Value);
    }
    private static int CompareTime(uint a, Half ap, uint b, Half bp) => a != b ? a.CompareTo(b) : ap.CompareTo(bp);
}

/// <summary>Catalogue boundary emits generic discrete nodes and edges; no part supplies an update loop.</summary>
public static class WorkshopActivationCompiler
{
    public static ActivationNetwork Compile(WorkshopConstruction construction)
    {
        construction.Validate();
        var nodes = new List<ActivationNodeDeclaration>(); var edges = new List<ActivationEdge>();
        foreach (var instance in construction.Instances)
        {
            if (instance is WorkshopSwitch trigger)
                nodes.Add(new(new(trigger.Id.Value), trigger.Id, ActivationNodeKind.ContactSource, WorkshopPhysicsCompiler.ContactTrigger(trigger.Id)));
            else if (instance is WorkshopLamp lamp)
                nodes.Add(new(new(lamp.Id.Value), lamp.Id, ActivationNodeKind.Latch, default));
        }
        foreach (var link in construction.Connections) edges.Add(new(new(link.Source.Value), new(link.Target.Value)));
        return new(nodes.ToArray(), edges.ToArray());
    }
}
