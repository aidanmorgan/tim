using System;
using System.Collections.Generic;

namespace CuriousContraptions.Presentation;

public enum AnimationProperty { LocalRotationAngle, LocalTranslation, Opacity, UniformScale, ColourBlend, ColourRed, ColourGreen, ColourBlue }
public enum AnimationCurve { Linear, SmoothStep, SinePulse }
public enum AnimationRepeat { Once, Loop, PingPong }
public enum AnimationClock { Presentation, Simulation }
public enum AnimationPlayback { Stopped, Playing, Completed }
public enum AnimationStop { Hold, RestoreInitial }
public enum AnimationEndpoint { From, To }

public readonly record struct AnimationTargetId
{
    public int Index { get; }
    public AnimationTargetId(int index)
    {
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
        Index = index;
    }
}
public readonly record struct AnimationGeneration
{
    public ulong Value { get; }
    public AnimationGeneration(ulong value)
    {
        if (value == 0) throw new ArgumentOutOfRangeException(nameof(value));
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
public readonly record struct AnimationSample(AnimationBinding Binding, double Value);
public readonly record struct AnimationRead(AnimationPlayback Playback, double Value, bool Visible);
public readonly record struct AnimationFrameWork(int Evaluated, int DirtyWrites, int Scheduled, int Registered);

/// <summary>Reusable scalar clip/procedural definition. Units come from its bound property.</summary>
public sealed record AnimationDefinition
{
    public double From { get; }
    public double To { get; }
    public double Duration { get; }
    public AnimationCurve Curve { get; }
    public AnimationRepeat Repeat { get; }
    public AnimationClock Clock { get; }

    public AnimationDefinition(double from, double to, double duration, AnimationCurve curve,
        AnimationRepeat repeat, AnimationClock clock)
    {
        if (!double.IsFinite(from) || !double.IsFinite(to) || !double.IsFinite(to - from))
            throw new ArgumentOutOfRangeException(nameof(to));
        if (!double.IsFinite(duration) || duration <= 0)
            throw new ArgumentOutOfRangeException(nameof(duration));
        if (!Enum.IsDefined(curve)) throw new ArgumentOutOfRangeException(nameof(curve));
        if (!Enum.IsDefined(repeat)) throw new ArgumentOutOfRangeException(nameof(repeat));
        if (!Enum.IsDefined(clock)) throw new ArgumentOutOfRangeException(nameof(clock));
        if (repeat == AnimationRepeat.PingPong && !double.IsFinite(duration * 2))
            throw new ArgumentOutOfRangeException(nameof(duration));
        From = from; To = to; Duration = duration; Curve = curve; Repeat = repeat; Clock = clock;
    }

    internal double Evaluate(double elapsed, bool completed)
    {
        double phase;
        if (completed) phase = 1;
        else phase = Repeat switch
        {
            AnimationRepeat.Once => elapsed / Duration,
            AnimationRepeat.Loop => elapsed % Duration / Duration,
            AnimationRepeat.PingPong => 1 - Math.Abs(elapsed % (2 * Duration) / Duration - 1),
            _ => throw new InvalidOperationException("Unsupported animation repetition.")
        };
        if (phase == 0) return From;
        if (phase == 1) return Curve == AnimationCurve.SinePulse ? From : To;
        var p = Math.Min(phase, 1 - phase);
        var smooth = p * p * p * (p * (p * 6 - 15) + 10);
        var amount = Curve switch
        {
            AnimationCurve.Linear => phase,
            AnimationCurve.SmoothStep => phase <= .5 ? smooth : 1 - smooth,
            AnimationCurve.SinePulse => Math.Sin(Math.PI * phase),
            _ => throw new InvalidOperationException("Unsupported animation curve.")
        };
        return Math.FusedMultiplyAdd(To - From, amount, From);
    }
}

/// <summary>Single-thread, fixed-capacity animation owner. Output is borrowed until the next
/// mutation. Registration allocates; stable-topology frame evaluation does not.</summary>
public sealed class AnimationBatch
{
    private enum AnimationKind { Clip, ExponentialFollow, Impulses, Oscillation }
    private struct Slot
    {
        public AnimationKind Kind;
        public AnimationClock Clock;
        public double Initial,AnchorValue,TargetValue,ImpulsePeak;
        public AnimationFollowDefinition? Follow;
        public AnimationImpulseState? Impulses;
        public AnimationOscillationState? Oscillation;
        public AnimationDefinition? Definition;
        public AnimationBinding Binding;
        public AnimationPlayback Playback;
        public double Started, Admitted, Value, Published, Offset, Rate;
        public AnimationEndpoint Endpoint;
        public bool Visible, HasPublished;
        public int ActiveIndex;
    }
    private readonly Slot[] _slots;
    private readonly ulong[] _versions;
    private readonly int[] _active, _free;
    private readonly AnimationSample[] _samples;
    private readonly Dictionary<AnimationBinding, int> _writers;
    private int _activeCount, _freeCount, _sampleCount;
    private ulong _frame;
    private double _presentationTime, _simulationTime;
    public AnimationGeneration Generation { get; private set; } = new(1);
    public int RegisteredCount => _slots.Length - _freeCount;
    public int ScheduledCount => _activeCount;
    public ReadOnlySpan<AnimationSample> Samples => _samples.AsSpan(0, _sampleCount);

    public AnimationBatch(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _slots = new Slot[capacity]; _versions = new ulong[capacity];
        _active = new int[capacity]; _free = new int[capacity]; _samples = new AnimationSample[capacity];
        _writers = new(capacity);
        FillFree();
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
        if(binding.Property is not (AnimationProperty.LocalRotationAngle or AnimationProperty.LocalTranslation))
            throw new ArgumentException("Oscillation requires a signed motion property.");
        return RegisterSlot(binding,new(){Kind=AnimationKind.Oscillation,Clock=definition.Clock,
            Oscillation=new(definition),Initial=0},0,0);
    }
    public void ValidateOscillationKick(AnimationHandle handle,AnimationOccurrenceId occurrence,double strength,double clockTime)
    {
        var index=Resolve(handle);ref var slot=ref _slots[index];
        if(slot.Kind!=AnimationKind.Oscillation)throw new InvalidOperationException("Kick requires an oscillation binding.");
        if(!double.IsFinite(clockTime)||clockTime<Clock(slot.Clock)||clockTime<slot.Admitted)
            throw new ArgumentOutOfRangeException(nameof(clockTime));
        slot.Oscillation!.ValidateKick(occurrence,strength,clockTime);
    }
    public void KickOscillation(AnimationHandle handle,AnimationOccurrenceId occurrence,double strength,double clockTime)
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

    public AnimationHandle RegisterImpulses(AnimationBinding binding,AnimationImpulseDefinition definition,double from,double to)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if(!double.IsFinite(from)||!double.IsFinite(to)||!double.IsFinite(to-from))
            throw new ArgumentOutOfRangeException(nameof(from));
        return RegisterSlot(binding,new(){Kind=AnimationKind.Impulses,Clock=definition.Clock,
            Impulses=new(definition),Initial=from,ImpulsePeak=to},from,to);
    }

    public AnimationImpulseAdmission ValidateImpulseAdmission(AnimationHandle handle,AnimationOccurrenceId occurrence,double strength,double occurredAt)
    {
        var index=Resolve(handle);ref var slot=ref _slots[index];
        if(slot.Kind!=AnimationKind.Impulses)throw new InvalidOperationException("Impulse admission requires an impulse definition.");
        return slot.Impulses!.ValidateAdmission(occurrence,strength,occurredAt);
    }

    public AnimationImpulseAdmission EnqueueImpulse(AnimationHandle handle,AnimationOccurrenceId occurrence,double strength,double occurredAt)
    {
        var index=Resolve(handle);ref var slot=ref _slots[index];
        if(slot.Kind!=AnimationKind.Impulses)throw new InvalidOperationException("Impulse admission requires an impulse definition.");
        var admission=slot.Impulses!.Enqueue(occurrence,strength,occurredAt);
        if(admission==AnimationImpulseAdmission.Accepted)
        {
            _sampleCount=0;slot.Playback=AnimationPlayback.Playing;Schedule(index);
        }
        return admission;
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

    private AnimationHandle RegisterSlot(AnimationBinding binding,Slot slot,double from,double to)
    {
        if(!Enum.IsDefined(binding.Property))throw new ArgumentOutOfRangeException(nameof(binding));
        if((binding.Property is AnimationProperty.Opacity or AnimationProperty.ColourBlend or AnimationProperty.ColourRed or AnimationProperty.ColourGreen or AnimationProperty.ColourBlue)&&
            (from<0||from>1||to<0||to>1)||binding.Property==AnimationProperty.UniformScale&&(from<=0||to<=0))
            throw new ArgumentException("Animation endpoints exceed the property's supported range.");
        if(_writers.ContainsKey(binding))throw new InvalidOperationException("Visual property already has an animation writer.");
        if(_freeCount==0)throw new InvalidOperationException("Animation registry capacity exceeded.");
        var index=_free[_freeCount-1];var version=checked(_versions[index]+1);
        _sampleCount=0;_freeCount--;_versions[index]=version;
        slot.Binding=binding;slot.Playback=AnimationPlayback.Stopped;slot.Value=slot.Initial;
        slot.Rate=1;slot.Admitted=Clock(slot.Clock);slot.Visible=true;slot.ActiveIndex=-1;
        _slots[index]=slot;_writers.Add(binding,index);Schedule(index);
        return new(this,Generation,index,version);
    }

    /// <summary>Retarget at an admitted clock boundary, including elapsed invisible/skipped frames.</summary>
    public void ValidateFollowValue(AnimationHandle handle,double target,double clockTime)
    {
        var index=Resolve(handle);ref var slot=ref _slots[index];
        if(slot.Kind!=AnimationKind.ExponentialFollow)throw new InvalidOperationException("Value following requires a follow definition.");
        var definition=slot.Follow!;
        if(!double.IsFinite(target)||target<Math.Min(definition.From,definition.To)||target>Math.Max(definition.From,definition.To))
            throw new ArgumentOutOfRangeException(nameof(target));
        if(!double.IsFinite(clockTime)||clockTime<Clock(slot.Clock)||clockTime<slot.Admitted)
            throw new ArgumentOutOfRangeException(nameof(clockTime));
    }

    public void FollowValue(AnimationHandle handle,double target,double clockTime)
    {
        ValidateFollowValue(handle,target,clockTime);
        var index=Resolve(handle);ref var slot=ref _slots[index];
        var value=FollowAt(slot,clockTime);
        _sampleCount=0;slot.AnchorValue=slot.Value=value;slot.TargetValue=target;
        slot.Started=slot.Admitted=clockTime;
        slot.Playback=value==target?AnimationPlayback.Completed:AnimationPlayback.Playing;
        Schedule(index);
    }

    private static double FollowAt(in Slot slot,double clockTime)
    {
        if(slot.Playback!=AnimationPlayback.Playing)return slot.Value;
        var decay=Math.Exp(-slot.Follow!.Response*(clockTime-slot.Started));
        return Math.FusedMultiplyAdd(slot.AnchorValue-slot.TargetValue,decay,slot.TargetValue);
    }

    public void Start(AnimationHandle handle) => StartAt(handle, Clock(_slots[Resolve(handle)].Clock));

    /// <summary>Start at an admitted clock boundary, even between rendered frames.</summary>
    public void StartAt(AnimationHandle handle, double clockTime)
    {
        var index = Resolve(handle);
        if(_slots[index].Kind!=AnimationKind.Clip)throw new InvalidOperationException("Start requires a clip definition.");
        if (!double.IsFinite(clockTime) || clockTime < Clock(_slots[index].Clock) || clockTime < _slots[index].Admitted)
            throw new ArgumentOutOfRangeException(nameof(clockTime));
        ref var slot = ref _slots[index];
        _sampleCount = 0;
        slot.Started = slot.Admitted = clockTime;
        slot.Offset = 0; slot.Rate = 1; slot.Endpoint = AnimationEndpoint.To;
        slot.Value = slot.Definition!.From;
        slot.Playback = AnimationPlayback.Playing;
        Schedule(index);
    }

    /// <summary>Drive a once-clip toward either endpoint without resetting its phase.
    /// A reversal evaluates the control boundary even if no frame was rendered.</summary>
    public void DriveTo(AnimationHandle handle, AnimationEndpoint endpoint, double clockTime)
    {
        var index = Resolve(handle);
        ref var slot = ref _slots[index];
        var definition = slot.Definition!;
        if(slot.Kind!=AnimationKind.Clip)throw new InvalidOperationException("Endpoint drive requires a clip definition.");
        if (!Enum.IsDefined(endpoint)) throw new ArgumentOutOfRangeException(nameof(endpoint));
        if (definition.Repeat != AnimationRepeat.Once)
            throw new InvalidOperationException("Endpoint drive requires a once-clip.");
        if (!double.IsFinite(clockTime) || clockTime < Clock(definition.Clock) || clockTime < slot.Admitted)
            throw new ArgumentOutOfRangeException(nameof(clockTime));
        var position = Position(slot, clockTime);
        _sampleCount = 0;
        slot.Offset = position;
        slot.Started = slot.Admitted = clockTime;
        slot.Endpoint = endpoint;
        slot.Value = definition.Evaluate(position, position >= definition.Duration);
        slot.Playback = AtEndpoint(slot, position) ? AnimationPlayback.Completed : AnimationPlayback.Playing;
        Schedule(index);
    }

    /// <summary>Signed clip-seconds per clock-second for a Loop. Zero holds phase.
    /// Changes integrate the old rate to the admitted boundary, including skipped frames.</summary>
    public void SetLoopRate(AnimationHandle handle, double rate, double clockTime)
    {
        var index = Resolve(handle);
        ref var slot = ref _slots[index];
        var definition = slot.Definition!;
        if(slot.Kind!=AnimationKind.Clip)throw new InvalidOperationException("Rate control requires a clip definition.");
        if (definition.Repeat != AnimationRepeat.Loop)
            throw new InvalidOperationException("Rate control requires a loop.");
        if (!double.IsFinite(rate)) throw new ArgumentOutOfRangeException(nameof(rate));
        if (!double.IsFinite(clockTime) || clockTime < Clock(definition.Clock) || clockTime < slot.Admitted)
            throw new ArgumentOutOfRangeException(nameof(clockTime));
        var position = Position(slot, clockTime);
        _sampleCount = 0;
        slot.Offset = position; slot.Rate = rate;
        slot.Started = slot.Admitted = clockTime;
        slot.Value = definition.Evaluate(position, false);
        slot.Playback = rate == 0 ? AnimationPlayback.Stopped : AnimationPlayback.Playing;
        Schedule(index);
    }

    private static double Position(in Slot slot, double clockTime)
    {
        if (slot.Playback != AnimationPlayback.Playing) return slot.Offset;
        var elapsed = clockTime - slot.Started;
        if (slot.Definition!.Repeat == AnimationRepeat.Loop)
        {
            var travel = elapsed * slot.Rate;
            if (!double.IsFinite(travel))
                throw new ArgumentOutOfRangeException(nameof(clockTime), "Loop rate times elapsed clock exceeds the supported finite range.");
            var duration = slot.Definition.Duration;
            var delta = travel % duration;
            if (delta >= 0)
                return slot.Offset >= duration - delta ? slot.Offset - (duration - delta) : slot.Offset + delta;
            var reversed = slot.Offset + delta;
            return reversed < 0 ? reversed + duration : reversed;
        }
        var position = slot.Endpoint == AnimationEndpoint.To ? slot.Offset + elapsed : slot.Offset - elapsed;
        return slot.Definition!.Repeat == AnimationRepeat.Once
            ? Math.Clamp(position, 0, slot.Definition.Duration) : position;
    }

    private static bool AtEndpoint(in Slot slot, double position) =>
        slot.Definition!.Repeat == AnimationRepeat.Once &&
        (slot.Endpoint == AnimationEndpoint.To ? position >= slot.Definition.Duration : position <= 0);

    public void Stop(AnimationHandle handle, AnimationStop policy, double clockTime)
    {
        var index = Resolve(handle);
        if (!Enum.IsDefined(policy)) throw new ArgumentOutOfRangeException(nameof(policy));
        ref var slot = ref _slots[index];
        var definition = slot.Definition!;
        if (!double.IsFinite(clockTime) || clockTime < Clock(slot.Clock) || clockTime < slot.Admitted)
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
            ? Position(slot, clockTime) : slot.Offset;
        _sampleCount = 0;
        if (policy == AnimationStop.RestoreInitial)
        {
            slot.Value = definition.From; slot.Offset = 0;
        }
        else if (slot.Playback == AnimationPlayback.Playing)
        {
            slot.Offset = held;
            slot.Value = definition.Evaluate(slot.Offset,
                definition.Repeat == AnimationRepeat.Once && slot.Offset >= definition.Duration);
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

    public AnimationFrameWork Advance(ulong frame, double presentationTime, double simulationTime)
    {
        if (frame <= _frame) throw new ArgumentOutOfRangeException(nameof(frame));
        if (!double.IsFinite(presentationTime) || presentationTime < _presentationTime ||
            !double.IsFinite(simulationTime) || simulationTime < _simulationTime)
            throw new ArgumentOutOfRangeException(nameof(presentationTime), "Animation clocks must be finite and monotone.");
        for (var i = 0; i < _activeCount; i++)
        {
            ref var pending = ref _slots[_active[i]];
            var time = pending.Clock == AnimationClock.Presentation ? presentationTime : simulationTime;
            if (pending.Admitted > time)
                throw new ArgumentException("Frame precedes an admitted animation control.");
            if (pending.Playback == AnimationPlayback.Playing && pending.Kind==AnimationKind.Clip && pending.Definition!.Repeat == AnimationRepeat.Loop)
                _ = Position(pending, time); // Validate every numeric limit before changing frame/output state.
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
                slot.Value=envelope==0?slot.Initial:envelope==1?slot.ImpulsePeak:
                    slot.Initial+(slot.ImpulsePeak-slot.Initial)*envelope;
                if(slot.Visible)evaluated++;
                if(slot.Impulses.Count==0)slot.Playback=AnimationPlayback.Completed;
            }
            else if(slot.Playback==AnimationPlayback.Playing&&slot.Kind==AnimationKind.Oscillation)
            {
                if(slot.Visible)
                {
                    var state=slot.Oscillation!.Read(Clock(slot.Clock));slot.Value=state.Position;evaluated++;
                    if(state.Position==0&&state.Velocity==0)slot.Playback=AnimationPlayback.Completed;
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
                    slot.Offset = position;
                    slot.Playback = AnimationPlayback.Completed;
                }
                if (slot.Visible || completed)
                {
                    slot.Value = definition.Evaluate(position,
                        definition.Repeat == AnimationRepeat.Once && position >= definition.Duration);
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
        _sampleCount = 0;
        for (var i = 0; i < _slots.Length; i++)
            if (_slots[i].Definition is not null||_slots[i].Follow is not null||_slots[i].Impulses is not null||_slots[i].Oscillation is not null)
                _samples[_sampleCount++] = new(_slots[i].Binding, _slots[i].Initial);
        Array.Clear(_slots);
        Array.Clear(_versions);
        _writers.Clear();
        _activeCount = 0;
        FillFree();
        _frame = 0; _presentationTime = _simulationTime = 0;
        Generation = generation;
    }

    private double Clock(AnimationClock clock) => clock switch
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
    private void FillFree()
    {
        _freeCount = _slots.Length;
        for (var i = 0; i < _free.Length; i++) _free[i] = _free.Length - 1 - i;
    }
}
