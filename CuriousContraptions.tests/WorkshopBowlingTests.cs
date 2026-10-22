using System.Buffers.Binary;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>The Bowling ball is the Basketball's record with another declared material (CAT-014): no solver path of its own, only
/// data the shared engine reads. Two lanes of identical stock Dominoes are the construction the Chrome proof builds.</summary>
public sealed class WorkshopBowlingTests
{
    private const double Upright = -0.46 + .55;
    private static WorkshopBall Bowling(ulong id, double x, double y = 3, double z = 0) =>
        WorkshopInput.Ball(PartId.BowlingBall, new(id), x, y, z, 0, 0, 0, 1);
    private static WorkshopConstruction TwoLanes() => new(new(1), WorkshopCadenceSettings.Default(),
        new(WorkshopInput.Domino(new(1), 0, Upright, -1.5, 0, 0, 0, 1), WorkshopInput.Domino(new(2), 0, Upright, 1.5, 0, 0, 0, 1),
        WorkshopInput.Basketball(new(3), .3, 1.05, -1.5, 0, 0, 0, 1), Bowling(4, .3, 1.05, 1.5)));

    [Fact]
    public void EachBallKindDeclaresItsOwnMaterialAndRejectsEveryOtherBitPattern()
    {
        var bowling = BallMaterial.For(PartId.BowlingBall);
        Assert.Equal(new BallMaterial(new((Half).28), new((Half)4), new((Half).14), new((Half).04), new((Half)0), new((Half).3), new((Half).1), new((Half).03)), bowling);
        var basketball = BallMaterial.For(PartId.Basketball);
        Assert.Equal(new BallMaterial(new((Half).34), new((Half)1), new((Half).55), new((Half).04), new((Half)0), new((Half).3), new((Half).1), new((Half).035)), basketball);
        Assert.Throws<ArgumentException>(() => BallMaterial.For(PartId.Receiver));
        Assert.Throws<ArgumentException>(() => BallMaterial.For(PartId.Unsupported));
        // A ball carries the material of its own kind only.
        Assert.Throws<ArgumentException>(() => bowling.Validate(PartId.Basketball));
        Assert.Throws<ArgumentException>(() => basketball.Validate(PartId.BowlingBall));
        var ball = Bowling(4, .3, 1.05, 1.5);
        Assert.Equal(PartId.BowlingBall, ball.PartId);
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
            ball with { PartId = PartId.Basketball },
            ball with { PartId = PartId.Domino }
        })
            Assert.Throws<ArgumentException>(() => invalid.Validate());
        Assert.Throws<ArgumentException>(() => WorkshopInput.Ball(PartId.Domino, new(4), 0, 3, 0, 0, 0, 0, 1));
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
        var decoded = WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes);
        Assert.Equal(save, decoded);
        Assert.Equal(bytes, WorkshopSaveCodec.Encode(decoded));
        var basketball = Assert.IsType<WorkshopBall>(decoded.Construction.Instances[2]);
        var bowling = Assert.IsType<WorkshopBall>(decoded.Construction.Instances[3]);
        Assert.Equal(PartId.Basketball, basketball.PartId);
        Assert.Equal(BallMaterial.For(PartId.Basketball), basketball.Material);
        Assert.Equal(PartId.BowlingBall, bowling.PartId);
        Assert.Equal(BallMaterial.For(PartId.BowlingBall), bowling.Material);
        var fbSave = WorkshopSaveCodec.Open(bytes);
        Assert.True(fbSave.Construction.HasValue);
        var fbBasketball = fbSave.Construction.Value.Instances(2)!.Value;
        var fbBowling = fbSave.Construction.Value.Instances(3)!.Value;
        Assert.Equal("ball", fbBasketball.PartId);
        Assert.Equal("bowling", fbBowling.PartId);
        Assert.Equal((float)BallMaterial.For(PartId.BowlingBall).Radius.Value, fbBowling.Radius);
        Assert.Equal((float)BallMaterial.For(PartId.BowlingBall).Mass.Value, fbBowling.Mass);
        foreach (var forgedMaterial in new[]
        {
            bowling.Material with { Radius = new((Half)17) },
            bowling.Material with { Mass = new((Half)17) },
            bowling.Material with { Bounce = new((Half)17) },
            bowling.Material with { Drag = new((Half)17) },
            bowling.Material with { Buoyancy = new((Half)17) }
        })
        {
            var forgedConstruction = decoded.Construction with {
                Instances = new WorkshopInstances(decoded.Construction.Instances[0], decoded.Construction.Instances[1], decoded.Construction.Instances[2], bowling with { Material = forgedMaterial })
            };
            var builder = new Google.FlatBuffers.FlatBufferBuilder(512);
            var crOffset = WorkshopWireFlatBuffers.EncodeConstructionRecord(builder, forgedConstruction, validate: false);
            var savedBytes = SaveFixture.Finish(builder, crOffset, 5);
            SaveFixture.Refused(WorkshopSaveRejection.OutOfRange, () => WorkshopSaveCodec.Decode(SaveFixture.Elements,savedBytes));
        }
        // Material bits of one kind under the other kind's tag are not that kind.
        var swapped1 = decoded.Construction with {
            Instances = new WorkshopInstances(decoded.Construction.Instances[0], decoded.Construction.Instances[1], decoded.Construction.Instances[2], bowling with { Material = BallMaterial.For(PartId.Basketball) })
        };
        var b1 = new Google.FlatBuffers.FlatBufferBuilder(512);
        var cr1 = WorkshopWireFlatBuffers.EncodeConstructionRecord(b1, swapped1, validate: false);
        var swappedBytes1 = SaveFixture.Finish(b1, cr1, 5);
        SaveFixture.Refused(WorkshopSaveRejection.OutOfRange, () => WorkshopSaveCodec.Decode(SaveFixture.Elements,swappedBytes1));

        var swapped2 = decoded.Construction with {
            Instances = new WorkshopInstances(decoded.Construction.Instances[0], decoded.Construction.Instances[1], basketball with { Material = BallMaterial.For(PartId.BowlingBall) }, decoded.Construction.Instances[3])
        };
        var b2 = new Google.FlatBuffers.FlatBufferBuilder(512);
        var cr2 = WorkshopWireFlatBuffers.EncodeConstructionRecord(b2, swapped2, validate: false);
        var swappedBytes2 = SaveFixture.Finish(b2, cr2, 5);
        SaveFixture.Refused(WorkshopSaveRejection.OutOfRange, () => WorkshopSaveCodec.Decode(SaveFixture.Elements,swappedBytes2));
    }

    [Fact]
    public void DeclaredDragAndRollingResistanceCompileIntoTheBodyAndMaterialRecordsAndOnlyBallsCarryACoefficient()
    {
        var scene = WorkshopPhysicsCompiler.Compile(TwoLanes(), new(1, 2));
        var bytes = PhysicsGpuAbi.Admission(scene, new(1), new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        var bodies = scene.Bodies.ToArray(); var colliders = scene.Colliders.ToArray(); var materials = scene.Materials.ToArray();
        // Body record +90 carries the declared linear drag; material record +14 the rolling-resistance coefficient (Domino tiles: zero).
        foreach (var (id, drag, rolling) in new[] { (3UL, (ushort)10527, (ushort)10363), (4UL, (ushort)10527, (ushort)10158), (1UL, (ushort)0, (ushort)0), (2UL, (ushort)0, (ushort)0) })
        {
            var slot = Array.FindIndex(bodies, b => b.Id == new GpuBodyId(id));
            Assert.Equal(drag, BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(scene.World.BodyOffset(slot) + PhysicsGpuAbi.BodyDragOffset)));
            Assert.Equal(90, PhysicsGpuAbi.BodyDragOffset);
            var collider = Assert.Single(colliders, c => c.Body == new GpuBodyId(id));
            var material = Array.FindIndex(materials, m => m.Id == collider.Material);
            Assert.Equal(rolling, BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(scene.World.MaterialOffset(material) + 14)));
        }
        // The bench plane (the first material record) declares zero.
        Assert.Equal((ushort)0, BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(scene.World.MaterialOffset(0) + 14)));
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
        // Story 9R.5b: the retired 16-row moving-body table no longer refuses; a seventeenth ball of either kind builds and
        // compiles as one more moving body.
        foreach (var seventeenth in new IWorkshopInstance[] { Bowling(17, 7), WorkshopInput.Basketball(new(17), 7, 3, 0, 0, 0, 0, 1) })
        {
            var seventeen = new WorkshopInstances([.. items, seventeenth]);
            seventeen.Validate();
            var bench = WorkshopPhysicsCompiler.Compile(new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), seventeen), new(1, 2)).World;
            Assert.Equal(17, bench.Dynamic);
        }
        // The compiled scene is the footprint authority; the workbench plane adds one static body and collider.
        var world = WorkshopPhysicsCompiler.Compile(new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new WorkshopInstances(Bowling(1, 0))), new(1, 2)).World;
        Assert.Equal((1 + 1, 1, 1 + 1, 0, 0, 0), (world.Bodies, world.Dynamic, world.Colliders, world.Triggers, world.ContactWorks, world.ElectricalSources));
        Assert.Empty(DeclaredPorts.Current.Of(PartId.BowlingBall).ToArray());
        Assert.Equal(PartAllowance.Unlimited, WorkbenchCapacityTests.FreePalette[PartId.BowlingBall]);
        Assert.Contains(PartId.BowlingBall, WorkbenchCapacityTests.FreePalette.Keys);
        Assert.Contains(PartId.Battery, WorkbenchCapacityTests.FreePalette.Keys);
        Assert.Contains(PartId.Springboard, WorkbenchCapacityTests.FreePalette.Keys);

        // One Receiver declares a residence sensor and a guide per ball of either kind, with the shared capture settings.
        var receiver = WorkshopInput.Generic(PartId.Receiver, new(5), 0, 1, 0, 0, 0, 0, 1);
        var scene = WorkshopPhysicsCompiler.Compile(TwoLanes().WithInstance(receiver), new(1, 2));
        Assert.Equal([new GpuBodyId(3), new GpuBodyId(4)], scene.Sensors.ToArray().Select(s => s.Target).OrderBy(id => id.Value).ToArray());
        Assert.Equal([new GpuBodyId(3), new GpuBodyId(4)], scene.Guides.ToArray().Select(g => g.Target).OrderBy(id => id.Value).ToArray());
        Assert.All(scene.Sensors.ToArray(), s => Assert.Equal(new LinearSpeed((Half)1.5), s.SpeedLimit));
        var second = WorkshopInput.Generic(PartId.Receiver, new(6), 4, 1, 0, 0, 0, 0, 1);
        Assert.Equal(WorkbenchTable.Guides, Assert.Throws<WorkbenchFullException>(() => TwoLanes().WithInstance(receiver).WithInstance(second).Validate()).Table);
    }

    [Fact]
    public void AuthoredPuzzlesRequireTheBasketballKindAndNeverOfferTheBowlingBall()
    {
        var first = PuzzleCatalogue.Create(new("first_principles"), new(1), WorkshopCadenceSettings.Default(), new(100001), new((Half).5));
        var delayed = PuzzleCatalogue.Create(new("delayed_signal"), new(1), WorkshopCadenceSettings.Default(), new(100001), new((Half).5));
        var dominoes = PuzzleCatalogue.Create(new("domino_effect"), new(1), WorkshopCadenceSettings.Default(), new(100001), new((Half).5));
        foreach (var level in new[] { first, delayed, dominoes })
        {
            var ball = level.Ball!.Value;
            Assert.Equal(PartId.Basketball, ball.PartId);
            // The same identity and pose as a Bowling ball is not the authored fixture.
            var replaced = level.WithInstance(ball with { PartId = PartId.BowlingBall, Material = BallMaterial.For(PartId.BowlingBall) });
            var error = EditRefusal.Is(WorkshopEditRejection.FixedPart, () => replaced.Validate());
            Assert.Equal("Fixed authored declaration changed.", error.Message);
            // A second ball of the heavier kind is not an inventory item either.
            EditRefusal.Is(WorkshopEditRejection.InventoryUnsupported, () => level.WithInstance(Bowling(100009, 2, 5)).Validate());
            Assert.False(WorkshopInventoryPolicy.Authored(level.Puzzle).ContainsKey(PartId.BowlingBall));
        }
        // Free play admits both kinds together.
        TwoLanes().Validate();
    }
}
