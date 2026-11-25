using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Bundle;

namespace CuriousContraptions.BundleElements;

/// <summary>Reads the package's authored text scene at build time. Runtime never opens scenes for physics.</summary>
public static partial class SceneGeometryExtractor
{
    private sealed record Section(string Kind, Dictionary<string, string> Attributes, Dictionary<string, string> Values);
    private sealed record Node(string Name, string Type, string Path, string? Parent, Dictionary<string, string> Values);

    /// <summary>The entry with its colliders and tube mouths baked from the scene. A package's colliders are its scene's proxies, each
    /// on the body whose <paramref name="ownership"/> root it sits under: the base body's on the entry, each moving body's as its list
    /// in the payload's moving-body collider lists.</summary>
    public static BundleEntry Bake(string text, BundleEntry entry, IReadOnlyList<SceneBodyGeometry> ownership)
    {
        var sections = Parse(text);
        var resources = Resources(sections);
        var nodes = ReadSceneNodes(sections);
        var byPath = NodesByPath(nodes);
        foreach (var node in nodes.Skip(1))
            if (node.Parent is null || !byPath.ContainsKey(node.Parent)) throw Refuse(BundleErrorCode.UnresolvedReference, "Scene has an unresolved parent.");
        RefuseInfixProxyNames(nodes);
        var proxies = nodes.Where(n => n.Type == "CollisionShape3D" ||
            n.Name.StartsWith("col_", StringComparison.OrdinalIgnoreCase)).ToArray();
        var sliders = entry.Mechanical?.Sliders ?? [];
        var hinges = entry.Mechanical?.Hinges ?? [];
        if (ownership.Count != 1 + sliders.Count + hinges.Count || ownership[0].Root != ".")
            throw Refuse(BundleErrorCode.SceneColliderOwnership, "Scene ownership must name the base, every slider and every hinge body.");
        ValidateBodyRoots(ownership, byPath);
        var bodies = Enumerable.Range(0, ownership.Count).Select(_ => new List<ColliderDescriptor>()).ToArray();
        BakeBodies(proxies, resources, byPath, ownership, bodies);
        if (bodies.Any(body => body.Count == 0)) throw Refuse(BundleErrorCode.BodyWithoutCollider, "A scene body has no collider.");
        return entry with
        {
            Colliders = bodies[0],
            Mechanical = entry.Mechanical is { } mechanical ? mechanical with { MovingBodyColliders = [.. bodies.Skip(1)] } : null,
            TubeMouths = Mouths(nodes, byPath)
        };
    }

    /// <summary>A <c>Loc_Port_</c> node as the locator check reads it: its name after the prefix, its parent's path ("." for the scene
    /// root) and its local origin.</summary>
    internal sealed record SceneLocator(string Name, string Parent, Vector3 Origin);

    /// <summary>Every <c>Loc_Port_</c> node of <paramref name="text"/>, read by this one scene parser, so the locator check and the
    /// bake read the same nodes and poses. Unreadable scene text or locator pose, and a parentless locator, are refused.</summary>
    internal static IReadOnlyList<SceneLocator> Locators(string text) =>
        ReadSceneNodes(Parse(text)).Where(node => node.Name.StartsWith(PackageValidator.LocatorPrefix, StringComparison.Ordinal))
            .Select(node => new SceneLocator(node.Name[PackageValidator.LocatorPrefix.Length..], node.Parent!, LocatorOrigin(node)))
            .ToArray();

    private static void BakeBodies(Node[] proxies, Dictionary<string, Section> resources, Dictionary<string, Node> byPath,
        IReadOnlyList<SceneBodyGeometry> ownership, List<ColliderDescriptor>[] bodies)
    {
        foreach (var node in proxies)
        {
            var owner = 0;
            for (var i = 1; i < ownership.Count; i++)
                if (Below(node.Path, ownership[i].Root) || node.Path == ownership[i].Root) owner = i;
            if (BakeProxy(node, proxies, resources, byPath, ownership[owner].Root, ownership[owner].Material) is { } collider)
                bodies[owner].Add(collider);
        }
    }

    private static List<TubeMouthDescriptor> Mouths(Node[] nodes, Dictionary<string, Node> byPath)
    {
        var mouths = new List<TubeMouthDescriptor>();
        foreach (var node in nodes.Where(n => n.Name.StartsWith(PackageValidator.LocatorPrefix, StringComparison.Ordinal)))
        {
            if (!PackageValidator.TubeMouth(node.Name[PackageValidator.LocatorPrefix.Length..], out var named)) continue;
            var end = named ?? throw Refuse(BundleErrorCode.UndeclaredPortLocator, "Unknown tube mouth: " + node.Path);
            if (mouths.Any(m => m.End == end)) throw Refuse(BundleErrorCode.UndeclaredPortLocator, "Duplicate tube mouth.");
            var (position, rotation, _) = Pose(node, ".", byPath);
            var bore = Number(node.Values.TryGetValue("metadata/bore_radius", out var boreText) ? boreText : throw Refuse(BundleErrorCode.MalformedScene, "A tube mouth has no metadata/bore_radius: " + node.Path));
            if (bore <= 0) throw Refuse(BundleErrorCode.OutOfRange, "Invalid tube bore.");
            mouths.Add(new(end, position, rotation, bore));
        }
        return mouths;
    }

    private static (Vector3 Position, Quaternion Rotation, Vector3 Scale) Pose(Node node, string root, Dictionary<string, Node> byPath)
    {
        if (node.Path == root) { _ = Local(node.Values); return (Vector3.Zero, Quaternion.Identity, Vector3.One); }
        var local = Local(node.Values);
        if (node.Parent == root) return local;
        var parent = Pose(byPath[node.Parent!], root, byPath);
        var matrix = Matrix4x4.CreateScale(local.Scale) * Matrix4x4.CreateFromQuaternion(local.Rotation) *
            Matrix4x4.CreateTranslation(local.Position) * Matrix4x4.CreateScale(parent.Scale) *
            Matrix4x4.CreateFromQuaternion(parent.Rotation) * Matrix4x4.CreateTranslation(parent.Position);
        if (!Matrix4x4.Decompose(matrix, out var scale, out var rotation, out var position))
            throw Refuse(BundleErrorCode.MalformedScene, "Cannot decompose scene transform.");
        return (position, rotation, scale);
    }

    private static ColliderDescriptor? BakeProxy(Node node, Node[] proxies, Dictionary<string, Section> resources,
        Dictionary<string, Node> byPath, string root, MaterialProperties material)
    {
        if (node.Type == "Node3D" && proxies.Any(child => Below(child.Path, node.Path))) return null;
        if (node.Type is not ("CollisionShape3D" or "MeshInstance3D") ||
            node.Name.StartsWith("col_capsule", StringComparison.OrdinalIgnoreCase) ||
            node.Name.StartsWith("col_hull", StringComparison.OrdinalIgnoreCase))
            throw Refuse(BundleErrorCode.UnsupportedColliderProxy, "Unsupported collider proxy: " + node.Path);
        if (!node.Values.TryGetValue(node.Type == "CollisionShape3D" ? "shape" : "mesh", out var reference)) throw Refuse(BundleErrorCode.UnresolvedReference, "Collider has no shape: " + node.Path);
        var resourceId = Reference(reference);
        if (!resources.TryGetValue(resourceId, out var shape)) throw Refuse(BundleErrorCode.UnresolvedReference, "Collider shape is missing: " + node.Path);
        var (position, rotation, scale) = Pose(node, root, byPath);
        if (scale.X <= 0 || scale.Y <= 0 || scale.Z <= 0) throw Refuse(BundleErrorCode.OutOfRange, "Collider scale must be positive.");
        // engine-hollow-shapes: an open cylinder mesh (both caps off) is a tube or frustum wall, a torus mesh a torus segment.
        if (Hollow(shape, node, scale) is { } hollow)
        {
            return hollow with { Offset = position, Rotation = rotation, Material = material };
        }
        var kind = shape.Attributes["type"] switch
        {
            "BoxShape3D" or "BoxMesh" => ColliderShapeKind.Box,
            "SphereShape3D" or "SphereMesh" => ColliderShapeKind.Sphere,
            "CylinderShape3D" or "CylinderMesh" => ColliderShapeKind.Cylinder,
            _ => throw Refuse(BundleErrorCode.UnsupportedColliderProxy, "Unsupported collider shape: " + shape.Attributes["type"])
        };
        var half = kind switch
        {
            ColliderShapeKind.Box => Vector(Required(shape, "size", node)) * .5f,
            ColliderShapeKind.Sphere => new Vector3(Number(Required(shape, "radius", node))),
            ColliderShapeKind.Cylinder => CylinderHalfExtents(shape, node),
            _ => throw Refuse(BundleErrorCode.UnsupportedColliderProxy, "Unsupported collider shape: " + node.Path)
        };
        half *= scale;
        if (half.X <= 0 || half.Y <= 0 || half.Z <= 0) throw Refuse(BundleErrorCode.OutOfRange, "Collider has nonpositive extent.");
        if (kind == ColliderShapeKind.Sphere && (half.X != half.Y || half.X != half.Z))
            throw Refuse(BundleErrorCode.UnsupportedColliderProxy, "A sphere cannot have nonuniform scale.");
        // A cylinder's axis is its local +Y: its height scales by scale.Y and its radius needs equal radial scale (X = Z).
        if (kind == ColliderShapeKind.Cylinder && half.X != half.Z)
            throw Refuse(BundleErrorCode.UnsupportedColliderProxy, "A cylinder cannot have nonuniform radial scale: " + node.Path);
        return new(kind, half, position, rotation, material, 0f);
    }

    private static void ValidateBodyRoots(IReadOnlyList<SceneBodyGeometry> ownership, Dictionary<string, Node> byPath)
    {
        for (var i = 0; i < ownership.Count; i++)
        {
            if (!byPath.ContainsKey(ownership[i].Root)) throw Refuse(BundleErrorCode.UnresolvedReference, "Body root does not exist: " + ownership[i].Root);
            for (var j = 0; j < i; j++)
                if (ownership[i].Root == ownership[j].Root ||
                    (j != 0 && (Below(ownership[i].Root, ownership[j].Root) || Below(ownership[j].Root, ownership[i].Root))))
                    throw Refuse(BundleErrorCode.SceneColliderOwnership, "Scene body roots overlap.");
        }
    }

    private static Node[] ReadSceneNodes(IReadOnlyList<Section> sections)
    {
        var nodes = sections.Where(s => s.Kind == "node").Select(s =>
        {
            var parent = s.Attributes.GetValueOrDefault("parent");
            var name = s.Attributes.TryGetValue("name", out var named) && named is not ("" or "." or "..") && !named.Contains('/', StringComparison.Ordinal) ? named : throw Refuse(BundleErrorCode.MalformedScene, "A scene node name is missing, empty, '.', '..' or contains '/': " + named);
            return new Node(name, s.Attributes.GetValueOrDefault("type") ?? "",
                parent is null ? "." : parent == "." ? name : parent + "/" + name, parent, s.Values);
        }).ToArray();
        if (nodes.Length == 0 || nodes[0].Parent is not null || nodes[0].Type != "Node3D")
            throw Refuse(BundleErrorCode.MalformedScene, "Scene root must be Node3D.");
        if (nodes.FirstOrDefault(node => node.Parent is null && node.Name.StartsWith(PackageValidator.LocatorPrefix, StringComparison.Ordinal)) is { } root)
            throw Refuse(BundleErrorCode.PortLocatorDisagrees, "A Loc_Port_ locator is a child node, never a scene root: " + root.Name);
        return nodes;
    }

    private static bool Below(string path, string parent) => path.StartsWith(parent + "/", StringComparison.Ordinal);

    /// <summary>A collider proxy is named by its <c>col_</c> prefix. A node whose name carries <c>-col_</c> without that prefix is
    /// refused, naming the node; it is never read as a proxy or as art.</summary>
    private static void RefuseInfixProxyNames(Node[] nodes)
    {
        foreach (var node in nodes)
            if (!node.Name.StartsWith("col_", StringComparison.OrdinalIgnoreCase) && node.Name.Contains("-col_", StringComparison.OrdinalIgnoreCase))
                throw Refuse(BundleErrorCode.ProxyNameInfix, "A collider proxy is named by its col_ prefix; a -col_ infix is refused: " + (node.Path == "." ? node.Name : node.Path));
    }

    /// <summary>A shape value the proxy must state explicitly (Godot omits default-valued keys, so a missing key is never assumed).</summary>
    private static string Required(Section shape, string key, Node node) =>
        shape.Values.TryGetValue(key, out var value) ? value
            : throw Refuse(BundleErrorCode.UnsupportedColliderProxy, $"Collider shape has no explicit '{key}': {node.Path}");

    /// <summary>A hollow proxy's descriptor (its pose and material are the caller's), or null for a solid shape (engine-hollow-shapes).
    /// A hollow proxy is drawn at uniform scale s:
    /// <list type="bullet">
    /// <item>a <c>CylinderMesh</c> with both caps off (<c>cap_top = false</c>, <c>cap_bottom = false</c>) and the node's
    /// <c>metadata/wall_thickness</c> t: equal radii r give the Tube (outer r, inner r − t, half-length height / 2); unequal radii the
    /// Frustum (outer r_0 = bottom_radius at −Y, r_1 = top_radius at +Y, radial wall t);</item>
    /// <item>a <c>TorusMesh</c>: the Torus of centreline (inner_radius + outer_radius) / 2 and tube radius (outer_radius − inner_radius)
    /// / 2, solid unless the node declares <c>metadata/wall_thickness</c>, over the node's <c>metadata/sweep_degrees</c> from local +X
    /// toward +Z, or the full turn without it.</item>
    /// </list>
    /// One open cap, an open cylinder without its wall, or a hollow proxy at nonuniform scale is refused; admission checks the
    /// dimensions.</summary>
    private static ColliderDescriptor? Hollow(Section shape, Node node, Vector3 scale)
    {
        var type = shape.Attributes["type"];
        var open = false;
        if (type == "CylinderMesh")
        {
            // Godot defaults both caps to true; explicit defaults remain solid.
            bool Cap(string key) => !shape.Values.TryGetValue(key, out var value) || value switch
            {
                "true" => true,
                "false" => false,
                _ => throw Refuse(BundleErrorCode.MalformedScene, "A cylinder cap must be true or false: " + node.Path)
            };
            open = !Cap("cap_top") | !Cap("cap_bottom");
        }
        if (type != "TorusMesh" && !open) return null;
        if (scale.X != scale.Y || scale.X != scale.Z) throw Refuse(BundleErrorCode.UnsupportedColliderProxy, "A hollow collider needs uniform scale: " + node.Path);
        var s = scale.X;
        node.Values.TryGetValue("metadata/wall_thickness", out var wallText);
        if (type == "TorusMesh")
        {
            var inner = Number(Required(shape, "inner_radius", node)) * s;
            var outer = Number(Required(shape, "outer_radius", node)) * s;
            var tube = (outer - inner) * .5f;
            var sweep = node.Values.TryGetValue("metadata/sweep_degrees", out var degrees)
                ? Number(degrees) * (MathF.PI / 180f) : MathF.Tau;
            var bore = wallText is null ? 0f : tube - Number(wallText) * s;
            return new(ColliderShapeKind.Torus, new(bore, tube, sweep), default, Quaternion.Identity, default, (inner + outer) * .5f);
        }
        if (Required(shape, "cap_top", node) != "false" || Required(shape, "cap_bottom", node) != "false")
            throw Refuse(BundleErrorCode.UnsupportedColliderProxy, "A hollow cylinder proxy opens both caps: " + node.Path);
        if (wallText is null) throw Refuse(BundleErrorCode.UnsupportedColliderProxy, "An open cylinder proxy declares metadata/wall_thickness: " + node.Path);
        var wall = Number(wallText) * s;
        var top = Number(Required(shape, "top_radius", node)) * s;
        var bottom = Number(Required(shape, "bottom_radius", node)) * s;
        var half = Number(Required(shape, "height", node)) * .5f * s;
        return top == bottom
            ? new(ColliderShapeKind.Tube, new(top - wall, half, 0f), default, Quaternion.Identity, default, top)
            : new(ColliderShapeKind.Frustum, new(wall, half, top), default, Quaternion.Identity, default, bottom);
    }

    /// <summary>(r, h, r) of a Godot cylinder (axis local +Y, h = height / 2). A mesh proxy is a cylinder only when its top and
    /// bottom radii are equal; a tapered mesh is rejected.</summary>
    private static Vector3 CylinderHalfExtents(Section shape, Node node)
    {
        float radius;
        if (shape.Attributes["type"] == "CylinderMesh")
        {
            radius = Number(Required(shape, "top_radius", node));
            if (radius != Number(Required(shape, "bottom_radius", node)))
                throw Refuse(BundleErrorCode.UnsupportedColliderProxy, "A tapered cylinder mesh is not a cylinder collider: " + node.Path);
        }
        else radius = Number(Required(shape, "radius", node));
        var half = Number(Required(shape, "height", node)) * .5f;
        return new(radius, half, radius);
    }

    private static (Vector3 Position, Quaternion Rotation, Vector3 Scale) Local(Dictionary<string, string> values)
    {
        if (values.TryGetValue("transform", out var transform))
        {
            var f = Components(transform, 12);
            var matrix = new Matrix4x4(f[0], f[3], f[6], 0, f[1], f[4], f[7], 0, f[2], f[5], f[8], 0, f[9], f[10], f[11], 1);
            if (!Matrix4x4.Decompose(matrix, out var scale, out var rotation, out var position))
                throw Refuse(BundleErrorCode.MalformedScene, "Cannot decompose scene transform.");
            if (scale.X <= 0 || scale.Y <= 0 || scale.Z <= 0) throw Refuse(BundleErrorCode.OutOfRange, "Scene scale must be positive.");
            return (position, Quaternion.Normalize(rotation), scale);
        }
        var p = values.TryGetValue("position", out var pos) ? Vector(pos) : Vector3.Zero;
        var s = values.TryGetValue("scale", out var size) ? Vector(size) : Vector3.One;
        if (s.X <= 0 || s.Y <= 0 || s.Z <= 0) throw Refuse(BundleErrorCode.OutOfRange, "Scene scale must be positive.");
        var q = Quaternion.Identity;
        if (values.TryGetValue("quaternion", out var quaternion))
        {
            var f = Components(quaternion, 4);
            q = new(f[0], f[1], f[2], f[3]);
            if (MathF.Abs(q.LengthSquared() - 1f) > 1e-5f) throw Refuse(BundleErrorCode.MalformedScene, "Invalid scene quaternion.");
        }
        else if (values.TryGetValue("rotation", out var rotation))
        {
            var e = Vector(rotation);
            q = Quaternion.CreateFromYawPitchRoll(e.Y, e.X, e.Z);
        }
        return (p, q, s);
    }

    private static Vector3 Vector(string value)
    {
        var f = Components(value, 3);
        return new(f[0], f[1], f[2]);
    }
    private static float[] Components(string value, int count)
    {
        var begin = value.IndexOf('(');
        if (begin < 0 || !value.EndsWith(')')) throw Refuse(BundleErrorCode.MalformedScene, "Malformed scene numeric value.");
        var result = value[(begin + 1)..^1].Split(',').Select(Number).ToArray();
        if (result.Length != count) throw Refuse(BundleErrorCode.MalformedScene, "Wrong scene numeric arity.");
        return result;
    }
    private static float Number(string value)
    {
        if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)) throw Refuse(BundleErrorCode.MalformedScene, "Invalid scene number.");
        if (!float.IsFinite(result)) throw Refuse(BundleErrorCode.NonFinite, "Nonfinite scene number.");
        return result;
    }
    /// <summary>The id of a local sub-resource reference, <c>SubResource("&lt;id&gt;")</c>; any other form, or a value too short to hold
    /// both delimiters, is refused.</summary>
    private static string Reference(string value)
    {
        const string open = "SubResource(\"", close = "\")";
        if (value.Length < open.Length + close.Length || !value.StartsWith(open, StringComparison.Ordinal) || !value.EndsWith(close, StringComparison.Ordinal))
            throw Refuse(BundleErrorCode.UnsupportedColliderProxy, "Collider shape must be a local subresource.");
        return value[open.Length..^close.Length];
    }

    /// <summary>A locator's local origin. A pose the parser cannot read is refused under the parser's code, naming the locator.</summary>
    private static Vector3 LocatorOrigin(Node node)
    {
        try
        {
            return Local(node.Values).Position;
        }
        catch (SceneGeometryRefusalException refusal)
        {
            throw Refuse(refusal.Code, node.Path + ": " + refusal.Message);
        }
    }

    /// <summary>The scene's sections in file order. Every line is blank, a <c>;</c> comment, a section header or a one-line
    /// <c>key = value</c> of the section above it. Any other line (a value continued onto a later line among them), a line before the
    /// first section, a section header without its closing bracket, a section attribute stated twice and a value stated twice are refused as
    /// <see cref="BundleErrorCode.MalformedScene"/>.</summary>
    private static List<Section> Parse(string text)
    {
        var result = new List<Section>();
        Section? current = null;
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith(';')) continue;
            if (line.StartsWith('['))
            {
                if (!line.EndsWith(']')) throw Refuse(BundleErrorCode.MalformedScene, "Malformed scene section.");
                current = new(line[1..].Split(' ', ']')[0], Attributes(line), new(StringComparer.Ordinal));
                result.Add(current);
                continue;
            }
            if (current is null) throw Refuse(BundleErrorCode.MalformedScene, "A scene line comes before the first section: " + line);
            var separator = line.IndexOf(" = ", StringComparison.Ordinal);
            if (separator <= 0) throw Refuse(BundleErrorCode.MalformedScene, "A scene line is neither a section header nor a one-line 'key = value': " + line);
            var key = line[..separator];
            if (!current.Values.TryAdd(key, line[(separator + 3)..])) throw Refuse(BundleErrorCode.MalformedScene, "A scene value is stated twice: " + key);
        }
        return result;
    }

    [GeneratedRegex("([a-z_]+)=\"([^\"]*)\"")]
    private static partial Regex Attribute();

    /// <summary>A refusal carrying the bundle error the generator reports for it.</summary>
    private static SceneGeometryRefusalException Refuse(BundleErrorCode code, string message) => new(code, message);

    /// <summary>Every sub-resource by its id. Each states an id and a type, and no id is used twice.</summary>
    private static Dictionary<string, Section> Resources(List<Section> sections)
    {
        var resources = new Dictionary<string, Section>(StringComparer.Ordinal);
        foreach (var section in sections.Where(s => s.Kind == "sub_resource"))
        {
            if (!section.Attributes.TryGetValue("id", out var id) || !section.Attributes.ContainsKey("type"))
                throw Refuse(BundleErrorCode.MalformedScene, "A scene sub-resource has no id or type.");
            if (!resources.TryAdd(id, section)) throw Refuse(BundleErrorCode.DuplicateId, "A scene sub-resource id is used twice: " + id);
        }
        return resources;
    }

    /// <summary>Every node by its path. No path is used twice.</summary>
    private static Dictionary<string, Node> NodesByPath(Node[] nodes)
    {
        var byPath = new Dictionary<string, Node>(StringComparer.Ordinal);
        foreach (var node in nodes)
            if (!byPath.TryAdd(node.Path, node)) throw Refuse(BundleErrorCode.DuplicateId, "A scene node path is used twice: " + node.Path);
        return byPath;
    }

    /// <summary>A section header's attributes. None is stated twice.</summary>
    private static Dictionary<string, string> Attributes(string line)
    {
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match match in Attribute().Matches(line))
            if (!attributes.TryAdd(match.Groups[1].Value, match.Groups[2].Value))
                throw Refuse(BundleErrorCode.MalformedScene, "A scene section states an attribute twice: " + match.Groups[1].Value);
        return attributes;
    }
}

/// <summary>A scene the extractor refuses. <see cref="Code"/> is the bundle error the generator reports for it; the message states the
/// refusal and names the node where the check has it. It is the extractor's only refusal, so the generator catches nothing broader.</summary>
public sealed class SceneGeometryRefusalException(BundleErrorCode code, string message) : Exception(message)
{
    public BundleErrorCode Code { get; } = code;
}
