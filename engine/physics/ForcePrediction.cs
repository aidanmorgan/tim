using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CuriousContraptions.Physics;

/// <summary>Returned instantaneous constraint solves used to initialize support.
/// Coupled midpoint equation evaluations and Newton factorizations are counted
/// separately by ForcePrediction; they are not additional impulse solves.
/// Failed predictions produce no completed report.</summary>
public readonly record struct PredictionConstraintWork(long Solves,long Iterations,long CouplingTests,long CoupledPairs,ImpulseCorrectionWork Corrections)
{
    internal PredictionConstraintWork Add(ImpulseSolveResult result)=>Add(new PredictionConstraintWork(
        1,result.Iterations,result.CouplingTests,result.CoupledPairs,result.Corrections));
    internal PredictionConstraintWork Add(PredictionConstraintWork other)
    {
        Validate();other.Validate();
        return new(checked(Solves+other.Solves),checked(Iterations+other.Iterations),
            checked(CouplingTests+other.CouplingTests),checked(CoupledPairs+other.CoupledPairs),Corrections.Add(other.Corrections));
    }
    public void Validate()
    {
        Corrections.Validate();
        if(Corrections.Factorizations>Iterations||Solves==0&&Corrections!=default||Solves<0||Iterations<Solves||CouplingTests<0||CoupledPairs<0||CoupledPairs>CouplingTests||
            Solves==0&&(Iterations!=0||CouplingTests!=0||CoupledPairs!=0))
            throw new ArgumentOutOfRangeException(nameof(PredictionConstraintWork),"Invalid prediction constraint work.");
    }
}

public enum ForcePredictionBoundary { IntervalEnd, Friction, Joint, Field, Accuracy }

/// <summary>The force-limited interval and the exact paths certified for it.
/// The world must commit these paths, not rebuild a different full-step path.</summary>
public sealed class ForcePrediction
{
    public IReadOnlyList<PredictedSpringConstraint> SpringConstraints { get; }
    public IReadOnlyList<PredictedMotorUse> Motors { get; }
    public IReadOnlyList<PredictedTransferWork> Transfers { get; }
    public IReadOnlyList<MechanicalTransferBodyImpulse> TransferBodyImpulses { get; }
    /// <summary>Actual minus evaluated support work, bounded per body without cancellation.
    /// Null when no transfer family requested this certificate.</summary>
    public WrenchPathWorkResult? TransferResidualWork { get; }
    public int MidpointEvaluations { get; }
    public int NewtonIterations { get; }
    public int Coordinates { get; }
    public PredictionConstraintWork ConstraintWork { get; }
    public double Duration { get; }
    public ForcePredictionBoundary Boundary { get; }
    public IReadOnlyDictionary<PhysicsBodyId,BodyWrench> Wrenches { get; }
    public IReadOnlyDictionary<PhysicsBodyId,BodyTrajectory> Trajectories { get; }
    internal ForcePrediction(double duration,ForcePredictionBoundary boundary,
        IReadOnlyDictionary<PhysicsBodyId,BodyWrench> wrenches,Dictionary<PhysicsBodyId,BodyTrajectory> trajectories,
        int midpointEvaluations,int newtonIterations,int coordinates,PredictionConstraintWork constraintWork,
        PredictedMotorUse[] motors,PredictedTransferWork[] transfers,MechanicalTransferBodyImpulse[] transferBodyImpulses,WrenchPathWorkResult? transferResidualWork,PredictedSpringConstraint[] springConstraints)
    {
        SpringConstraints=Array.AsReadOnly((PredictedSpringConstraint[])springConstraints.Clone());
        TransferResidualWork=transferResidualWork;
        TransferBodyImpulses=Array.AsReadOnly((MechanicalTransferBodyImpulse[])transferBodyImpulses.Clone());
        Transfers=transfers.Length==0?Array.Empty<PredictedTransferWork>():Array.AsReadOnly((PredictedTransferWork[])transfers.Clone());
        Motors=motors.Length==0?Array.Empty<PredictedMotorUse>():Array.AsReadOnly((PredictedMotorUse[])motors.Clone());
        constraintWork.Validate();ConstraintWork=constraintWork;
        MidpointEvaluations=midpointEvaluations;NewtonIterations=newtonIterations;Coordinates=coordinates;
        Duration=duration; Boundary=boundary; Wrenches=wrenches;
        Trajectories=new ReadOnlyDictionary<PhysicsBodyId,BodyTrajectory>(new Dictionary<PhysicsBodyId,BodyTrajectory>(trajectories));
    }
}
