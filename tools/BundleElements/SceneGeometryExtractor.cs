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

    public static BundleEntry Bake(string text, BundleEntry entry, IReadOnlyList<SceneBodyGeometry>? ownership)
    {
        var sections = Parse(text);
        var resources = sections.Where(s => s.Kind == "sub_resource").ToDictionary(s => s.Attributes["id"]);
        var nodes = sections.Where(s => s.Kind == "node").Select(s =>
        {
            var parent = s.Attributes.GetValueOrDefault("parent");
            var name = s.Attributes["name"];
            return new Node(name, s.Attributes.GetValueOrDefault("type") ?? "",
                parent is null ? "." : parent == "." ? name : parent + "/" + name, parent, s.Values);
        }).ToArray();
        if (nodes.Length == 0 || nodes[0].Parent is not null || nodes[0].Type != "Node3D")
            throw new ArgumentException("Scene root must be Node3D.");
        var byPath = nodes.ToDictionary(n => n.Path, StringComparer.Ordinal);
        foreach (var node in nodes.Skip(1))
            if (node.Parent is null || !byPath.ContainsKey(node.Parent)) throw new ArgumentException("Scene has an unresolved parent.");
        var proxies = nodes.Where(n => n.Type == "CollisionShape3D" ||
            n.Name.StartsWith("col_", StringComparison.OrdinalIgnoreCase) ||
            n.Name.Contains("-col_", StringComparison.OrdinalIgnoreCase)).ToArray();
        var sliders = entry.Mechanical?.Sliders ?? [];
        var hasData = entry.Colliders.Count != 0 || sliders.Any(s => s.MovingBody.Colliders.Count != 0);
        if (proxies.Length != 0 && hasData) throw new ArgumentException("Scene and data both declare colliders.");
        if (proxies.Length != 0 && ownership is null) throw new ArgumentException("Scene collider bodies have no ownership declaration.");
        if (ownership is null)
        {
            if (entry.Footprint.Bodies > 0 && entry.Colliders.Count == 0) throw new ArgumentException("Body has no collider.");
            return entry with { TubeMouths = Mouths() };
        }
        if (hasData) throw new ArgumentException("A scene-owned package also declares data colliders.");
        if (ownership.Count != 1 + sliders.Count || ownership[0].Root != ".")
            throw new ArgumentException("Scene ownership must name the base and every slider body.");
        for (var i = 0; i < ownership.Count; i++)
        {
            if (!byPath.ContainsKey(ownership[i].Root)) throw new ArgumentException("Body root does not exist: " + ownership[i].Root);
            for (var j = 0; j < i; j++)
                if (ownership[i].Root == ownership[j].Root ||
                    (j != 0 && (Below(ownership[i].Root, ownership[j].Root) || Below(ownership[j].Root, ownership[i].Root))))
                    throw new ArgumentException("Scene body roots overlap.");
        }
        var bodies = Enumerable.Range(0, ownership.Count).Select(_ => new List<ColliderDescriptor>()).ToArray();
        foreach (var node in proxies)
        {
            var owner = 0;
            for (var i = 1; i < ownership.Count; i++)
                if (Below(node.Path, ownership[i].Root) || node.Path == ownership[i].Root) owner = i;
            if (node.Type == "Node3D" && proxies.Any(child => Below(child.Path, node.Path))) continue;
            if (node.Type is not ("CollisionShape3D" or "MeshInstance3D") ||
                node.Name.StartsWith("col_capsule", StringComparison.OrdinalIgnoreCase) ||
                node.Name.StartsWith("col_hull", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Unsupported collider proxy: " + node.Path);
            if (!node.Values.TryGetValue(node.Type == "CollisionShape3D" ? "shape" : "mesh", out var reference)) throw new ArgumentException("Collider has no shape: " + node.Path);
            var resourceId = Reference(reference);
            if (!resources.TryGetValue(resourceId, out var shape)) throw new ArgumentException("Collider shape is missing: " + node.Path);
            var (position, rotation, scale) = Pose(node, ownership[owner].Root);
            if (scale.X <= 0 || scale.Y <= 0 || scale.Z <= 0) throw new ArgumentException("Collider scale must be positive.");
            // engine-hollow-shapes: an open cylinder mesh (both caps off) is a tube or frustum wall, a torus mesh a torus segment.
            if (Hollow(shape, node, scale) is { } hollow)
            {
                bodies[owner].Add(hollow with { Offset = position, Rotation = rotation, Material = ownership[owner].Material });
                continue;
            }
            var kind = shape.Attributes["type"] switch
            {
                "BoxShape3D" or "BoxMesh" => ColliderShapeKind.Box,
                "SphereShape3D" or "SphereMesh" => ColliderShapeKind.Sphere,
                "CylinderShape3D" or "CylinderMesh" => ColliderShapeKind.Cylinder,
                _ => throw new ArgumentException("Unsupported collider shape: " + shape.Attributes["type"])
            };
            var half = kind switch
            {
                ColliderShapeKind.Box => Vector(Required(shape, "size", node)) * .5f,
                ColliderShapeKind.Sphere => new Vector3(Number(Required(shape, "radius", node))),
                ColliderShapeKind.Cylinder => CylinderHalfExtents(shape, node),
                _ => throw new ArgumentException("Unsupported collider shape: " + node.Path)
            };
            half *= scale;
            if (half.X <= 0 || half.Y <= 0 || half.Z <= 0) throw new ArgumentException("Collider has nonpositive extent.");
            if (kind == ColliderShapeKind.Sphere && (half.X != half.Y || half.X != half.Z))
                throw new ArgumentException("A sphere cannot have nonuniform scale.");
            // A cylinder's axis is its local +Y: its height scales by scale.Y and its radius needs equal radial scale (X = Z).
            if (kind == ColliderShapeKind.Cylinder && half.X != half.Z)
                throw new ArgumentException("A cylinder cannot have nonuniform radial scale: " + node.Path);
            bodies[owner].Add(new(kind, half, position, rotation, ownership[owner].Material, 0f));
        }
        if (bodies.Any(body => body.Count == 0)) throw new ArgumentException("A scene body has no collider.");
        var bakedSliders = sliders.Select((slider, index) => slider with
        { MovingBody = slider.MovingBody with { Colliders = bodies[index + 1] } }).ToArray();
        return entry with
        {
            Colliders = bodies[0],
            Mechanical = entry.Mechanical is { } mechanical ? mechanical with { Sliders = bakedSliders } : null,
            TubeMouths = Mouths()
        };

        IReadOnlyList<TubeMouthDescriptor> Mouths()
        {
            var mouths = new List<TubeMouthDescriptor>();
            foreach (var node in nodes.Where(n => n.Name.StartsWith("Loc_Port_mouth_", StringComparison.Ordinal)))
            {
                var end = node.Name switch
                {
                    "Loc_Port_mouth_start" => TubeEnd.Start,
                    "Loc_Port_mouth_end" => TubeEnd.End,
                    _ => throw new ArgumentException("Unknown tube mouth.")
                };
                if (mouths.Any(m => m.End == end)) throw new ArgumentException("Duplicate tube mouth.");
                var (position, rotation, _) = Pose(node, ".");
                var bore = Number(node.Values["metadata/bore_radius"]);
                if (bore <= 0) throw new ArgumentException("Invalid tube bore.");
                mouths.Add(new(end, position, rotation, bore));
            }
            return mouths;
        }

        (Vector3 Position, Quaternion Rotation, Vector3 Scale) Pose(Node node, string root)
        {
            if (node.Path == root) { _ = Local(node.Values); return (Vector3.Zero, Quaternion.Identity, Vector3.One); }
            var local = Local(node.Values);
            if (node.Parent == root) return local;
            var parent = Pose(byPath[node.Parent!], root);
            var matrix = Matrix4x4.CreateScale(local.Scale) * Matrix4x4.CreateFromQuaternion(local.Rotation) *
                Matrix4x4.CreateTranslation(local.Position) * Matrix4x4.CreateScale(parent.Scale) *
                Matrix4x4.CreateFromQuaternion(parent.Rotation) * Matrix4x4.CreateTranslation(parent.Position);
            if (!Matrix4x4.Decompose(matrix, out var scale, out var rotation, out var position))
                throw new ArgumentException("Cannot decompose scene transform.");
            return (position, rotation, scale);
        }
    }

    private static bool Below(string path, string parent) => path.StartsWith(parent + "/", StringComparison.Ordinal);

    /// <summary>A shape value the proxy must state explicitly (Godot omits default-valued keys, so a missing key is never assumed).</summary>
    private static string Required(Section shape, string key, Node node) =>
        shape.Values.TryGetValue(key, out var value) ? value
            : throw new ArgumentException($"Collider shape has no explicit '{key}': {node.Path}");

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
                _ => throw new ArgumentException("A cylinder cap must be true or false: " + node.Path)
            };
            open = !Cap("cap_top") | !Cap("cap_bottom");
        }
        if (type != "TorusMesh" && !open) return null;
        if (scale.X != scale.Y || scale.X != scale.Z) throw new ArgumentException("A hollow collider needs uniform scale: " + node.Path);
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
            throw new ArgumentException("A hollow cylinder proxy opens both caps: " + node.Path);
        if (wallText is null) throw new ArgumentException("An open cylinder proxy declares metadata/wall_thickness: " + node.Path);
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
                throw new ArgumentException("A tapered cylinder mesh is not a cylinder collider: " + node.Path);
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
                throw new ArgumentException("Cannot decompose scene transform.");
            if (scale.X <= 0 || scale.Y <= 0 || scale.Z <= 0) throw new ArgumentException("Scene scale must be positive.");
            return (position, Quaternion.Normalize(rotation), scale);
        }
        var p = values.TryGetValue("position", out var pos) ? Vector(pos) : Vector3.Zero;
        var s = values.TryGetValue("scale", out var size) ? Vector(size) : Vector3.One;
        if (s.X <= 0 || s.Y <= 0 || s.Z <= 0) throw new ArgumentException("Scene scale must be positive.");
        var q = Quaternion.Identity;
        if (values.TryGetValue("quaternion", out var quaternion))
        {
            var f = Components(quaternion, 4);
            q = new(f[0], f[1], f[2], f[3]);
            if (MathF.Abs(q.LengthSquared() - 1f) > 1e-5f) throw new ArgumentException("Invalid scene quaternion.");
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
        if (begin < 0 || !value.EndsWith(')')) throw new ArgumentException("Malformed scene numeric value.");
        var result = value[(begin + 1)..^1].Split(',').Select(Number).ToArray();
        if (result.Length != count) throw new ArgumentException("Wrong scene numeric arity.");
        return result;
    }
    private static float Number(string value)
    {
        if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) || !float.IsFinite(result))
            throw new ArgumentException("Nonfinite or invalid scene number.");
        return result;
    }
    private static string Reference(string value)
    {
        if (!value.StartsWith("SubResource(\"", StringComparison.Ordinal) || !value.EndsWith("\")", StringComparison.Ordinal))
            throw new ArgumentException("Collider shape must be a local subresource.");
        return value[13..^2];
    }
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
                if (!line.EndsWith(']')) throw new ArgumentException("Malformed scene section.");
                var kind = line[1..].Split(' ', ']')[0];
                var attributes = Attribute().Matches(line).ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);
                current = new(kind, attributes, new(StringComparer.Ordinal));
                result.Add(current);
            }
            else if (current is not null)
            {
                var separator = line.IndexOf(" = ", StringComparison.Ordinal);
                if (separator > 0) current.Values.Add(line[..separator], line[(separator + 3)..]);
            }
        }
        return result;
    }

    [GeneratedRegex("([a-z_]+)=\"([^\"]*)\"")]
    private static partial Regex Attribute();
}
