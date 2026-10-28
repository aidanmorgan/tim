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

    public byte[] Encode()
    {
        var bytes = new byte[ByteLength];
        Write(bytes);
        return bytes;
    }

    public void Write(Span<byte> data)
    {
        Validate();
        if (data.Length != ByteLength) throw new ArgumentException("Invalid canonical destination length.");
        data.Clear();
        BinaryPrimitives.WriteUInt64LittleEndian(data[8..], Epoch);
        BinaryPrimitives.WriteUInt64LittleEndian(data[16..], Tick);
        BinaryPrimitives.WriteUInt64LittleEndian(data[24..], Id.Value);
        BinaryPrimitives.WriteInt32LittleEndian(data[48..], (int)CellScale.Metres);
        BinaryPrimitives.WriteInt32LittleEndian(data[52..], (int)TimeScale.Seconds);
        BinaryPrimitives.WriteInt32LittleEndian(data[32..], Cell.X);
        BinaryPrimitives.WriteInt32LittleEndian(data[36..], Cell.Y);
        BinaryPrimitives.WriteInt32LittleEndian(data[40..], Cell.Z);
        BinaryPrimitives.WriteSingleLittleEndian(data[56..], Local.X); BinaryPrimitives.WriteSingleLittleEndian(data[60..], Local.Y); BinaryPrimitives.WriteSingleLittleEndian(data[64..], Local.Z);
        BinaryPrimitives.WriteSingleLittleEndian(data[68..], Velocity.X);
        BinaryPrimitives.WriteSingleLittleEndian(data[72..], Velocity.Y);
        BinaryPrimitives.WriteSingleLittleEndian(data[76..], Velocity.Z);
    }

    public static CanonicalBody Decode(ReadOnlySpan<byte> data)
    {
        if (data.Length != ByteLength ||
            BinaryPrimitives.ReadUInt32LittleEndian(data) != 0 || // bytes 0..3: zero padding (one bundle, one schema)
            BinaryPrimitives.ReadUInt32LittleEndian(data[4..]) != (uint)BodyCandidateStatus.Committed ||
            BinaryPrimitives.ReadInt32LittleEndian(data[48..]) != (int)CellScale.Metres ||
            BinaryPrimitives.ReadInt32LittleEndian(data[52..]) != (int)TimeScale.Seconds ||
            BinaryPrimitives.ReadUInt32LittleEndian(data[44..]) != 0)
            throw new ArgumentException("Unsupported canonical body record.");
        var body = new CanonicalBody(
            new(BinaryPrimitives.ReadUInt64LittleEndian(data[24..])),
            BinaryPrimitives.ReadUInt64LittleEndian(data[8..]),
            BinaryPrimitives.ReadUInt64LittleEndian(data[16..]),
            new(BinaryPrimitives.ReadInt32LittleEndian(data[32..]), BinaryPrimitives.ReadInt32LittleEndian(data[36..]), BinaryPrimitives.ReadInt32LittleEndian(data[40..])),
            new(BinaryPrimitives.ReadSingleLittleEndian(data[56..]), BinaryPrimitives.ReadSingleLittleEndian(data[60..]), BinaryPrimitives.ReadSingleLittleEndian(data[64..])),
            new(BinaryPrimitives.ReadSingleLittleEndian(data[68..]), BinaryPrimitives.ReadSingleLittleEndian(data[72..]),
                BinaryPrimitives.ReadSingleLittleEndian(data[76..])));
        body.Validate();
        return body;
    }

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
