using System.Numerics;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.BundleElements.Tests;

/// <summary>Story 9A.17: a scene keeps a socket locator only under its typed socket's exact name, at the declared anchor, as a child
/// of the scene root; every declared port has its locator, and the only other locators are the tube mouths (Story 9R.6). Any
/// mismatch fails the bundle build.</summary>
public sealed class PortLocatorTests
{
    private static PortDescriptor PowerIn(Vector3 at) => new(PortSocket.PowerIn, SimulationDomain.Electrical, PortDirection.Input, null, at, -Vector3.UnitX);

    private const string Header = "[gd_scene format=3]\n\n[node name=\"Fixture\" type=\"Node3D\"]\n\n";

    private static string[] Errors(string scene, params PortDescriptor[] ports)
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "elements", "Fixture"));
        try
        {
            File.WriteAllText(Path.Combine(root, "elements", "Fixture", "fixture.tscn"), Header + scene);
            return PackageValidator.Locators(root, [new("Fixture", new Fixture { Ports = ports })]).Select(error => error.ToString()).ToArray();
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

    [Fact]
    public void TubeMouthLocatorsAreAdmittedWithoutADeclaredPort() =>
        Assert.Empty(Errors(Locator("mouth_start") + Locator("mouth_end")));

    [Theory]
    [InlineData("power_in")]
    [InlineData("powerin")]
    [InlineData("in")]
    [InlineData("mouth_middle")]
    [InlineData("Supply")]
    public void ALocatorThatNamesNoDeclaredSocketFailsTheBuild(string name) =>
        Assert.Equal([$"BUNDLE011 Fixture.Metadata.ScenePath: Loc_Port_{name} names no declared socket or tube mouth"],
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
        Assert.Equal(["BUNDLE013 Fixture.Ports[0]: Loc_Port_PowerIn has no readable transform"],
            Errors(Locator("PowerIn", "Transform3D(1, 0, 0)"), PowerIn(new(-.93f, 0f, 0f))));
    }

    [Fact]
    public void ADuplicateLocatorFailsTheBuild() =>
        Assert.Equal(["BUNDLE011 Fixture.Metadata.ScenePath: Loc_Port_PowerIn appears more than once"],
            Errors(Locator("PowerIn") + Locator("PowerIn"), PowerIn(new(-.93f, 0f, 0f))));

    [Fact]
    public void EveryRealPackageSceneAgreesWithItsDeclaredPorts() =>
        Assert.Empty(PackageValidator.Locators(TestRepository.Root, PackageDiscovery.Discover(TestRepository.Root).Packages));

    [Fact]
    public void ADuplicatePortSocketIsRefused() =>
        Assert.Equal(["BUNDLE014 FixturePorts.Ports[1].Socket: PowerIn is declared more than once"],
            PackageValidator.Validate([new("FixturePorts", new Fixture { Ports = [PowerIn(Vector3.Zero), PowerIn(Vector3.UnitX)] })])
                .Select(error => error.ToString()));

    [Fact]
    public void ASignalKindIsDeclaredExactlyOnSignalPorts()
    {
        PortDescriptor Port(SimulationDomain domain, PortSignalKind? signal) =>
            new(PortSocket.ActivationOut, domain, PortDirection.Output, signal, Vector3.Zero, Vector3.UnitY);
        Assert.Empty(PackageValidator.Validate([new("Fixture", new Fixture { Ports = [Port(SimulationDomain.Signal, PortSignalKind.Pulse)] })]));
        Assert.Equal(["BUNDLE015 FixtureSignal.Ports[0].Signal: a Signal port declares its signal kind and no other port does"],
            PackageValidator.Validate([new("FixtureSignal", new Fixture { Ports = [Port(SimulationDomain.Signal, null)] })]).Select(error => error.ToString()));
        Assert.Equal(["BUNDLE015 FixtureSignal.Ports[0].Signal: a Signal port declares its signal kind and no other port does"],
            PackageValidator.Validate([new("FixtureSignal", new Fixture { Ports = [Port(SimulationDomain.Electrical, PortSignalKind.Level)] })])
                .Select(error => error.ToString()));
    }

    [Fact]
    public void AnUndefinedSocketIsRefused() =>
        Assert.Equal(["BUNDLE003 FixtureSocket.Ports[0].Socket: undefined enum value"],
            PackageValidator.Validate([new("FixtureSocket", new Fixture { Ports = [PowerIn(Vector3.Zero) with { Socket = (PortSocket)99 }] })])
                .Select(error => error.ToString()));
}
