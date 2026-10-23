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

/// <summary>Bundle/runtime parity and immutable pre-cutover compiled golden, at the existing admitted physical lane precision.</summary>
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
    /// count is the runtime's placed target count. The other signal declarations (level thresholds, pulse durations, latch modes, the other event kinds and port normals)
    /// have no runtime reader yet (evidence-9a-14/f15-11-runtime-readers.txt): the story that makes the runtime read one adds its
    /// comparison here, and port normals are held to unit length by the bundle validator.</summary>
    [Fact]
    public void EverySignalDeclarationTheRuntimeReadsEqualsTheRuntime()
    {
        _ = godot;
        var packages = Packages().ToDictionary(package => package.Declaration.Metadata.Id, package => package.Declaration, StringComparer.Ordinal);
        var impact = (Abstractions.Signal.IContactTriggerElement)packages["switch"];
        // A contact trigger compiles for the balls that can strike it.
        var struck = WorkshopPhysicsCompiler.Compile(new WorkshopConstruction(new ConstructionRevision(1), WorkshopCadenceSettings.Default(),
            new WorkshopInstances(Placed(PartId.ImpactSwitch), WorkshopInput.Generic(PartId.Basketball,new(2), 0, 6, 0, 0, 0, 0, 1))), new(1, 2));
        var trigger = Assert.Single(struck.Triggers.ToArray());
        Assert.Equal(packages["switch"].Metadata.Parameters.Single(p => p.Key == impact.Contact.ThresholdParameter).Default, trigger.Threshold.Value);
        var domino = (Abstractions.Signal.ISignalElement)packages["domino"];
        // The compiler declares this threshold on a wired tile's orientation sensor.
        var wiredTile = WorkshopPhysicsCompiler.Compile(new WorkshopConstruction(new ConstructionRevision(1), WorkshopCadenceSettings.Default(),
            new WorkshopInstances(PlacedParts.Domino(new(1), 0, 3, 0, 0, 0, 0, 1), PlacedParts.Lamp(new(2), 3, 1, 0, 0, 0, 0, 1)),
            Connections: new(new WorkshopConnection(new(1), WorkshopSocket.ActivationOut, new(2), WorkshopSocket.ActivationIn,
                WorkshopConnectionDomain.Activation))), new(1, 2));
        var compiledDegrees = 2 * Math.Acos((double)Assert.Single(wiredTile.OrientationSensors.ToArray()).Threshold.CosineHalfAngle) * 180 / Math.PI;
        var declaredDegrees = Math.Acos(Assert.Single(domino.Triggers).Threshold) * 180 / Math.PI;
        Assert.True(Math.Abs(compiledDegrees - declaredDegrees) < .1,
            $"domino: topple threshold {declaredDegrees}° declared, {compiledDegrees}° compiled.");
        // 9A.32: the declared f32 orientation threshold is the compiled sensor's, narrowed to the binary16 half-angle cosine lane bit-equal
        // to the former record's OrientationThreshold.Degrees(45) (0x3b64, cos 22.5°).
        var orientation = ((Abstractions.Signal.IOrientationSensorElement)packages["domino"]).Orientation;
        Assert.Equal(45f, orientation.ThresholdDegrees);
        var compiledThreshold = Assert.Single(wiredTile.OrientationSensors.ToArray()).Threshold;
        Assert.Equal(OrientationThreshold.Degrees(orientation.ThresholdDegrees), compiledThreshold);
        Assert.Equal(BitConverter.HalfToUInt16Bits(OrientationThreshold.Degrees(45).CosineHalfAngle), BitConverter.HalfToUInt16Bits(compiledThreshold.CosineHalfAngle));
        Assert.Equal((ushort)0x3b64, BitConverter.HalfToUInt16Bits(compiledThreshold.CosineHalfAngle));
        // C-Memory: the counter's target is its declared parameter, and the latch's memory reads the two inputs the shared law reads.
        Assert.Equal("target_count", ((Abstractions.Signal.IPulseCountElement)packages["counter"]).PulseCount.TargetParameter);
        Assert.Equal(new Abstractions.Signal.SignalMemoryDescriptor(Abstractions.PortSocket.SetIn, Abstractions.PortSocket.ResetIn),
            ((Abstractions.Signal.IMemoryElement)packages["latch"]).Memory);
        // Story 9A.22: every package's declared event kinds equal its compiled rows' kinds (EventRingProducerTests).
        // C-Lamp+Detector: the lamp's declared indicator input selects its node, and the detector's declared f32 aperture is the
        // compiled passage sensor's radius and rearm clearance, narrowed to the binary16 lanes bit-equal to the former record's
        // (Half)0.65 and (Half)0.02.
        Assert.Equal(Abstractions.PortSocket.ActivationIn, ((Abstractions.Signal.IIndicatorElement)packages["lamp"]).Indicator.Input);
        var aperture = ((Abstractions.Signal.IPassageSensorElement)packages["ball_detector"]).Passage;
        Assert.Equal((.65f, .02f), (aperture.ApertureRadius, aperture.RearmClearance));
        var sensor = Assert.Single(WorkshopActivationCompiler.Compile(new WorkshopConstruction(new ConstructionRevision(1),
            WorkshopCadenceSettings.Default(), new WorkshopInstances(Placed(PartId.BallDetector)))).PassageSensors.ToArray());
        Assert.Equal(((ushort)0x3933, (ushort)0x251f), (BitConverter.HalfToUInt16Bits(sensor.Radius.Value), BitConverter.HalfToUInt16Bits(sensor.RearmClearance.Value)));
        Assert.Equal((BitConverter.HalfToUInt16Bits((Half)0.65), BitConverter.HalfToUInt16Bits((Half)0.02)),
            (BitConverter.HalfToUInt16Bits(sensor.Radius.Value), BitConverter.HalfToUInt16Bits(sensor.RearmClearance.Value)));
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
            AssertColliders(package.Package, TestBundle.Entry(id).Colliders, colliders.Select(collider => (collider.Shape, collider.Pose, collider.Radius.Value,
                collider.HalfExtents)).ToArray());
            AssertMaterials(package.Package, TestBundle.Entry(id).Colliders, colliders, scene.Materials.ToArray());

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

    /// <summary>Stories 9A.6, 9A.30 and 9A.31: the Basketball and Bowling ball packages are the only payload packages, and each placed
    /// generic instance compiles exactly its package's declared material (radius, mass, bounce, drag, friction, bounce threshold, rolling
    /// resistance) through the shared dynamic-body compile. Neither declares a fluid payload, so neither is buoyant.</summary>
    [Fact]
    public void EveryPayloadPackageCompilesItsDeclaredMaterial()
    {
        var balls = Packages().Where(package => DeclaredPhysics.IsPayload(new PartId(package.Declaration.Metadata.Id))).ToArray();
        Assert.Equal(["ball", "bowling"], balls.Select(package => package.Declaration.Metadata.Id).Order(StringComparer.Ordinal));
        foreach (var package in balls)
        {
            var ball = (IMechanicalElement)package.Declaration;
            var id = new PartId(ball.Metadata.Id);
            var collider = Assert.Single(TestBundle.Entry(id).Colliders);
            Assert.Equal(AbstractShape.Sphere, collider.ShapeKind);
            Assert.Null(TestBundle.Entry(id).Fluid);
            Assert.Empty(ball.Metadata.Parameters);
            var scene = Compile(Placed(id));
            var body = Assert.Single(scene.Bodies.ToArray(), row => row.Id == Owner);
            var sphere = Assert.Single(scene.Colliders.ToArray(), row => row.Body == Owner);
            var material = Assert.Single(scene.Materials.ToArray(), row => row.Id == sphere.Material);
            Assert.True(Same(collider.HalfExtents.X, sphere.Radius.Value) && Same(ball.MassProperties.Mass, body.Mass.Value) &&
                Same(collider.Material.Restitution, material.Restitution.Value) && Same(ball.MassProperties.LinearDrag, body.LinearDrag.Value) &&
                Same(collider.Material.Friction, material.Friction.Value) && Same(collider.Material.BounceThreshold, material.BounceThreshold.Value) &&
                Same(collider.Material.RollingResistance, material.RollingResistance.Value), $"{package.Package}: the compiled material differs from the package.");
            Assert.Equal(RigidMotionKind.Dynamic, body.Motion);
        }
    }

    /// <summary>Story 9A.12: each slider package declares the compiled prismatic joint and its moving body (frames, limits, collision,
    /// mass, drag, gravity, colliders and material), and the joint's stiffness, damping and starting compression come from the parameters
    /// the slider names. The part is compiled with every parameter at its declared maximum, so each key must bind its own value.</summary>
    [Fact]
    public void EverySliderPackageDeclaresTheCompiledPrismaticJointAndMovingBody()
    {
        var sliders = Packages().Where(package => package.Declaration is ISliderElement).ToArray();
        Assert.Equal(["PoweredGate", "Springboard"], sliders.Select(package => package.Package));
        foreach (var package in sliders)
        {
            var element = (ISliderElement)package.Declaration;
            var id = new PartId(element.Metadata.Id);
            var maxima = element.Metadata.Parameters.ToDictionary(parameter => parameter.Key, parameter => parameter.Maximum, StringComparer.Ordinal);
            var placed = Placed(id, maxima);
            var scene = Compile(placed);
            var joints = scene.Prismatics.ToArray();
            var declaredSliders = TestBundle.Entry(id).Mechanical!.Sliders;
            Assert.True(joints.Length == declaredSliders.Count, $"{package.Package}: {element.Sliders.Count} sliders declared, {joints.Length} compiled.");
            for (var i = 0; i < joints.Length; i++)
            {
                var (slider, joint, where) = (declaredSliders[i], joints[i], $"{package.Package}.Sliders[{i}]");
                Assert.Equal(Owner, joint.BodyA);
                AssertFrame(where + ".BaseFrame", slider.BaseFrame, joint.FrameA);
                AssertFrame(where + ".MovingFrame", slider.MovingFrame, joint.FrameB);
                Assert.True(Bits(slider.LowerLimit) == Bits(joint.Lower) && Bits(slider.UpperLimit) == Bits(joint.Upper),
                    $"{where}: limits [{slider.LowerLimit}, {slider.UpperLimit}] declared, [{joint.Lower}, {joint.Upper}] compiled.");
                Assert.True(Bits(slider.Stiffness.Evaluate(maxima)) == Bits(joint.Stiffness), $"{where}.StiffnessParameter binds {joint.Stiffness}.");
                Assert.True(Bits(slider.Damping.Evaluate(maxima)) == Bits(joint.Damping), $"{where}.DampingParameter binds {joint.Damping}.");
                Assert.Equal(slider.BodiesCollide ? ConnectedCollision.Enabled : ConnectedCollision.Disabled, joint.Collision);
                var moving = scene.Bodies.ToArray().Single(body => body.Id == joint.BodyB);
                AssertBody(where + ".MovingBody", KinematicMode.Dynamic, slider.MovingBody.Mass, slider.MovingBody.LinearDrag,
                    slider.MovingBody.Gravity, moving);
                // The moving body starts at the base frame, CompressionParameter below it along local Y (an unrotated placement).
                var compression = slider.Compression.Evaluate(maxima);
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

    [Fact]
    public void CompiledGeometryMatchesPreCutoverGolden()
    {
        _ = godot;
        var options = new System.Text.Json.JsonSerializerOptions { IncludeFields = true };
        var rows = new List<System.Text.Json.Nodes.JsonNode>();
        // The immutable 9A.16 baseline predates Hold, the supplied gates and the Weight; their new geometry is checked separately.
        foreach (var package in Packages().Where(package => package.Declaration.Metadata.Id is not ("hold_timer" or "both_gate" or "electrical_nand" or "electrical_nor" or "electrical_or" or "electrical_xor" or "weight")).OrderBy(package => package.Package, StringComparer.Ordinal))
        {
            Capture(package.Declaration.Metadata.Id, Placed(new PartId(package.Declaration.Metadata.Id)));
        }
        foreach (var length in new[] { 1f, 3.6f, 8f })
            Capture("pipe/" + length.ToString(System.Globalization.CultureInfo.InvariantCulture),
                WorkshopInput.Pipe(Owner, 0, 3, 0, 0, 0, 0, 1, new PipeLength(length)));

        var expected = System.Text.Json.Nodes.JsonNode.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(
            twodog.Engine.ResolveProjectDir(), "CuriousContraptions.tests/Fixtures/compiled-geometry-9a16.json")));
        var actual = new System.Text.Json.Nodes.JsonArray(rows.ToArray());
        Assert.True(System.Text.Json.Nodes.JsonNode.DeepEquals(expected, actual), "Compiled physical rows differ from the pre-cutover golden.");

        void Capture(string key, IWorkshopInstance instance)
        {
            var scene = Compile(instance);
            rows.Add(System.Text.Json.JsonSerializer.SerializeToNode(new
            {
                Key = key,
                Bodies = scene.Bodies.ToArray(),
                Colliders = scene.Colliders.ToArray(),
                Materials = scene.Materials.ToArray(),
                Prismatics = scene.Prismatics.ToArray()
            }, options)!);
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

    /// <summary>The runtime parameters a newly placed part starts from (MachineWorld.CaptureInstance) and the bounds its record admits.</summary>
    private static (string Key, ParameterUnit Unit, ParameterKind Kind, Lane Lane, float Default, Func<float, bool> Admits, string[] Options)[] Parameters(string id)
    {
        Func<float, bool> Generic(PartId part, string key) => Admits(value =>
        {
            var values = InstanceParameterCollection.Defaults(part).ToDictionary();
            values[new(key)] = value;
            WorkshopInput.Generic(part, Owner, 0, 3, 0, 0, 0, 0, 1, new(values)).Validate();
        });
        Func<float, bool> Wall(string key) => Admits(value =>
        {
            var values = WallFixture.Default.ToDictionary(pair => pair.Key, pair => pair.Value);
            values[new(key)] = value;
            WorkshopInput.Generic(PartId.Wall, Owner, 0, 3, 0, 0, 0, 0, 1, new(values));
        });
        var enables = Enum.GetValues<ElectricalEnable>();
        return id switch
        {
            "ramp" =>
            [
                ("length", ParameterUnit.Metre, ParameterKind.Real, Lane.Single, 3f, Generic(PartId.Ramp, "length"), []),
                ("width", ParameterUnit.Metre, ParameterKind.Real, Lane.Single, 1.3f, Generic(PartId.Ramp, "width"), [])
            ],
            "wall" =>
            [
                ("width", ParameterUnit.Metre, ParameterKind.Real, Lane.Single, 3f, Wall("width"), []),
                ("height", ParameterUnit.Metre, ParameterKind.Real, Lane.Single, 2f, Wall("height"), []),
                ("thickness", ParameterUnit.Metre, ParameterKind.Real, Lane.Single, .25f, Wall("thickness"), [])
            ],
            "bumper" =>
            [
                ("strength", ParameterUnit.MetrePerSecond, ParameterKind.Real, Lane.Single, 8f, Generic(PartId.PinballBumper, "strength"), [])
            ],
            "battery" =>
            [
                // The owner defaults of 10 Oct 2026 (3600 J, 120 W, full, Enabled); the enable options are the runtime enum's names,
                // in its order, so the declared index is the compiled state.
                ("capacity", ParameterUnit.Joule, ParameterKind.Real, Lane.Single, 3600f, Generic(PartId.Battery, "capacity"), []),
                ("power_limit", ParameterUnit.Watt, ParameterKind.Real, Lane.Single, 120f, Generic(PartId.Battery, "power_limit"), []),
                ("initial_charge", ParameterUnit.None, ParameterKind.Real, Lane.Single, 1f, Generic(PartId.Battery, "initial_charge"), []),
                ("enabled", ParameterUnit.None, ParameterKind.Choice, Lane.Whole, Array.IndexOf(enables, ElectricalEnable.Enabled),
                    Generic(PartId.Battery, "enabled"), Enum.GetNames<ElectricalEnable>())
            ],
            "pressure_plate" =>
            [
                ("minimum_mass", ParameterUnit.Kilogram, ParameterKind.Real, Lane.Single, .5f,
                    Admits(value => PlacedParts.Plate(Owner, 0, 3, 0, value)), [])
            ],
            "counter" =>
            [
                ("target_count", ParameterUnit.None, ParameterKind.WholeNumber, Lane.Whole, 3f,
                    Generic(PartId.Counter, "target_count"), [])
            ],
            "clock" =>
            [
                ("interval_seconds", ParameterUnit.Second, ParameterKind.Real, Lane.Single, 1f,
                    Admits(value => PlacedParts.Clock(Owner, 0, 3, 0, 0, 0, 0, 1, value)), [])
            ],
            "basket" =>
            [
                ("capture_margin", ParameterUnit.Metre, ParameterKind.Real, Lane.Single, .02f, Generic(PartId.Receiver, "capture_margin"), []),
                ("capture_speed_limit", ParameterUnit.MetrePerSecond, ParameterKind.Real, Lane.Single, 1.5f, Generic(PartId.Receiver, "capture_speed_limit"), []),
                ("capture_dwell", ParameterUnit.Second, ParameterKind.Real, Lane.Single, .35f, Generic(PartId.Receiver, "capture_dwell"), []),
                ("capture_participation", ParameterUnit.None, ParameterKind.Choice, Lane.Whole, 1f, Generic(PartId.Receiver, "capture_participation"), Enum.GetNames<SensorParticipation>()),
                ("force_support_margin", ParameterUnit.Metre, ParameterKind.Real, Lane.Single, 0f, Generic(PartId.Receiver, "force_support_margin"), []),
                ("force_maximum_acceleration", ParameterUnit.MetrePerSecondSquared, ParameterKind.Real, Lane.Single, 0f, Generic(PartId.Receiver, "force_maximum_acceleration"), [])
            ],
            "spring" =>
            [
                ("stiffness", ParameterUnit.NewtonPerMetre, ParameterKind.Real, Lane.Single, 400f, Generic(PartId.Springboard, "stiffness"), []),
                ("damping", ParameterUnit.NewtonSecondPerMetre, ParameterKind.Real, Lane.Single, .2f, Generic(PartId.Springboard, "damping"), []),
                ("initial_compression", ParameterUnit.Metre, ParameterKind.Real, Lane.Single, 0f, Generic(PartId.Springboard, "initial_compression"), [])
            ],
            "pipe" =>
            [
                ("length", ParameterUnit.Metre, ParameterKind.Real, Lane.Single, PipeLength.Default.Metres,
                    Admits(value => new PipeLength(value).Validate()), [])
            ],
            "delay" =>
            [
                ("delay_seconds", ParameterUnit.Second, ParameterKind.Real, Lane.Single, 1f,
                    Admits(value => WorkshopInput.Generic(PartId.Delay, Owner, 0, 3, 0, 0, 0, 0, 1, new InstanceParameterCollection([new(new ParameterId("delay_seconds"), (float)value)]))), [])
            ],
            "hold_timer" =>
            [
                ("hold_seconds", ParameterUnit.Second, ParameterKind.Real, Lane.Single, 2f,
                    Admits(value => WorkshopInput.Generic(new("hold_timer"), Owner, 0, 3, 0, 0, 0, 0, 1, new InstanceParameterCollection([new(new ParameterId("hold_seconds"), (float)value)]))), [])
            ],
            "weight" =>
            [
                ("mass", ParameterUnit.Kilogram, ParameterKind.Real, Lane.Single, 4f,
                    Admits(value => WorkshopInput.Generic(new("weight"), Owner, 0, 3, 0, 0, 0, 0, 1, new([new(new ParameterId("mass"), (float)value)]))), [])
            ],
            "switch" =>
            [
                ("trigger_threshold", ParameterUnit.MetrePerSecond, ParameterKind.Real, Lane.Single, .8f,
                    Admits(value => WorkshopInput.Generic(PartId.ImpactSwitch, Owner, 0, 3, 0, 0, 0, 0, 1, new([new(new ParameterId("trigger_threshold"), (float)value)]))), [])
            ],
            _ => []
        };
    }

    /// <summary>One newly placed instance of the part, as MachineWorld.CaptureInstance builds it.</summary>
    internal static IWorkshopInstance Placed(PartId id) => id.Value switch
    {
        "ball" => WorkshopInput.Generic(PartId.Basketball,Owner, 0, 3, 0, 0, 0, 0, 1),
        "bowling" => WorkshopInput.Generic(PartId.BowlingBall, Owner, 0, 3, 0, 0, 0, 0, 1),
        "basket" => WorkshopInput.Generic(PartId.Receiver, Owner, 0, 3, 0, 0, 0, 0, 1),
        "spring" => WorkshopInput.Generic(PartId.Springboard, Owner, 0, 3, 0, 0, 0, 0, 1),
        "ramp" => WorkshopInput.Generic(PartId.Ramp, Owner, 0, 3, 0, 0, 0, 0, 1),
        "wall" => WorkshopInput.Generic(PartId.Wall, Owner, 0, 3, 0, 0, 0, 0, 1),
        "battery" => PlacedParts.Battery(Owner, 0, 3, 0),
        "bumper" => WorkshopInput.Generic(PartId.PinballBumper, Owner, 0, 3, 0, 0, 0, 0, 1),
        "domino" => PlacedParts.Domino(Owner, 0, 3, 0, 0, 0, 0, 1),
        "pipe" => WorkshopInput.Pipe(Owner, 0, 3, 0, 0, 0, 0, 1),
        "lamp" or "ball_detector" or "funnel" or "pipe_bend_45" or "pipe_bend_90" or "powered_gate" or "hold_timer" or "both_gate" or "electrical_nand" or "electrical_nor" or "electrical_or" or "electrical_xor" or "weight" =>
            WorkshopInput.Generic(id, Owner, 0, 3, 0, 0, 0, 0, 1),
        "pressure_plate" => PlacedParts.Plate(Owner, 0, 3, 0, .5f),
        "counter" => PlacedParts.Counter(Owner, 0, 3, 0, 0, 0, 0, 1, 3),
        "clock" => PlacedParts.Clock(Owner, 0, 3, 0, 0, 0, 0, 1, 1f),
        "latch" => PlacedParts.Latch(Owner, 0, 3, 0, 0, 0, 0, 1),
        "switch" => WorkshopInput.Generic(PartId.ImpactSwitch, Owner, 0, 3, 0, 0, 0, 0, 1),
        "delay" => WorkshopInput.Generic(PartId.Delay, Owner, 0, 3, 0, 0, 0, 0, 1),
        _ => throw new ArgumentException($"No placement for packaged part '{id}'.")
    };

    /// <summary>One placed slider part with its declared parameters set to these values.</summary>
    private static IWorkshopInstance Placed(PartId id, Dictionary<string, float> parameters) => id.Value switch
    {
        "spring" => PlacedParts.Springboard(Owner, 0, 3, 0, 0, 0, 0, 1, parameters["initial_compression"],
            parameters["stiffness"], parameters["damping"]),
        "powered_gate" => Placed(id),
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
                AbstractShape.Cylinder => RuntimeShape.Cylinder,
                _ => throw new ArgumentException($"{where}: the runtime has no {collider.ShapeKind} collider.")
            }, $"{where}: shape {collider.ShapeKind} declared, {shape} at runtime.");
            Assert.True(Same(collider.Offset.X, pose.Translation.X) && Same(collider.Offset.Y, pose.Translation.Y) &&
                Same(collider.Offset.Z, pose.Translation.Z), $"{where}: offset {collider.Offset} declared, {pose.Translation} at runtime.");
            Assert.True(Bits(collider.Rotation.X) == Bits(pose.Rotation.X) && Bits(collider.Rotation.Y) == Bits(pose.Rotation.Y) &&
                Bits(collider.Rotation.Z) == Bits(pose.Rotation.Z) && Bits(collider.Rotation.W) == Bits(pose.Rotation.W),
                $"{where}: rotation {collider.Rotation} declared, {pose.Rotation} at runtime.");
            var extents = collider.HalfExtents;
            // A sphere is its radius on every lane; a cylinder (r, h, r) is its radius and its half-height lane only (10.1).
            var matches = shape switch
            {
                RuntimeShape.Sphere => Same(extents.X, radius) && Bits(extents.Y) == Bits(extents.X) && Bits(extents.Z) == Bits(extents.X) &&
                    Same(0f, halfExtents.X) && Same(0f, halfExtents.Y) && Same(0f, halfExtents.Z),
                RuntimeShape.Cylinder => Same(extents.X, radius) && Bits(extents.Z) == Bits(extents.X) &&
                    Same(0f, halfExtents.X) && Same(extents.Y, halfExtents.Y) && Same(0f, halfExtents.Z),
                _ => Same(extents.X, halfExtents.X) && Same(extents.Y, halfExtents.Y) && Same(extents.Z, halfExtents.Z) && Same(0f, radius)
            };
            Assert.True(matches, $"{where}: half-extents {extents} declared, radius {radius} and {halfExtents} at runtime.");
        }
    }

    private static int Bits(float value) => BitConverter.SingleToInt32Bits(value);
    private static bool Same(float declared, Half runtime) => BitConverter.HalfToInt16Bits((Half)declared) == BitConverter.HalfToInt16Bits(runtime);
}
