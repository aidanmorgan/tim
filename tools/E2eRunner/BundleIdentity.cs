using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace CuriousContraptions.E2eRunner;

/// <summary>Served: the preview must return the file's exact bytes. NotServed: a dot-file, which PhysicalFileProvider never serves.</summary>
internal enum ServeStatus { Served = 1, NotServed }

/// <summary>One bundle file: its bundle-relative path, SHA-256 and whether the preview serves it.</summary>
internal sealed record ManifestEntry(string Path, string Sha256, ServeStatus Serve);

/// <summary>How one served check ended.</summary>
internal enum ServedOutcome { Match = 1, Mismatch, NotServedInTime, ConnectionError }

/// <summary>One served check's typed outcome and its cause in words: for Mismatch the count and the first mismatching path; for
/// NotServedInTime the deadline; for ConnectionError the path and cause that ended the check.</summary>
internal sealed record ServedCheck(ServedOutcome Outcome, string Detail);

/// <summary>The bundle identity (spec D7): the SHA-256 of the sorted `relative-path\tsha256` manifest of every bundle file.</summary>
internal sealed record BundleManifest(IReadOnlyList<ManifestEntry> Entries, string Identity)
{
    /// <summary>The most fetches of one served check in flight at once.</summary>
    public const int Parallelism = 8;

    public static BundleManifest Compute(string bundleDirectory)
    {
        if (!Directory.Exists(bundleDirectory)) throw new RunnerException(RunnerError.PreviewUnavailable, $"No bundle at {bundleDirectory}.");
        var entries = Directory.EnumerateFiles(bundleDirectory, "*", SearchOption.AllDirectories)
            .Select(file => (Relative: Path.GetRelativePath(bundleDirectory, file).Replace('\\', '/'), Full: file))
            .OrderBy(file => file.Relative, StringComparer.Ordinal)
            .Select(file => new ManifestEntry(file.Relative, Hash(File.ReadAllBytes(file.Full)),
                file.Relative.Split('/').Any(segment => segment.StartsWith('.')) ? ServeStatus.NotServed : ServeStatus.Served))
            .ToArray();
        var manifest = string.Concat(entries.Select(entry => $"{entry.Path}\t{entry.Sha256}\n"));
        return new BundleManifest(entries, Hash(Encoding.UTF8.GetBytes(manifest)));
    }

    public static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    /// <summary>The served check: every Served path is fetched from the preview, at most <see cref="Parallelism"/> at once, and hashed
    /// against the manifest, all under one shared deadline (the client's HTTP timeout). The first fetch that fails, or the deadline,
    /// ends the check at once and cancels the remaining fetches, so a check costs at most one timeout. A path served with other bytes,
    /// or with an HTTP error status, is a mismatch.</summary>
    public async Task<ServedCheck> CheckServedAsync(HttpClient client, Uri baseUri, CancellationToken cancellation)
    {
        ArgumentNullException.ThrowIfNull(client);
        var mismatches = new List<string>();
        var gate = new Lock();
        ServedCheck? failed = null;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        deadline.CancelAfter(client.Timeout);
        try
        {
            await Parallel.ForEachAsync(Entries.Where(entry => entry.Serve == ServeStatus.Served),
                new ParallelOptions { MaxDegreeOfParallelism = Parallelism, CancellationToken = deadline.Token }, async (entry, token) =>
                {
                    var uri = new Uri(baseUri, string.Join('/', entry.Path.Split('/').Select(Uri.EscapeDataString)));
                    try
                    {
                        using var response = await client.GetAsync(uri, token).ConfigureAwait(false);
                        var served = response.IsSuccessStatusCode ? Hash(await response.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false)) : null;
                        if (served != entry.Sha256) lock (gate) mismatches.Add(entry.Path);
                    }
                    catch (Exception fault) when (fault is HttpRequestException or IOException)
                    {
                        lock (gate) failed ??= new ServedCheck(ServedOutcome.ConnectionError, $"{entry.Path} could not be fetched ({fault.Message})");
                        await deadline.CancelAsync().ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellation.IsCancellationRequested)
        {
            lock (gate)
                return failed ?? new ServedCheck(ServedOutcome.NotServedInTime, $"the preview at {baseUri} did not serve the bundle within the "
                    + $"HTTP timeout ({client.Timeout.TotalSeconds.ToString(CultureInfo.InvariantCulture)} s)");
        }
        lock (gate)
            return failed ?? (mismatches.Count == 0
                ? new ServedCheck(ServedOutcome.Match, "")
                : new ServedCheck(ServedOutcome.Mismatch,
                    $"{mismatches.Count} file(s) differ from the bundle on disk; first: {mismatches.Order(StringComparer.Ordinal).First()}"));
    }

    /// <summary>Refuses (ServedIdentityMismatch) unless the served check matches, naming the outcome's cause.</summary>
    public async Task VerifyServedAsync(HttpClient client, Uri baseUri, CancellationToken cancellation)
    {
        var check = await CheckServedAsync(client, baseUri, cancellation).ConfigureAwait(false);
        if (check.Outcome != ServedOutcome.Match)
            throw new RunnerException(RunnerError.ServedIdentityMismatch, $"The served check of {baseUri} did not match: {check.Detail}");
    }
}

/// <summary>Reports any change under the bundle directory. The run aborts on the first event (a conservative tripwire: identical
/// rewrites abort too); the end-of-run re-hash and second served check are authoritative.</summary>
internal interface IBundleWatcher : IDisposable
{
    /// <summary>The first change event, or null when nothing changed.</summary>
    string? FirstChange { get; }
    event Action<string>? Changed;
}

internal sealed class FileSystemBundleWatcher : IBundleWatcher
{
    private readonly FileSystemWatcher _watcher;
    private readonly Lock _gate = new();

    public FileSystemBundleWatcher(string bundleDirectory)
    {
        _watcher = new FileSystemWatcher(bundleDirectory)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size |
                NotifyFilters.CreationTime | NotifyFilters.Attributes,
        };
        _watcher.Created += (_, change) => Record($"{change.ChangeType} {change.FullPath}");
        _watcher.Changed += (_, change) => Record($"{change.ChangeType} {change.FullPath}");
        _watcher.Deleted += (_, change) => Record($"{change.ChangeType} {change.FullPath}");
        _watcher.Renamed += (_, change) => Record($"Renamed {change.OldFullPath} -> {change.FullPath}");
        _watcher.Error += (_, error) => Record($"Watcher error {error.GetException().Message}");
        _watcher.EnableRaisingEvents = true;
    }

    public string? FirstChange { get; private set; }
    public event Action<string>? Changed;

    private void Record(string change)
    {
        lock (_gate) FirstChange ??= change;
        Changed?.Invoke(change);
    }

    public void Dispose() => _watcher.Dispose();
}
