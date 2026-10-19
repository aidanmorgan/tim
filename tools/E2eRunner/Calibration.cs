using System.Globalization;

namespace CuriousContraptions.E2eRunner;

/// <summary>One calibration trial: the Parallel set on <see cref="Workers"/> workers.</summary>
internal sealed record CalibrationTrial(int Workers, double WallSeconds, bool AllPassed, int DebtFaults, long PeakRssBytes,
    IReadOnlyDictionary<string, double> DurationsMs);

/// <summary>Why a trial's N is not eligible (spec D9).</summary>
internal enum TrialRejection { NotAllPassed = 1, DebtFaults, Memory, SlowerThanTwiceSerial, GainBelowTenPercent }

internal static class Calibration
{
    /// <summary>The candidate worker counts, capped at ⌊logical CPUs / 2⌋.</summary>
    public static IReadOnlyList<int> Candidates(int logicalCpus) => [.. Trials.Where(n => n == 1 || n <= logicalCpus / 2)];

    private static readonly int[] Trials = [1, 2, 4, 6, 8];

    /// <summary>Every rule a trial fails (spec D9). N = 1 is the per-file baseline and only needs to pass.</summary>
    public static IReadOnlyList<TrialRejection> Rejections(CalibrationTrial trial, IReadOnlyList<CalibrationTrial> trials, long physicalMemory)
    {
        ArgumentNullException.ThrowIfNull(trial);
        ArgumentNullException.ThrowIfNull(trials);
        var rejections = new List<TrialRejection>();
        if (!trial.AllPassed) rejections.Add(TrialRejection.NotAllPassed);
        if (trial.DebtFaults > 0) rejections.Add(TrialRejection.DebtFaults);
        if (trial.PeakRssBytes > physicalMemory / 2) rejections.Add(TrialRejection.Memory);
        if (trial.Workers == 1) return rejections;
        var baseline = trials.Single(candidate => candidate.Workers == 1);
        if (trial.DurationsMs.Any(file => !baseline.DurationsMs.TryGetValue(file.Key, out var serial) || file.Value > 2 * serial))
            rejections.Add(TrialRejection.SlowerThanTwiceSerial);
        var lower = trials.Where(candidate => candidate.Workers < trial.Workers).MaxBy(candidate => candidate.Workers);
        if (lower is null || (lower.WallSeconds - trial.WallSeconds) / lower.WallSeconds < 0.10) rejections.Add(TrialRejection.GainBelowTenPercent);
        return rejections;
    }

    /// <summary>N: the largest worker count meeting every rule, or null when even N = 1 fails.</summary>
    public static int? Choose(IReadOnlyList<CalibrationTrial> trials, long physicalMemory) =>
        trials.Where(trial => Rejections(trial, trials, physicalMemory).Count == 0).Select(trial => (int?)trial.Workers).Max();

    public static string Table(IReadOnlyList<CalibrationTrial> trials, long physicalMemory)
    {
        ArgumentNullException.ThrowIfNull(trials);
        var lines = new List<string> { "| N | wall s | pass | debt faults | peak RSS GiB | slowest file vs N=1 | rejections |", "|---|---|---|---|---|---|---|" };
        var baseline = trials.FirstOrDefault(trial => trial.Workers == 1);
        foreach (var trial in trials.OrderBy(trial => trial.Workers))
        {
            var ratios = baseline is null ? [] : trial.DurationsMs.Where(file => baseline.DurationsMs.ContainsKey(file.Key))
                .Select(file => (Name: file.Key, Ratio: file.Value / baseline.DurationsMs[file.Key])).ToArray();
            var slowest = ratios.Length == 0 ? "-" : ratios.MaxBy(file => file.Ratio) is var worst
                ? FormattableString.Invariant($"{worst.Ratio:F2}x ({worst.Name})") : "-";
            lines.Add(FormattableString.Invariant(
                $"| {trial.Workers} | {trial.WallSeconds:F1} | {trial.AllPassed} | {trial.DebtFaults} | {trial.PeakRssBytes / (1024.0 * 1024 * 1024):F2} | {slowest} | {string.Join(",", Rejections(trial, trials, physicalMemory))} |"));
        }
        return string.Join('\n', lines) + "\n";
    }

    /// <summary>The MeasuredWorkers.cs constructor expression for a chosen N.</summary>
    public static string MeasuredWorkersSource(MeasuredWorkers measured)
    {
        ArgumentNullException.ThrowIfNull(measured);
        return string.Create(CultureInfo.InvariantCulture,
            $"new(Workers: {measured.Workers}, LogicalCpus: {measured.LogicalCpus}, MemoryBytes: {measured.MemoryBytes}, ChromeVersion: \"{measured.ChromeVersion}\", Date: \"{measured.Date}\", Evidence: \"{measured.Evidence}\", IdleLoad: {measured.IdleLoad:R})");
    }
}
