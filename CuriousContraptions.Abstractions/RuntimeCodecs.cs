using System;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Gpu;

public interface ICommandCodec
{
    void ValidateSaveFrame(ReadOnlySpan<byte> bytes);
    byte[] EncodeCommand(WorkshopCommand command);
    WorkshopCommand DecodeCommand(ReadOnlySpan<byte> bytes);
    byte[] ReadIdentity(WorkshopResponse response);
    byte[] EncodeResponse(WorkshopResponse response);
    WorkshopResponse DecodeResponse(ReadOnlySpan<byte> bytes);
    string CommandIdentifier { get; }
    string ResponseIdentifier { get; }
    byte[] EncodeObservations(WorkshopObservationBatch batch);
    WorkshopObservationBatch DecodeObservations(ReadOnlySpan<byte> bytes);
    string ObservationIdentifier { get; }
    IWorkshopResponseEncoder CreateResponseEncoder();
}

public interface IClockCodec
{
    byte[] EncodePeer(WorkshopClockPeer peer);
    WorkshopClockPeer DecodePeer(ReadOnlySpan<byte> data);
    byte[] EncodeProbe(ClockProbe probe, WorkshopClockPeer peer);
    ClockProbe DecodeProbe(ReadOnlySpan<byte> data, WorkshopClockPeer peer);
    byte[] EncodeReply(ReadOnlySpan<byte> encodedProbe, WorkshopClockPeer peer,
        MonotonicNanoseconds received, MonotonicNanoseconds sent, ClockDerivationEpoch derivation);
    ClockReply DecodeReply(ReadOnlySpan<byte> data, WorkshopClockPeer peer, MonotonicNanoseconds received);
    ClockObservation? ReceiveEncoded(IClockMapping mapping, ReadOnlySpan<byte> bytes, MonotonicNanoseconds received);
    void WriteReceipt(WorkshopResponse response, Span<byte> destination);
    int PeerBytes { get; }
    int ProbeBytes { get; }
    int ReplyBytes { get; }
    int ReceiptBytes { get; }
    ClockDerivationExchange DecodeExchange(ReadOnlySpan<byte> bytes);
    byte[] EncodeCorrection(long correction, ClockDerivationOutcome outcome);
    int ExchangeBytes { get; }
    int CorrectionBytes { get; }
}

public interface IAnimationCodec
{
    byte[] Control(RuntimeSessionId session, ClockGeneration master, CadenceRevision cadence,
        WorkshopAnimationControl control);
    WorkshopAnimationControl ReadControl(ReadOnlySpan<byte> bytes, RuntimeSessionId session,
        ClockGeneration master, CadenceRevision cadence);
    void Validate(WorkshopAnimationControl value);
    WorkshopAnimationSample Read(ReadOnlySpan<byte> bytes, RuntimeSessionId session,
        ClockGeneration master, CadenceRevision cadence, out AnimationOutputKind kind);
    bool IsChannel(AnimationProperty channel);
    AnimationValue Value(AnimationProperty channel, float value);
    int ControlBytes { get; }
    int OutputBytes { get; }
    int SessionOffset { get; }
    int MasterOffset { get; }
    int CadenceOffset { get; }
    int ControlSequenceOffset { get; }
    int ControlGenerationOffset { get; }
    int ControlKindOffset { get; }
    int ControlVisibleOffset { get; }
    int ControlTargetOffset { get; }
    int ControlWorldOffset { get; }
    int ControlFromOffset { get; }
    int ControlToOffset { get; }
    int ControlCurveOffset { get; }
    int ControlEventOrdinalOffset { get; }
    int ControlEventPhaseOffset { get; }
    int ControlDurationOffset { get; }
    int ControlPropertyOffset { get; }
    int OutputGenerationOffset { get; }
    int OutputPulseOffset { get; }
    int OutputAppliedAtOffset { get; }
    int OutputValueOffset { get; }
    int OutputKindOffset { get; }
    int OutputTargetOffset { get; }
    int OutputWorldOffset { get; }
    int OutputEventOrdinalOffset { get; }
    int OutputEventPhaseOffset { get; }
    int OutputPropertyOffset { get; }
}

public interface IScheduleCodec
{
    WorkshopCadenceSettings ReadCadence(ReadOnlySpan<byte> source);
    void WriteCadence(WorkshopCadenceSettings settings, Span<byte> destination);
    int CadenceBytes { get; }
    int PrepareBytes(int endpointBytes);
    int PayloadBytes(ScheduleControlKind kind);
    void WriteHeader(ScheduleControlHeader header, Span<byte> destination);
    void WriteHeader(ScheduleControlHeader header, Span<byte> destination, int payloadBytes);
    ScheduleControlHeader ReadHeader(ReadOnlySpan<byte> control);
    void WriteSchedule(WorkshopSchedule schedule, Span<byte> destination);
    WorkshopSchedule ReadSchedule(ReadOnlySpan<byte> source);
    void WriteCommit(ScheduleControlHeader header, WorkshopSchedule schedule, Span<byte> destination);
    WorkshopSchedule ReadCommit(ReadOnlySpan<byte> control, out ScheduleControlHeader header);
    int HeaderBytes { get; }
    int ScheduleBytes { get; }
    int PrepareFieldBytes { get; }
    int ResultBytes { get; }
    int AbortBytes { get; }
}

public interface IPoseRingCodec
{
    int Capacity(int bodies);
    int SlotBytes(int bodyCapacity);
    int TotalBytes(int bodyCapacity);
    int BodyCapacity(int ringBytes);
    int SlotOffset(int slot, int bodyCapacity);
    int SequenceOffset(int slot, int bodyCapacity);
    int TimestampOffset(int slot, int bodyCapacity);
    int BodyOffset(int slot, int bodyIndex, int bodyCapacity);
    byte[] ComposeSlot(long timestampNanoseconds, PhysicsBodyReadSet bodies);
    bool TryReadSlot(ReadOnlySpan<byte> ring, int slot, out ulong sequence, out long timestampNanoseconds, Span<PoseRingBody> bodies, out int bodyCount);
    int SlotCount { get; }
    int DefaultBodyCapacity { get; }
    int HeaderBytes { get; }
    int BodyBytes { get; }
}

public interface ICanonicalBodyCodec
{
    byte[] Encode(CanonicalBody body);
    void Write(CanonicalBody body, Span<byte> data);
    CanonicalBody Decode(ReadOnlySpan<byte> data);
}

public interface IRuntimeSessionCodec
{
    void WriteTo(RuntimeSessionId session, Span<byte> destination);
    RuntimeSessionId Read(ReadOnlySpan<byte> source);
}

public interface IWorkshopResponseEncoder
{
    byte[] EncodeResponse(WorkshopResponse response);
}

public interface IRuntimeCodecs
{
    ICommandCodec Commands { get; }
    IClockCodec Clocks { get; }
    IAnimationCodec Animation { get; }
    IScheduleCodec Schedules { get; }
    IPoseRingCodec Poses { get; }
    ICanonicalBodyCodec Bodies { get; }
    IRuntimeSessionCodec Sessions { get; }
}
