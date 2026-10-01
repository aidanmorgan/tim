using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using System.Text.Json;
if(args.Length!=1)throw new ArgumentException("Provide one trace path.");
const string TraceIndexExtension=".etlx";
using var log=new TraceLog(TraceLog.CreateFromEventPipeDataFile(args[0],Path.ChangeExtension(args[0],TraceIndexExtension)));
if(log.EventsLost!=0)throw new InvalidOperationException("Trace lost events.");
var totals=new Dictionary<AllocationSite,(long Bytes,long Samples)>();
foreach(var entry in log.Events)
{
    if(entry is not GCAllocationTickTraceData allocation)continue;
    var stack=new List<string>();
    for(var frame=entry.CallStack();frame is not null;frame=frame.Caller)
        stack.Add(frame.CodeAddress.FullMethodName);
    var site=new AllocationSite(new(allocation.TypeName),new(string.Join(Environment.NewLine,stack)));
    totals.TryGetValue(site,out var prior);
    totals[site]=(checked(prior.Bytes+(long)allocation.AllocationAmount64),checked(prior.Samples+1));
}
Console.WriteLine(JsonSerializer.Serialize(totals.OrderByDescending(p=>p.Value.Bytes).Take(30)
    .Select(p=>new {Type=p.Key.Type.Value,Stack=p.Key.Stack.Value,p.Value.Bytes,p.Value.Samples}),
    new JsonSerializerOptions {WriteIndented=true}));
readonly record struct TypeIdentity(string Value);
readonly record struct StackIdentity(string Value);
readonly record struct AllocationSite(TypeIdentity Type,StackIdentity Stack);
