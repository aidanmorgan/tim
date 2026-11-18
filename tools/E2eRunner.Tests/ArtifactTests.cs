using System.Text.Json;
using Xunit;

namespace CuriousContraptions.E2eRunner.Tests;

public sealed class ArtifactTests : IDisposable
{
    public enum IgnoreSource { RepositoryLocal, Global }
    public enum ReceiptFault { Missing, Malformed, UnknownCommit, Null }
    private readonly string _root = Path.Combine(TestRepository.Root, ".anvil", "artifact-tests-" + Guid.NewGuid().ToString("N"));
    private readonly string _artifact = Path.Combine(TestRepository.Root, ".anvil", "published-tests-" + Guid.NewGuid().ToString("N"));
    private readonly string _revision;

    public ArtifactTests()
    {
        Directory.CreateDirectory(_root);
        Processes.Git(_root, "init", "-q");
        Processes.Git(_root, "config", "user.email", "fixture@example.invalid");
        Processes.Git(_root, "config", "user.name", "Fixture");
        Write("app.cs", "source");
        Write("Production/Production.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("Production/obj/tracked.txt", "tracked");
        Write(".gitignore", ".anvil/\n");
        Processes.Git(_root, "add", ".");
        Processes.Git(_root, "commit", "-qm", "source");
        _revision = Processes.Git(_root, "rev-parse", "HEAD").Trim();
        foreach (var name in new[] { "index.html", "404.html", "godot.wasm", "godot.pck", "_framework/dotnet.js", ".nojekyll" })
        {
            var full = Path.Combine(_artifact, "AppBundle", name);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, name);
        }
        PublishedArtifact.Record(_root, _artifact, _revision, "123", "1");
    }

    private void Write(string path, string text)
    {
        var full = Path.Combine(_root, path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, text);
    }

    [Fact]
    public void RepeatedAdmissionReusesTheCiArtifactAndAllowsHarnessChanges()
    {
        Write("tools/e2e/test.ts", "harness");
        Write("docs/testing.md", "documentation");
        Processes.Git(_root, "add", ".");
        Processes.Git(_root, "commit", "-qm", "harness");
        for (var run = 0; run < 2; run++)
        {
            var admitted = PublishedArtifact.Admit(_root, _artifact);
            Assert.Equal(_revision, admitted.Receipt.SourceRevision);
            Assert.Equal("123", admitted.Receipt.CiRun);
            Assert.Equal("1", admitted.Receipt.CiAttempt);
            Assert.Equal(Path.Combine(_artifact, "AppBundle"), admitted.BundleDirectory);
        }
    }

    [Theory]
    [InlineData("app.cs")]
    [InlineData("CuriousContraptions.Simulation/Worker.cs")]
    [InlineData("Directory.Build.targets")]
    [InlineData("_bmad/.gdignore")]
    [InlineData("new-production.cs")]
    [InlineData("tools/runtime/new.ts")]
    public void ChangedOrNewProductionInputsRefuseBeforeLaunch(string path)
    {
        Write(path, "changed");
        Assert.Equal(RunnerError.InvalidArtifact, Assert.Throws<RunnerException>(() => PublishedArtifact.Admit(_root, _artifact)).Error);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IndexAndCommittedProductionChangesRefuseEvenWhenWorktreeIsRestored(bool commit)
    {
        Write("app.cs", "changed");
        Processes.Git(_root, "add", ".");
        if (commit) Processes.Git(_root, "commit", "-qm", "production");
        Write("app.cs", "source");
        Assert.Equal(RunnerError.InvalidArtifact, Assert.Throws<RunnerException>(() => PublishedArtifact.Admit(_root, _artifact)).Error);
    }

    [Fact]
    public void ProductionEditAfterAdmissionCannotPassEndVerification()
    {
        var admitted = PublishedArtifact.Admit(_root, _artifact);
        Write("new-worker.cs", "changed during run");
        Assert.Equal(RunnerError.InvalidArtifact, Assert.Throws<RunnerException>(() => admitted.Verify(_root)).Error);
    }

    [Fact]
    public async Task RunEngineReportsProductionEditAtItsEndBoundary()
    {
        var artifact = PublishedArtifact.Admit(_root, _artifact);
        var checks = 0;
        var output = Path.Combine(_artifact, "run");
        Directory.CreateDirectory(output);
        var options = new RunOptions(_root, output, artifact.BundleDirectory, "source-boundary", [], 1, null, [],
            new Dictionary<string, string>());
        var dependencies = new RunDependencies
        {
            VerifyArtifact = () =>
            {
                if (++checks == 2) Write("new-worker.cs", "changed during run");
                artifact.Verify(_root);
            },
            SuiteSources = () => "fixed",
            Watch = directory => new FileSystemBundleWatcher(directory),
            Ports = new FreePortSource(),
            TsxLoader = "",
            ParentEnvironment = Environment.GetEnvironmentVariables(),
            WallLimit = TimeSpan.FromSeconds(30),
            SuitePath = label => label,
        };
        var summary = await RunEngine.RunAsync(options, dependencies, TestContext.Current.CancellationToken);
        Assert.Equal(Verdict.IdentityChanged, summary.Verdict);
        Assert.Contains("new-worker.cs", summary.Detail, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(ReceiptFault.Missing)]
    [InlineData(ReceiptFault.Malformed)]
    [InlineData(ReceiptFault.UnknownCommit)]
    [InlineData(ReceiptFault.Null)]
    public void InvalidReceiptRefuses(ReceiptFault kind)
    {
        var path = Path.Combine(_artifact, PublishedArtifact.ReceiptName);
        if (kind == ReceiptFault.Missing) File.Delete(path);
        else if (kind == ReceiptFault.UnknownCommit)
            File.WriteAllText(path, JsonSerializer.Serialize(new ArtifactReceipt(new string('a', 40), "123", "1", new string('b', 64))));
        else File.WriteAllText(path, kind == ReceiptFault.Null ? "null" : "{}");
        Assert.Equal(RunnerError.InvalidArtifact, Assert.Throws<RunnerException>(() => PublishedArtifact.Admit(_root, _artifact)).Error);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MissingOrChangedBundleRefuses(bool missing)
    {
        var path = Path.Combine(_artifact, "AppBundle", "godot.wasm");
        if (missing) File.Delete(path); else File.WriteAllText(path, "changed");
        Assert.Equal(RunnerError.InvalidArtifact, Assert.Throws<RunnerException>(() => PublishedArtifact.Admit(_root, _artifact)).Error);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SourceAndBundleSymbolicLinksRefuse(bool source)
    {
        var link = source ? Path.Combine(_root, "tools/e2e/link.ts") : Path.Combine(_artifact, "AppBundle", "linked");
        Directory.CreateDirectory(Path.GetDirectoryName(link)!);
        File.CreateSymbolicLink(link, Path.Combine(_root, "app.cs"));
        Assert.Equal(RunnerError.InvalidArtifact, Assert.Throws<RunnerException>(() => PublishedArtifact.Admit(_root, _artifact)).Error);
    }


    [Theory]
    [InlineData(IgnoreSource.RepositoryLocal, "engine/gpu/hidden.cs")]
    [InlineData(IgnoreSource.Global, "engine/gpu/hidden.cs")]
    [InlineData(IgnoreSource.RepositoryLocal, "tools/runtime/hidden.ts")]
    [InlineData(IgnoreSource.Global, "tools/runtime/hidden.ts")]
    [InlineData(IgnoreSource.RepositoryLocal, "assets/hidden.tscn")]
    [InlineData(IgnoreSource.Global, "assets/hidden.tscn")]
    [InlineData(IgnoreSource.RepositoryLocal, "Directory.Build.targets")]
    [InlineData(IgnoreSource.Global, "Directory.Build.targets")]
    public void IgnoreRulesCannotHideProductionInputs(IgnoreSource source, string path)
    {
        if (source == IgnoreSource.RepositoryLocal) File.WriteAllText(Path.Combine(_root, ".git/info/exclude"), path + "\n");
        else
        {
            var ignoreFile = Path.Combine(_artifact, "global-ignore");
            File.WriteAllText(ignoreFile, path + "\n");
            Processes.Git(_root, "config", "core.excludesFile", ignoreFile);
        }
        Write(path, "ignored production input");
        Assert.Equal(path, Processes.Git(_root, "check-ignore", path).Trim());
        var refusal = Assert.Throws<RunnerException>(() => PublishedArtifact.Admit(_root, _artifact));
        Assert.Equal(RunnerError.InvalidArtifact, refusal.Error);
        Assert.Contains(path, refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AuditedGeneratedOutputsAndNpmInternalLinksDoNotInvalidateTheArtifact()
    {
        foreach (var path in new[]
        {
            "Production/bin/Debug/net10.0/Production.dll", "Production/obj/project.assets.json",
            ".godot/imported/resource", ".tools/sdk/tool", ".runtime-build/runtime.js", ".anvil/run/summary.json",
            "TestResults/result.trx", "CuriousContraptions.web/AppBundle/index.html",
            "elements/elements.bundle.json", "CuriousContraptions.web/wwwroot/workshop-client.js",
            "node_modules/typescript/bin/tsc",
        }) Write(path, "generated");
        Directory.CreateDirectory(Path.Combine(_root, "node_modules/.bin"));
        File.CreateSymbolicLink(Path.Combine(_root, "node_modules/.bin/tsc"), "../typescript/bin/tsc");
        Assert.Equal(_revision, PublishedArtifact.Admit(_root, _artifact).Receipt.SourceRevision);
    }

    [Theory]
    [InlineData("engine/bin/hidden.cs")]
    [InlineData("tools/runtime/obj/hidden.ts")]
    [InlineData("unknown/AppBundle/index.html")]
    [InlineData("CuriousContraptions.web/wwwroot/unexpected.js")]
    [InlineData("unknown.tmp")]
    public void GeneratedLookingUnknownInputsStillRefuse(string path)
    {
        File.WriteAllText(Path.Combine(_root, ".git/info/exclude"), "*");
        Write(path, "unknown");
        Assert.Equal(RunnerError.InvalidArtifact, Assert.Throws<RunnerException>(() => PublishedArtifact.Admit(_root, _artifact)).Error);
    }

    [Theory]
    [InlineData("node_modules")]
    [InlineData("engine/linked")]
    public void RedirectedGeneratedRootsAndUnknownSourceDirectoriesRefuse(string path)
    {
        var full = Path.Combine(_root, path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        Directory.CreateSymbolicLink(full, _artifact);
        Assert.Equal(RunnerError.InvalidArtifact, Assert.Throws<RunnerException>(() => PublishedArtifact.Admit(_root, _artifact)).Error);
    }

    [Fact]
    public void ExactGeneratedFileLinksAndTrackedOutputChangesRefuse()
    {
        Write("Production/obj/tracked.txt", "changed tracked output");
        Assert.Equal(RunnerError.InvalidArtifact, Assert.Throws<RunnerException>(() => PublishedArtifact.Admit(_root, _artifact)).Error);
        Write("Production/obj/tracked.txt", "tracked");
        Directory.CreateDirectory(Path.Combine(_root, "elements"));
        File.CreateSymbolicLink(Path.Combine(_root, "elements/elements.bundle.json"), Path.Combine(_root, "app.cs"));
        Assert.Equal(RunnerError.InvalidArtifact, Assert.Throws<RunnerException>(() => PublishedArtifact.Admit(_root, _artifact)).Error);
    }

    [Theory]
    [InlineData("0", true)]
    [InlineData("-1", true)]
    [InlineData("not-a-number", true)]
    [InlineData("0", false)]
    [InlineData("-1", false)]
    [InlineData("not-a-number", false)]
    public void InvalidCiIdentityRefusesAtProducerAndConsumer(string value, bool runId)
    {
        var valid = PublishedArtifact.Admit(_root, _artifact).Receipt;
        var run = runId ? value : valid.CiRun;
        var attempt = runId ? valid.CiAttempt : value;
        Assert.Equal(RunnerError.InvalidArtifact,
            Assert.Throws<RunnerException>(() => PublishedArtifact.Record(_root, _artifact, _revision, run, attempt)).Error);
        File.WriteAllText(Path.Combine(_artifact, PublishedArtifact.ReceiptName),
            JsonSerializer.Serialize(valid with { CiRun = run, CiAttempt = attempt }));
        Assert.Equal(RunnerError.InvalidArtifact, Assert.Throws<RunnerException>(() => PublishedArtifact.Admit(_root, _artifact)).Error);
    }

    public void Dispose()
    {
        Directory.Delete(_root, true);
        Directory.Delete(_artifact, true);
    }
}
