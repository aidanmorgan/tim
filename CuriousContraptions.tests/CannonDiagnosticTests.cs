using System.Text.Json;

namespace CuriousContraptions.Tests;

public class CannonDiagnosticTests
{
    [Fact]
    public void FrameRoundTripPreservesTypedCannonAndBodyEvidence()
    {
        const string CannonId="cannon",PayloadId="payload";
        var frame=new PlaytestFrame {Tick=240,
            Cannons=[new(){Id=CannonId,Phase=CannonPhase.Firing,LastShot=CannonShotResult.Fired,
                ShotCount=1,StoredEnergy=.5,ReleasedEnergy=45,PayloadId=PayloadId,RecoilOffset=-.1f}],
            Bodies=[new(){Id=PayloadId,Visible=true,Position=[0,5,0],Velocity=[0,9,0]}]};
        var json=JsonSerializer.Serialize(frame,PlaytestJson.Default.PlaytestFrame);
        var restored=JsonSerializer.Deserialize(json,PlaytestJson.Default.PlaytestFrame)!;
        Assert.Equal(240,restored.Tick);
        var cannon=Assert.Single(restored.Cannons);
        Assert.Equal(CannonPhase.Firing,cannon.Phase);
        Assert.Equal(CannonShotResult.Fired,cannon.LastShot);
        Assert.Equal(1,cannon.ShotCount);Assert.Equal(45,cannon.ReleasedEnergy);
        Assert.Equal(.5,cannon.StoredEnergy);Assert.Equal(-.1f,cannon.RecoilOffset);
        Assert.Equal(PayloadId,cannon.PayloadId);
        var body=Assert.Single(restored.Bodies);
        Assert.Equal(PayloadId,body.Id);Assert.True(body.Visible);
        Assert.Equal(new float[]{0,5,0},body.Position);Assert.Equal(new float[]{0,9,0},body.Velocity);
    }

    [Theory]
    [InlineData(CannonShotResult.None,"none")]
    [InlineData(CannonShotResult.Fired,"fired")]
    [InlineData(CannonShotResult.Empty,"empty")]
    [InlineData(CannonShotResult.Uncharged,"uncharged")]
    [InlineData(CannonShotResult.Unseated,"unseated")]
    [InlineData(CannonShotResult.Obstructed,"obstructed")]
    [InlineData(CannonShotResult.Ambiguous,"ambiguous")]
    [InlineData(CannonShotResult.Busy,"busy")]
    [InlineData(CannonShotResult.SpeedLimited,"speed_limited")]
    [InlineData(CannonShotResult.NoResponse,"no_response")]
    public void ShotBoundaryIsExact(CannonShotResult result,string wire)
    {
        var options=new JsonSerializerOptions();
        options.Converters.Add(new PlaytestCannonShotConverter());
        var json=JsonSerializer.Serialize(wire);
        Assert.Equal(json,JsonSerializer.Serialize(result,options));
        Assert.Equal(result,JsonSerializer.Deserialize<CannonShotResult>(json,options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<CannonShotResult>(
            JsonSerializer.Serialize(wire.ToUpperInvariant()),options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<CannonShotResult>(
            JsonSerializer.Serialize(" "+wire),options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Serialize((CannonShotResult)999,options));
    }

    [Theory]
    [InlineData(CannonPhase.Empty,"empty")]
    [InlineData(CannonPhase.Loading,"loading")]
    [InlineData(CannonPhase.Charging,"charging")]
    [InlineData(CannonPhase.Ready,"ready")]
    [InlineData(CannonPhase.Firing,"firing")]
    [InlineData(CannonPhase.Jammed,"jammed")]
    public void PhaseBoundaryIsExact(CannonPhase phase,string wire)
    {
        var options=new JsonSerializerOptions();
        options.Converters.Add(new PlaytestCannonPhaseConverter());
        var json=JsonSerializer.Serialize(wire);
        Assert.Equal(json,JsonSerializer.Serialize(phase,options));
        Assert.Equal(phase,JsonSerializer.Deserialize<CannonPhase>(json,options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<CannonPhase>(
            JsonSerializer.Serialize(wire.ToUpperInvariant()),options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Serialize((CannonPhase)999,options));
    }

    [Fact]
    public void DiagnosticEnumBoundariesRejectNumbersAndUnknownValues()
    {
        var options=new JsonSerializerOptions();
        options.Converters.Add(new PlaytestCannonPhaseConverter());
        options.Converters.Add(new PlaytestCannonShotConverter());
        const string NumericValue="1",UnknownValue="\"unsupported\"";
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<CannonPhase>(NumericValue,options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<CannonShotResult>(NumericValue,options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<CannonPhase>(UnknownValue,options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<CannonShotResult>(UnknownValue,options));
    }
}
