using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class NormalizationLinearizationTests
{
    [Theory]
    [InlineData(0.0)]
    [InlineData(3.7)]
    public void AlgebraicallyZeroCorrectionPreservesExistingSecantExactly(double coefficient)
    {
        var sample=new NormalizationLinearization(1,(u,v)=>
        {
            u[0]=coefficient;v[0]=-coefficient;
            return(.7,0.0);
        });
        sample.CaptureCenter();sample.CaptureHigh();sample.CaptureLow();
        var matrix=new double[,]{{.1}};
        sample.CorrectColumn(matrix,0,.3);
        Assert.Equal(BitConverter.DoubleToInt64Bits(.1),BitConverter.DoubleToInt64Bits(matrix[0,0]));
    }

    [Fact]
    public void FiniteDerivativeSurvivesOverflowingUnscaledSampleDifference()
    {
        double x=0;
        var sample=new NormalizationLinearization(1,(u,v)=>
        {
            u[0]=1;
            return(x,1.0);
        });
        sample.CaptureCenter();
        x=1e308;sample.CaptureHigh();
        x=-1e308;sample.CaptureLow();
        var matrix=new double[,]{{1}};
        sample.CorrectColumn(matrix,0,2);
        Assert.Equal(1e308,matrix[0,0]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SubnormalVectorDoesNotOverflowBeforeCoefficientWeighting(bool weighted)
    {
        double q0=double.Epsilon,q1=0;
        var sample=new NormalizationLinearization(1,(u,v)=>
        {
            v[0]=weighted?double.Epsilon:0;
            return(q0,q1);
        });
        sample.CaptureCenter();
        q0=1;q1=1;sample.CaptureHigh();
        q1=-1;sample.CaptureLow();
        var matrix=new double[,]{{0}};
        sample.CorrectColumn(matrix,0,2);
        Assert.Equal(weighted?1:0,matrix[0,0]);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(1e-12)]
    public void FullProductRuleRetainsChangingCoefficientsAndSmoothResidual(double magnitude)
    {
        double x = 0;
        // q=m*(3+x,4-2x), A=(2+3x,-1+5x), smooth residual=7x.
        var sample = new NormalizationLinearization(1, (u,v) =>
        {
            u[0]=2+3*x; v[0]=-1+5*x;
            return (magnitude*(3+x),magnitude*(4-2*x));
        });
        double Value()
        {
            var q0=3+x; var q1=4-2*x;
            return 7*x+((2+3*x)*q0+(-1+5*x)*q1)/Math.Sqrt(q0*q0+q1*q1);
        }
        sample.CaptureCenter();
        x=.2; sample.CaptureHigh(); var high=Value();
        x=-.2; sample.CaptureLow(); var low=Value();
        var matrix=new double[,] {{(high-low)/.4}};
        sample.CorrectColumn(matrix,0,.4);
        // n=(.6,.8), dq=(1,-2); Dn*dq=(.32,-.24).
        var expected=7+3*.6+5*.8+2*.32+(-1)*(-.24);
        Assert.InRange(Math.Abs(matrix[0,0]-expected),0,2e-14);
    }

    [Fact]
    public void ClearedWeightsCannotLeakFromPreviousSample()
    {
        var write=true; double x=0;
        var sample=new NormalizationLinearization(1,(u,v)=>
        {
            if(write)u[0]=9;
            return(1.0,x);
        });
        sample.CaptureCenter();
        write=false; x=1; sample.CaptureHigh();
        x=-1; sample.CaptureLow();
        var matrix=new double[,]{{0}};
        sample.CorrectColumn(matrix,0,2);
        Assert.Equal(0,matrix[0,0]);
        Assert.Throws<InvalidOperationException>(()=>sample.CorrectColumn(matrix,0,2));
        sample.CaptureHigh();
        sample.CaptureLow();
        sample.CorrectColumn(matrix,0,2);
    }

    [Theory]
    [InlineData(0,0)]
    [InlineData(double.NaN,1)]
    [InlineData(double.PositiveInfinity,1)]
    public void InvalidVectorRejectsEvenWhenAllCoefficientsAreZero(double u,double v)
    {
        var sample=new NormalizationLinearization(1,(_,_) => (u,v));
        Assert.Throws<InvalidOperationException>(sample.CaptureCenter);
        Assert.Throws<InvalidOperationException>(sample.CaptureHigh);
    }

    [Fact]
    public void FailedWriterInvalidatesPreviouslyCapturedState()
    {
        var fail=false;
        var sample=new NormalizationLinearization(1,(_,_) =>
        {
            if(fail)throw new ArgumentException();
            return(1.0,0.0);
        });
        sample.CaptureCenter(); fail=true;
        Assert.Throws<ArgumentException>(sample.CaptureHigh);
        Assert.Throws<InvalidOperationException>(sample.CaptureLow);
    }
}
