using System;
using System.Numerics;

namespace CuriousContraptions.Gpu;

public static partial class PhysicsGpuAbi
{
    public const int PrismaticBytes = 160;
    /// <summary>u32 <see cref="PrismaticDriveKind"/>: immutable after admission.</summary>
    public const int PrismaticDriveKindOffset = 92;
    public const int PrismaticImpulseOffset = 96;
    /// <summary>Five immutable f32 drive values (target, maximum speed, stopping acceleration, maximum effort, maximum power); all
    /// zero for <see cref="PrismaticDriveKind.None"/>.</summary>
    public const int PrismaticDriveOffset = 128;
    public const int PrismaticDriveBytes = 20;
    /// <summary>u32 0 or 1, written by every committed tick: whether the joint's moving body took a positive normal contact impulse
    /// from a body other than the joint's base in any substep of that tick (coordinator decision 2, C-Gate). Zero at admission.</summary>
    public const int PrismaticMovingContactOffset = 148;
    private const int PrismaticPaddingOffset = 152;
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
            foreach (var drive in scene.PrismaticDrives)
            {
                if (drive.Joint != value.Id) continue;
                U32(record, PrismaticDriveKindOffset, (uint)PrismaticDriveKind.Supplied);
                WriteEnergy(record, PrismaticDriveOffset, drive.Target); WriteEnergy(record, PrismaticDriveOffset + 4, drive.MaxSpeed);
                WriteEnergy(record, PrismaticDriveOffset + 8, drive.StoppingAcceleration);
                WriteEnergy(record, PrismaticDriveOffset + 12, drive.MaxEffort); WriteEnergy(record, PrismaticDriveOffset + 16, drive.MaxPower);
            }
        }
    }

    /// <summary>The supplied drive of the world's prismatic <paramref name="index"/>, or null when it declares none.</summary>
    public static PrismaticDriveDeclaration? ReadPrismaticDrive(ReadOnlySpan<byte> data, int index)
    {
        var joint = ReadPrismatic(data, index);
        var record = data.Slice(Layout(data).PrismaticOffset(index), PrismaticBytes);
        return (PrismaticDriveKind)R32(record, PrismaticDriveKindOffset) == PrismaticDriveKind.None ? null : DriveOf(joint, record);
    }

    /// <summary>The committed moving-contact lane of the world's prismatic <paramref name="index"/>.</summary>
    public static bool ReadPrismaticMovingContact(ReadOnlySpan<byte> data, int index)
    {
        _ = ReadPrismatic(data, index);
        return R32(data.Slice(Layout(data).PrismaticOffset(index), PrismaticBytes), PrismaticMovingContactOffset) == 1;
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
        var drive = (PrismaticDriveKind)R32(record, PrismaticDriveKindOffset);
        if (a >= R32(data, HeaderBodyCountOffset) || b >= R32(data, HeaderBodyCountOffset) || R32(record, 88) > (uint)ConnectedCollision.Enabled ||
            !Enum.IsDefined(drive) || (drive == PrismaticDriveKind.None && !AllZero(record.Slice(PrismaticDriveOffset, PrismaticDriveBytes))) ||
            R32(record, PrismaticMovingContactOffset) > 1 || !AllZero(record[PrismaticPaddingOffset..]))
            throw new ArgumentException("Invalid prismatic record.");
        var result = new PrismaticConstraintDeclaration(new(R64(record, 0)),
            new(R64(data, layout.BodyOffset((int)a))), new(R64(data, layout.BodyOffset((int)b))),
            ReadConstraintFrame(record, 16), ReadConstraintFrame(record, 44),
            RF(record, 72), RF(record, 76), RF(record, 80), RF(record, 84), (ConnectedCollision)R32(record, 88));
        result.Validate();
        // A supplied record carries a valid drive for this joint (its values within the joint's limits and the envelope).
        if (drive == PrismaticDriveKind.Supplied) DriveOf(result, record).Validate(result);
        return result;
    }

    private static PrismaticDriveDeclaration DriveOf(PrismaticConstraintDeclaration joint, ReadOnlySpan<byte> record) =>
        new(joint.Id, RF(record, PrismaticDriveOffset), RF(record, PrismaticDriveOffset + 4), RF(record, PrismaticDriveOffset + 8),
            RF(record, PrismaticDriveOffset + 12), RF(record, PrismaticDriveOffset + 16));

    private static void ValidatePrismaticCandidate(ReadOnlySpan<byte> candidate, ReadOnlySpan<byte> source)
    {
        // The header comparison already pinned the population; each row's declaration stays fixed.
        var layout = Layout(source);
        for (var i = 0; i < layout.Prismatics; i++)
        {
            _ = ReadPrismaticDrive(candidate, i);
            var offset = layout.PrismaticOffset(i);
            if (!candidate.Slice(offset, PrismaticImpulseOffset).SequenceEqual(source.Slice(offset, PrismaticImpulseOffset)) ||
                !candidate.Slice(offset + PrismaticDriveOffset, PrismaticDriveBytes).SequenceEqual(source.Slice(offset + PrismaticDriveOffset, PrismaticDriveBytes)))
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
