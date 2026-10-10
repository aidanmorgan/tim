using System.Buffers.Binary;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class WorkshopReadTests
{
    [Fact]
    public void PerCommitEnergyAndEnableChangesCannotBeCoalesced()
    {
        var baseline = new WorkshopRead(new(1), new(1), default, new(1),
            ContactWorks: new([new(new(2), new(3), 0, new(20f))], []),
            Electrical: new([new(new(4), new(5), new(100f), default, ElectricalEnable.Enabled)]));
        Assert.False(baseline.RequiresReliableRead(baseline));
        var credit = baseline with { ContactWorks = new([new(new(2), new(3), 0, new(21f), new(1f))], []) };
        Assert.True(credit.RequiresReliableRead(baseline));
        var debit = baseline with { Electrical = new([new(new(4), new(5), new(99f), new(1f), ElectricalEnable.Enabled)]) };
        Assert.True(debit.RequiresReliableRead(baseline));
        var disabled = baseline with { Electrical = new([new(new(4), new(5), new(100f), default, ElectricalEnable.Disabled)]) };
        Assert.True(disabled.RequiresReliableRead(baseline));
        Assert.False(disabled.RequiresReliableRead(disabled));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void IndependentlyDecodedReadsDeduplicateByExactContent(bool bodyPresent, bool captured)
    {
        var body = bodyPresent ? new CanonicalBody(new(1), 2, 0, default, default, default) : (CanonicalBody?)null;
        var read = new WorkshopRead(new(2), new(0), body.HasValue ? Bodies(body.Value) : default, new(4),
            new(WorkshopClockDomain.SimulationMonotonic, new(1), new(100_000_000), new(100_000)),
            Captures: captured ? new PhysicsCaptureRead([CaptureLatch.Clear(new(9))]) : default);
        var bytes = WorkshopWire.Encode(Response(read));
        var first = WorkshopWire.DecodeResponse(bytes).Read;
        var second = WorkshopWire.DecodeResponse(bytes).Read;
        Assert.NotSame(first.Motion, second.Motion);
        Assert.True(first.HasSameContent(second));
        Assert.True(first == second); // Independent immutable backing arrays must compare by canonical content.
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.False(first.HasSameContent(second with { Revision = new(5) }));
        Assert.False(first.HasSameContent(second with { Capture = second.Capture!.Value with { Time = new(100_000_001) } }));
        if (bodyPresent)
        {
            var signedZero = second.Bodies[0].Body with { Local = new(BitConverter.UInt16BitsToHalf(0x8000), (Half)0, (Half)0) };
            Assert.False(first.HasSameContent(second with { Bodies = Bodies(signedZero) }));
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
        var motion = MotionBytes(body);
        var piece = PhysicsMotionRead.HeaderBytes;
        var read = new WorkshopRead(new(2), new(1), Bodies(body), new(4),
            new(WorkshopClockDomain.SimulationMonotonic, new(1), new(100_000_000), new(100_000)),
            Captures: new([new(new(9), CaptureLatchPhase.Latched, 1, (Half)0)]),
            Motion: PhysicsMotionRead.Decode(motion, Bodies(body), new(1)));
        var encoded = WorkshopWire.Encode(Response(read));
        var first = WorkshopWire.DecodeResponse(encoded).Read;
        var second = WorkshopWire.DecodeResponse(encoded).Read;
        Assert.True(first.HasSameContent(second));
        var changed = (byte[])encoded.Clone();
        H(changed, WorkshopWire.ReadMotionOffset + piece + 54, (Half).125);
        Assert.False(first.HasSameContent(WorkshopWire.DecodeResponse(changed).Read));
        changed = (byte[])encoded.Clone();
        U32(changed, WorkshopWire.ReadCapturesOffset + 12, 2);
        Assert.False(first.HasSameContent(WorkshopWire.DecodeResponse(changed).Read));
        changed = (byte[])encoded.Clone();
        H(changed, WorkshopWire.ReadCapturesOffset + 16, BitConverter.UInt16BitsToHalf(0x8000));
        Assert.False(first.HasSameContent(WorkshopWire.DecodeResponse(changed).Read));
    }

    [Fact]
    public void MotionUsesExactProfileRateAndRejectsRetiredTiming()
    {
        var body = new CanonicalBody(new(1), 2, 15, default, default, default);
        var bytes = MotionBytes(body);
        var piece = PhysicsMotionRead.HeaderBytes;
        for (var i = 0; i < 4; i++)
            BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(piece + i * PhysicsMotionRead.PieceBytes + PhysicsMotionRead.VelocityOffset), 1f);
        var motion = PhysicsMotionRead.Decode(bytes, Bodies(body), new(15));
        Assert.True(motion.TrySample(body.Id, 58, out var pose));
        Assert.Equal(2, pose.Cell.X);
        Assert.Equal((Half)(58.0 / WorkshopCadenceSettings.PhysicalFrequency * 16 - 2), pose.Local.X);
        foreach (var invalid in new Half[] { (Half)(1.0 / 480), (Half)240, Half.NaN })
        {
            var changed = (byte[])bytes.Clone(); H(changed, piece + 22, invalid);
            Assert.Throws<ArgumentException>(() => PhysicsMotionRead.Decode(changed, Bodies(body), new(15)));
        }
        // The next interval must use its newly committed primary anchor.
        U32(bytes, 8, 60); U32(bytes, 12, 64);
        for (var i = 0; i < 4; i++)
        { U32(bytes, piece + i * PhysicsMotionRead.PieceBytes + 4, (uint)(60 + i));
          U32(bytes, piece + i * PhysicsMotionRead.PieceBytes + 8, (uint)(61 + i)); }
        Assert.Throws<ArgumentException>(() => PhysicsMotionRead.Decode(bytes, Bodies(body with { Tick = 16 }), new(16)));
    }

    [Fact]
    public void FreeMotionSampledBetweenTicksFollowsTheDragDecayedTrajectory()
    {
        // A free piece carrying drag k and gravity g samples x = v0(1 - e^-kt)/k and y = (g/k)(t - (1 - e^-kt)/k), the exact solution
        // of the exponential decay the solver applies; the same piece without its drag lane overshoots by the decay it omits.
        // The pieces keep their primary anchor at ordinal 0, so the sample at ordinal 58 extrapolates t = 58/480 s.
        var body = new CanonicalBody(new(1), 2, 15, default, default, default);
        var k = (Half).125; var g = (Half)(-9.81); const float v0 = 8f;
        byte[] Pieces(Half drag)
        {
            var bytes = MotionBytes(body);
            for (var i = 0; i < 4; i++)
            {
                var piece = PhysicsMotionRead.HeaderBytes + i * PhysicsMotionRead.PieceBytes;
                BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(piece + PhysicsMotionRead.VelocityOffset), v0);
                H(bytes, piece + PhysicsMotionRead.GravityOffset + 2, g);
                H(bytes, piece + PhysicsMotionRead.DragRateOffset, drag);
            }
            return bytes;
        }
        static double Metres(PresentedBody pose, int axis) => axis == 0
            ? (pose.Cell.X + (double)pose.Local.X) / 16 : (pose.Cell.Y + (double)pose.Local.Y) / 16;
        var t = 58.0 / WorkshopCadenceSettings.PhysicalFrequency; var rate = (double)k; var gravity = (double)g;
        var decay = (1 - Math.Exp(-rate * t)) / rate;
        Assert.True(PhysicsMotionRead.Decode(Pieces(k), Bodies(body), new(15)).TrySample(body.Id, 58, out var dragged));
        Assert.InRange(Metres(dragged, 0) - v0 * decay, -1e-4, 1e-4);
        Assert.InRange(Metres(dragged, 1) - gravity / rate * (t - decay), -1e-4, 1e-4);
        Assert.True(PhysicsMotionRead.Decode(Pieces((Half)0), Bodies(body), new(15)).TrySample(body.Id, 58, out var undamped));
        Assert.True(Metres(undamped, 0) - Metres(dragged, 0) > 5e-3);
    }

    [Fact]
    public void DecodedMotionOwnsValidatedBytesAndPreservesWireContent()
    {
        var body = new CanonicalBody(new(1), 2, 1, default, default, default);
        var bytes = MotionBytes(body);
        var expected = (byte[])bytes.Clone();
        var motion = PhysicsMotionRead.Decode(bytes, Bodies(body), new(1));
        bytes[^1] = 1; // A reused external input cannot change the validated snapshot.
        Assert.Throws<ArgumentException>(() => PhysicsMotionRead.Decode(bytes, Bodies(body), new(1)));
        Assert.True(motion.Bytes.SequenceEqual(expected));
        var read = MotionRead(body, motion);
        var encoded = WorkshopWire.Encode(Response(read));
        Assert.True(encoded.AsSpan(WorkshopWire.ReadMotionOffset, expected.Length).SequenceEqual(expected));
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
        var motion = PhysicsMotionRead.Decode(MotionBytes(body), Bodies(body), new(1));
        var read = MotionRead(body, motion);
        read = mismatch switch
        {
            MotionBindingMismatch.BodyIdentity => read with { Bodies = Bodies(body with { Id = new(2) }) },
            MotionBindingMismatch.BodyPresence => read with { Bodies = default },
            MotionBindingMismatch.Tick => read with { Tick = new(2), Bodies = Bodies(body with { Tick = 2 }) },
            _ => throw new ArgumentOutOfRangeException(nameof(mismatch))
        };
        var destination = Enumerable.Repeat((byte)0xa5, WorkshopWire.ResponseBytes).ToArray();
        Assert.Throws<ArgumentException>(() => WorkshopWire.Write(Response(read), destination));
        Assert.All(destination, value => Assert.Equal((byte)0xa5, value));
    }

    [Fact]
    public void AdmissionMotionRemainsCanonicalZeroWithExactBodyBinding()
    {
        var body = new CanonicalBody(new(1), 2, 0, default, default, default);
        var motion = PhysicsMotionRead.Decode(new byte[PhysicsMotionRead.ByteLength], Bodies(body), new(0));
        motion.ValidateBinding(Bodies(body), new(0));
        Assert.Throws<ArgumentException>(() => motion.ValidateBinding(default, new(0)));
        Assert.Throws<ArgumentException>(() => motion.ValidateBinding(Bodies(body with { Id = new(2) }), new(0)));
        Assert.Throws<ArgumentException>(() => motion.ValidateBinding(default, new(1)));
        motion = PhysicsMotionRead.Decode(new byte[PhysicsMotionRead.ByteLength], default, new(0));
        var read = new WorkshopRead(new(2), new(0), default, new(4),
            new(WorkshopClockDomain.SimulationMonotonic, new(1), new(100_000_000), new(100_000)),
            Motion: motion);
        var encoded = WorkshopWire.Encode(Response(read));
        Assert.Equal(encoded, WorkshopWire.Encode(WorkshopWire.DecodeResponse(encoded)));
    }

    [Fact]
    public void CurrentWireContactOccurrenceMustOwnCollider()
    {
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(
            WorkshopInput.Basketball(new(1),0,6,0,0,0,0,1),
            WorkshopInput.Bumper(new(2),0,4,0,0,0,0,1,BumperWork.FromCanonicalStrength((float)8))));
        var scene = WorkshopPhysicsCompiler.Compile(construction,new(11,23));
        var declaration = scene.ContactWorks[0];
        var colliders = scene.Colliders.ToArray();
        var owned = new ColliderSlot(checked((ushort)Array.FindIndex(colliders, c => c.Body == declaration.Owner)));
        var foreign = new ColliderSlot(checked((ushort)Array.FindIndex(colliders, c => c.Body == new GpuBodyId(1))));
        var body = new CanonicalBody(new(1),2,1,default,default,default);
        var motion = PhysicsMotionRead.Decode(MotionBytes(body),Bodies(body),new(1));
        var store = new ContactWorkRead(declaration.Id,declaration.Owner,1,new(20));
        var hit = new ContactWorkOccurrence(new(0),owned,new(1),1,1,(Half)0,
            declaration.Threshold,new(12),ContactWorkEffect.Paid);
        WorkshopResponse Wire(ContactWorkOccurrence occurrence) => WorkshopWire.DecodeResponse(WorkshopWire.Encode(
            Response(MotionRead(body,motion) with { ContactWorks=new(new[] {store}, new[] {occurrence}) })));
        Wire(hit).Read.ContactWorks.ValidateScene(scene, Wire(hit).Read.Bodies);
        foreach (var invalid in new[] {
            hit with { Collider=foreign },
            hit with { Collider=new(ushort.MaxValue) } })
            Assert.Throws<ArgumentException>(()=>Wire(invalid).Read.ContactWorks.ValidateScene(scene, Wire(invalid).Read.Bodies));
    }

    private static WorkshopRead MotionRead(CanonicalBody body, PhysicsMotionRead motion) =>
        new(new(2), new(1), Bodies(body), new(4),
            new(WorkshopClockDomain.SimulationMonotonic, new(1), new(100_000_000), new(100_000)),
            Motion: motion);

    private static PhysicsBodyReadSet Bodies(CanonicalBody body) =>
        new(new[] { new PhysicsBodyRead(body, CanonicalRotation.Identity, default, default) });

    private static byte[] MotionBytes(CanonicalBody body)
    {
        var bytes = new byte[PhysicsMotionRead.ByteLength];
        var start = checked((uint)(body.Tick * 4 - 4));
        U32(bytes, 0, 4); U32(bytes, 4, 4); U32(bytes, 8, start); U32(bytes, 12, start + 4);
        for (uint i = 0; i < 4; i++)
        {
            var piece = PhysicsMotionRead.HeaderBytes + checked((int)i) * PhysicsMotionRead.PieceBytes;
            U32(bytes, piece, (uint)PhysicsMotionKind.FreePolynomial);
            U32(bytes, piece + 4, start + i); U32(bytes, piece + 8, start + i + 1);
            H(bytes, piece + 22, (Half)WorkshopCadenceSettings.PhysicalFrequency);
            BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(piece + 24), body.Id.Value);
            H(bytes, piece + 62, (Half)1);
        }
        return bytes;
    }

    private static WorkshopResponse Response(WorkshopRead read) => new(new(1), WorkshopResponseKind.Acknowledgement,
        new(WorkshopCommandOutcome.Applied, WorkshopRejection.None),
        read.Tick.Value == 0 ? WorkshopSimulationPhase.Building : WorkshopSimulationPhase.Running,
        read, new(11, 23), default, new(1), new(1), new(1), new(read.Tick.Value));
    private static void U32(byte[] bytes, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), value);
    private static void H(byte[] bytes, int offset, Half value) => BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset), BitConverter.HalfToUInt16Bits(value));
}
