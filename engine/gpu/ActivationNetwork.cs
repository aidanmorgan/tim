using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CuriousContraptions.Gpu;

public readonly record struct ActivationNodeId(ulong Value);
public enum ActivationNodeKind : uint { ContactSource = 1, Latch = 2, Timer = 3 }
public enum ActivationPhase : uint { Clear, Latched }
public readonly record struct ActivationNodeDeclaration(ActivationNodeId Id, GpuBodyId Owner,
    ActivationNodeKind Kind, GpuContactTriggerId Trigger, uint DurationTicks = 0);
public readonly record struct ActivationEdge(ActivationNodeId Source, ActivationNodeId Target);

public readonly record struct ActivationLatch(ActivationNodeId Node, GpuBodyId Owner, ActivationPhase Phase,
    GpuContactTriggerId Trigger, GpuBodyId ContactBody, GpuColliderId Collider, uint EventOrdinal,
    Half EventPhase, LinearSpeed ApproachSpeed, ActivationOccurrenceKind Kind = default,
    ActivationNodeId Emitter = default, uint CauseOrdinal = 0, Half CausePhase = default)
{
    public ActivationOccurrence Occurrence => new(Kind, Emitter, new(EventOrdinal, EventPhase),
        new(Trigger, ContactBody, Collider, new(CauseOrdinal, CausePhase), ApproachSpeed));
    public static ActivationLatch From(ActivationNodeDeclaration node, ActivationOccurrence occurrence) =>
        new(node.Id, node.Owner, ActivationPhase.Latched, occurrence.Cause.Trigger, occurrence.Cause.Body,
            occurrence.Cause.Collider, occurrence.Time.Ordinal, occurrence.Time.Phase, occurrence.Cause.ApproachSpeed,
            occurrence.Kind, occurrence.Emitter, occurrence.Cause.Time.Ordinal, occurrence.Cause.Time.Phase);
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
                !PhysicsDeclarationBounds.Zero(EventPhase) || !PhysicsDeclarationBounds.Zero(ApproachSpeed.Value) ||
                Kind != 0 || Emitter.Value != 0 || CauseOrdinal != 0 || !PhysicsDeclarationBounds.Zero(CausePhase))
                throw new ArgumentException("Clear activation retained an event.");
        }
        else Occurrence.Validate();
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

public readonly record struct ActivationCheckpoint(PhysicsActivationRead Activations, PhysicsTimerRead Timers);

/// <summary>One bounded C# discrete network. Catalogue parts declare nodes; none owns a clock or callback.</summary>
public sealed class ActivationNetwork
{
    public const int Capacity = 8;
    private readonly ActivationNodeDeclaration[] _nodes;
    private readonly ActivationEdge[] _edges;
    private readonly ActivationTimerDeclaration[] _timers;
    private readonly uint _substeps;

    public ActivationNetwork(ReadOnlySpan<ActivationNodeDeclaration> nodes, ReadOnlySpan<ActivationEdge> edges,
        uint substeps = 4)
    {
        _ = ActivationTime.Boundary(new(0), substeps); _substeps = substeps;
        if (nodes.Length > Capacity || edges.Length > WorkshopConnections.Capacity)
            throw new ArgumentException("Activation declaration capacity exceeded.");
        _nodes = nodes.ToArray(); _edges = edges.ToArray();
        Array.Sort(_nodes, (a,b) => a.Id.Value.CompareTo(b.Id.Value));
        var owners = new HashSet<GpuBodyId>(); var triggers = new HashSet<GpuContactTriggerId>();
        var timers = new List<ActivationTimerDeclaration>();
        for (var i = 0; i < _nodes.Length; i++)
        {
            var node = _nodes[i];
            if (node.Id.Value == 0 || node.Owner.Value == 0 || !Enum.IsDefined(node.Kind) || !owners.Add(node.Owner) ||
                (i != 0 && _nodes[i - 1].Id == node.Id) ||
                (node.Kind == ActivationNodeKind.ContactSource ? node.Trigger.Value == 0 || !triggers.Add(node.Trigger) : node.Trigger.Value != 0) ||
                (node.Kind == ActivationNodeKind.Timer ? node.DurationTicks == 0 : node.DurationTicks != 0))
                throw new ArgumentException("Invalid activation node declaration.");
            if (node.Kind == ActivationNodeKind.Timer) timers.Add(new(node.Id, node.DurationTicks));
        }
        _timers = timers.ToArray();
        var links = new HashSet<ActivationEdge>();
        foreach (var edge in _edges)
            if (edge.Source == edge.Target || !links.Add(edge) || Slot(edge.Source) < 0 || Slot(edge.Target) < 0 ||
                _nodes[Slot(edge.Source)].Kind == ActivationNodeKind.Latch ||
                _nodes[Slot(edge.Target)].Kind == ActivationNodeKind.ContactSource)
                throw new ArgumentException("Unsupported activation edge.");
        Array.Sort(_edges, (a,b) => a.Source != b.Source ? a.Source.Value.CompareTo(b.Source.Value) : a.Target.Value.CompareTo(b.Target.Value));
    }
    public PhysicsActivationRead Clear()
    {
        Span<ActivationLatch> values = stackalloc ActivationLatch[Capacity];
        for (var i = 0; i < _nodes.Length; i++) values[i] = ActivationLatch.Clear(_nodes[i]);
        return new(values[.._nodes.Length]);
    }
    public PhysicsTimerRead ClearTimers()
    {
        Span<ActivationTimerState> values = stackalloc ActivationTimerState[Capacity];
        for (var i = 0; i < _timers.Length; i++) values[i] = ActivationTimerState.Ready(_timers[i].Node);
        return new(values[.._timers.Length]);
    }

    public ActivationCheckpoint Consume(PhysicsActivationRead committed, PhysicsTimerRead committedTimers,
        ReadOnlySpan<ContactTriggerRead> events, SimulationTick boundary)
    {
        ValidatePopulation(committed, committedTimers);
        if (events.Length > PhysicsSceneDeclaration.TriggerCapacity) throw new ArgumentException("Contact capacity exceeded.");
        var end = ActivationTime.Boundary(boundary, _substeps);
        Span<ActivationLatch> values = stackalloc ActivationLatch[Capacity];
        Span<ActivationTimerState> timers = stackalloc ActivationTimerState[Capacity];
        for (var i = 0; i < committed.Count; i++) values[i] = committed[i];
        for (var i = 0; i < committedTimers.Count; i++) timers[i] = committedTimers[i];
        Span<ActivationOccurrence> pending = stackalloc ActivationOccurrence[Capacity * 2];
        var count = 0;
        for (var i = 0; i < events.Length; i++)
        {
            var item = events[i]; var slot = -1;
            for (var j = 0; j < _nodes.Length; j++)
                if (_nodes[j].Kind == ActivationNodeKind.ContactSource && _nodes[j].Trigger == item.Id) slot = j;
            if (slot < 0 || item.Owner != _nodes[slot].Owner || item.OccurrenceCount > 1)
                throw new ArgumentException("Unowned contact occurrence.");
            for (var j = 0; j < i; j++) if (events[j].Id == item.Id)
                throw new ArgumentException("Duplicate contact occurrence batch.");
            if (item.OccurrenceCount == 0) continue;
            var time = new ActivationTime(item.EventOrdinal, item.EventPhase);
            var occurrence = new ActivationOccurrence(ActivationOccurrenceKind.Contact, _nodes[slot].Id, time,
                new(item.Id, item.Target, item.Collider, time, item.ApproachSpeed));
            occurrence.Validate();
            if (time.CompareTo(end) > 0) throw new ArgumentException("Future contact occurrence.");
            if (values[slot].Phase == ActivationPhase.Latched)
            {
                var prior = values[slot].Occurrence;
                var order = time.CompareTo(prior.Time);
                if (order < 0 || (order == 0 && occurrence != prior)) throw new ArgumentException("Stale or changed occurrence.");
                continue;
            }
            pending[count++] = occurrence;
        }
        for (var i = 0; i < _timers.Length; i++)
        {
            timers[i] = timers[i].Complete(boundary, _substeps, out var elapsed);
            if (elapsed is { } occurrence) pending[count++] = occurrence;
        }
        // Every source emits once. At most one physical event and one timer event per node;
        // positive integer durations prevent instantaneous timer cycles.
        while (count != 0)
        {
            var first = 0;
            for (var i = 1; i < count; i++) if (Compare(pending[i], pending[first]) < 0) first = i;
            var occurrence = pending[first]; pending[first] = pending[--count];
            var source = Slot(occurrence.Emitter);
            if (values[source].Phase != ActivationPhase.Clear) throw new ArgumentException("Duplicate source emission.");
            values[source] = ActivationLatch.From(_nodes[source], occurrence);
            foreach (var edge in _edges)
            {
                if (edge.Source != occurrence.Emitter) continue;
                var target = Slot(edge.Target);
                if (_nodes[target].Kind == ActivationNodeKind.Latch)
                {
                    if (values[target].Phase == ActivationPhase.Clear) values[target] = ActivationLatch.From(_nodes[target], occurrence);
                }
                else
                {
                    var index = TimerSlot(edge.Target);
                    timers[index] = timers[index].Trigger(_timers[index], occurrence, _substeps);
                    timers[index] = timers[index].Complete(boundary, _substeps, out var elapsed);
                    if (elapsed is { } due)
                    {
                        if (count == pending.Length) throw new ArgumentException("Activation fanout capacity exceeded.");
                        pending[count++] = due;
                    }
                }
            }
        }
        return new(new(values[.._nodes.Length]), new(timers[.._timers.Length]));
    }

    private void ValidatePopulation(PhysicsActivationRead activations, PhysicsTimerRead timers)
    {
        if (activations.Count != _nodes.Length || timers.Count != _timers.Length)
            throw new ArgumentException("Discrete checkpoint population changed.");
        for (var i = 0; i < _nodes.Length; i++)
        {
            activations[i].Validate();
            if (activations[i].Node != _nodes[i].Id || activations[i].Owner != _nodes[i].Owner)
                throw new ArgumentException("Activation checkpoint identity changed.");
        }
        for (var i = 0; i < _timers.Length; i++)
        {
            timers[i].Validate();
            if (timers[i].Node != _timers[i].Node) throw new ArgumentException("Timer checkpoint identity changed.");
        }
    }

    public void ValidateRead(PhysicsActivationRead values, PhysicsTimerRead timers, SimulationTick tick,
        uint substeps, PhysicsSceneDeclaration scene)
    {
        ValidatePopulation(values, timers);
        if (substeps != _substeps) throw new ArgumentException("Activation clock changed.");
        var end = ActivationTime.Boundary(tick, substeps);
        for (var i = 0; i < _nodes.Length; i++)
        {
            var value = values[i]; var node = _nodes[i];
            if (tick.Value == 0 && value.Phase != ActivationPhase.Clear)
                throw new ArgumentException("Admission retained an activation.");
            if (value.Phase == ActivationPhase.Clear) continue;
            var occurrence = value.Occurrence;
            ValidateOccurrence(occurrence, end, scene);
            if (node.Kind == ActivationNodeKind.ContactSource &&
                (occurrence.Kind != ActivationOccurrenceKind.Contact || occurrence.Emitter != node.Id || occurrence.Cause.Trigger != node.Trigger))
                throw new ArgumentException("Physical activation does not own its trigger.");
            if (node.Kind == ActivationNodeKind.Latch)
            {
                var expected = EarliestInput(node.Id, values);
                if (expected != occurrence) throw new ArgumentException("Activation target differs from its routed source.");
            }
        }
        for (var i = 0; i < _nodes.Length; i++)
            if (_nodes[i].Kind == ActivationNodeKind.Latch && values[i].Phase == ActivationPhase.Clear &&
                EarliestInput(_nodes[i].Id, values) is not null)
                throw new ArgumentException("Activation output was lost.");

        for (var i = 0; i < _timers.Length; i++)
        {
            var state = timers[i]; var declared = _timers[i]; var slot = Slot(state.Node);
            var expected = EarliestInput(state.Node, values);
            if (expected is null)
            {
                if (state != ActivationTimerState.Ready(state.Node) || values[slot].Phase != ActivationPhase.Clear)
                    throw new ArgumentException("Disconnected timer retained state.");
                continue;
            }
            ValidateOccurrence(state.Input, end, scene);
            var started = expected.Value.Time.CeilingTick(substeps);
            var due = checked(started + declared.DurationTicks);
            var phase = tick.Value < due ? ActivationTimerPhase.Counting : ActivationTimerPhase.Finished;
            if (state.Input != expected || state.StartedTick != started || state.DueTick != due || state.Phase != phase)
                throw new ArgumentException("Timer read differs from its declaration, input or deadline.");
            var output = phase == ActivationTimerPhase.Finished ?
                ActivationLatch.From(_nodes[slot], new(ActivationOccurrenceKind.TimerElapsed, state.Node,
                    ActivationTime.Boundary(new(due), substeps), state.Input.Cause)) : ActivationLatch.Clear(_nodes[slot]);
            if (values[slot] != output) throw new ArgumentException("Timer completion output changed.");
        }
    }
    private void ValidateOccurrence(ActivationOccurrence occurrence, ActivationTime end, PhysicsSceneDeclaration scene)
    {
        occurrence.Validate();
        if (occurrence.Time.CompareTo(end) > 0) throw new ArgumentException("Future activation read.");
        var emitter = Slot(occurrence.Emitter);
        if (emitter < 0 || (occurrence.Kind == ActivationOccurrenceKind.Contact ?
            _nodes[emitter].Kind != ActivationNodeKind.ContactSource : _nodes[emitter].Kind != ActivationNodeKind.Timer))
            throw new ArgumentException("Foreign activation emitter.");
        ContactTriggerDeclaration? trigger = null;
        foreach (var candidate in scene.Triggers) if (candidate.Id == occurrence.Cause.Trigger) trigger = candidate;
        if (trigger is not { } contact || occurrence.Cause.Body != contact.Target ||
            occurrence.Cause.ApproachSpeed.Value < contact.Threshold.Value ||
            (occurrence.Kind == ActivationOccurrenceKind.Contact && contact.Owner != _nodes[emitter].Owner))
            throw new ArgumentException("Activation cause differs from the declared contact.");
        var owned = false;
        foreach (var collider in scene.Colliders)
            if (collider.Id == occurrence.Cause.Collider && collider.Body == contact.Owner) owned = true;
        if (!owned) throw new ArgumentException("Activation cause collider is unowned.");
    }
    private ActivationOccurrence? EarliestInput(ActivationNodeId target, PhysicsActivationRead values)
    {
        ActivationOccurrence? first = null;
        foreach (var edge in _edges)
        {
            if (edge.Target != target) continue;
            var source = values[Slot(edge.Source)];
            if (source.Phase != ActivationPhase.Latched) continue;
            if (first is not { } prior || Compare(source.Occurrence, prior) < 0) first = source.Occurrence;
        }
        return first;
    }
    private int Slot(ActivationNodeId id)
    {
        for (var i = 0; i < _nodes.Length; i++) if (_nodes[i].Id == id) return i;
        return -1;
    }
    private int TimerSlot(ActivationNodeId id)
    {
        for (var i = 0; i < _timers.Length; i++) if (_timers[i].Node == id) return i;
        throw new ArgumentException("Undeclared timer node.");
    }
    private static int Compare(ActivationOccurrence a, ActivationOccurrence b)
    {
        var time = a.Time.CompareTo(b.Time);
        return time != 0 ? time : a.Emitter.Value.CompareTo(b.Emitter.Value);
    }
}

/// <summary>Catalogue boundary supplies declarations; no per-part controller callback.</summary>
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
            else if (instance is WorkshopDelay delay)
                nodes.Add(new(new(delay.Id.Value), delay.Id, ActivationNodeKind.Timer, default, delay.Duration.Ticks(construction.Settings.Simulation)));
        }
        foreach (var link in construction.Connections) edges.Add(new(new(link.Source.Value), new(link.Target.Value)));
        return new(nodes.ToArray(), edges.ToArray(), new WorkshopGpuProfile(construction.Settings.Simulation,
            construction.Settings.Physical, new(1)).Substeps);
    }
}
