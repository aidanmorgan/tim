using System;
using System.Collections.Immutable;
using System.Numerics;

namespace CuriousContraptions.Gpu;

public readonly record struct GpuContactLoadSensorId(ulong Value);

public enum ContactLoadPhase : uint
{
    Empty = 0,
    Underweight = 1,
    Loaded = 2
}

/// <summary>One compiled contact-load row (CAT-052 §2), all f32. A contact qualifies when it touches the frame's sensing
/// <paramref name="Region"/> collider, its normal toward the other body aligns with <paramref name="LocalNormal"/> by at least
/// <paramref name="MinimumAlignment"/>, its gap is at most <paramref name="MaximumGap"/> (the contact distance) or it carried normal
/// contact impulse in the substep (a bounce resolved from the speculative gap still presses the face), and its point lies
/// inside the face's local x–z footprint. The face is identified by its collider, normal and gap, never by a y band a resting
/// box straddles (coordinator decision, 20 Oct 2026). Each dynamic body counts its mass once; bodies sum; Loaded is inclusive (≥).</summary>
public readonly record struct ContactLoadSensorDeclaration(
    GpuContactLoadSensorId Id,
    GpuBodyId Frame,
    GpuColliderId Region,
    float MinimumX,
    float MaximumX,
    float MinimumZ,
    float MaximumZ,
    Vector3 LocalNormal,
    float MinimumAlignment,
    float MaximumGap,
    float MinimumMass,
    ImmutableArray<GpuColliderId> SupportingColliders)
{
    public void Validate()
    {
        if (Id.Value == 0 || Frame.Value == 0 || Region.Value == 0)
            throw new ArgumentException("Contact-load sensor, frame and face identities are required.");
        if (SupportingColliders.IsDefaultOrEmpty) throw new ArgumentException("A sensing region requires explicit supporting colliders.");
        for (var i = 0; i < SupportingColliders.Length; i++)
        {
            if (SupportingColliders[i].Value == 0) throw new ArgumentException("A supporting collider identity is absent.");
            for (var j = 0; j < i; j++)
                if (SupportingColliders[i] == SupportingColliders[j]) throw new ArgumentException("Duplicate supporting collider.");
        }
        if (!float.IsFinite(MinimumX) || !float.IsFinite(MaximumX) || !float.IsFinite(MinimumZ) || !float.IsFinite(MaximumZ) ||
            MinimumX >= MaximumX || MinimumZ >= MaximumZ || MathF.Abs(MinimumX) > 16 || MathF.Abs(MaximumX) > 16 ||
            MathF.Abs(MinimumZ) > 16 || MathF.Abs(MaximumZ) > 16)
            throw new ArgumentOutOfRangeException(nameof(MinimumX), "Contact-load footprint must be finite, ordered and within 16 m.");
        if (!float.IsFinite(LocalNormal.X) || !float.IsFinite(LocalNormal.Y) || !float.IsFinite(LocalNormal.Z) ||
            MathF.Abs(LocalNormal.LengthSquared() - 1) > 1e-6f)
            throw new ArgumentOutOfRangeException(nameof(LocalNormal), "Contact-load normal must be a unit vector.");
        if (!float.IsFinite(MinimumAlignment) || MinimumAlignment <= 0 || MinimumAlignment > 1)
            throw new ArgumentOutOfRangeException(nameof(MinimumAlignment), "Minimum alignment must be in (0, 1].");
        if (!float.IsFinite(MaximumGap) || MaximumGap < 0 || MaximumGap > .01f)
            throw new ArgumentOutOfRangeException(nameof(MaximumGap), "Maximum gap must be in [0, 0.01] m.");
        if (!float.IsFinite(MinimumMass) || MinimumMass <= 0)
            throw new ArgumentOutOfRangeException(nameof(MinimumMass), "Minimum mass must be finite and positive.");
    }

    /// <summary>The phase of a measured f32 mass, compared before any rounding: Empty (no qualifying mass), Underweight
    /// (0 &lt; mass &lt; minimum), Loaded (mass ≥ minimum).</summary>
    public ContactLoadPhase PhaseOf(float mass) =>
        mass <= 0 ? ContactLoadPhase.Empty : mass < MinimumMass ? ContactLoadPhase.Underweight : ContactLoadPhase.Loaded;
}

/// <summary>One row's committed tick measurement: the D2 maximum qualifying f32 mass over the tick's substeps and its phase.</summary>
public readonly record struct ContactLoadSensorRead(
    GpuContactLoadSensorId Sensor,
    GpuBodyId Frame,
    GpuColliderId Region,
    float MeasuredMass,
    ContactLoadPhase Phase);

/// <summary>One solver contact seen from its static side: the frame body and collider, the other (dynamic) body and its mass, the
/// contact point and normal toward the other body (world space), the signed gap (negative while penetrating), and whether the
/// point carried normal contact impulse in the substep.</summary>
public readonly record struct ContactPointRecord(
    GpuBodyId Frame,
    GpuColliderId Collider,
    GpuBodyId Other,
    float OtherMass,
    Vector3 Point,
    Vector3 NormalTowardOther,
    float Gap,
    bool Pressed);

/// <summary>Evaluates one compiled contact-load row against one substep's contacts. Allocation-free (Story 9R.4 D4): the caller owns
/// the scratch that records which bodies already counted.</summary>
public static class ContactLoadSensorTracker
{
    public static float Measure(in ContactLoadSensorDeclaration sensor, Vector3 framePosition, Quaternion frameRotation,
        ReadOnlySpan<ContactPointRecord> contacts, Span<GpuBodyId> counted)
    {
        if (counted.Length < contacts.Length)
            throw new ArgumentException("Contact-load scratch must hold one body per contact.", nameof(counted));
        var inverse = Quaternion.Conjugate(frameRotation);
        var bodies = 0;
        var mass = 0f;
        foreach (var contact in contacts)
        {
            if (contact.Frame != sensor.Frame || !sensor.SupportingColliders.Contains(contact.Collider) || contact.OtherMass <= 0 ||
                (contact.Gap > sensor.MaximumGap && !contact.Pressed))
                continue;
            if (Vector3.Dot(Vector3.Transform(contact.NormalTowardOther, inverse), sensor.LocalNormal) < sensor.MinimumAlignment)
                continue;
            var local = Vector3.Transform(contact.Point - framePosition, inverse);
            if (local.X < sensor.MinimumX || local.X > sensor.MaximumX || local.Z < sensor.MinimumZ || local.Z > sensor.MaximumZ)
                continue;
            if (counted[..bodies].Contains(contact.Other)) continue;
            counted[bodies++] = contact.Other;
            mass += contact.OtherMass;
        }
        return mass;
    }
}
