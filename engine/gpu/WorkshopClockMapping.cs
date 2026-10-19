using System;

namespace CuriousContraptions.Gpu;

public readonly record struct RuntimeSessionId(ulong Low, ulong High)
{
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

[Flags]
internal enum ClockBookkeepingFlags : byte { None = 0, Pending = 1 }

/// <summary>
/// Browser-owned mapping of one native worker origin. External-clock arithmetic is integral.
/// Live payload: eight 24-byte slots plus 88-byte aligned owner fields = 280 bytes. Array/object
/// headers are additional resident memory. Raw diagnostic observations are returned, not retained.
/// </summary>
public sealed class WorkshopClockMapping
{
    public const long StartupSpacing = 10_000_000;
    public const long RefreshSpacing = 250_000_000;
    public const long ProbeLifetime = 250_000_000;
    public const long MappingLifetime = 500_000_000;
    public const long MaximumMappedWidth = 2_000_000;
    public const long MaximumOffsetSpan = 502_550_000;
    public const int ProbeCapacity = 8;
    /// <summary>Eight 24-byte slots plus 88-byte aligned owner fields (the renewal lapse age added 8, Story 9.4b D23).</summary>
    public const int BookkeepingBytes = 280;
    public const long RecoveryLifetime = RefreshSpacing;
    public const int RecoveryProbeLimit = ProbeCapacity;
    private readonly WorkshopClockPeer _peer;
    private readonly ClockSlot[] _slots = new ClockSlot[ProbeCapacity];
    private long _anchor;
    private ulong _sequence;
    private long _lastRequester, _lastSent, _recoveryStarted;
    private long _renewalLapseAge;
    // One budget for both bounded episodes (R3-F1): probes sent in the current recovery or renewal, at most
    // RecoveryProbeLimit, within RecoveryLifetime of _recoveryStarted.
    private byte _recoverySends;
    private ClockDerivationEpoch? _derivation;
    private byte _count, _next;
    private ClockBookkeepingFlags _flags;
    public ClockMappingState State { get; private set; } = ClockMappingState.Calibrating;
    public ulong DisplayEpoch { get; private set; } = 1;
    public RuntimeSessionId Session => _peer.Session;
    /// <summary>The peer this mapping owns; the wire's one reply path decodes against it (WorkshopClockWire.ReceiveEncoded).</summary>
    internal WorkshopClockPeer Peer => _peer;
    public ClockGeneration Generation => _peer.Generation;
    public TimestampUncertainty EndpointUncertainty => _peer.Uncertainty;
    public int ValidProbeCount => _count;
    public bool IsQualified => State is ClockMappingState.Ready or ClockMappingState.Recovering;
    public ClockProbe? Pending => (_flags & ClockBookkeepingFlags.Pending) != 0
        ? new(Session, Generation, _peer.RequesterGeneration, _peer.RequesterRole, new(_sequence), new(_lastSent)) : null;

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private readonly record struct ClockSlot(long Receipt, ulong Sequence, int LowerDelta, uint Width);

    public WorkshopClockMapping(WorkshopClockPeer peer)
    {
        peer.Validate(); _peer = peer;
    }

    private void ValidateNow(MonotonicNanoseconds now)
    {
        now.Validate();
        if (State is ClockMappingState.Retired || now.Value < _lastRequester)
            throw new InvalidOperationException("Native browser clock retired or reversed.");
    }

    public ClockProbe? BeginProbe(MonotonicNanoseconds now)
    {
        Observe(now);
        if (State is ClockMappingState.Recovering or ClockMappingState.Renewing && _recoverySends == RecoveryProbeLimit) return null;
        if (Pending is { } pending && (Int128)now.Value - pending.RequesterSent.Value < ProbeLifetime) return null;
        var spacing = State switch
        {
            ClockMappingState.Ready => RefreshSpacing,
            ClockMappingState.Renewing => 0, // the renewing exchange is taken at once
            _ => StartupSpacing,
        };
        if (_sequence != 0 && (Int128)now.Value - _lastSent < spacing) return null;
        if (_sequence == ulong.MaxValue) throw new InvalidOperationException("Clock probe sequence exhausted.");
        var probe = new ClockProbe(Session, Generation, _peer.RequesterGeneration, _peer.RequesterRole, new(_sequence + 1), now);
        _sequence++; _flags |= ClockBookkeepingFlags.Pending; _lastSent = now.Value;
        if (State is ClockMappingState.Recovering or ClockMappingState.Renewing) _recoverySends++;
        _lastRequester = now.Value;
        return probe;
    }

    public ClockObservation Receive(ClockReply reply)
    {
        ValidateNow(reply.RequesterReceived);
        RequireRecoveryTime(reply.RequesterReceived, reply);
        // Only the pending probe's own answer may say anything about the master derivation: classify first, so a stale or
        // foreign reply can neither move the epoch nor discard the calibration (Story 9.4b review, blind hunter 8).
        var outcome = Classify(reply);
        // A reply stamped under a newer master derivation cannot be mapped together with intervals from the old one;
        // with no retained slot there is nothing to discard and no recalibration to report (edge-case E3).
        if (outcome == ClockProbeOutcome.Accepted && _derivation is { } installed && reply.Derivation != installed)
        {
            if (reply.Derivation.Value < installed.Value)
                throw new ArgumentException($"Clock reply derivation epoch reversed from {installed.Value} to {reply.Derivation.Value}.");
            if (_count != 0) Recalibrate(ClockRecalibrationCause.MasterRederived, reply.RequesterReceived, reply, ClockMappingState.Expired);
        }
        if (outcome == ClockProbeOutcome.Accepted) _derivation = reply.Derivation;
        var interval = outcome == ClockProbeOutcome.Accepted ? Offset(reply, EndpointUncertainty) : default;
        // Renewing (R2-F1, R3-F1): an exchange concludes the renewal only when it alone qualifies the mapping through the
        // refresh horizon, the width bound that ends ordinary recovery. A wider one cannot see a clock move smaller than
        // its own width, so it is inconclusive: not retained, consumers keep holding, the renewal keeps probing within
        // ordinary recovery's budget, and exhausting that budget recalibrates. A concluding exchange renews when its
        // interval overlaps the old mapping's, widened by the drift allowance for the old interval's age; otherwise the
        // lapse recalibrates.
        var renewing = State == ClockMappingState.Renewing && outcome == ClockProbeOutcome.Accepted;
        if (renewing && !QualifiesThroughRefresh(interval)) outcome = ClockProbeOutcome.Inconclusive;
        var retained = default(ClockInterval);
        // R4-F1: the old side is bounded too. Its narrowest retained interval, drift-widened to now, must itself fit the
        // mapped width, or a long stall would widen it until it swallowed a real move; past that the lapse recalibrates.
        var vouched = renewing && outcome == ClockProbeOutcome.Accepted && TryRetained(reply.RequesterReceived, out retained);
        // A stall past that bound is its own cause, distinct from a concluding exchange that disagrees (MappingLifetime).
        var stalledPastWidth = vouched && !WithinMappedWidth(retained);
        var renewed = vouched && !stalledPastWidth && interval.Overlaps(retained);
        var exhausted = outcome == ClockProbeOutcome.Inconclusive && _recoverySends == RecoveryProbeLimit;
        var expired = !renewed && outcome != ClockProbeOutcome.Inconclusive && _count != 0 &&
            (Int128)reply.RequesterReceived.Value - NewestReceipt() > MappingLifetime;
        if (!expired && outcome == ClockProbeOutcome.Accepted && _count != 0 &&
            TryBest(reply.RequesterReceived, null, out var old, out _) && !interval.Overlaps(old))
            outcome = ClockProbeOutcome.Disjoint;

        Span<ClockSlot> candidate = stackalloc ClockSlot[ProbeCapacity];
        candidate.Clear();
        var candidateCount = expired ? 0 : _count;
        var candidateNext = expired ? 0 : _next;
        var candidateAnchor = _anchor;
        if (outcome == ClockProbeOutcome.Accepted)
        {
            // The interval midpoint lies within I64 for validated nonnegative I64 endpoints.
            candidateAnchor = interval.Midpoint;
            var lower = interval.Lower; var upper = interval.Upper;
            for (var i = 0; i < candidateCount; i++)
            {
                if (candidateCount == ProbeCapacity && i == candidateNext) continue;
                var prior = Interval(_slots[i]);
                lower = Int128.Min(lower, prior.Lower); upper = Int128.Max(upper, prior.Upper);
            }
            if (upper - lower > MaximumOffsetSpan) outcome = ClockProbeOutcome.ProfileRange;
            else
            {
                for (var i = 0; i < candidateCount; i++)
                {
                    if (candidateCount == ProbeCapacity && i == candidateNext) continue;
                    var slot = _slots[i]; var prior = Interval(slot);
                    candidate[i] = new(slot.Receipt, slot.Sequence,
                        checked((int)(prior.Lower - candidateAnchor)), slot.Width);
                }
                candidate[candidateNext] = new(reply.RequesterReceived.Value, reply.Probe.Sequence.Value,
                    checked((int)(interval.Lower - candidateAnchor)), checked((uint)interval.Width));
                candidateNext = (candidateNext + 1) % ProbeCapacity;
                candidateCount = Math.Min(candidateCount + 1, ProbeCapacity);
            }
        }
        var suspect = outcome is ClockProbeOutcome.Disjoint or ClockProbeOutcome.ProfileRange;
        if ((expired || suspect || exhausted) && DisplayEpoch == ulong.MaxValue)
            throw new InvalidOperationException("Display epoch exhausted.");

        // All checked arithmetic and the entire replacement layout have passed before mutation.
        _lastRequester = reply.RequesterReceived.Value;
        if (Pending == reply.Probe) _flags &= ~ClockBookkeepingFlags.Pending;
        if (expired || suspect)
            Recalibrate(suspect ? outcome == ClockProbeOutcome.Disjoint ? ClockRecalibrationCause.Disjoint : ClockRecalibrationCause.ProfileRange
                    : stalledPastWidth ? ClockRecalibrationCause.StallPastMappedWidth : ClockRecalibrationCause.MappingLifetime,
                reply.RequesterReceived, reply, suspect ? ClockMappingState.Suspect : ClockMappingState.Expired);
        else if (exhausted)
            Recalibrate(ClockRecalibrationCause.ProbeLimit, reply.RequesterReceived, reply, ClockMappingState.Expired);
        if (renewed && outcome == ClockProbeOutcome.Accepted) RecordLateRenewal(reply, retained, interval);
        if (outcome == ClockProbeOutcome.Accepted)
        {
            candidate.CopyTo(_slots);
            _anchor = candidateAnchor; _count = checked((byte)candidateCount); _next = checked((byte)candidateNext);
            // A concluded renewal resumes Ready: its concluding exchange alone covers the refresh horizon (one path).
            if (renewed) { State = ClockMappingState.Ready; ClearRecovery(); }
            else if (_count == ProbeCapacity && !IsQualified && HasOffsetBudget(reply.RequesterReceived, 0))
                State = ClockMappingState.Ready;
        }
        UpdateAvailability(reply.RequesterReceived, outcome == ClockProbeOutcome.Accepted ? interval : null, reply);
        return new(reply, outcome, interval);
    }

    private ClockProbeOutcome Classify(ClockReply reply)
    {
        if (Pending is not { } pending || reply.Probe != pending || reply.Probe.Session != Session ||
            reply.Probe.Generation != Generation || reply.Probe.Sequence.Value == 0)
            return ClockProbeOutcome.WrongIdentity;
        if (reply.MasterReceived.Value < _peer.MasterOrigin.Value || reply.MasterSent.Value < reply.MasterReceived.Value ||
            reply.RequesterReceived.Value < reply.Probe.RequesterSent.Value) return ClockProbeOutcome.Reversed;
        if ((Int128)reply.RequesterReceived.Value - reply.Probe.RequesterSent.Value > ProbeLifetime)
            return ClockProbeOutcome.Expired;
        if ((Int128)reply.RequesterReceived.Value - reply.Probe.RequesterSent.Value -
            ((Int128)reply.MasterSent.Value - reply.MasterReceived.Value) < 0) return ClockProbeOutcome.NegativeRoundTrip;
        return ClockProbeOutcome.Accepted;
    }

    public static ClockInterval Offset(ClockReply reply, TimestampUncertainty uncertainty)
    {
        uncertainty.Validate();
        return new((Int128)reply.MasterSent.Value - reply.RequesterReceived.Value - 2 * uncertainty.Value,
            (Int128)reply.MasterReceived.Value - reply.Probe.RequesterSent.Value + 2 * uncertainty.Value);
    }

    public static ClockInterval Expand(ClockInterval interval, long elapsed)
    {
        if (elapsed < 0 || interval.Width < 0) throw new ArgumentException("Invalid offset interval age.");
        // Ceiling preserves the complete 500 ppm drift interval in integral nanoseconds.
        var drift = ((Int128)elapsed * 500 + 999_999) / 1_000_000;
        return new(interval.Lower - drift, interval.Upper + drift);
    }

    private ClockInterval Interval(ClockSlot slot) =>
        new((Int128)_anchor + slot.LowerDelta, (Int128)_anchor + slot.LowerDelta + slot.Width);

    private long NewestReceipt()
    {
        long newest = 0;
        for (var i = 0; i < _count; i++) newest = Math.Max(newest, _slots[i].Receipt);
        return newest;
    }

    private bool TryBest(MonotonicNanoseconds now, WorkshopClockStamp? capture,
        out ClockInterval best, out ClockSlot selected)
    {
        best = default; selected = default; var found = false;
        for (var i = 0; i < _count; i++)
        {
            var item = _slots[i];
            var age = (Int128)now.Value - item.Receipt;
            if (age < 0 || age > MappingLifetime) continue;
            var expanded = Expand(Interval(item), (long)age);
            if (capture is { } stamp)
            {
                var earliest = (Int128)stamp.Time.Value - stamp.Uncertainty.Value - expanded.Upper;
                // Older unmapped captures cannot borrow a newer calibration's zero-age uncertainty.
                if (earliest < item.Receipt) continue;
            }
            if (!found || expanded.Width < best.Width || (expanded.Width == best.Width &&
                item.Sequence > selected.Sequence))
            { found = true; best = expanded; selected = item; }
        }
        return found;
    }

    public bool TryMap(WorkshopClockStamp capture, MonotonicNanoseconds now, out MappedCapture mapped)
    {
        capture.Validate(); mapped = default;
        if (capture.Domain != WorkshopClockDomain.SimulationMonotonic || capture.Generation != Generation)
            throw new ArgumentException("Sample belongs to another native clock.");
        Observe(now);
        if (!IsQualified || !TryBest(now, capture, out var offset, out var selected)) return false;
        var interval = new ClockInterval(
            (Int128)capture.Time.Value - capture.Uncertainty.Value - offset.Upper,
            (Int128)capture.Time.Value + capture.Uncertainty.Value - offset.Lower);
        if (interval.Width > MaximumMappedWidth || interval.Lower < 0 || interval.Lower > now.Value ||
            (interval.Lower + interval.Upper) / 2 > long.MaxValue) return false;
        mapped = new(interval, now, new(selected.Sequence));
        return true;
    }

    /// <summary>Current master interval, including requester capture uncertainty; authorize pulses only from Lower.</summary>
    public bool TryMasterNow(MonotonicNanoseconds now, out ClockInterval masterInterval)
    {
        Observe(now); masterInterval = default;
        if (!IsQualified || !TryBest(now, null, out var offset, out _)) return false;
        var candidate = new ClockInterval(
            (Int128)now.Value - EndpointUncertainty.Value + offset.Lower - _peer.MasterOrigin.Value,
            (Int128)now.Value + EndpointUncertainty.Value + offset.Upper - _peer.MasterOrigin.Value);
        if (candidate.Lower < 0 || candidate.Upper > long.MaxValue || candidate.Width > MaximumMappedWidth) return false;
        masterInterval = candidate;
        return true;
    }

    /// <summary>The one mapped-width bound: a drift-widened interval plus twice the maximum admitted sample uncertainty
    /// fits <see cref="MaximumMappedWidth"/>. Every slot used for mapping, the old mapping a renewal is checked against
    /// (R4-F1) and every qualifying exchange are held to it.</summary>
    private static bool WithinMappedWidth(ClockInterval widened) =>
        widened.Width + 2 * TimestampUncertainty.Maximum <= MaximumMappedWidth;

    /// <summary>An accepted interval qualifies the mapping when, drift-widened over the refresh horizon, it is within the
    /// mapped width (R3-F1): it ends ordinary recovery and is the only exchange that can conclude a renewal.</summary>
    private static bool QualifiesThroughRefresh(ClockInterval fresh) =>
        WithinMappedWidth(Expand(fresh, RefreshSpacing));

    private bool HasOffsetBudget(MonotonicNanoseconds now, long horizon)
    {
        for (var i = 0; i < _count; i++)
        {
            var slot = _slots[i];
            var age = (Int128)now.Value + horizon - slot.Receipt;
            if (age < 0 || age > MappingLifetime) continue;
            if (WithinMappedWidth(Expand(Interval(slot), (long)age))) return true;
        }
        return false;
    }

    private void ClearRecovery() { _recoveryStarted = 0; _recoverySends = 0; }

    /// <summary>
    /// Every loss of the retained calibration recalibrates and continues (docs/gpu-f32-physics.md game-grade envelope:
    /// numerical residuals clamp or continue, never terminate the tick): a recovery episode that cannot re-qualify, a
    /// lapsed mapping lifetime (except a stalled requester's lapse renewed by a consistent exchange, D23), a disjoint or
    /// out-of-profile interval and a master re-derivation. Every retained slot is
    /// discarded, the display epoch advances so no capture is mapped across the gap, and startup calibration requalifies
    /// it. Nothing is widened, extrapolated or retried into the old episode. Each recalibration is a counted, typed
    /// telemetry occurrence (CCGPU_CLOCK_RECALIBRATED), which every Chrome suite fails on.
    /// </summary>
    private void Recalibrate(ClockRecalibrationCause cause, MonotonicNanoseconds now, ClockReply? reply,
        ClockMappingState next)
    {
        if (DisplayEpoch == ulong.MaxValue) throw new InvalidOperationException("Display epoch exhausted.");
        var record = DescribeRecalibration(cause, now, reply);
        _count = _next = 0; DisplayEpoch++;
        State = next;
        ClearRecovery();
        Recalibrations++;
        LastRecalibration = new(_peer.RequesterRole, cause, Recalibrations, DisplayEpoch, now);
        Console.WriteLine("CCGPU_CLOCK_RECALIBRATED " + record);
    }

    /// <summary>Recalibrations of this mapping so far: the typed telemetry count the browser suites read.</summary>
    public ulong Recalibrations { get; private set; }
    public ClockRecalibration? LastRecalibration { get; private set; }

    /// <summary>Replies the wire decoder rejected (<see cref="ClockProbeOutcome.Malformed"/>), counted.</summary>
    public ulong MalformedReplies { get; private set; }

    /// <summary>
    /// A reply the wire decoder rejected (WorkshopClockWire.DecodeReply) is never applied and never silently dropped
    /// (Murdoch-2, Story 9.4b): it is a counted, typed <see cref="ClockProbeOutcome.Malformed"/> occurrence
    /// (CCGPU_CLOCK_REPLY_REJECTED), which every Chrome suite fails on. The outstanding probe is left to expire, so
    /// the mapping recovers or recalibrates on its ordinary path. Only the wire's one reply path calls it.
    /// </summary>
    internal void RejectMalformedReply(int length, MonotonicNanoseconds received, string detail)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentException.ThrowIfNullOrEmpty(detail);
        received.Validate();
        if (MalformedReplies == ulong.MaxValue) throw new InvalidOperationException("Malformed reply count exhausted.");
        MalformedReplies++;
        const ClockProbeOutcome outcome = ClockProbeOutcome.Malformed;
        using var bytes = new System.IO.MemoryStream();
        using (var json = new System.Text.Json.Utf8JsonWriter(bytes))
        {
            json.WriteStartObject();
            json.WriteNumber("role", (uint)_peer.RequesterRole);
            json.WriteNumber("outcome", (int)outcome);
            json.WriteString("outcomeName", outcome.ToString());
            json.WriteString("count", MalformedReplies.ToString(System.Globalization.CultureInfo.InvariantCulture));
            json.WriteNumber("length", length);
            json.WriteString("received", received.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            json.WriteString("detail", detail);
            json.WriteEndObject();
        }
        Console.WriteLine("CCGPU_CLOCK_REPLY_REJECTED " + System.Text.Encoding.UTF8.GetString(bytes.GetBuffer(), 0, checked((int)bytes.Length)));
    }

    private string DescribeRecalibration(ClockRecalibrationCause cause, MonotonicNanoseconds now, ClockReply? reply)
    {
        using var bytes = new System.IO.MemoryStream();
        using (var json = new System.Text.Json.Utf8JsonWriter(bytes))
        {
            void Exact(string name, Int128 value) =>
                json.WriteString(name, value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            void Probe(ClockProbe probe)
            {
                json.WriteStartObject();
                Exact("sessionLow", probe.Session.Low); Exact("sessionHigh", probe.Session.High);
                Exact("masterGeneration", probe.Generation.Value);
                Exact("requesterGeneration", probe.RequesterGeneration.Value);
                json.WriteNumber("role", (uint)probe.RequesterRole);
                Exact("sequence", probe.Sequence.Value); Exact("sent", probe.RequesterSent.Value);
                json.WriteEndObject();
            }
            json.WriteStartObject();
            json.WriteNumber("role", (uint)_peer.RequesterRole);
            json.WriteNumber("cause", (byte)cause);
            json.WriteString("causeName", cause.ToString());
            Exact("count", Recalibrations + 1);
            json.WriteNumber("stateBefore", (byte)State);
            Exact("sessionLow", Session.Low); Exact("sessionHigh", Session.High);
            Exact("masterGeneration", Generation.Value);
            Exact("requesterGeneration", _peer.RequesterGeneration.Value);
            Exact("masterOrigin", _peer.MasterOrigin.Value);
            if (_derivation is { } derivation) Exact("derivation", derivation.Value); else json.WriteNull("derivation");
            Exact("uncertainty", EndpointUncertainty.Value);
            Exact("displayEpoch", DisplayEpoch); Exact("now", now.Value);
            Exact("recoveryStarted", _recoveryStarted);
            json.WriteNumber("recoverySends", _recoverySends);
            json.WritePropertyName("pending");
            if (Pending is { } pending) Probe(pending); else json.WriteNullValue();
            json.WritePropertyName("reply");
            if (reply is { } received)
            {
                json.WriteStartObject();
                json.WritePropertyName("probe"); Probe(received.Probe);
                Exact("masterReceived", received.MasterReceived.Value);
                Exact("masterSent", received.MasterSent.Value);
                Exact("requesterReceived", received.RequesterReceived.Value);
                json.WriteEndObject();
            }
            else json.WriteNullValue();
            // These slots may already include the triggering accepted reply.
            json.WriteStartArray("slotsBefore");
            for (var i = 0; i < _count; i++)
            {
                var slot = _slots[i]; var interval = Interval(slot);
                json.WriteStartObject();
                Exact("receipt", slot.Receipt); Exact("sequence", slot.Sequence);
                Exact("lower", interval.Lower); Exact("upper", interval.Upper);
                json.WriteEndObject();
            }
            json.WriteEndArray(); json.WriteEndObject();
        }
        return System.Text.Encoding.UTF8.GetString(bytes.GetBuffer(), 0, checked((int)bytes.Length));
    }

    private void RequireRecoveryTime(MonotonicNanoseconds now, ClockReply? reply = null)
    {
        // The reply at the exact endpoint may settle; one nanosecond later cannot.
        if (State is ClockMappingState.Recovering or ClockMappingState.Renewing && (Int128)now.Value - _recoveryStarted > RecoveryLifetime)
            Recalibrate(ClockRecalibrationCause.Elapsed, now, reply, ClockMappingState.Expired);
    }

    private void UpdateAvailability(MonotonicNanoseconds now, ClockInterval? acceptedInterval = null, ClockReply? reply = null)
    {
        if (!IsQualified) return;
        var covered = HasOffsetBudget(now, RefreshSpacing);
        if (State == ClockMappingState.Ready && !covered)
        {
            State = ClockMappingState.Recovering;
            _recoveryStarted = now.Value; _recoverySends = 0;
        }
        else if (State == ClockMappingState.Recovering && acceptedInterval is { } fresh && covered && QualifiesThroughRefresh(fresh))
        {
            State = ClockMappingState.Ready;
            ClearRecovery();
        }
        if (State == ClockMappingState.Recovering && _recoverySends == RecoveryProbeLimit && Pending is null)
            Recalibrate(ClockRecalibrationCause.ProbeLimit, now, reply, ClockMappingState.Expired);
    }

    public void Observe(MonotonicNanoseconds now)
    {
        ValidateNow(now);
        RequireRecoveryTime(now);
        // A renewal's retained slots are past their lifetime by definition; its time bound is RequireRecoveryTime's.
        if (State != ClockMappingState.Renewing && _count != 0 && (Int128)now.Value - NewestReceipt() > MappingLifetime)
        {
            if (LapsedWhileStalled()) BeginRenewal(now);
            else Recalibrate(ClockRecalibrationCause.MappingLifetime, now, null, ClockMappingState.Expired);
        }
        _lastRequester = now.Value;
        UpdateAvailability(now);
    }

    /// <summary>
    /// The one lapse that is renewed and checked rather than recalibrated (R2-F1): the requester's own event loop did not
    /// run. No probe is outstanding, and the requester last ran before its next probe fell due, which was itself before
    /// the mapping's lifetime ended, so a requester that was running would have refreshed the mapping in time. A lapse
    /// with a probe outstanding (the master or the reply path failed) or of a requester that kept running is not covered.
    /// </summary>
    private bool LapsedWhileStalled()
    {
        if (State is not (ClockMappingState.Ready or ClockMappingState.Recovering) || Pending is not null) return false;
        var due = (Int128)_lastSent + (State == ClockMappingState.Ready ? RefreshSpacing : StartupSpacing);
        return due < (Int128)NewestReceipt() + MappingLifetime && _lastRequester < due;
    }

    private void BeginRenewal(MonotonicNanoseconds now)
    {
        _renewalLapseAge = checked((long)((Int128)now.Value - NewestReceipt()));
        State = ClockMappingState.Renewing;
        ClearRecovery();
        _recoveryStarted = now.Value; // the renewal's budget runs from the lapse, as recovery's runs from its start
    }

    /// <summary>The old mapping at <paramref name="now"/>: its narrowest retained interval widened by the 500 ppm drift
    /// allowance for its age, the lifetime aside (every retained slot is past it while renewing).</summary>
    private bool TryRetained(MonotonicNanoseconds now, out ClockInterval best)
    {
        best = default; var found = false;
        for (var i = 0; i < _count; i++)
        {
            var age = (Int128)now.Value - _slots[i].Receipt;
            if (age < 0) continue;
            var expanded = Expand(Interval(_slots[i]), (long)age);
            if (!found || expanded.Width < best.Width) { found = true; best = expanded; }
        }
        return found;
    }

    /// <summary>Lapses of a stalled requester renewed by a consistent fresh exchange: counted, typed
    /// CCGPU_CLOCK_RENEWED_LATE telemetry the Chrome suites report but do not fail on. Not a recalibration: the
    /// derivation, display epoch and retained calibration are kept.</summary>
    public ulong LateRenewals { get; private set; }

    private void RecordLateRenewal(ClockReply reply, ClockInterval retained, ClockInterval fresh)
    {
        if (LateRenewals == ulong.MaxValue) throw new InvalidOperationException("Late renewal count exhausted.");
        LateRenewals++;
        using var bytes = new System.IO.MemoryStream();
        using (var json = new System.Text.Json.Utf8JsonWriter(bytes))
        {
            void Exact(string name, Int128 value) =>
                json.WriteString(name, value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            json.WriteStartObject();
            json.WriteNumber("role", (uint)_peer.RequesterRole);
            Exact("count", LateRenewals);
            Exact("lapseAge", _renewalLapseAge);
            json.WriteNumber("exchanges", _recoverySends);
            Exact("renewedAt", reply.RequesterReceived.Value);
            Exact("oldLower", retained.Lower); Exact("oldUpper", retained.Upper);
            Exact("newLower", fresh.Lower); Exact("newUpper", fresh.Upper);
            if (_derivation is { } derivation) Exact("derivation", derivation.Value); else json.WriteNull("derivation");
            Exact("displayEpoch", DisplayEpoch);
            json.WriteEndObject();
        }
        Console.WriteLine("CCGPU_CLOCK_RENEWED_LATE " + System.Text.Encoding.UTF8.GetString(bytes.GetBuffer(), 0, checked((int)bytes.Length)));
    }

    public void Retire()
    {
        _flags = ClockBookkeepingFlags.None; _count = _next = 0; State = ClockMappingState.Retired;
        ClearRecovery();
    }
}
