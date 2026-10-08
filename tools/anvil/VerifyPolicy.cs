#:property TargetFramework=net10.0
using System.Diagnostics;

var sourceRoot = Directory.GetCurrentDirectory();
var temporary = Directory.CreateTempSubdirectory("anvil-policy-verification-");
try
{
    var root = temporary.FullName;
    Directory.CreateDirectory(Path.Combine(root, "tools/anvil"));
    foreach (var name in new[] { "CheckStagedTests.cs", "no-skipped-tests.json" })
        File.Copy(Path.Combine(sourceRoot, "tools/anvil", name), Path.Combine(root, "tools/anvil", name));
    Directory.CreateDirectory(Path.Combine(root, ".git-hooks"));
    File.Copy(Path.Combine(sourceRoot, ".githooks/pre-commit"), Path.Combine(root, ".git-hooks/pre-commit"));
    Run(root, "chmod", ["+x", ".git-hooks/pre-commit"]);
    Run(root, "git", ["init", "-q"]);
    Run(root, "git", ["config", "user.name", "Policy verification"]);
    Run(root, "git", ["config", "user.email", "policy@example.invalid"]);
    Run(root, "git", ["config", "core.hooksPath", ".git-hooks"]);
    var options = new System.Text.Json.JsonSerializerOptions
    {
        RespectRequiredConstructorParameters = true,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
    };
    options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter<Source>(allowIntegerValues: false));
    options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter<Outcome>(allowIntegerValues: false));
    var fixtureContext = new FixtureContext(options);
    var cases = System.Text.Json.JsonSerializer.Deserialize<Case[]>(
        File.ReadAllText(Path.Combine(sourceRoot, "tools/anvil/fixtures.json")), fixtureContext.CaseArray)
        ?? throw new InvalidOperationException("Missing policy fixtures.");
    foreach (var invalid in new[]
    {
        """[{"Label":"invalid","Source":"Unsupported","Content":"","Expected":"Allow"}]""",
        """[{"Label":"invalid","Source":0,"Content":"","Expected":"Allow"}]""",
        """[{"Label":"invalid","Source":"CSharp","Content":"","Expected":"Unsupported"}]""",
        """[{"Label":"invalid","Source":"CSharp","Content":"","Expected":0}]"""
    })
    {
        try
        {
            System.Text.Json.JsonSerializer.Deserialize(invalid, fixtureContext.CaseArray);
            throw new InvalidOperationException("Unsupported fixture enum was accepted.");
        }
        catch (System.Text.Json.JsonException) { Console.WriteLine("PASS unsupported fixture enum rejected"); }
    }
    foreach (var test in cases)
    {
        Run(root, "git", ["read-tree", "--empty"]);
        var path = test.Source switch { Source.CSharp => "SampleTests.cs", Source.TypeScript => "sample.spec.ts", _ => throw new ArgumentOutOfRangeException() };
        File.WriteAllText(Path.Combine(root, path), test.Content);
        Run(root, "git", ["add", "--", path]);
        // Check staged bytes, even when the worktree looks clean.
        File.WriteAllText(Path.Combine(root, path), "// unstaged clean control");
        var result = Run(root, "sh", [".git-hooks/pre-commit"], requireSuccess: false);
        var actual = result.ExitCode == 0 ? Outcome.Allow : Outcome.Block;
        if (actual != test.Expected || actual == Outcome.Block && !result.Output.Contains("TEST-001", StringComparison.Ordinal))
            throw new InvalidOperationException($"FAIL {test.Label}: {result.ExitCode}\n{result.Output}");
        Console.WriteLine($"PASS {test.Label}");
    }
    Run(root, "git", ["read-tree", "--empty"]);
    var empty = Run(root, "sh", [".git-hooks/pre-commit"]);
    Console.WriteLine("PASS empty index");
    File.WriteAllText(Path.Combine(root, "SampleTests.cs"), cases.First(test => test.Expected == Outcome.Block).Content);
    Run(root, "git", ["add", "SampleTests.cs"]);
    var commit = Run(root, "git", ["commit", "-m", "Must be blocked"], requireSuccess: false);
    if (commit.ExitCode == 0 || !commit.Output.Contains("TEST-001", StringComparison.Ordinal))
        throw new InvalidOperationException("Git commit was not rejected by TEST-001.");
    Console.WriteLine("PASS real git commit rejected");
}
finally { temporary.Delete(true); }

static Result Run(string root, string executable, string[] arguments, bool requireSuccess = true)
{
    var start = new ProcessStartInfo(executable)
    {
        WorkingDirectory = root, RedirectStandardOutput = true,
        RedirectStandardError = true, UseShellExecute = false
    };
    foreach (var argument in arguments) start.ArgumentList.Add(argument);
    using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start verification command.");
    var output = process.StandardOutput.ReadToEndAsync();
    var error = process.StandardError.ReadToEndAsync();
    process.WaitForExit();
    var result = new Result(process.ExitCode, output.Result + error.Result);
    if (requireSuccess && result.ExitCode != 0) throw new InvalidOperationException(result.Output);
    return result;
}

enum Source { CSharp, TypeScript }
enum Outcome { Allow, Block }
record Case(string Label, Source Source, string Content, Outcome Expected);
record Result(int ExitCode, string Output);

[System.Text.Json.Serialization.JsonSerializable(typeof(Case[]))]
partial class FixtureContext : System.Text.Json.Serialization.JsonSerializerContext;
