using CuriousContraptions.Bridge;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

public class AnimationImpulseTests
{
    private static AnimationBinding Binding=>new(new(0),AnimationProperty.ColourBlend);
    private static AnimationImpulseDefinition Definition(AnimationImpulseOverlap overlap=AnimationImpulseOverlap.Maximum,
        AnimationImpulseVisibility visibility=AnimationImpulseVisibility.DeferUntilVisible,
        AnimationClock clock=AnimationClock.Presentation,int capacity=4,AnimationImpulseCurve curve=AnimationImpulseCurve.LinearDecay,
        AnimationImpulseTiming timing=AnimationImpulseTiming.FirstPresentation)
        =>new(1,curve,overlap,visibility,clock,capacity,timing,0);
    private static PoseReadStamp Stamp(long revision)=>new(new(1),new(revision),revision);
    private readonly record struct Pulse(double Strength);

    [Theory]
    [InlineData(AnimationImpulseOverlap.Maximum,AnimationImpulseCurve.LinearDecay)]
    [InlineData(AnimationImpulseOverlap.Maximum,AnimationImpulseCurve.SmoothDecay)]
    [InlineData(AnimationImpulseOverlap.SaturatingSum,AnimationImpulseCurve.LinearDecay)]
    [InlineData(AnimationImpulseOverlap.SaturatingSum,AnimationImpulseCurve.SmoothDecay)]
    public void OverlapPreservesIndependentOccurrencesAndFirstRenderedPeaks(AnimationImpulseOverlap overlap,AnimationImpulseCurve curve)
    {
        var batch=new AnimationBatch(1);var handle=batch.RegisterImpulses(Binding,Definition(overlap:overlap,curve:curve),0,1);
        Assert.Equal(AnimationImpulseAdmission.Accepted,batch.EnqueueImpulse(handle,new(1),.2,0));
        Assert.Equal(AnimationImpulseAdmission.Accepted,batch.EnqueueImpulse(handle,new(2),.3,0));
        Assert.Equal(2,batch.ReadImpulses(handle).Pending);Assert.Equal(0,batch.Read(handle).Value);
        batch.Advance(1,100,0); // No frame for a long time cannot expire pending occurrences.
        Assert.Equal(overlap==AnimationImpulseOverlap.Maximum?.3:.5,batch.Read(handle).Value);
        Assert.Equal(2,batch.ReadImpulses(handle).Playing);
        batch.EnqueueImpulse(handle,new(3),.4,0);batch.Advance(2,100.5,0);
        Assert.InRange(Math.Abs(batch.Read(handle).Value-(overlap==AnimationImpulseOverlap.Maximum?.4:.65)),0,1e-14);
        Assert.Equal(3,batch.ReadImpulses(handle).Playing);
        batch.Advance(3,101,0);
        Assert.InRange(Math.Abs(batch.Read(handle).Value-.2),0,1e-14);
        Assert.Equal(2UL,batch.ReadImpulses(handle).Completed);Assert.Equal(1,batch.ReadImpulses(handle).Playing);
        batch.Advance(4,101.5,0);
        Assert.Equal(0,batch.Read(handle).Value);Assert.Equal(AnimationPlayback.Completed,batch.Read(handle).Playback);
        Assert.Equal(new AnimationImpulseRead(0,0,3,3,0,3,0),batch.ReadImpulses(handle));
        Assert.Equal(0,batch.ScheduledCount);
    }

    [Theory]
    [InlineData(AnimationImpulseVisibility.DeferUntilVisible)]
    [InlineData(AnimationImpulseVisibility.AdvanceWhileHidden)]
    public void HiddenPolicyIsExplicitAndDoesNotEvaluateInvisibleEnvelopes(AnimationImpulseVisibility visibility)
    {
        var batch=new AnimationBatch(1);var handle=batch.RegisterImpulses(Binding,Definition(visibility:visibility),0,1);
        batch.SetVisible(handle,false);batch.EnqueueImpulse(handle,new(1),1,0);
        Assert.Equal(0,batch.Advance(1,10,0).Evaluated);Assert.Empty(batch.Samples.ToArray());
        Assert.Equal(0,batch.ReadImpulses(handle).EvaluatedLastFrame);
        batch.Advance(2,20,0);
        var deferred=visibility==AnimationImpulseVisibility.DeferUntilVisible;
        Assert.Equal(deferred?1:0,batch.ReadImpulses(handle).Pending);
        Assert.Equal(deferred?0UL:1UL,batch.ReadImpulses(handle).Completed);
        batch.SetVisible(handle,true);batch.Advance(3,20,0);
        Assert.Equal(deferred?1:0,batch.Read(handle).Value);
        batch.Advance(4,20.5,0);batch.SetVisible(handle,false);batch.Advance(5,22,0);
        Assert.Equal(1UL,batch.ReadImpulses(handle).Completed);
        batch.SetVisible(handle,true);batch.Advance(6,22,0);Assert.Equal(0,batch.Read(handle).Value);
    }

    [Theory]
    [InlineData(AnimationClock.Presentation)]
    [InlineData(AnimationClock.Simulation)]
    public void ChosenClockOwnsEnvelopeAndInvalidFramesCannotConsumePending(AnimationClock clock)
    {
        var batch=new AnimationBatch(1);var handle=batch.RegisterImpulses(Binding,Definition(clock:clock),0,1);
        batch.EnqueueImpulse(handle,new(1),1,0);
        Assert.Throws<ArgumentOutOfRangeException>(()=>batch.Advance(1,double.NaN,0));
        Assert.Equal(1,batch.ReadImpulses(handle).Pending);
        batch.Advance(1,0,0);batch.Advance(2,.5,0);
        Assert.Equal(clock==AnimationClock.Presentation?.5:1,batch.Read(handle).Value);
        batch.Advance(3,.5,.5);Assert.Equal(.5,batch.Read(handle).Value);
        var before=batch.ReadImpulses(handle);
        Assert.Throws<ArgumentOutOfRangeException>(()=>batch.Advance(3,1,1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>batch.Advance(4,.25,.5));
        Assert.Equal(before,batch.ReadImpulses(handle));
        batch.Advance(4,1,1);Assert.Equal(0,batch.Read(handle).Value);
    }

    [Fact]
    public void BackpressureLeavesCommittedEventUnacknowledgedUntilAnimationAcceptsIt()
    {
        var events=new CommittedEventStream<Pulse>(new(5),Stamp(0),3);
        for(var revision=1;revision<=3;revision++)
        {
            events.Begin(Stamp(revision));events.Append(new(1));events.Seal();events.Commit();
        }
        var batch=new AnimationBatch(1);var handle=batch.RegisterImpulses(Binding,Definition(capacity:2,timing:AnimationImpulseTiming.EventTimeRetainFirstFrame),0,1);
        void Transfer()
        {
            while(events.PendingCount>0)
            {
                var occurrence=events.Peek();
                var admitted=batch.EnqueueImpulse(handle,new(occurrence.Id.Sequence.Value),occurrence.Payload.Strength,occurrence.Stamp.SimulationTime);
                if(admitted==AnimationImpulseAdmission.CapacityExhausted)return;
                Assert.Equal(AnimationImpulseAdmission.Accepted,admitted);events.Acknowledge(occurrence.Id);
            }
        }
        Transfer();var retained=events.Peek();Assert.Equal(1,events.PendingCount);
        Assert.Equal(2UL,batch.ReadImpulses(handle).Accepted);
        Transfer();Assert.Equal(retained,events.Peek());
        batch.Advance(1,100,0);Assert.Equal(1,batch.Read(handle).Value);
        batch.Advance(2,101,0);Transfer();Assert.Equal(0,events.PendingCount);
        batch.Advance(3,101,0);Assert.Equal(1,batch.Read(handle).Value);
        batch.Advance(4,102,0);Assert.Equal(3UL,batch.ReadImpulses(handle).Completed);
        Assert.Equal(3UL,batch.ReadImpulses(handle).Accepted);
    }

    [Fact]
    public void InvalidAdmissionAndDuplicateIdentitiesPreservePendingOutputAndCanRetryAfterCapacity()
    {
        var batch=new AnimationBatch(1);var handle=batch.RegisterImpulses(Binding,Definition(capacity:1),0,1);
        foreach(var value in new[]{double.NaN,double.PositiveInfinity,-.1,0,1.1})
            Assert.Throws<ArgumentOutOfRangeException>(()=>batch.EnqueueImpulse(handle,new(1),value,0));
        Assert.Throws<ArgumentException>(()=>batch.EnqueueImpulse(handle,default,1,0));
        Assert.Equal(AnimationImpulseAdmission.Accepted,batch.EnqueueImpulse(handle,new(1),1,0));
        Assert.Throws<ArgumentException>(()=>batch.EnqueueImpulse(handle,new(1),1,0));
        Assert.Equal(AnimationImpulseAdmission.CapacityExhausted,batch.EnqueueImpulse(handle,new(2),1,0));
        Assert.Equal(1UL,batch.ReadImpulses(handle).Accepted);
        batch.Advance(1,0,0);batch.Advance(2,1,0);
        Assert.Equal(AnimationImpulseAdmission.Accepted,batch.EnqueueImpulse(handle,new(2),1,0));
        Assert.Equal(2UL,batch.ReadImpulses(handle).Accepted);
        Assert.Throws<ArgumentException>(()=>batch.EnqueueImpulse(handle,new(1),1,0));
    }

    [Fact]
    public void CancellationRemovalAndResetHaveExplicitIdentityLifetimes()
    {
        var batch=new AnimationBatch(1);var handle=batch.RegisterImpulses(Binding,Definition(),0,1);
        batch.EnqueueImpulse(handle,new(1),1,0);batch.Advance(1,0,0);
        batch.EnqueueImpulse(handle,new(2),1,0);batch.CancelImpulses(handle);
        Assert.Equal(2UL,batch.ReadImpulses(handle).Cancelled);Assert.Equal(0UL,batch.ReadImpulses(handle).Completed);
        batch.Advance(2,0,0);Assert.Equal(0,batch.Read(handle).Value);
        Assert.Throws<ArgumentException>(()=>batch.EnqueueImpulse(handle,new(2),1,0));
        batch.EnqueueImpulse(handle,new(3),1,0);batch.Remove(handle);
        Assert.Throws<ArgumentException>(()=>batch.ReadImpulses(handle));
        var next=batch.RegisterImpulses(Binding,Definition(),0,1);batch.EnqueueImpulse(next,new(1),1,0);batch.Reset();
        Assert.Equal(0,batch.RegisteredCount);Assert.Equal(0,batch.ScheduledCount);
        Assert.Throws<ArgumentException>(()=>batch.CancelImpulses(next));
        Assert.Equal(0,Assert.Single(batch.Samples.ToArray()).Value);
        var fresh=batch.RegisterImpulses(Binding,Definition(),0,1);Assert.Equal(AnimationImpulseAdmission.Accepted,batch.EnqueueImpulse(fresh,new(1),1,0));
    }

    [Fact]
    public void UnsupportedDefinitionsPropertyRangesAndControlsReject()
    {
        foreach(var duration in new[]{0,-1,double.NaN,double.PositiveInfinity})
            Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationImpulseDefinition(duration,AnimationImpulseCurve.LinearDecay,
                AnimationImpulseOverlap.Maximum,AnimationImpulseVisibility.DeferUntilVisible,AnimationClock.Presentation,1,AnimationImpulseTiming.FirstPresentation,0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Definition(curve:(AnimationImpulseCurve)(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Definition(overlap:(AnimationImpulseOverlap)(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Definition(visibility:(AnimationImpulseVisibility)(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Definition(clock:(AnimationClock)(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Definition(timing:(AnimationImpulseTiming)(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Definition(capacity:0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Definition(capacity:AnimationImpulseDefinition.MaximumCapacity+1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationOccurrenceId(0));
        var batch=new AnimationBatch(2);
        Assert.Throws<ArgumentException>(()=>batch.RegisterImpulses(new(new(0),AnimationProperty.UniformScale),Definition(),0,1));
        var impulse=batch.RegisterImpulses(Binding,Definition(),0,1);
        Assert.Throws<InvalidOperationException>(()=>batch.RegisterImpulses(Binding,Definition(),0,1));
        var clipDefinition=new AnimationDefinition(0,1,1,AnimationCurve.Linear,AnimationRepeat.Once,AnimationClock.Presentation);
        Assert.Throws<InvalidOperationException>(()=>batch.Register(Binding,clipDefinition));
        Assert.Throws<InvalidOperationException>(()=>batch.RegisterFollow(Binding,new(0,1,0,18,AnimationClock.Presentation)));
        Assert.Throws<InvalidOperationException>(()=>batch.Start(impulse));
        Assert.Throws<InvalidOperationException>(()=>batch.DriveTo(impulse,AnimationEndpoint.To,0));
        Assert.Throws<InvalidOperationException>(()=>batch.SetLoopRate(impulse,1,0));
        Assert.Throws<InvalidOperationException>(()=>batch.FollowValue(impulse,1,0));
        Assert.Throws<InvalidOperationException>(()=>batch.Stop(impulse,AnimationStop.Hold,0));
        Assert.Throws<InvalidOperationException>(()=>batch.Stop(impulse,AnimationStop.RestoreInitial,0));
        var clip=batch.Register(new(new(1),AnimationProperty.ColourBlend),clipDefinition);
        Assert.Throws<InvalidOperationException>(()=>batch.EnqueueImpulse(clip,new(1),1,0));
        Assert.Throws<InvalidOperationException>(()=>batch.ReadImpulses(clip));
        Assert.Throws<InvalidOperationException>(()=>batch.CancelImpulses(clip));
        Assert.Throws<ArgumentException>(()=>new AnimationBatch(1).ReadImpulses(impulse));
    }

    [Theory]
    [InlineData(AnimationImpulseOverlap.Maximum)]
    [InlineData(AnimationImpulseOverlap.SaturatingSum)]
    public void SaturatedValuesKeepAllOccurrenceLifetimes(AnimationImpulseOverlap overlap)
    {
        var batch=new AnimationBatch(1);var handle=batch.RegisterImpulses(Binding,Definition(overlap:overlap),0,1);
        for(ulong id=1;id<=4;id++)batch.EnqueueImpulse(handle,new(id),.75,0);
        batch.Advance(1,0,0);Assert.Equal(overlap==AnimationImpulseOverlap.Maximum?.75:1,batch.Read(handle).Value);
        Assert.Equal(4,batch.ReadImpulses(handle).Playing);Assert.Equal(4,batch.ReadImpulses(handle).EvaluatedLastFrame);
        batch.Advance(2,.75,0);Assert.Equal(overlap==AnimationImpulseOverlap.Maximum?.1875:.75,batch.Read(handle).Value);
        batch.Advance(3,1,0);Assert.Equal(4UL,batch.ReadImpulses(handle).Completed);
    }

    [Theory]
    [InlineData(AnimationImpulseCurve.LinearDecay,.75)]
    [InlineData(AnimationImpulseCurve.SmoothDecay,.896484375)]
    public void DecayCurvesHaveDistinctQuarterTimeValues(AnimationImpulseCurve curve,double expected)
    {
        var batch=new AnimationBatch(1);var handle=batch.RegisterImpulses(Binding,Definition(curve:curve),0,1);
        batch.EnqueueImpulse(handle,new(1),1,0);batch.Advance(1,0,0);batch.Advance(2,.25,0);
        Assert.Equal(expected,batch.Read(handle).Value);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(64)]
    [InlineData(1024)]
    public void WarmedOccurrenceAdmissionAndFramesAllocateNothing(int capacity)
    {
        var batch=new AnimationBatch(1);var handle=batch.RegisterImpulses(Binding,Definition(capacity:capacity),0,1);
        ulong id=0,frame=0;double time=0;
        void Cycle()
        {
            for(var i=0;i<capacity;i++)batch.EnqueueImpulse(handle,new(++id),1,0);
            batch.Advance(++frame,time,0);time+=1;batch.Advance(++frame,time,0);
        }
        for(var i=0;i<100;i++)Cycle();
        var before=GC.GetAllocatedBytesForCurrentThread();
        for(var i=0;i<1000;i++)Cycle();
        var allocated=GC.GetAllocatedBytesForCurrentThread()-before;
        Assert.Equal(0,allocated);Assert.Equal(id,batch.ReadImpulses(handle).Completed);
        Console.WriteLine($"Animation impulses: capacity={capacity}, cycles=1000, allocated_bytes={allocated}");
    }

    [Theory]
    [InlineData(AnimationImpulseTiming.EventTime)]
    [InlineData(AnimationImpulseTiming.FirstPresentation)]
    [InlineData(AnimationImpulseTiming.EventTimeRetainFirstFrame)]
    public void TimingPoliciesDistinguishExpiredFeedbackFromFreshPresentation(AnimationImpulseTiming timing)
    {
        var batch=new AnimationBatch(1);var handle=batch.RegisterImpulses(Binding,Definition(timing:timing),0,1);
        batch.EnqueueImpulse(handle,new(1),1,1);
        batch.Advance(1,10,0);
        Assert.Equal(timing==AnimationImpulseTiming.EventTime?0:1,batch.Read(handle).Value);
        batch.Advance(2,10.5,0);
        Assert.Equal(timing==AnimationImpulseTiming.FirstPresentation?.5:0,batch.Read(handle).Value);
        batch.Advance(3,11,0);Assert.Equal(1UL,batch.ReadImpulses(handle).Completed);
    }

    [Theory]
    [InlineData(AnimationImpulseTiming.EventTime)]
    [InlineData(AnimationImpulseTiming.FirstPresentation)]
    [InlineData(AnimationImpulseTiming.EventTimeRetainFirstFrame)]
    public void FutureOccurrencesCannotStartEarlyAndPartialAgeUsesItsDeclaredTiming(AnimationImpulseTiming timing)
    {
        var batch=new AnimationBatch(1);var handle=batch.RegisterImpulses(Binding,Definition(timing:timing),0,1);
        batch.EnqueueImpulse(handle,new(1),1,5);
        batch.Advance(1,4,0);Assert.Equal(1,batch.ReadImpulses(handle).Pending);Assert.Equal(0,batch.Read(handle).Value);
        batch.Advance(2,5.25,0);Assert.Equal(timing==AnimationImpulseTiming.FirstPresentation?1:.75,batch.Read(handle).Value);
        batch.Advance(3,5.5,0);Assert.Equal(timing==AnimationImpulseTiming.FirstPresentation?.75:.5,batch.Read(handle).Value);
    }

    [Theory]
    [InlineData(AnimationImpulseVisibility.DeferUntilVisible)]
    [InlineData(AnimationImpulseVisibility.AdvanceWhileHidden)]
    public void ExpiredFirstFrameRetentionHonoursHiddenEligibility(AnimationImpulseVisibility visibility)
    {
        var batch=new AnimationBatch(1);var handle=batch.RegisterImpulses(Binding,
            Definition(visibility:visibility,timing:AnimationImpulseTiming.EventTimeRetainFirstFrame),0,1);
        batch.SetVisible(handle,false);batch.EnqueueImpulse(handle,new(1),1,0);
        batch.Advance(1,10,0);Assert.Empty(batch.Samples.ToArray());
        batch.SetVisible(handle,true);batch.Advance(2,10,0);
        Assert.Equal(visibility==AnimationImpulseVisibility.DeferUntilVisible?1:0,batch.Read(handle).Value);
        batch.Advance(3,10,0);Assert.Equal(0,batch.Read(handle).Value);
        Assert.Equal(1UL,batch.ReadImpulses(handle).Completed);
    }

    [Fact]
    public void InvalidOccurrenceTimesDoNotConsumeIdentityOrCapacity()
    {
        var batch=new AnimationBatch(1);var handle=batch.RegisterImpulses(Binding,Definition(capacity:1),0,1);
        foreach(var time in new[]{double.NaN,double.PositiveInfinity,double.NegativeInfinity})
            Assert.Throws<ArgumentOutOfRangeException>(()=>batch.EnqueueImpulse(handle,new(1),1,time));
        Assert.Equal(0UL,batch.ReadImpulses(handle).Accepted);
        Assert.Equal(AnimationImpulseAdmission.Accepted,batch.EnqueueImpulse(handle,new(1),1,0));
    }
}
