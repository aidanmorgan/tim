using Godot;
using System;

namespace CuriousContraptions.Physics;

/// <summary>Double-precision geometry arithmetic inside collision queries.
/// Godot scene transforms remain explicit float boundaries.</summary>
public readonly record struct CollisionVector(double X,double Y,double Z)
{
    public double LengthSquared=>Dot(this,this);
    public double Length=>Math.Sqrt(LengthSquared);
    public bool IsFinite=>double.IsFinite(X)&&double.IsFinite(Y)&&double.IsFinite(Z);
    public static CollisionVector From(Vector3 v)=>new(v.X,v.Y,v.Z);
    public static CollisionVector Cross(CollisionVector a,CollisionVector b)=>
        new(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);
    public static double Dot(CollisionVector a,CollisionVector b)=>a.X*b.X+a.Y*b.Y+a.Z*b.Z;
    public static CollisionVector operator +(CollisionVector a,CollisionVector b)=>new(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
    public static CollisionVector operator -(CollisionVector a,CollisionVector b)=>new(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
    public static CollisionVector operator -(CollisionVector a)=>new(-a.X,-a.Y,-a.Z);
    public static CollisionVector operator *(CollisionVector a,double b)=>new(a.X*b,a.Y*b,a.Z*b);
    public static CollisionVector operator /(CollisionVector a,double b)=>new(a.X/b,a.Y/b,a.Z/b);
}

/// <summary>A convex shape is defined by its farthest point in a direction.
/// The collision algorithm does not inspect the concrete shape type.</summary>
public abstract class ConvexGeometry
{
    public abstract CollisionVector Support(CollisionVector direction);
    public abstract double BoundingRadius { get; }
}

public sealed class ConvexSphere : ConvexGeometry
{
    public double Radius { get; }
    public ConvexSphere(double radius)
    {
        if(!double.IsFinite(radius)||radius<=0) throw new ArgumentOutOfRangeException(nameof(radius));
        Radius=radius;
    }
    public override double BoundingRadius=>Radius;
    public override CollisionVector Support(CollisionVector direction)
    {
        var length=direction.Length;
        return length==0?new(Radius,0,0):direction*(Radius/length);
    }
}

public sealed class ConvexBox : ConvexGeometry
{
    public CollisionVector Half { get; }
    public ConvexBox(CollisionVector half)
    {
        if(!half.IsFinite||half.X<=0||half.Y<=0||half.Z<=0) throw new ArgumentOutOfRangeException(nameof(half));
        Half=half;
    }
    public override double BoundingRadius=>Half.Length;
    public override CollisionVector Support(CollisionVector direction)=>
        new(direction.X<0?-Half.X:Half.X,direction.Y<0?-Half.Y:Half.Y,direction.Z<0?-Half.Z:Half.Z);
}

/// <summary>The convex hull of a copied point cloud. Lower-dimensional hulls
/// are valid distance-query geometry; dynamic inertia validation is separate.</summary>
public sealed class ConvexHull : ConvexGeometry
{
    private readonly CollisionVector[] _points;
    public override double BoundingRadius { get; }
    public ConvexHull(ReadOnlySpan<CollisionVector> points)
    {
        if(points.Length==0) throw new ArgumentException("A hull requires points.",nameof(points));
        _points=points.ToArray();
        foreach(var p in _points)
        {
            if(!p.IsFinite||!double.IsFinite(p.LengthSquared)) throw new ArgumentOutOfRangeException(nameof(points));
            BoundingRadius=Math.Max(BoundingRadius,p.Length);
        }
    }
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
    CollisionVector Support(CollisionVector direction);
}

public readonly struct ConvexInstance : IConvexSupport
{
    public ConvexGeometry Geometry { get; }
    public Transform3D Pose { get; }
    public double RadiusBound { get; }
    public ConvexInstance(ConvexGeometry geometry,Transform3D pose)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        var b=pose.Basis;
        if(!pose.Origin.IsFinite()||!b.X.IsFinite()||!b.Y.IsFinite()||!b.Z.IsFinite()||
            Math.Abs(b.X.LengthSquared()-1)>.00001||Math.Abs(b.Y.LengthSquared()-1)>.00001||
            Math.Abs(b.Z.LengthSquared()-1)>.00001||Math.Abs(b.X.Dot(b.Y))>.00001||
            Math.Abs(b.X.Dot(b.Z))>.00001||Math.Abs(b.Y.Dot(b.Z))>.00001||
            Math.Abs(b.Determinant()-1)>.0001)
            throw new ArgumentException("Collision instances require proper rigid transforms.",nameof(pose));
        if(!double.IsFinite(geometry.BoundingRadius)||geometry.BoundingRadius<0)
            throw new ArgumentOutOfRangeException(nameof(geometry));
        var x=CollisionVector.From(b.X); var y=CollisionVector.From(b.Y); var z=CollisionVector.From(b.Z);
        var xy=Math.Abs(CollisionVector.Dot(x,y)); var xz=Math.Abs(CollisionVector.Dot(x,z)); var yz=Math.Abs(CollisionVector.Dot(y,z));
        // The maximum absolute row sum of B-transpose*B bounds its largest
        // eigenvalue, including float transform roundoff.
        RadiusBound=geometry.BoundingRadius*Math.Sqrt(Math.Max(x.LengthSquared+xy+xz,
            Math.Max(y.LengthSquared+xy+yz,z.LengthSquared+xz+yz)));
        Geometry=geometry; Pose=pose;
    }
    public CollisionVector Support(CollisionVector direction)
    {
        if(Geometry is null) throw new InvalidOperationException("Uninitialised convex instance.");
        if(!direction.IsFinite) throw new ArgumentOutOfRangeException(nameof(direction));
        var x=CollisionVector.From(Pose.Basis.X); var y=CollisionVector.From(Pose.Basis.Y); var z=CollisionVector.From(Pose.Basis.Z);
        var local=Geometry.Support(new(CollisionVector.Dot(x,direction),CollisionVector.Dot(y,direction),CollisionVector.Dot(z,direction)));
        var point=CollisionVector.From(Pose.Origin)+x*local.X+y*local.Y+z*local.Z;
        if(!point.IsFinite) throw new InvalidOperationException("Support point exceeds representable coordinates.");
        return point;
    }
}
