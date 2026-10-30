using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;

namespace CuriousContraptions.Tests;

/// <summary>Free play has no population rule (Story 9R.5b, owner decision 20 Oct 2026: an unlimited Free Workshop; memory is the
/// only bound). Every declared table is sized at Run; actual interaction state grows only on demand.</summary>
public sealed class WorkbenchCapacityTests
{
    private static ulong _next = 1;
    private static GpuBodyId Id() => new(_next++);
    private static double X(int i) => -6 + i * .4;
    private static IWorkshopInstance Make(PartId kind, int i)
    {
        if (kind == new PartId("ball")) return WorkshopInput.Generic(new PartId("ball"),Id(), (float)(X(i)), 5, 0, 0, 0, 0, 1);
        if (kind == new PartId("basket")) return WorkshopInput.Generic(new PartId("basket"), Id(), (float)(X(i)), 1, 2, 0, 0, 0, 1);
        if (kind == new PartId("ramp")) return WorkshopInput.Generic(new PartId("ramp"), Id(), (float)(X(i)), 2, -2, 0, 0, 0, 1);
        if (kind == new PartId("wall")) return WorkshopInput.Generic(new PartId("wall"), Id(), (float)(X(i)), 2, 4, 0, 0, 0, 1, WallFixture.Default);
        if (kind == new PartId("switch")) return WorkshopInput.Generic(new PartId("switch"), Id(), (float)(X(i)), 1, -4, 0, 0, 0, 1);
        if (kind == new PartId("lamp")) return PlacedParts.Lamp(Id(), (float)(X(i)), 1, 6, 0, 0, 0, 1);
        if (kind == new PartId("delay")) return WorkshopInput.Generic(new PartId("delay"), Id(), (float)(X(i)), 1, -6, 0, 0, 0, 1);
        if (kind == new PartId("pipe")) return WorkshopInput.Generic(new PartId("pipe"),Id(), (float)(X(i)), 3, 0, 0, 0, 0, 1);
        if (kind == new PartId("pipe_bend_45")) return WorkshopInput.Generic(new PartId("pipe_bend_45"), Id(), (float)(X(i)), 3, 0, 0, 0, 0, 1);
        throw new ArgumentException($"Unsupported test part id '{kind}'.");
    }
    private static WorkshopInstances Population(params (PartId Kind, int Count)[] groups)
    {
        var items = new List<IWorkshopInstance>();
        foreach (var (kind, count) in groups) for (var i = 0; i < count; i++) items.Add(Make(kind, items.Count));
        return new([.. items]);
    }
    private static WorkshopConstruction Free(WorkshopInstances instances) => new(new(1), WorkshopCadenceSettings.Default(), instances);

    [Fact]
    public void ReceiverPoliciesScaleWithOwnersRatherThanPotentialPayloadPairs()
    {
        var owners = Population((new PartId("basket"), 3));
        // Unit-spaced centres clear each ball's .35 m seam-inclusive reach.
        var instances = new WorkshopInstances([.. owners, .. Enumerable.Range(0, 17).Select(i =>
            (IWorkshopInstance)WorkshopInput.Generic(new PartId("ball"), Id(), (float)(i - 8), 5, 0, 0, 0, 0, 1))]);
        Free(instances).Validate();
        var scene = WorkshopPhysicsCompiler.Compile(Free(instances), new(1, 2));
        Assert.Equal(3, scene.Sensors.Length);
        Assert.Equal(3, scene.Guides.Length);
        Assert.Equal(17, scene.World.Dynamic);
        Assert.All(scene.Sensors.ToArray(), policy => Assert.Equal(BodyTargetKind.AllPayloads, policy.Targets.Kind));
    }

    [Fact]
    public async Task HeavyCompoundConstructionsValidateCompileAndStepWithoutColliderLimits()
    {
        // 20 pipes (3 hollow colliders each: shell tube and two collar tori) and 10 pipe bends (5 each: arc, two stubs, two collars).
        var heavy = Population((new PartId("pipe"), 20), (new PartId("pipe_bend_45"), 10));
        Free(heavy).Validate();
        var scene = WorkshopPhysicsCompiler.Compile(Free(heavy), new(1, 2));
        Assert.Equal(20 * 3 + 10 * 5 + 1, scene.Colliders.Length);
        // Story 9R.2: the 30-instance mixed bench also steps through the production engine.
        var reads = await WorkshopIntegrationHarness.Observe(Bench(heavy), SteppedTicks);
        Assert.Equal(new SimulationTick(SteppedTicks), reads[^1].Tick);
    }

    // elements-visible-solid-sweep: the parts whose proud faces, lamps and sockets now collide carry more colliders per instance
    // (Counter 15, each logic gate 10, Latch 9, Clock and Hold timer 7, Pressure plate 5). Four of each, 3 m apart, validate and compile
    // with every collider and the workbench plane: no collider limit applies.
    [Theory]
    [InlineData("counter", 15)]
    [InlineData("both_gate", 10)]
    [InlineData("electrical_nand", 10)]
    [InlineData("latch", 9)]
    [InlineData("clock", 8)]
    [InlineData("hold_timer", 7)]
    [InlineData("pressure_plate", 10)]
    public void WorkbenchCapacity_AdmitsProudPartElementsWithoutColliderLimit(string part, int colliders)
    {
        var instances = new WorkshopInstances([.. Enumerable.Range(0, 4).Select(i =>
            (IWorkshopInstance)WorkshopInput.Generic(new PartId(part), Id(), (float)(-4.5 + 3 * i), 3, 0, 0, 0, 0, 1))]);
        new WorkshopInstances([.. instances]).Validate();
        var world = WorkshopPhysicsCompiler.Compile(Free(instances), new(1, 2)).World;
        Assert.Equal((4 + 1, 4 * colliders + 1), (world.Bodies, world.Colliders));
    }

    [Fact]
    public void CampaignInventoriesAndFixturesAreUnchanged()
    {
        // Fixture identities sit far above the shared test counter so WithInstance appends rather than replaces.
        var first = PuzzleCatalogue.Create(new("first_principles"), new(1), WorkshopCadenceSettings.Default(), new(100001), new(.5f));
        Assert.Equal(new PartId("ramp"), first.Puzzle.InventoryPart);
        Assert.Equal(2u, first.Puzzle.InventoryCount);
        var ramps = first.WithInstance(Make(new PartId("ramp"), 0)).WithInstance(Make(new PartId("ramp"), 1));
        ramps.Validate();
        EditRefusal.Is(WorkshopEditRejection.InventoryExceeded, () => ramps.WithInstance(Make(new PartId("ramp"), 2)).Validate());
        Assert.ThrowsAny<ArgumentException>(() => first.WithInstance(Make(new PartId("basket"), 3)).Validate());
        EditRefusal.Is(WorkshopEditRejection.InventoryUnsupported, () => first.WithInstance(Make(new PartId("wall"), 4)).Validate());

        var delayed = PuzzleCatalogue.Create(new("delayed_signal"), new(1), WorkshopCadenceSettings.Default(), new(100001), new(.5f));
        Assert.Equal(new PartId("delay"), delayed.Puzzle.InventoryPart);
        Assert.Equal(1u, delayed.Puzzle.InventoryCount);
        var delay = delayed.WithInstance(Make(new PartId("delay"), 0));
        delay.Validate();
        EditRefusal.Is(WorkshopEditRejection.InventoryExceeded, () => delay.WithInstance(Make(new PartId("delay"), 1)).Validate());
        EditRefusal.Is(WorkshopEditRejection.InventoryUnsupported, () => delayed.WithInstance(Make(new PartId("lamp"), 2)).Validate());
        EditRefusal.Is(WorkshopEditRejection.InventoryUnsupported, () => delayed.WithInstance(Make(new PartId("switch"), 3)).Validate());

        var inventory = WorkshopInventoryPolicy.Authored(first.Puzzle);
        Assert.Equal(PartAllowance.Counted(2), Assert.Single(inventory).Value);
        Assert.Equal(PartAllowance.Counted(1), WorkshopInventoryPolicy.Authored(delayed.Puzzle)[new PartId("delay")]);
        Assert.Throws<ArgumentException>(() => WorkshopInventoryPolicy.Authored(default));
    }

    // ---------- Story 9R.2 and 9R.5b: every table is sized at Run from the compiled construction ----------

    private const int SteppedTicks = 30;
    /// <summary>One past the retired 32-part table.</summary>
    private const int PastThePartTable = 33;
    private const double DominoUpright = -0.46 + .55;
    // A cradle: the bend rotated -135 degrees about Z (27 fine-rotate steps of -5 degrees in Chrome), so both mouths point up and
    // its tube's lowest wall is a valley.
    private const double CradleZ = -0.9238795, CradleW = 0.3826834;

    /// <summary>The Free palette the generated bundle declares (Story 9A.14), read as the registry reads it.</summary>
    internal static IReadOnlyDictionary<PartId, PartAllowance> FreePalette =>WorkshopInventoryPolicy.Free(PartRegistry.Read(
        File.ReadAllText(Path.Combine(twodog.Engine.ResolveProjectDir(), "elements", "elements.bundle.json"))).Select(entry => new PartId(entry.Metadata.Id)));

    public static TheoryData<string> FreeKinds => new(FreePalette.Keys.Select(kind => kind.Value));

    private static IWorkshopInstance Place(PartId kind, GpuBodyId id, double x, double z) => kind.Value switch
    {
        "ball" or "bowling" => WorkshopInput.Generic(kind, id, (float)(x), 4, (float)(z), 0, 0, 0, 1),
        "domino" => PlacedParts.Domino(id, (float)(x), (float)(DominoUpright + .01), (float)(z), 0, 0, 0, 1),
        "weight" or "rope_anchor" or "pulley" => WorkshopInput.Generic(kind, id, (float)(x), 4, (float)(z), 0, 0, 0, 1),
        "basket" => WorkshopInput.Generic(new PartId("basket"), id, (float)(x), 1, (float)(z), 0, 0, 0, 1),
        "ramp" => WorkshopInput.Generic(new PartId("ramp"), id, (float)(x), 2, (float)(z), 0, 0, 0, 1),
        "wall" => WorkshopInput.Generic(new PartId("wall"), id, (float)(x), 2, (float)(z), 0, 0, 0, 1, WallFixture.Default),
        "switch" => WorkshopInput.Generic(new PartId("switch"), id, (float)(x), 1, (float)(z), 0, 0, 0, 1),
        "lamp" => PlacedParts.Lamp(id, (float)(x), 1, (float)(z), 0, 0, 0, 1),
        "delay" or "hold_timer" or "both_gate" or "electrical_nand" or "electrical_nor" or "electrical_or" or "electrical_xor" => WorkshopInput.Generic(kind, id, (float)(x), 1, (float)(z), 0, 0, 0, 1),
        "bumper" => WorkshopInput.Generic(new PartId("bumper"), id, (float)(x), 1, (float)(z), 0, 0, 0, 1),
        "battery" => PlacedParts.Battery(id, (float)(x), 1, (float)(z)),
        "spring" => WorkshopInput.Generic(new PartId("spring"), id, (float)(x), 1, (float)(z), 0, 0, 0, 1),
        "pipe" => WorkshopInput.Generic(new PartId("pipe"),id, (float)(x), 3, (float)(z), 0, 0, 0, 1),
        "ball_detector" => PlacedParts.Detector(id, (float)(x), 3, (float)(z), 0, 0, 0, 1),
        "pipe_bend_45" => WorkshopInput.Generic(new PartId("pipe_bend_45"), id, (float)(x), 3, (float)(z), 0, 0, 0, 1),
        "pipe_bend_90" => WorkshopInput.Generic(new PartId("pipe_bend_90"), id, (float)(x), 3, (float)(z), 0, 0, 0, 1),
        "funnel" => WorkshopInput.Generic(new PartId("funnel"), id, (float)(x), 3, (float)(z), 0, 0, 0, 1),
        "powered_gate" => WorkshopInput.Generic(new PartId("powered_gate"), id, (float)(x), 0, (float)(z), 0, 0, 0, 1),
        "pressure_plate" => PlacedParts.Plate(id, (float)(x), 1, (float)(z), .5f),
        "counter" => PlacedParts.Counter(id, (float)(x), 1, (float)(z), 0, 0, 0, 1, 3),
        "clock" => PlacedParts.Clock(id, (float)(x), 1, (float)(z), 0, 0, 0, 1, 1f),
        "latch" => PlacedParts.Latch(id, (float)(x), 1, (float)(z), 0, 0, 0, 1),
        _ => throw new ArgumentException($"Unsupported palette part '{kind}'.", nameof(kind)),
    };

    // Eight columns, rows 2.5 m apart, wide enough that no moving part starts inside a neighbour.
    /// <summary>Eight to a row for the first 64 parts; from the 65th on, sixteen to a row in a second field from x 20, so hundreds of
    /// parts stay inside the admitted world.</summary>
    private static WorkshopInstances Grid(PartId kind, int count) =>
        new([.. Enumerable.Range(0, count).Select(i => i < 64
            ? Place(kind, new((ulong)i + 1), -6 + i % 8 * 1.6, -5 + i / 8 * 2.5)
            : Place(kind, new((ulong)i + 1), 20 + (i - 64) % 16 * 1.6f, -5 + (i - 64) / 16 * 2.5))]);

    // The Story 9.4b harness admits at revision 2 through the production WorkshopSimulation, WorkshopGpuDevice and BrowserGpuTransport.
    private static WorkshopConstruction Bench(WorkshopInstances instances) => new(new(2), WorkshopCadenceSettings.Default(), instances);

    /// <summary>Story 9R.5b: every Free palette kind builds, compiles and steps one past the retired 32-part table (no kind is held
    /// back by a body, moving-body, trigger, contact-work, source or prismatic table any more).</summary>
    [Theory]
    [MemberData(nameof(FreeKinds))]
    public async Task EveryFreePaletteKindCompilesAndStepsPastTheRetiredPartTable(string kindId)
    {
        var kind = new PartId(kindId);
        var perPart = WorkshopPhysicsCompiler.Compile(Bench(Grid(kind, 1)), new(1, 2)).Colliders.Length - 1;
        var bench = Bench(Grid(kind, PastThePartTable));
        bench.Validate();
        var scene = WorkshopPhysicsCompiler.Compile(bench, new(1, 2));
        Assert.Equal(1 + PastThePartTable * perPart, scene.Colliders.Length);
        var reads = await WorkshopIntegrationHarness.Observe(bench, SteppedTicks);
        Assert.Equal(new SimulationTick(SteppedTicks), reads[^1].Tick);
    }

    [Fact]
    public void TheUndeliveredPartsStayOffTheFreePalette()
    {
        // Spec D2 (9R.2): Tennis and Balloon throw in CaptureInstance and Story 9R.0c removed them from the palette.
        foreach (var undelivered in new PartId[] { new("tennis"), new("balloon") })
        {
            Assert.False(FreePalette.ContainsKey(undelivered));
            Assert.Throws<ArgumentException>(() => Place(undelivered, new(1), 0, 0));
        }
    }

    // A 90° bend compiles 5 hollow colliders (engine-hollow-shapes: arc, two stubs, two collars). The plane alone, the full 9R.2 bench
    // (32 bends), one past the retired part table, then 409 bends (2,046 colliders) and 410 bends (2,051) straddling the retired 2,048
    // ceiling.
    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    [InlineData(PastThePartTable)]
    [InlineData(409)]
    [InlineData(410)]
    public async Task BendBenchesAcrossTheRetiredColliderCeilingCompileAndStep(int bends)
    {
        var bench = Bench(Grid(new PartId("pipe_bend_90"), bends));
        bench.Validate();
        Assert.Equal(1 + bends * 5, WorkshopPhysicsCompiler.Compile(bench, new(1, 2)).Colliders.Length);
        var reads = await WorkshopIntegrationHarness.Observe(bench, SteppedTicks);
        Assert.Equal(new SimulationTick(SteppedTicks), reads[^1].Tick);
    }

    // 410 bends to the left (5 colliders each), then (with the last bend) a cradle around the ball: 411 x 5 + plane + ball = 2,057
    // colliders, and the ball rests inside the cradle's tube on colliders whose slots lie beyond the retired 2,048 ceiling. The cradle's
    // geometry is the capacity-9r Chrome bench's (placement plane y 3). Control: without that bend the ball falls to the bench.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ABallRestsInTheLastOfFourHundredElevenBendsAndFallsWithoutIt(bool withLastBend)
    {
        var parts = Enumerable.Range(0, 410)
            .Select(i => (IWorkshopInstance)WorkshopInput.Generic(new PartId("pipe_bend_90"), new((ulong)i + 1), i < 30 ? -6 + i % 6 : 20 + (i - 30) % 16 * 1.6f, 3,
                i < 30 ? -3 + i / 6 * 1.5f : -5 + (i - 30) / 16 * 2.5f, 0, 0, 0, 1))
            .ToList();
        var cradle = new GpuBodyId(411);
        if (withLastBend) parts.Add(WorkshopInput.Generic(new PartId("pipe_bend_90"), cradle, 6, 3, 0, 0, 0, (float)(CradleZ), (float)(CradleW)));
        var ball = WorkshopInput.Generic(new PartId("ball"),new(412), 6, 3, 0, 0, 0, 0, 1);
        parts.Add(ball);
        var bench = Bench(new([.. parts]));
        bench.Validate();
        var scene = WorkshopPhysicsCompiler.Compile(bench, new(1, 2));
        Assert.Equal((withLastBend ? 411 : 410) * 5 + 2 + 1, scene.Colliders.Length); // five per bend, sphere + seam, plane
        if (withLastBend)
            Assert.All(Enumerable.Range(0, scene.Colliders.Length).Where(slot => scene.Colliders[slot].Body == cradle),
                slot => Assert.True(slot > 2048, $"cradle collider slot {slot} lies beyond the retired ceiling"));
        var reads = await WorkshopIntegrationHarness.Observe(bench, 3 * 120);
        var rest = reads[^1].Bodies[0];
        Assert.Equal(ball.Id, rest.Body.Id);
        var height = (rest.Body.Cell.Y + rest.Body.Local.Y) / 16f;
        if (withLastBend) Assert.InRange(height, 2.5f, 2.9f); // resting on the cradle tube's lowest wall
        else Assert.True(height < 0, $"without the cradle the ball falls to the bench (height {height})");
    }

    // One simulation, device and transport admit benches of changing size in turn: 1, 33, 1 and 33 bends, each Run, stepped and Reset.
    [Fact]
    public async Task OneTransportAdmitsBenchesOfChangingColliderCount()
    {
        await using var device = new WorkshopGpuDevice(new BrowserGpuTransport(new CuriousContraptions.Abstractions.EventRingPublication()), new(101, 206));
        await using var simulation = new WorkshopSimulation(device, new WorkshopPhysicsEngineTests.StepClock(), CompiledSceneFixture.Compile(new(new(1), WorkshopCadenceSettings.Default(), WorkshopInstances.Empty)),
            new(1), new WorkshopPhysicsEngineTests.NoInstallation(), GpuDeviceOwnership.Borrowed);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Initialize()).Outcome);
        var revision = 1UL;
        foreach (var bends in new[] { 1, PastThePartTable, 1, PastThePartTable })
        {
            var bench = new WorkshopConstruction(new(++revision), WorkshopCadenceSettings.Default(), Grid(new PartId("pipe_bend_90"), bends));
            Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Construct(CompiledSceneFixture.Compile(bench))).Outcome);
            Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Run()).Outcome);
            for (var tick = 0; tick < 3; tick++) Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Advance()).Outcome);
            Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Reset()).Outcome);
            Assert.Equal(WorkshopSimulationPhase.Building, simulation.Phase);
        }
    }

    /// <summary>Story 9R.5b: a work occurrence names its owner's collider and work slots in 32 bits, so a slot past 16 bits (a machine
    /// with more than 65,536 colliders) is held unchanged. Story 9A.23 D9: the occurrence rows are the device's private ledger; the
    /// response carries the owners' stores, which cross both codecs unchanged.</summary>
    [Fact]
    public void AWorkOccurrenceSlotPastSixteenBitsIsHeldByTheDeviceLedger()
    {
        var occurrence = new ContactWorkOccurrence(new(0), new(70_000), new(5), 1, 4, 0f, new(3f), new(1.5f), ContactWorkEffect.Paid);
        var ledger = new ContactWorkLedger([new(new(9), new(8), 1, new(30f))], [occurrence]);
        Assert.Equal(70_000u, ledger.Occurrence(0).Collider.Value);
        Assert.Equal(occurrence, ledger.Occurrence(0));
        var bodies = new PhysicsBodyReadSet([new PhysicsBodyRead(new CanonicalBody(new(5), 1, 0, default, default, default),
            CanonicalRotation.Identity, default, default)]);
        var read = new WorkshopRead(new(1), new(0), bodies, new(2),
            new(WorkshopClockDomain.SimulationMonotonic, new(1), new(100000000), new(100000)), ContactWorks: ledger.Stores);
        var response = new WorkshopResponse(new(4), WorkshopResponseKind.Acknowledgement, new(WorkshopCommandOutcome.Applied, WorkshopRejection.None),
            WorkshopSimulationPhase.Building, read, new(1, 2), default, new(1), new(1), new(1), new(0));
        foreach (var decoded in new[] { WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(response)),
            WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(response)) })
        {
            Assert.Equal(1, decoded.Read.ContactWorks.Count);
            Assert.Equal(new ContactWorkRead(new(9), new(8), 1, new(30f)), decoded.Read.ContactWorks[0]);
        }
    }

    [Fact]
    public void FreeInventoryListsEveryPlayableKindUnlimitedInPaletteRowOrder()
    {
        PartId[] rows = [new PartId("ball"), new PartId("basket"), new PartId("switch"), new PartId("lamp"),
            new PartId("wall"), new PartId("delay"), new PartId("bumper"), new PartId("ramp"), new PartId("domino"), new PartId("bowling"), new PartId("battery"), new PartId("spring"), new PartId("pipe"), new PartId("ball_detector"), new PartId("pipe_bend_45"), new PartId("pipe_bend_90"), new PartId("funnel"), new PartId("powered_gate"), new PartId("pressure_plate"), new PartId("counter"), new PartId("clock"), new PartId("latch"), new("hold_timer"), new("both_gate"), new("electrical_nand"), new("electrical_nor"), new("electrical_or"), new("electrical_xor"), new("weight"), new("rope_anchor"), new("pulley")];
        Assert.Equal(rows, FreePalette.Keys.ToArray());
        Assert.All(FreePalette.Values, allowance => Assert.Equal(PartAllowance.Unlimited, allowance));
        Assert.Equal(rows.Length, FreePalette.Count);
        Assert.False(PartAllowance.Unlimited.Less(1000).Exhausted);
        Assert.Equal(PartAllowance.Unlimited, PartAllowance.Unlimited.Less(1000));
        Assert.False(PartAllowance.Counted(2).Less(1).Exhausted);
        Assert.True(PartAllowance.Counted(2).Less(2).Exhausted);
        Assert.Equal(PartAllowance.None, PartAllowance.Counted(2).Less(3)); // over-placed parts clamp at zero, never "× -1"
        Assert.Equal(PartAllowance.Unlimited, FreePalette[new PartId("ramp")]);
        Assert.False(FreePalette.ContainsKey(new PartId("unsupported")));
        Assert.Throws<ArgumentException>(() => new PartInventoryDictionary((new PartId("wall"), PartAllowance.Unlimited), (new PartId("wall"), PartAllowance.None)));
        Assert.True(PartAllowance.None.Exhausted);
    }
}
