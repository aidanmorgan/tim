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

    private sealed class SilentWatcher : IBundleWatcher
    {
        public string? FirstChange => null;
        public event Action<string>? Changed { add { } remove { } }
        public void Dispose() { }
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

    [Fact]
    public async Task PreviewServesTheBundleAndAnotherBundleIsAMismatch()
    {
        var token = TestContext.Current.CancellationToken;
        var bundle = Bundle("served", "one");
        var other = BundleManifest.Compute(Bundle("other", "two"));
        using var client = new HttpClient();
        using var preview = await PreviewHost.StartAsync(TestRepository.Root, bundle, BundleManifest.Compute(bundle), new FreePortSource(), client,
            TextWriter.Null, token);
        Assert.Equal(["js/app.js"], await other.ServedMismatchesAsync(client, preview.BaseUri, token));
        var refusal = await Assert.ThrowsAsync<RunnerException>(() => other.VerifyServedAsync(client, preview.BaseUri, token));
        Assert.Equal(RunnerError.ServedIdentityMismatch, refusal.Error);
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
            BundleManifest.Compute(bundle), new HeldPort(port), client, TextWriter.Null, token));
        Assert.Equal(RunnerError.PreviewUnavailable, refusal.Error);
        Assert.Contains("attempt 3", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LostPortIsRetriedOnANewPort()
    {
        var token = TestContext.Current.CancellationToken;
        using var foreign = new TcpListener(IPAddress.Loopback, 0);
        foreign.Start();
        var held = ((IPEndPoint)foreign.LocalEndpoint).Port;
        var bundle = Bundle("retry", "one");
        using var client = new HttpClient();
        using var preview = await PreviewHost.StartAsync(TestRepository.Root, bundle, BundleManifest.Compute(bundle), new Ports(held), client,
            TextWriter.Null, token);
        Assert.NotEqual(held, preview.Port);
    }

    private async Task<RunSummary> RunWaitingAsync(string bundle, Func<string, Task> duringRun, IBundleWatcher? watcher = null,
        Func<string>? sources = null)
    {
        var token = TestContext.Current.CancellationToken;
        var output = Path.Combine(_work.FullName, "out-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        var dependencies = new RunDependencies
        {
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
