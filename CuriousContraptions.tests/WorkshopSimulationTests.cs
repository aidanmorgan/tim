using CuriousContraptions.Gpu;
using Xunit;

namespace CuriousContraptions.Tests;

/// <summary>Host transaction controls only. The controllable device is not a physical reference or GPU proof.</summary>
public sealed class WorkshopSimulationTests
{
    private static readonly WorkshopCadenceSettings Settings = WorkshopCadenceSettings.Default();
    private static WorkshopSimulation Create(ControlledDevice device, ControlledClock? clock = null, ControlledInstallation? installation = null) =>
        new(device, clock ?? new ControlledClock(), Settings, new(1), installation ?? new ControlledInstallation());
    private static WorkshopConstruction BallConstruction(ulong revision = 2) =>
        new(new(revision), WorkshopInput.Basketball(new(0x20000000000001), 0, 4, 0, 0, 0, 0, 1), Settings);

    [Fact]
    public async Task SaveChangesRevisionOnceWithoutChangingWorldOrDispatchingGpu()
    {
        var device = new ControlledDevice();
        await using var simulation = Create(device);
        await simulation.Initialize();
        var before = simulation.Committed;
        var construction = simulation.Construction;
        var commits = device.Commits;
        Assert.Equal(WorkshopCommandOutcome.Applied, simulation.Save().Outcome);
        Assert.Equal(before with { Revision = new(before.Revision.Value + 1) }, simulation.Committed);
        Assert.Equal(construction, simulation.Construction);
        Assert.Equal(commits, device.Commits);
        Assert.Empty(device.Admissions);
        await simulation.Run();
        var running = simulation.Committed;
        Assert.Equal(WorkshopRejection.WrongPhase, simulation.Save().Reason);
        Assert.Equal(running, simulation.Committed);
    }

    [Fact]
    public async Task SaveCannotOvertakePendingConstruction()
    {
        var device = new ControlledDevice();
        await using var simulation = Create(device);
        await simulation.Initialize();
        var before = simulation.Committed;
        var admission = simulation.Construct(BallConstruction()).AsTask();
        Assert.Equal(WorkshopCommandOutcome.Rejected, simulation.Save().Outcome);
        Assert.Equal(before, simulation.Committed);
        device.CompleteAdmission(0);
        await admission;
    }

    [Fact]
    public async Task RunRemainsPendingUntilRecipientsPrepareWithoutRestampingOrGpuWork()
    {
        var device = new ControlledDevice();
        var installation = new ControlledInstallation();
        await using var simulation = Create(device, installation: installation);
        await simulation.Initialize();
        var before = simulation.Committed;
        var commits = device.Commits;
        installation.HoldNext = true;
        var run = simulation.Run().AsTask();
        Assert.Equal(WorkshopSimulationPhase.Building, simulation.Phase);
        Assert.False(run.IsCompleted);
        Assert.Equal(WorkshopRejection.Busy, (await simulation.Run()).Reason);
        Assert.Equal(before, simulation.Committed);
        Assert.Empty(device.Admissions);
        installation.Complete(0);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await run).Outcome);
        Assert.Equal(WorkshopSimulationPhase.Running, simulation.Phase);
        Assert.Equal(before.Capture, simulation.Committed.Capture);
        Assert.Equal(before.Tick, simulation.Committed.Tick);
        Assert.Equal(commits, device.Commits);
    }

    [Fact]
    public async Task RejectedGpuAdmissionPreservesExactConstructionAndRead()
    {
        var device = new ControlledDevice();
        await using var simulation = Create(device);
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
        var installation = new ControlledInstallation();
        await using var simulation = Create(device, installation: installation);
        await simulation.Initialize();
        var construction = BallConstruction();
        var admission = simulation.Construct(construction).AsTask();
        device.CompleteAdmission(0); await admission;
        installation.HoldNext = true;
        var firstRun = simulation.Run().AsTask();
        var reset = simulation.Reset().AsTask();
        device.CompleteAdmission(1);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await reset).Outcome);
        var restored = simulation.Committed;
        installation.Complete(0);
        Assert.Equal(WorkshopCommandOutcome.Superseded, (await firstRun).Outcome);
        Assert.Equal(restored, simulation.Committed);
        Assert.Equal(construction, simulation.Construction);
        Assert.Equal(WorkshopSimulationPhase.Building, simulation.Phase);
    }

    [Fact]
    public async Task OldAdvanceCannotClearNewEpochsInFlightOwner()
    {
        var device = new ControlledDevice();
        await using var simulation = Create(device);
        await simulation.Initialize();
        var run = simulation.Run().AsTask();
        await run;
        var oldAdvance = simulation.Advance().AsTask();
        var reset = simulation.Reset().AsTask();
        device.CompleteAdmission(0);
        await reset;
        device.CompleteAdvance(0);
        Assert.Equal(WorkshopCommandOutcome.Superseded, (await oldAdvance).Outcome);
        run = simulation.Run().AsTask();
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
        await using var simulation = Create(device);
        await simulation.Initialize();
        var run = simulation.Run().AsTask();
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
        await using var simulation = Create(device);
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
    public async Task DisposeInvalidatesPendingInstallationAndOwnsDeviceOnce()
    {
        var device = new ControlledDevice();
        var installation = new ControlledInstallation();
        var simulation = Create(device, installation: installation);
        await simulation.Initialize();
        installation.HoldNext = true;
        var pending = simulation.Run().AsTask();
        await simulation.DisposeAsync();
        installation.Complete(0);
        Assert.Equal(WorkshopCommandOutcome.Superseded, (await pending).Outcome);
        await simulation.DisposeAsync();
        Assert.Equal(1, device.Disposals);
        Assert.Equal(1, installation.Retirements);
        Assert.Equal(WorkshopSimulationPhase.Disposed, simulation.Phase);
    }

    [Fact]
    public async Task StartupRejectsRunUntilGpuReadyAndRunPreservesGeneration()
    {
        var device = new ControlledDevice { HoldStartup = true };
        await using var simulation = Create(device);
        var startup = simulation.Initialize().AsTask();
        Assert.Equal(WorkshopSimulationPhase.Initializing, simulation.Phase);
        Assert.Equal(WorkshopRejection.WrongPhase, (await simulation.Run()).Reason);
        device.CompleteAdmission(0);
        await startup;
        var generation = simulation.Epoch;
        var run = simulation.Run().AsTask();
        await run;
        Assert.Equal(generation, simulation.Epoch);
    }

    [Fact]
    public async Task HeldResetRejectsRepeatedRequestsAndRestoresBitsAfterChangedPose()
    {
        var device = new ControlledDevice();
        await using var simulation = Create(device);
        await simulation.Initialize();
        var construction = BallConstruction();
        var admission = simulation.Construct(construction).AsTask();
        device.CompleteAdmission(0); await admission;
        var run = simulation.Run().AsTask();
        await run;
        var initialGeneration = simulation.Epoch;
        var advance = simulation.Advance().AsTask();
        device.CompleteAdvance(0, read => read with { Ball = read.Ball!.Value with
            { Cell = new(0, 48, 0), Local = new((Half)0, (Half)0.25, (Half)0), Velocity = new((Half)0, (Half)(-0.1), (Half)0) } });
        await advance;
        Assert.NotEqual(construction.Ball!.Value.Cell, simulation.Committed.Ball!.Value.Cell);
        var reset = simulation.Reset().AsTask();
        for (var i = 0; i < 8; i++) Assert.Equal(WorkshopRejection.Busy, (await simulation.Reset()).Reason);
        Assert.Equal(2, device.Admissions.Count);
        device.CompleteAdmission(1); await reset;
        Assert.Equal(initialGeneration.Value + 1, simulation.Epoch.Value);
        Assert.Equal(ControlledDevice.Initial(construction, simulation.Epoch).Ball!.Value.Encode(),
            simulation.Committed.Ball!.Value.Encode());
        Assert.Equal(construction, simulation.Construction);
    }

    [Fact]
    public async Task ChangedSignedZeroInAdmissionCannotCommit()
    {
        var device = new ControlledDevice();
        await using var simulation = Create(device);
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
        await using var simulation = Create(device);
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

    public enum AdvancedIdentityChange { None, Body, World, Tick }

    [Theory]
    [InlineData(AdvancedIdentityChange.None)]
    [InlineData(AdvancedIdentityChange.Body)]
    [InlineData(AdvancedIdentityChange.World)]
    [InlineData(AdvancedIdentityChange.Tick)]
    public async Task GenericAdvanceAcceptsLateralMotionButRejectsChangedIdentity(AdvancedIdentityChange change)
    {
        var device = new ControlledDevice();
        await using var simulation = Create(device);
        await simulation.Initialize();
        var admit = simulation.Construct(BallConstruction()).AsTask();
        device.CompleteAdmission(0); await admit;
        await simulation.Run();
        var before = simulation.Committed;
        var advance = simulation.Advance().AsTask();
        device.CompleteAdvance(0, read =>
        {
            var body = read.Ball!.Value;
            body = body with { Local = new((Half).25, body.Local.Y, (Half)(-.25)),
                Velocity = new((Half).03125, (Half)0, (Half)(-.03125)) };
            return change switch
            {
                AdvancedIdentityChange.None => read with { Ball = body },
                AdvancedIdentityChange.Body => read with { Ball = body with { Id = new(body.Id.Value + 1) } },
                AdvancedIdentityChange.World => read with { Epoch = new(read.Epoch.Value + 1),
                    Ball = body with { Epoch = read.Epoch.Value + 1 } },
                AdvancedIdentityChange.Tick => read with { Tick = new(read.Tick.Value + 1),
                    Ball = body with { Tick = read.Tick.Value + 1 } },
                _ => throw new ArgumentOutOfRangeException(nameof(change))
            };
        });
        Assert.Equal(change == AdvancedIdentityChange.None ? WorkshopCommandOutcome.Applied : WorkshopCommandOutcome.Faulted,
            (await advance).Outcome);
        if (change != AdvancedIdentityChange.None) Assert.Equal(before, simulation.Committed);
        else
        {
            Assert.Equal((Half).25, simulation.Committed.Ball!.Value.Local.X);
            Assert.Equal((Half)(-.25), simulation.Committed.Ball.Value.Local.Z);
            Assert.Equal((Half).03125, simulation.Committed.Ball.Value.Velocity.X);
            Assert.Equal((Half)(-.03125), simulation.Committed.Ball.Value.Velocity.Z);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancelledResetKeepsPriorModeAndOnlyCancelAdvancesAuthority(bool completed)
    {
        var device = new ControlledDevice();
        await using var simulation = Create(device);
        await simulation.Initialize();
        Assert.Equal(new AuthorityRevision(0), simulation.Revision);
        var run = simulation.Run().AsTask();
        await run;
        Assert.Equal(new AuthorityRevision(1), simulation.Revision);
        if (completed)
        {
            for (var i = 0; i < 3600; i++)
            {
                var tick = simulation.Advance().AsTask();
                device.CompleteAdvance(i);
                Assert.Equal(WorkshopCommandOutcome.Applied, (await tick).Outcome);
            }
            await simulation.CompleteRun();
        }
        var before = simulation.Committed;
        var phase = simulation.Phase;
        var epoch = simulation.Epoch;
        var reset = simulation.Reset().AsTask();
        Assert.Equal(WorkshopCommandOutcome.Applied, simulation.CancelPending().Outcome);
        Assert.Equal(phase, simulation.Phase);
        Assert.Equal(before with { Revision = new(before.Revision.Value + 1) }, simulation.Committed);
        device.CompleteAdmission(0);
        Assert.Equal(WorkshopCommandOutcome.Cancelled, (await reset).Outcome);
        Assert.Equal(epoch, simulation.Epoch);
        Assert.Equal(before.Revision.Value + 1, simulation.Revision.Value);
        var appliedReset = simulation.Reset().AsTask();
        device.CompleteAdmission(1);
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
        await using var simulation = Create(device);
        await simulation.Initialize();
        // Native negative boundary injection into the actual owner; no production setter is exposed.
        var exhausted = simulation.Committed with { Revision = new(ulong.MaxValue) };
        typeof(WorkshopSimulation).GetProperty(nameof(WorkshopSimulation.Committed))!.SetValue(simulation, exhausted);
        Assert.Equal(WorkshopRejection.IdentityExhausted, (await simulation.Run()).Reason);
        Assert.Equal(exhausted, simulation.Committed);
        Assert.Empty(device.Admissions);
        Assert.Equal(WorkshopSimulationPhase.Faulted, simulation.Phase);
    }

    private sealed class ControlledClock : IWorkshopCaptureClock
    {
        public ClockGeneration Generation => new(1);
        public long Now { get; set; } = 100_000_000;
        public bool Fail { get; set; }
        public WorkshopClockStamp Capture()
        {
            if (Fail) throw new ArgumentException("Injected native capture rejection.");
            var time = Now; Now += 10_000_000;
            return new(WorkshopClockDomain.SimulationMonotonic, Generation, new(time), new(100_000));
        }
    }

    [Fact]
    public async Task ClockFailureCannotSwapGpuOrRestampLastCommit()
    {
        var device = new ControlledDevice();
        var clock = new ControlledClock();
        await using var simulation = Create(device, clock);
        await simulation.Initialize();
        await simulation.Run();
        var original = simulation.Committed;
        var commits = device.Commits;
        clock.Fail = true;
        var advance = simulation.Advance().AsTask();
        device.CompleteAdvance(0);
        Assert.Equal(WorkshopCommandOutcome.Faulted, (await advance).Outcome);
        Assert.Equal(original, simulation.Committed);
        Assert.Equal(commits, device.Commits);
    }

    [Fact]
    public async Task NativeCaptureOccursOnPhysicalCommitAndNeverOnProjectionOrCancellation()
    {
        var device = new ControlledDevice();
        var clock = new ControlledClock();
        await using var simulation = Create(device, clock);
        await simulation.Initialize();
        var initial = simulation.Committed.Capture;
        await simulation.Run();
        Assert.Equal(initial, simulation.Committed.Capture);
        var advance = simulation.Advance().AsTask();
        device.CompleteAdvance(0); await advance;
        Assert.NotEqual(initial, simulation.Committed.Capture);
        var committed = simulation.Committed.Capture;
        await simulation.Pause(); await simulation.Resume();
        Assert.Equal(committed, simulation.Committed.Capture);
        var reset = simulation.Reset().AsTask();
        simulation.CancelPending();
        Assert.Equal(committed, simulation.Committed.Capture);
        device.CompleteAdmission(0); await reset;
        simulation.Stop(WorkshopRejection.Capacity);
        Assert.Equal(committed, simulation.Committed.Capture);
    }
    [Fact]
    public async Task OldTransportFailureCannotAbortNewerResetInstallation()
    {
        var device = new ControlledDevice();
        var installation = new ControlledInstallation();
        await using var simulation = Create(device, installation: installation);
        await simulation.Initialize(); await simulation.Run();
        var old = simulation.Advance().AsTask();
        installation.HoldNext = true;
        var reset = simulation.Reset().AsTask();
        device.CompleteAdmission(0);
        var resetOwner = installation.Active;
        Assert.NotNull(resetOwner);
        device.FailAdvance(0);
        Assert.Equal(WorkshopCommandOutcome.Superseded, (await old).Outcome);
        Assert.Equal(resetOwner, installation.Active);
        Assert.DoesNotContain(resetOwner.Value, installation.Aborted);
        installation.Complete(0);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await reset).Outcome);
        Assert.Equal(WorkshopSimulationPhase.Building, simulation.Phase);
    }

    [Fact]
    public async Task DisposedConfigureCannotPrepareOrInstallALateGpuCandidate()
    {
        var device = new ControlledDevice();
        var installation = new ControlledInstallation();
        var simulation = Create(device, installation: installation);
        await simulation.Initialize();
        var initial = simulation.Profile;
        var pending = simulation.Configure(Settings with { Simulation = SimulationCadence.Hz240 }, new(2)).AsTask();
        await simulation.DisposeAsync();
        device.CompleteAdmission(0);
        Assert.Equal(WorkshopCommandOutcome.Superseded, (await pending).Outcome);
        Assert.Equal(1, installation.Calls);
        Assert.Equal(initial, simulation.Profile);
        Assert.Equal(WorkshopSimulationPhase.Disposed, simulation.Phase);
    }

    [Theory]
    [InlineData(SimulationCadence.Hz60, 1800UL)]
    [InlineData(SimulationCadence.Hz120, 3600UL)]
    [InlineData(SimulationCadence.Hz240, 7200UL)]
    public async Task ConfiguredProfileCommitsAtomicallyThenPausesStepsAndCompletesAtItsOwnLimit(
        SimulationCadence cadence, ulong limit)
    {
        var device = new ControlledDevice();
        var installation = new ControlledInstallation();
        await using var simulation = Create(device, installation: installation);
        await simulation.Initialize();
        var before = simulation.Profile;
        var settings = Settings with { Simulation = cadence };
        installation.HoldNext = true;
        var configure = simulation.Configure(settings, new(3)).AsTask();
        device.CompleteAdmission(0);
        Assert.Equal(before, simulation.Profile);
        Assert.Equal(Settings, simulation.Construction.Settings);
        installation.Complete(0);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await configure).Outcome);
        Assert.Equal(cadence, simulation.Profile.Cadence);
        Assert.Equal(settings, simulation.Construction.Settings);
        Assert.Equal(WorkshopRejection.StaleGeneration, (await simulation.Configure(settings, new(2))).Reason);
        await simulation.Run();
        await simulation.Pause();
        var capture = simulation.Committed.Capture;
        Assert.Equal(WorkshopRejection.WrongPhase, (await simulation.Advance()).Reason);
        var step = simulation.Step().AsTask();
        device.CompleteAdvance(0);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await step).Outcome);
        Assert.Equal(new SimulationTick(1), simulation.Committed.Tick);
        Assert.Equal(WorkshopSimulationPhase.Paused, simulation.Phase);
        Assert.NotEqual(capture, simulation.Committed.Capture);
        await simulation.Resume();
        for (var tick = 1UL; tick < limit; tick++)
        {
            var advance = simulation.Advance().AsTask();
            device.CompleteAdvance(checked((int)tick));
            Assert.Equal(WorkshopCommandOutcome.Applied, (await advance).Outcome);
        }
        Assert.Equal(WorkshopRejection.WrongPhase, (await simulation.Advance()).Reason);
        await simulation.CompleteRun();
        Assert.Equal(WorkshopSimulationPhase.Completed, simulation.Phase);
        Assert.Equal(new SimulationTick(limit), simulation.Committed.Tick);
    }

    private sealed class ControlledInstallation : IWorkshopInstallation
    {
        public bool HoldNext { get; set; }
        public int Retirements { get; private set; }
        public int Calls { get; private set; }
        public CommandSequence? Active { get; private set; }
        public List<(CommandSequence Owner, TaskCompletionSource Completion)> Pending { get; } = [];
        public List<CommandSequence> Aborted { get; } = [];
        public ValueTask Prepare(CommandSequence owner, WorkshopRead read, WorkshopConstruction construction,
            WorkshopGpuProfile profile, WorkshopSimulationPhase phase)
        {
            Calls++;
            if (!HoldNext) return ValueTask.CompletedTask;
            HoldNext = false; Active = owner;
            var completion = new TaskCompletionSource();
            Pending.Add((owner, completion));
            return new(completion.Task);
        }
        public void Complete(int index) => Pending[index].Completion.SetResult();
        public void Abort(CommandSequence owner) { Aborted.Add(owner); if (Active == owner) Active = null; }
        public void Retire() { Retirements++; Active = null; }
    }


    private sealed class ControlledDevice : IWorkshopGpuDevice
    {
        public readonly List<Admission> Admissions = [];
        private readonly List<(WorkshopRead Source, CommandSequence Sequence, TaskCompletionSource<WorkshopGpuCandidate> Completion)> _advances = [];
        public int Disposals { get; private set; }
        public int Commits { get; private set; }
        private bool _initializing = true;
        public bool HoldStartup { get; init; }
        public ValueTask<WorkshopGpuCandidate> Admit(WorkshopConstruction construction, SimulationEpoch epoch, WorkshopGpuProfile profile, CommandSequence sequence)
        {
            profile.Validate();
            Assert.Equal(construction.Settings.Simulation, profile.Cadence);
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
                ? new CanonicalBody(ball.Id, epoch.Value, 0, ball.Cell, ball.Local, default) : null,
                Rotation: construction.Ball?.Rotation);
        public void FailAdvance(int index) => _advances[index].Completion.SetException(new InvalidOperationException("Injected old transport failure."));
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
        public void Commit(CommandSequence sequence) { Commits++; }
        public void Discard(CommandSequence sequence) { }
        public ValueTask DisposeAsync() { Disposals++; return ValueTask.CompletedTask; }
        public sealed record Admission(WorkshopConstruction Construction, SimulationEpoch Epoch, CommandSequence Sequence,
            TaskCompletionSource<WorkshopGpuCandidate> Completion);
    }
}
