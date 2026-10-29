using System;
using System.Collections.Immutable;
using System.Linq;
using Google.FlatBuffers;
using W = CuriousContraptions.Wire;

namespace CuriousContraptions.Gpu;

public readonly record struct WorkshopObservationBatch(RuntimeSessionId Session, WorkshopRuntimeRole Role,
    ImmutableArray<WorkshopDiagnostic> Records, string? Incomplete = null);

public static partial class WorkshopWireFlatBuffers
{
    public const string ObservationIdentifier = "CCWO";

    public static byte[] EncodeObservations(WorkshopObservationBatch batch)
    {
        batch.Session.Validate();
        if (!Enum.IsDefined(batch.Role) || batch.Records.IsDefault) throw new ArgumentException("Invalid observation batch.");
        var builder = new FlatBufferBuilder(1024);
        var offsets = new Offset<W.DiagnosticObservation>[batch.Records.Length];
        for (var i = 0; i < offsets.Length; i++) offsets[i] = EncodeDiagnostic(builder, batch.Records[i]);
        var records = W.WorkshopObservationBatch.CreateRecordsVector(builder, offsets);
        var identity = new byte[16]; batch.Session.WriteTo(identity);
        var session = W.WorkshopObservationBatch.CreateSessionVector(builder, identity);
        var incomplete = batch.Incomplete is { } detail ? builder.CreateString(detail) : default;
        var root = W.WorkshopObservationBatch.CreateWorkshopObservationBatch(builder, session, (uint)batch.Role, records, incomplete);
        builder.FinishSizePrefixed(root.Value, ObservationIdentifier);
        return builder.SizedByteArray();
    }

    public static WorkshopObservationBatch DecodeObservations(ReadOnlySpan<byte> bytes)
    {
        try
        {
            var root = W.WorkshopObservationBatch.GetRootAsWorkshopObservationBatch(
                OpenFrame(bytes, ObservationIdentifier, W.WorkshopObservationBatchVerify.Verify));
            var session = RuntimeSessionId.Read(root.GetSessionArray());
            var role = ObservationEnum<WorkshopRuntimeRole>(root.Role);
            var records = ImmutableArray.CreateBuilder<WorkshopDiagnostic>(root.RecordsLength);
            for (var i = 0; i < root.RecordsLength; i++)
            {
                var value = DecodeDiagnostic(root.Records(i) ?? throw new ArgumentException("Missing diagnostic."));
                ValidateDiagnostic(value);
                if (value is WorkshopTransferDiagnostic transfer && transfer.Response.Session != session)
                    throw new ArgumentException("Foreign transfer session.");
                if (value is ClockRecalibrationDiagnostic clock && clock.Peer.Session != session)
                    throw new ArgumentException("Foreign clock session.");
                if (value is WorkshopCaptureDiagnostic capture && capture.Session != session)
                    throw new ArgumentException("Foreign capture session.");
                records.Add(value);
            }
            return new(session, role, records.MoveToImmutable(), root.Incomplete);
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or OverflowException)
        { throw new WireFaultException(WireFaultReason.OutOfRange, "Invalid typed observation payload.", error); }
    }

    private static T ObservationEnum<T>(uint value) where T : struct, Enum
    {
        var result = (T)Enum.ToObject(typeof(T), value);
        if (!Enum.IsDefined(result) || Convert.ToUInt64(result, System.Globalization.CultureInfo.InvariantCulture) != value)
            throw new ArgumentException("Undefined observation discriminant.");
        return result;
    }
    private static ulong Low(Int128 value) => unchecked((ulong)value);
    private static long High(Int128 value) => (long)(value >> 64);
    private static Int128 Exact(ulong low, long high) => ((Int128)high << 64) | low;
    private static Offset<W.ObservationInterval> Interval(FlatBufferBuilder b, ClockInterval value) =>
        W.ObservationInterval.CreateObservationInterval(b, Low(value.Lower), High(value.Lower), Low(value.Upper), High(value.Upper));
    private static ClockInterval Interval(W.ObservationInterval? value)
    {
        var v = value ?? throw new ArgumentException("Missing diagnostic interval.");
        var result = new ClockInterval(Exact(v.LowerLow, v.LowerHigh), Exact(v.UpperLow, v.UpperHigh));
        if (result.Upper < result.Lower) throw new ArgumentException("Reversed diagnostic interval.");
        return result;
    }

    private static Offset<W.DiagnosticObservation> EncodeDiagnostic(FlatBufferBuilder b, WorkshopDiagnostic diagnostic)
    {
        ValidateDiagnostic(diagnostic);
        W.ObservationPayload kind; int offset;
        switch (diagnostic)
        {
            case WorkshopFaultDiagnostic d:
                kind = W.ObservationPayload.Fault;
                var exception = b.CreateString(d.ExceptionType); var detail = b.CreateString(d.Detail);
                offset = W.FaultObservation.CreateFaultObservation(b, (uint)d.Kind, (uint)d.Site, exception, detail).Value; break;
            case ClockRecalibrationDiagnostic d:
                kind = W.ObservationPayload.Recalibration;
                var peer = W.RecalibrationObservation.CreatePeerVector(b, WorkshopClockWire.EncodePeer(d.Peer));
                var pending = d.Pending is { } p ? W.RecalibrationObservation.CreatePendingVector(b, WorkshopClockWire.EncodeProbe(p, d.Peer)) : default;
                var reply = d.Reply is { } r ? W.ReplyObservation.CreateReplyObservation(b,
                    W.ReplyObservation.CreateProbeVector(b, WorkshopClockWire.EncodeProbe(r.Probe, d.Peer)),
                    r.MasterReceived.Value, r.MasterSent.Value, r.RequesterReceived.Value, r.Derivation.Value) : default;
                W.RecalibrationObservation.StartSlotsVector(b, d.Slots.Length);
                for (var i = d.Slots.Length - 1; i >= 0; i--)
                {
                    var slot = d.Slots[i]; W.ObservationSlot.CreateObservationSlot(b, slot.Receipt.Value, slot.Sequence.Value,
                        Low(slot.Interval.Lower), High(slot.Interval.Lower), Low(slot.Interval.Upper), High(slot.Interval.Upper));
                }
                var slots = b.EndVector();
                offset = W.RecalibrationObservation.CreateRecalibrationObservation(b, peer, (uint)d.Cause, d.Count,
                    (uint)d.StateBefore, d.Derivation.HasValue, d.Derivation?.Value ?? 0, d.DisplayEpoch, d.At.Value,
                    d.RecoveryStarted.Value, d.RecoverySends, pending, reply, slots).Value; break;
            case ClockReplyRejectedDiagnostic d:
                kind = W.ObservationPayload.RejectedClock;
                offset = W.RejectedClockObservation.CreateRejectedClockObservation(b, (uint)d.Role, d.Count, d.Length,
                    d.Received.Value, b.CreateString(d.Detail)).Value; break;
            case ClockLateRenewalDiagnostic d:
                kind = W.ObservationPayload.LateRenewal;
                W.LateRenewalObservation.StartLateRenewalObservation(b);
                W.LateRenewalObservation.AddRole(b, (uint)d.Role); W.LateRenewalObservation.AddCount(b, d.Count);
                W.LateRenewalObservation.AddLapseAge(b, d.LapseAge); W.LateRenewalObservation.AddExchanges(b, d.Exchanges);
                W.LateRenewalObservation.AddRenewedAt(b, d.RenewedAt.Value);
                W.LateRenewalObservation.AddRetained(b, Interval(b, d.Retained)); W.LateRenewalObservation.AddFresh(b, Interval(b, d.Fresh));
                W.LateRenewalObservation.AddHasDerivation(b, d.Derivation.HasValue); W.LateRenewalObservation.AddDerivation(b, d.Derivation?.Value ?? 0);
                W.LateRenewalObservation.AddDisplayEpoch(b, d.DisplayEpoch); offset = W.LateRenewalObservation.EndLateRenewalObservation(b).Value; break;
            case ClockWideOffsetDiagnostic d:
                kind = W.ObservationPayload.WideOffset;
                offset = W.WideOffsetObservation.CreateWideOffsetObservation(b, d.Offset, d.AdmittedLower, d.AdmittedUpper,
                    d.DeclaredUncertainty, d.Count).Value; break;
            case ClockDerivationDiagnostic d:
                kind = W.ObservationPayload.Derivation;
                W.DerivationObservation.StartDerivationObservation(b);
                W.DerivationObservation.AddReport(b, (uint)d.Report); W.DerivationObservation.AddOutcome(b, (uint)d.Outcome);
                W.DerivationObservation.AddEpoch(b, d.Epoch.Value); W.DerivationObservation.AddCorrection(b, d.Correction);
                W.DerivationObservation.AddHasPending(b, d.PendingCorrection.HasValue); W.DerivationObservation.AddPending(b, d.PendingCorrection ?? 0);
                W.DerivationObservation.AddHasAdmitted(b, d.AdmittedLower.HasValue);
                W.DerivationObservation.AddAdmittedLower(b, d.AdmittedLower ?? 0); W.DerivationObservation.AddAdmittedUpper(b, d.AdmittedUpper ?? 0);
                if (d.Exchange is { } exchange) W.DerivationObservation.AddExchange(b,
                    W.ObservationExchange.CreateObservationExchange(b, exchange.Before, exchange.Master, exchange.After, exchange.Epoch.Value));
                W.DerivationObservation.AddQualified(b, d.Qualified); W.DerivationObservation.AddStartupAttempts(b, d.StartupAttempts);
                W.DerivationObservation.AddStartupOutliers(b, d.StartupOutliers); W.DerivationObservation.AddStartupInconclusive(b, d.StartupInconclusive);
                W.DerivationObservation.AddStartupUnanswered(b, d.StartupUnanswered); W.DerivationObservation.AddChecks(b, d.Checks);
                W.DerivationObservation.AddSlews(b, d.Slews); W.DerivationObservation.AddRederivations(b, d.Rederivations);
                W.DerivationObservation.AddInconclusive(b, d.Inconclusive); W.DerivationObservation.AddUnanswered(b, d.Unanswered);
                W.DerivationObservation.AddCheckRetries(b, d.CheckRetries); offset = W.DerivationObservation.EndDerivationObservation(b).Value; break;
            case AnimationDeferralDiagnostic d:
                kind = W.ObservationPayload.Deferral; offset = W.DeferralObservation.CreateDeferralObservation(b, (uint)d.Cause, d.Count).Value; break;
            case WorkshopDebtDiagnostic d:
                kind = W.ObservationPayload.Debt; offset = W.DebtObservation.CreateDebtObservation(b, d.CommittedTick.Value,
                    (uint)d.Reason, d.OldestUnreceipted.Value, d.AgeMilliseconds).Value; break;
            case WorkshopTickFailureDiagnostic d:
                kind = W.ObservationPayload.TickFailure; offset = W.TickFailureObservation.CreateTickFailureObservation(b,
                    (uint)d.Result.Outcome, (uint)d.Result.Reason, (uint)d.Fault).Value; break;
            case WorkshopTransferDiagnostic d:
                kind = W.ObservationPayload.Transfer; offset = W.TransferObservation.CreateTransferObservation(b,
                    W.TransferObservation.CreateResponseVector(b, EncodeResponse(d.Response))).Value; break;
            case WorkshopCaptureDiagnostic d:
                kind = W.ObservationPayload.Capture;
                var samples = new Offset<W.TraceSampleObservation>[d.Samples.Length];
                for (var i = 0; i < samples.Length; i++)
                {
                    var sample = d.Samples[i];
                    var encoded = WorkshopTraceRecord.Encode(new(sample.Epoch, sample.Tick, sample.Bodies, Captures: sample.Captures), d.Profile);
                    samples[i] = W.TraceSampleObservation.CreateTraceSampleObservation(b, W.TraceSampleObservation.CreateRecordVector(b, encoded));
                }
                var sampleVector = W.CaptureObservation.CreateSamplesVector(b, samples);
                var times = EncodeDurations(b, d.Milliseconds);
                offset = W.CaptureObservation.CreateCaptureObservation(b, (uint)d.Kind, (uint)d.Stage, d.Identity, d.Epoch.Value,
                    (uint)d.Profile.Cadence, (uint)d.Profile.Physical, d.Profile.Revision.Value, (uint)d.End, (uint)d.Status,
                    d.ExpectedCount, d.Count, d.Chunk, d.Chunks, d.First, d.Scene?.Value, W.CaptureObservation.CreateSessionVector(b, RuntimeSessionBytes(d.Session)), sampleVector, times).Value; break;
            default: throw new ArgumentException("Unsupported diagnostic payload.");
        }
        W.DiagnosticObservation.StartDiagnosticObservation(b);
        W.DiagnosticObservation.AddKind(b, kind);
        switch (kind)
        {
            case W.ObservationPayload.Fault: W.DiagnosticObservation.AddFault(b, new Offset<W.FaultObservation>(offset)); break;
            case W.ObservationPayload.Recalibration: W.DiagnosticObservation.AddRecalibration(b, new Offset<W.RecalibrationObservation>(offset)); break;
            case W.ObservationPayload.RejectedClock: W.DiagnosticObservation.AddRejectedClock(b, new Offset<W.RejectedClockObservation>(offset)); break;
            case W.ObservationPayload.LateRenewal: W.DiagnosticObservation.AddLateRenewal(b, new Offset<W.LateRenewalObservation>(offset)); break;
            case W.ObservationPayload.WideOffset: W.DiagnosticObservation.AddWideOffset(b, new Offset<W.WideOffsetObservation>(offset)); break;
            case W.ObservationPayload.Derivation: W.DiagnosticObservation.AddDerivation(b, new Offset<W.DerivationObservation>(offset)); break;
            case W.ObservationPayload.Deferral: W.DiagnosticObservation.AddDeferral(b, new Offset<W.DeferralObservation>(offset)); break;
            case W.ObservationPayload.Debt: W.DiagnosticObservation.AddDebt(b, new Offset<W.DebtObservation>(offset)); break;
            case W.ObservationPayload.TickFailure: W.DiagnosticObservation.AddTickFailure(b, new Offset<W.TickFailureObservation>(offset)); break;
            case W.ObservationPayload.Transfer: W.DiagnosticObservation.AddTransfer(b, new Offset<W.TransferObservation>(offset)); break;
            case W.ObservationPayload.Capture: W.DiagnosticObservation.AddCapture(b, new Offset<W.CaptureObservation>(offset)); break;
            default: throw new ArgumentException("Unsupported diagnostic kind.");
        }
        return W.DiagnosticObservation.EndDiagnosticObservation(b);
    }

    private static WorkshopDiagnostic DecodeDiagnostic(W.DiagnosticObservation value)
    {
        if (!Enum.IsDefined(value.Kind) || value.Kind == W.ObservationPayload.None ||
            value.Fault.HasValue != (value.Kind == W.ObservationPayload.Fault) ||
            value.Recalibration.HasValue != (value.Kind == W.ObservationPayload.Recalibration) ||
            value.RejectedClock.HasValue != (value.Kind == W.ObservationPayload.RejectedClock) ||
            value.LateRenewal.HasValue != (value.Kind == W.ObservationPayload.LateRenewal) ||
            value.WideOffset.HasValue != (value.Kind == W.ObservationPayload.WideOffset) ||
            value.Derivation.HasValue != (value.Kind == W.ObservationPayload.Derivation) ||
            value.Deferral.HasValue != (value.Kind == W.ObservationPayload.Deferral) ||
            value.Debt.HasValue != (value.Kind == W.ObservationPayload.Debt) ||
            value.TickFailure.HasValue != (value.Kind == W.ObservationPayload.TickFailure) ||
            value.Transfer.HasValue != (value.Kind == W.ObservationPayload.Transfer) ||
            value.Capture.HasValue != (value.Kind == W.ObservationPayload.Capture))
            throw new ArgumentException("Diagnostic kind requires exactly its matching concrete payload.");
        switch (value.Kind)
        {
            case W.ObservationPayload.Fault:
                var fault = value.Fault!.Value;
                return new WorkshopFaultDiagnostic(ObservationEnum<WorkshopFaultKind>(fault.Kind), ObservationEnum<FaultSite>(fault.Site), fault.ExceptionType, fault.Detail);
            case W.ObservationPayload.Recalibration:
                var recalibration = value.Recalibration!.Value;
                var peer = WorkshopClockWire.DecodePeer(recalibration.GetPeerArray());
                var slots = ImmutableArray.CreateBuilder<ClockSlotDiagnostic>(recalibration.SlotsLength);
                for (var i = 0; i < recalibration.SlotsLength; i++)
                {
                    var slot = recalibration.Slots(i) ?? throw new ArgumentException("Missing clock slot.");
                    slots.Add(new(new(slot.Receipt), new(slot.Sequence), Interval(slot.Interval)));
                }
                return new ClockRecalibrationDiagnostic(peer, ObservationEnum<ClockRecalibrationCause>(recalibration.Cause), recalibration.Count,
                    ObservationEnum<ClockMappingState>(recalibration.StateBefore), Derivation(recalibration.HasDerivation, recalibration.Derivation),
                    recalibration.DisplayEpoch, new(recalibration.At), new(recalibration.RecoveryStarted), recalibration.RecoverySends,
                    recalibration.PendingLength == 0 ? null : WorkshopClockWire.DecodeProbe(recalibration.GetPendingArray(), peer),
                    recalibration.Reply is { } reply ? new ClockReply(WorkshopClockWire.DecodeProbe(reply.GetProbeArray(), peer),
                        new(reply.MasterReceived), new(reply.MasterSent), new(reply.Received), new(reply.Derivation)) : null,
                    slots.MoveToImmutable());
            case W.ObservationPayload.RejectedClock:
                var rejected = value.RejectedClock!.Value;
                return new ClockReplyRejectedDiagnostic(ObservationEnum<WorkshopRuntimeRole>(rejected.Role), rejected.Count, rejected.Length, new(rejected.Received), rejected.Detail);
            case W.ObservationPayload.LateRenewal:
                var late = value.LateRenewal!.Value;
                return new ClockLateRenewalDiagnostic(ObservationEnum<WorkshopRuntimeRole>(late.Role), late.Count, late.LapseAge,
                    late.Exchanges, new(late.RenewedAt), Interval(late.Retained), Interval(late.Fresh), Derivation(late.HasDerivation, late.Derivation), late.DisplayEpoch);
            case W.ObservationPayload.WideOffset:
                var wide = value.WideOffset!.Value;
                return new ClockWideOffsetDiagnostic(wide.Offset, wide.AdmittedLower, wide.AdmittedUpper, wide.DeclaredUncertainty, wide.Count);
            case W.ObservationPayload.Derivation:
                var derivation = value.Derivation!.Value;
                if ((!derivation.HasPending && derivation.Pending != 0) ||
                    (!derivation.HasAdmitted && (derivation.AdmittedLower != 0 || derivation.AdmittedUpper != 0)))
                    throw new ArgumentException("Absent clock fields carry values.");
                return new ClockDerivationDiagnostic(ObservationEnum<ClockDerivationReport>(derivation.Report), ObservationEnum<ClockDerivationOutcome>(derivation.Outcome),
                    new(derivation.Epoch), derivation.Correction, derivation.HasPending ? derivation.Pending : null,
                    derivation.HasAdmitted ? derivation.AdmittedLower : null, derivation.HasAdmitted ? derivation.AdmittedUpper : null,
                    derivation.Exchange is { } exchange ? new(exchange.Before, exchange.Master, exchange.After, new(exchange.Epoch)) : null,
                    derivation.Qualified, derivation.StartupAttempts, derivation.StartupOutliers, derivation.StartupInconclusive, derivation.StartupUnanswered,
                    derivation.Checks, derivation.Slews, derivation.Rederivations, derivation.Inconclusive, derivation.Unanswered, derivation.CheckRetries);
            case W.ObservationPayload.Deferral:
                var deferred = value.Deferral!.Value; return new AnimationDeferralDiagnostic(ObservationEnum<AnimationDeferralCause>(deferred.Cause), deferred.Count);
            case W.ObservationPayload.Debt:
                var debt = value.Debt!.Value; return new WorkshopDebtDiagnostic(new(debt.Tick), ObservationEnum<WorkshopRejection>(debt.Reason), new(debt.Oldest), debt.AgeMilliseconds);
            case W.ObservationPayload.TickFailure:
                var failed = value.TickFailure!.Value; return new WorkshopTickFailureDiagnostic(
                    new(ObservationEnum<WorkshopCommandOutcome>(failed.Outcome), ObservationEnum<WorkshopRejection>(failed.Reason)), ObservationEnum<WorkshopRejection>(failed.Fault));
            case W.ObservationPayload.Transfer:
                return new WorkshopTransferDiagnostic(DecodeResponse(value.Transfer!.Value.GetResponseArray()));
            case W.ObservationPayload.Capture:
                var capture = value.Capture!.Value;
                var profile = new WorkshopGpuProfile(ObservationEnum<SimulationCadence>(capture.Cadence), ObservationEnum<PhysicalStepProfile>(capture.Physical), new(capture.Revision));
                var samples = ImmutableArray.CreateBuilder<WorkshopTraceSample>(capture.SamplesLength);
                for (var i = 0; i < capture.SamplesLength; i++)
                {
                    var bytes = (capture.Samples(i) ?? throw new ArgumentException("Missing trace sample.")).GetRecordArray();
                    if (WorkshopTraceRecord.ReadProfile(bytes) != profile) throw new ArgumentException("Trace profile mismatch.");
                    samples.Add(WorkshopTraceRecord.Decode(bytes));
                }
                return new WorkshopCaptureDiagnostic(ObservationEnum<WorkshopCaptureKind>(capture.Kind), ObservationEnum<WorkshopCaptureStage>(capture.Stage), RequiredSession(capture.GetSessionArray()),
                    capture.Identity, new(capture.Epoch), profile, ObservationEnum<TraceEnd>(capture.End), ObservationEnum<TraceTimingStatus>(capture.Status),
                    capture.ExpectedCount, capture.Count, capture.Chunk, capture.Chunks, capture.First,
                    capture.Scene is { } scene ? new CompiledSceneId(scene) : null,
                    samples.MoveToImmutable(), DecodeDurations(capture));
            default: throw new ArgumentException("Undefined diagnostic payload.");
        }
    }
    private static ClockDerivationEpoch? Derivation(bool present, uint value) => present ? new(value)
        : value == 0 ? null : throw new ArgumentException("Absent derivation carries a value.");

    // External host elapsed-time observations retain their existing f64 lane; game values never enter these helpers.
    private static VectorOffset EncodeDurations(FlatBufferBuilder builder, ImmutableArray<double> values) =>
        W.CaptureObservation.CreateMillisecondsVector(builder, values.ToArray());
    private static ImmutableArray<double> DecodeDurations(W.CaptureObservation source) =>
        source.GetMillisecondsArray().ToImmutableArray();
    private static void ValidateDurations(ImmutableArray<double> values)
    {
        foreach (var duration in values)
            if (!double.IsFinite(duration) || duration < 0) throw new ArgumentException("Invalid capture duration.");
    }
    private static void ValidateDiagnostic(WorkshopDiagnostic value)
    {
        switch (value)
        {
            case WorkshopFaultDiagnostic d:
                Defined(d.Kind); Defined(d.Site); ArgumentException.ThrowIfNullOrEmpty(d.ExceptionType); ArgumentNullException.ThrowIfNull(d.Detail); break;
            case ClockRecalibrationDiagnostic d:
                d.Peer.Validate(); Defined(d.Cause); Defined(d.StateBefore); d.At.Validate(); d.RecoveryStarted.Validate();
                if (d.Reply is { } reply) { reply.MasterReceived.Validate(); reply.MasterSent.Validate(); reply.RequesterReceived.Validate(); }
                // A reversed reply is diagnostic evidence of recovery, not an admitted clock mapping.
                if (d.Count == 0 || d.RecoverySends < 0 || d.Slots.IsDefault) throw new ArgumentException("Invalid recalibration.");
                foreach (var slot in d.Slots) { slot.Receipt.Validate(); if (slot.Sequence.Value == 0 || slot.Interval.Upper < slot.Interval.Lower) throw new ArgumentException("Invalid clock slot."); }
                break;
            case ClockReplyRejectedDiagnostic d:
                Defined(d.Role); d.Received.Validate(); if (d.Count == 0 || d.Length < 0) throw new ArgumentException("Invalid rejected clock reply."); ArgumentException.ThrowIfNullOrEmpty(d.Detail); break;
            case ClockLateRenewalDiagnostic d:
                Defined(d.Role); d.RenewedAt.Validate();
                if (d.Count == 0 || d.LapseAge < 0 || d.Exchanges < 0 || d.Retained.Upper < d.Retained.Lower || d.Fresh.Upper < d.Fresh.Lower) throw new ArgumentException("Invalid renewal."); break;
            case ClockWideOffsetDiagnostic d:
                if (d.Count == 0 || d.AdmittedLower > d.AdmittedUpper || d.DeclaredUncertainty < 0) throw new ArgumentException("Invalid wide offset."); break;
            case ClockDerivationDiagnostic d:
                Defined(d.Report); Defined(d.Outcome);
                if (d.Qualified < 0 || d.StartupAttempts < 0 || d.StartupOutliers < 0 || d.StartupInconclusive < 0 || d.StartupUnanswered < 0 ||
                    d.AdmittedLower.HasValue != d.AdmittedUpper.HasValue || d.AdmittedLower > d.AdmittedUpper) throw new ArgumentException("Invalid derivation.");
                if (d.Exchange is { } exchange) _ = WorkshopClockDerivation.IsNarrow(exchange);
                break;
            case AnimationDeferralDiagnostic d: Defined(d.Cause); if (d.Count == 0) throw new ArgumentException("Invalid deferral count."); break;
            case WorkshopDebtDiagnostic d: Defined(d.Reason); if (d.AgeMilliseconds < 0) throw new ArgumentException("Negative debt age."); break;
            case WorkshopTickFailureDiagnostic d: Defined(d.Result.Outcome); Defined(d.Result.Reason); Defined(d.Fault); break;
            case WorkshopTransferDiagnostic d: if (!d.Response.Read.CarriesBalanceTransfer()) throw new ArgumentException("Transfer diagnostic has no transfer."); break;
            case WorkshopCaptureDiagnostic d:
                Defined(d.Kind); Defined(d.Stage); Defined(d.End); Defined(d.Status); d.Profile.Validate(); d.Session.Validate();
                if ((d.Stage == WorkshopCaptureStage.Begin) != d.Scene.HasValue || d.Scene is { Value: 0 } ||
                    d.Identity == 0 || d.Epoch.Value == 0 || d.Count < 0 || d.ExpectedCount < 0 || d.Chunks < 0 || d.Chunk < 0 || d.First < 0 ||
                    d.Samples.IsDefault || d.Milliseconds.IsDefault || (d.Kind == WorkshopCaptureKind.Pose && d.Milliseconds.Length != 0) ||
                    (d.Kind == WorkshopCaptureKind.Timing && d.Samples.Length != 0) ||
                    (d.Stage != WorkshopCaptureStage.Chunk && (d.Samples.Length != 0 || d.Milliseconds.Length != 0)) ||
                    (d.Stage == WorkshopCaptureStage.Chunk && (d.Chunk >= d.Chunks || d.Count != d.Samples.Length + d.Milliseconds.Length)))
                    throw new ArgumentException("Invalid capture fields.");
                if (d.Status == TraceTimingStatus.Complete && (d.End != TraceEnd.Complete ||
                    (d.Stage == WorkshopCaptureStage.End && d.Count != d.ExpectedCount)))
                    throw new ArgumentException("Contradictory complete capture metadata.");
                ValidateDurations(d.Milliseconds);
                foreach (var sample in d.Samples) if (sample.Epoch != d.Epoch) throw new ArgumentException("Foreign trace epoch.");
                break;
            default: throw new ArgumentException("Unknown diagnostic type.");
        }
        static void Defined<T>(T item) where T : struct, Enum
        { if (!Enum.IsDefined(item)) throw new ArgumentException("Undefined diagnostic enum."); }
    }
}
