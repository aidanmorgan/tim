using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class PersistentContactTests
{
    private static readonly CollisionVector Up=new(0,1,0);
    private static readonly ConvexInstance Box=new(new ConvexBox(new(1,1,1)),Transform3D.Identity);
    private static PhysicsBody Body(CollisionVector velocity=default)=>
        new(new(0),PhysicsMotionType.Dynamic,RigidPose.At(Up),velocity,default,1,new InertiaTensor(2.0/3,2.0/3,2.0/3));
    private static PhysicsBody Ground()=>new(new(1),PhysicsMotionType.Static,RigidPose.At(-Up),default,default);
    private static PersistentContactPair Pair(PhysicsBody a,PhysicsBody b,double restitution=0)=>
        new(a,Box,b,Box,new(restitution,.5,.5),.01,.2);
    private static ImpulseSolveResult Solve(PersistentContactPair pair,double duration)
    {
        pair.Prepare(duration); pair.WarmStart();
        var result=ImpulseSolver.Solve(pair.PreparedContacts.ToArray().Select(p=>p.Constraint).ToArray());
        pair.Complete(); return result;
    }
    private static void Near(CollisionVector a,CollisionVector b,double tolerance=1e-8)=>
        Assert.InRange((a-b).Length,0,tolerance);
    private static void Advance(PhysicsBody body,double duration)=>body.Advance(body.CreateTrajectory(duration),duration);

    [Fact]
    public void NewSweptImpactClearsTheEpisodeButNeverReusesContactIdentities()
    {
        var body=Body(-Up); var floor=Ground(); var pair=Pair(body,floor,1);
        Solve(pair,.01);
        var ids=pair.Contacts.ToArray().Select(p=>p.Id).ToHashSet();
        pair.BeginImpact();
        Assert.Empty(pair.Contacts.ToArray());
        body.Restore(body.Snapshot() with {LinearVelocity=-Up});
        pair.Prepare(.01);
        Assert.Throws<InvalidOperationException>(()=>pair.BeginImpact());
        Assert.All(pair.PreparedContacts.ToArray(),p=>
        {
            Assert.Equal(ContactPersistence.New,p.Persistence);
            Assert.DoesNotContain(p.Id,ids);
        });
        pair.WarmStart();
        ImpulseSolver.Solve(pair.PreparedContacts.ToArray().Select(p=>p.Constraint).ToArray());
        pair.Complete();
        Near(Up,body.LinearVelocity);
    }

    [Fact]
    public void WarmStartProjectsIntoTheCurrentFrictionDiskAndCanBeRetracted()
    {
        var body=Body(); var floor=Ground();
        var constraint=new ContactConstraint(body,floor,body.Center,Up,0,0,.5);
        constraint.WarmStart(new(2,new(4,5,0)));
        Near(new(1,2,0),body.LinearVelocity);
        Assert.Equal(2,constraint.Impulse.Normal); Near(new(1,0,0),constraint.Impulse.Tangent);
        ImpulseSolver.Solve([constraint]);
        Near(default,body.LinearVelocity);
        Assert.Equal(0,constraint.Impulse.Normal); Near(default,constraint.Impulse.Tangent);
    }

    [Fact]
    public void WarmStartRejectsDuplicateSolvedAndStaleRowsBeforeMutatingBodies()
    {
        var body=Body(); var floor=Ground();
        var constraint=new ContactConstraint(body,floor,default,Up,0,0,.5);
        constraint.WarmStart(default);
        var before=body.Snapshot();
        Assert.Throws<InvalidOperationException>(()=>constraint.WarmStart(new(1,default)));
        Assert.Equal(before,body.Snapshot());
        var solved=new ContactConstraint(body,floor,default,Up,0,0,.5);
        ImpulseSolver.Solve([solved]);
        Assert.Throws<InvalidOperationException>(()=>solved.WarmStart(new(1,default)));
        var stale=new ContactConstraint(body,floor,default,Up,0,0,.5);
        Advance(body,.1);
        Assert.Throws<InvalidOperationException>(()=>stale.WarmStart(new(1,default)));
        Assert.Equal(before,body.Snapshot());
        Assert.Throws<ArgumentException>(()=>new ContactImpulse(-1,default));
        Assert.Throws<ArgumentException>(()=>new ContactImpulse(1,new(double.NaN,0,0)));
    }

    [Fact]
    public void SixHundredGravityStepsKeepTheBoxSupportedWithoutRenewedBounce()
    {
        var body=Body(); var floor=Ground(); var pair=Pair(body,floor,.8);
        ContactPointId[]? ids=null; var duration=1.0/120;
        var firstIterations=0; var laterMaximum=0;
        for(var step=0;step<600;step++)
        {
            body.ApplyWrench(-Up*9.8,default,duration);
            pair.Prepare(duration);
            var current=pair.PreparedContacts.ToArray();
            Assert.Equal(4,current.Length);
            if(ids is null) ids=current.Select(p=>p.Id).ToArray();
            else Assert.Equal(ids,current.Select(p=>p.Id).ToArray());
            Assert.All(current,p=>Assert.Equal(step==0?ContactPersistence.New:ContactPersistence.Persisting,p.Persistence));
            pair.WarmStart();
            var solved=ImpulseSolver.Solve(current.Select(p=>p.Constraint).ToArray());
            if(step==0) firstIterations=solved.Iterations; else laterMaximum=Math.Max(laterMaximum,solved.Iterations);
            pair.Complete(); Advance(body,duration);
            Assert.InRange((body.Center-Up).Length,0,1e-6);
            Assert.InRange(body.AngularVelocity.Length,0,1e-8);
            Assert.InRange(body.LinearVelocity.Length,0,1e-8);
        }
        Assert.True(laterMaximum<firstIterations);
    }

    [Fact]
    public void ReleasingContactClearsItsCacheAndRecontactGetsNewIdentities()
    {
        var body=Body(-Up); var floor=Ground(); var pair=Pair(body,floor);
        Solve(pair,.01);
        var oldIds=pair.Contacts.ToArray().Select(p=>p.Id).ToHashSet();
        body.ApplyImpulse(Up*2,body.Center); Advance(body,.01);
        pair.Prepare(.01);
        Assert.Empty(pair.PreparedContacts.ToArray());
        pair.WarmStart(); pair.Complete();
        Assert.Empty(pair.Contacts.ToArray());
        body.Restore(body.Snapshot() with {Pose=RigidPose.At(Up),LinearVelocity=-Up});
        pair.Prepare(.01);
        Assert.All(pair.PreparedContacts.ToArray(),p=>
        {
            Assert.Equal(ContactPersistence.New,p.Persistence);
            Assert.DoesNotContain(p.Id,oldIds);
        });
        pair.WarmStart();
        ImpulseSolver.Solve(pair.PreparedContacts.ToArray().Select(p=>p.Constraint).ToArray());
        pair.Complete(); Near(default,body.LinearVelocity);
    }

    [Fact]
    public void WarmImpulseScalesWithStepDurationAndDoesNotMultiplyAcrossContacts()
    {
        var body=Body(); var floor=Ground(); var pair=Pair(body,floor);
        body.ApplyWrench(-Up*10,default,.01); Solve(pair,.01);
        Assert.InRange(pair.Contacts.ToArray().Sum(p=>p.ImpulseInA.Normal),.09999999,.10000001);
        body.ApplyWrench(-Up*10,default,.02);
        pair.Prepare(.02);
        Assert.Equal(4,pair.PreparedContacts.ToArray().Select(p=>p.Id).Distinct().Count());
        pair.WarmStart();
        var seeded=pair.PreparedContacts.ToArray().Sum(p=>p.Constraint.Impulse.Normal);
        Assert.InRange(seeded,.19999998,.20000002);
        ImpulseSolver.Solve(pair.PreparedContacts.ToArray().Select(p=>p.Constraint).ToArray());
        pair.Complete(); Near(default,body.LinearVelocity);
    }

    [Fact]
    public void CacheAndBodiesRestoreForBitExactReplay()
    {
        var body=Body(); var floor=Ground(); var pair=Pair(body,floor);
        void Step()
        {
            body.ApplyWrench(-Up*9.8,default,.01); Solve(pair,.01); Advance(body,.01);
        }
        for(var i=0;i<20;i++) Step();
        var initialBody=body.Snapshot(); var initialGround=floor.Snapshot(); var initialCache=pair.Capture();
        for(var i=0;i<40;i++) Step();
        var expectedBody=body.Snapshot(); var expectedCache=pair.Capture();
        Assert.Throws<ArgumentException>(()=>pair.Restore(initialCache));
        body.Restore(initialBody); floor.Restore(initialGround); pair.Restore(initialCache);
        Assert.Equal(initialCache.Contacts.ToArray(),pair.Contacts.ToArray());
        for(var i=0;i<40;i++) Step();
        Assert.Equal(expectedBody,body.Snapshot());
        Assert.Equal(expectedCache.Contacts.ToArray(),pair.Contacts.ToArray());
        Assert.Equal(expectedCache.NextId,pair.Capture().NextId);
        Assert.Equal(expectedCache.Duration,pair.Capture().Duration);
        Assert.Throws<ArgumentException>(()=>Pair(body,floor).Restore(expectedCache));
    }

    [Fact]
    public void BodyLocalAnchorsSurviveCommonMotion()
    {
        var body=Body(-Up); var floor=Ground(); var pair=Pair(body,floor);
        Solve(pair,.01);
        var old=pair.Contacts.ToArray();
        var rotation=RigidRotation.FromRotationVector(new(.3,.4,.2));
        var translation=new CollisionVector(4,5,6);
        var stateA=body.Snapshot(); var stateB=floor.Snapshot();
        body.Restore(stateA with {Pose=new(translation+rotation.Apply(stateA.Pose.Center),rotation)});
        floor.Restore(stateB with {Pose=new(translation+rotation.Apply(stateB.Pose.Center),rotation)});
        pair.Prepare(.01);
        Assert.All(pair.PreparedContacts.ToArray(),p=>Assert.Equal(ContactPersistence.Persisting,p.Persistence));
        Assert.Equal(old.Select(p=>p.Id).OrderBy(p=>p.Value),pair.PreparedContacts.ToArray().Select(p=>p.Id).OrderBy(p=>p.Value));
    }

    [Fact]
    public void ChangedNormalCannotBorrowAnOldContactsImpulse()
    {
        var body=Body(-Up); var floor=Ground(); var pair=Pair(body,floor);
        Solve(pair,.01);
        var prior=pair.Contacts.ToArray().Select(p=>p.Id).ToHashSet();
        body.Restore(body.Snapshot() with {Pose=RigidPose.At(new(2,-1,0)),LinearVelocity=default,AngularMomentum=default});
        pair.Prepare(.01);
        Assert.All(pair.PreparedContacts.ToArray(),p=>
        {
            Assert.Equal(ContactPersistence.New,p.Persistence); Assert.DoesNotContain(p.Id,prior);
        });
        pair.WarmStart();
        Assert.All(pair.PreparedContacts.ToArray(),p=>Assert.Equal(0,p.Constraint.Impulse.Normal));
    }

    [Fact]
    public void RestitutionIsAppliedOncePerContactEpisode()
    {
        var body=Body(-Up); var floor=Ground();
        var pair=new PersistentContactPair(body,Box,floor,Box,new(.8,0,0),.01,.2);
        Solve(pair,.01);
        Near(Up*.8,body.LinearVelocity);
        body.ApplyImpulse(-Up,body.Center);
        pair.Prepare(.01);
        Assert.All(pair.PreparedContacts.ToArray(),p=>Assert.Equal(0,p.Constraint.Normal.TargetSpeed));
        pair.WarmStart();
        ImpulseSolver.Solve(pair.PreparedContacts.ToArray().Select(p=>p.Constraint).ToArray());
        pair.Complete(); Near(default,body.LinearVelocity);
    }

    [Fact]
    public void LifecycleAndUnsolvedCompletionAreExplicitlyRejected()
    {
        var body=Body(-Up); var floor=Ground(); var pair=Pair(body,floor);
        Assert.Throws<InvalidOperationException>(()=>pair.WarmStart());
        Assert.Throws<InvalidOperationException>(()=>pair.Complete());
        Assert.Throws<ArgumentOutOfRangeException>(()=>pair.Prepare(0));
        pair.Prepare(.01);
        Assert.Throws<InvalidOperationException>(()=>pair.Prepare(.01));
        Assert.Throws<InvalidOperationException>(()=>pair.Capture());
        pair.WarmStart();
        Assert.Throws<InvalidOperationException>(()=>pair.WarmStart());
        Assert.Throws<InvalidOperationException>(()=>pair.Complete());
        Assert.Empty(pair.Contacts.ToArray());
        ImpulseSolver.Solve(pair.PreparedContacts.ToArray().Select(p=>p.Constraint).ToArray());
        pair.Complete();
        pair.Prepare(.01);
        Advance(body,.01);
        Assert.Throws<InvalidOperationException>(()=>pair.WarmStart());
    }

    [Fact]
    public void TwoBodyStackSolvesAllPersistentPairsTogether()
    {
        var lower=Body(); var floor=Ground();
        var upper=new PhysicsBody(new(2),PhysicsMotionType.Dynamic,RigidPose.At(Up*3),default,default,1,new InertiaTensor(2.0/3,2.0/3,2.0/3));
        var pairs=new[]{Pair(lower,floor),Pair(upper,lower)};
        var duration=1.0/120;
        for(var step=0;step<240;step++)
        {
            lower.ApplyWrench(-Up*9.8,default,duration); upper.ApplyWrench(-Up*9.8,default,duration);
            foreach(var pair in pairs) pair.Prepare(duration);
            var constraints=pairs.SelectMany(p=>p.PreparedContacts.ToArray()).Select(p=>p.Constraint).ToArray();
            Assert.Equal(8,constraints.Length);
            foreach(var pair in pairs) pair.WarmStart();
            // The checked quantity is absolute body speed, while solver residual
            // is per-contact relative speed. Request tighter residuals for the stack.
            ImpulseSolver.Solve(constraints,tolerance:1e-10);
            foreach(var pair in pairs) pair.Complete(1e-10);
            Advance(lower,duration); Advance(upper,duration);
            Near(Up,lower.Center,1e-6); Near(Up*3,upper.Center,1e-6);
            Near(default,lower.LinearVelocity,2e-8); Near(default,upper.LinearVelocity,2e-8);
        }
    }

    [Fact]
    public void RestoringBodiesAndCacheDiscardsAnInvalidPreparedFrame()
    {
        var body=Body(); var floor=Ground(); var pair=Pair(body,floor);
        var state=body.Snapshot(); var cache=pair.Capture();
        body.ApplyImpulse(-Up,body.Center); pair.Prepare(.01); Advance(body,.01);
        Assert.Throws<InvalidOperationException>(()=>pair.WarmStart());
        body.Restore(state); pair.Restore(cache);
        Assert.Equal(ContactPairPhase.Idle,pair.Phase);
        Assert.Empty(pair.PreparedContacts.ToArray());
        body.ApplyImpulse(-Up,body.Center); Solve(pair,.01);
        Near(default,body.LinearVelocity);
    }

    [Fact]
    public void InvalidDeclarationsAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>new ContactPointId(-1));
        Assert.Throws<ArgumentException>(()=>new ContactMaterial(2,0,0));
        Assert.Throws<ArgumentException>(()=>new ContactMaterial(0,-1,0));
        Assert.Throws<ArgumentException>(()=>new ContactMaterial(0,0,double.NaN));
        var body=Body(); var floor=Ground();
        Assert.Throws<ArgumentException>(()=>new PersistentContactPair(body,Box,body,Box,default,.01,.2));
        Assert.Throws<ArgumentException>(()=>new PersistentContactPair(body,default,floor,Box,default,.01,.2));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new PersistentContactPair(body,Box,floor,Box,default,0,.2));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new PersistentContactPair(body,Box,floor,Box,default,.01,Math.PI/2));
    }
}
