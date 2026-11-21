using System;
using System.Numerics;

namespace CuriousContraptions.Gpu;

/// <summary>F32 conical impedance and energy reservation. All source ratings and sample weights are declaration data.</summary>
public static class ConicalAirflow
{
    public static bool Contains(Vector3 relativePoint, Vector3 axis, float reach, float terminalRadius)
    {
        var axial = Vector3.Dot(relativePoint, axis);
        if (axial <= 0 || axial >= reach) return false;
        var radial = relativePoint - axial * axis;
        var radius = terminalRadius * (axial / reach);
        return radial.LengthSquared() < radius * radius;
    }

    /// <summary>Requested branch impulse in N·s, before the source-wide cap and shared joule allocation.</summary>
    public static float Impulse(float force, float flowSpeed, float relativeSpeed, float weight,
        float pressureScale, float seconds)
    {
        if (flowSpeed == 0 || force == 0 || pressureScale == 0 || relativeSpeed >= flowSpeed) return 0;
        var slip = MathF.Max(0, flowSpeed - relativeSpeed);
        var impulse = force * weight * pressureScale * seconds * (slip / flowSpeed);
        return float.IsFinite(impulse) ? impulse : float.MaxValue;
    }

    /// <summary>One source shares its force and power across all exposed branches.</summary>
    public static void LimitSource(Span<float> impulses, float force, float flowSpeed, float power, float seconds)
    {
        var limit = flowSpeed > 0 ? MathF.Min(force * seconds, power * seconds / flowSpeed) : 0;
        ProportionalAllocation.Grant(impulses, MathF.Max(0, limit), impulses);
    }

    /// <summary>Point-relative speed includes source-frame rotation at the receiver point.</summary>
    public static float RelativeSpeed(Vector3 point, Vector3 axis, Vector3 receiverCentre, Vector3 receiverVelocity,
        Vector3 receiverSpin, Vector3 sourceCentre, Vector3 sourceVelocity, Vector3 sourceSpin) =>
        Vector3.Dot(receiverVelocity + Vector3.Cross(receiverSpin, point - receiverCentre) -
            sourceVelocity - Vector3.Cross(sourceSpin, point - sourceCentre), axis);
}
