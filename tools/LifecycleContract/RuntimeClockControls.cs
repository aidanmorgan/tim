using CuriousContraptions.Gpu;

namespace CuriousContraptions.LifecycleDesign;

internal enum RuntimeClockCase
{
    ExactNativeConversion, NativeRangeRejection, CapacityReservation, StartupAndIdentity,
    ExactNormativeInterval, DriftRounding, ExpiryAndFreshStartup, DisjointRequiresFreshEight,
    CausalCapture, HalfwidthBoundary, ReplacementRebase, RecoveryKeepsCurrentMappings,
    HorizonBoundary, MaximumSampleReservation, RecoveryRejectsUnrelatedCaptureTriggers,
    RecoveryEighthReplyRecalibration, RecoveryEighthReplySuccess, RecoveryDeadline,
    RecoveryDisjointRequalification, HorizonOverflow
}

internal sealed record RuntimeClockResult(RuntimeClockCase Case, bool Passed, string Detail);

/// <summary>Controls against the production assembly. These do not qualify a real browser clock or renderer.</summary>
internal sealed class RuntimeClockControls(IClockServices clocks)
{
    private static readonly WorkshopClockPeer Peer = new(new(11, 23), new(7), new(0), new(1), new(100_000), WorkshopRuntimeRole.Browser);
    // The installed master derivation epoch; these controls never re-derive the master.
    private static readonly ClockDerivationEpoch Derived = new(0);

    public IReadOnlyList<RuntimeClockResult> Run()
    {
        List<RuntimeClockResult> results = [];
        void Check(RuntimeClockCase id, Action action)
        {
            try { action(); results.Add(new(id, true, "Passed")); }
            catch (Exception error) when (error is InvalidOperationException or ArgumentException) { results.Add(new(id, false, error.ToString())); }
        }
        Check(RuntimeClockCase.ExactNativeConversion, ExactNativeConversion);
        Check(RuntimeClockCase.NativeRangeRejection, NativeRangeRejection);
        Check(RuntimeClockCase.CapacityReservation, CapacityReservation);
        Check(RuntimeClockCase.StartupAndIdentity, StartupAndIdentity);
        Check(RuntimeClockCase.ExactNormativeInterval, ExactNormativeInterval);
        Check(RuntimeClockCase.DriftRounding, DriftRounding);
        Check(RuntimeClockCase.ExpiryAndFreshStartup, ExpiryAndFreshStartup);
        Check(RuntimeClockCase.DisjointRequiresFreshEight, DisjointRequiresFreshEight);
        Check(RuntimeClockCase.CausalCapture, CausalCapture);
        Check(RuntimeClockCase.HalfwidthBoundary, HalfwidthBoundary);
        Check(RuntimeClockCase.ReplacementRebase, ReplacementRebase);
        Check(RuntimeClockCase.RecoveryKeepsCurrentMappings, RecoveryKeepsCurrentMappings);
        Check(RuntimeClockCase.HorizonBoundary, HorizonBoundary);
        Check(RuntimeClockCase.MaximumSampleReservation, MaximumSampleReservation);
        Check(RuntimeClockCase.RecoveryRejectsUnrelatedCaptureTriggers, RecoveryRejectsUnrelatedCaptureTriggers);
        // Story 9.4b: an unqualified eighth reply recalibrates and continues (game-grade envelope); it never faults.
        Check(RuntimeClockCase.RecoveryEighthReplyRecalibration, RecoveryEighthReplyRecalibration);
        Check(RuntimeClockCase.RecoveryEighthReplySuccess, RecoveryEighthReplySuccess);
        Check(RuntimeClockCase.RecoveryDeadline, RecoveryDeadline);
        Check(RuntimeClockCase.RecoveryDisjointRequalification, RecoveryDisjointRequalification);
        Check(RuntimeClockCase.HorizonOverflow, HorizonOverflow);
        return results;
    }

    private IClockMapping Recovering()
    {
        var map = Ready();
        Accept(map, 320_000_000, roundtrip: 1_295_000);
        Require(map.State == ClockMappingState.Recovering && map.IsQualified && map.ValidProbeCount == 8);
        return map;
    }

    private static void Fault(Action action)
    {
        try { action(); } catch (InvalidOperationException) { return; }
        throw new InvalidOperationException("Bounded recovery failed to fault.");
    }

    private IClockMapping Ready()
    {
        var map = clocks.CreateMapping(Peer);
        for (var i = 0; i < 8; i++) Accept(map, i * 10_000_000);
        return map;
    }

    private static void Accept(IClockMapping map, long sent, long offset = 50_000_000, long roundtrip = 0)
    {
        var probe = map.BeginProbe(new(sent)) ?? throw new InvalidOperationException("Probe unexpectedly suppressed.");
        Require(map.Receive(new(probe, new(sent + offset), new(sent + offset), new(sent + roundtrip), Derived)).Outcome == ClockProbeOutcome.Accepted);
    }

    private static void Require(bool value) { if (!value) throw new InvalidOperationException("Source-linked clock oracle mismatch."); }
    private static void Reject(Action action)
    {
        try { action(); } catch (ArgumentException) { return; }
        throw new InvalidOperationException("Invalid native-clock input accepted.");
    }
    private void HorizonOverflow()
    {
        var map = clocks.CreateMapping(Peer);
        for (var i = 0; i < 8; i++) Accept(map, long.MaxValue - 100_000_000 + i * 10_000_000, 0);
        map.Observe(new(long.MaxValue));
        Require(map.State == ClockMappingState.Ready);
    }

    private void RecoveryDisjointRequalification()
    {
        var map = Recovering(); var epoch = map.DisplayEpoch;
        var probe = map.BeginProbe(new(330_000_000))!.Value;
        Require(map.Receive(new(probe, new(390_000_000), new(390_000_000), new(330_000_000), Derived))
            .Outcome == ClockProbeOutcome.Disjoint);
        Require(map.State == ClockMappingState.Suspect && !map.IsQualified && map.DisplayEpoch == epoch + 1);
        Require(map.Recalibrations == 1 && map.LastRecalibration?.Cause == ClockRecalibrationCause.Disjoint);
        for (var i = 0; i < 7; i++) Accept(map, 340_000_000 + i * 10_000_000, 60_000_000);
        Require(!map.IsQualified);
        Accept(map, 410_000_000, 60_000_000);
        Require(map.State == ClockMappingState.Ready);
    }

    private void RecoveryDeadline()
    {
        const long deadline = 571_295_000;
        var exact = Recovering();
        exact.Observe(new(deadline));
        Require(exact.State == ClockMappingState.Recovering);
        Accept(exact, deadline);
        Require(exact.State == ClockMappingState.Ready);
        var late = Recovering();
        late.Observe(new(deadline + 1));
        Require(late.State == ClockMappingState.Expired && late.Recalibrations == 1);
    }

    private void RecoveryEighthReplySuccess()
    {
        var map = Recovering();
        for (var i = 0; i < 7; i++) Accept(map, 330_000_000 + i * 10_000_000, roundtrip: 1_295_000);
        Accept(map, 400_000_000);
        Require(map.State == ClockMappingState.Ready && map.Pending is null);
        Require(map.BeginProbe(new(649_999_999)) is null);
        Require(map.BeginProbe(new(650_000_000)) is not null);
    }

    private void RecoveryEighthReplyRecalibration()
    {
        var map = Recovering();
        for (var i = 0; i < 7; i++) Accept(map, 330_000_000 + i * 10_000_000, roundtrip: 1_295_000);
        var eighth = map.BeginProbe(new(400_000_000))!.Value;
        var reply = new ClockReply(eighth, new(450_000_000), new(450_000_000), new(401_295_000), Derived);
        Require(map.Receive(reply with { Probe = eighth with { Sequence = new(eighth.Sequence.Value + 1) } })
            .Outcome == ClockProbeOutcome.WrongIdentity);
        Require(map.Pending == eighth && map.State == ClockMappingState.Recovering);
        Require(map.BeginProbe(new(410_000_000)) is null);
        var epoch = map.DisplayEpoch;
        map.Receive(reply with { RequesterReceived = new(410_000_000) });
        Require(map.State == ClockMappingState.Expired && map.Pending is null && !map.IsQualified);
        Require(map.Recalibrations == 1 && map.DisplayEpoch == epoch + 1 && map.ValidProbeCount == 0);
        Require(map.BeginProbe(new(411_000_000)) is not null);
        map.Retire();
        Require(map.State == ClockMappingState.Retired);
    }

    private void RecoveryRejectsUnrelatedCaptureTriggers()
    {
        var map = Ready();
        var capture = new WorkshopClockStamp(WorkshopClockDomain.SimulationMonotonic, Peer.Generation,
            new(40_000_000), new(100_000));
        Require(!map.TryMap(capture, new(80_000_000), out _));
        Require(!map.TryMap(capture with { Time = new(200_000_000) }, new(80_000_000), out _));
        Reject(() => map.TryMap(capture with { Generation = new(8) }, new(80_000_000), out _));
        Require(map.State == ClockMappingState.Ready && map.Pending is null);
    }

    private void MaximumSampleReservation()
    {
        var map = clocks.CreateMapping(Peer with { Uncertainty = new(0) });
        for (var i = 0; i < 8; i++) Accept(map, i * 10_000_000, roundtrip: 1_550_000);
        Require(map.State == ClockMappingState.Ready);
        map.Observe(new(71_550_001));
        Require(map.State == ClockMappingState.Recovering);
    }

    private void HorizonBoundary()
    {
        var map = clocks.CreateMapping(Peer);
        for (var i = 0; i < 8; i++) Accept(map, i * 10_000_000, roundtrip: 1_150_000);
        Require(map.State == ClockMappingState.Ready);
        map.Observe(new(71_150_001));
        Require(map.State == ClockMappingState.Recovering && map.ValidProbeCount == 8);
    }

    private void RecoveryKeepsCurrentMappings()
    {
        var map = Recovering();
        var epoch = map.DisplayEpoch;
        var capture = new WorkshopClockStamp(WorkshopClockDomain.SimulationMonotonic, Peer.Generation,
            new(375_000_000), new(100_000));
        Require(map.TryMap(capture, new(325_000_000), out var mapped));
        var retained = mapped;
        Require(map.IsQualified && map.State == ClockMappingState.Recovering);
        Require(map.BeginProbe(new(329_999_999)) is null);
        Accept(map, 330_000_000);
        Require(map.State == ClockMappingState.Ready && map.DisplayEpoch == epoch && mapped == retained);
        Require(map.BeginProbe(new(579_999_999)) is null);
        Require(map.BeginProbe(new(580_000_000)) is not null);
    }

    private void ReplacementRebase()
    {
        var map = Ready();
        for (var i = 0; i < 40; i++) Accept(map, 320_000_000L + i * 250_000_000L, 50_000_000L + i * 100_000L);
        Require(map.State == ClockMappingState.Ready && map.ValidProbeCount == 8);
    }

    private void HalfwidthBoundary()
    {
        var map = clocks.CreateMapping(Peer);
        for (var i = 0; i < 8; i++) Accept(map, i * 10_000_000, 50_000_000, 1_400_000);
        var capture = new WorkshopClockStamp(WorkshopClockDomain.SimulationMonotonic, Peer.Generation,
            new(130_000_000), new(100_000));
        Require(!map.TryMap(capture, new(80_000_000), out _));
        // Exact accepted boundary is tested independently by interval arithmetic: 1.4ms RTT + 4q + 2qs = 2ms.
        var reply = new ClockReply(new(Peer.Session, Peer.Generation, Peer.RequesterGeneration, Peer.RequesterRole, new(1), new(0)), new(50_000_000), new(50_000_000), new(1_400_000), Derived);
        Require(clocks.Offset(reply, Peer.Uncertainty).Width + 2 * Peer.Uncertainty.Value == clocks.MaximumMappedWidth);
    }

    private void CausalCapture()
    {
        var map = Ready();
        var old = new WorkshopClockStamp(WorkshopClockDomain.SimulationMonotonic, Peer.Generation, new(40_000_000), new(100_000));
        Require(!map.TryMap(old, new(80_000_000), out _));
        var capture = old with { Time = new(130_000_000) };
        Require(map.TryMap(capture, new(80_000_000), out var mapped));
        Require(mapped.BrowserInterval == new CuriousContraptions.Gpu.ClockInterval(79_695_000, 80_305_000));
        var retained = mapped;
        Accept(map, 320_000_000);
        Require(retained == mapped);
    }

    private void DisjointRequiresFreshEight()
    {
        var map = Ready();
        var probe = map.BeginProbe(new(320_000_000))!.Value;
        var outcome = map.Receive(new(probe, new(380_000_000), new(380_000_000), new(320_000_000), Derived));
        Require(outcome.Outcome == ClockProbeOutcome.Disjoint && map.State == ClockMappingState.Suspect && map.ValidProbeCount == 0);
        Require(map.Recalibrations == 1 && map.LastRecalibration?.Cause == ClockRecalibrationCause.Disjoint);
        for (var i = 0; i < 8; i++) Accept(map, 330_000_000 + i * 10_000_000, 60_000_000);
        Require(map.State == ClockMappingState.Ready);
    }

    private void ExpiryAndFreshStartup()
    {
        var map = Ready();
        var epoch = map.DisplayEpoch;
        map.Observe(new(570_000_000));
        Require(map.State == ClockMappingState.Recovering && map.IsQualified);
        map.Observe(new(570_000_001));
        Require(map.State == ClockMappingState.Expired && map.DisplayEpoch == epoch + 1 && map.ValidProbeCount == 0);
        // Story 9.4b: a lapsed mapping lifetime is a counted, typed recalibration, never a silent expiry.
        Require(map.Recalibrations == 1 && map.LastRecalibration?.Cause == ClockRecalibrationCause.MappingLifetime);
        for (var i = 0; i < 7; i++) Accept(map, 580_000_000 + i * 10_000_000);
        Require(map.State != ClockMappingState.Ready);
        Accept(map, 650_000_000);
        Require(map.State == ClockMappingState.Ready);
    }

    private void DriftRounding()
    {
        long[] ages = [0, 1, 1999, 2000, 2001];
        long[] expected = [0, 1, 1, 1, 2];
        for (var i = 0; i < ages.Length; i++)
            Require(clocks.Expand(new(0, 0), ages[i]) == new CuriousContraptions.Gpu.ClockInterval(-expected[i], expected[i]));
        Reject(() => clocks.Expand(new(0, 0), -1));
    }

    private void ExactNormativeInterval()
    {
        var reply = new ClockReply(new(Peer.Session, Peer.Generation, Peer.RequesterGeneration, Peer.RequesterRole, new(1), new(100_000_000)),
            new(150_200_000), new(150_300_000), new(100_700_000), Derived);
        var interval = clocks.Offset(reply, new(10_000));
        Require(interval == new CuriousContraptions.Gpu.ClockInterval(49_580_000, 50_220_000));
        Require(clocks.Expand(interval, 250_000_000) ==
            new CuriousContraptions.Gpu.ClockInterval(49_455_000, 50_345_000));
    }

    private void StartupAndIdentity()
    {
        var map = clocks.CreateMapping(Peer);
        var probe = map.BeginProbe(new(0))!.Value;
        Require(map.BeginProbe(new(1)) is null);
        var reply = new ClockReply(probe, new(50_000_000), new(50_000_000), new(1), Derived);
        Fault(() => map.Receive(reply with { RequesterReceived = new(0) }));
        Require(map.Pending == probe && map.ValidProbeCount == 0);
        Require(map.Receive(reply with { Probe = probe with { Sequence = new(2) } }).Outcome == ClockProbeOutcome.WrongIdentity);
        Require(map.Pending == probe && map.ValidProbeCount == 0);
        Require(map.Receive(reply).Outcome == ClockProbeOutcome.Accepted);
        Require(map.Receive(reply).Outcome == ClockProbeOutcome.WrongIdentity);
        Require(map.BeginProbe(new(9_999_999)) is null);
        for (var i = 1; i < 8; i++) Accept(map, i * 10_000_000);
        Require(map.State == ClockMappingState.Ready && map.ValidProbeCount == 8);
    }

    private void CapacityReservation()
    {
        RequireEqual(clocks.BookkeepingBytes, 280);
        var map = clocks.CreateMapping(Peer);
        for (var i = 0; i < clocks.ProbeCapacity - 1; i++)
        {
            Accept(map, i * clocks.StartupSpacing);
            Require(!map.IsQualified && map.ValidProbeCount == i + 1);
        }
        Accept(map, (clocks.ProbeCapacity - 1) * clocks.StartupSpacing);
        Require(map.IsQualified && map.ValidProbeCount == clocks.ProbeCapacity);
        // Refresh replaces an old sample without growing the public reservation.
        Accept(map, 320_000_000);
        Require(map.ValidProbeCount == clocks.ProbeCapacity && map.IsQualified);
        var pending = map.BeginProbe(new(570_000_000))!.Value;
        Require(map.BeginProbe(new(570_000_001)) is null && map.Pending == pending);
        var reply = new ClockReply(pending with { Sequence = new(pending.Sequence.Value + 1) },
            new(620_000_000), new(620_000_000), new(570_000_002), Derived);
        Require(map.Receive(reply).Outcome == ClockProbeOutcome.WrongIdentity && map.Pending == pending);

    }

    private void NativeRangeRejection()
    {
        Reject(() => clocks.FromMilliseconds(9_223_372_036_854.777));
        Reject(() => clocks.FromMilliseconds(double.NaN));
        Reject(() => clocks.FromMilliseconds(double.PositiveInfinity));
        Reject(() => clocks.FromMilliseconds(-1));
    }

    private void ExactNativeConversion()
    {
        Require(clocks.FromMilliseconds(9_223_372_036_854.775).Value == 9_223_372_036_854_775_391);
        Require(clocks.FromMilliseconds(double.Epsilon).Value == 0);
        Require(clocks.FromMilliseconds(0.001953125).Value == 1953);
        Require(clocks.FromMilliseconds(0.00390625).Value == 3906);
    }

    private static void RequireEqual<T>(T actual, T expected) where T : IEquatable<T> => Require(actual.Equals(expected));

}
