using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

/// <summary>Direct coupled solve for an arbitrary bilateral equation block.
/// Scaled rank-revealing factorization avoids slow scalar-row
/// convergence at offset anchors. Contacts remain coupled through the outer solve.</summary>
public sealed class BilateralConstraintBlock : IImpulseConstraint
{
    // Shared row state permits coupled contact/joint corrections between block solves.
    public IEnumerable<ImpulseConstraint> ScalarRows=>_rows;
    private readonly PhysicsBody[] _bodies;
    public ReadOnlySpan<PhysicsBody> Bodies=>_bodies;
    private readonly ImpulseConstraint[] _rows;
    private readonly ConstraintMassMatrix _mass;
    private readonly int[][] _bodyIndices;
    private readonly BodyVelocityUpdate[] _updates;
    private readonly double[] _increment,_rhs;
    private readonly CollisionVector[] _linear,_angular;
    public BilateralConstraintBlock(IReadOnlyList<ImpulseConstraint> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if(rows.Count<1) throw new ArgumentOutOfRangeException(nameof(rows));
        ArgumentNullException.ThrowIfNull(rows[0]);
        var bodies=new Dictionary<PhysicsBodyId,PhysicsBody>();
        _rows=new ImpulseConstraint[rows.Count];
        var jacobians=new ConstraintGradient[rows.Count]; var diagonal=new double[rows.Count];
        for(var i=0;i<rows.Count;i++)
        {
            var row=rows[i];
            if(row is null||row.MinimumImpulse!=double.NegativeInfinity||
                row.MaximumImpulse!=double.PositiveInfinity||row.AccumulatedImpulse!=0)
                throw new ArgumentException("Joint block requires fresh bilateral rows.");
            row.ValidatePose();
            _rows[i]=row;
            jacobians[i]=row.Gradient; diagonal[i]=row.Softness;
            foreach(var body in row.Bodies)
                if(bodies.TryGetValue(body.Id,out var prior)&&prior!=body)
                    throw new ArgumentException("Body identity refers to multiple states.");
                else bodies[body.Id]=body;
        }
        _bodies=bodies.Values.OrderBy(b=>b.Id.Index).ToArray();
        _mass=new(jacobians,diagonal);
        var indices=_bodies.Select((body,index)=>(body.Id,index)).ToDictionary(p=>p.Id,p=>p.index);
        _bodyIndices=_rows.Select(r=>r.Gradient.Terms.ToArray().Select(t=>indices[t.Body.Id]).ToArray()).ToArray();
        _updates=new BodyVelocityUpdate[_bodies.Length];
        _increment=new double[_rows.Length];_rhs=new double[_rows.Length];
        _linear=new CollisionVector[_bodies.Length];_angular=new CollisionVector[_bodies.Length];
    }
    private double Error(int index)
    {
        var row=_rows[index];
        var error=row.TargetSpeed-row.Speed-row.Softness*row.AccumulatedImpulse;
        if(!double.IsFinite(error)) throw new InvalidOperationException("Joint residual exceeds numeric range.");
        return error;
    }
    public double Residual
    {
        get
        {
            double result=0;
            for(var i=0;i<_rows.Length;i++) result=Math.Max(result,Math.Abs(Error(i)));
            return result;
        }
    }
    void IImpulseConstraint.Solve()=>Solve();
    internal void Solve()
    {
        for(var i=0;i<_rows.Length;i++) _rhs[i]=Error(i);
        _mass.Solve(_rhs,_increment);
        // This mutable block owns its single-thread solve workspace. Clear both
        // accumulation channels on every attempt, including retries after failure.
        Array.Clear(_linear);Array.Clear(_angular);
        for(var i=0;i<_rows.Length;i++)
        {
            var terms=_rows[i].Gradient.Terms;
            for(var j=0;j<terms.Length;j++)
            {
                var index=_bodyIndices[i][j];
                _linear[index]+=terms[j].Linear*_increment[i];
                _angular[index]+=terms[j].Angular*_increment[i];
            }
            if(!double.IsFinite(_rows[i].AccumulatedImpulse+_increment[i])) throw new InvalidOperationException("Joint impulse exceeds numeric range.");
        }
        for(var i=0;i<_bodies.Length;i++) _updates[i]=_bodies[i].AfterImpulse(_linear[i],_angular[i]);
        for(var i=0;i<_bodies.Length;i++) _bodies[i].CommitVelocity(_updates[i]);
        for(var i=0;i<_rows.Length;i++)
            _rows[i].CommitCoupledImpulse(_rows[i].AccumulatedImpulse+_increment[i]);
    }
}
