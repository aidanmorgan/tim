#:property TargetFramework=net10.0
using System.Diagnostics;
using System.Text.Json;

return StagedTestPolicy.Run();

enum SourceKind { CSharp, JavaScript, JavaScriptModule, JavaScriptCommon, JavaScriptReact, TypeScript, TypeScriptReact }

static class StagedTestPolicy
{
    // External filename boundary; internal choices remain enum-typed.
    static readonly IReadOnlyDictionary<string, SourceKind> Extensions = new Dictionary<string, SourceKind>
    {
        [".cs"] = SourceKind.CSharp, [".js"] = SourceKind.JavaScript,
        [".mjs"] = SourceKind.JavaScriptModule, [".cjs"] = SourceKind.JavaScriptCommon,
        [".jsx"] = SourceKind.JavaScriptReact, [".ts"] = SourceKind.TypeScript,
        [".tsx"] = SourceKind.TypeScriptReact
    };
    const string RegistryPath = "tools/anvil/no-skipped-tests.json";
    const string ScannerSuffix = ".ts";

    public static int Run()
    {
        var root = Git("rev-parse", "--show-toplevel").Trim();
        var paths = Git("diff", "--cached", "--name-only", "--diff-filter=ACMR", "-z")
            .Split('\0', StringSplitOptions.RemoveEmptyEntries)
            .Where(path => Extensions.ContainsKey(Path.GetExtension(path))).ToArray();
        if (paths.Length == 0) return 0;
        var directory = Directory.CreateTempSubdirectory("anvil-no-skipped-tests-");
        try
        {
            var arguments = new List<string> { "check" };
            for (var index = 0; index < paths.Length; index++)
            {
                // Anvil 0.12 does not dispatch C# source. This textual policy
                // scans unchanged staged bytes with a supported source suffix.
                var snapshot = Path.Combine(directory.FullName, index + ScannerSuffix);
                using var source = Start("git", ["show", ":" + paths[index]]);
                using (var output = File.Create(snapshot))
                    source.StandardOutput.BaseStream.CopyTo(output);
                source.WaitForExit();
                if (source.ExitCode != 0) throw new InvalidOperationException("Cannot read staged source.");
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

    static string Git(params string[] arguments)
    {
        using var process = Start("git", arguments);
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0) throw new InvalidOperationException("Git inspection failed.");
        return output;
    }

    static Process Start(string executable, IEnumerable<string> arguments,
        string? directory = null, string? registry = null)
    {
        var info = new ProcessStartInfo(executable) { RedirectStandardOutput = true, UseShellExecute = false };
        if (directory is not null) info.WorkingDirectory = directory;
        if (registry is not null) info.Environment["ANVIL_REGISTRY_PATH"] = registry;
        foreach (var argument in arguments) info.ArgumentList.Add(argument);
        return Process.Start(info) ?? throw new InvalidOperationException("Cannot start policy command.");
    }
}
