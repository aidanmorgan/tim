using CuriousContraptions.Abstractions;
using CuriousContraptions.Bundle;

namespace CuriousContraptions.BundleElements;

public sealed record GeneratedBundle(string? Json, int Count, IReadOnlyList<BundleError> Errors);

public static class BundleGenerator
{
    /// <summary>Bakes authored geometry before structural validation; any error refuses the whole bundle.</summary>
    public static GeneratedBundle Generate(IBundleServices services, string root) => Generate(services, root, PackageDiscovery.Discover(root));

    /// <summary>The same generation over packages already discovered; the test project supplies fixture declarations through it. A
    /// scene the extractor refuses is reported under the refusal's own code; any other failure is not a refusal and stops the build.</summary>
    internal static GeneratedBundle Generate(IBundleServices services, string root, DiscoveryResult discovered)
    {
        var errors = discovered.Errors.Concat(PackageValidator.Assets(root, discovered.Packages))
            .Concat(PackageValidator.Locators(root, discovered.Packages)).ToList();
        var entries = new List<NamedEntry>();
        if (errors.Count != 0) return new(null, 0, errors);
        foreach (var package in discovered.Packages)
        {
            var declaration = package.Declaration;
            var scene = File.ReadAllText(Path.Combine(root, declaration.Metadata.ScenePath["res://".Length..]));
            var entry = services.Entry(declaration);
            try
            {
                entries.Add(new(package.Package, SceneGeometryExtractor.Bake(scene, entry, declaration.SceneBodies)));
            }
            catch (SceneGeometryRefusalException refusal)
            {
                errors.Add(new(refusal.Code, package.Package + ": " + refusal.Message));
            }
        }
        errors.AddRange(services.Validate(entries));
        return errors.Count > 0 ? new(null, 0, errors)
            : new(services.WriteEntries(entries.Select(entry => entry.Entry)), entries.Count, []);
    }
}
