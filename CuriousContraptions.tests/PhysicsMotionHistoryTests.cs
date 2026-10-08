using CuriousContraptions.Physics;
namespace CuriousContraptions.Tests;

public class PhysicsMotionHistoryTests
{
    [Fact]
    public void CapturedPhysicalBoundCoversAcceleratedRotorAndSurvivesMutation()
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,new(1,0,0),default,1,new(1,1,1));
        var trajectory=body.CreateTrajectory(.1,new(new(2,0,0),new(0,0,2)));
        var accepted=new PhysicsMotionInterval(body.Id,10,10.05,.05,trajectory.PosePath);
        Assert.True(trajectory.PosePath.AngularSpeedBound<.2);
        Assert.True(accepted.PhysicalAngularSpeedBound>=.2);
        Assert.Equal(new CollisionVector(1.1,0,0),accepted.LinearVelocityAt(10.05));
        var bound=accepted.PhysicalAngularSpeedBound;
        body.Restore(body.Snapshot() with { LinearVelocity=new(99,0,0),AngularMomentum=new(0,0,99) });
        Assert.Equal(new CollisionVector(1.1,0,0),accepted.LinearVelocityAt(10.05));
        Assert.Equal(bound,accepted.PhysicalAngularSpeedBound);
        Assert.Throws<ArgumentOutOfRangeException>(()=>accepted.LinearVelocityAt(Math.BitIncrement(10.05)));
        Assert.Throws<ArgumentOutOfRangeException>(()=>accepted.LinearVelocityAt(double.PositiveInfinity));
    }
    private static readonly CompoundGeometry Shape=new([new(new ConvexSphere(.5),SceneGeometryAdapter.CaptureAffine(Godot.Transform3D.Identity))]);
    private static PhysicsObject Object(PhysicsBody body)=>new(body,Shape,new(1,0,0));
    private static PhysicsBody Ball()=>new(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(-2,0,0)),new(10,0,0),default,1,new(.1,.1,.1));
    private static PhysicsObject Wall()=>new(new(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default),
        new([new(new ConvexBox(new(.01,10,10)),SceneGeometryAdapter.CaptureAffine(Godot.Transform3D.Identity))]),new(1,0,0));

    [Fact]
    public void OnlyAcceptedPrefixIsVisible()
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,RigidPose.Identity,new(1,0,0),new(0,0,Math.Tau*3));
        var path=body.CreateTrajectory(1,default).PosePath;
        var accepted=new PhysicsMotionInterval(body.Id,2,2.25,.25,path);
        Assert.Equal(new CollisionVector(1,0,0),accepted.LinearVelocityAt(2));
        Assert.Equal(new CollisionVector(1,0,0),accepted.LinearVelocityAt(2.25));
        Assert.Equal(Math.Tau*3,accepted.PhysicalAngularSpeedBound);
        Assert.Throws<ArgumentOutOfRangeException>(()=>accepted.LinearVelocityAt(2.5));
        Assert.Throws<ArgumentOutOfRangeException>(()=>accepted.LinearVelocityAt(Math.BitDecrement(2)));
        Assert.Throws<ArgumentOutOfRangeException>(()=>accepted.LinearVelocityAt(double.NaN));
        Assert.Throws<InvalidOperationException>(()=>default(PhysicsMotionInterval).LinearVelocityAt(0));
        Assert.Throws<InvalidOperationException>(()=>default(PhysicsMotionInterval).PhysicalAngularSpeedBound);
        Assert.Equal(path.At(.125),accepted.At(2.125));
        Assert.Equal(path.At(.25),accepted.At(2.25));
        Assert.Throws<ArgumentOutOfRangeException>(()=>accepted.At(2.5));
        Assert.Throws<ArgumentOutOfRangeException>(()=>accepted.At(double.NaN));
        Assert.Throws<ArgumentException>(()=>new PhysicsMotionInterval(body.Id,0,2,2,path));
        Assert.Throws<InvalidOperationException>(()=>default(PhysicsMotionInterval).At(0));
        Assert.Throws<ArgumentException>(()=>new PhysicsMotionInterval(body.Id,2,2.5,.25,path));
        Assert.Throws<ArgumentException>(()=>new PhysicsMotionInterval(body.Id,2,2,.25,path));
        Assert.Throws<ArgumentException>(()=>new PhysicsMotionInterval(body.Id,2,double.PositiveInfinity,.25,path));
    }

    [Fact]
    public void ImpactSplitsAcceptedMotionAndFinalPoseMatchesCommittedBody()
    {
        var ball=Ball();
        var world=new PhysicsWorld([],[Object(ball),Wall()],[],new(default,maximumStep:1));
        Assert.Null(world.LastMotion);
        var initial=world.Capture();
        world.Step([],[],.2);
        var history=world.LastMotion!;
        Assert.Equal(1UL,history.StepIndex);
        Assert.Equal(0,history.StartTime);Assert.Equal(.2,history.EndTime);
        Assert.True(history.Intervals.Length>=4);
        Assert.Equal(ball.Pose,history.Sample(ball.Id,.2));
        Assert.InRange(history.Sample(ball.Id,.05).Center.X,-1.500001,-1.499999);
        Assert.True(history.Sample(ball.Id,.19).Center.X<history.Sample(ball.Id,.16).Center.X);
        Assert.Throws<ArgumentException>(()=>history.Sample(new(99),.1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>history.Sample(ball.Id,.21));
        var samples=Enumerable.Range(0,21).Select(i=>history.Sample(ball.Id,i*.01)).ToArray();
        world.Restore(initial);Assert.Null(world.LastMotion);
        world.Step([],[],.2);
        Assert.Equal(samples,Enumerable.Range(0,21).Select(i=>world.LastMotion!.Sample(ball.Id,i*.01)).ToArray());
        Assert.Equal(samples,Enumerable.Range(0,21).Select(i=>history.Sample(ball.Id,i*.01)).ToArray());
    }

    [Fact]
    public void CapacityFailureRestoresBodiesAndPreviousHistory()
    {
        var ball=Ball();
        var world=new PhysicsWorld([],[Object(ball),Wall()],[],new(default,maximumStep:1,maximumMotionIntervals:1));
        world.Step([],[],.01);
        var before=world.Capture();var history=world.LastMotion;
        Assert.Throws<InvalidOperationException>(()=>world.Step([],[],.2));
        Assert.Equal(before.Time,world.Time);
        Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Same(history,world.LastMotion);
        Assert.Equal(PhysicsWorldPhase.Idle,world.Phase);
        Assert.Throws<ArgumentException>(()=>new PhysicsWorldSettings(default,maximumMotionIntervals:0));
    }

    [Fact]
    public void OuterRollbackRestoresHistoryAcrossSeveralSteps()
    {
        var ball=Ball();var world=new PhysicsWorld([],[Object(ball)],[],new(default,maximumStep:.1));
        world.Step([],[],.1);
        var prior=world.LastMotion;
        var transaction=new SimulationTransaction([world]);
        transaction.Begin();
        world.Step([],[],.2);var retained=world.LastMotion!;
        world.Step([],[],.1);
        transaction.Rollback();
        Assert.Same(prior,world.LastMotion);
        Assert.Equal(.1,world.Time);
        Assert.InRange(retained.Sample(ball.Id,.3).Center.X,.999999,1.000001);
    }

    [Fact]
    public void PublishedMotionPreservesMultipleTurnsAndRightHandBoundaries()
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,RigidPose.Identity,default,new(0,0,Math.Tau*6));
        var world=new PhysicsWorld([],[Object(body)],[],new(default,maximumStep:.1));
        world.Step([],[],.5);
        var read=world.LastMotion!;
        foreach(var time in new[]{0,.05,.1,.137,.3,.5})
        {
            var expected=new CollisionVector(Math.Cos(Math.Tau*6*time),Math.Sin(Math.Tau*6*time),0);
            Assert.InRange((read.Sample(body.Id,time).Rotation.Apply(new(1,0,0))-expected).Length,0,1e-12);
        }
        var intervals=read.Intervals.ToArray();
        for(var i=1;i<intervals.Length;i++)
            Assert.Equal(intervals[i].At(intervals[i].StartTime),read.Sample(body.Id,intervals[i].StartTime));
        Assert.Equal(body.Pose,read.Sample(body.Id,read.EndTime));
    }

    [Theory]
    [InlineData(.7)]
    [InlineData(.9)]
    [InlineData(1)]
    [InlineData(.123)]
    [InlineData(.30833333333333335)]
    public void AdjacentIntervalsCoverEveryRepresentableBoundary(double duration)
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,RigidPose.Identity,new(1,0,0),default);
        var world=new PhysicsWorld([],[Object(body)],[],new(default,maximumStep:.1));
        world.Step([],[],1.2345);
        world.Step([],[],duration);
        var history=world.LastMotion!;
        var intervals=history.Intervals.ToArray();
        Assert.Equal(history.StartTime,intervals[0].StartTime);
        Assert.Equal(history.EndTime,intervals[^1].EndTime);
        for(var i=1;i<intervals.Length;i++)
        {
            Assert.Equal(intervals[i-1].EndTime,intervals[i].StartTime);
            var boundary=intervals[i].StartTime;
            foreach(var time in new[]{Math.BitDecrement(boundary),boundary,Math.BitIncrement(boundary)})
                Assert.InRange(Math.Abs(history.Sample(body.Id,time).Center.X-time),0,1e-12);
        }
    }
}
