using System;
using System.Collections.Generic;

namespace CuriousContraptions.Physics;

public readonly record struct CylindricalExtent(CollisionBounds Bounds,double RadialLowerBound,double RadialUpperBound);

/// <summary>Finite region along frame-local X. Occupancy uses the common support-map
/// distance algorithm; containment bounds the full compound, not its centre or a proxy sphere.</summary>
public readonly struct CylindricalRegion : IConvexSupport
{
    public double HalfLength { get; }
    public double Radius { get; }
    public InteriorBall InteriorBall=>new(default,Math.Min(HalfLength,Radius));
    public CylindricalRegion(double halfLength,double radius)
    {
        if(!double.IsFinite(halfLength)||halfLength<=0)throw new ArgumentOutOfRangeException(nameof(halfLength));
        if(!double.IsFinite(radius)||radius<=0)throw new ArgumentOutOfRangeException(nameof(radius));
        HalfLength=halfLength;Radius=radius;
    }
    private void Validate()
    {
        if(HalfLength<=0||Radius<=0)throw new InvalidOperationException("Uninitialised cylindrical region.");
    }
    public CollisionVector Support(CollisionVector direction)
    {
        Validate();
        if(!direction.IsFinite)throw new ArgumentOutOfRangeException(nameof(direction));
        var radial=Math.Sqrt(direction.Y*direction.Y+direction.Z*direction.Z);
        if(!double.IsFinite(radial))throw new ArgumentOutOfRangeException(nameof(direction));
        return new(direction.X<0?-HalfLength:HalfLength,
            radial==0?Radius:Radius*(direction.Y/radial),radial==0?0:Radius*(direction.Z/radial));
    }
    private readonly struct LocalChild(ConvexInstance child,RigidPose body,RigidPose frame):IConvexSupport
    {
        private CollisionVector Point(CollisionVector point)=>frame.InverseTransformPoint(body.TransformPoint(point));
        private CollisionVector Direction(CollisionVector direction)=>body.Rotation.Inverse().Apply(frame.Rotation.Apply(direction));
        public InteriorBall InteriorBall=>new(Point(child.InteriorBall.Center),child.InteriorBall.Radius);
        public CollisionVector Support(CollisionVector direction)=>Point(child.Support(Direction(direction)));
        public CollisionVector CoreSupport(CollisionVector direction)=>Point(child.CoreSupport(Direction(direction)));
        public double RoundingRadius=>child.RoundingRadius;
    }
    private static void ValidateQuery(CompoundGeometry geometry,RigidPose body,RigidPose frame,double tolerance)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        if(!body.Rotation.IsValid||!frame.Rotation.IsValid)throw new ArgumentException("Region queries require explicit rigid poses.");
        if(!double.IsFinite(tolerance)||tolerance<=0)throw new ArgumentOutOfRangeException(nameof(tolerance));
    }

    /// <summary>Any child touching or intersecting the region counts as occupancy.
    /// A compound's empty gaps do not become solid through a convex-hull approximation.</summary>
    public bool Overlaps(CompoundGeometry geometry,RigidPose body,RigidPose frame,double tolerance=ConvexDistance.DefaultTolerance)
    {
        Validate();ValidateQuery(geometry,body,frame,tolerance);
        for(var i=0;i<geometry.Count;i++)
        {
            var result=ConvexDistance.Query(new LocalChild(geometry[new(i)],body,frame),this,tolerance);
            if(result.UpperBound<=tolerance)return true;
        }
        return false;
    }

    public bool Contains(CylindricalExtent extent,double tolerance)
    {
        Validate();
        if(!double.IsFinite(tolerance)||tolerance<0)throw new ArgumentOutOfRangeException(nameof(tolerance));
        if(!extent.Bounds.Minimum.IsFinite||!extent.Bounds.Maximum.IsFinite||
            extent.Bounds.Minimum.X>extent.Bounds.Maximum.X||extent.Bounds.Minimum.Y>extent.Bounds.Maximum.Y||
            extent.Bounds.Minimum.Z>extent.Bounds.Maximum.Z||!double.IsFinite(extent.RadialLowerBound)||
            !double.IsFinite(extent.RadialUpperBound)||extent.RadialLowerBound<0||
            extent.RadialUpperBound<extent.RadialLowerBound)
            throw new ArgumentException("Cylindrical extent is invalid.",nameof(extent));
        return extent.Bounds.Minimum.X>=-HalfLength-tolerance&&extent.Bounds.Maximum.X<=HalfLength+tolerance&&
            extent.RadialUpperBound<=Radius+tolerance;
    }

    /// <summary>Exact axial/transverse support bounds and an error-bounded radial extent.
    /// Core support planes form an outer polygon; support witnesses give the lower bound.
    /// Known Minkowski rounding is restored analytically, including instance-basis distortion.</summary>
    public static CylindricalExtent Measure(CompoundGeometry geometry,RigidPose body,RigidPose frame,
        double tolerance=ConvexDistance.DefaultTolerance)
    {
        ValidateQuery(geometry,body,frame,tolerance);
        var minimum=new CollisionVector(double.PositiveInfinity,double.PositiveInfinity,double.PositiveInfinity);
        var maximum=-minimum;var lower=0.0;var upper=0.0;
        for(var i=0;i<geometry.Count;i++)
        {
            var child=new LocalChild(geometry[new(i)],body,frame);
            var bounds=CollisionBounds.Of(child);
            minimum=new(Math.Min(minimum.X,bounds.Minimum.X),Math.Min(minimum.Y,bounds.Minimum.Y),Math.Min(minimum.Z,bounds.Minimum.Z));
            maximum=new(Math.Max(maximum.X,bounds.Maximum.X),Math.Max(maximum.Y,bounds.Maximum.Y),Math.Max(maximum.Z,bounds.Maximum.Z));
            var radial=MeasureRadial(child,tolerance);
            lower=Math.Max(lower,radial.Lower);upper=Math.Max(upper,radial.Upper);
        }
        return new(new(minimum,maximum),lower,upper);
    }

    private readonly record struct Plane(CollisionVector Normal,double Height);
    private readonly record struct Sector(Plane A,Plane B,double Upper);
    private static (double Lower,double Upper) MeasureRadial(LocalChild child,double tolerance)
    {
        const int maximumRefinements=4096;
        var lower=0.0;
        Plane Sample(CollisionVector normal)
        {
            var point=child.CoreSupport(normal);
            var radius=Math.Sqrt(point.Y*point.Y+point.Z*point.Z);
            if(!double.IsFinite(radius))throw new InvalidOperationException("Radial support exceeds numerical range.");
            lower=Math.Max(lower,radius);
            return new(normal,CollisionVector.Dot(normal,point));
        }
        Sector Bound(Plane a,Plane b)
        {
            var determinant=a.Normal.Y*b.Normal.Z-a.Normal.Z*b.Normal.Y;
            if(determinant<=0)throw new InvalidOperationException("Radial support planes cannot resolve their angular interval.");
            var y=(a.Height*b.Normal.Z-a.Normal.Z*b.Height)/determinant;
            var z=(a.Normal.Y*b.Height-a.Height*b.Normal.Y)/determinant;
            var upper=Math.Sqrt(y*y+z*z);
            if(!double.IsFinite(upper))throw new InvalidOperationException("Radial upper bound exceeds numerical range.");
            return new(a,b,upper);
        }
        var directions=new CollisionVector[]{new(0,1,0),new(0,0,1),new(0,-1,0),new(0,0,-1)};
        var planes=new Plane[4];
        for(var i=0;i<planes.Length;i++)planes[i]=Sample(directions[i]);
        var queue=new PriorityQueue<Sector,double>();
        void Add(Sector sector)=>queue.Enqueue(sector,-sector.Upper);
        for(var i=0;i<planes.Length;i++)Add(Bound(planes[i],planes[(i+1)%planes.Length]));
        for(var iteration=0;iteration<maximumRefinements;iteration++)
        {
            var sector=queue.Dequeue();
            var upper=Math.Max(lower,sector.Upper);
            if(upper-lower<=tolerance)return (lower+child.RoundingRadius,upper+child.RoundingRadius);
            var normal=sector.A.Normal+sector.B.Normal;
            var middle=Sample(normal/normal.Length);
            Add(Bound(sector.A,middle));Add(Bound(middle,sector.B));
        }
        throw new InvalidOperationException("Radial projection did not meet its declared error bound.");
    }
}
