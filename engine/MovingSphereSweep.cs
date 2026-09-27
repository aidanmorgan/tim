using Godot;
using System;

namespace CuriousContraptions;

public readonly record struct MovingSphereHit(SphereSweepStatus Status,float Time,Vector3 Normal,float Penetration);

/// <summary>Continuous linear relative-motion query. Normal points from the second sphere toward the first.
/// This only reports time of impact; it does not advance bodies, repair overlap or apply an impulse.</summary>
public static class MovingSphereSweep
{
    public static MovingSphereHit Cast(Vector3 firstPosition,float firstRadius,Vector3 firstVelocity,
        Vector3 secondPosition,float secondRadius,Vector3 secondVelocity,float duration)
    {
        if(!firstPosition.IsFinite())throw new ArgumentOutOfRangeException(nameof(firstPosition));
        if(!secondPosition.IsFinite())throw new ArgumentOutOfRangeException(nameof(secondPosition));
        if(!firstVelocity.IsFinite())throw new ArgumentOutOfRangeException(nameof(firstVelocity));
        if(!secondVelocity.IsFinite())throw new ArgumentOutOfRangeException(nameof(secondVelocity));
        if(!float.IsFinite(firstRadius)||firstRadius<=0)throw new ArgumentOutOfRangeException(nameof(firstRadius));
        if(!float.IsFinite(secondRadius)||secondRadius<=0)throw new ArgumentOutOfRangeException(nameof(secondRadius));
        if(!float.IsFinite(duration)||duration<0)throw new ArgumentOutOfRangeException(nameof(duration));
        // Compute differences and products in double, including extreme finite float inputs.
        var px=(double)firstPosition.X-secondPosition.X;
        var py=(double)firstPosition.Y-secondPosition.Y;
        var pz=(double)firstPosition.Z-secondPosition.Z;
        var vx=(double)firstVelocity.X-secondVelocity.X;
        var vy=(double)firstVelocity.Y-secondVelocity.Y;
        var vz=(double)firstVelocity.Z-secondVelocity.Z;
        var radius=(double)firstRadius+secondRadius;
        var squaredDistance=px*px+py*py+pz*pz;
        var distance=Math.Sqrt(squaredDistance);
        var squaredSpeed=vx*vx+vy*vy+vz*vz;
        var speed=Math.Sqrt(squaredSpeed);
        Vector3 Normal(double time)
        {
            var x=px+vx*time;var y=py+vy*time;var z=pz+vz*time;
            var length=Math.Sqrt(x*x+y*y+z*z);
            if(length>0)return new((float)(x/length),(float)(y/length),(float)(z/length));
            // Coincident centres have no geometric normal: oppose relative approach, or use a fixed axis at rest.
            return speed>0?new((float)(-vx/speed),(float)(-vy/speed),(float)(-vz/speed)):Vector3.Right;
        }
        var gap=distance-radius;
        if(gap < -SphereSweep.ContactTolerance)
        {
            if(-gap>float.MaxValue)throw new ArgumentOutOfRangeException(nameof(firstRadius),"Penetration exceeds representable world distance.");
            return new(SphereSweepStatus.Overlapping,0,Normal(0),(float)-gap);
        }
        var approach=px*vx+py*vy+pz*vz;
        if(gap<=SphereSweep.ContactTolerance)
            return approach<0?new(SphereSweepStatus.Contact,0,Normal(0),0)
                :new(SphereSweepStatus.Clear,duration,Vector3.Zero,0);
        if(squaredSpeed==0||approach>=0)return new(SphereSweepStatus.Clear,duration,Vector3.Zero,0);
        var c=(distance-radius)*(distance+radius);
        var discriminant=approach*approach-squaredSpeed*c;
        if(discriminant<0)return new(SphereSweepStatus.Clear,duration,Vector3.Zero,0);
        // The alternate quadratic form avoids subtracting nearly equal values near a surface.
        var time=c/(-approach+Math.Sqrt(discriminant));
        if(time<0||time>duration)return new(SphereSweepStatus.Clear,duration,Vector3.Zero,0);
        var safeTime=(float)time;
        if(safeTime>time)safeTime=MathF.BitDecrement(safeTime);
        return new(SphereSweepStatus.Contact,safeTime,Normal(time),0);
    }
}
