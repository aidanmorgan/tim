using System;
using System.Collections.Generic;

namespace CuriousContraptions.Physics;

/// <summary>An externally supplied constant generalized force or torque.</summary>
public sealed record ConstantAxialEffortLoad : AxialEffortLoad
{
    public double Effort { get; }
    public ConstantAxialEffortLoad(PhysicsJointId joint,FrameJointKind kind,double effort):base(joint,kind)
    {
        if(!double.IsFinite(effort))throw new ArgumentOutOfRangeException(nameof(effort));
        Effort=effort;
    }
    public override ScalarSweepResult Sweep(PhysicsFrameJoint joint,IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsColliderUpdate> colliders,
        IReadOnlyDictionary<PhysicsBodyId,BodyTrajectory> paths,double duration,double tolerance)
    {
        ValidateSweep(joint,bodies,colliders,paths,duration,tolerance);
        return new(ScalarSweepStatus.Clear,duration,0);
    }
    public override double Evaluate(PhysicsFrameJoint joint,IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsColliderUpdate> colliders)
    {
        Validate(joint,bodies,colliders);
        return Effort;
    }
}
