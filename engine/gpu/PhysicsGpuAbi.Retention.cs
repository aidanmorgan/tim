using System;
using System.Collections.Generic;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Gpu;

public static partial class PhysicsGpuAbi
{
    public const int MechanicalTransmissionCompressionOffset = 128;

    private static void WriteRetentionAdmission(Span<byte> record, IPhysicsSceneDeclaration scene, MechanicalTransmissionDeclaration relation)
    {
        if (relation.Retention is not { } id) return;
        var slider = scene.Prismatics[scene.Layout.GetPrismaticSlot(relation.Driven.Prismatic.Value)];
        U64(record, MechanicalTransmissionRetentionOffset, id.Value);
        F32(record, MechanicalTransmissionRetentionInitialOffset, relation.InitialRetentionCoordinate);
        F32(record, MechanicalTransmissionRetentionBoundOffset, relation.InitialRetentionCoordinate);
        // 0f - q canonicalizes an uncompressed (+0) coordinate to +0 compression rather than -0.
        var compression = 0f - relation.InitialRetentionCoordinate;
        F32(record, MechanicalTransmissionCompressionOffset, compression);
        F32(record, MechanicalTransmissionPotentialOffset, .5f * slider.Stiffness * compression * compression);
        U32(record, MechanicalTransmissionRetentionPhaseOffset, (uint)(compression <= PhysicalTolerance.ActuatorStopSlopMetres
            ? RetentionPhase.Idle : RetentionPhase.Armed));
    }

    private static PrismaticRetentionRead RetentionRead(ReadOnlySpan<byte> record, MechanicalTransmissionDeclaration relation)
    {
        var outcome = R32(record, MechanicalTransmissionRequestResultOffset);
        var mode = R32(record, MechanicalTransmissionRetentionModeOffset);
        var phase = R32(record, MechanicalTransmissionRetentionPhaseOffset);
        if (mode > (uint)RetentionMode.Releasing || phase > (uint)RetentionPhase.Blocked ||
            outcome > (uint)RetentionRequestResult.AlreadyReleased + 1u)
            throw new ArgumentException("Undefined retained-coordinate ABI tag.");
        return new(relation.Retention!.Value, relation.Owner, relation.Driven.Prismatic,
            (RetentionMode)mode,
            (RetentionPhase)phase,
            RF(record, MechanicalTransmissionCompressionOffset), RF(record, MechanicalTransmissionPotentialOffset),
            RF(record, MechanicalTransmissionAcceptedOffset), RF(record, MechanicalTransmissionReleasedOffset),
            RF(record, MechanicalTransmissionWorkOffset), RF(record, MechanicalTransmissionPositiveWorkOffset),
            RF(record, MechanicalTransmissionContactWorkOffset), RF(record, MechanicalTransmissionGravityWorkOffset),
            outcome == 0 ? null : (RetentionRequestResult)(outcome - 1));
    }

    private static void ValidateRetentionState(ReadOnlySpan<byte> record, MechanicalTransmissionDeclaration relation, ReadOnlySpan<byte> data)
    {
        if (!float.IsFinite(RF(record, MechanicalTransmissionWorkOffset)) ||
            !float.IsFinite(RF(record, MechanicalTransmissionPositiveWorkOffset)) ||
            RF(record, MechanicalTransmissionPositiveWorkOffset) < 0f ||
            !AllZero(record.Slice(MechanicalTransmissionReservedOffset, MechanicalTransmissionReservedBytes)))
            throw new ArgumentException("Invalid transmission work or reserved fields.");
        // The carried accepted-work window: every float finite; only a retained row carries a moving body's contact impulses.
        for (var offset = MechanicalTransmissionWindowImpulseOffset; offset < MechanicalTransmissionBytes; offset += 4)
            if (!float.IsFinite(RF(record, offset))) throw new ArgumentException("Invalid carried work window.");
        if (relation.Retention is null)
        {
            if (!AllZero(record.Slice(MechanicalTransmissionRetentionOffset, 48)) ||
                !PhysicsDeclarationBounds.Zero(RF(record, MechanicalTransmissionCompressionOffset)) ||
                !AllZero(record.Slice(MechanicalTransmissionWindowContactTranslationalImpulseOffset, 24)))
                throw new ArgumentException("Unowned retention state.");
            return;
        }
        var slot = CoordinateSlotIndex(R32(record, MechanicalTransmissionDrivenOffset), Layout(data).Prismatics);
        var slider = ReadPrismatic(data, slot);
        ValidateRetainedSlider(relation, slider, ReadPrismaticDrive(data, slot));
        var bound = RF(record, MechanicalTransmissionRetentionBoundOffset);
        if (!float.IsFinite(bound) || bound < slider.Lower || bound > 0f)
            throw new ArgumentException("Invalid attained retention bound.");
        var read = RetentionRead(record, relation);
        read.Validate();
        if ((read.Mode == RetentionMode.Releasing && read.Phase is not (RetentionPhase.Releasing or RetentionPhase.Blocked)) ||
            (read.Mode == RetentionMode.Retained && read.Phase == RetentionPhase.Releasing))
            throw new ArgumentException("Retention mode and phase disagree.");
    }

    public static void ValidateRetainedSlider(MechanicalTransmissionDeclaration relation, PrismaticConstraintDeclaration slider,
        PrismaticDriveDeclaration? drive)
    {
        if (relation.Retention is null) return;
        if (slider.Id != relation.Driven.Prismatic || slider.Lower >= -PhysicalTolerance.ActuatorStopSlopMetres || !PhysicsDeclarationBounds.Zero(slider.Upper) ||
            slider.Stiffness <= 0f || drive.HasValue ||
            relation.InitialRetentionCoordinate < slider.Lower || relation.InitialRetentionCoordinate > 0f)
            throw new ArgumentException("Incompatible retained prismatic coordinate.");
    }

    public static PrismaticRetentionRead[] ReadRetentions(ReadOnlySpan<byte> data)
    {
        var layout = Layout(data);
        var reads = new List<PrismaticRetentionRead>();
        for (var i = 0; i < layout.MechanicalTransmissions; i++)
        {
            var relation = ReadMechanicalTransmission(data, i);
            if (relation.Retention is null) continue;
            reads.Add(RetentionRead(data.Slice(layout.MechanicalTransmissionOffset(i), MechanicalTransmissionBytes), relation));
        }
        reads.Sort((a, b) => a.Id.Value.CompareTo(b.Id.Value));
        return [.. reads];
    }
}
