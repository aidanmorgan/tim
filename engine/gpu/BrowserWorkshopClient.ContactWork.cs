using System;
using System.Collections.Generic;
using CuriousContraptions.Presentation;
namespace CuriousContraptions.Gpu;

public sealed partial class BrowserWorkshopClient
{
    private sealed class ContactPulse
    {
        public required ContactWorkRead Event;
        public WorkshopAnimationControl? Requested;
        public AnimationPulseSegment? Segment;
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
        if (_contactObserved.Count != 0 && _contactObserved.Count != read.ContactWorks.Count)
            throw new ArgumentException("Contact feedback population changed.");
        if (read.ContactWorks.Count != 0 && (_readScene is null || _readScene.ContactWorks.Length != read.ContactWorks.Count))
            throw new ArgumentException("Contact feedback lacks its validated declaration.");
        var advancing = 0;
        for (var i = 0; i < read.ContactWorks.Count; i++)
        {
            var current = read.ContactWorks[i];
            var declaration = _readScene!.ContactWorks[i];
            var previous = _contactObserved.Count == 0
                ? new ContactWorkRead(declaration.Id, declaration.Owner, declaration.Target, 0, default, 0,
                    (Half)0, default, declaration.InitialEnergy, default) : _contactObserved[i];
            if (current.Id != previous.Id || current.Owner != previous.Owner || current.Target != previous.Target ||
                current.OccurrenceCount < previous.OccurrenceCount || current.OccurrenceCount > previous.OccurrenceCount + 1 ||
                (current.OccurrenceCount == previous.OccurrenceCount && current != previous))
                throw new ArgumentException("A committed contact occurrence was skipped, reversed or changed.");
            if (current.OccurrenceCount > previous.OccurrenceCount)
            {
                var removed = (double)previous.RemainingEnergy.Value - (double)current.RemainingEnergy.Value;
                var due = checked(previous.EventOrdinal + declaration.CooldownPhysicalSteps);
                if (removed < 0 || current.LastDebit.Value != (Half)removed ||
                    (previous.OccurrenceCount != 0 && (current.EventOrdinal < due ||
                        (current.EventOrdinal == due && current.EventPhase < previous.EventPhase))))
                    throw new ArgumentException("Contact feedback changed its reservoir/debit or violated cooldown.");
                advancing++;
            }
        }
        // Capacity follows the admitted finite Run/cooldown count, rather than renderer throughput.
        if (_contactPulses.Count + advancing > PhysicsSceneDeclaration.ContactWorkCapacity * 1601)
            throw new InvalidOperationException("Contact feedback history exhausted.");
        for (var i = 0; i < read.ContactWorks.Count; i++)
            if (read.ContactWorks[i].OccurrenceCount > (_contactObserved.Count == 0 ? 0u : _contactObserved[i].OccurrenceCount))
                _contactPulses.Add(new() { Event = read.ContactWorks[i] });
        _contactObserved = read.ContactWorks;
    }
    private void PumpContactFeedback()
    {
        if (_animationPending is not null || _preparation is not null || _schedule is not { } schedule ||
            _contactWorld != schedule.World.WorldGeneration) return;
        foreach (var pulse in _contactPulses)
        {
            if (pulse.Segment is not null || pulse.Requested is not null) continue;
            var occurrence = pulse.Event;
            var control = new WorkshopAnimationControl(ContactTarget(occurrence.Owner), _contactWorld,
                checked(_hintSequence + 1), occurrence.OccurrenceCount, AnimationControlKind.Impulse, true,
                (Half)0, (Half)1, (Half).32, AnimationCurve.Linear,
                occurrence.EventOrdinal, occurrence.EventPhase, AnimationProperty.ColourBlend);
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
            if (pulse.Segment is null && pulse.Requested is { } request &&
                request.Target == sample.Target && request.World == sample.World && request.Generation == sample.Generation &&
                request.EventOrdinal == sample.EventOrdinal && HalfBits.Equal(request.EventPhase, sample.EventPhase))
                pulse.Requested = null;
    }
    private void ReceiveContactSample(WorkshopAnimationSample sample, AnimationOutputKind kind, WorkshopSchedule active)
    {
        if (sample.World.Value < active.World.WorldGeneration.Value) return;
        foreach (var pulse in _contactPulses)
        {
            if (ContactTarget(pulse.Event.Owner) != sample.Target || pulse.Event.OccurrenceCount != sample.Generation) continue;
            if (pulse.Requested is not { } control || sample.World != _contactWorld ||
                sample.Property != AnimationProperty.ColourBlend || sample.Timer != default ||
                sample.EventOrdinal != control.EventOrdinal || !HalfBits.Equal(sample.EventPhase, control.EventPhase) ||
                !HalfBits.Equal(sample.PulseDuration, control.Duration) || sample.Value != (Half)1)
                throw new ArgumentException("Contact pulse does not own its committed occurrence.");
            if (kind == AnimationOutputKind.Acknowledgement)
                pulse.Segment = AnimationPulseSegment.Create(new(sample.PulseDuration));
            return;
        }
        throw new ArgumentException("Unowned contact pulse output.");
    }
    public bool TryContactWorkFrame(ulong frame, WorkshopPresentationSample physical, GpuBodyId owner,
        out uint count, out Half blend)
    {
        ObjectDisposedException.ThrowIf(_disposed, this); ThrowIfTransportFailed(); PumpContactFeedback();
        count = 0; blend = (Half)0;
        if (!AdmitPresentationFrame(frame) || physical.Evidence.WorldEpoch != _contactWorld) return false;
        foreach (var pulse in _contactPulses)
        {
            if (pulse.Event.Owner != owner) continue;
            var occurred = (pulse.Event.EventOrdinal + (double)pulse.Event.EventPhase / 4096) / 480;
            if (occurred > physical.SimulationTime.Seconds) continue;
            count = pulse.Event.OccurrenceCount;
            if (pulse.Segment is not { } segment)
            {
                if (physical.SimulationTime.Seconds - occurred > .05)
                    throw new InvalidOperationException("Committed pulse exceeded the covered presentation interval.");
                return false;
            }
            blend = Half.Min((Half)1, (Half)(blend +
                segment.Sample(physical.SimulationTime.Seconds, pulse.Event.EventOrdinal, pulse.Event.EventPhase)));
        }
        _lastPresentationApplied = _frameMaster;
        return true;
    }
}
