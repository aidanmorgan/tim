using System.Text.RegularExpressions;

namespace CuriousContraptions.Coverage;

public readonly record struct NamedSourceReference(SourcePath Path, RequirementId Anchor);
public sealed record PlannedNamedPackage(RequirementId Package, WorkOrderId Identity,
    SourceRequirement Source, SourceArtifact Declaration, NamedSourceReference Ledger,
    NamedSourceReference Acceptance, NamedSourceReference Decision, PlannedMode[] Modes);

/// <summary>Explicit provenance for named packages; membership still comes from the generated bundle.</summary>
public static class NamedPackageSources
{
    public const string Marker = "## Package source binding";
    public static readonly SourcePath LedgerPath = new("docs/planning/invest/named-elements.md");
    public static readonly SourcePath DecisionPath = new("docs/planning/invest/scope-corrections.md");

    public static PlannedNamedPackage[] Read(DirectoryInfo root, string requirements)
    {
        var documents = Directory.GetFiles(Path.Combine(root.FullName, "docs/planning/elements"), "EL-*.md")
            .Order(StringComparer.Ordinal)
            .Select(path => (Path: new SourcePath(Path.GetRelativePath(root.FullName, path).Replace('\\', '/')),
                Text: File.ReadAllText(path)))
            .Where(document => document.Text.Contains(Marker, StringComparison.Ordinal)).ToArray();
        if (documents.Length == 0) return [];
        return Parse(documents, requirements, path =>
        {
            var full = Path.Combine(root.FullName, path.Value);
            return File.Exists(full) ? File.ReadAllText(full) : null;
        });
    }

    public static PlannedNamedPackage[] Parse(IEnumerable<(SourcePath Path, string Text)> documents,
        string requirements, Func<SourcePath, string?> read)
    {
        var tasks = RequirementDiscovery.Todo(requirements).ToDictionary(source => source.Key);
        var result = documents.Select(document => ParseDocument(document.Path, document.Text, requirements, tasks, read)).ToArray();
        ValidateUnique(result);
        return result;
    }

    public static void ValidateUnique(IReadOnlyCollection<PlannedNamedPackage> sources)
    {
        if (sources.Select(source => source.Package).Distinct().Count() != sources.Count)
            throw new InvalidDataException("Duplicate named package binding.");
        if (sources.Select(source => source.Identity).Distinct().Count() != sources.Count)
            throw new InvalidDataException("Duplicate named identity binding.");
        if (sources.Select(source => source.Source.Key).Distinct().Count() != sources.Count)
            throw new InvalidDataException("Duplicate named task source binding.");
    }

    private static PlannedNamedPackage ParseDocument(SourcePath path, string text, string requirements,
        Dictionary<SourceKey, SourceRequirement> tasks, Func<SourcePath, string?> read)
    {
        var block = BindingBlock(text);
        var binding = Regex.Match(block, @"\A## Package source binding\r?\n\r?\n" +
            @"\*\*Package:\*\* ([a-z0-9_]+)\.\r?\n" +
            @"\*\*Named identity:\*\* \[(EL-([0-9]{3}))\]\(\.\./invest/named-elements\.md#(element-\3)\)\.\r?\n" +
            @"\*\*Acceptance:\*\* \[(todo-[0-9]+)\]\(\.\./requirements\.md#\5\), " +
            @"\[(sequence-task-[0-9]+)\]\(\.\./requirements\.md#\6\)\.\r?\n" +
            @"\*\*Decision:\*\* \[S([0-9]{3})\]\(\.\./invest/scope-corrections\.md#s\7\)\.\r?\n" +
            @"\*\*Mode records:\*\* ([^\r\n]+)\.\z");
        if (!binding.Success) throw new InvalidDataException("Malformed named package source binding.");
        var identity = new WorkOrderId(binding.Groups[2].Value);
        if (!Path.GetFileName(path.Value).StartsWith(identity.Value + "-", StringComparison.Ordinal) ||
            !text.StartsWith("# " + identity.Value + " · ", StringComparison.Ordinal))
            throw new InvalidDataException("Named declaration filename or title differs from identity.");
        var ledger = new NamedSourceReference(LedgerPath, new(binding.Groups[4].Value));
        var acceptance = new NamedSourceReference(RequirementDiscovery.RequirementsPath, new(binding.Groups[6].Value));
        var decision = new NamedSourceReference(DecisionPath, new("s" + binding.Groups[7].Value));
        var ledgerBody = RequiredBlock(read, ledger);
        var decisionBody = RequiredBlock(read, decision);
        ValidateLedger(ledgerBody, identity, binding.Groups[5].Value);
        ValidateDecision(ledgerBody, decision);
        ValidateAcceptance(requirements, binding.Groups[5].Value, acceptance.Anchor);
        var key = new SourceKey(RequirementOrigin.Task, acceptance.Anchor);
        if (!tasks.TryGetValue(key, out var source)) throw new InvalidDataException("Unknown named task source.");
        var provenance = source.Hash.Value + "\n" + path.Value + "\n" + text + "\n" + ledgerBody + "\n" + decisionBody;
        return new(new(binding.Groups[1].Value), identity,
            source with { Hash = RequirementDiscovery.Hash(provenance) },
            new(path, RequirementDiscovery.Hash(text)), ledger, acceptance, decision,
            CurrentCatalogue.ParseModes(binding.Groups[8].Value));
    }

    private static string BindingBlock(string text)
    {
        var markers = Regex.Matches(text, @"(?m)^## Package source binding\r?$");
        if (markers.Count != 1) throw new InvalidDataException("Missing or duplicate named package source binding.");
        var start = markers[0].Index;
        var end = text.IndexOf("\n## ", start + markers[0].Length, StringComparison.Ordinal);
        return text[start..(end < 0 ? text.Length : end)].TrimEnd();
    }

    private static void ValidateLedger(string body, WorkOrderId identity, string todo)
    {
        if (!Regex.IsMatch(body, @"(?m)^### element-" + identity.Value[3..] + " · " + identity.Value + @" · ") ||
            Regex.Count(body, @"\]\(\.\./requirements\.md#todo-[0-9]+\)") != 1 ||
            !body.Contains("](../requirements.md#" + todo + ")", StringComparison.Ordinal))
            throw new InvalidDataException("Named ledger identity or acceptance differs.");
    }

    private static void ValidateDecision(string ledgerBody, NamedSourceReference decision)
    {
        var owners = Regex.Matches(ledgerBody, @"\bOwner \*\*S([0-9]{3})\*\*");
        if (owners.Count != 1)
            throw new InvalidDataException("Missing or ambiguous named ledger decision owner.");
        if (decision.Anchor != new RequirementId("s" + owners[0].Groups[1].Value))
            throw new InvalidDataException("Named decision differs from its ledger owner.");
    }

    private static void ValidateAcceptance(string text, string todo, RequirementId task)
    {
        var todoMatch = UniqueAnchor(text, todo);
        var taskMatch = UniqueAnchor(text, task.Value);
        var gapStart = todoMatch.Index + todoMatch.Length;
        if (taskMatch.Index < gapStart ||
            !Regex.IsMatch(text[gapStart..taskMatch.Index], @"\A\s*(?:\[[ xX]\])?\s*\z"))
            throw new InvalidDataException("Named todo and task acceptance are not co-anchored.");
        _ = AnchorBlock(text, task.Value);
    }

    private static string RequiredBlock(Func<SourcePath, string?> read, NamedSourceReference reference)
    {
        var text = read(reference.Path) ?? throw new InvalidDataException("Missing named source document: " + reference.Path.Value);
        return AnchorBlock(text, reference.Anchor.Value);
    }

    private static Match UniqueAnchor(string text, string id)
    {
        var matches = Regex.Matches(text, "<a id=\"" + Regex.Escape(id) + "\"></a>");
        if (matches.Count != 1) throw new InvalidDataException("Missing or duplicate named source anchor: " + id);
        return matches[0];
    }

    private static string AnchorBlock(string text, string id)
    {
        var anchor = UniqueAnchor(text, id);
        var start = anchor.Index + anchor.Length;
        var next = Regex.Match(text[start..], "<a id=\"[^\"]+\"></a>");
        var end = next.Success ? start + next.Index : text.Length;
        if (string.IsNullOrWhiteSpace(text[start..end]))
            throw new InvalidDataException("Empty named source acceptance: " + id);
        return text[anchor.Index..end].TrimEnd();
    }
}
