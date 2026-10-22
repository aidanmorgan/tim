using System.Text.Json;
using System.Text.RegularExpressions;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.E2eRunner;

/// <summary>Selected: the Tier 2 suites. Full: every suite (a Tier 3 trigger).</summary>
internal enum SelectionMode { Selected = 1, Full }

/// <summary>One changed path's classification and, for a Full path, why.</summary>
internal sealed record PathImpact(string Path, ImpactKind Kind, string Rule, FullReason? Full, string Detail);

/// <summary>One selected suite with its one-line reasons.</summary>
internal sealed record SelectedSuite(SuiteId Suite, IReadOnlyList<string> Reasons)
{
    public string ReasonLine => $"{SuiteCatalogue.FileName(Suite)}\t{string.Join("; ", Reasons)}";
}

/// <summary>The Tier 2 selection: the mode, the suites with reasons, and every changed path's classification.</summary>
internal sealed record Selection(SelectionMode Mode, IReadOnlyList<SelectedSuite> Suites, IReadOnlyList<PathImpact> Paths)
{
    public bool Tier3Required => Mode == SelectionMode.Full;

    public IEnumerable<string> Lines()
    {
        yield return $"Mode: {Mode}";
        yield return $"Tier 3 required: {(Tier3Required ? "yes" : "no")}";
        foreach (var path in Paths)
            yield return $"path\t{path.Path}\t{path.Kind}{(path.Full is { } full ? "(" + full + ")" : "")}\t{path.Rule}{(path.Detail.Length > 0 ? ": " + path.Detail : "")}";
        foreach (var suite in Suites) yield return suite.ReasonLine;
    }
}

/// <summary>The head and base views a selection reads: the head is the working tree (with optional overrides for tests), the base
/// is the diff base (HEAD for a --changed list).</summary>
internal sealed partial class ImpactContext
{
    private readonly string _root;
    private readonly Func<string, string?> _readBase;
    private readonly Func<string, string?>? _headOverride;

    public ImpactContext(string root, Func<string, string?> readBase, Func<string, string?>? headOverride = null)
    {
        _root = root;
        _readBase = readBase;
        _headOverride = headOverride;
        SuiteClosures = Enum.GetValues<SuiteId>().ToDictionary(id => id,
            id => (IReadOnlySet<string>)new HashSet<string>(TimingDetector.ImportClosure(root, SuiteCatalogue.RelativePath(id)), StringComparer.Ordinal));
    }

    public IReadOnlyDictionary<SuiteId, IReadOnlySet<string>> SuiteClosures { get; }

    public string? ReadBase(string path) => _readBase(path);

    public string? ReadHead(string path)
    {
        if (_headOverride is not null) return _headOverride(path);
        var full = Path.Combine(_root, path);
        return File.Exists(full) ? File.ReadAllText(full) : null;
    }

    /// <summary>The generated element bundle (Story 9A.4): a build output, read from the working tree and never from git. Story 9A.14
    /// made it the game's only catalogue, so an element change is always a change under its elements/&lt;Package&gt; directory.</summary>
    public const string BundlePath = "elements/elements.bundle.json";
    public const string LevelsPath = "content/puzzles.json";

    internal sealed record BundlePort(PortSocket Socket, SimulationDomain Domain, PortDirection Direction);
    internal sealed record BundleElement(PartId Part, string? ScenePath, IReadOnlyList<BundlePort> Ports);

    /// <summary>The bundle's elements, or null when the JSON does not parse as a bundle. Enum fields are written by name; an
    /// unknown name does not parse.</summary>
    public static IReadOnlyList<BundleElement>? BundleElements(string json)
    {
        static TEnum Named<TEnum>(JsonElement value) where TEnum : struct, Enum =>
            value.GetString() is { } name && Enum.GetNames<TEnum>().Contains(name) ? Enum.Parse<TEnum>(name)
                : throw new FormatException($"Undefined {typeof(TEnum).Name} '{value}'.");
        try
        {
            using var document = JsonDocument.Parse(json);
            var elements = new List<BundleElement>();
            foreach (var element in document.RootElement.GetProperty("Elements").EnumerateArray())
            {
                var metadata = element.GetProperty("Metadata");
                var ports = element.GetProperty("Ports").EnumerateArray().Select(port => new BundlePort(Named<PortSocket>(port.GetProperty("Socket")),
                    Named<SimulationDomain>(port.GetProperty("Domain")), Named<PortDirection>(port.GetProperty("Direction")))).ToArray();
                elements.Add(new BundleElement(new PartId(metadata.GetProperty("Id").GetString()!), metadata.GetProperty("ScenePath").GetString(), ports));
            }
            return elements;
        }
        catch (JsonException) { return null; }
        catch (KeyNotFoundException) { return null; }
        catch (InvalidOperationException) { return null; }
        catch (ArgumentException) { return null; }
        catch (FormatException) { return null; }
    }

    /// <summary>The levels of content/puzzles.json by id (their raw JSON), or null when it does not parse.</summary>
    private Dictionary<LevelId, IReadOnlySet<PartId>>? _levelParts;

    /// <summary>The parts a level pre-places (each fixture's element id in the head content/puzzles.json, read by its availability's
    /// current shape; none when the level or the file is absent).</summary>
    public IReadOnlySet<PartId> LevelParts(LevelId level)
    {
        _levelParts ??= ReadLevelParts(ReadHead(LevelsPath));
        return _levelParts.TryGetValue(level, out var parts) ? parts : new HashSet<PartId>();
    }

    private static Dictionary<LevelId, IReadOnlySet<PartId>> ReadLevelParts(string? json)
    {
        if (json is null) return [];
        return CuriousContraptions.Content.LessonFixtures.Read(json).GroupBy(fixture => fixture.Lesson, StringComparer.Ordinal).ToDictionary(
            lesson => new LevelId(lesson.Key), lesson => (IReadOnlySet<PartId>)lesson.Select(fixture => new PartId(fixture.Part)).ToHashSet());
    }

    public static IReadOnlyDictionary<LevelId, string>? Levels(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.EnumerateArray().ToDictionary(level => new LevelId(level.GetProperty("id").GetString()!), level => level.GetRawText());
        }
        catch (JsonException) { return null; }
        catch (KeyNotFoundException) { return null; }
        catch (InvalidOperationException) { return null; }
        catch (ArgumentException) { return null; }
    }
}

internal static class Selector
{
    /// <summary>The Tier 2 selection for a changed-path set (spec ImpactRules and Selection tasks).</summary>
    public static Selection Select(IReadOnlyList<string> changedPaths, ImpactContext context)
    {
        ArgumentNullException.ThrowIfNull(changedPaths);
        ArgumentNullException.ThrowIfNull(context);
        var impacts = new List<PathImpact>();
        var reasons = new Dictionary<SuiteId, List<string>>();
        void Add(SuiteId suite, string reason)
        {
            if (!reasons.TryGetValue(suite, out var list)) reasons[suite] = list = [];
            if (!list.Contains(reason, StringComparer.Ordinal)) list.Add(reason);
        }

        var headBundle = context.ReadHead(ImpactContext.BundlePath) is { } bundleText ? ImpactContext.BundleElements(bundleText) : null;
        foreach (var path in changedPaths.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
            impacts.Add(Classify(path, context, headBundle, Add));

        if (impacts.FirstOrDefault(impact => impact.Kind == ImpactKind.Full) is { } trigger)
        {
            var full = SuiteCatalogue.All.Select(declaration => new SelectedSuite(declaration.Id,
                [$"full: {trigger.Path} ({trigger.Full}, {trigger.Rule})"])).ToArray();
            return new Selection(SelectionMode.Full, full, impacts);
        }
        foreach (var smoke in SuiteCatalogue.SmokePair) Add(smoke, "smoke pair");
        var selected = SuiteCatalogue.All.Where(declaration => reasons.ContainsKey(declaration.Id))
            .Select(declaration => new SelectedSuite(declaration.Id, reasons[declaration.Id])).ToArray();
        return new Selection(SelectionMode.Selected, selected, impacts);
    }

    private static PathImpact Classify(string path, ImpactContext context, IReadOnlyList<ImpactContext.BundleElement>? headBundle,
        Action<SuiteId, string> add)
    {
        var rule = ImpactRules.Resolve(path);
        PathImpact Full(FullReason reason, string detail) => new(path, ImpactKind.Full, rule.Name, reason, detail);
        switch (rule.Kind)
        {
            case ImpactKind.Catalogue:
                var changedSuites = CatalogueImpact.Changed(context.ReadBase(path), context.ReadHead(path));
                if (changedSuites is null) return Full(FullReason.RunnerItself, "catalogue change is not confined to understood declaration rows");
                foreach (var changedSuite in changedSuites) add(changedSuite, $"catalogue declaration: {path}");
                return new(path, ImpactKind.Suite, rule.Name, null, $"{changedSuites.Count} changed declarations");
            case ImpactKind.PackageBuild:
                var buildParts = BuildImpact.Parts(path, context);
                if (buildParts is null) return Full(FullReason.Build, "unknown/shared build change");
                if (headBundle is null) return Full(FullReason.Unknown, "generated bundle missing");
                foreach (var part in buildParts) SelectElement(part, path, headBundle, context.LevelParts, add);
                return new(path, ImpactKind.Element, rule.Name, null, string.Join(",", buildParts));
            case ImpactKind.NoBrowserImpact:
                return new(path, rule.Kind, rule.Name, null, "");
            case ImpactKind.Full:
                return new(path, rule.Kind, rule.Name, rule.Full, "");
            case ImpactKind.Suite:
                if (context.ReadHead(path) is null) return Full(FullReason.Unknown, "a removed suite (the catalogue must change too)");
                var suite = SuiteCatalogue.TryParsePath(path)!.Value;
                add(suite, $"suite: {path} changed");
                return new(path, rule.Kind, rule.Name, null, SuiteCatalogue.FileName(suite));
            case ImpactKind.SharedTestModule:
                if (context.ReadHead(path) is null) return Full(FullReason.Unknown, "a removed shared test module");
                var importers = context.SuiteClosures.Where(entry => entry.Value.Contains(path)).Select(entry => entry.Key).ToArray();
                if (importers.Length == SuiteCatalogue.All.Count) return Full(FullReason.SharedDriverInfrastructure, "imported by every suite");
                foreach (var importer in importers) add(importer, $"shared module: imports {path}");
                return new(path, rule.Kind, rule.Name, null, $"{importers.Length} importing suite(s)");
            case ImpactKind.Element:
                var parts = ElementParts(path);
                if (parts.Count == 0) return Full(FullReason.Unknown, "no part resolves from this element path");
                // Connected suites come from the generated bundle's ports; without it the selection would silently miss them.
                if (headBundle is null) return Full(FullReason.Unknown, $"{ImpactContext.BundlePath} is missing or does not parse (build the solution first)");
                foreach (var part in parts) SelectElement(part, path, headBundle, context.LevelParts, add);
                return new(path, rule.Kind, rule.Name, null, string.Join(",", parts));
            case ImpactKind.Level:
                var baseLevels = context.ReadBase(path) is { } baseLevelText ? ImpactContext.Levels(baseLevelText) : new Dictionary<LevelId, string>();
                var headLevels = context.ReadHead(path) is { } headLevelText ? ImpactContext.Levels(headLevelText) : new Dictionary<LevelId, string>();
                if (baseLevels is null || headLevels is null) return Full(FullReason.Unknown, "content/puzzles.json does not parse");
                var levels = baseLevels.Keys.Union(headLevels.Keys).Where(level =>
                    !baseLevels.TryGetValue(level, out var was) || !headLevels.TryGetValue(level, out var now) || was != now)
                    .OrderBy(level => level.Value, StringComparer.Ordinal).ToArray();
                foreach (var level in levels)
                    foreach (var declaration in SuiteCatalogue.All.Where(declaration => declaration.Levels.Contains(level)))
                        add(declaration.Id, $"level: declares {level}");
                return new(path, rule.Kind, rule.Name, null, levels.Length == 0 ? "no level changed" : string.Join(",", levels));
            case ImpactKind.WorkshopArea:
                if (ImpactRules.AreaOf(path) is not { } area) return Full(FullReason.Unknown, "not in the area table");
                if (area == WorkshopArea.AllInteraction) return Full(FullReason.AllInteraction, "every interaction passes through it");
                foreach (var declaration in SuiteCatalogue.All.Where(declaration => declaration.Areas.Contains(area)))
                    add(declaration.Id, $"area: {area} ({path})");
                return new(path, rule.Kind, rule.Name, null, area.ToString());
            default:
                throw new InvalidOperationException($"Undefined impact kind {rule.Kind}.");
        }
    }

    private static SortedSet<PartId> ElementParts(string path)
    {
        var parts = new SortedSet<PartId>();
        if (ImpactRules.PackagePart(path) is { } package) parts.Add(package);
        return parts;
    }

    private static bool Declares(SuiteDeclaration declaration, PartId part, Func<LevelId, IReadOnlySet<PartId>> levelParts) =>
        declaration.Primary == part || declaration.Places.Contains(part) ||
        declaration.Levels.Any(level => levelParts(level).Contains(part));

    // own, integration and connected suites of one changed part (spec D6).
    private static void SelectElement(PartId part, string path, IReadOnlyList<ImpactContext.BundleElement> bundle,
        Func<LevelId, IReadOnlySet<PartId>> levelParts, Action<SuiteId, string> add)
    {
        // A changed element must have its own suite: without one, Tier 2 cannot prove the change (coordinator, 19 Oct 2026).
        if (!SuiteCatalogue.All.Any(declaration => Declares(declaration, part, levelParts)))
            throw new RunnerException(RunnerError.NoPrimarySuite,
                $"{path} changes {part}, which has no declared suite coverage; add its coverage to SuiteCatalogue.");
        foreach (var declaration in SuiteCatalogue.All)
        {
            if (declaration.Primary == part) add(declaration.Id, $"own: {path} changes {part}");
            if (declaration.Places.Contains(part)) add(declaration.Id, $"integration: places {part}");
            else if (declaration.Scope == PlacementScope.EveryPalettePart) add(declaration.Id, $"integration: places every palette part ({part})");
            // Spec D6: a suite's effective parts also include the parts its declared levels pre-place (domino_effect's locked lamp).
            foreach (var level in declaration.Levels.Where(level => levelParts(level).Contains(part)).OrderBy(level => level.Value, StringComparer.Ordinal))
                add(declaration.Id, $"integration: level {level} places {part}");
        }
        if (bundle.FirstOrDefault(element => element.Part == part) is not { } changed) return;
        foreach (var partner in bundle.Where(element => element.Part != part))
        {
            var pair = changed.Ports.SelectMany(port => partner.Ports.Where(other => other.Domain == port.Domain && other.Direction != port.Direction)
                .Select(other => (port, other))).FirstOrDefault();
            if (pair == default) continue;
            foreach (var declaration in SuiteCatalogue.All.Where(declaration => Declares(declaration, partner.Part, levelParts)))
                add(declaration.Id, $"connected: {part}.{pair.port.Socket} {pair.port.Domain} {partner.Part}.{pair.other.Socket}");
        }
    }
}
