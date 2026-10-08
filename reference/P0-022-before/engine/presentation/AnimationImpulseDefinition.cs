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
    public double Duration { get; }
    public double PeakPhase { get; }
    public AnimationImpulseCurve Curve { get; }
    public AnimationImpulseOverlap Overlap { get; }
    public AnimationImpulseVisibility Visibility { get; }
    public AnimationImpulseTiming Timing { get; }
    public AnimationClock Clock { get; }
    public int Capacity { get; }
    public AnimationImpulseDefinition(double duration,AnimationImpulseCurve curve,AnimationImpulseOverlap overlap,
        AnimationImpulseVisibility visibility,AnimationClock clock,int capacity,AnimationImpulseTiming timing,double peakPhase)
    {
        if(!double.IsFinite(duration)||duration<=0)throw new ArgumentOutOfRangeException(nameof(duration));
        if(!Enum.IsDefined(curve))throw new ArgumentOutOfRangeException(nameof(curve));
        if(!Enum.IsDefined(overlap))throw new ArgumentOutOfRangeException(nameof(overlap));
        if(!Enum.IsDefined(visibility))throw new ArgumentOutOfRangeException(nameof(visibility));
        if(!Enum.IsDefined(timing))throw new ArgumentOutOfRangeException(nameof(timing));
        if(!Enum.IsDefined(clock))throw new ArgumentOutOfRangeException(nameof(clock));
        if(capacity<1||capacity>MaximumCapacity)throw new ArgumentOutOfRangeException(nameof(capacity));
        if(!double.IsFinite(peakPhase)||(curve switch
        {
            AnimationImpulseCurve.LinearDecay or AnimationImpulseCurve.SmoothDecay=>peakPhase!=0,
            AnimationImpulseCurve.SineSquaredPulse=>peakPhase!=.5,
            AnimationImpulseCurve.SmoothRiseFall=>peakPhase<1e-6||peakPhase>1-1e-6,
            _=>true
        }))throw new ArgumentOutOfRangeException(nameof(peakPhase));
        PeakPhase=peakPhase;
        Duration=duration;Curve=curve;Overlap=overlap;Visibility=visibility;Clock=clock;Capacity=capacity;Timing=timing;
    }
    internal double Envelope(double elapsed)
    {
        if(elapsed<=0)return PeakPhase==0?1:0;
        if(elapsed>=Duration)return 0;
        var phase=elapsed/Duration;
        return Curve switch
        {
            AnimationImpulseCurve.LinearDecay=>1-phase,
            AnimationImpulseCurve.SmoothDecay=>Smooth(1-phase),
            AnimationImpulseCurve.SineSquaredPulse=>SquaredSine(phase),
            AnimationImpulseCurve.SmoothRiseFall=>phase<=PeakPhase?Smooth(phase/PeakPhase):Smooth((1-phase)/(1-PeakPhase)),
            _=>throw new InvalidOperationException("Unsupported impulse curve.")
        };
    }
    private static double SquaredSine(double phase)
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
        public double OccurredAt,Started,Strength;
        public bool HasPresented;
        public OccurrencePhase Phase;
    }
    private readonly AnimationImpulseDefinition _definition;
    private readonly Occurrence[] _occurrences;
    private int _count,_pending,_highWaterMark,_evaluated;
    private ulong _accepted,_completed,_cancelled;
    private AnimationOccurrenceId _lastOccurrence;
    public int Count=>_count;
    public double Value { get; private set; }
    public AnimationImpulseState(AnimationImpulseDefinition definition)
    {
        _definition=definition;_occurrences=new Occurrence[definition.Capacity];
    }
    public AnimationImpulseRead Read()=>new(_pending,_count-_pending,_accepted,_completed,_cancelled,_highWaterMark,_evaluated);
    public AnimationImpulseAdmission ValidateAdmission(AnimationOccurrenceId id,double strength,double occurredAt)
    {
        if(id.Value==0||id.Value<=_lastOccurrence.Value)throw new ArgumentException("Impulse identities must increase within their binding.");
        if(!double.IsFinite(strength)||strength<=0||strength>1)throw new ArgumentOutOfRangeException(nameof(strength));
        if(!double.IsFinite(occurredAt))throw new ArgumentOutOfRangeException(nameof(occurredAt));
        if(_count==_occurrences.Length)return AnimationImpulseAdmission.CapacityExhausted;
        return AnimationImpulseAdmission.Accepted;
    }
    public AnimationImpulseAdmission Enqueue(AnimationOccurrenceId id,double strength,double occurredAt)
    {
        var admission=ValidateAdmission(id,strength,occurredAt);
        if(admission!=AnimationImpulseAdmission.Accepted)return admission;
        _occurrences[_count++]=new(){Id=id,Strength=strength,OccurredAt=occurredAt,Phase=OccurrencePhase.Pending};
        _lastOccurrence=id;_pending++;_accepted++;_highWaterMark=Math.Max(_highWaterMark,_count);
        return AnimationImpulseAdmission.Accepted;
    }
    public double Advance(double time,bool visible)
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
            var elapsed=time-occurrence.Started;
            var retainedFirstFrame=elapsed>=_definition.Duration&&visible&&!occurrence.HasPresented&&
                _definition.Timing==AnimationImpulseTiming.EventTimeRetainFirstFrame;
            var retainedUnseen= !occurrence.HasPresented&&
                _definition.Timing==AnimationImpulseTiming.EventTimeRetainFirstFrame&&
                _definition.Visibility==AnimationImpulseVisibility.DeferUntilVisible;
            if(elapsed>=_definition.Duration&&!retainedFirstFrame&&!retainedUnseen){_completed++;continue;}
            if(visible)
            {
                var contribution=occurrence.Strength*(retainedFirstFrame?1:_definition.Envelope(elapsed));
                if(contribution>0)occurrence.HasPresented=true;
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
        if(visible||_count==0)Value=value;
        return Value;
    }
    public void Cancel()
    {
        _cancelled+=(ulong)_count;Array.Clear(_occurrences,0,_count);
        _count=_pending=_evaluated=0;Value=0;
    }
}
