namespace CuriousContraptions.Coverage;

/// <summary>Reviewed role input only. The empty current map records unfinished policy coverage, not absence in authority.</summary>
public static class CurrentImplementationRoles
{
    public static IReadOnlyDictionary<EngineCapability, CapabilityOwners> Reviewed { get; } =
        new Dictionary<EngineCapability, CapabilityOwners>();

    public sealed record Result(IReadOnlySet<WorkOrderId> Eligible, CurrentInputIssue[] Issues);
    public static Result Validate(IReadOnlySet<WorkOrderId> members,
        IReadOnlyDictionary<EngineCapability, CapabilityOwners> required,
        IReadOnlyDictionary<EngineCapability, CapabilityOwners> reviewed)
    {
        foreach (var pair in reviewed)
            if (!Enum.IsDefined(pair.Key) || !required.ContainsKey(pair.Key))
                throw new InvalidDataException("Reviewed role input has an unknown capability.");
        var eligible = new HashSet<WorkOrderId>();
        var issues = new List<CurrentInputIssue>();
        foreach (var pair in required.OrderBy(pair => pair.Key))
        {
            if (!Enum.IsDefined(pair.Key)) throw new InvalidDataException("Required role input has an undefined capability.");
            ValidateShape(pair.Value);
            if (!reviewed.TryGetValue(pair.Key, out var actual))
            {
                issues.Add(new(CurrentInputProblem.NoReviewedImplementationRole, pair.Key.ToString(), pair.Value.Implementation));
                continue;
            }
            ValidateShape(actual);
            var unknown = new[] { actual.Design, actual.Proof }.Concat(actual.Implementation).Distinct()
                .Where(owner => !members.Contains(owner)).ToArray();
            foreach (var owner in unknown) issues.Add(new(CurrentInputProblem.UnknownOwner, owner.Value, []));
            if (actual.Design != pair.Value.Design || actual.Proof != pair.Value.Proof ||
                !actual.Implementation.ToHashSet().SetEquals(pair.Value.Implementation))
                issues.Add(new(CurrentInputProblem.ConflictingImplementationRole, pair.Key.ToString(), actual.Implementation));
            else if (unknown.Length == 0) eligible.UnionWith(actual.Implementation);
        }
        return new(eligible, issues.ToArray());
    }

    private static void ValidateShape(CapabilityOwners role)
    {
        if (role is null || role.Implementation is null || role.Implementation.Length == 0 ||
            role.Implementation.Distinct().Count() != role.Implementation.Length)
            throw new InvalidDataException("Missing or duplicate implementation role identity.");
        _ = new WorkOrderId(role.Design.Value);
        _ = new WorkOrderId(role.Proof.Value);
        foreach (var owner in role.Implementation) _ = new WorkOrderId(owner.Value);
    }
}
