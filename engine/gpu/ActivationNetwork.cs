using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CuriousContraptions.Gpu;

public readonly record struct ActivationNodeId(ulong Value);
/// <summary>StickyFirstInput is the Signal lamp's first-input-wins hold (never reset within a run); StateLatch is the bistable
/// Set/Reset memory (CAT-037); LevelIndicator is a Signal lamp driven by a level ActivationOut (latch or plate), mirroring it.
/// ContactLoadSource is a contact-load row's committed level (Story 9R.4 D3): Latched from its first Loaded tick until its
/// committed release hold (Story 9R.5, ReleaseHoldTicks after the last Loaded tick) expires.
/// The kinds are distinct and share no name.</summary>
public enum ActivationNodeKind : uint { ContactSource = 1, StickyFirstInput = 2, Timer = 3, OrientationSource = 4, PassageSource = 5, Counter = 6, Oscillator = 7, StateLatch = 8, LevelIndicator = 9, ContactLoadSource = 10 }
public enum ActivationPhase : uint { Clear, Latched }
/// <summary>Committed bistable memory of one State Latch.</summary>
public enum SimulationLatchPhase : uint { Off = 0, On = 1 }
/// <summary>Requests a State Latch accumulates within one tick; Reset dominates Set at the boundary.</summary>
[Flags]
public enum StateLatchRequest : uint { None = 0, Set = 1, Reset = 2 }
public readonly record struct ActivationNodeDeclaration(ActivationNodeId Id, GpuBodyId Owner,
    ActivationNodeKind Kind, GpuContactTriggerId Trigger, uint DurationTicks = 0, GpuOrientationSensorId Sensor = default, GpuPassageSensorId Passage = default, uint TargetCount = 0, uint IntervalTicks = 0, GpuBodyId PowerSource = default, GpuColliderId Collider = default,
    GpuContactLoadSensorId ContactLoad = default, uint ReleaseHoldTicks = 0);
/// <summary>One routed signal; Input names the target socket (ActivationIn, or SetIn/ResetIn on a State Latch).</summary>
public readonly record struct ActivationEdge(ActivationNodeId Source, ActivationNodeId Target, WorkshopSocket Input);

public readonly record struct ActivationLatch(ActivationNodeId Node, GpuBodyId Owner, ActivationPhase Phase,
    GpuEventSourceId Source, GpuBodyId Body, GpuColliderId Collider, uint EventOrdinal,
    float EventPhase, LinearSpeed ApproachSpeed, ActivationOccurrenceKind Kind = default,
    ActivationNodeId Emitter = default, uint CauseOrdinal = 0, float CausePhase = default)
{
    public ActivationOccurrence Occurrence => new(Kind, Emitter, new(EventOrdinal, EventPhase),
        new(Source, Body, Collider, new(CauseOrdinal, CausePhase), ApproachSpeed));
    public static ActivationLatch From(ActivationNodeDeclaration node, ActivationOccurrence occurrence) =>
        new(node.Id, node.Owner, ActivationPhase.Latched, occurrence.Cause.Source, occurrence.Cause.Body,
            occurrence.Cause.Collider, occurrence.Time.Ordinal, occurrence.Time.Phase, occurrence.Cause.ApproachSpeed,
            occurrence.Kind, occurrence.Emitter, occurrence.Cause.Time.Ordinal, occurrence.Cause.Time.Phase);
    public static ActivationLatch Clear(ActivationNodeDeclaration node) => new(node.Id, node.Owner,
        ActivationPhase.Clear, default, default, default, 0, 0f, new((Half)0));
    public void Validate()
    {
        if (Node.Value == 0 || Owner.Value == 0 || !Enum.IsDefined(Phase) ||
            !float.IsFinite(EventPhase) || EventPhase < -2048f || EventPhase >= 2048f ||
            (EventOrdinal == 0 && EventPhase < 0f) ||
            !Half.IsFinite(ApproachSpeed.Value) || ApproachSpeed.Value < (Half)0 || ApproachSpeed.Value > (Half)4096)
            throw new ArgumentException("Invalid activation latch.");
        if (Phase == ActivationPhase.Clear)
        {
            if (Source.Value != 0 || Body.Value != 0 || Collider.Value != 0 || EventOrdinal != 0 ||
                EventPhase != 0f || !PhysicsDeclarationBounds.Zero(ApproachSpeed.Value) ||
                Kind != 0 || Emitter.Value != 0 || CauseOrdinal != 0 || CausePhase != 0f)
                throw new ArgumentException("Clear activation retained an event.");
        }
        else Occurrence.Validate();
    }
}

/// <summary>The committed activation population, sized exactly from the compiled network (no fixed activation table; owner
/// decision, 20 Oct 2026; Story 9R.5b: no instance bound either).</summary>
public readonly struct PhysicsActivationRead
{
    private readonly ActivationLatch[]? _values;
    public int Count => _values?.Length ?? 0;
    public PhysicsActivationRead(ReadOnlySpan<ActivationLatch> values)
    {
        for (var i = 0; i < values.Length; i++)
        {
            values[i].Validate();
            if (i != 0 && values[i - 1].Node.Value >= values[i].Node.Value)
                throw new ArgumentException("Activation nodes must be uniquely ordered.");
        }
        _values = values.ToArray();
    }
    public ActivationLatch this[int index] => index >= 0 && index < Count ? _values![index] : throw new ArgumentOutOfRangeException(nameof(index));
}

/// <summary>The committed discrete checkpoint: activations, timers and the Counter/Clock memory rows (Story 9R.1 D3).</summary>
public readonly record struct ActivationCheckpoint(PhysicsActivationRead Activations, PhysicsTimerRead Timers,
    PhysicsMemoryRead Memory = default);

/// <summary>One bounded C# discrete network. Catalogue parts declare nodes; none owns a clock or callback.</summary>
public sealed class ActivationNetwork
{
    private readonly ActivationNodeDeclaration[] _nodes;
    private readonly ActivationEdge[] _edges;
    private readonly ActivationTimerDeclaration[] _timers;
    private readonly PassageSensorDeclaration[] _passageSensors;
    private readonly uint _substeps;
    // Declaration data only: the memory-row index of each node (-1 for a node without Counter/Clock memory).
    private readonly int[] _memorySlots;
    private readonly ActivationNodeDeclaration[] _memoryNodes;
    // Story 9R.5b: Consume's working tables, sized from this compiled network once (per Run, never per tick) instead of stack
    // allocations that grow with an unbounded population. Consume clears each before use; the pending queue grows on demand.
    private readonly ActivationLatch[] _scratchValues;
    private readonly ActivationTimerState[] _scratchTimers;
    private readonly ActivationMemory[] _scratchRows;
    private readonly StateLatchRequest[] _latchRequests;
    private readonly ActivationOccurrence[] _latchSetBy;
    private ActivationOccurrence[] _pending;
    public ReadOnlySpan<ActivationNodeDeclaration> Nodes => _nodes;
    public ReadOnlySpan<ActivationEdge> Edges => _edges;
    public ReadOnlySpan<PassageSensorDeclaration> PassageSensors => _passageSensors;

    public ActivationNetwork(ReadOnlySpan<ActivationNodeDeclaration> nodes, ReadOnlySpan<ActivationEdge> edges,
        uint substeps = 4, ReadOnlySpan<PassageSensorDeclaration> passageSensors = default)
    {
        _ = ActivationTime.Boundary(new(0), substeps); _substeps = substeps;
        // Owner decision, 20 Oct 2026: no activation-slot limit, and (Story 9R.5b) no instance or connection bound.
        _nodes = nodes.ToArray(); _edges = edges.ToArray();
        _passageSensors = passageSensors.ToArray();
        Array.Sort(_nodes, (a,b) => a.Id.Value.CompareTo(b.Id.Value));
        _memorySlots = new int[_nodes.Length];
        var memoryNodes = new List<ActivationNodeDeclaration>();
        for (var i = 0; i < _nodes.Length; i++)
        {
            _memorySlots[i] = ActivationMemory.Holds(_nodes[i].Kind) ? memoryNodes.Count : -1;
            if (_memorySlots[i] >= 0) memoryNodes.Add(_nodes[i]);
        }
        _memoryNodes = memoryNodes.ToArray();
        var owners = new HashSet<GpuBodyId>(); var triggers = new HashSet<GpuContactTriggerId>();
        var sensors = new HashSet<GpuOrientationSensorId>();
        var passages = new HashSet<GpuPassageSensorId>();
        var loads = new HashSet<GpuContactLoadSensorId>();
        var timers = new List<ActivationTimerDeclaration>();
        for (var i = 0; i < _nodes.Length; i++)
        {
            var node = _nodes[i];
            if (node.Id.Value == 0 || node.Owner.Value == 0 || !Enum.IsDefined(node.Kind) || !owners.Add(node.Owner) ||
                (i != 0 && _nodes[i - 1].Id == node.Id) ||
                (node.Kind == ActivationNodeKind.ContactSource ? node.Trigger.Value == 0 || !triggers.Add(node.Trigger) : node.Trigger.Value != 0) ||
                (node.Kind == ActivationNodeKind.OrientationSource ? node.Sensor.Value == 0 || !sensors.Add(node.Sensor) : node.Sensor.Value != 0) ||
                (node.Kind == ActivationNodeKind.PassageSource ? node.Passage.Value == 0 || !passages.Add(node.Passage) : node.Passage.Value != 0) ||
                (node.Kind == ActivationNodeKind.ContactLoadSource ? node.ContactLoad.Value == 0 || !loads.Add(node.ContactLoad) : node.ContactLoad.Value != 0) ||
                (node.Kind == ActivationNodeKind.ContactLoadSource ? node.ReleaseHoldTicks == 0 : node.ReleaseHoldTicks != 0) ||
                (node.Kind == ActivationNodeKind.Timer ? node.DurationTicks == 0 : node.DurationTicks != 0) ||
                (node.Kind == ActivationNodeKind.Counter ? node.TargetCount < 1 || node.TargetCount > 9 : node.TargetCount != 0) ||
                (node.Kind == ActivationNodeKind.Oscillator ? node.IntervalTicks == 0 : (node.IntervalTicks != 0 || node.PowerSource.Value != 0 || node.Collider.Value != 0)))
                throw new ArgumentException("Invalid activation node declaration.");
            if (node.Kind == ActivationNodeKind.Timer) timers.Add(new(node.Id, node.DurationTicks));
        }
        foreach (var passage in _passageSensors)
        {
            passage.Validate();
            if (!passages.Contains(passage.Id)) throw new ArgumentException("Passage sensor has no owning node.");
        }
        _timers = timers.ToArray();
        _scratchValues = new ActivationLatch[_nodes.Length];
        _scratchTimers = new ActivationTimerState[_timers.Length];
        _scratchRows = new ActivationMemory[_memoryNodes.Length];
        _latchRequests = new StateLatchRequest[_nodes.Length];
        _latchSetBy = new ActivationOccurrence[_nodes.Length];
        _pending = new ActivationOccurrence[Math.Max(16, _nodes.Length * 4)];
        var links = new HashSet<ActivationEdge>();
        foreach (var edge in _edges)
            if (edge.Source == edge.Target || !links.Add(edge) || Slot(edge.Source) < 0 || Slot(edge.Target) < 0 ||
                _nodes[Slot(edge.Source)].Kind is ActivationNodeKind.StickyFirstInput or ActivationNodeKind.LevelIndicator ||
                // A level source drives level indicators only, and a level indicator listens to a level source only.
                IsLevelSource(_nodes[Slot(edge.Source)].Kind) != (_nodes[Slot(edge.Target)].Kind == ActivationNodeKind.LevelIndicator) ||
                _nodes[Slot(edge.Target)].Kind is ActivationNodeKind.ContactSource or ActivationNodeKind.OrientationSource or ActivationNodeKind.PassageSource or ActivationNodeKind.Oscillator or ActivationNodeKind.ContactLoadSource ||
                (_nodes[Slot(edge.Target)].Kind == ActivationNodeKind.StateLatch
                    ? edge.Input is not (WorkshopSocket.SetIn or WorkshopSocket.ResetIn)
                    : edge.Input != WorkshopSocket.ActivationIn))
                throw new ArgumentException("Unsupported activation edge.");
        foreach (var node in _nodes)
            if (node.Kind == ActivationNodeKind.LevelIndicator && _edges.Count(edge => edge.Target == node.Id) != 1)
                throw new ArgumentException("A level indicator follows exactly one level source.");
        Array.Sort(_edges, (a,b) => a.Source != b.Source ? a.Source.Value.CompareTo(b.Source.Value) :
            a.Target != b.Target ? a.Target.Value.CompareTo(b.Target.Value) : a.Input.CompareTo(b.Input));
    }
    public PhysicsActivationRead Clear()
    {
        var values = new ActivationLatch[_nodes.Length];
        for (var i = 0; i < _nodes.Length; i++) values[i] = ActivationLatch.Clear(_nodes[i]);
        return new(values);
    }
    public PhysicsTimerRead ClearTimers()
    {
        var values = new ActivationTimerState[_timers.Length];
        for (var i = 0; i < _timers.Length; i++) values[i] = ActivationTimerState.Ready(_timers[i].Node);
        return new(values);
    }
    /// <summary>The admitted memory rows: every Counter at count 0 with no counted input, every Clock Stopped with no due tick.</summary>
    public PhysicsMemoryRead ClearMemory()
    {
        var values = new ActivationMemory[_memoryNodes.Length];
        for (var i = 0; i < _memoryNodes.Length; i++) values[i] = ActivationMemory.Clear(_memoryNodes[i]);
        return new(values);
    }

    /// <summary>Queues one pending occurrence, growing the queue when a tick's fanout exceeds it (no fanout capacity).</summary>
    private void Push(ref int count, ActivationOccurrence occurrence)
    {
        if (count == _pending.Length) Array.Resize(ref _pending, _pending.Length * 2);
        _pending[count++] = occurrence;
    }
    /// <summary>The committed Counter or Clock memory row of one owner (a query over the checkpoint; the network holds none).</summary>
    public ActivationMemory Memory(PhysicsMemoryRead values, GpuBodyId owner)
    {
        ValidateMemory(values);
        for (var i = 0; i < _memoryNodes.Length; i++)
            if (_memoryNodes[i].Owner == owner) return values[i];
        throw new ArgumentException("Counter or clock not found for owner.");
    }
    public int GetCounterTarget(GpuBodyId owner)
    {
        for (var i = 0; i < _nodes.Length; i++)
            if (_nodes[i].Owner == owner && _nodes[i].Kind == ActivationNodeKind.Counter)
                return (int)_nodes[i].TargetCount;
        throw new ArgumentException("Counter not found for owner.");
    }
    /// <summary>State Latch memory lives in the committed checkpoint: On is Latched with the Set occurrence, Off is Clear.</summary>
    public SimulationLatchPhase StateLatchPhase(PhysicsActivationRead values, GpuBodyId owner)
    {
        ValidatePopulation(values, ClearTimers());
        for (var i = 0; i < _nodes.Length; i++)
            if (_nodes[i].Owner == owner && _nodes[i].Kind == ActivationNodeKind.StateLatch)
                return values[i].Phase == ActivationPhase.Latched ? SimulationLatchPhase.On : SimulationLatchPhase.Off;
        throw new ArgumentException("State latch not found for owner.");
    }
    /// <summary>A switched contact (State Latch, Counter or contact-load level) is closed exactly while its committed value is Latched.</summary>
    public bool ContactClosed(PhysicsActivationRead values, GpuBodyId owner)
    {
        ValidatePopulation(values, ClearTimers());
        for (var i = 0; i < _nodes.Length; i++)
            if (_nodes[i].Owner == owner && _nodes[i].Kind is ActivationNodeKind.StateLatch or ActivationNodeKind.Counter or ActivationNodeKind.ContactLoadSource)
                return values[i].Phase == ActivationPhase.Latched;
        throw new ArgumentException("Switched contact not found for owner.");
    }
    /// <summary>Evaluates one candidate tick from the committed checkpoint. The committed memory rows are read, never written: the
    /// candidate's rows are returned in the checkpoint, so only a committed candidate changes the published Counter and Clock state.
    /// A network with Counter or Clock nodes requires their committed rows (an empty population is rejected).</summary>
    public ActivationCheckpoint Consume(PhysicsActivationRead committed, PhysicsTimerRead committedTimers,
        ReadOnlySpan<ContactTriggerRead> events, SimulationTick boundary, ReadOnlySpan<OrientationSensorRead> orientations,
        ReadOnlySpan<PassageSensorRead> passages = default, PhysicsElectricalRead electrical = default,
        Span<byte> eventBuffer = default, PhysicsMemoryRead memory = default, ReadOnlySpan<ContactLoadSensorRead> contactLoads = default)
    {
        ValidatePopulation(committed, committedTimers);
        ValidateMemory(memory);
        // Every table is sized from this compiled network's own population (no fixed activation, contact or orientation table);
        // the working tables are this network's own, cleared here.
        Span<ActivationMemory> rows = _scratchRows; rows.Clear();
        for (var i = 0; i < memory.Count; i++) rows[i] = memory[i];
        var end = ActivationTime.Boundary(boundary, _substeps);
        Span<ActivationLatch> values = _scratchValues; values.Clear();
        Span<ActivationTimerState> timers = _scratchTimers; timers.Clear();
        for (var i = 0; i < committed.Count; i++) values[i] = committed[i];
        for (var i = 0; i < committedTimers.Count; i++) timers[i] = committedTimers[i];
        Array.Clear(_pending);
        // Requests routed to each State Latch within this tick; they resolve once at the boundary below.
        Span<StateLatchRequest> latchRequests = _latchRequests; latchRequests.Clear();
        Span<ActivationOccurrence> latchSetBy = _latchSetBy; latchSetBy.Clear();
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
            var time = new ActivationTime(item.EventOrdinal, (float)item.EventPhase);
            var occurrence = new ActivationOccurrence(ActivationOccurrenceKind.Contact, _nodes[slot].Id, time,
                new(new(item.Id.Value), item.Target, item.Collider, time, item.ApproachSpeed));
            occurrence.Validate();
            if (time.CompareTo(end) > 0) throw new ArgumentException("Future contact occurrence.");
            if (values[slot].Phase == ActivationPhase.Latched)
            {
                var prior = values[slot].Occurrence;
                var order = time.CompareTo(prior.Time);
                if (order < 0 || (order == 0 && occurrence != prior)) throw new ArgumentException("Stale or changed occurrence.");
                continue;
            }
            Push(ref count, occurrence);
        }
        // Every orientation source must be read each tick: a missing read would silently leave its Domino unlatched.
        for (var i = 0; i < _nodes.Length; i++)
        {
            if (_nodes[i].Kind != ActivationNodeKind.OrientationSource) continue;
            var found = false;
            for (var j = 0; j < orientations.Length; j++) if (orientations[j].Id == _nodes[i].Sensor) found = true;
            if (!found) throw new ArgumentException("Orientation sensor read is missing.");
        }
        // A fired orientation sensor is sticky within its world: the committed latch and the read must agree exactly.
        for (var i = 0; i < orientations.Length; i++)
        {
            var item = orientations[i]; var slot = -1;
            for (var j = 0; j < _nodes.Length; j++)
                if (_nodes[j].Kind == ActivationNodeKind.OrientationSource && _nodes[j].Sensor == item.Id) slot = j;
            if (slot < 0 || item.Body != _nodes[slot].Owner || !Enum.IsDefined(item.Phase))
                throw new ArgumentException("Unowned orientation occurrence.");
            for (var j = 0; j < i; j++) if (orientations[j].Id == item.Id)
                throw new ArgumentException("Duplicate orientation occurrence batch.");
            if (item.Phase == OrientationSensorPhase.Armed)
            {
                if (values[slot].Phase == ActivationPhase.Latched) throw new ArgumentException("Orientation sensor regressed.");
                continue;
            }
            var time = new ActivationTime(item.EventOrdinal, (float)item.EventPhase);
            var occurrence = new ActivationOccurrence(ActivationOccurrenceKind.Orientation, _nodes[slot].Id, time,
                new(new(item.Id.Value), item.Body, item.Collider, time, new((Half)0)));
            occurrence.Validate();
            if (time.CompareTo(end) > 0) throw new ArgumentException("Future orientation occurrence.");
            if (values[slot].Phase == ActivationPhase.Latched)
            {
                if (values[slot].Occurrence != occurrence) throw new ArgumentException("Changed orientation occurrence.");
                continue;
            }
            Push(ref count, occurrence);
        }
        for (var i = 0; i < passages.Length; i++)
        {
            var item = passages[i]; var slot = -1;
            for (var j = 0; j < _nodes.Length; j++)
                if (_nodes[j].Kind == ActivationNodeKind.PassageSource && _nodes[j].Passage == item.Id) slot = j;
            if (slot < 0 || item.Frame != _nodes[slot].Owner || !Enum.IsDefined(item.Phase))
                throw new ArgumentException("Unowned passage occurrence.");
            for (var j = 0; j < i; j++) if (passages[j].Id == item.Id)
                throw new ArgumentException("Duplicate passage occurrence batch.");
            if (item.Phase != PassageSensorPhase.Fired) continue;
            var time = new ActivationTime(item.EventOrdinal, (float)item.EventPhase);
            var occurrence = new ActivationOccurrence(ActivationOccurrenceKind.Passage, _nodes[slot].Id, time,
                new(new(item.Id.Value), item.Body, item.Collider, time, item.Speed));
            occurrence.Validate();
            if (time.CompareTo(end) > 0) throw new ArgumentException("Future passage occurrence.");
            if (values[slot].Phase == ActivationPhase.Latched)
            {
                var prior = values[slot].Occurrence;
                var order = time.CompareTo(prior.Time);
                if (order < 0 || (order == 0 && occurrence != prior)) throw new ArgumentException("Stale or changed occurrence.");
                if (order == 0) continue;
            }
            Push(ref count, occurrence);
        }
        CommitContactLoads(values, rows, contactLoads, boundary, end);
        for (var i = 0; i < _timers.Length; i++)
        {
            timers[i] = timers[i].Complete(boundary, _substeps, out var elapsed);
            if (elapsed is { } occurrence) Push(ref count, occurrence);
        }
        for (var i = 0; i < _nodes.Length; i++)
        {
            if (_nodes[i].Kind != ActivationNodeKind.Oscillator) continue;
            var node = _nodes[i];
            var isPowered = false;
            if (node.PowerSource.Value != 0)
            {
                for (var e = 0; e < electrical.Count; e++)
                {
                    if (electrical[e].Owner == node.PowerSource && electrical[e].Available)
                    {
                        isPowered = true;
                        break;
                    }
                }
            }

            var slot = _memorySlots[i];
            if (!isPowered)
            {
                rows[slot] = rows[slot] with { Phase = OscillatorPhase.Stopped, DueTick = 0 };
                continue;
            }

            if (rows[slot].Phase == OscillatorPhase.Stopped)
                rows[slot] = rows[slot] with { Phase = OscillatorPhase.Running,
                    DueTick = checked((boundary.Value <= 1 ? 0 : boundary.Value) + node.IntervalTicks) };

            if (boundary.Value == rows[slot].DueTick)
            {
                rows[slot] = rows[slot] with { Count = checked(rows[slot].Count + 1), DueTick = checked(rows[slot].DueTick + node.IntervalTicks) };

                var time = ActivationTime.Boundary(boundary, _substeps);
                var colliderId = node.Collider.Value != 0 ? node.Collider : new(node.Id.Value);
                var sourceId = new GpuEventSourceId(colliderId.Value);
                var cause = new ActivationCause(sourceId, node.Owner, colliderId, time, new((Half)0));
                var occurrence = new ActivationOccurrence(ActivationOccurrenceKind.OscillatorTick, node.Id, time, cause);
                occurrence.Validate();
                Push(ref count, occurrence);

                if (eventBuffer.Length >= CuriousContraptions.Abstractions.EventRingBuffer.HeaderBytes)
                {
                    var record = new CuriousContraptions.Abstractions.SimulationEventRecord(
                        0,
                        (ulong)boundary.Value * 8_333_333UL,
                        CuriousContraptions.Abstractions.SimulationDomain.Signal,
                        CuriousContraptions.Abstractions.SimulationEventKind.TriggerActivated,
                        node.Owner.Value,
                        0,
                        P0: (float)rows[slot].Count,
                        P1: (float)node.IntervalTicks / (float)_substeps);
                    CuriousContraptions.Abstractions.EventRingBuffer.Publish(eventBuffer, record);
                }
            }
        }
        // Every source emits once. At most one physical event and one timer event per node;
        // positive integer durations prevent instantaneous timer cycles.
        while (count != 0)
        {
            var first = 0;
            for (var i = 1; i < count; i++) if (Compare(_pending[i], _pending[first]) < 0) first = i;
            var occurrence = _pending[first]; _pending[first] = _pending[--count];
            var source = Slot(occurrence.Emitter);
            if (values[source].Phase != ActivationPhase.Clear)
            {
                if (_nodes[source].Kind == ActivationNodeKind.PassageSource &&
                    occurrence.Time.CompareTo(values[source].Occurrence.Time) > 0)
                {
                }
                else if (occurrence.Kind == ActivationOccurrenceKind.CounterReached && values[source].Phase == ActivationPhase.Latched)
                {
                }
                else if (_nodes[source].Kind == ActivationNodeKind.Oscillator && occurrence.Kind == ActivationOccurrenceKind.OscillatorTick)
                {
                }
                else
                {
                    throw new ArgumentException("Duplicate source emission.");
                }
            }
            if (values[source].Phase == ActivationPhase.Clear)
                values[source] = ActivationLatch.From(_nodes[source], occurrence);
            foreach (var edge in _edges)
            {
                if (edge.Source != occurrence.Emitter) continue;
                var target = Slot(edge.Target);
                if (_nodes[target].Kind == ActivationNodeKind.StickyFirstInput)
                {
                    if (values[target].Phase == ActivationPhase.Clear) values[target] = ActivationLatch.From(_nodes[target], occurrence);
                }
                else if (_nodes[target].Kind == ActivationNodeKind.StateLatch)
                {
                    if (edge.Input == WorkshopSocket.ResetIn) latchRequests[target] |= StateLatchRequest.Reset;
                    else if ((latchRequests[target] & StateLatchRequest.Set) == 0 || Compare(occurrence, latchSetBy[target]) < 0)
                    { latchRequests[target] |= StateLatchRequest.Set; latchSetBy[target] = occurrence; }
                }
                else if (_nodes[target].Kind == ActivationNodeKind.Counter)
                {
                    var slot = _memorySlots[target];
                    var isNewTrigger = rows[slot].Last == default || occurrence.Time.CompareTo(rows[slot].Last) > 0;
                    if (isNewTrigger)
                    {
                        rows[slot] = rows[slot] with { Last = occurrence.Time, Count = checked(rows[slot].Count + 1) };
                        // The counter emits once per world: its emission is its Latched activation.
                        if (rows[slot].Count == _nodes[target].TargetCount && values[target].Phase == ActivationPhase.Clear)
                        {
                            var reached = new ActivationOccurrence(ActivationOccurrenceKind.CounterReached, _nodes[target].Id, occurrence.Time, occurrence.Cause);
                            values[target] = ActivationLatch.From(_nodes[target], reached);
                            Push(ref count, reached);
                        }
                    }
                }
                else
                {
                    var index = TimerSlot(edge.Target);
                    timers[index] = timers[index].Trigger(_timers[index], occurrence, _substeps);
                    timers[index] = timers[index].Complete(boundary, _substeps, out var elapsed);
                    if (elapsed is { } due)
                    {
                        Push(ref count, due);
                    }
                }
            }
        }
        // Every request routed this tick resolves at the boundary: Reset dominates, otherwise Set, otherwise memory holds.
        for (var i = 0; i < _nodes.Length; i++)
        {
            if (_nodes[i].Kind != ActivationNodeKind.StateLatch) continue;
            var current = values[i].Phase == ActivationPhase.Latched ? SimulationLatchPhase.On : SimulationLatchPhase.Off;
            var next = (latchRequests[i] & StateLatchRequest.Reset) != 0 ? SimulationLatchPhase.Off :
                (latchRequests[i] & StateLatchRequest.Set) != 0 ? SimulationLatchPhase.On : current;
            if (next == current) continue;
            values[i] = next == SimulationLatchPhase.On ? ActivationLatch.From(_nodes[i], latchSetBy[i]) : ActivationLatch.Clear(_nodes[i]);
            if (eventBuffer.Length >= CuriousContraptions.Abstractions.EventRingBuffer.HeaderBytes)
                CuriousContraptions.Abstractions.EventRingBuffer.Publish(eventBuffer, new CuriousContraptions.Abstractions.SimulationEventRecord(
                    0, (ulong)boundary.Value * 8_333_333UL, CuriousContraptions.Abstractions.SimulationDomain.Signal,
                    CuriousContraptions.Abstractions.SimulationEventKind.LatchSet, _nodes[i].Owner.Value, 0,
                    P0: next == SimulationLatchPhase.On ? 1f : 0f, P1: 0f));
        }
        // A level ActivationOut (latch memory, held plate load): each level indicator mirrors its source's settled value on the same boundary.
        foreach (var edge in _edges)
        {
            var source = Slot(edge.Source);
            if (!IsLevelSource(_nodes[source].Kind)) continue;
            var target = Slot(edge.Target);            values[target] = values[source].Phase == ActivationPhase.Latched
                ? ActivationLatch.From(_nodes[target], values[source].Occurrence) : ActivationLatch.Clear(_nodes[target]);
        }
        return new(new(values[.._nodes.Length]), new(timers[.._timers.Length]), new(rows[.._memoryNodes.Length]));
    }

    /// <summary>The level sources: a State Latch's memory and a contact-load row's held level. Each drives level indicators only.</summary>
    public static bool IsLevelSource(ActivationNodeKind kind) => kind is ActivationNodeKind.StateLatch or ActivationNodeKind.ContactLoadSource;

    /// <summary>Story 9R.4 D3 with the Story 9R.5 release hold: every contact-load level follows its own row's committed phase on this
    /// boundary. A Loaded tick keeps (or starts, at this boundary, with no dwell) a Latched level and commits its release due at
    /// boundary + ReleaseHoldTicks in the node's memory row. Any other phase keeps the level until that due boundary, then clears the
    /// level and the hold. With n ticks since the last Loaded tick the level is high while n &lt; ReleaseHoldTicks. Every row must be
    /// read each tick, exactly once.</summary>
    private void CommitContactLoads(Span<ActivationLatch> values, Span<ActivationMemory> rows, ReadOnlySpan<ContactLoadSensorRead> contactLoads,
        SimulationTick boundary, ActivationTime end)
    {
        for (var i = 0; i < _nodes.Length; i++)
        {
            if (_nodes[i].Kind != ActivationNodeKind.ContactLoadSource) continue;
            var found = false;
            for (var j = 0; j < contactLoads.Length; j++) if (contactLoads[j].Sensor == _nodes[i].ContactLoad) found = true;
            if (!found) throw new ArgumentException("Contact-load read is missing.");
        }
        for (var i = 0; i < contactLoads.Length; i++)
        {
            var item = contactLoads[i]; var slot = -1;
            for (var j = 0; j < _nodes.Length; j++)
                if (_nodes[j].Kind == ActivationNodeKind.ContactLoadSource && _nodes[j].ContactLoad == item.Sensor) slot = j;
            if (slot < 0 || item.Frame != _nodes[slot].Owner || !Enum.IsDefined(item.Phase))
                throw new ArgumentException("Unowned contact-load read.");
            for (var j = 0; j < i; j++) if (contactLoads[j].Sensor == item.Sensor)
                throw new ArgumentException("Duplicate contact-load read.");
            var memory = _memorySlots[slot]; var hold = _nodes[slot].ReleaseHoldTicks;
            // The committed hold agrees with the committed level: due exactly while Latched, and never beyond one full hold.
            if ((values[slot].Phase == ActivationPhase.Latched) != (rows[memory].DueTick != 0) ||
                (rows[memory].DueTick != 0 && (rows[memory].DueTick < boundary.Value || rows[memory].DueTick > boundary.Value - 1 + hold)))
                throw new ArgumentException("Contact-load release hold differs from its committed level.");
            if (item.Phase != ContactLoadPhase.Loaded)
            {
                if (values[slot].Phase == ActivationPhase.Latched && boundary.Value >= rows[memory].DueTick)
                {
                    values[slot] = ActivationLatch.Clear(_nodes[slot]);
                    rows[memory] = ActivationMemory.Clear(_nodes[slot]);
                }
                continue;
            }
            rows[memory] = rows[memory] with { DueTick = checked(boundary.Value + hold) };
            if (values[slot].Phase == ActivationPhase.Latched) continue;
            var occurrence = new ActivationOccurrence(ActivationOccurrenceKind.ContactLoad, _nodes[slot].Id, end,
                new(new(item.Sensor.Value), item.Frame, item.Face, end, new((Half)0)));
            occurrence.Validate();
            values[slot] = ActivationLatch.From(_nodes[slot], occurrence);
        }
    }

    /// <summary>The memory rows belong to exactly this network's Counter and Clock nodes, in node order.</summary>
    public void ValidateMemory(PhysicsMemoryRead memory)
    {
        if (memory.Count != _memoryNodes.Length) throw new ArgumentException("Activation memory population changed.");
        for (var i = 0; i < _memoryNodes.Length; i++)
        {
            memory[i].Validate();
            if (memory[i].Node != _memoryNodes[i].Id || memory[i].Kind != _memoryNodes[i].Kind)
                throw new ArgumentException("Activation memory identity changed.");
        }
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
            if (node.Kind == ActivationNodeKind.StateLatch &&
                !_edges.ToArray().Any(edge => edge.Target == node.Id && edge.Input == WorkshopSocket.SetIn && edge.Source == occurrence.Emitter))
                throw new ArgumentException("State latch memory is not owned by a routed Set.");
            if (node.Kind == ActivationNodeKind.ContactSource &&
                (occurrence.Kind != ActivationOccurrenceKind.Contact || occurrence.Emitter != node.Id || occurrence.Cause.Source.Value != node.Trigger.Value))
                throw new ArgumentException("Physical activation does not own its trigger.");
            if (node.Kind == ActivationNodeKind.OrientationSource &&
                (occurrence.Kind != ActivationOccurrenceKind.Orientation || occurrence.Emitter != node.Id || occurrence.Cause.Source.Value != node.Sensor.Value))
                throw new ArgumentException("Orientation activation does not own its sensor.");
            if (node.Kind == ActivationNodeKind.PassageSource &&
                (occurrence.Kind != ActivationOccurrenceKind.Passage || occurrence.Emitter != node.Id || occurrence.Cause.Source.Value != node.Passage.Value))
                throw new ArgumentException("Passage activation does not own its sensor.");
            if (node.Kind == ActivationNodeKind.ContactLoadSource &&
                (occurrence.Kind != ActivationOccurrenceKind.ContactLoad || occurrence.Emitter != node.Id || occurrence.Cause.Source.Value != node.ContactLoad.Value))
                throw new ArgumentException("Contact-load level does not own its row.");
            if (node.Kind == ActivationNodeKind.Counter &&
                (occurrence.Kind != ActivationOccurrenceKind.CounterReached || occurrence.Emitter != node.Id))
                throw new ArgumentException("Counter activation does not own its emission.");
            if (node.Kind == ActivationNodeKind.Oscillator &&
                (occurrence.Kind != ActivationOccurrenceKind.OscillatorTick || occurrence.Emitter != node.Id))
                throw new ArgumentException("Oscillator activation does not own its emission.");
            if (node.Kind is ActivationNodeKind.StickyFirstInput or ActivationNodeKind.LevelIndicator)
            {
                var expected = EarliestInput(node.Id, values);
                if (expected != occurrence) throw new ArgumentException("Activation target differs from its routed source.");
            }
        }
        for (var i = 0; i < _nodes.Length; i++)
            if (_nodes[i].Kind is ActivationNodeKind.StickyFirstInput or ActivationNodeKind.LevelIndicator && values[i].Phase == ActivationPhase.Clear &&
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
        var expectedKind = occurrence.Kind switch
        {
            ActivationOccurrenceKind.Contact => ActivationNodeKind.ContactSource,
            ActivationOccurrenceKind.Orientation => ActivationNodeKind.OrientationSource,
            ActivationOccurrenceKind.Passage => ActivationNodeKind.PassageSource,
            ActivationOccurrenceKind.CounterReached => ActivationNodeKind.Counter,
            ActivationOccurrenceKind.OscillatorTick => ActivationNodeKind.Oscillator,
            ActivationOccurrenceKind.ContactLoad => ActivationNodeKind.ContactLoadSource,
            _ => ActivationNodeKind.Timer
        };
        if (emitter < 0 || _nodes[emitter].Kind != expectedKind) throw new ArgumentException("Foreign activation emitter.");
        // The document allocates every physical identity once, so one source id names at most one trigger or sensor.
        ContactTriggerDeclaration? trigger = null; OrientationSensorDeclaration? sensor = null; PassageSensorDeclaration? passage = null;
        foreach (var candidate in scene.Triggers) if (candidate.Id.Value == occurrence.Cause.Source.Value) trigger = candidate;
        foreach (var candidate in scene.OrientationSensors) if (candidate.Id.Value == occurrence.Cause.Source.Value) sensor = candidate;
        foreach (var candidate in _passageSensors) if (candidate.Id.Value == occurrence.Cause.Source.Value) passage = candidate;
        ContactLoadSensorDeclaration? load = null;
        foreach (var candidate in scene.ContactLoadSensors) if (candidate.Id.Value == occurrence.Cause.Source.Value) load = candidate;
        var isOscillatorSource = false;
        for (var i = 0; i < _nodes.Length; i++)
        {
            if (_nodes[i].Kind == ActivationNodeKind.Oscillator &&
                (_nodes[i].Collider.Value != 0 ? _nodes[i].Collider.Value : _nodes[i].Id.Value) == occurrence.Cause.Source.Value &&
                _nodes[i].Owner == occurrence.Cause.Body)
            {
                isOscillatorSource = true;
                break;
            }
        }
        GpuBodyId colliderOwner;
        if (trigger is { } contact)
        {
            if (occurrence.Kind == ActivationOccurrenceKind.Orientation || occurrence.Kind == ActivationOccurrenceKind.Passage || !contact.Targets.Contains(occurrence.Cause.Body) ||
                occurrence.Cause.ApproachSpeed.Value < contact.Threshold.Value ||
                (occurrence.Kind == ActivationOccurrenceKind.Contact && contact.Owner != _nodes[emitter].Owner))
                throw new ArgumentException("Activation cause differs from the declared contact.");
            colliderOwner = contact.Owner;
        }
        else if (sensor is { } turned)
        {
            if (occurrence.Kind == ActivationOccurrenceKind.Contact || occurrence.Kind == ActivationOccurrenceKind.Passage || turned.Body != occurrence.Cause.Body ||
                !PhysicsDeclarationBounds.Zero(occurrence.Cause.ApproachSpeed.Value) ||
                (occurrence.Kind == ActivationOccurrenceKind.Orientation && turned.Body != _nodes[emitter].Owner))
                throw new ArgumentException("Activation cause differs from the declared orientation sensor.");
            colliderOwner = turned.Body;
        }
        else if (passage is { } through)
        {
            if ((occurrence.Kind != ActivationOccurrenceKind.Passage && occurrence.Kind != ActivationOccurrenceKind.CounterReached) ||
                (occurrence.Kind == ActivationOccurrenceKind.Passage && through.Frame != _nodes[emitter].Owner))
                throw new ArgumentException("Activation cause differs from the declared passage sensor.");
            colliderOwner = occurrence.Cause.Body;
        }
        else if (isOscillatorSource)
        {
            if (occurrence.Kind == ActivationOccurrenceKind.OscillatorTick && occurrence.Cause.Body != _nodes[emitter].Owner)
                throw new ArgumentException("Activation cause differs from the declared oscillator.");
            colliderOwner = occurrence.Cause.Body;
        }
        else if (load is { } row)
        {
            // A contact-load level is the plate's own state, so its cause names the static frame and its sensing face.
            if (occurrence.Kind != ActivationOccurrenceKind.ContactLoad || row.Frame != _nodes[emitter].Owner ||
                occurrence.Cause.Body != row.Frame || occurrence.Cause.Collider != row.Face)
                throw new ArgumentException("Activation cause differs from the declared contact-load row.");
            colliderOwner = row.Frame;
        }
        else throw new ArgumentException("Activation cause names no declared source.");
        var dynamicTarget = false;
        foreach (var body in scene.Bodies)
            if (body.Id == occurrence.Cause.Body && body.Motion == RigidMotionKind.Dynamic) dynamicTarget = true;
        if (!dynamicTarget && !isOscillatorSource && load is null) throw new ArgumentException("Activation cause target is not dynamic.");
        var owned = false;
        foreach (var collider in scene.Colliders)
            if (collider.Id == occurrence.Cause.Collider && collider.Body == colliderOwner) owned = true;
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
        var passageSensors = new List<PassageSensorDeclaration>();
        foreach (var instance in construction.Instances)
        {
            if (instance is WorkshopSwitch trigger)
                nodes.Add(new(new(trigger.Id.Value), trigger.Id, ActivationNodeKind.ContactSource, WorkshopPhysicsCompiler.ContactTrigger(trigger.Id)));
            // A lamp fed by a declared level output follows that level; otherwise it holds its first pulse.
            else if (instance is WorkshopLamp lamp)
                nodes.Add(new(new(lamp.Id.Value), lamp.Id, construction.Connections.Any(link => link.Domain == WorkshopConnectionDomain.Activation &&
                    link.Target == lamp.Id && construction.Instances.Any(source => source.Id == link.Source &&
                        DeclaredPorts.Current.Signal(source.PartId, link.Output, WorkshopPortDirection.Output) == WorkshopSignalKind.Level))
                    ? ActivationNodeKind.LevelIndicator : ActivationNodeKind.StickyFirstInput, default));
            else if (instance is WorkshopGenericInstance generic && DeclaredPhysics.Of(instance.PartId).Signal?.Timer is { } timer)
                nodes.Add(new(new(instance.Id.Value), instance.Id, ActivationNodeKind.Timer, default,
                    ActivationTimerDeclaration.Duration(generic.Parameters[new ParameterId(timer.DurationParameter)], construction.Settings.Simulation)));
            // A Domino owns a node only while wired: its orientation sensor compiles under the same condition.
            else if (instance is WorkshopDomino domino && construction.Connections.HasSource(domino.Id))
                nodes.Add(new(new(domino.Id.Value), domino.Id, ActivationNodeKind.OrientationSource, default, 0, WorkshopPhysicsCompiler.OrientationSensor(domino.Id)));
            else if (instance is WorkshopBallDetector detector)
            {
                var sensorId = WorkshopPhysicsCompiler.PassageSensor(detector.Id);
                nodes.Add(new(new(detector.Id.Value), detector.Id, ActivationNodeKind.PassageSource, default, 0, default, sensorId));
                passageSensors.Add(new(sensorId, detector.Id, detector.Cell, detector.Local, detector.Rotation, new((Half)0.65), new((Half)0.02)));
            }
            else if (instance is WorkshopCounter counter)
                nodes.Add(new(new(counter.Id.Value), counter.Id, ActivationNodeKind.Counter, default, 0, default, default, (uint)counter.TargetCount));
            else if (instance is WorkshopLatch latch)
                nodes.Add(new(new(latch.Id.Value), latch.Id, ActivationNodeKind.StateLatch, default));
            // Every plate commits its own contact-load level (D3), wired or not, so its footprint is one activation node; its
            // release hold (Story 9R.5) is declared in ticks of this construction's cadence.
            else if (instance is WorkshopPressurePlate plate)
                nodes.Add(new(new(plate.Id.Value), plate.Id, ActivationNodeKind.ContactLoadSource, default,
                    ContactLoad: WorkshopPhysicsCompiler.ContactLoadSensor(plate.Id),
                    ReleaseHoldTicks: WorkshopPressurePlate.ReleaseHoldTicks(construction.Settings.Simulation)));
            else if (instance is WorkshopClock clock)
            {
                GpuBodyId powerSource = default;
                foreach (var link in construction.Connections)
                {
                    if (link.Domain == WorkshopConnectionDomain.Electrical && link.Target == clock.Id && link.Input == WorkshopSocket.PowerIn)
                    {
                        if (construction.Instances.OfType<WorkshopBattery>().Any(b => b.Id == link.Source))
                        {
                            powerSource = link.Source;
                            break;
                        }
                        var upstream = link.Source;
                        while (upstream != default)
                        {
                            var battery = construction.Instances.OfType<WorkshopBattery>().FirstOrDefault(b => b.Id == upstream);
                            if (battery.Id.Value != 0)
                            {
                                powerSource = battery.Id;
                                break;
                            }
                            var nextUpstream = construction.Connections
                                .Where(c => c.Domain == WorkshopConnectionDomain.Electrical && c.Target == upstream && c.Input == WorkshopSocket.PowerIn)
                                .Select(c => c.Source)
                                .FirstOrDefault();
                            if (nextUpstream == default || nextUpstream == upstream) break;
                            upstream = nextUpstream;
                        }
                        break;
                    }
                }
                var first = WorkshopPhysicsCompiler.PartIdentities(clock.Id);
                var colliderId = new GpuColliderId(first + 1);
                var intervalTicks = (uint)clock.Ticks(construction.Settings.Simulation);
                nodes.Add(new(new(clock.Id.Value), clock.Id, ActivationNodeKind.Oscillator, default, 0, default, default, 0, intervalTicks, powerSource, colliderId));
            }
        }
        foreach (var link in construction.Connections)
            if (link.Domain == WorkshopConnectionDomain.Activation) edges.Add(new(new(link.Source.Value), new(link.Target.Value), link.Input));
        return new(nodes.ToArray(), edges.ToArray(), new WorkshopGpuProfile(construction.Settings.Simulation,
            construction.Settings.Physical, new(1)).Substeps, passageSensors.ToArray());
    }
}
