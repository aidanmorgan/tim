using System.Numerics;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.BundleElements.Tests;

/// <summary>Story 9A.4 AC4: generation fails naming the package and the field (the spec's edge-case matrix).</summary>
public sealed class BundleValidatorTests
{
    private static string[] Errors(params (string Package, IPuzzleElementDeclaration Declaration)[] packages) =>
        BundleValidator.Validate(packages.Select(package => new BundlePackage(package.Package, package.Declaration)).ToArray())
            .Select(error => error.ToString()).ToArray();

    [Fact]
    public void AValidPackageIsAdmitted() => Assert.Empty(Errors(("Fixture", new Fixture())));

    [Fact]
    public void ADuplicateIdNamesBothPackages() =>
        Assert.Equal(["BUNDLE001 duplicate id 'x' (FixtureA, FixtureB)"],
            Errors(("FixtureA", new Fixture { Id = "x" }), ("FixtureB", new Fixture { Id = "x" })));

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
        Assert.Equal(["BUNDLE004 FixtureBody.Colliders: a mechanical body declares no collider"],
            Errors(("FixtureBody", new Fixture { Colliders = [] })));

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
    public void AnAssemblyWithTwoDeclarationsIsRefused() =>
        Assert.Equal(["BUNDLE005 FixturePair: declares 2 elements; a package declares exactly one"],
            PackageDiscovery.FromAssembly("FixturePair", typeof(Fixture).Assembly).Errors.Select(error => error.ToString()));

    [Fact]
    public void AnAssemblyWithNoDeclarationIsRefused() =>
        Assert.Equal(["BUNDLE005 FixtureNone: declares 0 elements; a package declares exactly one"],
            PackageDiscovery.FromAssembly("FixtureNone", typeof(IPuzzleElementDeclaration).Assembly).Errors.Select(error => error.ToString()));
}
