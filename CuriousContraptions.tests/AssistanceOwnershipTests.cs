using Godot;
using System.Text.Json;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class AssistanceOwnershipTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId BallCatalogue=new("ball");
    [Fact]
    public void ConstructionCurveAndSavedCopyHaveIndependentOwnership()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        try
        {
            var part=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=BallCatalogue.Value});
            var knot=new PartDifficulty {Precision=.5f,TriggerThreshold=.25f};
            var authored=new List<PartDifficulty> {knot};
            part.SetDifficulty(authored);
            authored.Clear();
            Assert.Equal(knot,Assert.Single(part.Difficulty));
            var exposed=Assert.IsAssignableFrom<IList<PartDifficulty>>(part.Difficulty);
            Assert.True(exposed.IsReadOnly);
            Assert.Throws<NotSupportedException>(()=>exposed.Clear());
            var saved=part.Serialize();
            saved.Difficulty[0]=knot with {TriggerThreshold=.75f};
            Assert.Equal(.25f,part.Assistance(.5f).TriggerThreshold);
            var serialized=JsonSerializer.Serialize(saved,MachineJson.Default.PartSpec);
            var restored=JsonSerializer.Deserialize(serialized,MachineJson.Default.PartSpec)!;
            Assert.Equal(.75f,Assert.Single(restored.Difficulty).TriggerThreshold);
            part.SetDifficulty(restored.Difficulty);
            Assert.Equal(.75f,part.Assistance(.5f).TriggerThreshold);
            Assert.Throws<ArgumentNullException>(()=>part.SetDifficulty(null!));
            Assert.Equal(.75f,part.Assistance(.5f).TriggerThreshold);
        }
        finally {world.Free();}
    }
}
