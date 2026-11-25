#:property TargetFramework=net10.0
using System.Collections.Frozen;
using System.Diagnostics;
using System.Text.Json;

return StagedTestPolicy.Run();

/// <summary>The questions the staged-source rule asks git.</summary>
enum GitQuery { IntentToAdd, StagedForScan, StagedInAnyStatus, Untracked, Ignored }

/// <summary>Git's answer to one query: its output, or null with its error text when it failed.</summary>
sealed record GitAnswer(string? Output, string Error);

/// <summary>What the staged-source rule decided: a git query failed, new C# files are not staged, or the staged
/// source paths may be scanned.</summary>
sealed record StagedDecision(string? GitFailure, string[] Unstaged, string[] Scan)
{
    public bool Refuses => GitFailure is not null || Unstaged.Length > 0;
}

/// <summary>Staged-source rule. Several projects compile every C# file in a directory, so a new C# file that is in the
/// working tree but not staged is compiled and run locally while the commit would not contain it.
/// An intent-to-add entry (git add -N) is the committer's own index state and is refused on every commit. Untracked
/// C# files, and C# files ignored by name, are refused only when the commit stages source: any path, in any status
/// including deletion, that this tool scans or that is a named build or content input. A checkout holding someone else's untracked
/// files can therefore still commit documents. It checks new files only: unstaged edits to a tracked file are not
/// checked.</summary>
static class StagedSourceRule
{
    const string CSharpAnywhere = ":(top,glob,icase)**/*.cs";
    static readonly FrozenDictionary<GitQuery, string[]> Queries = new Dictionary<GitQuery, string[]>
    {
        [GitQuery.IntentToAdd] = ["diff-files", "-z", "--name-only", "--diff-filter=A", "--", CSharpAnywhere],
        // The skip scan reads staged content, so its set leaves deletions out; a type change has content and is in.
        [GitQuery.StagedForScan] = ["diff", "--cached", "--name-only", "--diff-filter=ACMRT", "-z"],
        // What makes the commit a source commit: deletions and type changes too, and a rename as its deletion and its
        // addition.
        [GitQuery.StagedInAnyStatus] = ["diff", "--cached", "--name-only", "--no-renames", "--diff-filter=ACMRTD", "-z"],
        [GitQuery.Untracked] = ["ls-files", "-z", "--others", "--exclude-standard", "--", CSharpAnywhere],
        // Ignored directories collapse to one entry ending in '/', so build output and other worktrees are not walked.
        [GitQuery.Ignored] = ["ls-files", "-z", "--others", "--ignored", "--exclude-standard", "--directory", "--", CSharpAnywhere],
    }.ToFrozenDictionary();

    /// <summary>Asks git and decides. A failed query refuses, naming the query and git's error.</summary>
    public static StagedDecision Decide(Func<string[], GitAnswer> git)
    {
        var failures = new List<string>();
        string[] Ask(GitQuery query)
        {
            var answer = git(Queries[query]);
            if (answer.Output is not null) return answer.Output.Split('\0', StringSplitOptions.RemoveEmptyEntries);
            failures.Add(Failure(Queries[query], answer.Error));
            return [];
        }
        var intentToAdd = Ask(GitQuery.IntentToAdd);
        var scan = Ask(GitQuery.StagedForScan).Where(SourcePaths.IsScanned).ToArray();
        string[] unowned = Ask(GitQuery.StagedInAnyStatus).Any(SourcePaths.StagesSource)
            ? [.. Ask(GitQuery.Untracked), .. Ask(GitQuery.Ignored).Where(path => !path.EndsWith('/'))]
            : [];
        if (failures.Count > 0) return new(string.Join('\n', failures), [], []);
        var unstaged = intentToAdd.Concat(unowned).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        return new(null, unstaged, unstaged.Length > 0 ? [] : scan);
    }

    /// <summary>One failed query, named with git's error text.</summary>
    public static string Failure(string[] query, string error) => $"git {string.Join(' ', query)}: {error.Trim()}";

    /// <summary>The refused names are printed one per line between the lines "Not staged:" and "End of the list.".</summary>
    public static string Message(StagedDecision decision) => decision.GitFailure is { } failure
        ? $"CheckStagedTests: a git query failed, so the commit is refused:\n{failure}"
        : "CheckStagedTests: new C# files are not staged (untracked, ignored by name, or git add -N). The local " +
          "build compiles them, but this commit would not contain them.\nNot staged:\n" +
          string.Concat(decision.Unstaged.Select(path => $"  {path}\n")) + "End of the list.\n" +
          "Stage each one (git add, or git add -f for a file ignored by name) or move it outside the repository " +
          "tree. Do not delete a file you do not own. Only new files are checked; unstaged edits to a tracked file " +
          "are not.";
}

/// <summary>The rule's own decision, checked on every invocation before the real check (as the maintainability gate
/// checks its canaries). Each case gives git's answers; the fake answers only the exact queries, held here as
/// independent literals. What git itself answers is proven against real git by VerifyPolicy.cs.</summary>
static class StagedSourceSelfCheck
{
    /// <summary>Staged: added or modified paths. Deleted: staged deletions. Fails: the one query git fails. Scan: the
    /// paths the skip scan must be given, stated literally (none when omitted).</summary>
    sealed record Case(string Name, string[] Staged, string[] Deleted, string[] IntentToAdd, string[] Untracked,
        string[] Ignored, GitQuery? Fails, string[] Refused, string[]? Scan = null);

    const string Pathspec = ":(top,glob,icase)**/*.cs";
    const string GitError = "fatal: this query is unavailable";
    static readonly (GitQuery Query, string[] Arguments)[] Expected =
    [
        (GitQuery.IntentToAdd, ["diff-files", "-z", "--name-only", "--diff-filter=A", "--", Pathspec]),
        (GitQuery.StagedForScan, ["diff", "--cached", "--name-only", "--diff-filter=ACMRT", "-z"]),
        (GitQuery.StagedInAnyStatus, ["diff", "--cached", "--name-only", "--no-renames", "--diff-filter=ACMRTD", "-z"]),
        (GitQuery.Untracked, ["ls-files", "-z", "--others", "--exclude-standard", "--", Pathspec]),
        (GitQuery.Ignored, ["ls-files", "-z", "--others", "--ignored", "--exclude-standard", "--directory", "--", Pathspec]),
    ];

    static readonly Case[] Cases =
    [
        new("an untracked C# file beside a staged source is refused",
            ["engine/Changed.cs"], [], [], ["engine/New.cs"], [], null, ["engine/New.cs"]),
        new("an intent-to-add C# file is refused",
            ["engine/Changed.cs"], [], ["engine/New.cs"], [], [], null, ["engine/New.cs"]),
        new("an intent-to-add C# file is refused when nothing is staged",
            [], [], ["engine/New.cs"], [], [], null, ["engine/New.cs"]),
        new("a staged source with no new file passes",
            ["engine/Changed.cs", "docs/notes.md"], [], [], [], ["bin/", ".tools/"], null, [], ["engine/Changed.cs"]),
        new("an untracked C# file passes when no source is staged",
            ["docs/notes.md"], ["docs/old.md"], [], ["engine/New.cs"], ["tools/Scratch.cs"], null, []),
        new("a C# file ignored by name beside a staged source is refused",
            ["engine/Changed.cs"], [], [], [], ["bin/", "tools/Scratch.cs"], null, ["tools/Scratch.cs"]),
        new("a staged deletion of a source counts as staged source",
            [], ["engine/Old.cs"], [], ["engine/New.cs"], [], null, ["engine/New.cs"]),
        new("a staged build input counts as staged source",
            ["engine/Engine.csproj"], [], [], ["engine/New.cs"], [], null, ["engine/New.cs"]),
        new("a staged change beside a staged source deletion passes and only the change is scanned",
            ["engine/Changed.cs"], ["engine/Old.cs"], [], [], [], null, [], ["engine/Changed.cs"]),
        new("a failed intent-to-add query is a refusal",
            ["engine/Changed.cs"], [], [], [], [], GitQuery.IntentToAdd, []),
        new("a failed staged query for the scan is a refusal",
            ["engine/Changed.cs"], [], [], [], [], GitQuery.StagedForScan, []),
        new("a failed staged query for any status is a refusal",
            ["engine/Changed.cs"], [], [], [], [], GitQuery.StagedInAnyStatus, []),
        new("a failed untracked query is a refusal",
            ["engine/Changed.cs"], [], [], [], [], GitQuery.Untracked, []),
        new("a failed ignored query is a refusal",
            ["engine/Changed.cs"], [], [], [], [], GitQuery.Ignored, []),
    ];

    public static int Count => Cases.Length;

    /// <summary>The names of the cases that do not hold; none when the rule is sound.</summary>
    public static string[] Failures() => Cases.Where(test => !Holds(test)).Select(test => test.Name).ToArray();

    static bool Holds(Case test)
    {
        var decision = StagedSourceRule.Decide(arguments => Answer(test, arguments));
        // A failure names the query that failed and carries git's error text.
        if (test.Fails is { } failed)
            return decision.Refuses && decision.GitFailure is { } failure &&
                failure == $"git {string.Join(' ', Expected.Single(query => query.Query == failed).Arguments)}: {GitError}";
        return decision.GitFailure is null && decision.Refuses == test.Refused.Length > 0 &&
            decision.Unstaged.SequenceEqual(test.Refused, StringComparer.Ordinal) &&
            decision.Scan.SequenceEqual(test.Scan ?? [], StringComparer.Ordinal);
    }

    static GitAnswer Answer(Case test, string[] arguments)
    {
        var asked = Expected.Where(query => query.Arguments.SequenceEqual(arguments, StringComparer.Ordinal)).ToArray();
        if (asked.Length != 1) return new(null, "fatal: a query the rule must not make");
        var query = asked[0].Query;
        if (query == test.Fails) return new(null, GitError + "\n");
        string[] paths = query switch
        {
            GitQuery.IntentToAdd => test.IntentToAdd,
            GitQuery.StagedForScan => test.Staged,
            GitQuery.StagedInAnyStatus => [.. test.Staged, .. test.Deleted],
            GitQuery.Untracked => test.Untracked,
            GitQuery.Ignored => test.Ignored,
            _ => throw new InvalidOperationException("Unsupported git query."),
        };
        return new(string.Concat(paths.Select(path => path + '\0')), "");
    }
}

/// <summary>External filename boundary (extensions in any case). One place says which staged paths are skip-scanned
/// and which make a commit a source commit; the rule, its self-check and the line the tool prints all read it.</summary>
static class SourcePaths
{
    static readonly FrozenSet<string> Scanned = new[]
    {
        ".cs", ".js", ".mjs", ".cjs", ".jsx", ".ts", ".tsx"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    // Not scanned, but staging one makes the commit a source commit: build inputs and content the build or the tests
    // read. Documents (.md, .yaml, .toml) are not among them.
    static readonly FrozenSet<string> SourceCommitInputs = new[]
    {
        ".csproj", ".props", ".targets", ".slnx", ".sln", ".rsp", ".editorconfig", ".json", ".tscn"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static bool IsScanned(string path) => Scanned.Contains(Path.GetExtension(path));

    public static bool StagesSource(string path) => IsScanned(path) || SourceCommitInputs.Contains(Path.GetExtension(path));

    /// <summary>Every extension that makes a commit a source commit, in a stable order.</summary>
    public static IEnumerable<string> SourceCommitExtensions =>
        Scanned.Concat(SourceCommitInputs).Order(StringComparer.Ordinal);
}

static class StagedTestPolicy
{
    const string RegistryPath = "tools/anvil/no-skipped-tests.json";
    const string ScannerSuffix = ".ts";

    public static int Run()
    {
        var failed = StagedSourceSelfCheck.Failures();
        foreach (var name in failed) Console.Error.WriteLine($"CheckStagedTests: staged-source self-check failed: {name}");
        if (failed.Length > 0) return 1;
        Console.WriteLine($"CheckStagedTests: the staged-source rule held its {StagedSourceSelfCheck.Count} self-check cases. " +
            $"A commit stages source when it stages a path ending in one of: {string.Join(' ', SourcePaths.SourceCommitExtensions)}");
        string[] topLevel = ["rev-parse", "--show-toplevel"];
        var located = Git(null, topLevel);
        if (located.Output is null)
        {
            Console.Error.WriteLine(StagedSourceRule.Message(new(StagedSourceRule.Failure(topLevel, located.Error), [], [])));
            return 1;
        }
        var root = located.Output.Trim();
        var decision = StagedSourceRule.Decide(arguments => Git(root, arguments));
        if (decision.Refuses)
        {
            Console.Error.WriteLine(StagedSourceRule.Message(decision));
            return 1;
        }
        return decision.Scan.Length == 0 ? 0 : SkipScan(root, decision.Scan);
    }

    static int SkipScan(string root, string[] paths)
    {
        var directory = Directory.CreateTempSubdirectory("anvil-no-skipped-tests-");
        try
        {
            var arguments = new List<string> { "check" };
            for (var index = 0; index < paths.Length; index++)
            {
                // Anvil (checked at 0.14.3-alpha.6) does not apply this registry to C# source.
                // This textual policy scans unchanged staged bytes with a supported source suffix.
                var snapshot = Path.Combine(directory.FullName, index + ScannerSuffix);
                // The explicit stage 0 reads the path as named, even one that looks like a stage (0:Foo.cs); the closing
                // "--" keeps it a revision when a work-tree file has that literal name (:0:Foo.cs).
                using var source = Start("git", ["show", ":0:" + paths[index], "--"], captureError: true);
                var reading = source.StandardError.ReadToEndAsync();
                using (var output = File.Create(snapshot))
                    source.StandardOutput.BaseStream.CopyTo(output);
                source.WaitForExit();
                var error = reading.Result;
                // A staged path whose content git cannot show (a gitlink, for one) is a refusal: one line that names
                // the path and carries git's own error.
                if (source.ExitCode != 0)
                {
                    var gitError = string.Join("; ", error.Split('\n',
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                    Console.Error.WriteLine($"CheckStagedTests: the staged content of {paths[index]} could not be read " +
                        $"(git show :0:{paths[index]} -- failed), so the commit is refused: {gitError}");
                    return 1;
                }
                // Git's warnings on a successful read still reach the committer.
                Console.Error.Write(error);
                arguments.Add(snapshot);
                Console.WriteLine($"Anvil snapshot {Path.GetFileName(snapshot)}: {paths[index]}");
            }
            arguments.AddRange(["--severity", "error", "--json"]);
            using var scanner = Start("anvil", arguments, directory.FullName, Path.Combine(root, RegistryPath));
            var report = scanner.StandardOutput.ReadToEnd();
            scanner.WaitForExit();
            Console.WriteLine(report);
            if (scanner.ExitCode != 0) return scanner.ExitCode;
            using var document = JsonDocument.Parse(report);
            // Reject suppressed findings too; a suppression must not disable this rule.
            var summary = document.RootElement.GetProperty("summary");
            return summary.GetProperty("errors").GetInt32() > 0 ||
                summary.GetProperty("suppressed").GetInt32() > 0 ? 1 : 0;
        }
        finally { directory.Delete(true); }
    }

    /// <summary>Git's output, or its error text when it exits with an error or cannot be started. A directory makes
    /// paths relative to it.</summary>
    static GitAnswer Git(string? directory, string[] arguments)
    {
        try
        {
            using var process = Start("git", arguments, directory, captureError: true);
            var error = process.StandardError.ReadToEndAsync();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return process.ExitCode == 0 ? new(output, "") : new(null, error.Result);
        }
        catch (System.ComponentModel.Win32Exception error)
        {
            return new(null, $"git could not be started: {error.Message}");
        }
    }

    static Process Start(string executable, IEnumerable<string> arguments,
        string? directory = null, string? registry = null, bool captureError = false)
    {
        var info = new ProcessStartInfo(executable)
        {
            RedirectStandardOutput = true, RedirectStandardError = captureError, UseShellExecute = false
        };
        if (directory is not null) info.WorkingDirectory = directory;
        if (registry is not null) info.Environment["ANVIL_REGISTRY_PATH"] = registry;
        foreach (var argument in arguments) info.ArgumentList.Add(argument);
        return Process.Start(info) ?? throw new InvalidOperationException("Cannot start policy command.");
    }
}
