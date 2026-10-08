using CuriousContraptions.Gpu;
using Xunit;

namespace CuriousContraptions.Tests;

/// <summary>Host transaction controls only. The controllable device is not a physical reference or GPU proof.</summary>
public sealed class WorkshopSimulationTests
{
    private static WorkshopConstruction BallConstruction(ulong revision = 2) =>
        new(new(revision), WorkshopInput.Basketball(new(0x20000000000001), 0, 4, 0, 0, 0, 0, 1));

    [Fact]
    public async Task RunRemainsPendingUntilGpuAdmissionCommits()
    {
        var device = new ControlledDevice();
        await using var simulation = new WorkshopSimulation(device);
        await simulation.Initialize();
        var run = simulation.Run().AsTask();
        Assert.Equal(WorkshopSimulationPhase.Starting, simulation.Phase);
        Assert.False(run.IsCompleted);
        Assert.Equal(WorkshopCommandOutcome.Rejected, (await simulation.Run()).Outcome);
        device.CompleteAdmission(0);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await run).Outcome);
        Assert.Equal(WorkshopSimulationPhase.Running, simulation.Phase);
        Assert.Equal(0UL, simulation.Committed.Tick.Value);
    }

    [Fact]
    public async Task RejectedGpuAdmissionPreservesExactConstructionAndRead()
    {
        var device = new ControlledDevice();
        await using var simulation = new WorkshopSimulation(device);
        await simulation.Initialize();
        var construction = simulation.Construction;
        var read = simulation.Committed;
        var admission = simulation.Construct(BallConstruction()).AsTask();
        device.Admissions[0].Completion.SetException(new GpuAdmissionException("Overlap."));
        Assert.Equal(WorkshopRejection.GpuAdmission, (await admission).Reason);
        Assert.Equal(construction, simulation.Construction);
        Assert.Equal(read, simulation.Committed);
        Assert.Equal(WorkshopSimulationPhase.Building, simulation.Phase);
    }

    [Fact]
    public async Task ResetSupersedesPendingRunWithoutLateOverwrite()
    {
        var device = new ControlledDevice();
        await using var simulation = new WorkshopSimulation(device);
        await simulation.Initialize();
        var construction = BallConstruction();
        var admission = simulation.Construct(construction).AsTask();
        device.CompleteAdmission(0);
        await admission;
        var firstRun = simulation.Run().AsTask();
        var reset = simulation.Reset().AsTask();
        device.CompleteAdmission(2);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await reset).Outcome);
        var restored = simulation.Committed;
        device.CompleteAdmission(1);
        Assert.Equal(WorkshopCommandOutcome.Superseded, (await firstRun).Outcome);
        Assert.Equal(restored, simulation.Committed);
        Assert.Equal(construction, simulation.Construction);
        Assert.Equal(WorkshopSimulationPhase.Building, simulation.Phase);
    }

    [Fact]
    public async Task OldAdvanceCannotClearNewEpochsInFlightOwner()
    {
        var device = new ControlledDevice();
        await using var simulation = new WorkshopSimulation(device);
        await simulation.Initialize();
        var run = simulation.Run().AsTask();
        device.CompleteAdmission(0);
        await run;
        var oldAdvance = simulation.Advance().AsTask();
        var reset = simulation.Reset().AsTask();
        device.CompleteAdmission(1);
        await reset;
        device.CompleteAdvance(0);
        Assert.Equal(WorkshopCommandOutcome.Superseded, (await oldAdvance).Outcome);
        run = simulation.Run().AsTask();
        device.CompleteAdmission(2);
        await run;
        var newAdvance = simulation.Advance().AsTask();
        Assert.Equal(WorkshopRejection.Busy, (await simulation.Advance()).Reason);
        device.CompleteAdvance(1);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await newAdvance).Outcome);
        Assert.Equal(1UL, simulation.Committed.Tick.Value);
    }

    [Fact]
    public async Task DeviceLossInvalidatesPendingCommitAndRetainsLastRead()
    {
        var device = new ControlledDevice();
        await using var simulation = new WorkshopSimulation(device);
        await simulation.Initialize();
        var run = simulation.Run().AsTask();
        device.CompleteAdmission(0);
        await run;
        var committed = simulation.Committed;
        var advance = simulation.Advance().AsTask();
        simulation.DeviceLost();
        device.CompleteAdvance(0);
        Assert.Equal(WorkshopCommandOutcome.Superseded, (await advance).Outcome);
        Assert.Equal(WorkshopSimulationPhase.Faulted, simulation.Phase);
        Assert.Equal(WorkshopRejection.DeviceLost, simulation.Fault);
        Assert.Equal(committed, simulation.Committed);
    }

    [Fact]
    public async Task ForeignGpuBodyCannotReplaceConstruction()
    {
        var device = new ControlledDevice();
        await using var simulation = new WorkshopSimulation(device);
        await simulation.Initialize();
        var before = simulation.Construction;
        var pending = simulation.Construct(BallConstruction()).AsTask();
        var request = device.Admissions[0];
        var correct = ControlledDevice.Initial(request.Construction, request.Epoch);
        request.Completion.SetResult(new(request.Sequence, correct with { Ball = correct.Ball!.Value with { Id = new(7) } }));
        Assert.Equal(WorkshopCommandOutcome.Faulted, (await pending).Outcome);
        Assert.Equal(before, simulation.Construction);
    }

    [Fact]
    public async Task DisposeInvalidatesPendingAdmissionAndOwnsDeviceOnce()
    {
        var device = new ControlledDevice();
        var simulation = new WorkshopSimulation(device);
        await simulation.Initialize();
        var pending = simulation.Run().AsTask();
        await simulation.DisposeAsync();
        device.CompleteAdmission(0);
        Assert.Equal(WorkshopCommandOutcome.Superseded, (await pending).Outcome);
        await simulation.DisposeAsync();
        Assert.Equal(1, device.Disposals);
        Assert.Equal(WorkshopSimulationPhase.Disposed, simulation.Phase);
    }

    [Fact]
    public async Task StartupRejectsRunUntilGpuReadyAndRunPreservesGeneration()
    {
        var device = new ControlledDevice { HoldStartup = true };
        await using var simulation = new WorkshopSimulation(device);
        var startup = simulation.Initialize().AsTask();
        Assert.Equal(WorkshopSimulationPhase.Initializing, simulation.Phase);
        Assert.Equal(WorkshopRejection.WrongPhase, (await simulation.Run()).Reason);
        device.CompleteAdmission(0);
        await startup;
        var generation = simulation.Epoch;
        var run = simulation.Run().AsTask();
        device.CompleteAdmission(1);
        await run;
        Assert.Equal(generation, simulation.Epoch);
    }

    [Fact]
    public async Task HeldResetRejectsRepeatedRequestsAndRestoresBitsAfterChangedPose()
    {
        var device = new ControlledDevice();
        await using var simulation = new WorkshopSimulation(device);
        await simulation.Initialize();
        var construction = BallConstruction();
        var admission = simulation.Construct(construction).AsTask();
        device.CompleteAdmission(0); await admission;
        var run = simulation.Run().AsTask();
        device.CompleteAdmission(1); await run;
        var initialGeneration = simulation.Epoch;
        var advance = simulation.Advance().AsTask();
        device.CompleteAdvance(0, read => read with { Ball = read.Ball!.Value with
            { Cell = new(0, 48, 0), Local = new((Half)0, (Half)0.25, (Half)0), Velocity = new((Half)0, (Half)(-0.1), (Half)0) } });
        await advance;
        Assert.NotEqual(construction.Ball!.Value.Cell, simulation.Committed.Ball!.Value.Cell);
        var reset = simulation.Reset().AsTask();
        for (var i = 0; i < 8; i++) Assert.Equal(WorkshopRejection.Busy, (await simulation.Reset()).Reason);
        Assert.Equal(3, device.Admissions.Count);
        device.CompleteAdmission(2); await reset;
        Assert.Equal(initialGeneration.Value + 1, simulation.Epoch.Value);
        Assert.Equal(ControlledDevice.Initial(construction, simulation.Epoch).Ball!.Value.Encode(),
            simulation.Committed.Ball!.Value.Encode());
        Assert.Equal(construction, simulation.Construction);
    }

    [Fact]
    public async Task ChangedSignedZeroInAdmissionCannotCommit()
    {
        var device = new ControlledDevice();
        await using var simulation = new WorkshopSimulation(device);
        await simulation.Initialize();
        var before = simulation.Committed;
        var command = simulation.Construct(BallConstruction()).AsTask();
        var request = device.Admissions[0];
        var candidate = ControlledDevice.Initial(request.Construction, request.Epoch);
        var ball = candidate.Ball!.Value;
        request.Completion.SetResult(new(request.Sequence, candidate with
            { Ball = ball with { Local = ball.Local with { X = BitConverter.UInt16BitsToHalf(0x8000) } } }));
        Assert.Equal(WorkshopCommandOutcome.Faulted, (await command).Outcome);
        Assert.Equal(before, simulation.Committed);
    }

    [Fact]
    public async Task CancelBeforeCommitRetainsOldReadAndOriginalOutcome()
    {
        var device = new ControlledDevice();
        await using var simulation = new WorkshopSimulation(device);
        await simulation.Initialize();
        var old = simulation.Committed;
        var request = simulation.Construct(BallConstruction()).AsTask();
        Assert.Equal(WorkshopCommandOutcome.Applied, simulation.CancelPending().Outcome);
        Assert.Equal(old with { Revision = new(1) }, simulation.Committed);
        Assert.Equal(WorkshopSimulationPhase.Building, simulation.Phase);
        Assert.False(request.IsCompleted);
        device.CompleteAdmission(0);
        Assert.Equal(WorkshopCommandOutcome.Cancelled, (await request).Outcome);
        Assert.Equal(old with { Revision = new(1) }, simulation.Committed);
        Assert.Equal(WorkshopRejection.AlreadyCommitted, simulation.CancelPending().Reason);
        var reset = simulation.Reset().AsTask();
        device.CompleteAdmission(1);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await reset).Outcome);
        Assert.Null(simulation.Committed.Ball);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AdvancePreservesLateralBitsAndRejectsLateralCandidateMutation(bool mutate)
    {
        var device = new ControlledDevice();
        await using var simulation = new WorkshopSimulation(device);
        await simulation.Initialize();
        var ball = BallConstruction().Ball!.Value;
        ball = ball with { Local = ball.Local with { X = BitConverter.UInt16BitsToHalf(0x8000) } };
        var construction = new WorkshopConstruction(new(2), ball);
        var admit = simulation.Construct(construction).AsTask();
        device.CompleteAdmission(0); await admit;
        var run = simulation.Run().AsTask();
        device.CompleteAdmission(1); await run;
        var before = simulation.Committed;
        var advance = simulation.Advance().AsTask();
        device.CompleteAdvance(0, read => mutate ? read with
            { Ball = read.Ball!.Value with { Local = read.Ball.Value.Local with { X = (Half)0 } } } : read);
        Assert.Equal(mutate ? WorkshopCommandOutcome.Faulted : WorkshopCommandOutcome.Applied, (await advance).Outcome);
        if (mutate) Assert.Equal(before, simulation.Committed);
        else Assert.Equal((ushort)0x8000, BitConverter.HalfToUInt16Bits(simulation.Committed.Ball!.Value.Local.X));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancelledResetKeepsPriorModeAndOnlyCancelAdvancesAuthority(bool completed)
    {
        var device = new ControlledDevice();
        await using var simulation = new WorkshopSimulation(device);
        await simulation.Initialize();
        Assert.Equal(new AuthorityRevision(0), simulation.Revision);
        var run = simulation.Run().AsTask();
        device.CompleteAdmission(0); await run;
        Assert.Equal(new AuthorityRevision(1), simulation.Revision);
        if (completed)
        {
            for (var i = 0; i < 3600; i++)
            {
                var tick = simulation.Advance().AsTask();
                device.CompleteAdvance(i);
                Assert.Equal(WorkshopCommandOutcome.Applied, (await tick).Outcome);
            }
            simulation.CompleteRun();
        }
        var before = simulation.Committed;
        var phase = simulation.Phase;
        var epoch = simulation.Epoch;
        var reset = simulation.Reset().AsTask();
        Assert.Equal(WorkshopCommandOutcome.Applied, simulation.CancelPending().Outcome);
        Assert.Equal(phase, simulation.Phase);
        Assert.Equal(before with { Revision = new(before.Revision.Value + 1) }, simulation.Committed);
        device.CompleteAdmission(1);
        Assert.Equal(WorkshopCommandOutcome.Cancelled, (await reset).Outcome);
        Assert.Equal(epoch, simulation.Epoch);
        Assert.Equal(before.Revision.Value + 1, simulation.Revision.Value);
        var appliedReset = simulation.Reset().AsTask();
        device.CompleteAdmission(2);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await appliedReset).Outcome);
        Assert.Equal(before.Revision.Value + 2, simulation.Revision.Value);
        Assert.Equal(epoch.Value + 1, simulation.Epoch.Value);
        Assert.Equal(WorkshopSimulationPhase.Building, simulation.Phase);
        Assert.Equal(new ConstructionRevision(1), simulation.Construction.Revision);
    }

    [Fact]
    public async Task AuthorityExhaustionFaultsBeforeGpuMutation()
    {
        var device = new ControlledDevice();
        await using var simulation = new WorkshopSimulation(device);
        await simulation.Initialize();
        // Native negative boundary injection into the actual owner; no production setter is exposed.
        var exhausted = simulation.Committed with { Revision = new(ulong.MaxValue) };
        typeof(WorkshopSimulation).GetProperty(nameof(WorkshopSimulation.Committed))!.SetValue(simulation, exhausted);
        Assert.Equal(WorkshopRejection.IdentityExhausted, (await simulation.Run()).Reason);
        Assert.Equal(exhausted, simulation.Committed);
        Assert.Empty(device.Admissions);
        Assert.Equal(WorkshopSimulationPhase.Faulted, simulation.Phase);
    }

    private sealed class ControlledDevice : IWorkshopGpuDevice
    {
        public readonly List<Admission> Admissions = [];
        private readonly List<(WorkshopRead Source, CommandSequence Sequence, TaskCompletionSource<WorkshopGpuCandidate> Completion)> _advances = [];
        public int Disposals { get; private set; }
        private bool _initializing = true;
        public bool HoldStartup { get; init; }
        public ValueTask<WorkshopGpuCandidate> Admit(WorkshopConstruction construction, SimulationEpoch epoch, CommandSequence sequence)
        {
            if (_initializing && !HoldStartup) { _initializing = false; return ValueTask.FromResult(new WorkshopGpuCandidate(sequence, Initial(construction, epoch))); }
            var completion = new TaskCompletionSource<WorkshopGpuCandidate>();
            Admissions.Add(new(construction, epoch, sequence, completion));
            return new(completion.Task);
        }
        public ValueTask<WorkshopGpuCandidate> Advance(WorkshopRead source, CommandSequence sequence)
        {
            var completion = new TaskCompletionSource<WorkshopGpuCandidate>();
            _advances.Add((source, sequence, completion));
            return new(completion.Task);
        }
        public void CompleteAdmission(int index)
        {
            var request = Admissions[index];
            request.Completion.SetResult(new(request.Sequence, Initial(request.Construction, request.Epoch)));
        }
        public static WorkshopRead Initial(WorkshopConstruction construction, SimulationEpoch epoch) =>
            new(epoch, new(0), construction.Ball is { } ball
                ? new CanonicalBody(ball.Id, epoch.Value, 0, ball.Cell, ball.Local, default) : null);
        public void CompleteAdvance(int index, Func<WorkshopRead, WorkshopRead>? transform = null)
        {
            var (source, sequence, completion) = _advances[index];
            var tick = source.Tick.Value + 1;
            var read = source with
            {
                Revision = default,
                Tick = new(tick),
                Ball = source.Ball is { } ball ? ball with { Tick = tick } : null
            };
            completion.SetResult(new(sequence, transform is null ? read : transform(read)));
        }
        public void Commit(CommandSequence sequence) { }
        public void Discard(CommandSequence sequence) { }
        public ValueTask DisposeAsync() { Disposals++; return ValueTask.CompletedTask; }
        public sealed record Admission(WorkshopConstruction Construction, SimulationEpoch Epoch, CommandSequence Sequence,
            TaskCompletionSource<WorkshopGpuCandidate> Completion);
    }
}
