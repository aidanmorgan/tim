using System.Buffers.Binary;
using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;
using ChannelControl = CuriousContraptions.Gpu.WorkshopAnimationControl;

namespace CuriousContraptions.Tests;

/// <summary>The main thread's animation channel. Story 9A.23/9A.25b: it relays no one-shot cue (every one is a declared binding the
/// Animation worker drives from the event ring), so its controls are the UI clips, the goal cue and declared visibility.</summary>
public sealed class WorkshopActivationAnimationTests
{
    private static readonly RuntimeSessionId Session = new(101, 206);
    private static ChannelControl Control => new(new(4), new(1), 1, 1, AnimationControlKind.Endpoint,
        true, (float)1, (float)1, (float)1, AnimationCurve.Linear, 10, (float)0, AnimationProperty.ColourBlend);

    // The retired relay kinds (Impulse 6, Transition 7) and the impulse envelope words (bytes 92..95) are refused, never read.
    [Theory]
    [InlineData(48, 6u)]
    [InlineData(48, 7u)]
    [InlineData(92, 2u)]
    [InlineData(94, 1u)]
    public void TheRetiredOneShotRelayControlsAreRefused(int offset, uint value)
    {
        var bytes = WorkshopAnimationWire.Control(Session, new(1), new(1), Control);
        Assert.Equal(Control, WorkshopAnimationWire.ReadControl(bytes, Session, new(1), new(1)));
        var changed = (byte[])bytes.Clone();
        if (offset == 48) BinaryPrimitives.WriteUInt32LittleEndian(changed.AsSpan(offset), value);
        else BinaryPrimitives.WriteUInt16LittleEndian(changed.AsSpan(offset), (ushort)value);
        Assert.Throws<ArgumentException>(() => WorkshopAnimationWire.ReadControl(changed, Session, new(1), new(1)));
        Assert.False(Enum.IsDefined((AnimationControlKind)6));
        Assert.False(Enum.IsDefined((AnimationControlKind)7));
    }

    [Fact]
    public void TypedAnimationChannelRejectsRetiredSchemaUnknownPropertyAndForeignIdentity()
    {
        var bytes = WorkshopAnimationWire.Control(Session, new(1), new(1), Control);
        Assert.Equal(Control, WorkshopAnimationWire.ReadControl(bytes, Session, new(1), new(1)));
        // Story 9A.1 (F.10): bytes 142..143 are zero padding where the version was; the retired version 5 now rejects.
        Assert.Equal(0, BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(142)));
        foreach (var (offset, value) in new[] { (142, 5), (142, 1), (140, 0), (140, 99), (92, 1) })
        {
            var changed = (byte[])bytes.Clone(); BinaryPrimitives.WriteUInt16LittleEndian(changed.AsSpan(offset), (ushort)value);
            Assert.Throws<ArgumentException>(() => WorkshopAnimationWire.ReadControl(changed, Session, new(1), new(1)));
        }
        Assert.Throws<ArgumentException>(() => WorkshopAnimationWire.ReadControl(bytes, new(1, 2), new(1), new(1)));
        var output = Output(Control, new(1), AnimationOutputKind.Acknowledgement, 1);
        Assert.Equal(AnimationProperty.ColourBlend, WorkshopAnimationWire.Read(output, Session, new(1), new(1), out _).Property);
        output[88] = 5; // the retired output version, where zero padding now stands
        Assert.Throws<ArgumentException>(() => WorkshopAnimationWire.Read(output, Session, new(1), new(1), out _));
    }

    [Fact]
    public void SharedBindingEvaluationPreservesCanonicalNeutralActiveAndIntermediateValues()
    {
        var neutral = new AnimationValue(new AnimationMetres((float).06));
        var active = new AnimationValue(new AnimationMetres((float)(-.02)));
        Assert.Equal(neutral, AnimationValue.Blend(neutral, active, new((float)0)));
        Assert.Equal(active, AnimationValue.Blend(neutral, active, new((float)1)));
        Assert.Equal((float)(((double).06f + (double)(-.02f)) * .5),
            AnimationValue.Blend(neutral, active, new((float).5)).Metres.Value);
        Assert.Throws<ArgumentException>(() => AnimationValue.Blend(neutral, new(new AnimationOpacity((float)1)), new((float)1)));
    }

    // No relay owns a target outside the UI and declared ranges: its output is refused once the lease it answers is released.
    [Fact]
    public void AnOutputForATargetNoChannelOwnsIsRefused()
    {
        using var session = new NativeClient();
        Assert.Throws<ArgumentException>(() => session.Output(Control, AnimationOutputKind.Acknowledgement));
        Assert.Throws<ArgumentException>(() => session.Output(Control, AnimationOutputKind.Sample, 2));
    }

    [Fact]
    public async Task DeclaredCadenceBoundaryRestartsOrdinalsAndRejectsReplay()
    {
        using var session = new NativeClient();
        await session.Construct(new(new(1), WorkshopCadenceSettings.Default(), new(PlacedParts.Battery(new(1), 0, 1, 0))));
        var control = session.Declared(new(1), 0);
        var oldCadence = session.Schedule.Revision;
        session.Output(control, AnimationOutputKind.Sample, 100);
        await session.Client.Execute(WorkshopCommandKind.ConfigureCadence,
            settings: WorkshopCadenceSettings.Default() with { Simulation = SimulationCadence.Hz60 });
        Assert.NotEqual(oldCadence, session.Schedule.Revision);
        var fresh = session.Declared(new(1), 0);
        session.Output(fresh, AnimationOutputKind.Sample, 1);
        Assert.Throws<ArgumentException>(() => session.Output(fresh, AnimationOutputKind.Sample, 1));
        session.Output(control, AnimationOutputKind.Sample, 200, oldCadence);
        session.Frame();
        Assert.True(session.Client.TryDeclaredFrame(session.FrameNumber, default, new(1), out var samples));
        Assert.Equal(fresh.Target, Assert.Single(samples).Target);
        Assert.Throws<ArgumentException>(() => session.Output(fresh, AnimationOutputKind.Sample, 1));
    }


    [Fact]
    public async Task EveryOneShotBindingIsADeclaredTargetOfItsOwner()
    {
        var bumper = WorkshopInput.Generic(PartId.PinballBumper, new(1), 0, 1, 0, 0, 0, 0, 1);
        var receiver = WorkshopInput.Generic(PartId.Receiver, new(2), 3, 1, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(bumper, receiver));
        var targets = WorkshopDeclaredPresentation.Targets(construction);
        Assert.Equal(DeclaredPhysics.Of(PartId.PinballBumper).Presentation.Bindings.Count + DeclaredPhysics.Of(PartId.Receiver).Presentation.Bindings.Count,
            targets.Count);
        Assert.Equal([new GpuBodyId(1)], targets.Take(5).Select(target => target.Owner).Distinct());
        using var session = new NativeClient();
        await session.Construct(construction);
        var halo = session.Declared(receiver.Id, 1);
        session.Output(halo, AnimationOutputKind.Sample);
        session.Frame();
        Assert.False(session.Client.TryDeclaredFrame(session.FrameNumber, default, bumper.Id, out _));
        Assert.True(session.Client.TryDeclaredFrame(session.FrameNumber, default, receiver.Id, out var samples));
        var sample = Assert.Single(samples);
        Assert.Equal((halo.Target, receiver.Id, 1), (sample.Target, sample.Owner, sample.Binding));
        Assert.Equal(halo.To, BitConverter.UInt32BitsToSingle(sample.Value.CanonicalBits));
    }


    [Fact]
    public async Task SameConstructionProjectionReplacementRequeuesHiddenOwnerVisibility()
    {
        using var session = new NativeClient();
        var battery = PlacedParts.Battery(new(1), 0, 1, 0);
        await session.Construct(new(new(1), WorkshopCadenceSettings.Default(), new(battery)));
        session.Client.SetDeclaredVisible(battery.Id, false);
        var hidden = Assert.Single(session.Controls);
        Assert.False(hidden.Visible);
        session.Output(hidden, AnimationOutputKind.Acknowledgement);
        await session.Client.Execute(WorkshopCommandKind.Run);
        session.Frame();
        Assert.Equal(2, session.Controls.Count);
        var replacement = session.Controls[1];
        Assert.Equal(hidden.Target, replacement.Target);
        Assert.False(replacement.Visible);
        Assert.NotEqual(hidden.Generation, replacement.Generation);
        session.Output(replacement, AnimationOutputKind.Acknowledgement);
        session.Output(session.Declared(battery.Id, 0), AnimationOutputKind.Sample);
        Assert.False(session.Client.TryDeclaredFrame(session.FrameNumber, default, battery.Id, out _));
        session.Client.SetDeclaredVisible(battery.Id, true);
        session.Output(session.Controls[^1], AnimationOutputKind.Acknowledgement);
        Assert.True(session.Client.TryDeclaredFrame(session.FrameNumber, default, battery.Id, out _));
    }


    [Fact]
    public async Task WrongPropertyOrOccurrenceCannotReleaseOwnedAnimationLease()
    {
        using var session = new NativeClient();
        var goal = await session.Solve();
        foreach (var invalid in new[] { goal with { Property = AnimationProperty.ColourBlend },
            goal with { EventOrdinal = goal.EventOrdinal + 1 } })
        {
            Assert.Throws<ArgumentException>(() => session.Output(invalid, AnimationOutputKind.Acknowledgement));
            session.Client.ControlUi(WorkshopUiTarget.Hint, AnimationControlKind.Reveal, true);
            session.Frame();
            Assert.Single(session.Controls);
        }
        session.Output(goal, AnimationOutputKind.Acknowledgement);
        session.Frame();
        Assert.Equal(UiCurves.Hint.AnimationTarget, session.Controls[1].Target);
    }

    [Fact]
    public void TimerSegmentsSeedTheirCommittedEndpointAndRejectInconsistentIntervals()
    {
        var counting = AnimationTimerSegment.Create(new(68,188,128,2,AnimationTimerPhase.Counting));
        Assert.Equal((float).5, counting.Endpoint.Value);
        Assert.Equal((float)1, AnimationTimerSegment.Create(new(68,188,200,2,AnimationTimerPhase.Finished)).Endpoint.Value);
        Assert.Equal((float)0, AnimationTimerSegment.Create(new(0,0,5,0,AnimationTimerPhase.Ready)).Endpoint.Value);
        Assert.Throws<ArgumentException>(() => AnimationTimerSegment.Create(new(68,188,187,2,AnimationTimerPhase.Finished)));
        Assert.Throws<ArgumentException>(() => AnimationTimerSegment.Create(new(68,188,188,2,AnimationTimerPhase.Counting)));
        Assert.Throws<ArgumentException>(() => AnimationTimerSegment.Create(default));
    }

    [Fact]
    public async Task DeclaredPropertyRejectsForeignChannelAndResetCannotReviveOldWorld()
    {
        using var session = new NativeClient();
        var timer = WorkshopInput.Generic(PartId.Delay, new(1), 0, 1, 0, 0, 0, 0, 1);
        await session.Construct(new(new(1), WorkshopCadenceSettings.Default(), new(timer)));
        var control = session.Declared(timer.Id, 0);
        var oldCadence = session.Schedule.Revision;
        session.Output(control, AnimationOutputKind.Sample);
        Assert.Throws<ArgumentException>(() => session.Output(control with { Property = AnimationProperty.Opacity, To = .5f }, AnimationOutputKind.Sample, 2));
        await session.Client.Execute(WorkshopCommandKind.Run);
        await session.Client.Execute(WorkshopCommandKind.Reset);
        session.Output(control, AnimationOutputKind.Sample, 2, oldCadence);
        session.Frame();
        Assert.False(session.Client.TryDeclaredFrame(session.FrameNumber, default, timer.Id, out _));
        var fresh = session.Declared(timer.Id, 0);
        Assert.NotEqual(control.World, fresh.World);
        session.Output(fresh, AnimationOutputKind.Sample);
        Assert.True(session.Client.TryDeclaredFrame(session.FrameNumber, default, timer.Id, out var samples));
        Assert.Equal(timer.Id, Assert.Single(samples).Owner);
    }


    [Fact]
    public async Task GoalFeedbackOwnsOccurrenceAcrossProjectionRetryAndReset()
    {
        using var session = new NativeClient();
        var goal = await session.Solve();
        var rejectedCadence = await session.Client.Execute(WorkshopCommandKind.ConfigureCadence,
            settings: WorkshopCadenceSettings.Default() with { Simulation = SimulationCadence.Hz60 });
        Assert.Equal(WorkshopRejection.WrongPhase, rejectedCadence.Response.Result.Reason);
        session.Output(goal, AnimationOutputKind.Rejected);
        session.Frame();
        var retry = session.Controls[^1];
        Assert.Equal((goal.World, goal.EventOrdinal, goal.EventPhase), (retry.World, retry.EventOrdinal, retry.EventPhase));
        Assert.True(retry.Sequence > goal.Sequence);
        await session.Client.Execute(WorkshopCommandKind.Pause);
        session.Output(retry, AnimationOutputKind.Rejected);
        session.Frame();
        var projected = session.Controls[^1];
        Assert.Equal((goal.World, goal.EventOrdinal, goal.EventPhase), (projected.World, projected.EventOrdinal, projected.EventPhase));
        session.Output(projected, AnimationOutputKind.Acknowledgement);
        var count = session.Controls.Count;
        session.Frame();
        Assert.Equal(count, session.Controls.Count);
        // Reset retires goal state; changing cadence is supported only in this Building phase.
        await session.Client.Execute(WorkshopCommandKind.Reset);
        await session.Client.Execute(WorkshopCommandKind.ConfigureCadence,
            settings: WorkshopCadenceSettings.Default() with { Simulation = SimulationCadence.Hz60 });
        session.Output(projected, AnimationOutputKind.Sample, 100);
        session.Frame();
        Assert.Equal(count, session.Controls.Count);
        Assert.False(session.Client.TryUiFrame(session.FrameNumber, default, WorkshopUiTarget.Goal, out _));
        var fresh = await session.Solve(construct: false);
        Assert.NotEqual(goal.World, fresh.World);
        Assert.Equal(UiCurves.Goal.AnimationTarget, fresh.Target);
        session.Output(fresh, AnimationOutputKind.Acknowledgement);
    }

    [Fact]
    public void DeclaredImpulseEnvelopeCombinesOverlapInTheSharedBatchAndReturnsToNeutral()
    {
        var declaration = DeclaredPhysics.Of(PartId.PinballBumper).Presentation.Bindings[0];
        Assert.Equal((CuriousContraptions.Abstractions.PresentationCurve.SineSquaredPulse, CuriousContraptions.Abstractions.PresentationOverlap.SaturatingSum),
            (declaration.Curve, declaration.Overlap));
        var envelope = new AnimationImpulseDefinition(new(declaration.Duration), AnimationImpulseCurve.SineSquaredPulse, AnimationImpulseOverlap.SaturatingSum,
            AnimationImpulseVisibility.AdvanceWhileHidden, AnimationClock.Presentation,
            AnimationImpulseTiming.EventTime, AnimationImpulseDefinition.CanonicalPeak(AnimationImpulseCurve.SineSquaredPulse));
        var batch = new AnimationBatch();
        var handle = batch.RegisterImpulses(new(new(3), AnimationProperty.ColourBlend), envelope,
            new(new AnimationColourBlend((float)0)), new(new AnimationColourBlend((float)1)));
        float Value() => BitConverter.UInt32BitsToSingle(batch.Read(handle).Value.CanonicalBits);
        var period = (double)declaration.Duration;
        batch.EnqueueImpulse(handle, new(1), new((float)1), AnimationTestTime.Seconds(1));
        batch.Advance(1, AnimationTestTime.Seconds(1 + period / 8), AnimationTestTime.Seconds(0));
        var single = (double)Value();
        Assert.InRange(single, .14, .16); // sin²(π/8) of the declared SineSquaredPulse
        batch.EnqueueImpulse(handle, new(2), new((float)1), AnimationTestTime.Seconds(1 + period / 8));
        batch.Advance(2, AnimationTestTime.Seconds(1 + period / 4), AnimationTestTime.Seconds(0));
        Assert.InRange((double)Value(), .5 + single - .01, .5 + single + .01); // saturating sum exceeds either contribution
        batch.Advance(3, AnimationTestTime.Seconds(1 + period / 2), AnimationTestTime.Seconds(0));
        Assert.Equal(1f, Value()); // peak + partner is clamped to the admitted unit range
        batch.Advance(4, AnimationTestTime.Seconds(1 + period * 1.5), AnimationTestTime.Seconds(0));
        Assert.Equal(0f, Value());
        Assert.Equal(0, batch.ReadImpulses(handle).Playing);
        Assert.Throws<ArgumentException>(() => batch.EnqueueImpulse(handle, new(2), new((float)1), AnimationTestTime.Seconds(3)));
    }

    [Fact]
    public void HintControlSampleAppliesOnceOnAnAdmittedFrameAndNeverWhileHidden()
    {
        using var session = new NativeClient();
        var client = session.Client;
        client.ControlUi(WorkshopUiTarget.Hint, AnimationControlKind.Reveal, true);
        var reveal = Assert.Single(session.Controls);
        session.Output(reveal with { To = .25f }, AnimationOutputKind.Acknowledgement);
        Assert.True(client.TryUiFrame(session.FrameNumber, default, WorkshopUiTarget.Hint, out var opacity));
        Assert.Equal(new AnimationOpacity(.25f), opacity);
        Assert.False(client.TryUiFrame(session.FrameNumber, default, WorkshopUiTarget.Hint, out _));
        session.Output(reveal with { To = .5f }, AnimationOutputKind.Sample, 2);
        Assert.False(client.TryUiFrame(session.FrameNumber, default, WorkshopUiTarget.Hint, out _));
        client.ControlUi(WorkshopUiTarget.Hint, AnimationControlKind.Visibility, false);
        var hidden = session.Controls[^1];
        session.Output(hidden with { To = .5f }, AnimationOutputKind.Acknowledgement);
        session.Frame();
        Assert.False(client.TryUiFrame(session.FrameNumber, default, WorkshopUiTarget.Hint, out _));
        client.ControlUi(WorkshopUiTarget.Hint, AnimationControlKind.Visibility, true);
        session.Output(session.Controls[^1] with { To = .5f }, AnimationOutputKind.Acknowledgement);
        Assert.True(client.TryUiFrame(session.FrameNumber, default, WorkshopUiTarget.Hint, out opacity));
        Assert.Equal(new AnimationOpacity(.5f), opacity);
        Assert.False(client.TryUiFrame(session.FrameNumber, default, WorkshopUiTarget.Goal, out _));
    }

    [Fact]
    public async Task QueuedRevealSurvivesAPendingPreparationAndSendsOnceItClears()
    {
        using var session = new NativeClient { HoldPreparation = true };
        var run = session.Client.Execute(WorkshopCommandKind.Run);
        Assert.False(run.IsCompleted);
        session.Client.ControlUi(WorkshopUiTarget.Hint, AnimationControlKind.Reveal, true);
        session.Frame();
        Assert.Empty(session.Controls);
        session.CompletePreparation();
        Assert.True((await run).Applicable);
        session.Frame();
        var reveal = Assert.Single(session.Controls);
        Assert.Equal(AnimationControlKind.Reveal, reveal.Kind);
        session.Output(reveal, AnimationOutputKind.Acknowledgement);
        session.Frame();
        Assert.Single(session.Controls);
    }

    [Fact]
    public void UiControlQueuesBehindTheLeaseAndSendsTheDeclaredHintClipWhenFree()
    {
        using var session = new NativeClient();
        var client = session.Client;
        Assert.Throws<ArgumentException>(() => client.ControlUi(WorkshopUiTarget.Goal, AnimationControlKind.Reveal, true));
        Assert.Throws<ArgumentException>(() => client.ControlUi(WorkshopUiTarget.Hint, AnimationControlKind.Endpoint, true));
        client.ControlUi(WorkshopUiTarget.Hint, AnimationControlKind.Reveal, true);
        var first = Assert.Single(session.Controls);
        client.ControlUi(WorkshopUiTarget.Hint, AnimationControlKind.Visibility, false);
        client.ControlUi(WorkshopUiTarget.Hint, AnimationControlKind.Reveal, true);
        session.Frame();
        Assert.Single(session.Controls);
        session.Output(first, AnimationOutputKind.Acknowledgement);
        session.Frame();
        Assert.Equal(2, session.Controls.Count);
        var hint = UiCurves.Hint;
        var next = session.Controls[1];
        Assert.Equal(hint.AnimationTarget, next.Target);
        Assert.Equal(AnimationControlKind.Reveal, next.Kind);
        Assert.True(next.Visible);
        Assert.Equal(2ul, next.Generation);
        Assert.Equal((0f, 1f, hint.Duration, hint.Curve), (next.From, next.To, next.Duration, next.Curve));
        session.Output(next, AnimationOutputKind.Acknowledgement);
        session.Frame();
        Assert.Equal(2, session.Controls.Count);
    }

    /// <summary>Story 9A.1 (F.10): with no version word, a channel record is admitted by its exact length; a truncated or extended
    /// control or output rejects, and the exact records are the controls.</summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void AnAnimationChannelRecordOfAnyOtherLengthRejects(int change)
    {
        var control = WorkshopAnimationWire.Control(Session, new(1), new(1), Control);
        var output = Output(Control, new(1), AnimationOutputKind.Acknowledgement, 1);
        Assert.Equal(Control, WorkshopAnimationWire.ReadControl(control, Session, new(1), new(1)));
        WorkshopAnimationWire.Read(output, Session, new(1), new(1), out _);
        byte[] Resized(byte[] bytes) => change < 0 ? bytes[..^1] : [.. bytes, 0];
        Assert.Throws<ArgumentException>(() => WorkshopAnimationWire.ReadControl(Resized(control), Session, new(1), new(1)));
        Assert.Throws<ArgumentException>(() => WorkshopAnimationWire.Read(Resized(output), Session, new(1), new(1), out _));
    }

    // The retired pulse-duration word (bytes 132..135) is zero padding: a segment there, a timer on a plain output or other padding rejects.
    [Fact]
    public void AnimationOutputRejectsTheRetiredPulseSegmentAndPadding()
    {
        var output = Output(Control, new(1), AnimationOutputKind.Sample, 1);
        WorkshopAnimationWire.Read(output, Session, new(1), new(1), out _);
        foreach (var mutate in new Action<byte[]>[] {
            bytes => BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(132), (float).32),
            bytes => BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(138), 1),
            bytes => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(140), 1) })
        {
            var changed = (byte[])output.Clone(); mutate(changed);
            Assert.Throws<ArgumentException>(() => WorkshopAnimationWire.Read(changed, Session, new(1), new(1), out _));
        }
    }

    // A deterministic browser/worker boundary around the shared native integration harness.
    // The production client owns startup, clocks, schedule admission, animation leases and disposal.
    internal sealed class NativeClient : IWorkshopBrowserTransport, IDisposable
    {
        private WorkshopIntegrationHarness _world = null!;
        private Action _clockReply = null!, _physicalRead = null!, _scheduleControl = null!, _animationOutput = null!, _service = null!, _observation = null!;
        private Action<string> _retired = null!;
        private byte[] _event = [], _acknowledgement = [];
        private WorkshopSchedule? _installed;
        private Action? _commit;
        private TaskCompletionSource? _commitCompletion;
        private double _milliseconds;
        private readonly WorkshopClockPeer _peer = new(Session, new(1), new(0), new(1), new(100_000), WorkshopRuntimeRole.Browser);
        public BrowserWorkshopClient Client { get; }
        public WorkshopSchedule Schedule => _installed!.Value;
        public List<ChannelControl> Controls { get; } = [];
        public bool HoldPreparation { get; set; }
        private int _eventCursor;
        private readonly Dictionary<int, CuriousContraptions.Abstractions.EventRingConsumer> _observers = new();
        private int _observerId;
        private readonly Dictionary<ulong, byte[]> _acknowledgements = new();
        public bool HoldNextAcknowledgement { get; set; }
        private TaskCompletionSource? _heldAcknowledgement;
        public void ReleaseAcknowledgement() { _heldAcknowledgement!.SetResult(); _heldAcknowledgement = null; }
        public async Task<ChannelControl> Solve(bool construct = true)
        {
            if (construct)
                await Construct(PuzzleCatalogue.CreateSolution(new("first_principles"), new(2),
                    WorkshopCadenceSettings.Default(), new(1), new(1f)));
            Assert.Equal(WorkshopCommandOutcome.Applied, (await Client.Execute(WorkshopCommandKind.Run)).Response.Result.Outcome);
            var before = Controls.Count;
            for (var tick = 0; tick < 1200 && Controls.Count == before; tick++)
            {
                await Advance(1);
                Frame();
            }
            Assert.Equal(before + 1, Controls.Count);
            var goal = Controls[^1];
            Assert.Equal(UiCurves.Goal.AnimationTarget, goal.Target);
            return goal;
        }
        private readonly WorkshopDiagnostics _workerDiagnostics;
        public Action<byte[]> DiagnosticDelivery { get; set; }
        private readonly WorkshopObservationTests.StartupFailure _startupFailure;
        private readonly Action? _beforeReady;
        public NativeClient(WorkshopObservation? observation = null, WorkshopDiagnostics? diagnostics = null, WorkshopObservationTests.StartupFailure failure = WorkshopObservationTests.StartupFailure.None, Action? beforeReady = null)
        {
            _startupFailure = failure; _beforeReady = beforeReady;
            var journal = diagnostics ?? new WorkshopDiagnostics();
            Client = BrowserWorkshopClient.Create(observation ?? new WorkshopObservation(Session, journal), this, journal).GetAwaiter().GetResult();
            DiagnosticDelivery = DeliverObservation;
            _workerDiagnostics = new(record => PublishWorkerDiagnostic(record));
        }
        private void PublishWorkerDiagnostic(WorkshopDiagnostic record) =>
            _workerDiagnostics.Publish(record, Session, WorkshopRuntimeRole.Simulation, DiagnosticDelivery);
        public void Dispose()
        {
            Client.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _world.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        public async Task<WorkshopDelivery> Construct(WorkshopConstruction construction)
        {
            var delivery = await Client.Execute(WorkshopCommandKind.Construct,
                construction with { Revision = new(_world.Construction.Revision.Value + 1) });
            Assert.Equal(WorkshopCommandOutcome.Applied, delivery.Response.Result.Outcome);
            return delivery;
        }
        public ulong FrameNumber { get; private set; } = 1;
        public void Frame()
        {
            _milliseconds += 20; _service();
            Client.TryUiFrame(++FrameNumber, default, WorkshopUiTarget.Goal, out _);
        }
        public async Task Advance(int ticks)
        {
            for (var tick = 0; tick < ticks; tick++)
            {
                await _world.Advance(1);
                _workerDiagnostics.RetainTransfer(_world.Latest);
            }
            _milliseconds = Math.Max(_milliseconds, _world.Time.Nanoseconds / 1_000_000d);
            _service();
            _event = WorkshopWireFlatBuffers.EncodeResponse(_world.Latest);
            _physicalRead();
        }
        public ChannelControl Declared(GpuBodyId owner, int binding)
        {
            var instance = _world.Construction.Instances.Single(value => value.Id == owner);
            var declaration = DeclaredPhysics.Of(instance.PartId).Presentation.Bindings[binding];
            var target = WorkshopDeclaredPresentation.Targets(_world.Construction).Single(value => value.Owner == owner && value.Binding == binding);
            return Control with { Target = target.Target, World = Client.Epoch, Generation = Schedule.World.Epoch.Value,
                Property = WorkshopDeclaredPresentation.Property(declaration.Property), From = declaration.From, To = declaration.To };
        }
        public void Output(ChannelControl control, AnimationOutputKind kind, ulong ordinal = 1, CadenceRevision? cadence = null)
        {
            _event = WorkshopActivationAnimationTests.Output(control, cadence ?? Schedule.Revision, kind,
                kind is AnimationOutputKind.Acknowledgement or AnimationOutputKind.Rejected ? control.Sequence : ordinal);
            _animationOutput();
        }
        public void CompletePreparation()
        {
            _commit!(); _commit = null;
            _commitCompletion!.SetResult(); _commitCompletion = null;
        }
        public MonotonicNanoseconds NativeNow() => WorkshopNativeClock.FromMilliseconds(_milliseconds);
        public async Task<int> CreateClient(int[] states, byte[] bootstrap, int[] clockAbi, int captureMode, bool admitDisplay,
            Action clockReply, Action physicalRead, Action scheduleControl, Action animationOutput, Action service, Action observation, Action<string> retired)
        {
            _clockReply = clockReply; _physicalRead = physicalRead; _scheduleControl = scheduleControl;
            _animationOutput = animationOutput; _service = service; _observation = observation; _retired = retired;
            _world = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
            _beforeReady?.Invoke();
            return 1;
        }
        public void DeliverObservation(byte[] bytes) { _event = bytes; _observation(); }
        public void RetireTransport(string detail) => _retired(detail);
        public byte[] MasterBootstrap(int client) => _startupFailure == WorkshopObservationTests.StartupFailure.Bootstrap
            ? throw new InvalidOperationException("external bootstrap failure") : WorkshopClockWire.EncodePeer(_peer);
        public void ActivateClient(int client)
        {
            for (var i = 0; i < 8; i++) { _milliseconds += 10; _service(); }
        }
        public void SendProbe(int client, byte[] probe)
        {
            var now = WorkshopNativeClock.FromMilliseconds(_milliseconds);
            _event = WorkshopClockWire.EncodeReply(probe, _peer, now, now, new(0));
            _clockReply();
        }
        public byte[] EventBytes(int client) => _event;
        public MonotonicNanoseconds EventTime(int client) => WorkshopNativeClock.FromMilliseconds(_milliseconds);
        public bool AnimationQualified(int client) => _startupFailure != WorkshopObservationTests.StartupFailure.Qualification;
        public void AnimationKinds(int client, int[] kinds) { }
        public int[] DisplayRate(int client) => [60, 1];
        public void Qualified(int client) { }
        public int Status(int client) => (int)(_startupFailure == WorkshopObservationTests.StartupFailure.Qualification ? WorkshopTransportState.Indeterminate : WorkshopTransportState.Ready);
        public void ScheduleResult(int client, byte[] bytes) => WorkshopScheduleWire.ReadHeader(bytes);
        public void SendAnimationControl(int client, byte[] bytes) =>
            Controls.Add(WorkshopAnimationWire.ReadControl(bytes, Session, new(1), Schedule.Revision));
        public byte[] ReadEvents(int client, string epoch)
        {
            var generation = ulong.Parse(epoch, System.Globalization.CultureInfo.InvariantCulture);
            var records = _world.Records.Skip(_eventCursor).ToArray();
            _eventCursor = _world.Records.Count;
            return CuriousContraptions.Abstractions.EventRingDelivery.Write(generation, 0, records);
        }
        public int OpenObserverCount => _observers.Count;
        public int OpenObservationEvents(int client) { var id = ++_observerId; _observers.Add(id, new()); return id; }
        public byte[] ReadObservationEvents(int client, int cursor, string epoch)
        {
            var admitted = ulong.Parse(epoch, System.Globalization.CultureInfo.InvariantCulture);
            var records = new List<CuriousContraptions.Abstractions.SimulationEventRecord>();
            var consumer = _observers[cursor]; var before = consumer.Lapped;
            if (_world.Shared is { } memory && memory.Epoch == admitted)
                while (consumer.TryRead(memory, CuriousContraptions.Abstractions.SimulationDomain.None, out var record)) records.Add(record);
            return CuriousContraptions.Abstractions.EventRingDelivery.Write(admitted, consumer.Lapped - before, records.ToArray());
        }
        public void CloseObservationEvents(int client, int cursor) => _observers.Remove(cursor);
        public byte[] Acknowledgement(int client, byte[] sequence) => _acknowledgements[BinaryPrimitives.ReadUInt64LittleEndian(sequence)];
        public void CompleteAcknowledgement(int client, byte[] sequence) => _acknowledgements.Remove(BinaryPrimitives.ReadUInt64LittleEndian(sequence));
        public void DisposeClient(int client) => _world.DisposeAsync().AsTask().GetAwaiter().GetResult();
        public async Task Send(int client, byte[] bytes, byte[] sequence)
        {
            var hold = HoldNextAcknowledgement; HoldNextAcknowledgement = false;
            await SendCore(bytes);
            _acknowledgements.Add(BinaryPrimitives.ReadUInt64LittleEndian(sequence), _acknowledgement);
            if (hold) { _heldAcknowledgement = new(); await _heldAcknowledgement.Task; }
        }
        private async Task SendCore(byte[] bytes)
        {
            var command = WorkshopWireFlatBuffers.DecodeCommand(bytes);
            if (command.Kind == WorkshopCommandKind.Initialize && _startupFailure == WorkshopObservationTests.StartupFailure.Initialize)
                throw new InvalidOperationException("external initialize failure");
            if (command.Kind == WorkshopCommandKind.Dispose)
            {
                await _world.DisposeAsync();
                _acknowledgement = WorkshopWireFlatBuffers.EncodeResponse(_world.Latest);
                return;
            }
            _acknowledgement = WorkshopWireFlatBuffers.EncodeResponse(command.Kind == WorkshopCommandKind.Initialize
                ? _world.Latest with { Sequence = command.Sequence }
                : await _world.Dispatch(command));
            var schedule = _world.Schedule;
            if (_installed?.World.Epoch == schedule.World.Epoch) return;
            var endpoint = WorkshopWireFlatBuffers.DecodeResponse(_acknowledgement);
            var transition = command.Kind switch
            {
                WorkshopCommandKind.Run => ScheduleTransition.Run,
                WorkshopCommandKind.Reset => ScheduleTransition.Reset,
                WorkshopCommandKind.Pause => ScheduleTransition.Pause,
                WorkshopCommandKind.Resume => ScheduleTransition.Resume,
                _ => ScheduleTransition.Configure
            };
            var payload = new byte[WorkshopScheduleWire.HeaderBytes + WorkshopScheduleWire.PrepareBytes(_acknowledgement.Length)];
            var header = new ScheduleControlHeader(ScheduleControlKind.Prepare, Session, new(1), schedule.Revision,
                schedule.World.Epoch, WorkshopRuntimeRole.Simulation, WorkshopRuntimeRole.Browser);
            WorkshopScheduleWire.WriteHeader(header, payload.AsSpan(0, 64), payload.Length - 64);
            WorkshopCadenceWire.Write(schedule.Settings, payload.AsSpan(64, 32));
            BinaryPrimitives.WriteUInt64LittleEndian(payload.AsSpan(96), _installed?.Revision.Value ?? 0);
            BinaryPrimitives.WriteUInt64LittleEndian(payload.AsSpan(104), _installed?.World.Epoch.Value ?? 0);
            BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(112), (uint)transition);
            BinaryPrimitives.WriteUInt64LittleEndian(payload.AsSpan(120), endpoint.Read.Epoch.Value);
            BinaryPrimitives.WriteUInt64LittleEndian(payload.AsSpan(128), endpoint.Read.Tick.Value);
            BinaryPrimitives.WriteUInt64LittleEndian(payload.AsSpan(136), endpoint.Publication.Value);
            _acknowledgement.CopyTo(payload, 144);
            _event = payload; _scheduleControl();
            void Commit()
            {
                var commit = new byte[WorkshopScheduleWire.HeaderBytes + WorkshopScheduleWire.ScheduleBytes];
                WorkshopScheduleWire.WriteCommit(header with { Kind = ScheduleControlKind.Commit }, schedule, commit);
                if (_installed?.World.WorldGeneration != schedule.World.WorldGeneration) _eventCursor = _world.Records.Count;
                _event = commit; _scheduleControl(); _installed = schedule;
            }
            if (HoldPreparation)
            {
                _commit = Commit; _commitCompletion = new(); await _commitCompletion.Task;
            }
            else Commit();
        }
    }

    private static byte[] Output(ChannelControl control, CadenceRevision cadence, AnimationOutputKind kind, ulong ordinal)
    {
        var bytes = new byte[WorkshopAnimationWire.OutputBytes];
        U64(0, Session.Low); U64(8, Session.High); U64(16, 1); U64(24, cadence.Value);
        U64(32, control.Generation); U64(40, ordinal);
        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(56), control.To);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(136), (ushort)control.Property);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(60), (uint)kind);
        U64(64, control.Target.Value); U64(72, control.World.Value);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(80), control.EventOrdinal);
        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(84), control.EventPhase);
        WorkshopAnimationWire.WriteTimer(bytes, control.Timer);
        return bytes;
        void U64(int offset, ulong value) => BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(offset), value);
    }
}
