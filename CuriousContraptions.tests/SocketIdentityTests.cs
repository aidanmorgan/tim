using System.Text.Json;
using System.Text.Json.Nodes;
using Godot;

namespace CuriousContraptions.Tests;

public class SocketIdentityTests
{
    [Theory]
    [InlineData(SocketId.ActivationOut, "activation_out")]
    [InlineData(SocketId.ActivationIn, "activation_in")]
    [InlineData(SocketId.SetIn, "set_in")]
    [InlineData(SocketId.ResetIn, "reset_in")]
    [InlineData(SocketId.FirstIn, "first_in")]
    [InlineData(SocketId.SecondIn, "second_in")]
    [InlineData(SocketId.Supply, "supply")]
    [InlineData(SocketId.PowerIn, "power_in")]
    [InlineData(SocketId.Drive, "drive")]
    [InlineData(SocketId.DriveIn, "drive_in")]
    [InlineData(SocketId.Tie, "tie")]
    public void EverySocketHasAnExactCurrentWireName(SocketId socket, string wire)
    {
        var json = JsonSerializer.Serialize(socket, MachineJson.Default.SocketId);
        Assert.Equal("\"" + wire + "\"", json);
        Assert.Equal(socket, JsonSerializer.Deserialize(json, MachineJson.Default.SocketId));
        var machine = new MachineData { Connections = [new()
        {
            From = "source", To = "target", Type = ConnectionDomain.Signal,
            FromPort = socket, ToPort = socket
        }] };
        var copy = MachineCodec.Clone(machine);
        Assert.Equal(socket, copy.Connections[0].FromPort);
        Assert.Equal(socket, copy.Connections[0].ToPort);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("-1")]
    [InlineData("1.5")]
    [InlineData("null")]
    [InlineData("true")]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("\"1\"")]
    [InlineData("\"Supply\"")]
    [InlineData("\"SUPPLY\"")]
    [InlineData("\"power\"")]
    [InlineData("\"power-in\"")]
    [InlineData("\"drive-in\"")]
    [InlineData("\"\"")]
    [InlineData("\" supply\"")]
    [InlineData("\"supply \"")]
    [InlineData("\"unknown\"")]
    public void InvalidWireIdentitiesCannotBecomeRuntimeSockets(string token)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(token, MachineJson.Default.SocketId));
        var state = JsonNode.Parse("""
            {"connections":[{"from":"source","to":"target","type":"electrical","fromPort":"supply","toPort":"power_in"}]}
            """)!;
        state["connections"]![0]!["fromPort"] = JsonNode.Parse(token);
        var errors = new List<string>();
        var gaps = new List<string>();
        Audit.CheckResetConnections(state, state.DeepClone(), errors, gaps);
        Assert.NotEmpty(errors); // Identically malformed Run and Reset must not count as proof.
        Assert.Empty(gaps);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(999)]
    [InlineData(-1)]
    public void UndefinedRuntimeValuesAreRejectedEvenWhenEndpointsMatch(int invalid)
    {
        var socket = (SocketId)invalid;
        Assert.Throws<JsonException>(() => JsonSerializer.Serialize(socket, MachineJson.Default.SocketId));
        ConnectionPort output = new(socket, ConnectionDomain.Electrical, PortDirection.Output, Vector3.Zero);
        ConnectionPort input = new(socket, ConnectionDomain.Electrical, PortDirection.Input, Vector3.Zero);
        Assert.False(ConnectionRules.TryResolve(new()
        {
            From = "source", To = "target", Type = ConnectionDomain.Electrical,
            FromPort = socket, ToPort = socket
        }, [output], [input], out _, out _));
    }
}
