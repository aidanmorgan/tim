using System.Buffers.Binary;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class WorkshopReadTests
{
    // Story 9A.23 D9: no read is reliable. A commit that credits a store or debits a source carries a balance transfer, which gates only
    // the CCGPU_RETAINED observation line; a hit or an enable edge is an event ring record (Impact, ElectricalPowered), not a read.
    [Fact]
    public void OnlyACommitThatMovesEnergyCarriesABalanceTransfer()
    {
        var baseline = new WorkshopRead(new(1), new(1), default, new(1),
            ContactWorks: new([new(new(2), new(3), 0, new(20f))]),
            Electrical: new([new(new(4), new(5), new(100f), default, ElectricalEnable.Enabled)]));
        Assert.False(baseline.CarriesBalanceTransfer());
        Assert.True((baseline with { ContactWorks = new([new(new(2), new(3), 0, new(21f), new(1f))]) }).CarriesBalanceTransfer());
        Assert.True((baseline with { Electrical = new([new(new(4), new(5), new(99f), new(1f), ElectricalEnable.Enabled)]) }).CarriesBalanceTransfer());
        Assert.False((baseline with { ContactWorks = new([new(new(2), new(3), 1, new(20f))]) }).CarriesBalanceTransfer()); // a passive hit
        Assert.False((baseline with { Electrical = new([new(new(4), new(5), new(100f), default, ElectricalEnable.Disabled)]) }).CarriesBalanceTransfer());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void IndependentlyDecodedReadsDeduplicateByExactContent(bool bodyPresent, bool captured)
    {
        var body = bodyPresent ? new CanonicalBody(new(1), 2, 0, default, default, default) : (CanonicalBody?)null;
        var bodies = body.HasValue ? Bodies(body.Value) : default;
        var read = new WorkshopRead(new(2), new(0), bodies, new(4),
            new(WorkshopClockDomain.SimulationMonotonic, new(1), new(100_000_000), new(100_000)),
            Captures: captured ? new PhysicsCaptureRead([CaptureLatch.Clear(new(9), new(1))]) : default,
            Motion: PhysicsMotionRead.Decode(new byte[PhysicsMotionRead.ByteLength(bodies.Count)], bodies, new(0)));
        var bytes = WorkshopWireFlatBuffers.EncodeResponse(Response(read));
        var first = WorkshopWireFlatBuffers.DecodeResponse(bytes).Read;
        var second = WorkshopWireFlatBuffers.DecodeResponse(bytes).Read;
        Assert.NotSame(first.Motion, second.Motion);
        Assert.True(first.HasSameContent(second));
        Assert.True(first == second); // Independent immutable backing arrays must compare by canonical content.
        Assert.False(first.HasSameContent(second with { Revision = new(5) }));
        Assert.False(first.HasSameContent(second with { Capture = second.Capture!.Value with { Time = new(100_000_001) } }));
        if (bodyPresent)
        {
            var signedZero = second.Bodies[0].Body with { Local = new(BitConverter.UInt32BitsToSingle(0x80000000), 0f, 0f) };
            Assert.False(first.HasSameContent(second with { Bodies = Bodies(signedZero) }));
        }
        if (captured)
        {
            var changed = second with { Captures = new([CaptureLatch.Clear(new(10), new(1))]) };
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
            Captures: new([new(new(9), new(1), CaptureLatchPhase.Latched, 1, 0f)]),
            Motion: PhysicsMotionRead.Decode(motion, Bodies(body), new(1)));
        var encoded = WorkshopWireFlatBuffers.EncodeResponse(Response(read));
        var first = WorkshopWireFlatBuffers.DecodeResponse(encoded).Read;
        var second = WorkshopWireFlatBuffers.DecodeResponse(encoded).Read;
        Assert.True(first.HasSameContent(second));
        WorkshopRead Wire(WorkshopRead changed) => WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(Response(changed))).Read;
        var drag = (byte[])motion.Clone();
        F(drag, piece + PhysicsMotionRead.DragRateOffset, .125f);
        Assert.False(first.HasSameContent(Wire(read with { Motion = PhysicsMotionRead.Decode(drag, Bodies(body), new(1)) })));
        Assert.False(first.HasSameContent(Wire(read with { Captures = new([new(new(9), new(1), CaptureLatchPhase.Latched, 2, 0f)]) })));
        Assert.False(first.HasSameContent(Wire(read with { Captures = new([new(new(9), new(1), CaptureLatchPhase.Latched, 1, -0f)]) })));
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
        Assert.Equal((float)(58.0 / WorkshopCadenceSettings.PhysicalFrequency * 16 - 2), pose.Local.X);
        // The piece frequency is the U32 physical rate: every other rate is retired timing.
        foreach (var invalid in new uint[] { 0, 240, WorkshopCadenceSettings.PhysicalFrequency + 1 })
        {
            var changed = (byte[])bytes.Clone(); U32(changed, piece + PhysicsMotionRead.FrequencyOffset, invalid);
            Assert.Throws<ArgumentException>(() => PhysicsMotionRead.Decode(changed, Bodies(body), new(15)));
        }
        // The next interval must use its newly committed primary anchor.
        U32(bytes, 8, 60); U32(bytes, 12, 64);
        for (var i = 0; i < 4; i++)
        { U32(bytes, piece + i * PhysicsMotionRead.PieceBytes + PhysicsMotionRead.StartOrdinalOffset, (uint)(60 + i));
          U32(bytes, piece + i * PhysicsMotionRead.PieceBytes + PhysicsMotionRead.EndOrdinalOffset, (uint)(61 + i)); }
        Assert.Throws<ArgumentException>(() => PhysicsMotionRead.Decode(bytes, Bodies(body with { Tick = 16 }), new(16)));
    }

    [Fact]
    public void FreeMotionSampledBetweenTicksFollowsTheDragDecayedTrajectory()
    {
        // A free piece carrying drag k and gravity g samples x = v0(1 - e^-kt)/k and y = (g/k)(t - (1 - e^-kt)/k), the exact solution
        // of the exponential decay the solver applies; the same piece without its drag lane overshoots by the decay it omits.
        // The pieces keep their primary anchor at ordinal 0, so the sample at ordinal 58 extrapolates t = 58/480 s.
        var body = new CanonicalBody(new(1), 2, 15, default, default, default);
        var k = .125f; var g = -9.81f; const float v0 = 8f;
        byte[] Pieces(float drag)
        {
            var bytes = MotionBytes(body);
            for (var i = 0; i < 4; i++)
            {
                var piece = PhysicsMotionRead.HeaderBytes + i * PhysicsMotionRead.PieceBytes;
                BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(piece + PhysicsMotionRead.VelocityOffset), v0);
                F(bytes, piece + PhysicsMotionRead.GravityOffset + 4, g);
                F(bytes, piece + PhysicsMotionRead.DragRateOffset, drag);
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
        Assert.True(PhysicsMotionRead.Decode(Pieces(0f), Bodies(body), new(15)).TrySample(body.Id, 58, out var undamped));
        Assert.True(Metres(undamped, 0) - Metres(dragged, 0) > 5e-3);
    }

    [Fact]
    public void DecodedMotionOwnsItsTrajectoryAfterInputReuseAndWireRoundTrip()
    {
        var body = new CanonicalBody(new(1), 2, 1, default, default, default);
        var bytes = MotionBytes(body);
        for (var i = 0; i < 4; i++) F(bytes, PhysicsMotionRead.HeaderBytes + i * PhysicsMotionRead.PieceBytes + PhysicsMotionRead.VelocityOffset, 8f);
        var motion = PhysicsMotionRead.Decode(bytes, Bodies(body), new(1));
        for (var i = 0; i < 4; i++) F(bytes, PhysicsMotionRead.HeaderBytes + i * PhysicsMotionRead.PieceBytes + PhysicsMotionRead.VelocityOffset, 16f);
        var changed = PhysicsMotionRead.Decode(bytes, Bodies(body), new(1));
        bytes[^1] = 1; // A reused external input cannot change the validated snapshot.
        Assert.Throws<ArgumentException>(() => PhysicsMotionRead.Decode(bytes, Bodies(body), new(1)));
        var read = MotionRead(body, motion);
        var encoded = WorkshopWireFlatBuffers.EncodeResponse(Response(read));
        var decoded = WorkshopWireFlatBuffers.DecodeResponse(encoded);
        Assert.Equal(Response(read).Sequence, decoded.Sequence);
        Assert.Equal(WorkshopResponseKind.Acknowledgement, decoded.Kind);
        Assert.Equal(WorkshopCommandOutcome.Applied, decoded.Result.Outcome);
        Assert.Equal(WorkshopSimulationPhase.Running, decoded.Phase);
        Assert.Equal(read.Epoch, decoded.Read.Epoch);
        Assert.Equal(read.Revision, decoded.Read.Revision);
        Assert.Equal(read.Capture, decoded.Read.Capture);
        NoFixedCapacityTests.AssertSameState(read, decoded.Read, "motion response");
        foreach (var snapshot in new[] { motion, decoded.Read.Motion! })
        {
            Assert.Equal(4, snapshot.Count);
            Assert.Equal(4u, snapshot.Substeps);
            Assert.Equal(0u, snapshot.FirstOrdinal);
            Assert.Equal(4u, snapshot.LastOrdinal);
            foreach (var ordinal in new[] { .5, 1.5, 2.5, 3.5 })
            {
                Assert.True(snapshot.TrySample(body.Id, ordinal, out var pose));
                Assert.Equal(body.Id, pose.Id);
                Assert.Equal(CanonicalRotation.Identity, pose.Rotation);
                Assert.Equal(0, pose.Cell.Y); Assert.Equal(0f, pose.Local.Y);
                Assert.Equal(0, pose.Cell.Z); Assert.Equal(0f, pose.Local.Z);
                // This codec/interpolation check allows f32 rounding of x=v*t; it is not a scene-drift acceptance bound.
                Assert.InRange((pose.Cell.X + (double)pose.Local.X) / 16 - 8 * ordinal / 480, -1e-7, 1e-7);
                Assert.True(changed.TrySample(body.Id, ordinal, out var faster));
                Assert.True((faster.Cell.X + (double)faster.Local.X) / 16 > (pose.Cell.X + (double)pose.Local.X) / 16);
            }
            Assert.False(snapshot.TrySample(new(2), 1, out _));
        }
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
        Assert.Throws<ArgumentException>(() => WorkshopWireFlatBuffers.EncodeResponse(Response(read)));
    }

    [Fact]
    public void AdmissionMotionRemainsCanonicalZeroWithExactBodyBinding()
    {
        var body = new CanonicalBody(new(1), 2, 0, default, default, default);
        var motion = PhysicsMotionRead.Decode(new byte[PhysicsMotionRead.ByteLength(1)], Bodies(body), new(0));
        motion.ValidateBinding(Bodies(body), new(0));
        Assert.Throws<ArgumentException>(() => motion.ValidateBinding(default, new(0)));
        Assert.Throws<ArgumentException>(() => motion.ValidateBinding(Bodies(body with { Id = new(2) }), new(0)));
        Assert.Throws<ArgumentException>(() => motion.ValidateBinding(default, new(1)));
        motion = PhysicsMotionRead.Decode(new byte[PhysicsMotionRead.ByteLength(0)], default, new(0));
        var read = new WorkshopRead(new(2), new(0), default, new(4),
            new(WorkshopClockDomain.SimulationMonotonic, new(1), new(100_000_000), new(100_000)),
            Motion: motion);
        var encoded = WorkshopWireFlatBuffers.EncodeResponse(Response(read));
        var decoded = WorkshopWireFlatBuffers.DecodeResponse(encoded).Read;
        NoFixedCapacityTests.AssertSameState(read, decoded, "empty admission");
        Assert.Equal(read.Epoch, decoded.Epoch);
        Assert.Equal(read.Revision, decoded.Revision);
        Assert.Equal(read.Capture, decoded.Capture);
        Assert.NotNull(decoded.Motion);
        Assert.Equal(0, decoded.Motion.Count);
        Assert.Equal(0u, decoded.Motion.Substeps);
        Assert.Equal(0u, decoded.Motion.FirstOrdinal);
        Assert.Equal(0u, decoded.Motion.LastOrdinal);
        Assert.False(decoded.Motion.TrySample(body.Id, 0, out _));
    }

    [Theory]
    [InlineData(ContactWorkEffect.Paid)]
    [InlineData(ContactWorkEffect.Passive)]
    public void ADevicePrivateContactOccurrenceMustOwnColliderAndInclusiveThreshold(ContactWorkEffect effect)
    {
        // Story 9A.23 D9: the occurrence rows are the device's ledger, never on the read or the wire; the scene binds them there.
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(
            WorkshopInput.Generic(PartId.Basketball,new(1),0,6,0,0,0,0,1),
            WorkshopInput.Generic(PartId.PinballBumper,new(2),0,4,0,0,0,0,1)));
        var scene = WorkshopPhysicsCompiler.Compile(construction,new(11,23));
        var declaration = scene.ContactWorks[0];
        var colliders = scene.Colliders.ToArray();
        var owned = new ColliderSlot(checked((ushort)Array.FindIndex(colliders, c => c.Body == declaration.Owner)));
        var foreign = new ColliderSlot(checked((ushort)Array.FindIndex(colliders, c => c.Body == new GpuBodyId(1))));
        var body = new CanonicalBody(new(1),2,1,default,default,default);
        var store = new ContactWorkRead(declaration.Id,declaration.Owner,1,new(20));
        var hit = new ContactWorkOccurrence(new(0),owned,new(1),1,1,0f,
            declaration.Threshold,new(effect == ContactWorkEffect.Paid ? 12 : 0),effect);
        ContactWorkLedger Ledger(ContactWorkOccurrence occurrence) => new([store], [occurrence]);
        Ledger(hit).ValidateScene(scene, Bodies(body));
        var initial = new ContactWorkLedger(
            new[] { new ContactWorkRead(declaration.Id,declaration.Owner,0,declaration.InitialEnergy) },
            Array.Empty<ContactWorkOccurrence>());
        initial.ValidateScene(scene, Bodies(body));
        initial.ValidateAdmission();
        Assert.Throws<ArgumentException>(() => Ledger(hit).ValidateAdmission()); // an admitted world has hit nothing
        foreach (var invalid in new[] {
            hit with { Collider=foreign },
            hit with { Collider=new(ushort.MaxValue) },
            hit with { ApproachSpeed=new(float.BitDecrement(declaration.Threshold.Value)) } })
            Assert.Throws<ArgumentException>(()=>Ledger(invalid).ValidateScene(scene, Bodies(body)));
        // The read carries the stores alone, and they cross the wire unchanged.
        var read = MotionRead(body, PhysicsMotionRead.Decode(MotionBytes(body),Bodies(body),new(1))) with { ContactWorks = Ledger(hit).Stores };
        var decoded = WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(Response(read))).Read.ContactWorks;
        Assert.Equal(1, decoded.Count);
        Assert.Equal(store, decoded[0]);
    }

    // Story 9A.23 D9: the reservoir accounting lives on the device ledger (the client copy is deleted). Every count increment is exactly
    // its new rows, each later than the row it replaces, and the store moves by exactly their debits; anything else is refused.
    [Fact]
    public void TheDeviceLedgerAccountsForEveryHitAndRefusesAMalformedAdvance()
    {
        var target = new GpuBodyId(1);
        ContactWorkLedger Ledger(uint count, float remaining, ContactWorkOccurrence row, float supplied = 0f) =>
            new([new ContactWorkRead(new(9), new(8), count, new(remaining), new(supplied))], [row]);
        var clear = new ContactWorkOccurrence(new(0), default, target, 0, 0, 0f, default, default, ContactWorkEffect.Passive);
        var hit = new ContactWorkOccurrence(new(0), new(3), target, 1, 16, 0f, new(4f), new(12f), ContactWorkEffect.Paid);
        var before = Ledger(0, 32f, clear);
        var after = Ledger(1, 20f, hit);
        after.ValidateAdvance(before);
        Ledger(1, 20.5f, hit, supplied: .5f).ValidateAdvance(before); // a recharge in the same commit is its own transfer
        Ledger(1, 20f, hit).ValidateAdvance(after);                     // a repeated commit with no new hit
        foreach (var (name, invalid, previous) in new (string, ContactWorkLedger, ContactWorkLedger)[] {
            ("the store moved by other than the debit", Ledger(1, 21f, hit), before),
            ("a counted hit has no row", Ledger(2, 20f, hit), before),
            ("a hit reversed in time", Ledger(2, 8f, hit with { Sequence = 2, EventOrdinal = 10 }), after),
            ("an unchanged row mutated", Ledger(1, 20f, hit with { Debit = new(11f) }), after),
            ("the store moved with no hit", Ledger(0, 31f, clear), before) })
            Assert.True(Assert.Throws<ArgumentException>(() => invalid.ValidateAdvance(previous)) is not null, name);
        // A paid row without a debit, or a passive row with one, is refused where the ledger is read.
        Assert.Throws<ArgumentException>(() => Ledger(1, 32f, hit with { Debit = default }));
        Assert.Throws<ArgumentException>(() => Ledger(1, 20f, hit with { Effect = ContactWorkEffect.Passive }));
    }

    private static WorkshopRead MotionRead(CanonicalBody body, PhysicsMotionRead motion) =>
        new(new(2), new(1), Bodies(body), new(4),
            new(WorkshopClockDomain.SimulationMonotonic, new(1), new(100_000_000), new(100_000)),
            Motion: motion);

    private static PhysicsBodyReadSet Bodies(CanonicalBody body) =>
        new(new[] { new PhysicsBodyRead(body, CanonicalRotation.Identity, default, default) });

    private static byte[] MotionBytes(CanonicalBody body)
    {
        var bytes = new byte[PhysicsMotionRead.ByteLength(1)];
        var start = checked((uint)(body.Tick * 4 - 4));
        U32(bytes, 0, 4); U32(bytes, 4, 4); U32(bytes, 8, start); U32(bytes, 12, start + 4);
        for (uint i = 0; i < 4; i++)
        {
            var piece = PhysicsMotionRead.HeaderBytes + checked((int)i) * PhysicsMotionRead.PieceBytes;
            U32(bytes, piece, (uint)PhysicsMotionKind.FreePolynomial);
            U32(bytes, piece + PhysicsMotionRead.StartOrdinalOffset, start + i); U32(bytes, piece + PhysicsMotionRead.EndOrdinalOffset, start + i + 1);
            U32(bytes, piece + PhysicsMotionRead.FrequencyOffset, WorkshopCadenceSettings.PhysicalFrequency);
            BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(piece + PhysicsMotionRead.OwnerOffset), body.Id.Value);
            BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(piece + PhysicsMotionRead.RotationOffset + PhysicsGpuAbi.RotationWOffset), 1f);
        }
        return bytes;
    }

    private static WorkshopResponse Response(WorkshopRead read) => new(new(1), WorkshopResponseKind.Acknowledgement,
        new(WorkshopCommandOutcome.Applied, WorkshopRejection.None),
        read.Tick.Value == 0 ? WorkshopSimulationPhase.Building : WorkshopSimulationPhase.Running,
        read, new(11, 23), default, new(1), new(1), new(1), new(read.Tick.Value));
    private static void U32(byte[] bytes, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), value);
    private static void F(byte[] bytes, int offset, float value) => BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(offset), value);
}
