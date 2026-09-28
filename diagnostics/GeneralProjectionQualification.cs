#if PLAYTEST
using System;
using System.Linq;
using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

internal enum GeneralProjectionProbe { Translation, Simultaneous, FullTurn, BlockedJoint, ClearJoint }
internal enum GeneralProjectionOutcome { Applied, Clipped, Rejected }
internal sealed record GeneralProjectionReport(GeneralProjectionProbe Probe,GeneralProjectionOutcome Outcome,
    double Fraction,int SweepIterations,double[] Position,bool ExpectedVelocityState,bool ExactRestore,bool ExactReplay);
internal static class GeneralProjectionQualification
{
    private static PhysicsBody Body(int id,CollisionVector center,PhysicsMotionType type)=>
        type==PhysicsMotionType.Dynamic?new(new(id),type,RigidPose.At(center),new(.1,.2,.3),new(.2,.3,.4),1,new(.1,.1,.1)):
            new(new(id),type,RigidPose.At(center),default,default);
    private static readonly ConvexInstance Sphere=new(new ConvexSphere(.1),Transform3D.Identity);
    private static readonly ConvexInstance Wall=new(new ConvexBox(new(.001,2,2)),Transform3D.Identity);
    internal static GeneralProjectionReport Run(GeneralProjectionProbe probe)
    {
        if(!Enum.IsDefined(probe)) throw new ArgumentOutOfRangeException(nameof(probe));
        if(probe is GeneralProjectionProbe.BlockedJoint or GeneralProjectionProbe.ClearJoint) return Joint(probe);
        var a=Body(0,probe==GeneralProjectionProbe.FullTurn?default:new(-1,0,0),PhysicsMotionType.Dynamic);
        var b=Body(1,probe switch
        {
            GeneralProjectionProbe.Simultaneous=>new(1,0,0),
            GeneralProjectionProbe.FullTurn=>new(1.5*Math.Cos(.4),1.5*Math.Sin(.4),0),
            _=>default
        },probe==GeneralProjectionProbe.Simultaneous?PhysicsMotionType.Dynamic:PhysicsMotionType.Static);
        var shapeA=probe==GeneralProjectionProbe.FullTurn?new ConvexInstance(new ConvexBox(new(2,.02,.02)),Transform3D.Identity):Sphere;
        var shapeB=probe switch
        {
            GeneralProjectionProbe.FullTurn=>new ConvexInstance(new ConvexSphere(.03),Transform3D.Identity),
            GeneralProjectionProbe.Simultaneous=>Sphere,
            _=>Wall
        };
        BodyCorrection[] corrections=probe switch
        {
            GeneralProjectionProbe.FullTurn=>[new(a,default,new(0,0,Math.Tau))],
            GeneralProjectionProbe.Simultaneous=>[new(a,new(2,0,0),default),new(b,new(-2,0,0),default)],
            _=>[new(a,new(2,0,0),default)]
        };
        var beforeA=a.Snapshot(); var beforeB=b.Snapshot();
        PositionProjector Projector()=>new([a,b],[new(a,shapeA,b,shapeB)],1e-6);
        var projector=Projector(); var fraction=projector.Apply(corrections);
        var afterA=a.Snapshot(); var afterB=b.Snapshot();
        var momentum=a.LinearVelocity==beforeA.LinearVelocity&&a.AngularMomentum==beforeA.AngularMomentum&&
            b.LinearVelocity==beforeB.LinearVelocity&&b.AngularMomentum==beforeB.AngularMomentum;
        a.Restore(beforeA); b.Restore(beforeB);
        var restored=a.Snapshot()==beforeA&&b.Snapshot()==beforeB;
        var replayProjector=Projector();
        var replay=replayProjector.Apply(corrections)==fraction&&a.Snapshot()==afterA&&b.Snapshot()==afterB&&
            replayProjector.SweepIterations==projector.SweepIterations;
        return new(probe,fraction<1?GeneralProjectionOutcome.Clipped:GeneralProjectionOutcome.Applied,fraction,
            projector.SweepIterations,[a.Center.X,a.Center.Y,a.Center.Z],momentum,restored,replay);
    }
    private static GeneralProjectionReport Joint(GeneralProjectionProbe probe)
    {
        var a=Body(0,new(-1,0,0),PhysicsMotionType.Dynamic); var anchor=Body(1,new(1,0,0),PhysicsMotionType.Static);
        var wall=Body(2,new(0,probe==GeneralProjectionProbe.BlockedJoint?0:3,0),PhysicsMotionType.Static);
        var origin=new JointFrame(default,RigidRotation.Identity);
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.BallSocket,a,origin,anchor,origin,ConnectedBodyCollision.Disabled,null);
        PhysicsObject Object(PhysicsBody body,ConvexInstance shape)=>new(body,new([shape]),new(0,0,0));
        var world=new PhysicsWorld([Object(a,Sphere),Object(anchor,Sphere),Object(wall,Wall)],[joint],new(default));
        var before=world.Capture();
        GeneralProjectionOutcome Step()
        {
            try { world.Step(.01); return GeneralProjectionOutcome.Applied; }
            catch(InvalidOperationException) { return GeneralProjectionOutcome.Rejected; }
        }
        var outcome=Step(); var after=world.Capture();
        world.Restore(before);
        var restored=world.Capture().BodyStates.SequenceEqual(before.BodyStates)&&world.Time==before.Time&&world.StepIndex==before.StepIndex;
        var replay=Step()==outcome&&world.Capture().BodyStates.SequenceEqual(after.BodyStates)&&world.Time==after.Time&&world.StepIndex==after.StepIndex;
        var momentum=outcome==GeneralProjectionOutcome.Rejected?
            world.Capture().BodyStates.SequenceEqual(before.BodyStates):
            a.AngularMomentum==before.BodyStates[0].AngularMomentum&&a.LinearVelocity.Length<=1e-8;
        return new(probe,outcome,outcome==GeneralProjectionOutcome.Applied?1:0,0,
            [a.Center.X,a.Center.Y,a.Center.Z],momentum,restored,replay);
    }
}
#endif
