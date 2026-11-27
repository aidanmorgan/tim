using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace CuriousContraptions.E2eRunner;

/// <summary>One of a child process's two output streams.</summary>
internal enum OutputStream { Standard = 1, Error }

/// <summary>Receives a child's output: each chunk of characters as it is read, and each stream's end of file.</summary>
internal interface IOutputReceiver
{
    void Write(OutputStream stream, ReadOnlySpan<char> chunk);
    void End(OutputStream stream);
}

/// <summary>The operating-system handle of one started child process: the only seam between the process lifecycle model and the
/// operating system. Every wait of the model is one of its two waits.</summary>
internal interface IChildProcess : IDisposable
{
    int Id { get; }
    int ExitCode { get; }

    /// <summary>Signals the root's whole process tree.</summary>
    void KillTree();

    /// <summary>True once the root has exited, false at the limit. A cancelled token throws, also when it is cancelled at the call.</summary>
    Task<bool> WaitForRootAsync(TimeSpan limit, CancellationToken cancellation);

    /// <summary>True once both output streams have ended (end of file, or disposal of the handle), false at the limit. Faults at once
    /// when a read or the receiver throws.</summary>
    Task<bool> WaitForOutputEndAsync(TimeSpan limit);
}

/// <summary>Starts a child process whose output goes to the receiver, or throws a typed ProcessNotStarted refusal.</summary>
internal delegate IChildProcess LaunchChild(ProcessStartInfo start, IOutputReceiver receiver);

/// <summary>Whether a stopped root ended within the grace.</summary>
internal enum RootState { Ended = 1, StillRunning }

/// <summary>The result of one stop: the root's state after the grace, and what the kill did.</summary>
internal sealed record StopOutcome(RootState Root, string Detail);

/// <summary>The production child process: one start, and one chunk read loop per output stream.</summary>
internal sealed class ChildProcess : IChildProcess
{
    private const int ChunkLength = 4096;
    private const string StartRemedy = "Install the program or put it on PATH, make it executable, create the working directory, "
        + "or let the system start more processes (check its process limit), then run again.";

    private readonly Process _process;
    private readonly CancellationTokenSource _disposal = new();
    private readonly Task _outputEnd;
    private bool _disposed;

    private ChildProcess(Process process, IOutputReceiver receiver)
    {
        _process = process;
        var standard = ReadAsync(process.StandardOutput, OutputStream.Standard, receiver, _disposal.Token);
        var error = ReadAsync(process.StandardError, OutputStream.Error, receiver, _disposal.Token);
        _outputEnd = BothEndedAsync(standard, error);
        Observe(standard);
        Observe(error);
        Observe(_outputEnd);
    }

    /// <summary>The one start of every runner-started process (a <see cref="LaunchChild"/>). It redirects both output streams to its read
    /// loops and turns a start failure into a typed ProcessNotStarted refusal; no handle exists after a refused start.</summary>
    public static IChildProcess Start(ProcessStartInfo start, IOutputReceiver receiver)
    {
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(receiver);
        start.UseShellExecute = false;
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        var process = new Process { StartInfo = start };
        try { process.Start(); }
        catch (Win32Exception cause)
        {
            process.Dispose();
            throw new RunnerException(RunnerError.ProcessNotStarted, $"{start.FileName} could not start ({cause.Message}). {StartRemedy}");
        }
        return new ChildProcess(process, receiver);
    }

    public int Id => _process.Id;
    public int ExitCode => _process.ExitCode;

    public void KillTree() => _process.Kill(entireProcessTree: true);

    public async Task<bool> WaitForRootAsync(TimeSpan limit, CancellationToken cancellation)
    {
        // Process.WaitForExitAsync returns normally for an exited root even with a cancelled token, so the token is checked first.
        cancellation.ThrowIfCancellationRequested();
        using var timer = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        timer.CancelAfter(limit);
        try { await _process.WaitForExitAsync(timer.Token).ConfigureAwait(false); }
        catch (OperationCanceledException) when (!cancellation.IsCancellationRequested) { return false; }
        return true;
    }

    public async Task<bool> WaitForOutputEndAsync(TimeSpan limit)
    {
        try { await _outputEnd.WaitAsync(limit).ConfigureAwait(false); }
        catch (TimeoutException) when (!_outputEnd.IsCompleted) { return false; }
        return true;
    }

    /// <summary>Ends both read loops without waiting for them (disposing the process alone does not end a read blocked on a pipe a
    /// descendant holds) and releases the handle. It does not stop the process.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _disposal.Cancel();
        _process.Dispose();
        _disposal.Dispose();
    }

    private static async Task ReadAsync(StreamReader reader, OutputStream stream, IOutputReceiver receiver, CancellationToken disposal)
    {
        using (reader)
        {
            var buffer = new char[ChunkLength];
            try
            {
                int count;
                while ((count = await reader.ReadAsync(buffer.AsMemory(), disposal).ConfigureAwait(false)) > 0)
                {
                    if (disposal.IsCancellationRequested) return;
                    receiver.Write(stream, buffer.AsSpan(0, count));
                }
            }
            catch (OperationCanceledException) when (disposal.IsCancellationRequested) { return; } // ended by disposal: no End
            receiver.End(stream);
        }
    }

    /// <summary>Completes when both loops have finished, and faults as soon as either faults.</summary>
    private static async Task BothEndedAsync(Task first, Task second)
    {
        var done = await Task.WhenAny(first, second).ConfigureAwait(false);
        await done.ConfigureAwait(false);
        await (done == first ? second : first).ConfigureAwait(false);
    }

    /// <summary>Observes a fault of a task nobody may await (after disposal), so it never goes unobserved.</summary>
    private static void Observe(Task task) => _ = task.ContinueWith(static done => _ = done.Exception, CancellationToken.None,
        TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
}

/// <summary>The exact text of each output stream (a short command's result, whose `git … -z` output must round-trip).</summary>
internal sealed class CapturedOutput : IOutputReceiver
{
    private readonly StringBuilder _standard = new();
    private readonly StringBuilder _error = new();

    public string Output => _standard.ToString();
    public string Error => _error.ToString();

    public void Write(OutputStream stream, ReadOnlySpan<char> chunk) => Text(stream).Append(chunk);

    public void End(OutputStream stream) { }

    private StringBuilder Text(OutputStream stream) => stream switch
    {
        OutputStream.Standard => _standard,
        OutputStream.Error => _error,
        _ => throw new ArgumentOutOfRangeException(nameof(stream), stream, "Unknown output stream."),
    };
}

/// <summary>A log of a child's output lines and, last, the runner's own lines. Each stream is split into lines at `\n` (one trailing
/// `\r` dropped) and written with the prefix under one lock; <see cref="LineWritten"/> carries each raw line. After <see cref="Close"/>
/// a chunk is dropped and counted, never written to a closed writer. The sink never throws a writer fault: the first is recorded in
/// <see cref="Fault"/> and every later line is dropped and counted; its caller throws ProcessOutputFailed after closing it.</summary>
internal sealed class OutputSink : IOutputReceiver
{
    private readonly Lock _gate = new();
    private readonly TextWriter _writer;
    private readonly string _prefix;
    private readonly StringBuilder _standard = new();
    private readonly StringBuilder _error = new();
    private bool _closed;
    private int _dropped;
    private IOException? _fault;

    /// <summary>A sink over a log file it opens (appending, or replacing it) and owns: <see cref="Close"/> disposes it. A log that
    /// cannot be opened is a typed ProcessOutputFailed refusal naming its path.</summary>
    public OutputSink(string path, bool append, string prefix)
    {
        StreamWriter writer;
        try { writer = append ? File.AppendText(path) : File.CreateText(path); }
        catch (Exception cause) when (cause is IOException or UnauthorizedAccessException)
        {
            throw new RunnerException(RunnerError.ProcessOutputFailed, $"The log {path} could not be opened ({cause.Message}).", cause);
        }
        writer.AutoFlush = true;
        _writer = writer;
        _prefix = prefix;
    }

    /// <summary>A sink over a writer it owns from now on.</summary>
    public OutputSink(TextWriter writer, string prefix)
    {
        _writer = writer;
        _prefix = prefix;
    }

    /// <summary>Raised after each output line with its stream and the raw, unprefixed line (also when a faulted sink dropped it).</summary>
    public event Action<OutputStream, string>? LineWritten;

    public int Dropped { get { lock (_gate) return _dropped; } }
    public IOException? Fault { get { lock (_gate) return _fault; } }

    public void Write(OutputStream stream, ReadOnlySpan<char> chunk)
    {
        var lines = new List<string>();
        lock (_gate)
        {
            if (_closed)
            {
                _dropped++;
                return;
            }
            var pending = Pending(stream);
            var rest = chunk;
            for (var newline = rest.IndexOf('\n'); newline >= 0; newline = rest.IndexOf('\n'))
            {
                pending.Append(rest[..newline]);
                lines.Add(TakeLine(pending));
                rest = rest[(newline + 1)..];
            }
            pending.Append(rest);
        }
        Raise(stream, lines);
    }

    /// <summary>A stream's end of file: a pending partial line is written as a line.</summary>
    public void End(OutputStream stream)
    {
        List<string> lines;
        lock (_gate)
        {
            if (_closed) return;
            lines = TakePartial(Pending(stream));
        }
        Raise(stream, lines);
    }

    /// <summary>Writes any pending partial line of each stream, then the runner's lines (so they are the log's last lines), then
    /// disposes the writer, as one act under the lock. A second close does nothing.</summary>
    public void Close(IEnumerable<string> runnerLines)
    {
        ArgumentNullException.ThrowIfNull(runnerLines);
        List<string> standard;
        List<string> error;
        lock (_gate)
        {
            if (_closed) return;
            standard = TakePartial(_standard);
            error = TakePartial(_error);
            foreach (var line in runnerLines) WriteRaw(line);
            _closed = true;
            try { _writer.Dispose(); }
            catch (IOException fault) { _fault ??= fault; }
        }
        Raise(OutputStream.Standard, standard);
        Raise(OutputStream.Error, error);
    }

    private StringBuilder Pending(OutputStream stream) => stream switch
    {
        OutputStream.Standard => _standard,
        OutputStream.Error => _error,
        _ => throw new ArgumentOutOfRangeException(nameof(stream), stream, "Unknown output stream."),
    };

    private List<string> TakePartial(StringBuilder pending) => pending.Length == 0 ? [] : [TakeLine(pending)];

    private string TakeLine(StringBuilder pending)
    {
        var line = pending.ToString();
        pending.Clear();
        if (line.EndsWith('\r')) line = line[..^1];
        WriteRaw(line);
        return line;
    }

    private void WriteRaw(string line)
    {
        if (_fault is not null)
        {
            _dropped++;
            return;
        }
        try { _writer.WriteLine(_prefix + line); }
        catch (IOException fault)
        {
            _fault = fault;
            _dropped++;
        }
    }

    private void Raise(OutputStream stream, List<string> lines)
    {
        foreach (var line in lines) LineWritten?.Invoke(stream, line);
    }
}

/// <summary>The one process lifecycle every runner-started process goes through: start, run to a limit, stop the tree once with one
/// grace, drain bounded, close the log, one typed outcome.</summary>
internal static class ProcessLifecycle
{
    /// <summary>How long a root may take to exit after its tree is killed, and the shortest output drain.</summary>
    public static readonly TimeSpan Grace = TimeSpan.FromSeconds(5);

    public const string TreeKilled = "its process tree was killed";
    public const string RootStillRunningLine = "E2E_RUNNER root still running after the kill; later lines are not recorded";
    public const string OutputStillOpenLine = "E2E_RUNNER output still open after the root ended; later lines are not recorded";

    /// <summary>Starts a child whose output goes to a sink; a refused start closes the sink (nothing is written) and is rethrown.</summary>
    public static IChildProcess Start(LaunchChild launch, ProcessStartInfo start, OutputSink sink)
    {
        ArgumentNullException.ThrowIfNull(launch);
        ArgumentNullException.ThrowIfNull(sink);
        try { return launch(start, sink); }
        catch (RunnerException)
        {
            sink.Close([]);
            throw;
        }
    }

    /// <summary>The only kill of a runner-owned tree: signals it once, records a kill that throws, then waits once, for the grace,
    /// for the root's exit, whatever the kill did.</summary>
    public static async Task<StopOutcome> StopAsync(IChildProcess child, TimeSpan grace)
    {
        ArgumentNullException.ThrowIfNull(child);
        string detail;
        try
        {
            child.KillTree();
            detail = TreeKilled;
        }
        catch (Exception cause) when (cause is AggregateException or InvalidOperationException or Win32Exception or IOException)
        {
            detail = $"the kill did not complete ({cause.Message})";
        }
        var ended = await child.WaitForRootAsync(grace, CancellationToken.None).ConfigureAwait(false);
        return new StopOutcome(ended ? RootState.Ended : RootState.StillRunning, detail);
    }

    /// <summary>Waits, once the root has ended, for both output streams' end of file, bounded by max(time left, grace). False when
    /// the output is still open at the bound; an output fault is a typed ProcessOutputFailed refusal.</summary>
    public static async Task<bool> DrainAsync(IChildProcess child, string program, TimeSpan left, TimeSpan grace)
    {
        ArgumentNullException.ThrowIfNull(child);
        var bound = left > grace ? left : grace;
        try { return await child.WaitForOutputEndAsync(bound).ConfigureAwait(false); }
        catch (Exception cause) { throw new RunnerException(RunnerError.ProcessOutputFailed, OutputFailedDetail(program, cause.Message), cause); }
    }

    /// <summary>Ends a stopped or exited child's log: drains when its root has ended (noting output still open), or notes a root
    /// still running; closes the sink with the runner's lines; and returns the output failure, if any, for the caller to throw after
    /// any ProcessNotStopped of its own.</summary>
    public static async Task<RunnerException?> FinishAsync(IChildProcess child, OutputSink sink, string program, RootState root, TimeSpan left,
        TimeSpan grace, IReadOnlyList<string> runnerLines)
    {
        ArgumentNullException.ThrowIfNull(sink);
        var lines = new List<string>(runnerLines);
        RunnerException? failure = null;
        if (root == RootState.StillRunning) lines.Add(RootStillRunningLine);
        else
        {
            try { if (!await DrainAsync(child, program, left, grace).ConfigureAwait(false)) lines.Add(OutputStillOpenLine); }
            catch (RunnerException refusal) when (refusal.Error == RunnerError.ProcessOutputFailed) { failure = refusal; }
        }
        sink.Close(lines);
        return failure ?? (sink.Fault is { } fault ? OutputFailed(program, fault.Message) : null);
    }

    public static RunnerException OutputFailed(string program, string cause) => new(RunnerError.ProcessOutputFailed, OutputFailedDetail(program, cause));

    private static string OutputFailedDetail(string program, string cause) => $"The output of {program} could not be read or recorded ({cause}).";

    /// <summary>One ProcessNotStopped refusal naming two (each process the run owns that may still run), keeping both as its inner
    /// AggregateException.</summary>
    public static RunnerException BothNotStopped(Exception earlier, Exception later)
    {
        ArgumentNullException.ThrowIfNull(earlier);
        ArgumentNullException.ThrowIfNull(later);
        return new(RunnerError.ProcessNotStopped, $"More than one process tree could not be stopped. {earlier.Message} And {later.Message}",
            new AggregateException(earlier, later));
    }

    public static string Seconds(TimeSpan span) => span.TotalSeconds.ToString(CultureInfo.InvariantCulture);
}
