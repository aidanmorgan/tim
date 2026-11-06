using System.Text.Json.Nodes;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class GoalSignalSelectorTests
{
    [Theory]
    [InlineData(true, null)]
    [InlineData(false, null)]
    [InlineData(true, "missing")]
    [InlineData(false, "missing")]
    public void AuthoredGoalsRequireBothExactDeclaredLocalSelectors(bool source, string? selector)
    {
        _ = SaveFixture.Elements;
        var id = new PuzzleId("delayed_signal");
        var before = PuzzleCatalogue.CreateSolution(id, new(1), WorkshopCadenceSettings.Default(), new(1), new(.45f));
        var text = File.ReadAllText(Path.Combine(twodog.Engine.ResolveProjectDir(), "content/puzzles.json"));
        var content = JsonNode.Parse(text)!.AsArray();
        var goal = content.Single(row => row!["id"]!.GetValue<string>() == id.Value)!["goal"]!.AsObject();
        var field = source ? "source_node" : "target_node";
        if (selector is null) goal.Remove(field);
        else goal[field] = selector;
        Assert.Throws<InvalidDataException>(() => PuzzleCatalogue.ReadDefinitions(content.ToJsonString()));
        Assert.Equal(before, PuzzleCatalogue.CreateSolution(id, new(1), WorkshopCadenceSettings.Default(), new(1), new(.45f)));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SaveRefusesMissingSelectorAndPreservesTheAuthoredGoal(bool source)
    {
        var elements = SaveFixture.Elements;
        var bench = PuzzleCatalogue.CreateSolution(new("delayed_signal"), new(1), WorkshopCadenceSettings.Default(), new(1), new(.45f));
        var goal = bench.Puzzle.Goal;
        Assert.Equal(new SignalNodeId("signal"), goal.SourceNode);
        Assert.Equal(new SignalNodeId("signal"), goal.TargetNode);
        var invalid = source ? goal with { SourceNode = default } : goal with { TargetNode = default };
        var forged = SaveFixture.Forged(bench with { Puzzle = bench.Puzzle with { Goal = invalid } }, 100);
        SaveFixture.Refused(WorkshopSaveRejection.OutOfRange, () => WorkshopSaveCodec.Decode(elements, forged));
        var loaded = WorkshopSaveCodec.Decode(elements, WorkshopSaveCodec.Encode(new(bench, new(100))));
        Assert.Equal(goal, loaded.Construction.Puzzle.Goal);
        Assert.Equal(bench, loaded.Construction);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SaveRefusesAnUndeclaredLocalSelectorAsChangedAuthoredSettings(bool source)
    {
        var elements = SaveFixture.Elements;
        var bench = PuzzleCatalogue.CreateSolution(new("delayed_signal"), new(1), WorkshopCadenceSettings.Default(), new(1), new(.45f));
        var goal = source ? bench.Puzzle.Goal with { SourceNode = new("missing") } :
            bench.Puzzle.Goal with { TargetNode = new("missing") };
        var forged = SaveFixture.Forged(bench with { Puzzle = bench.Puzzle with { Goal = goal } }, 100);
        Assert.Equal(WorkshopEditRejection.PuzzleSettings,
            Assert.Throws<WorkshopEditRejectedException>(() => WorkshopSaveCodec.Decode(elements, forged)).Reason);
        Assert.Equal(new SignalNodeId("signal"), bench.Puzzle.Goal.SourceNode);
        Assert.Equal(new SignalNodeId("signal"), bench.Puzzle.Goal.TargetNode);
    }
}
