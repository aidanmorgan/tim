using System;
using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Runtime;

public sealed class CoreCommandCodec : ICommandCodec
{
    public void ValidateSaveFrame(ReadOnlySpan<byte> bytes) => WorkshopWireFlatBuffers.OpenFrame(bytes, "CCWS", CuriousContraptions.Wire.SavedConstructionVerify.Verify);
    public byte[] EncodeCommand(WorkshopCommand command) => WorkshopWireFlatBuffers.EncodeCommand(command);
    public WorkshopCommand DecodeCommand(ReadOnlySpan<byte> bytes) => WorkshopWireFlatBuffers.DecodeCommand(bytes);
    public byte[] ReadIdentity(WorkshopResponse response) => WorkshopWireFlatBuffers.ReadIdentity(response);
    public byte[] EncodeResponse(WorkshopResponse response) => WorkshopWireFlatBuffers.EncodeResponse(response);
    public WorkshopResponse DecodeResponse(ReadOnlySpan<byte> bytes) => WorkshopWireFlatBuffers.DecodeResponse(bytes);
    public string CommandIdentifier => WorkshopWireFlatBuffers.CommandIdentifier;
    public string ResponseIdentifier => WorkshopWireFlatBuffers.ResponseIdentifier;
    public byte[] EncodeObservations(WorkshopObservationBatch batch) => WorkshopWireFlatBuffers.EncodeObservations(batch);
    public WorkshopObservationBatch DecodeObservations(ReadOnlySpan<byte> bytes) => WorkshopWireFlatBuffers.DecodeObservations(bytes);
    public string ObservationIdentifier => WorkshopWireFlatBuffers.ObservationIdentifier;
    public IWorkshopResponseEncoder CreateResponseEncoder() => new CoreResponseEncoder();
}
