using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using twodog.Testing;

namespace CuriousContraptions.Tests;

// Raw names below are external Godot, filesystem, and environment boundaries.
internal static class NativeTestBoundary
{
    internal const string ProjectFile = "project.godot";
    internal const string OverrideFile = "override.cfg";
    internal const string GitDirectory = ".git";
    internal const string RunDirectoryPrefix = "curious-native-tests-";
    internal const string ProjectDirectory = "project";
    internal const string LogFile = "godot.log";
    internal const string CertificateEnvironment = "CURIOUS_NATIVE_TEST_CA_BUNDLE";
    internal const string MacOsCertificateBundle = "/etc/ssl/cert.pem";
    internal const string CertificateSetting = "network/tls/certificate_bundle_override";
    internal const string ResourceRoot = "res://";
    internal const string CampaignRelativePath = "content/puzzles.json";
    internal const string ProjectResource = "res://project.godot";
    internal const string CampaignResource = "res://content/puzzles.json";
    internal const string WorkshopResource = "res://scenes/workshop.tscn";
    internal const string CertificateSection = "[network]";
    internal const string CertificateProperty = "tls/certificate_bundle_override";
    internal static string Argument(NativeStartupArgument argument) => argument switch
    {
        NativeStartupArgument.Headless => "--headless",
        NativeStartupArgument.ProjectPath => "--path",
        NativeStartupArgument.LogFile => "--log-file",
        _ => throw new ArgumentOutOfRangeException(nameof(argument))
    };
}

internal enum NativeStartupArgument { Headless, ProjectPath, LogFile }

internal readonly record struct NativeTestPath
{
    internal string Value { get; }
    internal NativeTestPath(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (!Path.IsPathFullyQualified(value))
            throw new ArgumentException("Native test paths must be absolute.", nameof(value));
        Value = Path.GetFullPath(value);
    }

    internal NativeTestPath Child(string fileName) => new(Path.Combine(Value, fileName));
}

/// <summary>
/// A unique startup project links the exact source resources and settings. Only native-test TLS
/// and log destinations differ. Artifacts intentionally survive disposal for failure diagnosis.
/// </summary>
internal sealed class NativeTestEnvironment
{
    internal NativeTestPath Source { get; }
    internal NativeTestPath Project { get; }
    internal NativeTestPath Log { get; }
    internal NativeTestPath? CertificateBundle { get; }
    internal string[] Arguments =>
    [
        NativeTestBoundary.Argument(NativeStartupArgument.Headless),
        NativeTestBoundary.Argument(NativeStartupArgument.ProjectPath), Project.Value,
        NativeTestBoundary.Argument(NativeStartupArgument.LogFile), Log.Value
    ];

    private NativeTestEnvironment(NativeTestPath source, NativeTestPath project,
        NativeTestPath log, NativeTestPath? certificateBundle)
    {
        Source = source;
        Project = project;
        Log = log;
        CertificateBundle = certificateBundle;
    }

    internal static NativeTestEnvironment Prepare()
    {
        var explicitBundle = Environment.GetEnvironmentVariable(NativeTestBoundary.CertificateEnvironment);
        NativeTestPath? bundle = explicitBundle is null
            ? (OperatingSystem.IsMacOS() ? new(NativeTestBoundary.MacOsCertificateBundle) : null)
            : new(explicitBundle);
        return Prepare(new(twodog.Engine.ResolveProjectDir()), bundle);
    }

    internal static NativeTestEnvironment Prepare(NativeTestPath source, NativeTestPath? certificateBundle)
    {
        if (!File.Exists(source.Child(NativeTestBoundary.ProjectFile).Value))
            throw new FileNotFoundException("Native tests require the current production project settings.");
        // Do not silently discard an independently authored project override.
        if (File.Exists(source.Child(NativeTestBoundary.OverrideFile).Value))
            throw new InvalidOperationException("The native test host does not support a source override.cfg.");
        if (certificateBundle is { } bundle) ValidateCertificateBundle(bundle);

        var run = new NativeTestPath(Path.Combine(Path.GetTempPath(),
            NativeTestBoundary.RunDirectoryPrefix + Guid.NewGuid().ToString("N")));
        var project = run.Child(NativeTestBoundary.ProjectDirectory);
        Directory.CreateDirectory(project.Value);
        foreach (var entry in Directory.EnumerateFileSystemEntries(source.Value))
        {
            var name = Path.GetFileName(entry);
            if (name == NativeTestBoundary.GitDirectory) continue;
            var target = project.Child(name);
            if (Directory.Exists(entry)) Directory.CreateSymbolicLink(target.Value, entry);
            else File.CreateSymbolicLink(target.Value, entry);
        }

        if (certificateBundle is { } certificate)
        {
            // Godot ConfigFile string encoding. This is an external serialization boundary.
            var quotedPath = System.Text.Json.JsonSerializer.Serialize(certificate.Value);
            File.WriteAllText(project.Child(NativeTestBoundary.OverrideFile).Value,
                NativeTestBoundary.CertificateSection + Environment.NewLine +
                NativeTestBoundary.CertificateProperty + "=" + quotedPath + Environment.NewLine);
        }
        var environment = new NativeTestEnvironment(source, project,
            run.Child(NativeTestBoundary.LogFile), certificateBundle);
        Console.WriteLine("Native test project: " + environment.Project.Value);
        Console.WriteLine("Native test log: " + environment.Log.Value);
        Console.WriteLine("Native test CA bundle: " + (certificateBundle?.Value ?? "operating-system trust"));
        return environment;
    }

    internal static void ValidateCertificateBundle(NativeTestPath bundle)
    {
        if (!File.Exists(bundle.Value))
            throw new FileNotFoundException("The explicit native-test CA bundle does not exist.", bundle.Value);
        var certificates = new X509Certificate2Collection();
        try
        {
            certificates.ImportFromPemFile(bundle.Value);
            if (certificates.Count == 0)
                throw new CryptographicException("The native-test CA bundle contains no certificates.");
        }
        finally
        {
            foreach (var certificate in certificates) certificate.Dispose();
        }
    }
}

public sealed class NativeSceneFixture : FixtureBase
{
    internal NativeTestEnvironment Environment { get; }
    public NativeSceneFixture() : this(NativeTestEnvironment.Prepare()) { }
    private NativeSceneFixture(NativeTestEnvironment environment) : base(environment.Arguments)
        => Environment = environment;
}

[CollectionDefinition(DisableParallelization = true)]
public sealed class NativeSceneCollection : ICollectionFixture<NativeSceneFixture>;
