namespace CuriousContraptions.Coverage;

public static class CoverageAudit
{
    public static CoverageInventory Seed(IEnumerable<SourceRequirement> sources)
    {
        var array=sources.ToArray();RequirementDiscovery.RequireUnique(array);
        return new(array.Select(source=>new CoverageRecord(source,ReviewState.Unreviewed,
            ImplementationState.Unreviewed,EvidenceState.Missing,EvidenceState.Missing,
            EvidenceState.Missing,PublicationState.Unpublished)).ToArray());
    }
    public static CoverageSummary Analyze(IEnumerable<SourceRequirement> current,CoverageInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(inventory.Records);
        var sources=current.ToArray();RequirementDiscovery.RequireUnique(sources);
        if(inventory.Records.Any(record=>record is null||record.Source is null))
            throw new InvalidDataException("Null coverage record.");
        RequirementDiscovery.RequireUnique(inventory.Records.Select(record=>record.Source));
        foreach(var record in inventory.Records)
            if(!Enum.IsDefined(record.Mapping)||!Enum.IsDefined(record.Implementation)||
                !Enum.IsDefined(record.Correctness)||!Enum.IsDefined(record.RealUi)||
                !Enum.IsDefined(record.Performance)||!Enum.IsDefined(record.Publication))
                throw new InvalidDataException("Unsupported coverage state.");
        var expected=sources.ToDictionary(record=>record.Key);
        var actual=inventory.Records.ToDictionary(record=>record.Source.Key);
        var missing=expected.Keys.Count(key=>!actual.ContainsKey(key));
        var orphaned=actual.Keys.Count(key=>!expected.ContainsKey(key));
        var changed=expected.Count(pair=>actual.TryGetValue(pair.Key,out var record)&&record.Source!=pair.Value);
        var unreviewed=inventory.Records.Count(record=>record.Mapping!=ReviewState.Reviewed);
        // A source inventory cannot certify any implementation or evidence claims.
        // Canonical element/mode/process reconciliation is a subsequent required layer.
        return new(sources.Length,unreviewed,changed,missing,orphaned,
            missing==0&&orphaned==0&&changed==0,false);
    }
}
