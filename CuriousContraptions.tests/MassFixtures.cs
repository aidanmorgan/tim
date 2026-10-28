using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>Analytic authored inputs for native law scenes; never reads collision geometry from admitted engine state.</summary>
internal static class MassFixtures
{
    public static RigidMassProperties Primitive(CuriousContraptions.Gpu.ColliderShapeKind shape, float mass,
        float radius, Vector3 half, Vector3 centre = default, Quaternion? frame = null)
    {
        double m = mass, r = radius, x = half.X, y = half.Y, z = half.Z;
        var moments = shape switch
        {
            CuriousContraptions.Gpu.ColliderShapeKind.Sphere => new Vector3((float)(2 * m * r * r / 5)),
            CuriousContraptions.Gpu.ColliderShapeKind.Box => new Vector3((float)(m * (y * y + z * z) / 3),
                (float)(m * (x * x + z * z) / 3), (float)(m * (x * x + y * y) / 3)),
            CuriousContraptions.Gpu.ColliderShapeKind.Cylinder => new Vector3((float)(m * (r * r / 4 + y * y / 3)),
                (float)(m * r * r / 2), (float)(m * (r * r / 4 + y * y / 3))),
            _ => throw new ArgumentException("Law fixture requires a documented primitive mass law.")
        };
        var q = frame ?? Quaternion.Identity;
        return new(new(centre.X, centre.Y, centre.Z), new(q.X, q.Y, q.Z, q.W),
            new(moments.X), new(moments.Y), new(moments.Z));
    }


    public static RigidMassProperties AdmitPrimitive(RigidBodyDeclaration body, ColliderDeclaration collider)
    {
        var p = collider.Pose;
        var frame = Primitive(collider.Shape, body.Mass.Value, collider.Radius.Value,
            new(collider.HalfExtents.X, collider.HalfExtents.Y, collider.HalfExtents.Z),
            new(p.Translation.X, p.Translation.Y, p.Translation.Z),
            new Quaternion(p.Rotation.X, p.Rotation.Y, p.Rotation.Z, p.Rotation.W));
        body = body with { MassProperties = frame };
        var scene = new PhysicsSceneDeclaration(new(1, 2), 100, [body], [collider],
            [new(collider.Material, new(0f), new(.1f), new(.3f), new(0f))], [], []);
        var bytes = PhysicsGpuAbi.Admission(scene, new(1), new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        var offset = scene.Layout.GetBodyOffset(body.Id);
        float Read(int field) => BitConverter.Int32BitsToSingle(System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset + field, 4)));
        var com = PhysicsGpuAbi.BodyCentreOfMassOffset;
        var q = PhysicsGpuAbi.BodyPrincipalFrameOffset;
        var i = PhysicsGpuAbi.BodyPrincipalInertiaOffset;
        return new(new(Read(com), Read(com + 4), Read(com + 8)),
            new(Read(q), Read(q + 4), Read(q + 8), Read(q + 12)),
            new(Read(i)), new(Read(i + 4)), new(Read(i + 8)));
    }

    public static RigidBodyMassProperties Evaluate(MassFrameDeclaration frame, IReadOnlyList<ParameterDescriptor> parameters) =>
        frame.Evaluate(parameters.ToDictionary(p => new ParameterId(p.Key), p => p.Default));
}
