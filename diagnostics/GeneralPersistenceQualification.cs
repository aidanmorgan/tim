#if PLAYTEST
using System;
using System.Linq;
using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

internal enum GeneralPersistenceProbe { SingleSupport, TwoBodyStack }
internal sealed record GeneralPersistenceReport(GeneralPersistenceProbe Probe,int Steps,double SolverTolerance,
    double MaximumDrift,double MaximumSpeed,double MaximumSpin,double MaximumResidual,
    int FirstIterations,int LaterMaximumIterations,bool StableIds,bool ExactRestore,bool ExactReplay,bool ReleaseCleared);
internal static class GeneralPersistenceQualification
{
    internal static GeneralPersistenceReport Run(GeneralPersistenceProbe probe)
    {
        var count=probe switch
        {
            GeneralPersistenceProbe.SingleSupport=>1,
            GeneralPersistenceProbe.TwoBodyStack=>2,
            _=>throw new ArgumentOutOfRangeException(nameof(probe))
        };
        var steps=count==1?600:240;
        var tolerance=count==1?1e-8:1e-10;
        var up=new CollisionVector(0,1,0);
        var shape=new ConvexInstance(new ConvexBox(new(1,1,1)),Transform3D.Identity);
        var ground=new PhysicsBody(new(100),PhysicsMotionType.Static,RigidPose.At(-up),default,default);
        var bodies=Enumerable.Range(0,count).Select(i=>new PhysicsBody(new(i),PhysicsMotionType.Dynamic,
            RigidPose.At(up*(1+2*i)),default,default,1,new InertiaTensor(2.0/3,2.0/3,2.0/3))).ToArray();
        var pairs=bodies.Select((body,i)=>new PersistentContactPair(body,shape,i==0?ground:bodies[i-1],shape,new(.8,.5,.5),.01,.2)).ToArray();
        var ids=new ContactPointId[count][];
        double drift=0,speed=0,spin=0,residual=0;
        var first=0; var later=0; var stable=true;
        void Step(int index,bool record)
        {
            const double duration=1.0/120;
            foreach(var body in bodies) body.ApplyWrench(-up*9.8,default,duration);
            foreach(var pair in pairs) pair.Prepare(duration);
            var constraints=pairs.SelectMany(p=>p.PreparedContacts.ToArray()).Select(p=>p.Constraint).ToArray();
            if(record)
            {
                for(var i=0;i<count;i++)
                {
                    var points=pairs[i].PreparedContacts.ToArray();
                    var current=points.Select(p=>p.Id).ToArray();
                    if(index==0) ids[i]=current;
                    else stable&=ids[i].SequenceEqual(current)&&points.All(p=>p.Persistence==ContactPersistence.Persisting);
                    stable&=current.Length==4;
                }
            }
            foreach(var pair in pairs) pair.WarmStart();
            var result=ImpulseSolver.Solve(constraints,tolerance:tolerance);
            foreach(var pair in pairs) pair.Complete(tolerance);
            foreach(var body in bodies) body.Advance(body.CreateTrajectory(duration),duration);
            if(!record) return;
            if(index==0) first=result.Iterations; else later=Math.Max(later,result.Iterations);
            residual=Math.Max(residual,result.MaximumResidual);
            for(var i=0;i<count;i++)
            {
                drift=Math.Max(drift,(bodies[i].Center-up*(1+2*i)).Length);
                speed=Math.Max(speed,bodies[i].LinearVelocity.Length); spin=Math.Max(spin,bodies[i].AngularVelocity.Length);
            }
        }
        for(var i=0;i<steps;i++) Step(i,true);
        var initialBodies=bodies.Select(b=>b.Snapshot()).ToArray();
        var initialPairs=pairs.Select(p=>p.Capture()).ToArray();
        for(var i=0;i<20;i++) Step(i,false);
        var expectedBodies=bodies.Select(b=>b.Snapshot()).ToArray();
        var expectedPairs=pairs.Select(p=>p.Capture()).ToArray();
        for(var i=0;i<count;i++) bodies[i].Restore(initialBodies[i]);
        for(var i=0;i<count;i++) pairs[i].Restore(initialPairs[i]);
        bool Same(PersistentContactPair.Snapshot x,PersistentContactPair.Snapshot y)=>
            x.NextId==y.NextId&&x.Duration==y.Duration&&x.Contacts.SequenceEqual(y.Contacts);
        var restored=bodies.Select(b=>b.Snapshot()).SequenceEqual(initialBodies)&&
            pairs.Select((p,i)=>Same(p.Capture(),initialPairs[i])).All(value=>value);
        for(var i=0;i<20;i++) Step(i,false);
        var replayed=bodies.Select(b=>b.Snapshot()).SequenceEqual(expectedBodies)&&
            pairs.Select((p,i)=>Same(p.Capture(),expectedPairs[i])).All(value=>value);
        for(var i=0;i<count;i++)
        {
            bodies[i].ApplyImpulse(up*(2+2*i),bodies[i].Center);
            bodies[i].Advance(bodies[i].CreateTrajectory(.01),.01);
        }
        foreach(var pair in pairs) pair.Prepare(.01);
        var released=pairs.All(p=>p.PreparedContacts.Length==0);
        foreach(var pair in pairs) { pair.WarmStart(); pair.Complete(); }
        released&=pairs.All(p=>p.Contacts.Length==0);
        return new(probe,steps,tolerance,drift,speed,spin,residual,first,later,stable,restored,replayed,released);
    }
}
#endif
