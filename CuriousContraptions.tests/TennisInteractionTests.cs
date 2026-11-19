using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class TennisInteractionTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReceiverCapturesOnlyTheAlignedTennisPayload(bool aligned)
    {
        var original = TennisBallTests.Scene(TennisBallTests.Ball(x: aligned ? 0 : 2, y: 2),
            WorkshopInput.Generic(RuntimeTestServices.Bundle, new("basket"), new(2), 0, 1, 0, 0, 0, 0, 1));
        var decoded = WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply,
            RuntimeTestServices.Bundle, SaveFixture.Elements, WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply,
                RuntimeTestServices.Bundle, new WorkshopSavedConstruction(original, new(3))));
        await using var h = await WorkshopIntegrationHarness.Running(decoded.Construction);
        await h.Advance(360);
        Assert.Equal(aligned, h.Records.Any(r => r.Kind == SimulationEventKind.Captured && r.Other == 1));
        await h.Reset();
        await h.Run();
        await h.Advance(360);
        Assert.Equal(aligned, h.Records.Any(r => r.Kind == SimulationEventKind.Captured && r.Other == 1));
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public async Task TwoDefaultTennisBallsLoadThePlateWhileOneStaysBelowThreshold(int count, bool expected)
    {
        var plateY = WorkshopConstruction.WorkbenchSurface.Value + .22f;
        var parts = new List<IWorkshopInstance> { PlacedParts.Plate(new(1), 0, plateY, 0, .5f) };
        for (var i = 0; i < count; i++)
            parts.Add(TennisBallTests.Ball((ulong)i + 2, count == 1 ? 0 : i == 0 ? -.4f : .4f, plateY + .34f));
        await using var h = await WorkshopIntegrationHarness.Running(TennisBallTests.Scene(parts.ToArray()));
        var loaded = false;
        // A high-bounce payload need not share the old Basketball fixture's three-second settling time.
        // Observe the required load occurrence within the same twenty-second resting guard as the drop test.
        for (var tick = 0; tick < 2400 && !loaded; tick++)
        {
            await h.Advance(1);
            var state = Assert.Single(Enumerable.Range(0, h.Committed.Activations.Count).Select(i => h.Committed.Activations[i]),
                activation => activation.Owner == new GpuBodyId(1));
            loaded = state.Phase == ActivationPhase.Latched;
        }
        Assert.Equal(expected, loaded);
        await h.Reset();
        Assert.All(Enumerable.Range(0, h.Committed.Activations.Count), i =>
            Assert.NotEqual(ActivationPhase.Latched, h.Committed.Activations[i].Phase));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ImpactSwitchLatchesForTheTennisHitButNotAMissedDrop(bool aligned)
    {
        var parts = TennisBallTests.Scene(TennisBallTests.Ball(x: aligned ? 0 : 2, y: 3),
            WorkshopInput.Generic(RuntimeTestServices.Bundle, new("switch"), new(2), 0, 0, 0, 0, 0, 0, 1));
        await using var h = await WorkshopIntegrationHarness.Running(parts);
        await h.Advance(360);
        var state = Assert.Single(Enumerable.Range(0, h.Committed.Activations.Count).Select(i => h.Committed.Activations[i]),
            activation => activation.Owner == new GpuBodyId(2));
        Assert.Equal(aligned, state.Phase == ActivationPhase.Latched);
        if (aligned) Assert.Contains(h.Records, record => record.Kind == SimulationEventKind.TriggerActivated && record.Other == 1);
    }
}
