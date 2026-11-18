using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CuriousContraptions.E2eRunner;

/// <summary>CI provenance and the existing bundle identity; no second file manifest.</summary>
internal sealed record ArtifactReceipt(string SourceRevision, string CiRun, string CiAttempt, string BundleIdentity);

/// <summary>The only admission boundary for a published browser artifact.</summary>
internal sealed record PublishedArtifact(string Directory, ArtifactReceipt Receipt)
{
    public const string ReceiptName = "artifact-receipt.json";
    public string BundleDirectory => Path.Combine(Directory, "AppBundle");
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true,
    };
    private static readonly string[] RequiredFiles = ["index.html", "404.html", "godot.wasm", "godot.pck", "_framework/dotnet.js", ".nojekyll"];

    public static PublishedArtifact Admit(string root, string directory)
    {
        try
        {
            var full = Path.GetFullPath(directory);
            ArtifactPaths.RequireTree(full);
            var receipt = JsonSerializer.Deserialize<ArtifactReceipt>(File.ReadAllText(Path.Combine(full, ReceiptName)), JsonOptions)
                ?? throw Invalid("The artifact receipt is null.");
            RequireReceipt(receipt);
            var artifact = new PublishedArtifact(full, receipt);
            artifact.Verify(root);
            return artifact;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            throw Invalid($"Cannot admit artifact: {error.Message}");
        }
    }

    public static PublishedArtifact Record(string root, string directory, string revision, string run, string attempt)
    {
        var artifact = new PublishedArtifact(Path.GetFullPath(directory), new ArtifactReceipt(revision, run, attempt, ""));
        RequireProvenance(artifact.Receipt);
        ArtifactSource.RequireApplicable(root, revision);
        ArtifactPaths.RequireTree(artifact.Directory);
        artifact.RequireFiles();
        artifact = artifact with { Receipt = artifact.Receipt with { BundleIdentity = BundleManifest.Compute(artifact.BundleDirectory).Identity } };
        File.WriteAllText(Path.Combine(artifact.Directory, ReceiptName), JsonSerializer.Serialize(artifact.Receipt, JsonOptions));
        return artifact;
    }

    public void Verify(string root)
    {
        try { VerifyCore(root); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
        {
            throw Invalid($"Cannot verify artifact: {error.Message}");
        }
    }

    private void VerifyCore(string root)
    {
        ArtifactPaths.RequireTree(Directory);
        RequireFiles();
        ArtifactSource.RequireApplicable(root, Receipt.SourceRevision);
        if (BundleManifest.Compute(BundleDirectory).Identity != Receipt.BundleIdentity)
            throw Invalid("The published bundle differs from its CI receipt.");
    }

    private void RequireFiles()
    {
        foreach (var relative in RequiredFiles)
        {
            var file = new FileInfo(Path.Combine(BundleDirectory, relative));
            if (!file.Exists || (relative != ".nojekyll" && file.Length == 0))
                throw Invalid($"Missing or empty required bundle file: {relative}.");
        }
    }

    private static void RequireReceipt(ArtifactReceipt receipt)
    {
        RequireProvenance(receipt);
        if (!Hex(receipt.BundleIdentity, 64)) throw Invalid("Invalid bundle identity.");
    }

    private static void RequireProvenance(ArtifactReceipt receipt)
    {
        if (!Hex(receipt.SourceRevision, 40) || !Positive(receipt.CiRun) || !Positive(receipt.CiAttempt))
            throw Invalid("Receipt requires a full source commit and positive CI run/attempt.");
    }

    private static bool Positive(string value) =>
        long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number > 0;

    private static bool Hex(string value, int length) =>
        value is not null && value.Length == length && value.All(char.IsAsciiHexDigit);

    internal static RunnerException Invalid(string detail) => new(RunnerError.InvalidArtifact, detail);
}

/// <summary>Reject links before recursive reads, including links in ancestor directories.</summary>
internal static class ArtifactPaths
{
    public static void RequireTree(string root)
    {
        RequirePath(root);
        if (!System.IO.Directory.Exists(root)) throw PublishedArtifact.Invalid($"Missing artifact directory: {root}.");
        foreach (var entry in System.IO.Directory.EnumerateFileSystemEntries(root))
        {
            RequirePath(entry);
            if (System.IO.Directory.Exists(entry)) RequireTree(entry);
        }
    }

    public static void RequirePath(string path)
    {
        for (var current = new DirectoryInfo(Path.GetFullPath(path)); current is not null; current = current.Parent)
            if ((File.GetAttributes(current.FullName) & FileAttributes.ReparsePoint) != 0)
                throw PublishedArtifact.Invalid($"Symbolic links are not admitted: {current.FullName}.");
    }
}
