using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

/// <summary>Finite-work motor actuation along the piecewise admissible velocity
/// path. A new cone response is evaluated at each one-way velocity boundary.</summary>
public static class ConstrainedPoweredImpulse
{
    internal static PoweredImpulseResult Apply(ConstraintGradient applied,double targetSpeed,
        double maximumImpulse,double availableWork,IReadOnlyList<ImpulseResponseConstraint> constraints,double tolerance)
    {
        ArgumentNullException.ThrowIfNull(applied);
        ArgumentNullException.ThrowIfNull(constraints);
        if(!double.IsFinite(targetSpeed)||!double.IsFinite(maximumImpulse)||maximumImpulse<0||
            !double.IsFinite(availableWork)||availableWork<0||!double.IsFinite(tolerance)||tolerance<=0)
            throw new ArgumentException("Motor supply and response tolerance must be finite and valid.");
        foreach(var row in constraints) ArgumentNullException.ThrowIfNull(row);
        var declarations=constraints.ToArray();
        // One-way rows use nonnegative speed internally.
        ConstraintGradient Scale(ConstraintGradient gradient,double scale)=>new(gradient.Terms.ToArray()
            .Select(term=>new ConstraintTerm(term.Body,term.Linear*scale,term.Angular*scale)).ToArray());
        var equalities=declarations.Where(row=>row.Relation==ImpulseResponseRelation.Equal).ToArray();
        var limits=declarations.Where(row=>row.Relation!=ImpulseResponseRelation.Equal)
            .Select(row=>row.Relation==ImpulseResponseRelation.Nonnegative?row.Gradient:Scale(row.Gradient,-1)).ToArray();
        var participants=new ConstraintGradient([..applied.Terms,
            ..declarations.SelectMany(row=>row.Gradient.Terms.ToArray())]).Bodies.ToArray();
        var before=participants.Select(body=>new BodyVelocityUpdate(body.LinearVelocity,body.AngularMomentum)).ToArray();
        double remainingImpulse=maximumImpulse,remainingWork=availableWork,supplied=0,dissipated=0,signedImpulse=0;
        try
        {
            // Velocity feasibility is established by the caller's shared solve.
            foreach(var row in equalities)
                if(Math.Abs(row.Gradient.Speed)>tolerance)
                    throw new ArgumentException("Motor response requires feasible equality velocities.");
            foreach(var limit in limits)
                if(limit.Speed < -tolerance)
                    throw new ArgumentException("Motor response requires feasible one-way velocities.");
            const int maximumSegments=256;
            for(var segment=0;segment<maximumSegments;segment++)
            {
                var difference=targetSpeed-applied.Speed;
                if(!double.IsFinite(difference)) throw new InvalidOperationException("Motor speed difference is not representable.");
                if(difference==0||remainingImpulse==0)
                    return new(signedImpulse,supplied,dissipated);
                var direction=Math.Sign(difference);
                var active=new List<ImpulseResponseConstraint>(equalities);
                var inactive=new List<ConstraintGradient>();
                foreach(var limit in limits)
                    if(limit.Speed<=tolerance) active.Add(new(limit,ImpulseResponseRelation.Nonnegative));
                    else inactive.Add(limit);
                var directed=new AdmissibleImpulseResponse(Scale(applied,direction),active,tolerance).Gradient;
                var response=Scale(directed,direction);
                if(response.Coupling(response)==0)
                    return new(signedImpulse,supplied,dissipated);
                var allowance=remainingImpulse;
                foreach(var limit in inactive)
                {
                    var slope=limit.Coupling(directed);
                    if(slope<0) allowance=Math.Min(allowance,limit.Speed/-slope);
                }
                if(!double.IsFinite(allowance)||allowance<=0)
                    throw new InvalidOperationException("Motor response made no progress to its next velocity boundary.");
                // Measurement and response differ by reaction rows. Preserve the
                // original motor coordinate's target even at rounded boundaries.
                var responseTarget=targetSpeed+response.Speed-applied.Speed;
                var use=PoweredImpulse.Apply(response,responseTarget,allowance,remainingWork);
                var magnitude=Math.Abs(use.Impulse);
                if(magnitude==0) return new(signedImpulse,supplied,dissipated);
                remainingImpulse=PhysicsMotorBudget.Spend(remainingImpulse,magnitude);
                remainingWork=PhysicsMotorBudget.Spend(remainingWork,use.SuppliedWork);
                signedImpulse+=use.Impulse; supplied+=use.SuppliedWork; dissipated+=use.DissipatedWork;
                if(!double.IsFinite(signedImpulse)||!double.IsFinite(supplied)||supplied>availableWork||!double.IsFinite(dissipated))
                    throw new InvalidOperationException("Motor accounting exceeds numeric range.");
                // A target/energy-limited segment did not reach a new cone face.
                if(magnitude<allowance)
                    return new(signedImpulse,supplied,dissipated);
            }
            throw new InvalidOperationException("Motor response did not finish its velocity-boundary path.");
        }
        catch
        {
            for(var i=0;i<participants.Length;i++) participants[i].CommitVelocity(before[i]);
            throw;
        }
    }
}
