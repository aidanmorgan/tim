using CuriousContraptions.Abstractions;
using System;

namespace CuriousContraptions.Presentation;

/// <summary>Storage belongs to one entry in the central AnimationBatch registry.</summary>
internal sealed class AnimationImpulseState
{
    private enum OccurrencePhase { Pending, Playing }
    private struct Occurrence
    {
        public AnimationOccurrenceId Id;
        public PhysicalTime OccurredAt,Started;
        public AnimationStrength Strength;
        public bool HasPresented;
        public OccurrencePhase Phase;
    }
    private readonly AnimationImpulseDefinition _definition;
    private Occurrence[] _occurrences = new Occurrence[8];
    private int _count,_pending,_highWaterMark,_evaluated;
    private ulong _accepted,_completed,_cancelled;
    private AnimationOccurrenceId _lastOccurrence;
    public int Count=>_count;
    public AnimationStrength Value { get; private set; }
    public AnimationImpulseState(AnimationImpulseDefinition definition)
    {
        _definition=definition;
    }
    public AnimationImpulseRead Read()=>new(_pending,_count-_pending,_accepted,_completed,_cancelled,_highWaterMark,_evaluated,_occurrences.Length);
    public void ValidateAdmission(AnimationOccurrenceId id,AnimationStrength strength)
    {
        if(id.Value==0||id.Value<=_lastOccurrence.Value)throw new ArgumentException("Impulse identities must increase within their binding.");
        AnimationNumbers.Unit(strength.Value);
        if(strength.Value<=(float)0)throw new ArgumentOutOfRangeException(nameof(strength));
        if(_accepted==ulong.MaxValue)throw new InvalidOperationException("Impulse occurrence counter exhausted.");
    }
    public void Reserve(int additional)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(additional);
        if ((ulong)additional > ulong.MaxValue - _accepted) throw new OverflowException("Impulse occurrence counter exhausted.");
        var required = (long)_count + additional;
        if (required > Array.MaxLength) throw new AnimationStorageException();
        if (required <= 3L * _occurrences.Length / 4) return;
        var capacity = (int)Math.Min(Array.MaxLength, Math.Max((4L * required + 2) / 3, (long)_occurrences.Length * 2));
        if (capacity == _occurrences.Length) return;
        try
        {
            var candidate = new Occurrence[capacity];
            Array.Copy(_occurrences, candidate, _count);
            _occurrences = candidate;
        }
        catch (OutOfMemoryException) { throw new AnimationStorageException(); }
    }
    public void Enqueue(AnimationOccurrenceId id,AnimationStrength strength,PhysicalTime occurredAt)
    {
        ValidateAdmission(id,strength);
        Reserve(1);
        _occurrences[_count++]=new(){Id=id,Strength=strength,OccurredAt=occurredAt,Phase=OccurrencePhase.Pending};
        _lastOccurrence=id;_pending++;_accepted++;_highWaterMark=Math.Max(_highWaterMark,_count);
    }
    public void ValidateAdvance(PhysicalTime time,bool visible)
    {
        for(var i=0;i<_count;i++)
        {
            ref var occurrence=ref _occurrences[i];
            if(occurrence.Phase==OccurrencePhase.Pending && (time<occurrence.OccurredAt ||
                (!visible&&_definition.Visibility==AnimationImpulseVisibility.DeferUntilVisible)))continue;
            var started=occurrence.Phase==OccurrencePhase.Playing?occurrence.Started:
                _definition.Timing==AnimationImpulseTiming.FirstPresentation?time:occurrence.OccurredAt;
            _=AnimationNumbers.Elapsed(time,started);
        }
    }
    public AnimationStrength Advance(PhysicalTime time,bool visible)
    {
        var retained=0;var pending=0;var value=0.0f;_evaluated=0;
        for(var i=0;i<_count;i++)
        {
            var occurrence=_occurrences[i];
            if(occurrence.Phase==OccurrencePhase.Pending)
            {
                if(time<occurrence.OccurredAt||(!visible&&_definition.Visibility==AnimationImpulseVisibility.DeferUntilVisible))
                {
                    _occurrences[retained++]=occurrence;pending++;continue;
                }
                occurrence.Phase=OccurrencePhase.Playing;
                occurrence.Started=_definition.Timing switch
                {
                    AnimationImpulseTiming.FirstPresentation=>time,
                    AnimationImpulseTiming.EventTime or AnimationImpulseTiming.EventTimeRetainFirstFrame=>occurrence.OccurredAt,
                    _=>throw new InvalidOperationException("Unsupported impulse timing.")
                };
            }
            var elapsed=AnimationNumbers.Elapsed(time,occurrence.Started);
            var retainedFirstFrame=elapsed>=_definition.Duration.Value&&visible&&!occurrence.HasPresented&&
                _definition.Timing==AnimationImpulseTiming.EventTimeRetainFirstFrame;
            var retainedUnseen= !occurrence.HasPresented&&
                _definition.Timing==AnimationImpulseTiming.EventTimeRetainFirstFrame&&
                _definition.Visibility==AnimationImpulseVisibility.DeferUntilVisible;
            if(elapsed>=_definition.Duration.Value&&!retainedFirstFrame&&!retainedUnseen){_completed++;continue;}
            if(visible)
            {
                var contribution=occurrence.Strength.Value*(retainedFirstFrame?1:_definition.Envelope(elapsed));
                // Logical canonical envelope observation, not a renderer-delta acknowledgement.
                if((float)contribution>(float)0)occurrence.HasPresented=true;
                value=_definition.Overlap switch
                {
                    AnimationImpulseOverlap.Maximum=>Math.Max(value,contribution),
                    AnimationImpulseOverlap.SaturatingSum=>Math.Min(1,value+contribution),
                    AnimationImpulseOverlap.HyperbolicTangentSum=>value+contribution,
                    _=>throw new InvalidOperationException("Unsupported impulse overlap.")
                };
                _evaluated++;
            }
            _occurrences[retained++]=occurrence;
        }
        Array.Clear(_occurrences,retained,_count-retained);
        _count=retained;_pending=pending;
        if(_definition.Overlap==AnimationImpulseOverlap.HyperbolicTangentSum)value=MathF.Tanh(value);
        if(visible||_count==0)Value=new((float)value);
        return Value;
    }
    public void Cancel()
    {
        _cancelled+=(ulong)_count;Array.Clear(_occurrences,0,_count);
        _count=_pending=_evaluated=0;Value=default;
    }
}
