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
        WorkshopInput.Generic(payload, Ball, 0, height, z, 0, 0, 0, 1), PlacedParts.Bumper(Owner, 0, 3, 0, strength));

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

    // Preload = (½·m)·s·s in f32 with the declared 1 kg reference mass, bit for bit, at both ends, the default and a non-binary16
    // strength; threshold 0.05 m/s and the 72-step cooldown come with it.
    [Theory]
    [InlineData(0f)]
    [InlineData(8f)]
    [InlineData(20f)]
    [InlineData(3.3333333f)]
    public void TheCompiledStoreHoldsTheDeclaredPreloadBits(float strength)
    {
        var scene = WorkshopPhysicsCompiler.Compile(Drop(PartId.Basketball, 4.5, strength), new(1, 2));
        var work = Assert.Single(scene.ContactWorks.ToArray());
        var half = .5f * 1f;
        Assert.Equal(BitConverter.SingleToUInt32Bits(half * strength * strength), BitConverter.SingleToUInt32Bits(work.InitialEnergy.Value));
        Assert.Equal(BitConverter.SingleToUInt32Bits(strength), BitConverter.SingleToUInt32Bits(work.TargetSpeed.Value));
        Assert.Equal((WorkshopPhysicsCompiler.ContactWork(Owner), Owner, new BodyTargetSet(BodyTargetKind.AllDynamic, default)),
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
        var work = WorkshopPhysicsCompiler.ContactWorkFor(new(7), targets, descriptor, new([new(new("speed"), 3f)]));
        Assert.Equal((WorkshopPhysicsCompiler.ContactWork(new(7)), new GpuBodyId(7), targets, 3f, 9f, .5f, 100u),
            (work.Id, work.Owner, work.Targets, work.TargetSpeed.Value, work.InitialEnergy.Value, work.Threshold.Value, work.CooldownPhysicalSteps));
        Assert.ThrowsAny<KeyNotFoundException>(() => WorkshopPhysicsCompiler.ContactWorkFor(new(7), targets, descriptor,
            new([new(new("strength"), 3f)])));
    }

    // Only a part declaring the capability gets a store: a Wall, a Receiver and an Impact Switch on the same bench compile none.
    [Fact]
    public void OnlyTheDeclaringPartCompilesAStore()
    {
        var scene = WorkshopPhysicsCompiler.Compile(Free(WorkshopInput.Generic(PartId.Basketball, Ball, 0, 4.5, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(PartId.Wall, new(3), 4, 1, 0, 0, 0, 0, 1, WallFixture.Default),
            WorkshopInput.Generic(PartId.Receiver, new(4), -4, 1, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(PartId.ImpactSwitch, new(5), 8, 1, 0, 0, 0, 0, 1),
            PlacedParts.Bumper(Owner, 0, 3, 0)), new(1, 2));
        Assert.Equal(Owner, Assert.Single(scene.ContactWorks.ToArray()).Owner);
    }

    // Exact Run/Reset and Save/Load: the store, every body and every occurrence restore bit for bit, and the reloaded Bumper replays.
    [Fact]
    public async Task RunResetAndSaveLoadAreExact()
    {
        var bench = Drop(PartId.Basketball, 4.5, 12.5f);
        var (built, run, reset) = await NoFixedCapacityTests.RunAndReset(bench, 2 * TicksPerSecond);
        // A paid hit during the run: the store counted it and spent energy (its occurrence row is device-private, Story 9A.23 D9).
        Assert.Contains(run, read => read.ContactWorks[0].OccurrenceCount > 0 &&
            read.ContactWorks[0].RemainingEnergy.Value < built.ContactWorks[0].RemainingEnergy.Value);
        NoFixedCapacityTests.AssertSameState(built, reset, "Reset");
        Assert.Equal(built.ContactWorks[0], reset.ContactWorks[0]);
        var saved = new WorkshopSavedConstruction(bench, new(3));
        var bytes = WorkshopSaveCodec.Encode(saved);
        var reloaded = WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes);
        Assert.Equal(saved, reloaded);
        Assert.Equal(bytes, WorkshopSaveCodec.Encode(reloaded));
        var (_, replay, _) = await NoFixedCapacityTests.RunAndReset(reloaded.Construction, 2 * TicksPerSecond);
        NoFixedCapacityTests.AssertSameState(run[^1], replay[^1], "the reloaded Bumper's run");
        Assert.Equal(run[^1].ContactWorks[0], replay[^1].ContactWorks[0]);
    }

    [Fact]
    public void StrengthEndpointsHoldAnEmptyAndATwoHundredJouleStore()
    {
        Assert.Equal(0f, Assert.Single(WorkshopPhysicsCompiler.Compile(Drop(PartId.Basketball, 4.5, 0f), new(1, 2)).ContactWorks.ToArray()).InitialEnergy.Value);
        Assert.Equal(200f, Assert.Single(WorkshopPhysicsCompiler.Compile(Drop(PartId.Basketball, 4.5, 20f), new(1, 2)).ContactWorks.ToArray()).InitialEnergy.Value);
    }

    // ---------- real simulation ----------

    // Strength 8: the 1 kg ball leaves its first paid contact at the 8 m/s target; the ledger closes exactly against the 32 J store.
    [Fact]
    public async Task StrengthEightLaunchesTheBallPaidAtTheTargetSpeedAndTheLedgerCloses()
    {
        var (reads, ledgers) = await WorkshopPhysicsEngineTests.RunWithLedgers(Drop(PartId.Basketball, 4.5, 8f), 2 * TicksPerSecond);
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
        var (reads, ledgers) = await WorkshopPhysicsEngineTests.RunWithLedgers(Drop(PartId.BowlingBall, 6, 8f), 4 * TicksPerSecond);
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
        var (reads, ledgers) = await WorkshopPhysicsEngineTests.RunWithLedgers(Drop(PartId.Basketball, 4.5, 0f), 2 * TicksPerSecond);
        var occurrences = Occurrences(ledgers).ToArray();
        Assert.NotEmpty(occurrences);
        Assert.All(occurrences, occurrence => Assert.Equal((0f, ContactWorkEffect.Passive), (occurrence.Debit.Value, occurrence.Effect)));
        Assert.All(reads, read => Assert.Equal(0f, read.ContactWorks[0].RemainingEnergy.Value));
    }

    // Another depth plane: the ball falls past the Bumper 2 m behind it and never qualifies; the store stays whole.
    [Fact]
    public async Task ABallInAnotherDepthPlaneNeverPays()
    {
        var (reads, ledgers) = await WorkshopPhysicsEngineTests.RunWithLedgers(Drop(PartId.Basketball, 4.5, 8f, z: 2), 2 * TicksPerSecond);
        Assert.Empty(Occurrences(ledgers));
        Assert.All(reads, read => Assert.Equal(32f, read.ContactWorks[0].RemainingEnergy.Value));
    }

    // Recharge: a battery wired to PowerIn credits the emptied store; the same battery unwired credits nothing.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ADirectlyWiredBatteryRechargesTheStoreAndAnUnwiredOneDoesNot(bool wired)
    {
        var battery = PlacedParts.Battery(Source, 3, 1, 0, powerLimit: 10f);
        var bench = Drop(PartId.BowlingBall, 6, 8f);
        bench = bench with { Instances = bench.Instances.With(battery) };
        if (wired) bench = bench with { Connections = new(new WorkshopConnection(Source, WorkshopSocket.Supply, Owner, WorkshopSocket.PowerIn,
            WorkshopConnectionDomain.Electrical)) };
        var scene = WorkshopPhysicsCompiler.Compile(bench, new(1, 2));
        Assert.Equal(wired ? 1 : 0, scene.Electrical.Bindings.Length);
        var reads = await WorkshopPhysicsEngineTests.Run(bench, 4 * TicksPerSecond);
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
        var toggle = WorkshopInput.Generic(PartId.ImpactSwitch, new(4), 5, 1, 0, 0, 0, 0, 1);
        var bench = Free(PlacedParts.Bumper(Owner, 0, 3, 0), battery, toggle) with { Connections = new(
            new WorkshopConnection(Source, WorkshopSocket.Supply, toggle.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical),
            new WorkshopConnection(toggle.Id, WorkshopSocket.Supply, Owner, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical)) };
        EditRefusal.Is(WorkshopEditRejection.PowerInputSupplied, () => WorkshopPhysicsCompiler.Compile(bench, new(1, 2)));
    }
}
