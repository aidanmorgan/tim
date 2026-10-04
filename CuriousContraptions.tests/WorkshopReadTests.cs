using System.Buffers.Binary;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class WorkshopReadTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void IndependentlyDecodedReadsDeduplicateByExactContent(bool bodyPresent, bool captured)
    {
        var body = bodyPresent ? new CanonicalBody(new(1), 2, 0, default, default, default) : (CanonicalBody?)null;
        var read = new WorkshopRead(new(2), new(0), body, new(4),
            new(WorkshopClockDomain.SimulationMonotonic, new(1), new(100_000_000), new(100_000)),
            bodyPresent ? new CanonicalRotation((Half)0, (Half)0, (Half)0, (Half)1) : null,
            Captures: captured ? new PhysicsCaptureRead([CaptureLatch.Clear(new(9))]) : default);
        var bytes = WorkshopWire.Encode(Response(read));
        var first = WorkshopWire.DecodeResponse(bytes).Read;
        var second = WorkshopWire.DecodeResponse(bytes).Read;
        Assert.NotSame(first.Motion, second.Motion);
        Assert.True(first.HasSameContent(second));
        Assert.True(first == second); // Generated record dispatch must not enter InlineArray.Equals.
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.False(first.HasSameContent(second with { Revision = new(5) }));
        Assert.False(first.HasSameContent(second with { Capture = second.Capture!.Value with { Time = new(100_000_001) } }));
        if (bodyPresent)
        {
            var signedZero = second.Ball!.Value with { Local = new(BitConverter.UInt16BitsToHalf(0x8000), (Half)0, (Half)0) };
            Assert.False(first.HasSameContent(second with { Ball = signedZero }));
        }
        if (captured)
        {
            var changed = second with { Captures = new([CaptureLatch.Clear(new(10))]) };
            Assert.False(first.HasSameContent(changed));
        }
    }

    [Fact]
    public void MotionAndOccurrenceChangesAreNotDuplicateAcknowledgements()
    {
        var body = new CanonicalBody(new(1), 2, 1, default, default, default);
        var motion = new byte[PhysicsMotionRead.ByteLength];
        U32(motion, 0, 1); U32(motion, 4, 4); U32(motion, 12, 4);
        var piece = PhysicsMotionRead.HeaderBytes;
        U32(motion, piece, (uint)PhysicsMotionKind.FreePolynomial);
        U32(motion, piece + 8, 4);
        H(motion, piece + 22, (Half)WorkshopCadenceSettings.PhysicalFrequency);
        BinaryPrimitives.WriteUInt64LittleEndian(motion.AsSpan(piece + 24), body.Id.Value);
        H(motion, piece + 62, (Half)1);
        var read = new WorkshopRead(new(2), new(1), body, new(4),
            new(WorkshopClockDomain.SimulationMonotonic, new(1), new(100_000_000), new(100_000)),
            new((Half)0, (Half)0, (Half)0, (Half)1), Captures: new([new(new(9), CaptureLatchPhase.Latched, 1, (Half)0)]),
            Motion: PhysicsMotionRead.Decode(motion, body, new(1)));
        var encoded = WorkshopWire.Encode(Response(read));
        var first = WorkshopWire.DecodeResponse(encoded).Read;
        var second = WorkshopWire.DecodeResponse(encoded).Read;
        Assert.True(first.HasSameContent(second));
        var changed = (byte[])encoded.Clone();
        H(changed, WorkshopWire.ReadMotionOffset + piece + 54, (Half).125);
        Assert.False(first.HasSameContent(WorkshopWire.DecodeResponse(changed).Read));
        changed = (byte[])encoded.Clone();
        U32(changed, 268, 2);
        Assert.False(first.HasSameContent(WorkshopWire.DecodeResponse(changed).Read));
        changed = (byte[])encoded.Clone();
        H(changed, 272, BitConverter.UInt16BitsToHalf(0x8000));
        Assert.False(first.HasSameContent(WorkshopWire.DecodeResponse(changed).Read));
    }

    [Fact]
    public void MotionUsesExactProfileRateAndRejectsRetiredTiming()
    {
        var body = new CanonicalBody(new(1), 2, 15, default, default, default);
        var bytes = new byte[PhysicsMotionRead.ByteLength];
        U32(bytes, 0, 1); U32(bytes, 4, 4); U32(bytes, 8, 56); U32(bytes, 12, 60);
        var piece = PhysicsMotionRead.HeaderBytes;
        U32(bytes, piece, (uint)PhysicsMotionKind.FreePolynomial);
        U32(bytes, piece + 4, 56); U32(bytes, piece + 8, 60);
        H(bytes, piece + 22, (Half)WorkshopCadenceSettings.PhysicalFrequency);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(piece + 24), body.Id.Value);
        H(bytes, piece + 62, (Half)1); H(bytes, piece + 64, (Half)(1.0 / 32));
        var motion = PhysicsMotionRead.Decode(bytes, body, new(15));
        Assert.True(motion.TrySample(58, out var pose));
        Assert.Equal(2, pose.Cell.X);
        Assert.Equal((Half)(58.0 / WorkshopCadenceSettings.PhysicalFrequency * 16 - 2), pose.Local.X);
        foreach (var invalid in new Half[] { (Half)(1.0 / 480), (Half)240, Half.NaN })
        {
            var changed = (byte[])bytes.Clone(); H(changed, piece + 22, invalid);
            Assert.Throws<ArgumentException>(() => PhysicsMotionRead.Decode(changed, body, new(15)));
        }
        // The next interval must use its newly committed primary anchor.
        U32(bytes, 8, 60); U32(bytes, 12, 64);
        U32(bytes, piece + 4, 60); U32(bytes, piece + 8, 64);
        Assert.Throws<ArgumentException>(() => PhysicsMotionRead.Decode(bytes, body with { Tick = 16 }, new(16)));
    }

    [Fact]
    public void DecodedMotionOwnsValidatedBytesAndPreservesWireContent()
    {
        var body = new CanonicalBody(new(1), 2, 1, default, default, default);
        var bytes = MotionBytes(body);
        var expected = (byte[])bytes.Clone();
        var motion = PhysicsMotionRead.Decode(bytes, body, new(1));
        bytes[^1] = 1; // A reused external input cannot change the validated snapshot.
        Assert.Throws<ArgumentException>(() => PhysicsMotionRead.Decode(bytes, body, new(1)));
        Assert.True(motion.Bytes.SequenceEqual(expected));
        var read = MotionRead(body, motion);
        var encoded = WorkshopWire.Encode(Response(read));
        Assert.True(encoded.AsSpan(WorkshopWire.ReadMotionOffset).SequenceEqual(expected));
        Assert.Equal(encoded, WorkshopWire.Encode(WorkshopWire.DecodeResponse(encoded)));
    }

    public enum MotionBindingMismatch { BodyIdentity, BodyPresence, Tick }

    [Theory]
    [InlineData(MotionBindingMismatch.BodyIdentity)]
    [InlineData(MotionBindingMismatch.BodyPresence)]
    [InlineData(MotionBindingMismatch.Tick)]
    public void WrongMotionBindingRejectsBeforeWriting(MotionBindingMismatch mismatch)
    {
        var body = new CanonicalBody(new(1), 2, 1, default, default, default);
        var motion = PhysicsMotionRead.Decode(MotionBytes(body), body, new(1));
        var read = MotionRead(body, motion);
        read = mismatch switch
        {
            MotionBindingMismatch.BodyIdentity => read with { Ball = body with { Id = new(2) } },
            MotionBindingMismatch.BodyPresence => read with { Ball = null, Rotation = null },
            MotionBindingMismatch.Tick => read with { Tick = new(2), Ball = body with { Tick = 2 } },
            _ => throw new ArgumentOutOfRangeException(nameof(mismatch))
        };
        var destination = Enumerable.Repeat((byte)0xa5, WorkshopWire.ResponseBytes).ToArray();
        Assert.Throws<ArgumentException>(() => WorkshopWire.Write(Response(read), destination));
        Assert.All(destination, value => Assert.Equal((byte)0xa5, value));
    }

    [Fact]
    public void AdmissionMotionRemainsCanonicalZeroWithoutBodyBinding()
    {
        var body = new CanonicalBody(new(1), 2, 0, default, default, default);
        var motion = PhysicsMotionRead.Decode(new byte[PhysicsMotionRead.ByteLength], body, new(0));
        motion.ValidateBinding(null, new(0));
        motion.ValidateBinding(body with { Id = new(2) }, new(0));
        Assert.Throws<ArgumentException>(() => motion.ValidateBinding(null, new(1)));
        var read = new WorkshopRead(new(2), new(0), null, new(4),
            new(WorkshopClockDomain.SimulationMonotonic, new(1), new(100_000_000), new(100_000)),
            Motion: motion);
        var encoded = WorkshopWire.Encode(Response(read));
        Assert.Equal(encoded, WorkshopWire.Encode(WorkshopWire.DecodeResponse(encoded)));
    }

    private static WorkshopRead MotionRead(CanonicalBody body, PhysicsMotionRead motion) =>
        new(new(2), new(1), body, new(4),
            new(WorkshopClockDomain.SimulationMonotonic, new(1), new(100_000_000), new(100_000)),
            new((Half)0, (Half)0, (Half)0, (Half)1), Motion: motion);

    private static byte[] MotionBytes(CanonicalBody body)
    {
        var bytes = new byte[PhysicsMotionRead.ByteLength];
        U32(bytes, 0, 1); U32(bytes, 4, 4); U32(bytes, 12, 4);
        var piece = PhysicsMotionRead.HeaderBytes;
        U32(bytes, piece, (uint)PhysicsMotionKind.FreePolynomial);
        U32(bytes, piece + 8, 4);
        H(bytes, piece + 22, (Half)WorkshopCadenceSettings.PhysicalFrequency);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(piece + 24), body.Id.Value);
        H(bytes, piece + 62, (Half)1);
        return bytes;
    }

    private static WorkshopResponse Response(WorkshopRead read) => new(new(1), WorkshopResponseKind.Acknowledgement,
        new(WorkshopCommandOutcome.Applied, WorkshopRejection.None),
        read.Tick.Value == 0 ? WorkshopSimulationPhase.Building : WorkshopSimulationPhase.Running,
        read, new(11, 23), default, new(1), new(1), new(1), new(read.Tick.Value));
    private static void U32(byte[] bytes, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), value);
    private static void H(byte[] bytes, int offset, Half value) => BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset), BitConverter.HalfToUInt16Bits(value));
}

