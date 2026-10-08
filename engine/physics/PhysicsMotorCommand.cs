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
    public double MaximumPower { get; }
    public PhysicsMotorCommand(PhysicsJointId joint,double targetSpeed,double maximumEffort,double availableWork,double maximumPower)
    {
        if(!double.IsFinite(targetSpeed)||!double.IsFinite(maximumEffort)||maximumEffort<0||
            !double.IsFinite(availableWork)||availableWork<0||!double.IsFinite(maximumPower)||maximumPower<0)
            throw new ArgumentException("Motor target and budgets must be finite, with nonnegative supply.");
        Joint=joint; TargetSpeed=targetSpeed; MaximumEffort=maximumEffort; AvailableWork=availableWork; MaximumPower=maximumPower;
    }
}
public readonly record struct PhysicsMotorUse(PhysicsJointId Joint,double AbsoluteImpulse,double SuppliedWork,
    double DissipatedWork,double RemainingWork,double SuppliedWorkError,double DissipatedWorkError,double ReservedWork);

/// <summary>Cumulative committed motor work, including braking losses, owned by the world.
/// Remaining supply is step-local and is deliberately not accumulated.</summary>
public readonly record struct PhysicsMotorTotals(PhysicsJointId Joint,double AbsoluteImpulse,
    double SuppliedWork,double DissipatedWork,double SuppliedWorkError,double DissipatedWorkError,double ReservedWork)
{
    internal PhysicsMotorTotals Add(PhysicsMotorUse use)
    {
        if(use.Joint!=Joint) throw new ArgumentException("Motor accounting identities must match.");
        var next=new PhysicsMotorTotals(Joint,AbsoluteImpulse+use.AbsoluteImpulse,
            SuppliedWork+use.SuppliedWork,DissipatedWork+use.DissipatedWork,
            SuppliedWorkError+use.SuppliedWorkError,DissipatedWorkError+use.DissipatedWorkError,ReservedWork+use.ReservedWork);
        if(!double.IsFinite(next.AbsoluteImpulse)||!double.IsFinite(next.SuppliedWork)||
            !double.IsFinite(next.DissipatedWork)||!double.IsFinite(next.SuppliedWorkError)||
            !double.IsFinite(next.DissipatedWorkError)||!double.IsFinite(next.ReservedWork))
            throw new InvalidOperationException("Cumulative motor accounting is not representable.");
        return next;
    }
}

internal sealed class PhysicsMotorBudget
{
    public PhysicsFrameJoint? Joint { get; private set; }
    public PhysicsJointId JointId=>_command.Joint;
    private readonly FrameJointKind _kind;
    private readonly PhysicsMotorCommand _command;
    private readonly double _impulseBudget;
    private double _remainingImpulse,_remainingWork,_supplied,_dissipated,_suppliedError,_dissipatedError;
    public PhysicsMotorBudget(PhysicsFrameJoint joint,PhysicsMotorCommand command,double duration)
    {
        Joint=joint; _kind=joint.Kind; _command=command; _impulseBudget=command.MaximumEffort*duration;
        if(!double.IsFinite(_impulseBudget)) throw new ArgumentException("Motor impulse budget is not representable.");
        _remainingImpulse=_impulseBudget; _remainingWork=command.AvailableWork;
    }
    public void ValidateBinding(PhysicsJoint? joint)
    {
        if(joint is not null&&(joint.Id!=JointId||joint is not PhysicsFrameJoint frame||frame.Kind!=_kind))
            throw new InvalidOperationException("An active motor supply cannot change its joint identity or travel dimension.");
    }
    public void Bind(PhysicsJoint? joint)
    {
        ValidateBinding(joint);
        Joint=(PhysicsFrameJoint?)joint;
    }
    internal static double Spend(double remaining,double cost)
    {
        if(!double.IsFinite(cost)||cost<0||cost>remaining) throw new InvalidOperationException("Motor exceeded its remaining budget.");
        if(cost==0) return remaining;
        var next=remaining-cost;
        // Outward rounding: repeated tiny debits may not leave the supply
        // unchanged or round the remaining allowance upward.
        return next>0?Math.BitDecrement(next):0;
    }
    public MotorPredictionSupply PredictionSupply(double workTolerance)
    {
        if(Joint is null)throw new InvalidOperationException("Detached motor has no prediction supply.");
        return new(new(JointId,_command.TargetSpeed,_command.MaximumEffort,_remainingWork,_command.MaximumPower),_remainingImpulse,workTolerance);
    }
    public void Commit(PredictedMotorUse use)
    {
        if(Joint is null||use.Joint!=JointId)throw new InvalidOperationException("Motor interval identity mismatch.");
        var work=use.Work;
        if(!double.IsFinite(work.Supplied)||work.Supplied<0||!double.IsFinite(work.Dissipated)||work.Dissipated<0||
            !double.IsFinite(work.SuppliedErrorBound)||work.SuppliedErrorBound<0||
            !double.IsFinite(work.DissipatedErrorBound)||work.DissipatedErrorBound<0||
            !double.IsFinite(work.SuppliedPowerUpperBound)||work.SuppliedPowerUpperBound<0||
            work.SuppliedPowerUpperBound>_command.MaximumPower)
            throw new InvalidOperationException("Motor work report is invalid.");
        _remainingImpulse=Spend(_remainingImpulse,use.AbsoluteImpulse);
        _remainingWork=Spend(_remainingWork,work.Supplied+work.SuppliedErrorBound);
        _supplied+=work.Supplied;_dissipated+=work.Dissipated;
        _suppliedError+=work.SuppliedErrorBound;_dissipatedError+=work.DissipatedErrorBound;
        if(!double.IsFinite(_supplied)||!double.IsFinite(_dissipated)||
            !double.IsFinite(_suppliedError)||!double.IsFinite(_dissipatedError))
            throw new InvalidOperationException("Motor work accounting is not representable.");
    }
    public PhysicsMotorUse Report=>new(JointId,_impulseBudget-_remainingImpulse,
        _supplied,_dissipated,_remainingWork,_suppliedError,_dissipatedError,_command.AvailableWork-_remainingWork);
}
