using System;
using System.Collections.Generic;

namespace CuriousContraptions.Physics;

/// <summary>Immutable rejected line-search states and their evaluated residuals.</summary>
public sealed class NonlinearLineSearchFailure : InvalidOperationException
{
    public IReadOnlyList<double> FullTrial { get; }
    public IReadOnlyList<double> FullResidual { get; }
    public IReadOnlyList<double> BestTrial { get; }
    public IReadOnlyList<double> BestResidual { get; }
    public IReadOnlyList<double> State { get; }
    public IReadOnlyList<double> Residual { get; }
    public IReadOnlyList<double> Direction { get; }
    public IReadOnlyList<IReadOnlyList<double>> Jacobian { get; }
    public IReadOnlyList<int> RowScaleExponents { get; }
    public IReadOnlyList<int> ColumnScaleExponents { get; }
    public int NumericalRank { get; }
    public double EquilibratedNorm { get; }
    public double RankResolution { get; }
    internal NonlinearLineSearchFailure(string message, double[] fullTrial, double[] fullResidual,
        double[] bestTrial, double[] bestResidual,double[] state,double[] residual,double[,] jacobian,
        double[] direction,ReadOnlySpan<int> rowExponents,ReadOnlySpan<int> columnExponents,
        NewtonDirectionStatistics statistics) : base(message)
    {
        State=Array.AsReadOnly((double[])state.Clone());Residual=Array.AsReadOnly((double[])residual.Clone());
        Direction=Array.AsReadOnly((double[])direction.Clone());
        var rows=new IReadOnlyList<double>[jacobian.GetLength(0)];
        for(var i=0;i<rows.Length;i++)
        {
            var row=new double[jacobian.GetLength(1)];
            for(var j=0;j<row.Length;j++)row[j]=jacobian[i,j];
            rows[i]=Array.AsReadOnly(row);
        }
        Jacobian=Array.AsReadOnly(rows);
        RowScaleExponents=Array.AsReadOnly(rowExponents.ToArray());
        ColumnScaleExponents=Array.AsReadOnly(columnExponents.ToArray());
        NumericalRank=statistics.Rank;EquilibratedNorm=statistics.EquilibratedNorm;RankResolution=statistics.RankResolution;
        FullTrial=Array.AsReadOnly((double[])fullTrial.Clone());
        FullResidual=Array.AsReadOnly((double[])fullResidual.Clone());
        BestTrial=Array.AsReadOnly((double[])bestTrial.Clone());
        BestResidual=Array.AsReadOnly((double[])bestResidual.Clone());
    }
}

/// <summary>One globalized Newton iteration for a finite nonlinear residual.
/// Central differences form the local Jacobian; complete orthogonal reduction
/// solves its least-squares direction, including redundant reaction coordinates.
/// Armijo accepts only original residual decrease. Unresolved, nonfinite and
/// nondecreasing problems reject. No physical state is committed here.</summary>
public static class NonlinearIteration
{
    private const double DifferenceScale=6.055454452393343e-6; // cbrt(binary64 machine epsilon)
    private const double SufficientDecrease=1e-4;
    private const int MaximumLineSearch=32;

    internal static (double[] High,double[] Low) DifferenceSamples(ReadOnlySpan<double> state,int column)
    {
        if((uint)column>=(uint)state.Length)throw new ArgumentOutOfRangeException(nameof(column));
        var h=DifferenceScale*Math.Max(1,Math.Abs(state[column]));
        var high=state.ToArray();var low=state.ToArray();
        high[column]+=h;low[column]-=h;
        return(high,low);
    }

    public static double[] Advance(ReadOnlySpan<double> source,Func<double[],double[]> residual,
        ReadOnlySpan<NormalizationLinearization> normalizations)
    {
        ArgumentNullException.ThrowIfNull(residual);
        var x=source.ToArray(); var count=x.Length;
        foreach(var contribution in normalizations)
        {
            ArgumentNullException.ThrowIfNull(contribution);
            if(contribution.Dimension!=count)
                throw new ArgumentException("Normalization contribution dimension must match the state.");
        }
        foreach(var value in x)
            if(!double.IsFinite(value)) throw new ArgumentException("Nonlinear state must be finite.");
        double[] Evaluate(double[] state)
        {
            var result=residual(state);
            if(result is null||result.Length!=count) throw new ArgumentException("Residual dimension must match its state.");
            foreach(var value in result)
                if(!double.IsFinite(value)) throw new InvalidOperationException("Nonlinear residual is not finite.");
            return result;
        }
        static double Merit(double[] values)
        {
            double sum=0;
            foreach(var value in values) sum+=value*value;
            if(!double.IsFinite(sum)) throw new InvalidOperationException("Nonlinear residual norm exceeds numeric range.");
            return sum;
        }
        var initial=Evaluate(x); var merit=Merit(initial);
        if(merit==0) return x;
        foreach(var contribution in normalizations)contribution.CaptureCenter();
        var matrix=new double[count,count];
        for(var column=0;column<count;column++)
        {
            var (plus,minus)=DifferenceSamples(x,column);
            var high=Evaluate(plus);
            foreach(var contribution in normalizations)contribution.CaptureHigh();
            var low=Evaluate(minus);
            foreach(var contribution in normalizations)contribution.CaptureLow();
            var width=plus[column]-minus[column];
            for(var row=0;row<count;row++) matrix[row,column]=(high[row]-low[row])/width;
            foreach(var contribution in normalizations)contribution.CorrectColumn(matrix,column,width);
        }
        // Ephemeral scaling metadata is copied only if this exact solve fails.
        Span<int> rowExponents=stackalloc int[count];
        Span<int> columnExponents=stackalloc int[count];
        var step=NewtonDirection.Solve(matrix,initial,rowExponents,columnExponents,out var statistics);
        double scale=1; var bestMerit=double.PositiveInfinity;
        double[] fullTrial=[],fullResidual=[],bestTrial=[],bestResidual=[];
        for(var search=0;search<MaximumLineSearch;search++,scale*=.5)
        {
            var trial=new double[count];
            for(var i=0;i<count;i++)
            {
                trial[i]=x[i]+scale*step[i];
                if(!double.IsFinite(trial[i])) throw new InvalidOperationException("Nonlinear trial exceeds numeric range.");
            }
            var trialResidual=Evaluate(trial);
            var trialMerit=Merit(trialResidual);
            if(search==0){fullTrial=trial;fullResidual=trialResidual;}
            if(trialMerit<bestMerit){bestMerit=trialMerit;bestTrial=trial;bestResidual=trialResidual;}
            if(trialMerit<=(1-2*SufficientDecrease*scale)*merit) return trial;
        }
        throw new NonlinearLineSearchFailure($"Nonlinear line search could not establish residual decrease: merit {merit:R}, best {bestMerit:R}, step norm {Math.Sqrt(Merit(step)):R}.",fullTrial,fullResidual,bestTrial,bestResidual,x,initial,matrix,step,rowExponents,columnExponents,statistics);
    }
}
