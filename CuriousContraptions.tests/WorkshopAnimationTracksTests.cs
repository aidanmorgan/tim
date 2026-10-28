using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

public sealed class WorkshopAnimationTracksTests
{
    private static WorkshopAnimationControl Ui(ulong target) => new(new(target), new(0), target, 1,
        AnimationControlKind.Reveal, true, 0, 1, 1, AnimationCurve.Linear);
    private static float Read(WorkshopAnimationTracks tracks, ulong target)
    {
        for (var i = 0; i < tracks.Capacity; i++)
            if (tracks.TryRead(i, out var control, out var value) && control.Target.Value == target) return value;
        throw new InvalidOperationException("Expected registered target.");
    }

    [Fact]
    public void WorldRetirementPreservesUiIdentityAndProgressWhileRetiringOldWorldTargets()
    {
        var tracks = new WorkshopAnimationTracks();
        var ui = Ui(1);
        var world = Ui(2) with { World = new(1), Kind = AnimationControlKind.Endpoint, From = .4f, To = .4f };
        tracks.Apply(ui, new(120, 1), 0); tracks.Apply(world, new(120, 1), 0);
        tracks.Advance(1, .25);
        Assert.Equal(.25f, Read(tracks, 1)); Assert.Equal(.4f, Read(tracks, 2));
        tracks.RetireWorld(new(2));
        Assert.Throws<InvalidOperationException>(() => Read(tracks, 2));
        Assert.Throws<ArgumentException>(() => tracks.Apply(world with { Kind = AnimationControlKind.Visibility }, new(120, 1), .25));
        tracks.Apply(ui with { Sequence = 3, Kind = AnimationControlKind.Visibility }, new(120, 1), .25);
        tracks.Apply(world with { World = new(2), Generation = 2, Sequence = 4, From = .8f, To = .8f }, new(120, 1), .25);
        tracks.Advance(2, .5);
        Assert.Equal(.5f, Read(tracks, 1)); Assert.Equal(.8f, Read(tracks, 2));
        Assert.Throws<ArgumentException>(() => tracks.Apply(world with { World = new(2), Kind = AnimationControlKind.Visibility }, new(120, 1), .5));
        Assert.Equal(.5f, Read(tracks, 1)); Assert.Equal(.8f, Read(tracks, 2));
        Assert.Equal(16, tracks.Capacity);
        Assert.Equal(16, new WorkshopAnimationTracks().Capacity);
    }

    [Fact]
    public void SessionRegistryGrowsAtWatermarkAndKeepsAllRunningChannels()
    {
        var tracks = new WorkshopAnimationTracks();
        for (ulong target = 1; target <= 12; target++) tracks.Apply(Ui(target), new(120, 1), 0);
        Assert.Equal(16, tracks.Capacity);
        tracks.Apply(Ui(13), new(120, 1), 0);
        Assert.Equal(32, tracks.Capacity);
        tracks.Advance(1, .5);
        for (ulong target = 1; target <= 13; target++) Assert.Equal(.5f, Read(tracks, target));
        tracks.RetireWorld(new(3)); // All thirteen controls belong to the session, not a physical epoch.
        Assert.Equal(32, tracks.Capacity);
        tracks.Advance(2, .75);
        for (ulong target = 1; target <= 13; target++) Assert.Equal(.75f, Read(tracks, target));
        Assert.Throws<ArgumentException>(() => tracks.Apply(Ui(14) with { Kind = (AnimationControlKind)99 }, new(120, 1), .75));
        Assert.Equal(32, tracks.Capacity);
    }

    [Fact]
    public void TimerObservationUsesItsCommittedIntervalAndTerminalLevels()
    {
        var tracks = new WorkshopAnimationTracks();
        var timer = Ui(1) with { World = new(1), Kind = AnimationControlKind.TimerObservation,
            Property = AnimationProperty.ColourBlend, Timer = new(10, 130, 40, 7, AnimationTimerPhase.Counting) };
        Assert.Equal(.25f, tracks.Apply(timer, new(120, 1), 0).Value);
        tracks.Advance(1, .375); Assert.Equal(.625f, Read(tracks, 1));
        tracks.Apply(timer with { Sequence = 2, Timer = new(10, 130, 130, 7, AnimationTimerPhase.Finished) }, new(120, 1), .75);
        Assert.Equal(1, Read(tracks, 1));
        tracks.Apply(timer with { Sequence = 3, Timer = new(0, 0, 131, 0, AnimationTimerPhase.Ready) }, new(120, 1), .75);
        Assert.Equal(0, Read(tracks, 1));
    }
}
