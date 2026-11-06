using System;

namespace CuriousContraptions.Gpu;

public interface IClockMapping
{
    ClockMappingState State { get; }
    ulong DisplayEpoch { get; }
    RuntimeSessionId Session { get; }
    WorkshopClockPeer Peer { get; }
    ClockGeneration Generation { get; }
    TimestampUncertainty EndpointUncertainty { get; }
    int ValidProbeCount { get; }
    bool IsQualified { get; }
    ClockProbe? Pending { get; }
    IWorkshopDiagnostics Diagnostics { get; }
    ClockProbe? BeginProbe(MonotonicNanoseconds now);
    ClockObservation Receive(ClockReply reply);
    bool TryMap(WorkshopClockStamp capture, MonotonicNanoseconds now, out MappedCapture mapped);
    bool TryMasterNow(MonotonicNanoseconds now, out ClockInterval masterInterval);
    ulong Recalibrations { get; }
    ClockRecalibration? LastRecalibration { get; }
    ulong MalformedReplies { get; }
    void RejectMalformedReply(int length, MonotonicNanoseconds received, string detail);
    void Observe(MonotonicNanoseconds now);
    ulong LateRenewals { get; }
    void Retire();
}

public interface IClockDerivation
{
    IWorkshopDiagnostics Diagnostics { get; }
    ulong WideStartupOffsets { get; }
    int StartupOutliers { get; }
    ClockDerivationEpoch Epoch { get; }
    long Correction { get; }
    int Qualified { get; }
    int StartupAttempts { get; }
    int StartupInconclusive { get; }
    int StartupUnanswered { get; }
    ulong Checks { get; }
    ulong Slews { get; }
    ulong Rederivations { get; }
    ulong Inconclusive { get; }
    ulong Unanswered { get; }
    ulong CheckRetries { get; }
    bool IsQualified { get; }
    ClockDerivationOutcome Qualify(ClockDerivationExchange exchange);
    long StartupCorrection { get; }
    void QualificationMissed();
    ClockDerivationOutcome Check(ClockDerivationExchange exchange, out long correction);
    void Adopt(ClockDerivationEpoch epoch);
    void Retried();
    void Missed();
}

public interface IClockServices
{
    int ExchangeTimeoutMilliseconds { get; }
    int CheckIntervalMilliseconds { get; }
    int CheckExchangeLimit { get; }
    bool IsNarrow(ClockDerivationExchange exchange);
    IClockMapping CreateMapping(WorkshopClockPeer peer, IWorkshopDiagnostics? diagnostics = null);
    IClockDerivation CreateDerivation(IWorkshopDiagnostics? diagnostics = null);
    ClockInterval Offset(ClockReply reply, TimestampUncertainty uncertainty);
    ClockInterval Expand(ClockInterval interval, long elapsed);
    MonotonicNanoseconds FromMilliseconds(double milliseconds);
    long StartupSpacing { get; }
    long RefreshSpacing { get; }
    long ProbeLifetime { get; }
    long MappingLifetime { get; }
    long MaximumMappedWidth { get; }
    long MaximumOffsetSpan { get; }
    int ProbeCapacity { get; }
    int BookkeepingBytes { get; }
    long RecoveryLifetime { get; }
    int RecoveryProbeLimit { get; }
}
