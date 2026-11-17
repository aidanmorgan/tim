using System;
using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Numerics;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Gpu;

/// <summary>Atomic geometry payload on the existing animation output channel.</summary>
public static class MechanicalLinkWire
{
    private const int PointsOffset = 128;
    public static byte[] Encode(RuntimeSessionId session, ClockGeneration master, CadenceRevision cadence, MechanicalLinkSample sample)
    {
        sample.Geometry.Validate(); sample.AppliedAt.Validate();
        var bytes = new byte[checked(PointsOffset + sample.Geometry.Points.Length * 12)];
        session.WriteTo(bytes.AsSpan(0, 16));
        U64(bytes, 16, master.Value); U64(bytes, 24, cadence.Value);
        U64(bytes, 32, sample.Generation); U64(bytes, 40, sample.Pulse.Value);
        BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(48), sample.AppliedAt.Value);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(60), (uint)AnimationOutputKind.Geometry);
        U64(bytes, 64, sample.Geometry.Target.Value); U64(bytes, 72, sample.World.Value);
        U64(bytes, 80, sample.Geometry.Tick.Value);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(88), sample.Geometry.Points.Length);
        Vector(bytes, 104, sample.Geometry.SourceMarker); Vector(bytes, 116, sample.Geometry.TargetMarker);
        for (var i = 0; i < sample.Geometry.Points.Length; i++) Vector(bytes, PointsOffset + 12 * i, sample.Geometry.Points[i]);
        return bytes;
    }
    public static MechanicalLinkSample Decode(ReadOnlySpan<byte> bytes, RuntimeSessionId session, ClockGeneration master, CadenceRevision cadence)
    {
        if (bytes.Length < PointsOffset || RuntimeSessionCodec.Read(bytes[..16]) != session ||
            U64(bytes, 16) != master.Value || U64(bytes, 24) != cadence.Value ||
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[60..]) != (uint)AnimationOutputKind.Geometry ||
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[56..]) != 0 || bytes.Slice(92, 12).IndexOfAnyExcept((byte)0) >= 0)
            throw new ArgumentException("Invalid geometry output identity or header.");
        var count = BinaryPrimitives.ReadInt32LittleEndian(bytes[88..]);
        if (count < 5 || (long)PointsOffset + 12L * count != bytes.Length)
            throw new ArgumentException("Invalid geometry output length.");
        var points = ImmutableArray.CreateBuilder<Vector3>(count);
        for (var i = 0; i < count; i++) points.Add(Vector(bytes, PointsOffset + 12 * i));
        var geometry = new MechanicalLinkGeometry(new(U64(bytes, 64)), new(U64(bytes, 80)), points.MoveToImmutable(),
            Vector(bytes, 104), Vector(bytes, 116));
        geometry.Validate();
        var sample = new MechanicalLinkSample(new(U64(bytes, 72)), U64(bytes, 32), new(U64(bytes, 40)),
            new(BinaryPrimitives.ReadInt64LittleEndian(bytes[48..])), geometry);
        sample.AppliedAt.Validate();
        if (sample.World.Value == 0 || sample.Generation == 0 || sample.Pulse.Value == 0)
            throw new ArgumentException("Absent geometry sample generation.");
        return sample;
    }
    private static void U64(Span<byte> bytes, int offset, ulong value) => BinaryPrimitives.WriteUInt64LittleEndian(bytes[offset..], value);
    private static ulong U64(ReadOnlySpan<byte> bytes, int offset) => BinaryPrimitives.ReadUInt64LittleEndian(bytes[offset..]);
    private static void Vector(Span<byte> bytes, int offset, Vector3 value)
    {
        BinaryPrimitives.WriteSingleLittleEndian(bytes[offset..], value.X);
        BinaryPrimitives.WriteSingleLittleEndian(bytes[(offset + 4)..], value.Y);
        BinaryPrimitives.WriteSingleLittleEndian(bytes[(offset + 8)..], value.Z);
    }
    private static Vector3 Vector(ReadOnlySpan<byte> bytes, int offset) => new(BinaryPrimitives.ReadSingleLittleEndian(bytes[offset..]),
        BinaryPrimitives.ReadSingleLittleEndian(bytes[(offset + 4)..]), BinaryPrimitives.ReadSingleLittleEndian(bytes[(offset + 8)..]));
}
