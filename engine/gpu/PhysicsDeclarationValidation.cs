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
            Lower < -16 || Upper > 16 || Lower >= Upper || Lower > 0 || Upper < 0 ||
            !float.IsFinite(Stiffness) || Stiffness < 0f || !float.IsFinite(Damping) || Damping < 0f)
            throw new ArgumentException("Invalid prismatic declaration.");
        FrameA.Validate(); FrameB.Validate();
        if (Stiffness > 0f)
            _ = SoftConstraintCoefficients.FromSpring(Stiffness, Damping, 1f / WorkshopCadenceSettings.PhysicalFrequency);
        else if (Damping > 0f)
            _ = SoftConstraintCoefficients.FromDamper(Damping, 1f / WorkshopCadenceSettings.PhysicalFrequency);
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

    public static void ValidateAll(ReadOnlySpan<MechanicalTransmissionDeclaration> transmissions,
        IReadOnlyDictionary<GpuBodyId, RigidBodyDeclaration> bodies,
        ReadOnlySpan<RevoluteConstraintDeclaration> joints)
    {
        var jointIds = new HashSet<RevoluteConstraintId>();
        foreach (var joint in joints) jointIds.Add(joint.Id);
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
            if (!jointIds.Contains(transmission.DrivingJoint) || !jointIds.Contains(transmission.DrivenJoint))
                throw new ArgumentException("A mechanical transmission requires admitted revolute joints.");
            if (transmission.Owner != default &&
                !bodies.ContainsKey(transmission.Owner))
                throw new ArgumentException("Mechanical transmission provenance body must be admitted.");
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


/// <summary>Unique carrying-body membership for compiled packages, independent of the selected hinge support.</summary>
public sealed class PackageBodyMembership
{
    private readonly Dictionary<GpuBodyId, GpuBodyId> _roots = new();

    public PackageBodyMembership(IPhysicsSceneDeclaration physics)
    {
        var bodies = new HashSet<GpuBodyId>();
        foreach (var body in physics.Bodies) bodies.Add(body.Id);
        var parents = new Dictionary<GpuBodyId, GpuBodyId>();
        foreach (var joint in physics.Prismatics) Add(joint.BodyA, joint.BodyB);
        foreach (var joint in physics.Revolutes) Add(joint.BodyA, joint.BodyB);
        foreach (var body in bodies)
        {
            var cursor = body;
            var visited = new HashSet<GpuBodyId>();
            while (parents.TryGetValue(cursor, out var parent))
            {
                if (!visited.Add(cursor)) throw new ArgumentException("Cyclic package support.");
                cursor = parent;
            }
            _roots.Add(body, cursor);
        }
        void Add(GpuBodyId parent, GpuBodyId child)
        {
            if (!bodies.Contains(parent) || !bodies.Contains(child) || !parents.TryAdd(child, parent))
                throw new ArgumentException("Missing or shared package support body.");
        }
    }

    public GpuBodyId Root(GpuBodyId body) => _roots.TryGetValue(body, out var root) ? root :
        throw new ArgumentException("Absent physical package body.");

    public bool Owns(GpuBodyId owner, GpuBodyId body) => _roots.TryGetValue(body, out var root) && root == owner;

    public void ValidateOwners(IReadOnlySet<GpuBodyId> owners)
    {
        foreach (var (body, root) in _roots)
        {
            if (owners.Contains(body) && body != root)
                throw new ArgumentException("A package root cannot be carried by another package.");
            if (body != root && !owners.Contains(root))
                throw new ArgumentException("A carried body has no package owner.");
        }
    }
}
