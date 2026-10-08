using System;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Bridge;

/// <summary>Owned value copied at a presentation boundary. Contains no live body,
/// scene target, delegate, resource or mutable collection.</summary>
public readonly record struct BodyPoseRead
{
    public PhysicsBodyId Id { get; }
    public PhysicsMotionType MotionType { get; }
    public RigidPose Pose { get; }
    public RigidPose ReferencePose { get; }
    public RigidPose RelativePose => new(ReferencePose.InverseTransformPoint(Pose.Center),
        ReferencePose.Rotation.Inverse() * Pose.Rotation);

    public BodyPoseRead(PhysicsBodyId id, PhysicsMotionType motionType, RigidPose pose, RigidPose referencePose)
    {
        if (!Enum.IsDefined(motionType)) throw new ArgumentOutOfRangeException(nameof(motionType));
        if (!pose.Center.IsFinite || !pose.Rotation.IsValid)
            throw new ArgumentException("Body pose read must be finite with a valid rotation.", nameof(pose));
        if (!referencePose.Center.IsFinite || !referencePose.Rotation.IsValid)
            throw new ArgumentException("Reference pose must be finite with a valid rotation.", nameof(referencePose));
        Id = id; MotionType = motionType; Pose = pose; ReferencePose = referencePose;
    }
}
