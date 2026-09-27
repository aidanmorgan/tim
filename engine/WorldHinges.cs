using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

public sealed class HingeFixtureOverlapException(string ownerId) :
    InvalidOperationException($"Hinged beam {ownerId} overlaps a solid fixture.");

public partial class MachineWorld
{
    private readonly record struct HingeBox(Transform3D Pose, Vector3 Half);

    // The first obstruction integration covers declared fixed box proxies.
    // Curved solids and beam/beam contacts remain explicit outstanding work.
    private IEnumerable<HingeBox> HingeObstacles(HingedBody hinge)
    {
        foreach (var box in new[] { Workbench.Deck, Workbench.Base })
            yield return new(new(Basis.Identity, box.At), box.Half);
        foreach (var part in CollisionParts.Where(p => p.Visible && p.PhysicsOwner.Visible &&
                     p != hinge.Owner && p.PhysicsOwner != hinge.Owner && !p.Dynamic)
                     .OrderBy(p => p.Uid, StringComparer.Ordinal))
            foreach (var box in part.Boxes)
                yield return new(part.Transform * new Transform3D(Basis.Identity, box.At), box.Half);
    }

    private static RotatingObstacleHit SweepHingeBox(HingedBody hinge, HingeBox box,
        double speed, double duration) =>
        RotatingBoxObstacleSweep.Cast(hinge.Pivot, hinge.Axis, hinge.Pose, hinge.Half,
            speed, box.Pose, box.Half, duration);

    private void PrepareHingeContacts(IEnumerable<HingedBody> hinges)
    {
        foreach (var hinge in hinges)
        {
            hinge.Joint.ClearContactBlock();
            foreach (var box in HingeObstacles(hinge))
            foreach (var direction in new[] { AngularBlock.Negative, AngularBlock.Positive })
            {
                var hit = SweepHingeBox(hinge, box, direction == AngularBlock.Negative ? -1 : 1, 0);
                if (hit.Status == SphereSweepStatus.Overlapping)
                    throw new HingeFixtureOverlapException(hinge.Owner.Uid);
                if (hit.Status == SphereSweepStatus.Contact) hinge.Joint.Block(direction);
            }
        }
    }

    private void ValidateHingePlacement()
    {
        foreach (var hinge in Parts.Where(p => p.Visible).SelectMany(p => p.HingedBodies))
        foreach (var box in HingeObstacles(hinge))
            if (SweepHingeBox(hinge, box, 0, 0).Status == SphereSweepStatus.Overlapping)
                throw new HingeFixtureOverlapException(hinge.Owner.Uid);
    }
}
