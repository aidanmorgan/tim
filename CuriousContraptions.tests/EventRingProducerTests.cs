using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>Story 9A.22: the event ring producer on the production compile, the worker's engine and transport and the production
/// <see cref="WorkshopSimulation"/>. After every command and tick the harness drains the device's committed publication exactly as
/// the worker does and appends it to a C# reference ring (<see cref="EventRingBuffer.Append"/>). Every expectation is derived from the
/// same run's committed reads (latches, ledgers, rows, levels), never from the records themselves.</summary>
public sealed class EventRingProducerTests
{
    private static readonly double Floor = (double)WorkshopConstruction.WorkbenchSurface.Value;
    private static readonly double Half45 = Math.Sqrt(.5);

    private static WorkshopConnection Supply(GpuBodyId source, GpuBodyId target) =>
        new(source, WorkshopSocket.Supply, target, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);
    private static WorkshopConnection Signal(GpuBodyId source, GpuBodyId target, WorkshopSocket input = WorkshopSocket.ActivationIn) =>
        new(source, WorkshopSocket.ActivationOut, target, input, WorkshopConnectionDomain.Activation);
    private static WorkshopConstruction Bench(IWorkshopInstance[] instances, WorkshopConnection[] links, SimulationCadence cadence = SimulationCadence.Hz120) =>
        new(new(2), WorkshopCadenceSettings.Default() with { Simulation = cadence }, new(instances), Connections: new(links));

    internal static SimulationEventRecord[] Decode(byte[] batch) =>
        Enumerable.Range(0, (batch.Length - EventRingBuffer.HeaderBytes) / EventRingBuffer.SlotBytes)
            .Select(i => SimulationEventRecord.Read(batch.AsSpan(EventRingBuffer.HeaderBytes + i * EventRingBuffer.SlotBytes, EventRingBuffer.SlotBytes)))
            .ToArray();

    /// <summary>Sequences contiguous from 1, ticks non-decreasing and every tick's records in the D4 order (Ordinal, Phase, Kind,
    /// Source, Other), each stamped with the tick that committed it.</summary>
    private static void AssertOrderedRun(List<SimulationEventRecord> records)
    {
        for (var i = 0; i < records.Count; i++)
        {
            Assert.Equal((ulong)i + 1, records[i].Sequence);
            if (i == 0) continue;
            var (a, b) = (records[i - 1], records[i]);
            Assert.True(a.Tick < b.Tick || (a.Tick == b.Tick && Key(a).CompareTo(Key(b)) <= 0), $"records {i - 1} and {i} are out of order");
        }
        static (uint, float, uint, ulong, ulong) Key(SimulationEventRecord record) =>
            (record.Ordinal, record.Phase, (uint)record.Kind, record.Source, record.Other);
    }

    private static ActivationLatch Node(WorkshopRead read, GpuBodyId owner)
    {
        for (var i = 0; i < read.Activations.Count; i++) if (read.Activations[i].Owner == owner) return read.Activations[i];
        throw new InvalidOperationException($"No activation node for {owner.Value}.");
    }

    /// <summary>The ticks at which a committed level (the owner's activation phase) rose (1) or released (0).</summary>
    private static List<(ulong Tick, uint Level)> Edges(List<WorkshopRead> reads, Func<WorkshopRead, bool> high)
    {
        var edges = new List<(ulong, uint)>();
        for (var i = 1; i < reads.Count; i++)
            if (high(reads[i]) != high(reads[i - 1])) edges.Add((reads[i].Tick.Value, high(reads[i]) ? 1u : 0u));
        return edges;
    }

    // ---------- Impact: one record per contact-work occurrence; the bumper never publishes a trigger ----------

    [Fact]
    public async Task EachBumperHitPublishesExactlyOneImpactAtItsOwnTimeAndNoTrigger()
    {
        GpuBodyId ball = new(1), bumper = new(2);
        await using var ring = await WorkshopIntegrationHarness.Running(Bench([WorkshopInput.Generic(PartId.Basketball, ball, 0, 4.5f, 0, 0, 0, 0, 1),
            PlacedParts.Bumper(bumper, 0, 3, 0)], []));
        await ring.Advance(360);
        var impacts = ring.Of(SimulationEventKind.Impact).ToList();
        Assert.NotEmpty(impacts);
        Assert.Equal(ring.Records.Count, impacts.Count); // no TriggerActivated, ContactLoad or any other kind
        AssertOrderedRun(ring.Records);
        // Per tick, exactly the device ledger's occurrence-count increment; each record is that new (device-private) occurrence row.
        for (var i = 1; i < ring.Reads.Count; i++)
        {
            var (before, after) = (ring.Ledgers[i - 1], ring.Ledgers[i]);
            Assert.True(after.Stores.HasSameBits(ring.Reads[i].ContactWorks)); // the read carries exactly the ledger's stores
            var tick = ring.Reads[i].Tick.Value;
            var expected = Enumerable.Range(0, after.OccurrenceCount).Select(after.Occurrence)
                .Where(hit => hit.Sequence > before[0].OccurrenceCount).ToArray();
            Assert.Equal(after[0].OccurrenceCount - before[0].OccurrenceCount, (uint)expected.Length);
            var published = impacts.Where(record => record.Tick == tick).ToArray();
            Assert.Equal(expected.Length, published.Length);
            foreach (var (hit, record) in expected.OrderBy(hit => hit.Sequence).Zip(published.OrderBy(record => record.Count)))
                Assert.Equal((bumper.Value, hit.Target.Value, hit.EventOrdinal, (float)hit.EventPhase, hit.Debit.Value, hit.Sequence, 0u),
                    (record.Source, record.Other, record.Ordinal, record.Phase, record.Magnitude, record.Count, record.Level));
        }
        // The first hit is exactly one Impact on its tick, Count 1, struck body the ball.
        var first = impacts[0];
        Assert.Equal((1u, ball.Value), (first.Count, first.Other));
        Assert.Single(impacts, record => record.Tick == first.Tick);
        // Tick contract under same-substep restitution (engine restitution slice): each Impact's ordinal is one of its own tick's
        // four substeps, and the first hit is stamped on the tick in which the ball is struck back (its velocity turns upward).
        Assert.All(impacts, record => Assert.InRange(record.Ordinal, 4 * (uint)record.Tick - 3, 4 * (uint)record.Tick));
        var struck = ring.Reads.First(read => Enumerable.Range(0, read.Bodies.Count).Select(i => read.Bodies[i].Body)
            .Single(body => body.Id == ball).Velocity.Y > 0).Tick.Value;
        Assert.Equal(struck, first.Tick);
    }

    // ---------- Passage: every pass after clearance, counted per aperture; the first-input lamp stays at one ----------

    private static (WorkshopConstruction Bench, GpuBodyId Detector, GpuBodyId Lamp, GpuBodyId Ball) DropBench(bool forwardDown, double ballX)
    {
        var sign = forwardDown ? -1 : 1;
        var detector = PlacedParts.Detector(new(1), 0, (float)(Floor + .62), 0, 0, 0, (float)(sign * Half45), (float)(Half45));
        var lamp = PlacedParts.Lamp(new(2), 4, 1, 0, 0, 0, 0, 1);
        var ball = WorkshopInput.Generic(PartId.Basketball, new(3), (float)(ballX), (float)(Floor + .34 + 3), 0, 0, 0, 0, 1);
        return (Bench([detector, lamp, ball], [Signal(detector.Id, lamp.Id)]), detector.Id, lamp.Id, ball.Id);
    }

    [Fact]
    public async Task TwoInRunPassesAfterClearancePublishPassageCountOneThenTwoWhileTheFirstInputLampStaysAtOne()
    {
        var (bench, detector, lamp, ball) = DropBench(forwardDown: true, ballX: 0);
        await using var ring = await WorkshopIntegrationHarness.Running(bench);
        await ring.Advance(480);
        // The production passage tracker replayed over the committed bodies gives the expected passes (its sensor law is unchanged).
        var network = WorkshopActivationCompiler.Compile(bench);
        var scene = WorkshopPhysicsCompiler.Compile(bench, new(1, 2));
        var tracker = PassageSensorTracker.Create(network.PassageSensors, scene, ring.Reads[0].Bodies);
        var outputs = new PassageSensorRead[8];
        var expected = new List<(ulong Tick, uint Ordinal)>();
        foreach (var read in ring.Reads.Skip(1))
        {
            tracker = tracker.Advance(read.Tick, read.Bodies, Assert.IsType<PhysicsMotionRead>(read.Motion), outputs, out _);
            expected.AddRange(tracker.Fired.Select(pass => (read.Tick.Value, pass.EventOrdinal)));
        }
        Assert.Equal(2, expected.Count);
        var passages = ring.Of(SimulationEventKind.Passage).ToArray();
        Assert.Equal(expected, passages.Select(record => (record.Tick, record.Ordinal)));
        Assert.Equal([1u, 2u], passages.Select(record => record.Count));
        Assert.All(passages, record => Assert.Equal((detector.Value, ball.Value, 0u), (record.Source, record.Other, record.Level)));
        Assert.All(passages, record => Assert.True(record.Magnitude > 0, "a pass carries its crossing speed"));
        Assert.Equal(passages.Length, ring.Records.Count); // the network's Passage and the lamp publish nothing
        // The first-input lamp lit at the first pass and keeps that one occurrence.
        var lit = ring.Reads.First(read => Node(read, lamp).Phase == ActivationPhase.Latched);
        Assert.Equal(expected[0].Tick, lit.Tick.Value);
        Assert.All(ring.Reads.Where(read => read.Tick.Value >= lit.Tick.Value), read => Assert.Equal(Node(lit, lamp), Node(read, lamp)));
        AssertOrderedRun(ring.Records);
    }

    [Theory]
    [InlineData(true, -1.1)]  // a glancing drop outside the aperture
    [InlineData(false, 0)]    // a reverse crossing through the bore
    public async Task AGlancingOrReverseCrossingPublishesNoPassage(bool forwardDown, double ballX)
    {
        var (bench, _, _, ball) = DropBench(forwardDown, ballX);
        await using var ring = await WorkshopIntegrationHarness.Running(bench);
        await ring.Advance(480);
        Assert.Contains(ring.Reads, read => read.Bodies.TryGet(ball, out var row) && (row.Body.Cell.Y + (double)row.Body.Local.Y) / 16 < Floor + .62);
        Assert.Empty(ring.Records);
    }

    // ---------- Captured ----------

    [Theory]
    [InlineData(0, true)]
    [InlineData(2, false)]
    public async Task AReceiverCapturePublishesOneCapturedAndAMissNothing(double shift, bool captures)
    {
        GpuBodyId ball = new(1), receiver = new(2);
        await using var ring = await WorkshopIntegrationHarness.Running(Bench([PayloadInstanceTests.Place(PartId.Basketball, 1, shift, 1.1, 0),
            WorkshopInput.Generic(PartId.Receiver, receiver, 0, 1, 0, 0, 0, 0, 1)], []));
        await ring.Advance(240);
        if (!captures) { Assert.Empty(ring.Records); return; }
        var latched = ring.Reads.First(read => Enumerable.Range(0, read.Captures.Count).Any(i => read.Captures[i].Phase == CaptureLatchPhase.Latched));
        var latch = latched.Captures[0];
        var record = Assert.Single(ring.Records);
        Assert.Equal((SimulationEventKind.Captured, latched.Tick.Value, receiver.Value, ball.Value, latch.EventOrdinal, (float)latch.EventPhase),
            (record.Kind, record.Tick, record.Source, record.Other, record.Ordinal, record.Phase));
    }

    // ---------- ContactLoad: the plate's debounced level, 1 then 0; jitter in the raw row publishes nothing ----------

    [Theory]
    [InlineData(2.0, false)]
    [InlineData(8.0, true)]
    public async Task APlatePublishesItsLevelEdgesAndNeverItsRowJitter(double drop, bool released)
    {
        var plateY = Floor + .22;
        var plate = PlacedParts.Plate(new(1), -4, (float)(plateY), 0, .5f);
        var ball = WorkshopInput.Generic(PartId.Basketball, new(2), -4, (float)(plateY + .08 + .345 + drop), 0, 0, 0, 0, 1);
        await using var ring = await WorkshopIntegrationHarness.Running(Bench([plate, ball], []));
        await ring.Advance(720);
        var edges = Edges(ring.Reads, read => Node(read, plate.Id).Phase == ActivationPhase.Latched);
        var records = ring.Of(SimulationEventKind.ContactLoad).ToArray();
        Assert.Equal(edges, records.Select(record => (record.Tick, record.Level)));
        Assert.Equal(records.Length, ring.Records.Count);
        static ContactLoadSensorRead Row(WorkshopRead read) => read.Presentation.Loads.ToArray().Single();
        Assert.All(records, record => Assert.Equal(Row(ring.Reads.Single(read => read.Tick.Value == record.Tick)).MeasuredMass, record.Magnitude));
        Assert.All(records, record => Assert.Equal(record.Tick * 4, record.Ordinal));
        var rowEdges = Edges(ring.Reads, read => Row(read).Phase == ContactLoadPhase.Loaded);
        Assert.Equal(1u, records[0].Level);
        if (released) Assert.Equal(0u, records[1].Level);
        else
        {
            // The short rebounds toggle the raw row (jitter), yet the held level rises once and publishes one record.
            Assert.True(rowEdges.Count > 1, $"the raw row changed {rowEdges.Count} times");
            Assert.Single(records);
        }
    }

    // ---------- TriggerActivated: switch, domino, delay; a sub-threshold contact and a hold-timer expiry publish nothing ----------

    [Theory]
    [InlineData(.8f, true)]
    [InlineData(64f, false)]
    public async Task AnImpactSwitchPublishesOneTriggerAndASubThresholdContactNothing(float threshold, bool fires)
    {
        var hit = WorkshopInput.Generic(PartId.ImpactSwitch, new(1), 0, 1, 0, 0, 0, 0, 1, new([new(new ParameterId("trigger_threshold"), threshold)]));
        var lamp = PlacedParts.Lamp(new(2), 4, 1, 0, 0, 0, 0, 1);
        var ball = WorkshopInput.Generic(PartId.Basketball, new(4), 0, 3, 0, 0, 0, 0, 1);
        await using var ring = await WorkshopIntegrationHarness.Running(Bench([hit, lamp, ball], [Signal(hit.Id, lamp.Id)]));
        await ring.Advance(480);
        Assert.Contains(ring.Reads, read => read.Bodies.TryGet(ball.Id, out var row) && row.Body.Velocity.Y > 0); // the ball struck and rebounded
        if (!fires) { Assert.Empty(ring.Records); return; }
        var latched = ring.Reads.First(read => Node(read, hit.Id).Phase == ActivationPhase.Latched);
        var occurrence = Node(latched, hit.Id);
        var record = Assert.Single(ring.Records);
        Assert.Equal((SimulationEventKind.TriggerActivated, latched.Tick.Value, hit.Id.Value, ball.Id.Value, occurrence.EventOrdinal,
                occurrence.EventPhase, occurrence.ApproachSpeed.Value, 0u),
            (record.Kind, record.Tick, record.Source, record.Other, record.Ordinal, record.Phase, record.Magnitude, record.Count));
    }

    [Theory]
    [InlineData(30, true)]
    [InlineData(10, false)]
    public async Task AWiredDominoTopplingPublishesOneTriggerAndARockingTileNothing(int degrees, bool topples)
    {
        var radians = degrees * Math.PI / 180;
        var tile = PlacedParts.Domino(new(1), 0, (float)(Floor + .55 * Math.Cos(radians) + .125 * Math.Sin(radians) + .005), 0,
            0, 0, (float)(Math.Sin(radians / 2)), (float)(Math.Cos(radians / 2)));
        await using var ring = await WorkshopIntegrationHarness.Running(Bench([tile, PlacedParts.Lamp(new(2), 3, 1, 0, 0, 0, 0, 1)], [Signal(new(1), new(2))]));
        await ring.Advance(480);
        if (!topples) { Assert.Empty(ring.Records); return; }
        var latched = ring.Reads.First(read => Node(read, tile.Id).Phase == ActivationPhase.Latched);
        var record = Assert.Single(ring.Records);
        Assert.Equal((SimulationEventKind.TriggerActivated, latched.Tick.Value, 1UL, 1UL, Node(latched, tile.Id).EventOrdinal),
            (record.Kind, record.Tick, record.Source, record.Other, record.Ordinal));
    }

    // Switch → Delay (0.1 s, a latching timer completing after physics) → hold timer (rearming, before networks) → gate: the switch and
    // the Delay each publish exactly one trigger, the Delay's 12 ticks after the switch's start tick; the hold timer's expiry (a level)
    // and the battery (always available) publish nothing.
    [Fact]
    public async Task ADelayFeedingABeforeNetworksTimerPublishesExactlyOneTriggerAndTheHoldExpiryNothing()
    {
        var construction = HoldTimerTests.Construction(.5f, delayed: true);
        await using var ring = await WorkshopIntegrationHarness.Running(construction);
        await ring.Advance(360);
        var hold = ring.Reads.Select(HoldTimerTests.State).ToArray();
        Assert.Contains(hold, state => state.Phase == ActivationTimerPhase.Counting);
        Assert.Equal(ActivationTimerPhase.Ready, hold[^1].Phase); // the hold expired within the run
        Assert.Equal(2, ring.Records.Count);
        Assert.All(ring.Records, record => Assert.Equal(SimulationEventKind.TriggerActivated, record.Kind));
        var (trigger, delayed) = (ring.Records[0], ring.Records[1]);
        Assert.Equal((2UL, 6UL), (trigger.Source, delayed.Source));
        var delayLatched = ring.Reads.First(read => Node(read, new(6)).Phase == ActivationPhase.Latched);
        Assert.Equal(delayLatched.Tick.Value, delayed.Tick);
        Assert.Equal(delayed.Tick * 4, delayed.Ordinal);
        Assert.Equal(new ActivationTime(trigger.Ordinal, trigger.Phase).CeilingTick(4) + 12, delayed.Tick);
        AssertOrderedRun(ring.Records);
    }

    // ---------- the oscillator: one trigger per pulse at every cadence; unsupplied, nothing ----------

    private static WorkshopConstruction Periodic(float seconds, uint target, bool supplied = true, SimulationCadence cadence = SimulationCadence.Hz120,
        ElectricalEnable enabled = ElectricalEnable.Enabled)
    {
        GpuBodyId battery = new(1), oscillator = new(2), tally = new(3);
        return Bench([PlacedParts.Battery(battery, -3, 3, 0, enabled: enabled), PlacedParts.Clock(oscillator, 0, 3, 0, 0, 0, 0, 1, seconds),
                PlacedParts.Counter(tally, 3, 3, 0, 0, 0, 0, 1, (int)target)],
            supplied ? [Supply(battery, oscillator), Signal(oscillator, tally)] : [Signal(oscillator, tally)], cadence);
    }

    [Theory]
    [InlineData(SimulationCadence.Hz60, 60u, 8u)]
    [InlineData(SimulationCadence.Hz120, 120u, 4u)]
    [InlineData(SimulationCadence.Hz240, 240u, 2u)]
    public async Task ASuppliedOscillatorPublishesOneTriggerPerPulseAtEveryCadence(SimulationCadence cadence, uint rate, uint substeps)
    {
        await using var ring = await WorkshopIntegrationHarness.Running(Periodic(1f, 9, cadence: cadence));
        await ring.Advance((int)(rate * 5 / 2));
        var pulses = ring.Of(SimulationEventKind.TriggerActivated).ToArray();
        Assert.Equal([(ulong)rate, 2UL * rate], pulses.Select(record => record.Tick));
        Assert.Equal([1u, 2u], pulses.Select(record => record.Count));
        Assert.All(pulses, record => Assert.Equal((2UL, 2UL, record.Tick * substeps, 0f, 0f, 0u),
            (record.Source, record.Other, record.Ordinal, record.Phase, record.Magnitude, record.Level)));
        Assert.Equal(pulses.Length, ring.Records.Count);
    }

    [Fact]
    public async Task AnUnsuppliedOscillatorPublishesNothing()
    {
        await using var ring = await WorkshopIntegrationHarness.Running(Periodic(1f, 9, supplied: false));
        await ring.Advance(600);
        Assert.Empty(ring.Records);
    }

    // ---------- ThresholdReached on the third input only; two inputs publish nothing ----------

    [Theory]
    [InlineData(380, true)]
    [InlineData(300, false)]
    public async Task ATallyOfThreePublishesThresholdReachedOnTheThirdInputOnly(int ticks, bool reached)
    {
        await using var ring = await WorkshopIntegrationHarness.Running(Periodic(1f, 3));
        await ring.Advance(ticks);
        var thresholds = ring.Of(SimulationEventKind.ThresholdReached).ToArray();
        if (!reached) { Assert.Empty(thresholds); Assert.Equal(2, ring.Records.Count); return; }
        var record = Assert.Single(thresholds);
        Assert.Equal((360UL, 3UL, 3u, 2UL, 1440u), (record.Tick, record.Source, record.Count, record.Other, record.Ordinal));
        // The same physical time as the third pulse: the D4 kind order puts ThresholdReached (3) before TriggerActivated (8).
        var tick360 = ring.Records.Where(r => r.Tick == 360).ToArray();
        Assert.Equal([SimulationEventKind.ThresholdReached, SimulationEventKind.TriggerActivated], tick360.Select(r => r.Kind));
        AssertOrderedRun(ring.Records);
    }

    // ---------- MemoryChanged ----------

    private static WorkshopConstruction Memory()
    {
        var set = PlacedParts.Clock(new(1), -3, 3, 0, 0, 0, 0, 1, 1f);
        var reset = PlacedParts.Clock(new(2), -3, 3, 3, 0, 0, 0, 1, 1.5f);
        var latch = PlacedParts.Latch(new(3), 0, 3, 0, 0, 0, 0, 1);
        var battery = PlacedParts.Battery(new(4), -6, 3, 0);
        return Bench([set, reset, latch, battery], [Supply(battery.Id, set.Id), Supply(battery.Id, reset.Id),
            Signal(set.Id, latch.Id, WorkshopSocket.SetIn), Signal(reset.Id, latch.Id, WorkshopSocket.ResetIn)]);
    }

    [Fact]
    public async Task AMemoryPublishesOneRecordPerChangeAndNoneForARedundantSet()
    {
        await using var ring = await WorkshopIntegrationHarness.Running(Memory());
        await ring.Advance(600);
        var memory = ring.Of(SimulationEventKind.MemoryChanged).ToArray();
        // On@120, Off@180, On@240, Off@360 (Set and Reset together: Reset dominates), On@480, Off@540, On@600.
        Assert.Equal([(120UL, 1u), (180UL, 0u), (240UL, 1u), (360UL, 0u), (480UL, 1u), (540UL, 0u), (600UL, 1u)],
            memory.Select(record => (record.Tick, record.Level)));
        Assert.Equal(Edges(ring.Reads, read => Node(read, new(3)).Phase == ActivationPhase.Latched), memory.Select(record => (record.Tick, record.Level)));
        Assert.All(memory, record => Assert.Equal((3UL, 0UL, record.Tick * 4, 0f), (record.Source, record.Other, record.Ordinal, record.Phase)));
        AssertOrderedRun(ring.Records);
    }

    // A 1 s oscillator on SetIn only: the memory turns On at the first pulse (120) and the Sets at 240, 360, 480 and 600, each arriving
    // while already On, publish nothing; the pulses themselves still publish their triggers.
    [Fact]
    public async Task ARepeatedSetWhileOnPublishesNoMemoryChange()
    {
        var set = PlacedParts.Clock(new(1), -3, 3, 0, 0, 0, 0, 1, 1f);
        var latch = PlacedParts.Latch(new(3), 0, 3, 0, 0, 0, 0, 1);
        var battery = PlacedParts.Battery(new(4), -6, 3, 0);
        await using var ring = await WorkshopIntegrationHarness.Running(Bench([set, latch, battery], [Supply(battery.Id, set.Id), Signal(set.Id, latch.Id, WorkshopSocket.SetIn)]));
        await ring.Advance(600);
        var memory = Assert.Single(ring.Of(SimulationEventKind.MemoryChanged));
        Assert.Equal((120UL, 1u, 3UL), (memory.Tick, memory.Level, memory.Source));
        Assert.Equal([120UL, 240UL, 360UL, 480UL, 600UL], ring.Of(SimulationEventKind.TriggerActivated).Select(record => record.Tick));
        Assert.All(ring.Reads.Where(read => read.Tick.Value >= 120), read => Assert.Equal(ActivationPhase.Latched, Node(read, latch.Id).Phase));
    }

    // ---------- ElectricalPowered: a supply source's availability edges ----------

    [Fact]
    public async Task ASupplySourceSwitchedOffAndOnPublishesOneRecordPerEdge()
    {
        await using var ring = await WorkshopIntegrationHarness.Running(Periodic(1f, 9));
        await ring.Advance(10);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await ring.Execute(WorkshopCommandKind.ConfigureElectrical, electrical: new(new(1), ElectricalEnable.Disabled))).Result.Outcome);
        await ring.Advance(10);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await ring.Execute(WorkshopCommandKind.ConfigureElectrical, electrical: new(new(1), ElectricalEnable.Enabled))).Result.Outcome);
        await ring.Advance(10);
        var powered = ring.Of(SimulationEventKind.ElectricalPowered).ToArray();
        Assert.Equal(Edges(ring.Reads, read => read.Electrical[0].Available), powered.Select(record => (record.Tick, record.Level)));
        Assert.Equal([0u, 1u], powered.Select(record => record.Level));
        Assert.All(powered, record => Assert.Equal(
            (1UL, 0UL, ring.Reads.Single(read => read.Tick.Value == record.Tick).Electrical[0].Remaining.Value),
            (record.Source, record.Other, record.Magnitude)));
        Assert.Equal(powered.Length, ring.Records.Count);
    }

    // ---------- the commit point: Pause, a discarded candidate and a failed Step publish nothing and consume no sequence ----------

    [Fact]
    public async Task PauseADiscardedCandidateAndAFailedStepPublishNothingAndConsumeNoSequence()
    {
        await using var ring = await WorkshopIntegrationHarness.Running(Periodic(1f, 9));
        await ring.Advance(119);
        Assert.Empty(ring.Records);
        // A candidate for tick 120 (the first pulse), staged and then discarded.
        var committed = ring.Committed;
        var candidate = await ring.Device.Advance(committed, new(999_999), WorkshopTickControls.For(committed.Electrical, ring.Simulation.ElectricalState));
        Assert.Equal(120UL, candidate.Read.Tick.Value);
        ring.Device.Discard(new(999_999));
        Assert.Empty(ring.Device.Events.Pending);
        Assert.Equal(0UL, ring.Device.Events.WriteSequence);
        Assert.Empty(ring.Publish());
        // Paused: no tick commits, so nothing is published.
        Assert.Equal(WorkshopCommandOutcome.Applied, ((await ring.Execute(WorkshopCommandKind.Pause)).Result).Outcome);
        Assert.Empty(ring.Publish());
        Assert.NotEqual(WorkshopCommandOutcome.Applied, (await ring.AdvanceOne()).Outcome);
        Assert.Empty(ring.Publish());
        // A Step whose installation fails is discarded: tick 120's pulse is not published and no sequence is consumed.
        ring.FailNextStep = true;
        Assert.NotEqual(WorkshopCommandOutcome.Applied, ((await ring.Execute(WorkshopCommandKind.Step)).Result).Outcome);
        Assert.Equal(119UL, ring.Committed.Tick.Value);
        Assert.Equal((0, 0UL), (ring.Device.Events.Pending.Count, ring.Device.Events.WriteSequence));
        Assert.Empty(ring.Publish());
        Assert.Empty(ring.Records);
    }

    [Fact]
    public async Task AfterADiscardTheCommittedPulseIsSequenceOne()
    {
        await using var ring = await WorkshopIntegrationHarness.Running(Periodic(1f, 9));
        await ring.Advance(119);
        var committed = ring.Committed;
        _ = await ring.Device.Advance(committed, new(999_999), WorkshopTickControls.For(committed.Electrical, ring.Simulation.ElectricalState));
        ring.Device.Discard(new(999_999));
        await ring.Advance(1);
        var record = Assert.Single(ring.Records);
        // Neither a sequence nor a count was consumed by the discarded candidate: this is the first pulse, Count 1.
        Assert.Equal((1UL, 120UL, SimulationEventKind.TriggerActivated, 1u), (record.Sequence, record.Tick, record.Kind, record.Count));
    }

    // ---------- determinism, Reset, save/load ----------

    [Fact]
    public async Task TwoRunsAndRunResetRunPublishIdenticalRecordsUnderAnAdvancingEpoch()
    {
        await using var first = await WorkshopIntegrationHarness.Running(Memory());
        await first.Advance(600);
        await using var second = await WorkshopIntegrationHarness.Running(Memory());
        await second.Advance(600);
        Assert.Equal(first.Records, second.Records);
        Assert.Equal(first.Shared!.Header, second.Shared!.Header);
        Assert.Equal((ulong)first.Records.Count, first.Shared.Header.WriteSequence);
        Assert.NotEmpty(first.Records);

        var epoch = first.Shared!.Header.Epoch;
        var once = first.Records.ToArray();
        await first.Reset();
        // Reset restarts the ring: nothing pending, sequence 0, a new epoch and its own fresh ring.
        Assert.Equal((0, 0UL), (first.Device.Events.Pending.Count, first.Device.Events.WriteSequence));
        Assert.True(first.Device.Events.Epoch.Value > epoch);
        first.Records.Clear();
        await first.Run();
        await first.Advance(600);
        Assert.Equal(once, first.Records);
        var header = first.Shared!.Header;
        Assert.True(header.Epoch > epoch, "the header carries the new run's epoch");
        Assert.Equal((ulong)once.Length, header.WriteSequence);

        // A consumer that read the first run restarts at the new epoch and reads the second run's records.
        var consumer = new EventRingConsumer();
        var read = new List<SimulationEventRecord>();
        while (consumer.TryRead(first.Shared!, SimulationDomain.None, out var record)) read.Add(record);
        Assert.Equal(once.TakeLast((int)Math.Min(header.Capacity, (uint)once.Length)), read);
        Assert.Equal(header.Epoch, consumer.Epoch);
    }

    [Fact]
    public async Task ASavedAndLoadedMachinePublishesIdenticalRecordsAndTheSaveHoldsNoRingBytes()
    {
        var bench = Memory();
        var saved = new WorkshopSavedConstruction(bench, new(7));
        var before = WorkshopSaveCodec.Encode(saved);
        await using var original = await WorkshopIntegrationHarness.Running(bench);
        await original.Advance(600);
        var loaded = WorkshopSaveCodec.Decode(SaveFixture.Elements, before);
        Assert.Equal(saved, WorkshopSaveCodec.Decode(SaveFixture.Elements, WorkshopSaveCodec.Encode(new WorkshopSavedConstruction(original.Construction, new(7)))));
        await using var replay = await WorkshopIntegrationHarness.Running(loaded.Construction);
        await replay.Advance(600);
        Assert.Equal(original.Records, replay.Records);
    }

    // ---------- actual demand, retention and representation boundaries ----------

    [Theory]
    [InlineData(SimulationCadence.Hz60)]
    [InlineData(SimulationCadence.Hz120)]
    [InlineData(SimulationCadence.Hz240)]
    public async Task IdleProducerRowsDoNotReserveEventCapacity(SimulationCadence cadence)
    {
        await using var ring = await WorkshopIntegrationHarness.Building(Periodic(1f, 9, cadence: cadence));
        Assert.Equal(32u, ring.Device.Events.Capacity);
        Assert.Equal(32u, ring.Shared!.Capacity);
        Assert.Equal(0UL, ring.Shared.LoadWriteSequence());
        Assert.Equal(0, ring.Device.Events.RetainedCount);
    }

    [Fact]
    public async Task AnEmptyBenchHasTheCommonThirtyTwoSlotReserve()
    {
        await using var ring = await WorkshopIntegrationHarness.Building(Bench([], []));
        Assert.Equal(32u, ring.Shared!.Capacity);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(40)]
    public async Task BumperPoliciesReserveNoPotentialPairOrEventRows(int n)
    {
        var parts = new List<IWorkshopInstance>();
        for (var i = 0; i < n; i++)
        {
            parts.Add(PlacedParts.Bumper(new((ulong)(2 * i + 1)), (float)(-60 + 3 * i), 3, 0));
            parts.Add(WorkshopInput.Generic(PartId.Basketball, new((ulong)(2 * i + 2)), (float)(-60 + 3 * i), 6, 0, 0, 0, 0, 1));
        }
        await using var ring = await WorkshopIntegrationHarness.Building(Bench([.. parts], []));
        Assert.Equal(n, ring.Device.CommittedContactWork.Count);
        Assert.Equal(0, ring.Device.CommittedContactWork.OccurrenceCount);
        Assert.Equal(32u, ring.Device.Events.Capacity);
        await ring.Run();
        await ring.Advance(90);
        Assert.Equal(n, ring.Of(SimulationEventKind.Impact).Count());
        Assert.Equal(n, ring.Device.CommittedContactWork.OccurrenceCount);
        Assert.InRange(ring.Device.Events.Capacity, (uint)Math.Max(32, n), (uint)Math.Max(32, (8 * n + 2) / 3));
    }

    [Fact]
    public void RingSizingAcceptsEveryRepresentableDemandWithoutAllocatingIt()
    {
        var limit = (uint)((Array.MaxLength - EventRingBuffer.HeaderBytes) / EventRingBuffer.SlotBytes);
        Assert.Equal(32u, EventRingBuffer.Capacity(32, 0));
        Assert.Equal(32u, EventRingBuffer.Capacity(32, 24));
        Assert.Equal(64u, EventRingBuffer.Capacity(32, 25));
        Assert.Equal(limit, EventRingBuffer.Capacity(32, limit));
        Assert.Equal(limit, EventRingBuffer.Capacity(32, limit - 1));
        Assert.Equal(limit, EventRingBuffer.Capacity(limit, limit));
        Assert.Equal(EventRingBuffer.HeaderBytes + (long)limit * EventRingBuffer.SlotBytes, EventRingBuffer.ByteLength(limit));
        Assert.Equal(EventPublicationPreparation.StorageUnavailable, Assert.Throws<EventPreparationException>(() => EventRingBuffer.Capacity(32, (ulong)limit + 1)).Reason);
        Assert.Equal(EventPublicationPreparation.StorageUnavailable, Assert.Throws<EventPreparationException>(() => EventRingBuffer.ByteLength(limit + 1)).Reason);
        Assert.Equal(EventPublicationPreparation.StorageUnavailable, Assert.Throws<EventPreparationException>(() => EventRingBuffer.Capacity(32, ulong.MaxValue)).Reason);
        Assert.Equal(ulong.MaxValue, EventRingBuffer.LastSequence(ulong.MaxValue - 2, 2));
        Assert.Equal(EventPublicationPreparation.SequenceExhausted,
            Assert.Throws<EventPreparationException>(() => EventRingBuffer.LastSequence(ulong.MaxValue, 1)).Reason);
        Assert.Throws<ArgumentOutOfRangeException>(() => EventRingBuffer.LastSequence(0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EventPreparationException((EventPublicationPreparation)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EventPreparationException(EventPublicationPreparation.Ready));
        // Modulo indexing is the same rule for a small non-power-of-two ring.
        var ring = new EventRingMemory(3, 1);
        for (ulong sequence = 1; sequence <= 5; sequence++)
        {
            var record = new SimulationEventRecord(sequence, sequence, SimulationEventKind.Impact, 1, 2, (uint)sequence, 0, 0, 0, 0);
            ring = EventRingBuffer.Append(ring, EventRingBuffer.Batch(new(3, 64, 1, sequence), [record]));
        }
        var consumer = new EventRingConsumer();
        var seen = new List<ulong>();
        while (consumer.TryRead(ring, SimulationDomain.None, out var record)) seen.Add(record.Sequence);
        Assert.Equal([3UL, 4UL, 5UL], seen);
        Assert.Equal(2UL, consumer.Lapped);
    }

    [Theory]
    [InlineData(8738)]
    [InlineData(8739)]
    public async Task FormerProducerLimitAdmitsWithoutAllocatingHypotheticalEvents(int producers)
    {
        var sources = Enumerable.Range(0, producers)
            .Select(i => (IWorkshopInstance)PlacedParts.Battery(new((ulong)i + 1), (float)(-63 + i % 127), 3, (float)(-63 + i / 127))).ToArray();
        await using var ring = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        Assert.Equal(WorkshopCommandOutcome.Applied, ((await ring.Execute(WorkshopCommandKind.Construct, Bench(sources, []))).Result).Outcome);
        ring.Publish();
        Assert.Equal(producers, ring.Committed.Electrical.Count);
        Assert.Equal(32u, ring.Device.Events.Capacity);
        Assert.Equal(0UL, ring.Shared!.LoadWriteSequence());
        Assert.Equal(WorkshopCommandOutcome.Applied, ((await ring.Execute(WorkshopCommandKind.Construct, Periodic(1f, 9) with { Revision = new(3) })).Result).Outcome);
        ring.Publish();
        Assert.Equal(32u, ring.Device.Events.Capacity);
    }

    [Fact]
    public async Task AnUndrainedBurstGrowsBeforeCommitAndPublishesEveryRecord()
    {
        await using var control = await WorkshopIntegrationHarness.Running(Periodic(.1f, 9));
        await control.Advance(3588);
        await using var burst = await WorkshopIntegrationHarness.Running(Periodic(.1f, 9));
        var publication = await burst.AdvanceUndrained(3588);
        Assert.Equal(3588UL, burst.Committed.Tick.Value);
        var all = control.Records;
        Assert.True(all.Count > 256);
        var header = EventRingHeader.Read(publication);
        Assert.Equal(((ulong)all.Count, 0UL), (header.WriteSequence, 0UL));
        Assert.InRange(header.Capacity, (uint)all.Count, (uint)((8 * all.Count + 2) / 3));
        Assert.Equal(all, Decode(publication));
        var consumer = new EventRingConsumer();
        var read = new List<SimulationEventRecord>();
        while (consumer.TryRead(burst.Shared!, SimulationDomain.None, out var record)) read.Add(record);
        Assert.Equal(all, read);
        Assert.Equal(0UL, consumer.Lapped);
        await burst.Advance(12);
        Assert.Equal((ulong)all.Count + 1, burst.Records[^1].Sequence);
    }

    [Fact]
    public void PreparedGrowthDiscardAndSameEpochPublicationPreserveIndependentCursors()
    {
        var outbox = new EventRingOutbox();
        var host = new EventRingPublication();
        outbox.PrepareAdmission(new(5)); Assert.Equal(EventPublicationPreparation.Ready, host.Prepare(outbox.PreparedBatch));
        outbox.Commit(); host.Commit(); host.Publish(); outbox.Drain();
        var original = host.Memory!;
        var first = Enumerable.Range(1, 25).Select(i => new SimulationEventRecord(0, 2, SimulationEventKind.Impact, 1, 2, (uint)i, 0, 1, (uint)i, 0)).ToArray();
        outbox.PrepareAdvance(32, first); Assert.Equal(EventPublicationPreparation.Ready, host.Prepare(outbox.PreparedBatch));
        Assert.Same(original, host.Memory);
        Assert.Equal(0UL, original.LoadWriteSequence());
        outbox.Discard(); host.Discard();
        Assert.Equal((32u, 0UL), (outbox.Capacity, outbox.WriteSequence));
        outbox.PrepareAdvance(32, first); Assert.Equal(EventPublicationPreparation.Ready, host.Prepare(outbox.PreparedBatch));
        outbox.Commit(); host.Commit();
        Assert.Same(original, host.Memory);
        host.Publish(); outbox.Drain();
        Assert.Equal(64u, host.Memory!.Capacity);
        var fast = new EventRingConsumer(); var slow = new EventRingConsumer();
        var firstSeen = new List<ulong>();
        while (fast.TryRead(host.Memory, SimulationDomain.None, out var record)) firstSeen.Add(record.Sequence);
        Assert.Equal(Enumerable.Range(1, 25).Select(i => (ulong)i), firstSeen);
        var previous = host.Memory;
        var second = Enumerable.Range(33, 24).Select(i => new SimulationEventRecord(0, 3, SimulationEventKind.Impact, 1, 2, (uint)i, 0, 1, (uint)i, 0)).ToArray();
        outbox.PrepareAdvance(56, second); Assert.Equal(EventPublicationPreparation.Ready, host.Prepare(outbox.PreparedBatch));
        Assert.Equal(25UL, previous.LoadWriteSequence());
        outbox.Commit(); host.Commit(); host.Publish(); outbox.Drain();
        Assert.Equal(128u, host.Memory!.Capacity);
        var fastSeen = new List<ulong>(); var slowSeen = new List<ulong>();
        while (fast.TryRead(host.Memory, SimulationDomain.None, out var record)) fastSeen.Add(record.Sequence);
        while (slow.TryRead(host.Memory, SimulationDomain.None, out var record)) slowSeen.Add(record.Sequence);
        Assert.Equal(Enumerable.Range(26, 24).Select(i => (ulong)i), fastSeen);
        Assert.Equal(Enumerable.Range(1, 49).Select(i => (ulong)i), slowSeen);
        Assert.Equal(0UL, fast.Lapped); Assert.Equal(0UL, slow.Lapped);
        Assert.Empty(outbox.Drain());
        outbox.PrepareAdmission(new(6)); Assert.Equal(EventPublicationPreparation.Ready, host.Prepare(outbox.PreparedBatch));
        outbox.Discard(); host.Discard(); host.Publish();
        Assert.Equal(5UL, host.Memory.Epoch);
        outbox.PrepareAdmission(new(6)); Assert.Equal(EventPublicationPreparation.Ready, host.Prepare(outbox.PreparedBatch));
        outbox.Commit(); host.Commit(); host.Publish();
        Assert.Equal((6UL, 32u, 0UL), (host.Memory!.Epoch, host.Memory.Capacity, host.Memory.LoadWriteSequence()));
        Assert.False(fast.TryRead(host.Memory, SimulationDomain.None, out _));
        Assert.Equal(0UL, fast.Cursor);
        host.Dispose(); outbox.Dispose();
        Assert.Null(host.Memory); Assert.Empty(outbox.Pending);
        Assert.Throws<ObjectDisposedException>(() => host.Prepare(EventRingBuffer.Batch(new(1, 64, 7, 0), [])));
        Assert.Throws<ObjectDisposedException>(() => host.Commit());
    }

    [Theory]
    [InlineData(2u)]
    [InlineData(4u)]
    [InlineData(8u)]
    public void HalfSecondRetentionUsesPhysicalOrdinalsAcrossCadenceAndEmptyTicks(uint substeps)
    {
        var outbox = new EventRingOutbox();
        outbox.PrepareAdmission(new(1)); outbox.Commit(); outbox.Drain();
        var atWatermark = Enumerable.Range(1, 24).Select(i => new SimulationEventRecord(0, 1, SimulationEventKind.Impact, 1, (ulong)i, 8, 0, 1, (uint)i, 0)).ToArray();
        outbox.PrepareAdvance(8, atWatermark); outbox.Commit(); outbox.Drain();
        Assert.Equal(32u, outbox.Capacity);
        for (uint ordinal = 8 + substeps; ordinal <= 248; ordinal += substeps)
        { outbox.PrepareAdvance(ordinal, []); outbox.Commit(); outbox.Drain(); }
        Assert.Equal(24, outbox.RetainedCount); // inclusive physical half-second boundary
        outbox.PrepareAdvance(248 + substeps, []); outbox.Commit(); outbox.Drain();
        Assert.Equal(0, outbox.RetainedCount);
        outbox.PrepareAdvance(1000, []); outbox.Commit(); outbox.Drain();
        Assert.Equal(32u, outbox.Capacity);
        var afterIdle = atWatermark.Select(record => record with { Ordinal = 1001 }).ToArray();
        outbox.PrepareAdvance(1002, afterIdle); outbox.Commit();
        Assert.Equal(32u, outbox.Capacity); Assert.Equal(24, outbox.RetainedCount);
        Assert.Equal(Enumerable.Range(25, 24).Select(i => (ulong)i), Decode(outbox.Drain()).Select(record => record.Sequence));
        outbox.PrepareAdvance(1004, [new(0, 2, SimulationEventKind.Impact, 1, 25, 1003, 0, 1, 25, 0)]);
        outbox.Commit();
        Assert.Equal(64u, outbox.Capacity); Assert.Equal(25, outbox.RetainedCount);
        Assert.Equal(49UL, Assert.Single(Decode(outbox.Drain())).Sequence);
    }

    // ---------- round trip through the shipped worker's writeEventRing ----------


    private static (int Rings, byte[] Ring) RunWorker(IReadOnlyList<byte[]> batches)
    {
        var path = Path.Combine(twodog.Engine.ResolveProjectDir(), ".runtime-build/tools/runtime/event-roundtrip.js");
        var start = new ProcessStartInfo("node") { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, WorkingDirectory = twodog.Engine.ResolveProjectDir() };
        start.ArgumentList.Add("--experimental-vm-modules"); start.ArgumentList.Add("--no-warnings");
        start.ArgumentList.Add(path);
        using var node = Process.Start(start) ?? throw new InvalidOperationException("Node could not be started; the event ring round trip needs it.");
        node.StandardInput.Write(JsonSerializer.Serialize(new { batches = batches.Select(Convert.ToHexString) }));
        node.StandardInput.Close();
        var stdout = node.StandardOutput.ReadToEndAsync(); var stderr = node.StandardError.ReadToEndAsync();
        Assert.True(node.WaitForExit(30_000), "The worker script did not finish within 30 s.");
        Assert.True(node.ExitCode == 0, $"The worker script failed ({node.ExitCode}): {stderr.Result}");
        var result = JsonDocument.Parse(stdout.Result).RootElement;
        return (result.GetProperty("rings").GetInt32(), Convert.FromHexString(result.GetProperty("ring").GetString()!));
    }

    // Every publication of a real run (the empty pre-admission one, header-only admissions under three epochs, the run's records) and
    // then an overflowing one, through the shipped worker.js writer and through the C# reference: the same ring bytes, header and
    // records, and a fresh ring exactly at each epoch or capacity change.
    [Fact]
    public async Task TheShippedWorkerPreservesRecordsAcrossRealRunAndDemandGrowth()
    {
        await using var ring = await WorkshopIntegrationHarness.Running(Memory());
        await ring.Advance(600);
        await ring.Reset();
        await ring.Run();
        await ring.Advance(250);
        var outbox = new EventRingOutbox();
        outbox.PrepareAdmission(new(99)); outbox.Commit(); outbox.Drain();
        var selected = Enumerable.Range(1, 25).Select(i => new SimulationEventRecord(0, 8, SimulationEventKind.Impact,
            1, (ulong)i + 1, (uint)i, 0, 1, (uint)i, 0)).ToArray();
        outbox.PrepareAdvance(selected.Max(record => record.Ordinal) + 1, selected); outbox.Commit();
        var batches = ring.Publications.Append(outbox.Drain()).ToList();
        EventRingMemory? reference = null;
        var fresh = 0;
        foreach (var batch in batches.Where(batch => batch.Length != 0))
        {
            var next = EventRingBuffer.Append(reference, batch);
            if (!ReferenceEquals(next, reference)) fresh++;
            reference = next;
        }
        var (rings, written) = RunWorker(batches);
        Assert.Equal(fresh, rings);
        Assert.Equal(reference!.Header, EventRingHeader.Read(written));
        Assert.Equal(new EventRingHeader(64, 64, 99, 25), EventRingHeader.Read(written));
        var shipped = EventRingMemory.Load(written);
        var consumer = new EventRingConsumer();
        var read = new List<SimulationEventRecord>();
        while (consumer.TryRead(shipped, SimulationDomain.None, out var record)) read.Add(record);
        Assert.Equal(selected.Select((record, i) => record with { Sequence = (ulong)i + 1 }), read);
    }

    // ---------- conformance: each package declares exactly the kinds its compiled rows produce ----------

    [Fact]
    public async Task EveryPackageDeclaresExactlyTheKindsItsCompiledRowsProduce()
    {
        var lampKinds = new HashSet<ActivationNodeKind>();
        foreach (var entry in TestBundle.Entries)
        {
            var id = new PartId(entry.Metadata.Id);
            var part = PackageRuntimeParityTests.Placed(id);
            IWorkshopInstance[] parts = [part, WorkshopInput.Generic(PartId.Basketball, new(2), 6, 3, 0, 0, 0, 0, 1)];
            WorkshopConnection[] links = [];
            if (DeclaredPorts.Current.Has(id, WorkshopSocket.ActivationOut, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Output))
            {
                parts = [.. parts, PlacedParts.Lamp(new(3), -6, 3, 0, 0, 0, 0, 1)];
                links = [Signal(part.Id, new(3))];
            }
            await using var ring = await WorkshopIntegrationHarness.Building(Bench(parts, links));
            var scene = WorkshopPhysicsCompiler.Compile(Bench(parts, links), new(1, 2));
            var network = WorkshopActivationCompiler.Compile(Bench(parts, links));
            SimulationEventKind[] Produced(GpuBodyId owner) =>
                scene.ContactWorks.ToArray().Where(row => row.Owner == owner).Select(_ => SimulationEventKind.Impact)
                    .Concat(scene.Sensors.ToArray().Where(row => row.Frame == owner).Select(_ => SimulationEventKind.Captured))
                    .Concat(scene.Electrical.Sources.ToArray().Where(row => row.Owner == owner).Select(_ => SimulationEventKind.ElectricalPowered))
                    .Concat(network.PassageSensors.ToArray().Where(row => row.Frame == owner).Select(_ => SimulationEventKind.Passage))
                    .Concat(network.Nodes.ToArray().Where(row => row.Owner == owner).Select(row => EventRecordProducer.KindOf(row.Kind, row.Completion)))
                    .Where(kind => kind != SimulationEventKind.None).Distinct().Order().ToArray();
            var produced = Produced(part.Id);
            var declared = entry.Events.Select(descriptor => descriptor.Kind).Order().ToArray();
            Assert.True(declared.SequenceEqual(produced),
                $"{entry.Metadata.Id}: declares [{string.Join(", ", declared)}], its compiled rows produce [{string.Join(", ", produced)}]");
            // Review F3: the wired lamp conforms in each of its wirings: a level indicator (fed by a memory or a load level) and a
            // first-input indicator (fed by a pulse) both declare and produce nothing.
            if (links.Length == 0) continue;
            lampKinds.Add(WorkshopActivationCompiler.Compile(Bench(parts, links)).Nodes.ToArray().Single(node => node.Owner == new GpuBodyId(3)).Kind);
            Assert.True(Produced(new(3))
                .SequenceEqual(TestBundle.Entry(PartId.SignalLamp).Events.Select(descriptor => descriptor.Kind)), $"{entry.Metadata.Id}: the wired lamp produces a record kind");
        }
        Assert.Equal([ActivationNodeKind.StickyFirstInput, ActivationNodeKind.LevelIndicator], lampKinds.Order());
    }

    // ---------- no second producer: only the device's commit point and the worker's copy touch the ring ----------

    [Fact]
    public void OnlyTheEngineProducerAndTheWorkerWriteTheRing()
    {
        // This checkout's own production sources (tracked and untracked, not ignored), never another worktree's or a build output.
        var root = twodog.Engine.ResolveProjectDir();
        var start = new ProcessStartInfo("git") { WorkingDirectory = root, RedirectStandardOutput = true, UseShellExecute = false };
        foreach (var argument in new[] { "ls-files", "--cached", "--others", "--exclude-standard" }) start.ArgumentList.Add(argument);
        using var git = Process.Start(start) ?? throw new InvalidOperationException("git could not be started.");
        var listed = git.StandardOutput.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        git.WaitForExit();
        Assert.Equal(0, git.ExitCode);
        string[] excluded = ["_bmad-output", "docs", "CuriousContraptions.tests", "tools"];
        var sources = listed
            .Where(path => path.EndsWith(".cs", StringComparison.Ordinal) || path.EndsWith(".js", StringComparison.Ordinal) || path.EndsWith(".ts", StringComparison.Ordinal))
            .Where(path => !excluded.Contains(path.Split('/')[0]) && File.Exists(Path.Combine(root, path)))
            .Where(path => !path.EndsWith(".js", StringComparison.Ordinal) || !File.Exists(Path.ChangeExtension(Path.Combine(root, path), ".ts")))
            .ToArray();
        Assert.Contains("engine/gpu/EventRecordProducer.cs", sources);
        string[] Hits(string pattern) => sources.Where(path => File.ReadAllText(Path.Combine(root, path)).Contains(pattern, StringComparison.Ordinal)).Order().ToArray();
        Assert.Equal(["CuriousContraptions.Simulation/WorkshopGpuDevice.cs", "engine/gpu/EventRecordProducer.cs"], Hits("EventRingOutbox"));
        Assert.Empty(Hits("EventRingBuffer.Append")); // the C# reference writer is the tests' only; production publishes through the worker
        Assert.Equal(["CuriousContraptions.Simulation/wwwroot/worker.ts"], Hits("function prepareEvents("));
        Assert.Equal(["CuriousContraptions.Simulation/Program.cs", "CuriousContraptions.Simulation/WorkshopGpuDevice.cs"], Hits("DrainEvents"));
        foreach (var retired in new[] { "readEventRing", "createEventConsumer", "WorkshopEventRing", "eventBuffer" }) Assert.Empty(Hits(retired));
        // Story 9A.23 + 9A.25b: the occurrence-carrying reliable reads, the main-thread one-shot relay and the one-shot relay drivers.
        foreach (var retired in new[] { "occurrenceReads", "reserveOccurrenceRead", "releaseOccurrenceRead", "retainOccurrence", "RequiresReliableRead",
            "ContactWorkOccurrenceRecord", "PumpCosmetics", "PumpCapture", "PumpActivations", "PumpContactFeedback", "ObserveContactFeedback",
            "TryCosmeticFrame", "CosmeticCurveDeclaration", "AnimationFeedbackSource", "CosmeticBindingSet", "WorkshopCosmeticSample", "IsOneShot",
            "AnimationControlKind.Impulse", "AnimationControlKind.Transition", "ActivationPulse", "ContactImpulse", "CapturePulse" })
            Assert.Empty(Hits(retired));
        // One shared event ring reader, imported by both consumers (the C# reference reader names it as its twin).
        Assert.Equal(["CuriousContraptions.Abstractions/EventRingBuffer.cs", "CuriousContraptions.Animation.Worker/wwwroot/worker.ts",
            "CuriousContraptions.web/wwwroot/event-ring-cursor.ts",
            "CuriousContraptions.web/wwwroot/workshop-client.ts"], Hits("EventRingCursor"));
    }
}
