using System.Buffers.Binary;
using System.Reflection;
using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;
using ChannelControl = CuriousContraptions.Gpu.WorkshopAnimationControl;

namespace CuriousContraptions.Tests;

/// <summary>The main thread's animation channel. Story 9A.23/9A.25b: it relays no one-shot cue (every one is a declared binding the
/// Animation worker drives from the event ring), so its controls are the UI clips, the goal cue and declared visibility.</summary>
public sealed class WorkshopActivationAnimationTests
{
    private static readonly RuntimeSessionId Session = new(11, 22);
    private static ChannelControl Control => new(new(4), new(1), 1, 1, AnimationControlKind.Endpoint,
        true, (float)1, (float)1, (float)1, AnimationCurve.Linear, 10, (float)0, AnimationProperty.ColourBlend);
    private static ChannelControl Goal => Control with { Target = UiCurves.Goal.AnimationTarget, Property = AnimationProperty.Opacity };

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
        var client = Client(); SetPending(client, Control, new(1));
        Assert.Throws<ArgumentException>(() => client.ReceiveAnimation(Output(Control, new(1), AnimationOutputKind.Acknowledgement, 1)));
        Assert.Throws<ArgumentException>(() => client.ReceiveAnimation(Output(Control, new(1), AnimationOutputKind.Sample, 2)));
    }

    [Fact]
    public void DeclaredCadenceBoundaryRestartsOrdinalsAndRejectsReplay()
    {
        var client = Client();
        var battery = PlacedParts.Battery(new(1), 0, 1, 0);
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(battery));
        Set(client, "_readConstruction", construction); Invoke(client, "InstallDeclaredTargets", construction);
        var control = Control with { Target = new(WorkshopDeclaredPresentation.TargetBase), Property = AnimationProperty.ColourRed };
        client.ReceiveAnimation(Output(control, new(1), AnimationOutputKind.Sample, 100));
        var schedule = Schedule(1, 2);
        Invoke(client, "ReconcileAnimationSchedule", schedule); Set(client, "_schedule", schedule);
        control = control with { Generation = schedule.World.Epoch.Value };
        client.ReceiveAnimation(Output(control, new(2), AnimationOutputKind.Sample, 1));
        Assert.Throws<ArgumentException>(() => client.ReceiveAnimation(Output(control, new(2), AnimationOutputKind.Sample, 1)));
        client.ReceiveAnimation(Output(control, new(1), AnimationOutputKind.Sample, 200));
        var samples = Field<Dictionary<AnimationTargetId, WorkshopAnimationSample>>(client, "_declaredSamples");
        Assert.Equal(1UL, samples[control.Target].Pulse.Value);
    }

    // Story 9A.25b: a one-shot binding is a declared target like any other; its samples arrive on the declared range.
    [Fact]
    public void EveryOneShotBindingIsADeclaredTargetOfItsOwner()
    {
        var bumper = WorkshopInput.Generic(PartId.PinballBumper, new(1), 0, 1, 0, 0, 0, 0, 1);
        var receiver = WorkshopInput.Generic(PartId.Receiver, new(2), 3, 1, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(bumper, receiver));
        var targets = WorkshopDeclaredPresentation.Targets(construction);
        Assert.Equal(DeclaredPhysics.Of(PartId.PinballBumper).Presentation.Bindings.Count + DeclaredPhysics.Of(PartId.Receiver).Presentation.Bindings.Count,
            targets.Count);
        Assert.Equal([new GpuBodyId(1)], targets.Take(5).Select(target => target.Owner).Distinct());
        var client = Client(); Set(client, "_readConstruction", construction); Invoke(client, "InstallDeclaredTargets", construction);
        var halo = DeclaredPhysics.Of(PartId.Receiver).Presentation.Bindings[1];
        var control = Control with { Target = targets[6].Target, Property = WorkshopDeclaredPresentation.Property(halo.Property),
            From = halo.To, To = halo.To };
        client.ReceiveAnimation(Output(control, new(1), AnimationOutputKind.Sample, 1));
        Assert.Equal(halo.To, Field<Dictionary<AnimationTargetId, WorkshopAnimationSample>>(client, "_declaredSamples")[targets[6].Target].Value);
    }

    [Fact]
    public void SameConstructionProjectionReplacementRequeuesHiddenOwnerVisibility()
    {
        var client = Client();
        var battery = PlacedParts.Battery(new(1), 0, 1, 0);
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(battery));
        Set(client, "_readConstruction", construction); Invoke(client, "InstallDeclaredTargets", construction);
        var target = new AnimationTargetId(WorkshopDeclaredPresentation.TargetBase);
        Field<Dictionary<GpuBodyId, bool>>(client, "_declaredVisibility")[battery.Id] = false;
        Field<Dictionary<AnimationTargetId, bool>>(client, "_declaredVisibilitySent")[target] = false;
        var schedule = Schedule(1, 2);
        Invoke(client, "ReconcileAnimationSchedule", schedule);
        Assert.Empty(Field<Dictionary<AnimationTargetId, bool>>(client, "_declaredVisibilitySent"));
        Assert.False(Field<Dictionary<GpuBodyId, bool>>(client, "_declaredVisibility")[battery.Id]);
        Assert.Same(construction.Instances, Field<WorkshopConstruction?>(client, "_readConstruction")!.Value.Instances);
    }

    [Fact]
    public void WrongPropertyOrOccurrenceCannotReleaseOwnedAnimationLease()
    {
        foreach (var invalid in new[] { Goal with { Property = AnimationProperty.ColourBlend }, Goal with { EventOrdinal = 11 } })
        {
            var client = Client(); Set(client, "_goalRequested", new ActivationTime(Goal.EventOrdinal, Goal.EventPhase)); SetPending(client, Goal, new(1));
            Assert.Throws<ArgumentException>(() => client.ReceiveAnimation(Output(invalid, new(1), AnimationOutputKind.Acknowledgement, 1)));
            Assert.Equal(Goal, Field<ChannelControl?>(client, "_animationPending")); Assert.Null(Field<WorkshopAnimationSample?>(client, "_goalSample"));
        }
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
    public void DeclaredPropertyRejectsForeignChannelAndResetCannotReviveOldWorld()
    {
        var client = Client();
        var timer = WorkshopInput.Generic(PartId.Delay, new(1), 0, 1, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(timer));
        Set(client, "_readConstruction", construction); Invoke(client, "InstallDeclaredTargets", construction);
        var first = DeclaredPhysics.Of(PartId.Delay).Presentation.Bindings[0];
        var control = Control with { Target = new(WorkshopDeclaredPresentation.TargetBase),
            Property = WorkshopDeclaredPresentation.Property(first.Property), From = first.From, To = first.From };
        client.ReceiveAnimation(Output(control, new(1), AnimationOutputKind.Sample, 1));
        Assert.Throws<ArgumentException>(() => client.ReceiveAnimation(Output(control with { Property = AnimationProperty.Opacity },
            new(1), AnimationOutputKind.Sample, 2)));
        var reset = Schedule(2, 2);
        Invoke(client, "ReconcileAnimationSchedule", reset); Set(client, "_schedule", reset);
        client.ReceiveAnimation(Output(control, new(1), AnimationOutputKind.Sample, 2));
        var fresh = control with { World = new(2), Generation = reset.World.Epoch.Value };
        client.ReceiveAnimation(Output(fresh, new(2), AnimationOutputKind.Sample, 1));
        var samples = Field<Dictionary<AnimationTargetId, WorkshopAnimationSample>>(client, "_declaredSamples");
        Assert.Equal(new SimulationEpoch(2), samples[control.Target].World);
    }

    [Fact]
    public void GoalFeedbackOwnsOccurrenceAcrossCadenceRetryAndReset()
    {
        var goal = Control with { Target=new(ulong.MaxValue), Property=AnimationProperty.Opacity };
        var occurrence = new ActivationTime(goal.EventOrdinal,(float)goal.EventPhase);
        var client=Client(); Set(client,"_goalRequested",occurrence); SetPending(client,goal,new(1));
        client.ReceiveAnimation(Output(goal,new(1),AnimationOutputKind.Acknowledgement,1));
        Assert.NotNull(Field<WorkshopAnimationSample?>(client,"_goalSample"));
        Set(client,"_goalOrdinal",100ul);
        var cadence=Schedule(1,2); Invoke(client,"ReconcileAnimationSchedule",cadence); Set(client,"_schedule",cadence);
        Assert.Equal(0ul,Field<ulong>(client,"_goalOrdinal"));
        Assert.Equal(occurrence,Field<ActivationTime?>(client,"_goalRequested"));
        client.ReceiveAnimation(Output(goal,new(2),AnimationOutputKind.Sample,1));
        SetPending(client,goal,new(2));
        client.ReceiveAnimation(Output(goal,new(2),AnimationOutputKind.Rejected,1));
        Assert.True(Field<bool>(client,"_goalRetry"));
        SetPending(client,goal,new(2));
        Assert.Throws<ArgumentException>(()=>client.ReceiveAnimation(Output(goal with { EventOrdinal=11 },new(2),AnimationOutputKind.Acknowledgement,1)));
        Assert.NotNull(Field<ChannelControl?>(client,"_animationPending"));
        Set(client,"_goalWake",new WorkshopGoalWake(default, WorkshopPhysicsCompiler.Compile(new(new(1), WorkshopCadenceSettings.Default(), new()), new(11,22))));
        var reset=Schedule(2,3); Invoke(client,"ReconcileAnimationSchedule",reset); Set(client,"_schedule",reset);
        client.ReceiveAnimation(Output(goal,new(2),AnimationOutputKind.Acknowledgement,1));
        Assert.Null(Field<ChannelControl?>(client,"_animationPending"));
        Assert.Null(Field<ActivationTime?>(client,"_goalRequested"));
        Assert.Null(Field<WorkshopAnimationSample?>(client,"_goalSample"));
        Assert.False(Field<bool>(client,"_goalRetry"));
        Assert.Null(Field<WorkshopGoalWake?>(client,"_goalWake")); // a new world starts a new wake (and its own cursor epoch)
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
        batch.EnqueueImpulse(handle, new(1), new((float)1), 1);
        batch.Advance(1, 1 + period / 8, 0);
        var single = (double)Value();
        Assert.InRange(single, .14, .16); // sin²(π/8) of the declared SineSquaredPulse
        batch.EnqueueImpulse(handle, new(2), new((float)1), 1 + period / 8);
        batch.Advance(2, 1 + period / 4, 0);
        Assert.InRange((double)Value(), .5 + single - .01, .5 + single + .01); // saturating sum exceeds either contribution
        batch.Advance(3, 1 + period / 2, 0);
        Assert.Equal(1f, Value()); // peak + partner is clamped to the admitted unit range
        batch.Advance(4, 1 + period * 1.5, 0);
        Assert.Equal(0f, Value());
        Assert.Equal(0, batch.ReadImpulses(handle).Playing);
        Assert.Throws<ArgumentException>(() => batch.EnqueueImpulse(handle, new(2), new((float)1), 3));
    }

    private static void AdmitFrame(BrowserWorkshopClient client, ulong frame)
    {
        // The public frame entry points consult the JS transport status, so the admitted-frame state is seeded directly.
        Set(client, "_hasPresentationFrame", true); Set(client, "_presentationFrame", frame); Set(client, "_frameAdmitted", true);
        Set(client, "_frameMaster", new MasterTimeNanoseconds(10)); Set(client, "_frameHintTaken", false);
    }
    private static bool TrySample(BrowserWorkshopClient client, string method, object[] arguments) =>
        (bool)typeof(BrowserWorkshopClient).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(client, arguments)!;

    [Fact]
    public void HintControlSampleAppliesOnceOnAnAdmittedFrameAndNeverWhileHidden()
    {
        var client = Client(); Set(client, "_hintGeneration", 1ul);
        var sample = new WorkshopAnimationSample(UiCurves.Hint.AnimationTarget, default, 1, new(3), new(5), (float).25, 0, (float)0, AnimationProperty.Opacity);
        Set(client, "_hintSample", sample); AdmitFrame(client, 7);
        var arguments = new object[] { WorkshopUiTarget.Hint, null! };
        Assert.True(TrySample(client, "TryUiControlSample", arguments));
        Assert.Equal(new AnimationOpacity((float).25), (AnimationOpacity)arguments[1]);
        Assert.False(TrySample(client, "TryUiControlSample", new object[] { WorkshopUiTarget.Hint, null! })); // consumed for this frame
        Set(client, "_hintSample", sample); AdmitFrame(client, 8);
        Assert.False(TrySample(client, "TryUiControlSample", new object[] { WorkshopUiTarget.Goal, null! })); // the goal is not control-fed
        Set(client, "_uiControlHidden", true);
        Assert.False(TrySample(client, "TryUiControlSample", new object[] { WorkshopUiTarget.Hint, null! })); // hidden: no further samples applied
        Set(client, "_uiControlHidden", false);
        Assert.True(TrySample(client, "TryUiControlSample", new object[] { WorkshopUiTarget.Hint, null! }));
    }

    [Fact]
    public void QueuedRevealSurvivesAPendingPreparationAndSendsOnceItClears()
    {
        var client = Client();
        var preparationType = typeof(BrowserWorkshopClient).GetNestedType("Preparation", BindingFlags.NonPublic)!;
        Set(client, "_preparation", Activator.CreateInstance(preparationType, default(ScheduleControlHeader), WorkshopCadenceSettings.Default(), default(WorkshopResponse), ScheduleTransition.Run));
        client.ControlUi(WorkshopUiTarget.Hint, AnimationControlKind.Reveal, true);
        Assert.Single(Field<System.Collections.ICollection>(client, "_uiQueue"));
        Assert.Null(Field<ChannelControl?>(client, "_animationPending"));
        Set(client, "_schedule", null); Invoke(client, "PumpUiControls"); // no schedule yet: still queued, not dropped
        Assert.Single(Field<System.Collections.ICollection>(client, "_uiQueue"));
        Set(client, "_schedule", Schedule(1, 1)); Set(client, "_preparation", null);
        var fault = Assert.Throws<TargetInvocationException>(() => Invoke(client, "PumpUiControls"));
        Assert.IsType<PlatformNotSupportedException>(fault.InnerException);
        Assert.Equal(AnimationControlKind.Reveal, Field<ChannelControl?>(client, "_animationPending")!.Value.Kind);
        Assert.Empty(Field<System.Collections.ICollection>(client, "_uiQueue"));
    }

    [Fact]
    public void UiControlQueuesBehindTheLeaseAndSendsTheDeclaredHintClipWhenFree()
    {
        var goal = Goal;
        var client = Client(); Set(client, "_goalRequested", new ActivationTime(goal.EventOrdinal, goal.EventPhase)); SetPending(client, goal, new(1));
        Assert.Throws<ArgumentException>(() => client.ControlUi(WorkshopUiTarget.Goal, AnimationControlKind.Reveal, true));
        Assert.Throws<ArgumentException>(() => client.ControlUi(WorkshopUiTarget.Hint, AnimationControlKind.Endpoint, true));
        // The lease is busy: the hint request queues instead of throwing (deferred-work item 1).
        client.ControlUi(WorkshopUiTarget.Hint, AnimationControlKind.Visibility, false);
        client.ControlUi(WorkshopUiTarget.Hint, AnimationControlKind.Reveal, true);
        Assert.Single(Field<System.Collections.ICollection>(client, "_uiQueue")); // Reveal supersedes the queued Visibility.
        Assert.Equal(goal, Field<ChannelControl?>(client, "_animationPending"));
        client.ReceiveAnimation(Output(goal, new(1), AnimationOutputKind.Acknowledgement, 1));
        Assert.Null(Field<ChannelControl?>(client, "_animationPending"));
        // No JS transport exists here: the pump must have leased the declared hint clip before the send faults.
        var fault = Assert.Throws<TargetInvocationException>(() => Invoke(client, "PumpUiControls"));
        Assert.IsType<PlatformNotSupportedException>(fault.InnerException);
        var hint = UiCurves.Hint;
        Assert.Equal(new ChannelControl(hint.AnimationTarget, default, 1, 1, AnimationControlKind.Reveal, true, (float)0, (float)1, hint.Duration, hint.Curve),
            Field<ChannelControl?>(client, "_animationPending"));
        Assert.Empty(Field<System.Collections.ICollection>(client, "_uiQueue"));
        Assert.Equal(1ul, Field<ulong>(client, "_hintGeneration"));
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

    private static BrowserWorkshopClient Client()
    {
        var client = (BrowserWorkshopClient)Activator.CreateInstance(typeof(BrowserWorkshopClient),
            BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { Session }, null)!;
        Set(client, "_peer", new WorkshopClockPeer(Session, new(1), new(0), new(1), new(100_000), WorkshopRuntimeRole.Browser));
        Set(client, "_schedule", Schedule(1, 1)); return client;
    }
    private static WorkshopSchedule Schedule(ulong world, ulong revision) => WorkshopSchedule.Create(
        WorkshopCadenceSettings.Default(), new(revision), new(1), new(0), new(0), new(0), new(0),
        new(new(world), new(revision), new(0), new(0), WorldPlayback.Running));
    private static void SetPending(BrowserWorkshopClient client, ChannelControl control, CadenceRevision cadence)
    { Set(client, "_animationPending", control); Set(client, "_animationPendingCadence", cadence); }
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;
    private static void Set(object owner, string name, object? value) => owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(owner, value);
    private static void Invoke(object owner, string name, object value) => owner.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(owner, new[] { value });
    private static void Invoke(object owner, string name) => owner.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(owner, Array.Empty<object>());
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
