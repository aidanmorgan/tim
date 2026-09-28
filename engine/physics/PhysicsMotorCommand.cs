using System;

namespace CuriousContraptions.Physics;

/// <summary>One world's step supply, not a persistent infinite-energy velocity
/// target. Effort is force for a slider and torque for a hinge. Work is joules.
/// Zero effort disables actuation; zero work permits only dissipative braking.</summary>
public readonly record struct PhysicsMotorCommand
{
    public PhysicsJointId Joint { get; }
    public double TargetSpeed { get; }
    public double MaximumEffort { get; }
    public double AvailableWork { get; }
    public PhysicsMotorCommand(PhysicsJointId joint,double targetSpeed,double maximumEffort,double availableWork)
    {
        if(!double.IsFinite(targetSpeed)||!double.IsFinite(maximumEffort)||maximumEffort<0||
            !double.IsFinite(availableWork)||availableWork<0)
            throw new ArgumentException("Motor target and budgets must be finite, with nonnegative supply.");
        Joint=joint; TargetSpeed=targetSpeed; MaximumEffort=maximumEffort; AvailableWork=availableWork;
    }
}
public readonly record struct PhysicsMotorUse(PhysicsJointId Joint,double AbsoluteImpulse,double SuppliedWork,
    double DissipatedWork,double RemainingWork);

internal sealed class PhysicsMotorBudget
{
    public PhysicsFrameJoint Joint { get; }
    private readonly PhysicsMotorCommand _command;
    private readonly double _impulseBudget;
    private double _remainingImpulse,_remainingWork,_dissipated;
    public PhysicsMotorBudget(PhysicsFrameJoint joint,PhysicsMotorCommand command,double duration)
    {
        Joint=joint; _command=command; _impulseBudget=command.MaximumEffort*duration;
        if(!double.IsFinite(_impulseBudget)) throw new ArgumentException("Motor impulse budget is not representable.");
        _remainingImpulse=_impulseBudget; _remainingWork=command.AvailableWork;
    }
    private static double Spend(double remaining,double cost)
    {
        if(!double.IsFinite(cost)||cost<0||cost>remaining) throw new InvalidOperationException("Motor exceeded its remaining budget.");
        if(cost==0) return remaining;
        var next=remaining-cost;
        // Outward rounding: repeated tiny debits may not leave the supply
        // unchanged or round the remaining allowance upward.
        return next>0?Math.BitDecrement(next):0;
    }
    public void Apply(double duration)
    {
        var row=Joint.Travel;
        var limit=Math.Min(_remainingImpulse,_command.MaximumEffort*duration);
        var use=PoweredImpulse.Apply(Joint.A,Joint.B,row.Jacobian,_command.TargetSpeed,limit,_remainingWork);
        _remainingImpulse=Spend(_remainingImpulse,Math.Abs(use.Impulse));
        _remainingWork=Spend(_remainingWork,use.SuppliedWork);
        _dissipated+=use.DissipatedWork;
        if(!double.IsFinite(_dissipated)) throw new InvalidOperationException("Motor dissipation is not representable.");
    }
    public PhysicsMotorUse Report=>new(Joint.Id,_impulseBudget-_remainingImpulse,
        _command.AvailableWork-_remainingWork,_dissipated,_remainingWork);
}
