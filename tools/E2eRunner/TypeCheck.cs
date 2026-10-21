namespace CuriousContraptions.E2eRunner;

/// <summary>Pinned compiler and Node declarations from the installed toolchain; resolution never downloads or skips.</summary>
internal sealed partial record Toolchain
{
    public const string TypeScriptVersionRequired = "6.0.3";
    public const string NodeTypesVersionRequired = "26.0.1";

    public static string ResolveTypeScript(string home) =>
        Path.Combine(ResolveCachedPackage(home, "typescript", TypeScriptVersionRequired), "bin", "tsc");

    public static string ResolveNodeTypes(string home) =>
        Path.GetDirectoryName(ResolveCachedPackage(home, "@types/node", NodeTypesVersionRequired))!;

    private static string ResolveCachedPackage(string home, string name, string version)
    {
        var cache = Path.Combine(home, ".npm", "_npx");
        var package = Directory.Exists(cache)
            ? Directory.EnumerateDirectories(cache).Order(StringComparer.Ordinal)
                .Select(directory => Path.Combine(directory, "node_modules", name))
                .FirstOrDefault(directory => PackageVersion(Path.Combine(directory, "package.json")) == version)
            : null;
        return package ?? throw new RunnerException(RunnerError.ToolchainMissing,
            $"Required {name} {version} is absent from {cache}; install the pinned test toolchain before Tier 1.");
    }
}

/// <summary>Runs the repository's strict no-emit project through the external TypeScript boundary.</summary>
internal static class TypeCheck
{
    public const string Project = "tools/e2e/tsconfig.json";

    public static ProcessResult Run(string compiler, string root, string project)
    {
        if (!File.Exists(compiler))
            throw new RunnerException(RunnerError.ToolchainMissing, $"TypeScript compiler is missing: {compiler}.");
        var home = Environment.GetEnvironmentVariable("HOME") ?? throw new InvalidOperationException("HOME is not set.");
        return Processes.Run("node", [compiler, "--noEmit", "--project", project, "--typeRoots", Toolchain.ResolveNodeTypes(home)], root);
    }
}
