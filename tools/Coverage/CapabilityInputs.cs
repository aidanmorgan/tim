using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CuriousContraptions.Coverage;

/// <summary>Current declaration and admitted-resource inputs; unresolved global roles remain explicit.</summary>
public static class CapabilityInputs
{
    public static void ValidateEnumDeclaration(string text,SymbolId declaration,IReadOnlyDictionary<SymbolId,int> expected)
    {
        var match=Regex.Matches(text,@"public\s+enum\s+"+Regex.Escape(declaration.Value)+@"\s*\{([^}]+)\}");
        if(match.Count!=1)throw new InvalidDataException("Missing or duplicate canonical mode enum.");
        var actual=new Dictionary<SymbolId,int>();var next=0;
        foreach(var item in match[0].Groups[1].Value.Split(','))
        {
            if(string.IsNullOrWhiteSpace(item))continue;
            var member=Regex.Match(item,@"\A\s*([A-Za-z_][A-Za-z0-9_]*)\s*(?:=\s*([0-9]+))?\s*\z");
            if(!member.Success)throw new InvalidDataException("Unsupported canonical mode enum declaration.");
            var ordinal=next;
            if(member.Groups[2].Success&&
                !int.TryParse(member.Groups[2].Value,NumberStyles.None,CultureInfo.InvariantCulture,out ordinal))
                throw new InvalidDataException("Canonical mode enum ordinal is outside the supported integer range.");
            if(ordinal==int.MaxValue)throw new InvalidDataException("Canonical mode enum ordinal cannot advance within the supported integer range.");
            if(!actual.TryAdd(new(member.Groups[1].Value),ordinal))throw new InvalidDataException("Duplicate canonical mode enum member.");
            next=checked(ordinal+1);
        }
        if(actual.Count!=expected.Count||expected.Any(pair=>!actual.TryGetValue(pair.Key,out var ordinal)||ordinal!=pair.Value))
            throw new InvalidDataException("Canonical mode enum differs from verified serialized mapping.");
    }
    public static CapabilityInventory Read(DirectoryInfo root,string indexPath)
    {
        var index=ReadJson<CapabilityInventoryIndex>(indexPath);
        if(index.Files is null||index.Files.Length==0||index.Files.Distinct().Count()!=index.Files.Length)
            throw new InvalidDataException("Missing or duplicate inventory files.");
        var documents=new List<CapabilityInventory>();
        foreach(var path in index.Files)
        {
            _=new SourcePath(path.Value);
            documents.Add(ReadJson<CapabilityInventory>(Path.Combine(root.FullName,path.Value)));
        }
        if(documents.Any(x=>x.Capabilities is null||x.Consumers is null||x.Bindings is null||x.Modes is null))
            throw new InvalidDataException("Null inventory shard collection.");
        return new(documents.SelectMany(x=>x.Capabilities).ToArray(),documents.SelectMany(x=>x.Consumers).ToArray(),
            documents.SelectMany(x=>x.Bindings).ToArray(),documents.SelectMany(x=>x.Modes).ToArray());
    }
    private static T ReadJson<T>(string path)=>JsonSerializer.Deserialize<T>(File.ReadAllText(path),CoverageJson.Options)
        ??throw new InvalidDataException("Null inventory document.");
    public sealed record Preparation(CapabilityExpectations Expectations, CurrentInputIssue[] Issues);
    public static CapabilityExpectations Discover(DirectoryInfo root, IReadOnlyList<SourceRequirement> sources)
    {
        var prepared = Prepare(root, sources);
        if (prepared.Issues.Length != 0)
            throw new InvalidDataException("Current capability inputs unresolved:\n" +
                string.Join("\n", prepared.Issues.Select(issue =>
                    $"{issue.Problem}: {issue.Identity} [{string.Join(", ", issue.Candidates.Select(candidate => candidate.Value))}]")));
        return prepared.Expectations;
    }

    public static Preparation Prepare(DirectoryInfo root, IReadOnlyList<SourceRequirement> sources)
    {
        var text = File.ReadAllText(Path.Combine(root.FullName, RequirementDiscovery.RequirementsPath.Value));
        var catalogue = CurrentCatalogue.Read(root);
        var current = CurrentOwnerRelations.Read(root);
        var issues = new List<CurrentInputIssue>();
        var sourceOwners = new Dictionary<SourceKey, WorkOrderId>();
        var taskAnchors = SourceTaskAnchors(text);
        foreach (var source in sources.Where(source => source.Key.Origin == RequirementOrigin.Task))
            if (current.ResolveProof(source.Key, issues) is { } owner) sourceOwners.Add(source.Key, owner);
        foreach (var source in sources.Where(source => source.Key.Origin is RequirementOrigin.Element or
            RequirementOrigin.Thermal or RequirementOrigin.Radiation or RequirementOrigin.Gap))
        {
            if (!taskAnchors.TryGetValue(source.Key, out var task))
                throw new InvalidDataException("Source has no authoritative task anchor.");
            if (sourceOwners.TryGetValue(task, out var owner)) sourceOwners.Add(source.Key, owner);
            else issues.Add(new(CurrentInputProblem.UnresolvedProofRole, source.Key.Id.Value, current.Scopes.GetValueOrDefault(task, [])));
        }
        foreach (var source in sources.Where(source => source.Key.Origin == RequirementOrigin.Research))
            issues.Add(new(CurrentInputProblem.UnresolvedProofRole, source.Key.Id.Value, []));
        var children = SourceRelations(text, sources).ToDictionary(pair => pair.Key, pair => pair.Value);
        var artifacts = new Dictionary<SourceKey, SourceArtifact[]>();
        var symbols = new Dictionary<SourceKey, SourceSymbol[]>();
        var classifications = sources.ToDictionary(source => source.Key, source => Classification(source.Key));
        var capabilities = CapabilityTaskRequirements.Values.Where(pair => sources.Any(source => source.Key == pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value.Capabilities);
        var modes = new List<ModeContract>();
        foreach (var element in catalogue.Elements)
        {
            var key = element.Source.Key;
            if (!sources.Any(source => source.Key == key && source.Hash == element.Source.Hash))
                throw new InvalidDataException("Catalogue source identity or hash differs from discovered obligations.");
            if (!current.Members.Contains(element.DeliveryOwner)) throw new InvalidDataException("Catalogue delivery owner is undeclared.");
            sourceOwners.Add(key, element.DeliveryOwner);
            modes.AddRange(element.Modes.Select(mode => new ModeContract(key, mode.Dimension, mode.Choice, element.DeliveryOwner)));
            artifacts.Add(key, [element.Declaration]);
            if (!CapabilityCatalogueRequirements.Values.TryGetValue(key.Id, out var required))
                throw new InvalidDataException("Catalogue lacks required semantic capability contract.");
            capabilities.Add(key, required);
            if (catalogue.Admitted.Contains(key.Id))
            {
                var closure = ReadResourceClosure(root, key.Id);
                artifacts[key] = [..artifacts[key], ..closure.Artifacts];
                symbols.Add(key, closure.Symbols);
            }
            else classifications[key] = new(ObligationKind.FutureProduct, ConsumerKind.FutureDeclaration);
        }
        foreach (var pair in taskAnchors)
            if (sources.Any(source => source.Key == pair.Key))
                capabilities.Add(pair.Key, CapabilityTaskRequirements.Values[pair.Value].Capabilities);
        var fixtures = catalogue.Fixtures.ToDictionary(fixture => fixture.Instance);
        foreach (var fixture in RequirementDiscovery.DiscoverFixtures(root))
        {
            if (!fixtures.TryGetValue(fixture.Source.Key.Id, out var declared))
                throw new InvalidDataException("Fixture source lacks a declared proof owner.");
            sourceOwners.Add(fixture.Source.Key, declared.Id);
            capabilities.Add(fixture.Source.Key, capabilities[fixture.Catalogue]);
            children.Add(fixture.Source.Key, [new(SourceRelationKind.CoverageScope, fixture.Catalogue)]);
        }
        // The historical role table is not a grant of current implementation eligibility.
        // Surface every unresolved role; do not manufacture eligibility from a heading or stage order.
        foreach (var pair in CapabilityOwnerMap.Values.OrderBy(pair => pair.Key))
        {
            foreach (var owner in new[] { pair.Value.Design, pair.Value.Proof }.Concat(pair.Value.Implementation).Distinct())
                if (!current.Members.Contains(owner)) issues.Add(new(CurrentInputProblem.UnknownOwner, owner.Value, []));

        }
        var roleInput = CurrentImplementationRoles.Validate(current.Members, CapabilityOwnerMap.Values, CurrentImplementationRoles.Reviewed);
        issues.AddRange(roleInput.Issues);
        var expectations = new CapabilityExpectations(current.Members, sourceOwners, CapabilityOwnerMap.Values, modes,
            children, CapabilityRequirements.Dependencies, artifacts, capabilities, symbols, classifications,
            CapabilityImplementationRequirements.CurrentDeclarations, roleInput.Eligible);
        return new(expectations, issues.DistinctBy(issue => (issue.Problem, issue.Identity)).ToArray());
    }

    public sealed record ResourceClosure(SourceArtifact[] Artifacts, SourceSymbol[] Symbols);
    public static ResourceClosure ReadResourceClosure(DirectoryInfo root, RequirementId key)
    {
        var resource = new SourcePath("parts/catalog/" + key.Value + ".tres");
        var definition = File.ReadAllText(Path.Combine(root.FullName, resource.Value));
        var scene = ResourceLink(definition, ResourceBinding.CatalogueScene);
        var sceneText = File.ReadAllText(Path.Combine(root.FullName, scene.Value));
        var script = ResourceLink(sceneText, ResourceBinding.RootScript);
        var scriptText = File.ReadAllText(Path.Combine(root.FullName, script.Value));
        return new([new(resource, RequirementDiscovery.Hash(definition)), new(scene, RequirementDiscovery.Hash(sceneText))],
            [new(script, new(Path.GetFileNameWithoutExtension(script.Value)), RequirementDiscovery.Hash(scriptText))]);
    }

    private enum ResourceBinding { CatalogueScene, RootScript }
    private static SourcePath ResourceLink(string text, ResourceBinding binding)
    {
        var sections = Regex.Matches(text, @"(?m)^\[([^\r\n]+)\]\r?$").Cast<Match>().ToArray();
        var roots = sections.Where(section => binding == ResourceBinding.CatalogueScene
            ? section.Groups[1].Value == "resource"
            : section.Groups[1].Value.StartsWith("node ", StringComparison.Ordinal) &&
                !Regex.IsMatch(section.Groups[1].Value, @"\bparent\s*=")).ToArray();
        if (roots.Length != 1) throw new InvalidDataException("Missing or duplicate admitted root resource section.");
        var start = roots[0].Index + roots[0].Length;
        var end = sections.FirstOrDefault(section => section.Index >= start)?.Index ?? text.Length;
        var property = binding == ResourceBinding.CatalogueScene ? "Scene" : "script";
        var assignments = text[start..end].Split('\n').Select(line => line.Trim())
            .Where(line => Regex.IsMatch(line, "^" + property + @"\s*=")).ToArray();
        if (assignments.Length != 1) throw new InvalidDataException("Missing or duplicate admitted resource property binding.");
        var reference = Regex.Match(assignments[0], "^" + property + " = ExtResource\\(\"([A-Za-z0-9_]+)\"\\)$");
        if (!reference.Success) throw new InvalidDataException("Unsupported admitted resource property binding.");
        var declarations = new Dictionary<string, (string Type, SourcePath Path)>(StringComparer.Ordinal);
        foreach (var section in sections.Where(section => section.Groups[1].Value.StartsWith("ext_resource ", StringComparison.Ordinal)))
        {
            var declaration = Regex.Match(section.Groups[1].Value,
                "^ext_resource type=\"([^\"]+)\" path=\"res://([^\"]+)\" id=\"([A-Za-z0-9_]+)\"$");
            if (!declaration.Success || !declarations.TryAdd(declaration.Groups[3].Value,
                (declaration.Groups[1].Value, new(declaration.Groups[2].Value))))
                throw new InvalidDataException("Malformed or duplicate external resource identity.");
        }
        if (!declarations.TryGetValue(reference.Groups[1].Value, out var selected))
            throw new InvalidDataException("Admitted resource binding references an unknown external identity.");
        var type = binding == ResourceBinding.CatalogueScene ? "PackedScene" : "Script";
        var extension = binding == ResourceBinding.CatalogueScene ? ".tscn" : ".cs";
        if (selected.Type != type || !selected.Path.Value.EndsWith(extension, StringComparison.Ordinal))
            throw new InvalidDataException("Admitted resource binding has the wrong type.");
        return selected.Path;
    }

    private static readonly IReadOnlySet<RequirementId> WorkflowGaps=new HashSet<RequirementId>
    {
        new("gap-10"),new("gap-11"),new("gap-12"),new("gap-13"),new("gap-14"),new("gap-15"),new("gap-17")
    };
    private static SourceClassification Classification(SourceKey source)
    {
        if(source.Origin==RequirementOrigin.Task)
        {
            if(!CapabilityTaskRequirements.Values.TryGetValue(source,out var row))
                throw new InvalidDataException("Task lacks required semantic classification.");
            return new(row.Kind,row.Kind switch
            {
                ObligationKind.ProductWorkflow=>ConsumerKind.ProductWorkflow,
                ObligationKind.FutureProduct=>ConsumerKind.FutureDeclaration,
                ObligationKind.Engine or ObligationKind.CurrentConsumer=>ConsumerKind.EngineService,
                _=>throw new InvalidDataException("Unsupported task classification.")
            });
        }
        return source.Origin switch
        {
            RequirementOrigin.Catalogue=>new(ObligationKind.CurrentConsumer,ConsumerKind.CurrentPart),
            RequirementOrigin.Fixture=>new(ObligationKind.CurrentConsumer,ConsumerKind.AuthoredFixture),
            RequirementOrigin.Research=>new(ObligationKind.ResearchContainer,ConsumerKind.ProductWorkflow),
            RequirementOrigin.Gap when WorkflowGaps.Contains(source.Id)=>new(ObligationKind.ProductWorkflow,ConsumerKind.ProductWorkflow),
            RequirementOrigin.Element or RequirementOrigin.Thermal or RequirementOrigin.Radiation or RequirementOrigin.Gap=>
                new(ObligationKind.FutureProduct,ConsumerKind.FutureDeclaration),
            _=>throw new InvalidDataException("Unsupported source classification.")
        };
    }

    public static IReadOnlyDictionary<SourceKey,SourceKey> SourceTaskAnchors(string text)
    {
        var result=new Dictionary<SourceKey,SourceKey>();
        var anchors=Regex.Matches(text,"<a id=\"([^\"]+)\"></a>");
        for(var i=0;i<anchors.Count;i++)
        {
            var match=Regex.Match(anchors[i].Groups[1].Value,@"\A(element|thermal|radiation|gap)-[0-9]+\z");
            if(!match.Success)continue;
            var origin=match.Groups[1].Value switch
            {
                "element"=>RequirementOrigin.Element,"thermal"=>RequirementOrigin.Thermal,
                "radiation"=>RequirementOrigin.Radiation,"gap"=>RequirementOrigin.Gap,
                _=>throw new InvalidDataException("Unsupported source anchor.")
            };
            var source=new SourceKey(origin,new(anchors[i].Groups[1].Value));
            SourceKey? task=null;
            for(var next=i+1;next<anchors.Count;next++)
            {
                var id=anchors[next].Groups[1].Value;
                if(Regex.IsMatch(id,@"\Asequence-task-[0-9]+\z"))
                { task=new(RequirementOrigin.Task,new(id));break; }
                if(!Regex.IsMatch(id,@"\Atodo-[0-9]+\z"))break;
            }
            if(task is null||result.ContainsValue(task.Value)||!result.TryAdd(source,task.Value))
                throw new InvalidDataException("Missing or duplicate adjacent source task anchor.");
        }
        return result;
    }
    private static IReadOnlyDictionary<SourceKey,SourceRelation[]> SourceRelations(string text,IReadOnlyList<SourceRequirement> sources)
    {
        var byId=sources.Where(x=>x.Key.Origin is RequirementOrigin.Task or RequirementOrigin.Element or RequirementOrigin.Thermal or RequirementOrigin.Radiation or RequirementOrigin.Gap)
            .ToDictionary(x=>x.Key.Id.Value,x=>x.Key);
        var result=new Dictionary<SourceKey,SourceKey[]>();
        var references=new Dictionary<SourceKey,HashSet<SourceKey>>();
        var anchors=Regex.Matches(text,"<a id=\"([^\"]+)\"></a>");
        for(var i=0;i<anchors.Count;i++)
        {
            if(!byId.TryGetValue(anchors[i].Groups[1].Value,out var key))continue;
            var start=anchors[i].Index+anchors[i].Length;var next=i+1;
            while(next<anchors.Count&&string.IsNullOrWhiteSpace(text[start..anchors[next].Index])&&
                Regex.IsMatch(anchors[next].Groups[1].Value,@"\A(?:sequence-task|todo)-[0-9]+\z"))
            { start=anchors[next].Index+anchors[next].Length;next++; }
            var block=text[start..(next<anchors.Count?anchors[next].Index:text.Length)];
            var named=Regex.Matches(block,@"\]\(#((?:element|thermal|radiation|gap)-[0-9]+)\)")
                .Select(match=>byId[match.Groups[1].Value]).Where(child=>child!=key).Distinct().ToArray();
            result.Add(key,named);
            references.Add(key,named.ToHashSet());
        }
        foreach(var entry in CapabilityTaskRequirements.Values)
            if(byId.ContainsKey(entry.Key.Id.Value))
                result[entry.Key]=result.GetValueOrDefault(entry.Key,[])
                    .Concat(CapabilityTaskRequirements.Children(entry.Value,sources)).Distinct().ToArray();
        foreach(var entry in CapabilityRequirements.NamedChildren)
            if(byId.ContainsKey(entry.Key.Id.Value)||entry.Key.Origin==RequirementOrigin.Research)
                result[entry.Key]=result.GetValueOrDefault(entry.Key,[]).Concat(entry.Value).Distinct().ToArray();
        var complete=sources.Where(source=>source.Key.Origin is RequirementOrigin.Catalogue or RequirementOrigin.Fixture or
            RequirementOrigin.Element or RequirementOrigin.Thermal or RequirementOrigin.Radiation or RequirementOrigin.Gap)
            .Select(source=>source.Key).ToArray();
        foreach(var task in CapabilityRequirements.WholeInventoryTasks)
            if(byId.ContainsKey(task.Id.Value))result[task]=result.GetValueOrDefault(task,[]).Concat(complete).Distinct().ToArray();
        return result.ToDictionary(pair=>pair.Key,pair=>pair.Value.Select(target=>new SourceRelation(
            references.TryGetValue(pair.Key,out var named)&&named.Contains(target)?SourceRelationKind.SourceReference:
                SourceRelationKind.CoverageScope,target)).ToArray());
    }
}
