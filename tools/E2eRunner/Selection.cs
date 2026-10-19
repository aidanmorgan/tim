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
        if (_headOverride?.Invoke(path) is { } overridden) return overridden;
        var full = Path.Combine(_root, path);
        return File.Exists(full) ? File.ReadAllText(full) : null;
    }

    [GeneratedRegex(@"^Id\s*=\s*""([^""]+)""", RegexOptions.Multiline)]
    private static partial Regex CatalogueId();
    [GeneratedRegex(@"path=""res://(parts/scenes/[^""]+\.tscn)""")]
    private static partial Regex CatalogueScene();

    /// <summary>The part a catalogue resource declares (its Id line), at head or, when deleted, at base.</summary>
    public PartId? CataloguePart(string path) =>
        (ReadHead(path) ?? ReadBase(path)) is { } text && CatalogueId().Match(text) is { Success: true } match ? new PartId(match.Groups[1].Value) : null;

    /// <summary>Every catalogue resource at head with its part and scene.</summary>
    public IEnumerable<(PartId Part, string? Scene)> Catalogue()
    {
        var directory = Path.Combine(_root, "parts", "catalog");
        if (!Directory.Exists(directory)) yield break;
        foreach (var file in Directory.EnumerateFiles(directory, "*.tres").Order(StringComparer.Ordinal))
        {
            var text = File.ReadAllText(file);
            if (CatalogueId().Match(text) is not { Success: true } id) continue;
            var scene = CatalogueScene().Match(text);
            yield return (new PartId(id.Groups[1].Value), scene.Success ? scene.Groups[1].Value : null);
        }
    }

    /// <summary>Every part whose catalogue resource or bundle element references a scene.</summary>
    public IReadOnlySet<PartId> ScenePart(string scenePath)
    {
        var parts = new SortedSet<PartId>(Catalogue().Where(entry => entry.Scene == scenePath).Select(entry => entry.Part));
        if (ReadHead(BundlePath) is { } bundle)
            foreach (var element in BundleElements(bundle) ?? [])
                if (element.ScenePath == "res://" + scenePath) parts.Add(element.Part);
        return parts;
    }

    public const string BundlePath = "parts/elements.bundle.json";
    public const string LevelsPath = "content/puzzles.json";

    internal sealed record BundlePort(string PortId, SimulationDomain Domain, int Direction);
    internal sealed record BundleElement(PartId Part, string? ScenePath, IReadOnlyList<BundlePort> Ports, string Json);

    /// <summary>The bundle's elements, or null when the JSON does not parse as a bundle.</summary>
    public static IReadOnlyList<BundleElement>? BundleElements(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var elements = new List<BundleElement>();
            foreach (var element in document.RootElement.GetProperty("Elements").EnumerateArray())
            {
                var metadata = element.GetProperty("Metadata");
                var ports = element.GetProperty("Ports").EnumerateArray().Select(port => new BundlePort(port.GetProperty("PortId").GetString()!,
                    (SimulationDomain)port.GetProperty("Domain").GetUInt32(), port.GetProperty("Direction").GetInt32())).ToArray();
                elements.Add(new BundleElement(new PartId(metadata.GetProperty("Id").GetString()!),
                    metadata.TryGetProperty("ScenePath", out var scene) ? scene.GetString() : null, ports, element.GetRawText()));
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

    /// <summary>The parts a level pre-places (the `kind` of each of its `parts` in the head content/puzzles.json; none when the level
    /// or the file is absent).</summary>
    public IReadOnlySet<PartId> LevelParts(LevelId level)
    {
        _levelParts ??= ReadLevelParts(ReadHead(LevelsPath));
        return _levelParts.TryGetValue(level, out var parts) ? parts : new HashSet<PartId>();
    }

    private static Dictionary<LevelId, IReadOnlySet<PartId>> ReadLevelParts(string? json)
    {
        if (json is null) return [];
        using var document = JsonDocument.Parse(json);
        return document.RootElement.EnumerateArray().ToDictionary(level => new LevelId(level.GetProperty("id").GetString()!),
            level => (IReadOnlySet<PartId>)(level.TryGetProperty("parts", out var parts)
                ? parts.EnumerateArray().Select(entry => new PartId(entry.GetProperty("kind").GetString()!)).ToHashSet()
                : []));
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

        var headBundle = context.ReadHead(ImpactContext.BundlePath) is { } bundleText ? ImpactContext.BundleElements(bundleText) ?? [] : [];
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

    private static PathImpact Classify(string path, ImpactContext context, IReadOnlyList<ImpactContext.BundleElement> headBundle,
        Action<SuiteId, string> add)
    {
        var rule = ImpactRules.Resolve(path);
        PathImpact Full(FullReason reason, string detail) => new(path, ImpactKind.Full, rule.Name, reason, detail);
        switch (rule.Kind)
        {
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
                var parts = ElementParts(path, context);
                if (parts.Count == 0) return Full(FullReason.Unknown, "no part resolves from this element path");
                foreach (var part in parts) SelectElement(part, path, headBundle, context.LevelParts, add);
                return new(path, rule.Kind, rule.Name, null, string.Join(",", parts));
            case ImpactKind.ElementBundle:
                var before = context.ReadBase(path) is { } baseText ? ImpactContext.BundleElements(baseText) : [];
                var after = context.ReadHead(path) is { } headText ? ImpactContext.BundleElements(headText) : [];
                if (before is null || after is null) return Full(FullReason.Unknown, "the element bundle does not parse");
                var difference = before.Select(element => (element.Part, element.Json)).ToHashSet();
                difference.SymmetricExceptWith(after.Select(element => (element.Part, element.Json)));
                var changed = difference.Select(entry => entry.Part).Distinct().Order().ToArray();
                if (changed.Length == 0) return new(path, ImpactKind.NoBrowserImpact, rule.Name, null, "no element changed (GeneratedAt only)");
                foreach (var part in changed) SelectElement(part, path, headBundle, context.LevelParts, add);
                return new(path, rule.Kind, rule.Name, null, string.Join(",", changed));
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

    private static SortedSet<PartId> ElementParts(string path, ImpactContext context)
    {
        var parts = new SortedSet<PartId>();
        if (ImpactRules.PackagePart(path) is { } package) parts.Add(package);
        else if (path.StartsWith("parts/catalog/", StringComparison.Ordinal)) { if (context.CataloguePart(path) is { } part) parts.Add(part); }
        else if (path.StartsWith("parts/icons/", StringComparison.Ordinal))
        {
            var stem = Path.GetFileNameWithoutExtension(path);
            parts.UnionWith(context.Catalogue().Where(entry => entry.Part.Value == stem).Select(entry => entry.Part));
        }
        else if (path.StartsWith("parts/scenes/", StringComparison.Ordinal)) parts.UnionWith(context.ScenePart(path));
        return parts;
    }

    // own, integration and connected suites of one changed part (spec D6).
    private static void SelectElement(PartId part, string path, IReadOnlyList<ImpactContext.BundleElement> bundle,
        Func<LevelId, IReadOnlySet<PartId>> levelParts, Action<SuiteId, string> add)
    {
        // A changed element must have its own suite: without one, Tier 2 cannot prove the change (coordinator, 19 Oct 2026).
        if (!SuiteCatalogue.All.Any(declaration => declaration.Primary == part))
            throw new RunnerException(RunnerError.NoPrimarySuite,
                $"{path} changes {part}, which no suite declares as its Primary; add the element's suite (or its Primary) to SuiteCatalogue.");
        foreach (var declaration in SuiteCatalogue.All)
        {
            if (declaration.Primary == part) add(declaration.Id, $"own: {path} changes {part}");
            if (declaration.Places.Contains(part)) add(declaration.Id, $"integration: places {part}");
            else if (declaration.Scope == PlacementScope.EveryPalettePart) add(declaration.Id, $"integration: places every palette part ({part})");
            // Spec D6: a suite's effective parts also include the parts its declared levels pre-place (domino_effect's locked bowling ball).
            foreach (var level in declaration.Levels.Where(level => levelParts(level).Contains(part)).OrderBy(level => level.Value, StringComparer.Ordinal))
                add(declaration.Id, $"integration: level {level} places {part}");
        }
        if (bundle.FirstOrDefault(element => element.Part == part) is not { } changed) return;
        foreach (var partner in bundle.Where(element => element.Part != part))
        {
            var pair = changed.Ports.SelectMany(port => partner.Ports.Where(other => other.Domain == port.Domain && other.Direction != port.Direction)
                .Select(other => (port, other))).FirstOrDefault();
            if (pair == default) continue;
            if (SuiteCatalogue.All.FirstOrDefault(declaration => declaration.Primary == partner.Part) is not { } primary) continue;
            add(primary.Id, $"connected: {part}.{pair.port.PortId} {pair.port.Domain} {partner.Part}.{pair.other.PortId}");
        }
    }
}
