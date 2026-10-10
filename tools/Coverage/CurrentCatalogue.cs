using System.Text.Json;
using System.Text.RegularExpressions;

namespace CuriousContraptions.Coverage;

public readonly record struct PlannedMode(ModeDimension Dimension, ModeChoice Choice);
public sealed record PlannedCatalogue(WorkOrderId CatalogueId, WorkOrderId DeliveryOwner,
    SourceRequirement Source, SourceArtifact Declaration, PlannedMode[] Modes);
public sealed record PlannedFixture(WorkOrderId Id, RequirementId Instance, SourceKey Catalogue,
    bool Locked, WorkOrderId ProofCriterion);
public sealed record CurrentCatalogue(PlannedCatalogue[] Elements, PlannedFixture[] Fixtures)
{
    public IReadOnlySet<RequirementId> Admitted { get; init; } = new HashSet<RequirementId>();
    public SourceArtifact[] AvailableResources { get; init; } = [];
    public static readonly SourcePath ConsumersPath = new("docs/planning/invest/current-consumers.md");
    private const string DeclarationDirectory = "docs/planning/elements";
    private const string ResourceDirectory = "parts/catalog";
    private const string RegistryPath = "engine/PartRegistry.cs";

    public static CurrentCatalogue Read(DirectoryInfo root)
    {
        var requirements = File.ReadAllText(Path.Combine(root.FullName, RequirementDiscovery.RequirementsPath.Value));
        var ids = Regex.Matches(requirements, "<a id=\"current-cat-([0-9]{3})\"></a>")
            .Select(match => new WorkOrderId("CAT-" + match.Groups[1].Value)).ToArray();
        if (ids.Length == 0 || ids.Distinct().Count() != ids.Length)
            throw new InvalidDataException("Missing or duplicate current catalogue requirement identities.");
        var result = Parse(File.ReadAllText(Path.Combine(root.FullName, ConsumersPath.Value)), ids, id =>
        {
            var files = Directory.GetFiles(Path.Combine(root.FullName, DeclarationDirectory), id.Value + "-*.md");
            if (files.Length != 1) throw new InvalidDataException("Catalogue declaration is missing or ambiguous.");
            return (new SourcePath(Path.GetRelativePath(root.FullName, files[0])), File.ReadAllText(files[0]));
        });
        result = result.BindAcceptance(requirements);
        result.ValidateFixtures(File.ReadAllText(Path.Combine(root.FullName, "content/puzzles.json")));
        var registry = File.ReadAllText(Path.Combine(root.FullName, RegistryPath));
        var admitted = Regex.Matches(registry, "res://parts/catalog/([a-z0-9_]+)\\.tres")
            .Select(match => new RequirementId(match.Groups[1].Value)).ToArray();
        if (admitted.Length == 0 || admitted.Distinct().Count() != admitted.Length)
            throw new InvalidDataException("Missing or duplicate admitted catalogue resource boundary.");
        var present = Directory.GetFiles(Path.Combine(root.FullName, ResourceDirectory), "*.tres")
            .Select(path => new RequirementId(Path.GetFileNameWithoutExtension(path)));
        var artifacts = result.ValidateResources(admitted.Concat(present).Distinct().ToArray(), path =>
        {
            var full = Path.Combine(root.FullName, path.Value);
            return File.Exists(full) ? File.ReadAllText(full) : null;
        });
        return result with { AvailableResources = artifacts, Admitted = admitted.ToHashSet() };
    }

    public static CurrentCatalogue Parse(string text, IReadOnlyCollection<WorkOrderId> required,
        Func<WorkOrderId, (SourcePath Path, string Text)> declaration)
    {
        var headings = Regex.Matches(text, @"^### (CAT-[0-9]{3})-I · ([a-z0-9_]+)\r?$", RegexOptions.Multiline);
        var fixtureStart = text.IndexOf("## Exact fixture proof children", StringComparison.Ordinal);
        if (headings.Count == 0 || fixtureStart < 0)
            throw new InvalidDataException("Current catalogue or fixture boundary is missing.");
        var elements = new List<PlannedCatalogue>();
        var seenIds = new HashSet<WorkOrderId>();
        var seenKeys = new HashSet<RequirementId>();
        for (var i = 0; i < headings.Count; i++)
        {
            var heading = headings[i];
            var id = new WorkOrderId(heading.Groups[1].Value);
            var key = new RequirementId(heading.Groups[2].Value);
            if (!seenIds.Add(id) || !seenKeys.Add(key))
                throw new InvalidDataException("Duplicate catalogue identity or key.");
            var end = i + 1 < headings.Count ? headings[i + 1].Index : fixtureStart;
            if (end <= heading.Index) throw new InvalidDataException("Catalogue section is outside its boundary.");
            var block = text[heading.Index..end].Trim();
            var acceptance = Regex.Matches(block, @"\[Exact D/I/V acceptance\]\(\.\./requirements\.md#current-cat-([0-9]{3})\)");
            if (acceptance.Count != 1 || "CAT-" + acceptance[0].Groups[1].Value != id.Value)
                throw new InvalidDataException("Catalogue acceptance identity differs.");
            var markers = Regex.Matches(block, Regex.Escape("**Mode records:**"));
            var sentence = Regex.Matches(block, @"\*\*Mode records:\*\* ([^.\r\n]+)\.");
            if (markers.Count != 1 || sentence.Count != 1)
                throw new InvalidDataException("Mode sentence is missing, duplicated or malformed.");
            var modes = sentence[0].Groups[1].Value.Split(", ", StringSplitOptions.None).Select(ParseMode).ToArray();
            if (modes.Distinct().Count() != modes.Length || !modes.Contains(new(ModeDimension.Configuration, ModeChoice.Fixed)))
                throw new InvalidDataException("Missing fixed placeholder or duplicate planned mode.");
            var source = declaration(id);
            if (!source.Text.StartsWith("# " + id.Value + " ·", StringComparison.Ordinal))
                throw new InvalidDataException("Declaration identity differs from catalogue.");
            elements.Add(new(id, new WorkOrderId(id.Value + "-I"),
                new(new(RequirementOrigin.Catalogue, key), ConsumersPath.Value + "#" + id.Value.ToLowerInvariant() + "-i",
                    key.Value, RequirementDiscovery.Hash(block + "\n" + source.Path.Value + "\n" + source.Text)),
                new(source.Path, RequirementDiscovery.Hash(source.Text)), modes));
        }
        if (required.Count == 0 || required.Distinct().Count() != required.Count || !seenIds.SetEquals(required))
            throw new InvalidDataException("Catalogue membership differs from current requirements.");
        var byKey = elements.ToDictionary(element => element.Source.Key.Id);
        var fixtures = new List<PlannedFixture>();
        var fixtureIds = new HashSet<WorkOrderId>();
        var instances = new HashSet<RequirementId>();
        foreach (var line in text[fixtureStart..].Split('\n').Where(line => line.StartsWith("| <a", StringComparison.Ordinal)))
        {
            var row = Regex.Match(line.TrimEnd('\r'), "^\\| <a id=\"(fix-[0-9]{3}-[0-9]{3})\"></a>(FIX-[0-9]{3}-[0-9]{3}) \\| ([a-z0-9_]+/[a-z0-9_]+) \\| ([a-z0-9_]+) / (True|False) \\| (CAT-[0-9]{3}-V) \\| (FIX-[0-9]{3}-[0-9]{3}) \\|$");
            if (!row.Success || row.Groups[1].Value != row.Groups[2].Value.ToLowerInvariant() || row.Groups[2].Value != row.Groups[7].Value)
                throw new InvalidDataException("Malformed fixture identity row.");
            var id = new WorkOrderId(row.Groups[2].Value);
            var instance = new RequirementId(row.Groups[3].Value);
            var key = new RequirementId(row.Groups[4].Value);
            if (!fixtureIds.Add(id) || !instances.Add(instance) || !byKey.TryGetValue(key, out var element))
                throw new InvalidDataException("Duplicate fixture or unknown catalogue key.");
            var criterion = new WorkOrderId(row.Groups[6].Value);
            if (criterion.Value != element.CatalogueId.Value + "-V")
                throw new InvalidDataException("Fixture proof criterion differs from its declared catalogue.");
            fixtures.Add(new(id, instance, element.Source.Key, row.Groups[5].Value == "True", criterion));
        }
        if (fixtures.Count == 0) throw new InvalidDataException("Fixture identity table is empty.");
        return new(elements.ToArray(), fixtures.ToArray());
    }

    public CurrentCatalogue BindAcceptance(string requirements)
    {
        var anchors = Regex.Matches(requirements, "<a id=\"([^\"]+)\"></a>");
        var elements = Elements.Select(element =>
        {
            var id = "current-cat-" + element.CatalogueId.Value[4..];
            var body = Block(id);
            var targets = Regex.Matches(body, @"\]\(#([^)]+)\)").Select(match => match.Groups[1].Value).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);
            foreach (var target in targets) body += "\n" + Block(target);
            return element with { Source = element.Source with { Hash = RequirementDiscovery.Hash(element.Source.Hash.Value + "\n" + body) } };
        }).ToArray();
        return this with { Elements = elements };

        string Block(string id)
        {
            var matches = anchors.Cast<Match>().Where(anchor => anchor.Groups[1].Value == id).ToArray();
            if (matches.Length != 1) throw new InvalidDataException("Missing or duplicate catalogue acceptance body.");
            var start = matches[0].Index;
            var contentStart = start + matches[0].Length;
            var next = anchors.Cast<Match>().FirstOrDefault(anchor => anchor.Index > start);
            while (next is not null && string.IsNullOrWhiteSpace(requirements[contentStart..next.Index]))
            {
                contentStart = next.Index + next.Length;
                next = anchors.Cast<Match>().FirstOrDefault(anchor => anchor.Index >= contentStart);
            }
            var body = requirements[start..(next?.Index ?? requirements.Length)];
            if (string.IsNullOrWhiteSpace(requirements[contentStart..(next?.Index ?? requirements.Length)]))
                throw new InvalidDataException("Empty catalogue acceptance body.");
            return body;
        }
    }

    private static PlannedMode ParseMode(string text)
    {
        var fields = text.Split('=');
        if (fields.Length != 2 || !Enum.TryParse<ModeDimension>(fields[0], false, out var dimension) ||
            !Enum.IsDefined(dimension) || Enum.GetName(dimension) != fields[0] ||
            !Enum.TryParse<ModeChoice>(fields[1], false, out var choice) || !Enum.IsDefined(choice) || Enum.GetName(choice) != fields[1])
            throw new InvalidDataException("Unknown planned mode discriminant.");
        var valid = dimension switch
        {
            ModeDimension.Configuration => choice == ModeChoice.Fixed,
            ModeDimension.Logic => choice is ModeChoice.And or ModeChoice.Or or ModeChoice.Xor or ModeChoice.Nor or ModeChoice.Nand,
            ModeDimension.OpticalChannel => choice is ModeChoice.Broadband or ModeChoice.Red or ModeChoice.Green or ModeChoice.Blue or ModeChoice.Yellow or ModeChoice.Cyan or ModeChoice.Magenta or ModeChoice.White,
            ModeDimension.AcousticTone => choice is ModeChoice.Low or ModeChoice.Mid or ModeChoice.High,
            ModeDimension.TubeAngle => choice is ModeChoice.Degrees45 or ModeChoice.Degrees90,
            _ => false
        };
        if (!valid) throw new InvalidDataException("Planned mode value belongs to another dimension.");
        return new(dimension, choice);
    }

    public void ValidateFixtures(string json)
    {
        using var document = JsonDocument.Parse(json);
        var expected = Fixtures.ToDictionary(fixture => fixture.Instance);
        var seen = new HashSet<RequirementId>();
        foreach (var level in document.RootElement.EnumerateArray())
        foreach (var part in level.GetProperty("parts").EnumerateArray())
        {
            var instance = new RequirementId(level.GetProperty("id").GetString() + "/" + part.GetProperty("id").GetString());
            var key = new SourceKey(RequirementOrigin.Catalogue, new RequirementId(part.GetProperty("kind").GetString()!));
            if (!seen.Add(instance) || !expected.TryGetValue(instance, out var fixture) || fixture.Catalogue != key ||
                fixture.Locked != part.GetProperty("locked").GetBoolean())
                throw new InvalidDataException("Fixture content differs from current declared identity, kind or locked state.");
        }
        if (!seen.SetEquals(expected.Keys)) throw new InvalidDataException("Authored fixture membership differs.");
    }

    public SourceArtifact[] ValidateResources(IReadOnlyCollection<RequirementId> admitted, Func<SourcePath, string?> read)
    {
        if (admitted.Count == 0 || admitted.Distinct().Count() != admitted.Count)
            throw new InvalidDataException("Missing or duplicate admitted resource identity.");
        var known = Elements.Select(element => element.Source.Key.Id).ToHashSet();
        var artifacts = new List<SourceArtifact>();
        foreach (var key in admitted)
        {
            if (!known.Contains(key)) throw new InvalidDataException("Admitted resource has no declared catalogue identity.");
            var path = new SourcePath(ResourceDirectory + "/" + key.Value + ".tres");
            var content = read(path) ?? throw new InvalidDataException("Admitted catalogue resource is missing.");
            var ids = Regex.Matches(content, "^Id = \"([^\"]+)\"$", RegexOptions.Multiline);
            if (ids.Count != 1 || ids[0].Groups[1].Value != key.Value)
                throw new InvalidDataException("Admitted resource identity differs.");
            artifacts.Add(new(path, RequirementDiscovery.Hash(content)));
        }
        return artifacts.ToArray();
    }
}
