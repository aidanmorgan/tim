using CuriousContraptions.Gpu;
using System.Buffers.Binary;

namespace CuriousContraptions.Tests;

// Exercises the production C# device owner with a held external transport, never a numerical substitute.
public sealed class WorkshopGpuDeviceTests
{

    [Theory]
    [InlineData(0f)]
    [InlineData(.2f)]
    public async Task RealResetLifecycleRestoresSpringAndReplaysItsMotion(float compression)
    {
        await using var device = new WorkshopGpuDevice(new CuriousContraptions.Simulation.BrowserGpuTransport(
            new CuriousContraptions.Abstractions.EventRingPublication()), Document);
        await using var simulation = new WorkshopSimulation(device, new ResetClock(), CompiledSceneFixture.Compile(new(new(1), Settings, WorkshopInstances.Empty)), new(1),
            new ResetInstallation(), GpuDeviceOwnership.Borrowed);
        await simulation.Initialize();
        var construction = new WorkshopConstruction(new(2), Settings, new(
            PlacedParts.Springboard(new(1), 0, 3, 0, 0, 0, 0, 1, compression)));
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Construct(CompiledSceneFixture.Compile(construction))).Outcome);
        var original = simulation.Committed;
        var plate = construction.Instances.Identities[new(1)].Sliders[0].Body;
        static float Height(PhysicsBodyRead body) => (body.Body.Cell.Y + body.Body.Local.Y) / 16f;
        for (var run = 0; run < 2; run++)
        {
            Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Run()).Outcome);
            var heights = new List<float>();
            for (var tick = 0; tick < 600; tick++)
            {
                Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Advance()).Outcome);
                Assert.Equal((ulong)tick + 1, simulation.Committed.Tick.Value);
                Assert.True(simulation.Committed.Bodies.TryGet(plate, out var body));
                heights.Add(Height(body));
                Assert.InRange(Height(body), 2.9f, 3.4f); // the supported plate stays within its 0.5 m working clearance
            }
            Assert.True(original.Bodies.TryGet(plate, out var authoredPlate));
            if (compression > 0) Assert.True(heights.Max() > Height(authoredPlate) + .05f);
            else Assert.True(heights.Min() < Height(authoredPlate) - .001f); // gravity loads the uncharged control
            var settled = heights.TakeLast(120).ToArray();
            Assert.InRange(settled.Max() - settled.Min(), 0f, .01f); // final second: stable to one centimetre
            Assert.InRange(settled[^1], 3.0f, 3.15f);
            Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Reset()).Outcome);
            Assert.Equal(0UL, simulation.Committed.Tick.Value);
            NoFixedCapacityTests.AssertSameState(original, simulation.Committed, "spring Reset");
        }
    }

    private sealed class ResetClock : IWorkshopCaptureClock
    {
        private long _time = 100_000_000;
        public ClockGeneration Generation => new(1);
        public WorkshopClockStamp Capture() => new(WorkshopClockDomain.SimulationMonotonic, Generation, new(_time += 10_000_000), new(100_000));
    }
    private sealed class ResetInstallation : IWorkshopInstallation
    {
        public void Abort(CommandSequence owner) { }
        public void Retire() { }
        public ValueTask Prepare(CommandSequence owner, WorkshopRead read, WorkshopCompiledScene construction, WorkshopGpuProfile profile, WorkshopSimulationPhase phase) => ValueTask.CompletedTask;
    }
    private static readonly PhysicsDocumentId Document = new(101, 206);
    private static readonly WorkshopCadenceSettings Settings = WorkshopCadenceSettings.Default();
    private static readonly WorkshopGpuProfile Profile = new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1));
    [Fact]
    public async Task ResourcePreparationAndCandidateShareOneInitialization()
    {
        var transport = new HeldTransport(true);
        var device = new WorkshopGpuDevice(transport, Document);
        var preparation = device.Prepare();
        var candidate = device.Admit(CompiledSceneFixture.Compile(new(new(1), Settings, WorkshopInstances.Empty)), new(1), Profile, new(1)).AsTask();
        Assert.False(preparation.IsCompleted);
        Assert.False(candidate.IsCompleted);
        Assert.Equal(1, transport.Initializations);
        Assert.Equal(0, transport.Stages);
        Assert.Equal(0, transport.Reads);
        Assert.Equal(0, transport.Commits);
        transport.Held.SetResult();
        await preparation;
        var admitted = await candidate;
        Assert.Equal(1, transport.Initializations);
        Assert.Equal(1, transport.Stages);
        device.Commit(admitted.Sequence);
        device.Discard(admitted.Sequence);
        await device.Prepare();
        Assert.Equal(1, transport.Initializations);
        await device.DisposeAsync();
    }

    [Fact]
    public async Task FailedPreparationCannotBeRetriedByQueuedCandidateOrPrepare()
    {
        var transport = new HeldTransport(true);
        var device = new WorkshopGpuDevice(transport, Document);
        var preparation = device.Prepare();
        var candidate = device.Admit(CompiledSceneFixture.Compile(new(new(1), Settings, WorkshopInstances.Empty)), new(1), Profile, new(1)).AsTask();
        var failure = new IOException("Preparation failed");
        transport.Held.SetException(failure);
        Assert.Same(failure, await Assert.ThrowsAsync<IOException>(() => preparation));
        Assert.Same(failure, await Assert.ThrowsAsync<IOException>(() => candidate));
        Assert.Same(failure, await Assert.ThrowsAsync<IOException>(() => device.Prepare()));
        Assert.Equal(1, transport.Initializations);
        Assert.Equal(0, transport.Stages);
        Assert.Equal(0, transport.Reads);
        Assert.Equal(0, transport.Commits);
        await device.DisposeAsync();
    }

    [Fact]
    public async Task DisposalDuringPreparationCannotReleaseALateCandidate()
    {
        var transport = new HeldTransport(true);
        var device = new WorkshopGpuDevice(transport, Document);
        var preparation = device.Prepare();
        var candidate = device.Admit(CompiledSceneFixture.Compile(new(new(1), Settings, WorkshopInstances.Empty)), new(1), Profile, new(1)).AsTask();
        await device.DisposeAsync();
        transport.Held.SetResult();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => preparation);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => candidate);
        Assert.Equal(1, transport.Initializations);
        Assert.Equal(1, transport.Disposals);
        Assert.Equal(0, transport.Stages);
        Assert.Equal(0, transport.Reads);
        Assert.Equal(0, transport.Commits);
    }

    [Fact]
    // The worker owns no device since the pure C# engine (Story 9.2b); Story 9.4b deleted the device-loss path, so
    // preparation initializes once and admission reuses it.
    public async Task PreparedResourcesAloneDoNotPublishAndAdmissionReusesThem()
    {
        var transport = new HeldTransport(false);
        transport.Held.SetResult();
        var device = new WorkshopGpuDevice(transport, Document);
        await device.Prepare();
        Assert.Equal(1, transport.Initializations);
        Assert.Equal(0, transport.Stages);
        Assert.Equal(0, transport.Reads);
        Assert.Equal(0, transport.Commits);
        var admitted = await device.Admit(CompiledSceneFixture.Compile(new(new(1), Settings, WorkshopInstances.Empty)), new(2), Profile, new(1));
        Assert.Equal(1, transport.Initializations);
        device.Commit(admitted.Sequence);
        device.Discard(admitted.Sequence);
        await device.DisposeAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DisposeDuringQualificationOrReadbackCannotStageOrCommitLateCandidate(bool duringQualification)
    {
        var transport = new HeldTransport(duringQualification);
        var device = new WorkshopGpuDevice(transport, Document);
        var pending = device.Admit(CompiledSceneFixture.Compile(new(new(1), Settings, WorkshopInstances.Empty)), new(1), Profile, new(1)).AsTask();
        Assert.False(pending.IsCompleted);
        await device.DisposeAsync();
        await device.DisposeAsync();
        transport.Held.SetResult();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => pending);
        Assert.Equal(1, transport.Disposals);
        Assert.Equal(duringQualification ? 0 : 1, transport.Stages);
        Assert.Equal(0, transport.Reads);
        Assert.Equal(0, transport.Commits);
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            device.Admit(CompiledSceneFixture.Compile(new(new(1), Settings, WorkshopInstances.Empty)), new(1), Profile, new(2)).AsTask());
        Assert.Equal(0, transport.Commits);
    }

    [Fact]
    public async Task ActualAdapterRejectsMalformedDescriptorBeforeCommit()
    {
        var transport = new HeldTransport(false);
        var device = new WorkshopGpuDevice(transport, Document);
        var pending = device.Admit(CompiledSceneFixture.Compile(new(new(1), Settings, WorkshopInstances.Empty)), new(1), Profile, new(1)).AsTask();
        transport.Record[136] = 1;
        transport.Held.SetResult();
        await Assert.ThrowsAsync<ArgumentException>(() => pending);
        Assert.Equal(0, transport.Commits);
        Assert.Equal(1, transport.Reads);
        await device.DisposeAsync();
    }

    [Fact]
    public async Task TypedNumericalFailureDiscardsCandidateAndReleasesOwnedGate()
    {
        var transport = new HeldTransport(false);
        var device = new WorkshopGpuDevice(transport, Document);
        transport.Held.SetResult();
        var admitted = await device.Admit(CompiledSceneFixture.Compile(new(new(1), Settings, WorkshopInstances.Empty)), new(1), Profile, new(1));
        device.Commit(admitted.Sequence);
        device.Discard(admitted.Sequence);
        transport.Record[4] = (byte)PhysicsCandidateStatus.Invalid;
        transport.Record[8] = (byte)PhysicsFailure.RootBudget;
        var failure = await Assert.ThrowsAsync<PhysicsNumericalException>(() =>
            device.Advance(admitted.Read, new(2), new WorkshopTickControls([])).AsTask());
        Assert.Equal(PhysicsFailure.RootBudget, failure.Failure);
        Assert.Equal(1, transport.Commits);
        Assert.Equal(2, transport.Discards);
        transport.Record[4] = (byte)PhysicsCandidateStatus.Committed;
        transport.Record[8] = (byte)PhysicsFailure.None;
        var next = await device.Admit(CompiledSceneFixture.Compile(new(new(1), Settings, WorkshopInstances.Empty)), new(1), Profile, new(3));
        device.Commit(next.Sequence);
        device.Discard(next.Sequence);
        Assert.Equal(2, transport.Commits);
        Assert.Equal(3, transport.Discards);
        await device.DisposeAsync();
    }

    [Fact]
    public async Task UnknownFailureCannotBecomeACommittedRead()
    {
        var transport = new HeldTransport(false);
        var device = new WorkshopGpuDevice(transport, Document);
        var pending = device.Admit(CompiledSceneFixture.Compile(new(new(1), Settings, WorkshopInstances.Empty)), new(1), Profile, new(1)).AsTask();
        transport.Record[8] = byte.MaxValue;
        transport.Held.SetResult();
        await Assert.ThrowsAsync<ArgumentException>(() => pending);
        Assert.Equal(0, transport.Commits);
        Assert.Equal(1, transport.Discards);
        await device.DisposeAsync();
    }

    [Theory]
    [InlineData(SimulationCadence.Hz60)]
    [InlineData(SimulationCadence.Hz240)]
    public async Task CandidateProfileMustMatchItsAdmissionBeforeCommit(SimulationCadence cadence)
    {
        var transport = new HeldTransport(false);
        var device = new WorkshopGpuDevice(transport, Document);
        var settings = Settings with { Simulation = cadence };
        var profile = Profile with { Cadence = cadence, Revision = new(2) };
        // Corrupt the actual staged descriptor to the old120Hz/revision1 profile.
        var pending = device.Admit(CompiledSceneFixture.Compile(new(new(1), settings, WorkshopInstances.Empty)), new(1), profile, new(1)).AsTask();
        BinaryPrimitives.WriteUInt32LittleEndian(transport.Record.AsSpan(48), (uint)SimulationCadence.Hz120);
        BinaryPrimitives.WriteUInt64LittleEndian(transport.Record.AsSpan(56), 1);
        transport.Held.SetResult();
        await Assert.ThrowsAsync<ArgumentException>(() => pending);
        Assert.Equal(0, transport.Commits);
        Assert.Equal(1, transport.Discards);
        await device.DisposeAsync();
    }

    [Fact]
    public async Task InvalidActivationAdmissionPreservesCommittedPhysicsAndLogicalCheckpoint()
    {
        var transport = new InvalidReadTransport();
        await using var device = new WorkshopGpuDevice(transport, Document);
        var construction = new WorkshopConstruction(new(1), Settings, new(
            WorkshopInput.Generic(new PartId("ball"),new(1), 0, 3, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(new PartId("switch"), new(2), 0, 1, 0, 0, 0, 0, 1),
            PlacedParts.Lamp(new(3), 3, 1, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(new PartId("delay"), new(4), -3, 1, 0, 0, 0, 0, 1)), Connections: new(new WorkshopConnection(
                new(2), WorkshopSocket.ActivationOut, new(3), WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation)));
        var first = await device.Admit(CompiledSceneFixture.Compile(construction), new(1), Profile, new(1));
        device.Commit(first.Sequence); device.Discard(first.Sequence);
        Assert.Equal(ActivationTimerPhase.Ready, first.Read.Timers[0].Phase);
        transport.CorruptNextRead = true;
        await Assert.ThrowsAsync<ArgumentException>(() => device.Admit(CompiledSceneFixture.Compile(construction), new(2), Profile, new(2)).AsTask());
        var continued = await device.Advance(first.Read, new(3), new WorkshopTickControls([]));
        Assert.Equal(first.Read.Epoch, continued.Read.Epoch);
        Assert.Equal(1UL, continued.Read.Tick.Value);
        Assert.Equal(first.Read.Activations[0], continued.Read.Activations[0]);
        Assert.Equal(first.Read.Activations[1], continued.Read.Activations[1]);
        Assert.Equal(first.Read.Timers[0], continued.Read.Timers[0]);
        Assert.Equal(first.Read.Bodies[0].Body.Id, continued.Read.Bodies[0].Body.Id);
        Assert.True(continued.Read.Bodies[0].Body.Velocity.Y < first.Read.Bodies[0].Body.Velocity.Y);
        device.Commit(continued.Sequence); device.Discard(continued.Sequence);
        var reset = await device.Admit(CompiledSceneFixture.Compile(construction), new(2), Profile, new(4));
        NoFixedCapacityTests.AssertSameState(first.Read, reset.Read, "valid retry after malformed candidate");
        device.Commit(reset.Sequence); device.Discard(reset.Sequence);
    }

    private sealed class InvalidReadTransport : IWorkshopGpuTransport
    {
        private readonly CuriousContraptions.Simulation.BrowserGpuTransport _real = new(new CuriousContraptions.Abstractions.EventRingPublication());
        public bool CorruptNextRead;
        public Task Initialize() => _real.Initialize();
        public Task Stage(ReadOnlyMemory<byte> input, WorkshopGpuOperation operation, PhysicsTickInputs inputs) => _real.Stage(input, operation, inputs);
        public WorkshopTransportCandidate Read()
        {
            var candidate = _real.Read();
            if (!CorruptNextRead) return candidate;
            CorruptNextRead = false;
            var malformed = candidate.World.ToArray();
            BinaryPrimitives.WriteUInt32LittleEndian(malformed.AsSpan(PhysicsGpuAbi.Layout(malformed).TriggerOffset(0) + 32), 1);
            return new(malformed, candidate.Interactions);
        }
        public void PrepareEvents(byte[] batch) => _real.PrepareEvents(batch);
        public void PublishEvents() => _real.PublishEvents();
        public void Commit() => _real.Commit();
        public void Discard() => _real.Discard();
        public bool DeviceReady() => _real.DeviceReady();
        public void Dispose() => _real.Dispose();
    }

    // Story 9R.1 D3: the committed read publishes the Counter and System Clock rows; a discarded (superseded) Advance candidate
    // leaves the committed rows unchanged, and a fresh admission (Reset) publishes them cleared.
    [Fact]
    public async Task CommittedReadPublishesClockAndCounterMemory_SupersededCandidateLeavesItUnchanged()
    {
        var transport = new HeldTransport(false); transport.Held.SetResult();
        var device = new WorkshopGpuDevice(transport, Document);
        var battery = PlacedParts.Battery(new(1), -2, 0, 0);
        var clock = PlacedParts.Clock(new(2), 0, 0, 0, 0, 0, 0, 1, 1.0f);
        var counter = PlacedParts.Counter(new(3), 2, 0, 0, 0, 0, 0, 1, 5);
        var construction = new WorkshopConstruction(new(1), Settings, new(battery, clock, counter), Connections: new(
            new WorkshopConnection(battery.Id, WorkshopSocket.Supply, clock.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical),
            new WorkshopConnection(clock.Id, WorkshopSocket.ActivationOut, counter.Id, WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation)));
        var network = WorkshopActivationCompiler.Compile(construction);
        var admitted = await device.Admit(CompiledSceneFixture.Compile(construction), new(1), Profile, new(1));
        Assert.Equal(network.ClearMemory().Count, admitted.Read.Memory.Count);
        Assert.Equal(OscillatorPhase.Stopped, network.Memory(admitted.Read.Memory, clock.Id).Phase);
        Assert.Equal(0U, network.Memory(admitted.Read.Memory, counter.Id).Count);
        device.Commit(admitted.Sequence); device.Discard(admitted.Sequence);

        var controls = WorkshopTickControls.For(admitted.Read.Electrical, (_, enable) => enable);
        var committedWorld = (byte[])transport.Record.Clone();
        var superseded = await device.Advance(admitted.Read, new(2), controls);
        Assert.Equal(OscillatorPhase.Running, network.Memory(superseded.Read.Memory, clock.Id).Phase);
        device.Discard(superseded.Sequence);
        transport.Record = committedWorld; // the discarded GPU candidate leaves the committed world in place
        var advanced = await device.Advance(admitted.Read, new(3), controls);
        device.Commit(advanced.Sequence); device.Discard(advanced.Sequence);
        var running = network.Memory(advanced.Read.Memory, clock.Id);
        Assert.Equal((OscillatorPhase.Running, 120UL), (running.Phase, running.DueTick));

        var reset = await device.Admit(CompiledSceneFixture.Compile(construction), new(2), Profile, new(4));
        Assert.Equal(network.ClearMemory()[0], reset.Read.Memory[0]);
        Assert.Equal(network.ClearMemory()[1], reset.Read.Memory[1]);
        device.Commit(reset.Sequence); device.Discard(reset.Sequence); await device.DisposeAsync();
    }

    // ----- Story 9R.0: device ownership and the gate's disposal -----

    [Fact]
    public async Task ABorrowedSimulationLeavesTheDeviceUsableUntilItsOwnerDisposesIt()
    {
        var transport = new HeldTransport(false); transport.Held.SetResult();
        await using var device = new WorkshopGpuDevice(transport, Document);
        await using (var borrower = new WorkshopSimulation(device, new ResetClock(), CompiledSceneFixture.Compile(new(new(1), Settings, WorkshopInstances.Empty)), new(1), new ResetInstallation(), GpuDeviceOwnership.Borrowed))
        {
            Assert.Equal(WorkshopCommandOutcome.Applied, (await borrower.Initialize()).Outcome);
            var construction = new WorkshopConstruction(new(2), Settings, new(WorkshopInput.Generic(new PartId("ball"),new(1), 0, 4, 0, 0, 0, 0, 1)));
            Assert.Equal(WorkshopCommandOutcome.Applied, (await borrower.Construct(CompiledSceneFixture.Compile(construction))).Outcome);
            Assert.Equal(WorkshopCommandOutcome.Applied, (await borrower.Run()).Outcome);
            Assert.Equal(WorkshopCommandOutcome.Applied, (await borrower.Reset()).Outcome);
        }
        Assert.Equal(0, transport.Disposals);
        await using (var owner = new WorkshopSimulation(device, new ResetClock(), CompiledSceneFixture.Compile(new(new(1), Settings, WorkshopInstances.Empty)), new(1), new ResetInstallation(), GpuDeviceOwnership.Owned))
        {
            Assert.Equal(WorkshopCommandOutcome.Applied, (await owner.Initialize()).Outcome);
            Assert.Equal(0UL, owner.Committed.Tick.Value);
            Assert.Empty(owner.Scene.Owners);
        }
        Assert.Equal(1, transport.Disposals);
    }

    [Fact]
    public async Task DisposalRejectsHeldAndQueuedWorkWithoutLeakingTheTransport()
    {
        var transport = new HeldTransport(true);
        await using var device = new WorkshopGpuDevice(transport, Document);
        var preparation = device.Prepare();
        var candidate = device.Admit(CompiledSceneFixture.Compile(new(new(1), Settings, WorkshopInstances.Empty)), new(1), Profile, new(1)).AsTask();
        await device.DisposeAsync();
        Assert.Equal(1, transport.Disposals);
        transport.Held.SetResult();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => preparation);
        // The queued candidate acquires, sees the disposal and releases normally: its failure is the device's, not the gate's.
        var queued = await Assert.ThrowsAsync<ObjectDisposedException>(() => candidate);
        Assert.Contains(nameof(WorkshopGpuDevice), queued.ObjectName, StringComparison.Ordinal);
        Assert.Equal(1, transport.Disposals);
    }

    [Fact]
    public async Task DisposalOfAnIdleDeviceRejectsLaterWork()
    {
        var transport = new HeldTransport(false); transport.Held.SetResult();
        await using var device = new WorkshopGpuDevice(transport, Document);
        await device.Prepare();
        await device.DisposeAsync();
        Assert.Equal(1, transport.Disposals);
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            device.Admit(CompiledSceneFixture.Compile(new(new(1), Settings, WorkshopInstances.Empty)), new(1), Profile, new(1)).AsTask());
    }

    [Fact]
    public async Task DisposalWithAHeldCandidateAllowsDiscardAndRejectsLaterWork()
    {
        var transport = new HeldTransport(false); transport.Held.SetResult();
        await using var device = new WorkshopGpuDevice(transport, Document);
        var admitted = await device.Admit(CompiledSceneFixture.Compile(new(new(1), Settings, WorkshopInstances.Empty)), new(1), Profile, new(1));
        await device.DisposeAsync();
        Assert.Equal(1, transport.Disposals);
        device.Discard(admitted.Sequence);
        Assert.Equal(1, transport.Disposals);
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            device.Admit(CompiledSceneFixture.Compile(new(new(1), Settings, WorkshopInstances.Empty)), new(1), Profile, new(2)).AsTask());
    }

    private sealed class HeldTransport(bool holdInitialization) : IWorkshopGpuTransport
    {
        private readonly CuriousContraptions.Abstractions.EventRingPublication _events = new();
        public TaskCompletionSource Held { get; } = new();
        public byte[] Record { get; set; } = [];
        public int Initializations, Stages, Reads, Commits, Disposals, Discards;
        public Task Initialize()
        { Initializations++; return holdInitialization ? Held.Task : Task.CompletedTask; }
        public PhysicsTickInputs? LastInputs { get; private set; }
        public Task Stage(ReadOnlyMemory<byte> input, WorkshopGpuOperation operation, PhysicsTickInputs inputs)
        {
            Stages++; LastInputs = inputs;
            if (operation == WorkshopGpuOperation.Admit) Record = input.ToArray();
            else
            {
                var source = BinaryPrimitives.ReadUInt32LittleEndian(Record.AsSpan(88));
                var steps = checked((uint)PhysicsGpuAbi.ReadProfile(Record).Substeps);
                BinaryPrimitives.WriteUInt64LittleEndian(Record.AsSpan(40),
                    BinaryPrimitives.ReadUInt64LittleEndian(Record.AsSpan(40)) + 1);
                BinaryPrimitives.WriteUInt32LittleEndian(Record.AsSpan(88), source + steps);
                var layout = PhysicsGpuAbi.Layout(Record);
                Record.AsSpan(layout.MotionOffset, layout.MotionBytes).Clear();
                BinaryPrimitives.WriteUInt32LittleEndian(Record.AsSpan(layout.MotionOffset + 4), steps);
                BinaryPrimitives.WriteUInt32LittleEndian(Record.AsSpan(layout.MotionOffset + 8), source);
                BinaryPrimitives.WriteUInt32LittleEndian(Record.AsSpan(layout.MotionOffset + 12), source + steps);
            }
            return Held.Task;
        }
        public WorkshopTransportCandidate Read() { Reads++; var bytes = (byte[])Record.Clone(); return new(bytes, PhysicsInteractionSnapshot.Empty); }
        public void PrepareEvents(byte[] batch) => _events.Prepare(batch);
        public void PublishEvents() => _events.Publish();
        public void Commit() { _events.Commit(); Commits++; }
        public void Discard() { _events.Discard(); Discards++; }
        public bool DeviceReady() => Disposals == 0;
        public void Dispose() { _events.Dispose(); Disposals++; }
    }
}
