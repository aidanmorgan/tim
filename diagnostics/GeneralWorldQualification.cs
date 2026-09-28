#if PLAYTEST
using System;
using System.Linq;
using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

internal enum GeneralWorldProbe { ThinWall, RepeatedBounce, RestingBox, RotatingBeam }
internal sealed record GeneralWorldReport(GeneralWorldProbe Probe,int Events,int Steps,double Time,
    double[] Position,double[] Velocity,double Spin,bool ExactRestore,bool ExactReplay);
internal static class GeneralWorldQualification
{
    private static PhysicsObject Object(PhysicsBody body,ConvexInstance shape,double restitution=1)=>
        new(body,new CompoundGeometry([shape]),new(restitution,0,0));
    private static PhysicsObject Wall(int id,double x)=>
        Object(new(new(id),PhysicsMotionType.Static,RigidPose.At(new(x,0,0)),default,default),
            new(new ConvexBox(new(.01,10,10)),Transform3D.Identity));
    internal static GeneralWorldReport Run(GeneralWorldProbe probe)
    {
        var sphere=new ConvexInstance(new ConvexSphere(.5),Transform3D.Identity);
        CollisionVector center,velocity,gravity=default;
        double duration; int steps=1;
        switch(probe)
        {
            case GeneralWorldProbe.ThinWall: center=new(-5,0,0); velocity=new(1000,0,0); duration=.01; break;
            case GeneralWorldProbe.RepeatedBounce: center=default; velocity=new(10,0,0); duration=.8; break;
            case GeneralWorldProbe.RestingBox:
                center=new(0,.5,0); velocity=default; gravity=new(0,-9.8,0); duration=1.0/120; steps=600; break;
            case GeneralWorldProbe.RotatingBeam: center=new(1.5,.8,0); velocity=default; duration=.01; break;
            default: throw new ArgumentOutOfRangeException(nameof(probe));
        }
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(center),velocity,default,1,new(.1,.1,.1));
        PhysicsObject[] objects=probe switch
        {
            GeneralWorldProbe.ThinWall=>[Object(body,sphere),Wall(1,0)],
            GeneralWorldProbe.RepeatedBounce=>[Object(body,sphere),Wall(1,-2),Wall(2,2)],
            GeneralWorldProbe.RestingBox=>[
                Object(body,new(new ConvexBox(new(.5,.5,.5)),Transform3D.Identity),0),
                Object(new(new(1),PhysicsMotionType.Static,RigidPose.At(new(0,-.5,0)),default,default),
                    new(new ConvexBox(new(10,.5,10)),Transform3D.Identity),0)],
            GeneralWorldProbe.RotatingBeam=>[Object(body,sphere),
                Object(new(new(1),PhysicsMotionType.Kinematic,RigidPose.Identity,default,new(0,0,120)),
                    new(new ConvexBox(new(2,.1,.1)),Transform3D.Identity))],
            _=>throw new ArgumentOutOfRangeException(nameof(probe))
        };
        var world=new PhysicsWorld(objects,[],new(gravity,maximumStep:1));
        var before=world.Capture(); var results=new PhysicsStepResult[steps];
        var eventHistory=new PhysicsImpact[steps][]; var events=0;
        for(var i=0;i<steps;i++)
        {
            results[i]=world.Step([],duration); events+=results[i].Events;
            eventHistory[i]=world.Impacts.ToArray();
        }
        var after=world.Capture();
        world.Restore(before);
        var restored=world.Time==before.Time&&world.StepIndex==before.StepIndex&&
            world.Capture().BodyStates.SequenceEqual(before.BodyStates);
        var replay=true;
        for(var i=0;i<steps;i++)
            replay &= world.Step([],duration)==results[i]&&world.Impacts.SequenceEqual(eventHistory[i]);
        replay &= world.Time==after.Time&&world.StepIndex==after.StepIndex&&
            world.Capture().BodyStates.SequenceEqual(after.BodyStates);
        return new(probe,events,steps,world.Time,[body.Center.X,body.Center.Y,body.Center.Z],
            [body.LinearVelocity.X,body.LinearVelocity.Y,body.LinearVelocity.Z],body.AngularVelocity.Length,restored,replay);
    }
}
#endif
