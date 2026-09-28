using System;

namespace CuriousContraptions.Physics;

public enum ConvexSweepStatus { Clear, Contact, InitialContact }
public readonly record struct ConvexSweepResult(ConvexSweepStatus Status,double Time,
    ConvexDistanceResult Separation,int Iterations);

/// <summary>A convex child's local geometry evaluated along its body's captured
/// trajectory. There is no independent collision-only motion integrator.</summary>
public readonly struct ConvexMotion
{
    public ConvexInstance Instance { get; }
    public BodyTrajectory Trajectory { get; }
    public CollisionVector CenterAtStart=>Trajectory.StartPose.Center;
    public CollisionVector LinearVelocity=>Trajectory.LinearVelocity;
    public double AngularSpeedBound=>Trajectory.AngularSpeedBound;
    public double Reach { get; }
    public ConvexMotion(ConvexInstance localInstance,BodyTrajectory trajectory)
    {
        ArgumentNullException.ThrowIfNull(trajectory);
        if(localInstance.Geometry is null) throw new ArgumentException("Uninitialised collision instance.",nameof(localInstance));
        Instance=localInstance; Trajectory=trajectory;
        Reach=CollisionVector.From(localInstance.Pose.Origin).Length+localInstance.RadiusBound;
        if(!double.IsFinite(Reach)||!double.IsFinite(AngularSpeedBound*Reach))
            throw new ArgumentOutOfRangeException(nameof(localInstance));
    }
    public AtTime At(double time)
    {
        if(Trajectory is null) throw new InvalidOperationException("Uninitialised convex motion.");
        return new(Instance,Trajectory.At(time));
    }
    public readonly struct AtTime : IConvexSupport
    {
        private readonly ConvexInstance _instance;
        private readonly RigidPose _pose;
        internal AtTime(ConvexInstance instance,RigidPose pose) { _instance=instance; _pose=pose; }
        public InteriorBall InteriorBall=>new(_pose.TransformPoint(_instance.InteriorBall.Center),_instance.InteriorBall.Radius);
        public CollisionVector Support(CollisionVector direction)
        {
            var localDirection=_pose.Rotation.Inverse().Apply(direction);
            var result=_pose.TransformPoint(_instance.Support(localDirection));
            if(!result.IsFinite) throw new InvalidOperationException("Rigid support exceeds representable coordinates.");
            return result;
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
        // Validate both captured horizons before reading motion bounds.
        a.At(duration); b.At(duration);
        var speed=(a.LinearVelocity-b.LinearVelocity).Length+a.AngularSpeedBound*a.Reach+b.AngularSpeedBound*b.Reach;
        if(!double.IsFinite(speed)) throw new ArgumentOutOfRangeException(nameof(a));
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
