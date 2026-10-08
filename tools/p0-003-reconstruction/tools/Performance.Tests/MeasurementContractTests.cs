using System.Text.Json;
using System.Text.Json.Nodes;
using CuriousContraptions.PerformanceTools;

namespace CuriousContraptions.Tests;

public class MeasurementContractTests
{
    private static readonly AttemptContract Timeout = new(AttemptEnd.Timeout, 3600, 29700, 1000, CadenceTier.Sixty);
    private static readonly AttemptContract Early = new(AttemptEnd.Goal, 240, 1980, 100, CadenceTier.Sixty);
    private static AttemptObservation Run(decimal start = 31000, decimal duration = 30000,
        int ticks = 3600, AttemptEnd end = AttemptEnd.Timeout, CadenceTier tier = CadenceTier.Sixty)
    {
        var hz = tier == CadenceTier.Sixty ? 60 : 90;
        var count = (int)(duration * hz / 1000);
        var frames = Enumerable.Range(1, count).Select(i => new PresentedFrame(new((ulong)i),
            start + decimal.Round(i * 1000m / hz, 12, MidpointRounding.ToNegativeInfinity))).ToArray();
        return new(new(Guid.NewGuid()), AttemptPurpose.Measurement, end, start, start + duration, 0, ticks,
            true, true, true, 0, PresentationEvidence.SyntheticRehearsal, frames);
    }

    [Theory]
    [InlineData(CadenceTier.Sixty, 1800)]
    [InlineData(CadenceTier.Ninety, 2700)]
    public void FullTimeoutHasIndependentTickAndPacingVerdicts(CadenceTier tier, int presentations)
    {
        var observed = PerformanceAudit.AnalyzeAttempt(Timeout with { Tier = tier }, Run(tier: tier));
        Assert.Equal(MetricVerdict.Pass, observed.Coverage);
        Assert.Equal(MetricVerdict.Pass, observed.ArithmeticPacing);
        Assert.Equal(MetricVerdict.Pass, observed.SimulationRate);
        Assert.Equal(MetricVerdict.Incomplete, observed.BrowserPacing);
        Assert.Equal(presentations, observed.DistinctPresentations);
        Assert.Equal(0, observed.MissedSlots);
        Assert.Equal(1m, observed.SimulatedWallRatio);
    }

    [Fact]
    public void EarlyWinQualifiesOnlyItsFrozenShortWindow()
    {
        var run = Run(duration: 2000, ticks: 240, end: AttemptEnd.Goal);
        var shortResult = PerformanceAudit.AnalyzeAttempt(Early, run);
        Assert.Equal(MetricVerdict.Pass, shortResult.Coverage);
        Assert.Equal(120, shortResult.DistinctPresentations);
        Assert.Equal(MetricVerdict.Fail, PerformanceAudit.AnalyzeAttempt(Timeout, run).Coverage);
        var tooShort = Run(duration: 1000, ticks: 120, end: AttemptEnd.Goal);
        Assert.Equal(MetricVerdict.Incomplete, PerformanceAudit.AnalyzeAttempt(Early, tooShort).Coverage);
    }

    [Fact]
    public void SlowSimulationHasCompleteCoverageAndFailedThroughput()
    {
        var report = PerformanceAudit.AnalyzeAttempt(Timeout, Run(duration: 60000));
        Assert.Equal(MetricVerdict.Pass, report.Coverage);
        Assert.Equal(MetricVerdict.Pass, report.ArithmeticPacing);
        Assert.Equal(.5m, report.SimulatedWallRatio);
        Assert.Equal(MetricVerdict.Fail, report.SimulationRate);
    }

    [Fact]
    public void WarmupMustEndBeforeAFreshCompleteRun()
    {
        var warmup = Run(start: 0) with { Purpose = AttemptPurpose.Warmup };
        var measured = Run();
        Assert.Equal(MetricVerdict.Pass, PerformanceAudit.WarmupBeforeFreshRun([warmup], measured));
        Assert.Equal(MetricVerdict.Fail, PerformanceAudit.WarmupBeforeFreshRun([warmup], warmup));
        Assert.Equal(MetricVerdict.Fail, PerformanceAudit.WarmupBeforeFreshRun(
            [warmup with { StopObserved = false }], measured));
        Assert.Equal(MetricVerdict.Incomplete, PerformanceAudit.AnalyzeAttempt(Timeout,
            measured with { FirstTick = 1200 }).Coverage);
        var shortWarmup = Run(start: 0, duration: 2000, ticks: 240, end: AttemptEnd.Goal)
            with { Purpose = AttemptPurpose.Warmup };
        Assert.Equal(MetricVerdict.Incomplete, PerformanceAudit.WarmupBeforeFreshRun([shortWarmup], measured));
    }

    [Fact]
    public void RepeatedFramesAndExtraCallbacksCannotInflateDelivery()
    {
        var run = Run();
        var repeated = run with { Presentations = run.Presentations.Select(f => f with { Id = new(1) }).ToArray() };
        var result = PerformanceAudit.AnalyzeAttempt(Timeout, repeated);
        Assert.Equal(1, result.DistinctPresentations);
        Assert.Equal(1799, result.MissedSlots);
        Assert.Equal(MetricVerdict.Incomplete, result.ArithmeticPacing);
        Assert.Equal(MetricVerdict.Incomplete, PerformanceAudit.AnalyzeAttempt(Timeout,
            run with { Evidence = PresentationEvidence.AnimationFrameCallback }).BrowserPacing);
    }

    [Fact]
    public void DroppedGapRemainsInFullWindowAndFails()
    {
        var run = Run();
        var report = PerformanceAudit.AnalyzeAttempt(Timeout, run with
        {
            Presentations = run.Presentations.Where(f =>
                f.Milliseconds <= 41000 || f.Milliseconds > 43000).ToArray()
        });
        Assert.Equal(1680, report.DistinctPresentations);
        Assert.Equal(120, report.MissedSlots);
        Assert.Equal(56m, report.FramesPerSecond);
        Assert.Equal(MetricVerdict.Fail, report.ArithmeticPacing);
        Assert.Equal(MetricVerdict.Pass, report.Coverage);
    }

    [Fact]
    public void TooFewFramesCannotBePooledAcrossAttempts()
    {
        var a = Run(duration: 500, ticks: 60, end: AttemptEnd.Goal);
        var b = Run(start: 32000, duration: 500, ticks: 60, end: AttemptEnd.Goal);
        Assert.Equal(MetricVerdict.Incomplete, PerformanceAudit.AnalyzeAttempt(Early, a).Coverage);
        Assert.Equal(MetricVerdict.Incomplete, PerformanceAudit.AnalyzeAttempt(Early, b).Coverage);
    }

    private static TimelineInterval[] Thermal() => Enumerable.Range(0, 10).SelectMany(i =>
        new[] {
            new TimelineInterval(TimelinePhase.Active, i * 31000, i * 31000 + 30000, new AttemptId(Guid.NewGuid())),
            new TimelineInterval(TimelinePhase.Reset, i * 31000 + 30000, (i + 1) * 31000, null)
        }).ToArray();
    private static readonly ThermalContract ThermalLimits = new(300000, .9m, 2000);

    [Fact]
    public void ContinuousThermalCountsLifecycleAndRejectsMissingGap()
    {
        var intervals = Thermal();
        var good = PerformanceAudit.AnalyzeThermal(ThermalLimits, intervals);
        Assert.Equal(310000m, good.DurationMilliseconds);
        Assert.Equal(300000m / 310000m, good.ActiveFraction);
        Assert.Equal(1000m, good.MaximumGapMilliseconds);
        Assert.Equal(MetricVerdict.Pass, good.Continuity);
        Assert.Equal(MetricVerdict.Pass, good.Engagement);
        Assert.Equal(MetricVerdict.Incomplete,
            PerformanceAudit.AnalyzeThermal(ThermalLimits, intervals.Where((_, i) => i != 1).ToArray()).Continuity);
        Assert.Equal(MetricVerdict.Fail, PerformanceAudit.AnalyzeThermal(ThermalLimits,
            intervals.Select((v, i) => i == 1 ? v with { Phase = TimelinePhase.Hidden } : v).ToArray()).Continuity);
    }

    [Fact]
    public void ThermalRejectsStitchedIdentityAndCoolingBreak()
    {
        var intervals = Thermal();
        var changed = intervals.ToArray();
        changed[2] = changed[2] with { Attempt = changed[0].Attempt };
        Assert.Throws<InvalidDataException>(() => PerformanceAudit.AnalyzeThermal(ThermalLimits, changed));
        Assert.Equal(MetricVerdict.Fail, PerformanceAudit.AnalyzeThermal(ThermalLimits with
            { MaximumGapMilliseconds = 500 }, intervals).Engagement);
        Assert.Equal(MetricVerdict.Incomplete,
            PerformanceAudit.AnalyzeThermal(ThermalLimits, intervals.Take(4).ToArray()).Engagement);
    }

    [Fact]
    public void SchemaRoundTripsAndRejectsMissingUnknownUndefinedAndNull()
    {
        var packet = new MeasurementPacket(Timeout, [Run()]);
        var json = MeasurementEvidence.Write(packet);
        Assert.Equal(3600, MeasurementEvidence.Read(json).Attempts.Single().LastTick);
        // These literals are explicit JSON boundary field/enum rejection fixtures.
        foreach (var mutate in new Action<JsonObject>[] {
            root => root["unknown"] = true,
            root => root.Remove("contract"),
            root => root["attempts"] = null,
            root => root["contract"]!["tier"] = "sixty_alias",
            root => root["contract"]!["tier"] = 0,
            root => root["contract"]!["minimumTicks"] = 0,
            root => root["attempts"]![0]!["id"]!["value"] = Guid.Empty.ToString(),
            root => root["attempts"]![0]!["presentations"] = null
        })
        {
            var node = JsonNode.Parse(json)!.AsObject();
            mutate(node);
            var error = Record.Exception(() => MeasurementEvidence.Read(node.ToJsonString()));
            Assert.True(error is JsonException or InvalidDataException);
        }
        Assert.Throws<InvalidDataException>(() => PerformanceAudit.AnalyzeAttempt(
            Timeout with { Tier = (CadenceTier)999 }, Run()));
    }

    [Fact]
    public void NonzeroClockUncertaintyCannotClaimExactSlots()
    {
        var run = Run() with { Evidence = PresentationEvidence.CompositorTrace, MappingUncertaintyMilliseconds = .1m };
        Assert.Equal(MetricVerdict.Incomplete, PerformanceAudit.AnalyzeAttempt(Timeout, run).BrowserPacing);
    }

    [Fact]
    public void BadBoundariesOrStitchedPresentationOrderReject()
    {
        var run = Run();
        Assert.Throws<InvalidDataException>(() => PerformanceAudit.AnalyzeAttempt(Timeout, run with
            { Presentations = run.Presentations.Reverse().ToArray() }));
        Assert.Equal(MetricVerdict.Incomplete, PerformanceAudit.AnalyzeAttempt(Timeout,
            run with { Continuous = false }).Coverage);
        Assert.Equal(MetricVerdict.Fail, PerformanceAudit.AnalyzeAttempt(Timeout,
            run with { End = AttemptEnd.Interrupted }).Coverage);
    }

    [Fact]
    public void HiddenFailureSurvivesLaterMissingSpan()
    {
        TimelineInterval[] timeline =
        [
            new(TimelinePhase.Active, 0, 299000, new AttemptId(Guid.NewGuid())),
            new(TimelinePhase.Hidden, 299000, 300000, null),
            new(TimelinePhase.Active, 300001, 330001, new AttemptId(Guid.NewGuid()))
        ];
        Assert.Equal(MetricVerdict.Fail, PerformanceAudit.AnalyzeThermal(ThermalLimits, timeline).Continuity);
    }

    [Fact]
    public void WarmupRejectsNegativeTimeAndMalformedFreshRun()
    {
        var warmup = Run(start: 0) with { Purpose = AttemptPurpose.Warmup };
        var measured = Run();
        Assert.Equal(MetricVerdict.Fail, PerformanceAudit.WarmupBeforeFreshRun(
            [warmup with { StartMilliseconds = -1 }], measured));
        AttemptObservation[] malformed =
        [
            measured with { StartAcknowledged = false },
            measured with { StopObserved = false },
            measured with { EndMilliseconds = measured.StartMilliseconds - 1 },
            measured with { Id = default },
            measured with { End = (AttemptEnd)999 },
            measured with { LastTick = 1 },
            measured with { StartMilliseconds = -1 },
            measured with { EndMilliseconds = decimal.MaxValue },
            measured with { Presentations = null! }
        ];
        foreach (var attempt in malformed)
            Assert.Equal(MetricVerdict.Fail, PerformanceAudit.WarmupBeforeFreshRun([warmup], attempt));
    }

    [Fact]
    public void FrozenMinimumsCannotBeWeakened()
    {
        AttemptContract[] invalid =
        [
            Early with { MinimumTicks = 1 },
            Early with { MinimumWallMilliseconds = 1 },
            Early with { MinimumPresentations = 99 },
            Timeout with { MinimumTicks = 3599 },
            Timeout with { MinimumWallMilliseconds = 29699 },
            Timeout with { MinimumPresentations = 999 }
        ];
        foreach (var contract in invalid)
            Assert.Throws<InvalidDataException>(() => PerformanceAudit.AnalyzeAttempt(contract, Run()));
        Assert.Equal(MetricVerdict.Pass, PerformanceAudit.AnalyzeAttempt(
            Early, Run(duration: 2000, ticks: 240, end: AttemptEnd.Goal)).Coverage);
    }

    [Fact]
    public void ExtremeTimesAndNullPacketsRejectWithoutRuntimeExceptions()
    {
        var run = Run();
        Assert.Throws<InvalidDataException>(() => MeasurementEvidence.Write(null!));
        Assert.Throws<InvalidDataException>(() => PerformanceAudit.AnalyzeAttempt(
            Timeout, run with { EndMilliseconds = decimal.MaxValue }));
        Assert.Throws<InvalidDataException>(() => PerformanceAudit.AnalyzeAttempt(
            Timeout, run with { StartMilliseconds = 0, EndMilliseconds = .0000000000000000000000000001m }));
        Assert.Throws<InvalidDataException>(() => PerformanceAudit.AnalyzeAttempt(
            Timeout, run with { EndMilliseconds = run.StartMilliseconds + 180001 }));
        Assert.Equal(MetricVerdict.Fail, PerformanceAudit.AnalyzeAttempt(Timeout,
            run with { End = AttemptEnd.Goal, StartAcknowledged = false }).Coverage);
    }

    [Fact]
    public void DuplicateBoundaryPropertiesRejectRatherThanLastWins()
    {
        var json = MeasurementEvidence.Write(new(Timeout, [Run()]));
        string[] duplicates =
        [
            json.Replace("\"tier\":\"sixty\"", "\"tier\":\"ninety\",\"tier\":\"sixty\""),
            json.Replace("\"firstTick\":0", "\"firstTick\":9,\"firstTick\":0"),
            json[..^1] + ",\"contract\":" + JsonNode.Parse(json)!["contract"]!.ToJsonString() + "}"
        ];
        foreach (var duplicate in duplicates)
            Assert.Throws<JsonException>(() => MeasurementEvidence.Read(duplicate));
    }
}
