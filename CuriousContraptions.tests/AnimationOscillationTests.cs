using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

public class AnimationOscillationTests
{
    private static AnimationOscillationDefinition Definition(AnimationClock clock=AnimationClock.Presentation)=>new(7,48,5,clock);
    private static double Response(double age,double strength)=>strength*5/48*Math.Exp(-7*age)*Math.Sin(48*age);
    private static AnimationHandle Register(AnimationBatch batch,AnimationClock clock=AnimationClock.Presentation)=>
        batch.RegisterOscillation(new(new(0),AnimationProperty.LocalRotationAngle),Definition(clock));
    [Theory]
    [InlineData(AnimationClock.Presentation)]
    [InlineData(AnimationClock.Simulation)]
    public void AnalyticSuperpositionSurvivesSkippedHiddenFramesAndPause(AnimationClock clock)
    {
        var batch=new AnimationBatch(1);var handle=Register(batch,clock);
        batch.KickOscillation(handle,new(1),1,0);
        batch.Advance(1,.05,clock==AnimationClock.Simulation?.05:0);
        Assert.InRange(Math.Abs(batch.Read(handle).Value-Response(.05,1)),0,1e-15);
        batch.KickOscillation(handle,new(2),-.3,.05);
        batch.SetVisible(handle,false);var work=batch.Advance(2,.1,clock==AnimationClock.Simulation?.1:0);
        Assert.Equal(0,work.DirtyWrites);
        batch.SetVisible(handle,true);batch.Advance(3,.2,clock==AnimationClock.Simulation?.2:0);
        var expected=Response(.2,1)+Response(.15,-.3);
        Assert.InRange(Math.Abs(batch.Read(handle).Value-expected),0,1e-15);
        var read=batch.ReadOscillation(handle);Assert.Equal(2UL,read.Accepted);Assert.Equal(new(2),read.LastOccurrence);
        batch.Advance(4,.3,clock==AnimationClock.Simulation?.2:0);
        var elapsed=clock==AnimationClock.Simulation?.2:.3;
        Assert.InRange(Math.Abs(batch.Read(handle).Value-(Response(elapsed,1)+Response(elapsed-.05,-.3))),0,1e-15);
    }
    [Fact]
    public void RenderCadenceDoesNotAccumulateIntegrationError()
    {
        var fine=new AnimationBatch(1);var coarse=new AnimationBatch(1);var a=Register(fine);var b=Register(coarse);
        fine.KickOscillation(a,new(1),1,0);coarse.KickOscillation(b,new(1),1,0);
        for(ulong i=1;i<=120;i++)fine.Advance(i,i/120.0,0);
        coarse.Advance(1,1,0);Assert.Equal(fine.Read(a),coarse.Read(b));
        Assert.Equal(fine.ReadOscillation(a),coarse.ReadOscillation(b));
        coarse.Advance(2,double.MaxValue,0);
        Assert.Equal(0,coarse.Read(b).Value);Assert.Equal(AnimationPlayback.Completed,coarse.Read(b).Playback);
    }
    [Theory]
    [InlineData(0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(1.01)]
    [InlineData(-1.01)]
    public void InvalidStrengthRejectsWithoutChangingAcceptedMotion(double strength)
    {
        var batch=new AnimationBatch(1);var handle=Register(batch);batch.KickOscillation(handle,new(1),1,0);
        batch.Advance(1,.1,0);var before=batch.ReadOscillation(handle);
        Assert.Throws<ArgumentOutOfRangeException>(()=>batch.KickOscillation(handle,new(2),strength,.1));
        Assert.Equal(before,batch.ReadOscillation(handle));
    }
    [Fact]
    public void IdentityClockAndEnvelopeRejectionAreAtomicAndPreflightDoesNotAdmit()
    {
        var batch=new AnimationBatch(1);var handle=Register(batch);
        batch.ValidateOscillationKick(handle,new(1),1,0);Assert.Equal(0UL,batch.ReadOscillation(handle).Accepted);
        batch.KickOscillation(handle,new(1),1,0);batch.Advance(1,.1,0);var before=batch.ReadOscillation(handle);
        Assert.Throws<ArgumentException>(()=>batch.KickOscillation(handle,new(1),1,.1));
        Assert.Throws<ArgumentException>(()=>batch.KickOscillation(handle,default,1,.1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>batch.KickOscillation(handle,new(2),1,.09));
        Assert.Equal(before,batch.ReadOscillation(handle));
        batch.KickOscillation(handle,new(2),1,.2);
        Assert.Throws<ArgumentException>(()=>batch.Advance(2,.15,0));batch.Advance(2,.2,0);
        var bounded=new AnimationBatch(1);var motion=bounded.RegisterOscillation(new(new(0),AnimationProperty.LocalTranslation),new(1,1,1e6,AnimationClock.Presentation));
        Assert.Throws<ArgumentOutOfRangeException>(()=>bounded.KickOscillation(motion,new(1),1,0));
        Assert.Equal(0UL,bounded.ReadOscillation(motion).Accepted);
        bounded.KickOscillation(motion,new(1),.1,0);Assert.Equal(1UL,bounded.ReadOscillation(motion).Accepted);
    }
    [Theory]
    [InlineData(AnimationStop.Hold)]
    [InlineData(AnimationStop.RestoreInitial)]
    public void StopRemovalAndResetHaveExplicitLifecycle(AnimationStop stop)
    {
        var batch=new AnimationBatch(1);var handle=Register(batch);batch.KickOscillation(handle,new(1),1,0);
        batch.Advance(1,.1,0);batch.Stop(handle,stop,.1);batch.Advance(2,.2,0);
        Assert.Equal(stop==AnimationStop.Hold?Response(.1,1):0,batch.Read(handle).Value,14);
        Assert.Equal(0,batch.ReadOscillation(handle).Velocity);
        batch.Remove(handle);Assert.Throws<ArgumentException>(()=>batch.KickOscillation(handle,new(2),1,.2));
        var next=Register(batch);batch.KickOscillation(next,new(1),1,.2);batch.Reset();
        Assert.Throws<ArgumentException>(()=>batch.ReadOscillation(next));Assert.Equal(0,batch.RegisteredCount);
    }
    public enum Field { Decay, Frequency, Gain }
    [Theory]
    [InlineData(Field.Decay,0)]
    [InlineData(Field.Decay,1e7)]
    [InlineData(Field.Decay,double.NaN)]
    [InlineData(Field.Frequency,0)]
    [InlineData(Field.Frequency,1e7)]
    [InlineData(Field.Frequency,double.PositiveInfinity)]
    [InlineData(Field.Gain,0)]
    [InlineData(Field.Gain,1e7)]
    [InlineData(Field.Gain,double.NaN)]
    public void InvalidDefinitionsReject(Field field,double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationOscillationDefinition(
            field==Field.Decay?value:7,field==Field.Frequency?value:48,field==Field.Gain?value:5,AnimationClock.Presentation));
    }
    [Fact]
    public void UnsupportedClockPropertyAndControlReject()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationOscillationDefinition(7,48,5,(AnimationClock)999));
        var batch=new AnimationBatch(1);
        foreach(var property in Enum.GetValues<AnimationProperty>())
            if(property is not (AnimationProperty.LocalRotationAngle or AnimationProperty.LocalTranslation))
                Assert.Throws<ArgumentException>(()=>batch.RegisterOscillation(new(new(0),property),Definition()));
        var handle=Register(batch);
        Assert.Throws<InvalidOperationException>(()=>batch.Start(handle));
        Assert.Throws<InvalidOperationException>(()=>batch.DriveTo(handle,AnimationEndpoint.To,0));
        Assert.Throws<InvalidOperationException>(()=>batch.FollowValue(handle,1,0));
    }
    [Theory]
    [InlineData(1)]
    [InlineData(64)]
    [InlineData(256)]
    public void WarmedKickAndFrameProcessingAllocatesNoManagedScratch(int count)
    {
        var batch=new AnimationBatch(count);var handles=new AnimationHandle[count];
        for(var i=0;i<count;i++)handles[i]=batch.RegisterOscillation(new(new(i),AnimationProperty.LocalRotationAngle),Definition());
        void Frames(ulong first,ulong end)
        {
            for(var frame=first;frame<=end;frame++)
            {
                var time=frame/120.0;
                foreach(var handle in handles)batch.KickOscillation(handle,new(frame),.01,time);
                batch.Advance(frame,time,0);
            }
        }
        Frames(1,100);var before=GC.GetAllocatedBytesForCurrentThread();Frames(101,1100);
        Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread()-before);
        Assert.All(handles,h=>Assert.Equal(1100UL,batch.ReadOscillation(h).Accepted));
    }
}
