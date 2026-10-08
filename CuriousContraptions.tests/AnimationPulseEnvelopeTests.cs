using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

public class AnimationPulseEnvelopeTests
{
    private static AnimationBinding Binding=>new(new(0),AnimationProperty.ColourBlend);
    private static AnimationImpulseDefinition Definition(AnimationImpulseCurve curve,AnimationImpulseOverlap overlap=AnimationImpulseOverlap.SaturatingSum,
        int capacity=4)=>new(1,curve,overlap,AnimationImpulseVisibility.DeferUntilVisible,AnimationClock.Presentation,
            capacity,AnimationImpulseTiming.EventTimeRetainFirstFrame,curve==AnimationImpulseCurve.SineSquaredPulse?.5:.08/.53);
    [Theory]
    [InlineData(0,0)]
    [InlineData(.25,.5)]
    [InlineData(.5,1)]
    [InlineData(.75,.5)]
    [InlineData(1,0)]
    public void SymmetricPulseMatchesBumperEnvelope(double phase,double expected)
    {
        Assert.InRange(Math.Abs(Definition(AnimationImpulseCurve.SineSquaredPulse).Envelope(phase)-expected),0,1e-14);
    }
    [Theory]
    [InlineData(0,0)]
    [InlineData(.04,.5)]
    [InlineData(.08,1)]
    [InlineData(.305,.5)]
    [InlineData(.53,0)]
    public void AsymmetricPulseMatchesCannonCompressionAndReturn(double seconds,double expected)
    {
        Assert.InRange(Math.Abs(Definition(AnimationImpulseCurve.SmoothRiseFall).Envelope(seconds/.53)-expected),0,1e-14);
    }
    [Theory]
    [InlineData(AnimationImpulseCurve.SineSquaredPulse,AnimationImpulseOverlap.SaturatingSum)]
    [InlineData(AnimationImpulseCurve.SineSquaredPulse,AnimationImpulseOverlap.HyperbolicTangentSum)]
    [InlineData(AnimationImpulseCurve.SmoothRiseFall,AnimationImpulseOverlap.SaturatingSum)]
    [InlineData(AnimationImpulseCurve.SmoothRiseFall,AnimationImpulseOverlap.HyperbolicTangentSum)]
    public void OverlapCombinesOccurrencesBeforeNonlinearResponse(AnimationImpulseCurve curve,AnimationImpulseOverlap overlap)
    {
        var d=Definition(curve,overlap);var batch=new AnimationBatch(1);var handle=batch.RegisterImpulses(Binding,d,0,1);
        batch.EnqueueImpulse(handle,new(1),.6,0);batch.EnqueueImpulse(handle,new(2),.6,0);
        batch.Advance(1,d.PeakPhase,0);
        var expected=overlap==AnimationImpulseOverlap.SaturatingSum?1:Math.Tanh(1.2);
        Assert.InRange(Math.Abs(batch.Read(handle).Value-expected),0,1e-14);
        Assert.Equal(2UL,batch.ReadImpulses(handle).Accepted);Assert.Equal(2,batch.ReadImpulses(handle).Playing);
        batch.Advance(2,1,0);Assert.Equal(0,batch.Read(handle).Value);Assert.Equal(2UL,batch.ReadImpulses(handle).Completed);
    }
    [Theory]
    [InlineData(AnimationImpulseCurve.SineSquaredPulse)]
    [InlineData(AnimationImpulseCurve.SmoothRiseFall)]
    public void ZeroStartDoesNotConsumeBriefFeedbackAndHiddenExpiryRetainsOnePeak(AnimationImpulseCurve curve)
    {
        var batch=new AnimationBatch(1);var handle=batch.RegisterImpulses(Binding,Definition(curve),0,1);
        batch.EnqueueImpulse(handle,new(1),.7,0);batch.Advance(1,0,0);Assert.Equal(0,batch.Read(handle).Value);
        batch.SetVisible(handle,false);batch.Advance(2,2,0);Assert.Equal(0UL,batch.ReadImpulses(handle).Completed);
        batch.SetVisible(handle,true);batch.Advance(3,2,0);Assert.Equal(.7,batch.Read(handle).Value);
        batch.Advance(4,2,0);Assert.Equal(0,batch.Read(handle).Value);Assert.Equal(1UL,batch.ReadImpulses(handle).Completed);
    }
    [Theory]
    [InlineData(AnimationImpulseCurve.LinearDecay,.5)]
    [InlineData(AnimationImpulseCurve.SmoothDecay,.5)]
    [InlineData(AnimationImpulseCurve.SineSquaredPulse,0)]
    [InlineData(AnimationImpulseCurve.SineSquaredPulse,.25)]
    [InlineData(AnimationImpulseCurve.SmoothRiseFall,0)]
    [InlineData(AnimationImpulseCurve.SmoothRiseFall,1)]
    [InlineData(AnimationImpulseCurve.SmoothRiseFall,double.NaN)]
    [InlineData(AnimationImpulseCurve.SmoothRiseFall,double.PositiveInfinity)]
    public void UnsupportedPeakPhasesReject(AnimationImpulseCurve curve,double peak)
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationImpulseDefinition(1,curve,AnimationImpulseOverlap.Maximum,
            AnimationImpulseVisibility.DeferUntilVisible,AnimationClock.Presentation,4,AnimationImpulseTiming.EventTime,peak));
    }
    [Theory]
    [InlineData(AnimationImpulseCurve.SineSquaredPulse)]
    [InlineData(AnimationImpulseCurve.SmoothRiseFall)]
    public void CadenceCancellationCapacityAndResetRetainExistingContracts(AnimationImpulseCurve curve)
    {
        var sparse=new AnimationBatch(1);var dense=new AnimationBatch(1);var d=Definition(curve,capacity:1);
        var a=sparse.RegisterImpulses(Binding,d,0,1);var b=dense.RegisterImpulses(Binding,d,0,1);
        sparse.EnqueueImpulse(a,new(1),1,0);dense.EnqueueImpulse(b,new(1),1,0);
        Assert.Equal(AnimationImpulseAdmission.CapacityExhausted,sparse.EnqueueImpulse(a,new(2),1,0));
        for(var frame=1;frame<=50;frame++)dense.Advance((ulong)frame,frame*.01,0);
        sparse.Advance(1,.5,0);Assert.Equal(dense.Read(b).Value,sparse.Read(a).Value);
        sparse.CancelImpulses(a);Assert.Equal(1UL,sparse.ReadImpulses(a).Cancelled);
        Assert.Equal(AnimationImpulseAdmission.Accepted,sparse.EnqueueImpulse(a,new(2),1,.5));
        sparse.Reset();Assert.Throws<ArgumentException>(()=>sparse.Read(a));
    }
    [Theory]
    [InlineData(1)]
    [InlineData(64)]
    [InlineData(256)]
    public void WarmedRiseFallAdmissionAndFramesAllocateNothing(int capacity)
    {
        var batch=new AnimationBatch(1);var handle=batch.RegisterImpulses(Binding,Definition(AnimationImpulseCurve.SmoothRiseFall,
            AnimationImpulseOverlap.HyperbolicTangentSum,capacity),0,1);
        ulong id=0,frame=0;double time=0;
        void Cycle()
        {
            for(var i=0;i<capacity;i++)batch.EnqueueImpulse(handle,new(++id),1,time);
            batch.Advance(++frame,time+.08/.53,0);time+=1;batch.Advance(++frame,time,0);
        }
        for(var i=0;i<100;i++)Cycle();
        var before=GC.GetAllocatedBytesForCurrentThread();
        for(var i=0;i<1000;i++)Cycle();
        Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread()-before);
        Assert.Equal(id,batch.ReadImpulses(handle).Completed);
    }
}
