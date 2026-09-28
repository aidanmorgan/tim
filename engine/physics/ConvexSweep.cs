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
    public IRigidTrajectory Trajectory { get; }
    public CollisionVector CenterAtStart=>Trajectory.StartPose.Center;
    public CollisionVector LinearVelocity=>Trajectory.LinearVelocity;
    public double AngularSpeedBound=>Trajectory.AngularSpeedBound;
    public double Reach { get; }
    public double RotationalReach { get; }
    public ConvexMotion(ConvexInstance localInstance,IRigidTrajectory trajectory)
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
        public double RoundingRadius=>_instance.RoundingRadius;
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
    private enum PlaneReference { World, BodyA, BodyB }
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
            if(separation.UpperBound<=minimumSeparation+ConvexDistance.DefaultTolerance&&
                (time>0||separation.LowerBound<=minimumSeparation))
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
            // Stop inside the existing event tolerance, leaving a certified
            // positive margin above the forbidden separation. Landing exactly
            // on that boundary can round into overlap and prevent a later
            // separating correction. This reserve never widens the tolerance.
            var clearance=gap-minimumSeparation;
            var reserve=Math.Min(clearance*.5,2*queryTolerance);
            var step=(clearance-reserve)/closing;
            var next=Math.Min(duration,time+step);
            // A support-plane chord minus its material-point curvature bound
            // certifies the whole interval. Body-fixed planes follow rotating
            // faces rather than forcing a nearly resting pair into tiny steps.
            var segmentEnd=Math.Min(duration,Math.Min(a.Trajectory.SegmentEndAfter(time),b.Trajectory.SegmentEndAfter(time)));
            foreach(var reference in new[]{PlaneReference.World,PlaneReference.BodyA,PlaneReference.BodyB})
            {
                var end=segmentEnd;
                while(end>next)
                {
                    var lower=PlaneIntervalLowerBound(a,b,time,end,normal,gap,reference);
                    if(lower>minimumSeparation+queryTolerance) { next=end; break; }
                    end=time+(end-time)*.5;
                }
            }
            if(next<=time) throw new InvalidOperationException("Convex sweep cannot make representable progress.");
            time=next;
        }
        throw new InvalidOperationException("Convex sweep did not converge; collision-free travel was not inferred.");
    }
    private static double PlaneIntervalLowerBound(ConvexMotion a,ConvexMotion b,double start,double end,
        CollisionVector normal,double startGap,PlaneReference reference)
    {
        var poseA0=a.Trajectory.At(start); var poseB0=b.Trajectory.At(start);
        var poseA1=a.Trajectory.At(end); var poseB1=b.Trajectory.At(end);
        var wa=a.AngularSpeedBound; var wb=b.AngularSpeedBound;
        var relativeSpeed=(a.LinearVelocity-b.LinearVelocity).Length;
        var distance=Math.Max((poseA0.Center-poseB0.Center).Length,(poseA1.Center-poseB1.Center).Length);
        CollisionVector endNormal; double curvature;
        switch(reference)
        {
            case PlaneReference.World:
                endNormal=normal; curvature=wa*wa*a.Reach+wb*wb*b.Reach; break;
            case PlaneReference.BodyA:
                endNormal=(poseA1.Rotation*poseA0.Rotation.Inverse()).Apply(normal);
                curvature=wa*wa*distance+2*wa*relativeSpeed+(wa+wb)*(wa+wb)*b.Reach; break;
            case PlaneReference.BodyB:
                endNormal=(poseB1.Rotation*poseB0.Rotation.Inverse()).Apply(normal);
                curvature=wb*wb*distance+2*wb*relativeSpeed+(wa+wb)*(wa+wb)*a.Reach; break;
            default: throw new ArgumentOutOfRangeException(nameof(reference));
        }
        if(!double.IsFinite(curvature)) throw new InvalidOperationException("Sweep curvature exceeds numeric range.");
        var endGap=CollisionVector.Dot(endNormal,a.At(end).Support(-endNormal)-b.At(end).Support(endNormal));
        var horizon=end-start;
        return Math.Min(startGap,endGap)-curvature*horizon*horizon/8;
    }

}
