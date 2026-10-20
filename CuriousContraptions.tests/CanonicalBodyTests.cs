using System;
using System.Buffers.Binary;
using CuriousContraptions.Gpu;
using Xunit;

namespace CuriousContraptions.Tests;

public sealed class CanonicalBodyTests
{
    public enum InvalidField { LeadingPadding, Status, CellScale, TimeScale, Reserved, LocalInfinity, LocalBeyondRange, LocalNan, VelocityInfinity, VelocityNan, VelocityBeyondEnvelope, EmptyIdentity, OutwardPosition }
    // Velocity is f32 in m/s: 1e-7 m/s (far below a binary16 step at 1 m/s) survives the round trip.
    private static CanonicalBody Body => new(new(0xfedcba9876543210), 0xfedcba9876543210, 0x20000000000001,
        new(160, -1024, 1024), new((float)(-0.5), (float)0, (float)0),
        new(0.01f, 1e-7f, -1f));

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
    [InlineData(InvalidField.LeadingPadding)]
    [InlineData(InvalidField.Status)]
    [InlineData(InvalidField.CellScale)]
    [InlineData(InvalidField.TimeScale)]
    [InlineData(InvalidField.Reserved)]
    [InlineData(InvalidField.LocalInfinity)]
    [InlineData(InvalidField.LocalBeyondRange)]
    [InlineData(InvalidField.LocalNan)]
    [InlineData(InvalidField.VelocityInfinity)]
    [InlineData(InvalidField.VelocityNan)]
    [InlineData(InvalidField.VelocityBeyondEnvelope)]
    [InlineData(InvalidField.EmptyIdentity)]
    [InlineData(InvalidField.OutwardPosition)]
    public void InvalidBoundaryRecordRejectsWithoutMutatingInput(InvalidField field)
    {
        var bytes = Body.Encode();
        switch (field)
        {
            case InvalidField.LeadingPadding: bytes[0] = 255; break;
            case InvalidField.Status: bytes[4] = 255; break;
            case InvalidField.CellScale: bytes[48] = 0; break;
            case InvalidField.TimeScale: bytes[52] = 0; break;
            case InvalidField.Reserved: bytes[44] = 1; break;
            case InvalidField.LocalInfinity: BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(60), float.PositiveInfinity); break;
            case InvalidField.LocalBeyondRange: BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(56), .5f); break;
            case InvalidField.LocalNan: BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(56), float.NaN); break;
            case InvalidField.VelocityInfinity: BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(68), float.PositiveInfinity); break;
            case InvalidField.VelocityNan: BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(76), float.NaN); break;
            case InvalidField.VelocityBeyondEnvelope:
                // Each component is within 64 m/s but the vector is 70.7 m/s.
                BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(68), 50f);
                BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(72), 50f); break;
            case InvalidField.EmptyIdentity: Array.Clear(bytes, 24, 8); break;
            case InvalidField.OutwardPosition: BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(64), .25f); break;
            default: throw new ArgumentOutOfRangeException(nameof(field));
        }
        var before = (byte[])bytes.Clone();
        Assert.ThrowsAny<ArgumentException>(() => CanonicalBody.Decode(bytes));
        Assert.Equal(before, bytes);
    }

    [Theory]
    [InlineData(1u)]
    [InlineData(3u)] // the retired record version: a structurally invalid record now, never decoded as before
    public void TheLeadingWordIsZeroPaddingAndAnyOtherValueRejects(uint leading)
    {
        var bytes = Body.Encode();
        Assert.Equal(0u, BinaryPrimitives.ReadUInt32LittleEndian(bytes));
        Assert.Equal(Body, CanonicalBody.Decode(bytes)); // control
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, leading);
        Assert.Throws<ArgumentException>(() => CanonicalBody.Decode(bytes));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(64)]
    [InlineData(79)]
    [InlineData(81)]
    public void WrongLengthRejects(int length) =>
        Assert.Throws<ArgumentException>(() => CanonicalBody.Decode(new byte[length]));

    [Fact]
    public void VelocityIsF32MetresPerSecondAndTheEnvelopeEdgeIsAdmitted()
    {
        var bytes = (Body with { Velocity = new(64f, 0f, 0f) }).Encode();
        Assert.Equal(64f, BinaryPrimitives.ReadSingleLittleEndian(bytes.AsSpan(68)));
        Assert.Equal(1e-7f, BinaryPrimitives.ReadSingleLittleEndian(Body.Encode().AsSpan(72)));
        Assert.Equal(64f, CanonicalBody.Decode(bytes).Velocity.X);
        Assert.ThrowsAny<ArgumentException>(() => (Body with { Velocity = new(64.0001f, 0f, 0f) }).Encode());
    }

    [Fact]
    public void ZeroVelocityRetainsCanonicalPositiveZeroAndRejectsNegativeZeroIdentity()
    {
        // Only the current record is written; signed zero remains an exact state distinction.
        var bytes = (Body with { Velocity = default }).Encode();
        Assert.True(bytes.AsSpan(68, 12).IndexOfAnyExcept((byte)0) < 0);
        Assert.True(CanonicalBody.Decode(bytes).Velocity.IsPositiveZero);
        Assert.False((Body with { Velocity = new(-0f, 0f, 0f) }).Velocity.IsPositiveZero);
    }

    [Fact]
    public void DecodedStateDoesNotAliasTransportStorage()
    {
        var bytes = Body.Encode();
        var decoded = CanonicalBody.Decode(bytes);
        Array.Clear(bytes);
        Assert.Equal(Body, decoded);
    }
}
