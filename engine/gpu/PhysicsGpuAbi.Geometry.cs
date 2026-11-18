using System;
using System.Numerics;

namespace CuriousContraptions.Gpu;

public static partial class PhysicsGpuAbi
{
    public static BoxGeometryRead ReadBoxGeometry(ReadOnlySpan<byte> world, GpuColliderId identity)
    {
        var layout = Layout(world);
        for (var index = 0; index < layout.Colliders; index++)
        {
            var collider = world.Slice(layout.ColliderOffset(index), ColliderBytes);
            if (R64(collider, RecordIdentityOffset) != identity.Value) continue;
            if ((ColliderShapeKind)R32(collider, ColliderShapeOffset) != ColliderShapeKind.Box)
                throw new BoxQueryException(BoxQueryRefusal.NonBoxCollider);
            var body = world.Slice(layout.BodyOffset((int)R32(collider, ColliderBodyOffset)), BodyBytes);
            var material = world.Slice(layout.MaterialOffset((int)R32(collider, ColliderMaterialOffset)), MaterialBytes);
            var frame = PresentationFrame(body);
            var local = ReadConstraintFrame(collider, ColliderPoseOffset);
            var centre = frame.Position + Vector3.Transform(local.Position, frame.Rotation);
            var rotation = Quaternion.Multiply(frame.Rotation, local.Rotation);
            var half = PresentationVector(collider, ColliderHalfExtentsOffset);
            GpuBodyId? clipping = null;
            var jointIndex = R32(collider, ColliderExtrusionJointOffset);
            if (jointIndex != 0)
            {
                var joint = ReadPrismatic(world, checked((int)jointIndex - 1));
                var jointRow = world.Slice(layout.PrismaticOffset((int)jointIndex - 1), PrismaticBytes);
                var support = PresentationFrame(world.Slice(layout.BodyOffset((int)R32(jointRow, PrismaticBodyAOffset)), BodyBytes));
                rotation = Quaternion.Multiply(support.Rotation, joint.FrameA.Rotation);
                var axis = Vector3.Transform(Vector3.UnitY, rotation);
                var supportOrigin = support.Position + Vector3.Transform(joint.FrameA.Position, support.Rotation);
                var materialOrigin = frame.Position + Vector3.Transform(joint.FrameB.Position, frame.Rotation);
                var coordinate = Vector3.Dot(materialOrigin - supportOrigin, axis);
                var extrusion = new ClippedExtrusionDeclaration(joint.Id, joint.BodyB, joint.BodyA,
                    RF(collider, ColliderExtrusionCutOffset), RF(collider, ColliderExtrusionDistalOffset), new(half.X, half.Z));
                var bounds = extrusion.Bounds(coordinate);
                centre = supportOrigin + Vector3.Transform(bounds.Centre, rotation);
                half = bounds.HalfExtents;
                clipping = joint.BodyA;
            }
            return new(identity, new(R64(material, RecordIdentityOffset)), new(R64(body, RecordIdentityOffset)),
                clipping, centre, rotation, half);
        }
        throw new BoxQueryException(BoxQueryRefusal.UnknownCollider);
    }
}
