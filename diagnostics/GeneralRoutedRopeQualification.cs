#if PLAYTEST
using System;
using System.Linq;
using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

internal enum GeneralRoutedRopeProbe { MovingGuide, FixedGuide, Shortening, SlackEvent, ClearProjection, BlockedProjection, RotatingGuide }
internal enum RoutedRopeOutcome { Completed, Rejected }
internal sealed record GeneralRoutedRopeReport(GeneralRoutedRopeProbe Probe,RoutedRopeOutcome Outcome,
    double Length,double[] Speeds,double GuideHeight,double GuideSpin,double? BoundaryTime,
    int Events,double? BeforeFlightSpeed,bool ExactRestore,bool ExactReplay);
internal static class GeneralRoutedRopeQualification
{
    private static readonly CollisionVector X=new(1,0,0),Y=new(0,1,0),Z=new(0,0,1);
    private static PhysicsBody Body(int id,CollisionVector center,CollisionVector velocity=default,CollisionVector spin=default)=>
        new(new(id),PhysicsMotionType.Dynamic,RigidPose.At(center),velocity,spin,1,new(1,1,1));
    private static PhysicsBody Fixed(int id,CollisionVector center)=>new(new(id),PhysicsMotionType.Static,RigidPose.At(center),default,default);
    private static PhysicsObject Object(PhysicsBody body)=>new(body,new([new(new ConvexSphere(.05),AffineTransform.Identity)]),new(0,0,0));
    internal static GeneralRoutedRopeReport Run(GeneralRoutedRopeProbe probe)
    {
        if(!Enum.IsDefined(probe)) throw new ArgumentOutOfRangeException(nameof(probe));
        if(probe==GeneralRoutedRopeProbe.RotatingGuide) return Rotating();
        var projection=probe is GeneralRoutedRopeProbe.ClearProjection or GeneralRoutedRopeProbe.BlockedProjection;
        var slack=probe==GeneralRoutedRopeProbe.SlackEvent;
        var sign=probe==GeneralRoutedRopeProbe.Shortening?1:-1;
        var a=projection?Fixed(8,-X):Body(8,-X,Y*(slack?-100:3*sign));
        var b=projection?Fixed(5,X):Body(5,X,slack?default:Y*sign);
        var guide=probe is GeneralRoutedRopeProbe.FixedGuide or GeneralRoutedRopeProbe.SlackEvent?
            Fixed(2,Y):Body(2,Y*(projection?2:1));
        var rope=new PhysicsRopeJoint(new(0),new([new(a,default),new(guide,-X),new(guide,X),new(b,default)]),
            slack?5:4,ConnectedBodyCollision.Disabled);
        var objects=new[]{Object(a),Object(guide),Object(b)};
        if(probe==GeneralRoutedRopeProbe.BlockedProjection)
            objects=[..objects,new(Fixed(10,Y*1.5),new([new(new ConvexBox(new(.2,.05,.2)),AffineTransform.Identity)]),new(0,0,0))];
        var duration=slack?.012:.1;
        var world=new PhysicsWorld([],objects,[rope],new(default,maximumStep:duration));
        double? beforeFlight=null;
        if(slack)
        {
            ImpulseSolver.Solve(rope.VelocityConstraints(1e-7));
            beforeFlight=a.LinearVelocity.Length;
        }
        var before=world.Capture();
        (RoutedRopeOutcome Outcome,PhysicsStepResult? Result) Step()
        {
            try { return (RoutedRopeOutcome.Completed,world.Step([],[],duration)); }
            catch(InvalidOperationException) when(probe==GeneralRoutedRopeProbe.BlockedProjection)
            { return (RoutedRopeOutcome.Rejected,null); }
        }
        var result=Step(); var after=world.Capture(); var stops=world.JointStops.ToArray();
        var length=rope.Route.CurrentLength; var speeds=new[]{a.LinearVelocity.Y,b.LinearVelocity.Y,guide.LinearVelocity.Y};
        var height=guide.Center.Y; var spin=guide.AngularVelocity.Z;
        var failedRestored=result.Outcome!=RoutedRopeOutcome.Rejected||
            after.BodyStates.SequenceEqual(before.BodyStates)&&after.Time==before.Time&&after.StepIndex==before.StepIndex;
        world.Restore(before);
        var restored=failedRestored&&world.Capture().BodyStates.SequenceEqual(before.BodyStates)&&world.Time==before.Time&&
            world.StepIndex==before.StepIndex&&world.JointStops.Length==0;
        var replay=Step()==result&&world.Capture().BodyStates.SequenceEqual(after.BodyStates)&&world.Time==after.Time&&
            world.StepIndex==after.StepIndex&&world.JointStops.SequenceEqual(stops);
        return new(probe,result.Outcome,length,speeds,height,spin,stops.Length==0?null:stops[0].Time,
            result.Result?.Events??0,beforeFlight,restored,replay);
    }
    private static GeneralRoutedRopeReport Rotating()
    {
        var a=Fixed(4,X); var guide=Body(2,default,spin:Z*120); var b=Fixed(0,X);
        var rope=new PhysicsRopeJoint(new(0),new([new(a,default),new(guide,X),new(b,default)]),2,ConnectedBodyCollision.Disabled);
        var bodies=rope.Bodies.ToArray(); var before=bodies.Select(body=>body.Snapshot()).ToArray();
        JointSweepResult Sweep()
        {
            var duration=Math.Tau/120; var paths=bodies.Select(body=>body.CreateTrajectory(duration,default)).ToArray();
            var hit=rope.Sweep(paths,duration,1e-7,1e-8);
            if(hit.Status!=JointSweepStatus.Boundary) throw new InvalidOperationException("Expected hidden routed extension.");
            for(var i=0;i<bodies.Length;i++) bodies[i].Advance(paths[i],hit.Time);
            return hit;
        }
        var result=Sweep(); var after=bodies.Select(body=>body.Snapshot()).ToArray(); var length=rope.Route.CurrentLength;
        for(var i=0;i<bodies.Length;i++) bodies[i].Restore(before[i]);
        var restored=bodies.Select(body=>body.Snapshot()).SequenceEqual(before);
        var replay=Sweep()==result&&bodies.Select(body=>body.Snapshot()).SequenceEqual(after);
        return new(GeneralRoutedRopeProbe.RotatingGuide,RoutedRopeOutcome.Completed,length,[0,0,0],0,120,
            result.Time,1,null,restored,replay);
    }
}
#endif
