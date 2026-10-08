using System;
using System.Buffers.Binary;
using CuriousContraptions.Presentation;
namespace CuriousContraptions.Gpu;

public enum HintControlKind : uint { Reveal = 1, Hide = 2, Visibility = 3, Endpoint = 4 }
public enum HintOutputKind : uint { Acknowledgement = 1, Sample = 2, Rejected = 3 }
public readonly record struct WorkshopHintSample(ulong Generation, PulseOrdinal Pulse, MasterTimeNanoseconds AppliedAt, Half Opacity);
public readonly record struct WorkshopOpacityControl(AnimationTargetId Target, SimulationEpoch World,
    ulong Sequence, ulong Generation, HintControlKind Kind, bool Visible,
    Half From, Half To, Half Duration, AnimationCurve Curve, uint EventOrdinal = 0, Half EventPhase = default);
public readonly record struct WorkshopOpacitySample(AnimationTargetId Target, SimulationEpoch World,
    ulong Generation, PulseOrdinal Pulse, MasterTimeNanoseconds AppliedAt, Half Opacity, uint EventOrdinal, Half EventPhase);
public static class WorkshopHintWire
{
    public const int ControlBytes = 96;
    public const int OutputBytes = 96;
    public static byte[] Control(RuntimeSessionId session, ClockGeneration master, CadenceRevision cadence,
        WorkshopOpacityControl control)
    {
        session.Validate(); cadence.Validate(); Validate(control);
        if (master.Value == 0) throw new ArgumentException("Invalid animation master.");
        var bytes = new byte[ControlBytes];
        WorkshopWire.WriteSession(bytes, session);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(16), master.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(24), cadence.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(32), control.Sequence);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(40), control.Generation);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(48), (uint)control.Kind);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(52), control.Visible ? 1u : 0u);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(56), control.Target.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(64), control.World.Value);
        WriteHalf(bytes, 72, control.From); WriteHalf(bytes, 74, control.To); WriteHalf(bytes, 76, control.Duration);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(80), (uint)control.Curve);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(84), control.EventOrdinal);
        WriteHalf(bytes, 88, control.EventPhase);
        return bytes;
    }
    public static WorkshopOpacityControl ReadControl(ReadOnlySpan<byte> bytes, RuntimeSessionId session,
        ClockGeneration master, CadenceRevision cadence)
    {
        Identity(bytes, ControlBytes, session, master, cadence);
        var visible = BinaryPrimitives.ReadUInt32LittleEndian(bytes[52..]);
        if (visible > 1 || BinaryPrimitives.ReadUInt16LittleEndian(bytes[78..]) != 0 ||
            BinaryPrimitives.ReadUInt16LittleEndian(bytes[90..]) != 0 || BinaryPrimitives.ReadUInt32LittleEndian(bytes[92..]) != 0)
            throw new ArgumentException("Invalid opacity control padding.");
        var value = new WorkshopOpacityControl(new(BinaryPrimitives.ReadUInt64LittleEndian(bytes[56..])),
            new(BinaryPrimitives.ReadUInt64LittleEndian(bytes[64..])), BinaryPrimitives.ReadUInt64LittleEndian(bytes[32..]),
            BinaryPrimitives.ReadUInt64LittleEndian(bytes[40..]), (HintControlKind)BinaryPrimitives.ReadUInt32LittleEndian(bytes[48..]),
            visible != 0, ReadHalf(bytes,72), ReadHalf(bytes,74), ReadHalf(bytes,76),
            (AnimationCurve)BinaryPrimitives.ReadUInt32LittleEndian(bytes[80..]),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[84..]), ReadHalf(bytes,88));
        Validate(value); return value;
    }
    private static void Validate(WorkshopOpacityControl value)
    {
        if (value.Target.Value == 0 || value.Sequence == 0 || value.Generation == 0 ||
            !Enum.IsDefined(value.Kind) || !Enum.IsDefined(value.Curve) || !Half.IsFinite(value.From) ||
            !Half.IsFinite(value.To) || value.From < (Half)0 || value.From > (Half)1 ||
            value.To < (Half)0 || value.To > (Half)1 || !Half.IsFinite(value.Duration) ||
            value.Duration <= (Half)0 || value.Duration > (Half)30 ||
            (value.Kind == HintControlKind.Endpoint && value.From != value.To) ||
            !Half.IsFinite(value.EventPhase) || value.EventPhase < (Half)(-2048) || value.EventPhase >= (Half)2048 ||
            (value.EventOrdinal == 0 && value.EventPhase < (Half)0) ||
            (value.World.Value == 0 && (value.EventOrdinal != 0 || value.EventPhase != (Half)0)))
            throw new ArgumentException("Invalid opacity declaration.");
    }
    public static WorkshopOpacitySample Read(ReadOnlySpan<byte> bytes, RuntimeSessionId session,
        ClockGeneration master, CadenceRevision cadence, out HintOutputKind kind)
    {
        Identity(bytes, OutputBytes, session, master, cadence);
        if (BinaryPrimitives.ReadUInt16LittleEndian(bytes[58..]) != 0 ||
            BinaryPrimitives.ReadUInt16LittleEndian(bytes[86..]) != 0 || BinaryPrimitives.ReadUInt64LittleEndian(bytes[88..]) != 0) throw new ArgumentException("Invalid opacity padding.");
        kind = (HintOutputKind)BinaryPrimitives.ReadUInt32LittleEndian(bytes[60..]);
        var result = new WorkshopOpacitySample(new(BinaryPrimitives.ReadUInt64LittleEndian(bytes[64..])),
            new(BinaryPrimitives.ReadUInt64LittleEndian(bytes[72..])), BinaryPrimitives.ReadUInt64LittleEndian(bytes[32..]),
            new(BinaryPrimitives.ReadUInt64LittleEndian(bytes[40..])),
            new(BinaryPrimitives.ReadInt64LittleEndian(bytes[48..])), ReadHalf(bytes,56),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[80..]), ReadHalf(bytes,84));
        result.AppliedAt.Validate();
        if (!Enum.IsDefined(kind) || result.Generation == 0 || !Half.IsFinite(result.Opacity) ||
            result.Opacity < (Half)0 || result.Opacity > (Half)1 || !Half.IsFinite(result.EventPhase) ||
            result.EventPhase < (Half)(-2048) || result.EventPhase >= (Half)2048 ||
            (result.EventOrdinal == 0 && result.EventPhase < (Half)0) ||
            (result.World.Value == 0 && (result.EventOrdinal != 0 || result.EventPhase != (Half)0)))
            throw new ArgumentException("Invalid opacity output.");
        return result;
    }
    private static void Identity(ReadOnlySpan<byte> bytes, int length, RuntimeSessionId session, ClockGeneration master, CadenceRevision cadence)
    {
        if (bytes.Length != length || WorkshopWire.ReadSession(bytes[..16]) != session ||
            BinaryPrimitives.ReadUInt64LittleEndian(bytes[16..]) != master.Value ||
            BinaryPrimitives.ReadUInt64LittleEndian(bytes[24..]) != cadence.Value)
            throw new ArgumentException("Invalid animation identity.");
    }
    private static Half ReadHalf(ReadOnlySpan<byte> data, int offset) =>
        BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]));
    private static void WriteHalf(Span<byte> data, int offset, Half value) =>
        BinaryPrimitives.WriteUInt16LittleEndian(data[offset..], BitConverter.HalfToUInt16Bits(value));
}
