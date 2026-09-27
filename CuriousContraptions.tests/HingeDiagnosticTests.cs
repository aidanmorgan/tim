using System.Text.Json;

namespace CuriousContraptions.Tests;

public class HingeDiagnosticTests
{
    [Fact]
    public void FrameRoundTripPreservesTypedHingeState()
    {
        var frame = new PlaytestFrame { Tick = 120, Hinges = [new()
        {
            Id = "lever", Role = HingeRole.Beam, Limit = HingeLimit.Upper,
            Angle = .5, AngularVelocity = 0, Energy = 0
        }] };
        var json = JsonSerializer.Serialize(frame,PlaytestJson.Default.PlaytestFrame);
        var restored = JsonSerializer.Deserialize(json,PlaytestJson.Default.PlaytestFrame)!;
        var hinge = Assert.Single(restored.Hinges);
        Assert.Equal(HingeRole.Beam,hinge.Role);
        Assert.Equal(HingeLimit.Upper,hinge.Limit);
        Assert.Equal(.5,hinge.Angle);
        Assert.Equal(0,hinge.AngularVelocity);
        Assert.Equal(0,hinge.Energy);
    }

    [Fact]
    public void HingeEnumWireBoundaryUsesCanonicalNamesAndRejectsUnknownValues()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new PlaytestHingeRoleConverter());
        options.Converters.Add(new PlaytestHingeLimitConverter());
        Assert.Equal("\"beam\"",JsonSerializer.Serialize(HingeRole.Beam,options));
        Assert.Equal("\"upper\"",JsonSerializer.Serialize(HingeLimit.Upper,options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<HingeRole>("\"unsupported\"",options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<HingeLimit>("1",options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Serialize((HingeRole)99,options));
    }
}
