using System.Buffers.Binary;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

internal static class DrivenSurfaceAdmission
{
    public static void Validate(ReadOnlySpan<byte> input, ReadOnlySpan<DrivenSurfaceDeclaration> surfaces)
    {
        var layout = PhysicsGpuAbi.Layout(input);
        var colliders = new ColliderDeclaration[layout.Colliders];
        for (var index = 0; index < colliders.Length; index++)
        {
            var row = input.Slice(layout.ColliderOffset(index), PhysicsGpuAbi.ColliderBytes);
            var body = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(row[PhysicsGpuAbi.ColliderBodyOffset..]));
            if (body >= layout.Bodies) throw new ArgumentException("Invalid collider body.");
            var pose = PhysicsGpuAbi.ColliderPoseOffset;
            var rotation = pose + PhysicsGpuAbi.PoseRotationOffset;
            colliders[index] = new(new(BinaryPrimitives.ReadUInt64LittleEndian(row)),
                new(BinaryPrimitives.ReadUInt64LittleEndian(input[layout.BodyOffset(body)..])), default,
                (ColliderShapeKind)BinaryPrimitives.ReadUInt32LittleEndian(row[PhysicsGpuAbi.ColliderShapeOffset..]),
                new(default, new(F(row, rotation), F(row, rotation + 4), F(row, rotation + 8), F(row, rotation + 12))),
                default, default);
        }
        var joints = new RevoluteConstraintDeclaration[layout.Revolutes];
        for (var index = 0; index < joints.Length; index++) joints[index] = PhysicsGpuAbi.ReadRevolute(input, index);
        PhysicsDeclarationValidation.ValidateAll(surfaces, colliders, joints);
    }

    private static float F(ReadOnlySpan<byte> row, int offset) => BinaryPrimitives.ReadSingleLittleEndian(row[offset..]);
}
