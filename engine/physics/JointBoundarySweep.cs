using System;

namespace CuriousContraptions.Physics;

public enum JointBoundary { Lower, Upper }
public enum JointSweepStatus { Clear, Boundary }
public readonly record struct JointSweepResult(JointSweepStatus Status,double Time,JointBoundary? Boundary,int Iterations);

/// <summary>One conservative scalar-boundary search for declared joint motion.
/// Endpoint/curvature and derivative/curvature certificates bound the entire
/// interval. Every interval ends before either captured spin changes.</summary>
public static class JointBoundarySweep
{
    private const int MaximumIterations=4096;
    private readonly record struct Sample(double Value,double Rate);
    private readonly record struct Interval(Sample Start,Sample End,double Curvature);
    private abstract class Motion(BodyTrajectory a,BodyTrajectory b)
    {
        protected BodyTrajectory A { get; }=a;
        protected BodyTrajectory B { get; }=b;
        public virtual double SegmentEndAfter(double time)=>Math.Min(A.SegmentEndAfter(time),B.SegmentEndAfter(time));
        public abstract Interval Evaluate(double start,double end);
    }
    private sealed class FrameMotion(PhysicsFrameJoint joint,BodyTrajectory a,BodyTrajectory b,JointBoundary boundary):Motion(a,b)
    {
        private double Bound=>boundary==JointBoundary.Lower?joint.TravelRange!.Lower:joint.TravelRange!.Upper;
        private double Sign=>boundary==JointBoundary.Lower?1:-1;
        public override double SegmentEndAfter(double time)
        {
            var end=base.SegmentEndAfter(time);
            if(joint.Kind==FrameJointKind.Hinge)
            {
                var speed=(A.AngularVelocityAt(time)-B.AngularVelocityAt(time)).Length;
                // The relative quaternion stays in this sample's hemisphere.
                if(speed>0) end=Math.Min(end,time+Math.PI/(2*speed));
            }
            return end;
        }
        public override Interval Evaluate(double start,double end)
        {
            var wa=A.AngularVelocityAt(start); var wb=B.AngularVelocityAt(start);
            var pa=A.At(start); var pb=B.At(start);
            var ea=A.At(end); var eb=B.At(end);
            if(joint.Kind==FrameJointKind.Slider)
            {
                Sample At(RigidPose first,RigidPose second)
                {
                    var ra=first.Rotation.Apply(joint.LocalA.Anchor); var rb=second.Rotation.Apply(joint.LocalB.Anchor);
                    var delta=first.Center-second.Center+ra-rb;
                    var axis=(second.Rotation*joint.LocalB.Orientation).Apply(new(0,0,1));
                    var relative=A.LinearVelocity-B.LinearVelocity+CollisionVector.Cross(wa,ra)-CollisionVector.Cross(wb,rb);
                    var rate=CollisionVector.Dot(relative,axis)+CollisionVector.Dot(delta,CollisionVector.Cross(wb,axis));
                    return new(Sign*(CollisionVector.Dot(delta,axis)-Bound),Sign*rate);
                }
                var distance=Math.Max((pa.Center-pb.Center).Length,(ea.Center-eb.Center).Length);
                var speed=(A.LinearVelocity-B.LinearVelocity).Length;
                var curvature=wb.LengthSquared*distance+2*wb.Length*speed+
                    Math.Pow(wa.Length+wb.Length,2)*joint.LocalA.Anchor.Length;
                return new(At(pa,pb),At(ea,eb),curvature);
            }
            if(joint.Kind!=FrameJointKind.Hinge) throw new InvalidOperationException("Only axial frames have travel boundaries.");
            var frameA=pa.Rotation*joint.LocalA.Orientation; var frameB=pb.Rotation*joint.LocalB.Orientation;
            var reference=frameB.Inverse()*frameA;
            var referenceSign=reference.W<0?-1:1;
            Sample Twist(RigidPose first,RigidPose second)
            {
                var fa=first.Rotation*joint.LocalA.Orientation; var fb=second.Rotation*joint.LocalB.Orientation;
                var q=fb.Inverse()*fa;
                var dot=q.X*reference.X+q.Y*reference.Y+q.Z*reference.Z+q.W*reference.W;
                var lift=(dot<0?-1:1)*referenceSign;
                var relative=fb.Inverse().Apply(wa-wb);
                var dz=.5*(relative.Z*q.W+relative.X*q.Y-relative.Y*q.X);
                var dw=-.5*(relative.X*q.X+relative.Y*q.Y+relative.Z*q.Z);
                var cosine=Math.Cos(Bound*.5); var sine=Math.Sin(Bound*.5);
                return new(2*Sign*lift*(q.Z*cosine-q.W*sine),2*Sign*lift*(dz*cosine-dw*sine));
            }
            // q'' = 1/4 B^-1 [(wa-wb)^2 + [wa,wb]] A.
            var bound=.5*(wa-wb).LengthSquared+CollisionVector.Cross(wa,wb).Length;
            return new(Twist(pa,pb),Twist(ea,eb),bound);
        }
    }
    private sealed class RopeMotion(PhysicsRopeJoint joint,BodyTrajectory a,BodyTrajectory b):Motion(a,b)
    {
        public override Interval Evaluate(double start,double end)
        {
            var wa=A.AngularVelocityAt(start); var wb=B.AngularVelocityAt(start);
            Sample At(double time)
            {
                var pa=A.At(time); var pb=B.At(time);
                var ra=pa.Rotation.Apply(joint.LocalA); var rb=pb.Rotation.Apply(joint.LocalB);
                var delta=pa.Center-pb.Center+ra-rb;
                var velocity=A.LinearVelocity-B.LinearVelocity+CollisionVector.Cross(wa,ra)-CollisionVector.Cross(wb,rb);
                // Squared length avoids an undefined gradient at coincident slack endpoints.
                return new((joint.MaximumLength*joint.MaximumLength-delta.LengthSquared)/(2*joint.MaximumLength),
                    -CollisionVector.Dot(delta,velocity)/joint.MaximumLength);
            }
            var ra=joint.LocalA.Length; var rb=joint.LocalB.Length;
            var speed=(A.LinearVelocity-B.LinearVelocity).Length+wa.Length*ra+wb.Length*rb;
            var distance=Math.Max((A.At(start).Center-B.At(start).Center).Length,
                (A.At(end).Center-B.At(end).Center).Length)+ra+rb;
            var acceleration=wa.LengthSquared*ra+wb.LengthSquared*rb;
            return new(At(start),At(end),(speed*speed+distance*acceleration)/joint.MaximumLength);
        }
    }
    private static void Validate(PhysicsJoint joint,BodyTrajectory a,BodyTrajectory b,double duration,double tolerance)
    {
        ArgumentNullException.ThrowIfNull(joint); ArgumentNullException.ThrowIfNull(a); ArgumentNullException.ThrowIfNull(b);
        a.ValidateSource(joint.A); b.ValidateSource(joint.B);
        if(!double.IsFinite(duration)||duration<0||duration>a.Duration||duration>b.Duration||
            !double.IsFinite(tolerance)||tolerance<=0) throw new ArgumentOutOfRangeException(nameof(duration));
    }
    public static JointSweepResult Frame(PhysicsFrameJoint joint,BodyTrajectory a,BodyTrajectory b,double duration,double tolerance)
    {
        Validate(joint,a,b,duration,tolerance);
        if(joint.TravelRange is null) return new(JointSweepStatus.Clear,duration,null,0);
        if(joint.Kind==FrameJointKind.Hinge&&
            (joint.TravelRange.Lower<=-Math.PI+32*tolerance||joint.TravelRange.Upper>=Math.PI-32*tolerance))
            throw new ArgumentException("Hinge range is too close to the principal-angle branch for the requested precision.");
        var lower=Cast(new FrameMotion(joint,a,b,JointBoundary.Lower),duration,tolerance,JointBoundary.Lower);
        var upper=Cast(new FrameMotion(joint,a,b,JointBoundary.Upper),lower.Time,tolerance,JointBoundary.Upper);
        var result=upper.Status==JointSweepStatus.Boundary?upper:lower;
        return result with {Iterations=lower.Iterations+upper.Iterations};
    }
    public static JointSweepResult Rope(PhysicsRopeJoint joint,BodyTrajectory a,BodyTrajectory b,double duration,double tolerance)
    {
        Validate(joint,a,b,duration,tolerance);
        if(joint.MaximumLength<=32*tolerance) throw new ArgumentException("Rope length is too small for the requested precision.");
        return Cast(new RopeMotion(joint,a,b),duration,tolerance,JointBoundary.Upper);
    }
    private static JointSweepResult Cast(Motion motion,double duration,double tolerance,JointBoundary boundary)
    {
        var first=motion.Evaluate(0,0).Start.Value;
        // Like established contact, an active boundary permits a bounded drift
        // before position projection. This budget is fixed for the captured path.
        var allowance=first<=tolerance?4*tolerance:0;
        var eventTolerance=tolerance*.125;
        double time=0;
        for(var iteration=1;iteration<=MaximumIterations;iteration++)
        {
            var end=Math.Min(duration,motion.SegmentEndAfter(time));
            var interval=motion.Evaluate(time,end);
            var gap=interval.Start.Value+allowance; var last=interval.End.Value+allowance;
            var rate=interval.Start.Rate; var curvature=interval.Curvature;
            if(!double.IsFinite(gap)||!double.IsFinite(last)||!double.IsFinite(rate)||!double.IsFinite(curvature)||curvature<0)
                throw new InvalidOperationException("Joint boundary is not numerically representable.");
            if(gap<=eventTolerance) return new(JointSweepStatus.Boundary,time,boundary,iteration);
            if(time==duration) return new(JointSweepStatus.Clear,duration,null,iteration);
            var h=end-time;
            var chordLower=Math.Min(gap,last)-curvature*h*h/8;
            var derivativeLower=Math.Min(gap,gap+rate*h-curvature*h*h*.5);
            if(Math.Max(chordLower,derivativeLower)>eventTolerance) { time=end; continue; }
            var margin=gap-eventTolerance*.5;
            double safe;
            if(curvature==0) safe=rate<0?margin/-rate:h;
            else
            {
                var root=Math.Sqrt(rate*rate+2*curvature*margin);
                safe=rate<0?2*margin/(root-rate):(root+rate)/curvature;
            }
            var next=time+Math.Min(h,safe);
            if(!double.IsFinite(next)||next<=time) throw new InvalidOperationException("Joint boundary sweep made no temporal progress.");
            time=next;
        }
        throw new InvalidOperationException("Joint boundary sweep exhausted its interval budget.");
    }
}
