using System.Text.Json;
using System.Text.Json.Serialization;
using CuriousContraptions;

namespace CuriousContraptions.PerformanceTools;

public enum PerformanceScenario
{
    IdleConstruction, SparseCatalogue, DoubledPopulation, DenseContacts, ConnectedMechanism,
    FastTranslation, PureRotation, MovingHollow, CoupledMechanism, VisualHeavy, Lifecycle
}
public enum TickRecordCoverage { Partial, Complete, Failed }
public enum CounterReduction { Sum, Maximum }
public sealed class ScenarioConverter : ExactPlaytestEnumConverter<PerformanceScenario>;
public sealed class CoverageConverter : ExactPlaytestEnumConverter<TickRecordCoverage>;
public sealed class ReductionConverter : ExactPlaytestEnumConverter<CounterReduction>;
public sealed record Distribution(int Count,double P50,double P95,double P99,double Maximum,double Mean);
public sealed record StageReport(
    [property:JsonConverter(typeof(PlaytestPerformanceStageConverter))] PerformanceStage Stage,
    Distribution Milliseconds,Distribution AllocatedBytes);
public sealed record CounterReport(
    [property:JsonConverter(typeof(PlaytestPerformanceMetricConverter))] PerformanceMetric Metric,
    [property:JsonConverter(typeof(ReductionConverter))] CounterReduction Reduction,long Value);
public sealed record RunReport(
    [property:JsonConverter(typeof(PlaytestPerformanceRunIdConverter))] PerformanceRunId Run,
    [property:JsonConverter(typeof(CoverageConverter))] TickRecordCoverage Coverage,
    int ObservedTicks,int ObservedFrames,int? FirstTick,int? LastTick,StageReport[] Stages,CounterReport[] Counters);
public sealed record PerformanceReport(
    [property:JsonConverter(typeof(ScenarioConverter))] PerformanceScenario DeclaredScenario,
    int ExpectedTicks,long OverwrittenSamples,long OverwrittenCounters,RunReport[] Runs);

/// <summary>Read-only audit of current diagnostic records. Complete means complete tick
/// records only, never verified scene construction, behavior or performance qualification.</summary>
public static partial class PerformanceAudit
{
    public const int DefaultExpectedTicks=3600;
    private enum BrowserRecordKind { Frame }
    private static string WirePrefix(BrowserRecordKind kind)=>kind switch
    {
        BrowserRecordKind.Frame=>"CCFRAME ",
        _=>throw new ArgumentOutOfRangeException(nameof(kind))
    };
    private static readonly JsonSerializerOptions Json=new()
    {
        PropertyNamingPolicy=JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow
    };
    public static IEnumerable<PlaytestPerformanceBatch> ReadBrowserBatches(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        var prefix=WirePrefix(BrowserRecordKind.Frame);
        while(reader.ReadLine() is { } line)
        {
            var start=line.IndexOf(prefix,StringComparison.Ordinal);
            if(start<0)continue;
            // Explicit console transport boundary: parse one JSON value, not the trailing source URL.
            yield return ParseFrame(line[(start+prefix.Length)..]);
        }
    }
    private static PlaytestPerformanceBatch ParseFrame(string payload)
    {
        var utf8=System.Text.Encoding.UTF8.GetBytes(payload);
        var jsonReader=new Utf8JsonReader(utf8);
        using var frame=JsonDocument.ParseValue(ref jsonReader);
        if(!frame.RootElement.TryGetProperty("performance",out var performance)||
            performance.ValueKind!=JsonValueKind.Object)
            throw new InvalidDataException("Frame has no current performance evidence.");
        return performance.Deserialize<PlaytestPerformanceBatch>(Json)
            ??throw new InvalidDataException("Null performance batch.");
    }
    private sealed class DiagnosticRecord(int tick)
    {
        public int Tick { get; }=tick;
        public Dictionary<PerformanceStage,PlaytestPerformanceSample> Samples { get; }=[];
        public Dictionary<PerformanceMetric,PlaytestPerformanceCounter> Counters { get; }=[];
    }
    public static PerformanceReport Analyze(PerformanceScenario scenario,
        IEnumerable<PlaytestPerformanceBatch> batches,int expectedTicks=DefaultExpectedTicks)
    {
        if(!Enum.IsDefined(scenario))throw new ArgumentOutOfRangeException(nameof(scenario));
        if(expectedTicks<=0)throw new ArgumentOutOfRangeException(nameof(expectedTicks));
        ArgumentNullException.ThrowIfNull(batches);
        var runs=new Dictionary<PerformanceRunId,SortedDictionary<long,DiagnosticRecord>>();
        long overwrittenSamples=0,overwrittenCounters=0;
        var owners=new Dictionary<long,(PerformanceRunId Run,int Tick)>();
        DiagnosticRecord Get(PerformanceRunId run,long sequence,int tick,PerformanceRunId current)
        {
            if(run.Value<=0||run.Value>current.Value||sequence<0||tick<0)
                throw new InvalidDataException("Invalid run, sequence or tick identity.");
            if(owners.TryGetValue(sequence,out var owner)&&owner!=(run,tick))
                throw new InvalidDataException("A global sequence has conflicting ownership.");
            owners[sequence]=(run,tick);
            if(!runs.TryGetValue(run,out var records))runs.Add(run,records=[]);
            if(!records.TryGetValue(sequence,out var record))records.Add(sequence,record=new(tick));
            if(record.Tick!=tick)throw new InvalidDataException("One sample sequence names different ticks.");
            return record;
        }
        foreach(var batch in batches)
        {
            if(batch is null||batch.Run.Value<=0||batch.Samples is null||batch.Counters is null||
                batch.OverwrittenSamples<0||batch.OverwrittenCounters<0)
                throw new InvalidDataException("Invalid performance batch.");
            if(!runs.ContainsKey(batch.Run))runs.Add(batch.Run,[]);
            overwrittenSamples=checked(overwrittenSamples+batch.OverwrittenSamples);
            overwrittenCounters=checked(overwrittenCounters+batch.OverwrittenCounters);
            foreach(var sample in batch.Samples)
            {
                if(sample is null||!Enum.IsDefined(sample.Stage)||!Enum.IsDefined(sample.Outcome)||
                    sample.Calls<=0||!double.IsFinite(sample.Milliseconds)||sample.Milliseconds<0||sample.AllocatedBytes<0)
                    throw new InvalidDataException("Invalid performance stage sample.");
                if(!Get(sample.Run,sample.Sequence,sample.Tick,batch.Run).Samples.TryAdd(sample.Stage,sample))
                    throw new InvalidDataException("Duplicate stage sample.");
            }
            foreach(var counter in batch.Counters)
            {
                if(counter is null||!Enum.IsDefined(counter.Metric)||!Enum.IsDefined(counter.Outcome)||counter.Value<0)
                    throw new InvalidDataException("Invalid performance counter.");
                if(!Get(counter.Run,counter.Sequence,counter.Tick,batch.Run).Counters.TryAdd(counter.Metric,counter))
                    throw new InvalidDataException("Duplicate counter sample.");
            }
        }
        if(runs.Count==0)throw new InvalidDataException("No current performance records were found.");
        var reports=new List<RunReport>();
        foreach(var (run,records) in runs.OrderBy(p=>p.Key.Value))
        {
            var frames=records.Values.Where(t=>t.Samples.Keys.Any(PerformanceTopology.IsFrameStage)).ToArray();
            var ticks=records.Values.Where(t=>!t.Samples.Keys.Any(PerformanceTopology.IsFrameStage)).ToArray();
            var sequences=records.Keys.ToArray();
            var failed=false;
            var complete=overwrittenSamples==0&&overwrittenCounters==0&&ticks.Length==expectedTicks;
            foreach(var frame in frames)
            {
                if(frame.Counters.Count!=0||frame.Samples.Keys.Any(s=>!PerformanceTopology.IsFrameStage(s)))
                    throw new InvalidDataException("Frame work cannot contain tick stages or physics counters.");
                var outcomes=frame.Samples.Values.Select(s=>s.Outcome).Distinct().ToArray();
                if(outcomes.Length!=1)throw new InvalidDataException("One frame has conflicting outcomes.");
                failed|=outcomes[0]==PerformanceOutcome.Failed;
                foreach(var stage in Enum.GetValues<PerformanceStage>().Where(PerformanceTopology.IsFrameStage))
                {
                    var calls=stage==PerformanceStage.SceneSubmission?4:1;
                    if(frame.Samples.TryGetValue(stage,out var sample))
                    {
                        if(sample.Calls>calls)throw new InvalidDataException("Frame stage has too many calls.");
                        complete&=sample.Calls==calls;
                    }
                    else complete=false;
                }
            }
            for(var index=1;index<sequences.Length;index++)complete&=sequences[index]-sequences[index-1]==1;
            for(var index=0;index<ticks.Length;index++)
            {
                var tick=ticks[index];
                var outcomes=tick.Samples.Values.Select(s=>s.Outcome)
                    .Concat(tick.Counters.Values.Select(c=>c.Outcome)).Distinct().ToArray();
                if(outcomes.Length!=1)throw new InvalidDataException("One tick has conflicting outcomes.");
                failed|=outcomes[0]==PerformanceOutcome.Failed;
                complete&=tick.Tick==index&&tick.Samples.Count==Enum.GetValues<PerformanceStage>().Count(s=>!PerformanceTopology.IsFrameStage(s))&&
                    tick.Counters.Count==Enum.GetValues<PerformanceMetric>().Length;
                foreach(var stage in Enum.GetValues<PerformanceStage>().Where(s=>!PerformanceTopology.IsFrameStage(s)))
                {
                    var expectedCalls=stage switch
                    {
                        PerformanceStage.GameplayTick or PerformanceStage.Networks=>1,
                        PerformanceStage.Physics=>4,
                        PerformanceStage.Publication=>2,
                        _=>throw new ArgumentOutOfRangeException(nameof(stage))
                    };
                    complete&=tick.Samples.TryGetValue(stage,out var sample)&&sample.Calls==expectedCalls;
                }
                complete&=tick.Counters.TryGetValue(PerformanceMetric.PhysicsSteps,out var steps)&&steps.Value==4;
            }
            var samples=records.Values.SelectMany(t=>t.Samples.Values).ToArray();
            var counters=ticks.SelectMany(t=>t.Counters.Values).ToArray();
            var stages=samples.GroupBy(s=>s.Stage).OrderBy(g=>g.Key)
                .Select(g=>new StageReport(g.Key,Describe(g.Select(s=>s.Milliseconds)),
                    Describe(g.Select(s=>(double)s.AllocatedBytes)))).ToArray();
            var totals=counters.GroupBy(c=>c.Metric).OrderBy(g=>g.Key).Select(g=>
            {
                var reduction=Reduction(g.Key);
                return new CounterReport(g.Key,reduction,reduction==CounterReduction.Maximum
                    ?g.Max(c=>c.Value):g.Aggregate(0L,(total,c)=>checked(total+c.Value)));
            }).ToArray();
            reports.Add(new(run,failed?TickRecordCoverage.Failed:complete?TickRecordCoverage.Complete:
                TickRecordCoverage.Partial,ticks.Length,frames.Length,ticks.Length==0?null:ticks[0].Tick,
                ticks.Length==0?null:ticks[^1].Tick,stages,totals));
        }
        return new(scenario,expectedTicks,overwrittenSamples,overwrittenCounters,reports.ToArray());
    }
    private static CounterReduction Reduction(PerformanceMetric metric)=>metric switch
    {
        PerformanceMetric.MaximumPredictionCoordinates or PerformanceMetric.MaximumImpulseCorrectionCoordinates=>CounterReduction.Maximum,
        PerformanceMetric.PhysicsSteps or PerformanceMetric.PhysicsSubsteps or PerformanceMetric.PhysicsEvents or
        PerformanceMetric.SweepIterations or PerformanceMetric.VelocityIterations or PerformanceMetric.PositionIterations or
        PerformanceMetric.PredictionCalls or PerformanceMetric.PredictionMidpoints or PerformanceMetric.PredictionNewtonIterations or
        PerformanceMetric.BodyQueries or PerformanceMetric.BodyNodeTests or PerformanceMetric.BodyLeafTests or PerformanceMetric.BodyCandidatePairs or
        PerformanceMetric.CompoundQueries or PerformanceMetric.CompoundNodeTests or PerformanceMetric.CompoundLeafTests or PerformanceMetric.CompoundCandidatePairs or
        PerformanceMetric.PredictionConstraintSolves or PerformanceMetric.PredictionConstraintIterations or PerformanceMetric.PredictionCouplingTests or PerformanceMetric.PredictionCoupledPairs
        or PerformanceMetric.ImpulseFactorizations or PerformanceMetric.ImpulseCorrectionTrials
            =>CounterReduction.Sum,
        _=>throw new ArgumentOutOfRangeException(nameof(metric))
    };
    public static Distribution Describe(IEnumerable<double> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var sorted=values.Order().ToArray();
        if(sorted.Length==0||sorted.Any(v=>!double.IsFinite(v)||v<0))
            throw new ArgumentException("Distribution requires finite nonnegative observations.",nameof(values));
        double Quantile(double fraction)=>sorted[(int)Math.Ceiling(sorted.Length*fraction)-1];
        // Divide before summing to avoid overflowing a finite mean.
        return new(sorted.Length,Quantile(.5),Quantile(.95),Quantile(.99),sorted[^1],
            sorted.Sum(v=>v/sorted.Length));
    }
}
