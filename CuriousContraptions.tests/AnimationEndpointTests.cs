using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

public class AnimationEndpointTests
{
    public static IEnumerable<object[]> Cases()
    {
        foreach(var curve in Enum.GetValues<AnimationCurve>())
        foreach(var clock in Enum.GetValues<AnimationClock>())
        foreach(var renderAtReversal in new[]{false,true})
        foreach(var visible in new[]{false,true})
            yield return [curve,clock,renderAtReversal,visible];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void ReverseUsesTheControlBoundaryAndPreservesPhaseAcrossSkippedFrames(
        AnimationCurve curve,AnimationClock clock,bool renderAtReversal,bool visible)
    {
        var batch=new AnimationBatch(1);
        var handle=batch.Register(new(new(0),AnimationProperty.Opacity),
            new(0,1,2,curve,AnimationRepeat.Once,clock));
        batch.SetVisible(handle,visible);
        batch.DriveTo(handle,AnimationEndpoint.To,0);
        ulong frame=0;
        if(renderAtReversal)batch.Advance(++frame,1,1);
        batch.DriveTo(handle,AnimationEndpoint.From,1);
        Assert.Equal(curve==AnimationCurve.SinePulse?1:.5,batch.Read(handle).Value);
        batch.Advance(++frame,1.5,1.5);
        batch.SetVisible(handle,true);
        batch.Advance(++frame,1.5,1.5);
        var expected=curve switch
        {
            AnimationCurve.Linear=>.25,
            AnimationCurve.SmoothStep=>.103515625,
            AnimationCurve.SinePulse=>Math.Sqrt(.5),
            _=>throw new ArgumentOutOfRangeException(nameof(curve))
        };
        Assert.InRange(Math.Abs(batch.Read(handle).Value-expected),0,2e-16);
        batch.Advance(++frame,2,2);
        Assert.Equal(0,batch.Read(handle).Value);
        Assert.Equal(AnimationPlayback.Completed,batch.Read(handle).Playback);
        Assert.Equal(0,batch.ScheduledCount);
        batch.DriveTo(handle,AnimationEndpoint.To,2);
        batch.Advance(++frame,4,4);
        Assert.Equal(curve==AnimationCurve.SinePulse?0:1,batch.Read(handle).Value);
        Assert.Equal(AnimationPlayback.Completed,batch.Read(handle).Playback);
    }

    [Fact]
    public void HoldResumeRestoreAndRestartHaveDistinctPhaseSemantics()
    {
        var batch=new AnimationBatch(1);
        var handle=batch.Register(new(new(0),AnimationProperty.LocalRotationAngle),
            new(2,10,2,AnimationCurve.Linear,AnimationRepeat.Once,AnimationClock.Presentation));
        batch.DriveTo(handle,AnimationEndpoint.To,0);
        batch.Stop(handle,AnimationStop.Hold,.5);
        Assert.Equal(4,batch.Read(handle).Value);
        batch.DriveTo(handle,AnimationEndpoint.From,1);
        batch.Advance(1,1.25,0);
        Assert.Equal(3,batch.Read(handle).Value);
        batch.Stop(handle,AnimationStop.RestoreInitial,1.25);
        batch.DriveTo(handle,AnimationEndpoint.To,1.25);
        batch.Advance(2,1.5,0);Assert.Equal(3,batch.Read(handle).Value);
        batch.StartAt(handle,1.5);Assert.Equal(2,batch.Read(handle).Value);
        batch.Advance(3,3.5,0);Assert.Equal(10,batch.Read(handle).Value);
    }

    [Fact]
    public void InvalidEndpointClockRepeatAndStaleHandlesRejectWithoutMutation()
    {
        var batch=new AnimationBatch(2);
        var handle=batch.Register(new(new(0),AnimationProperty.Opacity),
            new(0,1,2,AnimationCurve.Linear,AnimationRepeat.Once,AnimationClock.Simulation));
        batch.DriveTo(handle,AnimationEndpoint.To,1);
        var before=batch.Read(handle);
        Assert.Throws<ArgumentOutOfRangeException>(()=>batch.DriveTo(handle,(AnimationEndpoint)999,1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>batch.DriveTo(handle,AnimationEndpoint.From,.5));
        Assert.Throws<ArgumentOutOfRangeException>(()=>batch.DriveTo(handle,AnimationEndpoint.From,double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(()=>batch.DriveTo(handle,AnimationEndpoint.From,double.PositiveInfinity));
        Assert.Throws<ArgumentException>(()=>batch.Advance(1,2,.5));
        Assert.Equal(before,batch.Read(handle));
        batch.Advance(1,2,1);Assert.Equal(0,batch.Read(handle).Value);
        batch.Advance(2,4,2);Assert.Equal(.5,batch.Read(handle).Value);
        foreach(var repeat in new[]{AnimationRepeat.Loop,AnimationRepeat.PingPong})
        {
            var loop=batch.Register(new(new(1),AnimationProperty.Opacity),
                new(0,1,1,AnimationCurve.Linear,repeat,AnimationClock.Presentation));
            Assert.Throws<InvalidOperationException>(()=>batch.DriveTo(loop,AnimationEndpoint.To,4));
            Assert.Equal(AnimationPlayback.Stopped,batch.Read(loop).Playback);
            batch.Remove(loop);
        }
        batch.Reset();
        Assert.Throws<ArgumentException>(()=>batch.DriveTo(handle,AnimationEndpoint.From,0));
    }

    [Fact]
    public void WarmedEndpointChangesAndFramesAllocateNoManagedMemory()
    {
        var batch=new AnimationBatch(1);
        var handle=batch.Register(new(new(0),AnimationProperty.Opacity),
            new(0,1,1,AnimationCurve.SmoothStep,AnimationRepeat.Once,AnimationClock.Presentation));
        void Frame(int index)
        {
            var time=index*.125;
            batch.DriveTo(handle,index%2==0?AnimationEndpoint.To:AnimationEndpoint.From,time);
            batch.Advance((ulong)index+1,time+.125,0);
        }
        for(var i=0;i<100;i++)Frame(i);
        var before=GC.GetAllocatedBytesForCurrentThread();
        for(var i=100;i<1100;i++)Frame(i);
        Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread()-before);
    }
}
