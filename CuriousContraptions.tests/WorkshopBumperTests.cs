using System.Buffers.Binary;
using System.Runtime.Intrinsics;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class WorkshopBumperTests
{

    [Theory]
    [InlineData("bumper_depth", "bumper_depth")]
    [InlineData("wall_and_bumper", "wall_and_bumper")]
    public void AdvancedLessonsPreserveSourceFixturesProfilesInventoryAndSave(
        string lessonId, string sourceId)
    {
        var lesson = new PuzzleId(lessonId);
        using var source = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(
            twodog.Engine.ResolveProjectDir(), "content/puzzles.json")));
        var level = source.RootElement.EnumerateArray().Single(value => value.GetProperty("id").GetString() == sourceId);
        var construction = PuzzleCatalogue.Create(lesson, new(1), WorkshopCadenceSettings.Default(), new(1), new(0f));
        Assert.Equal(WorkshopGoalKind.Captured, construction.Puzzle.Goal.Kind);
        Assert.Equal(new GpuBodyId(1), construction.Puzzle.Goal.Body);
        Assert.Equal(new GpuBodyId(2), construction.Puzzle.Goal.Target);
        var fixtures = level.GetProperty("placements").EnumerateArray().Select(row => row.GetProperty("instance")).ToArray();
        AuthoredContentAssertions.Placement(fixtures[0], Payloads.First(construction));
        AuthoredContentAssertions.Placement(fixtures[1], ((WorkshopGenericInstance)construction.Instances.Single(instance => instance.PartId == new PartId("basket"))));
        Assert.True(Payloads.First(construction).Locked && ((WorkshopGenericInstance)construction.Instances.Single(instance => instance.PartId == new PartId("basket"))).Locked);
        AuthoredContentAssertions.Profile(fixtures[0].GetProperty("assistance"), construction.Puzzle.PrimaryAssistance);
        AuthoredContentAssertions.Profile(fixtures[1].GetProperty("assistance"), construction.Puzzle.TargetAssistance);
        var solution = level.GetProperty("solution").EnumerateArray().Select(row => row.GetProperty("instance")).ToArray();
        AuthoredContentAssertions.Profile(solution.Single(value => value.GetProperty("part").GetString() == "bumper").GetProperty("assistance"),
            construction.Puzzle.PlacementAssistance);
        var inventory = WorkshopInventoryPolicy.Authored(construction.Puzzle);
        Assert.Equal(PartAllowance.Counted(1), inventory[new PartId("bumper")]);
        var bumper = WorkshopInput.Generic(new PartId("bumper"), new(3), 0, 1.5f, 0, 0, 0, 0, 1);
        var placed = construction.WithInstance(bumper);
        var wall = WorkshopInput.Generic(new PartId("wall"), new(4), -1, 4, 0, 0, 0, 0, 1, WallFixture.Values(.4f, 6, 1.5f));
        if (lesson == new PuzzleId("wall_and_bumper"))
        {
            Assert.Equal(2, inventory.Count);
            Assert.Equal(PartAllowance.Counted(1), inventory[new PartId("wall")]);
            Assert.Equal(1, level.GetProperty("inventory").EnumerateArray().Single(row => row.GetProperty("part").GetString() == "wall").GetProperty("count").GetInt32());
            AuthoredContentAssertions.Profile(solution.Single(value => value.GetProperty("part").GetString() == "wall").GetProperty("assistance"),
                construction.Puzzle.SecondaryAssistance);
            placed = placed.WithInstance(wall);
            EditRefusal.Is(WorkshopEditRejection.InventoryExceeded, () => placed.WithInstance(wall with { Id = new(5) }).Validate());
        }
        else
        {
            Assert.Single(inventory);
            Assert.Equal(default, construction.Puzzle.SecondaryAssistance);
            EditRefusal.Is(WorkshopEditRejection.InventoryUnsupported, () => placed.WithInstance(wall).Validate());
        }
        placed.Validate();
        EditRefusal.Is(WorkshopEditRejection.InventoryExceeded, () => placed.WithInstance(bumper with { Id = new(5) }).Validate());
        EditRefusal.Is(WorkshopEditRejection.InventoryUnsupported, () => placed.WithInstance(bumper with { Locked = true }).Validate());
        EditRefusal.Is(WorkshopEditRejection.FixedPart, () => placed.WithInstance(Payloads.First(construction) with { Locked = false }).Validate());
        EditRefusal.Is(WorkshopEditRejection.PuzzleSettings, () => (placed with { Puzzle = placed.Puzzle with { InventoryCount = 2 } }).Validate());
        EditRefusal.Is(WorkshopEditRejection.PuzzleSettings, () => (placed with { Puzzle = placed.Puzzle with { PlacementAssistance = PuzzleCatalogue.Definition(new("first_principles")).Placement } }).Validate());
        foreach (var precision in new float[] { 0f, .45f, 1f })
        {
            var next = PuzzleCatalogue.WithPrecision(placed, new(precision));
            var expected = precision == 0f ? (.3,3,.15,12) : precision == 1f ? (.02,1.5,.35,0) : (.174,2.325,.24,6.6);
            Assert.Equal((float)expected.Item1,((WorkshopGenericInstance)next.Instances.Single(instance => instance.PartId == new PartId("basket"))).Parameters[new("capture_margin")]);
            Assert.Equal((float)expected.Item2,((WorkshopGenericInstance)next.Instances.Single(instance => instance.PartId == new PartId("basket"))).Parameters[new("capture_speed_limit")]);
            Assert.Equal((float)expected.Item3,((WorkshopGenericInstance)next.Instances.Single(instance => instance.PartId == new PartId("basket"))).Parameters[new("capture_dwell")]);
            Assert.Equal((float)expected.Item4,((WorkshopGenericInstance)next.Instances.Single(instance => instance.PartId == new PartId("basket"))).Parameters[new("force_maximum_acceleration")]);
            var save = new WorkshopSavedConstruction(next, new(6));
            var bytes = WorkshopSaveCodec.Encode(save);
            Assert.Equal(save, WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes));
            Assert.Equal(bumper, next.Instances.Single(value => value.Id == bumper.Id));
            Assert.Single(WorkshopPhysicsCompiler.Compile(next, new(1, 2)).ContactWorks.ToArray());
        }
    }


    [Theory]
    [InlineData("bumper_depth")]
    [InlineData("wall_and_bumper")]
    public void AdvancedLessonSaveAndCommandRejectMalformedIdentityProfileAndInventory(string lessonId)
    {
        var lesson = new PuzzleId(lessonId);
        var construction = PuzzleCatalogue.Create(lesson, new(1), WorkshopCadenceSettings.Default(), new(1), new(.45f));
        var saved = new WorkshopSavedConstruction(construction,new(3));
        var original = WorkshopSaveCodec.Encode(saved);
        var command = new WorkshopCommand(new(1),WorkshopCommandKind.Construct,new(1),new(1),CompiledSceneFixture.Compile(construction),
            Session:new(1,2),Cadence:new(1),Projection:new(1));
        var wire = WorkshopWireFlatBuffers.EncodeCommand(command);
        var malformedPuzzles = new[]
        {
            construction.Puzzle! with { Id = new PuzzleId("unknown") },
            construction.Puzzle! with { InventoryCount = 2 },
            construction.Puzzle! with { Precision = new(float.NaN) },
            construction.Puzzle! with { PlacementAssistance = PuzzleCatalogue.Definition(new("first_principles")).Placement }
        };

        foreach (var badPuzzle in malformedPuzzles)
        {
            var badConstruction = construction with { Puzzle = badPuzzle };
            var builder = new Google.FlatBuffers.FlatBufferBuilder(512);
            var crOffset = WorkshopAuthoringFlatBuffers.EncodeConstructionRecord(builder, badConstruction, validate: false);
            var badSave = SaveFixture.Finish(builder, crOffset, 3);
            // Extensible unknown identities use the authored puzzle refusal; nonfinite precision is a structural refusal.
            if (float.IsNaN(badPuzzle.Precision.Value)) SaveFixture.Refused(WorkshopSaveRejection.NonFinite, () => WorkshopSaveCodec.Decode(SaveFixture.Elements,badSave));
            else EditRefusal.Is(WorkshopEditRejection.PuzzleSettings, () => WorkshopSaveCodec.Decode(SaveFixture.Elements,badSave));


            // The wire encoder validates the construction it carries: a puzzle rule's typed refusal, or for a
            // non-finite precision the structural bound (an argument guard).
            var refused = Assert.ThrowsAny<ArgumentException>(() => CompiledSceneFixture.Compile(badConstruction));
            Assert.Equal(float.IsNaN(badPuzzle.Precision.Value) ? typeof(ArgumentException) : typeof(WorkshopEditRejectedException), refused.GetType());
        }
        Assert.Equal(saved, WorkshopSaveCodec.Decode(SaveFixture.Elements,original));
        var decoded = WorkshopWireFlatBuffers.DecodeCommand(wire);
        Assert.Equal(command with { Scene = null }, decoded with { Scene = null });
        Assert.Equal(command.Scene!.Identity, decoded.Scene!.Identity);
        Assert.Equal(command.Scene.Physics.Bodies.ToArray(), decoded.Scene.Physics.Bodies.ToArray());
        Assert.Equal(command.Scene.Owners.ToArray(), decoded.Scene.Owners.ToArray());
    }





    [Fact]
    public void SidekickKeepsSourceProfilesAndAtomicInventory()
    {
        using var source = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(twodog.Engine.ResolveProjectDir(), "content/puzzles.json")));
        var level = source.RootElement.EnumerateArray().Single(value => value.GetProperty("id").GetString() == "bumper_sidekick");
        Assert.Equal(1, level.GetProperty("inventory").EnumerateArray().Single(row => row.GetProperty("part").GetString() == "bumper").GetProperty("count").GetInt32());
        var construction = PuzzleCatalogue.Create(new("bumper_sidekick"), new(1), WorkshopCadenceSettings.Default(), new(1), new(0f));
        Assert.Equal(WorkshopGoalKind.Captured, construction.Puzzle.Goal.Kind);
        AuthoredContentAssertions.Profile(level.GetProperty("placements")[0].GetProperty("instance").GetProperty("assistance"), construction.Puzzle.PrimaryAssistance);
        AuthoredContentAssertions.Profile(level.GetProperty("placements")[1].GetProperty("instance").GetProperty("assistance"), construction.Puzzle.TargetAssistance);
        AuthoredContentAssertions.Profile(level.GetProperty("solution")[0].GetProperty("instance").GetProperty("assistance"), construction.Puzzle.PlacementAssistance);
        Assert.Equal(WorkshopInput.Generic(new PartId("ball"),new(1), -3, 5, 0, 0, 0, 0, 1) with { Locked = true }, Payloads.Find(construction));
        AuthoredContentAssertions.Placement(level.GetProperty("placements")[1].GetProperty("instance"), ((WorkshopGenericInstance)construction.Instances.Single(instance => instance.PartId == new PartId("basket"))));
        var bumper = new WorkshopGenericInstance(new PartId("bumper"), new(3), new(-51, 24, 0), default, CanonicalRotation.Identity)
            { Parameters = InstanceParameterCollection.Defaults(new PartId("bumper")) };
        var placed = construction.WithInstance(bumper); placed.Validate();
        EditRefusal.Is(WorkshopEditRejection.InventoryExceeded, () => placed.WithInstance(bumper with { Id = new(4) }).Validate());
        EditRefusal.Is(WorkshopEditRejection.PuzzleSettings, () => (placed with { Puzzle = placed.Puzzle with { PlacementAssistance = PuzzleCatalogue.Definition(new("first_principles")).Placement } }).Validate());
        var unpaid = placed.WithInstance(bumper with { Parameters = new([new(new("strength"), 0f)]) }); unpaid.Validate();
        Assert.Equal(0f, Assert.Single(WorkshopPhysicsCompiler.Compile(unpaid, new(1, 2)).ContactWorks.ToArray()).InitialEnergy.Value);
        foreach (var precision in new float[] { 0f, .45f, 1f })
        {
            var next = PuzzleCatalogue.WithPrecision(placed, new(precision)); next.Validate();
            var saved = new WorkshopSavedConstruction(next, new(4));
            Assert.Equal(saved, WorkshopSaveCodec.Decode(SaveFixture.Elements,WorkshopSaveCodec.Encode(saved)));
        }

    }

    private const float Preload = 32f;
    private static readonly Vector128<float> Up = Vector128.Create(0f, 1f, 0f, 0f);
    // One f32 ULP just below the 32 J preload: the spacing of every request in [16, 32).
    private static readonly float UlpBelowPreload = Preload - MathF.BitDecrement(Preload);

    [Theory]
    [InlineData(32f)]
    [InlineData(2.5f)]
    [InlineData(.000001f)]
    [InlineData(0f)]
    public void StorePaymentUsesOneF32BudgetAndSimdNormalProjection(float budget)
    {
        var normal = Vector128.Create(.6f, .8f, 0f, 0f);
        // A store full to its own capacity: even the 1e-6 J store is above its minimum payable work.
        var result = ContactWorkImpulse.FromStore(4f, 8f, 1f, budget, budget == 0 ? Preload : budget, normal);
        Assert.InRange(result.Remaining, 0f, budget);
        Assert.Equal(budget - result.Remaining, result.Debit);
        var x = result.Impulse.GetElement(0);
        var y = result.Impulse.GetElement(1);
        Assert.Equal(0f, result.Impulse.GetElement(2));
        Assert.Equal(0f, result.Impulse.GetElement(3));
        Assert.InRange(Math.Abs(.8f * x - .6f * y), 0, 1e-6f);
        var impulse = .6 * x + .8 * y;
        var work = 4 * impulse + .5 * impulse * impulse;
        Assert.InRange(work, 0, result.Debit + 1e-5);
        Assert.InRange(4 + impulse, 4, 8.000001);
        if (budget == 0) Assert.Equal(Vector128<float>.Zero, result.Impulse);
        else Assert.True(result.Debit > 0 && impulse > 0);
    }

    // Story 9.4b (CAT-015 ledger): a payout capped by the finite store spends the whole store, so f32 rounding of the
    // affordable impulse cannot leave a residual that a later empty contact would then spend.
    [Theory]
    [InlineData(4f, .25f, 32f)]
    [InlineData(3f, .25f, 32f)]
    [InlineData(2.5f, .25f, 32f)]
    [InlineData(1.7f, .25f, 32f)]
    [InlineData(5.123f, .2f, 32f)]
    [InlineData(4f, 1f, 2.5f)]
    public void StoreLimitedPayoutExhaustsTheStoreAndALaterContactIsUnpaid(float speed, float inverseMass, float budget) =>
        AssertStoreLimitedPayoutExhausts(speed, inverseMass, budget);

    // Deterministic sweep of contact-space speeds and inverse effective masses (rotational arms included), so the
    // f32 rounding cases the Chrome heavy-payload route hit are covered rather than one hand-picked value.
    [Fact]
    public void EveryStoreLimitedPayoutInTheContactSweepExhaustsTheStore()
    {
        for (var s = 0; s < 160; s++)
        for (var m = 0; m < 160; m++)
        {
            var speed = .05f + s * .0371f;
            var inverseMass = .2f + m * .00731f;
            var requested = (8f - speed) / inverseMass * (speed + .5f * inverseMass * ((8f - speed) / inverseMass));
            // Over the store, or so close below it that the remainder would be under the minimum payable work.
            if (requested > 32f || 32f - requested < .5f * ContactWorkImpulse.MinimumPayableWork(32f))
                AssertStoreLimitedPayoutExhausts(speed, inverseMass, 32f);
        }
    }

    private static void AssertStoreLimitedPayoutExhausts(float speed, float inverseMass, float budget)
    {
        var normal = Vector128.Create(0f, 1f, 0f, 0f);
        var paid = ContactWorkImpulse.FromStore(speed, 8f, inverseMass, budget, budget, normal);
        Assert.Equal(0f, paid.Remaining);
        Assert.Equal(budget, paid.Debit);
        Assert.True(paid.Impulse.GetElement(1) > 0f);
        AssertDebitIsTheDeliveredWork(speed, inverseMass, paid);
        var later = ContactWorkImpulse.FromStore(speed, 8f, inverseMass, paid.Remaining, budget, normal);
        Assert.Equal(0f, later.Debit);
        Assert.Equal(0f, later.Remaining);
        Assert.Equal(Vector128<float>.Zero, later.Impulse);
    }

    /// <summary>A Bumper contact speed whose f32 target request lies exactly <paramref name="ulps"/> ULPs below the 32 J
    /// preload, searched over the contact-space inverse masses that can afford a request near the whole store.</summary>
    private static (float Speed, float InverseMass) RequestBelowPreload(int ulps)
    {
        foreach (var inverseMass in new[] { .2f, .25f, .3f, .4f, .5f, .6f, .75f, .9f })
        {
            var speed = MathF.Sqrt(64f - 2f * inverseMass * Preload);
            for (var step = 0; step < 8192; step++, speed = MathF.BitIncrement(speed))
            {
                var impulse = (8f - speed) / inverseMass;
                var requested = impulse * (speed + .5f * inverseMass * impulse);
                if (Preload - requested == ulps * UlpBelowPreload) return (speed, inverseMass);
            }
        }
        throw new InvalidOperationException($"No contact speed requests exactly {ulps} ULPs below the preload.");
    }

    // Edge-case review (Story 9.4b): a target-limited request 1, 2, 4 or 16 ULPs below the store would leave rounding dust
    // (Murdoch-2: 2^-19 J at k = 0.9, v = 2.529823 m/s) that a later contact pays from. It is store-limited: the payout
    // takes the whole store and the later contact, given the real leftover, is unpaid.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(16)]
    public void ARequestAFewUlpsBelowTheStoreSpendsTheWholeStoreAndLeavesNoPayableDust(int ulps)
    {
        var (speed, inverseMass) = RequestBelowPreload(ulps);
        // Control: the bare target law leaves exactly the dust the ledger must not keep, and that dust alone would pay.
        var target = ContactWorkImpulse.TowardTarget(speed, 8f, inverseMass, Preload);
        Assert.Equal(ContactWorkLimit.Target, target.Limit);
        var dust = Preload - target.Debit;
        Assert.InRange(dust, UlpBelowPreload, 32 * UlpBelowPreload);
        Assert.True(ContactWorkImpulse.FromAuthorizedWork(.5f, inverseMass, dust, ContactWorkLimit.Budget).Debit > 0f);

        var paid = ContactWorkImpulse.FromStore(speed, 8f, inverseMass, Preload, Preload, Up);
        Assert.Equal(0f, paid.Remaining);
        Assert.Equal(Preload, paid.Debit);
        AssertDebitIsTheDeliveredWork(speed, inverseMass, paid);
        var later = ContactWorkImpulse.FromStore(.5f, 8f, inverseMass, paid.Remaining, Preload, Up);
        Assert.Equal(new ContactWorkResponse(0f, 0f, Vector128<float>.Zero, ContactWorkStore.BelowMinimum), later);
        // The dust a pre-9.4b ledger kept is itself below the minimum payable work.
        Assert.Equal(new ContactWorkResponse(dust, 0f, Vector128<float>.Zero, ContactWorkStore.BelowMinimum),
            ContactWorkImpulse.FromStore(.5f, 8f, inverseMass, dust, Preload, Up));
    }

    // Positive control: a target payout that leaves at least the minimum payable work keeps the exact remainder, and a
    // later contact is paid from it.
    [Fact]
    public void ATargetPayoutLeavingPayableWorkKeepsTheExactRemainderForALaterContact()
    {
        var paid = ContactWorkImpulse.FromStore(4f, 8f, 1f, Preload, Preload, Up);
        Assert.Equal(Preload, paid.Remaining + paid.Debit);
        Assert.InRange(paid.Remaining, 8f - 1e-5f, 8f + 1e-5f);
        AssertDebitIsTheDeliveredWork(4f, 1f, paid);
        var later = ContactWorkImpulse.FromStore(4f, 8f, 1f, paid.Remaining, Preload, Up);
        Assert.Equal(0f, later.Remaining);
        Assert.Equal(paid.Remaining, later.Debit);
    }

    [Fact]
    public void AStoreBelowItsMinimumPayableWorkNeverPays()
    {
        var available = .5f * ContactWorkImpulse.MinimumPayableWork(Preload);
        Assert.Equal(new ContactWorkResponse(available, 0f, Vector128<float>.Zero, ContactWorkStore.BelowMinimum),
            ContactWorkImpulse.FromStore(0f, 8f, 1f, available, Preload, Up));
    }

    // D20 control: a 0 J capacity (a strength-0 Bumper) is a legal empty store, never an impossible one.
    [Fact]
    public void AZeroCapacityStoreIsALegalEmptyStoreThatNeverPays()
    {
        Assert.Equal(new ContactWorkResponse(0f, 0f, Vector128<float>.Zero, ContactWorkStore.BelowMinimum),
            ContactWorkImpulse.FromStore(0f, 8f, 1f, 0f, 0f, Up));
    }

    [Theory]
    [InlineData(33f, 32f)] // above its declared capacity
    [InlineData(-1f, 32f)]
    [InlineData(32f, 0f)]
    [InlineData(32f, -1f)]
    [InlineData(32f, float.NaN)]
    [InlineData(32f, float.PositiveInfinity)]
    [InlineData(float.NaN, 32f)]
    [InlineData(float.PositiveInfinity, 32f)]
    public void AStoreOutsideItsDeclaredCapacityIsReportedImpossibleAndNeverPays(float available, float capacity)
    {
        var result = ContactWorkImpulse.FromStore(0f, 8f, 1f, available, capacity, Up);
        Assert.Equal(ContactWorkStore.Impossible, result.Store);
        Assert.Equal(0f, result.Debit);
        Assert.Equal(Vector128<float>.Zero, result.Impulse);
        Assert.Equal(available, result.Remaining);
    }

    // Every paid response debits the work its impulse delivers: never less beyond f32 rounding of the same expression (the
    // store never funds work it did not hold) and more only by the four-step f32 impulse rounding.
    private static void AssertDebitIsTheDeliveredWork(float speed, float inverseMass, ContactWorkResponse paid)
    {
        var impulse = (double)paid.Impulse.GetElement(1);
        var delivered = impulse * (speed + .5 * inverseMass * impulse);
        var ulp = (double)(MathF.BitIncrement(paid.Debit) - paid.Debit);
        Assert.InRange(delivered, paid.Debit - 8 * ulp, paid.Debit + 2 * ulp);
    }

    [Fact]
    public void PaidGpuRecordReadsF32WorkAndRejectsPadding()
    {
        var scene = WorkshopPhysicsCompiler.Compile(Construction(8f), new(1, 2));
        var bytes = PhysicsGpuAbi.Admission(scene, new(1),
            new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        var before = PhysicsGpuAbi.ReadContactWorks(bytes, PhysicsInteractionSnapshot.Empty);
        var owner = scene.World.ContactWorkOffset(0);
        var collider = Array.FindIndex(scene.Colliders.ToArray(), value => value.Body == new GpuBodyId(2));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(88), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(owner + 32), 1);
        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(owner + 48), 20f);
        var occurrence = new ContactWorkOccurrence(new(0), new(checked((uint)collider)), new(1), 1, 1, 0,
            new(4.000001f), new(12f), ContactWorkEffect.Paid);
        var interactions = new PhysicsInteractionSnapshot(PhysicsInteractionSnapshot.Empty.Captures,
            PhysicsInteractionSnapshot.Empty.Works.Add(new(scene.ContactWorks[0].Id, new(1)), occurrence));
        var paid = PhysicsGpuAbi.ReadContactWorks(bytes, interactions);
        paid.ValidateAdvance(before);
        Assert.Equal(20f, paid[0].RemainingEnergy.Value);
        Assert.Equal(12f, paid.Occurrence(0).Debit.Value);
        Assert.Equal(4.000001f, paid.Occurrence(0).ApproachSpeed.Value);
        Assert.Equal(ContactWorkEffect.Paid, paid.Occurrence(0).Effect);
        bytes[owner + PhysicsGpuAbi.ContactWorkPaddingOffset] = 1;
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadContactWorks(bytes, interactions));
    }

    [Theory]
    [InlineData(32f, 8f, 24f)]
    [InlineData(2.5f, 4.582576f, 2.5f)]
    [InlineData(0f, 4f, 0f)]
    public void FiniteBudgetCapsTheNormalBoostAndNeverPaysPastTarget(
        float budget, float expectedSpeed, float expectedDebit)
    {
        var payment = ContactWorkImpulse.TowardTarget(4f, 8f, 1f, budget);
        Assert.InRange(4f + payment.NormalImpulse, expectedSpeed - 1e-5f, expectedSpeed + 1e-5f);
        Assert.InRange(payment.Debit, expectedDebit - 1e-5f, expectedDebit);
        Assert.InRange(payment.Debit, 0f, budget);
    }

    [Theory]
    [InlineData(8f)]
    [InlineData(12f)]
    public void ContactAlreadyAtTargetIsNeverSlowedOrCharged(float speed) =>
        Assert.Equal(default, ContactWorkImpulse.TowardTarget(speed, 8f, 1f, 32f));

    [Theory]
    [InlineData(0f, 1f, 32f, 8f)]
    [InlineData(4f, 1f, 24f, 4f)]
    [InlineData(4f, .25f, 96f, 16f)]
    public void AuthorizedNormalWorkPaysTheContactSpaceKineticIncrease(
        float outgoingSpeed, float inverseEffectiveMass, float work, float expectedImpulse)
    {
        var payment = ContactWorkImpulse.FromAuthorizedWork(outgoingSpeed, inverseEffectiveMass, work, ContactWorkLimit.Budget);
        Assert.InRange(payment.NormalImpulse, expectedImpulse - 1e-5f, expectedImpulse + 1e-5f);
        Assert.InRange(payment.Debit, work - 1e-4f, work);
        var after = outgoingSpeed + inverseEffectiveMass * payment.NormalImpulse;
        var increase = (after * after - outgoingSpeed * outgoingSpeed) / (2f * inverseEffectiveMass);
        Assert.InRange(increase, payment.Debit - 1e-4f, payment.Debit + 1e-4f);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void NoAuthorizedWorkNeverProducesAnImpulse(float work) =>
        Assert.Equal(default, ContactWorkImpulse.FromAuthorizedWork(4f, 1f, work, ContactWorkLimit.Budget));

    [Fact]
    public void TinyWorkAtHighOutgoingSpeedSurvivesWithoutSubtractingNearlyEqualRoots()
    {
        var payment = ContactWorkImpulse.FromAuthorizedWork(64f, 1f, 1e-6f, ContactWorkLimit.Budget);
        Assert.True(payment.NormalImpulse > 0f);
        Assert.InRange(payment.Debit, 0.999e-6f, 1e-6f);
    }

    [Theory]
    [InlineData(-1f, 1f)]
    [InlineData(float.NaN, 1f)]
    [InlineData(1f, 0f)]
    [InlineData(1f, -1f)]
    [InlineData(1f, float.PositiveInfinity)]
    public void InvalidContactResidualContinuesWithoutPaidImpulse(float speed, float inverseMass) =>
        Assert.Equal(default, ContactWorkImpulse.FromAuthorizedWork(speed, inverseMass, 32f, ContactWorkLimit.Budget));

    [Fact]
    public void AnUndefinedPaymentLimitNeverProducesAnImpulse() =>
        Assert.Equal(default, ContactWorkImpulse.FromAuthorizedWork(4f, 1f, 24f, (ContactWorkLimit)7));

    private static WorkshopConstruction Construction(float strength) => new(new(1), WorkshopCadenceSettings.Default(),
        new(WorkshopInput.Generic(new PartId("ball"),new(1), 0, 6, 0, 0, 0, 0, 1),
            PlacedParts.Bumper(new(2), 0, 4, 0, strength: (float)strength)));

    [Fact]
    public void SourceSphereAndExplicitPreloadCompileAsGenericDeclarations()
    {
        var scene = WorkshopPhysicsCompiler.Compile(Construction(8f), new(1, 2));
        // The head sphere and (engine-hollow-shapes) the upper ring: the drawn TorusMesh (inner 0.472, outer 0.508) at y 0.43, a
        // solid tube of radius 0.018 about a 0.49 m centreline.
        var bumper = scene.Colliders.ToArray().Where(x => x.Body == new GpuBodyId(2)).ToArray();
        Assert.Equal([ColliderShapeKind.Sphere, ColliderShapeKind.Torus, ColliderShapeKind.Cylinder], bumper.Select(x => x.Shape).ToArray());
        var ring = bumper[1];
        // Each lane is one f32 sum or difference of two scene numbers: within 1e-6 m of its doc value.
        Assert.Equal(.49f, ring.Radius.Value, 1e-6f);
        Assert.Equal(.018f, ring.HalfExtents.Y, 1e-6f);
        Assert.Equal((0f, ColliderDeclaration.FullSweep, .43f), (ring.HalfExtents.X, ring.HalfExtents.Z, ring.Pose.Translation.Y));
        var sphere = bumper[0];
        Assert.Equal(ColliderShapeKind.Sphere, sphere.Shape);
        Assert.Equal(new Metres(.65f), sphere.Radius);
        Assert.Equal(RigidLocalPose.Identity, sphere.Pose);
        var material = Assert.Single(scene.Materials.ToArray(), x => x.Id == sphere.Material);
        Assert.Equal(new Restitution(1f), material.Restitution);
        Assert.Equal(new LinearSpeed(.1f), material.BounceThreshold);
        Assert.Equal(new FrictionCoefficient(.3f), material.Friction);
        var work = Assert.Single(scene.ContactWorks.ToArray());
        Assert.Equal(new GpuBodyId(2), work.Owner);
        Assert.Equal(new BodyTargetSet(BodyTargetKind.AllDynamic, default), work.Targets);
        Assert.True(work.Targets.Contains(new(1), scene.Bodies.ToArray().Single(body => body.Id == new GpuBodyId(1)).Payload));
        Assert.Equal(new ContactSpeed(8f), work.TargetSpeed);
        Assert.Equal(new Joules(32), work.InitialEnergy);
        Assert.Equal(new ContactSpeed(.05f), work.Threshold);
        Assert.Equal(72u, work.CooldownPhysicalSteps);
    }

    // The saved Bumper is its declared strength only: the store is derived on compile, so a forged typed field, an undeclared
    // "preload" parameter, an out-of-range, nonfinite, missing or duplicate strength is refused, never loaded.
    [Fact]
    public void ConstructionRoundTripPreservesTheStrengthAndRefusesForgedStores()
    {
        foreach (var strength in new float[] { 0f, 8f, 20f })
        {
            var save = new WorkshopSavedConstruction(Construction(strength), new(3));
            var bytes = WorkshopSaveCodec.Encode(save);
            Assert.Equal(save, WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes));
            var bumper = Assert.IsType<WorkshopGenericInstance>(save.Construction.Instances[1]);
            Assert.Equal((float)strength, Assert.Single(bumper.Parameters).Value);
            Assert.Equal(.5f * (float)strength * (float)strength,
                Assert.Single(WorkshopPhysicsCompiler.Compile(save.Construction, new(1, 2)).ContactWorks.ToArray()).InitialEnergy.Value);
        }
        foreach (var parameters in new (string, float)[][]
        {
            [("strength", 8f), ("preload", 17f)], [("strength", 20.5f)], [("strength", -.01f)], [], [("strength", 8f), ("strength", 8f)]
        })
            SaveFixture.Refused(WorkshopSaveRejection.OutOfRange, () => WorkshopSaveCodec.Decode(SaveFixture.Elements,
                PayloadInstanceTests.GenericSave("bumper", parameters)));
        SaveFixture.Refused(WorkshopSaveRejection.NonFinite, () => WorkshopSaveCodec.Decode(SaveFixture.Elements,
            PayloadInstanceTests.GenericSave("bumper", [("strength", float.NaN)])));
        Assert.Equal(8f, Assert.IsType<WorkshopGenericInstance>(Assert.Single(WorkshopSaveCodec.Decode(SaveFixture.Elements,
            PayloadInstanceTests.GenericSave("bumper", [("strength", 8f)])).Construction.Instances)).Parameters[new("strength")]);
    }

    [Fact]
    public void WorkRequiresUniquePhysicalStaticOwnerAndDynamicTarget()
    {
        var scene = WorkshopPhysicsCompiler.Compile(Construction(8f), new(1, 2));
        var work = scene.ContactWorks[0];
        foreach (var invalid in new[] {
            work with { Owner = new(1) }, work with { Targets = new(BodyTargetKind.NamedBody, work.Owner) },
            work with { Owner = new(99) }, work with { CooldownPhysicalSteps = 0 },
            work with { InitialEnergy = new(201) } })
            Assert.Throws<ArgumentException>(() => new PhysicsSceneDeclaration(scene.Document, scene.NextIdentity,
                scene.Bodies, scene.Colliders, scene.Materials, scene.Sensors, scene.Guides, scene.Triggers, [invalid]));
        Assert.Throws<ArgumentException>(() => new PhysicsSceneDeclaration(scene.Document, scene.NextIdentity + 1,
            scene.Bodies, scene.Colliders, scene.Materials, scene.Sensors, scene.Guides, scene.Triggers,
            [work, work with { Id = new(scene.NextIdentity) }]));
    }

    [Fact]
    public void StrengthBoundariesAndPopulationRejectAtomically()
    {
        foreach (var invalid in new[] { float.NaN, float.PositiveInfinity, -.01f, MathF.BitIncrement(20f) })
            Assert.ThrowsAny<ArgumentException>(() => PlacedParts.Bumper(new(2), 0, 4, 0, strength: invalid));
        foreach (var valid in new[] { 0f, 20f })
            Assert.Equal(valid, ((WorkshopGenericInstance)PlacedParts.Bumper(new(2), 0, 4, 0, strength: valid)).Parameters[new("strength")]);
        var construction = Construction(8f);
        var bumper = Assert.IsType<WorkshopGenericInstance>(construction.Instances[1]);
        construction.WithInstance(bumper with { Id = new(3) }).Validate(); // No per-kind bumper count in free play.
        Assert.False(DeclaredPorts.Current.Has(new PartId("bumper"), WorkshopSocket.ActivationOut,
            WorkshopConnectionDomain.Activation, WorkshopPortDirection.Output));
    }
}
