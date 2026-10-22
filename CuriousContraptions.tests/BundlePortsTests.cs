using System.Numerics;
using System.Text.RegularExpressions;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Bundle;
using CuriousContraptions.BundleElements;
using CuriousContraptions.Gpu;
using Godot;
using Vector3 = System.Numerics.Vector3;

[assembly: AssemblyFixture(typeof(CuriousContraptions.Tests.DeclaredPortsFixture))]

namespace CuriousContraptions.Tests;

/// <summary>The element bundle as the tests see it: every discovered package, written and read back through the same bundle schema
/// and validation the game runs at load (Story 9A.17).</summary>
internal static class TestBundle
{
    public static IReadOnlyList<BundlePackage> Packages { get; } = Discover();

    public static string Json { get; } = BundleGenerator.Generate(twodog.Engine.ResolveProjectDir()).Json ?? throw new InvalidOperationException("Test bundle failed to bake.");

    public static IReadOnlyList<BundleEntry> Entries { get; } = PartRegistry.Read(Json);

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

    private static IReadOnlyList<BundlePackage> Discover()
    {
        var discovered = PackageDiscovery.Discover(twodog.Engine.ResolveProjectDir());
        if (discovered.Errors.Count > 0) throw new InvalidOperationException(string.Join("; ", discovered.Errors));
        return discovered.Packages;
    }
}

/// <summary>Installs the bundle's declared ports once for the whole test assembly, as the Workshop does after its registry loads and
/// the Simulation worker does at bootstrap.</summary>
public sealed class DeclaredPortsFixture
{
    public DeclaredPortsFixture()
    {
        DeclaredPhysics.Install(TestBundle.Entries);
        DeclaredPorts.Install(TestBundle.Ports);
    }
}

/// <summary>Story 9A.17: every element's sockets, directions, domains, signal kinds and anchor positions come only from its bundle
/// <see cref="PortDescriptor"/> list, and its footprint only from its declaration.</summary>
public sealed class BundlePortsTests
{
    private static WorkshopPort Activation(WorkshopSocket socket, WorkshopPortDirection direction, WorkshopSignalKind signal, float x, float y, float z) =>
        new(socket, WorkshopConnectionDomain.Activation, direction, signal, new(x, y, z));
    private static WorkshopPort Electrical(WorkshopSocket socket, WorkshopPortDirection direction, float x, float y, float z) =>
        new(socket, WorkshopConnectionDomain.Electrical, direction, null, new(x, y, z));

    private const WorkshopPortDirection In = WorkshopPortDirection.Input;
    private const WorkshopPortDirection Out = WorkshopPortDirection.Output;
    private const WorkshopSignalKind Pulse = WorkshopSignalKind.Pulse;
    private const WorkshopSignalKind Level = WorkshopSignalKind.Level;

    /// <summary>The golden table: the runtime's ports (in drawer order) and every declared footprint at the start of Story 9A.17,
    /// copied from the deleted engine port switch tables and the packages.</summary>
    private static readonly Dictionary<string, (WorkshopPort[] Ports, ElementFootprint Footprint)> Golden = new(StringComparer.Ordinal)
    {
        ["both_gate"] = ([Electrical(WorkshopSocket.FirstIn, In, -.94f, .35f, 0f), Electrical(WorkshopSocket.SecondIn, In, -.94f, -.35f, 0f), Electrical(WorkshopSocket.PowerIn, In, 0f, -.75f, .55f), Electrical(WorkshopSocket.Supply, Out, .94f, 0f, 0f)], new(1, 0, 0)),
        ["electrical_nand"] = ([Electrical(WorkshopSocket.FirstIn, In, -.94f, .35f, 0f), Electrical(WorkshopSocket.SecondIn, In, -.94f, -.35f, 0f), Electrical(WorkshopSocket.PowerIn, In, 0f, -.75f, .55f), Electrical(WorkshopSocket.Supply, Out, .94f, 0f, 0f)], new(1, 0, 0)),
        ["electrical_nor"] = ([Electrical(WorkshopSocket.FirstIn, In, -.94f, .35f, 0f), Electrical(WorkshopSocket.SecondIn, In, -.94f, -.35f, 0f), Electrical(WorkshopSocket.PowerIn, In, 0f, -.75f, .55f), Electrical(WorkshopSocket.Supply, Out, .94f, 0f, 0f)], new(1, 0, 0)),
        ["ball"] = ([], new(1, 1, 0)),
        ["ball_detector"] = ([Activation(WorkshopSocket.ActivationOut, Out, Pulse, 0f, 1.12f, 0f)], new(1, 0, 0)),
        ["basket"] = ([], new(1, 0, 0)),
        ["battery"] = ([Electrical(WorkshopSocket.Supply, Out, 0f, .62f, 0f)], new(1, 0, 0)),
        ["bowling"] = ([], new(1, 1, 0)),
        ["bumper"] = ([Electrical(WorkshopSocket.PowerIn, In, 0f, 0f, .65f)], new(1, 0, 0)),
        ["clock"] = ([Electrical(WorkshopSocket.PowerIn, In, -.78f, 0f, 0f), Activation(WorkshopSocket.ActivationOut, Out, Pulse, .78f, 0f, 0f)],
            new(1, 0, 0)),
        ["counter"] = ([Activation(WorkshopSocket.ActivationIn, In, Pulse, 0f, .72f, .1f), Activation(WorkshopSocket.ActivationOut, Out, Pulse, 0f, -.72f, .1f),
            Electrical(WorkshopSocket.PowerIn, In, -.93f, 0f, 0f), Electrical(WorkshopSocket.Supply, Out, .93f, 0f, 0f)], new(1, 0, 0)),
        ["delay"] = ([Activation(WorkshopSocket.ActivationIn, In, Pulse, -.72f, 0f, 0f), Activation(WorkshopSocket.ActivationOut, Out, Pulse, .72f, 0f, 0f)],
            new(1, 0, 0)),
        ["domino"] = ([Activation(WorkshopSocket.ActivationOut, Out, Pulse, 0f, .55f, 0f)], new(1, 1, 0)),
        ["funnel"] = ([], new(1, 0, 0)),
        ["hold_timer"] = ([Activation(WorkshopSocket.ActivationIn, In, Pulse, 0f, .65f, .2f), Electrical(WorkshopSocket.PowerIn, In, -.78f, 0f, 0f), Electrical(WorkshopSocket.Supply, Out, .78f, 0f, 0f)], new(1, 0, 0)),
        ["lamp"] = ([Activation(WorkshopSocket.ActivationIn, In, Level, 0f, 0f, 0f)], new(1, 0, 0)),
        ["latch"] = ([Activation(WorkshopSocket.SetIn, In, Pulse, -.45f, .72f, 0f), Activation(WorkshopSocket.ResetIn, In, Pulse, .45f, .72f, 0f),
            Electrical(WorkshopSocket.PowerIn, In, -.93f, 0f, 0f), Activation(WorkshopSocket.ActivationOut, Out, Level, 0f, -.56f, .38f),
            Electrical(WorkshopSocket.Supply, Out, .93f, 0f, 0f)], new(1, 0, 0)),
        ["pipe"] = ([], new(1, 0, 0)),
        ["pipe_bend_45"] = ([], new(1, 0, 0)),
        ["pipe_bend_90"] = ([], new(1, 0, 0)),
        ["powered_gate"] = ([Electrical(WorkshopSocket.PowerIn, In, 0f, 1.65f, .92f)], new(2, 1, 1)),
        ["pressure_plate"] = ([Electrical(WorkshopSocket.PowerIn, In, -1.18f, -.08f, 0f), Activation(WorkshopSocket.ActivationOut, Out, Level, 0f, -.08f, 1.18f),
            Electrical(WorkshopSocket.Supply, Out, 1.18f, -.08f, 0f)], new(1, 0, 0)),
        ["ramp"] = ([], new(1, 0, 0)),
        ["spring"] = ([], new(2, 1, 1)),
        ["switch"] = ([Activation(WorkshopSocket.ActivationOut, Out, Pulse, .45f, 0f, .4f),
            Electrical(WorkshopSocket.PowerIn, In, -.58f, 0f, 0f),
            Electrical(WorkshopSocket.Supply, Out, .58f, 0f, 0f)], new(1, 0, 0)),
        ["wall"] = ([], new(1, 0, 0)),
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

    /// <summary>The bundle's socket set and the engine's are one closed set: the same names with the same wire values.</summary>
    [Fact]
    public void EveryBundleSocketIsTheEngineSocketOfTheSameNameAndValue()
    {
        Assert.Equal(Enum.GetNames<PortSocket>(), Enum.GetNames<WorkshopSocket>());
        foreach (var socket in Enum.GetValues<PortSocket>())
            Assert.Equal(Enum.GetName(socket), Enum.GetName((WorkshopSocket)(uint)socket));
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
        Assert.Equal([WorkshopSocket.ActivationOut, WorkshopSocket.ActivationIn], catalogue.Of(counter).ToArray().Select(port => port.Socket));
        Assert.Equal(new Vector3(.25f, .5f, -.75f), catalogue.LocalPosition(counter, WorkshopSocket.ActivationOut));
        Assert.False(catalogue.Has(counter, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical, In));
        Assert.True(TestBundle.Ports.Has(counter, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical, In));
    }

    public static TheoryData<string, PortDescriptor, ElementPortRejection> InvalidPorts => new()
    {
        { "bidirectional", Port(PortSocket.ActivationOut, SimulationDomain.Signal, PortDirection.Bidirectional, PortSignalKind.Pulse), ElementPortRejection.UnsupportedDirection },
        { "undefined direction", Port(PortSocket.ActivationOut, SimulationDomain.Signal, (PortDirection)7, PortSignalKind.Pulse), ElementPortRejection.UnsupportedDirection },
        { "audio domain", Port(PortSocket.PowerIn, SimulationDomain.Audio, PortDirection.Input, null), ElementPortRejection.UnsupportedDomain },
        { "combined domain", Port(PortSocket.PowerIn, SimulationDomain.Electrical | SimulationDomain.Signal, PortDirection.Input, null), ElementPortRejection.UnsupportedDomain },
        { "undefined socket", Port((PortSocket)255, SimulationDomain.Signal, PortDirection.Input, PortSignalKind.Pulse), ElementPortRejection.UndefinedSocket },
        { "activation socket on the electrical domain", Port(PortSocket.ActivationOut, SimulationDomain.Electrical, PortDirection.Output, null), ElementPortRejection.SocketDisagrees },
        { "input socket declared as an output", Port(PortSocket.PowerIn, SimulationDomain.Electrical, PortDirection.Output, null), ElementPortRejection.SocketDisagrees },
        { "signal kind on an electrical port", Port(PortSocket.Supply, SimulationDomain.Electrical, PortDirection.Output, PortSignalKind.Level), ElementPortRejection.SignalKindDisagrees },
        { "signal port without a kind", Port(PortSocket.ActivationIn, SimulationDomain.Signal, PortDirection.Input, null), ElementPortRejection.SignalKindDisagrees },
        { "undefined signal kind", Port(PortSocket.ActivationIn, SimulationDomain.Signal, PortDirection.Input, (PortSignalKind)9), ElementPortRejection.SignalKindDisagrees },
        { "non-finite anchor", Port(PortSocket.ActivationIn, SimulationDomain.Signal, PortDirection.Input, PortSignalKind.Pulse) with { LocalPosition = new(0f, float.NaN, 0f) },
            ElementPortRejection.NonFinitePosition },
    };

    private static PortDescriptor Port(PortSocket socket, SimulationDomain domain, PortDirection direction, PortSignalKind? signal) =>
        new(socket, domain, direction, signal, Vector3.Zero, Vector3.UnitY);

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
        var port = Port(PortSocket.PowerIn, SimulationDomain.Electrical, PortDirection.Input, null);
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
            Assert.Throws<ElementPortRejectedException>(() => TestBundle.Ports.LocalPosition(PartId.SignalLamp, WorkshopSocket.Supply)).Reason);
        Assert.Equal(ElementPortRejection.UndeclaredSocket,
            Assert.Throws<ElementPortRejectedException>(() => TestBundle.Ports.Signal(PartId.Battery, WorkshopSocket.Supply, Out)).Reason);
        Assert.False(TestBundle.Ports.Has(new PartId("kite"), WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical, In));
    }

    /// <summary>The bundle read is the typing boundary: a socket is written by its exact name, and an unknown name or a number is
    /// refused structurally before any port is admitted.</summary>
    [Theory]
    [InlineData("\"Socket\": \"Kite\"")]
    [InlineData("\"Socket\": \"powerin\"")]
    [InlineData("\"Socket\": 8")]
    public void TheBundleRefusesAnUnknownSocketName(string replacement)
    {
        const string declared = "\"Socket\": \"PowerIn\"";
        Assert.Contains(declared, TestBundle.Json, StringComparison.Ordinal);
        var refused = Assert.Throws<InvalidDataException>(() => PartRegistry.Read(TestBundle.Json.Replace(declared, replacement, StringComparison.Ordinal)));
        Assert.Contains("PortSocket", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ADifferentCatalogueIsNeverInstalledOverTheBundles()
    {
        DeclaredPorts.Install(ElementPortCatalogue.FromBundle(TestBundle.Entries));
        Assert.Throws<InvalidOperationException>(() => DeclaredPorts.Install(ElementPortCatalogue.Declare([(PartId.Battery, [])])));
        Assert.Equal(TestBundle.Ports, DeclaredPorts.Current);
    }

    private static readonly Dictionary<GpuBodyId, PartId> Parts = new()
    {
        [new(1)] = PartId.ImpactSwitch, [new(2)] = PartId.Delay, [new(3)] = PartId.Battery, [new(4)] = PartId.Counter, [new(5)] = PartId.Latch,
        [new(6)] = PartId.SignalLamp, [new(7)] = PartId.Wall,
    };

    private static void Validate(WorkshopConnection link) => new WorkshopConnections(link).Validate(id => Parts.TryGetValue(id, out var part) ? part : null);

    /// <summary>AC4: a wrong-domain or output-to-output link is still refused; the matching link is admitted (positive control).</summary>
    [Fact]
    public void AWrongDomainOrOutputToOutputLinkIsStillRefused()
    {
        Validate(new(new(1), WorkshopSocket.ActivationOut, new(2), WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation));
        Validate(new(new(3), WorkshopSocket.Supply, new(4), WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical));
        // Wrong domain: the switch's activation output declared as an electrical link, and the battery's supply into an activation input.
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection,
            () => Validate(new(new(1), WorkshopSocket.ActivationOut, new(2), WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Electrical)));
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection,
            () => Validate(new(new(3), WorkshopSocket.Supply, new(2), WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation)));
        // Output to output: the switch's output into the delay's output, and the battery's supply into the counter's supply.
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection,
            () => Validate(new(new(1), WorkshopSocket.ActivationOut, new(2), WorkshopSocket.ActivationOut, WorkshopConnectionDomain.Activation)));
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection,
            () => Validate(new(new(3), WorkshopSocket.Supply, new(4), WorkshopSocket.Supply, WorkshopConnectionDomain.Electrical)));
        // A socket the bundle does not declare on that part: the delay has no Reset input, and the wall declares no port at all.
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection,
            () => Validate(new(new(1), WorkshopSocket.ActivationOut, new(2), WorkshopSocket.ResetIn, WorkshopConnectionDomain.Activation)));
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection,
            () => Validate(new(new(1), WorkshopSocket.ActivationOut, new(7), WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation)));
        // The declared level kinds still decide: the latch's level output lights only a Signal lamp.
        Validate(new(new(5), WorkshopSocket.ActivationOut, new(6), WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation));
        EditRefusal.Is(WorkshopEditRejection.LevelDrivesLampOnly,
            () => Validate(new(new(5), WorkshopSocket.ActivationOut, new(2), WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation)));
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

/// <summary>AC6: every declared port and its scene locator agree. The locator is named <c>Loc_Port_&lt;socket&gt;</c> exactly, at the
/// declared anchor; every other locator is a tube mouth (Story 9R.6).</summary>
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
                var (_, locators) = TestBundle.SceneGeometry(root);
                foreach (var port in declaration.Ports)
                {
                    var name = port.Socket.ToString();
                    Assert.True(locators.TryGetValue(name, out var locator), $"{package.Package}: no Loc_Port_{name} locator for its declared {name} port.");
                    Assert.True(Vector3.Distance(port.LocalPosition, new(locator.Position.X, locator.Position.Y, locator.Position.Z)) <= 1e-5f,
                        $"{package.Package}.{name}: declared at {port.LocalPosition}, its locator is at {locator.Position}.");
                    checkedPorts++;
                }
                foreach (var name in locators.Keys)
                    Assert.True(Mouths.Contains(name) || declaration.Ports.Any(port => port.Socket.ToString() == name),
                        $"{package.Package}: Loc_Port_{name} names no declared socket.");
            }
            finally { root.Free(); }
        }
        Assert.Equal(TestBundle.Packages.Sum(package => package.Declaration.Ports.Count), checkedPorts);
        Assert.True(checkedPorts >= 22, $"{checkedPorts} declared ports were checked.");
    }
}
