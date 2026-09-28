using System;

namespace CuriousContraptions.Physics;

public enum ConvexSweepStatus { Clear, Contact, InitialContact }
public readonly record struct ConvexSweepResult(ConvexSweepStatus Status,double Time,
    ConvexDistanceResult Separation,int Iterations);

/// <summary>Constant linear/angular rigid motion about an explicit world pivot.
/// Evaluates support in double precision without quantizing each trial rotation
/// back through a Godot float transform.</summary>
public readonly struct ConvexMotion
{
    public ConvexInstance Instance { get; }
    public CollisionVector Pivot { get; }
    public CollisionVector LinearVelocity { get; }
    public CollisionVector AngularVelocity { get; }
    public double AngularSpeed { get; }
    public double Reach { get; }
    public ConvexMotion(ConvexInstance instance,CollisionVector pivot,
        CollisionVector linearVelocity,CollisionVector angularVelocity)
    {
        if(instance.Geometry is null) throw new ArgumentException("Uninitialised collision instance.",nameof(instance));
        if(!pivot.IsFinite||!linearVelocity.IsFinite||!angularVelocity.IsFinite)
            throw new ArgumentException("Rigid motion must be finite.");
        Instance=instance; Pivot=pivot; LinearVelocity=linearVelocity; AngularVelocity=angularVelocity;
        AngularSpeed=angularVelocity.Length;
        Reach=(CollisionVector.From(instance.Pose.Origin)-pivot).Length+instance.RadiusBound;
        if(!double.IsFinite(AngularSpeed)||!double.IsFinite(Reach)||!double.IsFinite(AngularSpeed*Reach))
            throw new ArgumentOutOfRangeException(nameof(angularVelocity));
    }

    public AtTime At(double time)
    {
        if(!double.IsFinite(time)||time<0||!double.IsFinite(AngularSpeed*time))
            throw new ArgumentOutOfRangeException(nameof(time));
        return new(this,time);
    }

    public readonly struct AtTime : IConvexSupport
    {
        private readonly ConvexMotion _motion;
        private readonly double _time;
        internal AtTime(ConvexMotion motion,double time) { _motion=motion; _time=time; }
        public CollisionVector Support(CollisionVector direction)
        {
            var localDirection=Rotate(direction,-_motion.AngularSpeed*_time);
            var point=_motion.Instance.Support(localDirection)-_motion.Pivot;
            var result=_motion.Pivot+_motion.LinearVelocity*_time+Rotate(point,_motion.AngularSpeed*_time);
            if(!result.IsFinite) throw new InvalidOperationException("Rigid motion exceeds representable coordinates.");
            return result;
        }
        private CollisionVector Rotate(CollisionVector v,double angle)
        {
            if(_motion.AngularSpeed==0) return v;
            var axis=_motion.AngularVelocity/_motion.AngularSpeed;
            var reduced=Math.IEEERemainder(angle,Math.Tau);
            var c=Math.Cos(reduced); var s=Math.Sin(reduced);
            var cross=new CollisionVector(axis.Y*v.Z-axis.Z*v.Y,axis.Z*v.X-axis.X*v.Z,axis.X*v.Y-axis.Y*v.X);
            return v*c+cross*s+axis*(CollisionVector.Dot(axis,v)*(1-c));
        }
    }
}

/// <summary>Conservative rigid-motion sweep, shared by all convex shape pairs.
/// Both bodies contribute linear and angular travel. This finds contact only;
/// manifold generation, initial penetration and impulse response are separate.</summary>
public static class ConvexSweep
{
    public const double ContactDistance=.0001;
    private const int MaximumIterations=4096;
    public static ConvexSweepResult Cast(ConvexMotion a,ConvexMotion b,double duration)
    {
        if(!double.IsFinite(duration)||duration<0) throw new ArgumentOutOfRangeException(nameof(duration));
        var speed=(a.LinearVelocity-b.LinearVelocity).Length+a.AngularSpeed*a.Reach+b.AngularSpeed*b.Reach;
        if(!double.IsFinite(speed)) throw new ArgumentOutOfRangeException(nameof(a));
        // Validate the full requested interval before an early stationary/contact result.
        a.At(duration); b.At(duration);
        double time=0;
        for(var iteration=1;iteration<=MaximumIterations;iteration++)
        {
            var separation=ConvexDistance.Query(a.At(time),b.At(time));
            if(separation.UpperBound<=ContactDistance+ConvexDistance.DefaultTolerance)
                return new(time==0?ConvexSweepStatus.InitialContact:ConvexSweepStatus.Contact,time,separation,iteration);
            if(time>=duration||speed==0)
                return new(ConvexSweepStatus.Clear,duration,separation,iteration);
            var step=(separation.LowerBound-ContactDistance)/speed;
            var next=Math.Min(duration,time+step);
            if(next<=time) throw new InvalidOperationException("Convex sweep cannot make representable progress.");
            time=next;
        }
        throw new InvalidOperationException("Convex sweep did not converge; collision-free travel was not inferred.");
    }
}
