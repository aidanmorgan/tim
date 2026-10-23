using System;
using System.Collections.Generic;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Gpu;

/// <summary>One event producer of the admitted world: the owner whose transitions it publishes and the one kind it produces. Producers
/// are counted per owner and kind, never per (owner, body) pair, so the ring grows linearly with the scene's producers.</summary>
public readonly record struct EventRow(GpuBodyId Owner, SimulationEventKind Kind);

/// <summary>The row states one tick's records are derived from: the capture latches, the contact-work ledger, the supply sources and
/// the passage tracker (its per-body pass counts and the passes of the step that produced it).</summary>
public readonly record struct EventSourceRows(PhysicsCaptureRead Captures, PhysicsContactWorkRead ContactWorks,
    PhysicsElectricalRead Electrical, PassageSensorTracker Passages);

/// <summary>The event ring producer of one admitted world (Story 9A.22). Every one-shot transition of a committed tick becomes exactly
/// one record from exactly one source row:
/// <list type="bullet">
/// <item>Impact: each new contact-work occurrence (one per increment of its owner's count), at its own physical time;</item>
/// <item>Passage: each pass of a passage aperture (every pass, never only the first), Count the n-th pass of that aperture this run;</item>
/// <item>Captured: a residence capture latching;</item>
/// <item>ElectricalPowered: a supply source's availability edge, at the tick boundary;</item>
/// <item>the activation network's Physical-stage transitions, mapped by the emitting node's kind through <see cref="KindOf"/>
/// (TriggerActivated, ThresholdReached, MemoryChanged and the debounced ContactLoad level; a passage node's and an indicator's
/// emissions publish nothing, since their rows or levels own them).</item>
/// </list>
/// The producer is pure: <see cref="Stage"/> derives a candidate tick's records from the committed and candidate row states and
/// writes nothing; sequences are assigned only when the device commits (<see cref="EventRingOutbox"/>). It holds no element branch.</summary>
public sealed class EventRecordProducer
{
    /// <summary>The ring's horizon: half a second of the admitted cadence's ticks, a time requirement (not a table size).</summary>
    public const float HorizonSeconds = .5f;

    private readonly Dictionary<ActivationNodeId, ActivationNodeDeclaration> _nodes = new();
    private readonly Dictionary<GpuSensorId, ResidenceSensorDeclaration> _captures = new();
    private readonly Dictionary<GpuPassageSensorId, GpuBodyId> _apertures = new();
    private readonly uint _substeps;
    private readonly EventRow[] _rows;

    /// <summary>Every event producer, in source order: one per contact-work owner, passage aperture, capture frame, supply source and
    /// emitting activation node.</summary>
    public IReadOnlyList<EventRow> Rows => _rows;
    /// <summary>The ring capacity (coordinator decision, 24 Oct 2026): the next power of two of (Σ over producers of
    /// <see cref="RecordsPerTick"/>) × the horizon's ticks at the admitted cadence, at least one slot.</summary>
    public uint Capacity { get; }

    private EventRecordProducer(PhysicsSceneDeclaration scene, ActivationNetwork network, PassageSensorTracker passages, WorkshopGpuProfile profile)
    {
        _substeps = profile.Substeps;
        var rows = new List<EventRow>();
        var seen = new HashSet<EventRow>();
        void Producer(GpuBodyId owner, SimulationEventKind kind) { if (seen.Add(new(owner, kind))) rows.Add(new(owner, kind)); }
        foreach (var work in scene.ContactWorks) Producer(work.Owner, SimulationEventKind.Impact);
        foreach (var sensor in passages.Sensors)
        {
            _apertures.Add(sensor.Id, sensor.Frame);
            Producer(sensor.Frame, SimulationEventKind.Passage);
        }
        foreach (var sensor in scene.Sensors)
        {
            _captures.Add(sensor.Id, sensor);
            Producer(sensor.Frame, SimulationEventKind.Captured);
        }
        foreach (var source in scene.Electrical.Sources) Producer(source.Owner, SimulationEventKind.ElectricalPowered);
        foreach (var node in network.Nodes)
        {
            _nodes.Add(node.Id, node);
            var kind = KindOf(node.Kind, node.Completion);
            if (kind != SimulationEventKind.None) Producer(node.Owner, kind);
        }
        _rows = rows.ToArray();
        ulong perTick = 0;
        foreach (var row in _rows) perTick += RecordsPerTick(row.Kind);
        Capacity = RingCapacity(perTick, profile.Cadence);
    }

    /// <summary>The declared bound on the records one producer publishes per committed tick, per kind. It sizes the ring, not the
    /// truth: a tick that publishes more (a burst) still commits, and the excess beyond the ring is counted in Lapped.
    /// <list type="bullet">
    /// <item>Impact 1: a contact-work owner's typical tick strikes one body; each struck body then waits its declared cooldown
    /// (≥ 9 physical steps), so several strikes in one tick are a burst;</item>
    /// <item>Passage 1: a pass needs the body to cross the plane and then clear the rearm line upstream, so one aperture passes one
    /// body per tick except when several bodies cross together (a burst);</item>
    /// <item>Captured 1: each capture latches once per run, so a frame captures more than one body in a tick only in a burst;</item>
    /// <item>ElectricalPowered 1: a source's availability changes at most once per tick boundary;</item>
    /// <item>TriggerActivated, ThresholdReached, MemoryChanged, ContactLoad 1: a node emits at most once per boundary (a contact,
    /// orientation, timer or tally emission is once per run; an oscillator pulse, a memory or a load level change at most once per
    /// tick).</item>
    /// </list></summary>
    public static uint RecordsPerTick(SimulationEventKind kind) => kind switch
    {
        SimulationEventKind.Impact or SimulationEventKind.Passage or SimulationEventKind.Captured or SimulationEventKind.ElectricalPowered or
            SimulationEventKind.TriggerActivated or SimulationEventKind.ThresholdReached or SimulationEventKind.MemoryChanged or
            SimulationEventKind.ContactLoad => 1,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "No producer publishes this kind.")
    };

    /// <summary>The ring capacity for producers publishing <paramref name="recordsPerTick"/> records per tick at <paramref name="cadence"/>.
    /// The hard limit is checked at the fastest admitted cadence, so a cadence change never refuses a construction already admitted;
    /// beyond it the construction is refused with the typed admission rejection (the session stays Building, never faulted).</summary>
    public static uint RingCapacity(ulong recordsPerTick, SimulationCadence cadence)
    {
        ulong fastest = 0;
        foreach (var admitted in Enum.GetValues<SimulationCadence>())
            fastest = Math.Max(fastest, ActivationTimerDeclaration.Duration(HorizonSeconds, admitted));
        if (recordsPerTick > EventRingBuffer.MaximumCapacity / fastest)
            throw new GpuAdmissionException("The construction's event producers exceed the event ring's limit.");
        return EventRingBuffer.Capacity(recordsPerTick * ActivationTimerDeclaration.Duration(HorizonSeconds, cadence));
    }

    public static EventRecordProducer Compile(PhysicsSceneDeclaration scene, ActivationNetwork network, PassageSensorTracker passages,
        WorkshopGpuProfile profile)
    {
        ArgumentNullException.ThrowIfNull(scene); ArgumentNullException.ThrowIfNull(network); ArgumentNullException.ThrowIfNull(passages);
        return new(scene, network, passages, profile);
    }

    /// <summary>The one exhaustive table from an activation node's kind (and a timer's completion policy) to the record kind its
    /// Physical-stage transitions publish. None: the node's transitions publish nothing (a passage node's pulse is its aperture's
    /// Passage row; an indicator only mirrors its input; a rearming timer's expiry is a level).</summary>
    public static SimulationEventKind KindOf(ActivationNodeKind kind, TimerCompletionPolicy completion) => kind switch
    {
        ActivationNodeKind.ContactSource or ActivationNodeKind.OrientationSource or ActivationNodeKind.Oscillator =>
            SimulationEventKind.TriggerActivated,
        ActivationNodeKind.Timer => completion switch
        {
            TimerCompletionPolicy.Latch => SimulationEventKind.TriggerActivated,
            TimerCompletionPolicy.Rearm => SimulationEventKind.None,
            _ => throw new ArgumentOutOfRangeException(nameof(completion), completion, "Undefined timer completion.")
        },
        ActivationNodeKind.Counter => SimulationEventKind.ThresholdReached,
        ActivationNodeKind.StateLatch => SimulationEventKind.MemoryChanged,
        ActivationNodeKind.ContactLoadSource => SimulationEventKind.ContactLoad,
        ActivationNodeKind.PassageSource or ActivationNodeKind.StickyFirstInput or ActivationNodeKind.LevelIndicator => SimulationEventKind.None,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Undefined activation node kind.")
    };

    /// <summary>The unsequenced records of one candidate tick, in the D4 order (Ordinal, Phase, Kind, Source, Other). A row with no
    /// sub-tick time (a supply edge, a memory or load level) carries the tick boundary.</summary>
    public SimulationEventRecord[] Stage(SimulationTick tick, EventSourceRows committed, EventSourceRows candidate,
        ActivationTransitionRead transitions, ReadOnlySpan<ContactLoadSensorRead> loads)
    {
        var records = new List<SimulationEventRecord>();
        var boundary = ActivationTime.Boundary(tick, _substeps);
        void Add(SimulationEventKind kind, GpuBodyId source, GpuBodyId other, ActivationTime time, float magnitude, uint count, uint level) =>
            records.Add(new(0, tick.Value, kind, source.Value, other.Value, time.Ordinal, time.Phase, magnitude, count, level));

        // Impact: each owner's new occurrence rows, exactly the sequences in (committed, candidate].
        for (var work = 0; work < candidate.ContactWorks.Count; work++)
        {
            var before = committed.ContactWorks[work].OccurrenceCount; var owner = candidate.ContactWorks[work].Owner;
            for (var i = 0; i < candidate.ContactWorks.OccurrenceCount; i++)
            {
                var hit = candidate.ContactWorks.Occurrence(i);
                if (hit.Work.Value != work || hit.Sequence <= before) continue;
                Add(SimulationEventKind.Impact, owner, hit.Target, new(hit.EventOrdinal, (float)hit.EventPhase), hit.Debit.Value, hit.Sequence, 0);
            }
        }
        // Passage: every pass of the step, counted per aperture from the committed per-body pass counts, in time order.
        var fired = new List<PassageSensorRead>(candidate.Passages.Fired);
        fired.Sort((a, b) => a.EventOrdinal != b.EventOrdinal ? a.EventOrdinal.CompareTo(b.EventOrdinal) : a.Body.Value.CompareTo(b.Body.Value));
        var passes = new Dictionary<GpuPassageSensorId, ulong>();
        foreach (var state in committed.Passages.States) passes[state.Sensor] = passes.GetValueOrDefault(state.Sensor) + state.PassedCount;
        foreach (var pass in fired)
        {
            var count = passes[pass.Id] = passes.GetValueOrDefault(pass.Id) + 1;
            Add(SimulationEventKind.Passage, _apertures[pass.Id], pass.Body, new(pass.EventOrdinal, (float)pass.EventPhase),
                (float)pass.Speed.Value, (uint)Math.Min(count, uint.MaxValue), 0);
        }
        // Captured: a residence latch's Clear→Latched edge.
        for (var i = 0; i < candidate.Captures.Count; i++)
        {
            var capture = candidate.Captures[i];
            if (capture.Phase != CaptureLatchPhase.Latched || committed.Captures[i].Phase == CaptureLatchPhase.Latched) continue;
            var sensor = _captures[capture.Sensor];
            Add(SimulationEventKind.Captured, sensor.Frame, sensor.Target, new(capture.EventOrdinal, (float)capture.EventPhase), 0f, 0, 0);
        }
        // ElectricalPowered: an availability edge at the boundary.
        for (var i = 0; i < candidate.Electrical.Count; i++)
        {
            var source = candidate.Electrical[i];
            if (source.Available == committed.Electrical[i].Available) continue;
            Add(SimulationEventKind.ElectricalPowered, source.Owner, default, boundary, source.Remaining.Value, 0, source.Available ? 1u : 0u);
        }
        // The network's Physical-stage transitions, through the node table.
        for (var i = 0; i < transitions.Count; i++)
        {
            var transition = transitions[i];
            var node = _nodes[transition.Node];
            var kind = KindOf(node.Kind, node.Completion);
            if (kind == SimulationEventKind.None) continue;
            var level = transition.Level == ActivationPhase.Latched ? 1u : 0u;
            if (transition.Kind == ActivationTransitionKind.Emitted)
                Add(kind, node.Owner, transition.Occurrence.Cause.Body, transition.Occurrence.Time,
                    kind == SimulationEventKind.TriggerActivated ? (float)transition.Occurrence.Cause.ApproachSpeed.Value : 0f, transition.Count, 0);
            else if (kind == SimulationEventKind.ContactLoad)
                Add(kind, node.Owner, default, boundary, Measured(loads, node.ContactLoad), 0, level);
            else Add(kind, node.Owner, default, boundary, 0f, 0, level);
        }
        records.Sort(static (a, b) =>
            a.Ordinal != b.Ordinal ? a.Ordinal.CompareTo(b.Ordinal) : a.Phase != b.Phase ? a.Phase.CompareTo(b.Phase) :
            a.Kind != b.Kind ? a.Kind.CompareTo(b.Kind) : a.Source != b.Source ? a.Source.CompareTo(b.Source) : a.Other.CompareTo(b.Other));
        // Validated here, before the commit point: an invalid record discards the candidate, never faults a publication.
        foreach (var record in records) record.Validate();
        return records.ToArray();
    }

    private static float Measured(ReadOnlySpan<ContactLoadSensorRead> loads, GpuContactLoadSensorId sensor)
    {
        foreach (var load in loads) if (load.Sensor == sensor) return load.MeasuredMass;
        throw new ArgumentException("A contact-load level changed without its row's read.");
    }
}

/// <summary>The committed side of the event ring (Story 9A.22 D3, D5, D8). A committed tick's records receive contiguous sequences from
/// the last committed sequence + 1 and wait here until the worker publishes them; a discarded candidate never reaches it, so it
/// consumes no sequence. Every admission restarts it under the admitted epoch and capacity (Reset drops undrained records and
/// sequences start at 1). A publication of more records than the ring holds still advances the sequence, writes only the newest
/// capacity records and adds the rest to Lapped; nothing here throws, blocks or checks overflow.</summary>
public sealed class EventRingOutbox
{
    private readonly List<SimulationEventRecord> _pending = new();
    public SimulationEpoch Epoch { get; private set; }
    public uint Capacity { get; private set; } = 1;
    public ulong WriteSequence { get; private set; }
    public ulong Lapped { get; private set; }
    /// <summary>The committed records not yet published, oldest first.</summary>
    public IReadOnlyList<SimulationEventRecord> Pending => _pending;

    public void Restart(SimulationEpoch epoch, uint capacity)
    {
        _pending.Clear();
        Epoch = epoch; Capacity = capacity; WriteSequence = 0; Lapped = 0;
    }

    public void Commit(ReadOnlySpan<SimulationEventRecord> candidate)
    {
        foreach (var record in candidate) _pending.Add(record with { Sequence = unchecked(++WriteSequence) });
    }

    /// <summary>The publication of every pending record (empty before any admission): the ring header after it, then the newest
    /// records that fit. The pending records are taken.</summary>
    public byte[] Drain()
    {
        if (Epoch.Value == 0) return [];
        var skipped = Math.Max(0, _pending.Count - (int)Math.Min(Capacity, int.MaxValue));
        Lapped = unchecked(Lapped + (ulong)skipped);        var batch = EventRingBuffer.Batch(new(Capacity, EventRingBuffer.SlotBytes, Epoch.Value, WriteSequence, Lapped),
            System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_pending)[skipped..]);
        _pending.Clear();
        return batch;
    }
}
