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
        new(new(revision), Settings, new(WorkshopInput.Basketball(new(0xfffffffe), 0, 4, 0, 0, 0, 0, 1)));

    [Fact]
    public async Task RoundedGpuRotationCommitsThroughTheSimulationReadBoundary()
    {
        var device = new ControlledDevice();
        await using var simulation = Create(device);
        await simulation.Initialize();
        var admission = simulation.Construct(BallConstruction()).AsTask();
        device.CompleteAdmission(0); await admission;
        await simulation.Run();
        var advance = simulation.Advance().AsTask();
        // Actual First principles tick86 output: normalised f16, norm squared1.0012203.
        var rotation = new CanonicalRotation((Half)0, (Half)0, (Half)(-.301513671875), (Half).9541015625);
        device.CompleteAdvance(0, read => ChangeOnlyBody(read, body => body with { Rotation = rotation }));
        Assert.Equal(WorkshopCommandOutcome.Applied, (await advance).Outcome);
        Assert.Equal(WorkshopSimulationPhase.Running, simulation.Phase);
        Assert.Equal(rotation, simulation.Committed.Bodies[0].Rotation);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(16)]
    public async Task CompletePopulationSharesTimeAndResetRestoresEveryBody(int count)
    {
        var device = new ControlledDevice();
        await using var simulation = Create(device);
        await simulation.Initialize();
        var construction = PopulationConstruction(count);
        var admission = simulation.Construct(construction).AsTask();
        device.CompleteAdmission(0); await admission;
        var initial = simulation.Committed;
        Assert.Equal(count, initial.Bodies.Count);
        await simulation.Run();
        var advance = simulation.Advance().AsTask(); device.CompleteAdvance(0);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await advance).Outcome);
        Assert.Equal(1ul, simulation.Committed.Tick.Value);
        for (var i = 0; i < count; i++)
        {
            Assert.Equal(initial.Bodies[i].Body.Id, simulation.Committed.Bodies[i].Body.Id);
            Assert.Equal(simulation.Committed.Epoch.Value, simulation.Committed.Bodies[i].Body.Epoch);
            Assert.Equal(1ul, simulation.Committed.Bodies[i].Body.Tick);
        }
        var reset = simulation.Reset().AsTask(); device.CompleteAdmission(1); await reset;
        Assert.Equal(0ul, simulation.Committed.Tick.Value);
        Assert.Equal(count, simulation.Committed.Bodies.Count);
        for (var i = 0; i < count; i++)
        {
            var expected = initial.Bodies[i] with
            { Body = initial.Bodies[i].Body with { Epoch = simulation.Committed.Epoch.Value } };
            Assert.True(expected.HasSameBits(simulation.Committed.Bodies[i]));
        }
        Assert.Equal(construction, simulation.Construction);
    }

    public enum PopulationChange { MissingLast, ForeignLast }

    [Theory]
    [InlineData(PopulationChange.MissingLast)]
    [InlineData(PopulationChange.ForeignLast)]
    public async Task InvalidNonfirstMemberCannotReplaceCompleteCommittedPopulation(PopulationChange change)
    {
        var device = new ControlledDevice();
        await using var simulation = Create(device);
        await simulation.Initialize();
        var admission = simulation.Construct(PopulationConstruction(16)).AsTask();
        device.CompleteAdmission(0); await admission;
        await simulation.Run();
        var before = simulation.Committed;
        var advance = simulation.Advance().AsTask();
        device.CompleteAdvance(0, read =>
        {
            var values = Enumerable.Range(0, read.Bodies.Count).Select(i => read.Bodies[i]).ToArray();
            if (change == PopulationChange.MissingLast) Array.Resize(ref values, 15);
            else values[15] = values[15] with { Body = values[15].Body with { Id = new(999) } };
            return read with { Bodies = new(values) };
        });
        Assert.Equal(WorkshopCommandOutcome.Faulted, (await advance).Outcome);
        Assert.Equal(before, simulation.Committed);
        Assert.Equal(16, simulation.Committed.Bodies.Count);
    }

    private static WorkshopConstruction PopulationConstruction(int count) =>
        new(new(2), Settings, new(Enumerable.Range(0, count).Select(i =>
            (IWorkshopInstance)WorkshopInput.Basketball(new((ulong)i + 1),
                (i % 4) * 2, 4, (i / 4) * 2, 0, 0, 0, 1)).ToArray()));

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
        request.Completion.SetResult(new(request.Sequence, ChangeOnlyBody(correct, value => value with { Body = value.Body with { Id = new(7) } })));
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
        device.CompleteAdvance(0, read => ChangeOnlyBody(read, value => value with { Body = value.Body with
            { Cell = new(0, 48, 0), Local = new((Half)0, (Half)0.25, (Half)0), Velocity = new(0f, -3.2f, 0f) } }));
        await advance;
        Assert.NotEqual(construction.Instances.ToArray().OfType<WorkshopBall>().Single().Cell, simulation.Committed.Bodies[0].Body.Cell);
        var reset = simulation.Reset().AsTask();
        for (var i = 0; i < 8; i++) Assert.Equal(WorkshopRejection.Busy, (await simulation.Reset()).Reason);
        Assert.Equal(2, device.Admissions.Count);
        device.CompleteAdmission(1); await reset;
        Assert.Equal(initialGeneration.Value + 1, simulation.Epoch.Value);
        Assert.Equal(ControlledDevice.Initial(construction, simulation.Epoch).Bodies[0].Body.Encode(),
            simulation.Committed.Bodies[0].Body.Encode());
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
        var ball = candidate.Bodies[0].Body;
        request.Completion.SetResult(new(request.Sequence, ChangeOnlyBody(candidate, value => value with
            { Body = ball with { Local = ball.Local with { X = BitConverter.UInt16BitsToHalf(0x8000) } } })));
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
        Assert.Equal(0, simulation.Committed.Bodies.Count);
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
            var body = read.Bodies[0].Body;
            body = body with { Local = new((Half).25, body.Local.Y, (Half)(-.25)),
                Velocity = new(1f, 0f, -1f) };
            return change switch
            {
                AdvancedIdentityChange.None => ChangeOnlyBody(read, value => value with { Body = body }),
                AdvancedIdentityChange.Body => ChangeOnlyBody(read, value => value with { Body = body with { Id = new(body.Id.Value + 1) } }),
                AdvancedIdentityChange.World => ChangeOnlyBody(read with { Epoch = new(read.Epoch.Value + 1) },
                    value => value with { Body = body with { Epoch = read.Epoch.Value + 1 } }),
                AdvancedIdentityChange.Tick => ChangeOnlyBody(read with { Tick = new(read.Tick.Value + 1) },
                    value => value with { Body = body with { Tick = read.Tick.Value + 1 } }),
                _ => throw new ArgumentOutOfRangeException(nameof(change))
            };
        });
        Assert.Equal(change == AdvancedIdentityChange.None ? WorkshopCommandOutcome.Applied : WorkshopCommandOutcome.Faulted,
            (await advance).Outcome);
        if (change != AdvancedIdentityChange.None) Assert.Equal(before, simulation.Committed);
        else
        {
            Assert.Equal((Half).25, simulation.Committed.Bodies[0].Body.Local.X);
            Assert.Equal((Half)(-.25), simulation.Committed.Bodies[0].Body.Local.Z);
            Assert.Equal(1f, simulation.Committed.Bodies[0].Body.Velocity.X);
            Assert.Equal(-1f, simulation.Committed.Bodies[0].Body.Velocity.Z);
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

    [Fact]
    public async Task TimerBearingStepInstallationRejectsAtomicallyAndResetCancelsCountdown()
    {
        var device = new ControlledDevice(); var installation = new ControlledInstallation();
        await using var simulation = Create(device, installation: installation);
        await simulation.Initialize();
        var construction = new WorkshopConstruction(new(2), Settings, new(
            WorkshopInput.Basketball(new(1),0,3,0,0,0,0,1),
            WorkshopInput.Switch(new(2),0,1,0,0,0,0,1,ContactTriggerSettings.Default),
            WorkshopInput.Delay(new(3),-3,1,0,0,0,0,1,DelayDuration.Default)),
            Connections:new(new WorkshopConnection(new(2),WorkshopSocket.ActivationOut,new(3),WorkshopSocket.ActivationIn,WorkshopConnectionDomain.Activation)));
        var admission = simulation.Construct(construction).AsTask(); device.CompleteAdmission(0); await admission;
        await simulation.Run(); await simulation.Pause();
        var before = simulation.Committed; var commits = device.Commits;
        var network = WorkshopActivationCompiler.Compile(construction);
        var scene = WorkshopPhysicsCompiler.Compile(construction,new(1,2));
        var trigger = scene.Triggers[0];
        var collider = scene.Colliders.ToArray().First(value=>value.Body==trigger.Owner).Id;
        var occurrence = new ContactTriggerRead(trigger.Id,trigger.Owner,new GpuBodyId(1),1,collider,1,(Half)0,new((Half)1));
        WorkshopRead Counting(WorkshopRead read)
        {
            var checkpoint = network.Consume(read.Activations,read.Timers,new[] {occurrence},read.Tick,[]);
            network.ValidateRead(checkpoint.Activations,checkpoint.Timers,read.Tick,4,scene);
            return read with { Activations=checkpoint.Activations,Timers=checkpoint.Timers };
        }
        installation.HoldNext = true;
        var rejected = simulation.Step().AsTask(); device.CompleteAdvance(0,Counting);
        Assert.Equal(before,simulation.Committed); Assert.Equal(commits,device.Commits);
        installation.Pending[0].Completion.SetException(new WorkshopInstallationException());
        Assert.Equal(WorkshopRejection.Transport,(await rejected).Reason);
        Assert.Equal(before,simulation.Committed); Assert.Equal(commits,device.Commits);
        var accepted = simulation.Step().AsTask(); device.CompleteAdvance(1,Counting); await accepted;
        Assert.Equal(ActivationTimerPhase.Counting,simulation.Committed.Timers[0].Phase);
        var counting = simulation.Committed;
        var pending = simulation.Step().AsTask();
        var reset = simulation.Reset().AsTask(); device.CompleteAdmission(1); await reset;
        var restored = simulation.Committed;
        Assert.Equal(ActivationTimerPhase.Ready,restored.Timers[0].Phase);
        Assert.Equal(0ul,restored.Tick.Value); Assert.NotEqual(counting.Epoch,restored.Epoch);
        device.CompleteAdvance(2); Assert.Equal(WorkshopCommandOutcome.Superseded,(await pending).Outcome);
        Assert.Equal(restored,simulation.Committed);
    }

    [Fact]
    public async Task PaidWorkStepInstallationRejectsAtomicallyAndResetRestoresPreload()
    {
        var device = new ControlledDevice(); var installation = new ControlledInstallation();
        await using var simulation = Create(device, installation: installation);
        await simulation.Initialize();
        var construction = new WorkshopConstruction(new(2), Settings, new(
            WorkshopInput.Basketball(new(1),0,6,0,0,0,0,1),
            WorkshopInput.Bumper(new(2),0,4,0,0,0,0,1,BumperWork.FromCanonicalStrength((Half)8))));
        var admission = simulation.Construct(construction).AsTask(); device.CompleteAdmission(0); await admission;
        await simulation.Run(); await simulation.Pause();
        var before = simulation.Committed; var commits = device.Commits;
        var scene = WorkshopPhysicsCompiler.Compile(construction,new(1,2));
        var collider = scene.Colliders.ToArray().First(value=>value.Body==new GpuBodyId(2)).Id;
        WorkshopRead Paid(WorkshopRead read) => read with {
            ContactWorks = new(new[] { read.ContactWorks[0] with {
                OccurrenceCount=1, RemainingEnergy=new((Half)16) } },
                new[] { new ContactWorkOccurrence(new(0),
                    new(checked((ushort)Array.FindIndex(scene.Colliders.ToArray(), value => value.Id == collider))),
                    new(1), 1, 1, (Half)0, new((Half)4), new((Half)16), ContactWorkEffect.Paid) }) };
        installation.HoldNext = true;
        var rejected = simulation.Step().AsTask(); device.CompleteAdvance(0,Paid);
        Assert.Equal(before,simulation.Committed); Assert.Equal(commits,device.Commits);
        installation.Pending[0].Completion.SetException(new WorkshopInstallationException());
        Assert.Equal(WorkshopRejection.Transport,(await rejected).Reason);
        Assert.Equal(before,simulation.Committed); Assert.Equal(commits,device.Commits);
        var accepted = simulation.Step().AsTask(); device.CompleteAdvance(1,Paid); await accepted;
        Assert.Equal(1u,simulation.Committed.ContactWorks[0].OccurrenceCount);
        Assert.Equal((Half)16,simulation.Committed.ContactWorks[0].RemainingEnergy.Value);
        var spent = simulation.Committed;
        var pending = simulation.Step().AsTask();
        var reset = simulation.Reset().AsTask(); device.CompleteAdmission(1); await reset;
        var restored = simulation.Committed;
        Assert.Equal(0u,restored.ContactWorks[0].OccurrenceCount);
        Assert.Equal((Half)32,restored.ContactWorks[0].RemainingEnergy.Value);
        Assert.Equal((Half)0,restored.ContactWorks.Occurrence(0).Debit.Value);
        Assert.Equal(0ul,restored.Tick.Value); Assert.NotEqual(spent.Epoch,restored.Epoch);
        device.CompleteAdvance(2); Assert.Equal(WorkshopCommandOutcome.Superseded,(await pending).Outcome);
        Assert.Equal(restored,simulation.Committed);
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


    private static WorkshopRead ChangeOnlyBody(WorkshopRead read, Func<PhysicsBodyRead, PhysicsBodyRead> change)
    {
        Assert.Equal(1, read.Bodies.Count);
        return read with { Bodies = new(new[] { change(read.Bodies[0]) }) };
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
        public static WorkshopRead Initial(WorkshopConstruction construction, SimulationEpoch epoch)
        {
            var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
            var bytes = PhysicsGpuAbi.Admission(scene, epoch,
                new(construction.Settings.Simulation, PhysicalStepProfile.Canonical480Hz, new(1)));
            var network = WorkshopActivationCompiler.Compile(construction);
            return new(epoch, new(0), PhysicsGpuAbi.ReadDynamicBodies(bytes),
                Activations: network.Clear(), Timers: network.ClearTimers(),
                ContactWorks: PhysicsGpuAbi.ReadContactWorks(bytes));
        }
        public void FailAdvance(int index) => _advances[index].Completion.SetException(new InvalidOperationException("Injected old transport failure."));
        public void CompleteAdvance(int index, Func<WorkshopRead, WorkshopRead>? transform = null)
        {
            var (source, sequence, completion) = _advances[index];
            var tick = source.Tick.Value + 1;
            var read = source with
            {
                Revision = default,
                Tick = new(tick),
                Bodies = new(Enumerable.Range(0, source.Bodies.Count).Select(i =>
                    source.Bodies[i] with { Body = source.Bodies[i].Body with { Tick = tick } }).ToArray())
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
