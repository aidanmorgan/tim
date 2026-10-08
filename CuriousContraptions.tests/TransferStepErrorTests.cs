using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class TransferStepErrorTests
{
    private static PredictedTransferWork Report(double active,double loss,double uncertainty=0)=>new(new(0),new(0),
        new(0,0,uncertainty,0,0,0),default,new(active,loss,0,0,0,0),0);

    [Fact]
    public void EventCutsAndDissipationCannotRenewOrCancelAllowance()
    {
        var budget=new TransferStepError(1);
        var first=budget.Add([Report(.6,100)],default(WrenchPathWorkResult));
        Assert.Equal(.6,first.UpperBound);
        var failure=Assert.Throws<TransferStepErrorException>(()=>first.Add([Report(.6,100)],default(WrenchPathWorkResult)));
        Assert.True(failure.Error.UpperBound>1);
        Assert.Equal(1,failure.Error.Limit);
        Assert.Equal(.6,first.UpperBound);
    }

    [Fact]
    public void BranchesAndResidualShareTheSameStepLimit()
    {
        var budget=new TransferStepError(1);
        var residual=new WrenchPathWorkResult(.2,.2,0,0,0,0);
        Assert.Throws<TransferStepErrorException>(()=>budget.Add([Report(.4,0),Report(.4,0)],residual));
        var accepted=budget.Add([Report(.1,100,.1)],residual);
        Assert.InRange(accepted.UpperBound,.6,.600000000000001);
    }

    [Fact]
    public void ZeroErrorAndAbsentFamilyHaveNoArtificialCharge()
    {
        Assert.Equal(default,new TransferStepError(0).Add([],null));
        Assert.Equal(0,new TransferStepError(1).Add([Report(0,100)],default(WrenchPathWorkResult)).UpperBound);
        Assert.Throws<ArgumentException>(()=>new TransferStepError(1).Add([Report(0,0)],null));
        Assert.Throws<ArgumentException>(()=>new TransferStepError(1).Add([],default(WrenchPathWorkResult)));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void UnsupportedErrorInputsReject(double invalid)
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>new TransferStepError(invalid));
        Assert.Throws<ArgumentException>(()=>new TransferStepError(1).Add([Report(0,0,invalid)],default(WrenchPathWorkResult)));
    }
}
