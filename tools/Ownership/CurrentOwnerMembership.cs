using System.Globalization;
using System.Text.RegularExpressions;

namespace Ownership;

/// <summary>Current declared ID membership only; never source-owner selection or evidence approval.</summary>
public static class CurrentOwnerMembership
{
    public static IReadOnlySet<WorkId> Read(string root) => Parse(
        ReadDocument(root, "engine.md"), ReadDocument(root, "current-consumers.md"),
        [ReadDocument(root, "gpu-physics.md"), ReadDocument(root, "refinements.md"),
         ReadDocument(root, "decisions.md"), ReadDocument(root, "scope-corrections.md"),
         ReadDocument(root, "campaign.md")]);

    private static string ReadDocument(string root, string name) =>
        File.ReadAllText(Path.Combine(root, "docs/planning/invest", name));

    public static IReadOnlySet<WorkId> Parse(string engine, string consumers, IReadOnlyList<string> scopes)
    {
        var owners = new HashSet<WorkId>();
        foreach (var document in new[] { engine, consumers }.Concat(scopes))
        {
            var count = 0;
            foreach (var line in document.Split('\n'))
            {
                if (!Regex.IsMatch(line, @"^#{2,3} (?:(?:P0|CAT|LAW|ENGINE|OPT|LEVEL|ANIM|MOBILE)-|S[0-9])")) continue;
                var heading = Regex.Match(line, @"^#{2,3} ([^\s]+)(?:\s|$)");
                if (!heading.Success) throw new InvalidDataException("Malformed current owner heading.");
                owners.Add(Identity(heading.Groups[1].Value));
                count++;
            }
            if (count == 0) throw new InvalidDataException("Current authority document has no declared owner headings.");
        }

        const string tableHeading = "## Disposition of every P0 ID";
        var start = engine.IndexOf(tableHeading, StringComparison.Ordinal);
        if (start < 0 || engine.IndexOf(tableHeading, start + tableHeading.Length, StringComparison.Ordinal) >= 0)
            throw new InvalidDataException("Missing or duplicate current P0 disposition table.");
        var end = engine.IndexOf("\n## ", start + tableHeading.Length, StringComparison.Ordinal);
        var table = end < 0 ? engine[start..] : engine[start..end];
        var p0Count = 0;
        var p0Ids = new HashSet<WorkId>();
        foreach (var line in table.Split('\n').Where(line => line.StartsWith("| ", StringComparison.Ordinal)))
        {
            var fields = line.Split('|');
            if (fields.Length != 5) throw new InvalidDataException("Malformed current P0 table row.");
            var cell = fields[1].Trim();
            if (cell is "ID" or "---") continue;
            foreach (var value in cell.Split(" / ", StringSplitOptions.None))
            {
                var range = Regex.Match(value, @"\AP0-([0-9]{3})(?:–([0-9]{3}))?\z");
                if (!range.Success) throw new InvalidDataException("Unknown current P0 ID syntax.");
                var first = int.Parse(range.Groups[1].Value, CultureInfo.InvariantCulture);
                var last = range.Groups[2].Success ? int.Parse(range.Groups[2].Value, CultureInfo.InvariantCulture) : first;
                if (first < 1 || last < first) throw new InvalidDataException("Invalid current P0 range.");
                for (var ordinal = first; ordinal <= last; ordinal++)
                    p0Ids.Add(new("P0-" + ordinal.ToString("D3", CultureInfo.InvariantCulture)));
                p0Count++;
            }
        }
        if (p0Count == 0) throw new InvalidDataException("Current P0 disposition table is empty.");
        if (owners.Any(owner => owner.Value.StartsWith("P0-", StringComparison.Ordinal) && !p0Ids.Contains(owner)))
            throw new InvalidDataException("P0 owner heading is absent from its authoritative disposition table.");
        owners.UnionWith(p0Ids);

        const string fixtureHeading = "## Exact fixture proof children";
        var fixtureStart = consumers.IndexOf(fixtureHeading, StringComparison.Ordinal);
        if (fixtureStart < 0 || consumers.IndexOf(fixtureHeading, fixtureStart + fixtureHeading.Length, StringComparison.Ordinal) >= 0)
            throw new InvalidDataException("Current fixture owner table is missing or duplicated.");
        var fixtureEnd = consumers.IndexOf("\n## ", fixtureStart + fixtureHeading.Length, StringComparison.Ordinal);
        var fixtureTable = fixtureEnd < 0 ? consumers[fixtureStart..] : consumers[fixtureStart..fixtureEnd];
        var fixtureCount = 0;
        var fixtureIds = new HashSet<WorkId>();
        foreach (var line in fixtureTable.Split('\n').Where(line => line.StartsWith("|", StringComparison.Ordinal)))
        {
            if (line.StartsWith("| Canonical owner |", StringComparison.Ordinal) || line.StartsWith("| --- |", StringComparison.Ordinal)) continue;
            var row = Regex.Match(line.TrimEnd('\r'), "^\\| <a id=\"(fix-[0-9]{3}-[0-9]{3})\"></a>(FIX-[0-9]{3}-[0-9]{3}) \\| [^|]+ \\| [^|]+ \\| (CAT-[0-9]{3}-V) \\| (FIX-[0-9]{3}-[0-9]{3}) \\|$");
            if (!row.Success || row.Groups[1].Value != row.Groups[2].Value.ToLowerInvariant() || row.Groups[2].Value != row.Groups[4].Value)
                throw new InvalidDataException("Malformed current fixture owner declaration.");
            var fixture = Identity(row.Groups[2].Value);
            if (!fixtureIds.Add(fixture)) throw new InvalidDataException("Duplicate current fixture identity.");
            var proof = Identity(row.Groups[3].Value);
            // A declared proof criterion is distinct from its declared delivery owner; no suffix alias is added.
            if (!owners.Contains(new(proof.Value[..^1] + "I")))
                throw new InvalidDataException("Fixture proof criterion has no declared catalogue owner.");
            owners.Add(fixture);
            owners.Add(proof);
            fixtureCount++;
        }
        if (fixtureCount == 0) throw new InvalidDataException("Current fixture owner table is empty.");
        return owners;
    }

    private static WorkId Identity(string value)
    {
        if (!Regex.IsMatch(value, @"\A(?:P0-[0-9]{3}|S[0-9]{3}(?:-[A-Z]+)?|CAT-[0-9]{3}-[A-Z]+|LAW-[A-Z]+-[A-Z]+|ENGINE-[A-Z0-9]+(?:-[A-Z0-9]+)*|OPT-[A-Z]+(?:-[A-Z]+)*|ANIM-[0-9]+|MOBILE-[0-9]{2}|LEVEL-[0-9]{3}|FIX-[0-9]{3}-[0-9]{3})\z"))
            throw new InvalidDataException("Invalid current owner identity.");
        return new(value);
    }
}
