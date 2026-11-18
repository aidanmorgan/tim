using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;
using Xunit;

namespace CuriousContraptions.Tests;

public sealed class WoundSpringIntegrationTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("wind_then_release", 0f)]
    [InlineData("wind_then_release", .45f)]
    [InlineData("wind_then_release", 1f)]
    [InlineData("saved_for_later", 0f)]
    [InlineData("saved_for_later", .45f)]
    [InlineData("saved_for_later", 1f)]
    public async Task AuthoredTeachingWindsReleasesCapturesAndResets(string lesson, float assistance)
    {
        var construction = PuzzleCatalogue.CreateSolution(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle,
            new(lesson), new(2), WorkshopCadenceSettings.Default(), new(1), new(assistance));
        await using var harness = await WorkshopIntegrationHarness.Building(construction);
        var initial = Assert.Single(harness.Committed.Presentation.Retentions.ToArray());
        Assert.Equal(0f, initial.Compression);
        Assert.Null(initial.RequestResult);
        await harness.Run();
        var maximumEnergy = 0f;
        var released = false;
        var solved = false;
        var results = new HashSet<RetentionRequestResult>();
        var motor = construction.Instances.Single(instance => instance.PartId == new PartId("motor")).Id;
        ulong? unpoweredChargeAt = null;
        ulong? releasedAt = null;
        // Ten seconds is a generous stall guard; the saved lesson preserves its authored three-second release delay.
        for (var tick = 0; tick < 1200; tick++)
        {
            var advance = await harness.AdvanceOne();
            Assert.True(advance.Outcome == WorkshopCommandOutcome.Applied, $"Tick {tick}: {advance}; {string.Join("; ", harness.Simulation.Diagnostics.Snapshot())}");
            var state = Assert.Single(harness.Committed.Presentation.Retentions.ToArray());
            if (state.RequestResult is { } result) results.Add(result);
            maximumEnergy = MathF.Max(maximumEnergy, state.Potential);
            if (state.Potential > 40f && !harness.Committed.Presentation.Actuators.ToArray().Single(row => row.Owner == motor).SupplyClosed)
                unpoweredChargeAt ??= harness.Committed.Tick.Value;
            if (state.RequestResult == RetentionRequestResult.Released) releasedAt ??= harness.Committed.Tick.Value;
            released |= state.RequestResult == RetentionRequestResult.Released;
            solved |= LaunchLessonSolveTests.Solved(construction, [harness.Committed]);
        }
        output.WriteLine($"{lesson}: peak U={maximumEnergy}, released={released}, solved={solved}");
        output.WriteLine($"Request results: {string.Join(", ", results)}");

        Assert.True(maximumEnergy > 40f, "The taught motor route must wind useful charge near the 51.2 J capacity.");
        Assert.True(released);
        Assert.True(solved);
        if (lesson == "saved_for_later")
        {
            Assert.NotNull(unpoweredChargeAt);
            Assert.NotNull(releasedAt);
            Assert.True(releasedAt.Value - unpoweredChargeAt.Value >= 60, "The two-second winding window ends well before the three-second release delay.");
            var delayOwners = construction.Instances.OfType<WorkshopGenericInstance>().Where(instance => instance.PartId == new PartId("delay")).ToArray();
            var first = delayOwners.Single(instance => instance.Parameters[new("delay_seconds")] == 1f).Id;
            var delayed = delayOwners.Single(instance => instance.Parameters[new("delay_seconds")] == 3f).Id;
            var activations = Enumerable.Range(0, harness.Committed.Activations.Count).Select(index => harness.Committed.Activations[index]).ToArray();
            var from = Assert.Single(activations, value => value.Owner == first);
            var to = Assert.Single(activations, value => value.Owner == delayed);
            Assert.Equal(3f, new PhysicalTime(to.EventOrdinal, to.EventPhase).SecondsSince(new(from.EventOrdinal, from.EventPhase)));
            Assert.Equal(from.Occurrence.Cause, to.Occurrence.Cause);
        }
        var saved = new WorkshopSavedConstruction(construction, new(20));
        var loaded = WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, SaveFixture.Elements,
            WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, saved));
        Assert.Equal(saved, loaded);
        await harness.Reset();
        var reset = Assert.Single(harness.Committed.Presentation.Retentions.ToArray());
        Assert.Equal(initial, reset);
    }

    public enum MissingRoute { Supply, Belt, Release }

    [Theory]
    [InlineData("wind_then_release", MissingRoute.Supply)]
    [InlineData("wind_then_release", MissingRoute.Belt)]
    [InlineData("wind_then_release", MissingRoute.Release)]
    [InlineData("saved_for_later", MissingRoute.Supply)]
    [InlineData("saved_for_later", MissingRoute.Belt)]
    [InlineData("saved_for_later", MissingRoute.Release)]
    public async Task TeachingRequiresIndependentWindingAndReleaseRoutes(string lesson, MissingRoute missing)
    {
        var construction = PuzzleCatalogue.CreateSolution(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle,
            new(lesson), new(2), WorkshopCadenceSettings.Default(), new(1), new(.45f));
        var spring = construction.Instances.Single(instance => instance.PartId == new PartId("wound_spring")).Id;
        var battery = construction.Instances.Single(instance => instance.PartId == new PartId("battery")).Id;
        var removed = construction.Connections.Single(connection => missing switch
        {
            MissingRoute.Supply => connection.Domain == WorkshopConnectionDomain.Electrical && connection.Source == battery,
            MissingRoute.Belt => connection.Domain == WorkshopConnectionDomain.Mechanical && connection.Target == spring,
            MissingRoute.Release => connection.Domain == WorkshopConnectionDomain.Activation && connection.Target == spring,
            _ => throw new ArgumentOutOfRangeException(nameof(missing))
        });
        construction = construction with { Connections = new(construction.Connections.Where(connection => connection != removed).ToArray()) };
        await using var harness = await WorkshopIntegrationHarness.Running(construction);
        var solved = false;
        var released = false;
        for (var tick = 0; tick < 1200; tick++)
        {
            Assert.Equal(WorkshopCommandOutcome.Applied, (await harness.AdvanceOne()).Outcome);
            solved |= LaunchLessonSolveTests.Solved(construction, [harness.Committed]);
            released |= Assert.Single(harness.Committed.Presentation.Retentions.ToArray()).RequestResult == RetentionRequestResult.Released;
        }
        var retained = Assert.Single(harness.Committed.Presentation.Retentions.ToArray());
        Assert.False(solved, "Removing a taught causal route must prevent the intended capture.");
        if (missing == MissingRoute.Release)
        {
            Assert.False(released);
            Assert.True(retained.Potential > 40f, "The powered spring retains useful charge while its release route is absent.");
        }
        else Assert.InRange(retained.PositiveWindingTransfer, 0f, .005f);
        output.WriteLine($"{lesson}, missing {missing}: solved={solved}, released={released}, winding={retained.PositiveWindingTransfer}");
    }
}
