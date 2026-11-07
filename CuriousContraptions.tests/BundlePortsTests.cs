using System.Numerics;
using System.Text.RegularExpressions;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Bundle;
using CuriousContraptions.BundleElements;
using CuriousContraptions.Gpu;
using Godot;
using Vector3 = System.Numerics.Vector3;

[assembly: CollectionBehavior(typeof(CuriousContraptions.Tests.NativeSceneCollectionFactory), DisableTestParallelization = true)]

namespace CuriousContraptions.Tests;

/// <summary>The element bundle as the tests see it: every discovered package, written and read back through the same bundle schema
/// and validation the game runs at load (Story 9A.17).</summary>
internal static class TestBundle
{
    public static IReadOnlyList<BundlePackage> Packages { get; } = Discover();

    public static string Json { get; } = BundleGenerator.Generate(new CuriousContraptions.Bundle.BundleServices(), twodog.Engine.ResolveProjectDir()).Json ?? throw new InvalidOperationException("Test bundle failed to bake.");

    public static IReadOnlyList<BundleEntry> Entries { get; } = PartRegistry.Read(RuntimeTestServices.Bundle, Json);

    public static ElementPortCatalogue Ports { get; } = ElementPortCatalogue.FromBundle(Entries);

    public static BundleEntry Entry(PartId id) => Entries.Single(entry => entry.Metadata.Id == id.Value);

    public static (IReadOnlyList<ExtractedCollider> Colliders, IReadOnlyDictionary<string, ExtractedPortLocator> Ports) SceneGeometry(Node3D root)
    {
        var entry = Entries.Single(entry => entry.Metadata.ScenePath == root.SceneFilePath);
        var colliders = BundleGeometry.Colliders(entry).Select(DeclaredPhysics.Admit).ToArray();
        var ports = new Dictionary<string, ExtractedPortLocator>(StringComparer.Ordinal);
        foreach (var child in root.GetChildren().OfType<Node3D>())
        {
            var name = child.Name.ToString();
            if (!name.StartsWith("Loc_Port_", StringComparison.Ordinal)) continue;
            var key = name["Loc_Port_".Length..];
            var mouth = entry.TubeMouths.Where(m => key == (m.End == TubeEnd.Start ? "mouth_start" : "mouth_end")).ToArray();
            ports.Add(key, new(key, child.Position, child.Quaternion,
                mouth.Length == 0 ? null : mouth[0].End == TubeEnd.Start ? TubeMouthEnd.Start : TubeMouthEnd.End,
                mouth.Length == 0 ? null : mouth[0].BoreRadius));
        }
        return (colliders, ports);
    }

    /// <summary>Asserts one extracted collider's kind, radius (zero for a box), half-extents (zero for a sphere) and centre, the values
    /// a scene test reads from its element doc.</summary>
    public static void AssertCollider(ExtractedCollider collider, Gpu.ColliderShapeKind kind, float radius, MetreVector half, MetreVector centre)
    {
        Assert.Equal((kind, radius, half), (collider.ShapeKind, collider.Radius, collider.HalfExtents));
        Assert.Equal(centre, collider.LocalPose.Translation);
    }

    /// <summary>The f32 agreement of a hollow lane with its element doc's value: a lane is a single scene number or one f32 difference
    /// (a wall's r − t), within two roundings at up to 4 m, 2 × 2.4e-7 → 1e-6 m.</summary>
    private const float HollowLaneTolerance = 1e-6f;

    /// <summary>Asserts one extracted hollow collider's kind, its radius lane and three lanes, its centre and its axis (local +Y),
    /// against the values its element doc states.</summary>
    public static void AssertHollow(ExtractedCollider collider, Gpu.ColliderShapeKind kind, float radius, Vector3 lanes, Vector3 centre,
        Vector3 axis)
    {
        Assert.Equal(kind, collider.ShapeKind);
        var t = collider.LocalPose.Translation;
        var r = collider.LocalPose.Rotation;
        var actualAxis = Vector3.Transform(Vector3.UnitY, new System.Numerics.Quaternion(r.X, r.Y, r.Z, r.W));
        Assert.True(MathF.Abs(collider.Radius - radius) <= HollowLaneTolerance &&
            MathF.Abs(collider.HalfExtents.X - lanes.X) <= HollowLaneTolerance && MathF.Abs(collider.HalfExtents.Y - lanes.Y) <= HollowLaneTolerance &&
            MathF.Abs(collider.HalfExtents.Z - lanes.Z) <= HollowLaneTolerance,
            $"{kind}: radius {collider.Radius} and lanes {collider.HalfExtents}, not {radius} and {lanes}");
        Assert.True(Vector3.Distance(new(t.X, t.Y, t.Z), centre) <= 1e-5f, $"{kind}: centre {t}, not {centre}");
        Assert.True(MathF.Abs(Vector3.Dot(actualAxis, Vector3.Normalize(axis))) >= 1f - 1e-6f, $"{kind}: axis {actualAxis}, not along {axis}");
    }

    private static IReadOnlyList<BundlePackage> Discover()
    {
        var discovered = PackageDiscovery.Discover(twodog.Engine.ResolveProjectDir());
        if (discovered.Errors.Count > 0) throw new InvalidOperationException(string.Join("; ", discovered.Errors));
        return discovered.Packages;
    }
}

/// <summary>Story 9A.17: every element's sockets, directions, domains, signal kinds and anchor positions come only from its bundle
/// <see cref="PortDescriptor"/> list, and its footprint only from its declaration.</summary>
public sealed class BundlePortsTests
{
    private static WorkshopPort Activation(PortId socket, WorkshopPortDirection direction, WorkshopSignalKind signal, float x, float y, float z) =>
        new(socket, Enum.Parse<PortRole>(socket.Value), WorkshopConnectionDomain.Activation, direction, signal, new(x, y, z));
    private static WorkshopPort Electrical(PortId socket, WorkshopPortDirection direction, float x, float y, float z) =>
        new(socket, Enum.Parse<PortRole>(socket.Value), WorkshopConnectionDomain.Electrical, direction, null, new(x, y, z));

    private const WorkshopPortDirection In = WorkshopPortDirection.Input;
    private const WorkshopPortDirection Out = WorkshopPortDirection.Output;
    private const WorkshopSignalKind Pulse = WorkshopSignalKind.Pulse;
    private const WorkshopSignalKind Level = WorkshopSignalKind.Level;

    /// <summary>The golden table: the runtime's ports (in drawer order) and every declared footprint at the start of Story 9A.17,
    /// copied from the deleted engine port switch tables and the packages.</summary>
    private static readonly Dictionary<string, (WorkshopPort[] Ports, ElementFootprint Footprint)> Golden = new(StringComparer.Ordinal)
    {
        ["both_gate"] = ([Electrical(PortId.FirstIn, In, -.94f, .35f, 0f), Electrical(PortId.SecondIn, In, -.94f, -.35f, 0f), Electrical(PortId.PowerIn, In, 0f, -.75f, .55f), Electrical(PortId.Supply, Out, .94f, 0f, 0f)], new(1, 0, 0, 0)),
        ["electrical_nand"] = ([Electrical(PortId.FirstIn, In, -.94f, .35f, 0f), Electrical(PortId.SecondIn, In, -.94f, -.35f, 0f), Electrical(PortId.PowerIn, In, 0f, -.75f, .55f), Electrical(PortId.Supply, Out, .94f, 0f, 0f)], new(1, 0, 0, 0)),
        ["electrical_nor"] = ([Electrical(PortId.FirstIn, In, -.94f, .35f, 0f), Electrical(PortId.SecondIn, In, -.94f, -.35f, 0f), Electrical(PortId.PowerIn, In, 0f, -.75f, .55f), Electrical(PortId.Supply, Out, .94f, 0f, 0f)], new(1, 0, 0, 0)),
        ["electrical_or"] = ([Electrical(PortId.FirstIn, In, -.94f, .35f, 0f), Electrical(PortId.SecondIn, In, -.94f, -.35f, 0f), Electrical(PortId.PowerIn, In, 0f, -.75f, .55f), Electrical(PortId.Supply, Out, .94f, 0f, 0f)], new(1, 0, 0, 0)),
        ["electrical_xor"] = ([Electrical(PortId.FirstIn, In, -.94f, .35f, 0f), Electrical(PortId.SecondIn, In, -.94f, -.35f, 0f), Electrical(PortId.PowerIn, In, 0f, -.75f, .55f), Electrical(PortId.Supply, Out, .94f, 0f, 0f)], new(1, 0, 0, 0)),
        ["ball"] = ([], new(1, 1, 0, 0)),
        ["ball_detector"] = ([Activation(PortId.ActivationOut, Out, Pulse, 0f, 1.12f, 0f)], new(1, 0, 0, 0)),
        ["basket"] = ([], new(1, 0, 0, 0)),
        ["battery"] = ([Electrical(PortId.Supply, Out, 0f, .62f, 0f)], new(1, 0, 0, 0)),
        ["bowling"] = ([], new(1, 1, 0, 0)),
        ["weight"] = ([new(PortId.Tie, PortRole.Tie, WorkshopConnectionDomain.Rope, WorkshopPortDirection.Bidirectional, null,
            new(0f, 4f * (.5771f * .136734f) + .5771f * .453070f, 0f), RopeAttachmentRole.Load,
            new(new("mass"), new(0f, .5771f * .136734f, 0f), new(0f, .5771f * .453070f, 0f)))], new(1, 1, 0, 0)),
        ["rope_anchor"] = ([new(PortId.Tie, PortRole.Tie, WorkshopConnectionDomain.Rope, WorkshopPortDirection.Bidirectional, null,
            new(0f, 0f, .18f), RopeAttachmentRole.Anchor)], new(1, 0, 0, 0)),
        ["pulley"] = ([new(PortId.Tie, PortRole.Tie, WorkshopConnectionDomain.Rope, WorkshopPortDirection.Bidirectional, null,
            new(0f, .4f, .12f), RopeAttachmentRole.Guide, GuideRadius: 0.4f, GuideOffset: new(0f, 0f, .12f))], new(1, 0, 0, 0)),
        ["impact_lever"] = ([new(PortId.TieA, PortRole.TieA, WorkshopConnectionDomain.Rope, WorkshopPortDirection.Bidirectional, null,
            new(-1.8f, 0f, 0f), RopeAttachmentRole.Load, null, new(1)), new(PortId.TieB, PortRole.TieB, WorkshopConnectionDomain.Rope, WorkshopPortDirection.Bidirectional, null,
            new(1.8f, 0f, 0f), RopeAttachmentRole.Load, null, new(1))], new(2, 1, 0, 1)),
        ["trampoline"] = ([], new(1, 0, 0, 0)),
        ["motor"] = ([Electrical(PortId.PowerIn, In, -.55f, 0f, 0f)], new(2, 1, 0, 1)),
        ["bumper"] = ([Electrical(PortId.PowerIn, In, 0f, 0f, .65f)], new(1, 0, 0, 0)),
        ["clock"] = ([Electrical(PortId.PowerIn, In, -.78f, 0f, 0f), Activation(PortId.ActivationOut, Out, Pulse, .78f, 0f, 0f)],
            new(1, 0, 0, 0)),
        ["counter"] = ([Activation(PortId.ActivationIn, In, Pulse, 0f, .72f, .1f), Activation(PortId.ActivationOut, Out, Pulse, 0f, -.72f, .1f),
            Electrical(PortId.PowerIn, In, -.93f, 0f, 0f), Electrical(PortId.Supply, Out, .93f, 0f, 0f)], new(1, 0, 0, 0)),
        ["delay"] = ([Activation(PortId.ActivationIn, In, Pulse, -.72f, 0f, 0f), Activation(PortId.ActivationOut, Out, Pulse, .72f, 0f, 0f)],
            new(1, 0, 0, 0)),
        ["domino"] = ([Activation(PortId.ActivationOut, Out, Pulse, 0f, .55f, 0f)], new(1, 1, 0, 0)),
        ["funnel"] = ([], new(1, 0, 0, 0)),
        ["hold_timer"] = ([Activation(PortId.ActivationIn, In, Pulse, 0f, .65f, .2f), Electrical(PortId.PowerIn, In, -.78f, 0f, 0f), Electrical(PortId.Supply, Out, .78f, 0f, 0f)], new(1, 0, 0, 0)),
        ["lamp"] = ([Activation(PortId.ActivationIn, In, Level, 0f, 0f, 0f)], new(1, 0, 0, 0)),
        ["latch"] = ([Activation(PortId.SetIn, In, Pulse, -.45f, .72f, 0f), Activation(PortId.ResetIn, In, Pulse, .45f, .72f, 0f),
            Electrical(PortId.PowerIn, In, -.93f, 0f, 0f), Activation(PortId.ActivationOut, Out, Level, 0f, -.56f, .38f),
            Electrical(PortId.Supply, Out, .93f, 0f, 0f)], new(1, 0, 0, 0)),
        ["pipe"] = ([], new(1, 0, 0, 0)),
        ["pipe_bend_45"] = ([], new(1, 0, 0, 0)),
        ["pipe_bend_90"] = ([], new(1, 0, 0, 0)),
        ["powered_gate"] = ([Electrical(PortId.PowerIn, In, 0f, 1.65f, .92f)], new(2, 1, 1, 0)),
        ["pressure_plate"] = ([Electrical(PortId.PowerIn, In, -1.18f, -.08f, 0f), Activation(PortId.ActivationOut, Out, Level, 0f, -.08f, 1.18f),
            Electrical(PortId.Supply, Out, 1.18f, -.08f, 0f)], new(1, 0, 0, 0)),
        ["ramp"] = ([], new(1, 0, 0, 0)),
        ["spring"] = ([], new(2, 1, 1, 0)),
        ["switch"] = ([Activation(PortId.ActivationOut, Out, Pulse, .45f, 0f, .4f),
            Electrical(PortId.PowerIn, In, -.58f, 0f, 0f),
            Electrical(PortId.Supply, Out, .58f, 0f, 0f)], new(1, 0, 0, 0)),
        ["wall"] = ([], new(1, 0, 0, 0)),
    };

    private static uint Bits(float value) => BitConverter.SingleToUInt32Bits(value);

    [Fact]
    public void EveryElementsBundlePortsAndFootprintEqualTheGoldenTable()
    {
        Assert.Equal(Golden.Keys.Order(StringComparer.Ordinal), TestBundle.Entries.Select(entry => entry.Metadata.Id).Order(StringComparer.Ordinal));
        foreach (var entry in TestBundle.Entries)
        {
            var id = entry.Metadata.Id;
            var (ports, footprint) = Golden[id];
            Assert.True(footprint == entry.Footprint, $"{id}: footprint {entry.Footprint} in the bundle, {footprint} golden.");
            var declared = TestBundle.Ports.Of(new PartId(id)).ToArray();
            Assert.True(ports.Length == declared.Length, $"{id}: {declared.Length} ports from the bundle, {ports.Length} golden.");
            for (var i = 0; i < ports.Length; i++)
            {
                Assert.True(ports[i] == declared[i], $"{id}.Ports[{i}]: {declared[i]} from the bundle, {ports[i]} golden.");
                Assert.True(Bits(ports[i].LocalPosition.X) == Bits(declared[i].LocalPosition.X) && Bits(ports[i].LocalPosition.Y) == Bits(declared[i].LocalPosition.Y) &&
                    Bits(ports[i].LocalPosition.Z) == Bits(declared[i].LocalPosition.Z), $"{id}.Ports[{i}]: position bits differ.");
            }
        }
    }

    [Fact]
    public void TheInstalledPortsAreTheBundles() => Assert.Equal(TestBundle.Ports, DeclaredPorts.Current);

    /// <summary>Every declared PortRole is a defined value.</summary>
    [Fact]
    public void EveryPortRoleIsDefined()
    {
        foreach (var role in Enum.GetValues<PortRole>())
            Assert.True(Enum.IsDefined(role));
    }

    /// <summary>The catalogue reads the bundle: a moved, reordered or removed port in the bundle text is what the runtime sees.</summary>
    [Fact]
    public void ThePortsTheRuntimeSeesAreTheBundleTextsPorts()
    {
        var counter = new PartId("counter");
        var original = TestBundle.Entries.Single(entry => entry.Metadata.Id == "counter");
        var moved = original with
        {
            Ports = [original.Ports[1] with { LocalPosition = new Vector3(.25f, .5f, -.75f) }, original.Ports[0]]
        };
        var catalogue = ElementPortCatalogue.FromBundle(TestBundle.Entries.Select(entry => entry.Metadata.Id == "counter" ? moved : entry));
        Assert.Equal([PortId.ActivationOut, PortId.ActivationIn], catalogue.Of(counter).ToArray().Select(port => port.Id));
        Assert.Equal(new Vector3(.25f, .5f, -.75f), catalogue.LocalPosition(counter, PortId.ActivationOut));
        Assert.False(catalogue.Has(counter, PortId.PowerIn, WorkshopConnectionDomain.Electrical, In));
        Assert.True(TestBundle.Ports.Has(counter, PortId.PowerIn, WorkshopConnectionDomain.Electrical, In));
    }

    public static TheoryData<string, PortDescriptor, ElementPortRejection> InvalidPorts => new()
    {
        { "bidirectional", Port(PortId.ActivationOut, SimulationDomain.Signal, PortDirection.Bidirectional, PortRole.ActivationOut, PortSignalKind.Pulse), ElementPortRejection.SocketDisagrees },
        { "undefined direction", Port(PortId.ActivationOut, SimulationDomain.Signal, (PortDirection)7, PortRole.ActivationOut, PortSignalKind.Pulse), ElementPortRejection.UnsupportedDirection },
        { "audio domain", Port(PortId.PowerIn, SimulationDomain.Audio, PortDirection.Input, PortRole.PowerIn, null), ElementPortRejection.UnsupportedDomain },
        { "combined domain", Port(PortId.PowerIn, SimulationDomain.Electrical | SimulationDomain.Signal, PortDirection.Input, PortRole.PowerIn, null), ElementPortRejection.UnsupportedDomain },
        { "undefined role", Port(PortId.ActivationIn, SimulationDomain.Signal, PortDirection.Input, (PortRole)255, PortSignalKind.Pulse), ElementPortRejection.UndefinedSocket },
        { "activation socket on the electrical domain", Port(PortId.ActivationOut, SimulationDomain.Electrical, PortDirection.Output, PortRole.ActivationOut, null), ElementPortRejection.SocketDisagrees },
        { "input socket declared as an output", Port(PortId.PowerIn, SimulationDomain.Electrical, PortDirection.Output, PortRole.PowerIn, null), ElementPortRejection.SocketDisagrees },
        { "signal kind on an electrical port", Port(PortId.Supply, SimulationDomain.Electrical, PortDirection.Output, PortRole.Supply, PortSignalKind.Level), ElementPortRejection.SignalKindDisagrees },
        { "signal port without a kind", Port(PortId.ActivationIn, SimulationDomain.Signal, PortDirection.Input, PortRole.ActivationIn, null), ElementPortRejection.SignalKindDisagrees },
        { "undefined signal kind", Port(PortId.ActivationIn, SimulationDomain.Signal, PortDirection.Input, PortRole.ActivationIn, (PortSignalKind)9), ElementPortRejection.SignalKindDisagrees },
        { "non-finite anchor", Port(PortId.ActivationIn, SimulationDomain.Signal, PortDirection.Input, PortRole.ActivationIn, PortSignalKind.Pulse) with { LocalPosition = new(0f, float.NaN, 0f) },
            ElementPortRejection.NonFinitePosition },
    };

    private static PortDescriptor Port(PortId id, SimulationDomain domain, PortDirection direction, PortRole role, PortSignalKind? signal) =>
        new(id, domain, direction, role, signal, Vector3.Zero, Vector3.UnitY);

    /// <summary>Owner rule: an invalid declaration is refused structurally with a typed reason that names the element and port.</summary>
    [Theory]
    [MemberData(nameof(InvalidPorts))]
    public void AnInvalidPortDeclarationIsRefusedWithItsTypedReason(string defect, PortDescriptor port, ElementPortRejection reason)
    {
        var refused = Assert.Throws<ElementPortRejectedException>(() => ElementPortCatalogue.Declare([(new PartId("fixture"), [port])]));
        Assert.True(reason == refused.Reason, $"{defect}: refused as {refused.Reason}, expected {reason}.");
        Assert.Contains("'fixture'.Ports[0]", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ADuplicateSocketOrElementIsRefused()
    {
        var port = Port(PortId.PowerIn, SimulationDomain.Electrical, PortDirection.Input, PortRole.PowerIn, null);
        Assert.Equal(ElementPortRejection.DuplicateSocket,
            Assert.Throws<ElementPortRejectedException>(() => ElementPortCatalogue.Declare([(new PartId("fixture"), [port, port])])).Reason);
        Assert.Equal(ElementPortRejection.DuplicateElement,
            Assert.Throws<ElementPortRejectedException>(() => ElementPortCatalogue.Declare([(new PartId("fixture"), [port]), (new PartId("fixture"), [])])).Reason);
    }

    /// <summary>Owner rule: an unknown element, or a socket an element does not declare, is a typed refusal, never a default.</summary>
    [Fact]
    public void AnUnknownElementOrUndeclaredSocketIsRefusedWithItsTypedReason()
    {
        Assert.Equal(ElementPortRejection.UnknownElement, Assert.Throws<ElementPortRejectedException>(() => TestBundle.Ports.Of(new PartId("kite"))).Reason);
        Assert.Equal(ElementPortRejection.UndeclaredSocket,
            Assert.Throws<ElementPortRejectedException>(() => TestBundle.Ports.LocalPosition(new PartId("lamp"), PortId.Supply)).Reason);
        Assert.Equal(ElementPortRejection.UndeclaredSocket,
            Assert.Throws<ElementPortRejectedException>(() => TestBundle.Ports.Signal(new PartId("battery"), PortId.Supply, Out)).Reason);
        Assert.False(TestBundle.Ports.Has(new PartId("kite"), PortId.PowerIn, WorkshopConnectionDomain.Electrical, In));
    }

    /// <summary>The bundle read is the typing boundary: a role is written by its exact name, and an unknown name or a number is
    /// refused structurally before any port is admitted.</summary>
    [Theory]
    [InlineData("\"Role\": \"Kite\"")]
    [InlineData("\"Role\": \"powerin\"")]
    [InlineData("\"Role\": 8")]
    public void TheBundleRefusesAnUnknownSocketName(string replacement)
    {
        const string declared = "\"Role\": \"PowerIn\"";
        Assert.Contains(declared, TestBundle.Json, StringComparison.Ordinal);
        var refused = Assert.Throws<InvalidDataException>(() => PartRegistry.Read(RuntimeTestServices.Bundle, TestBundle.Json.Replace(declared, replacement, StringComparison.Ordinal)));
        Assert.Contains("PortRole", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ADifferentCatalogueIsNeverInstalledOverTheBundles()
    {
        DeclaredPorts.Install(ElementPortCatalogue.FromBundle(TestBundle.Entries));
        Assert.Throws<InvalidOperationException>(() => DeclaredPorts.Install(ElementPortCatalogue.Declare([(new PartId("battery"), [])])));
        Assert.Equal(TestBundle.Ports, DeclaredPorts.Current);
    }

    private static readonly WorkshopInstances Instances = new(
        WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("switch"), new(1), 0, 0, 0, 0, 0, 0, 1),
        WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("delay"), new(2), 0, 0, 0, 0, 0, 0, 1),
        WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("battery"), new(3), 0, 0, 0, 0, 0, 0, 1),
        WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("counter"), new(4), 0, 0, 0, 0, 0, 0, 1),
        WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("latch"), new(5), 0, 0, 0, 0, 0, 0, 1),
        WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("lamp"), new(6), 0, 0, 0, 0, 0, 0, 1),
        WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("wall"), new(7), 0, 0, 0, 0, 0, 0, 1));

    private static void Validate(WorkshopConnection link) => new WorkshopConnections(link).Validate(RuntimeTestServices.Core.Supply, Instances);

    /// <summary>AC4: a wrong-domain or output-to-output link is still refused; the matching link is admitted (positive control).</summary>
    [Fact]
    public void AWrongDomainOrOutputToOutputLinkIsStillRefused()
    {
        Validate(new(new(1), PortId.ActivationOut, new(2), PortId.ActivationIn, WorkshopConnectionDomain.Activation));
        Validate(new(new(3), PortId.Supply, new(4), PortId.PowerIn, WorkshopConnectionDomain.Electrical));
        // Wrong domain: the switch's activation output declared as an electrical link, and the battery's supply into an activation input.
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection,
            () => Validate(new(new(1), PortId.ActivationOut, new(2), PortId.ActivationIn, WorkshopConnectionDomain.Electrical)));
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection,
            () => Validate(new(new(3), PortId.Supply, new(2), PortId.ActivationIn, WorkshopConnectionDomain.Activation)));
        // Output to output: the switch's output into the delay's output, and the battery's supply into the counter's supply.
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection,
            () => Validate(new(new(1), PortId.ActivationOut, new(2), PortId.ActivationOut, WorkshopConnectionDomain.Activation)));
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection,
            () => Validate(new(new(3), PortId.Supply, new(4), PortId.Supply, WorkshopConnectionDomain.Electrical)));
        // A socket the bundle does not declare on that part: the delay has no Reset input, and the wall declares no port at all.
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection,
            () => Validate(new(new(1), PortId.ActivationOut, new(2), PortId.ResetIn, WorkshopConnectionDomain.Activation)));
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection,
            () => Validate(new(new(1), PortId.ActivationOut, new(7), PortId.ActivationIn, WorkshopConnectionDomain.Activation)));
        // The declared level kinds still decide: the latch's level output lights only a Signal lamp.
        Validate(new(new(5), PortId.ActivationOut, new(6), PortId.ActivationIn, WorkshopConnectionDomain.Activation));
        EditRefusal.Is(WorkshopEditRejection.LevelDrivesLevelInputOnly,
            () => Validate(new(new(5), PortId.ActivationOut, new(2), PortId.ActivationIn, WorkshopConnectionDomain.Activation)));
    }

    private static readonly string Root = twodog.Engine.ResolveProjectDir();

    private static readonly string[] ProductionDirectories =
        ["engine", "ui", "CuriousContraptions.Simulation", "CuriousContraptions.Animation.Worker", "CuriousContraptions.web"];

    private static IEnumerable<(string Path, string Text)> ProductionSources() =>
        ProductionDirectories
            .SelectMany(directory => Directory.EnumerateFiles(System.IO.Path.Combine(Root, directory), "*.cs", SearchOption.AllDirectories))
            .Where(path => !path.Contains($"{System.IO.Path.DirectorySeparatorChar}obj{System.IO.Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                !path.Contains($"{System.IO.Path.DirectorySeparatorChar}bin{System.IO.Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(path => (System.IO.Path.GetRelativePath(Root, path), File.ReadAllText(path)));

    /// <summary>AC3 and the Deletes list: no hand-written port or footprint table, no scene port lookup and no locator alias chain is
    /// re-added to the engine or the UI.</summary>
    [Fact]
    public void NoHandWrittenPortTableFootprintTableOrLocatorAliasIsReAdded()
    {
        var forbidden = new Regex(@"\bWorkshopPorts\b|\bWorkbenchFootprint\b|\bTryGetPortTransform\b|\bGetPortTransform\b|""Loc_Port_\{|""(power_in|in|out|supply|activation_in|activation_out|set_in|reset_in)""",
            RegexOptions.CultureInvariant);
        var hits = ProductionSources().SelectMany(source => source.Text.Split('\n').Select((line, index) => (source.Path, Line: index + 1, Text: line)))
            .Where(line => forbidden.IsMatch(line.Text)).Select(line => $"{line.Path}:{line.Line}: {line.Text.Trim()}").ToArray();
        Assert.True(hits.Length == 0, "A hand-written port source is re-added:\n" + string.Join("\n", hits));
    }

    /// <summary>The locator names are matched exactly: a differently cased name is a different locator.</summary>
    [Fact]
    public void LocatorNamesAreMatchedOrdinally()
    {
        var locatorLines = File.ReadAllLines(System.IO.Path.Combine(Root, "tools/BundleElements/SceneGeometryExtractor.cs"))
            .Where(line => line.Contains("Loc_Port_", StringComparison.Ordinal) || line.Contains("Dictionary<string, ExtractedPortLocator>", StringComparison.Ordinal))
            .Where(line => !line.TrimStart().StartsWith("//", StringComparison.Ordinal)).ToArray();
        Assert.NotEmpty(locatorLines);
        Assert.All(locatorLines, line => Assert.DoesNotContain("IgnoreCase", line, StringComparison.Ordinal));
    }
}

/// <summary>AC6: every declared port and its scene locator agree. The locator is named <c>Loc_Port_&lt;socket&gt;</c> exactly and is a
/// direct child of its owning body's scene root (the scene root for the base body, <c>SceneBodies[Body].Root</c> for a moving body,
/// Story 10.3a), at the declared anchor in that body's frame; every other locator, at any depth, is a tube mouth (Story 9R.6).</summary>
[Collection<NativeSceneCollectionDefinition>]
public sealed class BundlePortLocatorTests(NativeSceneFixture godot)
{
    private static readonly HashSet<string> Mouths = new(StringComparer.Ordinal) { "mouth_start", "mouth_end" };

    [Fact]
    public void EveryDeclaredPortAgreesWithItsSceneLocator()
    {
        _ = godot;
        var checkedPorts = 0;
        foreach (var package in TestBundle.Packages)
        {
            var declaration = package.Declaration;
            var scene = ResourceLoader.Load<PackedScene>(declaration.Metadata.ScenePath);
            Assert.NotNull(scene);
            var root = scene.Instantiate<Node3D>();
            try
            {
                foreach (var port in declaration.Ports)
                {
                    var name = port.Id.ToString();
                    var path = LocatorPath(package, port);
                    var locator = root.GetNodeOrNull<Node3D>(path);
                    if (locator is null) Assert.Fail($"{package.Package}: no {path} locator for its declared {name} port.");
                    Assert.True(Vector3.Distance(port.LocalPosition, new(locator.Position.X, locator.Position.Y, locator.Position.Z)) <= 1e-5f,
                        $"{package.Package}.{name}: declared at {port.LocalPosition} in its body's frame, its locator is at {locator.Position}.");
                    checkedPorts++;
                }
                foreach (var locator in root.FindChildren("Loc_Port_*", "", true, false))
                {
                    var name = locator.Name.ToString()["Loc_Port_".Length..];
                    var at = root.GetPathTo(locator).ToString();
                    Assert.True(Mouths.Contains(name) || declaration.Ports.Any(port => port.Id.ToString() == name && LocatorPath(package, port) == at),
                        $"{package.Package}: {at} names no declared socket on its owning body.");
                }
            }
            finally { root.Free(); }
        }
        Assert.Equal(TestBundle.Packages.Sum(package => package.Declaration.Ports.Count), checkedPorts);
        Assert.True(checkedPorts >= 22, $"{checkedPorts} declared ports were checked.");
    }

    /// <summary>A port's locator path from the scene root: directly under the scene root for the base body (body 0), otherwise
    /// directly under the owning body's declared scene root.</summary>
    private static string LocatorPath(BundlePackage package, PortDescriptor port)
    {
        if (port.Body.Value == 0) return $"Loc_Port_{port.Id}";
        var geometry = Assert.IsAssignableFrom<ISceneGeometryElement>(package.Declaration);
        Assert.InRange(port.Body.Value, 1, geometry.SceneBodies.Count - 1);
        return $"{geometry.SceneBodies[port.Body.Value].Root}/Loc_Port_{port.Id}";
    }
}
