using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

public sealed class HingeFixtureOverlapException(HingedBody hinge) :
    InvalidOperationException($"Hinged beam {hinge.Owner.Uid} overlaps a solid fixture.");

public partial class MachineWorld
{
    private readonly record struct HingeObstacle(SweepSurfaceKind Shape, Transform3D Pose, Vector3 Half, float Radius, TubeProxy Tube)
    {
        public static HingeObstacle Box(Transform3D pose, Vector3 half) =>
            new(SweepSurfaceKind.Box, pose, half, 0, default);
        public static HingeObstacle HollowTube(TubeProxy tube) =>
            new(SweepSurfaceKind.Tube, tube.Pose, Vector3.Zero, 0, tube);
        public static HingeObstacle Sphere(Transform3D ownerPose, Vector3 localCenter, float radius)
        {
            // A scaled owner would turn a sphere into an ellipsoid. Never
            // silently keep an unscaled radius after transforming its centre.
            var basis = ownerPose.Basis;
            if (!ownerPose.Origin.IsFinite() || !basis.X.IsFinite() || !basis.Y.IsFinite() || !basis.Z.IsFinite() ||
                Math.Abs(basis.X.LengthSquared() - 1) > .00001f ||
                Math.Abs(basis.Y.LengthSquared() - 1) > .00001f ||
                Math.Abs(basis.Z.LengthSquared() - 1) > .00001f ||
                Math.Abs(basis.X.Dot(basis.Y)) > .00001f ||
                Math.Abs(basis.X.Dot(basis.Z)) > .00001f ||
                Math.Abs(basis.Y.Dot(basis.Z)) > .00001f ||
                Math.Abs(basis.Determinant() - 1) > .0001f)
                throw new InvalidOperationException("Hinge sphere fixtures require proper rigid transforms.");
            return new(SweepSurfaceKind.Sphere, new(Basis.Identity, ownerPose * localCenter), Vector3.Zero, radius, default);
        }
    }
    private readonly record struct HingeObstacleHit(SphereSweepStatus Status, double Time);

    // Exact declared box, sphere and straight hollow tube proxies. Bends,
    // frustums, prescribed moving obstacles and beam/beam remain outstanding.
    private IEnumerable<HingeObstacle> HingeObstacles(HingedBody hinge)
    {
        foreach (var box in new[] { Workbench.Deck, Workbench.Base })
            yield return HingeObstacle.Box(new(Basis.Identity, box.At), box.Half);
        foreach (var part in CollisionParts.Where(p => p.Visible && p.PhysicsOwner.Visible &&
                     p != hinge.Owner && p.PhysicsOwner != hinge.Owner && !p.Dynamic)
                     .OrderBy(p => p.Uid, StringComparer.Ordinal))
        {
            foreach (var box in part.Boxes)
                yield return HingeObstacle.Box(part.Transform * new Transform3D(Basis.Identity, box.At), box.Half);
            foreach (var sphere in part.Spheres)
                yield return HingeObstacle.Sphere(part.Transform, sphere.At, sphere.Radius);
            foreach (var tube in part.Tubes)
                yield return HingeObstacle.HollowTube(tube with { Pose = part.Transform * tube.Pose });
        }
    }

    private static HingeObstacleHit SweepHingeObstacle(HingedBody hinge, HingeObstacle obstacle,
        double speed, double duration)
    {
        switch (obstacle.Shape)
        {
            case SweepSurfaceKind.Box:
                var box = RotatingBoxObstacleSweep.Cast(hinge.Pivot, hinge.Axis, hinge.Pose, hinge.Half,
                    speed, obstacle.Pose, obstacle.Half, duration);
                return new(box.Status, box.Time);
            case SweepSurfaceKind.Sphere:
                var sphere = RotatingBoxSweep.Cast(obstacle.Pose.Origin, obstacle.Radius, Vector3.Zero,
                    hinge.Pivot, hinge.Axis, hinge.Pose, hinge.Half, speed, duration);
                return new(sphere.Status, sphere.Time);
            case SweepSurfaceKind.Tube:
                var tube = RotatingTubeSweep.Cast(hinge.Pivot, hinge.Axis, hinge.Pose, hinge.Half,
                    speed, obstacle.Tube, duration);
                return new(tube.Status, tube.Time);
            default:
                throw new InvalidOperationException("Unsupported hinge obstruction shape.");
        }
    }

    private void PrepareHingeContacts(IEnumerable<HingedBody> hinges)
    {
        foreach (var hinge in hinges)
        {
            hinge.Joint.ClearContactBlock();
            foreach (var obstacle in HingeObstacles(hinge))
            foreach (var direction in new[] { AngularBlock.Negative, AngularBlock.Positive })
            {
                var hit = SweepHingeObstacle(hinge, obstacle, direction == AngularBlock.Negative ? -1 : 1, 0);
                if (hit.Status == SphereSweepStatus.Overlapping)
                    throw new HingeFixtureOverlapException(hinge);
                if (hit.Status == SphereSweepStatus.Contact) hinge.Joint.Block(direction);
            }
        }
    }

    private void ValidateHingePlacement()
    {
        foreach (var hinge in Parts.Where(p => p.Visible).SelectMany(p => p.HingedBodies))
        foreach (var obstacle in HingeObstacles(hinge))
            if (SweepHingeObstacle(hinge, obstacle, 0, 0).Status == SphereSweepStatus.Overlapping)
                throw new HingeFixtureOverlapException(hinge);
    }
}
