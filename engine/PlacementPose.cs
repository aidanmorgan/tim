using System;
using System.Collections.Generic;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;
using Godot;
using ColliderDescriptor = CuriousContraptions.Abstractions.ColliderDescriptor;
using ColliderShapeKind = CuriousContraptions.Abstractions.ColliderShapeKind;

namespace CuriousContraptions;

/// <summary>Why a composed placement pose is refused, by placement path: the part at its grid pose would reach outside the bench
/// volume (<see cref="OutsideBench"/>); joined at a tube mouth it would (<see cref="SnapOutsideBench"/>); rested on a top it would be
/// lifted outside it (<see cref="RestOutsideBench"/>), or into a Static part (<see cref="IntoPart"/>, for example a part lifted onto
/// a lower top into a higher box).</summary>
public enum PlacementRefusal : byte { None, OutsideBench, IntoPart, RestOutsideBench, SnapOutsideBench }

/// <summary>A composed placement: the pose, the tube-mouth snap that produced it (if any) and whether the pose is refused.</summary>
public readonly record struct ComposedPlacement(Vector3 Position, Quaternion Rotation, TubeMouthSnapResult? Snap, PlacementRefusal Refusal);

/// <summary>The pose a new part takes at a bench point (F.6 F-1), one pure main-thread composition shared by the preview, the click
/// and the native tests: the 0.1 m grid, then a typed tube-mouth snap when one exists (Story 9R.6), otherwise rest. Rest lifts a
/// Dynamic part that truly overlaps a Static part's collider whose top is flat and horizontal (a box face or an upright cylinder's
/// cap), lies within the part's vertical extent and lies
/// under the part's x–z centre, so its lowest point rests on the highest such top; x, z and the rotation are unchanged. A lifted pose
/// is admitted only when it is clear of every Static part (beyond a 10⁻⁴ m contact slop; coordinator decision B1, 23 Oct 2026).
/// Every final pose (snap, grid or rest) is admitted only when the part's world-space extent lies in the bench volume (x ∈ [−7, 7],
/// y from the bench surface to 9, z ∈ [−4, 4] m; coordinator decisions on 10.2a F3, 24 Oct 2026: the body, not its origin, on every
/// placement path); otherwise it is refused. Geometry is the declared f32 colliders of the
/// bundle (no part identity); it creates no command.</summary>
public static class PlacementPose
{
    /// <summary>The placement grid (m).</summary>
    public const float Grid = .1f;
    /// <summary>A box's top is horizontal when one of its axes is vertical to within this cosine.</summary>
    public const float VerticalAxis = 1f - 1e-6f;
    /// <summary>A lifted part touching the top it rests on is not inside it: only a deeper overlap refuses the pose (m).</summary>
    public const float ContactSlop = 1e-4f;

    /// <summary>The volume a placement point must lie in: x ∈ [−7, 7], y ∈ [0, 9], z ∈ [−4, 4] m.</summary>
    public static bool Inside(Vector3 point) =>
        float.IsFinite(point.X) && float.IsFinite(point.Y) && float.IsFinite(point.Z) &&
        point.Z >= -4 && point.Z <= 4 && point.X >= -7 && point.X <= 7 && point.Y >= 0 && point.Y <= 9;

    /// <summary>Whether every posed collider's world-space extent lies in the bench volume: x ∈ [−7, 7] and z ∈ [−4, 4] m, and from the
    /// bench surface (less the contact slop a resting body may touch it by) up to 9 m.</summary>
    private static bool Within(Posed[] shapes)
    {
        var bench = WorkshopConstruction.WorkbenchSurface.Value - ContactSlop;
        foreach (var shape in shapes)
        {
            float x = shape.Extent(Vector3.Right), y = shape.Height, z = shape.Extent(Vector3.Back);
            var c = shape.Centre;
            if (!float.IsFinite(c.X + c.Y + c.Z + x + y + z) || c.X - x < -7 || c.X + x > 7 || c.Z - z < -4 || c.Z + z > 4 ||
                c.Y - y < bench || c.Y + y > 9)
                return false;
        }
        return true;
    }

    private enum Shape : byte { Sphere, Box, Cylinder }
    /// <summary>One posed collider. A cylinder's axis is its local Y; its half-extents are (r, h, r).</summary>
    private readonly record struct Posed(Shape Kind, Vector3 Centre, Vector3 X, Vector3 Y, Vector3 Z, Vector3 HalfExtent, float Radius)
    {
        public Vector3 Axis(int i) => i switch { 0 => X, 1 => Y, _ => Z };
        public float HalfAlong(int i) => i switch { 0 => HalfExtent.X, 1 => HalfExtent.Y, _ => HalfExtent.Z };
        /// <summary>The shape's world vertical half-extent: its centre to its top.</summary>
        public float Height => Extent(Vector3.Up);
        /// <summary>The shape's half-extent along a unit direction: a box's projected half-extents, a cylinder's h·|a·n| + r·√(1 − (a·n)²).</summary>
        public float Extent(Vector3 direction) => Kind switch
        {
            Shape.Sphere => Radius,
            Shape.Box => MathF.Abs(X.Dot(direction)) * HalfExtent.X + MathF.Abs(Y.Dot(direction)) * HalfExtent.Y + MathF.Abs(Z.Dot(direction)) * HalfExtent.Z,
            Shape.Cylinder => MathF.Abs(Y.Dot(direction)) * HalfExtent.Y + HalfExtent.X * MathF.Sqrt(MathF.Max(0f, 1f - Y.Dot(direction) * Y.Dot(direction))),
            _ => throw new InvalidOperationException($"Undefined placement shape {Kind}.")
        };
    }

    public static ComposedPlacement Compose(IReadOnlyDictionary<PartId, PartDefinition> definitions,
        IReadOnlyList<IWorkshopInstance> placed, TubeMouthMover candidate, Vector3 at, Quaternion rotation)
    {
        ArgumentNullException.ThrowIfNull(placed);
        var grid = at.Snapped(Vector3.One * Grid);
        if (TubeMouthSnap.Find(definitions, placed, candidate, grid, rotation) is { } joined)
            return new(joined.Position, joined.Rotation, joined, Bounded(candidate.Part, joined.Position, joined.Rotation, PlacementRefusal.SnapOutsideBench));
        var (position, refusal) = Rest(placed, candidate, grid, rotation.Normalized());
        return new(position, rotation, null, refusal != PlacementRefusal.None ? refusal
            : Bounded(candidate.Part, position, rotation.Normalized(), PlacementRefusal.OutsideBench));
    }

    /// <summary>The one bench-volume rule of every placement path (snap, grid and rest): the part's declared colliders at its default
    /// parameters, posed at the final pose, must lie in the bench volume (<see cref="Within"/>), or the pose is refused with the path's
    /// <paramref name="outside"/> reason. An undeclared part, or one that declares no collider, has no geometry to bound; capturing an
    /// undeclared part is the placement's own typed refusal.</summary>
    private static PlacementRefusal Bounded(PartId part, Vector3 position, Quaternion rotation, PlacementRefusal outside) =>
        !DeclaredPhysics.Contains(part) || DeclaredPhysics.Of(part).Colliders.Count == 0 ||
        Within(Pose(BundleGeometry.Colliders(DeclaredPhysics.Of(part)), position, rotation))
            ? PlacementRefusal.None : outside;

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
        foreach (var support in statics)
        {
            if (Vertical(support) is not { } up) continue;
            var surface = support.Centre.Y + support.Height;
            if (surface < bottom || surface > top || !Over(support, up, at) || !Array.Exists(shapes, shape => Overlaps(shape, support, 0f))) continue;
            if (rest is not { } highest || surface > highest) rest = surface;
        }
        if (rest is not { } y) return (at, PlacementRefusal.None);
        // The lifted pose must stay in the bench volume and clear every Static part (B1).
        var lifted = at with { Y = at.Y + (y - bottom) };
        var raised = Pose(BundleGeometry.Colliders(entry), lifted, rotation);
        if (!Within(raised)) return (lifted, PlacementRefusal.RestOutsideBench);
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
                ColliderShapeKind.Cylinder => Shape.Cylinder,
                _ => throw new ArgumentException($"Placement rest has no rule for a {c.ShapeKind} collider.")
            };
            var local = (turn * new Quaternion(c.Rotation.X, c.Rotation.Y, c.Rotation.Z, c.Rotation.W)).Normalized();
            result[i] = new(kind, position + turn * new Vector3(c.Offset.X, c.Offset.Y, c.Offset.Z), local * Vector3.Right, local * Vector3.Up,
                local * Vector3.Back, new(c.HalfExtents.X, c.HalfExtents.Y, c.HalfExtents.Z), kind == Shape.Sphere ? c.HalfExtents.X : 0f);
        }
        return result;
    }

    /// <summary>The point of the box nearest a point, both relative to the box's centre.</summary>
    private static Vector3 ClosestOnBox(Posed box, Vector3 offset)
    {
        var closest = Vector3.Zero;
        for (var i = 0; i < 3; i++)
            closest += box.Axis(i) * Math.Clamp(offset.Dot(box.Axis(i)), -box.HalfAlong(i), box.HalfAlong(i));
        return closest;
    }

    /// <summary>The point of the solid cylinder (axis its local Y, half-height h, radius r) nearest a point, both relative to its centre:
    /// the axial coordinate clamped to [−h, h] and the radial part clamped to r.</summary>
    private static Vector3 ClosestOnCylinder(Posed cylinder, Vector3 offset)
    {
        var along = offset.Dot(cylinder.Y);
        var radial = offset - cylinder.Y * along;
        var distance = radial.Length();
        if (distance > cylinder.HalfExtent.X) radial *= cylinder.HalfExtent.X / distance;
        return cylinder.Y * Math.Clamp(along, -cylinder.HalfExtent.Y, cylinder.HalfExtent.Y) + radial;
    }

    /// <summary>The vertical axis of a shape whose top is flat and horizontal: any vertical axis of a box, or an upright cylinder's own
    /// axis (its cap). A sphere, and a cylinder on its side, have no flat top.</summary>
    private static int? Vertical(Posed shape)
    {
        if (shape.Kind == Shape.Cylinder) return MathF.Abs(shape.Y.Y) >= VerticalAxis ? 1 : null;
        if (shape.Kind != Shape.Box) return null;
        for (var i = 0; i < 3; i++) if (MathF.Abs(shape.Axis(i).Y) >= VerticalAxis) return i;
        return null;
    }

    /// <summary>Whether the x–z point lies over the top's footprint, edges included: a box's two horizontal axes, or an upright
    /// cylinder's cap disc.</summary>
    private static bool Over(Posed shape, int up, Vector3 point)
    {
        var offset = point - shape.Centre;
        if (shape.Kind == Shape.Cylinder) return offset.X * offset.X + offset.Z * offset.Z <= shape.HalfExtent.X * shape.HalfExtent.X;
        for (var i = 0; i < 3; i++)
            if (i != up && MathF.Abs(offset.Dot(shape.Axis(i))) > shape.HalfAlong(i)) return false;
        return true;
    }

    /// <summary>A true overlap: the shapes interpenetrate by more than <paramref name="slop"/>; touching is not overlapping. A sphere
    /// meets a box or a cylinder at its exact closest point; a cylinder and a box are separated on both frames' axes and their cross
    /// products by the cylinder's exact projection.</summary>
    private static bool Overlaps(Posed shape, Posed box, float slop)
    {
        if (box.Kind == Shape.Sphere)
        {
            if (shape.Kind != Shape.Sphere) return Overlaps(box, shape, slop);
            var reach = shape.Radius + box.Radius - slop;
            return (shape.Centre - box.Centre).LengthSquared() < reach * reach;
        }
        var offset = shape.Centre - box.Centre;
        if (shape.Kind == Shape.Sphere)
        {
            var closest = box.Kind == Shape.Cylinder ? ClosestOnCylinder(box, offset) : ClosestOnBox(box, offset);
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
            if (MathF.Abs(offset.Dot(axis)) >= shape.Extent(axis) + box.Extent(axis) - slop) return false;
        }
        return true;
    }
}
