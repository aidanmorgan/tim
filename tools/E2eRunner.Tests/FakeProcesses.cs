using System.Diagnostics;
using System.Text;

namespace CuriousContraptions.E2eRunner.Tests;

/// <summary>How a scripted root wait answers: the root exited, the limit passed, or the wait ends when its token is cancelled.</summary>
internal enum RootAnswer { Exited = 1, AtLimit, EndsAtCancel }

/// <summary>How a scripted output wait answers: both streams ended, still open at the bound, or a pipe fault.</summary>
internal enum OutputAnswer { Ended = 1, StillOpen, Faults }

/// <summary>A scripted operating-system process handle, the genuine external boundary of the process lifecycle. Each wait is
/// answered from its script without the clock and recorded with its bound, in an event list a fake table may share, so a test can
/// state which waits and kills the lifecycle made and in what order (the bounded-wait contract), including for a root that SIGKILL
/// does not end, which no real process can be made to do.</summary>
internal sealed class FakeChild(IEnumerable<RootAnswer> root, IEnumerable<OutputAnswer>? output = null, List<string>? events = null) : IChildProcess
{
    private readonly Queue<RootAnswer> _root = new(root);
    private readonly Queue<OutputAnswer> _output = new(output ?? []);

    public List<string> Events { get; } = events ?? [];
    public List<TimeSpan> RootWaits { get; } = [];
    public List<bool> RootWaitsCancellable { get; } = [];
    public List<TimeSpan> OutputWaits { get; } = [];
    public int Kills { get; private set; }
    public int Id { get; init; } = 100;
    public int ExitCode { get; init; }
    public Exception? KillFault { get; init; }

    public void KillTree()
    {
        Kills++;
        Events.Add("kill tree");
        if (KillFault is not null) throw KillFault;
    }

    public Task<bool> WaitForRootAsync(TimeSpan limit, CancellationToken cancellation)
    {
        RootWaits.Add(limit);
        RootWaitsCancellable.Add(cancellation.CanBeCanceled);
        Events.Add("root wait");
        return _root.Dequeue() switch
        {
            RootAnswer.Exited => Task.FromResult(true),
            RootAnswer.AtLimit => Task.FromResult(false),
            RootAnswer.EndsAtCancel => EndsAtCancelAsync(cancellation),
            var answer => throw new InvalidOperationException($"Unknown root answer {answer}."),
        };
    }

    public Task<bool> WaitForOutputEndAsync(TimeSpan limit)
    {
        OutputWaits.Add(limit);
        Events.Add("output wait");
        return _output.Dequeue() switch
        {
            OutputAnswer.Ended => Task.FromResult(true),
            OutputAnswer.StillOpen => Task.FromResult(false),
            OutputAnswer.Faults => Task.FromException<bool>(new IOException("the pipe broke")),
            var answer => throw new InvalidOperationException($"Unknown output answer {answer}."),
        };
    }

    public void Dispose() { }

    private static async Task<bool> EndsAtCancelAsync(CancellationToken cancellation)
    {
        await Task.Delay(Timeout.Infinite, cancellation);
        return false;
    }
}

/// <summary>A launcher that hands out one scripted handle, keeping the start description and receiver it was given; it may
/// deliver output to the receiver at the start.</summary>
internal sealed class FakeLauncher(IChildProcess child, Action<IOutputReceiver>? atStart = null)
{
    public IOutputReceiver? Receiver { get; private set; }
    public ProcessStartInfo? Start { get; private set; }

    public IChildProcess Launch(ProcessStartInfo start, IOutputReceiver receiver)
    {
        Start = start;
        Receiver = receiver;
        atStart?.Invoke(receiver);
        return child;
    }
}

/// <summary>A scripted process table: rows (another row set from a given read on), a read refused from a given read on, a hook run
/// inside each read, and scripted survivor kills.</summary>
internal sealed class FakeTable(List<string> events, IReadOnlyList<ProcessRow> rows) : IProcessTable
{
    private int _reads;

    /// <summary>The first read (1-based) refused as ProcessTableUnreadable; every later read is refused too.</summary>
    public int RefuseFromRead { get; init; } = int.MaxValue;

    /// <summary>The rows every read from <see cref="LaterFromRead"/> on answers instead (a pid reused, or a process gone).</summary>
    public IReadOnlyList<ProcessRow> LaterRows { get; init; } = [];
    public int LaterFromRead { get; init; } = int.MaxValue;

    /// <summary>Runs inside each read, with its 1-based number, before the read answers.</summary>
    public Action<int>? DuringRead { get; init; }
    public Dictionary<int, SurvivorKill> KillAnswers { get; } = [];
    public List<int> Killed { get; } = [];
    public int Reads => Volatile.Read(ref _reads);

    public IReadOnlyList<ProcessRow> Snapshot()
    {
        var read = Interlocked.Increment(ref _reads);
        lock (events) events.Add("snapshot");
        DuringRead?.Invoke(read);
        if (read >= RefuseFromRead) throw new RunnerException(RunnerError.ProcessTableUnreadable, $"ps refused read {read}");
        return read >= LaterFromRead ? LaterRows : rows;
    }

    public SurvivorKill Kill(int pid)
    {
        Killed.Add(pid);
        events.Add($"kill {pid}");
        return KillAnswers.TryGetValue(pid, out var answer) ? answer : new SurvivorKill(SurvivorKillResult.Killed, "");
    }
}

/// <summary>Forwards to a receiver and records each end of file it saw.</summary>
internal sealed class RecordingReceiver(IOutputReceiver inner) : IOutputReceiver
{
    private readonly Lock _gate = new();
    private readonly List<OutputStream> _ends = [];

    public IReadOnlyList<OutputStream> Ends { get { lock (_gate) return [.. _ends.Order()]; } }

    public void Write(OutputStream stream, ReadOnlySpan<char> chunk) => inner.Write(stream, chunk);

    public void End(OutputStream stream)
    {
        lock (_gate) _ends.Add(stream);
        inner.End(stream);
    }
}

/// <summary>A receiver whose every write fails as a full disk would.</summary>
internal sealed class ThrowingReceiver : IOutputReceiver
{
    public const string Cause = "the receiver's disk is full";

    public void Write(OutputStream stream, ReadOnlySpan<char> chunk) => throw new IOException(Cause);

    public void End(OutputStream stream) { }
}

/// <summary>A log writer whose disk fills at a given line: that write and every later one throw IOException.</summary>
internal sealed class FaultingWriter(int faultingLine) : TextWriter
{
    public const string Cause = "no space left on device";
    private int _attempts;

    public List<string> Lines { get; } = [];
    public bool Disposed { get; private set; }
    public override Encoding Encoding => Encoding.UTF8;

    public override void WriteLine(string? value)
    {
        if (++_attempts >= faultingLine) throw new IOException(Cause);
        Lines.Add(value ?? "");
    }

    protected override void Dispose(bool disposing)
    {
        Disposed = true;
        base.Dispose(disposing);
    }
}
