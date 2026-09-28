#if PLAYTEST
using System;
using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

internal enum GeneralSignedSweepProbe { SpinningSlide, BoxSlide, OrbitRecontact, GrazingReturn }
internal sealed record GeneralSignedSweepReport(GeneralSignedSweepProbe Probe,ConvexSweepStatus Status,
    double Time,double Duration,double MinimumSeparation,double InitialLower,double MidLower,double Lower,double Upper,
    int Iterations,double RotationalReach,bool ExactCommittedPose,bool ExactRestore,bool ExactReplay);
internal static class GeneralSignedSweepQualification
{
    internal static GeneralSignedSweepReport Run(GeneralSignedSweepProbe probe)
    {
        const double minimum=-1e-6;
        ConvexInstance shape,obstacleShape;
        RigidPose pose,obstaclePose;
        CollisionVector velocity,spin;
        double duration;
        switch(probe)
        {
            case GeneralSignedSweepProbe.SpinningSlide:
            case GeneralSignedSweepProbe.BoxSlide:
                shape=new(probe==GeneralSignedSweepProbe.SpinningSlide?new ConvexSphere(.5):new ConvexBox(new(.5,.5,.5)),Transform3D.Identity);
                obstacleShape=new(new ConvexBox(new(100,1,100)),Transform3D.Identity);
                pose=RigidPose.At(new(0,.5,0)); obstaclePose=RigidPose.At(new(0,-1,0));
                velocity=new(50,0,0); spin=probe==GeneralSignedSweepProbe.SpinningSlide?new(0,0,1000000):default;
                duration=1;
                break;
            case GeneralSignedSweepProbe.OrbitRecontact:
                shape=new(new ConvexSphere(.2),new(Basis.Identity,new(1,0,0)));
                obstacleShape=new(new ConvexSphere(.2),Transform3D.Identity);
                pose=RigidPose.Identity; obstaclePose=RigidPose.At(new(1,-.4,0));
                velocity=default; spin=new(0,0,120); duration=2*Math.PI/120;
                break;
            case GeneralSignedSweepProbe.GrazingReturn:
                shape=new(new ConvexSphere(.2),new(Basis.Identity,new(0,1,0)));
                obstacleShape=new(new ConvexSphere(.2),Transform3D.Identity);
                pose=RigidPose.Identity; obstaclePose=RigidPose.At(new(0,1.4,0));
                velocity=default; spin=new(0,0,1); duration=2*Math.PI;
                break;
            default: throw new ArgumentOutOfRangeException(nameof(probe));
        }
        var body=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,pose,velocity,spin);
        var obstacle=new PhysicsBody(new(1),PhysicsMotionType.Static,obstaclePose,default,default);
        var initial=body.Snapshot();
        var path=body.CreateTrajectory(duration);
        var moving=new ConvexMotion(shape,path);
        var fixedMotion=new ConvexMotion(obstacleShape,obstacle.CreateTrajectory(duration));
        var first=ConvexSeparation.Query(moving.At(0),fixedMotion.At(0)).LowerBound;
        var middle=ConvexSeparation.Query(moving.At(duration*.5),fixedMotion.At(duration*.5)).LowerBound;
        var hit=ConvexSweep.Cast(moving,fixedMotion,duration,minimum);
        var expected=path.At(hit.Time);
        body.Advance(path,hit.Time); var committed=body.Pose==expected;
        body.Restore(initial); var restored=body.Snapshot()==initial;
        var replay=ConvexSweep.Cast(new(shape,body.CreateTrajectory(duration)),fixedMotion,duration,minimum);
        return new(probe,hit.Status,hit.Time,duration,minimum,first,middle,hit.Separation.LowerBound,hit.Separation.UpperBound,
            hit.Iterations,moving.RotationalReach,committed,restored,hit==replay);
    }
}
#endif
