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

    // S5b: the e2e suites' flatbuffers runtime must be the version tools/e2e/package.json pins, installed in tools/e2e itself. The
    // pin is read from the manifest (1.2.3 here, not the real pin). Controls: nothing installed, a copy above tools/e2e only, and a
    // wrong installed version are each a typed refusal; the pinned version installed resolves. Boundary: a missing manifest,
    // malformed JSON, a non-object root or devDependencies, a pin that is not a string or not exactly x.y.z in ASCII digits (a
    // range, a trailing newline, a build or prerelease suffix, a non-ASCII digit), and an installed manifest without a version
    // string are typed refusals naming the reason; Toolchain.Resolve (with the pinned tsx loader present) refuses a missing
    // runtime, and then a missing generated entry module, before it looks for Playwright or Chrome.
    [Fact]
    public void TheE2eFlatBuffersRuntimeMustBeThePinnedVersionInstalledInToolsE2e()
    {
        var root = Directory.CreateTempSubdirectory("e2e-runtime-");
        var home = FakeHomeWithTsx();
        try
        {
            var e2e = Path.Combine(root.FullName, "tools", "e2e");
            Directory.CreateDirectory(e2e);
            Assert.Contains("tools/e2e/package.json is missing", Refusal(root.FullName), StringComparison.Ordinal);
            File.WriteAllText(Path.Combine(e2e, "package.json"), """{ "devDependencies": { "flatbuffers": """);
            Assert.Contains("tools/e2e/package.json is not valid JSON", Refusal(root.FullName), StringComparison.Ordinal);
            File.WriteAllText(Path.Combine(e2e, "package.json"), """{ "devDependencies": { "flatbuffers": 25 } }""");
            Assert.Contains("tools/e2e/package.json pins flatbuffers as 25, not a version string", Refusal(root.FullName), StringComparison.Ordinal);
            foreach (var manifest in UnpinnedManifests)
            {
                File.WriteAllText(Path.Combine(e2e, "package.json"), manifest);
                Assert.Contains("tools/e2e/package.json pins no flatbuffers devDependency", Refusal(root.FullName), StringComparison.Ordinal);
            }
            foreach (var pin in InexactPins)
            {
                File.WriteAllText(Path.Combine(e2e, "package.json"), $$"""{ "devDependencies": { "flatbuffers": {{pin}} } }""");
                Assert.Contains($"tools/e2e/package.json pins flatbuffers as {pin}, not an exact x.y.z version; the runner compares the installed version with the pin exactly",
                    Refusal(root.FullName), StringComparison.Ordinal);
            }
            File.WriteAllText(Path.Combine(e2e, "package.json"), """{ "private": true, "devDependencies": { "flatbuffers": "1.2.3" } }""");
            Assert.Contains("found none", Refusal(root.FullName), StringComparison.Ordinal);
            Assert.Contains("flatbuffers 1.2.3, pinned by tools/e2e/package.json", ResolveRefusal(home.FullName, root.FullName), StringComparison.Ordinal);

            Install(Path.Combine(root.FullName, "node_modules", "flatbuffers"), "1.2.3");
            Assert.Contains("found none", Refusal(root.FullName), StringComparison.Ordinal);

            var installed = Path.Combine(e2e, "node_modules", "flatbuffers");
            foreach (var manifest in VersionlessManifests)
            {
                Directory.CreateDirectory(installed);
                File.WriteAllText(Path.Combine(installed, "package.json"), manifest);
                Assert.Contains("tools/e2e/node_modules/flatbuffers/package.json has no version string", Refusal(root.FullName), StringComparison.Ordinal);
            }
            Install(installed, "1.2.4");
            Assert.Contains("flatbuffers 1.2.3, pinned by tools/e2e/package.json, is not installed in tools/e2e/node_modules (found 1.2.4)",
                Refusal(root.FullName), StringComparison.Ordinal);
            // Boundary: a dangling link in place of the installed manifest exists to File.Exists but cannot be read, so Resolve's
            // one IO handler refuses it, naming the path.
            var installedManifest = Path.Combine(installed, "package.json");
            File.Delete(installedManifest);
            File.CreateSymbolicLink(installedManifest, Path.Combine(root.FullName, "no-such-manifest.json"));
            var dangling = ResolveRefusal(home.FullName, root.FullName);
            Assert.Contains("A toolchain input cannot be read (", dangling, StringComparison.Ordinal);
            Assert.Contains(installedManifest, dangling, StringComparison.Ordinal);
            File.Delete(installedManifest);

            Install(installed, "1.2.3");
            Assert.Equal("1.2.3", Toolchain.RequireE2eRuntime(root.FullName));
            Assert.Contains("tools/e2e/.wire/curious-contraptions/wire.ts is missing", ResolveRefusal(home.FullName, root.FullName), StringComparison.Ordinal);
        }
        finally
        {
            root.Delete(true);
            home.Delete(true);
        }
    }

    private static readonly string[] UnpinnedManifests = ["""{ "devDependencies": { } }""", "[]", """{ "devDependencies": "x" }"""];
    private static readonly string[] VersionlessManifests = ["[]", """{ "version": 5 }"""];
    private static readonly string[] InexactPins = ["\"^1.2.3\"", "\"25.2.10\\n\"", "\"1.2.3+build\"", "\"1.2.3-beta\"", "\"1.2.٣\""];

    private static string ResolveRefusal(string home, string root)
    {
        var error = Assert.Throws<RunnerException>(() => Toolchain.Resolve(home, root));
        Assert.Equal(RunnerError.ToolchainMissing, error.Error);
        return error.Message;
    }

    private static string Refusal(string root)
    {
        var error = Assert.Throws<RunnerException>(() => Toolchain.RequireE2eRuntime(root));
        Assert.Equal(RunnerError.ToolchainMissing, error.Error);
        return error.Message;
    }

    private static void Install(string package, string version)
    {
        Directory.CreateDirectory(package);
        File.WriteAllText(Path.Combine(package, "package.json"), $$"""{ "name": "flatbuffers", "version": "{{version}}" }""");
    }

    // A home whose npx cache holds the pinned tsx loader, so Toolchain.Resolve gets past it.
    private static DirectoryInfo FakeHomeWithTsx()
    {
        var home = Directory.CreateTempSubdirectory("e2e-home-");
        var tsx = Path.Combine(home.FullName, ".npm", "_npx", "fixture", "node_modules", "tsx");
        Directory.CreateDirectory(Path.Combine(tsx, "dist"));
        File.WriteAllText(Path.Combine(tsx, "package.json"), $$"""{ "name": "tsx", "version": "{{Toolchain.TsxVersionRequired}}" }""");
        File.WriteAllText(Path.Combine(tsx, "dist", "loader.mjs"), "export {};\n");
        return home;
    }

    // S5b: the suites import the generated wire entry module, which a Debug build writes; without it, or with it older than a
    // schema or the vendored FlatBuffers project file, the runner refuses, naming the module and the build that (re)generates it.
    // Boundary: with no or an empty schemas/, or no project file, the runner refuses naming the missing input; a schema record
    // that is absent or has a directory in its place reaches Toolchain.Resolve's one file-system refusal (the smallest public
    // entry that holds it; the pinned tsx loader and runtime are present), whose cause names the path; a
    // schema renamed or deleted with every write time kept is refused naming the recorded and the current names. Control: a
    // present module, newer than both inputs and recorded from the current names, passes.
    [Fact]
    public void TheGeneratedWireEntryModuleMustExistBeforeSuitesRun()
    {
        var root = Directory.CreateTempSubdirectory("e2e-wire-");
        var home = FakeHomeWithTsx();
        try
        {
            Assert.Contains("tools/e2e/.wire/curious-contraptions/wire.ts is missing; a Debug build", Refused(root.FullName), StringComparison.Ordinal);

            var entry = Path.Combine(root.FullName, "tools", "e2e", ".wire", "curious-contraptions", "wire.ts");
            Directory.CreateDirectory(Path.GetDirectoryName(entry)!);
            File.WriteAllText(entry, "export {};\n");
            Assert.Contains("No schemas/*.fbs file exists, so the generated wire entry module tools/e2e/.wire/curious-contraptions/wire.ts", Refused(root.FullName), StringComparison.Ordinal);
            Directory.CreateDirectory(Path.Combine(root.FullName, "schemas"));
            Assert.Contains("No schemas/*.fbs file exists, so the generated wire entry module tools/e2e/.wire/curious-contraptions/wire.ts", Refused(root.FullName), StringComparison.Ordinal);

            var schema = Path.Combine(root.FullName, "schemas", "s.fbs");
            Directory.CreateDirectory(Path.GetDirectoryName(schema)!);
            File.WriteAllText(schema, "table S {}\n");
            Assert.Contains("CuriousContraptions.FlatBuffers/CuriousContraptions.FlatBuffers.csproj is missing, so the generated wire entry module", Refused(root.FullName), StringComparison.Ordinal);

            var project = Path.Combine(root.FullName, "CuriousContraptions.FlatBuffers", "CuriousContraptions.FlatBuffers.csproj");
            Directory.CreateDirectory(Path.GetDirectoryName(project)!);
            File.WriteAllText(project, "<Project />\n");
            // A missing record, and a directory in its place, each reach Resolve's one IO refusal; its cause names the path.
            File.WriteAllText(Path.Combine(root.FullName, "tools", "e2e", "package.json"), """{ "devDependencies": { "flatbuffers": "1.2.3" } }""");
            Install(Path.Combine(root.FullName, "tools", "e2e", "node_modules", "flatbuffers"), "1.2.3");
            var record = Path.Combine(root.FullName, "tools", "e2e", ".wire", "schemas.txt");
            var missingRecord = ResolveRefusal(home.FullName, root.FullName);
            Assert.Contains(UnreadableInput, missingRecord, StringComparison.Ordinal);
            Assert.Contains(record, missingRecord, StringComparison.Ordinal);
            Assert.EndsWith(InputRemedy, missingRecord, StringComparison.Ordinal);
            Directory.CreateDirectory(record);
            var occupiedRecord = ResolveRefusal(home.FullName, root.FullName);
            Assert.Contains(UnreadableInput, occupiedRecord, StringComparison.Ordinal);
            Assert.Contains(record, occupiedRecord, StringComparison.Ordinal);
            Assert.EndsWith(InputRemedy, occupiedRecord, StringComparison.Ordinal);
            Directory.Delete(record);
            File.WriteAllText(record, "s.fbs\n");
            File.SetLastWriteTimeUtc(project, File.GetLastWriteTimeUtc(schema));
            File.SetLastWriteTimeUtc(entry, File.GetLastWriteTimeUtc(schema).AddMinutes(-1));
            Assert.Contains(StaleEntry, Refused(root.FullName), StringComparison.Ordinal);

            File.SetLastWriteTimeUtc(entry, File.GetLastWriteTimeUtc(schema).AddMinutes(1));
            Toolchain.RequireGeneratedWireModule(root.FullName);

            File.SetLastWriteTimeUtc(project, File.GetLastWriteTimeUtc(entry).AddMinutes(1));
            Assert.Contains(StaleEntry, Refused(root.FullName), StringComparison.Ordinal);

            File.SetLastWriteTimeUtc(entry, File.GetLastWriteTimeUtc(project).AddMinutes(1));
            Toolchain.RequireGeneratedWireModule(root.FullName);

            // Boundary: with every input valid, a dangling link in place of the project file passes its existence probe but fails
            // the handle-based write-time read, which Resolve's one handler refuses naming the path; the file is then restored.
            var projectTime = File.GetLastWriteTimeUtc(project);
            File.Delete(project);
            File.CreateSymbolicLink(project, Path.Combine(root.FullName, "no-such-project.csproj"));
            var danglingProject = ResolveRefusal(home.FullName, root.FullName);
            Assert.Contains(UnreadableInput, danglingProject, StringComparison.Ordinal);
            Assert.Contains(project, danglingProject, StringComparison.Ordinal);
            Assert.EndsWith(InputRemedy, danglingProject, StringComparison.Ordinal);
            File.Delete(project);
            File.WriteAllText(project, "<Project />\n");
            File.SetLastWriteTimeUtc(project, projectTime);
            Toolchain.RequireGeneratedWireModule(root.FullName);

            // A schema renamed, then a recorded schema deleted, each with every write time kept.
            var renamed = Path.Combine(root.FullName, "schemas", "t.fbs");
            File.Move(schema, renamed);
            Assert.Contains("was generated from the schemas [s.fbs], but schemas/ holds [t.fbs]; a Debug build", Refused(root.FullName), StringComparison.Ordinal);
            File.Move(renamed, schema);
            Toolchain.RequireGeneratedWireModule(root.FullName);
            var second = Path.Combine(root.FullName, "schemas", "u.fbs");
            File.WriteAllText(record, "s.fbs\nu.fbs\n");
            File.WriteAllText(second, "table U {}\n");
            File.SetLastWriteTimeUtc(second, File.GetLastWriteTimeUtc(schema));
            Toolchain.RequireGeneratedWireModule(root.FullName);
            File.Delete(second);
            Assert.Contains("was generated from the schemas [s.fbs, u.fbs], but schemas/ holds [s.fbs]; a Debug build", Refused(root.FullName), StringComparison.Ordinal);
        }
        finally
        {
            root.Delete(true);
            home.Delete(true);
        }
    }

    private static string Refused(string root)
    {
        var error = Assert.Throws<RunnerException>(() => Toolchain.RequireGeneratedWireModule(root));
        Assert.Equal(RunnerError.ToolchainMissing, error.Error);
        return error.Message;
    }

    private const string UnreadableInput = "A toolchain input cannot be read (";
    private const string InputRemedy = "); a Debug build of the solution (or of CuriousContraptions.FlatBuffers) regenerates the generated files and the schema record, and any other path that cannot be read must be restored, made readable or, where something else occupies a generated path, removed.";
    private const string StaleEntry = "tools/e2e/.wire/curious-contraptions/wire.ts is older than a schemas/*.fbs file or CuriousContraptions.FlatBuffers/CuriousContraptions.FlatBuffers.csproj; a Debug build";

    [Fact]
    public void ConfigUsesTheExistingPlaywrightExternalBoundaryAndStrictChecking()
    {
        using var config = JsonDocument.Parse(TestRepository.Read(TypeCheck.Project));
        var options = config.RootElement.GetProperty("compilerOptions");
        Assert.True(options.GetProperty("strict").GetBoolean());
        Assert.True(options.GetProperty("noEmit").GetBoolean());
        Assert.Equal(Path.Combine(Toolchain.PlaywrightMcp, "node_modules", "playwright"),
            options.GetProperty("paths").GetProperty("playwright")[0].GetString());
    }
}
