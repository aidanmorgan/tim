using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CuriousContraptions.Tests;

/// <summary>
/// S5 (M1 to M5, M7): the vendored FlatBuffers project generates the wire accessors from the one schema file,
/// schemas/workshop_wire.fbs, with the pinned compiler in every build that needs them, and refuses a build host without a
/// release asset and the Span accessor define. S5b (T2 to T4, T6): the same build generates the TypeScript accessors from
/// that file into the copy's tools/e2e/.wire/. Each case
/// copies the vendored project, the schema file and the shared build files into its own temporary directory outside the
/// checkout, drives real <c>dotnet build</c> child processes there and reads the built assembly's metadata. The cases
/// need the pinned compiler already in the per-user cache and check that before any build; none touches the network or
/// places anything in that cache. Like any generating build, each deletes temporary files there older than 60 minutes. The
/// leftover-file case builds against a cache of its own, inside its temporary directory, holding a copy of the cached
/// compiler.
/// </summary>
public sealed class WorkshopWireBuildTests
{
    private const string ProjectDirectory = "CuriousContraptions.FlatBuffers";
    private const string ProjectFile = ProjectDirectory + "/CuriousContraptions.FlatBuffers.csproj";
    private const string AbstractionsProjectFile = "CuriousContraptions.Abstractions/CuriousContraptions.Abstractions.csproj";
    private const string Assembly = ProjectDirectory + "/bin/Debug/net10.0/CuriousContraptions.FlatBuffers.dll";
    private const string IntermediateAssembly = ProjectDirectory + "/obj/Debug/net10.0/CuriousContraptions.FlatBuffers.dll";
    private const string GenerationFolder = ProjectDirectory + "/obj/Debug/net10.0/WireAccessors";
    private const string TypeScriptFolder = "tools/e2e/.wire";
    private const string TypeScriptEntry = TypeScriptFolder + "/curious-contraptions/wire.ts";
    private const string SavedConstructionModule = TypeScriptFolder + "/curious-contraptions/wire/saved-construction.ts";
    private const string PuzzleRecordModule = TypeScriptFolder + "/curious-contraptions/wire/puzzle-record.ts";
    private const string TypeScriptStaging = "CuriousContraptions.Abstractions/obj/Debug/net10.0/WireTypeScriptStaging";
    private const string TypeScriptStamp = "CuriousContraptions.Abstractions/obj/WireTypeScript/.stamp";
    private const string WireNamespace = "CuriousContraptions.Wire";
    private const string OneSchema = "schemas/workshop_wire.fbs";
    private const string Download = "Downloading the pinned FlatBuffers compiler";

    // The shared build files the vendored project's build reads, besides its own tracked files and the schemas.
    private static readonly string[] SharedBuildFiles =
        ["Directory.Build.props", "Directory.Build.targets", "global.json", ".editorconfig", "tools/runtime/RuntimeTypeScript.targets", "tools/runtime/FlatcCompiler.targets"];

    // A cold restore and compile take seconds; this only stops a hung child from hanging the round.
    private static readonly TimeSpan ChildTimeout = TimeSpan.FromMinutes(10);

    private static readonly string Root = twodog.Engine.ResolveProjectDir();

    [Fact]
    public void ACleanCopyGeneratesEverySchemaTypeAndCompiles()
    {
        using var copy = WireBuildCopy.Create();
        Assert.Equal(copy.TrackedSources, copy.CSharpFiles());

        copy.Build();

        var schemaTypes = copy.SchemaTypes();
        Assert.Contains("SavedConstruction", schemaTypes);
        Assert.Contains("WorkshopCommand", schemaTypes);
        Assert.Empty(schemaTypes.Except(DeclaredWireTypes(copy.PathOf(Assembly))));
        Assert.Equal(schemaTypes.Order(StringComparer.Ordinal), copy.GeneratedTypeNames().Order(StringComparer.Ordinal));
    }

    // S13: the field is added to the one schema's save root and incrementally updates both C# and TypeScript.
    [Fact]
    public void AnAddedSchemaFieldIsAnAccessorInBothLanguagesAfterAnIncrementalBuild()
    {
        using var copy = WireBuildCopy.Create();
        using var tsCopy = WireBuildCopy.CreateAbstractions();
        copy.Build();
        tsCopy.BuildTypeScript();
        Assert.False(DeclaresProperty(copy.PathOf(Assembly), "SavedConstruction", "S5Probe"));
        Assert.DoesNotContain("s5Probe():", File.ReadAllText(tsCopy.PathOf(SavedConstructionModule)), StringComparison.Ordinal);

        copy.Edit("schemas/workshop_wire.fbs", "table SavedConstruction (authoring) {\n", "table SavedConstruction (authoring) {\n    s5_probe: uint32;\n");
        tsCopy.Edit("schemas/workshop_wire.fbs", "table SavedConstruction (authoring) {\n", "table SavedConstruction (authoring) {\n    s5_probe: uint32;\n");
        copy.Build();
        tsCopy.BuildTypeScript();

        Assert.True(DeclaresProperty(copy.PathOf(Assembly), "SavedConstruction", "S5Probe"));
        Assert.Contains("s5Probe():", File.ReadAllText(tsCopy.PathOf(SavedConstructionModule)), StringComparison.Ordinal);
        Assert.Empty(WorkshopWireTypeScriptTests.GeneratedTypeScriptViolations(tsCopy.TypeScript(), tsCopy.PathOf("schemas")));
    }

    [Fact]
    public void AGlobalWireSchemaPathCannotSelectAnotherSchema()
    {
        using var csharp = WireBuildCopy.Create();
        using var typescript = WireBuildCopy.CreateAbstractions();
        csharp.Build("-p:WireSchemaPath=" + WriteForeignSchema(csharp));
        typescript.BuildTypeScript(options: ["-p:WireSchemaPath=" + WriteForeignSchema(typescript)]);
        Assert.False(DeclaresProperty(csharp.PathOf(Assembly), "SavedConstruction", "ForeignOnly"));
        Assert.DoesNotContain("foreignOnly():", File.ReadAllText(typescript.PathOf(SavedConstructionModule)), StringComparison.Ordinal);
    }

    private static string WriteForeignSchema(WireBuildCopy copy)
    {
        var path = copy.PathOf("foreign/" + OneSchema);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, File.ReadAllText(copy.PathOf(OneSchema)).Replace(
            "table SavedConstruction (authoring) {\n", "table SavedConstruction (authoring) {\n    foreign_only: uint32;\n", StringComparison.Ordinal));
        return path;
    }

    [Theory]
    [InlineData(OneSchema)]
    [InlineData("tools/runtime/FlatcCompiler.targets")]
    [InlineData("tools/runtime/RuntimeTypeScript.targets")]
    public void AnUnchangedSchemaAndCompilerSelectionDoNotRegenerate(string changedInput)
    {
        using var copy = WireBuildCopy.Create();
        copy.Build();
        var first = copy.GeneratedSources();

        copy.Build();
        Assert.Equal(first, copy.GeneratedSources());

        File.SetLastWriteTimeUtc(copy.PathOf(changedInput), DateTime.UtcNow);
        copy.Build();
        var third = copy.GeneratedSources();
        Assert.Equal(first.Keys.Order(StringComparer.Ordinal), third.Keys.Order(StringComparer.Ordinal));
        Assert.All(first, source => Assert.True(third[source.Key] > source.Value, $"{source.Key} was not regenerated"));
    }

    // T3: a current set is reused across configurations. A touched schema, explicit Clean, and interrupted publication
    // states each cause the generation target to write a complete set. The entry module is published last.
    [Fact]
    public void AnUnchangedSchemaDoesNotRegenerateTypeScript()
    {
        using var copy = WireBuildCopy.CreateAbstractions();
        Assert.False(File.Exists(copy.PathOf(TypeScriptEntry)));
        copy.BuildTypeScript(configuration: "Release");
        var first = copy.TypeScriptWriteTimes();
        Assert.Empty(WorkshopWireTypeScriptTests.GeneratedTypeScriptViolations(copy.TypeScript(), copy.PathOf("schemas")));

        copy.BuildTypeScript();
        copy.BuildTypeScript(configuration: "Release");
        Assert.Equal(first, copy.TypeScriptWriteTimes());

        File.SetLastWriteTimeUtc(copy.PathOf("schemas/workshop_wire.fbs"), DateTime.UtcNow);
        copy.BuildTypeScript();
        var touched = copy.TypeScriptWriteTimes();
        Assert.Equal(first.Keys.Order(StringComparer.Ordinal), touched.Keys.Order(StringComparer.Ordinal));
        Assert.All(first, file => Assert.True(touched[file.Key] > file.Value, $"a touched schema did not regenerate {file.Key}"));

        copy.CleanTypeScript();
        Assert.Equal(touched, copy.TypeScriptWriteTimes());
        copy.BuildTypeScript();
        var rebuilt = copy.TypeScriptWriteTimes();
        Assert.All(touched, file => Assert.True(rebuilt[file.Key] > file.Value, $"Clean did not trigger regeneration of {file.Key}"));
        Assert.False(File.Exists(copy.PathOf(TypeScriptFolder + "/schemas.txt")));

        // Per-type files present, the entry module missing, one per-type file un-normalised and a staged file left over.
        File.Delete(copy.PathOf(TypeScriptEntry));
        var puzzleRecord = copy.PathOf(PuzzleRecordModule);
        File.WriteAllText(puzzleRecord, File.ReadAllText(puzzleRecord).Replace("optionalEncoding?:flatbuffers.Encoding", "optionalEncoding?:any", StringComparison.Ordinal));
        Directory.CreateDirectory(copy.PathOf(TypeScriptStaging + "/curious-contraptions"));
        File.WriteAllText(copy.PathOf(TypeScriptStaging + "/curious-contraptions/wire.ts"), "export {};\n");
        Assert.NotEmpty(WorkshopWireTypeScriptTests.GeneratedTypeScriptViolations(copy.TypeScript(), copy.PathOf("schemas")));
        copy.BuildTypeScript();
        Assert.Empty(WorkshopWireTypeScriptTests.GeneratedTypeScriptViolations(copy.TypeScript(), copy.PathOf("schemas")));

        // The entry module older than a schema, with a staged set left over: regenerated, the staging folder cleared.
        var current = copy.TypeScriptWriteTimes();
        File.SetLastWriteTimeUtc(copy.PathOf(TypeScriptEntry), DateTime.UtcNow.AddDays(-1));
        Directory.CreateDirectory(copy.PathOf(TypeScriptStaging + "/curious-contraptions/wire"));
        File.WriteAllText(copy.PathOf(TypeScriptStaging + "/curious-contraptions/wire/vec3f.ts"), "export class Vec3f {\n");
        copy.BuildTypeScript();
        var regenerated = copy.TypeScriptWriteTimes();
        Assert.Equal(current.Keys.Order(StringComparer.Ordinal), regenerated.Keys.Order(StringComparer.Ordinal));
        Assert.All(current, file => Assert.True(regenerated[file.Key] > file.Value, $"a stale entry module did not regenerate {file.Key}"));
        Assert.Empty(WorkshopWireTypeScriptTests.GeneratedTypeScriptViolations(copy.TypeScript(), copy.PathOf("schemas")));

        // Control: complete, current output with only empty staging folders left over is current and is not regenerated.
        Directory.CreateDirectory(copy.PathOf(TypeScriptStaging + "/curious-contraptions/wire"));
        copy.BuildTypeScript();
        Assert.Equal(regenerated, copy.TypeScriptWriteTimes());
        Assert.Empty(WorkshopWireTypeScriptTests.GeneratedTypeScriptViolations(copy.TypeScript(), copy.PathOf("schemas")));

        // A current entry cannot hide a lost leaf. Removing the entry permits ordinary regeneration.
        File.Delete(copy.PathOf(PuzzleRecordModule));
        Assert.True(File.Exists(copy.PathOf(TypeScriptEntry)));
        Assert.Contains("wire/puzzle-record.ts", RefusalLine(copy.TryBuildTypeScript(), "Missing generated TypeScript wire module"), StringComparison.Ordinal);
        File.Delete(copy.PathOf(TypeScriptEntry));
        copy.BuildTypeScript();
        Assert.Empty(WorkshopWireTypeScriptTests.GeneratedTypeScriptViolations(copy.TypeScript(), copy.PathOf("schemas")));
    }

    [Fact]
    public void AMissingCompiledWireLeafRegeneratesAnOtherwiseCurrentRuntime()
    {
        using var copy = WireBuildCopy.CreateAbstractions();
        copy.PrepareRuntimeCompile();
        var schema = copy.PathOf(OneSchema);
        var original = File.ReadAllText(schema);
        File.WriteAllText(schema, original + "\ntable S5cRetiredProbe { value: uint32; }\n");
        copy.BuildRuntimeTypeScript();
        var compiledEntry = copy.PathOf(".runtime-build/" + Path.ChangeExtension(TypeScriptEntry, ".js"));
        var compiledLeaf = copy.PathOf(".runtime-build/" + Path.ChangeExtension(PuzzleRecordModule, ".js"));
        var sourceEntry = File.GetLastWriteTimeUtc(copy.PathOf(TypeScriptEntry));
        Assert.True(File.Exists(compiledLeaf));
        File.Delete(compiledLeaf);
        Assert.True(File.Exists(compiledEntry));

        copy.BuildRuntimeTypeScript();

        Assert.True(File.Exists(compiledLeaf));
        Assert.Equal(sourceEntry, File.GetLastWriteTimeUtc(copy.PathOf(TypeScriptEntry)));

        File.Delete(compiledEntry);
        copy.BuildRuntimeTypeScript();
        Assert.True(File.Exists(compiledEntry));
        Assert.Equal(sourceEntry, File.GetLastWriteTimeUtc(copy.PathOf(TypeScriptEntry)));

        const string leafChange = "S5cLeafChange";
        File.AppendAllText(copy.PathOf(PuzzleRecordModule), $"\nexport const {leafChange} = 1;\n");
        copy.BuildRuntimeTypeScript();
        Assert.Contains(leafChange, File.ReadAllText(compiledLeaf), StringComparison.Ordinal);
        Assert.Equal(sourceEntry, File.GetLastWriteTimeUtc(copy.PathOf(TypeScriptEntry)));

        const string retired = ".runtime-build/" + TypeScriptFolder + "/curious-contraptions/wire/s5c-retired-probe.js";
        copy.LoadCompiledWireModule(retired);
        var retiredModule = File.ReadAllText(copy.PathOf(retired));
        File.WriteAllText(schema, original);
        copy.BuildRuntimeTypeScript();
        Assert.False(File.Exists(copy.PathOf(retired)));
        var neighbor = copy.PathOf(".runtime-build/tools/e2e/neighbor.js");
        File.Copy(compiledLeaf, neighbor);
        File.WriteAllText(copy.PathOf(retired), retiredModule);
        var compiledTime = File.GetLastWriteTimeUtc(compiledLeaf);
        Assert.True(File.Exists(compiledEntry));

        copy.BuildRuntimeTypeScript();

        Assert.False(File.Exists(copy.PathOf(retired)));
        Assert.True(File.Exists(neighbor));
        Assert.Equal(compiledTime, File.GetLastWriteTimeUtc(compiledLeaf));
        copy.LoadCompiledWireModule(".runtime-build/" + Path.ChangeExtension(PuzzleRecordModule, ".js"));
    }

    [Fact]
    public void ACurrentNpmMarkerStillRequiresThePinnedRootFlatBuffersInstall()
    {
        using var copy = WireBuildCopy.CreateAbstractions();
        copy.PrepareRuntimeCompile();
        copy.BuildRuntimeTypeScript();
        const string package = "node_modules/flatbuffers";
        var marker = File.GetLastWriteTimeUtc(copy.PathOf("node_modules/.package-lock.json"));
        var original = copy.PathOf("original-flatbuffers");
        Directory.Move(copy.PathOf(package), original);

        RefusalLine(copy.TryBuildRuntimeTypeScript(), "The pinned root FlatBuffers installation is missing");
        Directory.CreateDirectory(copy.PathOf(package));
        File.WriteAllText(copy.PathOf(package + "/package.json"), """{"version":"0.0.0"}""");
        Assert.Contains("0.0.0", RefusalLine(copy.TryBuildRuntimeTypeScript(), "must equal the declared pin"), StringComparison.Ordinal);
        Assert.Equal(marker, File.GetLastWriteTimeUtc(copy.PathOf("node_modules/.package-lock.json")));

        Directory.Delete(copy.PathOf(package), recursive: true);
        var sourceRuntime = Path.Combine(original, "js", "flatbuffers.js");
        File.Move(sourceRuntime, sourceRuntime + ".saved");
        Directory.CreateSymbolicLink(copy.PathOf(package), original);
        RefusalLine(copy.TryBuildRuntimeTypeScript(), "FlatBuffers installation resolves outside its canonical root path");
        Directory.Delete(copy.PathOf(package));
        File.Move(sourceRuntime + ".saved", sourceRuntime);
        Directory.Move(original, copy.PathOf(package));
        copy.BuildRuntimeTypeScript();
        Assert.Equal(marker, File.GetLastWriteTimeUtc(copy.PathOf("node_modules/.package-lock.json")));
    }

    [Fact]
    public void ALinkedNodeModulesRootRefusesStaleRestoreWithoutTouchingItsTarget()
    {
        using var copy = WireBuildCopy.CreateAbstractions();
        copy.PrepareRuntimeCompile();
        var local = copy.PathOf("node_modules");
        var foreign = Directory.CreateTempSubdirectory("wire-foreign-node-modules-");
        var installed = Path.Combine(foreign.FullName, "installed");
        try
        {
            Directory.Move(local, installed);
            var marker = Path.Combine(installed, ".package-lock.json");
            var stale = File.GetLastWriteTimeUtc(marker).AddDays(-1);
            File.SetLastWriteTimeUtc(marker, stale);
            var markerTime = File.GetLastWriteTimeUtc(marker);
            var sentinel = Path.Combine(installed, "fixture-sentinel");
            File.WriteAllText(sentinel, "foreign installation");
            Directory.CreateSymbolicLink(local, installed);

            RefusalLine(copy.TryBuildRuntimeTypeScript(), "Linked or non-directory owned path: node_modules");
            Assert.NotNull(new DirectoryInfo(local).LinkTarget);
            Assert.Equal("foreign installation", File.ReadAllText(sentinel));
            Assert.Equal(markerTime, File.GetLastWriteTimeUtc(marker));
            Assert.False(File.Exists(copy.PathOf(TypeScriptEntry)));
        }
        finally
        {
            if (new DirectoryInfo(local).LinkTarget is not null)
                Directory.Delete(local);
            foreign.Delete(recursive: true);
        }
    }

    [Fact]
    public void LinkedGeneratedFilesAndDirectoriesRefuseBeforeOfflineGeneration()
    {
        using var copy = WireBuildCopy.CreateAbstractions();
        copy.BuildTypeScript();
        var entry = copy.PathOf(TypeScriptEntry);
        File.SetLastWriteTimeUtc(entry, File.GetLastWriteTimeUtc(entry).AddDays(-1));
        var entryTime = File.GetLastWriteTimeUtc(entry);
        var leaf = copy.PathOf(PuzzleRecordModule);
        var saved = copy.PathOf("displaced-wire-leaf");
        File.Move(leaf, saved);
        File.CreateSymbolicLink(leaf, saved);
        Assert.Contains("puzzle-record.ts", RefusalLine(copy.TryBuildTypeScript(), "Linked generated wire source"), StringComparison.Ordinal);
        Assert.Equal(entryTime, File.GetLastWriteTimeUtc(entry));
        File.Delete(leaf);
        File.Move(saved, leaf);

        var published = copy.PathOf(TypeScriptFolder);
        var displaced = copy.PathOf("displaced-wire-directory");
        Directory.Move(published, displaced);
        Directory.CreateSymbolicLink(published, displaced);
        RefusalLine(copy.TryBuildTypeScript(), "Linked or non-directory owned path");
        Assert.True(File.Exists(Path.Combine(displaced, "curious-contraptions", "wire.ts")));
        Directory.Delete(published);
        Directory.Move(displaced, published);

        var stage = copy.PathOf(TypeScriptStaging);
        Directory.CreateDirectory(displaced);
        File.WriteAllText(Path.Combine(displaced, "fixture-sentinel"), "foreign staging");
        Directory.CreateSymbolicLink(stage, displaced);
        Assert.Contains("WireTypeScriptStaging", RefusalLine(copy.TryBuildTypeScript(), "Linked or non-directory owned path"), StringComparison.Ordinal);
        Assert.Equal("foreign staging", File.ReadAllText(Path.Combine(displaced, "fixture-sentinel")));
        Directory.Delete(stage);
        copy.BuildTypeScript();
        Assert.Empty(WorkshopWireTypeScriptTests.GeneratedTypeScriptViolations(copy.TypeScript(), copy.PathOf("schemas")));
    }

    [Fact]
    public void ALinkedGenerationStampRefusesFreshnessAndClean()
    {
        using var copy = WireBuildCopy.CreateAbstractions();
        copy.BuildTypeScript();
        var stamp = copy.PathOf(TypeScriptStamp);
        var foreign = copy.PathOf("foreign-generation-stamp");
        var sentinel = File.ReadAllText(stamp);
        File.Move(stamp, foreign);
        File.CreateSymbolicLink(stamp, foreign);

        Assert.Contains(".stamp", RefusalLine(copy.TryBuildTypeScript(), "Cannot use the wire generation stamp"), StringComparison.Ordinal);
        RefusalLine(copy.TryProjectTarget("Clean"), "Cannot use the wire generation stamp");
        Assert.Equal(foreign, new FileInfo(stamp).LinkTarget);
        Assert.Equal(sentinel, File.ReadAllText(foreign));

        File.Delete(stamp);
        File.Move(foreign, stamp);
        copy.BuildTypeScript(options: ["-p:ArtifactsPath=" + copy.PathOf("isolated artifacts")]);
    }

    [Theory]
    [InlineData(".runtime-build/tools/e2e", true)]
    [InlineData(".runtime-build/tools/e2e/.wire/curious-contraptions/wire/retired-probe.js", false)]
    [InlineData("CuriousContraptions.web/wwwroot/pose-ring.js", false)]
    public void LinkedRuntimeOutputsRefuseReuseReconciliationAndClean(string relativePath, bool directory)
    {
        using var copy = WireBuildCopy.CreateAbstractions();
        copy.PrepareRuntimeCompile();
        copy.BuildRuntimeTypeScript();
        var owned = copy.PathOf(relativePath);
        var foreign = copy.PathOf("foreign-runtime-output");
        string sentinelPath;
        if (directory)
        {
            Directory.Move(owned, foreign);
            Directory.CreateSymbolicLink(owned, foreign);
            sentinelPath = Path.Combine(foreign, "fixture-sentinel");
        }
        else
        {
            File.Delete(owned);
            File.CreateSymbolicLink(owned, foreign);
            sentinelPath = foreign;
        }
        const string sentinel = "foreign runtime output";
        File.WriteAllText(sentinelPath, sentinel);

        RefusalLine(copy.TryBuildRuntimeTypeScript(), "Cannot use linked or invalid runtime output");
        RefusalLine(copy.TryProjectTarget("ReconcileCompiledWireTypeScript"), "Cannot use linked or invalid runtime output");
        RefusalLine(copy.TryProjectTarget("Clean"), "Cannot use linked or invalid runtime output");
        Assert.Equal(foreign, new FileInfo(owned).LinkTarget);
        Assert.Equal(sentinel, File.ReadAllText(sentinelPath));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WrongKindEmittedWirePathsRefuseReuseAdmissionAndClean(bool parent)
    {
        using var copy = WireBuildCopy.CreateAbstractions();
        copy.PrepareRuntimeCompile();
        copy.BuildRuntimeTypeScript();
        var relativeLeaf = ".runtime-build/" + Path.ChangeExtension(PuzzleRecordModule, ".js");
        var leaf = copy.PathOf(relativeLeaf);
        var path = parent ? Path.GetDirectoryName(leaf)! : leaf;
        var displaced = copy.PathOf("displaced-wire-output");
        var sentinel = PrepareWrongKindOutput(path, displaced, parent);
        var stamp = copy.PathOf(TypeScriptStamp);
        var stampContents = File.ReadAllText(stamp);
        var stampTime = File.GetLastWriteTimeUtc(stamp);
        var diagnostic = parent ? "Linked or non-directory owned path" : "Linked or non-file owned path";

        foreach (var target in new[] { "BuildRuntimeTypeScript", "AdmitRuntimeTypeScriptArtifacts", "ReconcileCompiledWireTypeScript", "Clean" })
        {
            var refusal = RefusalLine(copy.TryProjectTarget(target), "Cannot use linked or invalid runtime output");
            Assert.Contains(diagnostic + ": " + path, refusal, StringComparison.Ordinal);
            Assert.Equal("wrong-kind output sentinel", File.ReadAllText(sentinel));
        }

        Assert.Equal(stampContents, File.ReadAllText(stamp));
        Assert.Equal(stampTime, File.GetLastWriteTimeUtc(stamp));

        if (parent)
        {
            File.Delete(path);
            Directory.Move(displaced, path);
            File.Delete(leaf);
        }
        else
        {
            Directory.Delete(path, recursive: true);
        }
        copy.BuildRuntimeTypeScript();
        copy.LoadCompiledWireModule(relativeLeaf);
    }

    private static string PrepareWrongKindOutput(string path, string displaced, bool parent)
    {
        if (parent)
        {
            Directory.Move(path, displaced);
            File.WriteAllText(path, "wrong-kind output sentinel");
            return path;
        }

        File.Move(path, displaced);
        Directory.CreateDirectory(path);
        var sentinel = Path.Combine(path, "sentinel");
        File.WriteAllText(sentinel, "wrong-kind output sentinel");
        return sentinel;
    }

    [Fact]
    public void RuntimePathsAndAdmissionIgnoreGlobalAndInheritedRedirections()
    {
        using var copy = WireBuildCopy.CreateAbstractions("s5c|runtime checkout ");
        copy.PrepareRuntimeCompile();
        File.WriteAllText(copy.PathOf("tools/runtime/pipe|proof.ts"), "export const probe = 1;\n");
        var decoy = Directory.CreateDirectory(copy.PathOf("foreign paths")).FullName + "/";
        var sentinel = Path.Combine(decoy, "sentinel");
        File.WriteAllText(sentinel, "foreign paths sentinel");
        string[] paths =
        [
            "RuntimeTypeScriptRoot", "WireTypeScriptDirectory", "_WireOwnedIntermediateRoot", "_RuntimeTypeScriptOutput",
            "_CompiledWireDirectory", "_RuntimeTypeScriptPublished", "_WireIntermediatePath", "_WireBaseIntermediatePath",
            "_WireOwnedBaseIntermediateRoot", "_RuntimeRestoreMarker", "_WireTypeScriptStaging",
            "_WireTypeScriptStamp", "_WireTypeScriptEntry", "_WireTypeScriptTargets", "_FlatcCompilerTargets", "_WireValidationDirectory"
        ];
        var redirectedBuild = copy.TryProjectTarget("BuildRuntimeTypeScript", paths.Select(name => $"-p:{name}={decoy}").ToArray());
        Assert.True(redirectedBuild.ExitCode == 0, redirectedBuild.Report);
        Assert.True(File.Exists(copy.PathOf(TypeScriptEntry)));
        Assert.True(File.Exists(copy.PathOf(TypeScriptStamp)));
        Assert.True(File.Exists(copy.PathOf("CuriousContraptions.web/wwwroot/pose-ring.js")));
        copy.LoadCompiledWireModule(".runtime-build/" + Path.ChangeExtension(PuzzleRecordModule, ".js"));
        copy.LoadCompiledWireModule(".runtime-build/tools/runtime/pipe|proof.js");

        var inherited = new Dictionary<string, string> { ["_WireValidationDirectory"] = decoy };
        var admission = copy.TryProjectTarget("AdmitRuntimeTypeScriptArtifacts",
            ["-getProperty:AdmittedWireSourceCount", "-getTargetResult:AdmitRuntimeTypeScriptArtifacts", "-p:AdmittedWireSourceCount=1"], inherited);
        Assert.True(admission.ExitCode == 0, admission.Report);
        using var result = JsonDocument.Parse(admission.Output);
        var targetResult = result.RootElement.GetProperty("TargetResults").GetProperty("AdmitRuntimeTypeScriptArtifacts");
        Assert.Equal(TypeScriptEntry, targetResult.GetProperty("Items")[0].GetProperty("Identity").GetString());
        var count = result.RootElement.GetProperty("Properties").GetProperty("AdmittedWireSourceCount").GetString();
        Assert.Equal(targetResult.GetProperty("Items").GetArrayLength().ToString(System.Globalization.CultureInfo.InvariantCulture), count);
        Assert.NotEqual("1", count);
        Assert.Equal([sentinel], Directory.GetFileSystemEntries(decoy));
        Assert.Equal("foreign paths sentinel", File.ReadAllText(sentinel));
    }

    public enum StandaloneWireOutput { CSharpSource, CSharpStamp, PublishedTypeScript, StagedTypeScript, TypeScriptStamp }

    [Theory]
    [InlineData(StandaloneWireOutput.CSharpSource, false)]
    [InlineData(StandaloneWireOutput.CSharpSource, true)]
    [InlineData(StandaloneWireOutput.CSharpStamp, false)]
    [InlineData(StandaloneWireOutput.CSharpStamp, true)]
    [InlineData(StandaloneWireOutput.PublishedTypeScript, false)]
    [InlineData(StandaloneWireOutput.PublishedTypeScript, true)]
    [InlineData(StandaloneWireOutput.StagedTypeScript, false)]
    [InlineData(StandaloneWireOutput.StagedTypeScript, true)]
    [InlineData(StandaloneWireOutput.TypeScriptStamp, false)]
    [InlineData(StandaloneWireOutput.TypeScriptStamp, true)]
    public void StandalonePreflightRefusesWrongKindBeforeAnyWrite(StandaloneWireOutput output, bool parent)
    {
        var csharp = output is StandaloneWireOutput.CSharpSource or StandaloneWireOutput.CSharpStamp;
        using var copy = csharp ? WireBuildCopy.Create() : WireBuildCopy.CreateAbstractions();
        var path = copy.PathOf(output switch
        {
            StandaloneWireOutput.CSharpSource => GenerationFolder + "/CuriousContraptions/Wire/PuzzleRecord.cs",
            StandaloneWireOutput.CSharpStamp => GenerationFolder + "/.generated",
            StandaloneWireOutput.PublishedTypeScript => PuzzleRecordModule,
            StandaloneWireOutput.StagedTypeScript => TypeScriptStaging + "/curious-contraptions/wire/puzzle-record.ts",
            StandaloneWireOutput.TypeScriptStamp => TypeScriptStamp,
            _ => throw new ArgumentOutOfRangeException(nameof(output))
        });
        if (parent)
            path = Path.GetDirectoryName(path)!;
        var sentinel = PutWrongKindOutput(path, parent);
        var diagnostic = parent ? "Linked or non-directory owned path" : "Linked or non-file owned path";
        var offline = copy.OfflineEnvironment();
        string[] targets = csharp ? ["GenerateWireAccessors", "Clean"] : ["GenerateWireTypeScript", "BuildRuntimeTypeScript", "Clean"];

        foreach (var target in targets)
        {
            var refusal = RefusalLine(copy.TryProjectTarget(target, environment: offline), diagnostic);
            Assert.Contains(path, refusal, StringComparison.Ordinal);
            Assert.Equal("wrong-kind standalone sentinel", File.ReadAllText(sentinel));
            Assert.False(File.Exists(copy.PathOf(TypeScriptEntry)));
            Assert.False(File.Exists(copy.PathOf(TypeScriptStamp)));
            Assert.False(File.Exists(copy.PathOf(GenerationFolder + "/.generated")));
            Assert.False(Directory.Exists(copy.PathOf("node_modules")));
        }
    }

    private static string PutWrongKindOutput(string path, bool parent)
    {
        var sentinel = parent ? path : Path.Combine(path, "sentinel");
        Directory.CreateDirectory(Path.GetDirectoryName(sentinel)!);
        File.WriteAllText(sentinel, "wrong-kind standalone sentinel");
        return sentinel;
    }

    [Fact]
    public void ADirectoryAtTheNpmMarkerRefusesBeforeRestoreFreshness()
    {
        using var copy = WireBuildCopy.CreateAbstractions();
        foreach (var manifest in new[] { "package.json", "package-lock.json" })
            File.Copy(Path.Combine(Root, manifest), copy.PathOf(manifest));
        var marker = copy.PathOf("node_modules/.package-lock.json");
        var sentinel = PutWrongKindOutput(marker, parent: false);

        var refusal = RefusalLine(copy.TryProjectTarget("RestoreRuntimeTypeScript", environment: copy.OfflineEnvironment()),
            "Cannot use repository node_modules");
        Assert.Contains("Linked or non-file owned path: " + marker, refusal, StringComparison.Ordinal);
        Assert.Equal("wrong-kind standalone sentinel", File.ReadAllText(sentinel));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void StandaloneGeneratorsPreserveSdkArtifactLayoutsWithoutNode(bool literalPipe, bool mixed)
    {
        using var csharp = WireBuildCopy.Create(literalPipe ? "wire|csharp checkout " : "wire-csharp-checkout-");
        using var typescript = WireBuildCopy.CreateAbstractions(literalPipe ? "wire|typescript checkout " : "wire-ts-checkout-");
        var artifacts = Directory.CreateTempSubdirectory(literalPipe ? "wire|external artifacts " : "wire-artifacts-");
        try
        {
            var redirect = "-p:ArtifactsPath=" + artifacts.FullName;
            string[] options = mixed ? [redirect, "-p:UseArtifactsIntermediateOutput=false"] : [redirect];
            AssertCSharpOfflineLayout(csharp, options, mixed ? csharp.PathOf(ProjectDirectory) : artifacts.FullName);
            AssertTypeScriptOfflineLayout(typescript, options, mixed ? typescript.PathOf("CuriousContraptions.Abstractions") : artifacts.FullName);
        }
        finally
        {
            artifacts.Delete(recursive: true);
        }
    }

    [Fact]
    public void BothOfflineGeneratorsPreserveLiteralDollarArguments()
    {
        using var csharp = WireBuildCopy.Create("wire$literal csharp ");
        using var typescript = WireBuildCopy.CreateAbstractions("wire$literal typescript ");
        var artifacts = Directory.CreateTempSubdirectory("wire$literal artifacts ");
        try
        {
            string[] options = ["-p:ArtifactsPath=" + artifacts.FullName];
            AssertCSharpOfflineLayout(csharp, options, artifacts.FullName);
            AssertTypeScriptOfflineLayout(typescript, options, artifacts.FullName);
        }
        finally
        {
            artifacts.Delete(recursive: true);
        }
    }

    [Fact]
    public void RuntimeCompilerPreservesLiteralDollarCheckout()
    {
        using var copy = WireBuildCopy.CreateAbstractions("wire$literal runtime ");
        copy.PrepareRuntimeCompile();

        copy.BuildRuntimeTypeScript();

        copy.LoadCompiledWireModule(".runtime-build/" + Path.ChangeExtension(PuzzleRecordModule, ".js"));
    }

    private static void AssertCSharpOfflineLayout(WireBuildCopy copy, string[] options, string expectedRoot)
    {
        var paths = copy.Properties(["IntermediateOutputPath", "MSBuildProjectDirectory"], options);
        var intermediate = Path.Combine(paths["MSBuildProjectDirectory"], paths["IntermediateOutputPath"].Replace('\\', Path.DirectorySeparatorChar));
        Assert.StartsWith(expectedRoot + Path.DirectorySeparatorChar, intermediate, StringComparison.Ordinal);
        var offline = copy.OfflineEnvironment();
        AssertGeneratedOffline(copy, "GenerateWireAccessors", options, offline);

        var generated = Path.Combine(intermediate, "WireAccessors");
        var leaf = Path.Combine(generated, "CuriousContraptions", "Wire", "PuzzleRecord.cs");
        Assert.True(File.Exists(Path.Combine(generated, ".generated")));
        Assert.True(File.Exists(leaf));
        File.Delete(leaf);
        AssertGeneratedOffline(copy, "GenerateWireAccessors", options, offline);
        var names = Directory.GetFiles(generated, "*.cs", SearchOption.AllDirectories).Select(Path.GetFileNameWithoutExtension);
        Assert.Equal(copy.SchemaTypes().Order(StringComparer.Ordinal), names.Order(StringComparer.Ordinal));
    }

    private static void AssertTypeScriptOfflineLayout(WireBuildCopy copy, string[] options, string expectedRoot)
    {
        var paths = copy.Properties(["BaseIntermediateOutputPath", "MSBuildProjectDirectory"], options);
        var intermediate = Path.Combine(paths["MSBuildProjectDirectory"], paths["BaseIntermediateOutputPath"].Replace('\\', Path.DirectorySeparatorChar));
        Assert.StartsWith(expectedRoot + Path.DirectorySeparatorChar, intermediate, StringComparison.Ordinal);
        var offline = copy.OfflineEnvironment();
        AssertGeneratedOffline(copy, "GenerateWireTypeScript", options, offline);
        Assert.True(File.Exists(Path.Combine(intermediate, "WireTypeScript", ".stamp")));
        File.Delete(copy.PathOf(TypeScriptEntry));
        AssertGeneratedOffline(copy, "GenerateWireTypeScript", options, offline);
        Assert.Empty(WorkshopWireTypeScriptTests.GeneratedTypeScriptViolations(copy.TypeScript(), copy.PathOf("schemas")));
    }

    private static void AssertGeneratedOffline(WireBuildCopy copy, string target, string[] options, IReadOnlyDictionary<string, string> offline)
    {
        var result = copy.TryProjectTarget(target, options, offline);
        Assert.True(result.ExitCode == 0, result.Report);
        Assert.False(Directory.Exists(copy.PathOf("node_modules")));
    }

    [Fact]
    public void LinkedCSharpAccessorOutputRefusesReuseAndClean()
    {
        using var copy = WireBuildCopy.Create();
        copy.Build();
        var source = Directory.EnumerateFiles(copy.PathOf(GenerationFolder), "*.cs", SearchOption.AllDirectories).First();
        var foreign = copy.PathOf("foreign-csharp-accessor");
        var sentinel = File.ReadAllText(source);
        File.Move(source, foreign);
        File.CreateSymbolicLink(source, foreign);

        RefusalLine(copy.TryBuild(), "Cannot use linked or invalid generated C# wire output");
        RefusalLine(copy.TryProjectTarget("Clean"), "Cannot use linked or invalid generated C# wire output");
        Assert.Equal(foreign, new FileInfo(source).LinkTarget);
        Assert.Equal(sentinel, File.ReadAllText(foreign));
    }

    [Fact]
    public void RetiredArtifactsRefuseBeforeTheGenerationWriter()
    {
        using var copy = WireBuildCopy.CreateAbstractions();
        string[] paths = [TypeScriptFolder + "/schemas.txt", "tools/e2e/package.json", "tools/e2e/package-lock.json"];
        foreach (var path in paths)
        {
            var retired = copy.PathOf(path);
            Directory.CreateDirectory(Path.GetDirectoryName(retired)!);
            File.WriteAllText(retired, "obsolete");
            var stamp = File.GetLastWriteTimeUtc(retired);
            Assert.Contains(retired, RefusalLine(copy.TryBuildTypeScript(), "Retired wire artifact"), StringComparison.Ordinal);
            Assert.Equal(stamp, File.GetLastWriteTimeUtc(retired));
            Assert.False(File.Exists(copy.PathOf(TypeScriptEntry)));
            Assert.False(Directory.Exists(copy.PathOf(TypeScriptStaging)));
            File.Delete(retired);
        }

        var extra = copy.PathOf("schemas/second.fbs");
        File.WriteAllText(extra, "table Extra {}\n");
        Assert.Contains("second.fbs", RefusalLine(copy.TryBuildTypeScript(), "Only the canonical wire schema"), StringComparison.Ordinal);
        Assert.False(File.Exists(copy.PathOf(TypeScriptEntry)));
        Assert.False(Directory.Exists(copy.PathOf(TypeScriptStaging)));
        File.Delete(extra);
        copy.BuildTypeScript();
        Assert.True(File.Exists(copy.PathOf(TypeScriptEntry)));
    }

    [Fact]
    public void RuntimeBuildRefusesIncompleteAndStaleGeneratedSourcesWithACurrentEntry()
    {
        using var copy = WireBuildCopy.CreateAbstractions();
        copy.PrepareRuntimeCompile();
        copy.BuildRuntimeTypeScript();
        var entry = copy.PathOf(TypeScriptEntry);
        var leaf = copy.PathOf(PuzzleRecordModule);
        var currentEntry = File.ReadAllText(entry);
        var currentTime = File.GetLastWriteTimeUtc(leaf);

        File.SetLastWriteTimeUtc(leaf, currentTime.AddDays(-1));
        try { RefusalLine(copy.TryBuildRuntimeTypeScript(), "is older than its generation inputs"); }
        finally { File.SetLastWriteTimeUtc(leaf, currentTime); }

        File.WriteAllLines(entry, File.ReadLines(entry).Where(line => !line.Contains("wire/puzzle-record.js", StringComparison.Ordinal)).ToArray());
        try { RefusalLine(copy.TryBuildRuntimeTypeScript(), "exports do not match the canonical schema"); }
        finally { File.WriteAllText(entry, currentEntry); }

        File.AppendAllText(entry, "\n  export { PuzzleRecord as Extra } from './wire/puzzle-record.js';\n");
        try { RefusalLine(copy.TryBuildRuntimeTypeScript(), "exports do not match the canonical schema"); }
        finally { File.WriteAllText(entry, currentEntry); }

        var stray = Path.Combine(Path.GetDirectoryName(leaf)!, "unlisted.ts");
        File.Copy(leaf, stray);
        RefusalLine(copy.TryBuildRuntimeTypeScript(), "Unlisted generated TypeScript wire module");
        Assert.True(File.Exists(stray));
        File.Delete(stray);
        copy.BuildRuntimeTypeScript();
    }

    [Fact]
    public void RootRuntimeAdmissionRequiresTheActualLocalCompilerEvenWithTscOnPath()
    {
        using var copy = WireBuildCopy.CreateAbstractions();
        copy.PrepareRuntimeCompile();
        Assert.True(File.Exists(Path.Combine(Root, "node_modules", ".bin", "tsc")));
        var local = copy.PathOf("node_modules/typescript");
        var original = copy.PathOf("original-typescript");
        Directory.Move(local, original);
        RefusalLine(copy.TryBuildRuntimeTypeScript(), "local TypeScript compiler node_modules/typescript/bin/tsc is missing");
        Directory.CreateSymbolicLink(local, original);
        RefusalLine(copy.TryBuildRuntimeTypeScript(), "local TypeScript compiler node_modules/typescript/bin/tsc resolves outside its canonical root path");
        Directory.Delete(local);
        Directory.Move(original, local);

        var manifest = copy.PathOf("package.json");
        var declared = File.ReadAllText(manifest);
        using var rootManifest = JsonDocument.Parse(declared);
        var actualPin = rootManifest.RootElement.GetProperty("devDependencies").GetProperty("typescript").GetString()!;
        File.WriteAllText(manifest, declared.Replace(actualPin, "0.0.0", StringComparison.Ordinal));
        copy.KeepRestoreMarkerCurrent();
        Assert.Contains($"reports Version {actualPin}, expected Version 0.0.0",
            RefusalLine(copy.TryBuildRuntimeTypeScript(), "local TypeScript compiler reports"), StringComparison.Ordinal);
        File.WriteAllText(manifest, declared);
        copy.KeepRestoreMarkerCurrent();
        copy.BuildRuntimeTypeScript();
    }

    [Theory]
    [InlineData("tools/runtime")]
    [InlineData("tools/e2e")]
    [InlineData("tools/e2e/.wire/curious-contraptions/wire")]
    [InlineData(".runtime-build/tools/runtime")]
    [InlineData(".runtime-build/tools/e2e/.wire/curious-contraptions/wire")]
    public void AFlatBuffersShadowRefusesRuntimeCompileAdmission(string importer)
    {
        using var copy = WireBuildCopy.CreateAbstractions();
        Directory.CreateDirectory(copy.PathOf("node_modules/flatbuffers"));
        File.Copy(Path.Combine(Root, "node_modules/flatbuffers/package.json"), copy.PathOf("node_modules/flatbuffers/package.json"));
        copy.AssertNoFlatBuffersShadow();

        var shadow = importer + "/node_modules/flatbuffers";
        Directory.CreateDirectory(copy.PathOf(shadow));
        Assert.Contains(copy.PathOf(shadow), RefusalLine(copy.TryBuildRuntimeTypeScript(), "shadows the pinned root package"), StringComparison.Ordinal);
        Assert.False(File.Exists(copy.PathOf(TypeScriptEntry)));
    }

    [Fact]
    public void ARemovedSchemaTypeLeavesNoGeneratedSource()
    {
        using var copy = WireBuildCopy.Create();
        var schema = copy.PathOf("schemas/workshop_wire.fbs");
        var original = File.ReadAllText(schema);
        File.WriteAllText(schema, original + "\ntable S5RemovedProbe {\n    value: uint32;\n}\n");
        copy.Build();
        Assert.Contains("S5RemovedProbe", copy.GeneratedTypeNames());
        Assert.Contains("S5RemovedProbe", DeclaredWireTypes(copy.PathOf(Assembly)));

        File.WriteAllText(schema, original);
        copy.Build();

        Assert.DoesNotContain("S5RemovedProbe", copy.GeneratedTypeNames());
        Assert.DoesNotContain("S5RemovedProbe", DeclaredWireTypes(copy.PathOf(Assembly)));
    }

    // T4: the removed table is appended after every declaration of the one schema.
    [Fact]
    public void ARemovedSchemaTypeLeavesNoTypeScript()
    {
        using var copy = WireBuildCopy.CreateAbstractions();
        var schema = copy.PathOf("schemas/workshop_wire.fbs");
        var original = File.ReadAllText(schema);
        File.WriteAllText(schema, original + "\ntable\n    S5bRemovedProbe { value: uint32; } table S5bInlineProbe { value: uint32; }\n");
        copy.BuildTypeScript();
        Assert.Contains("S5bRemovedProbe", copy.TypeScriptExports());
        Assert.Contains("S5bInlineProbe", copy.TypeScriptExports());
        Assert.Single(copy.TypeScript(), file => file.Value.Contains("export class S5bRemovedProbe implements flatbuffers.IUnpackableObject<S5bRemovedProbeT> {", StringComparison.Ordinal));

        File.WriteAllText(schema, original);
        copy.BuildTypeScript();

        Assert.DoesNotContain("S5bRemovedProbe", copy.TypeScriptExports());
        Assert.DoesNotContain("S5bInlineProbe", copy.TypeScriptExports());
        Assert.DoesNotContain(copy.TypeScript(), file => file.Key.Contains("removed-probe", StringComparison.Ordinal) || file.Value.Contains("S5bRemovedProbe", StringComparison.Ordinal));
        Assert.Empty(WorkshopWireTypeScriptTests.GeneratedTypeScriptViolations(copy.TypeScript(), copy.PathOf("schemas")));
    }

    // TypeScript outside the wire namespace's module is refused before any is replaced: a second namespace appended to the one
    // schema file makes the compiler write a second module, and the previous output stays as it was. The appended text leaves
    // the closed grammar, so the case asserts on the refusal line, never through the one schema reader. Control: every other
    // case builds a complete module set from the one file.
    [Fact]
    public void TypeScriptOutsideTheWireNamespaceRefusesTheBuild()
    {
        using var copy = WireBuildCopy.CreateAbstractions();
        copy.BuildTypeScript();
        var generated = copy.TypeScriptWriteTimes();
        var schema = copy.PathOf(OneSchema);
        var originalSchema = File.ReadAllText(schema);
        var originalWriteTime = File.GetLastWriteTimeUtc(schema);
        File.AppendAllText(schema, "\nnamespace S5bOther;\n\ntable S5bOtherRecord {\n    value: uint32;\n}\n");

        var stray = RefusalLine(copy.TryBuildTypeScript(), "wrote TypeScript outside the wire namespace's module");

        Assert.Contains("s5b-other", stray, StringComparison.Ordinal);
        Assert.Equal(generated, copy.TypeScriptWriteTimes());
        File.WriteAllText(schema, originalSchema);
        File.SetLastWriteTimeUtc(schema, originalWriteTime);
        copy.BuildTypeScript();
        Assert.Equal(generated, copy.TypeScriptWriteTimes());
        Assert.Empty(WorkshopWireTypeScriptTests.GeneratedTypeScriptViolations(copy.TypeScript(), copy.PathOf("schemas")));
    }

    // Both generation commands pass the one schema file by its quoted full path, so a checkout path with a space (and of any
    // length) builds like any other. Control: every other case builds in a path without one.
    [Fact]
    public void ACheckoutPathWithASpaceBuildsACompleteModuleSet()
    {
        using var copy = WireBuildCopy.CreateAbstractions("s5b wire build ");
        Assert.Contains(" ", copy.PathOf("schemas"), StringComparison.Ordinal);

        copy.BuildTypeScript();

        Assert.Empty(WorkshopWireTypeScriptTests.GeneratedTypeScriptViolations(copy.TypeScript(), copy.PathOf("schemas")));
    }

    private static string RefusalLine(ChildResult result, string phrase)
    {
        Assert.True(result.ExitCode != 0, $"the build was not refused:\n{result.Report}");
        return Assert.Single(result.Output.Split('\n').Where(line => line.Contains(phrase, StringComparison.Ordinal)).Distinct());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ABadDigestAtTheCanonicalCompilerLeafRefusesGeneration(bool typescript)
    {
        using var copy = typescript ? WireBuildCopy.CreateAbstractions() : WireBuildCopy.Create();
        var redirect = "-p:FlatcCacheRoot=" + copy.PathOf("isolated cache");
        var compiler = copy.Properties(["FlatcPath", "FlatcCacheDirectory"], redirect)["FlatcPath"];
        Directory.CreateDirectory(Path.GetDirectoryName(compiler)!);
        const string invalidCompiler = "cache fixture with a mismatched digest";
        File.WriteAllText(compiler, invalidCompiler);
        var originalTime = File.GetLastWriteTimeUtc(compiler);
        var actualDigest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(invalidCompiler)));

        var result = typescript ? copy.TryBuildTypeScript(options: [redirect]) : copy.TryBuild(redirect);
        var refusal = RefusalLine(result, "The cached FlatBuffers compiler");
        Assert.Contains(compiler, refusal, StringComparison.Ordinal);
        Assert.Contains(actualDigest, refusal, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"the pinned digest is {copy.PinnedCompilerDigest}", refusal, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("It was not run or replaced.", refusal, StringComparison.Ordinal);
        Assert.Equal(invalidCompiler, File.ReadAllText(compiler));
        Assert.Equal(originalTime, File.GetLastWriteTimeUtc(compiler));
        Assert.False(File.Exists(copy.PathOf(IntermediateAssembly)));
        Assert.False(File.Exists(copy.PathOf(Assembly)));
        Assert.False(Directory.Exists(copy.PathOf(TypeScriptFolder)));
    }

    public enum SdkAccessorLayout { Default, ExternalArtifacts, MixedArtifacts }

    [Theory]
    [InlineData(SdkAccessorLayout.Default)]
    [InlineData(SdkAccessorLayout.ExternalArtifacts)]
    [InlineData(SdkAccessorLayout.MixedArtifacts)]
    public void ANonIncrementalBuildRegeneratesAndCompiles(SdkAccessorLayout layout)
    {
        using var copy = WireBuildCopy.Create();
        var artifacts = layout == SdkAccessorLayout.Default ? null : Directory.CreateTempSubdirectory("wire-sdk-artifacts-");
        try
        {
            string[] options = artifacts is null ? [] : ["-p:ArtifactsPath=" + artifacts.FullName];
            if (layout == SdkAccessorLayout.MixedArtifacts)
                options = [.. options, "-p:UseArtifactsIntermediateOutput=false"];
            AssertSdkAccessorRebuild(copy, options);
        }
        finally
        {
            artifacts?.Delete(recursive: true);
        }
    }

    private static void AssertSdkAccessorRebuild(WireBuildCopy copy, string[] options)
    {
        var paths = copy.Properties(["IntermediateOutputPath", "OutputPath"], options);
        var intermediate = copy.PathOf(Path.Combine(ProjectDirectory, paths["IntermediateOutputPath"].Replace('\\', '/')));
        var generated = Path.Combine(intermediate, "WireAccessors");
        var assembly = copy.PathOf(Path.Combine(ProjectDirectory, paths["OutputPath"].Replace('\\', '/'), "CuriousContraptions.FlatBuffers.dll"));

        copy.Build(options);
        var first = copy.GeneratedSources(generated);
        var writes = File.ReadAllLines(Path.Combine(intermediate, "CuriousContraptions.FlatBuffers.csproj.FileListAbsolute.txt"));
        Assert.Contains(writes, path => path.EndsWith("/WireAccessors/.generated", StringComparison.Ordinal));
        Assert.All(first.Keys, leaf => Assert.Contains(writes, path => path.EndsWith("/WireAccessors/" + leaf, StringComparison.Ordinal)));
        copy.Build(options);
        Assert.Equal(first, copy.GeneratedSources(generated));

        copy.Build([.. options, "--no-incremental"]);

        var rebuilt = copy.GeneratedSources(generated);
        Assert.Equal(first.Keys.Order(StringComparer.Ordinal), rebuilt.Keys.Order(StringComparer.Ordinal));
        Assert.All(first, source => Assert.True(rebuilt[source.Key] > source.Value, $"{source.Key} was not regenerated"));
        Assert.Empty(copy.SchemaTypes().Except(DeclaredWireTypes(assembly)));
        var clean = copy.TryProjectTarget("Clean", options);
        Assert.True(clean.ExitCode == 0, clean.Report);
        Assert.False(File.Exists(Path.Combine(generated, ".generated")));
        Assert.Empty(copy.GeneratedSources(generated));
    }

    // The generated sources keep the compiler's ENABLE_SPAN_T alternatives; the build refuses the define before it generates
    // or compiles anything, wherever it stands in a list the compiler splits on ';', ',' or whitespace. The define is set in
    // the copied project file, between other defines (the SDK's own defines follow it), with each separator before or after.
    [Theory]
    [InlineData("$(DefineConstants);S5_PROBE;ENABLE_SPAN_T")]
    [InlineData("$(DefineConstants);S5_PROBE ENABLE_SPAN_T")]
    [InlineData("$(DefineConstants);S5_PROBE,ENABLE_SPAN_T")]
    [InlineData("$(DefineConstants);S5_PROBE;ENABLE_SPAN_T,S5_OTHER")]
    [InlineData("$(DefineConstants);S5_PROBE;ENABLE_SPAN_T S5_OTHER")]
    public void ASpanAccessorDefineRefusesTheBuild(string defines)
    {
        using var copy = WireBuildCopy.Create();
        copy.SetDefines(defines);

        AssertSpanAccessorDefineRefused(copy, copy.TryBuild());
    }

    // The whole DefineConstants value: the global property replaces the project's defines, and DisableImplicitFrameworkDefines
    // stops the SDK appending its framework defines before compile, so the symbol is both first and last. The message
    // prints the value the refusal read.
    [Fact]
    public void ASpanAccessorDefineAsTheWholeDefineListRefusesTheBuild()
    {
        using var copy = WireBuildCopy.Create();

        var result = copy.TryBuild("-p:DefineConstants=ENABLE_SPAN_T", "-p:DisableImplicitFrameworkDefines=true");

        AssertSpanAccessorDefineRefused(copy, result);
        Assert.Contains("in DefineConstants (ENABLE_SPAN_T).", result.Output, StringComparison.Ordinal);
    }

    private static void AssertSpanAccessorDefineRefused(WireBuildCopy copy, ChildResult result)
    {
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("built with ENABLE_SPAN_T in DefineConstants", result.Output, StringComparison.Ordinal);
        Assert.Contains("The nullable ArraySegment accessors are the only wire surface", result.Output, StringComparison.Ordinal);
        Assert.False(Directory.Exists(copy.PathOf(GenerationFolder)));
        Assert.False(File.Exists(copy.PathOf(IntermediateAssembly)));
        Assert.False(File.Exists(copy.PathOf(Assembly)));
    }

    // Control for the refusal: a define that only contains the Span accessor symbol, with a character after or before it, is
    // another symbol, and the build generates and compiles.
    [Theory]
    [InlineData("$(DefineConstants);S5_PROBE;ENABLE_SPAN_TX")]
    [InlineData("$(DefineConstants);S5_PROBE;S5_ENABLE_SPAN_T")]
    public void ADefineThatOnlyContainsTheSpanAccessorSymbolBuilds(string defines)
    {
        using var copy = WireBuildCopy.Create();
        copy.SetDefines(defines);

        copy.Build();

        Assert.True(File.Exists(copy.PathOf(Assembly)));
        Assert.NotEmpty(copy.GeneratedTypeNames());
    }

    [Fact]
    public void AnUnsupportedBuildHostRefusesProvisioning()
    {
        using var copy = WireBuildCopy.Create();
        var result = copy.TryProvisionOnUnsupportedHost();

        RefusalLine(result, $"has no release asset for this build host ({RuntimeInformation.OSDescription}, Arm).");
        Assert.False(File.Exists(copy.PathOf(IntermediateAssembly)));
        Assert.False(File.Exists(copy.PathOf(Assembly)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BothGeneratorsUseTheSharedCompilerPinAndReceivingGuard(bool typescript)
    {
        using var copy = typescript ? WireBuildCopy.CreateAbstractions() : WireBuildCopy.Create();
        string[] pins = ["FlatcVersion", "FlatcHostArchitecture", "FlatcAsset", "FlatcPath", "FlatcCacheDirectory",
            "FlatcArchiveSha256", "FlatcBinarySha256"];
        var fixedPins = copy.Properties(pins);
        var attemptedOverrides = pins.Select(property => $"-p:{property}=foreign").ToArray();
        Assert.Equal(fixedPins, copy.Properties(pins, attemptedOverrides));

        var cacheRoot = copy.PathOf("isolated cache");
        var redirect = "-p:FlatcCacheRoot=" + cacheRoot;
        var cache = copy.Properties(["FlatcCacheDirectory", "FlatcPath"], redirect)["FlatcCacheDirectory"].TrimEnd('/');
        Directory.CreateDirectory(Path.GetDirectoryName(cache)!);
        var foreign = Directory.CreateDirectory(copy.PathOf("foreign receiving directory")).FullName;
        var sentinel = Path.Combine(foreign, "fixture-sentinel");
        File.WriteAllText(sentinel, "foreign compiler cache");
        Directory.CreateSymbolicLink(cache, foreign);

        var result = typescript ? copy.TryBuildTypeScript(options: [redirect]) : copy.TryBuild(redirect);
        RefusalLine(result, "Cannot use linked or invalid FlatBuffers compiler receiving paths");
        Assert.Equal(foreign, new DirectoryInfo(cache).LinkTarget);
        Assert.Equal("foreign compiler cache", File.ReadAllText(sentinel));
    }

    [Fact]
    public void ADirectoryAtThePinnedCompilerLeafRefusesBeforeDownload()
    {
        using var copy = WireBuildCopy.CreateAbstractions();
        var redirect = "-p:FlatcCacheRoot=" + copy.PathOf("isolated cache");
        var compiler = copy.Properties(["FlatcPath", "FlatcCacheDirectory"], redirect)["FlatcPath"];
        Directory.CreateDirectory(compiler);
        var sentinel = Path.Combine(compiler, "sentinel");
        File.WriteAllText(sentinel, "compiler leaf sentinel");

        var result = copy.TryBuildTypeScript(options: [redirect, "-p:_FlatcReceivingStatus=0", "-p:_FlatcReceivingCheck="]);
        var refusal = RefusalLine(result, "Cannot use linked or invalid FlatBuffers compiler receiving paths");
        Assert.Contains("Linked or non-file owned path", refusal, StringComparison.Ordinal);
        Assert.Contains(compiler, refusal, StringComparison.Ordinal);
        Assert.Equal("compiler leaf sentinel", File.ReadAllText(sentinel));
        Assert.False(File.Exists(copy.PathOf(TypeScriptEntry)));
    }

    [Fact]
    public void ALinkedDownloadWorkspaceRefusesWithAValidCanonicalCompiler()
    {
        using var copy = WireBuildCopy.CreateAbstractions();
        var workspace = copy.PathOf("CuriousContraptions.Abstractions/obj/Debug/net10.0/flatc-download");
        Directory.CreateDirectory(Path.GetDirectoryName(workspace)!);
        var foreign = Directory.CreateDirectory(copy.PathOf("foreign download workspace")).FullName;
        var sentinel = Path.Combine(foreign, "sentinel");
        File.WriteAllText(sentinel, "download workspace sentinel");
        Directory.CreateSymbolicLink(workspace, foreign);

        var refusal = RefusalLine(copy.TryBuildTypeScript(), "Cannot use linked or invalid FlatBuffers compiler receiving paths");
        Assert.Contains("flatc-download", refusal, StringComparison.Ordinal);
        Assert.Equal(foreign, new DirectoryInfo(workspace).LinkTarget);
        Assert.Equal("download workspace sentinel", File.ReadAllText(sentinel));
        Assert.False(File.Exists(copy.PathOf(TypeScriptEntry)));

        Directory.Delete(workspace);
        copy.BuildTypeScript();
    }

    public enum ColdWorkspacePath { Archive, ExtractionDirectory, ExtractedCompiler }

    [Theory]
    [InlineData(false, ColdWorkspacePath.Archive)]
    [InlineData(false, ColdWorkspacePath.ExtractionDirectory)]
    [InlineData(false, ColdWorkspacePath.ExtractedCompiler)]
    [InlineData(true, ColdWorkspacePath.Archive)]
    [InlineData(true, ColdWorkspacePath.ExtractionDirectory)]
    [InlineData(true, ColdWorkspacePath.ExtractedCompiler)]
    public void ColdCompilerWorkspaceRefusesWrongKindsBeforeDownload(bool typescript, ColdWorkspacePath output)
    {
        using var copy = typescript ? WireBuildCopy.CreateAbstractions() : WireBuildCopy.Create();
        var cacheRoot = copy.PathOf("empty compiler cache");
        var redirect = "-p:FlatcCacheRoot=" + cacheRoot;
        var values = copy.Properties(["FlatcAsset", "IntermediateOutputPath", "MSBuildProjectDirectory"], redirect);
        var workspace = Path.Combine(values["MSBuildProjectDirectory"], values["IntermediateOutputPath"].Replace('\\', '/'), "flatc-download");
        var blocked = Path.Combine(workspace, output switch
        {
            ColdWorkspacePath.Archive => values["FlatcAsset"] + ".zip",
            ColdWorkspacePath.ExtractionDirectory => "extracted",
            ColdWorkspacePath.ExtractedCompiler => "extracted/flatc",
            _ => throw new ArgumentOutOfRangeException(nameof(output))
        });
        var parent = output == ColdWorkspacePath.ExtractionDirectory;
        var sentinel = PutWrongKindOutput(blocked, parent);

        var result = copy.TryProjectTarget(typescript ? "GenerateWireTypeScript" : "GenerateWireAccessors", [redirect], copy.OfflineEnvironment());
        var refusal = RefusalLine(result, "Cannot use linked or invalid FlatBuffers compiler receiving paths");
        Assert.Contains((parent ? "Linked or non-directory owned path: " : "Linked or non-file owned path: ") + blocked,
            refusal, StringComparison.Ordinal);
        Assert.Equal("wrong-kind standalone sentinel", File.ReadAllText(sentinel));
        Assert.False(Directory.Exists(cacheRoot));
        Assert.False(File.Exists(copy.PathOf(TypeScriptEntry)));
        Assert.False(File.Exists(copy.PathOf(GenerationFolder + "/.generated")));
    }

    // ECH3-1: a build killed between copying the compiler to flatc.<guid>.tmp and renaming it leaves that file in the cache.
    // FlatcCacheRoot, the production property the cache path derives from, points this case at a cache of its own (with a
    // space in its path, as on macOS) holding a copy of the verified compiler, a truncated temporary file 90 minutes old and
    // a garbage one ten minutes old, which a concurrent build could still be placing. The build sweeps only temporary files
    // older than 60 minutes and runs the verified compiler. The two ages pin that threshold between 10 and 90 minutes. On a
    // host one hour or more from UTC, reading local times as UTC fails the case: ahead of UTC the stale file reads at most
    // 30 minutes old and is kept (30 minutes' margin over the build's duration); behind it the fresh file reads at least 70
    // minutes old and is swept. A host less than an hour from UTC cannot show that slip. The compiler's old write time shows
    // that the sweep selects by name, not age alone. Of the shared cache, only what this case controls is checked.
    [Fact]
    public void ALeftoverTemporaryFileInTheCompilerCacheIsSweptWhenStaleAndNeverRun()
    {
        using var copy = WireBuildCopy.Create();
        var sharedCache = Path.GetDirectoryName(copy.CachedCompiler)!;
        var sharedEntries = Directory.GetFileSystemEntries(sharedCache);
        var sharedWriteTime = File.GetLastWriteTimeUtc(copy.CachedCompiler);
        var cacheRoot = copy.PathOf("flatc cache");
        var redirect = "-p:FlatcCacheRoot=" + cacheRoot;
        var properties = copy.Properties(["FlatcCacheDirectory", "FlatcPath"], redirect);
        var directory = properties["FlatcCacheDirectory"];
        var compiler = properties["FlatcPath"];
        Assert.StartsWith(cacheRoot + "/", compiler, StringComparison.Ordinal);
        Directory.CreateDirectory(directory);
        File.Copy(copy.CachedCompiler, compiler);
        var now = DateTime.UtcNow;
        File.SetLastWriteTimeUtc(compiler, now.AddDays(-2));
        var stale = Path.Combine(directory, $"flatc.{Guid.NewGuid():N}.tmp");
        File.WriteAllBytes(stale, File.ReadAllBytes(copy.CachedCompiler)[..4096]);
        File.SetLastWriteTimeUtc(stale, now.AddMinutes(-90));
        var fresh = Path.Combine(directory, $"flatc.{Guid.NewGuid():N}.tmp");
        File.WriteAllText(fresh, "not a compiler\n");
        File.SetLastWriteTimeUtc(fresh, now.AddMinutes(-10));
        var compilerWriteTime = File.GetLastWriteTimeUtc(compiler);
        var freshWriteTime = File.GetLastWriteTimeUtc(fresh);

        copy.Build(redirect);

        Assert.Equal(copy.SchemaTypes().Order(StringComparer.Ordinal), copy.GeneratedTypeNames().Order(StringComparer.Ordinal));
        Assert.Equal(new[] { compiler, fresh }.Order(StringComparer.Ordinal), Directory.GetFiles(directory).Order(StringComparer.Ordinal));
        Assert.Equal(compilerWriteTime, File.GetLastWriteTimeUtc(compiler));
        Assert.Equal(freshWriteTime, File.GetLastWriteTimeUtc(fresh));
        Assert.Empty(Directory.GetFileSystemEntries(sharedCache).Except(sharedEntries, StringComparer.Ordinal));
        Assert.True(File.Exists(copy.CachedCompiler));
        Assert.Equal(sharedWriteTime, File.GetLastWriteTimeUtc(copy.CachedCompiler));
        copy.Dispose();
        Assert.False(Directory.Exists(cacheRoot));
    }

    // A '*' or '?' in the cache path would make the sweep's glob, and every item use of the path, a file pattern, so the
    // build refuses it before reading or writing the cache. A decoy sibling whose name the wildcard matches holds a stale
    // temporary file, which an unescaped sweep would delete. Uncached, the wildcard cache root is absent, so a refusal placed
    // after MakeDir or the sweep leaves it created. Cached, the wildcard cache holds a copy of the verified compiler and a
    // stale temporary file, so a build that skipped the refusal would run that compiler without a download and exit 0, and
    // an unescaped sweep before the refusal would delete the stale file.
    [Theory]
    [InlineData("flatc?cache", "flatcXcache", false)]
    [InlineData("flatc*cache", "flatcXYcache", true)]
    public void AWildcardInTheCompilerCachePathRefusesTheBuild(string cacheName, string decoyName, bool cached)
    {
        using var copy = WireBuildCopy.Create();
        var cacheRoot = copy.PathOf(cacheName);
        var redirect = "-p:FlatcCacheRoot=" + cacheRoot;
        var directory = copy.Properties(["FlatcCacheDirectory", "FlatcPath"], redirect)["FlatcCacheDirectory"];
        Assert.StartsWith(cacheRoot + "/", directory, StringComparison.Ordinal);
        var now = DateTime.UtcNow;
        string[] entries = [];
        if (cached)
        {
            Directory.CreateDirectory(directory);
            File.Copy(copy.CachedCompiler, Path.Combine(directory, "flatc"));
            var stale = Path.Combine(directory, $"flatc.{Guid.NewGuid():N}.tmp");
            File.WriteAllText(stale, "not a compiler\n");
            File.SetLastWriteTimeUtc(stale, now.AddMinutes(-90));
            entries = Directory.GetFileSystemEntries(directory);
        }
        var decoyDirectory = copy.PathOf(decoyName) + directory[cacheRoot.Length..];
        Directory.CreateDirectory(decoyDirectory);
        var decoy = Path.Combine(decoyDirectory, $"flatc.{Guid.NewGuid():N}.tmp");
        File.WriteAllText(decoy, "not a compiler\n");
        File.SetLastWriteTimeUtc(decoy, now.AddDays(-1));
        var decoyWriteTime = File.GetLastWriteTimeUtc(decoy);

        var result = copy.TryBuild(redirect);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains($"The FlatBuffers compiler cache path \"{directory}\" contains a wildcard character", result.Output, StringComparison.Ordinal);
        Assert.Contains("Wildcard characters are not supported in the compiler cache path", result.Output, StringComparison.Ordinal);
        Assert.False(File.Exists(copy.PathOf(IntermediateAssembly)));
        Assert.False(File.Exists(copy.PathOf(Assembly)));
        if (cached) Assert.Equal(entries, Directory.GetFileSystemEntries(directory));
        else Assert.False(Directory.Exists(cacheRoot));
        Assert.Equal(decoyWriteTime, File.GetLastWriteTimeUtc(decoy));
    }

    private static HashSet<string> DeclaredWireTypes(string assembly)
    {
        using var stream = File.OpenRead(assembly);
        using var reader = new PEReader(stream);
        var metadata = reader.GetMetadataReader();
        return metadata.TypeDefinitions.Select(metadata.GetTypeDefinition)
            .Where(type => metadata.GetString(type.Namespace) == WireNamespace)
            .Select(type => metadata.GetString(type.Name)).ToHashSet(StringComparer.Ordinal);
    }

    private static bool DeclaresProperty(string assembly, string typeName, string propertyName)
    {
        using var stream = File.OpenRead(assembly);
        using var reader = new PEReader(stream);
        var metadata = reader.GetMetadataReader();
        var type = metadata.TypeDefinitions.Select(metadata.GetTypeDefinition)
            .Single(definition => metadata.GetString(definition.Namespace) == WireNamespace && metadata.GetString(definition.Name) == typeName);
        return type.GetProperties().Any(property => metadata.GetString(metadata.GetPropertyDefinition(property).Name) == propertyName);
    }

    /// <summary>One child process's exit code and its two streams, kept apart so stdout alone is parsed.</summary>
    private sealed record ChildResult(int ExitCode, string Output, string Error)
    {
        public string Report => $"exit code {ExitCode}\nstdout:\n{Output}\nstderr:\n{Error}";
    }

    /// <summary>One case's own copy of the vendored project's build inputs, deleted when the case ends.</summary>
    private sealed class WireBuildCopy : IDisposable
    {
        private readonly DirectoryInfo _directory;
        private readonly string _projectFile;

        private WireBuildCopy(DirectoryInfo directory, string projectFile, string[] trackedSources)
        {
            _directory = directory;
            _projectFile = projectFile;
            TrackedSources = trackedSources;
        }

        /// <summary>The C# files the copy holds before any build: the vendored project's tracked sources.</summary>
        public string[] TrackedSources { get; }

        /// <summary>The cached compiler path and the pinned binary digest, as the copied project file evaluates them.</summary>
        public string CachedCompiler { get; private set; } = "";
        public string PinnedCompilerDigest { get; private set; } = "";

        public static WireBuildCopy Create(string prefix = "s5-wire-build-")
        {
            var listed = Run("git", ["-C", Root, "ls-files", "-z", "--", ProjectDirectory], Root);
            Assert.True(listed.ExitCode == 0, $"git ls-files failed:\n{listed.Report}");
            var vendored = listed.Output.Split('\0', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            string[] schemas = [OneSchema];
            var sources = vendored.Where(file => file.EndsWith(".cs", StringComparison.Ordinal)).Order(StringComparer.Ordinal).ToArray();
            var copy = new WireBuildCopy(Directory.CreateTempSubdirectory(prefix), ProjectFile, sources);
            try
            {
                foreach (var file in SharedBuildFiles.Concat(vendored).Concat(schemas))
                {
                    var target = copy.PathOf(file);
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    File.Copy(Path.Combine(Root, file), target);
                }
                copy.RequireCachedCompiler();
                return copy;
            }
            catch
            {
                copy.Dispose();
                throw;
            }
        }

        public static WireBuildCopy CreateAbstractions(string prefix = "s5c-ts-build-")
        {
            var copy = new WireBuildCopy(Directory.CreateTempSubdirectory(prefix), AbstractionsProjectFile, []);
            try
            {
                foreach (var file in SharedBuildFiles.Concat([AbstractionsProjectFile, OneSchema]))
                {
                    var target = copy.PathOf(file);
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    File.Copy(Path.Combine(Root, file), target);
                }
                copy.RequireCachedCompiler();
                return copy;
            }
            catch
            {
                copy.Dispose();
                throw;
            }
        }

        // Evaluation only (no target runs), before any build: a build with an empty cache would download the compiler and
        // write the cache, which these cases never do.
        private void RequireCachedCompiler()
        {
            var properties = Properties(["FlatcPath", "FlatcBinarySha256"]);
            CachedCompiler = properties["FlatcPath"];
            PinnedCompilerDigest = properties["FlatcBinarySha256"];
            if (File.Exists(CachedCompiler)) return;
            Assert.Fail($"The pinned FlatBuffers compiler is not in the per-user cache at \"{CachedCompiler}\". These cases never download it " +
                $"or write the cache. Provision it first, with network access to github.com, by any build that generates the wire accessors, " +
                $"for example `dotnet build {ProjectFile} --no-incremental` in the checkout.");
        }

        public string PathOf(string relative) => Path.Combine(_directory.FullName, relative);

        public string[] CSharpFiles() => Directory.GetFiles(_directory.FullName, "*.cs", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(_directory.FullName, path).Replace('\\', '/')).Order(StringComparer.Ordinal).ToArray();

        public HashSet<string> SchemaTypes() => CSharpStaticGateTests
            .SchemaTypesIn(File.ReadAllText(PathOf(OneSchema))).Keys.ToHashSet(StringComparer.Ordinal);

        /// <summary>Every generated source (relative to the generation folder) with its last write time.</summary>
        public Dictionary<string, DateTime> GeneratedSources(string? directory = null)
        {
            directory ??= PathOf(GenerationFolder);
            return Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories)
                .ToDictionary(path => Path.GetRelativePath(directory, path), File.GetLastWriteTimeUtc, StringComparer.Ordinal);
        }

        /// <summary>The type names of the generated sources (one file per schema type, named after it).</summary>
        public string[] GeneratedTypeNames() => GeneratedSources().Keys.Select(path => Path.GetFileNameWithoutExtension(path)).ToArray();

        /// <summary>Every generated TypeScript file under the copy's tools/e2e/.wire/, by its path relative to it.</summary>
        public Dictionary<string, string> TypeScript() => WorkshopWireTypeScriptTests.ReadGeneratedTypeScript(PathOf(TypeScriptFolder));

        /// <summary>The names the generated TypeScript entry module re-exports.</summary>
        public string[] TypeScriptExports() => WorkshopWireTypeScriptTests.EntryExports(File.ReadAllText(PathOf(TypeScriptEntry)));

        /// <summary>Every generated TypeScript file with its last write time.</summary>
        public Dictionary<string, DateTime> TypeScriptWriteTimes() => Directory.GetFiles(PathOf(TypeScriptFolder), "*", SearchOption.AllDirectories)
            .ToDictionary(path => Path.GetRelativePath(PathOf(TypeScriptFolder), path), File.GetLastWriteTimeUtc, StringComparer.Ordinal);

        public void Edit(string relative, string find, string replacement)
        {
            var path = PathOf(relative);
            var text = File.ReadAllText(path);
            var index = text.IndexOf(find, StringComparison.Ordinal);
            Assert.True(index >= 0 && text.IndexOf(find, index + 1, StringComparison.Ordinal) < 0, $"{relative} must contain [{find}] exactly once");
            File.WriteAllText(path, text.Replace(find, replacement, StringComparison.Ordinal));
        }

        /// <summary>Sets DefineConstants in the copied project file's first property group, as a project would.</summary>
        public void SetDefines(string defines) => Edit(ProjectFile, "<ImplicitUsings>disable</ImplicitUsings>\n",
            $"<ImplicitUsings>disable</ImplicitUsings>\n    <DefineConstants>{defines}</DefineConstants>\n");

        public void Build(params string[] options)
        {
            var result = TryBuild(options);
            Assert.True(result.ExitCode == 0, $"dotnet build failed:\n{result.Report}");
        }

        public ChildResult TryBuild(params string[] options) =>
            RunWithoutProvisioning(["build", PathOf(_projectFile), "-c", "Debug", "--disable-build-servers", .. options]);

        public void BuildTypeScript(string configuration = "Debug", params string[] options)
        {
            var result = TryBuildTypeScript(configuration, options);
            Assert.True(result.ExitCode == 0, $"dotnet msbuild -t:GenerateWireTypeScript failed:\n{result.Report}");
        }

        public ChildResult TryBuildTypeScript(string configuration = "Debug", params string[] options) =>
            RunWithoutProvisioning(["msbuild", PathOf(_projectFile), "-t:GenerateWireTypeScript", $"-p:Configuration={configuration}", .. options]);

        public ChildResult TryBuildRuntimeTypeScript() => TryProjectTarget("BuildRuntimeTypeScript");

        public ChildResult TryProjectTarget(string target, string[]? options = null, IReadOnlyDictionary<string, string>? environment = null) =>
            RunWithoutProvisioning(["msbuild", PathOf(_projectFile), "-t:" + target, "-p:Configuration=Debug", .. (options ?? [])], environment);

        public Dictionary<string, string> OfflineEnvironment()
        {
            var tools = Directory.CreateDirectory(PathOf("offline build tools")).FullName;
            File.CreateSymbolicLink(Path.Combine(tools, "find"), "/usr/bin/find");
            File.CreateSymbolicLink(Path.Combine(tools, "sh"), "/bin/sh");
            return new Dictionary<string, string> { ["PATH"] = tools };
        }

        public ChildResult TryProvisionOnUnsupportedHost()
        {
            const string fixture = "unsupported-compiler-host.proj";
            File.WriteAllText(PathOf(fixture), """
                <Project TreatAsLocalProperty="FlatcHostArchitecture;RuntimeTypeScriptRoot">
                  <PropertyGroup>
                    <FlatcHostArchitecture>Arm</FlatcHostArchitecture>
                    <RuntimeTypeScriptRoot>$(MSBuildProjectDirectory)/</RuntimeTypeScriptRoot>
                  </PropertyGroup>
                  <Import Project="tools/runtime/FlatcCompiler.targets" />
                </Project>
                """);
            return RunWithoutProvisioning(["msbuild", PathOf(fixture), "-t:ProvisionFlatBuffersCompiler"]);
        }

        public void BuildRuntimeTypeScript()
        {
            var result = TryBuildRuntimeTypeScript();
            Assert.True(result.ExitCode == 0, $"dotnet msbuild -t:BuildRuntimeTypeScript failed:\n{result.Report}");
        }

        public void LoadCompiledWireModule(string path)
        {
            var result = Run("node", [PathOf(path)], _directory.FullName);
            Assert.True(result.ExitCode == 0, result.Report);
        }

        public void AssertNoFlatBuffersShadow()
        {
            var result = Dotnet(["msbuild", PathOf(_projectFile), "-t:RejectFlatBuffersShadow"]);
            Assert.True(result.ExitCode == 0, result.Report);
        }

        private static readonly string[] RuntimeManifests = ["package.json", "package-lock.json"];

        public void PrepareRuntimeCompile()
        {
            const string config = "tsconfig.runtime.json";
            using var tsconfig = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, config)));
            var inputs = tsconfig.RootElement.GetProperty("include").EnumerateArray()
                .Select(input => input.GetString()!)
                .SelectMany(path => path == "tools/runtime/**/*.ts"
                    ? Directory.EnumerateFiles(Path.Combine(Root, "tools/runtime"), "*.ts", SearchOption.AllDirectories)
                        .Select(file => Path.GetRelativePath(Root, file))
                    : [path]);
            // The observation harness imports this source through its emitted .js specifier.
            var sources = inputs.Append("CuriousContraptions.web/wwwroot/workshop-observation.ts");
            foreach (var path in sources.Append(config).Concat(RuntimeManifests))
            {
                var destination = PathOf(path);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(Path.Combine(Root, path), destination);
            }
            Directory.CreateDirectory(PathOf("node_modules"));
            foreach (var directory in new[] { "@types", "undici-types" })
                Directory.CreateSymbolicLink(PathOf("node_modules/" + directory), Path.Combine(Root, "node_modules", directory));
            foreach (var tool in new[] { "node_modules/flatbuffers", "node_modules/typescript" })
            {
                foreach (var file in Directory.EnumerateFiles(Path.Combine(Root, tool), "*", SearchOption.AllDirectories))
                {
                    var destination = PathOf(Path.GetRelativePath(Root, file));
                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                    File.Copy(file, destination);
                }
            }
            File.Copy(Path.Combine(Root, "node_modules/.package-lock.json"), PathOf("node_modules/.package-lock.json"));
            KeepRestoreMarkerCurrent();
        }

        public void KeepRestoreMarkerCurrent()
        {
            var latestInput = RuntimeManifests.Max(path => File.GetLastWriteTimeUtc(PathOf(path)));
            File.SetLastWriteTimeUtc(PathOf("node_modules/.package-lock.json"), latestInput);
        }

        public void CleanTypeScript()
        {
            var result = TryProjectTarget("Clean");
            Assert.True(result.ExitCode == 0, $"dotnet msbuild -t:Clean failed:\n{result.Report}");
        }

        private ChildResult RunWithoutProvisioning(string[] arguments, IReadOnlyDictionary<string, string>? environment = null)
        {
            var result = Dotnet(arguments, environment);
            Assert.DoesNotContain(Download, result.Output, StringComparison.Ordinal);
            Assert.DoesNotContain(Download, result.Error, StringComparison.Ordinal);
            return result;
        }

        /// <summary>The named properties as the copied project file evaluates them with the given build options; with two or
        /// more names, -getProperty prints one JSON object on stdout.</summary>
        public Dictionary<string, string> Properties(string[] names, params string[] options)
        {
            var result = Dotnet(["msbuild", PathOf(_projectFile), .. options, .. names.Select(name => "-getProperty:" + name)]);
            Assert.True(result.ExitCode == 0, $"dotnet msbuild -getProperty failed:\n{result.Report}");
            using var document = JsonDocument.Parse(result.Output);
            return names.ToDictionary(name => name, name => document.RootElement.GetProperty("Properties").GetProperty(name).GetString() ?? "", StringComparer.Ordinal);
        }

        private ChildResult Dotnet(string[] arguments, IReadOnlyDictionary<string, string>? environment = null)
        {
            var host = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } path
                ? path
                : throw new InvalidOperationException("DOTNET_HOST_PATH is not set, so the dotnet that runs this test cannot be found for the build child.");
            return Run(host, [.. arguments, "-nologo", "-noAutoResponse", "-nodeReuse:false"], _directory.FullName, Path.Combine(Root, "node_modules", ".bin"), environment);
        }

        public void Dispose()
        {
            if (_directory.Exists) _directory.Delete(recursive: true);
        }
    }

    // Each child inherits the test's environment except the variables through which this test host's own MSBuild and test
    // platform would reach the child build.
    private static ChildResult Run(string program, string[] arguments, string workingDirectory, string? binOnPath = null,
        IReadOnlyDictionary<string, string>? environment = null)
    {
        var start = new ProcessStartInfo(program)
        {
            WorkingDirectory = workingDirectory, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        if (binOnPath is not null)
            start.Environment["PATH"] = binOnPath + Path.PathSeparator + start.Environment["PATH"];
        foreach (var name in start.Environment.Keys.Where(IsHostBuildVariable).ToArray()) start.Environment.Remove(name);
        if (environment is not null)
            foreach (var (name, value) in environment)
                start.Environment[name] = value;
        var output = new StringBuilder();
        var error = new StringBuilder();
        using var process = new Process { StartInfo = start };
        process.OutputDataReceived += (_, line) => Append(output, line.Data);
        process.ErrorDataReceived += (_, line) => Append(error, line.Data);
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        if (!process.WaitForExit(ChildTimeout))
        {
            process.Kill(entireProcessTree: true);
            Assert.Fail($"{program} {string.Join(' ', arguments)} did not finish within {ChildTimeout}:\nstdout:\n{TextOf(output)}\nstderr:\n{TextOf(error)}");
        }
        process.WaitForExit();
        return new(process.ExitCode, TextOf(output), TextOf(error));
    }

    // A null line marks the end of the stream, not an empty line.
    private static void Append(StringBuilder stream, string? line)
    {
        if (line is null) return;
        lock (stream) stream.AppendLine(line);
    }

    private static string TextOf(StringBuilder stream)
    {
        lock (stream) return stream.ToString();
    }

    private static bool IsHostBuildVariable(string name) =>
        name.StartsWith("MSBUILD", StringComparison.Ordinal) || name is "MSBuildSDKsPath" or "MSBuildExtensionsPath" ||
        name.StartsWith("VSTEST_", StringComparison.Ordinal) || name.StartsWith("TESTINGPLATFORM_", StringComparison.Ordinal);
}
