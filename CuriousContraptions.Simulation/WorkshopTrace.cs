using System.Buffers.Binary;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

// Observational only. All methods contain diagnostic failure; none can reject or change a commit.
internal enum TraceEnd { Complete, Reset, Disposed, DeviceLost, Fault, InvalidRecord, Overflow }
internal enum TraceTimingStatus { Complete, Incomplete }
internal enum TraceTimingVersion : uint { NativeMilliseconds = 2 }
internal sealed class WorkshopTrace
{
    private const int Capacity = 7201;
    private const int ChunkRecords = 128;
    #if PLAYTEST
    private readonly byte[] _records = new byte[Capacity * WorkshopTraceRecord.ByteLength];
    private int _count;
    #endif
    private readonly double[] _durations = new double[Capacity - 1];
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
            var encoded = WorkshopWire.Encode(new WorkshopCommand(new(1), WorkshopCommandKind.Construct,
                epoch, default, construction, Session: session, Cadence: profile.Revision, Projection: projection));
            var expectedCount = checked((int)profile.RunTickLimit + 1);
            if (expectedCount > Capacity) throw new ArgumentException("Trace profile exceeds its reservation.");
            if (_active) End(TraceEnd.Reset);
            // Preserve the preceding capture before this one buffer is reused. No new RunLoop exists yet.
            FlushSealed();
            if (_identity == ulong.MaxValue) return;
            _identity++;
            _epoch = epoch;
            #if PLAYTEST
            _count = 0;
            #endif
            _durationCount = 0;
            _timingValid = true;
            _construction = encoded;
            _profile = profile;
            _expectedCount = expectedCount;
            _active = true;
        }
        catch { End(TraceEnd.InvalidRecord); }
    }
    #if PLAYTEST
    internal void Append(byte[] record)
    {
        if (!_active) return;
        try
        {
            if (_count == _expectedCount) { End(TraceEnd.Overflow); return; }
            if (record.Length != WorkshopTraceRecord.ByteLength || WorkshopTraceRecord.ReadProfile(record) != _profile ||
                BinaryPrimitives.ReadUInt64LittleEndian(record.AsSpan(16)) != _epoch.Value ||
                BinaryPrimitives.ReadUInt64LittleEndian(record.AsSpan(24)) != (ulong)_count)
            { End(TraceEnd.InvalidRecord); return; }
            record.CopyTo(_records, _count * WorkshopTraceRecord.ByteLength);
            _count++;
        }
        catch { End(TraceEnd.InvalidRecord); }
    }
    #endif
    // Host observational milliseconds only. This is the complete critical-path wall span,
    // including awaits and publication, not exclusive CPU time or numerical game state.
    internal void RecordTickDuration(SimulationTick tick, double milliseconds)
    {
        if (!_active) return;
        try
        {
            if (!_timingValid || !double.IsFinite(milliseconds) || milliseconds < 0 ||
                _durationCount >= _durations.Length || tick.Value != (ulong)_durationCount + 1 ||
                tick.Value >= (ulong)_expectedCount
                #if PLAYTEST
                || _count != _durationCount + 2
                #endif
                )
            { _timingValid = false; return; }
            _durations[_durationCount++] = milliseconds;
        }
        catch { _timingValid = false; }
    }

    internal void End(TraceEnd reason)
    {
        if (!_active) return;
        _active = false;
        #if PLAYTEST
        if (reason == TraceEnd.Complete && _count != _expectedCount) reason = TraceEnd.InvalidRecord;
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
            // Closed discriminants are serialized numerically at this console boundary.
            Console.WriteLine($"CCGPU_TRACE_BEGIN {_identity} {_epoch.Value} {(uint)WorkshopTraceVersion.GenericPoseAndCapture} {WorkshopTraceRecord.ByteLength} {_count} {chunks} {(int)reason} {Convert.ToBase64String(_construction)}");
            for (var ordinal = 0; ordinal < chunks; ordinal++)
            {
                var first = ordinal * ChunkRecords;
                var count = Math.Min(ChunkRecords, _count - first);
                var encoded = Convert.ToBase64String(_records, first * WorkshopTraceRecord.ByteLength,
                    count * WorkshopTraceRecord.ByteLength);
                Console.WriteLine($"CCGPU_TRACE_CHUNK {_identity} {ordinal} {chunks} {first} {count} {encoded}");
            }
            Console.WriteLine($"CCGPU_TRACE_END {_identity} {_count} {chunks} {(int)reason}");
            #endif
            var durationChunks = (_durationCount + ChunkRecords - 1) / ChunkRecords;
            var status = reason == TraceEnd.Complete && _timingValid && _durationCount == _expectedCount - 1
                ? TraceTimingStatus.Complete : TraceTimingStatus.Incomplete;
            // Version 2 binds original little-endian binary64 milliseconds to the construction and terminal reason.
            // Tick IDs are exactly 1..count only when the capture is Complete.
            Console.WriteLine($"CCGPU_TICK_TIMES_BEGIN {_identity} {_epoch.Value} {(uint)TraceTimingVersion.NativeMilliseconds} {sizeof(double)} {(uint)_profile.Cadence} {(uint)_profile.Physical} {_profile.Revision.Value} {_expectedCount - 1} {_durationCount} {durationChunks} {(int)status} {Convert.ToBase64String(_construction)} {(int)reason}");
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
        catch { /* Missing end/chunks are an explicit incomplete capture to its consumer. */ }
    }
}
