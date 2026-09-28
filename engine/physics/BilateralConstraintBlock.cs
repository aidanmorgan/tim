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
    private readonly double[,] _factor;
    private readonly double[] _scale,_impulses;
    public BilateralConstraintBlock(IReadOnlyList<ImpulseConstraint> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if(rows.Count<1||rows.Count>6) throw new ArgumentOutOfRangeException(nameof(rows));
        ArgumentNullException.ThrowIfNull(rows[0]);
        A=rows[0].A; B=rows[0].B;
        _rows=new ImpulseConstraint[rows.Count]; _scale=new double[rows.Count];
        _impulses=new double[rows.Count]; _factor=new double[rows.Count,rows.Count];
        for(var i=0;i<rows.Count;i++)
        {
            var row=rows[i];
            if(row is null||row.A!=A||row.B!=B||row.MinimumImpulse!=double.NegativeInfinity||
                row.MaximumImpulse!=double.PositiveInfinity||row.AccumulatedImpulse!=0)
                throw new ArgumentException("Joint block requires fresh bilateral rows sharing ordered body references.");
            row.ValidatePose();
            _rows[i]=row;
            _scale[i]=Math.Sqrt(row.InverseEffectiveMass);
            if(!double.IsFinite(_scale[i])||_scale[i]<=0)
                throw new ArgumentException("Joint block contains an immovable equation.");
        }
        for(var i=0;i<rows.Count;i++)
        for(var j=0;j<=i;j++)
        {
            var value=(Coupling(_rows[i].Jacobian,_rows[j].Jacobian)+(i==j?_rows[i].Softness:0))/_scale[i]/_scale[j];
            for(var k=0;k<j;k++) value-=_factor[i,k]*_factor[j,k];
            if(!double.IsFinite(value)||(i==j&&value<=0))
                throw new ArgumentException("Joint equations must be independent with positive effective mass.");
            _factor[i,j]=i==j?Math.Sqrt(value):value/_factor[j,j];
        }
    }
    private double Coupling(ConstraintJacobian x,ConstraintJacobian y)=>
        A.InverseMass*CollisionVector.Dot(x.LinearA,y.LinearA)+B.InverseMass*CollisionVector.Dot(x.LinearB,y.LinearB)+
        CollisionVector.Dot(x.AngularA,A.InverseInertia(y.AngularA))+CollisionVector.Dot(x.AngularB,B.InverseInertia(y.AngularB));
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
        Span<double> increment=stackalloc double[6];
        for(var i=0;i<_rows.Length;i++)
        {
            var value=Error(i)/_scale[i];
            for(var j=0;j<i;j++) value-=_factor[i,j]*increment[j];
            increment[i]=value/_factor[i,i];
        }
        for(var i=_rows.Length-1;i>=0;i--)
        {
            var value=increment[i];
            for(var j=i+1;j<_rows.Length;j++) value-=_factor[j,i]*increment[j];
            increment[i]=value/_factor[i,i];
        }
        CollisionVector la=default,aa=default,lb=default,ab=default;
        for(var i=0;i<_rows.Length;i++)
        {
            increment[i]/=_scale[i];
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
