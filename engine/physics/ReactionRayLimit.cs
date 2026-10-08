using System;

namespace CuriousContraptions.Physics;

internal enum ReactionRayExtent { Zero, Finite, Unbounded, BeyondRange }

/// <summary>Intersection of the connected feasible reaction-ray domains containing
/// the current coordinates. A finite result is a proposal boundary; rounded
/// committed reactions still require all original physical checks.</summary>
internal readonly record struct ReactionRayLimit
{
    internal ReactionRayExtent Extent { get; }
    internal double Distance { get; }
    private ReactionRayLimit(ReactionRayExtent extent,double distance){Extent=extent;Distance=distance;}
    internal static ReactionRayLimit Zero=>new(ReactionRayExtent.Zero,0);
    internal static ReactionRayLimit Unbounded=>new(ReactionRayExtent.Unbounded,0);
    private static ReactionRayLimit At(double value)=>value==0?Zero:
        double.IsPositiveInfinity(value)?new(ReactionRayExtent.BeyondRange,0):
        double.IsFinite(value)&&value>0?new(ReactionRayExtent.Finite,value):
        throw new InvalidOperationException("Reaction ray boundary is invalid.");
    internal ReactionRayLimit Intersect(ReactionRayLimit other)
    {
        if(Extent==ReactionRayExtent.Zero||other.Extent==ReactionRayExtent.Zero)return Zero;
        if(Extent==ReactionRayExtent.Finite)return other.Extent==ReactionRayExtent.Finite?At(Math.Min(Distance,other.Distance)):this;
        if(other.Extent==ReactionRayExtent.Finite)return other;
        return Extent==ReactionRayExtent.BeyondRange?this:other;
    }

    internal static ReactionRayLimit Scalar(double value,double direction,double lower,double upper)
    {
        if(!double.IsFinite(value)||!double.IsFinite(direction)||double.IsNaN(lower)||double.IsNaN(upper)||
            lower>value||value>upper)throw new ArgumentException("Reaction ray starts outside its scalar interval.");
        if(direction==0)return Unbounded;
        var bound=direction>0?upper:lower;
        if(double.IsInfinity(bound))return Unbounded;
        Span<ulong> storage=stackalloc ulong[BinaryProductSum.StorageLength];
        var sum=new BinaryProductSum(storage);
        sum.Add(bound,1);sum.Add(-value,1);
        var difference=sum.FinishScaled();
        if(difference.Mantissa==0)return Zero;
        var exponent=Math.ILogB(Math.Abs(direction));
        return At(Math.ScaleB(Math.Abs(difference.Mantissa/Math.ScaleB(direction,-exponent)),difference.Exponent-exponent));
    }

    internal static ReactionRayLimit Cone(double normal,double u,double v,
        double normalDirection,double uDirection,double vDirection,double friction)
    {
        if(!double.IsFinite(normal)||normal<0||!double.IsFinite(u)||!double.IsFinite(v)||
            !double.IsFinite(normalDirection)||!double.IsFinite(uDirection)||!double.IsFinite(vDirection)||
            !double.IsFinite(friction)||friction<0)throw new ArgumentException("Reaction cone ray requires finite inputs.");
        var normalLimit=Scalar(normal,normalDirection,0,double.PositiveInfinity);
        if(normalLimit.Extent==ReactionRayExtent.Zero)return Zero;
        var radius=friction*normal;
        if(!double.IsFinite(radius))throw new InvalidOperationException("Reaction cone radius exceeds numeric range.");
        var pointScale=Math.Max(radius,Math.Max(Math.Abs(u),Math.Abs(v)));
        var directionExponent=Math.Max(Exponent(uDirection),Exponent(vDirection));
        var radialExponent=normalDirection==0||friction==0?int.MinValue:
            checked(Exponent(friction)+Exponent(normalDirection)+2);
        directionExponent=Math.Max(directionExponent,radialExponent);
        if(directionExponent==int.MinValue)return normalLimit;
        var du=Math.ScaleB(uDirection,-directionExponent);
        var dv=Math.ScaleB(vDirection,-directionExponent);
        var dr=normalDirection==0||friction==0?0:
            Math.ScaleB(Math.ScaleB(friction,-Exponent(friction))*Math.ScaleB(normalDirection,-Exponent(normalDirection)),
                Exponent(friction)+Exponent(normalDirection)-directionExponent);
        var a=Quadratic(du,dv,dr);
        if(pointScale==0)
            return normalDirection>=0&&a<=0?normalLimit:Zero;
        var pointExponent=Exponent(pointScale);
        var pu=Math.ScaleB(u,-pointExponent);var pv=Math.ScaleB(v,-pointExponent);var pr=Math.ScaleB(radius,-pointExponent);
        var c=Quadratic(pu,pv,pr);
        if(c>0)return Zero; // No outward travel from an already rounded exterior point.
        var b=2*Dot(pu,du,pv,dv,pr,dr);
        if(c==0&&(b>0||b==0&&a>0))return Zero;
        double distance;
        if(a==0)
        {
            if(b<=0)return normalLimit;
            distance=-c/b;
        }
        else
        {
            if(a<0&&b<=0)return normalLimit;
            var discriminant=Discriminant(a,b,c);
            if(discriminant<=0)return normalLimit;
            var root=Math.Sqrt(discriminant);
            distance=b>=0?-2*c/(b+root):(root-b)/(2*a);
            if(distance==0&&c==0&&b<0)distance=-b/a;
            if(distance<0)return normalLimit;
        }
        return normalLimit.Intersect(At(Math.ScaleB(distance,pointExponent-directionExponent)));
    }

    private static int Exponent(double value)=>value==0?int.MinValue:Math.ILogB(Math.Abs(value));
    private static double Quadratic(double u,double v,double radius)
    {
        Span<ulong> storage=stackalloc ulong[BinaryProductSum.StorageLength];var sum=new BinaryProductSum(storage);
        sum.Add(u,u);sum.Add(v,v);sum.Add(-radius,radius);return sum.Finish();
    }
    private static double Dot(double u,double du,double v,double dv,double radius,double dr)
    {
        Span<ulong> storage=stackalloc ulong[BinaryProductSum.StorageLength];var sum=new BinaryProductSum(storage);
        sum.Add(u,du);sum.Add(v,dv);sum.Add(-radius,dr);return sum.Finish();
    }
    private static double Discriminant(double a,double b,double c)
    {
        Span<ulong> storage=stackalloc ulong[BinaryProductSum.StorageLength];var sum=new BinaryProductSum(storage);
        sum.Add(b,b);sum.Add(-4,a,c);return sum.Finish();
    }
}
