using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

/// <summary>Body-local payload; epoch and tick belong to the complete sample envelope.</summary>
/// <remarks>Identity at 0, cell at 8, then f32 position 20, rotation 32, centre of mass 48 and velocities 60/72; bytes 84..96 are
/// zero (Story 9A.44: every lane f32).</remarks>
public static class PhysicsBodyWire
{
    public const int ByteLength = 96;
    public const int IdentityOffset = 0;
    public const int CellOffset = 8;
    public const int LocalOffset = 20;
    public const int RotationOffset = 32;
    public const int CentreOfMassOffset = 48;
    public const int VelocityOffset = 60;
    public const int AngularVelocityOffset = 72;
    public const int PaddingOffset = 84;
    public static void Write(PhysicsBodyRead value, Span<byte> bytes)
    {
        if (bytes.Length != ByteLength) throw new ArgumentException("Invalid body payload width.");
        value.Validate(); bytes.Clear();
        BinaryPrimitives.WriteUInt64LittleEndian(bytes[IdentityOffset..], value.Body.Id.Value);
        BinaryPrimitives.WriteInt32LittleEndian(bytes[CellOffset..], value.Body.Cell.X);
        BinaryPrimitives.WriteInt32LittleEndian(bytes[(CellOffset + 4)..], value.Body.Cell.Y);
        BinaryPrimitives.WriteInt32LittleEndian(bytes[(CellOffset + 8)..], value.Body.Cell.Z);
        Vector(bytes, LocalOffset, value.Body.Local.X, value.Body.Local.Y, value.Body.Local.Z);
        Vector(bytes, RotationOffset, value.Rotation.X, value.Rotation.Y, value.Rotation.Z); F(bytes, RotationOffset + PhysicsGpuAbi.RotationWOffset, value.Rotation.W);
        Vector(bytes, CentreOfMassOffset, value.LocalCentreOfMass.X, value.LocalCentreOfMass.Y, value.LocalCentreOfMass.Z);
        Vector(bytes, VelocityOffset, value.Body.Velocity.X, value.Body.Velocity.Y, value.Body.Velocity.Z);
        Vector(bytes, AngularVelocityOffset, value.AngularVelocity.X, value.AngularVelocity.Y, value.AngularVelocity.Z);
    }

    public static PhysicsBodyRead Read(ReadOnlySpan<byte> bytes, SimulationEpoch epoch, SimulationTick tick)
    {
        if (bytes.Length != ByteLength || epoch.Value == 0 || bytes[PaddingOffset..].IndexOfAnyExcept((byte)0) >= 0)
            throw new ArgumentException("Invalid body payload width or time.");
        var body = new CanonicalBody(new(BinaryPrimitives.ReadUInt64LittleEndian(bytes[IdentityOffset..])), epoch.Value, tick.Value,
            new(BinaryPrimitives.ReadInt32LittleEndian(bytes[CellOffset..]), BinaryPrimitives.ReadInt32LittleEndian(bytes[(CellOffset + 4)..]),
                BinaryPrimitives.ReadInt32LittleEndian(bytes[(CellOffset + 8)..])),
            new(F(bytes,LocalOffset),F(bytes,LocalOffset+4),F(bytes,LocalOffset+8)),
            new(F(bytes,VelocityOffset),F(bytes,VelocityOffset+4),F(bytes,VelocityOffset+8)));
        var value = new PhysicsBodyRead(body, new(F(bytes,RotationOffset),F(bytes,RotationOffset+PhysicsGpuAbi.RotationYOffset),
            F(bytes,RotationOffset+PhysicsGpuAbi.RotationZOffset),F(bytes,RotationOffset+PhysicsGpuAbi.RotationWOffset)),
            new(F(bytes,AngularVelocityOffset),F(bytes,AngularVelocityOffset+4),F(bytes,AngularVelocityOffset+8)),
            new(F(bytes,CentreOfMassOffset),F(bytes,CentreOfMassOffset+4),F(bytes,CentreOfMassOffset+8)));
        value.Validate(); return value;
    }

    private static void Vector(Span<byte> bytes,int offset,float x,float y,float z)
    {
        F(bytes,offset,x); F(bytes,offset+4,y); F(bytes,offset+8,z);
    }
    private static float F(ReadOnlySpan<byte> bytes,int offset) => BinaryPrimitives.ReadSingleLittleEndian(bytes[offset..]);
    private static void F(Span<byte> bytes,int offset,float value) => BinaryPrimitives.WriteSingleLittleEndian(bytes[offset..],value);
}
