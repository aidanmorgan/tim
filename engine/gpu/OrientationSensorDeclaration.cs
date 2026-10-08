using System;

namespace CuriousContraptions.Gpu;

public readonly record struct GpuOrientationSensorId(ulong Value);

/// <summary>Half-angle cosine form of an orientation threshold: a body has turned at least θ from its admitted pose once
/// |⟨q, q₀⟩| ≤ cos(θ/2). Declared data evaluated by the shared solver; no element identity selects it.</summary>
public readonly record struct OrientationThreshold(Half CosineHalfAngle)
{
    // Half rounding of the cosine admits angles from 4° (cos 2° = 0.99939 stays below 1 − 2^-11) to 179°; outside that the value would fail Validate obscurely.
    public static OrientationThreshold Degrees(double angle)
    {
        if (!double.IsFinite(angle) || angle < 4 || angle > 179) throw new ArgumentOutOfRangeException(nameof(angle));
        return new((Half)Math.Cos(angle * Math.PI / 360));
    }
    public void Validate()
    {
        PhysicsDeclarationBounds.Range(CosineHalfAngle, (Half)(1.0 / 1024), (Half)1);
        if (CosineHalfAngle == (Half)1) throw new ArgumentException("An orientation threshold must admit some rotation.");
    }
}

/// <summary>Sticky orientation-threshold sensor on one dynamic body: it fires once per world when the body's rotation from its
/// admitted initial pose reaches the declared angle, evaluated at substep endpoints; only a fresh admission rearms it.</summary>
public readonly record struct OrientationSensorDeclaration(GpuOrientationSensorId Id, GpuBodyId Body,
    CanonicalRotation Initial, OrientationThreshold Threshold)
{
    public void Validate()
    {
        if (Id.Value == 0 || Body.Value == 0) throw new ArgumentException("Invalid orientation sensor identity.");
        Initial.Validate(); Threshold.Validate();
    }
}

public enum OrientationSensorPhase : uint { Armed, Fired }

/// <summary>Committed sensor state read from the candidate record; the collider is the sensed body's own homogeneous shape.</summary>
public readonly record struct OrientationSensorRead(GpuOrientationSensorId Id, GpuBodyId Body, GpuColliderId Collider,
    OrientationSensorPhase Phase, uint EventOrdinal, Half EventPhase);
