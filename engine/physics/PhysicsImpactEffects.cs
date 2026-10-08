using System;

namespace CuriousContraptions.Physics;

/// <summary>Immutable application-owned state; Restore must accept every state
/// returned by Capture without throwing or producing external side effects.</summary>
public abstract record PhysicsImpactEffectState;

public readonly record struct PhysicsImpactBody(PhysicsBodySnapshot Before,
    PhysicsBodySnapshot After,double InverseMass,CollisionVector AngularVelocity)
{
    public CollisionVector PointVelocity(CollisionVector point)
    {
        if(!point.IsFinite)throw new ArgumentException("Impact sample point must be finite.",nameof(point));
        return After.LinearVelocity+CollisionVector.Cross(AngularVelocity,point-After.Pose.Center);
    }
}
public readonly record struct PhysicsImpactContext(PhysicsImpact Impact,
    PhysicsImpactBody A,PhysicsImpactBody B,double ApproachSpeed);

/// <summary>An explicit external impulse, applied after ordinary contact response.
/// The command never changes pose or bypasses the next contact/constraint solve.</summary>
public readonly record struct PhysicsImpactImpulse
{
    public PhysicsBodyId Body { get; }
    public CollisionVector Impulse { get; }
    public CollisionVector Point { get; }
    public PhysicsImpactImpulse(PhysicsBodyId body,CollisionVector impulse,CollisionVector point)
    {
        if(!impulse.IsFinite||!point.IsFinite) throw new ArgumentException("Impact commands must be finite.");
        Body=body; Impulse=impulse; Point=point;
    }
}

/// <summary>Immutable scheduled commands. Collider changes are committed by the
/// world together with joint changes after the coupled contact response, never during its solve.</summary>
public sealed class PhysicsImpactCommands
{
    private readonly PhysicsImpactImpulse[] _impulses;
    private readonly PhysicsColliderUpdate[] _colliders;
    private readonly PhysicsJointChange[] _joints;
    public ReadOnlySpan<PhysicsImpactImpulse> Impulses=>_impulses;
    public ReadOnlySpan<PhysicsColliderUpdate> Colliders=>_colliders;
    public ReadOnlySpan<PhysicsJointChange> Joints=>_joints;
    public PhysicsImpactCommands(ReadOnlySpan<PhysicsImpactImpulse> impulses,ReadOnlySpan<PhysicsColliderUpdate> colliders,
        ReadOnlySpan<PhysicsJointChange> joints)
    {
        _impulses=impulses.ToArray(); _colliders=colliders.ToArray(); _joints=joints.ToArray();
    }
}

/// <summary>One component's impact behaviour, subscribed to its world-owned body.
/// Implementations receive values, not mutable physics bodies. Capture/Restore
/// must include cooldowns, counters and other simulation state; presentation and
/// external publication must wait until the enclosing step succeeds.</summary>
public abstract class PhysicsImpactEffect
{
    public PhysicsBodyId Body { get; }
    protected PhysicsImpactEffect(PhysicsBodyId body) { Body=body; }
    public abstract PhysicsImpactEffectState Capture();
    public abstract void Restore(PhysicsImpactEffectState state);
    public abstract PhysicsImpactCommands OnImpact(PhysicsImpactContext context);
}
