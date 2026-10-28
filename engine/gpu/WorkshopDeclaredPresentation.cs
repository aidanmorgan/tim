using System;
using System.Collections.Generic;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Gpu;

public readonly record struct DeclaredPresentationSample(
    AnimationTargetId Target, GpuBodyId Owner, int Binding, AnimationValue Value);

/// <summary>Animation-owned evaluation of every declared binding: committed-state queries from each committed read, and one-shot
/// cues from the event ring (Story 9A.23/9A.25b). Main-thread code only applies the resulting property samples.</summary>
public sealed class WorkshopDeclaredPresentation
{
    public const ulong TargetBase = 5UL << 32;
    private sealed class Track
    {
        /// <summary>A Replace event clip's cue times not yet reached by the presentation clock, oldest first.</summary>
        public List<double> Cues = new(8);
        public required GpuBodyId Owner;
        public required int Index;
        public required PresentationBinding Declaration;
        public required AnimationTargetId Target;
        public ActivationNodeDeclaration Node;
        public float Capacity;
        public AnimationHandle? Handle;
        public float Input;
        public AnimationValue FollowTarget;
        public uint Count;
        public float Value;
        public float Rest;
        public bool Visible = true;
        public Track Copy() => (Track)MemberwiseClone();
    }
    private ulong _lastSequence;
    /// <summary>Event records the cursor skipped (lapped or reused while copied) in this installation: cumulative and typed. A lapped
    /// cue is lost and never replayed; committed levels self-correct from the next read.</summary>
    public ulong Lapped { get; private set; }
    private readonly Track[] _tracks;
    private readonly AnimationBatch _batch;
    private WorkshopResponse _latest;
    private readonly List<DeclaredPresentationSample> _samples = new(16);
    private ulong _frame;
    private double _time;
    private readonly double _tickSeconds;
    private readonly WorkshopConstruction _construction;

    public WorkshopDeclaredPresentation(WorkshopConstruction construction, WorkshopResponse endpoint)
    {
        if (endpoint.Read.Epoch.Value == 0) throw new ArgumentException("Presentation needs an installed world.");
        _construction = construction;
        WorkshopMessages.ValidatePresentationOwners(endpoint.Read, construction);
        _latest = endpoint;
        _time = endpoint.Read.Capture is { } capture ? capture.Time.Value / 1_000_000_000d : 0;
        _tickSeconds = (double)construction.Settings.SimulationRate.Denominator / construction.Settings.SimulationRate.Numerator;
        var nodes = WorkshopActivationCompiler.Compile(construction).Nodes;
        var tracks = new List<Track>(16);
        foreach (var instance in construction.Instances)
        {
            var bindings = DeclaredPhysics.Of(instance.PartId).Presentation.Bindings;
            for (var i = 0; i < bindings.Count; i++)
            {
                var declaration = bindings[i];
                var node = default(ActivationNodeDeclaration);
                foreach (var candidate in nodes) if (candidate.Owner == instance.Id) node = candidate;
                var track = new Track { Owner = instance.Id, Index = i, Declaration = declaration,
                    Target = new(TargetBase + (ulong)tracks.Count), Node = node, Value = declaration.From, Rest = declaration.From,
                    Capacity = instance is WorkshopGenericInstance owner && DeclaredPhysics.SourceStore(owner.PartId) is { } store
                        ? owner.Parameters[new ParameterId(store.Capacity)] : 0 };
                tracks.Add(track);
            }
        }
        _tracks = tracks.ToArray();
        _batch = new();
        _batch.EnsureCapacity(_tracks.Length);
        foreach (var track in _tracks)
        {
            var d = track.Declaration;
            var binding = new AnimationBinding(track.Target, Property(d.Property));
            if (d.Curve == PresentationCurve.ExponentialFollow)
            {
                track.Value = 0;
                track.Handle = _batch.RegisterFollow(binding, new(Value(d.Property, d.From), Value(d.Property, d.To),
                    Value(d.Property, 0), new(d.Response), AnimationClock.Presentation));
            }
            else if (d.Driver == PresentationDriver.Event && d.Curve == PresentationCurve.SineSquaredPulse)
                // Each record adds one pulse at its own physical time; overlapping pulses combine (the bundle admits SaturatingSum).
                track.Handle = _batch.RegisterImpulses(new(track.Target, AnimationProperty.ColourBlend),
                    new(new(d.Duration), AnimationImpulseCurve.SineSquaredPulse, AnimationImpulseOverlap.SaturatingSum,
                        AnimationImpulseVisibility.AdvanceWhileHidden, AnimationClock.Presentation,
                        AnimationImpulseTiming.EventTime, AnimationImpulseDefinition.CanonicalPeak(AnimationImpulseCurve.SineSquaredPulse)),
                    new(new AnimationColourBlend(0)), new(new AnimationColourBlend(1)));
            else if (d.Curve == PresentationCurve.LinearDecay)
                track.Handle = _batch.RegisterImpulses(new(track.Target, AnimationProperty.ColourBlend),
                    new(new(d.Duration), AnimationImpulseCurve.LinearDecay, AnimationImpulseOverlap.Maximum,
                        AnimationImpulseVisibility.DeferUntilVisible, AnimationClock.Presentation,
                        AnimationImpulseTiming.EventTimeRetainFirstFrame, new(0)),
                    new(new AnimationColourBlend(0)), new(new AnimationColourBlend(1)));
            else if (d.Duration > 0)
                track.Handle = _batch.Register(new(track.Target, AnimationProperty.ColourBlend),
                    new(new(new AnimationColourBlend(0)), new(new AnimationColourBlend(1)), new(d.Duration),
                        d.Curve == PresentationCurve.SmoothStep ? AnimationCurve.SmoothStep :
                        d.Curve == PresentationCurve.CubicSmoothStep ? AnimationCurve.CubicSmoothStep : AnimationCurve.Linear,
                        AnimationRepeat.Once, AnimationClock.Presentation));
        }
        _batch.Advance(++_frame, _time, endpoint.Read.Tick.Value * _tickSeconds);
        // Installation begins with the current occurrence, not an unobserved historical backlog.
        foreach (var track in _tracks)
            if (track.Declaration.Driver == PresentationDriver.CommittedOscillatorPulse)
                for (var i = 0; i < endpoint.Read.Memory.Count; i++)
                {
                    var memory = endpoint.Read.Memory[i];
                    if (memory.Node == track.Node.Id && memory.Count > 0) track.Count = memory.Count - 1;
                }
        ApplyRead(endpoint.Read);
    }

    public static IReadOnlyList<(AnimationTargetId Target, GpuBodyId Owner, int Binding)> Targets(WorkshopConstruction construction)
    {
        var result = new List<(AnimationTargetId, GpuBodyId, int)>(16);
        foreach (var instance in construction.Instances)
        {
            var declarations = DeclaredPhysics.Of(instance.PartId).Presentation.Bindings;
            for (var i = 0; i < declarations.Count; i++) result.Add((new(TargetBase + (ulong)result.Count), instance.Id, i));
        }
        return result;
    }

    /// <summary>The presentation-clock time (master seconds) of a record's physical instant: its ordinal and phase (1/4096 of a
    /// physical step) on the 480 Hz physical clock, placed through the installed projection's anchor (the tick it maps to master time).
    /// It is the existing <see cref="AnimationImpulseTiming.EventTime"/> clock, so a cue starts at the event, never the tick boundary.</summary>
    public static double EventSeconds(SimulationEventRecord record, WorkshopWorldProjection world, double tickSeconds) =>
        world.AnchorMaster.Value / 1_000_000_000d +
        ((record.Ordinal + (double)record.Phase / 4096d) / WorkshopCadenceSettings.PhysicalFrequency - world.AnchorTick.Value * tickSeconds);

    /// <summary>Story 9A.23/9A.25b: one delivery of the Animation worker's event ring cursor, for the installed world. Each record starts
    /// exactly one cue on every event binding of its owner (record Source) that names its kind; any other record (a foreign owner, an
    /// undeclared kind, a level) cues nothing. Sequences only increase within an installation, so a record is never applied twice.</summary>
    public void AcceptEvents(EventRingDelivery delivery, WorkshopWorldProjection world)
    {
        if (delivery.Epoch != _latest.Read.Epoch.Value || world.WorldGeneration != _latest.Read.Epoch)
            throw new ArgumentException("Foreign event ring epoch.");
        var lapped = checked(Lapped + delivery.Skipped);
        var last = _lastSequence;
        var counts = delivery.Records.Length == 0 ? Array.Empty<int>() : new int[_tracks.Length];
        bool Matches(Track track, SimulationEventRecord record) => track.Declaration.Driver == PresentationDriver.Event &&
            track.Declaration.Event == record.Kind && track.Owner.Value == record.Source &&
            (track.Declaration.EventMagnitude != PresentationEventMagnitude.Positive || record.Magnitude > 0f);
        foreach (var record in delivery.Records)
        {
            record.Validate();
            if (record.Sequence <= last) throw new ArgumentException("An event record was delivered twice.");
            last = record.Sequence;
            var at = EventSeconds(record, world, _tickSeconds);
            if (!double.IsFinite(at)) throw new ArgumentException("Nonfinite event presentation time.");
            for (var i = 0; i < _tracks.Length; i++)
            {
                var track = _tracks[i];
                if (!Matches(track, record)) continue;
                counts[i] = checked(counts[i] + 1);
                if (track.Declaration.Curve == PresentationCurve.SineSquaredPulse)
                    _batch.ValidateImpulseAdmission(track.Handle!.Value, new(record.Sequence), new(1), at);
            }
        }
        // Prepare only the actual matching occurrences. No cue, sequence or level changes before all reservations succeed.
        for (var i = 0; i < counts.Length; i++)
        {
            if (counts[i] == 0) continue;
            var track = _tracks[i];
            if (track.Declaration.Curve == PresentationCurve.SineSquaredPulse)
                _batch.ReserveImpulses(track.Handle!.Value, counts[i]);
            else
            {
                var required = (long)track.Cues.Count + counts[i];
                if (required > Array.MaxLength) throw new AnimationStorageException();
                try
                {
                    if (required > track.Cues.Capacity)
                        track.Cues.EnsureCapacity((int)Math.Min(Array.MaxLength, Math.Max(required, 2L * track.Cues.Capacity)));
                }
                catch (OutOfMemoryException) { throw new AnimationStorageException(); }
            }
        }
        foreach (var record in delivery.Records)
        {
            var at = EventSeconds(record, world, _tickSeconds);
            foreach (var track in _tracks)
            {
                if (!Matches(track, record)) continue;
                if (track.Declaration.Curve == PresentationCurve.SineSquaredPulse)
                    _batch.EnqueueImpulse(track.Handle!.Value, new(record.Sequence), new(1), at);
                else track.Cues.Add(at);
            }
        }
        _lastSequence = last; Lapped = lapped;
    }

    public void Accept(WorkshopResponse response)
    {
        if (response.Session != _latest.Session || response.MasterGeneration != _latest.MasterGeneration ||
            response.Cadence != _latest.Cadence || response.Projection != _latest.Projection ||
            response.Read.Epoch != _latest.Read.Epoch || response.Read.Revision.Value < _latest.Read.Revision.Value ||
            response.Read.Tick.Value < _latest.Read.Tick.Value ||
            response.Publication.Value <= _latest.Publication.Value)
            throw new ArgumentException("Stale or foreign committed presentation read.");
        WorkshopMessages.ValidatePresentationOwners(response.Read, _construction);
        ApplyRead(response.Read);
        _latest = response;
    }

    public bool CanRetain(WorkshopConstruction construction, WorkshopResponse endpoint) =>
        endpoint.Read.Epoch == _latest.Read.Epoch &&
        construction == _construction;

    public void ValidateRebind(WorkshopConstruction construction, WorkshopResponse endpoint)
    {
        if (!CanRetain(construction, endpoint) || endpoint.Session != _latest.Session ||
            endpoint.MasterGeneration != _latest.MasterGeneration || endpoint.Projection.Value <= _latest.Projection.Value ||
            endpoint.Cadence.Value < _latest.Cadence.Value || endpoint.Read.Tick.Value < _latest.Read.Tick.Value ||
            endpoint.Read.Revision.Value < _latest.Read.Revision.Value || endpoint.Publication.Value != 0)
            throw new ArgumentException("Foreign or reversed presentation rebind.");
        WorkshopMessages.ValidatePresentationOwners(endpoint.Read, construction);
        _ = StageRead(endpoint.Read);
    }

    public void Rebind(WorkshopConstruction construction, WorkshopResponse endpoint)
    {
        // Prepare only validates; a matching worker Commit is the sole caller that changes active state.
        ValidateRebind(construction, endpoint);
        CommitRead(StageRead(endpoint.Read), endpoint.Read);
        _latest = endpoint;
    }

    private void ApplyRead(WorkshopRead read) => CommitRead(StageRead(read), read);

    private Track[] StageRead(WorkshopRead read)
    {
        var staged = new Track[_tracks.Length];
        for (var i = 0; i < _tracks.Length; i++)
        {
            staged[i] = _tracks[i].Copy();
            // A one-shot cue has no committed query: only its ring records drive it.
            if (staged[i].Declaration.Driver == PresentationDriver.Event) continue;
            staged[i].Input = Query(staged[i], read);
            if (!float.IsFinite(staged[i].Input)) throw new ArgumentException("Nonfinite presentation query.");
            if (staged[i].Declaration.Curve == PresentationCurve.LinearDecay)
            {
                var previous = _tracks[i];
                if (staged[i].Count < previous.Count) throw new ArgumentException("Committed pulse count reversed.");
                var count = staged[i].Count - previous.Count;
                if (count > Array.MaxLength) throw new AnimationStorageException();
                if (count > 0)
                {
                    _batch.ValidateImpulseAdmission(previous.Handle!.Value, new(staged[i].Count), new(1),
                        read.Capture!.Value.Time.Value / 1_000_000_000d);
                    _batch.ReserveImpulses(previous.Handle.Value, (int)count);
                }
            }
            if (staged[i].Declaration.Curve == PresentationCurve.ExponentialFollow)
            {
                var d = staged[i].Declaration;
                staged[i].FollowTarget = Value(d.Property, float.Lerp(d.From, d.To, staged[i].Input));
                _batch.ValidateFollowValue(staged[i].Handle!.Value, staged[i].FollowTarget, _time);
            }
        }
        return staged;
    }

    private void CommitRead(Track[] staged, WorkshopRead read)
    {
        for (var i = 0; i < _tracks.Length; i++)
        {
            var track = _tracks[i]; var next = staged[i];
            var d = track.Declaration;
            var input = next.Input;
            if (d.Curve == PresentationCurve.ExponentialFollow)
            {
                _batch.FollowValue(track.Handle!.Value, next.FollowTarget, _time);
            }
            else if (d.Curve == PresentationCurve.LinearDecay)
            {
                for (ulong occurrence = (ulong)track.Count + 1; occurrence <= next.Count; occurrence++)
                    _batch.EnqueueImpulse(track.Handle!.Value, new(occurrence), new(1),
                        read.Capture!.Value.Time.Value / 1_000_000_000d);
            }
            else if (track.Handle is { } handle && input != track.Input)
                _batch.DriveTo(handle, input >= 1 ? AnimationEndpoint.To : AnimationEndpoint.From, _time);
            track.Node = next.Node; track.Input = input; track.Count = next.Count; track.Rest = next.Rest;
        }
    }

    private static float Query(Track track, WorkshopRead read)
    {
        var d = track.Declaration;
        switch (d.Driver)
        {
            case PresentationDriver.CommittedActivation:
                for (var i = 0; i < read.Activations.Count; i++)
                    if (read.Activations[i].Owner == track.Owner)
                        return read.Activations[i].Phase == ActivationPhase.Latched ? 1 : 0;
                break;
            case PresentationDriver.CommittedCount:
            case PresentationDriver.CommittedOscillatorPhase:
            case PresentationDriver.CommittedOscillatorPulse:
                for (var i = 0; i < read.Memory.Count; i++)
                {
                    var memory = read.Memory[i];
                    if (memory.Node != track.Node.Id) continue;
                    if (d.Driver == PresentationDriver.CommittedCount) return memory.Count >= d.Threshold ? 1 : 0;
                    if (d.Driver == PresentationDriver.CommittedOscillatorPulse)
                    {
                        track.Count = memory.Count;
                        return memory.Count > 0 ? 1 : 0;
                    }
                    if (memory.Phase == OscillatorPhase.Stopped) return .5f;
                    if (track.Node.IntervalTicks == 0) throw new ArgumentException("Oscillator binding lacks its interval.");
                    var remaining = memory.DueTick > read.Tick.Value ? memory.DueTick - read.Tick.Value : 0;
                    var phase = 1f - (float)remaining / track.Node.IntervalTicks;
                    return .5f + .5f * MathF.Sin(MathF.Tau * phase);
                }
                break;
            case PresentationDriver.CommittedTimerProgress:
            case PresentationDriver.CommittedTimerRemaining:
            case PresentationDriver.CommittedTimerActive:
            case PresentationDriver.CommittedTimerPhase:
                for (var i = 0; i < read.Timers.Count; i++)
                {
                    var timer = read.Timers[i];
                    if (timer.Node != track.Node.Id) continue;
                    if (d.Driver == PresentationDriver.CommittedTimerPhase)
                        return TimerPhase(timer.Phase) == d.Phase ? 1 : 0;
                    if (d.Driver == PresentationDriver.CommittedTimerActive)
                        return timer.Phase == ActivationTimerPhase.Counting ? 1 : 0;
                    if (d.Driver == PresentationDriver.CommittedTimerRemaining)
                        return timer.Phase == ActivationTimerPhase.Counting
                            ? Math.Clamp((float)(timer.DueTick - read.Tick.Value) / (timer.DueTick - timer.StartedTick), 0, 1) : 0;
                    return timer.Phase switch
                    {
                        ActivationTimerPhase.Ready => 0,
                        ActivationTimerPhase.Finished => 1,
                        ActivationTimerPhase.Counting => Math.Clamp((float)(read.Tick.Value - timer.StartedTick) / (timer.DueTick - timer.StartedTick), 0, 1),
                        _ => throw new ArgumentException("Unknown timer phase.")
                    };
                }
                break;
            case PresentationDriver.CommittedElectricalSocket:
                foreach (var logic in read.Presentation.Logic)
                    if (logic.Owner == track.Owner) return logic.At((WorkshopSocket)d.Socket!.Value) ? 1 : 0;
                break;
            case PresentationDriver.CommittedSupply:
            case PresentationDriver.CommittedCharge:
                for (var i = 0; i < read.Electrical.Count; i++)
                {
                    var source = read.Electrical[i];
                    if (source.Owner != track.Owner) continue;
                    if (d.Driver == PresentationDriver.CommittedSupply) return source.Available ? 1 : 0;
                    if (track.Capacity <= 0) throw new ArgumentException("Charge binding lacks its declared capacity.");
                    return source.Remaining.Value >= track.Capacity * d.Threshold ? 1 : 0;
                }
                break;
            case PresentationDriver.CommittedActuatorDrive:
            case PresentationDriver.CommittedActuatorState:
                foreach (var actuator in read.Presentation.Actuators)
                    if (actuator.Owner == track.Owner)
                        return d.Driver == PresentationDriver.CommittedActuatorState && actuator.Blocked ? .5f : actuator.SupplyClosed ? 1 : 0;
                break;
            case PresentationDriver.CommittedContactLoadPhase:
                foreach (var load in read.Presentation.Loads)
                    if (load.Frame == track.Owner)
                    {
                        track.Rest = load.Phase == ContactLoadPhase.Underweight ? d.Intermediate ?? d.From : d.From;
                        return load.Phase == ContactLoadPhase.Loaded ? 1 : 0;
                    }
                break;
            default: throw new ArgumentException("Unsupported committed presentation driver.");
        }
        throw new ArgumentException("Declared presentation query is absent from the committed read.");
    }

    public IReadOnlyList<DeclaredPresentationSample> Advance(double presentationSeconds)
    {
        if (!double.IsFinite(presentationSeconds) || presentationSeconds < _time)
            throw new ArgumentException("Presentation clock reversed.");
        _time = presentationSeconds;
        // A Replace cue the clock has reached restarts its clip at the cue's own time (already past when observed late, so the clip is
        // at the exact phase, or at To once expired); of several reached cues only the newest matters, since each restarts from From.
        foreach (var track in _tracks)
        {
            var reached = -1;
            while (reached + 1 < track.Cues.Count && track.Cues[reached + 1] <= _time) reached++;
            if (reached < 0) continue;
            _batch.StartAtOccurrence(track.Handle!.Value, track.Cues[reached]);
            track.Cues.RemoveRange(0, reached + 1);
        }
        _batch.Advance(++_frame, _time, _latest.Read.Tick.Value * _tickSeconds);
        _samples.Clear();
        foreach (var track in _tracks)
        {
            var d = track.Declaration;
            if (!track.Visible || (d.Driver == PresentationDriver.CommittedTimerPhase && track.Input == 0)) continue;
            var amount = track.Handle is { } handle
                ? BitConverter.UInt32BitsToSingle(_batch.Read(handle).Value.CanonicalBits) : track.Input;
            if (d.Driver == PresentationDriver.Event)
            {
                // The one-shot blend arithmetic of the 9A.25a goldens (AnimationValue.Blend), bit for bit.
                var value = AnimationValue.Blend(Value(d.Property, d.From), Value(d.Property, d.To), new(amount));
                track.Value = BitConverter.UInt32BitsToSingle(value.CanonicalBits);
                _samples.Add(new(track.Target, track.Owner, track.Index, value));
                continue;
            }
            if (d.Curve == PresentationCurve.ExponentialFollow) track.Value = amount;
            else
            {
                var from = d.Driver == PresentationDriver.CommittedContactLoadPhase ? track.Rest : d.From;
                track.Value = d.Driver == PresentationDriver.CommittedActuatorState && amount == .5f && d.Intermediate is { } middle
                    ? middle : float.Lerp(from, d.To, amount);
            }
            _samples.Add(new(track.Target, track.Owner, track.Index, Value(d.Property, track.Value)));
        }
        return _samples;
    }

    public void SetVisible(AnimationTargetId target, AnimationProperty property, bool visible)
    {
        foreach (var track in _tracks)
            if (track.Target == target)
            {
                if (Property(track.Declaration.Property) != property)
                    throw new ArgumentException("Visibility property does not match its declared target.");
                foreach (var owned in _tracks)
                    if (owned.Owner == track.Owner)
                    {
                        if (owned.Handle is { } handle) _batch.SetVisible(handle, visible);
                        owned.Visible = visible;
                    }
                return;
            }
        throw new ArgumentException("Visibility requires a declared target.");
    }

    public static PresentationTimerPhase TimerPhase(ActivationTimerPhase phase) => phase switch
    {
        ActivationTimerPhase.Ready => PresentationTimerPhase.Ready,
        ActivationTimerPhase.Counting => PresentationTimerPhase.Counting,
        ActivationTimerPhase.Finished => PresentationTimerPhase.Finished,
        _ => throw new ArgumentException("Unknown committed timer phase.")
    };

    public static AnimationProperty Property(PresentationProperty property) => property switch
    {
        PresentationProperty.Visible => AnimationProperty.ColourBlend,
        PresentationProperty.LocalRotationZ => AnimationProperty.LocalRotationAngle,
        PresentationProperty.LocalX or PresentationProperty.LocalY or PresentationProperty.LocalZ => AnimationProperty.LocalTranslation,
        PresentationProperty.UniformScale or PresentationProperty.ScaleX or PresentationProperty.ScaleY or PresentationProperty.ScaleZ => AnimationProperty.UniformScale,
        PresentationProperty.AlbedoRed or PresentationProperty.EmissionRed => AnimationProperty.ColourRed,
        PresentationProperty.AlbedoGreen or PresentationProperty.EmissionGreen => AnimationProperty.ColourGreen,
        PresentationProperty.AlbedoBlue or PresentationProperty.EmissionBlue => AnimationProperty.ColourBlue,
        _ => throw new ArgumentException("Unknown declared property.")
    };
    public static AnimationValue Value(PresentationProperty property, float value) => WorkshopAnimationWire.Value(Property(property), value);
}

