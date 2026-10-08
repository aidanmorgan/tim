using CuriousContraptions.Presentation;
namespace CuriousContraptions.Tests;

public class AnimationFollowTests
{
    public static IEnumerable<object[]> Cases()
    {
        foreach(var clock in Enum.GetValues<AnimationClock>())
        foreach(var visible in new[]{false,true})
        foreach(var render in new[]{false,true})yield return [clock,visible,render];
    }
    [Theory]
    [MemberData(nameof(Cases))]
    public void AnalyticalResponseRetargetHoldAndResetIgnoreFrameSubdivision(AnimationClock clock,bool visible,bool render)
    {
        var batch=new AnimationBatch(1);
        var handle=batch.RegisterFollow(new(new(0),AnimationProperty.ColourBlend),new(0,1,0,2,clock));
        ulong frame=0;
        void Frame(double time)=>batch.Advance(++frame,clock==AnimationClock.Presentation?time:0,clock==AnimationClock.Simulation?time:0);
        batch.SetVisible(handle,visible);batch.FollowValue(handle,1,0);
        if(render)Frame(.25);
        batch.FollowValue(handle,.3,.25);
        var atTurn=1-Math.Exp(-.5);
        Assert.InRange(Math.Abs(batch.Read(handle).Value-atTurn),0,1e-15);
        Frame(.5);batch.SetVisible(handle,true);Frame(.5);
        var expected=.3+(atTurn-.3)*Math.Exp(-.5);
        Assert.InRange(Math.Abs(batch.Read(handle).Value-expected),0,1e-15);
        batch.Stop(handle,AnimationStop.Hold,.75);
        var held=.3+(atTurn-.3)*Math.Exp(-1);
        Assert.InRange(Math.Abs(batch.Read(handle).Value-held),0,1e-15);
        Frame(1);Assert.InRange(Math.Abs(batch.Read(handle).Value-held),0,1e-15);
        batch.FollowValue(handle,.8,1);Frame(2);
        Assert.InRange(Math.Abs(batch.Read(handle).Value-(.8+(held-.8)*Math.Exp(-2))),0,1e-15);
        batch.Stop(handle,AnimationStop.RestoreInitial,2);Frame(2);Assert.Equal(0,batch.Read(handle).Value);
        batch.FollowValue(handle,1,2);Frame(1000);
        Assert.Equal(1,batch.Read(handle).Value);Assert.Equal(AnimationPlayback.Completed,batch.Read(handle).Playback);
        Assert.Equal(0,batch.ScheduledCount);
        batch.Reset();Assert.Equal(0,Assert.Single(batch.Samples.ToArray()).Value);
        Assert.Throws<ArgumentException>(()=>batch.FollowValue(handle,1,0));
    }
    [Fact]
    public void ReversedRangeAndLargeResponseConvergeWithoutOvershoot()
    {
        var batch=new AnimationBatch(1);
        var handle=batch.RegisterFollow(new(new(0),AnimationProperty.LocalRotationAngle),new(1,-1,1,double.MaxValue,AnimationClock.Presentation));
        batch.FollowValue(handle,-1,0);batch.Advance(1,double.MaxValue,0);
        Assert.Equal(-1,batch.Read(handle).Value);Assert.Equal(AnimationPlayback.Completed,batch.Read(handle).Playback);
    }
    [Fact]
    public void InvalidControlsAndCrossModeCallsPreserveStateAndWriterOwnership()
    {
        var batch=new AnimationBatch(2);
        var key=new AnimationBinding(new(0),AnimationProperty.Opacity);
        var follow=batch.RegisterFollow(key,new(0,1,0,2,AnimationClock.Presentation));
        var clip=batch.Register(new(new(1),AnimationProperty.Opacity),new(0,1,1,AnimationCurve.Linear,AnimationRepeat.Once,AnimationClock.Presentation));
        Assert.Throws<InvalidOperationException>(()=>batch.Register(key,new(0,1,1,AnimationCurve.Linear,AnimationRepeat.Once,AnimationClock.Presentation)));
        Assert.Throws<InvalidOperationException>(()=>batch.RegisterFollow(new(new(1),AnimationProperty.Opacity),new(0,1,0,2,AnimationClock.Presentation)));
        batch.FollowValue(follow,1,1);var before=batch.Read(follow);
        foreach(var invalid in new[]{-.1,1.1,double.NaN,double.PositiveInfinity,double.NegativeInfinity})
            Assert.Throws<ArgumentOutOfRangeException>(()=>batch.FollowValue(follow,invalid,1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>batch.FollowValue(follow,1,.5));
        Assert.Throws<ArgumentOutOfRangeException>(()=>batch.FollowValue(follow,1,double.NaN));
        Assert.Throws<InvalidOperationException>(()=>batch.StartAt(follow,1));
        Assert.Throws<InvalidOperationException>(()=>batch.DriveTo(follow,AnimationEndpoint.To,1));
        Assert.Throws<InvalidOperationException>(()=>batch.SetLoopRate(follow,1,1));
        Assert.Throws<InvalidOperationException>(()=>batch.FollowValue(clip,1,1));
        Assert.Throws<ArgumentException>(()=>batch.Advance(1,.5,0));
        Assert.Equal(before,batch.Read(follow));
        batch.Advance(1,1,0);Assert.Equal(0,batch.Read(follow).Value);
        batch.Remove(follow);Assert.Throws<ArgumentException>(()=>batch.FollowValue(follow,1,1));
        var replacement=batch.RegisterFollow(key,new(0,1,0,2,AnimationClock.Presentation));
        Assert.NotEqual(follow.Version,replacement.Version);
        Assert.Throws<ArgumentException>(()=>new AnimationBatch(1).FollowValue(replacement,1,1));
    }
    [Fact]
    public void DefinitionAndPropertyRangesRejectUnsupportedValues()
    {
        foreach(var invalid in new[]{0d,-1,double.NaN,double.PositiveInfinity})
            Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationFollowDefinition(0,1,0,invalid,AnimationClock.Presentation));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationFollowDefinition(-double.MaxValue,double.MaxValue,0,1,AnimationClock.Presentation));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationFollowDefinition(0,1,0,1,(AnimationClock)999));
        var batch=new AnimationBatch(1);
        Assert.Throws<ArgumentException>(()=>batch.RegisterFollow(new(new(0),AnimationProperty.Opacity),new(-1,1,-1,2,AnimationClock.Presentation)));
        Assert.Throws<ArgumentException>(()=>batch.RegisterFollow(new(new(0),AnimationProperty.UniformScale),new(0,1,0,2,AnimationClock.Presentation)));
        Assert.Throws<ArgumentOutOfRangeException>(()=>batch.RegisterFollow(new(new(0),(AnimationProperty)999),new(0,1,0,2,AnimationClock.Presentation)));
        Assert.Equal(0,batch.RegisteredCount);
    }
    [Fact]
    public void WarmFollowChangesFramesAndVisibilityAllocateNothing()
    {
        var batch=new AnimationBatch(1);
        var handle=batch.RegisterFollow(new(new(0),AnimationProperty.Opacity),new(0,1,0,18,AnimationClock.Presentation));
        for(var i=0;i<100;i++){batch.FollowValue(handle,i%2,i*.01);batch.Advance((ulong)i+1,i*.01,0);}
        var before=GC.GetAllocatedBytesForCurrentThread();
        for(var i=100;i<1100;i++)
        {
            batch.FollowValue(handle,i%2,i*.01);batch.SetVisible(handle,i%3!=0);
            batch.Advance((ulong)i+1,i*.01,0);
        }
        var bytes=GC.GetAllocatedBytesForCurrentThread()-before;
        Assert.Equal(0,bytes);
    }
}
