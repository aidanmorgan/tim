using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

public class MappedImpulseTests
{
    private static AnimationImpulseDefinition Definition()=>new(1,AnimationImpulseCurve.SineSquaredPulse,AnimationImpulseOverlap.SaturatingSum,
        AnimationImpulseVisibility.DeferUntilVisible,AnimationClock.Presentation,2,AnimationImpulseTiming.EventTimeRetainFirstFrame,.5);
    [Theory]
    [InlineData(AnimationProperty.UniformScale,1,1.24)]
    [InlineData(AnimationProperty.LocalTranslation,0,-.16)]
    [InlineData(AnimationProperty.ColourBlend,.2,.8)]
    public void MappingHasCorrectInitialPeakCompletionCancellationAndReset(AnimationProperty property,double from,double to)
    {
        var batch=new AnimationBatch(1);var binding=new AnimationBinding(new(0),property);
        var handle=batch.RegisterImpulses(binding,Definition(),from,to);Assert.Equal(from,batch.Read(handle).Value);
        batch.EnqueueImpulse(handle,new(1),1,0);batch.Advance(1,.25,0);
        Assert.InRange(Math.Abs(batch.Read(handle).Value-(from+(to-from)*.5)),0,1e-14);
        batch.Advance(2,.5,0);Assert.Equal(to,batch.Read(handle).Value);
        batch.Advance(3,1,0);Assert.Equal(from,batch.Read(handle).Value);
        batch.EnqueueImpulse(handle,new(2),1,1);batch.Advance(4,1.5,0);
        batch.CancelImpulses(handle);Assert.Equal(from,batch.Read(handle).Value);batch.Advance(5,1.5,0);
        Assert.Equal(from,Assert.Single(batch.Samples.ToArray()).Value);
        batch.EnqueueImpulse(handle,new(3),1,1.5);batch.Advance(6,2,0);batch.Reset();
        Assert.Equal(from,Assert.Single(batch.Samples.ToArray()).Value);
        Assert.Throws<ArgumentException>(()=>batch.Read(handle));
    }
    [Theory]
    [InlineData(double.NaN,1)]
    [InlineData(0,double.NaN)]
    [InlineData(double.PositiveInfinity,1)]
    [InlineData(0,double.NegativeInfinity)]
    [InlineData(-double.MaxValue,double.MaxValue)]
    public void NonRepresentableRangesRejectBeforeClaiming(double from,double to)
    {
        var batch=new AnimationBatch(1);var binding=new AnimationBinding(new(0),AnimationProperty.LocalTranslation);
        Assert.Throws<ArgumentOutOfRangeException>(()=>batch.RegisterImpulses(binding,Definition(),from,to));
        Assert.Equal(0,batch.RegisteredCount);
        var handle=batch.RegisterImpulses(binding,Definition(),0,-.16);Assert.Equal(1,batch.RegisteredCount);
        Assert.Equal(0,batch.Read(handle).Value);
    }
}
