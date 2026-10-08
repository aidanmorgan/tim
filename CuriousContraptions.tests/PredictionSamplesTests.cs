using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class PredictionSamplesTests
{
    private static PhysicsBody Body(int id)=>new(new(id),PhysicsMotionType.Dynamic,
        RigidPose.Identity,new(1,2,3),new(.2,.3,.4),2,new(2,3,4));
    private static Dictionary<PhysicsBodyId,BodyWrench> Forces(params PhysicsBody[] bodies)=>
        bodies.ToDictionary(b=>b.Id,_=>default(BodyWrench));

    [Fact]
    public void ExactInputsReuseSamplesAndChangedWrenchInvalidatesOnlyItsBody()
    {
        var a=Body(0);var b=Body(1);var forces=Forces(a,b);
        var cache=new PredictionSamples([a,b]);
        var first=cache.Capture(forces,.01);
        var mid=cache.Sample(first,PredictionStage.Midpoint);
        var end=cache.Sample(first,PredictionStage.Endpoint);
        var repeated=cache.Capture(forces,.01);
        Assert.Same(first[a.Id],repeated[a.Id]);
        Assert.Same(mid[b.Id],cache.Sample(repeated,PredictionStage.Midpoint)[b.Id]);
        Assert.Same(end[a.Id],cache.Sample(repeated,PredictionStage.Endpoint)[a.Id]);
        forces[a.Id]=new(new(1,0,0),new(0,.1,0));
        var changed=cache.Capture(forces,.01);
        Assert.NotSame(first[a.Id],changed[a.Id]);Assert.Same(first[b.Id],changed[b.Id]);
        var changedMid=cache.Sample(changed,PredictionStage.Midpoint);
        Assert.NotSame(mid[a.Id],changedMid[a.Id]);Assert.Same(mid[b.Id],changedMid[b.Id]);
        Assert.Equal(a.CreateTrajectory(.01,forces[a.Id]).SampleBody(.005).Snapshot(),changedMid[a.Id].Snapshot());
        Assert.Equal(first[a.Id].At(.01),a.CreateTrajectory(.01,default).At(.01));
        Assert.Throws<ArgumentException>(()=>cache.Sample(first,PredictionStage.Endpoint));
    }

    [Fact]
    public void ShortenedHorizonRebuildsBothStagesExactly()
    {
        var body=Body(0);var forces=Forces(body);var cache=new PredictionSamples([body]);
        var full=cache.Capture(forces,.02);
        var mid=cache.Sample(full,PredictionStage.Midpoint)[body.Id];
        var end=cache.Sample(full,PredictionStage.Endpoint)[body.Id];
        var shortPaths=cache.Capture(forces,.01);
        var shortMid=cache.Sample(shortPaths,PredictionStage.Midpoint)[body.Id];
        var shortEnd=cache.Sample(shortPaths,PredictionStage.Endpoint)[body.Id];
        Assert.NotSame(mid,shortMid);Assert.NotSame(end,shortEnd);
        var direct=body.CreateTrajectory(.01,default);
        Assert.Equal(direct.SampleBody(.005).Snapshot(),shortMid.Snapshot());
        Assert.Equal(direct.SampleBody(.01).Snapshot(),shortEnd.Snapshot());
    }

    [Fact]
    public void SourceRevisionAndBorrowedSampleMutationCannotBeReused()
    {
        var body=Body(0);var forces=Forces(body);var cache=new PredictionSamples([body]);
        var paths=cache.Capture(forces,.01);
        body.Restore(body.Snapshot());
        Assert.Throws<InvalidOperationException>(()=>cache.Capture(forces,.01));
        var fresh=new PredictionSamples([body]);
        paths=fresh.Capture(forces,.01);
        var sample=fresh.Sample(paths,PredictionStage.Midpoint)[body.Id];
        sample.ApplyImpulse(new(1,0,0),sample.Center);
        Assert.Throws<InvalidOperationException>(()=>fresh.Capture(forces,.01));
        Assert.Throws<InvalidOperationException>(()=>fresh.Sample(paths,PredictionStage.Endpoint));
    }

    [Fact]
    public void UnsupportedStagesForeignIdentitiesAndInvalidHorizonsReject()
    {
        var body=Body(0);var cache=new PredictionSamples([body]);var forces=Forces(body);
        var paths=cache.Capture(forces,.01);
        Assert.Throws<ArgumentOutOfRangeException>(()=>cache.Sample(paths,(PredictionStage)99));
        Assert.Throws<ArgumentOutOfRangeException>(()=>cache.Capture(forces,double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(()=>cache.Capture(forces,0));
        Assert.Throws<ArgumentException>(()=>cache.Capture(Forces(Body(1)),.01));
        Assert.Throws<ArgumentException>(()=>cache.Sample(new Dictionary<PhysicsBodyId,BodyTrajectory>(),PredictionStage.Midpoint));
        Assert.Throws<ArgumentException>(()=>new PredictionSamples([body,body]));
    }
}
