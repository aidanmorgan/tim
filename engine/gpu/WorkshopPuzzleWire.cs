using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

internal static class WorkshopPuzzleWire
{
    public const int ByteLength = 256;
    public static void Write(Span<byte> bytes, WorkshopPuzzle puzzle)
    {
        if (bytes.Length != ByteLength) throw new ArgumentException("Invalid puzzle width.");
        bytes.Clear();
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, (uint)puzzle.Id);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes[4..], (uint)puzzle.Placement);
        H(bytes, 8, puzzle.Precision.Value);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes[10..], checked((ushort)puzzle.InventoryKind));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes[12..], puzzle.InventoryCount);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes[16..], (uint)puzzle.Goal.Kind);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes[24..], puzzle.Goal.Kind == WorkshopGoalKind.ActivatedAfter ? puzzle.Goal.SourceNode.Value : puzzle.Goal.Body.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes[32..], puzzle.Goal.Kind == WorkshopGoalKind.ActivatedAfter ? puzzle.Goal.TargetNode.Value : puzzle.Goal.Target.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes[40..], puzzle.Goal.EventSource.Value);
        H(bytes, 20, puzzle.Goal.MinimumDelay.Value);
        Profile(bytes[48..114], puzzle.BallAssistance);
        Profile(bytes[114..180], puzzle.ReceiverAssistance);
        Profile(bytes[180..246], puzzle.RampAssistance);
    }
    public static WorkshopPuzzle Read(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != ByteLength || !Zero(bytes[22..24]) || !Zero(bytes[246..]))
            throw new ArgumentException("Invalid puzzle width or padding.");
        var kind = (WorkshopGoalKind)BinaryPrimitives.ReadUInt32LittleEndian(bytes[16..]);
        var first = BinaryPrimitives.ReadUInt64LittleEndian(bytes[24..]);
        var second = BinaryPrimitives.ReadUInt64LittleEndian(bytes[32..]);
        var sensor = new GpuSensorId(BinaryPrimitives.ReadUInt64LittleEndian(bytes[40..]));
        var goal = kind == WorkshopGoalKind.ActivatedAfter
            ? new WorkshopGoal(kind, default, default, sensor, new(first), new(second), new(H(bytes, 20)))
            : new WorkshopGoal(kind, new(first), new(second), sensor, MinimumDelay: new(H(bytes, 20)));
        goal.Validate();
        return new((WorkshopPuzzleId)BinaryPrimitives.ReadUInt32LittleEndian(bytes),
            (WorkshopPlacementMode)BinaryPrimitives.ReadUInt32LittleEndian(bytes[4..]), new(H(bytes, 8)),
            (WorkshopPartKind)BinaryPrimitives.ReadUInt16LittleEndian(bytes[10..]),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[12..]), goal,
            Profile(bytes[48..114]), Profile(bytes[114..180]), Profile(bytes[180..246]));
    }
    private static void Profile(Span<byte> bytes, AssistanceProfile profile)
    {
        Knot(bytes[..22], profile.Forgiving); Knot(bytes[22..44], profile.Balanced); Knot(bytes[44..66], profile.Precise);
    }
    private static AssistanceProfile Profile(ReadOnlySpan<byte> bytes) => new(Knot(bytes[..22]), Knot(bytes[22..44]), Knot(bytes[44..66]));
    private static void Knot(Span<byte> bytes, AssistanceKnot knot)
    {
        H(bytes, 0, knot.Precision); H(bytes, 2, knot.PositionWindow.Value); H(bytes, 4, knot.RotationWindowDegrees);
        H(bytes, 6, knot.MaximumPositionCorrection.Value); H(bytes, 8, knot.MaximumRotationCorrectionDegrees);
        H(bytes, 10, knot.Blend.Value); H(bytes, 12, knot.CaptureMargin.Value); H(bytes, 14, knot.CaptureSpeed.Value);
        H(bytes, 16, knot.CaptureDwell.Value); H(bytes, 18, knot.GuideAcceleration.Value); H(bytes, 20, knot.TriggerThreshold);
    }
    private static AssistanceKnot Knot(ReadOnlySpan<byte> bytes) => new(H(bytes, 0), new(H(bytes, 2)), H(bytes, 4),
        new(H(bytes, 6)), H(bytes, 8), new(H(bytes, 10)), new(H(bytes, 12)), new(H(bytes, 14)), new(H(bytes, 16)), new(H(bytes, 18)), H(bytes, 20));
    private static void H(Span<byte> bytes, int offset, Half value) => BinaryPrimitives.WriteUInt16LittleEndian(bytes[offset..], BitConverter.HalfToUInt16Bits(value));
    private static Half H(ReadOnlySpan<byte> bytes, int offset) => BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReadUInt16LittleEndian(bytes[offset..]));
    private static bool Zero(ReadOnlySpan<byte> bytes) => bytes.IndexOfAnyExcept((byte)0) < 0;
}
