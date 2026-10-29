using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>
/// Story 9.4b P10 and Murdoch-2 H2/M1: the Simulation never trusts the clock master's derived domain once. Only an
/// exchange whose bracket is at most 2 x bound wide is evidence. Startup needs sixteen such consistent exchanges within an
/// attempt budget and refuses an inconsistent one; a running session slews a small measured drift under the same epoch
/// (requester mappings keep their calibration) and re-derives a large one under a new epoch (every requester mapping
/// recalibrates as counted, typed telemetry). Unanswered and inconclusive checks are counted, never fatal. Every other
/// loss of a mapping's calibration is a counted recalibration too.
/// </summary>
public sealed class WorkshopClockDerivationTests
{
    private const long Bound = WorkshopClockDerivation.BoundNanoseconds;
    private const long Step = WorkshopClockDerivation.SlewStepNanoseconds;
    private static readonly ClockDerivationEpoch Initial = new(0);

    private static (string Output, T Result) Capture<T>(Func<T> action)
    {
        using var output = new System.IO.StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        var previous = Console.Out;
        try { Console.SetOut(output); var result = action(); return (output.ToString(), result); }
        finally { Console.SetOut(previous); }
    }

    private static WorkshopClockDerivation Qualified()
    {
        var derivation = new WorkshopClockDerivation();
        Capture(() =>
        {
            // Integral offsets (7812 and 15624 ns), so the derived offset of a master at the bracket midpoint is exactly 0.
            for (var i = 0; i < WorkshopClockDerivation.StartupExchanges; i++)
                derivation.Qualify(new(100000000 + i, 100000000 + i + 7812, 100000000 + i + 15624, Initial));
            return 0;
        });
        Assert.True(derivation.IsQualified);
        Assert.Equal(0, derivation.StartupCorrection); // the master reads at the bracket midpoints: no offset
        derivation.Adopt(Initial);
        return derivation;
    }

    [Fact]
    public void StartupCountsOnlyNarrowConsistentExchangesAtTheBoundAndRetriesWideOrMissingOnes()
    {
        var derivation = new WorkshopClockDerivation();
        Assert.Equal(ClockDerivationOutcome.Consistent, derivation.Qualify(new(100000000, 100000000 - Bound, 100010000, Initial))); // lower edge
        Assert.Equal(ClockDerivationOutcome.Consistent, derivation.Qualify(new(100000000, 100010000 + Bound, 100010000, Initial))); // upper edge
        // Murdoch-2 H2: a bracket wider than 2 x bound proves nothing, even with the master inside it; it is retried.
        Assert.Equal(ClockDerivationOutcome.Inconclusive, derivation.Qualify(new(100000000, 100500000, 102000000, Initial)));
        Assert.Equal(ClockDerivationOutcome.Consistent,
            derivation.Qualify(new(0, 10000, WorkshopClockDerivation.MaximumBracketNanoseconds, Initial))); // widest narrow bracket
        derivation.QualificationMissed();
        Assert.Equal((3, 5, 1, 1), (derivation.Qualified, derivation.StartupAttempts, derivation.StartupInconclusive, derivation.StartupUnanswered));
        // Blind hunter 9: a narrow exchange admitting no correction in common with the others (scheduling jitter) is an
        // outlier, set aside within a bounded allowance instead of refusing the session.
        Assert.Equal(ClockDerivationOutcome.Consistent, derivation.Qualify(new(100000000, 100000000 - Bound - 1, 100010000, Initial)));
        Assert.Equal((3, 1), (derivation.Qualified, derivation.StartupOutliers));
        Assert.False(derivation.IsQualified);
        var (output, _) = Capture(() =>
        {
            for (var i = 3; i < WorkshopClockDerivation.StartupExchanges; i++) derivation.Qualify(new(100000000, 100005000, 100010000, Initial));
            return 0;
        });
        Assert.True(derivation.IsQualified);
        Assert.Contains("\"reportName\":\"Startup\"", output); // the startup counters are typed telemetry
        Assert.Contains("\"startupInconclusive\":1", output);
        Assert.Throws<InvalidOperationException>(() => derivation.Qualify(new(100000000, 100005000, 100010000, Initial)));
    }

    // D14 amended (coordinator, 19 Oct 2026; Murdoch-2 R2-F3): each worker's time origin carries its own host-clock
    // reference, so a host clock step can put a legitimate session's offset beyond the declared 0.1 ms uncertainty. The
    // offset is measured and corrected either way: a wide one is accepted, recorded once as typed counted telemetry, and
    // the correction is applied. (Formerly reviewer mutation M-C8 required this 1 ms offset to refuse the session.)
    [Fact]
    public void AStartupOffsetBeyondTheDeclaredUncertaintyIsAcceptedRecordedOnceAndApplied()
    {
        var ahead = new WorkshopClockDerivation();
        var (output, _) = Capture(() =>
        {
            for (var i = 0; i < WorkshopClockDerivation.StartupExchanges; i++) ahead.Qualify(new(200000000 + i, 201005000 + i, 200010000 + i, Initial));
            return 0;
        });
        Assert.True(ahead.IsQualified);
        var wide = Assert.Single(output.Split('\n', StringSplitOptions.RemoveEmptyEntries), line => line.StartsWith("CCGPU_CLOCK_OFFSET_WIDE ", StringComparison.Ordinal));
        using var json = System.Text.Json.JsonDocument.Parse(wide["CCGPU_CLOCK_OFFSET_WIDE ".Length..]);
        var offset = json.RootElement.GetProperty("offset").GetInt64();
        Assert.InRange(offset, -1_000_000L - Bound, -1_000_000L + Bound);
        Assert.True(json.RootElement.GetProperty("admittedLower").GetInt64() <= offset && offset <= json.RootElement.GetProperty("admittedUpper").GetInt64());
        Assert.Equal(WorkshopClockDerivation.WideStartupOffsetNanoseconds, json.RootElement.GetProperty("declaredUncertainty").GetInt64());
        Assert.Equal("1", json.RootElement.GetProperty("count").GetString());
        Assert.Equal(1UL, ahead.WideStartupOffsets);
        // Applied: the master installs the derived correction, and its corrected readings are consistent.
        Assert.Equal(offset, ahead.StartupCorrection);
        ahead.Adopt(Initial);
        Assert.Equal(offset, ahead.Correction);
        Assert.Equal(ClockDerivationOutcome.Consistent, ahead.Check(new(400000000, 400005000, 400010000, Initial), out var correction));
        Assert.Equal(offset, correction);
        Assert.Equal(1UL, ahead.WideStartupOffsets); // recorded once, at startup
        // Control: an offset within the declared uncertainty qualifies with its Startup record and no wide record.
        var narrow = new WorkshopClockDerivation();
        var (narrowOutput, _) = Capture(() =>
        {
            for (var i = 0; i < WorkshopClockDerivation.StartupExchanges; i++) narrow.Qualify(new(100000000 + i, 100000000 + i + 7812, 100000000 + i + 15624, Initial));
            return 0;
        });
        Assert.Contains("\"reportName\":\"Startup\"", narrowOutput);
        Assert.DoesNotContain("CCGPU_CLOCK_OFFSET_WIDE", narrowOutput);
        Assert.Equal(0UL, narrow.WideStartupOffsets);
    }

    // The Chrome start that the old rule refused (candidate/e2e-cumulative-run1.log): the master read 27.5 us before the
    // Simulation bracket [79.795, 79.800] ms. A constant offset between the contexts' time origins is derived, installed
    // under the initial epoch before any check, and later exchanges stamped with it are consistent.
    [Fact]
    public void AConstantOffsetBetweenTheContextsTimeOriginsIsDerivedAtStartupNotRefused()
    {
        var derivation = new WorkshopClockDerivation();
        var (startup, _) = Capture(() =>
        {
            for (var i = 0; i < WorkshopClockDerivation.StartupExchanges; i++)
                derivation.Qualify(new(79795000 + i, 79770020 + i, 79800000 + i, Initial));
            return 0;
        });
        var correction = derivation.StartupCorrection;
        Assert.InRange(correction, 27000L, 28000L);
        // Typed per-session telemetry of the derived offset and how tightly the exchanges pinned it.
        using (var json = System.Text.Json.JsonDocument.Parse(startup[(startup.IndexOf('{'))..]))
        {
            Assert.Equal(correction, json.RootElement.GetProperty("startupOffset").GetInt64());
            // Identical exchanges admit their own bracket width plus the bound on both sides: 0.005 + 2 x 0.0158 ms.
            Assert.Equal(5000L + 2 * Bound, json.RootElement.GetProperty("admittedWidth").GetInt64());
        }
        Assert.Throws<InvalidOperationException>(() => derivation.Check(new(500000000, 500004000, 500008000, Initial), out _));
        derivation.Adopt(Initial);
        Assert.Equal((Initial, correction), (derivation.Epoch, derivation.Correction));
        var stamped = 500004000L - 27500 + correction;
        Assert.Equal(ClockDerivationOutcome.Consistent, derivation.Check(new(500000000, stamped, 500008000, Initial), out _));
        Assert.Throws<InvalidOperationException>(() => derivation.StartupCorrection);
    }

    // Coordinator condition (D14 revision): after startup the running checks measure against the derived offset, so an
    // offset that keeps drifting is still followed: slower than the 50 ppm slew it is slewed under the same epoch and every
    // check stays within the bound; faster, the error grows past 0.1 ms and is re-derived under the next epoch.
    [Theory]
    [InlineData(2000L, false)] // 20 ppm: within the slew rate
    [InlineData(12000L, true)]  // 120 ppm: outruns the slew, re-derived
    public void ADriftingTimeOriginOffsetIsFollowedAfterTheDerivedStartupOffset(long driftPerCheck, bool rederives)
    {
        const long origin = 27500; // the master reads this much early at startup (the observed Chrome case)
        const long half = 7812; // half the integral 15624 ns bracket
        var derivation = new WorkshopClockDerivation();
        Capture(() =>
        {
            for (var i = 0; i < WorkshopClockDerivation.StartupExchanges; i++)
                derivation.Qualify(new(100000000 + i, 100000000 + i + half - origin, 100000000 + i + 2 * half, Initial));
            return 0;
        });
        Assert.InRange(derivation.StartupCorrection, origin, origin);
        derivation.Adopt(Initial);
        var outcomes = new List<ClockDerivationOutcome>();
        var worst = 0L;
        Capture(() =>
        {
            for (var check = 1; check <= 60; check++)
            {
                var before = 200_000_000L + check * 1_000_000L;
                var offset = origin + driftPerCheck * check; // the master's true lag behind the Simulation now
                var stamped = before + half - offset + derivation.Correction;
                var outcome = derivation.Check(new(before, stamped, before + 2 * half, derivation.Epoch), out _);
                outcomes.Add(outcome);
                worst = Math.Max(worst, Math.Abs(before + half - stamped));
                if (outcome == ClockDerivationOutcome.Slewed) derivation.Adopt(derivation.Epoch);
                if (outcome == ClockDerivationOutcome.Rederived) derivation.Adopt(new(derivation.Epoch.Value + 1));
            }
            return 0;
        });
        Assert.Contains(ClockDerivationOutcome.Slewed, outcomes);
        Assert.Equal(rederives, outcomes.Contains(ClockDerivationOutcome.Rederived));
        var final = origin + driftPerCheck * 60;
        var lag = half + Bound; // a check corrects only once its reading leaves the bracket by more than the bound
        if (!rederives)
        {
            Assert.Equal(Initial, derivation.Epoch);
            Assert.InRange(derivation.Correction, final - lag - Step - driftPerCheck, final + Bound);
            Assert.True(worst <= lag + driftPerCheck, $"worst error {worst} ms");
        }
        else
        {
            Assert.True(derivation.Epoch.Value >= 1);
            Assert.InRange(derivation.Correction, final - WorkshopClockDerivation.RederiveThresholdNanoseconds - driftPerCheck - lag, final + Bound);
        }
    }

    [Fact]
    public void MoreOutliersThanTheAllowanceRefuseTheSessionAsNoStableOffset()
    {
        // Two masters 1 ms apart in turn: no correction is admitted by all but at most StartupOutlierLimit exchanges.
        var derivation = new WorkshopClockDerivation();
        for (var i = 0; i < 2 * WorkshopClockDerivation.StartupOutlierLimit + 1; i++)
            derivation.Qualify(new(100000000 + i, 100000000 + i + 5000 + (i % 2) * 1_000_000, 100000000 + i + 10000, Initial));
        var error = Assert.Throws<InvalidOperationException>(() => derivation.Qualify(new(200000000, 201005000, 200010000, Initial)));
        Assert.Contains("not a stable offset within the nanosecond bound", error.Message);
    }

    [Fact]
    public void OneJitteredExchangeAmongTheStartupExchangesIsSetAsideAndTheOffsetComesFromTheRest()
    {
        var derivation = new WorkshopClockDerivation();
        Capture(() =>
        {
            derivation.Qualify(new(99000000, 99500000, 99010000, Initial)); // 0.49 ms late: no correction in common with the others
            for (var i = 0; i < WorkshopClockDerivation.StartupExchanges; i++)
                derivation.Qualify(new(100000000 + i, 100000000 + i + 7812, 100000000 + i + 15624, Initial));
            return 0;
        });
        Assert.True(derivation.IsQualified);
        Assert.Equal(1, derivation.StartupOutliers);
        Assert.Equal(0, derivation.StartupCorrection);
    }

    [Fact]
    public void AnExhaustedStartupBudgetRefusesTheSessionNamingEveryCounter()
    {
        var derivation = new WorkshopClockDerivation();
        for (var i = 0; i < WorkshopClockDerivation.StartupAttemptLimit; i++)
            if (i % 2 == 0) derivation.QualificationMissed(); else derivation.Qualify(new(100000000, 100500000, 102000000, Initial));
        var error = Assert.Throws<InvalidOperationException>(() => derivation.QualificationMissed());
        Assert.Contains("0 of 16 narrow consistent exchanges in 500 attempts (250 wider than 31600 ns, 250 unanswered)", error.Message);
    }

    [Theory]
    [InlineData(-1, -1, 0)]
    [InlineData(2, 1, 1)] // reversed bracket
    public void AMalformedExchangeRejects(long before, long master, long after)
    {
        Assert.Throws<ArgumentException>(() => new WorkshopClockDerivation().Qualify(new(before, master, after, Initial)));
        Assert.Throws<ArgumentException>(() => Qualified().Check(new(before, master, after, Initial), out _));
    }

    [Fact]
    public void AConsistentCheckChangesNothingAndReportsNothing()
    {
        var derivation = Qualified();
        var (output, outcome) = Capture(() => derivation.Check(new(500000000, 500004000, 500008000, Initial), out var correction)
            == ClockDerivationOutcome.Consistent && correction == 0);
        Assert.True(outcome);
        Assert.Equal("", output);
        Assert.Equal((1UL, 0UL, 0UL, Initial), (derivation.Checks, derivation.Slews, derivation.Rederivations, derivation.Epoch));
    }

    [Fact]
    public void AWideBracketWithTheMasterInsideIsInconclusiveNotConsistent()
    {
        var derivation = Qualified();
        Assert.Equal(ClockDerivationOutcome.Inconclusive, derivation.Check(new(500000000, 500500000, 502000000, Initial), out var correction));
        Assert.Equal((0L, 1UL), (correction, derivation.Inconclusive));
    }

    // Murdoch-2 M1: a small drift is slewed by at most one step per check under the same epoch, so no requester mapping
    // recalibrates; it converges until the reading is consistent.
    [Fact]
    public void ASmallDriftSlewsUnderTheSameEpochByAtMostOneStepPerCheckUntilConsistent()
    {
        var derivation = Qualified();
        const long drift = 30000; // the master reads 30 us late: beyond the bound, within the re-derivation threshold
        var outcomes = new List<ClockDerivationOutcome>();
        for (var check = 0; check < 10; check++)
        {
            var before = 500_000_000L + check * 1_000_000L;
            var master = before + 4000 + drift + derivation.Correction;
            var outcome = derivation.Check(new(before, master, before + 8000, derivation.Epoch), out var correction);
            outcomes.Add(outcome);
            if (outcome == ClockDerivationOutcome.Slewed)
            {
                Assert.InRange(derivation.Correction - correction, Step, Step);
                derivation.Adopt(derivation.Epoch);
            }
        }
        Assert.Equal(Initial, derivation.Epoch);
        Assert.Equal(ClockDerivationOutcome.Slewed, outcomes[0]);
        Assert.Equal(ClockDerivationOutcome.Consistent, outcomes[^1]);
        Assert.InRange(derivation.Correction, -drift - Step, -drift + Bound);
        Assert.Equal(0UL, derivation.Rederivations);
        Assert.Throws<InvalidOperationException>(() => derivation.Adopt(new(1)));
    }

    [Fact]
    public void ALargeDriftRederivesToTheBracketMidpointUnderTheNextEpoch()
    {
        var derivation = Qualified();
        var (output, result) = Capture(() => (derivation.Check(new(500000000, 501004000, 500008000, Initial), out var correction), correction));
        Assert.Equal(ClockDerivationOutcome.Rederived, result.Item1);
        Assert.Equal(-1_000_000L, result.Item2);
        var line = Assert.Single(output.Split('\n', StringSplitOptions.RemoveEmptyEntries));
        Assert.StartsWith("CCGPU_CLOCK_DERIVATION ", line);
        using (var json = System.Text.Json.JsonDocument.Parse(line["CCGPU_CLOCK_DERIVATION ".Length..]))
        {
            Assert.Equal("Rederived", json.RootElement.GetProperty("outcomeName").GetString());
            Assert.Equal(1UL, json.RootElement.GetProperty("rederivations").GetUInt64());
            Assert.Equal(result.Item2, json.RootElement.GetProperty("pendingCorrection").GetInt64());
        }
        // The correction takes effect only when the ring installed it under the next epoch.
        Assert.Throws<InvalidOperationException>(() => derivation.Check(new(600000000, 600004000, 600008000, Initial), out _));
        Assert.Throws<InvalidOperationException>(() => derivation.Adopt(Initial));
        derivation.Adopt(new(1));
        Assert.Equal((new ClockDerivationEpoch(1), result.Item2), (derivation.Epoch, derivation.Correction));
        // An exchange still stamped under the old epoch is a protocol violation, not a drift.
        Assert.Throws<InvalidOperationException>(() => derivation.Check(new(600000000, 600004000, 600008000, Initial), out _));
        Assert.Equal(ClockDerivationOutcome.Consistent, derivation.Check(new(600000000, 600004000, 600008000, new(1)), out _));
        Assert.Throws<InvalidOperationException>(() => derivation.Adopt(new(2)));
    }

    [Fact]
    public void InconclusiveAndUnansweredChecksAreCountedReportingTheFirstOfEachRunAndASummaryEveryHundredChecks()
    {
        var derivation = Qualified();
        var (output, outcomes) = Capture(() => new[]
        {
            derivation.Check(new(500000000, 503000000, 501000000, Initial), out _),
            derivation.Check(new(510000000, 513000000, 511000000, Initial), out _),
        });
        Assert.Equal(new[] { ClockDerivationOutcome.Inconclusive, ClockDerivationOutcome.Inconclusive }, outcomes);
        Assert.Single(output.Split('\n', StringSplitOptions.RemoveEmptyEntries));
        Assert.Equal((2UL, 0UL, 0L), (derivation.Inconclusive, derivation.Rederivations, derivation.Correction));
        var (missed, _) = Capture(() => { derivation.Missed(); derivation.Missed(); return 0; });
        Assert.Equal(2UL, derivation.Unanswered);
        Assert.Single(missed.Split('\n', StringSplitOptions.RemoveEmptyEntries)); // the first unanswered check of the run
        derivation.Retried(); // a wide exchange retried within a check is counted, not a check of its own
        Assert.Equal((1UL, 4UL), (derivation.CheckRetries, derivation.Checks));
        var (summary, _) = Capture(() =>
        {
            while (derivation.Checks < WorkshopClockDerivation.SummaryInterval) derivation.Check(new(520000000, 520004000, 520008000, Initial), out _);
            return 0;
        });
        Assert.Contains("\"reportName\":\"Summary\"", summary);
        Assert.Contains("\"checks\":100", summary);
        Assert.Contains("\"checkRetries\":1", summary);
        Assert.Equal(Initial, derivation.Epoch);
    }

    [Fact]
    public void ChecksRequireAQualifiedDerivation()
    {
        var derivation = new WorkshopClockDerivation();
        Assert.Throws<InvalidOperationException>(() => derivation.Check(new(1000000, 1000000, 1000000, Initial), out _));
        Assert.Throws<InvalidOperationException>(() => derivation.Missed());
        Assert.Throws<InvalidOperationException>(() => derivation.Retried());
    }

    [Fact]
    public void OverflowingDerivationCandidatesLeaveCountersAndPendingCorrectionUntouched()
    {
        var startup = new WorkshopClockDerivation();
        Assert.Throws<OverflowException>(() => startup.Qualify(new(long.MaxValue, 0, long.MaxValue, Initial)));
        Assert.Equal((0, 0, false), (startup.StartupAttempts, startup.Qualified, startup.IsQualified));
        var running = new WorkshopClockDerivation();
        Capture(() => {
            for (var i = 0; i < WorkshopClockDerivation.StartupExchanges; i++)
                running.Qualify(new(1_000_000, long.MaxValue - 1_000_000, 1_000_000, Initial));
            return 0;
        });
        running.Adopt(Initial);
        var correction = running.Correction;
        Assert.Throws<OverflowException>(() => running.Check(new(0, 10_000_000, 0, Initial), out _));
        Assert.Equal((0ul, 0ul, correction, Initial), (running.Checks, running.Rederivations, running.Correction, running.Epoch));
        // No hidden pending installation was retained by the rejected candidate.
        Assert.Equal(ClockDerivationOutcome.Consistent, running.Check(new(0, 0, 0, Initial), out _));
    }

    [Fact]
    public void IntegralInteropCarriesAdjacentLargeStampsAndRefusesMalformedEnvelopes()
    {
        var bytes = new byte[ClockDerivationWire.ExchangeBytes];
        System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(bytes, long.MaxValue - 2);
        System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(8), long.MaxValue - 1);
        System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(16), long.MaxValue);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(24), uint.MaxValue);
        Assert.Equal(new ClockDerivationExchange(long.MaxValue - 2, long.MaxValue - 1, long.MaxValue, new(uint.MaxValue)),
            ClockDerivationWire.DecodeExchange(bytes));
        Assert.Throws<ArgumentException>(() => ClockDerivationWire.DecodeExchange(bytes.AsSpan(1)));
        bytes[28] = 1;
        Assert.Throws<ArgumentException>(() => ClockDerivationWire.DecodeExchange(bytes));
        var correction = ClockDerivationWire.EncodeCorrection(long.MinValue, ClockDerivationOutcome.Rederived);
        Assert.Equal(16, correction.Length);
        Assert.Equal(long.MinValue, System.Buffers.Binary.BinaryPrimitives.ReadInt64LittleEndian(correction));
        Assert.Equal((uint)ClockDerivationOutcome.Rederived, System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(correction.AsSpan(8)));
        Assert.Equal(0u, System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(correction.AsSpan(12)));
        Assert.Throws<ArgumentOutOfRangeException>(() => ClockDerivationWire.EncodeCorrection(0, ClockDerivationOutcome.Inconclusive));
    }

    // ---- requester mappings ----

    private static readonly WorkshopClockPeer Peer = new(new(11, 23), new(7), new(0), new(1), new(100_000), WorkshopRuntimeRole.Browser);

    private static WorkshopClockMapping Ready(ClockDerivationEpoch derivation)
    {
        var map = new WorkshopClockMapping(Peer);
        for (var i = 0; i < WorkshopClockMapping.ProbeCapacity; i++) Accept(map, i * 10_000_000, derivation);
        Assert.Equal(ClockMappingState.Ready, map.State);
        return map;
    }

    private static ClockObservation Accept(WorkshopClockMapping map, long sent, ClockDerivationEpoch derivation, long offset = 50_000_000)
    {
        var probe = map.BeginProbe(new(sent))!.Value;
        return map.Receive(new(probe, new(sent + offset), new(sent + offset), new(sent), derivation));
    }

    [Fact]
    public void AReplyUnderANewDerivationEpochRecalibratesThenSeedsTheFreshCalibration()
    {
        var map = Ready(Initial);
        var epoch = map.DisplayEpoch;
        // The first refresh probe after qualification (refresh spacing 250 ms) is answered under epoch 1.
        var (output, observation) = Capture(() => Accept(map, 320_000_000, new(1), 49_000_000));
        Assert.Equal(ClockProbeOutcome.Accepted, observation.Outcome);
        Assert.Equal((1UL, ClockRecalibrationCause.MasterRederived), (map.Recalibrations, map.LastRecalibration!.Value.Cause));
        Assert.Equal(epoch + 1, map.DisplayEpoch);
        Assert.Equal(1, map.ValidProbeCount);
        Assert.Contains("\"causeName\":\"MasterRederived\"", output);
        Assert.Contains("\"derivation\":\"0\"", output);
        for (var i = 1; i < WorkshopClockMapping.ProbeCapacity; i++) Accept(map, 320_000_000 + i * 10_000_000, new(1), 49_000_000);
        Assert.Equal(ClockMappingState.Ready, map.State);
        Assert.Equal(1UL, map.Recalibrations);
        Assert.Throws<ArgumentException>(() => Accept(map, 700_000_000, Initial));
    }

    // Murdoch-2 M1, the other side: slewed replies keep the epoch, so the mapping keeps its calibration and its display
    // epoch; a 5 us slew is far inside every retained interval.
    [Fact]
    public void ASlewedMasterKeepsEveryRequesterMappingCalibrated()
    {
        var map = Ready(Initial);
        var epoch = map.DisplayEpoch;
        Assert.Equal(ClockProbeOutcome.Accepted, Accept(map, 320_000_000, Initial, 50_005_000).Outcome);
        Assert.Equal(ClockProbeOutcome.Accepted, Accept(map, 570_000_000, Initial, 50_010_000).Outcome);
        Assert.Equal((0UL, epoch, ClockMappingState.Ready), (map.Recalibrations, map.DisplayEpoch, map.State));
    }

    // R2-F1 decision (coordinator, 19 Oct 2026): a lapse is renewed and checked only when the requester itself did not run.
    // A lapse while a probe is outstanding (the master or the reply path failed) stays a counted recalibration.
    [Fact]
    public void ALapseWithAProbeOutstandingIsACountedRecalibration()
    {
        var map = Ready(Initial);
        Assert.NotNull(map.BeginProbe(new(320_000_000))); // the refresh is sent and never answered
        var (output, _) = Capture(() => { map.Observe(new(70_000_000 + WorkshopClockMapping.MappingLifetime + 1)); return 0; });
        Assert.Equal(ClockMappingState.Expired, map.State);
        Assert.Equal((1UL, ClockRecalibrationCause.MappingLifetime), (map.Recalibrations, map.LastRecalibration!.Value.Cause));
        Assert.Contains("\"causeName\":\"MappingLifetime\"", output);
        Assert.Equal(0UL, map.LateRenewals);
    }

    // A requester that kept running but had no probe outstanding at the instant of the lapse (its refresh was answered
    // unusably and the next one falls due only as the mapping lapses) is not one whose own event loop stopped.
    [Fact]
    public void ALapseOfARequesterThatKeptRunningIsACountedRecalibration()
    {
        var map = Ready(Initial);
        var refresh = map.BeginProbe(new(320_000_000))!.Value;
        // Reversed master stamps: classified and dropped, the probe is no longer outstanding.
        Assert.Equal(ClockProbeOutcome.Reversed, map.Receive(new(refresh, new(370_000_000), new(369_999_999), new(321_000_000), Initial)).Outcome);
        Assert.Null(map.Pending);
        map.Observe(new(560_000_000)); // the requester runs; its next refresh is due at 570 ms, when the mapping lapses
        var (output, _) = Capture(() => { map.Observe(new(570_000_001)); return 0; });
        Assert.Equal((1UL, ClockRecalibrationCause.MappingLifetime, 0UL), (map.Recalibrations, map.LastRecalibration!.Value.Cause, map.LateRenewals));
        Assert.DoesNotContain("CCGPU_CLOCK_RENEWED_LATE", output);
    }

    /// <summary>Ready (eight slots, offset +50 ms, the last answered at 70 ms), then the requester's own event loop does
    /// not run until <paramref name="resumed"/>: the mapping lapses with no probe outstanding and is renewing.</summary>
    private static WorkshopClockMapping Stalled(long resumed)
    {
        var map = Ready(Initial);
        var (output, _) = Capture(() => { map.Observe(new(resumed)); return 0; });
        Assert.Equal("", output);
        Assert.Equal((ClockMappingState.Renewing, false, 0UL), (map.State, map.IsQualified, map.Recalibrations));
        // Consumers hold, as they do for any mapping without a current calibration.
        Assert.False(map.TryMasterNow(new(resumed), out _));
        return map;
    }

    [Fact]
    public void AStalledRequestersLapseConsistentWithTheOldMappingIsRenewedLateWithoutARecalibration()
    {
        const long resumed = 770_000_000; // a 700 ms stall after the last refresh
        var map = Stalled(resumed);
        var epoch = map.DisplayEpoch;
        var probe = map.BeginProbe(new(resumed)); // the fresh exchange is taken at once
        Assert.NotNull(probe);
        var (output, observation) = Capture(() => map.Receive(new(probe!.Value, new(resumed + 50_000_000), new(resumed + 50_000_000), new(resumed), Initial)));
        Assert.Equal(ClockProbeOutcome.Accepted, observation.Outcome);
        Assert.Equal((ClockMappingState.Ready, 0UL, epoch, 1UL), (map.State, map.Recalibrations, map.DisplayEpoch, map.LateRenewals));
        Assert.True(map.TryMasterNow(new(resumed), out _));
        var line = Assert.Single(output.Split('\n', StringSplitOptions.RemoveEmptyEntries));
        Assert.StartsWith("CCGPU_CLOCK_RENEWED_LATE ", line);
        using var json = System.Text.Json.JsonDocument.Parse(line["CCGPU_CLOCK_RENEWED_LATE ".Length..]);
        var root = json.RootElement;
        Assert.Equal((uint)WorkshopRuntimeRole.Browser, root.GetProperty("role").GetUInt32());
        Assert.Equal("1", root.GetProperty("count").GetString());
        Assert.Equal((resumed - 70_000_000).ToString(System.Globalization.CultureInfo.InvariantCulture), root.GetProperty("lapseAge").GetString());
        Assert.Equal("0", root.GetProperty("derivation").GetString());
        Assert.Equal(epoch.ToString(System.Globalization.CultureInfo.InvariantCulture), root.GetProperty("displayEpoch").GetString());
        static Int128 Exact(System.Text.Json.JsonElement value) => Int128.Parse(value.GetString()!, System.Globalization.CultureInfo.InvariantCulture);
        var newLower = Exact(root.GetProperty("newLower")); var newUpper = Exact(root.GetProperty("newUpper"));
        var oldLower = Exact(root.GetProperty("oldLower")); var oldUpper = Exact(root.GetProperty("oldUpper"));
        Assert.Equal(WorkshopClockMapping.Offset(new(probe.Value, new(resumed + 50_000_000), new(resumed + 50_000_000), new(resumed), Initial), Peer.Uncertainty),
            new ClockInterval(newLower, newUpper));
        Assert.True(new ClockInterval(oldLower, oldUpper).Overlaps(new(newLower, newUpper)));
    }

    [Fact]
    public void AStalledRequestersLapseThatDisagreesWithTheOldMappingIsACountedRecalibration()
    {
        const long resumed = 770_000_000;
        var map = Stalled(resumed);
        var epoch = map.DisplayEpoch;
        var probe = map.BeginProbe(new(resumed))!.Value;
        // The offset moved by 10 ms during the stall, far beyond the old mapping's drift-widened interval.
        var (output, observation) = Capture(() => map.Receive(new(probe, new(resumed + 60_000_000), new(resumed + 60_000_000), new(resumed), Initial)));
        Assert.Equal(ClockProbeOutcome.Accepted, observation.Outcome); // it seeds the fresh calibration
        Assert.Equal((1UL, ClockRecalibrationCause.MappingLifetime, epoch + 1, 0UL),
            (map.Recalibrations, map.LastRecalibration!.Value.Cause, map.DisplayEpoch, map.LateRenewals));
        Assert.Equal((ClockMappingState.Expired, 1), (map.State, map.ValidProbeCount));
        Assert.Contains("CCGPU_CLOCK_RECALIBRATED ", output);
        Assert.DoesNotContain("CCGPU_CLOCK_RENEWED_LATE", output);
    }

    // ---- R3-F1 (Murdoch-2 round 3; D23 amended by the coordinator): only an exchange narrow enough to qualify the
    // mapping (the refresh-horizon width bound that ends ordinary recovery) concludes a renewal; a wider one is
    // inconclusive, within ordinary recovery's budget (RecoveryProbeLimit sends within RecoveryLifetime of the lapse). ----

    /// <summary>One renewing exchange answered at <paramref name="sent"/> + <paramref name="roundTrip"/>, its master read
    /// at the round trip's midpoint under <paramref name="offset"/>.</summary>
    private static (string Output, ClockObservation Observation) Renew(WorkshopClockMapping map, long sent, long roundTrip, long offset)
    {
        var probe = map.BeginProbe(new(sent)) ?? throw new InvalidOperationException($"No renewing probe could be sent at {sent}.");
        var master = sent + roundTrip / 2 + offset;
        return Capture(() => map.Receive(new(probe, new(master), new(master), new(sent + roundTrip), Initial)));
    }

    // Murdoch's probe (review-r3/probes/MurdochR3RenewalTests.cs.txt), inverted: a 200 ms renewing exchange while the
    // true offset moved 60 ms overlaps the old mapping only because it is wide. It must not renew; the next narrow
    // exchange shows the move and recalibrates.
    [Fact]
    public void AWideRenewingExchangeCannotConfirmAMovedClock()
    {
        const long resumed = 770_000_000, moved = 110_000_000;
        var map = Stalled(resumed);
        var epoch = map.DisplayEpoch;
        var (output, wide) = Renew(map, resumed, 200_000_000, moved);
        Assert.Equal(ClockProbeOutcome.Inconclusive, wide.Outcome);
        Assert.Equal((ClockMappingState.Renewing, 0UL, 0UL, epoch), (map.State, map.LateRenewals, map.Recalibrations, map.DisplayEpoch));
        Assert.False(map.TryMasterNow(new(resumed + 200_000_000), out _)); // consumers keep holding
        Assert.Equal("", output);
        var (recalibration, narrow) = Renew(map, resumed + 200_000_000, 1_000_000, moved);
        Assert.Equal(ClockProbeOutcome.Accepted, narrow.Outcome); // it seeds the fresh calibration
        Assert.Equal((1UL, ClockRecalibrationCause.MappingLifetime, 0UL, epoch + 1),
            (map.Recalibrations, map.LastRecalibration!.Value.Cause, map.LateRenewals, map.DisplayEpoch));
        Assert.Contains("\"stateBefore\":6", recalibration);
        Assert.DoesNotContain("CCGPU_CLOCK_RENEWED_LATE", recalibration);
    }

    // A narrow consistent exchange after wide ones renews exactly once, when the renewal concludes, and resumes Ready
    // (never Recovering: the concluding exchange alone covers the refresh horizon). Each next renewing probe is taken at
    // once after the previous answer (R3-F3).
    [Theory]
    [InlineData(1)]
    [InlineData(WorkshopClockMapping.RecoveryProbeLimit - 1)]
    public void ANarrowConsistentExchangeAfterWideOnesRenewsExactlyOnce(int wide)
    {
        const long resumed = 770_000_000, roundTrip = 2_000_000;
        var map = Stalled(resumed);
        var epoch = map.DisplayEpoch;
        var now = resumed;
        var output = "";
        for (var i = 0; i < wide; i++, now += roundTrip)
        {
            var (text, observation) = Renew(map, now, roundTrip, 50_000_000);
            Assert.Equal(ClockProbeOutcome.Inconclusive, observation.Outcome);
            output += text;
        }
        Assert.Equal((ClockMappingState.Renewing, 0UL, 0UL), (map.State, map.LateRenewals, map.Recalibrations));
        var (renewal, narrow) = Renew(map, now, 0, 50_000_000);
        Assert.Equal(ClockProbeOutcome.Accepted, narrow.Outcome);
        Assert.Equal((ClockMappingState.Ready, 1UL, 0UL, epoch), (map.State, map.LateRenewals, map.Recalibrations, map.DisplayEpoch));
        Assert.True(map.TryMasterNow(new(now), out _));
        Assert.Equal("", output);
        var line = Assert.Single(renewal.Split('\n', StringSplitOptions.RemoveEmptyEntries));
        Assert.StartsWith("CCGPU_CLOCK_RENEWED_LATE ", line);
        using var json = System.Text.Json.JsonDocument.Parse(line["CCGPU_CLOCK_RENEWED_LATE ".Length..]);
        Assert.Equal(wide + 1, json.RootElement.GetProperty("exchanges").GetInt32());
    }

    // The budget boundary: the RecoveryProbeLimit-th renewing exchange inconclusive too exhausts it (ProbeLimit).
    [Fact]
    public void ARenewalWhoseEveryExchangeIsTooWideExhaustsItsBudgetAsARecalibration()
    {
        const long resumed = 770_000_000, roundTrip = 2_000_000;
        var map = Stalled(resumed);
        var now = resumed;
        for (var i = 0; i < WorkshopClockMapping.RecoveryProbeLimit - 1; i++, now += roundTrip)
            Assert.Equal(ClockProbeOutcome.Inconclusive, Renew(map, now, roundTrip, 50_000_000).Observation.Outcome);
        Assert.Equal((ClockMappingState.Renewing, 0UL), (map.State, map.Recalibrations));
        var (output, last) = Renew(map, now, roundTrip, 50_000_000);
        Assert.Equal(ClockProbeOutcome.Inconclusive, last.Outcome);
        Assert.Equal((ClockMappingState.Expired, 1UL, ClockRecalibrationCause.ProbeLimit, 0UL),
            (map.State, map.Recalibrations, map.LastRecalibration!.Value.Cause, map.LateRenewals));
        Assert.Contains("\"stateBefore\":6", output);
    }

    // The time bound: a renewal not concluded within RecoveryLifetime of the lapse is a recalibration (Elapsed); at the
    // exact endpoint it still continues.
    [Theory]
    [InlineData(0L, false)]
    [InlineData(1L, true)]
    public void ARenewalNotConcludedWithinTheRecoveryLifetimeIsARecalibration(long beyond, bool elapsed)
    {
        const long resumed = 770_000_000;
        var map = Stalled(resumed);
        Assert.Equal(ClockProbeOutcome.Inconclusive, Renew(map, resumed, 2_000_000, 50_000_000).Observation.Outcome);
        Capture(() => { map.Observe(new(resumed + WorkshopClockMapping.RecoveryLifetime + beyond)); return 0; });
        Assert.Equal(elapsed ? (ClockMappingState.Expired, 1UL) : (ClockMappingState.Renewing, 0UL), (map.State, map.Recalibrations));
        if (elapsed) Assert.Equal(ClockRecalibrationCause.Elapsed, map.LastRecalibration!.Value.Cause);
    }

    // The width boundary: a concluding exchange qualifies the mapping exactly when its interval, drift-widened over the
    // refresh horizon, plus twice the maximum sample uncertainty fits the mapped width (the bound that ends recovery).
    [Theory]
    [InlineData(0L, true)]
    [InlineData(1L, false)]
    public void TheRenewalWidthBoundIsTheRefreshHorizonQualification(long wider, bool renews)
    {
        const long resumed = 770_000_000, u = 100_000;
        // A round trip r admits [X - r/2 - 2u, X + r/2 + 2u] (r even); the bound is r + 4u + 2 * 125 us + 2u <= 2 ms.
        var roundTrip = WorkshopClockMapping.MaximumMappedWidth - 2 * (long)TimestampUncertainty.Maximum
            - 2 * 125_000 - 4 * u + 2 * wider;
        var map = Stalled(resumed);
        var (_, observation) = Renew(map, resumed, roundTrip, 50_000_000);
        Assert.Equal(1_550_000 + 2 * wider, observation.Offset.Width);
        Assert.Equal(renews ? ClockProbeOutcome.Accepted : ClockProbeOutcome.Inconclusive, observation.Outcome);
        Assert.Equal((renews ? 1UL : 0UL, 0UL), (map.LateRenewals, map.Recalibrations));
    }

    // R3-F2: a lapse with a probe outstanding stays a recalibration where that clause decides it, in Recovering (10 ms
    // spacing): the requester ran (it sent the probe), so its lapse is not a stall.
    [Fact]
    public void ALapseInRecoveryWithAProbeOutstandingIsACountedRecalibration()
    {
        var map = Ready(Initial); // the newest slot answered at 70 ms
        map.Observe(new(321_000_000)); // the refresh horizon is lost: Recovering
        Assert.Equal(ClockMappingState.Recovering, map.State);
        Assert.NotNull(map.BeginProbe(new(321_000_000))); // sent and never answered
        var (output, _) = Capture(() => { map.Observe(new(70_000_000 + WorkshopClockMapping.MappingLifetime + 1)); return 0; });
        Assert.Equal((ClockMappingState.Expired, 1UL, ClockRecalibrationCause.MappingLifetime, 0UL),
            (map.State, map.Recalibrations, map.LastRecalibration!.Value.Cause, map.LateRenewals));
        Assert.Contains("\"stateBefore\":5", output);
    }

    // R3-F3: the renewing probe is taken the moment the lapse is detected, even 249 ms after the last send.
    [Fact]
    public void TheRenewingProbeIsSentAtOnceOnLapseDetection()
    {
        var map = Ready(Initial);
        map.Observe(new(321_000_000));
        var probe = map.BeginProbe(new(321_000_000))!.Value;
        // Answered unusably (reversed master stamps): dropped, no longer outstanding, and the requester then stalls.
        Assert.Equal(ClockProbeOutcome.Reversed, map.Receive(new(probe, new(372_000_000), new(371_999_999), new(322_000_000), Initial)).Outcome);
        const long lapse = 70_000_000 + WorkshopClockMapping.MappingLifetime + 1;
        map.Observe(new(lapse));
        Assert.Equal((ClockMappingState.Renewing, 0UL), (map.State, map.Recalibrations));
        Assert.NotNull(map.BeginProbe(new(lapse)));
    }

    // ---- R4-F1 (Murdoch-2 round 4; D23 amended again by the coordinator): the old mapping's side of the overlap is
    // bounded too. Its narrowest retained interval, drift-widened to the concluding exchange, must itself fit the mapped
    // width (plus twice the maximum sample uncertainty), or the lapse is a recalibration: a long stall widens the old
    // interval until it would swallow a real move. ----

    // Murdoch's probe (review-r4/probes/MurdochR4RenewalTests.cs.txt R4_3), inverted, plus small and zero moves after a
    // stall past the bound: never a renewal. Its cause is the stall itself (StallPastMappedWidth), not MappingLifetime,
    // which a renewal keeps for a genuine disagreement (TheRenewalBoundaryIsExactlyTheDriftWidenedOverlap).
    [Theory]
    [InlineData(10_070_000_000L, 4_000_000L)]    // 10 s stall, a 4 ms move
    [InlineData(120_070_000_000L, 50_000_000L)]  // 2 min stall, a 50 ms move
    [InlineData(1_570_000_000L, 1_000_000L)]     // 1.5 s stall, a 1 ms move
    [InlineData(1_570_000_000L, 0L)]             // 1.5 s stall, no move: the old mapping can no longer vouch for it
    public void AfterAStallPastTheBoundAConcludingExchangeRecalibratesNeverRenews(long resumed, long move)
    {
        var map = Stalled(resumed);
        var epoch = map.DisplayEpoch;
        var (output, observation) = Renew(map, resumed, 0, 50_000_000 + move);
        Assert.Equal(ClockProbeOutcome.Accepted, observation.Outcome); // it seeds the fresh calibration
        Assert.Equal((1UL, ClockRecalibrationCause.StallPastMappedWidth, 0UL, epoch + 1),
            (map.Recalibrations, map.LastRecalibration!.Value.Cause, map.LateRenewals, map.DisplayEpoch));
        Assert.Contains("\"stateBefore\":6", output);
        Assert.Contains("\"causeName\":\"StallPastMappedWidth\"", output);
        Assert.DoesNotContain("CCGPU_CLOCK_RENEWED_LATE", output);
    }

    // The boundary: Ready's newest slot is 400,000 ns wide (answered at 70 ms); drift-widened by 500 ppm of its age
    // plus 200,000 ns it fits 2,000,000 ns exactly at an age of 1.4 s; one nanosecond later it does not.
    [Theory]
    [InlineData(0L, true)]
    [InlineData(1L, false)]
    public void TheOldMappingWidthBoundIsExactlyTheMappedWidth(long beyond, bool renews)
    {
        var resumed = 70_000_000 + 1_400_000_000 + beyond;
        var map = Stalled(resumed);
        Renew(map, resumed, 0, 50_000_000);
        Assert.Equal((renews ? 1UL : 0UL, renews ? 0UL : 1UL), (map.LateRenewals, map.Recalibrations));
        if (!renews) Assert.Equal(ClockRecalibrationCause.StallPastMappedWidth, map.LastRecalibration!.Value.Cause);
    }

    // R5-02: the ceilinged drift widens both sides together, so an even-width slot steps 2 ns at a time and cannot tell
    // the bound from one 1 ns looser. An odd-width slot can: the newest slot's round trip is 1 ns (400,001 ns wide), so
    // at an age of 1.4 s it widens to exactly 2,000,001 ns with the sample allowance, one over the bound, and the lapse
    // recalibrates; 2,000 ns younger (drift 699,999 ns a side) it is 1,999,999 ns and renews.
    [Theory]
    [InlineData(1_400_000_000L, false)]
    [InlineData(1_399_998_000L, true)]
    public void AnOddWidthOldMappingIsBoundToTheNanosecond(long age, bool renews)
    {
        var map = new WorkshopClockMapping(Peer);
        for (var i = 0; i < WorkshopClockMapping.ProbeCapacity - 1; i++) Accept(map, i * 10_000_000, Initial);
        var newest = map.BeginProbe(new(70_000_000))!.Value;
        var answered = map.Receive(new(newest, new(120_000_000), new(120_000_000), new(70_000_001), Initial));
        Assert.Equal((ClockProbeOutcome.Accepted, 400_001L), (answered.Outcome, (long)answered.Offset.Width));
        Assert.Equal(ClockMappingState.Ready, map.State);
        var resumed = 70_000_001 + age;
        Capture(() => { map.Observe(new(resumed)); return 0; });
        Assert.Equal(ClockMappingState.Renewing, map.State);
        Renew(map, resumed, 0, 50_000_000);
        Assert.Equal((renews ? 1UL : 0UL, renews ? 0UL : 1UL), (map.LateRenewals, map.Recalibrations));
        if (!renews) Assert.Equal(ClockRecalibrationCause.StallPastMappedWidth, map.LastRecalibration!.Value.Cause);
    }

    // R4-F4: a renewal that begins in Recovering starts its own budget; it does not inherit recovery's sends.
    [Fact]
    public void ARenewalAfterRecoveryStartsAFreshBudget()
    {
        var map = Ready(Initial);
        map.Observe(new(321_000_000));
        var probe = map.BeginProbe(new(321_000_000))!.Value; // one recovery send
        map.Receive(new(probe, new(372_000_000), new(371_999_999), new(322_000_000), Initial)); // dropped (reversed)
        long now = 70_000_000 + WorkshopClockMapping.MappingLifetime + 1;
        map.Observe(new(now));
        Assert.Equal(ClockMappingState.Renewing, map.State);
        for (var i = 0; i < WorkshopClockMapping.RecoveryProbeLimit - 1; i++, now += 2_000_000)
            Assert.Equal(ClockProbeOutcome.Inconclusive, Renew(map, now, 2_000_000, 50_000_000).Observation.Outcome);
        var (renewal, last) = Renew(map, now, 0, 50_000_000);
        Assert.Equal(ClockProbeOutcome.Accepted, last.Outcome);
        Assert.Equal((ClockMappingState.Ready, 1UL, 0UL), (map.State, map.LateRenewals, map.Recalibrations));
        var line = Assert.Single(renewal.Split('\n', StringSplitOptions.RemoveEmptyEntries));
        using var json = System.Text.Json.JsonDocument.Parse(line["CCGPU_CLOCK_RENEWED_LATE ".Length..]);
        Assert.Equal(WorkshopClockMapping.RecoveryProbeLimit, json.RootElement.GetProperty("exchanges").GetInt32());
    }

    [Fact]
    public void AFreshExchangeThatIsNotAcceptedOrNeverAnsweredEndsTheRenewalAsARecalibration()
    {
        const long resumed = 770_000_000;
        var unanswered = Stalled(resumed);
        Assert.NotNull(unanswered.BeginProbe(new(resumed)));
        // Unanswered: the renewal's time bound (ordinary recovery's lifetime, from the lapse) ends it.
        Capture(() => { unanswered.Observe(new(resumed + WorkshopClockMapping.RecoveryLifetime + 1)); return 0; });
        Assert.Equal((1UL, ClockRecalibrationCause.Elapsed, 0UL), (unanswered.Recalibrations, unanswered.LastRecalibration!.Value.Cause, unanswered.LateRenewals));
        var reversed = Stalled(resumed);
        var probe = reversed.BeginProbe(new(resumed))!.Value;
        Capture(() => reversed.Receive(new(probe, new(resumed + 50_000_000), new(resumed + 49_999_999), new(resumed), Initial)));
        Assert.Equal((1UL, ClockRecalibrationCause.MappingLifetime, 0UL), (reversed.Recalibrations, reversed.LastRecalibration!.Value.Cause, reversed.LateRenewals));
    }

    // The consistency boundary: the fresh interval touching the old one, drift-widened for its age, renews; one
    // nanosecond further disagrees.
    [Theory]
    [InlineData(0L, true)]
    [InlineData(1L, false)]
    public void TheRenewalBoundaryIsExactlyTheDriftWidenedOverlap(long beyond, bool renewed)
    {
        const long resumed = 770_000_000, u = 100_000;
        var map = Stalled(resumed);
        // The newest old slot (answered at 70 ms) is the narrowest after widening: [50 ms - 2u, 50 ms + 2u] at age 700 ms.
        var old = WorkshopClockMapping.Expand(new(50_000_000 - 2 * u, 50_000_000 + 2 * u), resumed - 70_000_000);
        // A zero-round-trip exchange reading master = resumed + X admits [X - 2u, X + 2u]; its lower edge meets old.Upper.
        var offset = checked((long)(old.Upper + 2 * u)) + beyond;
        var probe = map.BeginProbe(new(resumed))!.Value;
        Capture(() => map.Receive(new(probe, new(resumed + offset), new(resumed + offset), new(resumed), Initial)));
        Assert.Equal((renewed ? 1UL : 0UL, renewed ? 0UL : 1UL), (map.LateRenewals, map.Recalibrations));
        // A 700 ms stall is within the bound: this is a genuine disagreement, not a stall past the mapped width.
        if (!renewed) Assert.Equal(ClockRecalibrationCause.MappingLifetime, map.LastRecalibration!.Value.Cause);
    }

    [Fact]
    public void ADisjointIntervalIsACountedRecalibration()
    {
        var map = Ready(Initial);
        var (output, observation) = Capture(() => Accept(map, 320_000_000, Initial, 60_000_000));
        Assert.Equal(ClockProbeOutcome.Disjoint, observation.Outcome);
        Assert.Equal(ClockMappingState.Suspect, map.State);
        Assert.Equal((1UL, ClockRecalibrationCause.Disjoint), (map.Recalibrations, map.LastRecalibration!.Value.Cause));
        Assert.Contains("\"causeName\":\"Disjoint\"", output);
    }

    // ---- Story 9.4b final-round review (blind hunter 8, edge-case E3, verification gaps V2/V3) ----

    private static ClockObservation Reply(WorkshopClockMapping map, long sent, long received, long master, ClockDerivationEpoch derivation)
    {
        var probe = map.BeginProbe(new(sent)) ?? throw new InvalidOperationException($"No probe could be sent at {sent}.");
        return map.Receive(new(probe, new(master), new(master), new(received), derivation));
    }

    // Blind hunter 8: a reply that is not the pending probe's answer is classified before anything else, so neither a newer
    // nor a reversed derivation epoch on it can recalibrate the mapping, move its installed epoch or fault the session.
    [Fact]
    public void AStaleReplyCannotMoveTheDerivationEpochRecalibrateOrFault()
    {
        var map = Ready(Initial);
        var probe = map.BeginProbe(new(320_000_000))!.Value;
        var stale = probe with { Sequence = new(probe.Sequence.Value - 1) };
        var (output, newer) = Capture(() => map.Receive(new(stale, new(370_000_000), new(370_000_000), new(320_000_000), new(1))));
        Assert.Equal(ClockProbeOutcome.WrongIdentity, newer.Outcome);
        Assert.Equal((0UL, ClockMappingState.Ready), (map.Recalibrations, map.State));
        Assert.Equal("", output);
        // The installed epoch is still 0: the owned answer under epoch 0 is accepted, not a reversal.
        Assert.Equal(ClockProbeOutcome.Accepted,
            map.Receive(new(probe, new(370_000_000), new(370_000_000), new(320_000_000), Initial)).Outcome);
    }

    // Edge-case E3: an epoch change after a recalibration already discarded every slot discards nothing more, so it is
    // not a second recalibration.
    [Fact]
    public void AnEpochChangeWithNoRetainedSlotsIsNotASecondRecalibration()
    {
        var map = Ready(Initial);
        Assert.NotNull(map.BeginProbe(new(320_000_000))); // a probe outstanding: the lapse recalibrates
        Capture(() => { map.Observe(new(70_000_000 + WorkshopClockMapping.MappingLifetime + 1)); return 0; });
        Assert.Equal(1UL, map.Recalibrations);
        var (output, first) = Capture(() => Accept(map, 600_000_000, new(1)));
        Assert.Equal(ClockProbeOutcome.Accepted, first.Outcome);
        Assert.Equal((1UL, ""), (map.Recalibrations, output));
    }

    // Verification gap V2: the ProfileRange cause. Each accepted interval overlaps the best current one, but slots retained
    // from before two half-second walks of the offset span more than MaximumOffsetSpan.
    [Fact]
    public void ARetainedSpanBeyondTheOffsetProfileIsACountedProfileRangeRecalibration()
    {
        const long ms = 1_000_000;
        var map = Ready(Initial); // eight narrow slots at +50 ms, receipts 0..70 ms
        Assert.Equal(ClockProbeOutcome.Accepted, Reply(map, 320 * ms, 569 * ms, 370 * ms, Initial).Outcome);    // [-199, +50]
        Assert.Equal(ClockProbeOutcome.Accepted, Reply(map, 580 * ms, 580 * ms, 381 * ms, Initial).Outcome);    // narrow -199
        Assert.Equal(ClockProbeOutcome.Accepted, Reply(map, 830 * ms, 1079 * ms, 631 * ms, Initial).Outcome);   // [-448, -199]
        Assert.Equal(ClockProbeOutcome.Accepted, Reply(map, 1081 * ms, 1081 * ms, 633 * ms, Initial).Outcome);  // narrow -448
        var (output, observation) = Capture(() => Reply(map, 1331 * ms, 1580 * ms, 883 * ms, Initial));         // [-697, -448]
        Assert.Equal(ClockProbeOutcome.ProfileRange, observation.Outcome);
        Assert.Equal((1UL, ClockRecalibrationCause.ProfileRange), (map.Recalibrations, map.LastRecalibration!.Value.Cause));
        Assert.Contains("\"causeName\":\"ProfileRange\"", output);
    }

    // Verification gap V3: a derivation epoch beyond 32 bits is a malformed reply, never truncated.
    [Fact]
    public void AReplyDerivationEpochAboveThirtyTwoBitsIsRejected()
    {
        var map = Ready(Initial);
        var probe = map.BeginProbe(new(320_000_000))!.Value;
        var reply = WorkshopClockWire.EncodeReply(WorkshopClockWire.EncodeProbe(probe, Peer), Peer, new(370_000_000), new(370_000_000), new(uint.MaxValue));
        Assert.Equal(new ClockDerivationEpoch(uint.MaxValue), WorkshopClockWire.DecodeReply(reply, Peer, new(320_000_000)).Derivation); // control
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(reply.AsSpan(100), 1); // the epoch's high word
        Assert.Contains("out of range", Assert.Throws<ArgumentException>(() => WorkshopClockWire.DecodeReply(reply, Peer, new(320_000_000))).Message);
    }

    // Murdoch-2 Low: a reply the wire decoder rejects was silently dropped by the Browser requester (and was fatal in the
    // Animation requester). Both now take the mapping's one reply path (ReceiveEncoded), and verification gap V4: each
    // malformed reply is exactly one counted, typed Malformed rejection that leaves the probe to expire.
    [Theory]
    [InlineData("reversed stamps")]
    [InlineData("truncated")]
    [InlineData("epoch beyond 32 bits")]
    [InlineData("foreign session")]
    public void EachMalformedReplyIsExactlyOneCountedTypedRejectionThatLeavesThePendingProbeToExpire(string corruption)
    {
        var map = Ready(Initial);
        var probe = map.BeginProbe(new(320_000_000))!.Value;
        var reply = WorkshopClockWire.EncodeReply(WorkshopClockWire.EncodeProbe(probe, Peer), Peer, new(370_000_000), new(370_000_000), Initial);
        // Corruptions the master's checks would never send (clock-ring.js encodeReply refuses them).
        var corrupted = corruption switch
        {
            "reversed stamps" => Patch(reply, 88, 369_999_999),
            "truncated" => reply[..(WorkshopClockWire.ReplyBytes - 8)],
            "epoch beyond 32 bits" => Patch(reply, 96, 1L << 32),
            "foreign session" => Patch(reply, 8, 99),
            _ => throw new ArgumentOutOfRangeException(nameof(corruption)),
        };
        var error = Assert.ThrowsAny<ArgumentException>(() => WorkshopClockWire.DecodeReply(corrupted, Peer, new(320_000_000)));
        var (output, observation) = Capture(() => map.ReceiveEncoded(corrupted, new(320_000_000)));
        Assert.Null(observation);
        Assert.Equal((1UL, (ClockProbe?)probe, 0UL, ClockMappingState.Ready), (map.MalformedReplies, map.Pending, map.Recalibrations, map.State));
        var line = Assert.Single(output.Split('\n', StringSplitOptions.RemoveEmptyEntries));
        Assert.StartsWith("CCGPU_CLOCK_REPLY_REJECTED ", line);
        using var json = System.Text.Json.JsonDocument.Parse(line["CCGPU_CLOCK_REPLY_REJECTED ".Length..]);
        Assert.Equal((int)ClockProbeOutcome.Malformed, json.RootElement.GetProperty("outcome").GetInt32());
        Assert.Equal("Malformed", json.RootElement.GetProperty("outcomeName").GetString());
        Assert.Equal("1", json.RootElement.GetProperty("count").GetString());
        Assert.Equal(corrupted.Length, json.RootElement.GetProperty("length").GetInt32());
        Assert.Equal(error.Message, json.RootElement.GetProperty("detail").GetString());
        // Control: the well-formed answer to the same probe takes the same path, is accepted and emits nothing.
        var (accepted, good) = Capture(() => map.ReceiveEncoded(reply, new(320_000_000)));
        Assert.Equal(("", ClockProbeOutcome.Accepted), (accepted, good!.Value.Outcome));
        Assert.Equal(1UL, map.MalformedReplies);
    }

    private static byte[] Patch(byte[] reply, int at, long value)
    {
        var copy = (byte[])reply.Clone();
        System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(copy.AsSpan(at), value);
        return copy;
    }

    // V4, the call sites: the Browser and Animation requesters decode replies only through ReceiveEncoded, so each
    // malformed reply they receive is the one rejection proved above (no second report, no silent drop).
    [Theory]
    [InlineData("engine/gpu/BrowserWorkshopClient.cs")]
    [InlineData("CuriousContraptions.Animation.Worker/Program.cs")]
    public void EveryRequesterReceivesRepliesOnlyThroughTheMappingsOneReplyPath(string requester)
    {
        var source = File.ReadAllText(Path.Combine(twodog.Engine.ResolveProjectDir(), requester));
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(source, @"\.ReceiveEncoded\("));
        Assert.DoesNotContain("DecodeReply(", source);
        Assert.DoesNotContain("RejectMalformedReply(", source);
    }

    // Blind hunter 2: since wire v4 the reply carries its derivation epoch at 96..103, and every diagnostic record keeps the
    // exact reply 0..103 (receipt I64 at 104, outcome U32 at 112, received length U32 at 116, offset I128 at 120/136).
    [Fact]
    public void EveryClockDiagnosticRecordKeepsTheExactReplyIncludingItsDerivationEpoch()
    {
        static long I64(byte[] d, int at) => System.Buffers.Binary.BinaryPrimitives.ReadInt64LittleEndian(d.AsSpan(at));
        static int I32(byte[] d, int at) => System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(d.AsSpan(at));
        static Int128 I128(byte[] d, int at) => System.Buffers.Binary.BinaryPrimitives.ReadInt128LittleEndian(d.AsSpan(at));
        Assert.Equal(152, WorkshopClockWire.DiagnosticBytes);
        var map = Ready(Initial);
        var probe = map.BeginProbe(new(320_000_000))!.Value;
        var reply = WorkshopClockWire.EncodeReply(WorkshopClockWire.EncodeProbe(probe, Peer), Peer, new(370_000_000), new(370_000_001), new(9));

        var rejected = WorkshopClockWire.EncodeRejectedReply(reply, new(320_000_002));
        Assert.Equal(WorkshopClockWire.DiagnosticBytes, rejected.Length);
        Assert.Equal(WorkshopClockWire.DecodeReply(reply, Peer, new(320_000_002)),
            WorkshopClockWire.DecodeReply(rejected.AsSpan(0, WorkshopClockWire.ReplyBytes), Peer, new(320_000_002)));
        Assert.Equal((320_000_002L, (int)ClockProbeOutcome.Malformed, WorkshopClockWire.ReplyBytes), (I64(rejected, 104), I32(rejected, 112), I32(rejected, 116)));
        var longer = new byte[WorkshopClockWire.ReplyBytes + 8]; reply.CopyTo(longer, 0); longer[^1] = 0xFF;
        var truncated = WorkshopClockWire.EncodeRejectedReply(longer, new(320_000_002));
        Assert.Equal(WorkshopClockWire.DecodeReply(reply, Peer, new(320_000_002)),
            WorkshopClockWire.DecodeReply(truncated.AsSpan(0, WorkshopClockWire.ReplyBytes), Peer, new(320_000_002))); // at most one reply retained; the length records truncation
        Assert.Equal(longer.Length, I32(truncated, 116));

        var expired = WorkshopClockWire.EncodeExpiredProbe(probe, Peer, new(320_000_003));
        Assert.Equal(probe, WorkshopClockWire.DecodeProbe(expired.AsSpan(0, WorkshopClockWire.ProbeBytes), Peer));
        Assert.Equal((320_000_003L, (int)ClockProbeOutcome.Expired, 0), (I64(expired, 104), I32(expired, 112), I32(expired, 116)));

        var decoded = WorkshopClockWire.DecodeReply(reply, Peer, new(320_000_004));
        var observation = new ClockObservation(decoded, ClockProbeOutcome.Accepted, new(-49_000_004, -49_000_000));
        var record = WorkshopClockWire.EncodeObservation(observation, Peer);
        Assert.Equal(decoded, WorkshopClockWire.DecodeReply(record.AsSpan(0, WorkshopClockWire.ReplyBytes), Peer, new(320_000_004)));
        Assert.Equal(9L, I64(record, 96)); // the derivation epoch the stamps were taken under
        Assert.Equal((320_000_004L, (int)ClockProbeOutcome.Accepted, WorkshopClockWire.ReplyBytes), (I64(record, 104), I32(record, 112), I32(record, 116)));
        Assert.Equal(((Int128)(-49_000_004), (Int128)(-49_000_000)), (I128(record, 120), I128(record, 136)));
    }
}
