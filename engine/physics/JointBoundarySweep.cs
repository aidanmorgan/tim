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
    private readonly record struct Interval(Sample Start,Sample End,double Curvature,double ChordCurvature);
    private abstract class Motion(BodyTrajectory[] paths)
    {
        protected BodyTrajectory[] Paths { get; }=paths;
        public virtual double SegmentEndAfter(double time)
        {
            var end=double.PositiveInfinity;
            foreach(var path in Paths) end=Math.Min(end,path.SegmentEndAfter(time));
            return end;
        }
        public abstract Interval Evaluate(double start,double end);
    }
    private sealed class FrameMotion(PhysicsFrameJoint joint,BodyTrajectory a,BodyTrajectory b,JointBoundary boundary):Motion([a,b])
    {
        private BodyTrajectory A=>a;
        private BodyTrajectory B=>b;
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
                return new(At(pa,pb),At(ea,eb),curvature,curvature);
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
            return new(Twist(pa,pb),Twist(ea,eb),bound,bound);
        }
    }
    private sealed class RopeMotion : Motion
    {
        private readonly PhysicsRopeJoint _joint;
        private readonly int[] _indices;
        public RopeMotion(PhysicsRopeJoint joint,BodyTrajectory[] paths):base(paths)
        {
            _joint=joint; _indices=new int[joint.Route.Anchors.Length];
            for(var i=0;i<_indices.Length;i++) _indices[i]=joint.BodyIndex(joint.Route.Anchors[i].Body);
        }
        public override Interval Evaluate(double start,double end)
        {
            var anchors=_joint.Route.Anchors;
            double first=0,last=0,rate=0,curvature=0,chord=0;
            for(var i=1;i<anchors.Length;i++)
            {
                var aa=anchors[i-1]; var ab=anchors[i];
                if(aa.Body==ab.Body)
                {
                    var length=(aa.LocalPosition-ab.LocalPosition).Length;
                    first+=length; last+=length; continue;
                }
                var a=Paths[_indices[i-1]]; var b=Paths[_indices[i]];
                var pa=a.At(start); var pb=b.At(start);
                var wa=a.AngularVelocityAt(start); var wb=b.AngularVelocityAt(start);
                var ra=pa.Rotation.Apply(aa.LocalPosition); var rb=pb.Rotation.Apply(ab.LocalPosition);
                var delta=pa.Center-pb.Center+ra-rb; var distance=delta.Length;
                var velocity=a.LinearVelocity-b.LinearVelocity+CollisionVector.Cross(wa,ra)-CollisionVector.Cross(wb,rb);
                var acceleration=wa.LengthSquared*aa.LocalPosition.Length+wb.LengthSquared*ab.LocalPosition.Length;
                first+=distance;
                last+=(a.At(end).TransformPoint(aa.LocalPosition)-b.At(end).TransformPoint(ab.LocalPosition)).Length;
                // ||r(t)|| <= ||r0+v0*t|| + A*t²/2. Concavity of sqrt
                // bounds the first norm by d + dot(r0/d,v0)*t + ||v0||²*t²/(2*d).
                // At d=0, ||v0||*t is exact for the linear term. Neither
                // certificate divides by a future minimum separation.
                rate-=distance==0?velocity.Length:CollisionVector.Dot(delta/distance,velocity);
                curvature+=acceleration+(distance==0?0:velocity.LengthSquared/distance);
                // Vector interpolation error is <= A*h²/8; convexity of norm
                // bounds its linear interpolation by the endpoint lengths.
                chord+=acceleration;
            }
            return new(new(_joint.MaximumLength-first,rate),new(_joint.MaximumLength-last,0),curvature,chord);
        }
    }
    public static JointSweepResult Frame(PhysicsFrameJoint joint,ReadOnlySpan<BodyTrajectory> paths,double duration,double tolerance)
    {
        ArgumentNullException.ThrowIfNull(joint); joint.ValidatePaths(paths,duration,tolerance);
        var a=paths[joint.BodyIndex(joint.A)]; var b=paths[joint.BodyIndex(joint.B)];
        if(joint.TravelRange is null) return new(JointSweepStatus.Clear,duration,null,0);
        if(joint.Kind==FrameJointKind.Hinge&&
            (joint.TravelRange.Lower<=-Math.PI+32*tolerance||joint.TravelRange.Upper>=Math.PI-32*tolerance))
            throw new ArgumentException("Hinge range is too close to the principal-angle branch for the requested precision.");
        var lower=Cast(new FrameMotion(joint,a,b,JointBoundary.Lower),duration,tolerance,JointBoundary.Lower);
        var upper=Cast(new FrameMotion(joint,a,b,JointBoundary.Upper),lower.Time,tolerance,JointBoundary.Upper);
        var result=upper.Status==JointSweepStatus.Boundary?upper:lower;
        return result with {Iterations=lower.Iterations+upper.Iterations};
    }
    public static JointSweepResult Rope(PhysicsRopeJoint joint,ReadOnlySpan<BodyTrajectory> paths,double duration,double tolerance)
    {
        ArgumentNullException.ThrowIfNull(joint); joint.ValidatePaths(paths,duration,tolerance);
        if(joint.MaximumLength<=32*tolerance) throw new ArgumentException("Rope length is too small for the requested precision.");
        return Cast(new RopeMotion(joint,paths.ToArray()),duration,tolerance,JointBoundary.Upper);
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
            if(!double.IsFinite(gap)||!double.IsFinite(last)||!double.IsFinite(rate)||!double.IsFinite(curvature)||curvature<0||!double.IsFinite(interval.ChordCurvature)||interval.ChordCurvature<0)
                throw new InvalidOperationException("Joint boundary is not numerically representable.");
            if(gap<=eventTolerance) return new(JointSweepStatus.Boundary,time,boundary,iteration);
            if(time==duration) return new(JointSweepStatus.Clear,duration,null,iteration);
            var h=end-time;
            var chordLower=Math.Min(gap,last)-interval.ChordCurvature*h*h/8;
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
