using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

// Dashed projection boxes only: no filled surfaces, shadow maps, or physics objects.
public partial class PlacementShadows : Node3D
{
    private sealed class Projection
    {
        public Node3D Root = null!;
        public ImmediateMesh Mesh = null!;
        public Aabb Bounds;
        public bool Highlighted;
        public bool Initialized;
    }
    private readonly Dictionary<MachinePart, Projection> _projections = new();
    private static readonly Color[] Colors = [new("#a5823b"), new("#527c98"), new("#a96f5c")];
    public static Vector3 Project(Vector3 point, int axis) => axis switch
    {
        0 => new(point.X, -.42f, point.Z),
        1 => new(point.X, point.Y, -4.65f),
        _ => new(-7.65f, point.Y, point.Z)
    };

    public override void _Ready()
    {
        Name = "PlacementShadows";
        Visible = false;
        // Reference walls intentionally have no geometry: completely transparent.
    }

    public static Aabb ArtworkBounds(MachinePart part)
    {
        ArgumentNullException.ThrowIfNull(part);
        if (!GodotObject.IsInstanceValid(part) || !part.IsInsideTree() ||
            part.Visual is null || !GodotObject.IsInstanceValid(part.Visual))
            throw new ArgumentException("Artwork bounds require a live constructed part in the scene tree.", nameof(part));
        Aabb? bounds = null;
        IncludeArtwork(part.Visual, ref bounds);
        return bounds ?? new Aabb(part.GlobalPosition - Vector3.One * .1f, Vector3.One * .2f);
    }

    private static void IncludeArtwork(Node node, ref Aabb? bounds)
    {
        // Query the native render identity, not a managed Mesh wrapper whose lifetime
        // is unrelated to this read. The instance owns its native mesh and local AABB.
        if (node is MeshInstance3D source && source.IsVisibleInTree() && source.GetBase().IsValid)
        {
            var box = source.GlobalTransform * source.GetAabb();
            bounds = bounds is { } previous ? previous.Merge(box) : box;
        }
        // Typed traversal avoids scene-name/type selectors and per-query child arrays.
        var count = node.GetChildCount();
        for (var i = 0; i < count; i++) IncludeArtwork(node.GetChild(i), ref bounds);
    }

    public void Follow(IEnumerable<MachinePart> parts, MachinePart? preview, MachinePart? selected, bool enabled)
    {
        Visible = enabled;
        var current = parts.Where(GodotObject.IsInstanceValid).ToHashSet();
        if (GodotObject.IsInstanceValid(preview)) current.Add(preview!);
        foreach (var removed in _projections.Keys.Where(part => !current.Contains(part)).ToArray())
        {
            var projection = _projections[removed];
            RemoveChild(projection.Root);
            projection.Root.Free();
            _projections.Remove(removed);
        }
        if (!enabled) return;
        foreach (var part in current)
        {
            if (!_projections.TryGetValue(part, out var projection))
            {
                var mesh = new ImmediateMesh();
                var root = new MeshInstance3D
                {
                    Name = "Projection_" + part.Uid, Mesh = mesh,
                    CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
                };
                root.SetMeta("part_id", part.Uid);
                AddChild(root);
                projection = new Projection { Root = root, Mesh = mesh };
                _projections.Add(part, projection);
            }
            projection.Root.Visible = part.Visible;
            if (!part.Visible) continue;
            var bounds = ArtworkBounds(part);
            var highlighted = part == selected || part == preview;
            if (projection.Initialized && projection.Bounds == bounds && projection.Highlighted == highlighted) continue;
            projection.Bounds = bounds;
            projection.Highlighted = highlighted;
            projection.Initialized = true;
            projection.Mesh.ClearSurfaces();
            for (var axis = 0; axis < 3; axis++)
            {
                var material = new StandardMaterial3D
                {
                    AlbedoColor = highlighted ? Colors[axis].Darkened(.2f) : Colors[axis],
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                    CullMode = BaseMaterial3D.CullModeEnum.Disabled
                };
                projection.Mesh.SurfaceBegin(Mesh.PrimitiveType.Lines, material);
                var min = bounds.Position;
                var max = bounds.End;
                var a = Project(min, axis);
                var c = Project(max, axis);
                var b = axis == 2 ? new Vector3(a.X, c.Y, a.Z) : new Vector3(c.X, a.Y, a.Z);
                var d = axis == 2 ? new Vector3(a.X, a.Y, c.Z) : new Vector3(a.X, c.Y, c.Z);
                Dash(projection.Mesh, a, b);
                Dash(projection.Mesh, b, c);
                Dash(projection.Mesh, c, d);
                Dash(projection.Mesh, d, a);
                projection.Mesh.SurfaceEnd();
            }
        }
    }

    private static void Dash(ImmediateMesh mesh, Vector3 from, Vector3 to)
    {
        var length = from.DistanceTo(to);
        if (length < .0001f) return;
        // World-space spacing stays consistent as parts move, rotate, or grow.
        var count = Mathf.Max(1, Mathf.CeilToInt(length / .22f));
        for (var i = 0; i < count; i++)
        {
            mesh.SurfaceAddVertex(from.Lerp(to, (float)i / count));
            mesh.SurfaceAddVertex(from.Lerp(to, (i + .6f) / count));
        }
    }
}
