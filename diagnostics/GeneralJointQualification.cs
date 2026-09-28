#if PLAYTEST
using System;
using System.Linq;
using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

internal enum GeneralJointProbe { HingePendulum, Slider, SlackRope, TautRope, CoupledImpact }
internal sealed record GeneralJointReport(GeneralJointProbe Probe,int Steps,int Events,int SweepIterations,
    double MaximumError,double MaximumVelocityResidual,double[] Position,double[] Velocity,double[] Spin,
    double? PayloadVelocityY,bool ExactRestore,bool ExactReplay);
internal static class GeneralJointQualification
{
    private static PhysicsObject Object(PhysicsBody body)=>new(body,
        new CompoundGeometry([new(new ConvexSphere(.1),Transform3D.Identity)]),new(0,0,0));
    internal static GeneralJointReport Run(GeneralJointProbe probe)
    {
        CollisionVector center=default,velocity=default,spin=default,gravity=default;
        int steps;
        switch(probe)
        {
            case GeneralJointProbe.HingePendulum: center=new(1,0,0); gravity=new(0,-9.8,0); steps=240; break;
            case GeneralJointProbe.Slider: velocity=new(2,3,4); spin=new(1,2,3); gravity=new(1,2,3); steps=120; break;
            case GeneralJointProbe.SlackRope: center=new(.5,0,0); velocity=new(.1,0,0); steps=120; break;
            case GeneralJointProbe.TautRope: center=new(1,0,0); velocity=new(2,0,0); steps=120; break;
            case GeneralJointProbe.CoupledImpact: steps=12; break;
            default: throw new ArgumentOutOfRangeException(nameof(probe));
        }
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(center),velocity,spin,1,new(.1,.1,.1));
        var anchor=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var origin=new JointFrame(default,RigidRotation.Identity);
        PhysicsJoint joint=probe switch
        {
            GeneralJointProbe.HingePendulum=>new PhysicsFrameJoint(new(0),FrameJointKind.Hinge,body,
                new(new(-1,0,0),RigidRotation.Identity),anchor,origin,ConnectedBodyCollision.Disabled,null),
            GeneralJointProbe.Slider=>new PhysicsFrameJoint(new(0),FrameJointKind.Slider,body,origin,anchor,origin,ConnectedBodyCollision.Disabled,null),
            GeneralJointProbe.SlackRope or GeneralJointProbe.TautRope=>new PhysicsRopeJoint(new(0),body,default,anchor,default,1,ConnectedBodyCollision.Disabled),
            GeneralJointProbe.CoupledImpact=>new PhysicsFrameJoint(new(0),FrameJointKind.Hinge,body,origin,anchor,origin,ConnectedBodyCollision.Disabled,null),
            _=>throw new ArgumentOutOfRangeException(nameof(probe))
        };
        PhysicsBody? payload=null;
        PhysicsObject[] objects;
        if(probe==GeneralJointProbe.CoupledImpact)
        {
            payload=new(new(2),PhysicsMotionType.Dynamic,RigidPose.At(new(1,1,0)),new(0,-10,0),default,1,new(.1,.1,.1));
            var beam=new CompoundGeometry([new(new ConvexBox(new(2,.1,.1)),Transform3D.Identity)]);
            objects=[new(body,beam,new(0,0,0)),Object(anchor),Object(payload)];
        }
        else objects=[Object(body),Object(anchor)];
        var world=new PhysicsWorld(objects,[joint],new(gravity));
        var before=world.Capture();
        const double duration=1.0/120;
        var results=new PhysicsStepResult[steps]; var impacts=new PhysicsImpact[steps][];
        double error=0,residual=0; var events=0; var sweeps=0;
        for(var i=0;i<steps;i++)
        {
            results[i]=world.Step(duration); impacts[i]=world.Impacts.ToArray();
            events+=results[i].Events; sweeps+=results[i].SweepIterations;
            error=Math.Max(error,joint.Error(1e-8));
            residual=Math.Max(residual,joint.VelocityConstraints(1e-7).Select(c=>c.Residual).DefaultIfEmpty(0).Max());
        }
        var after=world.Capture();
        world.Restore(before);
        var restored=world.Time==before.Time&&world.StepIndex==before.StepIndex&&world.Capture().BodyStates.SequenceEqual(before.BodyStates);
        var replay=true;
        for(var i=0;i<steps;i++)
            replay &= world.Step(duration)==results[i]&&world.Impacts.SequenceEqual(impacts[i]);
        replay &= world.Time==after.Time&&world.StepIndex==after.StepIndex&&world.Capture().BodyStates.SequenceEqual(after.BodyStates);
        return new(probe,steps,events,sweeps,error,residual,
            [body.Center.X,body.Center.Y,body.Center.Z],[body.LinearVelocity.X,body.LinearVelocity.Y,body.LinearVelocity.Z],
            [body.AngularVelocity.X,body.AngularVelocity.Y,body.AngularVelocity.Z],payload?.LinearVelocity.Y,restored,replay);
    }
}
#endif
