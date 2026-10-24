using System;
using System.Numerics;

namespace CuriousContraptions.Gpu;

public static partial class PhysicsGpuAbi
{
    public const int PrismaticBytes = 160;
    // Row: identity U64 at 0, body A and body B slots, frame A and frame B (f32 position then f32 rotation), f32 lower and upper
    // limits, f32 stiffness and damping, connected-collision U32; then the drive kind, impulses, drive values and contact lane.
    public const int PrismaticBodyAOffset = 8;
    public const int PrismaticBodyBOffset = 12;
    public const int PrismaticFrameAOffset = 16;
    public const int PrismaticFrameBOffset = 44;
    /// <summary>A constraint frame's f32 rotation follows its f32 position vector.</summary>
    public const int ConstraintFrameRotationOffset = VectorBytes;
    public const int PrismaticLowerOffset = 72;
    public const int PrismaticUpperOffset = 76;
    public const int PrismaticStiffnessOffset = 80;
    public const int PrismaticDampingOffset = 84;
    public const int PrismaticCollisionOffset = 88;
    /// <summary>u32 <see cref="PrismaticDriveKind"/>: immutable after admission.</summary>
    public const int PrismaticDriveKindOffset = 92;
    public const int PrismaticImpulseOffset = 96;
    /// <summary>Five immutable f32 drive values (target, maximum speed, stopping acceleration, maximum effort, maximum power); all
    /// zero for <see cref="PrismaticDriveKind.None"/>.</summary>
    public const int PrismaticDriveOffset = 128;
    public const int PrismaticDriveTargetOffset = PrismaticDriveOffset;
    public const int PrismaticDriveMaxSpeedOffset = 132;
    public const int PrismaticDriveStoppingAccelerationOffset = 136;
    public const int PrismaticDriveMaxEffortOffset = 140;
    public const int PrismaticDriveMaxPowerOffset = 144;
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
            U64(record, RecordIdentityOffset, value.Id.Value);
            U32(record, PrismaticBodyAOffset, (uint)scene.Layout.GetBodySlot(value.BodyA));
            U32(record, PrismaticBodyBOffset, (uint)scene.Layout.GetBodySlot(value.BodyB));
            WriteConstraintFrame(record, PrismaticFrameAOffset, value.FrameA); WriteConstraintFrame(record, PrismaticFrameBOffset, value.FrameB);
            WriteEnergy(record, PrismaticLowerOffset, value.Lower); WriteEnergy(record, PrismaticUpperOffset, value.Upper);
            WriteEnergy(record, PrismaticStiffnessOffset, value.Stiffness); WriteEnergy(record, PrismaticDampingOffset, value.Damping);
            U32(record, PrismaticCollisionOffset, (uint)value.Collision);
            foreach (var drive in scene.PrismaticDrives)
            {
                if (drive.Joint != value.Id) continue;
                U32(record, PrismaticDriveKindOffset, (uint)PrismaticDriveKind.Supplied);
                WriteEnergy(record, PrismaticDriveTargetOffset, drive.Target); WriteEnergy(record, PrismaticDriveMaxSpeedOffset, drive.MaxSpeed);
                WriteEnergy(record, PrismaticDriveStoppingAccelerationOffset, drive.StoppingAcceleration);
                WriteEnergy(record, PrismaticDriveMaxEffortOffset, drive.MaxEffort); WriteEnergy(record, PrismaticDriveMaxPowerOffset, drive.MaxPower);
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
        F32Vector(data, offset, frame.Position.X, frame.Position.Y, frame.Position.Z);
        var rotation = offset + ConstraintFrameRotationOffset;
        WriteEnergy(data, rotation, frame.Rotation.X); WriteEnergy(data, rotation + RotationYOffset, frame.Rotation.Y);
        WriteEnergy(data, rotation + RotationZOffset, frame.Rotation.Z); WriteEnergy(data, rotation + RotationWOffset, frame.Rotation.W);
    }

    private static ConstraintFrame ReadConstraintFrame(ReadOnlySpan<byte> data, int offset)
    {
        var rotation = offset + ConstraintFrameRotationOffset;
        return new(new Vector3(RF(data, offset), RF(data, offset + 4), RF(data, offset + 8)),
            new Quaternion(RF(data, rotation), RF(data, rotation + RotationYOffset), RF(data, rotation + RotationZOffset),
                RF(data, rotation + RotationWOffset)));
    }

    public static PrismaticConstraintDeclaration ReadPrismatic(ReadOnlySpan<byte> data, int index)
    {
        Header(data);
        var layout = Layout(data);
        if (index < 0 || index >= layout.Prismatics) throw new ArgumentException("Invalid prismatic table.");
        var record = data.Slice(layout.PrismaticOffset(index), PrismaticBytes);
        var a = R32(record, PrismaticBodyAOffset); var b = R32(record, PrismaticBodyBOffset);
        var drive = (PrismaticDriveKind)R32(record, PrismaticDriveKindOffset);
        if (a >= R32(data, HeaderBodyCountOffset) || b >= R32(data, HeaderBodyCountOffset) || R32(record, PrismaticCollisionOffset) > (uint)ConnectedCollision.Enabled ||
            !Enum.IsDefined(drive) || (drive == PrismaticDriveKind.None && !AllZero(record.Slice(PrismaticDriveOffset, PrismaticDriveBytes))) ||
            R32(record, PrismaticMovingContactOffset) > 1 || !AllZero(record[PrismaticPaddingOffset..]))
            throw new ArgumentException("Invalid prismatic record.");
        var result = new PrismaticConstraintDeclaration(new(R64(record, RecordIdentityOffset)),
            new(R64(data, layout.BodyOffset((int)a))), new(R64(data, layout.BodyOffset((int)b))),
            ReadConstraintFrame(record, PrismaticFrameAOffset), ReadConstraintFrame(record, PrismaticFrameBOffset),
            RF(record, PrismaticLowerOffset), RF(record, PrismaticUpperOffset), RF(record, PrismaticStiffnessOffset),
            RF(record, PrismaticDampingOffset), (ConnectedCollision)R32(record, PrismaticCollisionOffset));
        result.Validate();
        // A supplied record carries a valid drive for this joint (its values within the joint's limits and the envelope).
        if (drive == PrismaticDriveKind.Supplied) DriveOf(result, record).Validate(result);
        return result;
    }

    private static PrismaticDriveDeclaration DriveOf(PrismaticConstraintDeclaration joint, ReadOnlySpan<byte> record) =>
        new(joint.Id, RF(record, PrismaticDriveTargetOffset), RF(record, PrismaticDriveMaxSpeedOffset),
            RF(record, PrismaticDriveStoppingAccelerationOffset), RF(record, PrismaticDriveMaxEffortOffset), RF(record, PrismaticDriveMaxPowerOffset));

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
