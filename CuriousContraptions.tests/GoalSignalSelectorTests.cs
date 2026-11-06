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
        var before = PuzzleCatalogue.CreateSolution(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, id, new(1), WorkshopCadenceSettings.Default(), new(1), new(.45f));
        var text = File.ReadAllText(Path.Combine(twodog.Engine.ResolveProjectDir(), "content/puzzles.json"));
        var content = JsonNode.Parse(text)!.AsArray();
        var goal = content.Single(row => row!["id"]!.GetValue<string>() == id.Value)!["goal"]!.AsObject();
        var field = source ? "source_node" : "target_node";
        if (selector is null) goal.Remove(field);
        else goal[field] = selector;
        Assert.Throws<InvalidDataException>(() => PuzzleCatalogue.ReadDefinitions(RuntimeTestServices.Core, RuntimeTestServices.Bundle, content.ToJsonString()));
        Assert.Equal(before, PuzzleCatalogue.CreateSolution(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, id, new(1), WorkshopCadenceSettings.Default(), new(1), new(.45f)));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SaveRefusesMissingSelectorAndPreservesTheAuthoredGoal(bool source)
    {
        var elements = SaveFixture.Elements;
        var bench = PuzzleCatalogue.CreateSolution(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, new("delayed_signal"), new(1), WorkshopCadenceSettings.Default(), new(1), new(.45f));
        var goal = bench.Puzzle.Goal;
        Assert.Equal(new SignalNodeId("signal"), goal.SourceNode);
        Assert.Equal(new SignalNodeId("signal"), goal.TargetNode);
        var invalid = source ? goal with { SourceNode = default } : goal with { TargetNode = default };
        var forged = SaveFixture.Forged(bench with { Puzzle = bench.Puzzle with { Goal = invalid } }, 100);
        SaveFixture.Refused(WorkshopSaveRejection.OutOfRange, () => WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, elements, forged));
        var loaded = WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, elements, WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, new(bench, new(100))));
        Assert.Equal(goal, loaded.Construction.Puzzle.Goal);
        Assert.Equal(bench, loaded.Construction);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SaveRefusesAnUndeclaredLocalSelectorAsChangedAuthoredSettings(bool source)
    {
        var elements = SaveFixture.Elements;
        var bench = PuzzleCatalogue.CreateSolution(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, new("delayed_signal"), new(1), WorkshopCadenceSettings.Default(), new(1), new(.45f));
        var goal = source ? bench.Puzzle.Goal with { SourceNode = new("missing") } :
            bench.Puzzle.Goal with { TargetNode = new("missing") };
        var forged = SaveFixture.Forged(bench with { Puzzle = bench.Puzzle with { Goal = goal } }, 100);
        Assert.Equal(WorkshopEditRejection.PuzzleSettings,
            Assert.Throws<WorkshopEditRejectedException>(() => WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, elements, forged)).Reason);
        Assert.Equal(new SignalNodeId("signal"), bench.Puzzle.Goal.SourceNode);
        Assert.Equal(new SignalNodeId("signal"), bench.Puzzle.Goal.TargetNode);
    }
}
