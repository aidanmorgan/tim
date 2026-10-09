using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

public enum BodyCandidateStatus : uint { Committed = 0, InvalidRecord = 1, OutOfRange = 2 }
/// <summary>Canonical body record schema: binary16 pose remainder with f32 velocity (m/s). The value stays 2 because the 80-byte construction
/// and save bytes are unchanged (an admitted construction carries all-zero velocity, the same bits in both formats); any other value rejects.</summary>
public enum BodyRecordVersion : uint { HalfPoseF32Velocity = 2 }
public enum CellScale : int { Metres = -4 }
public enum TimeScale : int { Seconds = -9 }
public readonly record struct GpuBodyId(ulong Value);
public readonly record struct CellOrigin(int X, int Y, int Z);
public readonly record struct LocalPosition(Half X, Half Y, Half Z);

/// <summary>Committed linear velocity in metres per second as IEEE-754 f32, bounded by the game-grade 64 m/s envelope.</summary>
public readonly record struct LinearVelocity(float X, float Y, float Z)
{
    public const double MaximumSpeed = 64;
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
        BinaryPrimitives.WriteUInt32LittleEndian(data, (uint)BodyRecordVersion.HalfPoseF32Velocity);
        BinaryPrimitives.WriteUInt64LittleEndian(data[8..], Epoch);
        BinaryPrimitives.WriteUInt64LittleEndian(data[16..], Tick);
        BinaryPrimitives.WriteUInt64LittleEndian(data[24..], Id.Value);
        BinaryPrimitives.WriteInt32LittleEndian(data[48..], (int)CellScale.Metres);
        BinaryPrimitives.WriteInt32LittleEndian(data[52..], (int)TimeScale.Seconds);
        BinaryPrimitives.WriteInt32LittleEndian(data[32..], Cell.X);
        BinaryPrimitives.WriteInt32LittleEndian(data[36..], Cell.Y);
        BinaryPrimitives.WriteInt32LittleEndian(data[40..], Cell.Z);
        WriteHalf(data[56..], Local.X); WriteHalf(data[58..], Local.Y); WriteHalf(data[60..], Local.Z);
        BinaryPrimitives.WriteSingleLittleEndian(data[64..], Velocity.X);
        BinaryPrimitives.WriteSingleLittleEndian(data[68..], Velocity.Y);
        BinaryPrimitives.WriteSingleLittleEndian(data[72..], Velocity.Z);
    }

    public static CanonicalBody Decode(ReadOnlySpan<byte> data)
    {
        if (data.Length != ByteLength ||
            BinaryPrimitives.ReadUInt32LittleEndian(data) != (uint)BodyRecordVersion.HalfPoseF32Velocity ||
            BinaryPrimitives.ReadUInt32LittleEndian(data[4..]) != (uint)BodyCandidateStatus.Committed ||
            BinaryPrimitives.ReadInt32LittleEndian(data[48..]) != (int)CellScale.Metres ||
            BinaryPrimitives.ReadInt32LittleEndian(data[52..]) != (int)TimeScale.Seconds ||
            BinaryPrimitives.ReadUInt32LittleEndian(data[44..]) != 0 ||
            BinaryPrimitives.ReadUInt16LittleEndian(data[62..]) != 0 ||
            BinaryPrimitives.ReadUInt32LittleEndian(data[76..]) != 0)
            throw new ArgumentException("Unsupported canonical body record.");
        var body = new CanonicalBody(
            new(BinaryPrimitives.ReadUInt64LittleEndian(data[24..])),
            BinaryPrimitives.ReadUInt64LittleEndian(data[8..]),
            BinaryPrimitives.ReadUInt64LittleEndian(data[16..]),
            new(BinaryPrimitives.ReadInt32LittleEndian(data[32..]), BinaryPrimitives.ReadInt32LittleEndian(data[36..]), BinaryPrimitives.ReadInt32LittleEndian(data[40..])),
            new(ReadHalf(data[56..]), ReadHalf(data[58..]), ReadHalf(data[60..])),
            new(BinaryPrimitives.ReadSingleLittleEndian(data[64..]), BinaryPrimitives.ReadSingleLittleEndian(data[68..]),
                BinaryPrimitives.ReadSingleLittleEndian(data[72..])));
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

    private static void ValidateAxis(int cell, Half local)
    {
        if (!Half.IsFinite(local) ||
            local < (Half)(-0.5) || local >= (Half)0.5 ||
            cell < -MaximumCell || cell > MaximumCell ||
            (cell == MaximumCell && local > (Half)0) ||
            (cell == -MaximumCell && local < (Half)0))
            throw new ArgumentOutOfRangeException("Canonical body axis exceeds admitted profile.");
    }

    private static void WriteHalf(Span<byte> data, Half value) =>
        BinaryPrimitives.WriteUInt16LittleEndian(data, BitConverter.HalfToUInt16Bits(value));
    private static Half ReadHalf(ReadOnlySpan<byte> data) =>
        BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReadUInt16LittleEndian(data));
}
