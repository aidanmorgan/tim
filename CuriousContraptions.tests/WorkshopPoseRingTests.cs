using System;
using CuriousContraptions.Gpu;
using Xunit;

namespace CuriousContraptions.tests;

public class WorkshopPoseRingTests
{
    /// <summary>The Simulation worker's publication of one C#-composed slot (worker.js writePoseRing): the ring must already hold
    /// the read's bodies (installation prepares reserved demand first), the slot is copied verbatim after its sequence word, and the records past
    /// the read are cleared.</summary>
    private static void Publish(byte[] ring, int slot, ulong sequence, long timestamp, PhysicsBodyReadSet bodies)
    {
        var capacity = WorkshopPoseRing.BodyCapacity(ring.Length);
        var composed = WorkshopPoseRing.ComposeSlot(timestamp, bodies);
        if (composed.Length > WorkshopPoseRing.SlotBytes(capacity)) throw new ArgumentException("The read exceeds admitted pose storage.");
        var target = ring.AsSpan(WorkshopPoseRing.SlotOffset(slot, capacity), WorkshopPoseRing.SlotBytes(capacity));
        target.Clear();
        composed.AsSpan(sizeof(ulong)).CopyTo(target[sizeof(ulong)..]);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(target, sequence);
    }

    [Theory]
    [InlineData(0, 16)]
    [InlineData(12, 16)]
    [InlineData(13, 32)]
    [InlineData(40, 54)]
    public void PoseReserveUsesCommonDefaultWatermarkAndBulkHeadroom(int bodies, int capacity) =>
        Assert.Equal(capacity, WorkshopPoseRing.Capacity(bodies));

    [Fact]
    public void PoseReserveSaturatesAtRepresentableStorageAndRefusesExcessWithoutAllocation()
    {
        var limit = (Array.MaxLength / 3 - 16) / 64;
        Assert.Equal(limit, WorkshopPoseRing.Capacity(limit - 1));
        Assert.Equal(limit, WorkshopPoseRing.Capacity(limit));
        Assert.Throws<WorkshopStorageException>(() => WorkshopPoseRing.Capacity(limit + 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => WorkshopPoseRing.Capacity(-1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(17)]
    [InlineData(200)]
    public void PoseRingLayout_MatchesSpecificationAndSimdAlignment(int capacity)
    {
        Assert.Equal(3, WorkshopPoseRing.SlotCount);
        Assert.Equal(16, WorkshopPoseRing.HeaderBytes);
        Assert.Equal(64, WorkshopPoseRing.BodyBytes);
        Assert.Equal(16 + capacity * 64, WorkshopPoseRing.SlotBytes(capacity));
        Assert.Equal(3 * (16 + capacity * 64), WorkshopPoseRing.TotalBytes(capacity));
        // Story 9R.5b: a ring's capacity is its own length.
        Assert.Equal(capacity, WorkshopPoseRing.BodyCapacity(WorkshopPoseRing.TotalBytes(capacity)));

        // Every slot start must be 16-byte aligned (and 8-byte aligned for 64-bit Atomics)
        for (var s = 0; s < WorkshopPoseRing.SlotCount; s++)
        {
            var slotOffset = WorkshopPoseRing.SlotOffset(s, capacity);
            Assert.Equal(0, slotOffset % 16);
            Assert.Equal(0, slotOffset % 8);
            Assert.Equal(slotOffset, WorkshopPoseRing.SequenceOffset(s, capacity));
            Assert.Equal(slotOffset + 8, WorkshopPoseRing.TimestampOffset(s, capacity));

            // Every body record must be 16-byte aligned for 128-bit SIMD stores
            for (var b = 0; b < capacity; b++)
                Assert.Equal(0, WorkshopPoseRing.BodyOffset(s, b, capacity) % 16);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(47)]
    [InlineData(3 * 16 + 1)]
    [InlineData(3 * (16 + 64) + 3)]
    public void APoseRingThatIsNotWholeSlotsRejects(int bytes) =>
        Assert.Throws<ArgumentException>(() => WorkshopPoseRing.BodyCapacity(bytes));

    [Theory]
    [InlineData(4294967296UL)]
    [InlineData(18446744073709551615UL)]
    public void PoseRing_WriteAndReadSlot_SingleAndDualBodies(ulong secondaryId)
    {
        var ring = new byte[WorkshopPoseRing.TotalBytes(2)];

        var body1 = new CanonicalBody(new GpuBodyId(101), 1, 10, new CellOrigin(16, 32, 0),
            new LocalPosition((float)0.25, (float)(-0.125), (float)0.0),
            new LinearVelocity(16.0001f, -8f, 0f));
        var rot1 = CanonicalRotation.Identity;
        var read1 = new PhysicsBodyRead(body1, rot1, default, default);

        var body2 = new CanonicalBody(new GpuBodyId(secondaryId), 1, 10, new CellOrigin(-16, 0, 16),
            new LocalPosition((float)(-0.25), (float)0.125, (float)0.0),
            new LinearVelocity(-16f, 8f, 0f));
        var rot2 = CanonicalRotation.Normalize(0, MathF.Sqrt(.5f), 0, MathF.Sqrt(.5f));
        var read2 = new PhysicsBodyRead(body2, rot2, default, default);

        var bodySet = new PhysicsBodyReadSet([read1, read2]);
        var sequence = 42UL;
        var timestampNs = 123456789L;

        Publish(ring, 1, sequence, timestampNs, bodySet);

        Span<PoseRingBody> bodies = stackalloc PoseRingBody[16];
        var success = WorkshopPoseRing.TryReadSlot(ring, 1, out var readSeq, out var readTimestamp, bodies, out var readCount);

        Assert.True(success);
        Assert.Equal(sequence, readSeq);
        Assert.Equal(timestampNs, readTimestamp);
        Assert.Equal(2, readCount);

        // Verify body 1
        Assert.Equal(new GpuBodyId(101), bodies[0].Id);
        Assert.Equal((16 + 0.25f) / 16.0f, bodies[0].Px, precision: 4);
        Assert.Equal((32 - 0.125f) / 16.0f, bodies[0].Py, precision: 4);
        Assert.Equal(0.0f, bodies[0].Pz, precision: 4);
        Assert.Equal(0.0f, bodies[0].Qx, precision: 4);
        Assert.Equal(1.0f, bodies[0].Qw, precision: 4);
        // The committed f32 m/s value passes to the ring unscaled and bit-exact.
        Assert.Equal(16.0001f, bodies[0].Vx);
        Assert.Equal(-8f, bodies[0].Vy);
        Assert.Equal(1u, bodies[0].Flags);

        // Verify body 2
        Assert.Equal(new GpuBodyId(secondaryId), bodies[1].Id);
        Assert.Equal((-16 - 0.25f) / 16.0f, bodies[1].Px, precision: 4);
        Assert.Equal(0.125f / 16.0f, bodies[1].Py, precision: 4);
        Assert.Equal((16 + 0.0f) / 16.0f, bodies[1].Pz, precision: 4);
        Assert.Equal(0.7071f, bodies[1].Qy, precision: 3);
        Assert.Equal(0.7071f, bodies[1].Qw, precision: 3);
        Assert.Equal(-16f, bodies[1].Vx);
        Assert.Equal(1u, bodies[1].Flags);
    }

    /// <summary>Story 9R.5b: a ring sized for seventeen moving bodies (one past the retired 16) holds every one of them; a ring too
    /// small for a body set refuses the write rather than truncating it.</summary>
    [Fact]
    public void ARingSizedPastTheRetiredCapacityHoldsEveryBodyAndASmallRingRefuses()
    {
        var bodies = new PhysicsBodyRead[17];
        for (var i = 0; i < bodies.Length; i++)
            bodies[i] = new(new CanonicalBody(new GpuBodyId((ulong)i + 1), 1, 1, new CellOrigin(i, 0, 0), default, default),
                CanonicalRotation.Identity, default, default);
        var set = new PhysicsBodyReadSet(bodies);
        var ring = new byte[WorkshopPoseRing.TotalBytes(17)];
        Publish(ring, 2, 8UL, 5L, set);
        Span<PoseRingBody> read = stackalloc PoseRingBody[17];
        Assert.True(WorkshopPoseRing.TryReadSlot(ring, 2, out _, out _, read, out var count));
        Assert.Equal(17, count);
        Assert.Equal(new GpuBodyId(17), read[16].Id);
        Assert.Throws<ArgumentException>(() =>
            Publish(new byte[WorkshopPoseRing.TotalBytes(16)], 0, 8UL, 5L, set));
    }

    [Fact]
    public void PoseRing_RejectsOddSequence_IndicatingWriteInProgress()
    {
        var ring = new byte[WorkshopPoseRing.TotalBytes(0)];
        var bodySet = new PhysicsBodyReadSet([]);
        // Odd sequence means write in progress (Writer Protocol: 2k + 1)
        Publish(ring, 0, 7UL, 1000L, bodySet);

        Span<PoseRingBody> bodies = stackalloc PoseRingBody[16];
        var success = WorkshopPoseRing.TryReadSlot(ring, 0, out _, out _, bodies, out _);

        Assert.False(success);
    }
}
