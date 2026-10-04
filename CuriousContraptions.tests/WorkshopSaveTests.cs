using System.Buffers.Binary;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class WorkshopSaveTests
{
    private static WorkshopSavedConstruction Saved()
    {
        var ball = WorkshopInput.Basketball(new(2), .125, 3, -.25, 0, 0, 0, 1);
        var receiver = WorkshopInput.Receiver(new(3), .5, 1, -.5, 0, 0, 0, 1);
        return new(new(new(9), ball, WorkshopCadenceSettings.Default(), receiver), new(7));
    }

    [Fact]
    public void CanonicalBitsAndAllocatorRoundTripWithoutLiveState()
    {
        var saved = Saved();
        var bytes = WorkshopSaveCodec.Encode(saved);
        Assert.Equal(WorkshopSaveCodec.ByteLength, bytes.Length);
        Assert.Equal(saved, WorkshopSaveCodec.Decode(bytes));
        Assert.Equal(bytes, WorkshopSaveCodec.Encode(WorkshopSaveCodec.Decode(bytes)));
        var empty = new WorkshopSavedConstruction(new(new(1), null, WorkshopCadenceSettings.Default()), new(1));
        Assert.Equal(empty, WorkshopSaveCodec.Decode(WorkshopSaveCodec.Encode(empty)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(12)]
    public void UnknownHeaderCannotBeLoaded(int offset)
    {
        var bytes = WorkshopSaveCodec.Encode(Saved());
        bytes[offset] ^= 128;
        Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(bytes));
    }

    [Fact]
    public void TruncatedTrailingAndInvalidAllocatorReject()
    {
        var bytes = WorkshopSaveCodec.Encode(Saved());
        Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(bytes.AsSpan(1)));
        Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode([..bytes, 0]));
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(16), 3);
        Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(bytes));
        Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Encode(Saved() with { NextBodyId = new(3) }));
    }

    [Theory]
    [InlineData(32)]
    [InlineData(184)]
    public void UnsupportedPartPopulationRejectsBeforeAdmission(int offset)
    {
        var bytes = WorkshopSaveCodec.Encode(Saved());
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), 2);
        Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(bytes));
    }

    [Fact]
    public void SaveCommandRequiresExactRevisionAndRoundTrips()
    {
        var command = new WorkshopCommand(new(5), WorkshopCommandKind.Save, new(2), new(8), null,
            Session: new(1, 2), Cadence: new(1), Projection: new(2));
        var current = WorkshopWire.Encode(command);
        Assert.Equal(command, WorkshopWire.DecodeCommand(current));
        BinaryPrimitives.WriteUInt32LittleEndian(current.AsSpan(12), 7);
        Assert.Throws<ArgumentException>(() => WorkshopWire.DecodeCommand(current));
        Assert.Throws<ArgumentException>(() => WorkshopWire.Encode(command with
            { RevisionKind = ExpectedRevisionKind.Any, Revision = default }));
    }
}
