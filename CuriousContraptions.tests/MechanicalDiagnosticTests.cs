using System.Text.Json;

namespace CuriousContraptions.Tests;

public class MechanicalDiagnosticTests
{
    [Fact]
    public void ReadOnlyShaftEvidenceRoundTripsTypedSocketAndFiniteAllowances()
    {
        const string PartId = "shaft";
        var frame = new PlaytestFrame { Tick = 300, Mechanical = [
            new() { Id = PartId, Port = SocketId.DriveIn, Speed = -6, Torque = 10, WorkAvailable = .125 }] };
        var json = JsonSerializer.Serialize(frame, PlaytestJson.Default.PlaytestFrame);
        var restored = JsonSerializer.Deserialize(json, PlaytestJson.Default.PlaytestFrame)!;
        var shaft = Assert.Single(restored.Mechanical);
        Assert.Equal(PartId, shaft.Id);
        Assert.Equal(SocketId.DriveIn, shaft.Port);
        Assert.Equal(-6, shaft.Speed);
        Assert.Equal(10, shaft.Torque);
        Assert.Equal(.125, shaft.WorkAvailable);
    }
}
