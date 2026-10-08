using System;

namespace CuriousContraptions.Physics;

/// <summary>Signed winding and a lower bound on total angular distance.
/// The true distance is in [Distance, Distance + DistanceError].</summary>
public readonly record struct AngularPathTravel(double Winding,double Distance,double DistanceError);

/// <summary>Measures hinge twist on captured geometric paths, including full turns
/// and reversals. Physical endpoint velocities are not path derivatives.</summary>
public static class AngularPathMeasure
{
    private const int MaximumIntervals=262144;
    private const double MinimumTwistRadius=1e-12;

    public static AngularPathTravel Measure(BodyTrajectory a,RigidRotation localA,
        BodyTrajectory b,RigidRotation localB,double duration,double distanceTolerance)
    {
        ArgumentNullException.ThrowIfNull(a); ArgumentNullException.ThrowIfNull(b);
        if(!localA.IsValid||!localB.IsValid)throw new ArgumentException("Hinge frames must be valid.");
        if(!double.IsFinite(duration)||duration<0||duration>a.Duration||duration>b.Duration)
            throw new ArgumentOutOfRangeException(nameof(duration));
        if(!double.IsFinite(distanceTolerance)||distanceTolerance<=0)
            throw new ArgumentOutOfRangeException(nameof(distanceTolerance));
        var intervals=0;
        RigidRotation Relative(double time)=>
            (b.At(time).Rotation*localB).Inverse()*(a.At(time).Rotation*localA);
        static double Angle(RigidRotation q)
        {
            if(Math.Sqrt(q.W*q.W+q.Z*q.Z)<=MinimumTwistRadius)
                throw new InvalidOperationException("Antiparallel hinge frames have undefined twist.");
            return Math.Atan2(2*q.Z*q.W,q.W*q.W-q.Z*q.Z);
        }
        AngularPathTravel Interval(double start,double end)
        {
            if(++intervals>MaximumIntervals)
                throw new InvalidOperationException("Angular path measurement exceeded its interval budget.");
            var width=end-start; var middle=start+width*.5;
            if(middle<=start||middle>=end)
                throw new InvalidOperationException("Angular path measurement reached numeric resolution.");
            var motionA=a.PrescribedMotionAt(middle); var motionB=b.PrescribedMotionAt(middle);
            var accelerationA=Math.Min(a.AngularAccelerationBound,
                a.GeometricAngularAccelerationAt(middle).Length+(motionA?.AngularJerkBound??0)*width*.5);
            var accelerationB=Math.Min(b.AngularAccelerationBound,
                b.GeometricAngularAccelerationAt(middle).Length+(motionB?.AngularJerkBound??0)*width*.5);
            var localSpeedA=Math.Min(a.AngularSpeedBound,a.AngularVelocityAt(middle).Length+accelerationA*width*.5);
            var localSpeedB=Math.Min(b.AngularSpeedBound,b.AngularVelocityAt(middle).Length+accelerationB*width*.5);
            var localSpeed=localSpeedA+localSpeedB;
            var localAcceleration=accelerationA+accelerationB;
            if(!double.IsFinite(localSpeed)||!double.IsFinite(localAcceleration))
                throw new InvalidOperationException("Angular path bounds exceed numeric range.");
            var q=Relative(middle);
            var angleStart=Angle(Relative(start)); var angleEnd=Angle(Relative(end));
            Angle(q);
            // On each segment the unprescribed geometric spins are constant.
            // Equal world spins leave the relative local frames unchanged.
            if(motionA is not null&&motionB is not null&&ReferenceEquals(motionA.Path,motionB.Path)&&motionA.Time==motionB.Time||
                localAcceleration==0&&a.AngularVelocityAt(middle)==b.AngularVelocityAt(middle))
                return default;
            var radius=Math.Sqrt(q.W*q.W+q.Z*q.Z)-localSpeed*width*.25;
            if(radius>MinimumTwistRadius&&localSpeed*width/radius<Math.PI*.5)
            {
                var denominator=q.W*q.W+q.Z*q.Z;
                var gradient=(b.At(middle).Rotation*localB).Apply(new(
                    (q.W*q.Y+q.Z*q.X)/denominator,
                    (-q.W*q.X+q.Z*q.Y)/denominator,1));
                var rate=CollisionVector.Dot(gradient,a.AngularVelocityAt(middle)-b.AngularVelocityAt(middle));
                // q'=relativeSpin*q/2. Bound the rotating-frame derivative
                // and the derivative of atan2(q.Z,q.W), away from its singularity.
                var curvature=(localAcceleration+localSpeedB*localSpeed+localSpeed*localSpeed)/radius+
                    localSpeed*localSpeed/(radius*radius);
                var winding=Math.IEEERemainder(angleEnd-angleStart,Math.Tau);
                if(Math.Abs(rate)>curvature*width*.5)
                    return new(winding,Math.Abs(winding),0);
                var error=curvature*width*width*.5;
                if(error<=distanceTolerance*(width/duration))
                    return new(winding,Math.Abs(winding),error);
            }
            var left=Interval(start,middle); var right=Interval(middle,end);
            return new(left.Winding+right.Winding,left.Distance+right.Distance,left.DistanceError+right.DistanceError);
        }
        Angle(Relative(0));
        if(duration==0)return default;
        var result=default(AngularPathTravel); double time=0;
        while(time<duration)
        {
            var end=Math.Min(duration,Math.Min(a.SegmentEndAfter(time),b.SegmentEndAfter(time)));
            if(end<=time)throw new InvalidOperationException("Angular segment must advance.");
            var next=Interval(time,end);
            result=new(result.Winding+next.Winding,result.Distance+next.Distance,result.DistanceError+next.DistanceError);
            time=end;
        }
        if(!double.IsFinite(result.Winding)||!double.IsFinite(result.Distance)||!double.IsFinite(result.DistanceError))
            throw new InvalidOperationException("Angular path measurement exceeds numeric range.");
        return result;
    }
}
