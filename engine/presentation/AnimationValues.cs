using System;

namespace CuriousContraptions.Presentation;

// Authored and retained cosmetic values. Only external clock timestamps and temporary
// evaluation arithmetic may be wider. Constructors also define the supported Half profile.
internal static class AnimationNumbers
{
    public static Half Finite(Half value)
    {
        if (!Half.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
        return value;
    }
    public static Half Positive(Half value)
    {
        Finite(value);
        if (value <= (Half)0) throw new ArgumentOutOfRangeException(nameof(value));
        return value;
    }
    public static Half Unit(Half value)
    {
        Finite(value);
        if (value < (Half)0 || value > (Half)1) throw new ArgumentOutOfRangeException(nameof(value));
        return value;
    }
    public static Half SignedUnit(Half value)
    {
        Finite(value);
        if (value < (Half)(-1) || value > (Half)1) throw new ArgumentOutOfRangeException(nameof(value));
        return value;
    }
    public static Half OscillationCoefficient(Half value)
    {
        Finite(value);
        if ((double)value < 1e-6) throw new ArgumentOutOfRangeException(nameof(value));
        return value;
    }
    public static double Elapsed(double time, double started)
    {
        var elapsed = time - started;
        if (!double.IsFinite(elapsed)) throw new ArgumentOutOfRangeException(nameof(time));
        return elapsed;
    }
}

public readonly record struct AnimationRadians
{
    public Half Value { get; }
    public AnimationRadians(Half value) => Value = AnimationNumbers.Finite(value);
}

public readonly record struct AnimationMetres
{
    public Half Value { get; }
    public AnimationMetres(Half value) => Value = AnimationNumbers.Finite(value);
}

public readonly record struct AnimationScale
{
    public Half Value { get; }
    public AnimationScale(Half value) => Value = AnimationNumbers.Positive(value);
}

public readonly record struct AnimationOpacity
{
    public Half Value { get; }
    public AnimationOpacity(Half value) => Value = AnimationNumbers.Unit(value);
}

public readonly record struct AnimationColourBlend
{
    public Half Value { get; }
    public AnimationColourBlend(Half value) => Value = AnimationNumbers.Unit(value);
}

public readonly record struct AnimationColourChannel
{
    public Half Value { get; }
    public AnimationColourChannel(Half value) => Value = AnimationNumbers.Unit(value);
}

public readonly record struct AnimationDurationSeconds
{
    public Half Value { get; }
    public AnimationDurationSeconds(Half value) => Value = AnimationNumbers.Positive(value);
}

public readonly record struct AnimationResponsePerSecond
{
    public Half Value { get; }
    public AnimationResponsePerSecond(Half value) => Value = AnimationNumbers.Positive(value);
}

public readonly record struct AnimationDecayPerSecond
{
    public Half Value { get; }
    public AnimationDecayPerSecond(Half value) => Value = AnimationNumbers.OscillationCoefficient(value);
}

public readonly record struct AnimationRadiansPerSecond
{
    public Half Value { get; }
    public AnimationRadiansPerSecond(Half value) => Value = AnimationNumbers.Finite(value);
}

public readonly record struct AnimationMetresPerSecond
{
    public Half Value { get; }
    public AnimationMetresPerSecond(Half value) => Value = AnimationNumbers.Finite(value);
}

public readonly record struct AnimationFrequencyRadiansPerSecond
{
    public Half Value { get; }
    public AnimationFrequencyRadiansPerSecond(Half value) => Value = AnimationNumbers.OscillationCoefficient(value);
}

public readonly record struct AnimationPlaybackRate
{
    public Half Value { get; }
    public AnimationPlaybackRate(Half value) => Value = AnimationNumbers.Finite(value);
}

public readonly record struct AnimationStrength
{
    public Half Value { get; }
    public AnimationStrength(Half value) => Value = AnimationNumbers.Unit(value);
}

public readonly record struct AnimationSignedStrength
{
    public Half Value { get; }
    public AnimationSignedStrength(Half value) => Value = AnimationNumbers.SignedUnit(value);
}

public readonly record struct AnimationPeakPhase
{
    public Half Value { get; }
    public AnimationPeakPhase(Half value) => Value = AnimationNumbers.Unit(value);
}

public readonly record struct AnimationValue
{
    public AnimationProperty Property { get; }
    private readonly Half _value;
    internal double Number => (double)_value;
    public ushort CanonicalBits => BitConverter.HalfToUInt16Bits(_value);
    private AnimationValue(AnimationProperty property, Half value)
    {
        Property = property; _value = value; Validate();
    }
    public AnimationValue(AnimationRadians value) : this(AnimationProperty.LocalRotationAngle, value.Value) { }
    public AnimationValue(AnimationMetres value) : this(AnimationProperty.LocalTranslation, value.Value) { }
    public AnimationValue(AnimationScale value) : this(AnimationProperty.UniformScale, value.Value) { }
    public AnimationValue(AnimationOpacity value) : this(AnimationProperty.Opacity, value.Value) { }
    public AnimationValue(AnimationColourBlend value) : this(AnimationProperty.ColourBlend, value.Value) { }
    public static AnimationValue Channel(AnimationProperty property, AnimationColourChannel value)
    {
        if (property is not (AnimationProperty.ColourRed or AnimationProperty.ColourGreen or AnimationProperty.ColourBlue))
            throw new ArgumentOutOfRangeException(nameof(property));
        return new(property, value.Value);
    }
    public AnimationRadians Radians => new(Require(AnimationProperty.LocalRotationAngle));
    public AnimationMetres Metres => new(Require(AnimationProperty.LocalTranslation));
    public AnimationScale Scale => new(Require(AnimationProperty.UniformScale));
    public AnimationOpacity Opacity => new(Require(AnimationProperty.Opacity));
    public AnimationColourBlend ColourBlend => new(Require(AnimationProperty.ColourBlend));
    public AnimationColourChannel ColourChannel
    {
        get
        {
            if (Property is not (AnimationProperty.ColourRed or AnimationProperty.ColourGreen or AnimationProperty.ColourBlue))
                throw new InvalidOperationException("The value is not an RGB channel.");
            return new(_value);
        }
    }
    private Half Require(AnimationProperty property)
    {
        Validate();
        if (Property != property) throw new InvalidOperationException("Animation quantity has a different dimension/property.");
        return _value;
    }
    internal void Validate()
    {
        if (!Enum.IsDefined(Property)) throw new ArgumentOutOfRangeException(nameof(Property));
        AnimationNumbers.Finite(_value);
        if (Property == AnimationProperty.UniformScale) AnimationNumbers.Positive(_value);
        else if (Property is not (AnimationProperty.LocalRotationAngle or AnimationProperty.LocalTranslation))
            AnimationNumbers.Unit(_value);
    }
    public static AnimationValue Blend(AnimationValue from, AnimationValue to, AnimationColourBlend progress)
    {
        SameProperty(from, to);
        AnimationNumbers.Unit(progress.Value);
        if (progress.Value == (Half)0) return from;
        if (progress.Value == (Half)1) return to;
        return Narrow(from.Property, Math.FusedMultiplyAdd(to.Number - from.Number, (double)progress.Value, from.Number));
    }
    internal static AnimationValue Narrow(AnimationProperty property, double value)
    {
        if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
        return new(property, (Half)value);
    }
    internal static void SameProperty(AnimationValue first, AnimationValue second)
    {
        first.Validate(); second.Validate();
        if (first.Property != second.Property) throw new ArgumentException("Animation endpoint dimensions/properties differ.");
    }
}

public readonly record struct AnimationVelocity
{
    public AnimationProperty Property { get; }
    private readonly Half _value;
    internal double Number => (double)_value;
    public AnimationVelocity(AnimationRadiansPerSecond value)
    {
        Property = AnimationProperty.LocalRotationAngle; _value = AnimationNumbers.Finite(value.Value);
    }
    public AnimationVelocity(AnimationMetresPerSecond value)
    {
        Property = AnimationProperty.LocalTranslation; _value = AnimationNumbers.Finite(value.Value);
    }
    public AnimationRadiansPerSecond RadiansPerSecond => Property == AnimationProperty.LocalRotationAngle
        ? new(_value) : throw new InvalidOperationException("Velocity is not angular.");
    public AnimationMetresPerSecond MetresPerSecond => Property == AnimationProperty.LocalTranslation
        ? new(_value) : throw new InvalidOperationException("Velocity is not linear.");
    internal void Validate()
    {
        if (Property is not (AnimationProperty.LocalRotationAngle or AnimationProperty.LocalTranslation))
            throw new ArgumentException("Velocity requires a signed motion dimension.");
        AnimationNumbers.Finite(_value);
    }
    internal static AnimationVelocity Narrow(AnimationProperty property, double value) => property switch
    {
        AnimationProperty.LocalRotationAngle => new(new AnimationRadiansPerSecond((Half)value)),
        AnimationProperty.LocalTranslation => new(new AnimationMetresPerSecond((Half)value)),
        _ => throw new ArgumentException("Velocity requires a signed motion dimension.")
    };
}

// A canonical control anchor, never a second clock or accumulated numerical remainder.
// The repeat kind fixes its period: Once [0,1], Loop [0,1), PingPong [0,2).
internal readonly record struct AnimationPhase
{
    public Half Value { get; }
    private AnimationPhase(Half value) => Value = value;
    public static AnimationPhase Capture(double phase, AnimationRepeat repeat)
    {
        if (!double.IsFinite(phase)) throw new ArgumentOutOfRangeException(nameof(phase));
        var period = repeat == AnimationRepeat.PingPong ? 2.0 : 1.0;
        if (repeat == AnimationRepeat.Once) return new((Half)Math.Clamp(phase, 0, 1));
        if (!Enum.IsDefined(repeat)) throw new ArgumentOutOfRangeException(nameof(repeat));
        var reduced = phase % period;
        if (reduced < 0) reduced += period;
        var canonical = (Half)reduced;
        return new((double)canonical == period ? (Half)0 : canonical);
    }
}
