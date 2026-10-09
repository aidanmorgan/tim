using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

public readonly record struct PoseRingBody(
    GpuBodyId Id,
    float Px, float Py, float Pz,
    float Qx, float Qy, float Qz, float Qw,
    float Vx, float Vy, float Vz,
    uint Flags);

public readonly record struct PoseRingSlot(
    ulong Sequence,
    long TimestampNanoseconds,
    int BodyCount,
    PoseRingBody[] Bodies);

/// <summary>
/// Lock-free zero-copy triple-buffered SharedArrayBuffer pose ring layout and codecs.
/// 3 slots x 784 bytes = 2352 bytes. Each slot has a 16-byte header followed by 16 48-byte body slots.
/// </summary>
public static class WorkshopPoseRing
{
    public const int SlotCount = 3;
    public const int BodyCapacity = 16;
    public const int HeaderBytes = 16;
    public const int BodyBytes = 48; // 3 x 16-byte SIMD vectors = 12 floats / uint32s
    public const int SlotBytes = HeaderBytes + BodyCapacity * BodyBytes; // 784 bytes
    public const int TotalBytes = SlotCount * SlotBytes; // 2352 bytes

    public static int SlotOffset(int slot)
    {
        if ((uint)slot >= (uint)SlotCount) throw new ArgumentOutOfRangeException(nameof(slot));
        return slot * SlotBytes;
    }

    public static int SequenceOffset(int slot) => SlotOffset(slot);
    public static int TimestampOffset(int slot) => SlotOffset(slot) + 8;
    public static int BodyOffset(int slot, int bodyIndex)
    {
        if ((uint)bodyIndex >= (uint)BodyCapacity) throw new ArgumentOutOfRangeException(nameof(bodyIndex));
        return SlotOffset(slot) + HeaderBytes + bodyIndex * BodyBytes;
    }

    public static void WriteSlot(Span<byte> ring, int slot, ulong sequence, long timestampNanoseconds, PhysicsBodyReadSet bodies)
    {
        if (ring.Length < TotalBytes) throw new ArgumentException("Ring buffer width insufficient.");
        var slotSpan = ring.Slice(SlotOffset(slot), SlotBytes);

        // Write header
        BinaryPrimitives.WriteUInt64LittleEndian(slotSpan, sequence);
        BinaryPrimitives.WriteInt64LittleEndian(slotSpan[8..], timestampNanoseconds);

        // Write bodies
        var bodyData = slotSpan[HeaderBytes..];
        bodyData.Clear();
        var count = Math.Min((int)bodies.Count, BodyCapacity);
        for (var i = 0; i < count; i++)
        {
            var read = bodies[i];
            var dest = bodyData.Slice(i * BodyBytes, BodyBytes);
            // Vector 0: px, py, pz, bodyId
            var cell = read.Body.Cell;
            var local = read.Body.Local;
            var px = (cell.X + (float)local.X) / 16.0f;
            var py = (cell.Y + (float)local.Y) / 16.0f;
            var pz = (cell.Z + (float)local.Z) / 16.0f;
            BinaryPrimitives.WriteSingleLittleEndian(dest[0..], px);
            BinaryPrimitives.WriteSingleLittleEndian(dest[4..], py);
            BinaryPrimitives.WriteSingleLittleEndian(dest[8..], pz);
            BinaryPrimitives.WriteUInt32LittleEndian(dest[12..], (uint)read.Body.Id.Value);

            // Vector 1: qx, qy, qz, qw
            BinaryPrimitives.WriteSingleLittleEndian(dest[16..], (float)read.Rotation.X);
            BinaryPrimitives.WriteSingleLittleEndian(dest[20..], (float)read.Rotation.Y);
            BinaryPrimitives.WriteSingleLittleEndian(dest[24..], (float)read.Rotation.Z);
            BinaryPrimitives.WriteSingleLittleEndian(dest[28..], (float)read.Rotation.W);

            // Vector 2: vx, vy, vz, flags
            BinaryPrimitives.WriteSingleLittleEndian(dest[32..], read.Body.Velocity.X);
            BinaryPrimitives.WriteSingleLittleEndian(dest[36..], read.Body.Velocity.Y);
            BinaryPrimitives.WriteSingleLittleEndian(dest[40..], read.Body.Velocity.Z);
            BinaryPrimitives.WriteUInt32LittleEndian(dest[44..], 1u); // active flag
        }
    }

    public static bool TryReadSlot(ReadOnlySpan<byte> ring, int slot, out ulong sequence, out long timestampNanoseconds, Span<PoseRingBody> bodies, out int bodyCount)
    {
        sequence = 0;
        timestampNanoseconds = 0;
        bodyCount = 0;
        if (ring.Length < TotalBytes || (uint)slot >= (uint)SlotCount) return false;

        var slotSpan = ring.Slice(SlotOffset(slot), SlotBytes);
        var seq1 = BinaryPrimitives.ReadUInt64LittleEndian(slotSpan);
        if ((seq1 & 1UL) != 0UL) return false; // Write in progress

        var timestamp = BinaryPrimitives.ReadInt64LittleEndian(slotSpan[8..]);
        var bodyData = slotSpan[HeaderBytes..];
        var count = 0;
        for (var i = 0; i < BodyCapacity && count < bodies.Length; i++)
        {
            var src = bodyData.Slice(i * BodyBytes, BodyBytes);
            var bodyId = BinaryPrimitives.ReadUInt32LittleEndian(src[12..]);
            var flags = BinaryPrimitives.ReadUInt32LittleEndian(src[44..]);
            if (bodyId == 0 || (flags & 1u) == 0) continue;

            bodies[count++] = new PoseRingBody(
                new GpuBodyId(bodyId),
                BinaryPrimitives.ReadSingleLittleEndian(src[0..]),
                BinaryPrimitives.ReadSingleLittleEndian(src[4..]),
                BinaryPrimitives.ReadSingleLittleEndian(src[8..]),
                BinaryPrimitives.ReadSingleLittleEndian(src[16..]),
                BinaryPrimitives.ReadSingleLittleEndian(src[20..]),
                BinaryPrimitives.ReadSingleLittleEndian(src[24..]),
                BinaryPrimitives.ReadSingleLittleEndian(src[28..]),
                BinaryPrimitives.ReadSingleLittleEndian(src[32..]),
                BinaryPrimitives.ReadSingleLittleEndian(src[36..]),
                BinaryPrimitives.ReadSingleLittleEndian(src[40..]),
                flags);
        }

        var seq2 = BinaryPrimitives.ReadUInt64LittleEndian(slotSpan);
        if (seq1 != seq2) return false; // Raced during read

        sequence = seq2;
        timestampNanoseconds = timestamp;
        bodyCount = count;
        return true;
    }
}
