using System.Text.Json;

namespace CuriousContraptions.Tests;

public class LatchDiagnosticTests
{
    [Theory]
    [InlineData(SimulationLatchPhase.Off,"\"off\"")]
    [InlineData(SimulationLatchPhase.On,"\"on\"")]
    public void CanonicalPhaseAndOwnedReadingRoundTrip(SimulationLatchPhase phase,string wire)
    {
        var options=new JsonSerializerOptions();options.Converters.Add(new PlaytestLatchPhaseConverter());
        Assert.Equal(wire,JsonSerializer.Serialize(phase,options));
        Assert.Equal(phase,JsonSerializer.Deserialize<SimulationLatchPhase>(wire,options));
        var part=PlaytestPartIdentity.FromBoundary("latch-under-test");
        var frame=new PlaytestFrame {Latches=[new()
        {
            Part=part,Id=new(2),Phase=phase,BoundaryTick=12,
            CurrentRequests=SimulationLatchRequests.Set|SimulationLatchRequests.Reset,NextRequests=SimulationLatchRequests.Set
        }]};
        var json=JsonSerializer.Serialize(frame,PlaytestJson.Default.PlaytestFrame);
        var restored=Assert.Single(JsonSerializer.Deserialize(json,PlaytestJson.Default.PlaytestFrame)!.Latches);
        Assert.Equal(part,restored.Part);Assert.Equal(new SimulationLatchId(2),restored.Id);
        Assert.Equal(phase,restored.Phase);Assert.Equal(12,restored.BoundaryTick);
        Assert.Equal(SimulationLatchRequests.Set|SimulationLatchRequests.Reset,restored.CurrentRequests);
        Assert.Equal(SimulationLatchRequests.Set,restored.NextRequests);
    }
    [Theory]
    [InlineData(SimulationLatchRequests.None,"\"none\"")]
    [InlineData(SimulationLatchRequests.Set,"\"set\"")]
    [InlineData(SimulationLatchRequests.Reset,"\"reset\"")]
    [InlineData(SimulationLatchRequests.Set|SimulationLatchRequests.Reset,"\"set_and_reset\"")]
    public void EveryRequestCombinationHasOneCanonicalMapping(SimulationLatchRequests requests,string wire)
    {
        var options=new JsonSerializerOptions();options.Converters.Add(new PlaytestLatchRequestsConverter());
        Assert.Equal(wire,JsonSerializer.Serialize(requests,options));
        Assert.Equal(requests,JsonSerializer.Deserialize<SimulationLatchRequests>(wire,options));
    }
    [Fact]
    public void UnsupportedValuesAndMalformedIdsReject()
    {
        var options=new JsonSerializerOptions();
        options.Converters.Add(new PlaytestLatchPhaseConverter());options.Converters.Add(new PlaytestLatchIdConverter());
        options.Converters.Add(new PlaytestLatchRequestsConverter());
        Assert.Throws<JsonException>(()=>JsonSerializer.Serialize((SimulationLatchPhase)99,options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<SimulationLatchPhase>("\"On\"",options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<SimulationLatchPhase>("1",options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<SimulationLatchId>("-1",options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<SimulationLatchId>("\"1\"",options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Serialize((SimulationLatchRequests)4,options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<SimulationLatchRequests>("\"Set\"",options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<SimulationLatchRequests>("3",options));
    }
}
