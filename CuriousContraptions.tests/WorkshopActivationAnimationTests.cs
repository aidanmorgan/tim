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
    public void TimerSegmentsSeedTheirCommittedEndpointAndRejectInconsistentIntervals()
    {
        var counting = AnimationTimerSegment.Create(new(68,188,128,2,AnimationTimerPhase.Counting));
        Assert.Equal((Half).5, counting.Endpoint.Value);
        Assert.Equal((Half)1, AnimationTimerSegment.Create(new(68,188,200,2,AnimationTimerPhase.Finished)).Endpoint.Value);
        Assert.Equal((Half)0, AnimationTimerSegment.Create(new(0,0,5,0,AnimationTimerPhase.Ready)).Endpoint.Value);
        Assert.Throws<ArgumentException>(() => AnimationTimerSegment.Create(new(68,188,187,2,AnimationTimerPhase.Finished)));
        Assert.Throws<ArgumentException>(() => AnimationTimerSegment.Create(new(68,188,188,2,AnimationTimerPhase.Counting)));
        Assert.Throws<ArgumentException>(() => AnimationTimerSegment.Create(default));
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


    [Fact]
    public void ContactPulseOwnsEachOccurrenceAndResetRetiresPendingAcknowledgements()
    {
        var client = Client(); Invoke(client, "RetireContactFeedback", new SimulationEpoch(1));
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(
            WorkshopInput.Basketball(new(1),0,6,0,0,0,0,1),
            WorkshopInput.Bumper(new(2),0,4,0,0,0,0,1,BumperWork.FromCanonicalStrength((Half)8))));
        var scene = WorkshopPhysicsCompiler.Compile(construction,new(11,22));
        Set(client, "_readScene", scene);
        var declaration = scene.ContactWorks[0];
        var initial = new ContactWorkRead(declaration.Id, declaration.Owner, 0, declaration.InitialEnergy);
        var target = new GpuBodyId(1);
        var clear = new ContactWorkOccurrence(new(0), default, target, 0, 0, (Half)0,
            default, default, ContactWorkEffect.Passive);
        var bodies = new PhysicsBodyReadSet(new[] { new PhysicsBodyRead(
            new(target, 1, 4, default, default, default), CanonicalRotation.Identity, default, default) });
        WorkshopRead Read(ContactWorkRead store, ContactWorkOccurrence occurrence) => new(new(1), new(4), bodies,
            ContactWorks: new(new[] { store }, new[] { occurrence }));
        Invoke(client, "ObserveContactFeedback", Read(initial, clear));
        var paidStore = initial with { OccurrenceCount = 1, RemainingEnergy = new((Half)20) };
        var collider = new ColliderSlot(checked((ushort)Array.FindIndex(scene.Colliders.ToArray(), c => c.Body == declaration.Owner)));
        var hit = clear with { Sequence = 1, Collider = collider, EventOrdinal = 16,
            EventPhase = (Half)(-1024), ApproachSpeed = new((Half)4),
            Debit = new((Half)12), Effect = ContactWorkEffect.Paid };
        Invoke(client, "ObserveContactFeedback", Read(paidStore, hit));
        Invoke(client, "ObserveContactFeedback", Read(paidStore, hit));
        var pulses = Field<System.Collections.IList>(client, "_contactPulses");
        Assert.Single(pulses.Cast<object>());
        Assert.IsType<ArgumentException>(Assert.Throws<TargetInvocationException>(() =>
            Invoke(client, "ObserveContactFeedback", Read(paidStore with { OccurrenceCount = 3 },
                hit with { Sequence = 3 }))).InnerException);
        var passiveStore = paidStore with { OccurrenceCount = 2 };
        var second = hit with { Sequence = 2, EventOrdinal = 88, Debit = new((Half)0), Effect = ContactWorkEffect.Passive };
        foreach (var malformed in new[] {
            (Store: passiveStore with { RemainingEnergy = new((Half)21) }, Event: second),
            (Store: passiveStore, Event: second with { Debit = new((Half)1), Effect = ContactWorkEffect.Paid }),
            (Store: passiveStore, Event: second with { EventPhase = (Half)(-1025) }) })
            Assert.IsType<ArgumentException>(Assert.Throws<TargetInvocationException>(() =>
                Invoke(client, "ObserveContactFeedback", Read(malformed.Store, malformed.Event))).InnerException);
        Assert.Single(pulses.Cast<object>());
        Invoke(client, "ObserveContactFeedback", Read(passiveStore, second));
        Invoke(client, "ObserveContactFeedback", Read(passiveStore, second));
        Assert.Equal(2,pulses.Count);
        var control = Control with { Target = new((2UL << 32) + 2), Kind = AnimationControlKind.Impulse,
            From = (Half)0, To = (Half)1, Duration = CosmeticCurves.PinballBumper.Duration, EventOrdinal = hit.EventOrdinal, EventPhase = hit.EventPhase,
            ImpulseCurve = CosmeticCurves.PinballBumper.ImpulseCurve, Overlap = CosmeticCurves.PinballBumper.Overlap };
        var encoded = WorkshopAnimationWire.Control(Session, new(1), new(1), control);
        Assert.Equal(control, WorkshopAnimationWire.ReadControl(encoded, Session, new(1), new(1)));
        var pulse = pulses[0]!;
        pulse.GetType().GetField("Requested")!.SetValue(pulse, control);
        SetPending(client, control, new(1));
        var cadence = Schedule(1, 2); Invoke(client, "ReconcileAnimationSchedule", cadence); Set(client, "_schedule", cadence);
        client.ReceiveAnimation(Output(control, new(1), AnimationOutputKind.Acknowledgement, 1));
        Assert.Null(pulse.GetType().GetField("Requested")!.GetValue(pulse));
        Assert.Null(pulse.GetType().GetField("Sample")!.GetValue(pulse));
        pulse.GetType().GetField("Requested")!.SetValue(pulse, control);
        SetPending(client, control, new(2));
        var wrong = Output(control, new(2), AnimationOutputKind.Acknowledgement, 1);
        BinaryPrimitives.WriteUInt16LittleEndian(wrong.AsSpan(132), BitConverter.HalfToUInt16Bits((Half).4));
        Assert.Throws<ArgumentException>(() => client.ReceiveAnimation(wrong));
        Assert.NotNull(Field<ChannelControl?>(client, "_animationPending"));
        client.ReceiveAnimation(Output(control, new(2), AnimationOutputKind.Acknowledgement, 1));
        Assert.NotNull(pulse.GetType().GetField("Sample")!.GetValue(pulse));
        SetPending(client, control, new(2));
        var reset = Schedule(2, 3); Invoke(client, "ReconcileAnimationSchedule", reset); Set(client, "_schedule", reset);
        client.ReceiveAnimation(Output(control, new(2), AnimationOutputKind.Acknowledgement, 1));
        Assert.Empty(pulses); Assert.Null(Field<ChannelControl?>(client, "_animationPending"));
    }

    [Fact]
    public void ContactFeedbackBudgetsOnlyNewEventsAndRejectsTrueOverflowAtomically()
    {
        var client = Client(); Invoke(client, "RetireContactFeedback", new SimulationEpoch(1));
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(
            WorkshopInput.Basketball(new(1),0,6,0,0,0,0,1),
            WorkshopInput.Basketball(new(2),2,6,0,0,0,0,1),
            WorkshopInput.Bumper(new(3),0,4,0,0,0,0,1,BumperWork.FromCanonicalStrength((Half)8))));
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(11,22));
        Set(client, "_readScene", scene);
        var declaration = scene.ContactWorks[0];
        var collider = new ColliderSlot(checked((ushort)Array.FindIndex(scene.Colliders.ToArray(), c => c.Body == declaration.Owner)));
        var bodies = new PhysicsBodyReadSet(new[] { new PhysicsBodyRead(
            new(new(1),1,22,default,default,default), CanonicalRotation.Identity,default,default),
            new PhysicsBodyRead(new(new(2),1,22,default,default,default),CanonicalRotation.Identity,default,default) });
        var first = new ContactWorkOccurrence(new(0),collider,new(1),1,16,(Half)0,
            new((Half)4),default,ContactWorkEffect.Passive);
        var clear = new ContactWorkOccurrence(new(0),default,new(2),0,0,(Half)0,
            default,default,ContactWorkEffect.Passive);
        WorkshopRead Read(uint sequence, ContactWorkOccurrence a, ContactWorkOccurrence b) =>
            new(new(1),new(22),bodies,ContactWorks:new(
                new[] { new ContactWorkRead(declaration.Id,declaration.Owner,sequence,declaration.InitialEnergy) },
                new[] { a,b }));
        var initial = Read(1,first,clear);
        Invoke(client,"ObserveContactFeedback",initial);
        var pulses = Field<System.Collections.IList>(client,"_contactPulses");
        Assert.Single(pulses.Cast<object>());
        var capacity = checked((int)(PhysicsSceneDeclaration.ContactWorkCapacity * ContactWorkRead.MaximumOccurrences));
        while (pulses.Count < capacity - 1) pulses.Add(pulses[0]);
        Invoke(client,"ObserveContactFeedback",initial);
        Assert.Equal(capacity - 1,pulses.Count);
        var second = clear with { Collider=collider,Sequence=2,EventOrdinal=16,ApproachSpeed=new((Half)4) };
        var fitting = Read(2,first,second);
        Invoke(client,"ObserveContactFeedback",fitting);
        Assert.Equal(capacity,pulses.Count);
        Invoke(client,"ObserveContactFeedback",fitting);
        Assert.Equal(capacity,pulses.Count);
        var overflow = Read(3,first with { Sequence=3,EventOrdinal=88 },second);
        Assert.IsType<InvalidOperationException>(Assert.Throws<TargetInvocationException>(() =>
            Invoke(client,"ObserveContactFeedback",overflow)).InnerException);
        Assert.Equal(capacity,pulses.Count);
        Assert.True(fitting.ContactWorks.HasSameBits(Field<PhysicsContactWorkRead>(client,"_contactObserved")));
    }

    [Fact]
    public void DeclaredImpulseEnvelopeCombinesOverlapInTheSharedBatchAndReturnsToNeutral()
    {
        var declaration = CosmeticCurves.PinballBumper; declaration.Validate();
        var envelope = new AnimationImpulseDefinition(new(declaration.Duration), declaration.ImpulseCurve, declaration.Overlap,
            AnimationImpulseVisibility.AdvanceWhileHidden, AnimationClock.Presentation, WorkshopAnimationWire.ImpulseCapacity,
            AnimationImpulseTiming.EventTime, declaration.ImpulsePeak);
        var batch = new AnimationBatch(4);
        var handle = batch.RegisterImpulses(new(new(3), AnimationProperty.ColourBlend), envelope,
            new(new AnimationColourBlend((Half)0)), new(new AnimationColourBlend((Half)1)));
        Half Value() => BitConverter.UInt16BitsToHalf(batch.Read(handle).Value.CanonicalBits);
        var period = (double)declaration.Duration;
        Assert.Equal(AnimationImpulseAdmission.Accepted, batch.EnqueueImpulse(handle, new(1), new((Half)1), 1));
        batch.Advance(1, 1 + period / 8, 0);
        var single = (double)Value();
        Assert.InRange(single, .14, .16); // sin²(π/8) of the declared SineSquaredPulse
        Assert.Equal(AnimationImpulseAdmission.Accepted, batch.EnqueueImpulse(handle, new(2), new((Half)1), 1 + period / 8));
        batch.Advance(2, 1 + period / 4, 0);
        Assert.InRange((double)Value(), .5 + single - .01, .5 + single + .01); // saturating sum exceeds either contribution
        batch.Advance(3, 1 + period / 2, 0);
        Assert.Equal((Half)1, Value()); // peak + partner is clamped to the admitted unit range
        batch.Advance(4, 1 + period * 1.5, 0);
        Assert.Equal((Half)0, Value());
        Assert.Equal(0, batch.ReadImpulses(handle).Playing);
        Assert.Throws<ArgumentException>(() => batch.EnqueueImpulse(handle, new(2), new((Half)1), 3));
    }

    [Fact]
    public void CosmeticCurveDeclarationsValidateClosedSetsAndTheWireCarriesTheImpulseEnvelope()
    {
        foreach (var declared in new[] { CosmeticCurves.ImpactSwitch, CosmeticCurves.SignalLamp, CosmeticCurves.Delay, CosmeticCurves.PinballBumper })
        { declared.Validate(); Assert.True(declared.IsDeclared); }
        CosmeticCurveDeclaration.None.Validate(); Assert.False(CosmeticCurveDeclaration.None.IsDeclared);
        Assert.Throws<ArgumentException>(() => (CosmeticCurves.ImpactSwitch with { Duration = (Half)0 }).Validate());
        Assert.Throws<ArgumentException>(() => (CosmeticCurves.Delay with { Duration = (Half)1 }).Validate());
        Assert.Throws<ArgumentException>(() => (CosmeticCurves.PinballBumper with { Overlap = (AnimationImpulseOverlap)9 }).Validate());
        Assert.Throws<ArgumentException>(() => (CosmeticCurves.ImpactSwitch with { Source = (AnimationFeedbackSource)7 }).Validate());
        Assert.Throws<ArgumentException>(() => (CosmeticCurveDeclaration.None with { Duration = (Half).5 }).Validate());
        var impulse = Control with { Target = new((2UL << 32) + 2), Kind = AnimationControlKind.Impulse, From = (Half)0, To = (Half)1,
            Duration = CosmeticCurves.PinballBumper.Duration, ImpulseCurve = CosmeticCurves.PinballBumper.ImpulseCurve, Overlap = CosmeticCurves.PinballBumper.Overlap };
        var bytes = WorkshopAnimationWire.Control(Session, new(1), new(1), impulse);
        Assert.Equal(5, WorkshopAnimationWire.Version);
        Assert.Equal(impulse, WorkshopAnimationWire.ReadControl(bytes, Session, new(1), new(1)));
        var unknown = (byte[])bytes.Clone(); BinaryPrimitives.WriteUInt16LittleEndian(unknown.AsSpan(94), 9);
        Assert.Throws<ArgumentException>(() => WorkshopAnimationWire.ReadControl(unknown, Session, new(1), new(1)));
        Assert.Throws<ArgumentException>(() => WorkshopAnimationWire.Control(Session, new(1), new(1), Control with { Overlap = AnimationImpulseOverlap.SaturatingSum }));
    }

    [Fact]
    public void ContactPumpForwardsTheDeclaredEnvelopeUnchangedBeforeHandingTheLeaseToTransport()
    {
        var client = Client(); Invoke(client, "RetireContactFeedback", new SimulationEpoch(1));
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(
            WorkshopInput.Basketball(new(1),0,6,0,0,0,0,1),
            WorkshopInput.Bumper(new(2),0,4,0,0,0,0,1,BumperWork.FromCanonicalStrength((Half)8))));
        var scene = WorkshopPhysicsCompiler.Compile(construction,new(11,22));
        Set(client, "_readScene", scene); Set(client, "_readConstruction", construction);
        var declaration = scene.ContactWorks[0];
        var initial = new ContactWorkRead(declaration.Id, declaration.Owner, 0, declaration.InitialEnergy);
        var clear = new ContactWorkOccurrence(new(0), default, new(1), 0, 0, (Half)0, default, default, ContactWorkEffect.Passive);
        var bodies = new PhysicsBodyReadSet(new[] { new PhysicsBodyRead(
            new(new(1), 1, 4, default, default, default), CanonicalRotation.Identity, default, default) });
        var collider = new ColliderSlot(checked((ushort)Array.FindIndex(scene.Colliders.ToArray(), c => c.Body == declaration.Owner)));
        var hit = clear with { Sequence = 1, Collider = collider, EventOrdinal = 16, EventPhase = (Half)(-1024),
            ApproachSpeed = new((Half)4), Debit = new((Half)12), Effect = ContactWorkEffect.Paid };
        Invoke(client, "ObserveContactFeedback", new WorkshopRead(new(1), new(4), bodies, ContactWorks: new(new[] { initial }, new[] { clear })));
        Invoke(client, "ObserveContactFeedback", new WorkshopRead(new(1), new(4), bodies,
            ContactWorks: new(new[] { initial with { OccurrenceCount = 1, RemainingEnergy = new((Half)20) } }, new[] { hit })));
        // No JS transport exists here: the pump must have built and leased the declared control before the send faults.
        var fault = Assert.Throws<TargetInvocationException>(() => Invoke(client, "PumpContactFeedback"));
        Assert.IsType<PlatformNotSupportedException>(fault.InnerException);
        var curve = CosmeticCurves.PinballBumper;
        var expected = new ChannelControl(new((2UL << 32) + 2), new(1), 1, 1, AnimationControlKind.Impulse, true, (Half)0, (Half)1,
            curve.Duration, curve.Curve, hit.EventOrdinal, hit.EventPhase, AnimationProperty.ColourBlend,
            ImpulseCurve: curve.ImpulseCurve, Overlap: curve.Overlap);
        Assert.Equal(expected, Field<ChannelControl?>(client, "_animationPending"));
        Assert.Equal(expected, WorkshopAnimationWire.ReadControl(WorkshopAnimationWire.Control(Session, new(1), new(1), expected), Session, new(1), new(1)));
    }

    [Fact]
    public void CommittedPulseOutputRejectsOverlongUnownedOrPaddedSegments()
    {
        var curve = CosmeticCurves.PinballBumper;
        var impulse = Control with { Target = new((2UL << 32) + 2), Kind = AnimationControlKind.Impulse, From = (Half)0, To = (Half)1,
            Duration = curve.Duration, ImpulseCurve = curve.ImpulseCurve, Overlap = curve.Overlap };
        var pulse = Output(impulse, new(1), AnimationOutputKind.Sample, 1);
        Assert.Equal(curve.Duration, WorkshopAnimationWire.Read(pulse, Session, new(1), new(1), out _).PulseDuration);
        foreach (var mutate in new Action<byte[]>[] {
            bytes => BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(132), BitConverter.HalfToUInt16Bits((Half)31)),
            bytes => WorkshopAnimationWire.WriteTimer(bytes, new(68, 188, 128, 2, AnimationTimerPhase.Counting)),
            bytes => BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(134), 1),
            bytes => BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(136), 1) })
        {
            var changed = (byte[])pulse.Clone(); mutate(changed);
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
    private static ActivationLatch?[] Requested(BrowserWorkshopClient c) => Field<ActivationLatch?[]>(c, "_activationRequested");
    private static WorkshopAnimationSample?[] Samples(BrowserWorkshopClient c) => Field<WorkshopAnimationSample?[]>(c, "_activationSamples");
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;
    private static void Set(object owner, string name, object? value) => owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(owner, value);
    private static void Invoke(object owner, string name, object value) => owner.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(owner, new[] { value });
    private static void Invoke(object owner, string name) => owner.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(owner, Array.Empty<object>());
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
        if (control.Kind == AnimationControlKind.Impulse)
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(132), BitConverter.HalfToUInt16Bits(control.Duration));
        return bytes;
        void U64(int offset, ulong value) => BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(offset), value);
    }
}
