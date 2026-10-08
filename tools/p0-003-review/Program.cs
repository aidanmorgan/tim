using CuriousContraptions.PerformanceTools;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
var timeout=new AttemptContract(AttemptEnd.Timeout,3600,29700,1000,CadenceTier.Sixty);
var early=new AttemptContract(AttemptEnd.Goal,240,1980,100,CadenceTier.Sixty);
AttemptObservation Run(decimal start=31000,decimal duration=30000,int ticks=3600,AttemptEnd end=AttemptEnd.Timeout,CadenceTier tier=CadenceTier.Sixty)
{
 var hz=tier==CadenceTier.Sixty?60:90;
 return new(new(Guid.NewGuid()),AttemptPurpose.Measurement,end,start,start+duration,0,ticks,true,true,true,0,PresentationEvidence.SyntheticRehearsal,
 Enumerable.Range(1,(int)(duration*hz/1000)).Select(i=>new PresentedFrame(new((ulong)i),start+decimal.Floor(i*1000m/hz*1000000)/1000000)).ToArray());
}
var results=new List<ProbeResult>();
void Check(ProbeCase id,bool matches,object actual)=>results.Add(new(id,matches,JsonSerializer.Serialize(actual)));
void Reject(ProbeCase id,Action action){try{action();Check(id,false,"accepted");}catch(Exception e){Check(id,e is InvalidDataException or JsonException,e.GetType().Name);}}
var run=Run();
var normal=PerformanceAudit.AnalyzeAttempt(timeout,run);
Check(ProbeCase.Timeout,normal.Coverage==MetricVerdict.Pass&&normal.DistinctPresentations==1800&&normal.ScheduledSlots==1800&&normal.MissedSlots==0&&normal.FramesPerSecond==60&&normal.SimulatedWallRatio==1&&normal.BrowserPacing==MetricVerdict.Incomplete,normal);
var ninety=PerformanceAudit.AnalyzeAttempt(timeout with{Tier=CadenceTier.Ninety},Run(tier:CadenceTier.Ninety));
Check(ProbeCase.Ninety,ninety.DistinctPresentations==2700&&ninety.ArithmeticPacing==MetricVerdict.Pass&&ninety.MissedSlots==0,ninety);
var slow=PerformanceAudit.AnalyzeAttempt(timeout,Run(duration:60000));
Check(ProbeCase.Slow,slow.Coverage==MetricVerdict.Pass&&slow.SimulationRate==MetricVerdict.Fail&&slow.SimulatedWallRatio==.5m,slow);
var goal=PerformanceAudit.AnalyzeAttempt(early,Run(duration:2000,ticks:240,end:AttemptEnd.Goal));
Check(ProbeCase.Goal,goal.Coverage==MetricVerdict.Pass&&goal.DistinctPresentations==120,goal);
var insufficient=PerformanceAudit.AnalyzeAttempt(early,Run(duration:500,ticks:60,end:AttemptEnd.Goal));
Check(ProbeCase.Insufficient,insufficient.Coverage==MetricVerdict.Incomplete,insufficient);
var dropped=PerformanceAudit.AnalyzeAttempt(timeout,run with{Presentations=run.Presentations.Where(f=>f.Milliseconds<=41000||f.Milliseconds>43000).ToArray()});
Check(ProbeCase.Dropped,dropped.FramesPerSecond==56&&dropped.MissedSlots==120&&dropped.ArithmeticPacing==MetricVerdict.Fail,dropped);
foreach(var (id,count,expected) in new[]{(ProbeCase.OnePercent,18,MetricVerdict.Pass),(ProbeCase.OverOnePercent,19,MetricVerdict.Fail)}){
 var m=PerformanceAudit.AnalyzeAttempt(timeout,run with{Presentations=run.Presentations.Skip(count).ToArray()});
 Check(id,m.ArithmeticPacing==expected&&m.MissedSlots==count,m);
}
var repeated=PerformanceAudit.AnalyzeAttempt(timeout,run with{Presentations=run.Presentations.Select(f=>f with{Id=new(1)}).ToArray()});
Check(ProbeCase.RepeatedIdentity,repeated.DistinctPresentations==1&&repeated.Coverage==MetricVerdict.Incomplete,repeated);
var raf=PerformanceAudit.AnalyzeAttempt(timeout,run with{Evidence=PresentationEvidence.AnimationFrameCallback});
Check(ProbeCase.Callback,raf.BrowserPacing==MetricVerdict.Incomplete,raf);
var mapped=PerformanceAudit.AnalyzeAttempt(timeout,run with{Evidence=PresentationEvidence.CompositorTrace,MappingUncertaintyMilliseconds=.000001m});
Check(ProbeCase.Mapping,mapped.BrowserPacing==MetricVerdict.Incomplete,mapped);
var warm=Run(0) with{Purpose=AttemptPurpose.Warmup};
var fresh=PerformanceAudit.WarmupBeforeFreshRun([warm],run);
Check(ProbeCase.Fresh,fresh==MetricVerdict.Pass,fresh);
var negative=PerformanceAudit.WarmupBeforeFreshRun([warm with{StartMilliseconds=-1}],run);
Check(ProbeCase.NegativeWarmup,negative!=MetricVerdict.Pass,negative);
var malformed=PerformanceAudit.WarmupBeforeFreshRun([warm],run with{StartAcknowledged=false,StopObserved=false,EndMilliseconds=0});
Check(ProbeCase.InvalidMeasured,malformed!=MetricVerdict.Pass,malformed);
var self=PerformanceAudit.WarmupBeforeFreshRun([warm],run with{Id=warm.Id});
Check(ProbeCase.ReusedWarmup,self==MetricVerdict.Fail,self);
var thermal=Enumerable.Range(0,10).SelectMany(i=>new[]{new TimelineInterval(TimelinePhase.Active,i*31000,i*31000+30000,new AttemptId(Guid.NewGuid())),new TimelineInterval(TimelinePhase.Reset,i*31000+30000,(i+1)*31000,null)}).ToArray();
var limits=new ThermalContract(300000,.9m,2000);
var hot=PerformanceAudit.AnalyzeThermal(limits,thermal);
Check(ProbeCase.Thermal,hot.Continuity==MetricVerdict.Pass&&hot.Engagement==MetricVerdict.Pass&&hot.DurationMilliseconds==310000&&hot.ActiveFraction==300000m/310000&&hot.MaximumGapMilliseconds==1000,hot);
var gap=PerformanceAudit.AnalyzeThermal(limits,thermal.Where((_,i)=>i!=1).ToArray());
Check(ProbeCase.MissingSpan,gap.Continuity==MetricVerdict.Incomplete,gap);
var hiddenGap=PerformanceAudit.AnalyzeThermal(limits,[new(TimelinePhase.Active,0,299000,new AttemptId(Guid.NewGuid())),new(TimelinePhase.Hidden,299000,300000,null),new(TimelinePhase.Reset,300001,301000,null)]);
Check(ProbeCase.HiddenPrecedence,hiddenGap.Continuity==MetricVerdict.Fail,hiddenGap);
Reject(ProbeCase.WeakGoal,()=>PerformanceAudit.AnalyzeAttempt(early with{MinimumTicks=1,MinimumWallMilliseconds=1},Run(duration:2000,ticks:1,end:AttemptEnd.Goal)));
Reject(ProbeCase.WeakTimeout,()=>PerformanceAudit.AnalyzeAttempt(timeout with{MinimumWallMilliseconds=1,MinimumPresentations=100},run));
Reject(ProbeCase.Overflow,()=>PerformanceAudit.AnalyzeAttempt(timeout,run with{EndMilliseconds=decimal.MaxValue}));
Reject(ProbeCase.NullPacket,()=>MeasurementEvidence.Write(null!));
Reject(ProbeCase.Undefined,()=>PerformanceAudit.AnalyzeAttempt(timeout,run with{Evidence=(PresentationEvidence)999}));
Reject(ProbeCase.NullFrame,()=>PerformanceAudit.AnalyzeAttempt(timeout,run with{Presentations=[null!]}));
Reject(ProbeCase.Unordered,()=>PerformanceAudit.AnalyzeAttempt(timeout,run with{Presentations=run.Presentations.Reverse().ToArray()}));
Reject(ProbeCase.DuplicateAttempt,()=>MeasurementEvidence.Write(new(timeout,[run,run])));
var json=MeasurementEvidence.Write(new(timeout,[run]));
// Deliberately malformed literal wire values stay at this JSON test boundary.
foreach(var (id,mutate) in new (ProbeCase,Action<JsonObject>)[]{
 (ProbeCase.MissingField,n=>n["attempts"]![0]!.AsObject().Remove("stopObserved")),
 (ProbeCase.NaN,n=>n["attempts"]![0]!["endMilliseconds"]="NaN"),
 (ProbeCase.UnknownField,n=>n["arbitrary"]=true),
 (ProbeCase.NumericEnum,n=>n["contract"]!["tier"]=0),
 (ProbeCase.NullAttempts,n=>n["attempts"]=null)
}){var node=JsonNode.Parse(json)!.AsObject();mutate(node);Reject(id,()=>MeasurementEvidence.Read(node.ToJsonString()));}
Reject(ProbeCase.DuplicateRoot,()=>MeasurementEvidence.Read("{\"contract\":null,"+json[1..]));
Reject(ProbeCase.DuplicateNested,()=>MeasurementEvidence.Read(json.Replace("\"minimumTicks\":3600","\"minimumTicks\":1,\"minimumTicks\":3600")));
var options=new JsonSerializerOptions{WriteIndented=true};
options.Converters.Add(new JsonStringEnumConverter<ProbeCase>(allowIntegerValues:false));
Console.WriteLine(JsonSerializer.Serialize(results,options));
Environment.ExitCode=results.All(r=>r.Matches)?0:1;
enum ProbeCase{Timeout,Ninety,Slow,Goal,Insufficient,Dropped,OnePercent,OverOnePercent,RepeatedIdentity,Callback,Mapping,Fresh,NegativeWarmup,InvalidMeasured,ReusedWarmup,Thermal,MissingSpan,HiddenPrecedence,WeakGoal,WeakTimeout,Overflow,NullPacket,Undefined,NullFrame,Unordered,DuplicateAttempt,MissingField,NaN,UnknownField,NumericEnum,NullAttempts,DuplicateRoot,DuplicateNested}
record ProbeResult(ProbeCase Case,bool Matches,string Actual);
