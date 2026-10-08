using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class NonlinearIterationTests
{
    [Fact]
    public void NormalizationCompositionUsesSameEvaluationSamplesAtTinyScale()
    {
        double current=0;
        var contribution=new NormalizationLinearization(1,(u,v)=>
        {
            u[0]=1;
            return(current,1e-12);
        });
        double[] Residual(double[] state)
        {
            current=state[0];
            return[current/Math.Sqrt(current*current+1e-24)-.5];
        }
        var result=NonlinearIteration.Advance([0],Residual,[contribution]);
        Assert.InRange(Math.Abs(result[0]-5e-13),0,1e-27);
        Assert.True(Math.Abs(Residual(result)[0])<.5);
        for(var step=0;step<5;step++)result=NonlinearIteration.Advance(result,Residual,[contribution]);
        Assert.InRange(Math.Abs(Residual(result)[0]),0,1e-14);
    }

    [Fact]
    public void SatisfiedZeroResponseEquationDoesNotInventADirection()
    {
        var result=NonlinearIteration.Advance([0,0],state=>[state[0]-2,0], []);
        Assert.InRange(Math.Abs(result[0]-2),0,1e-10);
        Assert.Equal(0,result[1]);
    }

    [Fact]
    public void InconsistentNearDependentEquationsCannotQualifyAtLeastSquaresMinimum()
    {
        var delta=Math.ScaleB(1,-40);var mismatch=Math.ScaleB(1,-20);
        double[] Residual(double[] state)=>[
            state[0]+state[1]-1,
            state[0]+(1+delta)*state[1]-1,
            2*state[0]+(2+delta)*state[1]-2-mismatch];
        // The third equation's left side equals the sum of the first two;
        // its target differs by mismatch. No state can meet all three.
        // Add an inactive coordinate to retain the complete square system.
        var state=new double[3]; var rejected=false;
        for(var iteration=0;iteration<16;iteration++)
        {
            Assert.True(Residual(state).Max(Math.Abs)>1e-10);
            try {state=NonlinearIteration.Advance(state,Residual, []);}
            catch(InvalidOperationException){rejected=true;break;}
        }
        Assert.True(rejected);
        Assert.True(Residual(state).Max(Math.Abs)>1e-10);
    }

    [Fact]
    public void UnsatisfiedZeroResponseEquationRemainsAnExplicitFailure()
    {
        double[] Residual(double[] state)=>[state[0]-2,Math.ScaleB(1,-20)];
        var state=NonlinearIteration.Advance([0,0],Residual, []);
        Assert.InRange(Math.Abs(state[0]-2),0,1e-10);
        Assert.Equal(Math.ScaleB(1,-20),Residual(state)[1]);
        Assert.ThrowsAny<InvalidOperationException>(()=>NonlinearIteration.Advance(state,Residual, []));
    }

    [Theory]
    [InlineData(1.0,1.0)]
    [InlineData(1048576.0,.0009765625)]
    [InlineData(.0009765625,1048576.0)]
    public void RedundantReactionCoordinatesRetainEveryPhysicalEquation(double rowScale,double effortScale)
    {
        // a-lambda1-lambda2+2=0, with duplicate a=0 constraints.
        double[] Residual(double[] state)=>[
            rowScale*(state[0]-effortScale*state[1]-effortScale*state[2]+2),
            state[0],state[0]];
        var state=new double[3];
        for(var i=0;i<4&&Residual(state).Any(value=>Math.Abs(value)>1e-10);i++)
            state=NonlinearIteration.Advance(state,Residual, []);
        Assert.InRange(Math.Abs(state[0]),0,1e-10);
        Assert.InRange(Math.Abs(effortScale*(state[1]+state[2])-2),0,1e-10);
        Assert.All(Residual(state),value=>Assert.InRange(Math.Abs(value),0,1e-10));
    }

    [Fact]
    public void RepellingFixedPointIsSolvedWithoutPicardIteration()
    {
        // F(x)=4-3x repels direct fixed-point iteration; x-F(x)=4x-4.
        var initial=new[]{10.0};
        var next=NonlinearIteration.Advance(initial,x=>[4*x[0]-4], []);
        Assert.InRange(Math.Abs(next[0]-1),0,1e-9);
        Assert.Equal(10,initial[0]);
    }

    [Fact]
    public void CoupledSystemUsesPivotingAndDecreasesResidual()
    {
        var next=NonlinearIteration.Advance([0,0],x=>[2*x[1]-4,3*x[0]+x[1]-5], []);
        Assert.InRange(Math.Abs(next[0]-1),0,1e-9);
        Assert.InRange(Math.Abs(next[1]-2),0,1e-9);
    }

    [Fact]
    public void OvershootingNewtonDirectionIsBacktrackedUntilItDecreasesMerit()
    {
        static double[] Residual(double[] x)=>[x[0]*x[0]*x[0]-1];
        var value=new[]{.1};
        for(var i=0;i<12;i++)
        {
            var before=Math.Abs(Residual(value)[0]);
            value=NonlinearIteration.Advance(value,Residual, []);
            Assert.True(Math.Abs(Residual(value)[0])<=before);
        }
        Assert.InRange(Math.Abs(value[0]-1),0,1e-12);
    }

    [Fact]
    public void RejectedSearchRetainsImmutableFullAndBestEvaluations()
    {
        var initial=new[]{1e-6};
        var failure=Assert.Throws<NonlinearLineSearchFailure>(()=>
            NonlinearIteration.Advance(initial,x=>[1+x[0]*x[0]], []));
        Assert.Single(failure.FullTrial);Assert.Single(failure.BestTrial);
        Assert.Equal(1+failure.FullTrial[0]*failure.FullTrial[0],Assert.Single(failure.FullResidual));
        Assert.Equal(1+failure.BestTrial[0]*failure.BestTrial[0],Assert.Single(failure.BestResidual));
        Assert.True(failure.FullResidual[0]>failure.BestResidual[0]);
        Assert.Equal(1e-6,initial[0]);
        Assert.Throws<NotSupportedException>(()=>((IList<double>)failure.FullTrial)[0]=0);
        Assert.Equal(initial,failure.State);
        Assert.Equal(1+initial[0]*initial[0],Assert.Single(failure.Residual));
        var jacobian=Assert.Single(Assert.Single(failure.Jacobian));
        var (high,low)=NonlinearIteration.DifferenceSamples(failure.State.ToArray(),0);
        var replay=((1+high[0]*high[0])-(1+low[0]*low[0]))/(high[0]-low[0]);
        Assert.Equal(BitConverter.DoubleToInt64Bits(jacobian),BitConverter.DoubleToInt64Bits(replay));
        Assert.Equal(initial[0],failure.State[0]);
        Assert.Throws<ArgumentOutOfRangeException>(()=>NonlinearIteration.DifferenceSamples(initial,1));
        Assert.Equal(failure.State[0]+failure.Direction[0],failure.FullTrial[0]);
        Assert.InRange(Math.Abs(jacobian*failure.Direction[0]+failure.Residual[0]),0,1e-12);
        Assert.Equal(1,failure.NumericalRank);
        Assert.Equal(Math.ILogB(Math.Abs(jacobian)),Assert.Single(failure.ColumnScaleExponents));
        Assert.Equal(0,Assert.Single(failure.RowScaleExponents));
        Assert.True(failure.RankResolution>0&&double.IsFinite(failure.EquilibratedNorm));
        Assert.Throws<NotSupportedException>(()=>((IList<double>)failure.Jacobian[0])[0]=0);
        Assert.Throws<NotSupportedException>(()=>((IList<int>)failure.ColumnScaleExponents)[0]=0);
        initial[0]=0;Assert.Equal(1e-6,failure.State[0]);
    }

    [Fact]
    public void SingularNonfiniteOrMismatchedProblemsReject()
    {
        Assert.Throws<InvalidOperationException>(()=>NonlinearIteration.Advance([1],_=>[2], []));
        Assert.Throws<InvalidOperationException>(()=>NonlinearIteration.Advance([1],_=>[double.NaN], []));
        Assert.Throws<ArgumentException>(()=>NonlinearIteration.Advance([1],_=>[1,2], []));
        Assert.Throws<ArgumentException>(()=>NonlinearIteration.Advance([double.PositiveInfinity],x=>x, []));
        Assert.Throws<ArgumentNullException>(()=>NonlinearIteration.Advance([1],null!, []));
    }
}
