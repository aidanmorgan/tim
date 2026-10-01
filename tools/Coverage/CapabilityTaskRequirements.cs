namespace CuriousContraptions.Coverage;

public enum RequiredTaskScope { NamedSources, Catalogue, Declarations, DeclarationsAndFixtures }
public sealed record RequiredTaskContract(RequirementId Id,ObligationKind Kind,EngineCapability[] Capabilities,RequiredTaskScope Scope,SourceKey[] Children);
public static partial class CapabilityTaskRequirements
{
    public static readonly IReadOnlyDictionary<SourceKey,RequiredTaskContract> Values;
    static CapabilityTaskRequirements()
    {
        Values=new RequiredTaskContract[][]{Group1,Group2,Group3,Group4,Group5,Group6,Group7,Group8}
            .SelectMany(group=>group).ToDictionary(row=>new SourceKey(RequirementOrigin.Task,row.Id));
    }
    public static IEnumerable<SourceKey> Children(RequiredTaskContract row,IReadOnlyList<SourceRequirement> sources)=>
        row.Children.Concat(sources.Where(source=>row.Scope switch
        {
            RequiredTaskScope.NamedSources=>false,
            RequiredTaskScope.Catalogue=>source.Key.Origin==RequirementOrigin.Catalogue,
            RequiredTaskScope.Declarations=>source.Key.Origin is RequirementOrigin.Catalogue or RequirementOrigin.Element or RequirementOrigin.Thermal or RequirementOrigin.Radiation or RequirementOrigin.Gap,
            RequiredTaskScope.DeclarationsAndFixtures=>source.Key.Origin is RequirementOrigin.Catalogue or RequirementOrigin.Fixture or RequirementOrigin.Element or RequirementOrigin.Thermal or RequirementOrigin.Radiation or RequirementOrigin.Gap,
            _=>throw new InvalidDataException("Unsupported required task scope.")
        }).Select(source=>source.Key)).Distinct();
}
