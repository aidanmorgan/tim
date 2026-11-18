using System;
using System.Numerics;

namespace CuriousContraptions.Gpu;

/// <summary>A massless box exposure in the coordinate's inherited support frame, with material moving on BodyB.</summary>
public readonly record struct ClippedExtrusionDeclaration(PrismaticConstraintId Coordinate, GpuBodyId MaterialBody,
    GpuBodyId ClippingBody, float Cut, float DistalOffset, Vector2 HalfExtents)
{
    public void Validate()
    {
        if (Coordinate.Value == 0 || MaterialBody.Value == 0 || ClippingBody.Value == 0 || MaterialBody == ClippingBody ||
            !float.IsFinite(Cut) || !float.IsFinite(DistalOffset) || MathF.Abs(Cut) > 16f || MathF.Abs(DistalOffset) > 16f ||
            !float.IsFinite(HalfExtents.X) || !float.IsFinite(HalfExtents.Y) ||
            HalfExtents.X < ColliderDeclaration.MinimumThickness || HalfExtents.X > 16f ||
            HalfExtents.Y < ColliderDeclaration.MinimumThickness || HalfExtents.Y > 16f)
            throw new ArgumentException("Invalid clipped extrusion declaration.");
    }

    public void Validate(PrismaticConstraintDeclaration coordinate)
    {
        Validate();
        if (coordinate.Id != Coordinate || coordinate.BodyA != ClippingBody || coordinate.BodyB != MaterialBody ||
            !(DistalOffset + coordinate.Lower - Cut > 0f) || DistalOffset + coordinate.Upper - Cut > 32f)
            throw new ArgumentException("Clipped extrusion requires exact prismatic endpoints and positive bounded exposure.");
    }

    public void ValidateCollider(ColliderDeclaration collider, PrismaticConstraintDeclaration coordinate)
    {
        Validate(coordinate);
        var local = Bounds(0f);
        var expectedCentre = coordinate.FrameB.Position + Vector3.Transform(local.Centre, coordinate.FrameB.Rotation);
        var pose = collider.Pose;
        var centre = new Vector3(pose.Translation.X, pose.Translation.Y, pose.Translation.Z);
        var rotation = new Quaternion(pose.Rotation.X, pose.Rotation.Y, pose.Rotation.Z, pose.Rotation.W);
        var half = new Vector3(collider.HalfExtents.X, collider.HalfExtents.Y, collider.HalfExtents.Z);
        if (collider.Shape != ColliderShapeKind.Box || collider.Body != MaterialBody ||
            Vector3.Distance(centre, expectedCentre) > 1e-4f || Vector3.Distance(half, local.HalfExtents) > 1e-4f ||
            MathF.Abs(Quaternion.Dot(rotation, coordinate.FrameB.Rotation)) < 1f - 1e-4f)
            throw new ArgumentException("Clipped extrusion collider has an independent frame or cross-section.");
    }

    public (Vector3 Centre, Vector3 HalfExtents) Bounds(float coordinate)
    {
        var end = DistalOffset + coordinate;
        return (new(0f, (Cut + end) * .5f, 0f), new(HalfExtents.X, (end - Cut) * .5f, HalfExtents.Y));
    }
}
