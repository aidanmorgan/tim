using CuriousContraptions.Gpu;
using System.Buffers.Binary;

namespace CuriousContraptions.Tests;

// Exercises the production C# device owner with a held external transport, never a numerical substitute.
public sealed class WorkshopGpuDeviceTests
{
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
    public async Task PreparedResourcesAloneDoNotPublishAndLaterLossCanRequalify()
    {
        var transport = new HeldTransport(false);
        transport.Held.SetResult();
        var device = new WorkshopGpuDevice(transport, Document);
        await device.Prepare();
        Assert.Equal(1, transport.Initializations);
        Assert.Equal(0, transport.Stages);
        Assert.Equal(0, transport.Reads);
        Assert.Equal(0, transport.Commits);
        device.MarkLost();
        var admitted = await device.Admit(new(new(1), Settings, WorkshopInstances.Empty), new(2), Profile, new(1));
        Assert.Equal(2, transport.Initializations);
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
            device.Advance(admitted.Read, new(2)).AsTask());
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
            WorkshopInput.Switch(new(2), 0, 1, 0, 0, 0, 0, 1, ContactTriggerSettings.Default),
            WorkshopInput.Lamp(new(3), 3, 1, 0, 0, 0, 0, 1),
            WorkshopInput.Delay(new(4), -3, 1, 0, 0, 0, 0, 1, DelayDuration.Default)), Connections: new(new WorkshopConnection(
                new(2), WorkshopSocket.ActivationOut, new(3), WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation)));
        var first = await device.Admit(construction, new(1), Profile, new(1));
        device.Commit(first.Sequence); device.Discard(first.Sequence);
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var physical = (byte[])typeof(WorkshopGpuDevice).GetField("_committedWorld", flags)!.GetValue(device)!;
        var expected = (byte[])physical.Clone();
        var timers = first.Read.Timers;
        Assert.Equal(ActivationTimerPhase.Ready, timers[0].Phase);
        var logical = (PhysicsActivationRead)typeof(WorkshopGpuDevice).GetField("_committedActivations", flags)!.GetValue(device)!;
        transport.ReadMutation = bytes => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(PhysicsGpuAbi.TriggersOffset + 32), 1);
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

    private sealed class HeldTransport(bool holdInitialization) : IWorkshopGpuTransport
    {
        public TaskCompletionSource Held { get; } = new();
        public byte[] Record { get; private set; } = [];
        public int Initializations, Stages, Reads, Commits, Disposals, Discards;
        public Action<byte[]>? ReadMutation;
        public Task Initialize(string preamble)
        { Initializations++; return holdInitialization ? Held.Task : Task.CompletedTask; }
        public Task Stage(byte[] input, WorkshopGpuOperation operation)
        {
            Stages++;
            if (operation == WorkshopGpuOperation.Admit) Record = (byte[])input.Clone();
            else
            {
                var source = BinaryPrimitives.ReadUInt32LittleEndian(Record.AsSpan(88));
                var steps = checked((uint)PhysicsGpuAbi.ReadProfile(Record).Substeps);
                BinaryPrimitives.WriteUInt64LittleEndian(Record.AsSpan(40),
                    BinaryPrimitives.ReadUInt64LittleEndian(Record.AsSpan(40)) + 1);
                BinaryPrimitives.WriteUInt32LittleEndian(Record.AsSpan(88), source + steps);
                Record.AsSpan(PhysicsGpuAbi.MotionOffset).Clear();
                BinaryPrimitives.WriteUInt32LittleEndian(Record.AsSpan(PhysicsGpuAbi.MotionOffset + 4), steps);
                BinaryPrimitives.WriteUInt32LittleEndian(Record.AsSpan(PhysicsGpuAbi.MotionOffset + 8), source);
                BinaryPrimitives.WriteUInt32LittleEndian(Record.AsSpan(PhysicsGpuAbi.MotionOffset + 12), source + steps);
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
