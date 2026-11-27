using System.Text;

namespace CuriousContraptions.E2eRunner;

/// <summary>The source identity a run records (spec D7): HEAD, the porcelain status digest, the digest of every suite source
/// (tools/e2e/**, tools/*.ts, tools/E2eRunner/**, the root npm manifests and the generated wire modules) and the runner's own source
/// digest. The suite digest is compared at start and end.</summary>
internal sealed record SourceIdentity(string Head, string StatusDigest, string SuiteSourceDigest, string RunnerSourceDigest)
{
    private const string SuiteDirectoryPrefix = "tools/e2e/";
    private static readonly string[] RetiredNpmInputs = ["tools/e2e/package.json", "tools/e2e/package-lock.json"];

    public static SourceIdentity Capture(string root, IReadOnlyList<string> admittedWireSources) => new(
        Processes.Git(root, "rev-parse", "HEAD").Trim(),
        BundleManifest.Hash(Encoding.UTF8.GetBytes(Processes.Git(root, "status", "--porcelain=v1", "-z", "--untracked-files=all"))),
        SuiteSources(root, admittedWireSources),
        Digest(root, Files(root).Where(path => path.StartsWith("tools/E2eRunner/", StringComparison.Ordinal))));

    /// <summary>Recompute the suite digest using the admitted paths and presence of retired npm inputs.</summary>
    public static string SuiteSources(string root, IReadOnlyList<string> admittedWireSources)
    {
        var sources = Files(root).Where(IsSuiteSource).Concat(admittedWireSources)
            .Where(path => !RetiredNpmInputs.Any(retired => IsRetiredNpmInput(root, path, retired)))
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);
        var retired = RetiredNpmInputs.Where(path => Path.Exists(Path.Combine(root, path)));
        return ManifestDigest(sources.Select(path => FileEntry(root, path)).Concat(retired.Select(path => $"{path}\tpresent\n")));
    }

    private static bool IsRetiredNpmInput(string root, string path, string retired)
    {
        if (!path.StartsWith(SuiteDirectoryPrefix, StringComparison.Ordinal))
            return false;

        var separator = path.IndexOf('/', SuiteDirectoryPrefix.Length);
        var prefix = separator < 0 ? path : path[..separator];
        if (prefix == retired)
            return true;

        // Compare only the immediate name. Preserve the Git spelling for physical lookup.
        var name = prefix[SuiteDirectoryPrefix.Length..].Normalize(NormalizationForm.FormC);
        if (!name.Equals(retired[SuiteDirectoryPrefix.Length..], StringComparison.OrdinalIgnoreCase))
            return false;

        var canonicalPath = Path.Combine(root, retired);
        var alternatePath = Path.Combine(root, prefix);
        // A Git-listed spelling without an entry contributes no retired data.
        if (!Path.Exists(alternatePath))
            return true;
        if (!Path.Exists(canonicalPath))
            return false;

        // If both spellings have directory entries, the filesystem treats them as distinct.
        var names = Directory.EnumerateFileSystemEntries(Path.GetDirectoryName(canonicalPath)!)
            .Select(Path.GetFileName).ToHashSet(StringComparer.Ordinal);
        return !names.Contains(Path.GetFileName(canonicalPath)) || !names.Contains(Path.GetFileName(alternatePath));
    }

    private static bool IsSuiteSource(string path) =>
        (path.StartsWith(SuiteDirectoryPrefix, StringComparison.Ordinal) && !path.StartsWith("tools/e2e/.wire/", StringComparison.Ordinal)) ||
        path.StartsWith("tools/E2eRunner/", StringComparison.Ordinal) ||
        path is "tools/runtime/wire-frame.ts" or "package.json" or "package-lock.json" ||
        (path.StartsWith("tools/", StringComparison.Ordinal) && path.IndexOf('/', "tools/".Length) < 0 && path.EndsWith(".ts", StringComparison.Ordinal));

    private static IEnumerable<string> Files(string root) =>
        Processes.Git(root, "ls-files", "-z", "--cached", "--others", "--exclude-standard", "--", "tools", "package.json", "package-lock.json")
            .Split('\0', StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);

    /// <summary>The SHA-256 of the sorted `path\tsha256` manifest of the given files (a missing file, or a path that is not a file,
    /// hashes as "missing").</summary>
    public static string Digest(string root, IEnumerable<string> paths) =>
        ManifestDigest(paths.Select(path => FileEntry(root, path)));

    private static string ManifestDigest(IEnumerable<string> entries) =>
        BundleManifest.Hash(Encoding.UTF8.GetBytes(string.Concat(entries)));

    private static string FileEntry(string root, string path) => $"{path}\t{HashOf(Path.Combine(root, path))}\n";

    // A file removed between the check and the read (a build regenerating the folder) hashes as missing too.
    private static string HashOf(string full)
    {
        if (!File.Exists(full)) return "missing";
        try
        {
            return BundleManifest.Hash(File.ReadAllBytes(full));
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return "missing";
        }
    }
}
