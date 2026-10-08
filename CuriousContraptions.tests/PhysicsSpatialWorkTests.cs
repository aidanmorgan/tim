using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class PhysicsSpatialWorkTests
{
    private static PhysicsBody Ball(CollisionVector position,CollisionVector velocity)=>
        new(new(0),PhysicsMotionType.Dynamic,RigidPose.At(position),velocity,default,1,new(.1,.1,.1));
    private static PhysicsObject Sphere(PhysicsBody body)=>
        new(body,new([new(new ConvexSphere(.5),AffineTransform.Identity)]),new(1,0,0));
    private static PhysicsObject Wall(int id,double x)=>
        new(new(new(id),PhysicsMotionType.Static,RigidPose.At(new(x,0,0)),default,default),
            new([new(new ConvexBox(new(.01,10,10)),AffineTransform.Identity)]),new(1,0,0));

    [Theory]
    [InlineData(0,true)]
    [InlineData(100,false)]
    public void StepCountsActualQueriesAndReplaysWithPhysicalState(double wall,bool hits)
    {
        var ball=Ball(new(-5,0,0),new(1000,0,0));
        var world=new PhysicsWorld([], [Sphere(ball),Wall(1,wall)],[],new(default,maximumStep:1));
        var initial=world.Capture();
        var result=world.Step([],[],.01);
        var final=world.Capture();
        result.SpatialWork.Validate();
        Assert.True(result.SpatialWork.BodyQueries>0);
        Assert.True(result.SpatialWork.BodyNodeTests>0);
        Assert.Equal(hits,result.SpatialWork.CompoundQueries>0);
        Assert.Equal(hits,result.SpatialWork.CompoundCandidatePairs>0);
        Assert.Equal(hits?1:0,result.Events);
        Assert.Equal(hits?-1000:1000,ball.LinearVelocity.X,6);
        world.Restore(initial);
        Assert.Null(world.FindPlacementOverlap()); // Outside-step work cannot leak into the next result.
        Assert.Equal(result,world.Step([],[],.01));
        Assert.Equal(final.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
    }

    [Fact]
    public void FailedStepWorkDoesNotLeakIntoNextSuccessfulStep()
    {
        var ball=Ball(default,new(10,0,0));
        var world=new PhysicsWorld([], [Sphere(ball),Wall(1,-2),Wall(2,2)],[],
            new(default,maximumStep:1,maximumEvents:1));
        var initial=world.Capture();
        var expected=world.Step([],[],.01);
        world.Restore(initial);
        Assert.Throws<InvalidOperationException>(()=>world.Step([],[],.8));
        Assert.Equal(initial.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(expected,world.Step([],[],.01));
    }

    [Theory]
    [InlineData(-1,0,0,0)]
    [InlineData(0,1,0,0)]
    [InlineData(1,0,1,0)]
    [InlineData(1,1,0,1)]
    public void InvalidSpatialCountsRejectBeforeRecording(long queries,long nodes,long leaves,long pairs)
    {
        foreach(var work in new PhysicsSpatialWork[]{
            new(queries,nodes,leaves,pairs,0,0,0,0),
            new(0,0,0,0,queries,nodes,leaves,pairs)})
        {
            var recorder=new PerformanceRecorder();
            recorder.BeginRun();recorder.BeginTick(0);recorder.Begin(PerformanceStage.Physics);
            Assert.Throws<ArgumentOutOfRangeException>(()=>recorder.RecordPhysicsStep(new(1,0,0,0,0,0,0,0,0,work,default,default)));
            recorder.EndTick(PerformanceOutcome.Failed);
            Assert.Empty(recorder.Drain().Counters);
        }
    }
}
