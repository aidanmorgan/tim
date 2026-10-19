using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;

namespace CuriousContraptions.Tests;

/// <summary>Free play has no per-kind population rule; the compiled table capacities are the only ceiling and reject with a typed reason.</summary>
public sealed class WorkbenchCapacityTests
{
    private static ulong _next = 1;
    private static GpuBodyId Id() => new(_next++);
    private static double X(int i) => -6 + i * .4;
    private static IWorkshopInstance Make(PartId kind, int i)
    {
        if (kind == PartId.Basketball) return WorkshopInput.Basketball(Id(), X(i), 5, 0, 0, 0, 0, 1);
        if (kind == PartId.Battery) return new WorkshopBattery(Id(), default, default, CanonicalRotation.Identity, ElectricalSourceSettings.Default);
        if (kind == PartId.BowlingBall) return WorkshopInput.Ball(PartId.BowlingBall, Id(), X(i), 5, 0, 0, 0, 0, 1);
        if (kind == PartId.Receiver) return WorkshopInput.Receiver(Id(), X(i), 1, 2, 0, 0, 0, 1);
        if (kind == PartId.Ramp) return WorkshopInput.Ramp(Id(), X(i), 2, -2, 0, 0, 0, 1, RampDimensions.Default);
        if (kind == PartId.Wall) return WorkshopInput.Wall(Id(), X(i), 2, 4, 0, 0, 0, 1, WallDimensions.Default);
        if (kind == PartId.ImpactSwitch) return WorkshopInput.Switch(Id(), X(i), 1, -4, 0, 0, 0, 1, ContactTriggerSettings.Default);
        if (kind == PartId.SignalLamp) return WorkshopInput.Lamp(Id(), X(i), 1, 6, 0, 0, 0, 1);
        if (kind == PartId.Delay) return WorkshopInput.Delay(Id(), X(i), 1, -6, 0, 0, 0, 1, DelayDuration.Default);
        if (kind == PartId.PinballBumper) return WorkshopInput.Bumper(Id(), X(i), 3, 0, 0, 0, 0, 1, BumperWork.FromCanonicalStrength((float)8));
        if (kind == PartId.Domino) return WorkshopInput.Domino(Id(), X(i), .09, 2, 0, 0, 0, 1);
        if (kind == PartId.Pipe) return WorkshopInput.Pipe(Id(), X(i), 3, 0, 0, 0, 0, 1);
        if (kind == PartId.PipeBend45) return WorkshopInput.PipeBend45(Id(), X(i), 3, 0, 0, 0, 0, 1);
        throw new ArgumentException($"Unsupported test part id '{kind}'.");
    }
    private static WorkshopInstances Population(params (PartId Kind, int Count)[] groups)
    {
        var items = new List<IWorkshopInstance>();
        foreach (var (kind, count) in groups) for (var i = 0; i < count; i++) items.Add(Make(kind, items.Count));
        return new([.. items]);
    }
    private static WorkshopConstruction Free(WorkshopInstances instances) => new(new(1), WorkshopCadenceSettings.Default(), instances);
    private static int Dynamic(PhysicsSceneDeclaration scene) => scene.Bodies.ToArray().Count(b => b.Motion == RigidMotionKind.Dynamic);

    [Fact]
    public void FreePlayAcceptsManyOfEveryKindUpToTheCompiledTableLimits()
    {
        // Sixteen moving bodies, one receiver (sixteen sensors and guides), eight triggers (the whole activation table), seven walls: 32 parts.
        var full = Population((PartId.Basketball, 16), (PartId.Receiver, 1), (PartId.ImpactSwitch, 8), (PartId.Wall, 7));
        var scene = WorkshopPhysicsCompiler.Compile(Free(full), new(1, 2));
        Assert.Equal(WorkshopInstances.Capacity, full.Count);
        Assert.Equal(PhysicsSceneDeclaration.BodyCapacity, scene.Bodies.Length);
        Assert.Equal(PhysicsBodyReadSet.Capacity, Dynamic(scene));
        Assert.Equal(PhysicsSceneDeclaration.SensorCapacity, scene.Sensors.Length);
        Assert.Equal(PhysicsSceneDeclaration.GuideCapacity, scene.Guides.Length);
        Assert.Equal(PhysicsSceneDeclaration.TriggerCapacity, scene.Triggers.Length);
        Assert.Equal(ActivationNetwork.Capacity, (int)WorkshopActivationCompiler.Compile(Free(full)).Clear().Count);

        // Eight bumpers (the whole contact-work table), eight activation nodes across three kinds, three ramps, five balls.
        var mixed = Population((PartId.PinballBumper, 8), (PartId.Delay, 3), (PartId.SignalLamp, 3),
            (PartId.ImpactSwitch, 2), (PartId.Ramp, 3), (PartId.Receiver, 1), (PartId.Basketball, 5));
        scene = WorkshopPhysicsCompiler.Compile(Free(mixed), new(1, 2));
        Assert.Equal(PhysicsSceneDeclaration.ContactWorkCapacity, scene.ContactWorks.Length);
        Assert.Equal(5, scene.Sensors.Length);
        Assert.Equal(ActivationNetwork.Capacity, (int)WorkshopActivationCompiler.Compile(Free(mixed)).Clear().Count);

        // Several receivers compile while no ball needs guiding; each keeps its own body and walls.
        var receivers = Population((PartId.Receiver, 3), (PartId.Ramp, 2));
        var receiverIds = receivers.OfType<WorkshopReceiver>().Select(r => r.Id).ToHashSet();
        scene = WorkshopPhysicsCompiler.Compile(Free(receivers), new(1, 2));
        Assert.Equal(3, scene.Bodies.ToArray().Count(b => receiverIds.Contains(b.Id)));
        Assert.Equal(3 * 5, scene.Colliders.ToArray().Count(c => receiverIds.Contains(c.Body)));
        Assert.Empty(scene.Sensors.ToArray()); Assert.Empty(scene.Guides.ToArray());

        // Thirty-two of one static kind fills the part table exactly.
        WorkshopPhysicsCompiler.Compile(Free(Population((PartId.Wall, 32))), new(1, 2));
    }

    [Theory]
    [InlineData(WorkbenchTable.Instances, "wall", 33, "wall", 0)]
    [InlineData(WorkbenchTable.DynamicBodies, "ball", 17, "wall", 0)]
    [InlineData(WorkbenchTable.DynamicBodies, "domino", 9, "ball", 8)]
    [InlineData(WorkbenchTable.DynamicBodies, "bowling", 9, "ball", 8)]
    [InlineData(WorkbenchTable.Sensors, "basket", 2, "ball", 9)]
    [InlineData(WorkbenchTable.Sensors, "basket", 2, "bowling", 9)]
    [InlineData(WorkbenchTable.Guides, "basket", 2, "ball", 1)]
    [InlineData(WorkbenchTable.Guides, "basket", 2, "bowling", 1)]
    [InlineData(WorkbenchTable.ElectricalSources, "battery", 9, "wall", 0)]
    [InlineData(WorkbenchTable.Triggers, "switch", 9, "wall", 0)]
    [InlineData(WorkbenchTable.ContactWork, "bumper", 9, "wall", 0)]
    [InlineData(WorkbenchTable.ActivationNodes, "lamp", 5, "delay", 4)]
    public void ExceedingOneCompiledTableRejectsWithThatTypedReason(WorkbenchTable table, string firstId, int firstCount, string secondId, int secondCount)
    {
        var first = new PartId(firstId);
        var second = new PartId(secondId);
        var instances = Population((first, firstCount), (second, secondCount));
        var error = Assert.Throws<WorkbenchFullException>(() => instances.Validate());
        Assert.Equal(table, error.Table);
        Assert.StartsWith("Workbench is full: ", error.Message);
        Assert.Throws<WorkbenchFullException>(() => Free(instances).Validate());
        // One part fewer of the first kind fits again.
        Free(Population((first, firstCount - 1), (second, secondCount))).Validate();
    }

    [Fact]
    public async Task HeavyCompoundConstructionsValidateCompileAndStepWithoutColliderLimits()
    {
        // 20 pipes = 480 colliders, 10 pipe bends = 400 colliders (total 880 colliders)
        var heavy = Population((PartId.Pipe, 20), (PartId.PipeBend45, 10));
        Free(heavy).Validate();
        var scene = WorkshopPhysicsCompiler.Compile(Free(heavy), new(1, 2));
        Assert.Equal(20 * 24 + 10 * 40 + 1, scene.Colliders.Length);
        // Story 9R.2: the 30-instance mixed bench also steps through the production engine.
        var reads = await WorkshopPhysicsEngineTests.Run(Bench(heavy), SteppedTicks);
        Assert.Equal(new SimulationTick(SteppedTicks), reads[^1].Tick);
    }

    [Theory]
    [InlineData("ball")]
    [InlineData("basket")]
    [InlineData("ramp")]
    [InlineData("wall")]
    [InlineData("switch")]
    [InlineData("lamp")]
    [InlineData("delay")]
    [InlineData("bumper")]
    [InlineData("domino")]
    [InlineData("battery")]
    [InlineData("bowling")]
    public void DeclaredFootprintMatchesWhatTheCompilersEmit(string kindId)
    {
        var kind = new PartId(kindId);
        var construction = Free(Population((kind, 1)));
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        var expected = WorkbenchFootprint.Plane + WorkbenchFootprint.Of(kind);
        Assert.Equal(expected.Bodies, scene.Bodies.Length);
        Assert.Equal(expected.Bodies, scene.Materials.Length);
        Assert.Equal(expected.DynamicBodies, Dynamic(scene));
        Assert.Equal(expected.Colliders, scene.Colliders.Length);
        Assert.Equal(expected.Triggers, scene.Triggers.Length);
        Assert.Equal(expected.ContactWorks, scene.ContactWorks.Length);
        Assert.Equal(expected.ElectricalSources, scene.Electrical.Sources.Length);
        Assert.Equal(expected.ActivationNodes, (int)WorkshopActivationCompiler.Compile(construction).Clear().Count);
        Assert.Throws<ArgumentException>(() => WorkbenchFootprint.Of(PartId.Unsupported));
    }

    [Fact]
    public void CampaignInventoriesAndFixturesAreUnchanged()
    {
        // Fixture identities sit far above the shared test counter so WithInstance appends rather than replaces.
        var first = FirstPrinciples.Create(new(1), WorkshopCadenceSettings.Default(), new(100001), new(100002), new((Half).5));
        Assert.Equal(PartId.Ramp, first.Puzzle.InventoryPart);
        Assert.Equal(2u, first.Puzzle.InventoryCount);
        var ramps = first.WithInstance(Make(PartId.Ramp, 0)).WithInstance(Make(PartId.Ramp, 1));
        ramps.Validate();
        EditRefusal.Is(WorkshopEditRejection.InventoryExceeded, () => ramps.WithInstance(Make(PartId.Ramp, 2)).Validate());
        Assert.ThrowsAny<ArgumentException>(() => first.WithInstance(Make(PartId.Receiver, 3)).Validate());
        EditRefusal.Is(WorkshopEditRejection.InventoryUnsupported, () => first.WithInstance(Make(PartId.Wall, 4)).Validate());

        var delayed = DelayedSignal.Create(new(1), WorkshopCadenceSettings.Default(), new(100001), new(100002), new(100003), new((Half).5));
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

    // ---------- Story 9R.2: the collider table is sized at Run from the compiled construction ----------

    private const int SteppedTicks = 30;
    private const double DominoUpright = -0.46 + .55;
    // A cradle: the bend rotated -135 degrees about Z (27 fine-rotate steps of -5 degrees in Chrome), so both mouths point up and
    // its tube's lowest wall is a valley.
    private const double CradleZ = -0.9238795, CradleW = 0.3826834;

    /// <summary>Spec D2: per Free palette kind, the largest count Build admits within the 32-instance budget and, when that is
    /// below 32, the declared budget whose existing message rejects one more.</summary>
    private static readonly IReadOnlyDictionary<PartId, (int Largest, WorkbenchTable? Rejects)> Budgets = new Dictionary<PartId, (int, WorkbenchTable?)>
    {
        [PartId.Basketball] = (16, WorkbenchTable.DynamicBodies),
        [PartId.BowlingBall] = (16, WorkbenchTable.DynamicBodies),
        [PartId.Domino] = (16, WorkbenchTable.DynamicBodies),
        [PartId.Springboard] = (16, WorkbenchTable.Bodies),
        [PartId.PoweredGate] = (16, WorkbenchTable.Bodies),
        [PartId.Battery] = (8, WorkbenchTable.ElectricalSources),
        [PartId.ImpactSwitch] = (8, WorkbenchTable.Triggers),
        [PartId.PinballBumper] = (8, WorkbenchTable.ContactWork),
        [PartId.SignalLamp] = (8, WorkbenchTable.ActivationNodes),
        [PartId.Delay] = (8, WorkbenchTable.ActivationNodes),
        [PartId.BallDetector] = (8, WorkbenchTable.ActivationNodes),
        [PartId.Counter] = (8, WorkbenchTable.ActivationNodes),
        [PartId.Clock] = (8, WorkbenchTable.ActivationNodes),
        [PartId.Latch] = (8, WorkbenchTable.ActivationNodes),
        [PartId.Receiver] = (32, null),
        [PartId.Ramp] = (32, null),
        [PartId.Wall] = (32, null),
        [PartId.Pipe] = (32, null),
        [PartId.PipeBend45] = (32, null),
        [PartId.PipeBend90] = (32, null),
        [PartId.Funnel] = (32, null),
        [PartId.PressurePlate] = (32, null),
    };

    public static TheoryData<string> FreeKinds => new(WorkshopInventoryPolicy.Free.Keys.Select(kind => kind.Value));

    private static IWorkshopInstance Place(PartId kind, GpuBodyId id, double x, double z) => kind.Value switch
    {
        "ball" or "bowling" => WorkshopInput.Ball(kind, id, x, 4, z, 0, 0, 0, 1),
        "domino" => WorkshopInput.Domino(id, x, DominoUpright + .01, z, 0, 0, 0, 1),
        "basket" => WorkshopInput.Receiver(id, x, 1, z, 0, 0, 0, 1),
        "ramp" => WorkshopInput.Ramp(id, x, 2, z, 0, 0, 0, 1, RampDimensions.Default),
        "wall" => WorkshopInput.Wall(id, x, 2, z, 0, 0, 0, 1, WallDimensions.Default),
        "switch" => WorkshopInput.Switch(id, x, 1, z, 0, 0, 0, 1, ContactTriggerSettings.Default),
        "lamp" => WorkshopInput.Lamp(id, x, 1, z, 0, 0, 0, 1),
        "delay" => WorkshopInput.Delay(id, x, 1, z, 0, 0, 0, 1, DelayDuration.Default),
        "bumper" => WorkshopInput.Bumper(id, x, 1, z, 0, 0, 0, 1, BumperWork.Default),
        "battery" => WorkshopInput.Battery(id, x, 1, z, 0, 0, 0, 1, ElectricalSourceSettings.Default),
        "spring" => WorkshopInput.Springboard(id, x, 1, z, 0, 0, 0, 1, SpringboardSettings.Default),
        "pipe" => WorkshopInput.Pipe(id, x, 3, z, 0, 0, 0, 1),
        "ball_detector" => WorkshopInput.BallDetector(id, x, 3, z, 0, 0, 0, 1),
        "pipe_bend_45" => WorkshopInput.PipeBend45(id, x, 3, z, 0, 0, 0, 1),
        "pipe_bend_90" => WorkshopInput.PipeBend90(id, x, 3, z, 0, 0, 0, 1),
        "funnel" => WorkshopInput.Funnel(id, x, 3, z, 0, 0, 0, 1),
        "powered_gate" => WorkshopInput.Generic(PartId.PoweredGate, id, x, 0, z, 0, 0, 0, 1, WorkshopCanonicalColliders.PoweredGateColliders),
        "pressure_plate" => WorkshopInput.PressurePlate(id, x, 1, z, 0, 0, 0, 1, WorkshopPressurePlate.PlacedMinimumMass),
        "counter" => WorkshopInput.Counter(id, x, 1, z, 0, 0, 0, 1, WorkshopCounter.PlacedTargetCount),
        "clock" => WorkshopInput.Clock(id, x, 1, z, 0, 0, 0, 1, WorkshopClock.PlacedIntervalSeconds),
        "latch" => WorkshopInput.Latch(id, x, 1, z, 0, 0, 0, 1),
        _ => throw new ArgumentException($"Unsupported palette part '{kind}'.", nameof(kind)),
    };

    // Eight columns by four rows, wide enough that no moving part starts inside a neighbour.
    private static WorkshopInstances Grid(PartId kind, int count) =>
        new([.. Enumerable.Range(0, count).Select(i => Place(kind, new((ulong)i + 1), -6 + i % 8 * 1.6, -5 + i / 8 * 2.5))]);

    // The Story 9.4b harness admits at revision 2 through the production WorkshopSimulation, WorkshopGpuDevice and BrowserGpuTransport.
    private static WorkshopConstruction Bench(WorkshopInstances instances) => new(new(2), WorkshopCadenceSettings.Default(), instances);

    [Theory]
    [MemberData(nameof(FreeKinds))]
    public async Task EveryFreePaletteKindCompilesAndStepsAtTheLargestCountBuildAdmits(string kindId)
    {
        var kind = new PartId(kindId);
        var (largest, rejects) = Budgets[kind];
        if (rejects is { } table)
        {
            // Another declared budget rejects 32 first, with its existing Build message; one fewer than the largest count fits.
            foreach (var count in new[] { WorkshopInstances.Capacity, largest + 1 })
            {
                var error = Assert.Throws<WorkbenchFullException>(() => Bench(Grid(kind, count)).Validate());
                Assert.Equal(table, error.Table);
                Assert.StartsWith("Workbench is full: ", error.Message);
            }
        }
        else Assert.Equal(WorkshopInstances.Capacity, largest);
        var bench = Bench(Grid(kind, largest));
        bench.Validate();
        var scene = WorkshopPhysicsCompiler.Compile(bench, new(1, 2));
        Assert.Equal(1 + largest * WorkbenchFootprint.Of(kind).Colliders, scene.Colliders.Length);
        var reads = await WorkshopPhysicsEngineTests.Run(bench, SteppedTicks);
        Assert.Equal(new SimulationTick(SteppedTicks), reads[^1].Tick);
    }

    [Fact]
    public void TheBudgetTableCoversExactlyTheFreePaletteAndExcludesTheUndeliveredParts()
    {
        Assert.Equal(WorkshopInventoryPolicy.Free.Keys.OrderBy(kind => kind.Value), Budgets.Keys.OrderBy(kind => kind.Value));
        // Spec D2: Tennis and Balloon throw in CaptureInstance and Story 9R.0c removed them from the palette, so Build never admits them.
        foreach (var undelivered in new[] { PartId.Tennis, PartId.Balloon })
        {
            Assert.False(WorkshopInventoryPolicy.Free.ContainsKey(undelivered));
            Assert.Throws<ArgumentException>(() => WorkbenchFootprint.Of(undelivered));
        }
    }

    // The plane alone, then 28 bends (2,017 colliders) and 29 bends (2,089) straddling the retired 2,048 ceiling, then the full bench.
    [Theory]
    [InlineData(0)]
    [InlineData(28)]
    [InlineData(29)]
    [InlineData(32)]
    public async Task BendBenchesAcrossTheRetiredColliderCeilingCompileAndStep(int bends)
    {
        var bench = Bench(Grid(PartId.PipeBend90, bends));
        bench.Validate();
        Assert.Equal(1 + bends * 72, WorkshopPhysicsCompiler.Compile(bench, new(1, 2)).Colliders.Length);
        var reads = await WorkshopPhysicsEngineTests.Run(bench, SteppedTicks);
        Assert.Equal(new SimulationTick(SteppedTicks), reads[^1].Tick);
    }

    // Thirty bends to the left, then (with the last bend) a cradle around the ball: 31 x 72 + plane + ball = 2,234 colliders, and the
    // ball rests inside the cradle's tube on colliders whose slots lie beyond the retired 2,048 ceiling. The geometry is the
    // capacity-9r Chrome bench's (placement plane y 3). Control: without that bend the ball falls to the bench.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ABallRestsInTheLastOfThirtyOneBendsAndFallsWithoutIt(bool withLastBend)
    {
        var parts = Enumerable.Range(0, 30)
            .Select(i => (IWorkshopInstance)WorkshopInput.PipeBend90(new((ulong)i + 1), -6 + i % 6, 3, -3 + i / 6 * 1.5, 0, 0, 0, 1))
            .ToList();
        var cradle = new GpuBodyId(31);
        if (withLastBend) parts.Add(WorkshopInput.PipeBend90(cradle, 6, 3, 0, 0, 0, CradleZ, CradleW));
        var ball = WorkshopInput.Basketball(new(32), 6, 3, 0, 0, 0, 0, 1);
        parts.Add(ball);
        var bench = Bench(new([.. parts]));
        bench.Validate();
        var scene = WorkshopPhysicsCompiler.Compile(bench, new(1, 2));
        Assert.Equal(withLastBend ? 2234 : 2162, scene.Colliders.Length);
        if (withLastBend)
            Assert.All(Enumerable.Range(0, scene.Colliders.Length).Where(slot => scene.Colliders[slot].Body == cradle),
                slot => Assert.True(slot > 2048, $"cradle collider slot {slot} lies beyond the retired ceiling"));
        var reads = await WorkshopPhysicsEngineTests.Run(bench, 3 * 120);
        var rest = reads[^1].Bodies[0];
        Assert.Equal(ball.Id, rest.Body.Id);
        var height = (rest.Body.Cell.Y + rest.Body.Local.Y) / 16f;
        if (withLastBend) Assert.InRange(height, 2.5f, 2.9f); // resting on the cradle tube's lowest wall
        else Assert.True(height < 0, $"without the cradle the ball falls to the bench (height {height})");
    }

    [Fact]
    public void ThirtyThreeBendsAreRejectedAtBuildWithThePartTableMessage()
    {
        var error = Assert.Throws<WorkbenchFullException>(() => Bench(Grid(PartId.PipeBend90, 32)).WithInstance(
            WorkshopInput.PipeBend90(new(33), 6, 3, 4, 0, 0, 0, 1)).Validate());
        Assert.Equal(WorkbenchTable.Instances, error.Table);
        Assert.Equal("Workbench is full: part table", error.Message);
    }

    // One simulation, device and transport admit benches of changing size in turn: 1, 32, 1 and 32 bends, each Run, stepped and Reset.
    [Fact]
    public async Task OneTransportAdmitsBenchesOfChangingColliderCount()
    {
        await using var device = new WorkshopGpuDevice(new BrowserGpuTransport(), new(101, 206));
        await using var simulation = new WorkshopSimulation(device, new WorkshopPhysicsEngineTests.StepClock(), WorkshopCadenceSettings.Default(),
            new(1), new WorkshopPhysicsEngineTests.NoInstallation(), GpuDeviceOwnership.Borrowed);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Initialize()).Outcome);
        var revision = 1UL;
        foreach (var bends in new[] { 1, 32, 1, 32 })
        {
            var bench = new WorkshopConstruction(new(++revision), WorkshopCadenceSettings.Default(), Grid(PartId.PipeBend90, bends));
            Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Construct(bench)).Outcome);
            Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Run()).Outcome);
            for (var tick = 0; tick < 3; tick++) Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Advance()).Outcome);
            Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Reset()).Outcome);
            Assert.Equal(WorkshopSimulationPhase.Building, simulation.Phase);
        }
    }

    [Fact]
    public void TheLargestBuildAdmittedColliderPopulationFitsSixteenBitColliderSlots()
    {
        var widest = WorkshopInventoryPolicy.Free.Keys.Max(kind => WorkbenchFootprint.Of(kind).Colliders);
        Assert.True(WorkbenchFootprint.Plane.Colliders + WorkshopInstances.Capacity * widest <= ushort.MaxValue + 1,
            $"{WorkshopInstances.Capacity} x {widest} colliders exceed the u16 collider slot");
    }

    [Fact]
    public void FreeInventoryListsEveryPlayableKindUnlimitedInPaletteRowOrder()
    {
        PartId[] rows = [PartId.Basketball, PartId.Receiver, PartId.ImpactSwitch, PartId.SignalLamp,
            PartId.Wall, PartId.Delay, PartId.PinballBumper, PartId.Ramp, PartId.Domino, PartId.BowlingBall, PartId.Battery, PartId.Springboard, PartId.Pipe, PartId.BallDetector, PartId.PipeBend45, PartId.PipeBend90, PartId.Funnel, PartId.PoweredGate, PartId.PressurePlate, PartId.Counter, PartId.Clock, PartId.Latch];
        Assert.Equal(rows, WorkshopInventoryPolicy.Free.Keys.ToArray());
        Assert.All(WorkshopInventoryPolicy.Free.Values, allowance => Assert.Equal(PartAllowance.Unlimited, allowance));
        Assert.Equal(rows.Length, WorkshopInventoryPolicy.Free.Count);
        Assert.False(PartAllowance.Unlimited.Less(1000).Exhausted);
        Assert.Equal(PartAllowance.Unlimited, PartAllowance.Unlimited.Less(1000));
        Assert.False(PartAllowance.Counted(2).Less(1).Exhausted);
        Assert.True(PartAllowance.Counted(2).Less(2).Exhausted);
        Assert.Equal(PartAllowance.None, PartAllowance.Counted(2).Less(3)); // over-placed parts clamp at zero, never "× -1"
        Assert.Equal(PartAllowance.Unlimited, WorkshopInventoryPolicy.Free[PartId.Ramp]);
        Assert.False(WorkshopInventoryPolicy.Free.ContainsKey(new PartId("unsupported")));
        Assert.Throws<ArgumentException>(() => new PartInventoryDictionary((PartId.Wall, PartAllowance.Unlimited), (PartId.Wall, PartAllowance.None)));
        Assert.True(PartAllowance.None.Exhausted);
    }
}
