using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;
using System.Text.Json;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class CampaignProgressTests(HeadlessFixture godot)
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(999)]
    public void OnlyCurrentSaveSchemaIsSupported(int version)
    {
        Assert.NotNull(godot.Tree.Root);
        var puzzles = MachineCodec.ReadPuzzles(Godot.FileAccess.GetFileAsString("res://content/puzzles.json"));
        Assert.Equal(-1, CampaignProgress.ResolveLevel(new() { Version = version, PuzzleId = puzzles[0].Id }, puzzles));
        Assert.Equal(-1, CampaignProgress.ResolveLevel(new() { Version = version }, puzzles));
    }

    [Fact]
    public void StableIdsSurviveReorderingAndUnknownIdsAreRejected()
    {
        var puzzles = new List<PuzzleData> { new() { Id = "new" }, new() { Id = "old" } };
        var saved = new SavedMachine { Version = SavedMachine.CurrentVersion, PuzzleId = "old" };
        var json = JsonSerializer.Serialize(saved, MachineJson.Default.SavedMachine);
        saved = JsonSerializer.Deserialize(json, MachineJson.Default.SavedMachine)!;
        Assert.Equal(1, CampaignProgress.ResolveLevel(saved, puzzles));
        puzzles.Reverse();
        Assert.Equal(0, CampaignProgress.ResolveLevel(saved, puzzles));
        saved.PuzzleId = "removed";
        Assert.Equal(-1, CampaignProgress.ResolveLevel(saved, puzzles));
        saved.PuzzleId = "";
        Assert.Equal(puzzles.Count, CampaignProgress.ResolveLevel(saved, puzzles));
        saved.Version = 999;
        Assert.Equal(-1, CampaignProgress.ResolveLevel(saved, puzzles));
    }
}
