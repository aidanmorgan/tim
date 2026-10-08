using System;

namespace CuriousContraptions.Geometry;

/// <summary>Double-precision geometry arithmetic inside collision queries.
/// Scene conversion belongs to the presentation adapter.</summary>
public readonly record struct CollisionVector(double X,double Y,double Z)
{
    public double LengthSquared=>Dot(this,this);
    public double Length=>Math.Sqrt(LengthSquared);
    public bool IsFinite=>double.IsFinite(X)&&double.IsFinite(Y)&&double.IsFinite(Z);
    public static CollisionVector Cross(CollisionVector a,CollisionVector b)=>
        new(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);
    public static double Dot(CollisionVector a,CollisionVector b)=>a.X*b.X+a.Y*b.Y+a.Z*b.Z;
    public static CollisionVector operator +(CollisionVector a,CollisionVector b)=>new(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
    public static CollisionVector operator -(CollisionVector a,CollisionVector b)=>new(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
    public static CollisionVector operator -(CollisionVector a)=>new(-a.X,-a.Y,-a.Z);
    public static CollisionVector operator *(CollisionVector a,double b)=>new(a.X*b,a.Y*b,a.Z*b);
    public static CollisionVector operator /(CollisionVector a,double b)=>new(a.X/b,a.Y/b,a.Z/b);
}

