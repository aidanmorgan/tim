using CuriousContraptions.Gpu;

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
    public void HeavyCompoundConstructionsValidateAndCompileSmoothlyWithoutColliderLimits()
    {
        // 20 pipes = 480 colliders, 10 pipe bends = 400 colliders (total 880 colliders)
        var heavy = Population((PartId.Pipe, 20), (PartId.PipeBend45, 10));
        Free(heavy).Validate();
        var scene = WorkshopPhysicsCompiler.Compile(Free(heavy), new(1, 2));
        Assert.Equal(20 * 24 + 10 * 40 + 1, scene.Colliders.Length);
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
        Assert.Throws<ArgumentException>(() => ramps.WithInstance(Make(PartId.Ramp, 2)).Validate());
        Assert.ThrowsAny<ArgumentException>(() => first.WithInstance(Make(PartId.Receiver, 3)).Validate());
        Assert.Throws<ArgumentException>(() => first.WithInstance(Make(PartId.Wall, 4)).Validate());

        var delayed = DelayedSignal.Create(new(1), WorkshopCadenceSettings.Default(), new(100001), new(100002), new(100003), new((Half).5));
        Assert.Equal(PartId.Delay, delayed.Puzzle.InventoryPart);
        Assert.Equal(1u, delayed.Puzzle.InventoryCount);
        var delay = delayed.WithInstance(Make(PartId.Delay, 0));
        delay.Validate();
        Assert.Throws<ArgumentException>(() => delay.WithInstance(Make(PartId.Delay, 1)).Validate());
        Assert.Throws<ArgumentException>(() => delayed.WithInstance(Make(PartId.SignalLamp, 2)).Validate());
        Assert.Throws<ArgumentException>(() => delayed.WithInstance(Make(PartId.ImpactSwitch, 3)).Validate());

        var inventory = WorkshopInventoryPolicy.Authored(first.Puzzle);
        Assert.Equal(PartAllowance.Counted(2), Assert.Single(inventory).Value);
        Assert.Equal(PartAllowance.Counted(1), WorkshopInventoryPolicy.Authored(delayed.Puzzle)[PartId.Delay]);
        Assert.Throws<ArgumentException>(() => WorkshopInventoryPolicy.Authored(default));
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
        Assert.Throws<ArgumentException>(() => new PartInventory((PartId.Wall, PartAllowance.Unlimited), (PartId.Wall, PartAllowance.None)));
        Assert.True(PartAllowance.None.Exhausted);
    }
}
