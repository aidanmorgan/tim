using System;
using System.Linq;
using System.Numerics;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Gpu;

public static partial class WorkshopPhysicsCompiler
{
    private sealed partial class Compilation
    {
        private void ValidatePreparedGasPlacement()
        {
            if (gasShapes.Count == 0) return;
            var workspace = core.Geometry.CreateMaterialQueryWorkspace();
            var bodyMap = bodies.ToDictionary(body => body.Id);
            foreach (var binding in gasShapes)
            {
                var collider = colliders.Single(shape => shape.Id == binding.Collider);
                if (collider.Participation != CollisionParticipation.Enabled) continue;
                var moving = Partner(collider);
                moving = moving with { Radius = moving.Radius - core.Geometry.PlacementSlop };
                foreach (var obstacle in colliders)
                {
                    if (obstacle.Participation != CollisionParticipation.Enabled || obstacle.Body == collider.Body ||
                        identities.IsChild(obstacle.Body, collider.Body) || identities.IsChild(collider.Body, obstacle.Body)) continue;
                    if (workspace.SweepMaterial(moving, Partner(obstacle), Vector3.Zero) != MaterialQueryResult.Clear)
                        throw new WorkshopEditRejectedException(WorkshopEditRejection.InitialOverlap,
                            "The prepared envelope needs more space in this atmosphere.");
                }
            }

            HollowPartner Partner(ColliderDeclaration shape)
            {
                var body = bodyMap[shape.Body];
                var rotation = new Quaternion(body.Rotation.X, body.Rotation.Y, body.Rotation.Z, body.Rotation.W);
                var local = shape.Pose;
                var turn = Quaternion.Normalize(rotation * new Quaternion(local.Rotation.X, local.Rotation.Y, local.Rotation.Z, local.Rotation.W));
                var centre = new Vector3((body.Cell.X + body.Local.X) / 16f, (body.Cell.Y + body.Local.Y) / 16f, (body.Cell.Z + body.Local.Z) / 16f) +
                    Vector3.Transform(new Vector3(local.Translation.X, local.Translation.Y, local.Translation.Z), rotation);
                return new(shape.Shape, centre, Vector3.Transform(Vector3.UnitX, turn), Vector3.Transform(Vector3.UnitY, turn),
                    Vector3.Transform(Vector3.UnitZ, turn), shape.Radius.Value, shape.HalfExtents.X, shape.HalfExtents.Y, shape.HalfExtents.Z);
            }
        }
    }
}
