using System.Globalization;
using System.Text.RegularExpressions;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>The browser scripts read and write records whose layouts the C# ABI owns. Every layout constant a script
/// declares must equal its C# owner and must be used, so a change on either side fails here instead of silently
/// misreading committed bodies or poses, and no declared-but-dead layout survives (Story 9.4b deleted twenty).</summary>
public sealed class WorkerAbiTests(ITestOutputHelper output)
{
    public enum Script { SimulationWorker, WorkshopClient }

    // Story 9R.5b: the ring's body capacity is its own length, so no script declares a capacity or a ring width; the browser
    // alone declares the bootstrap ring's starting allocation.
    private static readonly IReadOnlyDictionary<string, int> PoseRing = new Dictionary<string, int>
    {
        ["POSE_SLOT_COUNT"] = WorkshopPoseRing.SlotCount,
        ["POSE_HEADER_BYTES"] = WorkshopPoseRing.HeaderBytes,
        ["POSE_BODY_BYTES"] = WorkshopPoseRing.BodyBytes,
    };

    private static Dictionary<string, int> Owned(Script script) => script switch
    {
        // Story 9A.2: the worker reads no read layout; it copies the C#-composed pose slot, so it declares only the ring's own.
        Script.SimulationWorker => new Dictionary<string, int>(PoseRing),
        Script.WorkshopClient => new Dictionary<string, int>(PoseRing) { ["POSE_INITIAL_BODIES"] = WorkshopPoseRing.InitialBodyCapacity },
        _ => throw new ArgumentOutOfRangeException(nameof(script), script, "Unknown browser script."),
    };

    private static string Path(Script script) => System.IO.Path.Combine(twodog.Engine.ResolveProjectDir(), script switch
    {
        Script.SimulationWorker => "CuriousContraptions.Simulation/wwwroot/worker.js",
        Script.WorkshopClient => "CuriousContraptions.web/wwwroot/workshop-client.js",
        _ => throw new ArgumentOutOfRangeException(nameof(script), script, "Unknown browser script."),
    });

    [Theory]
    [InlineData(Script.SimulationWorker)]
    [InlineData(Script.WorkshopClient)]
    public void EveryScriptLayoutConstantEqualsItsCSharpOwnerAndIsUsed(Script script)
    {
        var path = Path(script);
        output.WriteLine($"Script checked: {path}");
        var source = File.ReadAllText(path);
        // Every upper-case numeric layout constant the script declares, so an unpinned or dead one fails too.
        var declared = Regex.Matches(source, @"^(?:const|let) ([A-Z][A-Z0-9_]+) = (\d+);", RegexOptions.Multiline)
            .ToDictionary(match => match.Groups[1].Value, match => int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture));
        Assert.Equal(Owned(script).OrderBy(entry => entry.Key), declared.OrderBy(entry => entry.Key));
        foreach (var name in declared.Keys)
            Assert.True(Regex.Count(source, $@"\b{name}\b") > 1, $"{name} is declared but never used in {path}.");
    }
}
