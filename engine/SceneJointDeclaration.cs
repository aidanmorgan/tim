using System;
using System.Collections.Generic;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

/// <summary>Extensible component-owned joint identity; declare once per slot,
/// scope by owner, and never serialize reference identity.</summary>
public sealed class JointSlot { }

public readonly record struct SceneBodyKey
{
    public MachinePart? Owner { get; }
    public BodySlot Slot { get; }
    public SceneBodyKey(MachinePart? owner,BodySlot slot)
    {
        ArgumentNullException.ThrowIfNull(slot);
        if((owner is null)!=(slot==Workbench.Body))
            throw new ArgumentException("Only the workbench slot has no part owner.");
        Owner=owner; Slot=slot;
    }
}

public readonly record struct SceneJointKey
{
    public MachinePart Owner { get; }
    public JointSlot Slot { get; }
    public SceneJointKey(MachinePart owner,JointSlot slot)
    {
        ArgumentNullException.ThrowIfNull(owner); ArgumentNullException.ThrowIfNull(slot);
        Owner=owner; Slot=slot;
    }
}

/// <summary>Immutable scene references bind once to bodies owned by one shared
/// assembly. Declarations do not integrate motion or resolve contacts.</summary>
public abstract class SceneJointDeclaration
{
    public SceneJointKey Key { get; }
    protected SceneJointDeclaration(SceneJointKey key)
    {
        ArgumentNullException.ThrowIfNull(key.Owner); ArgumentNullException.ThrowIfNull(key.Slot);
        Key=key;
    }
    public abstract ReadOnlySpan<SceneJointKey> Dependencies { get; }
    public abstract PhysicsJoint Bind(PhysicsJointId id,IReadOnlyDictionary<SceneBodyKey,PhysicsBody> bodies,
        IReadOnlyDictionary<SceneJointKey,PhysicsJoint> joints);
}

public sealed class SceneFrameJoint : SceneJointDeclaration
{
    public FrameJointKind Kind { get; }
    public SceneBodyKey A { get; }
    public SceneBodyKey B { get; }
    public JointFrame LocalA { get; }
    public JointFrame LocalB { get; }
    public ConnectedBodyCollision Collision { get; }
    public JointTravelRange? Travel { get; }
    public JointTravelDirection Direction { get; }

    public SceneFrameJoint(SceneJointKey key,FrameJointKind kind,SceneBodyKey a,JointFrame localA,
        SceneBodyKey b,JointFrame localB,ConnectedBodyCollision collision,JointTravelRange? travel,JointTravelDirection direction):base(key)
    {
        ArgumentNullException.ThrowIfNull(a.Slot); ArgumentNullException.ThrowIfNull(b.Slot);
        if(a==b) throw new ArgumentException("A joint requires distinct body references.");
        if(!Enum.IsDefined(direction)||direction!=JointTravelDirection.Both&&kind==FrameJointKind.BallSocket)
            throw new ArgumentException("Direction requires a defined policy and an axial joint.");
        if(!Enum.IsDefined(kind)||!Enum.IsDefined(collision))
            throw new ArgumentException("Joint kind and collision policy must be defined.");
        if(!localA.Orientation.IsValid||!localB.Orientation.IsValid)
            throw new ArgumentException("Joint frames must be explicit.");
        Kind=kind; A=a; B=b; LocalA=localA; LocalB=localB; Collision=collision; Travel=travel; Direction=direction;
    }

    public override ReadOnlySpan<SceneJointKey> Dependencies=>[];
    public override PhysicsJoint Bind(PhysicsJointId id,IReadOnlyDictionary<SceneBodyKey,PhysicsBody> bodies,
        IReadOnlyDictionary<SceneJointKey,PhysicsJoint> joints)
    {
        ArgumentNullException.ThrowIfNull(bodies);
        if(!bodies.TryGetValue(A,out var a)||!bodies.TryGetValue(B,out var b))
            throw new ArgumentException("Joint refers to a body absent from this assembly.");
        return new PhysicsFrameJoint(id,Kind,a,LocalA,b,LocalB,Collision,Travel,Direction);
    }
}
