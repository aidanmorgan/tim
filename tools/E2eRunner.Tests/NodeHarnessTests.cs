using Xunit;

namespace CuriousContraptions.E2eRunner.Tests;

/// <summary>Runs the discovered TypeScript game runtime harnesses as emitted JavaScript, one Node process per file.
/// The shared TAP pass rule requires at least one declared test in each file.</summary>
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
        var (run, _, failures) = RunHarness(harness);
        Assert.True(failures.Count == 0, $"{harness}: {string.Join(", ", failures)} (exit {run.ExitCode})\n{run.Output}{run.Error}");
    }

    [Fact]
    public void EmittedModuleWithoutTestsIsRefused()
    {
        var (emptyRun, emptyTap, emptyFailures) = RunHarness("tools/runtime/worker-contracts.ts");
        Assert.Equal(0, emptyRun.ExitCode);
        Assert.Equal(0, emptyTap.Tests);
        Assert.Equal([FileFailure.NoTests], emptyFailures);

        var (_, oneTestTap, oneTestFailures) = RunHarness("tools/runtime/workshop-animation-output.test.ts");
        Assert.Equal(1, oneTestTap.Tests);
        Assert.Empty(oneTestFailures);
    }

    private static (ProcessResult Run, TapResult Tap, IReadOnlyList<FileFailure> Failures) RunHarness(string source)
    {
        var executable = Path.Combine(".runtime-build", Path.ChangeExtension(source, ".js"));
        Assert.True(File.Exists(Path.Combine(TestRepository.Root, executable)), $"Missing compiled runtime harness: {executable}");
        var run = Processes.Run("node", ["--experimental-vm-modules", "--test", "--test-reporter=tap", "--test-timeout=120000", executable],
            TestRepository.Root, WallLimit);
        var tap = TapResult.Parse(run.Output, Path.GetFileName(executable));
        return (run, tap, TapResult.Failures(run.ExitCode, tap));
    }
}
