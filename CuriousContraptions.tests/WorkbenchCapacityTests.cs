using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>Free play has no per-kind population rule; the compiled table capacities are the only ceiling and reject with a typed reason.</summary>
public sealed class WorkbenchCapacityTests
{
    private static ulong _next = 1;
    private static GpuBodyId Id() => new(_next++);
    private static double X(int i) => -6 + i * .4;
    private static IWorkshopInstance Make(WorkshopPartKind kind, int i) => kind switch
    {
        WorkshopPartKind.Basketball => WorkshopInput.Basketball(Id(), X(i), 5, 0, 0, 0, 0, 1),
        WorkshopPartKind.Battery => new WorkshopBattery(Id(), default, default, CanonicalRotation.Identity, ElectricalSourceSettings.Default),
        WorkshopPartKind.BowlingBall => WorkshopInput.Ball(WorkshopPartKind.BowlingBall, Id(), X(i), 5, 0, 0, 0, 0, 1),
        WorkshopPartKind.Receiver => WorkshopInput.Receiver(Id(), X(i), 1, 2, 0, 0, 0, 1),
        WorkshopPartKind.Ramp => WorkshopInput.Ramp(Id(), X(i), 2, -2, 0, 0, 0, 1, RampDimensions.Default),
        WorkshopPartKind.Wall => WorkshopInput.Wall(Id(), X(i), 2, 4, 0, 0, 0, 1, WallDimensions.Default),
        WorkshopPartKind.ImpactSwitch => WorkshopInput.Switch(Id(), X(i), 1, -4, 0, 0, 0, 1, ContactTriggerSettings.Default),
        WorkshopPartKind.SignalLamp => WorkshopInput.Lamp(Id(), X(i), 1, 6, 0, 0, 0, 1),
        WorkshopPartKind.Delay => WorkshopInput.Delay(Id(), X(i), 1, -6, 0, 0, 0, 1, DelayDuration.Default),
        WorkshopPartKind.PinballBumper => WorkshopInput.Bumper(Id(), X(i), 3, 0, 0, 0, 0, 1, BumperWork.FromCanonicalStrength((float)8)),
        WorkshopPartKind.Domino => WorkshopInput.Domino(Id(), X(i), .09, 2, 0, 0, 0, 1),
        _ => throw new ArgumentException("Unsupported test kind.")
    };
    private static WorkshopInstances Population(params (WorkshopPartKind Kind, int Count)[] groups)
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
        var full = Population((WorkshopPartKind.Basketball, 16), (WorkshopPartKind.Receiver, 1), (WorkshopPartKind.ImpactSwitch, 8), (WorkshopPartKind.Wall, 7));
        var scene = WorkshopPhysicsCompiler.Compile(Free(full), new(1, 2));
        Assert.Equal(WorkshopInstances.Capacity, full.Count);
        Assert.Equal(PhysicsSceneDeclaration.BodyCapacity, scene.Bodies.Length);
        Assert.Equal(PhysicsBodyReadSet.Capacity, Dynamic(scene));
        Assert.Equal(PhysicsSceneDeclaration.SensorCapacity, scene.Sensors.Length);
        Assert.Equal(PhysicsSceneDeclaration.GuideCapacity, scene.Guides.Length);
        Assert.Equal(PhysicsSceneDeclaration.TriggerCapacity, scene.Triggers.Length);
        Assert.Equal(ActivationNetwork.Capacity, (int)WorkshopActivationCompiler.Compile(Free(full)).Clear().Count);

        // Eight bumpers (the whole contact-work table), eight activation nodes across three kinds, three ramps, five balls.
        var mixed = Population((WorkshopPartKind.PinballBumper, 8), (WorkshopPartKind.Delay, 3), (WorkshopPartKind.SignalLamp, 3),
            (WorkshopPartKind.ImpactSwitch, 2), (WorkshopPartKind.Ramp, 3), (WorkshopPartKind.Receiver, 1), (WorkshopPartKind.Basketball, 5));
        scene = WorkshopPhysicsCompiler.Compile(Free(mixed), new(1, 2));
        Assert.Equal(PhysicsSceneDeclaration.ContactWorkCapacity, scene.ContactWorks.Length);
        Assert.Equal(5, scene.Sensors.Length);
        Assert.Equal(ActivationNetwork.Capacity, (int)WorkshopActivationCompiler.Compile(Free(mixed)).Clear().Count);

        // Several receivers compile while no ball needs guiding; each keeps its own body and walls.
        var receivers = Population((WorkshopPartKind.Receiver, 3), (WorkshopPartKind.Ramp, 2));
        var receiverIds = receivers.OfType<WorkshopReceiver>().Select(r => r.Id).ToHashSet();
        scene = WorkshopPhysicsCompiler.Compile(Free(receivers), new(1, 2));
        Assert.Equal(3, scene.Bodies.ToArray().Count(b => receiverIds.Contains(b.Id)));
        Assert.Equal(3 * ReceiverGeometry.Walls.Length, scene.Colliders.ToArray().Count(c => receiverIds.Contains(c.Body)));
        Assert.Empty(scene.Sensors.ToArray()); Assert.Empty(scene.Guides.ToArray());

        // Thirty-two of one static kind fills the part table exactly.
        WorkshopPhysicsCompiler.Compile(Free(Population((WorkshopPartKind.Wall, 32))), new(1, 2));
    }

    [Theory]
    [InlineData(WorkbenchTable.Instances, WorkshopPartKind.Wall, 33, WorkshopPartKind.Wall, 0)]
    [InlineData(WorkbenchTable.DynamicBodies, WorkshopPartKind.Basketball, 17, WorkshopPartKind.Wall, 0)]
    [InlineData(WorkbenchTable.DynamicBodies, WorkshopPartKind.Domino, 9, WorkshopPartKind.Basketball, 8)]
    [InlineData(WorkbenchTable.DynamicBodies, WorkshopPartKind.BowlingBall, 9, WorkshopPartKind.Basketball, 8)]
    [InlineData(WorkbenchTable.Colliders, WorkshopPartKind.Receiver, 12, WorkshopPartKind.ImpactSwitch, 3)]
    [InlineData(WorkbenchTable.Sensors, WorkshopPartKind.Receiver, 2, WorkshopPartKind.Basketball, 9)]
    [InlineData(WorkbenchTable.Sensors, WorkshopPartKind.Receiver, 2, WorkshopPartKind.BowlingBall, 9)]
    [InlineData(WorkbenchTable.Guides, WorkshopPartKind.Receiver, 2, WorkshopPartKind.Basketball, 1)]
    [InlineData(WorkbenchTable.Guides, WorkshopPartKind.Receiver, 2, WorkshopPartKind.BowlingBall, 1)]
    [InlineData(WorkbenchTable.ElectricalSources, WorkshopPartKind.Battery, 9, WorkshopPartKind.Wall, 0)]
    [InlineData(WorkbenchTable.Triggers, WorkshopPartKind.ImpactSwitch, 9, WorkshopPartKind.Wall, 0)]
    [InlineData(WorkbenchTable.ContactWork, WorkshopPartKind.PinballBumper, 9, WorkshopPartKind.Wall, 0)]
    [InlineData(WorkbenchTable.ActivationNodes, WorkshopPartKind.SignalLamp, 5, WorkshopPartKind.Delay, 4)]
    public void ExceedingOneCompiledTableRejectsWithThatTypedReason(WorkbenchTable table, WorkshopPartKind first, int firstCount, WorkshopPartKind second, int secondCount)
    {
        var instances = Population((first, firstCount), (second, secondCount));
        var error = Assert.Throws<WorkbenchFullException>(() => instances.Validate());
        Assert.Equal(table, error.Table);
        Assert.StartsWith("Workbench is full: ", error.Message);
        Assert.Throws<WorkbenchFullException>(() => Free(instances).Validate());
        // One part fewer of the first kind fits again.
        Free(Population((first, firstCount - 1), (second, secondCount))).Validate();
    }

    [Theory]
    [InlineData(WorkshopPartKind.Basketball)]
    [InlineData(WorkshopPartKind.Receiver)]
    [InlineData(WorkshopPartKind.Ramp)]
    [InlineData(WorkshopPartKind.Wall)]
    [InlineData(WorkshopPartKind.ImpactSwitch)]
    [InlineData(WorkshopPartKind.SignalLamp)]
    [InlineData(WorkshopPartKind.Delay)]
    [InlineData(WorkshopPartKind.PinballBumper)]
    [InlineData(WorkshopPartKind.Domino)]
    [InlineData(WorkshopPartKind.Battery)]
    [InlineData(WorkshopPartKind.BowlingBall)]
    public void DeclaredFootprintMatchesWhatTheCompilersEmit(WorkshopPartKind kind)
    {
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
        Assert.Throws<ArgumentException>(() => WorkbenchFootprint.Of(WorkshopPartKind.Unsupported));
    }

    [Fact]
    public void CampaignInventoriesAndFixturesAreUnchanged()
    {
        // Fixture identities sit far above the shared test counter so WithInstance appends rather than replaces.
        var first = FirstPrinciples.Create(new(1), WorkshopCadenceSettings.Default(), new(100001), new(100002), new((Half).5));
        Assert.Equal(WorkshopPartKind.Ramp, first.Puzzle.InventoryKind);
        Assert.Equal(2u, first.Puzzle.InventoryCount);
        var ramps = first.WithInstance(Make(WorkshopPartKind.Ramp, 0)).WithInstance(Make(WorkshopPartKind.Ramp, 1));
        ramps.Validate();
        Assert.Throws<ArgumentException>(() => ramps.WithInstance(Make(WorkshopPartKind.Ramp, 2)).Validate());
        Assert.ThrowsAny<ArgumentException>(() => first.WithInstance(Make(WorkshopPartKind.Receiver, 3)).Validate());
        Assert.Throws<ArgumentException>(() => first.WithInstance(Make(WorkshopPartKind.Wall, 4)).Validate());

        var delayed = DelayedSignal.Create(new(1), WorkshopCadenceSettings.Default(), new(100001), new(100002), new(100003), new((Half).5));
        Assert.Equal(WorkshopPartKind.Delay, delayed.Puzzle.InventoryKind);
        Assert.Equal(1u, delayed.Puzzle.InventoryCount);
        var delay = delayed.WithInstance(Make(WorkshopPartKind.Delay, 0));
        delay.Validate();
        Assert.Throws<ArgumentException>(() => delay.WithInstance(Make(WorkshopPartKind.Delay, 1)).Validate());
        Assert.Throws<ArgumentException>(() => delayed.WithInstance(Make(WorkshopPartKind.SignalLamp, 2)).Validate());
        Assert.Throws<ArgumentException>(() => delayed.WithInstance(Make(WorkshopPartKind.ImpactSwitch, 3)).Validate());

        var inventory = WorkshopInventoryPolicy.Authored(first.Puzzle);
        Assert.Equal(PartAllowance.Counted(2), Assert.Single(inventory).Value);
        Assert.Equal(PartAllowance.Counted(1), WorkshopInventoryPolicy.Authored(delayed.Puzzle)[WorkshopPartKind.Delay]);
        Assert.Throws<ArgumentException>(() => WorkshopInventoryPolicy.Authored(default));
    }

    [Fact]
    public void FreeInventoryListsEveryPlayableKindUnlimitedInPaletteRowOrder()
    {
        WorkshopPartKind[] rows = [WorkshopPartKind.Basketball, WorkshopPartKind.Receiver, WorkshopPartKind.ImpactSwitch, WorkshopPartKind.SignalLamp,
            WorkshopPartKind.Wall, WorkshopPartKind.Delay, WorkshopPartKind.PinballBumper, WorkshopPartKind.Ramp, WorkshopPartKind.Domino, WorkshopPartKind.BowlingBall, WorkshopPartKind.Battery];
        Assert.Equal(rows, WorkshopInventoryPolicy.Free.Keys.ToArray());
        Assert.All(WorkshopInventoryPolicy.Free.Values, allowance => Assert.Equal(PartAllowance.Unlimited, allowance));
        Assert.Equal(Enum.GetValues<WorkshopPartKind>().Count(kind => kind != WorkshopPartKind.Unsupported), WorkshopInventoryPolicy.Free.Count);
        Assert.False(PartAllowance.Unlimited.Less(1000).Exhausted);
        Assert.Equal(PartAllowance.Unlimited, PartAllowance.Unlimited.Less(1000));
        Assert.False(PartAllowance.Counted(2).Less(1).Exhausted);
        Assert.True(PartAllowance.Counted(2).Less(2).Exhausted);
        Assert.Equal(PartAllowance.None, PartAllowance.Counted(2).Less(3)); // over-placed parts clamp at zero, never "× -1"
        Assert.Equal(PartAllowance.Unlimited, WorkshopInventoryPolicy.Free[WorkshopPartKind.Ramp]);
        Assert.False(WorkshopInventoryPolicy.Free.ContainsKey(WorkshopPartKind.Unsupported));
        Assert.Throws<ArgumentException>(() => new PartInventory((WorkshopPartKind.Wall, PartAllowance.Unlimited), (WorkshopPartKind.Wall, PartAllowance.None)));
        Assert.True(PartAllowance.None.Exhausted);
    }
}
