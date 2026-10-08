using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

/// <summary>Immutable effort law on an owned axial joint. The shared predictor
/// evaluates it on current stage state and applies the travel Jacobian transpose.</summary>
public abstract record AxialEffortLoad
{
    public PhysicsJointId Joint { get; }
    public FrameJointKind Kind { get; }
    protected AxialEffortLoad(PhysicsJointId joint,FrameJointKind kind)
    {
        if(kind is not (FrameJointKind.Slider or FrameJointKind.Hinge))
            throw new ArgumentOutOfRangeException(nameof(kind));
        Joint=joint;Kind=kind;
    }
    public PhysicsFrameJoint Resolve(IEnumerable<PhysicsJoint> joints)
    {
        ArgumentNullException.ThrowIfNull(joints);
        if(joints.SingleOrDefault(j=>j.Id==Joint) is not PhysicsFrameJoint frame||frame.Kind!=Kind)
            throw new ArgumentException("Effort load requires its current owned axial joint.");
        return frame;
    }
    public virtual void Validate(PhysicsFrameJoint joint,IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsColliderUpdate> colliders)
    {
        ArgumentNullException.ThrowIfNull(joint);ArgumentNullException.ThrowIfNull(bodies);ArgumentNullException.ThrowIfNull(colliders);
        if(joint.Id!=Joint||joint.Kind!=Kind||!bodies.TryGetValue(joint.A.Id,out var a)||a!=joint.A||
            !bodies.TryGetValue(joint.B.Id,out var b)||b!=joint.B)
            throw new ArgumentException("Effort law must bind to the sampled joint and body states.");
    }
    /// <summary>Discontinuity horizon for this effort law on captured motion.</summary>
    public abstract ScalarSweepResult Sweep(PhysicsFrameJoint joint,IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsColliderUpdate> colliders,
        IReadOnlyDictionary<PhysicsBodyId,BodyTrajectory> paths,double duration,double tolerance);
    protected void ValidateSweep(PhysicsFrameJoint joint,IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsColliderUpdate> colliders,
        IReadOnlyDictionary<PhysicsBodyId,BodyTrajectory> paths,double duration,double tolerance)
    {
        Validate(joint,bodies,colliders);ArgumentNullException.ThrowIfNull(paths);
        if(!double.IsFinite(duration)||duration<0||!double.IsFinite(tolerance)||tolerance<=0)
            throw new ArgumentOutOfRangeException(nameof(duration));
        foreach(var body in joint.Bodies)
        {
            if(!paths.TryGetValue(body.Id,out var path)||path is null)
                throw new ArgumentException("Effort sweep requires current participant trajectories.",nameof(paths));
            path.ValidateSource(body);
            if(duration>path.Duration)throw new ArgumentOutOfRangeException(nameof(duration));
        }
    }
    public abstract double Evaluate(PhysicsFrameJoint joint,IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsColliderUpdate> colliders);
}
