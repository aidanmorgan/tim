using CuriousContraptions.Bundle;

namespace CuriousContraptions.BundleElements;

/// <summary>The bundle text, or every reason it cannot be generated.</summary>
public sealed record GeneratedBundle(string? Json, int Count, IReadOnlyList<BundleError> Errors);

public static class BundleGenerator
{
    /// <summary>Discovers, validates and writes every package under <paramref name="root"/>. Nothing is written when any package fails.</summary>
    public static GeneratedBundle Generate(string root)
    {
        var discovered = PackageDiscovery.Discover(root);
        var errors = discovered.Errors.Concat(PackageValidator.Validate(discovered.Packages))
            .Concat(PackageValidator.Assets(root, discovered.Packages)).Concat(PackageValidator.Locators(root, discovered.Packages)).ToArray();
        return errors.Length > 0
            ? new(null, 0, errors)
            : new(BundleSchema.Write(discovered.Packages.Select(package => package.Declaration)), discovered.Packages.Count, []);
    }
}
