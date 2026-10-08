using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

/// <summary>Finite cylindrical airflow sampled on current rigid poses. This is a
/// force-free geometry query shared by observations and conserved loads.</summary>
public sealed class AirJetGeometry
{
    private readonly HashSet<PhysicsBodyId> _excluded;
    public PhysicsBodyId Source { get; }
    public PhysicsBodyId Body { get; }
    public CollisionVector LocalOrigin { get; }
    public CollisionVector LocalDirection { get; }
    public CollisionVector LocalPoint { get; }
    public double Reach { get; }
    public double Width { get; }

    public AirJetGeometry(PhysicsBodyId source,PhysicsBodyId body,CollisionVector localOrigin,
        CollisionVector localDirection,CollisionVector localPoint,double reach,double width,
        IEnumerable<PhysicsBodyId> excluded)
    {
        ArgumentNullException.ThrowIfNull(excluded);
        if(source==body)throw new ArgumentException("A jet source and receiver must be distinct.");
        var length=localDirection.Length;
        if(!localOrigin.IsFinite||!localPoint.IsFinite||!localDirection.IsFinite||
            !double.IsFinite(length)||length<=0||!double.IsFinite(reach)||reach<=0||
            !double.IsFinite(width)||width<=0)
            throw new ArgumentException("Jet geometry must be finite with positive extents and direction.");
        Source=source;Body=body;LocalOrigin=localOrigin;LocalDirection=localDirection/length;
        LocalPoint=localPoint;Reach=reach;Width=width;
        _excluded=excluded.ToHashSet();_excluded.Add(source);_excluded.Add(body);
    }

    public void Validate(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsColliderUpdate> colliders)
    {
        ArgumentNullException.ThrowIfNull(bodies);ArgumentNullException.ThrowIfNull(colliders);
        if(!bodies.ContainsKey(Body)||!bodies.ContainsKey(Source))
            throw new ArgumentException("A jet sample requires its owned source and receiver.");
        if(bodies.Count!=colliders.Count||_excluded.Any(id=>!bodies.ContainsKey(id)))
            throw new ArgumentException("Jet participants and collider declarations must belong to the sampled world.");
        foreach(var (id,sample) in bodies)
            if(sample is null||sample.Id!=id||!colliders.TryGetValue(id,out var collider)||collider.Body!=id||
                collider.Geometry is null||!Enum.IsDefined(collider.Participation))
                throw new ArgumentException("Jet sampling requires complete current collider declarations.");
    }

    /// <summary>Find a change of side of any finite jet surface. Crossing is
    /// accepted strictly on the new side, within the positional error budget,
    /// so the next interval cannot rediscover the same zero-time event.
    /// This changes no poses and adds no force hysteresis.</summary>
    internal (BodyTrajectory SourcePath,BodyTrajectory ReceiverPath) ValidateSweep(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsColliderUpdate> colliders,
        IReadOnlyDictionary<PhysicsBodyId,BodyTrajectory> paths,double duration,double tolerance)
    {
        Validate(bodies,colliders);ArgumentNullException.ThrowIfNull(paths);
        if(!double.IsFinite(duration)||duration<0||!double.IsFinite(tolerance)||tolerance<=0||
            tolerance>=Math.Min(Reach,Width)*.125)
            throw new ArgumentOutOfRangeException(nameof(tolerance));
        if(!paths.TryGetValue(Source,out var source)||source is null||
            !paths.TryGetValue(Body,out var receiver)||receiver is null)
            throw new ArgumentException("Jet sweep requires captured source and receiver trajectories.",nameof(paths));
        source.ValidateSource(bodies[Source]);receiver.ValidateSource(bodies[Body]);
        if(duration>source.Duration||duration>receiver.Duration)throw new ArgumentOutOfRangeException(nameof(duration));
        return(source,receiver);
    }

    public AirJetSweepResult Sweep(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsColliderUpdate> colliders,
        IReadOnlyDictionary<PhysicsBodyId,BodyTrajectory> paths,double duration,double tolerance)
    {
        var (source,receiver)=ValidateSweep(bodies,colliders,paths,duration,tolerance);
        var result=new AirJetSweepResult(ScalarSweepStatus.Clear,duration,null,0);
        if(colliders[Body].Participation==CollisionParticipation.Disabled||
            colliders[Source].Participation==CollisionParticipation.Disabled)return result;
        foreach(var surface in new[]{AirJetBoundary.Inlet,AirJetBoundary.Outlet,AirJetBoundary.Rim})
        {
            var path=new AirJetBoundaryPath(this,source,receiver,surface);
            // The squared radial gap maps this allowance to at most tolerance
            // in physical radius; inlet/outlet gaps already have length units.
            var hit=ScalarBoundarySweep.Cast(path,result.Time,tolerance*.125,tolerance*.5);
            result=result with {Iterations=result.Iterations+hit.Iterations};
            if(hit.Status==ScalarSweepStatus.Boundary)
                result=result with {Status=hit.Status,Time=hit.Time,Boundary=surface};
        }
        // Outside the cylinder the next surface cut already precedes any
        // exposure. Inside, every convex child owns its entry/exit side.
        var initial=SpatialSample(bodies[Source].Pose,bodies[Body].Pose);
        if(initial.Along<=0||initial.Along>=Reach||initial.RadialDistance>=Width)return result;
        var segment=new AirJetStreamlinePath(this,bodies[Source],bodies[Body],source,receiver);
        foreach(var (id,collider) in colliders.OrderBy(entry=>entry.Key.Index))
        {
            if(_excluded.Contains(id)||collider.Participation==CollisionParticipation.Disabled)continue;
            if(!paths.TryGetValue(id,out var path)||path is null)
                throw new ArgumentException("Occlusion requires each current blocker's captured trajectory.");
            path.ValidateSource(bodies[id]);
            for(var child=0;child<collider.Geometry.Count;child++)
            {
                var hit=SegmentOcclusionSweep.Cast(segment,new(collider.Geometry[new(child)],path),result.Time,tolerance);
                result=result with {Iterations=checked(result.Iterations+hit.Iterations)};
                if(hit.Status==ScalarSweepStatus.Boundary)
                    result=result with {Status=hit.Status,Time=hit.Time,Boundary=AirJetBoundary.Occlusion};
            }
        }
        return result;
    }

    public bool IsExposed(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsColliderUpdate> colliders)
    {
        Validate(bodies,colliders);
        if(colliders[Body].Participation==CollisionParticipation.Disabled||
            colliders[Source].Participation==CollisionParticipation.Disabled)return false;
        var sample=SpatialSample(bodies[Source].Pose,bodies[Body].Pose);
        if(sample.Along<=0||sample.Along>=Reach||sample.RadialDistance>=Width)return false;
        foreach(var (id,collider) in colliders.OrderBy(entry=>entry.Key.Index))
        {
            if(_excluded.Contains(id)||collider.Participation==CollisionParticipation.Disabled)continue;
            for(var child=0;child<collider.Geometry.Count;child++)
            {
                var shape=new ConvexPose(collider.Geometry[new(child)],bodies[id].Pose);
                var separation=ConvexSeparation.Query(sample.Segment,shape,ConvexDistance.DefaultTolerance/64);
                if(separation.LowerBound<=0)return false;
            }
        }
        return true;
    }

    internal (CollisionSegment Segment,double Along,double RadialDistance) SpatialSample(RigidPose source,RigidPose receiver)
    {
        var point=receiver.TransformPoint(LocalPoint);
        var origin=source.TransformPoint(LocalOrigin);
        var axis=source.Rotation.Apply(LocalDirection);
        var offset=point-origin;var along=CollisionVector.Dot(offset,axis);
        var radial=(offset-axis*along).Length;
        if(!point.IsFinite||!origin.IsFinite||!double.IsFinite(along)||!double.IsFinite(radial))
            throw new InvalidOperationException("Jet sample exceeds representable geometry.");
        return(new(point-axis*along,point),along,radial);
    }
}
