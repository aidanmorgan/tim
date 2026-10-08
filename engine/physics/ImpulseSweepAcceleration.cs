using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

/// <summary>Simultaneous reaction natural-map correction of a complete sweep.
/// Numerical rank selects only a proposal. Every original physical residual,
/// bound and friction cone still governs acceptance.</summary>
internal sealed class ImpulseSweepAcceleration
{
    private readonly IReadOnlyList<IImpulseConstraint> _constraints;
    private readonly ImpulseConstraint[] _rows;
    private readonly ContactConstraint[] _contacts;
    private readonly int[] _normalIndices;
    private readonly ConstraintGradient[] _gradients;
    private double[,]? _mass,_jacobian;
    private readonly double[] _raw,_trial,_residual;
    private readonly int[] _rowExponents,_columnExponents;
    private readonly PhysicsBody[] _bodies;
    private readonly int[][] _indices;
    private readonly CollisionVector[] _linear,_angular;
    private readonly BodyVelocityUpdate[] _saved,_updates;
    internal int Factorizations { get; private set; }
    internal int TrialEvaluations { get; private set; }
    internal int MaximumCoordinates { get; private set; }

    internal ImpulseSweepAcceleration(IReadOnlyList<IImpulseConstraint> constraints,
        IReadOnlyList<ImpulseConstraint> rows)
    {
        _constraints=constraints;_rows=rows.ToArray();
        _contacts=constraints.OfType<ContactConstraint>().Distinct().ToArray();
        _gradients=new ConstraintGradient[_rows.Length+2*_contacts.Length];
        _normalIndices=new int[_contacts.Length];
        for(var i=0;i<_rows.Length;i++)_gradients[i]=_rows[i].Gradient;
        for(var i=0;i<_contacts.Length;i++)
        {
            _normalIndices[i]=Array.IndexOf(_rows,_contacts[i].Normal);
            if(_normalIndices[i]<0)throw new ArgumentException("Contact normal is absent from the sweep.");
            _gradients[_rows.Length+2*i]=_contacts[i].TangentGradients.U;
            _gradients[_rows.Length+2*i+1]=_contacts[i].TangentGradients.V;
        }
        var count=_gradients.Length;
        _raw=new double[count];_trial=new double[count];_residual=new double[count];
        _rowExponents=new int[count];_columnExponents=new int[count];
        _bodies=_gradients.SelectMany(g=>g.Bodies.ToArray()).Distinct().OrderBy(b=>b.Id.Index).ToArray();
        var indices=_bodies.Select((body,index)=>(body,index)).ToDictionary(p=>p.body,p=>p.index);
        _indices=_gradients.Select(g=>g.Bodies.ToArray().Select(body=>indices[body]).ToArray()).ToArray();
        _linear=new CollisionVector[_bodies.Length];_angular=new CollisionVector[_bodies.Length];
        _saved=new BodyVelocityUpdate[_bodies.Length];_updates=new BodyVelocityUpdate[_bodies.Length];
    }

    private void Capture()
    {
        for(var i=0;i<_rows.Length;i++)_raw[i]=_rows[i].AccumulatedImpulse;
        for(var i=0;i<_contacts.Length;i++)
        {
            var tangent=_contacts[i].TangentCoordinates;
            _raw[_rows.Length+2*i]=tangent.U;_raw[_rows.Length+2*i+1]=tangent.V;
        }
    }
    private void CommitCoordinates(double[] values)
    {
        for(var i=0;i<_rows.Length;i++)_rows[i].CommitCoupledImpulse(values[i]);
        for(var i=0;i<_contacts.Length;i++)
            _contacts[i].CommitTangentCoordinates(values[_rows.Length+2*i],values[_rows.Length+2*i+1]);
    }

    internal double Complete(double residual)
    {
        Capture();
        if(_mass is null)
        {
            var count=_gradients.Length;
            _mass=new double[count,count];_jacobian=new double[count,count];
            for(var i=0;i<count;i++)
                for(var j=0;j<count;j++)_mass[i,j]=_gradients[i].Coupling(_gradients[j]);
        }
        var jacobian=_jacobian!;
        Array.Clear(jacobian);Array.Clear(_residual);
        for(var i=0;i<_rows.Length;i++)
            _residual[i]=_rows[i].LinearizeReaction(_mass,jacobian,i);
        for(var i=0;i<_contacts.Length;i++)
            _contacts[i].LinearizeTangents(_mass,jacobian,_residual,_normalIndices[i],_rows.Length+2*i);
        var direction=NewtonDirection.Solve(jacobian,_residual,_rowExponents,_columnExponents,out _);
        Factorizations=checked(Factorizations+1);MaximumCoordinates=_raw.Length;
        double fraction=1;
        for(var search=0;search<32;search++,fraction*=.5)
        {
            for(var i=0;i<_trial.Length;i++)
            {
                _trial[i]=_raw[i]+fraction*direction[i];
                if(!double.IsFinite(_trial[i]))throw new InvalidOperationException("Reaction proposal exceeds numeric range.");
            }
            TrialEvaluations=checked(TrialEvaluations+1);
            var improved=EvaluateProposal(residual);
            if(improved<residual)return improved;
        }
        return residual;
    }

    private double EvaluateProposal(double residual)
    {
        for(var i=0;i<_rows.Length;i++)
            _trial[i]=Math.Clamp(_trial[i],_rows[i].MinimumImpulse,_rows[i].MaximumImpulse);
        for(var i=0;i<_contacts.Length;i++)
        {
            var index=_rows.Length+2*i;
            var tangent=_contacts[i].ProjectTangentCoordinates(_trial[_normalIndices[i]],_trial[index],_trial[index+1]);
            _trial[index]=tangent.U;_trial[index+1]=tangent.V;
        }
        Array.Clear(_linear);Array.Clear(_angular);
        for(var i=0;i<_gradients.Length;i++)
        {
            // Apply the representable committed difference, not an unrounded
            // Newton increment that no longer matches the stored effort.
            var change=_trial[i]-_raw[i];var terms=_gradients[i].Terms;
            for(var j=0;j<terms.Length;j++)
            {
                var index=_indices[i][j];
                _linear[index]+=terms[j].Linear*change;
                _angular[index]+=terms[j].Angular*change;
            }
        }
        for(var i=0;i<_bodies.Length;i++)
        {
            _saved[i]=new(_bodies[i].LinearVelocity,_bodies[i].AngularMomentum);
            _updates[i]=_bodies[i].AfterImpulse(_linear[i],_angular[i]);
        }
        var accepted=false;
        try
        {
            for(var i=0;i<_bodies.Length;i++)_bodies[i].CommitVelocity(_updates[i]);
            CommitCoordinates(_trial);
            double trialResidual=0;
            foreach(var constraint in _constraints)trialResidual=Math.Max(trialResidual,constraint.Residual);
            accepted=trialResidual<residual;
            return accepted?trialResidual:residual;
        }
        finally
        {
            if(!accepted)
            {
                for(var i=0;i<_bodies.Length;i++)_bodies[i].CommitVelocity(_saved[i]);
                CommitCoordinates(_raw);
            }
        }
    }
}
