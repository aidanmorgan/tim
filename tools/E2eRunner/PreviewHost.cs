using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace CuriousContraptions.E2eRunner;

/// <summary>Where the preview's candidate ports come from (a free loopback port by default; tests inject held ports).</summary>
internal interface IPortSource
{
    int Next();
}

internal sealed class FreePortSource : IPortSource
{
    public int Next()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}

/// <summary>The runner's matching prebuilt Preview output on a free port, retried on a new port up to three times when the port is
/// lost to a race, and proven to be this process by the full served-identity check. Each attempt appends to the preview log through
/// its own sink; <see cref="Stop"/> stops its process tree through the process lifecycle and closes that sink.</summary>
internal sealed class PreviewHost
{
    public const int Attempts = 3;
    private static readonly TimeSpan StartLimit = TimeSpan.FromMinutes(2);
    private readonly IChildProcess _child;
    private readonly OutputSink _sink;
    private readonly TimeSpan _grace;
    private bool _stopped;

    internal PreviewHost(IChildProcess child, OutputSink sink, Uri baseUri, TimeSpan grace)
    {
        _child = child;
        _sink = sink;
        BaseUri = baseUri;
        _grace = grace;
    }

    public Uri BaseUri { get; }
    public int Port => BaseUri.Port;

    /// <summary>The preview process's pid.</summary>
    public int ProcessId => _child.Id;

    /// <summary>Starts the preview; a program that cannot start is refused as ProcessNotStarted at once, without another attempt, and
    /// a cancellation stops the attempt in progress and makes no further one (OperationCanceledException); a cancellation already
    /// requested makes none.</summary>
    public static async Task<PreviewHost> StartAsync(string root, string bundleDirectory, BundleManifest manifest, IPortSource ports,
        HttpClient client, string logPath, LaunchChild launcher, CancellationToken cancellation)
    {
        ArgumentNullException.ThrowIfNull(ports);
        ArgumentNullException.ThrowIfNull(manifest);
        var failures = new List<string>();
        for (var attempt = 1; attempt <= Attempts; attempt++)
        {
            cancellation.ThrowIfCancellationRequested();
            var port = ports.Next();
            var host = await TryStartAsync(root, bundleDirectory, port, client, logPath, launcher, cancellation).ConfigureAwait(false);
            if (host is null)
            {
                cancellation.ThrowIfCancellationRequested();
                failures.Add($"attempt {attempt} on port {port} did not start");
                continue;
            }
            try { await manifest.VerifyServedAsync(client, host.BaseUri, cancellation).ConfigureAwait(false); }
            catch (Exception failure)
            {
                host.StopAfterFailure(failure);
                throw;
            }
            return host;
        }
        throw new RunnerException(RunnerError.PreviewUnavailable, "The preview did not start: " + string.Join("; ", failures));
    }

    /// <summary>One attempt: the started preview, or null after stopping an attempt that did not answer.</summary>
    private static async Task<PreviewHost?> TryStartAsync(string root, string bundleDirectory, int port, HttpClient client, string logPath,
        LaunchChild launcher, CancellationToken cancellation)
    {
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = root };
        // Kestrel's per-request information logs would make preview.log grow with every file fetched; warnings and errors remain.
        start.Environment["Logging__LogLevel__Default"] = "Warning";
        var executable = RequireExecutable(AppContext.BaseDirectory);
        foreach (var argument in new[] { executable, bundleDirectory, port.ToString(CultureInfo.InvariantCulture) }) start.ArgumentList.Add(argument);
        var sink = new OutputSink(logPath, append: true, $"preview[{port}] ");
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        sink.LineWritten += (stream, line) =>
        {
            if (stream == OutputStream.Standard && line.StartsWith("Workshop preview:", StringComparison.Ordinal)) ready.TrySetResult();
        };
        var host = new PreviewHost(ProcessLifecycle.Start(launcher, start, sink), sink, new Uri($"http://127.0.0.1:{port}/"), ProcessLifecycle.Grace);
        try
        {
            if (await host.AnswersAsync(client, ready.Task, cancellation).ConfigureAwait(false)) return host;
            host.Stop();
            return null;
        }
        catch (Exception failure)
        {
            host.StopAfterFailure(failure);
            throw;
        }
    }

    /// <summary>True when the preview announced itself and answers while its root still runs, within the start limit.</summary>
    private async Task<bool> AnswersAsync(HttpClient client, Task ready, CancellationToken cancellation)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        timeout.CancelAfter(StartLimit);
        var exited = _child.WaitForRootAsync(Timeout.InfiniteTimeSpan, timeout.Token);
        var answering = WaitUntilAnsweringAsync(client, BaseUri, ready, timeout.Token);
        var first = await Task.WhenAny(exited, answering).ConfigureAwait(false);
        var started = first == answering && await answering.ConfigureAwait(false) && !exited.IsCompleted;
        await timeout.CancelAsync().ConfigureAwait(false);
        return started;
    }

    private static async Task<bool> WaitUntilAnsweringAsync(HttpClient client, Uri baseUri, Task ready, CancellationToken cancellation)
    {
        try
        {
            await ready.WaitAsync(cancellation).ConfigureAwait(false);
            while (true)
            {
                try
                {
                    using var response = await client.GetAsync(baseUri, cancellation).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode) return true;
                }
                catch (HttpRequestException) { await Task.Delay(200, cancellation).ConfigureAwait(false); continue; } // not listening yet
                await Task.Delay(200, cancellation).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { return false; }
    }

    internal static string RequireExecutable(string outputDirectory)
    {
        var executable = Path.Combine(outputDirectory, "Preview.dll");
        foreach (var required in new[] { executable, Path.ChangeExtension(executable, ".runtimeconfig.json"), Path.ChangeExtension(executable, ".deps.json") })
            if (!File.Exists(required)) throw new RunnerException(RunnerError.PreviewUnavailable, $"Missing matching Preview output: {required}. Build E2eRunner first.");
        return executable;
    }

    /// <summary>Stops the preview once (a second call does nothing): one kill of its tree, one grace wait for its root, its output
    /// drained for the grace when the root ended, and its log closed with the runner's lines. A root still running is a typed
    /// ProcessNotStopped refusal; output that could not be read or recorded is ProcessOutputFailed.</summary>
    public void Stop()
    {
        if (StopOnce() is { } refusal) throw refusal;
    }

    /// <summary>Stops the preview while another failure is in flight. Only a ProcessNotStopped of the stop replaces that failure (a
    /// process the run owns may still run), as one refusal naming both when the failure in flight is a ProcessNotStopped too; any
    /// other outcome of the stop gives way to the failure in flight.</summary>
    public void StopAfterFailure(Exception inFlight)
    {
        ArgumentNullException.ThrowIfNull(inFlight);
        if (StopOnce() is not { Error: RunnerError.ProcessNotStopped } notStopped) return;
        throw inFlight is RunnerException { Error: RunnerError.ProcessNotStopped }
            ? ProcessLifecycle.BothNotStopped(inFlight, notStopped)
            : notStopped;
    }

    /// <summary>The outcome of the one stop: null when the preview stopped cleanly or was already stopped.</summary>
    private RunnerException? StopOnce()
    {
        if (_stopped) return null;
        _stopped = true;
        try { return KillAndCloseAsync().GetAwaiter().GetResult(); }
        finally { _child.Dispose(); }
    }

    private async Task<RunnerException?> KillAndCloseAsync()
    {
        var stop = await ProcessLifecycle.StopAsync(_child, _grace).ConfigureAwait(false);
        var outputFailure = await ProcessLifecycle.FinishAsync(_child, _sink, $"the preview on port {Port}", stop.Root, TimeSpan.Zero, _grace,
            [$"E2E_RUNNER stop (stop requested): {stop.Detail}"]).ConfigureAwait(false);
        return stop.Root == RootState.StillRunning
            ? new RunnerException(RunnerError.ProcessNotStopped,
                $"The preview on port {Port} was still running {ProcessLifecycle.Seconds(_grace)} s after the kill ({stop.Detail}).")
            : outputFailure;
    }
}
