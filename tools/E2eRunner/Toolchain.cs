using System.Text.Json;
using System.Text.RegularExpressions;

namespace CuriousContraptions.E2eRunner;

/// <summary>The resolved browser-test toolchain, recorded in every summary. Resolution never downloads anything.</summary>
internal sealed partial record Toolchain(string TsxLoader, string TsxVersion, string PlaywrightVersion, string NodeVersion, string ChromeVersion)
{
    public const string TsxVersionRequired = "4.23.15";
    public const string PlaywrightMcp = "/opt/homebrew/lib/node_modules/@playwright/mcp";
    public const string ChromeBinary = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";

    public const string GeneratedWireEntry = "tools/e2e/.wire/curious-contraptions/wire.ts";
    public const string GeneratedWireRecord = "tools/e2e/.wire/schemas.txt";
    public const string WireGeneratingProject = "CuriousContraptions.FlatBuffers/CuriousContraptions.FlatBuffers.csproj";

    /// <summary>Every part of the toolchain, or a typed ToolchainMissing refusal naming the missing part. Existence probes
    /// (File.Exists, Directory.Exists) report absence, which takes the named "missing" refusals; a read that throws an IO or
    /// access failure anywhere in the preflight (a manifest, a listing, the schema record, a write time, or a file gone between
    /// its probe and its read) takes the one refusal below, carrying the cause, which names the path.</summary>
    public static Toolchain Resolve(string home, string root)
    {
        try { return ResolveParts(home, root); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw Missing($"A toolchain input cannot be read ({exception.Message}); a Debug build of the solution (or of CuriousContraptions.FlatBuffers) regenerates the generated files and the schema record, and any other path that cannot be read must be restored, made readable or, where something else occupies a generated path, removed.");
        }
    }

    private static Toolchain ResolveParts(string home, string root)
    {
        var tsx = ResolveTsxLoader(home);
        RequireE2eRuntime(root);
        RequireGeneratedWireModule(root);
        var playwright = PackageVersion(Path.Combine(PlaywrightMcp, "node_modules", "playwright", "package.json"))
            ?? throw new RunnerException(RunnerError.ToolchainMissing, $"Playwright is not installed under {PlaywrightMcp} (npm install -g @playwright/mcp).");
        var node = Processes.Run("node", ["--version"]);
        if (node.ExitCode != 0) throw new RunnerException(RunnerError.ToolchainMissing, "node --version failed.");
        if (!File.Exists(ChromeBinary)) throw new RunnerException(RunnerError.ToolchainMissing, $"Google Chrome is not installed at {ChromeBinary}.");
        var chrome = Processes.Run(ChromeBinary, ["--version"]);
        if (chrome.ExitCode != 0) throw new RunnerException(RunnerError.ToolchainMissing, "Google Chrome --version failed.");
        return new Toolchain(tsx, TsxVersionRequired, playwright, node.Output.Trim(), chrome.Output.Trim());
    }

    /// <summary>The tsx loader of the exact required version in the npx cache (the first by ordinal path when several caches hold it).</summary>
    public static string ResolveTsxLoader(string home)
    {
        var cache = Path.Combine(home, ".npm", "_npx");
        var candidates = Directory.Exists(cache)
            ? Directory.EnumerateDirectories(cache).Order(StringComparer.Ordinal)
                .Select(directory => Path.Combine(directory, "node_modules", "tsx"))
                .Where(package => PackageVersion(Path.Combine(package, "package.json")) == TsxVersionRequired)
                .Select(package => Path.Combine(package, "dist", "loader.mjs")).Where(File.Exists).ToArray()
            : [];
        return candidates.Length > 0 ? candidates[0] : throw new RunnerException(RunnerError.ToolchainMissing,
            $"tsx {TsxVersionRequired} is not in {cache}. Install it once with: npx --yes --package=tsx@{TsxVersionRequired} tsx --version");
    }

    /// <summary>The version of the flatbuffers runtime the e2e suites import: installed by `npm ci` in tools/e2e at exactly the
    /// version tools/e2e/package.json pins, or a typed ToolchainMissing refusal (a copy elsewhere, such as above the checkout, would
    /// otherwise resolve instead).</summary>
    public static string RequireE2eRuntime(string root)
    {
        var e2e = Path.Combine(root, "tools", "e2e");
        var pinned = PinnedRuntime(Path.Combine(e2e, "package.json"));
        var installedManifest = Path.Combine(e2e, "node_modules", "flatbuffers", "package.json");
        var installed = PackageVersion(installedManifest);
        if (installed is null && File.Exists(installedManifest))
            throw Missing("tools/e2e/node_modules/flatbuffers/package.json has no version string. Reinstall it with: npm ci --prefix tools/e2e");
        return installed == pinned ? installed : throw new RunnerException(RunnerError.ToolchainMissing,
            $"flatbuffers {pinned}, pinned by tools/e2e/package.json, is not installed in tools/e2e/node_modules (found {installed ?? "none"}). Install it with: npm ci --prefix tools/e2e");
    }

    /// <summary>The generated wire entry module the suites import (S5b), which a Debug build writes: present, generated from
    /// exactly the current schemas/*.fbs file names (the build's record tools/e2e/.wire/schemas.txt, so a deleted, renamed or
    /// added schema is seen), and no older than every schemas/*.fbs file and the vendored FlatBuffers project file, both of
    /// which must exist; otherwise a typed refusal naming the module, the missing input or both name sets. A read that throws
    /// propagates to the one IO refusal of Resolve.</summary>
    public static void RequireGeneratedWireModule(string root)
    {
        var entry = Path.Combine(root, GeneratedWireEntry);
        if (!File.Exists(entry))
            throw new RunnerException(RunnerError.ToolchainMissing,
                $"The generated wire entry module {GeneratedWireEntry} is missing; a Debug build of the solution (or of CuriousContraptions.FlatBuffers) generates it.");
        var schemas = Path.Combine(root, "schemas");
        var noSchemas = $"No schemas/*.fbs file exists, so the generated wire entry module {GeneratedWireEntry} cannot be checked against its schemas.";
        if (!Directory.Exists(schemas)) throw Missing(noSchemas);
        var schemaFiles = Directory.GetFiles(schemas, "*.fbs");
        if (schemaFiles.Length == 0) throw Missing(noSchemas);
        var project = Path.Combine(root, WireGeneratingProject);
        if (!File.Exists(project))
            throw Missing($"{WireGeneratingProject} is missing, so the generated wire entry module {GeneratedWireEntry} cannot be checked against it.");
        var recorded = File.ReadAllLines(Path.Combine(root, GeneratedWireRecord)).Where(line => line.Length > 0).Order(StringComparer.Ordinal).ToArray();
        var current = schemaFiles.Select(path => Path.GetFileName(path)).Order(StringComparer.Ordinal).ToArray();
        if (!recorded.SequenceEqual(current, StringComparer.Ordinal))
            throw Missing($"The generated wire entry module {GeneratedWireEntry} was generated from the schemas [{string.Join(", ", recorded)}], but schemas/ holds [{string.Join(", ", current)}]; a Debug build of the solution (or of CuriousContraptions.FlatBuffers) regenerates it.");
        var newest = schemaFiles.Append(project).Select(WriteTime).Max();
        if (WriteTime(entry) < newest)
            throw Missing($"The generated wire entry module {GeneratedWireEntry} is older than a schemas/*.fbs file or {WireGeneratingProject}; a Debug build of the solution (or of CuriousContraptions.FlatBuffers) regenerates it.");
    }

    // A file's write time, read through an opened handle: a file gone since it was listed or a dangling link throws
    // FileNotFoundException and a directory in its place UnauthorizedAccessException, each reaching Resolve's one handler.
    private static DateTime WriteTime(string path)
    {
        using var handle = File.OpenHandle(path);
        return File.GetLastWriteTimeUtc(handle);
    }

    // The flatbuffers devDependency of tools/e2e/package.json, an exact x.y.z version that the installed runtime's version must
    // equal; a missing or malformed manifest, an absent entry, a value that is not a string and one that is not exactly x.y.z in
    // ASCII digits (a range, a prerelease or build suffix, surrounding whitespace) are each a typed refusal naming the reason.
    private static string PinnedRuntime(string packageJson)
    {
        if (!File.Exists(packageJson)) throw Missing("tools/e2e/package.json is missing, so the flatbuffers runtime version is unknown.");
        JsonDocument document;
        try { document = JsonDocument.Parse(File.ReadAllText(packageJson)); }
        catch (JsonException exception) { throw Missing($"tools/e2e/package.json is not valid JSON ({exception.Message})."); }
        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("devDependencies", out var dependencies) ||
                dependencies.ValueKind != JsonValueKind.Object || !dependencies.TryGetProperty("flatbuffers", out var version))
                throw Missing("tools/e2e/package.json pins no flatbuffers devDependency.");
            if (version.ValueKind != JsonValueKind.String) throw Missing($"tools/e2e/package.json pins flatbuffers as {version.GetRawText()}, not a version string.");
            var pin = version.GetString()!;
            return ExactVersion().IsMatch(pin) ? pin
                : throw Missing($"tools/e2e/package.json pins flatbuffers as {version.GetRawText()}, not an exact x.y.z version; the runner compares the installed version with the pin exactly, so the pin must be an exact x.y.z.");
        }
    }

    [GeneratedRegex(@"\A[0-9]+\.[0-9]+\.[0-9]+\z")]
    private static partial Regex ExactVersion();

    private static RunnerException Missing(string message) => new(RunnerError.ToolchainMissing, message);

    private static string? PackageVersion(string packageJson)
    {
        if (!File.Exists(packageJson)) return null;
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(packageJson));
            return document.RootElement.ValueKind == JsonValueKind.Object && document.RootElement.TryGetProperty("version", out var version) &&
                version.ValueKind == JsonValueKind.String ? version.GetString() : null;
        }
        catch (JsonException) { return null; }
    }
}
