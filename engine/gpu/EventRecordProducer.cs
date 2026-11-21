using System;
using System.Collections.Generic;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Gpu;

/// <summary>The row states one tick's records are derived from: the capture latches, the device-private contact-work ledger (its
/// occurrence rows never leave the device, Story 9A.23 D9), the supply sources and the passage tracker (its per-body pass counts and the
/// passes of the step that produced it).</summary>
public readonly record struct EventSourceRows(PhysicsCaptureRead Captures, ContactWorkLedger ContactWorks,
    PhysicsElectricalRead Electrical, PassageSensorTracker Passages, PhysicsFiniteWorkActuatorRead FiniteWork = default,
    PhysicsIntegrityRead Integrity = default, PhysicsCoordinateTravelRead CoordinateTravel = default);

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
    /// <summary>The physical-time retention horizon, independent of commit cadence.</summary>
    public const float HorizonSeconds = .5f;

    private readonly Dictionary<ActivationNodeId, ActivationNodeDeclaration> _nodes = new(4);
    private readonly Dictionary<GpuSensorId, ResidenceSensorDeclaration> _captures = new(4);
    private readonly Dictionary<GpuPassageSensorId, GpuEventSourceId> _apertures = new(4);
    private readonly uint _substeps;
    private EventRecordProducer(IPhysicsSceneDeclaration scene, IActivationProgram network, PassageSensorTracker passages, WorkshopGpuProfile profile)
    {
        _substeps = profile.Substeps;
        foreach (var sensor in passages.Sensors)
        {
            ActivationNodeId producer = default;
            foreach (var node in network.Nodes) if (node.Passage == sensor.Id) producer = node.Id;
            if (producer == default) throw new ArgumentException("Passage source is not bound.");
            _apertures.Add(sensor.Id, new(producer.Value));
        }
        foreach (var sensor in scene.Sensors)
        {
            _captures.Add(sensor.Id, sensor);
        }
        foreach (var node in network.Nodes)
        {
            _nodes.Add(node.Id, node);
        }
    }

    public static EventRecordProducer Compile(IPhysicsSceneDeclaration scene, IActivationProgram network, PassageSensorTracker passages,
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
        ActivationNodeKind.PassageSource or ActivationNodeKind.StickyFirstInput or ActivationNodeKind.LevelIndicator or ActivationNodeKind.PhysicalRequest => SimulationEventKind.None,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Undefined activation node kind.")
    };

    /// <summary>The unsequenced records of one candidate tick, in the D4 order (Ordinal, Phase, Kind, Source, Other). A row with no
    /// sub-tick time (a supply edge, a memory or load level) carries the tick boundary.</summary>
    public SimulationEventRecord[] Stage(SimulationTick tick, EventSourceRows committed, EventSourceRows candidate,
        ActivationTransitionRead transitions, ReadOnlySpan<ContactLoadSensorRead> loads)
    {
        var records = new List<SimulationEventRecord>(32);
        var boundary = ActivationTime.Boundary(tick, _substeps);
        AppendIntegrity(records, tick, committed.Integrity, candidate.Integrity);
        AppendCoordinateTravel(records, tick, committed.CoordinateTravel, candidate.CoordinateTravel);
        AppendFiniteWork(records, tick, checked((uint)((tick.Value - 1) * _substeps)), committed.FiniteWork, candidate.FiniteWork);
        void Add(SimulationEventKind kind, GpuEventSourceId source, GpuBodyId other, ActivationTime time, float magnitude, uint count, uint level) =>
            records.Add(new(0, tick.Value, kind, source, other.Value, time.Ordinal, time.Phase, magnitude, count, level));

        // Impact: each owner's new occurrence rows, exactly the sequences in (committed, candidate].
        for (var work = 0; work < candidate.ContactWorks.Count; work++)
        {
            var before = committed.ContactWorks[work].OccurrenceCount; var owner = candidate.ContactWorks[work].Owner;
            for (var i = 0; i < candidate.ContactWorks.OccurrenceCount; i++)
            {
                var hit = candidate.ContactWorks.Occurrence(i);
                if (hit.Work.Value != work || hit.Sequence <= before) continue;
                Add(SimulationEventKind.Impact, new(owner.Value), hit.Target, new(hit.EventOrdinal, (float)hit.EventPhase), hit.Debit.Value, hit.Sequence, 0);
            }
        }
        // Passage: every pass of the step, counted per aperture from the committed per-body pass counts, in time order.
        var fired = new List<PassageSensorRead>(candidate.Passages.Fired.ToArray());
        fired.Sort((a, b) => a.EventOrdinal != b.EventOrdinal ? a.EventOrdinal.CompareTo(b.EventOrdinal) : a.Body.Value.CompareTo(b.Body.Value));
        var passes = new Dictionary<GpuPassageSensorId, ulong>(4);
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
            if (capture.Phase != CaptureLatchPhase.Latched ||
                (committed.Captures.TryGet(new(capture.Sensor,capture.Target),out var prior) && prior.Phase==CaptureLatchPhase.Latched)) continue;
            var sensor = _captures[capture.Sensor];
            Add(SimulationEventKind.Captured, new(sensor.Frame.Value), capture.Target, new(capture.EventOrdinal, (float)capture.EventPhase), 0f, 0, 0);
        }
        // ElectricalPowered: an availability edge at the boundary.
        for (var i = 0; i < candidate.Electrical.Count; i++)
        {
            var source = candidate.Electrical[i];
            if (source.Available == committed.Electrical[i].Available) continue;
            Add(SimulationEventKind.ElectricalPowered, new(source.Owner.Value), default, boundary, source.Remaining.Value, 0, source.Available ? 1u : 0u);
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
                Add(kind, new(node.Id.Value), transition.Occurrence.Cause.Body, transition.Occurrence.Time,
                    kind == SimulationEventKind.TriggerActivated ? (float)transition.Occurrence.Cause.ApproachSpeed.Value : 0f, transition.Count, 0);
            else if (kind == SimulationEventKind.ContactLoad)
                Add(kind, new(node.Id.Value), default, boundary, Measured(loads, node.ContactLoad), 0, level);
            else Add(kind, new(node.Id.Value), default, boundary, 0f, 0, level);
        }
        records.Sort(CompareRecords);
        // Validated here, before the commit point: an invalid record discards the candidate, never faults a publication.
        foreach (var record in records) record.Validate();
        return records.ToArray();
    }

    public static SimulationEventRecord[] StageFiniteWork(SimulationTick tick, uint ordinal,
        PhysicsFiniteWorkActuatorRead before, PhysicsFiniteWorkActuatorRead after)
    {
        var records = new List<SimulationEventRecord>();
        AppendFiniteWork(records, tick, ordinal, before, after);
        records.Sort(CompareRecords);
        return records.ToArray();
    }

    private static void AppendFiniteWork(List<SimulationEventRecord> records, SimulationTick tick, uint ordinal,
        PhysicsFiniteWorkActuatorRead before, PhysicsFiniteWorkActuatorRead after)
    {
        if (before.Count != after.Count) throw new ArgumentException("Finite-work event population changed.");
        for (var index = 0; index < after.Count; index++)
        {
            var prior = before[index]; var current = after[index];
            if (prior.Id != current.Id || prior.Store != current.Store || prior.Region != current.Region ||
                current.OccurrenceCount < prior.OccurrenceCount || (ulong)current.OccurrenceCount > (ulong)prior.OccurrenceCount + 1)
                throw new ArgumentException("Finite-work event ownership or occurrence changed.");
            if (current.OccurrenceCount == prior.OccurrenceCount) continue;
            current.Validate();
            if (current.LastResult != FiniteWorkRequestResult.Fired)
                throw new ArgumentException("Finite-work occurrence has no accepted release.");
            var record = new SimulationEventRecord(0, tick.Value, SimulationEventKind.FiniteWorkReleased,
                new(current.Id.Value), current.LastPayload.Value, ordinal, 0f, current.LastDebit.Value, current.OccurrenceCount, 0);
            record.Validate();
            records.Add(record);
        }
    }

    private static void AppendCoordinateTravel(List<SimulationEventRecord> records, SimulationTick tick,
        PhysicsCoordinateTravelRead before, PhysicsCoordinateTravelRead after)
    {
        if (before.Count != after.Count) throw new ArgumentException("Coordinate travel event population changed.");
        for (var i = 0; i < after.Count; i++)
        {
            var prior = before[i]; var current = after[i];
            CoordinateTravelReadValidation.Advance(current, prior, 0);
            if (current.Phase != CoordinateTravelPhase.Reached || prior.Phase == CoordinateTravelPhase.Reached) continue;
            records.Add(new(0, tick.Value, SimulationEventKind.CoordinateTravelReached, new(current.Id.Value),
                0, current.Ordinal, 0, current.Threshold, 1, 0));
        }
    }

    private static void AppendIntegrity(List<SimulationEventRecord> records, SimulationTick tick,
        PhysicsIntegrityRead before, PhysicsIntegrityRead after)
    {
        if (before.Count != after.Count) throw new ArgumentException("Integrity event population changed.");
        for (var i = 0; i < after.Count; i++)
        {
            var prior = before[i]; var current = after[i];
            current.Validate();
            if (prior.Id != current.Id || prior.Phase == IntegrityPhase.Broken && current != prior)
                throw new ArgumentException("Integrity occurrence identity or latch changed.");
            if (current.Phase != IntegrityPhase.Broken || prior.Phase == IntegrityPhase.Broken) continue;
            records.Add(new(0, tick.Value, SimulationEventKind.IntegrityBroken, new(current.Id.Value),
                0, current.Ordinal, current.EventPhase, 0, 1, 0));
        }
    }

    private static int CompareRecords(SimulationEventRecord a, SimulationEventRecord b) =>
            a.Ordinal != b.Ordinal ? a.Ordinal.CompareTo(b.Ordinal) : a.Phase != b.Phase ? a.Phase.CompareTo(b.Phase) :
            a.Kind != b.Kind ? a.Kind.CompareTo(b.Kind) : a.Source != b.Source ? a.Source.Value.CompareTo(b.Source.Value) : a.Other.CompareTo(b.Other);

    private static float Measured(ReadOnlySpan<ContactLoadSensorRead> loads, GpuContactLoadSensorId sensor)
    {
        foreach (var load in loads) if (load.Sensor == sensor) return load.MeasuredMass;
        throw new ArgumentException("A contact-load level changed without its row's read.");
    }
}

/// <summary>Stages actual event demand with the same candidate as the physical world. Sequence assignment,
/// retention metadata and publication buffers are prepared before commit; Commit only publishes references.</summary>
public sealed class EventRingOutbox
{
    private sealed record State(SimulationEpoch Epoch, PhysicalCommitRevision PhysicalRevision, uint Ordinal, uint Capacity, ulong Sequence,
        System.Collections.Immutable.ImmutableSortedDictionary<ActivationTime, int> History,
        int Retained, SimulationEventRecord[] Pending, byte[] Batch);
    private static readonly State Empty = new(default, default, 0, EventRingBuffer.DefaultCapacity, 0,
        System.Collections.Immutable.ImmutableSortedDictionary<ActivationTime, int>.Empty, 0, [], []);
    private State _committed = Empty;
    private State? _candidate;
    private bool _drained = true;
    public SimulationEpoch Epoch => _committed.Epoch;
    public PhysicalCommitRevision PhysicalRevision => _committed.PhysicalRevision;
    public uint Capacity => _committed.Capacity;
    public ulong WriteSequence => _committed.Sequence;
    public IReadOnlyList<SimulationEventRecord> Pending => _drained ? Array.Empty<SimulationEventRecord>() : _committed.Pending;
    public int RetainedCount => _committed.Retained;
    public bool HasCandidate => _candidate is not null;
    public byte[] PreparedBatch => (_candidate ?? throw new InvalidOperationException("No event candidate.")).Batch;

    public void PrepareAdmission(SimulationEpoch epoch)
    {
        try
        {
            if (epoch.Value == 0) throw new ArgumentException("An event epoch must be nonzero.");
            if (_candidate is not null) throw new InvalidOperationException("An event candidate is already prepared.");
            var batch = EventRingBuffer.Batch(new(EventRingBuffer.DefaultCapacity, EventRingBuffer.SlotBytes, epoch.Value, 0), []);
            _candidate = Empty with { Epoch = epoch, Batch = batch };
        }
        catch (OutOfMemoryException) { throw new EventPreparationException(EventPublicationPreparation.StorageUnavailable); }
        catch (OverflowException) { throw new EventPreparationException(EventPublicationPreparation.StorageUnavailable); }
    }

    public void PreparePhysicalCommit(PhysicalCommitRevision revision, uint ordinal, ReadOnlySpan<SimulationEventRecord> records)
    {
        try
        {
            if (_candidate is not null) throw new InvalidOperationException("An event candidate is already prepared.");
            var prior = _committed;
            if (prior.Epoch.Value == 0 || !prior.PhysicalRevision.TryAdvance(out var expected) || revision != expected || ordinal < prior.Ordinal)
                throw new ArgumentException("Event publication must own the next physical commit without reversing time.");
            // The supported physical profile is 480 Hz: 0.5 seconds is 240 physical steps,
            // independent of whether the current committed tick contains two, four or eight.
            var cutoff = new ActivationTime(ordinal > 240 ? ordinal - 240 : 0, 0);
            var history = prior.History;
            var retained = prior.Retained;
            foreach (var entry in prior.History)
            {
                if (entry.Key >= cutoff) break;
                history = history.Remove(entry.Key);
                retained -= entry.Value;
            }
            var sequence = EventRingBuffer.LastSequence(prior.Sequence, records.Length);
            var previousPending = _drained ? Array.Empty<SimulationEventRecord>() : prior.Pending;
            var pending = new SimulationEventRecord[checked(previousPending.Length + records.Length)];
            previousPending.CopyTo(pending, 0);
            for (var i = 0; i < records.Length; i++)
            {
                var record = records[i];
                record.Validate();
                var time = new ActivationTime(record.Ordinal, record.Phase);
                time.Validate();
                if (record.Sequence != 0 || time > new ActivationTime(ordinal, 0))
                    throw new ArgumentException("An event candidate must be unsequenced and no later than its physical commit.");
                pending[previousPending.Length + i] = record with { Sequence = prior.Sequence + (ulong)i + 1 };
                if (time >= cutoff)
                {
                    history = history.SetItem(time, checked(history.GetValueOrDefault(time) + 1));
                    retained = checked(retained + 1);
                }
            }
            // Unpublished records cannot be overwritten even if a caller has not drained
            // them within the physical horizon. Recent pending records already count above.
            var unpublishedExpired = 0;
            foreach (var record in pending)
                if (new ActivationTime(record.Ordinal, record.Phase) < cutoff) unpublishedExpired++;
            var required = checked((ulong)retained + (ulong)unpublishedExpired);
            var capacity = EventRingBuffer.Capacity(prior.Capacity, required);
            var batch = EventRingBuffer.Batch(new(capacity, EventRingBuffer.SlotBytes, prior.Epoch.Value, sequence), pending);
            _candidate = new(prior.Epoch, revision, ordinal, capacity, sequence, history, retained, pending, batch);
        }
        catch (OutOfMemoryException) { throw new EventPreparationException(EventPublicationPreparation.StorageUnavailable); }
        catch (OverflowException) { throw new EventPreparationException(EventPublicationPreparation.StorageUnavailable); }
    }

    public void Commit()
    {
        _committed = _candidate ?? throw new InvalidOperationException("No event candidate.");
        _candidate = null;
        _drained = false;
    }
    public void Discard() => _candidate = null;
    public void Dispose() { _candidate = null; _committed = Empty; _drained = true; }

    /// <summary>Takes the already allocated committed publication. Repeated drains return no publication.</summary>
    public byte[] Drain()
    {
        if (_drained || HasCandidate) return [];
        _drained = true;
        return _committed.Batch;
    }
}
