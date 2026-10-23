using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;
using Ring = CuriousContraptions.Tests.EventRingProducerTests.Ring;

namespace CuriousContraptions.Tests;

/// <summary>Story 9A.23 + 9A.25b: the event ring's consumers on the production simulation. A real run publishes each committed tick's
/// records to the ring (the worker's drain, appended to the C# reference ring); the Animation worker's turn is reproduced natively: its
/// cursor (the reference reader, which the shared JS reader is proven equal to) drains the new records into one delivery for the
/// production evaluator (<see cref="WorkshopDeclaredPresentation"/>), and the main thread's cursor feeds the goal wake. Every expected
/// cue is derived from the run's own records and the declared envelope, never from the evaluator.</summary>
public sealed class EventRingConsumerTests
{
    private static readonly double Floor = (double)WorkshopConstruction.WorkbenchSurface.Value;
    private static readonly double Half45 = Math.Sqrt(.5);
    private const uint Substeps = 4; // the default 120 Hz cadence: four 480 Hz physical steps per tick

    private static WorkshopConstruction Bench(IWorkshopInstance[] instances, WorkshopConnection[] links) =>
        new(new(2), WorkshopCadenceSettings.Default(), new(instances), Connections: new(links));
    private static WorkshopConnection Signal(GpuBodyId source, GpuBodyId target) =>
        new(source, WorkshopSocket.ActivationOut, target, WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation);
    private static WorkshopResponse Response(WorkshopRead read, ulong publication, ulong projection = 1) =>
        new(default, WorkshopResponseKind.Read, new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), WorkshopSimulationPhase.Running,
            read, new(101, 206), new(publication), new(1), new(1), new(projection), new(read.Tick.Value));
    private static float F(AnimationValue value) => BitConverter.UInt32BitsToSingle(value.CanonicalBits);

    /// <summary>The Animation worker's consumer loop over a real run.</summary>
    private sealed class Animation
    {
        public Ring Ring { get; }
        public WorkshopConstruction Construction { get; }
        public WorkshopDeclaredPresentation Presentation { get; private set; } = null!;
        public WorkshopWorldProjection World { get; private set; }
        public List<SimulationEventRecord> Delivered { get; } = [];
        private readonly EventRingConsumer _cursor = new();
        private ulong _publication, _lapped;

        /// <summary>Installs on a running ring the caller owns.</summary>
        public Animation(Ring ring, WorkshopConstruction construction) { Ring = ring; Construction = construction; Install(); }

        /// <summary>A non-retained installation (Run or Reset): a fresh evaluator at rest, anchored at the admitted read.</summary>
        private void Install()
        {
            var read = Ring.Simulation.Committed;
            _publication = 1;
            Presentation = new(Construction, Response(read, _publication));
            World = new(read.Epoch, new(1), read.Tick, new(read.Capture!.Value.Time.Value), WorldPlayback.Running);
            Delivered.Clear();
        }

        /// <summary>One Service turn's read: every whole record since the cursor, as one delivery.</summary>
        public void Drain()
        {
            var records = new List<SimulationEventRecord>();
            while (_cursor.TryRead(Ring.Bytes, SimulationDomain.None, out var record)) records.Add(record);
            var skipped = _cursor.Lapped - _lapped; _lapped = _cursor.Lapped;
            if (records.Count == 0 && skipped == 0) return;
            Presentation.AcceptEvents(new(_cursor.Epoch, skipped, records.ToArray()), World);
            Delivered.AddRange(records);
        }

        /// <summary>Commits ticks. The worker publishes a tick's records before its read, so this reader takes them first.</summary>
        public async Task Advance(int ticks)
        {
            for (var tick = 0; tick < ticks; tick++)
            {
                await Ring.Advance(1);
                Drain();
                Presentation.Accept(Response(Ring.Simulation.Committed, ++_publication));
            }
        }

        public async Task ResetAndRun()
        {
            await Ring.Reset();
            await Ring.Run();
            Install();
            Drain();
        }

        /// <summary>The record's physical instant on the presentation clock, derived here (not by the evaluator): its ordinal plus phase
        /// (1/4096 of a 480 Hz step) since the anchored tick's first step, after the anchor's master time.</summary>
        public double At(SimulationEventRecord record) => World.AnchorMaster.Value / 1e9 +
            (record.Ordinal + record.Phase / 4096.0 - World.AnchorTick.Value * (double)Substeps) / 480.0;

        public double Origin => World.AnchorMaster.Value / 1_000_000_000d;

        public float[] Sample(double at, GpuBodyId owner) =>
            Presentation.Advance(at).Where(sample => sample.Owner == owner).OrderBy(sample => sample.Binding).Select(sample => F(sample.Value)).ToArray();
    }

    /// <summary>The declared one-shot values of <paramref name="part"/> at <paramref name="amount"/> of its envelope.</summary>
    private static float[] Cue(PartId part, double amount) =>
        DeclaredPhysics.Of(part).Presentation.Bindings.Select(binding => F(AnimationValue.Blend(
            WorkshopDeclaredPresentation.Value(binding.Property, binding.From), WorkshopDeclaredPresentation.Value(binding.Property, binding.To),
            new((float)amount)))).ToArray();

    private static float Duration(PartId part) => DeclaredPhysics.Of(part).Presentation.Bindings[0].Duration;

    // The quintic SmoothStep at the quarter points, exactly (the 9A.25a golden amounts).
    private static readonly double[] Quarter = [0, .103515625, .5, .896484375, 1];

    // ---------- the 9A.22 Impact record bytes stay the same after the occurrence rows become device-private ----------

    private static async Task<List<SimulationEventRecord>> BumperRun()
    {
        await using var ring = await Ring.Running(Bench([WorkshopInput.Generic(PartId.Basketball, new(1), 0, 4.5, 0, 0, 0, 0, 1),
            PlacedParts.Bumper(new(2), 0, 3, 0)], []));
        await ring.Advance(360);
        return ring.Records;
    }

    [Fact]
    public async Task TheImpactRecordBytesOfTheBumperRunAreUnchanged()
    {
        var impacts = (await BumperRun()).Where(record => record.Kind == SimulationEventKind.Impact).ToArray();
        var bytes = new byte[impacts.Length * EventRingBuffer.SlotBytes];
        for (var i = 0; i < impacts.Length; i++) impacts[i].Write(bytes.AsSpan(i * EventRingBuffer.SlotBytes, EventRingBuffer.SlotBytes));
        // Captured without D9 (occurrence rows on the read) on main 0cba945b, the restitution engine this slice merged
        // (evidence-9a-23-25b/post-merge/impact-records-0cba945b.txt, with its provenance; the f5ffdb4e capture is baseline/impact-records.txt).
        Assert.Equal(ImpactRecordCount, impacts.Length);
        var hash = Convert.ToHexString(SHA256.HashData(bytes));
        Assert.True(hash == ImpactRecordSha256, $"The Impact record bytes changed: SHA-256 {hash}, records {Convert.ToHexString(bytes)}");
    }
    private const int ImpactRecordCount = 3;
    private const string ImpactRecordSha256 = "291756D737E1FF95741B6BCA83C2C67BD4B3F0B8451FBE9FD4363B0AEC00DB8F";

    // ---------- 9A.25b: every declared one-shot is driven only by its ring kind ----------

    // Each paid Impact (positive Magnitude: the energy the store supplied) adds exactly one SineSquared pulse at its own physical time;
    // overlapping pulses saturate; an unpaid hit (the empty store's later contacts, a strength-0 bumper) cues nothing, as the powered ring
    // has always meant. The expected blend at every sampled instant is the declared envelope summed over the run's own paid Impact
    // records, so a doubled, missing, unpaid or mistimed cue differs.
    [Theory]
    [InlineData("ball", 4.5, 8f, 360)]      // paid, then unpaid hits once the target speed is reached
    [InlineData("bowling", 6, 8f, 480)]     // the heavy ball empties the store; every later hit is unpaid
    [InlineData("ball", 4.5, 0f, 360)]      // an empty store: every hit is unpaid, so the bumper never pulses
    public async Task EachPaidBumperImpactCuesExactlyOnePulseAtItsOwnPhysicalTime(string payload, double height, float strength, int ticks)
    {
        GpuBodyId ball = new(1), bumper = new(2);
        var bench = Bench([WorkshopInput.Generic(new PartId(payload), ball, 0, height, 0, 0, 0, 0, 1),
            PlacedParts.Bumper(bumper, 0, 3, 0, strength)], []);
        await using var ring = await Ring.Running(bench);
        var run = new Animation(ring, bench);
        await run.Advance(ticks);
        var impacts = run.Delivered.Where(record => record.Kind == SimulationEventKind.Impact).ToArray();
        Assert.NotEmpty(impacts);
        Assert.Equal(run.Ring.Of(SimulationEventKind.Impact).Count(), impacts.Length);
        var paid = impacts.Where(record => record.Magnitude > 0f).ToArray();
        Assert.Contains(impacts, record => record.Magnitude == 0f); // the control: an unpaid hit in every run
        Assert.Equal(strength > 0, paid.Length > 0);
        if (paid.Length > 0) Assert.Contains(paid, record => record.Ordinal % Substeps != 0); // a hit inside its tick, not a boundary
        var duration = (double)Duration(PartId.PinballBumper);
        double Expected(double at)
        {
            var sum = 0.0;
            foreach (var record in paid)
            {
                var elapsed = at - run.At(record);
                if (elapsed > 0 && elapsed < duration) { var sine = Math.Sin(Math.PI * elapsed / duration); sum = Math.Min(1, sum + sine * sine); }
            }
            return sum;
        }
        var times = impacts.SelectMany(record => Enumerable.Range(0, 5).Select(k => run.At(record) + duration * k / 4)).Order().ToArray();
        foreach (var at in times.Where(at => at >= run.Origin))
        {
            var sampled = run.Sample(at, bumper);
            var expected = Cue(PartId.PinballBumper, Expected(at));
            for (var i = 0; i < expected.Length; i++) Assert.Equal(expected[i], sampled[i], 1e-5f);
        }
        // Back at rest after the last pulse.
        Assert.Equal(Cue(PartId.PinballBumper, 0), run.Sample(times[^1] + duration, bumper));
    }

    public static TheoryData<string> ClipScenarios => new() { "capture", "switch", "detector" };

    private static (WorkshopConstruction Bench, GpuBodyId Owner, PartId Part, SimulationEventKind Kind, int Ticks) Scenario(string name, bool fires = true) => name switch
    {
        "capture" => (Bench([PayloadInstanceTests.Place(PartId.Basketball, 1, fires ? 0 : 2, 1.1, 0),
            WorkshopInput.Generic(PartId.Receiver, new(2), 0, 1, 0, 0, 0, 0, 1)], []), new(2), PartId.Receiver, SimulationEventKind.Captured, 240),
        "switch" => (Bench([WorkshopInput.Generic(PartId.ImpactSwitch, new(1), 0, 1, 0, 0, 0, 0, 1,
                new([new(new ParameterId("trigger_threshold"), fires ? .8f : 64f)])),
            PlacedParts.Lamp(new(2), 4, 1, 0, 0, 0, 0, 1), WorkshopInput.Generic(PartId.Basketball, new(4), 0, 3, 0, 0, 0, 0, 1)],
            [Signal(new(1), new(2))]), new(1), PartId.ImpactSwitch, SimulationEventKind.TriggerActivated, 480),
        "detector" => (Bench([PlacedParts.Detector(new(1), 0, Floor + .62, 0, 0, 0, (fires ? -1 : 1) * Half45, Half45),
            PlacedParts.Lamp(new(2), 4, 1, 0, 0, 0, 0, 1), WorkshopInput.Generic(PartId.Basketball, new(3), 0, Floor + .34 + 3, 0, 0, 0, 0, 1)],
            [Signal(new(1), new(2))]), new(1), PartId.BallDetector, SimulationEventKind.Passage, 480),
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    // A capture, a switch press and each detector crossing (every crossing, CAT-002 §2) restart the owner's clip from its rest value at
    // the record's own time; it holds the declared end value until the next record of its kind from that owner.
    [Theory]
    [MemberData(nameof(ClipScenarios))]
    public async Task EachRecordRestartsItsOwnersClipAtItsPhysicalTime(string name)
    {
        var (bench, owner, part, kind, ticks) = Scenario(name);
        await using var ring = await Ring.Running(bench);
        var run = new Animation(ring, bench);
        await run.Advance(ticks);
        var cues = run.Delivered.Where(record => record.Source == owner.Value && record.Kind == kind).ToArray();
        Assert.Equal(name == "detector" ? 2 : 1, cues.Length);
        Assert.Equal(run.Ring.Records.Count, run.Delivered.Count); // the cursor delivered every committed record exactly once
        var duration = (double)Duration(part);
        Assert.Equal(Cue(part, 0), run.Sample(run.Origin, owner)); // at rest before the first record
        foreach (var record in cues)
        {
            Assert.True(run.At(record) - run.Origin > duration, "each cue starts after the previous one completed");
            for (var k = 0; k <= 4; k++) Assert.Equal(Cue(part, Quarter[k]), run.Sample(run.At(record) + duration * k / 4, owner));
        }
        Assert.Equal(Cue(part, 1), run.Sample(run.At(cues[^1]) + 3 * duration, owner));
    }

    // Controls: a miss, a sub-threshold contact, a glancing or a reverse crossing publish nothing, so nothing is cued.
    [Theory]
    [MemberData(nameof(ClipScenarios))]
    public async Task WithoutItsRecordTheClipNeverLeavesRest(string name)
    {
        var (bench, owner, part, kind, ticks) = Scenario(name, fires: false);
        await using var ring = await Ring.Running(bench);
        var run = new Animation(ring, bench);
        await run.Advance(ticks);
        Assert.DoesNotContain(run.Delivered, record => record.Source == owner.Value && record.Kind == kind);
        for (var at = run.Origin; at < run.Origin + ticks / 120.0; at += .05) Assert.Equal(Cue(part, 0), run.Sample(at, owner));
    }

    // Controls: a record from another owner, a kind the package does not declare, and level records (memory, contact load) cue nothing.
    [Fact]
    public async Task AForeignOwnerAnUndeclaredKindOrALevelCuesNothing()
    {
        var (bench, owner, part, _, _) = Scenario("switch");
        await using var ring = await Ring.Running(bench);
        var run = new Animation(ring, bench);
        var tick = run.Ring.Simulation.Committed.Tick.Value + 1;
        SimulationEventRecord Record(ulong sequence, SimulationEventKind kind, ulong source, uint level = 0) =>
            new(sequence, tick, kind, source, 0, checked((uint)(tick * Substeps + 1)), 0f, 0f, 0, level);
        run.Presentation.AcceptEvents(new(run.World.WorldGeneration.Value, 0, [
            Record(1, SimulationEventKind.TriggerActivated, 2), // the lamp's owner, not the switch
            Record(2, SimulationEventKind.Impact, owner.Value), Record(3, SimulationEventKind.Passage, owner.Value),
            Record(4, SimulationEventKind.MemoryChanged, owner.Value, 1), Record(5, SimulationEventKind.ContactLoad, owner.Value, 1)]), run.World);
        for (var k = 0; k <= 8; k++) Assert.Equal(Cue(part, 0), run.Sample(run.Origin + .05 * k, owner));
        // The same owner's own kind cues it: the control is the filter, not a dead evaluator.
        run.Presentation.AcceptEvents(new(run.World.WorldGeneration.Value, 0, [Record(6, SimulationEventKind.TriggerActivated, owner.Value)]), run.World);
        Assert.Equal(Cue(part, 1), run.Sample(run.Origin + 1, owner));
    }

    // The cue starts at the record's ordinal and phase, not at its tick's boundary: three physical steps into the tick is 3/480 s later.
    [Fact]
    public async Task TheCueStartsAtTheRecordsOwnOrdinalAndPhase()
    {
        var (bench, owner, part, kind, _) = Scenario("switch");
        await using var ring = await Ring.Running(bench);
        var run = new Animation(ring, bench);
        var tick = run.Ring.Simulation.Committed.Tick.Value + 2;
        var record = new SimulationEventRecord(1, tick, kind, owner.Value, 4, checked((uint)(tick * Substeps + 3)), 1024f, 1f, 0, 0);
        Assert.Equal(3.25 / WorkshopCadenceSettings.PhysicalFrequency,
            run.At(record) - run.At(record with { Ordinal = checked((uint)(tick * Substeps)), Phase = 0f }), 12);
        Assert.Equal(run.At(record), WorkshopDeclaredPresentation.EventSeconds(record, run.World, 1 / 120.0), 9); // the evaluator's clock
        run.Presentation.AcceptEvents(new(run.World.WorldGeneration.Value, 0, [record]), run.World);
        var duration = (double)Duration(part);
        Assert.Equal(Cue(part, 0), run.Sample(run.At(record) - .001, owner));
        Assert.Equal(Cue(part, Quarter[2]), run.Sample(run.At(record) + duration / 2, owner));
    }

    // Pause, Resume and Step are retained rebinds: the cursor and the cues are kept, nothing is published by a Pause, and no record is
    // ever applied twice (a redelivered record is refused).
    [Fact]
    public async Task PauseResumeAndStepRetainTheCursorAndTheCuesWithNoDuplicate()
    {
        var (bench, owner, part, kind, _) = Scenario("switch");
        await using var ring = await Ring.Running(bench);
        var run = new Animation(ring, bench);
        await run.Advance(480);
        var press = Assert.Single(run.Delivered, record => record.Kind == kind);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await run.Ring.Simulation.Pause()).Outcome);
        Assert.Empty(EventRingProducerTests.Decode(run.Ring.Publish()));
        var paused = run.Ring.Simulation.Committed;
        var endpoint = Response(paused, 0, projection: 2);
        Assert.True(run.Presentation.CanRetain(bench, endpoint));
        run.Presentation.Rebind(bench, endpoint);
        run.Drain();
        Assert.Equal(WorkshopCommandOutcome.Applied, (await run.Ring.Simulation.Step()).Outcome);
        run.Ring.Publish(); run.Drain();
        Assert.Equal(WorkshopCommandOutcome.Applied, (await run.Ring.Simulation.Resume()).Outcome);
        run.Ring.Publish(); run.Drain();
        Assert.Single(run.Delivered, record => record.Kind == kind);
        Assert.Throws<ArgumentException>(() => run.Presentation.AcceptEvents(new(run.World.WorldGeneration.Value, 0, [press]), run.World));
        Assert.Equal(Cue(part, 1), run.Sample(run.At(press) + 1, owner)); // the retained cue holds its end value
    }

    // A hidden part's cue keeps time (D7): shown again, a cue that ended while hidden is at its end value and one still running is at its
    // exact phase; nothing restarts.
    [Theory]
    [InlineData("switch")]
    [InlineData("bumper")]
    public async Task AHiddenPartResumesAtItsCuesExactPhaseOrEndValue(string name)
    {
        var (bench, owner, part) = name == "bumper"
            ? (Bench([PlacedParts.Bumper(new(1), 0, 1, 0)], []), new GpuBodyId(1), PartId.PinballBumper)
            : (Scenario("switch").Bench, Scenario("switch").Owner, PartId.ImpactSwitch);
        var kind = DeclaredPhysics.Of(part).Presentation.Bindings[0].Event;
        await using var ring = await Ring.Running(bench);
        var run = new Animation(ring, bench);
        var duration = (double)Duration(part);
        var target = WorkshopDeclaredPresentation.Targets(bench).First(entry => entry.Owner == owner);
        var property = WorkshopDeclaredPresentation.Property(DeclaredPhysics.Of(part).Presentation.Bindings[0].Property);
        var tick = run.Ring.Simulation.Committed.Tick.Value + 1;
        SimulationEventRecord At(ulong sequence, double seconds) => new(sequence, tick, kind, owner.Value, 0,
            checked((uint)Math.Round((seconds - run.Origin) * WorkshopCadenceSettings.PhysicalFrequency + run.World.AnchorTick.Value * Substeps)), 0f, 1f, 0, 0);
        run.Presentation.SetVisible(target.Target, property, false);
        var early = At(1, run.Origin + .1);
        run.Presentation.AcceptEvents(new(run.World.WorldGeneration.Value, 0, [early]), run.World);
        Assert.Empty(run.Sample(run.Origin + 1, owner));
        Assert.Empty(run.Sample(run.Origin + 2, owner)); // two seconds hidden
        run.Presentation.SetVisible(target.Target, property, true);
        Assert.Equal(Cue(part, name == "bumper" ? 0 : 1), run.Sample(run.Origin + 2.01, owner)); // ended while hidden: its end value
        run.Presentation.SetVisible(target.Target, property, false);
        var late = At(2, run.Origin + 3);
        run.Presentation.AcceptEvents(new(run.World.WorldGeneration.Value, 0, [late]), run.World);
        Assert.Empty(run.Sample(run.At(late) + duration / 8, owner));
        run.Presentation.SetVisible(target.Target, property, true);
        // Still running: exactly the phase it reached while hidden (a quarter of the clip, or the pulse's half-height).
        Assert.Equal(name == "bumper" ? Cue(part, .5) : Cue(part, Quarter[1]), run.Sample(run.At(late) + duration / 4, owner));
    }

    // A record observed after the presentation clock passed its instant (a stalled worker, a late turn) starts its cue at its own time:
    // the clip or pulse is already at the phase it has reached, never restarted from rest when it is observed.
    [Theory]
    [InlineData("switch")]
    [InlineData("bumper")]
    public async Task ALateRecordStartsItsCueAtItsOwnTimeNotWhenObserved(string name)
    {
        var (bench, owner, part) = name == "bumper"
            ? (Bench([PlacedParts.Bumper(new(1), 0, 1, 0)], []), new GpuBodyId(1), PartId.PinballBumper)
            : (Scenario("switch").Bench, Scenario("switch").Owner, PartId.ImpactSwitch);
        await using var ring = await Ring.Running(bench);
        var run = new Animation(ring, bench);
        var duration = (double)Duration(part);
        Assert.Equal(Cue(part, 0), run.Sample(run.Origin + .2, owner)); // the clock has reached origin + 0.2 s
        var tick = run.Ring.Simulation.Committed.Tick.Value + 1;
        // Its instant is a quarter of the cue's duration before the clock (to the nearest physical step).
        var late = new SimulationEventRecord(1, tick, DeclaredPhysics.Of(part).Presentation.Bindings[0].Event, owner.Value, 0,
            checked((uint)Math.Round((.2 - duration / 4) * WorkshopCadenceSettings.PhysicalFrequency + run.World.AnchorTick.Value * Substeps)), 0f, 1f, 0, 0);
        Assert.True(run.At(late) < run.Origin + .2);
        run.Presentation.AcceptEvents(new(run.World.WorldGeneration.Value, 0, [late]), run.World);
        // Half its duration has passed since its own instant: the clip is half way, the pulse at its peak.
        Assert.Equal(Cue(part, name == "bumper" ? 1 : Quarter[2]), run.Sample(run.At(late) + duration / 2, owner));
    }

    // Reset (a non-retained installation of a new epoch) puts every one-shot target at rest and restarts the cursor at sequence 1; Run →
    // Reset → Run, and Save → Load → Run, give identical samples.
    [Fact]
    public async Task ResetRestsEveryCueRestartsTheCursorAndRunsReplayIdentically()
    {
        var (bench, owner, part, kind, ticks) = Scenario("capture");
        var duration = (double)Duration(part);
        async Task<float[][]> Trace(Animation run)
        {
            await run.Advance(ticks);
            var cue = Assert.Single(run.Delivered, record => record.Kind == kind);
            Assert.Equal(1UL, run.Delivered[0].Sequence);
            return Enumerable.Range(-1, 7).Select(k => run.Sample(run.At(cue) + duration * k / 4, owner)).ToArray();
        }
        await using var ring = await Ring.Running(bench);
        var run = new Animation(ring, bench);
        var first = await Trace(run);
        Assert.Equal(Cue(part, 1), first[^1]);
        var epoch = run.World.WorldGeneration;
        await run.ResetAndRun();
        Assert.NotEqual(epoch, run.World.WorldGeneration);
        Assert.Equal(Cue(part, 0), run.Sample(run.Origin, owner)); // at rest after Reset
        Assert.Equal(first, await Trace(run));
        var restored = WorkshopSaveCodec.Decode(SaveFixture.Elements, WorkshopSaveCodec.Encode(new WorkshopSavedConstruction(bench, new(9)))).Construction;
        await using var loadedRing = await Ring.Running(restored);
        var loaded = new Animation(loadedRing, restored);
        Assert.Equal(first, await Trace(loaded));
    }

    // A lapped delivery (D6): the skip is counted, cumulative and typed; the lost cue is never replayed and the later cues are exact.
    [Fact]
    public async Task ALappedRingCountsItsSkipLosesOnlyThoseCuesAndKeepsTheRestExact()
    {
        var (bench, owner, part, kind, _) = Scenario("capture");
        await using var ring = await Ring.Running(bench);
        var run = new Animation(ring, bench);
        var tick = run.Ring.Simulation.Committed.Tick.Value + 1;
        var epoch = run.World.WorldGeneration.Value;
        // Through the reference ring: six records into a ring of four; the reader skips the two the writer lapped.
        var outbox = new EventRingOutbox();
        outbox.Restart(new(epoch), 4);
        outbox.Commit(Enumerable.Range(0, 6).Select(i => new SimulationEventRecord(0, tick, kind, owner.Value, 1,
            checked((uint)(tick * Substeps + (ulong)i)), 0f, 0f, 0, 0)).ToArray());
        var lapped = EventRingBuffer.Append(null, outbox.Drain());
        var cursor = new EventRingConsumer();
        var records = new List<SimulationEventRecord>();
        while (cursor.TryRead(lapped, SimulationDomain.None, out var record)) records.Add(record);
        Assert.Equal(2UL, cursor.Lapped);
        Assert.Equal([3UL, 4UL, 5UL, 6UL], records.Select(record => record.Sequence));
        run.Presentation.AcceptEvents(new(epoch, cursor.Lapped, records.ToArray()), run.World);
        Assert.Equal(2UL, run.Presentation.Lapped);
        var duration = (double)Duration(part);
        Assert.Equal(Cue(part, Quarter[2]), run.Sample(run.At(records[^1]) + duration / 2, owner)); // the newest cue, exactly
        run.Presentation.AcceptEvents(new(epoch, 3, []), run.World);
        Assert.Equal(5UL, run.Presentation.Lapped);
    }

    [Fact]
    public async Task ADeliveryOfAnotherEpochOrARecordAlreadyAppliedIsRefused()
    {
        var (bench, owner, _, kind, _) = Scenario("switch");
        await using var ring = await Ring.Running(bench);
        var run = new Animation(ring, bench);
        var tick = run.Ring.Simulation.Committed.Tick.Value + 1;
        var record = new SimulationEventRecord(3, tick, kind, owner.Value, 0, checked((uint)(tick * Substeps)), 0f, 0f, 0, 0);
        var epoch = run.World.WorldGeneration.Value;
        Assert.Throws<ArgumentException>(() => run.Presentation.AcceptEvents(new(epoch + 1, 0, [record]), run.World));
        run.Presentation.AcceptEvents(new(epoch, 0, [record]), run.World);
        Assert.Throws<ArgumentException>(() => run.Presentation.AcceptEvents(new(epoch, 0, [record]), run.World));
        Assert.Throws<ArgumentException>(() => run.Presentation.AcceptEvents(new(epoch, 0, [record with { Sequence = 2 }]), run.World));
        Assert.Throws<ArgumentException>(() => EventRingDelivery.Read(EventRingDelivery.Write(epoch, 0, [record, record])));
        Assert.Throws<ArgumentException>(() => EventRingDelivery.Read(EventRingDelivery.Write(0, 1, [])));
        Assert.Throws<ArgumentException>(() => EventRingDelivery.Read(new byte[EventRingDelivery.HeaderBytes + 1]));
        var delivery = EventRingDelivery.Read(EventRingDelivery.Write(epoch, 2, [record]));
        Assert.Equal((epoch, 2UL, record), (delivery.Epoch, delivery.Skipped, Assert.Single(delivery.Records)));
    }

    // ---------- 9A.23 D8: the main-thread cursor's goal wake ----------

    private static WorkshopGoal CaptureGoal(WorkshopConstruction bench, GpuBodyId ball, GpuBodyId receiver) =>
        new(WorkshopGoalKind.Captured, ball, receiver, Assert.Single(WorkshopPhysicsCompiler.Compile(bench, new(1, 2)).Sensors.ToArray(),
            sensor => sensor.Frame == receiver && sensor.Target == ball).Id);

    // The goal's Captured record usually arrives before its read: the wake is held until a committed read of its tick is installed,
    // then the goal is evaluated once and its cue time is the record's own occurrence. A miss never wakes and never solves.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TheGoalWakeWaitsForItsCommittedReadThenSolvesOnceAtTheRecordsOccurrence(bool captures)
    {
        var (bench, receiver, _, _, ticks) = Scenario("capture", captures);
        GpuBodyId ball = new(1);
        var goal = CaptureGoal(bench, ball, receiver);
        await using var ring = await Ring.Running(bench);
        var wake = new WorkshopGoalWake(goal, WorkshopPhysicsCompiler.Compile(bench, new(1, 2)));
        var cursor = new EventRingConsumer();
        var installed = ring.Simulation.Committed;
        var evaluations = new List<(ulong Tick, ActivationTime? Occurrence)>();
        SimulationEventRecord? captured = null;
        for (var tick = 0; tick < ticks; tick++)
        {
            await ring.Advance(1);
            var records = new List<SimulationEventRecord>();
            while (cursor.TryRead(ring.Bytes, SimulationDomain.None, out var record)) records.Add(record);
            wake.Observe(new(cursor.Epoch, 0, records.ToArray()), installed.Tick);
            if (records.Any(record => record.Kind == SimulationEventKind.Captured))
            {
                captured = records.Single(record => record.Kind == SimulationEventKind.Captured);
                Assert.False(wake.Due(installed.Tick)); // its read is not installed yet: held
            }
            installed = ring.Simulation.Committed;
            if (!wake.Due(installed.Tick)) continue;
            var occurrence = WorkshopGoalEvaluator.Occurrence(goal, installed, installed.Epoch);
            evaluations.Add((installed.Tick.Value, occurrence));
            wake.Evaluated(installed.Tick, occurrence is not null);
        }
        if (!captures) { Assert.Null(captured); Assert.Empty(evaluations); return; }
        var solved = Assert.Single(evaluations);
        Assert.Equal(captured!.Value.Tick, solved.Tick);
        Assert.Equal(new ActivationTime(captured.Value.Ordinal, captured.Value.Phase), solved.Occurrence);
        Assert.Null(wake.WakeTick);
    }

    [Fact]
    public void TheGoalWakeIgnoresOtherOwnersAndALapMakesItDueAtOnceAndAfterTheNewestRecord()
    {
        var bench = Scenario("capture").Bench;
        var goal = CaptureGoal(bench, new(1), new(2));
        var wake = new WorkshopGoalWake(goal, WorkshopPhysicsCompiler.Compile(bench, new(1, 2)));
        SimulationEventRecord Record(ulong sequence, ulong tick, ulong source) =>
            new(sequence, tick, SimulationEventKind.Impact, source, 1, checked((uint)(tick * Substeps)), 0f, 0f, 1, 0);
        wake.Observe(new(1, 0, [Record(1, 10, 9)]), new(9));
        Assert.False(wake.Due(new(50))); // a record from another owner never wakes the goal
        wake.Observe(new(1, 2, [Record(4, 12, 9)]), new(11));
        Assert.True(wake.Due(new(11))); // a lap may have lost the goal's record: evaluate now
        Assert.Equal(2UL, wake.Lapped);
        wake.Evaluated(new(11), solved: false);
        Assert.False(wake.Due(new(11)));
        Assert.True(wake.Due(new(12))); // and again once a read past every delivered record is installed
        wake.Evaluated(new(12), solved: false);
        wake.Observe(new(1, 1, []), new(20));
        Assert.Equal(21UL, wake.WakeTick); // nothing delivered: the next read
        // A goal whose sensor is not in the installed scene has nothing to wake it, and never faults the pump.
        var orphan = new WorkshopGoalWake(goal with { EventSource = new(999_999) }, WorkshopPhysicsCompiler.Compile(bench, new(1, 2)));
        orphan.Observe(new(1, 4, [Record(1, 10, 2)]), new(9));
        Assert.False(orphan.Due(new(50)));
    }

    // Free play compiles one capture sensor per ball, so the goal's sensor is not an authored identity; the wake still finds its frame.
    // An ActivatedAfter goal wakes on every record: its target may be an indicator that publishes nothing (proven on the authored
    // lessons below).
    [Fact]
    public void TheGoalWakeFindsItsOwnersThroughTheCompiledScene()
    {
        var bench = Scenario("capture").Bench;
        var scene = WorkshopPhysicsCompiler.Compile(bench, new(1, 2));
        var goal = CaptureGoal(bench, new(1), new(2));
        Assert.NotEqual(WorkshopPhysicsCompiler.CaptureSensor(new(2)), goal.EventSource);
        var wake = new WorkshopGoalWake(goal, scene);
        wake.Observe(new(1, 0, [new(1, 7, SimulationEventKind.Captured, 2, 1, 28, 0f, 0f, 0, 0)]), new(6));
        Assert.Equal(7UL, wake.WakeTick);
        var after = new WorkshopGoalWake(new(WorkshopGoalKind.ActivatedAfter, default, default, default, new(3), new(5)), scene);
        after.Observe(new(1, 0, [new(1, 4, SimulationEventKind.TriggerActivated, 3, 1, 16, 0f, 0f, 0, 0)]), new(3));
        Assert.Equal(4UL, after.WakeTick);
        after.Evaluated(new(4), solved: false);
        after.Observe(new(1, 0, [new(2, 9, SimulationEventKind.TriggerActivated, 77, 1, 36, 0f, 0f, 0, 0)]), new(8));
        Assert.Equal(9UL, after.WakeTick); // a record from any owner: the delay box upstream of the goal's lamp
        var none = new WorkshopGoalWake(default, scene);
        none.Observe(new(1, 0, [new(1, 4, SimulationEventKind.Captured, 2, 1, 16, 0f, 0f, 0, 0)]), new(3));
        Assert.False(none.Due(new(10)));
    }

    // Every authored goal lesson through the real ring: its authored solution's run solves on the wake at exactly the tick and the
    // occurrence the evaluator gives when polled on every committed read (the goal's targets include lamps that publish nothing), and
    // the unsolved bench (no solution placed) is never solved. The record precedes its read, as in the browser.
    [Theory]
    [InlineData("spring_forward")]
    [InlineData("bumper_sidekick")]
    [InlineData("bumper_depth")]
    [InlineData("wall_and_bumper")]
    [InlineData("domino_effect")]
    [InlineData("delayed_signal")]
    public async Task EveryAuthoredGoalSolvesOnItsWakeExactlyWhenPollingWould(string lesson)
    {
        foreach (var solved in new[] { true, false })
        {
            var bench = solved ? LaunchLessonSolveTests.AuthoredSolution(lesson, (Half).45)
                : PuzzleCatalogue.Create(new PuzzleId(lesson), new(2), WorkshopCadenceSettings.Default(), new(1), new((Half).45));
            var goal = bench.Puzzle.Goal;
            await using var ring = await Ring.Running(bench);
            var wake = new WorkshopGoalWake(goal, WorkshopPhysicsCompiler.Compile(bench, new(1, 2)));
            var cursor = new EventRingConsumer();
            var installed = ring.Simulation.Committed;
            var polled = new List<(ulong Tick, ActivationTime Occurrence)>();
            var woken = new List<(ulong Tick, ActivationTime Occurrence)>();
            var evaluations = 0;
            for (var tick = 0; tick < 1200 && woken.Count == 0; tick++)
            {
                await ring.Advance(1);
                var records = new List<SimulationEventRecord>();
                while (cursor.TryRead(ring.Bytes, SimulationDomain.None, out var record)) records.Add(record);
                wake.Observe(new(cursor.Epoch, 0, records.ToArray()), installed.Tick);
                installed = ring.Simulation.Committed;
                if (WorkshopGoalEvaluator.Occurrence(goal, installed, installed.Epoch) is { } truth) polled.Add((installed.Tick.Value, truth));
                if (!wake.Due(installed.Tick)) continue;
                evaluations++;
                var occurrence = WorkshopGoalEvaluator.Occurrence(goal, installed, installed.Epoch);
                wake.Evaluated(installed.Tick, occurrence is not null);
                if (occurrence is { } found) woken.Add((installed.Tick.Value, found));
            }
            Assert.Equal(polled.Take(1), woken);
            Assert.Equal(solved, woken.Count == 1);
            Assert.True(evaluations < 1200, $"{lesson}: the goal is evaluated on wakes ({evaluations}), not polled every tick");
        }
    }

    // ---------- 9A.25b D3: the bundle admits exactly the typed event bindings ----------

    [Fact]
    public void TheBundleRefusesEveryMalformedEventBinding()
    {
        var entry = DeclaredPhysics.Of(PartId.ImpactSwitch);
        var cue = entry.Presentation.Bindings[0];
        Assert.Empty(BundleValidator.Validate([new NamedEntry("impact_switch", entry)]));
        PresentationBinding[] invalid = [
            cue with { Event = SimulationEventKind.None },                                             // an event binding without its kind
            cue with { Event = SimulationEventKind.Captured },                                         // a kind the package does not produce
            cue with { Event = (SimulationEventKind)99 },                                              // an undefined kind
            cue with { Driver = PresentationDriver.CommittedActivation },                              // a kind on a committed driver
            cue with { Overlap = PresentationOverlap.SaturatingSum },                                  // SmoothStep must Replace
            cue with { Curve = PresentationCurve.SineSquaredPulse },                                   // a pulse must saturate
            cue with { Curve = PresentationCurve.Linear }, cue with { Duration = 0 },
            // A magnitude gate on a committed driver (an envelope that driver admits, so only the gate rule refuses it); an undefined gate.
            cue with { Driver = PresentationDriver.CommittedActivation, Event = SimulationEventKind.None, EventMagnitude = PresentationEventMagnitude.Positive },
            cue with { EventMagnitude = (PresentationEventMagnitude)9 }
        ];
        // The control: the committed driver with its envelope and no gate is admitted, so the case above fails only on the gate.
        Assert.Empty(BundleValidator.Validate([new NamedEntry("impact_switch", entry with { Presentation = entry.Presentation with {
            Bindings = [cue with { Driver = PresentationDriver.CommittedActivation, Event = SimulationEventKind.None }, .. entry.Presentation.Bindings.Skip(1)] } })]));
        foreach (var binding in invalid)
        {
            var changed = entry with { Presentation = entry.Presentation with { Bindings = [binding, .. entry.Presentation.Bindings.Skip(1)] } };
            Assert.NotEmpty(BundleValidator.Validate([new NamedEntry("impact_switch", changed)]));
        }
        // Every shipped event binding names a kind its own package declares, and the four packages are the bundle's only cues.
        var cued = TestBundle.Entries.Where(e => e.Presentation.Bindings.Any(binding => binding.Driver == PresentationDriver.Event))
            .Select(e => e.Metadata.Id).Order().ToArray();
        Assert.Equal(["ball_detector", "basket", "bumper", "switch"], cued);
        Assert.All(TestBundle.Entries, e => Assert.All(e.Presentation.Bindings.Where(binding => binding.Driver == PresentationDriver.Event),
            binding => Assert.Contains(e.Events, declared => declared.Kind == binding.Event)));
    }

    // ---------- 9A.23 D2: the shared JS reader equals the reference consumer over the same bytes ----------

    private const string ReaderScript = """
        import fs from 'node:fs';
        let text = ''; for await (const chunk of process.stdin) text += chunk;
        const request = JSON.parse(text);
        const { EventRingCursor } = await import(process.env.CURSOR_URL);
        const results = [];
        for (const hex of request.rings) {
            const bytes = Buffer.from(hex, 'hex');
            const shared = new SharedArrayBuffer(bytes.length); new Uint8Array(shared).set(bytes);
            const cursor = new EventRingCursor(); cursor.attach(shared);
            const read = cursor.read(cursor.epoch);
            results.push({ records: Buffer.from(read.records).toString('hex'), skipped: read.skipped.toString(), epoch: read.epoch.toString() });
        }
        process.stdout.write(JSON.stringify(results));
        """;

    /// <summary>Each ring copied into a SharedArrayBuffer and read once by the shipped reader, for its own epoch.</summary>
    private static JsonElement[] RunReader(byte[][] rings)
    {
        var path = Path.Combine(twodog.Engine.ResolveProjectDir(), "CuriousContraptions.web/wwwroot/event-ring-cursor.js");
        var start = new ProcessStartInfo("node") { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        start.ArgumentList.Add("--no-warnings"); start.ArgumentList.Add("--input-type=module"); start.ArgumentList.Add("-e"); start.ArgumentList.Add(ReaderScript);
        start.Environment["CURSOR_URL"] = new Uri(path).AbsoluteUri;
        using var node = Process.Start(start) ?? throw new InvalidOperationException("Node could not be started; the reader comparison needs it.");
        node.StandardInput.Write(JsonSerializer.Serialize(new { rings = rings.Select(Convert.ToHexString) }));
        node.StandardInput.Close();
        var stdout = node.StandardOutput.ReadToEndAsync(); var stderr = node.StandardError.ReadToEndAsync();
        Assert.True(node.WaitForExit(30_000), "The reader script did not finish within 30 s.");
        Assert.True(node.ExitCode == 0, $"The reader script failed ({node.ExitCode}): {stderr.Result}");
        return JsonDocument.Parse(stdout.Result).RootElement.EnumerateArray().ToArray();
    }

    [Fact]
    public async Task TheSharedJsReaderDeliversExactlyWhatTheReferenceConsumerReads()
    {
        await using var ring = await Ring.Running(Scenario("detector").Bench);
        await ring.Advance(480);
        var outbox = new EventRingOutbox();
        outbox.Restart(new(77), 4);
        outbox.Commit(ring.Records.Take(2).Concat(ring.Records.Take(2)).Concat(ring.Records.Take(2)).ToArray());
        byte[][] rings = [ring.Bytes!, EventRingBuffer.Append(null, outbox.Drain())];
        var results = RunReader(rings);
        for (var i = 0; i < rings.Length; i++)
        {
            var consumer = new EventRingConsumer();
            var expected = new List<SimulationEventRecord>();
            while (consumer.TryRead(rings[i], SimulationDomain.None, out var record)) expected.Add(record);
            var delivered = Convert.FromHexString(results[i].GetProperty("records").GetString()!);
            var actual = EventRingDelivery.Read([.. EventRingDelivery.Write(consumer.Epoch, consumer.Lapped, []), .. delivered]);
            Assert.Equal(expected, actual.Records);
            Assert.Equal(consumer.Lapped.ToString(System.Globalization.CultureInfo.InvariantCulture), results[i].GetProperty("skipped").GetString());
            Assert.Equal(consumer.Epoch.ToString(System.Globalization.CultureInfo.InvariantCulture), results[i].GetProperty("epoch").GetString());
        }
        // The overflowing ring: six written into four slots, so both readers skip the same two and deliver the newest four.
        Assert.Equal(("2", 4 * EventRingBuffer.SlotBytes), (results[1].GetProperty("skipped").GetString(),
            Convert.FromHexString(results[1].GetProperty("records").GetString()!).Length));
    }
}
