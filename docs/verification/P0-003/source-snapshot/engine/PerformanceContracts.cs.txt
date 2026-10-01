using System;

namespace CuriousContraptions;

public enum PerformanceStage { GameplayTick, Networks, Physics, Publication, GameplayFrame, PhysicalPresentation, Animation, SceneSubmission }
public static class PerformanceTopology
{
    public static bool IsFrameStage(PerformanceStage stage)=>stage switch
    {
        PerformanceStage.GameplayFrame or PerformanceStage.PhysicalPresentation or
            PerformanceStage.Animation or PerformanceStage.SceneSubmission=>true,
        PerformanceStage.GameplayTick or PerformanceStage.Networks or PerformanceStage.Physics or
            PerformanceStage.Publication=>false,
        _=>throw new ArgumentOutOfRangeException(nameof(stage))
    };
}
public enum PerformanceOutcome { Completed, Failed }
public enum PerformanceMetric { PhysicsSteps, PhysicsSubsteps, PhysicsEvents, SweepIterations, VelocityIterations, PositionIterations, PredictionCalls, PredictionMidpoints, PredictionNewtonIterations, MaximumPredictionCoordinates, BodyQueries, BodyNodeTests, BodyLeafTests, BodyCandidatePairs, CompoundQueries, CompoundNodeTests, CompoundLeafTests, CompoundCandidatePairs, PredictionConstraintSolves, PredictionConstraintIterations, PredictionCouplingTests, PredictionCoupledPairs }
public readonly record struct PerformanceRunId
{
    public long Value { get; }
    public PerformanceRunId(long value)
    {
        if(value<=0)throw new ArgumentOutOfRangeException(nameof(value));
        Value=value;
    }
}
public readonly record struct PerformanceCounter(PerformanceRunId Run,long Sequence,int Tick,PerformanceMetric Metric,
    PerformanceOutcome Outcome,long Value);
public readonly record struct PerformanceSample(PerformanceRunId Run,long Sequence,int Tick,PerformanceStage Stage,
    PerformanceOutcome Outcome,int Calls,double Milliseconds,long AllocatedBytes);
public sealed record PerformanceBatch(PerformanceRunId Run,PerformanceSample[] Samples,long OverwrittenSamples,
    PerformanceCounter[] Counters,long OverwrittenCounters);
