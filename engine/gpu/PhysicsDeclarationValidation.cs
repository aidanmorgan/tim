using System;
using System.Numerics;
using System.Collections.Generic;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Gpu;

public static class PhysicsDeclarationValidation
{
    public static void Validate(this PrismaticConstraintDeclaration declaration)
    {
        var (Id, BodyA, BodyB, FrameA, FrameB, Lower, Upper, Stiffness, Damping, Collision) = declaration;
        if (Id.Value == 0 || BodyA.Value == 0 || BodyB.Value == 0 || BodyA == BodyB ||
            !Enum.IsDefined(Collision) || !float.IsFinite(Lower) || !float.IsFinite(Upper) ||
            Lower < -16 || Upper > 16 || Lower >= Upper || Lower > 0 || Upper < 0)
            throw new ArgumentException("Invalid prismatic declaration.");
        FrameA.Validate(); FrameB.Validate();
        _ = SoftConstraintCoefficients.FromSpring(Stiffness, Damping, 1f / WorkshopCadenceSettings.PhysicalFrequency);
    }

    public static void Validate(this RevoluteConstraintDeclaration declaration)
    {
        var (Id, BodyA, BodyB, FrameA, FrameB, Lower, Upper, Travel, Stiffness, Damping, FrictionTorque, Collision) = declaration;
        if (Id.Value == 0 || BodyA.Value == 0 || BodyB.Value == 0 || BodyA == BodyB ||
            !Enum.IsDefined(Collision) || !Enum.IsDefined(Travel) || !float.IsFinite(Lower) || !float.IsFinite(Upper) ||
            Lower < -MathF.PI || Upper > MathF.PI || Lower >= Upper ||
            !float.IsFinite(Stiffness) || Stiffness < 0f ||
            !float.IsFinite(Damping) || Damping < 0f ||
            !float.IsFinite(FrictionTorque) || FrictionTorque < 0f ||
            (Travel == RevoluteTravel.Continuous && (Lower != -MathF.PI || Upper != MathF.PI || Stiffness != 0f)))
            throw new ArgumentException("Invalid revolute declaration.");
        FrameA.Validate(); FrameB.Validate();
        if (Stiffness > 0f)
            _ = SoftConstraintCoefficients.FromSpring(Stiffness, Damping, RevoluteRows.SubstepSeconds);
        else if (Damping > 0f)
            _ = SoftConstraintCoefficients.FromDamper(Damping, RevoluteRows.SubstepSeconds);
    }

    public static void ValidateAll(ReadOnlySpan<MechanicalTransmissionDeclaration> transmissions, IReadOnlySet<RevoluteConstraintId> joints)
    {
        var ids = new HashSet<MechanicalTransmissionId>();
        var outputs = new HashSet<RevoluteConstraintId>();
        var inputs = new HashSet<RevoluteConstraintId>();
        foreach (var transmission in transmissions)
        {
            transmission.Validate();
            if (!ids.Add(transmission.Id))
                throw new ArgumentException("Duplicate mechanical transmission identity.");
            if (!outputs.Add(transmission.DrivingJoint) || !inputs.Add(transmission.DrivenJoint))
                throw new ArgumentException("Mechanical coordinates support one connection per role.");
            if (!joints.Contains(transmission.DrivingJoint) || !joints.Contains(transmission.DrivenJoint))
                throw new ArgumentException("A mechanical transmission requires admitted revolute joints.");
        }
    }

    public static void ValidateAll(ReadOnlySpan<DrivenSurfaceDeclaration> surfaces,
        ReadOnlySpan<ColliderDeclaration> colliders, ReadOnlySpan<RevoluteConstraintDeclaration> joints)
    {
        var geometry = colliders.ToArray().ToDictionary(collider => collider.Id);
        var sources = joints.ToArray().Select(joint => joint.Id).ToHashSet();
        var ids = new HashSet<DrivenSurfaceId>();
        var faces = new HashSet<(GpuColliderId, BoxFace)>();
        foreach (var surface in surfaces)
        {
            surface.Validate();
            if (!ids.Add(surface.Id) || !faces.Add((surface.Collider, surface.Face)) ||
                !geometry.TryGetValue(surface.Collider, out var collider) ||
                collider.Body != surface.Carrier || collider.Shape != ColliderShapeKind.Box ||
                !sources.Contains(surface.Source))
                throw new ArgumentException("Driven surface references must resolve uniquely.");
            var q = collider.Pose.Rotation;
            var normal = Vector3.Transform(BoxFaces.Normal(surface.Face), new Quaternion(q.X, q.Y, q.Z, q.W));
            if (MathF.Abs(Vector3.Dot(normal, surface.LocalDirection)) > 1e-4f)
                throw new ArgumentException("Driven surface tangent must lie in its selected face.");
        }
    }
}
