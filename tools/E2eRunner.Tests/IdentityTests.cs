using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Xunit;

namespace CuriousContraptions.E2eRunner.Tests;

/// <summary>AC4: the bundle manifest, the real tools/Preview served check and port handling, and a run whose bundle or suite sources
/// change while fixture suites wait (browserless: the `wait` fixture polls for a `release` file in the run's output directory).</summary>
public sealed class IdentityTests : IDisposable
{
    private readonly DirectoryInfo _work = Directory.CreateTempSubdirectory("e2e-identity-");
    private static readonly string Home = Environment.GetEnvironmentVariable("HOME") ?? throw new InvalidOperationException("HOME is not set.");

    public void Dispose() => _work.Delete(true);

    private string Bundle(string name, string script)
    {
        var bundle = Path.Combine(_work.FullName, name);
        Directory.CreateDirectory(Path.Combine(bundle, "js"));
        Directory.CreateDirectory(Path.Combine(bundle, ".well-known"));
        File.WriteAllText(Path.Combine(bundle, "index.html"), "<!doctype html><title>fixture</title>");
        File.WriteAllText(Path.Combine(bundle, "js", "app.js"), script);
        File.WriteAllText(Path.Combine(bundle, ".hidden"), "never served");
        File.WriteAllText(Path.Combine(bundle, ".well-known", "x"), "never served");
        return bundle;
    }

    private sealed class Ports(params int[] ports) : IPortSource
    {
        private readonly Queue<int> _ports = new(ports);
        private readonly FreePortSource _free = new();
        public int Next() => _ports.TryDequeue(out var port) ? port : _free.Next();
    }

    private sealed class HeldPort(int port) : IPortSource
    {
        public int Next() => port;
    }

    /// <summary>Free ports, counted; the last one handed out; an optional action run as each port is asked for.</summary>
    private sealed class CountingPorts(Action? onNext = null) : IPortSource
    {
        private readonly FreePortSource _free = new();
        public int Asked { get; private set; }
        public int Last { get; private set; }

        public int Next()
        {
            Asked++;
            onNext?.Invoke();
            Last = _free.Next();
            return Last;
        }
    }

    private string PreviewLog(string name) => Path.Combine(_work.FullName, name + "-preview.log");

    private static string StopLine(int port) => $"preview[{port}] E2E_RUNNER stop (stop requested): ";

    private sealed class SilentWatcher : IBundleWatcher
    {
        public string? FirstChange => null;
        public event Action<string>? Changed { add { } remove { } }
        public void Dispose() { }
    }

    /// <summary>A watcher the test fires (a bundle change at a chosen moment).</summary>
    private sealed class FiredWatcher : IBundleWatcher
    {
        public string? FirstChange { get; private set; }
        public event Action<string>? Changed;
        public void Fire(string change)
        {
            FirstChange ??= change;
            Changed?.Invoke(change);
        }
        public void Dispose() { }
    }

    /// <summary>A run of the real engine and preview that needs no suite toolchain (no tsx: any file fails before its process).</summary>
    private (RunOptions Options, RunDependencies Dependencies, string Output) Run(string bundle, IPortSource ports, IBundleWatcher watcher,
        IReadOnlyList<ScheduledFile> files, Func<string, string> suitePath, Action? verifyArtifact = null, TimeSpan? httpTimeout = null)
    {
        var output = Path.Combine(_work.FullName, "out-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        var dependencies = new RunDependencies
        {
            VerifyArtifact = verifyArtifact ?? (() => { }),
            SuiteSources = () => "fixed",
            Watch = _ => watcher,
            Ports = ports,
            TsxLoader = "",
            ParentEnvironment = Environment.GetEnvironmentVariables(),
            WallLimit = TimeSpan.FromMinutes(2),
            SuitePath = suitePath,
            HttpTimeout = httpTimeout ?? TimeSpan.FromMinutes(2),
        };
        return (new RunOptions(_work.FullName, output, bundle, "lifecycle-test", files, 1, null, [], new Dictionary<string, string>()), dependencies,
            output);
    }

    // BH3 (Spec Change Log 2): a bundle change during the preview's start stops the attempt in progress, asks for no second port and
    // refuses the run as ServedIdentityMismatch naming the change; no summary is written.
    [Fact]
    public async Task ABundleChangeDuringThePreviewStartIsRefusedWithoutRetrying()
    {
        var token = TestContext.Current.CancellationToken;
        using var watcher = new FiredWatcher();
        var ports = new CountingPorts(() => watcher.Fire("js/app.js changed"));
        var (options, dependencies, output) = Run(Bundle("aborted-start", "one"), ports, watcher, [], label => label);
        var refusal = await Assert.ThrowsAsync<RunnerException>(() => RunEngine.RunAsync(options, dependencies, token));
        Assert.Equal(RunnerError.ServedIdentityMismatch, refusal.Error);
        Assert.Contains("the bundle changed while the preview was starting: bundle watcher: js/app.js changed", refusal.Message,
            StringComparison.Ordinal);
        Assert.Equal(1, ports.Asked);
        Assert.False(File.Exists(Path.Combine(output, "summary.json")));
        var log = await File.ReadAllLinesAsync(Path.Combine(output, "preview.log"), token);
        Assert.StartsWith(StopLine(ports.Last), log[^1], StringComparison.Ordinal);
    }

    // ECH2-3: a start whose cancellation is already requested asks for no port and launches no preview.
    [Fact]
    public async Task APreviewStartAlreadyCancelledMakesNoAttempt()
    {
        var bundle = Bundle("cancelled-start", "one");
        var ports = new CountingPorts();
        using var client = new HttpClient();
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => PreviewHost.StartAsync(TestRepository.Root, bundle, BundleManifest.Compute(bundle),
            ports, client, PreviewLog("cancelled"), ChildProcess.Start, cancelled.Token));
        Assert.Equal(0, ports.Asked);
        Assert.False(File.Exists(PreviewLog("cancelled")));
    }

    /// <summary>A loopback HTTP server that answers its base URI and never answers any other path (a preview that hangs on its files),
    /// counting the file requests it receives. A failure with one client (IOException, ObjectDisposedException, cancellation) closes that
    /// client and does not end the accept loop, whose task never faults; disposing closes every client, including one accepted after.</summary>
    private sealed class RootOnlyServer : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly List<TcpClient> _held = [];
        private readonly CancellationTokenSource _stop = new();
        private bool _disposed;
        private int _fileRequests;

        /// <summary>Listens on the given loopback port (0: a free one).</summary>
        public RootOnlyServer(int port = 0)
        {
            _listener = new TcpListener(IPAddress.Loopback, port);
            _listener.Start();
            _ = AcceptAsync();
        }

        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

        public int FileRequests => Volatile.Read(ref _fileRequests);

        private async Task AcceptAsync()
        {
            while (true)
            {
                TcpClient client;
                try { client = await _listener.AcceptTcpClientAsync(_stop.Token); }
                catch (Exception stopped) when (stopped is OperationCanceledException or ObjectDisposedException or SocketException) { return; }
                try { await AnswerAsync(client); }
                catch (Exception failure) when (failure is IOException or ObjectDisposedException or OperationCanceledException) { client.Dispose(); }
            }
        }

        private async Task AnswerAsync(TcpClient client)
        {
            if (await RequestPathAsync(client) == "/")
            {
                await client.GetStream().WriteAsync("HTTP/1.1 200 OK\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"u8.ToArray(), _stop.Token);
                client.Dispose();
                return;
            }
            Interlocked.Increment(ref _fileRequests);
            lock (_held)
            {
                if (!_disposed)
                {
                    _held.Add(client); // held open, never answered
                    return;
                }
            }
            client.Dispose(); // accepted after Dispose began
        }

        private async Task<string> RequestPathAsync(TcpClient client)
        {
            var request = new StringBuilder();
            var buffer = new byte[1024];
            while (!request.ToString().Contains("\r\n\r\n", StringComparison.Ordinal))
            {
                var read = await client.GetStream().ReadAsync(buffer, _stop.Token);
                if (read == 0) break;
                request.Append(Encoding.ASCII.GetString(buffer, 0, read));
            }
            var requestLine = request.ToString().Split("\r\n")[0].Split(' ');
            return requestLine.Length > 1 ? requestLine[1] : "";
        }

        public void Dispose()
        {
            lock (_held)
            {
                _disposed = true;
                foreach (var client in _held) client.Dispose();
                _held.Clear();
            }
            _stop.Cancel();
            _listener.Dispose();
            _stop.Dispose();
        }
    }

    // VG3-3: at the start a served check the preview does not answer within the HTTP timeout is NotServedInTime, so the start is
    // refused as ServedIdentityMismatch naming that cause, and the attempt (a scripted handle announcing itself) is stopped: its log
    // ends with the stop line.
    [Fact]
    public async Task APreviewThatDoesNotServeItsFilesInTimeIsStoppedAndRefused()
    {
        var token = TestContext.Current.CancellationToken;
        using var server = new RootOnlyServer();
        var bundle = Bundle("unserved", "one");
        using var child = new FakeChild([RootAnswer.EndsAtCancel, RootAnswer.Exited], [OutputAnswer.Ended]);
        var launcher = new FakeLauncher(child, receiver => receiver.Write(OutputStream.Standard, "Workshop preview: ready\n"));
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
        var log = PreviewLog("unserved");
        var refusal = await Assert.ThrowsAsync<RunnerException>(() => PreviewHost.StartAsync(TestRepository.Root, bundle, BundleManifest.Compute(bundle),
            new HeldPort(server.Port), client, log, launcher.Launch, token));
        Assert.Equal(RunnerError.ServedIdentityMismatch, refusal.Error);
        Assert.Contains("did not serve the bundle within the HTTP timeout (1 s)", refusal.Message, StringComparison.Ordinal);
        var lines = await File.ReadAllLinesAsync(log, token);
        Assert.StartsWith(StopLine(server.Port), lines[^1], StringComparison.Ordinal);
    }

    // BH4-1: a served check runs under one shared deadline: once it passes, every fetch in flight is cancelled and no further file is
    // asked for, so a preview that answers nothing costs one timeout and at most one batch of requests (of 20 served files here, more
    // than the parallelism). The outcome names the timeout.
    [Fact]
    public async Task AServedCheckOfAPreviewThatAnswersNothingEndsAtOneDeadline()
    {
        var token = TestContext.Current.CancellationToken;
        using var server = new RootOnlyServer();
        var bundle = Bundle("many", "one");
        for (var file = 0; file < 18; file++)
            await File.WriteAllTextAsync(Path.Combine(bundle, "js", $"part-{file:D2}.js"), $"part {file}", token);
        var manifest = BundleManifest.Compute(bundle);
        Assert.Equal(20, manifest.Entries.Count(entry => entry.Serve == ServeStatus.Served));
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
        var check = await manifest.CheckServedAsync(client, new Uri($"http://127.0.0.1:{server.Port}/"), token);
        Assert.Equal(ServedOutcome.NotServedInTime, check.Outcome);
        Assert.Contains("within the HTTP timeout (1 s)", check.Detail, StringComparison.Ordinal);
        Assert.InRange(server.FileRequests, 0, BundleManifest.Parallelism);
    }

    // BH4-2/BH4-3/VG4-2: at the end of a finished run, a preview address that stops answering files is reported as "preview not
    // responding": the run is Incomplete (PreviewNotResponding) with that detail and header, not an identity change; the run's HTTP
    // timeout is injected (3 s). Once the run is under way (the file's suite-path lookup), the preview process is killed and its port
    // taken by a server that accepts and never answers a file. (Suspending the preview with SIGSTOP instead is not usable: the test
    // host is its parent, and a stopped child stalls the runtime's child-process tracking.)
    [Fact]
    public async Task AFinishedRunWhosePreviewStopsAnsweringIsIncompleteNotAnIdentityChange()
    {
        var ports = new CountingPorts();
        using var watcher = new SilentWatcher();
        RootOnlyServer? unanswering = null;
        try
        {
            var (options, dependencies, output) = Run(Bundle("hung", "one"), ports, watcher, [new ScheduledFile("hang-preview", SuiteTiming.Parallel)],
                label =>
                {
                    var pid = int.Parse(Processes.Run("lsof", ["-nP", $"-iTCP:{ports.Last}", "-sTCP:LISTEN", "-t"]).Output.Split('\n')[0].Trim(),
                        CultureInfo.InvariantCulture);
                    Assert.Equal(0, Processes.Run("kill", ["-9", pid.ToString(CultureInfo.InvariantCulture)]).ExitCode);
                    Assert.True(ProcessesTests.Gone(pid), $"the preview {pid} still runs");
                    unanswering = new RootOnlyServer(ports.Last);
                    return label;
                }, httpTimeout: TimeSpan.FromSeconds(3));
            var summary = await RunEngine.RunAsync(options, dependencies, TestContext.Current.CancellationToken);
            Assert.Equal(Verdict.Incomplete, summary.Verdict);
            Assert.Equal(IncompleteReason.PreviewNotResponding, summary.Incomplete);
            Assert.StartsWith("preview not responding: ", summary.Detail, StringComparison.Ordinal);
            Assert.StartsWith("preview not responding: ", summary.Header["servedCheck.end"], StringComparison.Ordinal);
            Assert.True(File.Exists(Path.Combine(output, "summary.json")));
        }
        finally { unanswering?.Dispose(); }
    }

    // VG5-1: a served check against an unlistening loopback port returns ConnectionError naming "could not be fetched".
    [Fact]
    public async Task AServedCheckOfAnUnlisteningPortIsConnectionError()
    {
        using var closed = new TcpListener(IPAddress.Loopback, 0);
        closed.Start();
        var port = ((IPEndPoint)closed.LocalEndpoint).Port;
        closed.Stop();
        var manifest = BundleManifest.Compute(Bundle("unlistening", "one"));
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        var check = await manifest.CheckServedAsync(client, new Uri($"http://127.0.0.1:{port}/"), TestContext.Current.CancellationToken);
        Assert.Equal(ServedOutcome.ConnectionError, check.Outcome);
        Assert.Contains("could not be fetched", check.Detail, StringComparison.Ordinal);
    }

    // VG5-1: at the end of a finished run, a preview killed with SIGKILL whose port refuses connections is IdentityChanged with
    // servedCheck.end naming "could not be fetched" and summary.json written.
    [Fact]
    public async Task AFinishedRunWhosePreviewStopsListeningIsIdentityChanged()
    {
        var ports = new CountingPorts();
        using var watcher = new SilentWatcher();
        var (options, dependencies, output) = Run(Bundle("stopped-listening", "one"), ports, watcher,
            [new ScheduledFile("kill-preview", SuiteTiming.Parallel)], label =>
            {
                var pid = int.Parse(Processes.Run("lsof", ["-nP", $"-iTCP:{ports.Last}", "-sTCP:LISTEN", "-t"]).Output.Split('\n')[0].Trim(),
                    CultureInfo.InvariantCulture);
                Assert.Equal(0, Processes.Run("kill", ["-9", pid.ToString(CultureInfo.InvariantCulture)]).ExitCode);
                Assert.True(ProcessesTests.Gone(pid), $"the preview {pid} still runs");
                return label;
            });
        var summary = await RunEngine.RunAsync(options, dependencies, TestContext.Current.CancellationToken);
        Assert.Equal(Verdict.IdentityChanged, summary.Verdict);
        Assert.Contains("the end served check differs from the bundle", summary.Detail, StringComparison.Ordinal);
        Assert.Contains("could not be fetched", summary.Header["servedCheck.end"], StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(output, "summary.json")));
    }

    // ECH4-2 (Spec Change Log 3(c)): a caller cancellation during the preview start (no bundle change) still ends as a typed refusal,
    // before any port is asked.
    [Fact]
    public async Task ARunCancelledByItsCallerDuringThePreviewStartIsATypedRefusal()
    {
        var ports = new CountingPorts();
        using var watcher = new SilentWatcher();
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        var (options, dependencies, _) = Run(Bundle("caller-cancelled", "one"), ports, watcher, [], label => label);
        var refusal = await Assert.ThrowsAsync<RunnerException>(() => RunEngine.RunAsync(options, dependencies, cancelled.Token));
        Assert.Equal(RunnerError.PreviewUnavailable, refusal.Error);
        Assert.Contains("cancelled by the run's caller", refusal.Message, StringComparison.Ordinal);
        Assert.Equal(0, ports.Asked);
    }

    // BH3-2/ECH3-4: in a finished run, an end re-hash that cannot read the bundle (a NotServed file made unreadable while the preview
    // starts, so the start's served check still passes) is an identity detail of the written summary, and the end served check is not
    // run. Its precondition (the file really unreadable; a run as root ignores the mode) is asserted before the verdict.
    [Fact]
    public async Task AFinishedRunWhoseEndReHashCannotReadTheBundleIsAnIdentityChange()
    {
        var token = TestContext.Current.CancellationToken;
        var bundle = Bundle("unreadable", "one");
        var hidden = Path.Combine(bundle, ".hidden");
        int? chmodExit = null;
        using var watcher = new SilentWatcher();
        var ports = new CountingPorts(() => chmodExit = Processes.Run("chmod", ["000", hidden]).ExitCode);
        try
        {
            var (options, dependencies, output) = Run(bundle, ports, watcher, [], label => label);
            var summary = await RunEngine.RunAsync(options, dependencies, token);
            Assert.Equal(0, chmodExit);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => File.ReadAllBytesAsync(hidden, token));
            Assert.Equal(Verdict.IdentityChanged, summary.Verdict);
            Assert.Contains("the bundle could not be re-hashed (", summary.Detail, StringComparison.Ordinal);
            Assert.Equal("not run: the bundle could not be re-hashed", summary.Header["servedCheck.end"]);
            Assert.True(File.Exists(Path.Combine(output, "summary.json")));
        }
        finally { Processes.Run("chmod", ["644", hidden]); }
    }

    // VG3-2: a change the watcher missed (a NotServed file rewritten while the preview starts) is caught by a finished run's end re-hash.
    [Fact]
    public async Task AFinishedRunCatchesAChangeTheWatcherMissedAtTheEndReHash()
    {
        var bundle = Bundle("missed", "one");
        using var watcher = new SilentWatcher();
        var ports = new CountingPorts(() => File.WriteAllText(Path.Combine(bundle, ".hidden"), "rewritten"));
        var (options, dependencies, _) = Run(bundle, ports, watcher, [], label => label);
        var summary = await RunEngine.RunAsync(options, dependencies, TestContext.Current.CancellationToken);
        Assert.Equal(Verdict.IdentityChanged, summary.Verdict);
        Assert.Contains("re-hash differs", summary.Detail, StringComparison.Ordinal);
    }

    // VG3: a preview serving another bundle is refused as ServedIdentityMismatch, and that preview is stopped (its log ends with the
    // stop line).
    [Fact]
    public async Task APreviewServingAnotherBundleIsStoppedAndRefused()
    {
        var token = TestContext.Current.CancellationToken;
        var bundle = Bundle("served-mismatch", "one");
        var other = BundleManifest.Compute(Bundle("other-mismatch", "two"));
        var ports = new CountingPorts();
        using var client = new HttpClient();
        var log = PreviewLog("mismatch");
        var refusal = await Assert.ThrowsAsync<RunnerException>(() =>
            PreviewHost.StartAsync(TestRepository.Root, bundle, other, ports, client, log, ChildProcess.Start, token));
        Assert.Equal(RunnerError.ServedIdentityMismatch, refusal.Error);
        var lines = await File.ReadAllLinesAsync(log, token);
        Assert.StartsWith(StopLine(ports.Last), lines[^1], StringComparison.Ordinal);
    }

    // VG4/VG3-2: an unchanged finished run passes and stops its preview before writing its summary: when the summary's end identity
    // check runs (the second artifact check), the preview log already ends with the stop line and no summary file exists yet.
    [Fact]
    public async Task ARunStopsItsPreviewBeforeWritingItsSummary()
    {
        var ports = new CountingPorts();
        using var watcher = new SilentWatcher();
        var output = "";
        var checks = 0;
        bool? stoppedBeforeSummary = null;
        void VerifyArtifact()
        {
            if (++checks != 2) return;
            var lines = File.ReadAllLines(Path.Combine(output, "preview.log"));
            stoppedBeforeSummary = lines[^1].StartsWith(StopLine(ports.Last), StringComparison.Ordinal) && !File.Exists(Path.Combine(output, "summary.json"));
        }
        var run = Run(Bundle("finished", "one"), ports, watcher, [], label => label, VerifyArtifact);
        output = run.Output;
        var summary = await RunEngine.RunAsync(run.Options, run.Dependencies, TestContext.Current.CancellationToken);
        Assert.Equal(Verdict.Pass, summary.Verdict);
        Assert.True(stoppedBeforeSummary);
        Assert.True(File.Exists(Path.Combine(output, "summary.json")));
    }

    // VG4: a run whose file fails before the end leaves through its catch path: the preview is stopped, the file's refusal
    // propagates unchanged and no summary is written.
    [Fact]
    public async Task ARunWhoseFileFailsStopsItsPreviewAndPropagatesTheRefusal()
    {
        var ports = new CountingPorts();
        using var watcher = new SilentWatcher();
        var failure = new RunnerException(RunnerError.UndeclaredSuite, "boom is not declared");
        var (options, dependencies, output) = Run(Bundle("failing", "one"), ports, watcher, [new ScheduledFile("boom", SuiteTiming.Parallel)],
            _ => throw failure);
        var thrown = await Assert.ThrowsAsync<RunnerException>(() => RunEngine.RunAsync(options, dependencies, TestContext.Current.CancellationToken));
        Assert.Same(failure, thrown);
        Assert.False(File.Exists(Path.Combine(output, "summary.json")));
        var log = await File.ReadAllLinesAsync(Path.Combine(output, "preview.log"), TestContext.Current.CancellationToken);
        Assert.StartsWith(StopLine(ports.Last), log[^1], StringComparison.Ordinal);
    }

    [Fact]
    public void ManifestIsDeterministicAndListsDotFilesAsNotServed()
    {
        var first = BundleManifest.Compute(Bundle("a", "one"));
        var second = BundleManifest.Compute(Bundle("b", "one"));
        Assert.Equal(first.Identity, second.Identity);
        Assert.Equal([".hidden", ".well-known/x", "index.html", "js/app.js"], first.Entries.Select(entry => entry.Path));
        Assert.Equal([ServeStatus.NotServed, ServeStatus.NotServed, ServeStatus.Served, ServeStatus.Served], first.Entries.Select(entry => entry.Serve));
        var manifest = string.Concat(first.Entries.Select(entry => $"{entry.Path}\t{entry.Sha256}\n"));
        Assert.Equal(BundleManifest.Hash(Encoding.UTF8.GetBytes(manifest)), first.Identity);
        Assert.NotEqual(first.Identity, BundleManifest.Compute(Bundle("c", "two")).Identity);
    }

    // L9 (real): after Stop the preview's root no longer runs.
    [Fact]
    public async Task PreviewServesTheBundleAndAnotherBundleIsAMismatch()
    {
        var token = TestContext.Current.CancellationToken;
        var bundle = Bundle("served", "one");
        var other = BundleManifest.Compute(Bundle("other", "two"));
        using var client = new HttpClient();
        var preview = await PreviewHost.StartAsync(TestRepository.Root, bundle, BundleManifest.Compute(bundle), new FreePortSource(), client,
            PreviewLog("served"), ChildProcess.Start, token);
        var root = preview.ProcessId;
        try
        {
            Assert.Equal(new ServedCheck(ServedOutcome.Mismatch, "1 file(s) differ from the bundle on disk; first: js/app.js"),
                await other.CheckServedAsync(client, preview.BaseUri, token));
            var refusal = await Assert.ThrowsAsync<RunnerException>(() => other.VerifyServedAsync(client, preview.BaseUri, token));
            Assert.Equal(RunnerError.ServedIdentityMismatch, refusal.Error);
        }
        finally { preview.Stop(); }
        Assert.True(ProcessesTests.Gone(root), $"the preview root {root} still runs after Stop");
    }

    [Fact]
    public async Task PortHeldByAForeignListenerRefusesAfterThreeAttempts()
    {
        var token = TestContext.Current.CancellationToken;
        using var foreign = new TcpListener(IPAddress.Loopback, 0);
        foreign.Start();
        var port = ((IPEndPoint)foreign.LocalEndpoint).Port;
        var bundle = Bundle("held", "one");
        using var client = new HttpClient();
        var refusal = await Assert.ThrowsAsync<RunnerException>(() => PreviewHost.StartAsync(TestRepository.Root, bundle,
            BundleManifest.Compute(bundle), new HeldPort(port), client, PreviewLog("held"), ChildProcess.Start, token));
        Assert.Equal(RunnerError.PreviewUnavailable, refusal.Error);
        Assert.Contains("attempt 3", refusal.Message, StringComparison.Ordinal);
    }

    // A lost port is retried on a new port; each attempt appends to the one real preview log through its own sink, so the failed
    // attempt's stop line is kept and the started preview's stop line is the log's last line.
    [Fact]
    public async Task LostPortIsRetriedOnANewPort()
    {
        var token = TestContext.Current.CancellationToken;
        using var foreign = new TcpListener(IPAddress.Loopback, 0);
        foreign.Start();
        var held = ((IPEndPoint)foreign.LocalEndpoint).Port;
        var bundle = Bundle("retry", "one");
        using var client = new HttpClient();
        var log = PreviewLog("retry");
        var preview = await PreviewHost.StartAsync(TestRepository.Root, bundle, BundleManifest.Compute(bundle), new Ports(held), client,
            log, ChildProcess.Start, token);
        try { Assert.NotEqual(held, preview.Port); }
        finally { preview.Stop(); }
        var lines = await File.ReadAllLinesAsync(log, token);
        Assert.Contains(lines, line => line.StartsWith($"preview[{held}] E2E_RUNNER stop (stop requested): ", StringComparison.Ordinal));
        Assert.StartsWith($"preview[{preview.Port}] E2E_RUNNER stop (stop requested): ", lines[^1], StringComparison.Ordinal);
    }

    // L2: a preview whose program cannot start (its working directory is missing) is ProcessNotStarted naming dotnet after the first
    // attempt; no second port is asked for. Control: the preview facts above start.
    [Fact]
    public async Task APreviewWhoseProgramCannotStartIsRefusedWithoutRetry()
    {
        var token = TestContext.Current.CancellationToken;
        var bundle = Bundle("unstartable", "one");
        var ports = new CountingPorts();
        using var client = new HttpClient();
        var missingRoot = Path.Combine(_work.FullName, "missing-root");
        var refusal = await Assert.ThrowsAsync<RunnerException>(() => PreviewHost.StartAsync(missingRoot, bundle, BundleManifest.Compute(bundle), ports,
            client, PreviewLog("unstartable"), ChildProcess.Start, token));
        Assert.Equal(RunnerError.ProcessNotStarted, refusal.Error);
        Assert.StartsWith("ProcessNotStarted: dotnet could not start (", refusal.Message, StringComparison.Ordinal);
        Assert.Equal(1, ports.Asked);
    }

    private async Task<RunSummary> RunWaitingAsync(string bundle, Func<string, Task> duringRun, IBundleWatcher? watcher = null,
        Func<string>? sources = null)
    {
        var token = TestContext.Current.CancellationToken;
        var output = Path.Combine(_work.FullName, "out-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        var dependencies = new RunDependencies
        {
            VerifyArtifact = () => { }, // These fixtures inject bundle/source boundaries independently of CI provenance.
            SuiteSources = sources ?? (() => "fixed"),
            Watch = directory => watcher ?? new FileSystemBundleWatcher(directory),
            Ports = new FreePortSource(),
            TsxLoader = Toolchain.ResolveTsxLoader(Home),
            ParentEnvironment = Environment.GetEnvironmentVariables(),
            WallLimit = TimeSpan.FromMinutes(2),
            SuitePath = label => $"tools/E2eRunner.Tests/Fixtures/{label}.test.ts",
        };
        var options = new RunOptions(TestRepository.Root, output, bundle, "identity-test", [new ScheduledFile("wait", SuiteTiming.Parallel)], 1,
            null, [], new Dictionary<string, string>());
        var run = RunEngine.RunAsync(options, dependencies, token);
        while (!File.Exists(Path.Combine(output, "wait.tap")) && !run.IsCompleted) await Task.Delay(100, token);
        await Task.Delay(1500, token);
        await duringRun(output);
        return await run;
    }

    [Fact]
    public async Task UnchangedRunPasses()
    {
        var summary = await RunWaitingAsync(Bundle("steady", "one"), output => File.WriteAllTextAsync(Path.Combine(output, "release"), ""));
        Assert.Equal(Verdict.Pass, summary.Verdict);
        Assert.Equal(summary.Header["bundleIdentity.start"], summary.Header["bundleIdentity.end"]);
    }

    [Fact]
    public async Task BundleChangedMidRunAbortsWithIdentityChanged()
    {
        var bundle = Bundle("changed", "one");
        var summary = await RunWaitingAsync(bundle, _ => File.WriteAllTextAsync(Path.Combine(bundle, "js", "app.js"), "two"));
        Assert.Equal(Verdict.IdentityChanged, summary.Verdict);
        Assert.Contains("bundle watcher", summary.Detail, StringComparison.Ordinal);
        Assert.NotEqual(FileOutcome.Pass, Assert.Single(summary.Files).Outcome);
    }

    [Fact]
    public async Task BundleChangedAndRestoredIsStillIdentityChanged()
    {
        var bundle = Bundle("restored", "one");
        var summary = await RunWaitingAsync(bundle, async output =>
        {
            await File.WriteAllTextAsync(Path.Combine(bundle, "js", "app.js"), "two");
            await File.WriteAllTextAsync(Path.Combine(bundle, "js", "app.js"), "one");
            await File.WriteAllTextAsync(Path.Combine(output, "release"), "");
        });
        Assert.Equal(Verdict.IdentityChanged, summary.Verdict);
        Assert.Equal(summary.Header["bundleIdentity.start"], summary.Header["bundleIdentity.end"]);
    }

    [Fact]
    public async Task EndRehashCatchesAChangeTheWatcherMissed()
    {
        var bundle = Bundle("silenced", "one");
        using var silent = new SilentWatcher();
        var summary = await RunWaitingAsync(bundle, async output =>
        {
            await File.WriteAllTextAsync(Path.Combine(bundle, "js", "app.js"), "two");
            await File.WriteAllTextAsync(Path.Combine(output, "release"), "");
        }, silent);
        Assert.Equal(Verdict.IdentityChanged, summary.Verdict);
        Assert.Contains("re-hash", summary.Detail, StringComparison.Ordinal);
    }

    // The tracked npm manifests and generated modules are suite inputs. Ignored non-module files are irrelevant.
    [Fact]
    public void TheSuiteDigestCoversTheGeneratedWireModulesAndNoOtherIgnoredFile()
    {
        var root = _work.FullName;
        Processes.Git(root, "init", "-q");
        File.WriteAllText(Path.Combine(root, ".gitignore"), "tools/e2e/.wire/\n*.tmp\n");
        Directory.CreateDirectory(Path.Combine(root, "tools", "e2e", ".wire", "curious-contraptions", "wire"));
        Directory.CreateDirectory(Path.Combine(root, "tools", "runtime"));
        File.WriteAllText(Path.Combine(root, "tools", "e2e", "a.test.ts"), "import { A } from './.wire/curious-contraptions/wire.ts';\n");
        var wireFrame = Path.Combine(root, "tools", "runtime", "wire-frame.ts");
        File.WriteAllText(wireFrame, "export function openFrame() {}\n");
        File.WriteAllText(Path.Combine(root, "tools", "e2e", ".wire", "curious-contraptions", "wire.ts"), "export { A } from './wire/a.js';\n");
        var generated = Path.Combine(root, "tools", "e2e", ".wire", "curious-contraptions", "wire", "a.ts");
        File.WriteAllText(generated, "export class A {}\n");
        var unrelated = Path.Combine(root, "tools", "e2e", "notes.tmp");
        File.WriteAllText(unrelated, "one");
        var retired = Path.Combine(root, "tools", "e2e", ".wire", "schemas.txt");
        File.WriteAllText(retired, "obsolete");
        var manifest = Path.Combine(root, "package.json");
        var packageLock = Path.Combine(root, "package-lock.json");
        File.WriteAllText(manifest, "{}");
        File.WriteAllText(packageLock, "{}");
        Processes.Git(root, "add", "--", "package.json", "package-lock.json");
        string[] admitted = ["tools/e2e/.wire/curious-contraptions/wire.ts", "tools/e2e/.wire/curious-contraptions/wire/a.ts"];
        var first = SourceIdentity.SuiteSources(root, admitted);

        File.WriteAllText(unrelated, "two");
        File.WriteAllText(retired, "other obsolete contents");
        var unlisted = Path.Combine(Path.GetDirectoryName(generated)!, "unlisted.ts");
        File.WriteAllText(unlisted, "export class Unlisted {}\n");
        Processes.Git(root, "add", "-f", "--", unlisted);
        Assert.Equal(first, SourceIdentity.SuiteSources(root, admitted));

        File.WriteAllText(manifest, """{"private":true}""");
        var manifestChanged = SourceIdentity.SuiteSources(root, admitted);
        Assert.NotEqual(first, manifestChanged);
        File.WriteAllText(packageLock, """{"lockfileVersion":3}""");
        var lockChanged = SourceIdentity.SuiteSources(root, admitted);
        Assert.NotEqual(manifestChanged, lockChanged);

        File.WriteAllText(generated, "export class A { b = 1; }\n");
        var regenerated = SourceIdentity.SuiteSources(root, admitted);
        Assert.NotEqual(lockChanged, regenerated);

        File.WriteAllText(wireFrame, "export function openFrame() { return 1; }\n");
        var frameChanged = SourceIdentity.SuiteSources(root, admitted);
        Assert.NotEqual(regenerated, frameChanged);

        Directory.Delete(Path.Combine(root, "tools", "e2e", ".wire"), recursive: true);
        var removed = SourceIdentity.SuiteSources(root, admitted);
        Assert.NotEqual(frameChanged, removed);
        Assert.NotEqual(first, removed);
    }

    [Theory]
    [InlineData("package.json")]
    [InlineData("package-lock.json")]
    public void RetiredNestedNpmManifestChangesSuiteIdentityByPresence(string filename)
    {
        var root = _work.FullName;
        Processes.Git(root, "init", "-q");
        File.WriteAllText(Path.Combine(root, ".gitignore"), $"tools/e2e/{filename}\n");
        var path = Path.Combine(Directory.CreateDirectory(Path.Combine(root, "tools", "e2e")).FullName, filename);
        var absent = SourceIdentity.SuiteSources(root, []);

        File.WriteAllText(path, "first ignored retired content");
        var present = SourceIdentity.SuiteSources(root, []);
        Assert.NotEqual(absent, present);

        File.WriteAllText(path, "different ignored retired content");
        Assert.Equal(present, SourceIdentity.SuiteSources(root, []));

        File.Delete(path);
        Assert.Equal(absent, SourceIdentity.SuiteSources(root, []));
    }

    [Theory]
    [InlineData("package.json")]
    [InlineData("package-lock.json")]
    public void GitListedRetiredNpmInputsContributePresenceOnly(string name)
    {
        var root = _work.FullName;
        Processes.Git(root, "init", "-q");
        var reserved = Path.Combine(Directory.CreateDirectory(Path.Combine(root, "tools", "e2e")).FullName, name);
        var absent = SourceIdentity.SuiteSources(root, []);
        File.WriteAllText(reserved, "malformed retired npm input");
        Processes.Git(root, "add", "--", reserved);

        var present = SourceIdentity.SuiteSources(root, []);
        Assert.NotEqual(absent, present);
        File.WriteAllText(reserved, "changed retired contents");
        Assert.Equal(present, SourceIdentity.SuiteSources(root, []));

        File.Delete(reserved);
        Assert.Equal(absent, SourceIdentity.SuiteSources(root, []));
    }

    [Theory]
    [InlineData("package.json", "package.json")]
    [InlineData("package-lock.json", "package-lock.json")]
    [InlineData("package.json", "PACKAGE.JSON")]
    [InlineData("package.json", "pac\u212Aage.json")]
    public void GitListedChildrenLeaveARetiredDirectoryIdentityUnchanged(string name, string spelling)
    {
        var root = _work.FullName;
        Processes.Git(root, "init", "-q");
        var parent = Directory.CreateDirectory(Path.Combine(root, "tools", "e2e")).FullName;
        var absent = SourceIdentity.SuiteSources(root, []);
        var directory = Directory.CreateDirectory(Path.Combine(parent, spelling));
        var retired = Directory.Exists(Path.Combine(parent, name));
        var empty = SourceIdentity.SuiteSources(root, []);
        Assert.Equal(retired, absent != empty);

        var tracked = Path.Combine(directory.FullName, "unread.txt");
        File.WriteAllText(tracked, "first child contents");
        TrackManifestSpelling(root, tracked);
        var withChild = SourceIdentity.SuiteSources(root, []);
        Assert.Equal(retired, empty == withChild);
        File.WriteAllText(tracked, "changed child contents");
        Assert.Equal(retired, withChild == SourceIdentity.SuiteSources(root, []));

        File.Delete(tracked);
        if (retired)
            Assert.Equal(empty, SourceIdentity.SuiteSources(root, []));
        Processes.Git(root, "rm", "--cached", "-f", "--", tracked);
        Assert.True(Directory.Exists(directory.FullName));
        Assert.Equal(empty, SourceIdentity.SuiteSources(root, []));

        directory.Delete();
        Assert.Equal(absent, SourceIdentity.SuiteSources(root, []));
    }

    [Theory]
    [InlineData("package.json", "PACKAGE.JSON")]
    [InlineData("package-lock.json", "PACKAGE-LOCK.JSON")]
    [InlineData("package.json", "pac\u212Aage.json")]
    public void GitListedManifestCaseVariantsFollowFilesystemIdentity(string name, string variant)
    {
        var root = _work.FullName;
        Processes.Git(root, "init", "-q");
        var directory = Directory.CreateDirectory(Path.Combine(root, "tools", "e2e")).FullName;
        var canonical = Path.Combine(directory, name);
        var alternate = Path.Combine(directory, variant);
        var absent = SourceIdentity.SuiteSources(root, []);
        File.WriteAllText(alternate, "first case variant contents");
        TrackManifestSpelling(root, alternate);

        var aliasesCanonical = Path.Exists(canonical);
        if (!aliasesCanonical)
            File.WriteAllText(canonical, "distinct retired manifest");
        var before = SourceIdentity.SuiteSources(root, []);
        File.WriteAllText(alternate, "changed case variant contents");
        var after = SourceIdentity.SuiteSources(root, []);

        if (aliasesCanonical)
        {
            Assert.NotEqual(absent, before);
            Assert.Equal(before, after);
            File.Delete(alternate);
            Assert.Equal(absent, SourceIdentity.SuiteSources(root, []));
        }
        else
        {
            Assert.NotEqual(before, after);
        }
    }

    private static void TrackManifestSpelling(string root, string path)
    {
        Processes.Git(root, "config", "core.precomposeunicode", "false");
        Processes.Git(root, "add", "--", path);
        var listed = Processes.Git(root, "ls-files", "-z", "--cached").Split('\0');
        Assert.Contains(Path.GetRelativePath(root, path).Replace('\\', '/'), listed);
    }

    [Fact]
    public void AdjacentManifestBackupParticipatesInSuiteIdentity()
    {
        var root = _work.FullName;
        Processes.Git(root, "init", "-q");
        var directory = Directory.CreateDirectory(Path.Combine(root, "tools", "e2e")).FullName;
        var backup = Path.Combine(directory, "package.json.backup");
        File.WriteAllText(backup, "first backup contents");
        Processes.Git(root, "add", "--", backup);
        var before = SourceIdentity.SuiteSources(root, []);

        File.WriteAllText(backup, "changed backup contents");

        Assert.NotEqual(before, SourceIdentity.SuiteSources(root, []));
    }

    [Fact]
    public async Task RetiredManifestAppearingMidRunChangesIdentity()
    {
        var root = _work.FullName;
        Processes.Git(root, "init", "-q");
        var e2e = Directory.CreateDirectory(Path.Combine(root, "tools", "e2e")).FullName;
        var summary = await RunWaitingAsync(Bundle("retired", "one"), async output =>
        {
            await File.WriteAllTextAsync(Path.Combine(e2e, "package.json"), "retired manifest appeared");
            await File.WriteAllTextAsync(Path.Combine(output, "release"), "");
        }, sources: () => SourceIdentity.SuiteSources(root, []));
        Assert.Equal(Verdict.IdentityChanged, summary.Verdict);
        Assert.Contains("suite sources changed", summary.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SuiteSourceChangedMidRunIsIdentityChanged()
    {
        var source = Path.Combine(_work.FullName, "suite-source.ts");
        await File.WriteAllTextAsync(source, "one", TestContext.Current.CancellationToken);
        var summary = await RunWaitingAsync(Bundle("sources", "one"), async output =>
        {
            await File.WriteAllTextAsync(source, "two");
            await File.WriteAllTextAsync(Path.Combine(output, "release"), "");
        }, sources: () => BundleManifest.Hash(File.ReadAllBytes(source)));
        Assert.Equal(Verdict.IdentityChanged, summary.Verdict);
        Assert.Contains("suite sources changed", summary.Detail, StringComparison.Ordinal);
    }
}
