using System;
using System.Buffers.Binary;
namespace CuriousContraptions.Gpu;

public enum WorkshopTraceVersion : uint { GenericPoseAndCapture = 1 }

/// <summary>Diagnostic projection of the current Workshop's one body/one sensor composition.</summary>
public static class WorkshopTraceRecord
{
    public const int ByteLength = 128;
    public static byte[] Encode(WorkshopRead read, WorkshopGpuProfile profile)
    {
        profile.Validate();
        if (read.Epoch.Value == 0 || read.Tick.Value > profile.RunTickLimit || read.Captures.Count > 1 ||
            read.Ball.HasValue != read.Rotation.HasValue)
            throw new ArgumentException("Unsupported diagnostic composition.");
        read.Ball?.Validate(); read.Rotation?.Validate();
        PhysicsDeclarationBounds.Vector(read.Angular.X,read.Angular.Y,read.Angular.Z,(Half)64);
        if (read.Ball is { } body && (body.Epoch != read.Epoch.Value || body.Tick != read.Tick.Value))
            throw new ArgumentException("Diagnostic body does not own its read.");
        if (read.Captures.Count == 1) read.Captures[0].Validate();
        var b=new byte[ByteLength];
        U32(b,0,(uint)WorkshopTraceVersion.GenericPoseAndCapture);U32(b,4,(uint)profile.Cadence);
        U64(b,8,profile.Revision.Value);U64(b,16,read.Epoch.Value);U64(b,24,read.Tick.Value);
        U32(b,32,(uint)profile.Physical);U32(b,36,read.Ball.HasValue?1u:0u);
        if(read.Ball is { } physical)
        {
            U64(b,40,physical.Id.Value);
            I32(b,48,physical.Cell.X);I32(b,52,physical.Cell.Y);I32(b,56,physical.Cell.Z);
            H(b,64,physical.Local.X);H(b,66,physical.Local.Y);H(b,68,physical.Local.Z);
            H(b,72,physical.Velocity.X);H(b,74,physical.Velocity.Y);H(b,76,physical.Velocity.Z);
            var q=read.Rotation!.Value;H(b,80,q.X);H(b,82,q.Y);H(b,84,q.Z);H(b,86,q.W);
            H(b,88,read.Angular.X);H(b,90,read.Angular.Y);H(b,92,read.Angular.Z);
        }
        U32(b,60,read.Captures.Count);
        if(read.Captures.Count==1)
        {
            var c=read.Captures[0];U64(b,96,c.Sensor.Value);U32(b,104,(uint)c.Phase);
            U32(b,108,c.EventOrdinal);H(b,112,c.EventPhase);
        }
        return b;
    }
    public static WorkshopGpuProfile ReadProfile(ReadOnlySpan<byte> b)
    {
        if(b.Length!=ByteLength||BinaryPrimitives.ReadUInt32LittleEndian(b)!=(uint)WorkshopTraceVersion.GenericPoseAndCapture)
            throw new ArgumentException("Unsupported diagnostic record.");
        var p=new WorkshopGpuProfile((SimulationCadence)BinaryPrimitives.ReadUInt32LittleEndian(b[4..]),
            (PhysicalStepProfile)BinaryPrimitives.ReadUInt32LittleEndian(b[32..]),
            new(BinaryPrimitives.ReadUInt64LittleEndian(b[8..])));
        p.Validate();return p;
    }
    private static void U32(Span<byte>b,int o,uint v)=>BinaryPrimitives.WriteUInt32LittleEndian(b[o..],v);
    private static void U64(Span<byte>b,int o,ulong v)=>BinaryPrimitives.WriteUInt64LittleEndian(b[o..],v);
    private static void I32(Span<byte>b,int o,int v)=>BinaryPrimitives.WriteInt32LittleEndian(b[o..],v);
    private static void H(Span<byte>b,int o,Half v)=>BinaryPrimitives.WriteUInt16LittleEndian(b[o..],BitConverter.HalfToUInt16Bits(v));
}
