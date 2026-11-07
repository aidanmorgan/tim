using CuriousContraptions.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Gpu;

/// <summary>Uncommitted scheduled state and due occurrences awaiting their post-physics destinations.</summary>
public sealed class ActivationSchedule : IActivationSchedule
{
    public ActivationCheckpoint State { get; }
    private readonly ActivationOccurrence[] _deferred;
    public ReadOnlySpan<ActivationOccurrence> Deferred => _deferred;
    internal ActivationSchedule(ActivationCheckpoint state, ActivationOccurrence[] deferred)
    { State = state; _deferred = deferred; }
}

/// <summary>One bounded C# discrete network. Catalogue parts declare nodes; none owns a clock or callback.</summary>
public sealed class ActivationNetwork : IActivationProgram
{
    private readonly ActivationNodeDeclaration[] _nodes;
    private readonly ActivationEdge[] _edges;
    private readonly System.Collections.Generic.Dictionary<SupplyNodeId, CompiledSignalContact> _contacts;
    private readonly ActivationTimerDeclaration[] _timers;
    private readonly PassageSensorDeclaration[] _passageSensors;
    private readonly uint _substeps;
    // Declaration data only: the memory-row index of each node (-1 for a node without committed memory).
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
    // Story 9A.22: the Physical stage's transitions, cleared at each Consume and copied into its checkpoint.
    private readonly List<ActivationTransition> _transitions = new(32);
    public uint Substeps => _substeps;
    public IEnumerable<CompiledSignalContact> Contacts => _contacts.Values.OrderBy(contact => contact.Contact.Value);
    public ReadOnlySpan<ActivationNodeDeclaration> Nodes => _nodes;
    public ReadOnlySpan<ActivationEdge> Edges => _edges;
    public ReadOnlySpan<PassageSensorDeclaration> PassageSensors => _passageSensors;

    public ActivationNetwork(ReadOnlySpan<ActivationNodeDeclaration> nodes, ReadOnlySpan<ActivationEdge> edges,
        uint substeps = 4, ReadOnlySpan<PassageSensorDeclaration> passageSensors = default,
        ReadOnlySpan<CompiledSignalContact> contacts = default)
    {
        _ = ActivationTime.Boundary(new(0), substeps); _substeps = substeps;
        // Owner decision, 20 Oct 2026: no activation-slot limit, and (Story 9R.5b) no instance or connection bound.
        _nodes = nodes.ToArray(); _edges = edges.ToArray();
        _contacts = contacts.ToArray().ToDictionary(contact => contact.Contact);
        ValidateContactBindings(contacts.Length);
        _passageSensors = passageSensors.ToArray();
        Array.Sort(_nodes, (a,b) => a.Id.Value.CompareTo(b.Id.Value));
        _memorySlots = new int[_nodes.Length];
        var memoryNodes = new List<ActivationNodeDeclaration>(16);
        for (var i = 0; i < _nodes.Length; i++)
        {
            _memorySlots[i] = ActivationMemory.Holds(_nodes[i].Kind) ? memoryNodes.Count : -1;
            if (_memorySlots[i] >= 0) memoryNodes.Add(_nodes[i]);
        }
        _memoryNodes = memoryNodes.ToArray();
        _timers = ValidateNodeDeclarations();
        _scratchValues = new ActivationLatch[_nodes.Length];
        _scratchTimers = new ActivationTimerState[_timers.Length];
        _scratchRows = new ActivationMemory[_memoryNodes.Length];
        _latchRequests = new StateLatchRequest[_nodes.Length];
        _latchSetBy = new ActivationOccurrence[_nodes.Length];
        _pending = new ActivationOccurrence[32];
        ValidateEdges();
        Array.Sort(_edges, (a,b) => a.Source != b.Source ? a.Source.Value.CompareTo(b.Source.Value) :
            a.Target != b.Target ? a.Target.Value.CompareTo(b.Target.Value) : a.Input.CompareTo(b.Input));
    }
    private void ValidateContactBindings(int count)
    {
        if (_contacts.Count != count || _contacts.Values.Any(contact => contact.Contact.Value == 0 || !Enum.IsDefined(contact.State) || !_nodes.Any(node => node.Id == contact.Node && node.Owner == contact.Owner &&
                (contact.State == SignalContactState.TimerCounting ? node.Kind == ActivationNodeKind.Timer :
                    node.Kind is ActivationNodeKind.ContactSource or ActivationNodeKind.Counter or ActivationNodeKind.StateLatch or ActivationNodeKind.ContactLoadSource or ActivationNodeKind.OrientationSource or ActivationNodeKind.PassageSource))))
            throw new ArgumentException("Declared contact must own an admitted activation state.");
    }

    private ActivationTimerDeclaration[] ValidateNodeDeclarations()
    {
        var triggers = new HashSet<GpuContactTriggerId>(4);
        var sensors = new HashSet<GpuOrientationSensorId>(4);
        var passages = new HashSet<GpuPassageSensorId>(4);
        var loads = new HashSet<GpuContactLoadSensorId>(4);
        var timers = new List<ActivationTimerDeclaration>(4);
        for (var i = 0; i < _nodes.Length; i++)
        {
            var node = _nodes[i];
            if (node.Id.Value == 0 || node.Owner.Value == 0 || !Enum.IsDefined(node.Kind) ||
                (i != 0 && _nodes[i - 1].Id == node.Id) ||
                InvalidSensorIdentity(node, triggers, sensors, passages, loads) || InvalidNodeState(node))
                throw new ArgumentException("Invalid activation node declaration.");
            if (node.Kind == ActivationNodeKind.Timer)
            {
                var timer = new ActivationTimerDeclaration(node.Id, node.DurationTicks, node.Completion, node.Boundary, node.ElapsedSignal);
                timer.Validate();
                timers.Add(timer);
            }
        }
        foreach (var passage in _passageSensors)
        {
            passage.Validate();
            if (!passages.Contains(passage.Id)) throw new ArgumentException("Passage sensor has no owning node.");
        }
        return timers.ToArray();
    }

    private void ValidateEdges()
    {
        var links = new HashSet<ActivationEdge>(16);
        foreach (var edge in _edges)
            if (edge.Source == edge.Target || !links.Add(edge) || Slot(edge.Source) < 0 || Slot(edge.Target) < 0 ||
                _nodes[Slot(edge.Source)].Kind is ActivationNodeKind.StickyFirstInput or ActivationNodeKind.LevelIndicator ||
                // A level source drives level indicators only, and a level indicator listens to a level source only.
                IsLevelSource(_nodes[Slot(edge.Source)].Kind) != (_nodes[Slot(edge.Target)].Kind == ActivationNodeKind.LevelIndicator) ||
                _nodes[Slot(edge.Target)].Kind is ActivationNodeKind.ContactSource or ActivationNodeKind.OrientationSource or ActivationNodeKind.PassageSource or ActivationNodeKind.Oscillator or ActivationNodeKind.ContactLoadSource ||
                (_nodes[Slot(edge.Target)].Kind == ActivationNodeKind.StateLatch
                    ? edge.Input is not (PortRole.SetIn or PortRole.ResetIn)
                    : edge.Input != PortRole.ActivationIn))
                throw new ArgumentException("Unsupported activation edge.");
        foreach (var node in _nodes)
            if (node.Kind == ActivationNodeKind.LevelIndicator && _edges.Count(edge => edge.Target == node.Id) != 1)
                throw new ArgumentException("A level indicator follows exactly one level source.");
        SignalTopology.Validate(_nodes.Select((node, slot) => new SignalTopologyNode(slot,
            node.Kind == ActivationNodeKind.Timer && node.DurationTicks > 0)).ToArray(),
            _edges.Select(edge => new SignalTopologyEdge(Slot(edge.Source), Slot(edge.Target),
                edge.Input switch
                {
                    PortRole.ActivationIn => SignalTerminal.Input,
                    PortRole.SetIn => SignalTerminal.Set,
                    PortRole.ResetIn => SignalTerminal.Reset,
                    _ => throw new ArgumentException("Unsupported terminal.")
                }, IsLevelSource(_nodes[Slot(edge.Source)].Kind))).ToArray());
    }

    private static bool InvalidSensorIdentity(ActivationNodeDeclaration node, HashSet<GpuContactTriggerId> triggers,
        HashSet<GpuOrientationSensorId> sensors, HashSet<GpuPassageSensorId> passages, HashSet<GpuContactLoadSensorId> loads) =>
        (node.Kind == ActivationNodeKind.ContactSource ? node.Trigger.Value == 0 || !triggers.Add(node.Trigger) : node.Trigger.Value != 0) ||
                (node.Kind == ActivationNodeKind.OrientationSource ? node.Sensor.Value == 0 || !sensors.Add(node.Sensor) : node.Sensor.Value != 0) ||
                (node.Kind == ActivationNodeKind.PassageSource ? node.Passage.Value == 0 || !passages.Add(node.Passage) : node.Passage.Value != 0) ||
                (node.Kind == ActivationNodeKind.ContactLoadSource ? node.ContactLoad.Value == 0 || !loads.Add(node.ContactLoad) : node.ContactLoad.Value != 0);

    private static bool InvalidNodeState(ActivationNodeDeclaration node) =>
        (node.Kind == ActivationNodeKind.ContactLoadSource ? node.ReleaseHoldTicks == 0 : node.ReleaseHoldTicks != 0) ||
                (node.Kind == ActivationNodeKind.Timer ? node.DurationTicks == 0 : node.DurationTicks != 0) ||
                (node.Kind == ActivationNodeKind.Counter ? node.TargetCount < 1 : node.TargetCount != 0) ||
                (node.Kind == ActivationNodeKind.Oscillator ? node.IntervalTicks == 0 : (node.IntervalTicks != 0 || node.PowerProvider.Value != 0));

    public static SignalNodeKind Operation(ActivationNodeKind kind) => kind switch
    {
        ActivationNodeKind.ContactSource => SignalNodeKind.Contact,
        ActivationNodeKind.OrientationSource => SignalNodeKind.Orientation,
        ActivationNodeKind.PassageSource => SignalNodeKind.Passage,
        ActivationNodeKind.ContactLoadSource => SignalNodeKind.ContactLoad,
        ActivationNodeKind.Counter => SignalNodeKind.PulseCount,
        ActivationNodeKind.StateLatch => SignalNodeKind.Memory,
        ActivationNodeKind.Oscillator => SignalNodeKind.Oscillator,
        ActivationNodeKind.Timer => SignalNodeKind.Timer,
        ActivationNodeKind.LevelIndicator or ActivationNodeKind.StickyFirstInput => SignalNodeKind.Indicator,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public void ValidateCommittedMemory(PhysicsMemoryRead memory, SimulationTick tick, IPhysicsSceneDeclaration scene)
    {
        ValidateMemory(memory);
        var end = ActivationTime.Boundary(tick, _substeps);
        for (var i = 0; i < memory.Count; i++)
        {
            if (memory[i].Last > end) throw new ArgumentException("Future counted input.");
            if (memory[i].Dispatched != default) ValidateOccurrence(memory[i].Dispatched, end, scene);
        }
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
    /// <summary>The admitted memory rows: every Counter at count 0 with no counted input, every oscillator Stopped with no due tick, every passage cursor empty.</summary>
    public PhysicsMemoryRead ClearMemory()
    {
        var values = new ActivationMemory[_memoryNodes.Length];
        for (var i = 0; i < _memoryNodes.Length; i++) values[i] = ActivationMemory.Clear(_memoryNodes[i]);
        return new(values);
    }

    /// <summary>Queues one pending occurrence, growing the queue when a tick's fanout exceeds it (no fanout capacity).</summary>
    private void Push(ref int count, ActivationOccurrence occurrence)
    {
        var required = (long)count + 1;
        if (required > Array.MaxLength) throw new WorkshopStorageException("Activation queue storage is not representable.");
        if (required > 3L * _pending.Length / 4)
        {
            var capacity = (int)Math.Min(Array.MaxLength, Math.Max(2L * _pending.Length, (4L * required + 2) / 3));
            if (capacity != _pending.Length) Array.Resize(ref _pending, capacity);
        }
        _pending[count++] = occurrence;
    }
    /// <summary>The committed memory row selected by exact node ID (counter, oscillator or passage cursor; the network holds none).</summary>
    public ActivationMemory Memory(PhysicsMemoryRead values, ActivationNodeId node)
    {
        ValidateMemory(values);
        for (var i = 0; i < _memoryNodes.Length; i++)
            if (_memoryNodes[i].Id == node) return values[i];
        throw new ArgumentException("Memory row not found for node.");
    }
    /// <summary>The authored count target selected by exact counter node ID.</summary>
    public uint GetCounterTarget(ActivationNodeId node)
    {
        for (var i = 0; i < _nodes.Length; i++)
            if (_nodes[i].Id == node && _nodes[i].Kind == ActivationNodeKind.Counter)
                return _nodes[i].TargetCount;
        throw new ArgumentException("Counter not found for node.");
    }
    /// <summary>The exact memory node's committed state: On is Latched with the Set occurrence, Off is Clear.</summary>
    public SimulationLatchPhase StateLatchPhase(PhysicsActivationRead values, ActivationNodeId node)
    {
        ValidatePopulation(values, ClearTimers());
        for (var i = 0; i < _nodes.Length; i++)
            if (_nodes[i].Id == node && _nodes[i].Kind == ActivationNodeKind.StateLatch)
                return values[i].Phase == ActivationPhase.Latched ? SimulationLatchPhase.On : SimulationLatchPhase.Off;
        throw new ArgumentException("State latch not found for node.");
    }
    /// <summary>A switched contact (State Latch, Counter or contact-load level) is closed exactly while its committed value is Latched.</summary>
    public bool ContactClosed(PhysicsActivationRead values, PhysicsTimerRead timers, SupplyNodeId id)
    {
        ValidatePopulation(values, timers);
        if (!_contacts.TryGetValue(id, out var contact)) throw new ArgumentException("Owner has no declared supplied contact.");
        return contact.State == SignalContactState.TimerCounting
            ? timers[TimerSlot(contact.Node)].Phase == ActivationTimerPhase.Counting
            : values[Slot(contact.Node)].Phase == ActivationPhase.Latched;
    }
    /// <summary>Evaluates one candidate tick from the committed checkpoint. The committed memory rows are read, never written: the
    /// candidate's rows are returned in the checkpoint, so only a committed candidate changes the published Counter and oscillator state.
    /// A network with Counter or oscillator nodes requires their committed rows (an empty population is rejected).</summary>
    public ActivationCheckpoint Consume(PhysicsActivationRead committed, PhysicsTimerRead committedTimers,
        ReadOnlySpan<ContactTriggerRead> events, SimulationTick boundary, ReadOnlySpan<OrientationSensorRead> orientations,
        ReadOnlySpan<PassageSensorRead> passages = default, SupplyOutputRead supply = default,
        PhysicsMemoryRead memory = default, ReadOnlySpan<ContactLoadSensorRead> contactLoads = default) =>
        Consume(committed, committedTimers, events, boundary, orientations, passages, supply, memory, contactLoads, EvaluationStage.Physical, [], out _);

    private enum EvaluationStage { Scheduled, Physical }

    /// <summary>Stages already-due timer transitions and deliveries before the electrical/physical solve. The input checkpoint is immutable.</summary>
    public IActivationSchedule AdvanceScheduled(ActivationCheckpoint committed, SimulationTick boundary)
    {
        var state = Consume(committed.Activations, committed.Timers, [], boundary, [], [], default, committed.Memory, [],
            EvaluationStage.Scheduled, [], out var deferred);
        return new ActivationSchedule(state, deferred);
    }

    public ActivationCheckpoint ConsumeScheduled(IActivationSchedule scheduled, ReadOnlySpan<ContactTriggerRead> events,
        SimulationTick boundary, ReadOnlySpan<OrientationSensorRead> orientations, ReadOnlySpan<PassageSensorRead> passages,
        SupplyOutputRead supply, ReadOnlySpan<ContactLoadSensorRead> contactLoads) =>
        Consume(scheduled.State.Activations, scheduled.State.Timers, events, boundary, orientations, passages, supply,
            scheduled.State.Memory, contactLoads, EvaluationStage.Physical, scheduled.Deferred, out _);

    private ActivationCheckpoint Consume(PhysicsActivationRead committed, PhysicsTimerRead committedTimers,
        ReadOnlySpan<ContactTriggerRead> events, SimulationTick boundary, ReadOnlySpan<OrientationSensorRead> orientations,
        ReadOnlySpan<PassageSensorRead> passages, SupplyOutputRead supply,
        PhysicsMemoryRead memory, ReadOnlySpan<ContactLoadSensorRead> contactLoads, EvaluationStage stage,
        ReadOnlySpan<ActivationOccurrence> scheduledOccurrences, out ActivationOccurrence[] deferredOccurrences)
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
        _transitions.Clear();
        // Requests routed to each State Latch within this tick; they resolve once at the boundary below.
        Span<StateLatchRequest> latchRequests = _latchRequests; latchRequests.Clear();
        Span<ActivationOccurrence> latchSetBy = _latchSetBy; latchSetBy.Clear();
        var count = 0;
        var deferred = stage == EvaluationStage.Scheduled ? new List<ActivationOccurrence>(32) : null;
        foreach (var occurrence in scheduledOccurrences) Push(ref count, occurrence);
        QueueContacts(events, values, end, ref count);
        QueueOrientations(orientations, values, end, stage, ref count);
        QueuePassages(passages, rows, end, ref count);
        if (stage == EvaluationStage.Physical) CommitContactLoads(values, rows, contactLoads, boundary, end);
        QueueTimedOccurrences(timers, rows, supply, boundary, stage, ref count);
        RouteOccurrences(values, rows, timers, latchRequests, latchSetBy, boundary, stage, scheduledOccurrences, deferred, ref count);
        // Every request routed this tick resolves at the boundary: Reset dominates, otherwise Set, otherwise memory holds.
        for (var i = 0; i < _nodes.Length; i++)
        {
            if (_nodes[i].Kind != ActivationNodeKind.StateLatch) continue;
            var current = values[i].Phase == ActivationPhase.Latched ? SimulationLatchPhase.On : SimulationLatchPhase.Off;
            var next = (latchRequests[i] & StateLatchRequest.Reset) != 0 ? SimulationLatchPhase.Off :
                (latchRequests[i] & StateLatchRequest.Set) != 0 ? SimulationLatchPhase.On : current;
            if (next == current) continue;
            values[i] = next == SimulationLatchPhase.On ? ActivationLatch.From(_nodes[i], latchSetBy[i]) : ActivationLatch.Clear(_nodes[i]);
            _transitions.Add(new(ActivationTransitionKind.LevelChanged, _nodes[i].Id, values[i].Phase == ActivationPhase.Latched ? latchSetBy[i] : default,
                0, values[i].Phase));
        }
        // A level ActivationOut (latch memory, a held contact load): each level indicator mirrors its source's settled value on the same boundary.
        foreach (var edge in _edges)
        {
            var source = Slot(edge.Source);
            if (!IsLevelSource(_nodes[source].Kind)) continue;
            var target = Slot(edge.Target);            values[target] = values[source].Phase == ActivationPhase.Latched
                ? ActivationLatch.From(_nodes[target], values[source].Occurrence) : ActivationLatch.Clear(_nodes[target]);
        }
        deferredOccurrences = deferred?.ToArray() ?? [];
        return new(new(values[.._nodes.Length]), new(timers[.._timers.Length]), new(rows[.._memoryNodes.Length]),
            new(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_transitions)));
    }

    private void QueueContacts(ReadOnlySpan<ContactTriggerRead> events, Span<ActivationLatch> values, ActivationTime end, ref int count)
    {
        for (var i = 0; i < events.Length; i++)
        {
            var item = events[i]; var slot = -1;
            for (var j = 0; j < _nodes.Length; j++)
                if (_nodes[j].Kind == ActivationNodeKind.ContactSource && _nodes[j].Trigger == item.Id) slot = j;
            if (slot < 0 || item.OccurrenceCount > 1)
                throw new ArgumentException("Unowned contact occurrence.");
            for (var j = 0; j < i; j++) if (events[j].Id == item.Id)
                throw new ArgumentException("Duplicate contact occurrence batch.");
            if (item.OccurrenceCount == 0) continue;
            var time = new ActivationTime(item.EventOrdinal, item.EventPhase);
            var occurrence = new ActivationOccurrence(ActivationOccurrenceKind.Contact, _nodes[slot].Id, time,
                new(new(item.Id.Value), item.Target, item.Collider, time, item.ApproachSpeed, ActivationCauseKind.Physical));
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
    }

    private void QueueOrientations(ReadOnlySpan<OrientationSensorRead> orientations, Span<ActivationLatch> values, ActivationTime end, EvaluationStage stage, ref int count)
    {
        // Every orientation source must be read each tick: a missing read would silently leave its owner unlatched.
        for (var i = 0; i < _nodes.Length; i++)
        {
            if (stage != EvaluationStage.Physical || _nodes[i].Kind != ActivationNodeKind.OrientationSource) continue;
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
            if (slot < 0 || !Enum.IsDefined(item.Phase))
                throw new ArgumentException("Unowned orientation occurrence.");
            for (var j = 0; j < i; j++) if (orientations[j].Id == item.Id)
                throw new ArgumentException("Duplicate orientation occurrence batch.");
            if (item.Phase == OrientationSensorPhase.Armed)
            {
                if (values[slot].Phase == ActivationPhase.Latched) throw new ArgumentException("Orientation sensor regressed.");
                continue;
            }
            var time = new ActivationTime(item.EventOrdinal, item.EventPhase);
            var occurrence = new ActivationOccurrence(ActivationOccurrenceKind.Orientation, _nodes[slot].Id, time,
                new(new(item.Id.Value), item.Body, item.Collider, time, new(0f), ActivationCauseKind.Physical));
            occurrence.Validate();
            if (time.CompareTo(end) > 0) throw new ArgumentException("Future orientation occurrence.");
            if (values[slot].Phase == ActivationPhase.Latched)
            {
                if (values[slot].Occurrence != occurrence) throw new ArgumentException("Changed orientation occurrence.");
                continue;
            }
            Push(ref count, occurrence);
        }
    }

    private void QueuePassages(ReadOnlySpan<PassageSensorRead> passages, Span<ActivationMemory> rows, ActivationTime end, ref int count)
    {
        for (var i = 0; i < passages.Length; i++)
        {
            var item = passages[i]; var slot = -1;
            for (var j = 0; j < _nodes.Length; j++)
                if (_nodes[j].Kind == ActivationNodeKind.PassageSource && _nodes[j].Passage == item.Id) slot = j;
            if (slot < 0 || !Enum.IsDefined(item.Phase))
                throw new ArgumentException("Unowned passage occurrence.");
            if (item.Phase != PassageSensorPhase.Fired || item.Sequence.Value == 0 ||
                !_passageSensors.Any(sensor => sensor.Id == item.Id && sensor.Frame == item.Frame))
                throw new ArgumentException("Invalid passage occurrence identity.");
            var time = new ActivationTime(item.EventOrdinal, item.EventPhase);
            var occurrence = new ActivationOccurrence(ActivationOccurrenceKind.Passage, _nodes[slot].Id, time,
                new(new(item.Id.Value), item.Body, item.Collider, time, new(item.Speed.Value), ActivationCauseKind.Physical));
            occurrence.Validate();
            if (time.CompareTo(end) > 0) throw new ArgumentException("Future passage occurrence.");
            var memorySlot = _memorySlots[slot];
            var prior = rows[memorySlot];
            if (item.Sequence == prior.PassageSequence)
            {
                if (occurrence != prior.Dispatched) throw new ArgumentException("Changed passage replay.");
                continue;
            }
            if (prior.PassageSequence.Value == ulong.MaxValue || item.Sequence.Value != prior.PassageSequence.Value + 1 ||
                (prior.Dispatched != default && (time < prior.Dispatched.Time || occurrence == prior.Dispatched)))
                throw new ArgumentException("Stale, gapped or duplicate passage occurrence.");
            rows[memorySlot] = prior with { Dispatched = occurrence, PassageSequence = item.Sequence };
            Push(ref count, occurrence);
        }
    }

    private void QueueTimedOccurrences(Span<ActivationTimerState> timers, Span<ActivationMemory> rows, SupplyOutputRead supply, SimulationTick boundary, EvaluationStage stage, ref int count)
    {
        for (var pass = 0; pass < 2; pass++)
            for (var i = 0; i < _timers.Length; i++)
            {
                if (_timers[i].Boundary != (pass == 0 ? TimerBoundary.BeforeNetworks : TimerBoundary.BeforePhysics)) continue;
                timers[i] = timers[i].Complete(_timers[i], boundary, _substeps, out var elapsed);
                if (elapsed is { } occurrence) Push(ref count, occurrence);
            }
        for (var i = 0; i < _nodes.Length; i++)
        {
            if (stage != EvaluationStage.Physical || _nodes[i].Kind != ActivationNodeKind.Oscillator) continue;
            var node = _nodes[i];
            var isPowered = supply.IsPowered(node.PowerProvider);

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
                var cause = new ActivationCause(new(node.Id.Value), node.Owner, default, time, new(0f), ActivationCauseKind.Node);
                var occurrence = new ActivationOccurrence(ActivationOccurrenceKind.OscillatorTick, node.Id, time, cause);
                occurrence.Validate();
                Push(ref count, occurrence);
            }
        }
    }

    private void RouteOccurrences(Span<ActivationLatch> values, Span<ActivationMemory> rows, Span<ActivationTimerState> timers, Span<StateLatchRequest> latchRequests, Span<ActivationOccurrence> latchSetBy, SimulationTick boundary, EvaluationStage stage, ReadOnlySpan<ActivationOccurrence> scheduledOccurrences, List<ActivationOccurrence>? deferred, ref int count)
    {
        // Passage batches have already passed sequence/replay validation and may contain simultaneous causes.
        // Positive integer durations prevent instantaneous timer cycles.
        while (count != 0)
        {
            var first = 0;
            for (var i = 1; i < count; i++) if (Compare(_pending[i], _pending[first]) < 0) first = i;
            var occurrence = _pending[first]; _pending[first] = _pending[--count];
            var source = Slot(occurrence.Emitter);
            if (stage == EvaluationStage.Physical && values[source].Phase != ActivationPhase.Clear)
            {
                if (_nodes[source].Kind == ActivationNodeKind.PassageSource &&
                    occurrence.Kind == ActivationOccurrenceKind.Passage)
                {
                }
                else if (occurrence.Kind == ActivationOccurrenceKind.ThresholdReached && values[source].Phase == ActivationPhase.Latched)
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
            if (stage == EvaluationStage.Physical && values[source].Phase == ActivationPhase.Clear)
                values[source] = ActivationLatch.From(_nodes[source], occurrence);
            // Only the Physical stage emits: a scheduled occurrence is deferred, replayed here and emitted exactly once.
            if (stage == EvaluationStage.Physical)
                _transitions.Add(new(ActivationTransitionKind.Emitted, occurrence.Emitter, occurrence,
                    _memorySlots[source] >= 0 ? rows[_memorySlots[source]].Count : 0, ActivationPhase.Latched));
            deferred?.Add(occurrence);
            foreach (var edge in _edges)
            {
                if (edge.Source != occurrence.Emitter) continue;
                var target = Slot(edge.Target);
                var earlyDestination = _nodes[target].Kind == ActivationNodeKind.Timer && _nodes[target].Boundary == TimerBoundary.BeforeNetworks;
                if (stage == EvaluationStage.Scheduled && !earlyDestination) continue;
                if (stage == EvaluationStage.Physical && earlyDestination && scheduledOccurrences.Contains(occurrence)) continue;
                RouteInput(target, edge, occurrence, values, rows, timers, latchRequests, latchSetBy, boundary, ref count);
            }
        }
    }

    private void RouteInput(int target, ActivationEdge edge, ActivationOccurrence occurrence,
        Span<ActivationLatch> values, Span<ActivationMemory> rows, Span<ActivationTimerState> timers,
        Span<StateLatchRequest> latchRequests, Span<ActivationOccurrence> latchSetBy, SimulationTick boundary, ref int count)
    {
        if (_nodes[target].Kind == ActivationNodeKind.StickyFirstInput)
        {
            if (values[target].Phase == ActivationPhase.Clear) values[target] = ActivationLatch.From(_nodes[target], occurrence);
        }
        else if (_nodes[target].Kind == ActivationNodeKind.StateLatch)
        {
            if (edge.Input == PortRole.ResetIn) latchRequests[target] |= StateLatchRequest.Reset;
            else if ((latchRequests[target] & StateLatchRequest.Set) == 0 || Compare(occurrence, latchSetBy[target]) < 0)
            { latchRequests[target] |= StateLatchRequest.Set; latchSetBy[target] = occurrence; }
        }
        else if (_nodes[target].Kind == ActivationNodeKind.Counter)
        {
            var slot = _memorySlots[target];
            var transition = rows[slot].CountInput(_nodes[target].TargetCount, occurrence.Time);
            rows[slot] = transition.Memory;
            if (transition.Crossed && values[target].Phase == ActivationPhase.Clear)
            {
                var reached = new ActivationOccurrence(ActivationOccurrenceKind.ThresholdReached, _nodes[target].Id, occurrence.Time, occurrence.Cause);
                values[target] = ActivationLatch.From(_nodes[target], reached);
                Push(ref count, reached);
            }
        }
        else
        {
            var index = TimerSlot(edge.Target);
            timers[index] = timers[index].Trigger(_timers[index], occurrence, _substeps);
            timers[index] = timers[index].Complete(_timers[index], boundary, _substeps, out var elapsed);
            if (elapsed is { } due)
            {
                Push(ref count, due);
            }
        }
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
            if (slot < 0 || !Enum.IsDefined(item.Phase))
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
                    _transitions.Add(new(ActivationTransitionKind.LevelChanged, _nodes[slot].Id, default, 0, ActivationPhase.Clear));
                }
                continue;
            }
            rows[memory] = rows[memory] with { DueTick = checked(boundary.Value + hold) };
            if (values[slot].Phase == ActivationPhase.Latched) continue;
            var occurrence = new ActivationOccurrence(ActivationOccurrenceKind.ContactLoad, _nodes[slot].Id, end,
                new(new(item.Sensor.Value), item.Frame, item.Region, end, new(0f), ActivationCauseKind.Physical));
            occurrence.Validate();
            values[slot] = ActivationLatch.From(_nodes[slot], occurrence);
            _transitions.Add(new(ActivationTransitionKind.LevelChanged, _nodes[slot].Id, occurrence, 0, ActivationPhase.Latched));
        }
    }

    /// <summary>The memory rows belong to exactly this network's counter, oscillator and passage nodes, in node order.</summary>
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

    public void ValidateOrigins(PhysicsActivationRead values, PhysicsTimerRead timers, SimulationTick tick, IPhysicsSceneDeclaration scene)
    {
        ValidatePopulation(values, timers);
        var end = ActivationTime.Boundary(tick, _substeps);
        for (var i = 0; i < values.Count; i++)
            if (values[i].Phase == ActivationPhase.Latched) ValidateOccurrence(values[i].Occurrence, end, scene);
        for (var i = 0; i < timers.Count; i++)
            if (timers[i].Phase != ActivationTimerPhase.Ready) ValidateOccurrence(timers[i].Input, end, scene);
    }

    public void ValidateRead(PhysicsActivationRead values, PhysicsTimerRead timers, SimulationTick tick,
        uint substeps, IPhysicsSceneDeclaration scene)
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
            ValidateLatchedActivation(node, occurrence, values);
        }
        for (var i = 0; i < _nodes.Length; i++)
            if (_nodes[i].Kind is ActivationNodeKind.StickyFirstInput or ActivationNodeKind.LevelIndicator && values[i].Phase == ActivationPhase.Clear &&
                EarliestInput(_nodes[i].Id, values) is not null)
                throw new ArgumentException("Activation output was lost.");

        for (var i = 0; i < _timers.Length; i++)
            ValidateTimerRead(timers[i], _timers[i], values, tick, substeps, end, scene);
    }
    private static void ValidatePhysicalActivation(ActivationNodeDeclaration node, ActivationOccurrence occurrence)
    {
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
    }

    private void ValidateLatchedActivation(ActivationNodeDeclaration node, ActivationOccurrence occurrence, PhysicsActivationRead values)
    {
        if (node.Kind == ActivationNodeKind.StateLatch &&
            !_edges.ToArray().Any(edge => edge.Target == node.Id && edge.Input == PortRole.SetIn && edge.Source == occurrence.Emitter))
            throw new ArgumentException("State latch memory is not owned by a routed Set.");
        ValidatePhysicalActivation(node, occurrence);
        if (node.Kind == ActivationNodeKind.Counter &&
            (occurrence.Kind != ActivationOccurrenceKind.ThresholdReached || occurrence.Emitter != node.Id))
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

    private void ValidateRearmingTimer(ActivationTimerState state, ActivationTimerDeclaration declared,
        PhysicsActivationRead values, SimulationTick tick, uint substeps, ActivationTime end, IPhysicsSceneDeclaration scene)
    {
        var slot = Slot(state.Node);

        if (values[slot] != ActivationLatch.Clear(_nodes[slot]))
            throw new ArgumentException("Silent timer retained an output.");
        if (state.Phase == ActivationTimerPhase.Ready) return;
        ValidateOccurrence(state.Input, end, scene);
        var connected = false;
        foreach (var edge in _edges)
            if (edge.Target == state.Node && edge.Source == state.Input.Emitter) connected = true;
        var origin = state.Input.Time.CeilingTick(substeps);
        if (!connected || state.Phase != ActivationTimerPhase.Counting || state.StartedTick != origin ||
            state.DueTick != checked(origin + declared.DurationTicks) || state.DueTick <= tick.Value)
            throw new ArgumentException("Rearming timer read differs from its declaration or deadline.");
    }

    private void ValidateTimerRead(ActivationTimerState state, ActivationTimerDeclaration declared,
        PhysicsActivationRead values, SimulationTick tick, uint substeps, ActivationTime end, IPhysicsSceneDeclaration scene)
    {
        var slot = Slot(state.Node);
        if (declared.Completion == TimerCompletionPolicy.Rearm)
        {
            ValidateRearmingTimer(state, declared, values, tick, substeps, end, scene);
            return;
        }
        var expected = EarliestInput(state.Node, values);
        if (expected is null)
        {
            if (state != ActivationTimerState.Ready(state.Node) || values[slot].Phase != ActivationPhase.Clear)
                throw new ArgumentException("Disconnected timer retained state.");
            return;
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

    private void ValidateOccurrence(ActivationOccurrence occurrence, ActivationTime end, IPhysicsSceneDeclaration scene)
    {
        occurrence.Validate();
        if (occurrence.Time.CompareTo(end) > 0) throw new ArgumentException("Future activation read.");
        var emitter = Slot(occurrence.Emitter);
        var expectedKind = occurrence.Kind switch
        {
            ActivationOccurrenceKind.Contact => ActivationNodeKind.ContactSource,
            ActivationOccurrenceKind.Orientation => ActivationNodeKind.OrientationSource,
            ActivationOccurrenceKind.Passage => ActivationNodeKind.PassageSource,
            ActivationOccurrenceKind.ThresholdReached => ActivationNodeKind.Counter,
            ActivationOccurrenceKind.OscillatorTick => ActivationNodeKind.Oscillator,
            ActivationOccurrenceKind.ContactLoad => ActivationNodeKind.ContactLoadSource,
            _ => ActivationNodeKind.Timer
        };
        if (emitter < 0 || _nodes[emitter].Kind != expectedKind) throw new ArgumentException("Foreign activation emitter.");
        if (occurrence.Cause.Kind == ActivationCauseKind.Node)
        {
            ValidateNodeCause(occurrence, emitter);
            return;
        }
        if (occurrence.Kind == ActivationOccurrenceKind.OscillatorTick)
            throw new ArgumentException("Scheduled node output has physical provenance.");
        var (colliderOwner, contactLoad) = ResolvePhysicalCause(occurrence, emitter, scene);
        var dynamicTarget = false;
        foreach (var body in scene.Bodies)
            if (body.Id == occurrence.Cause.Body && body.Motion == RigidMotionKind.Dynamic) dynamicTarget = true;
        if (!dynamicTarget && !contactLoad) throw new ArgumentException("Activation cause target is not dynamic.");
        var owned = false;
        foreach (var collider in scene.Colliders)
            if (collider.Id == occurrence.Cause.Collider && collider.Body == colliderOwner) owned = true;
        if (!owned) throw new ArgumentException("Activation cause collider is unowned.");
    }
    private void ValidateNodeCause(ActivationOccurrence occurrence, int emitter)
    {
        var origin = Slot(new(occurrence.Cause.Source.Value));
        if (origin < 0 || _nodes[origin].Kind != ActivationNodeKind.Oscillator ||
            _nodes[origin].Owner != occurrence.Cause.Body ||
            occurrence.Cause.Time.Ordinal % _substeps != 0 || occurrence.Cause.Time.Phase != 0 ||
            occurrence.Kind is not (ActivationOccurrenceKind.OscillatorTick or ActivationOccurrenceKind.ThresholdReached or ActivationOccurrenceKind.TimerElapsed) ||
            (occurrence.Kind == ActivationOccurrenceKind.OscillatorTick && origin != emitter))
            throw new ArgumentException("Activation cause names no originating node.");
    }

    private GpuBodyId ValidateContactCause(ActivationOccurrence occurrence, int emitter, ContactTriggerDeclaration contact, IPhysicsSceneDeclaration scene)
    {
        var admittedTarget=false;
        foreach(var body in scene.Bodies)
            if(body.Id==occurrence.Cause.Body && body.Motion==RigidMotionKind.Dynamic && contact.Targets.Contains(body.Id,body.Payload)) admittedTarget=true;
        if (occurrence.Kind == ActivationOccurrenceKind.Orientation || occurrence.Kind == ActivationOccurrenceKind.Passage || !admittedTarget ||
            occurrence.Cause.ApproachSpeed.Value < contact.Threshold.Value ||
            (occurrence.Kind == ActivationOccurrenceKind.Contact && contact.Id != _nodes[emitter].Trigger))
            throw new ArgumentException("Activation cause differs from the declared contact.");
        return contact.Owner;
    }

    private GpuBodyId ValidateOrientationCause(ActivationOccurrence occurrence, int emitter, OrientationSensorDeclaration turned)
    {
        if (occurrence.Kind == ActivationOccurrenceKind.Contact || occurrence.Kind == ActivationOccurrenceKind.Passage || turned.Body != occurrence.Cause.Body ||
            occurrence.Cause.ApproachSpeed.Value != 0f ||
            (occurrence.Kind == ActivationOccurrenceKind.Orientation && turned.Id != _nodes[emitter].Sensor))
            throw new ArgumentException("Activation cause differs from the declared orientation sensor.");
        return turned.Body;
    }

    private GpuBodyId ValidatePassageCause(ActivationOccurrence occurrence, int emitter, PassageSensorDeclaration through)
    {
        if (occurrence.Kind is not (ActivationOccurrenceKind.Passage or ActivationOccurrenceKind.ThresholdReached or ActivationOccurrenceKind.TimerElapsed) ||
            (occurrence.Kind == ActivationOccurrenceKind.Passage && through.Id != _nodes[emitter].Passage))
            throw new ArgumentException("Activation cause differs from the declared passage sensor.");
        return occurrence.Cause.Body;
    }

    private GpuBodyId ValidateLoadCause(ActivationOccurrence occurrence, int emitter, ContactLoadSensorDeclaration row)
    {
        // A contact-load level is its owner's own state, so its cause names the static frame and its sensing face.
        if (occurrence.Kind != ActivationOccurrenceKind.ContactLoad || row.Id != _nodes[emitter].ContactLoad ||
            occurrence.Cause.Body != row.Frame || occurrence.Cause.Collider != row.Region)
            throw new ArgumentException("Activation cause differs from the declared contact-load row.");
        return row.Frame;
    }

    private (GpuBodyId ColliderOwner, bool ContactLoad) ResolvePhysicalCause(ActivationOccurrence occurrence, int emitter, IPhysicsSceneDeclaration scene)
    {
        // The document allocates every physical identity once, so one source id names at most one trigger or sensor.
        ContactTriggerDeclaration? trigger = null; OrientationSensorDeclaration? sensor = null; PassageSensorDeclaration? passage = null;
        foreach (var candidate in scene.Triggers) if (candidate.Id.Value == occurrence.Cause.Source.Value) trigger = candidate;
        foreach (var candidate in scene.OrientationSensors) if (candidate.Id.Value == occurrence.Cause.Source.Value) sensor = candidate;
        foreach (var candidate in _passageSensors) if (candidate.Id.Value == occurrence.Cause.Source.Value) passage = candidate;
        ContactLoadSensorDeclaration? load = null;
        foreach (var candidate in scene.ContactLoadSensors) if (candidate.Id.Value == occurrence.Cause.Source.Value) load = candidate;
        GpuBodyId colliderOwner;
        if (trigger is { } contact)
        { colliderOwner = ValidateContactCause(occurrence, emitter, contact, scene); }
        else if (sensor is { } turned)
        { colliderOwner = ValidateOrientationCause(occurrence, emitter, turned); }
        else if (passage is { } through)
        { colliderOwner = ValidatePassageCause(occurrence, emitter, through); }
        else if (load is { } row)
        { colliderOwner = ValidateLoadCause(occurrence, emitter, row); }
        else throw new ArgumentException("Activation cause names no declared source.");
        return (colliderOwner, load is not null);
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
        if (time != 0) return time;
        var emitter = a.Emitter.Value.CompareTo(b.Emitter.Value);
        if (emitter != 0) return emitter;
        var body = a.Cause.Body.Value.CompareTo(b.Cause.Body.Value);
        return body != 0 ? body : a.Cause.Collider.Value.CompareTo(b.Cause.Collider.Value);
    }
}

/// <summary>Catalogue boundary supplies declarations; no per-part controller callback.</summary>
