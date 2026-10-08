using System;
using System.Diagnostics;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

/// <summary>Bounded diagnostic-only storage. Inclusive, same-thread elapsed time and
/// allocated bytes; not CPU or GPU time. Returned batches never alias the ring.</summary>
public sealed class PerformanceRecorder
{
    private static readonly PerformanceStage[] Stages=Enum.GetValues<PerformanceStage>();
    private readonly PerformanceSample[] _samples;
    private static readonly PerformanceMetric[] Metrics=Enum.GetValues<PerformanceMetric>();
    private readonly PerformanceCounter[] _counters;
    private readonly long[] _values=new long[Metrics.Length];
    private int _counterHead,_counterCount;
    private long _overwrittenCounters;
    private readonly long[] _started=new long[Stages.Length],_allocated=new long[Stages.Length];
    private readonly long[] _elapsed=new long[Stages.Length],_bytes=new long[Stages.Length];
    private readonly int[] _calls=new int[Stages.Length];
    private readonly bool[] _active=new bool[Stages.Length];
    private int _head,_count,_tick;
    private long _sequence,_overwritten;
    private enum RecordingPhase { Idle, Tick, Frame }
    private RecordingPhase _phase;
    private PerformanceRunId _run;
    public PerformanceRunId Run=>_run.Value>0?_run:
        throw new InvalidOperationException("No diagnostic run has started.");
    public PerformanceRunId BeginRun()
    {
        if(_phase!=RecordingPhase.Idle)throw new InvalidOperationException("Cannot change runs during a diagnostic tick.");
        _run=new(checked(_run.Value+1));
        return _run;
    }
    public PerformanceRecorder(int capacity=1024)
    {
        if(capacity<1)throw new ArgumentOutOfRangeException(nameof(capacity));
        _samples=new PerformanceSample[capacity];
        _counters=new PerformanceCounter[Math.Max(capacity,Metrics.Length)];
    }
    private static int Index(PerformanceStage stage)=>Enum.IsDefined(stage)?(int)stage:
        throw new ArgumentOutOfRangeException(nameof(stage));
    public void BeginTick(int tick)
    {
        if(tick<0)throw new ArgumentOutOfRangeException(nameof(tick));
        if(_phase!=RecordingPhase.Idle)throw new InvalidOperationException("A diagnostic tick is already active.");
        _=Run;
        _phase=RecordingPhase.Tick;_tick=tick;
        Array.Clear(_values);
        Array.Clear(_elapsed);Array.Clear(_bytes);Array.Clear(_calls);Array.Clear(_active);
        Begin(PerformanceStage.GameplayTick);
    }
    public void BeginFrame(int tick)
    {
        if(tick<0)throw new ArgumentOutOfRangeException(nameof(tick));
        if(_phase!=RecordingPhase.Idle)throw new InvalidOperationException("A diagnostic interval is already active.");
        _=Run;
        _phase=RecordingPhase.Frame;_tick=tick;
        Array.Clear(_values);
        Array.Clear(_elapsed);Array.Clear(_bytes);Array.Clear(_calls);Array.Clear(_active);
        Begin(PerformanceStage.GameplayFrame);
    }
    public void Begin(PerformanceStage stage)
    {
        var index=Index(stage);
        if(_phase==RecordingPhase.Idle||_active[index]||
            (_phase==RecordingPhase.Frame)!=PerformanceTopology.IsFrameStage(stage))throw new InvalidOperationException("Invalid diagnostic stage entry.");
        _started[index]=Stopwatch.GetTimestamp();
        _allocated[index]=GC.GetAllocatedBytesForCurrentThread();
        _active[index]=true;
    }
    public void End(PerformanceStage stage)
    {
        var index=Index(stage);
        if(_phase==RecordingPhase.Idle||!_active[index])throw new InvalidOperationException("Diagnostic stage is not active.");
        _elapsed[index]+=Stopwatch.GetTimestamp()-_started[index];
        _bytes[index]+=GC.GetAllocatedBytesForCurrentThread()-_allocated[index];
        _calls[index]++;_active[index]=false;
    }
    /// <summary>Counts only successfully returned physics calls. A failed outer tick
    /// retains earlier returned calls, marked Failed; unfinished solver work is absent.</summary>
    public void RecordPhysicsStep(PhysicsStepResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if(_phase==RecordingPhase.Idle||!_active[Index(PerformanceStage.Physics)])
            throw new InvalidOperationException("Physics counters require an active physics stage.");
        if(result.Substeps<0||result.Events<0||result.SweepIterations<0||
            result.VelocityIterations<0||result.PositionIterations<0||result.PredictionCalls<0||
            result.PredictionMidpoints<0||result.PredictionNewtonIterations<0||result.MaximumPredictionCoordinates<0)
            throw new ArgumentOutOfRangeException(nameof(result));
        result.SpatialWork.Validate();
        result.PredictionConstraints.Validate();
        result.ImpulseCorrections.Validate();
        _values[(int)PerformanceMetric.ImpulseFactorizations]=checked(_values[(int)PerformanceMetric.ImpulseFactorizations]+result.ImpulseCorrections.Factorizations);
        _values[(int)PerformanceMetric.ImpulseCorrectionTrials]=checked(_values[(int)PerformanceMetric.ImpulseCorrectionTrials]+result.ImpulseCorrections.Trials);
        _values[(int)PerformanceMetric.MaximumImpulseCorrectionCoordinates]=Math.Max(_values[(int)PerformanceMetric.MaximumImpulseCorrectionCoordinates],result.ImpulseCorrections.MaximumCoordinates);
        _values[(int)PerformanceMetric.PredictionConstraintSolves]=checked(_values[(int)PerformanceMetric.PredictionConstraintSolves]+result.PredictionConstraints.Solves);
        _values[(int)PerformanceMetric.PredictionConstraintIterations]=checked(_values[(int)PerformanceMetric.PredictionConstraintIterations]+result.PredictionConstraints.Iterations);
        _values[(int)PerformanceMetric.PredictionCouplingTests]=checked(_values[(int)PerformanceMetric.PredictionCouplingTests]+result.PredictionConstraints.CouplingTests);
        _values[(int)PerformanceMetric.PredictionCoupledPairs]=checked(_values[(int)PerformanceMetric.PredictionCoupledPairs]+result.PredictionConstraints.CoupledPairs);
        _values[(int)PerformanceMetric.PhysicsSteps]++;
        _values[(int)PerformanceMetric.PhysicsSubsteps]+=result.Substeps;
        _values[(int)PerformanceMetric.PhysicsEvents]+=result.Events;
        _values[(int)PerformanceMetric.SweepIterations]+=result.SweepIterations;
        _values[(int)PerformanceMetric.VelocityIterations]+=result.VelocityIterations;
        _values[(int)PerformanceMetric.PositionIterations]+=result.PositionIterations;
        _values[(int)PerformanceMetric.PredictionCalls]+=result.PredictionCalls;
        _values[(int)PerformanceMetric.PredictionMidpoints]+=result.PredictionMidpoints;
        _values[(int)PerformanceMetric.PredictionNewtonIterations]+=result.PredictionNewtonIterations;
        _values[(int)PerformanceMetric.MaximumPredictionCoordinates]=Math.Max(
            _values[(int)PerformanceMetric.MaximumPredictionCoordinates],result.MaximumPredictionCoordinates);
        _values[(int)PerformanceMetric.BodyQueries]=checked(_values[(int)PerformanceMetric.BodyQueries]+result.SpatialWork.BodyQueries);
        _values[(int)PerformanceMetric.BodyNodeTests]=checked(_values[(int)PerformanceMetric.BodyNodeTests]+result.SpatialWork.BodyNodeTests);
        _values[(int)PerformanceMetric.BodyLeafTests]=checked(_values[(int)PerformanceMetric.BodyLeafTests]+result.SpatialWork.BodyLeafTests);
        _values[(int)PerformanceMetric.BodyCandidatePairs]=checked(_values[(int)PerformanceMetric.BodyCandidatePairs]+result.SpatialWork.BodyCandidatePairs);
        _values[(int)PerformanceMetric.CompoundQueries]=checked(_values[(int)PerformanceMetric.CompoundQueries]+result.SpatialWork.CompoundQueries);
        _values[(int)PerformanceMetric.CompoundNodeTests]=checked(_values[(int)PerformanceMetric.CompoundNodeTests]+result.SpatialWork.CompoundNodeTests);
        _values[(int)PerformanceMetric.CompoundLeafTests]=checked(_values[(int)PerformanceMetric.CompoundLeafTests]+result.SpatialWork.CompoundLeafTests);
        _values[(int)PerformanceMetric.CompoundCandidatePairs]=checked(_values[(int)PerformanceMetric.CompoundCandidatePairs]+result.SpatialWork.CompoundCandidatePairs);
    }
    public void EndTick(PerformanceOutcome outcome)
    {
        if(_phase!=RecordingPhase.Tick)throw new InvalidOperationException("No diagnostic tick is active.");
        Finish(outcome,PerformanceStage.GameplayTick);
    }
    public void EndFrame(PerformanceOutcome outcome)
    {
        if(_phase!=RecordingPhase.Frame)throw new InvalidOperationException("No diagnostic frame is active.");
        Finish(outcome,PerformanceStage.GameplayFrame);
    }
    private void Finish(PerformanceOutcome outcome,PerformanceStage root)
    {
        if(!Enum.IsDefined(outcome))throw new ArgumentOutOfRangeException(nameof(outcome));
        if(_phase==RecordingPhase.Idle)throw new InvalidOperationException("No diagnostic tick is active.");
        if(outcome==PerformanceOutcome.Completed)
            foreach(var stage in Stages)
                if(stage!=root&&_active[Index(stage)])
                    throw new InvalidOperationException("Completed tick has an unfinished stage.");
        foreach(var stage in Stages)if(_active[Index(stage)])End(stage);
        foreach(var stage in Stages)
        {
            var index=Index(stage);
            if(_calls[index]==0)continue;
            var sample=new PerformanceSample(Run,_sequence,_tick,stage,outcome,_calls[index],
                _elapsed[index]*1000d/Stopwatch.Frequency,_bytes[index]);
            if(_count==_samples.Length){_head=(_head+1)%_samples.Length;_count--;_overwritten++;}
            _samples[(_head+_count)%_samples.Length]=sample;_count++;
        }
        if(_values[(int)PerformanceMetric.PhysicsSteps]>0)
            foreach(var metric in Metrics)
            {
                if(_counterCount==_counters.Length)
                {
                    _counterHead=(_counterHead+1)%_counters.Length;
                    _counterCount--;_overwrittenCounters++;
                }
                _counters[(_counterHead+_counterCount)%_counters.Length]=
                    new(Run,_sequence,_tick,metric,outcome,_values[(int)metric]);
                _counterCount++;
            }
        _sequence++;_phase=RecordingPhase.Idle;
    }
    public PerformanceBatch Drain()
    {
        if(_phase!=RecordingPhase.Idle)throw new InvalidOperationException("Cannot read an unfinished diagnostic tick.");
        var copy=new PerformanceSample[_count];
        for(var i=0;i<_count;i++)copy[i]=_samples[(_head+i)%_samples.Length];
        var counters=new PerformanceCounter[_counterCount];
        for(var i=0;i<_counterCount;i++)counters[i]=_counters[(_counterHead+i)%_counters.Length];
        var result=new PerformanceBatch(Run,copy,_overwritten,counters,_overwrittenCounters);
        _counterHead=0;_counterCount=0;_overwrittenCounters=0;
        _head=0;_count=0;_overwritten=0;
        return result;
    }
}
