using System.Text.Json;
using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;
using FileAccess = Godot.FileAccess;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class TrampolineCampaignTests(HeadlessFixture godot, ITestOutputHelper output)
{
    public enum Placement { Aimed, Flat, MissedDepth, Missing }
    private const string CampaignPath = "res://content/puzzles.json";

    [Theory]
    [InlineData(0f, Placement.Aimed)]
    [InlineData(.45f, Placement.Aimed)]
    [InlineData(1f, Placement.Aimed)]
    [InlineData(.45f, Placement.Flat)]
    [InlineData(.45f, Placement.MissedDepth)]
    [InlineData(.45f, Placement.Missing)]
    public void IntroductoryLessonRequiresAnEffectiveReboundAndResets(float precision, Placement placement)
    {
        var puzzle = MachineCodec.ReadPuzzles(FileAccess.GetFileAsString(CampaignPath))
            .Single(p => p.Solution.Count == 1 && p.Solution[0].Kind == TrampolinePart.CatalogId);
        var machine = MachineCodec.Clone(puzzle.CreateMachine());
        if (placement != Placement.Missing)
        {
            machine.Parts.AddRange(puzzle.Solution);
            var bed = machine.Parts.Single(p => p.Kind == TrampolinePart.CatalogId);
            switch (placement)
            {
                case Placement.Aimed: break;
                case Placement.Flat: bed.Rotation = [0, 0, 0]; break;
                case Placement.MissedDepth: bed.Position[2] = 2; break;
                default: throw new ArgumentOutOfRangeException(nameof(placement));
            }
        }
        Assert.Empty(machine.Connections);
        var world = new MachineWorld { Precision = precision };
        godot.Tree.Root.AddChild(world);
        try
        {
            world.LoadMachine(machine);
            var saved = JsonSerializer.Serialize(world.Snapshot(), MachineJson.Default.MachineData);
            world.Start();
            for (var tick = 0; tick < 1200 && world.Running; tick++) world.Step();
            output.WriteLine($"precision={precision}, placement={placement}, won={world.Won}, tick={world.Ticks}");
            Assert.Equal(placement == Placement.Aimed, world.Won);
            if (placement != Placement.Missing)
            {
                var bed = Assert.Single(world.Parts.OfType<TrampolinePart>());
                Assert.Equal(placement != Placement.MissedDepth, bed.ImpactCount > 0);
            }
            world.Restore();
            Assert.Equal(saved, JsonSerializer.Serialize(world.Snapshot(), MachineJson.Default.MachineData));
        }
        finally { world.Free(); }
    }

    [Fact]
    public void ParameterEnumMatchesResourceNamesAndRejectsUndefinedValues()
    {
        Assert.Equal("tension", PartParameterName.Of(TrampolineParameter.Tension));
        Assert.Equal("damping_ratio", PartParameterName.Of(TrampolineParameter.DampingRatio));
        Assert.Throws<ArgumentOutOfRangeException>(() => PartParameterName.Of((TrampolineParameter)(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => PartParameterName.Of((TrampolineParameter)2));
    }
}
