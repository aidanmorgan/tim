using System;

namespace CuriousContraptions.Gpu;

/// <summary>One finite phase allowance shared by storage charging and immediate work.
/// Reservations are entitlements only; the simulation's balance owner commits accepted payments.</summary>
public static class ElectricalSupply
{
    public const float PhaseSeconds = 1f / 120f;

    public static void Reserve(ElectricalEnable enabled, float available,
        ReadOnlySpan<float> requests, Span<float> grants)
    {
        if (!Enum.IsDefined(enabled) || !float.IsFinite(available) || available < 0 ||
            requests.Length != grants.Length || requests.Overlaps(grants))
            throw new ArgumentException("Invalid finite electrical reservation.");
        // Admit the entire input before output mutation, including disabled-source controls.
        foreach (var request in requests)
            if (!float.IsFinite(request) || request < 0)
                throw new ArgumentException("Invalid electrical request.");
        ProportionalAllocation.Grant(requests, enabled == ElectricalEnable.Enabled ? available : 0f, grants);
    }
}
