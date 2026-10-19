namespace CuriousContraptions.E2eRunner;

/// <summary>One runner at a time per repository: an exclusive OS lock on &lt;git common dir&gt;/e2e-runner.lock, shared by every
/// worktree and held for the whole run. The kernel releases it if the runner dies. Within the run, the scheduler alone decides which
/// Chromes run together, so a SerialAlone file never shares the machine with another runner's Chrome.</summary>
internal sealed class RunnerLock : IDisposable
{
    /// <summary>The largest worker count a run may use (⌊16 logical CPUs / 2⌋, the calibration cap).</summary>
    public const int MaxWorkers = 8;

    private readonly FileStream _held;

    private RunnerLock(FileStream held, string path)
    {
        _held = held;
        Path = path;
    }

    public string Path { get; }

    /// <summary>The lock file of a repository.</summary>
    public static string For(string root) =>
        System.IO.Path.Combine(Processes.Git(root, "rev-parse", "--path-format=absolute", "--git-common-dir").Trim(), "e2e-runner.lock");

    /// <summary>Takes the lock, or refuses (RunnerBusy) while another runner holds it.</summary>
    public static RunnerLock Acquire(string path)
    {
        try { return new RunnerLock(new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None), path); }
        catch (IOException busy) { throw new RunnerException(RunnerError.RunnerBusy, $"Another E2E runner holds {path}: {busy.Message}"); }
    }

    public void Dispose() => _held.Dispose();
}
