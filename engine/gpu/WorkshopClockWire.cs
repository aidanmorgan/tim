using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

/// <summary>Profile 2 asserts the witness-qualified isolated performance.now() source, not a pinned browser build.</summary>
public enum NativeClockProfile : uint { IsolatedWitnessedPerformanceNow = 2 }

/// <summary>Exact native-clock control boundary; no clock timestamp is converted to a JS Number.</summary>
public static class WorkshopClockWire
{
    public const int PeerBytes = 64;
    public const int ProbeBytes = 80;
    /// <summary>The probe, the two master stamps and (Story 9.4b) the master derivation epoch they were taken under. A peer's
    /// bytes 0..3 are zero padding (one bundle, one schema).</summary>
    public const int ReplyBytes = 104;
    public const int ReceiptBytes = 24;

    public static byte[] EncodePeer(WorkshopClockPeer peer)
    {
        peer.Validate();
        if (peer.Uncertainty.Value != 100_000) throw new ArgumentException("Unsupported isolated native clock profile.");
        var bytes = new byte[PeerBytes]; var data = bytes.AsSpan();
        BinaryPrimitives.WriteUInt32LittleEndian(data[4..], (uint)NativeClockProfile.IsolatedWitnessedPerformanceNow);
        WorkshopWire.WriteSession(data[8..24], peer.Session);
        BinaryPrimitives.WriteUInt64LittleEndian(data[24..], peer.Generation.Value);
        BinaryPrimitives.WriteInt64LittleEndian(data[32..], peer.MasterOrigin.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(data[40..], peer.RequesterGeneration.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(data[48..], peer.Uncertainty.Value);
        BinaryPrimitives.WriteUInt32LittleEndian(data[56..], (uint)peer.RequesterRole);
        BinaryPrimitives.WriteUInt32LittleEndian(data[60..], (uint)WorkshopRuntimeRole.Simulation);
        return bytes;
    }

    public static WorkshopClockPeer DecodePeer(ReadOnlySpan<byte> data)
    {
        if (data.Length != PeerBytes || BinaryPrimitives.ReadUInt32LittleEndian(data) != 0 ||
            (NativeClockProfile)BinaryPrimitives.ReadUInt32LittleEndian(data[4..]) != NativeClockProfile.IsolatedWitnessedPerformanceNow ||
            (WorkshopRuntimeRole)BinaryPrimitives.ReadUInt32LittleEndian(data[60..]) != WorkshopRuntimeRole.Simulation)
            throw new ArgumentException("Unsupported clock bootstrap.");
        var peer = new WorkshopClockPeer(WorkshopWire.ReadSession(data[8..24]),
            new(BinaryPrimitives.ReadUInt64LittleEndian(data[24..])),
            new(BinaryPrimitives.ReadInt64LittleEndian(data[32..])),
            new(BinaryPrimitives.ReadUInt64LittleEndian(data[40..])),
            new(BinaryPrimitives.ReadUInt64LittleEndian(data[48..])),
            (WorkshopRuntimeRole)BinaryPrimitives.ReadUInt32LittleEndian(data[56..]));
        peer.Validate();
        if (peer.Uncertainty.Value != 100_000) throw new ArgumentException("Unknown native precision bound.");
        return peer;
    }

    public static byte[] EncodeProbe(ClockProbe probe, WorkshopClockPeer peer)
    {
        if (probe.Session != peer.Session || probe.Generation != peer.Generation || probe.RequesterGeneration != peer.RequesterGeneration ||
            probe.RequesterRole != peer.RequesterRole || probe.Sequence.Value == 0)
            throw new ArgumentException("Probe does not own this native clock.");
        probe.RequesterSent.Validate();
        var bytes = new byte[ProbeBytes];
        var bootstrap = EncodePeer(peer);
        bootstrap.CopyTo(bytes, 0);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(64), probe.Sequence.Value);
        BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(72), probe.RequesterSent.Value);
        return bytes;
    }

    public static ClockProbe DecodeProbe(ReadOnlySpan<byte> data, WorkshopClockPeer peer)
    {
        if (data.Length != ProbeBytes) throw new ArgumentException("Invalid clock probe length.");
        var declared = DecodePeer(data[..PeerBytes]);
        if (declared != peer) throw new ArgumentException("Probe belongs to another session or native clock.");
        var probe = new ClockProbe(peer.Session, peer.Generation, peer.RequesterGeneration, peer.RequesterRole,
            new(BinaryPrimitives.ReadUInt64LittleEndian(data[64..])), new(BinaryPrimitives.ReadInt64LittleEndian(data[72..])));
        if (probe.Sequence.Value == 0) throw new ArgumentException("Probe identity must be positive.");
        probe.RequesterSent.Validate(); return probe;
    }

    public static byte[] EncodeReply(ReadOnlySpan<byte> encodedProbe, WorkshopClockPeer peer,
        MonotonicNanoseconds received, MonotonicNanoseconds sent, ClockDerivationEpoch derivation)
    {
        DecodeProbe(encodedProbe, peer);
        received.Validate(); sent.Validate();
        if (received.Value < peer.MasterOrigin.Value || sent.Value < received.Value)
            throw new ArgumentException("Master reply predates its origin or reverses time.");
        var bytes = new byte[ReplyBytes]; encodedProbe.CopyTo(bytes);
        BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(80), received.Value);
        BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(88), sent.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(96), derivation.Value);
        return bytes;
    }

    public static ClockReply DecodeReply(ReadOnlySpan<byte> data, WorkshopClockPeer peer, MonotonicNanoseconds received)
    {
        if (data.Length != ReplyBytes) throw new ArgumentException("Invalid clock reply length.");
        var probe = DecodeProbe(data[..ProbeBytes], peer);
        var masterReceived = new MonotonicNanoseconds(BinaryPrimitives.ReadInt64LittleEndian(data[80..]));
        var masterSent = new MonotonicNanoseconds(BinaryPrimitives.ReadInt64LittleEndian(data[88..]));
        if (masterReceived.Value < peer.MasterOrigin.Value || masterSent.Value < masterReceived.Value)
            throw new ArgumentException("Master reply predates its origin or reverses time.");
        var derivation = BinaryPrimitives.ReadUInt64LittleEndian(data[96..]);
        if (derivation > uint.MaxValue) throw new ArgumentException($"Clock reply derivation epoch {derivation} is out of range.");
        return new(probe, masterReceived, masterSent, received, new((uint)derivation));
    }

    /// <summary>
    /// The requesters' one reply path (Browser and Animation): decodes the encoded reply against the mapping's own peer
    /// and receives it. A reply this decoder rejects returns null after exactly one counted, typed rejection.
    /// </summary>
    public static ClockObservation? ReceiveEncoded(this WorkshopClockMapping mapping, ReadOnlySpan<byte> bytes, MonotonicNanoseconds received)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        ClockReply reply;
        try { reply = DecodeReply(bytes, mapping.Peer, received); }
        catch (ArgumentException error) { mapping.RejectMalformedReply(bytes.Length, received, error.Message); return null; }
        return mapping.Receive(reply);
    }

    /// <summary>A diagnostic record keeps the exact reply 0..103 (including its derivation epoch), then the requester receipt,
    /// the outcome, the received length and the offset interval.</summary>
    public const int DiagnosticBytes = 152;
    public const int DiagnosticReceiptOffset = ReplyBytes;
    public const int DiagnosticOutcomeOffset = DiagnosticReceiptOffset + 8;
    public const int DiagnosticLengthOffset = DiagnosticOutcomeOffset + 4;
    public const int DiagnosticLowerOffset = DiagnosticLengthOffset + 4;
    public const int DiagnosticUpperOffset = DiagnosticLowerOffset + 16;
    public static byte[] EncodeRejectedReply(ReadOnlySpan<byte> reply, MonotonicNanoseconds received)
    {
        received.Validate();
        var data = new byte[DiagnosticBytes];
        reply[..Math.Min(reply.Length, ReplyBytes)].CopyTo(data);
        BinaryPrimitives.WriteInt64LittleEndian(data.AsSpan(DiagnosticReceiptOffset), received.Value);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(DiagnosticOutcomeOffset), (int)ClockProbeOutcome.Malformed);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(DiagnosticLengthOffset), reply.Length);
        return data; // At most one native reply is retained; declared length records any truncation.
    }

    public static byte[] EncodeExpiredProbe(ClockProbe probe, WorkshopClockPeer peer, MonotonicNanoseconds now)
    {
        var data = new byte[DiagnosticBytes];
        EncodeProbe(probe, peer).CopyTo(data, 0);
        BinaryPrimitives.WriteInt64LittleEndian(data.AsSpan(DiagnosticReceiptOffset), now.Value);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(DiagnosticOutcomeOffset), (int)ClockProbeOutcome.Expired);
        return data; // Worker timestamps are unavailable; this outcome records a local expiry.
    }

    public static byte[] EncodeObservation(ClockObservation observation, WorkshopClockPeer peer)
    {
        var reply = observation.Reply;
        var data = new byte[DiagnosticBytes];
        var probe = EncodeProbe(reply.Probe, peer);
        probe.CopyTo(data, 0);
        BinaryPrimitives.WriteInt64LittleEndian(data.AsSpan(80), reply.MasterReceived.Value);
        BinaryPrimitives.WriteInt64LittleEndian(data.AsSpan(88), reply.MasterSent.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan(96), reply.Derivation.Value);
        BinaryPrimitives.WriteInt64LittleEndian(data.AsSpan(DiagnosticReceiptOffset), reply.RequesterReceived.Value);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(DiagnosticOutcomeOffset), (int)observation.Outcome);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(DiagnosticLengthOffset), ReplyBytes);
        BinaryPrimitives.WriteInt128LittleEndian(data.AsSpan(DiagnosticLowerOffset), observation.Offset.Lower);
        BinaryPrimitives.WriteInt128LittleEndian(data.AsSpan(DiagnosticUpperOffset), observation.Offset.Upper);
        return data;
    }

    public static void WriteReceipt(WorkshopResponse response, Span<byte> destination)
    {
        if (destination.Length != ReceiptBytes || response.Kind != WorkshopResponseKind.Read || response.Publication.Value == 0)
            throw new ArgumentException("Invalid physical publication receipt.");
        WorkshopWire.WriteSession(destination, response.Session);
        BinaryPrimitives.WriteUInt64LittleEndian(destination[16..], response.Publication.Value);
    }
}
