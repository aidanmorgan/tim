// Deleted by Story 9A.14 with the tables it compares.
using System;
using System.Collections.Generic;
using System.Linq;
using CuriousContraptions.Abstractions;
using CuriousContraptions.BundleElements;
using CuriousContraptions.Gpu;
using Godot;
using AbstractShape = CuriousContraptions.Abstractions.ColliderShapeKind;
using RuntimeShape = CuriousContraptions.Gpu.ColliderShapeKind;

namespace CuriousContraptions.Tests;

/// <summary>Story 9A.4 AC6 and AC7, the temporary cutover guard: every packaged element's metadata, category, ports, colliders,
/// footprint, contact material and parameter defaults equal today's runtime values, so the later cutover to the bundle changes nothing
/// the player sees. Values are compared at the runtime lane's precision (spec D2): package f32 values narrowed to the runtime's binary16
/// lanes, and f32 rotations and port positions bit for bit. The runtime body, collider and material rows come from one compiled
/// instance of each part built with the canonical colliders (WorkshopCanonicalColliders.For, as the AC names). The detector's and
/// gate's scene-extracted colliders, which MachineWorld.CaptureInstance prefers, are not compared: the detector's scene ring differs
/// from the canonical table (filed to Story 9A.16, which bakes scene colliders at build time).</summary>
[Collection<NativeSceneCollectionDefinition>]
public sealed class BundleCatalogueParityTests(NativeSceneFixture godot)
{
    private static readonly GpuBodyId Owner = new(1);

    private static IReadOnlyList<BundlePackage> Packages()
    {
        var discovered = PackageDiscovery.Discover(twodog.Engine.ResolveProjectDir());
        Assert.Empty(discovered.Errors);
        return discovered.Packages;
    }

    private static PartRegistry Registry()
    {
        var registry = new PartRegistry();
        registry.Discover();
        return registry;
    }

    [Fact]
    public void EveryPackageMatchesItsCatalogueMetadataAndCategory()
    {
        _ = godot;
        var registry = Registry();
        foreach (var package in Packages())
        {
            var metadata = package.Declaration.Metadata;
            Assert.True(registry.Definitions.TryGetValue(new PartId(metadata.Id), out var definition), $"{package.Package}: '{metadata.Id}' is not in the catalogue.");
            Assert.Equal(Enum.Parse<ElementCategory>(definition.Category), metadata.Category);
            Assert.Equal(definition.Scene.ResourcePath, metadata.ScenePath);
            Assert.Null(metadata.IconPath);
        }
    }

    [Fact]
    public void TheCatalogueIdsWithoutAPackageAreExactlyTheElevenListed()
    {
        var catalogue = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var file in DirAccess.GetFilesAt("res://parts/catalog").Where(file => file.EndsWith(".tres", StringComparison.Ordinal)))
        {
            using var definition = ResourceLoader.Load<PartDefinition>("res://parts/catalog/" + file);
            catalogue.Add(definition.Id);
        }
        var packaged = Packages().Select(package => package.Declaration.Metadata.Id).ToHashSet(StringComparer.Ordinal);
        Assert.Subset(catalogue, packaged);
        Assert.Equal(["balloon", "basket", "bowling", "delay", "funnel", "pipe", "pipe_bend_45", "pipe_bend_90", "spring", "switch", "tennis"],
            catalogue.Where(id => !packaged.Contains(id)));
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
        _ = Registry(); // captures the catalogue ball materials the ball's compile reads
        foreach (var package in Packages())
        {
            var declaration = package.Declaration;
            var id = new PartId(declaration.Metadata.Id);
            var scene = WorkshopPhysicsCompiler.Compile(new WorkshopConstruction(new ConstructionRevision(1), WorkshopCadenceSettings.Default(),
                new WorkshopInstances(Placed(id))), new(1, 2));
            // The workbench plane (the body of the one plane collider) belongs to no part.
            var plane = scene.Colliders.ToArray().Single(collider => collider.Shape == RuntimeShape.Plane).Body;
            var bodies = scene.Bodies.ToArray().Where(body => body.Id != plane).ToArray();
            Assert.True(new ElementFootprint(bodies.Length, bodies.Count(body => body.Motion != RigidMotionKind.Static), scene.Prismatics.Length) ==
                declaration.Footprint, $"{package.Package}: footprint {declaration.Footprint} declared.");
            var colliders = scene.Colliders.ToArray().Where(collider => collider.Body == Owner).ToArray();
            var materials = scene.Materials.ToArray();
            AssertColliders(package.Package, declaration.Colliders, colliders.Select(collider => (collider.Shape, collider.Pose, collider.Radius.Value,
                collider.HalfExtents)).ToArray());
            for (var i = 0; i < colliders.Length; i++)
            {
                var runtime = materials.Single(material => material.Id == colliders[i].Material);
                var declared = declaration.Colliders[i].Material;
                Assert.True(Same(declared.Restitution, runtime.Restitution.Value) && Same(declared.BounceThreshold, runtime.BounceThreshold.Value) &&
                    Same(declared.Friction, runtime.Friction.Value) && Same(declared.RollingResistance, runtime.RollingResistance.Value),
                    $"{package.Package}.Colliders[{i}].Material: {declared} declared, {runtime} at runtime.");
            }
            if (Canonical.Contains(id.Value))
                AssertColliders(package.Package, declaration.Colliders, WorkshopCanonicalColliders.For(id)
                    .Select(collider => (collider.ShapeKind, collider.LocalPose, collider.Radius, collider.HalfExtents)).ToArray());
        }
    }

    [Fact]
    public void EveryPackageDeclaresTheRuntimeParameterDefaultsAndBounds()
    {
        var registry = Registry();
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
            // The catalogue's own untyped parameters (the clock's interval) agree with the typed schema.
            foreach (var (key, value) in registry.Definitions[new PartId(id)].Parameters)
                Assert.Equal(value, declared.Single(parameter => parameter.Key == key).Default);
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

    private static readonly string[] Canonical = ["ball_detector", "powered_gate", "pressure_plate", "counter", "clock", "latch"];

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
            _ => []
        };
    }

    /// <summary>One newly placed instance of the part, as MachineWorld.CaptureInstance builds it.</summary>
    private static IWorkshopInstance Placed(PartId id) => id.Value switch
    {
        "ball" => WorkshopInput.Basketball(Owner, 0, 3, 0, 0, 0, 0, 1),
        "ramp" => WorkshopInput.Ramp(Owner, 0, 3, 0, 0, 0, 0, 1, RampDimensions.Default),
        "wall" => WorkshopInput.Wall(Owner, 0, 3, 0, 0, 0, 0, 1, WallDimensions.Default),
        "battery" => WorkshopInput.Battery(Owner, 0, 3, 0, 0, 0, 0, 1, ElectricalSourceSettings.Default),
        "bumper" => WorkshopInput.Bumper(Owner, 0, 3, 0, 0, 0, 0, 1, BumperWork.Default),
        "lamp" => WorkshopInput.Lamp(Owner, 0, 3, 0, 0, 0, 0, 1),
        "domino" => WorkshopInput.Domino(Owner, 0, 3, 0, 0, 0, 0, 1),
        "ball_detector" => WorkshopInput.BallDetector(Owner, 0, 3, 0, 0, 0, 0, 1),
        "powered_gate" => WorkshopInput.Generic(id, Owner, 0, 3, 0, 0, 0, 0, 1, WorkshopCanonicalColliders.For(id)),
        "pressure_plate" => WorkshopInput.PressurePlate(Owner, 0, 3, 0, 0, 0, 0, 1, WorkshopPressurePlate.PlacedMinimumMass),
        "counter" => WorkshopInput.Counter(Owner, 0, 3, 0, 0, 0, 0, 1, WorkshopCounter.PlacedTargetCount),
        "clock" => WorkshopInput.Clock(Owner, 0, 3, 0, 0, 0, 0, 1, WorkshopClock.PlacedIntervalSeconds),
        "latch" => WorkshopInput.Latch(Owner, 0, 3, 0, 0, 0, 0, 1),
        _ => throw new ArgumentException($"No placement for packaged part '{id}'.")
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
