using System;
using System.Runtime.Intrinsics;

namespace CuriousContraptions.Gpu;

/// <summary>One proportional allocation over finite nonnegative requests expressed in the same units.</summary>
public static class ProportionalAllocation
{
    public static void Grant(ReadOnlySpan<float> requests, float available, Span<float> grants)
    {
        if (requests.Length != grants.Length || !float.IsFinite(available) || available < 0 ||
            requests.Overlaps(grants, out var offset) && offset != 0)
            throw new ArgumentException("Invalid shared resource budget.");
        var largest = 0f;
        foreach (var request in requests)
        {
            if (!float.IsFinite(request) || request < 0) throw new ArgumentException("Invalid resource request.");
            largest = MathF.Max(largest, request);
        }
        if (largest == 0 || available == 0) { grants.Clear(); return; }
        // Normalize before summing so many finite requests cannot overflow a finite allowance.
        var normalized = 0f;
        foreach (var request in requests) normalized += request / largest;
        var scale = MathF.Min(1, (available / largest) / normalized);
        var index = 0;
        for (; index + 4 <= requests.Length; index += 4)
        {
            var vector = Vector128.Create(requests[index], requests[index + 1], requests[index + 2], requests[index + 3]) *
                Vector128.Create(scale);
            for (var lane = 0; lane < 4; lane++) grants[index + lane] = vector.GetElement(lane);
        }
        for (; index < requests.Length; index++) grants[index] = requests[index] * scale;
        // Residual rounding may not manufacture a grant beyond the common finite allowance.
        var spent = 0f;
        for (var i = 0; i < grants.Length; i++)
        {
            var grant = MathF.Min(grants[i], MathF.Max(0, available - spent));
            if (spent + grant > available) grant = MathF.Max(0, MathF.BitDecrement(grant));
            grants[i] = grant;
            spent += grant;
        }
    }
}
