using System;
using System.Collections.Generic;
using CuriousContraptions.Presentation;
namespace CuriousContraptions.Gpu;

public sealed partial class BrowserWorkshopClient
{
    private sealed class ContactPulse
    {
        public required ContactWorkOccurrence Event;
        public required GpuBodyId Owner;
        public WorkshopAnimationControl? Requested;
        public WorkshopAnimationSample? Sample;
    }
    private readonly List<ContactPulse> _contactPulses = new();
    private PhysicsContactWorkRead _contactObserved;
    private SimulationEpoch _contactWorld;
    private static AnimationTargetId ContactTarget(GpuBodyId owner) => new(checked((2UL << 32) + owner.Value));

    private void RetireContactFeedback(SimulationEpoch epoch)
    {
        _contactPulses.Clear(); _contactObserved = default; _contactWorld = epoch;
    }
    private void ObserveContactFeedback(WorkshopRead read)
    {
        if (_contactWorld != read.Epoch) throw new ArgumentException("Contact feedback epoch was not installed.");
        if (_readScene is null) throw new ArgumentException("Contact feedback lacks its installed scene.");
        read.ContactWorks.ValidateScene(_readScene,read.Bodies);
        if (_contactObserved.Count == 0)
        {
            Span<ContactWorkRead> stores=stackalloc ContactWorkRead[PhysicsSceneDeclaration.ContactWorkCapacity];
            Span<ContactWorkOccurrence> events=stackalloc ContactWorkOccurrence[PhysicsContactWorkRead.OccurrenceCapacity];
            for (var i=0; i<read.ContactWorks.Count; i++)
            {
                var declaration=_readScene.ContactWorks[i];
                stores[i]=new(declaration.Id,declaration.Owner,0,declaration.InitialEnergy);
            }
            for (var i=0; i<read.ContactWorks.OccurrenceCount; i++)
            {
                var current=read.ContactWorks.Occurrence(i);
                events[i]=new(current.Work,default,current.Target,0,0,(Half)0,default,default,ContactWorkEffect.Passive);
            }
            _contactObserved=new(stores[..read.ContactWorks.Count],events[..read.ContactWorks.OccurrenceCount]);
        }
        read.ContactWorks.ValidateAdvance(_contactObserved);
        for (var i=0; i<read.ContactWorks.OccurrenceCount; i++)
        {
            var current=read.ContactWorks.Occurrence(i); var previous=_contactObserved.Occurrence(i);
            if (current.Sequence == previous.Sequence) continue;
            var declaration=_readScene.ContactWorks[current.Work.Value];
            var due=checked(previous.EventOrdinal+declaration.CooldownPhysicalSteps);
            if (previous.Sequence!=0 && (current.EventOrdinal<due ||
                (current.EventOrdinal==due && current.EventPhase<previous.EventPhase)))
                throw new ArgumentException("Contact feedback violated its target cooldown.");
        }
        var additional = 0;
        for (var owner = 0; owner < read.ContactWorks.Count; owner++)
            additional = checked(additional + checked((int)(read.ContactWorks[owner].OccurrenceCount -
                _contactObserved[owner].OccurrenceCount)));
        if (additional > PhysicsSceneDeclaration.ContactWorkCapacity * ContactWorkRead.MaximumOccurrences - _contactPulses.Count)
            throw new InvalidOperationException("Contact feedback history exhausted.");
        // Owner sequences order simultaneous targets independently of target identity order.
        for (var owner=0; owner<read.ContactWorks.Count; owner++)
            for (var sequence=_contactObserved[owner].OccurrenceCount+1; sequence<=read.ContactWorks[owner].OccurrenceCount; sequence++)
                for (var i=0; i<read.ContactWorks.OccurrenceCount; i++)
                {
                    var current=read.ContactWorks.Occurrence(i);
                    if (current.Work.Value==owner && current.Sequence==sequence)
                        _contactPulses.Add(new() { Event=current, Owner=read.ContactWorks[owner].Owner });
                }
        _contactObserved = read.ContactWorks;
    }
    private void PumpContactFeedback()
    {
        if (_animationPending is not null || _preparation is not null || _schedule is not { } schedule ||
            _contactWorld != schedule.World.WorldGeneration) return;
        foreach (var pulse in _contactPulses)
        {
            if (pulse.Sample is not null || pulse.Requested is not null ||
                !TryCosmeticDeclaration(pulse.Owner, AnimationFeedbackSource.ContactWork, out var declaration)) continue;
            var occurrence = pulse.Event;
            // Occurrence sequence is the worker's impulse identity; the declared envelope travels unchanged.
            var control = new WorkshopAnimationControl(ContactTarget(pulse.Owner), _contactWorld,
                checked(_animationSequence + 1), occurrence.Sequence, AnimationControlKind.Impulse, true,
                (Half)0, (Half)1, declaration.Duration, declaration.Curve,
                occurrence.EventOrdinal, occurrence.EventPhase, AnimationProperty.ColourBlend,
                ImpulseCurve: declaration.ImpulseCurve, Overlap: declaration.Overlap);
            SendAnimation(control, schedule); pulse.Requested = control; return;
        }
    }
    private void RetryRejectedContact(WorkshopAnimationControl command)
    {
        foreach (var pulse in _contactPulses)
            if (pulse.Requested == command) pulse.Requested = null;
    }
    private void RetryStaleContactAcknowledgement(WorkshopAnimationSample sample)
    {
        foreach (var pulse in _contactPulses)
            if (pulse.Sample is null && pulse.Requested is { } request &&
                request.Target == sample.Target && request.World == sample.World && request.Generation == sample.Generation &&
                request.EventOrdinal == sample.EventOrdinal && HalfBits.Equal(request.EventPhase, sample.EventPhase))
                pulse.Requested = null;
    }
    private void ReceiveContactSample(WorkshopAnimationSample sample, AnimationOutputKind kind, WorkshopSchedule active)
    {
        if (sample.World.Value < active.World.WorldGeneration.Value) return;
        foreach (var pulse in _contactPulses)
        {
            if (ContactTarget(pulse.Owner) != sample.Target || pulse.Event.Sequence != sample.Generation) continue;
            if (pulse.Requested is not { } control || sample.World != _contactWorld ||
                sample.Property != AnimationProperty.ColourBlend || sample.Timer != default ||
                sample.EventOrdinal != control.EventOrdinal || !HalfBits.Equal(sample.EventPhase, control.EventPhase) ||
                !HalfBits.Equal(sample.PulseDuration, control.Duration))
                throw new ArgumentException("Contact pulse does not own its committed occurrence.");
            pulse.Sample = sample;
            return;
        }
        throw new ArgumentException("Unowned contact pulse output.");
    }
    /// <summary>The worker already combined overlapping occurrences; the newest owned sample is the target's blend.</summary>
    private bool TryContactSample(WorkshopPresentationSample physical, GpuBodyId owner, out WorkshopCosmeticSample result)
    {
        result = default;
        if (physical.Evidence.WorldEpoch != _contactWorld) return false;
        ContactPulse? newest = null;
        foreach (var pulse in _contactPulses)
        {
            if (pulse.Owner != owner || pulse.Sample is not { } sample || sample.World != _contactWorld ||
                sample.AppliedAt.Value > _frameMaster.Value) continue;
            if (newest is null || pulse.Event.Sequence > newest.Event.Sequence) newest = pulse;
        }
        if (newest is null) return false;
        _lastPresentationApplied = _frameMaster;
        result = new(newest.Sample!.Value.Value, AnimationTimerPhase.None);
        return true;
    }
}
