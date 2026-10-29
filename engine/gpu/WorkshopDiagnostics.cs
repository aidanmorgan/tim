using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CuriousContraptions.Gpu;

public enum FaultSite
{
    None = 0, Initialize = 1, Construct, Advance, Reset, Startup, LateDispose, RunLoop, StaleRunLoop, Presentation,
    Disposal, TraceFlush, Installation, UiHandler, WorkerEnvelope, Transport
}
public enum WorkshopDiagnosticCategory
{
    Fault, Rejection, AfterRetirement, ClockRecalibrated, ClockReplyRejected, ClockRenewedLate,
    ClockOffsetWide, ClockDerivation, AnimationControlDeferred, DebtFault, TickFailed, BalanceTransfer, Capture, ClockSample, AnimationSample, Presentation
}
public enum WorkshopFaultKind { Fault, Rejection, AfterRetirement }
public enum WorkshopCaptureKind { Pose, Timing }
public enum WorkshopCaptureStage { Begin, Chunk, End }
public enum TraceEnd { Complete = 0, Reset = 1, Disposed = 2, Fault = 4, InvalidRecord = 5, Overflow = 6 }
public enum TraceTimingStatus { Complete, Incomplete }

public abstract record WorkshopDiagnostic
{
    public abstract WorkshopDiagnosticCategory Category { get; }
    private protected WorkshopDiagnostic() { }
}
public sealed record WorkshopFaultDiagnostic(WorkshopFaultKind Kind, FaultSite Site, string ExceptionType, string Detail) : WorkshopDiagnostic
{
    public override WorkshopDiagnosticCategory Category => Kind switch
    {
        WorkshopFaultKind.Fault => WorkshopDiagnosticCategory.Fault,
        WorkshopFaultKind.Rejection => WorkshopDiagnosticCategory.Rejection,
        WorkshopFaultKind.AfterRetirement => WorkshopDiagnosticCategory.AfterRetirement,
        _ => throw new InvalidOperationException("Undefined fault kind.")
    };
}
public readonly record struct ClockSlotDiagnostic(MonotonicNanoseconds Receipt, ClockProbeSequence Sequence, ClockInterval Interval);
public sealed record ClockRecalibrationDiagnostic(WorkshopClockPeer Peer, ClockRecalibrationCause Cause,
    ulong Count, ClockMappingState StateBefore, ClockDerivationEpoch? Derivation, ulong DisplayEpoch,
    MonotonicNanoseconds At, MonotonicNanoseconds RecoveryStarted, int RecoverySends,
    ClockProbe? Pending, ClockReply? Reply, ImmutableArray<ClockSlotDiagnostic> Slots) : WorkshopDiagnostic
{ public override WorkshopDiagnosticCategory Category => WorkshopDiagnosticCategory.ClockRecalibrated; }
public sealed record ClockReplyRejectedDiagnostic(WorkshopRuntimeRole Role, ulong Count, int Length,
    MonotonicNanoseconds Received, string Detail) : WorkshopDiagnostic
{ public override WorkshopDiagnosticCategory Category => WorkshopDiagnosticCategory.ClockReplyRejected; }
public sealed record ClockLateRenewalDiagnostic(WorkshopRuntimeRole Role, ulong Count, long LapseAge,
    int Exchanges, MonotonicNanoseconds RenewedAt, ClockInterval Retained, ClockInterval Fresh,
    ClockDerivationEpoch? Derivation, ulong DisplayEpoch) : WorkshopDiagnostic
{ public override WorkshopDiagnosticCategory Category => WorkshopDiagnosticCategory.ClockRenewedLate; }
public sealed record ClockWideOffsetDiagnostic(long Offset, long AdmittedLower, long AdmittedUpper,
    long DeclaredUncertainty, ulong Count) : WorkshopDiagnostic
{ public override WorkshopDiagnosticCategory Category => WorkshopDiagnosticCategory.ClockOffsetWide; }
public sealed record ClockDerivationDiagnostic(ClockDerivationReport Report, ClockDerivationOutcome Outcome,
    ClockDerivationEpoch Epoch, long Correction, long? PendingCorrection, long? AdmittedLower, long? AdmittedUpper,
    ClockDerivationExchange? Exchange, int Qualified, int StartupAttempts, int StartupOutliers,
    int StartupInconclusive, int StartupUnanswered, ulong Checks, ulong Slews, ulong Rederivations,
    ulong Inconclusive, ulong Unanswered, ulong CheckRetries) : WorkshopDiagnostic
{ public override WorkshopDiagnosticCategory Category => WorkshopDiagnosticCategory.ClockDerivation; }
public sealed record AnimationDeferralDiagnostic(AnimationDeferralCause Cause, ulong Count) : WorkshopDiagnostic
{ public override WorkshopDiagnosticCategory Category => WorkshopDiagnosticCategory.AnimationControlDeferred; }
public sealed record WorkshopDebtDiagnostic(SimulationTick CommittedTick, WorkshopRejection Reason,
    PublicationSequence OldestUnreceipted, long AgeMilliseconds) : WorkshopDiagnostic
{ public override WorkshopDiagnosticCategory Category => WorkshopDiagnosticCategory.DebtFault; }
public sealed record WorkshopTickFailureDiagnostic(WorkshopCommandResult Result, WorkshopRejection Fault) : WorkshopDiagnostic
{ public override WorkshopDiagnosticCategory Category => WorkshopDiagnosticCategory.TickFailed; }
public sealed record WorkshopTransferDiagnostic(WorkshopResponse Response) : WorkshopDiagnostic
{ public override WorkshopDiagnosticCategory Category => WorkshopDiagnosticCategory.BalanceTransfer; }
public readonly record struct WorkshopTraceSample(SimulationEpoch Epoch, SimulationTick Tick,
    PhysicsBodyReadSet Bodies, PhysicsCaptureRead Captures);
public sealed record WorkshopCaptureDiagnostic(WorkshopCaptureKind Kind, WorkshopCaptureStage Stage,
    ulong Identity, SimulationEpoch Epoch, WorkshopGpuProfile Profile, TraceEnd End, TraceTimingStatus Status,
    int ExpectedCount, int Count, int Chunk, int Chunks, int First,
    WorkshopCommand? Construction, ImmutableArray<WorkshopTraceSample> Samples, ImmutableArray<double> Milliseconds) : WorkshopDiagnostic
{ public override WorkshopDiagnosticCategory Category => WorkshopDiagnosticCategory.Capture; }
// These records originate in the managed browser after admission; they do not cross a worker wire.
public sealed record WorkshopClockSampleDiagnostic(ClockProbe Probe, ClockReply? Reply, ClockProbeOutcome Outcome,
    ClockInterval Offset, MonotonicNanoseconds Received) : WorkshopDiagnostic
{ public override WorkshopDiagnosticCategory Category => WorkshopDiagnosticCategory.ClockSample; }
public sealed record WorkshopAnimationDiagnostic(CadenceRevision Cadence, AnimationOutputKind Kind,
    WorkshopAnimationSample Sample) : WorkshopDiagnostic
{ public override WorkshopDiagnosticCategory Category => WorkshopDiagnosticCategory.AnimationSample; }
public sealed record WorkshopPresentationDiagnostic(WorkshopPresentationSample Sample, bool Selected,
    PresentationScene Scene, MonotonicNanoseconds AppliedAt, CadenceRevision Cadence, PulseOrdinal Pulse) : WorkshopDiagnostic
{ public override WorkshopDiagnosticCategory Category => WorkshopDiagnosticCategory.Presentation; }
public readonly record struct WorkshopDiagnosticReceipt(WorkshopObservationOrdinal Ordinal, WorkshopDiagnostic Record);

/// <summary>One explicitly owned session journal. Each producer publishes its concrete immutable payload.</summary>
public sealed class WorkshopDiagnostics
{
    private readonly List<WorkshopDiagnostic> _records = new(32);
    private readonly Action<WorkshopDiagnostic>? _publish;
    public WorkshopDiagnostics(Action<WorkshopDiagnostic>? publish = null) => _publish = publish;
    public string? Incomplete { get; private set; }
    internal event Action<string>? BecameIncomplete;
    internal void MarkIncomplete(string detail)
    {
        if (Incomplete is not null) return;
        Incomplete = detail;
        BecameIncomplete?.Invoke(detail);
    }
    public void Publish(WorkshopDiagnostic record, RuntimeSessionId session, WorkshopRuntimeRole role, Action<byte[]> publish) =>
        publish(WorkshopWireFlatBuffers.EncodeObservations(new(session, role, [record], Incomplete)));
    public void RetainTransfer(WorkshopResponse response)
    {
        if (response.Read.CarriesBalanceTransfer()) Capture(() => new WorkshopTransferDiagnostic(response));
    }
    public WorkshopDiagnostic[] Snapshot() => _records.ToArray();
    internal int Count => _records.Count;
    internal WorkshopDiagnostic[] Since(int after) => _records.GetRange(after, _records.Count - after).ToArray();
    public void Record(WorkshopDiagnostic record)
    {
        ArgumentNullException.ThrowIfNull(record);
        try { _records.Add(record); _publish?.Invoke(record); }
        catch (Exception error) when (error is OutOfMemoryException or ArgumentException or InvalidOperationException or
            OverflowException or System.Runtime.InteropServices.JavaScript.JSException or WireFaultException or System.IO.IOException)
        { MarkIncomplete(record.Category + ": " + error); }
    }
    public void Capture(Func<WorkshopDiagnostic> capture)
    {
        try { Record(capture()); }
        catch (Exception error) when (error is OutOfMemoryException or ArgumentException or InvalidOperationException or OverflowException)
        { MarkIncomplete(error.ToString()); }
    }
    public void Fault(FaultSite site, Exception error) => ExceptionRecord(WorkshopFaultKind.Fault, site, error);
    public void AfterRetirement(FaultSite site, Exception error) => ExceptionRecord(WorkshopFaultKind.AfterRetirement, site, error);
    public void Rejection(Exception error) => ExceptionRecord(WorkshopFaultKind.Rejection, FaultSite.UiHandler, error);
    private void ExceptionRecord(WorkshopFaultKind kind, FaultSite site, Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);
        Capture(() => new WorkshopFaultDiagnostic(kind, site, error.GetType().ToString(), error.ToString()));
    }
}
