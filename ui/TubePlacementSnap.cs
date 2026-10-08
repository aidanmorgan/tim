using Godot;
using System.Linq;

namespace CuriousContraptions;

/// <summary>Editor-only geometric alignment. Never alters runtime assistance or adds transport links.</summary>
public static class TubePlacementSnap
{
    public const float Proximity = .45f;
    public const float AngleLimitDegrees = 20;
    private const float JoinedDistance = .025f;

    public static Transform3D? Find(MachineWorld world, MachinePart moving)
    {
        if (world.HasPhysicsState || !world.Parts.Contains(moving) || moving.Locked || moving is not ITubePart source) return null;
        Transform3D? result = null;
        var best = Proximity * Proximity;
        foreach (var target in world.Parts.OrderBy(p => p.Uid, System.StringComparer.Ordinal))
        {
            if (target == moving || target is not ITubePart destination) continue;
            foreach (var end in destination.Mouths)
            {
                var at = target.GlobalTransform * end.Position;
                var normal = (target.GlobalBasis * end.Outward).Normalized();
                if (Occupied(world, moving, target, at, normal, end.BoreRadius)) continue;
                foreach (var start in source.Mouths)
                {
                    if (!Mathf.IsEqualApprox(start.BoreRadius, end.BoreRadius)) continue;
                    var from = moving.GlobalTransform * start.Position;
                    var direction = (moving.GlobalBasis * start.Outward).Normalized();
                    var distance = from.DistanceSquaredTo(at);
                    if (distance > best || direction.Dot(-normal) < Mathf.Cos(Mathf.DegToRad(AngleLimitDegrees))) continue;
                    var rotation = new Basis(new Quaternion(direction, -normal));
                    var basis = rotation * moving.GlobalBasis;
                    result = new Transform3D(basis, at - basis * start.Position);
                    best = distance;
                }
            }
        }
        return result;
    }

    private static bool Occupied(MachineWorld world, MachinePart moving, MachinePart target,
        Vector3 at, Vector3 normal, float radius)
    {
        foreach (var other in world.Parts)
        {
            if (other == moving || other == target || other is not ITubePart tube) continue;
            foreach (var mouth in tube.Mouths)
                if (Mathf.IsEqualApprox(radius, mouth.BoreRadius) &&
                    (other.GlobalTransform * mouth.Position).DistanceTo(at) < JoinedDistance &&
                    (other.GlobalBasis * mouth.Outward).Normalized().Dot(normal) < -.999f)
                    return true;
        }
        return false;
    }
}
