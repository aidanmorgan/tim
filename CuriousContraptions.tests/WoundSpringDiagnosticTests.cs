using System.Text.Json;

namespace CuriousContraptions.Tests;

public class WoundSpringDiagnosticTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(SpringTriggerResult.Released)]
    [InlineData(SpringTriggerResult.Empty)]
    [InlineData(SpringTriggerResult.AlreadyReleased)]
    public void FrameRoundTripPreservesTypedSpringEvidence(SpringTriggerResult? trigger)
    {
        const string SpringId = "launcher";
        var frame = new PlaytestFrame { Tick = 240, WoundSprings = [new()
        {
            Id = SpringId, Phase = WoundSpringPhase.Releasing, LastTrigger = trigger,
            ReleaseCount = 1, Compression = .4f, StoredEnergy = 12.8,
            AcceptedWork = 51.2, ReleasedWork = 38.4
        }] };
        var json = JsonSerializer.Serialize(frame, PlaytestJson.Default.PlaytestFrame);
        var restored = JsonSerializer.Deserialize(json, PlaytestJson.Default.PlaytestFrame)!;
        Assert.Equal(frame.Tick, restored.Tick);
        var spring = Assert.Single(restored.WoundSprings);
        Assert.Equal(SpringId, spring.Id);
        Assert.Equal(WoundSpringPhase.Releasing, spring.Phase);
        Assert.Equal(trigger, spring.LastTrigger);
        Assert.Equal(1, spring.ReleaseCount);
        Assert.Equal(.4f, spring.Compression);
        Assert.Equal(12.8, spring.StoredEnergy);
        Assert.Equal(51.2, spring.AcceptedWork);
        Assert.Equal(38.4, spring.ReleasedWork);
    }

    [Theory]
    [InlineData("\"Idle\"")]
    [InlineData("\"IDLE\"")]
    [InlineData("\" idle\"")]
    [InlineData("\"idle \"")]
    [InlineData("\"idle, winding\"")]
    [InlineData("\"0\"")]
    public void PhaseBoundaryRejectsAlternateWireNames(string json)
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new PlaytestWoundSpringPhaseConverter());
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<WoundSpringPhase>(json, options));
    }

    [Theory]
    [InlineData("\"Released\"")]
    [InlineData("\"RELEASED\"")]
    [InlineData("\" released\"")]
    [InlineData("\"released \"")]
    [InlineData("\"released, empty\"")]
    [InlineData("\"0\"")]
    public void TriggerBoundaryRejectsAlternateWireNames(string json)
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new PlaytestSpringTriggerConverter());
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<SpringTriggerResult>(json, options));
    }

    [Theory]
    [InlineData(WoundSpringPhase.Idle, "\"idle\"")]
    [InlineData(WoundSpringPhase.Winding, "\"winding\"")]
    [InlineData(WoundSpringPhase.Armed, "\"armed\"")]
    [InlineData(WoundSpringPhase.Releasing, "\"releasing\"")]
    [InlineData(WoundSpringPhase.Blocked, "\"blocked\"")]
    public void EveryPhasePreservesItsCanonicalWireValue(WoundSpringPhase phase, string json)
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new PlaytestWoundSpringPhaseConverter());
        Assert.Equal(json, JsonSerializer.Serialize(phase, options));
        Assert.Equal(phase, JsonSerializer.Deserialize<WoundSpringPhase>(json, options));
    }

    [Theory]
    [InlineData(SpringTriggerResult.Released, "\"released\"")]
    [InlineData(SpringTriggerResult.Empty, "\"empty\"")]
    [InlineData(SpringTriggerResult.AlreadyReleased, "\"already_released\"")]
    public void EveryTriggerPreservesItsCanonicalWireValue(SpringTriggerResult trigger, string json)
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new PlaytestSpringTriggerConverter());
        Assert.Equal(json, JsonSerializer.Serialize(trigger, options));
        Assert.Equal(trigger, JsonSerializer.Deserialize<SpringTriggerResult>(json, options));
    }

    [Fact]
    public void DiagnosticEnumBoundariesRejectNumbersAndUnknownValues()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new PlaytestWoundSpringPhaseConverter());
        options.Converters.Add(new PlaytestSpringTriggerConverter());
        const string NumericValue = "1", UnknownValue = "\"unsupported\"";
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<WoundSpringPhase>(NumericValue, options));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<SpringTriggerResult>(NumericValue, options));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<WoundSpringPhase>(UnknownValue, options));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<SpringTriggerResult>(UnknownValue, options));
        Assert.Throws<JsonException>(() => JsonSerializer.Serialize((WoundSpringPhase)999, options));
        Assert.Throws<JsonException>(() => JsonSerializer.Serialize((SpringTriggerResult)999, options));
    }
}
