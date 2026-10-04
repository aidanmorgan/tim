using System;
using System.Buffers.Binary;
using CuriousContraptions.Gpu;
using Xunit;

namespace CuriousContraptions.Tests;

public sealed class CanonicalBodyTests
{
    public enum InvalidField { Version, Status, CellScale, TimeScale, Reserved, LocalPadding, VelocityPadding, LocalNan, VelocityInfinity, EmptyIdentity, OutwardPosition }
    private static CanonicalBody Body => new(new(0xfedcba9876543210), 0xfedcba9876543210, 0x20000000000001,
        new(160, -1024, 1024), new((Half)(-0.5), (Half)0, (Half)0),
        new((Half)(0.01 / 32), (Half)0, (Half)(-0.03125)));

    [Fact]
    public void CanonicalBitsAndIntegerIdentityRoundTripExactly()
    {
        var bytes = Body.Encode();
        Assert.Equal(80, bytes.Length);
        Assert.Equal(Body, CanonicalBody.Decode(bytes));
        Assert.Equal(bytes, CanonicalBody.Decode(bytes).Encode());
        Assert.Equal(0xfedcba9876543210UL, BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(24)));
    }

    [Theory]
    [InlineData(InvalidField.Version)]
    [InlineData(InvalidField.Status)]
    [InlineData(InvalidField.CellScale)]
    [InlineData(InvalidField.TimeScale)]
    [InlineData(InvalidField.Reserved)]
    [InlineData(InvalidField.LocalPadding)]
    [InlineData(InvalidField.VelocityPadding)]
    [InlineData(InvalidField.LocalNan)]
    [InlineData(InvalidField.VelocityInfinity)]
    [InlineData(InvalidField.EmptyIdentity)]
    [InlineData(InvalidField.OutwardPosition)]
    public void InvalidBoundaryRecordRejectsWithoutMutatingInput(InvalidField field)
    {
        var bytes = Body.Encode();
        switch (field)
        {
            case InvalidField.Version: bytes[0] = 255; break;
            case InvalidField.Status: bytes[4] = 255; break;
            case InvalidField.CellScale: bytes[48] = 0; break;
            case InvalidField.TimeScale: bytes[52] = 0; break;
            case InvalidField.Reserved: bytes[44] = 1; break;
            case InvalidField.LocalPadding: bytes[62] = 1; break;
            case InvalidField.VelocityPadding: bytes[70] = 1; break;
            case InvalidField.LocalNan: BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(56), 0x7e00); break;
            case InvalidField.VelocityInfinity: BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(64), 0x7c00); break;
            case InvalidField.EmptyIdentity: Array.Clear(bytes, 24, 8); break;
            case InvalidField.OutwardPosition: BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(60), BitConverter.HalfToUInt16Bits((Half)0.25)); break;
            default: throw new ArgumentOutOfRangeException();
        }
        var before = (byte[])bytes.Clone();
        Assert.ThrowsAny<ArgumentException>(() => CanonicalBody.Decode(bytes));
        Assert.Equal(before, bytes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(64)]
    [InlineData(79)]
    [InlineData(81)]
    public void WrongLengthRejects(int length) =>
        Assert.Throws<ArgumentException>(() => CanonicalBody.Decode(new byte[length]));

    [Fact]
    public void DecodedStateDoesNotAliasTransportStorage()
    {
        var bytes = Body.Encode();
        var decoded = CanonicalBody.Decode(bytes);
        Array.Clear(bytes);
        Assert.Equal(Body, decoded);
    }
}
