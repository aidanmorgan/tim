using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class AxialElasticPotentialTests
{
    [Theory]
    [InlineData(8,0)]
    [InlineData(128,3)]
    public void IntervalWorkExactlyMatchesPotentialDropInBothDirections(double stiffness,double rest)
    {
        var law=new AxialElasticPotential(stiffness,rest);
        foreach(var start in new[]{-2.0,-1,0,1,2})
        foreach(var end in new[]{-2.0,-1,0,1,2})
        {
            var a=rest+start;var b=rest+end;
            Assert.Equal(law.Energy(a)-law.Energy(b),law.IntervalEffort(a,b)*(b-a));
            Assert.Equal(law.IntervalEffort(a,b),law.IntervalEffort(b,a));
            Assert.Equal(-stiffness*start,law.IntervalEffort(a,a));
        }
    }
    [Fact]
    public void VanishingTravelAndRestCrossingDoNotRequireEnergyDivision()
    {
        var law=new AxialElasticPotential(160,0);
        var next=Math.BitIncrement(1.0);
        Assert.Equal(-160*(.5+.5*next),law.IntervalEffort(1,next));
        Assert.Equal(0,law.IntervalEffort(-1,1));
        Assert.Equal(0,law.Energy(0));
        Assert.Equal(law.Energy(-1),law.Energy(1));
    }
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidStiffnessRejects(double stiffness)=>
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AxialElasticPotential(stiffness,0));
    [Fact]
    public void NonfiniteAndUnrepresentableStatesReject()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AxialElasticPotential(1,double.NaN));
        var law=new AxialElasticPotential(160,0);
        Assert.Throws<ArgumentOutOfRangeException>(()=>law.Energy(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(()=>law.IntervalEffort(0,double.PositiveInfinity));
        Assert.Throws<InvalidOperationException>(()=>law.Energy(double.MaxValue));
        Assert.Throws<InvalidOperationException>(()=>law.IntervalEffort(double.MaxValue,double.MaxValue));
        var displaced=new AxialElasticPotential(1,-double.MaxValue);
        Assert.Throws<InvalidOperationException>(()=>displaced.Energy(double.MaxValue));
    }
}
