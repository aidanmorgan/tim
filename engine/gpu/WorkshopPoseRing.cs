using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

public enum PosePublicationPreparation { Ready = 0, StorageUnavailable = 1 }

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
/// Lock-free zero-copy triple-buffered SharedArrayBuffer pose ring layout and codecs. Each of the three slots has a 16-byte header
/// followed by one 64-byte record per moving body. Story 9R.5b: the ring's body capacity is its own length (no fixed capacity);
/// bootstrap reserves sixteen bodies. Installation prepares default-or-grown backing before commit; logical population remains actual.
/// </summary>
public static class WorkshopPoseRing
{
    /// <summary>Triple buffering (AGENTS.md: "triple-buffered pose ring"): structural, not a population.</summary>
    public const int SlotCount = 3;
    public const int DefaultBodyCapacity = 16;
    public const int HeaderBytes = 16;
    public const int BodyBytes = 64; // four SIMD vectors; full body identity at byte48

    public static int Capacity(int bodies)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bodies);
        var limit = (Array.MaxLength / SlotCount - HeaderBytes) / BodyBytes;
        if (bodies > limit) throw new WorkshopStorageException("Pose storage is not representable.");
        if (bodies <= 3 * DefaultBodyCapacity / 4) return DefaultBodyCapacity;
        return (int)Math.Min(limit, Math.Max(2L * DefaultBodyCapacity, (4L * bodies + 2) / 3));
    }

    public static int SlotBytes(int bodyCapacity) => bodyCapacity >= 0
        ? checked(HeaderBytes + bodyCapacity * BodyBytes)
        : throw new ArgumentOutOfRangeException(nameof(bodyCapacity));
    public static int TotalBytes(int bodyCapacity) => checked(SlotCount * SlotBytes(bodyCapacity));

    /// <summary>The body capacity a ring of this length holds; a length that is not a whole ring rejects.</summary>
    public static int BodyCapacity(int ringBytes)
    {
        if (ringBytes < SlotCount * HeaderBytes || ringBytes % SlotCount != 0 || (ringBytes / SlotCount - HeaderBytes) % BodyBytes != 0)
            throw new ArgumentException("A pose ring is not a whole number of slots and body records.");
        return (ringBytes / SlotCount - HeaderBytes) / BodyBytes;
    }

    public static int SlotOffset(int slot, int bodyCapacity)
    {
        if ((uint)slot >= (uint)SlotCount) throw new ArgumentOutOfRangeException(nameof(slot));
        return slot * SlotBytes(bodyCapacity);
    }

    public static int SequenceOffset(int slot, int bodyCapacity) => SlotOffset(slot, bodyCapacity);
    public static int TimestampOffset(int slot, int bodyCapacity) => SlotOffset(slot, bodyCapacity) + 8;
    public static int BodyOffset(int slot, int bodyIndex, int bodyCapacity)
    {
        if ((uint)bodyIndex >= (uint)bodyCapacity) throw new ArgumentOutOfRangeException(nameof(bodyIndex));
        return SlotOffset(slot, bodyCapacity) + HeaderBytes + bodyIndex * BodyBytes;
    }

    /// <summary>One committed read's slot, composed in C# (Story 9A.2): the 16-byte header (sequence 0, the read's capture time in
    /// nanoseconds) and one record per committed body. The Simulation worker copies it into the ring verbatim under its sequence
    /// lock into the already admitted epoch storage; no script reads a body layout.</summary>
    public static byte[] ComposeSlot(long timestampNanoseconds, PhysicsBodyReadSet bodies)
    {
        var slot = new byte[SlotBytes(bodies.Count)];
        BinaryPrimitives.WriteInt64LittleEndian(slot.AsSpan(8), timestampNanoseconds);
        WriteBodies(slot.AsSpan(HeaderBytes), bodies);
        return slot;
    }

    private static void WriteBodies(Span<byte> bodyData, PhysicsBodyReadSet bodies)
    {
        bodyData.Clear();
        for (var i = 0; i < bodies.Count; i++)
        {
            var read = bodies[i];
            var dest = bodyData.Slice(i * BodyBytes, BodyBytes);
            // Vector 0: px, py, pz, padding; full UInt64 identity in vector3.
            var cell = read.Body.Cell;
            var local = read.Body.Local;
            var px = (cell.X + (float)local.X) / 16.0f;
            var py = (cell.Y + (float)local.Y) / 16.0f;
            var pz = (cell.Z + (float)local.Z) / 16.0f;
            BinaryPrimitives.WriteSingleLittleEndian(dest[0..], px);
            BinaryPrimitives.WriteSingleLittleEndian(dest[4..], py);
            BinaryPrimitives.WriteSingleLittleEndian(dest[8..], pz);
            BinaryPrimitives.WriteUInt64LittleEndian(dest[48..], read.Body.Id.Value);

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
        if ((uint)slot >= (uint)SlotCount) return false;
        int capacity;
        try { capacity = BodyCapacity(ring.Length); }
        catch (ArgumentException) { return false; }

        var slotSpan = ring.Slice(SlotOffset(slot, capacity), SlotBytes(capacity));
        var seq1 = BinaryPrimitives.ReadUInt64LittleEndian(slotSpan);
        if ((seq1 & 1UL) != 0UL) return false; // Write in progress

        var timestamp = BinaryPrimitives.ReadInt64LittleEndian(slotSpan[8..]);
        var bodyData = slotSpan[HeaderBytes..];
        var count = 0;
        for (var i = 0; i < capacity && count < bodies.Length; i++)
        {
            var src = bodyData.Slice(i * BodyBytes, BodyBytes);
            var bodyId = BinaryPrimitives.ReadUInt64LittleEndian(src[48..]);
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
