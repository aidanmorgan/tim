using System.Text.Json;

namespace CuriousContraptions.Tests;

public class PerformanceRecorderTests
{
    [Fact]
    public void RepeatedStagesAggregateAndDrainedBatchesRemainImmutable()
    {
        var recorder=new PerformanceRecorder();
        recorder.BeginRun();
        recorder.BeginTick(0);
        recorder.Begin(PerformanceStage.Networks);
        var allocation=new byte[4096];
        recorder.End(PerformanceStage.Networks);
        for(var i=0;i<4;i++)
        {
            recorder.Begin(PerformanceStage.Physics);
            recorder.End(PerformanceStage.Physics);
        }
        recorder.EndTick(PerformanceOutcome.Completed);
        GC.KeepAlive(allocation);
        var first=recorder.Drain();
        Assert.Equal(0,first.OverwrittenSamples);
        Assert.Equal(3,first.Samples.Length);
        var physics=Assert.Single(first.Samples,s=>s.Stage==PerformanceStage.Physics);
        Assert.Equal(4,physics.Calls);
        var network=Assert.Single(first.Samples,s=>s.Stage==PerformanceStage.Networks);
        Assert.True(network.AllocatedBytes>=4096);
        var total=Assert.Single(first.Samples,s=>s.Stage==PerformanceStage.GameplayTick);
        Assert.True(total.Milliseconds>=network.Milliseconds+physics.Milliseconds);
        Assert.All(first.Samples,s=>Assert.Equal(PerformanceOutcome.Completed,s.Outcome));
        Assert.Empty(recorder.Drain().Samples);
        recorder.BeginRun();
        recorder.BeginTick(0); // Reset may repeat a gameplay tick, sequence must not repeat.
        recorder.EndTick(PerformanceOutcome.Completed);
        Assert.Equal(1,Assert.Single(recorder.Drain().Samples).Sequence);
        Assert.All(first.Samples,s=>Assert.Equal(0,s.Sequence));
    }

    [Fact]
    public void FailedTickClosesActiveStagesAndOverflowIsExplicit()
    {
        var recorder=new PerformanceRecorder(4);
        recorder.BeginRun();
        for(var tick=0;tick<3;tick++)
        {
            recorder.BeginTick(tick);
            recorder.Begin(PerformanceStage.Physics);
            recorder.EndTick(PerformanceOutcome.Failed);
        }
        var batch=recorder.Drain();
        Assert.Equal(2,batch.OverwrittenSamples);
        Assert.Equal(4,batch.Samples.Length);
        Assert.All(batch.Samples,s=>Assert.Equal(PerformanceOutcome.Failed,s.Outcome));
        Assert.Equal(new long[]{1,1,2,2},batch.Samples.Select(s=>s.Sequence));
        Assert.Equal(0,recorder.Drain().OverwrittenSamples);
        recorder.BeginTick(3);recorder.EndTick(PerformanceOutcome.Completed);
        Assert.Single(recorder.Drain().Samples);
    }

    [Fact]
    public void InvalidSelectorsAndUnfinishedReadsAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>new PerformanceRecorder(0));
        var recorder=new PerformanceRecorder();
        recorder.BeginRun();
        Assert.Throws<ArgumentOutOfRangeException>(()=>recorder.BeginTick(-1));
        Assert.Throws<InvalidOperationException>(()=>recorder.EndTick(PerformanceOutcome.Completed));
        recorder.BeginTick(0);
        Assert.Throws<InvalidOperationException>(()=>recorder.BeginTick(1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>recorder.Begin((PerformanceStage)99));
        Assert.Throws<ArgumentOutOfRangeException>(()=>recorder.End((PerformanceStage)99));
        Assert.Throws<ArgumentOutOfRangeException>(()=>recorder.EndTick((PerformanceOutcome)99));
        Assert.Throws<InvalidOperationException>(()=>recorder.Drain());
        recorder.Begin(PerformanceStage.Physics);
        Assert.Throws<InvalidOperationException>(()=>recorder.Begin(PerformanceStage.Physics));
        Assert.Throws<InvalidOperationException>(()=>recorder.EndTick(PerformanceOutcome.Completed));
        recorder.EndTick(PerformanceOutcome.Failed);
        Assert.Equal(2,recorder.Drain().Samples.Length);
    }

    [Theory]
    [InlineData(PerformanceStage.GameplayTick,"gameplay_tick")]
    [InlineData(PerformanceStage.Networks,"networks")]
    [InlineData(PerformanceStage.Physics,"physics")]
    [InlineData(PerformanceStage.Publication,"publication")]
    [InlineData(PerformanceStage.GameplayFrame,"gameplay_frame")]
    [InlineData(PerformanceStage.PhysicalPresentation,"physical_presentation")]
    [InlineData(PerformanceStage.Animation,"animation")]
    [InlineData(PerformanceStage.SceneSubmission,"scene_submission")]
    public void StageBoundaryIsCanonicalAndRejectsUnsupportedValues(PerformanceStage stage,string wire)
    {
        var sample=new PlaytestPerformanceSample(new(1),0,0,stage,PerformanceOutcome.Completed,1,0,0);
        var json=JsonSerializer.Serialize(sample,PlaytestJson.Default.PlaytestPerformanceSample);
        using var document=JsonDocument.Parse(json);
        Assert.Equal(wire,document.RootElement.GetProperty("stage").GetString());
        Assert.Equal(sample,JsonSerializer.Deserialize(json,PlaytestJson.Default.PlaytestPerformanceSample));
        foreach(var unsupported in new[]{wire.ToUpperInvariant(),"Unknown","99","presentation"})
            Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize(json.Replace(wire,unsupported),
                PlaytestJson.Default.PlaytestPerformanceSample));
        Assert.Throws<JsonException>(()=>JsonSerializer.Serialize(sample with {Stage=(PerformanceStage)99},
            PlaytestJson.Default.PlaytestPerformanceSample));
    }

    [Theory]
    [InlineData(PerformanceOutcome.Completed,"completed")]
    [InlineData(PerformanceOutcome.Failed,"failed")]
    public void OutcomeBoundaryIsCanonical(PerformanceOutcome outcome,string wire)
    {
        var sample=new PlaytestPerformanceSample(new(1),0,0,PerformanceStage.GameplayTick,outcome,1,0,0);
        var json=JsonSerializer.Serialize(sample,PlaytestJson.Default.PlaytestPerformanceSample);
        using var document=JsonDocument.Parse(json);
        Assert.Equal(wire,document.RootElement.GetProperty("outcome").GetString());
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize(json.Replace(wire,wire.ToUpperInvariant()),
            PlaytestJson.Default.PlaytestPerformanceSample));
        Assert.Throws<JsonException>(()=>JsonSerializer.Serialize(sample with {Outcome=(PerformanceOutcome)99},
            PlaytestJson.Default.PlaytestPerformanceSample));
    }

    [Fact]
    public void PhysicsCountersAggregateReturnedStepsAndSurviveDrainAndReset()
    {
        var recorder=new PerformanceRecorder();
        recorder.BeginRun();
        recorder.BeginTick(8);
        recorder.Begin(PerformanceStage.Physics);
        recorder.RecordPhysicsStep(new(2,3,5,7,11,2,20,1,12,new(1,10,8,6,2,12,9,7),new(22,31,40,10,default),new(2,3,4)));
        recorder.End(PerformanceStage.Physics);
        recorder.Begin(PerformanceStage.Physics);
        recorder.RecordPhysicsStep(new(13,17,19,23,29,3,70,2,6,new(3,20,16,12,4,24,18,14),new(73,81,100,20,default),new(3,7,6)));
        recorder.End(PerformanceStage.Physics);
        recorder.EndTick(PerformanceOutcome.Completed);
        var first=recorder.Drain();
        var values=first.Counters.ToDictionary(c=>c.Metric,c=>c.Value);
        Assert.Equal(2,values[PerformanceMetric.PhysicsSteps]);
        Assert.Equal(15,values[PerformanceMetric.PhysicsSubsteps]);
        Assert.Equal(20,values[PerformanceMetric.PhysicsEvents]);
        Assert.Equal(24,values[PerformanceMetric.SweepIterations]);
        Assert.Equal(30,values[PerformanceMetric.VelocityIterations]);
        Assert.Equal(40,values[PerformanceMetric.PositionIterations]);
        Assert.Equal(5,values[PerformanceMetric.PredictionCalls]);
        Assert.Equal(90,values[PerformanceMetric.PredictionMidpoints]);
        Assert.Equal(3,values[PerformanceMetric.PredictionNewtonIterations]);
        Assert.Equal(12,values[PerformanceMetric.MaximumPredictionCoordinates]);
        Assert.Equal(95,values[PerformanceMetric.PredictionConstraintSolves]);
        Assert.Equal(112,values[PerformanceMetric.PredictionConstraintIterations]);
        Assert.Equal(140,values[PerformanceMetric.PredictionCouplingTests]);
        Assert.Equal(30,values[PerformanceMetric.PredictionCoupledPairs]);
        Assert.Equal(5,values[PerformanceMetric.ImpulseFactorizations]);
        Assert.Equal(10,values[PerformanceMetric.ImpulseCorrectionTrials]);
        Assert.Equal(6,values[PerformanceMetric.MaximumImpulseCorrectionCoordinates]);
        Assert.Equal(4,values[PerformanceMetric.BodyQueries]);
        Assert.Equal(30,values[PerformanceMetric.BodyNodeTests]);
        Assert.Equal(24,values[PerformanceMetric.BodyLeafTests]);
        Assert.Equal(18,values[PerformanceMetric.BodyCandidatePairs]);
        Assert.Equal(6,values[PerformanceMetric.CompoundQueries]);
        Assert.Equal(36,values[PerformanceMetric.CompoundNodeTests]);
        Assert.Equal(27,values[PerformanceMetric.CompoundLeafTests]);
        Assert.Equal(21,values[PerformanceMetric.CompoundCandidatePairs]);
        Assert.All(first.Counters,c=>{Assert.Equal(8,c.Tick);Assert.Equal(0,c.Sequence);});
        Assert.Empty(recorder.Drain().Counters);
        recorder.BeginTick(0);
        recorder.Begin(PerformanceStage.Physics);
        recorder.RecordPhysicsStep(new(1,0,0,0,0,0,0,0,0,default,default,default));
        recorder.EndTick(PerformanceOutcome.Failed);
        var failed=recorder.Drain();
        Assert.Equal(Enum.GetValues<PerformanceMetric>().Length,failed.Counters.Length);
        Assert.All(failed.Counters,c=>
        {
            Assert.Equal(0,c.Tick);Assert.Equal(1,c.Sequence);
            Assert.Equal(PerformanceOutcome.Failed,c.Outcome);
        });
        foreach(var metric in new[]{PerformanceMetric.ImpulseFactorizations,PerformanceMetric.ImpulseCorrectionTrials,PerformanceMetric.MaximumImpulseCorrectionCoordinates})
            Assert.Equal(0,Assert.Single(failed.Counters,c=>c.Metric==metric).Value);
        Assert.Equal(0,Assert.Single(failed.Counters,c=>c.Metric==PerformanceMetric.PhysicsEvents).Value);
        Assert.Equal(20,Assert.Single(first.Counters,c=>c.Metric==PerformanceMetric.PhysicsEvents).Value);
    }

    [Theory]
    [InlineData(-1,0,0,0)]
    [InlineData(1,0,0,0)]
    [InlineData(0,1,0,0)]
    [InlineData(1,1,-1,0)]
    [InlineData(1,1,0,-1)]
    [InlineData(1,1,0,1)]
    public void InvalidPredictionConstraintWorkRejectsBeforeRecording(long solves,long iterations,long tests,long pairs)
    {
        var recorder=new PerformanceRecorder();
        recorder.BeginRun();recorder.BeginTick(0);recorder.Begin(PerformanceStage.Physics);
        Assert.Throws<ArgumentOutOfRangeException>(()=>recorder.RecordPhysicsStep(
            new(1,0,0,0,0,0,0,0,0,default,new(solves,iterations,tests,pairs,default),default)));
        recorder.EndTick(PerformanceOutcome.Failed);
        Assert.Empty(recorder.Drain().Counters);
    }

    [Fact]
    public void CounterOverflowAndInvalidRecordingAreExplicit()
    {
        var recorder=new PerformanceRecorder(4);
        recorder.BeginRun();
        Assert.Throws<InvalidOperationException>(()=>recorder.RecordPhysicsStep(new(1,0,0,0,0,0,0,0,0,default,default,default)));
        recorder.BeginTick(0);
        Assert.Throws<InvalidOperationException>(()=>recorder.RecordPhysicsStep(new(1,0,0,0,0,0,0,0,0,default,default,default)));
        recorder.Begin(PerformanceStage.Physics);
        Assert.Throws<ArgumentOutOfRangeException>(()=>recorder.RecordPhysicsStep(new(1,0,-1,0,0,0,0,0,0,default,default,default)));
        recorder.EndTick(PerformanceOutcome.Failed);
        Assert.Empty(recorder.Drain().Counters);
        for(var tick=0;tick<3;tick++)
        {
            recorder.BeginTick(tick);
            recorder.Begin(PerformanceStage.Physics);
            recorder.RecordPhysicsStep(new(1,0,0,0,0,0,0,0,0,default,default,default));
            recorder.End(PerformanceStage.Physics);
            recorder.EndTick(PerformanceOutcome.Completed);
        }
        var batch=recorder.Drain();
        Assert.Equal(2*Enum.GetValues<PerformanceMetric>().Length,batch.OverwrittenCounters);
        Assert.Equal(Enum.GetValues<PerformanceMetric>().Length,batch.Counters.Length);
        Assert.All(batch.Counters,c=>Assert.Equal(3,c.Sequence));
        Assert.Equal(0,recorder.Drain().OverwrittenCounters);
    }

    [Theory]
    [InlineData(-1,0,0,0)]
    [InlineData(0,-1,0,0)]
    [InlineData(0,0,-1,0)]
    [InlineData(0,0,0,-1)]
    public void NegativePredictionCountersRejectBeforeRecording(int calls,int midpoints,int iterations,int coordinates)
    {
        var recorder=new PerformanceRecorder();
        recorder.BeginRun();
        recorder.BeginTick(0);
        recorder.Begin(PerformanceStage.Physics);
        Assert.Throws<ArgumentOutOfRangeException>(()=>recorder.RecordPhysicsStep(
            new(1,0,0,0,0,calls,midpoints,iterations,coordinates,default,default,default)));
        recorder.EndTick(PerformanceOutcome.Failed);
        Assert.Empty(recorder.Drain().Counters);
    }

    [Theory]
    [InlineData(PerformanceMetric.PredictionConstraintSolves,"prediction_constraint_solves")]
    [InlineData(PerformanceMetric.PredictionConstraintIterations,"prediction_constraint_iterations")]
    [InlineData(PerformanceMetric.PredictionCouplingTests,"prediction_coupling_tests")]
    [InlineData(PerformanceMetric.PredictionCoupledPairs,"prediction_coupled_pairs")]
    [InlineData(PerformanceMetric.ImpulseFactorizations,"impulse_factorizations")]
    [InlineData(PerformanceMetric.ImpulseCorrectionTrials,"impulse_correction_trials")]
    [InlineData(PerformanceMetric.MaximumImpulseCorrectionCoordinates,"maximum_impulse_correction_coordinates")]
    [InlineData(PerformanceMetric.PhysicsSteps,"physics_steps")]
    [InlineData(PerformanceMetric.PhysicsSubsteps,"physics_substeps")]
    [InlineData(PerformanceMetric.PhysicsEvents,"physics_events")]
    [InlineData(PerformanceMetric.SweepIterations,"sweep_iterations")]
    [InlineData(PerformanceMetric.VelocityIterations,"velocity_iterations")]
    [InlineData(PerformanceMetric.PositionIterations,"position_iterations")]
    [InlineData(PerformanceMetric.PredictionCalls,"prediction_calls")]
    [InlineData(PerformanceMetric.PredictionMidpoints,"prediction_midpoints")]
    [InlineData(PerformanceMetric.PredictionNewtonIterations,"prediction_newton_iterations")]
    [InlineData(PerformanceMetric.MaximumPredictionCoordinates,"maximum_prediction_coordinates")]
    [InlineData(PerformanceMetric.BodyQueries,"body_queries")]
    [InlineData(PerformanceMetric.BodyNodeTests,"body_node_tests")]
    [InlineData(PerformanceMetric.BodyLeafTests,"body_leaf_tests")]
    [InlineData(PerformanceMetric.BodyCandidatePairs,"body_candidate_pairs")]
    [InlineData(PerformanceMetric.CompoundQueries,"compound_queries")]
    [InlineData(PerformanceMetric.CompoundNodeTests,"compound_node_tests")]
    [InlineData(PerformanceMetric.CompoundLeafTests,"compound_leaf_tests")]
    [InlineData(PerformanceMetric.CompoundCandidatePairs,"compound_candidate_pairs")]
    public void MetricBoundaryIsCanonicalAndRejectsUnsupportedValues(PerformanceMetric metric,string wire)
    {
        var counter=new PlaytestPerformanceCounter(new(1),0,0,metric,PerformanceOutcome.Completed,7);
        var json=JsonSerializer.Serialize(counter,PlaytestJson.Default.PlaytestPerformanceCounter);
        using var document=JsonDocument.Parse(json);
        Assert.Equal(wire,document.RootElement.GetProperty("metric").GetString());
        Assert.Equal(counter,JsonSerializer.Deserialize(json,PlaytestJson.Default.PlaytestPerformanceCounter));
        foreach(var unsupported in new[]{wire.ToUpperInvariant(),"Unknown","99"})
            Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize(json.Replace(wire,unsupported),
                PlaytestJson.Default.PlaytestPerformanceCounter));
        Assert.Throws<JsonException>(()=>JsonSerializer.Serialize(counter with {Metric=(PerformanceMetric)99},
            PlaytestJson.Default.PlaytestPerformanceCounter));
    }

    [Fact]
    public void RecordingUsesNoRoutineAllocationAfterWarmupAndDtoKeepsTypedValues()
    {
        var recorder=new PerformanceRecorder();
        recorder.BeginRun();
        var result=new Physics.PhysicsStepResult(1,2,3,4,5,1,20,1,6,default,default,default);
        void Tick(int tick)
        {
            recorder.BeginTick(tick);
            recorder.Begin(PerformanceStage.Physics);
            recorder.RecordPhysicsStep(result);
            recorder.End(PerformanceStage.Physics);
            recorder.EndTick(PerformanceOutcome.Completed);
        }
        for(var i=0;i<100;i++)Tick(i);
        recorder.Drain();
        var before=GC.GetAllocatedBytesForCurrentThread();
        for(var i=0;i<100;i++)Tick(i);
        var bytes=GC.GetAllocatedBytesForCurrentThread()-before;
        Assert.Equal(0,bytes);
        var batch=recorder.Drain();
        var dto=PlaytestPerformanceBatch.Capture(batch);
        Assert.Equal(batch.Run,dto.Run);
        Assert.Equal(batch.OverwrittenCounters,dto.OverwrittenCounters);
        Assert.Equal(batch.Counters.Length,dto.Counters.Length);
        for(var i=0;i<batch.Counters.Length;i++)
        {
            var source=batch.Counters[i];var mapped=dto.Counters[i];
            Assert.Equal(source.Metric,mapped.Metric);Assert.Equal(source.Value,mapped.Value);
            Assert.Equal(source.Outcome,mapped.Outcome);Assert.Equal(source.Sequence,mapped.Sequence);
            Assert.Equal(source.Tick,mapped.Tick);Assert.Equal(source.Run,mapped.Run);
        }
    }


    [Fact]
    public void UndrainedSamplesKeepTheirRunAcrossRepeatedTicks()
    {
        var recorder=new PerformanceRecorder();
        Assert.Throws<InvalidOperationException>(()=>recorder.BeginTick(0));
        Assert.Throws<InvalidOperationException>(()=>recorder.Drain());
        var first=recorder.BeginRun();
        recorder.BeginTick(0);
        Assert.Throws<InvalidOperationException>(()=>recorder.BeginRun());
        recorder.Begin(PerformanceStage.Physics);
        recorder.RecordPhysicsStep(new(1,0,0,0,0,0,0,0,0,default,default,default));
        recorder.End(PerformanceStage.Physics);
        recorder.EndTick(PerformanceOutcome.Completed);
        var second=recorder.BeginRun();
        Assert.NotEqual(first,second);
        var pending=recorder.Drain();
        Assert.Equal(second,pending.Run);
        Assert.All(pending.Samples,s=>Assert.Equal(first,s.Run));
        Assert.All(pending.Counters,c=>Assert.Equal(first,c.Run));
        recorder.BeginTick(0);
        recorder.EndTick(PerformanceOutcome.Completed);
        var current=Assert.Single(recorder.Drain().Samples);
        Assert.Equal(second,current.Run);Assert.Equal(0,current.Tick);Assert.Equal(1,current.Sequence);
        Assert.All(pending.Samples,s=>Assert.Equal(first,s.Run));
    }

    [Fact]
    public void RunBoundaryRejectsMissingInvalidAndStringIdentifiers()
    {
        var sample=new PlaytestPerformanceSample(new(7),0,0,PerformanceStage.GameplayTick,
            PerformanceOutcome.Completed,1,0,0);
        var json=JsonSerializer.Serialize(sample,PlaytestJson.Default.PlaytestPerformanceSample);
        Assert.Equal(sample,JsonSerializer.Deserialize(json,PlaytestJson.Default.PlaytestPerformanceSample));
        using var document=JsonDocument.Parse(json);
        Assert.Equal(7,document.RootElement.GetProperty("run").GetInt64());
        foreach(var invalid in new[]{"0","-1","1.5","null","\"7\"","9223372036854775808"})
            Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize(json.Replace("\"run\":7","\"run\":"+invalid),
                PlaytestJson.Default.PlaytestPerformanceSample));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize(json.Replace("\"run\":7,",""),
            PlaytestJson.Default.PlaytestPerformanceSample));
        Assert.Throws<JsonException>(()=>JsonSerializer.Serialize(sample with {Run=default},
            PlaytestJson.Default.PlaytestPerformanceSample));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new PerformanceRunId(0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new PerformanceRunId(-1));
    }


    [Fact]
    public void EveryPerformanceFieldIsRequiredAndUnknownFieldsReject()
    {
        static void Check<T>(T value,System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> type)
        {
            var json=JsonSerializer.Serialize(value,type);
            var original=System.Text.Json.Nodes.JsonNode.Parse(json)!.AsObject();
            foreach(var field in original.Select(p=>p.Key).ToArray())
            {
                var changed=original.DeepClone().AsObject();changed.Remove(field);
                Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize(changed.ToJsonString(),type));
            }
            original.Add("unsupported",0);
            Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize(original.ToJsonString(),type));
        }
        var sample=new PlaytestPerformanceSample(new(1),0,0,PerformanceStage.GameplayTick,
            PerformanceOutcome.Completed,1,0,0);
        var counter=new PlaytestPerformanceCounter(new(1),0,0,PerformanceMetric.PhysicsSteps,
            PerformanceOutcome.Completed,4);
        Check(sample,PlaytestJson.Default.PlaytestPerformanceSample);
        Check(counter,PlaytestJson.Default.PlaytestPerformanceCounter);
        Check(new PlaytestPerformanceBatch(new(1),[sample],0,[counter],0),PlaytestJson.Default.PlaytestPerformanceBatch);
    }


    [Fact]
    public void FramesAreSeparateIntervalsWithFailureAndNestingGuards()
    {
        var recorder=new PerformanceRecorder();recorder.BeginRun();
        recorder.BeginTick(0);
        Assert.Throws<InvalidOperationException>(()=>recorder.Begin(PerformanceStage.GameplayFrame));
        Assert.Throws<InvalidOperationException>(()=>recorder.BeginFrame(0));
        Assert.Throws<InvalidOperationException>(()=>recorder.EndFrame(PerformanceOutcome.Completed));
        recorder.EndTick(PerformanceOutcome.Completed);
        recorder.BeginFrame(1);
        Assert.Throws<InvalidOperationException>(()=>recorder.BeginTick(1));
        Assert.Throws<InvalidOperationException>(()=>recorder.Begin(PerformanceStage.Physics));
        Assert.Throws<InvalidOperationException>(()=>recorder.EndTick(PerformanceOutcome.Completed));
        Assert.Throws<InvalidOperationException>(()=>recorder.Drain());
        Assert.Throws<ArgumentOutOfRangeException>(()=>recorder.EndFrame((PerformanceOutcome)99));
        recorder.EndFrame(PerformanceOutcome.Failed);
        var batch=recorder.Drain();
        Assert.Empty(batch.Counters);
        var frame=Assert.Single(batch.Samples,s=>s.Stage==PerformanceStage.GameplayFrame);
        Assert.Equal(1,frame.Sequence);Assert.Equal(1,frame.Tick);Assert.Equal(1,frame.Calls);
        Assert.Equal(PerformanceOutcome.Failed,frame.Outcome);
        Assert.Throws<ArgumentOutOfRangeException>(()=>recorder.BeginFrame(-1));
        recorder.BeginFrame(1);recorder.EndFrame(PerformanceOutcome.Completed);
        Assert.Equal(2,Assert.Single(recorder.Drain().Samples).Sequence);
    }

    [Fact]
    public void WarmFrameRecordingAllocatesNoManagedMemory()
    {
        var recorder=new PerformanceRecorder();recorder.BeginRun();
        for(var i=0;i<100;i++){recorder.BeginFrame(0);recorder.EndFrame(PerformanceOutcome.Completed);}
        recorder.Drain();
        var before=GC.GetAllocatedBytesForCurrentThread();
        for(var i=0;i<100;i++){recorder.BeginFrame(0);recorder.EndFrame(PerformanceOutcome.Completed);}
        var bytes=GC.GetAllocatedBytesForCurrentThread()-before;
        Assert.Equal(0,bytes);
        var batch=recorder.Drain();Assert.Equal(100,batch.Samples.Length);Assert.Empty(batch.Counters);
    }
}
