using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

/// <summary>One pure prediction's remaining motor allowance; never an owned source.</summary>
public sealed record MotorPredictionSupply
{
    public PhysicsMotorCommand Command { get; }
    public double MaximumImpulse { get; }
    public double WorkTolerance { get; }
    public MotorPredictionSupply(PhysicsMotorCommand command,double maximumImpulse,double workTolerance)
    {
        if(!double.IsFinite(maximumImpulse)||maximumImpulse<0||
            !double.IsFinite(workTolerance)||workTolerance<=0)throw new ArgumentException("Invalid prediction motor allowance.");
        Command=command;MaximumImpulse=maximumImpulse;WorkTolerance=workTolerance;
    }
}
public readonly record struct PredictedMotorUse(PhysicsJointId Joint,double Effort,double AbsoluteImpulse,
    WrenchPathWorkResult Work);

internal sealed class MotorPredictionDrive
{
    internal MotorPredictionSupply Supply { get; }
    private readonly PhysicsFrameJoint _joint;
    private readonly double _startSpeed;
    internal PhysicsDriveId Id=>new(_joint.Id.Index);
    internal MotorPredictionDrive(MotorPredictionSupply supply,IReadOnlyList<PhysicsJoint> joints)
    {
        Supply=supply;
        var matches=joints.Where(joint=>joint.Id==supply.Command.Joint).ToArray();
        if(matches.Length!=1||matches[0] is not PhysicsFrameJoint joint||
            joint.Kind is not (FrameJointKind.Slider or FrameJointKind.Hinge))
            throw new ArgumentException("Motor prediction requires one owned axial joint.");
        _joint=joint;_startSpeed=joint.Motion.Speed;
    }
    private WrenchPathWorkResult Work(ConstraintGradient gradient,IReadOnlyDictionary<PhysicsBodyId,BodyTrajectory> paths,
        double duration,double effort)
    {
        var participants=new List<WrenchPathTerm>();
        foreach(var term in gradient.Terms)
        {
            var body=term.Body.Id==_joint.A.Id?_joint.A:term.Body.Id==_joint.B.Id?_joint.B:
                throw new ArgumentException("Foreign motor work participant.");
            participants.Add(new(body,paths[body.Id],new(term.Linear*effort,term.Angular*effort)));
        }
        return WrenchPathWork.Measure(participants,duration,Supply.WorkTolerance/Math.Max(1,Supply.Command.MaximumEffort),0);
    }
    internal AccelerationDrive Prepare(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> sample,
        IReadOnlyDictionary<PhysicsBodyId,BodyTrajectory> paths,double duration)
    {
        var stage=(PhysicsFrameJoint)_joint.Rebind(sample);var travel=stage.Travel;
        var gradient=travel.Jacobian.Bind(stage.A,stage.B);
        var unit=Work(gradient,paths,duration,1);
        // The accepted effort is integrated back into an impulse. Round the
        // quotient inward so that multiplication cannot exceed the allowance.
        static double Quotient(double budget,double cost)
        {
            var value=budget/cost;
            return value>0?Math.Max(0,Math.BitDecrement(value)):0;
        }
        var maximum=Math.Min(Supply.Command.MaximumEffort,Quotient(Supply.MaximumImpulse,duration));
        double Bound(double supplied,double dissipated,double error,double peak)
        {
            var upper=supplied+error;
            if(!double.IsFinite(upper))throw new InvalidOperationException("Motor work bound exceeds numeric range.");
            var workLimit=upper>0?Quotient(Supply.Command.AvailableWork,upper):
                (Supply.Command.AvailableWork>0||dissipated>0||Supply.Command.TargetSpeed==_startSpeed?maximum:0);
            var powerLimit=peak>0?Quotient(Supply.Command.MaximumPower,peak):
                (Supply.Command.MaximumPower>0||dissipated>0||Supply.Command.TargetSpeed==_startSpeed?maximum:0);
            return Math.Min(maximum,Math.Min(workLimit,powerLimit));
        }
        var acceleration=(Supply.Command.TargetSpeed-_startSpeed)/duration;
        if((Supply.Command.AvailableWork==0||Supply.Command.MaximumPower==0)&&Supply.Command.TargetSpeed==0&&_startSpeed!=0)
        {
            // Round the stop acceleration toward the admissible side. A nearest
            // quotient can cross zero by one ulp and falsely require supply.
            bool Reversed(double value)=>_startSpeed>0?value<0:value>0;
            for(var correction=0;Reversed(_startSpeed+acceleration*duration)||
                Reversed(Math.FusedMultiplyAdd(acceleration,duration,_startSpeed));correction++)
            {
                if(correction==4)throw new InvalidOperationException("Braking endpoint cannot be represented without reversal.");
                acceleration=_startSpeed>0?Math.BitIncrement(acceleration):Math.BitDecrement(acceleration);
            }
        }
        return new(Id,gradient,travel.ConvectiveAcceleration,acceleration,
            -Bound(unit.Dissipated,unit.Supplied,unit.DissipatedErrorBound,unit.DissipatedPowerUpperBound),
            Bound(unit.Supplied,unit.Dissipated,unit.SuppliedErrorBound,unit.SuppliedPowerUpperBound));
    }
    internal PredictedMotorUse Report(AccelerationDrive row,AccelerationDriveResult result,
        IReadOnlyDictionary<PhysicsBodyId,BodyTrajectory> paths,double duration)
    {
        if(row.Id!=Id||result.Id!=Id)throw new ArgumentException("Motor prediction identity mismatch.");
        var unit=Work(row.Gradient,paths,duration,1);var magnitude=Math.Abs(result.Effort);
        var work=new WrenchPathWorkResult(
            (result.Effort>=0?unit.Supplied:unit.Dissipated)*magnitude,
            (result.Effort>=0?unit.Dissipated:unit.Supplied)*magnitude,(result.Effort>=0?unit.SuppliedErrorBound:unit.DissipatedErrorBound)*magnitude,
            (result.Effort>=0?unit.DissipatedErrorBound:unit.SuppliedErrorBound)*magnitude,
            (result.Effort>=0?unit.SuppliedPowerUpperBound:unit.DissipatedPowerUpperBound)*magnitude,
            (result.Effort>=0?unit.DissipatedPowerUpperBound:unit.SuppliedPowerUpperBound)*magnitude);
        var impulse=Math.Abs(result.Effort)*duration;
        if(impulse>Supply.MaximumImpulse||work.Supplied+work.SuppliedErrorBound>Supply.Command.AvailableWork||
            !double.IsFinite(work.SuppliedPowerUpperBound)||work.SuppliedPowerUpperBound>Supply.Command.MaximumPower)
            throw new InvalidOperationException($"Converged motor prediction exceeded its source allowance: impulse {impulse:R}/{Supply.MaximumImpulse:R}, work {work.Supplied:R}+{work.SuppliedErrorBound:R}/{Supply.Command.AvailableWork:R}, power {work.SuppliedPowerUpperBound:R}/{Supply.Command.MaximumPower:R}, effort {result.Effort:R}, duration {duration:R}.");
        return new(_joint.Id,result.Effort,impulse,work);
    }
}
