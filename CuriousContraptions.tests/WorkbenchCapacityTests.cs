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
        if (kind == PartId.Basketball) return WorkshopInput.Generic(PartId.Basketball,Id(), (float)(X(i)), 5, 0, 0, 0, 0, 1);
        if (kind == PartId.Receiver) return WorkshopInput.Generic(PartId.Receiver, Id(), (float)(X(i)), 1, 2, 0, 0, 0, 1);
        if (kind == PartId.Ramp) return WorkshopInput.Generic(PartId.Ramp, Id(), (float)(X(i)), 2, -2, 0, 0, 0, 1);
        if (kind == PartId.Wall) return WorkshopInput.Generic(PartId.Wall, Id(), (float)(X(i)), 2, 4, 0, 0, 0, 1, WallFixture.Default);
        if (kind == PartId.ImpactSwitch) return WorkshopInput.Generic(PartId.ImpactSwitch, Id(), (float)(X(i)), 1, -4, 0, 0, 0, 1);
        if (kind == PartId.SignalLamp) return PlacedParts.Lamp(Id(), (float)(X(i)), 1, 6, 0, 0, 0, 1);
        if (kind == PartId.Delay) return WorkshopInput.Generic(PartId.Delay, Id(), (float)(X(i)), 1, -6, 0, 0, 0, 1);
        if (kind == PartId.Pipe) return WorkshopInput.Pipe(Id(), (float)(X(i)), 3, 0, 0, 0, 0, 1);
        if (kind == PartId.PipeBend45) return WorkshopInput.PipeBend45(Id(), (float)(X(i)), 3, 0, 0, 0, 0, 1);
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
        var owners = Population((PartId.Receiver, 3));
        // Unit-spaced centres clear each ball's .35 m seam-inclusive reach.
        var instances = new WorkshopInstances([.. owners, .. Enumerable.Range(0, 17).Select(i =>
            (IWorkshopInstance)WorkshopInput.Generic(PartId.Basketball, Id(), (float)(i - 8), 5, 0, 0, 0, 0, 1))]);
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
        var heavy = Population((PartId.Pipe, 20), (PartId.PipeBend45, 10));
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
        Assert.Equal(PartId.Ramp, first.Puzzle.InventoryPart);
        Assert.Equal(2u, first.Puzzle.InventoryCount);
        var ramps = first.WithInstance(Make(PartId.Ramp, 0)).WithInstance(Make(PartId.Ramp, 1));
        ramps.Validate();
        EditRefusal.Is(WorkshopEditRejection.InventoryExceeded, () => ramps.WithInstance(Make(PartId.Ramp, 2)).Validate());
        Assert.ThrowsAny<ArgumentException>(() => first.WithInstance(Make(PartId.Receiver, 3)).Validate());
        EditRefusal.Is(WorkshopEditRejection.InventoryUnsupported, () => first.WithInstance(Make(PartId.Wall, 4)).Validate());

        var delayed = PuzzleCatalogue.Create(new("delayed_signal"), new(1), WorkshopCadenceSettings.Default(), new(100001), new(.5f));
        Assert.Equal(PartId.Delay, delayed.Puzzle.InventoryPart);
        Assert.Equal(1u, delayed.Puzzle.InventoryCount);
        var delay = delayed.WithInstance(Make(PartId.Delay, 0));
        delay.Validate();
        EditRefusal.Is(WorkshopEditRejection.InventoryExceeded, () => delay.WithInstance(Make(PartId.Delay, 1)).Validate());
        EditRefusal.Is(WorkshopEditRejection.InventoryUnsupported, () => delayed.WithInstance(Make(PartId.SignalLamp, 2)).Validate());
        EditRefusal.Is(WorkshopEditRejection.InventoryUnsupported, () => delayed.WithInstance(Make(PartId.ImpactSwitch, 3)).Validate());

        var inventory = WorkshopInventoryPolicy.Authored(first.Puzzle);
        Assert.Equal(PartAllowance.Counted(2), Assert.Single(inventory).Value);
        Assert.Equal(PartAllowance.Counted(1), WorkshopInventoryPolicy.Authored(delayed.Puzzle)[PartId.Delay]);
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
        "weight" => WorkshopInput.Generic(kind, id, (float)(x), 4, (float)(z), 0, 0, 0, 1),
        "basket" => WorkshopInput.Generic(PartId.Receiver, id, (float)(x), 1, (float)(z), 0, 0, 0, 1),
        "ramp" => WorkshopInput.Generic(PartId.Ramp, id, (float)(x), 2, (float)(z), 0, 0, 0, 1),
        "wall" => WorkshopInput.Generic(PartId.Wall, id, (float)(x), 2, (float)(z), 0, 0, 0, 1, WallFixture.Default),
        "switch" => WorkshopInput.Generic(PartId.ImpactSwitch, id, (float)(x), 1, (float)(z), 0, 0, 0, 1),
        "lamp" => PlacedParts.Lamp(id, (float)(x), 1, (float)(z), 0, 0, 0, 1),
        "delay" or "hold_timer" or "both_gate" or "electrical_nand" or "electrical_nor" or "electrical_or" or "electrical_xor" => WorkshopInput.Generic(kind, id, (float)(x), 1, (float)(z), 0, 0, 0, 1),
        "bumper" => WorkshopInput.Generic(PartId.PinballBumper, id, (float)(x), 1, (float)(z), 0, 0, 0, 1),
        "battery" => PlacedParts.Battery(id, (float)(x), 1, (float)(z)),
        "spring" => WorkshopInput.Generic(PartId.Springboard, id, (float)(x), 1, (float)(z), 0, 0, 0, 1),
        "pipe" => WorkshopInput.Pipe(id, (float)(x), 3, (float)(z), 0, 0, 0, 1),
        "ball_detector" => PlacedParts.Detector(id, (float)(x), 3, (float)(z), 0, 0, 0, 1),
        "pipe_bend_45" => WorkshopInput.PipeBend45(id, (float)(x), 3, (float)(z), 0, 0, 0, 1),
        "pipe_bend_90" => WorkshopInput.PipeBend90(id, (float)(x), 3, (float)(z), 0, 0, 0, 1),
        "funnel" => WorkshopInput.Funnel(id, (float)(x), 3, (float)(z), 0, 0, 0, 1),
        "powered_gate" => WorkshopInput.Generic(PartId.PoweredGate, id, (float)(x), 0, (float)(z), 0, 0, 0, 1),
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
        foreach (var undelivered in UndeliveredParts.Ids)
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
        var bench = Bench(Grid(PartId.PipeBend90, bends));
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
            .Select(i => (IWorkshopInstance)WorkshopInput.PipeBend90(new((ulong)i + 1), i < 30 ? -6 + i % 6 : 20 + (i - 30) % 16 * 1.6f, 3,
                i < 30 ? -3 + i / 6 * 1.5f : -5 + (i - 30) / 16 * 2.5f, 0, 0, 0, 1))
            .ToList();
        var cradle = new GpuBodyId(411);
        if (withLastBend) parts.Add(WorkshopInput.PipeBend90(cradle, 6, 3, 0, 0, 0, (float)(CradleZ), (float)(CradleW)));
        var ball = WorkshopInput.Generic(PartId.Basketball,new(412), 6, 3, 0, 0, 0, 0, 1);
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
        await using var simulation = new WorkshopSimulation(device, new WorkshopPhysicsEngineTests.StepClock(), WorkshopCadenceSettings.Default(),
            new(1), new WorkshopPhysicsEngineTests.NoInstallation(), GpuDeviceOwnership.Borrowed);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Initialize()).Outcome);
        var revision = 1UL;
        foreach (var bends in new[] { 1, PastThePartTable, 1, PastThePartTable })
        {
            var bench = new WorkshopConstruction(new(++revision), WorkshopCadenceSettings.Default(), Grid(PartId.PipeBend90, bends));
            Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Construct(bench)).Outcome);
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
        Assert.True(occurrence.HasSameBits(ledger.Occurrence(0)));
        var bodies = new PhysicsBodyReadSet([new PhysicsBodyRead(new CanonicalBody(new(5), 1, 0, default, default, default),
            CanonicalRotation.Identity, default, default)]);
        var read = new WorkshopRead(new(1), new(0), bodies, new(2),
            new(WorkshopClockDomain.SimulationMonotonic, new(1), new(100000000), new(100000)), ContactWorks: ledger.Stores);
        var response = new WorkshopResponse(new(4), WorkshopResponseKind.Acknowledgement, new(WorkshopCommandOutcome.Applied, WorkshopRejection.None),
            WorkshopSimulationPhase.Building, read, new(1, 2), default, new(1), new(1), new(1), new(0));
        foreach (var decoded in new[] { WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(response)),
            WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(response)) })
            Assert.True(ledger.Stores.HasSameBits(decoded.Read.ContactWorks));
    }

    [Fact]
    public void FreeInventoryListsEveryPlayableKindUnlimitedInPaletteRowOrder()
    {
        PartId[] rows = [PartId.Basketball, PartId.Receiver, PartId.ImpactSwitch, PartId.SignalLamp,
            PartId.Wall, PartId.Delay, PartId.PinballBumper, PartId.Ramp, PartId.Domino, PartId.BowlingBall, PartId.Battery, PartId.Springboard, PartId.Pipe, PartId.BallDetector, PartId.PipeBend45, PartId.PipeBend90, PartId.Funnel, PartId.PoweredGate, PartId.PressurePlate, PartId.Counter, PartId.Clock, PartId.Latch, new("hold_timer"), new("both_gate"), new("electrical_nand"), new("electrical_nor"), new("electrical_or"), new("electrical_xor"), new("weight")];
        Assert.Equal(rows, FreePalette.Keys.ToArray());
        Assert.All(FreePalette.Values, allowance => Assert.Equal(PartAllowance.Unlimited, allowance));
        Assert.Equal(rows.Length, FreePalette.Count);
        Assert.False(PartAllowance.Unlimited.Less(1000).Exhausted);
        Assert.Equal(PartAllowance.Unlimited, PartAllowance.Unlimited.Less(1000));
        Assert.False(PartAllowance.Counted(2).Less(1).Exhausted);
        Assert.True(PartAllowance.Counted(2).Less(2).Exhausted);
        Assert.Equal(PartAllowance.None, PartAllowance.Counted(2).Less(3)); // over-placed parts clamp at zero, never "× -1"
        Assert.Equal(PartAllowance.Unlimited, FreePalette[PartId.Ramp]);
        Assert.False(FreePalette.ContainsKey(new PartId("unsupported")));
        Assert.Throws<ArgumentException>(() => new PartInventoryDictionary((PartId.Wall, PartAllowance.Unlimited), (PartId.Wall, PartAllowance.None)));
        Assert.True(PartAllowance.None.Exhausted);
    }
}
