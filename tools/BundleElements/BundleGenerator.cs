using CuriousContraptions.Abstractions;
using CuriousContraptions.Bundle;

namespace CuriousContraptions.BundleElements;

public sealed record GeneratedBundle(string? Json, int Count, IReadOnlyList<BundleError> Errors);

public static class BundleGenerator
{
    /// <summary>Bakes authored geometry before structural validation; any error refuses the whole bundle.</summary>
    public static GeneratedBundle Generate(string root)
    {
        var discovered = PackageDiscovery.Discover(root);
        var errors = discovered.Errors.Concat(PackageValidator.Assets(root, discovered.Packages))
            .Concat(PackageValidator.Locators(root, discovered.Packages)).ToList();
        var entries = new List<NamedEntry>();
        if (errors.Count != 0) return new(null, 0, errors);
        foreach (var package in discovered.Packages)
        {
            try
            {
                var declaration = package.Declaration;
                var path = Path.Combine(root, declaration.Metadata.ScenePath["res://".Length..]);
                var baked = SceneGeometryExtractor.Bake(File.ReadAllText(path), BundleSchema.Entry(declaration),
                    (declaration as ISceneGeometryElement)?.SceneBodies);
                entries.Add(new(package.Package, baked));
            }
            catch (Exception error) when (error is ArgumentException or KeyNotFoundException or InvalidOperationException)
            {
                errors.Add(new(BundleErrorCode.BodyWithoutCollider, package.Package + ": " + error.Message));
            }
        }
        errors.AddRange(BundleValidator.Validate(entries));
        return errors.Count > 0 ? new(null, 0, errors)
            : new(BundleSchema.WriteEntries(entries.Select(entry => entry.Entry)), entries.Count, []);
    }
}
