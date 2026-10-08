using System.Numerics;
using System.Text.Json;

namespace CuriousContraptions.Tests;

public class ConstructionDiagnosticTests
{
    [Fact]
    public void MissingLifecycleDiscriminatorIsRejectedDuringDeserialization()
    {
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize(
            "{\"construction\":{\"parts\":[],\"connections\":[]},\"savedText\":null}",PlaytestJson.Default.PlaytestConstruction));
    }
    [Theory]
    [InlineData(false,false)]
    [InlineData(true,false)]
    [InlineData(false,true)]
    [InlineData(true,true)]
    public void AuditRequiresFullBasisForEveryPartCategory(bool locked,bool dynamic)
    {
        var part=new System.Text.Json.Nodes.JsonObject { ["locked"]=locked,["dynamic"]=dynamic };
        var state=new System.Text.Json.Nodes.JsonObject { ["parts"]=new System.Text.Json.Nodes.JsonArray(part) };
        var errors=new List<string>();
        Audit.ValidateOrientations(state,errors);
        Assert.Single(errors);
        errors.Clear();
        part["orientation"]=System.Text.Json.Nodes.JsonNode.Parse("[1,0,0,0,1,0,0,0,1]");
        Audit.ValidateOrientations(state,errors);
        Assert.Empty(errors);
        part["rotation"]=System.Text.Json.Nodes.JsonNode.Parse("[0,0,0,1]");
        Audit.ValidateOrientations(state,errors);
        Assert.Single(errors);
        errors.Clear();
        part["rotation"]=null;
        Audit.ValidateOrientations(state,errors);
        Assert.Single(errors);
    }
    [Theory]
    [InlineData(PlaytestConstructionEvent.Run,"run")]
    [InlineData(PlaytestConstructionEvent.Reset,"reset")]
    [InlineData(PlaytestConstructionEvent.Save,"save")]
    [InlineData(PlaytestConstructionEvent.Load,"load")]
    public void LifecycleEventsUseOnlyCanonicalBoundaryValues(PlaytestConstructionEvent observation,string wire)
    {
        var json=JsonSerializer.Serialize(observation);
        Assert.Equal(JsonSerializer.Serialize(wire),json);
        Assert.Equal(observation,JsonSerializer.Deserialize<PlaytestConstructionEvent>(json));
    }
    [Theory]
    [InlineData("\"Run\"")]
    [InlineData("\" run\"")]
    [InlineData("\"run, reset\"")]
    [InlineData("\"unknown\"")]
    [InlineData("0")]
    public void UnknownEventsAreRejected(string json)=>
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<PlaytestConstructionEvent>(json));

    [Fact]
    public void ExactBasisAndPersistedTextSurviveObservationSerialization()
    {
        var basis=new PartOrientation(new(1.000002f,0,0),Vector3.UnitY,Vector3.UnitZ);
        var machine=new MachineData { Parts=[new() { Id="diagnostic-body",Kind="ball",Orientation=basis }] };
        var saved=new SavedMachine { Version=SavedMachine.CurrentVersion,Machine=machine,NextId=7 };
        var text=JsonSerializer.Serialize(saved,MachineJson.Default.SavedMachine)+"\n";
        foreach(var observation in Enum.GetValues<PlaytestConstructionEvent>())
        {
            var capture=new PlaytestConstruction(observation,machine,
                observation is PlaytestConstructionEvent.Save or PlaytestConstructionEvent.Load?text:null);
            var json=JsonSerializer.Serialize(capture,PlaytestJson.Default.PlaytestConstruction);
            var decoded=JsonSerializer.Deserialize(json,PlaytestJson.Default.PlaytestConstruction)!;
            Assert.Equal(basis,Assert.Single(decoded.Construction.Parts).Orientation);
            Assert.Equal(capture.SavedText,decoded.SavedText);
            Assert.Equal(observation,decoded.Event);
        }
    }
    [Theory]
    [InlineData("{}")]
    [InlineData("{\"rotation\":[0,0,0,1]}")]
    [InlineData("{\"orientation\":[0,0,0,1]}")]
    [InlineData("{\"orientation\":[1,0,0,0,1,0,0,0,-1]}")]
    public void IncompleteObsoleteAndInvalidOrientationAreRejected(string json)=>
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<PlaytestPart>(json,PlaytestJson.Default.Options));
    [Fact]
    public void ObservationCannotInventMissingSavedText()
    {
        Assert.Throws<ArgumentException>(()=>new PlaytestConstruction(PlaytestConstructionEvent.Save,new(),null));
        Assert.Throws<ArgumentException>(()=>new PlaytestConstruction(PlaytestConstructionEvent.Run,new(),"{}"));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new PlaytestConstruction((PlaytestConstructionEvent)99,new(),null));
        Assert.Throws<JsonException>(()=>JsonSerializer.Serialize((PlaytestConstructionEvent)99));
    }
}
