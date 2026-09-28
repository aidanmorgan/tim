using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

/// <summary>Direct coupled solve for up to six bilateral joint equations.
/// Diagonal scaling followed by Cholesky factorization avoids slow scalar-row
/// convergence at offset anchors. Contacts remain coupled through the outer solve.</summary>
public sealed class BilateralConstraintBlock : IImpulseConstraint
{
    // This block owns its accumulated impulses; its rows are solved together.
    public IEnumerable<ImpulseConstraint> ScalarRows=>Array.Empty<ImpulseConstraint>();
    private readonly PhysicsBody[] _bodies;
    public ReadOnlySpan<PhysicsBody> Bodies=>_bodies;
    private readonly ImpulseConstraint[] _rows;
    private readonly ConstraintMassMatrix _mass;
    private readonly double[] _impulses;
    private readonly int[][] _bodyIndices;
    private readonly BodyVelocityUpdate[] _updates;
    public BilateralConstraintBlock(IReadOnlyList<ImpulseConstraint> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if(rows.Count<1||rows.Count>6) throw new ArgumentOutOfRangeException(nameof(rows));
        ArgumentNullException.ThrowIfNull(rows[0]);
        var bodies=new Dictionary<PhysicsBodyId,PhysicsBody>();
        _rows=new ImpulseConstraint[rows.Count];
        _impulses=new double[rows.Count];
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
    }
    private double Error(int index)
    {
        var row=_rows[index];
        var error=row.TargetSpeed-row.Speed-row.Softness*_impulses[index];
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
    public void Solve()
    {
        Span<double> increment=stackalloc double[_rows.Length];
        Span<double> rhs=stackalloc double[_rows.Length];
        for(var i=0;i<_rows.Length;i++) rhs[i]=Error(i);
        _mass.Solve(rhs,increment);
        Span<CollisionVector> linear=new CollisionVector[_bodies.Length];
        Span<CollisionVector> angular=new CollisionVector[_bodies.Length];
        for(var i=0;i<_rows.Length;i++)
        {
            var terms=_rows[i].Gradient.Terms;
            for(var j=0;j<terms.Length;j++)
            {
                var index=_bodyIndices[i][j];
                linear[index]+=terms[j].Linear*increment[i];
                angular[index]+=terms[j].Angular*increment[i];
            }
            if(!double.IsFinite(_impulses[i]+increment[i])) throw new InvalidOperationException("Joint impulse exceeds numeric range.");
        }
        for(var i=0;i<_bodies.Length;i++) _updates[i]=_bodies[i].AfterImpulse(linear[i],angular[i]);
        for(var i=0;i<_bodies.Length;i++) _bodies[i].CommitVelocity(_updates[i]);
        for(var i=0;i<_rows.Length;i++) _impulses[i]+=increment[i];
    }
}
