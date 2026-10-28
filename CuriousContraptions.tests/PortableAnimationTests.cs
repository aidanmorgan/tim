using CuriousContraptions.Abstractions;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

public sealed class PortableAnimationTests(ITestOutputHelper output)
{
    private static AnimationValue Value(AnimationProperty property,double value) => property switch
    {
        AnimationProperty.LocalRotationAngle => new(new AnimationRadians((float)value)),
        AnimationProperty.LocalTranslation => new(new AnimationMetres((float)value)),
        AnimationProperty.UniformScale => new(new AnimationScale((float)value)),
        AnimationProperty.Opacity => new(new AnimationOpacity((float)value)),
        AnimationProperty.ColourBlend => new(new AnimationColourBlend((float)value)),
        AnimationProperty.ColourRed or AnimationProperty.ColourGreen or AnimationProperty.ColourBlue =>
            AnimationValue.Channel(property,new((float)value)),
        _ => throw new ArgumentOutOfRangeException(nameof(property))
    };
    private static void Equal(double expected,AnimationValue actual) =>
        Assert.Equal((float)expected,BitConverter.UInt32BitsToSingle(actual.CanonicalBits));
    private static AnimationBinding Binding(ulong id=1,AnimationProperty property=AnimationProperty.Opacity) => new(new(id),property);
    private static AnimationDefinition Clip(AnimationProperty property=AnimationProperty.Opacity,
        AnimationCurve curve=AnimationCurve.Linear,AnimationRepeat repeat=AnimationRepeat.Once,
        AnimationClock clock=AnimationClock.Presentation,float? duration=null) =>
        new(Value(property,.25),Value(property,.75),new(duration??1f),curve,repeat,clock);
    private static AnimationImpulseDefinition Impulse(AnimationImpulseCurve curve=AnimationImpulseCurve.LinearDecay,
        AnimationImpulseOverlap overlap=AnimationImpulseOverlap.Maximum,
        AnimationImpulseVisibility visibility=AnimationImpulseVisibility.AdvanceWhileHidden,
        AnimationImpulseTiming timing=AnimationImpulseTiming.EventTime) =>
        new(new(1f),curve,overlap,visibility,AnimationClock.Presentation,timing,
            new((float)(curve is AnimationImpulseCurve.LinearDecay or AnimationImpulseCurve.SmoothDecay?0:.5)));
    private static AnimationOscillationDefinition Oscillator(float? decay=null,float? frequency=null,float? velocity=null) =>
        new(new(decay??1f),new(frequency??2f),
            new AnimationVelocity(new AnimationRadiansPerSecond(velocity??2f)),AnimationClock.Presentation);

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
        var batch=new AnimationBatch();
        var handle=batch.Register(Binding(property:property),Clip(property,curve,repeat,clock));
        batch.Start(handle);batch.Advance(1,AnimationTestTime.Seconds(0),AnimationTestTime.Seconds(0));Equal(.25,batch.Read(handle).Value);
        var quarterAmount=curve switch
        {
            AnimationCurve.Linear=>.25,
            AnimationCurve.SmoothStep=>.103515625,
            AnimationCurve.CubicSmoothStep=>.15625,
            AnimationCurve.SinePulse=>Math.Sqrt(.5),
            _=>throw new ArgumentOutOfRangeException(nameof(curve))
        };
        batch.Advance(2,AnimationTestTime.Seconds(clock==AnimationClock.Presentation?.25:0),AnimationTestTime.Seconds(clock==AnimationClock.Simulation?.25:0));
        // One sine plus convex interpolation: allow eight f32 roundings within this unit-scale channel.
        Assert.InRange(BitConverter.UInt32BitsToSingle(batch.Read(handle).Value.CanonicalBits),
            (float)(.25+.5*quarterAmount)-5e-7f,(float)(.25+.5*quarterAmount)+5e-7f);
        batch.Advance(3,AnimationTestTime.Seconds(1),AnimationTestTime.Seconds(1));
        Equal(repeat==AnimationRepeat.Loop||curve==AnimationCurve.SinePulse?.25:.75,batch.Read(handle).Value);
        batch.Advance(4,AnimationTestTime.Seconds(2),AnimationTestTime.Seconds(2));
        Equal(repeat==AnimationRepeat.Once&&curve!=AnimationCurve.SinePulse?.75:.25,batch.Read(handle).Value);
        Assert.Equal(repeat==AnimationRepeat.Once?AnimationPlayback.Completed:AnimationPlayback.Playing,batch.Read(handle).Playback);
    }

    [Fact]
    public void EndpointReversalRateHoldRestoreAndCycleLegRemainCanonical()
    {
        var batch=new AnimationBatch();
        var once=batch.Register(Binding(),Clip());batch.Start(once);
        batch.DriveTo(once,AnimationEndpoint.From,AnimationTestTime.Seconds(.75));
        batch.Advance(1,AnimationTestTime.Seconds(1),AnimationTestTime.Seconds(0));Equal(.5,batch.Read(once).Value);
        batch.Stop(once,AnimationStop.Hold,AnimationTestTime.Seconds(1.25));Equal(.375,batch.Read(once).Value);
        batch.DriveTo(once,AnimationEndpoint.To,AnimationTestTime.Seconds(1.25));
        batch.Advance(2,AnimationTestTime.Seconds(1.5),AnimationTestTime.Seconds(0));Equal(.5,batch.Read(once).Value);
        batch.Stop(once,AnimationStop.RestoreInitial,AnimationTestTime.Seconds(1.5));Equal(.25,batch.Read(once).Value);
        var loop=batch.Register(Binding(2),Clip(repeat:AnimationRepeat.Loop));batch.Start(loop);
        batch.SetLoopRate(loop,new(-1f),AnimationTestTime.Seconds(1.75));
        batch.Advance(3,AnimationTestTime.Seconds(2),AnimationTestTime.Seconds(0));Equal(.25,batch.Read(loop).Value);
        batch.SetLoopRate(loop,new(0f),AnimationTestTime.Seconds(2.25));
        batch.Advance(4,AnimationTestTime.Seconds(3),AnimationTestTime.Seconds(0));Equal(.625,batch.Read(loop).Value);
        batch.SetLoopRate(loop,new(1f),AnimationTestTime.Seconds(3));
        batch.Advance(5,AnimationTestTime.Seconds(3.25),AnimationTestTime.Seconds(0));Equal(.25,batch.Read(loop).Value);
        batch.Remove(loop);
        var ping=batch.Register(Binding(2),Clip(repeat:AnimationRepeat.PingPong));batch.Start(ping);
        batch.Stop(ping,AnimationStop.Hold,AnimationTestTime.Seconds(4.75));Equal(.5,batch.Read(ping).Value);
        batch.Advance(6,AnimationTestTime.Seconds(5),AnimationTestTime.Seconds(0));Equal(.5,batch.Read(ping).Value);
        batch.Start(ping);batch.Advance(7,AnimationTestTime.Seconds(5.25),AnimationTestTime.Seconds(0));Equal(.375,batch.Read(ping).Value);
    }

    [Theory]
    [InlineData(AnimationRepeat.Once)]
    [InlineData(AnimationRepeat.Loop)]
    [InlineData(AnimationRepeat.PingPong)]
    public void MaximumIntegerTimeAndCanonicalDurationExtremesRemainFinite(AnimationRepeat repeat)
    {
        foreach(var duration in new[]{float.Epsilon,float.MaxValue})
        {
            var batch=new AnimationBatch();var handle=batch.Register(Binding(),Clip(repeat:repeat,duration:duration));
            batch.Start(handle);batch.Advance(1,new PhysicalTime(ulong.MaxValue),AnimationTestTime.Seconds(0));
            // The largest clock is still shorter than float.MaxValue seconds; the minimum duration has
            // ended for Once and is an exact divisor of every representable local f32 sample for repetitions.
            Equal(repeat == AnimationRepeat.Once && duration == float.Epsilon ? .75 : .25, batch.Read(handle).Value);
            batch.Stop(handle,AnimationStop.Hold,new PhysicalTime(ulong.MaxValue));
            Assert.True(float.IsFinite(BitConverter.UInt32BitsToSingle(batch.Read(handle).Value.CanonicalBits)));
        }
    }

    [Fact]
    public void ReversedWorldTimeCannotMutateEarlierSlotsClocksSamplesOrPendingOccurrences()
    {
        var batch=new AnimationBatch();
        var valid=batch.Register(Binding(),Clip());batch.Start(valid);
        var impulse=batch.RegisterImpulses(Binding(2),Impulse(),Value(AnimationProperty.Opacity,0),Value(AnimationProperty.Opacity,1));
        batch.EnqueueImpulse(impulse,new(1),new(1f),AnimationTestTime.Seconds(0));
        var loop=batch.Register(Binding(3),Clip(repeat:AnimationRepeat.Loop));batch.Start(loop);
        batch.SetLoopRate(loop,new(float.MaxValue),AnimationTestTime.Seconds(0));
        batch.Advance(1,AnimationTestTime.Seconds(0),new PhysicalTime(1));
        var reads=new[]{batch.Read(valid),batch.Read(impulse),batch.Read(loop)};
        var occurrences=batch.ReadImpulses(impulse);var samples=batch.Samples.ToArray();
        Assert.Throws<ArgumentOutOfRangeException>(()=>batch.Advance(2,AnimationTestTime.Seconds(.25),default));
        Assert.Equal(reads,new[]{batch.Read(valid),batch.Read(impulse),batch.Read(loop)});
        Assert.Equal(occurrences,batch.ReadImpulses(impulse));Assert.Equal(samples,batch.Samples.ToArray());
        batch.Advance(2,AnimationTestTime.Seconds(.25),new PhysicalTime(1));Equal(.375,batch.Read(valid).Value);
    }
    [Fact]
    public void InvalidOccurrenceTimestampCannotConsumeEarlierPlayback()
    {
        var batch=new AnimationBatch();var clip=batch.Register(Binding(),Clip());batch.Start(clip);
        var impulse=batch.RegisterImpulses(Binding(2),Impulse(),Value(AnimationProperty.Opacity,0),Value(AnimationProperty.Opacity,1));
        var before=batch.Read(clip);var pending=batch.ReadImpulses(impulse);
        Assert.Throws<ArgumentOutOfRangeException>(()=>batch.EnqueueImpulse(impulse,new(1),new(1f),new PhysicalTime(0,-1)));
        Assert.Equal(before,batch.Read(clip));Assert.Equal(pending,batch.ReadImpulses(impulse));
        batch.Advance(1,AnimationTestTime.Seconds(0),AnimationTestTime.Seconds(0));
    }

    [Fact]
    public void FollowUsesCanonicalControlAnchorsAndIndependentClock()
    {
        var batch=new AnimationBatch();
        var handle=batch.RegisterFollow(Binding(),new(Value(AnimationProperty.Opacity,0),Value(AnimationProperty.Opacity,1),
            Value(AnimationProperty.Opacity,.125),new(2f),AnimationClock.Presentation));
        batch.FollowValue(handle,Value(AnimationProperty.Opacity,.875),AnimationTestTime.Seconds(0));
        var first = (float)(.875 + (.125 - .875) * Math.Exp(-1));
        batch.Advance(1,AnimationTestTime.Seconds(.5),AnimationTestTime.Seconds(999));Equal(first,batch.Read(handle).Value);
        batch.FollowValue(handle,Value(AnimationProperty.Opacity,.25),AnimationTestTime.Seconds(.5));
        batch.Advance(2,AnimationTestTime.Seconds(1),AnimationTestTime.Seconds(999));Equal(.25 + (first - .25) * Math.Exp(-1),batch.Read(handle).Value);
        batch.Stop(handle,AnimationStop.RestoreInitial,AnimationTestTime.Seconds(1));Equal(.125,batch.Read(handle).Value);
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
        var batch=new AnimationBatch();
        var handle=batch.RegisterImpulses(Binding(),Impulse(curve,overlap,visibility,timing),
            Value(AnimationProperty.Opacity,0),Value(AnimationProperty.Opacity,1));
        batch.EnqueueImpulse(handle,new(1),new(.5f),AnimationTestTime.Seconds(0));
        batch.EnqueueImpulse(handle,new(2),new(.25f),AnimationTestTime.Seconds(0));
        batch.Advance(1,AnimationTestTime.Seconds(0),AnimationTestTime.Seconds(0));batch.Advance(2,AnimationTestTime.Seconds(.25),AnimationTestTime.Seconds(0));
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
        // Bounded unit envelope: trigonometric evaluation, square, strength and overlap use at most eight
        // rounded f32 operations at magnitude <= 1. Endpoints below remain exact.
        Assert.InRange(BitConverter.UInt32BitsToSingle(batch.Read(handle).Value.CanonicalBits) - expected,
            -8 * Math.ScaleB(1, -24), 8 * Math.ScaleB(1, -24));
        batch.Advance(3,AnimationTestTime.Seconds(1),AnimationTestTime.Seconds(0));Equal(0,batch.Read(handle).Value);
        Assert.Equal(2UL,batch.ReadImpulses(handle).Completed);
        batch.EnqueueImpulse(handle,new(3),new(1f),AnimationTestTime.Seconds(1));
        batch.CancelImpulses(handle);
        Assert.Equal(1UL,batch.ReadImpulses(handle).Cancelled);
        Assert.Throws<ArgumentException>(()=>batch.EnqueueImpulse(handle,new(3),new(1f),AnimationTestTime.Seconds(1)));
    }

    [Theory]
    [InlineData(AnimationImpulseVisibility.DeferUntilVisible)]
    [InlineData(AnimationImpulseVisibility.AdvanceWhileHidden)]
    public void HiddenImpulseVisibilityAndFirstFrameRetentionRemainDistinct(AnimationImpulseVisibility visibility)
    {
        var batch=new AnimationBatch();var handle=batch.RegisterImpulses(Binding(),
            Impulse(visibility:visibility,timing:AnimationImpulseTiming.EventTimeRetainFirstFrame),
            Value(AnimationProperty.Opacity,0),Value(AnimationProperty.Opacity,1));
        batch.SetVisible(handle,false);batch.EnqueueImpulse(handle,new(1),new(.5f),AnimationTestTime.Seconds(0));
        var hidden=batch.Advance(1,AnimationTestTime.Seconds(2),AnimationTestTime.Seconds(0));Assert.Equal(0,hidden.Evaluated);Assert.Empty(batch.Samples.ToArray());
        batch.SetVisible(handle,true);batch.Advance(2,AnimationTestTime.Seconds(2),AnimationTestTime.Seconds(0));
        Equal(visibility==AnimationImpulseVisibility.DeferUntilVisible?.5:0,batch.Read(handle).Value);
        batch.Advance(3,AnimationTestTime.Seconds(3),AnimationTestTime.Seconds(0));Equal(0,batch.Read(handle).Value);
    }

    [Fact]
    public void OscillationGoldenSampleAndRoundedZeroCrossingDoNotRetireFutureMotion()
    {
        var binding=Binding(property:AnimationProperty.LocalRotationAngle);
        var batch=new AnimationBatch();var handle=batch.RegisterOscillation(binding,Oscillator());
        batch.KickOscillation(handle,new(1),new(1f),AnimationTestTime.Seconds(0));batch.Advance(1,AnimationTestTime.Seconds(.25),AnimationTestTime.Seconds(0));
        var read=batch.ReadOscillation(handle);Equal(Math.Exp(-.25) * Math.Sin(.5),read.Position);
        Assert.Equal((float)(Math.Exp(-.25) * (2 * Math.Cos(.5) - Math.Sin(.5))),read.Velocity.RadiansPerSecond.Value);
        batch.Remove(handle);
        var slow=1e-6f;
        handle=batch.RegisterOscillation(binding,Oscillator(slow,slow,float.Epsilon));
        batch.KickOscillation(handle,new(1),new(1f),AnimationTestTime.Seconds(.25));
        var crossing=.25+Math.PI/(double)slow;
        batch.Advance(2,AnimationTestTime.Seconds(crossing),AnimationTestTime.Seconds(0));
        Assert.Equal(AnimationPlayback.Playing,batch.Read(handle).Playback);
        batch.Advance(3,AnimationTestTime.Seconds(.25+1.5*Math.PI/(double)slow),AnimationTestTime.Seconds(0));
        Equal(-Math.Exp(-1.5 * Math.PI) * (double)float.Epsilon / slow,batch.Read(handle).Value);
    }

    [Fact]
    public void OscillationAdmissionChecksRoundedAnchorsBeforeChangingIdentity()
    {
        var batch=new AnimationBatch();
        var handle=batch.RegisterOscillation(Binding(property:AnimationProperty.LocalRotationAngle),
            Oscillator(1f,1f,float.MaxValue));
        var before=batch.ReadOscillation(handle);
        Assert.Throws<ArgumentOutOfRangeException>(()=>batch.KickOscillation(handle,new(1),new(1f),AnimationTestTime.Seconds(0)));
        Assert.Equal(before,batch.ReadOscillation(handle));
        batch.KickOscillation(handle,new(1),new(.25f),AnimationTestTime.Seconds(0));
        Assert.Equal(1UL,batch.ReadOscillation(handle).Accepted);
        batch.Stop(handle,AnimationStop.RestoreInitial,AnimationTestTime.Seconds(0));Equal(0,batch.Read(handle).Value);
    }

    [Fact]
    public void WriterOwnershipUnitsStaleHandlesAndGenerationRemainExact()
    {
        var batch=new AnimationBatch();var handle=batch.Register(Binding(),Clip());
        Assert.Throws<InvalidOperationException>(()=>batch.Register(Binding(),Clip()));
        var second=batch.Register(Binding(2),Clip());
        Assert.Equal(2,batch.RegisteredCount);
        Assert.Throws<ArgumentException>(()=>new AnimationBatch().Read(handle));
        batch.Remove(handle);Assert.Throws<ArgumentException>(()=>batch.Read(handle));
        var next=batch.Register(Binding(),Clip());Assert.NotEqual(handle.Version,next.Version);
        batch.Reset();Assert.Throws<ArgumentException>(()=>batch.Read(next));Assert.Equal(2UL,batch.Generation.Value);
        Assert.Throws<ArgumentException>(()=>batch.Read(second));
        Assert.Equal(2,batch.Samples.Length);
        foreach(var sample in batch.Samples)Equal(.25,sample.Value);
        Assert.Throws<ArgumentException>(()=>batch.Register(Binding(),Clip(AnimationProperty.LocalTranslation)));
        Assert.Throws<ArgumentException>(()=>batch.Register(default,Clip()));
        Assert.Equal(0,batch.RegisteredCount);
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationDefinition(default,default,new(1f),
            AnimationCurve.Linear,AnimationRepeat.Once,AnimationClock.Presentation));
    }

    [Fact]
    public void F32BoundaryProfileRejectsUndefinedValuesWithoutClamping()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationDurationSeconds(float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationDurationSeconds(0f));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationRadians(float.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationOpacity(1.01f));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationFrequencyRadiansPerSecond(float.Epsilon));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationImpulseDefinition(new(1f),
            AnimationImpulseCurve.SmoothRiseFall,AnimationImpulseOverlap.Maximum,
            AnimationImpulseVisibility.AdvanceWhileHidden,AnimationClock.Presentation,
            AnimationImpulseTiming.EventTime,new(1f)));
        var positive=new AnimationValue(new AnimationScale(float.Epsilon));
        var batch=new AnimationBatch();
        batch.Register(Binding(property:AnimationProperty.UniformScale),new(positive,positive,new(float.MaxValue),
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
        var batch=new AnimationBatch();var handle=batch.Register(Binding(),Clip(repeat:AnimationRepeat.Loop));
        batch.Start(handle);
        for(ulong i=1;i<=1000;i++)batch.Advance(i,AnimationTestTime.Seconds(i/60.0),AnimationTestTime.Seconds(0));
        var before=GC.GetAllocatedBytesForCurrentThread();
        for(ulong i=1001;i<=3000;i++)batch.Advance(i,AnimationTestTime.Seconds(i/60.0),AnimationTestTime.Seconds(0));
        Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread()-before);
        batch.Stop(handle,AnimationStop.Hold,AnimationTestTime.Seconds(50));
        batch.Advance(3001,AnimationTestTime.Seconds(50),AnimationTestTime.Seconds(0));
        var inactive=batch.Advance(3002,AnimationTestTime.Seconds(51),AnimationTestTime.Seconds(0));
        Assert.Equal(0,inactive.Evaluated);Assert.Equal(0,inactive.DirtyWrites);Assert.Equal(0,inactive.Scheduled);
    }
    [Fact]
    public void SubF32ImpulseContributionCannotConsumeRetainedFirstFrame()
    {
        var batch=new AnimationBatch();var handle=batch.RegisterImpulses(Binding(),
            Impulse(AnimationImpulseCurve.SineSquaredPulse,timing:AnimationImpulseTiming.EventTimeRetainFirstFrame),
            Value(AnimationProperty.Opacity,0),Value(AnimationProperty.Opacity,1));
        batch.EnqueueImpulse(handle,new(1),new(1f),AnimationTestTime.Seconds(0));
        batch.Advance(1,AnimationTestTime.Seconds(1e-25),AnimationTestTime.Seconds(0));Equal(0,batch.Read(handle).Value);
        batch.Advance(2,AnimationTestTime.Seconds(2),AnimationTestTime.Seconds(0));Equal(1,batch.Read(handle).Value);
        Assert.Equal(1,batch.ReadImpulses(handle).Playing);
        batch.Advance(3,AnimationTestTime.Seconds(3),AnimationTestTime.Seconds(0));Equal(0,batch.Read(handle).Value);
        Assert.Equal(1UL,batch.ReadImpulses(handle).Completed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PositiveCanonicalImpulseIsLogicalObservationEvenWithNoMappedChange(bool flat)
    {
        var batch=new AnimationBatch();var handle=batch.RegisterImpulses(Binding(property:AnimationProperty.UniformScale),
            Impulse(timing:AnimationImpulseTiming.EventTimeRetainFirstFrame),
            Value(AnimationProperty.UniformScale,1),Value(AnimationProperty.UniformScale,flat?1:1.0009765625));
        batch.EnqueueImpulse(handle,new(1),new(float.Epsilon),AnimationTestTime.Seconds(0));
        batch.Advance(1,AnimationTestTime.Seconds(0),AnimationTestTime.Seconds(0));Equal(1,batch.Read(handle).Value);
        batch.Advance(2,AnimationTestTime.Seconds(2),AnimationTestTime.Seconds(0));
        Assert.Equal(1UL,batch.ReadImpulses(handle).Completed);
        Assert.Equal(0,batch.ReadImpulses(handle).Playing);
    }

    [Fact]
    public void MaximumMaskedPositiveCanonicalOccurrenceStillCompletes()
    {
        var batch=new AnimationBatch();var handle=batch.RegisterImpulses(Binding(),
            Impulse(timing:AnimationImpulseTiming.EventTimeRetainFirstFrame),
            Value(AnimationProperty.Opacity,0),Value(AnimationProperty.Opacity,1));
        batch.EnqueueImpulse(handle,new(1),new(1f),AnimationTestTime.Seconds(0));
        batch.EnqueueImpulse(handle,new(2),new(float.Epsilon),AnimationTestTime.Seconds(0));
        batch.Advance(1,AnimationTestTime.Seconds(0),AnimationTestTime.Seconds(0));Equal(1,batch.Read(handle).Value);
        batch.Advance(2,AnimationTestTime.Seconds(2),AnimationTestTime.Seconds(0));Equal(0,batch.Read(handle).Value);
        Assert.Equal(2UL,batch.ReadImpulses(handle).Completed);
    }

    [Fact]
    public void UndefinedEnumsAndDefaultInvalidQuantitiesRejectBeforeRegistration()
    {
        var from=Value(AnimationProperty.Opacity,0);var to=Value(AnimationProperty.Opacity,1);
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationDefinition(from,to,default,
            AnimationCurve.Linear,AnimationRepeat.Once,AnimationClock.Presentation));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationDefinition(from,to,new(1f),
            (AnimationCurve)255,AnimationRepeat.Once,AnimationClock.Presentation));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationDefinition(from,to,new(1f),
            AnimationCurve.Linear,(AnimationRepeat)255,AnimationClock.Presentation));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationDefinition(from,to,new(1f),
            AnimationCurve.Linear,AnimationRepeat.Once,(AnimationClock)255));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationFollowDefinition(from,to,from,default,AnimationClock.Presentation));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationOscillationDefinition(default,new(1f),
            new AnimationVelocity(new AnimationRadiansPerSecond(1f)),AnimationClock.Presentation));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationOscillationDefinition(new(1f),default,
            new AnimationVelocity(new AnimationRadiansPerSecond(1f)),AnimationClock.Presentation));
        Assert.Throws<ArgumentException>(()=>new AnimationOscillationDefinition(new(1f),new(1f),default,AnimationClock.Presentation));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Impulse((AnimationImpulseCurve)255));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Impulse(overlap:(AnimationImpulseOverlap)255));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Impulse(visibility:(AnimationImpulseVisibility)255));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Impulse(timing:(AnimationImpulseTiming)255));
        var batch=new AnimationBatch();var handle=batch.Register(Binding(),Clip());batch.Start(handle);
        var before=batch.Read(handle);
        Assert.Throws<ArgumentOutOfRangeException>(()=>batch.DriveTo(handle,(AnimationEndpoint)255,AnimationTestTime.Seconds(0)));
        Assert.Throws<ArgumentOutOfRangeException>(()=>batch.Stop(handle,(AnimationStop)255,AnimationTestTime.Seconds(0)));
        Assert.Throws<ArgumentException>(()=>batch.Read(default));
        foreach(var time in new[]{-1.0,double.NaN,double.PositiveInfinity})
            Assert.Throws<ArgumentOutOfRangeException>(()=>batch.Advance(1,AnimationTestTime.Seconds(time),AnimationTestTime.Seconds(0)));
        Assert.Equal(before,batch.Read(handle));
    }

    [Fact]
    public void OpaqueUlongTargetsAndDistinctPropertyWritersCannotAlias()
    {
        var batch=new AnimationBatch();
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
        var belowOne=(double)BitConverter.UInt32BitsToSingle(0x3f7fffff);
        var aboveOne=(double)BitConverter.UInt32BitsToSingle(0x3f800001);
        var belowTwo=(double)BitConverter.UInt32BitsToSingle(0x3fffffff);
        foreach(var phase in new[]{0.0,belowOne,1,aboveOne,belowTwo,2.0})
        {
            var batch=new AnimationBatch();var handle=batch.Register(Binding(),Clip(repeat:AnimationRepeat.PingPong));
            batch.Start(handle);batch.Stop(handle,AnimationStop.Hold,AnimationTestTime.Seconds(phase));
            var expected=(float)(.25+.5*(phase<=1?phase:2-phase));
            Assert.InRange(batch.Read(handle).Value.Opacity.Value,expected-2e-7f,expected+2e-7f);
        }
        var looping=new AnimationBatch();var loop=looping.Register(Binding(),Clip(repeat:AnimationRepeat.Loop));
        looping.Start(loop);
        looping.SetLoopRate(loop,new(0f),(AnimationTestTime.Seconds(1 - 1e-9))); // Stored float phase rounds to period, explicitly normalizes0.
        Equal(.25,looping.Read(loop).Value);
        looping.SetLoopRate(loop,new(-1f),(AnimationTestTime.Seconds(1 - 1e-9)));
        looping.Advance(1,AnimationTestTime.Seconds(1.25 - 1e-9),AnimationTestTime.Seconds(0));Equal(.625,looping.Read(loop).Value);
    }

    [Fact]
    public void HiddenFollowAndOscillatorMatchSkippedCadenceAndRetireOnlyTrueCompletion()
    {
        var visible=new AnimationBatch();var hidden=new AnimationBatch();
        var follow=new AnimationFollowDefinition(Value(AnimationProperty.Opacity,0),Value(AnimationProperty.Opacity,1),
            Value(AnimationProperty.Opacity,0),new(2f),AnimationClock.Presentation);
        var vf=visible.RegisterFollow(Binding(),follow);var hf=hidden.RegisterFollow(Binding(),follow);
        var vo=visible.RegisterOscillation(Binding(2,AnimationProperty.LocalRotationAngle),Oscillator());
        var ho=hidden.RegisterOscillation(Binding(2,AnimationProperty.LocalRotationAngle),Oscillator());
        visible.FollowValue(vf,Value(AnimationProperty.Opacity,1),AnimationTestTime.Seconds(0));hidden.FollowValue(hf,Value(AnimationProperty.Opacity,1),AnimationTestTime.Seconds(0));
        visible.KickOscillation(vo,new(1),new(1f),AnimationTestTime.Seconds(0));hidden.KickOscillation(ho,new(1),new(1f),AnimationTestTime.Seconds(0));
        hidden.SetVisible(hf,false);hidden.SetVisible(ho,false);
        for(ulong frame=1;frame<=30;frame++)
        {
            visible.Advance(frame,AnimationTestTime.Seconds(frame/60.0),AnimationTestTime.Seconds(0));
            var work=hidden.Advance(frame,AnimationTestTime.Seconds(frame/60.0),AnimationTestTime.Seconds(0));Assert.Equal(0,work.Evaluated);Assert.Equal(0,work.DirtyWrites);
        }
        hidden.SetVisible(hf,true);hidden.SetVisible(ho,true);
        visible.Advance(31,AnimationTestTime.Seconds(1),AnimationTestTime.Seconds(0));hidden.Advance(31,AnimationTestTime.Seconds(1),AnimationTestTime.Seconds(0));
        Assert.Equal(visible.Read(vf),hidden.Read(hf));Assert.Equal(visible.Read(vo),hidden.Read(ho));
        visible.Advance(32,AnimationTestTime.Seconds(1000),AnimationTestTime.Seconds(0));
        Assert.Equal(AnimationPlayback.Completed,visible.Read(vf).Playback);
        Assert.Equal(AnimationPlayback.Completed,visible.Read(vo).Playback);
        Assert.Equal(0,visible.Advance(33,AnimationTestTime.Seconds(1001),AnimationTestTime.Seconds(0)).Scheduled);
    }

    [Fact]
    public void OscillatorZeroCrossingFrameDoesNotChangeLaterSample()
    {
        var a=new AnimationBatch();var b=new AnimationBatch();var slow=1e-6f;
        var ha=a.RegisterOscillation(Binding(property:AnimationProperty.LocalRotationAngle),Oscillator(slow,slow,float.Epsilon));
        var hb=b.RegisterOscillation(Binding(property:AnimationProperty.LocalRotationAngle),Oscillator(slow,slow,float.Epsilon));
        a.KickOscillation(ha,new(1),new(1f),AnimationTestTime.Seconds(0));b.KickOscillation(hb,new(1),new(1f),AnimationTestTime.Seconds(0));
        a.Advance(1,AnimationTestTime.Seconds(Math.PI/(double)slow),AnimationTestTime.Seconds(0));
        a.Advance(2,AnimationTestTime.Seconds(1.5*Math.PI/(double)slow),AnimationTestTime.Seconds(0));b.Advance(1,AnimationTestTime.Seconds(1.5*Math.PI/(double)slow),AnimationTestTime.Seconds(0));
        Assert.Equal(a.Read(ha),b.Read(hb));
        a.Advance(3,AnimationTestTime.Seconds(1000/(double)slow),AnimationTestTime.Seconds(0));Assert.Equal(AnimationPlayback.Completed,a.Read(ha).Playback);
        Assert.Equal(0,a.Advance(4,AnimationTestTime.Seconds(1001/(double)slow),AnimationTestTime.Seconds(0)).Scheduled);
    }

    [Fact]
    public void MaximumOccurrenceIdentityRejectsReplayAndResetInvalidatesOldHandles()
    {
        var batch = new AnimationBatch();
        var impulse = batch.RegisterImpulses(Binding(), Impulse(), Value(AnimationProperty.Opacity, 0), Value(AnimationProperty.Opacity, 1));
        batch.EnqueueImpulse(impulse, new(ulong.MaxValue), new(1), AnimationTestTime.Seconds(0));
        var before = batch.ReadImpulses(impulse);
        Assert.Throws<ArgumentException>(() => batch.EnqueueImpulse(impulse, new(ulong.MaxValue), new(1), AnimationTestTime.Seconds(0)));
        Assert.Throws<ArgumentException>(() => batch.EnqueueImpulse(impulse, new(ulong.MaxValue - 1), new(1), AnimationTestTime.Seconds(0)));
        Assert.Equal(before, batch.ReadImpulses(impulse));
        batch.Advance(1, AnimationTestTime.Seconds(.25), AnimationTestTime.Seconds(0)); Equal(.75, batch.Read(impulse).Value);
        var oscillation = batch.RegisterOscillation(Binding(2, AnimationProperty.LocalRotationAngle), Oscillator());
        batch.KickOscillation(oscillation, new(ulong.MaxValue), new(1), AnimationTestTime.Seconds(.25));
        var prior = batch.ReadOscillation(oscillation);
        Assert.Throws<ArgumentException>(() => batch.KickOscillation(oscillation, new(ulong.MaxValue), new(1), AnimationTestTime.Seconds(.25)));
        Assert.Equal(prior, batch.ReadOscillation(oscillation));
        batch.Reset();
        Assert.Equal(16, batch.Capacity); Assert.Equal(0, batch.RegisteredCount);
        Assert.Throws<ArgumentException>(() => batch.Read(impulse));
        Assert.Throws<ArgumentException>(() => batch.Read(oscillation));
        var current = batch.RegisterImpulses(Binding(), Impulse(), Value(AnimationProperty.Opacity, 0), Value(AnimationProperty.Opacity, 1));
        batch.EnqueueImpulse(current, new(1), new(1), AnimationTestTime.Seconds(0)); batch.Advance(1, AnimationTestTime.Seconds(.25), AnimationTestTime.Seconds(0));
        Equal(.75, batch.Read(current).Value);
    }

    [Fact]
    public void RegistryGrowsFromCommonReserveKeepsHandlesAndResetsToCommonStorage()
    {
        var batch = new AnimationBatch(); Assert.Equal(16, batch.Capacity);
        var handles = Enumerable.Range(1, 65).Select(i => batch.Register(Binding((ulong)i), Clip())).ToArray();
        Assert.Equal(65, batch.RegisteredCount); Assert.InRange(batch.Capacity, 65, 130);
        foreach (var handle in handles) batch.Start(handle);
        batch.Remove(handles[0]);
        var replacement = batch.Register(Binding(66), Clip()); batch.Start(replacement);
        Assert.Equal(handles[0].Slot, replacement.Slot);
        Assert.True(replacement.Version > handles[0].Version);
        Assert.Throws<ArgumentException>(() => batch.Read(handles[0]));
        batch.Advance(1, AnimationTestTime.Seconds(.25), AnimationTestTime.Seconds(0));
        Assert.Equal(65, batch.Samples.Length);
        foreach (var handle in handles.Skip(1)) Equal(.375, batch.Read(handle).Value);
        Equal(.375, batch.Read(replacement).Value);
        var before = batch.Read(replacement); var capacity = batch.Capacity;
        Assert.Throws<AnimationStorageException>(() => batch.EnsureCapacity(int.MaxValue));
        Assert.Equal(before, batch.Read(replacement)); Assert.Equal(capacity, batch.Capacity);
        batch.Reset();
        Assert.Equal(16, batch.Capacity); Assert.Equal(0, batch.RegisteredCount);
        Assert.Equal(65, batch.Samples.Length);
        foreach (var sample in batch.Samples) Equal(.25, sample.Value);
        Assert.Throws<ArgumentException>(() => batch.Read(replacement));
        var next = batch.Register(Binding(1), Clip()); batch.Start(next); batch.Advance(1, AnimationTestTime.Seconds(.5), AnimationTestTime.Seconds(0));
        Equal(.5, batch.Read(next).Value);
    }

    [Fact]
    public void RegistryAndImpulseWatermarksReserveOnceAndPreserveValues()
    {
        var batch = new AnimationBatch();
        var handles = Enumerable.Range(1, 12).Select(i => batch.Register(Binding((ulong)i), Clip())).ToArray();
        Assert.Equal(16, batch.Capacity);
        var thirteenth = batch.Register(Binding(13), Clip());
        Assert.Equal(32, batch.Capacity);
        batch.EnsureCapacity(100);
        Assert.Equal(134, batch.Capacity);
        batch.EnsureCapacity(100);
        Assert.Equal(134, batch.Capacity);
        foreach (var handle in handles.Append(thirteenth)) { batch.Start(handle); Equal(.25, batch.Read(handle).Value); }
        var impulse = batch.RegisterImpulses(Binding(14), Impulse(), Value(AnimationProperty.Opacity, 0), Value(AnimationProperty.Opacity, 1));
        Assert.Equal(8, batch.ReadImpulses(impulse).Capacity);
        for (ulong id = 1; id <= 6; id++) batch.EnqueueImpulse(impulse, new(id), new(1), AnimationTestTime.Seconds(0));
        Assert.Equal(8, batch.ReadImpulses(impulse).Capacity);
        batch.EnqueueImpulse(impulse, new(7), new(1), AnimationTestTime.Seconds(0));
        Assert.Equal(16, batch.ReadImpulses(impulse).Capacity);
        batch.ReserveImpulses(impulse, 93);
        Assert.Equal(134, batch.ReadImpulses(impulse).Capacity);
        for (ulong id = 8; id <= 100; id++) batch.EnqueueImpulse(impulse, new(id), new(1), AnimationTestTime.Seconds(0));
        Assert.Equal(134, batch.ReadImpulses(impulse).Capacity);
        Assert.Equal(100UL, batch.ReadImpulses(impulse).Accepted);
        batch.Advance(1, AnimationTestTime.Seconds(.25), AnimationTestTime.Seconds(0));
        Equal(.75, batch.Read(impulse).Value);
        Assert.Equal(100, batch.ReadImpulses(impulse).Playing);
        batch.Advance(2, AnimationTestTime.Seconds(1), AnimationTestTime.Seconds(0));
        Assert.Equal(100UL, batch.ReadImpulses(impulse).Completed);
        batch.Reset(); Assert.Equal(16, batch.Capacity);
        Assert.Throws<ArgumentException>(() => batch.Read(impulse));
    }

    [Theory]
    [InlineData(65)]
    [InlineData(65537)]
    public void ImpulseStorageTracksRealDemandBeyondFormerLimits(int occurrences)
    {
        var batch = new AnimationBatch();
        var handle = batch.RegisterImpulses(Binding(), Impulse(), Value(AnimationProperty.Opacity, 0), Value(AnimationProperty.Opacity, 1));
        Assert.Equal(8, batch.ReadImpulses(handle).Capacity);
        for (var i = 1; i <= occurrences; i++) batch.EnqueueImpulse(handle, new((ulong)i), new(1), AnimationTestTime.Seconds(0));
        var pending = batch.ReadImpulses(handle);
        Assert.Equal(occurrences, pending.Pending); Assert.Equal((ulong)occurrences, pending.Accepted);
        Assert.InRange(pending.Capacity, occurrences, 2 * occurrences);
        batch.Advance(1, AnimationTestTime.Seconds(.25), AnimationTestTime.Seconds(0)); Equal(.75, batch.Read(handle).Value);
        Assert.Equal(occurrences, batch.ReadImpulses(handle).Playing);
        batch.Advance(2, AnimationTestTime.Seconds(1), AnimationTestTime.Seconds(0)); Equal(0, batch.Read(handle).Value);
        Assert.Equal((ulong)occurrences, batch.ReadImpulses(handle).Completed);
        batch.EnqueueImpulse(handle, new((ulong)occurrences + 1), new(1), AnimationTestTime.Seconds(1));
        batch.CancelImpulses(handle); Assert.Equal(1UL, batch.ReadImpulses(handle).Cancelled);
        batch.Reset(); Assert.Equal(16, batch.Capacity);
    }

    [Fact]
    public void WarmedFollowImpulseAndOscillationControlsAllocateNoScratch()
    {
        var batch=new AnimationBatch();
        var follow=batch.RegisterFollow(Binding(),new(Value(AnimationProperty.Opacity,0),Value(AnimationProperty.Opacity,1),
            Value(AnimationProperty.Opacity,0),new(2f),AnimationClock.Presentation));
        var impulse=batch.RegisterImpulses(Binding(2),Impulse(),Value(AnimationProperty.Opacity,0),Value(AnimationProperty.Opacity,1));
        var oscillation=batch.RegisterOscillation(Binding(3,AnimationProperty.LocalRotationAngle),Oscillator());
        void Step(ulong frame)
        {
            var time=frame/60.0;
            batch.FollowValue(follow,Value(AnimationProperty.Opacity,frame%2),AnimationTestTime.Seconds(time));
            batch.EnqueueImpulse(impulse,new(frame),new(.5f),AnimationTestTime.Seconds(time));
            batch.KickOscillation(oscillation,new(frame),new((float)(frame%2==0?.001:-.001)),AnimationTestTime.Seconds(time));
            batch.Advance(frame,AnimationTestTime.Seconds(time),AnimationTestTime.Seconds(0));batch.CancelImpulses(impulse);
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
            var batch=new AnimationBatch();
            var handle=batch.Register(Binding(),Clip(curve:AnimationCurve.SinePulse,repeat:AnimationRepeat.Loop));
            batch.Start(handle);
            if(mode==NativeWorkMode.Hidden)batch.SetVisible(handle,false);
            if(mode==NativeWorkMode.Inactive)batch.Stop(handle,AnimationStop.Hold,AnimationTestTime.Seconds(0));
            for(ulong frame=1;frame<=warmup;frame++)batch.Advance(frame,AnimationTestTime.Seconds(frame/60.0),AnimationTestTime.Seconds(0));
            _=System.Diagnostics.Stopwatch.GetTimestamp();
            var allocated=GC.GetAllocatedBytesForCurrentThread();
            var started=System.Diagnostics.Stopwatch.GetTimestamp();
            AnimationFrameWork final=default;
            for(ulong frame=warmup+1;frame<=warmup+iterations;frame++)
                final=batch.Advance(frame,AnimationTestTime.Seconds(frame/60.0),AnimationTestTime.Seconds(0));
            var elapsed=System.Diagnostics.Stopwatch.GetTimestamp()-started;
            var bytes=GC.GetAllocatedBytesForCurrentThread()-allocated;
            output.WriteLine($"ANIMATION_NATIVE_COST mode={mode} registered=1 warmup={warmup} iterations={iterations} elapsedTicks={elapsed} frequency={System.Diagnostics.Stopwatch.Frequency} allocatedBytes={bytes} finalEvaluated={final.Evaluated} finalScheduled={final.Scheduled}");
            Assert.Equal(0,bytes);
            Assert.True(elapsed>0);
            Assert.Equal(mode==NativeWorkMode.Active?1:0,final.Evaluated);
            Assert.Equal(mode==NativeWorkMode.Inactive?0:1,final.Scheduled);
        }
    }

    // Story 9A.44 D7: a lamp's 0→1 colour clip ends on exactly the f32 1 the client reads (`value === 1` in the browser suites):
    // at its end position and once completed, every rising curve returns its To endpoint bit for bit, and the last position before
    // the end is never above 1. Control: a sine pulse ends back on its From endpoint.
    [Theory]
    [InlineData(AnimationCurve.Linear)]
    [InlineData(AnimationCurve.SmoothStep)]
    [InlineData(AnimationCurve.CubicSmoothStep)]
    [InlineData(AnimationCurve.SinePulse)]
    public void AUnitClipEndsOnExactlyItsF32Endpoint(AnimationCurve curve)
    {
        var batch = new AnimationBatch();
        var handle = batch.Register(Binding(property: AnimationProperty.ColourBlend), new AnimationDefinition(
            Value(AnimationProperty.ColourBlend, 0), Value(AnimationProperty.ColourBlend, 1), new(.25f), curve, AnimationRepeat.Once,
            AnimationClock.Presentation));
        float At(ulong frame, double time) { batch.Advance(frame, AnimationTestTime.Seconds(time), AnimationTestTime.Seconds(0)); return BitConverter.UInt32BitsToSingle(batch.Read(handle).Value.CanonicalBits); }
        batch.Start(handle); At(1, 0);
        var mid = At(2, .125);
        var last = At(3, Math.BitDecrement(.25));
        var end = At(4, .25);
        var completed = At(5, 1);
        var endpoint = curve == AnimationCurve.SinePulse ? 0f : 1f;
        Assert.Equal(endpoint, end);
        Assert.Equal(endpoint, completed);
        Assert.Equal(AnimationPlayback.Completed, batch.Read(handle).Playback);
        Assert.InRange(last, 0f, 1f);
        if (curve != AnimationCurve.SinePulse) Assert.True(mid is > 0f and < 1f, $"mid-clip is partial: {mid}");
    }
}
