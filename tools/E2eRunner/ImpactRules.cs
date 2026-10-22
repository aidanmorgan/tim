namespace CuriousContraptions.E2eRunner;

/// <summary>What a changed path affects (spec ImpactRules task). Each path resolves to exactly one kind.</summary>
internal enum ImpactKind { NoBrowserImpact = 1, Suite, SharedTestModule, Element, Level, WorkshopArea, PackageBuild, Catalogue, Full }

/// <summary>Why a path selects the full run (the guide's Tier 3 triggers, plus central interaction files and unknown paths).</summary>
internal enum FullReason { MaterialEngine = 1, SharedRuntime, SharedDriverInfrastructure, AllInteraction, RunnerItself, Build, Unknown }

/// <summary>One path-classification rule: a name (reported in reasons), its kind and its path predicate.</summary>
internal sealed record PathRule(string Name, ImpactKind Kind, Func<string, bool> Matches, FullReason? Full = null);

internal static class ImpactRules
{
    /// <summary>elements/&lt;Dir&gt; package directories to their part. A test checks it against the directory listing and the bundle.</summary>
    public static readonly IReadOnlyDictionary<string, PartId> ElementPackages = new Dictionary<string, PartId>(StringComparer.Ordinal)
    {
        ["Ball"] = PartId.Basketball, ["BallDetector"] = PartId.BallDetector, ["Battery"] = PartId.Battery,
        ["BothGate"] = new("both_gate"), ["BowlingBall"] = PartId.BowlingBall, ["Bumper"] = PartId.PinballBumper, ["Counter"] = PartId.Counter,
        ["Domino"] = PartId.Domino, ["DelayBox"] = PartId.Delay, ["Funnel"] = PartId.Funnel, ["HoldTimer"] = new("hold_timer"),
        ["ImpactSwitch"] = PartId.ImpactSwitch, ["Pipe"] = PartId.Pipe,
        ["PipeBend45"] = PartId.PipeBend45, ["PipeBend90"] = PartId.PipeBend90,
        ["PoweredGate"] = PartId.PoweredGate, ["PressurePlate"] = PartId.PressurePlate, ["Ramp"] = PartId.Ramp,
        ["Receiver"] = PartId.Receiver, ["SignalLamp"] = PartId.SignalLamp, ["Springboard"] = PartId.Springboard,
        ["StateLatch"] = PartId.Latch, ["SystemClock"] = PartId.Clock, ["Wall"] = PartId.Wall,
    };

    /// <summary>Every engine/ (outside engine/gpu), ui/ and scenes/ source to its Workshop area. A file's Godot .uid sidecar follows
    /// its source. A test checks the table against `git ls-files engine ui scenes`; a missing file is Unknown, so Full.</summary>
    public static readonly IReadOnlyDictionary<string, WorkshopArea> AreaFiles = new Dictionary<string, WorkshopArea>(StringComparer.Ordinal)
    {
        ["engine/MachineWorld.cs"] = WorkshopArea.AllInteraction,
        ["engine/MachineWorld.Gpu.cs"] = WorkshopArea.AllInteraction,
        ["engine/WorkshopUiBinding.cs"] = WorkshopArea.AllInteraction,
        ["engine/MachinePart.cs"] = WorkshopArea.AllInteraction,
        ["ui/Workshop.cs"] = WorkshopArea.AllInteraction,
        ["ui/WorkshopGpu.cs"] = WorkshopArea.AllInteraction,
        ["ui/WorkshopUiFailure.cs"] = WorkshopArea.AllInteraction,
        ["scenes/workshop.tscn"] = WorkshopArea.AllInteraction,
        ["engine/PartDefinition.cs"] = WorkshopArea.Palette,
        ["engine/PartRegistry.cs"] = WorkshopArea.Palette,
        ["engine/PartId.cs"] = WorkshopArea.Palette,
        ["ui/WorkshopIcons.cs"] = WorkshopArea.Palette,
        ["engine/Workbench.cs"] = WorkshopArea.BenchPlacement,
        ["ui/WorkshopBenchPlacement.cs"] = WorkshopArea.BenchPlacement,
        ["ui/PlacementShadows.cs"] = WorkshopArea.BenchPlacement,
        ["engine/TubeMouthSnap.cs"] = WorkshopArea.BenchPlacement,
        ["ui/RotationGizmo.cs"] = WorkshopArea.Gizmo,
        ["engine/IResizablePart.cs"] = WorkshopArea.Gizmo,
        ["ui/WorkshopGuidance.cs"] = WorkshopArea.Guidance,
        ["ui/WorkshopConnections.cs"] = WorkshopArea.Connections,
        ["ui/WorkshopConfiguration.cs"] = WorkshopArea.Configuration,
        ["ui/WorkshopBattery.cs"] = WorkshopArea.Configuration,
        ["ui/WorkshopPuzzle.cs"] = WorkshopArea.LevelPicker,
        ["engine/WorkshopVisualBinding.cs"] = WorkshopArea.Presentation,
        ["engine/CosmeticBindingSet.cs"] = WorkshopArea.Presentation,
        ["ui/WorkshopAnimation.cs"] = WorkshopArea.Presentation,
        ["ui/WorkshopArtwork.cs"] = WorkshopArea.Presentation,
        ["engine/presentation/AnimationBatch.cs"] = WorkshopArea.Presentation,
        ["engine/presentation/AnimationFollowDefinition.cs"] = WorkshopArea.Presentation,
        ["engine/presentation/AnimationImpulseDefinition.cs"] = WorkshopArea.Presentation,
        ["engine/presentation/AnimationOscillationDefinition.cs"] = WorkshopArea.Presentation,
        ["engine/presentation/AnimationTimerSegment.cs"] = WorkshopArea.Presentation,
        ["engine/presentation/AnimationValues.cs"] = WorkshopArea.Presentation,
    };

    /// <summary>The area of an engine/, ui/ or scenes/ path (its .uid sidecar follows the source), or null when the table lacks it.</summary>
    public static WorkshopArea? AreaOf(string path)
    {
        var source = path.EndsWith(".cs.uid", StringComparison.Ordinal) ? path[..^".uid".Length] : path;
        return AreaFiles.TryGetValue(source, out var area) ? area : null;
    }

    private static readonly string[] NoBrowserToolDirectories =
        ["Coverage", "Coverage.Tests", "Ownership", "anvil", "LifecycleContract", "WireContract", "TraceAllocations"];

    private static bool Under(string path, string directory) => path.StartsWith(directory + "/", StringComparison.Ordinal);
    private static bool TopLevelIn(string path, string directory, string extension) =>
        Under(path, directory) && path.IndexOf('/', directory.Length + 1) < 0 && path.EndsWith(extension, StringComparison.Ordinal);
    private static string FileNameOf(string path) => path[(path.LastIndexOf('/') + 1)..];
    private static string? ElementDirectory(string path) =>
        Under(path, "elements") && path.IndexOf('/', "elements/".Length) is var slash and > 0 ? path["elements/".Length..slash] : null;

    /// <summary>The rule table. Full rules take precedence over every other rule (a build file inside a package is a build change);
    /// any other overlap is an error that the rule-table test forbids.</summary>
    public static readonly IReadOnlyList<PathRule> Rules =
    [
        new("docs/**", ImpactKind.NoBrowserImpact, path => Under(path, "docs")),
        new("_bmad-output/**", ImpactKind.NoBrowserImpact, path => Under(path, "_bmad-output")),
        new("_bmad/**", ImpactKind.NoBrowserImpact, path => Under(path, "_bmad")),
        new(".claude/**", ImpactKind.NoBrowserImpact, path => Under(path, ".claude")),
        new(".agents/**", ImpactKind.NoBrowserImpact, path => Under(path, ".agents")),
        new("root *.md", ImpactKind.NoBrowserImpact, path => !path.Contains('/', StringComparison.Ordinal) && path.EndsWith(".md", StringComparison.Ordinal)),
        new("tools/e2e/README.md", ImpactKind.NoBrowserImpact, path => path == "tools/e2e/README.md"),
        new("CuriousContraptions.tests/**", ImpactKind.NoBrowserImpact, path => Under(path, "CuriousContraptions.tests")),
        new("tools/{Coverage,Coverage.Tests,Ownership,anvil,LifecycleContract,WireContract,TraceAllocations}/**", ImpactKind.NoBrowserImpact,
            path => NoBrowserToolDirectories.Any(directory => Under(path, "tools/" + directory))),
        new("tools/e2e/<suite>.test.ts", ImpactKind.Suite, path => SuiteCatalogue.TryParsePath(path) is not null),
        new("tools/e2e/*.ts (shared module)", ImpactKind.SharedTestModule,
            path => TopLevelIn(path, "tools/e2e", ".ts") && !SuiteCatalogue.IsSuitePath(path)),
        new("tools/*.mjs (shared module)", ImpactKind.SharedTestModule, path => TopLevelIn(path, "tools", ".mjs")),
        new("elements/<package>/**", ImpactKind.Element, path => ElementDirectory(path) is { } directory && ElementPackages.ContainsKey(directory) && !path.EndsWith(".csproj", StringComparison.Ordinal)),
        new("known package and solution build", ImpactKind.PackageBuild, path => path == BuildImpact.Solution || (PackagePart(path) is not null && path.EndsWith(".csproj", StringComparison.Ordinal))),
        new("suite catalogue rows", ImpactKind.Catalogue, path => path == "tools/E2eRunner/SuiteCatalogue.cs"),
        new("content/puzzles.json", ImpactKind.Level, path => path == "content/puzzles.json"),
        new("engine/** (outside engine/gpu), ui/**, scenes/** (area table)", ImpactKind.WorkshopArea,
            path => (Under(path, "engine") && !Under(path, "engine/gpu")) || Under(path, "ui") || Under(path, "scenes")),
        new("engine/gpu/**", ImpactKind.Full, path => Under(path, "engine/gpu"), FullReason.MaterialEngine),
        new("workers, client, contracts and schemas", ImpactKind.Full,
            path => Under(path, "CuriousContraptions.Simulation") || path.StartsWith("CuriousContraptions.Animation", StringComparison.Ordinal) ||
                Under(path, "CuriousContraptions.web") || path.StartsWith("CuriousContraptions.Abstractions", StringComparison.Ordinal) ||
                Under(path, "CuriousContraptions.Bundle") ||
                Under(path, "CuriousContraptions.FlatBuffers") || Under(path, "schemas"),
            FullReason.SharedRuntime),
        new("tools/{BundleElements,Preview}/**", ImpactKind.Full,
            path => Under(path, "tools/BundleElements") || Under(path, "tools/Preview"), FullReason.SharedDriverInfrastructure),
        new("tools/E2eRunner*/**", ImpactKind.Full,
            path => (Under(path, "tools/E2eRunner") && path != "tools/E2eRunner/SuiteCatalogue.cs") || Under(path, "tools/E2eRunner.Tests"), FullReason.RunnerItself),
        new("build files", ImpactKind.Full,
            path => (path.EndsWith(".csproj", StringComparison.Ordinal) && PackagePart(path) is null) || (path.EndsWith(".slnx", StringComparison.Ordinal) && path != BuildImpact.Solution) ||
                FileNameOf(path).StartsWith("Directory.Build.", StringComparison.Ordinal) ||
                path is "global.json" or "project.godot" or "export_presets.cfg",
            FullReason.Build),
    ];

    /// <summary>The unmatched-path rule.</summary>
    public static readonly PathRule UnknownRule = new("unmatched path", ImpactKind.Full, _ => true, FullReason.Unknown);

    /// <summary>The one rule for a path: a Full rule wins; otherwise exactly one rule matches, or none (Unknown).</summary>
    public static PathRule Resolve(string path)
    {
        var matching = Rules.Where(rule => rule.Matches(path)).ToArray();
        if (matching.FirstOrDefault(rule => rule.Kind == ImpactKind.Full) is { } full) return full;
        return matching.Length switch
        {
            0 => UnknownRule,
            1 => matching[0],
            _ => throw new InvalidOperationException(
                $"Overlapping impact rules for {path}: {string.Join(", ", matching.Select(rule => rule.Name))}."),
        };
    }

    /// <summary>The element package directory of an elements/ path (null outside elements/).</summary>
    public static PartId? PackagePart(string path) =>
        ElementDirectory(path) is { } directory && ElementPackages.TryGetValue(directory, out var part) ? part : null;
}
