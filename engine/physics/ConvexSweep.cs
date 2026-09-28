using System;

namespace CuriousContraptions.Physics;

public enum ConvexSweepStatus { Clear, Contact, InitialContact }
public readonly record struct ConvexSweepResult(ConvexSweepStatus Status,double Time,
    ConvexSeparationResult Separation,int Iterations);

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
    public double RotationalReach { get; }
    public ConvexMotion(ConvexInstance localInstance,BodyTrajectory trajectory)
    {
        ArgumentNullException.ThrowIfNull(trajectory);
        if(localInstance.Geometry is null) throw new ArgumentException("Uninitialised collision instance.",nameof(localInstance));
        Instance=localInstance; Trajectory=trajectory;
        Reach=CollisionVector.From(localInstance.Pose.Origin).Length+localInstance.RadiusBound;
        RotationalReach=CollisionVector.From(localInstance.Pose.Origin).Length+localInstance.RotationRadiusBound;
        if(!double.IsFinite(Reach)||!double.IsFinite(AngularSpeedBound*RotationalReach))
            throw new ArgumentOutOfRangeException(nameof(localInstance));
    }
    public AtTime At(double time)
    {
        if(Trajectory is null) throw new InvalidOperationException("Uninitialised convex motion.");
        return new(Instance,Trajectory.At(time));
    }
    public readonly struct AtTime : IConvexFeatureSupport
    {
        private readonly ConvexInstance _instance;
        private readonly RigidPose _pose;
        internal AtTime(ConvexInstance instance,RigidPose pose) { _instance=instance; _pose=pose; }
        public InteriorBall InteriorBall=>new(_pose.TransformPoint(_instance.InteriorBall.Center),_instance.InteriorBall.Radius);
        public SupportFeature SupportingFeature(CollisionVector direction,double planeTolerance)
        {
            var feature=_instance.SupportingFeature(_pose.Rotation.Inverse().Apply(direction),planeTolerance);
            var vertices=new SupportVertex[feature.Vertices.Length];
            for(var i=0;i<vertices.Length;i++)
            {
                var vertex=feature.Vertices[i];
                vertices[i]=new(vertex.Id,_pose.TransformPoint(vertex.Point));
            }
            return new(vertices);
        }
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
/// Each accepted interval has a fixed supporting-plane certificate. Signed
/// targets allow ongoing contact to be checked without ignoring the pair.
/// Response and positional correction remain separate.</summary>
public static class ConvexSweep
{
    public const double ContactDistance=.0001;
    private const int MaximumIterations=4096;
    public static ConvexSweepResult Cast(ConvexMotion a,ConvexMotion b,double duration,double minimumSeparation)
    {
        if(!double.IsFinite(minimumSeparation)) throw new ArgumentOutOfRangeException(nameof(minimumSeparation));
        if(!double.IsFinite(duration)||duration<0) throw new ArgumentOutOfRangeException(nameof(duration));
        // Validate both captured horizons before reading motion bounds.
        a.At(duration); b.At(duration);
        var relative=a.LinearVelocity-b.LinearVelocity;
        var angular=a.AngularSpeedBound*a.RotationalReach+b.AngularSpeedBound*b.RotationalReach;
        if(!relative.IsFinite||!double.IsFinite(angular)) throw new ArgumentOutOfRangeException(nameof(a));
        // Reserve room in the event tolerance for both signed bounds and plane arithmetic.
        const double queryTolerance=ConvexDistance.DefaultTolerance*.25;
        ConvexSeparationResult At(double t)=>ConvexSeparation.Query(a.At(t),b.At(t),queryTolerance);
        double time=0;
        for(var iteration=1;iteration<=MaximumIterations;iteration++)
        {
            var separation=At(time);
            if(separation.UpperBound<=minimumSeparation+ConvexDistance.DefaultTolerance)
                return new(time==0?ConvexSweepStatus.InitialContact:ConvexSweepStatus.Contact,time,separation,iteration);
            if(time>=duration) return new(ConvexSweepStatus.Clear,duration,separation,iteration);
            var normal=separation.Normal;
            if(!normal.IsFinite||Math.Abs(normal.Length-1)>1e-10)
                throw new InvalidOperationException("Sweep requires a defined separating-plane normal.");
            var gap=CollisionVector.Dot(normal,a.At(time).Support(-normal)-b.At(time).Support(normal));
            var closing=angular-CollisionVector.Dot(normal,relative);
            if(!double.IsFinite(gap)||!double.IsFinite(closing)||gap<=minimumSeparation)
                throw new InvalidOperationException("Signed query cannot certify positive sweep progress.");
            // Tangential translation does not close this fixed plane. If its
            // worst possible closing rate is nonpositive, it certifies the rest
            // of the horizon, including rotation, without endpoint assumptions.
            if(closing<=0) return new(ConvexSweepStatus.Clear,duration,At(duration),iteration);
            var step=(gap-minimumSeparation)/closing;
            var next=Math.Min(duration,time+step);
            if(next<=time) throw new InvalidOperationException("Convex sweep cannot make representable progress.");
            time=next;
        }
        throw new InvalidOperationException("Convex sweep did not converge; collision-free travel was not inferred.");
    }
}
