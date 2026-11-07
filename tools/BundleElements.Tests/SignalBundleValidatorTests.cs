using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.BundleElements.Tests;

/// <summary>Admission of local signal operations, physical references and explicit public terminals.</summary>
public sealed class SignalBundleValidatorTests
{
    private static BundleEntry Entry(string id) => BundleSchema.Read(BundleGenerator.Generate(new CuriousContraptions.Bundle.BundleServices(), TestRepository.Root).Json!).Elements.Single(entry => entry.Metadata.Id == id);
    private static IReadOnlyList<BundleError> Check(BundleEntry entry) => BundleValidator.Validate([new("Candidate", entry)]);
    private static void Refused(BundleEntry entry) => Assert.NotEmpty(Check(entry));
    private static T Node<T>(BundleEntry entry) where T : SignalNodeDescriptor => Assert.IsType<T>(Assert.Single(entry.Signal!.Nodes));
    private static BundleEntry With(BundleEntry entry, SignalNodeDescriptor node) => entry with { Signal = entry.Signal! with { Nodes = [node] } };
    private static BundleEntry Parameter(BundleEntry entry, ParameterDescriptor parameter) => entry with { Metadata = entry.Metadata with { Parameters = [parameter] } };
    private static BundleEntry AddIndicator(BundleEntry entry) => entry with
    {
        Signal = entry.Signal! with { Nodes = [.. entry.Signal.Nodes, new SignalIndicatorDescriptor(new("indicator"))] }
    };

    [Fact]
    public void MissingSignalCollectionsAndNullNodesRefuseAtBundleAdmission()
    {
        var entry = Entry("clock");
        Refused(entry with { Signal = entry.Signal! with { Nodes = null! } });
        Refused(entry with { Signal = entry.Signal! with { Edges = null! } });
        Refused(entry with { Signal = entry.Signal! with { Ports = null! } });
        Refused(entry with { Signal = entry.Signal! with { Nodes = [null!] } });
    }

    [Fact]
    public void IndicatorsAndPassagesComposeWithExplicitPortsAndRangedLocalApertures()
    {
        var lamp = Entry("lamp");
        var detector = Entry("ball_detector");
        Assert.Empty(Check(lamp));
        Assert.Empty(Check(detector));
        Assert.Equal(new SignalNodeId("signal"), Node<SignalIndicatorDescriptor>(lamp).Id);
        var passage = Node<SignalPassageDescriptor>(detector);
        Assert.Equal((.65f, .02f), (passage.ApertureRadius, passage.RearmClearance));
        Assert.Empty(Check(AddIndicator(detector)));
        Refused(lamp with { Signal = lamp.Signal! with { Ports = [new(new("unknown"), new(new("signal"), SignalTerminal.Input))] } });
        Refused(lamp with { Signal = lamp.Signal! with { Ports = [new(PortId.ActivationIn, new(new("missing"), SignalTerminal.Input))] } });
        foreach (var (radius, clearance) in new[] { (.01f, .001f), (10f, 10f) })
            Assert.Empty(Check(With(detector, passage with { ApertureRadius = radius, RearmClearance = clearance })));
        foreach (var invalid in new[] { passage with { ApertureRadius = float.NaN }, passage with { ApertureRadius = .0099f },
            passage with { ApertureRadius = 10.01f }, passage with { RearmClearance = float.PositiveInfinity },
            passage with { RearmClearance = .00099f }, passage with { RearmClearance = 10.01f },
            passage with { Frame = new(1) }, passage with { LocalPosition = new(float.NaN, 0, 0) },
            passage with { LocalRotation = default } })
            Refused(With(detector, invalid));
        Refused(detector with { Ports = [] });
        Refused(detector with { Ports = [detector.Ports[0] with { Signal = PortSignalKind.Level }] });
        Refused(detector with { Mechanical = Entry("ball").Mechanical });
    }

    [Fact]
    public void OrientationRequiresItsSelectedDynamicBodyAndDefinedAngleDomain()
    {
        var domino = Entry("domino");
        var node = Node<SignalOrientationDescriptor>(domino);
        Assert.Empty(Check(domino));
        Assert.Equal(45f, node.ThresholdDegrees);
        Assert.Empty(Check(AddIndicator(domino)));
        foreach (var degrees in new[] { 4f, 179f }) Assert.Empty(Check(With(domino, node with { ThresholdDegrees = degrees })));
        // Exact adjacent values test the declared angle domain, not motion precision.
        foreach (var degrees in new[] { MathF.BitDecrement(4f), MathF.BitIncrement(179f), float.NaN, float.PositiveInfinity })
            Refused(With(domino, node with { ThresholdDegrees = degrees }));
        Refused(With(domino, node with { Body = new(8) }));
        Refused(domino with { Mechanical = null });
        Refused(domino with { Mechanical = Entry("wall").Mechanical });
        Refused(domino with { Ports = [] });
        Refused(domino with { Ports = [domino.Ports[0] with { Signal = PortSignalKind.Level }] });
        Refused(domino with { Ports = [domino.Ports[0] with { Direction = PortDirection.Input }] });
    }

    [Fact]
    public void ContactLoadsValidateSelectedFrameFaceParameterAndGeometricDomain()
    {
        var plate = Entry("pressure_plate");
        var node = Node<SignalContactLoadDescriptor>(plate);
        Assert.Empty(Check(plate));
        Assert.Empty(Check(AddIndicator(plate)));
        Assert.Equal([1, 7], node.SupportingColliders);
        foreach (var valid in new[] { node with { MinimumAlignment = 1 }, node with { ContactDistance = 0 },
            node with { ContactDistance = .01f }, node with { DepthMargin = 0 }, node with { DepthMargin = .01f },
            node with { ReleaseHoldSeconds = 1 }, node with { LocalNormal = -Vector3.UnitX } })
            Assert.Empty(Check(With(plate, valid)));
        foreach (var invalid in new[] { node with { MinimumMassParameter = new("absent") }, node with { Frame = new(9) },
            node with { RegionCollider = -1 }, node with { RegionCollider = plate.Colliders.Count },
            node with { LocalNormal = new(0, 1.001f, 0) }, node with { LocalNormal = new(float.NaN, 1, 0) },
            node with { MinimumAlignment = 0 }, node with { MinimumAlignment = MathF.BitIncrement(1) },
            node with { ContactDistance = MathF.BitDecrement(0) }, node with { ContactDistance = MathF.BitIncrement(.01f) },
            node with { ContactDistance = float.PositiveInfinity }, node with { DepthMargin = MathF.BitIncrement(.01f) },
            node with { ReleaseHoldSeconds = 0 }, node with { ReleaseHoldSeconds = MathF.BitIncrement(1) },
            node with { ReleaseHoldSeconds = float.NaN }, node with { SupportingColliders = [] },
            node with { SupportingColliders = [1, 1] }, node with { SupportingColliders = [99] } })
            Refused(With(plate, invalid));
        var parameter = plate.Metadata.Parameters[0];
        foreach (var invalid in new[] { parameter with { Unit = ParameterUnit.Metre }, parameter with { Minimum = 0 },
            parameter with { Kind = ParameterKind.WholeNumber, Minimum = 1, Maximum = 16, Default = 1 } })
            Refused(Parameter(plate, invalid));
        foreach (var shape in new[] { plate.Colliders[1] with { ShapeKind = ColliderShapeKind.Sphere },
            plate.Colliders[1] with { Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, .1f) } })
            Refused(plate with { Colliders = plate.Colliders.Select((old, i) => i == 1 ? shape : old).ToArray() });
        Assert.Empty(Check(plate with { Mechanical = null })); // A collider-owning root without mechanical motion is static.
        Refused(plate with { Mechanical = Entry("ball").Mechanical });
        var output = plate.Ports.Single(port => port.Id == PortId.ActivationOut);
        Refused(plate with { Ports = plate.Ports.Where(port => port != output).ToArray() });
        Refused(plate with { Ports = plate.Ports.Select(port => port == output ? port with { Signal = PortSignalKind.Pulse } : port).ToArray() });
    }

    [Fact]
    public void OscillatorsSelectPositiveSecondsAndAnElectricalConsumerWithoutPhysicalRestrictions()
    {
        var clock = Entry("clock");
        var node = Node<SignalOscillatorDescriptor>(clock);
        var parameter = Assert.Single(clock.Metadata.Parameters);
        Assert.Empty(Check(clock));
        Assert.Empty(Check(AddIndicator(clock)));
        Refused(With(clock, node with { IntervalParameter = new("missing") }));
        Refused(With(clock, node with { SupplyInput = new("missing") }));
        foreach (var invalid in new[] { parameter with { Unit = ParameterUnit.Metre },
            parameter with { Kind = ParameterKind.WholeNumber }, parameter with { Minimum = 0 } })
            Refused(Parameter(clock, invalid));
        Assert.Empty(Check(Parameter(clock, parameter with { Minimum = float.Epsilon })));
        Refused(clock with { Ports = clock.Ports.Where(port => port.Id != PortId.PowerIn).ToArray() });
        var consumer = Assert.Single(clock.Electrical!.Nodes);
        Refused(clock with { Electrical = clock.Electrical with { Nodes = [consumer with { Role = Abstractions.Electrical.ElectricalRole.Switch }] } });
        foreach (var id in new[] { "wall", "ball", "powered_gate" })
        {
            var physical = Entry(id);
            var mechanical = physical.Mechanical is null ? null : physical.Mechanical with
            { Sliders = physical.Mechanical.Sliders.Select(slider => slider with { Drive = null }).ToArray() };
            Assert.Empty(Check(clock with { Mechanical = mechanical, Presentation = clock.Presentation with { Bodies = physical.Presentation.Bodies } }));
        }
        Assert.Empty(Check(clock with { Footprint = default, Colliders = [] }));
        Refused(clock with { Signal = clock.Signal! with { Nodes = [] } });
    }

    [Fact]
    public void CountTargetsUseTheUintDomainAndExactSelectedBindings()
    {
        var counter = Entry("counter");
        var node = Node<SignalPulseCountDescriptor>(counter);
        var parameter = Assert.Single(counter.Metadata.Parameters);
        Assert.Empty(Check(counter));
        Assert.Empty(Check(AddIndicator(counter)));
        Refused(With(counter, node with { TargetParameter = new("missing") }));
        foreach (var invalid in new[] { parameter with { Kind = ParameterKind.Real }, parameter with { Unit = ParameterUnit.Second },
            parameter with { Minimum = 0 }, parameter with { Maximum = 4294967296f } })
            Refused(Parameter(counter, invalid));
        foreach (var maximum in new[] { 10f, 200f, MathF.BitDecrement(4294967296f) })
            Assert.Empty(Check(Parameter(counter, parameter with { Maximum = maximum })));
        Refused(counter with { Ports = [] });
        Assert.Empty(Check(counter with { Ports = counter.Ports.Where(p => p.Domain != SimulationDomain.Electrical).ToArray(), Electrical = null }));
        Assert.Empty(Check(counter with { Footprint = default, Colliders = [] }));
        Refused(counter with { Signal = counter.Signal! with { Nodes = [] } });
        Refused(counter with { Presentation = counter.Presentation with { Bindings = [counter.Presentation.Bindings[0] with { SignalNode = new("missing") }] } });
    }

    [Fact]
    public void MemoryPortsResolveExplicitSetResetAndLevelOutputWithOptionalSelectedContact()
    {
        var latch = Entry("latch");
        Assert.Empty(Check(latch));
        Assert.Empty(Check(AddIndicator(latch)));
        var reset = latch.Ports.Single(port => port.Id == PortId.ResetIn);
        Refused(latch with { Ports = latch.Ports.Where(port => port != reset).ToArray() });
        Refused(latch with { Ports = latch.Ports.Select(port => port == reset ? port with { Signal = PortSignalKind.Level } : port).ToArray() });
        var output = latch.Ports.Single(port => port.Id == PortId.ActivationOut);
        Refused(latch with { Ports = latch.Ports.Select(port => port == output ? port with { Signal = PortSignalKind.Pulse } : port).ToArray() });
        Refused(latch with { Signal = latch.Signal! with { Ports = [.. latch.Signal.Ports, latch.Signal.Ports[0]] } });
        Refused(latch with { Signal = latch.Signal! with { Ports = [new(PortId.SetIn, new(new("signal"), SignalTerminal.Input))] } });
        Assert.Empty(Check(latch with { Ports = latch.Ports.Where(p => p.Domain != SimulationDomain.Electrical).ToArray(), Electrical = null }));
        var contact = latch.Electrical!.SupplyGraph!.Nodes.OfType<CuriousContraptions.Abstractions.Electrical.SupplyContactDescriptor>().Single();
        Refused(latch with { Electrical = latch.Electrical with { SupplyGraph = latch.Electrical.SupplyGraph with { Nodes = [contact with { Control = new(new("missing"), SignalContactState.Latched) }] } } });
        Refused(latch with { Electrical = latch.Electrical with { SupplyGraph = latch.Electrical.SupplyGraph with { Nodes = [contact with { Control = new(new("signal"), (SignalContactState)99) }] } } });
        foreach (var id in new[] { "wall", "ball", "powered_gate" })
        {
            var physical = Entry(id);
            var mechanical = physical.Mechanical! with { Sliders = physical.Mechanical!.Sliders.Select(slider => slider with { Drive = null }).ToArray() };
            Assert.Empty(Check(latch with { Mechanical = mechanical, Presentation = latch.Presentation with { Bodies = physical.Presentation.Bodies } }));
        }
    }
}
