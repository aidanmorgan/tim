using System;
using System.Threading.Tasks;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;
using Xunit;

namespace CuriousContraptions.Tests;

public sealed partial class MechanicalLinkPresentationTests
{
    [Theory]
    [InlineData(2, 16_666_667L)]
    [InlineData(3, 25_000_000L)]
    public async Task ScheduledGeometryUsesItsActualObservationTimeThroughCodecAndClient(int ticks, long observation)
    {
        using var session = new WorkshopActivationAnimationTests.NativeClient();
        await session.Construct(Construction(Quaternion.Identity, new(0f, 4f, 0f)));
        await session.Client.Execute(WorkshopCommandKind.Run);
        await session.Advance(ticks);
        var schedule = session.Schedule;
        var pulse = RuntimeTestServices.Core.Scheduling.Due(schedule.Settings.AnimationRate, new(observation));
        session.Presentation.Advance(schedule, PhysicalTime.FromNanoseconds(
            RuntimeTestServices.Core.Scheduling.Deadline(schedule.Settings.AnimationRate, pulse).Value));
        Assert.Empty(session.Presentation.MechanicalSamples(schedule, pulse, new(1))); // Before first deadline.
        Assert.Empty(session.Presentation.MechanicalSamples(schedule, new(0), new(observation)));
        if (ticks == 3)
            Assert.Empty(session.Presentation.MechanicalSamples(schedule, pulse, new(16_666_667))); // Latest commit is ahead of lower bound.
        var sample = Assert.Single(session.Presentation.MechanicalSamples(schedule, pulse, new(observation)));
        Assert.Equal((ulong)ticks, sample.Geometry.Tick.Value);
        Assert.Equal(observation, sample.AppliedAt.Value);
        session.Client.ReceiveAnimation(RuntimeTestServices.Core.Codecs.Animation.Geometry(new(101, 206), new(1), schedule.Revision, sample));
        session.Frame();
        var physical = new WorkshopPresentationSample(default, new(observation),
            new(new((ulong)ticks * schedule.Settings.PhysicalStepsPerCommit)), PresentationQuality.Terminal);
        Assert.Equal(sample.Geometry.Tick, Assert.Single(session.Client.MechanicalFrames(session.FrameNumber, physical)).Tick);
        await session.Client.Execute(WorkshopCommandKind.Pause);
        var paused = session.Schedule;
        var pauseTime = new MasterTimeNanoseconds(100_000_000);
        var pausePulse = RuntimeTestServices.Core.Scheduling.Due(paused.Settings.AnimationRate, pauseTime);
        session.Presentation.Advance(paused, PhysicalTime.FromNanoseconds(pauseTime.Value));
        var held = Assert.Single(session.Presentation.MechanicalSamples(paused, pausePulse, pauseTime));
        Assert.Equal(paused.World.AnchorTick, held.Geometry.Tick);
        Assert.Empty(session.Presentation.MechanicalSamples(paused with { InstalledAt = new(200_000_000) }, pausePulse, pauseTime));
        Assert.Throws<ArgumentException>(() => session.Presentation.MechanicalSamples(
            paused with { World = paused.World with { Epoch = new(paused.World.Epoch.Value + 1) } }, pausePulse, pauseTime));
    }

    [Theory]
    [InlineData(16f, float.MaxValue, float.MaxValue, 11f, 9f)]
    [InlineData(16f, -float.MaxValue, float.MaxValue, 11f, 2f)]
    [InlineData(16f, float.MaxValue, 0f, 11f, 0f)]
    [InlineData(.0625f, float.Epsilon, float.Epsilon, 1f, 0f)]
    [InlineData(.0625f, -float.Epsilon, float.Epsilon, 1f, 0f)]
    [InlineData(.5f, 3f, 5f, 2f, 1.5f)]
    [InlineData(.5f, -3f, 5f, 2f, .5f)]
    [InlineData(1f, 2f, 3f, 3f, 0f)]
    [InlineData(1f, 1f, float.Epsilon, 1f, float.Epsilon)]
    public void PeriodicTravelReducesTheExactBinaryProduct(float radius, float scale, float coordinate, float length, float expected)
    {
        // Exact mathematical interface cases: MaxValue ≡9 (mod11), 16*9² ≡9; sign complement is2.
        Assert.Equal(expected, MechanicalLinkTravel.Distance(radius, scale, coordinate, length));
    }
}
