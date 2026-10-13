using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

/// <summary>Body-local payload; epoch and tick belong to the complete sample envelope.</summary>
/// <remarks>F32 position at20, rotation32, Half COM48, f32 velocities56/68; bytes54..56 are zero.</remarks>
public static class PhysicsBodyWire
{
    public const int ByteLength = 80;
    public const int VelocityOffset = 56;
    public const int AngularVelocityOffset = 68;
    public static void Write(PhysicsBodyRead value, Span<byte> bytes)
    {
        if (bytes.Length != ByteLength) throw new ArgumentException("Invalid body payload width.");
        value.Validate(); bytes.Clear();
        BinaryPrimitives.WriteUInt64LittleEndian(bytes, value.Body.Id.Value);
        BinaryPrimitives.WriteInt32LittleEndian(bytes[8..], value.Body.Cell.X);
        BinaryPrimitives.WriteInt32LittleEndian(bytes[12..], value.Body.Cell.Y);
        BinaryPrimitives.WriteInt32LittleEndian(bytes[16..], value.Body.Cell.Z);
        F(bytes,20,value.Body.Local.X); F(bytes,24,value.Body.Local.Y); F(bytes,28,value.Body.Local.Z);
        F(bytes,32,value.Rotation.X); F(bytes,36,value.Rotation.Y); F(bytes,40,value.Rotation.Z); F(bytes,44,value.Rotation.W);
        H(bytes,48,value.LocalCentreOfMass.X); H(bytes,50,value.LocalCentreOfMass.Y); H(bytes,52,value.LocalCentreOfMass.Z);
        F(bytes,VelocityOffset,value.Body.Velocity.X); F(bytes,VelocityOffset+4,value.Body.Velocity.Y); F(bytes,VelocityOffset+8,value.Body.Velocity.Z);
        F(bytes,AngularVelocityOffset,value.AngularVelocity.X); F(bytes,AngularVelocityOffset+4,value.AngularVelocity.Y);
        F(bytes,AngularVelocityOffset+8,value.AngularVelocity.Z);
    }

    public static PhysicsBodyRead Read(ReadOnlySpan<byte> bytes, SimulationEpoch epoch, SimulationTick tick)
    {
        if (bytes.Length != ByteLength || epoch.Value == 0 || BinaryPrimitives.ReadUInt16LittleEndian(bytes[54..]) != 0)
            throw new ArgumentException("Invalid body payload width or time.");
        var body = new CanonicalBody(new(BinaryPrimitives.ReadUInt64LittleEndian(bytes)), epoch.Value, tick.Value,
            new(BinaryPrimitives.ReadInt32LittleEndian(bytes[8..]), BinaryPrimitives.ReadInt32LittleEndian(bytes[12..]),
                BinaryPrimitives.ReadInt32LittleEndian(bytes[16..])),
            new(F(bytes,20),F(bytes,24),F(bytes,28)),
            new(F(bytes,VelocityOffset),F(bytes,VelocityOffset+4),F(bytes,VelocityOffset+8)));
        var value = new PhysicsBodyRead(body, new(F(bytes,32),F(bytes,36),F(bytes,40),F(bytes,44)),
            new(F(bytes,AngularVelocityOffset),F(bytes,AngularVelocityOffset+4),F(bytes,AngularVelocityOffset+8)),
            new(H(bytes,48),H(bytes,50),H(bytes,52)));
        value.Validate(); return value;
    }

    private static Half H(ReadOnlySpan<byte> bytes,int offset) =>
        BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReadUInt16LittleEndian(bytes[offset..]));
    private static void H(Span<byte> bytes,int offset,Half value) =>
        BinaryPrimitives.WriteUInt16LittleEndian(bytes[offset..],BitConverter.HalfToUInt16Bits(value));
    private static float F(ReadOnlySpan<byte> bytes,int offset) => BinaryPrimitives.ReadSingleLittleEndian(bytes[offset..]);
    private static void F(Span<byte> bytes,int offset,float value) => BinaryPrimitives.WriteSingleLittleEndian(bytes[offset..],value);
}
