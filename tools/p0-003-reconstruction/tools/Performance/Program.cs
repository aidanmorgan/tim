using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using CuriousContraptions.PerformanceTools;

// Explicit CLI boundary: scenario and input path only. This tool cannot command a game.
if(args.Length!=2)throw new ArgumentException("Supply a canonical scenario name and current browser log (.log or .log.gz).");
var options=new JsonSerializerOptions
{
    PropertyNamingPolicy=JsonNamingPolicy.CamelCase,WriteIndented=true,
    UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow
};
options.Converters.Add(new ScenarioConverter());
var scenario=JsonSerializer.Deserialize<PerformanceScenario>(JsonSerializer.Serialize(args[0]),options);
var input=new FileInfo(args[1]);
using var file=input.OpenRead();
var encoding=input.Extension switch
{
    ".gz"=>EvidenceEncoding.Gzip,
    ".log"=>EvidenceEncoding.PlainText,
    _=>throw new ArgumentException("Unsupported evidence file extension.")
};
using Stream decoded=encoding switch
{
    EvidenceEncoding.Gzip=>new GZipStream(file,CompressionMode.Decompress),
    EvidenceEncoding.PlainText=>file,
    _=>throw new ArgumentOutOfRangeException(nameof(encoding))
};
using var reader=new StreamReader(decoded);
var report=PerformanceAudit.Analyze(scenario,PerformanceAudit.ReadBrowserBatches(reader));
Console.WriteLine(JsonSerializer.Serialize(report,options));

internal enum EvidenceEncoding { PlainText, Gzip }
