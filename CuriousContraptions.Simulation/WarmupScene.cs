using System;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

/// <summary>Story 9A.21: the worker's one synthetic, declaration-free warm-up scene. A dynamic sphere rides a prismatic on a static
/// box (their contact enabled) that also carries a contact trigger and a contact-work store; the prismatic has a supplied drive.
/// Every value is arbitrary and is no element's: the scene is admitted through <see cref="PhysicsGpuAbi.Admission"/> and stepped by
/// the worker engine, its drive supplied for the first steps and unsupplied after, so the solver's contact, prismatic, drive,
/// event and contact-work paths run before the first Run. The per-element compile path is not warmed by it.</summary>
internal static class WarmupScene
{
    // Long enough for the driven rider to rise and then strike the base above the trigger and contact-work thresholds.
    public const int SuppliedSteps = 24;
    public const int Steps = 48;
    public static readonly GpuBodyId Base = new(1), Rider = new(2);
    public static readonly PrismaticConstraintId Joint = new(7);
    public const float Lower = -.5f, Upper = .75f, Stiffness = 37f, Damping = 1.5f;
    public const float Target = .5f, MaxSpeed = 1.25f, StoppingAcceleration = 9f, MaxEffort = 22f, MaxPower = 18f;
    private const float RiderRadius = .25f, RiderMass = 1.5f, BaseHalf = .5f, BaseHeight = 2f;
    public static readonly WorkshopGpuProfile Profile = new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1));

    public static PhysicsSceneDeclaration Declaration()
    {
        var gravity = -(float)WorkshopConstruction.Gravity.Value;
        var baseY = WorkshopInput.Position(BaseHeight);
        var riderY = WorkshopInput.Position(BaseHeight + BaseHalf + RiderRadius);
        RigidBodyDeclaration[] bodies =
        [
            new(Base, RigidMotionKind.Static, new(0, baseY.Cell, 0), new(0f, baseY.Local, 0f), CanonicalRotation.Identity,
                default, default, new((Half)0), default, new((Half)0)),
            new(Rider, RigidMotionKind.Dynamic, new(0, riderY.Cell, 0), new(0f, riderY.Local, 0f), CanonicalRotation.Identity,
                default, default, new((Half)RiderMass), new((Half)0, (Half)gravity, (Half)0), new((Half)0)),
        ];
        ContactMaterialDeclaration[] materials =
        [
            new(new(3), new((Half).25f), new((Half).5f), new((Half).5f), new((Half)0)),
            new(new(4), new((Half).25f), new((Half).5f), new((Half).5f), new((Half).0625f)),
        ];
        ColliderDeclaration[] colliders =
        [
            new(new(5), Base, new(3), ColliderShapeKind.Box, RigidLocalPose.Identity, new((Half)0),
                new((Half)BaseHalf, (Half)BaseHalf, (Half)BaseHalf)),
            new(new(6), Rider, new(4), ColliderShapeKind.Sphere, RigidLocalPose.Identity, new((Half)RiderRadius), default),
        ];
        var targets = new BodyTargetSet(BodyTargetKind.AllDynamic, default);
        PrismaticConstraintDeclaration[] prismatics =
        [
            new(Joint, Base, Rider, new(new(0f, BaseHalf + RiderRadius, 0f), System.Numerics.Quaternion.Identity), ConstraintFrame.Identity,
                Lower, Upper, Stiffness, Damping, ConnectedCollision.Enabled),
        ];
        return new(new(1, 2), 10, bodies, colliders, materials, [], [],
            triggers: [new(new(8), Base, targets, new(.5f))],
            contactWorks: [new(new(9), Base, targets, new(3f), new(6.75f), new(.5f), 30)],
            prismatics: prismatics,
            prismaticDrives: [new(Joint, Target, MaxSpeed, StoppingAcceleration, MaxEffort, MaxPower)]);
    }

    /// <summary>The inputs of warm-up step <paramref name="step"/>: no sources; the drive supplied for the first steps only.</summary>
    public static PhysicsTickInputs Inputs(int step) => new([], [step < SuppliedSteps]);
}
