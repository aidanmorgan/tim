using CuriousContraptions.Abstractions;
using System;

namespace CuriousContraptions.Presentation;

// Authored, retained and evaluated cosmetic values use f32. Integer timestamps retain clock identity.
internal static class AnimationNumbers
{
    // Convex f32 interpolation never forms an overflowing endpoint difference. Clamp the
    // rounded result to the authored interval (also preserves positive subnormal scales).
    public static float Interpolate(float from, float to, float amount) =>
        Math.Clamp(MathF.FusedMultiplyAdd(from, 1 - amount, to * amount), Math.Min(from, to), Math.Max(from, to));

    public static float Finite(float value)
    {
        if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
        return value;
    }
    public static float Positive(float value)
    {
        Finite(value);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
        return value;
    }
    public static float Unit(float value)
    {
        Finite(value);
        if (value < (float)0 || value > (float)1) throw new ArgumentOutOfRangeException(nameof(value));
        return value;
    }
    public static float SignedUnit(float value)
    {
        Finite(value);
        if (value < (float)(-1) || value > (float)1) throw new ArgumentOutOfRangeException(nameof(value));
        return value;
    }
    public static float OscillationCoefficient(float value)
    {
        Finite(value);
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1e-6f);
        return value;
    }
    public static float Elapsed(PhysicalTime time, PhysicalTime started)
    {
        var elapsed = time.SecondsSince(started);
        if (!float.IsFinite(elapsed)) throw new ArgumentOutOfRangeException(nameof(time));
        return elapsed;
    }
}

public readonly record struct AnimationRadians
{
    public float Value { get; }
    public AnimationRadians(float value) => Value = AnimationNumbers.Finite(value);
}

public readonly record struct AnimationMetres
{
    public float Value { get; }
    public AnimationMetres(float value) => Value = AnimationNumbers.Finite(value);
}

public readonly record struct AnimationScale
{
    public float Value { get; }
    public AnimationScale(float value) => Value = AnimationNumbers.Positive(value);
}

public readonly record struct AnimationOpacity
{
    public float Value { get; }
    public AnimationOpacity(float value) => Value = AnimationNumbers.Unit(value);
}

public readonly record struct AnimationColourBlend
{
    public float Value { get; }
    public AnimationColourBlend(float value) => Value = AnimationNumbers.Unit(value);
}

public readonly record struct AnimationColourChannel
{
    public float Value { get; }
    public AnimationColourChannel(float value) => Value = AnimationNumbers.Unit(value);
}

public readonly record struct AnimationDurationSeconds
{
    public float Value { get; }
    public AnimationDurationSeconds(float value) => Value = AnimationNumbers.Positive(value);
}

public readonly record struct AnimationResponsePerSecond
{
    public float Value { get; }
    public AnimationResponsePerSecond(float value) => Value = AnimationNumbers.Positive(value);
}

public readonly record struct AnimationDecayPerSecond
{
    public float Value { get; }
    public AnimationDecayPerSecond(float value) => Value = AnimationNumbers.OscillationCoefficient(value);
}

public readonly record struct AnimationRadiansPerSecond
{
    public float Value { get; }
    public AnimationRadiansPerSecond(float value) => Value = AnimationNumbers.Finite(value);
}

public readonly record struct AnimationMetresPerSecond
{
    public float Value { get; }
    public AnimationMetresPerSecond(float value) => Value = AnimationNumbers.Finite(value);
}

public readonly record struct AnimationFrequencyRadiansPerSecond
{
    public float Value { get; }
    public AnimationFrequencyRadiansPerSecond(float value) => Value = AnimationNumbers.OscillationCoefficient(value);
}

public readonly record struct AnimationPlaybackRate
{
    public float Value { get; }
    public AnimationPlaybackRate(float value) => Value = AnimationNumbers.Finite(value);
}

public readonly record struct AnimationStrength
{
    public float Value { get; }
    public AnimationStrength(float value) => Value = AnimationNumbers.Unit(value);
}

public readonly record struct AnimationSignedStrength
{
    public float Value { get; }
    public AnimationSignedStrength(float value) => Value = AnimationNumbers.SignedUnit(value);
}

public readonly record struct AnimationPeakPhase
{
    public float Value { get; }
    public AnimationPeakPhase(float value) => Value = AnimationNumbers.Unit(value);
}

public readonly record struct AnimationValue
{
    public AnimationProperty Property { get; }
    private readonly float _value;
    internal float Number => _value;
    public uint CanonicalBits => BitConverter.SingleToUInt32Bits(_value);
    private AnimationValue(AnimationProperty property, float value)
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
    private float Require(AnimationProperty property)
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
        if (progress.Value == (float)0) return from;
        if (progress.Value == (float)1) return to;
        return FromScalar(from.Property, AnimationNumbers.Interpolate(from.Number, to.Number, progress.Value));
    }
    internal static AnimationValue FromScalar(AnimationProperty property, float value)
    {
        if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
        return new(property, (float)value);
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
    private readonly float _value;
    internal float Number => _value;
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
    internal static AnimationVelocity FromScalar(AnimationProperty property, float value) => property switch
    {
        AnimationProperty.LocalRotationAngle => new(new AnimationRadiansPerSecond((float)value)),
        AnimationProperty.LocalTranslation => new(new AnimationMetresPerSecond((float)value)),
        _ => throw new ArgumentException("Velocity requires a signed motion dimension.")
    };
}

// A canonical control anchor, never a second clock or accumulated numerical remainder.
// The repeat kind fixes its period: Once [0,1], Loop [0,1), PingPong [0,2).
internal readonly record struct AnimationPhase
{
    public float Value { get; }
    private AnimationPhase(float value) => Value = value;
    public static AnimationPhase Capture(float phase, AnimationRepeat repeat)
    {
        if (!float.IsFinite(phase)) throw new ArgumentOutOfRangeException(nameof(phase));
        var period = repeat == AnimationRepeat.PingPong ? 2.0f : 1.0f;
        if (repeat == AnimationRepeat.Once) return new((float)Math.Clamp(phase, 0, 1));
        if (!Enum.IsDefined(repeat)) throw new ArgumentOutOfRangeException(nameof(repeat));
        var reduced = phase % period;
        if (reduced < 0) reduced += period;
        var canonical = (float)reduced;
        return new(canonical == period ? (float)0 : canonical);
    }
}
