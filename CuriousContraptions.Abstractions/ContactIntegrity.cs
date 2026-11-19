using System;
using System.Collections.Generic;

namespace CuriousContraptions.Gpu;

/// <summary>One accepted normal contact. Approach speed is captured before external loads and warmstart;
/// normal impulse is the final signed LambdaN after relaxation, restitution and energy limiting.</summary>
public readonly record struct ContactImpulseSample(GpuColliderId ColliderA, GpuColliderId ColliderB,
    float ApproachSpeed, float NormalImpulse);

/// <summary>Sparse per-collider-pair measurement, reset on every accepted substep. No accumulated hit damage.</summary>
public sealed class ContactIntegrity
{
    private readonly Dictionary<GpuColliderId, float> _pairs = new();

    public float Measure(ReadOnlySpan<ContactImpulseSample> contacts, GpuColliderId observed, float minimumApproachSpeed)
    {
        if (observed.Value == 0 || !float.IsFinite(minimumApproachSpeed) || minimumApproachSpeed < 0f)
            throw new ArgumentException("Invalid integrity contact selector.");
        _pairs.Clear();
        var maximum = 0f;
        foreach (var contact in contacts)
        {
            Validate(contact);
            if (contact.ApproachSpeed <= minimumApproachSpeed) continue;
            GpuColliderId other;
            if (contact.ColliderA == observed) other = contact.ColliderB;
            else if (contact.ColliderB == observed) other = contact.ColliderA;
            else continue;
            _pairs.TryGetValue(other, out var previous);
            var total = MathF.Min(float.MaxValue, previous + MathF.Max(0f, contact.NormalImpulse));
            _pairs[other] = total;
            maximum = MathF.Max(maximum, total);
        }
        return maximum;
    }

    public static bool Ruptures(float pairImpulse, float threshold)
    {
        if (!float.IsFinite(pairImpulse) || pairImpulse < 0f || !float.IsFinite(threshold) || threshold < 0f)
            throw new ArgumentException("Invalid integrity impulse threshold.");
        return pairImpulse > threshold;
    }

    private static void Validate(ContactImpulseSample sample)
    {
        if (sample.ColliderA.Value == 0 || sample.ColliderB.Value == 0 || sample.ColliderA == sample.ColliderB ||
            !float.IsFinite(sample.ApproachSpeed) || !float.IsFinite(sample.NormalImpulse))
            throw new ArgumentException("Invalid accepted contact sample.");
    }
}
