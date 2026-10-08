using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

[Flags]
public enum PresentationEvidenceFlags : byte { None = 0, Selected = 1, DisplayTiming = 2 }
[Flags]
public enum PresentationEndpointFlags : uint { None = 0, Present = 1, Mapped = 2 }
public readonly record struct PresentationScene(ulong Frame, byte PositionWrites, byte PartCount, double SimulationTime,
    float X, float Y, float Z, float Qx, float Qy, float Qz, float Qw);

/// <summary>Read-only fixed diagnostic encoding; none of these values can enter physics or history.</summary>
public static class WorkshopPresentationWire
{
    public const int Bytes = 352;
    public const int Capacity = 32;
    public const int RetainedBytes = Bytes * Capacity;

    public static byte[] Encode(WorkshopPresentationSample sample, bool selected,
        PresentationScene scene, MonotonicNanoseconds appliedAt, CadenceRevision cadence, PulseOrdinal presentationPulse)
    {
        var evidence = sample.Evidence;
        evidence.SelectedAt.Validate(); appliedAt.Validate(); cadence.Validate();
        if (appliedAt.Value < evidence.SelectedAt.Value || !Enum.IsDefined(sample.Quality))
            throw new ArgumentException("Invalid presentation diagnostic timing.");
        var bytes = new byte[Bytes]; var data = bytes.AsSpan();
        BinaryPrimitives.WriteUInt64LittleEndian(data, scene.Frame);
        BinaryPrimitives.WriteInt64LittleEndian(data[8..], evidence.SelectedAt.Value);
        BinaryPrimitives.WriteInt64LittleEndian(data[16..], appliedAt.Value);
        BinaryPrimitives.WriteInt64LittleEndian(data[24..], sample.DisplayWall?.Value ?? 0);
        BinaryPrimitives.WriteDoubleLittleEndian(data[32..], scene.SimulationTime);
        BinaryPrimitives.WriteUInt64LittleEndian(data[40..], evidence.WorldEpoch.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(data[48..], evidence.DisplayEpoch);
        data[56] = checked((byte)sample.Quality);
        data[57] = (byte)((selected ? PresentationEvidenceFlags.Selected : PresentationEvidenceFlags.None) |
            (sample.DisplayWall is not null ? PresentationEvidenceFlags.DisplayTiming : PresentationEvidenceFlags.None));
        data[58] = scene.PositionWrites; data[59] = scene.PartCount;
        BinaryPrimitives.WriteSingleLittleEndian(data[60..], scene.X);
        BinaryPrimitives.WriteSingleLittleEndian(data[64..], scene.Y);
        BinaryPrimitives.WriteSingleLittleEndian(data[68..], scene.Z);
        BinaryPrimitives.WriteSingleLittleEndian(data[72..], scene.Qx);
        BinaryPrimitives.WriteSingleLittleEndian(data[76..], scene.Qy);
        BinaryPrimitives.WriteSingleLittleEndian(data[80..], scene.Qz);
        BinaryPrimitives.WriteSingleLittleEndian(data[84..], scene.Qw);
        BinaryPrimitives.WriteUInt64LittleEndian(data[88..], evidence.ClockEpoch);
        WriteEndpoint(data.Slice(96, 80), evidence.Latest);
        WriteEndpoint(data.Slice(176, 80), evidence.Before);
        WriteEndpoint(data.Slice(256, 80), evidence.After);
        BinaryPrimitives.WriteUInt64LittleEndian(data[336..], cadence.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(data[344..], presentationPulse.Value);
        return bytes;
    }

    private static void WriteEndpoint(Span<byte> data, PresentationEndpoint? endpoint)
    {
        if (endpoint is not { } item) return;
        item.Capture.Validate();
        BinaryPrimitives.WriteUInt64LittleEndian(data, item.Tick.Value);
        BinaryPrimitives.WriteInt64LittleEndian(data[8..], item.Capture.Time.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(data[16..], item.Capture.Generation.Value);
        BinaryPrimitives.WriteUInt32LittleEndian(data[24..], checked((uint)item.Capture.Uncertainty.Value));
        var flags = PresentationEndpointFlags.Present;
        if (item.Mapped is { } mapped)
        {
            flags |= PresentationEndpointFlags.Mapped;
            BinaryPrimitives.WriteInt128LittleEndian(data[32..], mapped.BrowserInterval.Lower);
            BinaryPrimitives.WriteInt128LittleEndian(data[48..], mapped.BrowserInterval.Upper);
            BinaryPrimitives.WriteInt64LittleEndian(data[64..], mapped.Receipt.Value);
            BinaryPrimitives.WriteUInt64LittleEndian(data[72..], mapped.Probe.Value);
        }
        BinaryPrimitives.WriteUInt32LittleEndian(data[28..], (uint)flags);
    }
}
