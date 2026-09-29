namespace CuriousContraptions.Coverage;

public readonly record struct ElementId(RequirementId Identity);
public sealed record FixtureRequirement(SourceRequirement Source,SourceKey Catalogue);
public sealed record CatalogueElement(ElementId Id,CoverageRecord Catalogue,
    FixtureRequirement[] Fixtures,ReviewState Modes,ReviewState Processes);
public sealed record ElementManifest(CatalogueElement[] Elements,SourceRequirement[] UnresolvedSources);
public sealed record ElementSummary(int Elements,int Fixtures,int UnresolvedSources,
    int PendingModeReviews,int PendingProcessReviews,bool LinksCurrent,bool CompletionProven);

/// <summary>One entry per catalogue identity, including differently configured variants.
/// Specification equivalence and mode/process coverage require explicit later review.</summary>
public static class ElementAudit
{
    public static ElementManifest Seed(IEnumerable<SourceRequirement> sources,IEnumerable<FixtureRequirement> fixtures)
    {
        var all=sources.ToArray();RequirementDiscovery.RequireUnique(all);
        var bindings=fixtures.ToArray();ValidateBindings(all,bindings);
        var entries=all.Where(source=>source.Key.Origin==RequirementOrigin.Catalogue).ToArray();
        var elements=entries.Select(source=>new CatalogueElement(new(source.Key.Id),
            CoverageAudit.Seed([source]).Records[0],
            bindings.Where(fixture=>fixture.Catalogue==source.Key).ToArray(),
            ReviewState.Unreviewed,ReviewState.Unreviewed)).ToArray();
        return new(elements,all.Where(source=>source.Key.Origin is not
            (RequirementOrigin.Catalogue or RequirementOrigin.Fixture)).ToArray());
    }
    private static void ValidateBindings(SourceRequirement[] sources,FixtureRequirement[] fixtures)
    {
        RequirementDiscovery.RequireUnique(fixtures.Select(fixture=>fixture.Source));
        var known=sources.ToDictionary(source=>source.Key);
        foreach(var fixture in fixtures)
        {
            if(fixture.Source.Key.Origin!=RequirementOrigin.Fixture||
                fixture.Catalogue.Origin!=RequirementOrigin.Catalogue||
                !known.TryGetValue(fixture.Source.Key,out var current)||current!=fixture.Source||
                !known.ContainsKey(fixture.Catalogue))
                throw new InvalidDataException("Fixture binding references an unknown or changed source.");
        }
        if(sources.Count(source=>source.Key.Origin==RequirementOrigin.Fixture)!=fixtures.Length)
            throw new InvalidDataException("Every fixture requires its own catalogue binding.");
    }
    public static ElementSummary Analyze(IEnumerable<SourceRequirement> sources,
        IEnumerable<FixtureRequirement> fixtures,ElementManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(manifest.Elements);
        ArgumentNullException.ThrowIfNull(manifest.UnresolvedSources);
        var current=sources.ToArray();RequirementDiscovery.RequireUnique(current);
        var bindings=fixtures.ToArray();ValidateBindings(current,bindings);
        var byFixture=bindings.ToDictionary(fixture=>fixture.Source.Key);
        var elementIds=new HashSet<ElementId>();
        var mappedSources=new List<SourceRequirement>();
        var mappedFixtures=new HashSet<SourceKey>();
        var linksCurrent=true;
        foreach(var element in manifest.Elements)
        {
            if(element is null||element.Catalogue is null||element.Catalogue.Source is null||element.Fixtures is null)
                throw new InvalidDataException("Null element mapping.");
            _=new RequirementId(element.Id.Identity.Value);
            if(!elementIds.Add(element.Id))throw new InvalidDataException("Duplicate element identity.");
            var catalogue=element.Catalogue.Source;
            if(catalogue.Key.Origin!=RequirementOrigin.Catalogue||element.Id.Identity!=catalogue.Key.Id)
                throw new InvalidDataException("Catalogue identities must retain distinct element records.");
            if(!Enum.IsDefined(element.Modes)||!Enum.IsDefined(element.Processes))
                throw new InvalidDataException("Undefined element review state.");
            // This first manifest has no mode/process records capable of substantiating review.
            if(element.Modes==ReviewState.Reviewed||element.Processes==ReviewState.Reviewed)
                throw new InvalidDataException("Mode/process records are required before review can be completed.");
            _=CoverageAudit.Analyze([catalogue],new([element.Catalogue]));
            mappedSources.Add(catalogue);
            foreach(var fixture in element.Fixtures)
            {
                if(fixture is null||fixture.Source is null||fixture.Source.Key.Origin!=RequirementOrigin.Fixture||
                    fixture.Catalogue!=catalogue.Key||!mappedFixtures.Add(fixture.Source.Key))
                    throw new InvalidDataException("Invalid or duplicate fixture ownership.");
                if(!byFixture.TryGetValue(fixture.Source.Key,out var actual)||actual!=fixture)linksCurrent=false;
                mappedSources.Add(fixture.Source);
            }
        }
        if(manifest.UnresolvedSources.Any(source=>source is null||source.Key.Origin is
            RequirementOrigin.Catalogue or RequirementOrigin.Fixture))
            throw new InvalidDataException("Catalogue entries and fixtures cannot be hidden in unresolved specifications.");
        mappedSources.AddRange(manifest.UnresolvedSources);
        var audit=CoverageAudit.Analyze(current,CoverageAudit.Seed(mappedSources));
        linksCurrent&=audit.SourceInventoryCurrent&&mappedFixtures.Count==bindings.Length;
        return new(manifest.Elements.Length,mappedFixtures.Count,manifest.UnresolvedSources.Length,
            manifest.Elements.Count(element=>element.Modes!=ReviewState.Reviewed),
            manifest.Elements.Count(element=>element.Processes!=ReviewState.Reviewed),linksCurrent,false);
    }
}
