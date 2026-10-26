using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace CuriousContraptions.E2eRunner;

/// <summary>The runner's verbs (the CLI boundary converts the first argument to one of these).</summary>
internal enum Verb { Full = 1, Select, Run, Calibrate, DeferredFilter }

/// <summary>The runner's options.</summary>
internal enum Option { Out = 1, Workers, Durations, Diff, Changed }

/// <summary>A parsed command line.</summary>
internal sealed record CommandLine(Verb Verb, IReadOnlyDictionary<Option, string> Options)
{
    private static readonly Dictionary<string, Verb> Verbs = new(StringComparer.Ordinal)
    {
        ["full"] = Verb.Full, ["select"] = Verb.Select, ["run"] = Verb.Run, ["calibrate"] = Verb.Calibrate,
        ["deferred-filter"] = Verb.DeferredFilter,
    };
    private static readonly Dictionary<string, Option> Names = new(StringComparer.Ordinal)
    {
        ["--out"] = Option.Out, ["--workers"] = Option.Workers, ["--durations"] = Option.Durations, ["--diff"] = Option.Diff,
        ["--changed"] = Option.Changed,
    };
    private static readonly Dictionary<Verb, Option[]> Allowed = new()
    {
        [Verb.Full] = [Option.Out, Option.Workers, Option.Durations],
        [Verb.Select] = [Option.Diff, Option.Changed],
        [Verb.Run] = [Option.Diff, Option.Changed, Option.Out, Option.Workers, Option.Durations],
        [Verb.Calibrate] = [Option.Out],
        [Verb.DeferredFilter] = [],
    };
    private static readonly Dictionary<Verb, Option[]> Required = new()
    {
        [Verb.Full] = [Option.Out], [Verb.Select] = [], [Verb.Run] = [Option.Out], [Verb.Calibrate] = [Option.Out],
        [Verb.DeferredFilter] = [],
    };

    public const string Usage = """
        Usage: dotnet run --project tools/E2eRunner -- <verb> [options]
          full --out D [--workers k] [--durations S]      publish, then every tools/e2e suite: SerialAlone suites alone, Parallel ones on N workers
          select (--diff B | --changed F)                 print the Tier 2 selection with one reason per suite
          run (--diff B | --changed F) --out D            publish, then run the Tier 2 selection (the full run when Mode: Full)
          calibrate --out D                               publish, then measure N and print the MeasuredWorkers.cs value
          deferred-filter                                 print the E2eRunner.Tests filter that excludes the BrowserDeferral list
        """;

    public static CommandLine Parse(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        if (arguments.Count == 0 || !Verbs.TryGetValue(arguments[0], out var verb)) throw new RunnerException(RunnerError.Usage, Usage);
        var options = new Dictionary<Option, string>();
        for (var index = 1; index < arguments.Count; index += 2)
        {
            if (!Names.TryGetValue(arguments[index], out var option) || !Allowed[verb].Contains(option) || index + 1 >= arguments.Count ||
                !options.TryAdd(option, arguments[index + 1]))
                throw new RunnerException(RunnerError.Usage, $"Unknown, repeated or valueless option '{arguments[index]}' for {arguments[0]}.\n{Usage}");
        }
        foreach (var option in Required[verb].Where(option => !options.ContainsKey(option)))
            throw new RunnerException(RunnerError.Usage, $"{arguments[0]} needs {option}.\n{Usage}");
        if (verb is Verb.Select or Verb.Run && options.ContainsKey(Option.Diff) == options.ContainsKey(Option.Changed))
            throw new RunnerException(RunnerError.Usage, $"{arguments[0]} needs exactly one of --diff and --changed.\n{Usage}");
        return new CommandLine(verb, options);
    }

    public int? Workers => Options.TryGetValue(Option.Workers, out var value)
        ? int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var workers) && workers is >= 1 and <= RunnerLock.MaxWorkers
            ? workers : throw new RunnerException(RunnerError.Usage, $"--workers must be 1..{RunnerLock.MaxWorkers}.")
        : null;
}

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try { return await RunAsync(CommandLine.Parse(args)).ConfigureAwait(false); }
        catch (RunnerException refusal)
        {
            await Console.Error.WriteLineAsync(refusal.Message).ConfigureAwait(false);
            return 2;
        }
    }

    private static string Home => Environment.GetEnvironmentVariable("HOME") ?? throw new RunnerException(RunnerError.ToolchainMissing, "HOME is not set.");

    private static async Task<int> RunAsync(CommandLine command)
    {
        if (command.Verb == Verb.DeferredFilter)
        {
            Console.WriteLine(BrowserDeferral.LocalFilter());
            return 0;
        }
        var root = Processes.Git(Directory.GetCurrentDirectory(), "rev-parse", "--show-toplevel").Trim();
        if (command.Verb == Verb.Select)
        {
            foreach (var line in Select(root, command).Lines()) Console.WriteLine(line);
            return 0;
        }
        RequireDeclaredSuites(root);
        var output = NewOutputDirectory(command.Options[Option.Out]);
        using var runner = RunnerLock.Acquire(RunnerLock.For(root));
        var toolchain = Toolchain.Resolve(Home);
        var publish = await PublishAsync(root, output).ConfigureAwait(false);
        if (command.Verb == Verb.Calibrate) return await CalibrateAsync(root, output, toolchain, publish).ConfigureAwait(false);

        var memory = MeasuredWorkers.MachineMemory();
        var workers = MeasuredWorkers.Resolve(MeasuredWorkers.Current, command.Workers, MeasuredWorkers.MachineCpus, memory);
        IReadOnlyList<SuiteDeclaration> suites = SuiteCatalogue.All;
        IReadOnlyList<string> selectionLines = ["Mode: Full (full verb)"];
        if (command.Verb == Verb.Run)
        {
            var chosen = Select(root, command);
            selectionLines = [.. chosen.Lines()];
            suites = chosen.Mode == SelectionMode.Full ? SuiteCatalogue.All
                : [.. SuiteCatalogue.All.Where(declaration => chosen.Suites.Any(suite => suite.Suite == declaration.Id))];
        }
        var durations = command.Options.TryGetValue(Option.Durations, out var durationsPath) ? RunSummary.Durations(durationsPath) : null;
        var summary = await RunEngine.RunAsync(
            new RunOptions(root, output, BundleDirectory(root), command.Verb.ToString(), Files(suites), workers, durations, selectionLines,
                Header(root, toolchain, workers, memory, publish)),
            Dependencies(root, toolchain, MeasuredWorkers.Current is { } measured ? measured.IdleLoad + 2 : null), CancellationToken.None)
            .ConfigureAwait(false);
        Console.WriteLine($"{summary.Verdict}: {Path.Combine(output, "summary.txt")}");
        return RunSummary.ExitCode(summary.Verdict);
    }

    private static Selection Select(string root, CommandLine command)
    {
        var changes = command.Options.TryGetValue(Option.Diff, out var baseRef) ? ChangeSet.FromDiff(root, baseRef)
            : ChangeSet.FromList(root, command.Options[Option.Changed]);
        return Selector.Select(changes.Paths, new ImpactContext(root, changes.BaseReader(root)));
    }

    private static string BundleDirectory(string root) => Path.Combine(root, "CuriousContraptions.web", "AppBundle");

    /// <summary>The runner publishes the production bundle itself (`dotnet publish CuriousContraptions.web`), so a run always tests the
    /// bundle of the tree it records; the publish log is evidence, and a failed publish is a typed refusal.</summary>
    private static async Task<string> PublishAsync(string root, string output)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = root,
        };
        foreach (var argument in new[] { "publish", "CuriousContraptions.web", "-nologo" }) start.ArgumentList.Add(argument);
        var clock = Stopwatch.StartNew();
        using var process = new Process();
        process.StartInfo = start;
        process.Start();
        var error = process.StandardError.ReadToEndAsync();
        var log = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
        await process.WaitForExitAsync().ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(output, "publish.log"), log + await error.ConfigureAwait(false)).ConfigureAwait(false);
        if (process.ExitCode != 0)
            throw new RunnerException(RunnerError.PublishFailed, $"dotnet publish CuriousContraptions.web exited {process.ExitCode}; see {Path.Combine(output, "publish.log")}.");
        return FormattableString.Invariant($"dotnet publish CuriousContraptions.web: exit 0 in {clock.Elapsed.TotalSeconds:F1} s (publish.log)");
    }

    private static List<ScheduledFile> Files(IEnumerable<SuiteDeclaration> suites) =>
        [.. suites.Select(declaration => new ScheduledFile(SuiteCatalogue.FileName(declaration.Id), declaration.Timing))];

    private static string NewOutputDirectory(string path)
    {
        var full = Path.GetFullPath(path);
        if (Directory.Exists(full) || File.Exists(full))
            throw new RunnerException(RunnerError.OutputExists, $"{full} already exists; every run writes a fresh output directory.");
        Directory.CreateDirectory(full);
        return full;
    }

    /// <summary>The runner refuses to start (no Chrome launched) unless tools/e2e/*.test.ts and the catalogue are a bijection.</summary>
    internal static void RequireDeclaredSuites(string root)
    {
        var files = Directory.EnumerateFiles(Path.Combine(root, SuiteCatalogue.SuiteDirectory), "*.test.ts")
            .Select(file => SuiteCatalogue.SuiteDirectory + "/" + Path.GetFileName(file)).ToArray();
        var undeclared = files.Where(file => SuiteCatalogue.TryParsePath(file) is null).Order(StringComparer.Ordinal).ToArray();
        if (undeclared.Length > 0)
            throw new RunnerException(RunnerError.UndeclaredSuite, $"Undeclared suite file(s): {string.Join(", ", undeclared)} (declare each in tools/E2eRunner/SuiteCatalogue.cs).");
        var missing = SuiteCatalogue.All.Select(declaration => SuiteCatalogue.RelativePath(declaration.Id)).Except(files).ToArray();
        if (missing.Length > 0)
            throw new RunnerException(RunnerError.UndeclaredSuite, $"Declared suite file(s) missing: {string.Join(", ", missing)}.");
    }

    private static Dictionary<string, string> Header(string root, Toolchain toolchain, int workers, long memory, string publish)
    {
        var source = SourceIdentity.Capture(root);
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["runnerSourceDigest"] = source.RunnerSourceDigest, ["sourceHead"] = source.Head, ["sourceStatusDigest"] = source.StatusDigest,
            ["publish"] = publish,
            ["toolchain"] = $"tsx {toolchain.TsxVersion} ({toolchain.TsxLoader}); playwright {toolchain.PlaywrightVersion}; node {toolchain.NodeVersion}; {toolchain.ChromeVersion}",
            ["machine"] = FormattableString.Invariant($"{MeasuredWorkers.MachineCpus} logical CPUs, {memory} bytes"),
            ["measuredWorkers"] = MeasuredWorkers.Current is { } measured
                ? FormattableString.Invariant($"N={measured.Workers} ({measured.Date}, {measured.Evidence})") : "uncalibrated (serial)",
            ["workersUsed"] = workers.ToString(CultureInfo.InvariantCulture),
        };
    }

    private static RunDependencies Dependencies(string root, Toolchain toolchain, double? threshold) => new()
    {
        SuiteSources = () => SourceIdentity.SuiteSources(root),
        Watch = directory => new FileSystemBundleWatcher(directory),
        Ports = new FreePortSource(),
        TsxLoader = toolchain.TsxLoader,
        ParentEnvironment = Environment.GetEnvironmentVariables(),
        WallLimit = SuiteProcess.WallLimit,
        SuitePath = label => SuiteCatalogue.RelativePath(SuiteCatalogue.TryParsePath($"{SuiteCatalogue.SuiteDirectory}/{label}.test.ts")
            ?? throw new RunnerException(RunnerError.UndeclaredSuite, label)),
        SampleLoad = true,
        ContentionThreshold = threshold,
        CheckForeignChrome = true,
    };

    /// <summary>D9: Phase S once alone (the serial baseline and the solo load), then the Parallel set at each candidate N, under the one
    /// runner lock, then the N rule and the MeasuredWorkers.cs value.</summary>
    private static async Task<int> CalibrateAsync(string root, string output, Toolchain toolchain, string publish)
    {
        var memory = MeasuredWorkers.MachineMemory();
        var cpus = MeasuredWorkers.MachineCpus;
        var report = new StringBuilder();
        var idleAtStart = ProcessTable.LoadAverage();
        var dependencies = Dependencies(root, toolchain, threshold: null);
        var header = Header(root, toolchain, 1, memory, publish);

        async Task<RunSummary> Trial(string name, IEnumerable<SuiteDeclaration> suites, int workers, IReadOnlyDictionary<string, double>? durations)
        {
            var directory = Path.Combine(output, name);
            Directory.CreateDirectory(directory);
            return await RunEngine.RunAsync(new RunOptions(root, directory, BundleDirectory(root), $"calibrate {name}", Files(suites), workers,
                durations, [$"calibration {name}"], header), dependencies, CancellationToken.None).ConfigureAwait(false);
        }

        var serial = await Trial("serial", SuiteCatalogue.All.Where(declaration => declaration.Timing == SuiteTiming.SerialAlone), 1, null)
            .ConfigureAwait(false);
        var soloLoad = serial.Files.Select(file => file.LoadAfter).OfType<double>().DefaultIfEmpty(idleAtStart).Max();
        var trials = new List<CalibrationTrial>();
        IReadOnlyDictionary<string, double>? baseline = null;
        foreach (var workers in Calibration.Candidates(cpus))
        {
            var run = await Trial(FormattableString.Invariant($"trial-{workers}"),
                SuiteCatalogue.All.Where(declaration => declaration.Timing == SuiteTiming.Parallel), workers, baseline).ConfigureAwait(false);
            var trial = new CalibrationTrial(workers, (run.Finished - run.Started).TotalSeconds, run.Verdict == Verdict.Pass,
                run.Files.Sum(file => file.Tap?.DebtFaults ?? 0), long.Parse(run.Header["peakRssBytes"], CultureInfo.InvariantCulture),
                run.Files.ToDictionary(file => file.Label, file => file.DurationMs, StringComparer.Ordinal));
            trials.Add(trial);
            baseline ??= trial.DurationsMs;
            if (run.Verdict is Verdict.IdentityChanged or Verdict.Aborted) break;
            if (trial.PeakRssBytes > memory / 2) break;
        }
        var chosen = Calibration.Choose(trials, memory);
        report.AppendLine(CultureInfo.InvariantCulture, $"# Calibration {DateTimeOffset.UtcNow:O}");
        report.AppendLine(CultureInfo.InvariantCulture, $"machine: {cpus} logical CPUs, {memory} bytes; {toolchain.ChromeVersion}; candidates {string.Join(",", Calibration.Candidates(cpus))}");
        report.AppendLine(CultureInfo.InvariantCulture, $"load average at start: {idleAtStart:F2}; highest after a SerialAlone file alone: {soloLoad:F2}");
        report.AppendLine(CultureInfo.InvariantCulture, $"Phase S alone: verdict {serial.Verdict}, {(serial.Finished - serial.Started).TotalSeconds:F1} s");
        var one = trials.FirstOrDefault(trial => trial.Workers == 1);
        if (one is not null)
            report.AppendLine(CultureInfo.InvariantCulture,
                $"like-for-like serial baseline (Phase S + N=1 trial): {(serial.Finished - serial.Started).TotalSeconds + one.WallSeconds:F1} s; historical 26 min 17 s (1577 s)");
        report.AppendLine().Append(Calibration.Table(trials, memory)).AppendLine();
        report.AppendLine(CultureInfo.InvariantCulture, $"chosen N: {(chosen is { } n ? n.ToString(CultureInfo.InvariantCulture) : "none (N=1 failed)")}");
        if (chosen is { } workersChosen)
            report.AppendLine("MeasuredWorkers.Current = " + Calibration.MeasuredWorkersSource(new MeasuredWorkers(workersChosen, cpus, memory,
                toolchain.ChromeVersion, DateTimeOffset.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), Path.GetRelativePath(root, output), soloLoad)));
        await File.WriteAllTextAsync(Path.Combine(output, "calibration.md"), report.ToString()).ConfigureAwait(false);
        Console.Write(report.ToString());
        return chosen is null ? 1 : serial.Verdict == Verdict.Pass ? 0 : 1;
    }
}
