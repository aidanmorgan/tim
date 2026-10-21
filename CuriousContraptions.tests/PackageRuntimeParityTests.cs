using System;
using System.Collections.Generic;
using System.Linq;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.BundleElements;
using CuriousContraptions.Gpu;
using Godot;
using AbstractShape = CuriousContraptions.Abstractions.ColliderShapeKind;
using RuntimeShape = CuriousContraptions.Gpu.ColliderShapeKind;

namespace CuriousContraptions.Tests;

/// <summary>Every package's declaration equals the runtime tables that still read their own copy of it: ports (WorkshopPorts.For,
/// until Story 9A.17), colliders, footprint, body and material (the compiler and WorkshopCanonicalColliders.For, until Story 9A.16),
/// placement defaults and bounds (MachineWorld.CaptureInstance and the records, until each conversion), ball materials
/// (BallMaterial.For, until C-Balls) and the signal declarations the runtime reads (F15-11). Story 9A.14 deleted the catalogue half of
/// the former cutover guard with the catalogue; each remaining comparison leaves with the table it compares. Values are compared at
/// the runtime lane's precision (spec 9A.4 D2): package f32 values narrowed to the runtime's binary16 lanes, and f32 rotations and
/// port positions bit for bit. The runtime body, collider and material rows come from one compiled instance of each part built with
/// the canonical colliders. The detector's and gate's scene-extracted colliders, which MachineWorld.CaptureInstance prefers, are not
/// compared: the detector's scene ring differs from the canonical table (filed to Story 9A.16).</summary>
[Collection<NativeSceneCollectionDefinition>]
public sealed class PackageRuntimeParityTests(NativeSceneFixture godot)
{
    private static readonly GpuBodyId Owner = new(1);

    private static IReadOnlyList<BundlePackage> Packages()
    {
        var discovered = PackageDiscovery.Discover(twodog.Engine.ResolveProjectDir());
        Assert.Empty(discovered.Errors);
        return discovered.Packages;
    }

    /// <summary>F15-11: the Impact switch's declared trigger threshold is the compiled contact trigger's threshold, the Domino's
    /// declared topple threshold (the cosine of the topple angle) is the compiled orientation sensor's angle, and the Counter's declared
    /// count is the runtime's placed target count, and the clock and latch declare the event kinds the activation network publishes
    /// for them. The other signal declarations (level thresholds, pulse durations, latch modes, the other event kinds and port normals)
    /// have no runtime reader yet (evidence-9a-14/f15-11-runtime-readers.txt): the story that makes the runtime read one adds its
    /// comparison here, and port normals are held to unit length by the bundle validator.</summary>
    [Fact]
    public void EverySignalDeclarationTheRuntimeReadsEqualsTheRuntime()
    {
        _ = godot;
        var packages = Packages().ToDictionary(package => package.Declaration.Metadata.Id, package => package.Declaration, StringComparer.Ordinal);
        var impact = (Abstractions.Signal.ISignalElement)packages["switch"];
        // A contact trigger compiles for the balls that can strike it.
        var struck = WorkshopPhysicsCompiler.Compile(new WorkshopConstruction(new ConstructionRevision(1), WorkshopCadenceSettings.Default(),
            new WorkshopInstances(Placed(PartId.ImpactSwitch), WorkshopInput.Basketball(new(2), 0, 6, 0, 0, 0, 0, 1))), new(1, 2));
        var trigger = Assert.Single(struck.Triggers.ToArray());
        Assert.True(Same(Assert.Single(impact.Triggers).Threshold, trigger.Threshold.Value),
            $"switch: trigger threshold {impact.Triggers[0].Threshold} declared, {trigger.Threshold.Value} compiled.");
        var domino = (Abstractions.Signal.ISignalElement)packages["domino"];
        // The compiler declares this threshold on a wired tile's orientation sensor.
        var compiledDegrees = 2 * Math.Acos((double)WorkshopDomino.ActivationThreshold.CosineHalfAngle) * 180 / Math.PI;
        var declaredDegrees = Math.Acos(Assert.Single(domino.Triggers).Threshold) * 180 / Math.PI;
        Assert.True(Math.Abs(compiledDegrees - declaredDegrees) < .1,
            $"domino: topple threshold {declaredDegrees}° declared, {compiledDegrees}° compiled.");
        var counter = (Abstractions.Signal.ISignalElement)packages["counter"];
        Assert.Equal(WorkshopCounter.PlacedTargetCount, Assert.NotNull(counter.Counter).Threshold);
        // The activation network publishes these event kinds for these elements (ActivationNetwork: the oscillator tick and the
        // State Latch's memory change), so each package must declare the kind the runtime publishes for it.
        Assert.Contains(packages["clock"].Events, descriptor => descriptor.EventKind == Abstractions.SimulationEventKind.TriggerActivated);
        Assert.Contains(packages["latch"].Events, descriptor => descriptor.EventKind == Abstractions.SimulationEventKind.LatchSet);
    }

    [Fact]
    public void EveryPackageDeclaresTheRuntimePortsInOrder()
    {
        foreach (var package in Packages())
        {
            var id = new PartId(package.Declaration.Metadata.Id);
            var runtime = WorkshopPorts.For(id).ToArray();
            var declared = package.Declaration.Ports;
            Assert.True(runtime.Length == declared.Count, $"{package.Package}: {declared.Count} ports declared, {runtime.Length} at runtime.");
            for (var i = 0; i < runtime.Length; i++)
            {
                var port = declared[i];
                var where = $"{package.Package}.Ports[{i}]";
                Assert.True(Enum.GetNames<WorkshopSocket>().Contains(port.PortId), $"{where}: '{port.PortId}' names no socket.");
                Assert.Equal(runtime[i].Socket, Enum.Parse<WorkshopSocket>(port.PortId));
                Assert.Equal(runtime[i].Domain switch
                {
                    WorkshopConnectionDomain.Activation => SimulationDomain.Signal,
                    WorkshopConnectionDomain.Electrical => SimulationDomain.Electrical,
                    _ => throw new ArgumentException("Undefined connection domain.")
                }, port.Domain);
                Assert.Equal(runtime[i].Direction == WorkshopPortDirection.Input ? PortDirection.Input : PortDirection.Output, port.Direction);
                Assert.Equal(runtime[i].Signal switch
                {
                    WorkshopSignalKind.Pulse => PortSignalKind.Pulse,
                    WorkshopSignalKind.Level => PortSignalKind.Level,
                    null => (PortSignalKind?)null,
                    _ => throw new ArgumentException("Undefined signal kind.")
                }, port.Signal);
                var position = WorkshopPorts.LocalPosition(id, runtime[i].Socket);
                Assert.True(Bits(position.X) == Bits(port.LocalPosition.X) && Bits(position.Y) == Bits(port.LocalPosition.Y) &&
                    Bits(position.Z) == Bits(port.LocalPosition.Z), $"{where}: position {port.LocalPosition} declared, {position} at runtime.");
            }
        }
    }

    [Fact]
    public void EveryPackageDeclaresTheCompiledFootprintCollidersAndMaterial()
    {
        foreach (var package in Packages())
        {
            var declaration = package.Declaration;
            var id = new PartId(declaration.Metadata.Id);
            var scene = Compile(Placed(id));
            // The workbench plane (the body of the one plane collider) belongs to no part.
            var plane = scene.Colliders.ToArray().Single(collider => collider.Shape == RuntimeShape.Plane).Body;
            var bodies = scene.Bodies.ToArray().Where(body => body.Id != plane).ToArray();
            Assert.True(new ElementFootprint(bodies.Length, bodies.Count(body => body.Motion != RigidMotionKind.Static), scene.Prismatics.Length) ==
                declaration.Footprint, $"{package.Package}: footprint {declaration.Footprint} declared.");
            var colliders = scene.Colliders.ToArray().Where(collider => collider.Body == Owner).ToArray();
            AssertColliders(package.Package, declaration.Colliders, colliders.Select(collider => (collider.Shape, collider.Pose, collider.Radius.Value,
                collider.HalfExtents)).ToArray());
            AssertMaterials(package.Package, declaration.Colliders, colliders, scene.Materials.ToArray());
            if (Canonical.Contains(id.Value))
                AssertColliders(package.Package, declaration.Colliders, WorkshopCanonicalColliders.For(id)
                    .Select(collider => (collider.ShapeKind, collider.LocalPose, collider.Radius, collider.HalfExtents)).ToArray());
        }
    }

    [Fact]
    public void EveryPackageDeclaresTheRuntimeParameterDefaultsAndBounds()
    {
        foreach (var package in Packages())
        {
            var id = package.Declaration.Metadata.Id;
            var expected = Parameters(id);
            var declared = package.Declaration.Metadata.Parameters;
            Assert.Equal(expected.Select(parameter => parameter.Key), declared.Select(parameter => parameter.Key));
            for (var i = 0; i < expected.Length; i++)
            {
                var (key, unit, kind, lane, runtimeDefault, admits, options) = expected[i];
                var parameter = declared[i];
                var where = $"{package.Package}.Metadata.Parameters[{i}] ({key})";
                Assert.Equal(unit, parameter.Unit);
                Assert.Equal(kind, parameter.Kind);
                Assert.Equal(options, parameter.Options);
                Assert.True(Narrow(lane, parameter.Default) == Narrow(lane, runtimeDefault), $"{where}: default {parameter.Default} declared, {runtimeDefault} at runtime.");
                Assert.True(admits(parameter.Minimum) && admits(parameter.Maximum), $"{where}: the runtime refuses a declared bound.");
                Assert.False(admits(Below(lane, parameter.Minimum)), $"{where}: the runtime admits below the declared minimum.");
                Assert.False(admits(Above(lane, parameter.Maximum)), $"{where}: the runtime admits above the declared maximum.");
            }
        }
    }

    /// <summary>PKG-1: each mechanical package's body is the compiled owner body: its motion, mass, linear drag and gravity.</summary>
    [Fact]
    public void EveryMechanicalPackageDeclaresTheCompiledBodyMotionMassDragAndGravity()
    {
        foreach (var package in Packages())
        {
            if (package.Declaration is not IMechanicalElement mechanical) continue;
            var scene = Compile(Placed(new PartId(mechanical.Metadata.Id)));
            var mass = mechanical.MassProperties;
            AssertBody(package.Package, mechanical.KinematicMode, mass.Mass, mass.LinearDrag, mass.Gravity,
                scene.Bodies.ToArray().Single(body => body.Id == Owner));
        }
    }

    /// <summary>Story 9A.6: the Basketball and Bowling ball packages declare their ball material (radius, mass, bounce, drag, friction,
    /// bounce threshold, rolling resistance), equal to both BallMaterial.For and the material the registry builds from the bundle.
    /// Neither ball declares a fluid payload, so buoyancy is 0 at runtime for both kinds.</summary>
    [Fact]
    public void EveryBallPackageDeclaresItsBallMaterial()
    {
        var registry = new PartRegistry();
        registry.Load();
        var balls = Packages().Where(package => package.Declaration.Metadata.Id is "ball" or "bowling").ToArray();
        Assert.Equal(["ball", "bowling"], balls.Select(package => package.Declaration.Metadata.Id).Order(StringComparer.Ordinal));
        foreach (var package in balls)
        {
            var ball = (IMechanicalElement)package.Declaration;
            var id = new PartId(ball.Metadata.Id);
            var collider = Assert.Single(ball.Colliders);
            Assert.Equal(AbstractShape.Sphere, collider.ShapeKind);
            var loaded = registry.Definitions[id].Ball ?? throw new ArgumentException($"{package.Package}: no bundle ball material.");
            foreach (var material in new[] { BallMaterial.For(id), loaded })
            {
                Assert.True(Same(collider.HalfExtents.X, material.Radius.Value) && Same(ball.MassProperties.Mass, material.Mass.Value) &&
                    Same(collider.Material.Restitution, material.Bounce.Value) && Same(ball.MassProperties.LinearDrag, material.Drag.Value) &&
                    Same(collider.Material.Friction, material.Friction.Value) && Same(collider.Material.BounceThreshold, material.BounceThreshold.Value) &&
                    Same(collider.Material.RollingResistance, material.Rolling.Value), $"{package.Package}: declared ball material differs from {material}.");
                Assert.True(Same(0f, material.Buoyancy.Value), $"{package.Package}: the runtime ball is buoyant, and the package cannot say so.");
            }
        }
    }

    /// <summary>Story 9A.12: each slider package declares the compiled prismatic joint and its moving body (frames, limits, collision,
    /// mass, drag, gravity, colliders and material), and the joint's stiffness, damping and starting compression come from the parameters
    /// the slider names. The part is compiled with every parameter at its declared maximum, so each key must bind its own value.</summary>
    [Fact]
    public void EverySliderPackageDeclaresTheCompiledPrismaticJointAndMovingBody()
    {
        var sliders = Packages().Where(package => package.Declaration is ISliderElement).ToArray();
        Assert.Equal(["Springboard"], sliders.Select(package => package.Package));
        foreach (var package in sliders)
        {
            var element = (ISliderElement)package.Declaration;
            var id = new PartId(element.Metadata.Id);
            var maxima = element.Metadata.Parameters.ToDictionary(parameter => parameter.Key, parameter => parameter.Maximum, StringComparer.Ordinal);
            var placed = Placed(id, maxima);
            var scene = Compile(placed);
            var joints = scene.Prismatics.ToArray();
            Assert.True(joints.Length == element.Sliders.Count, $"{package.Package}: {element.Sliders.Count} sliders declared, {joints.Length} compiled.");
            for (var i = 0; i < joints.Length; i++)
            {
                var (slider, joint, where) = (element.Sliders[i], joints[i], $"{package.Package}.Sliders[{i}]");
                Assert.Equal(Owner, joint.BodyA);
                AssertFrame(where + ".BaseFrame", slider.BaseFrame, joint.FrameA);
                AssertFrame(where + ".MovingFrame", slider.MovingFrame, joint.FrameB);
                Assert.True(Bits(slider.LowerLimit) == Bits(joint.Lower) && Bits(slider.UpperLimit) == Bits(joint.Upper),
                    $"{where}: limits [{slider.LowerLimit}, {slider.UpperLimit}] declared, [{joint.Lower}, {joint.Upper}] compiled.");
                Assert.True(Bits(maxima[slider.StiffnessParameter]) == Bits(joint.Stiffness), $"{where}.StiffnessParameter binds {joint.Stiffness}.");
                Assert.True(Bits(maxima[slider.DampingParameter]) == Bits(joint.Damping), $"{where}.DampingParameter binds {joint.Damping}.");
                Assert.Equal(slider.BodiesCollide ? ConnectedCollision.Enabled : ConnectedCollision.Disabled, joint.Collision);
                var moving = scene.Bodies.ToArray().Single(body => body.Id == joint.BodyB);
                AssertBody(where + ".MovingBody", KinematicMode.Dynamic, slider.MovingBody.Mass, slider.MovingBody.LinearDrag,
                    slider.MovingBody.Gravity, moving);
                // The moving body starts at the base frame, CompressionParameter below it along local Y (an unrotated placement).
                var compression = maxima[slider.CompressionParameter];
                var start = slider.BaseFrame.Position - slider.MovingFrame.Position - new System.Numerics.Vector3(0f, compression, 0f);
                var x = WorkshopInput.Position((placed.Cell.X + (float)placed.Local.X) / 16f + start.X);
                var y = WorkshopInput.Position((placed.Cell.Y + (float)placed.Local.Y) / 16f + start.Y);
                var z = WorkshopInput.Position((placed.Cell.Z + (float)placed.Local.Z) / 16f + start.Z);
                Assert.True(moving.Cell == new CellOrigin(x.Cell, y.Cell, z.Cell) && moving.Local == new LocalPosition(x.Local, y.Local, z.Local),
                    $"{where}.CompressionParameter: the moving body starts at {moving.Cell} {moving.Local}.");
                var colliders = scene.Colliders.ToArray().Where(collider => collider.Body == joint.BodyB).ToArray();
                AssertColliders(where + ".MovingBody", slider.MovingBody.Colliders, colliders.Select(collider => (collider.Shape, collider.Pose,
                    collider.Radius.Value, collider.HalfExtents)).ToArray());
                AssertMaterials(where + ".MovingBody", slider.MovingBody.Colliders, colliders, scene.Materials.ToArray());
            }
        }
    }

    private static PhysicsSceneDeclaration Compile(IWorkshopInstance instance) =>
        WorkshopPhysicsCompiler.Compile(new WorkshopConstruction(new ConstructionRevision(1), WorkshopCadenceSettings.Default(),
            new WorkshopInstances(instance)), new(1, 2));

    private static void AssertBody(string where, KinematicMode mode, float mass, float drag, System.Numerics.Vector3 gravity, RigidBodyDeclaration body)
    {
        Assert.True(body.Motion == mode switch
        {
            KinematicMode.Static => RigidMotionKind.Static,
            KinematicMode.Dynamic => RigidMotionKind.Dynamic,
            _ => throw new ArgumentException($"{where}: the runtime has no {mode} body.")
        }, $"{where}: {mode} declared, {body.Motion} compiled.");
        Assert.True(Same(mass, body.Mass.Value), $"{where}: mass {mass} declared, {body.Mass.Value} compiled.");
        Assert.True(Same(drag, body.LinearDrag.Value), $"{where}: linear drag {drag} declared, {body.LinearDrag.Value} compiled.");
        Assert.True(Same(gravity.X, body.Gravity.X) && Same(gravity.Y, body.Gravity.Y) && Same(gravity.Z, body.Gravity.Z),
            $"{where}: gravity {gravity} declared, {body.Gravity} compiled.");
    }

    private static void AssertFrame(string where, SliderFrame declared, ConstraintFrame compiled) =>
        Assert.True(Bits(declared.Position.X) == Bits(compiled.Position.X) && Bits(declared.Position.Y) == Bits(compiled.Position.Y) &&
            Bits(declared.Position.Z) == Bits(compiled.Position.Z) && Bits(declared.Rotation.X) == Bits(compiled.Rotation.X) &&
            Bits(declared.Rotation.Y) == Bits(compiled.Rotation.Y) && Bits(declared.Rotation.Z) == Bits(compiled.Rotation.Z) &&
            Bits(declared.Rotation.W) == Bits(compiled.Rotation.W), $"{where}: {declared} declared, {compiled} compiled.");

    private static void AssertMaterials(string where, IReadOnlyList<ColliderDescriptor> declared, ColliderDeclaration[] colliders,
        ContactMaterialDeclaration[] materials)
    {
        for (var i = 0; i < colliders.Length; i++)
        {
            var runtime = materials.Single(material => material.Id == colliders[i].Material);
            var material = declared[i].Material;
            Assert.True(Same(material.Restitution, runtime.Restitution.Value) && Same(material.BounceThreshold, runtime.BounceThreshold.Value) &&
                Same(material.Friction, runtime.Friction.Value) && Same(material.RollingResistance, runtime.RollingResistance.Value),
                $"{where}.Colliders[{i}].Material: {material} declared, {runtime} at runtime.");
        }
    }

    private enum Lane { Half, Single, Whole }

    private static float Narrow(Lane lane, float value) => lane switch
    {
        Lane.Half => (float)(Half)value,
        Lane.Single => value,
        Lane.Whole => MathF.Round(value),
        _ => throw new ArgumentException("Undefined lane.")
    };
    private static float Below(Lane lane, float value) => lane switch
    {
        Lane.Half => (float)Half.BitDecrement((Half)value),
        Lane.Single => float.BitDecrement(value),
        Lane.Whole => value - 1,
        _ => throw new ArgumentException("Undefined lane.")
    };
    private static float Above(Lane lane, float value) => lane switch
    {
        Lane.Half => (float)Half.BitIncrement((Half)value),
        Lane.Single => float.BitIncrement(value),
        Lane.Whole => value + 1,
        _ => throw new ArgumentException("Undefined lane.")
    };

    private static Func<float, bool> Admits(Action<float> place) => value =>
    {
        try { place(value); return true; }
        catch (ArgumentException) { return false; }
    };

    /// <summary>PKG-2: the funnel and bends compile from their canonical tables, which the worker's wire decode and every load use. Their
    /// scene-extracted colliders, which placement on the main thread prefers, differ in rotation bits (filed to Story 9A.16).</summary>
    private static readonly string[] Canonical = ["ball_detector", "powered_gate", "pressure_plate", "counter", "clock", "latch", "funnel",
        "pipe_bend_45", "pipe_bend_90"];

    /// <summary>The runtime parameters a newly placed part starts from (MachineWorld.CaptureInstance) and the bounds its record admits.</summary>
    private static (string Key, ParameterUnit Unit, ParameterKind Kind, Lane Lane, float Default, Func<float, bool> Admits, string[] Options)[] Parameters(string id)
    {
        var ramp = RampDimensions.Default;
        var wall = WallDimensions.Default;
        Func<float, bool> Ramp(Func<float, RampDimensions> dimensions) =>
            Admits(value => WorkshopInput.Ramp(Owner, 0, 3, 0, 0, 0, 0, 1, dimensions(value)));
        Func<float, bool> Wall(Func<float, WallDimensions> dimensions) =>
            Admits(value => WorkshopInput.Wall(Owner, 0, 3, 0, 0, 0, 0, 1, dimensions(value)));
        var enables = Enum.GetValues<ElectricalEnable>();
        var capture = ReceiverCaptureSettings.Free;
        var region = ReceiverForceRegion.Free;
        var participations = Enum.GetValues<SensorParticipation>();
        Func<float, bool> Receiver(Func<WorkshopReceiver, float, WorkshopReceiver> edit) =>
            Admits(value => edit(WorkshopInput.Receiver(Owner, 0, 3, 0, 0, 0, 0, 1), value).Validate());
        var spring = SpringboardSettings.Default;
        return id switch
        {
            "ramp" =>
            [
                ("length", ParameterUnit.Metre, ParameterKind.Real, Lane.Half, (float)ramp.Length.Value, Ramp(value => ramp with { Length = new((Half)value) }), []),
                ("width", ParameterUnit.Metre, ParameterKind.Real, Lane.Half, (float)ramp.Width.Value, Ramp(value => ramp with { Width = new((Half)value) }), [])
            ],
            "wall" =>
            [
                ("width", ParameterUnit.Metre, ParameterKind.Real, Lane.Half, (float)wall.Width.Value, Wall(value => wall with { Width = new((Half)value) }), []),
                ("height", ParameterUnit.Metre, ParameterKind.Real, Lane.Half, (float)wall.Height.Value, Wall(value => wall with { Height = new((Half)value) }), []),
                ("thickness", ParameterUnit.Metre, ParameterKind.Real, Lane.Half, (float)wall.Thickness.Value, Wall(value => wall with { Thickness = new((Half)value) }), [])
            ],
            "bumper" =>
            [
                ("strength", ParameterUnit.MetrePerSecond, ParameterKind.Real, Lane.Single, BumperWork.Default.Strength.Value,
                    Admits(value => BumperWork.FromInput(value)), [])
            ],
            "battery" =>
            [
                ("enabled", ParameterUnit.None, ParameterKind.Choice, Lane.Whole, Array.IndexOf(enables, ElectricalSourceSettings.Default.Enabled),
                    Admits(value => _ = float.IsInteger(value) && value >= 0 && value < enables.Length ? enables[(int)value] :
                        throw new ArgumentException("No such option.")), Enum.GetNames<ElectricalEnable>())
            ],
            "pressure_plate" =>
            [
                ("minimum_mass", ParameterUnit.Kilogram, ParameterKind.Real, Lane.Single, WorkshopPressurePlate.PlacedMinimumMass,
                    Admits(value => WorkshopInput.PressurePlate(Owner, 0, 3, 0, 0, 0, 0, 1, value)), [])
            ],
            "counter" =>
            [
                ("target_count", ParameterUnit.None, ParameterKind.WholeNumber, Lane.Whole, WorkshopCounter.PlacedTargetCount,
                    Admits(value => WorkshopInput.Counter(Owner, 0, 3, 0, 0, 0, 0, 1, (int)value)), [])
            ],
            "clock" =>
            [
                ("interval_seconds", ParameterUnit.Second, ParameterKind.Real, Lane.Single, WorkshopClock.PlacedIntervalSeconds,
                    Admits(value => WorkshopInput.Clock(Owner, 0, 3, 0, 0, 0, 0, 1, value)), [])
            ],
            "basket" =>
            [
                ("capture_margin", ParameterUnit.Metre, ParameterKind.Real, Lane.Half, (float)capture.Margin.Value,
                    Receiver((receiver, value) => receiver with { Capture = receiver.Capture with { Margin = new((Half)value) } }), []),
                ("capture_speed_limit", ParameterUnit.MetrePerSecond, ParameterKind.Real, Lane.Half, (float)capture.SpeedLimit.Value,
                    Receiver((receiver, value) => receiver with { Capture = receiver.Capture with { SpeedLimit = new((Half)value) } }), []),
                ("capture_dwell", ParameterUnit.Second, ParameterKind.Real, Lane.Half, (float)capture.Dwell.Value,
                    Receiver((receiver, value) => receiver with { Capture = receiver.Capture with { Dwell = new((Half)value) } }), []),
                ("capture_participation", ParameterUnit.None, ParameterKind.Choice, Lane.Whole, Array.IndexOf(participations, capture.Participation),
                    Receiver((receiver, value) => receiver with { Capture = receiver.Capture with { Participation = Option(participations, value) } }),
                    Enum.GetNames<SensorParticipation>()),
                ("force_support_margin", ParameterUnit.Metre, ParameterKind.Real, Lane.Half, (float)region.SupportMargin.Value,
                    Receiver((receiver, value) => receiver with { ForceRegion = ReceiverForceRegion.Create(new((Half)value), region.MaximumAcceleration) }), []),
                ("force_maximum_acceleration", ParameterUnit.MetrePerSecondSquared, ParameterKind.Real, Lane.Half, (float)region.MaximumAcceleration.Value,
                    Receiver((receiver, value) => receiver with { ForceRegion = ReceiverForceRegion.Create(region.SupportMargin, new((Half)value)) }), [])
            ],
            "spring" =>
            [
                ("stiffness", ParameterUnit.NewtonPerMetre, ParameterKind.Real, Lane.Single, spring.Stiffness,
                    Admits(value => (spring with { Stiffness = value }).Validate()), []),
                ("damping", ParameterUnit.NewtonSecondPerMetre, ParameterKind.Real, Lane.Single, spring.Damping,
                    Admits(value => (spring with { Damping = value }).Validate()), []),
                ("initial_compression", ParameterUnit.Metre, ParameterKind.Real, Lane.Single, spring.InitialCompression,
                    Admits(value => (spring with { InitialCompression = value }).Validate()), [])
            ],
            "pipe" =>
            [
                ("length", ParameterUnit.Metre, ParameterKind.Real, Lane.Single, PipeLength.Default.Metres,
                    Admits(value => new PipeLength(value).Validate()), [])
            ],
            "delay" =>
            [
                ("duration_seconds", ParameterUnit.Second, ParameterKind.Real, Lane.Half, (float)DelayDuration.Default.Seconds.Value,
                    Admits(value => WorkshopInput.Delay(Owner, 0, 3, 0, 0, 0, 0, 1, new DelayDuration(new((Half)value)))), [])
            ],
            "switch" =>
            [
                ("trigger_threshold", ParameterUnit.MetrePerSecond, ParameterKind.Real, Lane.Half, (float)ContactTriggerSettings.Default.Threshold.Value,
                    Admits(value => WorkshopInput.Switch(Owner, 0, 3, 0, 0, 0, 0, 1, new ContactTriggerSettings(new((Half)value)))), [])
            ],
            _ => []
        };
    }

    private static T Option<T>(T[] options, float value) =>
        float.IsInteger(value) && value >= 0 && value < options.Length ? options[(int)value] : throw new ArgumentException("No such option.");

    /// <summary>One newly placed instance of the part, as MachineWorld.CaptureInstance builds it.</summary>
    private static IWorkshopInstance Placed(PartId id) => id.Value switch
    {
        "ball" => WorkshopInput.Basketball(Owner, 0, 3, 0, 0, 0, 0, 1),
        "bowling" => WorkshopInput.Ball(PartId.BowlingBall, Owner, 0, 3, 0, 0, 0, 0, 1),
        "basket" => WorkshopInput.Receiver(Owner, 0, 3, 0, 0, 0, 0, 1),
        "spring" => WorkshopInput.Springboard(Owner, 0, 3, 0, 0, 0, 0, 1, SpringboardSettings.Default),
        "ramp" => WorkshopInput.Ramp(Owner, 0, 3, 0, 0, 0, 0, 1, RampDimensions.Default),
        "wall" => WorkshopInput.Wall(Owner, 0, 3, 0, 0, 0, 0, 1, WallDimensions.Default),
        "battery" => WorkshopInput.Battery(Owner, 0, 3, 0, 0, 0, 0, 1, ElectricalSourceSettings.Default),
        "bumper" => WorkshopInput.Bumper(Owner, 0, 3, 0, 0, 0, 0, 1, BumperWork.Default),
        "lamp" => WorkshopInput.Lamp(Owner, 0, 3, 0, 0, 0, 0, 1),
        "domino" => WorkshopInput.Domino(Owner, 0, 3, 0, 0, 0, 0, 1),
        "ball_detector" => WorkshopInput.BallDetector(Owner, 0, 3, 0, 0, 0, 0, 1),
        "pipe" => WorkshopInput.Pipe(Owner, 0, 3, 0, 0, 0, 0, 1),
        "funnel" or "pipe_bend_45" or "pipe_bend_90" or "powered_gate" =>
            WorkshopInput.Generic(id, Owner, 0, 3, 0, 0, 0, 0, 1, WorkshopCanonicalColliders.For(id)),
        "pressure_plate" => WorkshopInput.PressurePlate(Owner, 0, 3, 0, 0, 0, 0, 1, WorkshopPressurePlate.PlacedMinimumMass),
        "counter" => WorkshopInput.Counter(Owner, 0, 3, 0, 0, 0, 0, 1, WorkshopCounter.PlacedTargetCount),
        "clock" => WorkshopInput.Clock(Owner, 0, 3, 0, 0, 0, 0, 1, WorkshopClock.PlacedIntervalSeconds),
        "latch" => WorkshopInput.Latch(Owner, 0, 3, 0, 0, 0, 0, 1),
        "switch" => WorkshopInput.Switch(Owner, 0, 3, 0, 0, 0, 0, 1, ContactTriggerSettings.Default),
        "delay" => WorkshopInput.Delay(Owner, 0, 3, 0, 0, 0, 0, 1, DelayDuration.Default),
        _ => throw new ArgumentException($"No placement for packaged part '{id}'.")
    };

    /// <summary>One placed slider part with its declared parameters set to these values.</summary>
    private static WorkshopSpringboard Placed(PartId id, Dictionary<string, float> parameters) => id.Value switch
    {
        "spring" => WorkshopInput.Springboard(Owner, 0, 3, 0, 0, 0, 0, 1,
            new SpringboardSettings(parameters["stiffness"], parameters["damping"], parameters["initial_compression"])),
        _ => throw new ArgumentException($"No slider placement for packaged part '{id}'.")
    };

    private static void AssertColliders(string package, IReadOnlyList<ColliderDescriptor> declared,
        (RuntimeShape Shape, RigidLocalPose Pose, Half Radius, MetreVector HalfExtents)[] runtime)
    {
        Assert.True(declared.Count == runtime.Length, $"{package}: {declared.Count} colliders declared, {runtime.Length} at runtime.");
        for (var i = 0; i < runtime.Length; i++)
        {
            var (shape, pose, radius, halfExtents) = runtime[i];
            var collider = declared[i];
            var where = $"{package}.Colliders[{i}]";
            Assert.True(shape == collider.ShapeKind switch
            {
                AbstractShape.Sphere => RuntimeShape.Sphere,
                AbstractShape.Box => RuntimeShape.Box,
                AbstractShape.Plane => RuntimeShape.Plane,
                _ => throw new ArgumentException($"{where}: the runtime has no {collider.ShapeKind} collider.")
            }, $"{where}: shape {collider.ShapeKind} declared, {shape} at runtime.");
            Assert.True(Same(collider.Offset.X, pose.Translation.X) && Same(collider.Offset.Y, pose.Translation.Y) &&
                Same(collider.Offset.Z, pose.Translation.Z), $"{where}: offset {collider.Offset} declared, {pose.Translation} at runtime.");
            Assert.True(Bits(collider.Rotation.X) == Bits(pose.Rotation.X) && Bits(collider.Rotation.Y) == Bits(pose.Rotation.Y) &&
                Bits(collider.Rotation.Z) == Bits(pose.Rotation.Z) && Bits(collider.Rotation.W) == Bits(pose.Rotation.W),
                $"{where}: rotation {collider.Rotation} declared, {pose.Rotation} at runtime.");
            var extents = collider.HalfExtents;
            var matches = shape == RuntimeShape.Sphere
                ? Same(extents.X, radius) && Bits(extents.Y) == Bits(extents.X) && Bits(extents.Z) == Bits(extents.X) &&
                    Same(0f, halfExtents.X) && Same(0f, halfExtents.Y) && Same(0f, halfExtents.Z)
                : Same(extents.X, halfExtents.X) && Same(extents.Y, halfExtents.Y) && Same(extents.Z, halfExtents.Z) && Same(0f, radius);
            Assert.True(matches, $"{where}: half-extents {extents} declared, radius {radius} and {halfExtents} at runtime.");
        }
    }

    private static int Bits(float value) => BitConverter.SingleToInt32Bits(value);
    private static bool Same(float declared, Half runtime) => BitConverter.HalfToInt16Bits((Half)declared) == BitConverter.HalfToInt16Bits(runtime);
}
