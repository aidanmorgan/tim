using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

public enum BodyCandidateStatus : uint { Committed = 0, InvalidRecord = 1, OutOfRange = 2 }
public enum CellScale : int { Metres = -4 }
public enum TimeScale : int { Seconds = -9 }
public readonly record struct GpuBodyId(ulong Value);
public readonly record struct CellOrigin(int X, int Y, int Z);
public readonly record struct LocalPosition(float X, float Y, float Z);

/// <summary>Committed linear velocity in metres per second as IEEE-754 f32, bounded by the game-grade 64 m/s envelope.</summary>
public readonly record struct LinearVelocity(float X, float Y, float Z)
{
    public const float MaximumSpeed = 64;
    public void Validate() => PhysicsDeclarationBounds.Magnitude(X, Y, Z, MaximumSpeed);
    public bool IsPositiveZero => F32Bits.IsPositiveZero(X, Y, Z);
    public bool HasSameBits(LinearVelocity other) =>
        F32Bits.Equal(X, other.X) && F32Bits.Equal(Y, other.Y) && F32Bits.Equal(Z, other.Z);
}

/// <summary>Canonical construction/readback. No host integration or correction.</summary>
public readonly record struct CanonicalBody(
    GpuBodyId Id, ulong Epoch, ulong Tick, CellOrigin Cell, LocalPosition Local, LinearVelocity Velocity)
{
    public const int ByteLength = 80;
    public const int MaximumCell = 1024;







    public void Validate()
    {
        if (Id.Value == 0) throw new ArgumentException("Body identity must be nonzero.");
        ValidateAxis(Cell.X, Local.X);
        ValidateAxis(Cell.Y, Local.Y);
        ValidateAxis(Cell.Z, Local.Z);
        Velocity.Validate();
    }

    private static void ValidateAxis(int cell, float local)
    {
        if (!float.IsFinite(local) ||
            local < -.5f || local >= .5f ||
            cell < -MaximumCell || cell > MaximumCell ||
            (cell == MaximumCell && local > (float)0) ||
            (cell == -MaximumCell && local < (float)0))
            throw new ArgumentOutOfRangeException(nameof(local), "Canonical body axis exceeds admitted profile.");
    }

}
