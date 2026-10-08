#if PLAYTEST
using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

internal sealed record GeneralTrajectoryReport(ConvexSweepStatus Status,double Time,int Segments,
    double InitialSeparation,double FinalSeparation,double ContactSeparation,bool ExactCommittedPose);
internal static class GeneralTrajectoryQualification
{
    internal static GeneralTrajectoryReport Run()
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,
            new(.2,.1,-.1),new(.7,1.1,8),2,new InertiaTensor(1,2,3));
        var path=body.CreateTrajectory(.24,default);
        var moving=new ConvexMotion(new(new ConvexBox(new(2,.05,.05)),AffineTransform.Identity),path);
        var target=path.At(.12).TransformPoint(new(1.5,0,0));
        var fixture=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.At(target),default,default);
        var obstacle=new ConvexMotion(new(new ConvexSphere(.03),AffineTransform.Identity),fixture.CreateTrajectory(.24,default));
        var initial=ConvexDistance.Query(moving.At(0),obstacle.At(0)).LowerBound;
        var final=ConvexDistance.Query(moving.At(.24),obstacle.At(.24)).LowerBound;
        var hit=ConvexSweep.Cast(moving,obstacle,.24,ConvexSweep.ContactDistance);
        var expected=path.At(hit.Time);
        body.Advance(path,hit.Time);
        return new(hit.Status,hit.Time,path.SegmentCount,initial,final,hit.Separation.UpperBound,body.Pose==expected);
    }
}
#endif
