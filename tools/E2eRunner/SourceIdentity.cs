using System.Text;

namespace CuriousContraptions.E2eRunner;

/// <summary>The source identity a run records (spec D7): HEAD, the porcelain status digest, the digest of every suite source
/// (tools/e2e/**, tools/*.ts, tools/E2eRunner/**, and the generated wire modules under tools/e2e/.wire/) and the runner's own source
/// digest. The suite digest is compared at start and end.</summary>
internal sealed record SourceIdentity(string Head, string StatusDigest, string SuiteSourceDigest, string RunnerSourceDigest)
{
    public static SourceIdentity Capture(string root) => new(
        Processes.Git(root, "rev-parse", "HEAD").Trim(),
        BundleManifest.Hash(Encoding.UTF8.GetBytes(Processes.Git(root, "status", "--porcelain=v1", "-z", "--untracked-files=all"))),
        SuiteSources(root),
        Digest(root, Files(root).Where(path => path.StartsWith("tools/E2eRunner/", StringComparison.Ordinal))));

    private const string GeneratedWireDirectory = "tools/e2e/.wire";

    /// <summary>The suite source digest alone (cheap enough to recompute at the end of a run).</summary>
    public static string SuiteSources(string root) => Digest(root, Files(root).Where(IsSuiteSource).Concat(GeneratedWire(root)));

    /// <summary>The generated wire modules the suites import (S5b): build output that git ignores, so they are listed from the file
    /// system. A folder that is absent, or removed while it is listed (a Debug build regenerating it), is recorded by its own path,
    /// which hashes as missing, so the digest changes rather than the run failing or skipping it.</summary>
    private static IEnumerable<string> GeneratedWire(string root)
    {
        try
        {
            return [.. Directory.EnumerateFiles(Path.Combine(root, GeneratedWireDirectory), "*", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/')).Order(StringComparer.Ordinal)];
        }
        catch (DirectoryNotFoundException)
        {
            return [GeneratedWireDirectory];
        }
    }

    private static bool IsSuiteSource(string path) =>
        path.StartsWith("tools/e2e/", StringComparison.Ordinal) || path.StartsWith("tools/E2eRunner/", StringComparison.Ordinal) ||
        (path.StartsWith("tools/", StringComparison.Ordinal) && path.IndexOf('/', "tools/".Length) < 0 && path.EndsWith(".ts", StringComparison.Ordinal));

    private static IEnumerable<string> Files(string root) =>
        Processes.Git(root, "ls-files", "-z", "--cached", "--others", "--exclude-standard", "--", "tools")
            .Split('\0', StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);

    /// <summary>The SHA-256 of the sorted `path\tsha256` manifest of the given files (a missing file, or a path that is not a file,
    /// hashes as "missing").</summary>
    public static string Digest(string root, IEnumerable<string> paths) =>
        BundleManifest.Hash(Encoding.UTF8.GetBytes(string.Concat(paths.Select(path => $"{path}\t{HashOf(Path.Combine(root, path))}\n"))));

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
