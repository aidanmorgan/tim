using System.Collections;
using Xunit;

namespace CuriousContraptions.E2eRunner.Tests;

/// <summary>AC1: one node process per file through the real tsx loader, judged by exit code and TAP (browserless fixtures).</summary>
public sealed class SuiteProcessTests
{
    private static readonly string Home = Environment.GetEnvironmentVariable("HOME")
        ?? throw new InvalidOperationException("HOME is not set.");

    internal static FileResult RunFixture(string fixture, TimeSpan? wallLimit = null, IDictionary? parent = null, string? output = null)
    {
        var owned = output is null;
        output ??= Directory.CreateTempSubdirectory("e2e-suite-").FullName;
        var environment = SuiteProcess.Environment(parent ?? Environment.GetEnvironmentVariables(), "http://127.0.0.1:1/", output, fixture,
            Path.Combine(output, "evidence"));
        var launch = new SuiteLaunch(fixture, $"tools/E2eRunner.Tests/Fixtures/{fixture}.test.ts", TestRepository.Root, output, environment,
            wallLimit ?? TimeSpan.FromMinutes(2), Toolchain.ResolveTsxLoader(Home));
        using var sampler = new ProcessSampler(TimeSpan.FromMilliseconds(500));
        var run = SuiteProcess.RunAsync(launch, sampler, CancellationToken.None).GetAwaiter().GetResult();
        try { return FileResult.Evaluate(run, output, fixture + ".test.ts", RunPhase.Parallel, 0); }
        finally { if (owned) Directory.Delete(output, true); }
    }

    [Fact]
    public void PassingFileIsPassAndCountsDriverSignals()
    {
        var result = RunFixture("pass");
        Assert.Equal(FileOutcome.Pass, result.Outcome);
        Assert.Equal((0, 2, 2, 0), (result.ExitCode, result.Tap!.Tests, result.Tap.Pass, result.Tap.Fail));
        Assert.Equal((1, 1, 1, 1, 1, 1, 1), (result.Tap.DebtFaults, result.Tap.Recalibrated, result.Tap.Rejected, result.Tap.RenewedLate,
            result.Tap.WideOffset, result.Tap.Unresponsive, result.Tap.LateRenewalsReported));
    }

    [Fact]
    public void FailingAssertionFailsTheFile()
    {
        var result = RunFixture("fail");
        Assert.Equal(FileOutcome.Fail, result.Outcome);
        Assert.Equal([FileFailure.NonZeroExit, FileFailure.FailedTests, FileFailure.NotOkLine], result.Failures);
        Assert.Equal((3, 2, 1), (result.Tap!.Tests, result.Tap.Pass, result.Tap.Fail));
    }

    [Fact]
    public void CloseFindingInAfterHookFailsTheFile()
    {
        var result = RunFixture("close-finding");
        Assert.Equal(FileOutcome.Fail, result.Outcome);
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains(FileFailure.NonZeroExit, result.Failures);
        Assert.True(result.Tap!.CloseFindings >= 1, "the close finding is counted");
    }

    [Fact]
    public void FileWithoutTestsIsNoTests()
    {
        var result = RunFixture("no-tests");
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(FileOutcome.Fail, result.Outcome);
        Assert.Equal([FileFailure.NoTests], result.Failures);
    }

    [Fact]
    public void ImportErrorFailsWithNoTests()
    {
        var result = RunFixture("import-error");
        Assert.Equal(FileOutcome.Fail, result.Outcome);
        Assert.Contains(FileFailure.NonZeroExit, result.Failures);
        Assert.Contains(FileFailure.NoTests, result.Failures);
    }

    [Fact]
    public void CrashFailsTheFile()
    {
        var result = RunFixture("crash");
        Assert.Equal(FileOutcome.Fail, result.Outcome);
        Assert.Contains(FileFailure.NonZeroExit, result.Failures);
    }

    [Fact]
    public void HangIsTimedOutAndTheWholeTreeIsKilled()
    {
        var result = RunFixture("hang", TimeSpan.FromSeconds(4));
        Assert.Equal(FileOutcome.TimedOut, result.Outcome);
        Assert.Equal(FileFailure.TimedOut, result.Failures[0]);
        Assert.Empty(result.Survivors);
    }

    [Fact]
    public void EnvironmentIsScrubbed()
    {
        var parent = new Hashtable(Environment.GetEnvironmentVariables())
        {
            ["E2E_SLOWMO"] = "50", ["E2E_HEADED"] = "1", ["BUMPER_PREVIEW_URL"] = "http://127.0.0.1:8060/",
            ["CLOCK_PREVIEW_URL"] = "http://127.0.0.1:8060/", ["PLAYWRIGHT_BASE_URL"] = "http://127.0.0.1:8060/", ["UNRELATED"] = "x",
        };
        var result = RunFixture("env", parent: parent);
        Assert.Equal(FileOutcome.Pass, result.Outcome);
    }

    [Fact]
    public void CleanTapWithNonZeroExitIsAFailure()
    {
        // A complete, clean TAP whose process still exited 1 (a close finding raised outside every test point): the exit code decides.
        var tap = TapResult.Parse("TAP version 13\nok 1 - passes\n1..1\n# tests 1\n# pass 1\n# fail 0\n# cancelled 0\n", "x.test.ts");
        Assert.Equal([], TapResult.Failures(0, tap));
        Assert.Equal([FileFailure.NonZeroExit], TapResult.Failures(1, tap));
    }

    [Theory]
    [InlineData("skipped", "SkippedTests")]
    [InlineData("todo", "TodoTests")]
    public void SkippedOrTodoTestFailsTheFile(string counter, string failure)
    {
        // Recorded TAP: a passing test plus one skipped or todo test (node reports them as `ok … # SKIP` / `# TODO` points).
        var tap = TapResult.Parse($"TAP version 13\nok 1 - passes\nok 2 - later # {counter.ToUpperInvariant()}\n1..2\n# tests 2\n# pass 1\n" +
            $"# fail 0\n# cancelled 0\n# {counter} 1\n", "x.test.ts");
        Assert.Equal([failure], TapResult.Failures(0, tap).Select(item => item.ToString()));
    }

    [Fact]
    public void TruncatedPlanIsIncomplete()
    {
        var tap = TapResult.Parse("TAP version 13\nok 1 - passes\n", "x.test.ts");
        Assert.Contains(FileFailure.IncompletePlan, TapResult.Failures(0, tap));
    }

    [Fact]
    public void MissingTsxIsATypedRefusal()
    {
        var empty = Directory.CreateTempSubdirectory("e2e-home-");
        try { Assert.Equal(RunnerError.ToolchainMissing, Assert.Throws<RunnerException>(() => Toolchain.ResolveTsxLoader(empty.FullName)).Error); }
        finally { empty.Delete(true); }
    }
}
