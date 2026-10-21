using CuriousContraptions.Abstractions;
using CuriousContraptions.Bundle;

namespace CuriousContraptions.BundleElements;

/// <summary>One discovered package and its single declaration.</summary>
public sealed record BundlePackage(string Package, IPuzzleElementDeclaration Declaration);

/// <summary>Admits packages into the bundle: every entry passes the bundle's structural validation (the same check the game runs
/// when it loads the bundle), and each package's icon and scene are files in its own <c>elements/&lt;Package&gt;/</c> directory
/// (Stories 9A.14 and 9A.18: a missing icon fails the bundle build).</summary>
public static class PackageValidator
{
    public static IReadOnlyList<BundleError> Validate(IReadOnlyList<BundlePackage> packages)
    {
        ArgumentNullException.ThrowIfNull(packages);
        return BundleValidator.Validate(packages.Select(package => new NamedEntry(package.Package, BundleSchema.Entry(package.Declaration))).ToArray());
    }

    /// <summary>Every declared asset path that is not a file in its own package directory under <paramref name="root"/>.</summary>
    public static IReadOnlyList<BundleError> Assets(string root, IReadOnlyList<BundlePackage> packages)
    {
        ArgumentNullException.ThrowIfNull(packages);
        var errors = new List<BundleError>();
        foreach (var package in packages)
        {
            var metadata = package.Declaration.Metadata;
            Asset(root, package.Package, "Metadata.IconPath", metadata.IconPath, errors);
            Asset(root, package.Package, "Metadata.ScenePath", metadata.ScenePath, errors);
        }
        return errors;
    }

    private static void Asset(string root, string package, string field, string path, List<BundleError> errors)
    {
        var prefix = $"res://elements/{package}/";
        var relative = path.StartsWith(prefix, StringComparison.Ordinal) ? path["res://".Length..] : null;
        if (relative is null || relative.Contains("..", StringComparison.Ordinal) || !File.Exists(Path.Combine(root, relative)))
            errors.Add(new(BundleErrorCode.MissingAsset, $"{package}.{field}: '{path}' is not a file in elements/{package}/"));
    }
}
