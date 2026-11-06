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
public sealed record WorkshopCaptureDiagnostic(WorkshopCaptureKind Kind, WorkshopCaptureStage Stage, RuntimeSessionId Session,
    ulong Identity, SimulationEpoch Epoch, WorkshopGpuProfile Profile, TraceEnd End, TraceTimingStatus Status,
    int ExpectedCount, int Count, int Chunk, int Chunks, int First,
    CompiledSceneId? Scene, ImmutableArray<WorkshopTraceSample> Samples, ImmutableArray<double> Milliseconds) : WorkshopDiagnostic
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

public interface IWorkshopDiagnostics
{
    string? Incomplete { get; }
    event Action<string>? BecameIncomplete;
    void MarkIncomplete(string detail);
    void Publish(WorkshopDiagnostic record, RuntimeSessionId session, WorkshopRuntimeRole role, Action<byte[]> publish);
    void RetainTransfer(WorkshopResponse response);
    WorkshopDiagnostic[] Snapshot();
    int Count { get; }
    WorkshopDiagnostic[] Since(int after);
    void Record(WorkshopDiagnostic record);
    void Capture(Func<WorkshopDiagnostic> capture);
    void Fault(FaultSite site, Exception exception);
    void AfterRetirement(FaultSite site, Exception exception);
    void Rejection(Exception exception);
}

public readonly record struct WorkshopObservationBatch(RuntimeSessionId Session, WorkshopRuntimeRole Role,
    ImmutableArray<WorkshopDiagnostic> Records, string? Incomplete = null);
