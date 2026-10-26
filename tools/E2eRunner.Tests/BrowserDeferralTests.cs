using System.Reflection;
using Xunit;

namespace CuriousContraptions.E2eRunner.Tests;

/// <summary>AGENTS.md browser exception: <see cref="BrowserDeferral"/> is the one place a test is disabled. Each entry is complete
/// and names real tests, the local test filter is generated from the list and excludes exactly its E2eRunner.Tests methods, and every
/// other test file of the game system runs in a required run.</summary>
public sealed class BrowserDeferralTests
{
    private const string Generator = "dotnet run --project tools/E2eRunner --no-build -c Debug -- deferred-filter";

    private static IEnumerable<DeferredTest> RunnerTests => BrowserDeferral.Tests.Where(test => test.Runner == DeferredRunner.E2eRunnerTests);

    [Fact]
    public void EveryEntryIsCompleteAndUnique()
    {
        Assert.All(BrowserDeferral.Tests, test =>
        {
            Assert.True(Enum.IsDefined(test.Runner), test.Test);
            Assert.Equal(ReEnableGate.PostCatalogueBrowserPhase, test.Gate);
            Assert.False(string.IsNullOrWhiteSpace(test.Reason), test.Test);
            Assert.False(string.IsNullOrWhiteSpace(test.Owner), test.Test);
        });
        Assert.Equal(BrowserDeferral.Tests.Count, BrowserDeferral.Tests.Select(test => test.Test).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void EveryDeferredRunnerTestIsATestMethodOfThisProject()
    {
        Assert.All(RunnerTests, test =>
        {
            var parts = test.Test.Split('.');
            Assert.Equal(2, parts.Length);
            var type = typeof(BrowserDeferralTests).Assembly.GetType($"CuriousContraptions.E2eRunner.Tests.{parts[0]}");
            Assert.NotNull(type);
            var method = Assert.Single(type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static),
                candidate => candidate.Name == parts[1]);
            Assert.NotNull(method.GetCustomAttribute<FactAttribute>()); // TheoryAttribute derives from FactAttribute
        });
    }

    [Fact]
    public void EveryDeferredFileEntryNamesTrackedFiles()
    {
        var files = TestRepository.ListFiles("tools");
        Assert.All(BrowserDeferral.Tests.Where(test => test.Runner != DeferredRunner.E2eRunnerTests),
            test => Assert.Contains(files, path => Matches(test.Test, path)));
    }

    [Fact]
    public void LocalFilterIsGeneratedFromTheListAndExcludesExactlyItsMethods()
    {
        Assert.Equal(RunnerTests.Select(test => $"FullyQualifiedName!=CuriousContraptions.E2eRunner.Tests.{test.Test}"),
            BrowserDeferral.LocalFilter().Split('&'));
        // The real local CLI prints exactly the independently enumerated exclusions.
        var run = Processes.Run("bash", ["-c", Generator], TestRepository.Root);
        Assert.True(run.ExitCode == 0, $"{Generator} exited {run.ExitCode}: {run.Output}{run.Error}");
        Assert.Equal(BrowserDeferral.LocalFilter() + "\n", run.Output);
    }

    /// <summary>The owner's scope rule (AGENTS.md, 24 October 2026): the system proof is the C# solution's test projects and the node
    /// harnesses for game runtime code. These repository areas are outside it and are never run, provisioned or gated.</summary>
    private static readonly string[] OutsideTheSystemProof =
    [
        ".agents/", ".claude/", "_bmad/",   // vendored BMAD tooling and its own tests
        "tools/anvil/",                      // Anvil policy tooling and its fixtures
        "_bmad-output/",                     // historical evidence, including old probe suites
        "tools/E2eRunner.Tests/Fixtures/",   // inputs of SuiteProcessTests and IdentityTests, not tests
    ];

    /// <summary>Every test file of the game system outside the C# test projects is a node harness for game runtime code
    /// (`tools/*.test.mjs`, run by NodeHarnessTests) or a deferred browser test on the list.</summary>
    [Fact]
    public void EveryOtherTestFileOfTheSystemRunsOrIsDeferred()
    {
        string[] suffixes = [".test.mjs", ".test.ts", ".test.js"];
        string[] unaccounted = [.. TestRepository.ListFiles(".")
            .Where(path => suffixes.Any(suffix => path.EndsWith(suffix, StringComparison.Ordinal))
                || Path.GetFileName(path).StartsWith("test_", StringComparison.Ordinal) && path.EndsWith(".py", StringComparison.Ordinal))
            .Where(path => !OutsideTheSystemProof.Any(scope => path.StartsWith(scope, StringComparison.Ordinal)))
            .Where(path => !NodeHarnessTests.IsHarness(path))
            .Where(path => !BrowserDeferral.Tests.Any(test => test.Runner != DeferredRunner.E2eRunnerTests && Matches(test.Test, path)))];
        Assert.Empty(unaccounted);
    }

    /// <summary>`dir/*.suffix` matches the files directly in dir; any other pattern is an exact repository path.</summary>
    private static bool Matches(string pattern, string path)
    {
        var star = pattern.IndexOf('*', StringComparison.Ordinal);
        if (star < 0) return path == pattern;
        var (prefix, suffix) = (pattern[..star], pattern[(star + 1)..]);
        return path.Length >= prefix.Length + suffix.Length && path.StartsWith(prefix, StringComparison.Ordinal)
            && path.EndsWith(suffix, StringComparison.Ordinal) && !path[prefix.Length..^suffix.Length].Contains('/', StringComparison.Ordinal);
    }
}
