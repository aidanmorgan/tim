using System;
using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Runtime;

public sealed class CoreClockCodec : IClockCodec
{
    public byte[] EncodePeer(WorkshopClockPeer peer) => WorkshopClockWire.EncodePeer(peer);
    public WorkshopClockPeer DecodePeer(ReadOnlySpan<byte> data) => WorkshopClockWire.DecodePeer(data);
    public byte[] EncodeProbe(ClockProbe probe, WorkshopClockPeer peer) => WorkshopClockWire.EncodeProbe(probe, peer);
    public ClockProbe DecodeProbe(ReadOnlySpan<byte> data, WorkshopClockPeer peer) => WorkshopClockWire.DecodeProbe(data, peer);
    public byte[] EncodeReply(ReadOnlySpan<byte> encodedProbe, WorkshopClockPeer peer,
        MonotonicNanoseconds received, MonotonicNanoseconds sent, ClockDerivationEpoch derivation) => WorkshopClockWire.EncodeReply(encodedProbe, peer, received, sent, derivation);
    public ClockReply DecodeReply(ReadOnlySpan<byte> data, WorkshopClockPeer peer, MonotonicNanoseconds received) => WorkshopClockWire.DecodeReply(data, peer, received);
    public ClockObservation? ReceiveEncoded(IClockMapping mapping, ReadOnlySpan<byte> bytes, MonotonicNanoseconds received) => WorkshopClockWire.ReceiveEncoded(mapping, bytes, received);
    public void WriteReceipt(WorkshopResponse response, Span<byte> destination) => WorkshopClockWire.WriteReceipt(response, destination);
    public int PeerBytes => WorkshopClockWire.PeerBytes;
    public int ProbeBytes => WorkshopClockWire.ProbeBytes;
    public int ReplyBytes => WorkshopClockWire.ReplyBytes;
    public int ReceiptBytes => WorkshopClockWire.ReceiptBytes;
    public ClockDerivationExchange DecodeExchange(ReadOnlySpan<byte> bytes) => ClockDerivationWire.DecodeExchange(bytes);
    public byte[] EncodeCorrection(long correction, ClockDerivationOutcome outcome) => ClockDerivationWire.EncodeCorrection(correction, outcome);
    public int ExchangeBytes => ClockDerivationWire.ExchangeBytes;
    public int CorrectionBytes => ClockDerivationWire.CorrectionBytes;
}
