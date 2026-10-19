using System.Security.Cryptography;
using System.Text;

namespace CuriousContraptions.E2eRunner;

/// <summary>Served: the preview must return the file's exact bytes. NotServed: a dot-file, which PhysicalFileProvider never serves.</summary>
internal enum ServeStatus { Served = 1, NotServed }

/// <summary>One bundle file: its bundle-relative path, SHA-256 and whether the preview serves it.</summary>
internal sealed record ManifestEntry(string Path, string Sha256, ServeStatus Serve);

/// <summary>The bundle identity (spec D7): the SHA-256 of the sorted `relative-path\tsha256` manifest of every bundle file.</summary>
internal sealed record BundleManifest(IReadOnlyList<ManifestEntry> Entries, string Identity)
{
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

    /// <summary>The served check: every Served path fetched from the preview hashes to the manifest; returns the mismatching paths.</summary>
    public async Task<IReadOnlyList<string>> ServedMismatchesAsync(HttpClient client, Uri baseUri, CancellationToken cancellation)
    {
        ArgumentNullException.ThrowIfNull(client);
        var mismatches = new List<string>();
        var gate = new Lock();
        await Parallel.ForEachAsync(Entries.Where(entry => entry.Serve == ServeStatus.Served),
            new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = cancellation }, async (entry, token) =>
            {
                var uri = new Uri(baseUri, string.Join('/', entry.Path.Split('/').Select(Uri.EscapeDataString)));
                string? served;
                try
                {
                    using var response = await client.GetAsync(uri, token).ConfigureAwait(false);
                    served = response.IsSuccessStatusCode ? Hash(await response.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false)) : null;
                }
                catch (HttpRequestException) { served = null; }
                if (served != entry.Sha256) lock (gate) mismatches.Add(entry.Path);
            }).ConfigureAwait(false);
        return [.. mismatches.Order(StringComparer.Ordinal)];
    }

    /// <summary>Refuses (ServedIdentityMismatch) unless every Served file is served byte-identical.</summary>
    public async Task VerifyServedAsync(HttpClient client, Uri baseUri, CancellationToken cancellation)
    {
        var mismatches = await ServedMismatchesAsync(client, baseUri, cancellation).ConfigureAwait(false);
        if (mismatches.Count > 0)
            throw new RunnerException(RunnerError.ServedIdentityMismatch,
                $"{mismatches.Count} file(s) served by {baseUri} differ from the bundle on disk; first: {mismatches[0]}");
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
