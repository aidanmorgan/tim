using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class GasRuntimeTests
{
    [Theory]
    [InlineData(3f, true, false, 300f)]
    [InlineData(3f, true, true, 1000f)]
    [InlineData(100f, false, false, 1000f)]
    public async Task ExactContactOpensFiniteStoreAndResetRestoresPreparedState(float threshold, bool ruptures, bool reverse, float maximumTemperature)
    {
        await using var h = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var original = TestBundle.Entry(new("tennis"));
        var material = new GasMaterial(2000, 3000, 1, maximumTemperature, 1, 200000);
        var entry = original with
        {
            Metadata = original.Metadata with { Id = "compliant_payload", Parameters = [] },
            ColliderBindings = [], MaterialBindings = [],
            Events = [new(SimulationEventKind.IntegrityBroken, Integrity: new(9)),
                new(SimulationEventKind.IntegrityBroken, Integrity: new(12))],
            Presentation = original.Presentation with { Parameters = [], ResizeAxes = PresentationAxes.None, Sizing = null },
            Mechanical = original.Mechanical! with
            {
                MassFrame = new(new(BodyMassSourceKind.CenteredInventory, MassScalar.Constant(2),
                    default, [new(7), new(11)], new(.002f)), MassVector.Constant(Vector3.Zero), Quaternion.Identity,
                    MassScalar.Constant(0), WorldAccelerationDeclaration.Constant(new(0, -9.81f, 0))),
                GasStores = [new(new(7), new(0), material, MassScalar.Constant(.1f), 102325, 300, 1000, .001f),
                    new(new(11), new(0), material, MassScalar.Constant(.05f), 102325, 300, 1000, .002f)],
                GasShapes = [new(new(7), new(0), new(0), .0625f)],
                IntegrityObservers = [new(new(9), new(0), new(0), new(7), threshold, .1f),
                    new(new(12), new(0), new(0), new(11), 100, .1f)],
                AmbientLoads = []
            }
        };
        if (reverse) entry = entry with { Mechanical = entry.Mechanical! with
            { GasStores = entry.Mechanical.GasStores.Reverse().ToArray(), IntegrityObservers = entry.Mechanical.IntegrityObservers.Reverse().ToArray() } };
        Assert.Empty(BundleValidator.Validate([new("compliant", entry)]));
        var stationary = entry with { Metadata = entry.Metadata with { Id = "separate_inventory" },
            Mechanical = entry.Mechanical! with { MassFrame = entry.Mechanical.MassFrame with
                { Gravity = WorldAccelerationDeclaration.Constant(Vector3.Zero) } } };
        using var scope = DeclaredPhysics.Scoped(BundleSchema.Read(BundleSchema.WriteEntries([entry, stationary])).Elements);
        using var ports = DeclaredPorts.Scoped(ElementPortCatalogue.FromBundle(DeclaredPhysics.Entries.Values));
        var instance = WorkshopInput.Generic(RuntimeTestServices.Bundle, new(entry.Metadata.Id), new(1), 0, 3, 0, 0, 0, 0, 1);
        var separate = WorkshopInput.Generic(RuntimeTestServices.Bundle, new(stationary.Metadata.Id), new(2), 3, 3, 0, 0, 0, 0, 1);
        await h.Require(WorkshopCommandKind.Construct, TennisBallTests.Scene(instance, separate));
        Assert.Equal(4, h.Committed.GasStores.Count);
        Assert.Equal(4, h.Committed.IntegrityObservers.Count);
        var separateInitial = h.Committed.GasStores[2];
        var initial = h.Committed.GasStores[0];
        Assert.Equal(GasOutletPhase.Closed, initial.Outlet);
        var other = h.Committed.GasStores[1];
        var goal = new WorkshopGoal(WorkshopGoalKind.IntegrityBroken, default, default, Integrity: new(instance.Id, new(9)));
        var unrelated = goal with { Integrity = new(instance.Id, new(12)) };
        var identities = h.Construction.Instances.Identities;
        await h.Run();
        var epoch = h.Committed.Epoch;
        await h.Advance(360); // Three seconds admits fall, impact and a full pressure-driven discharge.
        var gas = h.Committed.GasStores[0];
        var integrity = h.Committed.IntegrityObservers[0];
        Assert.Equal(ruptures ? IntegrityPhase.Broken : IntegrityPhase.Intact, integrity.Phase);
        Assert.Equal(ruptures ? GasOutletPhase.Open : GasOutletPhase.Closed, gas.Outlet);
        Assert.Equal(other.State, h.Committed.GasStores[1].State);
        Assert.Equal(separateInitial, h.Committed.GasStores[2]);
        Assert.Equal(WorkshopGoalPhase.Waiting, WorkshopGoalEvaluator.Evaluate(
            goal with { Integrity = new(separate.Id, new(9)) }, h.Committed, epoch, identities));
        Assert.Equal(WorkshopGoalPhase.Waiting, WorkshopGoalEvaluator.Evaluate(unrelated, h.Committed, epoch, identities));
        Assert.Equal(ruptures ? WorkshopGoalPhase.Solved : WorkshopGoalPhase.Waiting,
            WorkshopGoalEvaluator.Evaluate(goal, h.Committed, epoch, identities));
        if (ruptures)
        {
            Assert.Equal(GasInventoryPhase.Empty, gas.State.Phase);
            Assert.True(gas.EnthalpyOut > 0);
            Assert.True(integrity.Ordinal > 0);
            var record = Assert.Single(h.Of(SimulationEventKind.IntegrityBroken));
            Assert.Equal(integrity.Id.Value, record.Source.Value);
            Assert.Equal(integrity.Ordinal, record.Ordinal);
            var compiled = WorkshopPhysicsCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, h.Construction, new(1, 2));
            var wake = new WorkshopGoalWake(goal, compiled, identities, epoch);
            var waitingTick = new SimulationTick(record.Tick - 1);
            wake.Observe(new(epoch.Value, 0, [record with { Source = new(h.Committed.IntegrityObservers[1].Id.Value) }]), waitingTick);
            Assert.False(wake.Due(h.Committed.Tick));
            wake.Observe(new(epoch.Value + 1, 0, [record]), waitingTick);
            Assert.False(wake.Due(h.Committed.Tick));
            wake.Observe(new(epoch.Value, 0, [record]), waitingTick);
            Assert.False(wake.Due(waitingTick));
            Assert.True(wake.Due(h.Committed.Tick));
            Assert.Equal(new ActivationTime(record.Ordinal, record.Phase),
                WorkshopGoalEvaluator.Occurrence(goal, h.Committed, epoch, identities));
            wake.Evaluated(h.Committed.Tick, solved: true);
            Assert.False(wake.Due(h.Committed.Tick));
            var occurrence = integrity;
            await h.Advance(120);
            Assert.Equal(occurrence, h.Committed.IntegrityObservers[0]);
            Assert.Single(h.Of(SimulationEventKind.IntegrityBroken));
        }
        else Assert.Equal(initial.State, gas.State);
        var oldRead = h.Committed;
        await h.Reset();
        Assert.Equal(WorkshopGoalPhase.Waiting, WorkshopGoalEvaluator.Evaluate(goal, h.Committed, h.Committed.Epoch, identities));
        Assert.Equal(WorkshopGoalPhase.Waiting, WorkshopGoalEvaluator.Evaluate(goal, oldRead, h.Committed.Epoch, identities));
        Assert.Equal(initial.State, h.Committed.GasStores[0].State);
        Assert.Equal(IntegrityPhase.Intact, h.Committed.IntegrityObservers[0].Phase);
    }

    [Theory]
    [InlineData(false, false, 0f, 7f, true)]
    [InlineData(false, false, .5f, 13f, false)]
    [InlineData(true, false, 0f, 3f, true)]
    [InlineData(true, true, 0f, 5f, false)]
    [InlineData(true, false, .5f, 5f, true)]
    [InlineData(true, true, .5f, 7f, false)]
    public void AcceptedPairImpulseUsesBothDynamicOrientationsAndRestitution(bool dynamicTarget,
        bool observeTarget, float restitution, float threshold, bool ruptures)
    {
        var declared = new LawScene(bench: false);
        var contact = new LawMaterial(restitution, .1f, 0, 0);
        var moving = declared.Sphere(new(-1, 3, 0), .25f, 2, contact, new(4, 0, 0), default, 0, 0);
        var targetMaterial = new LawMaterial(1, .1f, 0, 0);
        var target = dynamicTarget
            ? declared.Sphere(new(0, 3, 0), .25f, 2, targetMaterial, default, default, 0, 0)
            : declared.StaticSphere(new(0, 3, 0), .25f, Quaternion.Identity, targetMaterial);
        var source = declared.Admit().Scene;
        var owner = observeTarget ? target : moving;
        var collider = source.Colliders.ToArray().Single(c => c.Body == owner).Id;
        var material = new GasMaterial(2000, 3000, 1, 1000, 1, 200000);
        var charge = IsobaricGas.Charge(material, .01f, 102325, 300);
        var store = new GasStoreDeclaration(new(source.NextIdentity), owner, material, charge, 101325, 1000, 0);
        var observer = new ContactIntegrityDeclaration(new(source.NextIdentity + 1), collider, store.Id, threshold, .1f);
        var scene = new PhysicsSceneDeclaration(source.Document, source.NextIdentity + 2, source.Bodies.ToArray(),
            source.Colliders.ToArray(), source.Materials.ToArray(), [], [], gasStores: [store],
            inventoryMasses: [new(owner, 2 - charge.Mass)], integrityObservers: [observer]);
        var world = new LawWorld(scene, PhysicsGpuAbi.Admission(scene, new(1),
            new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1))), SimulationCadence.Hz120);
        var state = world.Step(60);
        // J=(1+e)*reducedMass*4: dynamic/dynamic gives 4 or 6 Ns; a static target gives 8 or 12 Ns.
        // Thresholds leave a full 1 Ns clearance, distinguishing restitution and reduced mass without solver goldens.
        Assert.Equal(ruptures ? IntegrityPhase.Broken : IntegrityPhase.Intact,
            Assert.Single(PhysicsGpuAbi.ReadIntegrity(state.World)).Phase);
        Assert.Equal(ruptures ? GasOutletPhase.Open : GasOutletPhase.Closed,
            Assert.Single(PhysicsGpuAbi.ReadGas(state.World)).Outlet);
        // Zero conductance preserves the admitted prepared state, not the pre-equilibration reference charge.
        Assert.Equal(store.Prepared.State, Assert.Single(PhysicsGpuAbi.ReadGas(state.World)).State);
    }

    [Fact]
    public void FiniteOutflowChangesOneMassAndShapeWithoutThrustOrChangingDrySpinInertia()
    {
        var declared = new LawScene(bench: false);
        var contact = new LawMaterial(1, .1f, 0, 0);
        var owner = declared.Sphere(new(-2, 3, 0), .5f, 2, contact, new(4, 0, 0), new(0, 2, 0), 0, 0);
        declared.StaticSphere(new(0, 3, 0), .5f, Quaternion.Identity, contact);
        var source = declared.Admit().Scene;
        var material = new GasMaterial(2000, 3000, 1, 1000, 1, 200000);
        const float dryRadius = .0625f;
        var charge = IsobaricGas.Charge(material, 4 * MathF.PI / 3 * (.5f * .5f * .5f - dryRadius * dryRadius * dryRadius), 102325, 300);
        var store = new GasStoreDeclaration(new(source.NextIdentity), owner, material, charge, 101325, 1000, charge.Mass / 1000);
        var colliders = source.Colliders.ToArray();
        var colliderIndex = Array.FindIndex(colliders, row => row.Body == owner);
        colliders[colliderIndex] = colliders[colliderIndex] with { Radius = new(IsobaricGas.OuterRadius(dryRadius, store.Prepared.State.Volume)) };
        var collider = colliders[colliderIndex].Id;
        var scene = new PhysicsSceneDeclaration(source.Document, source.NextIdentity + 2, source.Bodies.ToArray(),
            colliders, source.Materials.ToArray(), [], [], gasStores: [store],
            gasShapes: [new(store.Id, collider, dryRadius)], inventoryMasses: [new(owner, 2 - charge.Mass)],
            integrityObservers: [new(new(source.NextIdentity + 1), collider, store.Id, 3, .1f)]);
        var admitted = PhysicsGpuAbi.Admission(scene, new(1),
            new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        var firstBrokenTick = -1;
        var observedPartial = false;
        var state = new LawWorld(scene, admitted, SimulationCadence.Hz120).Step(240, (tick, current) =>
        {
            if (PhysicsGpuAbi.ReadIntegrity(current.World)[0].Phase != IntegrityPhase.Broken) return;
            if (firstBrokenTick < 0) firstBrokenTick = tick;
            if (tick != firstBrokenTick + 30) return;
            observedPartial = true;
            var partial = PhysicsGpuAbi.ReadGas(current.World)[0].State;
            Assert.InRange(partial.Mass / charge.Mass, .70f, .80f);
            var radius = System.Buffers.Binary.BinaryPrimitives.ReadSingleLittleEndian(
                current.World.AsSpan(scene.Layout.GetColliderOffset(collider) + PhysicsGpuAbi.ColliderRadiusOffset));
            // A centimetre excludes a retained inflated collision sphere during the visible quarter-second collapse.
            var expected = MathF.Cbrt(dryRadius * dryRadius * dryRadius + partial.Volume * 3 / (4 * MathF.PI));
            Assert.InRange(radius, expected - .01f, expected + .01f);
        });
        Assert.True(observedPartial);
        var gas = Assert.Single(PhysicsGpuAbi.ReadGas(state.World));
        Assert.Equal(GasInventoryPhase.Empty, gas.State.Phase);
        float Lane(byte[] bytes, int offset) => System.Buffers.Binary.BinaryPrimitives.ReadSingleLittleEndian(bytes.AsSpan(offset));
        var bodyOffset = scene.Layout.GetBodyOffset(owner);
        Assert.Equal(2 - charge.Mass, Lane(state.World, bodyOffset + PhysicsGpuAbi.BodyMassOffset));
        Assert.Equal(dryRadius, Lane(state.World, scene.Layout.GetColliderOffset(collider) + PhysicsGpuAbi.ColliderRadiusOffset));
        for (var axis = 0; axis < 3; axis++)
            Assert.Equal(Lane(admitted, bodyOffset + PhysicsGpuAbi.BodyPrincipalInertiaOffset + axis * sizeof(float)),
                Lane(state.World, bodyOffset + PhysicsGpuAbi.BodyPrincipalInertiaOffset + axis * sizeof(float)));
        // A 0.01 m/s margin rejects momentum-conserving recoil from retaining expelled mass, while allowing contact arithmetic.
        Assert.InRange(state[owner].Velocity.X, -4.01f, -3.99f);
        Assert.InRange(state[owner].Angular.Y, 1.99f, 2.01f);
        // Local ledger conservation across finite allocations, 0.02% for accumulated f32 additions.
        void Near(float expected, float actual) => Assert.InRange(MathF.Abs(actual - expected), 0, MathF.Abs(expected) * .0002f);
        Near(-4 * charge.Mass, gas.MomentumOut.X);
        Near(8 * charge.Mass, gas.KineticOut);
        Near(store.Prepared.State.Energy + gas.BoundaryWork + gas.ElasticReleased, gas.EnthalpyOut);
    }

    private static BundleEntry MultiBodyEnvelope(bool reverse)
    {
        var template = TestBundle.Entry(new("impact_lever"));
        var material = new GasMaterial(2000, 3000, 1, 1000, 1, 200000);
        var sphere = new ColliderDescriptor(CuriousContraptions.Abstractions.ColliderShapeKind.Sphere,
            new(.3f), new(-1.5f, 0, 0), Quaternion.Identity, new(.1f, 0, 0, .1f), 0);
        RevoluteHingeDescriptor Child(int store, float z) => template.Mechanical!.Hinges[0] with
        {
            BaseFrame = new(new(0, 0, z), Quaternion.Identity),
            InitialAngle = MechanicalValue.Fixed(0), FrictionTorque = MechanicalValue.Fixed(0),
            MovingBody = new(new(new(BodyMassSourceKind.CenteredInventory, MassScalar.Constant(2), default,
                [new((uint)store)], new(.2f)), MassVector.Constant(Vector3.Zero), Quaternion.Identity,
                MassScalar.Constant(0), WorldAccelerationDeclaration.Constant(Vector3.Zero)),
                [sphere, sphere with { Offset = new(1.5f, 0, 0) }])
        };
        GasStoreDescriptor[] stores = [
            new(new(7), new(1), material, MassScalar.Constant(.1f), 102325, 300, 1000, .001f),
            new(new(41), new(2), material, MassScalar.Constant(.05f), 102325, 300, 1000, .002f)];
        ContactIntegrityDescriptor[] observers = [
            new(new(9), new(1), new(1), new(7), .1f, .1f),
            new(new(12), new(2), new(0), new(41), .1f, .1f)];
        return template with
        {
            Metadata = template.Metadata with { Id = "offset_envelopes", Parameters = [] },
            Footprint = new(3, 2, 0, 2), Ports = [], ColliderBindings = [], MaterialBindings = [],
            Colliders = [new(CuriousContraptions.Abstractions.ColliderShapeKind.Box, new(.1f),
                new(0, -2, 0), Quaternion.Identity, new(0, 0, 0, .1f), 0)],
            Presentation = new(1, PresentationAxes.None, [],
                [new("First", new(1), Vector3.Zero), new("Second", new(2), Vector3.Zero)], [], []),
            Events = [new(SimulationEventKind.IntegrityBroken, Integrity: new(9)), new(SimulationEventKind.IntegrityBroken, Integrity: new(12))],
            Mechanical = template.Mechanical! with
            {
                Hinges = [Child(7, 0), Child(41, 3)], GasStores = reverse ? stores.Reverse().ToArray() : stores,
                GasShapes = [new(new(7), new(1), new(1), .0625f)],
                IntegrityObservers = reverse ? observers.Reverse().ToArray() : observers
            }
        };
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void NonfirstBodyAndColliderOwnTheExactEnvelopeAndContact(bool reverse, bool selectedContact)
    {
        var entry = MultiBodyEnvelope(reverse);
        Assert.Empty(BundleValidator.Validate([new("unfamiliar", entry)]));
        using var scope = DeclaredPhysics.Scoped(BundleSchema.Read(BundleSchema.WriteEntries([entry])).Elements);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(new PartId(entry.Metadata.Id), entry.Ports)]));
        var instance = WorkshopInput.Generic(RuntimeTestServices.Bundle, new(entry.Metadata.Id), new(1), 0, 3, 0, 0, 0, 0, 1);
        var striker = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("bowling"), new(2),
            selectedContact ? 1.5f : -1.5f, 5, 0, 0, 0, 0, 1);
        var compiled = WorkshopPhysicsCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle,
            TennisBallTests.Scene(instance, striker), new(1, 2));
        var store = compiled.GasStores[0];
        var other = compiled.GasStores[1];
        Assert.NotEqual(instance.Id, store.Body);
        Assert.NotEqual(store.Body, other.Body);
        var shape = Assert.Single(compiled.GasShapes.ToArray());
        var siblings = compiled.Colliders.ToArray().Where(c => c.Body == store.Body).ToArray();
        Assert.Equal(siblings[1].Id, shape.Collider);
        Assert.Equal(shape.Collider, compiled.IntegrityObservers[0].Collider);
        var admitted = PhysicsGpuAbi.Admission(compiled, new(1),
            new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        var state = new LawWorld(compiled, admitted, SimulationCadence.Hz120).Step(300);
        var reads = PhysicsGpuAbi.ReadGas(state.World);
        Assert.Equal(selectedContact ? GasInventoryPhase.Empty : GasInventoryPhase.Charged, reads[0].State.Phase);
        Assert.Equal(other.Prepared.State, reads[1].State);
        Assert.Equal(IntegrityPhase.Intact, PhysicsGpuAbi.ReadIntegrity(state.World)[1].Phase);
        float Radius(GpuColliderId id) => System.Buffers.Binary.BinaryPrimitives.ReadSingleLittleEndian(
            state.World.AsSpan(compiled.Layout.GetColliderOffset(id) + PhysicsGpuAbi.ColliderRadiusOffset));
        Assert.Equal(siblings[0].Radius.Value, Radius(siblings[0].Id));
        Assert.Equal(selectedContact ? .0625f : siblings[1].Radius.Value, Radius(shape.Collider));
        float Mass(GpuBodyId id) => System.Buffers.Binary.BinaryPrimitives.ReadSingleLittleEndian(
            state.World.AsSpan(compiled.Layout.GetBodyOffset(id) + PhysicsGpuAbi.BodyMassOffset));
        Assert.Equal(selectedContact ? 2 - store.Reference.Mass : 2, Mass(store.Body));
        Assert.Equal(2, Mass(other.Body));
        Assert.Equal(0, Mass(instance.Id));
    }

    [Fact]
    public void ForeignGasOwnersAndConflictingShapeOrOutletWritersRefuse()
    {
        var entry = MultiBodyEnvelope(false);
        Assert.Empty(BundleValidator.Validate([new("valid", entry)]));
        var m = entry.Mechanical!;
        foreach (var invalid in new[] {
            m with { GasShapes = [m.GasShapes[0] with { Store = new(41) }] },
            m with { IntegrityObservers = [m.IntegrityObservers[0] with { Store = new(41) }] },
            m with { GasShapes = [m.GasShapes[0], m.GasShapes[0]] },
            m with { IntegrityObservers = [m.IntegrityObservers[0], m.IntegrityObservers[0] with { Id = new(99) }] }
        })
            Assert.Contains(BundleValidator.Validate([new("invalid", entry with { Mechanical = invalid })]),
                error => error.Code == BundleErrorCode.OutOfRange);
        using var scope = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(new PartId(entry.Metadata.Id), entry.Ports)]));
        var compiled = WorkshopPhysicsCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle,
            TennisBallTests.Scene(WorkshopInput.Generic(RuntimeTestServices.Bundle, new(entry.Metadata.Id), new(1),
                0, 3, 0, 0, 0, 0, 1)), new(1, 2));
        PhysicsSceneDeclaration Scene(GasShapeDeclaration[] shapes, ContactIntegrityDeclaration[] observers) =>
            new(compiled.Document, compiled.NextIdentity + 1, compiled.Bodies, compiled.Colliders, compiled.Materials,
                compiled.Sensors, compiled.Guides, revolutes: compiled.Revolutes, gasStores: compiled.GasStores,
                gasShapes: shapes, inventoryMasses: compiled.InventoryMasses, integrityObservers: observers);
        var shapes = compiled.GasShapes.ToArray();
        var observers = compiled.IntegrityObservers.ToArray();
        var valid = Scene(shapes, observers);
        var admitted = PhysicsGpuAbi.Admission(valid, new(1),
            new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        Assert.Throws<ArgumentException>(() => Scene([shapes[0] with { Store = compiled.GasStores[1].Id }], observers));
        Assert.Throws<ArgumentException>(() => Scene(shapes, [observers[0] with { Store = compiled.GasStores[1].Id }]));
        Assert.Throws<ArgumentException>(() => Scene([shapes[0], shapes[0]], observers));
        Assert.Throws<ArgumentException>(() => Scene(shapes,
            [observers[0], observers[0] with { Id = new(compiled.NextIdentity) }]));
        Assert.Equal(valid.GasStores[0].Prepared.State, PhysicsGpuAbi.ReadGas(admitted)[0].State);
    }

    [Fact]
    public void UnrepresentableCompleteGasAccountingCannotReplaceAnAdmittedWorld()
    {
        var law = new LawScene(bench: false);
        var owner = law.Sphere(new(0, 3, 0), .25f, 2, new(0, 0, 0, .1f), default, default, 0, 0);
        var source = law.Admit().Scene;
        var material = new GasMaterial(1, 1, 1, float.MaxValue, 1, 10);
        GasStoreDeclaration Store(float volume, float energy) =>
            new(new(source.NextIdentity), owner, material, new(1, volume, energy), 1, 1, 1);
        PhysicsSceneDeclaration Scene(GasStoreDeclaration store) => new(source.Document, source.NextIdentity + 1,
            source.Bodies, source.Colliders, source.Materials, [], [], gasStores: [store], inventoryMasses: [new(owner, 1)]);
        var valid = Scene(Store(1e37f, 2e37f));
        var admitted = PhysicsGpuAbi.Admission(valid, new(1),
            new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        Assert.Throws<ArgumentException>(() => Scene(Store(1e38f, 2e38f)));
        var after = new LawWorld(valid, admitted, SimulationCadence.Hz120).Step(1);
        Assert.Equal(valid.GasStores[0].Prepared.State, PhysicsGpuAbi.ReadGas(after.World)[0].State);
    }
}
