using CuriousContraptions.Abstractions;
using System;
using System.Buffers.Binary;
using CuriousContraptions.Presentation;
namespace CuriousContraptions.Gpu;

/// <summary>The main thread's animation controls: UI clips and visibility (Story 9A.25b: one-shot world cues are never relayed; the
/// Animation worker drives them from the event ring).</summary>
public enum AnimationControlKind : uint { Reveal = 1, Hide = 2, Visibility = 3, Endpoint = 4 }
public enum AnimationOutputKind : uint { Acknowledgement = 1, Sample = 2, Rejected = 3 }
public readonly record struct WorkshopAnimationControl(AnimationTargetId Target, SimulationEpoch World,
    ulong Sequence, ulong Generation, AnimationControlKind Kind, bool Visible,
    float From, float To, float Duration, AnimationCurve Curve, uint EventOrdinal = 0, float EventPhase = default, AnimationProperty Property = AnimationProperty.Opacity);
public readonly record struct WorkshopAnimationSample(AnimationTargetId Target, SimulationEpoch World,
    ulong Generation, PulseOrdinal Pulse, MasterTimeNanoseconds AppliedAt, float Value, uint EventOrdinal, float EventPhase, AnimationProperty Property);
public static class WorkshopAnimationWire
{
    public const int ControlBytes = 104;
    public const int OutputBytes = 104;
    // Both records: the session (RuntimeSessionId.ByteLength) at 0, the master generation at 16 and the cadence revision at 24, then
    // their own typed control or sample fields.
    public const int SessionOffset = 0;
    public const int MasterOffset = 16;
    public const int CadenceOffset = 24;
    // Control record: sequence, generation, kind, visible, target, world, f32 from/to, curve, event ordinal, f32 event phase, zero U32,
    // f32 duration, property U16 and a zero U16 tail.
    public const int ControlSequenceOffset = 32;
    public const int ControlGenerationOffset = 40;
    public const int ControlKindOffset = 48;
    public const int ControlVisibleOffset = 52;
    public const int ControlTargetOffset = 56;
    public const int ControlWorldOffset = 64;
    public const int ControlFromOffset = 72;
    public const int ControlToOffset = 76;
    public const int ControlCurveOffset = 80;
    public const int ControlEventOrdinalOffset = 84;
    public const int ControlEventPhaseOffset = 88;
    private const int ControlPaddingOffset = 92;
    public const int ControlDurationOffset = 96;
    public const int ControlPropertyOffset = 100;
    private const int ControlPropertyPaddingOffset = 102;
    /// <summary>Output record fields (the main-thread client reads the pulse, value, kind, target and property: workshop-client.js):
    /// generation, pulse ordinal, applied-at I64, f32 value, kind, target, world, event ordinal, f32 event phase, zero U64, property U16, then zero U16 and U32 tails.</summary>
    public const int OutputGenerationOffset = 32;
    public const int OutputPulseOffset = 40;
    public const int OutputAppliedAtOffset = 48;
    public const int OutputValueOffset = 56;
    public const int OutputKindOffset = 60;
    public const int OutputTargetOffset = 64;
    public const int OutputWorldOffset = 72;
    public const int OutputEventOrdinalOffset = 80;
    public const int OutputEventPhaseOffset = 84;
    private const int OutputPaddingOffset = 88;
    public const int OutputPropertyOffset = 96;
    private const int OutputPropertyPaddingOffset = 98;
    private const int OutputTailPaddingOffset = 100;
    public static byte[] Control(RuntimeSessionId session, ClockGeneration master, CadenceRevision cadence,
        WorkshopAnimationControl control)
    {
        session.Validate(); cadence.Validate(); Validate(control);
        if (master.Value == 0) throw new ArgumentException("Invalid animation master.");
        var bytes = new byte[ControlBytes];
        session.WriteTo(bytes.AsSpan(SessionOffset, RuntimeSessionId.ByteLength));
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(MasterOffset), master.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(CadenceOffset), cadence.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(ControlSequenceOffset), control.Sequence);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(ControlGenerationOffset), control.Generation);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(ControlKindOffset), (uint)control.Kind);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(ControlVisibleOffset), control.Visible ? 1u : 0u);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(ControlTargetOffset), control.Target.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(ControlWorldOffset), control.World.Value);
        WriteFloat(bytes, ControlFromOffset, control.From); WriteFloat(bytes, ControlToOffset, control.To);
        WriteFloat(bytes, ControlDurationOffset, control.Duration);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(ControlCurveOffset), (uint)control.Curve);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(ControlEventOrdinalOffset), control.EventOrdinal);
        WriteFloat(bytes, ControlEventPhaseOffset, control.EventPhase);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(ControlPropertyOffset), (ushort)control.Property);
        return bytes;
    }
    public static WorkshopAnimationControl ReadControl(ReadOnlySpan<byte> bytes, RuntimeSessionId session,
        ClockGeneration master, CadenceRevision cadence)
    {
        Identity(bytes, ControlBytes, session, master, cadence);
        var visible = BinaryPrimitives.ReadUInt32LittleEndian(bytes[ControlVisibleOffset..]);
        if (visible > 1 || BinaryPrimitives.ReadUInt32LittleEndian(bytes[ControlPaddingOffset..]) != 0 ||
            BinaryPrimitives.ReadUInt16LittleEndian(bytes[ControlPropertyPaddingOffset..]) != 0)
            throw new ArgumentException("Invalid animation channel control padding.");
        var value = new WorkshopAnimationControl(new(BinaryPrimitives.ReadUInt64LittleEndian(bytes[ControlTargetOffset..])),
            new(BinaryPrimitives.ReadUInt64LittleEndian(bytes[ControlWorldOffset..])),
            BinaryPrimitives.ReadUInt64LittleEndian(bytes[ControlSequenceOffset..]),
            BinaryPrimitives.ReadUInt64LittleEndian(bytes[ControlGenerationOffset..]),
            (AnimationControlKind)BinaryPrimitives.ReadUInt32LittleEndian(bytes[ControlKindOffset..]),
            visible != 0, ReadFloat(bytes, ControlFromOffset), ReadFloat(bytes, ControlToOffset), ReadFloat(bytes, ControlDurationOffset),
            (AnimationCurve)BinaryPrimitives.ReadUInt32LittleEndian(bytes[ControlCurveOffset..]),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[ControlEventOrdinalOffset..]), ReadFloat(bytes, ControlEventPhaseOffset),
            (AnimationProperty)BinaryPrimitives.ReadUInt16LittleEndian(bytes[ControlPropertyOffset..]));
        Validate(value); return value;
    }
    public static void Validate(WorkshopAnimationControl value)
    {
        _ = Value(value.Property, value.From); _ = Value(value.Property, value.To);
        if (value.Target.Value == 0 || value.Sequence == 0 || value.Generation == 0 ||
            !IsChannel(value.Property) || !Enum.IsDefined(value.Kind) || !Enum.IsDefined(value.Curve) || !float.IsFinite(value.From) ||
            !float.IsFinite(value.To) || !float.IsFinite(value.Duration) ||
            value.Duration <= 0f || value.Duration > 30f ||
            (value.Kind == AnimationControlKind.Endpoint && value.From != value.To) ||
            !PhysicalPhase.IsAdmitted(value.EventOrdinal, value.EventPhase) ||
            (value.World.Value == 0 && (value.EventOrdinal != 0 || value.EventPhase != 0f)))
            throw new ArgumentException("Invalid animation channel declaration.");
    }
    public static WorkshopAnimationSample Read(ReadOnlySpan<byte> bytes, RuntimeSessionId session,
        ClockGeneration master, CadenceRevision cadence, out AnimationOutputKind kind)
    {
        Identity(bytes, OutputBytes, session, master, cadence);
        if (BinaryPrimitives.ReadUInt64LittleEndian(bytes[OutputPaddingOffset..]) != 0 ||
            BinaryPrimitives.ReadUInt16LittleEndian(bytes[OutputPropertyPaddingOffset..]) != 0 ||
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[OutputTailPaddingOffset..]) != 0)
            throw new ArgumentException("Invalid animation channel padding.");
        kind = (AnimationOutputKind)BinaryPrimitives.ReadUInt32LittleEndian(bytes[OutputKindOffset..]);
        var result = new WorkshopAnimationSample(new(BinaryPrimitives.ReadUInt64LittleEndian(bytes[OutputTargetOffset..])),
            new(BinaryPrimitives.ReadUInt64LittleEndian(bytes[OutputWorldOffset..])),
            BinaryPrimitives.ReadUInt64LittleEndian(bytes[OutputGenerationOffset..]),
            new(BinaryPrimitives.ReadUInt64LittleEndian(bytes[OutputPulseOffset..])),
            new(BinaryPrimitives.ReadInt64LittleEndian(bytes[OutputAppliedAtOffset..])), ReadFloat(bytes, OutputValueOffset),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[OutputEventOrdinalOffset..]), ReadFloat(bytes, OutputEventPhaseOffset),
            (AnimationProperty)BinaryPrimitives.ReadUInt16LittleEndian(bytes[OutputPropertyOffset..]));
        result.AppliedAt.Validate();
        _ = Value(result.Property, result.Value);
        if (!IsChannel(result.Property) || !Enum.IsDefined(kind) || result.Generation == 0 || !float.IsFinite(result.Value) ||
            !PhysicalPhase.IsAdmitted(result.EventOrdinal, result.EventPhase) ||
            (result.World.Value == 0 && (result.EventOrdinal != 0 || result.EventPhase != 0f)))
            throw new ArgumentException("Invalid animation channel output.");
        return result;
    }
    public static bool IsChannel(AnimationProperty property) => Enum.IsDefined(property);
    public static AnimationValue Value(AnimationProperty property, float value) => property switch
    {
        AnimationProperty.Opacity => new(new AnimationOpacity(value)),
        AnimationProperty.ColourBlend => new(new AnimationColourBlend(value)),
        AnimationProperty.LocalRotationAngle => new(new AnimationRadians(value)),
        AnimationProperty.LocalTranslation => new(new AnimationMetres(value)),
        AnimationProperty.UniformScale => new(new AnimationScale(value)),
        AnimationProperty.ColourRed or AnimationProperty.ColourGreen or AnimationProperty.ColourBlue => AnimationValue.Channel(property, new(value)),
        _ => throw new ArgumentException("Unsupported animation channel.")
    };
    private static void Identity(ReadOnlySpan<byte> bytes, int length, RuntimeSessionId session, ClockGeneration master, CadenceRevision cadence)
    {
        if (bytes.Length != length || RuntimeSessionId.Read(bytes.Slice(SessionOffset, RuntimeSessionId.ByteLength)) != session ||
            BinaryPrimitives.ReadUInt64LittleEndian(bytes[MasterOffset..]) != master.Value ||
            BinaryPrimitives.ReadUInt64LittleEndian(bytes[CadenceOffset..]) != cadence.Value)
            throw new ArgumentException("Invalid animation identity.");
    }
    private static float ReadFloat(ReadOnlySpan<byte> data, int offset) =>
        BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
    private static void WriteFloat(Span<byte> data, int offset, float value) =>
        BinaryPrimitives.WriteSingleLittleEndian(data[offset..], value);
}
