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
public enum ClockMappingState : byte { Calibrating, Ready, Suspect, Expired, Retired, Recovering, Faulted }
public enum ClockProbeOutcome { Accepted, WrongIdentity, Reversed, NegativeRoundTrip, Disjoint, Expired, ProfileRange, Malformed }
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
public readonly record struct ClockReply(ClockProbe Probe, MonotonicNanoseconds MasterReceived,
    MonotonicNanoseconds MasterSent, MonotonicNanoseconds RequesterReceived);
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

[Flags]
internal enum ClockBookkeepingFlags : byte { None = 0, Pending = 1 }

/// <summary>
/// Browser-owned mapping of one native worker origin. External-clock arithmetic is integral.
/// Live payload: eight 24-byte slots plus 80-byte aligned owner fields = 272 bytes. Array/object
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
    public const int BookkeepingBytes = 272;
    public const long RecoveryLifetime = RefreshSpacing;
    public const int RecoveryProbeLimit = ProbeCapacity;
    private readonly WorkshopClockPeer _peer;
    private readonly ClockSlot[] _slots = new ClockSlot[ProbeCapacity];
    private long _anchor;
    private ulong _sequence;
    private long _lastRequester, _lastSent, _recoveryStarted;
    private byte _recoverySends;
    private byte _count, _next;
    private ClockBookkeepingFlags _flags;
    public ClockMappingState State { get; private set; } = ClockMappingState.Calibrating;
    public ulong DisplayEpoch { get; private set; } = 1;
    public RuntimeSessionId Session => _peer.Session;
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
        if (State is ClockMappingState.Retired or ClockMappingState.Faulted || now.Value < _lastRequester)
            throw new InvalidOperationException("Native browser clock retired, faulted or reversed.");
    }

    public ClockProbe? BeginProbe(MonotonicNanoseconds now)
    {
        Observe(now);
        if (State == ClockMappingState.Recovering && _recoverySends == RecoveryProbeLimit) return null;
        if (Pending is { } pending && (Int128)now.Value - pending.RequesterSent.Value < ProbeLifetime) return null;
        var spacing = State == ClockMappingState.Ready ? RefreshSpacing : StartupSpacing;
        if (_sequence != 0 && (Int128)now.Value - _lastSent < spacing) return null;
        if (_sequence == ulong.MaxValue) throw new InvalidOperationException("Clock probe sequence exhausted.");
        var probe = new ClockProbe(Session, Generation, _peer.RequesterGeneration, _peer.RequesterRole, new(_sequence + 1), now);
        _sequence++; _flags |= ClockBookkeepingFlags.Pending; _lastSent = now.Value;
        if (State == ClockMappingState.Recovering) _recoverySends++;
        _lastRequester = now.Value;
        return probe;
    }

    public ClockObservation Receive(ClockReply reply)
    {
        ValidateNow(reply.RequesterReceived);
        RequireRecoveryTime(reply.RequesterReceived);
        var outcome = Classify(reply);
        var interval = outcome == ClockProbeOutcome.Accepted ? Offset(reply, EndpointUncertainty) : default;
        var expired = _count != 0 && (Int128)reply.RequesterReceived.Value - NewestReceipt() > MappingLifetime;
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
        if ((expired || suspect) && DisplayEpoch == ulong.MaxValue)
            throw new InvalidOperationException("Display epoch exhausted.");

        // All checked arithmetic and the entire replacement layout have passed before mutation.
        _lastRequester = reply.RequesterReceived.Value;
        if (Pending == reply.Probe) _flags &= ~ClockBookkeepingFlags.Pending;
        if (expired || suspect)
        {
            _count = _next = 0; DisplayEpoch++;
            State = suspect ? ClockMappingState.Suspect : ClockMappingState.Expired;
            ClearRecovery();
        }
        if (outcome == ClockProbeOutcome.Accepted)
        {
            candidate.CopyTo(_slots);
            _anchor = candidateAnchor; _count = checked((byte)candidateCount); _next = checked((byte)candidateNext);
            if (_count == ProbeCapacity && !IsQualified && HasOffsetBudget(reply.RequesterReceived, 0))
                State = ClockMappingState.Ready;
        }
        UpdateAvailability(reply.RequesterReceived, outcome == ClockProbeOutcome.Accepted ? interval : null);
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

    private bool HasOffsetBudget(MonotonicNanoseconds now, long horizon)
    {
        for (var i = 0; i < _count; i++)
        {
            var slot = _slots[i];
            var age = (Int128)now.Value + horizon - slot.Receipt;
            if (age < 0 || age > MappingLifetime) continue;
            if (Expand(Interval(slot), (long)age).Width + 2 * TimestampUncertainty.Maximum <= MaximumMappedWidth)
                return true;
        }
        return false;
    }

    private void ClearRecovery() { _recoveryStarted = 0; _recoverySends = 0; }

    private void FailRecovery()
    {
        State = ClockMappingState.Faulted;
        _flags &= ~ClockBookkeepingFlags.Pending;
        throw new InvalidOperationException("Clock mapping recovery exhausted its bounded episode.");
    }

    private void RequireRecoveryTime(MonotonicNanoseconds now)
    {
        // The reply at the exact endpoint may settle; one nanosecond later cannot.
        if (State == ClockMappingState.Recovering && (Int128)now.Value - _recoveryStarted > RecoveryLifetime)
            FailRecovery();
    }

    private void UpdateAvailability(MonotonicNanoseconds now, ClockInterval? acceptedInterval = null)
    {
        if (!IsQualified) return;
        var covered = HasOffsetBudget(now, RefreshSpacing);
        if (State == ClockMappingState.Ready && !covered)
        {
            State = ClockMappingState.Recovering;
            _recoveryStarted = now.Value; _recoverySends = 0;
        }
        else if (State == ClockMappingState.Recovering && acceptedInterval is { } fresh && covered &&
            Expand(fresh, RefreshSpacing).Width + 2 * TimestampUncertainty.Maximum <= MaximumMappedWidth)
        {
            State = ClockMappingState.Ready;
            ClearRecovery();
        }
        if (State == ClockMappingState.Recovering && _recoverySends == RecoveryProbeLimit && Pending is null)
            FailRecovery();
    }

    public void Observe(MonotonicNanoseconds now)
    {
        ValidateNow(now);
        RequireRecoveryTime(now);
        if (_count != 0 && (Int128)now.Value - NewestReceipt() > MappingLifetime)
        {
            if (DisplayEpoch == ulong.MaxValue) throw new InvalidOperationException("Display epoch exhausted.");
            _count = _next = 0; State = ClockMappingState.Expired; DisplayEpoch++;
            ClearRecovery();
        }
        _lastRequester = now.Value;
        UpdateAvailability(now);
    }

    public void Retire()
    {
        _flags = ClockBookkeepingFlags.None; _count = _next = 0; State = ClockMappingState.Retired;
        ClearRecovery();
    }
}
