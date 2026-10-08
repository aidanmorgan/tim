using Godot;
using System.Diagnostics;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class CompoundBoundsAllocationTests(ITestOutputHelper output)
{
    public enum Scene { Sparse, Dense, Hollow }
    private static CompoundGeometry Geometry(int count,bool coincident)=>new(
        Enumerable.Range(0,count).Select(i=>new ConvexInstance(new ConvexBox(new(.2,.2,.2)),
            new(AffineBasis.Identity,new(coincident?0:i*2,0,0)))).ToArray());
    private static CompoundMotion At(CompoundGeometry geometry,CollisionVector center)=>new(
        geometry,new ConfigurationTrajectory(RigidPose.At(center),default,default));

    [Theory]
    [InlineData(Scene.Sparse)]
    [InlineData(Scene.Dense)]
    [InlineData(Scene.Hollow)]
    public void MeasureWarmQueriesAndKeepRetainedCandidates(Scene scene)
    {
        var pair=scene switch
        {
            Scene.Sparse=>(At(Geometry(1,false),new(4000,0,0)),At(Geometry(4096,false),default)),
            Scene.Dense=>(At(Geometry(64,true),default),At(Geometry(64,true),default)),
            Scene.Hollow=>(At(Geometry(1,false),new(2.4,0,0)),
                At(HollowGeometry.Bend(2.4,Math.PI/2,.65,.7,new(.005)).Geometry,default)),
            _=>throw new ArgumentOutOfRangeException(nameof(scene))
        };
        var retained=CompoundCollision.Candidates(pair.Item1,pair.Item2,0,0);
        var expected=retained.Pairs.ToArray();
        for(var i=0;i<20;i++)CompoundCollision.Candidates(pair.Item1,pair.Item2,0,0);
        const int count=100;
        var bytes=GC.GetAllocatedBytesForCurrentThread();var start=Stopwatch.GetTimestamp();
        for(var i=0;i<count;i++)CompoundCollision.Candidates(pair.Item1,pair.Item2,0,0);
        var elapsed=Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        bytes=GC.GetAllocatedBytesForCurrentThread()-bytes;
        output.WriteLine($"Scene={scene}; queries={count}; nodes={retained.NodeTests}; leaves={retained.LeafTests}; candidates={retained.Pairs.Count}; total ms={elapsed:R}; allocated bytes={bytes}");
        Assert.Equal(expected,retained.Pairs);
        Assert.Equal(expected,CompoundCollision.Candidates(pair.Item1,pair.Item2,0,0).Pairs);
    }

    [Fact]
    public async Task SharedGeometryQueriesDoNotShareMutableBoundsOrCandidateResults()
    {
        var geometry=Geometry(128,false);
        var target=At(geometry,default);
        var retained=CompoundCollision.Candidates(At(Geometry(1,false),new(80,0,0)),target,0,0);
        var probe=Geometry(1,false);
        await Task.WhenAll(Enumerable.Range(0,8).Select(worker=>Task.Run(()=>
        {
            for(var i=0;i<40;i++)
            {
                var child=(worker*13+i)%128;
                var result=CompoundCollision.Candidates(At(probe,new(child*2,0,0)),target,0,0);
                Assert.Equal(new ColliderChildId(child),Assert.Single(result.Pairs).B);
                Assert.Empty(CompoundCollision.Candidates(At(probe,new(-100,0,0)),target,0,0).Pairs);
            }
        })));
        Assert.Equal(new ColliderChildId(40),Assert.Single(retained.Pairs).B);
    }

    private sealed class FaultingTrajectory(IRigidTrajectory inner,int failureCall) : IRigidTrajectory
    {
        private int _calls;
        public double Duration=>inner.Duration;
        public RigidPose StartPose=>inner.StartPose;
        public double LinearAccelerationBound=>inner.LinearAccelerationBound;
        public double AngularAccelerationBound=>inner.AngularAccelerationBound;
        public double AngularSpeedBound=>inner.AngularSpeedBound;
        public CollisionVector LinearVelocityAt(double time)=>inner.LinearVelocityAt(time);
        public double SegmentEndAfter(double time)=>inner.SegmentEndAfter(time);
        public RigidPose At(double time)=>++_calls==failureCall?
            throw new InvalidOperationException("Deliberate captured-path failure."):inner.At(time);
    }

    [Fact]
    public void SameTreeOnBothSidesAndFailedQueriesCannotLeaveStaleBounds()
    {
        var geometry=Geometry(64,false);
        var first=At(geometry,default);var second=At(geometry,new(2,0,0));
        var result=CompoundCollision.Candidates(first,second,0,0);
        var expected=Enumerable.Range(1,63).Select(i=>new ColliderChildPair(new(i),new(i-1))).ToArray();
        Assert.Equal(expected,result.Pairs);
        for(var i=0;i<20;i++)
        {
            var failed=new CompoundMotion(geometry,new FaultingTrajectory(
                new ConfigurationTrajectory(RigidPose.At(default),default,default),4));
            Assert.Throws<InvalidOperationException>(()=>CompoundCollision.Candidates(failed,second,0,0));
            Assert.Empty(CompoundCollision.Candidates(first,At(geometry,new(-1000,0,0)),0,0).Pairs);
            Assert.Equal(expected,CompoundCollision.Candidates(first,second,0,0).Pairs);
        }
        Assert.Equal(expected,result.Pairs);
    }

}
