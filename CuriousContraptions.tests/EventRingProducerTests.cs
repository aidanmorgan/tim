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

    /// <summary>An installation that refuses the next paused Prepare when armed (a failed Step).</summary>
    private sealed class Installation : IWorkshopInstallation
    {
        public bool Fail { get; set; }
        public void Abort(CommandSequence owner) { }
        public void Retire() { }
        public ValueTask Prepare(CommandSequence owner, WorkshopRead read, WorkshopConstruction construction, WorkshopGpuProfile profile,
            WorkshopSimulationPhase phase) =>
            Fail && phase == WorkshopSimulationPhase.Paused ? throw new WorkshopInstallationException(new InvalidOperationException("refused")) : ValueTask.CompletedTask;
    }

    /// <summary>The production simulation with the worker's publication path: every command and tick is followed by one drain.</summary>
    internal sealed class Ring : IAsyncDisposable
    {
        public WorkshopGpuDevice Device { get; } = new(new CuriousContraptions.Simulation.BrowserGpuTransport(), new(101, 206));
        public WorkshopSimulation Simulation { get; }
        public byte[]? Bytes { get; private set; }
        public List<SimulationEventRecord> Records { get; } = [];
        public List<byte[]> Publications { get; } = [];
        public List<WorkshopRead> Reads { get; } = [];
        /// <summary>The device-private contact-work ledger beside each committed read (Story 9A.23 D9).</summary>
        public List<ContactWorkLedger> Ledgers { get; } = [];
        private readonly Installation _installation;
        public bool FailNextStep { set => _installation.Fail = value; }

        private Ring(WorkshopCadenceSettings settings)
        {
            _installation = new();
            Simulation = new(Device, new WorkshopPhysicsEngineTests.StepClock(), settings, new(1), _installation, GpuDeviceOwnership.Borrowed);
        }

        public static async Task<Ring> Initialized(WorkshopCadenceSettings settings)
        {
            var ring = new Ring(settings);
            Assert.Equal(WorkshopCommandOutcome.Applied, (await ring.Simulation.Initialize()).Outcome);
            ring.Publish();
            return ring;
        }

        public static async Task<Ring> Building(WorkshopConstruction construction)
        {
            var ring = await Initialized(construction.Settings);
            Assert.Equal(WorkshopCommandOutcome.Applied, (await ring.Simulation.Construct(construction)).Outcome);
            ring.Publish();
            return ring;
        }

        public static async Task<Ring> Running(WorkshopConstruction construction)
        {
            var ring = await Building(construction);
            await ring.Run();
            return ring;
        }

        public async Task Run()
        {
            Assert.Equal(WorkshopCommandOutcome.Applied, (await Simulation.Run()).Outcome);
            Publish();
            Reads.Clear(); Ledgers.Clear();
            Reads.Add(Simulation.Committed); Ledgers.Add(Device.CommittedContactWork);
        }

        /// <summary>The worker's drain: one publication, appended to the reference ring; its records are kept in publication order.</summary>
        public byte[] Publish()
        {
            var batch = Device.DrainEvents();
            Publications.Add(batch);
            if (batch.Length == 0) return batch;
            Bytes = EventRingBuffer.Append(Bytes, batch);
            Records.AddRange(Decode(batch));
            return batch;
        }

        public async Task Advance(int ticks)
        {
            for (var tick = 0; tick < ticks; tick++)
            {
                Assert.Equal(WorkshopCommandOutcome.Applied, (await Simulation.Advance()).Outcome);
                Publish();
                Reads.Add(Simulation.Committed); Ledgers.Add(Device.CommittedContactWork);
            }
        }

        /// <summary>Commits ticks without publishing (as when no read or acknowledgement follows them), then publishes once.</summary>
        public async Task<byte[]> AdvanceUndrained(int ticks)
        {
            for (var tick = 0; tick < ticks; tick++)
            {
                Assert.Equal(WorkshopCommandOutcome.Applied, (await Simulation.Advance()).Outcome);
                Reads.Add(Simulation.Committed); Ledgers.Add(Device.CommittedContactWork);
            }
            return Publish();
        }

        public async Task Reset()
        {
            Assert.Equal(WorkshopCommandOutcome.Applied, (await Simulation.Reset()).Outcome);
            Publish();
        }

        public IEnumerable<SimulationEventRecord> Of(SimulationEventKind kind) => Records.Where(record => record.Kind == kind);

        public async ValueTask DisposeAsync()
        {
            await Simulation.DisposeAsync();
            await Device.DisposeAsync();
        }
    }

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
        await using var ring = await Ring.Running(Bench([WorkshopInput.Generic(PartId.Basketball, ball, 0, 4.5, 0, 0, 0, 0, 1),
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
        var detector = PlacedParts.Detector(new(1), 0, Floor + .62, 0, 0, 0, sign * Half45, Half45);
        var lamp = PlacedParts.Lamp(new(2), 4, 1, 0, 0, 0, 0, 1);
        var ball = WorkshopInput.Generic(PartId.Basketball, new(3), ballX, Floor + .34 + 3, 0, 0, 0, 0, 1);
        return (Bench([detector, lamp, ball], [Signal(detector.Id, lamp.Id)]), detector.Id, lamp.Id, ball.Id);
    }

    [Fact]
    public async Task TwoInRunPassesAfterClearancePublishPassageCountOneThenTwoWhileTheFirstInputLampStaysAtOne()
    {
        var (bench, detector, lamp, ball) = DropBench(forwardDown: true, ballX: 0);
        await using var ring = await Ring.Running(bench);
        await ring.Advance(480);
        // The production passage tracker replayed over the committed bodies gives the expected passes (its sensor law is unchanged).
        var network = WorkshopActivationCompiler.Compile(bench);
        var colliders = WorkshopPhysicsCompiler.Compile(bench, new(1, 2)).Colliders.ToArray();
        var tracker = PassageSensorTracker.Create(network.PassageSensors, colliders, ring.Reads[0].Bodies);
        var outputs = new PassageSensorRead[8];
        var expected = new List<(ulong Tick, uint Ordinal)>();
        foreach (var read in ring.Reads.Skip(1))
        {
            tracker = tracker.Advance(read.Tick, read.Bodies, read.Motion, outputs, out _);
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
        await using var ring = await Ring.Running(bench);
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
        await using var ring = await Ring.Running(Bench([PayloadInstanceTests.Place(PartId.Basketball, 1, shift, 1.1, 0),
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
        var plate = PlacedParts.Plate(new(1), -4, plateY, 0, .5f);
        var ball = WorkshopInput.Generic(PartId.Basketball, new(2), -4, plateY + .08 + .345 + drop, 0, 0, 0, 0, 1);
        await using var ring = await Ring.Running(Bench([plate, ball], []));
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
        await using var ring = await Ring.Running(Bench([hit, lamp, ball], [Signal(hit.Id, lamp.Id)]));
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
        var tile = PlacedParts.Domino(new(1), 0, Floor + .55 * Math.Cos(radians) + .125 * Math.Sin(radians) + .005, 0,
            0, 0, Math.Sin(radians / 2), Math.Cos(radians / 2));
        await using var ring = await Ring.Running(Bench([tile, PlacedParts.Lamp(new(2), 3, 1, 0, 0, 0, 0, 1)], [Signal(new(1), new(2))]));
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
        await using var ring = await Ring.Running(construction);
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
        await using var ring = await Ring.Running(Periodic(1f, 9, cadence: cadence));
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
        await using var ring = await Ring.Running(Periodic(1f, 9, supplied: false));
        await ring.Advance(600);
        Assert.Empty(ring.Records);
    }

    // ---------- ThresholdReached on the third input only; two inputs publish nothing ----------

    [Theory]
    [InlineData(380, true)]
    [InlineData(300, false)]
    public async Task ATallyOfThreePublishesThresholdReachedOnTheThirdInputOnly(int ticks, bool reached)
    {
        await using var ring = await Ring.Running(Periodic(1f, 3));
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
        await using var ring = await Ring.Running(Memory());
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
        await using var ring = await Ring.Running(Bench([set, latch, battery], [Supply(battery.Id, set.Id), Signal(set.Id, latch.Id, WorkshopSocket.SetIn)]));
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
        await using var ring = await Ring.Running(Periodic(1f, 9));
        await ring.Advance(10);
        Assert.Equal(WorkshopCommandOutcome.Applied, ring.Simulation.ConfigureElectrical(new(new(1), ElectricalEnable.Disabled)).Outcome);
        await ring.Advance(10);
        Assert.Equal(WorkshopCommandOutcome.Applied, ring.Simulation.ConfigureElectrical(new(new(1), ElectricalEnable.Enabled)).Outcome);
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
        await using var ring = await Ring.Running(Periodic(1f, 9));
        await ring.Advance(119);
        Assert.Empty(ring.Records);
        // A candidate for tick 120 (the first pulse), staged and then discarded.
        var committed = ring.Simulation.Committed;
        var candidate = await ring.Device.Advance(committed, new(999_999), WorkshopTickControls.For(committed.Electrical, ring.Simulation.ElectricalState));
        Assert.Equal(120UL, candidate.Read.Tick.Value);
        ring.Device.Discard(new(999_999));
        Assert.Empty(ring.Device.Events.Pending);
        Assert.Equal(0UL, ring.Device.Events.WriteSequence);
        Assert.Equal(EventRingBuffer.HeaderBytes, ring.Publish().Length);
        // Paused: no tick commits, so nothing is published.
        Assert.Equal(WorkshopCommandOutcome.Applied, (await ring.Simulation.Pause()).Outcome);
        Assert.Equal(EventRingBuffer.HeaderBytes, ring.Publish().Length);
        Assert.NotEqual(WorkshopCommandOutcome.Applied, (await ring.Simulation.Advance()).Outcome);
        Assert.Equal(EventRingBuffer.HeaderBytes, ring.Publish().Length);
        // A Step whose installation fails is discarded: tick 120's pulse is not published and no sequence is consumed.
        ring.FailNextStep = true;
        Assert.NotEqual(WorkshopCommandOutcome.Applied, (await ring.Simulation.Step()).Outcome);
        Assert.Equal(119UL, ring.Simulation.Committed.Tick.Value);
        Assert.Equal((0, 0UL), (ring.Device.Events.Pending.Count, ring.Device.Events.WriteSequence));
        Assert.Equal(EventRingBuffer.HeaderBytes, ring.Publish().Length);
        Assert.Empty(ring.Records);
    }

    [Fact]
    public async Task AfterADiscardTheCommittedPulseIsSequenceOne()
    {
        await using var ring = await Ring.Running(Periodic(1f, 9));
        await ring.Advance(119);
        var committed = ring.Simulation.Committed;
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
        await using var first = await Ring.Running(Memory());
        await first.Advance(600);
        await using var second = await Ring.Running(Memory());
        await second.Advance(600);
        Assert.Equal(first.Records, second.Records);
        Assert.Equal(first.Bytes, second.Bytes);
        Assert.NotEmpty(first.Records);

        var epoch = EventRingHeader.Read(first.Bytes).Epoch;
        var once = first.Records.ToArray();
        await first.Reset();
        // Reset restarts the ring: nothing pending, sequence 0, a new epoch and its own fresh ring.
        Assert.Equal((0, 0UL), (first.Device.Events.Pending.Count, first.Device.Events.WriteSequence));
        Assert.True(first.Device.Events.Epoch.Value > epoch);
        first.Records.Clear();
        await first.Run();
        await first.Advance(600);
        Assert.Equal(once, first.Records);
        var header = EventRingHeader.Read(first.Bytes);
        Assert.True(header.Epoch > epoch, "the header carries the new run's epoch");
        Assert.Equal((ulong)once.Length, header.WriteSequence);

        // A consumer that read the first run restarts at the new epoch and reads the second run's records.
        var consumer = new EventRingConsumer();
        var read = new List<SimulationEventRecord>();
        while (consumer.TryRead(first.Bytes, SimulationDomain.None, out var record)) read.Add(record);
        Assert.Equal(once.TakeLast((int)Math.Min(header.Capacity, (uint)once.Length)), read);
        Assert.Equal(header.Epoch, consumer.Epoch);
    }

    [Fact]
    public async Task ASavedAndLoadedMachinePublishesIdenticalRecordsAndTheSaveHoldsNoRingBytes()
    {
        var bench = Memory();
        var saved = new WorkshopSavedConstruction(bench, new(7));
        var before = WorkshopSaveCodec.Encode(saved);
        await using var original = await Ring.Running(bench);
        await original.Advance(600);
        var loaded = WorkshopSaveCodec.Decode(SaveFixture.Elements, before);
        Assert.Equal(before, WorkshopSaveCodec.Encode(new WorkshopSavedConstruction(original.Simulation.Construction, new(7))));
        await using var replay = await Ring.Running(loaded.Construction);
        await replay.Advance(600);
        Assert.Equal(original.Records, replay.Records);
    }

    // ---------- capacity: sized from the compiled scene's rows over a 0.5 s horizon ----------

    [Theory]
    [InlineData(SimulationCadence.Hz60, 128u)]   // 3 rows × 30 ticks = 90
    [InlineData(SimulationCadence.Hz120, 256u)]  // 3 rows × 60 ticks = 180
    [InlineData(SimulationCadence.Hz240, 512u)]  // 3 rows × 120 ticks = 360
    public async Task TheRingIsSizedFromTheScenesRowsOverHalfASecond(SimulationCadence cadence, uint capacity)
    {
        await using var ring = await Ring.Building(Periodic(1f, 9, cadence: cadence));
        Assert.Equal([new EventRow(new(1), SimulationEventKind.ElectricalPowered), new EventRow(new(2), SimulationEventKind.TriggerActivated),
            new EventRow(new(3), SimulationEventKind.ThresholdReached)], ring.Device.EventRows!.Rows.OrderBy(row => row.Owner.Value));
        Assert.Equal(capacity, ring.Device.Events.Capacity);
        Assert.Equal(capacity, EventRingHeader.Read(ring.Bytes).Capacity);
        Assert.Equal(EventRingBuffer.ByteLength(capacity), ring.Bytes!.Length);
    }

    [Fact]
    public async Task AnEmptyBenchHasAOneSlotRing()
    {
        await using var ring = await Ring.Building(Bench([], []));
        Assert.Empty(ring.Device.EventRows!.Rows);
        Assert.Equal(1u, EventRingHeader.Read(ring.Bytes).Capacity);
    }

    // ---------- linear sizing and the hard limit (coordinator decision, 24 Oct 2026) ----------

    // N balls and N bumpers: N Impact producers (one per contact-work owner, never one per owner × ball pair), so the ring grows
    // linearly: nextPow2(N × 1 record per tick × 60 ticks) at 120 Hz.
    [Theory]
    [InlineData(1, 64u)]
    [InlineData(10, 1024u)]
    [InlineData(40, 4096u)]
    public async Task NBallsAndNBumpersSizeTheRingLinearlyInProducers(int n, uint capacity)
    {
        var parts = new List<IWorkshopInstance>();
        for (var i = 0; i < n; i++)
        {
            parts.Add(PlacedParts.Bumper(new((ulong)(2 * i + 1)), -60 + 3 * i, 3, 0));
            parts.Add(WorkshopInput.Generic(PartId.Basketball, new((ulong)(2 * i + 2)), -60 + 3 * i, 6, 0, 0, 0, 0, 1));
        }
        await using var ring = await Ring.Building(Bench([.. parts], []));
        Assert.Equal(n, ring.Device.EventRows!.Rows.Count);
        Assert.All(ring.Device.EventRows!.Rows, row => Assert.Equal(SimulationEventKind.Impact, row.Kind));
        Assert.Equal(capacity, ring.Device.Events.Capacity);
        Assert.Equal(capacity, EventRingBuffer.Capacity((ulong)n * 60));
    }

    // The limit: 2^20 slots (64 MiB). It is checked at the fastest cadence (240 Hz, 120 ticks per 0.5 s), so at most 8738 records per
    // tick are admitted at every cadence and 8739 are refused with the typed admission rejection; every admitted ring's byte length is
    // representable.
    [Theory]
    [InlineData(SimulationCadence.Hz60, 1u << 18)]
    [InlineData(SimulationCadence.Hz120, 1u << 19)]
    [InlineData(SimulationCadence.Hz240, 1u << 20)]
    public void TheLimitAdmitsEightThousandSevenHundredThirtyEightRecordsPerTickAndRefusesOneMoreTyped(SimulationCadence cadence, uint capacity)
    {
        Assert.Equal(8738UL, EventRingBuffer.MaximumCapacity / 120);
        Assert.Equal(capacity, EventRecordProducer.RingCapacity(8738, cadence));
        Assert.Equal(64 + (1 << 26), EventRingBuffer.ByteLength(EventRingBuffer.MaximumCapacity));
        Assert.Throws<GpuAdmissionException>(() => EventRecordProducer.RingCapacity(8739, cadence));
        Assert.Throws<ArgumentOutOfRangeException>(() => EventRingBuffer.Capacity(EventRingBuffer.MaximumCapacity + 1UL));
        Assert.Equal(1u, EventRecordProducer.RingCapacity(0, cadence));
    }

    // Through the production admission path: 8738 supply sources (8738 ElectricalPowered producers) are admitted; 8739 are refused as
    // GpuAdmission with the session kept in Building, no fault, and a smaller construction is then admitted.
    [Theory]
    [InlineData(8738, true)]
    [InlineData(8739, false)]
    public async Task AConstructionBeyondTheLimitIsRefusedTypedAndTheSessionStaysBuilding(int producers, bool admitted)
    {
        var sources = Enumerable.Range(0, producers)
            .Select(i => (IWorkshopInstance)PlacedParts.Battery(new((ulong)i + 1), -63 + i % 127, 3, -63 + i / 127)).ToArray();
        await using var ring = await Ring.Initialized(WorkshopCadenceSettings.Default());
        var result = await ring.Simulation.Construct(Bench(sources, []));
        if (admitted)
        {
            Assert.Equal(WorkshopCommandOutcome.Applied, result.Outcome);
            Assert.Equal(producers, ring.Device.EventRows!.Rows.Count);
            Assert.Equal(1u << 19, ring.Device.Events.Capacity);
            return;
        }
        Assert.Equal((WorkshopCommandOutcome.Rejected, WorkshopRejection.GpuAdmission), (result.Outcome, result.Reason));
        Assert.Equal((WorkshopSimulationPhase.Building, WorkshopRejection.None), (ring.Simulation.Phase, ring.Simulation.Fault));
        Assert.Equal(EventRingBuffer.HeaderBytes, ring.Publish().Length); // the empty bench's ring is kept
        Assert.Equal(WorkshopCommandOutcome.Applied, (await ring.Simulation.Construct(Periodic(1f, 9))).Outcome);
        Assert.Equal(256u, ring.Device.Events.Capacity);
    }

    // A burst exceeding the ring: 3588 committed ticks (299 pulses and one threshold) published at once (as when no read follows them). Every tick commits; the
    // publication advances the sequence by every record, writes the newest 256 and counts the rest in Lapped; the consumer reads the
    // newest 256 and reports the lap. A control run publishing every tick produces the same records.
    [Fact]
    public async Task ABurstLargerThanTheRingCommitsEveryTickAndCountsTheExcessInLapped()
    {
        await using var control = await Ring.Running(Periodic(.1f, 9));
        await control.Advance(3588);
        await using var burst = await Ring.Running(Periodic(.1f, 9));
        var publication = await burst.AdvanceUndrained(3588);
        Assert.Equal(3588UL, burst.Simulation.Committed.Tick.Value);
        var all = control.Records;
        Assert.True(all.Count > 256, $"{all.Count} records");
        var header = EventRingHeader.Read(publication);
        Assert.Equal((256u, (ulong)all.Count, (ulong)all.Count - 256), (header.Capacity, header.WriteSequence, header.Lapped));
        Assert.Equal(all.TakeLast(256), Decode(publication));
        var consumer = new EventRingConsumer();
        var read = new List<SimulationEventRecord>();
        while (consumer.TryRead(burst.Bytes, SimulationDomain.None, out var record)) read.Add(record);
        Assert.Equal(all.TakeLast(256), read);
        Assert.Equal((ulong)all.Count - 256, consumer.Lapped);
        // The run continues: the next tick's records follow the burst's sequence.
        await burst.Advance(12);
        Assert.Equal((ulong)all.Count + 1, burst.Records[^1].Sequence);
    }

    // ---------- overflow: the tick commits, the sequence advances by the whole batch, Lapped counts the excess ----------

    [Fact]
    public void ABatchLargerThanTheRingAdvancesTheSequenceCountsLappedAndTheConsumerReportsTheLap()
    {
        var outbox = new EventRingOutbox();
        outbox.Restart(new(5), 4);
        var batch = Enumerable.Range(1, 6).Select(i => new SimulationEventRecord(0, 9, SimulationEventKind.Impact, 1, 2, (uint)i, 0f, 1f, (uint)i, 0))
            .ToArray();
        outbox.Commit(batch);
        var publication = outbox.Drain();
        var header = EventRingHeader.Read(publication);
        Assert.Equal((4u, 5UL, 6UL, 2UL), (header.Capacity, header.Epoch, header.WriteSequence, header.Lapped));
        Assert.Equal([3UL, 4UL, 5UL, 6UL], Decode(publication).Select(record => record.Sequence));
        var ring = EventRingBuffer.Append(null, publication);
        var consumer = new EventRingConsumer();
        var read = new List<ulong>();
        while (consumer.TryRead(ring, SimulationDomain.Mechanical, out var record)) read.Add(record.Sequence);
        Assert.Equal([3UL, 4UL, 5UL, 6UL], read);
        Assert.Equal(2UL, consumer.Lapped);
        Assert.Equal(2UL, EventRingHeader.Read(ring).Lapped);
        // The next tick continues the sequence; nothing is re-published.
        outbox.Commit(batch.AsSpan(0, 1));
        Assert.Equal([7UL], Decode(outbox.Drain()).Select(record => record.Sequence));
        Assert.Empty(Decode(outbox.Drain()));
    }

    // ---------- round trip through the shipped worker's writeEventRing ----------

    private const string WorkerScript = """
        import vm from 'node:vm';
        import fs from 'node:fs/promises';
        let text = ''; for await (const chunk of process.stdin) text += chunk;
        const request = JSON.parse(text);
        const source = await fs.readFile(new URL(process.env.WORKER_URL), 'utf8');
        const rings = [];
        class Channel { constructor() { this.port1 = { close() {} }; this.port2 = { postMessage() {}, close() {} }; } }
        const context = vm.createContext({ self: { postMessage(message) { if (message?.eventRing) rings.push(message.eventRing); } }, console,
            Uint8Array, Uint32Array, Float32Array, Float64Array, ArrayBuffer, SharedArrayBuffer, BigInt64Array, BigUint64Array, DataView, Atomics,
            MessageChannel: Channel });
        const host = { ScheduleRoles: () => [1, 2] };
        const runtime = { setModuleImports() {}, getAssemblyExports: async () => ({ CuriousContraptions: { Simulation: { Program: host } } }),
            getConfig: () => ({ mainAssemblyName: 'EventRingRoundTrip' }) };
        const worker = new vm.SourceTextModule(source + '\nexport { writeEventRing };', { context });
        await worker.link(async name => {
            const names = name.includes('dotnet') ? ['dotnet'] : name.includes('clock-ring')
                ? ['attachClockRing', 'deriveExchange', 'qualifyDerivation', 'writeDerivation', 'beat'] : ['admitNativeClock', 'nativeNow', 'nativeClockEvidence'];
            return new vm.SyntheticModule(names, function () {
                for (const name of names) this.setExport(name, name === 'dotnet' ? { create: async () => runtime }
                    : name === 'admitNativeClock' ? async () => {} : () => { throw new Error('unused'); });
            }, { context });
        });
        await worker.evaluate();
        for (const hex of request.batches) worker.namespace.writeEventRing(Uint8Array.from(Buffer.from(hex, 'hex')));
        process.stdout.write(JSON.stringify({ rings: rings.length, ring: Buffer.from(new Uint8Array(rings.at(-1))).toString('hex') }));
        """;

    private static (int Rings, byte[] Ring) RunWorker(IReadOnlyList<byte[]> batches)
    {
        var path = Path.Combine(twodog.Engine.ResolveProjectDir(), "CuriousContraptions.Simulation/wwwroot/worker.js");
        var start = new ProcessStartInfo("node") { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        start.ArgumentList.Add("--experimental-vm-modules"); start.ArgumentList.Add("--no-warnings");
        start.ArgumentList.Add("--input-type=module"); start.ArgumentList.Add("-e"); start.ArgumentList.Add(WorkerScript);
        start.Environment["WORKER_URL"] = new Uri(path).AbsoluteUri;
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
    public async Task TheShippedWorkerWritesTheSameRingAsTheReferenceForARealRunAndAnOverflow()
    {
        await using var ring = await Ring.Running(Memory());
        await ring.Advance(600);
        await ring.Reset();
        await ring.Run();
        await ring.Advance(250);
        var outbox = new EventRingOutbox();
        outbox.Restart(new(99), 4);
        outbox.Commit(ring.Records.Take(6).ToArray());
        var batches = ring.Publications.Append(outbox.Drain()).ToList();
        byte[]? reference = null;
        var fresh = 0;
        foreach (var batch in batches.Where(batch => batch.Length != 0))
        {
            var next = EventRingBuffer.Append(reference, batch);
            if (!ReferenceEquals(next, reference)) fresh++;
            reference = next;
        }
        var (rings, written) = RunWorker(batches);
        Assert.Equal(fresh, rings);
        Assert.Equal(reference, written);
        Assert.Equal(new EventRingHeader(4, 64, 99, 6, 2), EventRingHeader.Read(written));
        var consumer = new EventRingConsumer();
        var read = new List<SimulationEventRecord>();
        while (consumer.TryRead(written, SimulationDomain.None, out var record)) read.Add(record);
        Assert.Equal(ring.Records.Take(6).Skip(2).Select((record, i) => record with { Sequence = (ulong)i + 3 }), read);
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
            await using var ring = await Ring.Building(Bench(parts, links));
            var produced = ring.Device.EventRows!.Rows.Where(row => row.Owner == part.Id).Select(row => row.Kind).Distinct().Order().ToArray();
            var declared = entry.Events.Select(descriptor => descriptor.Kind).Order().ToArray();
            Assert.True(declared.SequenceEqual(produced),
                $"{entry.Metadata.Id}: declares [{string.Join(", ", declared)}], its compiled rows produce [{string.Join(", ", produced)}]");
            // Review F3: the wired lamp conforms in each of its wirings: a level indicator (fed by a memory or a load level) and a
            // first-input indicator (fed by a pulse) both declare and produce nothing.
            if (links.Length == 0) continue;
            lampKinds.Add(WorkshopActivationCompiler.Compile(Bench(parts, links)).Nodes.ToArray().Single(node => node.Owner == new GpuBodyId(3)).Kind);
            Assert.True(ring.Device.EventRows!.Rows.Where(row => row.Owner == new GpuBodyId(3)).Select(row => row.Kind)
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
            .ToArray();
        Assert.Contains("engine/gpu/EventRecordProducer.cs", sources);
        string[] Hits(string pattern) => sources.Where(path => File.ReadAllText(Path.Combine(root, path)).Contains(pattern, StringComparison.Ordinal)).Order().ToArray();
        Assert.Equal(["CuriousContraptions.Simulation/WorkshopGpuDevice.cs", "engine/gpu/EventRecordProducer.cs"], Hits("EventRingOutbox"));
        Assert.Empty(Hits("EventRingBuffer.Append")); // the C# reference writer is the tests' only; production publishes through the worker
        // The worker defines and calls it; the C# reference names it as its twin.
        Assert.Equal(["CuriousContraptions.Abstractions/EventRingBuffer.cs", "CuriousContraptions.Simulation/wwwroot/worker.js"], Hits("writeEventRing"));
        Assert.Equal(["CuriousContraptions.Simulation/Program.cs", "CuriousContraptions.Simulation/WorkshopGpuDevice.cs"], Hits("DrainEvents"));
        foreach (var retired in new[] { "readEventRing", "createEventConsumer", "WorkshopEventRing", "eventBuffer" }) Assert.Empty(Hits(retired));
        // Story 9A.23 + 9A.25b: the occurrence-carrying reliable reads, the main-thread one-shot relay and the one-shot relay drivers.
        foreach (var retired in new[] { "occurrenceReads", "reserveOccurrenceRead", "releaseOccurrenceRead", "retainOccurrence", "RequiresReliableRead",
            "ContactWorkOccurrenceRecord", "PumpCosmetics", "PumpCapture", "PumpActivations", "PumpContactFeedback", "ObserveContactFeedback",
            "TryCosmeticFrame", "CosmeticCurveDeclaration", "AnimationFeedbackSource", "CosmeticBindingSet", "WorkshopCosmeticSample", "IsOneShot",
            "AnimationControlKind.Impulse", "AnimationControlKind.Transition", "ActivationPulse", "ContactImpulse", "CapturePulse" })
            Assert.Empty(Hits(retired));
        // One shared event ring reader, imported by both consumers (the C# reference reader names it as its twin).
        Assert.Equal(["CuriousContraptions.Abstractions/EventRingBuffer.cs", "CuriousContraptions.Animation.Worker/wwwroot/worker.js",
            "CuriousContraptions.web/wwwroot/event-ring-cursor.js",
            "CuriousContraptions.web/wwwroot/workshop-client.js"], Hits("EventRingCursor"));
    }
}
