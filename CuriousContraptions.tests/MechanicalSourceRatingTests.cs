using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class MechanicalSourceRatingTests
{
    [Theory]
    [InlineData(6,100,2,0.5)]
    [InlineData(100,12,2,0.5)]
    [InlineData(100,100,2,1)]
    [InlineData(6,12,2,0.5)]
    public void EveryBranchSharesOneForceAndPowerRating(double force,double power,double speed,double expected)
    {
        var rating=new MechanicalSourceRating(force,power);
        double[] demands=[2,4,6];
        var scale=rating.EffortScale(speed,demands);
        Assert.InRange(scale,expected-1e-14,expected);
        var total=demands.Sum(d=>d*scale);
        Assert.True(total<=force);Assert.True(total*speed<=power);
        Assert.Equal(scale,rating.EffortScale(speed,[6,2,4]));
        Assert.Equal(new double[]{2,4,6},demands);
    }

    [Fact]
    public void DemandSumMayExceedDoubleRangeWithoutDuplicatingCapacity()
    {
        var rating=new MechanicalSourceRating(double.MaxValue,double.MaxValue);
        var scale=rating.EffortScale(1,[double.MaxValue,double.MaxValue]);
        Assert.InRange(scale,.5-1e-14,.5);
        Assert.True(double.IsFinite(double.MaxValue*scale+double.MaxValue*scale));
        Assert.True(double.MaxValue*scale+double.MaxValue*scale<=rating.MaximumForce);
    }

    [Fact]
    public void CommonScalePreservesThePairedPowerIdentity()
    {
        var material=new JetTransferImpedance(2,100);
        var a=material.Evaluate(5,1);var b=material.Evaluate(5,-2);
        var scale=new MechanicalSourceRating(10,20).EffortScale(5,[a.Force,b.Force]);
        var supplied=(a.SourcePower+b.SourcePower)*scale;
        var received=(a.ReceiverPower+b.ReceiverPower)*scale;
        var lost=(a.DissipatedPower+b.DissipatedPower)*scale;
        Assert.InRange(supplied,20-1e-12,20);
        Assert.InRange(supplied-received-lost,-1e-12,1e-12);
        Assert.True(lost>=0);
    }

    [Theory]
    [InlineData(0,10,1)]
    [InlineData(10,0,1)]
    public void DisabledSourceAllocatesZero(double force,double power,double speed)=>
        Assert.Equal(0,new MechanicalSourceRating(force,power).EffortScale(speed,[1,2]));

    [Fact]
    public void StaticReactionRequiresForceCapacityButNoPositivePower()
    {
        Assert.Equal(.5,new MechanicalSourceRating(1,1).EffortScale(0,[1,1]));
        Assert.Equal(1,new MechanicalSourceRating(1,1).EffortScale(0,[0,0]));
    }

    [Fact]
    public void EmptyDemandAndZeroDemandRequireNoReduction()
    {
        var rating=new MechanicalSourceRating(1,1);
        Assert.Equal(1,rating.EffortScale(1,[]));
        Assert.Equal(1,rating.EffortScale(1,[0,0]));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void UnsupportedInputsRejectEvenWhenDisabled(double invalid)
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>new MechanicalSourceRating(invalid,1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new MechanicalSourceRating(1,invalid));
        var rating=new MechanicalSourceRating(0,0);
        Assert.Throws<ArgumentOutOfRangeException>(()=>rating.EffortScale(invalid,[1]));
        Assert.Throws<ArgumentOutOfRangeException>(()=>rating.EffortScale(1,[invalid]));
    }

    [Fact]
    public void UnrepresentablePositiveAllocationsRejectInsteadOfDroppingBranches()
    {
        Assert.Throws<InvalidOperationException>(()=>
            new MechanicalSourceRating(double.Epsilon,double.Epsilon).EffortScale(double.MaxValue,[1]));
        Assert.Throws<InvalidOperationException>(()=>
            new MechanicalSourceRating(1,1).EffortScale(1,[double.Epsilon,double.MaxValue]));
    }

    [Theory]
    [InlineData(-1073)]
    [InlineData(-1000)]
    [InlineData(-500)]
    [InlineData(0)]
    [InlineData(500)]
    [InlineData(1000)]
    [InlineData(1020)]
    public void DirectedAllocationPreservesExactCasesAndRespectsExactRealBudget(int exponent)
    {
        var unit=Math.ScaleB(1,exponent);
        var exact=new MechanicalSourceRating(unit,unit).EffortScale(1,[unit,unit]);
        Assert.Equal(.5,exact);
        double[] demands=[unit,3*unit,7*unit];
        var ceiling=5*unit;
        var rating=new MechanicalSourceRating(ceiling,ceiling);
        var scale=rating.EffortScale(1,demands);
        Assert.Equal(scale,rating.EffortScale(1,demands.Reverse().ToArray()));
        // Independent exact dyadic oracle: every double is an integer multiple
        // of 2^-1074. Compare products without floating-point rounding.
        static System.Numerics.BigInteger Units(double value)
        {
            var bits=BitConverter.DoubleToUInt64Bits(value);
            var field=(int)((bits>>52)&0x7ff);
            var significand=bits&0x000fffffffffffffUL;
            return field==0?new(significand):
                new System.Numerics.BigInteger(significand|0x0010000000000000UL)<<(field-1);
        }
        var sum=demands.Aggregate(System.Numerics.BigInteger.Zero,(total,demand)=>total+Units(demand));
        Assert.True(sum*Units(scale)<=(Units(ceiling)<<1074));
        Assert.True(scale>0);
    }
}
