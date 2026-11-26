using System.Globalization;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Bundle;

namespace CuriousContraptions.BundleElements;

/// <summary>One discovered package and its single declaration.</summary>
public sealed record BundlePackage(string Package, IPuzzleElementDeclaration Declaration);

/// <summary>The bundle build's package checks before baking: each package's icon and scene are files in its own
/// <c>elements/&lt;Package&gt;/</c> directory (Stories 9A.14 and 9A.18: a missing icon fails the bundle build), and its scene's
/// locators agree with its declared ports (Story 9A.17).</summary>
public static class PackageValidator
{
    /// <summary>The scene-resource naming boundary: a locator is <c>Loc_Port_&lt;name&gt;</c>, where the name is a declared socket's
    /// <see cref="PortId"/> or a tube mouth (<see cref="TubeMouth"/>).</summary>
    internal const string LocatorPrefix = "Loc_Port_";

    /// <summary>The one tube-mouth naming rule (Story 9R.6), read by the locator check and by the scene extractor: a name with the
    /// <c>mouth_</c> prefix names a tube mouth, never a declared socket, and the only tube mouths are <c>mouth_start</c> and
    /// <c>mouth_end</c>. Returns whether <paramref name="name"/> has the prefix; <paramref name="end"/> is the end it names, or null
    /// for a prefixed name that names neither end, which both refuse.</summary>
    internal static bool TubeMouth(string name, out TubeEnd? end)
    {
        end = name switch { "mouth_start" => TubeEnd.Start, "mouth_end" => TubeEnd.End, _ => null };
        return name.StartsWith("mouth_", StringComparison.Ordinal);
    }

    /// <summary>The f32 rounding a scene's decimal text may differ from the declared anchor by (metres).</summary>
    private const float AnchorTolerance = 1e-5f;

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

    /// <summary>Story 9A.17: the declared ports are the one port source, and each package's scene agrees with them. The scene is read
    /// by the one scene parser (<see cref="SceneGeometryExtractor.Locators"/>); scene text it cannot read is refused under the
    /// parser's code. A scene keeps a socket locator only under its typed socket's exact name, as a direct child of its owning body's
    /// scene root (the scene root for body 0, <see cref="ISceneGeometryElement.SceneBodies"/>[Body].Root otherwise, Story 10.3a), at
    /// the declared anchor in that body's frame; a port naming no declared scene body, or taking a tube-mouth name, is refused; every
    /// declared port has its locator; and the only other locators are the two tube mouths. A scene that is not a package file is
    /// reported by <see cref="Assets"/> and skipped here.</summary>
    public static IReadOnlyList<BundleError> Locators(string root, IReadOnlyList<BundlePackage> packages)
    {
        ArgumentNullException.ThrowIfNull(packages);
        var errors = new List<BundleError>();
        foreach (var package in packages)
        {
            var path = PackageFile(root, package.Package, package.Declaration.Metadata.ScenePath);
            if (path is null) continue;
            IReadOnlyList<SceneGeometryExtractor.SceneLocator> read;
            try
            {
                read = SceneGeometryExtractor.Locators(File.ReadAllText(path));
            }
            catch (SceneGeometryRefusalException refusal)
            {
                errors.Add(new(refusal.Code, package.Package + ": " + refusal.Message));
                continue;
            }
            var locators = SceneLocatorErrors(package, read, errors);
            for (var i = 0; i < package.Declaration.Ports.Count; i++)
                PortLocatorErrors(package, i, locators, errors);
        }
        return errors;
    }

    /// <summary>The scene's locators by name, after refusing a name stated twice, a tube-mouth name that names neither end, and any
    /// other name that names no declared socket.</summary>
    private static Dictionary<string, SceneGeometryExtractor.SceneLocator> SceneLocatorErrors(BundlePackage package,
        IReadOnlyList<SceneGeometryExtractor.SceneLocator> read, List<BundleError> errors)
    {
        var locators = new Dictionary<string, SceneGeometryExtractor.SceneLocator>(StringComparer.Ordinal);
        var scene = $"{package.Package}.Metadata.ScenePath";
        foreach (var locator in read)
        {
            if (!locators.TryAdd(locator.Name, locator))
                errors.Add(new(BundleErrorCode.UndeclaredPortLocator, $"{scene}: {LocatorPrefix}{locator.Name} appears more than once"));
            else if (TubeMouth(locator.Name, out var end))
            {
                if (end is null)
                    errors.Add(new(BundleErrorCode.UndeclaredPortLocator,
                        $"{scene}: {LocatorPrefix}{locator.Name} names no tube mouth; the mouth_ prefix names mouth_start or mouth_end only"));
            }
            else if (!package.Declaration.Ports.Any(port => port.Id.Value == locator.Name))
                errors.Add(new(BundleErrorCode.UndeclaredPortLocator, $"{scene}: {LocatorPrefix}{locator.Name} names no declared socket or tube mouth"));
        }
        return locators;
    }

    /// <summary>The refusals of declared port <paramref name="index"/>: a tube-mouth name, an undeclared scene body, a missing
    /// locator, or a locator outside its body root or away from the declared anchor.</summary>
    private static void PortLocatorErrors(BundlePackage package, int index, Dictionary<string, SceneGeometryExtractor.SceneLocator> locators,
        List<BundleError> errors)
    {
        var port = package.Declaration.Ports[index];
        var name = port.Id.Value;
        if (string.IsNullOrWhiteSpace(name)) return;
        var where = $"{package.Package}.Ports[{index}]";
        if (TubeMouth(name, out _))
        {
            errors.Add(new(BundleErrorCode.OutOfRange, $"{where}.Id: {name} takes the mouth_ prefix, which names a tube mouth, never a socket"));
            return;
        }
        string expectedParent;
        if (port.Body.Value == 0) expectedParent = ".";
        else if (port.Body.Value > 0 && port.Body.Value < package.Declaration.SceneBodies.Count)
            expectedParent = package.Declaration.SceneBodies[port.Body.Value].Root;
        else
        {
            errors.Add(new(BundleErrorCode.OutOfRange, $"{where}.Body: body {port.Body.Value} is not a declared scene body"));
            return;
        }
        if (!locators.TryGetValue(name, out var locator))
            errors.Add(new(BundleErrorCode.MissingPortLocator, $"{where}: the scene has no {LocatorPrefix}{name} locator"));
        else if (locator.Parent != expectedParent)
            errors.Add(new(BundleErrorCode.PortLocatorDisagrees, expectedParent == "."
                ? $"{where}: {LocatorPrefix}{name} is not a child of the scene root"
                : $"{where}: {LocatorPrefix}{name} is not a child of '{expectedParent}'"));
        else if (Vector3.Distance(locator.Origin, port.LocalPosition) > AnchorTolerance)
            errors.Add(new(BundleErrorCode.PortLocatorDisagrees,
                $"{where}: declared at {Text(port.LocalPosition)}, {LocatorPrefix}{name} is at {Text(locator.Origin)}"));
    }

    private static string Text(Vector3 value) => string.Create(CultureInfo.InvariantCulture, $"({value.X}, {value.Y}, {value.Z})");

    private static void Asset(string root, string package, string field, string path, List<BundleError> errors)
    {
        if (PackageFile(root, package, path) is null)
            errors.Add(new(BundleErrorCode.MissingAsset, $"{package}.{field}: '{path}' is not a file in elements/{package}/"));
    }

    private static string? PackageFile(string root, string package, string resourcePath)
    {
        var prefix = $"res://elements/{package}/";
        if (!resourcePath.StartsWith(prefix, StringComparison.Ordinal)) return null;
        var relative = resourcePath[prefix.Length..];
        var file = Path.Combine(root, "elements", package, relative);
        return File.Exists(file) ? file : null;
    }
}
