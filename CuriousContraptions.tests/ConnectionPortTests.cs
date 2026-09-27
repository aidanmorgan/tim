using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class ConnectionPortTests(HeadlessFixture godot)
{
    private static ConnectionPort Output(ConnectionDomain domain) => new(SocketId.Supply, domain, PortDirection.Output, new(1, 0, 0));
    private static ConnectionPort Input(ConnectionDomain domain) => new(SocketId.PowerIn, domain, PortDirection.Input, new(-1, 0, 0));
    private static ConnectionSpec Link(ConnectionDomain domain) => new()
    {
        From = "source", To = "target", FromPort = SocketId.Supply, ToPort = SocketId.PowerIn,
        Type = domain, RopeLength = domain == ConnectionDomain.Rope ? 1 : null
    };

    [Theory]
    [InlineData(ConnectionDomain.Electrical)]
    [InlineData(ConnectionDomain.Signal)]
    [InlineData(ConnectionDomain.Mechanical)]
    [InlineData(ConnectionDomain.Rope)]
    public void TypedSocketsRequireMatchingDomainDirectionAndIdentity(ConnectionDomain domain)
    {
        var link = Link(domain);
        var output = Output(domain);
        var input = Input(domain);
        Assert.True(ConnectionRules.TryResolve(link, [output], [input], out var resolved, out _));
        Assert.Equal(output.LocalPosition, resolved.LocalPosition);
        Assert.False(ConnectionRules.TryResolve(link, [output with { Direction = PortDirection.Input }], [input], out _, out _));
        Assert.False(ConnectionRules.TryResolve(link, [output], [input with { Direction = PortDirection.Output }], out _, out _));
        Assert.False(ConnectionRules.TryResolve(link, [output], [input with { Domain = ConnectionDomain.Activation }], out _, out _));
        Assert.False(ConnectionRules.TryResolve(link, [output, output], [input], out _, out _));
        link.FromPort = SocketId.Drive;
        Assert.False(ConnectionRules.TryResolve(link, [output], [input], out _, out _));
        link.FromPort = null;
        link.ToPort = null;
        Assert.False(ConnectionRules.TryResolve(link, [output], [input], out _, out _));
    }

    [Fact]
    public void RopeAnchorsCanBeBidirectionalWithoutCrossingDomains()
    {
        var a = Output(ConnectionDomain.Rope) with { Direction = PortDirection.Bidirectional };
        var b = Input(ConnectionDomain.Rope) with { Direction = PortDirection.Bidirectional };
        Assert.True(ConnectionRules.TryResolve(Link(ConnectionDomain.Rope), [a], [b], out _, out _));
        Assert.False(ConnectionRules.TryResolve(Link(ConnectionDomain.Signal), [a], [b], out _, out _));
    }

    [Fact]
    public void SocketIdSurviveIndependentMachineClone()
    {
        var original = new MachineData { Connections = [Link(ConnectionDomain.Electrical)] };
        var copy = MachineCodec.Clone(original);
        var edge = Assert.Single(copy.Connections);
        Assert.Equal(ConnectionDomain.Electrical, edge.Type);
        Assert.Equal(SocketId.Supply, edge.FromPort);
        Assert.Equal(SocketId.PowerIn, edge.ToPort);
        edge.FromPort = SocketId.Drive;
        Assert.Equal(SocketId.Supply, original.Connections[0].FromPort);
    }

    [Theory]
    [InlineData(ConnectionDomain.Electrical)]
    [InlineData(ConnectionDomain.Signal)]
    [InlineData(ConnectionDomain.Mechanical)]
    [InlineData(ConnectionDomain.Rope)]
    [InlineData(ConnectionDomain.Unknown)]
    public void InvalidConnectionTypesAreRejectedBeforeLoading(ConnectionDomain type)
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        try
        {
            var original = world.AddPart(new() { Id = "keep", Kind = "lamp" });
            Assert.Throws<ArgumentException>(() => world.LoadMachine(new()
            {
                Parts = [new() { Id = "source", Kind = "switch" }, new() { Id = "target", Kind = "lamp" }],
                Connections = [new() { From = "source", To = "target", Type = type }]
            }));
            Assert.Same(original, Assert.Single(world.Parts));
            Assert.Empty(world.Connections);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void EveryCataloguePartUsesDefinedUniqueSocketIdentities()
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        try
        {
            foreach (var kind in world.Registry.Definitions.Keys)
            {
                var part = world.AddPart(new() { Id = kind, Kind = kind });
                var ports = part.ConnectionPorts.ToArray();
                Assert.Equal(ports.Length, ports.Select(p => p.Id).Distinct().Count());
                Assert.All(ports, p =>
                {
                    Assert.True(Enum.IsDefined(p.Id), kind);
                    Assert.True(Enum.IsDefined(p.Domain) && p.Domain != ConnectionDomain.Unknown, kind);
                    Assert.True(Enum.IsDefined(p.Direction), kind);
                });
            }
            MechanicalNetwork.Validate(world.Parts, world.Connections);
            ElectricalNetwork.Validate(world);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void NamedSocketsRestoreAndDuplicateConnectionsAreRejected()
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        try
        {
            world.LoadMachine(new()
            {
                Parts = [new() { Id = "source", Kind = "switch" }, new() { Id = "target", Kind = "lamp" }],
                Connections = [new() { From = "source", To = "target", Type = ConnectionDomain.Activation, FromPort = SocketId.ActivationOut, ToPort = SocketId.ActivationIn }]
            });
            Assert.True(world.IsValidConnection(world.Connections[0]));
            Assert.False(world.Connect(world.FindPart("source")!, world.FindPart("target")!));
            world.Start();
            world.Activate(world.FindPart("source")!);
            Assert.True(world.FindPart("target")!.Active);
            world.Restore();
            Assert.False(world.FindPart("target")!.Active);
            Assert.Equal(SocketId.ActivationOut, world.Connections[0].FromPort);
            Assert.Equal(SocketId.ActivationIn, world.Connections[0].ToPort);
            world.RemovePart(world.FindPart("source")!);
            Assert.Empty(world.Connections);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void InvalidNamedActivationSocketDoesNotActivateAndForeignPartsCannotConnect()
    {
        var world = new MachineWorld();
        var other = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        godot.Tree.Root.AddChild(other);
        try
        {
            var a = world.AddPart(new() { Id = "source", Kind = "switch" });
            var b = world.AddPart(new() { Id = "target", Kind = "lamp" });
            var foreign = other.AddPart(new() { Id = "source", Kind = "switch" });
            Assert.False(world.Connect(foreign, b));
            world.Connections.Add(new() { From = "source", To = "target", Type = ConnectionDomain.Activation, FromPort = (SocketId)999, ToPort = SocketId.ActivationIn });
            world.Activate(a);
            Assert.False(b.Active);
            world.Connections.Clear();
            world.Activate(foreign);
            Assert.False(b.Active);
            world.Start();
            Assert.False(world.Connect(a, b)); // Topology edits are build-mode only.
        }
        finally { world.Free(); other.Free(); }
    }
}
