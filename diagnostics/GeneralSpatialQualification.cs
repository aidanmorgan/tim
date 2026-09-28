#if PLAYTEST
using System;
using System.Diagnostics;
using System.Linq;
using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

internal enum GeneralSpatialProbe { SparseQuery,DistantHollowBodies,LocalWorld,BlockedCorrection,IndependentLoads }
internal enum GeneralSpatialOutcome { Completed,Rejected }
internal sealed record GeneralSpatialReport(GeneralSpatialProbe Probe,GeneralSpatialOutcome Outcome,
    long CartesianPairs,int? NodeTests,int? LeafTests,int? Candidates,int? Child,int? RetainedPairs,
    int? CouplingTests,int? CoupledPairs,bool? ExactRestore,bool ExactReplay,bool? ReferenceMatch,
    double ElapsedMilliseconds,long AllocatedBytes);
internal static class GeneralSpatialQualification
{
    private static PhysicsBody Body(int id,CollisionVector center,PhysicsMotionType motion=PhysicsMotionType.Static,CollisionVector velocity=default)=>
        motion==PhysicsMotionType.Dynamic?new(new(id),motion,RigidPose.At(center),velocity,default,1,new(1,1,1)):
            new(new(id),motion,RigidPose.At(center),default,default);
    private static CompoundGeometry Sphere()=>new([new(new ConvexSphere(.1),Transform3D.Identity)]);
    private static CompoundGeometry Grid()=>new(Enumerable.Range(0,4096).Select(i=>
        new ConvexInstance(new ConvexBox(new(.2,.2,.2)),new(Basis.Identity,new(i*2,0,0)))).ToArray());
    internal static GeneralSpatialReport Run(GeneralSpatialProbe probe)
    {
        if(!Enum.IsDefined(probe)) throw new ArgumentOutOfRangeException(nameof(probe));
        var watch=Stopwatch.StartNew(); var allocation=GC.GetAllocatedBytesForCurrentThread();
        GeneralSpatialReport Report(GeneralSpatialOutcome outcome,long cartesian,int? nodes=null,int? leaves=null,int? candidates=null,int? child=null,
            int? retained=null,int? coupling=null,int? coupled=null,bool? restored=null,bool replay=true,bool? reference=null)=>
            new(probe,outcome,cartesian,nodes,leaves,candidates,child,retained,coupling,coupled,restored,replay,reference,
                watch.Elapsed.TotalMilliseconds,GC.GetAllocatedBytesForCurrentThread()-allocation);
        if(probe==GeneralSpatialProbe.SparseQuery)
        {
            var a=new CompoundMotion(Sphere(),Body(0,new(4000,0,0)).CreateTrajectory(0));
            var b=new CompoundMotion(Grid(),Body(1,default).CreateTrajectory(0));
            var result=CompoundCollision.Candidates(a,b,0,0);
            var second=CompoundCollision.Candidates(a,b,0,0);
            return Report(GeneralSpatialOutcome.Completed,4096,result.NodeTests,result.LeafTests,result.Pairs.Count,result.Pairs.Single().B.Index,
                replay:result.Pairs.SequenceEqual(second.Pairs)&&result.NodeTests==second.NodeTests&&result.LeafTests==second.LeafTests);
        }
        if(probe==GeneralSpatialProbe.IndependentLoads)
        {
            var floor=Body(5000,default);
            var bodies=Enumerable.Range(0,4096).Select(i=>Body(i,new(i*2,1,0),PhysicsMotionType.Dynamic,new(0,-1,0))).ToArray();
            var before=bodies.Select(b=>b.Snapshot()).ToArray();
            ImpulseSolveResult Solve()=>ImpulseSolver.Solve(bodies.Select(b=>ImpulseConstraint.Contact(b,floor,b.Center,new(0,1,0),0,0)).ToArray(),1);
            var result=Solve(); var after=bodies.Select(b=>b.Snapshot()).ToArray();
            for(var i=0;i<bodies.Length;i++) bodies[i].Restore(before[i]);
            var restored=bodies.Select(b=>b.Snapshot()).SequenceEqual(before);
            var second=Solve();
            return Report(GeneralSpatialOutcome.Completed,4096L*4095/2,coupling:result.CouplingTests,coupled:result.CoupledPairs,
                restored:restored,replay:result==second&&bodies.Select(b=>b.Snapshot()).SequenceEqual(after),
                reference:bodies.All(b=>b.LinearVelocity==default));
        }
        if(probe==GeneralSpatialProbe.DistantHollowBodies)
        {
            var bend=HollowGeometry.Bend(2.4,Math.PI/2,.65,.7,new(.005)).Geometry;
            var a=Body(0,default,PhysicsMotionType.Dynamic); var b=Body(1,new(100,0,0),PhysicsMotionType.Dynamic);
            var query=CompoundCollision.Candidates(new(bend,a.CreateTrajectory(.01)),new(bend,b.CreateTrajectory(.01)),.01,ConvexSweep.ContactDistance);
            var world=new PhysicsWorld([new(a,bend,new(0,0,0)),new(b,bend,new(0,0,0))],[],new(default));
            var before=world.Capture(); world.Step([],.01); var after=world.Capture();
            var retained=world.RetainedContactPairs;
            world.Restore(before);
            var restored=world.RetainedContactPairs==0&&world.Capture().BodyStates.SequenceEqual(before.BodyStates)&&world.Time==0;
            world.Step([],.01);
            return Report(GeneralSpatialOutcome.Completed,(long)bend.Count*bend.Count,query.NodeTests,query.LeafTests,query.Pairs.Count,
                retained:retained,restored:restored,replay:world.Capture().BodyStates.SequenceEqual(after.BodyStates)&&world.RetainedContactPairs==retained,
                reference:a.LinearVelocity==default&&b.LinearVelocity==default);
        }
        if(probe==GeneralSpatialProbe.BlockedCorrection)
        {
            var a=Body(0,new(-1,0,0),PhysicsMotionType.Dynamic); var anchor=Body(1,new(1,0,0)); var wall=Body(2,default);
            var joint=new PhysicsFrameJoint(new(0),FrameJointKind.BallSocket,a,new(default,RigidRotation.Identity),
                anchor,new(default,RigidRotation.Identity),ConnectedBodyCollision.Disabled,null);
            var world=new PhysicsWorld([new(a,Sphere(),new(0,0,0)),new(anchor,Sphere(),new(0,0,0)),
                new(wall,new([new(new ConvexBox(new(.001,2,2)),Transform3D.Identity)]),new(0,0,0))],[joint],new(default));
            var before=world.Capture();
            bool Reject()
            {
                try { world.Step([],.01); return false; }
                catch(InvalidOperationException) { return world.Time==0&&world.StepIndex==0&&world.RetainedContactPairs==0&&world.Capture().BodyStates.SequenceEqual(before.BodyStates); }
            }
            var restored=Reject(); var replay=Reject();
            return Report(restored?GeneralSpatialOutcome.Rejected:GeneralSpatialOutcome.Completed,1,retained:world.RetainedContactPairs,restored:restored,replay:replay);
        }
        var moving=Body(0,new(3999,.5,0),PhysicsMotionType.Dynamic,new(2,0,0)); var fixedBody=Body(1,default);
        var simulation=new PhysicsWorld([new(moving,Sphere(),new(0,0,0)),new(fixedBody,Grid(),new(0,0,0))],[],new(new(0,-1,0)));
        var referenceBody=Body(0,new(3999,.5,0),PhysicsMotionType.Dynamic,new(2,0,0));
        var referenceWorld=new PhysicsWorld([new(referenceBody,Sphere(),new(0,0,0)),
            new(Body(1,default),new([new(new ConvexBox(new(.2,.2,.2)),new(Basis.Identity,new(4000,0,0)))]),new(0,0,0))],[],new(new(0,-1,0)));
        var initial=simulation.Capture();
        for(var i=0;i<120;i++) { simulation.Step([],1.0/120); referenceWorld.Step([],1.0/120); }
        var final=simulation.Capture(); var count=simulation.RetainedContactPairs; var impacts=simulation.Impacts.ToArray();
        var matched=moving.Snapshot()==referenceBody.Snapshot();
        simulation.Restore(initial);
        var exact=simulation.RetainedContactPairs==0&&simulation.Capture().BodyStates.SequenceEqual(initial.BodyStates)&&simulation.Time==0;
        for(var i=0;i<120;i++) simulation.Step([],1.0/120);
        return Report(GeneralSpatialOutcome.Completed,4096,retained:count,restored:exact,
            replay:simulation.Capture().BodyStates.SequenceEqual(final.BodyStates)&&simulation.RetainedContactPairs==count&&simulation.Impacts.SequenceEqual(impacts),
            reference:matched);
    }
}
#endif
