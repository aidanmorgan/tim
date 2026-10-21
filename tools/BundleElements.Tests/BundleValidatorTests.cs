using System.Numerics;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.BundleElements.Tests;

/// <summary>Story 9A.4 AC4: generation fails naming the package and the field (the spec's edge-case matrix).</summary>
public sealed class BundleValidatorTests
{
    private static string[] Errors(params (string Package, IPuzzleElementDeclaration Declaration)[] packages) =>
        PackageValidator.Validate(packages.Select(package => new BundlePackage(package.Package, package.Declaration)).ToArray())
            .Select(error => error.ToString()).ToArray();

    [Fact]
    public void AValidPackageIsAdmitted() => Assert.Empty(Errors(("Fixture", new Fixture())));

    [Fact]
    public void ADuplicateIdNamesBothPackages() =>
        Assert.Equal(["BUNDLE001 duplicate id 'x' (FixtureA, FixtureB)"],
            Errors(("FixtureA", new Fixture { Id = "x" }), ("FixtureB", new Fixture { Id = "x", PaletteOrder = 2 })));

    [Fact]
    public void ADuplicatePaletteOrderNamesBothPackages() =>
        Assert.Equal(["BUNDLE008 duplicate palette order 4 (FixtureA, FixtureB)"],
            Errors(("FixtureA", new Fixture { Id = "a", PaletteOrder = 4 }), ("FixtureB", new Fixture { Id = "b", PaletteOrder = 4 })));

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void APaletteOrderMustBePositive(int order) =>
        Assert.Equal(["BUNDLE006 FixtureOrder.Metadata.PaletteOrder: outside its declared range"],
            Errors(("FixtureOrder", new Fixture { PaletteOrder = order })));

    [Theory]
    [InlineData("", "Fixture")]
    [InlineData(" Fixture", "Fixture")]
    [InlineData("Fixture", "")]
    public void TitlesAndDescriptionsMustBeText(string title, string description) =>
        Assert.Equal([$"BUNDLE010 FixtureText.Metadata.{(title.Trim() == title && title.Length > 0 ? "Description" : "DisplayName")}: empty or padded text"],
            Errors(("FixtureText", new Fixture { DisplayName = title, Description = description })));

    [Theory]
    [InlineData(-0.01f)]
    [InlineData(1.01f)]
    public void AColourChannelMustLieInUnitRange(float red) =>
        Assert.Equal(["BUNDLE006 FixtureColour.Metadata.Color.R: outside its declared range"],
            Errors(("FixtureColour", new Fixture { Color = new(red, .5f, .5f, 1f) })));

    [Theory]
    [InlineData("res://parts/scenes/fixture.tscn")]
    [InlineData("res://elements/../fixture.tscn")]
    [InlineData("elements/Fixture/fixture.tscn")]
    public void AnAssetPathMustBeAPackagePath(string path) =>
        Assert.Equal([$"BUNDLE007 FixturePath.Metadata.ScenePath: '{path}' is not a package asset path"],
            Errors(("FixturePath", new Fixture { ScenePath = path })));

    [Fact]
    public void APortNormalMustBeAUnitDirection()
    {
        PortDescriptor Port(Vector3 normal) => new(PortSocket.ActivationOut, SimulationDomain.Signal, PortDirection.Output, PortSignalKind.Pulse,
            Vector3.Zero, normal);
        Assert.Empty(Errors(("Fixture", new Fixture { Ports = [Port(-Vector3.UnitY)] })));
        Assert.Equal(["BUNDLE006 FixtureNormal.Ports[0].Normal: outside its declared range"],
            Errors(("FixtureNormal", new Fixture { Ports = [Port(new(0f, 2f, 0f))] })));
    }

    /// <summary>Stories 9A.14 and 9A.18: a missing icon (or scene) fails the bundle build, and an asset outside the package's own
    /// directory is refused even when the file exists.</summary>
    [Fact]
    public void AMissingOrForeignPackageAssetFailsTheBuild()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "elements", "Fixture"));
        Directory.CreateDirectory(Path.Combine(root, "elements", "Other"));
        try
        {
            File.WriteAllText(Path.Combine(root, "elements", "Fixture", "fixture.tscn"), "");
            File.WriteAllText(Path.Combine(root, "elements", "Other", "icon.svg"), "");
            BundlePackage[] packages = [new("Fixture", new Fixture())];
            Assert.Equal(["BUNDLE009 Fixture.Metadata.IconPath: 'res://elements/Fixture/icon.svg' is not a file in elements/Fixture/"],
                PackageValidator.Assets(root, packages).Select(error => error.ToString()));
            File.WriteAllText(Path.Combine(root, "elements", "Fixture", "icon.svg"), "");
            Assert.Empty(PackageValidator.Assets(root, packages));
            Assert.Equal(["BUNDLE009 Fixture.Metadata.IconPath: 'res://elements/Other/icon.svg' is not a file in elements/Fixture/"],
                PackageValidator.Assets(root, [new("Fixture", new Fixture { IconPath = "res://elements/Other/icon.svg" })])
                    .Select(error => error.ToString()));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void EveryRealPackageShipsItsIconAndScene() =>
        Assert.Empty(PackageValidator.Assets(TestRepository.Root, PackageDiscovery.Discover(TestRepository.Root).Packages));

    [Fact]
    public void ANonFiniteValueNamesThePackageAndTheField() =>
        Assert.Equal(["BUNDLE002 FixtureNaN.Colliders[0].HalfExtents.x: not finite"],
            Errors(("FixtureNaN", new Fixture { Colliders = [Fixture.Box(new(float.NaN, .5f, .5f))] })));

    [Fact]
    public void AnInfiniteMassIsNonFinite() =>
        Assert.Equal(["BUNDLE002 FixtureMass.MassProperties.Mass: not finite"],
            Errors(("FixtureMass", new Fixture { MassProperties = new(float.PositiveInfinity, Vector3.Zero, Vector3.Zero, 0f, Vector3.Zero) })));

    [Fact]
    public void AnUnknownEnumNamesThePackageAndTheField() =>
        Assert.Equal(["BUNDLE003 FixtureEnum.Metadata.Category: undefined enum value"],
            Errors(("FixtureEnum", new Fixture { Category = (ElementCategory)99 })));

    [Fact]
    public void ACombinedDomainSetIsDefinedButAnUnknownDomainBitIsNot()
    {
        Assert.Empty(Errors(("Fixture", new Fixture { SupportedDomains = SimulationDomain.Mechanical | SimulationDomain.Signal })));
        Assert.Equal(["BUNDLE003 FixtureDomain.SupportedDomains: undefined enum value"],
            Errors(("FixtureDomain", new Fixture { SupportedDomains = (SimulationDomain)(1u << 9) })));
    }

    [Fact]
    public void ABodyWithoutAColliderIsRefused() =>
        Assert.Equal(["BUNDLE004 FixtureBody.Colliders: a body declares no collider"],
            Errors(("FixtureBody", new Fixture { Colliders = [] })));

    [Fact]
    public void ANonMechanicalBodyWithoutAColliderIsRefused() =>
        Assert.Equal(["BUNDLE004 FixtureLamp.Colliders: a body declares no collider"],
            Errors(("FixtureLamp", new SignalFixture { Footprint = new(1, 0, 0), Colliders = [] })));

    [Fact]
    public void ANonMechanicalPackageWithColliderOrWithoutBodyIsAdmitted()
    {
        Assert.Empty(Errors(("FixtureLamp", new SignalFixture { Footprint = new(1, 0, 0) })));
        Assert.Empty(Errors(("FixtureSignal", new SignalFixture { Footprint = new(0, 0, 0), Colliders = [] })));
    }

    [Fact]
    public void ADefaultOutsideItsBoundsIsRefused() =>
        Assert.Equal(["BUNDLE006 FixtureRange.Metadata.Parameters[0].Default: outside its declared range"],
            Errors(("FixtureRange", new Fixture { Parameters = [new("length", ParameterUnit.Metre, ParameterKind.Real, 1f, 2f, 3f, [])] })));

    [Fact]
    public void AChoiceMustSpanItsOptions()
    {
        Assert.Empty(Errors(("Fixture", new Fixture { Parameters = [new("enabled", ParameterUnit.None, ParameterKind.Choice, 0f, 1f, 1f, ["Disabled", "Enabled"])] })));
        Assert.Equal(["BUNDLE006 FixtureChoice.Metadata.Parameters[0].Options: outside its declared range"],
            Errors(("FixtureChoice", new Fixture { Parameters = [new("enabled", ParameterUnit.None, ParameterKind.Choice, 0f, 2f, 1f, ["Disabled", "Enabled"])] })));
    }

    [Fact]
    public void AValidSliderIsAdmitted() =>
        Assert.Empty(Errors(("Fixture", new Fixture { Parameters = Fixture.SpringParameters, Sliders = [Fixture.Slider()] })));

    [Fact]
    public void ASliderNamingAnUndeclaredParameterIsRefused() =>
        Assert.Equal(["BUNDLE007 FixtureSlider.Sliders[0].StiffnessParameter: 'spring_rate' names no declared Real parameter"],
            Errors(("FixtureSlider", new Fixture { Parameters = Fixture.SpringParameters, Sliders = [Fixture.Slider(stiffness: "spring_rate")] })));

    [Fact]
    public void ASliderWhoseMovingBodyHasNoColliderIsRefused() =>
        Assert.Equal(["BUNDLE004 FixtureSlider.Sliders[0].MovingBody.Colliders: a body declares no collider"],
            Errors(("FixtureSlider", new Fixture { Parameters = Fixture.SpringParameters, Sliders = [Fixture.Slider(moving: [])] })));

    [Fact]
    public void ASliderMustTravelThroughItsRestFrame() =>
        Assert.Equal(["BUNDLE006 FixtureSlider.Sliders[0].LowerLimit: outside its declared range"],
            Errors(("FixtureSlider", new Fixture { Parameters = Fixture.SpringParameters, Sliders = [Fixture.Slider() with { LowerLimit = .1f }] })));

    [Fact]
    public void ASliderWithANonFiniteMovingMassIsRefused() =>
        Assert.Equal(["BUNDLE002 FixtureSlider.Sliders[0].MovingBody.Mass: not finite"],
            Errors(("FixtureSlider", new Fixture
            {
                Parameters = Fixture.SpringParameters,
                Sliders = [Fixture.Slider() with { MovingBody = Fixture.Slider().MovingBody with { Mass = float.NaN } }]
            })));

    [Fact]
    public void AnAssemblyWithTwoDeclarationsIsRefused() =>
        Assert.Equal(["BUNDLE005 FixturePair: declares 2 elements; a package declares exactly one"],
            PackageDiscovery.FromAssembly("FixturePair", typeof(Fixture).Assembly).Errors.Select(error => error.ToString()));

    [Fact]
    public void AnAssemblyWithNoDeclarationIsRefused() =>
        Assert.Equal(["BUNDLE005 FixtureNone: declares 0 elements; a package declares exactly one"],
            PackageDiscovery.FromAssembly("FixtureNone", typeof(IPuzzleElementDeclaration).Assembly).Errors.Select(error => error.ToString()));
}
