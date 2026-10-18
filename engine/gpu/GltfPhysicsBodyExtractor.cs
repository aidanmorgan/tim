using Godot;
using System;
using System.Collections.Generic;

namespace CuriousContraptions.Gpu;


public readonly record struct ExtractedPortLocator(
    string PortId,
    Vector3 Position,
    Quaternion Rotation);

public static class GltfPhysicsBodyExtractor
{
    public static (IReadOnlyList<ExtractedCollider> Colliders, IReadOnlyDictionary<string, ExtractedPortLocator> Ports) Extract(Node3D root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var colliders = new List<ExtractedCollider>();
        var ports = new Dictionary<string, ExtractedPortLocator>(StringComparer.OrdinalIgnoreCase);

        Traverse(root, Transform3D.Identity, isRoot: true, colliders, ports);

        return (colliders.AsReadOnly(), ports);
    }

    private static void Traverse(
        Node node,
        Transform3D parentAccumulatedTransform,
        bool isRoot,
        List<ExtractedCollider> colliders,
        Dictionary<string, ExtractedPortLocator> ports)
    {
        var currentTransform = parentAccumulatedTransform;
        if (node is Node3D node3D)
        {
            currentTransform = isRoot ? Transform3D.Identity : parentAccumulatedTransform * node3D.Transform;

            var name = node.Name.ToString();
            if (name.StartsWith("Loc_Port_", StringComparison.OrdinalIgnoreCase))
            {
                var portId = name["Loc_Port_".Length..];
                if (!string.IsNullOrWhiteSpace(portId))
                {
                    var pos = currentTransform.Origin;
                    var rot = currentTransform.Basis.GetRotationQuaternion().Normalized();
                    if (!ports.TryAdd(portId, new ExtractedPortLocator(portId, pos, rot)))
                    {
                        throw new ArgumentException($"Duplicate port locator '{portId}'.");
                    }
                }
            }
            else if (TryExtractCollider(node3D, currentTransform, out var collider))
            {
                node3D.Visible = false;
                colliders.Add(collider);
            }
        }

        foreach (Node child in node.GetChildren())
        {
            Traverse(child, currentTransform, false, colliders, ports);
        }
    }

    private static bool TryExtractCollider(Node3D node, Transform3D transform, out ExtractedCollider collider)
    {
        collider = default;
        var name = node.Name.ToString();

        if (name.StartsWith("Visual", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("vis_", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (name.StartsWith("col_cylinder", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("-col_cylinder", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("col_capsule", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("-col_capsule", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("col_hull", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("-convcol", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"Collider proxy '{name}' is not supported.");
        }

        if (node is CollisionShape3D cs)
        {
            if (cs.Shape is null)
                throw new ArgumentException($"CollisionShape3D '{name}' has a null Shape.");

            if (cs.Shape is CylinderShape3D or CapsuleShape3D)
                throw new NotSupportedException($"Collider shape '{cs.Shape.GetType().Name}' on '{name}' is not supported.");

            if (cs.Shape is BoxShape3D boxShape)
            {
                collider = ExtractBox(transform, boxShape.Size, Vector3.Zero, node);
                return true;
            }

            if (cs.Shape is SphereShape3D sphereShape)
            {
                collider = ExtractSphere(transform, sphereShape.Radius, Vector3.Zero, node);
                return true;
            }

            throw new NotSupportedException($"Collision shape '{cs.Shape.GetType().Name}' on '{name}' is not supported.");
        }

        bool isBox = name.StartsWith("col_box", StringComparison.OrdinalIgnoreCase) ||
                     name.EndsWith("-col_box", StringComparison.OrdinalIgnoreCase);
        bool isSphere = name.StartsWith("col_sphere", StringComparison.OrdinalIgnoreCase) ||
                       name.EndsWith("-col_sphere", StringComparison.OrdinalIgnoreCase);

        if (!isBox && !isSphere)
            return false;

        if (node is not MeshInstance3D && HasChildCollider(node))
            return false;

        if (isBox)
        {
            Vector3 baseSize;
            Vector3 offset = Vector3.Zero;
            if (node is MeshInstance3D mi && mi.Mesh is not null)
            {
                if (mi.Mesh is BoxMesh bm)
                {
                    baseSize = bm.Size;
                }
                else
                {
                    var aabb = mi.Mesh.GetAabb();
                    baseSize = aabb.Size;
                    offset = aabb.Position + aabb.Size * 0.5f;
                }
            }
            else
            {
                baseSize = Vector3.One;
            }
            collider = ExtractBox(transform, baseSize, offset, node);
            return true;
        }

        if (isSphere)
        {
            float baseRadius;
            Vector3 offset = Vector3.Zero;
            if (node is MeshInstance3D mi && mi.Mesh is not null)
            {
                if (mi.Mesh is SphereMesh sm)
                {
                    baseRadius = sm.Radius;
                }
                else
                {
                    var aabb = mi.Mesh.GetAabb();
                    baseRadius = aabb.Size.X * 0.5f;
                    offset = aabb.Position + aabb.Size * 0.5f;
                }
            }
            else
            {
                baseRadius = 0.5f;
            }
            collider = ExtractSphere(transform, baseRadius, offset, node);
            return true;
        }

        return false;
    }

    private static bool HasChildCollider(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is CollisionShape3D ||
                child is MeshInstance3D mi && mi.Name.ToString().StartsWith("col_", StringComparison.OrdinalIgnoreCase))
                return true;
            if (HasChildCollider(child))
                return true;
        }
        return false;
    }

    private static ExtractedCollider ExtractBox(Transform3D transform, Vector3 baseSize, Vector3 offset, Node3D node)
    {
        if (node.Scale.X <= 0f || node.Scale.Y <= 0f || node.Scale.Z <= 0f ||
            baseSize.X <= 0f || baseSize.Y <= 0f || baseSize.Z <= 0f)
        {
            throw new ArgumentException("Box collider extents must be positive finite values.");
        }

        var scale = transform.Basis.Scale;
        var hx = MathF.Abs(baseSize.X * scale.X) * 0.5f;
        var hy = MathF.Abs(baseSize.Y * scale.Y) * 0.5f;
        var hz = MathF.Abs(baseSize.Z * scale.Z) * 0.5f;

        if (!float.IsFinite(hx) || hx <= 0f ||
            !float.IsFinite(hy) || hy <= 0f ||
            !float.IsFinite(hz) || hz <= 0f)
        {
            throw new ArgumentException($"Box collider extents must be positive finite values: ({hx}, {hy}, {hz}).");
        }

        var effectiveOrigin = offset.LengthSquared() > 0.0001f ? transform * offset : transform.Origin;
        var q = transform.Basis.GetRotationQuaternion().Normalized();
        var canonicalRot = CanonicalRotation.Normalize(q.X, q.Y, q.Z, q.W);
        var localPose = new RigidLocalPose(
            new MetreVector((Half)effectiveOrigin.X, (Half)effectiveOrigin.Y, (Half)effectiveOrigin.Z),
            canonicalRot);
        localPose.Validate();

        var halfExtents = new MetreVector((Half)hx, (Half)hy, (Half)hz);
        return new ExtractedCollider(ColliderShapeKind.Box, localPose, (Half)0, halfExtents);
    }

    private static ExtractedCollider ExtractSphere(Transform3D transform, float baseRadius, Vector3 offset, Node3D node)
    {
        if (node.Scale.X <= 0f || node.Scale.Y <= 0f || node.Scale.Z <= 0f || baseRadius <= 0f)
        {
            throw new ArgumentException("Sphere collider radius must be a positive finite value.");
        }

        var scale = transform.Basis.Scale;
        var maxScale = MathF.Max(scale.X, MathF.Max(scale.Y, scale.Z));
        var effectiveRadius = baseRadius * maxScale;

        if (!float.IsFinite(effectiveRadius) || effectiveRadius <= 0f)
        {
            throw new ArgumentException($"Sphere collider radius must be a positive finite value: {effectiveRadius}.");
        }

        var effectiveOrigin = offset.LengthSquared() > 0.0001f ? transform * offset : transform.Origin;
        var q = transform.Basis.GetRotationQuaternion().Normalized();
        var canonicalRot = CanonicalRotation.Normalize(q.X, q.Y, q.Z, q.W);
        var localPose = new RigidLocalPose(
            new MetreVector((Half)effectiveOrigin.X, (Half)effectiveOrigin.Y, (Half)effectiveOrigin.Z),
            canonicalRot);
        localPose.Validate();

        return new ExtractedCollider(ColliderShapeKind.Sphere, localPose, (Half)effectiveRadius, default);
    }
}
