using CuriousContraptions.Abstractions;
using System;

namespace CuriousContraptions.Presentation;

/// <summary>Analytic cosmetic underdamped motion, with canonical float anchors and no physical feedback.</summary>
public sealed record AnimationOscillationDefinition
{
    public AnimationDecayPerSecond Decay { get; }
    public AnimationFrequencyRadiansPerSecond Frequency { get; }
    public AnimationVelocity VelocityPerStrength { get; }
    public AnimationClock Clock { get; }
    public AnimationOscillationDefinition(AnimationDecayPerSecond decay,
        AnimationFrequencyRadiansPerSecond frequency,AnimationVelocity velocityPerStrength,AnimationClock clock)
    {
        AnimationNumbers.OscillationCoefficient(decay.Value);
        AnimationNumbers.OscillationCoefficient(frequency.Value);
        velocityPerStrength.Validate();
        if(velocityPerStrength.Number<=0)throw new ArgumentOutOfRangeException(nameof(velocityPerStrength));
        if(!Enum.IsDefined(clock))throw new ArgumentOutOfRangeException(nameof(clock));
        Decay=decay;Frequency=frequency;VelocityPerStrength=velocityPerStrength;Clock=clock;
    }
}
public readonly record struct AnimationOscillationRead(AnimationValue Position, AnimationVelocity Velocity,
    ulong Accepted, AnimationOccurrenceId LastOccurrence);

internal sealed class AnimationOscillationState
{
    private readonly AnimationOscillationDefinition _definition;
    private AnimationValue _position;
    private AnimationVelocity _velocity;
    private PhysicalTime _at;
    private bool _frozen;
    private ulong _accepted;
    private AnimationOccurrenceId _last;
    public AnimationOscillationState(AnimationOscillationDefinition definition)
    {
        _definition=definition;
        _position=AnimationValue.FromScalar(definition.VelocityPerStrength.Property,0);
        _velocity=AnimationVelocity.FromScalar(definition.VelocityPerStrength.Property,0);
    }
    public void ValidateTime(PhysicalTime time)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(time,_at);
        _=AnimationNumbers.Elapsed(time,_at);
    }
    public AnimationOscillationRead Read(PhysicalTime time) => Evaluate(time,out _);
    public AnimationOscillationRead Evaluate(PhysicalTime time,out bool completed)
    {
        ValidateTime(time);
        completed=_frozen||(_position.Number==0&&_velocity.Number==0);
        if(_frozen)return new(_position,AnimationVelocity.FromScalar(_velocity.Property,0),_accepted,_last);
        var elapsed=AnimationNumbers.Elapsed(time,_at);
        var decayRate=_definition.Decay.Value;
        var frequency=_definition.Frequency.Value;
        var decay=MathF.Exp(-decayRate*elapsed);
        if(decay==0)
        {
            completed=true;
            return new(AnimationValue.FromScalar(_position.Property,0),
                AnimationVelocity.FromScalar(_velocity.Property,0),_accepted,_last);
        }
        var phase=time.PeriodicPhaseSince(_at,MathF.Tau/frequency,1)*MathF.Tau;
        var cosine=MathF.Cos(phase);var sine=MathF.Sin(phase);
        var a=_position.Number;var b=(_velocity.Number+decayRate*a)/frequency;
        return new(AnimationValue.FromScalar(_position.Property,decay*(a*cosine+b*sine)),
            AnimationVelocity.FromScalar(_velocity.Property,decay*((frequency*b-decayRate*a)*cosine+
                (-frequency*a-decayRate*b)*sine)),_accepted,_last);
    }
    private AnimationOscillationRead Candidate(AnimationOccurrenceId occurrence,AnimationSignedStrength strength,PhysicalTime time)
    {
        if(occurrence.Value==0||occurrence.Value<=_last.Value)
            throw new ArgumentException("Oscillation occurrences must increase within their binding.");
        if(_accepted==ulong.MaxValue)throw new InvalidOperationException("Oscillation occurrence counter exhausted.");
        AnimationNumbers.SignedUnit(strength.Value);
        if(strength.Value==(float)0)throw new ArgumentOutOfRangeException(nameof(strength));
        var state=Read(time);
        // Round BOTH new anchors first. The future envelope belongs to these exact stored bits.
        var position=state.Position;
        var velocity=AnimationVelocity.FromScalar(_velocity.Property,
            state.Velocity.Number+_definition.VelocityPerStrength.Number*strength.Value);
        ValidateEnvelope(position,velocity);
        return new(position,velocity,_accepted+1,occurrence);
    }
    private void ValidateEnvelope(AnimationValue position,AnimationVelocity velocity)
    {
        var decay=_definition.Decay.Value;var frequency=_definition.Frequency.Value;
        var a=position.Number;var b=(velocity.Number+decay*a)/frequency;
        var c=frequency*b-decay*a;var d=-frequency*a-decay*b;
        // Finite float displacement/velocity profile; no amplitude clipping or wider retained anchor.
        if(!float.IsFinite(float.Hypot(a,b))||!float.IsFinite(float.Hypot(c,d)))
            throw new ArgumentOutOfRangeException(nameof(velocity),"Canonical oscillation anchors exceed the supported envelope.");
    }
    public void ValidateKick(AnimationOccurrenceId occurrence,AnimationSignedStrength strength,PhysicalTime time)
        => _=Candidate(occurrence,strength,time);
    public void Kick(AnimationOccurrenceId occurrence,AnimationSignedStrength strength,PhysicalTime time)
    {
        var candidate=Candidate(occurrence,strength,time);
        _position=candidate.Position;_velocity=candidate.Velocity;
        _at=time;_frozen=false;_last=occurrence;_accepted=candidate.Accepted;
    }
    public void Stop(AnimationStop policy,PhysicalTime time)
    {
        var state=Read(time);
        _position=policy==AnimationStop.Hold?state.Position:AnimationValue.FromScalar(_position.Property,0);
        _velocity=AnimationVelocity.FromScalar(_velocity.Property,0);_at=time;_frozen=true;
    }
}
