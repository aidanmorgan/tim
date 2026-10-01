using System.Text.Json;
using CuriousContraptions.PerformanceTools;

namespace CuriousContraptions.Tests;

public class PerformanceAuditTests
{
    private static PlaytestPerformanceBatch Batch(PerformanceRunId run,int tick,long sequence,
        PerformanceOutcome outcome=PerformanceOutcome.Completed)=>new(run,
        Enum.GetValues<PerformanceStage>().Where(stage=>!PerformanceTopology.IsFrameStage(stage)).Select(stage=>new PlaytestPerformanceSample(run,sequence,tick,stage,
            outcome,stage==PerformanceStage.Physics?4:stage==PerformanceStage.Publication?2:1,sequence+1,100)).ToArray(),0,
        Enum.GetValues<PerformanceMetric>().Select(metric=>new PlaytestPerformanceCounter(run,sequence,tick,metric,
            outcome,metric==PerformanceMetric.PhysicsSteps?4:metric==PerformanceMetric.MaximumPredictionCoordinates?12*(sequence+1):1)).ToArray(),0);
    private static PerformanceReport Analyze(params PlaytestPerformanceBatch[] batches)=>
        PerformanceAudit.Analyze(PerformanceScenario.SparseCatalogue,batches,2);

    [Fact]
    public void RetainedPriorRunSamplesAreGroupedByOriginAndPeakCountersAreNotSummed()
    {
        var first=new PerformanceRunId(1);var second=new PerformanceRunId(2);
        var pending=Batch(first,0,0) with {Run=second};
        var report=Analyze(pending,Batch(second,0,1),Batch(second,1,2));
        Assert.Equal(TickRecordCoverage.Partial,report.Runs[0].Coverage);
        var complete=report.Runs[1];
        Assert.Equal(second,complete.Run);Assert.Equal(TickRecordCoverage.Complete,complete.Coverage);
        Assert.Equal(2,complete.ObservedTicks);
        Assert.Equal(8,Assert.Single(complete.Counters,c=>c.Metric==PerformanceMetric.PhysicsSteps).Value);
        var peak=Assert.Single(complete.Counters,c=>c.Metric==PerformanceMetric.MaximumPredictionCoordinates);
        Assert.Equal(CounterReduction.Maximum,peak.Reduction);Assert.Equal(36,peak.Value);
        var stage=Assert.Single(complete.Stages,s=>s.Stage==PerformanceStage.GameplayTick);
        Assert.Equal(2,stage.Milliseconds.Count);Assert.Equal(3,stage.Milliseconds.P95);
    }

    [Fact]
    public void MissingStagesCountersTicksSequencesAndOverwritesRemainPartial()
    {
        var a=Batch(new(1),0,0);var b=Batch(new(1),1,1);
        Assert.Equal(TickRecordCoverage.Complete,Assert.Single(Analyze(a,b).Runs).Coverage);
        var variants=new[]
        {
            a with {Samples=a.Samples.Where(s=>s.Stage!=PerformanceStage.Networks).ToArray()},
            a with {Counters=a.Counters.Where(c=>c.Metric!=PerformanceMetric.PhysicsEvents).ToArray()},
            a with {OverwrittenSamples=1},
            a with {OverwrittenCounters=1}
        };
        foreach(var changed in variants)
            Assert.Equal(TickRecordCoverage.Partial,Assert.Single(Analyze(changed,b).Runs).Coverage);
        Assert.Equal(TickRecordCoverage.Partial,Assert.Single(Analyze(b).Runs).Coverage);
        Assert.Equal(TickRecordCoverage.Partial,Assert.Single(Analyze(a,Batch(new(1),1,2)).Runs).Coverage);
        var wrongCalls=a with {Samples=a.Samples.Select(s=>s with {Calls=1}).ToArray()};
        Assert.Equal(TickRecordCoverage.Partial,Assert.Single(Analyze(wrongCalls,b).Runs).Coverage);
    }

    [Fact]
    public void FailedTicksCannotBecomeCompleteAndConflictingOrDuplicateDataRejects()
    {
        var a=Batch(new(1),0,0);var b=Batch(new(1),1,1,PerformanceOutcome.Failed);
        Assert.Equal(TickRecordCoverage.Failed,Assert.Single(Analyze(a,b).Runs).Coverage);
        Assert.Throws<InvalidDataException>(()=>Analyze(a,a));
        Assert.Throws<InvalidDataException>(()=>Analyze(a,Batch(new(2),0,0)));
        Assert.Throws<InvalidDataException>(()=>Analyze(a with {Run=new(2),
            Counters=a.Counters.Select(c=>c with {Outcome=PerformanceOutcome.Failed}).ToArray()}));
        Assert.Throws<InvalidDataException>(()=>Analyze(a with {Run=new(1),
            Samples=a.Samples.Select(s=>s with {Run=new(2)}).ToArray()}));
        Assert.Throws<InvalidDataException>(()=>Analyze(a with
            {Samples=a.Samples.Select(s=>s with {Milliseconds=double.NaN}).ToArray()}));
        Assert.Throws<InvalidDataException>(()=>Analyze(a with
            {Counters=a.Counters.Select(c=>c with {Value=-1}).ToArray()}));
        Assert.Throws<InvalidDataException>(()=>Analyze(a with {OverwrittenSamples=-1}));
        Assert.Throws<InvalidDataException>(()=>Analyze());
        Assert.Throws<ArgumentOutOfRangeException>(()=>PerformanceAudit.Analyze((PerformanceScenario)99,[a]));
    }

    [Fact]
    public void BrowserReaderUsesCurrentTypedSchemaAndRejectsOldOrInvalidRunIds()
    {
        var batch=Batch(new(7),0,0);
        var options=new JsonSerializerOptions {PropertyNamingPolicy=JsonNamingPolicy.CamelCase};
        var payload=JsonSerializer.Serialize(new {performance=batch},options);
        using var reader=new StringReader("[ 100ms] [LOG] CCFRAME "+payload+" @ http://localhost/godot.js:1\n");
        var parsed=Assert.Single(PerformanceAudit.ReadBrowserBatches(reader));
        Assert.Equal(batch.Run,parsed.Run);
        Assert.Equal(batch.Samples,parsed.Samples);
        foreach(var invalid in new[]{payload.Replace("\"run\":7,",""),payload.Replace("\"run\":7","\"run\":0"),
            payload.Replace("\"physics_steps\"","\"unknown\"")})
        {
            using var bad=new StringReader("CCFRAME "+invalid);
            Assert.Throws<JsonException>(()=>PerformanceAudit.ReadBrowserBatches(bad).ToArray());
        }
        using var missing=new StringReader("CCFRAME {\"tick\":0}");
        Assert.Throws<InvalidDataException>(()=>PerformanceAudit.ReadBrowserBatches(missing).ToArray());
    }

    [Theory]
    [InlineData(PerformanceScenario.IdleConstruction,"idle_construction")]
    [InlineData(PerformanceScenario.SparseCatalogue,"sparse_catalogue")]
    [InlineData(PerformanceScenario.DoubledPopulation,"doubled_population")]
    [InlineData(PerformanceScenario.DenseContacts,"dense_contacts")]
    [InlineData(PerformanceScenario.ConnectedMechanism,"connected_mechanism")]
    [InlineData(PerformanceScenario.FastTranslation,"fast_translation")]
    [InlineData(PerformanceScenario.PureRotation,"pure_rotation")]
    [InlineData(PerformanceScenario.MovingHollow,"moving_hollow")]
    [InlineData(PerformanceScenario.CoupledMechanism,"coupled_mechanism")]
    [InlineData(PerformanceScenario.VisualHeavy,"visual_heavy")]
    [InlineData(PerformanceScenario.Lifecycle,"lifecycle")]
    public void ScenarioNamesAreCanonicalBoundaryValues(PerformanceScenario scenario,string wire)
    {
        var options=new JsonSerializerOptions();options.Converters.Add(new ScenarioConverter());
        var json=JsonSerializer.Serialize(scenario,options);
        Assert.Equal(wire,JsonSerializer.Deserialize<string>(json));
        Assert.Equal(scenario,JsonSerializer.Deserialize<PerformanceScenario>(json,options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<PerformanceScenario>("\""+wire.ToUpperInvariant()+"\"",options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<PerformanceScenario>("99",options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Serialize((PerformanceScenario)99,options));
    }

    [Fact]
    public void NearestRankQuantilesAndFiniteMeanAreExplicit()
    {
        var distribution=PerformanceAudit.Describe(Enumerable.Range(1,100).Select(v=>(double)v));
        Assert.Equal(50,distribution.P50);Assert.Equal(95,distribution.P95);
        Assert.Equal(99,distribution.P99);Assert.Equal(100,distribution.Maximum);Assert.Equal(50.5,distribution.Mean,10);
        Assert.Equal(double.MaxValue,PerformanceAudit.Describe([double.MaxValue,double.MaxValue]).Mean);
        Assert.Throws<ArgumentException>(()=>PerformanceAudit.Describe([]));
        Assert.Throws<ArgumentException>(()=>PerformanceAudit.Describe([double.PositiveInfinity]));
    }

    [Fact]
    public void FramesHaveIndependentSequencesAndCannotMasqueradeAsTickWork()
    {
        var run=new PerformanceRunId(1);
        var frame=new PlaytestPerformanceBatch(run,
            Enum.GetValues<PerformanceStage>().Where(PerformanceTopology.IsFrameStage).Select(stage=>
                new PlaytestPerformanceSample(run,1,1,stage,PerformanceOutcome.Completed,
                    stage==PerformanceStage.SceneSubmission?4:1,.5,0)).ToArray(),0,[],0);
        var report=Assert.Single(Analyze(Batch(run,0,0),frame,Batch(run,1,2)).Runs);
        Assert.Equal(TickRecordCoverage.Complete,report.Coverage);
        Assert.Equal(2,report.ObservedTicks);Assert.Equal(1,report.ObservedFrames);
        Assert.Equal(.5,Assert.Single(report.Stages,s=>s.Stage==PerformanceStage.GameplayFrame).Milliseconds.Mean);
        foreach(var stage in Enum.GetValues<PerformanceStage>().Where(PerformanceTopology.IsFrameStage))
        {
            var missing=frame with {Samples=frame.Samples.Where(s=>s.Stage!=stage).ToArray()};
            Assert.Equal(TickRecordCoverage.Partial,Assert.Single(Analyze(Batch(run,0,0),missing,Batch(run,1,2)).Runs).Coverage);
        }
        var incompleteSubmission=frame with {Samples=frame.Samples.Select(s=>s.Stage==PerformanceStage.SceneSubmission?s with {Calls=3}:s).ToArray()};
        Assert.Equal(TickRecordCoverage.Partial,Assert.Single(Analyze(Batch(run,0,0),incompleteSubmission,Batch(run,1,2)).Runs).Coverage);
        var conflict=frame with {Samples=frame.Samples.Select(s=>s.Stage==PerformanceStage.Animation?s with {Outcome=PerformanceOutcome.Failed}:s).ToArray()};
        Assert.Throws<InvalidDataException>(()=>Analyze(conflict));
        var mixed=Batch(run,0,0);
        Assert.Throws<InvalidDataException>(()=>Analyze(mixed with
            {Samples=[..mixed.Samples,frame.Samples[0] with {Sequence=0,Tick=0}]}));
        Assert.Throws<InvalidDataException>(()=>Analyze(frame with
            {Samples=[frame.Samples[0] with {Calls=4}]}));
        var failed=frame with {Samples=[frame.Samples[0] with {Outcome=PerformanceOutcome.Failed}]};
        Assert.Equal(TickRecordCoverage.Failed,Assert.Single(Analyze(Batch(run,0,0),failed,Batch(run,1,2)).Runs).Coverage);
        var onlyFrames=Assert.Single(Analyze(frame).Runs);
        Assert.Equal(0,onlyFrames.ObservedTicks);Assert.Equal(1,onlyFrames.ObservedFrames);
        Assert.Equal(TickRecordCoverage.Partial,onlyFrames.Coverage);
    }
}
