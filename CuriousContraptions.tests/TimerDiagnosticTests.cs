using System.Text.Json;

namespace CuriousContraptions.Tests;

public class TimerDiagnosticTests
{
    [Theory]
    [InlineData(SimulationOscillatorPhase.Stopped,"\"stopped\"")]
    [InlineData(SimulationOscillatorPhase.Running,"\"running\"")]
    public void OscillatorBoundaryAndFrameRoundTrip(SimulationOscillatorPhase phase,string wire)
    {
        var options=new JsonSerializerOptions();
        options.Converters.Add(new PlaytestOscillatorPhaseConverter());
        options.Converters.Add(new PlaytestOscillatorIdConverter());
        Assert.Equal(wire,JsonSerializer.Serialize(phase,options));
        Assert.Equal(phase,JsonSerializer.Deserialize<SimulationOscillatorPhase>(wire,options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Serialize((SimulationOscillatorPhase)99,options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<SimulationOscillatorPhase>("\"Running\"",options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<SimulationOscillatorId>("-1",options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<SimulationOscillatorId>("\"1\"",options));
        var identity=PlaytestPartIdentity.FromBoundary("oscillator-under-test");
        var frame=new PlaytestFrame {Oscillators=[new()
        {
            Part=identity,Id=new(3),Phase=phase,DueTick=240,PulseCount=1,LastPulseTick=120,Progress=.5
        }]};
        var json=JsonSerializer.Serialize(frame,PlaytestJson.Default.PlaytestFrame);
        var restored=Assert.Single(JsonSerializer.Deserialize(json,PlaytestJson.Default.PlaytestFrame)!.Oscillators);
        Assert.Equal(identity,restored.Part);Assert.Equal(new SimulationOscillatorId(3),restored.Id);
        Assert.Equal(phase,restored.Phase);Assert.Equal(240,restored.DueTick);
        Assert.Equal(1,restored.PulseCount);Assert.Equal(120,restored.LastPulseTick);Assert.Equal(.5,restored.Progress);
    }

    [Fact]
    public void FrameRoundTripRetainsTypedTimerAndElectricalEvidence()
    {
        var part=PlaytestPartIdentity.FromBoundary("timer-under-test");
        var frame=new PlaytestFrame
        {
            Tick=20,
            Timers=[new()
            {
                Part=part,Id=new(2),Phase=SimulationTimerPhase.Counting,
                Completion=TimerCompletionPolicy.Rearm,Boundary=TimerBoundary.BeforeNetworks,
                StartedTick=10,DueTick=30,Progress=.5
            }],
            Electrical=[new(){Part=part,Port=SocketId.PowerIn,Powered=true}]
        };
        var json=JsonSerializer.Serialize(frame,PlaytestJson.Default.PlaytestFrame);
        var restored=JsonSerializer.Deserialize(json,PlaytestJson.Default.PlaytestFrame)!;
        var timer=Assert.Single(restored.Timers);
        Assert.Equal(part,timer.Part);Assert.Equal(new SimulationTimerId(2),timer.Id);
        Assert.Equal(SimulationTimerPhase.Counting,timer.Phase);
        Assert.Equal(TimerCompletionPolicy.Rearm,timer.Completion);
        Assert.Equal(TimerBoundary.BeforeNetworks,timer.Boundary);
        Assert.Equal(10,timer.StartedTick);Assert.Equal(30,timer.DueTick);Assert.Equal(.5,timer.Progress);
        var electrical=Assert.Single(restored.Electrical);
        Assert.Equal(part,electrical.Part);Assert.Equal(SocketId.PowerIn,electrical.Port);Assert.True(electrical.Powered);
    }

    [Theory]
    [InlineData(SimulationTimerPhase.Ready,"\"ready\"")]
    [InlineData(SimulationTimerPhase.Counting,"\"counting\"")]
    [InlineData(SimulationTimerPhase.Finished,"\"finished\"")]
    public void CanonicalPhaseBoundary(SimulationTimerPhase value,string wire)
    {
        var options=new JsonSerializerOptions();options.Converters.Add(new PlaytestTimerPhaseConverter());
        Assert.Equal(wire,JsonSerializer.Serialize(value,options));
        Assert.Equal(value,JsonSerializer.Deserialize<SimulationTimerPhase>(wire,options));
    }

    [Fact]
    public void UnknownPoliciesAndMalformedIdentitiesReject()
    {
        var options=new JsonSerializerOptions();
        options.Converters.Add(new PlaytestTimerPhaseConverter());
        options.Converters.Add(new PlaytestTimerCompletionConverter());
        options.Converters.Add(new PlaytestTimerBoundaryConverter());
        options.Converters.Add(new PlaytestTimerIdConverter());
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<SimulationTimerPhase>("\"Counting\"",options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Serialize((SimulationTimerPhase)99,options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<TimerCompletionPolicy>("\"restart\"",options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Serialize((TimerCompletionPolicy)99,options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<TimerBoundary>("99",options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Serialize((TimerBoundary)99,options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<SimulationTimerId>("-1",options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<SimulationTimerId>("\"1\"",options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<PlaytestPartIdentity>("\" \""));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<PlaytestPartIdentity>("1"));
        Assert.Throws<JsonException>(()=>JsonSerializer.Serialize(default(PlaytestPartIdentity)));
    }
}
