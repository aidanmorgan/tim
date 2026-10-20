using System.Text.RegularExpressions;

namespace CuriousContraptions.BundleElements.Tests;

/// <summary>Story 9A.4 AC1 and AC2: the solution builds every package, the domain Abstractions and the generator; the generator finds
/// packages by glob with no hand list; and the web host generates the bundle before the Godot export and before publish.</summary>
public sealed partial class SolutionMembershipTests
{
    [GeneratedRegex(@"<Project Path=""([^""]+)""")]
    private static partial Regex ProjectPath();

    private static string[] Required()
    {
        var root = TestRepository.Root;
        var elements = Directory.EnumerateDirectories(Path.Combine(root, "elements"))
            .SelectMany(directory => Directory.EnumerateFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly))
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'));
        string[] fixedProjects =
        [
            "CuriousContraptions.Abstractions/CuriousContraptions.Abstractions.csproj",
            "CuriousContraptions.Abstractions.Electrical/CuriousContraptions.Abstractions.Electrical.csproj",
            "CuriousContraptions.Abstractions.Fluid/CuriousContraptions.Abstractions.Fluid.csproj",
            "CuriousContraptions.Abstractions.Mechanical/CuriousContraptions.Abstractions.Mechanical.csproj",
            "CuriousContraptions.Abstractions.Signal/CuriousContraptions.Abstractions.Signal.csproj",
            "tools/BundleElements/BundleElements.csproj",
            "tools/BundleElements.Tests/BundleElements.Tests.csproj"
        ];
        return [.. elements.Concat(fixedProjects).Order(StringComparer.Ordinal)];
    }

    [Fact]
    public void TheSolutionContainsEveryPackageTheDomainAbstractionsAndTheGenerator()
    {
        var listed = ProjectPath().Matches(TestRepository.Read("CuriousContraptions.slnx")).Select(match => match.Groups[1].Value).ToHashSet();
        var required = Required();
        Assert.True(required.Length >= 20, $"Only {required.Length} required projects were found.");
        Assert.Equal([], required.Where(path => !listed.Contains(path)).ToArray());
    }

    [Fact]
    public void EveryPackageIsDiscoveredByGlobWithNoHandList()
    {
        var names = PackageDiscovery.PackageNames(TestRepository.Root);
        var discovered = PackageDiscovery.Discover(TestRepository.Root);
        Assert.Empty(discovered.Errors);
        Assert.Equal(names, discovered.Packages.Select(package => package.Package));
        // The generator's sources and project never name a package: the glob is the only list.
        var tool = Path.Combine(TestRepository.Root, "tools", "BundleElements");
        var sources = Directory.EnumerateFiles(tool, "*.cs", SearchOption.TopDirectoryOnly).Append(Path.Combine(tool, "BundleElements.csproj"))
            .Select(File.ReadAllText).ToArray();
        foreach (var package in discovered.Packages)
        {
            var type = package.Declaration.GetType();
            Assert.DoesNotContain(sources, source => source.Contains(type.Name, StringComparison.Ordinal) ||
                source.Contains("elements/" + package.Package + "/", StringComparison.Ordinal) ||
                source.Contains('"' + package.Declaration.Metadata.Id + '"', StringComparison.Ordinal));
        }
        Assert.Contains("<ProjectReference Include=\"../../elements/*/*.csproj\" />", sources[^1], StringComparison.Ordinal);
    }

    [Fact]
    public void TheWebHostGeneratesTheBundleBeforeTheExportAndBeforePublish()
    {
        var web = TestRepository.Read("CuriousContraptions.web/CuriousContraptions.web.csproj");
        var target = Regex.Match(web, @"<Target Name=""GenerateElementBundle""[^>]*BeforeTargets=""([^""]+)""");
        Assert.True(target.Success, "CuriousContraptions.web.csproj has no GenerateElementBundle target.");
        var before = target.Groups[1].Value.Split(';');
        Assert.Contains("TwoDogExportGamePack", before);
        Assert.Contains("_WasmGenerateAppBundle", before);
        Assert.Contains("../tools/BundleElements/BundleElements.csproj", web, StringComparison.Ordinal);
        Assert.Contains("AfterTargets=\"Build\"", TestRepository.Read("tools/BundleElements/BundleElements.csproj"), StringComparison.Ordinal);
    }
}
