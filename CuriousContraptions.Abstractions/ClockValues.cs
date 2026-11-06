using System;

namespace CuriousContraptions.Gpu;

public readonly record struct RuntimeSessionId(ulong Low, ulong High)
{
    public const int ByteLength = 16;
    public void Validate() { if ((Low | High) == 0) throw new ArgumentException("A runtime session must be nonzero."); }

}
public enum WorkshopRuntimeRole : uint { Browser = 1, Simulation = 2, Animation = 3 }
public readonly record struct MasterTimeNanoseconds(long Value)
{
    public void Validate() { if (Value < 0) throw new ArgumentOutOfRangeException(nameof(Value)); }
}
public readonly record struct ClockGeneration(ulong Value);
public readonly record struct ClockProbeSequence(ulong Value);
public readonly record struct PublicationSequence(ulong Value);
public interface IWorkshopCaptureClock
{
    ClockGeneration Generation { get; }
    WorkshopClockStamp Capture();
}
public readonly record struct MonotonicNanoseconds(long Value)
{
    public void Validate() { if (Value < 0) throw new ArgumentOutOfRangeException(nameof(Value)); }
}
public readonly record struct TimestampUncertainty(ulong Value)
{
    public const ulong Maximum = 100_000;
    public void Validate() { if (Value > Maximum) throw new ArgumentOutOfRangeException(nameof(Value)); }
}
public enum WorkshopClockDomain : byte { BrowserMonotonic = 1, SimulationMonotonic = 2 }
/// <summary>Renewing: the mapping lapsed because the requester's own event loop did not run (no probe outstanding and
/// none could be sent); consumers hold, as for any mapping without a current calibration, until a fresh exchange
/// narrow enough to qualify the mapping renews it (consistent with the old mapping) or recalibrates it (disagreeing),
/// within ordinary recovery's budget (R2-F1 and R3-F1, coordinator decisions 19 Oct 2026).</summary>
public enum ClockMappingState : byte { Calibrating, Ready, Suspect, Expired, Retired, Recovering, Renewing }
/// <summary>Inconclusive (R3-F1): an otherwise acceptable renewing exchange too wide to qualify the mapping; it cannot
/// see a clock move smaller than its own width, so it neither renews nor recalibrates and is not retained.</summary>
public enum ClockProbeOutcome { Accepted, WrongIdentity, Reversed, NegativeRoundTrip, Disjoint, Expired, ProfileRange, Malformed, Inconclusive }
public readonly record struct WorkshopClockStamp(WorkshopClockDomain Domain, ClockGeneration Generation,
    MonotonicNanoseconds Time, TimestampUncertainty Uncertainty)
{
    public void Validate()
    {
        if (!Enum.IsDefined(Domain) || Generation.Value == 0) throw new ArgumentException("Invalid native clock identity.");
        Time.Validate(); Uncertainty.Validate();
    }
}
public readonly record struct ClockProbe(RuntimeSessionId Session, ClockGeneration Generation, ClockGeneration RequesterGeneration, WorkshopRuntimeRole RequesterRole,
    ClockProbeSequence Sequence, MonotonicNanoseconds RequesterSent);
/// <summary>The epoch of the master context's derivation correction; every clock reply carries the epoch it was stamped under.</summary>
public readonly record struct ClockDerivationEpoch(uint Value);
public readonly record struct ClockReply(ClockProbe Probe, MonotonicNanoseconds MasterReceived,
    MonotonicNanoseconds MasterSent, MonotonicNanoseconds RequesterReceived, ClockDerivationEpoch Derivation);
public readonly record struct ClockInterval(Int128 Lower, Int128 Upper)
{
    public long Midpoint => checked((long)(((Int128)Lower + Upper) / 2));
    public Int128 Width => Upper - Lower;
    public bool Overlaps(ClockInterval other) => Lower <= other.Upper && other.Lower <= Upper;
}
public readonly record struct ClockObservation(ClockReply Reply, ClockProbeOutcome Outcome, ClockInterval Offset);
public readonly record struct MappedCapture(ClockInterval BrowserInterval, MonotonicNanoseconds Receipt,
    ClockProbeSequence Probe);

/// <summary>One immutable peer identity retained by the browser endpoint under its values quota.</summary>
public sealed record WorkshopClockPeer(RuntimeSessionId Session, ClockGeneration Generation,
    MonotonicNanoseconds MasterOrigin, ClockGeneration RequesterGeneration,
    TimestampUncertainty Uncertainty, WorkshopRuntimeRole RequesterRole)
{
    public void Validate()
    {
        Session.Validate(); Uncertainty.Validate(); MasterOrigin.Validate();
        if (Generation.Value == 0 || RequesterGeneration.Value == 0 ||
            RequesterRole is not (WorkshopRuntimeRole.Browser or WorkshopRuntimeRole.Animation))
            throw new ArgumentException("Invalid master/requester clock identity.");
    }
}

/// <summary>
/// Why a mapping recalibrated (discarded every retained slot under a new display epoch). A stalled requester's lapse
/// that a consistent exchange renews is not a recalibration.
/// <list type="bullet">
/// <item>Elapsed: a recovery or renewal episode's 250 ms bound elapsed, an unanswered renewing probe included.</item>
/// <item>ProbeLimit: the episode's eighth reply settled unqualified or inconclusive.</item>
/// <item>MappingLifetime: no accepted reply arrived within the 500 ms mapping lifetime. The master's context or the reply
/// path failed, or the requester kept running without a usable reply; or, after the requester's own stall, a renewing
/// reply was not accepted or the concluding exchange disagreed with the old mapping.</item>
/// <item>Disjoint: a fresh interval was disjoint from the retained ones.</item>
/// <item>ProfileRange: a fresh interval widened the retained span past the profile.</item>
/// <item>MasterRederived: the master context's derivation correction changed (WorkshopClockDerivation).</item>
/// <item>StallPastMappedWidth: the requester's stall lasted so long that the old mapping, drift-widened over it, no longer
/// fits the mapped width, so it can no longer vouch for any concluding exchange (R4-F1). Distinct from MappingLifetime, so
/// telemetry tells a too-long stall from a genuine disagreement.</item>
/// </list>
/// </summary>
public enum ClockRecalibrationCause : byte { Elapsed, ProbeLimit, MappingLifetime, Disjoint, ProfileRange, MasterRederived, StallPastMappedWidth }
public readonly record struct ClockRecalibration(WorkshopRuntimeRole Role, ClockRecalibrationCause Cause, ulong Count,
    ulong DisplayEpoch, MonotonicNanoseconds At);
