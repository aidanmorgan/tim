using System;
using System.Collections.Generic;

namespace CuriousContraptions.Gpu;

/// <summary>One derivation exchange of the master context with the Simulation, taken under one derivation epoch.</summary>
public readonly record struct ClockDerivationExchange(double Before, double Master, double After, ClockDerivationEpoch Epoch);
/// <summary>What one exchange proved. Only a bracket at most <see cref="WorkshopClockDerivation.MaximumBracketMilliseconds"/>
/// wide is evidence; a wider one, or none at all, proves nothing either way.</summary>
public enum ClockDerivationOutcome : byte { Consistent, Slewed, Rederived, Inconclusive, Unanswered }
public enum ClockDerivationReport : byte { Check, Startup, Summary }

/// <summary>
/// The Simulation's continuous check of the clock master context (Story 9.4b, P10). The master stamps every reply in the
/// Simulation's native domain derived from the shared isolated time origin plus a correction this check maintains.
/// <list type="bullet">
/// <item>Evidence: an exchange counts only when the Simulation's own [before, after] bracket is at most
/// <see cref="MaximumBracketMilliseconds"/> wide, so it pins the master reading to within <see cref="BoundMilliseconds"/>.
/// A wider bracket (the master or the Simulation was descheduled) is inconclusive and is retried or skipped.</item>
/// <item>Startup: <see cref="StartupExchanges"/> narrow exchanges within <see cref="StartupAttemptLimit"/> attempts derive
/// the constant offset between the contexts' time origins as the startup correction, whatever its size: an offset beyond
/// <see cref="WideStartupOffsetMilliseconds"/> is applied and recorded once as typed, counted CCGPU_CLOCK_OFFSET_WIDE
/// telemetry (each worker's time origin carries its own host-clock reference, so a host clock step widens it; D14 as
/// amended 19 Oct 2026). Only a derivation that cannot qualify refuses the session: exchanges that admit no common
/// correction beyond the outlier allowance, or an exhausted budget.</item>
/// <item>Running, every <see cref="CheckIntervalMilliseconds"/> between tick turns, up to <see cref="CheckExchangeLimit"/>
/// back-to-back exchanges until one is narrow (the first wakes a master context that slept since the last check; each
/// retried wide exchange is counted), all within one <see cref="ExchangeTimeoutMilliseconds"/> in total: the narrow one,
/// or else the last one answered, is the check. A narrow inconsistent reading moves
/// the correction toward the bracket midpoint. Within <see cref="RederiveThresholdMilliseconds"/> it slews by at most
/// <see cref="SlewStepMilliseconds"/> per check under the same epoch, which requester mappings absorb inside their 500 ppm
/// drift budget; beyond it the correction steps to the midpoint under a new epoch and every requester mapping recalibrates
/// (counted telemetry). Unanswered and inconclusive checks are counted and never stop anything.</item>
/// </list>
/// </summary>
public sealed class WorkshopClockDerivation
{
    /// <summary>The share of the declared 100 us timestamp uncertainty that the native clock's 84.1255 us conservative
    /// error leaves, rounded down.</summary>
    public const double BoundMilliseconds = 0.0158;
    public const double MaximumBracketMilliseconds = 2 * BoundMilliseconds;
    public const int StartupExchanges = 16;
    public const int StartupAttemptLimit = 500;
    public const int ExchangeTimeoutMilliseconds = 2;
    public const int CheckIntervalMilliseconds = 100;
    /// <summary>Exchanges one running check may take until one is narrow; together they block for at most one
    /// <see cref="ExchangeTimeoutMilliseconds"/>.</summary>
    public const int CheckExchangeLimit = 4;
    /// <summary>5 us per 100 ms check is 50 ppm, a tenth of the 500 ppm drift every requester mapping already budgets.</summary>
    public const double SlewStepMilliseconds = 0.005;
    /// <summary>The declared 100 us timestamp uncertainty: a larger error is re-derived, not slewed.</summary>
    public const double RederiveThresholdMilliseconds = 0.1;
    /// <summary>The declared 100 us timestamp uncertainty: a startup offset beyond it is still derived and applied, and is
    /// recorded as a wide offset (<see cref="WideStartupOffsets"/>).</summary>
    public const double WideStartupOffsetMilliseconds = 0.1;
    public ulong WideStartupOffsets { get; private set; }
    public const int SummaryInterval = 100;

    private double? _pending;
    private bool _pendingRederives;
    // Startup: the corrections every narrow exchange so far admits, [before - master - bound, after - master + bound]
    // intersected over the exchanges.
    private double _admittedLower = double.NegativeInfinity, _admittedUpper = double.PositiveInfinity;
    private readonly List<(double Lower, double Upper)> _narrow = new(16);
    /// <summary>Narrow startup exchanges the qualifying correction may set aside as outliers (one jittered sample must not
    /// refuse the session; a master that is no stable offset of the Simulation clock still does).</summary>
    public const int StartupOutlierLimit = 4;
    public int StartupOutliers { get; private set; }
    private ClockDerivationOutcome _last = ClockDerivationOutcome.Consistent;
    public ClockDerivationEpoch Epoch { get; private set; }
    public double Correction { get; private set; }
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
        if (!double.IsFinite(exchange.Before) || !double.IsFinite(exchange.Master) || !double.IsFinite(exchange.After) ||
            exchange.Before < 0 || exchange.After < exchange.Before)
            throw new ArgumentException($"Invalid clock derivation exchange {exchange}.");
    }

    public static bool IsNarrow(ClockDerivationExchange exchange)
    {
        Validate(exchange);
        return exchange.After - exchange.Before <= MaximumBracketMilliseconds;
    }

    public static bool IsConsistent(ClockDerivationExchange exchange)
    {
        Validate(exchange);
        return exchange.Master >= exchange.Before - BoundMilliseconds && exchange.Master <= exchange.After + BoundMilliseconds;
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
                $"narrow consistent exchanges in {StartupAttempts} attempts ({StartupInconclusive} wider than {MaximumBracketMilliseconds} ms, " +
                $"{StartupUnanswered} unanswered).");
        StartupAttempts++;
    }

    /// <summary>
    /// One startup exchange. A wide one is retried. Each narrow one admits the corrections that place its master reading
    /// within the bound of its bracket; the startup correction is the centre of what all of them admit. The contexts'
    /// isolated time origins differ by a constant of a few tens of microseconds in Chrome (measured −27.5 to +7.6 µs), so
    /// the offset is derived, not assumed zero. Narrow exchanges that admit no common correction beyond the outlier
    /// allowance (the master is not a stable offset of the Simulation clock) refuse the session; an offset beyond
    /// <see cref="WideStartupOffsetMilliseconds"/> is applied and recorded. The qualified correction awaits installation
    /// in the ring (<see cref="StartupCorrection"/>, then <see cref="Adopt"/> under the initial epoch) before any check.
    /// </summary>
    public ClockDerivationOutcome Qualify(ClockDerivationExchange exchange)
    {
        RequireEpoch(exchange);
        StartupAttempt();
        if (!IsNarrow(exchange)) { StartupInconclusive++; return ClockDerivationOutcome.Inconclusive; }
        _narrow.Add((exchange.Before - exchange.Master - BoundMilliseconds, exchange.After - exchange.Master + BoundMilliseconds));
        // The correction admitted by the most narrow exchanges; the rest are outliers.
        var (covered, lower, upper) = MostAdmitted();
        Qualified = Math.Min(covered, StartupExchanges);
        StartupOutliers = _narrow.Count - covered;
        (_admittedLower, _admittedUpper) = (lower, upper);
        if (StartupOutliers > StartupOutlierLimit)
            throw new InvalidOperationException($"Clock master derivation is not a stable offset within the {BoundMilliseconds} ms bound: " +
                $"the most narrow exchanges admitting one correction are {covered} of {_narrow.Count} (at most {StartupOutlierLimit} " +
                $"outliers); the last was master {exchange.Master:R} ms for the Simulation bracket [{exchange.Before:R}, {exchange.After:R}] ms.");
        if (!IsQualified) return ClockDerivationOutcome.Consistent;
        var correction = (lower + upper) / 2;
        _pending = correction; _pendingRederives = false;
        if (Math.Abs(correction) > WideStartupOffsetMilliseconds) RecordWideOffset(correction);
        Report(ClockDerivationReport.Startup, ClockDerivationOutcome.Consistent, exchange);
        return ClockDerivationOutcome.Consistent;
    }

    private void RecordWideOffset(double offset)
    {
        WideStartupOffsets++;
        using var bytes = new System.IO.MemoryStream();
        using (var json = new System.Text.Json.Utf8JsonWriter(bytes))
        {
            json.WriteStartObject();
            json.WriteNumber("offset", offset);
            json.WriteNumber("admittedLower", _admittedLower); json.WriteNumber("admittedUpper", _admittedUpper);
            json.WriteNumber("admittedWidth", _admittedUpper - _admittedLower);
            json.WriteNumber("declaredUncertainty", WideStartupOffsetMilliseconds);
            json.WriteString("count", WideStartupOffsets.ToString(System.Globalization.CultureInfo.InvariantCulture));
            json.WriteEndObject();
        }
        Console.WriteLine("CCGPU_CLOCK_OFFSET_WIDE " + System.Text.Encoding.UTF8.GetString(bytes.GetBuffer(), 0, checked((int)bytes.Length)));
    }

    private (int Covered, double Lower, double Upper) MostAdmitted()
    {
        // The best correction lies at some interval's lower edge; the first edge with the most intervals wins.
        var best = (Covered: 0, Lower: double.NegativeInfinity, Upper: double.PositiveInfinity);
        foreach (var (edge, _) in _narrow)
        {
            var covered = 0; var lower = double.NegativeInfinity; var upper = double.PositiveInfinity;
            foreach (var (l, u) in _narrow)
                if (l <= edge && edge <= u) { covered++; lower = Math.Max(lower, l); upper = Math.Min(upper, u); }
            if (covered > best.Covered) best = (covered, lower, upper);
        }
        return best;
    }

    /// <summary>The qualified startup correction the ring must install under the initial epoch.</summary>
    public double StartupCorrection => IsQualified && Checks == 0 && _pending is { } correction ? correction
        : throw new InvalidOperationException("No startup correction awaits installation.");

    public void QualificationMissed()
    {
        StartupAttempt();
        StartupUnanswered++;
    }

    /// <summary>One running check. On Slewed or Rederived, <paramref name="correction"/> is the correction the master must
    /// use from now on; the caller installs it in the ring (a new epoch exactly when Rederived) and calls
    /// <see cref="Adopt"/> with the epoch it was installed under.</summary>
    public ClockDerivationOutcome Check(ClockDerivationExchange exchange, out double correction)
    {
        if (!IsQualified) throw new InvalidOperationException("The clock derivation is not qualified.");
        RequireEpoch(exchange);
        Checks++;
        correction = Correction;
        if (!IsNarrow(exchange)) { Inconclusive++; return Settle(ClockDerivationOutcome.Inconclusive, exchange); }
        if (IsConsistent(exchange)) return Settle(ClockDerivationOutcome.Consistent, exchange);
        var target = Correction + ((exchange.Before + exchange.After) / 2 - exchange.Master);
        if (!double.IsFinite(target)) throw new ArgumentException($"Clock derivation correction overflowed for {exchange}.");
        var error = target - Correction;
        _pendingRederives = Math.Abs(error) > RederiveThresholdMilliseconds;
        if (_pendingRederives && Epoch.Value == uint.MaxValue) throw new InvalidOperationException("Clock derivation epoch exhausted.");
        correction = _pendingRederives ? target : Correction + Math.Clamp(error, -SlewStepMilliseconds, SlewStepMilliseconds);
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
        CheckRetries++;
    }

    public void Missed()
    {
        if (!IsQualified) throw new InvalidOperationException("The clock derivation is not qualified.");
        Checks++;
        Unanswered++;
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
        Console.WriteLine("CCGPU_CLOCK_DERIVATION " + Describe(report, outcome, exchange));

    private string Describe(ClockDerivationReport report, ClockDerivationOutcome outcome, ClockDerivationExchange? exchange)
    {
        using var bytes = new System.IO.MemoryStream();
        using (var json = new System.Text.Json.Utf8JsonWriter(bytes))
        {
            json.WriteStartObject();
            json.WriteNumber("report", (byte)report);
            json.WriteString("reportName", report.ToString());
            json.WriteNumber("outcome", (byte)outcome);
            json.WriteString("outcomeName", outcome.ToString());
            json.WriteNumber("epoch", Epoch.Value);
            json.WriteNumber("correction", Correction);
            if (_pending is { } pending) json.WriteNumber("pendingCorrection", pending);
            if (report == ClockDerivationReport.Startup)
            {
                // The derived time-origin offset and how tightly the narrow exchanges pinned it, per session (the evidence
                // for the startup offset distribution; a wide one is also recorded as CCGPU_CLOCK_OFFSET_WIDE).
                json.WriteNumber("startupOffset", (_admittedLower + _admittedUpper) / 2);
                json.WriteNumber("admittedLower", _admittedLower); json.WriteNumber("admittedUpper", _admittedUpper);
                json.WriteNumber("admittedWidth", _admittedUpper - _admittedLower);
            }
            if (exchange is { } value)
            {
                json.WriteNumber("before", value.Before); json.WriteNumber("master", value.Master); json.WriteNumber("after", value.After);
            }
            json.WriteNumber("qualified", Qualified); json.WriteNumber("startupAttempts", StartupAttempts);
            json.WriteNumber("startupOutliers", StartupOutliers);
            json.WriteNumber("startupInconclusive", StartupInconclusive); json.WriteNumber("startupUnanswered", StartupUnanswered);
            json.WriteNumber("checks", Checks); json.WriteNumber("slews", Slews); json.WriteNumber("rederivations", Rederivations);
            json.WriteNumber("inconclusive", Inconclusive); json.WriteNumber("unanswered", Unanswered); json.WriteNumber("checkRetries", CheckRetries);
            json.WriteEndObject();
        }
        return System.Text.Encoding.UTF8.GetString(bytes.GetBuffer(), 0, checked((int)bytes.Length));
    }
}
