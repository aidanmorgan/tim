using System;
using System.Collections.Generic;

namespace CuriousContraptions.Physics;

/// <summary>Scaled Cholesky solve for a coupled Jacobian block. This is algebra
/// only: callers explicitly choose velocity impulses or configuration changes.</summary>
public sealed class ConstraintMassMatrix
{
    private readonly double[,] _factor;
    private readonly double[] _scale;
    public int Count=>_scale.Length;
    public ConstraintMassMatrix(PhysicsBody a,PhysicsBody b,IReadOnlyList<ConstraintJacobian> jacobians,
        IReadOnlyList<double> diagonal)
    {
        ArgumentNullException.ThrowIfNull(a); ArgumentNullException.ThrowIfNull(b);
        ArgumentNullException.ThrowIfNull(jacobians); ArgumentNullException.ThrowIfNull(diagonal);
        if(jacobians.Count<1||jacobians.Count>6||diagonal.Count!=jacobians.Count)
            throw new ArgumentException("A mass block requires one to six matching equations.");
        _scale=new double[jacobians.Count]; _factor=new double[jacobians.Count,jacobians.Count];
        double Coupling(ConstraintJacobian x,ConstraintJacobian y)=>
            a.InverseMass*CollisionVector.Dot(x.LinearA,y.LinearA)+b.InverseMass*CollisionVector.Dot(x.LinearB,y.LinearB)+
            CollisionVector.Dot(x.AngularA,a.InverseInertia(y.AngularA))+CollisionVector.Dot(x.AngularB,b.InverseInertia(y.AngularB));
        for(var i=0;i<Count;i++)
        {
            if(!jacobians[i].IsFinite||!double.IsFinite(diagonal[i])||diagonal[i]<0)
                throw new ArgumentException("Mass equations must be finite with nonnegative compliance.");
            _scale[i]=Math.Sqrt(Coupling(jacobians[i],jacobians[i])+diagonal[i]);
            if(!double.IsFinite(_scale[i])||_scale[i]<=0) throw new ArgumentException("Mass block contains an immovable equation.");
        }
        for(var i=0;i<Count;i++)
        for(var j=0;j<=i;j++)
        {
            var value=(Coupling(jacobians[i],jacobians[j])+(i==j?diagonal[i]:0))/_scale[i]/_scale[j];
            for(var k=0;k<j;k++) value-=_factor[i,k]*_factor[j,k];
            if(!double.IsFinite(value)||(i==j&&value<=0))
                throw new ArgumentException("Mass equations must be independent and positive definite.");
            _factor[i,j]=i==j?Math.Sqrt(value):value/_factor[j,j];
        }
    }
    public void Solve(ReadOnlySpan<double> rhs,Span<double> result)
    {
        if(rhs.Length!=Count||result.Length!=Count) throw new ArgumentException("Mass solve dimensions do not match.");
        for(var i=0;i<Count;i++)
        {
            if(!double.IsFinite(rhs[i])) throw new ArgumentException("Mass right-hand side must be finite.");
            var value=rhs[i]/_scale[i];
            for(var j=0;j<i;j++) value-=_factor[i,j]*result[j];
            result[i]=value/_factor[i,i];
        }
        for(var i=Count-1;i>=0;i--)
        {
            var value=result[i];
            for(var j=i+1;j<Count;j++) value-=_factor[j,i]*result[j];
            result[i]=value/_factor[i,i];
        }
        for(var i=0;i<Count;i++)
        {
            result[i]/=_scale[i];
            if(!double.IsFinite(result[i])) throw new InvalidOperationException("Mass solution exceeds numeric range.");
        }
    }
}
