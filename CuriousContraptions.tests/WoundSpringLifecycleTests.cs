using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class WoundSpringLifecycleTests
{
    private static WorkshopConstruction Lesson() => PuzzleCatalogue.CreateSolution(RuntimeTestServices.Core.Supply,
        RuntimeTestServices.Bundle, new("wind_then_release"), new(2), WorkshopCadenceSettings.Default(), new(1), new(.45f));
    private static PrismaticRetentionRead Retention(WorkshopIntegrationHarness harness) =>
        Assert.Single(harness.Committed.Presentation.Retentions.ToArray());

    [Fact]
    public async Task ChargedPauseStepRejectedEditResetRemovalAndReloadKeepTheirOwnership()
    {
        var construction = Lesson();
        await using var harness = await WorkshopIntegrationHarness.Building(construction);
        var initial = Retention(harness);
        await harness.Run(); await harness.Advance(180);
        Assert.True(Retention(harness).Potential > 40f);
        await harness.Require(WorkshopCommandKind.Pause);
        var charged = Retention(harness);
        var before = harness.Committed.Tick;
        Assert.Equal(WorkshopCommandOutcome.Rejected, (await harness.AdvanceOne()).Outcome);
        Assert.Equal(before, harness.Committed.Tick);
        Assert.Equal(charged, Retention(harness));
        var edit = await harness.Execute(WorkshopCommandKind.Construct, construction with { Revision = new(3) });
        Assert.Equal(WorkshopCommandOutcome.Rejected, edit.Result.Outcome);
        Assert.Equal(charged, Retention(harness));
        await harness.Require(WorkshopCommandKind.Step);
        Assert.Equal(before.Value + 1, harness.Committed.Tick.Value);
        Assert.Equal(WorkshopSimulationPhase.Paused, harness.Simulation.Phase);
        await harness.Require(WorkshopCommandKind.Resume);
        await harness.Advance(120);
        Assert.Equal(RetentionRequestResult.Released, Retention(harness).RequestResult);
        await harness.Reset();
        Assert.Equal(initial, Retention(harness));
        var spring = construction.Instances.Single(instance => instance.PartId == new PartId("wound_spring")).Id;
        var removed = construction.WithoutInstance(spring) with { Revision = new(3), Puzzle = default };
        await harness.Require(WorkshopCommandKind.Construct, removed);
        Assert.Empty(harness.Committed.Presentation.Retentions.ToArray());
        var saved = new WorkshopSavedConstruction(construction, new(20));
        var loaded = WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply,
            RuntimeTestServices.Bundle, SaveFixture.Elements,
            WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, saved));
        Assert.Equal(saved, loaded);
        await harness.Require(WorkshopCommandKind.Construct, loaded.Construction with { Revision = new(4) });
        Assert.Equal(initial, Retention(harness));
        await harness.Run(); await harness.Advance(1);
        Assert.Null(Retention(harness).RequestResult);
        Assert.Equal(RetentionMode.Retained, Retention(harness).Mode);
    }

    [Fact]
    public async Task LatchTracksEightRadiansPerSimulationSecondAcrossSkippedFramesAndReversal()
    {
        await using var harness = await WorkshopIntegrationHarness.Running(Lesson());
        var track = Assert.Single(harness.Simulation.Scene.Presentation.ToArray(),
            binding => binding.Declaration.Driver == PresentationDriver.CommittedRetentionReleased);
        var expected = 0f;
        var previousMode = Retention(harness).Mode;
        var opened = false;
        var reversed = false;
        for (var tick = 0; tick < 420; tick++)
        {
            Assert.Equal(WorkshopCommandOutcome.Applied, (await harness.AdvanceOne()).Outcome);
            expected = Math.Clamp(expected + (previousMode == RetentionMode.Releasing ? -8f : 8f) / 120f, -.65f, 0f);
            var retained = Retention(harness);
            opened |= retained.Mode == RetentionMode.Releasing;
            reversed |= previousMode == RetentionMode.Releasing && retained.Mode == RetentionMode.Retained;
            previousMode = retained.Mode;
            if (tick % 3 != 0) continue;
            var sample = Assert.Single(harness.Presentation!.Advance(new(harness.Committed.Tick.Value * 4)),
                value => value.Target == track.Target);
            // One tick of latch travel (8 rad/s ÷ 120 Hz) is the sampling granularity; it still excludes a wrong rate, wall-clock
            // motion and a phase reset, each of which departs by several ticks of travel.
            Assert.InRange(sample.Value.Number - expected, -8f / 120f, 8f / 120f);
            Assert.Equal(retained, Retention(harness)); // Cosmetic evaluation cannot write physical storage.
        }
        Assert.True(opened && reversed);
        var final = Assert.Single(harness.Presentation!.Advance(new(harness.Committed.Tick.Value * 4)),
            value => value.Target == track.Target);
        Assert.Equal(0f, final.Value.Number);
        await harness.Reset();
        var reset = Assert.Single(harness.Presentation!.Advance(default(PhysicalTime)), value => value.Target == track.Target);
        Assert.Equal(0f, reset.Value.Number);
    }
}
