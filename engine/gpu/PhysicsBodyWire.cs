using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

/// <summary>Body-local payload; epoch and tick belong to the complete sample envelope.</summary>
public static class PhysicsBodyWire
{
    public const int ByteLength = 56;
    public static void Write(PhysicsBodyRead value, Span<byte> bytes)
    {
        if (bytes.Length != ByteLength) throw new ArgumentException("Invalid body payload width.");
        value.Validate(); bytes.Clear();
        BinaryPrimitives.WriteUInt64LittleEndian(bytes, value.Body.Id.Value);
        BinaryPrimitives.WriteInt32LittleEndian(bytes[8..], value.Body.Cell.X);
        BinaryPrimitives.WriteInt32LittleEndian(bytes[12..], value.Body.Cell.Y);
        BinaryPrimitives.WriteInt32LittleEndian(bytes[16..], value.Body.Cell.Z);
        H(bytes,20,value.Body.Local.X); H(bytes,22,value.Body.Local.Y); H(bytes,24,value.Body.Local.Z);
        H(bytes,26,value.Body.Velocity.X); H(bytes,28,value.Body.Velocity.Y); H(bytes,30,value.Body.Velocity.Z);
        H(bytes,32,value.Rotation.X); H(bytes,34,value.Rotation.Y); H(bytes,36,value.Rotation.Z); H(bytes,38,value.Rotation.W);
        H(bytes,40,value.AngularVelocity.X); H(bytes,42,value.AngularVelocity.Y); H(bytes,44,value.AngularVelocity.Z);
        H(bytes,46,value.LocalCentreOfMass.X); H(bytes,48,value.LocalCentreOfMass.Y); H(bytes,50,value.LocalCentreOfMass.Z);
    }

    public static PhysicsBodyRead Read(ReadOnlySpan<byte> bytes, SimulationEpoch epoch, SimulationTick tick)
    {
        if (bytes.Length != ByteLength || BinaryPrimitives.ReadUInt32LittleEndian(bytes[52..]) != 0 || epoch.Value == 0)
            throw new ArgumentException("Invalid body payload width, time or reserved bytes.");
        var body = new CanonicalBody(new(BinaryPrimitives.ReadUInt64LittleEndian(bytes)), epoch.Value, tick.Value,
            new(BinaryPrimitives.ReadInt32LittleEndian(bytes[8..]), BinaryPrimitives.ReadInt32LittleEndian(bytes[12..]),
                BinaryPrimitives.ReadInt32LittleEndian(bytes[16..])),
            new(H(bytes,20),H(bytes,22),H(bytes,24)), new(H(bytes,26),H(bytes,28),H(bytes,30)));
        var value = new PhysicsBodyRead(body, new(H(bytes,32),H(bytes,34),H(bytes,36),H(bytes,38)),
            new(H(bytes,40),H(bytes,42),H(bytes,44)), new(H(bytes,46),H(bytes,48),H(bytes,50)));
        value.Validate(); return value;
    }

    private static Half H(ReadOnlySpan<byte> bytes,int offset) =>
        BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReadUInt16LittleEndian(bytes[offset..]));
    private static void H(Span<byte> bytes,int offset,Half value) =>
        BinaryPrimitives.WriteUInt16LittleEndian(bytes[offset..],BitConverter.HalfToUInt16Bits(value));
}
