using System;
using System.Numerics;

namespace CuriousContraptions.Gpu;

public static partial class PhysicsGpuAbi
{
    public const int PrismaticBytes = 160;
    public const int PrismaticImpulseOffset = 96;
    /// <summary>The rows of one prismatic joint (two transverse, three angular, the axial spring, two stops): structural.</summary>
    public const int PrismaticRowCount = 8;

    private static void WritePrismaticAdmission(Span<byte> data, PhysicsSceneDeclaration scene)
    {
        for (var i = 0; i < scene.Prismatics.Length; i++)
        {
            var value = scene.Prismatics[i];
            var record = data.Slice(scene.Layout.GetPrismaticOffset(value.Id.Value), PrismaticBytes);
            U64(record, 0, value.Id.Value);
            U32(record, 8, (uint)scene.Layout.GetBodySlot(value.BodyA)); U32(record, 12, (uint)scene.Layout.GetBodySlot(value.BodyB));
            WriteConstraintFrame(record, 16, value.FrameA); WriteConstraintFrame(record, 44, value.FrameB);
            WriteEnergy(record, 72, value.Lower); WriteEnergy(record, 76, value.Upper);
            WriteEnergy(record, 80, value.Stiffness); WriteEnergy(record, 84, value.Damping);
            U32(record, 88, (uint)value.Collision);
        }
    }

    private static void WriteConstraintFrame(Span<byte> data, int offset, ConstraintFrame frame)
    {
        WriteEnergy(data, offset, frame.Position.X); WriteEnergy(data, offset + 4, frame.Position.Y);
        WriteEnergy(data, offset + 8, frame.Position.Z);
        WriteEnergy(data, offset + 12, frame.Rotation.X); WriteEnergy(data, offset + 16, frame.Rotation.Y);
        WriteEnergy(data, offset + 20, frame.Rotation.Z); WriteEnergy(data, offset + 24, frame.Rotation.W);
    }

    private static ConstraintFrame ReadConstraintFrame(ReadOnlySpan<byte> data, int offset) =>
        new(new Vector3(RF(data, offset), RF(data, offset + 4), RF(data, offset + 8)),
            new Quaternion(RF(data, offset + 12), RF(data, offset + 16), RF(data, offset + 20), RF(data, offset + 24)));

    public static PrismaticConstraintDeclaration ReadPrismatic(ReadOnlySpan<byte> data, int index)
    {
        Header(data);
        var layout = Layout(data);
        if (index < 0 || index >= layout.Prismatics) throw new ArgumentException("Invalid prismatic table.");
        var record = data.Slice(layout.PrismaticOffset(index), PrismaticBytes);
        var a = R32(record, 8); var b = R32(record, 12);
        if (a >= R32(data, HeaderBodyCountOffset) || b >= R32(data, HeaderBodyCountOffset) || R32(record, 88) > (uint)ConnectedCollision.Enabled ||
            !AllZero(record[92..96]) || !AllZero(record[128..]))
            throw new ArgumentException("Invalid prismatic record.");
        var result = new PrismaticConstraintDeclaration(new(R64(record, 0)),
            new(R64(data, layout.BodyOffset((int)a))), new(R64(data, layout.BodyOffset((int)b))),
            ReadConstraintFrame(record, 16), ReadConstraintFrame(record, 44),
            RF(record, 72), RF(record, 76), RF(record, 80), RF(record, 84), (ConnectedCollision)R32(record, 88));
        result.Validate();
        return result;
    }

    private static void ValidatePrismaticCandidate(ReadOnlySpan<byte> candidate, ReadOnlySpan<byte> source)
    {
        // The header comparison already pinned the population; each row's declaration stays fixed.
        var layout = Layout(source);
        for (var i = 0; i < layout.Prismatics; i++)
        {
            _ = ReadPrismatic(candidate, i);
            var offset = layout.PrismaticOffset(i);
            if (!candidate.Slice(offset, PrismaticImpulseOffset).SequenceEqual(source.Slice(offset, PrismaticImpulseOffset)))
                throw new ArgumentException("Prismatic declaration changed.");
            // Two transverse, three angular, axial spring, then lower/upper unilateral stops.
            for (var row = 0; row < PrismaticRowCount; row++)
            {
                var impulse = RF(candidate, offset + PrismaticImpulseOffset + row * 4);
                if (!float.IsFinite(impulse) || (PrismaticRows.IsStop((PrismaticRowKind)row) && impulse < 0))
                    throw new ArgumentException("Invalid prismatic impulse.");
            }
        }
    }
}
