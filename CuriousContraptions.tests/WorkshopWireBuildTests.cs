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
/// Each case copies the vendored project, the schemas and the shared build files into its own temporary directory outside
/// the checkout, drives real <c>dotnet build</c> child processes there and reads the built assembly's metadata. The cases
/// need the pinned compiler already in the per-user cache and check that before any build; none touches the network or
/// writes the cache.
/// </summary>
public sealed class WorkshopWireBuildTests
{
    private const string ProjectDirectory = "CuriousContraptions.FlatBuffers";
    private const string ProjectFile = ProjectDirectory + "/CuriousContraptions.FlatBuffers.csproj";
    private const string Assembly = ProjectDirectory + "/bin/Debug/net10.0/CuriousContraptions.FlatBuffers.dll";
    private const string IntermediateAssembly = ProjectDirectory + "/obj/Debug/net10.0/CuriousContraptions.FlatBuffers.dll";
    private const string GenerationFolder = ProjectDirectory + "/obj/Debug/net10.0/WireAccessors";
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

    // Deleting a whole schema file leaves every remaining input older than the previous output, so only the schema list
    // can show the change to an incremental build.
    [Fact]
    public void ADeletedSchemaFileLeavesNoGeneratedSource()
    {
        using var copy = WireBuildCopy.Create();
        copy.Build();
        Assert.Contains("SavedConstruction", copy.GeneratedTypeNames());
        Assert.Contains("SavedConstruction", DeclaredWireTypes(copy.PathOf(Assembly)));

        File.Delete(copy.PathOf("schemas/workshop_authoring.fbs"));
        copy.Build();

        Assert.DoesNotContain("SavedConstruction", copy.GeneratedTypeNames());
        Assert.DoesNotContain("SavedConstruction", DeclaredWireTypes(copy.PathOf(Assembly)));
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

        public static WireBuildCopy Create()
        {
            var listed = Run("git", ["-C", Root, "ls-files", "-z", "--", ProjectDirectory], Root);
            Assert.True(listed.ExitCode == 0, $"git ls-files failed:\n{listed.Report}");
            var vendored = listed.Output.Split('\0', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var schemas = Directory.GetFiles(Path.Combine(Root, "schemas"), "*.fbs").Select(path => "schemas/" + Path.GetFileName(path));
            var sources = vendored.Where(file => file.EndsWith(".cs", StringComparison.Ordinal)).Order(StringComparer.Ordinal).ToArray();
            var copy = new WireBuildCopy(Directory.CreateTempSubdirectory("s5-wire-build-"), sources);
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
            var properties = Properties("FlatcPath", "FlatcBinarySha256");
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

        public ChildResult TryBuild(params string[] options)
        {
            var result = Dotnet(["build", PathOf(ProjectFile), "-c", "Debug", "--disable-build-servers", .. options]);
            Assert.DoesNotContain(Download, result.Output, StringComparison.Ordinal);
            Assert.DoesNotContain(Download, result.Error, StringComparison.Ordinal);
            return result;
        }

        // With two or more names, -getProperty prints one JSON object on stdout.
        private Dictionary<string, string> Properties(params string[] names)
        {
            var result = Dotnet(["msbuild", PathOf(ProjectFile), .. names.Select(name => "-getProperty:" + name)]);
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
