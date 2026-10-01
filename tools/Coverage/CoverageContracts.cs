using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace CuriousContraptions.Coverage;

public enum RequirementOrigin { Element, Thermal, Radiation, Gap, Catalogue, Fixture, Research, Task }
public enum ReviewState { Unreviewed, Incomplete, Reviewed }
public enum ImplementationState { Unreviewed, Missing, Partial, Present }
public enum EvidenceState { Missing, Stale, Failed, Recorded }
public enum PublicationState { Unpublished, Committed, Pushed }

// Extensible source identities are values, never behavior selectors.
public readonly record struct RequirementId
{
    public string Value { get; }
    public RequirementId(string value)
    {
        if(string.IsNullOrWhiteSpace(value)||!Regex.IsMatch(value,@"\A[a-z0-9][a-z0-9_./-]*\z"))
            throw new ArgumentException("Invalid requirement identity.",nameof(value));
        Value=value;
    }
}
public readonly record struct SourceKey(RequirementOrigin Origin,RequirementId Id);
public readonly record struct ContentHash
{
    public string Value { get; }
    public ContentHash(string value)
    {
        if(value is null||!Regex.IsMatch(value,@"\A[0-9a-f]{64}\z"))
            throw new ArgumentException("Invalid SHA256.",nameof(value));
        Value=value;
    }
}
public sealed record SourceRequirement(SourceKey Key,string Location,string Title,ContentHash Hash);
public sealed record CoverageRecord(SourceRequirement Source,ReviewState Mapping,
    ImplementationState Implementation,EvidenceState Correctness,EvidenceState RealUi,
    EvidenceState Performance,PublicationState Publication);
public sealed record CoverageInventory(CoverageRecord[] Records);
public sealed record CoverageSummary(int Sources,int Unreviewed,int Changed,int Missing,int Orphaned,
    bool SourceInventoryCurrent,bool CompletionProven);

/// <summary>Strict external JSON boundary. Enum casing and integer values are rejected.</summary>
public static class CoverageJson
{
    private sealed class ExactEnum<T>:JsonConverter<T> where T:struct,Enum
    {
        public override T Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options)
        {
            if(reader.TokenType!=JsonTokenType.String)throw new JsonException("Expected canonical enum name.");
            var wire=reader.GetString();
            foreach(var value in Enum.GetValues<T>())
                if(string.Equals(value.ToString(),wire,StringComparison.Ordinal))return value;
            throw new JsonException("Unsupported enum value.");
        }
        public override void Write(Utf8JsonWriter writer,T value,JsonSerializerOptions options)
        {
            if(!Enum.IsDefined(value))throw new JsonException("Undefined enum value.");
            writer.WriteStringValue(value.ToString());
        }
    }
    private sealed class IdentityConverter:JsonConverter<RequirementId>
    {
        public override RequirementId Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options)=>
            new(reader.GetString()??throw new JsonException("Null identity."));
        public override void Write(Utf8JsonWriter writer,RequirementId value,JsonSerializerOptions options)=>
            writer.WriteStringValue(new RequirementId(value.Value).Value);
    }
    private sealed class HashConverter:JsonConverter<ContentHash>
    {
        public override ContentHash Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options)=>
            new(reader.GetString()??throw new JsonException("Null hash."));
        public override void Write(Utf8JsonWriter writer,ContentHash value,JsonSerializerOptions options)=>
            writer.WriteStringValue(new ContentHash(value.Value).Value);
    }
    private sealed class TextValue<T>(Func<string,T> read,Func<T,string> write):JsonConverter<T>
    {
        public override T Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options)=>
            read(reader.GetString()??throw new JsonException("Null typed value."));
        public override void Write(Utf8JsonWriter writer,T value,JsonSerializerOptions options)=>
            writer.WriteStringValue(write(value));
    }
    public static JsonSerializerOptions Options { get; }=Create();
    private static JsonSerializerOptions Create()
    {
        var options=new JsonSerializerOptions
        {
            WriteIndented=true,UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow,
            RespectRequiredConstructorParameters=true
        };
        options.Converters.Add(new ExactEnum<RequirementOrigin>());
        options.Converters.Add(new ExactEnum<ReviewState>());
        options.Converters.Add(new ExactEnum<ImplementationState>());
        options.Converters.Add(new ExactEnum<EvidenceState>());
        options.Converters.Add(new ExactEnum<PublicationState>());
        options.Converters.Add(new ExactEnum<EngineCapability>());
        options.Converters.Add(new ExactEnum<ObligationKind>());
        options.Converters.Add(new ExactEnum<SourceRelationKind>());
        options.Converters.Add(new ExactEnum<ConsumerKind>());
        options.Converters.Add(new ExactEnum<CapabilityAvailability>());
        options.Converters.Add(new ExactEnum<MeasurementUnit>());
        options.Converters.Add(new ExactEnum<ModeDimension>());
        options.Converters.Add(new ExactEnum<ModeChoice>());
        options.Converters.Add(new TextValue<WorkOrderId>(value=>new(value),value=>new WorkOrderId(value.Value).Value));
        options.Converters.Add(new TextValue<SourcePath>(value=>new(value),value=>new SourcePath(value.Value).Value));
        options.Converters.Add(new TextValue<SymbolId>(value=>new(value),value=>new SymbolId(value.Value).Value));
        options.Converters.Add(new IdentityConverter());options.Converters.Add(new HashConverter());
        options.MakeReadOnly(populateMissingResolver:true);
        return options;
    }
}

