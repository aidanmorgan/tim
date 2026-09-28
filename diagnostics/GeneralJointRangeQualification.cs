#if PLAYTEST
using System;
using System.Linq;
using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

internal enum GeneralJointRangeProbe { HingeLower, HingeUpper, SliderLower, SliderUpper, SliderObstacle }
internal sealed record GeneralJointRangeReport(GeneralJointRangeProbe Probe,double Coordinate,double ReleasedCoordinate,
    double Speed,int Events,double Error,bool ExactRestore,bool ExactReplay);
internal static class GeneralJointRangeQualification
{
    internal static GeneralJointRangeReport Run(GeneralJointRangeProbe probe)
    {
        if(!Enum.IsDefined(probe)) throw new ArgumentOutOfRangeException(nameof(probe));
        var kind=probe is GeneralJointRangeProbe.HingeLower or GeneralJointRangeProbe.HingeUpper?FrameJointKind.Hinge:FrameJointKind.Slider;
        var sign=probe is GeneralJointRangeProbe.HingeLower or GeneralJointRangeProbe.SliderLower?-1:1;
        var a=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,
            kind==FrameJointKind.Slider?new(0,0,sign*10000):default,
            kind==FrameJointKind.Hinge?new(0,0,sign*10000):default,1,new(.1,.1,.1));
        var b=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var origin=new JointFrame(default,RigidRotation.Identity);
        var joint=new PhysicsFrameJoint(new(0),kind,a,origin,b,origin,ConnectedBodyCollision.Disabled,new(-.5,.5));
        PhysicsObject Object(PhysicsBody body)=>new(body,new([new(new ConvexSphere(.1),Transform3D.Identity)]),new(0,0,0));
        PhysicsObject[] objects;
        if(probe==GeneralJointRangeProbe.SliderObstacle)
        {
            var wall=new PhysicsBody(new(2),PhysicsMotionType.Static,RigidPose.At(new(0,0,.3)),default,default);
            objects=[Object(a),Object(b),new(wall,new([new(new ConvexBox(new(1,1,.001)),Transform3D.Identity)]),new(0,0,0))];
        }
        else objects=[Object(a),Object(b)];
        var world=new PhysicsWorld(objects,[joint],new(default));
        var before=world.Capture();
        var result=world.Step([],.01);
        var coordinate=joint.Travel.Error;
        var speed=a.LinearVelocity.Length+a.AngularVelocity.Length;
        var error=joint.Error(1e-8);
        var after=world.Capture();
        world.Restore(before);
        var restored=world.Capture().BodyStates.SequenceEqual(before.BodyStates)&&world.Time==before.Time&&world.StepIndex==before.StepIndex;
        var replay=world.Step([],.01)==result&&world.Capture().BodyStates.SequenceEqual(after.BodyStates)&&world.Time==after.Time&&world.StepIndex==after.StepIndex;
        if(kind==FrameJointKind.Slider) a.ApplyImpulse(new(0,0,-sign),a.Center);
        else a.ApplyWrench(default,new(0,0,-sign*.1),1);
        world.Step([],.01);
        return new(probe,coordinate,joint.Travel.Error,speed,result.Events,error,restored,replay);
    }
}
#endif
