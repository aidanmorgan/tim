using System.ComponentModel;
using System.Globalization;

namespace CuriousContraptions.E2eRunner;

/// <summary>One row of the machine's process table.</summary>
internal readonly record struct ProcessRow(int Pid, int ParentPid, long RssKilobytes, string Command);

/// <summary>The machine's process table (ps), read at the process-listing boundary.</summary>
internal static class ProcessTable
{
    public static IReadOnlyList<ProcessRow> Snapshot()
    {
        var result = Processes.Run("ps", ["-A", "-ww", "-o", "pid=,ppid=,rss=,command="]);
        if (result.ExitCode != 0) throw new InvalidOperationException($"ps exited {result.ExitCode}: {result.Error.Trim()}");
        var rows = new List<ProcessRow>();
        foreach (var line in result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var fields = line.Split(' ', 4, StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length < 3) continue;
            rows.Add(new ProcessRow(int.Parse(fields[0], CultureInfo.InvariantCulture), int.Parse(fields[1], CultureInfo.InvariantCulture),
                long.Parse(fields[2], CultureInfo.InvariantCulture), fields.Length > 3 ? fields[3] : ""));
        }
        return rows;
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

    /// <summary>The processes still running with the same pid and command as when they were seen (so a reused pid is never matched).</summary>
    public static IReadOnlyList<ProcessRow> StillRunning(IReadOnlyDictionary<int, string> seen)
    {
        var current = Snapshot().ToDictionary(row => row.Pid);
        return [.. seen.Where(entry => current.TryGetValue(entry.Key, out var row) && row.Command == entry.Value).Select(entry => current[entry.Key])];
    }

    /// <summary>Sends SIGKILL to one process seen in a runner-owned tree; a process that already exited is ignored.</summary>
    public static void KillOwned(int pid)
    {
        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(pid);
            process.Kill();
        }
        catch (ArgumentException) { return; } // already exited
        catch (InvalidOperationException) { return; } // already exited
        catch (Win32Exception) { return; } // exited between the check and the signal
    }

    /// <summary>The 1-minute load average (sysctl vm.loadavg).</summary>
    public static double LoadAverage()
    {
        var result = Processes.Run("sysctl", ["-n", "vm.loadavg"]);
        var fields = result.Output.Trim().Trim('{', '}').Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return result.ExitCode == 0 && fields.Length > 0 && double.TryParse(fields[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var load)
            ? load : throw new InvalidOperationException("sysctl vm.loadavg did not return a load average.");
    }
}

/// <summary>Samples every registered runner-owned process tree every interval: the pids (with commands) each tree ever held, and the
/// peak summed resident memory of all registered trees.</summary>
internal sealed class ProcessSampler : IDisposable
{
    private readonly Lock _gate = new();
    private readonly Dictionary<int, Dictionary<int, string>> _seen = [];
    private readonly Timer _timer;

    public ProcessSampler(TimeSpan interval) => _timer = new Timer(_ => Sample(), null, interval, interval);

    public long PeakRssBytes { get; private set; }

    public void Register(int root)
    {
        lock (_gate) _seen[root] = [];
        Sample();
    }

    /// <summary>Stops tracking a tree and returns every process it was seen to hold.</summary>
    public IReadOnlyDictionary<int, string> Unregister(int root)
    {
        Sample();
        lock (_gate)
        {
            var seen = _seen.GetValueOrDefault(root) ?? [];
            _seen.Remove(root);
            return seen;
        }
    }

    public void Sample()
    {
        IReadOnlyList<ProcessRow> rows;
        try { rows = ProcessTable.Snapshot(); }
        catch (InvalidOperationException) { return; }
        catch (Win32Exception) { return; }
        lock (_gate)
        {
            long total = 0;
            foreach (var (root, seen) in _seen)
                foreach (var row in ProcessTable.Tree(rows, root))
                {
                    seen.TryAdd(row.Pid, row.Command);
                    total += row.RssKilobytes * 1024;
                }
            PeakRssBytes = Math.Max(PeakRssBytes, total);
        }
    }

    public void Dispose() => _timer.Dispose();
}
