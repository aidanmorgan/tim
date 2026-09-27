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
