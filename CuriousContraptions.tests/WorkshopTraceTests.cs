using System.Buffers.Binary;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;

namespace CuriousContraptions.Tests;

// Exercises the actual all-mode observer and diagnostic-only pose recorder.
public sealed class WorkshopTraceTests(ITestOutputHelper testOutput)
{
    private static readonly RuntimeSessionId Session = new(0x20000000000001, ulong.MaxValue);
    private static readonly WorkshopCadenceSettings Settings = WorkshopCadenceSettings.Default();
    private static readonly WorkshopGpuProfile Profile = new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1));
    private static readonly WorkshopConstruction Construction = new(new(7), Settings, new(WorkshopInput.Generic(new PartId("ball"),new(3), 0, 8.875f, 0, 0, 0, 0, 1)));

    [Fact]
    public void RoundedCommittedRotationPreservesDeclaredNumericValues()
    {
        var state = PhysicsGpuAbi.Admission(WorkshopPhysicsCompiler.Compile(Construction, new(101, 207)), new(9), Profile);
        var physical = PhysicsGpuAbi.ReadDynamicBodies(state)[0];
        var rotation = CanonicalRotation.Normalize(0, 0, -.30151367f, MathF.Sqrt(1 - .30151367f * .30151367f));
        var record = WorkshopTraceRecord.Encode(new(new(9), new(0), new([physical with { Rotation = rotation }])), Profile);
        var values = new[] { rotation.X, rotation.Y, rotation.Z, rotation.W };
        for (var index = 0; index < values.Length; index++)
            Assert.Equal(values[index], BinaryPrimitives.ReadSingleLittleEndian(record.AsSpan(WorkshopTraceRecord.HeaderBytes + 32 + index * 4)));
    }

    // Blind hunter 4 (Story 9.4b final round): the retired DeviceLost end (3) is undefined, and a trace refuses to seal
    // with an undefined reason rather than writing it into the record header.
    [Theory]
    [InlineData(3)]
    [InlineData(7)]
    public void ATraceRefusesAnUndefinedEndReason(int reason)
    {
        Assert.False(Enum.IsDefined((TraceEnd)reason));
        var trace = new WorkshopTrace();
        Assert.Throws<ArgumentOutOfRangeException>(() => trace.End((TraceEnd)reason));
        trace.End(TraceEnd.Complete); // control: a defined reason is accepted
    }

    [Theory]
    [InlineData(1u)]
    [InlineData(2u)] // the retired schema whose bodies were 56-byte half-precision-velocity records
    [InlineData(5u)] // the retired trace version: a structurally invalid record now
    public void ATraceRecordsLeadingWordIsZeroPaddingAndAnyOtherValueRejects(uint leading)
    {
        var record = Record(Construction, new(9), Profile);
        Assert.Equal(0u, BinaryPrimitives.ReadUInt32LittleEndian(record));
        Assert.Equal(WorkshopTraceRecord.HeaderBytes + PhysicsBodyWire.ByteLength, record.Length);
        Assert.Equal(Profile, WorkshopTraceRecord.ReadProfile(record));
        BinaryPrimitives.WriteUInt32LittleEndian(record, leading);
        Assert.Throws<ArgumentException>(() => WorkshopTraceRecord.ReadProfile(record));
    }

    public enum TraceRecordDefect { Truncated, Extended, ExtraBody, UnknownCadence, UnknownPhysicalProfile, HeaderPadding }

    /// <summary>Story 9A.1 (F.10): with no version word, the record is admitted by its structure alone: its length must equal the
    /// declared populations exactly, its cadence and physical profile must be defined, and its header padding must be zero.</summary>
    [Theory]
    [InlineData(TraceRecordDefect.Truncated)]
    [InlineData(TraceRecordDefect.Extended)]
    [InlineData(TraceRecordDefect.ExtraBody)]
    [InlineData(TraceRecordDefect.UnknownCadence)]
    [InlineData(TraceRecordDefect.UnknownPhysicalProfile)]
    [InlineData(TraceRecordDefect.HeaderPadding)]
    public void ATraceRecordIsAdmittedByItsStructureAlone(TraceRecordDefect defect)
    {
        var record = Record(Construction, new(9), Profile);
        Assert.Equal(Profile, WorkshopTraceRecord.ReadProfile(record)); // control
        var defective = defect switch
        {
            TraceRecordDefect.Truncated => record[..^1],
            TraceRecordDefect.Extended => [.. record, 0],
            _ => (byte[])record.Clone(),
        };
        switch (defect)
        {
            case TraceRecordDefect.ExtraBody: BinaryPrimitives.WriteUInt32LittleEndian(defective.AsSpan(36), 2); break;
            case TraceRecordDefect.UnknownCadence: BinaryPrimitives.WriteUInt32LittleEndian(defective.AsSpan(4), 99); break;
            case TraceRecordDefect.UnknownPhysicalProfile: BinaryPrimitives.WriteUInt32LittleEndian(defective.AsSpan(32), 99); break;
            case TraceRecordDefect.HeaderPadding: defective[44] = 1; break;
        }
        Assert.Throws<ArgumentException>(() => WorkshopTraceRecord.ReadProfile(defective));
    }

    private static byte[] Record(WorkshopConstruction construction, SimulationEpoch epoch, WorkshopGpuProfile profile)
    {
        var state = PhysicsGpuAbi.Admission(WorkshopPhysicsCompiler.Compile(construction, new(101,207)), epoch, profile);
        var physical = PhysicsGpuAbi.ReadDynamicBodies(state);
        return WorkshopTraceRecord.Encode(new(epoch, new(0), physical), profile);
    }

    private static void Begin(WorkshopTrace trace, RuntimeSessionId session, WorkshopConstruction construction,
        SimulationEpoch epoch, ProjectionEpoch projection, byte[] record)
    {
        trace.Begin(session, CompiledSceneFixture.Compile(construction), epoch, projection, WorkshopTraceRecord.ReadProfile(record));
        Append(trace, record);
    }

    private static void Append(WorkshopTrace trace, byte[] record)
    {
        trace.Append(record);
    }

    [Theory]
    [InlineData(SimulationCadence.Hz60, 1800UL, 15)]
    [InlineData(SimulationCadence.Hz120, 3600UL, 29)]
    [InlineData(SimulationCadence.Hz240, 7200UL, 57)]
    public void CompleteCapturePreservesSessionConstructionAndEveryOriginalRecord(SimulationCadence cadence, ulong ticks, int chunks)
    {
        var trace = new WorkshopTrace();
        var configured = Construction with { Settings = Settings with { Simulation = cadence } };
        var profile = Profile with { Cadence = cadence };
        var record = Record(configured, new(9), profile);
        var count = checked((int)ticks + 1);
        var expected = new byte[count * record.Length];
        Begin(trace, Session, configured, new(9), new(3), record);
        record.CopyTo(expected, 0);
        for (ulong tick = 1; tick <= ticks; tick++)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(record.AsSpan(24), tick);
            record.CopyTo(expected, checked((int)tick * record.Length));
            Append(trace, record);
        }
        var finalRecord = (byte[])record.Clone();
        trace.End(TraceEnd.Complete);
        Assert.Empty(Pose(trace));
        trace.Flush(WorkshopSimulationPhase.Completed);
        trace.Flush(WorkshopSimulationPhase.Completed);
        var lines = Pose(trace);
        Assert.Equal(chunks + 2, lines.Length);
        var header = lines[0];
        Assert.Equal(WorkshopCaptureStage.Begin, header.Stage);
        Assert.Equal(9UL, header.Epoch.Value);
        Assert.Equal((ulong)count, (ulong)header.Count);
        Assert.Equal((ulong)chunks, (ulong)header.Chunks);
        Assert.Equal(TraceEnd.Complete, header.End);
        Assert.Equal(Session, header.Session);
        Assert.Equal(new CompiledSceneId(configured.Revision.Value), header.Scene);
        Assert.Equal(profile.Revision, header.Profile.Revision);
        var observed = new List<WorkshopTraceSample>();
        for (var ordinal = 0; ordinal < chunks; ordinal++)
        {
            var fields = lines[ordinal + 1];
            Assert.Equal(WorkshopCaptureStage.Chunk, fields.Stage);
            Assert.Equal(header.Identity, fields.Identity);
            Assert.Equal((ulong)ordinal, (ulong)fields.Chunk);
            Assert.Equal((ulong)chunks, (ulong)fields.Chunks);
            Assert.Equal((ulong)(ordinal * 128), (ulong)fields.First);
            var chunkCount = Math.Min(128, count - ordinal * 128);
            Assert.Equal((ulong)chunkCount, (ulong)fields.Count);
            Assert.Equal(chunkCount, fields.Samples.Length);
            observed.AddRange(fields.Samples);
        }
        AssertRecordValues(expected, observed.ToArray());
        AssertRecordValues(finalRecord, [WorkshopTraceRecord.Decode(record)]);
        Assert.Equal(WorkshopCaptureStage.End, lines[^1].Stage);
        Assert.Equal(TraceEnd.Complete, lines[^1].End);
        Append(trace, record);
        trace.End(TraceEnd.Reset);
        trace.Flush(WorkshopSimulationPhase.Building);
        Assert.Equal(chunks + 2, Pose(trace).Length);
    }

    // Story 9R.5b (Murdoch-12 R1-B1): the pose trace is sized from its first record's width and the run's tick count, never a fixed
    // byte budget. Twenty moving bodies (1,648-byte records) for the full 7,200-tick run used to overflow the retired
    // 7,201 x 128-byte buffer after about 559 ticks.
    [Fact]
    public void ATwentyBodyTraceOfTheWholeRunCompletesWithEveryRecord()
    {
        var trace = new WorkshopTrace();
        var balls = Enumerable.Range(0, 20)
            .Select(i => (IWorkshopInstance)WorkshopInput.Generic(new PartId("ball"),new((ulong)i + 3), (float)(-6 + i * .6), 8.875f, 0, 0, 0, 0, 1)).ToArray();
        var profile = Profile with { Cadence = SimulationCadence.Hz240 };
        var configured = new WorkshopConstruction(new(7), Settings with { Simulation = SimulationCadence.Hz240 }, new(balls));
        var record = Record(configured, new(9), profile);
        Assert.Equal(WorkshopTraceRecord.HeaderBytes + 20 * PhysicsBodyWire.ByteLength, record.Length);
        Begin(trace, Session, configured, new(9), new(3), record);
        const ulong ticks = 7200;
        for (ulong tick = 1; tick <= ticks; tick++)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(record.AsSpan(24), tick);
            Append(trace, record);
        }
        trace.End(TraceEnd.Complete);
        trace.Flush(WorkshopSimulationPhase.Completed);
        var header = Pose(trace)[0];
        Assert.Equal(WorkshopTraceRecord.HeaderBytes + 20 * PhysicsBodyWire.ByteLength, record.Length);
        Assert.Equal(ticks + 1, (ulong)header.Count);
        Assert.Equal(TraceEnd.Complete, header.End);
        Assert.Equal(WorkshopCaptureStage.End, Pose(trace)[^1].Stage);
        Assert.Equal(TraceEnd.Complete, Pose(trace)[^1].End);
    }

    [Fact]
    public void InvalidSessionCannotStartOrContaminateTheNextRun()
    {
        var trace = new WorkshopTrace();
        var record = Record(Construction, new(9), Profile);
        Begin(trace, default, Construction, new(9), new(3), record);
        Append(trace, record);
        trace.End(TraceEnd.Complete);
        trace.Flush(WorkshopSimulationPhase.Completed);
        Assert.Empty(Pose(trace));
        Begin(trace, Session, Construction, new(9), new(3), record);
        trace.End(TraceEnd.Reset);
        trace.Flush(WorkshopSimulationPhase.Building);
        Assert.Equal(3, Pose(trace).Length);
        var header = Pose(trace)[0];
        Assert.Equal(TraceEnd.Reset, header.End);
        Assert.Equal(Session, header.Session);
    }

    [Fact]
    public void RestartClosesThePreviousPartialCaptureBeforeTheNewSession()
    {
        var trace = new WorkshopTrace();
        Begin(trace, Session, Construction, new(9), new(3), Record(Construction, new(9), Profile));
        var previousRecord = Record(Construction, new(9), Profile);
        var nextRecord = Record(Construction, new(10), Profile);
        var nextSession = new RuntimeSessionId(23, 41);
        Begin(trace, nextSession, Construction, new(10), new(3), Record(Construction, new(10), Profile));
        trace.End(TraceEnd.Reset);
        trace.Flush(WorkshopSimulationPhase.Building);
        var lines = Pose(trace);
        Assert.Equal(6, lines.Length);
        var previous = lines[0];
        var next = lines[3];
        Assert.Equal(TraceEnd.Reset, previous.End);
        Assert.Equal(TraceEnd.Reset, next.End);
        Assert.True(next.Identity > previous.Identity);
        Assert.Equal(9UL, previous.Epoch.Value);
        Assert.Equal(10UL, next.Epoch.Value);
        AssertRecordValues(previousRecord, lines[1].Samples.ToArray());
        AssertRecordValues(nextRecord, lines[4].Samples.ToArray());
        Assert.Equal(Session, previous.Session);
        Assert.Equal(nextSession, next.Session);
    }

    [Fact]
    public void MissingOrOutOfOrderRecordsCannotClaimComplete()
    {
        var trace = new WorkshopTrace();
        var record = Record(Construction, new(9), Profile);
        Begin(trace, Session, Construction, new(9), new(3), record);
        BinaryPrimitives.WriteUInt64LittleEndian(record.AsSpan(24), 2);
        Append(trace, record);
        trace.End(TraceEnd.Complete);
        trace.Flush(WorkshopSimulationPhase.Completed);
        Assert.Equal(3, Pose(trace).Length);
        var header = Pose(trace)[0];
        Assert.Equal(1UL, (ulong)header.Count);
        Assert.Equal(TraceEnd.InvalidRecord, header.End);
        var before = Pose(trace).Length;
        Begin(trace, Session, Construction, new(10), new(3), Record(Construction, new(10), Profile));
        trace.End(TraceEnd.Complete);
        trace.Flush(WorkshopSimulationPhase.Completed);
        Assert.Equal(before + 3, Pose(trace).Length);
        Assert.Equal(TraceEnd.InvalidRecord, Pose(trace)[before].End);
    }

    [Theory]
    [InlineData(WorkshopSimulationPhase.Running)]
    [InlineData(WorkshopSimulationPhase.Starting)]
    [InlineData(WorkshopSimulationPhase.Resetting)]
    public void InvalidCaptureRemainsSealedUntilStopped(WorkshopSimulationPhase phase)
    {
        foreach (var reason in new[] { TraceEnd.InvalidRecord, TraceEnd.Overflow })
        {
                var trace = new WorkshopTrace();
            var record = Record(Construction, new(9), Profile);
            Begin(trace, Session, Construction, new(9), new(3), record);
            if (reason == TraceEnd.Overflow)
                for (ulong tick = 1; tick <= 3600; tick++)
                {
                    BinaryPrimitives.WriteUInt64LittleEndian(record.AsSpan(24), tick);
                    Append(trace, record);
                }
            else BinaryPrimitives.WriteUInt64LittleEndian(record.AsSpan(24), 2);
            Append(trace, record);
            trace.Flush(phase);
            Assert.Empty(Pose(trace));
            trace.End(TraceEnd.Fault);
            trace.Flush(WorkshopSimulationPhase.Faulted);
            Assert.Equal(reason == TraceEnd.Overflow ? 31 : 3, Pose(trace).Length);
            Assert.Equal(reason, Pose(trace)[0].End);
        }
    }

    [Theory]
    [InlineData(WorkshopSimulationPhase.Building)]
    [InlineData(WorkshopSimulationPhase.Disposed)]
    [InlineData(WorkshopSimulationPhase.Faulted)]
    public void TerminalSealingPreservesDecodedStateUntilOneFlush(WorkshopSimulationPhase phase)
    {
        var reason = phase switch
        {
            WorkshopSimulationPhase.Building => TraceEnd.Reset,
            WorkshopSimulationPhase.Disposed => TraceEnd.Disposed,
            WorkshopSimulationPhase.Faulted => TraceEnd.Fault,
            _ => throw new ArgumentOutOfRangeException(nameof(phase))
        };
        var trace = new WorkshopTrace();
        var record = Record(Construction, new(9), Profile);
        var expected = (byte[])record.Clone();
        Begin(trace, Session, Construction, new(9), new(3), record);
        trace.End(reason);
        record.AsSpan().Clear();
        Append(trace, record);
        Assert.Empty(Pose(trace));
        trace.Flush(phase);
        AssertRecordValues(expected, Pose(trace)[1].Samples.ToArray());
        Assert.Equal(reason, Pose(trace)[0].End);
        trace.Flush(phase);
        Assert.Equal(3, Pose(trace).Length);
    }

    [Fact]
    public void FailedOutputIsContainedAndCannotContaminateTheNextCapture()
    {
        var failed = true;
        var diagnostics = new WorkshopDiagnostics(_ => { if (failed) throw new IOException("Deliberate diagnostic output failure."); });
        var trace = new WorkshopTrace(diagnostics);
        Begin(trace, Session, Construction, new(9), new(3), Record(Construction, new(9), Profile));
        trace.End(TraceEnd.Reset);
        trace.Flush(WorkshopSimulationPhase.Building);
        Assert.Contains("Deliberate diagnostic output failure.", diagnostics.Incomplete);
        var priorCount = Pose(trace).Length;
        trace.Flush(WorkshopSimulationPhase.Building);
        Assert.Equal(priorCount, Pose(trace).Length);
        failed = false;
        Begin(trace, Session, Construction, new(10), new(3), Record(Construction, new(10), Profile));
        trace.End(TraceEnd.Reset);
        trace.Flush(WorkshopSimulationPhase.Building);
        Assert.Equal(priorCount + 3, Pose(trace).Length);
        Assert.Equal(10UL, Pose(trace)[priorCount].Epoch.Value);
    }


    [Fact]
    public void ASixteenBodyWholeRunKeepsEveryPoseRecordAndCompleteScalarTiming()
    {
        var trace = new WorkshopTrace();
        var bodies = new PhysicsBodyReadSet(Enumerable.Range(1, 16).Select(id =>
            new PhysicsBodyRead(new(new((ulong)id), 9, 0, default, default, default),
                CanonicalRotation.Identity, default, default)).ToArray());
        var record = WorkshopTraceRecord.Encode(new(new(9), new(0), bodies), Profile);
        Begin(trace, Session, Construction, new(9), new(3), record);
        for (ulong tick = 1; tick <= Profile.RunTickLimit; tick++)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(record.AsSpan(24), tick);
            Append(trace, record);
            trace.RecordTickDuration(new(tick), .75);
        }
        trace.End(TraceEnd.Complete);
        trace.Flush(WorkshopSimulationPhase.Completed);

        Assert.Equal(TraceEnd.Complete, Pose(trace)[0].End);
        Assert.Equal(Profile.RunTickLimit + 1, (ulong)Pose(trace)[0].Count);
        Assert.Equal(Profile.RunTickLimit, (ulong)Timing(trace)[0].Count);
        Assert.Equal(TraceTimingStatus.Complete, Timing(trace)[0].Status);
        ulong nextTick = 1;
        foreach (var chunk in Timing(trace).Where(record => record.Stage == WorkshopCaptureStage.Chunk))
        {
            Assert.Equal(nextTick, (ulong)chunk.First);
            Assert.Equal(chunk.Count, chunk.Milliseconds.Length);
            Assert.All(chunk.Milliseconds, duration => Assert.Equal(.75, duration));
            nextTick += (ulong)chunk.Count;
        }
        Assert.Equal(Profile.RunTickLimit + 1, nextTick);
    }

    [Theory]
    [InlineData(SimulationCadence.Hz60)]
    [InlineData(SimulationCadence.Hz120)]
    [InlineData(SimulationCadence.Hz240)]
    public void CompleteTimingRetainsEveryOriginalSpanAtTheTerminalBarrier(SimulationCadence cadence)
    {
        var trace = new WorkshopTrace();
        var profile = Profile with { Cadence = cadence };
        var construction = Construction with { Settings = Settings with { Simulation = cadence } };
        var record = Record(construction, new(9), profile);
        Begin(trace, Session, construction, new(9), new(3), record);
        var expected = new double[checked((int)profile.RunTickLimit)];
        for (var index = 0; index < expected.Length; index++)
        {
            var tick = (ulong)index + 1;
            BinaryPrimitives.WriteUInt64LittleEndian(record.AsSpan(24), tick);
            Append(trace, record);
            expected[index] = .5 + index / 1024d;
            trace.RecordTickDuration(new(tick), expected[index]);
        }
        trace.End(TraceEnd.Complete);
        trace.Flush(WorkshopSimulationPhase.Running);
        Assert.Empty(trace.Diagnostics.Snapshot());
        trace.Flush(WorkshopSimulationPhase.Completed);

        var records = Timing(trace);
        var header = records[0];
        Assert.Equal(WorkshopCaptureStage.Begin, header.Stage);
        Assert.Equal(9UL, header.Epoch.Value);
        Assert.Equal(profile, header.Profile);
        Assert.Equal(profile.RunTickLimit, (ulong)header.ExpectedCount);
        Assert.Equal(profile.RunTickLimit, (ulong)header.Count);
        Assert.Equal(TraceTimingStatus.Complete, header.Status);
        Assert.Equal(Session, header.Session);
        Assert.Equal(new CompiledSceneId(construction.Revision.Value), header.Scene);
        Assert.Equal(new SimulationEpoch(9), header.Epoch);
        Assert.Equal(TraceEnd.Complete, header.End);
        var observed = new List<double>();
        for (var ordinal = 0; ordinal < header.Chunks; ordinal++)
        {
            var chunk = records[ordinal + 1];
            Assert.Equal(WorkshopCaptureStage.Chunk, chunk.Stage);
            Assert.Equal(header.Identity, chunk.Identity);
            Assert.Equal(ordinal, chunk.Chunk);
            Assert.Equal(header.Chunks, chunk.Chunks);
            Assert.Equal(observed.Count + 1, chunk.First);
            Assert.InRange(chunk.Count, 1, 128);
            Assert.Equal(chunk.Count, chunk.Milliseconds.Length);
            observed.AddRange(chunk.Milliseconds);
        }
        Assert.Equal(expected, observed);
        Assert.Equal(WorkshopCaptureStage.End, records[^1].Stage);
        Assert.Equal(expected.Length, records[^1].Count);
        Assert.Equal(header.Chunks, records[^1].Chunks);
        Assert.Equal(TraceTimingStatus.Complete, records[^1].Status);
        trace.Flush(WorkshopSimulationPhase.Completed);
        Assert.Equal(records.Length, Timing(trace).Length);
    }

    public enum TimingDefect { Missing, Duplicate, NonFinite, Negative, Overflow }
    [Theory]
    [InlineData(TimingDefect.Missing)]
    [InlineData(TimingDefect.Duplicate)]
    [InlineData(TimingDefect.NonFinite)]
    [InlineData(TimingDefect.Negative)]
    [InlineData(TimingDefect.Overflow)]
    public void TimingDefectsRemainIncompleteAndTheNextRunHasFreshOwnership(TimingDefect defect)
    {
        var trace = new WorkshopTrace();
        for (ulong epoch = 9; epoch <= 10; epoch++)
        {
            var record = Record(Construction, new(epoch), Profile);
            Begin(trace, Session, Construction, new(epoch), new(3), record);
            for (ulong tick = 1; tick <= Profile.RunTickLimit; tick++)
            {
                BinaryPrimitives.WriteUInt64LittleEndian(record.AsSpan(24), tick);
                Append(trace, record);
                if (epoch == 9 && tick == 1 && defect == TimingDefect.Missing) continue;
                trace.RecordTickDuration(new(tick), epoch == 9 && tick == 1 && defect == TimingDefect.NonFinite ? double.NaN :
                    epoch == 9 && tick == 1 && defect == TimingDefect.Negative ? -.25 : .75);
                if (epoch == 9 && tick == 1 && defect == TimingDefect.Duplicate)
                    trace.RecordTickDuration(new(tick), .75);
            }
            if (epoch == 9 && defect == TimingDefect.Overflow)
                trace.RecordTickDuration(new(Profile.RunTickLimit + 1), .75);
            trace.End(TraceEnd.Complete);
            trace.Flush(WorkshopSimulationPhase.Completed);
            var header = Timing(trace).Last(record => record.Stage == WorkshopCaptureStage.Begin);
            Assert.Equal(epoch, header.Epoch.Value);
            Assert.Equal(epoch == 9 ? TraceTimingStatus.Incomplete : TraceTimingStatus.Complete, header.Status);
            if (epoch == 10) Assert.Equal(Profile.RunTickLimit, (ulong)header.Count);
        }
    }

    public enum TimingTerminal { Reset, Disposed, Fault }
    [Theory]
    [InlineData(TimingTerminal.Reset)]
    [InlineData(TimingTerminal.Disposed)]
    [InlineData(TimingTerminal.Fault)]
    public void EarlyTerminalTimingIsIncompleteAndFlushesOnce(TimingTerminal terminal)
    {
        var reason = terminal switch
        {
            TimingTerminal.Reset => TraceEnd.Reset,
            TimingTerminal.Disposed => TraceEnd.Disposed,
            TimingTerminal.Fault => TraceEnd.Fault,
            _ => throw new ArgumentOutOfRangeException(nameof(terminal))
        };
        var trace = new WorkshopTrace();
        Begin(trace, Session, Construction, new(9), new(3), Record(Construction, new(9), Profile));
        trace.End(reason);
        trace.Flush(WorkshopSimulationPhase.Running);
        Assert.Empty(trace.Diagnostics.Snapshot());
        trace.Flush(WorkshopSimulationPhase.Building);
        var original = Timing(trace);
        var header = original[0];
        Assert.Equal(TraceTimingStatus.Incomplete, header.Status);
        Assert.Equal(reason, header.End);
        trace.Flush(WorkshopSimulationPhase.Building);
        Assert.Equal(original.Length, Timing(trace).Length);
    }

    [Fact]
    public void TimingOutputFailureCannotEscapeOrContaminateNextRun()
    {
        var failed = true;
        var delivered = new List<WorkshopCaptureDiagnostic>();
        var diagnostics = new WorkshopDiagnostics(record =>
        {
            if (record is not WorkshopCaptureDiagnostic { Kind: WorkshopCaptureKind.Timing } timing) return;
            if (failed) throw new IOException("Deliberate timing delivery failure.");
            delivered.Add(timing);
        });
        var trace = new WorkshopTrace(diagnostics);
        Begin(trace, Session, Construction, new(9), new(3), Record(Construction, new(9), Profile));
        trace.End(TraceEnd.Reset);
        trace.Flush(WorkshopSimulationPhase.Building);
        Assert.Contains("Deliberate timing delivery failure.", diagnostics.Incomplete);
        trace.Flush(WorkshopSimulationPhase.Building);
        Assert.Empty(delivered);
        failed = false;
        Begin(trace, Session, Construction, new(10), new(3), Record(Construction, new(10), Profile));
        trace.End(TraceEnd.Reset);
        trace.Flush(WorkshopSimulationPhase.Building);
        Assert.All(delivered, record => Assert.Equal(10UL, record.Epoch.Value));
        Assert.Equal(WorkshopCaptureStage.Begin, delivered[0].Stage);
        Assert.Equal(WorkshopCaptureStage.End, delivered[^1].Stage);
    }

    public enum BeginDefect { Session, ProfileMismatch, UndefinedProfile, Projection }
    [Theory]
    [InlineData(BeginDefect.Session)]
    [InlineData(BeginDefect.ProfileMismatch)]
    [InlineData(BeginDefect.UndefinedProfile)]
    [InlineData(BeginDefect.Projection)]
    public void InvalidTimingIdentityCannotStartOrContaminateTheNextRun(BeginDefect defect)
    {
        var trace = new WorkshopTrace();
        var profile = defect switch
        {
            BeginDefect.ProfileMismatch => Profile with { Cadence = SimulationCadence.Hz60 },
            BeginDefect.UndefinedProfile => Profile with { Cadence = (SimulationCadence)999 },
            _ => Profile
        };
        trace.Begin(defect == BeginDefect.Session ? default : Session, CompiledSceneFixture.Compile(Construction), new(9),
            defect == BeginDefect.Projection ? default : new(3), profile);
        trace.RecordTickDuration(new(1), .25);
        trace.End(TraceEnd.Complete);
        trace.Flush(WorkshopSimulationPhase.Completed);
        Assert.Empty(trace.Diagnostics.Snapshot());
        Begin(trace, Session, Construction, new(10), new(3), Record(Construction, new(10), Profile));
        trace.End(TraceEnd.Reset);
        trace.Flush(WorkshopSimulationPhase.Building);
        var header = Timing(trace)[0];
        Assert.Equal(10UL, header.Epoch.Value);
        Assert.Equal(Session, header.Session);
    }

    [Fact]
    // The measuring loop is fully optimised too. Story 9.4b's rare allocation in this window has an open cause (P0-034 /
    // Story 16.1): tiering was never confirmed, and an ablation without this attribute did not fail either.
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
    public void DurationRetentionHasNoRoutineAllocationInNativeExecution()
    {
        var trace = new WorkshopTrace();
        var profile = Profile with { Cadence = SimulationCadence.Hz240 };
        var construction = Construction with { Settings = Settings with { Simulation = SimulationCadence.Hz240 } };
        long allocated = 0, elapsed = 0;
        var growths = 0;
        var allocatingCalls = new List<string>(4); // diagnosis: which call allocated, and whether a GC ran inside it
        for (ulong epoch = 9; epoch <= 10; epoch++)
        {
            var record = Record(construction, new(epoch), profile);
            Begin(trace, Session, construction, new(epoch), new(3), record);
            for (ulong tick = 1; tick <= profile.RunTickLimit; tick++)
            {
                BinaryPrimitives.WriteUInt64LittleEndian(record.AsSpan(24), tick);
                Append(trace, record);
                // Only the call itself is inside the allocation window; the timestamps are outside it. This narrows what
                // the window measures; it does not prove the cause of Story 9.4b's rare failure, which is open (Story 9.4b
                // spec §12, P0-034 / Story 16.1).
                var capacityBefore = trace.DurationCapacity;
                var beforeTime = System.Diagnostics.Stopwatch.GetTimestamp();
                var beforeCollections = GC.CollectionCount(0);
                var beforeJit = System.Runtime.JitInfo.GetCompiledMethodCount(currentThread: true);
                var beforeBytes = GC.GetAllocatedBytesForCurrentThread();
                trace.RecordTickDuration(new(tick), .75);
                var afterBytes = GC.GetAllocatedBytesForCurrentThread();
                var afterJit = System.Runtime.JitInfo.GetCompiledMethodCount(currentThread: true);
                var afterTime = System.Diagnostics.Stopwatch.GetTimestamp();
                if (epoch != 10) continue;
                if (trace.DurationCapacity != capacityBefore)
                {
                    growths++;
                    Assert.True(afterBytes > beforeBytes);
                    continue;
                }
                allocated += afterBytes - beforeBytes; elapsed += afterTime - beforeTime;
                if (afterBytes != beforeBytes && allocatingCalls.Count < 4)
                    allocatingCalls.Add($"tick {tick}: {afterBytes - beforeBytes} B, gen0 collections {GC.CollectionCount(0) - beforeCollections}, methods jitted on this thread {afterJit - beforeJit}");
            }
            trace.End(TraceEnd.Complete);
            trace.Flush(WorkshopSimulationPhase.Completed);
        }
        Assert.True(growths > 0, "the full duration history crosses actual storage watermarks");
        Assert.True(allocated == 0, $"{allocated} B allocated by RecordTickDuration: {string.Join("; ", allocatingCalls)}");
        testOutput.WriteLine($"Native retention: {profile.RunTickLimit} calls, {allocated} allocated bytes, {elapsed * 1000d / System.Diagnostics.Stopwatch.Frequency:R} total observed milliseconds including timestamp overhead. Not a WASM timing bound.");
    }

    [Fact]
    public void TraceDurationsGrowAtTheirCommonWatermarkAndResetWithTheOwningRun()
    {
        var trace = new WorkshopTrace();
        Assert.Equal(32, trace.DurationCapacity);
        Begin(trace, Session, Construction, new(9), new(3), Record(Construction, new(9), Profile));
        for (ulong tick = 1; tick <= 24; tick++) trace.RecordTickDuration(new(tick), tick * .25);
        Assert.Equal(32, trace.DurationCapacity);
        trace.RecordTickDuration(new(25), 6.25);
        Assert.Equal(64, trace.DurationCapacity);
        trace.End(TraceEnd.Reset);
        Begin(trace, Session, Construction, new(10), new(3), Record(Construction, new(10), Profile));
        Assert.Equal(32, trace.DurationCapacity);
        var chunk = Timing(trace)[1].Milliseconds;
        for (var i = 0; i < 25; i++) Assert.Equal((i + 1) * .25, chunk[i]);
    }

    [Fact]
    public void TraceRetainsChangingSparseCaptureWidthsAcrossWatermarkGrowth()
    {
        var trace = new WorkshopTrace();
        Assert.Equal(16384, trace.RecordCapacityBytes);
        Assert.Equal(32, trace.RecordOffsetCapacity);
        var physical = PhysicsGpuAbi.ReadDynamicBodies(PhysicsGpuAbi.Admission(WorkshopPhysicsCompiler.Compile(Construction, new(101, 207)), new(9), Profile))[0];
        var expected = new List<byte>();
        trace.Begin(Session, CompiledSceneFixture.Compile(Construction), new(9), new(3), Profile);
        for (ulong tick = 0; tick < 100; tick++)
        {
            var captures = tick == 0 ? new PhysicsCaptureRead([]) : new PhysicsCaptureRead([new(new(17), new(3), CaptureLatchPhase.Latched, 1, 0)]);
            var body = physical with { Body = physical.Body with { Tick = tick } };
            var record = WorkshopTraceRecord.Encode(new(new(9), new(tick), new([body]), Captures: captures), Profile);
            expected.AddRange(record);
            trace.Append(record);
            if (tick == 23) Assert.Equal(32, trace.RecordOffsetCapacity);
            if (tick == 24) Assert.Equal(64, trace.RecordOffsetCapacity);
        }
        Assert.True(trace.RecordCapacityBytes > 16384);
        trace.End(TraceEnd.Reset); trace.Flush(WorkshopSimulationPhase.Building);
        var observed = Pose(trace).Where(record => record.Stage == WorkshopCaptureStage.Chunk).SelectMany(record => record.Samples).ToArray();
        AssertRecordValues(expected.ToArray(), observed);
        trace.Begin(Session, CompiledSceneFixture.Compile(Construction), new(10), new(3), Profile);
        Assert.Equal(16384, trace.RecordCapacityBytes); Assert.Equal(32, trace.RecordOffsetCapacity);
    }

    private static WorkshopCaptureDiagnostic[] Pose(WorkshopTrace trace) =>
        trace.Diagnostics.Snapshot().OfType<WorkshopCaptureDiagnostic>().Where(record => record.Kind == WorkshopCaptureKind.Pose).ToArray();
    private static WorkshopCaptureDiagnostic[] Timing(WorkshopTrace trace) =>
        trace.Diagnostics.Snapshot().OfType<WorkshopCaptureDiagnostic>().Where(record => record.Kind == WorkshopCaptureKind.Timing).ToArray();

    private static void AssertRecordValues(byte[] expected, WorkshopTraceSample[] actual)
    {
        var at = 0;
        foreach (var sample in actual)
        {
            var bytes = expected.AsSpan(at);
            var bodies = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes[36..]);
            var captures = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes[40..]);
            var epoch = new SimulationEpoch(BinaryPrimitives.ReadUInt64LittleEndian(bytes[16..]));
            var tick = new SimulationTick(BinaryPrimitives.ReadUInt64LittleEndian(bytes[24..]));
            Assert.Equal(epoch, sample.Epoch);
            Assert.Equal(tick, sample.Tick);
            Assert.Equal(bodies, sample.Bodies.Count);
            Assert.Equal(captures, sample.Captures.Count);
            for (var i = 0; i < bodies; i++)
            {
                var offset = WorkshopTraceRecord.HeaderBytes + i * PhysicsBodyWire.ByteLength;
                var first = PhysicsBodyWire.Read(bytes.Slice(offset, PhysicsBodyWire.ByteLength), epoch, tick);
                var second = sample.Bodies[i];
                Assert.Equal((first.Body.Id, first.Body.Cell, first.Body.Local, first.Body.Velocity, first.Rotation, first.AngularVelocity, first.LocalCentreOfMass),
                    (second.Body.Id, second.Body.Cell, second.Body.Local, second.Body.Velocity, second.Rotation, second.AngularVelocity, second.LocalCentreOfMass));
            }
            for (var i = 0; i < captures; i++)
            {
                var value = bytes[(WorkshopTraceRecord.HeaderBytes + bodies * PhysicsBodyWire.ByteLength + i * WorkshopTraceRecord.CaptureBytes)..];
                var latch = sample.Captures[i];
                Assert.Equal(BinaryPrimitives.ReadUInt64LittleEndian(value), latch.Sensor.Value);
                Assert.Equal(BinaryPrimitives.ReadUInt64LittleEndian(value[8..]), latch.Target.Value);
                Assert.Equal((CaptureLatchPhase)BinaryPrimitives.ReadUInt32LittleEndian(value[16..]), latch.Phase);
                Assert.Equal(BinaryPrimitives.ReadUInt32LittleEndian(value[20..]), latch.EventOrdinal);
                Assert.Equal(BinaryPrimitives.ReadSingleLittleEndian(value[24..]), latch.EventPhase);
            }
            at += WorkshopTraceRecord.ByteLength(bodies, captures);
        }
        Assert.Equal(expected.Length, at);
    }
}
