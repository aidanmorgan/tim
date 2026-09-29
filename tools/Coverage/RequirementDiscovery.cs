using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CuriousContraptions.Coverage;

/// <summary>Explicit source boundaries. Discovery preserves separate obligations; it
/// never infers canonical element equivalence or proof from a title or file's existence.</summary>
public static class RequirementDiscovery
{
    private const string TodoPath="TODO.md";
    private const string CataloguePath="parts/catalog";
    private const string CampaignPath="content/puzzles.json";
    private const string DocumentationPath="docs";
    private const string ResearchPattern="*research.md";
    public static ContentHash Hash(string content)=>
        new(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(content))));
    public static IReadOnlyList<SourceRequirement> Discover(DirectoryInfo root)
    {
        var records=new List<SourceRequirement>();
        var todo=File.ReadAllText(Path.Combine(root.FullName,TodoPath));
        records.AddRange(Todo(todo));
        foreach(var path in Directory.EnumerateFiles(Path.Combine(root.FullName,CataloguePath),"*.tres").Order(StringComparer.Ordinal))
        {
            var text=File.ReadAllText(path);
            var ids=Regex.Matches(text,"^Id = \"([^\"]+)\"$",RegexOptions.Multiline);
            if(ids.Count!=1)throw new InvalidDataException("Catalogue definition must have exactly one Id.");
            var title=Regex.Match(text,"^Title = \"([^\"]+)\"$",RegexOptions.Multiline);
            if(!title.Success)throw new InvalidDataException("Catalogue definition has no title.");
            records.Add(new(new(RequirementOrigin.Catalogue,new(ids[0].Groups[1].Value)),
                Path.GetRelativePath(root.FullName,path),title.Groups[1].Value,Hash(text)));
        }
        records.AddRange(DiscoverFixtures(root).Select(fixture=>fixture.Source));
        foreach(var fullPath in Directory.EnumerateFiles(Path.Combine(root.FullName,DocumentationPath),ResearchPattern).Order(StringComparer.Ordinal))
        {
            var path=Path.GetRelativePath(root.FullName,fullPath);
            var text=File.ReadAllText(fullPath);
            // The entire contract remains an unresolved obligation until its named
            // candidates, modes and cross-references are reconciled individually.
            records.Add(new(new(RequirementOrigin.Research,new(path)),path,
                text.Split('\n')[0].TrimStart('#',' '),Hash(text)));
        }
        RequireUnique(records);
        return records;
    }
    public static IReadOnlyList<SourceRequirement> Todo(string text)
    {
        var result=new List<SourceRequirement>();
        var anchors=Regex.Matches(text,"<a id=\"([^\"]+)\"></a>");
        for(var i=0;i<anchors.Count;i++)
        {
            var anchor=anchors[i].Groups[1].Value;
            var match=Regex.Match(anchor,@"\A(element|thermal|radiation|gap)-([0-9]+)\z");
            if(!match.Success)continue;
            var origin=match.Groups[1].Value switch
            {
                "element"=>RequirementOrigin.Element,"thermal"=>RequirementOrigin.Thermal,
                "radiation"=>RequirementOrigin.Radiation,"gap"=>RequirementOrigin.Gap,
                _=>throw new InvalidDataException("Unsupported source anchor.")
            };
            var start=anchors[i].Index+anchors[i].Length;
            var end=i+1<anchors.Count?anchors[i+1].Index:text.Length;
            var block=text[start..end].Trim();
            if(block.Length==0)throw new InvalidDataException("Empty requirement block.");
            var title=block.Split('\n').First(line=>!string.IsNullOrWhiteSpace(line));
            result.Add(new(new(origin,new(anchor)),TodoPath+"#"+anchor,title,Hash(block)));
        }
        RequireUnique(result);return result;
    }
    public static IReadOnlyList<FixtureRequirement> DiscoverFixtures(DirectoryInfo root)=>
        Fixtures(File.ReadAllText(Path.Combine(root.FullName,CampaignPath)));
    public static IReadOnlyList<FixtureRequirement> Fixtures(string json)
    {
        using var document=JsonDocument.Parse(json);
        var result=new List<FixtureRequirement>();
        foreach(var level in document.RootElement.EnumerateArray())
        {
            var levelId=new RequirementId(level.GetProperty("id").GetString()!);
            foreach(var part in level.GetProperty("parts").EnumerateArray())
            {
                if(!part.GetProperty("locked").GetBoolean())continue;
                var id=new RequirementId(part.GetProperty("id").GetString()!);
                var kind=new RequirementId(part.GetProperty("kind").GetString()!);
                var key=new RequirementId(levelId.Value+"/"+id.Value);
                result.Add(new(new(new(RequirementOrigin.Fixture,key),CampaignPath+"#"+key.Value,
                    kind.Value+" in "+levelId.Value,Hash(part.GetRawText())),new(RequirementOrigin.Catalogue,kind)));
            }
        }
        RequireUnique(result.Select(fixture=>fixture.Source));return result;
    }
    public static void RequireUnique(IEnumerable<SourceRequirement> records)
    {
        var seen=new HashSet<SourceKey>();
        foreach(var record in records)
        {
            if(!Enum.IsDefined(record.Key.Origin))throw new InvalidDataException("Unsupported requirement origin.");
            _=new RequirementId(record.Key.Id.Value);_=new ContentHash(record.Hash.Value);
            if(!seen.Add(record.Key))throw new InvalidDataException("Duplicate source identity.");
        }
    }
}
