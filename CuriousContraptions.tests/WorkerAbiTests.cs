using System.Globalization;
using System.Text.RegularExpressions;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>The browser scripts read and write records whose layouts the C# ABI owns. Every layout constant a script
/// declares must equal its C# owner and must be used, so a change on either side fails here instead of silently
/// misreading committed bodies or poses, and no declared-but-dead layout survives (Story 9.4b deleted twenty).</summary>
public sealed class WorkerAbiTests(ITestOutputHelper output)
{
    public enum Script { SimulationWorker, WorkshopClient, EventRingCursor }

    // Story 9R.5b: the ring's body capacity is its own length, so no script declares a capacity or a ring width; the browser
    // alone declares the bootstrap ring's starting allocation.
    private static readonly IReadOnlyDictionary<string, int> PoseRing = new Dictionary<string, int>
    {
        ["POSE_SLOT_COUNT"] = WorkshopPoseRing.SlotCount,
        ["POSE_HEADER_BYTES"] = WorkshopPoseRing.HeaderBytes,
        ["POSE_BODY_BYTES"] = WorkshopPoseRing.BodyBytes,
    };

    // Story 9A.22: the worker copies each C#-composed event ring publication; it declares the slot and header sizes and the header
    // words it reads or stores. The ring's capacity is the publication's own (sized from the scene), never a script constant.
    private static readonly IReadOnlyDictionary<string, int> EventRing = new Dictionary<string, int>
    {
        ["EVENT_HEADER_BYTES"] = Abstractions.EventRingBuffer.HeaderBytes,
        ["EVENT_SLOT_BYTES"] = Abstractions.EventRingBuffer.SlotBytes,
        ["EVENT_CAPACITY_OFFSET"] = Abstractions.EventRingHeader.CapacityOffset,
        ["EVENT_EPOCH_OFFSET"] = Abstractions.EventRingHeader.EpochOffset,
        ["EVENT_WRITE_SEQUENCE_OFFSET"] = Abstractions.EventRingHeader.WriteSequenceOffset,
        ["EVENT_LAPPED_OFFSET"] = Abstractions.EventRingHeader.LappedOffset,
    };

    private static Dictionary<string, int> Owned(Script script) => script switch
    {
        // Story 9A.2: the worker reads no read layout; it copies the C#-composed pose slot, so it declares only the ring's own.
        Script.SimulationWorker => new Dictionary<string, int>(PoseRing.Concat(EventRing)),
        Script.WorkshopClient => new Dictionary<string, int>(PoseRing)
        {
            ["POSE_INITIAL_BODIES"] = WorkshopPoseRing.InitialBodyCapacity,
            // Story 9A.44 D7: the Animation output fields the client decodes (the f32 value at 56, the u16 property at 136).
            ["ANIMATION_OUTPUT_BYTES"] = WorkshopAnimationWire.OutputBytes,
            ["ANIMATION_OUTPUT_PULSE_OFFSET"] = WorkshopAnimationWire.OutputPulseOffset,
            ["ANIMATION_OUTPUT_VALUE_OFFSET"] = WorkshopAnimationWire.OutputValueOffset,
            ["ANIMATION_OUTPUT_KIND_OFFSET"] = WorkshopAnimationWire.OutputKindOffset,
            ["ANIMATION_OUTPUT_TARGET_OFFSET"] = WorkshopAnimationWire.OutputTargetOffset,
            ["ANIMATION_OUTPUT_PROPERTY_OFFSET"] = WorkshopAnimationWire.OutputPropertyOffset,
        },
        // Story 9A.23: the ring's memory (EventRingMemory) owns every header word it stores, validates and loads; the shared reader
        // hands C# the delivery header.
        Script.EventRingCursor => new Dictionary<string, int>
        {
            ["EVENT_HEADER_BYTES"] = Abstractions.EventRingBuffer.HeaderBytes, ["EVENT_SLOT_BYTES"] = Abstractions.EventRingBuffer.SlotBytes,
            ["EVENT_CAPACITY_OFFSET"] = Abstractions.EventRingHeader.CapacityOffset, ["EVENT_SLOT_BYTES_OFFSET"] = Abstractions.EventRingHeader.SlotBytesOffset,
            ["EVENT_EPOCH_OFFSET"] = Abstractions.EventRingHeader.EpochOffset, ["EVENT_WRITE_SEQUENCE_OFFSET"] = Abstractions.EventRingHeader.WriteSequenceOffset,
            ["EVENT_LAPPED_OFFSET"] = Abstractions.EventRingHeader.LappedOffset,
            ["EVENT_DELIVERY_BYTES"] = Abstractions.EventRingDelivery.HeaderBytes,
        },
        _ => throw new ArgumentOutOfRangeException(nameof(script), script, "Unknown browser script."),
    };

    private static string Path(Script script) => System.IO.Path.Combine(twodog.Engine.ResolveProjectDir(), script switch
    {
        Script.SimulationWorker => "CuriousContraptions.Simulation/wwwroot/worker.js",
        Script.WorkshopClient => "CuriousContraptions.web/wwwroot/workshop-client.js",
        Script.EventRingCursor => "CuriousContraptions.web/wwwroot/event-ring-cursor.js",
        _ => throw new ArgumentOutOfRangeException(nameof(script), script, "Unknown browser script."),
    });

    [Theory]
    [InlineData(Script.SimulationWorker)]
    [InlineData(Script.WorkshopClient)]
    [InlineData(Script.EventRingCursor)]
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
