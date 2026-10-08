using System;
using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

/// <summary>Only scene capture and rendering cross the single-precision boundary.</summary>
public static class SceneGeometryAdapter
{
    public static CollisionVector CaptureVector(Vector3 value) => new(value.X, value.Y, value.Z);
    public static AffineBasis CaptureBasis(Basis value) =>
        new(CaptureVector(value.X), CaptureVector(value.Y), CaptureVector(value.Z));
    public static AffineTransform CaptureAffine(Transform3D value) =>
        new(new(CaptureVector(value.Basis.X), CaptureVector(value.Basis.Y), CaptureVector(value.Basis.Z)),
            CaptureVector(value.Origin));
    public static RigidPose CaptureRigidPose(Transform3D pose)
    {
        var b = pose.Basis;
        if (!pose.Origin.IsFinite() || !b.X.IsFinite() || !b.Y.IsFinite() || !b.Z.IsFinite() ||
            Math.Abs(b.X.LengthSquared() - 1) > .00001 || Math.Abs(b.Y.LengthSquared() - 1) > .00001 ||
            Math.Abs(b.Z.LengthSquared() - 1) > .00001 || Math.Abs(b.X.Dot(b.Y)) > .00001 ||
            Math.Abs(b.X.Dot(b.Z)) > .00001 || Math.Abs(b.Y.Dot(b.Z)) > .00001 ||
            Math.Abs(b.Determinant() - 1) > .0001)
            throw new ArgumentException("Scene pose must be a finite proper rigid transform.", nameof(pose));
        var q = b.GetRotationQuaternion();
        return new(CaptureVector(pose.Origin), new(q.X, q.Y, q.Z, q.W));
    }
    public static Transform3D ToScene(this RigidPose pose)
    {
        var origin = new Vector3((float)pose.Center.X, (float)pose.Center.Y, (float)pose.Center.Z);
        if (!origin.IsFinite()) throw new InvalidOperationException("Physics position exceeds scene range.");
        return new(new Basis(new Quaternion((float)pose.Rotation.X, (float)pose.Rotation.Y,
            (float)pose.Rotation.Z, (float)pose.Rotation.W).Normalized()), origin);
    }
    public static Transform3D ToScene(this AffineTransform pose)
    {
        static Vector3 Convert(CollisionVector value)
        {
            var result = new Vector3((float)value.X, (float)value.Y, (float)value.Z);
            if (!result.IsFinite()) throw new InvalidOperationException("Affine value exceeds scene range.");
            return result;
        }
        return new(new Basis(Convert(pose.Basis.X), Convert(pose.Basis.Y), Convert(pose.Basis.Z)),
            Convert(pose.Origin));
    }
}
