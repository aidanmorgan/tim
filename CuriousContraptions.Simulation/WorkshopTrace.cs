using System.Buffers.Binary;
using System.Collections.Immutable;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

// Observational only. All methods contain diagnostic failure; none can reject or change a commit.
// 3 was DeviceLost: the worker owns no device since the pure C# engine (Story 9.2b) and Story 9.4b deleted that path.
// Recorded trace values keep their numbers; 3 is undefined and rejects.
internal sealed class WorkshopTrace
{
    public WorkshopDiagnostics Diagnostics { get; }
    public WorkshopTrace(WorkshopDiagnostics? diagnostics = null) => Diagnostics = diagnostics ?? new();
    private const int Capacity = 7201;
    private const int ChunkRecords = 128;
    #if PLAYTEST
    private byte[] _records = new byte[16 * 1024];
    private int[] _recordEnds = new int[32];
    internal int RecordCapacityBytes => _records.Length;
    internal int RecordOffsetCapacity => _recordEnds.Length;
    private TraceEnd? _poseEnd;
    private int _count;
    #endif
    private double[] _durations = new double[32];
    internal int DurationCapacity => _durations.Length;
    private int _durationCount;
    private bool _timingValid;
    private ulong _identity;
    private SimulationEpoch _epoch;
    private int _expectedCount;
    private WorkshopGpuProfile _profile;
    private bool _active, _sealed;
    private TraceEnd _sealedReason;
    private CompiledSceneId _scene;
    private RuntimeSessionId _session;
    internal void Begin(RuntimeSessionId session, WorkshopCompiledScene construction, SimulationEpoch epoch, ProjectionEpoch projection, WorkshopGpuProfile profile)
    {
        try
        {
            profile.Validate(); session.Validate();
            construction.Validate(); projection.Validate();
            if (construction.Settings.Simulation != profile.Cadence ||
                construction.Settings.Physical != profile.Physical)
                throw new ArgumentException("Trace construction and physical cadence differ.");
            var expectedCount = checked((int)profile.RunTickLimit + 1);
            if (expectedCount > Capacity) throw new ArgumentException("Trace profile exceeds its reservation.");
            #if PLAYTEST
            var records = new byte[16 * 1024];
            var recordEnds = new int[32];
            #endif
            var durations = new double[32];
            if (_active) End(TraceEnd.Reset);
            // Preserve the preceding capture before this one buffer is reused. No new RunLoop exists yet.
            FlushSealed();
            if (_identity == ulong.MaxValue) return;
            _identity++;
            _epoch = epoch;
            #if PLAYTEST
            _count = 0;
            _records = records;
            _recordEnds = recordEnds;
            _poseEnd = null;
            #endif
            _durations = durations;
            _durationCount = 0;
            _timingValid = true;
            _scene = construction.Identity; _session = session;
            _profile = profile;
            _expectedCount = expectedCount;
            _active = true;
        }
        catch (OutOfMemoryException) { End(TraceEnd.Overflow); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or OverflowException)
        { End(TraceEnd.InvalidRecord); }
    }
    #if PLAYTEST
    internal void Append(byte[] record)
    {
        if (!_active || _poseEnd.HasValue) return;
        try
        {
            if (_count == _expectedCount) { _poseEnd = TraceEnd.Overflow; return; }
            if (WorkshopTraceRecord.ReadProfile(record) != _profile ||
                BinaryPrimitives.ReadUInt64LittleEndian(record.AsSpan(16)) != _epoch.Value ||
                BinaryPrimitives.ReadUInt64LittleEndian(record.AsSpan(24)) != (ulong)_count)
            { _poseEnd = TraceEnd.InvalidRecord; return; }
            var first = _count == 0 ? 0 : _recordEnds[_count - 1];
            var end = checked(first + record.Length);
            var records = Expanded(_records, end, Array.MaxLength);
            var ends = Expanded(_recordEnds, _count + 1, Capacity);
            record.CopyTo(records, first);
            ends[_count] = end;
            _records = records; _recordEnds = ends;
            _count++;
        }
        catch (Exception error) when (error is OverflowException or OutOfMemoryException)
        { _poseEnd = TraceEnd.Overflow; }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or IndexOutOfRangeException)
        { _poseEnd = TraceEnd.InvalidRecord; }
    }
    #endif
    // Host observational milliseconds only. This is the complete critical-path wall span,
    // including awaits and publication, not exclusive CPU time or numerical game state.
    // Called once per tick on the hot path, so it is fully optimised from its first call. Story 9.4b added this while
    // chasing a rare allocation in WorkshopTraceTests.DurationRetentionHasNoRoutineAllocationInNativeExecution; tiering
    // was never confirmed as the cause, which is open (P0-034 / Story 16.1), so this is not a proven mitigation.
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
    internal void RecordTickDuration(SimulationTick tick, double milliseconds)
    {
        if (!_active) return;
        // The guard bounds the index, so the store cannot throw.
        if (!_timingValid || !double.IsFinite(milliseconds) || milliseconds < 0 ||
            tick.Value != (ulong)_durationCount + 1 ||
            tick.Value >= (ulong)_expectedCount)
        { _timingValid = false; return; }
        try
        {
            _durations = Expanded(_durations, _durationCount + 1, Capacity - 1);
            _durations[_durationCount++] = milliseconds;
        }
        catch (Exception error) when (error is OverflowException or OutOfMemoryException)
        { _timingValid = false; }
    }

    private static T[] Expanded<T>(T[] current, int required, int maximum)
    {
        if (required > maximum) throw new OverflowException("Trace storage exceeds its representable or temporal extent.");
        if ((long)required * 4 <= (long)current.Length * 3 || current.Length == maximum) return current;
        var headroom = ((long)required * 4 + 2) / 3;
        var capacity = (int)Math.Min(maximum, Math.Max((long)current.Length * 2, headroom));
        if (capacity <= current.Length) return current;
        var next = new T[capacity];
        current.CopyTo(next, 0);
        return next;
    }

    internal void End(TraceEnd reason)
    {
        // TraceEnd 3 (the retired DeviceLost) and every other undefined value never reach a record header.
        if (!Enum.IsDefined(reason)) throw new ArgumentOutOfRangeException(nameof(reason), reason, "Undefined trace end reason.");
        if (!_active) return;
        _active = false;
        #if PLAYTEST
        if (!_poseEnd.HasValue) _poseEnd = reason == TraceEnd.Complete && _count != _expectedCount ? TraceEnd.InvalidRecord : reason;
        #endif
        _sealedReason = reason;
        _sealed = true;
    }

    internal bool CanFlush(WorkshopSimulationPhase phase) =>
        _sealed && phase is WorkshopSimulationPhase.Building or WorkshopSimulationPhase.Completed or
            WorkshopSimulationPhase.Faulted or WorkshopSimulationPhase.Disposed;

    internal void Flush(WorkshopSimulationPhase phase)
    {
        // Invalid/overflow captures sealed during Run must not stall an ordinary publication.
        if (CanFlush(phase)) FlushSealed();
    }

    private void FlushSealed()
    {
        if (!_sealed) return;
        _sealed = false;
        var reason = _sealedReason;
        try
        {
#if PLAYTEST
            var chunks = (_count + ChunkRecords - 1) / ChunkRecords;
            var poseReason = _poseEnd ?? reason;
            var poseStatus = poseReason == TraceEnd.Complete ? TraceTimingStatus.Complete : TraceTimingStatus.Incomplete;
            Record(WorkshopCaptureKind.Pose, WorkshopCaptureStage.Begin, poseReason, poseStatus, _count, chunks);
            for (var ordinal = 0; ordinal < chunks; ordinal++)
            {
                var first = ordinal * ChunkRecords;
                var count = Math.Min(ChunkRecords, _count - first);
                var samples = ImmutableArray.CreateBuilder<WorkshopTraceSample>(count);
                for (var index = first; index < first + count; index++)
                {
                    var offset = index == 0 ? 0 : _recordEnds[index - 1];
                    samples.Add(WorkshopTraceRecord.Decode(_records.AsSpan(offset, _recordEnds[index] - offset)));
                }
                Record(WorkshopCaptureKind.Pose, WorkshopCaptureStage.Chunk, poseReason, poseStatus, count, chunks,
                    ordinal, first, samples.MoveToImmutable());
            }
            Record(WorkshopCaptureKind.Pose, WorkshopCaptureStage.End, poseReason, poseStatus, _count, chunks);
#endif
            var durationChunks = (_durationCount + ChunkRecords - 1) / ChunkRecords;
            var status = reason == TraceEnd.Complete && _timingValid && _durationCount == _expectedCount - 1
                ? TraceTimingStatus.Complete : TraceTimingStatus.Incomplete;
            Record(WorkshopCaptureKind.Timing, WorkshopCaptureStage.Begin, reason, status, _durationCount, durationChunks);
            for (var ordinal = 0; ordinal < durationChunks; ordinal++)
            {
                var first = ordinal * ChunkRecords;
                var count = Math.Min(ChunkRecords, _durationCount - first);
                Record(WorkshopCaptureKind.Timing, WorkshopCaptureStage.Chunk, reason, status, count, durationChunks,
                    ordinal, first + 1, milliseconds: _durations.AsSpan(first, count).ToArray().ToImmutableArray());
            }
            Record(WorkshopCaptureKind.Timing, WorkshopCaptureStage.End, reason, status, _durationCount, durationChunks);
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or OverflowException or OutOfMemoryException or WireFaultException)
        { Diagnostics.Fault(FaultSite.TraceFlush, error); }
    }

    private void Record(WorkshopCaptureKind kind, WorkshopCaptureStage stage, TraceEnd end, TraceTimingStatus status,
        int count, int chunks, int chunk = 0, int first = 0, ImmutableArray<WorkshopTraceSample> samples = default,
        ImmutableArray<double> milliseconds = default) =>
        Diagnostics.Record(new WorkshopCaptureDiagnostic(kind, stage, _session, _identity, _epoch, _profile, end, status,
            kind == WorkshopCaptureKind.Pose ? _expectedCount : _expectedCount - 1, count, chunk, chunks, first,
            stage == WorkshopCaptureStage.Begin ? _scene : null,
            samples.IsDefault ? [] : samples, milliseconds.IsDefault ? [] : milliseconds));
}
