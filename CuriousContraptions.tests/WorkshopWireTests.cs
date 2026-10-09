using System.Buffers.Binary;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class WorkshopWireTests
{
    [Fact]
    public void PresentationDiagnosticBindsCadenceAndPulseWithinItsFixedRingBudget()
    {
        var sample = new WorkshopPresentationSample(default, null, new(0), PresentationQuality.AwaitingHistory,
            new(new(100), 1, 1, new(2), null, null, null));
        var scene = new PresentationScene(3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1);
        var bytes = WorkshopPresentationWire.Encode(sample, false, scene, new(101), new(7), new(17));
        Assert.Equal(352, bytes.Length);
        Assert.Equal(7UL, BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(336)));
        Assert.Equal(17UL, BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(344)));
        Assert.Equal(29_696, WorkshopPresentationWire.RetainedBytes + 128 * 144);
        Assert.True(WorkshopPresentationWire.RetainedBytes + 128 * 144 <= 32 * 1024);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WorkshopPresentationWire.Encode(sample, false, scene, new(101), default, new(17)));
    }

    [Fact]
    public void MasterClockProjectionKeepsBothBoundsAndDoesNotAuthorizeAnEarlyPulse()
    {
        var peer = new WorkshopClockPeer(Session, new(1), new(25_000_000), new(1), new(100_000), WorkshopRuntimeRole.Browser);
        var clock = new WorkshopClockMapping(peer);
        for (var i = 0; i < 8; i++)
        {
            var sent = i * 10_000_000L;
            var probe = clock.BeginProbe(new(sent))!.Value;
            Assert.Equal(ClockProbeOutcome.Accepted,
                clock.Receive(new(probe, new(sent + 50_000_000), new(sent + 50_000_000), new(sent + 500_000))).Outcome);
        }
        Assert.True(clock.TryMasterNow(new(74_800_000), out var master));
        Assert.Equal((Int128)98_997_850, master.Lower);
        Assert.Equal((Int128)100_102_150, master.Upper);
        Assert.Equal(new PulseOrdinal(5), WorkshopPulse.Due(new(60, 1), new(checked((long)master.Lower))));
        Assert.Equal(new PulseOrdinal(6), WorkshopPulse.Due(new(60, 1), new(checked((long)master.Upper))));
        clock.Retire();
        Assert.Throws<InvalidOperationException>(() => clock.TryMasterNow(new(74_800_001), out _));
    }

    [Fact]
    public void MasterPeerAndProbeBindEveryRequesterAndAuthorityIdentity()
    {
        var peer = new WorkshopClockPeer(Session, new(9), new(50_000_000), new(7), new(100_000), WorkshopRuntimeRole.Animation);
        var probe = new ClockProbe(Session, peer.Generation, peer.RequesterGeneration, peer.RequesterRole, new(1), new(100_000_000));
        var bytes = WorkshopClockWire.EncodeProbe(probe, peer);
        Assert.Equal(80, bytes.Length);
        Assert.Equal(probe, WorkshopClockWire.DecodeProbe(bytes, peer));
        foreach (var offset in new[] { 8, 24, 32, 40, 56, 60 })
        {
            var corrupt = (byte[])bytes.Clone(); corrupt[offset] ^= 1;
            Assert.ThrowsAny<ArgumentException>(() => WorkshopClockWire.DecodeProbe(corrupt, peer));
        }
        var reply = WorkshopClockWire.EncodeReply(bytes, peer, new(150_000_000), new(150_000_000));
        Assert.Throws<ArgumentException>(() => WorkshopClockWire.EncodeReply(bytes, peer, new(49_999_999), new(50_000_000)));
        var preOrigin = (byte[])reply.Clone();
        BinaryPrimitives.WriteInt64LittleEndian(preOrigin.AsSpan(80), 49_999_999);
        Assert.Throws<ArgumentException>(() => WorkshopClockWire.DecodeReply(preOrigin, peer, new(100_000_000)));
        var mapping = new WorkshopClockMapping(peer);
        Assert.Equal(probe, mapping.BeginProbe(new(100_000_000))!.Value);
        Assert.Throws<ArgumentException>(() => WorkshopClockWire.DecodeReply(preOrigin, peer, new(100_000_000)));
        Assert.Equal(probe, mapping.Pending!.Value);
        Assert.Equal(0, mapping.ValidProbeCount);
        Assert.Equal(ClockProbeOutcome.Accepted,
            mapping.Receive(WorkshopClockWire.DecodeReply(reply, peer, new(100_000_000))).Outcome);
        Assert.Equal(1, mapping.ValidProbeCount);
        Assert.Equal(96, reply.Length);
        Assert.Equal(new ClockReply(probe, new(150_000_000), new(150_000_000), new(100_000_000)),
            WorkshopClockWire.DecodeReply(reply, peer, new(100_000_000)));
        Assert.Throws<ArgumentException>(() => WorkshopClockWire.DecodePeer(new byte[40]));
        var oldVersion = WorkshopClockWire.EncodePeer(peer);
        BinaryPrimitives.WriteUInt32LittleEndian(oldVersion, 2);
        Assert.Throws<ArgumentException>(() => WorkshopClockWire.DecodePeer(oldVersion));
    }

    private static WorkshopSchedule SharedSchedule() => WorkshopSchedule.Create(
        WorkshopCadenceSettings.Default(), new(2), new(1), new(1000), new(50_000_000),
        new(41_666_667), new(41_666_667),
        new(new(1), new(3), new(0), new(50_000_000), WorldPlayback.Running));

    [Fact]
    public void SharedScheduleCommitBindsIdentitiesRoutesAndReservedFields()
    {
        var schedule = SharedSchedule();
        var header = new ScheduleControlHeader(ScheduleControlKind.Commit, Session, new(1), new(2), new(3),
            WorkshopRuntimeRole.Simulation, WorkshopRuntimeRole.Browser);
        var bytes = new byte[WorkshopScheduleWire.HeaderBytes + WorkshopScheduleWire.ScheduleBytes];
        WorkshopScheduleWire.WriteCommit(header, schedule, bytes);
        Assert.Equal(schedule, WorkshopScheduleWire.ReadCommit(bytes, out var decoded));
        Assert.Equal(header, decoded);
        foreach (var offset in new[] { 60, 64 + 124, 64 + 136 })
        {
            var malformed = (byte[])bytes.Clone(); malformed[offset] = 1;
            Assert.Throws<ArgumentException>(() => WorkshopScheduleWire.ReadCommit(malformed, out _));
        }
        foreach (var offset in new[] { 24, 32, 40 })
        {
            var wrongIdentity = (byte[])bytes.Clone();
            BinaryPrimitives.WriteUInt64LittleEndian(wrongIdentity.AsSpan(offset), 99);
            Assert.Throws<ArgumentException>(() => WorkshopScheduleWire.ReadCommit(wrongIdentity, out _));
        }
        var wrongRoute = (byte[])bytes.Clone();
        BinaryPrimitives.WriteUInt32LittleEndian(wrongRoute.AsSpan(48), (uint)WorkshopRuntimeRole.Animation);
        Assert.Throws<ArgumentException>(() => WorkshopScheduleWire.ReadCommit(wrongRoute, out _));
        Assert.Throws<ArgumentException>(() => WorkshopScheduleWire.ReadCommit(bytes.AsSpan(0, bytes.Length - 1), out _));
        Assert.Throws<ArgumentException>(() => WorkshopScheduleWire.ReadCommit(new byte[bytes.Length + 1], out _));
        var prior = (byte[])bytes.Clone();
        Assert.Throws<ArgumentException>(() => WorkshopScheduleWire.WriteCommit(header with { Revision = new(3) }, schedule, bytes));
        Assert.Equal(prior, bytes);
        Assert.Throws<ArgumentException>(() => (header with { Kind = ScheduleControlKind.Presented,
            Sender = WorkshopRuntimeRole.Animation, Recipient = WorkshopRuntimeRole.Simulation }).Validate());
    }

    [Fact]
    public void ScheduleAdmissionRejectsUnreachableNextPulseAndUnadmittedDisplay()
    {
        var basis = SharedSchedule();
        var schedule = basis with { Settings = basis.Settings with { Presentation = PresentationCadence.AdmittedDisplay } };
        schedule.ValidateDisplay(new(60, 1));
        Assert.Throws<ArgumentException>(() => schedule.ValidateDisplay(new(60000, 1001)));
        var late = new MasterTimeNanoseconds(long.MaxValue);
        Assert.Throws<OverflowException>(() => WorkshopSchedule.Create(schedule.Settings, new(3), new(1), new(0),
            late, new(0), new(0), schedule.World));
        Assert.Throws<OverflowException>(() => WorkshopSchedule.Create(schedule.Settings, new(3), new(1), new(5_000_000),
            new(long.MaxValue - 10_000_000), new(0), new(0), schedule.World));
        var prior = new byte[WorkshopScheduleWire.ScheduleBytes]; Array.Fill(prior, (byte)0xA5);
        var bytes = (byte[])prior.Clone();
        Assert.Throws<ArgumentException>(() => WorkshopScheduleWire.WriteSchedule(
            schedule with { FirstPresentation = new(1) }, bytes));
        Assert.Equal(prior, bytes);
    }

    [Fact]
    public void PhysicalDebtUsesUnclampedMasterElapsedAndStrictHundredMillisecondBoundary()
    {
        var schedule = SharedSchedule();
        Assert.False(schedule.ExceedsPhysicalDebt(new(0), new(150_000_000)));
        Assert.True(schedule.ExceedsPhysicalDebt(new(0), new(150_000_001)));
        Assert.Equal(new SimulationTick(3600), schedule.World.DueTick(schedule.Settings, new(31_050_000_000)));
        Assert.True(schedule.ExceedsPhysicalDebt(new(3599), new(31_050_000_000)));
        Assert.False((schedule with { World = schedule.World with { Playback = WorldPlayback.Paused } })
            .ExceedsPhysicalDebt(new(0), new(31_050_000_000)));
    }

    [Theory]
    [InlineData(90U, 11_111_111L, 11_111_112L)]
    [InlineData(144U, 6_944_444L, 6_944_445L)]
    public void SharedPulseUsesAbsoluteRationalDeadlines(uint frequency, long before, long first)
    {
        var rate = new PulseRate(frequency, 1);
        Assert.Equal(new PulseOrdinal(0), WorkshopPulse.Due(rate, new(before)));
        Assert.Equal(new PulseOrdinal(1), WorkshopPulse.Due(rate, new(first)));
        Assert.Equal(new MasterTimeNanoseconds(first), WorkshopPulse.Deadline(rate, new(1)));
        Assert.Equal(new PulseOrdinal(frequency), WorkshopPulse.Due(rate, new(1_000_000_000)));
        Assert.Equal(new MasterTimeNanoseconds(1_000_000_000), WorkshopPulse.Deadline(rate, new(frequency)));
        Assert.NotEqual(first * frequency, WorkshopPulse.Deadline(rate, new(frequency)).Value);
    }

    [Fact]
    public void SharedPulseRejectsUnrepresentableDeadlinesAndDoesNotRewindOnInstall()
    {
        var priorAppliedUpper = WorkshopPulse.Deadline(new(120, 1), new(5));
        Assert.Equal(41_666_667, priorAppliedUpper.Value);
        Assert.Equal(new PulseOrdinal(2), WorkshopPulse.FirstAfter(new(30, 1), new(50_000_000), priorAppliedUpper));
        Assert.Equal(new PulseOrdinal(4), WorkshopPulse.FirstAfter(new(30, 1), new(50_000_000), new(100_000_000)));
        Assert.Equal(new MasterTimeNanoseconds(66_666_667), WorkshopPulse.Deadline(new(30, 1), new(2)));
        Assert.Throws<OverflowException>(() => WorkshopPulse.Deadline(new(30, 1), new(ulong.MaxValue)));
        Assert.Equal(new PulseOrdinal(2_213_609_288_845), WorkshopPulse.Due(new(240, 1), new(long.MaxValue)));
        Assert.Throws<ArgumentOutOfRangeException>(() => WorkshopPulse.Due(new(60, 1), new(-1)));
        Assert.Throws<ArgumentException>(() => WorkshopPulse.FromNative(new(99), new(100)));
        Assert.Equal(new MasterTimeNanoseconds(1), WorkshopPulse.FromNative(new(101), new(100)));
    }

    [Theory]
    [InlineData(SimulationCadence.Hz60, 8U, 1800UL)]
    [InlineData(SimulationCadence.Hz120, 4U, 3600UL)]
    [InlineData(SimulationCadence.Hz240, 2U, 7200UL)]
    public void ConfiguredCommitsCoverTheSamePhysicalRun(SimulationCadence cadence, uint substeps, ulong ticks)
    {
        var settings = WorkshopCadenceSettings.Default() with { Simulation = cadence };
        settings.Validate();
        Assert.Equal(substeps, settings.PhysicalStepsPerCommit);
        Assert.Equal(ticks, settings.RunTickLimit);
        Assert.Equal(WorkshopCadenceSettings.PhysicalOrdinalLimit, ticks * substeps);
        Assert.Equal(new MasterTimeNanoseconds(30_000_000_000),
            WorkshopPulse.Deadline(settings.SimulationRate, new(ticks)));
    }

    [Fact]
    public void WorldPauseLeavesAutonomousMasterPulsesAdvancingAndResumeWaitsAWholePeriod()
    {
        var settings = WorkshopCadenceSettings.Default();
        var projection = new WorkshopWorldProjection(new(1), new(1), new(0), new(100_000_000), WorldPlayback.Running);
        Assert.Equal(new SimulationTick(0), projection.DueTick(settings, new(108_333_333)));
        Assert.Equal(new SimulationTick(1), projection.DueTick(settings, new(108_333_334)));
        var paused = new WorkshopWorldProjection(new(1), new(2), new(7), new(200_000_000), WorldPlayback.Paused);
        Assert.Equal(new SimulationTick(7), paused.DueTick(settings, new(10_000_000_000)));
        Assert.Equal(new PulseOrdinal(600), WorkshopPulse.Due(settings.AnimationRate, new(10_000_000_000)));
        var resumed = paused with { Epoch = new(3), AnchorMaster = new(10_000_000_000), Playback = WorldPlayback.Running };
        Assert.Equal(new SimulationTick(7), resumed.DueTick(settings, new(10_008_333_333)));
        Assert.Equal(new SimulationTick(8), resumed.DueTick(settings, new(10_008_333_334)));
        Assert.Equal(new SimulationTick(3600), resumed.DueTick(settings, new(long.MaxValue)));
        Assert.Throws<ArgumentException>(() => resumed.DueTick(settings, new(9_999_999_999)));
        Assert.Throws<ArgumentException>(() => (paused with { Playback = WorldPlayback.Completed }).Validate(settings));
    }

    [Fact]
    public void PauseSurvivesACommittedTickButNeverAChangedGenerationOrProjection()
    {
        var command = new WorkshopCommand(new(7), WorkshopCommandKind.Pause, new(3), default, null,
            RevisionKind: ExpectedRevisionKind.Any, Session: Session, Cadence: new(2), Projection: new(4));
        var decoded = WorkshopWire.DecodeCommand(WorkshopWire.Encode(command));
        Assert.True(WorkshopWire.Matches(decoded, new(3), new(42), Session, new(2), new(4)));
        Assert.False(WorkshopWire.Matches(decoded, new(4), new(42), Session, new(2), new(4)));
        Assert.False(WorkshopWire.Matches(decoded, new(3), new(42), new(99, 100), new(2), new(4)));
        Assert.False(WorkshopWire.Matches(decoded, new(3), new(42), Session, new(3), new(4)));
        Assert.False(WorkshopWire.Matches(decoded, new(3), new(42), Session, new(2), new(5)));
        var exact = decoded with { RevisionKind = ExpectedRevisionKind.Exact, Revision = new(41) };
        Assert.False(WorkshopWire.Matches(exact, new(3), new(42), Session, new(2), new(4)));
    }

    [Fact]
    public void ActiveDefaultsUse120SimulationAnd60AnimationPresentationWithoutDisplayRateAuthority()
    {
        var settings = WorkshopCadenceSettings.Default();
        Assert.Equal(SimulationCadence.Hz120, settings.Simulation);
        Assert.Equal(AnimationCadence.Hz60, settings.Animation);
        Assert.Equal(PresentationCadence.Hz60, settings.Presentation);
        Assert.Equal(new PulseRate(60, 1), settings.PresentationRate);
        var schedule = WorkshopSchedule.Create(settings, new(1), new(1), new(0), new(0), new(0), new(0),
            new(new(1), new(1), new(0), new(0), WorldPlayback.Building));
        schedule.ValidateDisplay(new(144, 1));
        Assert.Equal(new MasterTimeNanoseconds(16_666_667), WorkshopPulse.Deadline(settings.PresentationRate, schedule.FirstPresentation));
        // Future internal configuration remains typed; it does not mutate the active default.
        (settings with { Animation = AnimationCadence.Hz90 }).Validate();
        Assert.Equal(settings, WorkshopCadenceSettings.Default());
    }

    [Fact]
    public void SettingsBoundaryRejectsNoncanonicalRatiosUnknownEnumsVersionsAndPadding()
    {
        var settings = WorkshopCadenceSettings.Default() with { Presentation = PresentationCadence.AdmittedDisplay, PresentationRate = new(60000, 1001) };
        var bytes = new byte[WorkshopCadenceWire.ByteLength];
        WorkshopCadenceWire.Write(settings, bytes);
        Assert.Equal(settings, WorkshopCadenceWire.Read(bytes));
        Assert.Equal(WorkshopSettingsVersion.SharedMaster,
            (WorkshopSettingsVersion)BinaryPrimitives.ReadUInt32LittleEndian(bytes));
        Assert.Throws<ArgumentException>(() => new PulseRate(120, 2).Validate());
        Assert.Throws<ArgumentException>(() => new PulseRate(29, 1).Validate());
        Assert.Throws<ArgumentException>(() => new PulseRate(241, 1).Validate());
        Assert.Throws<ArgumentException>(() => new PulseRate(0, 0).Validate());
        Assert.Throws<ArgumentException>(() => (settings with { Presentation = PresentationCadence.Hz60 }).Validate());
        Assert.Throws<ArgumentException>(() => (settings with { Simulation = (SimulationCadence)99 }).Validate());
        Assert.Throws<ArgumentException>(() => (settings with { Physical = (PhysicalStepProfile)0 }).Validate());
        Assert.Throws<ArgumentException>(() => (settings with { Animation = (AnimationCadence)99 }).Validate());
        Assert.Throws<ArgumentException>(() => (settings with { Presentation = (PresentationCadence)99 }).Validate());
        foreach (var offset in new[] { 0, 28 })
        {
            var malformed = (byte[])bytes.Clone();
            BinaryPrimitives.WriteUInt32LittleEndian(malformed.AsSpan(offset), 99);
            Assert.Throws<ArgumentException>(() => WorkshopCadenceWire.Read(malformed));
        }
        Assert.Throws<ArgumentException>(() => WorkshopCadenceWire.Read(new byte[31]));
        Assert.Throws<ArgumentException>(() => WorkshopCadenceWire.Read(new byte[33]));
        var prior = (byte[])bytes.Clone();
        Assert.Throws<ArgumentException>(() =>
            WorkshopCadenceWire.Write(settings with { Simulation = (SimulationCadence)99 }, bytes));
        Assert.Equal(prior, bytes);
    }

    private static readonly WorkshopCadenceSettings Settings = WorkshopCadenceSettings.Default();
    private static readonly WorkshopGpuProfile Profile = new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1));
    private static readonly RuntimeSessionId Session = new(0x20000000000001, ulong.MaxValue);
    // Typed protocol fixture, not a numerical solver: one declared constant-motion interval.
    private static WorkshopRead Described(WorkshopRead read)
    {
        var bytes = new byte[PhysicsMotionRead.ByteLength];
        if (read.Tick.Value != 0)
        {
            var last = checked((uint)(read.Tick.Value * Profile.Substeps)); var first = last - Profile.Substeps;
            BinaryPrimitives.WriteUInt32LittleEndian(bytes, checked((uint)read.Bodies.Count * Profile.Substeps));
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), Profile.Substeps);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), first);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12), last);
            for (var bodyIndex = 0; bodyIndex < read.Bodies.Count; bodyIndex++)
            for (uint step = 0; step < Profile.Substeps; step++)
            {
                var value = read.Bodies[bodyIndex]; var body = value.Body;
                var pieceIndex = checked(bodyIndex * (int)Profile.Substeps + (int)step);
                var piece = bytes.AsSpan(PhysicsMotionRead.HeaderBytes + pieceIndex * PhysicsMotionRead.PieceBytes, PhysicsMotionRead.PieceBytes);
                BinaryPrimitives.WriteUInt32LittleEndian(piece, (uint)PhysicsMotionKind.FreePolynomial);
                BinaryPrimitives.WriteUInt32LittleEndian(piece[4..], first + step);
                BinaryPrimitives.WriteUInt32LittleEndian(piece[8..], first + step + 1);
                BinaryPrimitives.WriteUInt32LittleEndian(piece[12..], first);
                BinaryPrimitives.WriteUInt64LittleEndian(piece[24..], body.Id.Value);
                BinaryPrimitives.WriteInt32LittleEndian(piece[32..], body.Cell.X);
                BinaryPrimitives.WriteInt32LittleEndian(piece[36..], body.Cell.Y);
                BinaryPrimitives.WriteInt32LittleEndian(piece[40..], body.Cell.Z);
                static void H(Span<byte> target, int offset, Half number) =>
                    BinaryPrimitives.WriteUInt16LittleEndian(target[offset..], BitConverter.HalfToUInt16Bits(number));
                H(piece,22,(Half)WorkshopCadenceSettings.PhysicalFrequency); H(piece,48,body.Local.X); H(piece,50,body.Local.Y); H(piece,52,body.Local.Z);
                var q = value.Rotation;
                H(piece,56,q.X); H(piece,58,q.Y); H(piece,60,q.Z); H(piece,62,q.W);
                H(piece,64,body.Velocity.X); H(piece,66,body.Velocity.Y); H(piece,68,body.Velocity.Z);
                H(piece,72,value.AngularVelocity.X); H(piece,74,value.AngularVelocity.Y); H(piece,76,value.AngularVelocity.Z);
            }
        }
        return read with { Motion = PhysicsMotionRead.Decode(bytes, read.Bodies, read.Tick) };
    }

    [Fact]
    public void MotionPiecesAdmitTheWorkerAngularClampAndRejectBeyondIt()
    {
        // The worker clamps |omega| below 128 rad/s; the piece decode must admit 100 rad/s and reject (100,100,0) ≈ 141 rad/s.
        var body = new CanonicalBody(new(1), 1, 1, new(0, 9, 0), default, default);
        var read = new WorkshopRead(new(1), new(1), new(new[] { new PhysicsBodyRead(body, CanonicalRotation.Identity, new((Half)100, (Half)0, (Half)0), default) }));
        var described = Described(read);
        Assert.Equal((int)Profile.Substeps, described.Motion!.Count);
        var bytes = described.Motion.Bytes.ToArray();
        for (var i = 0; i < Profile.Substeps; i++)
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(PhysicsMotionRead.HeaderBytes + i * PhysicsMotionRead.PieceBytes + 74),
                BitConverter.HalfToUInt16Bits((Half)100));
        Assert.Throws<ArgumentException>(() => PhysicsMotionRead.Decode(bytes, read.Bodies, read.Tick));
    }

    private static PhysicsBodyReadSet Bodies(CanonicalBody body) =>
        new(new[] { new PhysicsBodyRead(body, CanonicalRotation.Identity, default, default) });
    private static WorkshopBall OnlyBall(WorkshopConstruction construction) =>
        construction.Instances.OfType<WorkshopBall>().Single();
    private static PhysicsBodyRead NamedBody(byte[] bytes, GpuBodyId id)
    {
        Assert.True(PhysicsGpuAbi.ReadDynamicBodies(bytes).TryGet(id, out var body));
        return body;
    }

    private static WorkshopRead Stamped(WorkshopRead read) => Described(read with { Capture = new(WorkshopClockDomain.SimulationMonotonic,
        new(1), new(100_000_000), new(100_000)) });
    private static bool AdmitRead(WorkshopClientCursor cursor, WorkshopResponse response)
    {
        var candidate = cursor.PrepareRead(response); cursor.Commit(candidate); return candidate.Applicable;
    }
    private static WorkshopDelivery AdmitAcknowledgement(WorkshopClientCursor cursor, WorkshopCommand command,
        WorkshopResponse response, ulong order, bool disposed)
    {
        var candidate = cursor.PrepareAcknowledgement(command, response, order, disposed);
        cursor.Commit(candidate); return new(response, candidate.Applicable);
    }
    private static WorkshopConstruction Construction => new(new(2), Settings, new(WorkshopInput.Basketball(new(0x20000000000001), 0, 4, 0, 0, 0, 0, 1)));


    [Fact]
    public void IsolatedNativeProfileRoundTripsAndRejectsOldOrUnprovenDeclarations()
    {
        var peer = new WorkshopClockPeer(Session, new(ulong.MaxValue), new(0), new(1), new(100_000), WorkshopRuntimeRole.Browser);
        var bytes = WorkshopClockWire.EncodePeer(peer);
        Assert.Equal(peer, WorkshopClockWire.DecodePeer(bytes));
        Assert.Equal(3U, BinaryPrimitives.ReadUInt32LittleEndian(bytes));
        Assert.Equal(NativeClockProfile.IsolatedWitnessedPerformanceNow,
            (NativeClockProfile)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4)));
        var oldVersion = (byte[])bytes.Clone();
        BinaryPrimitives.WriteUInt32LittleEndian(oldVersion, 1);
        Assert.Throws<ArgumentException>(() => WorkshopClockWire.DecodePeer(oldVersion));
        var oldProfile = (byte[])bytes.Clone();
        BinaryPrimitives.WriteUInt32LittleEndian(oldProfile.AsSpan(4), 1);
        Assert.Throws<ArgumentException>(() => WorkshopClockWire.DecodePeer(oldProfile));
        var unknown = (byte[])bytes.Clone();
        BinaryPrimitives.WriteUInt64LittleEndian(unknown.AsSpan(48), 100_001);
        Assert.Throws<ArgumentOutOfRangeException>(() => WorkshopClockWire.DecodePeer(unknown));
        Assert.Throws<ArgumentException>(() => WorkshopClockWire.EncodePeer(peer with { Uncertainty = new(99_999) }));
    }

    [Fact]
    public void RecoveryKeepsUsableCommittedHistoryAndAdvancesTheDisplay()
    {
        var peer = new WorkshopClockPeer(Session, new(1), new(0), new(1), new(100_000), WorkshopRuntimeRole.Browser);
        var clock = new WorkshopClockMapping(peer);
        void Probe(long sent, long roundtrip = 0)
        {
            var probe = clock.BeginProbe(new(sent))!.Value;
            Assert.Equal(ClockProbeOutcome.Accepted,
                clock.Receive(new(probe, new(sent + 50_000_000), new(sent + 50_000_000), new(sent + roundtrip))).Outcome);
        }
        for (var i = 0; i < 8; i++) Probe(i * 10_000_000);
        var history = new WorkshopPoseHistory();
        void Admit(ulong tick, long browserTime, PresentationBoundary boundary)
        {
            var capture = new WorkshopClockStamp(WorkshopClockDomain.SimulationMonotonic, peer.Generation,
                new(browserTime + 50_000_000), new(100_000));
            Assert.True(clock.TryMap(capture, new(browserTime), out var mapped));
            var response = new WorkshopResponse(new(0), WorkshopResponseKind.Read,
                new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), WorkshopSimulationPhase.Running,
                Described(new(new(1), new(tick), Bodies(new CanonicalBody(new(1), 1, tick, new(0, 10 - (int)tick, 0), default, default)),
                    new(tick + 1), capture)), peer.Session, new(tick + 1), new(1), new(1), new(1));
            if (boundary == PresentationBoundary.Run)
            {
                var schedule = WorkshopSchedule.Create(Settings, new(1), peer.Generation, peer.MasterOrigin,
                    new(browserTime + 50_000_000), default, default,
                    new(new(1), new(1), new(tick), new(browserTime + 50_000_000), WorldPlayback.Running));
                history.Commit(history.PrepareInstall(schedule, response, mapped, boundary, clock.DisplayEpoch));
            }
            else history.Commit(history.Prepare(response, mapped, boundary, clock.DisplayEpoch));
        }
        Admit(0, 317_000_000, PresentationBoundary.Run);
        Assert.True(history.TryPresent(new(317_000_000), new(367_000_000), clock, out _));
        var epoch = clock.DisplayEpoch;
        Probe(320_000_000, 1_295_000);
        Assert.Equal(ClockMappingState.Recovering, clock.State);
        Admit(1, 325_000_000, PresentationBoundary.Continuous);
        Admit(2, 333_000_000, PresentationBoundary.Continuous);
        Assert.True(history.TryPresent(new(347_000_000), new(397_000_000), clock, out var presented));
        Assert.Equal(PresentationQuality.Interpolated, presented.Quality);
        Assert.Equal(1UL, presented.Evidence.Before!.Value.Tick.Value);
        Assert.Equal(2UL, presented.Evidence.After!.Value.Tick.Value);
        Assert.Equal(epoch, clock.DisplayEpoch);
        Assert.Equal(ClockMappingState.Recovering, clock.State);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public void InstalledRunKeepsPhysicalCaptureAndProjectsNewAnchorRegardlessOfAckOrder(bool ackFirst, bool faultBeforeTick)
    {
        var peer = new WorkshopClockPeer(new(11, 23), new(1), new(0), new(1), new(0), WorkshopRuntimeRole.Browser);
        var clock = new WorkshopClockMapping(peer);
        for (var i = 0; i < 8; i++)
        {
            var time = new MonotonicNanoseconds(i * 10_000_000);
            var probe = clock.BeginProbe(time)!.Value;
            clock.Receive(new(probe, time, time, time));
        }
        var history = new WorkshopPoseHistory();
        var cursor = new WorkshopClientCursor(peer.Session);
        WorkshopResponse Response(ulong sequence, WorkshopResponseKind kind, ulong tick, ulong revision,
            long captured, WorkshopSimulationPhase phase, ulong publication = 0) =>
            new(new(sequence), kind, new(phase == WorkshopSimulationPhase.Faulted ? WorkshopCommandOutcome.Faulted : WorkshopCommandOutcome.Applied,
                phase == WorkshopSimulationPhase.Faulted ? WorkshopRejection.DeviceLost : WorkshopRejection.None), phase,
                Described(new(new(1), new(tick), Bodies(new CanonicalBody(new(1), 1, tick, new(0, 10 - (int)tick, 0), default, default)),
                    new(revision), new(WorkshopClockDomain.SimulationMonotonic, new(1), new(captured), new(0)))),
                peer.Session, new(publication), new(1), new(1), new(phase == WorkshopSimulationPhase.Building ? 1UL : 2UL));
        void Admit(WorkshopResponse response, PresentationBoundary boundary)
        {
            var stamp = response.Read.Capture!.Value.Time;
            var mapped = new MappedCapture(new(stamp.Value, stamp.Value), stamp, new(8));
            if (boundary is PresentationBoundary.Seed or PresentationBoundary.Run)
            {
                var playback = boundary == PresentationBoundary.Seed ? WorldPlayback.Building : WorldPlayback.Running;
                var at = boundary == PresentationBoundary.Seed ? 100_000_000L : 300_000_000L;
                var schedule = WorkshopSchedule.Create(Settings, new(1), peer.Generation, peer.MasterOrigin,
                    new(at), default, default, new(new(1), response.Projection, response.Read.Tick, new(at), playback));
                history.Commit(history.PrepareInstall(schedule, response, mapped, boundary, 1));
            }
            else history.Commit(history.Prepare(response, mapped, boundary, 1));
        }
        var building = Response(1, WorkshopResponseKind.Acknowledgement, 0, 1, 100_000_000, WorkshopSimulationPhase.Building);
        var initialized = new WorkshopCommand(new(1), WorkshopCommandKind.Initialize, new(1), default, null, Session: peer.Session, Settings: Settings);
        cursor.Commit(cursor.PrepareAcknowledgement(initialized, building, 0, false));
        Admit(building, PresentationBoundary.Seed);
        Assert.True(history.TryPresent(new(100_000_000), new(100_000_000), clock, out _));
        var command = new WorkshopCommand(new(2), WorkshopCommandKind.Run, cursor.Epoch, cursor.Revision, null, Session: peer.Session, Cadence: new(1), Projection: new(1));
        var order = cursor.ReadOrder;
        var run = Response(2, WorkshopResponseKind.Acknowledgement, 0, 2, 100_000_000, WorkshopSimulationPhase.Running);
        Admit(run, PresentationBoundary.Run); // Schedule commit precedes S publication and the command ACK.
        if (ackFirst)
        {
            var prepared = cursor.PrepareAcknowledgement(command, run, order, false);
            Assert.True(prepared.Applicable); cursor.Commit(prepared);
            Assert.True(history.TryPresent(new(300_000_000), new(300_000_000), clock, out var seed));
            Assert.Equal(100_000_000, seed.Evidence.Before!.Value.Capture.Time.Value);
        }
        var first = faultBeforeTick
            ? Response(0, WorkshopResponseKind.Read, 0, 3, 100_000_000, WorkshopSimulationPhase.Faulted, 1)
            : Response(0, WorkshopResponseKind.Read, 1, 3, 308_000_000, WorkshopSimulationPhase.Running, 1);
        cursor.Commit(cursor.PrepareRead(first)); Admit(first, PresentationBoundary.Continuous);
        if (!ackFirst)
        {
            var oldAck = cursor.PrepareAcknowledgement(command, run, order, false);
            Assert.False(oldAck.Applicable);
            Assert.Equal(faultBeforeTick ? 1 : 2, history.Count);
        }
        var selected = history.TryPresent(new(316_000_000), new(316_000_000), clock, out var held);
        Assert.Equal(!ackFirst && !faultBeforeTick, selected);
        if (selected)
        {
            // The schedule is installed independently of command ACK order. This branch
            // has not yet consumed its Run seed; the old physical capture is retained.
            Assert.Equal(PresentationQuality.Terminal, held.Quality);
            Assert.Equal(0UL, held.Evidence.Before!.Value.Tick.Value);
            Assert.Equal(100_000_000, held.Evidence.Before.Value.Capture.Time.Value);
            Assert.Equal(0, held.SimulationTime.Seconds);
            Assert.False(history.TryPresent(new(316_000_000), new(316_000_000), clock, out _));
        }
        if (faultBeforeTick)
        {
            Assert.Equal(PresentationQuality.Faulted, held.Quality);
            Assert.Equal(0, held.Bodies.Count); Assert.Null(held.Evidence.Before); Assert.Null(held.Evidence.After);
            Assert.Equal(100_000_000, held.Evidence.Latest!.Value.Capture.Time.Value);
            return;
        }
        var second = Response(0, WorkshopResponseKind.Read, 2, 4, 316_000_000, WorkshopSimulationPhase.Running, 2);
        cursor.Commit(cursor.PrepareRead(second)); Admit(second, PresentationBoundary.Continuous);
        Assert.True(history.TryPresent(new(329_000_000), new(329_000_000), clock, out var presented));
        Assert.Equal(1UL, presented.Evidence.Before!.Value.Tick.Value);
        Assert.Equal(2UL, presented.Evidence.After!.Value.Tick.Value);
        Assert.True(presented.Evidence.Before.Value.Capture.Time.Value >= 300_000_000);
    }

    [Fact]
    public void RejectedConstructionReadValidatesAgainstCurrentAdmittedPose()
    {
        // This is an admitted-world check; the shared codec fixture deliberately uses a wider ID domain.
        var current = Construction with { Instances = new(OnlyBall(Construction) with { Id = new(1) }) };
        var ball = OnlyBall(current);
        var read = Stamped(new(new(1), new(0), Bodies(new CanonicalBody(ball.Id, 1, 0, ball.Cell, ball.Local, default))));
        var proposed = current.WithInstance(ball with { Cell = new(32, 64, 0) }) with { Revision = new(3) };
        var currentScene = WorkshopPhysicsCompiler.Compile(current, new(Session.Low, Session.High));
        var proposedScene = WorkshopPhysicsCompiler.Compile(proposed, new(Session.Low, Session.High));
        read.Bodies.ValidateScene(currentScene, read.Epoch, read.Tick);
        Assert.Throws<ArgumentException>(() => read.Bodies.ValidateScene(proposedScene, read.Epoch, read.Tick));
        Assert.Equal(ball.Cell, read.Bodies[0].Body.Cell);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OriginalAckRemainsExactAfterNewerReadOrSameRevisionFault(bool sameRevisionFault)
    {
        var cursor = new WorkshopClientCursor(Session);
        var command = new WorkshopCommand(new(1), WorkshopCommandKind.Run, new(1), default, null, Session: Session, Cadence: new(1), Projection: new(1));
        var dispatched = cursor.ReadOrder;
        var ack = new WorkshopResponse(new(1), WorkshopResponseKind.Acknowledgement,
            new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), WorkshopSimulationPhase.Running, Stamped(new(new(1), new(0), default, new(1))), Session, Cadence: new(1), MasterGeneration: new(1), Projection: new(1));
        var original = WorkshopWire.Encode(ack);
        var later = new WorkshopResponse(default, WorkshopResponseKind.Read,
            new(sameRevisionFault ? WorkshopCommandOutcome.Faulted : WorkshopCommandOutcome.Applied,
                sameRevisionFault ? WorkshopRejection.DeviceLost : WorkshopRejection.None),
            sameRevisionFault ? WorkshopSimulationPhase.Faulted : WorkshopSimulationPhase.Running, Stamped(new(new(1), new(sameRevisionFault ? 0UL : 1UL), default, new(sameRevisionFault ? 1UL : 2UL))), Session, new(1), new(1), new(1), new(1));
        Assert.True(AdmitRead(cursor, later));
        var delivery = AdmitAcknowledgement(cursor, command, ack, dispatched, false);
        Assert.False(delivery.Applicable);
        Assert.Equal(original, WorkshopWire.Encode(delivery.Response));
        Assert.Equal(later.Read.Revision, cursor.Revision);
    }

    [Fact]
    public void FutureReadCannotInstallEpochAndOnlyOwnedSuccessCanAdvanceIt()
    {
        var cursor = new WorkshopClientCursor(Session);
        var future = new WorkshopResponse(default, WorkshopResponseKind.Read,
            new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), WorkshopSimulationPhase.Building, Stamped(new(new(2), default, default, new(1))), Session, new(1), new(1), new(1), new(1));
        Assert.Throws<ArgumentException>(() => AdmitRead(cursor, future));
        Assert.Equal(new SimulationEpoch(1), cursor.Epoch);
        Assert.Equal(default, cursor.Revision);
        var command = new WorkshopCommand(new(1), WorkshopCommandKind.Reset, new(1), default, null,
            RevisionKind: ExpectedRevisionKind.Any, Session: Session, Cadence: new(1), Projection: new(1));
        var ack = future with { Sequence = new(1), Kind = WorkshopResponseKind.Acknowledgement, Publication = default };
        Assert.Throws<ArgumentException>(() => AdmitAcknowledgement(cursor, command with { Kind = WorkshopCommandKind.Run }, ack, 0, false));
        Assert.True(AdmitAcknowledgement(cursor, command, ack, 0, false).Applicable);
        Assert.Equal(new SimulationEpoch(2), cursor.Epoch);
        var retired = AdmitAcknowledgement(cursor, command with { Epoch = new(2) },
            ack with { Read = ack.Read with { Epoch = new(3), Revision = new(2) } }, 0, true);
        Assert.False(retired.Applicable);
        Assert.Equal(WorkshopCommandOutcome.Applied, retired.Response.Result.Outcome);
        Assert.Equal(new SimulationEpoch(2), cursor.Epoch);
        var old = AdmitAcknowledgement(cursor, command, ack with { Read = ack.Read with { Epoch = new(1) } }, 0, false);
        Assert.False(old.Applicable);
        Assert.Equal(new SimulationEpoch(1), old.Response.Read.Epoch);
        Assert.Equal(WorkshopCommandOutcome.Applied, old.Response.Result.Outcome);
    }

    [Fact]
    public void SameEpochAckCannotMoveTickBackwardsEvenWithNewerRevision()
    {
        var cursor = new WorkshopClientCursor(Session);
        var read = new WorkshopResponse(default, WorkshopResponseKind.Read,
            new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), WorkshopSimulationPhase.Running, Stamped(new(new(1), new(7), default, new(8))), Session, new(1), new(1), new(1), new(1));
        Assert.True(AdmitRead(cursor, read));
        var command = new WorkshopCommand(new(2), WorkshopCommandKind.Run, new(1), new(8), null, Session: Session, Cadence: new(1), Projection: new(1));
        var ack = read with { Sequence = new(2), Kind = WorkshopResponseKind.Acknowledgement, Publication = default,
            Read = read.Read with { Tick = new(6), Revision = new(9) } };
        Assert.False(AdmitAcknowledgement(cursor, command, ack, cursor.ReadOrder, false).Applicable);
        Assert.Equal(new AuthorityRevision(8), cursor.Revision);
        Assert.False(AdmitRead(cursor, read with { Read = read.Read with { Tick = new(6), Revision = new(9) } }));
    }

    [Fact]
    public void AuthorityRevisionIsSeparateAndAnyControlSurvivesLaterTicks()
    {
        var exact = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(3), new(9), Construction, Session: Session, Cadence: new(1), Projection: new(1));
        Assert.True(WorkshopWire.Matches(exact, new(3), new(9), Session, new(1), new(1)));
        Assert.False(WorkshopWire.Matches(exact, new(3), new(10), Session, new(1), new(1)));
        var reset = new WorkshopCommand(new(2), WorkshopCommandKind.Reset, new(3), default, null,
            RevisionKind: ExpectedRevisionKind.Any, Session: Session, Cadence: new(1), Projection: new(1));
        Assert.True(WorkshopWire.Matches(WorkshopWire.DecodeCommand(WorkshopWire.Encode(reset)), new(3), new(1234), Session, new(1), new(1)));
        Assert.False(WorkshopWire.Matches(reset, new(4), new(1234), Session, new(1), new(1)));
        Assert.Throws<ArgumentException>(() => WorkshopWire.Encode(reset with { Revision = new(1) }));
        Assert.Throws<ArgumentException>(() => WorkshopWire.Encode(exact with { Revision = default, RevisionKind = ExpectedRevisionKind.Any }));
        var response = new WorkshopResponse(new(1), WorkshopResponseKind.Acknowledgement,
            new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), WorkshopSimulationPhase.Building, Stamped(new(new(3), new(0), default, new(ulong.MaxValue))), Session, Cadence: new(1), MasterGeneration: new(1), Projection: new(1));
        var bytes = WorkshopWire.Encode(response);
        Assert.Equal(23952, bytes.Length);
        Assert.Equal(response, WorkshopWire.DecodeResponse(bytes));
        bytes[112] = 1;
        Assert.Throws<ArgumentException>(() => WorkshopWire.DecodeResponse(bytes));
    }

    [Fact]
    public void CancelWireRetainsExactOriginalIdentityAndRejectsMissingTarget()
    {
        var identity = new WorkshopCommandIdentity(new(0x20000000000001), new(19), new(23), new(1), new(1));
        var command = new WorkshopCommand(new(8), WorkshopCommandKind.Cancel, new(19), new(23), null, identity, Session: Session, Cadence: new(1), Projection: new(1));
        Assert.Equal(command, WorkshopWire.DecodeCommand(WorkshopWire.Encode(command)));
        Assert.Throws<ArgumentException>(() => WorkshopWire.Encode(command with { Target = null }));
        var bytes = WorkshopWire.Encode(command);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(56), 0);
        Assert.Throws<ArgumentException>(() => WorkshopWire.DecodeCommand(bytes));
    }

    [Fact]
    public void InvalidQuaternionAndOutOfDeckDescriptorReject()
    {
        Assert.Throws<ArgumentException>(() => new CanonicalRotation((Half)1, (Half)1, (Half)1, (Half)1).Validate());
        Assert.Throws<ArgumentException>(() => new CanonicalRotation((Half).001, (Half)0, (Half)0, (Half)0).Validate());
        var bytes = GenericAdmission();
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(DynamicOffset(bytes) + 16), 2048);
        Assert.ThrowsAny<ArgumentException>(() => PhysicsGpuAbi.ReadDynamicBodies(bytes));
    }

    [Fact]
    public void CanonicalConstructionWireRetainsEveryBitAndWideIdentity()
    {
        var ball = OnlyBall(Construction) with
        {
            Local = new(BitConverter.UInt16BitsToHalf(0x8000), (Half)0.25, (Half)0),
            Rotation = new(BitConverter.UInt16BitsToHalf(0x8000), (Half)0, (Half)0, (Half)1)
        };
        var command = new WorkshopCommand(new(0x20000000000001), WorkshopCommandKind.Construct,
            new(0x30000000000001), new(1), Construction.WithoutInstance(OnlyBall(Construction).Id).WithInstance(ball), Session: Session, Cadence: new(1), Projection: new(1));
        var bytes = WorkshopWire.Encode(command);
        Assert.Equal(bytes, WorkshopWire.Encode(WorkshopWire.DecodeCommand(bytes)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ObsoleteResponseSchemaRejectsEmptyAndBall(bool withBall)
    {
        var body = withBall ? new CanonicalBody(new(1), 3, 8, default, default, default) : (CanonicalBody?)null;
        var response = new WorkshopResponse(new(0), WorkshopResponseKind.Read,
            new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), WorkshopSimulationPhase.Running, Stamped(new(new(3), new(8), body.HasValue ? Bodies(body.Value) : default)), Session, new(1), new(1), new(1), new(1), new(8));
        var bytes = WorkshopWire.Encode(response);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12), 15);
        Assert.Throws<ArgumentException>(() => WorkshopWire.DecodeResponse(bytes));
    }


    private static PhysicsSceneDeclaration GenericScene => WorkshopPhysicsCompiler.Compile(
        Construction.WithoutInstance(OnlyBall(Construction).Id).WithInstance(OnlyBall(Construction) with { Id = new(1) }), new(101, 206));
    [Fact]
    public void AuthoredCompilerRejectsWideBodyIdentityWithoutChangingWireIdentityDomain()
    {
        Assert.Throws<ArgumentException>(() => WorkshopPhysicsCompiler.Compile(Construction, new(101, 206)));
        var command = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(1), new(1),
            Construction, Session: Session, Cadence: new(1), Projection: new(1));
        Assert.Equal(OnlyBall(Construction).Id, OnlyBall(WorkshopWire.DecodeCommand(WorkshopWire.Encode(command)).Construction!.Value).Id);
    }

    private static byte[] GenericAdmission() => PhysicsGpuAbi.Admission(GenericScene, new(2), Profile);
    private static int DynamicOffset(byte[] bytes)
    {
        var count = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(12));
        for (var i = 0; i < count; i++)
        {
            var offset = PhysicsGpuAbi.BodiesOffset + checked((int)i) * PhysicsGpuAbi.BodyBytes;
            if (BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(offset)) == 1) return offset;
        }
        throw new InvalidOperationException("Fixture dynamic body identity is missing.");
    }
    private static void WriteHalf(byte[] bytes, int offset, Half value) =>
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset), BitConverter.HalfToUInt16Bits(value));

    [Fact]
    public void CurrentGenericDescriptorPreservesConstructionAndRejectsRetiredShapes()
    {
        var bytes = GenericAdmission();
        PhysicsGpuAbi.ValidateCandidate(bytes, bytes, new(0));
        var body = NamedBody(bytes, new(1));
        Assert.Equal(OnlyBall(Construction).Cell, body.Body.Cell);
        Assert.Equal(OnlyBall(Construction).Rotation, body.Rotation);
        Assert.Equal(Profile, PhysicsGpuAbi.ReadProfile(bytes));
        Assert.Equal(PhysicsFailure.None, PhysicsGpuAbi.ReadFailure(bytes));
        Assert.Equal(0, PhysicsMotionRead.Decode(bytes.AsSpan(PhysicsGpuAbi.MotionOffset, PhysicsMotionRead.ByteLength), new(new[] { body }), new(0)).Count);
        foreach (var length in new[] { 80, 128, 144, 8832, PhysicsGpuAbi.ByteLength - 1, PhysicsGpuAbi.ByteLength + 1 })
            Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadDynamicBodies(new byte[length]));
        var reset = PhysicsGpuAbi.Admission(GenericScene, new(3), Profile);
        Assert.Equal(3UL, NamedBody(reset, new(1)).Body.Epoch);
        Assert.Equal(body.Rotation, NamedBody(reset, new(1)).Rotation);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(.45)]
    [InlineData(1)]
    public void GuideDeclarationPreservesAuthoredKnotsAndRejectsCandidateMutation(double precision)
    {
        var construction = FirstPrinciples.Create(new(1), WorkshopCadenceSettings.Default(), new(1), new(2), new((Half)precision));
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        Assert.Equal(1, scene.Guides.Length);
        var guide = scene.Guides[0];
        var knot = construction.Puzzle.ReceiverAssistance.Evaluate(construction.Puzzle.Precision);
        Assert.Equal(construction.Puzzle.Goal.Body, guide.Target);
        Assert.Equal(construction.Puzzle.Goal.Target, guide.Frame);
        Assert.Equal(knot.GuideAcceleration, guide.MaximumAcceleration);
        Assert.Equal(knot.CaptureMargin, guide.SupportMargin);
        Assert.Equal(new MetreVector((Half)(-1.1), (Half).5, (Half)(-1.1)), guide.Minimum);
        Assert.Equal(new MetreVector((Half)1.1, (Half)1.5, (Half)1.1), guide.Maximum);
        var bytes = PhysicsGpuAbi.Admission(scene, new(2), Profile);
        PhysicsGpuAbi.ValidateCandidate(bytes, bytes, new(0));
        Assert.Equal((uint)PhysicsStateVersion.GenericMechanical, BinaryPrimitives.ReadUInt32LittleEndian(bytes));
        Assert.Equal(1u, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(96)));
        for (var offset = PhysicsGpuAbi.GuidesOffset; offset < PhysicsGpuAbi.MotionOffset; offset++)
        {
            var changed = (byte[])bytes.Clone(); changed[offset] ^= 1;
            Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(changed, bytes, new(0)));
        }
    }

    [Fact]
    public void GuideAdmissionRejectsUnsupportedOrAmbiguousForceOwnership()
    {
        var scene = WorkshopPhysicsCompiler.Compile(FirstPrinciples.Create(new(1), WorkshopCadenceSettings.Default(), new(1), new(2), new((Half)0)), new(1, 2));
        var guide = scene.Guides[0];
        PhysicsSceneDeclaration Replace(params PlanarGuideDeclaration[] values) => new(scene.Document, scene.NextIdentity,
            scene.Bodies, scene.Colliders, scene.Materials, scene.Sensors, values);
        Assert.Throws<ArgumentException>(() => Replace(guide with { MaximumAcceleration = new((Half)13) }));
        Assert.Throws<ArgumentException>(() => Replace(guide with { SupportMargin = new(Half.NaN) }));
        Assert.Throws<ArgumentException>(() => Replace(guide with { Frame = guide.Target }));
        Assert.Throws<ArgumentException>(() => Replace(guide with { Target = guide.Frame }));
        Assert.Throws<ArgumentException>(() => Replace(guide with { Target = new(99) }));
        Assert.Throws<ArgumentException>(() => Replace(guide, guide with { Id = new(guide.Id.Value + 1) }));
        var input = new[] { guide }; var copied = Replace(input); input[0] = default;
        Assert.Equal(guide, copied.Guides[0]);
        Assert.Empty(GenericScene.Guides.ToArray());
        var bytes = PhysicsGpuAbi.Admission(scene, new(2), Profile);
        foreach (var (offset, value) in new[] { (0, 3u), (0, 6u), (96, (uint)PhysicsSceneDeclaration.GuideCapacity + 1), (100, (uint)PhysicsSceneDeclaration.TriggerCapacity + 1), (104, (uint)PhysicsSceneDeclaration.ContactWorkCapacity + 1), (108, (uint)PhysicsContactWorkRead.OccurrenceCapacity + 1), (116, (uint)PhysicsSceneDeclaration.OrientationSensorCapacity + 1), (120, 1u) })
        {
            var changed = (byte[])bytes.Clone(); BinaryPrimitives.WriteUInt32LittleEndian(changed.AsSpan(offset), value);
            Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadDynamicBodies(changed));
        }
        var unownedEvent = (byte[])bytes.Clone();
        BinaryPrimitives.WriteUInt32LittleEndian(unownedEvent.AsSpan(108), 1);
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadContactWorks(unownedEvent));
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(unownedEvent, bytes, new(0)));
    }

    [Theory]
    [InlineData(0, 0u)]
    [InlineData(0, 2u)]
    [InlineData(4, 1u)]
    [InlineData(8, uint.MaxValue)]
    [InlineData(12, uint.MaxValue)]
    [InlineData(28, uint.MaxValue)]
    [InlineData(32, 3u)]
    [InlineData(48, uint.MaxValue)]
    [InlineData(56, 2u)]
    [InlineData(96, 1u)]
    public void GenericCandidateRejectsChangedHeaderIdentityOrPadding(int offset, uint value)
    {
        var source = GenericAdmission(); var candidate = (byte[])source.Clone();
        BinaryPrimitives.WriteUInt32LittleEndian(candidate.AsSpan(offset), value);
        Assert.ThrowsAny<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(candidate, source, new(0)));
        PhysicsGpuAbi.ValidateCandidate(source, source, new(0));
    }

    [Fact]
    public void GenericCacheRejectsEveryChangedInactiveOrImmutableByte()
    {
        var source = GenericAdmission();
        Assert.True(BinaryPrimitives.ReadUInt32LittleEndian(source.AsSpan(112)) > 0);
        PhysicsGpuAbi.ValidateCandidate(source, source, new(0));
        for (var offset = 0; offset < PhysicsGpuAbi.CacheBytes; offset++)
        {
            if (offset == 24) continue; // The enabled selector has its separate active-record domain.
            foreach (var bit in new byte[] { 1, 0x80 })
            {
                var changed = (byte[])source.Clone();
                changed[PhysicsGpuAbi.CacheOffset + offset] ^= bit;
                Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(changed, source, new(0)));
            }
        }
        var invalidEnabled = (byte[])source.Clone();
        invalidEnabled[PhysicsGpuAbi.CacheOffset + 24] = 2;
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(invalidEnabled, source, new(0)));
    }

    [Fact]
    public void GenericCandidateBindsImmutableDeclarationsAndVectorDomain()
    {
        var source = GenericAdmission();
        foreach (var offset in new[] { DynamicOffset(source), DynamicOffset(source) + 64,
            PhysicsGpuAbi.CollidersOffset + 40, PhysicsGpuAbi.MaterialsOffset + 8 })
        {
            var changed = (byte[])source.Clone(); changed[offset] ^= 1;
            Assert.ThrowsAny<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(changed, source, new(0)));
        }
        var velocity = (byte[])source.Clone();
        WriteHalf(velocity, DynamicOffset(source) + 48, (Half)2);
        WriteHalf(velocity, DynamicOffset(source) + 50, (Half)2);
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(velocity, source, new(0)));
        var segment = (byte[])source.Clone();
        WriteHalf(segment, DynamicOffset(source) + 104, (Half)2);
        WriteHalf(segment, DynamicOffset(source) + 106, (Half)2);
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(segment, source, new(0)));
    }

    private static byte[] GenericEndpoint(ulong tick, uint origin, Half phase)
    {
        var bytes = GenericAdmission(); var offset = DynamicOffset(bytes);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(40), tick);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(88), checked((uint)(tick * Profile.Substeps)));
        var body = NamedBody(bytes, new(1));
        var read = Described(new(new(2), new(tick), new(new[] { body })));
        read.Motion!.Bytes.CopyTo(bytes.AsSpan(PhysicsGpuAbi.MotionOffset));
        for (var i = 0; i < Profile.Substeps; i++)
        {
            var piece = PhysicsGpuAbi.MotionOffset + PhysicsMotionRead.HeaderBytes + i * PhysicsMotionRead.PieceBytes;
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(piece + 12), origin);
            WriteHalf(bytes, piece + 20, phase);
        }
        return bytes;
    }

    [Theory]
    [InlineData(2UL, 4u, -2048, true)]
    [InlineData(2UL, 4u, 2048, false)]
    [InlineData(2UL, 4u, 0, true)]
    [InlineData(2UL, 4u, 1, false)]
    [InlineData(16UL, 4u, -1, false)]
    [InlineData(16UL, 4u, 0, true)]
    [InlineData(16UL, 4u, 1, true)]
    [InlineData(16UL, 3u, 0, false)]
    public void GenericMotionAnchorKeepsCanonicalPhaseAndBoundedAge(ulong tick, uint origin, int phase, bool accepted)
    {
        var source = GenericAdmission(); var candidate = GenericEndpoint(tick, origin, (Half)phase);
        if (accepted) PhysicsGpuAbi.ValidateCandidate(candidate, source, new(tick));
        else Assert.ThrowsAny<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(candidate, source, new(tick)));
    }

    [Fact]
    public void GenericMotionMustMatchWorldProfileAndCoverItsWholeCommit()
    {
        var source = GenericAdmission(); var candidate = GenericEndpoint(1, 0, (Half)0);
        PhysicsGpuAbi.ValidateCandidate(candidate, source, new(1));
        var motion = PhysicsGpuAbi.MotionOffset;
        // Internally valid two-substep motion cannot describe this four-substep world.
        var wrongProfile = (byte[])candidate.Clone();
        BinaryPrimitives.WriteUInt32LittleEndian(wrongProfile.AsSpan(motion + 4), 2);
        BinaryPrimitives.WriteUInt32LittleEndian(wrongProfile.AsSpan(motion + 12), 2);
        BinaryPrimitives.WriteUInt32LittleEndian(wrongProfile.AsSpan(motion + PhysicsMotionRead.HeaderBytes + 8), 2);
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(wrongProfile, source, new(1)));
        foreach (var offset in new[] { motion + PhysicsMotionRead.HeaderBytes + 4, // missing beginning
            motion + PhysicsMotionRead.HeaderBytes + 24, // wrong body
            motion + PhysicsMotionRead.HeaderBytes + 102, // padding
            motion + PhysicsMotionRead.HeaderBytes + checked((int)Profile.Substeps) * PhysicsMotionRead.PieceBytes }) // unused tail
        {
            var changed = (byte[])candidate.Clone(); changed[offset] ^= 1;
            Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(changed, source, new(1)));
        }
    }

    [Fact]
    public void ForceDrivenMotionSamplesQuadraticAndAcceptsGameGradeResiduals()
    {
        var state = GenericEndpoint(1, 0, (Half)0);
        var body = NamedBody(state, new(1)).Body;
        var bytes = state.AsSpan(PhysicsGpuAbi.MotionOffset, PhysicsMotionRead.ByteLength).ToArray();
        var seed = bytes.AsSpan(PhysicsMotionRead.HeaderBytes, PhysicsMotionRead.PieceBytes).ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, 4);
        for (var index = 0; index < 4; index++)
        {
            var offset = PhysicsMotionRead.HeaderBytes + index * PhysicsMotionRead.PieceBytes;
            seed.CopyTo(bytes, offset);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), (uint)PhysicsMotionKind.ForceDrivenQuadratic);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset + 4), (uint)index);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset + 8), (uint)index + 1);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset + 12), (uint)index);
            WriteHalf(bytes, offset + 88, (Half)32);
            WriteHalf(bytes, offset + 104, (Half).5);
            WriteHalf(bytes, offset + 106, (Half).002);
            WriteHalf(bytes, offset + 108, (Half).00006103515625);
        }
        var motion = PhysicsMotionRead.Decode(bytes, Bodies(body), new(1));
        Assert.True(motion.TrySample(body.Id, .5, out var pose));
        Assert.Equal(0, pose.Cell.X);
        Assert.Equal((Half)(16 * .5 * 32 * Math.Pow(.5 / 480, 2)), pose.Local.X);
        var unknown = (byte[])bytes.Clone(); BinaryPrimitives.WriteUInt32LittleEndian(unknown.AsSpan(PhysicsMotionRead.HeaderBytes), 4);
        Assert.Throws<ArgumentException>(() => PhysicsMotionRead.Decode(unknown, Bodies(body), new(1)));
    }

    [Fact]
    public void RoundedGpuQuaternionDoesNotTripAuthoredInputNormGate()
    {
        var rotation = new CanonicalRotation((Half).57861328125, (Half)(-.57275390625),
            (Half).55712890625, (Half).1676025390625);
        rotation.ValidateCommitted();
        Assert.Throws<ArgumentException>(() => rotation.Validate());
        var state = GenericEndpoint(1, 0, (Half)0);
        var values = new[] { rotation.X, rotation.Y, rotation.Z, rotation.W };
        for (var i = 0; i < 4; i++) WriteHalf(state, DynamicOffset(state) + 40 + i * 2, values[i]);
        Assert.Equal(rotation, NamedBody(state, new(1)).Rotation);
        Assert.Throws<ArgumentException>(() => (rotation with { X = Half.NaN }).ValidateCommitted());
        Assert.Throws<ArgumentException>(() => default(CanonicalRotation).ValidateCommitted());
    }

    public enum CanonicalPaddingRegion { UnusedBodies, UnusedSensors, UnusedMotion, BodyPadding, MotionPadding }

    [Theory]
    [InlineData(CanonicalPaddingRegion.UnusedBodies)]
    [InlineData(CanonicalPaddingRegion.UnusedSensors)]
    [InlineData(CanonicalPaddingRegion.UnusedMotion)]
    [InlineData(CanonicalPaddingRegion.BodyPadding)]
    [InlineData(CanonicalPaddingRegion.MotionPadding)]
    public void GenericCandidateRejectsEveryBoundaryOfZeroRegions(CanonicalPaddingRegion region)
    {
        var source = GenericAdmission();
        var candidate = GenericEndpoint(1, 0, (Half)0);
        var bodyTail = PhysicsGpuAbi.BodiesOffset +
            checked((int)BinaryPrimitives.ReadUInt32LittleEndian(candidate.AsSpan(12))) * PhysicsGpuAbi.BodyBytes;
        var sensorTail = PhysicsGpuAbi.SensorsOffset +
            checked((int)BinaryPrimitives.ReadUInt32LittleEndian(candidate.AsSpan(24))) * PhysicsGpuAbi.SensorBytes;
        var motionTail = PhysicsGpuAbi.MotionOffset + PhysicsMotionRead.HeaderBytes +
            checked((int)BinaryPrimitives.ReadUInt32LittleEndian(candidate.AsSpan(PhysicsGpuAbi.MotionOffset))) * PhysicsMotionRead.PieceBytes;
        var (start, length) = region switch
        {
            CanonicalPaddingRegion.UnusedBodies => (bodyTail, PhysicsGpuAbi.CollidersOffset - bodyTail),
            CanonicalPaddingRegion.UnusedSensors => (sensorTail, PhysicsGpuAbi.GuidesOffset - sensorTail),
            CanonicalPaddingRegion.UnusedMotion => (motionTail, PhysicsGpuAbi.CacheOffset - motionTail),
            CanonicalPaddingRegion.BodyPadding => (DynamicOffset(candidate) + 124, 4),
            CanonicalPaddingRegion.MotionPadding => (PhysicsGpuAbi.MotionOffset + PhysicsMotionRead.HeaderBytes + 124, 4),
            _ => throw new ArgumentOutOfRangeException(nameof(region))
        };
        Assert.True(length > 0);
        var originalMotion = PhysicsGpuAbi.ValidateCandidate(candidate, source, new(1));
        foreach (var offset in new[] { start, start + length / 2, start + length - 1 })
        foreach (var value in new byte[] { 1, 0x80, 0xff })
        {
            var changed = (byte[])candidate.Clone();
            changed[offset] = value; // 0x80 also exercises a Half negative-zero sign bit in raw-zero storage.
            Assert.ThrowsAny<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(changed, source, new(1)));
        }
        Assert.True(originalMotion.Motion.Bytes.SequenceEqual(PhysicsGpuAbi.ValidateCandidate(candidate, source, new(1)).Motion.Bytes));
    }

    [Theory]
    [InlineData(0x100u)]
    [InlineData(0x101u)]
    public void UnknownFullWidthCaptureDiscriminantRejects(uint phase)
    {
        var read = Stamped(new(new(2), new(0), default, Captures: new([CaptureLatch.Clear(new(9))])));
        var response = new WorkshopResponse(new(1), WorkshopResponseKind.Acknowledgement,
            new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), WorkshopSimulationPhase.Building,
            read, Session, default, new(1), new(1), new(1));
        var bytes = WorkshopWire.Encode(response);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(WorkshopWire.ReadCapturesOffset + 8), phase);
        Assert.Throws<ArgumentException>(() => WorkshopWire.DecodeResponse(bytes));
    }

    [Fact]
    public void ResponseWriterRejectsInvalidBodyWithoutChangingReservedOutput()
    {
        var body = new CanonicalBody(new(1), 2, 0, default, default, default);
        var read = Stamped(new(new(2), new(0), Bodies(body)));
        var response = new WorkshopResponse(new(1), WorkshopResponseKind.Acknowledgement,
            new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), WorkshopSimulationPhase.Building,
            read, Session, default, new(1), new(1), new(1));
        var bytes = WorkshopWire.Encode(response); var before = (byte[])bytes.Clone();
        Assert.Throws<ArgumentException>(() => WorkshopWire.Write(response with { Read = read with { Bodies = Bodies(body with { Epoch = 3 }) } }, bytes));
        Assert.Equal(before, bytes);
    }

    [Fact]
    public async Task ActualRouterReplaysOnlyExactPendingAndCompletedCommand()
    {
        var held = new TaskCompletionSource<byte[]>();
        var applied = new List<WorkshopCommand>();
        var router = new WorkshopCommandRouter((command, reserved) => { applied.Add(command); return held.Task; });
        var command = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(1), new(1), Construction, Session: Session, Cadence: new(1), Projection: new(1));
        var bytes = WorkshopWire.Encode(command);
        var first = router.Dispatch(bytes);
        var duplicate = router.Dispatch((byte[])bytes.Clone());
        Assert.Same(first, duplicate);
        await Assert.ThrowsAsync<ArgumentException>(() => router.Dispatch(WorkshopWire.Encode(command with { Kind = WorkshopCommandKind.Run, Construction = null })));
        await Assert.ThrowsAsync<ArgumentException>(() => router.Dispatch(WorkshopWire.Encode(command with { Epoch = new(2) })));
        var different = command with { Construction = Construction.WithoutInstance(OnlyBall(Construction).Id) };
        await Assert.ThrowsAsync<ArgumentException>(() => router.Dispatch(WorkshopWire.Encode(different)));
        bytes[16] ^= 1; // caller cannot change the retained identity after admission.
        await Assert.ThrowsAsync<ArgumentException>(() => router.Dispatch(bytes));
        held.SetResult([1, 2, 3]);
        Assert.Equal(new byte[] { 1, 2, 3 }, await first);
        Assert.Same(first, router.Dispatch(WorkshopWire.Encode(command)));
        Assert.Single(applied);
    }

    [Fact]
    public async Task MalformedAndOutOfOrderInputsDoNotConsumeNextReliableIdentity()
    {
        var seen = new List<CommandSequence>();
        var router = new WorkshopCommandRouter((command, reserved) => { seen.Add(command.Sequence); return Task.FromResult(Array.Empty<byte>()); });
        var next = new WorkshopCommand(new(1), WorkshopCommandKind.Run, new(1), new(1), null, Session: Session, Cadence: new(1), Projection: new(1));
        var malformed = WorkshopWire.Encode(next);
        BinaryPrimitives.WriteUInt32LittleEndian(malformed.AsSpan(12), 99);
        await Assert.ThrowsAsync<ArgumentException>(() => router.Dispatch(malformed));
        await Assert.ThrowsAsync<ArgumentException>(() => router.Dispatch(WorkshopWire.Encode(next with { Sequence = new(2) })));
        await router.Dispatch(WorkshopWire.Encode(next));
        Assert.Equal(new[] { new CommandSequence(1) }, seen);
    }

    private static WorkshopMemoryRequest MemoryRequest => new(WorkshopMemoryKind.PostGc, WorkshopMemoryContext.Simulation,
        Session, new(ulong.MaxValue), new(ulong.MaxValue), new(ulong.MaxValue), new(ulong.MaxValue));

    [Fact]
    public void StoppedMemoryWirePreservesExactWideIdentitiesAndSeparateContext()
    {
        var bytes = WorkshopMemoryWire.Encode(MemoryRequest);
        Assert.Equal(64, bytes.Length);
        Assert.Equal(MemoryRequest, WorkshopMemoryWire.DecodeRequest(bytes));
        Assert.Equal(ulong.MaxValue, BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(32)));
        var sample = new WorkshopMemoryResult(MemoryRequest, WorkshopMemoryStatus.Accepted,
            1234, 1200, 19, 1_000_000_000, 5000, 5050, 6, 7);
        var result = WorkshopMemoryWire.Encode(sample);
        Assert.Equal(128, result.Length);
        Assert.Equal(sample, WorkshopMemoryWire.DecodeResult(result));
        Assert.Equal(sample with { Request = MemoryRequest with { Context = WorkshopMemoryContext.Browser } },
            WorkshopMemoryWire.DecodeResult(WorkshopMemoryWire.Encode(sample with
            { Request = MemoryRequest with { Context = WorkshopMemoryContext.Browser } })));
    }

    [Fact]
    public void StoppedMemoryRequestsRejectUnknownEnumsReservedBytesAndInvalidIdentity()
    {
        var valid = WorkshopMemoryWire.Encode(MemoryRequest);
        foreach (var offset in new[] { 0, 4, 8, 12 })
        {
            var malformed = (byte[])valid.Clone();
            BinaryPrimitives.WriteUInt32LittleEndian(malformed.AsSpan(offset), uint.MaxValue);
            Assert.Throws<ArgumentException>(() => WorkshopMemoryWire.DecodeRequest(malformed));
        }
        foreach (var offset in new[] { 32, 40 })
        {
            var malformed = (byte[])valid.Clone();
            BinaryPrimitives.WriteUInt64LittleEndian(malformed.AsSpan(offset), 0);
            Assert.Throws<ArgumentException>(() => WorkshopMemoryWire.DecodeRequest(malformed));
        }
        var missingSession = (byte[])valid.Clone();
        missingSession.AsSpan(16, 16).Clear();
        Assert.Throws<ArgumentException>(() => WorkshopMemoryWire.DecodeRequest(missingSession));
        Assert.Throws<ArgumentException>(() => WorkshopMemoryWire.DecodeRequest(valid.AsSpan(1)));
        Assert.Throws<ArgumentException>(() => WorkshopMemoryWire.DecodeRequest(new byte[65]));
    }

    [Fact]
    public void StoppedMemoryResultRejectsUnboundedOrFabricatedMetrics()
    {
        var sample = new WorkshopMemoryResult(MemoryRequest, WorkshopMemoryStatus.Accepted,
            1234, 1200, 19, 1_000_000_000, 5000, 5050, 6, 7);
        foreach (var invalid in new[] {
            sample with { UsedBefore = -1 }, sample with { UsedAfter = -1 },
            sample with { DurationTicks = -1 }, sample with { Frequency = 0 },
            sample with { AllocatedBefore = -1 }, sample with { AllocatedAfter = 4999 },
            sample with { CollectionsBefore = -1 }, sample with { CollectionsAfter = 5 },
            sample with { Status = (WorkshopMemoryStatus)uint.MaxValue },
            sample with { Status = WorkshopMemoryStatus.Busy } })
            Assert.Throws<ArgumentException>(() => WorkshopMemoryWire.Encode(invalid));
        var malformed = WorkshopMemoryWire.Encode(sample);
        malformed[120] = 1;
        Assert.Throws<ArgumentException>(() => WorkshopMemoryWire.DecodeResult(malformed));
        Assert.Throws<ArgumentException>(() => WorkshopMemoryWire.DecodeResult(new byte[127]));
        Assert.Throws<ArgumentException>(() => WorkshopMemoryWire.DecodeResult(new byte[129]));
        foreach (var status in new[] { WorkshopMemoryStatus.Busy, WorkshopMemoryStatus.Stale, WorkshopMemoryStatus.Unsupported })
        {
            var rejection = new WorkshopMemoryResult(MemoryRequest, status);
            Assert.Equal(rejection, WorkshopMemoryWire.DecodeResult(WorkshopMemoryWire.Encode(rejection)));
        }
    }
}
