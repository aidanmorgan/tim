using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>The C-Memory suite-first guards (Stories 9A.41 and 9A.43), recorded on the record path at fc7f7cac and unchanged after the
/// conversion: only <see cref="PlacedParts"/> builds the counter and the latch. Every case runs the production compile, the worker
/// engine and tick progression; ring records are drained after every command and tick exactly as the worker does
/// (<see cref="WorkshopIntegrationHarness"/>), and every expectation is derived from the same run's committed reads or from the
/// declared pulse schedule, never from the records themselves.</summary>
public sealed class MemoryGuardTests
{
    private static readonly double Floor = (double)WorkshopConstruction.WorkbenchSurface.Value;
    private static readonly double Half45 = Math.Sqrt(.5);

    private static WorkshopConnection Signal(GpuBodyId source, GpuBodyId target, WorkshopSocket input = WorkshopSocket.ActivationIn) =>
        new(source, WorkshopSocket.ActivationOut, target, input, WorkshopConnectionDomain.Activation);
    private static WorkshopConnection Supply(GpuBodyId source, GpuBodyId target) =>
        new(source, WorkshopSocket.Supply, target, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);
    private static WorkshopConstruction Bench(IWorkshopInstance[] instances, params WorkshopConnection[] links) =>
        new(new(2), WorkshopCadenceSettings.Default(), new(instances), Connections: new(links));

    private static ActivationLatch Node(WorkshopRead read, GpuBodyId owner)
    {
        for (var i = 0; i < read.Activations.Count; i++) if (read.Activations[i].Owner == owner) return read.Activations[i];
        throw new InvalidOperationException($"No activation node for {owner.Value}.");
    }
    private static bool Lit(WorkshopRead read, GpuBodyId owner) => Node(read, owner).Phase == ActivationPhase.Latched;
    private static double Height(WorkshopRead read, GpuBodyId body)
    {
        Assert.True(read.Bodies.TryGet(body, out var row));
        return (row.Body.Cell.Y + (double)row.Body.Local.Y) / 16;
    }

    // Both packages bake their housing, foot and proud parts (the Counter's face, nine dot lamps and three sockets; the Latch's face,
    // indicator lamp and five sockets) from the scene into the bundle, so the declaration alone gives each body its colliders (the
    // generic decoder admits only an element with colliders).
    [Fact]
    public void TheCounterAndTheLatchDeclareTheirBakedColliders()
    {
        Assert.Equal(15, DeclaredPhysics.Of(new PartId("counter")).Colliders.Count);
        Assert.Equal(9, DeclaredPhysics.Of(new PartId("latch")).Colliders.Count);
        Assert.Equal([15, 9], new[] { new PartId("counter"), new PartId("latch") }.Select(part => DeclaredPhysics.Colliders(part).Count));
    }

    // ---------- Count: three balls through one detector ----------

    // The detector's crossing plane is 2 m above the bench with local +X down (forward is downward); three Basketballs fall through
    // it one after another (2 m apart) onto a Ramp tilted 20° that carries each one away down the slope, so each ball crosses exactly
    // once. The counter (target N) lights a first-input lamp with its emission, and a battery supplies its switched contact.
    private const double Plane = 2;
    private static readonly GpuBodyId Detector = new(1), Counter = new(2), CounterLamp = new(3), Ramp = new(4), Battery = new(8);
    private static readonly GpuBodyId[] Balls = [new(5), new(6), new(7)];

    internal static WorkshopConstruction ThreeBalls(int target)
    {
        var tilt = Math.PI / 180 * -20 / 2;
        return Bench([
                PlacedParts.Detector(Detector, 0, (float)(Floor + Plane), 0, 0, 0, (float)(-Half45), (float)(Half45)),
                PlacedParts.Counter(Counter, -4, 1, 0, 0, 0, 0, 1, target),
                PlacedParts.Lamp(CounterLamp, -4, 1, 3, 0, 0, 0, 1),
                WorkshopInput.Generic(new PartId("ramp"), Ramp, 1, (float)(Floor + .6), 0, 0, 0, (float)(Math.Sin(tilt)), (float)(Math.Cos(tilt))),
                WorkshopInput.Generic(new PartId("ball"), Balls[0], 0, (float)(Floor + Plane + .5), 0, 0, 0, 0, 1),
                WorkshopInput.Generic(new PartId("ball"), Balls[1], 0, (float)(Floor + Plane + 2.5), 0, 0, 0, 0, 1),
                WorkshopInput.Generic(new PartId("ball"), Balls[2], 0, (float)(Floor + Plane + 4.5), 0, 0, 0, 0, 1),
                PlacedParts.Battery(Battery, -7, 1, 0)],
            Signal(Detector, Counter), Signal(Counter, CounterLamp), Supply(Battery, Counter));
    }

    /// <summary>The ticks at which the counter's committed count rose, each by exactly one.</summary>
    private static List<ulong> Increments(ActivationNetwork network, IReadOnlyList<WorkshopRead> reads, GpuBodyId owner)
    {
        var ticks = new List<ulong>();
        var count = 0u;
        foreach (var read in reads)
        {
            var row = network.Memory(read.Memory, owner);
            Assert.True(row.Count == count || row.Count == count + 1, $"one input at most per tick at {read.Tick.Value}");
            if (row.Count == count + 1) ticks.Add(read.Tick.Value);
            count = row.Count;
        }
        return ticks;
    }

    // 9A.41 AC: with N = 3 the third crossing emits once (one ThresholdReached, Count 3, Source the counter) and two crossings emit
    // nothing; N = 1 emits on the first crossing; N = 9 never emits with three crossings (control). The committed count equals the
    // crossings whatever the target (9R.1). The first-input lamp lights with the emission and the switched contact closes at it.
    [Theory]
    [InlineData(3, 3)]
    [InlineData(1, 1)]
    [InlineData(9, 0)]
    public async Task ThreeBallsThroughTheDetectorEmitOnceOnTheTargetCrossingOnly(int target, int emittingCrossing)
    {
        var bench = ThreeBalls(target);
        var network = WorkshopActivationCompiler.Compile(bench);
        await using var ring = await WorkshopIntegrationHarness.Running(bench);
        await ring.Advance(600);
        var reads = ring.Reads;

        // Three crossings, each by a different ball: at the k-th count exactly k balls are below the plane.
        var crossings = Increments(network, reads, Counter);
        Assert.Equal(3, crossings.Count);
        for (var k = 0; k < 3; k++)
        {
            var read = reads.Single(r => r.Tick.Value == crossings[k]);
            Assert.Equal(k + 1, Balls.Count(ball => Height(read, ball) < Floor + Plane));
        }
        Assert.Equal(3u, network.Memory(reads[^1].Memory, Counter).Count);

        var thresholds = ring.Of(SimulationEventKind.ThresholdReached).ToArray();
        if (emittingCrossing == 0)
        {
            Assert.Empty(thresholds);
            Assert.All(reads, read => Assert.False(Lit(read, Counter) || Lit(read, CounterLamp) ||
                network.ContactClosed(read.Activations, read.Timers, Counter), $"nothing emitted at {read.Tick.Value}"));
            return;
        }
        var emitted = crossings[emittingCrossing - 1];
        var record = Assert.Single(thresholds);
        // The record names the counter and, as its cause, the ball whose crossing reached the target.
        Assert.Equal((emitted, Counter.Value, (uint)target, Balls[emittingCrossing - 1].Value), (record.Tick, record.Source, record.Count, record.Other));
        Assert.Equal(Node(reads.Single(r => r.Tick.Value == emitted), Counter).EventOrdinal, record.Ordinal);
        Assert.All(reads, read =>
        {
            var on = read.Tick.Value >= emitted;
            Assert.Equal(on, Lit(read, Counter));
            Assert.Equal(on, Lit(read, CounterLamp));
            Assert.Equal(on, network.ContactClosed(read.Activations, read.Timers, Counter));
        });
    }

    // N = 9 on a 1 s clock: the ninth pulse (tick 1080) emits once and the eighth nothing; N = 1 emits on the first pulse. Past the
    // target the count keeps counting (twelve pulses: 12) and nothing more is emitted.
    [Theory]
    [InlineData(9)]
    [InlineData(1)]
    public async Task APulsedTargetEmitsOnItsOwnPulseAndNeverAfterSaturation(int target)
    {
        GpuBodyId battery = new(1), clock = new(2), tally = new(3);
        var bench = Bench([PlacedParts.Battery(battery, -3, 3, 0), PlacedParts.Clock(clock, 0, 3, 0, 0, 0, 0, 1, 1f),
            PlacedParts.Counter(tally, 3, 3, 0, 0, 0, 0, 1, target)], Supply(battery, clock), Signal(clock, tally));
        var network = WorkshopActivationCompiler.Compile(bench);
        await using var ring = await WorkshopIntegrationHarness.Running(bench);
        await ring.Advance(1500);
        Assert.Equal(Enumerable.Range(1, 12).Select(pulse => 120UL * (ulong)pulse), Increments(network, ring.Reads, tally));
        Assert.Equal(12u, network.Memory(ring.Reads[^1].Memory, tally).Count);
        var record = Assert.Single(ring.Of(SimulationEventKind.ThresholdReached));
        Assert.Equal((120UL * (ulong)target, tally.Value, (uint)target), (record.Tick, record.Source, record.Count));
        Assert.All(ring.Reads, read => Assert.Equal(read.Tick.Value >= 120UL * (ulong)target, Lit(read, tally)));
    }

    // The counter law evaluated twice from one committed checkpoint with the same inputs gives the same candidate (no hidden state).
    [Fact]
    public void ConsumingTheSameCheckpointTwiceIsIdentical()
    {
        var bench = ThreeBalls(3);
        var network = WorkshopActivationCompiler.Compile(bench);
        var sensor = bench.Instances.Identities[Detector].Passage;
        PassageSensorRead Pass(uint ordinal) => new(sensor, Detector, Balls[0], new(11), PassageSensorPhase.Fired, ordinal, 0f, new(2f));
        var committed = network.Consume(network.Clear(), network.ClearTimers(), [], new(1), [], [Pass(4)], memory: network.ClearMemory());
        committed = network.Consume(committed.Activations, committed.Timers, [], new(2), [], [Pass(8)], memory: committed.Memory);
        var first = network.Consume(committed.Activations, committed.Timers, [], new(3), [], [Pass(12)], memory: committed.Memory);
        var second = network.Consume(committed.Activations, committed.Timers, [], new(3), [], [Pass(12)], memory: committed.Memory);
        Assert.Equal(3u, network.Memory(first.Memory, Counter).Count);
        Assert.Equal(network.Memory(first.Memory, Counter), network.Memory(second.Memory, Counter));
        Assert.Equal(Enumerable.Range(0, first.Activations.Count).Select(i => first.Activations[i]),
            Enumerable.Range(0, second.Activations.Count).Select(i => second.Activations[i]));
        Assert.Equal(2u, network.Memory(committed.Memory, Counter).Count);
    }

    // Run/Reset and Save/Load: Reset restores the built state exactly (count 0, nothing latched, contact open) and restarts the ring
    // (nothing pending, sequence 0, a new epoch); the next run publishes identical records from sequence 1. The save holds no count:
    // the run preserves the decoded authored construction and allocator, and loading replays the same records.
    [Fact]
    public async Task ResetIsExactAndASavedCounterReplaysIdentically()
    {
        var bench = ThreeBalls(3);
        var network = WorkshopActivationCompiler.Compile(bench);
        var (built, reads, reset) = await WorkshopIntegrationHarness.ObserveReset(bench, 600);
        Assert.Equal(3u, network.Memory(reads[^1].Memory, Counter).Count);
        NoFixedCapacityTests.AssertSameState(built, reset, "Reset");
        Assert.Equal(0u, network.Memory(reset.Memory, Counter).Count);
        Assert.False(network.ContactClosed(reset.Activations, reset.Timers, Counter));

        await using var ring = await WorkshopIntegrationHarness.Running(bench);
        await ring.Advance(600);
        var once = ring.Records.ToArray();
        Assert.Single(once, record => record.Kind == SimulationEventKind.ThresholdReached);
        var epoch = ring.Device.Events.Epoch.Value;
        await ring.Reset();
        Assert.Equal((0, 0UL), (ring.Device.Events.Pending.Count, ring.Device.Events.WriteSequence));
        Assert.True(ring.Device.Events.Epoch.Value > epoch);
        ring.Records.Clear();
        await ring.Run();
        await ring.Advance(600);
        Assert.Equal(Counter.Value, Assert.Single(ring.Records, record => record.Kind == SimulationEventKind.ThresholdReached).Source);
        Assert.Equal(3u, network.Memory(ring.Committed.Memory, Counter).Count);
        Assert.Equal(1UL, ring.Records[0].Sequence);

        var saved = new WorkshopSavedConstruction(bench, new(9));
        var bytes = WorkshopSaveCodec.Encode(saved);
        var afterRun = WorkshopSaveCodec.Decode(SaveFixture.Elements,
            WorkshopSaveCodec.Encode(new WorkshopSavedConstruction(ring.Construction, new(9))));
        Assert.Equal(saved, afterRun);
        var loaded = WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes);
        Assert.Equal(saved, loaded);
        await using var replay = await WorkshopIntegrationHarness.Running(loaded.Construction);
        await replay.Advance(600);
        Assert.Equal(Counter.Value, Assert.Single(replay.Records, record => record.Kind == SimulationEventKind.ThresholdReached).Source);
        Assert.Equal(3u, network.Memory(replay.Committed.Memory, Counter).Count);
    }

    // ---------- Memory: Set/Reset, same-tick dominance, one source on both inputs, the level output ----------

    private static readonly GpuBodyId SetClock = new(1), ResetClock = new(2), Latch = new(3), LatchBattery = new(4), LevelLamp = new(5);

    // Set clock 1 s and reset clock 1.5 s → latch (supplied) → a lamp on its level output. Set pulses at 120k, Reset at 180k; at 360 both
    // arrive together and Reset dominates. The latch memory, its level ActivationOut (the lamp), its switched contact and the
    // MemoryChanged records all follow the schedule derived here.
    [Fact]
    public async Task SetTurnsOnResetTurnsOffAndASameTickPairResolvesToReset()
    {
        var bench = Bench([PlacedParts.Clock(SetClock, -3, 3, 0, 0, 0, 0, 1, 1f), PlacedParts.Clock(ResetClock, -3, 3, 3, 0, 0, 0, 1, 1.5f),
                PlacedParts.Latch(Latch, 0, 3, 0, 0, 0, 0, 1), PlacedParts.Battery(LatchBattery, -6, 3, 0), PlacedParts.Lamp(LevelLamp, 3, 3, 0, 0, 0, 0, 1)],
            Supply(LatchBattery, SetClock), Supply(LatchBattery, ResetClock), Signal(SetClock, Latch, WorkshopSocket.SetIn),
            Signal(ResetClock, Latch, WorkshopSocket.ResetIn), Signal(Latch, LevelLamp));
        var network = WorkshopActivationCompiler.Compile(bench);
        await using var ring = await WorkshopIntegrationHarness.Running(bench);
        await ring.Advance(720);
        var on = false;
        var edges = new List<(ulong Tick, uint Level)>();
        foreach (var read in ring.Reads)
        {
            var tick = read.Tick.Value;
            var next = tick > 0 && tick % 180 == 0 ? false : tick > 0 && tick % 120 == 0 ? true : on;
            if (next != on) edges.Add((tick, next ? 1u : 0u));
            on = next;
            Assert.Equal(on ? SimulationLatchPhase.On : SimulationLatchPhase.Off, network.StateLatchPhase(read.Activations, Latch));
            Assert.Equal(on, Lit(read, LevelLamp));
            Assert.Equal(on, network.ContactClosed(read.Activations, read.Timers, Latch));
        }
        Assert.Contains((360UL, 0u), edges);
        var memory = ring.Of(SimulationEventKind.MemoryChanged).ToArray();
        Assert.Equal(edges, memory.Select(record => (record.Tick, record.Level)));
        Assert.All(memory, record => Assert.Equal(Latch.Value, record.Source));
    }

    // One clock wired to both SetIn and ResetIn: every pulse is a same-tick Set+Reset while Off, so memory never turns On, no
    // MemoryChanged is published and the level lamp stays dark; the pulses themselves still arrive (one trigger each).
    [Fact]
    public async Task OneSourceOnBothInputsAlwaysResolvesToReset()
    {
        var bench = Bench([PlacedParts.Clock(SetClock, -3, 3, 0, 0, 0, 0, 1, 1f), PlacedParts.Latch(Latch, 0, 3, 0, 0, 0, 0, 1),
                PlacedParts.Battery(LatchBattery, -6, 3, 0), PlacedParts.Lamp(LevelLamp, 3, 3, 0, 0, 0, 0, 1)],
            Supply(LatchBattery, SetClock), Signal(SetClock, Latch, WorkshopSocket.SetIn), Signal(SetClock, Latch, WorkshopSocket.ResetIn),
            Signal(Latch, LevelLamp));
        var network = WorkshopActivationCompiler.Compile(bench);
        await using var ring = await WorkshopIntegrationHarness.Running(bench);
        await ring.Advance(600);
        Assert.Equal([120UL, 240UL, 360UL, 480UL, 600UL], ring.Of(SimulationEventKind.TriggerActivated).Select(record => record.Tick));
        Assert.Empty(ring.Of(SimulationEventKind.MemoryChanged));
        Assert.All(ring.Reads, read =>
        {
            Assert.Equal(SimulationLatchPhase.Off, network.StateLatchPhase(read.Activations, Latch));
            Assert.False(Lit(read, LevelLamp));
            Assert.False(network.ContactClosed(read.Activations, read.Timers, Latch));
        });
    }

    // The latch's ActivationOut is a level: only an input that admits a level (a declared indicator) may follow it. Every occurrence
    // consumer refuses the link with the typed reason, and the lamp admits it (control).
    [Fact]
    public void EveryOccurrenceConsumerRefusesTheLevelOutput()
    {
        var latch = PlacedParts.Latch(new(1), 0, 0, 0, 0, 0, 0, 1);
        var other = PlacedParts.Latch(new(2), 0, 3, 0, 0, 0, 0, 1);
        var counter = PlacedParts.Counter(new(3), 3, 0, 0, 0, 0, 0, 1, 3);
        var delay = WorkshopInput.Generic(new PartId("delay"), new(4), 3, 3, 0, 0, 0, 0, 1);
        var lamp = PlacedParts.Lamp(new(5), -3, 0, 0, 0, 0, 0, 1);
        var instances = new WorkshopInstances(latch, other, counter, delay, lamp);
        new WorkshopConnections(Signal(latch.Id, lamp.Id)).Validate(instances);
        foreach (var link in new[] { Signal(latch.Id, counter.Id), Signal(latch.Id, delay.Id), Signal(latch.Id, other.Id, WorkshopSocket.SetIn),
                     Signal(latch.Id, other.Id, WorkshopSocket.ResetIn) })
            EditRefusal.Is(WorkshopEditRejection.LevelDrivesLevelInputOnly, () => new WorkshopConnections(link).Validate(instances));
    }

    // ---------- 9A.43 "Clear" admission: a committed Latched→Clear flip is admitted only for a bistable (level) node ----------

    /// <summary>The production device with one armed read change on the next Advance (the change is applied to the real candidate).</summary>
    private sealed class FlipDevice : IWorkshopGpuDevice
    {
        private readonly WorkshopGpuDevice _inner = new(new CuriousContraptions.Simulation.BrowserGpuTransport(new CuriousContraptions.Abstractions.EventRingPublication()), new(101, 206));
        public Func<WorkshopRead, WorkshopRead>? Next { get; set; }
        public ValueTask<WorkshopGpuCandidate> Admit(WorkshopCompiledScene construction, SimulationEpoch epoch, WorkshopGpuProfile profile, CommandSequence sequence) =>
            _inner.Admit(construction, epoch, profile, sequence);
        public async ValueTask<WorkshopGpuCandidate> Advance(WorkshopRead committed, CommandSequence sequence, WorkshopTickControls controls)
        {
            var candidate = await _inner.Advance(committed, sequence, controls);
            if (Next is not { } change) return candidate;
            Next = null;
            return new(candidate.Sequence, change(candidate.Read));
        }
        public void Commit(CommandSequence sequence) => _inner.Commit(sequence);
        public void Discard(CommandSequence sequence) => _inner.Discard(sequence);
        public ValueTask DisposeAsync() => _inner.DisposeAsync();
    }

    public enum Flipped { Switch = 1, StickyLamp = 2, Delay = 3, Tally = 4, Memory = 5, LevelLamp = 6, Plate = 7, Passage = 8, Oscillator = 9, Unknown = 99 }

    // A ball strikes a switch that feeds a first-input lamp, a Delay (a latching timer), a counter of target 1 and a latch's SetIn; the
    // latch drives a lamp through its level output; a ball rests on a plate; a ball falls through a detector; a supplied clock pulses.
    // By tick 300 every node holds Latched (asserted), each compiled from its declaration to the node kind named here.
    internal static WorkshopConstruction AdmissionBench()
    {
        var plateY = Floor + .22;
        return Bench([
                WorkshopInput.Generic(new PartId("switch"), new(1), 0, 1, 0, 0, 0, 0, 1),
                PlacedParts.Lamp(new(2), 4, 1, 0, 0, 0, 0, 1),
                WorkshopInput.Generic(new PartId("delay"), new(3), 4, 1, 3, 0, 0, 0, 1),
                PlacedParts.Counter(new(4), -4, 1, 0, 0, 0, 0, 1, 1),
                PlacedParts.Latch(new(5), -4, 1, 3, 0, 0, 0, 1),
                PlacedParts.Lamp(new(6), -4, 1, 6, 0, 0, 0, 1),
                PlacedParts.Plate(new(7), -12, (float)(plateY), 0, .5f),
                PlacedParts.Detector(new(8), 12, (float)(Floor + .62), 0, 0, 0, (float)(-Half45), (float)(Half45)),
                PlacedParts.Clock(new(9), 0, 3, 8, 0, 0, 0, 1, 1f),
                PlacedParts.Battery(new(10), -3, 3, 8),
                WorkshopInput.Generic(new PartId("ball"), new(20), 0, 3, 0, 0, 0, 0, 1),
                WorkshopInput.Generic(new PartId("ball"), new(21), -12, (float)(plateY + .08 + .345 + 2), 0, 0, 0, 0, 1),
                WorkshopInput.Generic(new PartId("ball"), new(22), 12, (float)(Floor + .34 + 3), 0, 0, 0, 0, 1)],
            Signal(new(1), new(2)), Signal(new(1), new(3)), Signal(new(1), new(4)), Signal(new(1), new(5), WorkshopSocket.SetIn),
            Signal(new(5), new(6)), Supply(new(10), new(9)));
    }

    [Theory]
    [InlineData(Flipped.Memory, ActivationNodeKind.StateLatch, true)]
    [InlineData(Flipped.LevelLamp, ActivationNodeKind.LevelIndicator, true)]
    [InlineData(Flipped.Plate, ActivationNodeKind.ContactLoadSource, true)]
    [InlineData(Flipped.Switch, ActivationNodeKind.ContactSource, false)]
    [InlineData(Flipped.StickyLamp, ActivationNodeKind.StickyFirstInput, false)]
    [InlineData(Flipped.Delay, ActivationNodeKind.Timer, false)]
    [InlineData(Flipped.Tally, ActivationNodeKind.Counter, false)]
    [InlineData(Flipped.Passage, ActivationNodeKind.PassageSource, false)]
    [InlineData(Flipped.Oscillator, ActivationNodeKind.Oscillator, false)]
    public async Task ALatchedToClearFlipIsAdmittedOnlyForABistableNode(Flipped owner, ActivationNodeKind kind, bool admitted)
    {
        var bench = AdmissionBench();
        var target = new GpuBodyId((ulong)owner);
        Assert.Equal(kind, WorkshopActivationCompiler.Compile(bench).Nodes.ToArray().Single(node => node.Owner == target).Kind);
        var (simulation, device) = await Admitted(bench);
        await using var _ = simulation;
        var before = simulation.Committed;
        Assert.True(Lit(before, target), $"{owner} holds Latched before the flip");
        device.Next = read => Cleared(read, target, keepNode: true);
        var result = await simulation.Advance();
        if (admitted)
        {
            Assert.Equal(WorkshopCommandOutcome.Applied, result.Outcome);
            Assert.False(Lit(simulation.Committed, target));
            return;
        }
        Assert.Equal((WorkshopCommandOutcome.Faulted, WorkshopRejection.InvalidRead), (result.Outcome, result.Reason));
        Assert.Equal(before, simulation.Committed);
    }

    // An activation row naming a node the construction does not compile is refused, even on a bistable owner's slot.
    [Fact]
    public async Task AFlipNamingAnUnknownNodeIsRefused()
    {
        var (simulation, device) = await Admitted(AdmissionBench());
        await using var _ = simulation;
        var before = simulation.Committed;
        device.Next = read => Cleared(read, new((ulong)Flipped.Memory), keepNode: false);
        var result = await simulation.Advance();
        Assert.Equal((WorkshopCommandOutcome.Faulted, WorkshopRejection.InvalidRead), (result.Outcome, result.Reason));
        Assert.Equal(before, simulation.Committed);
    }

    private static WorkshopRead Cleared(WorkshopRead read, GpuBodyId owner, bool keepNode)
    {
        var values = Enumerable.Range(0, read.Activations.Count).Select(i => read.Activations[i]).ToArray();
        var index = Array.FindIndex(values, value => value.Owner == owner);
        var node = keepNode ? values[index].Node : new ActivationNodeId((ulong)Flipped.Unknown);
        values[index] = new(node, owner, ActivationPhase.Clear, default, default, default, 0, 0f, new(0f));
        return read with { Activations = new(values) };
    }

    private static async Task<(WorkshopSimulation Simulation, FlipDevice Device)> Admitted(WorkshopConstruction bench)
    {
        var device = new FlipDevice();
        var simulation = new WorkshopSimulation(device, new WorkshopPhysicsEngineTests.StepClock(), CompiledSceneFixture.Compile(new(new(1), bench.Settings, WorkshopInstances.Empty)), new(1),
            new WorkshopPhysicsEngineTests.NoInstallation(), GpuDeviceOwnership.Owned);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Initialize()).Outcome);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Construct(CompiledSceneFixture.Compile(bench))).Outcome);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Run()).Outcome);
        for (var tick = 0; tick < 300; tick++) Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Advance()).Outcome);
        foreach (var owner in Enum.GetValues<Flipped>().Where(value => value != Flipped.Unknown))
            Assert.True(Lit(simulation.Committed, new((ulong)owner)), $"{owner} is Latched by tick 300");
        return (simulation, device);
    }
}
