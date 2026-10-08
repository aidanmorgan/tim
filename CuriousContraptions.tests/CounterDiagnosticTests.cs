using System.Text.Json;

namespace CuriousContraptions.Tests;

public class CounterDiagnosticTests
{
    [Theory]
    [InlineData(SimulationCounterPhase.Counting,1,"\"counting\"")]
    [InlineData(SimulationCounterPhase.Reached,3,"\"reached\"")]
    public void CanonicalPhaseAndOwnedReadingRoundTrip(SimulationCounterPhase phase,int count,string wire)
    {
        var options=new JsonSerializerOptions();options.Converters.Add(new PlaytestCounterPhaseConverter());
        Assert.Equal(wire,JsonSerializer.Serialize(phase,options));
        Assert.Equal(phase,JsonSerializer.Deserialize<SimulationCounterPhase>(wire,options));
        var part=PlaytestPartIdentity.FromBoundary("counter-under-test");
        var frame=new PlaytestFrame {Counters=[new(){Part=part,Id=new(2),Phase=phase,Count=count,Target=3}]};
        var json=JsonSerializer.Serialize(frame,PlaytestJson.Default.PlaytestFrame);
        var restored=Assert.Single(JsonSerializer.Deserialize(json,PlaytestJson.Default.PlaytestFrame)!.Counters);
        Assert.Equal(part,restored.Part);Assert.Equal(new SimulationCounterId(2),restored.Id);
        Assert.Equal(phase,restored.Phase);Assert.Equal(count,restored.Count);Assert.Equal(3,restored.Target);
    }

    [Fact]
    public void UnknownPhasesAndMalformedIdsReject()
    {
        var options=new JsonSerializerOptions();
        options.Converters.Add(new PlaytestCounterPhaseConverter());options.Converters.Add(new PlaytestCounterIdConverter());
        Assert.Throws<JsonException>(()=>JsonSerializer.Serialize((SimulationCounterPhase)99,options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<SimulationCounterPhase>("\"Reached\"",options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<SimulationCounterPhase>("1",options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<SimulationCounterId>("-1",options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<SimulationCounterId>("\"1\"",options));
    }
}
