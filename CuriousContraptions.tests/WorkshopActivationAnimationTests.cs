using System.Buffers.Binary;
using System.Reflection;
using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;
using ChannelControl = CuriousContraptions.Gpu.WorkshopAnimationControl;

namespace CuriousContraptions.Tests;

public sealed class WorkshopActivationAnimationTests
{
    private static readonly RuntimeSessionId Session = new(11, 22);
    private static readonly ActivationLatch Latch = new(new(2), new(2), ActivationPhase.Latched,
        new(55), new(1), new(44), 10, (Half)0, new((Half).8), ActivationOccurrenceKind.Contact, new(2), 10, (Half)0);
    private static ChannelControl Control => new(new(4), new(1), 1, 1, AnimationControlKind.Endpoint,
        true, (Half)1, (Half)1, (Half)1, AnimationCurve.Linear, 10, (Half)0, AnimationProperty.ColourBlend);

    [Fact]
    public void TypedAnimationChannelRejectsRetiredSchemaUnknownPropertyAndForeignIdentity()
    {
        var bytes = WorkshopAnimationWire.Control(Session, new(1), new(1), Control);
        Assert.Equal(Control, WorkshopAnimationWire.ReadControl(bytes, Session, new(1), new(1)));
        foreach (var (offset, value) in new[] { (90, 0), (78, 0), (78, 99), (92, 1) })
        {
            var changed = (byte[])bytes.Clone(); BinaryPrimitives.WriteUInt16LittleEndian(changed.AsSpan(offset), (ushort)value);
            Assert.Throws<ArgumentException>(() => WorkshopAnimationWire.ReadControl(changed, Session, new(1), new(1)));
        }
        Assert.Throws<ArgumentException>(() => WorkshopAnimationWire.ReadControl(bytes, new(1, 2), new(1), new(1)));
        var output = Output(Control, new(1), AnimationOutputKind.Acknowledgement, 1);
        Assert.Equal(AnimationProperty.ColourBlend, WorkshopAnimationWire.Read(output, Session, new(1), new(1), out _).Property);
        output[86] = 0;
        Assert.Throws<ArgumentException>(() => WorkshopAnimationWire.Read(output, Session, new(1), new(1), out _));
    }

    [Fact]
    public void SharedBindingEvaluationPreservesCanonicalNeutralActiveAndIntermediateValues()
    {
        var neutral = new AnimationValue(new AnimationMetres((Half).06));
        var active = new AnimationValue(new AnimationMetres((Half)(-.02)));
        Assert.Equal(neutral, AnimationValue.Blend(neutral, active, new((Half)0)));
        Assert.Equal(active, AnimationValue.Blend(neutral, active, new((Half)1)));
        Assert.Equal((Half)(((double)(Half).06 + (double)(Half)(-.02)) * .5),
            AnimationValue.Blend(neutral, active, new((Half).5)).Metres.Value);
        Assert.Throws<ArgumentException>(() => AnimationValue.Blend(neutral, new(new AnimationOpacity((Half)1)), new((Half)1)));
    }

    [Fact]
    public void FirstAnimationOutputCanBeTheOwnedActivationAcknowledgement()
    {
        var client = Client(); SetPending(client, Control, new(1));
        Requested(client)[0] = Latch;
        client.ReceiveAnimation(Output(Control, new(1), AnimationOutputKind.Acknowledgement, 1));
        Assert.Null(Field<ChannelControl?>(client, "_animationPending"));
        Assert.Equal(Latch, Requested(client)[0]);
        Assert.Equal((Half)1, Samples(client)[0]!.Value.Value);
    }

    [Fact]
    public void SameWorldCadenceRetainsOwnershipAndRestartsOnlySampleOrdinals()
    {
        var client = Client(); Requested(client)[0] = Latch;
        Field<ulong[]>(client, "_activationOrdinals")[0] = 100;
        var schedule = Schedule(1, 2);
        Invoke(client, "ReconcileAnimationSchedule", schedule); Set(client, "_schedule", schedule);
        Assert.Equal(Latch, Requested(client)[0]);
        Assert.Equal(0ul, Field<ulong[]>(client, "_activationOrdinals")[0]);
        client.ReceiveAnimation(Output(Control, new(2), AnimationOutputKind.Sample, 1));
        Assert.NotNull(Samples(client)[0]);
    }

    [Fact]
    public void RetiredWorldAcknowledgementDrainsItsLeaseWithoutRevivingVisuals()
    {
        var client = Client(); Requested(client)[0] = Latch; SetPending(client, Control, new(1));
        var next = Schedule(2, 2); Invoke(client, "ReconcileAnimationSchedule", next); Set(client, "_schedule", next);
        Assert.Null(Requested(client)[0]);
        client.ReceiveAnimation(Output(Control, new(1), AnimationOutputKind.Acknowledgement, 1));
        Assert.Null(Field<ChannelControl?>(client, "_animationPending"));
        Assert.Null(Requested(client)[0]); Assert.Null(Samples(client)[0]);
    }

    [Fact]
    public void WrongPropertyOrOccurrenceCannotReleaseOwnedAnimationLease()
    {
        foreach (var invalid in new[] { Control with { Property = AnimationProperty.Opacity }, Control with { EventOrdinal = 11 } })
        {
            var client = Client(); Requested(client)[0] = Latch; SetPending(client, Control, new(1));
            Assert.Throws<ArgumentException>(() => client.ReceiveAnimation(Output(invalid, new(1), AnimationOutputKind.Acknowledgement, 1)));
            Assert.Equal(Control, Field<ChannelControl?>(client, "_animationPending")); Assert.Null(Samples(client)[0]);
        }
    }

    [Fact]
    public void TimerSegmentsCoverCommittedTimeAndNeverFinishTheDisplayedPast()
    {
        var counting = AnimationTimerSegment.Create(new(68,188,128,2,AnimationTimerPhase.Counting));
        Assert.True(counting.TrySample(98, out var halfwayToObservation));
        Assert.Equal((Half).25, halfwayToObservation.Progress.Value);
        Assert.False(counting.TrySample(129, out _));
        Assert.True(counting.TrySample(98, out var paused)); Assert.Equal(halfwayToObservation, paused);
        var finished = AnimationTimerSegment.Create(new(68,188,200,2,AnimationTimerPhase.Finished));
        Assert.True(finished.TrySample(187, out var before));
        Assert.Equal(AnimationTimerPhase.Counting, before.Phase); Assert.True(before.Progress.Value < (Half)1);
        Assert.True(finished.TrySample(188, out var due));
        Assert.Equal(AnimationTimerPhase.Finished, due.Phase); Assert.Equal((Half)1, due.Progress.Value);
        Assert.True(finished.TrySample(67, out var ready)); Assert.Equal(AnimationTimerPhase.Ready, ready.Phase);
        Assert.Throws<ArgumentException>(() => AnimationTimerSegment.Create(new(68,188,187,2,AnimationTimerPhase.Finished)));
        Assert.Throws<ArgumentException>(() => AnimationTimerSegment.Create(new(68,188,188,2,AnimationTimerPhase.Counting)));
    }

    [Fact]
    public void TimerAcknowledgementBindsExactIntervalAndResetCannotReviveIt()
    {
        var control = Control with { Target = new((1UL << 32) + 3), Kind = AnimationControlKind.TimerObservation,
            Generation = 129, Timer = new(68,188,128,2,AnimationTimerPhase.Counting), From=(Half)0, To=(Half).5 };
        var wire = WorkshopAnimationWire.Control(Session, new(1), new(1), control);
        Assert.Equal(control, WorkshopAnimationWire.ReadControl(wire, Session, new(1), new(1)));
        var client = Client(); SetPending(client, control, new(1));
        Field<ChannelControl?[]>(client, "_timerRequested")[0] = control;
        var wrong = control with { Timer = control.Timer with { Due=189 } };
        Assert.Throws<ArgumentException>(() => client.ReceiveAnimation(Output(wrong, new(1), AnimationOutputKind.Acknowledgement, control.Sequence)));
        Assert.NotNull(Field<ChannelControl?>(client, "_animationPending"));
        client.ReceiveAnimation(Output(control, new(1), AnimationOutputKind.Acknowledgement, control.Sequence));
        Assert.NotNull(Field<WorkshopAnimationSample?[]>(client, "_timerSamples")[0]);
        Field<ulong[]>(client, "_timerOrdinals")[0] = 100;
        var cadence = Schedule(1,2); Invoke(client, "ReconcileAnimationSchedule", cadence); Set(client, "_schedule", cadence);
        Assert.Equal(0ul, Field<ulong[]>(client, "_timerOrdinals")[0]);
        client.ReceiveAnimation(Output(control, new(2), AnimationOutputKind.Sample, 1));
        Assert.NotNull(Field<WorkshopAnimationSample?[]>(client, "_timerSamples")[0]);
        SetPending(client, control, new(2));
        client.ReceiveAnimation(Output(control, new(2), AnimationOutputKind.Rejected, control.Sequence));
        Assert.True(Field<bool[]>(client, "_timerRetry")[0]);
        Assert.NotNull(Field<WorkshopAnimationSample?[]>(client, "_timerSamples")[0]);
        SetPending(client, control, new(2));
        var next = Schedule(2,3); Invoke(client, "ReconcileAnimationSchedule", next); Set(client, "_schedule", next);
        client.ReceiveAnimation(Output(control, new(2), AnimationOutputKind.Acknowledgement, control.Sequence));
        Assert.Null(Field<ChannelControl?>(client, "_animationPending"));
        Assert.Null(Field<WorkshopAnimationSample?[]>(client, "_timerSamples")[0]);
        Assert.False(Field<bool[]>(client, "_timerRetry")[0]);
    }

    [Fact]
    public void GoalFeedbackOwnsOccurrenceAcrossCadenceRetryAndReset()
    {
        var goal = Control with { Target=new(ulong.MaxValue), Property=AnimationProperty.Opacity };
        var occurrence = new ActivationTime(goal.EventOrdinal,goal.EventPhase);
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
        var reset=Schedule(2,3); Invoke(client,"ReconcileAnimationSchedule",reset); Set(client,"_schedule",reset);
        client.ReceiveAnimation(Output(goal,new(2),AnimationOutputKind.Acknowledgement,1));
        Assert.Null(Field<ChannelControl?>(client,"_animationPending"));
        Assert.Null(Field<ActivationTime?>(client,"_goalRequested"));
        Assert.Null(Field<WorkshopAnimationSample?>(client,"_goalSample"));
        Assert.False(Field<bool>(client,"_goalRetry"));
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
    private static ActivationLatch?[] Requested(BrowserWorkshopClient c) => Field<ActivationLatch?[]>(c, "_activationRequested");
    private static WorkshopAnimationSample?[] Samples(BrowserWorkshopClient c) => Field<WorkshopAnimationSample?[]>(c, "_activationSamples");
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;
    private static void Set(object owner, string name, object? value) => owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(owner, value);
    private static void Invoke(object owner, string name, object value) => owner.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(owner, new[] { value });
    private static byte[] Output(ChannelControl control, CadenceRevision cadence, AnimationOutputKind kind, ulong ordinal)
    {
        var bytes = new byte[WorkshopAnimationWire.OutputBytes];
        U64(0, Session.Low); U64(8, Session.High); U64(16, 1); U64(24, cadence.Value);
        U64(32, control.Generation); U64(40, ordinal);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(56), BitConverter.HalfToUInt16Bits(control.To));
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(58), (ushort)control.Property);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(60), (uint)kind);
        U64(64, control.Target.Value); U64(72, control.World.Value);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(80), control.EventOrdinal);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(84), BitConverter.HalfToUInt16Bits(control.EventPhase));
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(86), WorkshopAnimationWire.Version);
        WorkshopAnimationWire.WriteTimer(bytes, control.Timer);
        return bytes;
        void U64(int offset, ulong value) => BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(offset), value);
    }
}
