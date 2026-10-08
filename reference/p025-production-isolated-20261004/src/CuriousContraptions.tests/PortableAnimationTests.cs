using System.Reflection;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

public sealed class PortableAnimationTests(ITestOutputHelper output)
{
    private static AnimationValue Value(AnimationProperty property,double value) => property switch
    {
        AnimationProperty.LocalRotationAngle => new(new AnimationRadians((Half)value)),
        AnimationProperty.LocalTranslation => new(new AnimationMetres((Half)value)),
        AnimationProperty.UniformScale => new(new AnimationScale((Half)value)),
        AnimationProperty.Opacity => new(new AnimationOpacity((Half)value)),
        AnimationProperty.ColourBlend => new(new AnimationColourBlend((Half)value)),
        AnimationProperty.ColourRed or AnimationProperty.ColourGreen or AnimationProperty.ColourBlue =>
            AnimationValue.Channel(property,new((Half)value)),
        _ => throw new ArgumentOutOfRangeException(nameof(property))
    };
    private static double Number(AnimationValue value) =>
        (double)BitConverter.UInt16BitsToHalf(value.CanonicalBits);
    private static void Equal(double expected,AnimationValue actual) =>
        Assert.Equal(BitConverter.HalfToUInt16Bits((Half)expected),actual.CanonicalBits);
    private static AnimationBinding Binding(ulong id=1,AnimationProperty property=AnimationProperty.Opacity) => new(new(id),property);
    private static AnimationDefinition Clip(AnimationProperty property=AnimationProperty.Opacity,
        AnimationCurve curve=AnimationCurve.Linear,AnimationRepeat repeat=AnimationRepeat.Once,
        AnimationClock clock=AnimationClock.Presentation,Half? duration=null) =>
        new(Value(property,.25),Value(property,.75),new(duration??(Half)1),curve,repeat,clock);
    private static AnimationImpulseDefinition Impulse(AnimationImpulseCurve curve=AnimationImpulseCurve.LinearDecay,
        AnimationImpulseOverlap overlap=AnimationImpulseOverlap.Maximum,
        AnimationImpulseVisibility visibility=AnimationImpulseVisibility.AdvanceWhileHidden,
        AnimationImpulseTiming timing=AnimationImpulseTiming.EventTime,int capacity=4) =>
        new(new((Half)1),curve,overlap,visibility,AnimationClock.Presentation,capacity,timing,
            new((Half)(curve is AnimationImpulseCurve.LinearDecay or AnimationImpulseCurve.SmoothDecay?0:.5)));
    private static AnimationOscillationDefinition Oscillator(Half? decay=null,Half? frequency=null,Half? velocity=null) =>
        new(new(decay??(Half)1),new(frequency??(Half)2),
            new AnimationVelocity(new AnimationRadiansPerSecond(velocity??(Half)2)),AnimationClock.Presentation);

    public static IEnumerable<object[]> ClipCases()
    {
        foreach(var property in Enum.GetValues<AnimationProperty>())
        foreach(var curve in Enum.GetValues<AnimationCurve>())
        foreach(var repeat in Enum.GetValues<AnimationRepeat>())
        foreach(var clock in Enum.GetValues<AnimationClock>())
            yield return new object[]{property,curve,repeat,clock};
    }
    [Theory,MemberData(nameof(ClipCases))]
    public void AllExistingPropertiesCurvesRepeatsAndClocksRetainMathematics(
        AnimationProperty property,AnimationCurve curve,AnimationRepeat repeat,AnimationClock clock)
    {
        var batch=new AnimationBatch(1);
        var handle=batch.Register(Binding(property:property),Clip(property,curve,repeat,clock));
        batch.Start(handle);batch.Advance(1,0,0);Equal(.25,batch.Read(handle).Value);
        var quarterAmount=curve switch
        {
            AnimationCurve.Linear=>.25,
            AnimationCurve.SmoothStep=>.103515625,
            AnimationCurve.SinePulse=>Math.Sqrt(.5),
            _=>throw new ArgumentOutOfRangeException(nameof(curve))
        };
        batch.Advance(2,clock==AnimationClock.Presentation?.25:0,clock==AnimationClock.Simulation?.25:0);
        Equal(.25+.5*quarterAmount,batch.Read(handle).Value);
        batch.Advance(3,1,1);
        Equal(repeat==AnimationRepeat.Loop||curve==AnimationCurve.SinePulse?.25:.75,batch.Read(handle).Value);
        batch.Advance(4,2,2);
        Equal(repeat==AnimationRepeat.Once&&curve!=AnimationCurve.SinePulse?.75:.25,batch.Read(handle).Value);
        Assert.Equal(repeat==AnimationRepeat.Once?AnimationPlayback.Completed:AnimationPlayback.Playing,batch.Read(handle).Playback);
    }

    [Fact]
    public void EndpointReversalRateHoldRestoreAndCycleLegRemainCanonical()
    {
        var batch=new AnimationBatch(2);
        var once=batch.Register(Binding(),Clip());batch.Start(once);
        batch.DriveTo(once,AnimationEndpoint.From,.75);
        batch.Advance(1,1,0);Equal(.5,batch.Read(once).Value);
        batch.Stop(once,AnimationStop.Hold,1.25);Equal(.375,batch.Read(once).Value);
        batch.DriveTo(once,AnimationEndpoint.To,1.25);
        batch.Advance(2,1.5,0);Equal(.5,batch.Read(once).Value);
        batch.Stop(once,AnimationStop.RestoreInitial,1.5);Equal(.25,batch.Read(once).Value);
        var loop=batch.Register(Binding(2),Clip(repeat:AnimationRepeat.Loop));batch.Start(loop);
        batch.SetLoopRate(loop,new((Half)(-1)),1.75);
        batch.Advance(3,2,0);Equal(.25,batch.Read(loop).Value);
        batch.SetLoopRate(loop,new((Half)0),2.25);
        batch.Advance(4,3,0);Equal(.625,batch.Read(loop).Value);
        batch.SetLoopRate(loop,new((Half)1),3);
        batch.Advance(5,3.25,0);Equal(.25,batch.Read(loop).Value);
        batch.Remove(loop);
        var ping=batch.Register(Binding(2),Clip(repeat:AnimationRepeat.PingPong));batch.Start(ping);
        batch.Stop(ping,AnimationStop.Hold,4.75);Equal(.5,batch.Read(ping).Value);
        batch.Advance(6,5,0);Equal(.5,batch.Read(ping).Value);
        batch.Start(ping);batch.Advance(7,5.25,0);Equal(.375,batch.Read(ping).Value);
    }

    [Theory]
    [InlineData(AnimationRepeat.Once)]
    [InlineData(AnimationRepeat.Loop)]
    [InlineData(AnimationRepeat.PingPong)]
    public void MaximumExternalClockAndMinimumCanonicalDurationRemainFinite(AnimationRepeat repeat)
    {
        foreach(var duration in new[]{Half.Epsilon,Half.MaxValue})
        {
            var batch=new AnimationBatch(1);var handle=batch.Register(Binding(),Clip(repeat:repeat,duration:duration));
            batch.Start(handle);batch.Advance(1,double.MaxValue,0);
            // Independent integer remainder of max-double: 53216 modulo65504,
            // 118720 modulo131008. A power-of-two subnormal duration divides it exactly.
            var phase=repeat==AnimationRepeat.Once?1:duration==Half.Epsilon?0:
                repeat==AnimationRepeat.Loop?53216.0/65504:1-(118720.0/65504-1);
            Equal(.25+.5*phase,batch.Read(handle).Value);
            batch.Stop(handle,AnimationStop.Hold,double.MaxValue);
            Assert.True(Half.IsFinite(BitConverter.UInt16BitsToHalf(batch.Read(handle).Value.CanonicalBits)));
        }
    }

    [Fact]
    public void LaterInvalidLoopProductCannotMutateEarlierSlotsClocksSamplesOrPendingOccurrences()
    {
        var batch=new AnimationBatch(3);
        var valid=batch.Register(Binding(),Clip());batch.Start(valid);
        var impulse=batch.RegisterImpulses(Binding(2),Impulse(),Value(AnimationProperty.Opacity,0),Value(AnimationProperty.Opacity,1));
        batch.EnqueueImpulse(impulse,new(1),new((Half)1),0);
        var loop=batch.Register(Binding(3),Clip(repeat:AnimationRepeat.Loop));batch.Start(loop);
        batch.SetLoopRate(loop,new(Half.MaxValue),0);
        batch.Advance(1,0,0);
        var reads=new[]{batch.Read(valid),batch.Read(impulse),batch.Read(loop)};
        var occurrences=batch.ReadImpulses(impulse);var samples=batch.Samples.ToArray();
        Assert.Throws<ArgumentOutOfRangeException>(()=>batch.Advance(2,double.MaxValue,0));
        Assert.Equal(reads,new[]{batch.Read(valid),batch.Read(impulse),batch.Read(loop)});
        Assert.Equal(occurrences,batch.ReadImpulses(impulse));Assert.Equal(samples,batch.Samples.ToArray());
        batch.Advance(2,.25,0);Equal(.375,batch.Read(valid).Value);
    }
    [Fact]
    public void LaterInvalidOccurrenceSubtractionCannotConsumeEarlierPlayback()
    {
        var batch=new AnimationBatch(2);var clip=batch.Register(Binding(),Clip());batch.Start(clip);
        var impulse=batch.RegisterImpulses(Binding(2),Impulse(),Value(AnimationProperty.Opacity,0),Value(AnimationProperty.Opacity,1));
        batch.EnqueueImpulse(impulse,new(1),new((Half)1),-double.MaxValue);
        var before=batch.Read(clip);var pending=batch.ReadImpulses(impulse);
        Assert.Throws<ArgumentOutOfRangeException>(()=>batch.Advance(1,double.MaxValue,0));
        Assert.Equal(before,batch.Read(clip));Assert.Equal(pending,batch.ReadImpulses(impulse));
        batch.Advance(1,0,0);
    }

    [Fact]
    public void FollowUsesCanonicalControlAnchorsAndIndependentClock()
    {
        var batch=new AnimationBatch(1);
        var handle=batch.RegisterFollow(Binding(),new(Value(AnimationProperty.Opacity,0),Value(AnimationProperty.Opacity,1),
            Value(AnimationProperty.Opacity,.125),new((Half)2),AnimationClock.Presentation));
        batch.FollowValue(handle,Value(AnimationProperty.Opacity,.875),0);
        batch.Advance(1,.5,999);Equal(.59912109375,batch.Read(handle).Value);
        batch.FollowValue(handle,Value(AnimationProperty.Opacity,.25),.5);
        batch.Advance(2,1,999);Equal(.37841796875,batch.Read(handle).Value);
        batch.Stop(handle,AnimationStop.RestoreInitial,1);Equal(.125,batch.Read(handle).Value);
    }

    public static IEnumerable<object[]> ImpulseCases()
    {
        foreach(var curve in Enum.GetValues<AnimationImpulseCurve>())
        foreach(var overlap in Enum.GetValues<AnimationImpulseOverlap>())
        foreach(var visibility in Enum.GetValues<AnimationImpulseVisibility>())
        foreach(var timing in Enum.GetValues<AnimationImpulseTiming>())
            yield return new object[]{curve,overlap,visibility,timing};
    }
    [Theory,MemberData(nameof(ImpulseCases))]
    public void EveryImpulseModePreservesOccurrenceLifetimeAndCanonicalOutput(AnimationImpulseCurve curve,
        AnimationImpulseOverlap overlap,AnimationImpulseVisibility visibility,AnimationImpulseTiming timing)
    {
        var batch=new AnimationBatch(1);
        var handle=batch.RegisterImpulses(Binding(),Impulse(curve,overlap,visibility,timing),
            Value(AnimationProperty.Opacity,0),Value(AnimationProperty.Opacity,1));
        batch.EnqueueImpulse(handle,new(1),new((Half).5),0);
        batch.EnqueueImpulse(handle,new(2),new((Half).25),0);
        batch.Advance(1,0,0);batch.Advance(2,.25,0);
        var envelope=curve switch
        {
            AnimationImpulseCurve.LinearDecay=>.75,
            AnimationImpulseCurve.SmoothDecay=>.896484375,
            AnimationImpulseCurve.SineSquaredPulse=>.5,
            AnimationImpulseCurve.SmoothRiseFall=>.5,
            _=>throw new ArgumentOutOfRangeException(nameof(curve))
        };
        var expected=overlap switch
        {
            AnimationImpulseOverlap.Maximum=>envelope*.5,
            AnimationImpulseOverlap.SaturatingSum=>envelope*.75,
            AnimationImpulseOverlap.HyperbolicTangentSum=>Math.Tanh(envelope*.75),
            _=>throw new ArgumentOutOfRangeException(nameof(overlap))
        };
        Equal(expected,batch.Read(handle).Value);
        batch.Advance(3,1,0);Equal(0,batch.Read(handle).Value);
        Assert.Equal(2UL,batch.ReadImpulses(handle).Completed);
        batch.EnqueueImpulse(handle,new(3),new((Half)1),1);
        batch.CancelImpulses(handle);
        Assert.Equal(1UL,batch.ReadImpulses(handle).Cancelled);
        Assert.Throws<ArgumentException>(()=>batch.EnqueueImpulse(handle,new(3),new((Half)1),1));
    }

    [Theory]
    [InlineData(AnimationImpulseVisibility.DeferUntilVisible)]
    [InlineData(AnimationImpulseVisibility.AdvanceWhileHidden)]
    public void HiddenImpulseVisibilityAndFirstFrameRetentionRemainDistinct(AnimationImpulseVisibility visibility)
    {
        var batch=new AnimationBatch(1);var handle=batch.RegisterImpulses(Binding(),
            Impulse(visibility:visibility,timing:AnimationImpulseTiming.EventTimeRetainFirstFrame),
            Value(AnimationProperty.Opacity,0),Value(AnimationProperty.Opacity,1));
        batch.SetVisible(handle,false);batch.EnqueueImpulse(handle,new(1),new((Half).5),0);
        var hidden=batch.Advance(1,2,0);Assert.Equal(0,hidden.Evaluated);Assert.Empty(batch.Samples.ToArray());
        batch.SetVisible(handle,true);batch.Advance(2,2,0);
        Equal(visibility==AnimationImpulseVisibility.DeferUntilVisible?.5:0,batch.Read(handle).Value);
        batch.Advance(3,3,0);Equal(0,batch.Read(handle).Value);
    }

    [Fact]
    public void OscillationGoldenSampleAndRoundedZeroCrossingDoNotRetireFutureMotion()
    {
        var binding=Binding(property:AnimationProperty.LocalRotationAngle);
        var batch=new AnimationBatch(1);var handle=batch.RegisterOscillation(binding,Oscillator());
        batch.KickOscillation(handle,new(1),new((Half)1),0);batch.Advance(1,.25,0);
        var read=batch.ReadOscillation(handle);Equal(.373291015625,read.Position);
        Assert.Equal((Half).99365234375,read.Velocity.RadiansPerSecond.Value);
        batch.Remove(handle);
        var slow=(Half)1e-6;
        handle=batch.RegisterOscillation(binding,Oscillator(slow,slow,Half.Epsilon));
        batch.KickOscillation(handle,new(1),new((Half)1),.25);
        var crossing=.25+Math.PI/(double)slow;
        batch.Advance(2,crossing,0);
        Assert.Equal(AnimationPlayback.Playing,batch.Read(handle).Playback);
        batch.Advance(3,.25+1.5*Math.PI/(double)slow,0);
        Equal(-.0005283355712890625,batch.Read(handle).Value);
    }

    [Fact]
    public void OscillationAdmissionChecksRoundedAnchorsBeforeChangingIdentity()
    {
        var batch=new AnimationBatch(1);
        var handle=batch.RegisterOscillation(Binding(property:AnimationProperty.LocalRotationAngle),
            Oscillator((Half)1,(Half)1,Half.MaxValue));
        var before=batch.ReadOscillation(handle);
        Assert.Throws<ArgumentOutOfRangeException>(()=>batch.KickOscillation(handle,new(1),new((Half)1),0));
        Assert.Equal(before,batch.ReadOscillation(handle));
        batch.KickOscillation(handle,new(1),new((Half).25),0);
        Assert.Equal(1UL,batch.ReadOscillation(handle).Accepted);
        batch.Stop(handle,AnimationStop.RestoreInitial,0);Equal(0,batch.Read(handle).Value);
    }

    [Fact]
    public void WriterCapacityUnitsStaleHandlesAndGenerationRemainExact()
    {
        var batch=new AnimationBatch(1);var handle=batch.Register(Binding(),Clip());
        Assert.Throws<InvalidOperationException>(()=>batch.Register(Binding(),Clip()));
        Assert.Throws<InvalidOperationException>(()=>batch.Register(Binding(2),Clip()));
        Assert.Throws<ArgumentException>(()=>new AnimationBatch(1).Read(handle));
        batch.Remove(handle);Assert.Throws<ArgumentException>(()=>batch.Read(handle));
        var next=batch.Register(Binding(),Clip());Assert.NotEqual(handle.Version,next.Version);
        batch.Reset();Assert.Throws<ArgumentException>(()=>batch.Read(next));Assert.Equal(2UL,batch.Generation.Value);
        Assert.Single(batch.Samples.ToArray());Equal(.25,batch.Samples[0].Value);
        Assert.Throws<ArgumentException>(()=>batch.Register(Binding(),Clip(AnimationProperty.LocalTranslation)));
        Assert.Throws<ArgumentException>(()=>batch.Register(default,Clip()));
        Assert.Equal(0,batch.RegisteredCount);
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationDefinition(default,default,new((Half)1),
            AnimationCurve.Linear,AnimationRepeat.Once,AnimationClock.Presentation));
    }

    [Fact]
    public void HalfBoundaryProfileRejectsUndefinedValuesWithoutClamping()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationDurationSeconds(Half.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationDurationSeconds((Half)0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationRadians(Half.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationOpacity((Half)1.01));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationFrequencyRadiansPerSecond(Half.Epsilon));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationImpulseDefinition(new((Half)1),
            AnimationImpulseCurve.SmoothRiseFall,AnimationImpulseOverlap.Maximum,
            AnimationImpulseVisibility.AdvanceWhileHidden,AnimationClock.Presentation,1,
            AnimationImpulseTiming.EventTime,new((Half)(1-1e-6))));
        var positive=new AnimationValue(new AnimationScale(Half.Epsilon));
        var batch=new AnimationBatch(1);
        batch.Register(Binding(property:AnimationProperty.UniformScale),new(positive,positive,new(Half.MaxValue),
            AnimationCurve.Linear,AnimationRepeat.PingPong,AnimationClock.Presentation));
        Assert.Throws<InvalidOperationException>(()=>positive.Metres);
    }

    [Fact]
    public void PortableAssemblyHasNoSceneDependencyAndWarmFramesAllocateNoScratch()
    {
        var assembly=typeof(AnimationBatch).Assembly;
        Assert.Equal("CuriousContraptions.Animation",assembly.GetName().Name);
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(),r=>
            r.Name!.Contains("Godot",StringComparison.OrdinalIgnoreCase)||
            r.Name.Contains("2dog",StringComparison.OrdinalIgnoreCase));
        var batch=new AnimationBatch(1);var handle=batch.Register(Binding(),Clip(repeat:AnimationRepeat.Loop));
        batch.Start(handle);
        for(ulong i=1;i<=1000;i++)batch.Advance(i,i/60.0,0);
        var before=GC.GetAllocatedBytesForCurrentThread();
        for(ulong i=1001;i<=3000;i++)batch.Advance(i,i/60.0,0);
        Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread()-before);
        batch.Stop(handle,AnimationStop.Hold,50);
        batch.Advance(3001,50,0);
        var inactive=batch.Advance(3002,51,0);
        Assert.Equal(0,inactive.Evaluated);Assert.Equal(0,inactive.DirtyWrites);Assert.Equal(0,inactive.Scheduled);
    }
    [Fact]
    public void SubHalfImpulseContributionCannotConsumeRetainedFirstFrame()
    {
        var batch=new AnimationBatch(1);var handle=batch.RegisterImpulses(Binding(),
            Impulse(AnimationImpulseCurve.SineSquaredPulse,timing:AnimationImpulseTiming.EventTimeRetainFirstFrame),
            Value(AnimationProperty.Opacity,0),Value(AnimationProperty.Opacity,1));
        batch.EnqueueImpulse(handle,new(1),new((Half)1),0);
        batch.Advance(1,1e-5,0);Equal(0,batch.Read(handle).Value);
        batch.Advance(2,2,0);Equal(1,batch.Read(handle).Value);
        Assert.Equal(1,batch.ReadImpulses(handle).Playing);
        batch.Advance(3,3,0);Equal(0,batch.Read(handle).Value);
        Assert.Equal(1UL,batch.ReadImpulses(handle).Completed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PositiveCanonicalImpulseIsLogicalObservationEvenWithNoMappedChange(bool flat)
    {
        var batch=new AnimationBatch(1);var handle=batch.RegisterImpulses(Binding(property:AnimationProperty.UniformScale),
            Impulse(timing:AnimationImpulseTiming.EventTimeRetainFirstFrame),
            Value(AnimationProperty.UniformScale,1),Value(AnimationProperty.UniformScale,flat?1:1.0009765625));
        batch.EnqueueImpulse(handle,new(1),new(Half.Epsilon),0);
        batch.Advance(1,0,0);Equal(1,batch.Read(handle).Value);
        batch.Advance(2,2,0);
        Assert.Equal(1UL,batch.ReadImpulses(handle).Completed);
        Assert.Equal(0,batch.ReadImpulses(handle).Playing);
    }

    [Fact]
    public void MaximumMaskedPositiveCanonicalOccurrenceStillCompletes()
    {
        var batch=new AnimationBatch(1);var handle=batch.RegisterImpulses(Binding(),
            Impulse(timing:AnimationImpulseTiming.EventTimeRetainFirstFrame),
            Value(AnimationProperty.Opacity,0),Value(AnimationProperty.Opacity,1));
        batch.EnqueueImpulse(handle,new(1),new((Half)1),0);
        batch.EnqueueImpulse(handle,new(2),new(Half.Epsilon),0);
        batch.Advance(1,0,0);Equal(1,batch.Read(handle).Value);
        batch.Advance(2,2,0);Equal(0,batch.Read(handle).Value);
        Assert.Equal(2UL,batch.ReadImpulses(handle).Completed);
    }

    [Fact]
    public void UndefinedEnumsAndDefaultInvalidQuantitiesRejectBeforeRegistration()
    {
        var from=Value(AnimationProperty.Opacity,0);var to=Value(AnimationProperty.Opacity,1);
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationDefinition(from,to,default,
            AnimationCurve.Linear,AnimationRepeat.Once,AnimationClock.Presentation));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationDefinition(from,to,new((Half)1),
            (AnimationCurve)255,AnimationRepeat.Once,AnimationClock.Presentation));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationDefinition(from,to,new((Half)1),
            AnimationCurve.Linear,(AnimationRepeat)255,AnimationClock.Presentation));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationDefinition(from,to,new((Half)1),
            AnimationCurve.Linear,AnimationRepeat.Once,(AnimationClock)255));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationFollowDefinition(from,to,from,default,AnimationClock.Presentation));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationOscillationDefinition(default,new((Half)1),
            new AnimationVelocity(new AnimationRadiansPerSecond((Half)1)),AnimationClock.Presentation));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationOscillationDefinition(new((Half)1),default,
            new AnimationVelocity(new AnimationRadiansPerSecond((Half)1)),AnimationClock.Presentation));
        Assert.Throws<ArgumentException>(()=>new AnimationOscillationDefinition(new((Half)1),new((Half)1),default,AnimationClock.Presentation));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Impulse((AnimationImpulseCurve)255));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Impulse(overlap:(AnimationImpulseOverlap)255));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Impulse(visibility:(AnimationImpulseVisibility)255));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Impulse(timing:(AnimationImpulseTiming)255));
        var batch=new AnimationBatch(1);var handle=batch.Register(Binding(),Clip());batch.Start(handle);
        var before=batch.Read(handle);
        Assert.Throws<ArgumentOutOfRangeException>(()=>batch.DriveTo(handle,(AnimationEndpoint)255,0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>batch.Stop(handle,(AnimationStop)255,0));
        Assert.Throws<ArgumentException>(()=>batch.Read(default));
        foreach(var time in new[]{-1.0,double.NaN,double.PositiveInfinity})
            Assert.Throws<ArgumentOutOfRangeException>(()=>batch.Advance(1,time,0));
        Assert.Equal(before,batch.Read(handle));
    }

    [Fact]
    public void OpaqueUlongTargetsAndDistinctPropertyWritersCannotAlias()
    {
        var batch=new AnimationBatch(4);
        var ids=new[]{1UL<<53,(1UL<<53)+1,ulong.MaxValue};
        var handles=ids.Select(id=>batch.Register(Binding(id),Clip())).ToArray();
        var channel=batch.Register(Binding(ids[0],AnimationProperty.ColourRed),Clip(AnimationProperty.ColourRed));
        Assert.Equal(4,batch.RegisteredCount);
        for(var i=0;i<ids.Length;i++)Assert.Equal(ids[i],batch.Binding(handles[i]).Target.Value);
        Assert.Equal(AnimationProperty.ColourRed,batch.Binding(channel).Property);
        batch.Remove(handles[1]);
        Assert.Throws<InvalidOperationException>(()=>batch.Register(Binding(ids[0]),Clip()));
        Assert.Equal(3,batch.RegisteredCount);
    }

    [Fact]
    public void PhaseNeighborsRetainTurnaroundAndRoundedPeriodPolicies()
    {
        var belowOne=(double)BitConverter.UInt16BitsToHalf(0x3bff);
        var aboveOne=(double)BitConverter.UInt16BitsToHalf(0x3c01);
        var belowTwo=(double)BitConverter.UInt16BitsToHalf(0x3fff);
        foreach(var phase in new[]{0.0,belowOne,1,aboveOne,belowTwo,2.0})
        {
            var batch=new AnimationBatch(1);var handle=batch.Register(Binding(),Clip(repeat:AnimationRepeat.PingPong));
            batch.Start(handle);batch.Stop(handle,AnimationStop.Hold,phase);
            Equal(.25+.5*(phase<=1?phase:2-phase),batch.Read(handle).Value);
        }
        var looping=new AnimationBatch(1);var loop=looping.Register(Binding(),Clip(repeat:AnimationRepeat.Loop));
        looping.Start(loop);
        looping.SetLoopRate(loop,new((Half)0),.9999); // Stored Half phase rounds to period, explicitly normalizes0.
        Equal(.25,looping.Read(loop).Value);
        looping.SetLoopRate(loop,new((Half)(-1)),.9999);
        looping.Advance(1,1.2499,0);Equal(.625,looping.Read(loop).Value);
    }

    [Fact]
    public void HiddenFollowAndOscillatorMatchSkippedCadenceAndRetireOnlyTrueCompletion()
    {
        var visible=new AnimationBatch(2);var hidden=new AnimationBatch(2);
        var follow=new AnimationFollowDefinition(Value(AnimationProperty.Opacity,0),Value(AnimationProperty.Opacity,1),
            Value(AnimationProperty.Opacity,0),new((Half)2),AnimationClock.Presentation);
        var vf=visible.RegisterFollow(Binding(),follow);var hf=hidden.RegisterFollow(Binding(),follow);
        var vo=visible.RegisterOscillation(Binding(2,AnimationProperty.LocalRotationAngle),Oscillator());
        var ho=hidden.RegisterOscillation(Binding(2,AnimationProperty.LocalRotationAngle),Oscillator());
        visible.FollowValue(vf,Value(AnimationProperty.Opacity,1),0);hidden.FollowValue(hf,Value(AnimationProperty.Opacity,1),0);
        visible.KickOscillation(vo,new(1),new((Half)1),0);hidden.KickOscillation(ho,new(1),new((Half)1),0);
        hidden.SetVisible(hf,false);hidden.SetVisible(ho,false);
        for(ulong frame=1;frame<=30;frame++)
        {
            visible.Advance(frame,frame/60.0,0);
            var work=hidden.Advance(frame,frame/60.0,0);Assert.Equal(0,work.Evaluated);Assert.Equal(0,work.DirtyWrites);
        }
        hidden.SetVisible(hf,true);hidden.SetVisible(ho,true);
        visible.Advance(31,1,0);hidden.Advance(31,1,0);
        Assert.Equal(visible.Read(vf),hidden.Read(hf));Assert.Equal(visible.Read(vo),hidden.Read(ho));
        visible.Advance(32,1000,0);
        Assert.Equal(AnimationPlayback.Completed,visible.Read(vf).Playback);
        Assert.Equal(AnimationPlayback.Completed,visible.Read(vo).Playback);
        Assert.Equal(0,visible.Advance(33,1001,0).Scheduled);
    }

    [Fact]
    public void OscillatorZeroCrossingFrameDoesNotChangeLaterSample()
    {
        var a=new AnimationBatch(1);var b=new AnimationBatch(1);var slow=(Half)1e-6;
        var ha=a.RegisterOscillation(Binding(property:AnimationProperty.LocalRotationAngle),Oscillator(slow,slow,Half.Epsilon));
        var hb=b.RegisterOscillation(Binding(property:AnimationProperty.LocalRotationAngle),Oscillator(slow,slow,Half.Epsilon));
        a.KickOscillation(ha,new(1),new((Half)1),0);b.KickOscillation(hb,new(1),new((Half)1),0);
        a.Advance(1,Math.PI/(double)slow,0);
        a.Advance(2,1.5*Math.PI/(double)slow,0);b.Advance(1,1.5*Math.PI/(double)slow,0);
        Assert.Equal(a.Read(ha),b.Read(hb));
        a.Advance(3,1000/(double)slow,0);Assert.Equal(AnimationPlayback.Completed,a.Read(ha).Playback);
        Assert.Equal(0,a.Advance(4,1001/(double)slow,0).Scheduled);
    }

    [Fact]
    public void ExactCounterExhaustionRejectsBeforeChangingOwnerState()
    {
        var batch=new AnimationBatch(1);
        typeof(AnimationBatch).GetProperty(nameof(AnimationBatch.Generation))!.SetValue(batch,new AnimationGeneration(ulong.MaxValue));
        var handle=batch.Register(Binding(),Clip());var before=batch.Read(handle);
        Assert.Throws<OverflowException>(()=>batch.Reset());Assert.Equal(before,batch.Read(handle));Assert.Equal(1,batch.RegisteredCount);
        batch.Remove(handle);
        var versions=(ulong[])typeof(AnimationBatch).GetField("_versions",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(batch)!;
        versions[handle.Slot]=ulong.MaxValue;
        Assert.Throws<OverflowException>(()=>batch.Register(Binding(),Clip()));Assert.Equal(0,batch.RegisteredCount);

        var impulses=new AnimationBatch(1);var ih=impulses.RegisterImpulses(Binding(),Impulse(),Value(AnimationProperty.Opacity,0),Value(AnimationProperty.Opacity,1));
        var slots=(Array)typeof(AnimationBatch).GetField("_slots",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(impulses)!;
        var slot=slots.GetValue(ih.Slot)!;var state=slot.GetType().GetField("Impulses")!.GetValue(slot)!;
        state.GetType().GetField("_accepted",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(state,ulong.MaxValue);
        var prior=impulses.ReadImpulses(ih);
        Assert.Throws<InvalidOperationException>(()=>impulses.EnqueueImpulse(ih,new(1),new((Half)1),0));
        Assert.Equal(prior,impulses.ReadImpulses(ih));

        var oscillation=new AnimationBatch(1);var oh=oscillation.RegisterOscillation(Binding(property:AnimationProperty.LocalRotationAngle),Oscillator());
        slots=(Array)typeof(AnimationBatch).GetField("_slots",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(oscillation)!;
        slot=slots.GetValue(oh.Slot)!;state=slot.GetType().GetField("Oscillation")!.GetValue(slot)!;
        state.GetType().GetField("_accepted",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(state,ulong.MaxValue);
        var priorOscillation=oscillation.ReadOscillation(oh);
        Assert.Throws<InvalidOperationException>(()=>oscillation.KickOscillation(oh,new(1),new((Half)1),0));
        Assert.Equal(priorOscillation,oscillation.ReadOscillation(oh));
    }

    [Fact]
    public void WarmedFollowImpulseAndOscillationControlsAllocateNoScratch()
    {
        var batch=new AnimationBatch(3);
        var follow=batch.RegisterFollow(Binding(),new(Value(AnimationProperty.Opacity,0),Value(AnimationProperty.Opacity,1),
            Value(AnimationProperty.Opacity,0),new((Half)2),AnimationClock.Presentation));
        var impulse=batch.RegisterImpulses(Binding(2),Impulse(),Value(AnimationProperty.Opacity,0),Value(AnimationProperty.Opacity,1));
        var oscillation=batch.RegisterOscillation(Binding(3,AnimationProperty.LocalRotationAngle),Oscillator());
        void Step(ulong frame)
        {
            var time=frame/60.0;
            batch.FollowValue(follow,Value(AnimationProperty.Opacity,frame%2),time);
            batch.EnqueueImpulse(impulse,new(frame),new((Half).5),time);
            batch.KickOscillation(oscillation,new(frame),new((Half)(frame%2==0?.001:-.001)),time);
            batch.Advance(frame,time,0);batch.CancelImpulses(impulse);
        }
        for(ulong frame=1;frame<=1000;frame++)Step(frame);
        var before=GC.GetAllocatedBytesForCurrentThread();
        for(ulong frame=1001;frame<=3000;frame++)Step(frame);
        Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread()-before);
    }

    private enum NativeWorkMode { Active, Hidden, Inactive }

    [Fact]
    public void RetainMeasuredActiveHiddenAndInactiveNativeCost()
    {
        const ulong warmup=10000;
        const ulong iterations=100000;
        foreach(var mode in Enum.GetValues<NativeWorkMode>())
        {
            var batch=new AnimationBatch(1);
            var handle=batch.Register(Binding(),Clip(curve:AnimationCurve.SinePulse,repeat:AnimationRepeat.Loop));
            batch.Start(handle);
            if(mode==NativeWorkMode.Hidden)batch.SetVisible(handle,false);
            if(mode==NativeWorkMode.Inactive)batch.Stop(handle,AnimationStop.Hold,0);
            for(ulong frame=1;frame<=warmup;frame++)batch.Advance(frame,frame/60.0,0);
            _=System.Diagnostics.Stopwatch.GetTimestamp();
            var allocated=GC.GetAllocatedBytesForCurrentThread();
            var started=System.Diagnostics.Stopwatch.GetTimestamp();
            AnimationFrameWork final=default;
            for(ulong frame=warmup+1;frame<=warmup+iterations;frame++)
                final=batch.Advance(frame,frame/60.0,0);
            var elapsed=System.Diagnostics.Stopwatch.GetTimestamp()-started;
            var bytes=GC.GetAllocatedBytesForCurrentThread()-allocated;
            output.WriteLine($"ANIMATION_NATIVE_COST mode={mode} registered=1 warmup={warmup} iterations={iterations} elapsedTicks={elapsed} frequency={System.Diagnostics.Stopwatch.Frequency} allocatedBytes={bytes} finalEvaluated={final.Evaluated} finalScheduled={final.Scheduled}");
            Assert.Equal(0,bytes);
            Assert.True(elapsed>0);
            Assert.Equal(mode==NativeWorkMode.Active?1:0,final.Evaluated);
            Assert.Equal(mode==NativeWorkMode.Inactive?0:1,final.Scheduled);
        }
    }

}
