using CuriousContraptions.Presentation;
using CuriousContraptions.Physics;
using Godot;

namespace CuriousContraptions.Tests;

public class AnimationBatchTests
{
    private static AnimationBinding Binding(int index = 0, AnimationProperty property = AnimationProperty.LocalRotationAngle) =>
        new(new(index), property);
    private static AnimationDefinition Clip(AnimationClock clock = AnimationClock.Presentation,
        AnimationRepeat repeat = AnimationRepeat.Loop, AnimationCurve curve = AnimationCurve.Linear) =>
        new(0, 1, 1, curve, repeat, clock);

    [Theory]
    [InlineData(AnimationCurve.Linear, .25)]
    [InlineData(AnimationCurve.SmoothStep, .103515625)]
    [InlineData(AnimationCurve.SinePulse, .7071067811865476)]
    public void TypedCurvesSampleIndependentClocksAndDirtyValues(AnimationCurve curve, double expected)
    {
        var batch = new AnimationBatch(2);
        var visual = batch.Register(Binding(), Clip(curve: curve));
        var physicalClock = batch.Register(Binding(1), Clip(AnimationClock.Simulation, curve: curve));
        batch.Start(visual); batch.Start(physicalClock);
        var first = batch.Advance(1, .25, 0);
        Assert.InRange(Math.Abs(batch.Read(visual).Value - expected), 0, 1e-15);
        Assert.Equal(0, batch.Read(physicalClock).Value);
        Assert.Equal(2, first.DirtyWrites);
        var same = batch.Advance(2, .25, 0);
        Assert.Equal(0, same.DirtyWrites);
        Assert.Empty(batch.Samples.ToArray());
        batch.Advance(3, .25, .25);
        Assert.Equal(batch.Read(visual).Value, batch.Read(physicalClock).Value);
        Assert.Equal(Binding(1), Assert.Single(batch.Samples.ToArray()).Binding);
    }

    [Theory]
    [InlineData(AnimationRepeat.Once, 1, AnimationPlayback.Completed, 0)]
    [InlineData(AnimationRepeat.Loop, .25, AnimationPlayback.Playing, 1)]
    [InlineData(AnimationRepeat.PingPong, .75, AnimationPlayback.Playing, 1)]
    public void RepetitionAndCompletionUseAbsoluteClock(AnimationRepeat repeat, double value,
        AnimationPlayback state, int scheduled)
    {
        var batch = new AnimationBatch(1);
        var handle = batch.Register(Binding(), Clip(repeat: repeat));
        batch.Start(handle);
        batch.Advance(1, 1.25, 0);
        Assert.Equal(value, batch.Read(handle).Value);
        Assert.Equal(state, batch.Read(handle).Playback);
        Assert.Equal(scheduled, batch.ScheduledCount);
    }

    [Fact]
    public void HiddenLoopsResumeCurrentPhaseAndHiddenOneShotPublishesItsEnd()
    {
        var batch = new AnimationBatch(2);
        var loop = batch.Register(Binding(), Clip());
        var once = batch.Register(Binding(1), Clip(repeat: AnimationRepeat.Once));
        batch.Start(loop); batch.Start(once);
        batch.Advance(1, .25, 0);
        batch.SetVisible(loop, false); batch.SetVisible(once, false);
        var hidden = batch.Advance(2, 10.75, 0);
        Assert.Equal(0, hidden.DirtyWrites);
        Assert.Equal(AnimationPlayback.Completed, batch.Read(once).Playback);
        Assert.Equal(.25, batch.Read(loop).Value);
        Assert.Equal(1, batch.ScheduledCount);
        batch.SetVisible(loop, true); batch.SetVisible(once, true);
        var visible = batch.Advance(3, 10.75, 0);
        Assert.Equal(.75, batch.Read(loop).Value);
        Assert.Equal(1, batch.Read(once).Value);
        Assert.Equal(2, visible.DirtyWrites);
        Assert.Equal(1, batch.ScheduledCount);
    }

    [Fact]
    public void StopPoliciesAndRestartHaveExplicitValues()
    {
        var batch = new AnimationBatch(1);
        var handle = batch.Register(Binding(), Clip());
        batch.Start(handle);
        batch.Advance(1, .25, 0);
        batch.Stop(handle, AnimationStop.Hold, .25);
        batch.Advance(2, 100, 0);
        Assert.Equal(.25, batch.Read(handle).Value);
        Assert.Equal(0, batch.ScheduledCount);
        batch.Stop(handle, AnimationStop.RestoreInitial, 100);
        batch.Advance(3, 100, 0);
        Assert.Equal(0, Assert.Single(batch.Samples.ToArray()).Value);
        batch.Start(handle);
        batch.Advance(4, 100.5, 0);
        Assert.Equal(.5, batch.Read(handle).Value);
    }

    [Fact]
    public void PropertyOwnershipCapacityRemovalAndGenerationRejectStaleWriters()
    {
        var batch = new AnimationBatch(1);
        var old = batch.Register(Binding(), Clip());
        Assert.Throws<InvalidOperationException>(() => batch.Register(Binding(), Clip()));
        Assert.Throws<InvalidOperationException>(() => batch.Register(Binding(1), Clip()));
        var other = new AnimationBatch(1);
        var foreign = other.Register(Binding(), Clip());
        Assert.Throws<ArgumentException>(() => batch.Start(foreign));
        batch.Remove(old);
        Assert.Throws<ArgumentException>(() => batch.Read(old));
        var replacement = batch.Register(Binding(), Clip());
        Assert.NotEqual(old.Version, replacement.Version);
        Assert.Throws<ArgumentException>(() => batch.Start(old));
        batch.Start(replacement);
        batch.Advance(1, .5, 0);
        var generation = batch.Generation;
        batch.Reset();
        Assert.Equal(0, Assert.Single(batch.Samples.ToArray()).Value);
        Assert.NotEqual(generation, batch.Generation);
        Assert.Equal(0, batch.RegisteredCount);
        Assert.Equal(0, batch.ScheduledCount);
        Assert.Throws<ArgumentException>(() => batch.Start(replacement));
        var fresh = batch.Register(Binding(), Clip());
        batch.Start(fresh);
        batch.Advance(1, .25, 0);
        Assert.Equal(.25, batch.Read(fresh).Value);
    }

    [Fact]
    public void PulseCompletionAndPropertyRangesAreExact()
    {
        var batch = new AnimationBatch(1);
        var pulse = batch.Register(Binding(property: AnimationProperty.Opacity),
            Clip(repeat: AnimationRepeat.Once, curve: AnimationCurve.SinePulse));
        batch.Start(pulse);
        batch.Advance(1, .5, 0);
        Assert.Equal(1, batch.Read(pulse).Value);
        batch.Advance(2, 1, 0);
        Assert.Equal(0, batch.Read(pulse).Value);
        batch.Remove(pulse);
        Assert.Throws<ArgumentException>(() => batch.Register(Binding(property: AnimationProperty.UniformScale), Clip()));
        Assert.Throws<ArgumentException>(() => batch.Register(Binding(property: AnimationProperty.Opacity),
            new(-1, 1, 1, AnimationCurve.Linear, AnimationRepeat.Once, AnimationClock.Presentation)));
        var scale = batch.Register(Binding(property: AnimationProperty.UniformScale),
            new(1, 2, 1, AnimationCurve.SmoothStep, AnimationRepeat.Once, AnimationClock.Presentation));
        batch.Start(scale);
        batch.Advance(3, 2, 0);
        Assert.Equal(2, batch.Read(scale).Value);
    }

    [Fact]
    public void InvalidInputsDoNotChangeExistingPlaybackOrOutput()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimationBatch(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimationTargetId(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimationGeneration(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimationDefinition(0, 1, 1, (AnimationCurve)99, AnimationRepeat.Once, AnimationClock.Presentation));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimationDefinition(0, 1, 1, AnimationCurve.Linear, (AnimationRepeat)99, AnimationClock.Presentation));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimationDefinition(0, 1, 1, AnimationCurve.Linear, AnimationRepeat.Once, (AnimationClock)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimationDefinition(0, 1, double.MaxValue, AnimationCurve.Linear, AnimationRepeat.PingPong, AnimationClock.Presentation));
        var batch = new AnimationBatch(1);
        Assert.Throws<ArgumentOutOfRangeException>(() => batch.Register(Binding(property: (AnimationProperty)99), Clip()));
        Assert.Throws<ArgumentNullException>(() => batch.Register(Binding(), null!));
        var handle = batch.Register(Binding(), Clip());
        batch.Start(handle);
        batch.Advance(1, .5, .25);
        var before = batch.Read(handle);
        var output = batch.Samples.ToArray();
        Assert.Throws<ArgumentOutOfRangeException>(() => batch.Advance(1, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => batch.Advance(2, .25, .25));
        Assert.Throws<ArgumentOutOfRangeException>(() => batch.Advance(2, .5, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => batch.Stop(handle, (AnimationStop)99, .5));
        Assert.Throws<ArgumentException>(() => batch.Remove(default));
        Assert.Equal(before, batch.Read(handle));
        Assert.Equal(output, batch.Samples.ToArray());
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void NonfiniteEndpointsDurationsAndClocksReject(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimationDefinition(value, 1, 1, AnimationCurve.Linear, AnimationRepeat.Once, AnimationClock.Presentation));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimationDefinition(0, value, 1, AnimationCurve.Linear, AnimationRepeat.Once, AnimationClock.Presentation));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimationDefinition(0, 1, value, AnimationCurve.Linear, AnimationRepeat.Once, AnimationClock.Presentation));
        var batch = new AnimationBatch(1);
        Assert.Throws<ArgumentOutOfRangeException>(() => batch.Advance(1, value, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => batch.Advance(1, 0, value));
    }

    [Fact]
    public void StableTopologyFrameLoopAllocatesNoManagedMemory()
    {
        var batch = new AnimationBatch(32);
        var definition = Clip();
        for (var i = 0; i < 32; i++) batch.Start(batch.Register(Binding(i), definition));
        for (ulong frame = 1; frame <= 1000; frame++) batch.Advance(frame, frame / 60.0, 0);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (ulong frame = 1001; frame <= 3000; frame++) batch.Advance(frame, frame / 60.0, 0);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
        Assert.Equal(32, batch.ScheduledCount);
    }

    [Theory]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(90)]
    [InlineData(120)]
    public void CosmeticCadenceDoesNotChangeAuthoritativeMotion(int renderRate)
    {
        static PhysicsBodySnapshot Run(int framesPerSecond)
        {
            var body = new PhysicsBody(new(0), PhysicsMotionType.Dynamic, RigidPose.At(new(0, 4, 0)),
                new(1, 0, 0), default, 1, new(1, 1, 1));
            var world = new PhysicsWorld([], [new(body, new([new(new ConvexSphere(.1), AffineTransform.Identity)]), new(0, 0, 0))],
                [], new(new(0, -9.81, 0)));
            var animations = new AnimationBatch(1);
            animations.Start(animations.Register(Binding(), Clip()));
            ulong frame = 0;
            for (var tick = 1; tick <= 120; tick++)
            {
                world.Step([], [], 1.0 / 120);
                if (framesPerSecond > 0)
                    while ((frame + 1) / (double)framesPerSecond <= tick / 120.0)
                        animations.Advance(++frame, frame / (double)framesPerSecond, world.Time);
            }
            return body.Snapshot();
        }
        Assert.Equal(Run(0), Run(renderRate));
    }

    [Fact]
    public void ExtremeSupportedClocksRemainFiniteAndInvalidDurationsReject()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimationDefinition(0, 1, 0,
            AnimationCurve.Linear, AnimationRepeat.Once, AnimationClock.Presentation));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimationDefinition(0, 1, -1,
            AnimationCurve.Linear, AnimationRepeat.Once, AnimationClock.Presentation));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimationDefinition(-double.MaxValue, double.MaxValue, 1,
            AnimationCurve.Linear, AnimationRepeat.Once, AnimationClock.Presentation));
        var batch = new AnimationBatch(2);
        var loop = batch.Register(Binding(),
            new(0, 1, 1e-200, AnimationCurve.Linear, AnimationRepeat.Loop, AnimationClock.Presentation));
        var once = batch.Register(Binding(1),
            new(0, 1, 1e-200, AnimationCurve.SmoothStep, AnimationRepeat.Once, AnimationClock.Presentation));
        batch.Start(loop); batch.Start(once);
        batch.Advance(1, 1e300, 0);
        Assert.InRange(batch.Read(loop).Value, 0, 1);
        Assert.Equal(1, batch.Read(once).Value);
        Assert.Equal(AnimationPlayback.Completed, batch.Read(once).Playback);
    }


    [Theory]
    [InlineData(AnimationClock.Presentation)]
    [InlineData(AnimationClock.Simulation)]
    public void StopSamplesItsClockAcrossCurvesRepetitionsAndHiddenFrames(AnimationClock clock)
    {
        foreach(var curve in Enum.GetValues<AnimationCurve>())
        foreach(var repeat in Enum.GetValues<AnimationRepeat>())
        foreach(var visible in new[]{false,true})
        {
            var batch=new AnimationBatch(1);
            var definition=Clip(clock,repeat,curve);
            var handle=batch.Register(Binding(),definition);
            batch.StartAt(handle,2);
            batch.SetVisible(handle,visible);
            batch.Stop(handle,AnimationStop.Hold,3.75);
            var phase=repeat switch
            {
                AnimationRepeat.Once=>1.0,
                AnimationRepeat.Loop=>.75,
                AnimationRepeat.PingPong=>.25,
                _=>throw new ArgumentOutOfRangeException(nameof(repeat))
            };
            var expected=curve switch
            {
                AnimationCurve.Linear=>phase,
                AnimationCurve.SmoothStep=>phase*phase*phase*(phase*(phase*6-15)+10),
                AnimationCurve.SinePulse=>phase==1?0:Math.Sin(Math.PI*phase),
                _=>throw new ArgumentOutOfRangeException(nameof(curve))
            };
            Assert.InRange(Math.Abs(batch.Read(handle).Value-expected),0,1e-15);
            Assert.Equal(AnimationPlayback.Stopped,batch.Read(handle).Playback);
            batch.Advance(1,4,4);
            batch.SetVisible(handle,true);
            batch.Advance(2,5,5);
            Assert.InRange(Math.Abs(batch.Read(handle).Value-expected),0,1e-15);
            Assert.Equal(0,batch.ScheduledCount);
        }
    }

    [Fact]
    public void ControlTimesAreMonotoneAndInvalidControlsPreserveOutput()
    {
        var batch=new AnimationBatch(1);
        var handle=batch.Register(Binding(),Clip(AnimationClock.Simulation));
        batch.StartAt(handle,1);
        batch.Stop(handle,AnimationStop.Hold,1.75);
        var held=batch.Read(handle);
        foreach(var invalid in new[]{double.NaN,double.PositiveInfinity,double.NegativeInfinity,1.5})
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>batch.Stop(handle,AnimationStop.Hold,invalid));
            Assert.Throws<ArgumentOutOfRangeException>(()=>batch.StartAt(handle,invalid));
        }
        Assert.Throws<ArgumentException>(()=>batch.Advance(1,0,1.5));
        Assert.Equal(held,batch.Read(handle));
        batch.Advance(1,0,1.75);
        var output=batch.Samples.ToArray();
        Assert.Throws<ArgumentOutOfRangeException>(()=>batch.Stop(handle,AnimationStop.Hold,1.7));
        Assert.Equal(output,batch.Samples.ToArray());
        batch.Stop(handle,AnimationStop.Hold,2);
        Assert.Equal(held,batch.Read(handle)); // Repeated Hold does not advance an already stopped clip.
        batch.StartAt(handle,2);
        batch.Stop(handle,AnimationStop.RestoreInitial,2.5);
        Assert.Equal(0,batch.Read(handle).Value);
        batch.Advance(2,0,2.5);
        Assert.Equal(0,Assert.Single(batch.Samples.ToArray()).Value);
    }

    [Fact]
    public void WarmBoundaryControlsAllocateNothing()
    {
        var batch=new AnimationBatch(1);
        var handle=batch.Register(Binding(),Clip(AnimationClock.Simulation));
        for(var i=0;i<100;i++){batch.StartAt(handle,i);batch.Stop(handle,AnimationStop.Hold,i+.5);}
        var before=GC.GetAllocatedBytesForCurrentThread();
        for(var i=100;i<1100;i++){batch.StartAt(handle,i);batch.Stop(handle,AnimationStop.Hold,i+.5);}
        Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread()-before);
        Assert.Equal(.5,batch.Read(handle).Value);
    }

    [Fact]
    public void AdmissionBetweenFramesStartsAtExactClockBoundary()
    {
        var batch = new AnimationBatch(1);
        var clip = batch.Register(Binding(), Clip(AnimationClock.Simulation));
        batch.StartAt(clip, 2);
        Assert.Throws<ArgumentException>(() => batch.Advance(1, 0, 1));
        batch.Advance(1, 0, 2.25);
        Assert.Equal(.25, batch.Read(clip).Value);
        Assert.Throws<ArgumentOutOfRangeException>(() => batch.StartAt(clip, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => batch.StartAt(clip, double.NaN));
        batch.StartAt(clip, 3);
        batch.Advance(2, 0, 3);
        Assert.Equal(0, batch.Read(clip).Value);
    }
}
