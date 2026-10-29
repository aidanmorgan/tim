using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

/// <summary>C-Clock suite-first guards (9A.42, 9R.10, 9.3; CAT-017 §4–§5). Written on the record path (base 14658883) and run unchanged on
/// the declared generic instance: every clock is built through <see cref="PlacedParts.Clock"/>, and every expectation reads the production
/// compile, Consume, Advance, Pause, Reset, save codec and declared presentation path (committed memory rows, activation latches and
/// presentation samples), never a record property. Tick counts are stated literally: ceil(seconds × rate), the product taken in f32.</summary>
public sealed class ClockGuardTests
{
    private static readonly GpuBodyId Source = new(1), Owner = new(2), Counter = new(3);

    private static WorkshopConnection Supply(GpuBodyId source, GpuBodyId target) =>
        new(source, WorkshopSocket.Supply, target, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);
    private static WorkshopConnection Signal(GpuBodyId source, GpuBodyId target) =>
        new(source, WorkshopSocket.ActivationOut, target, WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation);

    /// <summary>Battery (1) → clock (2) of an interval, and the clock's ActivationOut → a counter (3, target 9). Unwired leaves only the
    /// signal link.</summary>
    private static WorkshopConstruction Bench(float seconds, bool wired = true, ElectricalEnable enabled = ElectricalEnable.Enabled,
        SimulationCadence cadence = SimulationCadence.Hz120)
    {
        IWorkshopInstance[] parts = [PlacedParts.Battery(Source, -3, 3, 0, enabled: enabled), PlacedParts.Clock(Owner, 0, 3, 0, 0, 0, 0, 1, seconds),
            PlacedParts.Counter(Counter, 3, 3, 0, 0, 0, 0, 1, 9)];
        return new(new(2), WorkshopCadenceSettings.Default() with { Simulation = cadence }, new(parts),
            Connections: wired ? new(Supply(Source, Owner), Signal(Owner, Counter)) : new(Signal(Owner, Counter)));
    }

    private static ActivationMemory Row(WorkshopRead read, GpuBodyId owner)
    {
        for (var i = 0; i < read.Memory.Count; i++) if (read.Memory[i].Node == new ActivationNodeId(owner.Value)) return read.Memory[i];
        throw new InvalidOperationException($"No committed memory row for {owner.Value}.");
    }

    private static List<ulong> PulseTicks(List<WorkshopRead> reads)
    {
        var ticks = new List<ulong>();
        for (var i = 1; i < reads.Count; i++)
            if (Row(reads[i], Owner).Count > Row(reads[i - 1], Owner).Count)
            {
                Assert.Equal(Row(reads[i - 1], Owner).Count + 1, Row(reads[i], Owner).Count);
                ticks.Add(reads[i].Tick.Value);
            }
        return ticks;
    }

    /// <summary>A Workshop simulation on the production engine (Hz120) that a test can pause, resume and reset.</summary>


    // ---------- period (9.3 / 9A.42 / 9R.10 AC1; CAT-017 C11, C12) ----------

    // A supplied 1.0 s clock on the production engine pulses exactly 5 times in 5.0 s, at ticks 120, 240, 360, 480 and 600: no
    // startup pulse, one tick of resolution. The clock's pulse output latches the first OscillatorTick of its own node, caused by the
    // clock's first collider at tick 120's physical boundary (4 substeps of 480 Hz per tick), and holds it; the counter downstream
    // receives every pulse at its own tick's boundary. The oscillator row keeps no last-input time (that lane is the counter's).
    [Fact]
    public async Task AOneSecondClockPulsesFiveTimesInFiveSecondsAtEachWholeSecondAndTheCounterCountsEach()
    {
        var reads = await WorkshopIntegrationHarness.Observe(Bench(1f), 600);
        Assert.Equal([120UL, 240UL, 360UL, 480UL, 600UL], PulseTicks(reads));
        Assert.Equal((OscillatorPhase.Running, 5U, 720UL), (Row(reads[^1], Owner).Phase, Row(reads[^1], Owner).Count, Row(reads[^1], Owner).DueTick));
        Assert.Equal(5U, Row(reads[^1], Counter).Count);
        ActivationLatch Latch(WorkshopRead read) => Assert.Single(Enumerable.Range(0, read.Activations.Count).Select(i => read.Activations[i]),
            candidate => candidate.Owner == Owner);
        Assert.Equal(ActivationPhase.Clear, Latch(reads[119]).Phase);
        var first = Latch(reads[120]);
        Assert.Equal((ActivationPhase.Latched, ActivationOccurrenceKind.OscillatorTick, new ActivationNodeId(Owner.Value), Owner,
                Bench(1f).Instances.Identities[Owner].Colliders[0], 480U),
            (first.Phase, first.Kind, first.Emitter, first.Body, first.Collider, first.EventOrdinal));
        Assert.All(reads.Skip(120), read => Assert.Equal(first, Latch(read)));
        foreach (var tick in new[] { 120, 240, 360, 480, 600 })
        {
            Assert.Equal((uint)tick * 4, Row(reads[tick], Counter).Last.Ordinal);
            Assert.Equal(default, Row(reads[tick], Owner).Last);
        }
    }

    public static TheoryData<float, SimulationCadence, uint> Periods() => new()
    {
        { .1f, SimulationCadence.Hz60, 6 }, { .1f, SimulationCadence.Hz120, 12 }, { .1f, SimulationCadence.Hz240, 24 },
        { 1f, SimulationCadence.Hz60, 60 }, { 1f, SimulationCadence.Hz120, 120 }, { 1f, SimulationCadence.Hz240, 240 },
        { 12f, SimulationCadence.Hz60, 720 }, { 12f, SimulationCadence.Hz120, 1440 }, { 12f, SimulationCadence.Hz240, 2880 },
    };

    // Over 7200 committed ticks of the production network at every cadence, a supplied clock's count is exactly floor(tick / interval)
    // at every tick (no drift, no startup pulse), and Consume of the same committed inputs twice yields the same checkpoint.
    [Theory]
    [MemberData(nameof(Periods))]
    public void TheCountIsTheFloorOfTheTickOverTheIntervalAtEveryCadence(float seconds, SimulationCadence cadence, uint interval)
    {
        var bench = Bench(seconds, cadence: cadence);
        var network = WorkshopActivationCompiler.Compile(bench);
        Assert.Equal(interval, Assert.Single(network.Nodes.ToArray(), node => node.Owner == Owner).IntervalTicks);
        var powered = new PhysicsElectricalRead([new ElectricalSourceRead(new(1), Source, new Joules(3600f), default, ElectricalEnable.Enabled)]);
        var committed = new ActivationCheckpoint(network.Clear(), network.ClearTimers(), network.ClearMemory());
        for (ulong tick = 1; tick <= 7200; tick++)
        {
            var next = network.Consume(committed.Activations, committed.Timers, [], new(tick), [], [], powered, memory: committed.Memory);
            if (tick == interval)
            {
                var again = network.Consume(committed.Activations, committed.Timers, [], new(tick), [], [], powered, memory: committed.Memory);
                Assert.Equal(network.Memory(next.Memory, Owner), network.Memory(again.Memory, Owner));
                Assert.Equal(Enumerable.Range(0, next.Activations.Count).Select(i => next.Activations[i]),
                    Enumerable.Range(0, again.Activations.Count).Select(i => again.Activations[i]));
            }
            committed = next;
            var row = network.Memory(committed.Memory, Owner);
            if (row.Count != (uint)(tick / interval)) Assert.Fail($"tick {tick}: count {row.Count}, expected {tick / interval}");
        }
    }

    public static TheoryData<float, int, SimulationCadence, uint> Boundaries() => new()
    {
        // 0.1 s and its upper neighbour (the f32 product rounds 6.0000005 up past 6).
        { .1f, 0, SimulationCadence.Hz60, 6 }, { .1f, 0, SimulationCadence.Hz120, 12 }, { .1f, 0, SimulationCadence.Hz240, 24 },
        { .1f, 1, SimulationCadence.Hz60, 7 }, { .1f, 1, SimulationCadence.Hz120, 13 }, { .1f, 1, SimulationCadence.Hz240, 25 },
        // 1 s and both neighbours: one ulp above is the next tick, one ulp below rounds back to the whole tick.
        { 1f, -1, SimulationCadence.Hz60, 60 }, { 1f, -1, SimulationCadence.Hz120, 120 }, { 1f, -1, SimulationCadence.Hz240, 240 },
        { 1f, 0, SimulationCadence.Hz60, 60 }, { 1f, 0, SimulationCadence.Hz120, 120 }, { 1f, 0, SimulationCadence.Hz240, 240 },
        { 1f, 1, SimulationCadence.Hz60, 61 }, { 1f, 1, SimulationCadence.Hz120, 121 }, { 1f, 1, SimulationCadence.Hz240, 241 },
        // 12 s and its lower neighbour.
        { 12f, -1, SimulationCadence.Hz60, 720 }, { 12f, -1, SimulationCadence.Hz120, 1440 }, { 12f, -1, SimulationCadence.Hz240, 2880 },
        { 12f, 0, SimulationCadence.Hz60, 720 }, { 12f, 0, SimulationCadence.Hz120, 1440 }, { 12f, 0, SimulationCadence.Hz240, 2880 },
    };

    private static float Ulps(float value, int ulps) => BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(value) + ulps);

    // The compiled interval at the admitted f32 boundaries and their one-ulp neighbours, at each cadence (the parity pin of D2).
    [Theory]
    [MemberData(nameof(Boundaries))]
    public void TheCompiledIntervalIsTheCeilingOfTheF32ProductAtTheBoundaries(float seconds, int ulps, SimulationCadence cadence, uint interval)
    {
        var network = WorkshopActivationCompiler.Compile(Bench(Ulps(seconds, ulps), cadence: cadence));
        Assert.Equal(interval, Assert.Single(network.Nodes.ToArray(), node => node.Owner == Owner).IntervalTicks);
    }

    // C19 at the construction boundary: an interval below 0.1 s or above 12 s, zero, negative, NaN or infinite is refused, including the
    // one-ulp neighbours just outside the admitted range.
    [Theory]
    [InlineData(.1f, -1)]
    [InlineData(12f, 1)]
    [InlineData(0f, 0)]
    [InlineData(-1f, 0)]
    [InlineData(.01f, 0)]
    [InlineData(13f, 0)]
    [InlineData(float.NaN, 0)]
    [InlineData(float.PositiveInfinity, 0)]
    public void AnIntervalOutsideItsAdmittedRangeIsRefusedAtConstruction(float seconds, int ulps) =>
        Assert.ThrowsAny<ArgumentException>(() => PlacedParts.Clock(Owner, 0, 3, 0, 0, 0, 0, 1, float.IsFinite(seconds) ? Ulps(seconds, ulps) : seconds));

    // ---------- pause, resume and reset (9R.10 AC2, AC3; CAT-017 C20, C21) ----------

    // Pausing mid-interval holds the committed world: every Advance while paused is refused WrongPhase (2.0 s of them) and the committed
    // read, its clock row included, is unchanged; Resume continues the spacing, so the paused and unpaused runs retain the authored pulse schedule.
    [Fact]
    public async Task APauseAddsNoPulseAndNoPhaseAndResumeContinuesTheSpacing()
    {
        var bench = Bench(1f);
        var plain = await WorkshopIntegrationHarness.Observe(bench, 400);
        await using var harness = await WorkshopIntegrationHarness.Running(bench);
        var first = await harness.AdvanceIncludingCurrent(180);
        Assert.Equal((OscillatorPhase.Running, 1U, 240UL), (Row(first[^1], Owner).Phase, Row(first[^1], Owner).Count, Row(first[^1], Owner).DueTick));
        Assert.Equal(WorkshopCommandOutcome.Applied, ((await harness.Execute(WorkshopCommandKind.Pause)).Result).Outcome);
        var held = harness.Committed;
        for (var i = 0; i < 240; i++)
        {
            var refused = await harness.AdvanceOne();
            Assert.Equal((WorkshopCommandOutcome.Rejected, WorkshopRejection.WrongPhase), (refused.Outcome, refused.Reason));
        }
        Assert.Equal(held.Revision, harness.Committed.Revision);
        NoFixedCapacityTests.AssertSameState(held, harness.Committed, "held while paused");
        Assert.Equal(Row(first[^1], Owner), Row(harness.Committed, Owner));
        Assert.Equal(WorkshopCommandOutcome.Applied, ((await harness.Execute(WorkshopCommandKind.Resume)).Result).Outcome);
        var resumed = first.Concat((await harness.AdvanceIncludingCurrent(220)).Skip(1)).ToList();
        Assert.Equal([120UL, 240UL, 360UL], PulseTicks(resumed));
        Assert.Equal([120UL, 240UL, 360UL], PulseTicks(plain));
        Assert.All(resumed, read => Assert.Equal((uint)(read.Tick.Value / 120), Row(read, Owner).Count));
    }

    // Reset admits the authored world again: the clock is Stopped with no due tick, no pulses and no last occurrence, the read equals the
    // built one, and the next Run's first pulse is again at tick 120 (phase 0), with the authored count at each tick.
    [Fact]
    public async Task ResetStopsTheClockAndTheNextRunPulsesFirstAtOneSecond()
    {
        var (built, run, reset) = await WorkshopIntegrationHarness.ObserveReset(Bench(1f), 300);
        Assert.Equal([120UL, 240UL], PulseTicks([built, .. run]));
        Assert.Equal((OscillatorPhase.Stopped, 0UL, 0U, default(ActivationTime)),
            (Row(reset, Owner).Phase, Row(reset, Owner).DueTick, Row(reset, Owner).Count, Row(reset, Owner).Last));
        NoFixedCapacityTests.AssertSameState(built, reset, "reset");
        await using var harness = await WorkshopIntegrationHarness.Running(Bench(1f));
        var first = await harness.AdvanceIncludingCurrent(150);
        Assert.Equal(WorkshopCommandOutcome.Applied, ((await harness.Execute(WorkshopCommandKind.Reset)).Result).Outcome);
        NoFixedCapacityTests.AssertSameState(first[0], harness.Committed, "reset after 150 ticks");
        Assert.Equal(WorkshopCommandOutcome.Applied, ((await harness.Execute(WorkshopCommandKind.Run)).Result).Outcome);
        var second = await harness.AdvanceIncludingCurrent(150);
        Assert.Equal([120UL], PulseTicks(second));
        Assert.Equal([120UL], PulseTicks(first));
        Assert.All(second, read => Assert.Equal((uint)(read.Tick.Value / 120), Row(read, Owner).Count));
    }

    // ---------- negative and boundary (9R.10 AC5; CAT-017 C13, C14, C16) ----------

    // Controls: an unsupplied clock (no PowerIn link) and one supplied by a disabled battery stay Stopped and emit nothing in 5.0 s.
    [Theory]
    [InlineData(false, ElectricalEnable.Enabled)]
    [InlineData(true, ElectricalEnable.Disabled)]
    public async Task AnUnsuppliedClockStaysStoppedAndEmitsNothing(bool wired, ElectricalEnable enabled)
    {
        var reads = await WorkshopIntegrationHarness.Observe(Bench(1f, wired, enabled), 600);
        Assert.Empty(PulseTicks(reads));
        Assert.All(reads, read => Assert.Equal((OscillatorPhase.Stopped, 0UL, 0U), (Row(read, Owner).Phase, Row(read, Owner).DueTick, Row(read, Owner).Count)));
        Assert.Equal(0U, Row(reads[^1], Counter).Count);
    }

    // C13: supply lost on the deadline tick suppresses that pulse and stops the clock; restoring supply starts a fresh full interval
    // from the first supplied tick (no catch-up of the missed pulse).
    [Fact]
    public async Task SupplyLostAtTheDeadlineSuppressesThePulseAndRestoreStartsAFreshInterval()
    {
        var phases = await WorkshopIntegrationHarness.ObservePhases(Bench(1f), ([], 119), ([new(Source, ElectricalEnable.Disabled)], 31),
            ([new(Source, ElectricalEnable.Enabled)], 300));
        Assert.Equal((OscillatorPhase.Running, 120UL, 0U), (Row(phases[0][^1], Owner).Phase, Row(phases[0][^1], Owner).DueTick, Row(phases[0][^1], Owner).Count));
        Assert.All(phases[1], read => Assert.Equal((OscillatorPhase.Stopped, 0U), (Row(read, Owner).Phase, Row(read, Owner).Count)));
        Assert.Equal(120UL, phases[1][0].Tick.Value);
        var restored = phases[2];
        var start = restored.First(read => Row(read, Owner).Phase == OscillatorPhase.Running);
        Assert.Equal(start.Tick.Value + 120, Row(start, Owner).DueTick);
        var pulses = PulseTicks([phases[1][^1], .. restored]);
        Assert.Equal([start.Tick.Value + 120, start.Tick.Value + 240], pulses.Take(2));
    }

    // Two sources supplying one clock's PowerIn are refused with the typed reason.
    [Fact]
    public void TwoSourcesOnOneClockAreRefused()
    {
        var bench = Bench(1f);
        var second = PlacedParts.Battery(new(4), -6, 3, 0);
        bench = bench with { Instances = bench.Instances.With(second), Connections = bench.Connections.With(Supply(second.Id, Owner)) };
        Assert.Equal(WorkshopEditRejection.PowerInputSupplied,
            Assert.Throws<WorkshopEditRejectedException>(() => WorkshopActivationCompiler.Compile(bench)).Reason);
    }

    // ---------- lifecycle: save and load (9A.42 AC3; CAT-017 C22) ----------

    // A saved bench (0.1 s, 1 s and 12 s) loads equal at the save and command boundaries, its built read holds a stopped clock (counts
    // are not saved), and each run preserves the authored pulse schedule.
    [Theory]
    [InlineData(.1f)]
    [InlineData(1f)]
    [InlineData(12f)]
    public async Task SaveAndLoadPreserveThePulseSchedule(float seconds)
    {
        var bench = Bench(seconds);
        var restored = WorkshopSaveCodec.Decode(SaveFixture.Elements, WorkshopSaveCodec.Encode(new WorkshopSavedConstruction(bench, new(4)))).Construction;
        Assert.Equal(bench, restored);
        var command = WorkshopWireFlatBuffers.DecodeCommand(WorkshopWireFlatBuffers.EncodeCommand(new WorkshopCommand(new(1),
            WorkshopCommandKind.Construct, new(1), new(1), CompiledSceneFixture.Compile(bench), Session: new(1, 2), Cadence: new(1), Projection: new(1))));
        Assert.Equal((uint)(seconds * 120), command.Scene!.Activation.Nodes.ToArray().Single(node => node.Kind == ActivationNodeKind.Oscillator).IntervalTicks);
        var run = await WorkshopIntegrationHarness.Observe(bench, 240);
        var replay = await WorkshopIntegrationHarness.Observe(restored, 240);
        Assert.Equal((OscillatorPhase.Stopped, 0U), (Row(replay[0], Owner).Phase, Row(replay[0], Owner).Count));
        var period = seconds switch { .1f => 12, 1f => 120, 12f => 1440, _ => throw new ArgumentOutOfRangeException(nameof(seconds)) };
        var expected = Enumerable.Range(1, 240 / period).Select(index => (ulong)(index * period)).ToArray();
        Assert.Equal(expected, PulseTicks(run));
        Assert.Equal(expected, PulseTicks(replay));
        Assert.All(replay, read => Assert.Equal((uint)(read.Tick.Value / (ulong)period), Row(read, Owner).Count));
    }

    // ---------- presentation (9R.10 AC6, coordinator decision 18 Oct 2026; CAT-017 C20, C23) ----------

    private static double Time(WorkshopRead read) => read.Tick.Value / 120.0;
    private static WorkshopResponse Response(WorkshopRead read, ulong publication) =>
        new(default, WorkshopResponseKind.Read, new(WorkshopCommandOutcome.Applied, WorkshopRejection.None),
            WorkshopSimulationPhase.Running, read, new(101, 206), new(publication), new(1), new(1), new(1), new(read.Tick.Value));
    private static float Sample(IReadOnlyList<DeclaredPresentationSample> samples, int binding) =>
        BitConverter.UInt32BitsToSingle(Assert.Single(samples, sample => sample.Owner == Owner && sample.Binding == binding).Value.CanonicalBits);

    // The package's pendulum binding (Visual/PendulumPivot, local Z rotation from -0.42 to 0.42 rad) follows .5 + .5·sin(2π·phase) of
    // the committed row, phase = 1 − ticks to the due tick / interval: a sample left to converge from a committed read equals that
    // input mapped onto the declared range. A stopped (unsupplied) clock rests at the middle, 0 rad.
    [Theory]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(90)]
    [InlineData(119)]
    [InlineData(120)]
    [InlineData(150)]
    public async Task ThePendulumFollowsTheCommittedPhase(int tick)
    {
        var bench = Bench(1f);
        var reads = await WorkshopIntegrationHarness.Observe(bench, 150);
        var row = Row(reads[tick], Owner);
        var phase = 1f - (float)(row.DueTick - (ulong)tick) / 120;
        var input = .5f + .5f * MathF.Sin(MathF.Tau * phase);
        var presentation = new WorkshopDeclaredPresentation(CompiledSceneFixture.Compile(bench), Response(reads[tick], 1));
        Assert.Equal(-.42f + .84f * input, Sample(presentation.Advance(AnimationTestTime.Seconds(Time(reads[tick]) + 10)), 0), 1e-5f);
        // Paused (no further committed read): the follow target is unchanged and the sample stays converged on it.
        Assert.Equal(-.42f + .84f * input, Sample(presentation.Advance(AnimationTestTime.Seconds(Time(reads[tick]) + 12)), 0), 1e-5f);

        var stopped = await WorkshopIntegrationHarness.Observe(Bench(1f, wired: false), 1);
        var rest = new WorkshopDeclaredPresentation(CompiledSceneFixture.Compile(Bench(1f, wired: false)), Response(stopped[1], 1));
        Assert.Equal(0f, Sample(rest.Advance(AnimationTestTime.Seconds(Time(stopped[1]) + 10)), 0), 1e-6f);
    }

    // The face flashes once per committed pulse: following every committed read of a 5.0 s run tick by tick, the indicator's red channel
    // reaches its declared lit value (247/255) on exactly the five pulse frames, and decays to the idle value (85/255) in between. With no
    // read after a pause, no new flash starts and the face settles to idle.
    [Fact]
    public async Task TheFaceFlashesOncePerCommittedPulse()
    {
        var bench = Bench(1f);
        var reads = await WorkshopIntegrationHarness.Observe(bench, 600);
        var presentation = new WorkshopDeclaredPresentation(CompiledSceneFixture.Compile(bench), Response(reads[1], 1));
        presentation.Advance(AnimationTestTime.Seconds(Time(reads[1])));
        var lit = new List<ulong>();
        for (var i = 2; i < reads.Count; i++)
        {
            presentation.Accept(Response(reads[i], (ulong)i));
            if (Sample(presentation.Advance(AnimationTestTime.Seconds(Time(reads[i]))), 1) == 247f / 255f) lit.Add(reads[i].Tick.Value);
        }
        Assert.Equal([120UL, 240UL, 360UL, 480UL, 600UL], lit);
        Assert.Equal(85f / 255f, Sample(presentation.Advance(AnimationTestTime.Seconds(Time(reads[^1]) + 2)), 1));
    }

    // Reset returns the presentation to rest: from the reset read the pendulum settles at 0 rad and the face is idle, with no flash.
    [Fact]
    public async Task ResetReturnsThePendulumToRestAndTheFaceToIdle()
    {
        var bench = Bench(1f);
        var (_, run, reset) = await WorkshopIntegrationHarness.ObserveReset(bench, 300);
        var presentation = new WorkshopDeclaredPresentation(CompiledSceneFixture.Compile(bench), Response(reset, 1) with { Projection = new(2) });
        var settled = presentation.Advance(AnimationTestTime.Seconds(Time(run[^1]) + 10));
        Assert.Equal(0f, Sample(settled, 0), 1e-6f);
        Assert.Equal(85f / 255f, Sample(settled, 1));
        Assert.Throws<ArgumentException>(() => presentation.Accept(Response(run[119], 2) with { Projection = new(2) }));
    }
}
