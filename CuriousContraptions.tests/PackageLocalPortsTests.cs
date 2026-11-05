using System;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class PackageLocalPortsTests
{
    private static PortDescriptor Port(PortId id, SimulationDomain domain, PortDirection direction, PortRole role, PortSignalKind? signal, Vector3? position = null) =>
        new(id, domain, direction, role, signal, position ?? Vector3.Zero, Vector3.UnitY);

    [Fact]
    public void DuplicateLocalPortIdOnSameElement_RefusesAtomically()
    {
        var duplicatePort = new PortId("pulse_out");
        var ports = new PortDescriptor[]
        {
            Port(duplicatePort, SimulationDomain.Signal, PortDirection.Output, PortRole.ActivationOut, PortSignalKind.Pulse),
            Port(duplicatePort, SimulationDomain.Signal, PortDirection.Output, PortRole.ActivationOut, PortSignalKind.Pulse)
        };

        var refusal = Assert.Throws<ElementPortRejectedException>(() =>
            ElementPortCatalogue.Declare([(new PartId("custom_emitter"), ports)]));

        Assert.Equal(ElementPortRejection.DuplicateSocket, refusal.Reason);
        Assert.Contains("declares pulse_out more than once", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DistinctElementsReusingSameLocalPortName_AcceptedCleanly()
    {
        var sharedName = new PortId("bus");
        var elemA = new PartId("element_alpha");
        var elemB = new PartId("element_beta");

        var portsA = new PortDescriptor[]
        {
            Port(sharedName, SimulationDomain.Electrical, PortDirection.Input, PortRole.PowerIn, null, new Vector3(1f, 0f, 0f))
        };
        var portsB = new PortDescriptor[]
        {
            Port(sharedName, SimulationDomain.Electrical, PortDirection.Output, PortRole.Supply, null, new Vector3(-1f, 0f, 0f))
        };

        var catalogue = ElementPortCatalogue.Declare([(elemA, portsA), (elemB, portsB)]);

        Assert.True(catalogue.Has(elemA, sharedName, WorkshopConnectionDomain.Electrical, WorkshopPortDirection.Input));
        Assert.True(catalogue.Has(elemB, sharedName, WorkshopConnectionDomain.Electrical, WorkshopPortDirection.Output));
        Assert.Equal(new Vector3(1f, 0f, 0f), catalogue.LocalPosition(elemA, sharedName));
        Assert.Equal(new Vector3(-1f, 0f, 0f), catalogue.LocalPosition(elemB, sharedName));
        Assert.Equal(PortRole.PowerIn, catalogue.Role(elemA, sharedName));
        Assert.Equal(PortRole.Supply, catalogue.Role(elemB, sharedName));
    }

    [Fact]
    public void MissingPortEndpointInConnection_RefusesAtomically()
    {
        var battery = PlacedParts.Battery(new(2), 0, 1, 0);
        var gate = WorkshopInput.Generic(new PartId("both_gate"), new(3), 3, 1, 0, 0, 0, 0, 1);
        var missingPort = new PortId("nonexistent_socket");

        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(),
            new(battery, gate),
            Connections: new(new WorkshopConnection(battery.Id, missingPort, gate.Id, PortId.PowerIn, WorkshopConnectionDomain.Electrical)));

        EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => construction.Validate());

        var missingTargetPort = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(),
            new(battery, gate),
            Connections: new(new WorkshopConnection(battery.Id, PortId.Supply, gate.Id, missingPort, WorkshopConnectionDomain.Electrical)));

        EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => missingTargetPort.Validate());
    }

    [Fact]
    public void WrongDomainAndWrongDirection_RefusesAtomically()
    {
        var battery1 = PlacedParts.Battery(new(2), 0, 1, 0);
        var battery2 = PlacedParts.Battery(new(3), 3, 1, 0);
        var gate1 = WorkshopInput.Generic(new PartId("both_gate"), new(4), 6, 1, 0, 0, 0, 0, 1);
        var gate2 = WorkshopInput.Generic(new PartId("both_gate"), new(5), 9, 1, 0, 0, 0, 0, 1);

        // Output to Output
        var outputToOutput = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(),
            new(battery1, battery2),
            Connections: new(new WorkshopConnection(battery1.Id, PortId.Supply, battery2.Id, PortId.Supply, WorkshopConnectionDomain.Electrical)));
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => outputToOutput.Validate());

        // Input to Input
        var inputToInput = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(),
            new(gate1, gate2),
            Connections: new(new WorkshopConnection(gate1.Id, PortId.PowerIn, gate2.Id, PortId.PowerIn, WorkshopConnectionDomain.Electrical)));
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => inputToInput.Validate());

        // Wrong Domain (Signal port connected via Electrical domain)
        var lamp = PlacedParts.Lamp(new(6), 12, 1, 0, 0, 0, 0, 1);
        var wrongDomain = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(),
            new(battery1, lamp),
            Connections: new(new WorkshopConnection(battery1.Id, PortId.Supply, lamp.Id, PortId.ActivationIn, WorkshopConnectionDomain.Electrical)));
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => wrongDomain.Validate());
    }

    [Fact]
    public void DuplicateFeedsFromSameSourceToSameInput_RefusesAtomically()
    {
        var customPart = new PartId("dual_source");
        var outA = new PortId("out_alpha");
        var outB = new PortId("out_beta");
        var supA = new PortId("sup_alpha");
        var supB = new PortId("sup_beta");

        var customPorts = new PortDescriptor[]
        {
            Port(outA, SimulationDomain.Signal, PortDirection.Output, PortRole.ActivationOut, PortSignalKind.Level),
            Port(outB, SimulationDomain.Signal, PortDirection.Output, PortRole.ActivationOut, PortSignalKind.Level),
            Port(supA, SimulationDomain.Electrical, PortDirection.Output, PortRole.Supply, null),
            Port(supB, SimulationDomain.Electrical, PortDirection.Output, PortRole.Supply, null),
        };

        var baseEntry = TestBundle.Entry(new PartId("switch"));
        var customEntry = baseEntry with
        {
            Metadata = baseEntry.Metadata with { Id = customPart.Value },
            Ports = customPorts
        };

        using var physicsScope = DeclaredPhysics.Scoped([customEntry]);
        using var portScope = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(customPart, customPorts)]));

        var source = WorkshopInput.Generic(customPart, new(2), 0, 1, 0, 0, 0, 0, 1);
        var lamp = PlacedParts.Lamp(new(3), 3, 1, 0, 0, 0, 0, 1);

        // Multiple activation feeds from same source to same input
        var dupActivation1 = new WorkshopConnection(source.Id, outA, lamp.Id, PortId.ActivationIn, WorkshopConnectionDomain.Activation);
        var dupActivation2 = new WorkshopConnection(source.Id, outB, lamp.Id, PortId.ActivationIn, WorkshopConnectionDomain.Activation);

        var dupActivationConstruction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(),
            new(source, lamp),
            Connections: new(dupActivation1, dupActivation2));

        var actRefusal = Assert.Throws<WorkshopEditRejectedException>(() => dupActivationConstruction.Validate());
        Assert.Equal(WorkshopEditRejection.InvalidConnection, actRefusal.Reason);
        Assert.Contains("Duplicate activation connection", actRefusal.Message, StringComparison.Ordinal);

        // Multiple electrical feeds from same source to same input
        var gate = WorkshopInput.Generic(new PartId("both_gate"), new(4), 6, 1, 0, 0, 0, 0, 1);
        var dupElec1 = new WorkshopConnection(source.Id, supA, gate.Id, PortId.FirstIn, WorkshopConnectionDomain.Electrical);
        var dupElec2 = new WorkshopConnection(source.Id, supB, gate.Id, PortId.FirstIn, WorkshopConnectionDomain.Electrical);

        var dupElecConstruction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(),
            new(source, gate),
            Connections: new(dupElec1, dupElec2));

        var elecRefusal = Assert.Throws<WorkshopEditRejectedException>(() => dupElecConstruction.Validate());
        Assert.Equal(WorkshopEditRejection.InvalidConnection, elecRefusal.Reason);
        Assert.Contains("Duplicate electrical connection", elecRefusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PortIdValidationAndBoundaries_RefusesInvalid()
    {
        // Constructor throws on null, empty, whitespace
        Assert.Throws<ArgumentNullException>(() => new PortId(null!));
        Assert.Throws<ArgumentException>(() => new PortId(""));
        Assert.Throws<ArgumentException>(() => new PortId("   "));
        Assert.Throws<ArgumentException>(() => new PortId("\t\r\n"));

        // PortIdConverter rejects empty or whitespace
        var options = new JsonSerializerOptions { Converters = { new PortIdConverter() } };
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PortId>("\"\"", options));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PortId>("\"   \"", options));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PortId>("123", options));

        // ElementPortCatalogue.Admit rejects undefined/whitespace port id and undefined role
        var refusalEmptyId = Assert.Throws<ElementPortRejectedException>(() =>
            ElementPortCatalogue.Declare([(new PartId("custom"), [new PortDescriptor(default, SimulationDomain.Signal, PortDirection.Output, PortRole.ActivationOut, PortSignalKind.Pulse, Vector3.Zero, Vector3.UnitY)])]));
        Assert.Equal(ElementPortRejection.UndefinedSocket, refusalEmptyId.Reason);
        Assert.Contains("A port needs an identity", refusalEmptyId.Message, StringComparison.Ordinal);

        var refusalUndefinedRole = Assert.Throws<ElementPortRejectedException>(() =>
            ElementPortCatalogue.Declare([(new PartId("custom"), [new PortDescriptor(new PortId("p1"), SimulationDomain.Signal, PortDirection.Output, (PortRole)999, PortSignalKind.Pulse, Vector3.Zero, Vector3.UnitY)])]));
        Assert.Equal(ElementPortRejection.UndefinedSocket, refusalUndefinedRole.Reason);
        Assert.Contains("names the undefined role 999", refusalUndefinedRole.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UnfamiliarPortNamesAndTwoOutputsSharingOneRole_ConnectCompileSaveLoadRoundtrip()
    {
        // Two outputs sharing one role (ActivationOut) and unfamiliar custom port names
        var customPart = new PartId("dual_trigger");
        var portA = new PortId("out_alpha");
        var portB = new PortId("out_beta");

        var customPorts = new PortDescriptor[]
        {
            Port(portA, SimulationDomain.Signal, PortDirection.Output, PortRole.ActivationOut, PortSignalKind.Level, new Vector3(-0.5f, 0f, 0f)),
            Port(portB, SimulationDomain.Signal, PortDirection.Output, PortRole.ActivationOut, PortSignalKind.Level, new Vector3(0.5f, 0f, 0f)),
        };

        var extendedCatalogue = TestBundle.Ports.Extend([(customPart, customPorts)]);
        Assert.Equal(2, extendedCatalogue.Of(customPart).Length);
        Assert.Equal(portA, extendedCatalogue.Of(customPart)[0].Id);
        Assert.Equal(portB, extendedCatalogue.Of(customPart)[1].Id);
        Assert.Equal(PortRole.ActivationOut, extendedCatalogue.Of(customPart)[0].Role);
        Assert.Equal(PortRole.ActivationOut, extendedCatalogue.Of(customPart)[1].Role);
        Assert.Equal(new Vector3(-0.5f, 0f, 0f), extendedCatalogue.LocalPosition(customPart, portA));
        Assert.Equal(new Vector3(0.5f, 0f, 0f), extendedCatalogue.LocalPosition(customPart, portB));

        var baseEntry = TestBundle.Entry(new PartId("switch"));
        var customEntry = baseEntry with
        {
            Metadata = baseEntry.Metadata with { Id = customPart.Value },
            Ports = customPorts
        };
        using var physicsScope = DeclaredPhysics.Scoped([customEntry]);
        using var scope = DeclaredPorts.Scoped(extendedCatalogue);

        // Connect both outputs to separate lamps and verify roundtrip through save/load
        var trigger = WorkshopInput.Generic(customPart, new(2), 0, 1, 0, 0, 0, 0, 1);
        var lamp1 = PlacedParts.Lamp(new(3), 3, 1, 0, 0, 0, 0, 1);
        var lamp2 = PlacedParts.Lamp(new(4), 6, 1, 0, 0, 0, 0, 1);

        var conn1 = new WorkshopConnection(trigger.Id, portA, lamp1.Id, PortId.ActivationIn, WorkshopConnectionDomain.Activation);
        var conn2 = new WorkshopConnection(trigger.Id, portB, lamp2.Id, PortId.ActivationIn, WorkshopConnectionDomain.Activation);

        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(),
            new(trigger, lamp1, lamp2),
            Connections: new(conn1, conn2));

        construction.Validate();

        var save = new WorkshopSavedConstruction(construction, new(10));
        var bytes = WorkshopSaveCodec.Encode(save);
        var decoded = WorkshopSaveCodec.Decode(SaveFixture.Elements.Append(customPart).ToHashSet(), bytes);

        Assert.Equal(construction.Revision, decoded.Construction.Revision);
        Assert.Equal(2, decoded.Construction.Connections.Count);
        Assert.Equal(conn1, decoded.Construction.Connections[0]);
        Assert.Equal(conn2, decoded.Construction.Connections[1]);
        Assert.Equal(portA, decoded.Construction.Connections[0].Output);
        Assert.Equal(PortId.ActivationIn, decoded.Construction.Connections[0].Input);
        Assert.Equal(portB, decoded.Construction.Connections[1].Output);
        Assert.Equal(PortId.ActivationIn, decoded.Construction.Connections[1].Input);

        var edges = WorkshopActivationCompiler.Edges(construction);
        Assert.Equal(2, edges.Count);
        Assert.Equal(new ActivationNodeId(lamp1.Id.Value), edges[0].Target);
        Assert.Equal(new ActivationNodeId(lamp2.Id.Value), edges[1].Target);
    }

    [Fact]
    public void FullRegression_ExistingElementPortsAndPuzzleConnectionsFunctionIdentically()
    {
        var battery = new PartId("battery");
        var lamp = new PartId("lamp");
        var gate = new PartId("both_gate");
        var latch = new PartId("latch");
        var counter = new PartId("counter");
        var motor = new PartId("motor");

        Assert.True(TestBundle.Ports.Has(battery, PortId.Supply, WorkshopConnectionDomain.Electrical, WorkshopPortDirection.Output));
        Assert.True(TestBundle.Ports.Has(lamp, PortId.ActivationIn, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Input));
        Assert.True(TestBundle.Ports.Has(gate, PortId.FirstIn, WorkshopConnectionDomain.Electrical, WorkshopPortDirection.Input));
        Assert.True(TestBundle.Ports.Has(gate, PortId.SecondIn, WorkshopConnectionDomain.Electrical, WorkshopPortDirection.Input));
        Assert.True(TestBundle.Ports.Has(gate, PortId.PowerIn, WorkshopConnectionDomain.Electrical, WorkshopPortDirection.Input));
        Assert.True(TestBundle.Ports.Has(gate, PortId.Supply, WorkshopConnectionDomain.Electrical, WorkshopPortDirection.Output));
        Assert.True(TestBundle.Ports.Has(latch, PortId.SetIn, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Input));
        Assert.True(TestBundle.Ports.Has(latch, PortId.ResetIn, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Input));
        Assert.True(TestBundle.Ports.Has(latch, PortId.ActivationOut, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Output));
        Assert.True(TestBundle.Ports.Has(counter, PortId.ActivationIn, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Input));
        Assert.True(TestBundle.Ports.Has(counter, PortId.ActivationOut, WorkshopConnectionDomain.Activation, WorkshopPortDirection.Output));
        Assert.True(TestBundle.Ports.Has(motor, PortId.PowerIn, WorkshopConnectionDomain.Electrical, WorkshopPortDirection.Input));

        // Placed parts connect and validate cleanly
        var b = PlacedParts.Battery(new(2), 0, 1, 0);
        var g = WorkshopInput.Generic(gate, new(3), 3, 1, 0, 0, 0, 0, 1);
        var m = WorkshopInput.Generic(motor, new(4), 6, 1, 0, 0, 0, 0, 1);
        var wire1 = new WorkshopConnection(b.Id, PortId.Supply, g.Id, PortId.FirstIn, WorkshopConnectionDomain.Electrical);
        var wire2 = new WorkshopConnection(b.Id, PortId.Supply, g.Id, PortId.SecondIn, WorkshopConnectionDomain.Electrical);
        var wire3 = new WorkshopConnection(g.Id, PortId.Supply, m.Id, PortId.PowerIn, WorkshopConnectionDomain.Electrical);

        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(),
            new(b, g, m),
            Connections: new(wire1, wire2, wire3));

        construction.Validate();
        Assert.Equal(3, construction.Connections.Count);
    }
}
