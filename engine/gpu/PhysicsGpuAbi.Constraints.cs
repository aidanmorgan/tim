using System;
using CuriousContraptions.Abstractions;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Gpu;

public static partial class PhysicsGpuAbi
{
    public const int PrismaticBytes = 224;
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
    public const int PrismaticSelectionOffset = 152;
    public const int PrismaticInactiveOffset = 156;
    public const int PrismaticPositiveOffset = 160;
    public const int PrismaticNegativeOffset = 168;
    public const int PrismaticMotorImpulseOffset = 176;
    public const int PrismaticCommandSpeedOffset = 180;
    public const int PrismaticHoldCoordinateOffset = 184;
    public const int PrismaticPhaseOffset = 188;
    public const int PrismaticWorkOffset = 192;
    public const int PrismaticTickWorkOffset = 196;
    public const int PrismaticHoldingOffset = 200;
    public const int PrismaticMotorStateEnd = 204;
    private const int PrismaticPaddingOffset = PrismaticMotorStateEnd;
    /// <summary>The rows of one prismatic joint (two transverse, three angular, the axial spring, two stops): structural.</summary>
    public const int PrismaticRowCount = 8;

    // Row: identity U64 at 0, body A and body B slots, frame A and frame B, f32 lower and upper limits, f32 stiffness and damping,
    // connected-collision U32, the f32 friction torque, the U32 travel kind, the U32 drive kind, the f32 committed drive target speed and
    // the f32 maximum torque. Every byte before RevoluteImpulseOffset is immutable after admission except the drive target, which
    // admission writes from the declaration and each tick writes from its tick input (engine-revolute-drive-target); then one committed
    // f32 impulse per RevoluteRowKind, the committed U32 supplied flag, f32 travel and f32 drive work, and zero padding to the 16-byte
    // row multiple.
    public const int RevoluteBytes = 176;
    public const int RevoluteBodyAOffset = 8;
    public const int RevoluteBodyBOffset = 12;
    public const int RevoluteFrameAOffset = 16;
    public const int RevoluteFrameBOffset = 44;
    public const int RevoluteLowerOffset = 72;
    public const int RevoluteUpperOffset = 76;
    public const int RevoluteStiffnessOffset = 80;
    public const int RevoluteDampingOffset = 84;
    public const int RevoluteCollisionOffset = 88;
    /// <summary>The hinge's f32 Coulomb friction torque (N·m).</summary>
    public const int RevoluteFrictionTorqueOffset = 92;
    /// <summary>u32 <see cref="CuriousContraptions.Abstractions.Mechanical.RevoluteTravel"/>.</summary>
    public const int RevoluteTravelKindOffset = 96;
    /// <summary>u32 <see cref="RevoluteDriveKind"/>.</summary>
    public const int RevoluteDriveKindOffset = 100;
    /// <summary>Two f32 drive values (the committed target speed rad/s, the immutable maximum torque N·m); both zero for
    /// <see cref="RevoluteDriveKind.None"/>. The target is the one target field: every reader takes the committed value.</summary>
    public const int RevoluteDriveTargetSpeedOffset = 104;
    public const int RevoluteDriveMaxTorqueOffset = 108;
    public const int RevoluteDriveBytes = 8;
    public const int RevoluteImpulseOffset = 112;
    /// <summary>The rows of one revolute joint (three linear, two transverse angular, the axial spring, two stops, friction, drive):
    /// structural.</summary>
    public const int RevoluteRowCount = RevoluteRows.Count;
    /// <summary>u32 0 or 1, written every substep: whether the hinge's drive supply route was closed for this tick (always 0 for an
    /// undriven hinge). Zero at admission.</summary>
    public const int RevoluteSuppliedOffset = 152;
    /// <summary>f32 signed travel (rad): the accumulated relative axial ω·h since admission. Zero at admission.</summary>
    public const int RevoluteTravelOffset = 156;
    /// <summary>f32 positive drive work (J): the accumulated max(0, λ_drive·ω_rel) since admission. Zero at admission.</summary>
    public const int RevoluteWorkOffset = 160;
    private const int RevolutePaddingOffset = 164;

    // Compliant surface row (Story 10.5): identity U64 at 0, frame body slot, then the f32 declaration (arrival depth, half extents,
    // rest height, stroke, tension, damping ratio) in its first CompliantSurfaceDeclarationBytes, which never change after admission;
    // then the committed f32 bed deflection (the deepest OnTop sample, 0 at rest) and zero padding.
    public const int CompliantSurfaceBytes = 64;
    public const int CompliantSurfaceBodyOffset = 8;
    public const int CompliantSurfaceArrivalDepthOffset = 12;
    public const int CompliantSurfaceHalfXOffset = 16;
    public const int CompliantSurfaceHalfZOffset = 20;
    public const int CompliantSurfaceRestHeightOffset = 24;
    public const int CompliantSurfaceMaxStrokeOffset = 28;

    // Mechanical transmission row (MechanicalTransmissionBytes = 208), one merged linear/angular relation between two typed
    // coordinates: identity U64 at 0, owner body slot (or NoBody when unowned), driving and driven coordinate slots (prismatic or
    // revolute table, by the kinds at 56/60), f32 ratio and f32 close_seconds (+0f when untimed) in
    // MechanicalTransmissionDeclarationBytes; then U32 supplied, f32 closure, U32 phase, transition origin ordinal/closure, f32
    // accumulated reaction impulse at 52 conjugate to the driven coordinate (N*s for a prismatic, N*m*s for a revolute driven
    // coordinate) against the merged Jacobian; U32 driving kind, driven kind and constraint kind at 56/60/64; four reserved zero
    // bytes at 68; the retention block at 72 (U64 id, f32 initial coordinate, U32 mode and phase, f32 attained bound, potential,
    // accepted/released elastic energy, contact and gravity work, U32 request result); f32 signed interface work and positive
    // transfer at 120/124; f32 compression at 128; then the open accepted-work window carried across the tick commit: f32 relation
    // impulse at 132, the driven coordinate's body A and body B window-start linear/angular velocities at 136/148/160/172 (zero for
    // a static body), and the retained moving body's window contact linear/angular impulse at 184/196 (zero when unretained).
    public const int MechanicalTransmissionBytes = 208;
    public const int MechanicalTransmissionOwnerOffset = 8;
    public const int MechanicalTransmissionDrivingOffset = 12;
    public const int MechanicalTransmissionDrivenOffset = 16;
    public const int MechanicalTransmissionRatioOffset = 20;
    public const int MechanicalTransmissionCloseSecondsOffset = 24;
    public const int MechanicalTransmissionDeclarationBytes = 28;
    public const int MechanicalTransmissionSuppliedOffset = 28;
    public const int MechanicalTransmissionClosureOffset = 32;
    public const int MechanicalTransmissionPhaseOffset = 36;
    public const int MechanicalTransmissionOriginOrdinalOffset = 40;
    public const int MechanicalTransmissionOriginClosureOffset = 48;
    public const int MechanicalTransmissionImpulseOffset = 52;
    public const int MechanicalTransmissionDrivingKindOffset = 56;
    public const int MechanicalTransmissionDrivenKindOffset = 60;
    public const int MechanicalTransmissionKindOffset = 64;
    public const int MechanicalTransmissionReservedOffset = 68;
    public const int MechanicalTransmissionReservedBytes = 4;
    public const int MechanicalTransmissionRetentionOffset = 72;
    public const int MechanicalTransmissionRetentionInitialOffset = 80;
    public const int MechanicalTransmissionRetentionModeOffset = 84;
    public const int MechanicalTransmissionRetentionPhaseOffset = 88;
    public const int MechanicalTransmissionRetentionBoundOffset = 92;
    public const int MechanicalTransmissionPotentialOffset = 96;
    public const int MechanicalTransmissionAcceptedOffset = 100;
    public const int MechanicalTransmissionReleasedOffset = 104;
    public const int MechanicalTransmissionContactWorkOffset = 108;
    public const int MechanicalTransmissionGravityWorkOffset = 112;
    public const int MechanicalTransmissionRequestResultOffset = 116;
    public const int MechanicalTransmissionWorkOffset = 120;
    public const int MechanicalTransmissionPositiveWorkOffset = 124;
    public const int MechanicalTransmissionWindowImpulseOffset = 132;
    public const int MechanicalTransmissionWindowDrivenAVelocityOffset = 136;
    public const int MechanicalTransmissionWindowDrivenAAngularOffset = 148;
    public const int MechanicalTransmissionWindowDrivenBVelocityOffset = 160;
    public const int MechanicalTransmissionWindowDrivenBAngularOffset = 172;
    public const int MechanicalTransmissionWindowContactTranslationalImpulseOffset = 184;
    public const int MechanicalTransmissionWindowContactAngularOffset = 196;
    public const int CompliantSurfaceTensionOffset = 32;
    public const int CompliantSurfaceDampingRatioOffset = 36;
    public const int CompliantSurfaceDeclarationBytes = 40;
    public const int CompliantSurfaceDeflectionOffset = 40;
    public const int CompliantSurfacePaddingOffset = 44;

    // Compliant engagement row (Story 10.5): one row per (surface, dynamic body), surface-major in dynamic-body order: four committed
    // U32 CompliantSampleState words, one per diagonal sample direction in sample order, then the four samples' f32 offsets.
    // Admission's zero bytes are every sample Outside with offset 0.
    public const int CompliantSampleCount = 4;
    public const int CompliantEngagementBytes = 32;
    public const int CompliantSampleStateOffset = 0;
    public const int CompliantSampleOffsetOffset = 16;

    private static void WritePrismaticAdmission(Span<byte> data, IPhysicsSceneDeclaration scene)
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
                U32(record, PrismaticSelectionOffset, (uint)drive.Selection);
                U32(record, PrismaticInactiveOffset, (uint)drive.Inactive);
                U64(record, PrismaticPositiveOffset, drive.Positive.Value);
                U64(record, PrismaticNegativeOffset, drive.Negative.Value);
                U32(record, PrismaticPhaseOffset, (uint)PrismaticDrivePhase.Unpowered);
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
            !Enum.IsDefined(drive) || (drive == PrismaticDriveKind.None && (!AllZero(record.Slice(PrismaticDriveOffset, PrismaticDriveBytes)) ||
                !AllZero(record.Slice(PrismaticSelectionOffset, PrismaticMotorStateEnd - PrismaticSelectionOffset)))) ||
            R32(record, PrismaticMovingContactOffset) > 1 || !AllZero(record[PrismaticPaddingOffset..]) ||
            R32(record, PrismaticHoldingOffset) > 1 || !Enum.IsDefined((PrismaticDrivePhase)R32(record, PrismaticPhaseOffset)))
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

    public static PrismaticMotorRead ReadPrismaticMotor(ReadOnlySpan<byte> data, int index)
    {
        _ = ReadPrismatic(data, index);
        var record = data.Slice(Layout(data).PrismaticOffset(index), PrismaticBytes);
        var result = new PrismaticMotorRead((PrismaticDrivePhase)R32(record, PrismaticPhaseOffset), RF(record, PrismaticCommandSpeedOffset),
            R32(record, PrismaticHoldingOffset) != 0, RF(record, PrismaticHoldCoordinateOffset),
            RF(record, PrismaticMotorImpulseOffset), RF(record, PrismaticWorkOffset), RF(record, PrismaticTickWorkOffset));
        result.Validate();
        return result;
    }

    private static PrismaticDriveDeclaration DriveOf(PrismaticConstraintDeclaration joint, ReadOnlySpan<byte> record) =>
        new(joint.Id, RF(record, PrismaticDriveTargetOffset), RF(record, PrismaticDriveMaxSpeedOffset),
            RF(record, PrismaticDriveStoppingAccelerationOffset), RF(record, PrismaticDriveMaxEffortOffset), RF(record, PrismaticDriveMaxPowerOffset),
            (PrismaticTargetSelection)R32(record, PrismaticSelectionOffset), (PrismaticInactiveResponse)R32(record, PrismaticInactiveOffset),
            new(R64(record, PrismaticPositiveOffset)), new(R64(record, PrismaticNegativeOffset)));

    private static void ValidatePrismaticCandidate(ReadOnlySpan<byte> candidate, ReadOnlySpan<byte> source)
    {
        // The header comparison already pinned the population; each row's declaration stays fixed.
        var layout = Layout(source);
        for (var i = 0; i < layout.Prismatics; i++)
        {
            _ = ReadPrismaticDrive(candidate, i);
            var offset = layout.PrismaticOffset(i);
            if (!candidate.Slice(offset, PrismaticImpulseOffset).SequenceEqual(source.Slice(offset, PrismaticImpulseOffset)) ||
                !candidate.Slice(offset + PrismaticDriveOffset, PrismaticDriveBytes).SequenceEqual(source.Slice(offset + PrismaticDriveOffset, PrismaticDriveBytes)) ||
                !candidate.Slice(offset + PrismaticSelectionOffset, PrismaticMotorImpulseOffset - PrismaticSelectionOffset)
                    .SequenceEqual(source.Slice(offset + PrismaticSelectionOffset, PrismaticMotorImpulseOffset - PrismaticSelectionOffset)))
                throw new ArgumentException("Prismatic declaration changed.");
            _ = ReadPrismaticMotor(candidate, i);
            // Two transverse, three angular, axial spring, then lower/upper unilateral stops.
            for (var row = 0; row < PrismaticRowCount; row++)
            {
                var impulse = RF(candidate, offset + PrismaticImpulseOffset + row * 4);
                if (!float.IsFinite(impulse) || (PrismaticRows.IsStop((PrismaticRowKind)row) && impulse < 0))
                    throw new ArgumentException("Invalid prismatic impulse.");
            }
        }
    }

    private static void WriteRevoluteAdmission(Span<byte> data, IPhysicsSceneDeclaration scene)
    {
        for (var i = 0; i < scene.Revolutes.Length; i++)
        {
            var value = scene.Revolutes[i];
            var record = data.Slice(scene.Layout.GetRevoluteOffset(value.Id.Value), RevoluteBytes);
            U64(record, RecordIdentityOffset, value.Id.Value);
            U32(record, RevoluteBodyAOffset, (uint)scene.Layout.GetBodySlot(value.BodyA));
            U32(record, RevoluteBodyBOffset, (uint)scene.Layout.GetBodySlot(value.BodyB));
            WriteConstraintFrame(record, RevoluteFrameAOffset, value.FrameA);
            WriteConstraintFrame(record, RevoluteFrameBOffset, value.FrameB);
            WriteEnergy(record, RevoluteLowerOffset, value.Lower);
            WriteEnergy(record, RevoluteUpperOffset, value.Upper);
            WriteEnergy(record, RevoluteStiffnessOffset, value.Stiffness);
            WriteEnergy(record, RevoluteDampingOffset, value.Damping);
            U32(record, RevoluteCollisionOffset, (uint)value.Collision);
            WriteEnergy(record, RevoluteFrictionTorqueOffset, value.FrictionTorque);
            U32(record, RevoluteTravelKindOffset, (uint)value.Travel);
            foreach (var drive in scene.RevoluteDrives)
            {
                if (drive.Joint != value.Id) continue;
                U32(record, RevoluteDriveKindOffset, (uint)RevoluteDriveKind.Supplied);
                WriteEnergy(record, RevoluteDriveTargetSpeedOffset, drive.TargetSpeed);
                WriteEnergy(record, RevoluteDriveMaxTorqueOffset, drive.MaxTorque);
            }
        }
    }

    /// <summary>The supplied drive of the world's revolute <paramref name="index"/>, with its committed target, or null when it declares
    /// none.</summary>
    public static RevoluteDriveDeclaration? ReadRevoluteDrive(ReadOnlySpan<byte> data, int index)
    {
        var joint = ReadRevolute(data, index);
        var record = data.Slice(Layout(data).RevoluteOffset(index), RevoluteBytes);
        return (RevoluteDriveKind)R32(record, RevoluteDriveKindOffset) == RevoluteDriveKind.None ? null : RevoluteDriveOf(joint, record);
    }

    /// <summary>Every supplied revolute's committed drive target, in world revolute order (joint identity order, which is the compiled
    /// drive-table order): the hinge's base body and the committed target.</summary>
    public static PhysicsDriveTargetRead ReadDriveTargets(ReadOnlySpan<byte> data)
    {
        var layout = Layout(data);
        var count = 0;
        for (var i = 0; i < layout.Revolutes; i++) if (ReadRevoluteDrive(data, i) is not null) count++;
        var values = new RevoluteDriveTargetRead[count];
        var next = 0;
        for (var i = 0; i < layout.Revolutes; i++)
            if (ReadRevoluteDrive(data, i) is { } drive) values[next++] = new(drive.Joint, ReadRevolute(data, i).BodyA, drive.TargetSpeed);
        return new(values);
    }

    /// <summary>The committed supplied flag, signed travel and positive drive work of the world's revolute <paramref name="index"/>.
    /// Refuses a supplied flag other than 0 or 1 (or 1 on an undriven hinge), a non-finite travel, and a work that is negative or
    /// non-finite.</summary>
    public static RevoluteStateRead ReadRevoluteState(ReadOnlySpan<byte> data, int index)
    {
        var drive = ReadRevoluteDrive(data, index);
        var record = data.Slice(Layout(data).RevoluteOffset(index), RevoluteBytes);
        var supplied = R32(record, RevoluteSuppliedOffset);
        var travel = RF(record, RevoluteTravelOffset);
        var work = RF(record, RevoluteWorkOffset);
        if (supplied > 1 || (supplied == 1 && drive is null) || !float.IsFinite(travel) || !float.IsFinite(work) || work < 0f)
            throw new ArgumentException("Invalid revolute committed state.");
        return new(supplied == 1, travel, work);
    }

    private static RevoluteDriveDeclaration RevoluteDriveOf(RevoluteConstraintDeclaration joint, ReadOnlySpan<byte> record) =>
        new(joint.Id, RF(record, RevoluteDriveTargetSpeedOffset), RF(record, RevoluteDriveMaxTorqueOffset));

    public static RevoluteConstraintDeclaration ReadRevolute(ReadOnlySpan<byte> data, int index)
    {
        Header(data);
        var layout = Layout(data);
        if (index < 0 || index >= layout.Revolutes) throw new ArgumentException("Invalid revolute table.");
        var record = data.Slice(layout.RevoluteOffset(index), RevoluteBytes);
        var a = R32(record, RevoluteBodyAOffset);
        var b = R32(record, RevoluteBodyBOffset);
        var drive = (RevoluteDriveKind)R32(record, RevoluteDriveKindOffset);
        if (a >= R32(data, HeaderBodyCountOffset) || b >= R32(data, HeaderBodyCountOffset) ||
            R32(record, RevoluteCollisionOffset) > (uint)ConnectedCollision.Enabled ||
            !Enum.IsDefined(drive) || (drive == RevoluteDriveKind.None && !AllZero(record.Slice(RevoluteDriveTargetSpeedOffset, RevoluteDriveBytes))) ||
            !AllZero(record[RevolutePaddingOffset..]))
            throw new ArgumentException("Invalid revolute record.");
        var result = new RevoluteConstraintDeclaration(new(R64(record, RecordIdentityOffset)),
            new(R64(data, layout.BodyOffset((int)a))), new(R64(data, layout.BodyOffset((int)b))),
            ReadConstraintFrame(record, RevoluteFrameAOffset), ReadConstraintFrame(record, RevoluteFrameBOffset),
            RF(record, RevoluteLowerOffset), RF(record, RevoluteUpperOffset),
            (CuriousContraptions.Abstractions.Mechanical.RevoluteTravel)R32(record, RevoluteTravelKindOffset),
            RF(record, RevoluteStiffnessOffset), RF(record, RevoluteDampingOffset), RF(record, RevoluteFrictionTorqueOffset),
            (ConnectedCollision)R32(record, RevoluteCollisionOffset));
        result.Validate();
        // A supplied record carries a valid drive for this joint (a target within the angular envelope, a positive torque).
        if (drive == RevoluteDriveKind.Supplied) RevoluteDriveOf(result, record).Validate(result);
        return result;
    }

    private static void ValidateRevoluteCandidate(ReadOnlySpan<byte> candidate, ReadOnlySpan<byte> source)
    {
        var layout = Layout(source);
        for (var i = 0; i < layout.Revolutes; i++)
        {
            var joint = ReadRevolute(candidate, i);
            var offset = layout.RevoluteOffset(i);
            // The declaration is locked except the committed drive target, which the record rule (ReadRevolute above) checks: finite,
            // within the angular envelope, and zero when undriven.
            if (!candidate.Slice(offset, RevoluteDriveTargetSpeedOffset).SequenceEqual(source.Slice(offset, RevoluteDriveTargetSpeedOffset)) ||
                !candidate.Slice(offset + RevoluteDriveMaxTorqueOffset, RevoluteImpulseOffset - RevoluteDriveMaxTorqueOffset)
                    .SequenceEqual(source.Slice(offset + RevoluteDriveMaxTorqueOffset, RevoluteImpulseOffset - RevoluteDriveMaxTorqueOffset)))
                throw new ArgumentException("Revolute declaration changed.");
            var state = ReadRevoluteState(candidate, i);
            var maxTorque = ReadRevoluteDrive(candidate, i)?.MaxTorque ?? 0f;
            // Three linear, two transverse angular, the axial spring, lower/upper unilateral stops within the travel's stop bound,
            // friction within ±τ·h, then the drive within the bound the solver clamped it to for the committed supplied flag.
            var stopLimit = RevoluteRows.StopImpulseLimit(joint.Travel);
            var frictionLimit = RevoluteRows.FrictionImpulseLimit(joint.FrictionTorque);
            var driveLimit = RevoluteRows.DriveImpulseLimit(maxTorque, state.Supplied);
            for (var row = 0; row < RevoluteRowCount; row++)
            {
                var kind = (RevoluteRowKind)row;
                var impulse = RF(candidate, offset + RevoluteImpulseOffset + row * 4);
                if (!float.IsFinite(impulse) || (RevoluteRows.IsStop(kind) && (impulse < 0 || impulse > stopLimit)) ||
                    (kind == RevoluteRowKind.Friction && MathF.Abs(impulse) > frictionLimit) ||
                    (kind == RevoluteRowKind.Drive && MathF.Abs(impulse) > driveLimit))
                    throw new ArgumentException("Invalid revolute impulse.");
            }
            // Positive work only accumulates.
            if (state.Work < RF(source, offset + RevoluteWorkOffset))
                throw new ArgumentException("Revolute drive work decreased.");
        }
    }

    private static void WriteCompliantSurfaceAdmission(Span<byte> data, IPhysicsSceneDeclaration scene)
    {
        for (var i = 0; i < scene.CompliantSurfaces.Length; i++)
        {
            var value = scene.CompliantSurfaces[i];
            var record = data.Slice(scene.Layout.GetCompliantSurfaceOffset(value.Id.Value), CompliantSurfaceBytes);
            U64(record, RecordIdentityOffset, value.Id.Value);
            U32(record, CompliantSurfaceBodyOffset, (uint)scene.Layout.GetBodySlot(value.Body));
            WriteEnergy(record, CompliantSurfaceArrivalDepthOffset, value.ArrivalDepth);
            WriteEnergy(record, CompliantSurfaceHalfXOffset, value.HalfX);
            WriteEnergy(record, CompliantSurfaceHalfZOffset, value.HalfZ);
            WriteEnergy(record, CompliantSurfaceRestHeightOffset, value.RestHeight);
            WriteEnergy(record, CompliantSurfaceMaxStrokeOffset, value.MaxStroke);
            WriteEnergy(record, CompliantSurfaceTensionOffset, value.Tension);
            WriteEnergy(record, CompliantSurfaceDampingRatioOffset, value.DampingRatio);
            record[CompliantSurfacePaddingOffset..].Clear();
        }
    }

    public static CompliantSurfaceDeclaration ReadCompliantSurface(ReadOnlySpan<byte> data, int index)
    {
        Header(data);
        var layout = Layout(data);
        if (index < 0 || index >= layout.CompliantSurfaces) throw new ArgumentException("Invalid compliant surface table.");
        var record = data.Slice(layout.CompliantSurfaceOffset(index), CompliantSurfaceBytes);
        var body = R32(record, CompliantSurfaceBodyOffset);
        if (body >= R32(data, HeaderBodyCountOffset) ||
            (RigidMotionKind)R32(data, layout.BodyOffset((int)body) + BodyMotionOffset) != RigidMotionKind.Static ||
            !AllZero(record[CompliantSurfacePaddingOffset..]))
            throw new ArgumentException("Invalid compliant surface record.");
        var result = new CompliantSurfaceDeclaration(
            new(R64(record, RecordIdentityOffset)),
            new(R64(data, layout.BodyOffset((int)body))),
            RF(record, CompliantSurfaceHalfXOffset),
            RF(record, CompliantSurfaceHalfZOffset),
            RF(record, CompliantSurfaceRestHeightOffset),
            RF(record, CompliantSurfaceMaxStrokeOffset),
            RF(record, CompliantSurfaceArrivalDepthOffset),
            RF(record, CompliantSurfaceTensionOffset),
            RF(record, CompliantSurfaceDampingRatioOffset));
        result.Validate();
        var deflection = RF(record, CompliantSurfaceDeflectionOffset);
        if (!float.IsFinite(deflection) || deflection < 0f || deflection > result.MaxStroke)
            throw new ArgumentException("Invalid compliant surface deflection.");
        return result;
    }

    /// <summary>The committed bed deflection of one compliant surface: its frame body and the depth of its deepest engaged sample.</summary>
    public static CompliantSurfaceRead ReadCompliantSurfaceDeflection(ReadOnlySpan<byte> data, int index)
    {
        var surface = ReadCompliantSurface(data, index);
        return new(surface.Body, RF(data, Layout(data).CompliantSurfaceOffset(index) + CompliantSurfaceDeflectionOffset));
    }

    /// <summary>The committed state and offset of one support sample (0–3, in sample order) of one (surface, dynamic body) pair; the body
    /// is named by its dynamic ordinal. Refuses an undefined state, and an offset that is non-finite, negative, above the surface's
    /// declared arrival depth (the law never enters a sample deeper), or non-zero on a sample that is not OnTop.</summary>
    public static CompliantSampleRead ReadCompliantSample(ReadOnlySpan<byte> data, int surface, int dynamicIndex, int sample)
    {
        if ((uint)sample >= CompliantSampleCount) throw new ArgumentOutOfRangeException(nameof(sample));
        var arrivalDepth = ReadCompliantSurface(data, surface).ArrivalDepth;
        var record = data.Slice(Layout(data).CompliantEngagementOffset(surface, dynamicIndex), CompliantEngagementBytes);
        var state = (CompliantSampleState)R32(record, CompliantSampleStateOffset + 4 * sample);
        var offset = RF(record, CompliantSampleOffsetOffset + 4 * sample);
        if (!Enum.IsDefined(state) || !float.IsFinite(offset) || offset < 0f || offset > arrivalDepth ||
            (state != CompliantSampleState.OnTop && offset != 0f))
            throw new ArgumentException("Invalid compliant sample record.");
        return new(state, offset);
    }

    /// <summary>Each surface's declaration bytes are its admitted ones; its committed deflection and every sample's state and offset are
    /// admissible values, and a deflected bed has an OnTop sample.</summary>
    private static void ValidateCompliantSurfaceCandidate(ReadOnlySpan<byte> candidate, ReadOnlySpan<byte> source)
    {
        var layout = Layout(source);
        for (var i = 0; i < layout.CompliantSurfaces; i++)
        {
            var offset = layout.CompliantSurfaceOffset(i);
            if (!candidate.Slice(offset, CompliantSurfaceDeclarationBytes).SequenceEqual(source.Slice(offset, CompliantSurfaceDeclarationBytes)))
                throw new ArgumentException("Compliant surface declaration changed.");
            var onTop = false;
            for (var body = 0; body < layout.Dynamic; body++)
                for (var sample = 0; sample < CompliantSampleCount; sample++)
                    onTop |= ReadCompliantSample(candidate, i, body, sample).State == CompliantSampleState.OnTop;
            if (ReadCompliantSurfaceDeflection(candidate, i).Deflection > 0f && !onTop)
                throw new ArgumentException("A compliant surface is deflected with no sample on top.");
        }
    }

    private static void WriteMechanicalTransmissionAdmission(Span<byte> data, IPhysicsSceneDeclaration scene)
    {
        for (var i = 0; i < scene.Transmissions.Length; i++)
        {
            var value = scene.Transmissions[i];
            var record = data.Slice(scene.Layout.GetMechanicalTransmissionOffset(value.Id.Value), MechanicalTransmissionBytes);
            U64(record, RecordIdentityOffset, value.Id.Value);
            U32(record, MechanicalTransmissionOwnerOffset,
                value.Owner == default ? NoBody : (uint)scene.Layout.GetBodySlot(value.Owner));
            U32(record, MechanicalTransmissionDrivingOffset, CoordinateSlot(scene.Layout, value.Driving));
            U32(record, MechanicalTransmissionDrivenOffset, CoordinateSlot(scene.Layout, value.Driven));
            U32(record, MechanicalTransmissionDrivingKindOffset, (uint)value.Driving.Kind);
            U32(record, MechanicalTransmissionDrivenKindOffset, (uint)value.Driven.Kind);
            U32(record, MechanicalTransmissionKindOffset, (uint)value.ConstraintKind);
            WriteEnergy(record, MechanicalTransmissionRatioOffset, value.Ratio);
            WriteEnergy(record, MechanicalTransmissionCloseSecondsOffset, value.CloseSeconds);
            U32(record, MechanicalTransmissionSuppliedOffset, 0u);
            if (PhysicsDeclarationBounds.Zero(value.CloseSeconds))
            {
                F32(record, MechanicalTransmissionClosureOffset, 1f);
                U32(record, MechanicalTransmissionPhaseOffset, (uint)TransmissionEngagementPhase.Engaged);
            }
            else
            {
                WriteEnergy(record, MechanicalTransmissionClosureOffset, 0f);
                U32(record, MechanicalTransmissionPhaseOffset, (uint)TransmissionEngagementPhase.Open);
            }
            F32(record, MechanicalTransmissionImpulseOffset, 0f);
            WriteRetentionAdmission(record, scene, value);
            WriteWorkWindowAdmission(record, scene, value);
        }
    }

    /// <summary>Admission opens an empty accepted-work window at the admitted velocities of the driven coordinate's bodies.</summary>
    private static void WriteWorkWindowAdmission(Span<byte> record, IPhysicsSceneDeclaration scene, MechanicalTransmissionDeclaration relation)
    {
        GpuBodyId a, b;
        if (relation.Driven.Kind == CoordinateKind.Prismatic)
        {
            var slider = scene.Prismatics[scene.Layout.GetPrismaticSlot(relation.Driven.Prismatic.Value)];
            (a, b) = (slider.BodyA, slider.BodyB);
        }
        else
        {
            var hinge = scene.Revolutes[scene.Layout.GetRevoluteSlot(relation.Driven.Revolute.Value)];
            (a, b) = (hinge.BodyA, hinge.BodyB);
        }
        F32(record, MechanicalTransmissionWindowImpulseOffset, 0f);
        WriteWindowStart(record, MechanicalTransmissionWindowDrivenAVelocityOffset, scene, a);
        WriteWindowStart(record, MechanicalTransmissionWindowDrivenBVelocityOffset, scene, b);
        record.Slice(MechanicalTransmissionWindowContactTranslationalImpulseOffset, 24).Clear();
    }

    private static void WriteWindowStart(Span<byte> record, int offset, IPhysicsSceneDeclaration scene, GpuBodyId id)
    {
        foreach (var body in scene.Bodies)
            if (body.Id == id && body.Motion == RigidMotionKind.Dynamic)
            {
                F32Vector(record, offset, body.Velocity.X, body.Velocity.Y, body.Velocity.Z);
                F32Vector(record, offset + 12, body.AngularVelocity.X, body.AngularVelocity.Y, body.AngularVelocity.Z);
                return;
            }
        record.Slice(offset, 24).Clear();
    }

    public static MechanicalTransmissionDeclaration ReadMechanicalTransmission(ReadOnlySpan<byte> data, int index)
    {
        Header(data);
        var layout = Layout(data);
        if (index < 0 || index >= layout.MechanicalTransmissions) throw new ArgumentException("Invalid mechanical transmission table.");
        var record = data.Slice(layout.MechanicalTransmissionOffset(index), MechanicalTransmissionBytes);
        var ownerSlot = R32(record, MechanicalTransmissionOwnerOffset);
        var drivingSlot = R32(record, MechanicalTransmissionDrivingOffset);
        var drivenSlot = R32(record, MechanicalTransmissionDrivenOffset);
        var drivingKind = R32(record, MechanicalTransmissionDrivingKindOffset);
        var drivenKind = R32(record, MechanicalTransmissionDrivenKindOffset);
        var constraintKind = R32(record, MechanicalTransmissionKindOffset);
        if ((drivingKind != (uint)CoordinateKind.Revolute && drivingKind != (uint)CoordinateKind.Prismatic) ||
            (drivenKind != (uint)CoordinateKind.Revolute && drivenKind != (uint)CoordinateKind.Prismatic) ||
            constraintKind > (uint)TransmissionConstraintKind.NonPositiveVelocity)
            throw new ArgumentException("Undefined mechanical transmission ABI tag.");
        var driving = ReadCoordinate(data, layout, (CoordinateKind)drivingKind, drivingSlot);
        var driven = ReadCoordinate(data, layout, (CoordinateKind)drivenKind, drivenSlot);
        var owner = ReadTransmissionOwner(data, layout, ownerSlot);
        var closeSeconds = RF(record, MechanicalTransmissionCloseSecondsOffset);
        if ((closeSeconds == 0f && !PhysicsDeclarationBounds.Zero(closeSeconds)) ||
            (ownerSlot == NoBody && !PhysicsDeclarationBounds.Zero(closeSeconds)))
            throw new ArgumentException("Invalid mechanical transmission close duration.");
        var result = new MechanicalTransmissionDeclaration(
            new(R64(record, RecordIdentityOffset)), driving, driven,
            RF(record, MechanicalTransmissionRatioOffset), (TransmissionConstraintKind)constraintKind, closeSeconds, owner,
            R64(record, MechanicalTransmissionRetentionOffset) is var retention && retention != 0 ? new PrismaticRetentionId(retention) : null,
            RF(record, MechanicalTransmissionRetentionInitialOffset));
        result.Validate();
        ValidateMechanicalTransmissionState(record, result.CloseSeconds, result.Retention.HasValue, result.ConstraintKind);
        ValidateRetentionState(record, result, data);
        var ordinal = checked(R64(data, HeaderTickOffset) * ReadProfile(data).Substeps);
        if (R64(record, MechanicalTransmissionOriginOrdinalOffset) > ordinal)
            throw new ArgumentException("Transmission transition origin is in the future.");
        return result;
    }

    private static uint CoordinateSlot(ICompiledMemoryLayout layout, ConstraintCoordinateReference coordinate) =>
        checked((uint)(coordinate.Kind == CoordinateKind.Prismatic
            ? layout.GetPrismaticSlot(coordinate.Prismatic.Value) : layout.GetRevoluteSlot(coordinate.Revolute.Value)));

    private static ConstraintCoordinateReference ReadCoordinate(ReadOnlySpan<byte> data, PhysicsWorldLayout layout, CoordinateKind kind, uint slot) =>
        kind switch
        {
            CoordinateKind.Prismatic => new(ReadPrismatic(data, CoordinateSlotIndex(slot, layout.Prismatics)).Id),
            CoordinateKind.Revolute => new(ReadRevolute(data, CoordinateSlotIndex(slot, layout.Revolutes)).Id),
            _ => throw new ArgumentException("Unsupported transmission coordinate kind.")
        };

    /// <summary>A full-width U32 coordinate slot inside its joint table, refused as a typed slot error before narrowing.</summary>
    private static int CoordinateSlotIndex(uint slot, int count) => slot < (uint)count
        ? (int)slot : throw new ArgumentException("Invalid mechanical transmission coordinate slot.");

    private static GpuBodyId ReadTransmissionOwner(ReadOnlySpan<byte> data, PhysicsWorldLayout layout, uint ownerSlot)
    {
        if (ownerSlot == NoBody) return default;
        if (ownerSlot >= (uint)layout.Bodies)
            throw new ArgumentException("Invalid mechanical transmission owner slot.");
        var owner = new GpuBodyId(R64(data, layout.BodyOffset((int)ownerSlot)));
        return owner;
    }

    private static void ValidateMechanicalTransmissionState(ReadOnlySpan<byte> record, float closeSeconds, bool retained,
        TransmissionConstraintKind constraintKind)
    {
        var originClosure = RF(record, MechanicalTransmissionOriginClosureOffset);
        if (!float.IsFinite(originClosure) || originClosure < 0f || originClosure > 1f)
            throw new ArgumentException("Invalid transmission transition closure.");
        var supplied = R32(record, MechanicalTransmissionSuppliedOffset);
        var closure = RF(record, MechanicalTransmissionClosureOffset);
        var phase = (TransmissionEngagementPhase)R32(record, MechanicalTransmissionPhaseOffset);
        if (supplied > 1 || !float.IsFinite(closure) || closure < 0f || closure > 1f || !Enum.IsDefined(phase) ||
            (closure == 0f && !PhysicsDeclarationBounds.Zero(closure)))
            throw new ArgumentException("Invalid mechanical transmission committed state.");
        var impulse = RF(record, MechanicalTransmissionImpulseOffset);
        if (!float.IsFinite(impulse) || (phase != TransmissionEngagementPhase.Engaged && impulse != 0f) ||
            (constraintKind == TransmissionConstraintKind.NonPositiveVelocity && impulse > 0f))
            throw new ArgumentException("Invalid mechanical transmission reaction impulse.");
        if (PhysicsDeclarationBounds.Zero(closeSeconds))
        {
            ValidateUntimedTransmissionState(record, retained, supplied, closure, phase, originClosure);
            return;
        }
        var expected = supplied == 1
            ? (closure == 1f ? TransmissionEngagementPhase.Engaged :
                closure > 0f ? TransmissionEngagementPhase.Closing : (TransmissionEngagementPhase)uint.MaxValue)
            : (PhysicsDeclarationBounds.Zero(closure) ? TransmissionEngagementPhase.Open :
                TransmissionEngagementPhase.Opening);
        if (phase != expected)
            throw new ArgumentException("Timed mechanical transmission phase disagrees with its supply or closure.");
    }

    private static void ValidateUntimedTransmissionState(ReadOnlySpan<byte> record, bool retained,
        uint supplied, float closure, TransmissionEngagementPhase phase, float originClosure)
    {
        var releasing = retained && R32(record, MechanicalTransmissionRetentionModeOffset) == (uint)RetentionMode.Releasing;
        if (supplied != 0 || closure != (releasing ? 0f : 1f) ||
            phase != (releasing ? TransmissionEngagementPhase.Open : TransmissionEngagementPhase.Engaged) ||
            R64(record, MechanicalTransmissionOriginOrdinalOffset) != 0 || !PhysicsDeclarationBounds.Zero(originClosure))
            throw new ArgumentException("Untimed mechanical transmission state disagrees with admission.");
    }

    /// <summary>Validates transmission slot <paramref name="index"/> and reads its committed reaction impulse, signed interface work and
    /// positive transfer.</summary>
    public static MechanicalTransmissionStateRead ReadMechanicalTransmissionState(ReadOnlySpan<byte> data, int index)
    {
        _ = ReadMechanicalTransmission(data, index);
        var offset = Layout(data).MechanicalTransmissionOffset(index);
        return new(RF(data, offset + MechanicalTransmissionImpulseOffset), RF(data, offset + MechanicalTransmissionWorkOffset),
            RF(data, offset + MechanicalTransmissionPositiveWorkOffset));
    }

    public static MechanicalTransmissionRead[] ReadMechanicalTransmissionReads(ReadOnlySpan<byte> data)
    {
        var layout = Layout(data);
        var list = new List<MechanicalTransmissionRead>(layout.MechanicalTransmissions);
        for (var i = 0; i < layout.MechanicalTransmissions; i++)
        {
            var declaration = ReadMechanicalTransmission(data, i);
            if (declaration.Owner == default) continue;
            var record = data.Slice(layout.MechanicalTransmissionOffset(i), MechanicalTransmissionBytes);
            var item = new MechanicalTransmissionRead(declaration.Id, declaration.Owner,
                RF(record, MechanicalTransmissionClosureOffset),
                (TransmissionEngagementPhase)R32(record, MechanicalTransmissionPhaseOffset));
            item.Validate();
            list.Add(item);
        }
        list.Sort((a, b) => a.Transmission.Value.CompareTo(b.Transmission.Value));
        return [.. list];
    }

    private static void ValidateMechanicalTransmissionCandidate(ReadOnlySpan<byte> candidate, ReadOnlySpan<byte> source)
    {
        var layout = Layout(source);
        for (var i = 0; i < layout.MechanicalTransmissions; i++)
        {
            _ = ReadMechanicalTransmission(candidate, i);
            var offset = layout.MechanicalTransmissionOffset(i);
            if (!candidate.Slice(offset, MechanicalTransmissionDeclarationBytes)
                    .SequenceEqual(source.Slice(offset, MechanicalTransmissionDeclarationBytes)) ||
                !candidate.Slice(offset + MechanicalTransmissionDrivingKindOffset, 12)
                    .SequenceEqual(source.Slice(offset + MechanicalTransmissionDrivingKindOffset, 12)) ||
                !candidate.Slice(offset + MechanicalTransmissionRetentionOffset, 12)
                    .SequenceEqual(source.Slice(offset + MechanicalTransmissionRetentionOffset, 12)))
                throw new ArgumentException("Mechanical transmission declaration changed.");
            if (RF(candidate, offset + MechanicalTransmissionPositiveWorkOffset) < RF(source, offset + MechanicalTransmissionPositiveWorkOffset) ||
                RF(candidate, offset + MechanicalTransmissionAcceptedOffset) < RF(source, offset + MechanicalTransmissionAcceptedOffset) ||
                RF(candidate, offset + MechanicalTransmissionReleasedOffset) < RF(source, offset + MechanicalTransmissionReleasedOffset))
                throw new ArgumentException("Mechanical transmission work or retention energy reversed.");
        }
    }
}
