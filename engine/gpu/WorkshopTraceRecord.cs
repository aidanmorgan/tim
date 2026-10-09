using System;
using System.Buffers.Binary;
namespace CuriousContraptions.Gpu;

/// <summary>Trace schema. 3: every committed body as the 64-byte <see cref="PhysicsBodyWire"/> with f32 velocity and angular velocity
/// (Story 6.1c); the 56-byte binary16-velocity schema 2 is rejected like any unknown version.</summary>
public enum WorkshopTraceVersion : uint { CompleteBodySetF32Velocity = 3 }

/// <summary>Bounded observational projection of every committed body and capture latch.</summary>
public static class WorkshopTraceRecord
{
    public const int HeaderBytes = 48;
    public const int CaptureBytes = 24;
    public static int ByteLength(int bodies, int captures)
    {
        if (bodies is < 0 or > PhysicsBodyReadSet.Capacity || captures < 0 || captures > PhysicsSceneDeclaration.SensorCapacity)
            throw new ArgumentException("Unsupported diagnostic population.");
        return HeaderBytes + bodies * PhysicsBodyWire.ByteLength + captures * CaptureBytes;
    }

    public static byte[] Encode(WorkshopRead read, WorkshopGpuProfile profile)
    {
        profile.Validate();
        if (read.Epoch.Value == 0 || read.Tick.Value > profile.RunTickLimit)
            throw new ArgumentException("Unsupported diagnostic time.");
        read.Bodies.ValidateTime(read.Epoch, read.Tick);
        var b = new byte[ByteLength(read.Bodies.Count, read.Captures.Count)];
        U32(b,0,(uint)WorkshopTraceVersion.CompleteBodySetF32Velocity); U32(b,4,(uint)profile.Cadence);
        U64(b,8,profile.Revision.Value); U64(b,16,read.Epoch.Value); U64(b,24,read.Tick.Value);
        U32(b,32,(uint)profile.Physical); b[36]=read.Bodies.Count; b[37]=read.Captures.Count;
        for (var i=0; i<read.Bodies.Count; i++)
            PhysicsBodyWire.Write(read.Bodies[i], b.AsSpan(HeaderBytes+i*PhysicsBodyWire.ByteLength,PhysicsBodyWire.ByteLength));
        var offset=HeaderBytes+read.Bodies.Count*PhysicsBodyWire.ByteLength;
        for (var i=0; i<read.Captures.Count; i++)
        {
            var c=read.Captures[i]; c.Validate();
            U64(b,offset,c.Sensor.Value); U32(b,offset+8,(uint)c.Phase);
            U32(b,offset+12,c.EventOrdinal); H(b,offset+16,c.EventPhase);
            offset+=CaptureBytes;
        }
        return b;
    }

    public static WorkshopGpuProfile ReadProfile(ReadOnlySpan<byte> b)
    {
        if(b.Length<HeaderBytes || BinaryPrimitives.ReadUInt32LittleEndian(b)!=(uint)WorkshopTraceVersion.CompleteBodySetF32Velocity ||
            b.Length!=ByteLength(b[36],b[37]) || b[38..HeaderBytes].IndexOfAnyExcept((byte)0)>=0)
            throw new ArgumentException("Unsupported diagnostic record.");
        var p=new WorkshopGpuProfile((SimulationCadence)BinaryPrimitives.ReadUInt32LittleEndian(b[4..]),
            (PhysicalStepProfile)BinaryPrimitives.ReadUInt32LittleEndian(b[32..]),
            new(BinaryPrimitives.ReadUInt64LittleEndian(b[8..])));
        p.Validate(); return p;
    }
    private static void U32(Span<byte>b,int o,uint v)=>BinaryPrimitives.WriteUInt32LittleEndian(b[o..],v);
    private static void U64(Span<byte>b,int o,ulong v)=>BinaryPrimitives.WriteUInt64LittleEndian(b[o..],v);
    private static void H(Span<byte>b,int o,Half v)=>BinaryPrimitives.WriteUInt16LittleEndian(b[o..],BitConverter.HalfToUInt16Bits(v));
}
