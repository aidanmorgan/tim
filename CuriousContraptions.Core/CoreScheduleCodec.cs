using System;
using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Runtime;

public sealed class CoreScheduleCodec : IScheduleCodec
{
    public WorkshopCadenceSettings ReadCadence(ReadOnlySpan<byte> source) => WorkshopCadenceWire.Read(source);
    public void WriteCadence(WorkshopCadenceSettings settings, Span<byte> destination) => WorkshopCadenceWire.Write(settings, destination);
    public int CadenceBytes => WorkshopCadenceWire.ByteLength;
    public int PrepareBytes(int endpointBytes) => WorkshopScheduleWire.PrepareBytes(endpointBytes);
    public int PayloadBytes(ScheduleControlKind kind) => WorkshopScheduleWire.PayloadBytes(kind);
    public void WriteHeader(ScheduleControlHeader header, Span<byte> destination) => WorkshopScheduleWire.WriteHeader(header, destination);
    public void WriteHeader(ScheduleControlHeader header, Span<byte> destination, int payloadBytes) => WorkshopScheduleWire.WriteHeader(header, destination, payloadBytes);
    public ScheduleControlHeader ReadHeader(ReadOnlySpan<byte> control) => WorkshopScheduleWire.ReadHeader(control);
    public void WriteSchedule(WorkshopSchedule schedule, Span<byte> destination) => WorkshopScheduleWire.WriteSchedule(schedule, destination);
    public WorkshopSchedule ReadSchedule(ReadOnlySpan<byte> source) => WorkshopScheduleWire.ReadSchedule(source);
    public void WriteCommit(ScheduleControlHeader header, WorkshopSchedule schedule, Span<byte> destination) => WorkshopScheduleWire.WriteCommit(header, schedule, destination);
    public WorkshopSchedule ReadCommit(ReadOnlySpan<byte> control, out ScheduleControlHeader header) => WorkshopScheduleWire.ReadCommit(control, out header);
    public int HeaderBytes => WorkshopScheduleWire.HeaderBytes;
    public int ScheduleBytes => WorkshopScheduleWire.ScheduleBytes;
    public int PrepareFieldBytes => WorkshopScheduleWire.PrepareFieldBytes;
    public int ResultBytes => WorkshopScheduleWire.ResultBytes;
    public int AbortBytes => WorkshopScheduleWire.AbortBytes;
}
