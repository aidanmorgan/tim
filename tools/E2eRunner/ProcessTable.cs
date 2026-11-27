using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace CuriousContraptions.E2eRunner;

/// <summary>One row of the machine's process table.</summary>
internal readonly record struct ProcessRow(int Pid, int ParentPid, long RssKilobytes, string Command);

/// <summary>What a kill of one survivor did.</summary>
internal enum SurvivorKillResult { Killed = 1, Gone, Refused }

/// <summary>The result of killing one survivor, with the cause when it was gone or refused.</summary>
internal readonly record struct SurvivorKill(SurvivorKillResult Result, string Cause);

/// <summary>The machine's process table: the snapshot every reader uses and the kill of one survivor.</summary>
internal interface IProcessTable
{
    /// <summary>Every process, or a typed refusal (ProcessTableUnreadable, or the short command's own refusal).</summary>
    IReadOnlyList<ProcessRow> Snapshot();

    /// <summary>Sends SIGKILL to one survivor.</summary>
    SurvivorKill Kill(int pid);
}

/// <summary>The machine's process table (ps), read at the process-listing boundary with one short limit.</summary>
internal sealed class ProcessTable(LaunchChild launch, TimeSpan limit) : IProcessTable
{
    /// <summary>The limit of every process-table read (the sampler, the survivor sweep, the foreign-Chrome wait).</summary>
    public static readonly TimeSpan SnapshotLimit = TimeSpan.FromSeconds(10);

    public IReadOnlyList<ProcessRow> Snapshot()
    {
        var result = Processes.Run(launch, ProcessLifecycle.Grace, "ps", ["-A", "-ww", "-o", "pid=,ppid=,rss=,command="], null, limit);
        if (result.ExitCode != 0) throw Unreadable($"ps exited {result.ExitCode}: {result.Error.Trim()}");
        return [.. result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(Row)];
    }

    private static ProcessRow Row(string line)
    {
        var fields = line.Split(' ', 4, StringSplitOptions.RemoveEmptyEntries);
        return fields.Length >= 3
            && int.TryParse(fields[0], NumberStyles.None, CultureInfo.InvariantCulture, out var pid)
            && int.TryParse(fields[1], NumberStyles.None, CultureInfo.InvariantCulture, out var parent)
            && long.TryParse(fields[2], NumberStyles.None, CultureInfo.InvariantCulture, out var rss)
            ? new ProcessRow(pid, parent, rss, fields.Length > 3 ? fields[3] : "")
            : throw Unreadable($"ps printed a row that does not parse: '{line}'");
    }

    private static RunnerException Unreadable(string detail) =>
        new(RunnerError.ProcessTableUnreadable, $"The process table could not be read: {detail}.");

    /// <summary>Sends SIGKILL to one process seen in a runner-owned tree. A process already gone (its pid no longer resolves, or
    /// its handle has no process) is Gone; a refused signal is Refused.</summary>
    public SurvivorKill Kill(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            process.Kill();
            return new SurvivorKill(SurvivorKillResult.Killed, "");
        }
        catch (ArgumentException gone) { return new SurvivorKill(SurvivorKillResult.Gone, gone.Message); }
        catch (InvalidOperationException gone) { return new SurvivorKill(SurvivorKillResult.Gone, gone.Message); }
        catch (Win32Exception refused) { return new SurvivorKill(SurvivorKillResult.Refused, refused.Message); }
    }

    /// <summary>The root and every descendant of it in one snapshot.</summary>
    public static IReadOnlyList<ProcessRow> Tree(IReadOnlyList<ProcessRow> rows, int root)
    {
        var children = rows.ToLookup(row => row.ParentPid);
        var tree = new List<ProcessRow>();
        var pending = new Stack<int>([root]);
        var seen = new HashSet<int>();
        while (pending.TryPop(out var pid))
        {
            if (!seen.Add(pid)) continue;
            var row = rows.FirstOrDefault(candidate => candidate.Pid == pid);
            if (row.Pid == pid && row.Pid != 0) tree.Add(row);
            foreach (var child in children[pid]) pending.Push(child.Pid);
        }
        return tree;
    }

    /// <summary>The 1-minute load average (sysctl vm.loadavg), or a typed SystemQueryFailed refusal.</summary>
    public static double LoadAverage() => LoadAverage(ChildProcess.Start);

    /// <summary><see cref="LoadAverage()"/> through a given launcher.</summary>
    internal static double LoadAverage(LaunchChild launch)
    {
        var result = Processes.Run(launch, ProcessLifecycle.Grace, "sysctl", ["-n", "vm.loadavg"], null, Processes.DefaultLimit);
        var fields = result.Output.Trim().Trim('{', '}').Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return result.ExitCode == 0 && fields.Length > 0 && double.TryParse(fields[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var load)
            ? load
            : throw new RunnerException(RunnerError.SystemQueryFailed,
                $"sysctl vm.loadavg did not return a load average (exit {result.ExitCode}: '{result.Output.Trim()}' {result.Error.Trim()}).");
    }
}

/// <summary>One survivor the sweep could not kill (its pid), or a refused table read (no pid).</summary>
internal sealed record SweepFailure(int? Pid, string Cause);

/// <summary>What one survivor sweep did: the survivors it killed and every failure.</summary>
internal sealed record SurvivorSweep(IReadOnlyList<ProcessRow> Killed, IReadOnlyList<SweepFailure> Failures);

/// <summary>Samples every registered runner-owned process tree every interval: the pids (with commands) each tree ever held, and the
/// peak summed resident memory of all registered trees. A sample whose table read is refused is skipped and counted (its timer has no
/// caller), keeping the first reason.</summary>
internal sealed class ProcessSampler : IDisposable
{
    private readonly IProcessTable _table;
    private readonly Lock _gate = new();
    private readonly Dictionary<int, Dictionary<int, string>> _seen = [];
    private readonly Timer _timer;
    private long _peakRssBytes;
    private int _skipped;
    private string? _firstSkipReason;
    private int _sampling;

    public ProcessSampler(IProcessTable table, TimeSpan interval)
    {
        _table = table;
        _timer = new Timer(_ => SampleOnTick(), null, interval, interval);
    }

    public long PeakRssBytes { get { lock (_gate) return _peakRssBytes; } }
    public int SkippedSamples { get { lock (_gate) return _skipped; } }
    public string? FirstSkipReason { get { lock (_gate) return _firstSkipReason; } }

    /// <summary>The timer's sample: a tick that finds the previous tick's sample still reading the table returns at once, so a slow
    /// read never stacks samples.</summary>
    internal void SampleOnTick()
    {
        if (Interlocked.Exchange(ref _sampling, 1) == 1) return;
        try { Sample(); }
        finally { Volatile.Write(ref _sampling, 0); }
    }

    public void Register(int root)
    {
        lock (_gate) _seen[root] = [];
        Sample();
    }

    public void Sample()
    {
        IReadOnlyList<ProcessRow> rows;
        try { rows = _table.Snapshot(); }
        catch (RunnerException refusal)
        {
            lock (_gate)
            {
                _skipped++;
                _firstSkipReason ??= refusal.Message;
            }
            return;
        }
        lock (_gate)
        {
            long total = 0;
            foreach (var (root, seen) in _seen)
                foreach (var row in ProcessTable.Tree(rows, root))
                {
                    seen.TryAdd(row.Pid, row.Command);
                    total += row.RssKilobytes * 1024;
                }
            _peakRssBytes = Math.Max(_peakRssBytes, total);
        }
    }

    /// <summary>Stops tracking a tree, as its first act under the lock (so no later sample adds to it: after the root has ended its pid
    /// may be reused), and kills, one by one, every process it was seen to hold other than its root (the root is the stop's alone) that
    /// still runs with the same pid and command (so a reused pid is never matched). A survivor already gone is neither a failure nor
    /// killed; each refused kill, and a refused table read, is a failure.</summary>
    public SurvivorSweep Sweep(int root)
    {
        Dictionary<int, string> seen;
        lock (_gate)
            seen = _seen.Remove(root, out var walked) ? walked : [];
        IReadOnlyList<ProcessRow> current;
        try { current = _table.Snapshot(); }
        catch (RunnerException refusal) { return new SurvivorSweep([], [new SweepFailure(null, refusal.Message)]); }
        var killed = new List<ProcessRow>();
        var failures = new List<SweepFailure>();
        foreach (var survivor in current.Where(row => row.Pid != root && seen.TryGetValue(row.Pid, out var command) && command == row.Command))
        {
            var kill = _table.Kill(survivor.Pid);
            switch (kill.Result)
            {
                case SurvivorKillResult.Killed: killed.Add(survivor); break;
                case SurvivorKillResult.Gone: break;
                case SurvivorKillResult.Refused: failures.Add(new SweepFailure(survivor.Pid, kill.Cause)); break;
                default: throw new InvalidOperationException($"Unknown survivor kill result {kill.Result}.");
            }
        }
        return new SurvivorSweep(killed, failures);
    }

    public void Dispose() => _timer.Dispose();
}
