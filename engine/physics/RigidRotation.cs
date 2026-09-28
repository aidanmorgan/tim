using System;

namespace CuriousContraptions.Physics;

/// <summary>Unit quaternion with double precision. Default is deliberately invalid;
/// callers must declare Identity rather than silently obtaining a zero rotation.</summary>
public readonly record struct RigidRotation
{
    public double X { get; }
    public double Y { get; }
    public double Z { get; }
    public double W { get; }
    public static RigidRotation Identity=>new(0,0,0,1);
    public bool IsValid=>double.IsFinite(X)&&double.IsFinite(Y)&&double.IsFinite(Z)&&double.IsFinite(W)&&
        Math.Abs(X*X+Y*Y+Z*Z+W*W-1)<1e-12;
    public RigidRotation(double x,double y,double z,double w)
    {
        var scale=Math.Max(Math.Max(Math.Abs(x),Math.Abs(y)),Math.Max(Math.Abs(z),Math.Abs(w)));
        if(!double.IsFinite(scale)||scale==0) throw new ArgumentException("Rotation requires a finite nonzero quaternion.");
        x/=scale; y/=scale; z/=scale; w/=scale;
        var norm=Math.Sqrt(x*x+y*y+z*z+w*w);
        X=x/norm; Y=y/norm; Z=z/norm; W=w/norm;
    }
    private void Validate()
    {
        if(!IsValid) throw new InvalidOperationException("Uninitialised rigid rotation.");
    }
    public RigidRotation Inverse()
    {
        Validate(); return new(-X,-Y,-Z,W);
    }
    public CollisionVector Apply(CollisionVector v)
    {
        Validate();
        if(!v.IsFinite) throw new ArgumentOutOfRangeException(nameof(v));
        var q=new CollisionVector(X,Y,Z);
        var cross=CollisionVector.Cross(q,v);
        var rotated=v+cross*(2*W)+CollisionVector.Cross(q,cross)*2;
        if(!rotated.IsFinite) throw new InvalidOperationException("Rotation exceeds numeric range.");
        return rotated;
    }
    public static RigidRotation operator *(RigidRotation a,RigidRotation b)
    {
        a.Validate(); b.Validate();
        return new(a.W*b.X+a.X*b.W+a.Y*b.Z-a.Z*b.Y,
            a.W*b.Y-a.X*b.Z+a.Y*b.W+a.Z*b.X,
            a.W*b.Z+a.X*b.Y-a.Y*b.X+a.Z*b.W,
            a.W*b.W-a.X*b.X-a.Y*b.Y-a.Z*b.Z);
    }
    public static RigidRotation FromRotationVector(CollisionVector vector)
    {
        var angle=vector.Length;
        if(!vector.IsFinite||!double.IsFinite(angle)) throw new ArgumentOutOfRangeException(nameof(vector));
        if(angle==0) return Identity;
        var half=Math.IEEERemainder(angle,Math.Tau)*.5;
        var axis=vector/angle;
        var s=Math.Sin(half);
        return new(axis.X*s,axis.Y*s,axis.Z*s,Math.Cos(half));
    }
    public CollisionVector RotationVector()
    {
        Validate();
        var sign=W<0?-1:1;
        var v=new CollisionVector(X*sign,Y*sign,Z*sign);
        var length=v.Length;
        if(length==0) return default;
        return v*(2*Math.Atan2(length,W*sign)/length);
    }
}
