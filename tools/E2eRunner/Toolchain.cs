using System.Text.Json;

namespace CuriousContraptions.E2eRunner;

/// <summary>The resolved browser-test toolchain, recorded in every summary. Resolution never downloads anything.</summary>
internal sealed partial record Toolchain(string TsxLoader, string TsxVersion, string PlaywrightVersion, string NodeVersion, string ChromeVersion)
{
    public const string TsxVersionRequired = "4.23.15";
    public const string PlaywrightMcp = "/opt/homebrew/lib/node_modules/@playwright/mcp";
    public const string ChromeBinary = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";

    /// <summary>Every part of the toolchain, or a typed ToolchainMissing refusal naming the missing part.</summary>
    public static Toolchain Resolve(string home)
    {
        var tsx = ResolveTsxLoader(home);
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

    private static string? PackageVersion(string packageJson)
    {
        if (!File.Exists(packageJson)) return null;
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(packageJson));
            return document.RootElement.TryGetProperty("version", out var version) ? version.GetString() : null;
        }
        catch (JsonException) { return null; }
    }
}
