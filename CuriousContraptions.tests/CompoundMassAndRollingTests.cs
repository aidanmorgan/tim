using System.Numerics;
using Google.FlatBuffers;
using Wire = CuriousContraptions.Wire;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Bundle;
using ColliderShapeKind = CuriousContraptions.Gpu.ColliderShapeKind;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class CompoundMassAndRollingTests
{
    [Theory]
    [InlineData(3, false)]
    [InlineData(3, true)]
    [InlineData(9, false)]
    [InlineData(9, true)]
    public void ManyChildSupportKeepsFiniteDissipativeMotionAndFrictionControl(int width, bool rough)
    {
        var scene = new LawScene();
        var origin = new Vector3(0, LawScene.BenchY + .25f, 0);
        var body = scene.Sphere(origin, .05f, 1, new(0, .1f, rough ? .6f : 0, rough ? .1f : 0),
            new(.4f, 0, 0), default, 0, LawScene.Gravity);
        // Every disjoint .05 m foot is tangent to the same plane: nine or eighty-one
        // simultaneous supports, with one material pair and no per-child mass contribution.
        for (var x = 0; x < width; x++)
            for (var z = 0; z < width; z++)
                scene.ChildCollider(body, ColliderShapeKind.Sphere,
                    new((x - width / 2) * .12f, -.2f, (z - width / 2) * .12f), .05f);
        scene.DeclareMassFrame(body, Vector3.One);
        var final = scene.Admit().Step(60, (_, state) =>
        {
            var current = state[body];
            Assert.InRange(current.Position.Y, origin.Y - .0005f, origin.Y + .0005f);
            var energy = .5f * (current.Velocity.LengthSquared() + current.Angular.LengthSquared())
                - LawScene.Gravity * (current.Position.Y - origin.Y);
            Assert.True(float.IsFinite(energy) && energy <= .08f + 9.81f * .0002f, $"energy {energy}");
        })[body];
        if (rough) Assert.True(final.Velocity.X < .39f, $"friction must reduce actual motion: {final.Velocity}");
        else Assert.InRange(MathF.Abs(final.Velocity.X - .4f), 0, 1e-6f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RotatingOffsetChildSweepsFromDeclaredCentreIntoAnObstacle(bool obstacle)
    {
        var scene = new LawScene(bench: false);
        var centre = new Vector3(.4f, .2f, 0);
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, .3f);
        var origin = new Vector3(0, 4, 0);
        var child = centre + new Vector3(2, 0, 0);
        var body = scene.Sphere(origin, 1f / 32, 1, new(0, .1f, 0, 0), default, new(0, 0, 60), 0, 0,
            colliderOffset: centre, rotation: rotation);
        scene.ChildCollider(body, ColliderShapeKind.Sphere, child, 1f / 32,
            rotation: Quaternion.CreateFromAxisAngle(Vector3.UnitY, .4f));
        scene.DeclareMassFrame(body, Vector3.One, centre);
        // The child travels a 2m arc; at theta=.5rad it meets this thin box.
        // Both initial gap and within-substep arc exceed the bare radius spin bound.
        var com = origin + Vector3.Transform(centre, rotation);
        var hit = com + new Vector3(2 * MathF.Cos(.5f), 2 * MathF.Sin(.5f), obstacle ? 0 : .3f);
        scene.StaticBox(hit, new(.06f, .06f, .06f), material: new(0, .1f, 0, 0));
        var final = scene.Admit(SimulationCadence.Hz60).Step(1)[body];
        if (obstacle)
        {
            Assert.True(final.Angular.Z < 50, $"offset contact must slow rotation: {final.Angular.Z}");
            Assert.True(final.Velocity.Length() > .1f, "contact transfers linear impulse to the owner");
        }
        else
        {
            Assert.InRange(final.Angular.Z, 59.9f, 60.1f);
            Assert.InRange(final.Velocity.Length(), 0, .001f);
        }
    }

    public enum Children { Bare, Forward, Reverse }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void DifferentChildMaterialsKeepTheirOwnFrictionAndTorque(int roughSide)
    {
        var scene = new LawScene();
        var body = scene.Sphere(new(0, LawScene.BenchY + .3f, 0), .1f, 1,
            new(0, .1f, 0, 0), new(0, 0, 1), default, 0, LawScene.Gravity, new(0, 2, 0));
        foreach (var side in new[] { -1, 1 })
            scene.ChildCollider(body, ColliderShapeKind.Sphere, new(side * .5f, -.2f, 0), .1f,
                material: new LawMaterial(0, .1f, side == roughSide ? .6f : 0f, 0));
        scene.DeclareMassFrame(body, Vector3.One);
        var state = scene.Admit().Step(12)[body];
        if (roughSide == 0)
        {
            Assert.InRange(MathF.Abs(state.Velocity.Z - 1), 0, 1e-6f);
            Assert.InRange(MathF.Abs(state.Angular.Y), 0, 1e-6f);
        }
        else
        {
            // Friction on the +X foot is -Z, hence positive yaw torque;
            // reversing which literal material is rough reverses that torque.
            Assert.True(roughSide * state.Angular.Y > .01f);
            Assert.True(state.Velocity.Z < .99f);
        }
        Assert.InRange(.5 * (state.Velocity.LengthSquared() + state.Angular.LengthSquared())
            - LawScene.Gravity * (state.Position.Y - (LawScene.BenchY + .3f)), 0, .5 + 9.81 * .0002);
    }


    [Theory]
    [InlineData(ColliderShapeKind.Tube)]
    [InlineData(ColliderShapeKind.Frustum)]
    [InlineData(ColliderShapeKind.Torus)]
    public void HollowChildLocalPoseAndDeclaredCentreGiveRealContactAndEmptySpace(ColliderShapeKind shape)
    {
        LawBody Observe(float gap)
        {
            var bodyTurn = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 37f * MathF.PI / 180);
            var childTurn = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 4);
            // At 45 degrees the lower cap's .4 radius and .25 half-height
            // reach (.4+.25)/sqrt(2). A torus reaches R/sqrt(2)+r.
            // Body yaw leaves both vertical supports unchanged.
            var reach = shape == ColliderShapeKind.Torus ? .4f / MathF.Sqrt(2) + .08f : .65f / MathF.Sqrt(2);
            var scene = new LawScene();
            var body = scene.Sphere(new(0, LawScene.BenchY + reach + .2f + gap, 0), .1f, 1,
                new(0, .1f, 0, 0), new(0, -.1f, 0), default, 0, 0, new(0, 2, 0), bodyTurn);
            var lanes = shape switch
            {
                ColliderShapeKind.Tube => new Vector3(.3f, .25f, 0),
                ColliderShapeKind.Frustum => new Vector3(.05f, .25f, .3f),
                _ => new Vector3(0, .08f, MathF.Tau)
            };
            scene.ChildCollider(body, shape, new(.15f, -.2f, 0), .4f, lanes, childTurn);
            scene.DeclareMassFrame(body, Vector3.One, new(.1f, .05f, -.03f),
                Quaternion.CreateFromAxisAngle(Vector3.UnitX, .3f));
            return scene.Admit(SimulationCadence.Hz240).Step(1)[body];
        }
        var touching = Observe(-.0002f);
        var empty = Observe(.01f);
        Assert.True(touching.Velocity.Y > -.04f, $"{shape}: {touching.Velocity}");
        Assert.InRange(.5 * (touching.Velocity.LengthSquared() + touching.Angular.LengthSquared()), 0, .00501);
        Assert.InRange(MathF.Abs(empty.Velocity.Y + .1f), 0, 1e-6f);
        Assert.InRange(empty.Spin, 0, 1e-6);
    }


    [Fact]
    public void InvalidDeclaredMassFramesAreRefusedBeforeAdmission()
    {
        var invalid = new[]
        {
            (Vector3.Zero, Vector3.Zero, Quaternion.Identity),
            (new Vector3(-1, 1, 1), Vector3.Zero, Quaternion.Identity),
            (new Vector3(float.NaN, 1, 1), Vector3.Zero, Quaternion.Identity),
            (new Vector3(float.PositiveInfinity, 1, 1), Vector3.Zero, Quaternion.Identity),
            (Vector3.One, new Vector3(float.NaN, 0, 0), Quaternion.Identity),
            (Vector3.One, Vector3.Zero, new Quaternion(0, 0, 0, 2))
        };
        foreach (var (moments, centre, frame) in invalid)
        {
            var scene = new LawScene(false);
            var body = scene.Sphere(Vector3.Zero, .1f, 1, new(0, .1f, 0, 0), default, default, 0, 0);
            scene.DeclareMassFrame(body, moments, centre, frame);
            Assert.Throws<ArgumentException>(() => scene.Admit());
        }
    }


    [Theory]
    [InlineData(ColliderShapeKind.Sphere, 1f / 32)]
    [InlineData(ColliderShapeKind.Sphere, .045f)]
    [InlineData(ColliderShapeKind.Sphere, .055f)]
    [InlineData(ColliderShapeKind.Sphere, 16f)]
    [InlineData(ColliderShapeKind.Torus, 1f / 512)]
    [InlineData(ColliderShapeKind.Torus, .0075f * (.136734f * .25f + .453070f))]
    public void AdmittedSmallFeaturesGiveContactAndEmptySpaceControls(ColliderShapeKind shape, float radius)
    {
        LawBody Observe(float gap)
        {
            var scene = new LawScene();
            var position = new Vector3(0, LawScene.BenchY + radius + gap, 0);
            var material = new LawMaterial(0, .1f, 0, 0);
            var body = shape == ColliderShapeKind.Sphere
                ? scene.Sphere(position, radius, 1, material, new(0, -.1f, 0), default, 0, 0)
                : scene.DynamicHollow(shape, position, Quaternion.Identity, .2f,
                    new(0, radius, MathF.Tau), material, 1, Vector3.One, new(0, -.1f, 0));
            scene.DeclareMassFrame(body, Vector3.One);
            return scene.Admit(SimulationCadence.Hz240).Step(1)[body];
        }
        var contact = Observe(-.0002f);
        var empty = Observe(.01f);
        Assert.True(contact.Velocity.Y > -.02f, $"contact velocity {contact.Velocity.Y:R}");
        Assert.InRange(MathF.Abs(empty.Velocity.Y + .1f), 0, 1e-6f);
        Assert.InRange(empty.Spin, 0, 1e-6);
    }

    [Fact]
    public void SphereAndTorusRejectPredecessorsAndNonfiniteShapeInputsAtAdmission()
    {
        foreach (var radius in new[] { MathF.BitDecrement(1f / 32), MathF.BitIncrement(16f), float.NaN, float.PositiveInfinity })
        {
            var scene = new LawScene(false);
            var body = scene.Sphere(new(0, 2, 0), .1f, 1, new(0, .1f, 0, 0), default, default, 0, 0);
            scene.ChildCollider(body, ColliderShapeKind.Sphere, Vector3.Zero, radius);
            Assert.Throws<ArgumentException>(() => scene.Admit());
        }
        foreach (var radius in new[] { MathF.BitDecrement(1f / 512), float.NaN, float.PositiveInfinity })
        {
            var scene = new LawScene(false);
            scene.DynamicHollow(ColliderShapeKind.Torus, new(0, 2, 0), Quaternion.Identity,
                .2f, new(0, radius, MathF.Tau), new(0, .1f, 0, 0), 1, Vector3.One);
            Assert.Throws<ArgumentException>(() => scene.Admit());
        }
    }


    [Theory]
    [InlineData(Children.Bare)]
    [InlineData(Children.Forward)]
    [InlineData(Children.Reverse)]
    public void OffCentreImpactUsesDeclaredMassCentreAndRotatedPrincipalInertia(Children children)
    {
        var scene = new LawScene();
        var body = scene.Sphere(new(0, LawScene.BenchY + .1f - .0002f, 0), .1f, 2,
            new(0, .1f, 0, 0), new(0, -1, 0), Vector3.Zero, 0, 0);
        scene.DeclareMassFrame(body, new(.2f, .3f, .4f), new(.2f, 0, 0),
            Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 4));
        if (children != Children.Bare)
        {
            var radii = children == Children.Forward ? new[] { .2f, .5f } : new[] { .5f, .2f };
            foreach (var radius in radii)
                scene.ChildCollider(body, ColliderShapeKind.Sphere, new(radius * 2, 2, 0), radius);
        }
        var observed = scene.Admit(SimulationCadence.Hz240).Step(1)[body];
        var impulse = 2f * (observed.Velocity.Y + 1f);
        Assert.InRange(impulse, .5f, 2f);
        // Rotation of diag(5,10/3,2.5) by Y45 gives inverse-inertia
        // XZ=-1.25, ZZ=3.75. The normal impulse at x=-.2 relative
        // to declared COM gives angular impulse (0,0,-.2 J).
        // Over 1/240 s, energy <=1 J bounds spin<sqrt(10) and frame
        // travel<.014 rad. A .04 rad/s envelope covers that finite-step
        // change; inferring sphere inertia or ignoring COM fails directly.
        Assert.InRange(MathF.Abs(observed.Angular.X - .25f * impulse), 0, .04f);
        Assert.InRange(MathF.Abs(observed.Angular.Z + .75f * impulse), 0, .04f);
        Assert.InRange(MathF.Abs(observed.Angular.Y), 0, .04f);
        Assert.InRange(MathF.Abs(observed.Velocity.X) + MathF.Abs(observed.Velocity.Z), 0, 1e-6f);
    }

    [Theory]
    [InlineData(.6f)]
    [InlineData(.9f)]
    public void AnUnloadedLargerChildCannotEnlargeTheRollingTorqueBudget(float childRadius)
    {
        LawBody Observe(bool child, float resistance)
        {
            var scene = new LawScene();
            var body = scene.Sphere(new(0, LawScene.BenchY + .3f - .0002f, 0), .3f, 1,
                new(0, .1f, 0, resistance), new(0, -.1f, 0), Vector3.UnitZ, 0, 0);
            scene.DeclareMassFrame(body, Vector3.One);
            // The larger child is inside the speculative envelope but 2 mm clear
            // of the plane. Its bottom remains above the loaded primary surface.
            if (child) scene.ChildCollider(body, ColliderShapeKind.Sphere,
                new(0, childRadius - .3f + .0022f, 0), childRadius);
            return scene.Admit(SimulationCadence.Hz240).Step(1)[body];
        }

        var free = Observe(false, 0);
        var primary = Observe(false, .1f);
        var compound = Observe(true, .1f);
        // I=1 and r=.3: .1 N·s impact gives approximately .003 rad/s
        // rolling loss. The control must directly detect missing resistance.
        Assert.True(free.Angular.Z - primary.Angular.Z > .001f);
        // The added, unloaded geometry cannot change linear or angular impulse.
        // 10 µrad/s is over 100 f32 roundings at unit spin, below the .003
        // rad/s extra loss that incorrectly using the .6 m arm would produce.
        Assert.InRange(Vector3.Distance(primary.Angular, compound.Angular), 0, 1e-5f);
        Assert.InRange(Vector3.Distance(primary.Velocity, compound.Velocity), 0, 1e-5f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CollisionChildrenDoNotChangeDeclaredFreeFlight(bool reverseChildren)
    {
        var scene = new LawScene(false);
        var body = scene.Sphere(new(0, 2, 0), .1f, 2, new(0, .1f, 0, 0),
            new(1, 2, 3), Vector3.Zero, 0, -4);
        scene.DeclareMassFrame(body, new(.2f, .3f, .4f), new(.2f, 0, 0),
            Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 4));
        var radii = reverseChildren ? new[] { .5f, .2f } : new[] { .2f, .5f };
        foreach (var radius in radii)
            scene.ChildCollider(body, ColliderShapeKind.Sphere, new(radius * 2, 0, 0), radius);
        var world = scene.Admit(SimulationCadence.Hz120);
        var observed = world.Step(30)[body];
        // Quarter-second acceleration, with zero torque: geometry has no role
        // in either the translational acceleration or principal inertia.
        Assert.InRange(Vector3.Distance(observed.Velocity, new(1, 1, 3)), 0, 2e-5f);
        Assert.InRange(observed.Spin, 0, 1e-7);
        Assert.InRange(MathF.Abs(observed.Position.X - .25f), 0, 2e-5f);
        Assert.InRange(MathF.Abs(observed.Position.Z - .75f), 0, 2e-5f);
    }
}



public sealed class CompoundPlateMembershipTests
{
    public enum Support { Ring, TopAndRing, TwoBodies, Stud, Side }

    [Fact]
    public void LoadMembershipRejectsMissingDuplicateAndForeignColliders()
    {
        var plate = PlacedParts.Plate(new(1), 0, 0, 0, .75f);
        var compiled = WorkshopPhysicsCompiler.Compile(new(new(2), WorkshopCadenceSettings.Default(), new([plate])), new(1, 2));
        var row = compiled.ContactLoadSensors[0];
        var foreign = compiled.Colliders.ToArray().Single(c => c.Shape == ColliderShapeKind.Plane).Id;
        var invalid = new[]
        {
            row with { SupportingColliders = [] },
            row with { SupportingColliders = [row.Region, row.Region] },
            row with { SupportingColliders = [foreign] },
            row with { SupportingColliders = [new(compiled.NextIdentity + 1)] }
        };
        foreach (var bad in invalid)
            Assert.Throws<ArgumentException>(() => new PhysicsSceneDeclaration(new(1, 2), compiled.NextIdentity,
                compiled.Bodies, compiled.Colliders, compiled.Materials, [], [], contactLoadSensors: [bad]));
        // Rejected declarations never alter the valid input's membership.
        var admitted = PhysicsGpuAbi.Admission(compiled, new(1),
            new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        Assert.Equal(row.SupportingColliders.ToArray(), PhysicsGpuAbi.ReadContactLoadDeclaration(admitted, 0).SupportingColliders.ToArray());
    }


    [Theory]
    [InlineData(Support.Ring, 1f)]
    [InlineData(Support.TopAndRing, 1f)]
    [InlineData(Support.TwoBodies, 1f)]
    [InlineData(Support.Stud, 0f)]
    [InlineData(Support.Side, 0f)]
    public void ActualPlateMembersMeasureDirectBodiesOnceWithoutSupply(Support support, float expectedMass)
    {
        var plate = PlacedParts.Plate(new(1), 0, 0, 0, .75f);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new([plate]));
        var compiled = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        var fixtures = new LawScene(false);
        GpuBodyId Ball(Vector3 position, float mass, Vector3 velocity) => fixtures.Sphere(position,
            .05f, mass, new(0, .1f, 0, 0), velocity, default, 0, 0);
        // Literal art: top .08, ring centreline .3 at y=.084 with .018
        // tube radius, studs .055 centred at (+/-1,.015,+/-1).
        var body = support switch
        {
            Support.Ring or Support.TwoBodies => Ball(new(.3f, .15195f, 0), support == Support.TwoBodies ? .4f : 1f, new(0, -.1f, 0)),
            Support.TopAndRing => Ball(new(.6f, .12995f, 0), 1, new(0, -.1f, 0)),
            Support.Stud => Ball(new(1, .11995f, 1), 1, new(0, -.1f, 0)),
            Support.Side => Ball(new(.94995f, .03f, 0), 1, new(-.1f, 0, 0)),
            _ => throw new ArgumentOutOfRangeException(nameof(support))
        };
        if (support == Support.TopAndRing)
            fixtures.ChildCollider(body, ColliderShapeKind.Sphere, new(-.3f, .022f, 0), .05f);
        if (support == Support.TwoBodies) Ball(new(-.6f, .12995f, 0), .6f, new(0, -.1f, 0));
        var fixture = fixtures.Admit().Scene;
        var offset = compiled.NextIdentity;
        var bodies = compiled.Bodies.ToArray().Concat(fixture.Bodies.ToArray().Select(b => b with { Id = new(b.Id.Value + offset) })).ToArray();
        var colliders = compiled.Colliders.ToArray().Concat(fixture.Colliders.ToArray().Select(c => c with
        { Id = new(c.Id.Value + offset), Body = new(c.Body.Value + offset), Material = new(c.Material.Value + offset) })).ToArray();
        var materials = compiled.Materials.ToArray().Concat(fixture.Materials.ToArray().Select(m => m with { Id = new(m.Id.Value + offset) })).ToArray();
        var scene = new PhysicsSceneDeclaration(new(1, 2), offset + fixture.NextIdentity, bodies, colliders, materials, [], [],
            contactLoadSensors: compiled.ContactLoadSensors);
        var world = new LawWorld(scene, PhysicsGpuAbi.Admission(scene, new(1),
            new(SimulationCadence.Hz240, PhysicalStepProfile.Canonical480Hz, new(1))), SimulationCadence.Hz240);
        var observed = world.Step(1);
        var load = PhysicsGpuAbi.ReadContactLoadSensor(observed.World, 0);
        Assert.Equal(expectedMass, load.MeasuredMass);
        Assert.Equal(expectedMass > 0 ? ContactLoadPhase.Loaded : ContactLoadPhase.Empty, load.Phase);
        Assert.Equal(plate.Id, load.Frame);
        var motion = observed[new GpuBodyId(body.Value + offset)];
        // Negative membership controls still physically hit the visible surface.
        if (support == Support.Stud) Assert.True(motion.Velocity.Y > -.09f);
        if (support == Support.Side) Assert.True(motion.Velocity.X > -.09f);
    }
}


public sealed class AssignedCompoundFeatureTests
{
    public enum Feature { BasketballSeam, DominoLowerPip, DominoUpperPip, WeightBase, WeightLowerBand,
        WeightUpperBand, WeightEye, PlateStud0, PlateStud1, PlateStud2, PlateStud3, PlateRing, ClockPivot }

    [Theory]
    [InlineData(Feature.BasketballSeam)]
    [InlineData(Feature.DominoLowerPip)]
    [InlineData(Feature.DominoUpperPip)]
    [InlineData(Feature.WeightBase)]
    [InlineData(Feature.WeightLowerBand)]
    [InlineData(Feature.WeightUpperBand)]
    [InlineData(Feature.WeightEye)]
    [InlineData(Feature.PlateStud0)]
    [InlineData(Feature.PlateStud1)]
    [InlineData(Feature.PlateStud2)]
    [InlineData(Feature.PlateStud3)]
    [InlineData(Feature.PlateRing)]
    [InlineData(Feature.ClockPivot)]
    public void EveryAssignedVisibleFeatureStopsAProbeAndLeavesAdjacentSpaceEmpty(Feature feature)
    {
        // Expected surfaces are literal art geometry, independent of compiled
        // collider lookup/support queries. Weight's default mass is 4 kg.
        const float scale = .136734f * 4 + .453070f;
        var (part, surface, normal) = feature switch
        {
            Feature.BasketballSeam => (PartId.Basketball, new Vector3(0, 0, .35f), Vector3.UnitZ),
            Feature.DominoLowerPip => (PartId.Domino, new Vector3(.185f, -.25f, 0), Vector3.UnitX),
            Feature.DominoUpperPip => (PartId.Domino, new Vector3(.185f, .25f, 0), Vector3.UnitX),
            Feature.WeightBase => (new PartId("weight"), scale * new Vector3(.36f, .0517f, 0), Vector3.UnitX),
            Feature.WeightLowerBand => (new PartId("weight"), scale * new Vector3(.3633f, .1862f, 0), Vector3.UnitX),
            Feature.WeightUpperBand => (new PartId("weight"), scale * new Vector3(.3633f, .3724f, 0), Vector3.UnitX),
            Feature.WeightEye => (new PartId("weight"), scale * new Vector3(0, .7346f, 0), Vector3.UnitY),
            Feature.PlateStud0 => (PartId.PressurePlate, new Vector3(-1, .07f, -1), Vector3.UnitY),
            Feature.PlateStud1 => (PartId.PressurePlate, new Vector3(1, .07f, -1), Vector3.UnitY),
            Feature.PlateStud2 => (PartId.PressurePlate, new Vector3(-1, .07f, 1), Vector3.UnitY),
            Feature.PlateStud3 => (PartId.PressurePlate, new Vector3(1, .07f, 1), Vector3.UnitY),
            Feature.PlateRing => (PartId.PressurePlate, new Vector3(.3f, .102f, 0), Vector3.UnitY),
            Feature.ClockPivot => (PartId.Clock, new Vector3(0, .4f, .495f), Vector3.UnitZ),
            _ => throw new ArgumentOutOfRangeException(nameof(feature))
        };
        LawBody Observe(float gap)
        {
            var instance = WorkshopInput.Generic(part, new(1), 0, 3, 0, 0, 0, 0, 1);
            var compiled = WorkshopPhysicsCompiler.Compile(new(new(2), WorkshopCadenceSettings.Default(), new([instance])), new(1, 2));
            var fixtures = new LawScene(false);
            var probe = fixtures.Sphere(new Vector3(0, 3, 0) + surface + (.03125f + gap) * normal,
                .03125f, .01f, new(0, .1f, 0, 0), -.1f * normal, default, 0, 0);
            var fixture = fixtures.Admit().Scene;
            var offset = compiled.NextIdentity;
            var bodies = compiled.Bodies.ToArray().Select(b => b with { Gravity = default }).Concat(
                fixture.Bodies.ToArray().Select(b => b with { Id = new(b.Id.Value + offset) })).ToArray();
            var colliders = compiled.Colliders.ToArray().Concat(fixture.Colliders.ToArray().Select(c => c with
            { Id = new(c.Id.Value + offset), Body = new(c.Body.Value + offset), Material = new(c.Material.Value + offset) })).ToArray();
            var materials = compiled.Materials.ToArray().Concat(fixture.Materials.ToArray().Select(m => m with { Id = new(m.Id.Value + offset) })).ToArray();
            var scene = new PhysicsSceneDeclaration(new(1, 2), offset + fixture.NextIdentity, bodies, colliders, materials, [], []);
            var world = new LawWorld(scene, PhysicsGpuAbi.Admission(scene, new(1),
                new(SimulationCadence.Hz240, PhysicalStepProfile.Canonical480Hz, new(1))), SimulationCadence.Hz240);
            return world.Step(1)[new GpuBodyId(probe.Value + offset)];
        }
        var contact = Observe(-.00005f);
        var empty = Observe(.01f);
        Assert.True(Vector3.Dot(contact.Velocity, normal) > -.03f, $"{feature}: contact velocity {contact.Velocity}");
        Assert.InRange(Vector3.Distance(empty.Velocity, -.1f * normal), 0, 1e-6f);
        Assert.InRange(empty.Spin, 0, 1e-6);
    }
}


public sealed class CompoundCaptureOwnershipTests
{

    [Fact]
    public async Task SparseCapturePrepareDiscardAndForeignPairRefusalPreserveCommittedHistory()
    {
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(
            PayloadInstanceTests.Place(PartId.Basketball, 1, 0, 1.1, 0),
            WorkshopInput.Generic(PartId.Receiver, new(2), 0, 1, 0, 0, 0, 0, 1)));
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        var admitted = PhysicsGpuAbi.Admission(scene, new(1),
            new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        var transport = new CuriousContraptions.Simulation.BrowserGpuTransport(new CuriousContraptions.Abstractions.EventRingPublication());
        try
        {
        await transport.Initialize();
        var inputs = new PhysicsTickInputs([], []);
        await transport.Stage(admitted, WorkshopGpuOperation.Admit, inputs);
        var committed = transport.Read();
        Assert.Empty(committed.Interactions.Captures);
        Assert.Empty(committed.Interactions.Works);
        transport.PrepareEvents(EventRingBuffer.Batch(new(1, EventRingBuffer.SlotBytes, 1, 0), []));
        transport.Commit();
        var captured = false;
        for (ulong tick = 1; tick <= 120 && !captured; tick++)
        {
            await transport.Stage(ReadOnlyMemory<byte>.Empty, WorkshopGpuOperation.Advance, inputs);
            var candidate = transport.Read();
            PhysicsGpuAbi.ValidateCandidate(candidate.World.Span, committed.World.Span, new(tick), candidate.Interactions, committed.Interactions);
            var latch = PhysicsGpuAbi.ReadCaptures(candidate.Interactions);
            if (latch.Count == 0 || latch[0].Phase != CaptureLatchPhase.Latched)
            {
                transport.PrepareEvents(EventRingBuffer.Batch(new(1, EventRingBuffer.SlotBytes, 1, 0), []));
        transport.Commit(); committed = candidate; continue;
            }
            var earned = Assert.Single(candidate.Interactions.Captures);
            // Reject foreign policy, absent target, and a present static non-payload target
            // without publishing or destroying the valid staged candidate.
            foreach (var key in new[] { new CapturePairKey(new(999), new(1)), new CapturePairKey(earned.Key.Policy, new(999)),
                new CapturePairKey(earned.Key.Policy, new(2)) })
            {
                var value = new CapturePairState(earned.Value.Residence with { Id = key.Policy, Target = key.Target },
                    earned.Value.Latch with { Sensor = key.Policy, Target = key.Target });
                var malformed = new PhysicsInteractionSnapshot(PhysicsInteractionSnapshot.Empty.Captures.Add(key, value),
                    PhysicsInteractionSnapshot.Empty.Works);
                Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(candidate.World.Span, committed.World.Span,
                    new(tick), malformed, committed.Interactions));
            }
            transport.Discard();
            Assert.DoesNotContain(committed.Interactions.Captures.Values, value => value.Latch.Phase == CaptureLatchPhase.Latched);
            await transport.Stage(ReadOnlyMemory<byte>.Empty, WorkshopGpuOperation.Advance, inputs);
            var retry = transport.Read();
            Assert.Equal(earned.Value, Assert.Single(retry.Interactions.Captures).Value);
            PhysicsGpuAbi.ValidateCandidate(retry.World.Span, committed.World.Span, new(tick), retry.Interactions, committed.Interactions);
            transport.PrepareEvents(EventRingBuffer.Batch(new(1, EventRingBuffer.SlotBytes, 1, 0), []));
        transport.Commit();
            await transport.Stage(ReadOnlyMemory<byte>.Empty, WorkshopGpuOperation.Advance, inputs);
            var next = transport.Read();
            var retained = Assert.Single(next.Interactions.Captures).Value;
            Assert.Equal(earned.Value.Latch, retained.Latch);
            Assert.Equal(0u, retained.Residence.OccurrenceCount);
            PhysicsGpuAbi.ValidateCandidate(next.World.Span, retry.World.Span, new(tick + 1), next.Interactions, retry.Interactions);
            transport.PrepareEvents(EventRingBuffer.Batch(new(1, EventRingBuffer.SlotBytes, 1, 0), []));
        transport.Commit();
            captured = true;
        }
        Assert.True(captured, "The fixture must earn a real capture through the production dwell.");
        }
        finally { transport.Dispose(); }
    }

    [Theory]
    [InlineData(false, 0f)]
    [InlineData(false, 37f)]
    [InlineData(true, 0f)]
    [InlineData(true, 37f)]
    public void CompetingGuidesChooseNearestThenAuthoredOwnerWithoutSummingAcceleration(bool tied, float yaw)
    {
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, yaw * MathF.PI / 180);
        var position = Vector3.Transform(new Vector3(tied ? 0 : .08f, 2.2f, 0), rotation);
        IWorkshopInstance Receiver(ulong id, float x)
        {
            var centre = Vector3.Transform(new Vector3(x, 1, 0), rotation);
            var parameters = InstanceParameterCollection.Defaults(PartId.Receiver).ToDictionary();
            parameters[new("force_maximum_acceleration")] = 12f;
            return WorkshopInput.Generic(PartId.Receiver, new(id), (float)(centre.X), (float)(centre.Y), (float)(centre.Z),
                (float)(rotation.X), (float)(rotation.Y), (float)(rotation.Z), (float)(rotation.W),
                new(parameters));
        }
        var left = Receiver(5, -.2f); var right = Receiver(6, .2f);
        var ball = PayloadInstanceTests.Place(PartId.Basketball, 1, position.X, position.Y, position.Z);
        var chosen = tied ? left : right;
        LawBody Step(params IWorkshopInstance[] owners)
        {
            var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new([ball, ..owners]));
            var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
            var world = new LawWorld(scene, PhysicsGpuAbi.Admission(scene, new(1),
                new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1))), SimulationCadence.Hz120);
            return world.Step(1)[ball.Id];
        }
        var single = Step(chosen); var both = Step(left, right); var reversed = Step(right, left);
        // One guide acts for 1/120 s at 12 m/s² per metre. The literal .12/.2 m
        // offset predicts .012/.020 m/s; summed or opposing forces fail this control.
        var direction = Vector3.Transform(tied ? -Vector3.UnitX : Vector3.UnitX, rotation);
        Assert.InRange(Vector3.Dot(both.Velocity, direction), tied ? .019f : .011f, tied ? .021f : .013f);
        Assert.InRange(Vector3.Distance(single.Velocity, both.Velocity), 0, 2e-6f);
        Assert.InRange(Vector3.Distance(single.Position, both.Position), 0, 2e-6f);
        Assert.Equal(both, reversed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DistantReceiversCaptureTheirOwnPayloadsWithSparseStateAndResetReplay(bool reverse)
    {
        IWorkshopInstance[] parts = [
            PayloadInstanceTests.Place(PartId.Basketball, 1, -4, 1.1, 0),
            PayloadInstanceTests.Place(PartId.BowlingBall, 2, 4, 1.1, 0),
            PayloadInstanceTests.Place(PartId.Basketball, 3, 10, 5, 0),
            WorkshopInput.Generic(PartId.Receiver, new(5), -4, 1, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(PartId.Receiver, new(6), 4, 1, 0, 0, 0, 0, 1)];
        if (reverse) Array.Reverse(parts);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(parts));
        await using var ring = await WorkshopIntegrationHarness.Running(construction);
        Assert.Equal(0, ring.Committed.Captures.Count);
        await ring.Advance(120);
        var read = ring.Committed;
        Assert.Equal(2, read.Captures.Count); // six potential pairs, two actual interactions
        AssertCaptureCodec(read);
        Assert.Equal(new GpuBodyId(1), read.Captures[0].Target);
        Assert.Equal(new GpuBodyId(2), read.Captures[1].Target);
        Assert.All(Enumerable.Range(0, read.Captures.Count), i => Assert.Equal(CaptureLatchPhase.Latched, read.Captures[i].Phase));
        Assert.Equal(new[] { (5UL, 1UL), (6UL, 2UL) }, ring.Of(SimulationEventKind.Captured).Select(e => (e.Source, e.Other)).ToArray());
        foreach (var (owner, target) in new[] { (5UL, 1UL), (6UL, 2UL) })
        {
            Assert.Equal(WorkshopGoalPhase.Solved, WorkshopGoalEvaluator.Evaluate(new(WorkshopGoalKind.Captured, new(target), new(owner)),
                read, read.Epoch, construction.Instances.Identities));
            Assert.Equal(WorkshopGoalPhase.Waiting, WorkshopGoalEvaluator.Evaluate(new(WorkshopGoalKind.Captured, new(3), new(owner)),
                read, read.Epoch, construction.Instances.Identities));
        }
        await ring.Reset();
        Assert.Equal(0, ring.Committed.Captures.Count);
        var saved = WorkshopSaveCodec.Decode(SaveFixture.Elements, WorkshopSaveCodec.Encode(new WorkshopSavedConstruction(construction, new(7))));
        var replay = await WorkshopIntegrationHarness.Observe(saved.Construction, 120);
        Assert.Equal(2, replay[^1].Captures.Count);
        Assert.Equal(new[] { new GpuBodyId(1), new GpuBodyId(2) },
            Enumerable.Range(0, 2).Select(i => replay[^1].Captures[i].Target));
        Assert.All(Enumerable.Range(0, 2), i => Assert.Equal(CaptureLatchPhase.Latched, replay[^1].Captures[i].Phase));
        foreach (var (owner, target) in new[] { (5UL, 1UL), (6UL, 2UL) })
        {
            Assert.Equal(WorkshopGoalPhase.Solved, WorkshopGoalEvaluator.Evaluate(new(WorkshopGoalKind.Captured, new(target), new(owner)),
                replay[^1], replay[^1].Epoch, saved.Construction.Instances.Identities));
            Assert.Equal(WorkshopGoalPhase.Waiting, WorkshopGoalEvaluator.Evaluate(new(WorkshopGoalKind.Captured, new(3), new(owner)),
                replay[^1], replay[^1].Epoch, saved.Construction.Instances.Identities));
        }
    }
    [Fact]
    public async Task OneCapturePolicyKeepsTwoActualTargetsAcrossTheCommittedReadCodec()
    {
        // Literal Bowling radius .28: centres .60 apart clear each other and remain inside the receiver walls.
        IWorkshopInstance[] parts = [
            PayloadInstanceTests.Place(PartId.BowlingBall, 1, -.3, 1.1, 0),
            PayloadInstanceTests.Place(PartId.BowlingBall, 2, .3, 1.1, 0),
            WorkshopInput.Generic(PartId.Receiver, new(3), 0, 1, 0, 0, 0, 0, 1)];
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(parts));
        await using var ring = await WorkshopIntegrationHarness.Running(construction);
        await ring.Advance(120);
        var read = ring.Committed;
        Assert.Equal(2, read.Captures.Count);
        Assert.Equal(read.Captures[0].Sensor, read.Captures[1].Sensor);
        Assert.Equal(new GpuBodyId(1), read.Captures[0].Target);
        Assert.Equal(new GpuBodyId(2), read.Captures[1].Target);
        Assert.All(Enumerable.Range(0, 2), i => Assert.Equal(CaptureLatchPhase.Latched, read.Captures[i].Phase));
        AssertCaptureCodec(read);
    }

    private static void AssertCaptureCodec(WorkshopRead read)
    {
        var response = new WorkshopResponse(default, WorkshopResponseKind.Read,
            new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), WorkshopSimulationPhase.Running, read,
            new(1, 2), new(1), new(1), read.Capture!.Value.Generation, new(1), new(read.Tick.Value));
        var decoded = WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(response)).Read;
        Assert.Equal(read.Captures.Count, decoded.Captures.Count);
        for (var i = 0; i < read.Captures.Count; i++)
        {
            var expected = read.Captures[i]; var actual = decoded.Captures[i];
            Assert.Equal(expected.Sensor, actual.Sensor);
            Assert.Equal(expected.Target, actual.Target);
            Assert.Equal(expected.Phase, actual.Phase);
            Assert.Equal(expected.EventOrdinal, actual.EventOrdinal);
            Assert.Equal(expected.EventPhase, actual.EventPhase);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task CaptureTruthKeepsTheAuthoredPayloadThroughGrowthPlacementResetAndLoad(bool growth, bool namedCaptures)
    {
        var goal = new WorkshopGoal(WorkshopGoalKind.Captured, new(1), new(3));
        var parts = new List<IWorkshopInstance>
        {
            PayloadInstanceTests.Place(PartId.Basketball, 1, namedCaptures ? 0 : 4, 1.1, 0),
            PayloadInstanceTests.Place(PartId.BowlingBall, 2, namedCaptures ? 4 : 0, 1.1, 0)
        };
        if (growth)
            for (var i = 0; i < 32; i++) parts.Add(PlacedParts.Plate(new((ulong)(100 + i)), (float)(8 + 2.5 * (i % 4)), 3, (float)(-10 + 2.5 * (i / 4)), .5f));
        var withoutReceiver = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(parts.ToArray()));
        var absentRead = (await WorkshopIntegrationHarness.Observe(withoutReceiver, 1))[^1];
        Assert.Equal(WorkshopGoalPhase.Waiting, WorkshopGoalEvaluator.Evaluate(goal, absentRead, absentRead.Epoch, withoutReceiver.Instances.Identities));
        var absentWake = new WorkshopGoalWake(goal, WorkshopPhysicsCompiler.Compile(withoutReceiver, new(1, 2)));
        var construction = withoutReceiver.WithInstance(WorkshopInput.Generic(PartId.Receiver, new(3), 0, 1, 0, 0, 0, 0, 1));
        await using var ring = await WorkshopIntegrationHarness.Running(construction);
        var initial = ring.Committed;
        await ring.Advance(240);
        var read = ring.Committed;
        var captured = Assert.Single(ring.Of(SimulationEventKind.Captured));
        Assert.Equal(3UL, captured.Source);
        Assert.Equal(namedCaptures ? 1UL : 2UL, captured.Other);
        var expected = namedCaptures ? WorkshopGoalPhase.Solved : WorkshopGoalPhase.Waiting;
        Assert.Equal(expected, WorkshopGoalEvaluator.Evaluate(goal, read, read.Epoch, construction.Instances.Identities));
        Assert.Equal(WorkshopGoalPhase.Waiting, WorkshopGoalEvaluator.Evaluate(goal, read,
            new(read.Epoch.Value + 1), construction.Instances.Identities));
        var wake = new WorkshopGoalWake(goal, WorkshopPhysicsCompiler.Compile(construction, new(1, 2)));
        var delivery = new EventRingDelivery(read.Epoch.Value, 0, [captured]);
        wake.Observe(delivery, read.Tick);
        Assert.True(wake.Due(read.Tick)); // owner-level notification; precise truth remains above
        absentWake.Observe(delivery, read.Tick);
        Assert.False(absentWake.Due(read.Tick));
        await ring.Reset();
        var reset = ring.Committed;
        Assert.Equal(WorkshopGoalPhase.Waiting, WorkshopGoalEvaluator.Evaluate(goal, reset, reset.Epoch, construction.Instances.Identities));
        Assert.Equal(WorkshopGoalPhase.Waiting, WorkshopGoalEvaluator.Evaluate(goal, read, reset.Epoch, construction.Instances.Identities));
        foreach (var id in new[] { new GpuBodyId(1), new GpuBodyId(2) })
        {
            Assert.True(initial.Bodies.TryGet(id, out var before));
            Assert.True(reset.Bodies.TryGet(id, out var after));
            Assert.Equal(before.Body.Cell, after.Body.Cell);
            Assert.Equal(before.Body.Local, after.Body.Local);
            Assert.Equal(before.Rotation, after.Rotation);
        }
        var loaded = WorkshopSaveCodec.Decode(SaveFixture.Elements,
            WorkshopSaveCodec.Encode(new WorkshopSavedConstruction(construction, new(132)))).Construction;
        var replay = await WorkshopIntegrationHarness.Observe(loaded, 240);
        Assert.Equal(expected, WorkshopGoalEvaluator.Evaluate(goal, replay[^1], replay[^1].Epoch, loaded.Instances.Identities));
        Assert.Equal(WorkshopGoalEvaluator.Occurrence(goal, read, read.Epoch, construction.Instances.Identities),
            WorkshopGoalEvaluator.Occurrence(goal, replay[^1], replay[^1].Epoch, loaded.Instances.Identities));
    }
}


public sealed class CompoundPassageBoundaryTests
{
    [Theory]
    [InlineData(0f, 1)]
    [InlineData(.8f, 0)]
    public void ARealBounceCrossesAndReturnsWithinOneTickWithoutLosingThePass(float offset, int expected)
    {
        var fixture = new LawScene(bench: false);
        var frame = fixture.StaticBox(new(0, 7, 0), new(.1f));
        fixture.StaticBox(new(.15f, 3 + offset, 0), new(.01f, .2f, .2f), material: new(1, .1f, 0, 0));
        var body = fixture.Sphere(new(-.08f, 3 + offset, 0), .04f, 1, new(1, .1f, 0, 0),
            new(40, 0, 0), default, 0, 0);
        var world = fixture.Admit(SimulationCadence.Hz60);
        var sensor = new PassageSensorDeclaration(new(900), frame, new(0,48,0), default,
            CanonicalRotation.Identity, new(.65f), new(.02f));
        var initial = PhysicsGpuAbi.ReadDynamicBodies(world.Admitted);
        var tracker = PassageSensorTracker.Create([sensor], world.Scene, initial);
        Assert.Empty(tracker.States);
        var final = world.Step(1);
        var bodies = PhysicsGpuAbi.ReadDynamicBodies(final.World);
        var layout = PhysicsGpuAbi.Layout(final.World);
        var motion = PhysicsMotionRead.Decode(final.World.AsSpan(layout.MotionOffset, layout.MotionBytes), bodies, new(1));
        Assert.True(final[body].Position.X < 0, "the committed endpoint has returned upstream");
        Assert.True(Enumerable.Range(1,7).Any(i => motion.TrySample(body, new((ulong)i, -.4096f), out var pose) &&
            (pose.Cell.X + pose.Local.X) / 16 > 0), "the real trajectory crossed within the tick");
        var outputs = new PassageSensorRead[1];
        var candidate = tracker.Advance(new(1), bodies, motion, outputs, out var count);
        Assert.Equal(expected, count);
        Assert.Equal(expected, candidate.Fired.Count);
        Assert.Empty(tracker.States); // discarded candidate cannot earn the pass
        Assert.Throws<ArgumentNullException>(() => tracker.Advance(new(1), bodies, null!, outputs, out _));
        var admission = PhysicsMotionRead.Decode(new byte[PhysicsMotionRead.ByteLength(initial.Count)], initial, new(0));
        Assert.Throws<ArgumentException>(() => tracker.Advance(new(1), bodies, admission, outputs, out _));
    }


    [Theory]
    [InlineData(0f, 2f)]
    [InlineData(37f, 2f)]
    [InlineData(0f, 60f)]
    [InlineData(37f, 60f)]
    public void PassagePairsAppearOnlyWhenRealMotionNeedsMemoryAndCandidateDiscardPreservesPrior(float degrees, float speed)
    {
        var rotation=Quaternion.CreateFromAxisAngle(Vector3.UnitY,degrees*MathF.PI/180);
        var fixture=new LawScene(bench:false);
        var frames=Enumerable.Range(0,32).Select(i=>fixture.StaticBox(new(0,10+i,0),new(.1f))).ToArray();
        var origin=new Vector3(0,5,0);
        var start=origin+Vector3.Transform(new Vector3(-1,0,0),rotation);
        var body=fixture.Sphere(start,.1f,1,new(0,.1f,0,0),Vector3.Transform(new Vector3(speed,0,0),rotation),default,drag:0,gravity:0);
        var world=fixture.Admit();
        var policies=frames.Select((frame,i)=>new PassageSensorDeclaration(new((ulong)(10000+i)),frame,
            new(0,80,16*i*4),default,CanonicalRotation.Normalize(rotation.X,rotation.Y,rotation.Z,rotation.W),new(.65f),new(.02f))).ToArray();
        PhysicsBodyReadSet Read(LawState state)=>new(world.Scene.Bodies.ToArray().Where(declared=>declared.Motion==RigidMotionKind.Dynamic).Select(declared=>
        {
            var value=state[declared.Id];
            return new PhysicsBodyRead(new CanonicalBody(declared.Id,0,0,value.Cell,value.Local,default),
                CanonicalRotation.Normalize(value.Rotation.X,value.Rotation.Y,value.Rotation.Z,value.Rotation.W),default,default);
        }).ToArray());
        var initial=PassageSensorTracker.Create(policies,world.Scene,Read(new(world.Scene,world.Admitted,PhysicsInteractionSnapshot.Empty)));
        Assert.Empty(initial.States);
        Assert.Equal(1,initial.ParticipantCount);
        var tracker=initial;
        var outputs=new PassageSensorRead[policies.Length];
        var fired=0;
        world.Step((int)(240/speed),(tick,state)=>
        {
            var layout=PhysicsGpuAbi.Layout(state.World);
            var motion=PhysicsMotionRead.Decode(state.World.AsSpan(layout.MotionOffset,layout.MotionBytes),
                PhysicsGpuAbi.ReadDynamicBodies(state.World),new((ulong)tick));
            var candidate=tracker.Advance(new((ulong)tick),Read(state),motion,outputs,out var count);
            // Candidate evaluation leaves the prior immutable interaction snapshot available to discard.
            var replay=tracker.Advance(new((ulong)tick),Read(state),motion,new PassageSensorRead[policies.Length],out var repeated);
            Assert.Equal(count,repeated);
            Assert.Equal(candidate.States,replay.States);
            fired+=count;
            tracker=candidate;
        });
        Assert.Equal(1,fired);
        var earned=Assert.Single(tracker.States);
        Assert.Equal(body,earned.Body);
        Assert.Equal(policies[0].Id,earned.Sensor);
        Assert.Equal(1UL,earned.PassedCount);
        Assert.Empty(initial.States);
        Assert.Empty(PassageSensorTracker.Create(policies,world.Scene,Read(new(world.Scene,world.Admitted,PhysicsInteractionSnapshot.Empty))).States);
    }

    [Theory]
    [InlineData(0f, false)]
    [InlineData(37f, false)]
    [InlineData(0f, true)]
    [InlineData(37f, true)]
    public void SparsePassageRetainsUpstreamArmingAcrossLateralEntryButNeverInventsInitialBandArming(float degrees, bool initiallyUpstream)
    {
        var q=Quaternion.CreateFromAxisAngle(Vector3.UnitY,degrees*MathF.PI/180);
        var detector=PlacedParts.Detector(new(10),0,3,0,(float)(q.X),(float)(q.Y),(float)(q.Z),(float)(q.W));
        var ball=PayloadInstanceTests.Place(PartId.Basketball,2,-1,3,2);
        var construction=new WorkshopConstruction(new(2),WorkshopCadenceSettings.Default(),new([detector,ball]));
        var scene=WorkshopPhysicsCompiler.Compile(construction,new(1,2));
        var sensors=WorkshopActivationCompiler.Compile(construction).PassageSensors.ToArray();
        PhysicsBodyReadSet At(float x,float z)
        {
            var world=new Vector3(0,3,0)+Vector3.Transform(new Vector3(x,0,z),q);
            var px=WorkshopInput.Position((float)(world.X)); var py=WorkshopInput.Position((float)(world.Y)); var pz=WorkshopInput.Position((float)(world.Z));
            return new([new(new CanonicalBody(ball.Id,0,0,new(px.Cell,py.Cell,pz.Cell),new(px.Local,py.Local,pz.Local),default),
                CanonicalRotation.Identity,default,default)]);
        }
        var initial=PassageSensorTracker.Create(sensors,scene,At(initiallyUpstream ? -1 : -.1f,.8f));
        var motion = new PassageMotionFixture(At(initiallyUpstream ? -1 : -.1f,.8f));
        Assert.Empty(initial.States);
        var outputs=new PassageSensorRead[1];
        var band=initial.Advance(new(1),At(-.1f,.8f),motion.Create(At(-.1f,.8f)),outputs,out var entry);
        Assert.Equal(0,entry);
        Assert.Equal(initiallyUpstream ? 1 : 0,band.States.Count);
        var lateral=band.Advance(new(2),At(-.1f,0),motion.Create(At(-.1f,0)),outputs,out var sideways);
        Assert.Equal(0,sideways);
        var passed=lateral.Advance(new(3),At(.1f,0),motion.Create(At(.1f,0)),outputs,out var crossing);
        Assert.Equal(initiallyUpstream ? 1 : 0,crossing);
        Assert.Equal(initiallyUpstream ? 1 : 0,passed.States.Count);
        Assert.Empty(initial.States);
        Assert.Throws<ArgumentException>(()=>initial.Advance(new(4),new([]),motion.Create(At(.1f,0)),outputs,out _));
        Assert.Empty(initial.States);
    }

    [Theory]
    [InlineData(.365f, 1)]
    [InlineData(.36998f, 1)]
    [InlineData(.37002f, 2)]
    [InlineData(.4f, 2)]
    public void BasketballSeamMustClearBeforeRearmingAndStaticPlaneReadsAreExcluded(float upstream, int expected)
    {
        var detector = PlacedParts.Detector(new(10), 0, 3, 0, 0, 0, 0, 1);
        var ball = PayloadInstanceTests.Place(PartId.Basketball, 2, -1, 3, 0);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new([detector, ball]));
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        var sensors = WorkshopActivationCompiler.Compile(construction).PassageSensors.ToArray();
        PhysicsBodyReadSet At(float x)
        {
            var position = WorkshopInput.Position((float)(x));
            return new(scene.Bodies.ToArray().Select(body => new PhysicsBodyRead(
                new CanonicalBody(body.Id, 0, 0,
                    body.Id == ball.Id ? new(position.Cell, body.Cell.Y, body.Cell.Z) : body.Cell,
                    body.Id == ball.Id ? new(position.Local, body.Local.Y, body.Local.Z) : body.Local, default),
                body.Rotation, default, default)).ToArray());
        }
        var tracker = PassageSensorTracker.Create(sensors, scene, At(-1));
        var motion = new PassageMotionFixture(At(-1));
        Assert.Empty(tracker.States);
        Assert.Equal(1, tracker.ParticipantCount);
        // Literal seam centreline .336 + tube .014 = .35 m, plus CAT-002
        // clearance .02 m. The previous bare-sphere .34 m rule arms too soon.
        var output = new PassageSensorRead[1];
        var count = 0;
        ulong tick = 0;
        foreach (var x in new[] { .01f, -upstream, .01f })
        {
            tracker = tracker.Advance(new(++tick), At(x), motion.Create(At(x)), output, out var fired);
            count += fired;
        }
        Assert.Equal(expected, count);
        Assert.Equal((ulong)expected, tracker.States[0].PassedCount);
        var foreign = new PhysicsBodyRead(new CanonicalBody(new(9999), 0, 0, default, default, default),
            CanonicalRotation.Identity, default, default);
        Assert.Throws<ArgumentException>(() => PassageSensorTracker.Create(sensors, scene, new([foreign])));
    }
}


public sealed class CurrentPuzzleShapeTests
{
    public enum Envelope { Command, Response, Save }

    [Theory]
    [InlineData(Envelope.Command, 10)]
    [InlineData(Envelope.Response, 10)]
    [InlineData(Envelope.Save, 10)]
    [InlineData(Envelope.Command, 13)]
    [InlineData(Envelope.Response, 13)]
    [InlineData(Envelope.Save, 13)]
    public void EveryConstructionEnvelopeRefusesUnknownPuzzleFieldsBeforeDomainDecoding(Envelope envelope, int extraField)
    {
        var builder = new FlatBufferBuilder(512);
        var id = builder.CreateString("ready_to_rebound");
        Wire.PuzzleRecord.StartAssistanceVector(builder, 0);
        var assistance = builder.EndVector();
        builder.StartTable(extraField + 1);
        builder.AddOffset(0, id.Value, 0);
        builder.AddOffset(9, assistance.Value, 0);
        builder.AddUlong(extraField, 123, 0);
        var puzzle = new Offset<Wire.PuzzleRecord>(builder.EndTable());
        Wire.ConstructionRecord.StartConstructionRecord(builder);
        Wire.ConstructionRecord.AddPuzzle(builder, puzzle);
        var construction = Wire.ConstructionRecord.EndConstructionRecord(builder);
        string identifier;
        VerifyTableAction verify;
        switch (envelope)
        {
            case Envelope.Command:
                Wire.WorkshopCommand.StartWorkshopCommand(builder);
                Wire.WorkshopCommand.AddConstructionRecord(builder, construction);
                builder.FinishSizePrefixed(Wire.WorkshopCommand.EndWorkshopCommand(builder).Value, WorkshopWireFlatBuffers.CommandIdentifier);
                identifier = WorkshopWireFlatBuffers.CommandIdentifier; verify = Wire.WorkshopCommandVerify.Verify;
                break;
            case Envelope.Response:
                Wire.WorkshopResponse.StartWorkshopResponse(builder);
                Wire.WorkshopResponse.AddConstruction(builder, construction);
                builder.FinishSizePrefixed(Wire.WorkshopResponse.EndWorkshopResponse(builder).Value, WorkshopWireFlatBuffers.ResponseIdentifier);
                identifier = WorkshopWireFlatBuffers.ResponseIdentifier; verify = Wire.WorkshopResponseVerify.Verify;
                break;
            case Envelope.Save:
                var saved = Wire.SavedConstruction.CreateSavedConstruction(builder, 1, construction);
                builder.FinishSizePrefixed(saved.Value, WorkshopSaveCodec.Identifier);
                identifier = WorkshopSaveCodec.Identifier; verify = Wire.SavedConstructionVerify.Verify;
                break;
            default: throw new ArgumentOutOfRangeException(nameof(envelope));
        }
        var bytes = builder.SizedByteArray();
        // The vendored verifier deliberately permits unknown fields. The current
        // game schema must refuse them before interpreting any shifted scalar.
        Assert.True(new Verifier(new ByteBuffer(bytes)).VerifyBuffer(identifier, true, verify));
        switch (envelope)
        {
            case Envelope.Command:
                Assert.Equal(WireFaultReason.VerifierRejected, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeCommand(bytes)).Reason);
                break;
            case Envelope.Response:
                Assert.Equal(WireFaultReason.VerifierRejected, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeResponse(bytes)).Reason);
                break;
            case Envelope.Save:
                SaveFixture.Refused(WorkshopSaveRejection.VerifierRejected, () => WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes));
                break;
        }
    }
}


public sealed class CompoundIdentityRefusalTests
{
    public enum InvalidEdit { DuplicateOwner, ExhaustedIdentity, ForeignConnection }

    [Theory]
    [InlineData(InvalidEdit.DuplicateOwner)]
    [InlineData(InvalidEdit.ExhaustedIdentity)]
    [InlineData(InvalidEdit.ForeignConnection)]
    public async Task InvalidSceneSizedReferencesRefuseAtomicallyAndThePriorWorldStillRuns(InvalidEdit edit)
    {
        var first = WorkshopInput.Generic(PartId.Receiver, new(5), 0, 1, 0, 0, 0, 0, 1);
        var second = WorkshopInput.Generic(PartId.Receiver, new(6), 4, 1, 0, 0, 0, 0, 1);
        var tile = PlacedParts.Domino(new(2), -3, 3, 0, 0, 0, 0, 1);
        var valid = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(first, second, tile));
        await using var device = new WorkshopGpuDevice(
            new CuriousContraptions.Simulation.BrowserGpuTransport(new CuriousContraptions.Abstractions.EventRingPublication()), new(101, 206));
        await using var simulation = new WorkshopSimulation(device, new WorkshopPhysicsEngineTests.StepClock(),
            WorkshopCadenceSettings.Default(), new(1), new WorkshopPhysicsEngineTests.NoInstallation(), GpuDeviceOwnership.Borrowed);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Initialize()).Outcome);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Construct(valid)).Outcome);
        var before = simulation.Committed;
        var invalid = valid with { Revision = new(3) };
        invalid = edit switch
        {
            InvalidEdit.DuplicateOwner => invalid with { Instances = new(first, second, tile, tile) },
            InvalidEdit.ExhaustedIdentity => invalid with { Instances = new(first, second, ((WorkshopGenericInstance)tile) with { Id = new(ulong.MaxValue) }) },
            InvalidEdit.ForeignConnection => invalid with { Connections = new(new WorkshopConnection(new(999), WorkshopSocket.ActivationOut,
                tile.Id, WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation)) },
            _ => throw new ArgumentOutOfRangeException(nameof(edit))
        };
        if (edit == InvalidEdit.ExhaustedIdentity)
        {
            Assert.Equal(WorkshopEditRejection.IdentityExhausted,
                Assert.Throws<WorkshopEditRejectedException>(() => invalid.Validate()).Reason);
            var edited = valid.WithInstance(((WorkshopGenericInstance)tile) with { Id = new(ulong.MaxValue) });
            Assert.Equal(WorkshopEditRejection.IdentityExhausted,
                Assert.Throws<WorkshopEditRejectedException>(() => edited.Validate()).Reason);
            Assert.Equal(3, valid.Instances.Count);
            Assert.Equal(new ConstructionRevision(2), valid.Revision);
        }
        var result = await simulation.Construct(invalid);
        Assert.Equal(WorkshopCommandOutcome.Rejected, result.Outcome);
        Assert.Equal(WorkshopRejection.InvalidConstruction, result.Reason);
        Assert.Equal(valid, simulation.Construction);
        Assert.Equal(before.Epoch, simulation.Committed.Epoch);
        Assert.Equal(before.Tick, simulation.Committed.Tick);
        Assert.Equal(before.Bodies.Count, simulation.Committed.Bodies.Count);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Run()).Outcome);
        for (var i = 0; i < 12; i++) Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Advance()).Outcome);
        Assert.True(simulation.Committed.Bodies.TryGet(tile.Id, out var fallen));
        Assert.True(before.Bodies.TryGet(tile.Id, out var built));
        Assert.True(fallen.Body.Cell.Y + fallen.Body.Local.Y < built.Body.Cell.Y + built.Body.Local.Y);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Reset()).Outcome);
        Assert.True(simulation.Committed.Bodies.TryGet(tile.Id, out var reset));
        Assert.Equal(built.Body.Cell, reset.Body.Cell);
        Assert.Equal(built.Body.Local, reset.Body.Local);
        Assert.Equal(built.Rotation, reset.Rotation);
        var loaded = WorkshopSaveCodec.Decode(SaveFixture.Elements,
            WorkshopSaveCodec.Encode(new WorkshopSavedConstruction(simulation.Construction, new(10))));
        Assert.Equal(valid, loaded.Construction);
        var replay = (await WorkshopIntegrationHarness.Observe(loaded.Construction, 12))[^1];
        Assert.True(replay.Bodies.TryGet(tile.Id, out var repeated));
        Assert.Equal(fallen.Body.Local, repeated.Body.Local);
        Assert.Equal(fallen.Body.Velocity, repeated.Body.Velocity);
    }
}


[Collection<NativeSceneCollectionDefinition>]
public sealed class MultiBodyPresentationTests(NativeSceneFixture godot)
{

    [Fact]
    public async Task CommittedVisualTargetsReuseCommonStorageAndGrowAtTheWatermark()
    {
        var instances = Enumerable.Range(0, 25).Select(index => WorkshopInput.Generic(PartId.Basketball,
            new GpuBodyId((ulong)index + 1), (float)(index % 5 - 2), 3, (float)(index / 5 - 2), 0, 0, 0, 1)).ToArray();
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(instances.Cast<IWorkshopInstance>().ToArray()));
        var read = (await WorkshopIntegrationHarness.Observe(construction, 1))[^1];
        var registry = new PartRegistry(); registry.Load();
        var root = new Godot.Node3D();
        var parts = instances.Select(instance =>
        {
            var part = registry.Create(PartId.Basketball); part.AuthoredId = instance.Id; return part;
        }).ToArray();
        var presentation = new WorkshopPhysicalPresentation();
        Assert.Equal(16, presentation.Capacity);
        try
        {
            godot.Tree.Root.AddChild(root);
            foreach (var part in parts) root.AddChild(part);
            foreach (var (count, capacity) in new[] { (12, 16), (13, 32), (25, 64), (12, 64) })
            {
                var before = parts.Select(part => part.Transform).ToArray();
                var poses = new PresentedBodySet(Enumerable.Range(0, count).Select(index =>
                {
                    var state = read.Bodies[index];
                    return new PresentedBody(state.Body.Id, state.Body.Cell, state.Body.Local, state.Rotation);
                }).ToArray());
                presentation.Apply(parts, root.GlobalTransform, poses);
                Assert.Equal(capacity, presentation.Capacity);
                for (var index = 0; index < count; index++)
                {
                    var body = read.Bodies[index].Body;
                    var expected = new Godot.Vector3((body.Cell.X + body.Local.X) / 16f,
                        (body.Cell.Y + body.Local.Y) / 16f, (body.Cell.Z + body.Local.Z) / 16f);
                    Assert.Equal(expected, parts[index].Position);
                }
                for (var index = count; index < parts.Length; index++) Assert.Equal(before[index], parts[index].Transform);
            }
            Assert.Equal(16, new WorkshopPhysicalPresentation().Capacity);
        }
        finally { root.Free(); }
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EveryDeclaredChildUsesItsCommittedMotionAndResetAndRefusesForeignSets(bool duplicate)
    {
        var template = TestBundle.Entry(PartId.Springboard);
        var shape = new ColliderDescriptor(CuriousContraptions.Abstractions.ColliderShapeKind.Box, new(.1f),
            Vector3.Zero, Quaternion.Identity, new(0, 0, 0, .1f), 0);
        var sliders = Enumerable.Range(0, 2).Select(index => new PrismaticSliderDescriptor(
            new(new(index == 0 ? -2 : 2, 0, 0), Quaternion.Identity), new(Vector3.Zero, Quaternion.Identity),
            -1, 1, MechanicalValue.Fixed(1f / 1024), MechanicalValue.Fixed(0), MechanicalValue.Fixed((index + 1) * .05f), false,
            new(MassFrameDeclaration.Constant(1, Vector3.Zero, Vector3.One, Quaternion.Identity, 0, Vector3.Zero), [shape]), null)).ToArray();
        var entry = template with
        {
            Metadata = template.Metadata with { Id = "native_multislider", Parameters = [] },
            Footprint = new(3, 2, 2), Colliders = [shape], ColliderBindings = [],
            Mechanical = template.Mechanical! with { Sliders = sliders },
            Presentation = new(1, PresentationAxes.None, [],
                [new("Visual/First", new(1), Vector3.Zero), new("Visual/Second", new(2), Vector3.Zero)], [], [])
        };
        Assert.Empty(BundleValidator.Validate([new("native", entry)]));
        var decoded = Assert.Single(BundleSchema.Read(BundleSchema.WriteEntries([entry])).Elements);
        Assert.Equal(new[] { new PackageBodyIndex(1), new PackageBodyIndex(2) }, decoded.Presentation.Bodies.Select(binding => binding.Body));
        foreach (var invalidBodies in new[]
        {
            new[] { entry.Presentation.Bodies[0] },
            new[] { entry.Presentation.Bodies[0], entry.Presentation.Bodies[1] with { Body = new(1) } },
            new[] { entry.Presentation.Bodies[0], entry.Presentation.Bodies[1] with { Body = new(3) } }
        })
            Assert.NotEmpty(BundleValidator.Validate([new("native", entry with
                { Presentation = entry.Presentation with { Bodies = invalidBodies } })]));
        var oldShape = System.Text.Json.Nodes.JsonNode.Parse(BundleSchema.WriteEntries([entry]))!;
        oldShape["Elements"]![0]!["Presentation"]!["Bodies"]![0]!["Body"] = "Secondary";
        Assert.Throws<System.Text.Json.JsonException>(() => BundleSchema.Read(oldShape.ToJsonString()));

        var partId = new PartId(entry.Metadata.Id);
        var owner = new WorkshopGenericInstance(partId, new(1), new(0, 48, 0), default, CanonicalRotation.Identity);
        var map = new CompiledIdentityMap([owner], new Dictionary<PartId, BundleEntry> { [partId] = entry });
        var rows = map[owner.Id];
        var bodies = new List<RigidBodyDeclaration>
        {
            new(owner.Id, RigidMotionKind.Static, owner.Cell, owner.Local, owner.Rotation, default, default, default, default, default, default)
        };
        var colliders = new List<ColliderDeclaration>
        {
            new(rows.Colliders[0], owner.Id, rows.Material, ColliderShapeKind.Box, RigidLocalPose.Identity, default, new(.1f,.1f,.1f))
        };
        var materials = new List<ContactMaterialDeclaration> { new(rows.Material, new(0), new(.1f), new(0), new(0)) };
        var joints = new List<PrismaticConstraintDeclaration>();
        for (var index = 0; index < sliders.Length; index++)
        {
            var slider = sliders[index]; var ids = rows.Sliders[index];
            var x = WorkshopInput.Position((float)(slider.BaseFrame.Position.X));
            var y = WorkshopInput.Position((float)(3 - slider.Compression.Constant));
            bodies.Add(new(ids.Body, RigidMotionKind.Dynamic, new(x.Cell, y.Cell, 0), new(x.Local, y.Local, 0),
                CanonicalRotation.Identity, new(0, index == 0 ? .2f : -.3f, 0), default, new(1), default, default,
                new(default, CanonicalRotation.Identity, new(1), new(1), new(1))));
            materials.Add(new(ids.Material, new(0), new(.1f), new(0), new(0)));
            colliders.Add(new(ids.Colliders[0], ids.Body, ids.Material, ColliderShapeKind.Box, RigidLocalPose.Identity, default, new(.1f,.1f,.1f)));
            joints.Add(new(ids.Joint, owner.Id, ids.Body, new(slider.BaseFrame.Position, Quaternion.Identity),
                new(Vector3.Zero, Quaternion.Identity), -1, 1, 1f / 1024, 0, ConnectedCollision.Disabled));
        }
        var scene = new PhysicsSceneDeclaration(new(1,2), map.NextIdentity, [..bodies], [..colliders], [..materials], [], [], prismatics:[..joints]);
        var physics = new LawWorld(scene, PhysicsGpuAbi.Admission(scene,new(1),new(SimulationCadence.Hz120,PhysicalStepProfile.Canonical480Hz,new(1))),SimulationCadence.Hz120);
        var root = new Godot.Node3D();
        var part = new MachinePart { AuthoredId = owner.Id, Position = new(0,3,0) };
        part.Configure(new(partId,"Native", "Native fixture",entry.Metadata.Category,new Godot.Color(.5f,.5f,.5f),entry.Metadata.IconPath,null,entry));
        var visual = new Godot.Node3D { Name = "Visual" }; part.AddChild(visual);
        var first = new Godot.Node3D { Name = "First", Position = new(-2,0,0) };
        var second = new Godot.Node3D { Name = "Second", Position = new(2,0,0) };
        visual.AddChild(first); visual.AddChild(second);
        try
        {
            godot.Tree.Root.AddChild(root); root.AddChild(part); part.EnsureConstructed();
            for (var i = 0; i < 2; i++) part.InstallSliderPresentation(new(i+1), rows.Sliders[i].Body, sliders[i].Compression.Constant, map);
            Assert.Throws<ArgumentException>(() => part.InstallPhysicalIdentity(new(1), rows.Sliders[1].Body, map));
            var presentation = new WorkshopPhysicalPresentation();
            var initialFirst = first.Transform; var initialSecond = second.Transform;
            var state = physics.Step(30);
            var poses = new PresentedBodySet(rows.Sliders.Select(ids =>
            {
                var body = state[ids.Body]; var q = body.Rotation;
                return new PresentedBody(ids.Body,body.Cell,body.Local,new(q.X,q.Y,q.Z,q.W));
            }).ToArray());
            presentation.Apply([part],root.GlobalTransform,poses);
            // k=1/1024 N/m, mass1 and |offset|<.2 bound spring displacement
            // over .25s below7 micrometres; the1mm motion interval is independent.
            Assert.InRange(first.Position.Y - initialFirst.Origin.Y, .049f,.051f);
            Assert.InRange(second.Position.Y - initialSecond.Origin.Y,-.076f,-.074f);
            var beforeFirst = first.Transform; var beforeSecond = second.Transform;
            var bad = new PresentedBodySet([poses[0] with { Local = new(0,0,0) },
                poses[1] with { Id = duplicate ? poses[1].Id : new(9999) }]);
            Assert.Throws<ArgumentException>(() => presentation.Apply(duplicate ? [part,part] : [part],root.GlobalTransform,bad));
            Assert.Equal(beforeFirst,first.Transform); Assert.Equal(beforeSecond,second.Transform);
            part.ResetPhysicalVisuals();
            Assert.Equal(initialFirst,first.Transform); Assert.Equal(initialSecond,second.Transform);
            Assert.Same(first,part.PhysicalVisual(rows.Sliders[0].Body));
            Assert.Same(second,part.PhysicalVisual(rows.Sliders[1].Body));
        }
        finally { root.Free(); }
    }
}
