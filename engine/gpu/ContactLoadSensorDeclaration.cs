using System;
using System.Collections.Immutable;
using System.Numerics;

namespace CuriousContraptions.Gpu;

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
    /// <summary>The phase of a measured f32 mass, compared before any rounding: Empty (no qualifying mass), Underweight
    /// (0 &lt; mass &lt; minimum), Loaded (mass ≥ minimum).</summary>
    public static ContactLoadPhase PhaseOf(this ContactLoadSensorDeclaration declaration, float mass) =>
        mass <= 0 ? ContactLoadPhase.Empty : mass < declaration.MinimumMass ? ContactLoadPhase.Underweight : ContactLoadPhase.Loaded;

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
