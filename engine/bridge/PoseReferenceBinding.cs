using System;
using CuriousContraptions.Physics;
namespace CuriousContraptions.Bridge;

public enum PoseReferenceKind { Fixed, Body }

/// <summary>Immutable reference-frame binding. Contains no scene or physics object.</summary>
public readonly record struct PoseReferenceBinding
{
    public PoseReferenceKind Kind { get; }
    public PhysicsBodyId Body { get; }
    public RigidPose Offset { get; }
    private PoseReferenceBinding(PoseReferenceKind kind,PhysicsBodyId body,RigidPose offset)
    {
        Kind=kind;Body=body;Offset=offset;Validate();
    }
    public static PoseReferenceBinding Fixed(RigidPose pose)=>new(PoseReferenceKind.Fixed,default,pose);
    public static PoseReferenceBinding Attached(PhysicsBodyId body,RigidPose offset)=>new(PoseReferenceKind.Body,body,offset);
    internal void Validate()
    {
        if(!Enum.IsDefined(Kind)||!Offset.Center.IsFinite||!Offset.Rotation.IsValid)
            throw new ArgumentException("Reference binding requires a valid kind and finite rigid pose.");
    }
    public RigidPose Resolve(RigidPose bodyPose)
    {
        Validate();
        return Kind switch
        {
            PoseReferenceKind.Fixed=>Offset,
            PoseReferenceKind.Body=>bodyPose.Compose(Offset),
            _=>throw new InvalidOperationException("Unsupported reference binding.")
        };
    }
}
