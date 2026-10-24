using Xunit;

namespace CuriousContraptions.E2eRunner.Tests;

/// <summary>The CLI boundary, the undeclared-suite refusal and the exit-code contract.</summary>
public sealed class CommandLineTests
{
    [Fact]
    public void VerbsAndOptionsParse()
    {
        var full = CommandLine.Parse(["full", "--out", "D", "--workers", "4"]);
        Assert.Equal((Verb.Full, "D", 4), (full.Verb, full.Options[Option.Out], full.Workers));
        Assert.Equal(Verb.Select, CommandLine.Parse(["select", "--diff", "main"]).Verb);
        Assert.Equal(Verb.Calibrate, CommandLine.Parse(["calibrate", "--out", "D"]).Verb);
        Assert.Equal(Verb.DeferredFilter, CommandLine.Parse(["deferred-filter"]).Verb);
    }

    [Theory]
    [InlineData("")]
    [InlineData("serial")]
    [InlineData("full")]
    [InlineData("full --out")]
    [InlineData("full --out D --out E")]
    [InlineData("full --out D --diff main")]
    [InlineData("select")]
    [InlineData("select --diff main --changed list.txt")]
    [InlineData("run --diff main")]
    [InlineData("checkpoint --out D")]
    [InlineData("full --out D --workers 9")]
    [InlineData("full --out D --workers two")]
    [InlineData("deferred-filter --out D")]
    public void InvalidCommandLinesAreUsageRefusals(string line)
    {
        var arguments = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var refusal = Assert.Throws<RunnerException>(() => CommandLine.Parse(arguments).Workers);
        Assert.Equal(RunnerError.Usage, refusal.Error);
    }

    [Fact]
    public void UndeclaredSuiteFileRefusesToStart()
    {
        var root = Directory.CreateTempSubdirectory("e2e-root-");
        try
        {
            Directory.CreateDirectory(Path.Combine(root.FullName, "tools", "e2e"));
            foreach (var declaration in SuiteCatalogue.All)
                File.WriteAllText(Path.Combine(root.FullName, SuiteCatalogue.RelativePath(declaration.Id)), "");
            Program.RequireDeclaredSuites(root.FullName);
            File.WriteAllText(Path.Combine(root.FullName, "tools", "e2e", "x.test.ts"), "");
            var refusal = Assert.Throws<RunnerException>(() => Program.RequireDeclaredSuites(root.FullName));
            Assert.Equal(RunnerError.UndeclaredSuite, refusal.Error);
            Assert.Contains("tools/e2e/x.test.ts", refusal.Message, StringComparison.Ordinal);
        }
        finally { root.Delete(true); }
    }

    [Fact]
    public void ExitCodesFollowTheVerdict()
    {
        Assert.Equal(0, RunSummary.ExitCode(Verdict.Pass));
        Assert.Equal(1, RunSummary.ExitCode(Verdict.Fail));
        Assert.All(new[] { Verdict.Incomplete, Verdict.IdentityChanged, Verdict.Aborted }, verdict => Assert.Equal(2, RunSummary.ExitCode(verdict)));
    }

    private static FileResult Result(string label, FileOutcome outcome, Observation observation = Observation.Clean) =>
        new(label, RunPhase.SerialAlone, null, outcome, [], 0, null, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, [], observation, null, null);

    [Fact]
    public void JudgeKeepsContendedFailuresAndNeedsACleanPass()
    {
        Assert.Equal((Verdict.Pass, (IncompleteReason?)null), RunSummary.Judge([Result("a", FileOutcome.Pass)], ["a"]));
        Assert.Equal(Verdict.Fail, RunSummary.Judge([Result("a", FileOutcome.Fail, Observation.Contended), Result("a", FileOutcome.Pass)], ["a"]).Verdict);
        Assert.Equal((Verdict.Incomplete, (IncompleteReason?)IncompleteReason.Contended),
            RunSummary.Judge([Result("a", FileOutcome.Pass, Observation.Contended), Result("a", FileOutcome.Pass, Observation.Contended)], ["a"]));
        Assert.Equal(Verdict.Pass, RunSummary.Judge([Result("a", FileOutcome.Pass, Observation.Contended), Result("a", FileOutcome.Pass)], ["a"]).Verdict);
        Assert.Equal((Verdict.Incomplete, (IncompleteReason?)IncompleteReason.NotRun), RunSummary.Judge([Result("a", FileOutcome.Pass)], ["a", "b"]));
        Assert.Equal(Verdict.Fail, RunSummary.Judge([Result("a", FileOutcome.TimedOut)], ["a"]).Verdict);
    }
}
