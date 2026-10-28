using System.Buffers.Binary;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>The Bowling ball is a generic instance of its own package, differing from the Basketball only in declared material (CAT-014):
/// no solver path of its own, only data the shared engine reads. Two lanes of identical stock Dominoes are the construction the Chrome
/// proof builds.</summary>
public sealed class WorkshopBowlingTests
{
    private const double Upright = -0.46 + .55;
    private static WorkshopGenericInstance Bowling(ulong id, double x, double y = 3, double z = 0) =>
        WorkshopInput.Generic(PartId.BowlingBall, new(id), x, y, z, 0, 0, 0, 1);
    private static WorkshopConstruction TwoLanes() => new(new(1), WorkshopCadenceSettings.Default(),
        new(PlacedParts.Domino(new(1), 0, Upright, -1.5, 0, 0, 0, 1), PlacedParts.Domino(new(2), 0, Upright, 1.5, 0, 0, 0, 1),
        WorkshopInput.Generic(PartId.Basketball,new(3), .3, 1.05, -1.5, 0, 0, 0, 1), Bowling(4, .3, 1.05, 1.5)));

    // C-Balls: each kind is a zero-parameter generic instance of its own package; its material is the package's and no authored value
    // can change it. The owner-decided literals are pinned in PayloadInstanceTests.BothKindsCompileTheOwnerDeclaredMaterialsOnOneBench.
    [Fact]
    public void EachBallKindIsAZeroParameterGenericInstanceOfItsOwnPackage()
    {
        var ball = Bowling(4, .3, 1.05, 1.5);
        Assert.Equal(PartId.BowlingBall, ball.PartId);
        Assert.Empty(ball.Parameters);
        Assert.DoesNotContain(DeclaredPhysics.Of(PartId.BowlingBall).Presentation.Bindings,
            binding => binding.Driver == CuriousContraptions.Abstractions.PresentationDriver.Event);
        Assert.Empty(DeclaredPhysics.Of(PartId.BowlingBall).Metadata.Parameters);
        Assert.Empty(DeclaredPhysics.Of(PartId.Basketball).Metadata.Parameters);
        Assert.True(DeclaredPhysics.IsPayload(PartId.BowlingBall) && DeclaredPhysics.IsPayload(PartId.Basketball));
        // A declared collider is the package's own: the Bowling ball is the 0.28 m sphere whatever its pose.
        Assert.Equal(new Metres(.28f), new Metres(Assert.Single(ball.DeclaredColliders).Radius));
        // No authored parameter exists to carry another material.
        foreach (var forged in new[] { "mass", "radius", "bounce", "drag" })
            Assert.Throws<ArgumentException>(() => (ball with { Parameters = new([new(new(forged), 1f)]) }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => Bowling(4, 65));
    }

    [Fact]
    public void BothBallsCompileAsDynamicSpheresWithTheirDeclaredMaterialsAndSolidInertia()
    {
        var scene = WorkshopPhysicsCompiler.Compile(TwoLanes(), new(1, 2));
        Assert.Equal(4, scene.Bodies.ToArray().Count(b => b.Motion == RigidMotionKind.Dynamic));
        var body = Assert.Single(scene.Bodies.ToArray(), b => b.Id == new GpuBodyId(4));
        Assert.Equal(RigidMotionKind.Dynamic, body.Motion);
        Assert.Equal(new Kilograms(4f), body.Mass);
        Assert.Equal(new InverseSeconds(.04f), body.LinearDrag);
        Assert.Equal(new AccelerationVector(0f, -9.81f, 0f), body.Gravity);
        var sphere = Assert.Single(scene.Colliders.ToArray(), c => c.Body == body.Id);
        Assert.Equal(ColliderShapeKind.Sphere, sphere.Shape);
        Assert.Equal(new Metres(.28f), sphere.Radius);
        var material = Assert.Single(scene.Materials.ToArray(), m => m.Id == sphere.Material);
        Assert.Equal(new Restitution(.14f), material.Restitution);
        Assert.Equal(new LinearSpeed(.1f), material.BounceThreshold);
        Assert.Equal(new FrictionCoefficient(.3f), material.Friction);
        Assert.Equal(new RollingResistance(.03f), material.RollingResistance);
        // The Basketball keeps its declared values through the same generic loop; friction and threshold are its material data, not constants.
        var basketball = Assert.Single(scene.Bodies.ToArray(), b => b.Id == new GpuBodyId(3));
        var basketballChildren = scene.Colliders.ToArray().Where(c => c.Body == basketball.Id).ToArray();
        var basketballSphere = Assert.Single(basketballChildren, c => c.Shape == ColliderShapeKind.Sphere);
        Assert.Single(basketballChildren, c => c.Shape == ColliderShapeKind.Torus);
        Assert.All(basketballChildren, c => Assert.Equal(basketballSphere.Material, c.Material));
        var basketballMaterial = Assert.Single(scene.Materials.ToArray(), m => m.Id == basketballSphere.Material);
        Assert.Equal(new Kilograms(1f), basketball.Mass);
        Assert.Equal(new Restitution(.55f), basketballMaterial.Restitution);
        Assert.Equal(new FrictionCoefficient(.3f), basketballMaterial.Friction);
        Assert.Equal(new LinearSpeed(.1f), basketballMaterial.BounceThreshold);
        Assert.Equal(new RollingResistance(.035f), basketballMaterial.RollingResistance);
        // Solid sphere 0.4·m·r² of the declared f32 values, within its f32 roundings (the 6 · 2^-24 relative inertia bound).
        var properties = body.MassProperties;
        var expected = .4 * 4 * (double).28f * (double).28f;
        foreach (var moment in new[] { properties.X, properties.Y, properties.Z })
            Assert.InRange(Math.Abs(moment.Moment - expected) / expected, 0, 6.0 / (1 << 24));
        Assert.Equal(default, properties.LocalCentreOfMass);
    }

    [Fact]
    public void SaveRoundTripsBothKindsAsMaterialFreeRecordsAndRefusesAForgedMaterialOrParameter()
    {
        var save = new WorkshopSavedConstruction(TwoLanes(), new(5));
        var bytes = WorkshopSaveCodec.Encode(save);
        var decoded = WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes);
        Assert.Equal(save, decoded);
        Assert.Equal(bytes, WorkshopSaveCodec.Encode(decoded));
        Assert.Equal(PartId.Basketball, Assert.IsType<WorkshopGenericInstance>(decoded.Construction.Instances[2]).PartId);
        Assert.Equal(PartId.BowlingBall, Assert.IsType<WorkshopGenericInstance>(decoded.Construction.Instances[3]).PartId);
        // The stored record names the kind and nothing else: the material is the package's, never the save's.
        var fbSave = WorkshopSaveCodec.Open(bytes);
        foreach (var (index, part) in new[] { (2, "ball"), (3, "bowling") })
        {
            var record = fbSave.Construction!.Value.Instances(index)!.Value;
            Assert.Equal(part, record.PartId);
            Assert.False(WorkshopWireFlatBuffers.CarriesTypedField(record));
            Assert.Equal(0, record.ParametersLength);
        }
        // A Basketball record carrying a typed record field, or a declared-parameter material, never loads.
        SaveFixture.Refused(WorkshopSaveRejection.OutOfRange, () => WorkshopSaveCodec.Decode(SaveFixture.Elements,
            PayloadInstanceTests.GenericSave("ball", TypedField.Length)));
        SaveFixture.Refused(WorkshopSaveRejection.OutOfRange, () => WorkshopSaveCodec.Decode(SaveFixture.Elements,
            PayloadInstanceTests.GenericSave("ball", null, [("mass", 4f)])));
    }

    [Fact]
    public void DeclaredDragAndRollingResistanceCompileIntoTheBodyAndMaterialRecordsAndOnlyBallsCarryACoefficient()
    {
        var scene = WorkshopPhysicsCompiler.Compile(TwoLanes(), new(1, 2));
        var bytes = PhysicsGpuAbi.Admission(scene, new(1), new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        var bodies = scene.Bodies.ToArray(); var colliders = scene.Colliders.ToArray(); var materials = scene.Materials.ToArray();
        // The body record's drag lane carries the declared f32 linear drag; the material record's rolling lane the declared f32
        // rolling-resistance coefficient (Basketball 0.035, Bowling 0.03; Domino tiles: zero).
        foreach (var (id, drag, rolling) in new[] { (3UL, .04f, .035f), (4UL, .04f, .03f), (1UL, 0f, 0f), (2UL, 0f, 0f) })
        {
            var slot = Array.FindIndex(bodies, b => b.Id == new GpuBodyId(id));
            Assert.Equal(BitConverter.SingleToUInt32Bits(drag),
                BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(scene.World.BodyOffset(slot) + PhysicsGpuAbi.BodyDragOffset)));
            var owned = colliders.Where(c => c.Body == new GpuBodyId(id)).ToArray();
            Assert.NotEmpty(owned);
            foreach (var collider in owned)
            {
                var material = Array.FindIndex(materials, m => m.Id == collider.Material);
                Assert.Equal(rolling, BinaryPrimitives.ReadSingleLittleEndian(bytes.AsSpan(
                    scene.World.MaterialOffset(material) + PhysicsGpuAbi.MaterialRollingResistanceOffset)));
            }
        }
        // The bench plane (the first material record) declares zero.
        Assert.Equal(0u, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(scene.World.MaterialOffset(0) + PhysicsGpuAbi.MaterialRollingResistanceOffset)));
        // The coefficient is a bounded, finite declaration in [0, 0.1].
        var declared = materials[0];
        foreach (var admitted in new[] { 0f, .1f }) (declared with { RollingResistance = new(admitted) }).Validate();
        foreach (var rejected in new[] { (-.01f), .11f, float.NaN, float.PositiveInfinity })
            Assert.Throws<ArgumentException>(() => (declared with { RollingResistance = new(rejected) }).Validate());
    }

    [Fact]
    public void CapacityCountsEveryBallKindAndTheReceiverGuidesBothBalls()
    {
        var items = new List<IWorkshopInstance>();
        for (var i = 0; i < 8; i++) items.Add(WorkshopInput.Generic(PartId.Basketball,new((ulong)(i + 1)), -6 + i * .8, 3, -2, 0, 0, 0, 1));
        for (var i = 0; i < 8; i++) items.Add(Bowling((ulong)(i + 9), -6 + i * .8, 3, 2));
        new WorkshopInstances([.. items]).Validate();
        // Story 9R.5b: the retired 16-row moving-body table no longer refuses; a seventeenth ball of either kind builds and
        // compiles as one more moving body.
        foreach (var seventeenth in new IWorkshopInstance[] { Bowling(17, 7), WorkshopInput.Generic(PartId.Basketball,new(17), 7, 3, 0, 0, 0, 0, 1) })
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

        // One Receiver declares policies selecting both payload kinds, with the shared capture settings.
        var receiver = WorkshopInput.Generic(PartId.Receiver, new(5), 0, 1, 0, 0, 0, 0, 1);
        var scene = WorkshopPhysicsCompiler.Compile(TwoLanes().WithInstance(receiver), new(1, 2));
        Assert.Equal(BodyTargetKind.AllPayloads, Assert.Single(scene.Sensors.ToArray()).Targets.Kind);
        Assert.Equal(BodyTargetKind.AllPayloads, Assert.Single(scene.Guides.ToArray()).Targets.Kind);
        Assert.All(scene.Sensors.ToArray(), s => Assert.Equal(new LinearSpeed(1.5f), s.SpeedLimit));
        var second = WorkshopInput.Generic(PartId.Receiver, new(6), 4, 1, 0, 0, 0, 0, 1);
        var multiple = WorkshopPhysicsCompiler.Compile(TwoLanes().WithInstance(receiver).WithInstance(second), new(1, 2));
        Assert.Equal(2, multiple.Sensors.Length);
        Assert.Equal(2, multiple.Guides.Length);
    }

    [Fact]
    public void AuthoredPuzzlesRequireTheBasketballKindAndNeverOfferTheBowlingBall()
    {
        var first = PuzzleCatalogue.Create(new("first_principles"), new(1), WorkshopCadenceSettings.Default(), new(100001), new(.5f));
        var delayed = PuzzleCatalogue.Create(new("delayed_signal"), new(1), WorkshopCadenceSettings.Default(), new(100001), new(.5f));
        var dominoes = PuzzleCatalogue.Create(new("domino_effect"), new(1), WorkshopCadenceSettings.Default(), new(100001), new(.5f));
        foreach (var level in new[] { first, delayed, dominoes })
        {
            var ball = Payloads.First(level);
            Assert.Equal(PartId.Basketball, ball.PartId);
            // The same identity and pose as a Bowling ball is not the authored fixture.
            var replaced = level.WithInstance(ball with { PartId = PartId.BowlingBall });
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
