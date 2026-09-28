#if PLAYTEST
using System;
using System.Linq;
using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

internal enum GeneralJointBoundaryProbe { HingeStop, SliderStop, RopeStop, RotatingSlider, RotatingRope }
internal sealed record GeneralJointBoundaryReport(GeneralJointBoundaryProbe Probe,double HitTime,double Coordinate,
    double InitialSpeed,double? AfterInitialSolveSpeed,double FinalSpeed,int Events,bool ExactRestore,bool ExactReplay);
internal static class GeneralJointBoundaryQualification
{
    private static readonly JointFrame Origin=new(default,RigidRotation.Identity);
    private static PhysicsBody Body(int id,CollisionVector center,CollisionVector velocity=default,CollisionVector spin=default)=>
        new(new(id),PhysicsMotionType.Dynamic,RigidPose.At(center),velocity,spin,1,new(.1,.1,.1));
    private static PhysicsObject Object(PhysicsBody body)=>new(body,new([new(new ConvexSphere(.01),Transform3D.Identity)]),new(0,0,0));
    internal static GeneralJointBoundaryReport Run(GeneralJointBoundaryProbe probe)
    {
        if(!Enum.IsDefined(probe)) throw new ArgumentOutOfRangeException(nameof(probe));
        if(probe is GeneralJointBoundaryProbe.RotatingSlider or GeneralJointBoundaryProbe.RotatingRope) return Rotating(probe);
        var hinge=probe==GeneralJointBoundaryProbe.HingeStop;
        var rope=probe==GeneralJointBoundaryProbe.RopeStop;
        var a=Body(0,rope?new(.5,0,0):default,hinge?default:rope?new(100,0,0):new(0,0,100),
            hinge?new(0,0,100):default);
        var b=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        PhysicsJoint joint=rope?new PhysicsRopeJoint(new(0),a,default,b,default,1,ConnectedBodyCollision.Disabled):
            new PhysicsFrameJoint(new(0),hinge?FrameJointKind.Hinge:FrameJointKind.Slider,a,Origin,b,Origin,ConnectedBodyCollision.Disabled,new(-.5,.5));
        var speed=a.LinearVelocity.Length+a.AngularVelocity.Length;
        ImpulseSolver.Solve(joint.VelocityConstraints(1e-7));
        var afterSolve=a.LinearVelocity.Length+a.AngularVelocity.Length;
        var world=new PhysicsWorld([Object(a),Object(b)],[joint],new(default,maximumStep:.01));
        var before=world.Capture(); var result=world.Step(.01); var stops=world.JointStops.ToArray();
        var after=world.Capture();
        var coordinate=rope?a.Center.X:((PhysicsFrameJoint)joint).Travel.Error;
        var finalSpeed=a.LinearVelocity.Length+a.AngularVelocity.Length;
        world.Restore(before);
        var restored=world.Capture().BodyStates.SequenceEqual(before.BodyStates)&&world.Time==before.Time&&world.StepIndex==before.StepIndex&&world.JointStops.Length==0;
        var replay=world.Step(.01)==result&&world.JointStops.SequenceEqual(stops)&&world.Capture().BodyStates.SequenceEqual(after.BodyStates)&&world.Time==after.Time&&world.StepIndex==after.StepIndex;
        return new(probe,stops.Single().Time,coordinate,speed,afterSolve,finalSpeed,result.Events,restored,replay);
    }
    private static GeneralJointBoundaryReport Rotating(GeneralJointBoundaryProbe probe)
    {
        var slider=probe==GeneralJointBoundaryProbe.RotatingSlider;
        var a=Body(0,slider?new(0,1,0):default,spin:slider?default:new(0,0,120));
        var b=slider?new PhysicsBody(new(1),PhysicsMotionType.Kinematic,RigidPose.Identity,default,new(0,0,120)):
            new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.At(new(1,0,0)),default,default);
        var frame=new JointFrame(default,RigidRotation.FromRotationVector(new(0,Math.PI/2,0)));
        PhysicsJoint joint=slider?new PhysicsFrameJoint(new(0),FrameJointKind.Slider,a,frame,b,frame,ConnectedBodyCollision.Disabled,new(-.5,.5)):
            new PhysicsRopeJoint(new(0),a,new(1,0,0),b,default,1,ConnectedBodyCollision.Disabled);
        var beforeA=a.Snapshot(); var beforeB=b.Snapshot();
        var duration=Math.Tau/120;
        JointSweepResult Sweep()
        {
            var pa=a.CreateTrajectory(duration); var pb=b.CreateTrajectory(duration);
            var hit=joint.Sweep(pa,pb,duration,1e-7);
            if(hit.Status!=JointSweepStatus.Boundary) throw new InvalidOperationException("Expected an intermediate boundary.");
            a.Advance(pa,hit.Time); b.Advance(pb,hit.Time);
            return hit;
        }
        var initialSpeed=a.LinearVelocity.Length+a.AngularVelocity.Length+b.LinearVelocity.Length+b.AngularVelocity.Length;
        var result=Sweep(); var afterA=a.Snapshot(); var afterB=b.Snapshot();
        var coordinate=slider?((PhysicsFrameJoint)joint).Travel.Error:
            (((PhysicsRopeJoint)joint).PointA-((PhysicsRopeJoint)joint).PointB).Length;
        a.Restore(beforeA); b.Restore(beforeB);
        var restored=a.Snapshot()==beforeA&&b.Snapshot()==beforeB;
        var replay=Sweep()==result&&a.Snapshot()==afterA&&b.Snapshot()==afterB;
        var finalSpeed=a.LinearVelocity.Length+a.AngularVelocity.Length+b.LinearVelocity.Length+b.AngularVelocity.Length;
        return new(probe,result.Time,coordinate,initialSpeed,null,finalSpeed,1,restored,replay);
    }
}
#endif
