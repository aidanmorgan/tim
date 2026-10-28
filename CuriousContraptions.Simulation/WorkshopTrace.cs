using System.Buffers.Binary;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

// Observational only. All methods contain diagnostic failure; none can reject or change a commit.
// 3 was DeviceLost: the worker owns no device since the pure C# engine (Story 9.2b) and Story 9.4b deleted that path.
// Recorded trace values keep their numbers; 3 is undefined and rejects.
internal enum TraceEnd { Complete = 0, Reset = 1, Disposed = 2, Fault = 4, InvalidRecord = 5, Overflow = 6 }
internal enum TraceTimingStatus { Complete, Incomplete }
internal sealed class WorkshopTrace
{
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
    private readonly byte[] _durationBytes = new byte[ChunkRecords * sizeof(double)];
    private int _durationCount;
    private bool _timingValid;
    private ulong _identity;
    private SimulationEpoch _epoch;
    private int _expectedCount;
    private WorkshopGpuProfile _profile;
    private bool _active, _sealed;
    private TraceEnd _sealedReason;
    private byte[] _construction = [];
    internal void Begin(RuntimeSessionId session, WorkshopConstruction construction, SimulationEpoch epoch, ProjectionEpoch projection, WorkshopGpuProfile profile)
    {
        try
        {
            profile.Validate();
            construction.Validate(); projection.Validate();
            if (construction.Settings.Simulation != profile.Cadence ||
                construction.Settings.Physical != profile.Physical)
                throw new ArgumentException("Trace construction and physical cadence differ.");
            var encoded = WorkshopWireFlatBuffers.EncodeCommand(new WorkshopCommand(new(1), WorkshopCommandKind.Construct,
                epoch, default, construction, Session: session, Cadence: profile.Revision, Projection: projection));
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
            _construction = encoded;
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
            // Closed discriminants are serialized numerically at this console boundary.
            Console.WriteLine($"CCGPU_TRACE_BEGIN {_identity} {_epoch.Value} {_count} {chunks} {(int)poseReason} {Convert.ToBase64String(_construction)}");
            for (var ordinal = 0; ordinal < chunks; ordinal++)
            {
                var first = ordinal * ChunkRecords;
                var count = Math.Min(ChunkRecords, _count - first);
                var start = first == 0 ? 0 : _recordEnds[first - 1];
                var encoded = Convert.ToBase64String(_records, start, _recordEnds[first + count - 1] - start);
                Console.WriteLine($"CCGPU_TRACE_CHUNK {_identity} {ordinal} {chunks} {first} {count} {encoded}");
            }
            Console.WriteLine($"CCGPU_TRACE_END {_identity} {_count} {chunks} {(int)poseReason}");
            #endif
            var durationChunks = (_durationCount + ChunkRecords - 1) / ChunkRecords;
            var status = reason == TraceEnd.Complete && _timingValid && _durationCount == _expectedCount - 1
                ? TraceTimingStatus.Complete : TraceTimingStatus.Incomplete;
            // The timing lines bind original little-endian binary64 milliseconds to the construction and terminal reason.
            // Tick IDs are exactly 1..count only when the capture is Complete.
            Console.WriteLine($"CCGPU_TICK_TIMES_BEGIN {_identity} {_epoch.Value} {sizeof(double)} {(uint)_profile.Cadence} {(uint)_profile.Physical} {_profile.Revision.Value} {_expectedCount - 1} {_durationCount} {durationChunks} {(int)status} {Convert.ToBase64String(_construction)} {(int)reason}");
            for (var ordinal = 0; ordinal < durationChunks; ordinal++)
            {
                var first = ordinal * ChunkRecords;
                var count = Math.Min(ChunkRecords, _durationCount - first);
                for (var index = 0; index < count; index++)
                    BinaryPrimitives.WriteDoubleLittleEndian(_durationBytes.AsSpan(index * sizeof(double)), _durations[first + index]);
                var encoded = Convert.ToBase64String(_durationBytes, 0, count * sizeof(double));
                Console.WriteLine($"CCGPU_TICK_TIMES_CHUNK {_identity} {ordinal} {durationChunks} {first + 1} {count} {encoded}");
            }
            Console.WriteLine($"CCGPU_TICK_TIMES_END {_identity} {_durationCount} {durationChunks} {(int)status}");
        }
        catch (Exception error) when (error is IOException or ArgumentException)
        {
            // Missing end/chunks are an explicit incomplete capture to its consumer; the cause goes to the error stream
            // (the fault channel), never the standard stream that just failed.
            WorkshopDiagnostics.Fault(FaultSite.TraceFlush, error);
        }
    }
}
