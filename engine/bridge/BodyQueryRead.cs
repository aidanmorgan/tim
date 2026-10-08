using System;
using CuriousContraptions.Physics;
namespace CuriousContraptions.Bridge;

/// <summary>Immutable query declaration retained with one committed body pose.
/// Geometry versions are immutable replacement values, never scene-node captures.</summary>
public readonly record struct BodyQueryRead
{
    public PhysicsBodyId Owner { get; }
    public PhysicsColliderRevision Revision { get; }
    public CollisionParticipation Participation { get; }
    public CompoundGeometry Solid { get; }
    public CompoundGeometry? Opaque { get; }
    public BodyQueryRead(PhysicsBodyId owner,PhysicsColliderRevision revision,
        CollisionParticipation participation,CompoundGeometry solid,CompoundGeometry? opaque)
    {
        ArgumentNullException.ThrowIfNull(solid);
        if(!Enum.IsDefined(participation))throw new ArgumentOutOfRangeException(nameof(participation));
        Owner=owner;Revision=revision;Participation=participation;Solid=solid;Opaque=opaque;
    }
    internal void Validate()
    {
        if(Solid is null||!Enum.IsDefined(Participation))
            throw new ArgumentException("Invalid committed query declaration.");
    }
}

public enum OwnerActivity { Inactive, Active }

/// <summary>World-frame velocity at the body centre, in metres/second and radians/second.</summary>
public readonly record struct BodyVelocityRead(CollisionVector LinearMetresPerSecond,CollisionVector AngularRadiansPerSecond)
{
    internal void Validate()
    {
        if(!LinearMetresPerSecond.IsFinite||!AngularRadiansPerSecond.IsFinite)
            throw new ArgumentException("Committed velocities must be finite.");
    }
}

/// <summary>Pose, query, activity and velocity from one successful transaction.</summary>
public readonly record struct BodyPublicationRead(BodyPoseRead Pose,BodyQueryRead Query,OwnerActivity Activity,BodyVelocityRead Velocity);
