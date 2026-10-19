using System.Text;

namespace CuriousContraptions.E2eRunner;

/// <summary>The source identity a run records (spec D7): HEAD, the porcelain status digest, the digest of every suite source
/// (tools/e2e/**, tools/*.mjs, tools/E2eRunner/**) and the runner's own source digest. The suite digest is compared at start and end.</summary>
internal sealed record SourceIdentity(string Head, string StatusDigest, string SuiteSourceDigest, string RunnerSourceDigest)
{
    public static SourceIdentity Capture(string root) => new(
        Processes.Git(root, "rev-parse", "HEAD").Trim(),
        BundleManifest.Hash(Encoding.UTF8.GetBytes(Processes.Git(root, "status", "--porcelain=v1", "-z", "--untracked-files=all"))),
        SuiteSources(root),
        Digest(root, Files(root).Where(path => path.StartsWith("tools/E2eRunner/", StringComparison.Ordinal))));

    /// <summary>The suite source digest alone (cheap enough to recompute at the end of a run).</summary>
    public static string SuiteSources(string root) => Digest(root, Files(root).Where(IsSuiteSource));

    private static bool IsSuiteSource(string path) =>
        path.StartsWith("tools/e2e/", StringComparison.Ordinal) || path.StartsWith("tools/E2eRunner/", StringComparison.Ordinal) ||
        (path.StartsWith("tools/", StringComparison.Ordinal) && path.IndexOf('/', "tools/".Length) < 0 && path.EndsWith(".mjs", StringComparison.Ordinal));

    private static IEnumerable<string> Files(string root) =>
        Processes.Git(root, "ls-files", "-z", "--cached", "--others", "--exclude-standard", "--", "tools")
            .Split('\0', StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);

    /// <summary>The SHA-256 of the sorted `path\tsha256` manifest of the given files (a missing file hashes as "missing").</summary>
    public static string Digest(string root, IEnumerable<string> paths) =>
        BundleManifest.Hash(Encoding.UTF8.GetBytes(string.Concat(paths.Select(path =>
        {
            var full = Path.Combine(root, path);
            return $"{path}\t{(File.Exists(full) ? BundleManifest.Hash(File.ReadAllBytes(full)) : "missing")}\n";
        }))));
}
