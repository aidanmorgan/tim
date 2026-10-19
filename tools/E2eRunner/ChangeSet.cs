namespace CuriousContraptions.E2eRunner;

/// <summary>Where a changed-path set came from: a diff against a base ref, or an explicit list (whose base is HEAD).</summary>
internal enum ChangeSource { Diff = 1, List }

/// <summary>A validated changed-path set and the base its content diffs read.</summary>
internal sealed record ChangeSet(ChangeSource Source, string Base, IReadOnlyList<string> Paths)
{
    /// <summary>`--diff base`: `git diff --name-status -M base` (working tree; a rename gives both paths) plus untracked files.</summary>
    public static ChangeSet FromDiff(string root, string baseRef)
    {
        var paths = new SortedSet<string>(StringComparer.Ordinal);
        var fields = Processes.Git(root, "diff", "--name-status", "-M", "-z", baseRef).Split('\0', StringSplitOptions.RemoveEmptyEntries);
        for (var index = 0; index < fields.Length;)
        {
            var status = fields[index++];
            var count = status[0] is 'R' or 'C' ? 2 : 1;
            for (var path = 0; path < count && index < fields.Length; path++) paths.Add(fields[index++]);
        }
        foreach (var path in Processes.Git(root, "ls-files", "--others", "--exclude-standard", "-z").Split('\0', StringSplitOptions.RemoveEmptyEntries))
            paths.Add(path);
        return new ChangeSet(ChangeSource.Diff, baseRef, [.. paths]);
    }

    /// <summary>`--changed file`: one repository-relative path per line; absolute paths, '..', '\' and non-canonical case are refused.</summary>
    public static ChangeSet FromList(string root, string file)
    {
        if (!File.Exists(file)) throw new RunnerException(RunnerError.InvalidChangedPath, $"The changed-path list {file} does not exist.");
        var paths = File.ReadAllLines(file).Select(line => line.Trim()).Where(line => line.Length > 0)
            .Select(line => Validate(root, line)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        return new ChangeSet(ChangeSource.List, "HEAD", paths);
    }

    /// <summary>The repository-relative path boundary.</summary>
    public static string Validate(string root, string path)
    {
        if (Path.IsPathRooted(path) || path.StartsWith('/') || path.Contains('\\', StringComparison.Ordinal))
            throw new RunnerException(RunnerError.InvalidChangedPath, $"'{path}' is not a repository-relative path.");
        var segments = path.Split('/');
        if (segments.Any(segment => segment is "" or "." or ".."))
            throw new RunnerException(RunnerError.InvalidChangedPath, $"'{path}' has an empty, '.' or '..' segment.");
        var directory = root;
        foreach (var segment in segments)
        {
            if (!Directory.Exists(directory)) break;
            var entry = Directory.EnumerateFileSystemEntries(directory).Select(Path.GetFileName)
                .FirstOrDefault(name => string.Equals(name, segment, StringComparison.OrdinalIgnoreCase));
            if (entry is null) break;
            if (!string.Equals(entry, segment, StringComparison.Ordinal))
                throw new RunnerException(RunnerError.InvalidChangedPath, $"'{path}' is not in canonical case ('{entry}' on disk).");
            directory = Path.Combine(directory, entry);
        }
        return path;
    }

    /// <summary>A base-content reader for this change set (git show base:path; null when the path is absent at base).</summary>
    public Func<string, string?> BaseReader(string root) => path =>
    {
        var result = Processes.Run("git", ["-C", root, "show", $"{Base}:{path}"]);
        return result.ExitCode == 0 ? result.Output : null;
    };
}
