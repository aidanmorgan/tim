using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>CAT-015 Pinball Bumper (Story 9A.33) on the production compiler and the worker engine: the finite contact-work store
/// its strength declares, paid and unpaid contacts, the depth-plane miss and the electrical recharge. Every bench is built through
/// <see cref="PlacedParts"/>, so the same assertions ran on the record path before the conversion.</summary>
public sealed class ContactWorkInstanceTests
{
    private const int TicksPerSecond = 120;
    private static readonly GpuBodyId Ball = new(1), Owner = new(2), Source = new(3);

    private static WorkshopConstruction Free(params IWorkshopInstance[] instances) =>
        new(new(2), WorkshopCadenceSettings.Default(), new(instances));

    private static WorkshopConstruction Drop(PartId payload, double height, float strength, double z = 0) => Free(
        WorkshopInput.Generic(payload, Ball, 0, (float)(height), (float)(z), 0, 0, 0, 1), PlacedParts.Bumper(Owner, 0, 3, 0, (float)(strength)));

    /// <summary>Each new occurrence row of the device-private ledgers (Story 9A.23 D9), once.</summary>
    private static IEnumerable<ContactWorkOccurrence> Occurrences(IEnumerable<ContactWorkLedger> ledgers)
    {
        var seen = new HashSet<uint>();
        foreach (var ledger in ledgers)
            for (var i = 0; i < ledger.OccurrenceCount; i++)
            {
                var occurrence = ledger.Occurrence(i);
                if (occurrence.Sequence != 0 && seen.Add(occurrence.Sequence)) yield return occurrence;
            }
    }

    private static PhysicsBodyRead Body(WorkshopRead read, GpuBodyId id)
    {
        for (var i = 0; i < read.Bodies.Count; i++) if (read.Bodies[i].Body.Id == id) return read.Bodies[i];
        throw new ArgumentException($"Body {id.Value} is not read.");
    }

    private static float Speed(PhysicsBodyRead body) => MathF.Sqrt(body.Body.Velocity.X * body.Body.Velocity.X +
        body.Body.Velocity.Y * body.Body.Velocity.Y + body.Body.Velocity.Z * body.Body.Velocity.Z);

    // ---------- compiled store ----------

    // Preload = (½·m)·s·s in f32 with the declared 1 kg reference mass, at both ends, the default and a full-precision
    // strength; threshold 0.05 m/s and the 72-step cooldown come with it.
    [Theory]
    [InlineData(0f)]
    [InlineData(8f)]
    [InlineData(20f)]
    [InlineData(3.3333333f)]
    public void TheCompiledStoreHoldsTheDeclaredPreload(float strength)
    {
        var scene = WorkshopPhysicsCompiler.Compile(Drop(new PartId("ball"), 4.5, strength), new(1, 2));
        var work = Assert.Single(scene.ContactWorks.ToArray());
        var half = .5f * 1f;
        Assert.Equal(half * strength * strength, work.InitialEnergy.Value);
        Assert.Equal(strength, work.TargetSpeed.Value);
        Assert.Equal((Drop(new PartId("ball"), 4.5, strength).Instances.Identities[Owner].Work, Owner, new BodyTargetSet(BodyTargetKind.AllDynamic, default)),
            (work.Id, work.Owner, work.Targets));
        Assert.Equal((.05f, 72u), (work.Threshold.Value, work.CooldownPhysicalSteps));
    }

    // Every store value comes from the declared capability, not from the Bumper's numbers: a synthetic capability (2 kg reference mass,
    // 0.5 m/s threshold, 100-step cooldown, its own key) compiles exactly those values.
    [Fact]
    public void TheCompiledStoreTakesEveryValueFromTheDeclaredCapability()
    {
        var descriptor = new CuriousContraptions.Abstractions.Mechanical.ContactWorkDescriptor("speed", 2f, .5f, 100);
        var targets = new BodyTargetSet(BodyTargetKind.NamedBody, new(9));
        var work = WorkshopPhysicsCompiler.ContactWorkFor(new(10), new(7), targets, descriptor, new([new(new("speed"), 3f)]));
        Assert.Equal((new GpuContactWorkId(10), new GpuBodyId(7), targets, 3f, 9f, .5f, 100u),
            (work.Id, work.Owner, work.Targets, work.TargetSpeed.Value, work.InitialEnergy.Value, work.Threshold.Value, work.CooldownPhysicalSteps));
        Assert.ThrowsAny<KeyNotFoundException>(() => WorkshopPhysicsCompiler.ContactWorkFor(new(10), new(7), targets, descriptor,
            new([new(new("strength"), 3f)])));
    }

    // Only a part declaring the capability gets a store: a Wall, a Receiver and an Impact Switch on the same bench compile none.
    [Fact]
    public void OnlyTheDeclaringPartCompilesAStore()
    {
        var scene = WorkshopPhysicsCompiler.Compile(Free(WorkshopInput.Generic(new PartId("ball"), Ball, 0, 4.5f, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(new PartId("wall"), new(3), 4, 1, 0, 0, 0, 0, 1, WallFixture.Default),
            WorkshopInput.Generic(new PartId("basket"), new(4), -4, 1, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(new PartId("switch"), new(5), 8, 1, 0, 0, 0, 0, 1),
            PlacedParts.Bumper(Owner, 0, 3, 0)), new(1, 2));
        Assert.Equal(Owner, Assert.Single(scene.ContactWorks.ToArray()).Owner);
    }

    // Exact Run/Reset and Save/Load: the store, every body and every occurrence restore their authored fields, and the reloaded Bumper replays.
    [Fact]
    public async Task RunResetAndSaveLoadAreExact()
    {
        var bench = Drop(new PartId("ball"), 4.5, 12.5f);
        var (built, run, reset) = await WorkshopIntegrationHarness.ObserveReset(bench, 2 * TicksPerSecond);
        // A paid hit during the run: the store counted it and spent energy (its occurrence row is device-private, Story 9A.23 D9).
        Assert.Contains(run, read => read.ContactWorks[0].OccurrenceCount > 0 &&
            read.ContactWorks[0].RemainingEnergy.Value < built.ContactWorks[0].RemainingEnergy.Value);
        NoFixedCapacityTests.AssertSameState(built, reset, "Reset");
        Assert.Equal(built.ContactWorks[0], reset.ContactWorks[0]);
        var saved = new WorkshopSavedConstruction(bench, new(3));
        var bytes = WorkshopSaveCodec.Encode(saved);
        var reloaded = WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes);
        Assert.Equal(saved, reloaded);
        var (_, replay, _) = await WorkshopIntegrationHarness.ObserveReset(reloaded.Construction, 2 * TicksPerSecond);
        Assert.Contains(replay, read => read.ContactWorks[0].Owner == Owner && read.ContactWorks[0].OccurrenceCount > 0 &&
            read.ContactWorks[0].RemainingEnergy.Value < built.ContactWorks[0].RemainingEnergy.Value);
    }

    [Fact]
    public void StrengthEndpointsHoldAnEmptyAndATwoHundredJouleStore()
    {
        Assert.Equal(0f, Assert.Single(WorkshopPhysicsCompiler.Compile(Drop(new PartId("ball"), 4.5, 0f), new(1, 2)).ContactWorks.ToArray()).InitialEnergy.Value);
        Assert.Equal(200f, Assert.Single(WorkshopPhysicsCompiler.Compile(Drop(new PartId("ball"), 4.5, 20f), new(1, 2)).ContactWorks.ToArray()).InitialEnergy.Value);
    }

    // ---------- real simulation ----------

    // Strength 8: the 1 kg ball leaves its first paid contact at the 8 m/s target; the ledger closes exactly against the 32 J store.
    [Fact]
    public async Task StrengthEightLaunchesTheBallPaidAtTheTargetSpeedAndTheLedgerCloses()
    {
        var (reads, ledgers) = await WorkshopIntegrationHarness.ObserveAccounting(Drop(new PartId("ball"), 4.5, 8f), 2 * TicksPerSecond);
        Assert.Equal(32f, reads[0].ContactWorks[0].RemainingEnergy.Value);
        var index = ledgers.FindIndex(ledger => ledger.OccurrenceCount > 0 && ledger.Occurrence(0).Sequence == 1);
        Assert.True(index > 0, "the ball reaches the Bumper");
        var first = ledgers[index].Occurrence(0);
        Assert.Equal(ContactWorkEffect.Paid, first.Effect);
        Assert.Equal(32f, reads[index].ContactWorks[0].RemainingEnergy.Value + first.Debit.Value);
        // The read closes the tick after the event: gravity acts for at most one tick (≤ 9.81/120 m/s) after the 8 m/s launch. The
        // zero-gravity > 7.99 m/s radial launch itself is CAT-015 fact 1 (WorkshopPhysicsLawSensorWorkTests).
        var launch = Speed(Body(reads[index], Ball));
        Assert.True(launch <= 8.0001f && launch + 9.81f / TicksPerSecond > 7.99f, $"paid launch speed {launch} m/s");
    }

    // The 4 kg ball can afford only part of the boost: its first paid contact spends the whole 32 J store and leaves below the
    // target; every later contact with the empty store spends nothing and is an ordinary bounce (the ball still rises).
    [Fact]
    public async Task APartiallyAffordableBoostEmptiesTheStoreAndALaterContactIsAnOrdinaryUnpaidBounce()
    {
        var (reads, ledgers) = await WorkshopIntegrationHarness.ObserveAccounting(Drop(new PartId("bowling"), 6, 8f), 4 * TicksPerSecond);
        var occurrences = Occurrences(ledgers).ToArray();
        Assert.True(occurrences.Length >= 2, "the ball returns to the empty Bumper");
        Assert.Equal((32f, ContactWorkEffect.Paid), (occurrences[0].Debit.Value, occurrences[0].Effect));
        Assert.All(occurrences.Skip(1), later => Assert.Equal((0f, ContactWorkEffect.Passive), (later.Debit.Value, later.Effect)));
        var paidAt = ledgers.FindIndex(ledger => ledger.OccurrenceCount > 0 && ledger.Occurrence(0).Sequence == 1);
        Assert.True(Speed(Body(reads[paidAt], Ball)) < 7.99f, "the heavy ball cannot reach the target");
        var laterAt = ledgers.FindIndex(ledger => ledger.OccurrenceCount > 0 && ledger.Occurrence(0).Sequence == 2);
        Assert.Contains(reads.Skip(laterAt).Take(TicksPerSecond / 4), read => Body(read, Ball).Body.Velocity.Y > 0f);
        Assert.Equal(0f, reads[^1].ContactWorks[0].RemainingEnergy.Value);
    }

    [Fact]
    public async Task StrengthZeroIsAnEmptyStoreWhoseContactsAreUnpaid()
    {
        var (reads, ledgers) = await WorkshopIntegrationHarness.ObserveAccounting(Drop(new PartId("ball"), 4.5, 0f), 2 * TicksPerSecond);
        var occurrences = Occurrences(ledgers).ToArray();
        Assert.NotEmpty(occurrences);
        Assert.All(occurrences, occurrence => Assert.Equal((0f, ContactWorkEffect.Passive), (occurrence.Debit.Value, occurrence.Effect)));
        Assert.All(reads, read => Assert.Equal(0f, read.ContactWorks[0].RemainingEnergy.Value));
    }

    // Another depth plane: the ball falls past the Bumper 2 m behind it and never qualifies; the store stays whole.
    [Fact]
    public async Task ABallInAnotherDepthPlaneNeverPays()
    {
        var (reads, ledgers) = await WorkshopIntegrationHarness.ObserveAccounting(Drop(new PartId("ball"), 4.5, 8f, z: 2), 2 * TicksPerSecond);
        Assert.Empty(Occurrences(ledgers));
        Assert.All(reads, read => Assert.Equal(32f, read.ContactWorks[0].RemainingEnergy.Value));
    }

    [Theory]
    [InlineData(false, 8f)]
    [InlineData(true, 8f)]
    [InlineData(false, 0f)]
    [InlineData(true, 0f)]
    public async Task ExposedCapContactsUseTheFiniteStoreAndOnlyPaidWorkEmitsAPulse(bool heavy, float strength)
    {
        var payload = heavy ? new PartId("bowling") : new PartId("ball");
        // The face at x=.2 is exposed above the sphere. Starting .2 m above the cap
        // gives a slow incoming bounce: a 4 kg payload needs more than 32 J to reach 8 m/s.
        var radius = heavy ? .28 : .34;
        var bench = Free(WorkshopInput.Generic(payload, Ball, .2f, (float)(3 + .65 + radius + .2), 0, 0, 0, 0, 1),
            PlacedParts.Bumper(Owner, 0, 3, 0, (float)(strength)));
        await using var ring = await WorkshopIntegrationHarness.Running(bench);
        await ring.Advance(480);
        var hits = Occurrences(ring.Ledgers).ToArray();
        Assert.NotEmpty(hits);
        var paid = hits.Where(hit => hit.Effect == ContactWorkEffect.Paid).ToArray();
        var pulses = ring.Records.Where(record => record.Kind == CuriousContraptions.Abstractions.SimulationEventKind.Impact &&
            record.Source == Owner.Value && record.Magnitude > 0).ToArray();
        Assert.Equal(paid.Length, pulses.Length);
        // Repeated cap contacts retain the owner's per-target cooldown.
        for (var i = 1; i < hits.Length; i++)
            Assert.True(hits[i].EventOrdinal >= hits[i - 1].EventOrdinal + 72,
                "contacts on one bumper share the 72-step target cooldown");
        Assert.All(ring.Reads, read => Assert.InRange(read.ContactWorks[0].RemainingEnergy.Value, 0f, .5f * strength * strength));
        if (strength == 0)
        {
            Assert.Empty(paid);
            Assert.All(hits, hit => Assert.Equal(0f, hit.Debit.Value));
        }
        else
        {
            Assert.NotEmpty(paid);
            if (heavy)
            {
                Assert.Equal(32f, paid[0].Debit.Value);
                Assert.Single(paid);
                Assert.Contains(hits, hit => hit.Effect == ContactWorkEffect.Passive);
                Assert.Equal(0f, ring.Reads[^1].ContactWorks[0].RemainingEnergy.Value);
            }
        }
    }

    // Recharge: a battery wired to PowerIn credits the emptied store; the same battery unwired credits nothing.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ADirectlyWiredBatteryRechargesTheStoreAndAnUnwiredOneDoesNot(bool wired)
    {
        var battery = PlacedParts.Battery(Source, 3, 1, 0, powerLimit: 10f);
        var bench = Drop(new PartId("bowling"), 6, 8f);
        bench = bench with { Instances = bench.Instances.With(battery) };
        if (wired) bench = bench with { Connections = new(new WorkshopConnection(Source, WorkshopSocket.Supply, Owner, WorkshopSocket.PowerIn,
            WorkshopConnectionDomain.Electrical)) };
        var scene = WorkshopPhysicsCompiler.Compile(bench, new(1, 2));
        Assert.Equal(wired ? 1 : 0, scene.Electrical.Bindings.Length);
        var reads = await WorkshopIntegrationHarness.Observe(bench, 4 * TicksPerSecond);
        var emptied = reads.FindIndex(read => read.ContactWorks[0].RemainingEnergy.Value == 0f);
        Assert.True(emptied > 0, "the heavy ball empties the store");
        var later = reads[^1].ContactWorks[0];
        if (wired) Assert.True(later.RemainingEnergy.Value > 0f || later.SuppliedEnergy.Value > 0f, $"recharged store {later.RemainingEnergy.Value} J");
        else Assert.Equal((0f, 0f), (later.RemainingEnergy.Value, later.SuppliedEnergy.Value));
    }

    // Routing a store through a switch is refused: a finite-work store needs one direct declared source.
    [Fact]
    public void SwitchedStoreRoutingIsRefused()
    {
        var battery = PlacedParts.Battery(Source, 3, 1, 0);
        var toggle = WorkshopInput.Generic(new PartId("switch"), new(4), 5, 1, 0, 0, 0, 0, 1);
        var bench = Free(PlacedParts.Bumper(Owner, 0, 3, 0), battery, toggle) with { Connections = new(
            new WorkshopConnection(Source, WorkshopSocket.Supply, toggle.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical),
            new WorkshopConnection(toggle.Id, WorkshopSocket.Supply, Owner, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical)) };
        EditRefusal.Is(WorkshopEditRejection.PowerInputSupplied, () => WorkshopPhysicsCompiler.Compile(bench, new(1, 2)));
    }

    [Fact]
    public async Task ARealCapThenRingContactSharesTheCooldownEvenWithRechargedWorkAvailable()
    {
        const float degrees = 25, strength = 2, radius = .28f;
        var angle = degrees * MathF.PI / 180;
        var rotation = System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitZ, angle);
        WorkshopConstruction Bench(double x) => Free(
            WorkshopInput.Generic(new PartId("bowling"), Ball, (float)(x), 5.5f, 0, 0, 0, 0, 1),
            PlacedParts.Bumper(Owner, 0, 3, 0, (float)(strength), qz: rotation.Z, qw: rotation.W),
            PlacedParts.Battery(Source, 3, 1, 0)) with
        {
            Connections = new(new WorkshopConnection(Source, WorkshopSocket.Supply, Owner,
                WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical))
        };
        var bench = Bench(.1);
        var scene = WorkshopPhysicsCompiler.Compile(bench, new(1, 2));
        ColliderShapeKind Shape(ContactWorkOccurrence hit) => scene.Colliders[(int)hit.Collider.Value].Shape;
        await using var ring = await WorkshopIntegrationHarness.Running(bench);
        await ring.Advance(120); // one second suffices for a drop and adjacent-child contact
        var first = Occurrences(ring.Ledgers).First();
        Assert.Equal((Ball, ColliderShapeKind.Cylinder, ContactWorkEffect.Paid), (first.Target, Shape(first), first.Effect));

        // Independent distances to the literal authored solids identify the second
        // contact. The 5 mm observation band is smaller than the exposed ring tube
        // radius 18 mm; require the head and cap to be outside that same band.
        // Upward velocity gain greater than one tick of gravity proves a physical
        // response, rather than merely passing near the ring.
        var ringContactsDuringCooldown = Enumerable.Range(1, ring.Reads.Count - 1).Where(i =>
        {
            var ordinal = ring.Reads[i].Tick.Value * 4;
            if (ordinal <= first.EventOrdinal + 4 || ordinal >= first.EventOrdinal + 72) return false;
            var body = Body(ring.Reads[i], Ball).Body;
            var p = new System.Numerics.Vector3((body.Cell.X + body.Local.X) / 16f,
                (body.Cell.Y + body.Local.Y) / 16f - 3, (body.Cell.Z + body.Local.Z) / 16f);
            var local = System.Numerics.Vector3.Transform(p, System.Numerics.Quaternion.Inverse(rotation));
            var radial = MathF.Sqrt(local.X * local.X + local.Z * local.Z);
            var capGap = MathF.Sqrt(MathF.Pow(MathF.Max(0, radial - .25f), 2) +
                MathF.Pow(MathF.Max(0, MathF.Abs(local.Y - .59f) - .06f), 2)) - radius;
            var headGap = p.Length() - .65f - radius;
            var ringGap = MathF.Sqrt(MathF.Pow(radial - .49f, 2) + MathF.Pow(local.Y - .43f, 2)) - .018f - radius;
            return MathF.Abs(ringGap) < .005f && headGap > .005f && capGap > .005f &&
                body.Velocity.Y - Body(ring.Reads[i - 1], Ball).Body.Velocity.Y > 9.81f / TicksPerSecond;
        }).ToArray();
        Assert.NotEmpty(ringContactsDuringCooldown);
        foreach (var i in ringContactsDuringCooldown)
        {
            Assert.Equal(1u, ring.Reads[i].ContactWorks[0].OccurrenceCount);
            Assert.Equal(2f, ring.Reads[i - 1].ContactWorks[0].RemainingEnergy.Value);
            Assert.Equal(2f, ring.Reads[i].ContactWorks[0].RemainingEnergy.Value);
            Assert.Equal(first, Assert.Single(Occurrences(ring.Ledgers.Take(i + 1))));
        }

        // Matched control: only the drop x changes, to directly above the ring's
        // centreline. It bypasses the cap, so this owner's first ring hit must pay.
        var ringX = .49f * MathF.Cos(angle) - .43f * MathF.Sin(angle);
        await using var direct = await WorkshopIntegrationHarness.Running(Bench(ringX));
        await direct.Advance(120);
        var directHit = Occurrences(direct.Ledgers).First();
        Assert.Equal((Ball, ColliderShapeKind.Torus, ContactWorkEffect.Paid),
            (directHit.Target, Shape(directHit), directHit.Effect));
        Assert.True(directHit.Debit.Value > 0, "an eligible ring contact spends available work");
    }
}
