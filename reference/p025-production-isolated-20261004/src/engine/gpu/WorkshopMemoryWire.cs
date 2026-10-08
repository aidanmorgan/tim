using System;
using System.Buffers.Binary;
using System.Diagnostics;

namespace CuriousContraptions.Gpu;

public enum WorkshopCaptureMode : uint { Production = 1, Diagnostic = 2 }
public static class WorkshopBuild
{
#if PLAYTEST
    public const WorkshopCaptureMode CaptureMode = WorkshopCaptureMode.Diagnostic;
#else
    public const WorkshopCaptureMode CaptureMode = WorkshopCaptureMode.Production;
#endif
}
public enum WorkshopMemoryKind : uint { PostGc = 1 }
public enum WorkshopMemoryContext : uint { Browser = 1, Simulation = 2 }
public enum WorkshopMemoryStatus : uint { Accepted = 1, Busy = 2, Stale = 3, Unsupported = 4 }
public readonly record struct MemoryObservationId(ulong Value);
public readonly record struct WorkshopMemoryRequest(WorkshopMemoryKind Kind, WorkshopMemoryContext Context,
    RuntimeSessionId Session, MemoryObservationId Id, SimulationEpoch Epoch, AuthorityRevision Revision,
    PublicationSequence Publication);
public readonly record struct WorkshopMemoryResult(WorkshopMemoryRequest Request, WorkshopMemoryStatus Status,
    long UsedBefore = 0, long UsedAfter = 0, long DurationTicks = 0, long Frequency = 0,
    long AllocatedBefore = 0, long AllocatedAfter = 0, int CollectionsBefore = 0, int CollectionsAfter = 0);

/// <summary>One stopped collector observation; usage is not exact live or resident memory.</summary>
public static class WorkshopMemoryWire
{
    public const int RequestBytes = 64, ResultBytes = 128;
    private const uint Version = 1;

    public static bool IsStopped(WorkshopSimulationPhase phase) =>
        phase is WorkshopSimulationPhase.Building or WorkshopSimulationPhase.Completed or WorkshopSimulationPhase.Faulted;

    public static byte[] Encode(WorkshopMemoryRequest request)
    {
        var bytes = new byte[RequestBytes];
        WriteRequest(request, bytes);
        return bytes;
    }

    private static void WriteRequest(WorkshopMemoryRequest request, Span<byte> bytes)
    {
        if (!Enum.IsDefined(request.Kind) || !Enum.IsDefined(request.Context) ||
            request.Id.Value == 0 || request.Epoch.Value == 0)
            throw new ArgumentException("Invalid memory observation identity.");
        request.Session.Validate();
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, Version);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes[4..], (uint)request.Kind);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes[8..], (uint)request.Context);
        WorkshopWire.WriteSession(bytes[16..32], request.Session);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes[32..], request.Id.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes[40..], request.Epoch.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes[48..], request.Revision.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes[56..], request.Publication.Value);
    }

    public static WorkshopMemoryRequest DecodeRequest(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != RequestBytes || BinaryPrimitives.ReadUInt32LittleEndian(bytes[12..]) != 0)
            throw new ArgumentException("Invalid memory observation request shape.");
        return ReadIdentity(bytes);
    }

    private static WorkshopMemoryRequest ReadIdentity(ReadOnlySpan<byte> bytes)
    {
        if (BinaryPrimitives.ReadUInt32LittleEndian(bytes) != Version)
            throw new ArgumentException("Unsupported memory observation version.");
        var request = new WorkshopMemoryRequest((WorkshopMemoryKind)BinaryPrimitives.ReadUInt32LittleEndian(bytes[4..]),
            (WorkshopMemoryContext)BinaryPrimitives.ReadUInt32LittleEndian(bytes[8..]), WorkshopWire.ReadSession(bytes[16..32]),
            new(BinaryPrimitives.ReadUInt64LittleEndian(bytes[32..])), new(BinaryPrimitives.ReadUInt64LittleEndian(bytes[40..])),
            new(BinaryPrimitives.ReadUInt64LittleEndian(bytes[48..])), new(BinaryPrimitives.ReadUInt64LittleEndian(bytes[56..])));
        if (!Enum.IsDefined(request.Kind) || !Enum.IsDefined(request.Context) || request.Id.Value == 0 || request.Epoch.Value == 0)
            throw new ArgumentException("Invalid memory observation identity.");
        request.Session.Validate();
        return request;
    }

    public static byte[] Encode(WorkshopMemoryResult result)
    {
        Validate(result);
        var bytes = new byte[ResultBytes];
        WriteRequest(result.Request, bytes);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12), (uint)result.Status);
        BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(64), result.UsedBefore);
        BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(72), result.UsedAfter);
        BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(80), result.DurationTicks);
        BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(88), result.Frequency);
        BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(96), result.AllocatedBefore);
        BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(104), result.AllocatedAfter);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(112), result.CollectionsBefore);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(116), result.CollectionsAfter);
        return bytes;
    }

    public static WorkshopMemoryResult DecodeResult(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != ResultBytes || BinaryPrimitives.ReadUInt64LittleEndian(bytes[120..]) != 0)
            throw new ArgumentException("Invalid memory observation result shape.");
        var result = new WorkshopMemoryResult(ReadIdentity(bytes), (WorkshopMemoryStatus)BinaryPrimitives.ReadUInt32LittleEndian(bytes[12..]),
            BinaryPrimitives.ReadInt64LittleEndian(bytes[64..]), BinaryPrimitives.ReadInt64LittleEndian(bytes[72..]),
            BinaryPrimitives.ReadInt64LittleEndian(bytes[80..]), BinaryPrimitives.ReadInt64LittleEndian(bytes[88..]),
            BinaryPrimitives.ReadInt64LittleEndian(bytes[96..]), BinaryPrimitives.ReadInt64LittleEndian(bytes[104..]),
            BinaryPrimitives.ReadInt32LittleEndian(bytes[112..]), BinaryPrimitives.ReadInt32LittleEndian(bytes[116..]));
        Validate(result);
        return result;
    }

    private static void Validate(WorkshopMemoryResult result)
    {
        if (!Enum.IsDefined(result.Status)) throw new ArgumentException("Unknown memory observation status.");
        if (result.Status != WorkshopMemoryStatus.Accepted)
        {
            if (result.UsedBefore != 0 || result.UsedAfter != 0 || result.DurationTicks != 0 || result.Frequency != 0 ||
                result.AllocatedBefore != 0 || result.AllocatedAfter != 0 || result.CollectionsBefore != 0 || result.CollectionsAfter != 0)
                throw new ArgumentException("Rejected memory observation cannot contain measurements.");
            return;
        }
        if (result.UsedBefore < 0 || result.UsedAfter < 0 || result.DurationTicks < 0 || result.Frequency <= 0 ||
            result.AllocatedBefore < 0 || result.AllocatedAfter < result.AllocatedBefore ||
            result.CollectionsBefore < 0 || result.CollectionsAfter < result.CollectionsBefore)
            throw new ArgumentException("Invalid memory observation measurements.");
    }

    public static WorkshopMemoryResult Capture(WorkshopMemoryRequest request)
    {
        try
        {
            var used = GC.GetTotalMemory(false);
            var collections = GC.CollectionCount(GC.MaxGeneration);
            var allocated = GC.GetTotalAllocatedBytes(precise: true);
            var started = Stopwatch.GetTimestamp();
            var after = GC.GetTotalMemory(true);
            var elapsed = checked(Stopwatch.GetTimestamp() - started);
            return new(request, WorkshopMemoryStatus.Accepted, used, after, elapsed, Stopwatch.Frequency,
                allocated, GC.GetTotalAllocatedBytes(precise: true), collections, GC.CollectionCount(GC.MaxGeneration));
        }
        catch (PlatformNotSupportedException) { return new(request, WorkshopMemoryStatus.Unsupported); }
    }
}
