using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class SpatialCandidateTests
{
    private static CompoundGeometry Grid(int count)=>new(Enumerable.Range(0,count)
        .Select(i=>new ConvexInstance(new ConvexBox(new(.2,.2,.2)),new(Basis.Identity,new(i*2,0,0)))).ToArray());
    private static PhysicsBody Body(int id,CollisionVector center,PhysicsMotionType motion=PhysicsMotionType.Static,
        CollisionVector velocity=default,CollisionVector spin=default)=>
        motion==PhysicsMotionType.Dynamic?
            new(new(id),motion,RigidPose.At(center),velocity,spin,1,new(1,1,1)):
            new(new(id),motion,RigidPose.At(center),velocity,spin);
    private static CompoundGeometry Sphere(double radius)=>new([new(new ConvexSphere(radius),Transform3D.Identity)]);

    [Fact]
    public void SparseQueryVisitsALogarithmicSubsetAndKeepsDeclarationIds()
    {
        var grid=Grid(4096);
        var first=new CompoundMotion(Sphere(.1),Body(0,new(4000,0,0)).CreateTrajectory(0));
        var second=new CompoundMotion(grid,Body(1,default).CreateTrajectory(0));
        var result=CompoundCollision.Candidates(first,second,0,0);
        var pair=Assert.Single(result.Pairs);
        Assert.Equal(new ColliderChildId(2000),pair.B);
        Assert.InRange(result.NodeTests,1,60); Assert.Equal(1,result.LeafTests);
        Assert.Equal(result.Pairs,CompoundCollision.Candidates(first,second,0,0).Pairs);
    }

    [Theory]
    [InlineData(140)]
    [InlineData(701)]
    public void AllSampledOverlapsSurviveHierarchyPruningForMovingRotatingCompounds(int seed)
    {
        var random=new Random(seed);
        double Signed()=>random.NextDouble()*2-1;
        CollisionVector Vector(double scale)=>new(Signed()*scale,Signed()*scale,Signed()*scale);
        CompoundGeometry Geometry()=>new(Enumerable.Range(0,12).Select(i=>new ConvexInstance(
            new ConvexBox(new(.15+random.NextDouble(),.1+random.NextDouble()*.5,.1+random.NextDouble()*.3)),
            new(Basis.FromEuler(new((float)Signed(),(float)Signed(),(float)Signed())),new((float)Signed()*3,(float)Signed()*3,(float)Signed()*3)))).ToArray());
        for(var trial=0;trial<20;trial++)
        {
            var a=new CompoundMotion(Geometry(),Body(0,Vector(2),PhysicsMotionType.Kinematic,Vector(4),Vector(10)).CreateTrajectory(.5));
            var b=new CompoundMotion(Geometry(),Body(1,Vector(2),PhysicsMotionType.Kinematic,Vector(4),Vector(10)).CreateTrajectory(.5));
            const double margin=.01;
            var candidates=CompoundCollision.Candidates(a,b,.5,margin).Pairs.ToHashSet();
            for(var sample=0;sample<=50;sample++)
            for(var i=0;i<a.Count;i++)
            for(var j=0;j<b.Count;j++)
                if(CollisionBounds.Of(a.Child(new(i)).At(sample*.01)).DistanceLowerBound(CollisionBounds.Of(b.Child(new(j)).At(sample*.01)))<=margin)
                    Assert.Contains(new ColliderChildPair(new(i),new(j)),candidates);
        }
    }

    [Fact]
    public void FullTurnHiddenImpactUsesTheHierarchyAndTheSameSweep()
    {
        var geometry=new CompoundGeometry([new(new ConvexBox(new(2,.02,.02)),Transform3D.Identity)]);
        var a=new CompoundMotion(geometry,Body(0,default,PhysicsMotionType.Kinematic,spin:new(0,0,Math.Tau)).CreateTrajectory(1));
        var b=new CompoundMotion(Sphere(.03),Body(1,new(1.5*Math.Cos(.4),1.5*Math.Sin(.4),0)).CreateTrajectory(1));
        var direct=ConvexSweep.Cast(a.Child(new(0)),b.Child(new(0)),1,ConvexSweep.ContactDistance);
        var compound=CompoundCollision.Cast(a,b,1,ConvexSweep.ContactDistance);
        Assert.Equal(ConvexSweepStatus.Contact,compound.Status);
        Assert.Equal(direct.Time,compound.Time);
        Assert.Equal(direct.Separation,compound.Separation);
    }

    [Fact]
    public void WorldDoesNotAllocateTheCartesianProductOfTwoHollowBodies()
    {
        var bend=HollowGeometry.Bend(2.4,Math.PI/2,.65,.7,new(.005)).Geometry;
        var a=Body(0,default,PhysicsMotionType.Dynamic);
        var b=Body(1,new(100,0,0),PhysicsMotionType.Dynamic);
        var world=new PhysicsWorld([new(a,bend,new(0,0,0)),new(b,bend,new(0,0,0))],[],new(default));
        Assert.Equal(0,world.RetainedContactPairs);
        world.Step([],1.0/120);
        Assert.Equal(0,world.RetainedContactPairs);
        Assert.Equal(default,a.LinearVelocity); Assert.Equal(default,b.LinearVelocity);
    }

    [Fact]
    public void WorldRetainsOnlyEncounteredCellsAndRestoreRemovesLaterDiscoveries()
    {
        var a=Body(0,new(3999,.5,0),PhysicsMotionType.Dynamic,new(2,0,0));
        var b=Body(1,default);
        var world=new PhysicsWorld([new(a,Sphere(.1),new(0,0,0)),new(b,Grid(4096),new(0,0,0))],[],new(new(0,-1,0)));
        var referenceBody=Body(0,new(3999,.5,0),PhysicsMotionType.Dynamic,new(2,0,0));
        var reference=new PhysicsWorld([new(referenceBody,Sphere(.1),new(0,0,0)),
            new(Body(1,default),new([new(new ConvexBox(new(.2,.2,.2)),new(Basis.Identity,new(4000,0,0)))]),new(0,0,0))],[],new(new(0,-1,0)));
        var before=world.Capture();
        for(var i=0;i<120;i++)
        {
            world.Step([],1.0/120); reference.Step([],1.0/120);
            Assert.Equal(referenceBody.Snapshot(),a.Snapshot());
            Assert.Equal(reference.Impacts.ToArray().Select(p=>p.Time),world.Impacts.ToArray().Select(p=>p.Time));
        }
        Assert.InRange(world.RetainedContactPairs,1,3);
        var after=world.Capture(); var count=world.RetainedContactPairs; var impacts=world.Impacts.ToArray();
        world.Restore(before); Assert.Equal(0,world.RetainedContactPairs);
        for(var i=0;i<120;i++) world.Step([],1.0/120);
        Assert.Equal(count,world.RetainedContactPairs); Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(impacts,world.Impacts.ToArray());
        world.Restore(after); Assert.Equal(count,world.RetainedContactPairs);
    }

    [Fact]
    public void NewlyDiscoveredCorrectionObstacleRejectsAndRollsBackItsCache()
    {
        var a=Body(0,new(-1,0,0),PhysicsMotionType.Dynamic);
        var anchor=Body(1,new(1,0,0)); var obstacle=Body(2,default);
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.BallSocket,a,new(default,RigidRotation.Identity),
            anchor,new(default,RigidRotation.Identity),ConnectedBodyCollision.Disabled,null);
        var world=new PhysicsWorld([new(a,Sphere(.1),new(0,0,0)),new(anchor,Sphere(.1),new(0,0,0)),
            new(obstacle,new([new(new ConvexBox(new(.001,2,2)),Transform3D.Identity)]),new(0,0,0))],[joint],new(default));
        var before=world.Capture(); Assert.Equal(0,world.RetainedContactPairs);
        Assert.Throws<InvalidOperationException>(()=>world.Step([],.01));
        Assert.Equal(0,world.RetainedContactPairs);
        Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
    }

    [Fact]
    public void IndependentLoadsOnOneStaticFloorDoNotCreateQuadraticRowPairs()
    {
        var floor=Body(5000,default);
        var bodies=Enumerable.Range(0,4096).Select(i=>Body(i,new(i*2,1,0),PhysicsMotionType.Dynamic,new(0,-1,0))).ToArray();
        var constraints=bodies.Select(b=>ImpulseConstraint.Contact(b,floor,b.Center,new(0,1,0),0,0)).ToArray();
        var result=ImpulseSolver.Solve(constraints,1);
        Assert.Equal(0,result.CouplingTests); Assert.Equal(0,result.CoupledPairs);
        Assert.All(bodies,b=>Assert.Equal(default,b.LinearVelocity));
    }

    [Fact]
    public void MultipleSharedBodiesDoNotRepeatACoupledRowPair()
    {
        var a=Body(0,default,PhysicsMotionType.Dynamic,new(1,0,0));
        var b=Body(1,new(2,0,0),PhysicsMotionType.Dynamic);
        var first=new ConstraintGradient([new(a,new(1,0,0),default),new(b,new(-1,0,0),default)]);
        var second=new ConstraintGradient([new(a,new(1,1,0),default),new(b,new(-1,-1,0),default)]);
        var result=ImpulseSolver.Solve([new ImpulseConstraint(first,0,double.NegativeInfinity,0),
            new ImpulseConstraint(second,0,double.NegativeInfinity,0)],1);
        Assert.Equal(1,result.CouplingTests); Assert.Equal(1,result.CoupledPairs);
        Assert.InRange((a.LinearVelocity+b.LinearVelocity-new CollisionVector(1,0,0)).Length,0,1e-12);
    }

    [Fact]
    public void InvalidCandidateHorizonsAndMarginsAreRejectedEvenForDistantRoots()
    {
        var a=new CompoundMotion(Sphere(1),Body(0,default).CreateTrajectory(.1));
        var b=new CompoundMotion(Sphere(1),Body(1,new(100,0,0)).CreateTrajectory(.1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>CompoundCollision.Candidates(a,b,.2,0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>CompoundCollision.Candidates(a,b,.1,double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(()=>CompoundCollision.Candidates(a,b,.1,-1));
    }
}
