using System.Text.Json;
using CuriousContraptions.Coverage;

try
{
    if(args.Length is <2 or >3)throw new ArgumentException(
        "Usage: Coverage <inventory|audit-inventory|elements|audit-elements|audit-capabilities> <repository-root> [manifest.json]");
    var operation=args[0] switch
    {
        "inventory"=>CoverageOperation.Inventory,"audit-inventory"=>CoverageOperation.AuditInventory,
        "elements"=>CoverageOperation.Elements,"audit-elements"=>CoverageOperation.AuditElements,"audit-capabilities"=>CoverageOperation.AuditCapabilities,
        _=>throw new ArgumentException("Unsupported coverage operation.")
    };
    var audit=operation is CoverageOperation.AuditInventory or CoverageOperation.AuditElements or CoverageOperation.AuditCapabilities;
    if(args.Length!=(audit?3:2))throw new ArgumentException("Incorrect argument count for coverage operation.");
    var root=new DirectoryInfo(args[1]);
    var sources=RequirementDiscovery.Discover(root);
    switch(operation)
    {
        case CoverageOperation.Inventory:
            Write(CoverageAudit.Seed(sources));return 0;
        case CoverageOperation.Elements:
            Write(ElementAudit.Seed(sources,RequirementDiscovery.DiscoverFixtures(root)));return 0;
        case CoverageOperation.AuditInventory:
            var inventory=Read<CoverageInventory>(args[2]);
            var summary=CoverageAudit.Analyze(sources,inventory);
            Write(summary);return summary.SourceInventoryCurrent?0:2;
        case CoverageOperation.AuditElements:
            var elements=Read<ElementManifest>(args[2]);
            var report=ElementAudit.Analyze(sources,RequirementDiscovery.DiscoverFixtures(root),elements);
            Write(report);return report.LinksCurrent?0:2;
        case CoverageOperation.AuditCapabilities:
            var capabilities=CapabilityInputs.Read(root,args[2]);
            var expectations=CapabilityInputs.Discover(root,sources);
            var cache=new Dictionary<SourcePath,string>();
            string Source(SourcePath path)
            {
                if(!cache.TryGetValue(path,out var content))
                    cache.Add(path,content=File.ReadAllText(Path.Combine(root.FullName,path.Value)));
                return content;
            }
            var capabilityReport=CapabilityAudit.Analyze(sources,capabilities,expectations,Source);
            Write(capabilityReport);return capabilityReport.InventoryCurrent?0:2;
        default:throw new ArgumentOutOfRangeException(nameof(operation));
    }
}
catch(Exception error) when(error is IOException or InvalidDataException or JsonException or ArgumentException or InvalidOperationException)
{
    Console.Error.WriteLine(error.Message);return 1;
}
static T Read<T>(string path)=>JsonSerializer.Deserialize<T>(File.ReadAllText(path),CoverageJson.Options)
    ??throw new InvalidDataException("Null coverage document.");
static void Write<T>(T value)=>Console.WriteLine(JsonSerializer.Serialize(value,CoverageJson.Options));
enum CoverageOperation { Inventory, AuditInventory, Elements, AuditElements, AuditCapabilities }
