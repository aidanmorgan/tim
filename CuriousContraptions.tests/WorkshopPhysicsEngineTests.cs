using System.Buffers.Binary;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;

namespace CuriousContraptions.Tests;

/// <summary>
/// Steps the shipped C# physics engine natively, with no numerical substitute. Run drives the production WorkshopSimulation and
/// WorkshopGpuDevice over the worker's own BrowserGpuTransport; Step and StepWorld drive WorkshopPhysicsEngine directly from an
/// admitted world, bypassing BrowserGpuTransport. Story 9.4b moved here the physical laws the deleted JS-kernel harness
/// (tools/workshop-rigid-body.test.mjs) asserted, where Chrome does not already observe them directly.
/// </summary>
public sealed class WorkshopPhysicsEngineTests
{

    [Fact]
    public async Task DeclaredRampLengthChangesRealDownhillRunoutWhileSideAndDepthMissesDoNotDeflect()
    {
        async Task<List<WorkshopRead>> Roll(float length, double x, double z) => await Run(Free(
            WorkshopInput.Generic(PartId.Basketball,new(1), x, 4, z, 0, 0, 0, 1),
            WorkshopInput.Generic(PartId.Ramp, new(2), 0, 2.5, 0, 0, 0, -Math.Sin(Math.PI / 18), Math.Cos(Math.PI / 18),
                new([new(new("length"), length), new(new("width"), 1.3f)]))), 3 * TicksPerSecond);
        var longRun = await Roll(4f, -.3, 0);
        var shortRun = await Roll(1f, -.3, 0);
        var sideMiss = await Roll(4f, 3, 0);
        var depthMiss = await Roll(4f, -.3, 2);
        static float Travel(List<WorkshopRead> reads) =>
            Horizontal(Find(reads[^1].Bodies, new(1))) - Horizontal(Find(reads[0].Bodies, new(1)));
        Assert.True(Travel(longRun) > 1f, $"long ramp downstream travel {Travel(longRun)}");
        Assert.True(Travel(shortRun) > .2f, $"short ramp downstream travel {Travel(shortRun)}");
        Assert.True(Travel(longRun) > Travel(shortRun) + .1f,
            $"a longer ramp gives a longer runout: long {Travel(longRun)}, short {Travel(shortRun)}");
        Assert.InRange(MathF.Abs(Travel(sideMiss)), 0f, .05f);
        Assert.InRange(MathF.Abs(Travel(depthMiss)), 0f, .05f);
        Assert.True(Height(Find(longRun[^1].Bodies, new(1))) < Height(Find(longRun[0].Bodies, new(1))));
    }

    // Story 9A.44 adds the support gate: the Receiver's guide volume spans local Y 0.5–1.5 with its support height 0.5 and default
    // margin 0, so a ball (radius 0.34) whose underside is below 0.5 (local Y 0.7, world 1.7) is not pushed, while one just above
    // the gate (local Y 1.0, world 2.0) is.
    [Theory]
    [InlineData(0, -1, .8, 2.1, true)]
    [InlineData(45, -1, .8, 2.1, true)]
    [InlineData(0, 1, .8, 2.1, false)]
    [InlineData(0, -1, 2, 2.1, false)]
    [InlineData(0, -1, .8, 1.7, false)]
    [InlineData(0, -1, .8, 2.0, true)]
    public void DeclaredReceiverGuideActsOnlyForDescendingInsideTargetsAndDoesNotMoveItsFrame(
        double degrees, float verticalSpeed, double localX, double height, bool guides)
    {
        var radians = degrees * Math.PI / 180;
        var x = localX * Math.Cos(radians);
        var z = -localX * Math.Sin(radians);
        var parameters = InstanceParameterCollection.Defaults(PartId.Receiver).ToDictionary();
        parameters[new("force_maximum_acceleration")] = 12f;
        var construction = Free(
            WorkshopInput.Generic(PartId.Basketball,new(1), x, height, z, 0, 0, 0, 1),
            WorkshopInput.Generic(PartId.Receiver, new(2), 0, 1, 0, 0, Math.Sin(radians / 2), 0, Math.Cos(radians / 2), new(parameters)));
        var world = AdmitMoving(construction, 1, 0f, verticalSpeed);
        var frameOffset = BodyRecord(world, 2);
        var frameBefore = world.AsSpan(frameOffset, PhysicsGpuAbi.BodyBytes).ToArray();
        StepWorld(world, 4, (_, state) =>
        {
            var body = Find(PhysicsGpuAbi.ReadDynamicBodies(state), new(1));
            var lateralSpeed = MathF.Sqrt(body.Body.Velocity.X * body.Body.Velocity.X + body.Body.Velocity.Z * body.Body.Velocity.Z);
            Assert.InRange(lateralSpeed, 0f, 12f * 4f / 120f + .01f);
        });
        var final = Find(PhysicsGpuAbi.ReadDynamicBodies(world), new(1));
        var toward = -final.Body.Velocity.X * (float)Math.Cos(radians) + final.Body.Velocity.Z * (float)Math.Sin(radians);
        if (guides) Assert.True(toward > .01f, $"declared guide changes lateral velocity: {toward}");
        else Assert.InRange(MathF.Abs(toward), 0f, .001f);
        Assert.Equal(frameBefore, world.AsSpan(frameOffset, PhysicsGpuAbi.BodyBytes).ToArray());
    }

    [Theory]
    [InlineData(1f, .1f, true)]
    [InlineData(1f, .5f, true)]
    [InlineData(0f, .1f, false)]
    public void DeclaredReceiverParticipationAndDwellDriveRealCapture(float participation, float dwell, bool captures)
    {
        var parameters = InstanceParameterCollection.Defaults(PartId.Receiver).ToDictionary();
        parameters[new("capture_participation")] = participation;
        parameters[new("capture_dwell")] = dwell;
        var construction = Free(
            WorkshopInput.Generic(PartId.Basketball,new(1), 0, 1.1, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(PartId.Receiver, new(2), 0, 1, 0, 0, 0, 0, 1, new(parameters)));
        var world = AdmitMoving(construction, 1, 0f);
        var first = 0;
        StepWorld(world, 240, (tick, state) =>
        {
            var sensor = PhysicsGpuAbi.ReadSensor(state, 0);
            if (sensor.OccurrenceCount > 0 && first == 0) first = tick;
            Assert.InRange(sensor.OccurrenceCount, 0u, 1u);
        });
        Assert.Equal(captures, first > 0);
        if (captures) Assert.True(first >= (int)(dwell * 120f), $"capture tick {first} honors dwell {dwell}");
    }
    [Theory]
    [InlineData(.25f, .5f, false)]
    [InlineData(1f, .5f, true)]
    [InlineData(1f, 8f, false)]
    public void DeclaredCaptureSpeedRejectsFastPassAtTheProductionSensor(float limit, float speed, bool enters)
    {
        var values = InstanceParameterCollection.Defaults(PartId.Receiver).ToDictionary();
        values[new("capture_speed_limit")] = limit;
        var world = AdmitMoving(Free(
            WorkshopInput.Generic(PartId.Basketball,new(1), 0, 1.1, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(PartId.Receiver, new(2), 0, 1, 0, 0, 0, 0, 1, new(values))), 1, speed);
        StepWorld(world, 1, (_, state) =>
        {
            var read = PhysicsGpuAbi.ReadSensor(state, 0);
            Assert.Equal(enters ? ResidencePhase.Dwelling : ResidencePhase.Outside, read.Phase);
            Assert.Equal(0u, read.OccurrenceCount);
        });
    }

    [Fact]
    public void LeavingBeforeDwellDiscardsPartialResidenceAndReentryStartsAFullNewDwell()
    {
        var values = InstanceParameterCollection.Defaults(PartId.Receiver).ToDictionary();
        values[new("capture_dwell")] = .1f;
        var world = AdmitMoving(Free(
            WorkshopInput.Generic(PartId.Basketball,new(1), 0, 1.1, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(PartId.Receiver, new(2), 0, 1, 0, 0, 0, 0, 1, new(values))), 1, 0f);
        var body = BodyRecord(world, 1);
        var initial = world.AsSpan(body, PhysicsGpuAbi.BodyBytes).ToArray();
        var firstCapture = 0;
        StepWorld(world, 36, (tick, state) =>
        {
            var read = PhysicsGpuAbi.ReadSensor(state, 0);
            if (tick == 5) Assert.Equal(ResidencePhase.Dwelling, read.Phase);
            if (tick == 6)
            {
                Assert.Equal(ResidencePhase.Outside, read.Phase);
                Assert.Equal(0u, read.StartOrdinal);
            }
            if (tick >= 7 && tick <= 16) Assert.Equal(0u, read.OccurrenceCount);
            if (read.OccurrenceCount != 0 && firstCapture == 0) firstCapture = tick;
            // External motion input at two boundaries: move out, then return. Sensor state is never edited.
            if (tick is 5 or 6)
            {
                initial.AsSpan(PhysicsGpuAbi.BodyCellOffset, 12).CopyTo(state.AsSpan(body + PhysicsGpuAbi.BodyCellOffset));
                initial.AsSpan(PhysicsGpuAbi.BodyLocalOffset, 12).CopyTo(state.AsSpan(body + PhysicsGpuAbi.BodyLocalOffset));
                state.AsSpan(body + PhysicsGpuAbi.BodyVelocityOffset, 12).Clear();
                if (tick == 5)
                    BinaryPrimitives.WriteInt32LittleEndian(state.AsSpan(body + PhysicsGpuAbi.BodyCellOffset), 48);
            }
        });
        Assert.InRange(firstCapture, 18, 21);
    }

    // CAT-004 margin and decoy: the declared capture_margin raises the mouth's upper bound. A held ball 0.05 m above the
    // fixed 0.45 m rim enters only with the wider margin; a second ball beside the Receiver stays Outside in its own sensor.
    [Theory]
    [InlineData(.02f, false)]
    [InlineData(.1f, true)]
    public void DeclaredCaptureMarginWidensTheMouthWhileABesideDecoyStaysOutside(float margin, bool enters)
    {
        var values = InstanceParameterCollection.Defaults(PartId.Receiver).ToDictionary();
        values[new("capture_margin")] = margin;
        var construction = Free(
            WorkshopInput.Generic(PartId.Basketball,new(1), 0, 1.5, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(PartId.Receiver, new(2), 0, 1, 0, 0, 0, 0, 1, new(values)),
            WorkshopInput.Generic(PartId.Basketball,new(3), 3, 1.5, 0, 0, 0, 0, 1));
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        var world = AdmitMoving(construction, 1, 0f);
        foreach (var id in new ulong[] { 1, 3 })
        {
            var body = BodyRecord(world, id);
            world.AsSpan(body + PhysicsGpuAbi.BodyGravityOffset, 6).Clear();
            world.AsSpan(body + PhysicsGpuAbi.BodyDragOffset, 2).Clear();
        }
        StepWorld(world, 1, (_, state) =>
        {
            for (var slot = 0; slot < scene.Sensors.Length; slot++)
            {
                var read = PhysicsGpuAbi.ReadSensor(state, slot);
                var target = Assert.Single(scene.Sensors.ToArray(), sensor => sensor.Id == read.Id).Target;
                var expected = target == new GpuBodyId(1) && enters ? ResidencePhase.Dwelling : ResidencePhase.Outside;
                Assert.Equal(expected, read.Phase);
                Assert.Equal(0u, read.OccurrenceCount);
            }
        });
    }

    // CAT-004/CAT-054 Run/Reset and Save/Load with full-precision f32 authored values: a ball rolls down a resized Ramp while a
    // second ball is captured by a Receiver with non-default capture settings; Reset restores the built state bit for bit and
    // the reloaded construction replays the identical run.
    [Fact]
    public async Task NonHalfRampAndReceiverParametersRunCaptureResetAndReloadExactly()
    {
        const float length = 3.123457f, width = 1.234567f, margin = .1234567f, speed = 2.345678f, dwell = .2345678f;
        foreach (var value in new[] { length, width, margin, speed, dwell }) Assert.True(TestPrecision.NeedsMoreThanElevenSignificantBits(value));
        var capture = InstanceParameterCollection.Defaults(PartId.Receiver).ToDictionary();
        capture[new("capture_margin")] = margin;
        capture[new("capture_speed_limit")] = speed;
        capture[new("capture_dwell")] = dwell;
        var construction = Free(
            WorkshopInput.Generic(PartId.Basketball,new(1), -.3, 4, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(PartId.Ramp, new(2), 0, 2.5, 0, 0, 0, -Math.Sin(Math.PI / 18), Math.Cos(Math.PI / 18),
                new([new(new("length"), length), new(new("width"), width)])),
            WorkshopInput.Generic(PartId.Basketball,new(3), 0, 1.1, -6, 0, 0, 0, 1),
            WorkshopInput.Generic(PartId.Receiver, new(4), 0, 1, -6, 0, 0, 0, 1, new(capture)));
        var saved = new WorkshopSavedConstruction(construction, new(5));
        var bytes = WorkshopSaveCodec.Encode(saved);
        var loaded = WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes);
        Assert.Equal(saved, loaded);
        Assert.Equal(bytes, WorkshopSaveCodec.Encode(loaded));
        Assert.Equal(length, ((WorkshopGenericInstance)loaded.Construction.Instances[1]).Parameters[new("length")]);
        Assert.Equal(dwell, ((WorkshopGenericInstance)loaded.Construction.Instances[3]).Parameters[new("capture_dwell")]);

        var (built, run, reset) = await NoFixedCapacityTests.RunAndReset(construction, 2 * TicksPerSecond);
        static bool Latched(WorkshopRead read) =>
            Enumerable.Range(0, read.Captures.Count).Any(i => read.Captures[i].Phase == CaptureLatchPhase.Latched);
        Assert.False(Latched(built));
        Assert.Contains(run, Latched);
        Assert.True(Horizontal(Find(run[^1].Bodies, new(1))) - Horizontal(Find(built.Bodies, new(1))) > 1f, "ball rolls down the Ramp");
        NoFixedCapacityTests.AssertSameState(built, reset, "Reset");
        var (_, replay, _) = await NoFixedCapacityTests.RunAndReset(loaded.Construction, 2 * TicksPerSecond);
        NoFixedCapacityTests.AssertSameState(run[^1], replay[^1], "the reloaded construction's run");
    }
    private const int TicksPerSecond = 120;
    private static readonly WorkshopCadenceSettings Settings = WorkshopCadenceSettings.Default();
    private static readonly float BenchY = (float)WorkshopConstruction.WorkbenchSurface.Value;

    internal static async Task<List<WorkshopRead>> Run(WorkshopConstruction construction, int ticks) =>
        (await RunWithLedgers(construction, ticks)).Reads;

    /// <summary>The committed reads and, beside each, the device-private contact-work ledger the read's stores come from (Story 9A.23
    /// D9: the occurrence rows never leave the device; tests read them where the producer and the accounting do).</summary>
    internal static async Task<(List<WorkshopRead> Reads, List<ContactWorkLedger> Ledgers)> RunWithLedgers(WorkshopConstruction construction, int ticks)
    {
        await using var device = new WorkshopGpuDevice(new BrowserGpuTransport(), new(101, 206));
        await using var simulation = new WorkshopSimulation(device, new StepClock(), Settings, new(1), new NoInstallation(),
            GpuDeviceOwnership.Borrowed);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Initialize()).Outcome);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Construct(construction)).Outcome);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Run()).Outcome);
        var reads = new List<WorkshopRead>(ticks + 1) { simulation.Committed };
        var ledgers = new List<ContactWorkLedger>(ticks + 1) { device.CommittedContactWork };
        for (var tick = 0; tick < ticks; tick++)
        {
            Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Advance()).Outcome);
            reads.Add(simulation.Committed);
            ledgers.Add(device.CommittedContactWork);
        }
        return (reads, ledgers);
    }

    private static WorkshopConstruction Free(params IWorkshopInstance[] instances) =>
        new(new(2), Settings, new(instances));

    private static float Height(PhysicsBodyRead body) => (body.Body.Cell.Y + body.Body.Local.Y) / 16f;
    private static float Horizontal(PhysicsBodyRead body) => (body.Body.Cell.X + body.Body.Local.X) / 16f;
    private static float Lateral(PhysicsBodyRead body) => (body.Body.Cell.Z + body.Body.Local.Z) / 16f;
    private static double Tilt(PhysicsBodyRead body) =>
        Math.Acos(Math.Clamp(1 - 2 * (body.Rotation.X * body.Rotation.X + body.Rotation.Z * body.Rotation.Z), -1, 1)) * 180 / Math.PI;
    private static float Speed(PhysicsBodyRead body) => MathF.Sqrt(body.Body.Velocity.X * body.Body.Velocity.X +
        body.Body.Velocity.Y * body.Body.Velocity.Y + body.Body.Velocity.Z * body.Body.Velocity.Z);
    private static PhysicsBodyRead Find(PhysicsBodyReadSet bodies, GpuBodyId id)
    {
        for (var i = 0; i < bodies.Count; i++) if (bodies[i].Body.Id == id) return bodies[i];
        throw new ArgumentException($"Body {id.Value} is not among the {bodies.Count} dynamic bodies read.", nameof(id));
    }

    // CAT-015 ledger (Story 9.4b, cat-015a heavy payload): the 4 kg ball can afford only part of the 8 m/s boost, so its first
    // paid contact spends the whole 32 J store and every later contact with the empty Bumper is unpaid and passive.
    [Theory]
    [InlineData(4.25)] [InlineData(4.5)] [InlineData(4.75)] [InlineData(5)] [InlineData(5.25)] [InlineData(5.5)]
    [InlineData(5.75)] [InlineData(6)] [InlineData(6.375)] [InlineData(6.75)] [InlineData(7.125)] [InlineData(7.5)]
    public async Task HeavyPayloadSpendsTheWholeStoreOnceAndEveryLaterBumperContactIsUnpaid(double dropHeight)
    {
        var (reads, ledgers) = await RunWithLedgers(Free(
            WorkshopInput.Generic(PartId.BowlingBall, new(1), 0, dropHeight, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(PartId.PinballBumper, new(2), 0, 3, 0, 0, 0, 0, 1)), 4 * TicksPerSecond);
        var seen = new HashSet<uint>();
        var paid = new List<float>();
        var exhausted = false;
        for (var tick = 0; tick < reads.Count; tick++)
        {
            var store = reads[tick].ContactWorks[0].RemainingEnergy.Value;
            if (exhausted) Assert.Equal(0f, store);
            exhausted |= store == 0f;
            for (var i = 0; i < ledgers[tick].OccurrenceCount; i++)
            {
                var occurrence = ledgers[tick].Occurrence(i);
                if (occurrence.Sequence == 0 || !seen.Add(occurrence.Sequence)) continue;
                paid.Add(occurrence.Debit.Value);
                Assert.Equal(occurrence.Debit.Value > 0, occurrence.Effect == ContactWorkEffect.Paid);
            }
        }
        Assert.True(paid.Count >= 2, "the ball returns to the empty Bumper");
        Assert.Equal(32f, paid[0]);
        Assert.All(paid.Skip(1), debit => Assert.Equal(0f, debit));
        Assert.True(exhausted);
    }

    // CAT-015 control: the 1 kg Basketball reaches the 8 m/s target within budget, so it pays less than the store and
    // the ledger closes exactly (remaining + debit = 32 J).
    [Fact]
    public async Task TargetLimitedPayoutLeavesTheExactRemainder()
    {
        var (reads, ledgers) = await RunWithLedgers(Free(
            WorkshopInput.Generic(PartId.Basketball,new(1), 0, 4.5, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(PartId.PinballBumper, new(2), 0, 3, 0, 0, 0, 0, 1)), 2 * TicksPerSecond);
        var first = ledgers.FindIndex(ledger => ledger.OccurrenceCount > 0 && ledger.Occurrence(0).Sequence == 1);
        var debit = ledgers[first].Occurrence(0).Debit.Value;
        Assert.InRange(debit, 1f, 31f);
        Assert.Equal(32f, reads[first].ContactWorks[0].RemainingEnergy.Value + debit);
    }

    private static byte[] BasketballOverBumperWithStore(float available)
    {
        var admitted = AdmitMoving(Free(
            WorkshopInput.Generic(PartId.Basketball,new(1), 0, 4.5, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(PartId.PinballBumper, new(2), 0, 3, 0, 0, 0, 0, 1)), 1, 0f);
        var store = PhysicsGpuAbi.Layout(admitted).ContactWorkOffset(0);
        Assert.Equal(32f, BinaryPrimitives.ReadSingleLittleEndian(admitted.AsSpan(store + PhysicsGpuAbi.ContactWorkPreloadOffset)));
        BinaryPrimitives.WriteSingleLittleEndian(admitted.AsSpan(store + PhysicsGpuAbi.ContactWorkAvailableOffset), available);
        return admitted;
    }

    // Verification gap V1 (Story 9.4b): the engine must judge a partly spent store against its declared 32 J preload, not
    // its available work. A half of the minimum payable work of the preload is dust: the contact qualifies and is recorded,
    // debits nothing and leaves the store unchanged. Control: twice the minimum pays, store-limited, the whole store.
    [Theory]
    [InlineData(.5f, false)]
    [InlineData(2f, true)]
    public void APartlySpentStoreIsJudgedAgainstItsPreloadSoDustNeverPays(float minimums, bool pays)
    {
        var seeded = minimums * ContactWorkImpulse.MinimumPayableWork(32f);
        var admitted = BasketballOverBumperWithStore(seeded);
        var layout = PhysicsGpuAbi.Layout(admitted);
        var store = layout.ContactWorkOffset(0);
        var occurrence = layout.WorkOccurrenceOffset(0);
        uint occurrences = 0;
        var debits = new List<(float Debit, byte Effect)>();
        var available = new List<float>();
        StepWorld(admitted, 2 * TicksPerSecond, (tick, world) =>
        {
            var count = BinaryPrimitives.ReadUInt32LittleEndian(world.AsSpan(store + PhysicsGpuAbi.ContactWorkOccurrenceCountOffset));
            if (count > occurrences)
                debits.Add((BinaryPrimitives.ReadSingleLittleEndian(world.AsSpan(occurrence + PhysicsGpuAbi.WorkOccurrenceDebitOffset)),
                    world[occurrence + PhysicsGpuAbi.WorkOccurrenceEffectOffset]));
            occurrences = count;
            available.Add(BinaryPrimitives.ReadSingleLittleEndian(world.AsSpan(store + PhysicsGpuAbi.ContactWorkAvailableOffset)));
        });
        Assert.NotEmpty(debits); // the ball reached the Bumper with a qualifying approach
        if (pays)
        {
            Assert.Equal((seeded, (byte)ContactWorkEffect.Paid), debits[0]);
            Assert.Equal(0f, available[^1]);
            return;
        }
        Assert.All(debits, paid => Assert.Equal((0f, (byte)ContactWorkEffect.Passive), paid));
        Assert.All(available, value => Assert.Equal(seeded, value));
    }

    // Blind hunter 14 (Story 9.4b): a store above its declared preload is impossible in a validated world. The engine
    // reports it explicitly instead of treating it as an empty store (controls: the 32 J store pays, above; a strength-0
    // Bumper's legal 0 J store, below).
    [Theory]
    [InlineData(33f)]
    [InlineData(float.NaN)]
    [InlineData(-1f)]
    public void AnImpossibleStoreIsReportedNotTreatedAsAnEmptyOne(float available)
    {
        var error = Assert.Throws<InvalidOperationException>(() => StepWorld(BasketballOverBumperWithStore(available), 2 * TicksPerSecond, (_, _) => { }));
        Assert.Contains("impossible store", error.Message);
    }

    // D20 control (advisor review): strength 0 is an authored Bumper whose 0 J preload is a legal, empty store. A ball
    // striking it bounces passively: every contact is recorded, debits nothing, and no tick faults.
    [Fact]
    public async Task AStrengthZeroBumpersEmptyStoreIsPassiveAndNeverFaults()
    {
        var (reads, ledgers) = await RunWithLedgers(Free(
            WorkshopInput.Generic(PartId.Basketball,new(1), 0, 4.5, 0, 0, 0, 0, 1),
            PlacedParts.Bumper(new(2), 0, 3, 0, strength: 0f)), 2 * TicksPerSecond);
        var occurrences = ledgers.SelectMany(ledger => Enumerable.Range(0, ledger.OccurrenceCount).Select(ledger.Occurrence))
            .Where(occurrence => occurrence.Sequence != 0).ToArray();
        Assert.NotEmpty(occurrences); // the ball struck the Bumper with a qualifying approach
        Assert.All(occurrences, occurrence => Assert.Equal((0f, ContactWorkEffect.Passive), (occurrence.Debit.Value, occurrence.Effect)));
        Assert.All(reads, read => Assert.Equal(0f, read.ContactWorks[0].RemainingEnergy.Value));
    }

    // Former harness fact (rigid-body): a dropped sphere bounces dissipatively and comes to rest at its radius on the bench, with
    // the harness's bounds: its first rebound apex stays below 1.5 m and it rests within 1 mm of its radius below 0.02 m/s.
    [Fact]
    public async Task DroppedBasketballBouncesLowerEachTimeAndRestsOnTheBench()
    {
        var reads = await Run(Free(WorkshopInput.Generic(PartId.Basketball,new(1), 0, 3, 0, 0, 0, 0, 1)), 6 * TicksPerSecond);
        var heights = reads.Select(read => Height(read.Bodies[0])).ToArray();
        var peaks = new List<float>();
        for (var i = 1; i + 1 < heights.Length; i++)
            if (heights[i] > heights[i - 1] && heights[i] >= heights[i + 1] && reads[i].Bodies[0].Body.Velocity.Y <= 0) peaks.Add(heights[i]);
        Assert.True(peaks.Count >= 2, "at least two rebound apexes");
        for (var i = 1; i < peaks.Count; i++) Assert.True(peaks[i] < peaks[i - 1], "each rebound apex is lower");
        Assert.True(peaks[0] < 1.5f, $"the first rebound apex {peaks[0]} m stays below the drop height");
        var radius = LawBalls.Radius(LawBall.Basketball);
        var atFive = reads[5 * TicksPerSecond].Bodies[0];
        Assert.InRange(Height(atFive), BenchY + radius - .01f, BenchY + radius + .01f);
        Assert.InRange(MathF.Abs(atFive.Body.Velocity.Y), 0f, .05f);
        var rest = reads[^1].Bodies[0];
        Assert.InRange(Height(rest), BenchY + radius - .001f, BenchY + radius + .001f);
        Assert.True(Speed(rest) < .02f, $"rest speed {Speed(rest)}");
    }

    // Former harness fact (bowling ball): the declared mass and bounce alone decide the rebound; the Bowling ball barely
    // rebounds and rests at its own 0.28 m radius.
    [Fact]
    public async Task DroppedBowlingBallBarelyReboundsAndRestsAtItsOwnRadius()
    {
        var reads = await Run(Free(WorkshopInput.Generic(LawBalls.Part(LawBall.Bowling), new(1), 0, 3, 0, 0, 0, 0, 1)), 4 * TicksPerSecond);
        var maximumRise = reads.Max(read => read.Bodies[0].Body.Velocity.Y);
        Assert.InRange(maximumRise, 0f, 1.5f);
        var radius = LawBalls.Radius(LawBall.Bowling);
        Assert.InRange(Height(reads[^1].Bodies[0]), BenchY + radius - .01f, BenchY + radius + .01f);
    }

    // Former harness fact (bowling ball): dropped from 3 m each ball's first rebound apex is e²·h of its own declared bounce, where
    // h is the centre's fall to its rest height; the Bowling ball (e 0.14) barely rebounds and the Basketball (e 0.55) rebounds high.
    // 15% separates them: swapping either ball's restitution for the other's misses its band by more than a factor of ten.
    [Theory]
    [InlineData(LawBall.Bowling)]
    [InlineData(LawBall.Basketball)]
    public async Task DroppedFromThreeMetresEachBallReboundsToESquaredHOfItsDeclaredBounce(LawBall kind)
    {
        var radius = LawBalls.Radius(kind); var restitution = LawBalls.Restitution(kind); var rest = BenchY + radius;
        var reads = await Run(Free(WorkshopInput.Generic(LawBalls.Part(kind), new(1), 0, 3, 0, 0, 0, 0, 1)), 6 * TicksPerSecond);
        var landed = false; var rising = false; var peak = float.MinValue; float? apex = null;
        foreach (var read in reads.Skip(1)) // the initial read is the admitted pose, not a rebound
        {
            var body = read.Bodies[0]; var vy = body.Body.Velocity.Y;
            if (vy < -1) landed = true;
            if (apex is not null) break;
            if (landed && vy > .05f) rising = true;
            if (!rising) continue;
            peak = MathF.Max(peak, Height(body));
            if (vy <= 0) apex = peak - rest;
        }
        var expected = restitution * restitution * (3 - rest);
        // The band excludes the other kind's declared bounce at this kind's fall height, so a swapped restitution fails here.
        var other = LawBalls.Restitution(kind == LawBall.Bowling ? LawBall.Basketball : LawBall.Bowling);
        Assert.True(MathF.Abs(other * other * (3 - rest) - expected) > .15f * expected, "the 15% band separates the two declared bounces");
        Assert.True(apex is { } first && MathF.Abs(first - expected) <= .15f * expected,
            $"{kind} first rebound apex {apex} m vs e²·h {expected} m");
        var final = reads[^1].Bodies[0];
        Assert.InRange(Height(final), rest - .001f, rest + .001f);
        Assert.True(Speed(final) < .02f, $"{kind} rest speed {Speed(final)}");
    }

    // Former harness fact (bowling ball): the two-lane e2e setup, both balls dropped at x = 0.3 from a 1.05 m centre beside a stock
    // Domino in lanes z = -1.5 / +1.5. The Bowling lane tile topples and lies flat, the Basketball lane tile only rocks and stands
    // again, the lanes never interact and both struck balls come to rest without jitter.
    [Fact]
    public async Task TheTwoLaneSetupSeparatesTheBallKindsAndBothStruckBallsComeToRest()
    {
        GpuBodyId tileA = new(1), tileB = new(2), ballA = new(3), ballB = new(4);
        var tileY = BenchY + LawTile.HalfExtents.Y + .01f;
        var reads = await Run(Free(
            PlacedParts.Domino(tileA, 0, tileY, -1.5, 0, 0, 0, 1), PlacedParts.Domino(tileB, 0, tileY, 1.5, 0, 0, 0, 1),
            WorkshopInput.Generic(LawBalls.Part(LawBall.Basketball), ballA, .3, 1.05, -1.5, 0, 0, 0, 1),
            WorkshopInput.Generic(LawBalls.Part(LawBall.Bowling), ballB, .3, 1.05, 1.5, 0, 0, 0, 1)), 7 * TicksPerSecond);
        var peakA = reads.Max(read => Tilt(Find(read.Bodies, tileA)));
        var peakB = reads.Max(read => Tilt(Find(read.Bodies, tileB)));
        Assert.True(peakA < 20, $"Basketball lane tilt {peakA} deg stays below 20");
        Assert.True(peakB > 60, $"Bowling lane tilt {peakB} deg passes 60");
        var final = reads[^1].Bodies;
        Assert.True(Tilt(Find(final, tileA)) < 2 && Math.Abs(Tilt(Find(final, tileB)) - 90) < 5, "tiles end upright and flat");
        Assert.All(reads, read =>
        {
            Assert.True(MathF.Abs(Lateral(Find(read.Bodies, tileA)) + 1.5f) < .01f && MathF.Abs(Lateral(Find(read.Bodies, ballA)) + 1.5f) < .01f);
            Assert.True(MathF.Abs(Lateral(Find(read.Bodies, tileB)) - 1.5f) < .01f && MathF.Abs(Lateral(Find(read.Bodies, ballB)) - 1.5f) < .01f);
        });
        foreach (var (ball, kind) in new[] { (ballA, LawBall.Basketball), (ballB, LawBall.Bowling) })
        {
            Assert.InRange(Height(Find(final, ball)), BenchY + LawBalls.Radius(kind) - .005f, BenchY + LawBalls.Radius(kind) + .005f);
            var settled = reads.Skip(6 * TicksPerSecond).Select(read => Find(read.Bodies, ball)).ToArray();
            Assert.True(settled.Max(Speed) < .02f, $"{kind} stays below 0.02 m/s from 6 s");
            Assert.True(settled.Max(b => MathF.Sqrt(MathF.Pow(Horizontal(b) - Horizontal(settled[0]), 2) + MathF.Pow(Height(b) - Height(settled[0]), 2) +
                MathF.Pow(Lateral(b) - Lateral(settled[0]), 2))) < .001f, $"{kind} moves less than 1 mm over the following second");
        }
    }

    /// <summary>The shipped engine's own admission record for a compiled construction, with the dynamic body
    /// <paramref name="id"/> given an initial linear velocity (the authoring model always admits bodies at rest).</summary>
    private static byte[] AdmitMoving(WorkshopConstruction construction, ulong id, float vx, float vy = 0f,
        SimulationCadence cadence = SimulationCadence.Hz120)
    {
        var bytes = PhysicsGpuAbi.Admission(WorkshopPhysicsCompiler.Compile(construction, new(1, 2)), new(1),
            new(cadence, PhysicalStepProfile.Canonical480Hz, new(1)));
        var body = BodyRecord(bytes, id);
        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(body + PhysicsGpuAbi.BodyVelocityOffset), vx);
        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(body + PhysicsGpuAbi.BodyVelocityOffset + 4), vy);
        return bytes;
    }

    /// <summary>The record offset of admitted body <paramref name="id"/>, searched only among the admitted body count the header
    /// declares (stale bytes beyond it are never matched).</summary>
    private static int BodyRecord(byte[] bytes, ulong id)
    {
        var count = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(PhysicsGpuAbi.HeaderBodyCountOffset));
        for (var slot = 0; slot < count; slot++)
        {
            var body = PhysicsWorldLayout.BodiesOffset + slot * PhysicsGpuAbi.BodyBytes;
            if (BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(body)) == id) return body;
        }
        throw new ArgumentException($"Body {id} is not among the {count} admitted bodies.", nameof(id));
    }

    [Fact]
    public void BodyRecordFindsOnlyAnAdmittedIdentity()
    {
        var bytes = AdmitMoving(Free(WorkshopInput.Generic(PartId.Basketball,new(1), 0, 30, 0, 0, 0, 0, 1)), 1, 0f);
        var count = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(PhysicsGpuAbi.HeaderBodyCountOffset));
        var found = BodyRecord(bytes, 1);
        Assert.Equal(1ul, BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(found)));
        var unknown = Assert.Throws<ArgumentException>(() => BodyRecord(bytes, 99));
        Assert.StartsWith($"Body 99 is not among the {count} admitted bodies.", unknown.Message);
        // A record-shaped identity beyond the admitted count (the next table's bytes) is never matched.
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(PhysicsWorldLayout.BodiesOffset + (int)count * PhysicsGpuAbi.BodyBytes), 99);
        Assert.Throws<ArgumentException>(() => BodyRecord(bytes, 99));
    }

    private static void Step(byte[] admitted, int ticks, Action<int, PhysicsBodyReadSet> observe) =>
        StepWorld(admitted, ticks, (tick, world) => observe(tick, PhysicsGpuAbi.ReadDynamicBodies(world)));

    private static int Rate(SimulationCadence cadence) => cadence switch
    {
        SimulationCadence.Hz60 => 60, SimulationCadence.Hz120 => 120, SimulationCadence.Hz240 => 240,
        _ => throw new ArgumentOutOfRangeException(nameof(cadence))
    };

    /// <summary>Steps the admitted world with the production tick inputs of an unswitched, battery-free scene: no source
    /// enables and every prismatic's switched supply open, as PhysicsTickInputs.Compose yields without routes.</summary>
    internal static void StepWorld(byte[] admitted, int ticks, Action<int, byte[]> observe, int prismatics = 0, int enabledSources = 0)
    {
        var engine = new WorkshopPhysicsEngine();
        engine.Reset();
        var source = admitted;
        var candidate = new byte[admitted.Length];
        var inputs = new PhysicsTickInputs(Enumerable.Repeat(ElectricalEnable.Enabled, enabledSources).ToArray(), new bool[prismatics]);
        for (var tick = 1; tick <= ticks; tick++)
        {
            engine.Advance(source, candidate, inputs);
            (source, candidate) = (candidate, source);
            observe(tick, source);
        }
    }

    // Former harness Fact18 (CAT-062, owner decision 13 Oct 2026, review-6-5 R5): the compiled Spring Forward lesson with its
    // locked Receiver shifted +0.3 m or -0.5 m still captures the launched ball under physical simulation. The shifted scene is
    // the compiler's own output for the construction with only the Receiver moved, so every Receiver declaration (residence
    // sensor, planar guide and their electrical parameters) is compiled, not copied. The lesson rejects a moved fixed Receiver
    // ("The passive lesson fixtures changed."), so the same instances compile as free play, which is first proved to compile the
    // identical physics scene (only the lesson's named capture-sensor identity differs).
    [Theory]
    [InlineData(.3, true)]
    [InlineData(-.5, true)]
    [InlineData(3.5, false)] // control: a Receiver far beyond the launch arc never captures
    public void ForgivingSpringForwardReceiverOffsetsCaptureInTheShippedEngine(double offset, bool captures)
    {
        var lesson = PuzzleCatalogue.Create(new PuzzleId("spring_forward"), new(1), Settings, new(1), new(0f));
        var construction = lesson.WithInstance(WorkshopInput.Generic(PartId.Springboard, new(3), -3, .8, 0,
            0, 0, -Math.Sin(Math.PI / 18), Math.Cos(Math.PI / 18)));
        var receiver = ((WorkshopGenericInstance)construction.Instances.Single(instance => instance.PartId == PartId.Receiver));
        var position = WorkshopInput.Generic(PartId.Receiver, receiver.Id, -.7 + offset, .55, 0, 0, 0, 0, 1);
        var original = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        var freePlay = construction with { Puzzle = default };
        var unshifted = WorkshopPhysicsCompiler.Compile(freePlay, new(1, 2));
        Assert.Equal(original.Bodies.ToArray(), unshifted.Bodies.ToArray());
        Assert.Equal(original.Colliders.ToArray(), unshifted.Colliders.ToArray());
        Assert.Equal(original.Materials.ToArray(), unshifted.Materials.ToArray());
        Assert.Equal(original.Sensors.Length, unshifted.Sensors.Length);
        for (var i = 0; i < original.Sensors.Length; i++) Assert.Equal(original.Sensors[i] with { Id = unshifted.Sensors[i].Id }, unshifted.Sensors[i]);
        Assert.Equal(original.Guides.Length, unshifted.Guides.Length);
        for (var i = 0; i < original.Guides.Length; i++) Assert.Equal(original.Guides[i] with { Id = unshifted.Guides[i].Id }, unshifted.Guides[i]);
        Assert.Equal(original.Prismatics.ToArray(), unshifted.Prismatics.ToArray());
        Assert.Equal(original.Triggers.ToArray(), unshifted.Triggers.ToArray());
        Assert.Equal(original.ContactWorks.ToArray(), unshifted.ContactWorks.ToArray());
        Assert.Equal(original.OrientationSensors.ToArray(), unshifted.OrientationSensors.ToArray());
        Assert.Equal(original.Electrical.Sources.ToArray(), unshifted.Electrical.Sources.ToArray());
        Assert.Equal(original.Electrical.Bindings.ToArray(), unshifted.Electrical.Bindings.ToArray());
        Assert.Equal(original.SupplyGraph.Nodes.ToArray(), unshifted.SupplyGraph.Nodes.ToArray());
        EditRefusal.Is(WorkshopEditRejection.FixedPart, () => WorkshopPhysicsCompiler.Compile(construction.WithInstance(receiver with { Cell = position.Cell, Local = position.Local }), new(1, 2)));
        var scene = WorkshopPhysicsCompiler.Compile(freePlay.WithInstance(receiver with { Cell = position.Cell, Local = position.Local }), new(1, 2));
        var moved = scene.Bodies.ToArray().Single(b => b.Id == receiver.Id);
        Assert.Equal((position.Cell, position.Local), (moved.Cell, moved.Local));
        Assert.True(scene.Sensors.Length > 0 && scene.Sensors.Length == original.Sensors.Length, "the Receiver's residence sensor compiles");
        for (var i = 0; i < scene.Sensors.Length; i++)
            Assert.Equal((receiver.Id, original.Sensors[i].Pose), (scene.Sensors[i].Frame, scene.Sensors[i].Pose));
        Assert.True(scene.Guides.Length > 0 && scene.Guides.Length == original.Guides.Length, "the Receiver's planar guide compiles");
        for (var i = 0; i < scene.Guides.Length; i++)
            Assert.Equal((receiver.Id, original.Guides[i].Pose), (scene.Guides[i].Frame, scene.Guides[i].Pose));
        var admitted = PhysicsGpuAbi.Admission(scene, new(1), new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        var capturedAt = 0;
        StepWorld(admitted, 10 * TicksPerSecond, (tick, world) =>
        {
            if (capturedAt == 0 && PhysicsGpuAbi.ReadSensor(world, 0).OccurrenceCount > 0) capturedAt = tick;
        }, scene.Prismatics.Length);
        Assert.True(captures == capturedAt > 0, $"offset {offset} m capture expected {captures}, first capture tick {capturedAt}");
    }

    // Former harness facts (drag): in free flight the declared 0.04 1/s drag slows the horizontal speed as exp(-drag t) on
    // every committed tick, gravity leaves the horizontal component alone, and the free-flight spin is never damped.
    [Theory]
    [InlineData(SimulationCadence.Hz60)]
    [InlineData(SimulationCadence.Hz120)]
    [InlineData(SimulationCadence.Hz240)]
    public void FreeFlightHorizontalSpeedDecaysAsTheDeclaredDragOnEveryTick(SimulationCadence cadence)
    {
        var rate = Rate(cadence);
        var admitted = AdmitMoving(Free(WorkshopInput.Generic(PartId.Basketball,new(1), 0, 30, 0, 0, 0, 0, 1)), 1, 2f, cadence: cadence);
        BinaryPrimitives.WriteSingleLittleEndian(admitted.AsSpan(BodyRecord(admitted, 1) + PhysicsGpuAbi.BodyAngularVelocityOffset + 4), 5f);
        var drag = LawBalls.Drag(LawBall.Basketball);
        var previous = 2f;
        Step(admitted, rate, (tick, bodies) =>
        {
            var vx = bodies[0].Body.Velocity.X;
            var expected = 2f * MathF.Exp(-drag * tick / rate);
            Assert.True(vx < previous, $"tick {tick} slows the horizontal speed");
            Assert.InRange(vx, expected - 1e-4f, expected + 1e-4f);
            Assert.Equal(5f, bodies[0].AngularVelocity.Y);
            previous = vx;
        });
    }

    // Former harness fact (rolling resistance): a ball rolling on the bench at 1 m/s decelerates through its declared rolling
    // resistance and drag, comes to rest at its radius and never reverses, at every supported cadence.
    [Theory]
    [InlineData(LawBall.Basketball, SimulationCadence.Hz60)] [InlineData(LawBall.Basketball, SimulationCadence.Hz120)]
    [InlineData(LawBall.Basketball, SimulationCadence.Hz240)]
    [InlineData(LawBall.Bowling, SimulationCadence.Hz60)] [InlineData(LawBall.Bowling, SimulationCadence.Hz120)]
    [InlineData(LawBall.Bowling, SimulationCadence.Hz240)]
    public void RollingBallComesToRestAtItsRadiusAndNeverReverses(LawBall ball, SimulationCadence cadence)
    {
        var kind = LawBalls.Part(ball);
        var radius = LawBalls.Radius(ball);
        var rate = Rate(cadence);
        var admitted = AdmitMoving(Free(WorkshopInput.Generic(kind, new(1), 0, -.46 + radius, 0, 0, 0, 0, 1)), 1, 1f, cadence: cadence);
        var body = BodyRecord(admitted, 1);
        BinaryPrimitives.WriteSingleLittleEndian(admitted.AsSpan(body + PhysicsGpuAbi.BodyAngularVelocityOffset + 8), -1f / radius);
        PhysicsBodyRead last = default;
        var maximumLateSpeed = 0f;
        Step(admitted, 8 * rate, (tick, bodies) =>
        {
            last = bodies[0];
            Assert.True(last.Body.Velocity.X >= -1e-3f, $"tick {tick} never reverses (vx {last.Body.Velocity.X})");
            if (tick >= 6 * rate) maximumLateSpeed = MathF.Max(maximumLateSpeed, MathF.Abs(last.Body.Velocity.X));
        });
        Assert.True(maximumLateSpeed < .02f, $"at rest from 6 s (max {maximumLateSpeed} m/s)");
        Assert.InRange(Height(last), -.46f + radius - .005f, -.46f + radius + .005f);
        Assert.True(Horizontal(last) > .5f, "the ball rolled forward before resting");
    }

    // Former harness fact (electrical): the finite source pays on exactly 120 electrical phases per physical second at
    // every simulation cadence (every fourth 480 Hz substep), so a 10 W battery feeding an empty Bumper store debits 5 J
    // per half second regardless of the tick rate.
    [Theory]
    [InlineData(SimulationCadence.Hz60)]
    [InlineData(SimulationCadence.Hz120)]
    [InlineData(SimulationCadence.Hz240)]
    public void BatterySupplyRunsOneHundredTwentyPhasesPerSecondAtEveryCadence(SimulationCadence cadence)
    {
        var battery = PlacedParts.Battery(new(1), 2, 1, 0, powerLimit: 10f);
        var bumper = WorkshopInput.Generic(PartId.PinballBumper, new(2), 0, 1, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new(2), Settings, new(battery, bumper), Connections: new(
            new WorkshopConnection(battery.Id, WorkshopSocket.Supply, bumper.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical)));
        var admitted = PhysicsGpuAbi.Admission(WorkshopPhysicsCompiler.Compile(construction, new(1, 2)), new(1),
            new(cadence, PhysicalStepProfile.Canonical480Hz, new(1)));
        BinaryPrimitives.WriteSingleLittleEndian(admitted.AsSpan(PhysicsGpuAbi.Layout(admitted).ContactWorkOffset(0) + PhysicsGpuAbi.ContactWorkAvailableOffset), 0f);
        var rate = Rate(cadence);
        var debits = new List<double>();
        StepWorld(admitted, rate, (tick, world) =>
        {
            if (tick % (rate / 2) == 0) debits.Add(3600.0 - PhysicsGpuAbi.ReadElectrical(world)[0].Remaining.Value);
        }, enabledSources: 1);
        // One phase debits 10 W x 1/120 s, quantised to the f32 grid of the 3600 J balance (the ledger rounds toward the
        // source keeping at least what it credits); the count of phases is what this proves.
        var phaseDebit = 3600f - (3600f - 10f * ElectricalSupply.PhaseSeconds);
        Assert.Equal(2, debits.Count);
        Assert.InRange(debits[0], 60 * phaseDebit - 1e-4, 60 * phaseDebit + 1e-4);
        Assert.InRange(debits[1], 120 * phaseDebit - 1e-4, 120 * phaseDebit + 1e-4);
    }

    // Former harness fact (game-grade envelope): a speed beyond the envelope clamps inside the 64 m/s host bound and the
    // tick continues; numerical residuals never fault the tick.
    [Fact]
    public void SpeedBeyondTheEnvelopeClampsAndTheTickContinues()
    {
        var admitted = AdmitMoving(Free(WorkshopInput.Generic(PartId.Basketball,new(1), 0, 30, 0, 0, 0, 0, 1)), 1, 100f, 40f);
        var ticks = 0;
        Step(admitted, 12, (tick, bodies) =>
        {
            var v = bodies[0].Body.Velocity;
            Assert.True(MathF.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z) <= 64f, $"tick {tick} speed within the 64 m/s bound");
            ticks = tick;
        });
        Assert.Equal(12, ticks);
    }

    // Former harness fact (rigid-body): an upright box set on the bench stands within the rest tolerances; a struck one
    // topples. The standing control is native; the strike is cat-023a test 2 in Chrome.
    [Fact]
    public async Task UprightDominoAloneStandsStill()
    {
        var reads = await Run(Free(PlacedParts.Domino(new(1), 0, .1, 0, 0, 0, 0, 1)), 3 * TicksPerSecond);
        var end = reads[^1].Bodies[0];
        var upY = 1 - 2 * (end.Rotation.X * end.Rotation.X + end.Rotation.Z * end.Rotation.Z);
        Assert.InRange(upY, .9999f, 1.0001f);
        Assert.InRange(MathF.Abs(Horizontal(end)), 0f, .002f);
    }

    // Former harness fact (CAT-014 mass isolation, cited by cat-014.test.ts): in a 0.7 m/s rolling strike on a stock Domino the
    // declared mass alone decides the outcome. 4 kg topples the tile at either radius; 1 kg only rocks it at either radius.
    // The ball's admitted mass and principal inertia are scaled together in the shipped engine's own admission record.
    [Theory]
    [InlineData(LawBall.Basketball, 1f, false)]
    [InlineData(LawBall.Basketball, 4f, true)]
    [InlineData(LawBall.Bowling, 4f, true)]
    [InlineData(LawBall.Bowling, 1f, false)]
    public void RollingStrikeTopplesTheDominoOnlyWithTheHeavierMass(LawBall ballKind, float mass, bool topples)
    {
        var kind = LawBalls.Part(ballKind);
        var radius = LawBalls.Radius(ballKind);
        var declaredMass = LawBalls.Mass(ballKind);
        var construction = Free(
            PlacedParts.Domino(new(1), 0, .09, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(kind, new(2), -(.125 + radius + .05), -.46 + radius, 0, 0, 0, 0, 1));
        var bytes = AdmitMoving(construction, 2, .7f);
        var ball = BodyRecord(bytes, 2);
        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(ball + PhysicsGpuAbi.BodyMassOffset), (float)mass);
        // The mass ratio is a power of two, so scaling each f32 moment by it is exact.
        var scale = (float)(mass / declaredMass);
        for (var axis = 0; axis < 3; axis++)
        {
            var moment = bytes.AsSpan(ball + PhysicsGpuAbi.BodyPrincipalInertiaOffset + axis * PhysicsGpuAbi.PrincipalInertiaBytes, 4);
            BinaryPrimitives.WriteSingleLittleEndian(moment, BinaryPrimitives.ReadSingleLittleEndian(moment) * scale);
        }
        var maximumTilt = 0.0;
        Step(bytes, 4 * TicksPerSecond, (_, bodies) =>
        {
            var tile = bodies[0];
            Assert.Equal(new GpuBodyId(1), tile.Body.Id);
            maximumTilt = Math.Max(maximumTilt, Tilt(tile));
        });
        if (topples) Assert.True(maximumTilt > 60, $"{mass} kg {ballKind} topples the tile (max {maximumTilt:F1} deg)");
        else Assert.True(maximumTilt is > 1 and < 20, $"{mass} kg {ballKind} strikes but only rocks the tile (max {maximumTilt:F1} deg)");
    }

    // Story 9R.3: each directly wired Powered Gate follows its own battery's committed player enable during a run. Every bench runs
    // the production WorkshopSimulation; the only player input is the typed in-run ConfigureElectrical command between Advances.
    private const float GateHeight = 3f;
    private static WorkshopGenericInstance Gate(ulong id, double z) =>
        WorkshopInput.Generic(PartId.PoweredGate, new(id), 0, GateHeight, z, 0, 0, 0, 1);
    private static WorkshopGenericInstance Battery(ulong id, double z, ElectricalEnable enabled) =>
        PlacedParts.Battery(new(id), 3, GateHeight, z, enabled: enabled);
    private static WorkshopConnection Wire(GpuBodyId battery, GpuBodyId gate) =>
        new(battery, WorkshopSocket.Supply, gate, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);
    /// <summary>The blade's travel along its prismatic above the gate frame (closed ≈ 0, fully open ≈ 1.55 m).</summary>
    private static float Travel(WorkshopRead read, WorkshopGenericInstance gate) =>
        Height(Find(read.Bodies, WorkshopPhysicsCompiler.SliderBody(gate.Id, 0))) - GateHeight;
    private const float ClosedTravel = .2f, OpenTravel = 1.3f;

    /// <summary>Runs the construction through the production simulation in phases: each phase first applies its in-run supply
    /// controls through ConfigureElectrical, then advances its ticks. Returns every phase's committed reads.</summary>
    internal static async Task<List<List<WorkshopRead>>> RunPhases(WorkshopConstruction construction,
        params (WorkshopElectricalControl[] Controls, int Ticks)[] phases)
    {
        await using var device = new WorkshopGpuDevice(new BrowserGpuTransport(), new(101, 206));
        await using var simulation = new WorkshopSimulation(device, new StepClock(), Settings, new(1), new NoInstallation(),
            GpuDeviceOwnership.Borrowed);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Initialize()).Outcome);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Construct(construction)).Outcome);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Run()).Outcome);
        var result = new List<List<WorkshopRead>>();
        foreach (var (controls, ticks) in phases)
        {
            foreach (var control in controls) Assert.Equal(WorkshopCommandOutcome.Applied, simulation.ConfigureElectrical(control).Outcome);
            var reads = new List<WorkshopRead>(ticks);
            for (var tick = 0; tick < ticks; tick++)
            {
                Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Advance()).Outcome);
                reads.Add(simulation.Committed);
            }
            result.Add(reads);
        }
        return result;
    }

    [Fact]
    public async Task AnAuthoredDisabledBatteryOpensItsGateWhenEnabledInRun()
    {
        var gate = Gate(1, 0); var battery = Battery(2, 0, ElectricalEnable.Disabled);
        var phases = await RunPhases(Free(gate, battery) with { Connections = new(Wire(battery.Id, gate.Id)) },
            ([], TicksPerSecond), ([new(battery.Id, ElectricalEnable.Enabled)], 2 * TicksPerSecond));
        Assert.All(phases[0], read => Assert.True(Travel(read, gate) < ClosedTravel, $"closed before Enable, travel {Travel(read, gate)}"));
        Assert.True(Travel(phases[1][^1], gate) > OpenTravel, $"open after in-run Enable, travel {Travel(phases[1][^1], gate)}");
    }

    [Fact]
    public async Task AnEnabledBatteryClosesItsGateWhenDisabledInRun()
    {
        var gate = Gate(1, 0); var battery = Battery(2, 0, ElectricalEnable.Enabled);
        var phases = await RunPhases(Free(gate, battery) with { Connections = new(Wire(battery.Id, gate.Id)) },
            ([], 2 * TicksPerSecond), ([new(battery.Id, ElectricalEnable.Disabled)], 2 * TicksPerSecond));
        Assert.True(Travel(phases[0][^1], gate) > OpenTravel, $"open before Disable, travel {Travel(phases[0][^1], gate)}");
        Assert.True(Travel(phases[1][^1], gate) < ClosedTravel, $"closed after in-run Disable, travel {Travel(phases[1][^1], gate)}");
    }

    // Negative control for the deleted global battery flag: disabling one circuit leaves the other gate open throughout.
    [Fact]
    public async Task TwoCircuitsAreIndependent()
    {
        var gate1 = Gate(1, -3); var gate2 = Gate(2, 3);
        var battery1 = Battery(3, -3, ElectricalEnable.Enabled); var battery2 = Battery(4, 3, ElectricalEnable.Enabled);
        var phases = await RunPhases(Free(gate1, gate2, battery1, battery2) with
            { Connections = new(Wire(battery1.Id, gate1.Id), Wire(battery2.Id, gate2.Id)) },
            ([], 2 * TicksPerSecond), ([new(battery1.Id, ElectricalEnable.Disabled)], 2 * TicksPerSecond));
        Assert.True(Travel(phases[0][^1], gate1) > OpenTravel && Travel(phases[0][^1], gate2) > OpenTravel, "both gates open before B1 is disabled");
        Assert.True(Travel(phases[1][^1], gate1) < ClosedTravel, $"G1 closes, travel {Travel(phases[1][^1], gate1)}");
        Assert.All(phases[1], read => Assert.True(Travel(read, gate2) > OpenTravel, $"G2 stays open, travel {Travel(read, gate2)}"));
    }

    // Control: a gate with no supply stays closed even while an unwired battery on the bench is enabled in run.
    [Fact]
    public async Task AnUnpoweredGateStaysClosed()
    {
        var gate = Gate(1, 0); var battery = Battery(2, 0, ElectricalEnable.Disabled);
        var phases = await RunPhases(Free(gate, battery),
            ([], TicksPerSecond), ([new(battery.Id, ElectricalEnable.Enabled)], 2 * TicksPerSecond));
        Assert.All(phases.SelectMany(reads => reads), read => Assert.True(Travel(read, gate) < ClosedTravel, $"travel {Travel(read, gate)}"));
    }

    internal sealed class StepClock : IWorkshopCaptureClock
    {
        private long _time = 100_000_000;
        public ClockGeneration Generation => new(1);
        public WorkshopClockStamp Capture() => new(WorkshopClockDomain.SimulationMonotonic, Generation, new(_time += 8_333_333), new(100_000));
    }

    internal sealed class NoInstallation : IWorkshopInstallation
    {
        public void Abort(CommandSequence owner) { }
        public void Retire() { }
        public ValueTask Prepare(CommandSequence owner, WorkshopRead read, WorkshopConstruction construction, WorkshopGpuProfile profile, WorkshopSimulationPhase phase) => ValueTask.CompletedTask;
    }
}
