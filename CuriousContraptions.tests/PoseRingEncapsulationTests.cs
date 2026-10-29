using System.Text.RegularExpressions;

namespace CuriousContraptions.Tests;

/// <summary>Named architecture guard. EventRingConsumerTests inventories all views in the shared callers;
/// this guard owns the pose-specific receivers and prevents shared views from escaping the producer/reader.</summary>
public sealed class PoseRingEncapsulationTests
{
    private static readonly string[] Owners = ["CuriousContraptions.Simulation/wwwroot/worker.ts", "CuriousContraptions.web/wwwroot/pose-ring.ts",
        "CuriousContraptions.web/wwwroot/workshop-client.ts"];

    private static bool BypassesPoseApi(string source) =>
        Regex.IsMatch(source, @"new (?:\w+Array|DataView)\([^\r\n]*(?:poseRing|poses\.)")
        || Regex.IsMatch(source, @"Atomics\.\w+\([^\r\n]*pose", RegexOptions.IgnoreCase);

    [Fact]
    public void GuardPoseMemoryIsOwnedOnlyByItsTypedProducerAndReader()
    {
        var root = twodog.Engine.ResolveProjectDir();
        string[] folders = ["CuriousContraptions.Simulation/wwwroot", "CuriousContraptions.Animation.Worker/wwwroot", "CuriousContraptions.web/wwwroot"];
        var receivers = folders.SelectMany(folder => Directory.EnumerateFiles(Path.Combine(root, folder), "*.ts"))
            .Where(path => Regex.IsMatch(File.ReadAllText(path), @"\bposeRing\b"))
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/')).Order().ToArray();
        Assert.Equal(Owners.Order(), receivers);
        foreach (var path in receivers.Where(path => !path.EndsWith("/pose-ring.ts", StringComparison.Ordinal)))
            Assert.False(BypassesPoseApi(File.ReadAllText(Path.Combine(root, path))), path);
        var module = File.ReadAllText(Path.Combine(root, "CuriousContraptions.web/wwwroot/pose-ring.ts"));
        Assert.Contains("export class PoseProducer", module);
        Assert.Contains("export class PoseReader", module);
        Assert.DoesNotContain("get buffer(", module);
        Assert.Contains("readonly #sequences:", module);
        Assert.Contains("#poseStorage:", module);
        // Positive controls prove the guard sees a direct read and atomic bypass without running mutated code.
        Assert.True(BypassesPoseApi("new DataView(message.poseRing)"));
        Assert.True(BypassesPoseApi("Atomics.load(client.poseSequences, 0)"));
        Assert.False(BypassesPoseApi("new PoseReader(message.poseRing)"));
    }
}
