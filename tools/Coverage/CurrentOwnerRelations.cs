using System.Text.RegularExpressions;

namespace CuriousContraptions.Coverage;

public enum CurrentInputProblem { MissingScope, AmbiguousScope, UnresolvedProofRole, UnknownOwner, NoReviewedImplementationRole, ConflictingImplementationRole }
public sealed record CurrentInputIssue(CurrentInputProblem Problem, string Identity, WorkOrderId[] Candidates);
public sealed record CurrentOwnerRelations(
    IReadOnlySet<WorkOrderId> Members,
    IReadOnlyDictionary<SourceKey, WorkOrderId[]> Scopes,
    IReadOnlyDictionary<SourceKey, WorkOrderId[]> ProofRoles)
{
    public static CurrentOwnerRelations Read(DirectoryInfo root)
    {
        var members = Ownership.CurrentOwnerMembership.Read(root.FullName).Select(id => new WorkOrderId(id.Value)).ToHashSet();
        var documents = new[] { "engine", "gpu-physics", "refinements", "decisions", "scope-corrections", "current-consumers", "campaign" }
            .Select(name => File.ReadAllText(Path.Combine(root.FullName, "docs/planning/invest", name + ".md")));
        return Parse(members, documents);
    }

    public static CurrentOwnerRelations Parse(IReadOnlySet<WorkOrderId> members, IEnumerable<string> documents)
    {
        var scopes = new Dictionary<SourceKey, HashSet<WorkOrderId>>();
        var proof = new Dictionary<SourceKey, HashSet<WorkOrderId>>();
        foreach (var document in documents)
        {
            WorkOrderId? owner = null;
            foreach (var line in document.Split('\n'))
            {
                var heading = Regex.Match(line, @"^#{2,3} ([^\s]+)");
                if (heading.Success)
                {
                    owner = null;
                    foreach (var candidate in members)
                        if (candidate.Value == heading.Groups[1].Value) { owner = candidate; break; }
                }
                if (!line.Contains("**Exact source clauses:**", StringComparison.Ordinal)) continue;
                if (owner is null) throw new InvalidDataException("Exact source clause has no declared current owner heading.");
                var clauses = Regex.Matches(line, @"\[sequence-task-([0-9]{3})\]\(\.\./requirements\.md#sequence-task-\1\)");
                if (clauses.Count == 0 || Regex.Matches(line, Regex.Escape("../requirements.md#sequence-task-")).Count != clauses.Count) throw new InvalidDataException("Malformed exact source clauses.");
                var role = Regex.Match(line, @"Original owner, all criteria and technical stage: ([A-Z0-9-]+)\.");
                if (role.Success && role.Groups[1].Value != owner.Value.Value)
                    throw new InvalidDataException("Grouped role owner differs from its current heading.");
                foreach (Match clause in clauses)
                {
                    var key = new SourceKey(RequirementOrigin.Task, new("sequence-task-" + clause.Groups[1].Value));
                    Add(scopes, key, owner.Value);
                    // The explicit all-criteria grant, not cardinality or a hyperlink, authorizes the grouped proof role.
                    if (role.Success) Add(proof, key, owner.Value);
                }
            }
        }
        return new(members, Freeze(scopes), Freeze(proof));
    }

    public WorkOrderId? ResolveProof(SourceKey key, List<CurrentInputIssue> issues)
    {
        var scopes = Scopes.GetValueOrDefault(key, []);
        var roles = ProofRoles.GetValueOrDefault(key, []);
        if (scopes.Length == 0) issues.Add(new(CurrentInputProblem.MissingScope, key.Id.Value, []));
        else if (scopes.Length > 1) issues.Add(new(CurrentInputProblem.AmbiguousScope, key.Id.Value, scopes));
        else if (roles.Length != 1 || roles[0] != scopes[0])
            issues.Add(new(CurrentInputProblem.UnresolvedProofRole, key.Id.Value, scopes));
        else return roles[0];
        return null;
    }

    private static void Add(Dictionary<SourceKey, HashSet<WorkOrderId>> values, SourceKey key, WorkOrderId owner)
    {
        if (!values.TryGetValue(key, out var owners)) values.Add(key, owners = []);
        owners.Add(owner);
    }
    private static IReadOnlyDictionary<SourceKey, WorkOrderId[]> Freeze(Dictionary<SourceKey, HashSet<WorkOrderId>> values) =>
        values.ToDictionary(pair => pair.Key, pair => pair.Value.OrderBy(owner => owner.Value, StringComparer.Ordinal).ToArray());
}
