using System;

namespace CuriousContraptions.Geometry;

public readonly record struct RigidPose
{
    public CollisionVector Center { get; }
    public RigidRotation Rotation { get; }
    public static RigidPose Identity=>new(default,RigidRotation.Identity);
    public static RigidPose At(CollisionVector center)=>new(center,RigidRotation.Identity);
    public RigidPose(CollisionVector center,RigidRotation rotation)
    {
        if(!center.IsFinite||!rotation.IsValid) throw new ArgumentException("Pose must be finite and rigid.");
        Center=center; Rotation=rotation;
    }
    /// <summary>Compose local frames without a single-precision scene round trip.</summary>
    public RigidPose Compose(RigidPose local)=>new(TransformPoint(local.Center),Rotation*local.Rotation);
    public CollisionVector TransformPoint(CollisionVector local)=>Center+Rotation.Apply(local);
    public CollisionVector InverseTransformPoint(CollisionVector world)=>Rotation.Inverse().Apply(world-Center);
}
