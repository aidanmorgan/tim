using System;
using CuriousContraptions.Gpu;
using Xunit;

namespace CuriousContraptions.tests;

public class WorkshopPoseRingTests
{
    [Fact]
    public void PoseRingLayout_MatchesSpecificationAndSimdAlignment()
    {
        Assert.Equal(3, WorkshopPoseRing.SlotCount);
        Assert.Equal(16, WorkshopPoseRing.BodyCapacity);
        Assert.Equal(16, WorkshopPoseRing.HeaderBytes);
        Assert.Equal(48, WorkshopPoseRing.BodyBytes);
        Assert.Equal(784, WorkshopPoseRing.SlotBytes);
        Assert.Equal(2352, WorkshopPoseRing.TotalBytes);

        // Every slot start must be 16-byte aligned (and 8-byte aligned for 64-bit Atomics)
        for (var s = 0; s < WorkshopPoseRing.SlotCount; s++)
        {
            var slotOffset = WorkshopPoseRing.SlotOffset(s);
            Assert.Equal(0, slotOffset % 16);
            Assert.Equal(0, slotOffset % 8);
            Assert.Equal(slotOffset, WorkshopPoseRing.SequenceOffset(s));
            Assert.Equal(slotOffset + 8, WorkshopPoseRing.TimestampOffset(s));

            // Every body record must be 16-byte aligned for 128-bit SIMD stores
            for (var b = 0; b < WorkshopPoseRing.BodyCapacity; b++)
            {
                var bodyOffset = WorkshopPoseRing.BodyOffset(s, b);
                Assert.Equal(0, bodyOffset % 16);
            }
        }
    }

    [Fact]
    public void PoseRing_WriteAndReadSlot_SingleAndDualBodies()
    {
        var ring = new byte[WorkshopPoseRing.TotalBytes];
        var epoch = new SimulationEpoch(1);
        var tick = new SimulationTick(10);

        var body1 = new CanonicalBody(new GpuBodyId(101), 1, 10, new CellOrigin(16, 32, 0),
            new LocalPosition((Half)0.25, (Half)(-0.125), (Half)0.0),
            new CellVelocity((Half)0.5, (Half)(-0.25), (Half)0.0));
        var rot1 = CanonicalRotation.Identity;
        var read1 = new PhysicsBodyRead(body1, rot1, default, default);

        var body2 = new CanonicalBody(new GpuBodyId(102), 1, 10, new CellOrigin(-16, 0, 16),
            new LocalPosition((Half)(-0.25), (Half)0.125, (Half)0.0),
            new CellVelocity((Half)(-0.5), (Half)0.25, (Half)0.0));
        var rot2 = new CanonicalRotation((Half)0.0, (Half)0.7071, (Half)0.0, (Half)0.7071);
        var read2 = new PhysicsBodyRead(body2, rot2, default, default);

        var bodySet = new PhysicsBodyReadSet([read1, read2]);
        var sequence = 42UL;
        var timestampNs = 123456789L;

        WorkshopPoseRing.WriteSlot(ring, 1, sequence, timestampNs, bodySet);

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
        Assert.Equal(0.5f * 32.0f, bodies[0].Vx, precision: 4);
        Assert.Equal(1u, bodies[0].Flags);

        // Verify body 2
        Assert.Equal(new GpuBodyId(102), bodies[1].Id);
        Assert.Equal((-16 - 0.25f) / 16.0f, bodies[1].Px, precision: 4);
        Assert.Equal(0.125f / 16.0f, bodies[1].Py, precision: 4);
        Assert.Equal((16 + 0.0f) / 16.0f, bodies[1].Pz, precision: 4);
        Assert.Equal(0.7071f, bodies[1].Qy, precision: 3);
        Assert.Equal(0.7071f, bodies[1].Qw, precision: 3);
        Assert.Equal(-0.5f * 32.0f, bodies[1].Vx, precision: 4);
        Assert.Equal(1u, bodies[1].Flags);
    }

    [Fact]
    public void PoseRing_RejectsOddSequence_IndicatingWriteInProgress()
    {
        var ring = new byte[WorkshopPoseRing.TotalBytes];
        var bodySet = new PhysicsBodyReadSet([]);
        // Odd sequence means write in progress (Writer Protocol: 2k + 1)
        WorkshopPoseRing.WriteSlot(ring, 0, 7UL, 1000L, bodySet);

        Span<PoseRingBody> bodies = stackalloc PoseRingBody[16];
        var success = WorkshopPoseRing.TryReadSlot(ring, 0, out _, out _, bodies, out _);

        Assert.False(success);
    }
}
