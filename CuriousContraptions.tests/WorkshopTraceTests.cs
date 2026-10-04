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
    private static readonly WorkshopConstruction Construction = new(new(7), Settings, new(WorkshopInput.Basketball(new(3), 0, 8.875, 0, 0, 0, 0, 1)));

    private static byte[] Record(WorkshopConstruction construction, SimulationEpoch epoch, WorkshopGpuProfile profile)
    {
        var state = PhysicsGpuAbi.Admission(WorkshopPhysicsCompiler.Compile(construction, new(101,207)), epoch, profile);
        var physical = PhysicsGpuAbi.ReadDynamicBody(state);
        return WorkshopTraceRecord.Encode(new(epoch, new(0), physical?.Body,
            Rotation: physical?.Rotation, Angular: physical?.AngularVelocity ?? default), profile);
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
        var expected = new byte[count * WorkshopTraceRecord.ByteLength];
        Begin(trace, Session, configured, new(9), new(3), record);
        record.CopyTo(expected, 0);
        for (ulong tick = 1; tick <= ticks; tick++)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(record.AsSpan(24), tick);
            record.CopyTo(expected, checked((int)tick * WorkshopTraceRecord.ByteLength));
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
        Assert.Equal((ulong)count, Number(header[5]));
        Assert.Equal((ulong)chunks, Number(header[6]));
        Assert.Equal(TraceEnd.Complete, Reason(header[7]));
        var construction = WorkshopWire.DecodeCommand(Convert.FromBase64String(header[8]));
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
            Assert.Equal(chunkCount * WorkshopTraceRecord.ByteLength, bytes.Length);
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
        Assert.Equal(TraceEnd.Reset, Reason(header[7]));
        Assert.Equal(Session, WorkshopWire.DecodeCommand(Convert.FromBase64String(header[8])).Session);
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
        Assert.Equal(TraceEnd.Reset, Reason(previous[7]));
        Assert.Equal(TraceEnd.Reset, Reason(next[7]));
        Assert.True(Number(next[1]) > Number(previous[1]));
        Assert.Equal(9UL, Number(previous[2]));
        Assert.Equal(10UL, Number(next[2]));
        Assert.Equal(previousRecord, Convert.FromBase64String(lines[1].Split(' ')[6]));
        Assert.Equal(nextRecord, Convert.FromBase64String(lines[4].Split(' ')[6]));
        Assert.Equal(Session, WorkshopWire.DecodeCommand(Convert.FromBase64String(previous[8])).Session);
        Assert.Equal(nextSession, WorkshopWire.DecodeCommand(Convert.FromBase64String(next[8])).Session);
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
        Assert.Equal(1UL, Number(header[5]));
        Assert.Equal(TraceEnd.InvalidRecord, Reason(header[7]));
        var before = output.TraceLines.Length;
        Begin(trace, Session, Construction, new(10), new(3), Record(Construction, new(10), Profile));
        trace.End(TraceEnd.Complete);
        trace.Flush(WorkshopSimulationPhase.Completed);
        Assert.Equal(before + 3, output.TraceLines.Length);
        Assert.Equal(TraceEnd.InvalidRecord, Reason(output.TraceLines[before].Split(' ')[7]));
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
            trace.Flush(WorkshopSimulationPhase.Faulted);
            Assert.Equal(reason == TraceEnd.Overflow ? 31 : 3, output.TraceLines.Length);
            Assert.Equal(reason, Reason(output.TraceLines[0].Split(' ')[7]));
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
            WorkshopSimulationPhase.Faulted => TraceEnd.DeviceLost,
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
        Assert.Equal(reason, Reason(output.TraceLines[0].Split(' ')[7]));
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
        Assert.Equal(2UL, Number(header[3]));
        Assert.Equal(8UL, Number(header[4]));
        Assert.Equal((ulong)profile.Cadence, Number(header[5]));
        Assert.Equal((ulong)profile.Physical, Number(header[6]));
        Assert.Equal(profile.Revision.Value, Number(header[7]));
        Assert.Equal(profile.RunTickLimit, Number(header[8]));
        Assert.Equal(profile.RunTickLimit, Number(header[9]));
        Assert.Equal(0UL, Number(header[11]));
        var command = WorkshopWire.DecodeCommand(Convert.FromBase64String(header[12]));
        Assert.Equal(Session, command.Session);
        Assert.Equal(construction, command.Construction);
        Assert.Equal(new ProjectionEpoch(3), command.Projection);
        Assert.Equal(new SimulationEpoch(9), command.Epoch);
        Assert.Equal(TraceEnd.Complete, Reason(header[13]));
#if !PLAYTEST
        Assert.Empty(output.TraceLines);
#endif
        var observed = new List<double>();
        var chunks = checked((int)Number(header[10]));
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
            Assert.Equal(epoch == 9 ? 1UL : 0UL, Number(header[11]));
            if (epoch == 10) Assert.Equal(Profile.RunTickLimit, Number(header[9]));
        }
    }

    public enum TimingTerminal { Reset, Disposed, DeviceLost, Fault }
    [Theory]
    [InlineData(TimingTerminal.Reset)]
    [InlineData(TimingTerminal.Disposed)]
    [InlineData(TimingTerminal.DeviceLost)]
    [InlineData(TimingTerminal.Fault)]
    public void EarlyTerminalTimingIsIncompleteAndFlushesOnce(TimingTerminal terminal)
    {
        var reason = terminal switch
        {
            TimingTerminal.Reset => TraceEnd.Reset,
            TimingTerminal.Disposed => TraceEnd.Disposed,
            TimingTerminal.DeviceLost => TraceEnd.DeviceLost,
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
        Assert.Equal(1UL, Number(header[11]));
        Assert.Equal(reason, Reason(header[13]));
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
        Assert.Equal(Session, WorkshopWire.DecodeCommand(Convert.FromBase64String(header[12])).Session);
    }

    [Fact]
    public void DurationRetentionHasNoRoutineAllocationInNativeExecution()
    {
        using var output = new CapturedConsole();
        var trace = new WorkshopTrace();
        var profile = Profile with { Cadence = SimulationCadence.Hz240 };
        var construction = Construction with { Settings = Settings with { Simulation = SimulationCadence.Hz240 } };
        long allocated = 0, elapsed = 0;
        for (ulong epoch = 9; epoch <= 10; epoch++)
        {
            var record = Record(construction, new(epoch), profile);
            Begin(trace, Session, construction, new(epoch), new(3), record);
            for (ulong tick = 1; tick <= profile.RunTickLimit; tick++)
            {
                BinaryPrimitives.WriteUInt64LittleEndian(record.AsSpan(24), tick);
                Append(trace, record);
                var beforeBytes = GC.GetAllocatedBytesForCurrentThread();
                var beforeTime = System.Diagnostics.Stopwatch.GetTimestamp();
                trace.RecordTickDuration(new(tick), .75);
                var afterTime = System.Diagnostics.Stopwatch.GetTimestamp();
                var afterBytes = GC.GetAllocatedBytesForCurrentThread();
                if (epoch == 10) { allocated += afterBytes - beforeBytes; elapsed += afterTime - beforeTime; }
            }
            trace.End(TraceEnd.Complete);
            trace.Flush(WorkshopSimulationPhase.Completed);
        }
        Assert.Equal(0, allocated);
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
        private readonly TextWriter _previous = Console.Out;
        private readonly StringWriter _output = new(CultureInfo.InvariantCulture);
        public CapturedConsole() => Console.SetOut(_output);
        public string[] Lines => _output.ToString().Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        public string[] TraceLines => Lines.Where(line => line.StartsWith("CCGPU_TRACE_", StringComparison.Ordinal)).ToArray();
        public string[] TimingLines => Lines.Where(line => line.StartsWith("CCGPU_TICK_TIMES_", StringComparison.Ordinal)).ToArray();
        public void Dispose() { Console.SetOut(_previous); _output.Dispose(); }
    }
}
