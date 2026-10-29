using System;
using System.Collections.Generic;

namespace CuriousContraptions.Gpu;

/// <summary>One derivation exchange of the master context with the Simulation, taken under one derivation epoch.</summary>
public readonly record struct ClockDerivationExchange(long Before, long Master, long After, ClockDerivationEpoch Epoch);
/// <summary>What one exchange proved. Only a bracket at most <see cref="WorkshopClockDerivation.MaximumBracketNanoseconds"/>
/// wide is evidence; a wider one, or none at all, proves nothing either way.</summary>
public enum ClockDerivationOutcome : byte { Consistent, Slewed, Rederived, Inconclusive, Unanswered }
public enum ClockDerivationReport : byte { Check, Startup, Summary }

/// <summary>
/// The Simulation's continuous check of the clock master context (Story 9.4b, P10). The master stamps every reply in the
/// Simulation's native domain derived from the shared isolated time origin plus a correction this check maintains.
/// <list type="bullet">
/// <item>Evidence: an exchange counts only when the Simulation's own [before, after] bracket is at most
/// <see cref="MaximumBracketNanoseconds"/> wide, so it pins the master reading to within <see cref="BoundNanoseconds"/>.
/// A wider bracket (the master or the Simulation was descheduled) is inconclusive and is retried or skipped.</item>
/// <item>Startup: <see cref="StartupExchanges"/> narrow exchanges within <see cref="StartupAttemptLimit"/> attempts derive
/// the constant offset between the contexts' time origins as the startup correction, whatever its size: an offset beyond
/// <see cref="WideStartupOffsetNanoseconds"/> is applied and recorded once as typed, counted ClockOffsetWide diagnostic
/// telemetry (each worker's time origin carries its own host-clock reference, so a host clock step widens it; D14 as
/// amended 19 Oct 2026). Only a derivation that cannot qualify refuses the session: exchanges that admit no common
/// correction beyond the outlier allowance, or an exhausted budget.</item>
/// <item>Running, every <see cref="CheckIntervalMilliseconds"/> between tick turns, up to <see cref="CheckExchangeLimit"/>
/// back-to-back exchanges until one is narrow (the first wakes a master context that slept since the last check; each
/// retried wide exchange is counted), all within one <see cref="ExchangeTimeoutMilliseconds"/> in total: the narrow one,
/// or else the last one answered, is the check. A narrow inconsistent reading moves
/// the correction toward the bracket midpoint. Within <see cref="RederiveThresholdNanoseconds"/> it slews by at most
/// <see cref="SlewStepNanoseconds"/> per check under the same epoch, which requester mappings absorb inside their 500 ppm
/// drift budget; beyond it the correction steps to the midpoint under a new epoch and every requester mapping recalibrates
/// (counted telemetry). Unanswered and inconclusive checks are counted and never stop anything.</item>
/// </list>
/// </summary>
public sealed class WorkshopClockDerivation
{
    public WorkshopDiagnostics Diagnostics { get; }
    public WorkshopClockDerivation(WorkshopDiagnostics? diagnostics = null) => Diagnostics = diagnostics ?? new();
    /// <summary>The share of the declared 100 us timestamp uncertainty that the native clock's 84.1255 us conservative
    /// error leaves, rounded down.</summary>
    public const long BoundNanoseconds = 15_800;
    public const long MaximumBracketNanoseconds = 2 * BoundNanoseconds;
    public const int StartupExchanges = 16;
    public const int StartupAttemptLimit = 500;
    public const int ExchangeTimeoutMilliseconds = 2;
    public const int CheckIntervalMilliseconds = 100;
    /// <summary>Exchanges one running check may take until one is narrow; together they block for at most one
    /// <see cref="ExchangeTimeoutMilliseconds"/>.</summary>
    public const int CheckExchangeLimit = 4;
    /// <summary>5 us per 100 ms check is 50 ppm, a tenth of the 500 ppm drift every requester mapping already budgets.</summary>
    public const long SlewStepNanoseconds = 5_000;
    /// <summary>The declared 100 us timestamp uncertainty: a larger error is re-derived, not slewed.</summary>
    public const long RederiveThresholdNanoseconds = 100_000;
    /// <summary>The declared 100 us timestamp uncertainty: a startup offset beyond it is still derived and applied, and is
    /// recorded as a wide offset (<see cref="WideStartupOffsets"/>).</summary>
    public const long WideStartupOffsetNanoseconds = 100_000;
    public ulong WideStartupOffsets { get; private set; }
    public const int SummaryInterval = 100;

    private long? _pending;
    private bool _pendingRederives;
    // Startup: the corrections every narrow exchange so far admits, [before - master - bound, after - master + bound]
    // intersected over the exchanges.
    private (long Lower, long Upper)? _admitted;
    private readonly List<(long Lower, long Upper)> _narrow = new(16);
    /// <summary>Narrow startup exchanges the qualifying correction may set aside as outliers (one jittered sample must not
    /// refuse the session; a master that is no stable offset of the Simulation clock still does).</summary>
    public const int StartupOutlierLimit = 4;
    public int StartupOutliers { get; private set; }
    private ClockDerivationOutcome _last = ClockDerivationOutcome.Consistent;
    public ClockDerivationEpoch Epoch { get; private set; }
    public long Correction { get; private set; }
    public int Qualified { get; private set; }
    public int StartupAttempts { get; private set; }
    public int StartupInconclusive { get; private set; }
    public int StartupUnanswered { get; private set; }
    public ulong Checks { get; private set; }
    public ulong Slews { get; private set; }
    public ulong Rederivations { get; private set; }
    public ulong Inconclusive { get; private set; }
    public ulong Unanswered { get; private set; }
    public ulong CheckRetries { get; private set; }
    public bool IsQualified => Qualified == StartupExchanges;

    private static void Validate(ClockDerivationExchange exchange)
    {
        if (exchange.Before < 0 || exchange.Master < 0 || exchange.After < exchange.Before)
            throw new ArgumentException($"Invalid clock derivation exchange {exchange}.");
    }

    public static bool IsNarrow(ClockDerivationExchange exchange)
    {
        Validate(exchange);
        return exchange.After - exchange.Before <= MaximumBracketNanoseconds;
    }

    public static bool IsConsistent(ClockDerivationExchange exchange)
    {
        Validate(exchange);
        return (Int128)exchange.Master >= (Int128)exchange.Before - BoundNanoseconds && (Int128)exchange.Master <= (Int128)exchange.After + BoundNanoseconds;
    }

    private void RequireEpoch(ClockDerivationExchange exchange)
    {
        if (_pending is not null) throw new InvalidOperationException("A derivation correction awaits its installation.");
        if (exchange.Epoch != Epoch)
            throw new InvalidOperationException($"Clock derivation exchange epoch {exchange.Epoch.Value} is not the installed epoch {Epoch.Value}.");
    }

    private void StartupAttempt()
    {
        if (IsQualified) throw new InvalidOperationException("The clock derivation is already qualified.");
        if (StartupAttempts == StartupAttemptLimit)
            throw new InvalidOperationException($"Clock master derivation could not be qualified: {Qualified} of {StartupExchanges} " +
                $"narrow consistent exchanges in {StartupAttempts} attempts ({StartupInconclusive} wider than {MaximumBracketNanoseconds} ns, " +
                $"{StartupUnanswered} unanswered).");
        StartupAttempts++;
    }

    /// <summary>
    /// One startup exchange. A wide one is retried. Each narrow one admits the corrections that place its master reading
    /// within the bound of its bracket; the startup correction is the centre of what all of them admit. The contexts'
    /// isolated time origins differ by a constant of a few tens of microseconds in Chrome (measured −27.5 to +7.6 µs), so
    /// the offset is derived, not assumed zero. Narrow exchanges that admit no common correction beyond the outlier
    /// allowance (the master is not a stable offset of the Simulation clock) refuse the session; an offset beyond
    /// <see cref="WideStartupOffsetNanoseconds"/> is applied and recorded. The qualified correction awaits installation
    /// in the ring (<see cref="StartupCorrection"/>, then <see cref="Adopt"/> under the initial epoch) before any check.
    /// </summary>
    public ClockDerivationOutcome Qualify(ClockDerivationExchange exchange)
    {
        RequireEpoch(exchange);
        var narrow = IsNarrow(exchange);
        // Validate the entire interval before attempts, retained evidence or counters change.
        var candidate = (Lower: checked((long)((Int128)exchange.Before - exchange.Master - BoundNanoseconds)),
            Upper: checked((long)((Int128)exchange.After - exchange.Master + BoundNanoseconds)));
        if (!narrow) { StartupAttempt(); StartupInconclusive++; return ClockDerivationOutcome.Inconclusive; }
        var (covered, lower, upper) = MostAdmitted(candidate);
        var outliers = _narrow.Count + 1 - covered;
        if (outliers > StartupOutlierLimit)
            throw new InvalidOperationException("Clock master derivation is not a stable offset within the nanosecond bound.");
        var correction = checked((long)(((Int128)lower + upper) / 2));
        _narrow.EnsureCapacity(_narrow.Count + 1);
        StartupAttempt();
        _narrow.Add(candidate);
        Qualified = Math.Min(covered, StartupExchanges);
        StartupOutliers = outliers;
        _admitted = (lower, upper);
        if (!IsQualified) return ClockDerivationOutcome.Consistent;
        _pending = correction; _pendingRederives = false;
        if (Int128.Abs(correction) > WideStartupOffsetNanoseconds) RecordWideOffset(correction);
        Report(ClockDerivationReport.Startup, ClockDerivationOutcome.Consistent, exchange);
        return ClockDerivationOutcome.Consistent;
    }

    private void RecordWideOffset(long offset)
    {
        WideStartupOffsets++;
        Diagnostics.Capture(() => new ClockWideOffsetDiagnostic(offset, _admitted!.Value.Lower,
            _admitted!.Value.Upper, WideStartupOffsetNanoseconds, WideStartupOffsets));
    }

    private (int Covered, long Lower, long Upper) MostAdmitted((long Lower, long Upper) candidate)
    {
        // Every edge belongs to at least its own interval; no infinity sentinel is needed.
        var best = (Covered: 0, Lower: candidate.Lower, Upper: candidate.Upper);
        for (var edgeIndex = 0; edgeIndex <= _narrow.Count; edgeIndex++)
        {
            var edge = edgeIndex == _narrow.Count ? candidate.Lower : _narrow[edgeIndex].Lower;
            var covered = 0;
            long lower = edge, upper = edge;
            for (var i = 0; i <= _narrow.Count; i++)
            {
                var interval = i == _narrow.Count ? candidate : _narrow[i];
                if (interval.Lower > edge || edge > interval.Upper) continue;
                if (covered++ == 0) { lower = interval.Lower; upper = interval.Upper; }
                else { lower = Math.Max(lower, interval.Lower); upper = Math.Min(upper, interval.Upper); }
            }
            if (covered > best.Covered) best = (covered, lower, upper);
        }
        return best;
    }

    /// <summary>The qualified startup correction the ring must install under the initial epoch.</summary>
    public long StartupCorrection => IsQualified && Checks == 0 && _pending is { } correction ? correction
        : throw new InvalidOperationException("No startup correction awaits installation.");

    public void QualificationMissed()
    {
        StartupAttempt();
        StartupUnanswered++;
    }

    /// <summary>One running check. On Slewed or Rederived, <paramref name="correction"/> is the correction the master must
    /// use from now on; the caller installs it in the ring (a new epoch exactly when Rederived) and calls
    /// <see cref="Adopt"/> with the epoch it was installed under.</summary>
    public ClockDerivationOutcome Check(ClockDerivationExchange exchange, out long correction)
    {
        if (!IsQualified) throw new InvalidOperationException("The clock derivation is not qualified.");
        RequireEpoch(exchange);
        var narrow = IsNarrow(exchange);
        correction = Correction;
        if (!narrow)
        {
            var nextChecks = checked(Checks + 1); var nextInconclusive = checked(Inconclusive + 1);
            Checks = nextChecks; Inconclusive = nextInconclusive;
            return Settle(ClockDerivationOutcome.Inconclusive, exchange);
        }
        if (IsConsistent(exchange)) { Checks = checked(Checks + 1); return Settle(ClockDerivationOutcome.Consistent, exchange); }
        var error = ((Int128)exchange.Before + exchange.After) / 2 - exchange.Master;
        var target = checked((long)(Correction + error));
        var rederives = Int128.Abs(error) > RederiveThresholdNanoseconds;
        if (rederives && Epoch.Value == uint.MaxValue) throw new InvalidOperationException("Clock derivation epoch exhausted.");
        correction = rederives ? target : checked((long)(Correction + Int128.Clamp(error, -SlewStepNanoseconds, SlewStepNanoseconds)));
        var checks = checked(Checks + 1);
        if (rederives) _ = checked(Rederivations + 1); else _ = checked(Slews + 1);
        Checks = checks;
        _pendingRederives = rederives;
        _pending = correction;
        if (_pendingRederives) Rederivations++; else Slews++;
        return Settle(_pendingRederives ? ClockDerivationOutcome.Rederived : ClockDerivationOutcome.Slewed, exchange);
    }

    public void Adopt(ClockDerivationEpoch epoch)
    {
        var expected = _pendingRederives ? Epoch.Value + 1 : Epoch.Value;
        if (_pending is not { } correction || epoch.Value != expected)
            throw new InvalidOperationException($"Clock derivation epoch {epoch.Value} is not the pending correction's epoch {expected}.");
        Epoch = epoch; Correction = correction; _pending = null; _pendingRederives = false;
    }

    /// <summary>A running check's wide exchange that is followed by another exchange of the same check.</summary>
    public void Retried()
    {
        if (!IsQualified) throw new InvalidOperationException("The clock derivation is not qualified.");
        CheckRetries = checked(CheckRetries + 1);
    }

    public void Missed()
    {
        if (!IsQualified) throw new InvalidOperationException("The clock derivation is not qualified.");
        var checks = checked(Checks + 1); var unanswered = checked(Unanswered + 1);
        Checks = checks; Unanswered = unanswered;
        Settle(ClockDerivationOutcome.Unanswered, null);
    }

    // Typed, counted telemetry: every re-derivation, the first slew, inconclusive or unanswered check of a run, and a
    // summary of every counter at qualification and every SummaryInterval checks.
    private ClockDerivationOutcome Settle(ClockDerivationOutcome outcome, ClockDerivationExchange? exchange)
    {
        var report = outcome == ClockDerivationOutcome.Rederived || (outcome != ClockDerivationOutcome.Consistent && _last != outcome);
        _last = outcome;
        if (report) Report(ClockDerivationReport.Check, outcome, exchange);
        if (Checks % SummaryInterval == 0) Report(ClockDerivationReport.Summary, outcome, null);
        return outcome;
    }

    private void Report(ClockDerivationReport report, ClockDerivationOutcome outcome, ClockDerivationExchange? exchange) =>
        Diagnostics.Capture(() => new ClockDerivationDiagnostic(report, outcome, Epoch, Correction, _pending,
            report == ClockDerivationReport.Startup ? _admitted!.Value.Lower : null,
            report == ClockDerivationReport.Startup ? _admitted!.Value.Upper : null, exchange,
            Qualified, StartupAttempts, StartupOutliers, StartupInconclusive, StartupUnanswered,
            Checks, Slews, Rederivations, Inconclusive, Unanswered, CheckRetries));
}
