using System.Xml.Linq;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Fluid;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.BundleElements.Tests;

/// <summary>Stories 9A.5 and 9A.6 (PKG-1): every element package references exactly the general Abstractions and the domain
/// Abstractions its declaration implements; never the engine, a sibling package or a NuGet package.</summary>
public sealed class PackageReferenceTests
{
    public static TheoryData<string> Packages() => [.. PackageDiscovery.PackageNames(TestRepository.Root)];

    private const string General = "CuriousContraptions.Abstractions/CuriousContraptions.Abstractions.csproj";

    private static string Domain(string name) => $"CuriousContraptions.Abstractions.{name}/CuriousContraptions.Abstractions.{name}.csproj";

    /// <summary>The package's project and NuGet references; project paths relative to the repository root.</summary>
    private static (string[] Projects, string[] Packages) References(string package)
    {
        var directory = Path.Combine(TestRepository.Root, "elements", package);
        var project = XDocument.Load(Path.Combine(directory, package + ".csproj"));
        var projects = project.Descendants("ProjectReference").Select(reference => (string?)reference.Attribute("Include") ?? "")
            .Select(include => Path.GetRelativePath(TestRepository.Root, Path.GetFullPath(Path.Combine(directory, include))).Replace('\\', '/'))
            .Order(StringComparer.Ordinal).ToArray();
        var packages = project.Descendants("PackageReference").Select(reference => (string?)reference.Attribute("Include") ?? "").ToArray();
        return (projects, packages);
    }

    [Theory]
    [MemberData(nameof(Packages))]
    public void EveryPackageReferencesExactlyTheAbstractionsItsDeclarationImplements(string package)
    {
        var discovered = PackageDiscovery.Discover(TestRepository.Root);
        Assert.Empty(discovered.Errors);
        var declaration = discovered.Packages.Single(entry => entry.Package == package).Declaration;
        var needed = new List<string> { General };
        if (declaration is IMechanicalElement) needed.Add(Domain("Mechanical"));
        if (declaration is IElectricalElement) needed.Add(Domain("Electrical"));
        if (declaration is ISignalElement) needed.Add(Domain("Signal"));
        if (declaration is IFluidElement) needed.Add(Domain("Fluid"));
        var (projects, packages) = References(package);
        Assert.Equal(needed.Order(StringComparer.Ordinal), projects);
        Assert.Empty(packages);
    }

    [Fact]
    public void TheReceiverBowlingBallAndSpringboardPackagesExist() =>
        Assert.Superset(new HashSet<string>(["BowlingBall", "Receiver", "Springboard"], StringComparer.Ordinal),
            PackageDiscovery.PackageNames(TestRepository.Root).ToHashSet(StringComparer.Ordinal));

    /// <summary>PKG-2 (Stories 9A.8 to 9A.11).</summary>
    [Fact]
    public void TheFunnelPipeAndPipeBendPackagesExist() =>
        Assert.Superset(new HashSet<string>(["Funnel", "Pipe", "PipeBend45", "PipeBend90"], StringComparer.Ordinal),
            PackageDiscovery.PackageNames(TestRepository.Root).ToHashSet(StringComparer.Ordinal));

    /// <summary>PKG-3 (Stories 9A.7 and 9A.13).</summary>
    [Fact]
    public void TheDelayBoxAndImpactSwitchPackagesExist() =>
        Assert.Superset(new HashSet<string>(["DelayBox", "ImpactSwitch"], StringComparer.Ordinal),
            PackageDiscovery.PackageNames(TestRepository.Root).ToHashSet(StringComparer.Ordinal));
}
