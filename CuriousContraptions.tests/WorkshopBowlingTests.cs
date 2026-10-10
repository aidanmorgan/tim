using System.Buffers.Binary;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>The Bowling ball is the Basketball's record with another declared material (CAT-014): no solver path of its own, only
/// data the shared engine reads. Two lanes of identical stock Dominoes are the construction the Chrome proof builds.</summary>
public sealed class WorkshopBowlingTests
{
    private const double Upright = -0.46 + .55;
    private static WorkshopBall Bowling(ulong id, double x, double y = 3, double z = 0) =>
        WorkshopInput.Ball(WorkshopPartKind.BowlingBall, new(id), x, y, z, 0, 0, 0, 1);
    private static WorkshopConstruction TwoLanes() => new(new(1), WorkshopCadenceSettings.Default(),
        new(WorkshopInput.Domino(new(1), 0, Upright, -1.5, 0, 0, 0, 1), WorkshopInput.Domino(new(2), 0, Upright, 1.5, 0, 0, 0, 1),
            WorkshopInput.Basketball(new(3), .3, 1.05, -1.5, 0, 0, 0, 1), Bowling(4, .3, 1.05, 1.5)));

    [Fact]
    public void EachBallKindDeclaresItsOwnMaterialAndRejectsEveryOtherBitPattern()
    {
        var bowling = BallMaterial.For(WorkshopPartKind.BowlingBall);
        Assert.Equal(new BallMaterial(new((Half).28), new((Half)4), new((Half).14), new((Half).04), new((Half)0), new((Half).3), new((Half).1), new((Half).03)), bowling);
        var basketball = BallMaterial.For(WorkshopPartKind.Basketball);
        Assert.Equal(new BallMaterial(new((Half).34), new((Half)1), new((Half).55), new((Half).04), new((Half)0), new((Half).3), new((Half).1), new((Half).035)), basketball);
        Assert.Throws<ArgumentException>(() => BallMaterial.For(WorkshopPartKind.Receiver));
        Assert.Throws<ArgumentException>(() => BallMaterial.For(WorkshopPartKind.Unsupported));
        // A ball carries the material of its own kind only.
        Assert.Throws<ArgumentException>(() => bowling.Validate(WorkshopPartKind.Basketball));
        Assert.Throws<ArgumentException>(() => basketball.Validate(WorkshopPartKind.BowlingBall));
        var ball = Bowling(4, .3, 1.05, 1.5);
        Assert.Equal(WorkshopPartKind.BowlingBall, ball.Kind);
        Assert.False(ball.Cosmetic.IsDeclared);
        foreach (var invalid in new[]
        {
            ball with { Material = bowling with { Mass = new((Half)7.2) } },
            ball with { Material = bowling with { Radius = new((Half).11) } },
            ball with { Material = bowling with { Bounce = new((Half).55) } },
            ball with { Material = bowling with { Drag = new((Half)0) } },
            ball with { Material = bowling with { Buoyancy = new((Half).1) } },
            ball with { Material = bowling with { Friction = new((Half).6) } },
            ball with { Material = bowling with { BounceThreshold = new((Half).2) } },
            ball with { Material = bowling with { Rolling = new((Half)0) } },
            ball with { Material = bowling with { Rolling = basketball.Rolling } },
            ball with { Material = basketball },
            ball with { Kind = WorkshopPartKind.Basketball },
            ball with { Kind = WorkshopPartKind.Domino }
        })
            Assert.Throws<ArgumentException>(() => invalid.Validate());
        Assert.Throws<ArgumentException>(() => WorkshopInput.Ball(WorkshopPartKind.Domino, new(4), 0, 3, 0, 0, 0, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Bowling(4, 65));
    }

    [Fact]
    public void BothBallsCompileAsDynamicSpheresWithTheirDeclaredMaterialsAndSolidInertia()
    {
        var scene = WorkshopPhysicsCompiler.Compile(TwoLanes(), new(1, 2));
        Assert.Equal(4, scene.Bodies.ToArray().Count(b => b.Motion == RigidMotionKind.Dynamic));
        var body = Assert.Single(scene.Bodies.ToArray(), b => b.Id == new GpuBodyId(4));
        Assert.Equal(RigidMotionKind.Dynamic, body.Motion);
        Assert.Equal(new Kilograms((Half)4), body.Mass);
        Assert.Equal(new InverseSeconds((Half).04), body.LinearDrag);
        Assert.Equal(new AccelerationVector((Half)0, (Half)(-9.81), (Half)0), body.Gravity);
        var sphere = Assert.Single(scene.Colliders.ToArray(), c => c.Body == body.Id);
        Assert.Equal(ColliderShapeKind.Sphere, sphere.Shape);
        Assert.Equal(new Metres((Half).28), sphere.Radius);
        var material = Assert.Single(scene.Materials.ToArray(), m => m.Id == sphere.Material);
        Assert.Equal(new Restitution((Half).14), material.Restitution);
        Assert.Equal(new LinearSpeed((Half).1), material.BounceThreshold);
        Assert.Equal(new FrictionCoefficient((Half).3), material.Friction);
        Assert.Equal(new RollingResistance((Half).03), material.RollingResistance);
        // The Basketball keeps its declared values through the same generic loop; friction and threshold are its material data, not constants.
        var basketball = Assert.Single(scene.Bodies.ToArray(), b => b.Id == new GpuBodyId(3));
        var basketballMaterial = Assert.Single(scene.Materials.ToArray(), m => m.Id == Assert.Single(scene.Colliders.ToArray(), c => c.Body == basketball.Id).Material);
        Assert.Equal(new Kilograms((Half)1), basketball.Mass);
        Assert.Equal(new Restitution((Half).55), basketballMaterial.Restitution);
        Assert.Equal(new FrictionCoefficient((Half).3), basketballMaterial.Friction);
        Assert.Equal(new LinearSpeed((Half).1), basketballMaterial.BounceThreshold);
        Assert.Equal(new RollingResistance((Half).035), basketballMaterial.RollingResistance);
        // Solid sphere 0.4·m·r² from the declared values; the worker reads these moments from the body record.
        var properties = RigidMassProperties.Compile(body, sphere);
        const double expected = .4 * 4 * .28 * .28;
        foreach (var moment in new[] { properties.X, properties.Y, properties.Z })
            Assert.InRange(Math.Abs(Math.ScaleB((double)moment.Mantissa, moment.Exponent) - expected) / expected, 0, .003);
        Assert.Equal(default, properties.LocalCentreOfMass);
    }

    [Fact]
    public void SaveRoundTripsBothKindsAndRejectsForgedMaterialBitsOrASwappedKind()
    {
        var save = new WorkshopSavedConstruction(TwoLanes(), new(5));
        var bytes = WorkshopSaveCodec.Encode(save);
        var decoded = WorkshopSaveCodec.Decode(bytes);
        Assert.Equal(save, decoded);
        Assert.Equal(bytes, WorkshopSaveCodec.Encode(decoded));
        var basketball = Assert.IsType<WorkshopBall>(decoded.Construction.Instances[2]);
        var bowling = Assert.IsType<WorkshopBall>(decoded.Construction.Instances[3]);
        Assert.Equal(WorkshopPartKind.Basketball, basketball.Kind);
        Assert.Equal(BallMaterial.For(WorkshopPartKind.Basketball), basketball.Material);
        Assert.Equal(WorkshopPartKind.BowlingBall, bowling.Kind);
        Assert.Equal(BallMaterial.For(WorkshopPartKind.BowlingBall), bowling.Material);
        var bowlingOffset = 24 + WorkshopWire.ConstructionHeaderBytes + 3 * WorkshopWire.InstanceBytes;
        var basketballOffset = 24 + WorkshopWire.ConstructionHeaderBytes + 2 * WorkshopWire.InstanceBytes;
        Assert.Equal((uint)WorkshopPartKind.BowlingBall, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(bowlingOffset)));
        Assert.Equal((ushort)13435, BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(bowlingOffset + 104)));
        Assert.Equal((ushort)17408, BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(bowlingOffset + 106)));
        Assert.Equal((ushort)0, BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(bowlingOffset + 114))); // rolling resistance is kind data, not persisted
        foreach (var field in new[] { 104, 106, 108, 110, 112 })
        {
            var forged = (byte[])bytes.Clone();
            BinaryPrimitives.WriteUInt16LittleEndian(forged.AsSpan(bowlingOffset + field), BitConverter.HalfToUInt16Bits((Half)17));
            Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(forged));
        }
        // Material bits of one kind under the other kind's tag are not that kind.
        var swapped = (byte[])bytes.Clone();
        BinaryPrimitives.WriteUInt32LittleEndian(swapped.AsSpan(bowlingOffset), (uint)WorkshopPartKind.Basketball);
        Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(swapped));
        var swappedBack = (byte[])bytes.Clone();
        BinaryPrimitives.WriteUInt32LittleEndian(swappedBack.AsSpan(basketballOffset), (uint)WorkshopPartKind.BowlingBall);
        Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(swappedBack));
        var padding = (byte[])bytes.Clone(); padding[bowlingOffset + 114] = 1;
        Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(padding));
        // Each instance's canonical body velocity lane (slot bytes 80..96; f32 m/s since Story 6.1c, binary16 before) is all-zero bits,
        // so saves written before the f32 change hold the same bytes and load unchanged; any live velocity, even -0, is rejected.
        for (var instance = 0; instance < decoded.Construction.Instances.Count; instance++)
        {
            var slot = 24 + WorkshopWire.ConstructionHeaderBytes + instance * WorkshopWire.InstanceBytes;
            Assert.True(bytes.AsSpan(slot + 80, 16).IndexOfAnyExcept((byte)0) < 0);
        }
        foreach (var bits in new[] { BitConverter.SingleToUInt32Bits(1f), 0x80000000u, BitConverter.SingleToUInt32Bits(float.NaN) })
        {
            var live = (byte[])bytes.Clone();
            BinaryPrimitives.WriteUInt32LittleEndian(live.AsSpan(bowlingOffset + 84), bits);
            Assert.ThrowsAny<ArgumentException>(() => WorkshopSaveCodec.Decode(live));
        }
        Assert.Equal((uint)WorkshopSaveVersion.CanonicalConstruction, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4)));
    }

    [Fact]
    public void DeclaredDragAndRollingResistanceCompileIntoTheBodyAndMaterialRecordsAndOnlyBallsCarryACoefficient()
    {
        var scene = WorkshopPhysicsCompiler.Compile(TwoLanes(), new(1, 2));
        var bytes = PhysicsGpuAbi.Admission(scene, new(1), new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        var bodies = scene.Bodies.ToArray(); var colliders = scene.Colliders.ToArray(); var materials = scene.Materials.ToArray();
        // Body record +74 carries the declared linear drag; material record +14 the rolling-resistance coefficient (Domino tiles: zero).
        foreach (var (id, drag, rolling) in new[] { (3UL, (ushort)10527, (ushort)10363), (4UL, (ushort)10527, (ushort)10158), (1UL, (ushort)0, (ushort)0), (2UL, (ushort)0, (ushort)0) })
        {
            var slot = Array.FindIndex(bodies, b => b.Id == new GpuBodyId(id));
            Assert.Equal(drag, BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(PhysicsGpuAbi.BodiesOffset + slot * PhysicsGpuAbi.BodyBytes + PhysicsGpuAbi.BodyDragOffset)));
            Assert.Equal(74, PhysicsGpuAbi.BodyDragOffset);
            var collider = Assert.Single(colliders, c => c.Body == new GpuBodyId(id));
            var material = Array.FindIndex(materials, m => m.Id == collider.Material);
            Assert.Equal(rolling, BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(PhysicsGpuAbi.MaterialsOffset + material * PhysicsGpuAbi.MaterialBytes + 14)));
        }
        // The bench plane (the first material record) declares zero.
        Assert.Equal((ushort)0, BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(PhysicsGpuAbi.MaterialsOffset + 14)));
        // The coefficient is a bounded, finite declaration in [0, 0.1].
        var declared = materials[0];
        foreach (var admitted in new[] { (Half)0, (Half).1 }) (declared with { RollingResistance = new(admitted) }).Validate();
        foreach (var rejected in new[] { (Half)(-.01), (Half).11, Half.NaN, Half.PositiveInfinity })
            Assert.Throws<ArgumentException>(() => (declared with { RollingResistance = new(rejected) }).Validate());
    }

    [Fact]
    public void CapacityCountsEveryBallKindAndTheReceiverGuidesBothBalls()
    {
        var items = new List<IWorkshopInstance>();
        for (var i = 0; i < 8; i++) items.Add(WorkshopInput.Basketball(new((ulong)(i + 1)), -6 + i * .8, 3, -2, 0, 0, 0, 1));
        for (var i = 0; i < 8; i++) items.Add(Bowling((ulong)(i + 9), -6 + i * .8, 3, 2));
        new WorkshopInstances([.. items]).Validate();
        foreach (var seventeenth in new IWorkshopInstance[] { Bowling(17, 7), WorkshopInput.Basketball(new(17), 7, 3, 0, 0, 0, 0, 1) })
        {
            var error = Assert.Throws<WorkbenchFullException>(() => new WorkshopInstances([.. items, seventeenth]).Validate());
            Assert.Equal(WorkbenchTable.DynamicBodies, error.Table);
            Assert.Equal("Workbench is full: moving body table", error.Message);
        }
        Assert.Equal(new WorkbenchFootprint(1, 1, 1, 0, 0, 0), WorkbenchFootprint.Of(WorkshopPartKind.BowlingBall));
        Assert.Empty(WorkshopPorts.For(WorkshopPartKind.BowlingBall).ToArray());
        Assert.Equal(PartAllowance.Unlimited, WorkshopInventoryPolicy.Free[WorkshopPartKind.BowlingBall]);
        Assert.Equal(WorkshopPartKind.BowlingBall, WorkshopInventoryPolicy.Free.Keys.ElementAt(9));
        Assert.Equal(WorkshopPartKind.Battery, WorkshopInventoryPolicy.Free.Keys.ElementAt(10));
        Assert.Equal(WorkshopPartKind.Springboard, WorkshopInventoryPolicy.Free.Keys.Last());

        // One Receiver declares a residence sensor and a guide per ball of either kind, with the shared capture settings.
        var receiver = WorkshopInput.Receiver(new(5), 0, 1, 0, 0, 0, 0, 1);
        var scene = WorkshopPhysicsCompiler.Compile(TwoLanes().WithInstance(receiver), new(1, 2));
        Assert.Equal([new GpuBodyId(3), new GpuBodyId(4)], scene.Sensors.ToArray().Select(s => s.Target).OrderBy(id => id.Value).ToArray());
        Assert.Equal([new GpuBodyId(3), new GpuBodyId(4)], scene.Guides.ToArray().Select(g => g.Target).OrderBy(id => id.Value).ToArray());
        Assert.All(scene.Sensors.ToArray(), s => Assert.Equal(ReceiverCaptureSettings.Free.SpeedLimit, s.SpeedLimit));
        var second = WorkshopInput.Receiver(new(6), 4, 1, 0, 0, 0, 0, 1);
        Assert.Equal(WorkbenchTable.Guides, Assert.Throws<WorkbenchFullException>(() => TwoLanes().WithInstance(receiver).WithInstance(second).Validate()).Table);
    }

    [Fact]
    public void AuthoredPuzzlesRequireTheBasketballKindAndNeverOfferTheBowlingBall()
    {
        var first = FirstPrinciples.Create(new(1), WorkshopCadenceSettings.Default(), new(100001), new(100002), new((Half).5));
        var delayed = DelayedSignal.Create(new(1), WorkshopCadenceSettings.Default(), new(100001), new(100002), new(100003), new((Half).5));
        var dominoes = DominoEffect.Create(new(1), WorkshopCadenceSettings.Default(), new(100001), new(100002), new(100003), new((Half).5));
        foreach (var level in new[] { first, delayed, dominoes })
        {
            var ball = level.Ball!.Value;
            Assert.Equal(WorkshopPartKind.Basketball, ball.Kind);
            // The same identity and pose as a Bowling ball is not the authored fixture.
            var replaced = level.WithInstance(ball with { Kind = WorkshopPartKind.BowlingBall, Material = BallMaterial.For(WorkshopPartKind.BowlingBall) });
            var error = Assert.Throws<ArgumentException>(() => replaced.Validate());
            Assert.Equal("Authored puzzles admit only their named Basketball.", error.Message);
            // A second ball of the heavier kind is not an inventory item either.
            Assert.Throws<ArgumentException>(() => level.WithInstance(Bowling(100009, 2, 5)).Validate());
            Assert.False(WorkshopInventoryPolicy.Authored(level.Puzzle).ContainsKey(WorkshopPartKind.BowlingBall));
        }
        // Free play admits both kinds together.
        TwoLanes().Validate();
    }
}
