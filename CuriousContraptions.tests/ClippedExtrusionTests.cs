using System.Numerics;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class ClippedExtrusionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CommittedExposureBlocksTheInitiallyClearSegment(bool reversed)
    {
        var (world, rod, _, moving) = Fixture(reverse: reversed);
        var initial = PhysicsGpuAbi.ReadBoxGeometry(world.Admitted, rod);
        Assert.Null(initial.IntersectSegment(new(-.3f, 5.3f, 0), new(.3f, 5.3f, 0)));
        var extended = world.Step(180, supplied: true);
        var shape = PhysicsGpuAbi.ReadBoxGeometry(extended.World, rod);
        var hit = Assert.IsType<BoxSegmentHit>(shape.IntersectSegment(new(-.3f, 5.3f, 0), new(.3f, 5.3f, 0)));
        Assert.InRange(hit.Distance, .2399f, .2401f); // Defined segment distance from -.3 m to the -.06 m face.
        Assert.Equal(rod, hit.Collider);
        Assert.Equal(moving, hit.PhysicalOwner);
        Assert.Equal(BoxFace.NegativeX, hit.Face);
        Assert.InRange(hit.Point.X, -.0601f, -.0599f); // Local box-slab intersection contract at declared 6 cm half-width.
        Assert.InRange(shape.Centre.Y - shape.HalfExtents.Y, 4.7499f, 4.7501f); // The cut stays fixed while material advances.
    }

    [Fact]
    public void ClosureUsesClippingOwnerAndInsideStartFindsTheForwardExit()
    {
        var (world, rod, frame, moving) = Fixture();
        var shape = PhysicsGpuAbi.ReadBoxGeometry(world.Admitted, rod);
        var proximal = Assert.IsType<BoxSegmentHit>(shape.IntersectSegment(new(0, 4.5f, 0), new(0, 5.2f, 0)));
        Assert.Equal(BoxFace.NegativeY, proximal.Face);
        Assert.Equal(frame, proximal.PhysicalOwner);
        var distal = Assert.IsType<BoxSegmentHit>(shape.IntersectSegment(new(0, 4.9f, 0), new(0, 5.2f, 0)));
        Assert.Equal(BoxFace.PositiveY, distal.Face);
        Assert.Equal(moving, distal.PhysicalOwner);
        Assert.InRange(distal.Point.Y, 4.9999f, 5.0001f);
        Assert.Null(shape.IntersectSegment(new(.2f, 4.9f, 0), new(.2f, 5.2f, 0)));
        var boundary = Assert.IsType<BoxSegmentHit>(shape.IntersectSegment(new(-.06f, 4.9f, 0), new(-.2f, 4.9f, 0)));
        Assert.Equal(0f, boundary.Fraction);
    }

    [Fact]
    public void OrdinaryBoxCornerTieUsesTheExactColliderMaterialAndFirstAxis()
    {
        var source = new LawScene(false);
        source.StaticBox(new(0, 5, 0), new(.5f));
        var selected = source.StaticBox(new(3, 5, 0), new(.5f));
        var world = source.Admit();
        var declaration = world.Scene.Colliders.ToArray().Single(c => c.Body == selected);
        var shape = PhysicsGpuAbi.ReadBoxGeometry(world.Step(1).World, declaration.Id);
        var hit = Assert.IsType<BoxSegmentHit>(shape.IntersectSegment(new(2, 4, 0), new(3, 5, 0)));
        Assert.Equal(declaration.Id, hit.Collider);
        Assert.Equal(declaration.Material, hit.Material);
        Assert.Equal(selected, hit.PhysicalOwner);
        Assert.Equal(BoxFace.NegativeX, hit.Face);
        Assert.Equal(-Vector3.UnitX, hit.Normal);
        Assert.Equal(.5f, hit.Fraction); // Simultaneous half-way intersections choose X before Y.
        Assert.InRange(hit.Distance, .7070f, .7072f); // sqrt(.5²+.5²) metres, local slab contract.
    }

    [Fact]
    public void InvalidQueryInputsRefuseWithTypedReasons()
    {
        GpuBodyId sphere = default;
        var (world, rod, _, _) = Fixture(source => sphere = source.Sphere(new(4, 5, 0), .1f, 1f,
            LawMaterial.Domino, Vector3.Zero, Vector3.Zero, 0f, 0f));
        var shape = PhysicsGpuAbi.ReadBoxGeometry(world.Admitted, rod);
        var sphereCollider = world.Scene.Colliders.ToArray().Single(c => c.Body == sphere).Id;
        Assert.Equal(BoxQueryRefusal.NonBoxCollider, Assert.Throws<BoxQueryException>(
            () => PhysicsGpuAbi.ReadBoxGeometry(world.Admitted, sphereCollider)).Reason);
        Assert.Equal(BoxQueryRefusal.UnknownCollider, Assert.Throws<BoxQueryException>(
            () => PhysicsGpuAbi.ReadBoxGeometry(world.Admitted, new(99999))).Reason);
        Assert.Equal(BoxQueryRefusal.ZeroLengthSegment, Assert.Throws<BoxQueryException>(
            () => shape.IntersectSegment(Vector3.Zero, Vector3.Zero)).Reason);
        Assert.Equal(BoxQueryRefusal.NonFiniteSegment, Assert.Throws<BoxQueryException>(
            () => shape.IntersectSegment(new(float.NaN, 0, 0), Vector3.Zero)).Reason);
    }

    [Theory]
    [InlineData(0f, true)]
    [InlineData(.4f, false)]
    public void ExposedMidspanPhysicallyBlocksAnApproachingBodyButAdjacentSpaceIsClear(float z, bool hits)
    {
        GpuBodyId cargo = default;
        var (world, _, _, _) = Fixture(source =>
            cargo = source.Sphere(new(.4f, 5.3f, z), .1f, .25f, new(0f, .1f, .3f, 0f),
                new(-.3f, 0, 0), Vector3.Zero, 0f, 0f));
        var final = world.Step(240, supplied: true);
        if (hits)
        {
            Assert.True(final[cargo].Position.X >= .14f, "The exposed 6 cm rod plus 10 cm sphere must block crossing, allowing 2 cm contact clearance.");
            Assert.True(final[cargo].Velocity.X > -.05f, "The physical shaft must stop inward motion.");
        }
        else Assert.True(final[cargo].Position.X < -.15f, "An adjacent path must remain open.");
    }

    [Fact]
    public void LateralFrictionUsesRigidMaterialSpeedRatherThanExposureStretchSpeed()
    {
        static float Run(float friction)
        {
            GpuBodyId cargo = default;
            var (world, _, _, _) = Fixture(source =>
                cargo = source.Sphere(new(.4f, 5.2f, 0f), .1f, .25f, new(0f, .1f, friction, 0f),
                    new(-.3f, .35f, 0f), Vector3.Zero, 0f, 0f), initialCoordinate: .8f);
            return world.Step(240, supplied: true)[cargo].Position.Y;
        }
        var frictionless = Run(0f);
        var frictional = Run(.8f);
        // The probe already moves faster than a stretching midpoint but slower than the .5 m/s rigid material.
        Assert.True(frictional > frictionless + .02f, "Shaft friction must accelerate the touching probe with its full material motion.");
    }

    [Fact]
    public void ExposedProximalCapRemainsWithTheClippingSupport()
    {
        GpuBodyId cargo = default;
        var (world, _, _, _) = Fixture(source =>
            cargo = source.Sphere(new(0f, 4.35f, 0f), .1f, .25f, new(0f, .1f, 0f, 0f),
                new(0f, .3f, 0f), Vector3.Zero, 0f, 0f));
        var final = world.Step(240, supplied: true)[cargo];
        Assert.InRange(final.Position.Y, 4.63f, 4.67f); // Fixed cut 4.75 m minus probe radius, 2 cm physical contact allowance.
        Assert.InRange(MathF.Abs(final.Velocity.Y), 0f, .02f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RetractionRemovesBothMidspanContactAndCommittedQuery(bool retracting)
    {
        var source = new LawScene(false);
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -MathF.PI / 2f);
        var support = source.StaticBox(new(0, 6, 0), new(.1f));
        var moving = source.Box(new(1, 7, 0), new(.1f), .5f, new(0f, .1f, 0f, 0f),
            velocity: retracting ? new(-.5f, 0, 0) : Vector3.Zero, gravity: 0f);
        var coordinate = source.Prismatic(support, moving, new(new(0, -1, 0), rotation), new(new(0, -2, 0), rotation),
            0f, 2f, 0f, 0f, ConnectedCollision.Disabled);
        // The material body's primary collider stays above the probe, so only shaft withdrawal can release it.
        var rod = source.ChildCollider(moving, ColliderShapeKind.Box, new(-.125f, -2, 0), 0f,
            new(.06f, .125f, .06f), rotation);
        var cargo = source.Sphere(new(.5f, 5.3f, 0f), .1f, .25f, new(0f, .1f, 0f, 0f),
            Vector3.Zero, Vector3.Zero, 0f, -9.81f);
        var original = source.Admit().Scene;
        var colliders = original.Colliders.ToArray();
        var index = Array.FindIndex(colliders, item => item.Id == rod);
        colliders[index] = colliders[index] with { Extrusion = new(coordinate, moving, support, -.25f, 0f, new(.06f)) };
        var scene = new PhysicsSceneDeclaration(original.Document, 1000, original.Bodies, colliders, original.Materials, [], [],
            prismatics: original.Prismatics);
        var admitted = PhysicsGpuAbi.Admission(scene, new(1),
            new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        Assert.NotNull(PhysicsGpuAbi.ReadBoxGeometry(admitted, rod).IntersectSegment(new(.5f, 4.5f, 0), new(.5f, 5.5f, 0)));
        var withdrew = false;
        var released = false;
        var final = new LawWorld(scene, admitted, SimulationCadence.Hz120).Step(600, (_, state) =>
        {
            if (!retracting) return;
            var shape = PhysicsGpuAbi.ReadBoxGeometry(state.World, rod);
            var distal = shape.Centre + Vector3.Transform(Vector3.UnitY * shape.HalfExtents.Y, shape.Rotation);
            if (distal.X < .35f)
            {
                withdrew = true;
                Assert.Null(shape.IntersectSegment(new(.5f, 4.5f, 0), new(.5f, 5.5f, 0)));
            }
            released |= withdrew && state[cargo].Position.Y < 4.5f;
        });
        var hit = PhysicsGpuAbi.ReadBoxGeometry(final.World, rod).IntersectSegment(new(.5f, 4.5f, 0), new(.5f, 5.5f, 0));
        if (retracting)
        {
            Assert.True(withdrew, "The free carrier must actually withdraw its exposure from the probe.");
            Assert.True(released, "Withdrawal must release previously supported cargo; later free-carrier rebound is not stale geometry.");
        }
        else
        {
            Assert.NotNull(hit);
            var shape = PhysicsGpuAbi.ReadBoxGeometry(final.World, rod);
            var surface = Assert.IsType<BoxSegmentHit>(shape.IntersectSegment(
                new(final[cargo].Position.X, 5.5f, 0), new(final[cargo].Position.X, 4.5f, 0)));
            Assert.InRange(surface.Point.Y + .1f - final[cargo].Position.Y, -.0005f, .0005f); // Documented 0.5 mm resting penetration after a five-second settling guard.
        }
    }

    [Fact]
    public void RotatedMovingSupportCarriesTheExactClippingFaceAndItsOwner()
    {
        var (original, rod, frame, moving) = Fixture();
        var rotation = Quaternion.CreateFromYawPitchRoll(.4f, -.3f, .7f);
        var bodies = original.Scene.Bodies.ToArray().Select(body =>
        {
            var position = original.Initial(body.Id).Position;
            position = Vector3.Transform(position, rotation) + new Vector3(3, -1, 2);
            var x = WorkshopInput.Position(position.X);
            var y = WorkshopInput.Position(position.Y);
            var z = WorkshopInput.Position(position.Z);
            var changed = body with { Cell = new(x.Cell, y.Cell, z.Cell), Local = new(x.Local, y.Local, z.Local),
                Rotation = CanonicalRotation.Normalize(rotation.X, rotation.Y, rotation.Z, rotation.W), Velocity = new(.3f, 0, 0) };
            return body.Id == frame ? changed with { Motion = RigidMotionKind.Dynamic, Mass = new(2f),
                MassProperties = MassFixtures.Primitive(ColliderShapeKind.Box, 2f, 0f, new(.1f), Vector3.Zero) } : changed;
        }).ToArray();
        var source = original.Scene;
        var scene = new PhysicsSceneDeclaration(source.Document, source.NextIdentity, bodies, source.Colliders, source.Materials, [], [],
            prismatics: source.Prismatics, prismaticDrives: source.PrismaticDrives,
            supplyNodes: source.SupplyGraph.Nodes, supplyEdges: source.SupplyGraph.Edges);
        var admitted = PhysicsGpuAbi.Admission(scene, new(1),
            new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        var world = new LawWorld(scene, admitted, SimulationCadence.Hz120);
        var final = world.Step(120, supplied: true);
        var support = final[frame];
        var cap = support.Position + Vector3.Transform(new(-1, -.25f, 0), support.Rotation);
        var axis = Vector3.Transform(Vector3.UnitY, support.Rotation);
        var shape = PhysicsGpuAbi.ReadBoxGeometry(final.World, rod);
        var hit = Assert.IsType<BoxSegmentHit>(shape.IntersectSegment(cap - axis * .2f, cap + axis * .2f));
        Assert.Equal(frame, hit.PhysicalOwner);
        Assert.Equal(moving, shape.MaterialOwner);
        Assert.Equal(BoxFace.NegativeY, hit.Face);
        Assert.InRange(Vector3.Distance(hit.Point, cap), 0f, .001f); // A moved/rotated joint-frame cut must stay on its physical support.
        Assert.True(Vector3.Distance(support.Position, world.Initial(frame).Position) > .1f);
    }

    [Fact]
    public void InvalidExposureReferencesFramesAndDomainRefuseAtCompiledAdmission()
    {
        var (world, rod, _, _) = Fixture();
        var source = world.Scene;
        var original = source.Colliders.ToArray().Single(collider => collider.Id == rod);
        var extrusion = original.Extrusion!.Value;
        ColliderDeclaration[] invalid =
        [
            original with { Extrusion = extrusion with { Coordinate = new(9999) } },
            original with { Extrusion = extrusion with { ClippingBody = extrusion.MaterialBody } },
            original with { Extrusion = extrusion with { Cut = 0f } },
            original with { Extrusion = extrusion with { Cut = .1f } },
            original with { Pose = RigidLocalPose.Identity }
        ];
        foreach (var changed in invalid)
            Assert.Throws<ArgumentException>(() => new PhysicsSceneDeclaration(source.Document, source.NextIdentity,
                source.Bodies, source.Colliders.ToArray().Select(collider => collider.Id == rod ? changed : collider).ToArray(),
                source.Materials, [], [], prismatics: source.Prismatics, prismaticDrives: source.PrismaticDrives,
                supplyNodes: source.SupplyGraph.Nodes, supplyEdges: source.SupplyGraph.Edges));
    }

    private static (LawWorld World, GpuColliderId Rod, GpuBodyId Frame, GpuBodyId Moving) Fixture(Action<LawScene>? add = null,
        float initialCoordinate = 0f, bool reverse = false)
    {
        var source = new LawScene(false);
        var frame = source.StaticBox(new(1, 5, 0), new(.1f));
        var moving = source.Box(new(0, 5 + initialCoordinate, 0), new(.1f), .5f, LawMaterial.Domino, gravity: 0f);
        var joint = source.Prismatic(frame, moving, new(new(-1, 0, 0), Quaternion.Identity), ConstraintFrame.Identity,
            0f, 2f, 0f, 0f, ConnectedCollision.Disabled);
        source.Drive(joint, 1f + initialCoordinate, .5f, 2f, 20f, 10f);
        var rod = source.ChildCollider(moving, ColliderShapeKind.Box, new(0, -.125f, 0), 0f, new(.06f, .125f, .06f));
        add?.Invoke(source);
        var original = source.Admit().Scene;
        var colliders = original.Colliders.ToArray();
        var index = Array.FindIndex(colliders, collider => collider.Id == rod);
        colliders[index] = colliders[index] with { Extrusion = new(joint, moving, frame, -.25f, 0f, new(.06f)) };
        if (reverse) Array.Reverse(colliders);
        var scene = new PhysicsSceneDeclaration(original.Document, 1000, original.Bodies, colliders, original.Materials, [], [],
            prismatics: original.Prismatics, prismaticDrives: original.PrismaticDrives,
            supplyNodes: original.SupplyGraph.Nodes, supplyEdges: original.SupplyGraph.Edges);
        return (new(scene, PhysicsGpuAbi.Admission(scene, new(1),
            new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1))), SimulationCadence.Hz120), rod, frame, moving);
    }
}
