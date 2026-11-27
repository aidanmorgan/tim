using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CuriousContraptions.E2eRunner;

/// <summary>The resolved browser-test toolchain, recorded in every summary. Resolution never downloads anything.</summary>
internal sealed partial record Toolchain(string TsxLoader, string TsxVersion, string PlaywrightVersion, string NodeVersion, string ChromeVersion, IReadOnlyList<string> AdmittedWireSources)
{
    public const string TsxVersionRequired = "4.23.15";
    public const string PlaywrightMcp = "/opt/homebrew/lib/node_modules/@playwright/mcp";
    public const string ChromeBinary = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";

    private const string AbstractionsProject = "CuriousContraptions.Abstractions/CuriousContraptions.Abstractions.csproj";
    private const string AdmittedSourceCountProperty = "AdmittedWireSourceCount";
    private const string AdmissionTarget = "AdmitRuntimeTypeScriptArtifacts";
    private const string GeneratedWireNamespace = "tools/e2e/.wire/curious-contraptions/";
    private const string GeneratedWireEntry = GeneratedWireNamespace + "wire.ts";

    /// <summary>Resolve the browser tools and the admitted wire paths. Report unreadable inputs as a typed toolchain refusal.</summary>
    public static Toolchain Resolve(string home, string root)
    {
        try { return ResolveParts(home, root); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw Missing($"A toolchain input cannot be read ({exception.Message}); a build of the solution (or of CuriousContraptions.Abstractions) regenerates the generated files, and any other path that cannot be read must be restored, made readable or, where something else occupies a generated path, removed.");
        }
    }

    private static Toolchain ResolveParts(string home, string root)
    {
        var tsx = ResolveTsxLoader(home);
        var admittedWireSources = AdmitRuntimeTypeScriptArtifacts(root);
        var playwright = PackageVersion(Path.Combine(PlaywrightMcp, "node_modules", "playwright", "package.json"))
            ?? throw new RunnerException(RunnerError.ToolchainMissing, $"Playwright is not installed under {PlaywrightMcp} (npm install -g @playwright/mcp).");
        var node = Processes.Run("node", ["--version"]);
        if (node.ExitCode != 0) throw new RunnerException(RunnerError.ToolchainMissing, "node --version failed.");
        if (!File.Exists(ChromeBinary)) throw new RunnerException(RunnerError.ToolchainMissing, $"Google Chrome is not installed at {ChromeBinary}.");
        var chrome = Processes.Run(ChromeBinary, ["--version"]);
        if (chrome.ExitCode != 0) throw new RunnerException(RunnerError.ToolchainMissing, "Google Chrome --version failed.");
        return new Toolchain(tsx, TsxVersionRequired, playwright, node.Output.Trim(), chrome.Output.Trim(), admittedWireSources);
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

    /// <summary>Read-only admission through the same Abstractions targets the build uses before tsc.</summary>
    public static IReadOnlyList<string> AdmitRuntimeTypeScriptArtifacts(string root)
    {
        try
        {
            var result = Processes.Run("dotnet",
                ["msbuild", AbstractionsProject, "-getTargetResult:" + AdmissionTarget,
                    "-getProperty:" + AdmittedSourceCountProperty, "-nologo", "-noAutoResponse", "-v:q"], root);
            if (result.ExitCode != 0)
                throw Missing($"Runtime TypeScript admission failed: {result.Error.Trim()} {result.Output.Trim()}".TrimEnd());
            return ReadAdmittedWireSources(result.Output);
        }
        catch (RunnerException exception) when (exception.Error != RunnerError.ToolchainMissing)
        {
            throw Missing($"Runtime TypeScript admission could not run: {exception.Message}");
        }
    }

    internal static IReadOnlyList<string> ReadAdmittedWireSources(string output)
    {
        try
        {
            using var document = JsonDocument.Parse(output);
            var target = document.RootElement.GetProperty("TargetResults").GetProperty(AdmissionTarget);
            if (target.GetProperty("Result").GetString() != "Success")
                throw Missing("Runtime TypeScript admission did not report a successful target.");
            var sources = target.GetProperty("Items").EnumerateArray().Select(ReadWireIdentity).ToArray();
            var countText = document.RootElement.GetProperty("Properties").GetProperty(AdmittedSourceCountProperty).GetString();
            if (!int.TryParse(countText, NumberStyles.None, CultureInfo.InvariantCulture, out var count) ||
                count < 2 || sources.Length != count)
                throw Missing("Runtime TypeScript admission returned an incomplete generated wire source list.");
            if (!sources.Contains(GeneratedWireEntry, StringComparer.Ordinal))
                throw Missing("Runtime TypeScript admission requires the generated wire entry and its leaves.");
            if (sources.Distinct(StringComparer.Ordinal).Count() != sources.Length)
                throw Missing("Runtime TypeScript admission returned duplicate wire sources.");
            return sources;
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or KeyNotFoundException)
        {
            throw Missing($"Runtime TypeScript admission returned an invalid MSBuild result: {exception.Message}");
        }
    }

    private static string ReadWireIdentity(JsonElement item)
    {
        var identity = item.GetProperty("Identity").GetString();
        if (string.IsNullOrWhiteSpace(identity))
            throw Missing("Runtime TypeScript admission returned a missing wire source identity.");
        if (identity != GeneratedWireEntry && !WireLeafIdentity().IsMatch(identity))
            throw Missing("Runtime TypeScript admission returned a noncanonical generated wire source identity.");
        return identity;
    }

    [GeneratedRegex(@"\Atools/e2e/\.wire/curious-contraptions/wire/[a-z0-9-]+\.ts\z", RegexOptions.CultureInvariant)]
    private static partial Regex WireLeafIdentity();

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
