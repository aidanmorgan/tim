using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class EnergyStoreContractTests
{

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SupplyStoreRequiresItsPackageOwnerIncludingCarriedBodies(bool carried)
    {
        var fixture = new LawScene(false);
        var root = fixture.StaticBox(new(0f, 3f, 0f), new(.1f));
        var foreign = fixture.StaticBox(new(5f, 3f, 0f), new(.1f));
        var child = fixture.Sphere(new(0f, 4f, 0f), .2f, 1f, new(0, 0, 0, 0),
            System.Numerics.Vector3.Zero, System.Numerics.Vector3.Zero, 0f, 0f);
        var jointFrame = new ConstraintFrame(System.Numerics.Vector3.Zero, System.Numerics.Quaternion.Identity);
        fixture.Prismatic(root, child, jointFrame, jointFrame, -1f, 1f, 0f, 0f, ConnectedCollision.Disabled);
        var original = fixture.Admit().Scene;
        var store = new EnergyStoreDeclaration(new(original.NextIdentity), carried ? child : root, new(45f), new(0f), new(30f));
        ICompiledScene Install(GpuBodyId consumerOwner)
        {
            var physics = new PhysicsSceneDeclaration(original.Document, original.NextIdentity + 2,
                original.Bodies, original.Colliders, original.Materials, [], [],
                prismatics: original.Prismatics,
                supplyNodes: [new(new(original.NextIdentity + 1), consumerOwner, SupplyNodeKind.Consumer, Storages: [store.Id])],
                energyStores: [store]);
            return RuntimeTestServices.Core.CreateScene(new(1), new(1), WorkshopCadenceSettings.Default(), physics,
                RuntimeTestServices.Core.CreateActivation([], []), [], [root, foreign], []);
        }
        var owned = Install(root);
        Assert.Equal(store, Assert.Single(owned.Physics.EnergyStores.ToArray()));
        var command = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(1), default, owned,
            Session: new(101, 206), Cadence: new(1), Projection: new(1));
        var decoded = WorkshopWireFlatBuffers.DecodeCommand(WorkshopWireFlatBuffers.EncodeCommand(command)).Scene!;
        Assert.Equal(root, Assert.Single(decoded.Physics.SupplyGraph.Nodes.ToArray()).Owner);
        Assert.Equal(carried ? child : root, Assert.Single(decoded.Physics.EnergyStores.ToArray()).Owner);
        Assert.Throws<ArgumentException>(() => Install(foreign));
    }


    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void DischargeStoreRequiresTheSamePackageButMayBelongToAChild(bool contact, bool carried)
    {
        var fixture = new LawScene(false);
        var root = fixture.StaticBox(new(0f, 3f, 0f), new(.1f));
        var foreign = fixture.StaticBox(new(5f, 3f, 0f), new(.1f));
        var child = fixture.Sphere(new(0f, 4f, 0f), .2f, 1f, new(0, 0, 0, 0),
            System.Numerics.Vector3.Zero, System.Numerics.Vector3.Zero, 0f, 0f);
        var jointFrame = new ConstraintFrame(System.Numerics.Vector3.Zero, System.Numerics.Quaternion.Identity);
        fixture.Prismatic(root, child, jointFrame, jointFrame, -1f, 1f, 0f, 0f, ConnectedCollision.Disabled);
        var original = fixture.Admit().Scene;
        var storeId = new EnergyStoreId(original.NextIdentity);
        var region = new ChamberRegionDeclaration(new(original.NextIdentity + 1), root,
            original.Colliders.ToArray().First(collider => collider.Body == root).Id,
            new(BodyTargetKind.AllDynamic, default), new(new(0f, 0f, 0f), new(0f, 0f, 0f, 1f)),
            .48f, .55f, .6f, .84f, .001f, 1f, SensorParticipation.Enabled);
        ICompiledScene Install(GpuBodyId reservoirOwner)
        {
            var physics = new PhysicsSceneDeclaration(original.Document, original.NextIdentity + 4,
                original.Bodies, original.Colliders, original.Materials, [], [],
                contactWorks: contact ? [new(new(original.NextIdentity + 3), root, new(BodyTargetKind.AllDynamic, default),
                    new(12f), storeId, new(.1f), 72)] : [],
                prismatics: original.Prismatics,
                energyStores: [new(storeId, reservoirOwner, new(45f), new(0f), new(30f))],
                chamberRegions: [region],
                finiteWorkActuators: contact ? [] : [new(new(original.NextIdentity + 2), storeId, region.Id, new(12f), .02f)]);
            return RuntimeTestServices.Core.CreateScene(new(1), new(1), WorkshopCadenceSettings.Default(), physics,
                RuntimeTestServices.Core.CreateActivation([], []), [], [root, foreign], []);
        }
        var owned = Install(carried ? child : root);
        var command = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(1), default, owned,
            Session: new(101, 206), Cadence: new(1), Projection: new(1));
        var decoded = WorkshopWireFlatBuffers.DecodeCommand(WorkshopWireFlatBuffers.EncodeCommand(command)).Scene!;
        Assert.Equal(carried ? child : root, Assert.Single(decoded.Physics.EnergyStores.ToArray()).Owner);
        if (contact) Assert.Equal(storeId, Assert.Single(decoded.Physics.ContactWorks.ToArray()).Store);
        else Assert.Equal(storeId, Assert.Single(decoded.Physics.FiniteWorkActuators.ToArray()).Store);
        Assert.Throws<ArgumentException>(() => Install(foreign));
    }


    [Theory]
    [InlineData(0u)]
    [InlineData(4u)]
    [InlineData(258u)]
    [InlineData(uint.MaxValue)]
    public void ContactTargetPolicyRejectsUndefinedAndWrappedEncodedValues(uint encoded)
    {
        var fixture = new LawScene(false);
        var owner = fixture.StaticBox(new(0f, 3f, 0f), new(.1f));
        var work = fixture.ContactWork(owner, new(BodyTargetKind.AllDynamic, default), 12f, 45f, .1f, 72);
        var world = fixture.Admit();
        Assert.Equal(work, PhysicsGpuAbi.ReadContactWorks(world.Admitted, PhysicsInteractionSnapshot.Empty)[0].Id);
        var offset = world.Scene.Layout.GetContactWorkOffset(work) + PhysicsGpuAbi.ContactWorkTargetKindOffset;
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(world.Admitted.AsSpan(offset), encoded);
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadContactWorks(world.Admitted, PhysicsInteractionSnapshot.Empty));
    }

    private static EnergyExpression Energy(float value) => EnergyExpression.Direct(MechanicalValue.Fixed(value));
    private static EnergyStoreDescriptor Store(uint id, float capacity, float initial, float power) =>
        new(new(id), new(0), Energy(capacity), Energy(initial), MechanicalValue.Fixed(power));

    private static BundleEntry Entry(params EnergyStoreDescriptor[] stores)
    {
        var wall = TestBundle.Entry(new("wall"));
        return wall with
        {
            Metadata = wall.Metadata with { Id = "reservoir_fixture" },
            Mechanical = wall.Mechanical! with { EnergyStores = stores }
        };
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IndependentEmptyAndPreloadedStoresKeepTheirIdentitiesThroughCompilationAndAdmission(bool reverse)
    {
        EnergyStoreDescriptor[] stores = [Store(9, 45f, 0f, 30f), Store(3, 100f, 9f, 3f)];
        if (reverse) Array.Reverse(stores);
        var entry = Entry(stores);
        Assert.Empty(BundleValidator.Validate([new("reservoir_fixture", entry)]));
        using var scope = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(new PartId(entry.Metadata.Id), entry.Ports)]));
        var instance = WorkshopInput.Generic(RuntimeTestServices.Bundle, new(entry.Metadata.Id), new(11),
            0f, 3f, 0f, 0f, 0f, 0f, 1f, WallFixture.Default);
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new([instance]));
        var scene = CompiledSceneFixture.Compile(construction);
        var identities = construction.Instances.Identities[instance.Id].EnergyStores;
        var declared = scene.Physics.EnergyStores.ToArray();
        Assert.Equal(2, declared.Length);
        Assert.Equal(new EnergyStoreDeclaration(identities[new(9)], instance.Id, new(45f), new(0f), new(30f)),
            Assert.Single(declared, store => store.Id == identities[new(9)]));
        Assert.Equal(new EnergyStoreDeclaration(identities[new(3)], instance.Id, new(100f), new(9f), new(3f)),
            Assert.Single(declared, store => store.Id == identities[new(3)]));
        var world = PhysicsGpuAbi.Admission(scene.Physics, new(1),
            new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        var reads = PhysicsGpuAbi.ReadEnergyStores(world);
        Assert.Equal(new EnergyStoreRead(identities[new(9)], instance.Id, new(0f), default, default, default),
            Assert.Single(reads, store => store.Id == identities[new(9)]));
        Assert.Equal(new EnergyStoreRead(identities[new(3)], instance.Id, new(9f), default, default, default),
            Assert.Single(reads, store => store.Id == identities[new(3)]));
    }

    [Fact]
    public async Task IndependentStoreObservationsSurvivePhysicalTicksAndReset()
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var entry = Entry(Store(9, 45f, 0f, 30f), Store(3, 100f, 9f, 3f));
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(new PartId(entry.Metadata.Id), entry.Ports)]));
        var instance = WorkshopInput.Generic(RuntimeTestServices.Bundle, new(entry.Metadata.Id), new(11),
            0f, 3f, 0f, 0f, 0f, 0f, 1f, WallFixture.Default);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new([instance]));
        await harness.Require(WorkshopCommandKind.Construct, construction);
        await harness.Run();
        await harness.Advance(4);
        AssertStores(harness.Committed);
        await harness.Require(WorkshopCommandKind.Reset);
        await harness.Run();
        AssertStores(harness.Committed);

        static void AssertStores(WorkshopRead read)
        {
            Assert.Equal(2, read.EnergyStores.Count);
            var energies = Enumerable.Range(0, read.EnergyStores.Count).Select(i => read.EnergyStores[i].Energy.Value).Order().ToArray();
            Assert.Equal([0f, 9f], energies);
            for (var i = 0; i < read.EnergyStores.Count; i++)
            {
                Assert.Equal(0f, read.EnergyStores[i].Supplied.Value);
                Assert.Equal(0f, read.EnergyStores[i].Released.Value);
                Assert.Equal(0f, read.EnergyStores[i].Dissipated.Value);
            }
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ReorderedStoresKeepIndependentChargeLimitsAndCapacity(bool reverse, bool wired)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var source = TestBundle.Entry(new("bumper"));
        EnergyStoreDescriptor[] stores = [Store(9, 45f, 0f, 30f), Store(3, 100f, 9f, 3f)];
        if (reverse) Array.Reverse(stores);
        var entry = source with
        {
            Metadata = source.Metadata with { Id = "dual_reservoir_fixture" },
            Mechanical = source.Mechanical! with { EnergyStores = stores, ContactWorks = [] },
            Presentation = source.Presentation with { Bindings = [] },
            Events = [],
            Electrical = source.Electrical! with { SupplyGraph = new(
                [new SupplyConsumerDescriptor(new("consumer"), [new SupplyEnergyStoreTarget(new(3)), new SupplyEnergyStoreTarget(new(9))])],
                [], source.Electrical!.SupplyGraph!.Ports) }
        };
        Assert.Empty(BundleValidator.Validate([new("dual", entry)]));
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(new PartId(entry.Metadata.Id), entry.Ports)]));
        var reservoir = WorkshopInput.Generic(RuntimeTestServices.Bundle, new(entry.Metadata.Id), new(11), 0, 3, 0, 0, 0, 0, 1);
        var battery = PlacedParts.Battery(new(12), 3, 1, 0, powerLimit: 120f);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new([reservoir, battery]),
            Connections: wired ? new(new WorkshopConnection(battery.Id, PortId.Supply, reservoir.Id, PortId.PowerIn, WorkshopConnectionDomain.Electrical)) : WorkshopConnections.Empty);
        await harness.Require(WorkshopCommandKind.Construct, construction);
        await harness.Run();
        await harness.Advance(240);
        var identities = construction.Instances.Identities[reservoir.Id].EnergyStores;
        var reads = Enumerable.Range(0, harness.Committed.EnergyStores.Count).Select(i => harness.Committed.EnergyStores[i]).ToArray();
        var fast = Assert.Single(reads, value => value.Id == identities[new(9)]);
        var slow = Assert.Single(reads, value => value.Id == identities[new(3)]);
        // Two seconds gives min(45,30*2)=45 J and9+3*2=15 J. 0.01 J excludes swapped
        // demand/source identities and overcharge while allowing accumulated f32 arithmetic.
        Assert.InRange(fast.Energy.Value, wired ? 44.99f : 0f, wired ? 45f : 0f);
        Assert.InRange(slow.Energy.Value, wired ? 14.99f : 9f, wired ? 15.01f : 9f);
        Assert.InRange(fast.Supplied.Value, wired ? 44.99f : 0f, wired ? 45.01f : 0f);
        Assert.InRange(slow.Supplied.Value, wired ? 5.99f : 0f, wired ? 6.01f : 0f);
        Assert.Equal(0f, fast.Released.Value);
        Assert.Equal(0f, slow.Released.Value);
        await harness.Reset();
        var reset = Enumerable.Range(0, harness.Committed.EnergyStores.Count).Select(i => harness.Committed.EnergyStores[i]).ToArray();
        Assert.Equal(0f, Assert.Single(reset, value => value.Id == identities[new(9)]).Energy.Value);
        Assert.Equal(9f, Assert.Single(reset, value => value.Id == identities[new(3)]).Energy.Value);
        Assert.All(reset, value => Assert.Equal(0f, value.Supplied.Value));
    }

    [Theory]
    [InlineData(400f)]
    [InlineData(0f)]
    public async Task ContactDischargeUsesItsReferencedReservoirBeyondTheOldProductCapacity(float initial)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var source = TestBundle.Entry(new("bumper"));
        var entry = source with
        {
            Metadata = source.Metadata with { Id = "independent_discharge_fixture" },
            Mechanical = source.Mechanical! with
            {
                EnergyStores = [Store(9, 400f, initial, 0f)],
                ContactWorks = [source.Mechanical!.ContactWorks[0] with
                    { Store = new(9), TargetSpeed = MechanicalValue.Fixed(32f) }]
            },
            Electrical = source.Electrical! with { SupplyGraph = new(
                [new SupplyConsumerDescriptor(new("consumer"), [new SupplyEnergyStoreTarget(new(9))])], [], source.Electrical!.SupplyGraph!.Ports) }
        };
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(new PartId(entry.Metadata.Id), entry.Ports)]));
        var support = WorkshopInput.Generic(RuntimeTestServices.Bundle, new(entry.Metadata.Id), new(11), 0, 3, 0, 0, 0, 0, 1);
        var load = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("bowling"), new(12), 0, 6, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new([support, load]));
        await harness.Require(WorkshopCommandKind.Construct, construction);
        await harness.Run();
        for (var tick = 0; tick < 1200 && harness.Committed.ContactWorks[0].OccurrenceCount == 0; tick++)
            await harness.Advance(1);
        Assert.Equal(1u, harness.Committed.ContactWorks[0].OccurrenceCount);
        var storeId = construction.Instances.Identities[support.Id].EnergyStores[new(9)];
        Assert.Equal(storeId, harness.Committed.ContactWorks[0].Store);
        Assert.Equal(storeId, harness.Committed.EnergyStores[0].Id);
        // Raising a 7.2 kg payload to 32 m/s requires far more than400 J; the paid case exhausts
        // its declared budget. The empty control qualifies the same contact without supplying work.
        Assert.Equal(0f, harness.Committed.EnergyStores[0].Energy.Value);
        Assert.Equal(initial, harness.Committed.EnergyStores[0].Released.Value);
        var occurrence = Assert.Single(Enumerable.Range(0, harness.Ledgers[^1].OccurrenceCount)
            .Select(harness.Ledgers[^1].Occurrence));
        Assert.Equal(initial, occurrence.Debit.Value);
        Assert.Equal(initial == 0f ? ContactWorkEffect.Passive : ContactWorkEffect.Paid, occurrence.Effect);
    }

    [Theory]
    [InlineData(0f, 0f, 0f)]
    [InlineData(45f, 0f, 30f)]
    [InlineData(45f, 45f, 30f)]
    public void FiniteStoreAdmitsEmptyPartialCapacityAndFullInitialEnergy(float capacity, float initial, float power)
    {
        new EnergyStoreDeclaration(new(2), new(1), new(capacity), new(initial), new(power)).Validate();
        Assert.Empty(BundleValidator.Validate([new("store", Entry(Store(1, capacity, initial, power)))]));
    }

    [Theory]
    [InlineData(-1f, 0f, 1f)]
    [InlineData(1f, -1f, 1f)]
    [InlineData(1f, 2f, 1f)]
    [InlineData(1f, 0f, -1f)]
    [InlineData(float.NaN, 0f, 1f)]
    [InlineData(1f, float.PositiveInfinity, 1f)]
    [InlineData(1f, 0f, float.NaN)]
    public void InvalidStoreAmountsAreRefusedAtBothDeclarationBoundaries(float capacity, float initial, float power)
    {
        Assert.Throws<ArgumentException>(() =>
            new EnergyStoreDeclaration(new(2), new(1), new(capacity), new(initial), new(power)).Validate());
        Assert.Contains(BundleValidator.Validate([new("store", Entry(Store(1, capacity, initial, power)))]),
            error => error.Code == BundleErrorCode.OutOfRange && error.Detail.EndsWith(".Amounts", StringComparison.Ordinal));
    }

    [Fact]
    public void DuplicateStoresAndForeignOwnersAreRefused()
    {
        var duplicate = Store(1, 45f, 0f, 30f);
        Assert.Contains(BundleValidator.Validate([new("store", Entry(duplicate, duplicate))]),
            error => error.Code == BundleErrorCode.DuplicateId);
        Assert.Contains(BundleValidator.Validate([new("store", Entry(duplicate with { Owner = new(1) }))]),
            error => error.Code == BundleErrorCode.UnresolvedReference);
    }

    [Fact]
    public void KineticEnergyIsAnExplicitExpressionWithDeclaredMassAndSpeed()
    {
        var expression = EnergyExpression.Kinetic(MechanicalValue.Fixed(2f), MechanicalValue.FromParameter("speed"));
        // The independent calculation is 1/2 * 2 kg * (3 m/s)^2 = 9 J.
        Assert.Equal(9f, expression.Evaluate(new Dictionary<string, float> { ["speed"] = 3f }));
        Assert.Throws<ArgumentException>(() => expression.Evaluate(new Dictionary<string, float>()));
        Assert.Throws<ArgumentException>(() => (expression with { Kind = (EnergyExpressionKind)99 }).Evaluate(new Dictionary<string, float>()));
    }
}
