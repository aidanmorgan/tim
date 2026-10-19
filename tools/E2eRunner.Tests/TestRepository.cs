namespace CuriousContraptions.E2eRunner.Tests;

/// <summary>The repository the tests run in (the directory holding CuriousContraptions.slnx above the test output).</summary>
internal static class TestRepository
{
    public static readonly string Root = FindRoot();

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "CuriousContraptions.slnx"))) return directory.FullName;
        throw new InvalidOperationException("The repository root (CuriousContraptions.slnx) is not above the test output.");
    }

    public static string Read(string relative) => File.ReadAllText(Path.Combine(Root, relative));

    /// <summary>The directory of the browserless suite fixtures.</summary>
    public static string Fixtures => Path.Combine(Root, "tools", "E2eRunner.Tests", "Fixtures");

    /// <summary>Tracked plus untracked-but-not-ignored files under the given pathspecs.</summary>
    public static IReadOnlyList<string> ListFiles(params string[] pathspecs) =>
        [.. Processes.Git(Root, ["ls-files", "-z", "--cached", "--others", "--exclude-standard", "--", .. pathspecs])
            .Split('\0', StringSplitOptions.RemoveEmptyEntries).Where(path => File.Exists(Path.Combine(Root, path))).Distinct().Order(StringComparer.Ordinal)];
}
