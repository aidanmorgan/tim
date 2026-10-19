using Xunit;

namespace CuriousContraptions.E2eRunner.Tests;

/// <summary>AC1 / spec D9: the N rule over recorded trial tables.</summary>
public sealed class CalibrationTests
{
    private const long Memory = 64L * 1024 * 1024 * 1024;
    private const long Gib = 1024L * 1024 * 1024;

    private static CalibrationTrial Trial(int workers, double wall, double fileMs, bool pass = true, int debt = 0, long rss = 4 * Gib) =>
        new(workers, wall, pass, debt, rss, new Dictionary<string, double> { ["a"] = fileMs, ["b"] = fileMs / 2 });

    private static readonly CalibrationTrial[] Typical =
        [Trial(1, 1200, 60000), Trial(2, 650, 70000), Trial(4, 380, 80000), Trial(6, 330, 90000), Trial(8, 320, 100000)];

    [Fact]
    public void LargestNMeetingEveryRuleIsChosen() => Assert.Equal(6, Calibration.Choose(Typical, Memory));

    [Fact]
    public void GainBelowTenPercentRejectsN() =>
        Assert.Contains(TrialRejection.GainBelowTenPercent, Calibration.Rejections(Typical[4], Typical, Memory));

    [Fact]
    public void DebtFaultRejectsN()
    {
        CalibrationTrial[] trials = [Typical[0], Typical[1], Typical[2], Trial(6, 330, 90000, debt: 1), Typical[4]];
        Assert.Equal(4, Calibration.Choose(trials, Memory));
    }

    [Fact]
    public void FailedFileRejectsN()
    {
        CalibrationTrial[] trials = [Typical[0], Typical[1], Typical[2], Typical[3], Trial(8, 200, 90000, pass: false)];
        Assert.Equal(6, Calibration.Choose(trials, Memory));
    }

    [Fact]
    public void MemoryAboveHalfRejectsN()
    {
        CalibrationTrial[] trials = [Typical[0], Typical[1], Trial(4, 380, 80000, rss: 33 * Gib)];
        Assert.Equal(2, Calibration.Choose(trials, Memory));
    }

    [Fact]
    public void FileSlowerThanTwiceItsSerialDurationRejectsN()
    {
        CalibrationTrial[] trials = [Typical[0], Typical[1], Trial(4, 380, 120001)];
        Assert.Equal(2, Calibration.Choose(trials, Memory));
    }

    [Fact]
    public void FailingSerialBaselineChoosesNothing() =>
        Assert.Null(Calibration.Choose([Trial(1, 1200, 60000, pass: false)], Memory));

    [Fact]
    public void CandidatesAreCappedAtHalfTheLogicalCpus()
    {
        Assert.Equal([1, 2, 4, 6, 8], Calibration.Candidates(16));
        Assert.Equal([1, 2, 4], Calibration.Candidates(8));
    }

    [Fact]
    public void UncalibratedOrDifferentMachineRefusesParallel()
    {
        Assert.Equal(1, MeasuredWorkers.Resolve(null, 1, 16, Memory));
        Assert.Equal(1, MeasuredWorkers.Resolve(null, null, 16, Memory));
        Assert.Equal(RunnerError.Uncalibrated, Assert.Throws<RunnerException>(() => MeasuredWorkers.Resolve(null, 2, 16, Memory)).Error);
        var measured = new MeasuredWorkers(6, 16, Memory, "Google Chrome", "2026-10-19", "evidence", 4.0);
        Assert.Equal(6, MeasuredWorkers.Resolve(measured, null, 16, Memory));
        Assert.Equal(4, MeasuredWorkers.Resolve(measured, 4, 16, Memory));
        Assert.Equal(RunnerError.Uncalibrated, Assert.Throws<RunnerException>(() => MeasuredWorkers.Resolve(measured, null, 8, Memory)).Error);
        Assert.Equal(1, MeasuredWorkers.Resolve(measured, 1, 8, Memory));
    }
}
