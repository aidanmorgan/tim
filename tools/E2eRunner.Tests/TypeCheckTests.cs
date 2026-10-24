using System.Text.RegularExpressions;
using Xunit;

namespace CuriousContraptions.E2eRunner.Tests;

/// <summary>Story T.1 AC1: `tsc --noEmit -p tools/e2e/tsconfig.json` is part of Tier 1 (this test runs in `dotnet test` on a workstation
/// with the browser-suite toolchain; CI defers it to the post-catalogue browser phase through BrowserDeferral), so a suite
/// that reads the Counter's retired accessor fails before Chrome. The TypeScript toolchain resolves from the npx cache by exact version and is
/// never downloaded; a missing toolchain fails, it does not skip.</summary>
public sealed partial class TypeCheckTests
{
    private static readonly string Home = Environment.GetEnvironmentVariable("HOME") ?? throw new InvalidOperationException("HOME is not set.");

    [Fact]
    public void EverySuiteAndSharedModuleTypeChecks()
    {
        var result = TypeCheck.Run(Toolchain.ResolveTypeScript(Home), TestRepository.Root, TypeCheck.Project);
        Assert.True(result.ExitCode == 0, $"tsc --noEmit -p {TypeCheck.Project} exited {result.ExitCode}:\n{result.Output}{result.Error}");
    }

    [GeneratedRegex(@"from '(\.{1,2}/[^']+)'")]
    private static partial Regex RelativeImport();

    /// <summary>The Story 9A.2 case re-introduced: CAT-020 read the Counter target through the retired Counter `threshold()` accessor and only
    /// Chrome caught it. C-Clock deleted `threshold` and C-Memory deleted `targetCount` (the target is now the declared parameter).
    /// A copy of today's cat-020 reading either deleted accessor (its relative imports made absolute so the copy lives outside the
    /// tree) must fail the same type check with TS2339 naming the accessor.</summary>
    [Theory]
    [InlineData("threshold")]
    [InlineData("targetCount")]
    public void ASuiteReadingADeletedAccessorFailsTheTypeCheck(string accessor)
    {
        var suite = TestRepository.Read("tools/e2e/cat-020.test.ts");
        const string current = "assert.equal(counterTarget?.value(), 3);";
        Assert.Contains(current, suite, StringComparison.Ordinal);
        var directory = Path.Combine(TestRepository.Root, "tools", "e2e");
        var mutant = RelativeImport().Replace(suite.Replace(current, $"assert.equal(counterInst.{accessor}(), 3);", StringComparison.Ordinal),
            match => $"from '{Path.GetFullPath(Path.Combine(directory, match.Groups[1].Value))}'");
        var scratch = Directory.CreateTempSubdirectory("e2e-typecheck-").FullName;
        try
        {
            File.WriteAllText(Path.Combine(scratch, "cat-020-threshold.test.ts"), mutant);
            File.WriteAllText(Path.Combine(scratch, "tsconfig.json"),
                $$"""{ "extends": "{{Path.Combine(directory, "tsconfig.json")}}", "include": ["cat-020-threshold.test.ts"] }""");
            var result = TypeCheck.Run(Toolchain.ResolveTypeScript(Home), scratch, "tsconfig.json");
            Assert.NotEqual(0, result.ExitCode);
            Assert.Matches($@"cat-020-threshold\.test\.ts\(\d+,\d+\): error TS2339: Property '{accessor}' does not exist", result.Output);
        }
        finally { Directory.Delete(scratch, true); }
    }
}
