using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Bundle;

namespace CuriousContraptions.BundleElements;

/// <summary>One discovered package and its single declaration.</summary>
public sealed record BundlePackage(string Package, IPuzzleElementDeclaration Declaration);

/// <summary>Admits packages into the bundle: every entry passes the bundle's structural validation (the same check the game runs
/// when it loads the bundle), and each package's icon and scene are files in its own <c>elements/&lt;Package&gt;/</c> directory
/// (Stories 9A.14 and 9A.18: a missing icon fails the bundle build).</summary>
public static partial class PackageValidator
{
    /// <summary>The scene-resource naming boundary: a socket locator is <c>Loc_Port_&lt;PortId&gt;</c>, and a tube mouth locator
    /// (Story 9R.6) is <c>Loc_Port_mouth_start</c> or <c>Loc_Port_mouth_end</c>.</summary>
    private const string LocatorPrefix = "Loc_Port_";
    private static readonly HashSet<string> TubeMouths = new(StringComparer.Ordinal) { "mouth_start", "mouth_end" };

    /// <summary>The f32 rounding a scene's decimal text may differ from the declared anchor by (metres).</summary>
    private const float AnchorTolerance = 1e-5f;

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

    /// <summary>Story 9A.17: the declared ports are the one port source, and each package's scene agrees with them. A scene keeps a
    /// socket locator only under its typed socket's exact name, as a direct child of its owning body's scene root (the scene root for
    /// body 0, <see cref="ISceneGeometryElement.SceneBodies"/>[Body].Root otherwise, Story 10.3a), at the declared anchor in that body's
    /// frame; a port naming no declared scene body is refused; every declared port has its locator; and the only other locators are
    /// the two tube mouths. A scene that is not a package file is reported by <see cref="Assets"/> and skipped here.</summary>
    public static IReadOnlyList<BundleError> Locators(string root, IReadOnlyList<BundlePackage> packages)
    {
        ArgumentNullException.ThrowIfNull(packages);
        var errors = new List<BundleError>();
        foreach (var package in packages)
        {
            var path = PackageFile(root, package.Package, package.Declaration.Metadata.ScenePath);
            if (path is null) continue;
            var locators = new Dictionary<string, SceneLocator>(StringComparer.Ordinal);
            var scene = $"{package.Package}.Metadata.ScenePath";
            foreach (var locator in SceneLocators(File.ReadAllText(path)))
            {
                if (!locators.TryAdd(locator.Name, locator))
                    errors.Add(new(BundleErrorCode.UndeclaredPortLocator, $"{scene}: {LocatorPrefix}{locator.Name} appears more than once"));
                else if (!TubeMouths.Contains(locator.Name) && !package.Declaration.Ports.Any(port => port.Id.Value == locator.Name))
                    errors.Add(new(BundleErrorCode.UndeclaredPortLocator, $"{scene}: {LocatorPrefix}{locator.Name} names no declared socket or tube mouth"));
            }
            for (var i = 0; i < package.Declaration.Ports.Count; i++)
            {
                var port = package.Declaration.Ports[i];
                var name = port.Id.Value;
                if (string.IsNullOrWhiteSpace(name)) continue;
                var where = $"{package.Package}.Ports[{i}]";
                string expectedParent;
                if (port.Body.Value == 0) expectedParent = ".";
                else if (package.Declaration is ISceneGeometryElement geometry && port.Body.Value > 0 && port.Body.Value < geometry.SceneBodies.Count)
                    expectedParent = geometry.SceneBodies[port.Body.Value].Root;
                else
                {
                    errors.Add(new(BundleErrorCode.OutOfRange, $"{where}.Body: body {port.Body.Value} is not a declared scene body"));
                    continue;
                }
                if (!locators.TryGetValue(name, out var locator))
                    errors.Add(new(BundleErrorCode.MissingPortLocator, $"{where}: the scene has no {LocatorPrefix}{name} locator"));
                else if (locator.Parent != expectedParent)
                    errors.Add(new(BundleErrorCode.PortLocatorDisagrees, expectedParent == "."
                        ? $"{where}: {LocatorPrefix}{name} is not a child of the scene root"
                        : $"{where}: {LocatorPrefix}{name} is not a child of '{expectedParent}'"));
                else if (locator.Origin is not { } origin)
                    errors.Add(new(BundleErrorCode.PortLocatorDisagrees, $"{where}: {LocatorPrefix}{name} has no readable transform"));
                else if (Vector3.Distance(origin, port.LocalPosition) > AnchorTolerance)
                    errors.Add(new(BundleErrorCode.PortLocatorDisagrees,
                        $"{where}: declared at {Text(port.LocalPosition)}, {LocatorPrefix}{name} is at {Text(origin)}"));
            }
        }
        return errors;
    }

    private static string Text(Vector3 value) => string.Create(CultureInfo.InvariantCulture, $"({value.X}, {value.Y}, {value.Z})");

    /// <summary>One <c>Loc_Port_</c> node of a scene: its name after the prefix, its parent's path ("." for the scene root), and its
    /// local origin (null when its transform cannot be read; a node with no transform line is at the origin).</summary>
    private sealed record SceneLocator(string Name, string Parent, Vector3? Origin);

    [GeneratedRegex(@"^\[node name=""Loc_Port_(?<name>[^""]*)""(?<attributes>[^\]]*)\]$", RegexOptions.CultureInvariant)]
    private static partial Regex LocatorNode();

    [GeneratedRegex(@" parent=""(?<parent>[^""]*)""", RegexOptions.CultureInvariant)]
    private static partial Regex ParentAttribute();

    [GeneratedRegex(@"^transform = Transform3D\((?<values>[^)]*)\)$", RegexOptions.CultureInvariant)]
    private static partial Regex TransformLine();

    /// <summary>The scene text's locator nodes. A node's properties are the lines up to the next section header.</summary>
    private static IEnumerable<SceneLocator> SceneLocators(string scene)
    {
        var lines = scene.Split('\n').Select(line => line.TrimEnd('\r')).ToArray();
        for (var i = 0; i < lines.Length; i++)
        {
            var node = LocatorNode().Match(lines[i]);
            if (!node.Success) continue;
            var name = node.Groups["name"].Value;
            var parent = ParentAttribute().Match(node.Groups["attributes"].Value);
            Vector3? origin = Vector3.Zero;
            while (++i < lines.Length && !lines[i].StartsWith('['))
            {
                var transform = TransformLine().Match(lines[i]);
                if (!transform.Success) continue;
                origin = Origin(transform.Groups["values"].Value);
                break;
            }
            yield return new(name, parent.Success ? parent.Groups["parent"].Value : ".", origin);
        }
    }

    /// <summary>A Transform3D value's origin vector, or null when the transform is malformed.</summary>
    private static Vector3? Origin(string values)
    {
        var parts = values.Split(',').Select(part => part.Trim()).ToArray();
        if (parts.Length != 12) return null;
        return float.TryParse(parts[9], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &
               float.TryParse(parts[10], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) &
               float.TryParse(parts[11], NumberStyles.Float, CultureInfo.InvariantCulture, out var z)
            ? new Vector3(x, y, z) : null;
    }

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
