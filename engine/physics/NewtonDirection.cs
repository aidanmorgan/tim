using System;

namespace CuriousContraptions.Physics;

/// <summary>Minimum-norm Newton direction in equilibrated coordinates. Two
/// orthogonal factorizations retain dependent equations without forming normal
/// equations. Numerical rank selects a trial direction, never root acceptance.</summary>
internal readonly record struct NewtonDirectionStatistics(int Rank,double EquilibratedNorm,double RankResolution);

internal static class NewtonDirection
{
    private const double UnitRoundoff=1.1102230246251565e-16;

    internal static double[] Solve(double[,] jacobian,ReadOnlySpan<double> residual,
        Span<int> rowExponents,Span<int> columnExponents,out NewtonDirectionStatistics statistics)
    {
        statistics=default;
        var count=residual.Length;
        if(jacobian.GetLength(0)!=count||jacobian.GetLength(1)!=count||rowExponents.Length!=count||columnExponents.Length!=count)
            throw new ArgumentException("Newton equations require matching dimensions.");
        var matrix=(double[,])jacobian.Clone();
        var rhs=new double[count]; var order=new int[count];
        // Powers of two nondimensionalize each coordinate, then each equation.
        // The minimum norm is in these coordinates; physical residual acceptance
        // remains in the caller's original units.
        for(var column=0;column<count;column++)
        {
            double largest=0;
            for(var row=0;row<count;row++)
            {
                var value=matrix[row,column];
                if(!double.IsFinite(value))throw new InvalidOperationException("Nonlinear Jacobian is not finite.");
                largest=Math.Max(largest,Math.Abs(value));
            }
            order[column]=column;
            columnExponents[column]=largest==0?0:Math.ILogB(largest);
            for(var row=0;row<count;row++)matrix[row,column]=Math.ScaleB(matrix[row,column],-columnExponents[column]);
        }
        for(var row=0;row<count;row++)
        {
            double largest=0;
            for(var column=0;column<count;column++)largest=Math.Max(largest,Math.Abs(matrix[row,column]));
            var exponent=largest==0?0:Math.ILogB(largest);rowExponents[row]=exponent;
            for(var column=0;column<count;column++)matrix[row,column]=Math.ScaleB(matrix[row,column],-exponent);
            rhs[row]=Math.ScaleB(-residual[row],-exponent);
            if(!double.IsFinite(rhs[row]))throw new InvalidOperationException("Scaled nonlinear residual exceeds numeric range.");
        }
        double norm=0;
        for(var column=0;column<count;column++)norm=Hypot(norm,ColumnNorm(matrix,column,0));
        // Dimension-dependent numerical step-rank policy, expressed using a
        // gamma-shaped roundoff scale for both orthogonal reductions. This is
        // not a certified normwise error bound or physical rank criterion;
        // every original residual remains authoritative for root acceptance.
        var operations=16.0*count*count;
        var accumulated=operations*UnitRoundoff;
        if(accumulated>=1)throw new InvalidOperationException("Newton factorization exceeds arithmetic resolution.");
        var resolution=norm*accumulated/(1-accumulated);
        var vector=new double[count]; var rank=0;
        for(var column=0;column<count;column++)
        {
            var pivot=column; var largest=ColumnNorm(matrix,column,column);
            for(var other=column+1;other<count;other++)
            {
                var magnitude=ColumnNorm(matrix,other,column);
                if(magnitude>largest){largest=magnitude;pivot=other;}
            }
            if(largest<=resolution)break;
            if(pivot!=column)
            {
                for(var row=0;row<count;row++)
                    (matrix[row,column],matrix[row,pivot])=(matrix[row,pivot],matrix[row,column]);
                (order[column],order[pivot])=(order[pivot],order[column]);
            }
            Reflect(matrix,column,column,vector);
            double dot=0;
            for(var row=column;row<count;row++)dot+=vector[row]*rhs[row];
            for(var row=column;row<count;row++)rhs[row]-=2*vector[row]*dot;
            rank++;
        }
        statistics=new(rank,norm,resolution);
        if(rank==0)throw new InvalidOperationException("Nonlinear Jacobian has no resolved direction.");
        // R_top^T = Z [T;0]. Solving T^T y = Q^T(-r), followed by
        // z=Z[y;0], gives the minimum-norm least-squares direction.
        var transpose=new double[count,rank]; var reflectors=new double[count,rank];
        for(var row=0;row<count;row++)
        for(var column=0;column<rank;column++)transpose[row,column]=matrix[column,row];
        for(var column=0;column<rank;column++)
        {
            Reflect(transpose,column,column,vector);
            for(var row=column;row<count;row++)reflectors[row,column]=vector[row];
        }
        var coordinates=new double[count];
        for(var row=0;row<rank;row++)
        {
            var value=rhs[row];
            for(var column=0;column<row;column++)value-=transpose[column,row]*coordinates[column];
            coordinates[row]=value/transpose[row,row];
            if(!double.IsFinite(coordinates[row]))throw new InvalidOperationException("Nonlinear direction exceeds numeric range.");
        }
        for(var column=rank-1;column>=0;column--)
        {
            double dot=0;
            for(var row=column;row<count;row++)dot+=reflectors[row,column]*coordinates[row];
            for(var row=column;row<count;row++)coordinates[row]-=2*reflectors[row,column]*dot;
        }
        var result=new double[count];
        for(var column=0;column<count;column++)
        {
            var value=Math.ScaleB(coordinates[column],-columnExponents[order[column]]);
            if(!double.IsFinite(value))throw new InvalidOperationException("Nonlinear direction exceeds numeric range.");
            result[order[column]]=value;
        }
        return result;
    }

    private static double Hypot(double a,double b)
    {
        var scale=Math.Max(Math.Abs(a),Math.Abs(b));
        if(scale==0)return 0;
        return scale*Math.Sqrt((a/scale)*(a/scale)+(b/scale)*(b/scale));
    }
    private static double ColumnNorm(double[,] matrix,int column,int start)
    {
        double norm=0;
        for(var row=start;row<matrix.GetLength(0);row++)norm=Hypot(norm,matrix[row,column]);
        return norm;
    }
    private static void Reflect(double[,] matrix,int column,int start,double[] vector)
    {
        var count=matrix.GetLength(0); var norm=ColumnNorm(matrix,column,start);
        if(!double.IsFinite(norm)||norm==0)throw new InvalidOperationException("Orthogonal factorization lost a resolved direction.");
        var sign=matrix[start,column]>=0?1.0:-1.0;
        for(var row=start;row<count;row++)vector[row]=matrix[row,column]/norm;
        vector[start]+=sign;
        double length=0;
        for(var row=start;row<count;row++)length=Hypot(length,vector[row]);
        for(var row=start;row<count;row++)vector[row]/=length;
        for(var other=column;other<matrix.GetLength(1);other++)
        {
            double dot=0;
            for(var row=start;row<count;row++)dot+=vector[row]*matrix[row,other];
            for(var row=start;row<count;row++)matrix[row,other]-=2*vector[row]*dot;
        }
        matrix[start,column]=-sign*norm;
        for(var row=start+1;row<count;row++)matrix[row,column]=0;
    }
}
