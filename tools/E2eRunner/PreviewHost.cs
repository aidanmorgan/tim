using System.Diagnostics;
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

/// <summary>The runner's own preview (spec D10): `dotnet run --project tools/Preview -- &lt;bundle&gt; &lt;port&gt;` on a free port, retried on
/// a new port up to three times when the port is lost to a race, and proven to be this process by the full served-identity check.
/// Disposing kills its process tree.</summary>
internal sealed class PreviewHost : IDisposable
{
    public const int Attempts = 3;
    private readonly Process _process;

    private PreviewHost(Process process, Uri baseUri)
    {
        _process = process;
        BaseUri = baseUri;
    }

    public Uri BaseUri { get; }
    public int Port => BaseUri.Port;

    public static async Task<PreviewHost> StartAsync(string root, string bundleDirectory, BundleManifest manifest, IPortSource ports,
        HttpClient client, TextWriter log, CancellationToken cancellation)
    {
        ArgumentNullException.ThrowIfNull(ports);
        ArgumentNullException.ThrowIfNull(manifest);
        var failures = new List<string>();
        for (var attempt = 1; attempt <= Attempts; attempt++)
        {
            var port = ports.Next();
            var host = await TryStartAsync(root, bundleDirectory, port, client, log, cancellation).ConfigureAwait(false);
            if (host is null)
            {
                failures.Add($"attempt {attempt} on port {port} did not start");
                continue;
            }
            try
            {
                await manifest.VerifyServedAsync(client, host.BaseUri, cancellation).ConfigureAwait(false);
                var verified = host;
                host = null;
                return verified;
            }
            finally { host?.Dispose(); }
        }
        throw new RunnerException(RunnerError.PreviewUnavailable, "The preview did not start: " + string.Join("; ", failures));
    }

    private static async Task<PreviewHost?> TryStartAsync(string root, string bundleDirectory, int port, HttpClient client, TextWriter log,
        CancellationToken cancellation)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = root,
        };
        // Kestrel's per-request information logs would make preview.log grow with every file fetched; warnings and errors remain.
        start.Environment["Logging__LogLevel__Default"] = "Warning";
        foreach (var argument in new[] { "run", "--project", Path.Combine(root, "tools", "Preview"), "--", bundleDirectory,
            port.ToString(System.Globalization.CultureInfo.InvariantCulture) }) start.ArgumentList.Add(argument);
        Process? process = new();
        try
        {
            process.StartInfo = start;
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            process.OutputDataReceived += (_, line) =>
            {
                if (line.Data is null) return;
                lock (log) log.WriteLine($"preview[{port}] {line.Data}");
                if (line.Data.StartsWith("Workshop preview:", StringComparison.Ordinal)) ready.TrySetResult();
            };
            process.ErrorDataReceived += (_, line) => { if (line.Data is not null) lock (log) log.WriteLine($"preview[{port}] {line.Data}"); };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            var baseUri = new Uri($"http://127.0.0.1:{port}/");
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            timeout.CancelAfter(TimeSpan.FromMinutes(2));
            var exited = process.WaitForExitAsync(timeout.Token);
            var answering = WaitUntilAnsweringAsync(client, baseUri, ready.Task, timeout.Token);
            var first = await Task.WhenAny(exited, answering).ConfigureAwait(false);
            var started = first == answering && await answering.ConfigureAwait(false) && !process.HasExited;
            await timeout.CancelAsync().ConfigureAwait(false);
            if (!started) return null;
            var host = new PreviewHost(process, baseUri);
            process = null;
            return host;
        }
        finally
        {
            if (process is not null) Stop(process);
            process?.Dispose();
        }
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

    private static void Stop(Process process)
    {
        try
        {
            if (process.HasExited) return;
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
        }
        catch (InvalidOperationException) { return; } // never started
    }

    public void Dispose()
    {
        Stop(_process);
        _process.Dispose();
    }
}
