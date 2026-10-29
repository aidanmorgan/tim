using System.Diagnostics;
using System.Text.Json;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;

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
        public WorkshopIntegrationHarness Harness { get; }
        public WorkshopConstruction Construction { get; }
        public WorkshopDeclaredPresentation Presentation { get; private set; } = null!;
        public WorkshopWorldProjection World { get; private set; }
        public List<SimulationEventRecord> Delivered { get; } = [];
        private readonly EventRingConsumer _cursor = new();
        private ulong _publication, _lapped;

        /// <summary>Installs on a running ring the caller owns.</summary>
        public Animation(WorkshopIntegrationHarness ring, WorkshopConstruction construction) { Harness = ring; Construction = construction; Install(); }

        /// <summary>A non-retained installation (Run or Reset): a fresh evaluator at rest, anchored at the admitted read.</summary>
        private void Install()
        {
            var read = Harness.Committed;
            _publication = 1;
            Presentation = new(Construction, Response(read, _publication));
            World = new(read.Epoch, new(1), read.Tick, new(read.Capture!.Value.Time.Value), WorldPlayback.Running);
            Delivered.Clear();
        }

        /// <summary>One Service turn's read: every whole record since the cursor, as one delivery.</summary>
        public void Drain()
        {
            var records = new List<SimulationEventRecord>();
            while (_cursor.TryRead(Harness.Shared!, SimulationDomain.None, out var record)) records.Add(record);
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
                await Harness.Advance(1);
                Drain();
                Presentation.Accept(Response(Harness.Committed, ++_publication));
            }
        }

        public async Task ResetAndRun()
        {
            await Harness.Reset();
            await Harness.Run();
            Install();
            Drain();
        }

        /// <summary>The record's physical instant on the presentation clock, derived here (not by the evaluator): its ordinal plus phase
        /// (1/4096 of a 480 Hz step) since the anchored tick's first step, after the anchor's master time.</summary>
        public static double At(SimulationEventRecord record) => (record.Ordinal + record.Phase / 4096.0) / 480.0;

        public double Origin => World.AnchorTick.Value / 120.0;

        public float[] Sample(double at, GpuBodyId owner) =>
            Presentation.Advance(AnimationTestTime.Seconds(at)).Where(sample => sample.Owner == owner).OrderBy(sample => sample.Binding).Select(sample => F(sample.Value)).ToArray();
    }

    /// <summary>The declared one-shot values of <paramref name="part"/> at <paramref name="amount"/> of its envelope.</summary>
    private static float[] Cue(PartId part, double amount) =>
        DeclaredPhysics.Of(part).Presentation.Bindings.Select(binding => F(AnimationValue.Blend(
            WorkshopDeclaredPresentation.Value(binding.Property, binding.From), WorkshopDeclaredPresentation.Value(binding.Property, binding.To),
            new((float)amount)))).ToArray();

    private static float Duration(PartId part) => DeclaredPhysics.Of(part).Presentation.Bindings[0].Duration;

    // The quintic SmoothStep at the quarter points, exactly (the 9A.25a golden amounts).
    private static readonly double[] Quarter = [0, .103515625, .5, .896484375, 1];

    // ---------- the 9A.22 Impact records of the bumper run, after the occurrence rows become device-private (D9) ----------

    /// <summary>The bumper run's published records, and every hit row the device's private contact-work ledger held over the run (each
    /// committed tick's ledger holds its stores' latest rows; a sequence-0 row is the admission's empty slot), once each by sequence.</summary>
    private static async Task<(List<SimulationEventRecord> Records, ContactWorkOccurrence[] Hits)> BumperRun()
    {
        await using var ring = await WorkshopIntegrationHarness.Running(Bench([WorkshopInput.Generic(PartId.Basketball, new(1), 0, 4.5f, 0, 0, 0, 0, 1),
            PlacedParts.Bumper(new(2), 0, 3, 0)], []));
        await ring.Advance(360);
        return (ring.Records, ring.Ledgers.SelectMany(ledger => Enumerable.Range(0, ledger.OccurrenceCount).Select(ledger.Occurrence))
            .Where(hit => hit.Sequence > 0).DistinctBy(hit => hit.Sequence).OrderBy(hit => hit.Sequence).ToArray());
    }

    // The bench, from the declarations: a Ball (mass 1 kg, collider radius .34 m, restitution .55, linear drag .04/s, gravity 9.81 m/s²)
    // released at rest at y 4.5 above a static Bumper sphere (radius .65 m, restitution 1) at y 3, at the default strength 8 m/s: a store
    // of ½·1 kg·(8 m/s)² = 32 J. A contact bounces at the product of the restitutions and the store then pays toward 8 m/s normal speed.
    private const double G = 9.81, LinearDrag = .04, BounceRestitution = .55 * 1.0, Strength = 8, BallMass = 1, ReferenceMass = 1;
    private const double ContactHeight = 3 + .65 + .34; // the ball's centre when the spheres touch, the bumper directly below
    private const double StoreCapacity = .5 * ReferenceMass * Strength * Strength;

    /// <summary>Height and upward speed after <paramref name="t"/> s of vertical flight from <paramref name="y0"/> at upward speed
    /// <paramref name="v0"/>, under gravity and linear drag (the engine decays velocity by exp(−k·dt) each step).</summary>
    private static (double Height, double Speed) Flight(double y0, double v0, double t) =>
        (y0 + (v0 + G / LinearDrag) * (1 - Math.Exp(-LinearDrag * t)) / LinearDrag - G / LinearDrag * t,
            (v0 + G / LinearDrag) * Math.Exp(-LinearDrag * t) - G / LinearDrag);

    /// <summary>The flight time until a ball leaving <paramref name="y0"/> upward at <paramref name="v0"/> descends to the contact
    /// height: past the apex the descent is monotone, so it is bracketed from the apex and bisected.</summary>
    private static double Arrival(double y0, double v0)
    {
        bool Reached(double t) { var (y, v) = Flight(y0, v0, t); return v < 0 && y <= ContactHeight; }
        var lo = Math.Log(1 + LinearDrag * v0 / G) / LinearDrag;
        var hi = lo + 1;
        while (!Reached(hi)) (lo, hi) = (hi, hi + 1);
        for (var i = 0; i < 80; i++) { var mid = (lo + hi) / 2; if (Reached(mid)) hi = mid; else lo = mid; }
        return hi;
    }

    // The bumper run's Impact records, against the ballistic law and the store's ledger (Story 9A.22 records; 9A.23 D9 moves their
    // occurrence rows into the device's private ledger, and these records are what every consumer sees):
    // - hit 1: the dropped ball bounces at e·v₁ and the store pays the target-limited work ½m(8² − (e·v₁)²) < 32 J;
    // - hit 2: the ball, kicked to 8 m/s, returns at v₂; the target needs ½m(8² − (e·v₂)²), more than the remainder, so the store pays
    //   all it holds (the ledger closes: the two payments sum to the capacity);
    // - hit 3: the ball, launched at √((e·v₂)² + 2·paid₂/m), returns to an empty store: an Impact occurrence that pays nothing.
    // Tolerances: contact is resolved at 480 Hz step granularity, at most one 120 Hz tick from the analytic instant per contact, and each
    // launch inherits the earlier hits' offsets, so hit k lies within k ticks; a one-tick timing offset moves the impact speed by g/120,
    // so the paid work by m·e²·v·g/120.
    [Fact]
    public async Task TheBumperRunsImpactRecordsFollowTheBallisticLawAndTheStore()
    {
        var (records, rows) = await BumperRun();
        var impacts = records.Where(record => record.Kind == SimulationEventKind.Impact).ToArray();
        var t1 = Arrival(4.5, 0);
        var v1 = -Flight(4.5, 0, t1).Speed;
        var paid1 = .5 * BallMass * (Strength * Strength - Math.Pow(BounceRestitution * v1, 2));
        Assert.InRange(paid1, 0, StoreCapacity); // the derivation's premise: hit 1 is target-limited
        var flight2 = Arrival(ContactHeight, Strength);
        var v2 = -Flight(ContactHeight, Strength, flight2).Speed;
        var t2 = t1 + flight2;
        Assert.True(.5 * BallMass * (Strength * Strength - Math.Pow(BounceRestitution * v2, 2)) > StoreCapacity - paid1); // store-limited
        double[] times = [t1, t2, double.NaN];

        Assert.Equal(3, impacts.Length);
        Assert.All(impacts, record => Assert.Equal((SimulationEventKind.Impact, SimulationDomain.Mechanical, 2UL, 1UL, 0U),
            (record.Kind, record.Domain, record.Source, record.Other, record.Level))); // the bumper's records, striking the ball
        Assert.Equal(new ulong[] { 1, 2, 3 }, impacts.Select(record => record.Sequence).ToArray());
        Assert.Equal(new uint[] { 1, 2, 3 }, impacts.Select(record => record.Count).ToArray()); // the bumper's occurrence count
        static double Steps(SimulationEventRecord record) => record.Ordinal + record.Phase / 4096.0;
        foreach (var record in impacts) // each record lies inside its own tick's four physical steps
            Assert.True(record.Ordinal > (record.Tick - 1) * Substeps && record.Ordinal <= record.Tick * Substeps,
                $"Impact {record.Sequence}: ordinal {record.Ordinal} is outside tick {record.Tick}");
        for (var i = 1; i < impacts.Length; i++) // sequences ascend with the physical time, within a tick and across ticks
            Assert.True(impacts[i - 1].Tick < impacts[i].Tick || (impacts[i - 1].Tick == impacts[i].Tick && Steps(impacts[i - 1]) <= Steps(impacts[i])));

        var workTolerance = BallMass * BounceRestitution * BounceRestitution * v1 * G / 120;
        Assert.InRange((double)impacts[0].Magnitude, paid1 - workTolerance, paid1 + workTolerance);
        // The store-limited payment is everything the first left: their sum is the capacity, to the f32 rounding of the remainder (the
        // ledger may hold the remainder one f32 step low so that it never pays work it did not hold).
        var ulp = (double)(MathF.BitIncrement(32f) - 32f);
        Assert.InRange((double)impacts[0].Magnitude + impacts[1].Magnitude, StoreCapacity - 2 * ulp, StoreCapacity + 2 * ulp);
        Assert.Equal(0f, impacts[2].Magnitude); // the store is empty

        // D9 as behaviour: each Impact record is the device's private hit row, field by field, and the first hit's recorded approach
        // speed is the analytic impact speed within one tick's change of gravity.
        Assert.Equal(impacts.Length, rows.Length);
        foreach (var (hit, record) in rows.Zip(impacts))
            Assert.Equal((hit.Target.Value, hit.Sequence, hit.EventOrdinal, (float)hit.EventPhase, hit.Debit.Value),
                (record.Other, record.Count, record.Ordinal, record.Phase, record.Magnitude));
        Assert.InRange((double)rows[0].ApproachSpeed.Value, v1 - G / 120, v1 + G / 120);

        var launch3 = Math.Sqrt(Math.Pow(BounceRestitution * v2, 2) + 2 * impacts[1].Magnitude / BallMass);
        times[2] = t2 + Arrival(ContactHeight, launch3);
        for (var k = 0; k < 3; k++)
        {
            var tolerance = (k + 1) / 120.0;
            Assert.InRange(Steps(impacts[k]) / 480.0, times[k] - tolerance, times[k] + tolerance);
            Assert.InRange(impacts[k].Tick, (ulong)Math.Ceiling((times[k] - tolerance) * 120), (ulong)Math.Ceiling((times[k] + tolerance) * 120));
        }
    }

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
        var bench = Bench([WorkshopInput.Generic(new PartId(payload), ball, 0, (float)(height), 0, 0, 0, 0, 1),
            PlacedParts.Bumper(bumper, 0, 3, 0, (float)(strength))], []);
        await using var ring = await WorkshopIntegrationHarness.Running(bench);
        var run = new Animation(ring, bench);
        await run.Advance(ticks);
        var impacts = run.Delivered.Where(record => record.Kind == SimulationEventKind.Impact).ToArray();
        Assert.NotEmpty(impacts);
        Assert.Equal(run.Harness.Of(SimulationEventKind.Impact).Count(), impacts.Length);
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
                var elapsed = at - Animation.At(record);
                if (elapsed > 0 && elapsed < duration) { var sine = Math.Sin(Math.PI * elapsed / duration); sum = Math.Min(1, sum + sine * sine); }
            }
            return sum;
        }
        var times = impacts.SelectMany(record => Enumerable.Range(0, 5).Select(k => Animation.At(record) + duration * k / 4)).Order().ToArray();
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
        "detector" => (Bench([PlacedParts.Detector(new(1), 0, (float)(Floor + .62), 0, 0, 0, (fires ? -1 : 1) * (float)Half45, (float)(Half45)),
            PlacedParts.Lamp(new(2), 4, 1, 0, 0, 0, 0, 1), WorkshopInput.Generic(PartId.Basketball, new(3), 0, (float)(Floor + .34 + 3), 0, 0, 0, 0, 1)],
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
        await using var ring = await WorkshopIntegrationHarness.Running(bench);
        var run = new Animation(ring, bench);
        await run.Advance(ticks);
        var cues = run.Delivered.Where(record => record.Source == owner.Value && record.Kind == kind).ToArray();
        Assert.Equal(name == "detector" ? 2 : 1, cues.Length);
        Assert.Equal(run.Harness.Records.Count, run.Delivered.Count); // the cursor delivered every committed record exactly once
        var duration = (double)Duration(part);
        Assert.Equal(Cue(part, 0), run.Sample(run.Origin, owner)); // at rest before the first record
        foreach (var record in cues)
        {
            Assert.True(Animation.At(record) - run.Origin > duration, "each cue starts after the previous one completed");
            for (var k = 0; k <= 4; k++) Assert.Equal(Cue(part, Quarter[k]), run.Sample(Animation.At(record) + duration * k / 4, owner));
        }
        Assert.Equal(Cue(part, 1), run.Sample(Animation.At(cues[^1]) + 3 * duration, owner));
    }

    // Controls: a miss, a sub-threshold contact, a glancing or a reverse crossing publish nothing, so nothing is cued.
    [Theory]
    [MemberData(nameof(ClipScenarios))]
    public async Task WithoutItsRecordTheClipNeverLeavesRest(string name)
    {
        var (bench, owner, part, kind, ticks) = Scenario(name, fires: false);
        await using var ring = await WorkshopIntegrationHarness.Running(bench);
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
        await using var ring = await WorkshopIntegrationHarness.Running(bench);
        var run = new Animation(ring, bench);
        var tick = run.Harness.Committed.Tick.Value + 1;
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
        await using var ring = await WorkshopIntegrationHarness.Running(bench);
        var run = new Animation(ring, bench);
        var tick = run.Harness.Committed.Tick.Value + 2;
        var record = new SimulationEventRecord(1, tick, kind, owner.Value, 4, checked((uint)(tick * Substeps + 3)), 1024f, 1f, 0, 0);
        Assert.Equal(3.25 / WorkshopCadenceSettings.PhysicalFrequency,
            Animation.At(record) - Animation.At(record with { Ordinal = checked((uint)(tick * Substeps)), Phase = 0f }), 12);
        var timestamp = WorkshopDeclaredPresentation.EventTime(record);
        Assert.Equal((ulong)record.Ordinal, timestamp.Ordinal);
        Assert.Equal(1024f, timestamp.Phase);
        run.Presentation.AcceptEvents(new(run.World.WorldGeneration.Value, 0, [record]), run.World);
        var duration = (double)Duration(part);
        Assert.Equal(Cue(part, 0), run.Sample(Animation.At(record) - .001, owner));
        Assert.Equal(Cue(part, Quarter[2]), run.Sample(Animation.At(record) + duration / 2, owner));
    }

    // Pause, Resume and Step are retained rebinds: the cursor and the cues are kept, nothing is published by a Pause, and no record is
    // ever applied twice (a redelivered record is refused).
    [Fact]
    public async Task PauseResumeAndStepRetainTheCursorAndTheCuesWithNoDuplicate()
    {
        var (bench, owner, part, kind, _) = Scenario("switch");
        await using var ring = await WorkshopIntegrationHarness.Running(bench);
        var run = new Animation(ring, bench);
        await run.Advance(480);
        var press = Assert.Single(run.Delivered, record => record.Kind == kind);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await run.Harness.Execute(WorkshopCommandKind.Pause)).Result.Outcome);
        Assert.Empty(run.Harness.Publish());
        var paused = run.Harness.Committed;
        var endpoint = Response(paused, 0, projection: 2);
        Assert.True(run.Presentation.CanRetain(bench, endpoint));
        run.Presentation.Rebind(bench, endpoint);
        run.Drain();
        Assert.Equal(WorkshopCommandOutcome.Applied, (await run.Harness.Execute(WorkshopCommandKind.Step)).Result.Outcome);
        run.Harness.Publish(); run.Drain();
        Assert.Equal(WorkshopCommandOutcome.Applied, (await run.Harness.Execute(WorkshopCommandKind.Resume)).Result.Outcome);
        run.Harness.Publish(); run.Drain();
        Assert.Single(run.Delivered, record => record.Kind == kind);
        Assert.Throws<ArgumentException>(() => run.Presentation.AcceptEvents(new(run.World.WorldGeneration.Value, 0, [press]), run.World));
        Assert.Equal(Cue(part, 1), run.Sample(Animation.At(press) + 1, owner)); // the retained cue holds its end value
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
        await using var ring = await WorkshopIntegrationHarness.Running(bench);
        var run = new Animation(ring, bench);
        var duration = (double)Duration(part);
        var target = WorkshopDeclaredPresentation.Targets(bench).First(entry => entry.Owner == owner);
        var property = WorkshopDeclaredPresentation.Property(DeclaredPhysics.Of(part).Presentation.Bindings[0].Property);
        var tick = run.Harness.Committed.Tick.Value + 1;
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
        Assert.Empty(run.Sample(Animation.At(late) + duration / 8, owner));
        run.Presentation.SetVisible(target.Target, property, true);
        // Still running: exactly the phase it reached while hidden (a quarter of the clip, or the pulse's half-height).
        var expected = name == "bumper" ? Cue(part, .5) : Cue(part, Quarter[1]);
        var actual = run.Sample(Animation.At(late) + duration / 4, owner);
        Assert.Equal(expected.Length, actual.Length);
        // This bounded envelope evaluation includes phase normalization, elapsed/duration, sine-square and blending:
        // fewer than sixteen rounded f32 operations at the authored scalar magnitude. Terminal values above remain exact.
        for (var i = 0; i < expected.Length; i++)
        {
            var allowance = 16 * Math.ScaleB(1, -24) * Math.Max(1, Math.Abs(expected[i]));
            Assert.InRange(actual[i] - expected[i], -allowance, allowance);
        }
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
        await using var ring = await WorkshopIntegrationHarness.Running(bench);
        var run = new Animation(ring, bench);
        var duration = (double)Duration(part);
        Assert.Equal(Cue(part, 0), run.Sample(run.Origin + .2, owner)); // the clock has reached origin + 0.2 s
        var tick = run.Harness.Committed.Tick.Value + 1;
        // Its instant is a quarter of the cue's duration before the clock (to the nearest physical step).
        var late = new SimulationEventRecord(1, tick, DeclaredPhysics.Of(part).Presentation.Bindings[0].Event, owner.Value, 0,
            checked((uint)Math.Round((.2 - duration / 4) * WorkshopCadenceSettings.PhysicalFrequency + run.World.AnchorTick.Value * Substeps)), 0f, 1f, 0, 0);
        Assert.True(Animation.At(late) < run.Origin + .2);
        run.Presentation.AcceptEvents(new(run.World.WorldGeneration.Value, 0, [late]), run.World);
        // A half of its duration has passed since its own instant: the clip is half way, the pulse at its peak.
        Assert.Equal(Cue(part, name == "bumper" ? 1 : Quarter[2]), run.Sample(Animation.At(late) + duration / 2, owner));
    }

    // Reset (a non-retained installation of a new epoch) puts every one-shot target at rest and restarts the cursor at sequence 1; Run →
    // Reset → Run, and Save → Load → Run, each restart the cursor at sequence 1, capture the ball again at the same tick, and play the
    // declared cue from rest through its quarter points to its held end value.
    [Fact]
    public async Task ResetRestsEveryCueRestartsTheCursorAndRunsReplayTheCue()
    {
        var (bench, owner, part, kind, ticks) = Scenario("capture");
        var duration = (double)Duration(part);
        async Task<SimulationEventRecord> Replay(Animation run)
        {
            await run.Advance(ticks);
            var cue = Assert.Single(run.Delivered, record => record.Kind == kind);
            Assert.Equal((1UL, owner.Value), (run.Delivered[0].Sequence, cue.Source));
            Assert.Equal(Cue(part, 0), run.Sample(Animation.At(cue) - duration / 4, owner)); // at rest before the capture
            for (var k = 0; k <= 4; k++) Assert.Equal(Cue(part, Quarter[k]), run.Sample(Animation.At(cue) + duration * k / 4, owner));
            Assert.Equal(Cue(part, 1), run.Sample(Animation.At(cue) + duration * 5 / 4, owner)); // the end value holds
            return cue;
        }
        await using var ring = await WorkshopIntegrationHarness.Running(bench);
        var run = new Animation(ring, bench);
        await Replay(run);
        var epoch = run.World.WorldGeneration;
        await run.ResetAndRun();
        Assert.NotEqual(epoch, run.World.WorldGeneration);
        Assert.Equal(Cue(part, 0), run.Sample(run.Origin, owner)); // at rest after Reset
        await Replay(run);
        var restored = WorkshopSaveCodec.Decode(SaveFixture.Elements, WorkshopSaveCodec.Encode(new WorkshopSavedConstruction(bench, new(9)))).Construction;
        await using var loadedRing = await WorkshopIntegrationHarness.Running(restored);
        var loaded = new Animation(loadedRing, restored);
        await Replay(loaded);
    }

    // A lapped delivery (D6): the skip is counted, cumulative and typed; the lost cue is never replayed and the later cues are exact.
    [Fact]
    public async Task ALappedRingCountsItsSkipLosesOnlyThoseCuesAndKeepsTheRestExact()
    {
        var (bench, owner, part, kind, _) = Scenario("capture");
        await using var ring = await WorkshopIntegrationHarness.Running(bench);
        var run = new Animation(ring, bench);
        var tick = run.Harness.Committed.Tick.Value + 1;
        var epoch = run.World.WorldGeneration.Value;
        // Through the reference ring: six records into a ring of four; the reader skips the two the writer lapped.
        var published = Enumerable.Range(0, 6).Select(i => new SimulationEventRecord((ulong)i + 1, tick, kind, owner.Value, 1,
            checked((uint)(tick * Substeps + (ulong)i)), 0f, 0f, 0, 0)).ToArray();
        var lapped = EventRingBuffer.Append(null, EventRingBuffer.Batch(new(4, 64, epoch, 4), published.AsSpan(0, 4)));
        lapped = EventRingBuffer.Append(lapped, EventRingBuffer.Batch(new(4, 64, epoch, 6), published.AsSpan(4)));
        var cursor = new EventRingConsumer();
        var records = new List<SimulationEventRecord>();
        while (cursor.TryRead(lapped, SimulationDomain.None, out var record)) records.Add(record);
        Assert.Equal(2UL, cursor.Lapped);
        Assert.Equal([3UL, 4UL, 5UL, 6UL], records.Select(record => record.Sequence));
        run.Presentation.AcceptEvents(new(epoch, cursor.Lapped, records.ToArray()), run.World);
        Assert.Equal(2UL, run.Presentation.Lapped);
        var duration = (double)Duration(part);
        Assert.Equal(Cue(part, Quarter[2]), run.Sample(Animation.At(records[^1]) + duration / 2, owner)); // the newest cue, exactly
        run.Presentation.AcceptEvents(new(epoch, 3, []), run.World);
        Assert.Equal(5UL, run.Presentation.Lapped);
    }

    [Fact]
    public async Task ADeliveryOfAnotherEpochOrARecordAlreadyAppliedIsRefused()
    {
        var (bench, owner, _, kind, _) = Scenario("switch");
        await using var ring = await WorkshopIntegrationHarness.Running(bench);
        var run = new Animation(ring, bench);
        var tick = run.Harness.Committed.Tick.Value + 1;
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

    private static WorkshopGoal CaptureGoal(GpuBodyId ball, GpuBodyId receiver) =>
        new(WorkshopGoalKind.Captured, ball, receiver);

    // The goal's Captured record usually arrives before its read: the wake is held until a committed read of its tick is installed,
    // then the goal is evaluated once and its cue time is the record's own occurrence. A miss never wakes and never solves.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TheGoalWakeWaitsForItsCommittedReadThenSolvesOnceAtTheRecordsOccurrence(bool captures)
    {
        var (bench, receiver, _, _, ticks) = Scenario("capture", captures);
        GpuBodyId ball = new(1);
        var goal = CaptureGoal(ball, receiver);
        await using var ring = await WorkshopIntegrationHarness.Running(bench);
        var wake = new WorkshopGoalWake(goal, WorkshopPhysicsCompiler.Compile(bench, new(1, 2)));
        var cursor = new EventRingConsumer();
        var installed = ring.Committed;
        var evaluations = new List<(ulong Tick, ActivationTime? Occurrence)>();
        SimulationEventRecord? captured = null;
        for (var tick = 0; tick < ticks; tick++)
        {
            await ring.Advance(1);
            var records = new List<SimulationEventRecord>();
            while (cursor.TryRead(ring.Shared!, SimulationDomain.None, out var record)) records.Add(record);
            wake.Observe(new(cursor.Epoch, 0, records.ToArray()), installed.Tick);
            if (records.Any(record => record.Kind == SimulationEventKind.Captured))
            {
                captured = records.Single(record => record.Kind == SimulationEventKind.Captured);
                Assert.False(wake.Due(installed.Tick)); // its read is not installed yet: held
            }
            installed = ring.Committed;
            if (!wake.Due(installed.Tick)) continue;
            var occurrence = WorkshopGoalEvaluator.Occurrence(goal, installed, installed.Epoch, bench.Instances.Identities);
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
        var goal = CaptureGoal(new(1), new(2));
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
        var orphan = new WorkshopGoalWake(goal with { Target = new(999_999) }, WorkshopPhysicsCompiler.Compile(bench, new(1, 2)));
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
        var goal = CaptureGoal(new(1), new(2));
        Assert.True(bench.Instances.Identities.TryCapture(goal.Target, goal.Body, out var capture));
        Assert.Contains(scene.Sensors.ToArray(), sensor => sensor.Id == capture.Sensor && sensor.Frame == goal.Target && sensor.Targets.Contains(goal.Body, scene.Bodies.ToArray().Single(body => body.Id == goal.Body).Payload));
        var wake = new WorkshopGoalWake(goal, scene);
        wake.Observe(new(1, 0, [new(1, 7, SimulationEventKind.Captured, 2, 1, 28, 0f, 0f, 0, 0)]), new(6));
        Assert.Equal(7UL, wake.WakeTick);
        var after = new WorkshopGoalWake(new(WorkshopGoalKind.ActivatedAfter, default, default, new(3), new(5)), scene);
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
            var bench = solved ? PuzzleCatalogue.CreateSolution(new(lesson), new(2), WorkshopCadenceSettings.Default(), new(1), new(.45f))
                : PuzzleCatalogue.Create(new PuzzleId(lesson), new(2), WorkshopCadenceSettings.Default(), new(1), new(.45f));
            var goal = bench.Puzzle.Goal;
            await using var ring = await WorkshopIntegrationHarness.Running(bench);
            var wake = new WorkshopGoalWake(goal, WorkshopPhysicsCompiler.Compile(bench, new(1, 2)));
            var cursor = new EventRingConsumer();
            var installed = ring.Committed;
            var polled = new List<(ulong Tick, ActivationTime Occurrence)>();
            var woken = new List<(ulong Tick, ActivationTime Occurrence)>();
            var evaluations = 0;
            for (var tick = 0; tick < 1200 && woken.Count == 0; tick++)
            {
                await ring.Advance(1);
                var records = new List<SimulationEventRecord>();
                while (cursor.TryRead(ring.Shared!, SimulationDomain.None, out var record)) records.Add(record);
                wake.Observe(new(cursor.Epoch, 0, records.ToArray()), installed.Tick);
                installed = ring.Committed;
                if (WorkshopGoalEvaluator.Occurrence(goal, installed, installed.Epoch, bench.Instances.Identities) is { } truth) polled.Add((installed.Tick.Value, truth));
                if (!wake.Due(installed.Tick)) continue;
                evaluations++;
                var occurrence = WorkshopGoalEvaluator.Occurrence(goal, installed, installed.Epoch, bench.Instances.Identities);
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

    [Fact]
    public async Task DeclaredPulseDeliveryRetainsMoreThanSixtyFourOccurrencesAtTheirPhysicalTimes()
    {
        var bench = Bench([PlacedParts.Bumper(new(2), 0, 1, 0)], []);
        await using var ring = await WorkshopIntegrationHarness.Running(bench);
        var run = new Animation(ring, bench);
        var duration = Duration(PartId.PinballBumper);
        var later = checked((uint)Math.Ceiling((duration + 1) * 480));
        var records = Enumerable.Range(1, 96).Select(i => {
            var ordinal = i <= 64 ? 4u : later;
            return new SimulationEventRecord((ulong)i, (ordinal + 3u) / 4u, SimulationEventKind.Impact, 2, 1, ordinal, 0, 1, (uint)i, 0);
        }).ToArray();
        run.Presentation.AcceptEvents(new(run.World.WorldGeneration.Value, 0, records), run.World);
        var peak = Animation.At(records[^1]) + duration / 2;
        // The first64 have expired here. Dropping the later32 would incorrectly leave the target at rest.
        Assert.True(peak > Animation.At(records[63]) + duration);
        Assert.Equal(Cue(PartId.PinballBumper, 1), run.Sample(peak, new(2)));
        Assert.Equal(Cue(PartId.PinballBumper, 0), run.Sample(Animation.At(records[^1]) + duration + 1, new(2)));
    }

    [Fact]
    public async Task InvalidLaterCueLeavesEarlierCueAndSequenceUnconsumedThenValidRetrySucceeds()
    {
        var bench = Bench([PlacedParts.Bumper(new(2), 0, 1, 0)], []);
        await using var ring = await WorkshopIntegrationHarness.Running(bench);
        var run = new Animation(ring, bench);
        var first = new SimulationEventRecord(1, 120, SimulationEventKind.Impact, 2, 1, 480, 0, 1, 1, 0);
        var second = first with { Sequence = 2, Count = 2 };
        Assert.ThrowsAny<ArgumentException>(() => run.Presentation.AcceptEvents(
            new(run.World.WorldGeneration.Value, 3, [first, second with { Source = 0 }]), run.World));
        Assert.Equal(0UL, run.Presentation.Lapped);
        Assert.Equal(Cue(PartId.PinballBumper, 0), run.Sample(Animation.At(first) + Duration(PartId.PinballBumper) / 4, new(2)));
        run.Presentation.AcceptEvents(new(run.World.WorldGeneration.Value, 3, [first, second]), run.World);
        Assert.Equal(3UL, run.Presentation.Lapped);
        Assert.Equal(Cue(PartId.PinballBumper, 1), run.Sample(Animation.At(first) + Duration(PartId.PinballBumper) / 2, new(2)));
    }

    // ---------- 9A.23 D2: the shared JS reader equals the reference consumer over the same bytes ----------


    /// <summary>Each ring copied into a SharedArrayBuffer and read once by the shipped reader, for its own epoch.</summary>
    private static JsonElement[] RunReader(byte[][] rings)
    {
        var path = Path.Combine(twodog.Engine.ResolveProjectDir(), ".runtime-build/tools/runtime/event-roundtrip.js");
        var start = new ProcessStartInfo("node") { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, WorkingDirectory = twodog.Engine.ResolveProjectDir() };
        start.ArgumentList.Add("--experimental-vm-modules"); start.ArgumentList.Add("--no-warnings"); start.ArgumentList.Add(path);
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
        await using var ring = await WorkshopIntegrationHarness.Running(Scenario("detector").Bench);
        await ring.Advance(480);
        var published = ring.Records.Take(2).Concat(ring.Records.Take(2)).Concat(ring.Records.Take(2))
            .Select((record, i) => record with { Sequence = (ulong)i + 1 }).ToArray();
        var lapped = EventRingBuffer.Append(null, EventRingBuffer.Batch(new(4, 64, 77, 4), published.AsSpan(0, 4)));
        lapped = EventRingBuffer.Append(lapped, EventRingBuffer.Batch(new(4, 64, 77, 6), published.AsSpan(4)));
        EventRingMemory[] rings = [ring.Shared!, lapped];
        var results = RunReader(rings.Select(shared => shared.Snapshot()).ToArray());
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

    // ---------- review F1 / R2-F2 and the owner's encapsulation: only EventRingMemory touches the ring, atomically ----------

    private static string Source(string path) => File.ReadAllText(Path.Combine(twodog.Engine.ResolveProjectDir(), path)).ReplaceLineEndings("\n");

    /// <summary>The text of a top-level declaration, from its first line to the first line that is a lone closing brace.</summary>
    private static string Declaration(string text, string start)
    {
        var from = text.IndexOf(start, StringComparison.Ordinal);
        Assert.True(from >= 0, $"'{start}' is missing");
        return text[from..text.IndexOf("\n}\n", from, StringComparison.Ordinal)];
    }

    private static int Count(string text, string pattern) => System.Text.RegularExpressions.Regex.Count(text, pattern);

    // Every typed-array or DataView construction in a source, as "Type(first argument", in order.
    private static string[] Views(string text) => System.Text.RegularExpressions.Regex.Matches(text, @"new (\w+Array|DataView)\(\s*([\w$#.]*)")
        .Select(match => $"{match.Groups[1].Value}({match.Groups[2].Value}").ToArray();

    // Every `.buffer` access in a source, as its receiver expression, in order.
    private static string[] Buffers(string text) => System.Text.RegularExpressions.Regex.Matches(text, @"([\w$#]+(?:\.[\w$#]+)*(?:\(\))?)\.buffer\b")
        .Select(match => match.Groups[1].Value).ToArray();

    // The ring's JS files: the module that owns its memory, and every production script that imports it.
    private const string RingModule = "CuriousContraptions.web/wwwroot/event-ring-cursor.ts";
    private const string SimulationWorker = "CuriousContraptions.Simulation/wwwroot/worker.ts";
    private const string AnimationWorker = "CuriousContraptions.Animation.Worker/wwwroot/worker.ts";
    private const string WorkshopClient = "CuriousContraptions.web/wwwroot/workshop-client.ts";

    // Each file's complete list of views and `.buffer` accesses (pose ring, clock ring, wire and sample buffers); none is over the event
    // ring, whose one view is the wrapper's `BigUint64Array(buffer` in the module. A new view or `.buffer` access anywhere in these files
    // (the reviewer's G3 `events.ring.buffer` in the client, G4 `const { buffer: shared } = eventRing` in the worker) fails until it is
    // reviewed and listed here; the ring itself must go through EventRingMemory.
    private static readonly (string Path, string[] Views, string[] Buffers)[] RingFiles =
    [
        (RingModule,
            ["Uint8Array(0", "BigUint64Array(buffer", "BigUint64Array(EVENT_SLOT_BYTES", "BigUint64Array(records.length", "Uint8Array(words.buffer",
                "Uint8Array(EVENT_DELIVERY_BYTES", "DataView(delivery.buffer"],
            ["words", "delivery"]),
        (SimulationWorker,
            ["DataView(batch.buffer", "Uint8Array(batch", "BigUint64Array(copy.buffer", "Uint8Array(bytes", "Uint8Array(bytes", "Uint8Array(bytes",
                "Uint8Array(sequenceBytes", "Uint8Array(bytes", "Uint8Array(identity", "Uint8Array(poseSlot",
                "Uint8Array(host.Bootstrap", "Uint8Array(host.AnimationPeer", "Float64Array(nativeClockEvidence", "Uint8Array(host.AnimationPeer"],
            ["batch", "copy", "bytes", "bytes", "read.bytes", "read.identity", "observation", "res", "sequence", "committed", "nativeClock",
                "masterPeer", "animationPeer"]),
        (AnimationWorker,
            ["Uint8Array(bytes", "Uint8Array(bytes", "Uint8Array(bytes", "Uint8Array(bytes", "DataView(value.buffer", "DataView(value.buffer", "Float64Array(nativeClockEvidence"],
            ["value", "observation", "value", "value", "value", "value", "nativeClock"]),
        (WorkshopClient,
            ["DataView(bytes.buffer", "Uint8Array(bootstrapBytes",
                // Story 9A.44: the Animation output is read through one DataView over its named offsets.
                "Uint8Array(bootstrapBytes", "DataView(item.animationOutput.buffer",
                "DataView(masterPeer.buffer", "Uint8Array(bytes", "Uint8Array(bytes", "Uint8Array(bytes", "Uint8Array(sequenceBytes", "Uint8Array(bytes",
                "Uint8Array(bytes"],
            ["bytes", "bootstrap", "item.animationOutput", "animationAcknowledged", "masterPeer", "bootstrap", "message.readIdentity",
                "scheduleControl", "animationControl", "owned"]),
    ];

    // Architecture guard (docs/testing-guidance.md): a plain (non-atomic) access on either side of the ring behaves identically under any
    // single-threaded interleaving, and the concurrent stress tests catch a one-sided regression only by chance on a weakly ordered CPU
    // (the reviewer's one-sided mutants survived 0 of 20 runs each). The encapsulation makes the violation impossible through the public
    // API (EventRingMemory has no raw accessor, the cursor's ring is private; the node harness checks both through the API), and this guard
    // checks it is respected, in each language: the raw view over the ring's memory is constructed in exactly one place, the
    // EventRingMemory wrapper, and each of the wrapper's accessors is an atomic whole-word access.
    [Fact]
    public void GuardOnlyTheEventRingMemoryViewsTheRingAndEveryAccessorIsAtomic()
    {
        // JS: the files that touch the ring are the module and exactly the production scripts that import it.
        var root = twodog.Engine.ResolveProjectDir();
        string[] scripts = ["CuriousContraptions.Simulation/wwwroot", "CuriousContraptions.Animation.Worker/wwwroot", "CuriousContraptions.web/wwwroot"];
        var importers = scripts.SelectMany(folder => Directory.EnumerateFiles(Path.Combine(root, folder), "*.ts", SearchOption.TopDirectoryOnly))
            .Where(path => File.ReadAllText(path).Contains("event-ring-cursor.js", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/')).Order().ToArray();
        Assert.Equal(new[] { AnimationWorker, SimulationWorker, WorkshopClient }.Order(), importers);
        // Every receiver of the posted buffer is one of the declared ring owners.
        var receivers = scripts.SelectMany(folder => Directory.EnumerateFiles(Path.Combine(root, folder), "*.ts", SearchOption.TopDirectoryOnly))
            .Where(path => System.Text.RegularExpressions.Regex.IsMatch(File.ReadAllText(path), @"\beventRing\b"))
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/')).Order().ToArray();
        Assert.Equal(new[] { RingModule, AnimationWorker, SimulationWorker, WorkshopClient }.Order(), receivers);
        foreach (var (path, views, buffers) in RingFiles)
        {
            var text = Source(path);
            Assert.True(views.SequenceEqual(Views(text)), $"{path}: the typed-array/DataView constructions changed: {string.Join(", ", Views(text))}");
            Assert.True(buffers.SequenceEqual(Buffers(text)), $"{path}: the .buffer accesses changed: {string.Join(", ", Buffers(text))}");
        }
        // The module's one view over an existing shared buffer is the wrapper's, its view is private, and each use of it is one
        // Atomics.load or Atomics.store (its declaration and assignment aside; a private field is unreachable outside the class).
        var module = Source(RingModule);
        var memory = Declaration(module, "export class EventRingMemory {");
        Assert.Contains("this.#words = new BigUint64Array(buffer);", memory);
        Assert.Equal(2 + Count(memory, @"Atomics\.(load|store)\((this|ring)\.#words, "), Count(memory, "#words"));
        // The preallocated private message is the only path that exposes the shared buffer.
        Assert.Contains("this.#message = { eventRing: buffer };", memory);
        Assert.Contains("postTo(target: EventRingRecipient) { target.postMessage(this.#message); }", memory);
        Assert.Equal(3, Count(memory, "#message"));

        // C#: the reference ring's memory.
        var reference = Source("CuriousContraptions.Abstractions/EventRingBuffer.cs");
        var wrapper = Declaration(reference, "public sealed class EventRingMemory");
        // The raw word view is made once, in the wrapper's private Word accessor, over its private bytes.
        Assert.Equal(1, Count(reference, @"Unsafe\.As<"));
        Assert.Contains("private ref ulong Word(int offset) => ref Unsafe.As<byte, ulong>(ref _memory[offset]);", wrapper);
        Assert.Equal(["private readonly byte[] _memory;", "_memory = new byte[", "_memory.Length", "ref _memory[offset]"],
            System.Text.RegularExpressions.Regex.Matches(wrapper, @"private readonly byte\[\] _memory;|_memory = new byte\[|_memory\.Length|ref _memory\[offset\]|_memory")
                .Select(match => match.Value));
        // Every use of Word, but its definition, is one Volatile read or write; nothing outside the wrapper uses Volatile on the ring.
        Assert.Equal(1 + Count(wrapper, @"Volatile\.(Read|Write)\(ref (memory\.)?Word\("), Count(wrapper, @"\bWord\("));
        Assert.Equal(Count(wrapper, @"Volatile\."), Count(reference, @"Volatile\."));
    }
}
