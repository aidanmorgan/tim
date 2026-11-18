namespace CuriousContraptions.E2eRunner;

/// <summary>Default-deny comparison of the published commit against committed, index and working inputs.</summary>
internal static class ArtifactSource
{
    public static void RequireApplicable(string root, string revision)
    {
        var resolved = Processes.Run("git", ["rev-parse", "--verify", revision + "^{commit}"], root);
        if (resolved.ExitCode != 0 || resolved.Output.Trim() != revision)
            throw PublishedArtifact.Invalid("The artifact source commit is unknown.");
        var changed = Names(root, "diff", "--name-only", "-z", "--no-renames", revision, "HEAD")
            .Concat(Names(root, "diff", "--name-only", "-z", "--no-renames", "--cached", revision))
            .Concat(Names(root, "diff", "--name-only", "-z", "--no-renames", revision));
        RefuseProduction(changed);
        var tracked = Names(root, "ls-files", "-z", "--cached");
        foreach (var path in tracked) RequireSourcePath(root, path);
        var generated = new ArtifactGeneratedPaths(tracked);
        foreach (var path in Names(root, "ls-files", "-z", "--others"))
        {
            // An audited generated directory is opaque only after its boundary and ancestors are proven ordinary.
            var directory = generated.DirectoryOf(path);
            if (directory is not null)
            {
                var full = Path.Combine(root, directory);
                ArtifactPaths.RequirePath(full);
                if (!Directory.Exists(full)) throw PublishedArtifact.Invalid($"Generated directory is not a directory: {directory}.");
                continue;
            }
            RequireSourcePath(root, path);
            if (!ArtifactGeneratedPaths.IsFile(path)) RefuseProduction([path]);
        }
    }

    private static void RequireSourcePath(string root, string path)
    {
        var full = Path.Combine(root, path);
        if (File.Exists(full) || Directory.Exists(full) || new FileInfo(full).LinkTarget is not null)
            ArtifactPaths.RequirePath(full);
    }

    private static void RefuseProduction(IEnumerable<string> paths)
    {
        var production = paths.Distinct(StringComparer.Ordinal).Where(path => !HarnessOnly(path)).Order(StringComparer.Ordinal).FirstOrDefault();
        if (production is not null) throw PublishedArtifact.Invalid($"Production inputs differ from the artifact commit: {production}.");
    }

    private static string[] Names(string root, params string[] arguments) =>
        Processes.Git(root, arguments).Split('\0', StringSplitOptions.RemoveEmptyEntries);

    private static bool HarnessOnly(string path) =>
        !path.EndsWith("/.gdignore", StringComparison.Ordinal) && (
        path.StartsWith("tools/e2e/", StringComparison.Ordinal) ||
        path.StartsWith("tools/E2eRunner/", StringComparison.Ordinal) ||
        path.StartsWith("tools/E2eRunner.Tests/", StringComparison.Ordinal) ||
        path.StartsWith("docs/", StringComparison.Ordinal) ||
        path.StartsWith("_bmad/", StringComparison.Ordinal) ||
        path.StartsWith("_bmad-output/", StringComparison.Ordinal) ||
        (!path.Contains('/') && path.EndsWith(".md", StringComparison.Ordinal)));
}
