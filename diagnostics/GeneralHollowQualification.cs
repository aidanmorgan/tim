#if PLAYTEST
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

internal enum GeneralHollowProbe { TubePassage, TubeWall, FunnelPassage, FunnelWall, Bend45Passage, Bend90Passage, Bend45Wall, Bend90Wall, JoinedTube }
internal sealed record GeneralHollowReport(GeneralHollowProbe Probe,int Children,double MaximumSurfaceError,double MinimumBoreRadius,
    ConvexSweepStatus? Status,double? Time,int? NarrowPhaseCalls,int? WorldEvents,double[]? Position,double[]? Velocity,
    bool? ExactRestore,bool ExactReplay,double ElapsedMilliseconds);
internal static class GeneralHollowQualification
{
    private static BodyTrajectory Path(RigidPose pose,CollisionVector velocity=default,CollisionVector spin=default,double duration=1)=>
        new PhysicsBody(new(0),PhysicsMotionType.Kinematic,pose,velocity,spin).CreateTrajectory(duration,default);
    internal static GeneralHollowReport Run(GeneralHollowProbe probe)
    {
        if(!Enum.IsDefined(probe)) throw new ArgumentOutOfRangeException(nameof(probe));
        var watch=Stopwatch.StartNew();
        if(probe==GeneralHollowProbe.JoinedTube) return Joined(watch);
        var bend=probe is GeneralHollowProbe.Bend45Passage or GeneralHollowProbe.Bend90Passage or GeneralHollowProbe.Bend45Wall or GeneralHollowProbe.Bend90Wall;
        var funnel=probe is GeneralHollowProbe.FunnelPassage or GeneralHollowProbe.FunnelWall;
        var wall=probe is GeneralHollowProbe.TubeWall or GeneralHollowProbe.FunnelWall or GeneralHollowProbe.Bend45Wall or GeneralHollowProbe.Bend90Wall;
        var sweep=probe is GeneralHollowProbe.Bend45Passage or GeneralHollowProbe.Bend45Wall?Math.PI/4:Math.PI/2;
        var shape=bend?HollowGeometry.Bend(2.4,sweep,.65,.7,new(.005)):
            funnel?HollowGeometry.Frustum(.9,1.3,.65,.05,new(.005)):HollowGeometry.Tube(2,.65,.7,new(.005));
        CompoundMotion moving,shell; double duration;
        if(bend)
        {
            duration=wall?.002:1;
            var center=new CollisionVector(2.4*Math.Sin(sweep*.5),2.4*Math.Cos(sweep*.5),0);
            moving=wall?new(new([new(new ConvexSphere(.05),AffineTransform.Identity)]),Path(RigidPose.At(center),new(0,0,1000),duration:duration)):
                new(new([new(new ConvexSphere(.25),new(AffineBasis.Identity,new(0,2.4f,0)))]),Path(RigidPose.Identity,spin:new(0,0,-sweep)));
            shell=new(shape.Geometry,Path(RigidPose.Identity));
        }
        else
        {
            duration=.008;
            var rotation=RigidRotation.FromRotationVector(new(.2,.7,-.4)); var center=new CollisionVector(2,3,-1);
            var height=wall?(funnel?1.325:.675):0;
            moving=new(new([new(new ConvexSphere(.05),AffineTransform.Identity)]),
                Path(new(center+rotation.Apply(new(-4,height,0)),rotation),rotation.Apply(new(1000,0,0)),duration:duration));
            shell=new(shape.Geometry,Path(new(center,rotation),duration:duration));
        }
        var result=CompoundCollision.Cast(moving,shell,duration,ConvexSweep.ContactDistance);
        var replay=CompoundCollision.Cast(moving,shell,duration,ConvexSweep.ContactDistance)==result;
        return new(probe,shape.Geometry.Count,shape.MaximumSurfaceError,shape.MinimumBoreRadius,result.Status,result.Time,
            result.NarrowPhaseCalls,null,null,null,null,replay,watch.Elapsed.TotalMilliseconds);
    }
    private static GeneralHollowReport Joined(Stopwatch watch)
    {
        var tube=HollowGeometry.Tube(1,.65,.7,new(.005)); var children=new List<ConvexInstance>();
        for(var section=0;section<2;section++)
        for(var i=0;i<tube.Geometry.Count;i++)
        {
            var child=tube.Geometry[new(i)];
            children.Add(new(child.Geometry,new(AffineBasis.Identity,child.Pose.Origin+new CollisionVector(section*2,0,0))));
        }
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(-.5,-.44,0)),new(2,0,0),default,1,new(.016,.016,.016));
        var wall=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var world=new PhysicsWorld([],[new(body,new([new(new ConvexSphere(.2),AffineTransform.Identity)]),new(0,0,0)),
            new(wall,new(children.ToArray()),new(0,0,0))],[],new(new(0,-9.8,0)));
        var before=world.Capture();
        int Run()
        {
            var events=0;
            for(var i=0;i<120;i++) events+=world.Step([],[],1.0/120).Events;
            return events;
        }
        var events=Run(); var after=world.Capture(); var impacts=world.Impacts.ToArray();
        var position=body.Center; var velocity=body.LinearVelocity;
        world.Restore(before);
        var restored=world.Capture().BodyStates.SequenceEqual(before.BodyStates)&&world.Time==before.Time&&world.StepIndex==before.StepIndex&&world.Impacts.Length==0;
        var replay=Run()==events&&world.Capture().BodyStates.SequenceEqual(after.BodyStates)&&world.Time==after.Time&&
            world.StepIndex==after.StepIndex&&world.Impacts.SequenceEqual(impacts);
        return new(GeneralHollowProbe.JoinedTube,children.Count,tube.MaximumSurfaceError,tube.MinimumBoreRadius,
            null,null,null,events,[position.X,position.Y,position.Z],[velocity.X,velocity.Y,velocity.Z],
            restored,replay,watch.Elapsed.TotalMilliseconds);
    }
}
#endif
