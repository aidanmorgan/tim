using System;
using System.Collections.Generic;

namespace CuriousContraptions.Physics;

/// <summary>Direct coupled solve for up to six bilateral joint equations.
/// Diagonal scaling followed by Cholesky factorization avoids slow scalar-row
/// convergence at offset anchors. Contacts remain coupled through the outer solve.</summary>
public sealed class BilateralConstraintBlock : IImpulseConstraint
{
    public PhysicsBody A { get; }
    public PhysicsBody B { get; }
    private readonly ImpulseConstraint[] _rows;
    private readonly ConstraintMassMatrix _mass;
    private readonly double[] _impulses;
    public BilateralConstraintBlock(IReadOnlyList<ImpulseConstraint> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if(rows.Count<1||rows.Count>6) throw new ArgumentOutOfRangeException(nameof(rows));
        ArgumentNullException.ThrowIfNull(rows[0]);
        A=rows[0].A; B=rows[0].B;
        _rows=new ImpulseConstraint[rows.Count];
        _impulses=new double[rows.Count];
        var jacobians=new ConstraintJacobian[rows.Count]; var diagonal=new double[rows.Count];
        for(var i=0;i<rows.Count;i++)
        {
            var row=rows[i];
            if(row is null||row.A!=A||row.B!=B||row.MinimumImpulse!=double.NegativeInfinity||
                row.MaximumImpulse!=double.PositiveInfinity||row.AccumulatedImpulse!=0)
                throw new ArgumentException("Joint block requires fresh bilateral rows sharing ordered body references.");
            row.ValidatePose();
            _rows[i]=row;
            jacobians[i]=row.Jacobian; diagonal[i]=row.Softness;
        }
        _mass=new(a:A,b:B,jacobians,diagonal);
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
        CollisionVector la=default,aa=default,lb=default,ab=default;
        for(var i=0;i<_rows.Length;i++)
        {
            var j=_rows[i].Jacobian;
            la+=j.LinearA*increment[i]; aa+=j.AngularA*increment[i];
            lb+=j.LinearB*increment[i]; ab+=j.AngularB*increment[i];
            if(!double.IsFinite(_impulses[i]+increment[i])) throw new InvalidOperationException("Joint impulse exceeds numeric range.");
        }
        var va=A.AfterImpulse(la,aa); var vb=B.AfterImpulse(lb,ab);
        A.CommitVelocity(va); B.CommitVelocity(vb);
        for(var i=0;i<_rows.Length;i++) _impulses[i]+=increment[i];
    }
}
