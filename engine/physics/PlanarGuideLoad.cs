using System;
using System.Collections.Generic;

namespace CuriousContraptions.Physics;

/// <summary>Authored bounded centring acceleration in a moving local frame.
/// This is explicit gameplay assistance, not a passive spring or capture shortcut.
/// It cannot move solid walls or change a body's state outside the shared solve.</summary>
public sealed record PlanarGuideLoad
{
    public PhysicsBodyId Body { get; }
    public PhysicsBodyId Frame { get; }
    public double MinimumHeight { get; }
    public double MinimumSupportHeight { get; }
    public double MaximumHeight { get; }
    public double HalfX { get; }
    public double HalfZ { get; }
    public double MaximumAcceleration { get; }
    public PlanarGuideLoad(PhysicsBodyId body,PhysicsBodyId frame,double minimumHeight,double maximumHeight,
        double halfX,double halfZ,double maximumAcceleration,double minimumSupportHeight)
    {
        if(body==frame)throw new ArgumentException("Guidance requires distinct target and frame bodies.");
        if(!double.IsFinite(minimumSupportHeight)||!double.IsFinite(minimumHeight)||!double.IsFinite(maximumHeight)||minimumHeight>maximumHeight||
            !double.IsFinite(halfX)||halfX<=0||!double.IsFinite(halfZ)||halfZ<=0||
            !double.IsFinite(maximumAcceleration)||maximumAcceleration<0)
            throw new ArgumentOutOfRangeException(nameof(maximumAcceleration));
        MinimumSupportHeight=minimumSupportHeight;
        Body=body;Frame=frame;MinimumHeight=minimumHeight;MaximumHeight=maximumHeight;
        HalfX=halfX;HalfZ=halfZ;MaximumAcceleration=maximumAcceleration;
    }
    public void Validate(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsColliderUpdate> colliders)
    {
        ArgumentNullException.ThrowIfNull(bodies);ArgumentNullException.ThrowIfNull(colliders);
        if(!bodies.TryGetValue(Body,out var body)||body is null||body.MotionType!=PhysicsMotionType.Dynamic||!bodies.ContainsKey(Frame))
            throw new ArgumentException("Guidance requires its owned dynamic target and frame.");
        foreach(var id in new[]{Body,Frame})
            if(bodies[id] is null||bodies[id].Id!=id||!colliders.TryGetValue(id,out var collider)||
                collider.Body!=id||collider.Geometry is null||!Enum.IsDefined(collider.Participation))
                throw new ArgumentException("Guidance requires current target and frame collider declarations.");
    }
    private bool Enabled(IReadOnlyDictionary<PhysicsBodyId,PhysicsColliderUpdate> colliders)=>
        MaximumAcceleration>0&&colliders[Body].Participation==CollisionParticipation.Enabled&&
        colliders[Frame].Participation==CollisionParticipation.Enabled;


    internal static double LowerSupportHeight(CompoundGeometry geometry,RigidPose body,RigidPose frame)
    {
        var down=body.Rotation.Inverse().Apply(frame.Rotation.Apply(new(0,-1,0)));
        var height=double.PositiveInfinity;
        for(var i=0;i<geometry.Count;i++)
            height=Math.Min(height,frame.InverseTransformPoint(body.TransformPoint(geometry[new(i)].Support(down))).Y);
        return height;
    }

    public BodyWrench Evaluate(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsColliderUpdate> colliders)
    {
        Validate(bodies,colliders);
        if(!Enabled(colliders))return default;
        var body=bodies[Body];var frame=bodies[Frame];
        var local=frame.Pose.InverseTransformPoint(body.Center);
        var velocity=frame.Pose.Rotation.Inverse().Apply(body.LinearVelocity-frame.PointVelocity(body.Center));
        if(velocity.Y>0||local.Y<MinimumHeight||local.Y>MaximumHeight||
            Math.Abs(local.X)>HalfX||Math.Abs(local.Z)>HalfZ||
            LowerSupportHeight(colliders[Body].Geometry,body.Pose,frame.Pose)<MinimumSupportHeight)return default;
        var acceleration=new CollisionVector(-local.X,0,-local.Z);
        acceleration*=MaximumAcceleration/Math.Max(1,acceleration.Length);
        return new(frame.Pose.Rotation.Apply(acceleration)/body.InverseMass,default);
    }
}
