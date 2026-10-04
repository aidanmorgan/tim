using System;
using System.Buffers.Binary;
using CuriousContraptions.Presentation;
namespace CuriousContraptions.Gpu;

public enum AnimationControlKind : uint { Reveal = 1, Hide = 2, Visibility = 3, Endpoint = 4, TimerObservation = 5 }
public enum AnimationOutputKind : uint { Acknowledgement = 1, Sample = 2, Rejected = 3 }
public readonly record struct WorkshopHintSample(ulong Generation, PulseOrdinal Pulse, MasterTimeNanoseconds AppliedAt, Half Opacity);
public readonly record struct WorkshopAnimationControl(AnimationTargetId Target, SimulationEpoch World,
    ulong Sequence, ulong Generation, AnimationControlKind Kind, bool Visible,
    Half From, Half To, Half Duration, AnimationCurve Curve, uint EventOrdinal = 0, Half EventPhase = default, AnimationProperty Property = AnimationProperty.Opacity, AnimationTimerObservation Timer = default);
public readonly record struct WorkshopAnimationSample(AnimationTargetId Target, SimulationEpoch World,
    ulong Generation, PulseOrdinal Pulse, MasterTimeNanoseconds AppliedAt, Half Value, uint EventOrdinal, Half EventPhase, AnimationProperty Property, AnimationTimerObservation Timer = default);
public static class WorkshopAnimationWire
{
    public const ushort Version = 3;
    public const int TargetCapacity = 2 * ActivationNetwork.Capacity + 2;
    public const int ControlBytes = 144;
    public const int OutputBytes = 144;
    public static byte[] Control(RuntimeSessionId session, ClockGeneration master, CadenceRevision cadence,
        WorkshopAnimationControl control)
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
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(78), (ushort)control.Property);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(90), Version);
        WriteTimer(bytes, control.Timer);
        return bytes;
    }
    public static WorkshopAnimationControl ReadControl(ReadOnlySpan<byte> bytes, RuntimeSessionId session,
        ClockGeneration master, CadenceRevision cadence)
    {
        Identity(bytes, ControlBytes, session, master, cadence);
        var visible = BinaryPrimitives.ReadUInt32LittleEndian(bytes[52..]);
        if (visible > 1 || BinaryPrimitives.ReadUInt16LittleEndian(bytes[90..]) != Version || BinaryPrimitives.ReadUInt32LittleEndian(bytes[92..]) != 0)
            throw new ArgumentException("Invalid animation channel control padding.");
        var value = new WorkshopAnimationControl(new(BinaryPrimitives.ReadUInt64LittleEndian(bytes[56..])),
            new(BinaryPrimitives.ReadUInt64LittleEndian(bytes[64..])), BinaryPrimitives.ReadUInt64LittleEndian(bytes[32..]),
            BinaryPrimitives.ReadUInt64LittleEndian(bytes[40..]), (AnimationControlKind)BinaryPrimitives.ReadUInt32LittleEndian(bytes[48..]),
            visible != 0, ReadHalf(bytes,72), ReadHalf(bytes,74), ReadHalf(bytes,76),
            (AnimationCurve)BinaryPrimitives.ReadUInt32LittleEndian(bytes[80..]),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[84..]), ReadHalf(bytes,88), (AnimationProperty)BinaryPrimitives.ReadUInt16LittleEndian(bytes[78..]), ReadTimer(bytes));
        Validate(value); return value;
    }
    private static void Validate(WorkshopAnimationControl value)
    {
        value.Timer.Validate();
        if ((value.Kind == AnimationControlKind.TimerObservation) != (value.Timer.Phase != AnimationTimerPhase.None) ||
            (value.Kind == AnimationControlKind.TimerObservation && (value.World.Value == 0 || value.Property != AnimationProperty.ColourBlend)))
            throw new ArgumentException("Timer observation requires its committed world channel.");
        if (value.Target.Value == 0 || value.Sequence == 0 || value.Generation == 0 ||
            !IsChannel(value.Property) || !Enum.IsDefined(value.Kind) || !Enum.IsDefined(value.Curve) || !Half.IsFinite(value.From) ||
            !Half.IsFinite(value.To) || value.From < (Half)0 || value.From > (Half)1 ||
            value.To < (Half)0 || value.To > (Half)1 || !Half.IsFinite(value.Duration) ||
            value.Duration <= (Half)0 || value.Duration > (Half)30 ||
            (value.Kind == AnimationControlKind.Endpoint && value.From != value.To) ||
            !Half.IsFinite(value.EventPhase) || value.EventPhase < (Half)(-2048) || value.EventPhase >= (Half)2048 ||
            (value.EventOrdinal == 0 && value.EventPhase < (Half)0) ||
            (value.World.Value == 0 && (value.EventOrdinal != 0 || value.EventPhase != (Half)0)))
            throw new ArgumentException("Invalid animation channel declaration.");
    }
    public static WorkshopAnimationSample Read(ReadOnlySpan<byte> bytes, RuntimeSessionId session,
        ClockGeneration master, CadenceRevision cadence, out AnimationOutputKind kind)
    {
        Identity(bytes, OutputBytes, session, master, cadence);
        if (BinaryPrimitives.ReadUInt16LittleEndian(bytes[86..]) != Version || BinaryPrimitives.ReadUInt64LittleEndian(bytes[88..]) != 0) throw new ArgumentException("Invalid animation channel padding.");
        kind = (AnimationOutputKind)BinaryPrimitives.ReadUInt32LittleEndian(bytes[60..]);
        var result = new WorkshopAnimationSample(new(BinaryPrimitives.ReadUInt64LittleEndian(bytes[64..])),
            new(BinaryPrimitives.ReadUInt64LittleEndian(bytes[72..])), BinaryPrimitives.ReadUInt64LittleEndian(bytes[32..]),
            new(BinaryPrimitives.ReadUInt64LittleEndian(bytes[40..])),
            new(BinaryPrimitives.ReadInt64LittleEndian(bytes[48..])), ReadHalf(bytes,56),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[80..]), ReadHalf(bytes,84), (AnimationProperty)BinaryPrimitives.ReadUInt16LittleEndian(bytes[58..]), ReadTimer(bytes));
        result.AppliedAt.Validate();
        if (!IsChannel(result.Property) || !Enum.IsDefined(kind) || result.Generation == 0 || !Half.IsFinite(result.Value) ||
            result.Value < (Half)0 || result.Value > (Half)1 || !Half.IsFinite(result.EventPhase) ||
            result.EventPhase < (Half)(-2048) || result.EventPhase >= (Half)2048 ||
            (result.EventOrdinal == 0 && result.EventPhase < (Half)0) ||
            (result.World.Value == 0 && (result.EventOrdinal != 0 || result.EventPhase != (Half)0)))
            throw new ArgumentException("Invalid animation channel output.");
        return result;
    }
    public static void WriteTimer(Span<byte> bytes, AnimationTimerObservation timer)
    {
        timer.Validate();
        BinaryPrimitives.WriteUInt64LittleEndian(bytes[96..], timer.Started);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes[104..], timer.Due);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes[112..], timer.Observed);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes[120..], timer.InputEmitter);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes[128..], (uint)timer.Phase);
    }
    private static AnimationTimerObservation ReadTimer(ReadOnlySpan<byte> bytes)
    {
        if (BinaryPrimitives.ReadUInt32LittleEndian(bytes[132..]) != 0 ||
            BinaryPrimitives.ReadUInt64LittleEndian(bytes[136..]) != 0)
            throw new ArgumentException("Invalid timer animation padding.");
        var timer = new AnimationTimerObservation(BinaryPrimitives.ReadUInt64LittleEndian(bytes[96..]),
            BinaryPrimitives.ReadUInt64LittleEndian(bytes[104..]), BinaryPrimitives.ReadUInt64LittleEndian(bytes[112..]),
            BinaryPrimitives.ReadUInt64LittleEndian(bytes[120..]), (AnimationTimerPhase)BinaryPrimitives.ReadUInt32LittleEndian(bytes[128..]));
        timer.Validate(); return timer;
    }
    public static bool IsChannel(AnimationProperty property) => property is AnimationProperty.Opacity or AnimationProperty.ColourBlend;
    public static AnimationValue Value(AnimationProperty property, Half value) => property switch
    {
        AnimationProperty.Opacity => new(new AnimationOpacity(value)),
        AnimationProperty.ColourBlend => new(new AnimationColourBlend(value)),
        _ => throw new ArgumentException("Unsupported animation channel.")
    };
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
