namespace CuriousContraptions.E2eRunner;

/// <summary>Audited build outputs, not Git ignore rules. Tracked changes never reach these exemptions.</summary>
internal sealed class ArtifactGeneratedPaths
{
    // SDK/Godot/npm/runtime build caches and the web host's exact publish output. Not arbitrary similarly named directories.
    private static readonly string[] RootDirectories =
        [".anvil", ".godot", ".tools", ".runtime-build", "node_modules", "TestResults", "CuriousContraptions.web/AppBundle"];
    // Exact outputs declared by tools/runtime/RuntimeTypeScript.targets and tools/BundleElements/BundleElements.csproj.
    private static readonly HashSet<string> Files = new(StringComparer.Ordinal)
    {
        "elements/elements.bundle.json",
        "CuriousContraptions.Simulation/wwwroot/worker.js",
        "CuriousContraptions.Simulation/wwwroot/clock-master.js",
        "CuriousContraptions.Animation.Worker/wwwroot/worker.js",
        "CuriousContraptions.web/wwwroot/event-ring-cursor.js",
        "CuriousContraptions.web/wwwroot/workshop-client.js",
        "CuriousContraptions.web/wwwroot/workshop-observation.js",
        "CuriousContraptions.web/wwwroot/clock-ring.js",
        "CuriousContraptions.web/wwwroot/native-clock.js",
        "CuriousContraptions.web/wwwroot/pose-ring.js",
        "CuriousContraptions.web/wwwroot/workshop-isolation.js",
        "CuriousContraptions.web/wwwroot/workshop-isolation-worker.js",
        "CuriousContraptions.web/wwwroot/workshop-bootstrap.js",
    };
    private readonly string[] _directories;

    public ArtifactGeneratedPaths(IEnumerable<string> tracked)
    {
        // Current projects use the SDK's immediate bin/obj output exclusions; explicit root compile lists omit them too.
        var projectOutputs = tracked.Where(path => path.EndsWith(".csproj", StringComparison.Ordinal)).SelectMany(path =>
        {
            var slash = path.LastIndexOf('/');
            var prefix = slash < 0 ? "" : path[..(slash + 1)];
            return new[] { prefix + "bin", prefix + "obj" };
        });
        _directories = [.. RootDirectories.Concat(projectOutputs).Distinct(StringComparer.Ordinal)];
    }

    public string? DirectoryOf(string path) =>
        _directories.FirstOrDefault(directory => path == directory || path.StartsWith(directory + "/", StringComparison.Ordinal));

    public static bool IsFile(string path) => Files.Contains(path);
}
