using CuriousContraptions.Abstractions;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

public sealed class PhysicalTimeTests
{
    [Fact]
    public void RationalClockSamplesCarryAtHalfAnOrdinalWithoutLosingTheirLocalPhase()
    {
        var before = PhysicalTime.FromNanoseconds(1_041_666);
        var after = PhysicalTime.FromNanoseconds(1_041_667);
        Assert.Equal(0ul, before.Ordinal);
        Assert.Equal(1ul, after.Ordinal);
        Assert.True(before.Phase > 2047 && after.Phase < -2047);
        Assert.True(after > before);
        // One nanosecond across normalization; each bounded phase conversion rounds at most one f32 spacing.
        Assert.InRange(after.SecondsSince(before), .8e-9f, 1.2e-9f);
        Assert.Equal(new PhysicalTime(480), PhysicalTime.FromNanoseconds(1_000_000_000));
    }

    [Theory]
    [InlineData(0ul)]
    [InlineData(1_728_000ul)]
    [InlineData(18_014_398_509_481_984ul)]
    public void FreshLocalMotionKeepsAdjacentSamplesAtLargeClockOrigins(ulong origin)
    {
        var anchor = new PhysicalTime(origin);
        var first = new PhysicalTime(origin, 1024);
        var second = new PhysicalTime(origin, 1025);
        Assert.InRange(first.SecondsSince(anchor), .0005208332f, .0005208335f);
        Assert.True(second.SecondsSince(anchor) > first.SecondsSince(anchor));
        // A two-second loop: a quarter substep is 1/3840 of the loop.
        Assert.InRange(first.PeriodicPhaseSince(anchor, 2, 1), 1f / 3840 - 1e-10f, 1f / 3840 + 1e-10f);
    }

    [Fact]
    public void LongLivedLoopsReduceCompletePeriodsBeforeConvertingToFloat()
    {
        var atHour = new PhysicalTime(1_728_000);
        Assert.Equal(0f, atHour.PeriodicPhaseSince(default, 2, 1));
        Assert.Equal(.25f, new PhysicalTime(1_728_240).PeriodicPhaseSince(default, 2, 1));
        Assert.Equal(.75f, new PhysicalTime(1_728_240).PeriodicPhaseSince(default, 2, -1));
        Assert.Equal(0f, atHour.PeriodicPhaseSince(default, 2, 0));
        Assert.Equal(1.5f, new PhysicalTime(1_728_720).PeriodicPhaseSince(default, 1, 1, 2));
    }

    [Fact]
    public void RunningLoopStartedAtZeroKeepsKnownPhasesAfterAnHour()
    {
        var batch = new AnimationBatch();
        var handle = batch.Register(new(new(1), AnimationProperty.Opacity),
            new(new(new AnimationOpacity(0)), new(new AnimationOpacity(1)), new(2),
                AnimationCurve.Linear, AnimationRepeat.Loop, AnimationClock.Presentation));
        batch.Start(handle);
        batch.Advance(1, new(240), default);
        Assert.InRange(batch.Read(handle).Value.Opacity.Value, .24999f, .25001f);
        // The same running two-second loop, aged by a direct jump: 1800 whole periods then known quarters.
        ulong frame = 1;
        foreach (var (ordinal, expected) in new[] { (1_728_000ul, 0f), (1_728_240ul, .25f),
                     (1_728_480ul, .5f), (1_728_720ul, .75f), (1_728_960ul, 0f) })
        {
            batch.Advance(++frame, new(ordinal), default);
            Assert.InRange(batch.Read(handle).Value.Opacity.Value, expected - .00001f, expected + .00001f);
        }
    }

    [Fact]
    public void TinyValidLocalPhasesRemainRepresentableWithoutAFixedPhaseGrid()
    {
        var time = new PhysicalTime(0, .000001f);
        Assert.InRange(time.PeriodicPhaseSince(default, 1, 1), 5.08e-13f, 5.10e-13f);
    }

    [Theory]
    [InlineData(0ul)]
    [InlineData(1_728_000ul)]
    [InlineData(18_014_398_509_481_984ul)]
    public void RealAnimationKeepsLocalLoopProgressAndFreezesOnlyTheWorldClock(ulong ordinal)
    {
        var batch = new CuriousContraptions.Presentation.AnimationBatch();
        var origin = new PhysicalTime(ordinal);
        batch.Advance(1, origin, origin);
        var ui = batch.Register(new(new(1), CuriousContraptions.Presentation.AnimationProperty.Opacity),
            new(new(new CuriousContraptions.Presentation.AnimationOpacity(0)),
                new(new CuriousContraptions.Presentation.AnimationOpacity(1)), new(2),
                CuriousContraptions.Presentation.AnimationCurve.Linear, CuriousContraptions.Presentation.AnimationRepeat.Loop,
                CuriousContraptions.Presentation.AnimationClock.Presentation));
        var world = batch.Register(new(new(2), CuriousContraptions.Presentation.AnimationProperty.Opacity),
            new(new(new CuriousContraptions.Presentation.AnimationOpacity(0)),
                new(new CuriousContraptions.Presentation.AnimationOpacity(1)), new(2),
                CuriousContraptions.Presentation.AnimationCurve.Linear, CuriousContraptions.Presentation.AnimationRepeat.Loop,
                CuriousContraptions.Presentation.AnimationClock.Simulation));
        batch.Start(ui); batch.Start(world);
        batch.Advance(2, new(ordinal + 240), new(ordinal + 240));
        Assert.InRange(batch.Read(ui).Value.Opacity.Value, .24999f, .25001f);
        Assert.Equal(batch.Read(ui).Value, batch.Read(world).Value);
        batch.Advance(3, new(ordinal + 480), new(ordinal + 240));
        Assert.InRange(batch.Read(ui).Value.Opacity.Value, .49999f, .50001f);
        Assert.InRange(batch.Read(world).Value.Opacity.Value, .24999f, .25001f);
        batch.Advance(4, new(ordinal + 720), new(ordinal + 480));
        Assert.InRange(batch.Read(world).Value.Opacity.Value, .49999f, .50001f);
        batch.Reset();
        Assert.Throws<ArgumentException>(() => batch.Read(world));
        Assert.Equal(0, batch.RegisteredCount);
    }

    [Fact]
    public void InvalidPhaseAndOverflowingClockConversionsRefuse()
    {
        foreach (var phase in new[] { float.NaN, float.PositiveInfinity, -2049f, 2048f })
            Assert.Throws<ArgumentOutOfRangeException>(() => new PhysicalTime(1, phase));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PhysicalTime(0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PhysicalTime.FromNanoseconds(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PhysicalTime.FromRatio(1, 0));
        Assert.Throws<OverflowException>(() => PhysicalTime.FromRatio(UInt128.MaxValue, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => default(PhysicalTime).PeriodicPhaseSince(default, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => default(PhysicalTime).PeriodicPhaseSince(default, 1, float.NaN));
    }

    [Theory]
    [InlineData(0ul)]
    [InlineData(1_728_000ul)]
    [InlineData(18_014_398_509_481_984ul)]
    public void ShortFollowImpulseAndOscillationProgressAtTheSameLocalTimes(ulong ordinal)
    {
        var batch = new AnimationBatch();
        var origin = new PhysicalTime(ordinal);
        batch.Advance(1, origin, origin);
        var zero = new AnimationValue(new AnimationOpacity(0));
        var one = new AnimationValue(new AnimationOpacity(1));
        var follow = batch.RegisterFollow(new(new(1), AnimationProperty.Opacity),
            new(zero, one, zero, new(2), AnimationClock.Presentation));
        var impulse = batch.RegisterImpulses(new(new(2), AnimationProperty.Opacity),
            new(new(1), AnimationImpulseCurve.LinearDecay, AnimationImpulseOverlap.Maximum,
                AnimationImpulseVisibility.AdvanceWhileHidden, AnimationClock.Presentation,
                AnimationImpulseTiming.EventTime, new(0)), zero, one);
        var oscillator = batch.RegisterOscillation(new(new(3), AnimationProperty.LocalRotationAngle),
            new(new(1), new(2), new(new AnimationRadiansPerSecond(2)), AnimationClock.Presentation));
        batch.FollowValue(follow, one, origin);
        batch.EnqueueImpulse(impulse, new(1), new(1), origin);
        batch.KickOscillation(oscillator, new(1), new(1), origin);
        batch.Advance(2, new(ordinal + 120), origin);
        // Independently evaluated analytic values at 1/4 second, with <1e-5 channel/radian error.
        Assert.InRange(batch.Read(follow).Value.Opacity.Value, .39346f, .39348f);
        Assert.Equal(.75f, batch.Read(impulse).Value.Opacity.Value);
        Assert.InRange(batch.ReadOscillation(oscillator).Position.Radians.Value, .37337f, .37339f);
        batch.Advance(3, new(ordinal + 480), origin);
        Assert.Equal(0f, batch.Read(impulse).Value.Opacity.Value);
        Assert.Equal(1ul, batch.ReadImpulses(impulse).Completed);
        Assert.InRange(batch.Read(follow).Value.Opacity.Value, .86465f, .86468f);
        batch.Advance(4, new(ordinal + 48_000), origin);
        Assert.Equal(one, batch.Read(follow).Value);
        // Under a milliradian is visually settled for this cosmetic rotation.
        const float settledAngle = .001f;
        Assert.InRange(batch.ReadOscillation(oscillator).Position.Radians.Value, -settledAngle, settledAngle);
    }

    [Fact]
    public void FiniteExtremeEndpointsRemainInsideTheirAuthoredInterval()
    {
        var from = new AnimationValue(new AnimationMetres(-float.MaxValue));
        var to = new AnimationValue(new AnimationMetres(float.MaxValue));
        Assert.Equal(0f, AnimationValue.Blend(from, to, new(.5f)).Metres.Value);
        var tiny = new AnimationValue(new AnimationScale(float.Epsilon));
        Assert.Equal(tiny, AnimationValue.Blend(tiny, tiny, new(.5f)));
        var batch = new AnimationBatch();
        var clip = batch.Register(new(new(1), AnimationProperty.LocalTranslation),
            new(from, to, new(1), AnimationCurve.Linear, AnimationRepeat.Once, AnimationClock.Presentation));
        var follow = batch.RegisterFollow(new(new(2), AnimationProperty.LocalTranslation),
            new(from, to, from, new(1), AnimationClock.Presentation));
        var impulse = batch.RegisterImpulses(new(new(3), AnimationProperty.LocalTranslation),
            new(new(1), AnimationImpulseCurve.LinearDecay, AnimationImpulseOverlap.Maximum,
                AnimationImpulseVisibility.AdvanceWhileHidden, AnimationClock.Presentation,
                AnimationImpulseTiming.EventTime, new(0)), from, to);
        batch.Start(clip); batch.FollowValue(follow, to, default);
        batch.EnqueueImpulse(impulse, new(1), new(1), default);
        batch.Advance(1, new(240), default);
        Assert.Equal(0f, batch.Read(clip).Value.Metres.Value);
        Assert.Equal(0f, batch.Read(impulse).Value.Metres.Value);
        // At 1/2 second, the exponential response has travelled about 39.35% of its interval.
        Assert.InRange(batch.Read(follow).Value.Metres.Value / float.MaxValue, -.214f, -.212f);
        batch.Advance(2, new(480), default);
        Assert.Equal(to, batch.Read(clip).Value);
        Assert.Equal(from, batch.Read(impulse).Value);
    }
}
