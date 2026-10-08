using System.Text.Json;

namespace CuriousContraptions.Tests;

public class MechanicalDiagnosticTests
{
    [Fact]
    public void OldAllowancePayloadAndMissingOwnedStateReject()
    {
        var current=new PlaytestMechanical {Id="shaft",Port=SocketId.DriveIn,Speed=1,Joint=new(10),KineticEnergy=.1};
        var json=JsonSerializer.Serialize(current,PlaytestJson.Default.Options);
        Assert.NotNull(JsonSerializer.Deserialize<PlaytestMechanical>(json,PlaytestJson.Default.Options));
        var obsolete=System.Text.Json.Nodes.JsonNode.Parse(json)!.AsObject();
        obsolete.Add("torque",10);
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<PlaytestMechanical>(obsolete.ToJsonString(),PlaytestJson.Default.Options));
        var incomplete=System.Text.Json.Nodes.JsonNode.Parse(json)!.AsObject();
        Assert.True(incomplete.Remove("joint"));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<PlaytestMechanical>(incomplete.ToJsonString(),PlaytestJson.Default.Options));
    }

    [Fact]
    public void ReadOnlyShaftEvidenceRoundTripsTypedSocketAndOwnedJointEnergy()
    {
        const string PartId = "shaft";
        var frame = new PlaytestFrame { Tick = 300, Mechanical = [
            new() { Id = PartId, Port = SocketId.DriveIn, Speed = -6, Joint = new(10), KineticEnergy = .125 }] };
        var json = JsonSerializer.Serialize(frame, PlaytestJson.Default.PlaytestFrame);
        var restored = JsonSerializer.Deserialize(json, PlaytestJson.Default.PlaytestFrame)!;
        var shaft = Assert.Single(restored.Mechanical);
        Assert.Equal(PartId, shaft.Id);
        Assert.Equal(SocketId.DriveIn, shaft.Port);
        Assert.Equal(-6, shaft.Speed);
        Assert.Equal(new CuriousContraptions.Physics.PhysicsJointId(10), shaft.Joint);
        Assert.Equal(.125, shaft.KineticEnergy);
    }
}
