using System;
using System.Buffers.Binary;
using CuriousContraptions.Presentation;
namespace CuriousContraptions.Gpu;

public enum AnimationControlKind : uint { Reveal = 1, Hide = 2, Visibility = 3, Endpoint = 4, TimerObservation = 5, Impulse = 6, Transition = 7 }
public enum AnimationOutputKind : uint { Acknowledgement = 1, Sample = 2, Rejected = 3 }
public readonly record struct WorkshopAnimationControl(AnimationTargetId Target, SimulationEpoch World,
    ulong Sequence, ulong Generation, AnimationControlKind Kind, bool Visible,
    float From, float To, float Duration, AnimationCurve Curve, uint EventOrdinal = 0, float EventPhase = default, AnimationProperty Property = AnimationProperty.Opacity, AnimationTimerObservation Timer = default,
    AnimationImpulseCurve ImpulseCurve = default, AnimationImpulseOverlap Overlap = default);
public readonly record struct WorkshopAnimationSample(AnimationTargetId Target, SimulationEpoch World,
    ulong Generation, PulseOrdinal Pulse, MasterTimeNanoseconds AppliedAt, float Value, uint EventOrdinal, float EventPhase, AnimationProperty Property, AnimationTimerObservation Timer = default, float PulseDuration = default);
public static class WorkshopAnimationWire
{
    /// <summary>Concurrent occurrences one impulse target retains; a declared cooldown keeps real overlap far below it (time, not a
    /// machine population).</summary>
    public const int ImpulseCapacity = 64;
    public const int ControlBytes = 144;
    public const int OutputBytes = 144;
    public static byte[] Control(RuntimeSessionId session, ClockGeneration master, CadenceRevision cadence,
        WorkshopAnimationControl control)
    {
        session.Validate(); cadence.Validate(); Validate(control);
        if (master.Value == 0) throw new ArgumentException("Invalid animation master.");
        var bytes = new byte[ControlBytes];
        session.WriteTo(bytes.AsSpan(0, RuntimeSessionId.ByteLength));
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(16), master.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(24), cadence.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(32), control.Sequence);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(40), control.Generation);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(48), (uint)control.Kind);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(52), control.Visible ? 1u : 0u);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(56), control.Target.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(64), control.World.Value);
        WriteFloat(bytes, 72, control.From); WriteFloat(bytes, 76, control.To); WriteFloat(bytes, 136, control.Duration);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(80), (uint)control.Curve);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(84), control.EventOrdinal);
        WriteFloat(bytes, 88, control.EventPhase);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(140), (ushort)control.Property);
        WriteTimer(bytes, control.Timer);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(92), (ushort)control.ImpulseCurve);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(94), (ushort)control.Overlap);
        return bytes;
    }
    public static WorkshopAnimationControl ReadControl(ReadOnlySpan<byte> bytes, RuntimeSessionId session,
        ClockGeneration master, CadenceRevision cadence)
    {
        Identity(bytes, ControlBytes, session, master, cadence);
        var visible = BinaryPrimitives.ReadUInt32LittleEndian(bytes[52..]);
        if (visible > 1 || BinaryPrimitives.ReadUInt32LittleEndian(bytes[132..]) != 0 || BinaryPrimitives.ReadUInt16LittleEndian(bytes[142..]) != 0)
            throw new ArgumentException("Invalid animation channel control padding.");
        var value = new WorkshopAnimationControl(new(BinaryPrimitives.ReadUInt64LittleEndian(bytes[56..])),
            new(BinaryPrimitives.ReadUInt64LittleEndian(bytes[64..])), BinaryPrimitives.ReadUInt64LittleEndian(bytes[32..]),
            BinaryPrimitives.ReadUInt64LittleEndian(bytes[40..]), (AnimationControlKind)BinaryPrimitives.ReadUInt32LittleEndian(bytes[48..]),
            visible != 0, ReadFloat(bytes,72), ReadFloat(bytes,76), ReadFloat(bytes,136),
            (AnimationCurve)BinaryPrimitives.ReadUInt32LittleEndian(bytes[80..]),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[84..]), ReadFloat(bytes,88), (AnimationProperty)BinaryPrimitives.ReadUInt16LittleEndian(bytes[140..]), ReadTimer(bytes),
            (AnimationImpulseCurve)BinaryPrimitives.ReadUInt16LittleEndian(bytes[92..]), (AnimationImpulseOverlap)BinaryPrimitives.ReadUInt16LittleEndian(bytes[94..]));
        Validate(value); return value;
    }
    private static void Validate(WorkshopAnimationControl value)
    {
        value.Timer.Validate();
        _ = Value(value.Property, value.From); _ = Value(value.Property, value.To);
        if (value.Kind == AnimationControlKind.Transition && (value.World.Value == 0 || value.Property != AnimationProperty.ColourBlend))
            throw new ArgumentException("Transition requires a committed world colour channel.");
        if ((value.Kind == AnimationControlKind.TimerObservation) != (value.Timer.Phase != AnimationTimerPhase.None) ||
            (value.Kind == AnimationControlKind.TimerObservation && (value.World.Value == 0 || value.Property != AnimationProperty.ColourBlend)))
            throw new ArgumentException("Timer observation requires its committed world channel.");
        if (value.Kind == AnimationControlKind.Impulse && (value.World.Value == 0 ||
            value.Property != AnimationProperty.ColourBlend || value.From != (float)0 || value.To != (float)1))
            throw new ArgumentException("Impulse requires an owned committed occurrence.");
        if (value.Kind == AnimationControlKind.Impulse ? !Enum.IsDefined(value.ImpulseCurve) || !Enum.IsDefined(value.Overlap)
            : value.ImpulseCurve != default || value.Overlap != default)
            throw new ArgumentException("Impulse envelope belongs only to a declared impulse control.");
        if (value.Target.Value == 0 || value.Sequence == 0 || value.Generation == 0 ||
            !IsChannel(value.Property) || !Enum.IsDefined(value.Kind) || !Enum.IsDefined(value.Curve) || !float.IsFinite(value.From) ||
            !float.IsFinite(value.To) || !float.IsFinite(value.Duration) ||
            value.Duration <= (float)0 || value.Duration > (float)30 ||
            (value.Kind == AnimationControlKind.Endpoint && value.From != value.To) ||
            !float.IsFinite(value.EventPhase) || value.EventPhase < (float)(-2048) || value.EventPhase >= (float)2048 ||
            (value.EventOrdinal == 0 && value.EventPhase < (float)0) ||
            (value.World.Value == 0 && (value.EventOrdinal != 0 || value.EventPhase != (float)0)))
            throw new ArgumentException("Invalid animation channel declaration.");
    }
    public static WorkshopAnimationSample Read(ReadOnlySpan<byte> bytes, RuntimeSessionId session,
        ClockGeneration master, CadenceRevision cadence, out AnimationOutputKind kind)
    {
        Identity(bytes, OutputBytes, session, master, cadence);
        if (BinaryPrimitives.ReadUInt64LittleEndian(bytes[88..]) != 0 || BinaryPrimitives.ReadUInt16LittleEndian(bytes[138..]) != 0 || BinaryPrimitives.ReadUInt32LittleEndian(bytes[140..]) != 0) throw new ArgumentException("Invalid animation channel padding.");
        kind = (AnimationOutputKind)BinaryPrimitives.ReadUInt32LittleEndian(bytes[60..]);
        var result = new WorkshopAnimationSample(new(BinaryPrimitives.ReadUInt64LittleEndian(bytes[64..])),
            new(BinaryPrimitives.ReadUInt64LittleEndian(bytes[72..])), BinaryPrimitives.ReadUInt64LittleEndian(bytes[32..]),
            new(BinaryPrimitives.ReadUInt64LittleEndian(bytes[40..])),
            new(BinaryPrimitives.ReadInt64LittleEndian(bytes[48..])), ReadFloat(bytes,56),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[80..]), ReadFloat(bytes,84), (AnimationProperty)BinaryPrimitives.ReadUInt16LittleEndian(bytes[136..]), ReadTimer(bytes), ReadFloat(bytes,132));
        result.AppliedAt.Validate();
        _ = Value(result.Property, result.Value);
        if (!float.IsFinite(result.PulseDuration) || result.PulseDuration < (float)0 || result.PulseDuration > (float)30 ||
            (result.PulseDuration > (float)0 && (result.World.Value == 0 || result.Property != AnimationProperty.ColourBlend || result.Timer != default)))
            throw new ArgumentException("Invalid committed pulse segment.");
        if (!IsChannel(result.Property) || !Enum.IsDefined(kind) || result.Generation == 0 || !float.IsFinite(result.Value) ||
            !float.IsFinite(result.EventPhase) ||
            result.EventPhase < (float)(-2048) || result.EventPhase >= (float)2048 ||
            (result.EventOrdinal == 0 && result.EventPhase < (float)0) ||
            (result.World.Value == 0 && (result.EventOrdinal != 0 || result.EventPhase != (float)0)))
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
        var timer = new AnimationTimerObservation(BinaryPrimitives.ReadUInt64LittleEndian(bytes[96..]),
            BinaryPrimitives.ReadUInt64LittleEndian(bytes[104..]), BinaryPrimitives.ReadUInt64LittleEndian(bytes[112..]),
            BinaryPrimitives.ReadUInt64LittleEndian(bytes[120..]), (AnimationTimerPhase)BinaryPrimitives.ReadUInt32LittleEndian(bytes[128..]));
        timer.Validate(); return timer;
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
        if (bytes.Length != length || RuntimeSessionId.Read(bytes[..16]) != session ||
            BinaryPrimitives.ReadUInt64LittleEndian(bytes[16..]) != master.Value ||
            BinaryPrimitives.ReadUInt64LittleEndian(bytes[24..]) != cadence.Value)
            throw new ArgumentException("Invalid animation identity.");
    }
    private static float ReadFloat(ReadOnlySpan<byte> data, int offset) =>
        BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);
    private static void WriteFloat(Span<byte> data, int offset, float value) =>
        BinaryPrimitives.WriteSingleLittleEndian(data[offset..], value);
}
