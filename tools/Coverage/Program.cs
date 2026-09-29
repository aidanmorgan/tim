using System.Text.Json;
using CuriousContraptions.Coverage;

if(args.Length is <1 or >2)
{
    Console.Error.WriteLine("Usage: Coverage <repository-root> [inventory.json]");
    return 1;
}
try
{
    var sources=RequirementDiscovery.Discover(new DirectoryInfo(args[0]));
    if(args.Length==1)
    {
        Console.WriteLine(JsonSerializer.Serialize(CoverageAudit.Seed(sources),CoverageJson.Options));
        return 0;
    }
    var inventory=JsonSerializer.Deserialize<CoverageInventory>(File.ReadAllText(args[1]),CoverageJson.Options)
        ??throw new InvalidDataException("Null inventory.");
    var report=CoverageAudit.Analyze(sources,inventory);
    Console.WriteLine(JsonSerializer.Serialize(report,CoverageJson.Options));
    return report.SourceInventoryCurrent?0:2;
}
catch(Exception error) when(error is IOException or JsonException or ArgumentException or InvalidOperationException)
{
    Console.Error.WriteLine(error.Message);return 1;
}
