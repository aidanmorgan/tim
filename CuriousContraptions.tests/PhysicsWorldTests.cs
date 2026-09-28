using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class PhysicsWorldTests
{
    private static readonly ConvexInstance Sphere=new(new ConvexSphere(.5),Transform3D.Identity);
    private static PhysicsBody Dynamic(int id,CollisionVector center,CollisionVector velocity=default,double mass=1)=>
        new(new(id),PhysicsMotionType.Dynamic,RigidPose.At(center),velocity,default,mass,new(.1*mass,.1*mass,.1*mass));
    private static PhysicsObject Object(PhysicsBody body,ConvexInstance shape,double restitution=1)=>
        new(body,new CompoundGeometry([shape]),new(restitution,0,0));
    private static PhysicsObject Wall(int id,double x)=>
        Object(new(new(id),PhysicsMotionType.Static,RigidPose.At(new(x,0,0)),default,default),
            new(new ConvexBox(new(.01,10,10)),Transform3D.Identity));
    private static PhysicsWorld World(params PhysicsObject[] objects)=>new(objects,[],new(default,maximumStep:1));
    private static void Near(double expected,double actual,double tolerance=1e-6)=>Assert.InRange(Math.Abs(expected-actual),0,tolerance);

    [Fact]
    public void HighSpeedSphereBouncesWithoutCrossingThinWall()
    {
        var ball=Dynamic(0,new(-5,0,0),new(1000,0,0));
        var world=World(Object(ball,Sphere),Wall(1,0));
        var result=world.Step(.01);
        Assert.Equal(1,result.Events);
        Near(-1000,ball.LinearVelocity.X);
        Assert.InRange(ball.AngularVelocity.Length,0,1e-8);
        Near(-6.0202,ball.Center.X,1e-5);
        Assert.Equal(PhysicsWorldPhase.Idle,world.Phase);
        Assert.Equal(.01,world.Time);
    }

    [Fact]
    public void OpposingDynamicBodiesShareTheSameImpactClock()
    {
        var a=Dynamic(0,new(-2,0,0),new(10,0,0));
        var b=Dynamic(1,new(2,0,0),new(-10,0,0));
        var world=World(Object(a,Sphere),Object(b,Sphere));
        Assert.Equal(1,world.Step(.2).Events);
        Near(-10,a.LinearVelocity.X); Near(10,b.LinearVelocity.X);
        Near(-a.Center.X,b.Center.X);
        Near(.149995,world.Impacts[0].Time);
    }

    [Fact]
    public void ClearBodiesKeepTheirIndependentTrajectories()
    {
        var a=Dynamic(0,new(-2,3,0),new(10,0,0));
        var b=Dynamic(1,new(2,-3,0),new(-10,0,0));
        var world=World(Object(a,Sphere),Object(b,Sphere));
        Assert.Equal(0,world.Step(.2).Events);
        Near(0,a.Center.X); Near(0,b.Center.X);
        Near(10,a.LinearVelocity.X); Near(-10,b.LinearVelocity.X);
    }

    [Fact]
    public void MultipleImpactsWithinOneStepReplayExactly()
    {
        var ball=Dynamic(0,default,new(10,0,0));
        var world=World(Object(ball,Sphere),Wall(1,-2),Wall(2,2));
        var before=world.Capture();
        var result=world.Step(.8); var after=ball.Snapshot(); var impacts=world.Impacts.ToArray();
        Assert.Equal(3,result.Events);
        world.Restore(before);
        Assert.Equal(before.BodyStates[0],ball.Snapshot());
        Assert.Equal(0,world.Time); Assert.Equal(0ul,world.StepIndex);
        Assert.Equal(result,world.Step(.8));
        Assert.Equal(after,ball.Snapshot()); Assert.Equal(impacts,world.Impacts.ToArray());
    }

    [Fact]
    public void ExhaustedEventBudgetRollsBackAllBodyAndClockState()
    {
        var ball=Dynamic(0,default,new(10,0,0));
        var world=new PhysicsWorld([Object(ball,Sphere),Wall(1,-2),Wall(2,2)],[],
            new(default,maximumStep:1,maximumEvents:1));
        var before=world.Capture();
        Assert.Throws<InvalidOperationException>(()=>world.Step(.8));
        Assert.Equal(before.BodyStates[0],ball.Snapshot());
        Assert.Equal(PhysicsWorldPhase.Idle,world.Phase);
        Assert.Equal(0,world.Time); Assert.Equal(0ul,world.StepIndex); Assert.Empty(world.Impacts.ToArray());
    }

    [Fact]
    public void ContactProjectionUsesMassWeightingWithoutChangingMomentum()
    {
        var a=Dynamic(0,default,new(1,2,3),1);
        var b=Dynamic(1,new(.8,0,0),new(3,2,1),3);
        var oldA=a.Snapshot(); var oldB=b.Snapshot();
        var result=PositionSolver.Solve([new ContactPositionConstraint(a,Sphere,b,Sphere)],1e-7);
        Assert.True(result.Iterations>0);
        Near(-.15,a.Center.X); Near(.85,b.Center.X);
        Assert.Equal(oldA.LinearVelocity,a.LinearVelocity); Assert.Equal(oldB.LinearVelocity,b.LinearVelocity);
        Assert.Equal(oldA.AngularMomentum,a.AngularMomentum); Assert.Equal(oldB.AngularMomentum,b.AngularMomentum);
    }

    [Fact]
    public void RestingBoxRemainsSupportedForSixHundredWorldSteps()
    {
        var box=new ConvexInstance(new ConvexBox(new(.5,.5,.5)),Transform3D.Identity);
        var body=Dynamic(0,new(0,.5,0));
        var floor=Object(new(new(1),PhysicsMotionType.Static,RigidPose.At(new(0,-.5,0)),default,default),
            new(new ConvexBox(new(10,.5,10)),Transform3D.Identity),0);
        var world=new PhysicsWorld([Object(body,box,0),floor],[],new(new(0,-9.8,0)));
        for(var i=0;i<600;i++) world.Step(1.0/120);
        Near(.5,body.Center.Y); Assert.InRange(body.LinearVelocity.Length,0,1e-7);
        Assert.InRange(body.AngularVelocity.Length,0,1e-7);
    }

    [Fact]
    public void RotatingKinematicBeamHitsBeforeItsClearEndpoint()
    {
        var beam=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,RigidPose.At(default),default,new(0,0,120));
        var ball=Dynamic(1,new(1.5,.8,0));
        var world=World(Object(beam,new(new ConvexBox(new(2,.1,.1)),Transform3D.Identity)),Object(ball,Sphere));
        Assert.True(world.Step(.01).Events>0);
        Assert.True(ball.LinearVelocity.Length>1);
        Near(120,beam.AngularVelocity.Z);
        Assert.Equal(new PhysicsBodyId(0),world.Impacts[0].Pair.A);
    }

    [Theory]
    [InlineData(0,false)]
    [InlineData(2,true)]
    public void CompoundOpeningRemainsEmptyAndWallsHaveTypedChildIdentities(double y,bool collision)
    {
        var ball=Dynamic(0,new(-3,y,0),new(10,0,0));
        var fixture=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.At(default),default,default);
        var compound=new CompoundGeometry([
            new(new ConvexBox(new(.1,.5,2)),new(Basis.Identity,new(0,2,0))),
            new(new ConvexBox(new(.1,.5,2)),new(Basis.Identity,new(0,-2,0)))]);
        var world=World(Object(ball,Sphere),new(fixture,compound,new(1,0,0)));
        Assert.Equal(collision?1:0,world.Step(.5).Events);
        if(collision)
        {
            Assert.Equal(new ColliderChildId(0),world.Impacts[0].Pair.ChildB);
            Near(-10,ball.LinearVelocity.X);
        }
        else Near(2,ball.Center.X);
    }

    [Fact]
    public void OverlappingBoxProjectsWithoutAddingVelocity()
    {
        var box=new ConvexInstance(new ConvexBox(new(.5,.5,.5)),Transform3D.Identity);
        var body=Dynamic(0,new(0,.45,0));
        var floor=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.At(new(0,-.5,0)),default,default);
        var before=floor.Snapshot();
        var constraint=new ContactPositionConstraint(body,box,floor,
            new(new ConvexBox(new(10,.5,10)),Transform3D.Identity));
        Assert.True(PositionSolver.Solve([constraint],1e-7).Iterations>0);
        Assert.InRange(constraint.Error(1e-8),0,1e-7);
        Assert.Equal(default,body.LinearVelocity); Assert.Equal(default,body.AngularMomentum);
        Assert.Equal(before,floor.Snapshot());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CurvedContactRemainsOnePointUnderBothBodyOrders(bool reverse)
    {
        var beam=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,RigidPose.Identity,default,new(0,0,120));
        var ball=Dynamic(1,new(1.5,.8,0));
        var shape=new ConvexInstance(new ConvexBox(new(2,.1,.1)),Transform3D.Identity);
        var a=new ConvexMotion(shape,beam.CreateTrajectory(.01));
        var b=new ConvexMotion(Sphere,ball.CreateTrajectory(.01));
        var hit=ConvexSweep.Cast(a,b,.01,ConvexSweep.ContactDistance-ConvexDistance.DefaultTolerance);
        var manifold=reverse?ContactManifold.Query(b.At(hit.Time),a.At(hit.Time)):
            ContactManifold.Query(a.At(hit.Time),b.At(hit.Time));
        Assert.Single(manifold.Points.ToArray());
    }

    [Fact]
    public void ProjectionRejectsImmovablePenetration()
    {
        var a=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var b=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.At(new(.8,0,0)),default,default);
        Assert.Throws<InvalidOperationException>(()=>
            PositionSolver.Solve([new ContactPositionConstraint(a,Sphere,b,Sphere)],1e-7));
    }

    [Fact]
    public void UnrepresentableStepCannotSilentlyAdvanceClockWithoutBodies()
    {
        var world=new PhysicsWorld([],[],new(default,maximumStep:double.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(()=>world.Step(double.Epsilon));
        Assert.Equal(0,world.Time);
    }

    [Fact]
    public void WorldRejectsDuplicateIdentitiesAndForeignSnapshots()
    {
        var a=Object(Dynamic(0,default),Sphere);
        Assert.Throws<ArgumentException>(()=>World(a,a));
        var world=World(a);
        Assert.Throws<ArgumentException>(()=>world.Restore(World().Capture()));
        Assert.Throws<ArgumentOutOfRangeException>(()=>world.Step(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(()=>world.Step(0));
    }
}
