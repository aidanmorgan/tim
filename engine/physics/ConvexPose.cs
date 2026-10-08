using System;

namespace CuriousContraptions.Physics;

/// <summary>Immutable convex support at one captured rigid pose. Current geometry reads
/// and evaluated swept paths share this value; no velocity integration is needed to query a pose.</summary>
public readonly struct ConvexPose : IConvexFeatureSupport
{
    private readonly ConvexInstance _instance;
    private readonly RigidPose _pose;
    private readonly RigidRotation _inverse;
    public ConvexPose(ConvexInstance instance,RigidPose pose)
    {
        if(instance.Geometry is null)throw new ArgumentException("Uninitialised collision instance.",nameof(instance));
        if(!pose.Center.IsFinite||!pose.Rotation.IsValid)throw new ArgumentException("Convex pose requires a finite rigid pose.",nameof(pose));
        _instance=instance;_pose=pose;_inverse=pose.Rotation.Inverse();
    }
    public double RoundingRadius {get{if(_instance.Geometry is null)throw new InvalidOperationException("Uninitialised convex pose.");return _instance.RoundingRadius;}}
    public CollisionVector CoreSupport(CollisionVector direction)
    {
        if(_instance.Geometry is null)throw new InvalidOperationException("Uninitialised convex pose.");
        return _pose.TransformPoint(_instance.CoreSupport(_inverse.Apply(direction)));
    }
    public InteriorBall InteriorBall {get{if(_instance.Geometry is null)throw new InvalidOperationException("Uninitialised convex pose.");return new(_pose.TransformPoint(_instance.InteriorBall.Center),_instance.InteriorBall.Radius);}}
    public SupportFeature SupportingFeature(CollisionVector direction,double planeTolerance)
    {
        if(_instance.Geometry is null)throw new InvalidOperationException("Uninitialised convex pose.");
        var feature=_instance.SupportingFeature(_inverse.Apply(direction),planeTolerance);
        var vertices=new SupportVertex[feature.Vertices.Length];
        for(var i=0;i<vertices.Length;i++)
        {
            var vertex=feature.Vertices[i];
            vertices[i]=new(vertex.Id,_pose.TransformPoint(vertex.Point));
        }
        return new(vertices);
    }
    public CollisionVector Support(CollisionVector direction)
    {
        if(_instance.Geometry is null)throw new InvalidOperationException("Uninitialised convex pose.");
        var localDirection=_inverse.Apply(direction);
        var result=_pose.TransformPoint(_instance.Support(localDirection));
        if(!result.IsFinite) throw new InvalidOperationException("Rigid support exceeds representable coordinates.");
        return result;
    }
}
