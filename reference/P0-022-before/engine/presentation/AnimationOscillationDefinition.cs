using System;

namespace CuriousContraptions.Presentation;

/// <summary>Cosmetic underdamped response: displacement and velocity decay analytically.
/// Frequency is the damped angular frequency; no mass, energy or physical feedback is owned.</summary>
public sealed record AnimationOscillationDefinition
{
    public const double MaximumMagnitude=1e6;
    public double Decay { get; }
    public double Frequency { get; }
    public double VelocityPerStrength { get; }
    public AnimationClock Clock { get; }
    public AnimationOscillationDefinition(double decay,double frequency,double velocityPerStrength,AnimationClock clock)
    {
        if(!double.IsFinite(decay)||decay<1e-6||decay>1e6)throw new ArgumentOutOfRangeException(nameof(decay));
        if(!double.IsFinite(frequency)||frequency<1e-6||frequency>1e6)throw new ArgumentOutOfRangeException(nameof(frequency));
        if(!double.IsFinite(velocityPerStrength)||velocityPerStrength<=0||velocityPerStrength>MaximumMagnitude)
            throw new ArgumentOutOfRangeException(nameof(velocityPerStrength));
        if(!Enum.IsDefined(clock))throw new ArgumentOutOfRangeException(nameof(clock));
        Decay=decay;Frequency=frequency;VelocityPerStrength=velocityPerStrength;Clock=clock;
    }
}
public readonly record struct AnimationOscillationRead(double Position,double Velocity,ulong Accepted,AnimationOccurrenceId LastOccurrence);

internal sealed class AnimationOscillationState(AnimationOscillationDefinition definition)
{
    private double _position,_velocity,_at;
    private bool _frozen;
    private ulong _accepted;
    private AnimationOccurrenceId _last;
    public AnimationOscillationRead Read(double time)
    {
        if(!double.IsFinite(time)||time<_at)throw new ArgumentOutOfRangeException(nameof(time));
        if(_frozen)return new(_position,0,_accepted,_last);
        var elapsed=time-_at;var decay=Math.Exp(-definition.Decay*elapsed);
        if(decay==0)return new(0,0,_accepted,_last);
        var phase=(elapsed%(Math.Tau/definition.Frequency))*definition.Frequency;
        var cosine=Math.Cos(phase);var sine=Math.Sin(phase);
        var a=_position;var b=(_velocity+definition.Decay*a)/definition.Frequency;
        return new(decay*(a*cosine+b*sine),
            decay*((definition.Frequency*b-definition.Decay*a)*cosine+
                (-definition.Frequency*a-definition.Decay*b)*sine),_accepted,_last);
    }
    public void ValidateKick(AnimationOccurrenceId occurrence,double strength,double time)
    {
        if(occurrence.Value==0||occurrence.Value<=_last.Value)
            throw new ArgumentException("Oscillation occurrences must increase within their binding.");
        if(_accepted==ulong.MaxValue)throw new InvalidOperationException("Oscillation occurrence counter exhausted.");
        if(!double.IsFinite(strength)||strength==0||Math.Abs(strength)>1)
            throw new ArgumentOutOfRangeException(nameof(strength));
        var state=Read(time);var velocity=state.Velocity+definition.VelocityPerStrength*strength;
        var a=state.Position;var b=(velocity+definition.Decay*a)/definition.Frequency;
        var c=definition.Frequency*b-definition.Decay*a;
        var d=-definition.Frequency*a-definition.Decay*b;
        // Bound both sinusoidal envelopes before admitting a kick; no amplitude clipping.
        if(Math.Sqrt(a*a+b*b)>AnimationOscillationDefinition.MaximumMagnitude||
            Math.Sqrt(c*c+d*d)>AnimationOscillationDefinition.MaximumMagnitude)
            throw new ArgumentOutOfRangeException(nameof(strength),"Oscillation exceeds its supported displacement/velocity envelope.");
    }
    public void Kick(AnimationOccurrenceId occurrence,double strength,double time)
    {
        ValidateKick(occurrence,strength,time);var state=Read(time);
        _position=state.Position;_velocity=state.Velocity+definition.VelocityPerStrength*strength;
        _at=time;_frozen=false;_last=occurrence;_accepted++;
    }
    public void Stop(AnimationStop policy,double time)
    {
        var state=Read(time);
        _position=policy==AnimationStop.Hold?state.Position:0;_velocity=0;_at=time;_frozen=true;
    }
}
