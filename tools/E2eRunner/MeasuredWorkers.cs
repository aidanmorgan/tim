using System.Globalization;

namespace CuriousContraptions.E2eRunner;

/// <summary>The calibrated worker count (spec D9), checked in from the `calibrate` evidence it names. A machine whose CPU or memory
/// fingerprint differs refuses Parallel runs until it is recalibrated; `--workers 1` is always allowed. IdleLoad is the 1-minute load
/// average a SerialAlone file reached alone during calibration; a SerialAlone observation above IdleLoad + 2 is Contended.</summary>
internal sealed record MeasuredWorkers(int Workers, int LogicalCpus, long MemoryBytes, string ChromeVersion, string Date, string Evidence,
    double IdleLoad)
{
    /// <summary>The checked-in calibration result (null runs every suite serially). N = 2: at N = 4 a debt fault (cat-020) and a clock
    /// recalibration (cat-048a) failed two files, and N = 6 and 8 failed too (evidence-9r-0a/calibration/calibration.md).</summary>
    public static readonly MeasuredWorkers? Current = new(Workers: 2, LogicalCpus: 16, MemoryBytes: 68719476736,
        ChromeVersion: "Google Chrome 155.0.8059.82", Date: "2026-10-19", Evidence: "_bmad-output/implementation-artifacts/evidence-9r-0a/calibration",
        IdleLoad: 5.23);

    public static int MachineCpus => Environment.ProcessorCount;

    public static long MachineMemory()
    {
        var result = Processes.Run("sysctl", ["-n", "hw.memsize"]);
        return result.ExitCode == 0 && long.TryParse(result.Output.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var bytes)
            ? bytes : throw new InvalidOperationException("sysctl hw.memsize did not return the physical memory.");
    }

    /// <summary>The worker count for a run: serial (1) when asked for or when nothing is calibrated, otherwise the calibrated N (or a
    /// smaller requested N) on a matching machine.</summary>
    public static int Resolve(MeasuredWorkers? measured, int? requested, int cpus, long memory)
    {
        if (requested == 1) return 1;
        if (measured is null)
        {
            if (requested is null) return 1;
            throw new RunnerException(RunnerError.Uncalibrated, "No calibrated worker count is recorded (run `calibrate`); runs are serial.");
        }
        if (measured.LogicalCpus != cpus || measured.MemoryBytes != memory)
            throw new RunnerException(RunnerError.Uncalibrated, FormattableString.Invariant(
                $"This machine ({cpus} CPUs, {memory} bytes) differs from the calibrated one ({measured.LogicalCpus} CPUs, {measured.MemoryBytes} bytes); recalibrate, or use --workers 1."));
        if (requested is { } count && (count < 1 || count > measured.Workers))
            throw new RunnerException(RunnerError.Usage, FormattableString.Invariant($"--workers must be 1..{measured.Workers} (the calibrated N)."));
        return requested ?? measured.Workers;
    }
}
