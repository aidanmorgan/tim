using System;

namespace CuriousContraptions.Physics;

public enum PhysicsJointChangeKind { Attach, Replace, Detach }

/// <summary>One explicitly addressed constraint edit. Attach requires a fresh
/// identity; Replace and Detach require an existing identity. No upsert semantics.</summary>
public readonly record struct PhysicsJointChange
{
    public PhysicsJointChangeKind Kind { get; }
    public PhysicsJointId Id { get; }
    public PhysicsJoint? Declaration { get; }
    private PhysicsJointChange(PhysicsJointChangeKind kind,PhysicsJointId id,PhysicsJoint? declaration)
    {
        Kind=kind; Id=id; Declaration=declaration;
    }
    public static PhysicsJointChange Attach(PhysicsJoint joint)
    {
        ArgumentNullException.ThrowIfNull(joint);
        return new(PhysicsJointChangeKind.Attach,joint.Id,joint);
    }
    public static PhysicsJointChange Replace(PhysicsJoint joint)
    {
        ArgumentNullException.ThrowIfNull(joint);
        return new(PhysicsJointChangeKind.Replace,joint.Id,joint);
    }
    public static PhysicsJointChange Detach(PhysicsJointId joint)=>new(PhysicsJointChangeKind.Detach,joint,null);
}
