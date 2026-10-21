using CuriousContraptions.Gpu;
using System.Buffers.Binary;

namespace CuriousContraptions.Tests;

// Exercises the production C# device owner with a held external transport, never a numerical substitute.
public sealed class WorkshopGpuDeviceTests
{

    [Theory]
    [InlineData(0f)]
    [InlineData(.2f)]
    public async Task RealResetLifecycleRebuildsAllEightPrismaticImpulseBytes(float compression)
    {
        var transport = new HeldTransport(false); transport.Held.SetResult();
        // The test owns the device; the simulation borrows it (disposed first, it leaves the device to the test).
        await using var device = new WorkshopGpuDevice(transport, Document);
        await using var simulation = new WorkshopSimulation(device, new ResetClock(), Settings, new(1),
            new ResetInstallation(), GpuDeviceOwnership.Borrowed);
        await simulation.Initialize();
        var construction = new WorkshopConstruction(new(2), Settings, new(
            WorkshopInput.Springboard(new(1), 0, 3, 0, 0, 0, 0, 1, SpringboardSettings.Default with { InitialCompression = compression })));
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Construct(construction)).Outcome);
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        byte[] Committed() => ((byte[])typeof(WorkshopGpuDevice).GetField("_committedWorld", flags)!.GetValue(device)!).ToArray();
        var original = Committed();
        var layout = PhysicsGpuAbi.Layout(original);
        var offset = layout.PrismaticOffset(0) + PhysicsGpuAbi.PrismaticImpulseOffset;
        for (var run = 0; run < 2; run++)
        {
            Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Run()).Outcome);
            transport.ReadMutation = bytes =>
            {
                var motion = bytes.AsSpan(layout.MotionOffset, layout.MotionBytes);
                var first = BinaryPrimitives.ReadUInt32LittleEndian(motion[8..]);
                BinaryPrimitives.WriteUInt32LittleEndian(motion, 4);
                var body = bytes.AsSpan(layout.BodyOffset(2), PhysicsGpuAbi.BodyBytes);
                for (var step = 0; step < 4; step++)
                {
                    var piece = motion.Slice(PhysicsMotionRead.HeaderBytes + step * PhysicsMotionRead.PieceBytes, PhysicsMotionRead.PieceBytes);
                    BinaryPrimitives.WriteUInt32LittleEndian(piece, (uint)PhysicsMotionKind.FreePolynomial);
                    BinaryPrimitives.WriteUInt32LittleEndian(piece[4..], first + (uint)step);
                    BinaryPrimitives.WriteUInt32LittleEndian(piece[8..], first + (uint)step + 1);
                    BinaryPrimitives.WriteUInt32LittleEndian(piece[12..], first + (uint)step);
                    BinaryPrimitives.WriteUInt16LittleEndian(piece[22..], BitConverter.HalfToUInt16Bits((Half)480));
                    body[..8].CopyTo(piece[24..]); body[16..28].CopyTo(piece[32..]);
                    body[32..44].CopyTo(piece[48..]); body[48..64].CopyTo(piece[64..]);
                }
                for (var lane = 0; lane < 8; lane++)
                    BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(offset + lane * 4), lane < 6 ? -.125f * (lane + 1) : .125f);
            };
            Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Advance()).Outcome);
            Assert.Contains(Committed().AsSpan(offset, 32).ToArray(), value => value != 0);
            transport.ReadMutation = null;
            Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Reset()).Outcome);
            var reset = Committed();
            Assert.Equal(0UL, simulation.Committed.Tick.Value);
            Assert.All(reset.AsSpan(offset, 32).ToArray(), value => Assert.Equal(0, value));
            Assert.Equal(original.AsSpan(layout.PrismaticsOffset).ToArray(), reset.AsSpan(layout.PrismaticsOffset).ToArray());
            Assert.Equal(original.AsSpan(PhysicsWorldLayout.BodiesOffset, 3 * PhysicsGpuAbi.BodyBytes).ToArray(),
                reset.AsSpan(PhysicsWorldLayout.BodiesOffset, 3 * PhysicsGpuAbi.BodyBytes).ToArray());
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
        public ValueTask Prepare(CommandSequence owner, WorkshopRead read, WorkshopConstruction construction, WorkshopGpuProfile profile, WorkshopSimulationPhase phase) => ValueTask.CompletedTask;
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
        var candidate = device.Admit(new(new(1), Settings, WorkshopInstances.Empty), new(1), Profile, new(1)).AsTask();
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
        var candidate = device.Admit(new(new(1), Settings, WorkshopInstances.Empty), new(1), Profile, new(1)).AsTask();
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
        var candidate = device.Admit(new(new(1), Settings, WorkshopInstances.Empty), new(1), Profile, new(1)).AsTask();
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
        var admitted = await device.Admit(new(new(1), Settings, WorkshopInstances.Empty), new(2), Profile, new(1));
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
        var pending = device.Admit(new(new(1), Settings, WorkshopInstances.Empty), new(1), Profile, new(1)).AsTask();
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
            device.Admit(new(new(1), Settings, WorkshopInstances.Empty), new(1), Profile, new(2)).AsTask());
        Assert.Equal(0, transport.Commits);
    }

    [Fact]
    public async Task ActualAdapterRejectsMalformedDescriptorBeforeCommit()
    {
        var transport = new HeldTransport(false);
        var device = new WorkshopGpuDevice(transport, Document);
        var pending = device.Admit(new(new(1), Settings, WorkshopInstances.Empty), new(1), Profile, new(1)).AsTask();
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
        var admitted = await device.Admit(new(new(1), Settings, WorkshopInstances.Empty), new(1), Profile, new(1));
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
        var next = await device.Admit(new(new(1), Settings, WorkshopInstances.Empty), new(1), Profile, new(3));
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
        var pending = device.Admit(new(new(1), Settings, WorkshopInstances.Empty), new(1), Profile, new(1)).AsTask();
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
        var pending = device.Admit(new(new(1), settings, WorkshopInstances.Empty), new(1), profile, new(1)).AsTask();
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
        var transport = new HeldTransport(false); transport.Held.SetResult();
        var device = new WorkshopGpuDevice(transport, Document);
        var construction = new WorkshopConstruction(new(1), Settings, new(
            WorkshopInput.Basketball(new(1), 0, 3, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(PartId.ImpactSwitch, new(2), 0, 1, 0, 0, 0, 0, 1),
            WorkshopInput.Lamp(new(3), 3, 1, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(PartId.Delay, new(4), -3, 1, 0, 0, 0, 0, 1)), Connections: new(new WorkshopConnection(
                new(2), WorkshopSocket.ActivationOut, new(3), WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation)));
        var first = await device.Admit(construction, new(1), Profile, new(1));
        device.Commit(first.Sequence); device.Discard(first.Sequence);
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var physical = (byte[])typeof(WorkshopGpuDevice).GetField("_committedWorld", flags)!.GetValue(device)!;
        var expected = (byte[])physical.Clone();
        var timers = first.Read.Timers;
        Assert.Equal(ActivationTimerPhase.Ready, timers[0].Phase);
        var logical = (PhysicsActivationRead)typeof(WorkshopGpuDevice).GetField("_committedActivations", flags)!.GetValue(device)!;
        transport.ReadMutation = bytes => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(PhysicsGpuAbi.Layout(bytes).TriggerOffset(0) + 32), 1);
        await Assert.ThrowsAsync<ArgumentException>(() => device.Admit(construction, new(2), Profile, new(2)).AsTask());
        Assert.Equal(1, transport.Commits);
        Assert.Equal(expected, (byte[])typeof(WorkshopGpuDevice).GetField("_committedWorld", flags)!.GetValue(device)!);
        var after = (PhysicsActivationRead)typeof(WorkshopGpuDevice).GetField("_committedActivations", flags)!.GetValue(device)!;
        Assert.Equal(logical[0], after[0]); Assert.Equal(logical[1], after[1]);
        Assert.Equal(timers[0], ((PhysicsTimerRead)typeof(WorkshopGpuDevice).GetField("_committedTimers", flags)!.GetValue(device)!)[0]);
        transport.ReadMutation = null;
        var reset = await device.Admit(construction, new(2), Profile, new(3));
        Assert.Equal(ActivationPhase.Clear, reset.Read.Activations[0].Phase);
        Assert.Equal(timers[0], reset.Read.Timers[0]);
        device.Commit(reset.Sequence); device.Discard(reset.Sequence); await device.DisposeAsync();
    }

    // Story 9R.1 D3: the committed read publishes the Counter and System Clock rows; a discarded (superseded) Advance candidate
    // leaves the committed rows unchanged, and a fresh admission (Reset) publishes them cleared.
    [Fact]
    public async Task CommittedReadPublishesClockAndCounterMemory_SupersededCandidateLeavesItUnchanged()
    {
        var transport = new HeldTransport(false); transport.Held.SetResult();
        var device = new WorkshopGpuDevice(transport, Document);
        var battery = WorkshopInput.Battery(new(1), -2, 0, 0, 0, 0, 0, 1, ElectricalSourceSettings.Default);
        var clock = WorkshopInput.Clock(new(2), 0, 0, 0, 0, 0, 0, 1, 1.0f);
        var counter = WorkshopInput.Counter(new(3), 2, 0, 0, 0, 0, 0, 1, 5);
        var construction = new WorkshopConstruction(new(1), Settings, new(battery, clock, counter), Connections: new(
            new WorkshopConnection(battery.Id, WorkshopSocket.Supply, clock.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical),
            new WorkshopConnection(clock.Id, WorkshopSocket.ActivationOut, counter.Id, WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation)));
        var network = WorkshopActivationCompiler.Compile(construction);
        var admitted = await device.Admit(construction, new(1), Profile, new(1));
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
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var committedMemory = (PhysicsMemoryRead)typeof(WorkshopGpuDevice).GetField("_committedMemory", flags)!.GetValue(device)!;
        Assert.Equal(OscillatorPhase.Stopped, network.Memory(committedMemory, clock.Id).Phase);

        var advanced = await device.Advance(admitted.Read, new(3), controls);
        device.Commit(advanced.Sequence); device.Discard(advanced.Sequence);
        var running = network.Memory(advanced.Read.Memory, clock.Id);
        Assert.Equal((OscillatorPhase.Running, 120UL), (running.Phase, running.DueTick));

        var reset = await device.Admit(construction, new(2), Profile, new(4));
        Assert.Equal(network.ClearMemory()[0], reset.Read.Memory[0]);
        Assert.Equal(network.ClearMemory()[1], reset.Read.Memory[1]);
        device.Commit(reset.Sequence); device.Discard(reset.Sequence); await device.DisposeAsync();
    }

    // ----- Story 9R.0: device ownership and the gate's disposal -----

    private static readonly System.Reflection.FieldInfo GateField =
        typeof(WorkshopGpuDevice).GetField("_gate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
    // AvailableWaitHandle throws ObjectDisposedException exactly when the semaphore is disposed (and changes no count).
    private static bool GateDisposed(WorkshopGpuDevice device)
    {
        var gate = (SemaphoreSlim)GateField.GetValue(device)!;
        try { _ = gate.AvailableWaitHandle; return false; }
        catch (ObjectDisposedException) { return true; }
    }

    [Fact]
    public async Task ABorrowedWarmupLeavesTheDeviceToItsOwnerWhichThenInitialisesAFreshSimulation()
    {
        var transport = new HeldTransport(false); transport.Held.SetResult();
        await using var device = new WorkshopGpuDevice(transport, Document);
        await using (var warmup = new WorkshopSimulation(device, new ResetClock(), Settings, new(1), new ResetInstallation(), GpuDeviceOwnership.Borrowed))
        {
            Assert.Equal(WorkshopCommandOutcome.Applied, (await warmup.Initialize()).Outcome);
            var construction = new WorkshopConstruction(new(2), Settings, new(WorkshopInput.Basketball(new(1), 0, 4, 0, 0, 0, 0, 1)));
            Assert.Equal(WorkshopCommandOutcome.Applied, (await warmup.Construct(construction)).Outcome);
            Assert.Equal(WorkshopCommandOutcome.Applied, (await warmup.Run()).Outcome);
            Assert.Equal(WorkshopCommandOutcome.Applied, (await warmup.Reset()).Outcome);
        }
        Assert.Equal(0, transport.Disposals);
        Assert.False(GateDisposed(device));
        await using (var owner = new WorkshopSimulation(device, new ResetClock(), Settings, new(1), new ResetInstallation(), GpuDeviceOwnership.Owned))
        {
            Assert.Equal(WorkshopCommandOutcome.Applied, (await owner.Initialize()).Outcome);
            Assert.Equal(0UL, owner.Committed.Tick.Value);
            Assert.Empty(owner.Construction.Instances);
        }
        Assert.Equal(1, transport.Disposals);
    }

    [Fact]
    public async Task DisposalWithAQueuedCandidateDisposesTheGateOnlyAfterTheQueueDrains()
    {
        var transport = new HeldTransport(true);
        await using var device = new WorkshopGpuDevice(transport, Document);
        var preparation = device.Prepare();
        var candidate = device.Admit(new(new(1), Settings, WorkshopInstances.Empty), new(1), Profile, new(1)).AsTask();
        await device.DisposeAsync();
        Assert.False(GateDisposed(device));
        transport.Held.SetResult();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => preparation);
        // The queued candidate acquires, sees the disposal and releases normally: its failure is the device's, not the gate's.
        var queued = await Assert.ThrowsAsync<ObjectDisposedException>(() => candidate);
        Assert.Contains(nameof(WorkshopGpuDevice), queued.ObjectName, StringComparison.Ordinal);
        Assert.True(GateDisposed(device));
    }

    [Fact]
    public async Task DisposalWhileTheGateIsFreeDisposesItAtOnceAndRejectsLaterWork()
    {
        var transport = new HeldTransport(false); transport.Held.SetResult();
        await using var device = new WorkshopGpuDevice(transport, Document);
        await device.Prepare();
        await device.DisposeAsync();
        Assert.True(GateDisposed(device));
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            device.Admit(new(new(1), Settings, WorkshopInstances.Empty), new(1), Profile, new(1)).AsTask());
    }

    [Fact]
    public async Task DisposalWhileACandidateHoldsTheGateDisposesItWhenTheCandidateIsDiscarded()
    {
        var transport = new HeldTransport(false); transport.Held.SetResult();
        await using var device = new WorkshopGpuDevice(transport, Document);
        var admitted = await device.Admit(new(new(1), Settings, WorkshopInstances.Empty), new(1), Profile, new(1));
        await device.DisposeAsync();
        Assert.False(GateDisposed(device));
        device.Discard(admitted.Sequence);
        Assert.True(GateDisposed(device));
    }

    private sealed class HeldTransport(bool holdInitialization) : IWorkshopGpuTransport
    {
        public TaskCompletionSource Held { get; } = new();
        public byte[] Record { get; set; } = [];
        public int Initializations, Stages, Reads, Commits, Disposals, Discards;
        public Action<byte[]>? ReadMutation;
        public Task Initialize(string preamble)
        { Initializations++; return holdInitialization ? Held.Task : Task.CompletedTask; }
        public PhysicsTickInputs? LastInputs { get; private set; }
        public Task Stage(byte[] input, WorkshopGpuOperation operation, PhysicsTickInputs inputs)
        {
            Stages++; LastInputs = inputs;
            if (operation == WorkshopGpuOperation.Admit) Record = (byte[])input.Clone();
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
        public byte[] Read() { Reads++; var bytes = (byte[])Record.Clone(); ReadMutation?.Invoke(bytes); return bytes; }
        public void Commit() => Commits++;
        public void Discard() => Discards++;
        public bool DeviceReady() => Disposals == 0;
        public void Dispose() => Disposals++;
    }
}
