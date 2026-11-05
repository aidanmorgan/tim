using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;
using Xunit;

namespace CuriousContraptions.Tests;

/// <summary>
/// Story 9A.46e: Connect mechanical ports to declared coordinates.
/// Tests verify:
/// - Unfamiliar mixed slider/hinge package where mechanical port is on the second hinge and has a root locator.
/// - Reordering declarations keeps coordinate bindings exact.
/// - Deletion of body-index-to-hinge arithmetic and root-means-first-hinge heuristics.
/// - Controls: unsupported slider (Prismatic), missing joint index, base-body without hinges, non-mechanical port with coordinate,
///   incompatible domain/direction, self-connections, duplicate drive connections.
/// - Positive transmission: driving joint transmits motion to driven joint according to ratio and sign.
/// - Save/load and wire FlatBuffers roundtrips.
/// </summary>
public sealed class MechanicalCoordinateBindingsTests
{
    private static readonly PartId MixedDriverPart = new("unfamiliar_mixed_driver");
    private static readonly PartId DrivenPart = new("unfamiliar_driven");

    private static (BundleEntry MixedDriver, BundleEntry Driven) CreateUnfamiliarPackages()
    {
        var motorEntry = TestBundle.Entry(new PartId("motor"));
        var springEntry = TestBundle.Entry(new PartId("spring"));

        var slider = springEntry.Mechanical!.Sliders[0] with
        {
            Compression = MechanicalValue.Fixed(0f),
            Stiffness = MechanicalValue.Fixed(400f),
            Damping = MechanicalValue.Fixed(0.2f)
        };
        var hinge0 = motorEntry.Mechanical!.Hinges[0] with { Drive = null, FrictionTorque = MechanicalValue.Fixed(0f) };
        var hinge1 = motorEntry.Mechanical!.Hinges[0] with
        {
            BaseFrame = new(new Vector3(0f, 0f, 2f), Quaternion.Identity)
        };

        var mixedMechanical = new MechanicalPayload(
            KinematicMode.Static,
            motorEntry.Mechanical.MassFrame,
            PayloadPolicy.None,
            [],
            [slider],
            [hinge0, hinge1],
            null);

        var mixedEntry = motorEntry with
        {
            Metadata = motorEntry.Metadata with { Id = MixedDriverPart.Value },
            Footprint = new ElementFootprint(Bodies: 4, MovingBodies: 3, PrismaticConstraints: 1, RevoluteConstraints: 2),
            Mechanical = mixedMechanical,
            Ports =
            [
                new PortDescriptor(
                    Id: PortId.PowerIn,
                    Domain: SimulationDomain.Electrical,
                    Direction: PortDirection.Input,
                    Role: PortRole.PowerIn,
                    Signal: null,
                    LocalPosition: new Vector3(-0.55f, 0f, 0f),
                    Normal: -Vector3.UnitX),
                new PortDescriptor(
                    Id: new PortId("mech_drive"),
                    Domain: SimulationDomain.Mechanical,
                    Direction: PortDirection.Output,
                    Role: PortRole.Drive,
                    Signal: null,
                    LocalPosition: Vector3.Zero,
                    Normal: Vector3.UnitZ,
                    Body: default, // root locator: proves deletion of root-means-first-hinge convention
                    Coordinate: new CoordinateReference(CoordinateKind.Revolute, 1), // explicitly hinge index 1 (second hinge)
                    CoordinatePerRadian: 2.0f)
            ]
        };

        var unpoweredHinge = motorEntry.Mechanical!.Hinges[0] with
        {
            Drive = null, // unpowered rotor
            FrictionTorque = MechanicalValue.Fixed(0f)
        };

        var drivenMechanical = new MechanicalPayload(
            KinematicMode.Static,
            motorEntry.Mechanical.MassFrame,
            PayloadPolicy.None,
            [],
            [],
            [unpoweredHinge],
            null);

        var drivenEntry = motorEntry with
        {
            Metadata = motorEntry.Metadata with { Id = DrivenPart.Value, Parameters = [] },
            Presentation = motorEntry.Presentation with { Bindings = [] },
            Footprint = new ElementFootprint(Bodies: 2, MovingBodies: 1, PrismaticConstraints: 0, RevoluteConstraints: 1),
            Mechanical = drivenMechanical,
            Ports =
            [
                new PortDescriptor(
                    Id: new PortId("mech_in"),
                    Domain: SimulationDomain.Mechanical,
                    Direction: PortDirection.Input,
                    Role: PortRole.DriveIn,
                    Signal: null,
                    LocalPosition: Vector3.Zero,
                    Normal: Vector3.UnitZ,
                    Body: default,
                    Coordinate: new CoordinateReference(CoordinateKind.Revolute, 0),
                    CoordinatePerRadian: 4.0f)
            ]
        };

        return (mixedEntry, drivenEntry);
    }

    [Fact]
    public void BundleJson_PreservesDeclaredCoordinateAndCompilesSignedRatio()
    {
        var (authoredDriver, authoredDriven) = CreateUnfamiliarPackages();
        authoredDriven = authoredDriven with { Metadata = authoredDriven.Metadata with { PaletteOrder = 101 } };
        authoredDriver = authoredDriver with
        {
            Presentation = authoredDriver.Presentation with
            {
                Bodies = [new("Slider", new(1), Vector3.Zero), new("FirstRotor", new(2), Vector3.Zero),
                    new("SecondRotor", new(3), new(0, 0, 2))]
            },
            Ports = authoredDriver.Ports.Select(port => port.Domain == SimulationDomain.Mechanical
                ? port with { CoordinatePerRadian = -2f } : port).ToImmutableArray()
        };
        var decoded = PartRegistry.Read(BundleSchema.WriteEntries([authoredDriver, authoredDriven]));
        var mixedDriver = Assert.Single(decoded, entry => entry.Metadata.Id == MixedDriverPart.Value);
        var driven = Assert.Single(decoded, entry => entry.Metadata.Id == DrivenPart.Value);
        var endpoint = Assert.Single(mixedDriver.Ports, port => port.Domain == SimulationDomain.Mechanical);
        Assert.Equal(new CoordinateReference(CoordinateKind.Revolute, 1), endpoint.Coordinate);
        Assert.Equal(-2f, endpoint.CoordinatePerRadian);
        Assert.Equal(new CoordinateReference(CoordinateKind.Revolute, 0), Assert.Single(driven.Ports).Coordinate);
        Assert.Equal(4f, Assert.Single(driven.Ports).CoordinatePerRadian);

        using var physicsScope = DeclaredPhysics.Scoped(decoded);
        using var portScope = DeclaredPorts.Scoped(TestBundle.Ports.Extend([
            (MixedDriverPart, mixedDriver.Ports), (DrivenPart, driven.Ports)]));
        var driverInstance = WorkshopInput.Generic(MixedDriverPart, new(10), 0, 4, 0, 0, 0, 0, 1);
        var drivenInstance = WorkshopInput.Generic(DrivenPart, new(20), 4, 4, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new([driverInstance, drivenInstance]), Connections: new([new WorkshopConnection(
                driverInstance.Id, new PortId("mech_drive"), drivenInstance.Id, new PortId("mech_in"),
                WorkshopConnectionDomain.Mechanical)]));
        construction.Validate();
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        var transmission = Assert.Single(scene.Transmissions.ToArray());
        Assert.Equal(construction.Instances.Identities[driverInstance.Id].Hinges[1].Joint, transmission.DrivingJoint);
        Assert.Equal(construction.Instances.Identities[drivenInstance.Id].Hinges[0].Joint, transmission.DrivenJoint);
        // Canonical authored scales define the signed ratio exactly: -2 / 4.
        Assert.Equal(-0.5f, transmission.Ratio);
    }

    [Fact]
    public void DeclaredCoordinate_SecondHingeOnMixedSliderHingePackage_CompilesExactJoint()
    {
        var (mixedDriver, driven) = CreateUnfamiliarPackages();
        var ports = TestBundle.Ports.Extend([
            (MixedDriverPart, mixedDriver.Ports),
            (DrivenPart, driven.Ports)
        ]);

        using var physicsScope = DeclaredPhysics.Scoped([mixedDriver, driven]);
        using var portScope = DeclaredPorts.Scoped(ports);

        var driverInstance = WorkshopInput.Generic(MixedDriverPart, new(10), 0, 4, 0, 0, 0, 0, 1);
        var drivenInstance = WorkshopInput.Generic(DrivenPart, new(20), 4, 4, 0, 0, 0, 0, 1);

        var connection = new WorkshopConnection(
            driverInstance.Id,
            new PortId("mech_drive"),
            drivenInstance.Id,
            new PortId("mech_in"),
            WorkshopConnectionDomain.Mechanical);

        var construction = new WorkshopConstruction(
            new(2),
            WorkshopCadenceSettings.Default(),
            new([driverInstance, drivenInstance]),
            Connections: new([connection]));

        construction.Validate();

        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));

        var transmission = Assert.Single(scene.Transmissions.ToArray());
        Assert.Equal(new MechanicalTransmissionId(1), transmission.Id);

        // Crucial verification: DrivingJoint is the second hinge (index 1), NOT the first hinge (index 0),
        // and NOT offset by slider counts or confused by root locator Body = default!
        var identities = construction.Instances.Identities;
        var expectedDrivingJoint = identities[driverInstance.Id].Hinges[1].Joint;
        var firstHingeJoint = identities[driverInstance.Id].Hinges[0].Joint;
        var expectedDrivenJoint = identities[drivenInstance.Id].Hinges[0].Joint;

        Assert.Equal(expectedDrivingJoint, transmission.DrivingJoint);
        Assert.NotEqual(firstHingeJoint, transmission.DrivingJoint);
        Assert.Equal(expectedDrivenJoint, transmission.DrivenJoint);

        // Equal endpoint travel: source scale 2 / target scale 4 = one half.
        Assert.Equal(0.5f, transmission.Ratio);
    }

    [Fact]
    public void ReorderedDeclarations_PreservesExactCoordinateBinding()
    {
        var (mixedDriver, driven) = CreateUnfamiliarPackages();
        var springEntry = TestBundle.Entry(new PartId("spring"));
        var secondSlider = springEntry.Mechanical!.Sliders[0] with
        {
            BaseFrame = new(new Vector3(0f, 0.5f, 0f), Quaternion.Identity),
            Compression = MechanicalValue.Fixed(0f),
            Stiffness = MechanicalValue.Fixed(400f),
            Damping = MechanicalValue.Fixed(0.2f)
        };

        // Add a second slider to the package (Sliders.Count changed from 1 to 2).
        // Under deleted legacy arithmetic (Body.Value - 1 - Sliders.Length), this would have broken the index!
        var reorderedMechanical = mixedDriver.Mechanical! with
        {
            Sliders = [mixedDriver.Mechanical!.Sliders[0], secondSlider]
        };

        var reorderedDriver = mixedDriver with
        {
            Metadata = mixedDriver.Metadata with { Id = "reordered_driver" },
            Footprint = new ElementFootprint(Bodies: 5, MovingBodies: 4, PrismaticConstraints: 2, RevoluteConstraints: 2),
            Mechanical = reorderedMechanical
        };

        var reorderedPart = new PartId("reordered_driver");
        var ports = TestBundle.Ports.Extend([
            (reorderedPart, reorderedDriver.Ports),
            (DrivenPart, driven.Ports)
        ]);

        using var physicsScope = DeclaredPhysics.Scoped([reorderedDriver, driven]);
        using var portScope = DeclaredPorts.Scoped(ports);

        var driverInstance = WorkshopInput.Generic(reorderedPart, new(10), 0, 4, 0, 0, 0, 0, 1);
        var drivenInstance = WorkshopInput.Generic(DrivenPart, new(20), 4, 4, 0, 0, 0, 0, 1);

        var connection = new WorkshopConnection(
            driverInstance.Id,
            new PortId("mech_drive"),
            drivenInstance.Id,
            new PortId("mech_in"),
            WorkshopConnectionDomain.Mechanical);

        var construction = new WorkshopConstruction(
            new(2),
            WorkshopCadenceSettings.Default(),
            new([driverInstance, drivenInstance]),
            Connections: new([connection]));

        construction.Validate();

        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));

        var transmission = Assert.Single(scene.Transmissions.ToArray());
        var identities = construction.Instances.Identities;

        // Even with two sliders, Coordinate.Index = 1 binds strictly to Hinges[1].Joint!
        Assert.Equal(identities[driverInstance.Id].Hinges[1].Joint, transmission.DrivingJoint);
    }

    [Fact]
    public void ValidationControls_RejectUnsupportedSliderAndOutOfRangeJoints()
    {
        var motorEntry = TestBundle.Entry(new PartId("motor"));
        var baseEntry = TestBundle.Entry(new PartId("wall"));

        // 1. Unsupported slider coordinate (CoordinateKind.Prismatic) on mechanical port
        var prismaticPort = new PortDescriptor(
            Id: new PortId("bad_slider_mech"),
            Domain: SimulationDomain.Mechanical,
            Direction: PortDirection.Output,
            Role: PortRole.Drive,
            Signal: null,
            LocalPosition: Vector3.Zero,
            Normal: Vector3.UnitZ,
            Coordinate: new CoordinateReference(CoordinateKind.Prismatic, 0),
            CoordinatePerRadian: 1.0f);

        var badSliderEntry = motorEntry with
        {
            Metadata = motorEntry.Metadata with { Id = "bad_slider_entry" },
            Ports = [prismaticPort]
        };

        var errors1 = BundleValidator.Validate([new NamedEntry("bad_slider_entry", badSliderEntry)]);
        Assert.Contains(errors1, e => e.Detail.Contains(".Coordinate.Kind", StringComparison.Ordinal) && e.Code == BundleErrorCode.OutOfRange);

        Assert.Throws<ElementPortRejectedException>(() =>
            ElementPortCatalogue.Declare([(new PartId("bad_slider_entry"), [prismaticPort])]));

        // 2. Out-of-range hinge index (5 on an element with 1 hinge)
        var outOfRangePort = new PortDescriptor(
            Id: new PortId("out_of_range_mech"),
            Domain: SimulationDomain.Mechanical,
            Direction: PortDirection.Output,
            Role: PortRole.Drive,
            Signal: null,
            LocalPosition: Vector3.Zero,
            Normal: Vector3.UnitZ,
            Coordinate: new CoordinateReference(CoordinateKind.Revolute, 5),
            CoordinatePerRadian: 1.0f);

        var badRangeEntry = motorEntry with
        {
            Metadata = motorEntry.Metadata with { Id = "bad_range_entry" },
            Ports = [outOfRangePort]
        };

        var errors2 = BundleValidator.Validate([new NamedEntry("bad_range_entry", badRangeEntry)]);
        Assert.Contains(errors2, e => e.Detail.Contains(".Coordinate.Index", StringComparison.Ordinal) && e.Code == BundleErrorCode.OutOfRange);

        // 3. Negative hinge index (-1)
        var negativeIndexPort = outOfRangePort with
        {
            Coordinate = new CoordinateReference(CoordinateKind.Revolute, -1)
        };
        var badNegativeEntry = motorEntry with
        {
            Metadata = motorEntry.Metadata with { Id = "bad_neg_entry" },
            Ports = [negativeIndexPort]
        };
        var errors3 = BundleValidator.Validate([new NamedEntry("bad_neg_entry", badNegativeEntry)]);
        Assert.Contains(errors3, e => e.Detail.Contains(".Coordinate.Index", StringComparison.Ordinal) && e.Code == BundleErrorCode.OutOfRange);

        // 4. Base-body without hinges declaring mechanical port
        var noHingesPort = new PortDescriptor(
            Id: new PortId("no_hinges_mech"),
            Domain: SimulationDomain.Mechanical,
            Direction: PortDirection.Output,
            Role: PortRole.Drive,
            Signal: null,
            LocalPosition: Vector3.Zero,
            Normal: Vector3.UnitZ,
            Coordinate: new CoordinateReference(CoordinateKind.Revolute, 0),
            CoordinatePerRadian: 1.0f);

        var noHingesEntry = baseEntry with
        {
            Metadata = baseEntry.Metadata with { Id = "no_hinges_entry" },
            Ports = [noHingesPort]
        };

        var errors4 = BundleValidator.Validate([new NamedEntry("no_hinges_entry", noHingesEntry)]);
        Assert.Contains(errors4, e => e.Detail.Contains(".Coordinate.Index", StringComparison.Ordinal) && e.Code == BundleErrorCode.OutOfRange);

        // 5. Mechanical port missing Coordinate (null)
        var nullCoordPort = new PortDescriptor(
            Id: new PortId("null_coord_mech"),
            Domain: SimulationDomain.Mechanical,
            Direction: PortDirection.Output,
            Role: PortRole.Drive,
            Signal: null,
            LocalPosition: Vector3.Zero,
            Normal: Vector3.UnitZ,
            Coordinate: null,
            CoordinatePerRadian: 1.0f);

        var nullCoordEntry = motorEntry with
        {
            Metadata = motorEntry.Metadata with { Id = "null_coord_entry" },
            Ports = [nullCoordPort]
        };

        var errors5 = BundleValidator.Validate([new NamedEntry("null_coord_entry", nullCoordEntry)]);
        Assert.Contains(errors5, e => e.Detail.Contains(".Coordinate", StringComparison.Ordinal) && e.Code == BundleErrorCode.OutOfRange);

        Assert.Throws<ElementPortRejectedException>(() =>
            ElementPortCatalogue.Declare([(new PartId("null_coord_entry"), [nullCoordPort])]));

        // 6. Mechanical port with zero CoordinatePerRadian
        var zeroRatioPort = new PortDescriptor(
            Id: new PortId("zero_ratio_mech"),
            Domain: SimulationDomain.Mechanical,
            Direction: PortDirection.Output,
            Role: PortRole.Drive,
            Signal: null,
            LocalPosition: Vector3.Zero,
            Normal: Vector3.UnitZ,
            Coordinate: new CoordinateReference(CoordinateKind.Revolute, 0),
            CoordinatePerRadian: 0f);

        var zeroRatioEntry = motorEntry with
        {
            Metadata = motorEntry.Metadata with { Id = "zero_ratio_entry" },
            Ports = [zeroRatioPort]
        };

        var errors6 = BundleValidator.Validate([new NamedEntry("zero_ratio_entry", zeroRatioEntry)]);
        Assert.Contains(errors6, e => e.Detail.Contains(".CoordinatePerRadian", StringComparison.Ordinal) && e.Code == BundleErrorCode.OutOfRange);

        Assert.Throws<ElementPortRejectedException>(() =>
            ElementPortCatalogue.Declare([(new PartId("zero_ratio_entry"), [zeroRatioPort])]));

        // 7. Non-mechanical port declaring Coordinate or non-zero CoordinatePerRadian
        var badSignalPort = new PortDescriptor(
            Id: new PortId("bad_signal"),
            Domain: SimulationDomain.Signal,
            Direction: PortDirection.Output,
            Role: PortRole.ActivationOut,
            Signal: PortSignalKind.Pulse,
            LocalPosition: Vector3.Zero,
            Normal: Vector3.UnitZ,
            Coordinate: new CoordinateReference(CoordinateKind.Revolute, 0),
            CoordinatePerRadian: 0f);

        var badSignalEntry = motorEntry with
        {
            Metadata = motorEntry.Metadata with { Id = "bad_signal_entry" },
            Ports = [badSignalPort]
        };

        var errors7 = BundleValidator.Validate([new NamedEntry("bad_signal_entry", badSignalEntry)]);
        Assert.Contains(errors7, e => e.Detail.Contains(".Coordinate", StringComparison.Ordinal) && e.Code == BundleErrorCode.OutOfRange);

        Assert.Throws<ElementPortRejectedException>(() =>
            ElementPortCatalogue.Declare([(new PartId("bad_signal_entry"), [badSignalPort])]));
    }

    [Fact]
    public void WorkshopConnections_ValidateControls_RefuseInvalidMechanicalConnections()
    {
        var (mixedDriver, driven) = CreateUnfamiliarPackages();
        var ports = TestBundle.Ports.Extend([
            (MixedDriverPart, mixedDriver.Ports),
            (DrivenPart, driven.Ports)
        ]);

        using var physicsScope = DeclaredPhysics.Scoped([mixedDriver, driven]);
        using var portScope = DeclaredPorts.Scoped(ports);

        var driver1 = WorkshopInput.Generic(MixedDriverPart, new(10), 0, 4, 0, 0, 0, 0, 1);
        var driver2 = WorkshopInput.Generic(MixedDriverPart, new(11), 0, 8, 0, 0, 0, 0, 1);
        var driven1 = WorkshopInput.Generic(DrivenPart, new(20), 4, 4, 0, 0, 0, 0, 1);

        // 1. Direction / role mismatch: Output to Output (Drive to Drive)
        var outToOut = new WorkshopConnection(driver1.Id, new PortId("mech_drive"), driver2.Id, new PortId("mech_drive"), WorkshopConnectionDomain.Mechanical);
        var ex1 = Assert.Throws<WorkshopEditRejectedException>(() =>
            new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new([driver1, driver2]), Connections: new([outToOut])).Validate());
        Assert.Equal(WorkshopEditRejection.InvalidConnection, ex1.Reason);

        // 2. Cross-domain: Drive to Electrical PowerIn
        var crossDomain = new WorkshopConnection(driver1.Id, new PortId("mech_drive"), driver2.Id, PortId.PowerIn, WorkshopConnectionDomain.Mechanical);
        var ex2 = Assert.Throws<WorkshopEditRejectedException>(() =>
            new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new([driver1, driver2]), Connections: new([crossDomain])).Validate());
        Assert.Equal(WorkshopEditRejection.InvalidConnection, ex2.Reason);

        // 3. Self-connection: driver1 connecting to driver1
        var selfConn = new WorkshopConnection(driver1.Id, new PortId("mech_drive"), driver1.Id, PortId.PowerIn, WorkshopConnectionDomain.Mechanical);
        var ex3 = Assert.Throws<WorkshopEditRejectedException>(() =>
            new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new([driver1]), Connections: new([selfConn])).Validate());
        Assert.Equal(WorkshopEditRejection.InvalidConnection, ex3.Reason);

        // 4. Duplicate drive feeds targeting the same driven input
        var feed1 = new WorkshopConnection(driver1.Id, new PortId("mech_drive"), driven1.Id, new PortId("mech_in"), WorkshopConnectionDomain.Mechanical);
        var feed2 = new WorkshopConnection(driver2.Id, new PortId("mech_drive"), driven1.Id, new PortId("mech_in"), WorkshopConnectionDomain.Mechanical);
        var ex4 = Assert.Throws<WorkshopEditRejectedException>(() =>
            new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new([driver1, driver2, driven1]), Connections: new([feed1, feed2])).Validate());
        Assert.Equal(WorkshopEditRejection.InvalidConnection, ex4.Reason);
        Assert.Contains("Duplicate mechanical drive connection", ex4.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PositiveTransmission_DrivingJointTransmitsProportionalAngularMotion()
    {
        await using var rig = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var motorEntry = TestBundle.Entry(new PartId("motor"));
        var driverPart = new PartId("sim_motor_driver");
        var drivenPart = new PartId("sim_unpowered_driven");

        var driverPort = new PortDescriptor(
            Id: new PortId("mech_drive"),
            Domain: SimulationDomain.Mechanical,
            Direction: PortDirection.Output,
            Role: PortRole.Drive,
            Signal: null,
            LocalPosition: Vector3.Zero,
            Normal: Vector3.UnitZ,
            Coordinate: new CoordinateReference(CoordinateKind.Revolute, 0),
            CoordinatePerRadian: 1.0f);

        var drivenPort = new PortDescriptor(
            Id: new PortId("mech_in"),
            Domain: SimulationDomain.Mechanical,
            Direction: PortDirection.Input,
            Role: PortRole.DriveIn,
            Signal: null,
            LocalPosition: Vector3.Zero,
            Normal: Vector3.UnitZ,
            Coordinate: new CoordinateReference(CoordinateKind.Revolute, 0),
            CoordinatePerRadian: 2.0f); // Equal travel gives target angular speed one half of source.

        var driverEntry = motorEntry with
        {
            Metadata = motorEntry.Metadata with { Id = driverPart.Value },
            Ports = [motorEntry.Ports[0], driverPort]
        };

        var unpoweredHinge = motorEntry.Mechanical!.Hinges[0] with
        {
            Drive = null,
            FrictionTorque = MechanicalValue.Fixed(0f)
        };

        var drivenEntry = motorEntry with
        {
            Metadata = motorEntry.Metadata with { Id = drivenPart.Value, Parameters = [] },
            Presentation = motorEntry.Presentation with { Bindings = [] },
            Mechanical = motorEntry.Mechanical with { Hinges = [unpoweredHinge] },
            Ports = [drivenPort]
        };

        var ports = TestBundle.Ports.Extend([
            (driverPart, driverEntry.Ports),
            (drivenPart, drivenEntry.Ports)
        ]);

        using var physicsScope = DeclaredPhysics.Scoped([driverEntry, drivenEntry]);
        using var portScope = DeclaredPorts.Scoped(ports);

        var battery = PlacedParts.Battery(new(1), 0, 4, 0, enabled: ElectricalEnable.Enabled);
        var driver = WorkshopInput.Generic(driverPart, new(2), 3, 4, 0, 0, 0, 0, 1);
        var driven = WorkshopInput.Generic(drivenPart, new(3), 6, 4, 0, 0, 0, 0, 1);

        var powerWire = new WorkshopConnection(battery.Id, PortId.Supply, driver.Id, PortId.PowerIn, WorkshopConnectionDomain.Electrical);
        var mechWire = new WorkshopConnection(driver.Id, new PortId("mech_drive"), driven.Id, new PortId("mech_in"), WorkshopConnectionDomain.Mechanical);

        var construction = new WorkshopConstruction(
            new(2),
            WorkshopCadenceSettings.Default(),
            new([battery, driver, driven]),
            Connections: new([powerWire, mechWire]));

        construction.Validate();

        var driverRotor = construction.Instances.Identities[driver.Id].Hinges[0].Body;
        var drivenRotor = construction.Instances.Identities[driven.Id].Hinges[0].Body;

        await rig.Require(WorkshopCommandKind.Construct, construction);
        await rig.Run();
        await rig.Advance(120);
        var committed = rig.Committed;

        Assert.True(committed.Bodies.TryGet(driverRotor, out var driverBody));
        Assert.True(committed.Bodies.TryGet(drivenRotor, out var drivenBody));

        Assert.True(driverBody.AngularVelocity.Z > 1f, "Within a one-second stall guard the driver must visibly rotate.");
        // At a 0.5 m rim, 0.1 rad/s mismatch gives at most 5 cm drift over a second.
        Assert.InRange(drivenBody.AngularVelocity.Z - 0.5f * driverBody.AngularVelocity.Z, -0.1f, 0.1f);
    }

    [Fact]
    public async Task PositiveTransmission_NegativeRatioReversesAngularMotion()
    {
        await using var rig = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var motorEntry = TestBundle.Entry(new PartId("motor"));
        var driverPart = new PartId("sim_motor_driver_neg");
        var drivenPart = new PartId("sim_unpowered_driven_neg");

        var driverPort = new PortDescriptor(
            Id: new PortId("mech_drive"),
            Domain: SimulationDomain.Mechanical,
            Direction: PortDirection.Output,
            Role: PortRole.Drive,
            Signal: null,
            LocalPosition: Vector3.Zero,
            Normal: Vector3.UnitZ,
            Coordinate: new CoordinateReference(CoordinateKind.Revolute, 0),
            CoordinatePerRadian: 1.0f);

        // Negative coordinate per radian creates a negative ratio = -1.0f
        var drivenPort = new PortDescriptor(
            Id: new PortId("mech_in"),
            Domain: SimulationDomain.Mechanical,
            Direction: PortDirection.Input,
            Role: PortRole.DriveIn,
            Signal: null,
            LocalPosition: Vector3.Zero,
            Normal: Vector3.UnitZ,
            Coordinate: new CoordinateReference(CoordinateKind.Revolute, 0),
            CoordinatePerRadian: -1.0f);

        var driverEntry = motorEntry with
        {
            Metadata = motorEntry.Metadata with { Id = driverPart.Value },
            Ports = [motorEntry.Ports[0], driverPort]
        };

        var unpoweredHinge = motorEntry.Mechanical!.Hinges[0] with
        {
            Drive = null,
            FrictionTorque = MechanicalValue.Fixed(0f)
        };

        var drivenEntry = motorEntry with
        {
            Metadata = motorEntry.Metadata with { Id = drivenPart.Value, Parameters = [] },
            Presentation = motorEntry.Presentation with { Bindings = [] },
            Mechanical = motorEntry.Mechanical with { Hinges = [unpoweredHinge] },
            Ports = [drivenPort]
        };

        var ports = TestBundle.Ports.Extend([
            (driverPart, driverEntry.Ports),
            (drivenPart, drivenEntry.Ports)
        ]);

        using var physicsScope = DeclaredPhysics.Scoped([driverEntry, drivenEntry]);
        using var portScope = DeclaredPorts.Scoped(ports);

        var battery = PlacedParts.Battery(new(1), 0, 4, 0, enabled: ElectricalEnable.Enabled);
        var driver = WorkshopInput.Generic(driverPart, new(2), 3, 4, 0, 0, 0, 0, 1);
        var driven = WorkshopInput.Generic(drivenPart, new(3), 6, 4, 0, 0, 0, 0, 1);

        var powerWire = new WorkshopConnection(battery.Id, PortId.Supply, driver.Id, PortId.PowerIn, WorkshopConnectionDomain.Electrical);
        var mechWire = new WorkshopConnection(driver.Id, new PortId("mech_drive"), driven.Id, new PortId("mech_in"), WorkshopConnectionDomain.Mechanical);

        var construction = new WorkshopConstruction(
            new(2),
            WorkshopCadenceSettings.Default(),
            new([battery, driver, driven]),
            Connections: new([powerWire, mechWire]));

        construction.Validate();

        var driverRotor = construction.Instances.Identities[driver.Id].Hinges[0].Body;
        var drivenRotor = construction.Instances.Identities[driven.Id].Hinges[0].Body;

        await rig.Require(WorkshopCommandKind.Construct, construction);
        await rig.Run();
        await rig.Advance(120);
        var committed = rig.Committed;

        Assert.True(committed.Bodies.TryGet(driverRotor, out var driverBody));
        Assert.True(committed.Bodies.TryGet(drivenRotor, out var drivenBody));

        Assert.True(driverBody.AngularVelocity.Z > 1f, "Within a one-second stall guard the driver must visibly rotate.");
        Assert.True(drivenBody.AngularVelocity.Z < -1f, "A negative ratio reverses the driven rotor.");
        // Same 5 cm rim-travel allowance at radius 0.5 m over one second.
        Assert.InRange(drivenBody.AngularVelocity.Z + driverBody.AngularVelocity.Z, -0.1f, 0.1f);
    }

    [Fact]
    public void SaveLoadAndWireSerialization_PreservesMechanicalConnectionAndTransmissions()
    {
        var (mixedDriver, driven) = CreateUnfamiliarPackages();
        var ports = TestBundle.Ports.Extend([
            (MixedDriverPart, mixedDriver.Ports),
            (DrivenPart, driven.Ports)
        ]);

        using var physicsScope = DeclaredPhysics.Scoped([mixedDriver, driven]);
        using var portScope = DeclaredPorts.Scoped(ports);

        var driverInstance = WorkshopInput.Generic(MixedDriverPart, new(10), 0, 4, 0, 0, 0, 0, 1);
        var drivenInstance = WorkshopInput.Generic(DrivenPart, new(20), 4, 4, 0, 0, 0, 0, 1);

        var connection = new WorkshopConnection(
            driverInstance.Id,
            new PortId("mech_drive"),
            drivenInstance.Id,
            new PortId("mech_in"),
            WorkshopConnectionDomain.Mechanical);

        var construction = new WorkshopConstruction(
            new(2),
            WorkshopCadenceSettings.Default(),
            new([driverInstance, drivenInstance]),
            Connections: new([connection]));

        construction.Validate();

        // 1. Test WorkshopSaveCodec roundtrip
        var save = new WorkshopSavedConstruction(construction, new(42));
        var bytes = WorkshopSaveCodec.Encode(save);
        var admittedElements = SaveFixture.Elements.Concat([MixedDriverPart, DrivenPart]).ToHashSet();
        var decoded = WorkshopSaveCodec.Decode(admittedElements, bytes);

        var decodedConn = Assert.Single(decoded.Construction.Connections);
        Assert.Equal(WorkshopConnectionDomain.Mechanical, decodedConn.Domain);
        Assert.Equal(driverInstance.Id, decodedConn.Source);
        Assert.Equal(drivenInstance.Id, decodedConn.Target);
        Assert.Equal(new PortId("mech_drive"), decodedConn.Output);
        Assert.Equal(new PortId("mech_in"), decodedConn.Input);

        // 2. Test FlatBuffers wire encoding of compiled scene
        var compiled = CompiledSceneFixture.Compile(construction);
        var command = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(1), default, compiled,
            Session: new(101, 206), Cadence: new(1), Projection: new(1));
        var wireBytes = WorkshopWireFlatBuffers.EncodeCommand(command);
        var decodedCommand = WorkshopWireFlatBuffers.DecodeCommand(wireBytes);
        var decodedScene = decodedCommand.Scene!;

        var wireTrans = Assert.Single(decodedScene.Physics.Transmissions.ToArray());
        var originalTrans = Assert.Single(compiled.Physics.Transmissions.ToArray());
        Assert.Equal(originalTrans.Id, wireTrans.Id);
        Assert.Equal(originalTrans.DrivingJoint, wireTrans.DrivingJoint);
        Assert.Equal(originalTrans.DrivenJoint, wireTrans.DrivenJoint);
        Assert.Equal(originalTrans.Ratio, wireTrans.Ratio);
    }

    // The rim of these fixture rotors is about half a metre. A 0.1 rad/s
    // coupling residual permits at most 5 cm relative travel during the one-second observation.
    private const float CoupledSpeedTolerance = 0.1f;
    private static readonly PortId MechanicalOutput = new("mech_drive");
    private static readonly PortId MechanicalInput = new("mech_in");

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task MixedSecondCoordinateMovesThroughLifecycleAndSave(bool reorder, bool connected)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        await using var replay = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var (driver, driven) = CreateUnfamiliarPackages();
        if (reorder)
        {
            var slider = driver.Mechanical!.Sliders[0];
            driver = driver with
            {
                Mechanical = driver.Mechanical with
                {
                    Sliders = [slider with { BaseFrame = new(new Vector3(0, 0, -3), Quaternion.Identity) }, slider]
                },
                Footprint = new(Bodies: 5, MovingBodies: 4, PrismaticConstraints: 2, RevoluteConstraints: 2),
                Ports = [driver.Ports[1] with { Body = new PackageBodyIndex(4) }, driver.Ports[0]]
            };
        }
        using var physics = DeclaredPhysics.Scoped([driver, driven]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(MixedDriverPart, driver.Ports), (DrivenPart, driven.Ports)]));
        var battery = PlacedParts.Battery(new(1), -4, 4, 0, enabled: ElectricalEnable.Enabled);
        var placed = new List<IWorkshopInstance> { battery };
        WorkshopGenericInstance Place(PartId part, GpuBodyId id, Godot.Vector3 point)
        {
            var pose = PlacementPose.Compose(new Dictionary<PartId, PartDefinition>(), placed,
                new(part, id, false), point, Godot.Quaternion.Identity);
            Assert.Equal(PlacementRefusal.None, pose.Refusal);
            var instance = WorkshopInput.Generic(part, id, pose.Position.X, pose.Position.Y, pose.Position.Z,
                pose.Rotation.X, pose.Rotation.Y, pose.Rotation.Z, pose.Rotation.W);
            placed.Add(instance);
            return instance;
        }
        var source = Place(MixedDriverPart, new(2), new(0, 4, 0));
        var target = Place(DrivenPart, new(3), new(4, 4, 0));
        var links = new WorkshopConnections(new WorkshopConnection(battery.Id, PortId.Supply, source.Id, PortId.PowerIn, WorkshopConnectionDomain.Electrical));
        if (connected) links = links.With(new(source.Id, MechanicalOutput, target.Id, MechanicalInput, WorkshopConnectionDomain.Mechanical));
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            reorder ? new(target, source, battery) : new(battery, source, target), Connections: links);
        await harness.Require(WorkshopCommandKind.Construct, construction);
        var authored = harness.Committed;
        var identities = construction.Instances.Identities;
        var selected = identities[source.Id].Hinges[1].Body;
        var unrelated = identities[source.Id].Hinges[0].Body;
        var follower = identities[target.Id].Hinges[0].Body;
        void Motion(WorkshopRead read)
        {
            Assert.True(read.Bodies.TryGet(selected, out var selectedBody));
            Assert.True(read.Bodies.TryGet(unrelated, out var unrelatedBody));
            Assert.True(read.Bodies.TryGet(follower, out var targetBody));
            Assert.True(selectedBody.AngularVelocity.Z > 1f, "The explicitly selected second hinge visibly rotates within one second.");
            Assert.InRange(unrelatedBody.AngularVelocity.Z, -CoupledSpeedTolerance, CoupledSpeedTolerance);
            var expected = connected ? selectedBody.AngularVelocity.Z * 0.5f : 0f;
            Assert.InRange(targetBody.AngularVelocity.Z - expected, -CoupledSpeedTolerance, CoupledSpeedTolerance);
        }
        await harness.Run();
        await harness.Advance(120);
        Motion(harness.Committed);
        await harness.Require(WorkshopCommandKind.Pause);
        var paused = harness.Committed.Tick;
        await harness.Require(WorkshopCommandKind.Step);
        Assert.Equal(paused.Value + 1, harness.Committed.Tick.Value);
        await harness.Require(WorkshopCommandKind.Resume);
        await harness.Advance(12);
        Motion(harness.Committed);
        await harness.Reset();
        Assert.Equal(0UL, harness.Committed.Tick.Value);
        for (var i = 0; i < authored.Bodies.Count; i++)
        {
            var body = authored.Bodies[i];
            Assert.True(harness.Committed.Bodies.TryGet(body.Body.Id, out var reset));
            Assert.Equal(body.Body.Cell, reset.Body.Cell);
            Assert.Equal(body.Body.Local, reset.Body.Local);
            Assert.Equal(body.Rotation, reset.Rotation);
            Assert.Equal(body.AngularVelocity, reset.AngularVelocity);
            Assert.Equal(body.Body.Velocity, reset.Body.Velocity);
        }
        await harness.Run();
        await harness.Advance(120);
        Motion(harness.Committed);
        await harness.Reset();
        await harness.Require(WorkshopCommandKind.Save);
        var saved = WorkshopSaveCodec.Encode(new(construction, new(42)));
        var loaded = WorkshopSaveCodec.Decode(SaveFixture.Elements.Concat([MixedDriverPart, DrivenPart]).ToHashSet(), saved);
        Assert.Equal(construction.Connections, loaded.Construction.Connections);
        await replay.Require(WorkshopCommandKind.Construct, loaded.Construction);
        await replay.Run();
        await replay.Advance(120);
        Motion(replay.Committed);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void MechanicalScaleMustBeFiniteAndNonzero(float scale)
    {
        var (driver, _) = CreateUnfamiliarPackages();
        var port = driver.Ports[1] with { CoordinatePerRadian = scale };
        Assert.Equal(ElementPortRejection.OutOfRange, Assert.Throws<ElementPortRejectedException>(() =>
            ElementPortCatalogue.Declare([(MixedDriverPart, new[] { port })])).Reason);
        var errors = BundleValidator.Validate([new NamedEntry("scale", driver with { Ports = [port] })]);
        Assert.Contains(errors, error => error.Code == BundleErrorCode.OutOfRange && error.Detail.Contains(".CoordinatePerRadian", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(float.MaxValue, float.Epsilon)]
    [InlineData(float.Epsilon, float.MaxValue)]
    public void DerivedRatioMustBeRepresentableBeforeCompilation(float sourceScale, float targetScale)
    {
        var (driver, driven) = CreateUnfamiliarPackages();
        driver = driver with { Ports = [driver.Ports[1] with { CoordinatePerRadian = sourceScale }] };
        driven = driven with { Ports = [driven.Ports[0] with { CoordinatePerRadian = targetScale }] };
        using var physics = DeclaredPhysics.Scoped([driver, driven]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(MixedDriverPart, driver.Ports), (DrivenPart, driven.Ports)]));
        var source = WorkshopInput.Generic(MixedDriverPart, new(1), 0, 4, 0, 0, 0, 0, 1);
        var target = WorkshopInput.Generic(DrivenPart, new(2), 4, 4, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(source, target));
        var candidate = construction with { Connections = new(new WorkshopConnection(source.Id, MechanicalOutput, target.Id, MechanicalInput, WorkshopConnectionDomain.Mechanical)) };
        Assert.Equal(WorkshopEditRejection.InvalidConnection, Assert.Throws<WorkshopEditRejectedException>(() => candidate.Validate()).Reason);
        Assert.Empty(construction.Connections);
        Assert.Equal(new ConstructionRevision(2), construction.Revision);
    }

    [Fact]
    public void AliasesCannotReuseEitherCoordinateRole()
    {
        var (driver, driven) = CreateUnfamiliarPackages();
        var outputAlias = new PortId("output_alias");
        var inputAlias = new PortId("input_alias");
        driver = driver with { Ports = [driver.Ports[1], driver.Ports[1] with { Id = outputAlias }] };
        driven = driven with { Ports = [driven.Ports[0], driven.Ports[0] with { Id = inputAlias }] };
        using var physics = DeclaredPhysics.Scoped([driver, driven]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(MixedDriverPart, driver.Ports), (DrivenPart, driven.Ports)]));
        var source = WorkshopInput.Generic(MixedDriverPart, new(1), 0, 4, 0, 0, 0, 0, 1);
        var source2 = WorkshopInput.Generic(MixedDriverPart, new(2), 0, 8, 0, 0, 0, 0, 1);
        var target = WorkshopInput.Generic(DrivenPart, new(3), 4, 4, 0, 0, 0, 0, 1);
        var target2 = WorkshopInput.Generic(DrivenPart, new(4), 4, 8, 0, 0, 0, 0, 1);
        var instances = new WorkshopInstances(source, source2, target, target2);
        var first = new WorkshopConnection(source.Id, MechanicalOutput, target.Id, MechanicalInput, WorkshopConnectionDomain.Mechanical);
        new WorkshopConnections(first).Validate(instances);
        foreach (var second in new[] {
            new WorkshopConnection(source.Id, outputAlias, target2.Id, MechanicalInput, WorkshopConnectionDomain.Mechanical),
            new WorkshopConnection(source2.Id, MechanicalOutput, target.Id, inputAlias, WorkshopConnectionDomain.Mechanical) })
            Assert.Equal(WorkshopEditRejection.InvalidConnection, Assert.Throws<WorkshopEditRejectedException>(() =>
                new WorkshopConnections(first, second).Validate(instances)).Reason);
    }

    [Theory]
    [InlineData(2f)]
    [InlineData(-1f)]
    [InlineData(1f)]
    public void SharedCarrierUsesOneCombinedBodyJacobian(float ratio)
    {
        var scene = new LawScene(bench: false);
        var carrier = scene.Sphere(new(0, 4, 0), .1f, 1f, LawMaterial.Bench, Vector3.Zero, Vector3.Zero, 0f, 0f);
        var source = scene.Sphere(new(0, 4, 1), .1f, 1f, LawMaterial.Bench, Vector3.Zero, new(0, 0, 6), 0f, 0f);
        var target = scene.Sphere(new(0, 4, -1), .1f, 1f, LawMaterial.Bench, Vector3.Zero, Vector3.Zero, 0f, 0f);
        var a = scene.Revolute(carrier, source, new(new(0, 0, .5f), Quaternion.Identity), new(new(0, 0, -.5f), Quaternion.Identity),
            -MathF.PI, MathF.PI, 0f, travel: RevoluteTravel.Continuous);
        var b = scene.Revolute(carrier, target, new(new(0, 0, -.5f), Quaternion.Identity), new(new(0, 0, .5f), Quaternion.Identity),
            -MathF.PI, MathF.PI, 0f, travel: RevoluteTravel.Continuous);
        scene.Transmission(a, b, ratio);
        var state = scene.Admit().Step(120);
        var wc = state[carrier].Angular.Z;
        var ws = state[source].Angular.Z;
        var wt = state[target].Angular.Z;
        // Equal spherical inertias cancel in J M^-1 J^T. Initial velocities are (0,6,0),
        // J=(ratio-1,-ratio,1); the ideal bilateral impulse projects onto J*w=0.
        var impulseOverInertia = 6f * ratio / ((ratio - 1f) * (ratio - 1f) + ratio * ratio + 1f);
        Assert.InRange(wc - (ratio - 1f) * impulseOverInertia, -CoupledSpeedTolerance, CoupledSpeedTolerance);
        Assert.InRange(ws - (6f - ratio * impulseOverInertia), -CoupledSpeedTolerance, CoupledSpeedTolerance);
        Assert.InRange(wt - impulseOverInertia, -CoupledSpeedTolerance, CoupledSpeedTolerance);
        Assert.InRange((wt - wc) - ratio * (ws - wc), -CoupledSpeedTolerance, CoupledSpeedTolerance);
        // No source exists: neither the shared carrier nor redundant solving may manufacture rotation energy.
        Assert.True(wc * wc + ws * ws + wt * wt <= 36f + 1f, "Passive coupling cannot add more than the 0.1 rad/s observation allowance to initial energy.");
    }

    [Theory]
    [InlineData(2f, true)]
    [InlineData(-2f, true)]
    [InlineData(2f, false)]
    public void RotatedJointFramesTransmitLoadBackToTheSource(float ratio, bool connected)
    {
        var scene = new LawScene(bench: false);
        var sourceAnchor = scene.StaticSphere(new(-2, 4, 0), .05f, Quaternion.Identity, LawMaterial.Bench);
        var targetAnchor = scene.StaticSphere(new(2, 4, 0), .05f, Quaternion.Identity, LawMaterial.Bench);
        var source = scene.Sphere(new(-2, 4, 0), .1f, 1f, LawMaterial.Bench, Vector3.Zero, new(0, 0, 6), 0f, 0f);
        var target = scene.Sphere(new(2, 4, 0), .1f, 1f, LawMaterial.Bench, Vector3.Zero, Vector3.Zero, 0f, 0f);
        var frame = new ConstraintFrame(Vector3.Zero, Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2f));
        var a = scene.Revolute(sourceAnchor, source, ConstraintFrame.Identity, ConstraintFrame.Identity, -MathF.PI, MathF.PI, 0f, travel: RevoluteTravel.Continuous);
        var b = scene.Revolute(targetAnchor, target, frame, frame, -MathF.PI, MathF.PI, 0f, travel: RevoluteTravel.Continuous);
        if (connected) scene.Transmission(a, b, ratio);
        var state = scene.Admit().Step(120);
        var expectedSource = connected ? 6f / (1f + ratio * ratio) : 6f;
        var expectedTarget = connected ? ratio * expectedSource : 0f;
        Assert.InRange(state[source].Angular.Z - expectedSource, -CoupledSpeedTolerance, CoupledSpeedTolerance);
        Assert.InRange(state[target].Angular.X - expectedTarget, -CoupledSpeedTolerance, CoupledSpeedTolerance);
        Assert.InRange(state[target].Angular.Z, -CoupledSpeedTolerance, CoupledSpeedTolerance);
    }

    [Theory]
    [InlineData(false, 1f)]
    [InlineData(true, 1f)]
    [InlineData(true, 2f)]
    public void ChainsAndClosedRelationsUseTheSamePassiveLaw(bool close, float closingRatio)
    {
        var scene = new LawScene(bench: false);
        var rotors = new GpuBodyId[3];
        var joints = new RevoluteConstraintId[3];
        for (var i = 0; i < 3; i++)
        {
            var at = new Vector3(-3f + 3f * i, 4, 0);
            var anchor = scene.StaticSphere(at, .05f, Quaternion.Identity, LawMaterial.Bench);
            rotors[i] = scene.Sphere(at, .1f, 1f, LawMaterial.Bench, Vector3.Zero, i == 0 ? new(0, 0, 6) : Vector3.Zero, 0f, 0f);
            joints[i] = scene.Revolute(anchor, rotors[i], ConstraintFrame.Identity, ConstraintFrame.Identity,
                -MathF.PI, MathF.PI, 0f, travel: RevoluteTravel.Continuous);
        }
        scene.Transmission(joints[0], joints[1], 1f);
        scene.Transmission(joints[1], joints[2], 1f);
        if (close) scene.Transmission(joints[2], joints[0], closingRatio);
        var world = scene.Admit();
        var state = world.Step(120);
        // A redundant unit loop shares the initial momentum; a non-unit closed relation can only rest.
        var expected = close && closingRatio != 1f ? 0f : 2f;
        foreach (var rotor in rotors)
            Assert.InRange(state[rotor].Angular.Z - expected, -CoupledSpeedTolerance, CoupledSpeedTolerance);
        Assert.True(rotors.Sum(rotor => state[rotor].Angular.LengthSquared()) <= 37f, "Passive closed relations cannot manufacture rotational energy.");
    }

    public enum TransmissionWireDefect { ZeroId, DuplicateId, MissingJoint, SameJoint, DuplicateSource, DuplicateTarget, ZeroRatio, NaNRatio, InfiniteRatio }

    [Theory]
    [InlineData(TransmissionWireDefect.ZeroId)]
    [InlineData(TransmissionWireDefect.DuplicateId)]
    [InlineData(TransmissionWireDefect.MissingJoint)]
    [InlineData(TransmissionWireDefect.SameJoint)]
    [InlineData(TransmissionWireDefect.DuplicateSource)]
    [InlineData(TransmissionWireDefect.DuplicateTarget)]
    [InlineData(TransmissionWireDefect.ZeroRatio)]
    [InlineData(TransmissionWireDefect.NaNRatio)]
    [InlineData(TransmissionWireDefect.InfiniteRatio)]
    public async Task MalformedTransmissionWireRefusesWithoutReplacingTheWorld(TransmissionWireDefect defect)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var (driver, driven) = CreateUnfamiliarPackages();
        using var physics = DeclaredPhysics.Scoped([driver, driven]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(MixedDriverPart, driver.Ports), (DrivenPart, driven.Ports)]));
        var sources = new[] { WorkshopInput.Generic(MixedDriverPart, new(1), -3, 4, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(MixedDriverPart, new(2), -3, 7, 0, 0, 0, 0, 1) };
        var targets = new[] { WorkshopInput.Generic(DrivenPart, new(3), 3, 4, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(DrivenPart, new(4), 3, 7, 0, 0, 0, 0, 1) };
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new([.. sources, .. targets]),
            Connections: new(sources.Select((source, i) => new WorkshopConnection(source.Id, MechanicalOutput, targets[i].Id,
                MechanicalInput, WorkshopConnectionDomain.Mechanical)).ToArray()));
        await harness.Require(WorkshopCommandKind.Construct, construction);
        var before = harness.Committed;
        var original = harness.Simulation.Scene;
        var command = harness.Command(WorkshopCommandKind.Construct, construction with { Revision = new(3) });
        var bytes = WorkshopWireFlatBuffers.EncodeCommand(command);
        var response = WorkshopWireFlatBuffers.EncodeResponse(harness.Latest with { Scene = command.Scene });
        Corrupt(bytes, 11);
        Corrupt(response, 27);
        Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() => harness.Router.Dispatch(bytes)).Reason);
        Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeResponse(response)).Reason);
        Assert.Same(original, harness.Simulation.Scene);
        Assert.Equal(before.Revision, harness.Committed.Revision);
        Assert.Equal(before.Epoch, harness.Committed.Epoch);
        Assert.Equal(before.Tick, harness.Committed.Tick);
        Assert.Equal(construction.Instances, harness.Construction.Instances);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await harness.Dispatch(command)).Result.Outcome);

        void Corrupt(byte[] buffer, int sceneField)
        {
            var bb = new Google.FlatBuffers.ByteBuffer(buffer);
            var scene = CompiledSceneBoundaryTests.Child(new Google.FlatBuffers.Table(4 + bb.GetInt(4), bb), sceneField);
            var rows = CompiledSceneBoundaryTests.Rows(scene, 34); // Current schema's transmissions vector.
            var first = rows[0];
            var second = rows[1];
            ulong Read(Google.FlatBuffers.Table row, int field) => bb.GetUlong(CompiledSceneBoundaryTests.Slot(row, field));
            void Identity(Google.FlatBuffers.Table row, int field, ulong value) =>
                System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(CompiledSceneBoundaryTests.Slot(row, field)), value);
            void Ratio(float value) => System.Buffers.Binary.BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(CompiledSceneBoundaryTests.Slot(first, 3)), value);
            switch (defect)
            {
                case TransmissionWireDefect.ZeroId: Identity(first, 0, 0); break;
                case TransmissionWireDefect.DuplicateId: Identity(second, 0, Read(first, 0)); break;
                case TransmissionWireDefect.MissingJoint: Identity(first, 1, ulong.MaxValue); break;
                case TransmissionWireDefect.SameJoint: Identity(first, 2, Read(first, 1)); break;
                case TransmissionWireDefect.DuplicateSource: Identity(second, 1, Read(first, 1)); break;
                case TransmissionWireDefect.DuplicateTarget: Identity(second, 2, Read(first, 2)); break;
                case TransmissionWireDefect.ZeroRatio: Ratio(0f); break;
                case TransmissionWireDefect.NaNRatio: Ratio(float.NaN); break;
                case TransmissionWireDefect.InfiniteRatio: Ratio(float.PositiveInfinity); break;
                default: throw new ArgumentOutOfRangeException(nameof(defect));
            }
        }
    }

    [Theory]
    [InlineData(CoordinateKind.Revolute, -1, false)]
    [InlineData(CoordinateKind.Revolute, 0, true)]
    [InlineData(CoordinateKind.Revolute, 1, true)]
    [InlineData(CoordinateKind.Revolute, 2, false)]
    [InlineData(CoordinateKind.Prismatic, 0, false)]
    [InlineData((CoordinateKind)255, 0, false)]
    public void CoordinateAdmissionChecksKindAndTheExactLocalTableBoundary(CoordinateKind kind, int index, bool valid)
    {
        var (driver, driven) = CreateUnfamiliarPackages();
        var output = driver.Ports[1] with { Coordinate = new(kind, index) };
        driver = driver with { Ports = [driver.Ports[0], output] };
        var errors = BundleValidator.Validate([new NamedEntry("coordinate", driver)]);
        var coordinateErrors = errors.Where(error => error.Detail.Contains(".Coordinate", StringComparison.Ordinal)).ToArray();
        if (valid) Assert.Empty(coordinateErrors);
        else Assert.NotEmpty(coordinateErrors);
        if (kind != CoordinateKind.Revolute || index < 0)
        {
            Assert.Equal(ElementPortRejection.OutOfRange, Assert.Throws<ElementPortRejectedException>(() =>
                ElementPortCatalogue.Declare([(MixedDriverPart, driver.Ports)])).Reason);
            return;
        }
        using var physics = DeclaredPhysics.Scoped([driver, driven]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(MixedDriverPart, driver.Ports), (DrivenPart, driven.Ports)]));
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(WorkshopInput.Generic(MixedDriverPart, new(1), 0, 4, 0, 0, 0, 0, 1),
                WorkshopInput.Generic(DrivenPart, new(2), 4, 4, 0, 0, 0, 0, 1)),
            Connections: new(new WorkshopConnection(new(1), MechanicalOutput, new(2), MechanicalInput, WorkshopConnectionDomain.Mechanical)));
        if (valid) construction.Validate();
        else Assert.Equal(WorkshopEditRejection.InvalidConnection, Assert.Throws<WorkshopEditRejectedException>(() =>
            WorkshopSceneCompiler.Compile(construction, new(1, 2), new(1))).Reason);
    }
}
