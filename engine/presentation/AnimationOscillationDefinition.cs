using System;

namespace CuriousContraptions.Presentation;

/// <summary>Analytic cosmetic underdamped motion, with canonical Half anchors and no physical feedback.</summary>
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
    private double _at; // External clock timestamp of the canonical anchor.
    private bool _frozen;
    private ulong _accepted;
    private AnimationOccurrenceId _last;
    public AnimationOscillationState(AnimationOscillationDefinition definition)
    {
        _definition=definition;
        _position=AnimationValue.Narrow(definition.VelocityPerStrength.Property,0);
        _velocity=AnimationVelocity.Narrow(definition.VelocityPerStrength.Property,0);
    }
    public void ValidateTime(double time)
    {
        if(!double.IsFinite(time)||time<_at)throw new ArgumentOutOfRangeException(nameof(time));
        _=AnimationNumbers.Elapsed(time,_at);
    }
    public AnimationOscillationRead Read(double time) => Evaluate(time,out _);
    public AnimationOscillationRead Evaluate(double time,out bool completed)
    {
        ValidateTime(time);
        completed=_frozen||(_position.Number==0&&_velocity.Number==0);
        if(_frozen)return new(_position,AnimationVelocity.Narrow(_velocity.Property,0),_accepted,_last);
        var elapsed=AnimationNumbers.Elapsed(time,_at);
        var decayRate=(double)_definition.Decay.Value;
        var frequency=(double)_definition.Frequency.Value;
        var decay=Math.Exp(-decayRate*elapsed);
        if(decay==0)
        {
            completed=true;
            return new(AnimationValue.Narrow(_position.Property,0),
                AnimationVelocity.Narrow(_velocity.Property,0),_accepted,_last);
        }
        var phase=(elapsed%(Math.Tau/frequency))*frequency;
        var cosine=Math.Cos(phase);var sine=Math.Sin(phase);
        var a=_position.Number;var b=(_velocity.Number+decayRate*a)/frequency;
        return new(AnimationValue.Narrow(_position.Property,decay*(a*cosine+b*sine)),
            AnimationVelocity.Narrow(_velocity.Property,decay*((frequency*b-decayRate*a)*cosine+
                (-frequency*a-decayRate*b)*sine)),_accepted,_last);
    }
    private AnimationOscillationRead Candidate(AnimationOccurrenceId occurrence,AnimationSignedStrength strength,double time)
    {
        if(occurrence.Value==0||occurrence.Value<=_last.Value)
            throw new ArgumentException("Oscillation occurrences must increase within their binding.");
        if(_accepted==ulong.MaxValue)throw new InvalidOperationException("Oscillation occurrence counter exhausted.");
        AnimationNumbers.SignedUnit(strength.Value);
        if(strength.Value==(Half)0)throw new ArgumentOutOfRangeException(nameof(strength));
        var state=Read(time);
        // Round BOTH new anchors first. The future envelope belongs to these exact stored bits.
        var position=state.Position;
        var velocity=AnimationVelocity.Narrow(_velocity.Property,
            state.Velocity.Number+_definition.VelocityPerStrength.Number*(double)strength.Value);
        ValidateEnvelope(position,velocity);
        return new(position,velocity,_accepted+1,occurrence);
    }
    private void ValidateEnvelope(AnimationValue position,AnimationVelocity velocity)
    {
        var decay=(double)_definition.Decay.Value;var frequency=(double)_definition.Frequency.Value;
        var a=position.Number;var b=(velocity.Number+decay*a)/frequency;
        var c=frequency*b-decay*a;var d=-frequency*a-decay*b;
        // Finite Half displacement/velocity profile; no amplitude clipping or wider retained anchor.
        if(Math.Sqrt(a*a+b*b)>(double)Half.MaxValue||Math.Sqrt(c*c+d*d)>(double)Half.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(velocity),"Canonical oscillation anchors exceed the supported envelope.");
    }
    public void ValidateKick(AnimationOccurrenceId occurrence,AnimationSignedStrength strength,double time)
        => _=Candidate(occurrence,strength,time);
    public void Kick(AnimationOccurrenceId occurrence,AnimationSignedStrength strength,double time)
    {
        var candidate=Candidate(occurrence,strength,time);
        _position=candidate.Position;_velocity=candidate.Velocity;
        _at=time;_frozen=false;_last=occurrence;_accepted=candidate.Accepted;
    }
    public void Stop(AnimationStop policy,double time)
    {
        var state=Read(time);
        _position=policy==AnimationStop.Hold?state.Position:AnimationValue.Narrow(_position.Property,0);
        _velocity=AnimationVelocity.Narrow(_velocity.Property,0);_at=time;_frozen=true;
    }
}
