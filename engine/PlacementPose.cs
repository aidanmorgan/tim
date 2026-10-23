using System;
using System.Collections.Generic;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;
using Godot;
using ColliderDescriptor = CuriousContraptions.Abstractions.ColliderDescriptor;
using ColliderShapeKind = CuriousContraptions.Abstractions.ColliderShapeKind;

namespace CuriousContraptions;

/// <summary>Why a rested placement pose is refused: it would leave the bench volume, or it would interpenetrate a Static part (for
/// example a part lifted onto a lower top into a higher box).</summary>
public enum PlacementRefusal : byte { None, OutsideBench, IntoPart }

/// <summary>A composed placement: the pose, the tube-mouth snap that produced it (if any) and whether the pose is refused.</summary>
public readonly record struct ComposedPlacement(Vector3 Position, Quaternion Rotation, TubeMouthSnapResult? Snap, PlacementRefusal Refusal);

/// <summary>The pose a new part takes at a bench point (F.6 F-1), one pure main-thread composition shared by the preview, the click
/// and the native tests: the 0.1 m grid, then a typed tube-mouth snap when one exists (Story 9R.6), otherwise rest. Rest lifts a
/// Dynamic part that truly overlaps a Static part's collider whose top is horizontal, lies within the part's vertical extent and lies
/// under the part's x–z centre, so its lowest point rests on the highest such top; x, z and the rotation are unchanged. A lifted pose
/// is admitted only inside the bench volume and clear of every Static part (beyond a 10⁻⁴ m contact slop); otherwise it is refused
/// (coordinator decision B1, 23 Oct 2026). Every other pose is the snap or the grid. Geometry is the declared f32 colliders of the
/// bundle (no part identity); it creates no command.</summary>
public static class PlacementPose
{
    /// <summary>The placement grid (m).</summary>
    public const float Grid = .1f;
    /// <summary>A box's top is horizontal when one of its axes is vertical to within this cosine.</summary>
    public const float VerticalAxis = 1f - 1e-6f;
    /// <summary>A lifted part touching the top it rests on is not inside it: only a deeper overlap refuses the pose (m).</summary>
    public const float ContactSlop = 1e-4f;

    /// <summary>The bench volume a placement point (and a lifted pose) must lie in: x ∈ [−7, 7], y ∈ [0, 9], z ∈ [−4, 4] m.</summary>
    public static bool Inside(Vector3 point) =>
        float.IsFinite(point.X) && float.IsFinite(point.Y) && float.IsFinite(point.Z) &&
        point.Z >= -4 && point.Z <= 4 && point.X >= -7 && point.X <= 7 && point.Y >= 0 && point.Y <= 9;

    private enum Shape : byte { Sphere, Box }
    private readonly record struct Posed(Shape Kind, Vector3 Centre, Vector3 X, Vector3 Y, Vector3 Z, Vector3 Half, float Radius)
    {
        public Vector3 Axis(int i) => i switch { 0 => X, 1 => Y, _ => Z };
        public float HalfAlong(int i) => i switch { 0 => Half.X, 1 => Half.Y, _ => Half.Z };
        /// <summary>Half the shape's world vertical extent.</summary>
        public float Height => Kind == Shape.Sphere ? Radius : MathF.Abs(X.Y) * Half.X + MathF.Abs(Y.Y) * Half.Y + MathF.Abs(Z.Y) * Half.Z;
    }

    public static ComposedPlacement Compose(IReadOnlyDictionary<PartId, PartDefinition> definitions,
        IReadOnlyList<IWorkshopInstance> placed, TubeMouthMover candidate, Vector3 at, Quaternion rotation)
    {
        ArgumentNullException.ThrowIfNull(placed);
        var grid = at.Snapped(Vector3.One * Grid);
        if (TubeMouthSnap.Find(definitions, placed, candidate, grid, rotation) is { } joined)
            return new(joined.Position, joined.Rotation, joined, PlacementRefusal.None);
        var (position, refusal) = Rest(placed, candidate, grid, rotation.Normalized());
        return new(position, rotation, null, refusal);
    }

    private static (Vector3 Position, PlacementRefusal Refusal) Rest(IReadOnlyList<IWorkshopInstance> placed, TubeMouthMover candidate,
        Vector3 at, Quaternion rotation)
    {
        // An undeclared part has no geometry to rest; capturing it is the placement's own typed refusal.
        if (!DeclaredPhysics.Contains(candidate.Part)) return (at, PlacementRefusal.None);
        var entry = DeclaredPhysics.Of(candidate.Part);
        if (entry.Mechanical?.KinematicMode != KinematicMode.Dynamic) return (at, PlacementRefusal.None);
        var shapes = Pose(BundleGeometry.Colliders(entry), at, rotation);
        float bottom = float.PositiveInfinity, top = float.NegativeInfinity;
        foreach (var shape in shapes)
        {
            bottom = MathF.Min(bottom, shape.Centre.Y - shape.Height);
            top = MathF.Max(top, shape.Centre.Y + shape.Height);
        }
        var statics = new List<Posed>();
        foreach (var instance in placed)
        {
            if (instance.Id == candidate.Body) continue;
            var other = DeclaredPhysics.Of(instance.PartId);
            if ((other.Mechanical?.KinematicMode ?? KinematicMode.Static) != KinematicMode.Static) continue;
            var (position, turn) = TubeMouthSnap.Pose(instance);
            statics.AddRange(Pose(BundleGeometry.Colliders(other, DeclaredPhysics.Parameters(instance)), position, turn));
        }
        float? rest = null;
        foreach (var box in statics)
        {
            if (box.Kind != Shape.Box || Vertical(box) is not { } up) continue;
            var surface = box.Centre.Y + box.Height;
            if (surface < bottom || surface > top || !Over(box, up, at) || !Array.Exists(shapes, shape => Overlaps(shape, box, 0f))) continue;
            if (rest is not { } highest || surface > highest) rest = surface;
        }
        if (rest is not { } y) return (at, PlacementRefusal.None);
        // The lifted pose must stay in the bench volume and clear every Static part (B1).
        var lifted = at with { Y = at.Y + (y - bottom) };
        if (!Inside(lifted)) return (lifted, PlacementRefusal.OutsideBench);
        var raised = Pose(BundleGeometry.Colliders(entry), lifted, rotation);
        foreach (var other in statics)
            if (Array.Exists(raised, shape => Overlaps(shape, other, ContactSlop))) return (lifted, PlacementRefusal.IntoPart);
        return (lifted, PlacementRefusal.None);
    }

    private static Posed[] Pose(IReadOnlyList<ColliderDescriptor> colliders, Vector3 position, Quaternion turn)
    {
        var result = new Posed[colliders.Count];
        for (var i = 0; i < colliders.Count; i++)
        {
            var c = colliders[i];
            var kind = c.ShapeKind switch
            {
                ColliderShapeKind.Sphere => Shape.Sphere,
                ColliderShapeKind.Box => Shape.Box,
                _ => throw new ArgumentException($"Placement rest has no rule for a {c.ShapeKind} collider.")
            };
            var local = (turn * new Quaternion(c.Rotation.X, c.Rotation.Y, c.Rotation.Z, c.Rotation.W)).Normalized();
            result[i] = new(kind, position + turn * new Vector3(c.Offset.X, c.Offset.Y, c.Offset.Z), local * Vector3.Right, local * Vector3.Up,
                local * Vector3.Back, new(c.HalfExtents.X, c.HalfExtents.Y, c.HalfExtents.Z), kind == Shape.Sphere ? c.HalfExtents.X : 0f);
        }
        return result;
    }

    /// <summary>The box's vertical axis, when its top is horizontal.</summary>
    private static int? Vertical(Posed box)
    {
        for (var i = 0; i < 3; i++) if (MathF.Abs(box.Axis(i).Y) >= VerticalAxis) return i;
        return null;
    }

    /// <summary>Whether the x–z point lies over the box's footprint (its two horizontal axes, edges included).</summary>
    private static bool Over(Posed box, int up, Vector3 point)
    {
        var offset = point - box.Centre;
        for (var i = 0; i < 3; i++)
            if (i != up && MathF.Abs(offset.Dot(box.Axis(i))) > box.HalfAlong(i)) return false;
        return true;
    }

    /// <summary>A true overlap: the shapes interpenetrate by more than <paramref name="slop"/>; touching is not overlapping.</summary>
    private static bool Overlaps(Posed shape, Posed box, float slop)
    {
        if (box.Kind == Shape.Sphere)
        {
            if (shape.Kind == Shape.Box) return Overlaps(box, shape, slop);
            var reach = shape.Radius + box.Radius - slop;
            return (shape.Centre - box.Centre).LengthSquared() < reach * reach;
        }
        var offset = shape.Centre - box.Centre;
        if (shape.Kind == Shape.Sphere)
        {
            var closest = Vector3.Zero;
            for (var i = 0; i < 3; i++)
                closest += box.Axis(i) * Math.Clamp(offset.Dot(box.Axis(i)), -box.HalfAlong(i), box.HalfAlong(i));
            var radius = shape.Radius - slop;
            return (offset - closest).LengthSquared() < radius * radius;
        }
        // Separating axes: the 3 + 3 face normals and the 9 edge cross products; a gap or contact on any one separates them.
        Span<Vector3> axes = stackalloc Vector3[15];
        var count = 0;
        for (var i = 0; i < 3; i++) { axes[count++] = shape.Axis(i); axes[count++] = box.Axis(i); }
        for (var i = 0; i < 3; i++)
            for (var j = 0; j < 3; j++)
            {
                var cross = shape.Axis(i).Cross(box.Axis(j));
                if (cross.LengthSquared() > 1e-12f) axes[count++] = cross.Normalized();
            }
        for (var k = 0; k < count; k++)
        {
            var axis = axes[k];
            float a = 0, b = 0;
            for (var i = 0; i < 3; i++)
            {
                a += MathF.Abs(shape.Axis(i).Dot(axis)) * shape.HalfAlong(i);
                b += MathF.Abs(box.Axis(i).Dot(axis)) * box.HalfAlong(i);
            }
            if (MathF.Abs(offset.Dot(axis)) >= a + b - slop) return false;
        }
        return true;
    }
}
