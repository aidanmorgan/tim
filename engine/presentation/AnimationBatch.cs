using CuriousContraptions.Abstractions;
using System;
using System.Collections.Generic;

namespace CuriousContraptions.Presentation;

public enum AnimationProperty { LocalRotationAngle = 1, LocalTranslation = 2, Opacity = 3, UniformScale = 4, ColourBlend = 5, ColourRed = 6, ColourGreen = 7, ColourBlue = 8 }
public enum AnimationCurve { Linear, SmoothStep, SinePulse, CubicSmoothStep }
public enum AnimationRepeat { Once, Loop, PingPong }
public enum AnimationClock { Presentation, Simulation }
public enum AnimationPlayback { Stopped, Playing, Completed }
public enum AnimationStop { Hold, RestoreInitial }
public enum AnimationEndpoint { From, To }

public readonly record struct AnimationTargetId
{
    public ulong Value { get; }
    public AnimationTargetId(ulong value)
    {
        ArgumentOutOfRangeException.ThrowIfZero(value);
        Value = value;
    }
}
public readonly record struct AnimationGeneration
{
    public ulong Value { get; }
    public AnimationGeneration(ulong value)
    {
        ArgumentOutOfRangeException.ThrowIfZero(value);
        Value = value;
    }
}
public readonly record struct AnimationBinding(AnimationTargetId Target, AnimationProperty Property);
public readonly record struct AnimationHandle
{
    internal AnimationBatch Owner { get; }
    public AnimationGeneration Generation { get; }
    public int Slot { get; }
    public ulong Version { get; }
    internal AnimationHandle(AnimationBatch owner, AnimationGeneration generation, int slot, ulong version)
    {
        Owner = owner; Generation = generation; Slot = slot; Version = version;
    }
}
public readonly record struct AnimationSample(AnimationBinding Binding, AnimationValue Value);
public readonly record struct AnimationRead(AnimationPlayback Playback, AnimationValue Value, bool Visible);
public readonly record struct AnimationFrameWork(int Evaluated, int DirtyWrites, int Scheduled, int Registered);

/// <summary>Canonical property-specific clip with f32 local evaluation.</summary>
public sealed record AnimationDefinition
{
    public AnimationValue From { get; }
    public AnimationValue To { get; }
    public AnimationDurationSeconds Duration { get; }
    public AnimationCurve Curve { get; }
    public AnimationRepeat Repeat { get; }
    public AnimationClock Clock { get; }

    public AnimationDefinition(AnimationValue from, AnimationValue to, AnimationDurationSeconds duration,
        AnimationCurve curve, AnimationRepeat repeat, AnimationClock clock)
    {
        AnimationValue.SameProperty(from, to);
        AnimationNumbers.Positive(duration.Value);
        if (!Enum.IsDefined(curve)) throw new ArgumentOutOfRangeException(nameof(curve));
        if (!Enum.IsDefined(repeat)) throw new ArgumentOutOfRangeException(nameof(repeat));
        if (!Enum.IsDefined(clock)) throw new ArgumentOutOfRangeException(nameof(clock));
        From = from; To = to; Duration = duration; Curve = curve; Repeat = repeat; Clock = clock;
    }

    internal AnimationValue Evaluate(float position, bool completed)
    {
        var phase = completed ? 1 : Repeat switch
        {
            AnimationRepeat.Once => position,
            AnimationRepeat.Loop => position % 1,
            AnimationRepeat.PingPong => 1 - Math.Abs(position % 2 - 1),
            _ => throw new InvalidOperationException("Unsupported animation repetition.")
        };
        if (phase == 0) return From;
        if (phase == 1) return Curve == AnimationCurve.SinePulse ? From : To;
        var p = Math.Min(phase, 1 - phase);
        var smooth = p * p * p * (p * (p * 6 - 15) + 10);
        var amount = Curve switch
        {
            AnimationCurve.Linear => phase,
            AnimationCurve.CubicSmoothStep => phase * phase * (3 - 2 * phase),
            AnimationCurve.SmoothStep => phase <= .5f ? smooth : 1 - smooth,
            AnimationCurve.SinePulse => MathF.Sin(MathF.PI * phase),
            _ => throw new InvalidOperationException("Unsupported animation curve.")
        };
        return AnimationValue.FromScalar(From.Property, AnimationNumbers.Interpolate(From.Number, To.Number, amount));
    }
}

/// <summary>Single-thread animation owner with demand-grown storage its owner may reserve (<see cref="EnsureCapacity"/>). Output is
/// borrowed until the next mutation. Registration allocates; stable-topology frame evaluation does not.</summary>
public sealed class AnimationBatch
{
    private enum AnimationKind { Clip, ExponentialFollow, Impulses, Oscillation }
    private struct Slot
    {
        public AnimationKind Kind;
        public AnimationClock Clock;
        public AnimationValue Initial, AnchorValue, TargetValue, ImpulsePeak;
        public AnimationFollowDefinition? Follow;
        public AnimationImpulseState? Impulses;
        public AnimationOscillationState? Oscillation;
        public AnimationDefinition? Definition;
        public AnimationBinding Binding;
        public AnimationPlayback Playback;
        public PhysicalTime Started, Admitted;
        public AnimationValue Value, Published;
        public AnimationPhase Offset;
        public AnimationPlaybackRate Rate;
        public AnimationEndpoint Endpoint;
        public bool Visible, HasPublished;
        public int ActiveIndex;
    }
    private Slot[] _slots = [];
    private ulong[] _versions = [];
    private int[] _active = [], _free = [];
    private AnimationSample[] _samples = [];
    private Dictionary<AnimationBinding, int> _writers = new(16);
    private int _activeCount, _freeCount, _sampleCount;
    private ulong _frame;
    private PhysicalTime _presentationTime, _simulationTime;
    public AnimationGeneration Generation { get; private set; } = new(1);
    public int RegisteredCount => _slots.Length - _freeCount;
    public int ScheduledCount => _activeCount;
    public ReadOnlySpan<AnimationSample> Samples => _samples.AsSpan(0, _sampleCount);

    public int Capacity => _slots.Length;
    public AnimationBatch() => EnsureCapacity(12);

    /// <summary>Story 9R.5b: grows the registry to at least <paramref name="capacity"/> slots, keeping every registered binding,
    /// its handle and its schedule. An owner whose target population is the machine's (unbounded) grows it as targets arrive.</summary>
    public void EnsureCapacity(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        if (capacity > Array.MaxLength) throw new AnimationStorageException();
        if (capacity <= 3L * _slots.Length / 4) return;
        var previous = _slots.Length;
        capacity = (int)Math.Min(Array.MaxLength, Math.Max(16L, Math.Max((4L * capacity + 2) / 3, (long)previous * 2)));
        if (capacity == previous) return;
        try
        {
            var slots = new Slot[capacity]; var slotGenerations = new ulong[capacity];
            var active = new int[capacity]; var free = new int[capacity]; var samples = new AnimationSample[capacity];
            Array.Copy(_slots, slots, previous); Array.Copy(_versions, slotGenerations, previous);
            Array.Copy(_active, active, _activeCount); Array.Copy(_free, free, _freeCount);
            var freeCount = _freeCount;
            for (var index = capacity - 1; index >= previous; index--) free[freeCount++] = index;
            _slots = slots; _versions = slotGenerations; _active = active; _free = free; _samples = samples;
            _freeCount = freeCount; _sampleCount = 0;
        }
        catch (OutOfMemoryException) { throw new AnimationStorageException(); }
    }

    public AnimationHandle Register(AnimationBinding binding,AnimationDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return RegisterSlot(binding,new(){Kind=AnimationKind.Clip,Definition=definition,Clock=definition.Clock,
            Initial=definition.From},definition.From,definition.To);
    }

    public AnimationHandle RegisterFollow(AnimationBinding binding,AnimationFollowDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return RegisterSlot(binding,new(){Kind=AnimationKind.ExponentialFollow,Follow=definition,Clock=definition.Clock,
            Initial=definition.Initial,AnchorValue=definition.Initial,TargetValue=definition.Initial},definition.From,definition.To);
    }

    public AnimationHandle RegisterOscillation(AnimationBinding binding,AnimationOscillationDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if(binding.Property is not (AnimationProperty.LocalRotationAngle or AnimationProperty.LocalTranslation) ||
            binding.Property != definition.VelocityPerStrength.Property)
            throw new ArgumentException("Oscillation requires a signed motion property.");
        return RegisterSlot(binding,new(){Kind=AnimationKind.Oscillation,Clock=definition.Clock,
            Oscillation=new(definition),Initial=AnimationValue.FromScalar(binding.Property,0)},
            AnimationValue.FromScalar(binding.Property,0),AnimationValue.FromScalar(binding.Property,0));
    }
    public void ValidateOscillationKick(AnimationHandle handle,AnimationOccurrenceId occurrence,AnimationSignedStrength strength,PhysicalTime clockTime)
    {
        var index=Resolve(handle);ref var slot=ref _slots[index];
        if(slot.Kind!=AnimationKind.Oscillation)throw new InvalidOperationException("Kick requires an oscillation binding.");
        if(clockTime<Clock(slot.Clock)||clockTime<slot.Admitted)
            throw new ArgumentOutOfRangeException(nameof(clockTime));
        slot.Oscillation!.ValidateKick(occurrence,strength,clockTime);
    }
    public void KickOscillation(AnimationHandle handle,AnimationOccurrenceId occurrence,AnimationSignedStrength strength,PhysicalTime clockTime)
    {
        ValidateOscillationKick(handle,occurrence,strength,clockTime);
        var index=Resolve(handle);ref var slot=ref _slots[index];
        slot.Oscillation!.Kick(occurrence,strength,clockTime);
        slot.Admitted=clockTime;slot.Playback=AnimationPlayback.Playing;_sampleCount=0;Schedule(index);
    }
    public AnimationOscillationRead ReadOscillation(AnimationHandle handle)
    {
        ref var slot=ref _slots[Resolve(handle)];
        if(slot.Kind!=AnimationKind.Oscillation)throw new InvalidOperationException("Diagnostics require an oscillation binding.");
        return slot.Oscillation!.Read(Clock(slot.Clock));
    }

    public AnimationHandle RegisterImpulses(AnimationBinding binding,AnimationImpulseDefinition definition,AnimationValue from,AnimationValue to)
    {
        ArgumentNullException.ThrowIfNull(definition);
        AnimationValue.SameProperty(from,to);
        return RegisterSlot(binding,new(){Kind=AnimationKind.Impulses,Clock=definition.Clock,
            Impulses=new(definition),Initial=from,ImpulsePeak=to},from,to);
    }

    public void ValidateImpulseAdmission(AnimationHandle handle,AnimationOccurrenceId occurrence,AnimationStrength strength)
    {
        var index=Resolve(handle);ref var slot=ref _slots[index];
        if(slot.Kind!=AnimationKind.Impulses)throw new InvalidOperationException("Impulse admission requires an impulse definition.");
        slot.Impulses!.ValidateAdmission(occurrence,strength);
    }

    public void ReserveImpulses(AnimationHandle handle, int additional)
    {
        ref var slot = ref _slots[Resolve(handle)];
        if (slot.Kind != AnimationKind.Impulses) throw new InvalidOperationException("Impulse reservation requires an impulse definition.");
        slot.Impulses!.Reserve(additional);
    }

    public void EnqueueImpulse(AnimationHandle handle,AnimationOccurrenceId occurrence,AnimationStrength strength,PhysicalTime occurredAt)
    {
        var index=Resolve(handle);ref var slot=ref _slots[index];
        if(slot.Kind!=AnimationKind.Impulses)throw new InvalidOperationException("Impulse admission requires an impulse definition.");
        slot.Impulses!.Enqueue(occurrence,strength,occurredAt);
        _sampleCount=0;slot.Playback=AnimationPlayback.Playing;Schedule(index);
    }

    public AnimationImpulseRead ReadImpulses(AnimationHandle handle)
    {
        ref var slot=ref _slots[Resolve(handle)];
        if(slot.Kind!=AnimationKind.Impulses)throw new InvalidOperationException("Impulse diagnostics require an impulse definition.");
        return slot.Impulses!.Read();
    }

    public void CancelImpulses(AnimationHandle handle)
    {
        var index=Resolve(handle);ref var slot=ref _slots[index];
        if(slot.Kind!=AnimationKind.Impulses)throw new InvalidOperationException("Impulse cancellation requires an impulse definition.");
        slot.Impulses!.Cancel();slot.Value=slot.Initial;slot.Playback=AnimationPlayback.Stopped;
        _sampleCount=0;Schedule(index);
    }

    private AnimationHandle RegisterSlot(AnimationBinding binding,Slot slot,AnimationValue from,AnimationValue to)
    {
        AnimationValue.SameProperty(from,to);
        if(binding.Target.Value==0 || !Enum.IsDefined(binding.Property) || binding.Property!=from.Property)
            throw new ArgumentException("Animation target/property is invalid or differs from the quantity.");
        if(_writers.ContainsKey(binding))throw new InvalidOperationException("Visual property already has an animation writer.");
        try
        {
            var writerCapacity = _writers.EnsureCapacity(0);
            var required = checked(_writers.Count + 1);
            if (required > writerCapacity)
                _writers.EnsureCapacity((int)Math.Min(Array.MaxLength, Math.Max(required, 2L * writerCapacity)));
            EnsureCapacity(checked(RegisteredCount + 1));
        }
        catch (OutOfMemoryException) { throw new AnimationStorageException(); }
        catch (OverflowException) { throw new AnimationStorageException(); }
        var index=_free[_freeCount-1];var version=checked(_versions[index]+1);
        _sampleCount=0;_freeCount--;_versions[index]=version;
        slot.Binding=binding;slot.Playback=AnimationPlayback.Stopped;slot.Value=slot.Initial;
        slot.Rate=new((float)1);slot.Admitted=Clock(slot.Clock);slot.Visible=true;slot.ActiveIndex=-1;
        _slots[index]=slot;_writers.Add(binding,index);Schedule(index);
        return new(this,Generation,index,version);
    }

    /// <summary>Retarget at an admitted clock boundary, including elapsed invisible/skipped frames.</summary>
    public void ValidateFollowValue(AnimationHandle handle,AnimationValue target,PhysicalTime clockTime)
    {
        var index=Resolve(handle);ref var slot=ref _slots[index];
        if(slot.Kind!=AnimationKind.ExponentialFollow)throw new InvalidOperationException("Value following requires a follow definition.");
        var definition=slot.Follow!;
        AnimationValue.SameProperty(definition.From,target);
        if(target.Number<Math.Min(definition.From.Number,definition.To.Number)||target.Number>Math.Max(definition.From.Number,definition.To.Number))
            throw new ArgumentOutOfRangeException(nameof(target));
        if(clockTime<Clock(slot.Clock)||clockTime<slot.Admitted)
            throw new ArgumentOutOfRangeException(nameof(clockTime));
    }

    public void FollowValue(AnimationHandle handle,AnimationValue target,PhysicalTime clockTime)
    {
        ValidateFollowValue(handle,target,clockTime);
        var index=Resolve(handle);ref var slot=ref _slots[index];
        var value=FollowAt(slot,clockTime);
        _sampleCount=0;slot.AnchorValue=slot.Value=value;slot.TargetValue=target;
        slot.Started=slot.Admitted=clockTime;
        slot.Playback=value==target?AnimationPlayback.Completed:AnimationPlayback.Playing;
        Schedule(index);
    }

    private static AnimationValue FollowAt(in Slot slot,PhysicalTime clockTime)
    {
        if(slot.Playback!=AnimationPlayback.Playing)return slot.Value;
        var elapsed=AnimationNumbers.Elapsed(clockTime,slot.Started);
        var decay=MathF.Exp(-slot.Follow!.Response.Value*elapsed);
        return AnimationValue.FromScalar(slot.TargetValue.Property,
            AnimationNumbers.Interpolate(slot.TargetValue.Number,slot.AnchorValue.Number,decay));
    }

    public void Start(AnimationHandle handle) => StartAt(handle, Clock(_slots[Resolve(handle)].Clock));

    /// <summary>Start at an admitted clock boundary, even between rendered frames.</summary>
    public void StartAt(AnimationHandle handle, PhysicalTime clockTime)
    {
        var index = Resolve(handle);
        if(_slots[index].Kind!=AnimationKind.Clip)throw new InvalidOperationException("Start requires a clip definition.");
        if (clockTime < Clock(_slots[index].Clock) || clockTime < _slots[index].Admitted)
            throw new ArgumentOutOfRangeException(nameof(clockTime));
        ref var slot = ref _slots[index];
        _sampleCount = 0;
        slot.Started = slot.Admitted = clockTime;
        slot.Offset = default; slot.Rate = new((float)1); slot.Endpoint = AnimationEndpoint.To;
        slot.Value = slot.Definition!.From;
        slot.Playback = AnimationPlayback.Playing;
        Schedule(index);
    }

    /// <summary>Restart a once-clip from its From value at an occurrence's clock time, which may already have passed (an
    /// occurrence observed late): the clip is at the phase it would have reached had it started then, and at To once
    /// that time plus its duration has passed. A future occurrence is admitted at its own time, as <see cref="StartAt"/>.</summary>
    public void StartAtOccurrence(AnimationHandle handle, PhysicalTime occurredAt)
    {
        var index = Resolve(handle);
        ref var slot = ref _slots[index];
        if (slot.Kind != AnimationKind.Clip) throw new InvalidOperationException("Start requires a clip definition.");
        if (slot.Definition!.Repeat != AnimationRepeat.Once) throw new InvalidOperationException("An occurrence starts a once-clip.");
        _sampleCount = 0;
        slot.Started = occurredAt;
        slot.Admitted = PhysicalTime.Max(PhysicalTime.Max(slot.Admitted, Clock(slot.Clock)), occurredAt);
        slot.Offset = default; slot.Rate = new((float)1); slot.Endpoint = AnimationEndpoint.To;
        slot.Value = slot.Definition.From;
        slot.Playback = AnimationPlayback.Playing;
        Schedule(index);
    }

    /// <summary>Drive a once-clip toward either endpoint without resetting its phase.
    /// A reversal evaluates the control boundary even if no frame was rendered.</summary>
    public void DriveTo(AnimationHandle handle, AnimationEndpoint endpoint, PhysicalTime clockTime)
    {
        var index = Resolve(handle);
        ref var slot = ref _slots[index];
        var definition = slot.Definition!;
        if(slot.Kind!=AnimationKind.Clip)throw new InvalidOperationException("Endpoint drive requires a clip definition.");
        if (!Enum.IsDefined(endpoint)) throw new ArgumentOutOfRangeException(nameof(endpoint));
        if (definition.Repeat != AnimationRepeat.Once)
            throw new InvalidOperationException("Endpoint drive requires a once-clip.");
        if (clockTime < Clock(definition.Clock) || clockTime < slot.Admitted)
            throw new ArgumentOutOfRangeException(nameof(clockTime));
        var position = AnimationPhase.Capture(Position(slot, clockTime),definition.Repeat);
        var value = definition.Evaluate(position.Value, position.Value >= 1);
        _sampleCount = 0;
        slot.Offset = position;
        slot.Started = slot.Admitted = clockTime;
        slot.Endpoint = endpoint;
        slot.Value = value;
        slot.Playback = AtEndpoint(slot, position.Value) ? AnimationPlayback.Completed : AnimationPlayback.Playing;
        Schedule(index);
    }

    /// <summary>Signed clip-seconds per clock-second for a Loop. Zero holds phase.
    /// Changes integrate the old rate to the admitted boundary, including skipped frames.</summary>
    public void SetLoopRate(AnimationHandle handle, AnimationPlaybackRate rate, PhysicalTime clockTime)
    {
        var index = Resolve(handle);
        ref var slot = ref _slots[index];
        var definition = slot.Definition!;
        if(slot.Kind!=AnimationKind.Clip)throw new InvalidOperationException("Rate control requires a clip definition.");
        if (definition.Repeat != AnimationRepeat.Loop)
            throw new InvalidOperationException("Rate control requires a loop.");
        AnimationNumbers.Finite(rate.Value);
        if (clockTime < Clock(definition.Clock) || clockTime < slot.Admitted)
            throw new ArgumentOutOfRangeException(nameof(clockTime));
        var position = AnimationPhase.Capture(Position(slot, clockTime),definition.Repeat);
        var value = definition.Evaluate(position.Value,false);
        _sampleCount = 0;
        slot.Offset = position; slot.Rate = rate;
        slot.Started = slot.Admitted = clockTime;
        slot.Value = value;
        slot.Playback = rate.Value == (float)0 ? AnimationPlayback.Stopped : AnimationPlayback.Playing;
        Schedule(index);
    }

    private static float Position(in Slot slot, PhysicalTime clockTime)
    {
        var offset = slot.Offset.Value;
        if (slot.Playback != AnimationPlayback.Playing) return offset;
        var elapsed = AnimationNumbers.Elapsed(clockTime, slot.Started);
        var duration = slot.Definition!.Duration.Value;
        if (slot.Definition.Repeat == AnimationRepeat.Loop)
        {
            var phase = (offset + clockTime.PeriodicPhaseSince(slot.Started, duration, slot.Rate.Value)) % 1;
            return phase < 0 ? phase + 1 : phase;
        }
        if (slot.Definition.Repeat == AnimationRepeat.PingPong)
            return (offset + clockTime.PeriodicPhaseSince(slot.Started, duration, 1, 2)) % 2;
        var remaining = slot.Endpoint == AnimationEndpoint.To ? 1 - offset : offset;
        if (elapsed >= remaining * duration) return slot.Endpoint == AnimationEndpoint.To ? 1 : 0;
        return slot.Endpoint == AnimationEndpoint.To ? offset + elapsed / duration : offset - elapsed / duration;
    }

    private static bool AtEndpoint(in Slot slot, float position) =>
        slot.Definition!.Repeat == AnimationRepeat.Once &&
        (slot.Endpoint == AnimationEndpoint.To ? position >= 1 : position <= 0);

    public void Stop(AnimationHandle handle, AnimationStop policy, PhysicalTime clockTime)
    {
        var index = Resolve(handle);
        if (!Enum.IsDefined(policy)) throw new ArgumentOutOfRangeException(nameof(policy));
        ref var slot = ref _slots[index];
        var definition = slot.Definition!;
        if (clockTime < Clock(slot.Clock) || clockTime < slot.Admitted)
            throw new ArgumentOutOfRangeException(nameof(clockTime));
        if(slot.Kind==AnimationKind.Oscillation)
        {
            slot.Oscillation!.Stop(policy,clockTime);slot.Value=slot.Oscillation.Read(clockTime).Position;
            slot.Admitted=clockTime;slot.Playback=AnimationPlayback.Stopped;_sampleCount=0;Schedule(index);return;
        }
        if(slot.Kind==AnimationKind.ExponentialFollow)
        {
            var value=policy==AnimationStop.RestoreInitial?slot.Initial:FollowAt(slot,clockTime);
            _sampleCount=0;slot.Value=slot.AnchorValue=slot.TargetValue=value;
            slot.Started=slot.Admitted=clockTime;slot.Playback=AnimationPlayback.Stopped;Schedule(index);return;
        }
        if(slot.Kind==AnimationKind.Impulses)
            throw new InvalidOperationException("Impulse occurrences require explicit cancellation or removal.");
        var held = policy == AnimationStop.Hold && slot.Playback == AnimationPlayback.Playing
            ? AnimationPhase.Capture(Position(slot, clockTime),definition.Repeat) : slot.Offset;
        var heldValue = definition.Evaluate(held.Value,
            definition.Repeat == AnimationRepeat.Once && held.Value >= (float)1);
        _sampleCount = 0;
        if (policy == AnimationStop.RestoreInitial)
        {
            slot.Value = definition.From; slot.Offset = default;
        }
        else if (slot.Playback == AnimationPlayback.Playing)
        {
            slot.Offset = held;
            slot.Value = heldValue;
        }
        slot.Admitted = clockTime;
        slot.Playback = AnimationPlayback.Stopped;
        Schedule(index);
    }

    public void SetVisible(AnimationHandle handle, bool visible)
    {
        var index = Resolve(handle);
        _sampleCount = 0;
        _slots[index].Visible = visible;
        if (visible) Schedule(index);
    }

    public AnimationBinding Binding(AnimationHandle handle) => _slots[Resolve(handle)].Binding;

    /// <summary>Borrow the binding at an active batch position; valid until mutation.</summary>
    public AnimationBinding ScheduledBindingAt(int index)
    {
        if (index < 0 || index >= _activeCount) throw new ArgumentOutOfRangeException(nameof(index));
        return _slots[_active[index]].Binding;
    }

    public AnimationRead Read(AnimationHandle handle)
    {
        ref var slot = ref _slots[Resolve(handle)];
        return new(slot.Playback, slot.Value, slot.Visible);
    }

    public void Remove(AnimationHandle handle)
    {
        var index = Resolve(handle);
        _sampleCount = 0;
        Unschedule(index);
        _writers.Remove(_slots[index].Binding);
        _slots[index] = default;
        _free[_freeCount++] = index;
    }

    public AnimationFrameWork Advance(ulong frame, PhysicalTime presentationTime, PhysicalTime simulationTime)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(frame, _frame);
        if (presentationTime < _presentationTime ||
            simulationTime < _simulationTime)
            throw new ArgumentOutOfRangeException(nameof(presentationTime), "Animation clocks must be finite and monotone.");
        // Validate the whole active operation before changing clocks, output, slots or occurrences.
        for (var i = 0; i < _activeCount; i++)
        {
            ref var pending = ref _slots[_active[i]];
            var time = pending.Clock == AnimationClock.Presentation ? presentationTime : simulationTime;
            if (pending.Admitted > time) throw new ArgumentException("Frame precedes an admitted animation control.");
            if (pending.Playback != AnimationPlayback.Playing) continue;
            switch (pending.Kind)
            {
                case AnimationKind.Clip:
                    _ = Position(pending, time);
                    break;
                case AnimationKind.ExponentialFollow: _ = AnimationNumbers.Elapsed(time,pending.Started); break;
                case AnimationKind.Oscillation: pending.Oscillation!.ValidateTime(time); break;
                case AnimationKind.Impulses: pending.Impulses!.ValidateAdvance(time,pending.Visible); break;
                default: throw new InvalidOperationException("Unsupported animation kind.");
            }
        }
        _sampleCount = 0;
        _frame = frame; _presentationTime = presentationTime; _simulationTime = simulationTime;
        var evaluated = 0;
        var cursor = 0;
        while (cursor < _activeCount)
        {
            var index = _active[cursor];
            ref var slot = ref _slots[index];
            var definition = slot.Definition!;
            if(slot.Playback==AnimationPlayback.Playing&&slot.Kind==AnimationKind.Impulses)
            {
                var envelope=slot.Impulses!.Advance(Clock(slot.Clock),slot.Visible);
                var amount=envelope.Value;
                slot.Value=amount==0?slot.Initial:amount==1?slot.ImpulsePeak:
                    AnimationValue.FromScalar(slot.Initial.Property,
                        AnimationNumbers.Interpolate(slot.Initial.Number,slot.ImpulsePeak.Number,amount));
                if(slot.Visible)evaluated++;
                if(slot.Impulses.Count==0)slot.Playback=AnimationPlayback.Completed;
            }
            else if(slot.Playback==AnimationPlayback.Playing&&slot.Kind==AnimationKind.Oscillation)
            {
                if(slot.Visible)
                {
                    var state=slot.Oscillation!.Evaluate(Clock(slot.Clock),out var completed);slot.Value=state.Position;evaluated++;
                    if(completed)slot.Playback=AnimationPlayback.Completed;
                }
            }
            else if(slot.Playback==AnimationPlayback.Playing&&slot.Kind==AnimationKind.ExponentialFollow)
            {
                if(slot.Visible)
                {
                    slot.Value=FollowAt(slot,Clock(slot.Clock));evaluated++;
                    if(slot.Value==slot.TargetValue)slot.Playback=AnimationPlayback.Completed;
                }
            }
            else if (slot.Playback == AnimationPlayback.Playing)
            {
                var position = Position(slot, Clock(definition.Clock));
                var completed = AtEndpoint(slot, position);
                if (completed)
                {
                    slot.Offset = AnimationPhase.Capture(position,definition.Repeat);
                    slot.Playback = AnimationPlayback.Completed;
                }
                if (slot.Visible || completed)
                {
                    slot.Value = definition.Evaluate(position,
                        definition.Repeat == AnimationRepeat.Once && position >= 1);
                    evaluated++;
                }
            }
            if (slot.Visible && (!slot.HasPublished || slot.Value != slot.Published))
            {
                _samples[_sampleCount++] = new(slot.Binding, slot.Value);
                slot.Published = slot.Value;
                slot.HasPublished = true;
            }
            if (slot.Playback != AnimationPlayback.Playing) Unschedule(index);
            else cursor++;
        }
        return new(evaluated, _sampleCount, _activeCount, RegisteredCount);
    }

    /// <summary>Emit initial values before the adapter frees/rebinds targets. All old
    /// handles are invalid after this call; both clocks restart in the new generation.</summary>
    public void Reset()
    {
        var generation = new AnimationGeneration(checked(Generation.Value + 1));
        AnimationSample[] samples;
        Dictionary<AnimationBinding, int> writers;
        Slot[] slots; ulong[] slotGenerations; int[] active, free;
        try
        {
            samples = new AnimationSample[Math.Max(16, RegisteredCount)]; writers = new(16);
            slots = new Slot[16]; slotGenerations = new ulong[16]; active = new int[16]; free = new int[16];
            for (var index = 0; index < 16; index++) free[index] = 15 - index;
        }
        catch (OutOfMemoryException) { throw new AnimationStorageException(); }
        var count = 0;
        for (var i = 0; i < _slots.Length; i++)
            if (_slots[i].Definition is not null||_slots[i].Follow is not null||_slots[i].Impulses is not null||_slots[i].Oscillation is not null)
                samples[count++] = new(_slots[i].Binding, _slots[i].Initial);
        _samples = samples; _sampleCount = count;
        _slots = slots; _versions = slotGenerations; _active = active; _free = free; _writers = writers;
        _activeCount = 0; _freeCount = 16;
        _frame = 0; _presentationTime = _simulationTime = default;
        Generation = generation;
    }

    private PhysicalTime Clock(AnimationClock clock) => clock switch
    {
        AnimationClock.Presentation => _presentationTime,
        AnimationClock.Simulation => _simulationTime,
        _ => throw new InvalidOperationException("Unsupported animation clock.")
    };
    private int Resolve(AnimationHandle handle)
    {
        if (!ReferenceEquals(handle.Owner, this) || handle.Generation != Generation || handle.Slot < 0 || handle.Slot >= _slots.Length ||
            (_slots[handle.Slot].Definition is null&&_slots[handle.Slot].Follow is null&&_slots[handle.Slot].Impulses is null&&_slots[handle.Slot].Oscillation is null) || handle.Version != _versions[handle.Slot])
            throw new ArgumentException("Animation handle is stale or not owned by this registry.", nameof(handle));
        return handle.Slot;
    }
    private void Schedule(int index)
    {
        ref var slot = ref _slots[index];
        if (slot.ActiveIndex >= 0) return;
        slot.ActiveIndex = _activeCount;
        _active[_activeCount++] = index;
    }
    private void Unschedule(int index)
    {
        ref var slot = ref _slots[index];
        if (slot.ActiveIndex < 0) return;
        var last = _active[--_activeCount];
        _active[slot.ActiveIndex] = last;
        _slots[last].ActiveIndex = slot.ActiveIndex;
        slot.ActiveIndex = -1;
    }

}
