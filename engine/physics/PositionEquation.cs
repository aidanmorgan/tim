using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

public readonly record struct PositionEquation(ConstraintGradient Gradient,double Error);

/// <summary>Shared mass-weighted configuration correction. It changes no physical
/// velocity and commits all participants through the collision-checked projector.</summary>
public static class PositionEquations
{
    public static void Project(ReadOnlySpan<PositionEquation> equations,PositionProjector projector)
    {
        ArgumentNullException.ThrowIfNull(projector);
        if(equations.Length==0) return;
        var gradients=new ConstraintGradient[equations.Length]; var rhs=new double[equations.Length];
        for(var i=0;i<equations.Length;i++)
        {
            var equation=equations[i]; ArgumentNullException.ThrowIfNull(equation.Gradient);
            if(!double.IsFinite(equation.Error)) throw new ArgumentException("Position error must be finite.");
            foreach(var body in equation.Gradient.Bodies) projector.ValidateBody(body);
            if(equation.Gradient.Coupling(equation.Gradient)<=0)
                throw new InvalidOperationException("An immovable equation cannot correct configuration.");
            gradients[i]=equation.Gradient; rhs[i]=-equation.Error;
        }
        var matrix=new ConstraintMassMatrix(gradients,new double[equations.Length]); var result=new double[equations.Length];
        matrix.Solve(rhs,result);
        var terms=new List<ConstraintTerm>();
        for(var i=0;i<equations.Length;i++)
            foreach(var term in gradients[i].Terms)
                terms.Add(new(term.Body,term.Linear*result[i],term.Angular*result[i]));
        var combined=new ConstraintGradient(terms.ToArray());
        var corrections=combined.Terms.ToArray().Select(t=>
            new BodyCorrection(t.Body,t.Linear*t.Body.InverseMass,t.Body.InverseInertia(t.Angular))).ToArray();
        projector.Apply(corrections);
    }
}
