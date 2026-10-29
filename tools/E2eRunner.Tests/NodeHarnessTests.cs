using Xunit;

namespace CuriousContraptions.E2eRunner.Tests;

/// <summary>The node harnesses for game runtime code (`tools/runtime/*.test.ts`: workers, rings, client lifecycle; no browser) run in every
/// `dotnet test` of this project, one node process per file under a wall limit, judged by the runner's own pass rule: exit 0, a
/// complete plan, at least one declared test, and no failed, cancelled, skipped or todo test. The `tools/runtime/*.test.ts` files of the
/// Chrome suites' own support modules are on the BrowserDeferral list instead.</summary>
public sealed class NodeHarnessTests
{
    private static readonly TimeSpan WallLimit = TimeSpan.FromMinutes(5);

    public static TheoryData<string> Harnesses => new(TestRepository.ListFiles("tools").Where(IsHarness));

    internal static bool IsHarness(string path) =>
        ((Path.GetDirectoryName(path) == "tools" || path.StartsWith("tools/runtime/", StringComparison.Ordinal))
        && path.EndsWith(".test.ts", StringComparison.Ordinal))
        && !BrowserDeferral.Tests.Any(test => test.Runner == DeferredRunner.NodeTest && test.Test == path);

    [Theory]
    [MemberData(nameof(Harnesses))]
    public void HarnessPasses(string harness)
    {
        var executable = Path.Combine(".runtime-build", Path.ChangeExtension(harness, ".js"));
        Assert.True(File.Exists(Path.Combine(TestRepository.Root, executable)), $"Missing compiled runtime harness: {executable}");
        var run = Processes.Run("node", ["--experimental-vm-modules", "--test", "--test-reporter=tap", "--test-timeout=120000", executable],
            TestRepository.Root, WallLimit);
        var failures = TapResult.Failures(run.ExitCode, TapResult.Parse(run.Output, Path.GetFileName(harness)));
        Assert.True(failures.Count == 0, $"{harness}: {string.Join(", ", failures)} (exit {run.ExitCode})\n{run.Output}{run.Error}");
    }
}
