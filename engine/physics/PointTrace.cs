using System;
namespace CuriousContraptions.Physics;

/// <summary>Shared point-versus-compound cast. The result is the ray parameter t in
/// origin + direction * t, bounded by range; it is distance only for unit directions.</summary>
internal struct PointTrace
{
    private static readonly CompoundGeometry Point=new([new(new ConvexHull([default]),AffineTransform.Identity)]);
    private readonly CompoundMotion _ray;
    private readonly double _range;
    internal double Closest { get; private set; }

    internal PointTrace(CollisionVector origin,CollisionVector direction,double range)
    {
        if(!origin.IsFinite||!direction.IsFinite||direction.LengthSquared==0||!double.IsFinite(range)||range<0)
            throw new ArgumentException("A trace requires finite origin, nonzero direction and nonnegative range.");
        _range=Closest=range;
        _ray=new(Point,new LinearRigidTrajectory(RigidPose.At(origin),direction,range));
    }
    internal void Test(CompoundGeometry geometry,RigidPose pose)
    {
        var stationary=new LinearRigidTrajectory(pose,default,_range);
        var result=CompoundCollision.Cast(_ray,new(geometry,stationary),Closest,0);
        if(result.Status!=ConvexSweepStatus.Clear)Closest=result.Time;
    }
}
