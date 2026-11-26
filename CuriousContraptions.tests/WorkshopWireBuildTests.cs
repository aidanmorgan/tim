using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace CuriousContraptions.Tests;

/// <summary>
/// S5 (M1 to M5, M7): the vendored FlatBuffers project generates the wire accessors from schemas/*.fbs with the pinned
/// compiler in every build that needs them, and refuses a build host without a release asset and the Span accessor define.
/// S5b (T2 to T4, T6, T10): the same build generates the TypeScript accessors into the copy's tools/e2e/.wire/. Each case
/// copies the vendored project, the schemas and the shared build files into its own temporary directory outside the
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
    private const string Assembly = ProjectDirectory + "/bin/Debug/net10.0/CuriousContraptions.FlatBuffers.dll";
    private const string ReleaseAssembly = ProjectDirectory + "/bin/Release/net10.0/CuriousContraptions.FlatBuffers.dll";
    private const string IntermediateAssembly = ProjectDirectory + "/obj/Debug/net10.0/CuriousContraptions.FlatBuffers.dll";
    private const string GenerationFolder = ProjectDirectory + "/obj/Debug/net10.0/WireAccessors";
    private const string TypeScriptFolder = "tools/e2e/.wire";
    private const string TypeScriptEntry = TypeScriptFolder + "/curious-contraptions/wire.ts";
    private const string SavedConstructionModule = TypeScriptFolder + "/curious-contraptions/wire/saved-construction.ts";
    private const string PuzzleRecordModule = TypeScriptFolder + "/curious-contraptions/wire/puzzle-record.ts";
    private const string TypeScriptStaging = TypeScriptFolder + "/.staging";
    private const string WireNamespace = "CuriousContraptions.Wire";
    private const string Download = "Downloading the pinned FlatBuffers compiler";

    // The shared build files the vendored project's build reads, besides its own tracked files and the schemas.
    private static readonly string[] SharedBuildFiles =
        ["Directory.Build.props", "Directory.Build.targets", "global.json", ".editorconfig", "tools/runtime/RuntimeTypeScript.targets"];

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

    [Fact]
    public void AnAddedSchemaFieldIsAnAccessorAfterAnIncrementalBuild()
    {
        using var copy = WireBuildCopy.Create();
        copy.Build();
        Assert.False(DeclaresProperty(copy.PathOf(Assembly), "SavedConstruction", "S5Probe"));

        copy.Edit("schemas/workshop_authoring.fbs", "table SavedConstruction {\n", "table SavedConstruction {\n    s5_probe: uint32;\n");
        copy.Build();

        Assert.True(DeclaresProperty(copy.PathOf(Assembly), "SavedConstruction", "S5Probe"));
    }

    // T2: the field is added to the authoring schema, which reaches the wire schema through its include.
    [Fact]
    public void AnAddedSchemaFieldIsATypeScriptAccessorAfterAnIncrementalBuild()
    {
        using var copy = WireBuildCopy.Create();
        copy.Build();
        Assert.DoesNotContain("s5bProbe():", File.ReadAllText(copy.PathOf(SavedConstructionModule)), StringComparison.Ordinal);

        copy.Edit("schemas/workshop_authoring.fbs", "table SavedConstruction {\n", "table SavedConstruction {\n    s5b_probe: uint32;\n");
        copy.Build();

        Assert.Contains("s5bProbe():", File.ReadAllText(copy.PathOf(SavedConstructionModule)), StringComparison.Ordinal);
        Assert.Empty(WorkshopWireTypeScriptTests.GeneratedTypeScriptViolations(copy.TypeScript(), copy.PathOf("schemas")));
    }

    [Fact]
    public void AnUnchangedSchemaDoesNotRegenerate()
    {
        using var copy = WireBuildCopy.Create();
        copy.Build();
        var first = copy.GeneratedSources();

        copy.Build();
        Assert.Equal(first, copy.GeneratedSources());

        File.SetLastWriteTimeUtc(copy.PathOf("schemas/workshop_authoring.fbs"), DateTime.UtcNow);
        copy.Build();
        var third = copy.GeneratedSources();
        Assert.Equal(first.Keys.Order(StringComparer.Ordinal), third.Keys.Order(StringComparer.Ordinal));
        Assert.All(first, source => Assert.True(third[source.Key] > source.Value, $"{source.Key} was not regenerated"));
    }

    // T3: an unchanged incremental build leaves every generated file alone; a touched schema, and a no-incremental build, whose
    // Clean deletes the schema list, each rewrite every one. Boundary: the states an interrupted generation can leave, seeded (no
    // fault is injected): per-type files present with the entry module missing (one un-normalised, a staged file left over);
    // the entry module older than a schema with a staged set left over; the schema record missing beside a current entry
    // module; each is regenerated by the next incremental build into
    // a complete module set (T1's structural checks). Control: complete, current output with empty staging folders left over
    // is not regenerated. The target's exact move order (entry module last) is a source-reviewed property.
    [Fact]
    public void AnUnchangedSchemaDoesNotRegenerateTypeScript()
    {
        using var copy = WireBuildCopy.Create();
        copy.Build();
        var first = copy.TypeScriptWriteTimes();

        copy.Build();
        Assert.Equal(first, copy.TypeScriptWriteTimes());

        File.SetLastWriteTimeUtc(copy.PathOf("schemas/workshop_authoring.fbs"), DateTime.UtcNow);
        copy.Build();
        var touched = copy.TypeScriptWriteTimes();
        Assert.Equal(first.Keys.Order(StringComparer.Ordinal), touched.Keys.Order(StringComparer.Ordinal));
        Assert.All(first, file => Assert.True(touched[file.Key] > file.Value, $"a touched schema did not regenerate {file.Key}"));

        copy.Build("--no-incremental");
        var rebuilt = copy.TypeScriptWriteTimes();
        Assert.All(touched, file => Assert.True(rebuilt[file.Key] > file.Value, $"a no-incremental build did not regenerate {file.Key}"));

        // Per-type files present, the entry module missing, one per-type file un-normalised and a staged file left over.
        File.Delete(copy.PathOf(TypeScriptEntry));
        var puzzleRecord = copy.PathOf(PuzzleRecordModule);
        File.WriteAllText(puzzleRecord, File.ReadAllText(puzzleRecord).Replace("optionalEncoding?:flatbuffers.Encoding", "optionalEncoding?:any", StringComparison.Ordinal));
        Directory.CreateDirectory(copy.PathOf(TypeScriptStaging + "/curious-contraptions"));
        File.WriteAllText(copy.PathOf(TypeScriptStaging + "/curious-contraptions/wire.ts"), "export {};\n");
        Assert.NotEmpty(WorkshopWireTypeScriptTests.GeneratedTypeScriptViolations(copy.TypeScript(), copy.PathOf("schemas")));
        copy.Build();
        Assert.Empty(WorkshopWireTypeScriptTests.GeneratedTypeScriptViolations(copy.TypeScript(), copy.PathOf("schemas")));

        // The entry module older than a schema, with a staged set left over: regenerated, the staging folder cleared.
        var current = copy.TypeScriptWriteTimes();
        File.SetLastWriteTimeUtc(copy.PathOf(TypeScriptEntry), DateTime.UtcNow.AddDays(-1));
        Directory.CreateDirectory(copy.PathOf(TypeScriptStaging + "/curious-contraptions/wire"));
        File.WriteAllText(copy.PathOf(TypeScriptStaging + "/curious-contraptions/wire/vec3f.ts"), "export class Vec3f {\n");
        copy.Build();
        var regenerated = copy.TypeScriptWriteTimes();
        Assert.Equal(current.Keys.Order(StringComparer.Ordinal), regenerated.Keys.Order(StringComparer.Ordinal));
        Assert.All(current, file => Assert.True(regenerated[file.Key] > file.Value, $"a stale entry module did not regenerate {file.Key}"));
        Assert.Empty(WorkshopWireTypeScriptTests.GeneratedTypeScriptViolations(copy.TypeScript(), copy.PathOf("schemas")));

        // The schema record missing beside a current entry module: regenerated, since the record is an output too.
        File.Delete(copy.PathOf(TypeScriptFolder + "/schemas.txt"));
        copy.Build();
        Assert.Empty(WorkshopWireTypeScriptTests.GeneratedTypeScriptViolations(copy.TypeScript(), copy.PathOf("schemas")));
        regenerated = copy.TypeScriptWriteTimes();

        // Control: complete, current output with only empty staging folders left over is current and is not regenerated.
        Directory.CreateDirectory(copy.PathOf(TypeScriptStaging + "/curious-contraptions/wire"));
        copy.Build();
        Assert.Equal(regenerated, copy.TypeScriptWriteTimes());
        Assert.Empty(WorkshopWireTypeScriptTests.GeneratedTypeScriptViolations(copy.TypeScript(), copy.PathOf("schemas")));
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

    // T4: the removed table is the last type of the wire schema, reached through the authoring schema's include.
    [Fact]
    public void ARemovedSchemaTypeLeavesNoTypeScript()
    {
        using var copy = WireBuildCopy.Create();
        var schema = copy.PathOf("schemas/workshop_wire.fbs");
        var original = File.ReadAllText(schema);
        File.WriteAllText(schema, original + "\ntable S5bRemovedProbe {\n    value: uint32;\n}\n");
        copy.Build();
        Assert.Contains("S5bRemovedProbe", copy.TypeScriptExports());
        Assert.Single(copy.TypeScript(), file => file.Value.Contains("export class S5bRemovedProbe {", StringComparison.Ordinal));

        File.WriteAllText(schema, original);
        copy.Build();

        Assert.DoesNotContain("S5bRemovedProbe", copy.TypeScriptExports());
        Assert.DoesNotContain(copy.TypeScript(), file => file.Key.Contains("removed-probe", StringComparison.Ordinal) || file.Value.Contains("S5bRemovedProbe", StringComparison.Ordinal));
        Assert.Empty(WorkshopWireTypeScriptTests.GeneratedTypeScriptViolations(copy.TypeScript(), copy.PathOf("schemas")));
    }

    // The schema order comes from the compiler's dependency output: the root, the one schema no other schema includes, is
    // compiled last and writes the entry module. A second root would leave it short, a set with no root has no entry writer, a
    // root whose includes miss a separate include cycle would leave that cycle's types out, and a schema in another namespace
    // would write a second module, a schema file name outside [A-Za-z0-9_.-] could be split or expanded, and an include that
    // does not name a schema file by its exact name could not be matched to one; each is refused, naming the schemas, files or
    // includes, and leaves the folder a previous build generated exactly as it was. Control: ./workshop_wire.fbs, which the
    // compiler folds to the exact file name, builds a complete module set.
    [Fact]
    public void ASchemaSetWithoutOneRootReachingEverySchemaRefusesTheBuild()
    {
        using var copy = WireBuildCopy.Create();
        copy.Build();
        var generated = copy.TypeScriptWriteTimes();

        var secondRoot = copy.PathOf("schemas/s5b_second_root.fbs");
        File.WriteAllText(secondRoot, "namespace CuriousContraptions.Wire;\n\ntable S5bSecondRoot {\n    value: uint32;\n}\n");
        var twoRoots = RefusalLine(copy.TryBuild(), "these schemas are included by none: ");
        Assert.Contains("s5b_second_root.fbs", twoRoots, StringComparison.Ordinal);
        Assert.Contains("workshop_authoring.fbs", twoRoots, StringComparison.Ordinal);
        Assert.DoesNotContain("workshop_wire.fbs", twoRoots, StringComparison.Ordinal);
        Assert.Equal(generated, copy.TypeScriptWriteTimes());
        File.Delete(secondRoot);

        var authoring = copy.PathOf("schemas/workshop_authoring.fbs");
        var original = File.ReadAllText(authoring);
        File.WriteAllText(authoring, original.Replace("include \"workshop_wire.fbs\";\n", "include \"workshop_wire.fbs\";\ninclude \"s5b_cycle.fbs\";\n", StringComparison.Ordinal));
        var cycle = copy.PathOf("schemas/s5b_cycle.fbs");
        File.WriteAllText(cycle, "include \"workshop_authoring.fbs\";\n\nnamespace CuriousContraptions.Wire;\n\ntable S5bCycle {\n    value: uint32;\n}\n");
        var noRoot = RefusalLine(copy.TryBuild(), "each schema is included by another: ");
        Assert.Contains("s5b_cycle.fbs", noRoot, StringComparison.Ordinal);
        Assert.Contains("workshop_authoring.fbs", noRoot, StringComparison.Ordinal);
        Assert.Contains("workshop_wire.fbs", noRoot, StringComparison.Ordinal);
        Assert.Equal(generated, copy.TypeScriptWriteTimes());
        File.WriteAllText(authoring, original);
        File.Delete(cycle);

        File.WriteAllText(copy.PathOf("schemas/s5b_loop_a.fbs"), "include \"s5b_loop_b.fbs\";\n\nnamespace CuriousContraptions.Wire;\n\ntable S5bLoopA {\n    value: uint32;\n}\n");
        File.WriteAllText(copy.PathOf("schemas/s5b_loop_b.fbs"), "include \"s5b_loop_a.fbs\";\n\nnamespace CuriousContraptions.Wire;\n\ntable S5bLoopB {\n    value: uint32;\n}\n");
        var unreached = RefusalLine(copy.TryBuild(), "but its includes do not reach ");
        Assert.Contains("written by workshop_authoring.fbs, the one schema", unreached, StringComparison.Ordinal);
        Assert.Contains("s5b_loop_a.fbs", unreached, StringComparison.Ordinal);
        Assert.Contains("s5b_loop_b.fbs", unreached, StringComparison.Ordinal);
        Assert.Equal(generated, copy.TypeScriptWriteTimes());
        File.Delete(copy.PathOf("schemas/s5b_loop_a.fbs"));
        File.Delete(copy.PathOf("schemas/s5b_loop_b.fbs"));

        File.WriteAllText(authoring, original.Replace("include \"workshop_wire.fbs\";\n", "include \"workshop_wire.fbs\";\ninclude \"s5b_other.fbs\";\n", StringComparison.Ordinal));
        File.WriteAllText(copy.PathOf("schemas/s5b_other.fbs"), "namespace S5bOther;\n\ntable S5bOtherRecord {\n    value: uint32;\n}\n");
        var stray = RefusalLine(copy.TryBuild(), "wrote TypeScript outside the wire namespace's module");
        Assert.Contains("s5b-other", stray, StringComparison.Ordinal);
        Assert.Equal(generated, copy.TypeScriptWriteTimes());
        File.Delete(copy.PathOf("schemas/s5b_other.fbs"));

        File.WriteAllText(authoring, original);

        // A schema file name that does not start with [A-Za-z0-9_] or continues outside [A-Za-z0-9_.-] (a leading '-', which the
        // compiler would read as an option, a shell character, a space) is refused by name.
        foreach (var misnamed in MisnamedSchemas)
        {
            File.WriteAllText(copy.PathOf("schemas/" + misnamed), "namespace CuriousContraptions.Wire;\n\ntable S5bMisnamed {\n    value: uint32;\n}\n");
            var refusal = RefusalLine(copy.TryBuild(), "A wire schema file name must start with a letter, digit or '_' and consist of letters, digits, '_', '.' and '-' only: ");
            Assert.Contains($"\"{misnamed}\"", refusal, StringComparison.Ordinal);
            Assert.Equal(generated, copy.TypeScriptWriteTimes());
            File.Delete(copy.PathOf("schemas/" + misnamed));
        }

        // An include must name a schema file in schemas/ by its exact file name: a parent-directory spelling is refused, naming it.
        File.WriteAllText(authoring, original.Replace("include \"workshop_wire.fbs\";\n", "include \"../schemas/workshop_wire.fbs\";\n", StringComparison.Ordinal));
        var parent = RefusalLine(copy.TryBuild(), "An include must name a schema file in schemas/ by its exact file name, but the compiler reached: ");
        Assert.Contains("../schemas/workshop_wire.fbs", parent, StringComparison.Ordinal);
        Assert.Equal(generated, copy.TypeScriptWriteTimes());
        // Boundary: a case variant fails on every volume (the exact-name refusal where the compiler resolves it, the compiler's
        // own load failure where it does not), naming it and leaving the previous output as it was.
        File.WriteAllText(authoring, original.Replace("include \"workshop_wire.fbs\";\n", "include \"Workshop_Wire.fbs\";\n", StringComparison.Ordinal));
        var caseVariant = copy.TryBuild();
        Assert.True(caseVariant.ExitCode != 0, $"the build was not refused:\n{caseVariant.Report}");
        Assert.Contains("Workshop_Wire.fbs", caseVariant.Output, StringComparison.Ordinal);
        Assert.Equal(generated, copy.TypeScriptWriteTimes());

        // Control: the compiler folds ./workshop_wire.fbs to workshop_wire.fbs, the exact file name, so it builds a complete set.
        File.WriteAllText(authoring, original.Replace("include \"workshop_wire.fbs\";\n", "include \"./workshop_wire.fbs\";\n", StringComparison.Ordinal));
        copy.Build();
        Assert.Empty(WorkshopWireTypeScriptTests.GeneratedTypeScriptViolations(copy.TypeScript(), copy.PathOf("schemas")));

        // Control: a schema the compiler warns about (a camelCase field) still builds a complete set; its warning never reaches
        // the dependency output.
        File.WriteAllText(authoring, original.Replace("include \"workshop_wire.fbs\";\n", "include \"workshop_wire.fbs\";\ninclude \"s5b_warned.fbs\";\n", StringComparison.Ordinal));
        File.WriteAllText(copy.PathOf("schemas/s5b_warned.fbs"), "namespace CuriousContraptions.Wire;\n\ntable S5bWarned {\n    camelCase: uint32;\n}\n");
        copy.Build();
        Assert.Contains("S5bWarned", copy.SchemaTypes());
        Assert.Empty(WorkshopWireTypeScriptTests.GeneratedTypeScriptViolations(copy.TypeScript(), copy.PathOf("schemas")));
    }

    private static readonly string[] MisnamedSchemas = ["-x.fbs", "s5b(v2).fbs", "s5b spaced.fbs"];

    // The schema order is read from the compiler's output run in the schemas directory on plain file names, so a checkout path
    // with a space (and of any length) reads the same as any other. Control: every other case builds in a path without one.
    [Fact]
    public void ACheckoutPathWithASpaceBuildsACompleteModuleSet()
    {
        using var copy = WireBuildCopy.Create("s5b wire build ");
        Assert.Contains(" ", copy.PathOf("schemas"), StringComparison.Ordinal);

        copy.Build();

        Assert.Empty(WorkshopWireTypeScriptTests.GeneratedTypeScriptViolations(copy.TypeScript(), copy.PathOf("schemas")));
    }

    private static string RefusalLine(ChildResult result, string phrase)
    {
        Assert.True(result.ExitCode != 0, $"the build was not refused:\n{result.Report}");
        return Assert.Single(result.Output.Split('\n').Where(line => line.Contains(phrase, StringComparison.Ordinal)).Distinct());
    }

    // One writer per build graph: only the Debug instance generates TypeScript, the configuration every reader of
    // tools/e2e/.wire/ builds. A Release build compiles every accessor and writes no TypeScript. Control: every other case
    // builds Debug and writes it.
    [Fact]
    public void AReleaseBuildWritesNoTypeScript()
    {
        using var copy = WireBuildCopy.Create();

        copy.BuildAs("Release");

        Assert.True(File.Exists(copy.PathOf(ReleaseAssembly)));
        Assert.Empty(copy.SchemaTypes().Except(DeclaredWireTypes(copy.PathOf(ReleaseAssembly))));
        Assert.False(Directory.Exists(copy.PathOf(TypeScriptFolder)), "a Release build wrote TypeScript");
    }

    // Deleting a whole schema file leaves every remaining input older than the previous output, so only the schema list
    // can show the change to an incremental build. T10: the TypeScript entry is then written by the remaining schema, which
    // includes nothing, and exports exactly its types.
    [Fact]
    public void ADeletedSchemaFileLeavesNoGeneratedSource()
    {
        using var copy = WireBuildCopy.Create();
        copy.Build();
        Assert.Contains("SavedConstruction", copy.GeneratedTypeNames());
        Assert.Contains("SavedConstruction", DeclaredWireTypes(copy.PathOf(Assembly)));
        Assert.Contains("SavedConstruction", copy.TypeScriptExports());

        File.Delete(copy.PathOf("schemas/workshop_authoring.fbs"));
        copy.Build();

        Assert.DoesNotContain("SavedConstruction", copy.GeneratedTypeNames());
        Assert.DoesNotContain("SavedConstruction", DeclaredWireTypes(copy.PathOf(Assembly)));
        Assert.DoesNotContain("SavedConstruction", copy.TypeScriptExports());
        Assert.DoesNotContain(copy.TypeScript(), file => file.Value.Contains("SavedConstruction", StringComparison.Ordinal));
        Assert.Empty(WorkshopWireTypeScriptTests.GeneratedTypeScriptViolations(copy.TypeScript(), copy.PathOf("schemas")));
    }

    [Fact]
    public void APinThatTheCachedCompilerDoesNotMatchRefusesTheBuild()
    {
        using var copy = WireBuildCopy.Create();
        var cached = copy.CachedCompiler;
        var pinned = copy.PinnedCompilerDigest;
        var cachedWriteTime = File.GetLastWriteTimeUtc(cached);
        var altered = (pinned[0] == '0' ? '1' : '0') + pinned[1..];
        copy.Edit(ProjectFile, pinned, altered);

        var result = copy.TryBuild();

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains(cached, result.Output, StringComparison.Ordinal);
        Assert.Contains(altered, result.Output, StringComparison.Ordinal);
        Assert.Contains(pinned, result.Output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"Delete \"{cached}\"", result.Output, StringComparison.Ordinal);
        Assert.False(File.Exists(copy.PathOf(IntermediateAssembly)));
        Assert.False(File.Exists(copy.PathOf(Assembly)));
        Assert.False(Directory.Exists(copy.PathOf(TypeScriptFolder)), "the refused build wrote TypeScript");
        Assert.Equal(cachedWriteTime, File.GetLastWriteTimeUtc(cached));
    }

    [Fact]
    public void ANonIncrementalBuildRegeneratesAndCompiles()
    {
        using var copy = WireBuildCopy.Create();
        copy.Build();
        var first = copy.GeneratedSources();
        copy.Build();
        Assert.Equal(first, copy.GeneratedSources());

        copy.Build("--no-incremental");

        var rebuilt = copy.GeneratedSources();
        Assert.Equal(first.Keys.Order(StringComparer.Ordinal), rebuilt.Keys.Order(StringComparer.Ordinal));
        Assert.All(first, source => Assert.True(rebuilt[source.Key] > source.Value, $"{source.Key} was not regenerated"));
        Assert.Empty(copy.SchemaTypes().Except(DeclaredWireTypes(copy.PathOf(Assembly))));
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
        Assert.False(Directory.Exists(copy.PathOf(TypeScriptFolder)), "the refused build wrote TypeScript");
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

    // FlatcHostArchitecture is the production property the asset table is keyed on; Arm (32-bit) has no release asset.
    // Control: every other case builds on this host's own architecture.
    [Fact]
    public void AnUnsupportedBuildHostRefusesTheBuild()
    {
        using var copy = WireBuildCopy.Create();

        var result = copy.TryBuild("-p:FlatcHostArchitecture=Arm");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains($"has no release asset for this build host ({RuntimeInformation.OSDescription}, Arm).", result.Output, StringComparison.Ordinal);
        Assert.False(File.Exists(copy.PathOf(IntermediateAssembly)));
        Assert.False(File.Exists(copy.PathOf(Assembly)));
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

        private WireBuildCopy(DirectoryInfo directory, string[] trackedSources)
        {
            _directory = directory;
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
            var schemas = Directory.GetFiles(Path.Combine(Root, "schemas"), "*.fbs").Select(path => "schemas/" + Path.GetFileName(path));
            var sources = vendored.Where(file => file.EndsWith(".cs", StringComparison.Ordinal)).Order(StringComparer.Ordinal).ToArray();
            var copy = new WireBuildCopy(Directory.CreateTempSubdirectory(prefix), sources);
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
            .SchemaTypesIn(Directory.GetFiles(PathOf("schemas"), "*.fbs").Select(File.ReadAllText)).Keys.ToHashSet(StringComparer.Ordinal);

        /// <summary>Every generated source (relative to the generation folder) with its last write time.</summary>
        public Dictionary<string, DateTime> GeneratedSources() =>
            Directory.GetFiles(PathOf(GenerationFolder), "*.cs", SearchOption.AllDirectories)
                .ToDictionary(path => Path.GetRelativePath(PathOf(GenerationFolder), path), File.GetLastWriteTimeUtc, StringComparer.Ordinal);

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

        public void Build(params string[] options) => BuildAs("Debug", options);

        /// <summary>Builds the copy in the given configuration; every other case builds Debug.</summary>
        public void BuildAs(string configuration, params string[] options)
        {
            var result = TryBuildAs(configuration, options);
            Assert.True(result.ExitCode == 0, $"dotnet build failed:\n{result.Report}");
        }

        public ChildResult TryBuild(params string[] options) => TryBuildAs("Debug", options);

        private ChildResult TryBuildAs(string configuration, string[] options)
        {
            var result = Dotnet(["build", PathOf(ProjectFile), "-c", configuration, "--disable-build-servers", .. options]);
            Assert.DoesNotContain(Download, result.Output, StringComparison.Ordinal);
            Assert.DoesNotContain(Download, result.Error, StringComparison.Ordinal);
            return result;
        }

        /// <summary>The named properties as the copied project file evaluates them with the given build options; with two or
        /// more names, -getProperty prints one JSON object on stdout.</summary>
        public Dictionary<string, string> Properties(string[] names, params string[] options)
        {
            var result = Dotnet(["msbuild", PathOf(ProjectFile), .. options, .. names.Select(name => "-getProperty:" + name)]);
            Assert.True(result.ExitCode == 0, $"dotnet msbuild -getProperty failed:\n{result.Report}");
            using var document = JsonDocument.Parse(result.Output);
            return names.ToDictionary(name => name, name => document.RootElement.GetProperty("Properties").GetProperty(name).GetString() ?? "", StringComparer.Ordinal);
        }

        private ChildResult Dotnet(string[] arguments)
        {
            var host = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } path
                ? path
                : throw new InvalidOperationException("DOTNET_HOST_PATH is not set, so the dotnet that runs this test cannot be found for the build child.");
            return Run(host, [.. arguments, "-nologo", "-noAutoResponse", "-nodeReuse:false"], _directory.FullName);
        }

        public void Dispose()
        {
            if (_directory.Exists) _directory.Delete(recursive: true);
        }
    }

    // Each child inherits the test's environment except the variables through which this test host's own MSBuild and test
    // platform would reach the child build.
    private static ChildResult Run(string program, string[] arguments, string workingDirectory)
    {
        var start = new ProcessStartInfo(program)
        {
            WorkingDirectory = workingDirectory, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        foreach (var name in start.Environment.Keys.Where(IsHostBuildVariable).ToArray()) start.Environment.Remove(name);
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
