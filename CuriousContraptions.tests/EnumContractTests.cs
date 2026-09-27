using System.Text.Json;

namespace CuriousContraptions.Tests;

public class EnumContractTests
{
    [Theory]
    [InlineData("power")]
    [InlineData("electical")]
    [InlineData("belt")]
    public void UnknownConnectionNamesAreRejected(string type)
    {
        var json = """{"connections":[{"from":"a","to":"b","type":"TYPE","from_port":"supply","to_port":"power_in"}]}"""
            .Replace("TYPE", type);
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(json, MachineJson.Default.MachineData));
    }

    [Theory]
    [InlineData("""{"connections":[{"type":2}]}""")]
    [InlineData("""{"goals":[{"type":1}]}""")]
    [InlineData("""{"goals":[{"type":"powerd_after"}]}""")]
    public void NumericEnumsAndMisspelledGoalsAreRejected(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(json, MachineJson.Default.MachineData));
    }

    [Fact]
    public void EnumValuesRoundTripThroughTheCurrentJsonContract()
    {
        var original = new MachineData
        {
            Connections = [new() { From = "battery", To = "motor", Type = ConnectionDomain.Electrical,
                FromPort = SocketId.Supply, ToPort = SocketId.PowerIn }],
            Goals = [new() { Type = GoalKind.PoweredAfter, Target = "motor", Body = "switch" }]
        };
        var json = JsonSerializer.Serialize(original, MachineJson.Default.MachineData);
        Assert.Contains("\"electrical\"", json);
        Assert.Contains("\"powered_after\"", json);
        var copy = MachineCodec.Clone(original);
        Assert.Equal(ConnectionDomain.Electrical, copy.Connections[0].Type);
        Assert.Equal(GoalKind.PoweredAfter, copy.Goals[0].Type);
    }

    [Fact]
    public void EventIdentityIncludesKindTargetAndBody()
    {
        var captured = new MachineEvent(MachineEventKind.Captured, "basket", "ball");
        Assert.NotEqual(captured, new(MachineEventKind.Bumped, "basket", "ball"));
        Assert.NotEqual(captured, new(MachineEventKind.Captured, "other", "ball"));
        Assert.NotEqual(captured, new(MachineEventKind.Captured, "basket", "other"));
    }
}
