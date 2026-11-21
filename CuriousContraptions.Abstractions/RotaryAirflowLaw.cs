using System;

namespace CuriousContraptions.Gpu;

/// <summary>One frozen, allocated branch. Speed and relative velocity are m/s, conductance N s/m,
/// force limit N and alignment the signed cosine against the exact coordinate axis.</summary>
public readonly record struct RotaryAirflowBranch(float Speed, float RelativeVelocity,
    float Conductance, float ForceLimit, float Alignment)
{
    public void Validate()
    {
        if (!float.IsFinite(Speed) || Speed <= 0 || !float.IsFinite(RelativeVelocity) ||
            !float.IsFinite(Conductance) || Conductance < 0 || !float.IsFinite(ForceLimit) ||
            ForceLimit < 0 || !float.IsFinite(Alignment) || Alignment < -1 || Alignment > 1)
            throw new ArgumentException("Invalid frozen rotary airflow branch.");
    }

    public float Force(float pitch, float rate) =>
        MathF.Min(ForceLimit, Conductance * MathF.Max(0, Speed - RelativeVelocity - pitch * Alignment * rate));
}

/// <summary>Prepared response: pitch m/rad, resistance N m s/rad and requested unloaded rate rad/s.</summary>
public readonly record struct RotaryAirflowResponse(float Pitch, float Resistance, float TargetRate)
{
    public float Torque(ReadOnlySpan<RotaryAirflowBranch> branches, float rate) =>
        Pitch * RotaryAirflowLaw.AlignedForce(branches, Pitch, rate) - Resistance * rate;
}

/// <summary>First-boundary calibration of the same capped impedance used for accepted transfer.
/// Callers freeze geometry, velocities and allocation throughout Prepare; changed allocation requires a new preparation.</summary>
public static class RotaryAirflowLaw
{
    public static RotaryAirflowResponse Prepare(ReadOnlySpan<RotaryAirflowBranch> branches,
        float maximumPitch, float response, float cutInForce, float maximumRate)
    {
        Positive(maximumPitch); Positive(response); Positive(cutInForce); Positive(maximumRate);
        var passiveResistance = maximumPitch / response;
        Positive(passiveResistance);
        if (!(response * cutInForce > 0))
            throw new ArgumentException("Rotary activation rate is not representable.");
        foreach (var branch in branches) branch.Validate();
        var restForce = AlignedForce(branches, 0, 0);
        if (!float.IsFinite(restForce)) throw new ArgumentException("Rotary rest force must be finite.");
        if (MathF.Abs(restForce) < cutInForce)
            return new(0, passiveResistance, 0);
        var target = Math.Clamp(response * restForce, -maximumRate, maximumRate);
        var pitch = FirstBoundary(branches, maximumPitch, target);
        var resistance = pitch * (AlignedForce(branches, pitch, target) / target);
        if (!float.IsFinite(resistance) || !float.IsFinite(target) || target == 0)
            throw new ArgumentException("Rotary response is not representable.");
        return new(pitch, MathF.Max(0, resistance), target);
    }

    private static float FirstBoundary(ReadOnlySpan<RotaryAirflowBranch> branches, float maximumPitch, float target)
    {
        var sign = MathF.Sign(target);
        if (sign * AlignedForce(branches, maximumPitch, target) > 0) return maximumPitch;
        var lower = 0f;
        var upper = maximumPitch;
        // Each interval shrinks until adjacent f32 values. Zero is the upper bracket, including a plateau.
        while (true)
        {
            var middle = .5f * lower + .5f * upper;
            if (middle <= lower || middle >= upper) return lower;
            if (sign * AlignedForce(branches, middle, target) > 0) lower = middle;
            else upper = middle;
        }
    }

    public static float AlignedForce(ReadOnlySpan<RotaryAirflowBranch> branches, float pitch, float rate)
    {
        var result = 0f;
        foreach (var branch in branches)
            result += branch.Alignment * branch.Force(pitch, rate);
        return result;
    }

    private static void Positive(float value)
    {
        if (!float.IsFinite(value) || value <= 0) throw new ArgumentException("Expected finite positive rotary parameter.");
    }
}
