using System;

namespace CuriousContraptions.Physics;

/// <summary>A convex shape is defined by its farthest point in a direction.
/// The collision algorithm does not inspect the concrete shape type.</summary>
public readonly record struct InteriorBall
{
    public CollisionVector Center { get; }
    public double Radius { get; }
    public InteriorBall(CollisionVector center,double radius)
    {
        if(!center.IsFinite||!double.IsFinite(radius)||radius<0) throw new ArgumentException("Interior ball must be finite.");
        Center=center; Radius=radius;
    }
}

public abstract class ConvexGeometry
{
    public abstract CollisionVector Support(CollisionVector direction);
    public abstract CollisionVector CoreSupport(CollisionVector direction);
    public abstract SupportFeature SupportingFeature(CollisionVector direction,double planeTolerance);
    public abstract double BoundingRadius { get; }
    /// <summary>Radius of an exact Minkowski-summed ball. The remaining core
    /// fits within BoundingRadius minus RoundingRadius, about the local origin.</summary>
    public abstract double RoundingRadius { get; }
    public abstract InteriorBall InteriorBall { get; }
}

public sealed class ConvexSphere : ConvexGeometry
{
    public double Radius { get; }
    public override CollisionVector CoreSupport(CollisionVector direction)=>default;
    public ConvexSphere(double radius)
    {
        if(!double.IsFinite(radius)||radius<=0) throw new ArgumentOutOfRangeException(nameof(radius));
        Radius=radius;
    }
    public override SupportFeature SupportingFeature(CollisionVector direction,double planeTolerance)=>
        new([new(new(0),Support(SupportFeature.UnitDirection(direction,planeTolerance)))]);
    public override double BoundingRadius=>Radius;
    public override double RoundingRadius=>Radius;
    public override InteriorBall InteriorBall=>new(default,Radius);
    public override CollisionVector Support(CollisionVector direction)
    {
        var length=direction.Length;
        return length==0?new(Radius,0,0):direction*(Radius/length);
    }
}

public sealed class ConvexBox : ConvexGeometry
{
    public CollisionVector Half { get; }
    public override CollisionVector CoreSupport(CollisionVector direction)=>Support(direction);
    public ConvexBox(CollisionVector half)
    {
        if(!half.IsFinite||half.X<=0||half.Y<=0||half.Z<=0) throw new ArgumentOutOfRangeException(nameof(half));
        Half=half;
    }
    public override SupportFeature SupportingFeature(CollisionVector direction,double planeTolerance)
    {
        Span<CollisionVector> points=stackalloc CollisionVector[8];
        for(var i=0;i<8;i++) points[i]=new((i&1)==0?-Half.X:Half.X,(i&2)==0?-Half.Y:Half.Y,(i&4)==0?-Half.Z:Half.Z);
        return SupportFeature.FromPoints(points,direction,planeTolerance);
    }
    public override double BoundingRadius=>Half.Length;
    public override double RoundingRadius=>0;
    public override InteriorBall InteriorBall=>new(default,Math.Min(Half.X,Math.Min(Half.Y,Half.Z)));
    public override CollisionVector Support(CollisionVector direction)=>
        new(direction.X<0?-Half.X:Half.X,direction.Y<0?-Half.Y:Half.Y,direction.Z<0?-Half.Z:Half.Z);
}

/// <summary>The convex hull of a copied point cloud. Lower-dimensional hulls
/// are valid distance-query geometry; dynamic inertia validation is separate.</summary>
public sealed class ConvexHull : ConvexGeometry
{
    private readonly CollisionVector[] _points;
    public override CollisionVector CoreSupport(CollisionVector direction)=>Support(direction);
    public override double BoundingRadius { get; }
    public override double RoundingRadius=>0;
    public override InteriorBall InteriorBall { get; }
    public ConvexHull(ReadOnlySpan<CollisionVector> points)
    {
        if(points.Length==0) throw new ArgumentException("A hull requires points.",nameof(points));
        _points=points.ToArray();
        CollisionVector center=default;
        foreach(var p in _points)
        {
            if(!p.IsFinite||!double.IsFinite(p.LengthSquared)) throw new ArgumentOutOfRangeException(nameof(points));
            BoundingRadius=Math.Max(BoundingRadius,p.Length);
            center+=p/_points.Length;
        }
        InteriorBall=new(center,0); // A convex combination is inside even a degenerate hull.
    }
    public override SupportFeature SupportingFeature(CollisionVector direction,double planeTolerance)=>
        SupportFeature.FromPoints(_points,direction,planeTolerance);
    public override CollisionVector Support(CollisionVector direction)
    {
        var best=_points[0]; var projection=CollisionVector.Dot(best,direction);
        for(var i=1;i<_points.Length;i++)
        {
            var candidate=CollisionVector.Dot(_points[i],direction);
            if(candidate<=projection) continue;
            best=_points[i]; projection=candidate;
        }
        return best;
    }
}

public interface IConvexSupport
{
    InteriorBall InteriorBall { get; }
    CollisionVector Support(CollisionVector direction);
}

public readonly struct ConvexInstance : IConvexFeatureSupport
{
    public ConvexGeometry Geometry { get; }
    public AffineTransform Pose { get; }
    public double RadiusBound { get; }
    public double RotationRadiusBound { get; }
    public double RoundingRadius { get; }
    public InteriorBall InteriorBall { get; }
    public ConvexInstance(ConvexGeometry geometry,AffineTransform pose)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        var b=pose.Basis;
        if(!pose.Origin.IsFinite||!b.IsApproximatelyRigid)
            throw new ArgumentException("Collision instances require proper rigid transforms.",nameof(pose));
        if(!double.IsFinite(geometry.BoundingRadius)||geometry.BoundingRadius<0||
            !double.IsFinite(geometry.RoundingRadius)||geometry.RoundingRadius<0||geometry.RoundingRadius>geometry.BoundingRadius)
            throw new ArgumentOutOfRangeException(nameof(geometry));
        var x=b.X; var y=b.Y; var z=b.Z;
        var xy=Math.Abs(CollisionVector.Dot(x,y)); var xz=Math.Abs(CollisionVector.Dot(x,z)); var yz=Math.Abs(CollisionVector.Dot(y,z));
        // The maximum absolute row sum of B-transpose*B bounds its largest
        // eigenvalue, including float transform roundoff.
        var maximum=Math.Max(x.LengthSquared+xy+xz,Math.Max(y.LengthSquared+xy+yz,z.LengthSquared+xz+yz));
        RadiusBound=geometry.BoundingRadius*Math.Sqrt(maximum);
        var ball=geometry.InteriorBall;
        if(ball.Radius<geometry.RoundingRadius) throw new ArgumentException("Interior ball must contain the declared rounding ball.",nameof(geometry));
        var minimum=Math.Min(x.LengthSquared-xy-xz,Math.Min(y.LengthSquared-xy-yz,z.LengthSquared-xz-yz));
        if(!double.IsFinite(minimum)||minimum<=0) throw new ArgumentException("Rigid transform has no positive interior-radius bound.",nameof(pose));
        // The minimum curvature radius of the transformed rounding ellipsoid
        // is bounded below by r*sigmaMin^2/sigmaMax. Subtracting this ball
        // leaves a convex support function, including float-basis anisotropy.
        RoundingRadius=geometry.RoundingRadius*minimum/Math.Sqrt(maximum);
        InteriorBall=new(pose.Origin+x*ball.Center.X+y*ball.Center.Y+z*ball.Center.Z,
            ball.Radius*Math.Sqrt(minimum));
        // For the rounded term h(n)=r*sqrt(n^T B B^T n), the derivative under
        // unit angular travel is bounded by r*(lambdaMax-lambdaMin)/sqrt(lambdaMin).
        // This retains float-basis anisotropy rather than pretending it is exactly rigid.
        RotationRadiusBound=(geometry.BoundingRadius-geometry.RoundingRadius)*Math.Sqrt(maximum)+
            geometry.RoundingRadius*(maximum-minimum)/Math.Sqrt(minimum);
        if(!double.IsFinite(RotationRadiusBound)) throw new ArgumentOutOfRangeException(nameof(geometry));
        Geometry=geometry; Pose=pose;
    }
    public SupportFeature SupportingFeature(CollisionVector direction,double planeTolerance)
    {
        if(Geometry is null) throw new InvalidOperationException("Uninitialised convex instance.");
        var normal=SupportFeature.UnitDirection(direction,planeTolerance);
        var x=Pose.Basis.X; var y=Pose.Basis.Y; var z=Pose.Basis.Z;
        var localDirection=new CollisionVector(CollisionVector.Dot(x,normal),CollisionVector.Dot(y,normal),CollisionVector.Dot(z,normal));
        var local=Geometry.SupportingFeature(localDirection,planeTolerance/localDirection.Length);
        var vertices=new SupportVertex[local.Vertices.Length];
        for(var i=0;i<vertices.Length;i++)
        {
            var v=local.Vertices[i]; var p=v.Point;
            vertices[i]=new(v.Id,Pose.Origin+x*p.X+y*p.Y+z*p.Z);
        }
        return new(vertices);
    }
    public CollisionVector CoreSupport(CollisionVector direction)
    {
        if(Geometry is null) throw new InvalidOperationException("Uninitialised convex instance.");
        var normal=SupportFeature.UnitDirection(direction,0);
        var x=Pose.Basis.X; var y=Pose.Basis.Y; var z=Pose.Basis.Z;
        var localDirection=new CollisionVector(CollisionVector.Dot(x,normal),CollisionVector.Dot(y,normal),CollisionVector.Dot(z,normal));
        var core=Geometry.CoreSupport(localDirection);
        // Remove the contained ball before adding the translation. The residual
        // ellipsoid support remains explicit for float-basis anisotropy.
        var delta=new CollisionVector(
            (x.X*x.X+y.X*y.X+z.X*z.X-1)*normal.X+(x.X*x.Y+y.X*y.Y+z.X*z.Y)*normal.Y+(x.X*x.Z+y.X*y.Z+z.X*z.Z)*normal.Z,
            (x.Y*x.X+y.Y*y.X+z.Y*z.X)*normal.X+(x.Y*x.Y+y.Y*y.Y+z.Y*z.Y-1)*normal.Y+(x.Y*x.Z+y.Y*y.Z+z.Y*z.Z)*normal.Z,
            (x.Z*x.X+y.Z*y.X+z.Z*z.X)*normal.X+(x.Z*x.Y+y.Z*y.Y+z.Z*z.Y)*normal.Y+(x.Z*x.Z+y.Z*y.Z+z.Z*z.Z-1)*normal.Z);
        var distortion=CollisionVector.Dot(normal,delta)/normal.LengthSquared;
        var stretch=Math.Sqrt(1+distortion);
        var inverseStretchMinusOne=-distortion/(stretch*(stretch+1));
        var remainder=delta*(Geometry.RoundingRadius/stretch)+
            normal*((Geometry.RoundingRadius-RoundingRadius)+Geometry.RoundingRadius*inverseStretchMinusOne);
        return Pose.Origin+x*core.X+y*core.Y+z*core.Z+remainder;
    }
    public CollisionVector Support(CollisionVector direction)
    {
        if(Geometry is null) throw new InvalidOperationException("Uninitialised convex instance.");
        if(!direction.IsFinite) throw new ArgumentOutOfRangeException(nameof(direction));
        var x=Pose.Basis.X; var y=Pose.Basis.Y; var z=Pose.Basis.Z;
        var local=Geometry.Support(new(CollisionVector.Dot(x,direction),CollisionVector.Dot(y,direction),CollisionVector.Dot(z,direction)));
        var point=Pose.Origin+x*local.X+y*local.Y+z*local.Z;
        if(!point.IsFinite) throw new InvalidOperationException("Support point exceeds representable coordinates.");
        return point;
    }
}
