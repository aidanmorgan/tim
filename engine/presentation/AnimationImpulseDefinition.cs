using System;

namespace CuriousContraptions.Presentation;

public enum AnimationImpulseCurve { LinearDecay, SmoothDecay, SineSquaredPulse, SmoothRiseFall }
public enum AnimationImpulseOverlap { Maximum, SaturatingSum, HyperbolicTangentSum }
public enum AnimationImpulseVisibility { DeferUntilVisible, AdvanceWhileHidden }
public enum AnimationImpulseTiming { EventTime, FirstPresentation, EventTimeRetainFirstFrame }
public enum AnimationImpulseAdmission { Accepted, CapacityExhausted }
public readonly record struct AnimationOccurrenceId
{
    public ulong Value { get; }
    public AnimationOccurrenceId(ulong value)
    {
        if(value==0)throw new ArgumentOutOfRangeException(nameof(value));
        Value=value;
    }
}
public readonly record struct AnimationImpulseRead(int Pending,int Playing,ulong Accepted,
    ulong Completed,ulong Cancelled,int HighWaterMark,int EvaluatedLastFrame);

/// <summary>Normalized independent impulse envelopes. Each accepted occurrence owns a slot until
/// completion or explicit cancellation. Overlap combines values, never occurrence identities.</summary>
public sealed record AnimationImpulseDefinition
{
    public const int MaximumCapacity=65536;
    public AnimationDurationSeconds Duration { get; }
    public AnimationPeakPhase PeakPhase { get; }
    public AnimationImpulseCurve Curve { get; }
    public AnimationImpulseOverlap Overlap { get; }
    public AnimationImpulseVisibility Visibility { get; }
    public AnimationImpulseTiming Timing { get; }
    public AnimationClock Clock { get; }
    public int Capacity { get; }
    public AnimationImpulseDefinition(AnimationDurationSeconds duration,AnimationImpulseCurve curve,AnimationImpulseOverlap overlap,
        AnimationImpulseVisibility visibility,AnimationClock clock,int capacity,AnimationImpulseTiming timing,AnimationPeakPhase peakPhase)
    {
        AnimationNumbers.Positive(duration.Value);
        if(!Enum.IsDefined(curve))throw new ArgumentOutOfRangeException(nameof(curve));
        if(!Enum.IsDefined(overlap))throw new ArgumentOutOfRangeException(nameof(overlap));
        if(!Enum.IsDefined(visibility))throw new ArgumentOutOfRangeException(nameof(visibility));
        if(!Enum.IsDefined(timing))throw new ArgumentOutOfRangeException(nameof(timing));
        if(!Enum.IsDefined(clock))throw new ArgumentOutOfRangeException(nameof(clock));
        if(capacity<1||capacity>MaximumCapacity)throw new ArgumentOutOfRangeException(nameof(capacity));
        AnimationNumbers.Unit(peakPhase.Value);
        if(curve switch
        {
            AnimationImpulseCurve.LinearDecay or AnimationImpulseCurve.SmoothDecay=>peakPhase.Value!=(Half)0,
            AnimationImpulseCurve.SineSquaredPulse=>peakPhase.Value!=(Half).5,
            AnimationImpulseCurve.SmoothRiseFall=>(double)peakPhase.Value<1e-6||(double)peakPhase.Value>1-1e-6,
            _=>true
        })throw new ArgumentOutOfRangeException(nameof(peakPhase));
        PeakPhase=peakPhase;
        Duration=duration;Curve=curve;Overlap=overlap;Visibility=visibility;Clock=clock;Capacity=capacity;Timing=timing;
    }
    /// <summary>The one peak phase each curve admits; declarations and the worker share this table.</summary>
    public static AnimationPeakPhase CanonicalPeak(AnimationImpulseCurve curve)=>curve switch
    {
        AnimationImpulseCurve.LinearDecay or AnimationImpulseCurve.SmoothDecay=>new((Half)0),
        AnimationImpulseCurve.SineSquaredPulse or AnimationImpulseCurve.SmoothRiseFall=>new((Half).5),
        _=>throw new ArgumentOutOfRangeException(nameof(curve))
    };
    internal double Envelope(double elapsed)
    {
        if(elapsed<=0)return PeakPhase.Value==(Half)0?1:0;
        if(elapsed>=(double)Duration.Value)return 0;
        var phase=elapsed/(double)Duration.Value;
        return Curve switch
        {
            AnimationImpulseCurve.LinearDecay=>1-phase,
            AnimationImpulseCurve.SmoothDecay=>Smooth(1-phase),
            AnimationImpulseCurve.SineSquaredPulse=>SquaredSine(phase),
            AnimationImpulseCurve.SmoothRiseFall=>phase<=(double)PeakPhase.Value?Smooth(phase/(double)PeakPhase.Value):Smooth((1-phase)/(1-(double)PeakPhase.Value)),
            _=>throw new InvalidOperationException("Unsupported impulse curve.")
        };
    }
    internal static double SquaredSine(double phase)
    {
        var sine=Math.Sin(Math.PI*phase);return sine*sine;
    }
    private static double Smooth(double phase)
    {
        var p=Math.Min(phase,1-phase);
        var smooth=p*p*p*(p*(p*6-15)+10);
        return phase<=.5?smooth:1-smooth;
    }
}

/// <summary>Storage belongs to one entry in the central AnimationBatch registry.</summary>
internal sealed class AnimationImpulseState
{
    private enum OccurrencePhase { Pending, Playing }
    private struct Occurrence
    {
        public AnimationOccurrenceId Id;
        public double OccurredAt,Started; // External clock timestamps.
        public AnimationStrength Strength;
        public bool HasPresented;
        public OccurrencePhase Phase;
    }
    private readonly AnimationImpulseDefinition _definition;
    private readonly Occurrence[] _occurrences;
    private int _count,_pending,_highWaterMark,_evaluated;
    private ulong _accepted,_completed,_cancelled;
    private AnimationOccurrenceId _lastOccurrence;
    public int Count=>_count;
    public AnimationStrength Value { get; private set; }
    public AnimationImpulseState(AnimationImpulseDefinition definition)
    {
        _definition=definition;_occurrences=new Occurrence[definition.Capacity];
    }
    public AnimationImpulseRead Read()=>new(_pending,_count-_pending,_accepted,_completed,_cancelled,_highWaterMark,_evaluated);
    public AnimationImpulseAdmission ValidateAdmission(AnimationOccurrenceId id,AnimationStrength strength,double occurredAt)
    {
        if(id.Value==0||id.Value<=_lastOccurrence.Value)throw new ArgumentException("Impulse identities must increase within their binding.");
        AnimationNumbers.Unit(strength.Value);
        if(strength.Value<=(Half)0)throw new ArgumentOutOfRangeException(nameof(strength));
        if(_accepted==ulong.MaxValue)throw new InvalidOperationException("Impulse occurrence counter exhausted.");
        if(!double.IsFinite(occurredAt))throw new ArgumentOutOfRangeException(nameof(occurredAt));
        if(_count==_occurrences.Length)return AnimationImpulseAdmission.CapacityExhausted;
        return AnimationImpulseAdmission.Accepted;
    }
    public AnimationImpulseAdmission Enqueue(AnimationOccurrenceId id,AnimationStrength strength,double occurredAt)
    {
        var admission=ValidateAdmission(id,strength,occurredAt);
        if(admission!=AnimationImpulseAdmission.Accepted)return admission;
        _occurrences[_count++]=new(){Id=id,Strength=strength,OccurredAt=occurredAt,Phase=OccurrencePhase.Pending};
        _lastOccurrence=id;_pending++;_accepted++;_highWaterMark=Math.Max(_highWaterMark,_count);
        return AnimationImpulseAdmission.Accepted;
    }
    public void ValidateAdvance(double time,bool visible)
    {
        if(!double.IsFinite(time))throw new ArgumentOutOfRangeException(nameof(time));
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
    public AnimationStrength Advance(double time,bool visible)
    {
        var retained=0;var pending=0;var value=0.0;_evaluated=0;
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
            var retainedFirstFrame=elapsed>=(double)_definition.Duration.Value&&visible&&!occurrence.HasPresented&&
                _definition.Timing==AnimationImpulseTiming.EventTimeRetainFirstFrame;
            var retainedUnseen= !occurrence.HasPresented&&
                _definition.Timing==AnimationImpulseTiming.EventTimeRetainFirstFrame&&
                _definition.Visibility==AnimationImpulseVisibility.DeferUntilVisible;
            if(elapsed>=(double)_definition.Duration.Value&&!retainedFirstFrame&&!retainedUnseen){_completed++;continue;}
            if(visible)
            {
                var contribution=(double)occurrence.Strength.Value*(retainedFirstFrame?1:_definition.Envelope(elapsed));
                // Logical canonical envelope observation, not a renderer-delta acknowledgement.
                if((Half)contribution>(Half)0)occurrence.HasPresented=true;
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
        if(_definition.Overlap==AnimationImpulseOverlap.HyperbolicTangentSum)value=Math.Tanh(value);
        if(visible||_count==0)Value=new((Half)value);
        return Value;
    }
    public void Cancel()
    {
        _cancelled+=(ulong)_count;Array.Clear(_occurrences,0,_count);
        _count=_pending=_evaluated=0;Value=default;
    }
}
