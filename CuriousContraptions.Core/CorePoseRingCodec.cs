using System;
using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Runtime;

public sealed class CorePoseRingCodec : IPoseRingCodec
{
    public int Capacity(int bodies) => WorkshopPoseRing.Capacity(bodies);
    public int SlotBytes(int bodyCapacity) => WorkshopPoseRing.SlotBytes(bodyCapacity);
    public int TotalBytes(int bodyCapacity) => WorkshopPoseRing.TotalBytes(bodyCapacity);
    public int BodyCapacity(int ringBytes) => WorkshopPoseRing.BodyCapacity(ringBytes);
    public int SlotOffset(int slot, int bodyCapacity) => WorkshopPoseRing.SlotOffset(slot, bodyCapacity);
    public int SequenceOffset(int slot, int bodyCapacity) => WorkshopPoseRing.SequenceOffset(slot, bodyCapacity);
    public int TimestampOffset(int slot, int bodyCapacity) => WorkshopPoseRing.TimestampOffset(slot, bodyCapacity);
    public int BodyOffset(int slot, int bodyIndex, int bodyCapacity) => WorkshopPoseRing.BodyOffset(slot, bodyIndex, bodyCapacity);
    public byte[] ComposeSlot(long timestampNanoseconds, PhysicsBodyReadSet bodies) => WorkshopPoseRing.ComposeSlot(timestampNanoseconds, bodies);
    public bool TryReadSlot(ReadOnlySpan<byte> ring, int slot, out ulong sequence, out long timestampNanoseconds, Span<PoseRingBody> bodies, out int bodyCount) => WorkshopPoseRing.TryReadSlot(ring, slot, out sequence, out timestampNanoseconds, bodies, out bodyCount);
    public int SlotCount => WorkshopPoseRing.SlotCount;
    public int DefaultBodyCapacity => WorkshopPoseRing.DefaultBodyCapacity;
    public int HeaderBytes => WorkshopPoseRing.HeaderBytes;
    public int BodyBytes => WorkshopPoseRing.BodyBytes;
}
