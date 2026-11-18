using System.Numerics;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>Independent mechanical budget for the spring fixtures: all moving masses, principal inertia,
/// elastic stores and gravitational displacement. Contact exchange is internal and is not added again.</summary>
internal static class SpringTestEnergy
{
    public static float Measure(IPhysicsSceneDeclaration scene, WorkshopRead read, WorkshopRead origin)
    {
        var energy = scene.Prismatics.ToArray().Sum(joint =>
        {
            var q = Coordinate(scene, read, joint);
            return .5f * joint.Stiffness * q * q;
        });
        foreach (var declaration in scene.Bodies)
        {
            if (declaration.Motion != RigidMotionKind.Dynamic) continue;
            Assert.True(read.Bodies.TryGet(declaration.Id, out var body));
            Assert.True(origin.Bodies.TryGet(declaration.Id, out var start));
            var velocity = body.Body.Velocity;
            var spin = body.AngularVelocity;
            var principal = declaration.MassProperties.PrincipalFrame;
            var frame = Quaternion.Normalize(Rotation(body) * new Quaternion(principal.X, principal.Y, principal.Z, principal.W));
            var omega = Vector3.Transform(new Vector3(spin.X, spin.Y, spin.Z), Quaternion.Conjugate(frame));
            var inertia = declaration.MassProperties;
            energy += .5f * declaration.Mass.Value * (velocity.X * velocity.X + velocity.Y * velocity.Y + velocity.Z * velocity.Z)
                + .5f * (inertia.X.Moment * omega.X * omega.X + inertia.Y.Moment * omega.Y * omega.Y + inertia.Z.Moment * omega.Z * omega.Z);
            var displacement = Centre(body) - Centre(start);
            energy -= declaration.Mass.Value * Vector3.Dot(new(declaration.Gravity.X, declaration.Gravity.Y, declaration.Gravity.Z), displacement);
        }
        return energy;
    }

    public static float SourceWork(WorkshopIntegrationHarness harness)
    {
        var work = 0f;
        for (var i = 0; i < harness.Simulation.Scene.Physics.Revolutes.Length; i++)
            work += PhysicsGpuAbi.ReadRevoluteState(harness.Device.CommittedWorld.Span, i).Work;
        return work;
    }

    public static float Allowance(IPhysicsSceneDeclaration scene) =>
        // A fixed one-centimetre equivalent rise across the moving population; never accumulated per tick.
        // This is at most 1% of the required metre-scale launch and excludes an extra useful launch impulse.
        .005f + scene.Bodies.ToArray().Where(b => b.Motion == RigidMotionKind.Dynamic).Sum(b => b.Mass.Value) * 9.81f * .01f;

    public static float Coordinate(IPhysicsSceneDeclaration scene, WorkshopRead read, PrismaticConstraintDeclaration joint)
    {
        var a = Pose(scene, read, joint.BodyA);
        var b = Pose(scene, read, joint.BodyB);
        var anchorA = a.Position + Vector3.Transform(joint.FrameA.Position, a.Rotation);
        var anchorB = b.Position + Vector3.Transform(joint.FrameB.Position, b.Rotation);
        var axis = Vector3.Transform(Vector3.UnitY, a.Rotation * joint.FrameA.Rotation);
        return Vector3.Dot(anchorB - anchorA, axis);
    }

    /// <summary>A dynamic body's pose is its committed read and must be present; only a static frame, which never moves, uses its
    /// authored pose.</summary>
    private static (Vector3 Position, Quaternion Rotation) Pose(IPhysicsSceneDeclaration scene, WorkshopRead read, GpuBodyId id)
    {
        var declaration = scene.Bodies.ToArray().Single(b => b.Id == id);
        if (declaration.Motion == RigidMotionKind.Dynamic)
        {
            Assert.True(read.Bodies.TryGet(id, out var body), $"Dynamic body {id.Value} is missing from the committed read.");
            return (new((body.Body.Cell.X + body.Body.Local.X) / 16f, (body.Body.Cell.Y + body.Body.Local.Y) / 16f,
                (body.Body.Cell.Z + body.Body.Local.Z) / 16f), Rotation(body));
        }
        return (new((declaration.Cell.X + declaration.Local.X) / 16f, (declaration.Cell.Y + declaration.Local.Y) / 16f,
            (declaration.Cell.Z + declaration.Local.Z) / 16f), Quaternion.Normalize(new(declaration.Rotation.X,
                declaration.Rotation.Y, declaration.Rotation.Z, declaration.Rotation.W)));
    }

    private static Quaternion Rotation(PhysicsBodyRead body) =>
        Quaternion.Normalize(new(body.Rotation.X, body.Rotation.Y, body.Rotation.Z, body.Rotation.W));
    private static Vector3 Centre(PhysicsBodyRead body) => new Vector3(
        (body.Body.Cell.X + body.Body.Local.X) / 16f, (body.Body.Cell.Y + body.Body.Local.Y) / 16f,
        (body.Body.Cell.Z + body.Body.Local.Z) / 16f) +
        Vector3.Transform(new Vector3(body.LocalCentreOfMass.X, body.LocalCentreOfMass.Y, body.LocalCentreOfMass.Z), Rotation(body));
}
