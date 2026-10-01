using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ownership;

public static class OwnershipAudit
{
    public static OwnershipContract Read(string root, string indexPath)
    {
        var index = Decode<OwnershipIndex>(File.ReadAllText(indexPath));
        if (index.Files is not { Length: > 0 } || index.Files.Distinct().Count() != index.Files.Length)
            throw new InvalidDataException("Ownership index requires unique shards.");
        var sources = new List<SourceInput>();
        var assignments = new List<OwnershipAssignment>();
        foreach (var file in index.Files)
        {
            if (string.IsNullOrWhiteSpace(file.Value) || Path.IsPathRooted(file.Value) ||
                file.Value.Split('/').Any(part => part is "." or ".." or "") || file.Value.Contains('\\'))
                throw new InvalidDataException("Shard path must be a canonical repository-relative resource.");
            var shard = Decode<OwnershipContract>(File.ReadAllText(Path.Combine(root, file.Value)));
            if (shard.Sources is null || shard.Assignments is null) throw new InvalidDataException("Missing shard arrays.");
            sources.AddRange(shard.Sources); assignments.AddRange(shard.Assignments);
        }
        return new(sources.ToArray(), assignments.ToArray());
    }
    public static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static T Decode<T>(string json)
    {
        using var document = JsonDocument.Parse(json);
        RejectDuplicates(document.RootElement);
        return document.RootElement.Deserialize<T>(Json) ?? throw new InvalidDataException("Null ownership document.");
    }

    private static void RejectDuplicates(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new InvalidDataException("Duplicate ownership JSON field.");
                RejectDuplicates(property.Value);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var child in value.EnumerateArray()) RejectDuplicates(child);
    }

    public static string Fingerprint(StateMember member) =>
        Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(member, Json)));

    public static void Validate(OwnershipSnapshot current, OwnershipContract contract, IReadOnlySet<WorkId> tasks)
    {
        if (current.Diagnostics.Length != 0)
            throw new InvalidDataException("Semantic binding errors make the ownership census incomplete.");
        if (contract.Sources is null || contract.Assignments is null ||
            contract.Sources.Any(source => source is null) || contract.Assignments.Any(assignment => assignment is null))
            throw new InvalidDataException("Null source or assignment record.");
        var sources = current.Sources.ToDictionary(source => source.Path);
        if (contract.Sources.Length != sources.Count) throw new InvalidDataException("Source membership differs.");
        var admittedSources = new HashSet<SourcePath>();
        foreach (var source in contract.Sources)
            if (!admittedSources.Add(source.Path) || !sources.TryGetValue(source.Path, out var expected) ||
                expected.Sha256 != source.Sha256)
                throw new InvalidDataException("Duplicate, unknown or stale source.");
        var members = current.Members.ToDictionary(member => member.Id);
        if (contract.Assignments.Length != members.Count) throw new InvalidDataException("State ownership membership differs.");
        var admitted = new HashSet<MemberId>();
        foreach (var assignment in contract.Assignments)
        {
            if (!admitted.Add(assignment.Member) || !members.TryGetValue(assignment.Member, out var member))
                throw new InvalidDataException("Duplicate or unknown member.");
            if (!Enum.IsDefined(assignment.Owner) || !Enum.IsDefined(assignment.Rule))
                throw new InvalidDataException("Unknown ownership discriminant.");
            if (!tasks.Contains(assignment.Migration)) throw new InvalidDataException("Unknown migration owner.");
            if (Fingerprint(member) != assignment.MemberSha256)
                throw new InvalidDataException("Member or caller/writer closure is stale.");
            var valid = assignment.Rule switch
            {
                OwnershipRule.SimulationAuthority => assignment.Owner == AssemblyOwner.SimulationCore,
                OwnershipRule.AnimationAuthority => assignment.Owner == AssemblyOwner.AnimationKernel,
                OwnershipRule.SceneResource or OwnershipRule.SceneCapture => assignment.Owner == AssemblyOwner.GodotPresenter,
                OwnershipRule.ImmutableContract => assignment.Owner is AssemblyOwner.Protocol or AssemblyOwner.Geometry,
                OwnershipRule.InvocationScratch => assignment.Owner is AssemblyOwner.Geometry or AssemblyOwner.SimulationCore,
                OwnershipRule.CompilerState => assignment.Owner == AssemblyOwner.ConstructionCompiler,
                OwnershipRule.HostState => assignment.Owner is AssemblyOwner.SimulationHost or AssemblyOwner.AnimationHost,
                _ => false
            };
            if (!valid) throw new InvalidDataException("Assembly placement and instance ownership conflict.");
            OwnershipPolicy.Validate(member, assignment);
        }
    }
}
