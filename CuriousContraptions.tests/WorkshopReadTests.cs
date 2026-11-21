using CuriousContraptions.Abstractions;
using System.Buffers.Binary;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class WorkshopReadTests
{
    [Theory]
    [InlineData(.1f)]
    [InlineData(.01f)]
    public void ManyAcceptedPaymentsValidateDespiteOrdinarySummaryRounding(float payment)
    {
        var balance = new FiniteEnergyBalance(14400, 0);
        float summary = 0;
        for (var i = 0; i < 1000; i++) { balance = balance.Debit(payment); summary += payment; }
        var previous = new PhysicsElectricalRead([new(new(1), new(2), new(14400, 0), new(0), ElectricalEnable.Enabled, 0)]);
        var after = new ElectricalSourceRead(new(1), new(2), balance, new(summary), ElectricalEnable.Enabled, 1000);
        new PhysicsElectricalRead([after]).ValidateAdvance(previous);
        // A whole missing joule materially violates payment; this is not a trajectory tolerance.
        Assert.Throws<ArgumentException>(() => new PhysicsElectricalRead([after with
        { Balance = new(balance.Bulk + 1, balance.Remainder) }]).ValidateAdvance(previous));
        PhysicsReadValidation.ValidateElectricalBudget(summary, 1000, new(14400, 0), default,
            payment == .01f ? 10 : 100.01f, 1);
        Assert.Throws<ArgumentException>(() => PhysicsReadValidation.ValidateElectricalBudget(
            summary + 1, 1000, new(14400, 0), default, payment == .01f ? 10 : 100.01f, 1));
    }


    [Fact]
    public void MixedCreditAndReleaseAreValidatedAsAcceptedTransactions()
    {
        var initial = new EnergyStoreRead(new(1), new(2), new(10, 0), new(0), new(0), new(0),
            new(0), new(0), new(0), 0);
        var next = initial with { Balance = initial.Balance.Debit(5).Credit(4, 10),
            Supplied = new(4), Released = new(5), CommitSupplied = new(4), CommitReleased = new(5), Transactions = 2 };
        new PhysicsEnergyStoreRead([next]).ValidateAdvance(new([initial]));
        Assert.Throws<ArgumentException>(() => new PhysicsEnergyStoreRead([next with { Balance = new(10, 0) }])
            .ValidateAdvance(new([initial])));
    }

    [Fact]
    public void ObservationEnclosuresCoverFiniteExtremesAndPreserveZeroRefusals()
    {
        foreach (var value in new[] { float.Epsilon, float.Epsilon * 16, 1f, float.MaxValue })
        {
            var normalized = EnergyReadInterval.Ratio(value, float.MaxValue);
            var exact = (double)value / float.MaxValue;
            Assert.True(normalized.Lower <= exact && normalized.Upper >= exact);
            var error = EnergyReadInterval.Error(value, uint.MaxValue, value);
            Assert.True(float.IsFinite(error));
            PhysicsReadValidation.ValidateSummary(0, value, value, 1);
        }
        PhysicsReadValidation.ValidateSummary(float.MaxValue, float.MaxValue, float.Epsilon, 1);
        Assert.Throws<ArgumentException>(() => PhysicsReadValidation.ValidateSummary(0, 1, 1, 0));
        Assert.Throws<ArgumentException>(() => PhysicsReadValidation.ValidateElectricalBudget(1, 1, default, default, 0, 0));
        Assert.Throws<ArgumentException>(() => PhysicsReadValidation.ValidateConnectedCredits(0, 1, 1, 1, 1));
        // Two finite MaxValue credits cannot be funded by one finite MaxValue debit.
        Assert.Throws<ArgumentException>(() => PhysicsReadValidation.ValidateConnectedCredits(float.MaxValue, 2, float.MaxValue, 2, 2));
        PhysicsReadValidation.ValidateConnectedCredits(float.MaxValue, 1, float.MaxValue, 1, 1);
        PhysicsReadValidation.ValidateElectricalBudget(MathF.BitIncrement(1), 1,
            new(2, 0), new(1, 1f / 8388608f), 0, 0);
        PhysicsReadValidation.ValidateElectricalBudget(MathF.BitIncrement(1), 1,
            new(1, 1f / 8388608f), new(2, 0), 0, 0);
        // A live phase remainder is real entitlement; zero new windows must not drop it.
        PhysicsReadValidation.ValidateElectricalBudget(.25f, 1, new(1, 0), new(.25f, 0), 0, 0);
    }

    // Story 9A.23 D9: no read is reliable. A commit that credits a store or debits a source carries a balance transfer, which gates only
    // the CCGPU_RETAINED observation line; a hit or an enable edge is an event ring record (Impact, ElectricalPowered), not a read.
    [Theory]
    [InlineData(SimulationCadence.Hz60)]
    [InlineData(SimulationCadence.Hz120)]
    [InlineData(SimulationCadence.Hz240)]
    public void IntegratedWorldRotationMovesOffsetBodyOriginAcrossAVisibleClearance(SimulationCadence cadence)
    {
        var scene = new LawScene(false);
        var start = System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitX, MathF.PI / 2);
        var body = scene.Sphere(new(40, 5, 0), .1f, 1f, LawMaterial.Domino,
            new(1, 0, 0), new(0, 0, 128), 0, 0, rotation: start);
        scene.DeclareMassFrame(body, System.Numerics.Vector3.One, new(.5f, 0, 0));
        var world = scene.Admit(cadence);
        var result = world.Step(1);
        var layout = PhysicsGpuAbi.Layout(result.World);
        var bodies = PhysicsGpuAbi.ReadDynamicBodies(result.World);
        var motion = PhysicsMotionRead.Decode(result.World.AsSpan(layout.MotionOffset, layout.MotionBytes), bodies, new(1));
        Assert.True(motion.TrySample(body, new(1, -2048), out var pose));
        const double elapsed = .5 / 480;
        var angle = 128 * elapsed;
        var x = (pose.Cell.X + (double)pose.Local.X) / 16;
        var y = (pose.Cell.Y + (double)pose.Local.Y) / 16;
        Assert.InRange(x - (40.5 + elapsed - .5 * Math.Cos(angle)), -.001, .001);
        Assert.InRange(y - (5 - .5 * Math.Sin(angle)), -.001, .001);
        Assert.True(y < 4.95, "The offset origin must pass the authored 5 cm clearance.");
        var q = new System.Numerics.Quaternion(pose.Rotation.X, pose.Rotation.Y, pose.Rotation.Z, pose.Rotation.W);
        var axis = System.Numerics.Vector3.Transform(System.Numerics.Vector3.UnitX, q);
        Assert.InRange(axis.X - Math.Cos(angle), -.002, .002);
        Assert.InRange(axis.Y - Math.Sin(angle), -.002, .002);
        Assert.False(motion.TrySample(body, new(motion.LastOrdinal), out _));
        // Unsupported record width is refused, with no alternate reader for the former payload.
        Assert.Throws<ArgumentException>(() => PhysicsMotionRead.Decode(new byte[16 + 8 * 160], bodies, new(1)));
    }

     [Theory]
    [InlineData(SimulationCadence.Hz60)]
    [InlineData(SimulationCadence.Hz120)]
    [InlineData(SimulationCadence.Hz240)]
    public void ContactIntervalDoesNotApplyTheLaterRestitutionVelocityEarly(SimulationCadence cadence)
    {
        var scene = new LawScene();
        var height = LawScene.BenchY + .1f;
        var body = scene.Sphere(new(0, height, 0), .1f, 1f, new LawMaterial(1, .1f, 0, 0),
            new(0, -20, 0), default, 0, 0);
        var result = scene.Admit(cadence).Step(1);
        var layout = PhysicsGpuAbi.Layout(result.World);
        var bodies = PhysicsGpuAbi.ReadDynamicBodies(result.World);
        var motion = PhysicsMotionRead.Decode(result.World.AsSpan(layout.MotionOffset, layout.MotionBytes), bodies, new(1));
        Assert.True(result[body].Velocity.Y > 15, "The ordinary contact must produce a substantial elastic rebound.");
        Assert.True(motion.TrySample(body, new(1, -2048), out var pose));
        var sampledHeight = (pose.Cell.Y + pose.Local.Y) / 16;
        // The first interval is integrated while support arrests the incoming sphere; restitution is applied afterward.
        // A 5 mm contact allowance excludes using the later >15 m/s rebound early (over 15 mm at this midpoint).
        Assert.InRange(sampledHeight - height, -.005f, .005f);
        Assert.False(motion.TrySample(body, new(motion.LastOrdinal), out _));
    }
    [Fact]
    public void OnlyACommitThatMovesEnergyCarriesABalanceTransfer()
    {
        var baseline = new WorkshopRead(new(1), new(1), new(1), default, new(1),
            ContactWorks: new([new(new(2), new(3), 0, new(6))]),
            Electrical: new([new(new(4), new(5), new(100f, 0f), default, ElectricalEnable.Enabled, 0)]));
        Assert.False(baseline.CarriesBalanceTransfer());
        Assert.True((baseline with { Electrical = new([new(new(4), new(5), new(99f, 0f), new(1f), ElectricalEnable.Enabled, 1)]) }).CarriesBalanceTransfer());
        Assert.False((baseline with { ContactWorks = new([new(new(2), new(3), 1, new(6))]) }).CarriesBalanceTransfer()); // a passive hit
        Assert.False((baseline with { Electrical = new([new(new(4), new(5), new(100f, 0f), default, ElectricalEnable.Disabled, 0)]) }).CarriesBalanceTransfer());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void IndependentlyDecodedReadsDeduplicateByExactContent(bool bodyPresent, bool captured)
    {
        var body = bodyPresent ? new CanonicalBody(new(1), 2, 0, default, default, default) : (CanonicalBody?)null;
        var bodies = body.HasValue ? Bodies(body.Value) : default;
        var read = new WorkshopRead(new(2), new(0), new(0), bodies, new(4),
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
        var read = new WorkshopRead(new(2), new(1), new(1), Bodies(body), new(4),
            new(WorkshopClockDomain.SimulationMonotonic, new(1), new(100_000_000), new(100_000)),
            Captures: new([new(new(9), new(1), CaptureLatchPhase.Latched, 1, 0f)]),
            Motion: PhysicsMotionRead.Decode(motion, Bodies(body), new(1)));
        var encoded = WorkshopWireFlatBuffers.EncodeResponse(Response(read));
        var first = WorkshopWireFlatBuffers.DecodeResponse(encoded).Read;
        var second = WorkshopWireFlatBuffers.DecodeResponse(encoded).Read;
        Assert.True(first.HasSameContent(second));
        WorkshopRead Wire(WorkshopRead changed) => WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(Response(changed))).Read;
        var velocity = (byte[])motion.Clone();
        F(velocity, piece + PhysicsMotionRead.VelocityOffset, .125f);
        Assert.False(first.HasSameContent(Wire(read with { Motion = PhysicsMotionRead.Decode(velocity, Bodies(body), new(1)) })));
        Assert.False(first.HasSameContent(Wire(read with { Captures = new([new(new(9), new(1), CaptureLatchPhase.Latched, 2, 0f)]) })));
        Assert.False(first.HasSameContent(Wire(read with { Captures = new([new(new(9), new(1), CaptureLatchPhase.Latched, 1, -0f)]) })));
        // A committed surface deflection is read content: the same deflection through the wire is the same read, another deflection or
        // a read without the surface row is a new one.
        WorkshopRead Surface(float deflection) => Wire(read with { Presentation = new([], [], surfaces: [new(new(3), deflection)]) });
        var surfaced = Surface(.1f);
        Assert.True(surfaced.HasSameContent(Surface(.1f)));
        Assert.False(surfaced.HasSameContent(Surface(.2f)));
        Assert.False(first.HasSameContent(surfaced));
        // A committed drive target is read content too (engine-revolute-drive-target): 6 rad/s through the wire is the same read,
        // 12 rad/s or a read without the drive row is a new one.
        WorkshopRead Driven(float target) => Wire(read with { DriveTargets = new([new(new(1), new(3), target)]) });
        var driven = Driven(6f);
        Assert.True(driven.HasSameContent(Driven(6f)));
        Assert.False(driven.HasSameContent(Driven(12f)));
        Assert.False(first.HasSameContent(driven));
    }

    [Fact]
    public void AbsentMotionIsNotAWildcardForCommittedReadEquality()
    {
        var body = new CanonicalBody(new(1), 2, 1, default, default, default);
        var bodies = Bodies(body);
        var bytes = MotionBytes(body);
        var first = new WorkshopRead(new(2), new(1), new(1), bodies,
            Motion: PhysicsMotionRead.Decode(bytes, bodies, new(1)));
        var same = first with { Motion = PhysicsMotionRead.Decode(bytes, bodies, new(1)) };
        var absent = first with { Motion = null };
        F(bytes, PhysicsMotionRead.HeaderBytes + PhysicsMotionRead.VelocityOffset, .125f);
        var different = first with { Motion = PhysicsMotionRead.Decode(bytes, bodies, new(1)) };
        Assert.True(first.Equals(first));
        Assert.Equal(first, same);
        Assert.Equal(absent, same with { Motion = null });
        Assert.False(first.HasSameContent(absent));
        Assert.False(absent.HasSameContent(first));
        Assert.NotEqual(first, absent);
        Assert.NotEqual(absent, different);
        Assert.NotEqual(first, different);
        Assert.False(WorkshopRead.SamePhysicalContent(first, absent));
    }

    [Fact]
    public void IntegratedMotionUsesEachSubstepStartAndTheFixedPhysicalRate()
    {
        var body = new CanonicalBody(new(1), 2, 15, default, default, default);
        var bytes = MotionBytes(body);
        for (var i = 0; i < 4; i++)
            F(bytes, PhysicsMotionRead.HeaderBytes + i * PhysicsMotionRead.PieceBytes + PhysicsMotionRead.VelocityOffset, 8f);
        var motion = PhysicsMotionRead.Decode(bytes, Bodies(body), new(15));
        Assert.True(motion.TrySample(body.Id, new(58, -2048), out var pose));
        // Each piece starts at its own committed COM; half a substep at 8 m/s is 1/120 m.
        Assert.InRange((pose.Cell.X + (double)pose.Local.X) / 16 - 1.0 / 120, -1e-7, 1e-7);
        Assert.True(motion.TrySample(body.Id, new(58), out var boundary));
        Assert.Equal(default(CellOrigin), boundary.Cell);
        Assert.Equal(default(LocalPosition), boundary.Local);
        Assert.False(motion.TrySample(body.Id, new(60), out _)); // PoseHistory owns the committed endpoint.
        var gap = (byte[])bytes.Clone();
        U32(gap, PhysicsMotionRead.HeaderBytes + PhysicsMotionRead.StartOrdinalOffset, 55);
        Assert.Throws<ArgumentException>(() => PhysicsMotionRead.Decode(gap, Bodies(body), new(15)));
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
        bytes[^1] = 1; // Nonzero unused storage is refused; the decoded snapshot retains its own input.
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
                Assert.True(snapshot.TrySample(body.Id, new((ulong)(ordinal + .5), -2048), out var pose));
                Assert.Equal(body.Id, pose.Id);
                Assert.Equal(CanonicalRotation.Identity, pose.Rotation);
                Assert.Equal(0, pose.Cell.Y); Assert.Equal(0f, pose.Local.Y);
                Assert.Equal(0, pose.Cell.Z); Assert.Equal(0f, pose.Local.Z);
                // This codec/interpolation check allows f32 rounding of x=v*t; it is not a scene-drift acceptance bound.
                Assert.InRange((pose.Cell.X + (double)pose.Local.X) / 16 - 8 * .5 / 480, -1e-7, 1e-7);
                Assert.True(changed.TrySample(body.Id, new((ulong)(ordinal + .5), -2048), out var faster));
                Assert.True((faster.Cell.X + (double)faster.Local.X) / 16 > (pose.Cell.X + (double)pose.Local.X) / 16);
            }
            Assert.False(snapshot.TrySample(new(2), new(1), out _));
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
            MotionBindingMismatch.Tick => read with { Tick = new(2), PhysicalRevision = new(2), Bodies = Bodies(body with { Tick = 2 }) },
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
        var read = new WorkshopRead(new(2), new(0), new(0), default, new(4),
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
        Assert.False(decoded.Motion.TrySample(body.Id, default, out _));
    }

    [Theory]
    [InlineData(ContactWorkEffect.Paid)]
    [InlineData(ContactWorkEffect.Passive)]
    public void ADevicePrivateContactOccurrenceMustOwnColliderAndInclusiveThreshold(ContactWorkEffect effect)
    {
        // Story 9A.23 D9: the occurrence rows are the device's ledger, never on the read or the wire; the scene binds them there.
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(
            WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("ball"),new(1),0,6,0,0,0,0,1),
            WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("bumper"),new(2),0,4,0,0,0,0,1)));
        var scene = WorkshopPhysicsCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, construction,new(11,23));
        var declaration = scene.ContactWorks[0];
        var colliders = scene.Colliders.ToArray();
        var owned = new ColliderSlot(checked((ushort)Array.FindIndex(colliders, c => c.Body == declaration.Owner)));
        var foreign = new ColliderSlot(checked((ushort)Array.FindIndex(colliders, c => c.Body == new GpuBodyId(1))));
        var body = new CanonicalBody(new(1),2,1,default,default,default);
        var store = new ContactWorkRead(declaration.Id,declaration.Owner,1,declaration.Store);
        var hit = new ContactWorkOccurrence(new(0),owned,new(1),1,1,0f,
            declaration.Threshold,new(effect == ContactWorkEffect.Paid ? 12 : 0),effect);
        ContactWorkLedger Ledger(ContactWorkOccurrence occurrence) => new([store], [occurrence]);
        Ledger(hit).ValidateScene(scene, Bodies(body));
        var initial = new ContactWorkLedger(
            new[] { new ContactWorkRead(declaration.Id,declaration.Owner,0,declaration.Store) },
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
        ContactWorkLedger Ledger(uint count, ContactWorkOccurrence row) =>
            new([new ContactWorkRead(new(9), new(8), count, new(6))], [row]);
        static PhysicsEnergyStoreRead Stores(float remaining, float released, float supplied = 0f) =>
            new([new(new(6), new(8), new(remaining, 0f), new(supplied), new(released), default, new(supplied), new(released), default, released > 0 || supplied > 0 ? 2u : 0u)]);
        var clear = new ContactWorkOccurrence(new(0), default, target, 0, 0, 0f, default, default, ContactWorkEffect.Passive);
        var hit = new ContactWorkOccurrence(new(0), new(3), target, 1, 16, 0f, new(4f), new(12f), ContactWorkEffect.Paid);
        var before = Ledger(0, clear);
        var after = Ledger(1, hit);
        after.ValidateAdvance(before);
        after.ValidateReleasedWork(before, Stores(20f, 12f), Stores(32f, 0f), default, default, []);
        Stores(20f, 12f).ValidateAdvance(Stores(32f, 0f));
        Stores(20.5f, 12f, .5f).ValidateAdvance(Stores(32f, 0f));
        Ledger(1, hit).ValidateAdvance(after);
        Assert.Throws<ArgumentException>(() => Stores(21f, 12f).ValidateAdvance(Stores(32f, 0f)));
        Assert.Throws<ArgumentException>(() => Stores(31f, 0f).ValidateAdvance(Stores(32f, 0f)));
        Assert.Throws<ArgumentException>(() => after.ValidateReleasedWork(before, Stores(21f, 11f), Stores(32f, 0f), default, default, []));
        foreach (var (name, invalid, previous) in new (string, ContactWorkLedger, ContactWorkLedger)[] {
            ("a counted hit has no row", Ledger(2, hit), before),
            ("a hit reversed in time", Ledger(2, hit with { Sequence = 2, EventOrdinal = 10 }), after),
            ("an unchanged row mutated", Ledger(1, hit with { Debit = new(11f) }), after) })
            Assert.True(Assert.Throws<ArgumentException>(() => invalid.ValidateAdvance(previous)) is not null, name);
        Assert.Throws<ArgumentException>(() => Ledger(1, hit with { Debit = default }));
        Assert.Throws<ArgumentException>(() => Ledger(1, hit with { Effect = ContactWorkEffect.Passive }));
    }

    private static WorkshopRead MotionRead(CanonicalBody body, PhysicsMotionRead motion) =>
        new(new(2), new(1), new(1), Bodies(body), new(4),
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
            U32(bytes, piece + PhysicsMotionRead.StartOrdinalOffset, start + i); U32(bytes, piece + PhysicsMotionRead.EndOrdinalOffset, start + i + 1);
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
