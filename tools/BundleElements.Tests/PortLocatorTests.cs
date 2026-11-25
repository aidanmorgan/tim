using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.BundleElements.Tests;

/// <summary>Story 9A.17: a scene keeps a socket locator only under its typed socket's exact name, at the declared anchor, as a direct
/// child of its owning body's scene root (the scene root for body 0; the declared body root for a moving body, Story 10.3a); every
/// declared port has its locator, a port naming no declared scene body is refused, and the only other locators are the tube mouths
/// (Story 9R.6). Any mismatch fails the bundle build.</summary>
public sealed class PortLocatorTests
{
    private static PortDescriptor PowerIn(Vector3 at) => new(PortId.PowerIn, SimulationDomain.Electrical, PortDirection.Input, PortRole.PowerIn, new("PowerIn", SocketGlyph.Text), null, at, -Vector3.UnitX);

    private const string Header = "[gd_scene format=3]\n\n[node name=\"Fixture\" type=\"Node3D\"]\n\n";

    private static string[] Errors(string scene, params PortDescriptor[] ports) => Errors(scene, new Fixture { Ports = ports });

    private static string[] Errors(string scene, IPuzzleElementDeclaration declaration, string header = Header)
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "elements", "Fixture"));
        try
        {
            File.WriteAllText(Path.Combine(root, "elements", "Fixture", "fixture.tscn"), header + scene);
            return PackageValidator.Locators(root, [new("Fixture", declaration)]).Select(error => error.ToString()).ToArray();
        }
        finally { Directory.Delete(root, true); }
    }

    private static string Locator(string name, string transform = "Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, -0.93, 0, 0)", string parent = ".") =>
        $"[node name=\"Loc_Port_{name}\" type=\"Node3D\" parent=\"{parent}\"]\ntransform = {transform}\n\n";

    [Fact]
    public void ALocatorNamedByItsSocketAtTheDeclaredAnchorIsAdmitted() =>
        Assert.Empty(Errors(Locator("PowerIn"), PowerIn(new(-.93f, 0f, 0f))));

    [Fact]
    public void ALocatorAtTheOriginMayOmitItsTransform() =>
        Assert.Empty(Errors("[node name=\"Loc_Port_PowerIn\" type=\"Node3D\" parent=\".\"]\n\n", PowerIn(Vector3.Zero)));

    /// <summary>VG7-1: a locator posed by <c>position</c>, without a transform, is read at that point: it is admitted for a port
    /// declared there (not at the origin, so the position is read, not defaulted) and refused for a port declared elsewhere, the
    /// refusal reporting the locator at its position.</summary>
    [Theory]
    [InlineData(0f, null)]
    [InlineData(1.12f, "BUNDLE013 Fixture.Ports[0]: declared at (-0.93, 1.12, 0), Loc_Port_PowerIn is at (-0.93, 0, 0)")]
    public void ALocatorPosedByPositionIsReadAtThatPoint(float declaredY, string? refusal)
    {
        string[] expected = refusal is null ? [] : [refusal];
        Assert.Equal(expected, Errors("[node name=\"Loc_Port_PowerIn\" type=\"Node3D\" parent=\".\"]\nposition = Vector3(-0.93, 0, 0)\n\n",
            PowerIn(new(-.93f, declaredY, 0f))));
    }

    /// <summary>VG7-2: the <c>Loc_Port_</c> prefix is matched with its case, so <c>loc_port_PowerIn</c> is no locator and the declared
    /// port has none. Control: <see cref="ALocatorNamedByItsSocketAtTheDeclaredAnchorIsAdmitted"/>.</summary>
    [Fact]
    public void ALowerCaseLocatorPrefixIsNoLocator() =>
        Assert.Equal(["BUNDLE012 Fixture.Ports[0]: the scene has no Loc_Port_PowerIn locator"],
            Errors("[node name=\"loc_port_PowerIn\" type=\"Node3D\" parent=\".\"]\ntransform = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, -0.93, 0, 0)\n\n",
                PowerIn(new(-.93f, 0f, 0f))));

    /// <summary>ECH7-3: a scene root named as a locator is not a child of itself; it is refused, naming it. Control:
    /// <see cref="ALocatorAtTheOriginMayOmitItsTransform"/>, the same locator at the origin as a child of the root.</summary>
    [Fact]
    public void ASceneRootNamedAsALocatorFailsTheBuild() =>
        Assert.Equal(["BUNDLE013 Fixture: A Loc_Port_ locator is a child node, never a scene root: Loc_Port_PowerIn"],
            Errors("", new Fixture { Ports = [PowerIn(Vector3.Zero)] }, "[gd_scene format=3]\n\n[node name=\"Loc_Port_PowerIn\" type=\"Node3D\"]\n\n"));

    [Fact]
    public void TubeMouthLocatorsAreAdmittedWithoutADeclaredPort() =>
        Assert.Empty(Errors(Locator("mouth_start") + Locator("mouth_end")));

    [Theory]
    [InlineData("power_in", "names no declared socket or tube mouth")]
    [InlineData("powerin", "names no declared socket or tube mouth")]
    [InlineData("in", "names no declared socket or tube mouth")]
    [InlineData("mouth_middle", "names no tube mouth; the mouth_ prefix names mouth_start or mouth_end only")]
    [InlineData("Supply", "names no declared socket or tube mouth")]
    public void ALocatorThatNamesNoDeclaredSocketFailsTheBuild(string name, string reason) =>
        Assert.Equal([$"BUNDLE011 Fixture.Metadata.ScenePath: Loc_Port_{name} {reason}"],
            Errors(Locator("PowerIn") + Locator(name), PowerIn(new(-.93f, 0f, 0f))));

    [Fact]
    public void ADeclaredPortWithoutItsLocatorFailsTheBuild() =>
        Assert.Equal(["BUNDLE012 Fixture.Ports[0]: the scene has no Loc_Port_PowerIn locator"],
            Errors("", PowerIn(new(-.93f, 0f, 0f))));

    [Fact]
    public void ALocatorAwayFromTheDeclaredAnchorFailsTheBuild() =>
        Assert.Equal(["BUNDLE013 Fixture.Ports[0]: declared at (-0.93, 0, 0), Loc_Port_PowerIn is at (0, 1.12, 0)"],
            Errors(Locator("PowerIn", "Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 1.12, 0)"), PowerIn(new(-.93f, 0f, 0f))));

    [Fact]
    public void ANestedOrMalformedLocatorFailsTheBuild()
    {
        Assert.Equal(["BUNDLE013 Fixture.Ports[0]: Loc_Port_PowerIn is not a child of the scene root"],
            Errors(Locator("PowerIn", parent: "Housing"), PowerIn(new(-.93f, 0f, 0f))));
        // The one scene parser reads the locator's pose and refuses one it cannot read under its own code, naming the locator.
        Assert.Equal(["BUNDLE017 Fixture: Loc_Port_PowerIn: Wrong scene numeric arity."],
            Errors(Locator("PowerIn", "Transform3D(1, 0, 0)"), PowerIn(new(-.93f, 0f, 0f))));
    }

    /// <summary>A rope tie on body <paramref name="body"/> at x = -1.8 m in that body's frame, like the Impact Lever's TieA.</summary>
    private static PortDescriptor BodyTie(int body) =>
        new(PortId.TieA, SimulationDomain.Rope, PortDirection.Bidirectional, PortRole.TieA, new("TieA", SocketGlyph.Text), null, new(-1.8f, 0f, 0f), -Vector3.UnitX,
            RopeAttachmentRole.Load, null, new PackageBodyIndex(body));

    private const string BeamBody = "[node name=\"Visual\" type=\"Node3D\" parent=\".\"]\n\n[node name=\"Beam\" type=\"Node3D\" parent=\"Visual\"]\n\n";
    private const string TieAnchor = "Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, -1.8, 0, 0)";

    [Fact]
    public void ABodyOwnedLocatorUnderItsBodyRootAtTheAnchorInThatBodysFrameIsAdmitted() =>
        Assert.Empty(Errors(BeamBody + Locator("TieA", TieAnchor, "Visual/Beam"), new SceneBodyFixture { Ports = [BodyTie(1)] }));

    [Theory]
    [InlineData(".")]
    [InlineData("Visual")]
    public void ABodyOwnedLocatorOutsideItsBodyRootFailsTheBuild(string parent) =>
        Assert.Equal(["BUNDLE013 Fixture.Ports[0]: Loc_Port_TieA is not a child of 'Visual/Beam'"],
            Errors(BeamBody + Locator("TieA", TieAnchor, parent), new SceneBodyFixture { Ports = [BodyTie(1)] }));

    [Theory]
    [InlineData(2)]
    [InlineData(-1)]
    public void APortNamingNoDeclaredSceneBodyFailsTheBuild(int body) =>
        Assert.Equal([$"BUNDLE006 Fixture.Ports[0].Body: body {body} is not a declared scene body"],
            Errors(BeamBody + Locator("TieA", TieAnchor, "Visual/Beam"), new SceneBodyFixture { Ports = [BodyTie(body)] }));

    [Fact]
    public void AMovingBodyPortOnADeclarationWithoutSceneBodiesFailsTheBuild() =>
        Assert.Equal(["BUNDLE006 Fixture.Ports[0].Body: body 1 is not a declared scene body"],
            Errors(Locator("TieA", TieAnchor), BodyTie(1)));

    [Fact]
    public void ADuplicateLocatorFailsTheBuild() =>
        Assert.Equal(["BUNDLE011 Fixture.Metadata.ScenePath: Loc_Port_PowerIn appears more than once"],
            Errors(Locator("PowerIn") + Locator("PowerIn"), PowerIn(new(-.93f, 0f, 0f))));

    [Fact]
    public void EveryRealPackageSceneAgreesWithItsDeclaredPorts() =>
        Assert.Empty(PackageValidator.Locators(TestRepository.Root, PackageDiscovery.Discover(TestRepository.Root).Packages));

    /// <summary>The bundle entry validator's errors for one declaration's entry.</summary>
    private static IEnumerable<string> EntryErrors(string package, IPuzzleElementDeclaration declaration)
    {
        var services = new CuriousContraptions.Bundle.BundleServices();
        return services.Validate([new NamedEntry(package, services.Entry(declaration))]).Select(error => error.ToString());
    }

    [Fact]
    public void ADuplicatePortIdIsRefused() =>
        Assert.Equal(["BUNDLE014 FixturePorts.Ports[1].Id: PowerIn is declared more than once", "BUNDLE006 fixture.SupplyGraph.Missing"],
            EntryErrors("FixturePorts", new Fixture { Ports = [PowerIn(Vector3.Zero), PowerIn(Vector3.UnitX)] }));

    [Fact]
    public void ASignalKindIsDeclaredExactlyOnSignalPorts()
    {
        PortDescriptor Port(SimulationDomain domain, PortSignalKind? signal) =>
            new(PortId.ActivationOut, domain, PortDirection.Output, PortRole.ActivationOut, new("ActivationOut", SocketGlyph.Text), signal, Vector3.Zero, Vector3.UnitY);
        Assert.Empty(EntryErrors("Fixture", new SignalFixture
        {
            Ports = [Port(SimulationDomain.Signal, PortSignalKind.Level)],
            SignalNodes = [new SignalMemoryDescriptor(new("memory"))],
            SignalPorts = [new(PortId.ActivationOut, new(new("memory"), SignalTerminal.Output))]
        }));
        Assert.Equal(["BUNDLE015 FixtureSignal.Ports[0].Signal: a Signal port declares its signal kind and no other port does",
            "BUNDLE006 fixture.Signal.Missing"],
            EntryErrors("FixtureSignal", new Fixture { Ports = [Port(SimulationDomain.Signal, null)] }));
        Assert.Equal(["BUNDLE015 FixtureSignal.Ports[0].Signal: a Signal port declares its signal kind and no other port does",
            "BUNDLE006 fixture.SupplyGraph.Missing"],
            EntryErrors("FixtureSignal", new Fixture { Ports = [Port(SimulationDomain.Electrical, PortSignalKind.Level)] }));
    }

    [Fact]
    public void AnUndefinedRoleIsRefused() =>
        Assert.Equal(["BUNDLE003 FixtureSocket.Ports[0].Role: undefined enum value", "BUNDLE006 fixture.SupplyGraph.Missing"],
            EntryErrors("FixtureSocket", new Fixture { Ports = [PowerIn(Vector3.Zero) with { Role = (PortRole)99 }] }));
}
