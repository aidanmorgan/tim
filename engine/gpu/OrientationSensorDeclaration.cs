using System;

namespace CuriousContraptions.Gpu;

public readonly record struct GpuOrientationSensorId(ulong Value);

/// <summary>Orientation threshold as the cosine of θ/2: a body has turned at least θ from its admitted pose once
/// |⟨q, q₀⟩| ≤ cos(θ/2). Declared data evaluated by the shared solver; no element identity selects it.</summary>
public readonly record struct OrientationThreshold(float CosineHalfAngle)
{
    /// <summary>The declared threshold angle range in degrees (Domino: 45°).</summary>
    public const float MinimumDegrees = 4;
    public const float MaximumDegrees = 179;
    /// <summary>The f32 cosines of the bounding half-angles: the admitted range of the compiled lane.</summary>
    public static float MinimumCosine => Cosine(MaximumDegrees);
    public static float MaximumCosine => Cosine(MinimumDegrees);
    public static OrientationThreshold Degrees(float angle)
    {
        if (!float.IsFinite(angle) || angle < MinimumDegrees || angle > MaximumDegrees) throw new ArgumentOutOfRangeException(nameof(angle));
        return new(Cosine(angle));
    }
    public void Validate()
    {
        PhysicsDeclarationBounds.Range(CosineHalfAngle, MinimumCosine, MaximumCosine);
    }
    private static float Cosine(float angle) => (float)MathF.Cos(angle * MathF.PI / 360);
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
    OrientationSensorPhase Phase, uint EventOrdinal, float EventPhase);
