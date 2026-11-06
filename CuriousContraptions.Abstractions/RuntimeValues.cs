using System;

namespace CuriousContraptions.Gpu;

public readonly record struct WorkshopObservationOrdinal(ulong Value);
public readonly record struct ConstructionRevision(ulong Value);
public readonly record struct AuthorityRevision(ulong Value)
{
    /// <summary>Advances a session authority once; exhaustion refuses without changing or wrapping the value.</summary>
    public bool TryAdvance(out AuthorityRevision next)
    {
        next = this;
        if (Value == ulong.MaxValue) return false;
        next = new(Value + 1);
        return true;
    }
}
public readonly record struct SimulationEpoch(ulong Value);
public readonly record struct SimulationTick(ulong Value);
public readonly record struct CommandSequence(ulong Value);
public readonly record struct Metres(float Value);
public readonly record struct Kilograms(float Value);
public readonly record struct Acceleration(float Value);
public readonly record struct InverseSeconds(float Value);
public readonly record struct Restitution(float Value);
public readonly record struct CanonicalRotation(float X, float Y, float Z, float W)
{
    public static CanonicalRotation Identity => new(0f, 0f, 0f, 1f);

    public static CanonicalRotation Normalize(float x, float y, float z, float w)
    {
        var q = new System.Numerics.Quaternion(x, y, z, w);
        var length = q.LengthSquared();
        if (!float.IsFinite(length) || length < .999f || length > 1.001f)
            throw new ArgumentException("Input rotation must represent a unit quaternion.");
        q = System.Numerics.Quaternion.Normalize(q);
        return new(q.X, q.Y, q.Z, q.W);
    }

    public void Validate()
    {
        ValidateCommitted();
    }

    public void ValidateCommitted()
    {
        if (!float.IsFinite(X) || !float.IsFinite(Y) || !float.IsFinite(Z) || !float.IsFinite(W) ||
            X < -1f || X > 1f || Y < -1f || Y > 1f ||
            Z < -1f || Z > 1f || W < -1f || W > 1f ||
            (X == 0f && Y == 0f && Z == 0f && W == 0f))
            throw new ArgumentException("Unsupported committed rotation.");
        // One canonical f32 rotation: allow rounding residual, reject scaled quaternions.
        var norm = X * X + Y * Y + Z * Z + W * W;
        if (norm < .99999f || norm > 1.00001f)
            throw new ArgumentException("Canonical rotation must represent a unit quaternion.");
    }
}

/// <summary>Canonical f32 bit identity, including signed zero; positive zero is the all-zero bit pattern.</summary>
public static class F32Bits
{
    public static bool Equal(LocalPosition a, LocalPosition b) => Equal(a.X,b.X) && Equal(a.Y,b.Y) && Equal(a.Z,b.Z);
    public static bool Equal(MetreVector a, MetreVector b) => Equal(a.X,b.X) && Equal(a.Y,b.Y) && Equal(a.Z,b.Z);
    public static bool Equal(float a, float b) => BitConverter.SingleToUInt32Bits(a) == BitConverter.SingleToUInt32Bits(b);
    public static bool IsPositiveZero(float x, float y, float z) =>
        (BitConverter.SingleToUInt32Bits(x) | BitConverter.SingleToUInt32Bits(y) | BitConverter.SingleToUInt32Bits(z)) == 0;
}
