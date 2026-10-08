using CuriousContraptions.Gpu;

namespace CuriousContraptions.LifecycleDesign;

internal enum RuntimeClockCase
{
    ExactNativeConversion, NativeRangeRejection, PackedReservation, StartupAndIdentity,
    ExactNormativeInterval, DriftRounding, ExpiryAndFreshStartup, DisjointRequiresFreshEight,
    CausalCapture, HalfwidthBoundary, ReplacementRebase, RecoveryKeepsCurrentMappings,
    HorizonBoundary, MaximumSampleReservation, RecoveryRejectsUnrelatedCaptureTriggers,
    RecoveryEighthReplyFailure, RecoveryEighthReplySuccess, RecoveryDeadline,
    RecoveryDisjointRequalification, HorizonOverflow
}

internal sealed record RuntimeClockResult(RuntimeClockCase Case, bool Passed, string Detail);

/// <summary>Source-linked controls. These do not qualify a real browser clock or renderer.</summary>
internal static class RuntimeClockControls
{
    private static readonly WorkshopClockPeer Peer = new(new(11, 23), new(7), new(0), new(1), new(100_000), WorkshopRuntimeRole.Browser);

    public static IReadOnlyList<RuntimeClockResult> Run()
    {
        List<RuntimeClockResult> results = [];
        void Check(RuntimeClockCase id, Action action)
        {
            try { action(); results.Add(new(id, true, "Passed")); }
            catch (Exception error) { results.Add(new(id, false, error.ToString())); }
        }
        Check(RuntimeClockCase.ExactNativeConversion, () =>
        {
            Require(WorkshopNativeClock.FromMilliseconds(9_223_372_036_854.775).Value == 9_223_372_036_854_775_391);
            Require(WorkshopNativeClock.FromMilliseconds(double.Epsilon).Value == 0);
            Require(WorkshopNativeClock.FromMilliseconds(0.001953125).Value == 1953);
            Require(WorkshopNativeClock.FromMilliseconds(0.00390625).Value == 3906);
        });
        Check(RuntimeClockCase.NativeRangeRejection, () =>
        {
            Reject(() => WorkshopNativeClock.FromMilliseconds(9_223_372_036_854.777));
            Reject(() => WorkshopNativeClock.FromMilliseconds(double.NaN));
            Reject(() => WorkshopNativeClock.FromMilliseconds(double.PositiveInfinity));
            Reject(() => WorkshopNativeClock.FromMilliseconds(-1));
        });
        Check(RuntimeClockCase.PackedReservation, () =>
        {
            var type = typeof(WorkshopClockMapping);
            var slot = type.GetNestedType("ClockSlot", System.Reflection.BindingFlags.NonPublic)!;
            Require(System.Runtime.InteropServices.Marshal.SizeOf(slot) == 24);
            Require(WorkshopClockMapping.BookkeepingBytes == 272);
            Require(WorkshopClockMapping.ProbeCapacity * 24 + 80 == 272);
            Require(type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Count(field => field.FieldType.IsArray) == 1);
        });
        Check(RuntimeClockCase.StartupAndIdentity, () =>
        {
            var map = new WorkshopClockMapping(Peer);
            var probe = map.BeginProbe(new(0))!.Value;
            Require(map.BeginProbe(new(1)) is null);
            var reply = new ClockReply(probe, new(50_000_000), new(50_000_000), new(1));
            Fault(() => map.Receive(reply with { RequesterReceived = new(0) }));
            Require(map.Pending == probe && map.ValidProbeCount == 0);
            Require(map.Receive(reply with { Probe = probe with { Sequence = new(2) } }).Outcome == ClockProbeOutcome.WrongIdentity);
            Require(map.Pending == probe && map.ValidProbeCount == 0);
            Require(map.Receive(reply).Outcome == ClockProbeOutcome.Accepted);
            Require(map.Receive(reply).Outcome == ClockProbeOutcome.WrongIdentity);
            Require(map.BeginProbe(new(9_999_999)) is null);
            for (var i = 1; i < 8; i++) Accept(map, i * 10_000_000);
            Require(map.State == ClockMappingState.Ready && map.ValidProbeCount == 8);
        });
        Check(RuntimeClockCase.ExactNormativeInterval, () =>
        {
            var reply = new ClockReply(new(Peer.Session, Peer.Generation, Peer.RequesterGeneration, Peer.RequesterRole, new(1), new(100_000_000)),
                new(150_200_000), new(150_300_000), new(100_700_000));
            var interval = WorkshopClockMapping.Offset(reply, new(10_000));
            Require(interval == new CuriousContraptions.Gpu.ClockInterval(49_580_000, 50_220_000));
            Require(WorkshopClockMapping.Expand(interval, 250_000_000) ==
                new CuriousContraptions.Gpu.ClockInterval(49_455_000, 50_345_000));
        });
        Check(RuntimeClockCase.DriftRounding, () =>
        {
            long[] ages = [0, 1, 1999, 2000, 2001];
            long[] expected = [0, 1, 1, 1, 2];
            for (var i = 0; i < ages.Length; i++)
                Require(WorkshopClockMapping.Expand(new(0, 0), ages[i]) == new CuriousContraptions.Gpu.ClockInterval(-expected[i], expected[i]));
            Reject(() => WorkshopClockMapping.Expand(new(0, 0), -1));
        });
        Check(RuntimeClockCase.ExpiryAndFreshStartup, () =>
        {
            var map = Ready();
            var epoch = map.DisplayEpoch;
            map.Observe(new(570_000_000));
            Require(map.State == ClockMappingState.Recovering && map.IsQualified);
            map.Observe(new(570_000_001));
            Require(map.State == ClockMappingState.Expired && map.DisplayEpoch == epoch + 1 && map.ValidProbeCount == 0);
            for (var i = 0; i < 7; i++) Accept(map, 580_000_000 + i * 10_000_000);
            Require(map.State != ClockMappingState.Ready);
            Accept(map, 650_000_000);
            Require(map.State == ClockMappingState.Ready);
        });
        Check(RuntimeClockCase.DisjointRequiresFreshEight, () =>
        {
            var map = Ready();
            var probe = map.BeginProbe(new(320_000_000))!.Value;
            var outcome = map.Receive(new(probe, new(380_000_000), new(380_000_000), new(320_000_000)));
            Require(outcome.Outcome == ClockProbeOutcome.Disjoint && map.State == ClockMappingState.Suspect && map.ValidProbeCount == 0);
            for (var i = 0; i < 8; i++) Accept(map, 330_000_000 + i * 10_000_000, 60_000_000);
            Require(map.State == ClockMappingState.Ready);
        });
        Check(RuntimeClockCase.CausalCapture, () =>
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
        });
        Check(RuntimeClockCase.HalfwidthBoundary, () =>
        {
            var map = new WorkshopClockMapping(Peer);
            for (var i = 0; i < 8; i++) Accept(map, i * 10_000_000, 50_000_000, 1_400_000);
            var capture = new WorkshopClockStamp(WorkshopClockDomain.SimulationMonotonic, Peer.Generation,
                new(130_000_000), new(100_000));
            Require(!map.TryMap(capture, new(80_000_000), out _));
            // Exact accepted boundary is tested independently by interval arithmetic: 1.4ms RTT + 4q + 2qs = 2ms.
            var reply = new ClockReply(new(Peer.Session, Peer.Generation, Peer.RequesterGeneration, Peer.RequesterRole, new(1), new(0)), new(50_000_000), new(50_000_000), new(1_400_000));
            Require(WorkshopClockMapping.Offset(reply, Peer.Uncertainty).Width + 2 * Peer.Uncertainty.Value == WorkshopClockMapping.MaximumMappedWidth);
        });
        Check(RuntimeClockCase.ReplacementRebase, () =>
        {
            var map = Ready();
            for (var i = 0; i < 40; i++) Accept(map, 320_000_000L + i * 250_000_000L, 50_000_000L + i * 100_000L);
            Require(map.State == ClockMappingState.Ready && map.ValidProbeCount == 8);
        });
        Check(RuntimeClockCase.RecoveryKeepsCurrentMappings, () =>
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
        });
        Check(RuntimeClockCase.HorizonBoundary, () =>
        {
            var map = new WorkshopClockMapping(Peer);
            for (var i = 0; i < 8; i++) Accept(map, i * 10_000_000, roundtrip: 1_150_000);
            Require(map.State == ClockMappingState.Ready);
            map.Observe(new(71_150_001));
            Require(map.State == ClockMappingState.Recovering && map.ValidProbeCount == 8);
        });
        Check(RuntimeClockCase.MaximumSampleReservation, () =>
        {
            var map = new WorkshopClockMapping(Peer with { Uncertainty = new(0) });
            for (var i = 0; i < 8; i++) Accept(map, i * 10_000_000, roundtrip: 1_550_000);
            Require(map.State == ClockMappingState.Ready);
            map.Observe(new(71_550_001));
            Require(map.State == ClockMappingState.Recovering);
        });
        Check(RuntimeClockCase.RecoveryRejectsUnrelatedCaptureTriggers, () =>
        {
            var map = Ready();
            var capture = new WorkshopClockStamp(WorkshopClockDomain.SimulationMonotonic, Peer.Generation,
                new(40_000_000), new(100_000));
            Require(!map.TryMap(capture, new(80_000_000), out _));
            Require(!map.TryMap(capture with { Time = new(200_000_000) }, new(80_000_000), out _));
            Reject(() => map.TryMap(capture with { Generation = new(8) }, new(80_000_000), out _));
            Require(map.State == ClockMappingState.Ready && map.Pending is null);
        });
        Check(RuntimeClockCase.RecoveryEighthReplyFailure, () =>
        {
            var map = Recovering();
            for (var i = 0; i < 7; i++) Accept(map, 330_000_000 + i * 10_000_000, roundtrip: 1_295_000);
            var eighth = map.BeginProbe(new(400_000_000))!.Value;
            var reply = new ClockReply(eighth, new(450_000_000), new(450_000_000), new(401_295_000));
            Require(map.Receive(reply with { Probe = eighth with { Sequence = new(eighth.Sequence.Value + 1) } })
                .Outcome == ClockProbeOutcome.WrongIdentity);
            Require(map.Pending == eighth && map.State == ClockMappingState.Recovering);
            Require(map.BeginProbe(new(410_000_000)) is null);
            Fault(() => map.Receive(reply with { RequesterReceived = new(410_000_000) }));
            Require(map.State == ClockMappingState.Faulted && map.Pending is null);
            Fault(() => map.BeginProbe(new(411_000_000)));
            Fault(() => map.Observe(new(1_000_000_000)));
            map.Retire();
            Require(map.State == ClockMappingState.Retired);
        });
        Check(RuntimeClockCase.RecoveryEighthReplySuccess, () =>
        {
            var map = Recovering();
            for (var i = 0; i < 7; i++) Accept(map, 330_000_000 + i * 10_000_000, roundtrip: 1_295_000);
            Accept(map, 400_000_000);
            Require(map.State == ClockMappingState.Ready && map.Pending is null);
            Require(map.BeginProbe(new(649_999_999)) is null);
            Require(map.BeginProbe(new(650_000_000)) is not null);
        });
        Check(RuntimeClockCase.RecoveryDeadline, () =>
        {
            const long deadline = 571_295_000;
            var exact = Recovering();
            exact.Observe(new(deadline));
            Require(exact.State == ClockMappingState.Recovering);
            Accept(exact, deadline);
            Require(exact.State == ClockMappingState.Ready);
            var late = Recovering();
            Fault(() => late.Observe(new(deadline + 1)));
            Require(late.State == ClockMappingState.Faulted);
        });
        Check(RuntimeClockCase.RecoveryDisjointRequalification, () =>
        {
            var map = Recovering(); var epoch = map.DisplayEpoch;
            var probe = map.BeginProbe(new(330_000_000))!.Value;
            Require(map.Receive(new(probe, new(390_000_000), new(390_000_000), new(330_000_000)))
                .Outcome == ClockProbeOutcome.Disjoint);
            Require(map.State == ClockMappingState.Suspect && !map.IsQualified && map.DisplayEpoch == epoch + 1);
            for (var i = 0; i < 7; i++) Accept(map, 340_000_000 + i * 10_000_000, 60_000_000);
            Require(!map.IsQualified);
            Accept(map, 410_000_000, 60_000_000);
            Require(map.State == ClockMappingState.Ready);
        });
        Check(RuntimeClockCase.HorizonOverflow, () =>
        {
            var map = new WorkshopClockMapping(Peer);
            for (var i = 0; i < 8; i++) Accept(map, long.MaxValue - 100_000_000 + i * 10_000_000, 0);
            map.Observe(new(long.MaxValue));
            Require(map.State == ClockMappingState.Ready);
        });
        return results;
    }

    private static WorkshopClockMapping Recovering()
    {
        var map = Ready();
        Accept(map, 320_000_000, roundtrip: 1_295_000);
        Require(map.State == ClockMappingState.Recovering && map.IsQualified && map.ValidProbeCount == 8);
        return map;
    }

    private static void Fault(Action action)
    {
        try { action(); } catch (InvalidOperationException) { return; }
        throw new Exception("Bounded recovery failed to fault.");
    }

    private static WorkshopClockMapping Ready()
    {
        var map = new WorkshopClockMapping(Peer);
        for (var i = 0; i < 8; i++) Accept(map, i * 10_000_000);
        return map;
    }

    private static void Accept(WorkshopClockMapping map, long sent, long offset = 50_000_000, long roundtrip = 0)
    {
        var probe = map.BeginProbe(new(sent)) ?? throw new InvalidOperationException("Probe unexpectedly suppressed.");
        Require(map.Receive(new(probe, new(sent + offset), new(sent + offset), new(sent + roundtrip))).Outcome == ClockProbeOutcome.Accepted);
    }

    private static void Require(bool value) { if (!value) throw new InvalidOperationException("Source-linked clock oracle mismatch."); }
    private static void Reject(Action action)
    {
        try { action(); } catch (ArgumentException) { return; }
        throw new InvalidOperationException("Invalid native-clock input accepted.");
    }
}
