using System;
using System.Buffers;
using System.Numerics;
using System.Collections.Generic;

namespace CuriousContraptions.Physics;

/// <summary>Diagonally scaled, pivoted Cholesky factorization of a positive
/// semidefinite constraint mass matrix. Dependent rows are retained and checked
/// against every right-hand side; no compliance or regularization is invented.</summary>
public sealed class ConstraintMassMatrix
{
    private const double UnitRoundoff=1.1102230246251565e-16;
    private readonly double[,] _matrix,_factor;
    private readonly double[] _scale;
    private readonly int[] _order,_groups;
    private readonly int _groupCount;
    private readonly double _roundoff;
    public int Count=>_scale.Length;
    public int Rank { get; }
    public ConstraintMassMatrix(IReadOnlyList<ConstraintGradient> jacobians,
        IReadOnlyList<double> diagonal)
    {
        ArgumentNullException.ThrowIfNull(jacobians); ArgumentNullException.ThrowIfNull(diagonal);
        if(jacobians.Count<1||diagonal.Count!=jacobians.Count)
            throw new ArgumentException("A mass block requires a nonempty set of matching equations.");
        _scale=new double[jacobians.Count];
        _matrix=new double[Count,Count];_factor=new double[Count,Count];_order=new int[Count];
        // Backward-error allowance for dot products and Schur updates on the
        // unit-diagonal scaled matrix, not an added physical stiffness.
        _roundoff=32*Count*UnitRoundoff;
        for(var i=0;i<Count;i++)
        {
            if(jacobians[i] is null||!double.IsFinite(diagonal[i])||diagonal[i]<0)
                throw new ArgumentException("Mass equations must be finite with nonnegative compliance.");
            var mass=jacobians[i].Coupling(jacobians[i])+diagonal[i];
            if(!double.IsFinite(mass)||mass<0) throw new ArgumentException("Invalid equation mass.");
            _scale[i]=mass==0?1:Math.Sqrt(mass);
            _order[i]=i;
        }
        for(var i=0;i<Count;i++)
        for(var j=0;j<=i;j++)
        {
            var value=(jacobians[i].Coupling(jacobians[j])+(i==j?diagonal[i]:0))/_scale[i]/_scale[j];
            if(!double.IsFinite(value)) throw new ArgumentException("Mass matrix exceeds numeric range.");
            _matrix[i,j]=_matrix[j,i]=value;
        }
        // Exactly uncoupled matrix components may be normalized independently.
        // Use mass coupling, not body identity: orthogonal coordinates on the
        // same body can be independent, while a transmission joins many bodies.
        _groups=new int[Count];Array.Fill(_groups,-1);
        var pending=new Queue<int>();var groupCount=0;
        for(var seed=0;seed<Count;seed++)
        {
            if(_groups[seed]>=0) continue;
            _groups[seed]=groupCount;pending.Enqueue(seed);
            while(pending.TryDequeue(out var row))
                for(var other=0;other<Count;other++)
                    if(_groups[other]<0&&_matrix[row,other]!=0)
                    {
                        _groups[other]=groupCount;pending.Enqueue(other);
                    }
            groupCount++;
        }
        _groupCount=groupCount;
        double Residual(int i,int j,int columns)
        {
            var value=_matrix[_order[i],_order[j]];
            for(var k=0;k<columns;k++) value-=_factor[i,k]*_factor[j,k];
            return value;
        }
        var rank=0;
        for(var column=0;column<Count;column++)
        {
            var pivot=column;var largest=Residual(column,column,column);
            for(var i=column+1;i<Count;i++)
            {
                var value=Residual(i,i,column);
                if(value>largest) {largest=value;pivot=i;}
            }
            if(largest<=_roundoff)
            {
                for(var i=column;i<Count;i++)
                for(var j=column;j<=i;j++)
                    if(Math.Abs(Residual(i,j,column))>_roundoff)
                        throw new ArgumentException("Mass matrix is not positive semidefinite.");
                break;
            }
            (_order[column],_order[pivot])=(_order[pivot],_order[column]);
            for(var j=0;j<column;j++)
                (_factor[column,j],_factor[pivot,j])=(_factor[pivot,j],_factor[column,j]);
            _factor[column,column]=Math.Sqrt(largest);
            for(var i=column+1;i<Count;i++)
                _factor[i,column]=Residual(i,column,column)/_factor[column,column];
            rank++;
        }
        Rank=rank;
    }
    public void Solve(ReadOnlySpan<double> rhs,Span<double> result)
    {
        if(rhs.Length!=Count||result.Length!=Count) throw new ArgumentException("Mass solve dimensions do not match.");
        var storage=ArrayPool<double>.Shared.Rent(checked(4*Count+Rank+_groupCount));
        int[]? exponents=null;
        try
        {
            exponents=ArrayPool<int>.Shared.Rent(_groupCount);
            SolveUsingScratch(rhs,result,storage.AsSpan(0,4*Count+Rank+_groupCount),
                exponents.AsSpan(0,_groupCount));
        }
        finally
        {
            ArrayPool<double>.Shared.Return(storage);
            if(exponents is not null)ArrayPool<int>.Shared.Return(exponents);
        }
    }
    /// <summary>Largest binary exponent by which a finite nonzero value can be
    /// divided without discarding any significand bit, including subnormals.</summary>
    internal static int MaximumExactDownshift(double value)
    {
        if (!double.IsFinite(value) || value == 0)
            throw new ArgumentOutOfRangeException(nameof(value));
        var bits = BitConverter.DoubleToUInt64Bits(value);
        var exponent = (int)((bits >> 52) & 0x7ff);
        var significand = bits & 0x000fffffffffffffUL;
        if (exponent != 0) significand |= 1UL << 52;
        var lowestBitExponent = (exponent == 0 ? -1074 : exponent - 1075) +
            BitOperations.TrailingZeroCount(significand);
        return lowestBitExponent + 1074;
    }

    private void SolveUsingScratch(ReadOnlySpan<double> rhs,Span<double> result,
        Span<double> storage,Span<int> exponents)
    {
        // Dependent ordered pivots and accumulation buffers must start at zero.
        // Query-scoped numeric storage keeps the factorization concurrently readable.
        storage.Clear();
        var scaled=storage[..Count];
        var solution=storage.Slice(Count,Count);
        var ordered=storage.Slice(2*Count,Count);
        var maximum=storage.Slice(3*Count,_groupCount);
        var factorProduct=storage.Slice(3*Count+_groupCount,Rank);
        var factorMagnitude=storage.Slice(3*Count+_groupCount+Rank,Count);
        for(var i=0;i<Count;i++)
        {
            scaled[i]=rhs[i]/_scale[i];
            if(!double.IsFinite(scaled[i])) throw new ArgumentException("Mass right-hand side must be finite and representable.");
        }
        // Normalize each coupled block toward unit magnitude, constrained by
        // its lowest nonzero significand bit. A large equation must not round
        // a small equation (even a still-nonzero odd subnormal) while scaling.
        // Uncoupled blocks still scale independently.
        for(var i=0;i<Count;i++)
            maximum[_groups[i]]=Math.Max(maximum[_groups[i]],Math.Abs(scaled[i]));
        for(var group=0;group<_groupCount;group++)
            exponents[group]=maximum[group]==0?0:Math.ILogB(maximum[group]);
        for(var i=0;i<Count;i++)
            if(scaled[i]!=0)
                exponents[_groups[i]]=Math.Min(exponents[_groups[i]],MaximumExactDownshift(scaled[i]));
        for(var i=0;i<Count;i++)
        {
            var normalized=Math.ScaleB(scaled[i],-exponents[_groups[i]]);
            if(scaled[i]!=0&&normalized==0)
                throw new InvalidOperationException("Constraint right-hand side dynamic range is not representable.");
            scaled[i]=normalized;
        }
        for(var i=0;i<Rank;i++)
        {
            var value=scaled[_order[i]];
            for(var j=0;j<i;j++) value-=_factor[i,j]*ordered[j];
            ordered[i]=value/_factor[i,i];
        }
        for(var i=Rank-1;i>=0;i--)
        {
            var value=ordered[i];
            for(var j=i+1;j<Rank;j++) value-=_factor[j,i]*ordered[j];
            ordered[i]=value/_factor[i,i];
        }
        for(var i=0;i<Count;i++) solution[_order[i]]=ordered[i];
        // Cholesky backward error depends on |L| |L^T|, not just |A|:
        // cancellation can make an entry of A zero while its factor products
        // remain nonzero. Include that arithmetic scale without adding an
        // absolute tolerance or changing the physical equations.
        for(var k=0;k<Rank;k++)
            for(var j=k;j<Count;j++)
                factorProduct[k]+=Math.Abs(_factor[j,k])*Math.Abs(ordered[j]);
        for(var i=0;i<Count;i++)
            for(var k=0;k<Rank&&k<=i;k++)
                factorMagnitude[_order[i]]+=Math.Abs(_factor[i,k])*factorProduct[k];
        // Check all equations, including discarded dependent pivots, before
        // publishing any answer. Inconsistent constraints are explicit errors.
        for(var i=0;i<Count;i++)
        {
            double actual=0,magnitude=Math.Abs(scaled[i])+factorMagnitude[i];
            for(var j=0;j<Count;j++)
            {
                var term=_matrix[i,j]*solution[j];
                actual+=term;magnitude+=Math.Abs(term);
            }
            if(!double.IsFinite(actual)||!double.IsFinite(magnitude)||
                Math.Abs(actual-scaled[i])>_roundoff*magnitude)
                throw new InvalidOperationException($"Constraint right-hand side is inconsistent or unresolved at working precision: rank {Rank}/{Count}, row {i}, expected {scaled[i]:R}, actual {actual:R}, roundoff bound {_roundoff*magnitude:R}.");
        }
        for(var i=0;i<Count;i++)
        {
            solution[i]=Math.ScaleB(solution[i]/_scale[i],exponents[_groups[i]]);
            if(!double.IsFinite(solution[i])) throw new InvalidOperationException("Mass solution exceeds numeric range.");
        }
        solution.CopyTo(result);
    }
}
