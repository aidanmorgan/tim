using System.Text.Json;
using Xunit;

namespace CuriousContraptions.E2eRunner.Tests;

public sealed class TypeCheckBoundaryTests
{
    [Fact]
    public void CompilerAbsenceIsARefusalNotASkip()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "tsc");
        var error = Assert.Throws<RunnerException>(() => TypeCheck.Run(missing, TestRepository.Root, TypeCheck.Project));
        Assert.Equal(RunnerError.ToolchainMissing, error.Error);
    }

    [Fact]
    public void MissingPinnedToolchainIsExplicit()
    {
        var home = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Assert.Equal(RunnerError.ToolchainMissing, Assert.Throws<RunnerException>(() => Toolchain.ResolveTypeScript(home)).Error);
        Assert.Equal(RunnerError.ToolchainMissing, Assert.Throws<RunnerException>(() => Toolchain.ResolveNodeTypes(home)).Error);
    }

    [Fact]
    public void TheE2eFlatBuffersRuntimeMustBeThePinnedVersionInstalledAtTheRoot()
    {
        using var copy = new WireAdmissionCopy();
        var root = copy.PathOf("package.json");
        var pinned = File.ReadAllText(root);
        using var manifest = JsonDocument.Parse(pinned);
        var pin = manifest.RootElement.GetProperty("devDependencies").GetProperty("flatbuffers").GetString()!;
        File.Delete(root);
        Assert.Contains("package.json", Refusal(copy), StringComparison.Ordinal);
        File.WriteAllText(root, """{"devDependencies":{""");
        Assert.Contains("package.json", Refusal(copy), StringComparison.Ordinal);
        string[] invalid = ["[]", """{"devDependencies":"x"}""", """{"devDependencies":{}}"""];
        foreach (var text in invalid)
        {
            File.WriteAllText(root, text);
            Assert.Contains("must equal the declared pin", Refusal(copy), StringComparison.Ordinal);
        }
        string[] inexact = ["25", "\"^1.2.3\"", "\"25.2.10\\n\"", "\"1.2.3+build\"", "\"1.2.3-beta\"", "\"1.2.٣\""];
        foreach (var value in inexact)
        {
            File.WriteAllText(root, pinned.Replace("\"" + pin + "\"", value, StringComparison.Ordinal));
            Assert.Contains("must equal the declared pin", Refusal(copy), StringComparison.Ordinal);
        }
        File.WriteAllText(root, pinned);

        var installed = copy.PathOf("node_modules/flatbuffers");
        var saved = copy.PathOf("node_modules/flatbuffers-displaced");
        Directory.Move(installed, saved);
        Assert.Contains("root FlatBuffers installation is missing", Refusal(copy), StringComparison.Ordinal);
        CheckShadow(copy, "tools/e2e");
        var foreignEntry = Path.Combine(saved, "js", "flatbuffers.js");
        File.Move(foreignEntry, foreignEntry + ".saved");
        Directory.CreateSymbolicLink(installed, saved);
        Assert.Contains("FlatBuffers installation resolves outside its canonical root path", Refusal(copy), StringComparison.Ordinal);
        Directory.Delete(installed);
        File.Move(foreignEntry + ".saved", foreignEntry);
        Directory.Move(saved, installed);

        var package = Path.Combine(installed, "package.json");
        var packageText = File.ReadAllText(package);
        File.Delete(package);
        File.CreateSymbolicLink(package, copy.PathOf("no-such-manifest.json"));
        Assert.Contains("installed FlatBuffers manifest is missing or unreadable", Refusal(copy), StringComparison.Ordinal);
        File.Delete(package);
        var foreignManifest = copy.PathOf("foreign-package.json");
        File.WriteAllText(foreignManifest, "invalid JSON");
        File.CreateSymbolicLink(package, foreignManifest);
        Assert.Contains("installed FlatBuffers manifest resolves outside its canonical root path", Refusal(copy), StringComparison.Ordinal);
        File.Delete(package);
        foreach (var value in new[] { "[]", """{"version":5}""" })
        {
            File.WriteAllText(package, value);
            Assert.Contains("must equal the declared pin", Refusal(copy), StringComparison.Ordinal);
        }

        const string wrong = "0.0.0";
        File.WriteAllText(package, packageText.Replace(pin, wrong, StringComparison.Ordinal));
        Assert.Contains(wrong, Refusal(copy), StringComparison.Ordinal);
        var lockPath = copy.PathOf("package-lock.json");
        var lockText = File.ReadAllText(lockPath);
        File.WriteAllText(lockPath, lockText.Replace(pin, wrong, StringComparison.Ordinal));
        File.WriteAllText(root, pinned.Replace(pin, wrong, StringComparison.Ordinal));
        Assert.Contains($"[\"{wrong}\",\"{wrong}\",\"{wrong}\",\"{wrong}\"] must equal the declared pin for flatc {pin}",
            Refusal(copy), StringComparison.Ordinal);
        File.WriteAllText(lockPath, lockText);
        File.WriteAllText(root, pinned);
        File.WriteAllText(package, packageText);
        File.WriteAllText(lockPath, lockText.Replace(pin, wrong, StringComparison.Ordinal));
        Assert.Contains("must equal the declared pin", Refusal(copy), StringComparison.Ordinal);
        File.WriteAllText(lockPath, lockText);

        var runtime = Path.Combine(installed, "js", "flatbuffers.js");
        File.Move(runtime, runtime + ".saved");
        Assert.Contains("could not be loaded from the root installation", Refusal(copy), StringComparison.Ordinal);
        Assert.False(File.Exists(runtime));
        var foreignRuntime = copy.PathOf("foreign-flatbuffers.js");
        File.Move(runtime + ".saved", foreignRuntime);
        File.CreateSymbolicLink(runtime, foreignRuntime);
        Assert.Contains("links are refused", Refusal(copy), StringComparison.Ordinal);
        File.Delete(runtime);
        File.Move(foreignRuntime, runtime);

        var compiler = copy.PathOf("node_modules/typescript/bin/tsc");
        File.Move(compiler, compiler + ".saved");
        File.CreateSymbolicLink(compiler, compiler + ".saved");
        Assert.Contains("local TypeScript compiler node_modules/typescript/bin/tsc resolves outside its canonical root path",
            Refusal(copy), StringComparison.Ordinal);
        File.Delete(compiler);
        File.Move(compiler + ".saved", compiler);
        RefuseLinkedPackageMember(copy, "node_modules/flatbuffers/js/builder.js");
        RefuseLinkedPackageMember(copy, "node_modules/typescript/lib/_tsc.js");
        copy.Generate();

        string[] importers = ["tools/runtime", "tools/e2e", "tools/e2e/.wire/curious-contraptions/wire",
            ".runtime-build/tools/runtime", ".runtime-build/tools/e2e/.wire/curious-contraptions/wire"];
        foreach (var importer in importers)
            CheckShadow(copy, importer);
        AssertCompleteGeneratedSet(copy, Toolchain.AdmitRuntimeTypeScriptArtifacts(copy.Root));
    }

    private static void CheckShadow(WireAdmissionCopy copy, string importer)
    {
        var shadow = copy.PathOf(importer + "/node_modules/flatbuffers");
        Directory.CreateDirectory(shadow);
        var error = Assert.Throws<RunnerException>(() => Toolchain.Resolve(copy.Home, copy.Root));
        Assert.Equal(RunnerError.ToolchainMissing, error.Error);
        Assert.Contains($"{shadow} shadows the pinned root package", error.Message, StringComparison.Ordinal);
        Assert.True(Directory.Exists(shadow));
        Directory.Delete(shadow);
    }

    [Fact]
    public void TheGeneratedWireEntryModuleMustExistBeforeSuitesRun()
    {
        using var copy = new WireAdmissionCopy();
        var entry = copy.PathOf(WireAdmissionCopy.Entry);
        Assert.Contains("wire.ts is missing", Refusal(copy), StringComparison.Ordinal);
        Assert.False(File.Exists(entry));
        Assert.False(File.Exists(copy.PathOf("node_modules/.package-lock.json")));
        copy.Generate();

        var entryTime = File.GetLastWriteTimeUtc(entry);
        foreach (var path in WireAdmissionCopy.GenerationInputs)
        {
            var input = copy.PathOf(path);
            var current = File.GetLastWriteTimeUtc(input);
            File.SetLastWriteTimeUtc(input, entryTime.AddMinutes(1));
            Assert.Contains("is older than its generation inputs", Refusal(copy), StringComparison.Ordinal);
            Assert.Equal(entryTime, File.GetLastWriteTimeUtc(entry));
            File.SetLastWriteTimeUtc(input, current);
            File.Move(input, input + ".saved");
            Assert.Contains(Path.GetFileName(input), Refusal(copy), StringComparison.Ordinal);
            Assert.False(File.Exists(input));
            File.Move(input + ".saved", input);
        }

        var schema = copy.PathOf("schemas/workshop_wire.fbs");
        File.Move(schema, schema + ".saved");
        var extra = copy.PathOf("schemas/unrelated.fbs");
        File.WriteAllText(extra, "table Unrelated {}\n");
        Assert.Contains("Only the canonical wire schema", Refusal(copy), StringComparison.Ordinal);
        File.Move(schema + ".saved", schema);
        Assert.Contains("unrelated.fbs", Refusal(copy), StringComparison.Ordinal);
        Assert.Equal(entryTime, File.GetLastWriteTimeUtc(entry));
        File.Delete(extra);
        CheckLeaves(copy, entry);
        CheckRetiredArtifacts(copy, entry);
        Assert.False(File.Exists(copy.PathOf("node_modules/.package-lock.json")));
    }

    private static void CheckLeaves(WireAdmissionCopy copy, string entry)
    {
        const string expected = "tools/e2e/.wire/curious-contraptions/wire/puzzle-record.ts";
        var leaf = copy.PathOf(expected);
        var admitted = Toolchain.AdmitRuntimeTypeScriptArtifacts(copy.Root);
        AssertCompleteGeneratedSet(copy, admitted);

        var time = File.GetLastWriteTimeUtc(leaf);
        var stale = time.AddDays(-1);
        File.SetLastWriteTimeUtc(leaf, stale);
        Assert.Contains("puzzle-record.ts is older than its generation inputs", Refusal(copy), StringComparison.Ordinal);
        Assert.Equal(stale, File.GetLastWriteTimeUtc(leaf));
        File.SetLastWriteTimeUtc(leaf, time);
        var savedLeaf = copy.PathOf("saved-wire-leaf");
        File.Move(leaf, savedLeaf);
        Assert.Contains("Missing generated TypeScript wire module", Refusal(copy), StringComparison.Ordinal);
        Assert.False(File.Exists(leaf));
        File.CreateSymbolicLink(leaf, savedLeaf);
        Assert.Contains("Linked generated wire source", Refusal(copy), StringComparison.Ordinal);
        Assert.NotNull(new FileInfo(leaf).LinkTarget);
        Assert.True(File.Exists(savedLeaf));
        File.Delete(leaf);
        File.Move(savedLeaf, leaf);

        var leafText = File.ReadAllText(leaf);
        const string retired = "export class PuzzleRecord {\n}\n";
        File.WriteAllText(leaf, retired);
        Assert.Contains("puzzle-record.ts is missing its current object API", Refusal(copy), StringComparison.Ordinal);
        Assert.Equal(retired, File.ReadAllText(leaf));
        File.WriteAllText(leaf, leafText);
        var entryText = File.ReadAllText(entry);
        File.WriteAllLines(entry, File.ReadLines(entry).Where(line => !line.Contains("wire/puzzle-record.js", StringComparison.Ordinal)).ToArray());
        var entryTime = File.GetLastWriteTimeUtc(entry);
        Assert.Contains("exports do not match the canonical schema", Refusal(copy), StringComparison.Ordinal);
        Assert.Equal(entryTime, File.GetLastWriteTimeUtc(entry));
        File.WriteAllText(entry, entryText);

        var lines = File.ReadAllLines(entry);
        var missing = Array.FindIndex(lines, line => line.Contains("wire/puzzle-record.js", StringComparison.Ordinal));
        lines[missing] = lines.Single(line => line.Contains("wire/simulation-phase.js", StringComparison.Ordinal));
        File.WriteAllLines(entry, lines);
        Assert.Contains("missing its expected export for PuzzleRecord", Refusal(copy), StringComparison.Ordinal);
        File.WriteAllText(entry, entryText);

        File.AppendAllText(entry, "\n  export { PuzzleRecord as Extra } from './wire/puzzle-record.js';\n");
        var aliasTime = File.GetLastWriteTimeUtc(entry);
        Assert.Contains("exports do not match the canonical schema", Refusal(copy), StringComparison.Ordinal);
        Assert.Equal(aliasTime, File.GetLastWriteTimeUtc(entry));
        File.WriteAllText(entry, entryText);

        var stray = Path.Combine(Path.GetDirectoryName(leaf)!, "unlisted.ts");
        File.Copy(leaf, stray);
        Assert.Contains("Unlisted generated TypeScript wire module", Refusal(copy), StringComparison.Ordinal);
        Assert.True(File.Exists(stray));
        File.Delete(stray);
        Assert.Equal(admitted, Toolchain.AdmitRuntimeTypeScriptArtifacts(copy.Root));

        Processes.Git(copy.Root, "init", "-q");
        File.WriteAllText(copy.PathOf(".gitignore"), "node_modules/\nhome/\ntools/e2e/.wire/\n");
        var suiteStart = SourceIdentity.SuiteSources(copy.Root, admitted);
        File.Move(leaf, leaf + ".saved");
        Assert.NotEqual(suiteStart, SourceIdentity.SuiteSources(copy.Root, admitted));
        File.Move(leaf + ".saved", leaf);
        Assert.Equal(suiteStart, SourceIdentity.SuiteSources(copy.Root, admitted));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"Identity":null}""")]
    [InlineData("""{"Identity":5}""")]
    [InlineData("""{"Identity":"../foreign.ts"}""")]
    [InlineData("""{"Identity":""}""")]
    public void MalformedWireAdmissionIdentityIsATypedRefusal(string item)
    {
        var output = WireAdmissionResult("\"Result\":\"Success\",", EntryItem + "," + LeafItem + "," + item);
        var error = Assert.Throws<RunnerException>(() => Toolchain.ReadAdmittedWireSources(output));
        Assert.Equal(RunnerError.ToolchainMissing, error.Error);
    }

    private const string EntryItem = """{"Identity":"tools/e2e/.wire/curious-contraptions/wire.ts"}""";
    private const string LeafItem = """{"Identity":"tools/e2e/.wire/curious-contraptions/wire/puzzle-record.ts"}""";

    [Theory]
    [InlineData("", EntryItem + "," + LeafItem, 2)]
    [InlineData("\"Result\":\"Failure\",", EntryItem + "," + LeafItem, 2)]
    [InlineData("\"Result\":null,", EntryItem + "," + LeafItem, 2)]
    [InlineData("\"Result\":\"Success\",", EntryItem, 1)]
    [InlineData("\"Result\":\"Success\",", EntryItem + "," + EntryItem + "," + LeafItem, 3)]
    [InlineData("\"Result\":\"Success\",", EntryItem + "," + LeafItem + "," + LeafItem, 3)]
    [InlineData("\"Result\":\"Success\",", EntryItem + "," + """{"Identity":"tools/runtime/other.ts"}""", 2)]
    [InlineData("\"Result\":\"Success\",", EntryItem + "," + """{"Identity":"tools/e2e/.wire/curious-contraptions/wire/nested/leaf.ts"}""", 2)]
    public void NoncanonicalWireAdmissionResultIsATypedRefusal(string status, string items, int count)
    {
        var error = Assert.Throws<RunnerException>(() => Toolchain.ReadAdmittedWireSources(WireAdmissionResult(status, items, $"\"{count}\"")));
        Assert.Equal(RunnerError.ToolchainMissing, error.Error);
    }

    [Fact]
    public void TwoCanonicalLeavesCannotStandInForTheGeneratedEntry()
    {
        const string secondLeaf = """{"Identity":"tools/e2e/.wire/curious-contraptions/wire/simulation-phase.ts"}""";
        var output = WireAdmissionResult("\"Result\":\"Success\",", LeafItem + "," + secondLeaf, "\"2\"");
        var error = Assert.Throws<RunnerException>(() => Toolchain.ReadAdmittedWireSources(output));
        Assert.Equal(RunnerError.ToolchainMissing, error.Error);
        Assert.Contains("requires the generated wire entry", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("\"AdmittedWireSourceCount\":\"3\"")]
    [InlineData("\"AdmittedWireSourceCount\":\"not-count\"")]
    [InlineData("\"AdmittedWireSourceCount\":null")]
    [InlineData("")]
    public void IncompleteWireAdmissionResultIsATypedRefusal(string properties)
    {
        var output = WireAdmissionResult("\"Result\":\"Success\",", EntryItem + "," + LeafItem, properties: properties);
        var error = Assert.Throws<RunnerException>(() => Toolchain.ReadAdmittedWireSources(output));
        Assert.Equal(RunnerError.ToolchainMissing, error.Error);
    }

    private static string WireAdmissionResult(string status, string items, string count = "\"3\"", string? properties = null) =>
        "{\"Properties\":{" + (properties ?? "\"AdmittedWireSourceCount\":" + count) +
        "},\"TargetResults\":{\"AdmitRuntimeTypeScriptArtifacts\":{" + status + "\"Items\":[" + items + "]}}}";

    private static void AssertCompleteGeneratedSet(WireAdmissionCopy copy, IReadOnlyList<string> admitted)
    {
        var wireDirectory = copy.PathOf("tools/e2e/.wire");
        var expected = Directory.EnumerateFiles(wireDirectory, "*.ts", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(copy.Root, path).Replace('\\', '/')).Order(StringComparer.Ordinal);
        Assert.Equal(expected, admitted.Order(StringComparer.Ordinal));
    }

    private static void RefuseLinkedPackageMember(WireAdmissionCopy copy, string relative)
    {
        var member = copy.PathOf(relative);
        var foreign = copy.PathOf("displaced-package-member");
        File.Move(member, foreign);
        File.CreateSymbolicLink(member, foreign);
        var refusal = Refusal(copy);
        Assert.Contains("links are refused", refusal, StringComparison.Ordinal);
        Assert.Contains(relative, refusal, StringComparison.Ordinal);
        Assert.True(File.Exists(foreign));
        File.Delete(member);
        File.Move(foreign, member);
    }

    private static void CheckRetiredArtifacts(WireAdmissionCopy copy, string entry)
    {
        var entryTime = File.GetLastWriteTimeUtc(entry);
        string[] paths = ["tools/e2e/.wire/schemas.txt", "tools/e2e/package.json", "tools/e2e/package-lock.json"];
        foreach (var path in paths)
        {
            var retired = copy.PathOf(path);
            File.WriteAllText(retired, "obsolete");
            var retiredTime = File.GetLastWriteTimeUtc(retired);
            var refusal = Refusal(copy);
            Assert.Contains("Retired wire artifact", refusal, StringComparison.Ordinal);
            Assert.Contains(path + " is present", refusal, StringComparison.Ordinal);
            Assert.Equal(retiredTime, File.GetLastWriteTimeUtc(retired));
            Assert.Equal(entryTime, File.GetLastWriteTimeUtc(entry));
            File.Delete(retired);
        }
    }

    private static string Refusal(WireAdmissionCopy copy)
    {
        var error = Assert.Throws<RunnerException>(() => Toolchain.AdmitRuntimeTypeScriptArtifacts(copy.Root));
        Assert.Equal(RunnerError.ToolchainMissing, error.Error);
        return error.Message;
    }

    private sealed class WireAdmissionCopy : IDisposable
    {
        private const string Project = "CuriousContraptions.Abstractions/CuriousContraptions.Abstractions.csproj";
        public const string Entry = "tools/e2e/.wire/curious-contraptions/wire.ts";
        public static readonly string[] GenerationInputs =
            ["schemas/workshop_wire.fbs", "tools/runtime/RuntimeTypeScript.targets", "tools/runtime/FlatcCompiler.targets"];
        private static readonly string[] Shared = ["Directory.Build.props", "Directory.Build.targets", "global.json", "package.json", "package-lock.json", Project];
        private readonly DirectoryInfo _copy = Directory.CreateTempSubdirectory("e2e-admission-");
        public string Root => _copy.FullName;
        public string Home => PathOf("home");

        public WireAdmissionCopy()
        {
            try
            {
                foreach (var path in Shared.Concat(GenerationInputs))
                    Copy(path);
                foreach (var tool in new[] { "node_modules/flatbuffers", "node_modules/typescript" })
                    foreach (var file in Directory.EnumerateFiles(Path.Combine(TestRepository.Root, tool), "*", SearchOption.AllDirectories))
                        Copy(Path.GetRelativePath(TestRepository.Root, file));
                var tsx = Path.Combine(Home, ".npm", "_npx", "fixture", "node_modules", "tsx");
                Directory.CreateDirectory(Path.Combine(tsx, "dist"));
                File.WriteAllText(Path.Combine(tsx, "package.json"), $$"""{ "name": "tsx", "version": "{{Toolchain.TsxVersionRequired}}" }""");
                File.WriteAllText(Path.Combine(tsx, "dist", "loader.mjs"), "export {};\n");
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public string PathOf(string relative) => Path.Combine(Root, relative);

        private void Copy(string relative)
        {
            var target = PathOf(relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(Path.Combine(TestRepository.Root, relative), target);
        }

        public void Generate()
        {
            var result = Processes.Run("dotnet", ["msbuild", Project, "-t:GenerateWireTypeScript", "-nologo", "-noAutoResponse", "-v:q"], Root);
            Assert.True(result.ExitCode == 0, result.Output + result.Error);
        }

        public void Dispose() => _copy.Delete(recursive: true);
    }

    [Fact]
    public void ConfigUsesTheExistingPlaywrightExternalBoundaryAndStrictChecking()
    {
        var root = TestRepository.Root;
        var text = File.ReadAllText(Path.Combine(root, "tools", "e2e", "tsconfig.json"));
        using var json = JsonDocument.Parse(text);
        var options = json.RootElement.GetProperty("compilerOptions");
        Assert.True(options.GetProperty("strict").GetBoolean());
        Assert.True(options.GetProperty("noEmit").GetBoolean());
        Assert.Equal(Toolchain.PlaywrightMcp + "/node_modules/playwright",
            options.GetProperty("paths").GetProperty("playwright")[0].GetString());
    }
}
