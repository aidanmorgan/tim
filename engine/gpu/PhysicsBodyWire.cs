using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

/// <summary>Body-local payload; epoch and tick belong to the complete sample envelope.</summary>
/// <remarks>Layout: id 0..8, cell 8..20, local remainder (Half) 20..26, rotation (Half) 26..34, local centre of mass (Half)
/// 34..40, linear velocity m/s (f32) 40..52, angular velocity rad/s (f32) 52..64.</remarks>
public static class PhysicsBodyWire
{
    public const int ByteLength = 64;
    public const int VelocityOffset = 40;
    public const int AngularVelocityOffset = 52;
    public static void Write(PhysicsBodyRead value, Span<byte> bytes)
    {
        if (bytes.Length != ByteLength) throw new ArgumentException("Invalid body payload width.");
        value.Validate(); bytes.Clear();
        BinaryPrimitives.WriteUInt64LittleEndian(bytes, value.Body.Id.Value);
        BinaryPrimitives.WriteInt32LittleEndian(bytes[8..], value.Body.Cell.X);
        BinaryPrimitives.WriteInt32LittleEndian(bytes[12..], value.Body.Cell.Y);
        BinaryPrimitives.WriteInt32LittleEndian(bytes[16..], value.Body.Cell.Z);
        H(bytes,20,value.Body.Local.X); H(bytes,22,value.Body.Local.Y); H(bytes,24,value.Body.Local.Z);
        H(bytes,26,value.Rotation.X); H(bytes,28,value.Rotation.Y); H(bytes,30,value.Rotation.Z); H(bytes,32,value.Rotation.W);
        H(bytes,34,value.LocalCentreOfMass.X); H(bytes,36,value.LocalCentreOfMass.Y); H(bytes,38,value.LocalCentreOfMass.Z);
        F(bytes,VelocityOffset,value.Body.Velocity.X); F(bytes,VelocityOffset+4,value.Body.Velocity.Y); F(bytes,VelocityOffset+8,value.Body.Velocity.Z);
        F(bytes,AngularVelocityOffset,value.AngularVelocity.X); F(bytes,AngularVelocityOffset+4,value.AngularVelocity.Y);
        F(bytes,AngularVelocityOffset+8,value.AngularVelocity.Z);
    }

    public static PhysicsBodyRead Read(ReadOnlySpan<byte> bytes, SimulationEpoch epoch, SimulationTick tick)
    {
        if (bytes.Length != ByteLength || epoch.Value == 0)
            throw new ArgumentException("Invalid body payload width or time.");
        var body = new CanonicalBody(new(BinaryPrimitives.ReadUInt64LittleEndian(bytes)), epoch.Value, tick.Value,
            new(BinaryPrimitives.ReadInt32LittleEndian(bytes[8..]), BinaryPrimitives.ReadInt32LittleEndian(bytes[12..]),
                BinaryPrimitives.ReadInt32LittleEndian(bytes[16..])),
            new(H(bytes,20),H(bytes,22),H(bytes,24)),
            new(F(bytes,VelocityOffset),F(bytes,VelocityOffset+4),F(bytes,VelocityOffset+8)));
        var value = new PhysicsBodyRead(body, new(H(bytes,26),H(bytes,28),H(bytes,30),H(bytes,32)),
            new(F(bytes,AngularVelocityOffset),F(bytes,AngularVelocityOffset+4),F(bytes,AngularVelocityOffset+8)),
            new(H(bytes,34),H(bytes,36),H(bytes,38)));
        value.Validate(); return value;
    }

    private static Half H(ReadOnlySpan<byte> bytes,int offset) =>
        BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReadUInt16LittleEndian(bytes[offset..]));
    private static void H(Span<byte> bytes,int offset,Half value) =>
        BinaryPrimitives.WriteUInt16LittleEndian(bytes[offset..],BitConverter.HalfToUInt16Bits(value));
    private static float F(ReadOnlySpan<byte> bytes,int offset) => BinaryPrimitives.ReadSingleLittleEndian(bytes[offset..]);
    private static void F(Span<byte> bytes,int offset,float value) => BinaryPrimitives.WriteSingleLittleEndian(bytes[offset..],value);
}
