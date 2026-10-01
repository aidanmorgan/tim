using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CuriousContraptions.Coverage;

/// <summary>Validated external Markdown/resource boundaries for current register and mode expectations.</summary>
public static class CapabilityInputs
{
    private const string TodoPath="TODO.md";
    private const string ScenePattern="path=\"res://([^\" ]+\\.tscn)\"";
    private const string ScriptPattern="path=\"res://([^\" ]+\\.cs)\"";
    private const string OperationProperty="Operation";
    private const string ColourProperty="Colour";
    private const string AngleProperty="BendAngle";
    private enum ModeProfile { Fixed, Logic, Filter, Receiver, Tone, Chime, Bend }
    private static readonly IReadOnlyDictionary<SourcePath,ModeProfile> ModeProfiles=
        new Dictionary<SourcePath,ModeProfile>
        {
            [new("parts/ElectricalLogicPart.cs")]=ModeProfile.Logic,
            [new("parts/OpticalLogicPart.cs")]=ModeProfile.Logic,
            [new("parts/ColourFilterPart.cs")]=ModeProfile.Filter,
            [new("parts/LightReceiverPart.cs")]=ModeProfile.Receiver,
            [new("parts/BellPart.cs")]=ModeProfile.Tone,
            [new("parts/SpeakerPart.cs")]=ModeProfile.Tone,
            [new("parts/WindChimesPart.cs")]=ModeProfile.Chime,
            [new("parts/PipeBendPart.cs")]=ModeProfile.Bend
        };

    private sealed record ModeEnumBoundary(SourcePath Path,SymbolId Declaration,IReadOnlyDictionary<SymbolId,int> Values);
    private static readonly ModeEnumBoundary LogicBoundary=new(new("engine/LogicGate.cs"),new("LogicGateKind"),
        new Dictionary<SymbolId,int>{[new("And")]=0,[new("Or")]=1,[new("Xor")]=2,[new("Nor")]=3,[new("Nand")]=4});
    private static readonly ModeEnumBoundary OpticalBoundary=new(new("engine/OpticalColour.cs"),new("OpticalColour"),
        new Dictionary<SymbolId,int>{[new("Broadband")]=0,[new("Red")]=1,[new("Green")]=2,[new("Blue")]=3,
            [new("Yellow")]=4,[new("Cyan")]=5,[new("Magenta")]=6,[new("White")]=7});
    private static readonly ModeEnumBoundary ToneBoundary=new(new("engine/Acoustics.cs"),new("ToneBand"),
        new Dictionary<SymbolId,int>{[new("Low")]=0,[new("Mid")]=1,[new("High")]=2});
    private static readonly ModeEnumBoundary BendBoundary=new(new("engine/MachineData.cs"),new("TubeBendAngle"),
        new Dictionary<SymbolId,int>{[new("Degrees45")]=45,[new("Degrees90")]=90});
    private static ModeEnumBoundary? ModeBoundary(ModeProfile profile)=>profile switch
    {
        ModeProfile.Logic=>LogicBoundary,ModeProfile.Filter or ModeProfile.Receiver=>OpticalBoundary,
        ModeProfile.Tone or ModeProfile.Chime=>ToneBoundary,ModeProfile.Bend=>BendBoundary,
        ModeProfile.Fixed=>null,_=>throw new InvalidDataException("Unsupported mode profile.")
    };
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
    private enum WorkStage { Baseline,Design,Native,Integration,Worker,Optimize,Qualify,Release,Audit,Cleanup,Element,ElementProof }
    private sealed record RegisterRow(int Order,WorkOrderId Id,string[] Sources,WorkStage Stage);
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
    public static CapabilityExpectations Discover(DirectoryInfo root,IReadOnlyList<SourceRequirement> sources)
    {
        var text=File.ReadAllText(Path.Combine(root.FullName,TodoPath));
        var rows=new List<RegisterRow>();
        foreach(var line in text.Split('\n'))
        {
            var match=Regex.Match(line,@"^\| ([0-9]+) \|.*?\*\*([^ ]+) — ");
            if(!match.Success)continue;
            var cell=line.Split(" | ",StringSplitOptions.None)[1];
            var stageMatch=Regex.Match(line,@"\[([^\]]+)\]\(#stage-gates\)");
            if(!stageMatch.Success||!Enum.TryParse<WorkStage>(stageMatch.Groups[1].Value,false,out var stage)||!Enum.IsDefined(stage)||Enum.GetName(stage)!=stageMatch.Groups[1].Value)
                throw new InvalidDataException("Unknown work-order stage.");
            rows.Add(new(int.Parse(match.Groups[1].Value,CultureInfo.InvariantCulture),new(match.Groups[2].Value),
                Regex.Matches(cell,@"\]\(([^)]+)\)").Select(x=>x.Groups[1].Value).ToArray(),stage));
        }
        if(rows.Count==0||rows.Select(x=>x.Id).Distinct().Count()!=rows.Count)throw new InvalidDataException("Invalid work-order register.");
        var owners=rows.Select(x=>x.Id).ToHashSet();
        var sourceOwners=new Dictionary<SourceKey,WorkOrderId>();
        foreach(var source in sources.Where(x=>x.Key.Origin==RequirementOrigin.Task))
        {
            var candidates=rows.Where(row=>Regex.IsMatch(row.Id.Value,@"\AS[0-9]{3}\z")&&
                row.Sources.Contains("#"+source.Key.Id.Value)).OrderBy(row=>row.Sources.Length).ThenByDescending(row=>row.Order).ToArray();
            if(candidates.Length==0)throw new InvalidDataException("Task has no delivery owner.");
            sourceOwners.Add(source.Key,candidates[0].Id);
        }
        var taskAnchors=SourceTaskAnchors(text);
        foreach(var source in sources.Where(x=>x.Key.Origin!=RequirementOrigin.Task))
        {
            WorkOrderId owner;
            switch(source.Key.Origin)
            {
                case RequirementOrigin.Catalogue:
                    var design=rows.Single(row=>Regex.IsMatch(row.Id.Value,@"\ACAT-[0-9]{3}-D\z")&&row.Sources.Contains(source.Location));
                    owner=new(design.Id.Value[..^1]+"V");break;
                case RequirementOrigin.Fixture:
                    owner=rows.Single(row=>Regex.IsMatch(row.Id.Value,@"\AFIX-[0-9]+-[0-9]+\z")&&row.Sources.Contains(source.Location)).Id;break;
                case RequirementOrigin.Research:owner=new("P0-002");break;
                case RequirementOrigin.Element:
                case RequirementOrigin.Thermal:
                case RequirementOrigin.Radiation:
                case RequirementOrigin.Gap:
                    if(!taskAnchors.TryGetValue(source.Key,out var task))
                        throw new InvalidDataException("Source has no authoritative task anchor.");
                    owner=sourceOwners[task];break;
                default:throw new InvalidDataException("Unsupported source origin.");
            }
            if(!owners.Contains(owner))throw new InvalidDataException("Source owner absent from current register.");
            sourceOwners.Add(source.Key,owner);
        }
        var children=SourceRelations(text,sources).ToDictionary(pair=>pair.Key,pair=>pair.Value);
        var requiredArtifacts=new Dictionary<SourceKey,SourceArtifact[]>();
        var requiredSymbols=new Dictionary<SourceKey,SourceSymbol[]>();
        var requiredCapabilities=CapabilityTaskRequirements.Values
            .Where(pair=>sources.Any(source=>source.Key==pair.Key))
            .ToDictionary(pair=>pair.Key,pair=>pair.Value.Capabilities);
        foreach(var source in sources.Where(source=>source.Key.Origin==RequirementOrigin.Catalogue))
        {
            var definition=File.ReadAllText(Path.Combine(root.FullName,source.Location));
            var sceneMatch=Regex.Match(definition,ScenePattern);
            if(!sceneMatch.Success)throw new InvalidDataException("Catalogue has no scene resource.");
            var path=new SourcePath(sceneMatch.Groups[1].Value);
            var scene=File.ReadAllText(Path.Combine(root.FullName,path.Value));
            requiredArtifacts.Add(source.Key,[new(path,RequirementDiscovery.Hash(scene))]);
            var scriptMatch=Regex.Match(scene,ScriptPattern);
            if(!scriptMatch.Success)throw new InvalidDataException("Scene has no script resource.");
            var script=new SourcePath(scriptMatch.Groups[1].Value);
            
            if(ModeProfiles.TryGetValue(script,out var modeProfile)&&ModeBoundary(modeProfile) is { } boundary)
            {
                var enumText=File.ReadAllText(Path.Combine(root.FullName,boundary.Path.Value));
                ValidateEnumDeclaration(enumText,boundary.Declaration,boundary.Values);
                requiredArtifacts[source.Key]=[..requiredArtifacts[source.Key],new(boundary.Path,RequirementDiscovery.Hash(enumText))];
            }
            requiredSymbols.Add(source.Key,[new(script,new SymbolId(Path.GetFileNameWithoutExtension(script.Value)),
                RequirementDiscovery.Hash(File.ReadAllText(Path.Combine(root.FullName,script.Value))))]);
            if(!CapabilityCatalogueRequirements.Values.TryGetValue(source.Key.Id,out var catalogueRequirements))
                throw new InvalidDataException("Catalogue lacks required semantic capability contract.");
            requiredCapabilities.Add(source.Key,catalogueRequirements);
            if(script==new SourcePath("parts/BallPart.cs")&&!catalogueRequirements.Contains(EngineCapability.Buoyancy))
                throw new InvalidDataException("BallPart contract must retain supported buoyancy.");
        }
        foreach(var pair in taskAnchors)
            if(sources.Any(source=>source.Key==pair.Key))
                requiredCapabilities.Add(pair.Key,CapabilityTaskRequirements.Values[pair.Value].Capabilities);
        if(sources.Any(source=>source.Key.Origin==RequirementOrigin.Fixture))
            foreach(var fixture in RequirementDiscovery.DiscoverFixtures(root))
            {
                requiredCapabilities.Add(fixture.Source.Key,requiredCapabilities[fixture.Catalogue]);
                children.Add(fixture.Source.Key,[new(SourceRelationKind.CoverageScope,fixture.Catalogue)]);
            }
        return new(owners,sourceOwners,CapabilityOwnerMap.Values,DiscoverModes(root,sources,sourceOwners),children,
            CapabilityRequirements.Dependencies,requiredArtifacts,requiredCapabilities,requiredSymbols,
            sources.ToDictionary(source=>source.Key,source=>Classification(source.Key)),
            CapabilityImplementationRequirements.CurrentDeclarations,
            rows.Where(row=>row.Stage is WorkStage.Native or WorkStage.Integration or WorkStage.Worker or WorkStage.Optimize)
                .Select(row=>row.Id).ToHashSet());
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
    private static ModeContract[] DiscoverModes(DirectoryInfo root,IReadOnlyList<SourceRequirement> sources,
        IReadOnlyDictionary<SourceKey,WorkOrderId> owners)
    {
        var modes=new List<ModeContract>();
        foreach(var source in sources.Where(x=>x.Key.Origin==RequirementOrigin.Catalogue))
        {
            var definition=File.ReadAllText(Path.Combine(root.FullName,source.Location));
            var sceneMatch=Regex.Match(definition,ScenePattern);
            if(!sceneMatch.Success)throw new InvalidDataException("Catalogue has no scene resource.");
            var scenePath=new SourcePath(sceneMatch.Groups[1].Value);
            var scene=File.ReadAllText(Path.Combine(root.FullName,scenePath.Value));
            var scriptMatch=Regex.Match(scene,ScriptPattern);
            if(!scriptMatch.Success)throw new InvalidDataException("Scene has no script resource.");
            var script=new SourcePath(scriptMatch.Groups[1].Value);
            var scriptText=File.ReadAllText(Path.Combine(root.FullName,script.Value));
            Add(ModeDimension.Configuration,ModeChoice.Fixed);
            if(!ModeProfiles.TryGetValue(script,out var profile))continue;
            switch(profile)
            {
                case ModeProfile.Logic:
                    Add(ModeDimension.Logic,Property(OperationProperty,0) switch
                    {0=>ModeChoice.And,1=>ModeChoice.Or,2=>ModeChoice.Xor,3=>ModeChoice.Nor,4=>ModeChoice.Nand,
                        _=>throw new InvalidDataException("Unsupported logic operation.")});break;
                case ModeProfile.Filter:
                case ModeProfile.Receiver:
                    var colour=Property(ColourProperty,profile==ModeProfile.Filter?1:0);
                    if(profile==ModeProfile.Filter && colour is not (1 or 2 or 3))
                        throw new InvalidDataException("Unsupported filter channel.");
                    Add(ModeDimension.OpticalChannel,colour switch
                    {0=>ModeChoice.Broadband,1=>ModeChoice.Red,2=>ModeChoice.Green,3=>ModeChoice.Blue,4=>ModeChoice.Yellow,
                        5=>ModeChoice.Cyan,6=>ModeChoice.Magenta,7=>ModeChoice.White,
                        _=>throw new InvalidDataException("Unsupported optical channel.")});break;
                case ModeProfile.Tone:
                case ModeProfile.Chime:
                    Add(ModeDimension.AcousticTone,ModeChoice.Low);Add(ModeDimension.AcousticTone,ModeChoice.Mid);
                    Add(ModeDimension.AcousticTone,ModeChoice.High);break;
                case ModeProfile.Bend:
                    Add(ModeDimension.TubeAngle,Property(AngleProperty,90) switch
                    {45=>ModeChoice.Degrees45,90=>ModeChoice.Degrees90,_=>throw new InvalidDataException("Unsupported tube angle.")});break;
                case ModeProfile.Fixed:break;
                default:throw new InvalidDataException("Unsupported mode profile.");
            }
            void Add(ModeDimension dimension,ModeChoice choice)=>modes.Add(new(source.Key,dimension,choice,owners[source.Key]));
            int Property(string name,int declaredDefault)
            {
                var values=Regex.Matches(scene,"^"+Regex.Escape(name)+@" = (-?[0-9]+)$",RegexOptions.Multiline);
                var assignments=Regex.Matches(scene,"^"+Regex.Escape(name)+@"\s*=",RegexOptions.Multiline);
                if(values.Count!=assignments.Count||values.Count>1)
                    throw new InvalidDataException("Malformed or duplicate scene mode property.");
                if(values.Count!=0)return int.Parse(values[0].Groups[1].Value,CultureInfo.InvariantCulture);
                var declaration=profile switch
                {
                    ModeProfile.Logic=>@"public\s+LogicGateKind\s+Operation\s*\{\s*get;\s*set;\s*\}(?!\s*=)",
                    ModeProfile.Filter=>@"public\s+OpticalColour\s+Colour\s*\{\s*get;\s*set;\s*\}\s*=\s*OpticalColour.Red;",
                    ModeProfile.Receiver=>@"public\s+OpticalColour\s+Colour\s*\{\s*get;\s*set;\s*\}\s*=\s*OpticalColour.Broadband;",
                    ModeProfile.Bend=>@"public\s+TubeBendAngle\s+BendAngle\s*\{\s*get;\s*set;\s*\}\s*=\s*TubeBendAngle.Degrees90;",
                    _=>throw new InvalidDataException("No declared default for mode profile.")
                };
                if(!Regex.IsMatch(scriptText,declaration))
                    throw new InvalidDataException("Omitted scene property no longer matches verified script default.");
                return declaredDefault;
            }
        }
        return modes.ToArray();
    }
}
