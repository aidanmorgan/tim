#if PLAYTEST
using System;
using System.Linq;
using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

internal enum GeneralMotorProbe { SliderSupply, HingeSupply, BrakeReverse, Disabled, StopEvent }
internal sealed record GeneralMotorReport(GeneralMotorProbe Probe,double Speed,double Coordinate,double Energy,
    PhysicsMotorUse Use,int Events,bool ExactRestore,bool ExactReplay);
internal static class GeneralMotorQualification
{
    internal static GeneralMotorReport Run(GeneralMotorProbe probe)
    {
        if(!Enum.IsDefined(probe)) throw new ArgumentOutOfRangeException(nameof(probe));
        var kind=probe==GeneralMotorProbe.HingeSupply?FrameJointKind.Hinge:FrameJointKind.Slider;
        var velocity=probe==GeneralMotorProbe.BrakeReverse?new CollisionVector(0,0,3):
            probe==GeneralMotorProbe.Disabled?new CollisionVector(0,0,2):default;
        var a=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,velocity,default,1,new(.1,.1,.1));
        var b=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var origin=new JointFrame(default,RigidRotation.Identity);
        var joint=new PhysicsFrameJoint(new(0),kind,a,origin,b,origin,ConnectedBodyCollision.Disabled,
            probe==GeneralMotorProbe.StopEvent?new(-.02,.02):null,JointTravelDirection.Both);
        var duration=probe is GeneralMotorProbe.SliderSupply or GeneralMotorProbe.HingeSupply?.5:.1;
        var maximumStep=probe is GeneralMotorProbe.BrakeReverse or GeneralMotorProbe.StopEvent?.1:1.0/120;
        PhysicsObject Object(PhysicsBody body)=>new(body,new([new(new ConvexSphere(.01),AffineTransform.Identity)]),new(0,0,0));
        var world=new PhysicsWorld([],[Object(a),Object(b)],[joint],new(default,maximumStep:maximumStep));
        var command=probe switch
        {
            GeneralMotorProbe.SliderSupply or GeneralMotorProbe.HingeSupply=>new PhysicsMotorCommand(joint.Id,10,2,.5,1000000),
            GeneralMotorProbe.BrakeReverse=>new PhysicsMotorCommand(joint.Id,-10,100,.5,1000000),
            GeneralMotorProbe.Disabled=>new PhysicsMotorCommand(joint.Id,-10,0,3,1000000),
            GeneralMotorProbe.StopEvent=>new PhysicsMotorCommand(joint.Id,1,100,100,1000000),
            _=>throw new ArgumentOutOfRangeException(nameof(probe))
        };
        var before=world.Capture(); var result=world.Step([],[command],duration);
        var after=world.Capture(); var use=world.MotorUse.ToArray(); var stops=world.JointStops.ToArray();
        var speed=a.LinearVelocity.Z+a.AngularVelocity.Z; var energy=a.KineticEnergy;
        var coordinate=joint.Travel.Error;
        world.Restore(before);
        var restored=world.Capture().BodyStates.SequenceEqual(before.BodyStates)&&world.Time==before.Time&&
            world.StepIndex==before.StepIndex&&world.MotorUse.Length==0&&world.JointStops.Length==0;
        var replay=world.Step([],[command],duration)==result&&world.Capture().BodyStates.SequenceEqual(after.BodyStates)&&
            world.MotorUse.SequenceEqual(use)&&world.JointStops.SequenceEqual(stops)&&world.Time==after.Time&&world.StepIndex==after.StepIndex;
        return new(probe,speed,coordinate,energy,use.Single(),result.Events,restored,replay);
    }
}
#endif
