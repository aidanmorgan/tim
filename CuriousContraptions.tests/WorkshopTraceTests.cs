using System.Buffers.Binary;
using System.Globalization;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;

namespace CuriousContraptions.Tests;

// Exercises the actual all-mode observer and diagnostic-only pose recorder.
public sealed class WorkshopTraceTests(ITestOutputHelper testOutput)
{
    private static readonly RuntimeSessionId Session = new(0x20000000000001, ulong.MaxValue);
    private static readonly WorkshopCadenceSettings Settings = WorkshopCadenceSettings.Default();
    private static readonly WorkshopGpuProfile Profile = new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1));
    private static readonly WorkshopConstruction Construction = new(new(7), Settings, new(WorkshopInput.Generic(PartId.Basketball,new(3), 0, 8.875, 0, 0, 0, 0, 1)));

    [Fact]
    public void RoundedCommittedRotationPreservesTraceBits()
    {
        var state = PhysicsGpuAbi.Admission(WorkshopPhysicsCompiler.Compile(Construction, new(101, 207)), new(9), Profile);
        var physical = PhysicsGpuAbi.ReadDynamicBodies(state)[0];
        var rotation = CanonicalRotation.Normalize(0, 0, -.30151367f, MathF.Sqrt(1 - .30151367f * .30151367f));
        var record = WorkshopTraceRecord.Encode(new(new(9), new(0), new([physical with { Rotation = rotation }])), Profile);
        var values = new[] { rotation.X, rotation.Y, rotation.Z, rotation.W };
        for (var index = 0; index < values.Length; index++)
            Assert.Equal(BitConverter.SingleToUInt32Bits(values[index]),
                BinaryPrimitives.ReadUInt32LittleEndian(record.AsSpan(WorkshopTraceRecord.HeaderBytes + 32 + index * 4)));
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
        trace.Begin(session, construction, epoch, projection, WorkshopTraceRecord.ReadProfile(record));
        Append(trace, record);
    }

    private static void Append(WorkshopTrace trace, byte[] record)
    {
#if PLAYTEST
        trace.Append(record);
#endif
    }

#if PLAYTEST
    [Theory]
    [InlineData(SimulationCadence.Hz60, 1800UL, 15)]
    [InlineData(SimulationCadence.Hz120, 3600UL, 29)]
    [InlineData(SimulationCadence.Hz240, 7200UL, 57)]
    public void CompleteCapturePreservesSessionConstructionAndEveryOriginalRecord(SimulationCadence cadence, ulong ticks, int chunks)
    {
        using var output = new CapturedConsole();
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
        Assert.Empty(output.TraceLines);
        trace.Flush(WorkshopSimulationPhase.Completed);
        trace.Flush(WorkshopSimulationPhase.Completed);
        var lines = output.TraceLines;
        Assert.Equal(chunks + 2, lines.Length);
        var header = lines[0].Split(' ');
        Assert.Equal("CCGPU_TRACE_BEGIN", header[0]);
        Assert.Equal(9UL, Number(header[2]));
        Assert.Equal((ulong)count, Number(header[4]));
        Assert.Equal((ulong)chunks, Number(header[5]));
        Assert.Equal(TraceEnd.Complete, Reason(header[6]));
        var construction = WorkshopWireFlatBuffers.DecodeCommand(Convert.FromBase64String(header[7]));
        Assert.Equal(Session, construction.Session);
        Assert.Equal(configured, construction.Construction);
        Assert.Equal(profile.Revision, construction.Cadence);
        Assert.Equal(new ProjectionEpoch(3), construction.Projection);
        Assert.Equal(new SimulationEpoch(9), construction.Epoch);
        var observed = new List<byte>(expected.Length);
        for (var ordinal = 0; ordinal < chunks; ordinal++)
        {
            var fields = lines[ordinal + 1].Split(' ');
            Assert.Equal("CCGPU_TRACE_CHUNK", fields[0]);
            Assert.Equal(header[1], fields[1]);
            Assert.Equal((ulong)ordinal, Number(fields[2]));
            Assert.Equal((ulong)chunks, Number(fields[3]));
            Assert.Equal((ulong)(ordinal * 128), Number(fields[4]));
            var chunkCount = Math.Min(128, count - ordinal * 128);
            Assert.Equal((ulong)chunkCount, Number(fields[5]));
            var bytes = Convert.FromBase64String(fields[6]);
            Assert.Equal(chunkCount * record.Length, bytes.Length);
            observed.AddRange(bytes);
        }
        Assert.Equal(expected, observed.ToArray());
        Assert.Equal(finalRecord, record);
        Assert.Equal($"CCGPU_TRACE_END {header[1]} {count} {chunks} 0", lines[^1]);
        Append(trace, record);
        trace.End(TraceEnd.Reset);
        trace.Flush(WorkshopSimulationPhase.Building);
        Assert.Equal(chunks + 2, output.TraceLines.Length);
    }

    // Story 9R.5b (Murdoch-12 R1-B1): the pose trace is sized from its first record's width and the run's tick count, never a fixed
    // byte budget. Twenty moving bodies (1,648-byte records) for the full 7,200-tick run used to overflow the retired
    // 7,201 x 128-byte buffer after about 559 ticks.
    [Fact]
    public void ATwentyBodyTraceOfTheWholeRunCompletesWithEveryRecord()
    {
        using var output = new CapturedConsole();
        var trace = new WorkshopTrace();
        var balls = Enumerable.Range(0, 20)
            .Select(i => (IWorkshopInstance)WorkshopInput.Generic(PartId.Basketball,new((ulong)i + 3), -6 + i * .6, 8.875, 0, 0, 0, 0, 1)).ToArray();
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
        var header = output.TraceLines[0].Split(' ');
        Assert.Equal((ulong)record.Length, Number(header[3]));
        Assert.Equal(ticks + 1, Number(header[4]));
        Assert.Equal(TraceEnd.Complete, Reason(header[6]));
        Assert.Equal($"CCGPU_TRACE_END {header[1]} {ticks + 1} {Number(header[5])} 0", output.TraceLines[^1]);
    }

    [Fact]
    public void InvalidSessionCannotStartOrContaminateTheNextRun()
    {
        using var output = new CapturedConsole();
        var trace = new WorkshopTrace();
        var record = Record(Construction, new(9), Profile);
        Begin(trace, default, Construction, new(9), new(3), record);
        Append(trace, record);
        trace.End(TraceEnd.Complete);
        trace.Flush(WorkshopSimulationPhase.Completed);
        Assert.Empty(output.TraceLines);
        Begin(trace, Session, Construction, new(9), new(3), record);
        trace.End(TraceEnd.Reset);
        trace.Flush(WorkshopSimulationPhase.Building);
        Assert.Equal(3, output.TraceLines.Length);
        var header = output.TraceLines[0].Split(' ');
        Assert.Equal(TraceEnd.Reset, Reason(header[6]));
        Assert.Equal(Session, WorkshopWireFlatBuffers.DecodeCommand(Convert.FromBase64String(header[7])).Session);
    }

    [Fact]
    public void RestartClosesThePreviousPartialCaptureBeforeTheNewSession()
    {
        using var output = new CapturedConsole();
        var trace = new WorkshopTrace();
        Begin(trace, Session, Construction, new(9), new(3), Record(Construction, new(9), Profile));
        var previousRecord = Record(Construction, new(9), Profile);
        var nextRecord = Record(Construction, new(10), Profile);
        var nextSession = new RuntimeSessionId(23, 41);
        Begin(trace, nextSession, Construction, new(10), new(3), Record(Construction, new(10), Profile));
        trace.End(TraceEnd.Reset);
        trace.Flush(WorkshopSimulationPhase.Building);
        var lines = output.TraceLines;
        Assert.Equal(6, lines.Length);
        var previous = lines[0].Split(' ');
        var next = lines[3].Split(' ');
        Assert.Equal(TraceEnd.Reset, Reason(previous[6]));
        Assert.Equal(TraceEnd.Reset, Reason(next[6]));
        Assert.True(Number(next[1]) > Number(previous[1]));
        Assert.Equal(9UL, Number(previous[2]));
        Assert.Equal(10UL, Number(next[2]));
        Assert.Equal(previousRecord, Convert.FromBase64String(lines[1].Split(' ')[6]));
        Assert.Equal(nextRecord, Convert.FromBase64String(lines[4].Split(' ')[6]));
        Assert.Equal(Session, WorkshopWireFlatBuffers.DecodeCommand(Convert.FromBase64String(previous[7])).Session);
        Assert.Equal(nextSession, WorkshopWireFlatBuffers.DecodeCommand(Convert.FromBase64String(next[7])).Session);
    }

    [Fact]
    public void MissingOrOutOfOrderRecordsCannotClaimComplete()
    {
        using var output = new CapturedConsole();
        var trace = new WorkshopTrace();
        var record = Record(Construction, new(9), Profile);
        Begin(trace, Session, Construction, new(9), new(3), record);
        BinaryPrimitives.WriteUInt64LittleEndian(record.AsSpan(24), 2);
        Append(trace, record);
        trace.End(TraceEnd.Complete);
        trace.Flush(WorkshopSimulationPhase.Completed);
        Assert.Equal(3, output.TraceLines.Length);
        var header = output.TraceLines[0].Split(' ');
        Assert.Equal(1UL, Number(header[4]));
        Assert.Equal(TraceEnd.InvalidRecord, Reason(header[6]));
        var before = output.TraceLines.Length;
        Begin(trace, Session, Construction, new(10), new(3), Record(Construction, new(10), Profile));
        trace.End(TraceEnd.Complete);
        trace.Flush(WorkshopSimulationPhase.Completed);
        Assert.Equal(before + 3, output.TraceLines.Length);
        Assert.Equal(TraceEnd.InvalidRecord, Reason(output.TraceLines[before].Split(' ')[6]));
    }

    [Theory]
    [InlineData(WorkshopSimulationPhase.Running)]
    [InlineData(WorkshopSimulationPhase.Starting)]
    [InlineData(WorkshopSimulationPhase.Resetting)]
    public void InvalidCaptureRemainsSealedUntilStopped(WorkshopSimulationPhase phase)
    {
        foreach (var reason in new[] { TraceEnd.InvalidRecord, TraceEnd.Overflow })
        {
            using var output = new CapturedConsole();
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
            Assert.Empty(output.TraceLines);
            trace.End(TraceEnd.Fault);
            trace.Flush(WorkshopSimulationPhase.Faulted);
            Assert.Equal(reason == TraceEnd.Overflow ? 31 : 3, output.TraceLines.Length);
            Assert.Equal(reason, Reason(output.TraceLines[0].Split(' ')[6]));
        }
    }

    [Theory]
    [InlineData(WorkshopSimulationPhase.Building)]
    [InlineData(WorkshopSimulationPhase.Disposed)]
    [InlineData(WorkshopSimulationPhase.Faulted)]
    public void TerminalSealingPreservesBytesUntilOneFlush(WorkshopSimulationPhase phase)
    {
        var reason = phase switch
        {
            WorkshopSimulationPhase.Building => TraceEnd.Reset,
            WorkshopSimulationPhase.Disposed => TraceEnd.Disposed,
            WorkshopSimulationPhase.Faulted => TraceEnd.Fault,
            _ => throw new ArgumentOutOfRangeException(nameof(phase))
        };
        using var output = new CapturedConsole();
        var trace = new WorkshopTrace();
        var record = Record(Construction, new(9), Profile);
        var expected = (byte[])record.Clone();
        Begin(trace, Session, Construction, new(9), new(3), record);
        trace.End(reason);
        record.AsSpan().Clear();
        Append(trace, record);
        Assert.Empty(output.TraceLines);
        trace.Flush(phase);
        Assert.Equal(expected, Convert.FromBase64String(output.TraceLines[1].Split(' ')[6]));
        Assert.Equal(reason, Reason(output.TraceLines[0].Split(' ')[6]));
        trace.Flush(phase);
        Assert.Equal(3, output.TraceLines.Length);
    }

    [Fact]
    public void FailedOutputIsContainedAndCannotContaminateTheNextCapture()
    {
        using var output = new CapturedConsole();
        var trace = new WorkshopTrace();
        Begin(trace, Session, Construction, new(9), new(3), Record(Construction, new(9), Profile));
        trace.End(TraceEnd.Reset);
        var destination = Console.Out;
        using var failure = new FailingOutput();
        try { Console.SetOut(failure); trace.Flush(WorkshopSimulationPhase.Building); }
        finally { Console.SetOut(destination); }
        trace.Flush(WorkshopSimulationPhase.Building);
        Assert.Empty(output.TraceLines);
        Begin(trace, Session, Construction, new(10), new(3), Record(Construction, new(10), Profile));
        trace.End(TraceEnd.Reset);
        trace.Flush(WorkshopSimulationPhase.Building);
        Assert.Equal(3, output.TraceLines.Length);
        Assert.Equal(10UL, Number(output.TraceLines[0].Split(' ')[2]));
    }


    [Fact]
    public void ASixteenBodyWholeRunKeepsEveryPoseRecordAndCompleteScalarTiming()
    {
        using var output = new CapturedConsole();
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
        var pose = output.TraceLines[0].Split(' ');
        // Story 9R.5b (R1-B1): the pose trace is sized from the record width, so the whole run is retained (it overflowed before).
        Assert.Equal(TraceEnd.Complete, Reason(pose[6]));
        Assert.Equal(Profile.RunTickLimit + 1, Number(pose[4]));
        var timing = output.TimingLines[0].Split(' ');
        Assert.Equal(Profile.RunTickLimit, Number(timing[8]));
        Assert.Equal(0UL, Number(timing[10]));
        ulong nextTick = 1;
        foreach (var line in output.TimingLines.Skip(1).SkipLast(1))
        {
            var fields = line.Split(' ');
            Assert.Equal(nextTick, Number(fields[4]));
            var values = Convert.FromBase64String(fields[6]);
            Assert.Equal(Number(fields[5]) * 8, (ulong)values.Length);
            for (var offset = 0; offset < values.Length; offset += 8)
                Assert.Equal(.75, BinaryPrimitives.ReadDoubleLittleEndian(values.AsSpan(offset)));
            nextTick += Number(fields[5]);
        }
        Assert.Equal(Profile.RunTickLimit + 1, nextTick);
    }

#endif
    [Theory]
    [InlineData(SimulationCadence.Hz60)]
    [InlineData(SimulationCadence.Hz120)]
    [InlineData(SimulationCadence.Hz240)]
    public void CompleteTimingRetainsEveryOriginalSpanAtTheTerminalBarrier(SimulationCadence cadence)
    {
        using var output = new CapturedConsole();
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
        Assert.Empty(output.Lines);
        trace.Flush(WorkshopSimulationPhase.Completed);
        var lines = output.TimingLines;
        var header = lines[0].Split(' ');
        Assert.Equal("CCGPU_TICK_TIMES_BEGIN", header[0]);
        Assert.Equal(9UL, Number(header[2]));
        Assert.Equal(8UL, Number(header[3]));
        Assert.Equal((ulong)profile.Cadence, Number(header[4]));
        Assert.Equal((ulong)profile.Physical, Number(header[5]));
        Assert.Equal(profile.Revision.Value, Number(header[6]));
        Assert.Equal(profile.RunTickLimit, Number(header[7]));
        Assert.Equal(profile.RunTickLimit, Number(header[8]));
        Assert.Equal(0UL, Number(header[10]));
        var command = WorkshopWireFlatBuffers.DecodeCommand(Convert.FromBase64String(header[11]));
        Assert.Equal(Session, command.Session);
        Assert.Equal(construction, command.Construction);
        Assert.Equal(new ProjectionEpoch(3), command.Projection);
        Assert.Equal(new SimulationEpoch(9), command.Epoch);
        Assert.Equal(TraceEnd.Complete, Reason(header[12]));
#if !PLAYTEST
        Assert.Empty(output.TraceLines);
#endif
        var observed = new List<double>();
        var chunks = checked((int)Number(header[9]));
        for (var ordinal = 0; ordinal < chunks; ordinal++)
        {
            var fields = lines[ordinal + 1].Split(' ');
            Assert.Equal("CCGPU_TICK_TIMES_CHUNK", fields[0]);
            Assert.Equal(header[1], fields[1]);
            Assert.Equal((ulong)ordinal, Number(fields[2]));
            Assert.Equal((ulong)chunks, Number(fields[3]));
            Assert.Equal((ulong)observed.Count + 1, Number(fields[4]));
            var bytes = Convert.FromBase64String(fields[6]);
            Assert.InRange(bytes.Length, 8, 1024);
            Assert.Equal(Number(fields[5]) * 8, (ulong)bytes.Length);
            for (var offset = 0; offset < bytes.Length; offset += 8)
                observed.Add(BinaryPrimitives.ReadDoubleLittleEndian(bytes.AsSpan(offset)));
        }
        Assert.Equal(expected, observed);
        Assert.Equal($"CCGPU_TICK_TIMES_END {header[1]} {expected.Length} {chunks} 0", lines[^1]);
        trace.Flush(WorkshopSimulationPhase.Completed);
        Assert.Equal(lines, output.TimingLines);
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
        using var output = new CapturedConsole();
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
            var header = output.TimingLines.Last(line => line.StartsWith("CCGPU_TICK_TIMES_BEGIN ", StringComparison.Ordinal)).Split(' ');
            Assert.Equal(epoch, Number(header[2]));
            Assert.Equal(epoch == 9 ? 1UL : 0UL, Number(header[10]));
            if (epoch == 10) Assert.Equal(Profile.RunTickLimit, Number(header[8]));
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
        using var output = new CapturedConsole();
        var trace = new WorkshopTrace();
        Begin(trace, Session, Construction, new(9), new(3), Record(Construction, new(9), Profile));
        trace.End(reason);
        trace.Flush(WorkshopSimulationPhase.Running);
        Assert.Empty(output.Lines);
        trace.Flush(WorkshopSimulationPhase.Building);
        var original = output.TimingLines;
        var header = original[0].Split(' ');
        Assert.Equal(1UL, Number(header[10]));
        Assert.Equal(reason, Reason(header[12]));
        trace.Flush(WorkshopSimulationPhase.Building);
        Assert.Equal(original, output.TimingLines);
    }

    [Fact]
    public void TimingOutputFailureCannotEscapeOrContaminateNextRun()
    {
        using var output = new CapturedConsole();
        var trace = new WorkshopTrace();
        Begin(trace, Session, Construction, new(9), new(3), Record(Construction, new(9), Profile));
        trace.End(TraceEnd.Reset);
        var destination = Console.Out;
        using var failure = new FailingOutput();
        try { Console.SetOut(failure); trace.Flush(WorkshopSimulationPhase.Building); }
        finally { Console.SetOut(destination); }
        trace.Flush(WorkshopSimulationPhase.Building);
        Assert.Empty(output.TimingLines);
        Begin(trace, Session, Construction, new(10), new(3), Record(Construction, new(10), Profile));
        trace.End(TraceEnd.Reset);
        trace.Flush(WorkshopSimulationPhase.Building);
        Assert.Equal(10UL, Number(output.TimingLines[0].Split(' ')[2]));
    }

    public enum BeginDefect { Session, ProfileMismatch, UndefinedProfile, Projection }
    [Theory]
    [InlineData(BeginDefect.Session)]
    [InlineData(BeginDefect.ProfileMismatch)]
    [InlineData(BeginDefect.UndefinedProfile)]
    [InlineData(BeginDefect.Projection)]
    public void InvalidTimingIdentityCannotStartOrContaminateTheNextRun(BeginDefect defect)
    {
        using var output = new CapturedConsole();
        var trace = new WorkshopTrace();
        var profile = defect switch
        {
            BeginDefect.ProfileMismatch => Profile with { Cadence = SimulationCadence.Hz60 },
            BeginDefect.UndefinedProfile => Profile with { Cadence = (SimulationCadence)999 },
            _ => Profile
        };
        trace.Begin(defect == BeginDefect.Session ? default : Session, Construction, new(9),
            defect == BeginDefect.Projection ? default : new(3), profile);
        trace.RecordTickDuration(new(1), .25);
        trace.End(TraceEnd.Complete);
        trace.Flush(WorkshopSimulationPhase.Completed);
        Assert.Empty(output.Lines);
        Begin(trace, Session, Construction, new(10), new(3), Record(Construction, new(10), Profile));
        trace.End(TraceEnd.Reset);
        trace.Flush(WorkshopSimulationPhase.Building);
        var header = output.TimingLines[0].Split(' ');
        Assert.Equal(10UL, Number(header[2]));
        Assert.Equal(Session, WorkshopWireFlatBuffers.DecodeCommand(Convert.FromBase64String(header[11])).Session);
    }

    [Fact]
    // The measuring loop is fully optimised too. Story 9.4b's rare allocation in this window has an open cause (P0-034 /
    // Story 16.1): tiering was never confirmed, and an ablation without this attribute did not fail either.
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
    public void DurationRetentionHasNoRoutineAllocationInNativeExecution()
    {
        using var output = new CapturedConsole();
        var trace = new WorkshopTrace();
        var profile = Profile with { Cadence = SimulationCadence.Hz240 };
        var construction = Construction with { Settings = Settings with { Simulation = SimulationCadence.Hz240 } };
        long allocated = 0, elapsed = 0;
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
                var beforeTime = System.Diagnostics.Stopwatch.GetTimestamp();
                var beforeCollections = GC.CollectionCount(0);
                var beforeJit = System.Runtime.JitInfo.GetCompiledMethodCount(currentThread: true);
                var beforeBytes = GC.GetAllocatedBytesForCurrentThread();
                trace.RecordTickDuration(new(tick), .75);
                var afterBytes = GC.GetAllocatedBytesForCurrentThread();
                var afterJit = System.Runtime.JitInfo.GetCompiledMethodCount(currentThread: true);
                var afterTime = System.Diagnostics.Stopwatch.GetTimestamp();
                if (epoch != 10) continue;
                allocated += afterBytes - beforeBytes; elapsed += afterTime - beforeTime;
                if (afterBytes != beforeBytes && allocatingCalls.Count < 4)
                    allocatingCalls.Add($"tick {tick}: {afterBytes - beforeBytes} B, gen0 collections {GC.CollectionCount(0) - beforeCollections}, methods jitted on this thread {afterJit - beforeJit}");
            }
            trace.End(TraceEnd.Complete);
            trace.Flush(WorkshopSimulationPhase.Completed);
        }
        Assert.True(allocated == 0, $"{allocated} B allocated by RecordTickDuration: {string.Join("; ", allocatingCalls)}");
        testOutput.WriteLine($"Native retention: {profile.RunTickLimit} calls, {allocated} allocated bytes, {elapsed * 1000d / System.Diagnostics.Stopwatch.Frequency:R} total observed milliseconds including timestamp overhead. Not a WASM timing bound.");
    }

    private sealed class FailingOutput : StringWriter
    {
        public override void WriteLine(string? value) => throw new IOException("Deliberate diagnostic output failure.");
    }

    private static ulong Number(string value) => ulong.Parse(value, CultureInfo.InvariantCulture);
    private static TraceEnd Reason(string value)
    {
        var result = (TraceEnd)int.Parse(value, CultureInfo.InvariantCulture);
        Assert.True(Enum.IsDefined(result));
        return result;
    }

    private sealed class CapturedConsole : IDisposable
    {
        // The previous writer is borrowed (it belongs to the runner), so only a restore action is kept, never the writer.
        private readonly Action _restore;
        private readonly StringWriter _output = new(CultureInfo.InvariantCulture);
        public CapturedConsole()
        {
            var previous = Console.Out;
            _restore = () => Console.SetOut(previous);
            Console.SetOut(_output);
        }
        public string[] Lines => _output.ToString().Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        public string[] TraceLines => Lines.Where(line => line.StartsWith("CCGPU_TRACE_", StringComparison.Ordinal)).ToArray();
        public string[] TimingLines => Lines.Where(line => line.StartsWith("CCGPU_TICK_TIMES_", StringComparison.Ordinal)).ToArray();
        public void Dispose() { _restore(); _output.Dispose(); }
    }
}
